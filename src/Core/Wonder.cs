using System;
using System.Collections.Generic;

namespace CivGame.Core;

public abstract class Wonder : Building
{
    public override int MaintenanceCost => 0;
    public virtual bool IsNationalWonder => false;
    
    public override void OnCompleted(City city, GameSimulation sim)
    {
        sim.ClaimWonder(this, city.Faction);
        System.Console.WriteLine($"[Wonder] The {Name} has been completed in {city.Name}!");
    }
    
    public virtual void OnBuiltGlobally(GameSimulation sim) { }
}

public abstract class SmallWonder : Wonder
{
    public override bool IsNationalWonder => true;
}

public static class WonderRegistry
{
    public static readonly Dictionary<string, Wonder> All = new();

    static WonderRegistry()
    {
        // Ancient Era Wonders
        Register(new GenericWonder("pyramids", "Pyramids", 200, null,
            "Each city gets granary effect automatically. City growth rate +1."));
        Register(new GenericWonder("hanging_gardens", "Hanging Gardens", 200, null,
            "Makes unhappy citizens content. +1 culture in all cities."));
        Register(new GenericWonder("colossus", "Colossus", 200, null,
            "Makes coastal tiles near city produce 2 food. +2 culture."));
        Register(new GenericWonder("temple_of_artemis", "Temple of Artemis", 300, null,
            "Makes 2 citizens happy. +2 culture."));
        Register(new GenericWonder("great_wall", "Great Wall", 300, null,
            "Defense bonus for all cities. +2 culture."));
        Register(new GenericWonder("statue_of_zeus", "Statue of Zeus", 200, null,
            "Increases city growth rate. +2 culture. Eliminates war weariness."));
        Register(new GenericWonder("oracle", "Oracle", 300, null,
            "Temples have +2 additional happy faces. +3 culture."));
        Register(new GenericWonder("louvre", "Louvre", 800, null,
            "Automatically obsolete all improvements. Doubles culture."));

        // Medieval Era Wonders
        Register(new GenericWonder("knights_hall", "Knights Hall", 300, null,
            "All units built here as veterans. +2 culture."));
        Register(new GenericWonder("sovereign_bath", "Sovereign Bath", 400, null,
            "Makes 2 citizens happy. +2 culture. Eliminates fear from civil wars."));
        Register(new GenericWonder("leonardo_workshop", "Leonardo's Workshop", 400, null,
            "All military units heal in 1 turn. +1 culture. Obsolete obsolete units."));
        Register(new GenericWonder("shakespeares_theatre", "Shakespeare's Theatre", 300, null,
            "Makes 2 citizens happy. +2 culture."));
        Register(new GenericWonder("sun_tzu_war_academy", "Sun Tzu's War Academy", 400, null,
            "All units built here as veterans. +2 culture."));
        Register(new GenericWonder("cure_for_cancer", "Cure for Cancer", 600, null,
            "Makes 2 citizens happy in every city. +2 culture."));
        Register(new GenericWonder("sistine_chapel", "Sistine Chapel", 500, null,
            "Doubles wonder production speed. +3 culture."));
        Register(new GenericWonder("taj_mahal", "Taj Mahal", 500, null,
            "Makes 2 citizens happy. +2 culture. Obsolete if wonder never obsolete."));

        // Industrial Era Wonders
        Register(new GenericWonder("astrolabe", "Astrolabe", 400, null,
            "Doubles science output in city. +2 culture."));
        Register(new GenericWonder("hermitage", "Hermitage", 500, null,
            "Makes 2 citizens happy. +2 culture. Increases cultural expansion."));
        Register(new GenericWonder("smith_mansion", "Smith's Mansion", 400, null,
            "Bank generates 5 gold. Stock Exchange generates 10 gold. +2 culture."));
        Register(new GenericWonder("train_station", "Train Station", 600, null,
            "All cities connected by railroad. +1 science. +1 gold. +2 culture."));
        Register(new GenericWonder("united_nations", "United Nations", 600, null,
            "Diplomatic victory possible. +3 culture."));

        // Modern Era Wonders
        Register(new GenericWonder("manhattan_project", "Manhattan Project", 800, null,
            "Nuclear weapons available to all. +2 culture."));
        Register(new GenericWonder("internet", "Internet", 800, null,
            "Doubles science output in all cities. +2 culture. Obsolete immediately."));
        Register(new GenericWonder("longevity_vaccine", "Longevity Vaccine", 600, null,
            "Makes 2 citizens happy everywhere. +1 culture."));
        Register(new GenericWonder("mars_colony", "Mars Colony", 800, null,
            "+1 happy face. +2 food in capital. Eliminates overcrowding. +3 culture."));
        Register(new GenericWonder("world_bank", "World Bank", 1000, null,
            "All players have access to your treasury. +3 culture."));
        Register(new GenericWonder("space_station", "Space Station", 800, null,
            "Counts as global communications. +3 culture."));

        // Small Wonders (National Wonders)
        Register(new GenericSmallWonder("heroic_epic", "Heroic Epic", 200, "literature",
            "Increases attacker's combat strength by +25% globally. Requires a Victorious Unit."));
        Register(new GenericSmallWonder("military_academy", "Military Academy", 300, "military_tradition",
            "Units built in this city start with +25% max health. Requires a Victorious Unit."));
        Register(new GenericSmallWonder("pentagon", "The Pentagon", 400, "military_tradition",
            "All units receive a +25% defense bonus globally. Requires 3 military units of strength >= 2."));
        Register(new GenericSmallWonder("forbidden_palace", "Forbidden Palace", 300, "code_of_laws",
            "Acts as a second Palace. Grants city +50% commerce bonus. Requires at least 4 cities."));
        Register(new GenericSmallWonder("wall_street", "Wall Street", 600, "the_corporation",
            "Generates 5% interest on treasury up to 50 gold per turn. Requires 3 Banks."));
        Register(new GenericSmallWonder("intelligence_agency", "Intelligence Agency", 400, "espionage",
            "Spy network generates +10 gold per turn."));
        Register(new GenericSmallWonder("battlefield_medicine", "Battlefield Medicine", 400, "medicine",
            "Heals all wounded units by +15 HP at the end of each turn. Requires 3 Hospitals."));
        Register(new GenericSmallWonder("sdi_defense", "SDI Defense", 500, "superconductor",
            "Star Wars shield grants +100% defense bonus in this city."));
        Register(new GenericSmallWonder("apollo_program", "Apollo Program", 800, "space_flight",
            "Enables construction of spaceship components."));
        Register(new GenericSmallWonder("iron_works", "Iron Works", 400, "steam_power",
            "Doubles production in this city. Requires both Iron and Coal resources in workable tiles."));
    }

