namespace CivGame.Core;

/// <summary>
/// Civilization III government types with authentic properties.
/// </summary>
public enum GovernmentType
{
    Despotism,
    Monarchy,
    Republic,
    Democracy,
    Communism,
    Feudalism,
    Fascism
}

/// <summary>
/// Civ3 hurry production methods.
/// </summary>
public enum HurryMethod
{
    None,
    ForcedLabor,   // Sacrifice population to rush production (1 pop = 20 shields)
    PayCitizens    // Pay gold to rush production (4 gold = 1 shield)
}

/// <summary>
/// Civ3 corruption severity levels (affects distance-based corruption formula).
/// </summary>
public enum CorruptionLevel
{
    Minimal,        // Democracy: lowest corruption, +10% OCN, -25% distance
    Nuisance,       // Republic, Fascism: low corruption, +10% OCN
    Problematic,    // Monarchy, Feudalism: moderate corruption
    Communal,       // Communism: flat rate everywhere (no distance factor)
    Rampant,        // Despotism: highest corruption, +50% distance penalty
    Catastrophic    // Anarchy: total corruption
}

/// <summary>
/// Defines properties for each government type matching Civ3 Complete v1.22:
/// - Unit support per Town (pop 1-6), City (pop 7-12), Metropolis (pop 13+)
/// - Gold cost per excess unit
/// - Worker efficiency (50%-200%)
/// - Military police limit
/// - War weariness susceptibility (None/Low/High)
/// - Corruption level
/// - Draft rate (citizens per turn)
/// - Hurry method (None/Forced Labor/Pay Citizens)
/// - Tile penalty (Despotism penalty: -1 on tiles producing 3+)
/// - Commerce bonus (+1 commerce on productive tiles for Republic/Democracy)
/// </summary>
public class Government
{
    public GovernmentType Type { get; }
    public string Name { get; }

    // ═══════ Unit Support (Civ3: depends on city size) ═══════
    /// <summary>Free units supported per Town (pop 1-6).</summary>
    public int UnitSupportPerTown { get; }
    /// <summary>Free units supported per City (pop 7-12).</summary>
    public int UnitSupportPerCity { get; }
    /// <summary>Free units supported per Metropolis (pop 13+).</summary>
    public int UnitSupportPerMetropolis { get; }
    /// <summary>Gold cost per unit beyond free support.</summary>
    public int UnitSupportCost { get; }

    // ═══════ Worker & Production ═══════
    /// <summary>Worker efficiency percentage (50=half speed, 100=normal, 150=1.5x, 200=2x).</summary>
    public int WorkerEfficiency { get; }
    /// <summary>Hurry production method.</summary>
    public HurryMethod Hurry { get; }

    // ═══════ Happiness & Police ═══════
    /// <summary>Maximum military police units that reduce unhappiness (0 = none allowed).</summary>
    public int MaxMilitaryPolice { get; }
    /// <summary>Whether this government suffers war weariness.</summary>
    public bool HasWarWeariness { get; }
    /// <summary>War weariness severity: 0=immune, 1=Low (Republic/Feudalism), 2=High (Democracy).</summary>
    public int WarWearinessSeverity { get; }
    /// <summary>Max citizens that can be drafted per city per turn.</summary>
    public int DraftRate { get; }

    // ═══════ Economy & Corruption ═══════
    /// <summary>Corruption level category.</summary>
    public CorruptionLevel Corruption { get; }
    /// <summary>Corruption modifier for distance formula (lower = less corrupt). 1.0 = baseline.</summary>
    public float CorruptionModifier { get; }
    /// <summary>Whether tiles producing ≥1 commerce get +1 commerce bonus (Republic/Democracy).</summary>
    public bool HasCommerceBonus { get; }

    // ═══════ Penalties & Bonuses ═══════
    /// <summary>Tile penalty: under Despotism/Feudalism, any tile producing 3+ of a yield loses 1.</summary>
    public bool HasTilePenalty { get; }

    // ═══════ Unlock & Transition ═══════
    /// <summary>Required technology to unlock this government (null = available from start).</summary>
    public string? RequiredTechId { get; }
    /// <summary>Whether switching to this government causes anarchy.</summary>
    public bool CausesAnarchy { get; }
    /// <summary>Anarchy duration range (min turns).</summary>
    public int AnarchyMinTurns { get; }
    /// <summary>Anarchy duration range (max turns).</summary>
    public int AnarchyMaxTurns { get; }

