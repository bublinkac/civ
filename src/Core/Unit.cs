using System;
using System.Collections.Generic;

namespace CivGame.Core;

public enum UnitDomain
{
    Land,
    Sea,
    Air
}

public enum UnitType
{
    // Civilian & Basic
    Explorer,
    Settler,
    Worker,
    Barbarian,

    // Ancient Era Land
    Warrior,
    Spearman,
    Archer,
    Swordsman,
    Chariot,
    Horseman,
    Catapult,

    // Middle Ages Land
    Pikeman,
    MedievalInfantry,
    Longbowman,
    Knight,
    Musketman,
    Trebuchet,
    Cannon,
    Crusader,
    AncientCavalry,

    // Industrial Era Land
    Rifleman,
    Infantry,
    Cavalry,
    Guerrilla,
    Artillery,
    Flak,
    Marine,
    Paratrooper,
    Tank,

    // Modern Era Land
    MechInfantry,
    ModernArmor,
    TOWInfantry,
    ModernParatrooper,
    MobileSAM,
    RadarArtillery,

    // Naval Units (Sea Domain)
    Curragh,
    Galley,
    Dromon,
    Caravel,
    Carrack,
    Galleon,
    Frigate,
    ManOWar,
    Privateer,
    Ironclad,
    Transport,
    Destroyer,
    Cruiser,
    Battleship,
    Submarine,
    Carrier,
    NuclearSubmarine,
    AegisCruiser,

    // Air Units (Air Domain)
    Fighter,
    Bomber,
    JetFighter,
    F15,
    StealthFighter,
    StealthBomber,
    Helicopter,
    CruiseMissile,
    TacticalNuke,

    // Unique Units (UU) - Land
    Legionary,
    Hoplite,
    Immortals,
    WarChariot,
    MountedWarrior,
    JaguarWarrior,
    EnkiduWarrior,
    Impi,
    NumidianMercenary,
    Bowman,
    JavelinThrower,
    ThreeManChariot,
    ChasquiScout,
    GallicSwordsman,
    Samurai,
    Rider,
    AnsarWarrior,
    WarElephant,
    Keshik,
    Berserk,
    SwissMercenary,
    Musketeer,
    Hwacha,
    Cossack,
    Sipahi,
    Panzer
}

public class Unit
{
    public string Id { get; }
    public UnitType Type { get; }
    public int X { get; set; }
    public int Y { get; set; }
    public float MaxMovement { get; }
    public float RemainingMovement { get; set; }
    public int VisionRange { get; }

    // Combat Stats
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int AttackStrength { get; }
    public int DefenseStrength { get; }
    public int BombardStrength { get; }
    public int BombardRange { get; }
    public Faction Faction { get; set; }
    public string? CivilizationId { get; set; }
    public Civilization? Civilization => CivilizationId != null ? CivilizationRegistry.Get(CivilizationId) : null;
    public bool IsBarbarian => Faction == Faction.Barbarian;

    // Domain & Special Abilities
    public UnitDomain Domain { get; }
    public int TransportCapacity { get; }
    public int OperationalRange { get; }
    public bool SinksInSeaOcean { get; }
    public bool IsStealth { get; }
    public bool DetectsStealth { get; }
    public bool HasHiddenNationality { get; }

    // Transport & Cargo tracking
    public List<string> LoadedUnitIds { get; } = new();
    public string? TransportUnitId { get; set; }
    public bool IsLoadedInTransport => !string.IsNullOrEmpty(TransportUnitId);

    public bool IsAirUnit => Domain == UnitDomain.Air;
    public bool IsNavalUnit => Domain == UnitDomain.Sea;
    public bool IsLandUnit => Domain == UnitDomain.Land;

    // Worker Construction properties
    public TileImprovement? ImprovementUnderConstruction { get; set; }
    public int ConstructionTurnsRemaining { get; set; }
    public bool IsWorkerBuilding() => ImprovementUnderConstruction != null;

