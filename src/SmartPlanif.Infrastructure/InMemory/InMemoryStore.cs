using SmartPlanif.Application.Abstractions;
using SmartPlanif.Domain.Allocation;
using SmartPlanif.Domain.Common;
using SmartPlanif.Domain.Entities;

namespace SmartPlanif.Infrastructure.InMemory;

/// <summary>Adaptateur de démonstration : permet d'exécuter l'exemple sans base de données.</summary>
public sealed class InMemoryStore : ITripRepository, IResourceRepository, IAssignmentRepository
{
    public static readonly DateOnly DemoDate = new(2026, 10, 8);   // jeudi
    private static readonly DateTime D = DemoDate.ToDateTime(TimeOnly.MinValue);

    private static Trip T(long id, long line, int h1, int m1, int h2, int m2, double km) =>
        new(id, line, D.AddHours(h1).AddMinutes(m1), D.AddHours(h2).AddMinutes(m2), km, 1, 1, line, line + 100, null);

    private readonly List<Trip> _toAllocate =
    [
        T(1, 10, 6, 0, 8, 0, 60),
        T(2, 11, 7, 0, 9, 0, 55),      // chevauche le voyage 1
        T(3, 10, 8, 30, 10, 30, 60),
        T(4, 12, 9, 0, 11, 0, 50),
        T(5, 10, 11, 0, 17, 0, 180),   // long voyage : teste la limite de 8 h
    ];

    private readonly ResourcePool _pool = new(
        buses:
        [
            new Bus(1, "123 TU 4567", true,  40, 40, 1, 1, 1, new DateTime(2021, 3, 1)),
            new Bus(2, "124 TU 4568", true,  40, 40, 1, 2, 2, new DateTime(2018, 6, 1)),
            new Bus(3, "125 TU 4569", false, 40, 40, 1, 1, 1, new DateTime(2022, 1, 1)),   // inactif
        ],
        agents:
        [
            new Agent(101, "Chauffeur A", AgentRole.Driver,   null, 1, 1),
            new Agent(102, "Chauffeur B", AgentRole.Driver,   null, 2, 2),
            new Agent(103, "Chauffeur C", AgentRole.Driver,   DayOfWeek.Thursday, 1, 1),   // repos le jeudi
            new Agent(201, "Receveur A",  AgentRole.Receiver, null, 1, 1),
            new Agent(202, "Receveur B",  AgentRole.Receiver, null, 2, 2),
            new Agent(203, "Receveur C",  AgentRole.Receiver, null, 1, 1),
        ],
        unavailabilities:
        [
            new Unavailability(ResourceKind.Bus, 1, new TimeInterval(D.AddHours(12), D.AddHours(18)), "Maintenance"),
        ]);

    public List<TripAssignment> Saved { get; } = new();

    public Task<IReadOnlyList<Trip>> GetToAllocateAsync(DateOnly date, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Trip>>(_toAllocate.Where(t => t.WorkDate == date).ToList());

    public Task<IReadOnlyList<AssignedTrip>> GetAssignedAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AssignedTrip>>(Array.Empty<AssignedTrip>());

    public Task<ResourcePool> GetPoolAsync(DateOnly from, DateOnly to, CancellationToken ct) => Task.FromResult(_pool);

    public Task SaveProposalsAsync(IReadOnlyList<TripAssignment> assignments, string user, CancellationToken ct)
    {
        Saved.AddRange(assignments);
        return Task.CompletedTask;
    }
}
