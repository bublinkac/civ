using System.Collections.Generic;

namespace CivGame.Core;

public enum CultureGroup { American, European, Mediterranean, MidEastern, Asian }
public enum CivTrait { Militaristic, Scientific, Industrious, Agricultural, Expansionist, Commercial, Religious, Seafaring }

public class Civilization
{
    public string Id { get; }
    public string Name { get; }
    public string LeaderName { get; }
    public CultureGroup Culture { get; }
    public CivTrait Trait1 { get; }
    public CivTrait Trait2 { get; }
    public string[] StartingTechIds { get; }
    public string UniqueUnitName { get; }
    public UnitType ReplacedUnitType { get; }
    public string UniqueUnitDescription { get; }
    public int UniqueUnitAttackBonus { get; }
    public int UniqueUnitDefenseBonus { get; }
    public int UniqueUnitMovementBonus { get; }

    public Civilization(
        string id, string name, string leaderName, CultureGroup culture,
        CivTrait t1, CivTrait t2, string[] startingTechs,
        string uuName, UnitType replacedType, string uuDesc,
        int atkBonus = 0, int defBonus = 0, int movBonus = 0)
    {
        Id = id;
        Name = name;
        LeaderName = leaderName;
        Culture = culture;
        Trait1 = t1;
        Trait2 = t2;
        StartingTechIds = startingTechs;
        UniqueUnitName = uuName;
        ReplacedUnitType = replacedType;
        UniqueUnitDescription = uuDesc;
        UniqueUnitAttackBonus = atkBonus;
        UniqueUnitDefenseBonus = defBonus;
        UniqueUnitMovementBonus = movBonus;
    }
}

/// <summary>
/// Civ3 Golden Age:
/// - Duration: 20 turns
/// - Effect: Every worked tile producing ≥1 shield gets +1 shield; every tile producing ≥1 commerce gets +1 commerce
/// - Can only happen ONCE per civilization per game
/// - Triggers: (1) Win combat with Unique Unit for first time, OR (2) Build a Wonder matching both civ traits
/// </summary>
public class GoldenAge
{
    public bool IsActive { get; set; } = false;
    public int TurnsRemaining { get; set; } = 0;
    /// <summary>Once triggered, cannot trigger again.</summary>
    public bool HasBeenUsed { get; set; } = false;
    public const int Duration = 20;

    /// <summary>
    /// Attempt to trigger a Golden Age. Returns true if successfully triggered.
    /// Will fail if already used or currently active.
    /// </summary>
    public bool Trigger(string factionName, string reason)
    {
        if (HasBeenUsed || IsActive) return false;
        IsActive = true;
        HasBeenUsed = true;
        TurnsRemaining = Duration;
        System.Console.WriteLine($"[GOLDEN AGE] {factionName} enters a Golden Age! ({reason}) +1 shield and +1 commerce on all productive tiles for {Duration} turns!");
        return true;
    }

    public void ProcessTurn()
    {
        if (!IsActive) return;
        TurnsRemaining--;
        if (TurnsRemaining <= 0)
        {
            IsActive = false;
            System.Console.WriteLine("[GOLDEN AGE] The Golden Age has ended. Your civilization returns to normal productivity.");
        }
    }
}