    public Unit(string id, UnitType type, int x, int y, Faction faction = Faction.Player, string? civilizationId = null)
    {
        Id = id;
        Type = type;
        X = x;
        Y = y;
        Faction = faction;
        CivilizationId = civilizationId;
        if (type == UnitType.Barbarian)
        {
            Faction = Faction.Barbarian;
        }

        var def = UnitDefinitionRegistry.Get(type);
        MaxMovement = def.Movement;
        VisionRange = def.VisionRange;
        AttackStrength = def.AttackStrength;
        DefenseStrength = def.DefenseStrength;
        BombardStrength = def.BombardStrength;
        BombardRange = def.BombardRange;
        Domain = def.Domain;
        TransportCapacity = def.TransportCapacity;
        OperationalRange = def.OperationalRange;
        SinksInSeaOcean = def.SinksInSeaOcean;
        IsStealth = def.IsStealth;
        DetectsStealth = def.DetectsStealth;
        HasHiddenNationality = def.HasHiddenNationality;

        // Special HP bonus for War Elephant and Ancient Cavalry
        if (type == UnitType.WarElephant || type == UnitType.AncientCavalry)
        {
            MaxHealth = 125;
            Health = 125;
        }

        RemainingMovement = MaxMovement;
    }

    // Status properties
    public bool IsFortified { get; set; } = false;
    public bool IsSleeping { get; set; } = false;

    public bool HasMovementRemaining() => RemainingMovement > 0.0f && !IsWorkerBuilding() && !IsSleeping && !IsLoadedInTransport;

    public bool CanBoardTransport(Unit transport)
    {
        if (transport.Domain != UnitDomain.Sea && transport.Domain != UnitDomain.Air) return false;
        if (transport.Faction != Faction) return false;
        if (transport.LoadedUnitIds.Count >= transport.TransportCapacity) return false;
        if (IsLoadedInTransport) return false;
        // Aircraft can only board carriers
        if (IsAirUnit && transport.Type != UnitType.Carrier) return false;
        // Land units cannot board carriers
        if (IsLandUnit && transport.Type == UnitType.Carrier) return false;
        return true;
    }

    public void BoardTransport(Unit transport)
    {
        if (!CanBoardTransport(transport)) return;
        TransportUnitId = transport.Id;
        transport.LoadedUnitIds.Add(Id);
        X = transport.X;
        Y = transport.Y;
        RemainingMovement = 0f;
    }

    public void Disembark(int targetX, int targetY, Unit? transport = null)
    {
        transport?.LoadedUnitIds.Remove(Id);
        TransportUnitId = null;
        X = targetX;
        Y = targetY;
        RemainingMovement = 0f;
    }

    public void MoveTo(int targetX, int targetY, float cost)
    {
        if (IsWorkerBuilding()) return; // Cannot move while building!

        X = targetX;
        Y = targetY;
        
        IsFortified = false; // Moving breaks fortification
        IsSleeping = false;  // Moving wakes up unit
        
        // Civ rules: moving consumes movement points. 
        // Entering a tile always consumes at least its movement cost,
        // but a unit can move onto a high-cost tile (like a mountain) even with 1 MP left, reducing it to 0.
        RemainingMovement = Math.Max(0.0f, RemainingMovement - cost);
    }

    public void ResetMovement()
    {
        if (IsWorkerBuilding())
        {
            RemainingMovement = 0.0f; // Keep movement locked to 0 while building
        }
        else if (IsSleeping)
        {
            RemainingMovement = 0.0f; // Sleeping units do not get movement refreshed
        }
        else
        {
            RemainingMovement = MaxMovement;
        }
    }

    public void StartImprovement(TileImprovement imp)
    {
        if (Type != UnitType.Worker) return;

        ImprovementUnderConstruction = imp;

        int baseTurns = imp.ConstructionTurns;
        bool isIndustrious = Civilization != null && (Civilization.Trait1 == CivTrait.Industrious || Civilization.Trait2 == CivTrait.Industrious);
        if (isIndustrious)
        {
            // Industrious workers build improvements 50% faster (half the turns, rounded up)
            ConstructionTurnsRemaining = Math.Max(1, (int)Math.Ceiling(baseTurns / 2.0f));
        }
        else
        {
            ConstructionTurnsRemaining = baseTurns;
        }

        RemainingMovement = 0; // Consumes movement for the starting turn
    }

    public void CancelImprovement()
    {
        ImprovementUnderConstruction = null;
        ConstructionTurnsRemaining = 0;
    }
}
