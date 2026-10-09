using SmartPlanif.Domain.Entities;
using SmartPlanif.Domain.Rules;
using SmartPlanif.Domain.Scheduling;

namespace SmartPlanif.Domain.Allocation;

public sealed record Candidate<T>(long Id, T Resource, ResourceSchedule Schedule, IReadOnlyList<Unavailability> Unavailabilities);
public sealed record ScoredCandidate<T>(long Id, T Resource, double Score, IReadOnlyList<ScoreDetail> Details);

public sealed record SelectionResult<T>(IReadOnlyList<ScoredCandidate<T>> Candidates, IReadOnlyDictionary<string, int> RejectionsByCode)
{
    public ScoredCandidate<T>? Best => Candidates.Count > 0 ? Candidates[0] : null;
}

/// <summary>Étape 1 : filtre par règles dures. Étape 2 : score des survivants par règles souples. Tri déterministe (score puis Id).</summary>
public static class CandidateSelector
{
    public static SelectionResult<T> Select<T>(IEnumerable<Candidate<T>> candidates, Trip trip, RuleSet<T> rules, AllocationPolicy policy)
    {
        var accepted = new List<ScoredCandidate<T>>();
        var rejections = new Dictionary<string, int>();

        foreach (var cand in candidates)
        {
            var ctx = new RuleContext<T>(cand.Resource, trip, cand.Schedule, cand.Unavailabilities, policy);

            RuleViolation? violation = null;
            foreach (var rule in rules.Hard)
            {
                violation = rule.Check(ctx);
                if (violation is not null) break;           // court-circuit : première cause de rejet
            }
            if (violation is not null)
            {
                rejections[violation.Code] = rejections.GetValueOrDefault(violation.Code) + 1;
                continue;
            }

            var details = rules.Soft
                .Select(r => new ScoreDetail(r.Code, r.Weight, Math.Clamp(r.Evaluate(ctx), 0, 1)))
                .ToList();
            accepted.Add(new(cand.Id, cand.Resource, details.Sum(d => d.Points), details));
        }

        var sorted = accepted.OrderByDescending(x => x.Score).ThenBy(x => x.Id).ToList();
        return new(sorted, rejections);
    }
}
