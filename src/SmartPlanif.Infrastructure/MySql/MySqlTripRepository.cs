using Dapper;
using SmartPlanif.Application.Abstractions;
using SmartPlanif.Domain.Entities;

namespace SmartPlanif.Infrastructure.MySql;

/// <summary>NON TESTÉ contre votre base : à valider. Les colonnes viennent du dump (trips, stop_times, drligne).</summary>
public sealed class MySqlTripRepository(IDbConnectionFactory db) : ITripRepository
{
    // trips n'a pas d'heure d'arrivée : on la dérive du dernier arrêt (stop_times).
    // ETAT <> 3 : "3 = terminé" d'après le commentaire de la colonne. Les autres valeurs sont à confirmer (V11).
    private const string BaseSql = """
        SELECT t.ID AS Id, t.DENUMLI AS LineId, t.TIME_DEPART AS Departure,
               (SELECT MAX(s.ARRIVAL_TIME) FROM stop_times s WHERE s.TRIP_ID = t.ID) AS Arrival,
               COALESCE((SELECT MAX(s.SHAPE_DIST_TRAVELED) FROM stop_times s WHERE s.TRIP_ID = t.ID), l.DENBRKM, 0) AS DistanceKm,
               l.DECCENT AS CenterId, l.DECDELEG AS DelegationId,
               (SELECT s.DECSTAT FROM stop_times s WHERE s.TRIP_ID = t.ID ORDER BY s.STOP_SEQUENCE ASC  LIMIT 1) AS OriginStationId,
               (SELECT s.DECSTAT FROM stop_times s WHERE s.TRIP_ID = t.ID ORDER BY s.STOP_SEQUENCE DESC LIMIT 1) AS DestinationStationId,
               t.BUS_PR AS BusId, t.CHAUFF_PR AS DriverId, t.REC_PR AS ReceiverId
        FROM trips t
        JOIN drligne l ON l.DENUMLI = t.DENUMLI AND l.DEACTIF = 1
        WHERE t.TIME_DEPART >= @From AND t.TIME_DEPART < @To
          AND COALESCE(t.ETAT, 0) <> 3
        """;

    private sealed class Row
    {
        public long Id { get; set; }
        public long LineId { get; set; }
        public DateTime Departure { get; set; }
        public DateTime? Arrival { get; set; }
        public double DistanceKm { get; set; }
        public long? CenterId { get; set; }
        public long? DelegationId { get; set; }
        public long? OriginStationId { get; set; }
        public long? DestinationStationId { get; set; }
        public long? BusId { get; set; }
        public long? DriverId { get; set; }
        public long? ReceiverId { get; set; }

        // V08 : horaires cohérents, sinon le voyage est écarté (à journaliser).
        public Trip? ToTrip() => Arrival is { } a && a > Departure
            ? new Trip(Id, LineId, Departure, a, DistanceKm, CenterId, DelegationId, OriginStationId, DestinationStationId, null)
            : null;
    }

    private async Task<List<Row>> QueryAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var conn = await db.OpenAsync(ct);
        var rows = await conn.QueryAsync<Row>(new CommandDefinition(
            BaseSql, new { From = from.ToDateTime(TimeOnly.MinValue), To = to.AddDays(1).ToDateTime(TimeOnly.MinValue) },
            cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<Trip>> GetToAllocateAsync(DateOnly date, CancellationToken ct) =>
        (await QueryAsync(date, date, ct))
            .Where(r => r.BusId is null || r.DriverId is null || r.ReceiverId is null)
            .Select(r => r.ToTrip()).OfType<Trip>().ToList();

    public async Task<IReadOnlyList<AssignedTrip>> GetAssignedAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        (await QueryAsync(from, to, ct))
            .Where(r => r.BusId is not null && r.DriverId is not null && r.ReceiverId is not null)
            .Select(r => r.ToTrip() is { } t ? new AssignedTrip(t, r.BusId!.Value, r.DriverId!.Value, r.ReceiverId!.Value) : null)
            .OfType<AssignedTrip>().ToList();
}
