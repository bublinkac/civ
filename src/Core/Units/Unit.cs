using System;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.Core;

public enum UnitDomain
{
    Land,
    Sea,
    Air
}

public enum AirMission
{
    None,
    AirSuperiority,
    Rebase,
    Bombard,
    Recon
}

public enum ExperienceLevel
{
    Conscript,
    Regular,
    Veteran,
    Elite
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
    ICBM,

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
    Panzer,

    // Great Leaders & Armies
    GreatLeader,
    Army
}

public class Unit
{
    public string Id { get; }
    public UnitType Type { get; private set; }
    public int X { get; set; }
    public int Y { get; set; }
    public float RemainingMovement { get; set; }
    public int VisionRange { get; private set; }

    // Combat Stats
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    
    private int _baseAttackStrength;
    private int _baseDefenseStrength;
    private float _baseMaxMovement;

    public int AttackStrength 
    { 
        get 
        {
            if (Type == UnitType.Army && LoadedUnitIds.Count > 0)
            {
                return 0; // Handled specially in GameSimulation, but we can return max:
            }
            return _baseAttackStrength;
        }
    }
    
    public int DefenseStrength 
    { 
        get 
        {
            if (Type == UnitType.Army && LoadedUnitIds.Count > 0) return 0; // Handled specially
            return _baseDefenseStrength;
        }
    }
    
    public float MovementBonus { get; set; } = 0f;

    public float MaxMovement 
    { 
        get 
        {
            if (Type == UnitType.Army && LoadedUnitIds.Count > 0) return 0; // Handled specially
            return _baseMaxMovement + MovementBonus;
        }
    }

    public int BombardStrength { get; private set; }
    public int BombardRange { get; private set; }
    public Faction Faction { get; set; }
    public string? CivilizationId { get; set; }
    public Civilization? Civilization => CivilizationId != null ? CivilizationRegistry.Get(CivilizationId) : null;
    public bool IsBarbarian => Faction == Faction.Barbarian;
    public ExperienceLevel Experience { get; set; } = ExperienceLevel.Regular;
    public bool HasGeneratedLeader { get; set; } = false;

    // Domain & Special Abilities
    public UnitDomain Domain { get; private set; }
    public AirMission CurrentAirMission { get; set; } = AirMission.None;
    public int TransportCapacity { get; private set; }
    public int OperationalRange { get; private set; }
    public bool SinksInSeaOcean { get; private set; }
    public bool IsStealth { get; private set; }
    public bool DetectsStealth { get; private set; }
    public bool HasHiddenNationality { get; private set; }

    // Transport & Cargo tracking
    public List<string> LoadedUnitIds { get; } = new();
    public string? TransportUnitId { get; set; }
    public bool IsLoadedInTransport => !string.IsNullOrEmpty(TransportUnitId);

    public bool IsAirUnit => Domain == UnitDomain.Air;
    public bool IsNavalUnit => Domain == UnitDomain.Sea;
    public bool IsLandUnit => Domain == UnitDomain.Land;

    // Advanced Commands (Phase 36)
    public bool IsAutomated { get; set; }

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
        _baseMaxMovement = def.Movement;
        VisionRange = def.VisionRange;
        _baseAttackStrength = def.AttackStrength;
        _baseDefenseStrength = def.DefenseStrength;
        BombardStrength = def.BombardStrength;
        BombardRange = def.BombardRange;
        Domain = def.Domain;
        TransportCapacity = def.TransportCapacity;
        OperationalRange = def.OperationalRange;
        SinksInSeaOcean = def.SinksInSeaOcean;
        IsStealth = def.IsStealth;
        DetectsStealth = def.DetectsStealth;
        HasHiddenNationality = def.HasHiddenNationality;

        // Initialize Health based on default Experience (Regular = 60 HP)
        // 1 HP = 20 Health in our engine (Conscript=40, Regular=60, Veteran=80, Elite=100)
        SetExperience(ExperienceLevel.Regular);

