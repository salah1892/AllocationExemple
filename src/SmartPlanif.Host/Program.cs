using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartPlanif.Application.UseCases;
using SmartPlanif.Domain.Allocation;
using SmartPlanif.Infrastructure;
using SmartPlanif.Infrastructure.InMemory;
using SmartPlanif.Infrastructure.MySql;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<AllocationPolicy>(builder.Configuration.GetSection("Allocation"));
builder.Services.AddApplication();

var cs = builder.Configuration.GetConnectionString("TransportAi");
if (string.IsNullOrWhiteSpace(cs))
    builder.Services.AddInMemoryInfrastructure();                       // démo sans base
else
    builder.Services.AddMySqlInfrastructure(cs, o => builder.Configuration.GetSection("Qualifications").Bind(o));

using var host = builder.Build();

var handler = host.Services.GetRequiredService<AllocateDayHandler>();
var date = args.Length > 0 ? DateOnly.Parse(args[0]) : InMemoryStore.DemoDate;
var report = await handler.HandleAsync(new AllocateDayCommand(date, User: "demo", DryRun: string.IsNullOrWhiteSpace(cs)), CancellationToken.None);

Console.WriteLine($"=== Allocation du {date:yyyy-MM-dd} : {report.Assigned}/{report.Total} voyages affectés ===\n");

foreach (var a in report.Assignments)
{
    Console.WriteLine($"Voyage {a.Trip.Id} (ligne {a.Trip.LineId}, {a.Trip.Departure:HH:mm}-{a.Trip.Arrival:HH:mm})");
    Console.WriteLine($"  Bus {a.Bus.Plate} | Chauffeur {a.Driver.Name} | Receveur {a.Receiver.Name}");
    Console.WriteLine($"  Score {a.Score:0.#}/{a.MaxScore:0.#}  -> {a.Justification}\n");
}

foreach (var f in report.Failures)
{
    Console.WriteLine($"NON AFFECTÉ : voyage {f.Trip.Id} ({f.Trip.Departure:HH:mm}-{f.Trip.Arrival:HH:mm}) - manque : {string.Join(", ", f.MissingRoles)}");
    Console.WriteLine($"  Causes de rejet (code: nb de ressources) : {string.Join(", ", f.RejectionsByCode.Select(kv => $"{kv.Key}: {kv.Value}"))}\n");
}
