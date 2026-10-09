using Microsoft.Extensions.DependencyInjection;
using SmartPlanif.Application.Abstractions;
using SmartPlanif.Domain.Entities;
using SmartPlanif.Infrastructure.InMemory;
using SmartPlanif.Infrastructure.MySql;

namespace SmartPlanif.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInMemoryInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryStore>();
        services.AddSingleton<ITripRepository>(sp => sp.GetRequiredService<InMemoryStore>());
        services.AddSingleton<IResourceRepository>(sp => sp.GetRequiredService<InMemoryStore>());
        services.AddSingleton<IAssignmentRepository>(sp => sp.GetRequiredService<InMemoryStore>());
        services.AddSingleton<IAllocationPolicyProvider, OptionsAllocationPolicyProvider>();
        return services;
    }

    public static IServiceCollection AddMySqlInfrastructure(
        this IServiceCollection services, string connectionString, Action<QualificationOptions> qualifications)
    {
        services.Configure(qualifications);
        services.AddSingleton<IDbConnectionFactory>(new MySqlConnectionFactory(connectionString));
        services.AddTransient<ITripRepository, MySqlTripRepository>();
        services.AddTransient<IResourceRepository, MySqlResourceRepository>();
        services.AddTransient<IAssignmentRepository, MySqlAssignmentRepository>();
        services.AddSingleton<IAllocationPolicyProvider, OptionsAllocationPolicyProvider>();
        return services;
    }
}
