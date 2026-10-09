using SmartPlanif.Domain.Common;
using SmartPlanif.Domain.Entities;

namespace SmartPlanif.Domain.Scheduling;

/// <summary>Planning en mémoire d'UNE ressource (bus ou agent). Source de vérité pendant l'exécution du batch.</summary>
public sealed class ResourceSchedule
{
    private readonly List<Trip> _trips = new();
    public IReadOnlyList<Trip> Trips => _trips;

    public void Add(Trip trip)
    {
        _trips.Add(trip);
        _trips.Sort((a, b) => a.Departure.CompareTo(b.Departure));
    }

    public bool Overlaps(TimeInterval interval) => _trips.Any(t => t.Interval.Overlaps(interval));

    public Trip? Previous(DateTime at) => _trips.Where(t => t.Arrival <= at).MaxBy(t => t.Arrival);
    public Trip? Next(DateTime at) => _trips.Where(t => t.Departure >= at).MinBy(t => t.Departure);

    private IEnumerable<Trip> On(DateOnly day) => _trips.Where(t => t.WorkDate == day);

    public TimeSpan WorkedOn(DateOnly day) => On(day).Aggregate(TimeSpan.Zero, (sum, t) => sum + t.Interval.Duration);
    public int TripCountOn(DateOnly day) => On(day).Count();
    public double KmOn(DateOnly day) => On(day).Sum(t => t.DistanceKm);

    /// <summary>Amplitude (premier départ -> dernière arrivée) du jour, en incluant le voyage candidat.</summary>
    public TimeSpan SpanOn(DateOnly day, TimeInterval candidate)
    {
        var start = candidate.Start;
        var end = candidate.End;
        foreach (var t in On(day))
        {
            if (t.Departure < start) start = t.Departure;
            if (t.Arrival > end) end = t.Arrival;
        }
        return end - start;
    }
}
