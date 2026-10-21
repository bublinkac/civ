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

            if (unit.IsNavalUnit)
            {
                if (unit.TransportCapacity > 0)
                {
                    ExecuteTransportTask(sim, unit, leader);
                }
                else
                {
                    ExecuteNavalWarshipTask(sim, unit, leader);
                }
            }
            else if (unit.Type == UnitType.Settler)
            {
                ExecuteSettlerTask(sim, unit);
            }
            else if (unit.Type == UnitType.Worker)
            {
                ExecuteWorkerTask(sim, unit);
            }
            else if (unit.Type == UnitType.Explorer)
            {
                ExecuteExplorerTask(sim, unit, rand);
            }
            else
            {
                ExecuteMilitaryTask(sim, unit, leader, aiCities);
            }
        }
    }

    private static void PlanCityProduction(GameSimulation sim, City city, LeaderStrategy leader)
    {
        if (city.CurrentProject != ProductionProject.None) return;

        // Update AI city citizen mood for decision-making
        city.UpdateCitizenMood(sim, sim.DifficultyConfig);

        // Check if under immediate threat (enemy unit within 3 tiles)
        bool underThreat = false;
        var playerUnits = sim.Units.Where(u => u.Faction == Faction.Player).ToList();
        foreach (var pUnit in playerUnits)
        {
            int dist = sim.Map.GetChebyshevDistance(pUnit.X, pUnit.Y, city.X, city.Y);
            if (dist <= 3)
            {
                underThreat = true;
                break;
            }
        }

        // Garrison strength check
        int garrisonCount = sim.Units.Count(u => u.X == city.X && u.Y == city.Y && (u.Type == UnitType.Warrior || u.Type == UnitType.Archer));

        // Priority 0a: Handle starvation — build Granary if city food is negative
        if (city.LastTurnNetFood < 0 && !city.Buildings.Any(b => b.Id == "granary"))
        {
            city.CurrentProject = ProductionProject.Granary;
            Console.WriteLine($"[AI Rival] City {city.Name} is starving (food: {city.LastTurnNetFood})! Building Granary.");
            return;
        }

        // Priority 0b: Handle unhappiness/civil disorder
        // AI prioritizes happiness buildings when city is in disorder or has many unhappy citizens
        if (city.IsInDisorder || city.UnhappyCitizens > city.HappyCitizens + 1)
        {
            // Try Temple first (cheapest happiness building)
            if (!city.Buildings.Any(b => b.Id == "temple"))
            {
                city.CurrentProject = ProductionProject.Temple;
                Console.WriteLine($"[AI Rival] City {city.Name} is unhappy! Building Temple to restore order.");
                return;
            }
            // Then Colosseum
            if (!city.Buildings.Any(b => b.Id == "colosseum"))
            {
                var colosseumBuilding = BuildingRegistry.Get("colosseum");
                if (colosseumBuilding != null && (colosseumBuilding.RequiredTechId == null || sim.IsTechResearched(city.Faction, colosseumBuilding.RequiredTechId)))
                {
                    city.CurrentProject = ProductionProject.Colosseum;
                    Console.WriteLine($"[AI Rival] City {city.Name} is unhappy! Building Colosseum.");
                    return;
                }
            }
            // Then Cathedral
            if (!city.Buildings.Any(b => b.Id == "cathedral"))
            {
                var cathedralBuilding = BuildingRegistry.Get("cathedral");
                if (cathedralBuilding != null && (cathedralBuilding.RequiredTechId == null || sim.IsTechResearched(city.Faction, cathedralBuilding.RequiredTechId)))
                {
                    city.CurrentProject = ProductionProject.Cathedral;
                    Console.WriteLine($"[AI Rival] City {city.Name} is unhappy! Building Cathedral.");
                    return;
                }
            }
            // Last resort: Marketplace for luxury happiness amplification
            if (!city.Buildings.Any(b => b.Id == "marketplace"))
            {
                city.CurrentProject = ProductionProject.Marketplace;
                Console.WriteLine($"[AI Rival] City {city.Name} is unhappy! Building Marketplace for luxury amplification.");
                return;
            }
        }

        // Priority 1: Defend the city if threatened or completely empty
        if (underThreat && garrisonCount < 2)
        {
            city.CurrentProject = GetBestDefensiveUnit(city, sim);
            Console.WriteLine($"[AI Rival] City {city.Name} is threatened! Training {city.CurrentProject} as defender.");
            return;
        }
        if (garrisonCount == 0)
        {
            city.CurrentProject = GetBestDefensiveUnit(city, sim);
            Console.WriteLine($"[AI Rival] City {city.Name} is undefended! Training {city.CurrentProject}.");
            return;
        }

        // Priority 1b: Coastal city naval fleet planning (Transports and Escort Warships)
        if (city.IsCoastal(sim.Map))
        {
            int aiTransports = sim.Units.Count(u => u.Faction == Faction.AiRival && u.IsNavalUnit && u.TransportCapacity > 0);
            bool playerAcrossWater = sim.Cities.Any(c => c.Faction == Faction.Player && !sim.Map.AreOnSameContinent(city.X, city.Y, c.X, c.Y));

            if (playerAcrossWater && aiTransports == 0 && city.Population >= 2)
            {
                city.CurrentProject = GetBestNavalTransport(city, sim);
                Console.WriteLine($"[Naval AI] Coastal city {city.Name} constructing invasion transport: {city.CurrentProject}!");
                return;
            }

            int aiWarships = sim.Units.Count(u => u.Faction == Faction.AiRival && u.IsNavalUnit && u.TransportCapacity == 0 && u.AttackStrength > 0);
            if (aiWarships < 2 && (sim.IsAtWarWithAi || aiTransports > 0))
            {
                city.CurrentProject = GetBestNavalWarship(city, sim);
                Console.WriteLine($"[Naval AI] Coastal city {city.Name} constructing naval warship: {city.CurrentProject}!");
                return;
            }
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
            city.CurrentProject = rand.Next(2) == 0 ? GetBestDefensiveUnit(city, sim) : GetBestOffensiveUnit(city, sim);
        }
        else
        {
            if (!city.Buildings.Any(b => b.Id == "barracks"))
            {
                city.CurrentProject = ProductionProject.Barracks;
            }
            else
            {
                city.CurrentProject = rand.Next(2) == 0 ? GetBestDefensiveUnit(city, sim) : GetBestOffensiveUnit(city, sim);
            }
        }
        Console.WriteLine($"[AI Rival] City {city.Name} defaulted to project: {city.CurrentProject}");
    }

    private static ProductionProject GetBestDefensiveUnit(City city, GameSimulation sim)
    {
        UnitType[] candidates = new[]
        {
            UnitType.MechInfantry,
            UnitType.TOWInfantry,
            UnitType.Infantry,
            UnitType.Rifleman,
            UnitType.Musketman,
            UnitType.Pikeman,
            UnitType.Spearman,
            UnitType.Warrior
        };

        foreach (var baseType in candidates)
        {
            var effectiveType = UnitDefinitionRegistry.GetCivUnitType(baseType, city.CivilizationId);
            var def = UnitDefinitionRegistry.Get(effectiveType);

            if (def.RequiredTechId != null && !sim.IsTechResearched(city.Faction, def.RequiredTechId))
                continue;

            bool hasAllRes = true;
            foreach (var reqRes in def.RequiredResourceIds)
            {
                if (!sim.CityHasResourceAccess(city, reqRes))
                {
                    hasAllRes = false;
                    break;
                }
            }
            if (!hasAllRes) continue;

            return UnitDefinitionRegistry.MapUnitTypeToProject(effectiveType);
        }

        return ProductionProject.Warrior;
    }

    private static ProductionProject GetBestOffensiveUnit(City city, GameSimulation sim)
    {
        UnitType[] candidates = new[]
        {
            UnitType.ModernArmor,
            UnitType.Tank,
            UnitType.Cavalry,
            UnitType.Knight,
            UnitType.MedievalInfantry,
            UnitType.Swordsman,
            UnitType.Horseman,
            UnitType.Chariot,
            UnitType.Archer,
            UnitType.Warrior
        };

        foreach (var baseType in candidates)
        {
            var effectiveType = UnitDefinitionRegistry.GetCivUnitType(baseType, city.CivilizationId);
            var def = UnitDefinitionRegistry.Get(effectiveType);

            if (def.RequiredTechId != null && !sim.IsTechResearched(city.Faction, def.RequiredTechId))
                continue;

            bool hasAllRes = true;
            foreach (var reqRes in def.RequiredResourceIds)
            {
                if (!sim.CityHasResourceAccess(city, reqRes))
                {
                    hasAllRes = false;
                    break;
                }
            }
            if (!hasAllRes) continue;

            return UnitDefinitionRegistry.MapUnitTypeToProject(effectiveType);
        }

        return ProductionProject.Warrior;
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
                if (tile == null || !tile.CanFoundCity) continue;

                // Ensure distance is at least 4 from all existing cities
                bool tooClose = false;
                foreach (var city in sim.Cities)
                {
                    int dist = sim.Map.GetChebyshevDistance(city.X, city.Y, x, y);
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
                int travelDist = sim.Map.GetChebyshevDistance(settler.X, settler.Y, x, y);
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
            int currentDist = sim.Map.GetChebyshevDistance(settler.X, settler.Y, bestX, bestY);
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
            int d = sim.Map.GetChebyshevDistance(worker.X, worker.Y, c.X, c.Y);
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
                int tx = sim.Map.NormalizeX(nearestCity.X + dx);
                int ty = nearestCity.Y + dy;
                if (!sim.Map.IsInBounds(tx, ty)) continue;

                var tile = sim.Map.GetTile(tx, ty);
                if (tile == null || !tile.IsLand) continue;

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
                    else if (new Farm().CanBeBuiltOn(targetTile.Terrain) && sim.HasIrrigationAccess(worker.X, worker.Y))
                    {
                        worker.StartImprovement(new Farm());
                        Console.WriteLine($"[AI Rival Worker] Started building Farm at ({worker.X}, {worker.Y})");
                    }
                    else if (new Mine().CanBeBuiltOn(targetTile.Terrain))
                    {
                        // Fallback: build Mine if can't irrigate
                        worker.StartImprovement(new Mine());
                        Console.WriteLine($"[AI Rival Worker] No irrigation access, building Mine instead at ({worker.X}, {worker.Y})");
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
                    int d = sim.Map.GetManhattanDistance(explorer.X, explorer.Y, x, y);
                    if (d < minExplorerDist)
                    {
                        var tile = sim.Map.GetTile(x, y);
                        if (tile != null && tile.IsLand)
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
                int dist = sim.Map.GetChebyshevDistance(city.X, city.Y, military.X, military.Y);
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
                else
                {
                    // Target city cannot be reached on foot (different continent / across water)
                    // Path to or board a nearby friendly transport ship!
                    var transport = sim.Units.FirstOrDefault(u => 
                        u.Faction == military.Faction && 
                        u.IsNavalUnit && 
                        u.TransportCapacity > 0 && 
                        u.LoadedUnitIds.Count < u.TransportCapacity);

                    if (transport != null)
                    {
                        int dist = sim.Map.GetChebyshevDistance(transport.X, transport.Y, military.X, military.Y);
                        if (dist <= 1)
                        {
                            if (sim.LoadUnitIntoTransport(military, transport))
                            {
                                Console.WriteLine($"[Naval AI] {military.Type} embarked on invasion transport {transport.Type} at ({transport.X}, {transport.Y})!");
                                return;
                            }
                        }
                        else
                        {
                            var walkPath = sim.FindPath(military, military.X, military.Y, transport.X, transport.Y, ignoreUnits: true);
                            if (walkPath != null && walkPath.Count > 1)
                            {
                                sim.MoveUnit(military, walkPath[1].X, walkPath[1].Y);
                                return;
                            }
                        }
                    }
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
                int d = sim.Map.GetChebyshevDistance(military.X, military.Y, camp.X, camp.Y);
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
            int tx = sim.Map.NormalizeX(unit.X + dxs[idx]);
            int ty = unit.Y + dys[idx];
            if (sim.Map.IsInBounds(tx, ty))
            {
                var tile = sim.Map.GetTile(tx, ty);
                if (tile != null && tile.IsLand)
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
            int tx = sim.Map.NormalizeX(unit.X + dxs[idx]);
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

    private static ProductionProject GetBestNavalTransport(City city, GameSimulation sim)
    {
        UnitType[] candidates = new[]
        {
            UnitType.Transport,
            UnitType.Galleon,
            UnitType.Caravel,
            UnitType.Galley
        };

        foreach (var baseType in candidates)
        {
            var effectiveType = UnitDefinitionRegistry.GetCivUnitType(baseType, city.CivilizationId);
            var def = UnitDefinitionRegistry.Get(effectiveType);

            if (def.RequiredTechId != null && !sim.IsTechResearched(city.Faction, def.RequiredTechId))
                continue;

            bool hasAllRes = true;
            foreach (var reqRes in def.RequiredResourceIds)
            {
                if (!sim.CityHasResourceAccess(city, reqRes))
                {
                    hasAllRes = false;
                    break;
                }
            }
            if (!hasAllRes) continue;

            return UnitDefinitionRegistry.MapUnitTypeToProject(effectiveType);
        }

        return ProductionProject.Galley;
    }

    private static ProductionProject GetBestNavalWarship(City city, GameSimulation sim)
    {
        UnitType[] candidates = new[]
        {
            UnitType.Battleship,
            UnitType.AegisCruiser,
            UnitType.Cruiser,
            UnitType.Destroyer,
            UnitType.Ironclad,
            UnitType.Frigate,
            UnitType.Dromon,
            UnitType.Curragh
        };

        foreach (var baseType in candidates)
        {
            var effectiveType = UnitDefinitionRegistry.GetCivUnitType(baseType, city.CivilizationId);
            var def = UnitDefinitionRegistry.Get(effectiveType);

            if (def.RequiredTechId != null && !sim.IsTechResearched(city.Faction, def.RequiredTechId))
                continue;

            bool hasAllRes = true;
            foreach (var reqRes in def.RequiredResourceIds)
            {
                if (!sim.CityHasResourceAccess(city, reqRes))
                {
                    hasAllRes = false;
                    break;
                }
            }
            if (!hasAllRes) continue;

            return UnitDefinitionRegistry.MapUnitTypeToProject(effectiveType);
        }

        return ProductionProject.Curragh;
    }

    private static void ExecuteTransportTask(GameSimulation sim, Unit transport, LeaderStrategy leader)
    {
        if (transport.RemainingMovement <= 0) return;

        // 1. If transport has cargo, sail towards the invasion target!
        if (transport.LoadedUnitIds.Count > 0)
        {
            var playerCities = sim.Cities.Where(c => c.Faction == Faction.Player).ToList();
            City? targetCity = null;
            int minCityDist = int.MaxValue;

            foreach (var c in playerCities)
            {
                int dist = sim.Map.GetChebyshevDistance(c.X, c.Y, transport.X, transport.Y);
                if (dist < minCityDist)
                {
                    minCityDist = dist;
                    targetCity = c;
                }
            }

            (int X, int Y)? landingSpot = null;
            if (targetCity != null)
            {
                // Check if we are adjacent to land near the target
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int tx = sim.Map.NormalizeX(transport.X + dx);
                        int ty = transport.Y + dy;
                        if (sim.Map.IsInBounds(tx, ty))
                        {
                            var t = sim.Map.GetTile(tx, ty);
                            if (t != null && t.IsLand)
                            {
                                var existingUnit = sim.Units.Find(u => u.X == tx && u.Y == ty && u.TransportUnitId == null);
                                if (existingUnit == null || existingUnit.Faction == transport.Faction)
                                {
                                    landingSpot = (tx, ty);
                                    break;
                                }
                            }
                        }
                    }
                    if (landingSpot.HasValue) break;
                }

                // If adjacent to a landing spot, launch the amphibious landing!
                if (landingSpot.HasValue)
                {
                    var loadedIds = transport.LoadedUnitIds.ToList();
                    foreach (var cargoId in loadedIds)
                    {
                        var cargo = sim.Units.Find(u => u.Id == cargoId);
                        if (cargo != null)
                        {
                            if (sim.UnloadUnitFromTransport(cargo, landingSpot.Value.X, landingSpot.Value.Y))
                            {
                                Console.WriteLine($"[Naval AI] Faction {transport.Faction} launched amphibious invasion landing {cargo.Type} at ({landingSpot.Value.X}, {landingSpot.Value.Y}) near {targetCity.Name}!");
                            }
                        }
                    }
                    return;
                }

                // Otherwise, sail towards the water tile closest to targetCity
                (int X, int Y)? bestWaterTile = null;
                int minWaterDist = int.MaxValue;

                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        int wx = sim.Map.NormalizeX(targetCity.X + dx);
                        int wy = targetCity.Y + dy;
                        if (sim.Map.IsInBounds(wx, wy))
                        {
                            var wt = sim.Map.GetTile(wx, wy);
                            if (wt != null && wt.IsOcean)
                            {
                                int d = sim.Map.GetChebyshevDistance(wx, wy, transport.X, transport.Y);
                                if (d < minWaterDist)
                                {
                                    minWaterDist = d;
                                    bestWaterTile = (wx, wy);
                                }
                            }
                        }
                    }
                }

                if (bestWaterTile.HasValue)
                {
                    var path = sim.FindPath(transport, transport.X, transport.Y, bestWaterTile.Value.X, bestWaterTile.Value.Y, ignoreUnits: true);
                    if (path != null && path.Count > 1)
                    {
                        sim.MoveUnit(transport, path[1].X, path[1].Y);
                        return;
                    }
                }
            }
        }
        else
        {
            // 2. Transport is empty — look for friendly units to board
            var nearbyLandUnits = sim.Units.Where(u => 
                u.Faction == transport.Faction && 
                u.Domain == UnitDomain.Land && 
                u.TransportUnitId == null &&
                (u.AttackStrength > 0 || u.Type == UnitType.Settler)).ToList();

            foreach (var candidate in nearbyLandUnits)
            {
                if (transport.LoadedUnitIds.Count >= transport.TransportCapacity) break;
                int dist = sim.Map.GetChebyshevDistance(candidate.X, candidate.Y, transport.X, transport.Y);
                if (dist <= 1)
                {
                    if (sim.LoadUnitIntoTransport(candidate, transport))
                    {
                        Console.WriteLine($"[Naval AI] {candidate.Type} boarded transport {transport.Type} at ({transport.X}, {transport.Y})!");
                    }
                }
            }

            if (transport.LoadedUnitIds.Count > 0) return;

            // If empty, sail to the closest friendly coastal city
            var aiCities = sim.Cities.Where(c => c.Faction == transport.Faction && c.IsCoastal(sim.Map)).ToList();
            if (aiCities.Count > 0)
            {
                var homeCity = aiCities.OrderBy(c => sim.Map.GetChebyshevDistance(c.X, c.Y, transport.X, transport.Y)).First();
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int wx = sim.Map.NormalizeX(homeCity.X + dx);
                        int wy = homeCity.Y + dy;
                        if (sim.Map.IsInBounds(wx, wy) && (wx != transport.X || wy != transport.Y))
                        {
                            var wt = sim.Map.GetTile(wx, wy);
                            if (wt != null && wt.IsOcean)
                            {
                                var path = sim.FindPath(transport, transport.X, transport.Y, wx, wy, ignoreUnits: true);
                                if (path != null && path.Count > 1)
                                {
                                    sim.MoveUnit(transport, path[1].X, path[1].Y);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static void ExecuteNavalWarshipTask(GameSimulation sim, Unit warship, LeaderStrategy leader)
    {
        if (warship.RemainingMovement <= 0) return;

        // 1. Attack adjacent enemy ships / transports
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int tx = sim.Map.NormalizeX(warship.X + dx);
                int ty = warship.Y + dy;
                if (!sim.Map.IsInBounds(tx, ty)) continue;

                var hostile = sim.Units.Find(u => u.X == tx && u.Y == ty && u.TransportUnitId == null && sim.IsHostile(warship, u) && u.Domain == UnitDomain.Sea);
                if (hostile != null)
                {
                    Console.WriteLine($"[Naval AI] {warship.Type} engages enemy {hostile.Type} at ({tx}, {ty})!");
                    sim.MoveUnit(warship, tx, ty);
                    return;
                }
            }
        }

        // 2. Coastal bombardment if warship has Bombard capability
        if (warship.BombardStrength > 0)
        {
            for (int dx = -warship.BombardRange; dx <= warship.BombardRange; dx++)
            {
                for (int dy = -warship.BombardRange; dy <= warship.BombardRange; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int tx = sim.Map.NormalizeX(warship.X + dx);
                    int ty = warship.Y + dy;
                    if (!sim.Map.IsInBounds(tx, ty)) continue;

                    var enemyCity = sim.Cities.Find(c => c.X == tx && c.Y == ty && c.Faction == Faction.Player);
                    var enemyUnit = sim.Units.Find(u => u.X == tx && u.Y == ty && u.TransportUnitId == null && sim.IsHostile(warship, u));
                    if (enemyCity != null || enemyUnit != null)
                    {
                        Console.WriteLine($"[Naval AI] {warship.Type} conducts naval bombardment on ({tx}, {ty})!");
                        sim.ExecuteBombard(warship, tx, ty);
                        return;
                    }
                }
            }
        }

        // 3. Escort friendly loaded transports
        var friendlyTransport = sim.Units.FirstOrDefault(u => 
            u.Faction == warship.Faction && 
            u.IsNavalUnit && 
            u.LoadedUnitIds.Count > 0 && 
            u.Id != warship.Id);

        if (friendlyTransport != null)
        {
            int distToTransport = sim.Map.GetChebyshevDistance(friendlyTransport.X, friendlyTransport.Y, warship.X, warship.Y);
            if (distToTransport > 1)
            {
                var path = sim.FindPath(warship, warship.X, warship.Y, friendlyTransport.X, friendlyTransport.Y, ignoreUnits: true);
                if (path != null && path.Count > 1)
                {
                    sim.MoveUnit(warship, path[1].X, path[1].Y);
                    return;
                }
            }
        }

        // 4. Hunt enemy naval vessels or patrol towards enemy coastal waters
        var enemyShip = sim.Units.FirstOrDefault(u => u.Domain == UnitDomain.Sea && sim.IsHostile(warship, u));
        if (enemyShip != null)
        {
            var path = sim.FindPath(warship, warship.X, warship.Y, enemyShip.X, enemyShip.Y, ignoreUnits: true);
            if (path != null && path.Count > 1)
            {
                sim.MoveUnit(warship, path[1].X, path[1].Y);
                return;
            }
        }

        // 5. Patrol random adjacent water tile
        MoveRandomAdjacentWater(sim, warship);
    }

    private static void MoveRandomAdjacentWater(GameSimulation sim, Unit navalUnit)
    {
        var rand = new Random();
        int[] dxs = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dys = { -1, -1, -1, 0, 0, 1, 1, 1 };
        var indices = Enumerable.Range(0, 8).OrderBy(_ => rand.Next()).ToList();

        foreach (var i in indices)
        {
            int nx = sim.Map.NormalizeX(navalUnit.X + dxs[i]);
            int ny = navalUnit.Y + dys[i];
            if (sim.Map.IsInBounds(nx, ny))
            {
                var tile = sim.Map.GetTile(nx, ny);
                if (tile != null && tile.IsOcean)
                {
                    if (sim.ValidateUnitMove(navalUnit, nx, ny).IsValid)
                    {
                        sim.MoveUnit(navalUnit, nx, ny);
                        return;
                    }
                }
            }
        }
    }
}
