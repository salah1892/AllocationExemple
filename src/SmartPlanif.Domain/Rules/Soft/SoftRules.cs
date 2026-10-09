using SmartPlanif.Domain.Entities;

namespace SmartPlanif.Domain.Rules.Soft;

// Convention : 1 = idéal, 0 = pire, 0.5 = donnée inconnue (neutre, ni bonus ni pénalité).

/// <summary>BP01/BP02/AP01/AP02 : même centre / même délégation que la ligne.</summary>
public sealed class MatchRule<T>(string code, double weight, Func<T, long?> resourceKey, Func<Trip, long?> tripKey) : ISoftRule<T>
{
    public string Code => code;
    public double Weight => weight;
    public double Evaluate(RuleContext<T> c)
    {
        var r = resourceKey(c.Resource); var t = tripKey(c.Trip);
        return r is null || t is null ? 0.5 : r == t ? 1 : 0;
    }
}

/// <summary>BP07/AP05 : peu de voyages déjà faits dans la journée (équilibrage naturel).</summary>
public sealed class FewTripsRule<T>(string code, double weight) : ISoftRule<T>
{
    public string Code => code;
    public double Weight => weight;
    public double Evaluate(RuleContext<T> c) => 1.0 / (1 + c.Schedule.TripCountOn(c.Trip.WorkDate));
}

public sealed class CapacityFitRule(double weight) : ISoftRule<Bus>
{
    public string Code => "BP04";
    public double Weight => weight;
    public double Evaluate(RuleContext<Bus> c) =>
        c.Trip.ExpectedPassengers is { } n && c.Resource.Capacity > 0
            ? Math.Clamp((double)n / c.Resource.Capacity, 0, 1) : 0.5;
}

/// <summary>BP05 : le bus part là où son voyage précédent s'est terminé (peu de déplacement à vide).</summary>
public sealed class StationContinuityRule(double weight) : ISoftRule<Bus>
{
    public string Code => "BP05";
    public double Weight => weight;
    public double Evaluate(RuleContext<Bus> c)
    {
        var prev = c.Schedule.Previous(c.Trip.Departure);
        if (prev is null || prev.DestinationStationId is null || c.Trip.OriginStationId is null) return 0.5;
        return prev.DestinationStationId == c.Trip.OriginStationId ? 1 : 0;
    }
}

public sealed class LowKmRule(double weight) : ISoftRule<Bus>
{
    public string Code => "BP06";
    public double Weight => weight;
    public double Evaluate(RuleContext<Bus> c) =>
        1 - Math.Clamp(c.Schedule.KmOn(c.Trip.WorkDate) / c.Policy.MaxBusKmPerDay, 0, 1);
}

public sealed class NewerBusRule(double weight, double maxAgeYears = 20) : ISoftRule<Bus>
{
    public string Code => "BP08";
    public double Weight => weight;
    public double Evaluate(RuleContext<Bus> c)
    {
        if (c.Resource.InServiceSince is not { } d) return 0.5;
        var age = (c.Trip.Departure - d).TotalDays / 365.25;
        return 1 - Math.Clamp(age / maxAgeYears, 0, 1);
    }
}

/// <summary>AP03 : plus le repos depuis le dernier voyage est long, mieux c'est (saturé à RestSaturation).</summary>
public sealed class RestTimeRule(double weight) : ISoftRule<Agent>
{
    public string Code => "AP03";
    public double Weight => weight;
    public double Evaluate(RuleContext<Agent> c)
    {
        var prev = c.Schedule.Previous(c.Trip.Departure);
        if (prev is null) return 1;
        return Math.Clamp((c.Trip.Departure - prev.Arrival) / c.Policy.RestSaturation, 0, 1);
    }
}

/// <summary>AP04 : peu d'heures déjà travaillées aujourd'hui.</summary>
public sealed class LowWorkedHoursRule(double weight) : ISoftRule<Agent>
{
    public string Code => "AP04";
    public double Weight => weight;
    public double Evaluate(RuleContext<Agent> c) =>
        1 - Math.Clamp(c.Schedule.WorkedOn(c.Trip.WorkDate) / c.Policy.MaxDailyWork, 0, 1);
}

/// <summary>AP06 : continuité sur la même ligne que le voyage précédent.</summary>
public sealed class SameLineContinuityRule(double weight) : ISoftRule<Agent>
{
    public string Code => "AP06";
    public double Weight => weight;
    public double Evaluate(RuleContext<Agent> c)
    {
        var prev = c.Schedule.Previous(c.Trip.Departure);
        return prev is null ? 0.5 : prev.LineId == c.Trip.LineId ? 1 : 0;
    }
}
