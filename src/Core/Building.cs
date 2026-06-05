using System;

namespace CivGame.Core;

public abstract class Building
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public virtual string DisplayName => Name;
    public abstract int ProductionCost { get; }
    public abstract int MaintenanceCost { get; }
    public virtual string? RequiredTechId => null;
    public virtual string? RequiredResourceId => null; // Strategic resource requirement

    public virtual int DefenseBonus => 0;
    public virtual float CommerceMultiplier => 1.0f;

    public virtual void OnCompleted(City city, GameSimulation sim) { }
    public virtual string GetDescription() => $"{Name}\nCost: {ProductionCost} | Maintenance: {MaintenanceCost}\nDefense: +{DefenseBonus}% | Commerce: x{CommerceMultiplier}";
}

public class Monument : Building
{
    public override string Id => "monument";
    public override string Name => "Monument";
    public override int ProductionCost => 60;
    public override int MaintenanceCost => 1;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;

    public override void OnCompleted(City city, GameSimulation sim)
    {
        sim.ClaimCityTerritory(city, 2);
        System.Console.WriteLine($"[Cultural Expansion] {city.Name}'s borders expanded due to newly built Monument!");
    }
}

public class Granary : Building
{
    public override string Id => "granary";
    public override string Name => "Granary";
    public override int ProductionCost => 60;
    public override int MaintenanceCost => 1;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;

    public float FoodKeepRatio => 0.5f;
}

public class Aqueduct : Building
{
    public override string Id => "aqueduct";
    public override string Name => "Aqueduct";
    public override int ProductionCost => 100;
    public override int MaintenanceCost => 1;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;
}

public class Hospital : Building
{
    public override string Id => "hospital";
    public override string Name => "Hospital";
    public override int ProductionCost => 160;
    public override int MaintenanceCost => 2;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;
}

// Wonders - 0 maintenance
public class Palace : Building
{
    public override string Id => "palace";
    public override string Name => "Palace";
    public override int ProductionCost => 100;
    public override int MaintenanceCost => 0;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;
}

public class SSCockpit : Building
{
    public override string Id => "ss_cockpit";
    public override string Name => "SS Cockpit";
    public override int ProductionCost => 320;
    public override int MaintenanceCost => 0;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;
}

public class SSDockingBay : Building
{
    public override string Id => "ss_docking_bay";
    public override string Name => "SS Docking Bay";
    public override int ProductionCost => 160;
    public override int MaintenanceCost => 0;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;
}

public class SSEngine : Building
{
    public override string Id => "ss_engine";
    public override string Name => "SS Engine";
    public override int ProductionCost => 640;
    public override int MaintenanceCost => 0;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;
}

public class SSExteriorCasing : Building
{
    public override string Id => "ss_exterior_casing";
    public override string Name => "SS Exterior Casing";
    public override int ProductionCost => 160;
    public override int MaintenanceCost => 0;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;
}

public class SSFuelCells : Building
{
    public override string Id => "ss_fuel_cells";
    public override string Name => "SS Fuel Cells";
    public override int ProductionCost => 160;
    public override int MaintenanceCost => 0;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;
}

public class SSLifeSupportSystem : Building
{
    public override string Id => "ss_life_support_system";
    public override string Name => "SS Life Support System";
    public override int ProductionCost => 320;
    public override int MaintenanceCost => 0;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;
}

public class SSPlanetaryPartyLounge : Building
{
    public override string Id => "ss_planetary_party_lounge";
    public override string Name => "SS Planetary Party Lounge";
    public override int ProductionCost => 160;
    public override int MaintenanceCost => 0;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;
}

public class SSStasisChamber : Building
{
    public override string Id => "ss_stasis_chamber";
    public override string Name => "SS Stasis Chamber";
    public override int ProductionCost => 320;
    public override int MaintenanceCost => 0;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;
}

public class SSStorageSupply : Building
{
    public override string Id => "ss_storage_supply";
    public override string Name => "SS Storage/Supply";
    public override int ProductionCost => 160;
    public override int MaintenanceCost => 0;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;
}

public class SSThrusters : Building
{
    public override string Id => "ss_thrusters";
    public override string Name => "SS Thrusters";
    public override int ProductionCost => 320;
    public override int MaintenanceCost => 0;
    public override int DefenseBonus => 0;
    public override float CommerceMultiplier => 1.0f;
}
