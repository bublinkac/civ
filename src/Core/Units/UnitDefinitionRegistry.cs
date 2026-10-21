using System;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.Core;

public class UnitDefinition
{
    public UnitType Type { get; }
    public string Name { get; }
    public TechEra Era { get; }
    public int AttackStrength { get; }
    public int DefenseStrength { get; }
    public float Movement { get; }
    public int VisionRange { get; }
    public int ShieldCost { get; }
    public string? RequiredTechId { get; }
    public string[] RequiredResourceIds { get; }
    public int BombardStrength { get; }
    public int BombardRange { get; }
    public UnitType? UpgradesTo { get; }
    public bool IsUniqueUnit { get; }
    public UnitType? ReplacesUnit { get; }
    public string? CivId { get; }
    public string Description { get; }

    public UnitDomain Domain { get; }
    public int TransportCapacity { get; }
    public int OperationalRange { get; }
    public bool SinksInSeaOcean { get; }
    public bool IsStealth { get; }
    public bool DetectsStealth { get; }
    public bool HasHiddenNationality { get; }

    public UnitDefinition(
        UnitType type,
        string name,
        TechEra era,
        int attack,
        int defense,
        float movement,
        int shieldCost,
        string? requiredTechId = null,
        string[]? requiredResourceIds = null,
        int visionRange = 1,
        int bombardStrength = 0,
        int bombardRange = 0,
        UnitType? upgradesTo = null,
        bool isUniqueUnit = false,
        UnitType? replacesUnit = null,
        string? civId = null,
        string description = "",
        UnitDomain domain = UnitDomain.Land,
        int transportCapacity = 0,
        int operationalRange = 0,
        bool sinksInSeaOcean = false,
        bool isStealth = false,
        bool detectsStealth = false,
        bool hasHiddenNationality = false)
    {
        Type = type;
        Name = name;
        Era = era;
        AttackStrength = attack;
        DefenseStrength = defense;
        Movement = movement;
        ShieldCost = shieldCost;
        RequiredTechId = requiredTechId;
        RequiredResourceIds = requiredResourceIds ?? Array.Empty<string>();
        VisionRange = visionRange;
        BombardStrength = bombardStrength;
        BombardRange = bombardRange;
        UpgradesTo = upgradesTo;
        IsUniqueUnit = isUniqueUnit;
        ReplacesUnit = replacesUnit;
        CivId = civId;
        Description = description;
        Domain = domain;
        TransportCapacity = transportCapacity;
        OperationalRange = operationalRange;
        SinksInSeaOcean = sinksInSeaOcean;
        IsStealth = isStealth;
        DetectsStealth = detectsStealth;
        HasHiddenNationality = hasHiddenNationality;
    }
}

public static class UnitDefinitionRegistry
{
    private static readonly Dictionary<UnitType, UnitDefinition> _definitions = new();