    private static void Register(Wonder wonder) => All[wonder.Id] = wonder;

    public static Wonder? Get(string id) => All.TryGetValue(id, out var w) ? w : null;
}

public class GenericWonder : Wonder
{
    public override string Id { get; }
    public override string Name { get; }
    public override int ProductionCost { get; }
    public override string? RequiredTechId { get; }
    public override string? RequiredResourceId { get; }
    public override int DefenseBonus { get; }
    public override float CommerceMultiplier { get; }
    public string Description { get; }

    public GenericWonder(string id, string name, int cost, string? requiredTech, string description, 
        string? requiredResource = null, int defenseBonus = 0, float commerceMultiplier = 1.0f)
    {
        Id = id;
        Name = name;
        ProductionCost = cost;
        RequiredTechId = requiredTech;
        RequiredResourceId = requiredResource;
        Description = description;
        DefenseBonus = defenseBonus;
        CommerceMultiplier = commerceMultiplier;
    }
}

public class GenericSmallWonder : SmallWonder
{
    public override string Id { get; }
    public override string Name { get; }
    public override int ProductionCost { get; }
    public override string? RequiredTechId { get; }
    public override string? RequiredResourceId { get; }
    public override int DefenseBonus { get; }
    public override float CommerceMultiplier { get; }
    public string Description { get; }

    public GenericSmallWonder(string id, string name, int cost, string? requiredTech, string description,
        string? requiredResource = null, int defenseBonus = 0, float commerceMultiplier = 1.0f)
    {
        Id = id;
        Name = name;
        ProductionCost = cost;
        RequiredTechId = requiredTech;
        RequiredResourceId = requiredResource;
        Description = description;
        DefenseBonus = defenseBonus;
        CommerceMultiplier = commerceMultiplier;
    }
}