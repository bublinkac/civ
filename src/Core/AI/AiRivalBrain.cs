using System;
using System.Collections.Generic;
using System.Linq;
using CivGame.Core.Pathfinding;

namespace CivGame.Core.AI;

public class LeaderStrategy
{
    public string LeaderName { get; }
    public int AggressionLevel { get; } // 1 to 5
    public float BuildUnitChance { get; } // 0.0f to 1.0f
    public List<ProductionProject> PreferredBuildings { get; }
    public bool FocusesOnWonders { get; }

    public LeaderStrategy(string leaderName, int aggression, float buildUnitChance, List<ProductionProject> preferredBuildings, bool focusesOnWonders)
    {
        LeaderName = leaderName;
        AggressionLevel = aggression;
        BuildUnitChance = buildUnitChance;
        PreferredBuildings = preferredBuildings;
        FocusesOnWonders = focusesOnWonders;
    }
}

public static class AiRivalBrain
{
    private static readonly Dictionary<string, LeaderStrategy> LeaderStrategies = new(StringComparer.OrdinalIgnoreCase);

    static AiRivalBrain()
    {
        // 16 base leaders + expansion leaders
        Register(new("Lincoln", aggression: 2, buildUnitChance: 0.35f, new() { 
            ProductionProject.Marketplace, ProductionProject.Bank, ProductionProject.Courthouse, ProductionProject.Granary, ProductionProject.Library 
        }, focusesOnWonders: false));

        Register(new("Montezuma", aggression: 5, buildUnitChance: 0.65f, new() { 
            ProductionProject.Barracks, ProductionProject.Temple, ProductionProject.Monument, ProductionProject.Walls, ProductionProject.Granary 
        }, focusesOnWonders: false));

        Register(new("Hammurabi", aggression: 3, buildUnitChance: 0.30f, new() { 
            ProductionProject.Temple, ProductionProject.Library, ProductionProject.Cathedral, ProductionProject.University, ProductionProject.Granary 
        }, focusesOnWonders: true));

        Register(new("Mao", aggression: 4, buildUnitChance: 0.50f, new() { 
            ProductionProject.Barracks, ProductionProject.Walls, ProductionProject.Marketplace, ProductionProject.Library, ProductionProject.Granary 
        }, focusesOnWonders: false));

        Register(new("Cleopatra", aggression: 3, buildUnitChance: 0.35f, new() { 
            ProductionProject.Temple, ProductionProject.Monument, ProductionProject.Colosseum, ProductionProject.Marketplace, ProductionProject.Granary 
        }, focusesOnWonders: true));

        Register(new("Elizabeth", aggression: 2, buildUnitChance: 0.30f, new() { 
            ProductionProject.Harbor, ProductionProject.Marketplace, ProductionProject.Bank, ProductionProject.Library, ProductionProject.Granary 
        }, focusesOnWonders: false));

        Register(new("Joan d'Arc", aggression: 2, buildUnitChance: 0.30f, new() { 
            ProductionProject.Cathedral, ProductionProject.Temple, ProductionProject.Marketplace, ProductionProject.StockExchange, ProductionProject.Granary 
        }, focusesOnWonders: true));

        Register(new("Bismarck", aggression: 5, buildUnitChance: 0.60f, new() { 
            ProductionProject.Barracks, ProductionProject.Factory, ProductionProject.CoalPlant, ProductionProject.Library, ProductionProject.Walls 
        }, focusesOnWonders: false));

        Register(new("Alexander", aggression: 4, buildUnitChance: 0.40f, new() { 
            ProductionProject.Library, ProductionProject.University, ProductionProject.Temple, ProductionProject.Marketplace, ProductionProject.Granary 
        }, focusesOnWonders: true));

        Register(new("Gandhi", aggression: 1, buildUnitChance: 0.20f, new() { 
            ProductionProject.Temple, ProductionProject.Cathedral, ProductionProject.Bank, ProductionProject.Marketplace, ProductionProject.Library 
        }, focusesOnWonders: true));

        Register(new("Hiawatha", aggression: 3, buildUnitChance: 0.45f, new() { 
            ProductionProject.Granary, ProductionProject.Monument, ProductionProject.Marketplace, ProductionProject.Temple, ProductionProject.Library 
        }, focusesOnWonders: false));

        Register(new("Tokugawa", aggression: 4, buildUnitChance: 0.55f, new() { 
            ProductionProject.Barracks, ProductionProject.Walls, ProductionProject.Temple, ProductionProject.Monument, ProductionProject.Granary 
        }, focusesOnWonders: false));

        Register(new("Xerxes", aggression: 3, buildUnitChance: 0.45f, new() { 
            ProductionProject.Barracks, ProductionProject.Library, ProductionProject.Marketplace, ProductionProject.Bank, ProductionProject.University 
        }, focusesOnWonders: true));

        Register(new("Caesar", aggression: 4, buildUnitChance: 0.50f, new() { 
            ProductionProject.Barracks, ProductionProject.Temple, ProductionProject.Colosseum, ProductionProject.Marketplace, ProductionProject.Granary 
        }, focusesOnWonders: false));

        Register(new("Catherine", aggression: 3, buildUnitChance: 0.40f, new() { 
            ProductionProject.Library, ProductionProject.Marketplace, ProductionProject.Granary, ProductionProject.Monument, ProductionProject.University 
        }, focusesOnWonders: false));

        Register(new("Shaka", aggression: 5, buildUnitChance: 0.70f, new() { 
            ProductionProject.Barracks, ProductionProject.Walls, ProductionProject.Monument, ProductionProject.Temple, ProductionProject.Granary 
        }, focusesOnWonders: false));
    }