    // ═══════ Backward-compatible properties ═══════
    /// <summary>Average free units per city (uses City-size value for backward compat).</summary>
    public int FreeUnitsPerCity => UnitSupportPerCity;

    private Government(GovernmentType type, string name,
        int supportTown, int supportCity, int supportMetro, int unitCost,
        int workerEff, HurryMethod hurry,
        int maxMP, bool hasWW, int wwSeverity, int draftRate,
        CorruptionLevel corruption, float corruptionMod, bool commerceBonus,
        bool tilePenalty, string? techId, bool anarchy, int anarchyMin, int anarchyMax)
    {
        Type = type;
        Name = name;
        UnitSupportPerTown = supportTown;
        UnitSupportPerCity = supportCity;
        UnitSupportPerMetropolis = supportMetro;
        UnitSupportCost = unitCost;
        WorkerEfficiency = workerEff;
        Hurry = hurry;
        MaxMilitaryPolice = maxMP;
        HasWarWeariness = hasWW;
        WarWearinessSeverity = wwSeverity;
        DraftRate = draftRate;
        Corruption = corruption;
        CorruptionModifier = corruptionMod;
        HasCommerceBonus = commerceBonus;
        HasTilePenalty = tilePenalty;
        RequiredTechId = techId;
        CausesAnarchy = anarchy;
        AnarchyMinTurns = anarchyMin;
        AnarchyMaxTurns = anarchyMax;
    }

    /// <summary>
    /// Get the number of free supported units for a city based on its population.
    /// Town: pop 1-6, City: pop 7-12, Metropolis: pop 13+
    /// </summary>
    public int GetUnitSupport(int population)
    {
        if (population >= 13) return UnitSupportPerMetropolis;
        if (population >= 7) return UnitSupportPerCity;
        return UnitSupportPerTown;
    }

