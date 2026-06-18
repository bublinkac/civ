using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace CivGame.Core;

public static class TerrainRegistry
{
    public static readonly Dictionary<string, Terrain> All = new();

    private const string TerrainStatsJsonPath = "res://assets/terrains/terrain_stats.json";
    
    private class TerrainImpl : Terrain 
    {
        public TerrainImpl(string id, string name, TileYield baseYield, int movementCost, int defenseBonusPercent, Color color) 
            : base(id, name, baseYield, movementCost, defenseBonusPercent, color) { }
    }

    private sealed class TerrainDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int MovementCost { get; set; }
        public int DefenseBonusPercent { get; set; }
        public int Food { get; set; }
        public int Shields { get; set; }
        public int Commerce { get; set; }
        public Color Color { get; set; }

        public TileYield Yield => new(Food, Shields, Commerce);
    }

    private sealed class TerrainStatsDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("movement_cost")]
        public int? MovementCost { get; set; }

        [JsonPropertyName("defense_bonus_percent")]
        public int? DefenseBonusPercent { get; set; }

        [JsonPropertyName("food")]
        public int? Food { get; set; }

        [JsonPropertyName("shields")]
        public int? Shields { get; set; }

        [JsonPropertyName("commerce")]
        public int? Commerce { get; set; }
    }

    static TerrainRegistry()
    {
        var definitions = BuildDefaultDefinitions();
        int overridesApplied = ApplyJsonOverrides(definitions);

        if (overridesApplied > 0)
        {
            GD.Print($"[TerrainRegistry] Loaded {definitions.Count} terrains with {overridesApplied} JSON overrides from {TerrainStatsJsonPath}.");
        }
        else
        {
            GD.Print($"[TerrainRegistry] Loaded {definitions.Count} terrains from fallback defaults (no JSON overrides).");
        }

        foreach (var def in definitions.Values)
        {
            Register(new TerrainImpl(def.Id, def.Name, def.Yield, def.MovementCost, def.DefenseBonusPercent, def.Color));
        }
    }

    private static Dictionary<string, TerrainDefinition> BuildDefaultDefinitions()
    {
        return new Dictionary<string, TerrainDefinition>
        {
            ["coast"] = new() { Id = "coast", Name = "Coast", Food = 1, Shields = 0, Commerce = 2, MovementCost = 1, DefenseBonusPercent = 10, Color = new Color(0.3f, 0.5f, 0.85f) },
            ["desert"] = new() { Id = "desert", Name = "Desert", Food = 0, Shields = 1, Commerce = 0, MovementCost = 1, DefenseBonusPercent = 10, Color = new Color(0.85f, 0.82f, 0.45f) },
            ["floodplains"] = new() { Id = "floodplains", Name = "Flood Plains", Food = 3, Shields = 0, Commerce = 0, MovementCost = 1, DefenseBonusPercent = 10, Color = new Color(0.8f, 0.7f, 0.3f) },
            ["forest"] = new() { Id = "forest", Name = "Forest", Food = 1, Shields = 2, Commerce = 0, MovementCost = 2, DefenseBonusPercent = 50, Color = new Color(0.1f, 0.3f, 0.1f) },
            ["grassland"] = new() { Id = "grassland", Name = "Grassland", Food = 2, Shields = 0, Commerce = 0, MovementCost = 1, DefenseBonusPercent = 10, Color = new Color(0.2f, 0.55f, 0.2f) },
            ["hills"] = new() { Id = "hills", Name = "Hills", Food = 1, Shields = 0, Commerce = 0, MovementCost = 2, DefenseBonusPercent = 50, Color = new Color(0.5f, 0.4f, 0.2f) },
            ["jungle"] = new() { Id = "jungle", Name = "Jungle", Food = 1, Shields = 0, Commerce = 0, MovementCost = 2, DefenseBonusPercent = 50, Color = new Color(0.2f, 0.5f, 0.2f) },
            ["marsh"] = new() { Id = "marsh", Name = "Marsh", Food = 0, Shields = 0, Commerce = 0, MovementCost = 2, DefenseBonusPercent = 25, Color = new Color(0.3f, 0.5f, 0.4f) },
            ["mountain"] = new() { Id = "mountain", Name = "Mountain", Food = 0, Shields = 1, Commerce = 0, MovementCost = 2, DefenseBonusPercent = 50, Color = new Color(0.4f, 0.4f, 0.42f) },
            ["ocean"] = new() { Id = "ocean", Name = "Ocean", Food = 1, Shields = 0, Commerce = 1, MovementCost = 1, DefenseBonusPercent = 10, Color = new Color(0.1f, 0.28f, 0.65f) },
            ["plains"] = new() { Id = "plains", Name = "Plains", Food = 1, Shields = 1, Commerce = 0, MovementCost = 1, DefenseBonusPercent = 10, Color = new Color(0.55f, 0.48f, 0.25f) },
            ["sea"] = new() { Id = "sea", Name = "Sea", Food = 1, Shields = 0, Commerce = 1, MovementCost = 1, DefenseBonusPercent = 10, Color = new Color(0.2f, 0.4f, 0.75f) },
            ["tundra"] = new() { Id = "tundra", Name = "Tundra", Food = 1, Shields = 0, Commerce = 0, MovementCost = 1, DefenseBonusPercent = 10, Color = new Color(0.6f, 0.6f, 0.6f) }
        };
    }

    private static int ApplyJsonOverrides(Dictionary<string, TerrainDefinition> definitions)
    {
        int overridesApplied = 0;

        try
        {
            string jsonPath = ProjectSettings.GlobalizePath(TerrainStatsJsonPath);
            if (!File.Exists(jsonPath))
            {
                return 0;
            }

            string json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<TerrainStatsDto>>(json, options);
            if (items == null || items.Count == 0)
            {
                return 0;
            }

            foreach (var item in items)
            {
                string? resolvedId = NormalizeTerrainId(item.Id ?? item.Name);
                if (string.IsNullOrWhiteSpace(resolvedId) || !definitions.TryGetValue(resolvedId, out var def))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(item.Name)) def.Name = item.Name!;
                if (item.MovementCost.HasValue) def.MovementCost = item.MovementCost.Value;
                if (item.DefenseBonusPercent.HasValue) def.DefenseBonusPercent = item.DefenseBonusPercent.Value;
                if (item.Food.HasValue) def.Food = item.Food.Value;
                if (item.Shields.HasValue) def.Shields = item.Shields.Value;
                if (item.Commerce.HasValue) def.Commerce = item.Commerce.Value;

                overridesApplied++;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[TerrainRegistry] Failed to load terrain_stats.json overrides: {ex.Message}");
            return 0;
        }

        return overridesApplied;
    }

    private static string? NormalizeTerrainId(string? idOrName)
    {
        if (string.IsNullOrWhiteSpace(idOrName)) return null;

        string key = idOrName.Trim().ToLowerInvariant()
            .Replace("_", " ")
            .Replace("-", " ");
        key = string.Join(" ", key.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return key switch
        {
            "flood plain" => "floodplains",
            "flood plains" => "floodplains",
            "floodplains" => "floodplains",
            "mountains" => "mountain",
            "hill" => "hills",
            "coast" => "coast",
            "desert" => "desert",
            "forest" => "forest",
            "grassland" => "grassland",
            "hills" => "hills",
            "jungle" => "jungle",
            "marsh" => "marsh",
            "mountain" => "mountain",
            "ocean" => "ocean",
            "plains" => "plains",
            "sea" => "sea",
            "tundra" => "tundra",
            _ => null
        };
    }

    private static void Register(Terrain terrain)
    {
        All[terrain.Id] = terrain;
    }

    public static Terrain? Get(string id) => All.TryGetValue(id, out var t) ? t : null;
}