    private static void Register(LeaderStrategy strategy)
    {
        LeaderStrategies[strategy.LeaderName] = strategy;
    }

    public static LeaderStrategy GetStrategy(string leaderName)
    {
        if (LeaderStrategies.TryGetValue(leaderName, out var strategy))
        {
            return strategy;
        }
        // Fallback Strategy
        return new("Generic", aggression: 3, buildUnitChance: 0.40f, new() { 
            ProductionProject.Granary, ProductionProject.Monument, ProductionProject.Barracks, ProductionProject.Temple, ProductionProject.Library 
        }, focusesOnWonders: false);
    }

    /// <summary>
    /// Processes the complete turn for the AI Rival faction.
    /// Executes city production planning and triggers unit task automation.
    /// </summary>
    public static void ProcessTurn(GameSimulation sim)
    {
        var leader = GetStrategy(sim.AiCiv.LeaderName);
        var aiCities = sim.Cities.Where(c => c.Faction == Faction.AiRival).ToList();
        var aiUnits = sim.Units.Where(u => u.Faction == Faction.AiRival).ToList();

        // 1. Plan City Productions
        foreach (var city in aiCities)
        {
            PlanCityProduction(sim, city, leader);
        }

        // 2. Automate Unit Tasks
        var rand = new Random();
        foreach (var unit in aiUnits)
        {
            // Verify if still alive (might have died during this turn's actions)
            if (!sim.Units.Contains(unit)) continue;
            if (!unit.HasMovementRemaining()) continue;

            switch (unit.Type)
            {
                case UnitType.Settler:
                    ExecuteSettlerTask(sim, unit);
                    break;
                case UnitType.Worker:
                    ExecuteWorkerTask(sim, unit);
                    break;
                case UnitType.Explorer:
                    ExecuteExplorerTask(sim, unit, rand);
                    break;
                case UnitType.Warrior:
                case UnitType.Archer:
                    ExecuteMilitaryTask(sim, unit, leader, aiCities);
                    break;
            }
        }
    }

