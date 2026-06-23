namespace CivGame.Core;

/// <summary>
/// Authentic Civilization III difficulty levels, from easiest to hardest.
/// </summary>
public enum DifficultyLevel
{
    Chieftain,
    Warlord,
    Regent,
    Monarch,
    Emperor,
    Deity
}

/// <summary>
/// Encapsulates all difficulty-specific parameters matching Civilization III mechanics.
/// At Regent (the middle level), the player and AI are on equal footing.
/// Below Regent, the player receives bonuses and the AI receives penalties.
/// Above Regent, the AI receives escalating bonuses and the player faces harsher conditions.
/// </summary>
public class DifficultySettings
{
    public DifficultyLevel Level { get; }

    /// <summary>Number of citizens that start as Content in each city (before buildings/luxuries).</summary>
    public int BaseContentCitizens { get; }

    /// <summary>Number of citizens that start as Unhappy in each city (before buildings/luxuries).</summary>
    public int BaseUnhappyCitizens { get; }

    /// <summary>Multiplier applied to AI city production, science, and gold yields. 1.0 = no bonus.</summary>
    public float AiYieldMultiplier { get; }

    /// <summary>Multiplier applied to AI unit production speed (lower = faster). 1.0 = normal.</summary>
    public float AiProductionCostMultiplier { get; }

    /// <summary>Extra starting units the AI receives (Warriors) beyond the standard set.</summary>
    public int AiExtraStartingUnits { get; }

    /// <summary>Extra starting technologies the AI receives for free.</summary>
    public int AiExtraStartingTechs { get; }

    /// <summary>Maximum turns of anarchy during government transitions.</summary>
    public int MaxAnarchyTurns { get; }

    /// <summary>Barbarian spawn interval in turns. 0 = no barbarians.</summary>
    public int BarbarianSpawnInterval { get; }

    /// <summary>Bonus multiplier applied to player's science output. 1.0 = no bonus.</summary>
    public float PlayerResearchMultiplier { get; }

    /// <summary>Multiplier applied to player's corruption rate. Lower = less corruption. 1.0 = normal.</summary>
    public float PlayerCorruptionMultiplier { get; }

    /// <summary>Display name for UI.</summary>
    public string DisplayName { get; }

    /// <summary>Short description for UI.</summary>
    public string Description { get; }

    private DifficultySettings(DifficultyLevel level, int content, int unhappy, float aiYield,
        float aiProdCost, int aiExtraUnits, int aiExtraTechs, int maxAnarchy,
        int barbInterval, float playerResearch, float playerCorruption,
        string displayName, string description)
    {
        Level = level;
        BaseContentCitizens = content;
        BaseUnhappyCitizens = unhappy;
        AiYieldMultiplier = aiYield;
        AiProductionCostMultiplier = aiProdCost;
        AiExtraStartingUnits = aiExtraUnits;
        AiExtraStartingTechs = aiExtraTechs;
        MaxAnarchyTurns = maxAnarchy;
        BarbarianSpawnInterval = barbInterval;
        PlayerResearchMultiplier = playerResearch;
        PlayerCorruptionMultiplier = playerCorruption;
        DisplayName = displayName;
        Description = description;
    }

    /// <summary>
    /// Returns the settings for the given difficulty level, matching authentic Civ3 values.
    /// </summary>
    public static DifficultySettings Get(DifficultyLevel level) => level switch
    {
        DifficultyLevel.Chieftain => new(
            DifficultyLevel.Chieftain,
            content: 4, unhappy: 0,
            aiYield: 0.50f, aiProdCost: 2.00f,
            aiExtraUnits: 0, aiExtraTechs: 0,
            maxAnarchy: 2, barbInterval: 0,
            playerResearch: 1.20f, playerCorruption: 0.75f,
            "Chieftain", "Easiest. The AI is heavily penalized and you receive significant bonuses."
        ),
        DifficultyLevel.Warlord => new(
            DifficultyLevel.Warlord,
            content: 3, unhappy: 0,
            aiYield: 0.80f, aiProdCost: 1.25f,
            aiExtraUnits: 0, aiExtraTechs: 0,
            maxAnarchy: 3, barbInterval: 16,
            playerResearch: 1.10f, playerCorruption: 0.90f,
            "Warlord", "Easy. The AI is mildly penalized and you receive small bonuses."
        ),
        DifficultyLevel.Regent => new(
            DifficultyLevel.Regent,
            content: 2, unhappy: 0,
            aiYield: 1.00f, aiProdCost: 1.00f,
            aiExtraUnits: 0, aiExtraTechs: 0,
            maxAnarchy: 4, barbInterval: 8,
            playerResearch: 1.00f, playerCorruption: 1.00f,
            "Regent", "Balanced. You and the AI are on equal footing — the classic Civ3 experience."
        ),
        DifficultyLevel.Monarch => new(
            DifficultyLevel.Monarch,
            content: 2, unhappy: 1,
            aiYield: 1.10f, aiProdCost: 0.90f,
            aiExtraUnits: 1, aiExtraTechs: 0,
            maxAnarchy: 5, barbInterval: 6,
            playerResearch: 1.00f, playerCorruption: 1.00f,
            "Monarch", "Hard. The AI gets bonuses and extra starting units. Barbarians are restless."
        ),
        DifficultyLevel.Emperor => new(
            DifficultyLevel.Emperor,
            content: 1, unhappy: 2,
            aiYield: 1.30f, aiProdCost: 0.75f,
            aiExtraUnits: 2, aiExtraTechs: 1,
            maxAnarchy: 6, barbInterval: 4,
            playerResearch: 1.00f, playerCorruption: 1.00f,
            "Emperor", "Very hard. The AI gets significant bonuses, extra units, and a free tech."
        ),
        DifficultyLevel.Deity => new(
            DifficultyLevel.Deity,
            content: 1, unhappy: 3,
            aiYield: 1.60f, aiProdCost: 0.60f,
            aiExtraUnits: 3, aiExtraTechs: 2,
            maxAnarchy: 8, barbInterval: 4,
            playerResearch: 1.00f, playerCorruption: 1.00f,
            "Deity", "Nightmare. The AI is massively boosted with extra units, techs, and yields."
        ),
        _ => Get(DifficultyLevel.Regent)
    };

    /// <summary>Returns all difficulty levels in order from easiest to hardest.</summary>
    public static readonly DifficultyLevel[] AllLevels =
    {
        DifficultyLevel.Chieftain,
        DifficultyLevel.Warlord,
        DifficultyLevel.Regent,
        DifficultyLevel.Monarch,
        DifficultyLevel.Emperor,
        DifficultyLevel.Deity
    };
}
