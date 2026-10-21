using System.Collections.Generic;

namespace CivGame.Core;

/// <summary>
/// Snapshot of a civilization's stats at a specific turn, for the Histograph graph.
/// Matches Civ3 histograph categories: Score, Culture, Territory (land), Population, Military (power).
/// </summary>
public class HistographEntry
{
    public int Turn { get; set; }
    public int Score { get; set; }
    public int Population { get; set; }
    public int Territory { get; set; }
    public int Culture { get; set; }
    public int Military { get; set; }
}

/// <summary>
/// Civ3 Histograph: Tracks per-turn statistics for each faction.
/// Recorded at the end of each turn in GameSimulation.
/// </summary>
public class HistographData
{
    public List<HistographEntry> PlayerHistory { get; } = new();
    public List<HistographEntry> AiHistory { get; } = new();

    /// <summary>
    /// Record a snapshot of both factions' stats for the current turn.
    /// </summary>
    public void RecordTurn(GameSimulation sim)
    {
        PlayerHistory.Add(CreateEntry(sim, Faction.Player));
        AiHistory.Add(CreateEntry(sim, Faction.AiRival));
    }

    private static HistographEntry CreateEntry(GameSimulation sim, Faction faction)
    {
        var cities = sim.Cities.FindAll(c => c.Faction == faction);
        var units = sim.Units.FindAll(u => u.Faction == faction);

        // Population: total citizens
        int population = 0;
        foreach (var c in cities) population += c.Population;

        // Territory: owned tiles
        int territory = 0;
        for (int x = 0; x < sim.Map.Width; x++)
        {
            for (int y = 0; y < sim.Map.Height; y++)
            {
                var tile = sim.Map.GetTile(x, y);
                if (tile != null && !string.IsNullOrEmpty(tile.OwnerCityId))
                {
                    var city = sim.Cities.Find(c => c.Id == tile.OwnerCityId);
                    if (city != null && city.Faction == faction)
                        territory++;
                }
            }
        }

        // Culture: total accumulated culture across all cities
        int culture = 0;
        foreach (var c in cities) culture += c.AccumulatedCulture;

        // Military: attack + defense power of all units
        int military = 0;
        foreach (var u in units) military += u.AttackStrength + u.DefenseStrength;

        return new HistographEntry
        {
            Turn = sim.TurnNumber,
            Score = sim.CalculateScore(faction),
            Population = population,
            Territory = territory,
            Culture = culture,
            Military = military
        };
    }

    /// <summary>Available graph categories matching Civ3 histograph.</summary>
    public static readonly string[] Categories = { "Score", "Population", "Territory", "Culture", "Military" };

    /// <summary>Get the value for a given category from an entry.</summary>
    public static int GetValue(HistographEntry entry, string category)
    {
        return category switch
        {
            "Score" => entry.Score,
            "Population" => entry.Population,
            "Territory" => entry.Territory,
            "Culture" => entry.Culture,
            "Military" => entry.Military,
            _ => entry.Score
        };
    }
}