    private static void PlanCityProduction(GameSimulation sim, City city, LeaderStrategy leader)
    {
        if (city.CurrentProject != ProductionProject.None) return;

        // Check if under immediate threat (enemy unit within 3 tiles)
        bool underThreat = false;
        var playerUnits = sim.Units.Where(u => u.Faction == Faction.Player).ToList();
        foreach (var pUnit in playerUnits)
        {
            int dist = Math.Max(Math.Abs(pUnit.X - city.X), Math.Abs(pUnit.Y - city.Y));
            if (dist <= 3)
            {
                underThreat = true;
                break;
            }
        }

        // Garrison strength check
        int garrisonCount = sim.Units.Count(u => u.X == city.X && u.Y == city.Y && (u.Type == UnitType.Warrior || u.Type == UnitType.Archer));

        // Priority 1: Defend the city if threatened or completely empty
        if (underThreat && garrisonCount < 2)
        {
            city.CurrentProject = ProductionProject.Archer;
            Console.WriteLine($"[AI Rival] City {city.Name} is threatened! Training Archer as defender.");
            return;
        }
        if (garrisonCount == 0)
        {
            city.CurrentProject = ProductionProject.Warrior;
            Console.WriteLine($"[AI Rival] City {city.Name} is undefended! Training Warrior.");
            return;
        }

        // Priority 2: Rapid early expansion (REX) - Spawn Settler if criteria met
        int aiSettlers = sim.Units.Count(u => u.Faction == Faction.AiRival && u.Type == UnitType.Settler);
        int totalAiCities = sim.Cities.Count(c => c.Faction == Faction.AiRival);

        if (city.Population >= 2 && aiSettlers == 0 && totalAiCities < 4)
        {
            city.CurrentProject = ProductionProject.Settler;
            Console.WriteLine($"[AI Rival] City {city.Name} started project: Settler (Early Expansion)");
            return;
        }

        // Priority 3: Need an Aqueduct if population is locked at 6
        bool hasFreshWater = sim.HasFreshWaterAccess(city);
        if (city.Population == 6 && !city.HasBuilding<Aqueduct>() && !hasFreshWater)
        {
            city.CurrentProject = ProductionProject.Aqueduct;
            Console.WriteLine($"[AI Rival] City {city.Name} reached size 6 cap. Building Aqueduct.");
            return;
        }

        // Priority 4: Leader Wonder focus
        if (leader.FocusesOnWonders && sim.TurnNumber % 3 == 0)
        {
            // Find first available wonder
            foreach (Wonder wonder in WonderRegistry.All.Values)
            {
                if (sim.IsWonderAvailable(wonder) && !sim.CompletedWonderIds.Contains(wonder.Id))
                {
                    // Map wonder id to production project enum
                    if (Enum.TryParse<ProductionProject>(wonder.Id, true, out var wonderProj))
                    {
                        city.CurrentProject = wonderProj;
                        Console.WriteLine($"[AI Rival] leader {leader.LeaderName} chose to build Great Wonder: {wonder.Name} in {city.Name}!");
                        return;
                    }
                }
            }
        }

        // Priority 5: Build Preferred Infrastructure
        foreach (var preferredProj in leader.PreferredBuildings)
        {
            // Verify building isn't already completed
            string? bId = GetBuildingIdFromProject(preferredProj);
            if (bId != null && !city.Buildings.Any(b => b.Id == bId))
            {
                // Check tech prerequisites (simplified check)
                var building = BuildingRegistry.Get(bId);
                if (building != null && (building.RequiredTechId == null || sim.IsTechResearched(city.Faction, building.RequiredTechId)))
                {
                    city.CurrentProject = preferredProj;
                    Console.WriteLine($"[AI Rival] City {city.Name} started preferred building: {building.Name}");
                    return;
                }
            }
        }

        // Priority 6: Default to unit training or barracks depending on leader
        var rand = new Random();
        if (rand.NextDouble() < leader.BuildUnitChance)
        {
            city.CurrentProject = rand.Next(2) == 0 ? ProductionProject.Archer : ProductionProject.Warrior;
        }
        else
        {
            if (!city.Buildings.Any(b => b.Id == "barracks"))
            {
                city.CurrentProject = ProductionProject.Barracks;
            }
            else
            {
                city.CurrentProject = ProductionProject.Warrior;
            }
        }
        Console.WriteLine($"[AI Rival] City {city.Name} defaulted to project: {city.CurrentProject}");
    }

    private static string? GetBuildingIdFromProject(ProductionProject project)
    {
        return project switch
        {
            ProductionProject.Granary => "granary",
            ProductionProject.Monument => "monument",
            ProductionProject.Walls => "walls",
            ProductionProject.Barracks => "barracks",
            ProductionProject.Temple => "temple",
            ProductionProject.Library => "library",
            ProductionProject.Courthouse => "courthouse",
            ProductionProject.Marketplace => "marketplace",
            ProductionProject.Aqueduct => "aqueduct",
            ProductionProject.Colosseum => "colosseum",
            ProductionProject.Harbor => "harbor",
            ProductionProject.Bank => "bank",
            ProductionProject.Cathedral => "cathedral",
            ProductionProject.University => "university",
            _ => null
        };
    }

