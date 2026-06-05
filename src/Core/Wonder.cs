using System;
using System.Collections.Generic;

namespace CivGame.Core;

public abstract class Wonder : Building
{
    public override int MaintenanceCost => 0;
    public virtual bool IsNationalWonder => false;
    
    public override void OnCompleted(City city, GameSimulation sim)
    {
        sim.ClaimWonder(this);
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
        Register(new GenericWonder("iron_works", "Iron Works", 400, null,
            "Doubles shield production in city. +2 culture."));
        Register(new GenericWonder("smith_mansion", "Smith's Mansion", 400, null,
            "Bank generates 5 gold. Stock Exchange generates 10 gold. +2 culture."));
        Register(new GenericWonder("train_station", "Train Station", 600, null,
            "All cities connected by railroad. +1 science. +1 gold. +2 culture."));
        Register(new GenericWonder("united_nations", "United Nations", 600, null,
            "Diplomatic victory possible. +3 culture."));

        // Modern Era Wonders
        Register(new GenericWonder("apollo_program", "Apollo Program", 800, null,
            "Reveals entire map. +2 culture."));
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
        Register(new GenericSmallWonder("heroic_epic", "Heroic Epic", 200, "iron_working",
            "Military unit barracks effect in this city. Required to build military units here."));
        Register(new GenericSmallWonder("military_academy", "Military Academy", 300, "military_tradition",
            "All units built here as elite. +1 culture."));
        Register(new GenericSmallWonder("university_grounds", "University Grounds", 200, "education",
            "University effect in this city. Required for university."));
        Register(new GenericSmallWonder("bank_of_america", "Bank of America", 300, "the_corporation",
            "Bank effect in this city."));
        Register(new GenericSmallWonder("forbidden_palace", "Forbidden Palace", 300, "code_of_laws",
            "Reduces corruption in all your cities. Can only be built in cities without a Palace."));
        Register(new GenericSmallWonder("mount_rushmore", "Mount Rushmore", 400, "mass_production",
            "Makes 2 citizens happy in this city. +2 culture."));
        Register(new GenericSmallWonder("pentagon", "Pentagon", 400, "iron_working",
            "All units built here as veterans. +2 culture."));
        Register(new GenericSmallWonder("intel_pentagon", "Intelligence Agency", 400, "espionage",
            "Enables CIA/Surveillance outcomes. +2 culture."));
        Register(new GenericSmallWonder("wall_street", "Wall Street", 600, "the_corporation",
            "Stock Exchange effect in this city. +3 culture."));
        Register(new GenericSmallWonder("holocaust_memorial", "Holocaust Memorial", 200, "theology",
            "Makes 2 citizens happy in this city. Eliminates war weariness. +2 culture."));
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