        RemainingMovement = MaxMovement;
    }

    public void SetExperience(ExperienceLevel exp)
    {
        Experience = exp;
        MaxHealth = exp switch
        {
            ExperienceLevel.Conscript => 40,
            ExperienceLevel.Regular => 60,
            ExperienceLevel.Veteran => 80,
            ExperienceLevel.Elite => 100,
            _ => 60
        };

        // Special HP bonus for War Elephant and Ancient Cavalry
        if (Type == UnitType.WarElephant || Type == UnitType.AncientCavalry)
        {
            MaxHealth += 25;
        }
        Health = MaxHealth;
    }

    public void SetType(UnitType type)
    {
        Type = type;
        var def = UnitDefinitionRegistry.Get(type);
        _baseMaxMovement = def.Movement;
        VisionRange = def.VisionRange;
        _baseAttackStrength = def.AttackStrength;
        _baseDefenseStrength = def.DefenseStrength;
        BombardStrength = def.BombardStrength;
        BombardRange = def.BombardRange;
        Domain = def.Domain;
        TransportCapacity = def.TransportCapacity;
        OperationalRange = def.OperationalRange;
        SinksInSeaOcean = def.SinksInSeaOcean;
        IsStealth = def.IsStealth;
        DetectsStealth = def.DetectsStealth;
        HasHiddenNationality = def.HasHiddenNationality;
    }

    // Status properties
    public bool IsFortified { get; set; } = false;
    public bool IsSentry { get; set; } = false;
    public bool IsSleeping { get; set; } = false;

    public bool HasMovementRemaining() => RemainingMovement > 0.0f && !IsWorkerBuilding() && !IsSleeping && !IsFortified && !IsSentry && !IsAutomated && !IsLoadedInTransport;

    public bool CanBoardTransport(Unit transport, GameSimulation? sim = null)
    {
        if (transport.Type != UnitType.Army && transport.Domain != UnitDomain.Sea && transport.Domain != UnitDomain.Air) return false;
        if (transport.Faction != Faction) return false;
        if (IsLoadedInTransport) return false;

        int capacity = transport.TransportCapacity;
        if (transport.Type == UnitType.Army && sim != null)
        {
            if (sim.Cities.Any(c => c.Faction == transport.Faction && c.Buildings.Any(b => b.Id == "pentagon")))
            {
                capacity = 4;
            }
        }
        
        if (transport.LoadedUnitIds.Count >= capacity) return false;
        
        // Army logic: only land combat units can join armies, and once they join they can never leave (but we won't strictly enforce 'never leave' here)
        if (transport.Type == UnitType.Army)
        {
            if (this.Domain != UnitDomain.Land) return false;
            // Usually workers/settlers can't join armies
            if (this.AttackStrength == 0 && this.DefenseStrength == 0) return false;
            return true;
        }

        // Aircraft can only board carriers
        if (IsAirUnit && transport.Type != UnitType.Carrier) return false;
        // Land units cannot board carriers
        if (IsLandUnit && transport.Type == UnitType.Carrier) return false;
        return true;
    }

    public void BoardTransport(Unit transport, GameSimulation? sim = null)
    {
        if (!CanBoardTransport(transport, sim)) return;
        TransportUnitId = transport.Id;
        transport.LoadedUnitIds.Add(Id);
        X = transport.X;
        Y = transport.Y;
        RemainingMovement = 0f;
        
        // Special logic for Armies: combine stats
        if (transport.Type == UnitType.Army)
        {
            transport._baseAttackStrength = Math.Max(transport._baseAttackStrength, this.AttackStrength);
            transport._baseDefenseStrength = Math.Max(transport._baseDefenseStrength, this.DefenseStrength);
            if (transport._baseMaxMovement == 1f) transport._baseMaxMovement = this.MaxMovement; // Take first unit's movement, simplified
            else transport._baseMaxMovement = Math.Min(transport._baseMaxMovement, this.MaxMovement);
            
            // In Civ3, Army health is sum of all units' health
            transport.MaxHealth += this.MaxHealth;
            transport.Health += this.Health;
        }
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
