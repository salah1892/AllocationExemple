using SmartPlanif.Domain.Entities;
using SmartPlanif.Domain.Rules;
using SmartPlanif.Domain.Scheduling;

namespace SmartPlanif.Domain.Allocation;

public sealed record TripAssignment(Trip Trip, Bus Bus, Agent Driver, Agent Receiver, double Score, double MaxScore, string Justification);

public sealed record UnassignedTrip(Trip Trip, IReadOnlyList<string> MissingRoles, IReadOnlyDictionary<string, int> RejectionsByCode);

public sealed record AllocationResult(IReadOnlyList<TripAssignment> Assignments, IReadOnlyList<UnassignedTrip> Unassigned);

/// <summary>
/// Service de domaine (aucune dépendance technique). Heuristique gloutonne chronologique :
/// pour chaque voyage -> meilleur bus, meilleur chauffeur, meilleur receveur (différent du chauffeur).
/// Le score étant additif et le seul couplage entre rôles étant "chauffeur != receveur", choisir le meilleur de chaque rôle
/// est optimal POUR CE VOYAGE. L'optimum global (tous les voyages) demande un solveur (voir README).
/// </summary>
public sealed class TripAllocator
{
    private readonly AllocationPolicy _policy;
    private readonly RuleSet<Bus> _busRules;
    private readonly RuleSet<Agent> _agentRules;

    public TripAllocator(AllocationPolicy policy)
    {
        _policy = policy;
        _busRules = DefaultRuleSets.ForBus(policy);
        _agentRules = DefaultRuleSets.ForAgent(policy);
    }

    public AllocationResult Allocate(IEnumerable<Trip> trips, ResourcePool pool, ScheduleBook book)
    {
        var assignments = new List<TripAssignment>();
        var unassigned = new List<UnassignedTrip>();
        var maxScore = _busRules.MaxScore + 2 * _agentRules.MaxScore;

        foreach (var trip in trips.OrderBy(t => t.Departure).ThenBy(t => t.Id))
        {
            var buses = CandidateSelector.Select(
                pool.Buses.Select(b => new Candidate<Bus>(b.Id, b, book.Bus(b.Id), pool.UnavailabilityFor(ResourceKind.Bus, b.Id))),
                trip, _busRules, _policy);

            var drivers = SelectAgents(pool, book, trip, AgentRole.Driver);
            var receivers = SelectAgents(pool, book, trip, AgentRole.Receiver);

            var bus = buses.Best;
            var driver = drivers.Best;
            var receiver = receivers.Candidates.FirstOrDefault(c => c.Id != driver?.Id);   // A06

            if (bus is null || driver is null || receiver is null)
            {
                var missing = new List<string>();
                if (bus is null) missing.Add("BUS");
                if (driver is null) missing.Add("DRIVER");
                if (receiver is null) missing.Add("RECEIVER");

                var rejections = new Dictionary<string, int>();
                foreach (var r in new[] { buses.RejectionsByCode, drivers.RejectionsByCode, receivers.RejectionsByCode })
                    foreach (var (code, n) in r)
                        rejections[code] = rejections.GetValueOrDefault(code) + n;

                unassigned.Add(new(trip, missing, rejections));
                continue;                                    // tout ou rien : pas d'affectation partielle
            }

            book.Commit(trip, bus.Id, driver.Id, receiver.Id);   // le voyage suivant voit déjà cette affectation

            assignments.Add(new(trip, bus.Resource, driver.Resource, receiver.Resource,
                bus.Score + driver.Score + receiver.Score, maxScore,
                string.Join(" | ", Explain("Bus", bus), Explain("Chauffeur", driver), Explain("Receveur", receiver))));
        }

        return new(assignments, unassigned);
    }

    private SelectionResult<Agent> SelectAgents(ResourcePool pool, ScheduleBook book, Trip trip, AgentRole role) =>
        CandidateSelector.Select(
            pool.Agents.Where(a => a.Role == role)           // A01/A02 : existence + qualification
                .Select(a => new Candidate<Agent>(a.Id, a, book.Agent(a.Id), pool.UnavailabilityFor(ResourceKind.Agent, a.Id))),
            trip, _agentRules, _policy);

    private static string Explain<T>(string label, ScoredCandidate<T> c) =>
        $"{label} #{c.Id}: {c.Score:0.#} pts [{string.Join(", ", c.Details.Where(d => d.Points > 0).Select(d => $"{d.Code}={d.Points:0.#}"))}]";
}
