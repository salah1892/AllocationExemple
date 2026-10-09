using SmartPlanif.Domain.Allocation;
using SmartPlanif.Domain.Common;
using SmartPlanif.Domain.Entities;
using SmartPlanif.Domain.Scheduling;
using Xunit;

namespace SmartPlanif.Domain.Tests;

public class TripAllocatorTests
{
    private static readonly DateTime D = new(2026, 10, 8);   // jeudi

    private static Trip T(long id, double fromH, double toH, long center = 1, long line = 10) =>
        new(id, line, D.AddHours(fromH), D.AddHours(toH), 50, center, 1, 1, 2, null);
    private static Bus B(long id, bool active = true, long center = 1) =>
        new(id, $"B{id}", active, 40, 40, 1, center, 1, new DateTime(2022, 1, 1));
    private static Agent A(long id, AgentRole role, DayOfWeek? rest = null, long center = 1) =>
        new(id, $"A{id}", role, rest, center, 1);

    private static AllocationResult Run(IEnumerable<Trip> trips, IEnumerable<Bus> buses, IEnumerable<Agent> agents,
        IEnumerable<Unavailability>? unav = null) =>
        new TripAllocator(new AllocationPolicy()).Allocate(
            trips, new ResourcePool(buses.ToList(), agents.ToList(), unav ?? Array.Empty<Unavailability>()), new ScheduleBook());

    [Fact]
    public void Agent_on_rest_day_is_excluded()
    {
        var r = Run([T(1, 6, 8)], [B(1)],
            [A(1, AgentRole.Driver, DayOfWeek.Thursday), A(2, AgentRole.Driver), A(10, AgentRole.Receiver)]);
        Assert.Equal(2, r.Assignments.Single().Driver.Id);
    }

    [Fact]
    public void Inactive_bus_is_excluded()
    {
        var r = Run([T(1, 6, 8)], [B(1, active: false), B(2)], [A(1, AgentRole.Driver), A(10, AgentRole.Receiver)]);
        Assert.Equal(2, r.Assignments.Single().Bus.Id);
    }

    [Fact]
    public void Bus_in_maintenance_is_excluded()
    {
        var maintenance = new Unavailability(ResourceKind.Bus, 1, new TimeInterval(D.AddHours(5), D.AddHours(9)), "Maintenance");
        var r = Run([T(1, 6, 8)], [B(1), B(2)], [A(1, AgentRole.Driver), A(10, AgentRole.Receiver)], [maintenance]);
        Assert.Equal(2, r.Assignments.Single().Bus.Id);
    }

    [Fact]
    public void Overlapping_trips_cannot_share_the_only_bus()
    {
        var r = Run([T(1, 6, 8), T(2, 7, 9)], [B(1)],
            [A(1, AgentRole.Driver), A(2, AgentRole.Driver), A(10, AgentRole.Receiver), A(11, AgentRole.Receiver)]);
        Assert.Single(r.Assignments);
        var failure = Assert.Single(r.Unassigned);
        Assert.Contains("BUS", failure.MissingRoles);
        Assert.True(failure.RejectionsByCode.ContainsKey("B03"));
    }

    [Fact]
    public void Daily_work_limit_of_8_hours_is_enforced()
    {
        // 4 h + 5 h = 9 h > 8 h pour le seul chauffeur disponible
        var r = Run([T(1, 6, 10), T(2, 11, 16)], [B(1), B(2)], [A(1, AgentRole.Driver), A(10, AgentRole.Receiver), A(11, AgentRole.Receiver)]);
        var failure = Assert.Single(r.Unassigned);
        Assert.Equal(2, failure.Trip.Id);
        Assert.True(failure.RejectionsByCode.ContainsKey("A08"));
    }

    [Fact]
    public void Soft_rules_prefer_same_center()
    {
        var r = Run([T(1, 6, 8, center: 1)], [B(1, center: 2), B(2, center: 1)],
            [A(1, AgentRole.Driver, center: 2), A(2, AgentRole.Driver, center: 1), A(10, AgentRole.Receiver, center: 2), A(11, AgentRole.Receiver, center: 1)]);
        var a = r.Assignments.Single();
        Assert.Equal((2L, 2L, 11L), (a.Bus.Id, a.Driver.Id, a.Receiver.Id));
    }

    [Fact]
    public void Result_is_deterministic()
    {
        var trips = Enumerable.Range(1, 10).Select(i => T(i, 6 + i * 0.5, 7 + i * 0.5)).ToList();
        var buses = Enumerable.Range(1, 5).Select(i => B(i)).ToList();
        var agents = Enumerable.Range(1, 5).Select(i => A(i, AgentRole.Driver))
            .Concat(Enumerable.Range(11, 5).Select(i => A(i, AgentRole.Receiver))).ToList();

        string Sig(AllocationResult x) => string.Join(";", x.Assignments.Select(a => $"{a.Trip.Id}:{a.Bus.Id}/{a.Driver.Id}/{a.Receiver.Id}"));
        Assert.Equal(Sig(Run(trips, buses, agents)), Sig(Run(trips, buses, agents)));
    }

    /// <summary>Vérificateur INDÉPENDANT : quel que soit l'algorithme, aucune ressource ne doit avoir deux voyages qui se chevauchent.</summary>
    [Fact]
    public void Invariant_no_resource_has_overlapping_trips()
    {
        var rnd = new Random(42);
        var trips = Enumerable.Range(1, 60).Select(i =>
        {
            var start = 5 + rnd.NextDouble() * 14;
            return T(i, start, start + 0.5 + rnd.NextDouble() * 2);
        }).ToList();
        var buses = Enumerable.Range(1, 8).Select(i => B(i)).ToList();
        var agents = Enumerable.Range(1, 10).Select(i => A(i, AgentRole.Driver))
            .Concat(Enumerable.Range(11, 10).Select(i => A(i, AgentRole.Receiver))).ToList();

        var r = Run(trips, buses, agents);

        var usage = r.Assignments.SelectMany(a => new[]
        {
            (Key: $"bus{a.Bus.Id}", a.Trip), (Key: $"ag{a.Driver.Id}", a.Trip), (Key: $"ag{a.Receiver.Id}", a.Trip),
        }).GroupBy(x => x.Key);

        foreach (var g in usage)
        {
            var list = g.Select(x => x.Trip).OrderBy(t => t.Departure).ToList();
            for (var i = 1; i < list.Count; i++)
                Assert.True(list[i].Departure >= list[i - 1].Arrival, $"Chevauchement détecté pour {g.Key}");
        }
    }
}
