using System.Collections.Generic;

namespace CivGame.Core;

public interface ISaveSystem
{
    void Save(string slotName, GameSimulation sim);
    bool Load(string slotName, GameSimulation sim);
    bool SaveExists(string slotName);
    GameSimulation? LoadAndReconstruct(string slotName);
}

// -------------------------------------------------------------
// DTO classes for simplified JSON serialization
// -------------------------------------------------------------

public class TileSaveDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public string TerrainId { get; set; } = "";
    public string? OwnerCityId { get; set; }
    public string? ImprovementName { get; set; }
    public bool HasRoad { get; set; }
    public bool HasRailroad { get; set; }
    public bool IsPolluted { get; set; }
}

public class UnitSaveDto
{
    public string Id { get; set; } = "";
    public UnitType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public float RemainingMovement { get; set; }
    public int Health { get; set; }
    public Faction Faction { get; set; }
    public string? CivilizationId { get; set; }
    public string? ImprovementName { get; set; }
    public int ConstructionTurnsRemaining { get; set; }
    public bool IsFortified { get; set; }
    public bool IsSleeping { get; set; }
    public bool IsSentry { get; set; }
    public ExperienceLevel Experience { get; set; } = ExperienceLevel.Regular;
    public string? TransportUnitId { get; set; }
    public List<string> LoadedUnitIds { get; set; } = new();
    public bool HasGeneratedLeader { get; set; }
}

public class CitySaveDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int StoredFood { get; set; }
    public int StoredProduction { get; set; }
    public int StoredCommerce { get; set; }
    public int Population { get; set; }
    public int LastTurnNetFood { get; set; }
    public int FoundedYear { get; set; }
    public ProductionProject CurrentProject { get; set; }
    public int CurrentProductionProgress { get; set; }
    public List<ProductionProject> ProductionQueue { get; set; } = new();
    public List<string> BuildingNames { get; set; } = new();
    public List<string> WorkedTiles { get; set; } = new(); // format: "x,y"
    public Faction Faction { get; set; }
    public string? CivilizationId { get; set; }
    public bool IsCapital { get; set; }
    public int AccumulatedCulture { get; set; }
    public List<SpecialistType> Specialists { get; set; } = new();
    public int DisorderTurns { get; set; }
    public bool IsWLTKD { get; set; }
}

public class SaveDataDto
{
    public int TurnNumber { get; set; }
    public bool IsAtWarWithAi { get; set; }
    public GameEndState EndState { get; set; }
    public string PlayerCivId { get; set; } = "rome";
    public string AiCivId { get; set; } = "babylon";
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Regent;

    // Government & Revolution
    public GovernmentType PlayerGovernment { get; set; } = GovernmentType.Despotism;
    public GovernmentType AiGovernment { get; set; } = GovernmentType.Despotism;
    public GovernmentType? PendingGovernment { get; set; } = null;
    public int AnarchyTurnsRemaining { get; set; } = 0;

    // Golden Age
    public bool PlayerGoldenAgeIsActive { get; set; }
    public int PlayerGoldenAgeTurnsRemaining { get; set; }
    public bool PlayerGoldenAgeHasBeenUsed { get; set; }
    public bool AiGoldenAgeIsActive { get; set; }
    public int AiGoldenAgeTurnsRemaining { get; set; }
    public bool AiGoldenAgeHasBeenUsed { get; set; }

    // War Weariness
    public int WarWearinessPoints { get; set; }
    public int WarStartTurn { get; set; } = -1;
    public bool IsDefensiveWar { get; set; }
    public int PeaceTurns { get; set; }

    // Diplomacy & Espionage
    public List<string> Embassies { get; set; } = new();
    public List<string> Spies { get; set; } = new();
    
    // Economy and Treasury
    public int PlayerTreasury { get; set; }
    public int PlayerTaxRate { get; set; }
    public int PlayerEntertainmentRate { get; set; }
    public int LastTurnIncome { get; set; }
    public int LastTurnMaintenance { get; set; }
    public int LastTurnScience { get; set; }
    public int LastTurnNetGold { get; set; }
    
    // Flat Visibility list of int representing the FogState enum
    public List<int> VisibilityGridFlat { get; set; } = new();
    
    public List<TileSaveDto> Tiles { get; set; } = new();
    public List<UnitSaveDto> Units { get; set; } = new();
    public List<CitySaveDto> Cities { get; set; } = new();
    public List<string> BarbarianCamps { get; set; } = new(); // format: "x,y"
    
    // Research stats
    public List<string> ResearchedTechIds { get; set; } = new();
    public List<string> AiResearchedTechIds { get; set; } = new();
    public string? CurrentResearchId { get; set; }
    public int CurrentScienceProgress { get; set; }
    public int LastTurnScienceGenerated { get; set; }
    public string? AiCurrentResearchId { get; set; }
    public int AiScienceProgress { get; set; }
    public List<string> BuiltSpaceshipParts { get; set; } = new();
    public List<string> AiBuiltSpaceshipParts { get; set; } = new();

    // Histograph data
    public List<HistographEntry> PlayerHistograph { get; set; } = new();
    public List<HistographEntry> AiHistograph { get; set; } = new();

    // Multiplayer Support
    public GameMode Mode { get; set; } = GameMode.SinglePlayer;
    public Faction ActiveTurnFaction { get; set; } = Faction.Player;
    public string Player1Name { get; set; } = "Player 1";
    public string Player2Name { get; set; } = "Player 2";
    public List<int> Player2VisibilityGridFlat { get; set; } = new();
    public int AiTreasury { get; set; }
    public int AiTaxRate { get; set; } = 50;
    public int AiEntertainmentRate { get; set; } = 0;
}
