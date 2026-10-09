using Microsoft.Extensions.Options;
using SmartPlanif.Application.Abstractions;
using SmartPlanif.Domain.Allocation;

namespace SmartPlanif.Infrastructure;

/// <summary>Lit la politique depuis appsettings.json (section "Allocation"). Evolution : la lire depuis une table de paramètres modifiable par l'exploitation.</summary>
public sealed class OptionsAllocationPolicyProvider(IOptions<AllocationPolicy> options) : IAllocationPolicyProvider
{
    public Task<AllocationPolicy> GetAsync(CancellationToken ct) => Task.FromResult(options.Value);
}
