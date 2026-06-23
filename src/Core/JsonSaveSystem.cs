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
            PlayerTreasury = sim.PlayerTreasury,
            PlayerTaxRate = sim.PlayerTaxRate,
            LastTurnIncome = sim.LastTurnIncome,
            LastTurnMaintenance = sim.LastTurnMaintenance,
            LastTurnScience = sim.LastTurnScience,
            LastTurnNetGold = sim.LastTurnNetGold,
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
                        ImprovementName = tile.Improvement?.Name,
                        HasRoad = tile.HasRoad,
                        HasRailroad = tile.HasRailroad,
                        IsPolluted = tile.IsPolluted
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
                ProductionQueue = city.ProductionQueue.ToList(),
                WorkedTiles = city.WorkedTiles.Select(t => $"{t.X},{t.Y}").ToList(),
                Faction = city.Faction,
                CivilizationId = city.CivilizationId,
                IsCapital = city.IsCapital,
                AccumulatedCulture = city.AccumulatedCulture
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

        // AI Researched Technologies
        foreach (var techId in sim.AiResearchedTechs)
        {
            dto.AiResearchedTechIds.Add(techId);
        }

        // AI Research progress
        dto.AiCurrentResearchId = sim.AiCurrentResearchId;
        dto.AiScienceProgress = sim.AiScienceProgress;

        // Spaceship parts
        foreach (var part in sim.BuiltSpaceshipParts)
            dto.BuiltSpaceshipParts.Add(part.ToString());
        foreach (var part in sim.AiBuiltSpaceshipParts)
            dto.AiBuiltSpaceshipParts.Add(part.ToString());

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
                dto.PlayerTreasury,
                dto.PlayerTaxRate,
                dto.LastTurnIncome,
                dto.LastTurnMaintenance,
                dto.LastTurnScience,
                dto.LastTurnNetGold,
                dto.Units,
                dto.Cities,
                dto.Tiles,
                dto.ResearchedTechIds,
                dto.CurrentResearchId,
                dto.CurrentScienceProgress,
                dto.LastTurnScienceGenerated,
                camps,
                dto.AiResearchedTechIds
            );

            // Restore AI research progress
            sim.AiCurrentResearchId = dto.AiCurrentResearchId;
            sim.AiScienceProgress = dto.AiScienceProgress;

            // Restore spaceship parts
            sim.BuiltSpaceshipParts.Clear();
            foreach (var partStr in dto.BuiltSpaceshipParts)
                if (Enum.TryParse<ProductionProject>(partStr, out var pp))
                    sim.BuiltSpaceshipParts.Add(pp);
            sim.AiBuiltSpaceshipParts.Clear();
            foreach (var partStr in dto.AiBuiltSpaceshipParts)
                if (Enum.TryParse<ProductionProject>(partStr, out var pp))
                    sim.AiBuiltSpaceshipParts.Add(pp);

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

    public GameSimulation? LoadAndReconstruct(string slotName)
    {
        string path = GetSavePath(slotName);
        if (!File.Exists(path))
        {
            Console.WriteLine($"[Save System] LoadAndReconstruct failed: Save file does not exist at {path}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<SaveDataDto>(json);
            if (dto == null || dto.Tiles.Count == 0) return null;

            int width = dto.Tiles.Max(t => t.X) + 1;
            int height = dto.Tiles.Max(t => t.Y) + 1;

            var map = new GameMap(width, height);
            
            // Pre-fill map tiles so they are not null before reconstruction
            var grassland = TerrainRegistry.Get("grassland") ?? TerrainRegistry.All.Values.First();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    map.SetTile(x, y, new TileData(x, y, grassland));
                }
            }

            var sim = new GameSimulation(map);

            if (Load(slotName, sim))
            {
                return sim;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Save System] Failed to reconstruct loaded game: {ex.Message}");
        }
        return null;
    }
}