    private static void ExecuteSettlerTask(GameSimulation sim, Unit settler)
    {
        // 1. Evaluate closest settlement target
        int bestX = -1;
        int bestY = -1;
        float bestScore = -1f;

        for (int x = 2; x < sim.Map.Width - 2; x += 2)
        {
            for (int y = 2; y < sim.Map.Height - 2; y += 2)
            {
                var tile = sim.Map.GetTile(x, y);
                if (tile == null || tile.Terrain.Id == "ocean") continue;

                // Ensure distance is at least 4 from all existing cities
                bool tooClose = false;
                foreach (var city in sim.Cities)
                {
                    int dist = Math.Max(Math.Abs(city.X - x), Math.Abs(city.Y - y));
                    if (dist < 4)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // Score this settlement site based on surrounding yields
                float siteScore = 0f;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        var t = sim.Map.GetTile(x + dx, y + dy);
                        if (t != null)
                        {
                            siteScore += t.TotalYield.Food * 1.5f + t.TotalYield.Production * 1.0f + t.TotalYield.Commerce * 0.5f;
                            if (t.Terrain.Id == "coast") siteScore += 2f; // Coastal settlement bonus
                        }
                    }
                }

                // Prefer closer targets slightly to minimize travel turns
                int travelDist = Math.Max(Math.Abs(settler.X - x), Math.Abs(settler.Y - y));
                siteScore -= travelDist * 0.15f;

                if (siteScore > bestScore)
                {
                    bestScore = siteScore;
                    bestX = x;
                    bestY = y;
                }
            }
        }

