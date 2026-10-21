using System;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.Core;

public static class VolcanoSystem
{
    // Configure volcano parameters
    public const double EruptionProbabilityPerTurn = 0.0035; // 0.35% chance per turn per volcano

    /// <summary>
    /// Processes eruptions for all volcanoes on the map.
    /// </summary>
    public static void ProcessEruptions(GameSimulation sim)
    {
        var random = new Random();

        for (int x = 0; x < sim.Map.Width; x++)
        {
            for (int y = 0; y < sim.Map.Height; y++)
            {
                var tile = sim.Map.GetTile(x, y);
                if (tile != null && tile.Terrain.Id == "volcano")
                {
                    if (random.NextDouble() < EruptionProbabilityPerTurn)
                    {
                        TriggerEruption(sim, x, y, random);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Triggers an eruption at the specified volcano coordinate.
    /// </summary>
    public static void TriggerEruption(GameSimulation sim, int vx, int vy, Random random)
    {
        Console.WriteLine($"🔥 [VOLCANO ERUPTION] The mighty volcano at ({vx}, {vy}) has ERUPTED! Fiery magma is pouring down!");

        // 1. Destroy any units directly on the volcano
        var unitsOnVolcano = sim.Units.Where(u => u.X == vx && u.Y == vy).ToList();
        foreach (var unit in unitsOnVolcano)
        {
            Console.WriteLine($"🔥 [VOLCANO] Unit {unit.Type} of faction {unit.Faction} was vaporized by the eruption!");
            sim.Units.Remove(unit);
        }

        // 2. Process all 8 adjacent tiles
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int tx = vx + dx;
                int ty = vy + dy;

                if (sim.Map.IsInBounds(tx, ty))
                {
                    var adjacentTile = sim.Map.GetTile(tx, ty);
                    if (adjacentTile != null)
                    {
                        // 2a. Spew lava (turns tile into Pollution with 60% chance, except water tiles)
                        if (adjacentTile.Terrain.Id != "ocean" && adjacentTile.Terrain.Id != "sea" && adjacentTile.Terrain.Id != "coast")
                        {
                            if (random.NextDouble() < 0.60)
                            {
                                adjacentTile.IsPolluted = true;
                                Console.WriteLine($"🔥 [VOLCANO] Magma flows have polluted tile at ({tx}, {ty})!");
                            }
                        }

                        // 2b. Damage adjacent units
                        var unitsAdjacent = sim.Units.Where(u => u.X == tx && u.Y == ty).ToList();
                        foreach (var unit in unitsAdjacent)
                        {
                            unit.Health -= 50;
                            Console.WriteLine($"🔥 [VOLCANO] Unit {unit.Type} at ({tx}, {ty}) took 50 volcanic ash damage!");
                            if (unit.Health <= 0)
                            {
                                Console.WriteLine($"🔥 [VOLCANO] Unit {unit.Type} succumbed to toxic fumes and ash!");
                                sim.Units.Remove(unit);
                            }
                        }

                        // 2c. Damage adjacent cities (kill 1 pop, destroy a random building)
                        var city = sim.Cities.FirstOrDefault(c => c.X == tx && c.Y == ty);
                        if (city != null)
                        {
                            int originalPop = city.Population;
                            city.Population = Math.Max(1, city.Population - 1);
                            
                            string buildingLog = "";
                            if (city.Buildings.Count > 0)
                            {
                                int randBldIdx = random.Next(city.Buildings.Count);
                                var destroyedBld = city.Buildings[randBldIdx];
                                city.Buildings.RemoveAt(randBldIdx);
                                buildingLog = $" and destroyed its {destroyedBld.Name}!";
                            }

                            Console.WriteLine($"🔥 [VOLCANO] Adjacent city {city.Name} was hit! Population reduced from {originalPop} to {city.Population}{buildingLog}");
                        }
                    }
                }
            }
        }
    }
}
