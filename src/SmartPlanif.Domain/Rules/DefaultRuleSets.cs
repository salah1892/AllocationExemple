using SmartPlanif.Domain.Allocation;
using SmartPlanif.Domain.Entities;
using SmartPlanif.Domain.Rules.Hard;
using SmartPlanif.Domain.Rules.Soft;

namespace SmartPlanif.Domain.Rules;

/// <summary>Composition des règles. Ajouter une règle = créer une classe + l'ajouter ici (principe ouvert/fermé).</summary>
public static class DefaultRuleSets
{
    public static RuleSet<Bus> ForBus(AllocationPolicy p)
    {
        var w = p.Weights;
        return new RuleSet<Bus>(
            hard: new IHardRule<Bus>[]
            {
                new BusActiveRule(),                                        // B01/B02
                new NoOverlapRule<Bus>("B03"),                              // B03/B04
                new UnavailabilityRule<Bus>("B10"),                         // maintenance
                new MinGapRule<Bus>("B09", x => x.MinBusTurnaround),
                new BusCapacityRule(),                                      // B05
                new BusDailyKmRule(),                                       // B11
            },
            soft: new ISoftRule<Bus>[]
            {
                new MatchRule<Bus>("BP01", w.BusSameCenter,     b => b.CenterId,     t => t.CenterId),
                new MatchRule<Bus>("BP02", w.BusSameDelegation, b => b.DelegationId, t => t.DelegationId),
                new CapacityFitRule(w.BusCapacityFit),
                new StationContinuityRule(w.BusStationContinuity),
                new LowKmRule(w.BusLowKm),
                new FewTripsRule<Bus>("BP07", w.BusFewTrips),
                new NewerBusRule(w.BusNewer),
            });
    }

    public static RuleSet<Agent> ForAgent(AllocationPolicy p)
    {
        var w = p.Weights;
        return new RuleSet<Agent>(
            hard: new IHardRule<Agent>[]
            {
                new RestDayRule(),                                          // A03
                new UnavailabilityRule<Agent>("A04"),                       // congés / absences
                new NoOverlapRule<Agent>("A05"),                            // couvre aussi A06
                new MinGapRule<Agent>("A07", x => x.MinAgentRest),
                new DailyWorkRule(),                                        // A08
                new MaxTripsPerDayRule(),                                   // A09
                new AmplitudeRule(),                                        // A10
            },
            soft: new ISoftRule<Agent>[]
            {
                new MatchRule<Agent>("AP01", w.AgentSameCenter,     a => a.CenterId,     t => t.CenterId),
                new MatchRule<Agent>("AP02", w.AgentSameDelegation, a => a.DelegationId, t => t.DelegationId),
                new RestTimeRule(w.AgentRest),
                new LowWorkedHoursRule(w.AgentLowHours),
                new FewTripsRule<Agent>("AP05", w.AgentFewTrips),
                new SameLineContinuityRule(w.AgentSameLine),
            });
    }
}