    static UnitDefinitionRegistry()
    {
        // ═══════════════════════════════════════════════════════
        // CIVILIAN UNITS
        // ═══════════════════════════════════════════════════════
        Register(new(UnitType.Settler, "Settler", TechEra.Ancient, 0, 1, 1f, 30, visionRange: 2, description: "Founds new cities."));
        Register(new(UnitType.Worker, "Worker", TechEra.Ancient, 0, 1, 1f, 10, visionRange: 1, description: "Builds roads, mines, irrigation."));
        Register(new(UnitType.Explorer, "Explorer", TechEra.Ancient, 1, 1, 2f, 20, visionRange: 2, description: "Fast scout unit."));
        Register(new(UnitType.Barbarian, "Barbarian", TechEra.Ancient, 2, 1, 1f, 15, visionRange: 1, description: "Hostile wandering raider."));

        // ═══════════════════════════════════════════════════════
        // ANCIENT ERA (Standard Units)
        // ═══════════════════════════════════════════════════════
        Register(new(UnitType.Warrior, "Warrior", TechEra.Ancient, 1, 1, 1f, 10,
            upgradesTo: UnitType.Swordsman, description: "Basic club-wielding infantry."));

        Register(new(UnitType.Spearman, "Spearman", TechEra.Ancient, 1, 2, 1f, 20,
            requiredTechId: "bronze_working", upgradesTo: UnitType.Pikeman, description: "Stalwart defensive infantry."));

        Register(new(UnitType.Archer, "Archer", TechEra.Ancient, 2, 1, 1f, 20,
            requiredTechId: "warrior_code", visionRange: 2, upgradesTo: UnitType.Longbowman, description: "Ranged arrow attacker."));

        Register(new(UnitType.Swordsman, "Swordsman", TechEra.Ancient, 3, 2, 1f, 30,
            requiredTechId: "iron_working", requiredResourceIds: new[] { "iron" }, upgradesTo: UnitType.MedievalInfantry, description: "Iron-age heavy offensive infantry."));

        Register(new(UnitType.Chariot, "Chariot", TechEra.Ancient, 1, 1, 2f, 20,
            requiredTechId: "the_wheel", requiredResourceIds: new[] { "horses" }, upgradesTo: UnitType.Horseman, description: "Swift horse-drawn carriage."));

        Register(new(UnitType.Horseman, "Horseman", TechEra.Ancient, 2, 1, 2f, 30,
            requiredTechId: "horseback_riding", requiredResourceIds: new[] { "horses" }, upgradesTo: UnitType.Knight, description: "Fast mounted raider."));

        Register(new(UnitType.Catapult, "Catapult", TechEra.Ancient, 0, 0, 1f, 20,
            requiredTechId: "mathematics", bombardStrength: 4, bombardRange: 1, upgradesTo: UnitType.Trebuchet, description: "Early siege engine for bombarding enemy walls."));

        // ═══════════════════════════════════════════════════════
        // MIDDLE AGES (Standard Units)
        // ═══════════════════════════════════════════════════════
        Register(new(UnitType.Pikeman, "Pikeman", TechEra.MiddleAges, 1, 3, 1f, 30,
            requiredTechId: "feudalism", requiredResourceIds: new[] { "iron" }, upgradesTo: UnitType.Musketman, description: "Anti-cavalry defensive infantry with long pikes."));

        Register(new(UnitType.MedievalInfantry, "Medieval Infantry", TechEra.MiddleAges, 4, 2, 1f, 40,
            requiredTechId: "feudalism", requiredResourceIds: new[] { "iron" }, upgradesTo: UnitType.Guerrilla, description: "Broadsword-wielding medieval assault infantry."));

        Register(new(UnitType.Longbowman, "Longbowman", TechEra.MiddleAges, 4, 1, 1f, 40,
            requiredTechId: "invention", visionRange: 2, upgradesTo: UnitType.Guerrilla, description: "Devastating ranged offensive infantry."));

        Register(new(UnitType.Knight, "Knight", TechEra.MiddleAges, 4, 3, 2f, 70,
            requiredTechId: "chivalry", requiredResourceIds: new[] { "horses", "iron" }, upgradesTo: UnitType.Cavalry, description: "Armored noble mounted warrior."));

        Register(new(UnitType.Musketman, "Musketman", TechEra.MiddleAges, 2, 4, 1f, 60,
            requiredTechId: "gunpowder", requiredResourceIds: new[] { "saltpeter" }, upgradesTo: UnitType.Rifleman, description: "Gunpowder-armed premier defensive infantry."));

        Register(new(UnitType.Trebuchet, "Trebuchet", TechEra.MiddleAges, 0, 0, 1f, 30,
            requiredTechId: "engineering", bombardStrength: 6, bombardRange: 1, upgradesTo: UnitType.Cannon, description: "Powerful medieval siege engine."));

        Register(new(UnitType.Cannon, "Cannon", TechEra.MiddleAges, 0, 0, 1f, 40,
            requiredTechId: "metallurgy", requiredResourceIds: new[] { "iron", "saltpeter" }, bombardStrength: 8, bombardRange: 1, upgradesTo: UnitType.Artillery, description: "Early gunpowder siege cannon."));

        Register(new(UnitType.Crusader, "Crusader", TechEra.MiddleAges, 5, 3, 1f, 70,
            requiredTechId: "chivalry", description: "Zealous heavy knight on foot."));

        Register(new(UnitType.AncientCavalry, "Ancient Cavalry", TechEra.MiddleAges, 3, 2, 2f, 40,
            requiredTechId: "horseback_riding", description: "Veteran armored horseman (+1 HP)."));

        // ═══════════════════════════════════════════════════════
        // INDUSTRIAL ERA (Standard Units)
        // ═══════════════════════════════════════════════════════
        Register(new(UnitType.Rifleman, "Rifleman", TechEra.Industrial, 4, 6, 1f, 80,
            requiredTechId: "nationalism", upgradesTo: UnitType.Infantry, description: "Rifled-musket defensive troop."));

        Register(new(UnitType.Infantry, "Infantry", TechEra.Industrial, 6, 10, 1f, 90,
            requiredTechId: "replaceable_parts", requiredResourceIds: new[] { "rubber" }, upgradesTo: UnitType.MechInfantry, description: "Bolt-action trench infantry with massive defense."));

        Register(new(UnitType.Cavalry, "Cavalry", TechEra.Industrial, 6, 3, 3f, 80,
            requiredTechId: "military_tradition", requiredResourceIds: new[] { "horses", "saltpeter" }, upgradesTo: UnitType.Tank, description: "Rapid saber-and-carbine cavalry charge."));

        Register(new(UnitType.Guerrilla, "Guerrilla", TechEra.Industrial, 6, 6, 1f, 90,
            requiredTechId: "replaceable_parts", upgradesTo: UnitType.TOWInfantry, description: "Resource-free versatile insurgent infantry."));

        Register(new(UnitType.Artillery, "Artillery", TechEra.Industrial, 0, 0, 1f, 80,
            requiredTechId: "replaceable_parts", bombardStrength: 12, bombardRange: 2, upgradesTo: UnitType.RadarArtillery, description: "Long-range howitzer capable of 2-tile bombard."));

        Register(new(UnitType.Flak, "Flak", TechEra.Industrial, 1, 2, 1f, 60,
            requiredTechId: "replaceable_parts", upgradesTo: UnitType.MobileSAM, description: "Anti-aircraft mobile flak cannon."));

        Register(new(UnitType.Marine, "Marine", TechEra.Industrial, 8, 6, 1f, 100,
            requiredTechId: "amphibious_war", description: "Elite amphibious assault infantry."));

        Register(new(UnitType.Paratrooper, "Paratrooper", TechEra.Industrial, 6, 8, 1f, 110,
            requiredTechId: "advanced_flight", upgradesTo: UnitType.ModernParatrooper, description: "Airborne drop infantry."));

        Register(new(UnitType.Tank, "Tank", TechEra.Industrial, 16, 8, 2f, 100,
            requiredTechId: "motorized_transportation", requiredResourceIds: new[] { "oil", "rubber" }, upgradesTo: UnitType.ModernArmor, description: "Heavy armored war machine with lethal punch."));

        // ═══════════════════════════════════════════════════════
        // MODERN ERA (Standard Units)
        // ═══════════════════════════════════════════════════════
        Register(new(UnitType.MechInfantry, "Mech Infantry", TechEra.Modern, 12, 18, 2f, 110,
            requiredTechId: "computers", requiredResourceIds: new[] { "rubber" }, description: "Armored personnel carrier infantry with devastating defense."));

        Register(new(UnitType.ModernArmor, "Modern Armor", TechEra.Modern, 24, 16, 3f, 120,
            requiredTechId: "synthetic_fibers", requiredResourceIds: new[] { "aluminum", "oil" }, description: "Apex main battle tank with extreme firepower and speed."));

        Register(new(UnitType.TOWInfantry, "TOW Infantry", TechEra.Modern, 12, 22, 1f, 120,
            requiredTechId: "rocketry", description: "Guided anti-tank missile defense squad."));

        Register(new(UnitType.ModernParatrooper, "Modern Paratrooper", TechEra.Modern, 11, 11, 1f, 120,
            requiredTechId: "synthetic_fibers", description: "High-tech airborne shock infantry."));

        Register(new(UnitType.MobileSAM, "Mobile SAM", TechEra.Modern, 1, 4, 2f, 100,
            requiredTechId: "robotics", description: "Surface-to-air missile defense vehicle."));

        Register(new(UnitType.RadarArtillery, "Radar Artillery", TechEra.Modern, 0, 0, 2f, 100,
            requiredTechId: "robotics", requiredResourceIds: new[] { "aluminum" }, bombardStrength: 16, bombardRange: 2, description: "Automated precision radar-guided artillery."));

        // ═══════════════════════════════════════════════════════
        // UNIQUE UNITS (Civ-specific)
        // ═══════════════════════════════════════════════════════
        Register(new(UnitType.Legionary, "Legionary", TechEra.Ancient, 3, 3, 1f, 30,
            requiredTechId: "iron_working", requiredResourceIds: new[] { "iron" }, upgradesTo: UnitType.MedievalInfantry,
            isUniqueUnit: true, replacesUnit: UnitType.Swordsman, civId: "rome",
            description: "Roman heavy infantry with superior shields (+1 Def)."));

        Register(new(UnitType.Hoplite, "Hoplite", TechEra.Ancient, 1, 3, 1f, 20,
            requiredTechId: "bronze_working", upgradesTo: UnitType.Pikeman,
            isUniqueUnit: true, replacesUnit: UnitType.Spearman, civId: "greece",
            description: "Greek phalanx with massive defensive discipline (+1 Def)."));

        Register(new(UnitType.Immortals, "Immortals", TechEra.Ancient, 4, 2, 1f, 30,
            requiredTechId: "iron_working", requiredResourceIds: new[] { "iron" }, upgradesTo: UnitType.MedievalInfantry,
            isUniqueUnit: true, replacesUnit: UnitType.Swordsman, civId: "persia",
            description: "Persian royal guard with devastating attack strength (4 Atk)."));

        Register(new(UnitType.WarChariot, "War Chariot", TechEra.Ancient, 2, 1, 2f, 20,
            requiredTechId: "the_wheel", upgradesTo: UnitType.Horseman,
            isUniqueUnit: true, replacesUnit: UnitType.Chariot, civId: "egypt",
            description: "Egyptian chariot with increased attack (+1 Atk), requires no horses."));

        Register(new(UnitType.MountedWarrior, "Mounted Warrior", TechEra.Ancient, 3, 1, 2f, 30,
            requiredTechId: "horseback_riding", requiredResourceIds: new[] { "horses" }, upgradesTo: UnitType.Knight,
            isUniqueUnit: true, replacesUnit: UnitType.Horseman, civId: "iroquois",
            description: "Iroquois swift horseman with potent strike (+1 Atk)."));

        Register(new(UnitType.JaguarWarrior, "Jaguar Warrior", TechEra.Ancient, 1, 1, 2f, 10,
            upgradesTo: UnitType.Swordsman,
            isUniqueUnit: true, replacesUnit: UnitType.Warrior, civId: "aztec",
            description: "Aztec warrior with swift jungle speed (2 Mov)."));

        Register(new(UnitType.EnkiduWarrior, "Enkidu Warrior", TechEra.Ancient, 1, 2, 1f, 10,
            upgradesTo: UnitType.Pikeman,
            isUniqueUnit: true, replacesUnit: UnitType.Warrior, civId: "sumeria",
            description: "Sumerian warrior with great shield defense (+1 Def)."));

        Register(new(UnitType.Impi, "Impi", TechEra.Ancient, 1, 2, 2f, 20,
            requiredTechId: "bronze_working", upgradesTo: UnitType.Pikeman,
            isUniqueUnit: true, replacesUnit: UnitType.Spearman, civId: "zulu",
            description: "Zulu agile spearmen with rapid scouting speed (2 Mov)."));

        Register(new(UnitType.NumidianMercenary, "Numidian Mercenary", TechEra.Ancient, 2, 3, 1f, 30,
            requiredTechId: "bronze_working", upgradesTo: UnitType.Pikeman,
            isUniqueUnit: true, replacesUnit: UnitType.Spearman, civId: "carthage",
            description: "Carthaginian elite mercenary with balanced attack and defense (2/3/1)."));

        Register(new(UnitType.Bowman, "Bowman", TechEra.Ancient, 2, 2, 1f, 20,
            requiredTechId: "warrior_code", visionRange: 2, upgradesTo: UnitType.Longbowman,
            isUniqueUnit: true, replacesUnit: UnitType.Archer, civId: "babylon",
            description: "Babylonian defensive marksman with enhanced defense (2/2/1)."));

        Register(new(UnitType.JavelinThrower, "Javelin Thrower", TechEra.Ancient, 2, 2, 1f, 30,
            requiredTechId: "warrior_code", visionRange: 2, upgradesTo: UnitType.Longbowman,
            isUniqueUnit: true, replacesUnit: UnitType.Archer, civId: "maya",
            description: "Mayan ranged skirmisher with solid defense (+1 Def)."));

        Register(new(UnitType.ThreeManChariot, "Three-Man Chariot", TechEra.Ancient, 2, 1, 2f, 20,
            requiredTechId: "the_wheel", requiredResourceIds: new[] { "horses" }, upgradesTo: UnitType.Horseman,
            isUniqueUnit: true, replacesUnit: UnitType.Chariot, civId: "hittites",
            description: "Hittite heavy chariot carrying extra crew (+1 Atk)."));

        Register(new(UnitType.ChasquiScout, "Chasqui Scout", TechEra.Ancient, 1, 1, 2f, 20,
            visionRange: 2, isUniqueUnit: true, replacesUnit: UnitType.Explorer, civId: "inca",
            description: "Incan mountain runner ignoring terrain penalties."));

        Register(new(UnitType.GallicSwordsman, "Gallic Swordsman", TechEra.Ancient, 3, 2, 2f, 40,
            requiredTechId: "iron_working", requiredResourceIds: new[] { "iron" }, upgradesTo: UnitType.MedievalInfantry,
            isUniqueUnit: true, replacesUnit: UnitType.Swordsman, civId: "celts",
            description: "Celtic fast-charging swordsman (2 Mov)."));

        // Middle Ages UUs
        Register(new(UnitType.Samurai, "Samurai", TechEra.MiddleAges, 4, 4, 2f, 70,
            requiredTechId: "chivalry", requiredResourceIds: new[] { "iron" }, upgradesTo: UnitType.Cavalry,
            isUniqueUnit: true, replacesUnit: UnitType.Knight, civId: "japan",
            description: "Japanese master swordsman (+1 Def, requires no horses)."));

        Register(new(UnitType.Rider, "Rider", TechEra.MiddleAges, 4, 3, 3f, 70,
            requiredTechId: "chivalry", requiredResourceIds: new[] { "horses", "iron" }, upgradesTo: UnitType.Cavalry,
            isUniqueUnit: true, replacesUnit: UnitType.Knight, civId: "china",
            description: "Chinese fast-moving cavalry (3 Mov)."));

        Register(new(UnitType.AnsarWarrior, "Ansar Warrior", TechEra.MiddleAges, 4, 2, 3f, 60,
            requiredTechId: "chivalry", requiredResourceIds: new[] { "horses" }, upgradesTo: UnitType.Cavalry,
            isUniqueUnit: true, replacesUnit: UnitType.Knight, civId: "arabia",
            description: "Arabian desert raider (3 Mov, requires no iron)."));

        Register(new(UnitType.WarElephant, "War Elephant", TechEra.MiddleAges, 4, 3, 2f, 70,
            requiredTechId: "chivalry", upgradesTo: UnitType.Cavalry,
            isUniqueUnit: true, replacesUnit: UnitType.Knight, civId: "india",
            description: "Indian war elephant (+1 HP, requires neither horses nor iron)."));

        Register(new(UnitType.Keshik, "Keshik", TechEra.MiddleAges, 4, 2, 2f, 60,
            requiredTechId: "chivalry", requiredResourceIds: new[] { "horses" }, upgradesTo: UnitType.Cavalry,
            isUniqueUnit: true, replacesUnit: UnitType.Knight, civId: "mongols",
            description: "Mongol horse archer (costs less, requires no iron)."));

        Register(new(UnitType.Berserk, "Berserk", TechEra.MiddleAges, 6, 2, 1f, 70,
            requiredTechId: "invention", upgradesTo: UnitType.Guerrilla,
            isUniqueUnit: true, replacesUnit: UnitType.MedievalInfantry, civId: "vikings",
            description: "Fierce Viking raider with brutal offensive power (6 Atk)."));

        Register(new(UnitType.SwissMercenary, "Swiss Mercenary", TechEra.MiddleAges, 1, 4, 1f, 30,
            requiredTechId: "feudalism", upgradesTo: UnitType.Musketman,
            isUniqueUnit: true, replacesUnit: UnitType.Pikeman, civId: "dutch",
            description: "Swiss pikeman with legendary defensive fortitude (+1 Def, requires no iron)."));

        Register(new(UnitType.Musketeer, "Musketeer", TechEra.MiddleAges, 3, 4, 1f, 60,
            requiredTechId: "gunpowder", requiredResourceIds: new[] { "saltpeter" }, upgradesTo: UnitType.Rifleman,
            isUniqueUnit: true, replacesUnit: UnitType.Musketman, civId: "france",
            description: "French royal musketeer with enhanced attack (+1 Atk)."));

        Register(new(UnitType.Hwacha, "Hwacha", TechEra.MiddleAges, 0, 0, 1f, 40,
            requiredTechId: "engineering", bombardStrength: 8, bombardRange: 1, upgradesTo: UnitType.Cannon,
            isUniqueUnit: true, replacesUnit: UnitType.Trebuchet, civId: "korea",
            description: "Korean rocket cart with formidable bombardment (8 Bombard)."));

        // Industrial & Modern UUs
        Register(new(UnitType.Cossack, "Cossack", TechEra.Industrial, 6, 4, 3f, 80,
            requiredTechId: "military_tradition", requiredResourceIds: new[] { "horses", "saltpeter" }, upgradesTo: UnitType.Tank,
            isUniqueUnit: true, replacesUnit: UnitType.Cavalry, civId: "russia",
            description: "Russian cavalry with fortified defense (+1 Def)."));

        Register(new(UnitType.Sipahi, "Sipahi", TechEra.Industrial, 8, 3, 3f, 100,
            requiredTechId: "military_tradition", requiredResourceIds: new[] { "horses", "saltpeter" }, upgradesTo: UnitType.Tank,
            isUniqueUnit: true, replacesUnit: UnitType.Cavalry, civId: "ottomans",
            description: "Ottoman heavy cavalry with devastating charge (8 Atk)."));

        Register(new(UnitType.Panzer, "Panzer", TechEra.Industrial, 16, 8, 3f, 100,
            requiredTechId: "motorized_transportation", requiredResourceIds: new[] { "oil", "rubber" }, upgradesTo: UnitType.ModernArmor,
            isUniqueUnit: true, replacesUnit: UnitType.Tank, civId: "germany",
            description: "German blitzkrieg tank with superior mobility (3 Mov)."));

        // ═══════════════════════════════════════════════════════
        // NAVAL UNITS (Sea Domain)
        // ═══════════════════════════════════════════════════════
        Register(new(UnitType.Curragh, "Curragh", TechEra.Ancient, 1, 1, 2f, 15,
            requiredTechId: "alphabet", domain: UnitDomain.Sea, sinksInSeaOcean: true,
            upgradesTo: UnitType.Galley, description: "Early scouting vessel. Risk of sinking in sea and ocean."));

        Register(new(UnitType.Galley, "Galley", TechEra.Ancient, 1, 1, 3f, 30,
            requiredTechId: "map_making", domain: UnitDomain.Sea, transportCapacity: 2, sinksInSeaOcean: true,
            upgradesTo: UnitType.Caravel, description: "Early coastal transport galley. Carries 2 land units."));

        Register(new(UnitType.Dromon, "Dromon", TechEra.Ancient, 2, 1, 3f, 30,
            requiredTechId: "map_making", domain: UnitDomain.Sea, bombardStrength: 2, bombardRange: 1, sinksInSeaOcean: true,
            isUniqueUnit: true, replacesUnit: UnitType.Galley, civId: "byzantines", upgradesTo: UnitType.Caravel,
            description: "Byzantine warship armed with Greek Fire (+1 Atk, Bombard 2)."));

        Register(new(UnitType.Caravel, "Caravel", TechEra.MiddleAges, 1, 2, 4f, 40,
            requiredTechId: "astronomy", domain: UnitDomain.Sea, transportCapacity: 3,
            upgradesTo: UnitType.Galleon, description: "Seaworthy exploration ship. Safe in sea, carries 3 units."));

        Register(new(UnitType.Carrack, "Carrack", TechEra.MiddleAges, 2, 2, 4f, 40,
            requiredTechId: "astronomy", domain: UnitDomain.Sea, transportCapacity: 3,
            isUniqueUnit: true, replacesUnit: UnitType.Caravel, civId: "portugal", upgradesTo: UnitType.Galleon,
            description: "Portuguese oceanic carrack (+1 Atk, safe in deep ocean)."));

        Register(new(UnitType.Galleon, "Galleon", TechEra.MiddleAges, 1, 2, 4f, 60,
            requiredTechId: "navigation", domain: UnitDomain.Sea, transportCapacity: 4,
            upgradesTo: UnitType.Transport, description: "Oceanic transport vessel. Carries 4 units safely across all waters."));

        Register(new(UnitType.Frigate, "Frigate", TechEra.MiddleAges, 2, 2, 5f, 60,
            requiredTechId: "magnetism", requiredResourceIds: new[] { "iron", "saltpeter" }, domain: UnitDomain.Sea,
            bombardStrength: 3, bombardRange: 1, upgradesTo: UnitType.Ironclad,
            description: "Square-rigged naval warship with coastal cannon bombardment."));

        Register(new(UnitType.ManOWar, "Man-O-War", TechEra.MiddleAges, 3, 2, 5f, 60,
            requiredTechId: "magnetism", domain: UnitDomain.Sea, bombardStrength: 4, bombardRange: 1,
            isUniqueUnit: true, replacesUnit: UnitType.Frigate, civId: "england", upgradesTo: UnitType.Ironclad,
            description: "British royal warship with heavier cannons (+1 Atk, Bombard 4)."));

        Register(new(UnitType.Privateer, "Privateer", TechEra.MiddleAges, 2, 1, 4f, 50,
            requiredTechId: "magnetism", requiredResourceIds: new[] { "iron", "saltpeter" }, domain: UnitDomain.Sea,
            hasHiddenNationality: true,
            description: "Corsair vessel with hidden nationality. Can attack without triggering war."));

        Register(new(UnitType.Ironclad, "Ironclad", TechEra.Industrial, 4, 4, 4f, 80,
            requiredTechId: "ironclads", requiredResourceIds: new[] { "iron", "coal" }, domain: UnitDomain.Sea,
            bombardStrength: 6, bombardRange: 1, upgradesTo: UnitType.Destroyer,
            description: "Steam-powered armored warship with iron hull and heavy cannons."));

        Register(new(UnitType.Transport, "Transport", TechEra.Industrial, 1, 2, 5f, 100,
            requiredTechId: "combustion", requiredResourceIds: new[] { "oil" }, domain: UnitDomain.Sea,
            transportCapacity: 8,
            description: "Industrial troop ship capable of transporting 8 land units across the world."));

        Register(new(UnitType.Destroyer, "Destroyer", TechEra.Industrial, 12, 8, 6f, 120,
            requiredTechId: "combustion", requiredResourceIds: new[] { "oil" }, domain: UnitDomain.Sea,
            bombardStrength: 6, bombardRange: 1, detectsStealth: true, upgradesTo: UnitType.AegisCruiser,
            description: "Fast escort warship equipped with sonar to detect submerged submarines."));

        Register(new(UnitType.Cruiser, "Cruiser", TechEra.Industrial, 15, 10, 5f, 160,
            requiredTechId: "combustion", requiredResourceIds: new[] { "oil" }, domain: UnitDomain.Sea,
            bombardStrength: 8, bombardRange: 1,
            description: "Heavy cruiser armed with rapid-firing coastal batteries."));

        Register(new(UnitType.Battleship, "Battleship", TechEra.Industrial, 18, 12, 5f, 200,
            requiredTechId: "mass_production", requiredResourceIds: new[] { "oil" }, domain: UnitDomain.Sea,
            bombardStrength: 12, bombardRange: 2,
            description: "Apex naval dreadnought with devastating 2-tile bombard artillery."));

        Register(new(UnitType.Submarine, "Submarine", TechEra.Industrial, 8, 4, 3f, 100,
            requiredTechId: "combustion", requiredResourceIds: new[] { "oil" }, domain: UnitDomain.Sea,
            isStealth: true, upgradesTo: UnitType.NuclearSubmarine,
            description: "Submerged stealth predator. Invisible to enemies unless adjacent or spotted by Destroyers."));

        Register(new(UnitType.Carrier, "Carrier", TechEra.Industrial, 1, 8, 5f, 180,
            requiredTechId: "motorized_transportation", requiredResourceIds: new[] { "oil" }, domain: UnitDomain.Sea,
            transportCapacity: 4,
            description: "Floating flight deck capable of basing and transporting up to 4 aircraft."));

        Register(new(UnitType.NuclearSubmarine, "Nuclear Submarine", TechEra.Modern, 8, 4, 4f, 140,
            requiredTechId: "fission", requiredResourceIds: new[] { "uranium" }, domain: UnitDomain.Sea,
            isStealth: true, transportCapacity: 1,
            description: "Nuclear-powered stealth submarine capable of carrying a tactical nuke."));

        Register(new(UnitType.AegisCruiser, "AEGIS Cruiser", TechEra.Modern, 14, 10, 5f, 160,
            requiredTechId: "rocketry", requiredResourceIds: new[] { "aluminum", "oil" }, domain: UnitDomain.Sea,
            bombardStrength: 8, bombardRange: 2, detectsStealth: true,
            description: "Advanced guided missile cruiser with sonar and anti-air radar defense."));

        // ═══════════════════════════════════════════════════════
        // AIR UNITS (Air Domain)
        // ═══════════════════════════════════════════════════════
        Register(new(UnitType.Fighter, "Fighter", TechEra.Industrial, 4, 2, 1f, 80,
            requiredTechId: "flight", requiredResourceIds: new[] { "oil" }, domain: UnitDomain.Air,
            operationalRange: 4, bombardStrength: 2, upgradesTo: UnitType.JetFighter,
            description: "Propeller fighter aircraft capable of air patrol, reconnaissance, and bombing runs."));

        Register(new(UnitType.Bomber, "Bomber", TechEra.Industrial, 0, 2, 1f, 100,
            requiredTechId: "flight", requiredResourceIds: new[] { "oil" }, domain: UnitDomain.Air,
            operationalRange: 6, bombardStrength: 8, upgradesTo: UnitType.StealthBomber,
            description: "Strategic bomber delivering heavy ordnance onto enemy tiles, cities, and units."));

        Register(new(UnitType.JetFighter, "Jet Fighter", TechEra.Modern, 8, 4, 1f, 100,
            requiredTechId: "rocketry", requiredResourceIds: new[] { "aluminum", "oil" }, domain: UnitDomain.Air,
            operationalRange: 6, bombardStrength: 4, upgradesTo: UnitType.StealthFighter,
            description: "Supersonic jet interceptor with high air-to-air lethality."));

        Register(new(UnitType.F15, "F-15", TechEra.Modern, 8, 4, 1f, 100,
            requiredTechId: "rocketry", requiredResourceIds: new[] { "oil" }, domain: UnitDomain.Air,
            operationalRange: 6, bombardStrength: 6, isUniqueUnit: true, replacesUnit: UnitType.JetFighter, civId: "america",
            description: "American strike fighter with upgraded precision bombing (+2 Bombard)."));

        Register(new(UnitType.StealthFighter, "Stealth Fighter", TechEra.Modern, 8, 6, 1f, 120,
            requiredTechId: "stealth", requiredResourceIds: new[] { "aluminum", "oil" }, domain: UnitDomain.Air,
            operationalRange: 8, bombardStrength: 6, isStealth: true,
            description: "Radar-evading stealth fighter immune to normal anti-air interception."));

        Register(new(UnitType.StealthBomber, "Stealth Bomber", TechEra.Modern, 0, 4, 1f, 140,
            requiredTechId: "stealth", requiredResourceIds: new[] { "aluminum", "oil" }, domain: UnitDomain.Air,
            operationalRange: 8, bombardStrength: 12, isStealth: true,
            description: "Advanced stealth strategic bomber with massive bombing damage."));

        Register(new(UnitType.Helicopter, "Helicopter", TechEra.Modern, 0, 2, 1f, 100,
            requiredTechId: "advanced_flight", requiredResourceIds: new[] { "oil" }, domain: UnitDomain.Air,
            operationalRange: 6, transportCapacity: 2,
            description: "Air transport helicopter carrying up to 2 foot units across all terrain."));

        Register(new(UnitType.CruiseMissile, "Cruise Missile", TechEra.Industrial, 0, 0, 1f, 60,
            requiredTechId: "rocketry", requiredResourceIds: new[] { "aluminum" }, domain: UnitDomain.Air,
            operationalRange: 3, bombardStrength: 16,
            description: "Single-use tactical cruise missile delivering precision high-explosive strike."));

        Register(new(UnitType.TacticalNuke, "Tactical Nuke", TechEra.Modern, 0, 0, 1f, 140,
            requiredTechId: "fission", requiredResourceIds: new[] { "uranium" }, domain: UnitDomain.Air,
            operationalRange: 4, bombardStrength: 30,
            description: "Tactical nuclear weapon causing cataclysmic area destruction and radiation fallout."));

        Register(new(UnitType.ICBM, "ICBM", TechEra.Modern, 0, 0, 1f, 500,
            requiredTechId: "space_flight", requiredResourceIds: new[] { "uranium", "aluminum" }, domain: UnitDomain.Air,
            operationalRange: 999, bombardStrength: 999,
            description: "Intercontinental Ballistic Missile. Infinite range, city-destroying power."));

        // ═══════════════════════════════════════════════════════
        // GREAT LEADERS & ARMIES
        // ═══════════════════════════════════════════════════════
        Register(new(UnitType.GreatLeader, "Great Leader", TechEra.Ancient, 0, 0, 3f, 0,
            description: "A great military leader spawned from elite combat. Can hurry production or form an Army."));
            
        Register(new(UnitType.Army, "Army", TechEra.Ancient, 0, 0, 1f, 0,
            transportCapacity: 3,
            description: "A coalition of military units. Fights as a single massive force with combined health and multiple attacks."));
    }

