using System.Collections.Generic;
using System.Linq;

namespace CivGame.Core;

/// <summary>
/// One row of the Civ3 Demographics (F11) screen: player's value, rank vs. other civs,
/// and the names of the best/worst civilizations in the category.
/// </summary>
public record DemographicRow(string Category, string PlayerValue, int Rank, int TotalCivs, string BestCiv, string WorstCiv);

/// <summary>
/// Computes Civ3-style demographic comparisons between the player and the AI rival.
/// Values are estimated the same way the Domestic Advisor displays city yields
/// (city center minimum 1 + manually worked tiles), not the full turn-processing pipeline.
/// </summary>
public static class DemographicsData
{
    private static readonly string[] Categories =
    {
        "Population", "Land Area", "GNP", "Manufactured Goods",
        "Literacy", "Military Service", "Culture", "Cities"
    };

    public static List<DemographicRow> Compute(GameSimulation sim)
    {
        var rows = new List<DemographicRow>();
        foreach (var category in Categories)
        {
            int playerVal = GetValue(sim, Faction.Player, category);
            int aiVal = GetValue(sim, Faction.AiRival, category);

            int rank = playerVal >= aiVal ? 1 : 2;
            string best = playerVal >= aiVal ? sim.PlayerCiv.Name : sim.AiCiv.Name;
            string worst = playerVal >= aiVal ? sim.AiCiv.Name : sim.PlayerCiv.Name;

            rows.Add(new DemographicRow(category, FormatValue(category, playerVal), rank, 2, best, worst));
        }
        return rows;
    }

    private static int GetValue(GameSimulation sim, Faction faction, string category)
    {
        var cities = sim.Cities.FindAll(c => c.Faction == faction);
        switch (category)
        {
            case "Population":
                return cities.Sum(c => c.Population);

            case "Land Area":
            {
                int territory = 0;
                for (int x = 0; x < sim.Map.Width; x++)
                {
                    for (int y = 0; y < sim.Map.Height; y++)
                    {
                        var tile = sim.Map.GetTile(x, y);
                        if (tile != null && !string.IsNullOrEmpty(tile.OwnerCityId))
                        {
                            var owner = sim.Cities.Find(c => c.Id == tile.OwnerCityId);
                            if (owner != null && owner.Faction == faction)
                                territory++;
                        }
                    }
                }
                return territory;
            }

            case "GNP":
                return cities.Sum(c => EstimateCityYields(sim, c).Commerce);

            case "Manufactured Goods":
                return cities.Sum(c => EstimateCityYields(sim, c).Production);

            case "Literacy":
            {
                int total = sim.Research.AllTechnologies.Count;
                if (total <= 0) return 0;
                int known = faction == Faction.Player
                    ? sim.Research.ResearchedTechIds.Count
                    : sim.Research.ResearchedTechIds.Union(sim.AiResearchedTechs).Count();
                return known * 100 / total;
            }

            case "Military Service":
                return sim.Units.Count(u => u.Faction == faction);

            case "Culture":
                return cities.Sum(c => c.AccumulatedCulture);

            case "Cities":
                return cities.Count;

            default:
                return 0;
        }
    }

    /// <summary>
    /// Simplified per-turn yield estimate matching DomesticAdvisorPanel's display:
    /// city center (minimum 1/1/1) plus manually worked tiles, without
    /// government/corruption/golden-age modifiers.
    /// </summary>
    private static (int Food, int Production, int Commerce) EstimateCityYields(GameSimulation sim, City city)
    {
        int food = 0, prod = 0, comm = 0;
        var centerTile = sim.Map.GetTile(city.X, city.Y);
        if (centerTile != null)
        {
            food += System.Math.Max(1, centerTile.TotalYield.Food);
            prod += System.Math.Max(1, centerTile.TotalYield.Production);
            comm += System.Math.Max(1, centerTile.TotalYield.Commerce);
        }

        foreach (var tilePos in city.WorkedTiles)
        {
            var t = sim.Map.GetTile(tilePos.X, tilePos.Y);
            if (t != null && t.OwnerCityId == city.Id)
            {
                food += t.TotalYield.Food;
                prod += t.TotalYield.Production;
                comm += t.TotalYield.Commerce;
            }
        }

        return (food, prod, comm);
    }

    private static string FormatValue(string category, int value)
    {
        return category switch
        {
            "Land Area" => $"{value} sq. tiles",
            "GNP" => $"{value} gold",
            "Manufactured Goods" => $"{value} shields",
            "Literacy" => $"{value}%",
            "Military Service" => $"{value} units",
            _ => value.ToString()
        };
    }
}
