using System;
using System.Collections.Generic;
using System.Linq;
using CivGame.Core.Pathfinding;
using CivGame.Core.AI;

namespace CivGame.Core;

public class GameSimulation
    {
        public GameMap Map { get; }
        private Pathfinder _pathfinder;
        public Pathfinder Pathfinder => _pathfinder ??= new Pathfinder(Map, this);

        public List<(int X, int Y)>? FindPath(Unit unit, int startX, int startY, int targetX, int targetY, bool ignoreUnits = false)
        {
            return Pathfinder.FindPath(unit, startX, startY, targetX, targetY, ignoreUnits);
        }

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
        public Dictionary<Faction, HashSet<string>> CompletedSmallWondersByFaction { get; } = new();
        public HashSet<string> CompletedSmallWonderIds => GetCompletedSmallWonders(Faction.Player);
        public HashSet<Faction> FactionsWithVictoriousUnit { get; } = new();

        public static bool DebugIgnorePrerequisites { get; set; }

        public HashSet<string> GetCompletedSmallWonders(Faction faction)
        {
            if (!CompletedSmallWondersByFaction.TryGetValue(faction, out var set))
            {
                set = new HashSet<string>();
                CompletedSmallWondersByFaction[faction] = set;
            }
            return set;
        }

    public string PlayerCivId { get; set; } = "rome";
    public string AiCivId { get; set; } = "babylon";
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Regent;
    public DifficultySettings DifficultyConfig => DifficultySettings.Get(Difficulty);
    public Civilization PlayerCiv => CivilizationRegistry.Get(PlayerCivId)!;
    public Civilization AiCiv => CivilizationRegistry.Get(AiCivId)!;

    public HashSet<string> AiResearchedTechs { get; } = new();
    public string? AiCurrentResearchId { get; set; }
    public int AiScienceProgress { get; set; }

    // Space Race tracking – parts built by the player
    public HashSet<ProductionProject> BuiltSpaceshipParts { get; } = new();
    public HashSet<ProductionProject> AiBuiltSpaceshipParts { get; } = new();

    private static readonly HashSet<ProductionProject> AllSpaceshipParts = new()
    {
        ProductionProject.SSCockpit,
        ProductionProject.SSDockingBay,
        ProductionProject.SSEngine,
        ProductionProject.SSExteriorCasing,
        ProductionProject.SSFuelCells,
        ProductionProject.SSLifeSupportSystem,
        ProductionProject.SSPlanetaryPartyLounge,
        ProductionProject.SSStasisChamber,
        ProductionProject.SSStorageSupply,
        ProductionProject.SSThrusters,
    };

    // Cultural Victory threshold (total culture in a single city)
    public const int CulturalVictoryThreshold = 50_000;


    private static readonly string[] BasicAncientTechs = {
        "bronze_working", "pottery", "alphabet", "ceremonial_burial",
        "warrior_code", "masonry", "the_wheel"
    };

    public bool IsTechResearched(Faction faction, string techId)
    {
        if (string.IsNullOrEmpty(techId)) return true;
        if (faction == Faction.Player)
        {
            return Research.IsResearched(techId);
        }
        else
        {
            return Research.IsResearched(techId) || AiResearchedTechs.Contains(techId);
        }
    }

    private int _cityCounter = 0;
    private static readonly string[] CityNames = { "Rome", "Sparta", "Athens", "Carthage", "Constantinople", "Alexandria", "Babylon", "Thebes" };
    private int _aiCityCounter = 0;
    private static readonly string[] AiCityNames = { "Kyoto", "Berlin", "Paris", "London", "Madrid", "Washington", "Beijing", "Delhi" };

    public GameSimulation(GameMap map)
    {
        DebugIgnorePrerequisites = Environment.GetEnvironmentVariable("CIV_DEBUG_BUILD") == "1";
        if (DebugIgnorePrerequisites)
            Console.WriteLine("[DEBUG] CIV_DEBUG_BUILD=1 — All wonder/building prerequisites ignored!");

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

    public void InitializeStartingTechnologies()
    {
        // 1. Give Player their starting technologies based on Civilization registry
        if (PlayerCiv?.StartingTechIds != null)
        {
            foreach (var techId in PlayerCiv.StartingTechIds)
            {
                Research.ResearchedTechIds.Add(techId);
            }
        }

        // 2. Give AI their starting technologies based on Civilization registry
        if (AiCiv?.StartingTechIds != null)
        {
            foreach (var techId in AiCiv.StartingTechIds)
            {
                AiResearchedTechs.Add(techId);
            }
        }

        // 3. Give AI difficulty-specific starting technologies
        int extraTechCount = DifficultyConfig.AiExtraStartingTechs;
        if (extraTechCount > 0)
        {
            var rand = new Random(42);
            // Get ancient era techs that are not already researched by AI
            var availableTechs = BasicAncientTechs
                .Where(t => !AiResearchedTechs.Contains(t))
                .ToList();

            int techsGiven = 0;
            while (techsGiven < extraTechCount && availableTechs.Count > 0)
            {
                int index = rand.Next(availableTechs.Count);
                string selectedTech = availableTechs[index];
                AiResearchedTechs.Add(selectedTech);
                availableTechs.RemoveAt(index);
                techsGiven++;
                Console.WriteLine($"[Difficulty: {Difficulty}] AI Rival receives free starting technology: {selectedTech}!");
            }
        }
    }

    public void SetupInitialUnits()
    {
        InitializeStartingTechnologies();

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

        // Expansionist Trait: Spawn extra starting Explorer
        bool playerIsExpansionist = PlayerCiv.Trait1 == CivTrait.Expansionist || PlayerCiv.Trait2 == CivTrait.Expansionist;
        if (playerIsExpansionist)
        {
            Units.Add(new Unit("explorer_extra_1", UnitType.Explorer, startX, startY, Faction.Player, PlayerCivId));
            Console.WriteLine("[Expansionist] Player receives an extra starting Explorer!");
        }
        
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

        // If the calculated position is too close to the player (e.g. because px/py is in the exact center), offset it!
        int distance = Math.Abs(aiStartX - px) + Math.Abs(aiStartY - py);
        if (distance < Map.Width / 3)
        {
            // Shift AI starting search position to another quadrant
            aiStartX = (px + Map.Width / 3) % Map.Width;
            aiStartY = (py + Map.Height / 3) % Map.Height;
        }

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

        // Expansionist Trait: Spawn extra starting Explorer for AI
        bool aiIsExpansionist = AiCiv.Trait1 == CivTrait.Expansionist || AiCiv.Trait2 == CivTrait.Expansionist;
        if (aiIsExpansionist)
        {
            string extraAiExplorerId = $"ai_explorer_extra_{Guid.NewGuid().ToString().Substring(0, 8)}";
            Units.Add(new Unit(extraAiExplorerId, UnitType.Explorer, aiStartX, aiStartY, Faction.AiRival, AiCivId));
            Console.WriteLine("[Expansionist] AI Rival receives an extra starting Explorer!");
        }

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

        // Difficulty: AI receives extra starting Warrior units at higher difficulty levels
        int extraAiUnits = DifficultyConfig.AiExtraStartingUnits;
        for (int i = 0; i < extraAiUnits; i++)
        {
            string extraWarriorId = $"ai_warrior_extra_{i}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            Units.Add(new Unit(extraWarriorId, UnitType.Warrior, aiStartX, aiStartY, Faction.AiRival, AiCivId));
        }
        if (extraAiUnits > 0)
        {
            Console.WriteLine($"[Difficulty: {Difficulty}] AI Rival receives {extraAiUnits} extra starting Warrior(s)!");
        }

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
            var tile = Map.GetTile(unit.X, unit.Y);
            if (tile != null && tile.Improvement != null && tile.Improvement.Id == "outpost")
            {
                r += 2;
            }

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

        var city = new City($"city_{Guid.NewGuid().ToString().Substring(0, 8)}", name, unit.X, unit.Y, TurnNumber, unit.Faction, unit.CivilizationId);
        
        // If this is the first city for this faction, it becomes the Capital with a Palace
        bool isFirstCity = !Cities.Any(c => c.Faction == unit.Faction);
        if (isFirstCity)
        {
            city.IsCapital = true;
            city.Buildings.Add(new Palace());
            Console.WriteLine($"[Capital] {city.Name} has been established as the capital!");
        }

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

    public void ClaimWonder(Wonder wonder, Faction faction)
    {
        if (wonder.IsNationalWonder)
        {
            GetCompletedSmallWonders(faction).Add(wonder.Id);
            Console.WriteLine($"[Wonder] Small Wonder {wonder.Name} claimed by {faction}!");
        }
        else
        {
            CompletedWonderIds.Add(wonder.Id);
            Console.WriteLine($"[Wonder] Wonder {wonder.Name} has been claimed globally - no other city can build it!");
            wonder.OnBuiltGlobally(this);
        }
    }

    public bool IsWonderClaimedByFaction(Faction faction, string wonderId)
    {
        Wonder? wonder = WonderRegistry.Get(wonderId);
        if (wonder == null) return false;

        if (wonder.IsNationalWonder)
        {
            return GetCompletedSmallWonders(faction).Contains(wonderId);
        }
        else
        {
            return CompletedWonderIds.Contains(wonderId);
        }
    }

    public bool AreSmallWonderPrerequisitesMet(City city, string wonderId)
    {
        if (DebugIgnorePrerequisites) return true;

        switch (wonderId)
        {
            case "forbidden_palace":
                // Requires at least 4 cities owned by this faction
                int cityCount = Cities.Count(c => c.Faction == city.Faction);
                return cityCount >= 4;

            case "heroic_epic":
                // Requires a Victorious Unit for this faction
                return FactionsWithVictoriousUnit.Contains(city.Faction);

            case "military_academy":
                // Requires a Victorious Unit for this faction
                return FactionsWithVictoriousUnit.Contains(city.Faction);

            case "pentagon":
                // Requires at least 3 military units of strength >= 2 owned by this faction
                int milUnits = Units.Count(u => u.Faction == city.Faction && u.AttackStrength >= 2);
                return milUnits >= 3;

            case "wall_street":
                // Requires at least 3 Banks in this faction's cities
                int bankCount = Cities.Where(c => c.Faction == city.Faction).Sum(c => c.Buildings.Count(b => b.Id == "bank"));
                return bankCount >= 3;

            case "battlefield_medicine":
                // Requires at least 3 Hospitals in this faction's cities
                int hospitalCount = Cities.Where(c => c.Faction == city.Faction).Sum(c => c.Buildings.Count(b => b.Id == "hospital"));
                return hospitalCount >= 3;

            case "iron_works":
                // Requires BOTH Iron and Coal resources inside the city's 3x3 territory/workable tiles (access)
                return CityHasResourceAccess(city, "iron") && CityHasResourceAccess(city, "coal");

            default:
                return true;
        }
    }

    public bool IsWonderAvailable(Wonder wonder)
    {
        if (IsWonderClaimedByFaction(Faction.Player, wonder.Id)) return false;

        if (!DebugIgnorePrerequisites)
        {
            if (wonder.RequiredTechId != null && !Research.IsResearched(wonder.RequiredTechId))
                return false;

            if (wonder.RequiredResourceId != null)
            {
                var playerCityWithAccess = Cities.FirstOrDefault(c => c.Faction == Faction.Player &&
                    CityHasResourceAccess(c, wonder.RequiredResourceId));
                if (playerCityWithAccess == null) return false;
            }

            if (wonder.IsNationalWonder)
            {
                var firstPlayerCity = Cities.FirstOrDefault(c => c.Faction == Faction.Player);
                if (firstPlayerCity != null && !AreSmallWonderPrerequisitesMet(firstPlayerCity, wonder.Id))
                    return false;
            }
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
        float moveCost = GetTileMovementCostForUnit(tile, unit);
        unit.MoveTo(targetX, targetY, moveCost);
        
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

    public float GetTileMovementCostForUnit(TileData tile, Unit unit)
    {
        float cost = tile.MovementCost;
        if (tile.Improvement != null && tile.Improvement.Id == "barricade" && tile.OwnerCityId != null)
        {
            var city = Cities.Find(c => c.Id == tile.OwnerCityId);
            if (city != null && city.Faction != unit.Faction)
            {
                cost += 1.0f; // Barricades slow down enemies
            }
        }
        return cost;
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
        
        // Militaristic trait bonus: +10% attack strength
        bool attackerIsMilitaristic = attacker.Civilization != null && 
            (attacker.Civilization.Trait1 == CivTrait.Militaristic || attacker.Civilization.Trait2 == CivTrait.Militaristic);
        if (attackerIsMilitaristic)
        {
            attStrength *= 1.10f;
            Console.WriteLine($"[Militaristic] {attacker.Faction}'s {attacker.Type} receives +10% attack strength bonus!");
        }

        // Heroic Epic effect: Attacker gets +25% attack strength globally if their faction completed it
        bool attackerHasHeroicEpic = Cities.Any(c => c.Faction == attacker.Faction && c.Buildings.Any(b => b.Id == "heroic_epic"));
        if (attackerHasHeroicEpic)
        {
            attStrength *= 1.25f;
            Console.WriteLine($"[Heroic Epic] {attacker.Faction}'s {attacker.Type} receives +25% attack bonus!");
        }

        float defStrength = Math.Max(0.5f, defender.DefenseStrength);

        // Militaristic trait bonus: +10% defense strength
        bool defenderIsMilitaristic = defender.Civilization != null && 
            (defender.Civilization.Trait1 == CivTrait.Militaristic || defender.Civilization.Trait2 == CivTrait.Militaristic);
        if (defenderIsMilitaristic)
        {
            defStrength *= 1.10f;
            Console.WriteLine($"[Militaristic] {defender.Faction}'s {defender.Type} receives +10% defense strength bonus!");
        }

        if (defender.IsFortified)
        {
            defStrength *= 1.25f; // +25% defense bonus when fortified
        }

        // Apply terrain defense bonus
        var defTile = Map.GetTile(defender.X, defender.Y);
        if (defTile != null)
        {
            float terrainBonus = defTile.Terrain.DefenseBonusPercent / 100.0f;
            defStrength *= (1.0f + terrainBonus);
            Console.WriteLine($"[Combat] {defender.Type} receives +{defTile.Terrain.DefenseBonusPercent}% defense bonus from terrain ({defTile.Terrain.Name})!");

            if (defTile.Improvement != null)
            {
                if (defTile.Improvement.Id == "fortress")
                {
                    defStrength *= 1.50f; // +50% defense from Fortress
                    Console.WriteLine($"[Combat] {defender.Type} receives +50% defense bonus from Fortress!");
                }
                else if (defTile.Improvement.Id == "barricade")
                {
                    defStrength *= 2.00f; // +100% defense from Barricade
                    Console.WriteLine($"[Combat] {defender.Type} receives +100% defense bonus from Barricade!");
                }
            }
        }

        // The Pentagon effect: Defender gets +25% defense strength globally if their faction completed it
        bool defenderHasPentagon = Cities.Any(c => c.Faction == defender.Faction && c.Buildings.Any(b => b.Id == "pentagon"));
        if (defenderHasPentagon)
        {
            defStrength *= 1.25f;
            Console.WriteLine($"[The Pentagon] {defender.Faction}'s {defender.Type} receives +25% defense bonus!");
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

            // Attacker advances onto the defender's tile upon victory and is registered as victorious
            if (!attackerDied)
            {
                var tile = Map.GetTile(defender.X, defender.Y)!;
                float moveCost = GetTileMovementCostForUnit(tile, attacker);
                attacker.MoveTo(defender.X, defender.Y, moveCost);
                Console.WriteLine($"[Combat] {attacker.Type} wins and advances to ({defender.X}, {defender.Y})!");
                
                FactionsWithVictoriousUnit.Add(attacker.Faction);
            }
        }

        if (attackerDied)
        {
            Console.WriteLine($"[Combat] {attacker.Type} has been destroyed in battle!");
            Units.Remove(attacker);
            
            // Defender wins and is registered as victorious
            if (!defenderDied)
            {
                FactionsWithVictoriousUnit.Add(defender.Faction);
            }
        }

        // Combat consumes all remaining movement points for the attacker
        if (!attackerDied)
        {
            attacker.RemainingMovement = 0;
        }
    }

    private void RelocateCapital(Faction faction)
    {
        var remainingCities = Cities.Where(c => c.Faction == faction).OrderBy(c => c.FoundedYear).ToList();
        if (remainingCities.Count == 0) return;

        var newCapital = remainingCities[0];
        newCapital.IsCapital = true;
        if (!newCapital.Buildings.Any(b => b is Palace))
        {
            newCapital.Buildings.Add(new Palace());
        }
        Console.WriteLine($"[Capital] {newCapital.Name} has become the new capital of {faction}!");
    }

    private int GetDistanceToNearestCapital(City city)
    {
        if (city.IsCapital) return 0;

        int minDist = int.MaxValue;

        // Distance to main capital
        var capital = Cities.FirstOrDefault(c => c.Faction == city.Faction && c.IsCapital);
        if (capital != null)
        {
            int dx = Math.Abs(city.X - capital.X);
            int dy = Math.Abs(city.Y - capital.Y);
            minDist = Math.Min(minDist, Math.Max(dx, dy)); // Chebyshev
        }

        // Distance to Forbidden Palace cities (secondary capitals for corruption)
        foreach (var fpCity in Cities.Where(c => c.Faction == city.Faction && c.Buildings.Any(b => b.Id == "forbidden_palace")))
        {
            int dx = Math.Abs(city.X - fpCity.X);
            int dy = Math.Abs(city.Y - fpCity.Y);
            minDist = Math.Min(minDist, Math.Max(dx, dy));
        }

        return minDist == int.MaxValue ? 0 : minDist;
    }

    public bool HasFreshWaterAccess(City city)
    {
        // Check adjacent tiles (including city center tile itself)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int tx = city.X + dx;
                int ty = city.Y + dy;
                if (Map.IsInBounds(tx, ty))
                {
                    var tile = Map.GetTile(tx, ty);
                    if (tile != null && (tile.Terrain.Id == "coast" || tile.Terrain.Id == "floodplains"))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
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
            var oldFaction = city.Faction;
            bool wasCapital = city.IsCapital;

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

                if (wasCapital)
                {
                    RelocateCapital(oldFaction);
                }
            }
            else
            {
                city.Faction = unit.Faction;
                city.IsCapital = false; // Captured city is no longer the old faction's capital
                // Remove Palace from captured city (old faction gets a new one)
                city.Buildings.RemoveAll(b => b is Palace);

                // Clear city production progress upon capture
                city.CurrentProject = ProductionProject.None;
                city.StoredProduction = 0;
                city.CurrentProductionProgress = 0;

                // Reduce population by 1 (conquest penalty)
                city.Population = Math.Max(1, city.Population - 1);

                Console.WriteLine($"[City Captured] Faction {unit.Faction} has captured the city of {city.Name} from {oldFaction} at ({x}, {y})!");

                if (wasCapital)
                {
                    RelocateCapital(oldFaction);
                }
            }
        }
    }

    public void CheckGameEndConditions()
    {
        if (EndState != GameEndState.None) return;

        // --- 1. Conquest Victory / Defeat ---
        // A faction is "alive" if it has at least one city or a settler.
        bool playerAlive = Cities.Any(c => c.Faction == Faction.Player) ||
                           Units.Any(u => u.Faction == Faction.Player && u.Type == UnitType.Settler);
        bool aiAlive     = Cities.Any(c => c.Faction == Faction.AiRival) ||
                           Units.Any(u => u.Faction == Faction.AiRival && u.Type == UnitType.Settler);

        if (!playerAlive && aiAlive)
        {
            EndState = GameEndState.DefeatConquest;
            Console.WriteLine("[Game End] DEFEAT! All your cities and settlers have been destroyed.");
            return;
        }
        if (!aiAlive && playerAlive)
        {
            EndState = GameEndState.VictoryConquest;
            Console.WriteLine("[Game End] VICTORY! You have eliminated all rival civilizations!");
            return;
        }

        // --- 2. Space Race Victory ---
        // Player must have built the Apollo Program and all 10 spaceship parts.
        bool playerApollo = Cities.Any(c => c.Faction == Faction.Player &&
                                            c.Buildings.Any(b => b.Id == "apollo_program"));
        if (playerApollo && AllSpaceshipParts.IsSubsetOf(BuiltSpaceshipParts))
        {
            EndState = GameEndState.VictorySpaceRace;
            Console.WriteLine("[Game End] VICTORY! Your spaceship has launched to Alpha Centauri!");
            return;
        }
        // AI Space Race check
        bool aiApollo = Cities.Any(c => c.Faction == Faction.AiRival &&
                                       c.Buildings.Any(b => b.Id == "apollo_program"));
        if (aiApollo && AllSpaceshipParts.IsSubsetOf(AiBuiltSpaceshipParts))
        {
            EndState = GameEndState.DefeatSpaceRace;
            Console.WriteLine("[Game End] DEFEAT! The AI Rival has launched their spaceship to Alpha Centauri!");
            return;
        }

        // --- 3. Domination Victory ---
        // Player controls 2/3 of all land tiles AND 2/3 of total population.
        int totalLandTiles   = 0;
        int playerLandTiles  = 0;
        for (int x = 0; x < Map.Width; x++)
        {
            for (int y = 0; y < Map.Height; y++)
            {
                var tile = Map.GetTile(x, y);
                if (tile == null || tile.IsOcean) continue;
                totalLandTiles++;
                if (!string.IsNullOrEmpty(tile.OwnerCityId))
                {
                    var ownerCity = Cities.Find(c => c.Id == tile.OwnerCityId);
                    if (ownerCity?.Faction == Faction.Player) playerLandTiles++;
                }
            }
        }

        int totalPop  = Cities.Sum(c => c.Population);
        int playerPop = Cities.Where(c => c.Faction == Faction.Player).Sum(c => c.Population);
        int aiPop     = totalPop - playerPop;

        int aiLandTiles = 0;
        for (int x = 0; x < Map.Width; x++)
        {
            for (int y = 0; y < Map.Height; y++)
            {
                var tile = Map.GetTile(x, y);
                if (tile == null || tile.IsOcean) continue;
                if (!string.IsNullOrEmpty(tile.OwnerCityId))
                {
                    var ownerCity = Cities.Find(c => c.Id == tile.OwnerCityId);
                    if (ownerCity?.Faction == Faction.AiRival) aiLandTiles++;
                }
            }
        }

        bool playerLandDomination = totalLandTiles > 0 && playerLandTiles * 3 >= totalLandTiles * 2;
        bool playerPopDomination  = totalPop > 0        && playerPop  * 3 >= totalPop * 2;
        bool aiLandDomination     = totalLandTiles > 0 && aiLandTiles * 3 >= totalLandTiles * 2;
        bool aiPopDomination      = totalPop > 0        && aiPop     * 3 >= totalPop * 2;

        if (playerLandDomination && playerPopDomination)
        {
            EndState = GameEndState.VictoryDomination;
            Console.WriteLine($"[Game End] VICTORY! You control {playerLandTiles}/{totalLandTiles} land tiles and {playerPop}/{totalPop} population — Domination!");
            return;
        }
        if (aiLandDomination && aiPopDomination)
        {
            EndState = GameEndState.DefeatDomination;
            Console.WriteLine($"[Game End] DEFEAT! The AI Rival controls {aiLandTiles}/{totalLandTiles} land tiles and {aiPop}/{totalPop} population — Domination!");
            return;
        }

        // --- 4. Cultural Victory ---
        // Any single city has accumulated 50,000+ culture.
        var playerCulturalCity = Cities.FirstOrDefault(c => c.Faction == Faction.Player &&
                                                            c.AccumulatedCulture >= CulturalVictoryThreshold);
        if (playerCulturalCity != null)
        {
            EndState = GameEndState.VictoryCultural;
            Console.WriteLine($"[Game End] VICTORY! {playerCulturalCity.Name} has achieved legendary cultural status with {playerCulturalCity.AccumulatedCulture} culture!");
            return;
        }
        var aiCulturalCity = Cities.FirstOrDefault(c => c.Faction == Faction.AiRival &&
                                                        c.AccumulatedCulture >= CulturalVictoryThreshold);
        if (aiCulturalCity != null)
        {
            EndState = GameEndState.DefeatCultural;
            Console.WriteLine($"[Game End] DEFEAT! The AI's {aiCulturalCity.Name} has achieved legendary cultural status with {aiCulturalCity.AccumulatedCulture} culture!");
            return;
        }

        // --- 5. Diplomatic Victory (checked when UN is built, see SpawnProjectResult) ---
        // Handled inline in SpawnProjectResult to fire immediately on UN completion.

        // --- 6. Histograph / Score Victory (turn limit) ---
        if (TurnNumber >= MaxTurnLimit)
        {
            int playerScore = CalculateScore(Faction.Player);
            int aiScore     = CalculateScore(Faction.AiRival);

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
            techPoints = AiResearchedTechs.Count * 10;
        }

        return cityPoints + popPoints + unitPoints + tilePoints + techPoints;
    }

    public void LoadSimulationState(
        int turnNumber, 
        bool isAtWarWithAi, 
        GameEndState endState,
        string playerCivId,
        string aiCivId,
        int playerTreasury,
        int playerTaxRate,
        int lastTurnIncome,
        int lastTurnMaintenance,
        int lastTurnScience,
        int lastTurnNetGold,
        List<UnitSaveDto> unitDtos, 
        List<CitySaveDto> cityDtos, 
        List<TileSaveDto> tileDtos, 
        List<string> researchedIds, 
        string? activeTechId, 
        int progress, 
        int lastTurnScienceGenerated,
        List<(int X, int Y)> camps,
        List<string>? aiResearchedTechs = null)
    {
        TurnNumber = turnNumber;
        IsAtWarWithAi = isAtWarWithAi;
        EndState = endState;
        PlayerCivId = playerCivId;
        AiCivId = aiCivId;
        PlayerTreasury = playerTreasury;
        PlayerTaxRate = playerTaxRate;
        LastTurnIncome = lastTurnIncome;
        LastTurnMaintenance = lastTurnMaintenance;
        LastTurnScience = lastTurnScience;
        LastTurnNetGold = lastTurnNetGold;

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
                tile.HasRoad = dto.HasRoad;
                tile.HasRailroad = dto.HasRailroad;
                tile.IsPolluted = dto.IsPolluted;
                
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
                        "Fortress" => new Fortress(),
                        "Barricade" => new Barricade(),
                        "Outpost" => new Outpost(),
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
                    "Road" => new RoadBuild(),
                    "Railroad" => new RailroadBuild(),
                    "Fortress" => new Fortress(),
                    "Barricade" => new Barricade(),
                    "Outpost" => new Outpost(),
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
            var city = new City(dto.Id, dto.Name, dto.X, dto.Y, dto.FoundedYear, dto.Faction, dto.CivilizationId);
            city.StoredFood = dto.StoredFood;
            city.StoredProduction = dto.StoredProduction;
            city.StoredCommerce = dto.StoredCommerce;
            city.Population = dto.Population;
            city.LastTurnNetFood = dto.LastTurnNetFood;
            city.IsCapital = dto.IsCapital;
            city.AccumulatedCulture = dto.AccumulatedCulture;
            city.CurrentProject = dto.CurrentProject;
            city.CurrentProductionProgress = dto.CurrentProductionProgress;
            if (dto.ProductionQueue != null)
            {
                city.ProductionQueue.Clear();
                city.ProductionQueue.AddRange(dto.ProductionQueue);
            }
            city.UpdateCityType();

            // Reconstruct Worked Tiles
            city.WorkedTiles.Clear();
            if (dto.WorkedTiles != null)
            {
                foreach (var wtStr in dto.WorkedTiles)
                {
                    var parts = wtStr.Split(',');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int wx) && int.TryParse(parts[1], out int wy))
                    {
                        city.WorkedTiles.Add((wx, wy));
                    }
                }
            }
            
            // Reconstruct Buildings
            city.Buildings.Clear();
            foreach (var bName in dto.BuildingNames)
            {
                Building? b = bName switch
                {
                    "Monument" => new Monument(),
                    "Granary" => new Granary(),
                    "Aqueduct" => new Aqueduct(),
                    "Hospital" => new Hospital(),
                    "Palace" => new Palace(),
                    "SS Cockpit" => new SSCockpit(),
                    "SS Docking Bay" => new SSDockingBay(),
                    "SS Engine" => new SSEngine(),
                    "SS Exterior Casing" => new SSExteriorCasing(),
                    "SS Fuel Cells" => new SSFuelCells(),
                    "SS Life Support System" => new SSLifeSupportSystem(),
                    "SS Planetary Party Lounge" => new SSPlanetaryPartyLounge(),
                    "SS Stasis Chamber" => new SSStasisChamber(),
                    "SS Storage/Supply" => new SSStorageSupply(),
                    "SS Thrusters" => new SSThrusters(),
                    _ => null
                };

                if (b == null)
                {
                    b = BuildingRegistry.All.Values.FirstOrDefault(x => string.Equals(x.Name, bName, StringComparison.OrdinalIgnoreCase))
                        ?? WonderRegistry.All.Values.FirstOrDefault(x => string.Equals(x.Name, bName, StringComparison.OrdinalIgnoreCase)) as Building;
                }

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
        Research.LoadResearchState(researchedIds, activeTechId, progress, lastTurnScienceGenerated);

        // Reconstruct AI Research
        AiResearchedTechs.Clear();
        if (aiResearchedTechs != null)
        {
            foreach (var techId in aiResearchedTechs)
            {
                AiResearchedTechs.Add(techId);
            }
        }
        else
        {
            // Fallback for older saves
            InitializeStartingTechnologies();
        }

        // 6. Reconstruct Wonder Claim and Victory State from board
        CompletedWonderIds.Clear();
        CompletedSmallWondersByFaction.Clear();
        FactionsWithVictoriousUnit.Clear();
        
        foreach (var city in Cities)
        {
            foreach (var building in city.Buildings)
            {
                if (building is Wonder wonder)
                {
                    if (wonder.IsNationalWonder)
                    {
                        var set = GetCompletedSmallWonders(city.Faction);
                        set.Add(wonder.Id);
                    }
                    else
                    {
                        CompletedWonderIds.Add(wonder.Id);
                    }
                }
            }
        }
        
        // Mark factions as victorious if they built Heroic Epic or Military Academy
        foreach (var city in Cities)
        {
            if (city.Buildings.Any(b => b.Id == "heroic_epic" || b.Id == "military_academy"))
            {
                FactionsWithVictoriousUnit.Add(city.Faction);
            }
        }
    }

    public HashSet<(int X, int Y)> GetReachableTiles(Unit unit)
    {
        var reachable = new HashSet<(int, int)>();
        if (unit.RemainingMovement <= 0.0f) return reachable;

        // BFS: each state tracks (x, y, mpRemaining after entering that tile)
        var queue = new Queue<(int X, int Y, float Mp)>();
        var bestMp = new Dictionary<(int, int), float>();

        queue.Enqueue((unit.X, unit.Y, unit.RemainingMovement));
        bestMp[(unit.X, unit.Y)] = unit.RemainingMovement;

        int[] dxs = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dys = { -1, -1, -1, 0, 0, 1, 1, 1 };

        while (queue.Count > 0)
        {
            var (cx, cy, mp) = queue.Dequeue();
            if (mp <= 0.0f) continue;

            for (int i = 0; i < 8; i++)
            {
                int nx = cx + dxs[i];
                int ny = cy + dys[i];

                if (!Map.IsInBounds(nx, ny)) continue;
                var tile = Map.GetTile(nx, ny);
                if (tile == null || tile.Terrain.Id == "ocean") continue;

                float moveCost = GetTileMovementCostForUnit(tile, unit);
                float remaining = Math.Max(0.0f, mp - moveCost);
                reachable.Add((nx, ny));

                if (!bestMp.TryGetValue((nx, ny), out float prev) || remaining > prev)
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

        // 2.5 Process AI Research
        ProcessAiResearch();

        // 3. Advance Worker Construction
        ProcessWorkerConstruction();

        // 3.5 Apply Battlefield Medicine healing effect
        HealWoundedUnits();

        // 3.6 Process Volcanic Eruptions (0.35% chance per turn per volcano)
        VolcanoSystem.ProcessEruptions(this);

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

    private void HealWoundedUnits()
    {
        // For each faction, if they have Battlefield Medicine, heal their wounded units by +15 HP
        foreach (Faction faction in Enum.GetValues<Faction>())
        {
            bool hasBattlefieldMedicine = Cities.Any(c => c.Faction == faction && c.Buildings.Any(b => b.Id == "battlefield_medicine"));
            if (hasBattlefieldMedicine)
            {
                foreach (var unit in Units.Where(u => u.Faction == faction && u.Health < u.MaxHealth))
                {
                    int originalHealth = unit.Health;
                    unit.Health = Math.Min(unit.MaxHealth, unit.Health + 15);
                    Console.WriteLine($"[Battlefield Medicine] Healed {unit.Type} at ({unit.X}, {unit.Y}) from {originalHealth} to {unit.Health} HP.");
                }
            }
        }
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
                        else if (improvement is RailroadBuild)
                        {
                            tile.HasRailroad = true;
                            Console.WriteLine($"[Construction Completed] Railroad built at ({unit.X}, {unit.Y})!");
                        }
                        else if (improvement is CleanPollution)
                        {
                            tile.IsPolluted = false;
                            Console.WriteLine($"[Construction Completed] Pollution cleaned up at ({unit.X}, {unit.Y})!");
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

    private void ProcessAiResearch()
    {
        // Calculate AI science per turn from AI city commerce
        int aiCommerce = Cities
            .Where(c => c.Faction == Faction.AiRival)
            .Sum(c => c.StoredCommerce);

        if (aiCommerce <= 0) return;

        // Pick a research target if none selected
        if (AiCurrentResearchId == null || AiResearchedTechs.Contains(AiCurrentResearchId))
        {
            var available = Research.AllTechnologies
                .Where(t => !AiResearchedTechs.Contains(t.Id)
                            && t.PrerequisiteIds.All(p => AiResearchedTechs.Contains(p) || Research.IsResearched(p)))
                .OrderBy(t => t.Era)
                .ThenBy(t => t.ScienceCost)
                .FirstOrDefault();

            if (available == null) return;
            AiCurrentResearchId = available.Id;
            AiScienceProgress = 0;
        }

        var tech = Research.AllTechnologies.Find(t => t.Id == AiCurrentResearchId);
        if (tech == null)
        {
            AiCurrentResearchId = null;
            return;
        }

        AiScienceProgress += aiCommerce;

        if (AiScienceProgress >= tech.ScienceCost)
        {
            AiResearchedTechs.Add(tech.Id);
            AiCurrentResearchId = null;
            AiScienceProgress = 0;
            Console.WriteLine($"[AI Research] AI Rival has discovered {tech.Name}!");
        }
    }

    private void ProcessBarbarianTurn()
    {
        // Spawn barbarians based on difficulty settings
        int barbInterval = DifficultyConfig.BarbarianSpawnInterval;
        if (barbInterval > 0 && TurnNumber % barbInterval == 0)
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

            // If a player target was found, calculate path and take a step towards it
            if (closestDist != int.MaxValue && closestDist > 0)
            {
                var path = FindPath(barb, barb.X, barb.Y, targetX, targetY, ignoreUnits: true);
                if (path != null && path.Count > 1)
                {
                    var (nextX, nextY) = path[1];
                    MoveUnit(barb, nextX, nextY);
                }
                else
                {
                    // Fallback to direct sign step if no path was found (e.g., if we want to attack or move directly)
                    int stepX = barb.X + Math.Sign(targetX - barb.X);
                    int stepY = barb.Y + Math.Sign(targetY - barb.Y);

                    if (Map.IsInBounds(stepX, stepY))
                    {
                        MoveUnit(barb, stepX, stepY);
                    }
                }
            }
        }
    }

    private void ProcessAiRivalTurn()
    {
        AiRivalBrain.ProcessTurn(this);
    }

    private int CollectCityYields()
    {
        int totalCommerceThisTurn = 0;
        int totalMaintenanceThisTurn = 0;

        foreach (var city in Cities)
        {
            // Update citizen happiness/mood and accumulate culture
            city.UpdateCitizenMood(this, DifficultyConfig);

            int cultPerTurn = city.GetCulturePerTurn();
            city.CultureOutput = cultPerTurn;
            city.AccumulatedCulture += cultPerTurn;

            // Expand cultural borders based on threshold
            int currentRadius = 1;
            if (city.AccumulatedCulture >= 10000) currentRadius = 5;
            else if (city.AccumulatedCulture >= 1000) currentRadius = 4;
            else if (city.AccumulatedCulture >= 100) currentRadius = 3;
            else if (city.AccumulatedCulture >= 10) currentRadius = 2;
            ClaimCityTerritory(city, currentRadius);

            int food = 0, production = 0, commerce = 0;

            // 1. Center tile (city center itself) is ALWAYS worked
            var centerTile = Map.GetTile(city.X, city.Y);
            if (centerTile != null)
            {
                var yield = centerTile.TotalYield;
                int centerFood = yield.Food;
                int centerProduction = yield.Production;
                int centerCommerce = yield.Commerce;

                // Agricultural Trait: +1 Food in city center
                if (city.HasTrait(CivTrait.Agricultural))
                {
                    centerFood += 1;
                }

                // Industrious Trait: +1 Production in city center
                if (city.HasTrait(CivTrait.Industrious))
                {
                    centerProduction += 1;
                }

                // Commercial Trait: +1 Commerce in city center
                if (city.HasTrait(CivTrait.Commercial))
                {
                    centerCommerce += 1;
                }

                // Seafaring Trait: +1 Commerce in city center for coastal cities
                if (city.HasTrait(CivTrait.Seafaring) && city.IsCoastal(Map))
                {
                    centerCommerce += 1;
                }

                food += centerFood;
                production += centerProduction;
                commerce += centerCommerce;
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

            // Apply Small Wonder modifiers
            if (city.Buildings.Any(b => b.Id == "forbidden_palace"))
            {
                commerce = (int)Math.Round(commerce * 1.5f);
            }
            if (city.Buildings.Any(b => b.Id == "iron_works"))
            {
                production *= 2;
            }

            // Apply Corruption & Waste (based on distance to capital)
            int dist = GetDistanceToNearestCapital(city);
            float corruptionRate = city.IsCapital ? 0f : Math.Min(dist * 0.10f, 0.70f);
            float wasteRate = city.IsCapital ? 0f : Math.Min(dist * 0.08f, 0.50f);

            // Commercial Trait: 25% lower base corruption
            if (city.HasTrait(CivTrait.Commercial))
            {
                corruptionRate *= 0.75f;
            }

            // Difficulty: Player corruption modifier (easier levels reduce corruption)
            if (city.Faction == Faction.Player)
            {
                corruptionRate *= DifficultyConfig.PlayerCorruptionMultiplier;
                wasteRate *= DifficultyConfig.PlayerCorruptionMultiplier;
            }

            if (city.Buildings.Any(b => b.Id == "courthouse")) corruptionRate *= 0.5f;
            if (city.Buildings.Any(b => b.Id == "police_station")) corruptionRate *= 0.5f;

            int corruption = (int)Math.Round(commerce * corruptionRate);
            int waste = (int)Math.Round(production * wasteRate);
            city.LastTurnCorruption = corruption;
            city.LastTurnWaste = waste;
            commerce -= corruption;
            production -= waste;

            // Difficulty: AI yield multiplier (higher difficulty = AI gets more production)
            if (city.Faction == Faction.AiRival)
            {
                production = (int)Math.Round(production * DifficultyConfig.AiYieldMultiplier);
                commerce = (int)Math.Round(commerce * DifficultyConfig.AiYieldMultiplier);
            }

            // Civil Disorder halts all production and commerce
            if (city.IsInDisorder)
            {
                production = 0;
                commerce = 0;
                Console.WriteLine($"[REVOLT] {city.Name} is in Civil Disorder! Production and Commerce are completely HALTED.");
            }

            // 4.5 Pollution Event Roll (1.5% probability per point of pollution per turn)
            var pollutionStats = PollutionSystem.GetCityPollutionStats(city, production);
            var rand = new Random();
            if (rand.NextDouble() < pollutionStats.Probability && workingTiles.Count > 0)
            {
                int randIndex = rand.Next(workingTiles.Count);
                var targetTile = workingTiles[randIndex];
                if (!targetTile.IsPolluted)
                {
                    targetTile.IsPolluted = true;
                    Console.WriteLine($"[POLLUTION] Active pollution has erupted at ({targetTile.X}, {targetTile.Y}) near {city.Name}! The tile yields nothing until cleaned.");
                }
            }

            // 5. Food Consumption & Growth/Starvation
            int foodConsumption = city.Population * 2;
            int netFood = food - foodConsumption;
            city.LastTurnNetFood = netFood;

            city.StoredFood += netFood;
            
            // Check population cap based on Civ3 rules (Fresh Water or Aqueduct allows Size 7-12, Hospital allows Size 13+)
            int popCap = 6;
            bool hasFreshWater = HasFreshWaterAccess(city);
            if (city.HasBuilding<Hospital>())
            {
                popCap = 20; // Metropolis cap
            }
            else if (city.HasBuilding<Aqueduct>() || hasFreshWater)
            {
                popCap = 12;
            }

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
            if (city.Faction == Faction.Player)
            {
                totalMaintenanceThisTurn += cityMaintenance;
                totalCommerceThisTurn += commerce;
            }
            
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

                int cost = city.GetProjectCost(city.CurrentProject, DifficultyConfig);
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
            
            // Scientific Trait: +10% science research bonus
            bool playerIsScientific = PlayerCiv.Trait1 == CivTrait.Scientific || PlayerCiv.Trait2 == CivTrait.Scientific;
            if (playerIsScientific)
            {
                scienceGain = (int)Math.Round(scienceGain * 1.10f);
            }

            // Difficulty: Player research bonus (easier levels give more science)
            scienceGain = (int)Math.Round(scienceGain * DifficultyConfig.PlayerResearchMultiplier);
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

        // Wall Street effect: +5% interest on player's treasury (capped at 50 gold per turn)
        bool playerHasWallStreet = Cities.Any(c => c.Faction == Faction.Player && c.Buildings.Any(b => b.Id == "wall_street"));
        if (playerHasWallStreet && PlayerTreasury > 0)
        {
            int wallStreetInterest = (int)Math.Floor(PlayerTreasury * 0.05f);
            if (wallStreetInterest > 50) wallStreetInterest = 50;
            PlayerTreasury += wallStreetInterest;
            goldGain += wallStreetInterest;
            Console.WriteLine($"[Wall Street] Earned {wallStreetInterest} gold in interest!");
        }

        // Intelligence Agency effect: +10 gold per turn from spy operations
        bool playerHasIntelAgency = Cities.Any(c => c.Faction == Faction.Player && c.Buildings.Any(b => b.Id == "intelligence_agency"));
        if (playerHasIntelAgency)
        {
            PlayerTreasury += 10;
            goldGain += 10;
            Console.WriteLine($"[Intelligence Agency] Generated +10 gold from espionage networks!");
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
            ProductionProject.Pentagon => "pentagon",
            ProductionProject.ForbiddenPalace => "forbidden_palace",
            ProductionProject.WallStreet => "wall_street",
            ProductionProject.IntelligenceAgency => "intelligence_agency",
            ProductionProject.BattlefieldMedicine => "battlefield_medicine",
            ProductionProject.SDIDefense => "sdi_defense",
            _ => null
        };

        if (buildingId != null)
        {
            Wonder? wonder = WonderRegistry.Get(buildingId);
            Building? building = BuildingRegistry.Get(buildingId);

            // Spaceship parts are tracked separately (not in Building/Wonder registries)
            if (AllSpaceshipParts.Contains(project))
            {
                if (city.Faction == Faction.Player)
                {
                    BuiltSpaceshipParts.Add(project);
                    Console.WriteLine($"[Space Race] {city.Name} completed spaceship part: {project}. ({BuiltSpaceshipParts.Count}/{AllSpaceshipParts.Count})");
                }
                else if (city.Faction == Faction.AiRival)
                {
                    AiBuiltSpaceshipParts.Add(project);
                    Console.WriteLine($"[Space Race] AI {city.Name} completed spaceship part: {project}. ({AiBuiltSpaceshipParts.Count}/{AllSpaceshipParts.Count})");
                }
            }
            else if (wonder != null && !IsWonderClaimedByFaction(city.Faction, buildingId))
            {
                ClaimWonder(wonder, city.Faction);
                city.Buildings.Add(wonder);
                System.Console.WriteLine($"[Production] {city.Name} has completed building a Wonder: {wonder.Name}!");
                wonder.OnCompleted(city, this);

                // Diplomatic victory: builder of United Nations with majority pop wins
                if (buildingId == "united_nations")
                {
                    int builderPop = Cities.Where(c => c.Faction == city.Faction).Sum(c => c.Population);
                    int totalPop  = Cities.Sum(c => c.Population);
                    if (totalPop > 0 && builderPop * 2 > totalPop)
                    {
                        if (city.Faction == Faction.Player)
                        {
                            EndState = GameEndState.VictoryDiplomatic;
                            Console.WriteLine($"[Game End] VICTORY! You were elected world leader by the United Nations!");
                        }
                        else
                        {
                            EndState = GameEndState.DefeatDiplomatic;
                            Console.WriteLine($"[Game End] DEFEAT! The AI Rival was elected world leader by the United Nations!");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Diplomatic] United Nations built by {city.Faction} — but no population majority yet.");
                    }
                }
            }
            else if (building != null)
            {
                // If building a Palace, move capital from old to new city
                if (building is Palace && !city.IsCapital)
                {
                    var oldCapital = Cities.FirstOrDefault(c => c.Faction == city.Faction && c.IsCapital);
                    if (oldCapital != null)
                    {
                        oldCapital.IsCapital = false;
                        oldCapital.Buildings.RemoveAll(b => b is Palace);
                        Console.WriteLine($"[Capital] Palace moved from {oldCapital.Name} to {city.Name}!");
                    }
                    city.IsCapital = true;
                }

                city.Buildings.Add(building);
                System.Console.WriteLine($"[Production] {city.Name} has completed building a {building.Name}!");
                building.OnCompleted(city, this);
            }
        }
        if (project != ProductionProject.None && buildingId == null)
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

            // Military Academy effect: if city has military_academy, military units start with +25% max health (125 HP instead of 100)
            if (city.Buildings.Any(b => b.Id == "military_academy") && (type == UnitType.Warrior || type == UnitType.Archer))
            {
                unit.MaxHealth = 125;
                unit.Health = 125;
                System.Console.WriteLine($"[Military Academy] Trained elite {type} with +25% max health (125 HP)!");
            }

            Units.Add(unit);
            System.Console.WriteLine($"[Production] {city.Name} has completed building a {type}!");
        }

        if (city.ProductionQueue.Count > 0)
        {
            var nextProject = city.ProductionQueue[0];
            city.ProductionQueue.RemoveAt(0);
            city.CurrentProject = nextProject;
            System.Console.WriteLine($"[Production] {city.Name} started next queued project: {nextProject}!");
        }
        else
        {
            city.CurrentProject = ProductionProject.None;
            city.CurrentProductionProgress = 0;
        }
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
            ProductionProject.Warrior => IsTechResearched(city.Faction, "bronze_working")
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
            (production: ProductionProject.Pentagon, buildingId: "pentagon"),
            (production: ProductionProject.ForbiddenPalace, buildingId: "forbidden_palace"),
            (production: ProductionProject.WallStreet, buildingId: "wall_street"),
            (production: ProductionProject.IntelligenceAgency, buildingId: "intelligence_agency"),
            (production: ProductionProject.BattlefieldMedicine, buildingId: "battlefield_medicine"),
            (production: ProductionProject.SDIDefense, buildingId: "sdi_defense"),
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

            if (!DebugIgnorePrerequisites)
            {
                if (targetBuilding!.RequiredTechId != null && !Research.IsResearched(targetBuilding.RequiredTechId))
                    continue;

                if (targetBuilding.RequiredResourceId != null && !CityHasResourceAccess(city, targetBuilding.RequiredResourceId))
                    continue;
            }

            // For Wonders, check if claimed globally or by faction
            if (wonder != null && IsWonderClaimedByFaction(city.Faction, buildingId))
                continue;

            // Check custom prerequisites for Small Wonders
            if (wonder != null && wonder.IsNationalWonder && !AreSmallWonderPrerequisitesMet(city, buildingId))
                continue;

            // For spaceship parts, check if Apollo Program is built by this faction
            if (buildingId.StartsWith("ss_") && !IsWonderClaimedByFaction(city.Faction, "apollo_program"))
                continue;

            // For regular buildings, check if city already has it
            if (building != null && city.Buildings.Any(b => b.Id == buildingId))
                continue;

            return production;
        }

        return ProductionProject.None;
    }
}