    private static void Register(UnitDefinition def)
    {
        _definitions[def.Type] = def;
    }

    public static UnitDefinition Get(UnitType type)
    {
        if (_definitions.TryGetValue(type, out var def))
            return def;

        // Fallback default
        return new UnitDefinition(type, type.ToString(), TechEra.Ancient, 1, 1, 1f, 10);
    }

    public static IReadOnlyCollection<UnitDefinition> GetAll() => _definitions.Values;

    public static int GetCost(UnitType type) => Get(type).ShieldCost;

    public static int GetCost(ProductionProject project)
    {
        if (TryMapProjectToUnitType(project, out var unitType))
        {
            return GetCost(unitType);
        }
        return 10;
    }

    public static bool TryMapProjectToUnitType(ProductionProject project, out UnitType unitType)
    {
        if (Enum.TryParse<UnitType>(project.ToString(), out var parsed))
        {
            unitType = parsed;
            return true;
        }

        unitType = UnitType.Warrior;
        return false;
    }

    public static ProductionProject MapUnitTypeToProject(UnitType type)
    {
        if (Enum.TryParse<ProductionProject>(type.ToString(), out var project))
        {
            return project;
        }
        return ProductionProject.None;
    }

    /// <summary>
    /// Returns the effective UnitType for a given civilization, replacing base unit types with UU if applicable.
    /// </summary>
    public static UnitType GetCivUnitType(UnitType baseType, string? civId)
    {
        if (string.IsNullOrEmpty(civId)) return baseType;

        var uu = _definitions.Values.FirstOrDefault(d => d.IsUniqueUnit && d.ReplacesUnit == baseType && d.CivId == civId);
        return uu != null ? uu.Type : baseType;
    }
}
