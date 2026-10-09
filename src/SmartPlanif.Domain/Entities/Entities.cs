using SmartPlanif.Domain.Common;

namespace SmartPlanif.Domain.Entities;

public enum AgentRole { Driver, Receiver, Other }
public enum ResourceKind { Bus, Agent }

public sealed record Agent(
    long Id, string Name, AgentRole Role,
    DayOfWeek? RestDay,               // converti depuis dragent.DECJOUR (1=lundi..7=dimanche) -> attention : DayOfWeek.Sunday = 0 en .NET
    long? CenterId, long? DelegationId);

public sealed record Bus(
    long Id, string Plate, bool Active, int Seats, int Standing,
    long? CategoryId, long? CenterId, long? DelegationId, DateTime? InServiceSince)
{
    public int Capacity => Seats + Standing;
}

/// <summary>Voyage à planifier. Arrival n'existe pas dans trips : elle est dérivée de stop_times (dernier arrêt) ou de dritin.</summary>
public sealed record Trip(
    long Id, long LineId, DateTime Departure, DateTime Arrival, double DistanceKm,
    long? CenterId, long? DelegationId,
    long? OriginStationId, long? DestinationStationId,
    int? ExpectedPassengers)
{
    public TimeInterval Interval => new(Departure, Arrival);
    public DateOnly WorkDate => DateOnly.FromDateTime(Departure);
}

/// <summary>Congé, maintenance, suspension... (nouvelle table resource_unavailability)</summary>
public sealed record Unavailability(ResourceKind Kind, long ResourceId, TimeInterval Period, string Reason);

/// <summary>Voyage déjà affecté (verrouillé) : il consomme de la capacité mais n'est pas replanifié.</summary>
public sealed record AssignedTrip(Trip Trip, long BusId, long DriverId, long ReceiverId);
