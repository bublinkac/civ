using System;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.Core;

public class GameSimulation
    {
        public GameMap Map { get; }
        public List<Unit> Units { get; } = new();
        public List<City> Cities { get; } = new();
        public FogState[,] VisibilityGrid { get; }
        public int TurnNumber { get; private set; } = 1;
        public List<(int X, int Y)> BarbarianCamps { get; } = new();
        public TechManager Research { get; } = new();
        public bool IsAtWarWithAi { get; set; } = false;
        public GameEndState EndState { get; private set; } = GameEndState.None;
        public const int MaxTurnLimit = 50;

        // Economy
        public int PlayerTreasury { get; set; } = 0;
        public int PlayerTaxRate { get; set; } = 50; // 0-100 percentage
        public int LastTurnIncome { get; private set; } = 0;
        public int LastTurnMaintenance { get; private set; } = 0;
        public int LastTurnScience { get; private set; } = 0;
        public int LastTurnNetGold { get; private set; } = 0;

        // Wonder tracking
        public HashSet<string> CompletedWonderIds { get; } = new();
        public HashSet<string> CompletedSmallWonderIds { get; } = new();

        public string PlayerCivId { get; set; } = "rome";
        public string AiCivId { get; set; } = "babylon";
    public Civilization PlayerCiv => CivilizationRegistry.Get(PlayerCivId)!;
    public Civilization AiCiv => CivilizationRegistry.Get(AiCivId)!;

    private int _cityCounter = 0;
    private static readonly string[] CityNames = { "Rome", "Sparta", "Athens", "Carthage", "Constantinople", "Alexandria", "Babylon", "Thebes" };
    private int _aiCityCounter = 0;
    private static readonly string[] AiCityNames = { "Kyoto", "Berlin", "Paris", "London", "Madrid", "Washington", "Beijing", "Delhi" };

    public GameSimulation(GameMap map)
    {
        Map = map;
        VisibilityGrid = new FogState[map.Width, map.Height];
        
        // Initialize all tiles as completely Unexplored
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                VisibilityGrid[x, y] = FogState.Unexplored;
            }
        }

        // Procedurally spawn 3 Barbarian Camps on land, far from center starting area
        SpawnInitialBarbarianCamps();
    }

    private void SpawnInitialBarbarianCamps()
    {
        int centerX = Map.Width / 2;
        int centerY = Map.Height / 2;
        var rand = new Random(42); // Seeded random for predictability

        int campsSpawned = 0;
        int attempts = 0;

        while (campsSpawned < 3 && attempts < 200)
        {
            attempts++;
            int rx = rand.Next(2, Map.Width - 2);
            int ry = rand.Next(2, Map.Height - 2);

            int distFromCenter = Math.Max(Math.Abs(rx - centerX), Math.Abs(ry - centerY));
            if (distFromCenter < 8) continue; // Keep clear of starting area

            var tile = Map.GetTile(rx, ry);
            if (tile != null && tile.Terrain.Id != "ocean")
            {
                // Ensure no duplicates
                if (!BarbarianCamps.Contains((rx, ry)))
                {
                    BarbarianCamps.Add((rx, ry));
                    campsSpawned++;
                }
            }
        }
    }

    public void SetupInitialUnits()
    {
        // Find a safe land tile near the center of the map for our starting units
        int centerX = Map.Width / 2;
        int centerY = Map.Height / 2;
        
        int startX = centerX;
        int startY = centerY;
        bool foundLand = false;

        for (int r = 0; r < 12 && !foundLand; r++)
        {
            for (int dx = -r; dx <= r && !foundLand; dx++)
            {
                for (int dy = -r; dy <= r && !foundLand; dy++)
                {
                    int tx = centerX + dx;
                    int ty = centerY + dy;
                    if (Map.IsInBounds(tx, ty))
                    {
                        var tile = Map.GetTile(tx, ty);
                        if (tile != null && tile.Terrain.Id != "ocean")
                        {
                            startX = tx;
                            startY = ty;
                            foundLand = true;
                        }
                    }
                }
            }
        }

        // Spawn initial Explorer
        Units.Add(new Unit("explorer_1", UnitType.Explorer, startX, startY, Faction.Player, PlayerCivId));
        
        // Find an adjacent land tile for our Settler
        int settlerX = startX;
        int settlerY = startY;
        bool foundSettlerLand = false;

        foreach (int dx in new[] { 0, 1, -1 })
        {
            foreach (int dy in new[] { 1, -1, 0 })
            {
                if (foundSettlerLand) break;
                int tx = startX + dx;
                int ty = startY + dy;
                if (Map.IsInBounds(tx, ty) && (tx != startX || ty != startY))
                {
                    var tile = Map.GetTile(tx, ty);
                    if (tile != null && tile.Terrain.Id != "ocean")
                    {
                        settlerX = tx;
                        settlerY = ty;
                        foundSettlerLand = true;
                    }
                }
            }
        }

        Units.Add(new Unit("settler_1", UnitType.Settler, settlerX, settlerY, Faction.Player, PlayerCivId));

        SetupAiRival();

        UpdateVisibility();
    }

    public void SetupAiRival()
    {
        // Player start is near the center, let's find our player's first unit (or center)
        var playerUnit = Units.FirstOrDefault(u => u.Faction == Faction.Player);
        int px = playerUnit?.X ?? Map.Width / 2;
        int py = playerUnit?.Y ?? Map.Height / 2;

        // Opposite side of the map
        int aiStartX = Map.Width - px;
        int aiStartY = Map.Height - py;

        // Ensure within reasonable bounds
        aiStartX = Math.Clamp(aiStartX, 2, Map.Width - 3);
        aiStartY = Math.Clamp(aiStartY, 2, Map.Height - 3);

        bool foundLand = false;
        // Search in a spiral for a safe land tile
        for (int r = 0; r < 15 && !foundLand; r++)
        {
            for (int dx = -r; dx <= r && !foundLand; dx++)
            {
                for (int dy = -r; dy <= r && !foundLand; dy++)
                {
                    int tx = aiStartX + dx;
                    int ty = aiStartY + dy;
                    if (Map.IsInBounds(tx, ty))
                    {
                        var tile = Map.GetTile(tx, ty);
                        if (tile != null && tile.Terrain.Id != "ocean")
                        {
                            aiStartX = tx;
                            aiStartY = ty;
                            foundLand = true;
                        }
                    }
                }
            }
        }

        // Spawn AI Explorer
        string aiExplorerId = $"ai_explorer_{Guid.NewGuid().ToString().Substring(0, 8)}";
        Units.Add(new Unit(aiExplorerId, UnitType.Explorer, aiStartX, aiStartY, Faction.AiRival, AiCivId));

        // Find an adjacent land tile for AI Settler
        int aiSettlerX = aiStartX;
        int aiSettlerY = aiStartY;
        bool foundSettlerLand = false;

        foreach (int dx in new[] { 0, 1, -1 })
        {
            foreach (int dy in new[] { 1, -1, 0 })
            {
                if (foundSettlerLand) break;
                int tx = aiStartX + dx;
                int ty = aiStartY + dy;
                if (Map.IsInBounds(tx, ty) && (tx != aiStartX || ty != aiStartY))
                {
                    var tile = Map.GetTile(tx, ty);
                    if (tile != null && tile.Terrain.Id != "ocean")
                    {
                        aiSettlerX = tx;
                        aiSettlerY = ty;
                        foundSettlerLand = true;
                    }
                }
            }
        }

        string aiSettlerId = $"ai_settler_{Guid.NewGuid().ToString().Substring(0, 8)}";
        Units.Add(new Unit(aiSettlerId, UnitType.Settler, aiSettlerX, aiSettlerY, Faction.AiRival, AiCivId));
        
        Console.WriteLine($"[AI Rival] Spawned AI Rival starting units at ({aiStartX}, {aiStartY})");
    }

    public void UpdateVisibility()
    {
        // 1. Shroud currently visible areas (representing memory of explored tiles)
        for (int x = 0; x < Map.Width; x++)
        {
            for (int y = 0; y < Map.Height; y++)
            {
                if (VisibilityGrid[x, y] == FogState.Visible)
                {
                    VisibilityGrid[x, y] = FogState.Shrouded;
                }
            }
        }

        // 2. Apply active vision around all units
        foreach (var unit in Units)
        {
            int r = unit.VisionRange;
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    int tx = unit.X + dx;
                    int ty = unit.Y + dy;
                    if (Map.IsInBounds(tx, ty))
                    {
                        VisibilityGrid[tx, ty] = FogState.Visible;
                    }
                }
            }
        }

        // 3. Apply active vision around all cities
        foreach (var city in Cities)
        {
            int r = city.VisionRange;
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    int tx = city.X + dx;
                    int ty = city.Y + dy;
                    if (Map.IsInBounds(tx, ty))
                    {
                        VisibilityGrid[tx, ty] = FogState.Visible;
                    }
                }
            }
        }
    }

    public bool CanBuildCity(Unit unit)
    {
        if (unit.Type != UnitType.Settler) return false;

        // Cannot build city on ocean
        var tile = Map.GetTile(unit.X, unit.Y);
        if (tile == null || tile.Terrain.Id == "ocean") return false;

        // Cannot build city if another city is already here
        if (Cities.Any(c => c.X == unit.X && c.Y == unit.Y)) return false;

        return true;
    }

    public City? BuildCity(Unit unit)
    {
        if (!CanBuildCity(unit)) return null;

        string name;
        if (unit.Faction == Faction.AiRival)
        {
            name = _aiCityCounter < AiCityNames.Length ? AiCityNames[_aiCityCounter] : $"AI City {_aiCityCounter + 1}";
            _aiCityCounter++;
        }
        else
        {
            name = _cityCounter < CityNames.Length ? CityNames[_cityCounter] : $"City {_cityCounter + 1}";
            _cityCounter++;
        }

        var city = new City($"city_{Guid.NewGuid().ToString().Substring(0, 8)}", name, unit.X, unit.Y, unit.Faction, unit.CivilizationId);
        Cities.Add(city);
        Units.Remove(unit); // Consume settler

        ClaimCityTerritory(city);
        UpdateVisibility();
        return city;
    }

    public void ClaimCityTerritory(City city, int radius = 1)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int tx = city.X + dx;
                int ty = city.Y + dy;
                if (Map.IsInBounds(tx, ty))
                {
                    var tile = Map.GetTile(tx, ty);
                    // Cities only claim land tiles and unclaimed tiles
                    if (tile != null && tile.Terrain.Id != "ocean")
                    {
                        if (string.IsNullOrEmpty(tile.OwnerCityId))
                        {
                            tile.OwnerCityId = city.Id;
                        }
                    }
                }
            }
        }
    }

    public void ClaimWonder(Wonder wonder)
    {
        if (wonder.IsNationalWonder)
        {
            CompletedSmallWonderIds.Add(wonder.Id);
            Console.WriteLine($"[Wonder] Small Wonder {wonder.Name} claimed (can be built in multiple cities)!");
        }
        else
        {
            CompletedWonderIds.Add(wonder.Id);
            Console.WriteLine($"[Wonder] Wonder {wonder.Name} has been claimed globally - no other city can build it!");
            wonder.OnBuiltGlobally(this);
        }
    }

    public bool IsWonderClaimed(string wonderId)
    {
        return CompletedWonderIds.Contains(wonderId) || CompletedSmallWonderIds.Contains(wonderId);
    }

    public bool IsWonderAvailable(Wonder wonder)
    {
        if (CompletedWonderIds.Contains(wonder.Id)) return false;
        
        if (wonder.RequiredTechId != null && !Research.IsResearched(wonder.RequiredTechId))
            return false;
            
        if (wonder.RequiredResourceId != null)
        {
            var playerCityWithAccess = Cities.FirstOrDefault(c => c.Faction == Faction.Player && 
                CityHasResourceAccess(c, wonder.RequiredResourceId));
            if (playerCityWithAccess == null) return false;
        }
        
        return true;
    }

    public bool CanMoveUnit(Unit unit, int targetX, int targetY)
    {
        return ValidateUnitMove(unit, targetX, targetY).IsValid;
    }

    public MoveValidationResult ValidateUnitMove(Unit unit, int targetX, int targetY)
    {
        if (!Map.IsInBounds(targetX, targetY)) return MoveValidationResult.Invalid(MoveValidationFailureReason.TargetOutOfBounds);

        // Check if movement destination is adjacent (Chebyshev distance of 1)
        int dx = Math.Abs(targetX - unit.X);
        int dy = Math.Abs(targetY - unit.Y);
        if (dx > 1 || dy > 1 || (dx == 0 && dy == 0)) return MoveValidationResult.Invalid(MoveValidationFailureReason.TargetNotAdjacent);

        // Check if unit has any movement points left
        if (!unit.HasMovementRemaining()) return MoveValidationResult.Invalid(MoveValidationFailureReason.NoMovementRemaining);

        // Land units cannot enter Ocean tiles
        var tile = Map.GetTile(targetX, targetY);
        if (tile == null) return MoveValidationResult.Invalid(MoveValidationFailureReason.TargetTileMissing);
        if (tile.Terrain.Id == "ocean") return MoveValidationResult.Invalid(MoveValidationFailureReason.TargetTileImpassable);

        // Friendly units cannot stack on the same tile (Civ rule: 1 unit per tile of same owner category)
        var existingUnit = Units.Find(u => u.X == targetX && u.Y == targetY);
        if (existingUnit != null)
        {
            if (!IsHostile(unit, existingUnit))
            {
                return MoveValidationResult.Invalid(MoveValidationFailureReason.TargetTileImpassable);
            }
        }

        return MoveValidationResult.Valid();
    }

    public bool MoveUnit(Unit unit, int targetX, int targetY)
    {
        if (!ValidateUnitMove(unit, targetX, targetY).IsValid) return false;

        var tile = Map.GetTile(targetX, targetY)!;

        // Check if there is an enemy unit on the target tile
        var enemyUnit = Units.Find(u => u.X == targetX && u.Y == targetY);
        if (enemyUnit != null && IsHostile(unit, enemyUnit))
        {
            // Trigger Combat!
            ResolveCombat(unit, enemyUnit);
            UpdateVisibility();

            // Check for city capture if defender died and attacker advanced to that tile
            var survivingUnit = Units.Find(u => u.X == targetX && u.Y == targetY);
            if (survivingUnit != null)
            {
                CheckCityCapture(survivingUnit, targetX, targetY);
            }

            CheckGameEndConditions();
            return true;
        }

        CheckTerritoryIntrusion(unit, targetX, targetY);

        // Normal movement
        unit.MoveTo(targetX, targetY, tile.MovementCost);
        
        // Check for city capture on normal move into undefended city
        CheckCityCapture(unit, targetX, targetY);

        UpdateVisibility();

        CheckGameEndConditions();
        return true;
    }

    public bool IsHostile(Unit a, Unit b)
    {
        if (a.Faction == b.Faction) return false;
        if (a.Faction == Faction.Barbarian || b.Faction == Faction.Barbarian) return true;

        // Player vs. AI Rival
        if ((a.Faction == Faction.Player && b.Faction == Faction.AiRival) ||
            (a.Faction == Faction.AiRival && b.Faction == Faction.Player))
        {
            return IsAtWarWithAi;
        }

        return false;
    }

    private void CheckTerritoryIntrusion(Unit unit, int targetX, int targetY)
    {
        var tile = Map.GetTile(targetX, targetY);
        if (tile == null || string.IsNullOrEmpty(tile.OwnerCityId)) return;

        var city = Cities.FirstOrDefault(c => c.Id == tile.OwnerCityId);
        if (city == null) return;

        // If a player unit enters AI territory and we are at peace
        if (unit.Faction == Faction.Player && city.Faction == Faction.AiRival && !IsAtWarWithAi)
        {
            IsAtWarWithAi = true;
            Console.WriteLine($"[Diplomacy] WAR declared! Player unit {unit.Type} intruded into {city.Name}'s territory at ({targetX}, {targetY})!");
        }
        // If an AI unit enters player territory and we are at peace
        else if (unit.Faction == Faction.AiRival && city.Faction == Faction.Player && !IsAtWarWithAi)
        {
            IsAtWarWithAi = true;
            Console.WriteLine($"[Diplomacy] WAR declared! AI unit {unit.Type} intruded into {city.Name}'s territory at ({targetX}, {targetY})!");
        }
    }

    public void ResolveCombat(Unit attacker, Unit defender)
    {
        Console.WriteLine($"[Combat] {attacker.Type} (HP: {attacker.Health}) attacks {defender.Type} (HP: {defender.Health}) at ({defender.Y}, {defender.Y})!");

        // 1. Calculate Damage to Defender (based on attack and defense ratio)
        float attStrength = Math.Max(0.5f, attacker.AttackStrength);
        float defStrength = Math.Max(0.5f, defender.DefenseStrength);
        if (defender.IsFortified)
        {
            defStrength *= 1.25f; // +25% defense bonus when fortified
        }

        int damageToDefender = Math.Max(12, (int)(28.0f * (attStrength / defStrength)));
        defender.Health = Math.Max(0, defender.Health - damageToDefender);
        Console.WriteLine($"[Combat] {defender.Type} takes {damageToDefender} damage! Remaining HP: {defender.Health}");

        // 2. Counter-attack: Defender fights back if still alive
        if (defender.Health > 0)
        {
            float attDefense = Math.Max(0.5f, attacker.DefenseStrength);
            int damageToAttacker = Math.Max(10, (int)(24.0f * (defStrength / attDefense)));
            attacker.Health = Math.Max(0, attacker.Health - damageToAttacker);
            Console.WriteLine($"[Combat] {attacker.Type} takes {damageToAttacker} damage in counter-attack! Remaining HP: {attacker.Health}");
        }

        // 3. Resolve Deaths and Advances
        bool defenderDied = defender.Health <= 0;
        bool attackerDied = attacker.Health <= 0;

        if (defenderDied)
        {
            Console.WriteLine($"[Combat] {defender.Type} has been destroyed!");
            Units.Remove(defender);

            // Attacker advances onto the defender's tile upon victory
            if (!attackerDied)
            {
                var tile = Map.GetTile(defender.X, defender.Y)!;
                attacker.MoveTo(defender.X, defender.Y, tile.MovementCost);
                Console.WriteLine($"[Combat] {attacker.Type} wins and advances to ({defender.X}, {defender.Y})!");
            }
        }

        if (attackerDied)
        {
            Console.WriteLine($"[Combat] {attacker.Type} has been destroyed in battle!");
            Units.Remove(attacker);
        }

        // Combat consumes all remaining movement points for the attacker
        if (!attackerDied)
        {
            attacker.RemainingMovement = 0;
        }
    }

    private void CheckCityCapture(Unit unit, int x, int y)
    {
        // Only military units can capture cities
        if (unit.Type != UnitType.Warrior && unit.Type != UnitType.Archer && unit.Type != UnitType.Barbarian) return;

        var city = Cities.Find(c => c.X == x && c.Y == y);
        if (city == null) return;

        // Check if it's an enemy city
        if (city.Faction != unit.Faction)
        {
            if (unit.Faction == Faction.Barbarian)
            {
                Console.WriteLine($"[City Captured] Barbarians have razed the city of {city.Name} at ({x}, {y})!");

                // Reset ownership of tiles owned by this city
                for (int tx = 0; tx < Map.Width; tx++)
                {
                    for (int ty = 0; ty < Map.Height; ty++)
                    {
                        var t = Map.GetTile(tx, ty);
                        if (t != null && t.OwnerCityId == city.Id)
                        {
                            t.OwnerCityId = null;
                        }
                    }
                }
                Cities.Remove(city);
            }
            else
            {
                var oldFaction = city.Faction;
                city.Faction = unit.Faction;

                // Clear city production progress upon capture
                city.CurrentProject = ProductionProject.None;
                city.StoredProduction = 0;
                city.CurrentProductionProgress = 0;

                // Reduce population by 1 (conquest penalty)
                city.Population = Math.Max(1, city.Population - 1);

                Console.WriteLine($"[City Captured] Faction {unit.Faction} has captured the city of {city.Name} from {oldFaction} at ({x}, {y})!");
            }
        }
    }

    public void CheckGameEndConditions()
    {
        if (EndState != GameEndState.None) return;

        // Factions are alive if they have at least one city or a settler to found one
        bool playerAlive = Cities.Any(c => c.Faction == Faction.Player) || 
                           Units.Any(u => u.Faction == Faction.Player && u.Type == UnitType.Settler);
        bool aiAlive = Cities.Any(c => c.Faction == Faction.AiRival) || 
                       Units.Any(u => u.Faction == Faction.AiRival && u.Type == UnitType.Settler);

        if (!playerAlive && aiAlive)
        {
            EndState = GameEndState.DefeatDomination;
            Console.WriteLine("[Game End] DEFEAT! All your cities and settlers have been destroyed.");
            return;
        }
        if (!aiAlive && playerAlive)
        {
            EndState = GameEndState.VictoryDomination;
            Console.WriteLine("[Game End] VICTORY! You have captured all enemy cities and settlers.");
            return;
        }

        // Science Victory check (Player has unlocked all technologies)
        if (Research.ResearchedTechIds.Count == 3)
        {
            EndState = GameEndState.VictoryScience;
            Console.WriteLine("[Game End] VICTORY! You have researched all technologies and achieved a Scientific Victory!");
            return;
        }

        // Turn Limit check
        if (TurnNumber >= MaxTurnLimit)
        {
            int playerScore = CalculateScore(Faction.Player);
            int aiScore = CalculateScore(Faction.AiRival);

            if (playerScore > aiScore)
            {
                EndState = GameEndState.VictoryScore;
                Console.WriteLine($"[Game End] VICTORY! Turn limit reached. Your Score: {playerScore} | AI Score: {aiScore}");
            }
            else
            {
                EndState = GameEndState.DefeatScore;
                Console.WriteLine($"[Game End] DEFEAT! Turn limit reached. Your Score: {playerScore} | AI Score: {aiScore}");
            }
        }
    }

    public int CalculateScore(Faction faction)
    {
        var factionCities = Cities.Where(c => c.Faction == faction).ToList();
        var factionUnits = Units.Where(u => u.Faction == faction).ToList();

        int cityPoints = factionCities.Count * 15;
        int popPoints = factionCities.Sum(c => c.Population) * 3;
        int unitPoints = factionUnits.Count * 2;

        int ownedTiles = 0;
        for (int x = 0; x < Map.Width; x++)
        {
            for (int y = 0; y < Map.Height; y++)
            {
                var tile = Map.GetTile(x, y);
                if (tile != null && !string.IsNullOrEmpty(tile.OwnerCityId))
                {
                    var city = Cities.Find(c => c.Id == tile.OwnerCityId);
                    if (city != null && city.Faction == faction)
                    {
                        ownedTiles++;
                    }
                }
            }
        }
        int tilePoints = ownedTiles * 1;

        int techPoints = 0;
        if (faction == Faction.Player)
        {
            techPoints = Research.ResearchedTechIds.Count * 10;
        }
        else
        {
            techPoints = Math.Min(3, TurnNumber / 15) * 10;
        }

        return cityPoints + popPoints + unitPoints + tilePoints + techPoints;
    }

    public void LoadSimulationState(
        int turnNumber, 
        bool isAtWarWithAi, 
        GameEndState endState,
        string playerCivId,
        string aiCivId,
        List<UnitSaveDto> unitDtos, 
        List<CitySaveDto> cityDtos, 
        List<TileSaveDto> tileDtos, 
        List<string> researchedIds, 
        string? activeTechId, 
        int progress, 
        int lastTurnScience,
        List<(int X, int Y)> camps)
    {
        TurnNumber = turnNumber;
        IsAtWarWithAi = isAtWarWithAi;
        EndState = endState;
        PlayerCivId = playerCivId;
        AiCivId = aiCivId;

        // 1. Reconstruct Map Tiles
        foreach (var dto in tileDtos)
        {
            var tile = Map.GetTile(dto.X, dto.Y);
            if (tile != null)
            {
                var terrain = TerrainRegistry.Get(dto.TerrainId);
                if (terrain != null) tile.Terrain = terrain;
                
                tile.OwnerCityId = dto.OwnerCityId;
                tile.MovementCost = tile.Terrain.MovementCost;
                
                // Reconstruct Improvement
                if (string.IsNullOrEmpty(dto.ImprovementName))
                {
                    tile.Improvement = null;
                }
                else
                {
                    tile.Improvement = dto.ImprovementName switch
                    {
                        "Farm" => new Farm(),
                        "Mine" => new Mine(),
                        "Plantation" => new Plantation(),
                        _ => null
                    };
                }
            }
        }

        // 2. Reconstruct Units
        Units.Clear();
        foreach (var dto in unitDtos)
        {
            var unit = new Unit(dto.Id, dto.Type, dto.X, dto.Y, dto.Faction, dto.CivilizationId);
            unit.RemainingMovement = dto.RemainingMovement;
            unit.Health = dto.Health;
            
            if (!string.IsNullOrEmpty(dto.ImprovementName))
            {
                unit.ImprovementUnderConstruction = dto.ImprovementName switch
                {
                    "Farm" => new Farm(),
                    "Mine" => new Mine(),
                    "Plantation" => new Plantation(),
                    _ => null
                };
                unit.ConstructionTurnsRemaining = dto.ConstructionTurnsRemaining;
            }
            Units.Add(unit);
        }

        // 3. Reconstruct Cities
        Cities.Clear();
        foreach (var dto in cityDtos)
        {
            var city = new City(dto.Id, dto.Name, dto.X, dto.Y, dto.Faction, dto.CivilizationId);
            city.StoredFood = dto.StoredFood;
            city.StoredProduction = dto.StoredProduction;
            city.StoredCommerce = dto.StoredCommerce;
            city.Population = dto.Population;
            city.LastTurnNetFood = dto.LastTurnNetFood;
            city.CurrentProject = dto.CurrentProject;
            city.CurrentProductionProgress = dto.CurrentProductionProgress;
            
            // Reconstruct Buildings
            city.Buildings.Clear();
            foreach (var bName in dto.BuildingNames)
            {
                Building? b = bName switch
                {
                    "Monument" => new Monument(),
                    "Granary" => new Granary(),
                    _ => null
                };
                if (b != null)
                {
                    city.Buildings.Add(b);
                }
            }
            Cities.Add(city);
        }

        // 4. Reconstruct Barbarian Camps
        BarbarianCamps.Clear();
        BarbarianCamps.AddRange(camps);

        // 5. Reconstruct Research
        Research.LoadResearchState(researchedIds, activeTechId, progress, lastTurnScience);
    }

    public HashSet<(int X, int Y)> GetReachableTiles(Unit unit)
    {
        var reachable = new HashSet<(int, int)>();
        if (unit.RemainingMovement <= 0) return reachable;

        // BFS: each state tracks (x, y, mpRemaining after entering that tile)
        var queue = new Queue<(int X, int Y, int Mp)>();
        var bestMp = new Dictionary<(int, int), int>();

        queue.Enqueue((unit.X, unit.Y, unit.RemainingMovement));
        bestMp[(unit.X, unit.Y)] = unit.RemainingMovement;

        int[] dxs = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dys = { -1, -1, -1, 0, 0, 1, 1, 1 };

        while (queue.Count > 0)
        {
            var (cx, cy, mp) = queue.Dequeue();
            if (mp <= 0) continue;

            for (int i = 0; i < 8; i++)
            {
                int nx = cx + dxs[i];
                int ny = cy + dys[i];

                if (!Map.IsInBounds(nx, ny)) continue;
                var tile = Map.GetTile(nx, ny);
                if (tile == null || tile.Terrain.Id == "ocean") continue;

                int remaining = Math.Max(0, mp - tile.MovementCost);
                reachable.Add((nx, ny));

                if (!bestMp.TryGetValue((nx, ny), out int prev) || remaining > prev)
                {
                    bestMp[(nx, ny)] = remaining;
                    queue.Enqueue((nx, ny, remaining));
                }
            }
        }

        return reachable;
    }

    public void EndTurn()
    {
        // 1. Process Barbarian action before starting next turn
        ProcessBarbarianTurn();

        // 2. Process AI Rival action
        ProcessAiRivalTurn();

        // 3. Advance Worker Construction
        ProcessWorkerConstruction();

        TurnNumber++;
        foreach (var unit in Units)
        {
            unit.ResetMovement();
        }
        int scienceGenerated = CollectCityYields();
        Research.AddScience(scienceGenerated, this);
        
        UpdateVisibility();

        // 4. Check for game end conditions
        CheckGameEndConditions();
    }

    private void ProcessWorkerConstruction()
    {
        foreach (var unit in Units)
        {
            if (unit.Type == UnitType.Worker && unit.IsWorkerBuilding())
            {
                unit.ConstructionTurnsRemaining--;
                Console.WriteLine($"[Construction] Worker {unit.Id} working at ({unit.X}, {unit.Y}). {unit.ConstructionTurnsRemaining} turns remaining for {unit.ImprovementUnderConstruction!.Name}.");

                if (unit.ConstructionTurnsRemaining <= 0)
                {
                    var tile = Map.GetTile(unit.X, unit.Y);
                    if (tile != null)
                    {
                        var improvement = unit.ImprovementUnderConstruction;
                        if (improvement is RoadBuild)
                        {
                            tile.HasRoad = true;
                            Console.WriteLine($"[Construction Completed] Road built at ({unit.X}, {unit.Y})!");
                        }
                        else
                        {
                            tile.Improvement = improvement;
                            Console.WriteLine($"[Construction Completed] {improvement.Name} built at ({unit.X}, {unit.Y})!");
                        }
                    }

                    unit.CancelImprovement(); // Reset worker state
                }
            }
        }
    }

    private void ProcessBarbarianTurn()
    {
        // Spawn barbarians every 8 turns from each camp
        if (TurnNumber % 8 == 0)
        {
            foreach (var camp in BarbarianCamps)
            {
                // Ensure no unit is blocking the camp tile before spawning
                bool isBlocked = Units.Any(u => u.X == camp.X && u.Y == camp.Y);
                if (!isBlocked)
                {
                    string barbId = $"barb_{Guid.NewGuid().ToString().Substring(0, 8)}";
                    Units.Add(new Unit(barbId, UnitType.Barbarian, camp.X, camp.Y, Faction.Barbarian));
                    Console.WriteLine($"[Barbarians] A new barbarian warrior spawned at ({camp.X}, {camp.Y})!");
                }
            }
        }

        // Move/Attack with all Barbarian units
        // Use ToList() to prevent collection modification exceptions if barbarians die in combat during their turn!
        var barbarians = Units.Where(u => u.IsBarbarian).ToList();
        foreach (var barb in barbarians)
        {
            // Verify if still alive (might have died in a previous combat this turn)
            if (!Units.Contains(barb)) continue;

            // Find closest target (either player unit or player city)
            int closestDist = int.MaxValue;
            int targetX = -1;
            int targetY = -1;

            // Search player units
            var playerUnits = Units.Where(u => !u.IsBarbarian).ToList();
            foreach (var pUnit in playerUnits)
            {
                int dist = Math.Max(Math.Abs(pUnit.X - barb.X), Math.Abs(pUnit.Y - barb.Y));
                if (dist < closestDist)
                {
                    closestDist = dist;
                    targetX = pUnit.X;
                    targetY = pUnit.Y;
                }
            }

            // Search player cities
            foreach (var city in Cities)
            {
                int dist = Math.Max(Math.Abs(city.X - barb.X), Math.Abs(city.Y - barb.Y));
                if (dist < closestDist)
                {
                    closestDist = dist;
                    targetX = city.X;
                    targetY = city.Y;
                }
            }

            // If a player target was found, take a step towards it
            if (closestDist != int.MaxValue && closestDist > 0)
            {
                int stepX = barb.X + Math.Sign(targetX - barb.X);
                int stepY = barb.Y + Math.Sign(targetY - barb.Y);

                if (Map.IsInBounds(stepX, stepY))
                {
                    // Move or attack!
                    MoveUnit(barb, stepX, stepY);
                }
            }
        }
    }

    private void ProcessAiRivalTurn()
    {
        // 1. Manage AI cities' production projects
        var aiCities = Cities.Where(c => c.Faction == Faction.AiRival).ToList();
        foreach (var city in aiCities)
        {
            if (city.CurrentProject == ProductionProject.None)
            {
                // Count existing AI units of different types
                int aiSettlers = Units.Count(u => u.Faction == Faction.AiRival && u.Type == UnitType.Settler);
                int aiWorkers = Units.Count(u => u.Faction == Faction.AiRival && u.Type == UnitType.Worker);
                int aiMilitary = Units.Count(u => u.Faction == Faction.AiRival && (u.Type == UnitType.Warrior || u.Type == UnitType.Archer));

                if (city.Population >= 2 && Cities.Count(c => c.Faction == Faction.AiRival) < 3 && aiSettlers == 0)
                {
                    city.CurrentProject = ProductionProject.Settler;
                }
                else if (aiWorkers == 0)
                {
                    city.CurrentProject = ProductionProject.Worker;
                }
                else if (aiMilitary < 2)
                {
                    city.CurrentProject = ProductionProject.Warrior;
                }
                else if (!city.HasBuilding<Granary>())
                {
                    city.CurrentProject = ProductionProject.Granary;
                }
                else if (!city.HasBuilding<Monument>())
                {
                    city.CurrentProject = ProductionProject.Monument;
                }
                else
                {
                    city.CurrentProject = ProductionProject.Warrior;
                }

                Console.WriteLine($"[AI Rival] City {city.Name} started project: {city.CurrentProject}");
            }
        }

        // 2. Process AI unit actions
        var aiUnits = Units.Where(u => u.Faction == Faction.AiRival).ToList();
        var rand = new Random();

        foreach (var unit in aiUnits)
        {
            if (!Units.Contains(unit)) continue;

            if (unit.Type == UnitType.Settler)
            {
                bool canSettleHere = true;
                foreach (var otherCity in Cities)
                {
                    int dist = Math.Max(Math.Abs(otherCity.X - unit.X), Math.Abs(otherCity.Y - unit.Y));
                    if (dist < 4)
                    {
                        canSettleHere = false;
                        break;
                    }
                }

                if (canSettleHere && CanBuildCity(unit))
                {
                    var newCity = BuildCity(unit);
                    if (newCity != null)
                    {
                        Console.WriteLine($"[AI Rival] Founded city {newCity.Name} at ({newCity.X}, {newCity.Y})!");
                    }
                }
                else
                {
                    MoveToRandomAdjacentLand(unit, rand);
                }
            }
            else if (unit.Type == UnitType.Worker)
            {
                if (unit.IsWorkerBuilding()) continue;

                var tile = Map.GetTile(unit.X, unit.Y);
                if (tile != null && tile.Improvement == null)
                {
                    if (tile.Terrain.Id == "mountain" || tile.Terrain.Id == "desert")
                    {
                        unit.StartImprovement(new Mine());
                        Console.WriteLine($"[AI Rival] Worker at ({unit.X}, {unit.Y}) started building Mine");
                    }
                    else if (tile.Terrain.Id == "grassland" || tile.Terrain.Id == "plains")
                    {
                        unit.StartImprovement(new Farm());
                        Console.WriteLine($"[AI Rival] Worker at ({unit.X}, {unit.Y}) started building Farm");
                    }
                    else
                    {
                        MoveToRandomAdjacentLand(unit, rand);
                    }
                }
                else
                {
                    MoveToRandomAdjacentLand(unit, rand);
                }
            }
            else
            {
                // Explorer / Warrior / Archer
                var target = FindAdjacentHostileTarget(unit);
                if (target != null)
                {
                    MoveUnit(unit, target.X, target.Y);
                }
                else
                {
                    MoveToRandomAdjacentLand(unit, rand);
                }
            }
        }
    }

    private void MoveToRandomAdjacentLand(Unit unit, Random rand)
    {
        if (!unit.HasMovementRemaining()) return;

        int[] dxs = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dys = { -1, -1, -1, 0, 0, 1, 1, 1 };

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
            if (Map.IsInBounds(tx, ty))
            {
                var tile = Map.GetTile(tx, ty);
                if (tile != null && tile.Terrain.Id != "ocean")
                {
                    if (CanMoveUnit(unit, tx, ty))
                    {
                        MoveUnit(unit, tx, ty);
                        break;
                    }
                }
            }
        }
    }

    private Unit? FindAdjacentHostileTarget(Unit unit)
    {
        int[] dxs = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dys = { -1, -1, -1, 0, 0, 1, 1, 1 };

        foreach (int idx in new[] { 0, 1, 2, 3, 4, 5, 6, 7 })
        {
            int tx = unit.X + dxs[idx];
            int ty = unit.Y + dys[idx];
            if (Map.IsInBounds(tx, ty))
            {
                var other = Units.FirstOrDefault(u => u.X == tx && u.Y == ty);
                if (other != null)
                {
                    if (other.Faction == Faction.Barbarian || (other.Faction == Faction.Player && IsAtWarWithAi))
                    {
                        return other;
                    }
                }
            }
        }
        return null;
    }

    private int CollectCityYields()
    {
        int totalCommerceThisTurn = 0;
        int totalMaintenanceThisTurn = 0;

        foreach (var city in Cities)
        {
            int food = 0, production = 0, commerce = 0;

            // 1. Center tile (city center itself) is ALWAYS worked
            var centerTile = Map.GetTile(city.X, city.Y);
            if (centerTile != null)
            {
                var yield = centerTile.TotalYield;
                food += yield.Food;
                production += yield.Production;
                commerce += yield.Commerce;
            }

            // 2. Gather other owned tiles in a 3x3 radius
            var surroundingTiles = new System.Collections.Generic.List<TileData>();
            int radius = 1;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    // Skip the center city tile as it is already worked
                    if (dx == 0 && dy == 0) continue;

                    int tx = city.X + dx;
                    int ty = city.Y + dy;
                    if (Map.IsInBounds(tx, ty))
                    {
                        var tile = Map.GetTile(tx, ty);
                        if (tile != null && tile.OwnerCityId == city.Id)
                        {
                            surroundingTiles.Add(tile);
                        }
                    }
                }
            }

            // 4. Citizens work up to Population number of surrounding tiles
            // Determine which tiles are worked based on manual assignment, 
            // or default to automated if not assigned.
            
            var workingTiles = new System.Collections.Generic.List<TileData>();
            
            // Try to use manually assigned tiles first
            if (city.WorkedTiles.Count > 0)
            {
                foreach (var tilePos in city.WorkedTiles)
                {
                    var t = Map.GetTile(tilePos.X, tilePos.Y);
                    if (t != null && t.OwnerCityId == city.Id)
                    {
                        workingTiles.Add(t);
                    }
                }
            }
            
            // If we don't have enough manual tiles, fill the rest with the best automatic ones
            if (workingTiles.Count < city.Population)
            {
                // Find all potential tiles the city can work
                var potentialTiles = new System.Collections.Generic.List<TileData>();
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // Center tile handled separately
                        int tx = city.X + dx;
                        int ty = city.Y + dy;
                        if (Map.IsInBounds(tx, ty))
                        {
                            var tile = Map.GetTile(tx, ty);
                            if (tile != null && tile.OwnerCityId == city.Id && !city.WorkedTiles.Contains((tx, ty)))
                            {
                                potentialTiles.Add(tile);
                            }
                        }
                    }
                }
                
                // Sort by total yield
                potentialTiles.Sort((a, b) => 
                    (b.TotalYield.Food + b.TotalYield.Production + b.TotalYield.Commerce)
                    .CompareTo(a.TotalYield.Food + a.TotalYield.Production + a.TotalYield.Commerce)
                );
                
                int needed = city.Population - workingTiles.Count;
                for (int i = 0; i < Math.Min(needed, potentialTiles.Count); i++)
                {
                    workingTiles.Add(potentialTiles[i]);
                }
            }

            // Sum up yields from working tiles
            foreach (var tile in workingTiles)
            {
                var yield = tile.TotalYield;
                food += yield.Food;
                production += yield.Production;
                commerce += yield.Commerce;
            }

            // 5. Food Consumption & Growth/Starvation
            int foodConsumption = city.Population * 2;
            int netFood = food - foodConsumption;
            city.LastTurnNetFood = netFood;

            city.StoredFood += netFood;
            
            // Check population cap
            int popCap = 9999;
            if (city.HasBuilding<Hospital>()) popCap = 20;
            else if (city.HasBuilding<Aqueduct>()) popCap = 12;

            if (city.StoredFood >= city.FoodNeededForGrowth)
            {
                if (city.Population < popCap)
                {
                    int needed = city.FoodNeededForGrowth;
                    int overflow = city.StoredFood - needed;
                    city.Population++;

                    // Granary preserves a percentage of the required food on growth
                    if (city.HasBuilding<Granary>())
                    {
                        float ratio = city.GetBuilding<Granary>()?.FoodKeepRatio ?? 0.5f;
                        int preservedFood = (int)(needed * ratio);
                        city.StoredFood = preservedFood + overflow;
                        Console.WriteLine($"[City Growth] {city.Name} has grown! Population is now {city.Population}. Granary preserved {preservedFood} food!");
                    }
                    else
                    {
                        city.StoredFood = overflow;
                        Console.WriteLine($"[City Growth] {city.Name} has grown! Population is now {city.Population}.");
                    }
                }
                else
                {
                    city.StoredFood = city.FoodNeededForGrowth; // Cap food if at pop cap
                    Console.WriteLine($"[City Growth] {city.Name} cannot grow further (Pop Cap: {popCap})");
                }
            }
            else if (city.StoredFood < 0)
            {
                if (city.Population > 1)
                {
                    city.Population--;
                    city.StoredFood = city.FoodNeededForGrowth / 2; // Soft cushion after population loss
                    Console.WriteLine($"[Famine] {city.Name} has starved! Population shrank to {city.Population}.");
                }
                else
                {
                    city.StoredFood = 0; // Cap at 0 for pop 1
                    Console.WriteLine($"[Famine] {city.Name} is starving, but population cannot shrink below 1.");
                }
            }

            // 6. Commerce collection and Maintenance
            int cityMaintenance = city.GetTotalMaintenance();
            totalMaintenanceThisTurn += cityMaintenance;
            totalCommerceThisTurn += commerce;
            
            // City stores local net commerce (optional, mostly for display logic)
            int netCityCommerce = Math.Max(0, commerce - cityMaintenance);
            city.StoredCommerce += netCityCommerce;

            // Handle production allocation
            if (city.CurrentProject != ProductionProject.None)
            {
                city.CurrentProductionProgress += production;
                
                // If there's stored production from previous idle turns, feed it in
                if (city.StoredProduction > 0)
                {
                    city.CurrentProductionProgress += city.StoredProduction;
                    city.StoredProduction = 0;
                }

                int cost = city.GetProjectCost(city.CurrentProject);
                if (city.CurrentProductionProgress >= cost)
                {
                    city.CurrentProductionProgress -= cost; // Keep remainder/overflow
                    SpawnProjectResult(city);
                }
            }
            else
            {
                city.StoredProduction += production;
            }
        }

        // Apply national economy (only for player, AI doesn't track treasury yet)
        int netCommerce = totalCommerceThisTurn - totalMaintenanceThisTurn;
        int scienceGain = 0;
        int goldGain = 0;

        if (netCommerce > 0)
        {
            goldGain = (netCommerce * PlayerTaxRate) / 100;
            scienceGain = netCommerce - goldGain; // Remainder goes to science
        }
        else if (netCommerce < 0)
        {
            goldGain = netCommerce; // Negative gold!
            scienceGain = 0;
        }

        PlayerTreasury += goldGain;
        if (PlayerTreasury < 0)
        {
            PlayerTreasury = 0; // Cap at 0 for now (later we might disband units)
        }

        LastTurnIncome = totalCommerceThisTurn;
        LastTurnMaintenance = totalMaintenanceThisTurn;
        LastTurnScience = scienceGain;
        LastTurnNetGold = goldGain;

        return scienceGain;
    }

    private void SpawnProjectResult(City city)
    {
        var project = city.CurrentProject;

        string? buildingId = project switch
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
            ProductionProject.Hospital => "hospital",
            ProductionProject.Factory => "factory",
            ProductionProject.CoalPlant => "coal_plant",
            ProductionProject.HydroPlant => "hydro_plant",
            ProductionProject.SolarPlant => "solar_plant",
            ProductionProject.NuclearPlant => "nuclear_plant",
            ProductionProject.ManufacturingPlant => "manufacturing_plant",
            ProductionProject.Airport => "airport",
            ProductionProject.CivilDefense => "civil_defense",
            ProductionProject.PoliceStation => "police_station",
            ProductionProject.StockExchange => "stock_exchange",
            ProductionProject.RecyclingCenter => "recycling_center",
            ProductionProject.ResearchLab => "research_lab",
            ProductionProject.SAMMissileBattery => "sam_missile_battery",
            ProductionProject.OffshorePlatform => "offshore_platform",
            ProductionProject.MassTransitSystem => "mass_transit_system",
            ProductionProject.CommercialDock => "commercial_dock",
            ProductionProject.CoastalFortress => "coastal_fortress",
            ProductionProject.SSCockpit => "ss_cockpit",
            ProductionProject.SSDockingBay => "ss_docking_bay",
            ProductionProject.SSEngine => "ss_engine",
            ProductionProject.SSExteriorCasing => "ss_exterior_casing",
            ProductionProject.SSFuelCells => "ss_fuel_cells",
            ProductionProject.SSLifeSupportSystem => "ss_life_support_system",
            ProductionProject.SSPlanetaryPartyLounge => "ss_planetary_party_lounge",
            ProductionProject.SSStasisChamber => "ss_stasis_chamber",
            ProductionProject.SSStorageSupply => "ss_storage_supply",
            ProductionProject.SSThrusters => "ss_thrusters",
            ProductionProject.Palace => "palace",
            // Wonders
            ProductionProject.Pyramids => "pyramids",
            ProductionProject.HangingGardens => "hanging_gardens",
            ProductionProject.Colossus => "colossus",
            ProductionProject.GreatWall => "great_wall",
            ProductionProject.StatueOfZeus => "statue_of_zeus",
            ProductionProject.Oracle => "oracle",
            ProductionProject.KnightsHall => "knights_hall",
            ProductionProject.SovereignBath => "sovereign_bath",
            ProductionProject.LeonardoWorkshop => "leonardo_workshop",
            ProductionProject.ShakespearesTheatre => "shakespeares_theatre",
            ProductionProject.SunTzusWarAcademy => "sun_tzu_war_academy",
            ProductionProject.CureForCancer => "cure_for_cancer",
            ProductionProject.SistineChapel => "sistine_chapel",
            ProductionProject.TajMahal => "taj_mahal",
            ProductionProject.Astrolabe => "astrolabe",
            ProductionProject.Hermitage => "hermitage",
            ProductionProject.IronWorks => "iron_works",
            ProductionProject.SmithsMansion => "smith_mansion",
            ProductionProject.TrainStation => "train_station",
            ProductionProject.UnitedNations => "united_nations",
            ProductionProject.ApolloProgram => "apollo_program",
            ProductionProject.ManhattanProject => "manhattan_project",
            ProductionProject.Internet => "internet",
            ProductionProject.LongevityVaccine => "longevity_vaccine",
            ProductionProject.MarsColony => "mars_colony",
            ProductionProject.WorldBank => "world_bank",
            ProductionProject.SpaceStation => "space_station",
            // Small Wonders
            ProductionProject.HeroicEpic => "heroic_epic",
            ProductionProject.MilitaryAcademy => "military_academy",
            ProductionProject.UniversityGrounds => "university_grounds",
            ProductionProject.BankOfAmerica => "bank_of_america",
            ProductionProject.ForbiddenPalace => "forbidden_palace",
            ProductionProject.MountRushmore => "mount_rushmore",
            ProductionProject.Pentagon => "pentagon",
            ProductionProject.IntelPentagon => "intel_pentagon",
            ProductionProject.WallStreet => "wall_street",
            ProductionProject.HolocaustMemorial => "holocaust_memorial",
            _ => null
        };

        if (buildingId != null)
        {
            Wonder? wonder = WonderRegistry.Get(buildingId);
            Building? building = BuildingRegistry.Get(buildingId);
            
            if (wonder != null && !IsWonderClaimed(buildingId))
            {
                ClaimWonder(wonder);
                city.Buildings.Add(wonder);
                System.Console.WriteLine($"[Production] {city.Name} has completed building a Wonder: {wonder.Name}!");
                wonder.OnCompleted(city, this);
            }
            else if (building != null)
            {
                city.Buildings.Add(building);
                System.Console.WriteLine($"[Production] {city.Name} has completed building a {building.Name}!");
                building.OnCompleted(city, this);
            }
        }
        else
        {
            string unitId = $"unit_{Guid.NewGuid().ToString().Substring(0, 8)}";
            UnitType type = project switch
            {
                ProductionProject.Settler => UnitType.Settler,
                ProductionProject.Worker => UnitType.Worker,
                ProductionProject.Warrior => UnitType.Warrior,
                ProductionProject.Archer => UnitType.Archer,
                _ => UnitType.Explorer
            };

            var unit = new Unit(unitId, type, city.X, city.Y, city.Faction, city.CivilizationId);
            Units.Add(unit);
            System.Console.WriteLine($"[Production] {city.Name} has completed building a {type}!");
        }

        city.CurrentProject = ProductionProject.None;
        city.CurrentProductionProgress = 0;
    }

    // Helper to check if city has access to a strategic resource
    public bool CityHasResourceAccess(City city, string resourceId)
    {
        int radius = city.VisionRange * 2;
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int tx = city.X + dx;
                int ty = city.Y + dy;
                if (Map.IsInBounds(tx, ty))
                {
                    var tile = Map.GetTile(tx, ty);
                    if (tile?.Resource?.Id == resourceId && tile.OwnerCityId == city.Id)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void CycleCityProduction(City city)
    {
        ProductionProject next = city.CurrentProject switch
        {
            ProductionProject.None => ProductionProject.Explorer,
            ProductionProject.Explorer => ProductionProject.Settler,
            ProductionProject.Settler => ProductionProject.Worker,
            ProductionProject.Worker => ProductionProject.Warrior,
            ProductionProject.Warrior => Research.IsResearched("bronze_working")
                ? ProductionProject.Archer
                : GetNextAvailableBuildingProject(city, ProductionProject.Warrior),
            ProductionProject.Archer => GetNextAvailableBuildingProject(city, ProductionProject.Archer),
            _ => GetNextAvailableBuildingProject(city, city.CurrentProject)
        };

        city.CurrentProject = next;
        city.CurrentProductionProgress = 0;
    }

    private ProductionProject GetNextAvailableBuildingProject(City city, ProductionProject current)
    {
        var allBuildings = new[]
        {
            (production: ProductionProject.Granary, buildingId: "granary"),
            (production: ProductionProject.Monument, buildingId: "monument"),
            (production: ProductionProject.Walls, buildingId: "walls"),
            (production: ProductionProject.Barracks, buildingId: "barracks"),
            (production: ProductionProject.Bank, buildingId: "bank"),
            (production: ProductionProject.Temple, buildingId: "temple"),
            (production: ProductionProject.Library, buildingId: "library"),
            (production: ProductionProject.Courthouse, buildingId: "courthouse"),
            (production: ProductionProject.Marketplace, buildingId: "marketplace"),
            (production: ProductionProject.Aqueduct, buildingId: "aqueduct"),
            (production: ProductionProject.Colosseum, buildingId: "colosseum"),
            (production: ProductionProject.Harbor, buildingId: "harbor"),
            (production: ProductionProject.Bank, buildingId: "bank"),
            (production: ProductionProject.Cathedral, buildingId: "cathedral"),
            (production: ProductionProject.University, buildingId: "university"),
            (production: ProductionProject.Hospital, buildingId: "hospital"),
            (production: ProductionProject.Factory, buildingId: "factory"),
            (production: ProductionProject.CoalPlant, buildingId: "coal_plant"),
            (production: ProductionProject.HydroPlant, buildingId: "hydro_plant"),
            (production: ProductionProject.SolarPlant, buildingId: "solar_plant"),
            (production: ProductionProject.NuclearPlant, buildingId: "nuclear_plant"),
            (production: ProductionProject.ManufacturingPlant, buildingId: "manufacturing_plant"),
            (production: ProductionProject.Airport, buildingId: "airport"),
            (production: ProductionProject.CivilDefense, buildingId: "civil_defense"),
            (production: ProductionProject.PoliceStation, buildingId: "police_station"),
            (production: ProductionProject.StockExchange, buildingId: "stock_exchange"),
            (production: ProductionProject.RecyclingCenter, buildingId: "recycling_center"),
            (production: ProductionProject.ResearchLab, buildingId: "research_lab"),
            (production: ProductionProject.SAMMissileBattery, buildingId: "sam_missile_battery"),
            (production: ProductionProject.OffshorePlatform, buildingId: "offshore_platform"),
            (production: ProductionProject.MassTransitSystem, buildingId: "mass_transit_system"),
            (production: ProductionProject.CommercialDock, buildingId: "commercial_dock"),
            (production: ProductionProject.CoastalFortress, buildingId: "coastal_fortress"),
            (production: ProductionProject.SSCockpit, buildingId: "ss_cockpit"),
            (production: ProductionProject.SSDockingBay, buildingId: "ss_docking_bay"),
            (production: ProductionProject.SSEngine, buildingId: "ss_engine"),
            (production: ProductionProject.SSExteriorCasing, buildingId: "ss_exterior_casing"),
            (production: ProductionProject.SSFuelCells, buildingId: "ss_fuel_cells"),
            (production: ProductionProject.SSLifeSupportSystem, buildingId: "ss_life_support_system"),
            (production: ProductionProject.SSPlanetaryPartyLounge, buildingId: "ss_planetary_party_lounge"),
            (production: ProductionProject.SSStasisChamber, buildingId: "ss_stasis_chamber"),
            (production: ProductionProject.SSStorageSupply, buildingId: "ss_storage_supply"),
            (production: ProductionProject.SSThrusters, buildingId: "ss_thrusters"),
            (production: ProductionProject.Palace, buildingId: "palace"),
            // Wonders
            (production: ProductionProject.Pyramids, buildingId: "pyramids"),
            (production: ProductionProject.HangingGardens, buildingId: "hanging_gardens"),
            (production: ProductionProject.Colossus, buildingId: "colossus"),
            (production: ProductionProject.GreatWall, buildingId: "great_wall"),
            (production: ProductionProject.StatueOfZeus, buildingId: "statue_of_zeus"),
            (production: ProductionProject.Oracle, buildingId: "oracle"),
            (production: ProductionProject.KnightsHall, buildingId: "knights_hall"),
            (production: ProductionProject.SovereignBath, buildingId: "sovereign_bath"),
            (production: ProductionProject.LeonardoWorkshop, buildingId: "leonardo_workshop"),
            (production: ProductionProject.ShakespearesTheatre, buildingId: "shakespeares_theatre"),
            (production: ProductionProject.SunTzusWarAcademy, buildingId: "sun_tzu_war_academy"),
            (production: ProductionProject.CureForCancer, buildingId: "cure_for_cancer"),
            (production: ProductionProject.SistineChapel, buildingId: "sistine_chapel"),
            (production: ProductionProject.TajMahal, buildingId: "taj_mahal"),
            (production: ProductionProject.Astrolabe, buildingId: "astrolabe"),
            (production: ProductionProject.Hermitage, buildingId: "hermitage"),
            (production: ProductionProject.IronWorks, buildingId: "iron_works"),
            (production: ProductionProject.SmithsMansion, buildingId: "smith_mansion"),
            (production: ProductionProject.TrainStation, buildingId: "train_station"),
            (production: ProductionProject.UnitedNations, buildingId: "united_nations"),
            (production: ProductionProject.ApolloProgram, buildingId: "apollo_program"),
            (production: ProductionProject.ManhattanProject, buildingId: "manhattan_project"),
            (production: ProductionProject.Internet, buildingId: "internet"),
            (production: ProductionProject.LongevityVaccine, buildingId: "longevity_vaccine"),
            (production: ProductionProject.MarsColony, buildingId: "mars_colony"),
            (production: ProductionProject.WorldBank, buildingId: "world_bank"),
            (production: ProductionProject.SpaceStation, buildingId: "space_station"),
            // Small Wonders
            (production: ProductionProject.HeroicEpic, buildingId: "heroic_epic"),
            (production: ProductionProject.MilitaryAcademy, buildingId: "military_academy"),
            (production: ProductionProject.UniversityGrounds, buildingId: "university_grounds"),
            (production: ProductionProject.BankOfAmerica, buildingId: "bank_of_america"),
            (production: ProductionProject.ForbiddenPalace, buildingId: "forbidden_palace"),
            (production: ProductionProject.MountRushmore, buildingId: "mount_rushmore"),
            (production: ProductionProject.Pentagon, buildingId: "pentagon"),
            (production: ProductionProject.IntelPentagon, buildingId: "intel_pentagon"),
            (production: ProductionProject.WallStreet, buildingId: "wall_street"),
            (production: ProductionProject.HolocaustMemorial, buildingId: "holocaust_memorial"),
        };

        int startIndex = 0;
        for (int i = 0; i < allBuildings.Length; i++)
        {
            if (allBuildings[i].production == current)
            {
                startIndex = i + 1;
                break;
            }
        }

        for (int offset = 0; offset < allBuildings.Length; offset++)
        {
            int idx = (startIndex + offset) % allBuildings.Length;
            var (production, buildingId) = allBuildings[idx];

            var building = BuildingRegistry.Get(buildingId);
            var wonder = WonderRegistry.Get(buildingId);

            if (building == null && wonder == null) continue;

            var targetBuilding = building ?? wonder;

            if (targetBuilding!.RequiredTechId != null && !Research.IsResearched(targetBuilding.RequiredTechId))
                continue;

            if (targetBuilding.RequiredResourceId != null && !CityHasResourceAccess(city, targetBuilding.RequiredResourceId))
                continue;

            // For Wonders, check if claimed globally
            if (wonder != null && IsWonderClaimed(buildingId))
                continue;

            // For regular buildings, check if city already has it
            if (building != null && city.Buildings.Any(b => b.Id == buildingId))
                continue;

            return production;
        }

        return ProductionProject.None;
    }
}
