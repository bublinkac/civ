using System;

namespace CivGame.Core;

public enum UnitType
{
    Explorer,
    Settler,
    Warrior,
    Archer,
    Barbarian,
    Worker
}

public class Unit
{
    public string Id { get; }
    public UnitType Type { get; }
    public int X { get; set; }
    public int Y { get; set; }
    public int MaxMovement { get; }
    public int RemainingMovement { get; set; }
    public int VisionRange { get; }

    // Combat Stats
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int AttackStrength { get; }
    public int DefenseStrength { get; }
    public Faction Faction { get; set; }
    public string? CivilizationId { get; set; }
    public Civilization? Civilization => CivilizationId != null ? CivilizationRegistry.Get(CivilizationId) : null;
    public bool IsBarbarian => Faction == Faction.Barbarian;

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

        switch (type)
        {
            case UnitType.Explorer:
                MaxMovement = 2;
                VisionRange = 2;
                AttackStrength = 1;
                DefenseStrength = 1;
                break;
            case UnitType.Settler:
                MaxMovement = 1;
                VisionRange = 2;
                AttackStrength = 0;
                DefenseStrength = 1;
                break;
            case UnitType.Warrior:
                MaxMovement = 1;
                VisionRange = 1;
                AttackStrength = 2;
                DefenseStrength = 2;
                break;
            case UnitType.Archer:
                MaxMovement = 1;
                VisionRange = 2;
                AttackStrength = 3;
                DefenseStrength = 1;
                break;
            case UnitType.Barbarian:
                MaxMovement = 1;
                VisionRange = 1;
                AttackStrength = 2;
                DefenseStrength = 1;
                break;
            case UnitType.Worker:
                MaxMovement = 1;
                VisionRange = 1;
                AttackStrength = 0;
                DefenseStrength = 1;
                break;
            default:
                MaxMovement = 1;
                VisionRange = 1;
                AttackStrength = 1;
                DefenseStrength = 1;
                break;
        }

        RemainingMovement = MaxMovement;
    }

    // Status properties
    public bool IsFortified { get; set; } = false;
    public bool IsSleeping { get; set; } = false;

    public bool HasMovementRemaining() => RemainingMovement > 0 && !IsWorkerBuilding() && !IsSleeping;

    public void MoveTo(int targetX, int targetY, int cost)
    {
        if (IsWorkerBuilding()) return; // Cannot move while building!

        X = targetX;
        Y = targetY;
        
        IsFortified = false; // Moving breaks fortification
        IsSleeping = false;  // Moving wakes up unit
        
        // Civ rules: moving consumes movement points. 
        // Entering a tile always consumes at least its movement cost,
        // but a unit can move onto a high-cost tile (like a mountain) even with 1 MP left, reducing it to 0.
        RemainingMovement = Math.Max(0, RemainingMovement - cost);
    }

    public void ResetMovement()
    {
        if (IsWorkerBuilding())
        {
            RemainingMovement = 0; // Keep movement locked to 0 while building
        }
        else if (IsSleeping)
        {
            RemainingMovement = 0; // Sleeping units do not get movement refreshed
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
        ConstructionTurnsRemaining = imp.ConstructionTurns;
        RemainingMovement = 0; // Consumes movement for the starting turn
    }

    public void CancelImprovement()
    {
        ImprovementUnderConstruction = null;
        ConstructionTurnsRemaining = 0;
    }
}
