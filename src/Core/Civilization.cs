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

public class GoldenAge
{
    public bool IsActive { get; set; } = false;
    public int TurnsRemaining { get; set; } = 0;
    public const int MaxDuration = 20;

    public void Trigger()
    {
        if (IsActive) return;
        IsActive = true;
        TurnsRemaining = MaxDuration;
        System.Console.WriteLine("[GOLDEN AGE] A Golden Age has begun! Production and commerce output are boosted across all tiles!");
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