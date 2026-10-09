namespace SmartPlanif.Domain.Allocation;

/// <summary>Paramètres métier (à valider avec la société, chapitre 10 du document). Jamais de valeurs en dur dans les règles.</summary>
public sealed class AllocationPolicy
{
    public TimeSpan MaxDailyWork { get; set; } = TimeSpan.FromHours(8);
    public TimeSpan MaxAmplitude { get; set; } = TimeSpan.FromHours(12);
    public TimeSpan MinAgentRest { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan MinBusTurnaround { get; set; } = TimeSpan.FromMinutes(15);
    public int MaxTripsPerAgentPerDay { get; set; } = 6;
    public double MaxBusKmPerDay { get; set; } = 500;
    /// <summary>Au-delà de cette durée de repos, le critère "repos" est au maximum.</summary>
    public TimeSpan RestSaturation { get; set; } = TimeSpan.FromHours(4);
    public SoftWeights Weights { get; set; } = new();
}

/// <summary>Poids des critères souples (tableaux 6.1 et 6.2 du document).</summary>
public sealed class SoftWeights
{
    public double BusSameCenter { get; set; } = 40;
    public double BusSameDelegation { get; set; } = 25;
    public double BusCapacityFit { get; set; } = 20;
    public double BusStationContinuity { get; set; } = 20;
    public double BusLowKm { get; set; } = 15;
    public double BusFewTrips { get; set; } = 15;
    public double BusNewer { get; set; } = 10;

    public double AgentSameCenter { get; set; } = 40;
    public double AgentSameDelegation { get; set; } = 25;
    public double AgentRest { get; set; } = 20;
    public double AgentLowHours { get; set; } = 20;
    public double AgentFewTrips { get; set; } = 15;
    public double AgentSameLine { get; set; } = 10;
}
