using System;
using System.Collections.Generic;

namespace CivGame.Core.Diplomacy;

/// <summary>
/// Represents a diplomatic trade agreement between two factions.
/// Can include both instantaneous items (Techs, Lump Sum Gold, Maps)
/// and recurring/active items (Gold Per Turn, Resources, Treaties) lasting for X turns.
/// </summary>
public class TradeDeal
{
    public Faction FactionA { get; set; } = Faction.Player;
    public Faction FactionB { get; set; } = Faction.AiRival;
    public int TurnsRemaining { get; set; } = 20; // Classic Civ3 deal length

    // Items given by Faction A to Faction B
    public int GoldPerTurnA { get; set; }
    public int LumpSumGoldA { get; set; }
    public List<string> TechsA { get; set; } = new();
    public List<string> ResourcesA { get; set; } = new();
    public bool WorldMapA { get; set; }

    // Items given by Faction B to Faction A
    public int GoldPerTurnB { get; set; }
    public int LumpSumGoldB { get; set; }
    public List<string> TechsB { get; set; } = new();
    public List<string> ResourcesB { get; set; } = new();
    public bool WorldMapB { get; set; }

    // Treaties & Agreements
    public bool RightOfPassage { get; set; }
    public bool MutualProtectionPact { get; set; }
    public bool MilitaryAlliance { get; set; }
    public bool PeaceTreaty { get; set; }
    
    public bool HasRecurringElements => 
        GoldPerTurnA > 0 || GoldPerTurnB > 0 ||
        ResourcesA.Count > 0 || ResourcesB.Count > 0 ||
        RightOfPassage || MutualProtectionPact || MilitaryAlliance || PeaceTreaty;
}
