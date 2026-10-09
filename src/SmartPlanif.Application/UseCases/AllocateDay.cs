using Microsoft.Extensions.DependencyInjection;
using SmartPlanif.Application.Abstractions;
using SmartPlanif.Domain.Allocation;
using SmartPlanif.Domain.Scheduling;

namespace SmartPlanif.Application.UseCases;

public sealed record AllocateDayCommand(DateOnly Date, string User, bool DryRun = true);

public sealed record AllocationReport(
    int Total, int Assigned, int Unassigned,
    IReadOnlyList<TripAssignment> Assignments, IReadOnlyList<UnassignedTrip> Failures);

public sealed class AllocateDayHandler(
    ITripRepository trips, IResourceRepository resources,
    IAssignmentRepository assignments, IAllocationPolicyProvider policies)
{
    public async Task<AllocationReport> HandleAsync(AllocateDayCommand cmd, CancellationToken ct)
    {
        var policy = await policies.GetAsync(ct);

        // Charger UNE FOIS toutes les données de la fenêtre (J-1..J+1 : repos et voyages après minuit) -> pas de N+1 requêtes.
        var toAllocate = await trips.GetToAllocateAsync(cmd.Date, ct);
        var locked = await trips.GetAssignedAsync(cmd.Date.AddDays(-1), cmd.Date.AddDays(1), ct);
        var pool = await resources.GetPoolAsync(cmd.Date.AddDays(-1), cmd.Date.AddDays(1), ct);

        var book = new ScheduleBook();
        foreach (var a in locked) book.Commit(a.Trip, a.BusId, a.DriverId, a.ReceiverId);

        var result = new TripAllocator(policy).Allocate(toAllocate, pool, book);

        if (!cmd.DryRun && result.Assignments.Count > 0)
            await assignments.SaveProposalsAsync(result.Assignments, cmd.User, ct);

        return new(toAllocate.Count, result.Assignments.Count, result.Unassigned.Count, result.Assignments, result.Unassigned);
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services.AddTransient<AllocateDayHandler>();
}