        // 2. Head to the best settlement site
        if (bestX != -1 && bestY != -1)
        {
            int currentDist = Math.Max(Math.Abs(settler.X - bestX), Math.Abs(settler.Y - bestY));
            if (currentDist <= 1 && sim.CanBuildCity(settler))
            {
                var newCity = sim.BuildCity(settler);
                if (newCity != null)
                {
                    Console.WriteLine($"[AI Rival] Settler founded city {newCity.Name} at ({newCity.X}, {newCity.Y})!");
                }
            }
            else
            {
                var path = sim.FindPath(settler, settler.X, settler.Y, bestX, bestY, ignoreUnits: true);
                if (path != null && path.Count > 1)
                {
                    var (nextX, nextY) = path[1];
                    sim.MoveUnit(settler, nextX, nextY);
                }
                else
                {
                    // Naive fallback
                    MoveRandomAdjacentLand(sim, settler);
                }
            }
        }
        else
        {
            MoveRandomAdjacentLand(sim, settler);
        }
    }

    private static void ExecuteWorkerTask(GameSimulation sim, Unit worker)
    {
        if (worker.IsWorkerBuilding()) return;

        // Find nearest AI city
        var aiCities = sim.Cities.Where(c => c.Faction == Faction.AiRival).ToList();
        if (aiCities.Count == 0) return;

        City nearestCity = aiCities[0];
        int minCityDist = int.MaxValue;
        foreach (var c in aiCities)
        {
            int d = Math.Max(Math.Abs(worker.X - c.X), Math.Abs(worker.Y - c.Y));
            if (d < minCityDist)
            {
                minCityDist = d;
                nearestCity = c;
            }
        }

        // Find tiles around nearest city that need improvement
        int radius = 1;
        TileData? targetTile = null;
        int bestTaskPriority = -1; // 3: Pollution, 2: Road/Railroad, 1: Improvement (Mine/Farm)

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int tx = nearestCity.X + dx;
                int ty = nearestCity.Y + dy;
                if (!sim.Map.IsInBounds(tx, ty)) continue;

                var tile = sim.Map.GetTile(tx, ty);
                if (tile == null || tile.Terrain.Id == "ocean") continue;

                // Priority 1: Clean Pollution (Value 3)
                if (tile.IsPolluted)
                {
                    targetTile = tile;
                    bestTaskPriority = 3;
                    break; // Immediate escape to handle pollution!
                }

                // Priority 2: Mine or Farm (Value 2)
                if (tile.Improvement == null && bestTaskPriority < 2)
                {
                    targetTile = tile;
                    bestTaskPriority = 2;
                }

                // Priority 3: Road (Value 1)
                if (!tile.HasRoad && !tile.HasRailroad && bestTaskPriority < 1)
                {
                    targetTile = tile;
                    bestTaskPriority = 1;
                }
            }
            if (bestTaskPriority == 3) break;
        }

        // Walk to and improve target tile
        if (targetTile != null)
        {
            if (worker.X == targetTile.X && worker.Y == targetTile.Y)
            {
                // We are on the target tile! Initiate improvement
                if (targetTile.IsPolluted)
                {
                    worker.StartImprovement(new CleanPollution());
                    Console.WriteLine($"[AI Rival Worker] Started cleaning pollution at ({worker.X}, {worker.Y})");
                }
                else if (targetTile.Improvement == null)
                {
                    if (targetTile.Terrain.Id == "mountain" || targetTile.Terrain.Id == "hills" || targetTile.Terrain.Id == "volcano")
                    {
                        worker.StartImprovement(new Mine());
                        Console.WriteLine($"[AI Rival Worker] Started building Mine at ({worker.X}, {worker.Y})");
                    }
                    else
                    {
                        worker.StartImprovement(new Farm());
                        Console.WriteLine($"[AI Rival Worker] Started building Farm at ({worker.X}, {worker.Y})");
                    }
                }
                else if (!targetTile.HasRoad)
                {
                    worker.StartImprovement(new RoadBuild());
                    Console.WriteLine($"[AI Rival Worker] Started building Road at ({worker.X}, {worker.Y})");
                }
            }
            else
            {
                // Walk towards it
                var path = sim.FindPath(worker, worker.X, worker.Y, targetTile.X, targetTile.Y, ignoreUnits: true);
                if (path != null && path.Count > 1)
                {
                    var (nextX, nextY) = path[1];
                    sim.MoveUnit(worker, nextX, nextY);
                }
                else
                {
                    MoveRandomAdjacentLand(sim, worker);
                }
            }
        }
        else
        {
            MoveRandomAdjacentLand(sim, worker);
        }
    }

    private static void ExecuteExplorerTask(GameSimulation sim, Unit explorer, Random rand)
    {
        // Find closest unexplored tile
        int closestUnexploredX = -1;
        int closestUnexploredY = -1;
        int minExplorerDist = int.MaxValue;

        // Check map (sub-sampled for efficiency)
        for (int x = 0; x < sim.Map.Width; x += 3)
        {
            for (int y = 0; y < sim.Map.Height; y += 3)
            {
                if (sim.VisibilityGrid[x, y] == FogState.Unexplored)
                {
                    int d = Math.Abs(explorer.X - x) + Math.Abs(explorer.Y - y);
                    if (d < minExplorerDist)
                    {
                        var tile = sim.Map.GetTile(x, y);
                        if (tile != null && tile.Terrain.Id != "ocean")
                        {
                            minExplorerDist = d;
                            closestUnexploredX = x;
                            closestUnexploredY = y;
                        }
                    }
                }
            }
        }

        if (closestUnexploredX != -1)
        {
            var path = sim.FindPath(explorer, explorer.X, explorer.Y, closestUnexploredX, closestUnexploredY, ignoreUnits: true);
            if (path != null && path.Count > 1)
            {
                var (nextX, nextY) = path[1];
                sim.MoveUnit(explorer, nextX, nextY);
                return;
            }
        }

        // Default to random move
        MoveRandomAdjacentLand(sim, explorer);
    }

    private static void ExecuteMilitaryTask(GameSimulation sim, Unit military, LeaderStrategy leader, List<City> aiCities)
    {
        // 1. Is there an enemy unit/city adjacent? Attack immediately!
        var adjacentHostile = FindAdjacentHostileTarget(sim, military);
        if (adjacentHostile != null)
        {
            sim.MoveUnit(military, adjacentHostile.X, adjacentHostile.Y);
            return;
        }

        // 2. If at war, march towards the closest player city or unit!
        if (sim.IsAtWarWithAi || leader.AggressionLevel >= 4)
        {
            // Find closest player city
            City? targetCity = null;
            int minCityDist = int.MaxValue;
            var playerCities = sim.Cities.Where(c => c.Faction == Faction.Player).ToList();

            foreach (var city in playerCities)
            {
                int dist = Math.Max(Math.Abs(city.X - military.X), Math.Abs(city.Y - military.Y));
                if (dist < minCityDist)
                {
                    minCityDist = dist;
                    targetCity = city;
                }
            }

            if (targetCity != null)
            {
                var path = sim.FindPath(military, military.X, military.Y, targetCity.X, targetCity.Y, ignoreUnits: true);
                if (path != null && path.Count > 1)
                {
                    var (nextX, nextY) = path[1];
                    sim.MoveUnit(military, nextX, nextY);
                    return;
                }
            }
        }

        // 3. Otherwise, check if undefended cities need garrison
        foreach (var city in aiCities)
        {
            int defenders = sim.Units.Count(u => u.X == city.X && u.Y == city.Y && u.Faction == Faction.AiRival && (u.Type == UnitType.Warrior || u.Type == UnitType.Archer));
            if (defenders < 2)
            {
                // We are not at this city - path to it and reinforce!
                if (military.X != city.X || military.Y != city.Y)
                {
                    var path = sim.FindPath(military, military.X, military.Y, city.X, city.Y, ignoreUnits: true);
                    if (path != null && path.Count > 1)
                    {
                        var (nextX, nextY) = path[1];
                        sim.MoveUnit(military, nextX, nextY);
                        return;
                    }
                }
            }
        }

        // 4. Hunt Barbarian Camps
        if (sim.BarbarianCamps.Count > 0)
        {
            var closestCamp = sim.BarbarianCamps[0];
            int minCampDist = int.MaxValue;
            foreach (var camp in sim.BarbarianCamps)
            {
                int d = Math.Max(Math.Abs(military.X - camp.X), Math.Abs(military.Y - camp.Y));
                if (d < minCampDist)
                {
                    minCampDist = d;
                    closestCamp = camp;
                }
            }

            var path = sim.FindPath(military, military.X, military.Y, closestCamp.X, closestCamp.Y, ignoreUnits: true);
            if (path != null && path.Count > 1)
            {
                var (nextX, nextY) = path[1];
                sim.MoveUnit(military, nextX, nextY);
                return;
            }
        }

        // 5. Wander/Patrol
        MoveRandomAdjacentLand(sim, military);
    }

    private static void MoveRandomAdjacentLand(GameSimulation sim, Unit unit)
    {
        int[] dxs = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dys = { -1, -1, -1, 0, 0, 1, 1, 1 };
        var rand = new Random();

        var indices = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int k = rand.Next(i + 1);
            int temp = indices[i];
            indices[i] = indices[k];
            indices[k] = temp;
        }

        foreach (int idx in indices)
        {
            int tx = unit.X + dxs[idx];
            int ty = unit.Y + dys[idx];
            if (sim.Map.IsInBounds(tx, ty))
            {
                var tile = sim.Map.GetTile(tx, ty);
                if (tile != null && tile.Terrain.Id != "ocean")
                {
                    if (sim.CanMoveUnit(unit, tx, ty))
                    {
                        sim.MoveUnit(unit, tx, ty);
                        break;
                    }
                }
            }
        }
    }

    private static Unit? FindAdjacentHostileTarget(GameSimulation sim, Unit unit)
    {
        int[] dxs = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dys = { -1, -1, -1, 0, 0, 1, 1, 1 };

        foreach (int idx in new[] { 0, 1, 2, 3, 4, 5, 6, 7 })
        {
            int tx = unit.X + dxs[idx];
            int ty = unit.Y + dys[idx];
            if (sim.Map.IsInBounds(tx, ty))
            {
                var other = sim.Units.FirstOrDefault(u => u.X == tx && u.Y == ty);
                if (other != null)
                {
                    if (other.Faction == Faction.Barbarian || (other.Faction == Faction.Player && sim.IsAtWarWithAi))
                    {
                        return other;
                    }
                }
            }
        }
        return null;
    }
}
