using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace CivGame.Core;

public class JsonSaveSystem : ISaveSystem
{
    private string GetSavePath(string slotName)
    {
        try
        {
            return Godot.ProjectSettings.GlobalizePath($"user://{slotName}.json");
        }
        catch
        {
            // Fallback for tests/environments without Godot context
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{slotName}.json");
        }
    }

    public void Save(string slotName, GameSimulation sim)
    {
        var dto = new SaveDataDto
        {
            TurnNumber = sim.TurnNumber,
            IsAtWarWithAi = sim.IsAtWarWithAi,
            EndState = sim.EndState,
            PlayerCivId = sim.PlayerCivId,
            AiCivId = sim.AiCivId,
            CurrentResearchId = sim.Research.CurrentResearch?.Id,
            CurrentScienceProgress = sim.Research.CurrentScienceProgress,
            LastTurnScienceGenerated = sim.Research.LastTurnScienceGenerated
        };

        // Flatten Visibility Grid
        for (int x = 0; x < sim.Map.Width; x++)
        {
            for (int y = 0; y < sim.Map.Height; y++)
            {
                dto.VisibilityGridFlat.Add((int)sim.VisibilityGrid[x, y]);
            }
        }

        // Map Tiles
        for (int x = 0; x < sim.Map.Width; x++)
        {
            for (int y = 0; y < sim.Map.Height; y++)
            {
                var tile = sim.Map.GetTile(x, y);
                if (tile != null)
                {
                    dto.Tiles.Add(new TileSaveDto
                    {
                        X = tile.X,
                        Y = tile.Y,
                        TerrainId = tile.Terrain.Id,
                        OwnerCityId = tile.OwnerCityId,
                        ImprovementName = tile.Improvement?.Name
                    });
                }
            }
        }

        // Units
        foreach (var unit in sim.Units)
        {
            dto.Units.Add(new UnitSaveDto
            {
                Id = unit.Id,
                Type = unit.Type,
                X = unit.X,
                Y = unit.Y,
                RemainingMovement = unit.RemainingMovement,
                Health = unit.Health,
                Faction = unit.Faction,
                CivilizationId = unit.CivilizationId,
                ImprovementName = unit.ImprovementUnderConstruction?.Name,
                ConstructionTurnsRemaining = unit.ConstructionTurnsRemaining,
                IsFortified = unit.IsFortified,
                IsSleeping = unit.IsSleeping
            });
        }

        // Cities
        foreach (var city in sim.Cities)
        {
            var cityDto = new CitySaveDto
            {
                Id = city.Id,
                Name = city.Name,
                X = city.X,
                Y = city.Y,
                StoredFood = city.StoredFood,
                StoredProduction = city.StoredProduction,
                StoredCommerce = city.StoredCommerce,
                Population = city.Population,
                LastTurnNetFood = city.LastTurnNetFood,
                FoundedYear = city.FoundedYear,
                CurrentProject = city.CurrentProject,
                CurrentProductionProgress = city.CurrentProductionProgress,
                Faction = city.Faction,
                CivilizationId = city.CivilizationId
            };

            foreach (var building in city.Buildings)
            {
                cityDto.BuildingNames.Add(building.Name);
            }
            dto.Cities.Add(cityDto);
        }

        // Barbarian Camps
        foreach (var camp in sim.BarbarianCamps)
        {
            dto.BarbarianCamps.Add($"{camp.X},{camp.Y}");
        }

        // Researched Technologies
        foreach (var techId in sim.Research.ResearchedTechIds)
        {
            dto.ResearchedTechIds.Add(techId);
        }

        // Serialize to file
        string path = GetSavePath(slotName);
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(dto, options);
        File.WriteAllText(path, json);

        Console.WriteLine($"[Save System] Game state successfully saved to: {path}");
    }

    public bool Load(string slotName, GameSimulation sim)
    {
        string path = GetSavePath(slotName);
        if (!File.Exists(path))
        {
            Console.WriteLine($"[Save System] Load failed: Save file does not exist at {path}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<SaveDataDto>(json);
            if (dto == null) return false;

            // Reconstruct Map and Simulation status
            var camps = new List<(int X, int Y)>();
            foreach (var campStr in dto.BarbarianCamps)
            {
                var parts = campStr.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int cx) && int.TryParse(parts[1], out int cy))
                {
                    camps.Add((cx, cy));
                }
            }

            sim.LoadSimulationState(
                dto.TurnNumber,
                dto.IsAtWarWithAi,
                dto.EndState,
                dto.PlayerCivId,
                dto.AiCivId,
                dto.Units,
                dto.Cities,
                dto.Tiles,
                dto.ResearchedTechIds,
                dto.CurrentResearchId,
                dto.CurrentScienceProgress,
                dto.LastTurnScienceGenerated,
                camps
            );

            // Reconstruct Visibility Grid
            int index = 0;
            for (int x = 0; x < sim.Map.Width; x++)
            {
                for (int y = 0; y < sim.Map.Height; y++)
                {
                    if (index < dto.VisibilityGridFlat.Count)
                    {
                        sim.VisibilityGrid[x, y] = (FogState)dto.VisibilityGridFlat[index++];
                    }
                }
            }

            Console.WriteLine($"[Save System] Game state successfully loaded from: {path}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Save System] Error loading save: {ex.Message}");
            return false;
        }
    }

    public bool SaveExists(string slotName)
    {
        string path = GetSavePath(slotName);
        return File.Exists(path);
    }
}