    /// <summary>
    /// Get government definition by type. Values from Civ3 Complete v1.22.
    /// Sources: CivFanatics, GameFAQs government guides.
    /// </summary>
    public static Government Get(GovernmentType type) => type switch
    {
        // Despotism: Starting government. Rampant corruption, tile penalty, weak but stable.
        // Worker Eff: 50%, Hurry: None, Corruption: Rampant, WW: None
        // Draft: 0, MP: 2, Support Town/City/Metro: 4/4/4, Cost: 1gpt
        GovernmentType.Despotism => new(
            GovernmentType.Despotism, "Despotism",
            supportTown: 4, supportCity: 4, supportMetro: 4, unitCost: 1,
            workerEff: 50, hurry: HurryMethod.None,
            maxMP: 2, hasWW: false, wwSeverity: 0, draftRate: 0,
            corruption: CorruptionLevel.Rampant, corruptionMod: 1.5f, commerceBonus: false,
            tilePenalty: true, techId: null, anarchy: false, anarchyMin: 0, anarchyMax: 0),

        // Monarchy: Good early-game. No WW, decent MP, balanced support.
        // Worker Eff: 100%, Hurry: Pay Citizens, Corruption: Problematic, WW: None
        // Draft: 2, MP: 3, Support Town/City/Metro: 2/4/8, Cost: 1gpt
        GovernmentType.Monarchy => new(
            GovernmentType.Monarchy, "Monarchy",
            supportTown: 2, supportCity: 4, supportMetro: 8, unitCost: 1,
            workerEff: 100, hurry: HurryMethod.PayCitizens,
            maxMP: 3, hasWW: false, wwSeverity: 0, draftRate: 2,
            corruption: CorruptionLevel.Problematic, corruptionMod: 1.0f, commerceBonus: false,
            tilePenalty: false, techId: "monarchy", anarchy: true, anarchyMin: 2, anarchyMax: 6),

        // Republic: Commerce bonus, low WW, expensive units. Great for peacetime economy.
        // Worker Eff: 100%, Hurry: Pay Citizens, Corruption: Nuisance, WW: Low
        // Draft: 1, MP: 0, Support Town/City/Metro: 1/3/4, Cost: 2gpt
        GovernmentType.Republic => new(
            GovernmentType.Republic, "Republic",
            supportTown: 1, supportCity: 3, supportMetro: 4, unitCost: 2,
            workerEff: 100, hurry: HurryMethod.PayCitizens,
            maxMP: 0, hasWW: true, wwSeverity: 1, draftRate: 1,
            corruption: CorruptionLevel.Nuisance, corruptionMod: 0.7f, commerceBonus: true,
            tilePenalty: false, techId: "republic", anarchy: true, anarchyMin: 2, anarchyMax: 6),

        // Democracy: Best economy. Minimal corruption, commerce bonus, but severe WW.
        // Worker Eff: 150%, Hurry: Pay Citizens, Corruption: Minimal, WW: High
        // Draft: 1, MP: 0, Support Town/City/Metro: 0/0/0, Cost: 1gpt (all units cost)
        GovernmentType.Democracy => new(
            GovernmentType.Democracy, "Democracy",
            supportTown: 0, supportCity: 0, supportMetro: 0, unitCost: 1,
            workerEff: 150, hurry: HurryMethod.PayCitizens,
            maxMP: 0, hasWW: true, wwSeverity: 2, draftRate: 1,
            corruption: CorruptionLevel.Minimal, corruptionMod: 0.4f, commerceBonus: true,
            tilePenalty: false, techId: "democracy", anarchy: true, anarchyMin: 3, anarchyMax: 8),

        // Communism: Flat corruption everywhere. Great for huge empires at war.
        // Worker Eff: 100%, Hurry: Forced Labor, Corruption: Communal, WW: None
        // Draft: 2, MP: 4, Support Town/City/Metro: 6/6/6, Cost: 1gpt
        GovernmentType.Communism => new(
            GovernmentType.Communism, "Communism",
            supportTown: 6, supportCity: 6, supportMetro: 6, unitCost: 1,
            workerEff: 100, hurry: HurryMethod.ForcedLabor,
            maxMP: 4, hasWW: false, wwSeverity: 0, draftRate: 2,
            corruption: CorruptionLevel.Communal, corruptionMod: 0.8f, commerceBonus: false,
            tilePenalty: false, techId: "communism", anarchy: true, anarchyMin: 2, anarchyMax: 6),

        // Feudalism (Conquests): Good for small warring towns. Expensive excess units.
        // Worker Eff: 100%, Hurry: Forced Labor, Corruption: Problematic, WW: Low
        // Draft: 2, MP: 3, Support Town/City/Metro: 5/2/1, Cost: 3gpt (!)
        GovernmentType.Feudalism => new(
            GovernmentType.Feudalism, "Feudalism",
            supportTown: 5, supportCity: 2, supportMetro: 1, unitCost: 3,
            workerEff: 100, hurry: HurryMethod.ForcedLabor,
            maxMP: 3, hasWW: true, wwSeverity: 1, draftRate: 2,
            corruption: CorruptionLevel.Problematic, corruptionMod: 1.2f, commerceBonus: false,
            tilePenalty: true, techId: "feudalism", anarchy: true, anarchyMin: 2, anarchyMax: 4),

        // Fascism (Conquests): Military powerhouse. High support, mass drafting, fast workers.
        // Worker Eff: 200%, Hurry: Forced Labor, Corruption: Nuisance, WW: None
        // Draft: 4, MP: 4, Support Town/City/Metro: 4/7/10, Cost: 1gpt
        GovernmentType.Fascism => new(
            GovernmentType.Fascism, "Fascism",
            supportTown: 4, supportCity: 7, supportMetro: 10, unitCost: 1,
            workerEff: 200, hurry: HurryMethod.ForcedLabor,
            maxMP: 4, hasWW: false, wwSeverity: 0, draftRate: 4,
            corruption: CorruptionLevel.Nuisance, corruptionMod: 0.7f, commerceBonus: false,
            tilePenalty: false, techId: "fascism", anarchy: true, anarchyMin: 2, anarchyMax: 4),

        _ => Get(GovernmentType.Despotism)
    };

    /// <summary>All available government types in unlock order.</summary>
    public static readonly GovernmentType[] All =
    {
        GovernmentType.Despotism,
        GovernmentType.Monarchy,
        GovernmentType.Feudalism,
        GovernmentType.Republic,
        GovernmentType.Democracy,
        GovernmentType.Communism,
        GovernmentType.Fascism
    };
}
