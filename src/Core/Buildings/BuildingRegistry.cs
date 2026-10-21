using System.Collections.Generic;

namespace CivGame.Core;

public class GenericBuilding : Building
{
    public override string Id { get; }
    public override string Name { get; }
    public override int ProductionCost { get; }
    public override int MaintenanceCost { get; }
    public override string? RequiredTechId { get; }
    public override string? RequiredResourceId { get; }
    public override int DefenseBonus { get; }
    public override float CommerceMultiplier { get; }

    public GenericBuilding(string id, string name, int cost, int maintenance, string? requiredTech = null, string? requiredResource = null, int defenseBonus = 0, float commerceMultiplier = 1.0f)
    {
        Id = id;
        Name = name;
        ProductionCost = cost;
        MaintenanceCost = maintenance;
        RequiredTechId = requiredTech;
        RequiredResourceId = requiredResource;
        DefenseBonus = defenseBonus;
        CommerceMultiplier = commerceMultiplier;
    }
}

public static class BuildingRegistry
{
    public static readonly Dictionary<string, Building> All = new();

    static BuildingRegistry()
    {
        // Buildings with resource requirements (Strategic resources: iron, coal, uranium, aluminum, rubber)
        Register(new GenericBuilding("aqueduct", "Aqueduct", 100, 1, "construction"));
        Register(new GenericBuilding("barracks", "Barracks", 40, 1));
        Register(new GenericBuilding("bank", "Bank", 160, 1, "banking"));
        Register(new GenericBuilding("cathedral", "Cathedral", 160, 2, "monotheism"));
        Register(new GenericBuilding("civil_defense", "Civil Defense", 120, 1, "electronics"));
        Register(new GenericBuilding("coal_plant", "Coal Plant", 160, 3, "industrialization", "coal"));
        Register(new GenericBuilding("coastal_fortress", "Coastal Fortress", 100, 0, "metallurgy", "iron"));
        Register(new GenericBuilding("colosseum", "Colosseum", 120, 2, "construction"));
        Register(new GenericBuilding("commercial_dock", "Commercial Dock", 160, 2, "mass_production"));
        Register(new GenericBuilding("courthouse", "Courthouse", 80, 1, "code_of_laws"));
        Register(new GenericBuilding("factory", "Factory", 240, 3, "industrialization", "iron"));
        Register(new GenericBuilding("granary", "Granary", 60, 1, "pottery"));
        Register(new GenericBuilding("harbor", "Harbor", 80, 1, "map_making"));
        Register(new GenericBuilding("hospital", "Hospital", 160, 2, "sanitation"));
        Register(new GenericBuilding("hydro_plant", "Hydro Plant", 240, 3, "electronics"));
        Register(new GenericBuilding("library", "Library", 80, 1, "literature"));
        Register(new GenericBuilding("manufacturing_plant", "Manufacturing Plant", 320, 3, "robotics"));
        Register(new GenericBuilding("marketplace", "Marketplace", 100, 1, "currency"));
        Register(new GenericBuilding("mass_transit_system", "Mass Transit System", 200, 3, "ecology", "rubber"));
        Register(new GenericBuilding("monument", "Monument", 60, 0, "ceremonial_burial"));
        Register(new GenericBuilding("nuclear_plant", "Nuclear Plant", 240, 3, "nuclear_power", "uranium"));
        Register(new GenericBuilding("offshore_platform", "Offshore Platform", 240, 3, "miniaturization"));
        Register(new GenericBuilding("palace", "Palace", 100, 0, "masonry"));
        Register(new GenericBuilding("airport", "Airport", 160, 2, "flight"));
        Register(new GenericBuilding("police_station", "Police Station", 160, 2, "communism"));
        Register(new GenericBuilding("recycling_center", "Recycling Center", 200, 2, "recycling"));
        Register(new GenericBuilding("research_lab", "Research Lab", 200, 2, "computers"));
        Register(new GenericBuilding("sam_missile_battery", "SAM Missile Battery", 80, 2, "rocketry", "aluminum"));
        Register(new GenericBuilding("solar_plant", "Solar Plant", 320, 3, "ecology"));
        Register(new GenericBuilding("ss_cockpit", "SS Cockpit", 320, 0, "space_flight", "aluminum"));
        Register(new GenericBuilding("ss_docking_bay", "SS Docking Bay", 160, 0, "space_flight", "aluminum"));
        Register(new GenericBuilding("ss_engine", "SS Engine", 640, 0, "space_flight", "aluminum"));
        Register(new GenericBuilding("ss_exterior_casing", "SS Exterior Casing", 160, 0, "synthetic_fibers"));
        Register(new GenericBuilding("ss_fuel_cells", "SS Fuel Cells", 160, 0, "space_flight", "uranium"));
        Register(new GenericBuilding("ss_life_support_system", "SS Life Support System", 320, 0, "superconductor"));
        Register(new GenericBuilding("ss_planetary_party_lounge", "SS Planetary Party Lounge", 160, 0, "the_laser"));
        Register(new GenericBuilding("ss_stasis_chamber", "SS Stasis Chamber", 320, 0, "synthetic_fibers"));
        Register(new GenericBuilding("ss_storage_supply", "SS Storage/Supply", 160, 0, "synthetic_fibers"));
        Register(new GenericBuilding("ss_thrusters", "SS Thrusters", 320, 0, "satellites"));
        Register(new GenericBuilding("stock_exchange", "Stock Exchange", 200, 3, "the_corporation"));
        Register(new GenericBuilding("temple", "Temple", 60, 1, "ceremonial_burial"));
        Register(new GenericBuilding("university", "University", 200, 2, "education"));
        Register(new GenericBuilding("walls", "Walls", 20, 0, "masonry"));
    }

    private static void Register(Building building) => All[building.Id] = building;

    public static Building? Get(string id) => All.TryGetValue(id, out var b) ? b : null;
}