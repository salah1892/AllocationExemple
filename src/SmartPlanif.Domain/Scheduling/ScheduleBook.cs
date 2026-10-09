using SmartPlanif.Domain.Entities;

namespace SmartPlanif.Domain.Scheduling;

/// <summary>
/// Registre des plannings. Un agent a UN SEUL planning, qu'il soit chauffeur ou receveur :
/// la règle A06 (jamais chauffeur ET receveur en même temps) découle donc du test de chevauchement.
/// </summary>
public sealed class ScheduleBook
{
    private readonly Dictionary<long, ResourceSchedule> _buses = new();
    private readonly Dictionary<long, ResourceSchedule> _agents = new();

    public ResourceSchedule Bus(long id) => _buses.TryGetValue(id, out var s) ? s : _buses[id] = new();
    public ResourceSchedule Agent(long id) => _agents.TryGetValue(id, out var s) ? s : _agents[id] = new();

    public void Commit(Trip trip, long busId, long driverId, long receiverId)
    {
        Bus(busId).Add(trip);
        Agent(driverId).Add(trip);
        Agent(receiverId).Add(trip);
    }
}
