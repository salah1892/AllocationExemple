using SmartPlanif.Domain.Allocation;
using SmartPlanif.Domain.Entities;

namespace SmartPlanif.Application.Abstractions;

public interface ITripRepository
{
    /// <summary>Voyages valides et planifiables du jour (ligne active, non annulés, non clôturés) qui n'ont pas encore les 3 ressources.</summary>
    Task<IReadOnlyList<Trip>> GetToAllocateAsync(DateOnly date, CancellationToken ct);

    /// <summary>Voyages déjà affectés sur la fenêtre [from, to] : ils bloquent leurs ressources.</summary>
    Task<IReadOnlyList<AssignedTrip>> GetAssignedAsync(DateOnly from, DateOnly to, CancellationToken ct);
}

public interface IResourceRepository
{
    Task<ResourcePool> GetPoolAsync(DateOnly from, DateOnly to, CancellationToken ct);
}

public interface IAssignmentRepository
{
    Task SaveProposalsAsync(IReadOnlyList<TripAssignment> assignments, string user, CancellationToken ct);
}

public interface IAllocationPolicyProvider
{
    Task<AllocationPolicy> GetAsync(CancellationToken ct);
}
