using System.Collections.Generic;

namespace CivGame.Core;

public interface ISaveSystem
{
    void Save(string slotName, GameSimulation sim);
    bool Load(string slotName, GameSimulation sim);
    bool SaveExists(string slotName);
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
}

public class UnitSaveDto
{
    public string Id { get; set; } = "";
    public UnitType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int RemainingMovement { get; set; }
    public int Health { get; set; }
    public Faction Faction { get; set; }
    public string? CivilizationId { get; set; }
    public string? ImprovementName { get; set; }
    public int ConstructionTurnsRemaining { get; set; }
    public bool IsFortified { get; set; }
    public bool IsSleeping { get; set; }
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
    public List<string> BuildingNames { get; set; } = new();
    public List<string> WorkedTiles { get; set; } = new(); // format: "x,y"
    public Faction Faction { get; set; }
    public string? CivilizationId { get; set; }
}

public class SaveDataDto
{
    public int TurnNumber { get; set; }
    public bool IsAtWarWithAi { get; set; }
    public GameEndState EndState { get; set; }
    public string PlayerCivId { get; set; } = "rome";
    public string AiCivId { get; set; } = "babylon";
    
    // Economy and Treasury
    public int PlayerTreasury { get; set; }
    public int PlayerTaxRate { get; set; }
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
    public string? CurrentResearchId { get; set; }
    public int CurrentScienceProgress { get; set; }
    public int LastTurnScienceGenerated { get; set; }
}
