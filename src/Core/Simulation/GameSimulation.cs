using System;
using System.Collections.Generic;
using System.Linq;
using CivGame.Core.Pathfinding;
using CivGame.Core.AI;
using CivGame.Core.Diplomacy;

namespace CivGame.Core;

public enum CityCaptureChoice
{
    Keep,
    Raze
}

public class GameSimulation
    {
        public event Action<City, Unit>? OnCityCapturePrompt;
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
        public FogState[,] Player2VisibilityGrid { get; }
        public int TurnNumber { get; private set; } = 1;
        public List<(int X, int Y)> BarbarianCamps { get; } = new();
        public List<(int X, int Y)> GoodyHuts { get; } = new();
        public TechManager Research { get; } = new();
        public TechManager Player2Research { get; } = new();
        public EspionageSystem Espionage { get; private set; }

        // Multiplayer State
        public GameMode Mode { get; set; } = GameMode.SinglePlayer;
        public Faction ActiveTurnFaction { get; set; } = Faction.Player;
        public string Player1Name { get; set; } = "Player 1";
        public string Player2Name { get; set; } = "Player 2";
        public int AiTreasury { get; set; } = 0;
        public int AiTaxRate { get; set; } = 50;
        public int AiEntertainmentRate { get; set; } = 0;

        public event Action<Faction>? OnHotseatTurnChange;

        public FogState[,] GetVisibilityGrid(Faction faction)
        {
            return faction == Faction.AiRival ? Player2VisibilityGrid : VisibilityGrid;
        }

        public TechManager GetResearch(Faction faction)
        {
            return faction == Faction.AiRival ? Player2Research : Research;
        }

        public int GetTreasury(Faction faction) => faction == Faction.AiRival ? AiTreasury : PlayerTreasury;
        public void SetTreasury(Faction faction, int val)
        {
            if (faction == Faction.AiRival) AiTreasury = val;
            else PlayerTreasury = val;
        }

        public int GetTaxRate(Faction faction) => faction == Faction.AiRival ? AiTaxRate : PlayerTaxRate;
        public int GetEntertainmentRate(Faction faction) => faction == Faction.AiRival ? AiEntertainmentRate : PlayerEntertainmentRate;
        public GovernmentType GetGovernment(Faction faction) => faction == Faction.AiRival ? AiGovernment : PlayerGovernment;
        public void SetGovernment(Faction faction, GovernmentType gov)
        {
            if (faction == Faction.AiRival) AiGovernment = gov;
            else PlayerGovernment = gov;
        }

        public Civilization ActiveCiv => ActiveTurnFaction == Faction.AiRival ? AiCiv : PlayerCiv;
        public string ActivePlayerName => ActiveTurnFaction == Faction.AiRival ? Player2Name : Player1Name;
        
        private DiplomacyManager _diplomacy;
        public DiplomacyManager Diplomacy => _diplomacy ??= new DiplomacyManager(this);
        public bool IsAtWarWithAi { get; set; } = false;
        public GameEndState EndState { get; private set; } = GameEndState.None;
        public const int MaxTurnLimit = 50;

        // Histograph (per-turn stats tracking for graph)
        public HistographData Histograph { get; } = new();

        // Government
        public GovernmentType PlayerGovernment { get; set; } = GovernmentType.Despotism;
        public GovernmentType AiGovernment { get; set; } = GovernmentType.Despotism;
        public int AnarchyTurnsRemaining { get; set; } = 0;
        /// <summary>Government that will take effect after anarchy ends.</summary>
        public GovernmentType? PendingGovernment { get; set; } = null;
        /// <summary>True if the player is currently in anarchy (revolution in progress).</summary>
        public bool IsInAnarchy => AnarchyTurnsRemaining > 0;

        // War Weariness (Civ3 authentic: tracked per enemy civ via War Weariness Points)
        // WWP thresholds: 0-30 = no effect, 31-60 = level 1, 61-90 = level 2, 91-120 = level 3, 121+ = level 4
        public int WarWearinessPoints { get; set; } = 0;
        /// <summary>Turn war was declared. -1 = never at war.</summary>
        public int WarStartTurn { get; set; } = -1;
        /// <summary>True if AI declared war on us (defensive war = delayed WW).</summary>
        public bool IsDefensiveWar { get; set; } = false;
        /// <summary>Turns since peace was signed (for WW decay).</summary>
        public int PeaceTurns { get; set; } = 0;

        // Economy
        public int PlayerTreasury { get; set; } = 0;
        public int PlayerTaxRate { get; set; } = 50; // 0-100 percentage
        public int PlayerEntertainmentRate { get; set; } = 0; // 0-100 percentage

        public int LastTurnIncome { get; private set; } = 0;
        public int LastTurnMaintenance { get; private set; } = 0;
        public int LastTurnUnitSupport { get; private set; } = 0;
        public int LastTurnScience { get; private set; } = 0;
        public int LastTurnNetGold { get; private set; } = 0;

        // Trade Networks & Resource Pooling
        public Dictionary<int, HashSet<string>> TradeNetworkResources { get; } = new();
        public Dictionary<string, int> CityTradeNetworks { get; } = new(); // CityId -> TradeNetworkId

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

    // Golden Age (Civ3: one per civ, 20 turns, +1 shield/commerce on productive tiles)
    public GoldenAge PlayerGoldenAge { get; } = new();
    public GoldenAge AiGoldenAge { get; } = new();

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
        Map.CalculateContinents();
        VisibilityGrid = new FogState[map.Width, map.Height];
        Player2VisibilityGrid = new FogState[map.Width, map.Height];
        Espionage = new EspionageSystem(this);
        
        // Initialize all tiles as completely Unexplored for both factions
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                VisibilityGrid[x, y] = FogState.Unexplored;
                Player2VisibilityGrid[x, y] = FogState.Unexplored;
            }
        }

        // Procedurally spawn 3 Barbarian Camps on land, far from center starting area
        SpawnInitialBarbarianCamps();

        // Procedurally spawn Goody Huts
        SpawnInitialGoodyHuts();
    }

    private void SpawnInitialGoodyHuts()
    {
        int centerX = Map.Width / 2;
        int centerY = Map.Height / 2;
        var rand = new Random(84); // Seeded random

        int targetHuts = (Map.Width * Map.Height) / 50;
        int hutsSpawned = 0;
        int attempts = 0;

        while (hutsSpawned < targetHuts && attempts < 500)
        {
            attempts++;
            int rx = rand.Next(2, Map.Width - 2);
            int ry = rand.Next(2, Map.Height - 2);

            int distFromCenter = Map.GetChebyshevDistance(rx, ry, centerX, centerY);
            if (distFromCenter < 5) continue; // Keep clear of immediate starting area

            var tile = Map.GetTile(rx, ry);
            if (tile != null && tile.IsLand && tile.Terrain.Id != "mountain" && tile.Terrain.Id != "volcano")
            {
                if (!BarbarianCamps.Contains((rx, ry)) && !GoodyHuts.Contains((rx, ry)))
                {
                    GoodyHuts.Add((rx, ry));
                    hutsSpawned++;
                }
            }
        }
        Console.WriteLine($"[Setup] Spawned {hutsSpawned} Goody Huts.");
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

            int distFromCenter = Map.GetChebyshevDistance(rx, ry, centerX, centerY);
            if (distFromCenter < 8) continue; // Keep clear of starting area

            var tile = Map.GetTile(rx, ry);
            if (tile != null && tile.IsLand)
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
                        if (tile != null && tile.IsValidStartTile)
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
                    if (tile != null && tile.IsValidStartTile)
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
        int distance = Map.GetManhattanDistance(aiStartX, aiStartY, px, py);
        if (distance < Map.Width / 3)
        {
            // Shift AI starting search position to another quadrant
            aiStartX = Map.NormalizeX(px + Map.Width / 3);
            aiStartY = (py + Map.Height / 3) % Map.Height;
        }

        // Ensure within reasonable bounds
        aiStartX = Map.NormalizeX(aiStartX);
        aiStartY = Math.Clamp(aiStartY, 2, Map.Height - 3);

        bool foundLand = false;
        // Search in a spiral for a safe land tile
        for (int r = 0; r < 15 && !foundLand; r++)
        {
            for (int dx = -r; dx <= r && !foundLand; dx++)
            {
                for (int dy = -r; dy <= r && !foundLand; dy++)
                {
                    int tx = Map.NormalizeX(aiStartX + dx);
                    int ty = aiStartY + dy;
                    if (Map.IsInBounds(tx, ty))
                    {
                        var tile = Map.GetTile(tx, ty);
                        if (tile != null && tile.IsValidStartTile)
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
                int tx = Map.NormalizeX(aiStartX + dx);
                int ty = aiStartY + dy;
                if (Map.IsInBounds(tx, ty) && (tx != aiStartX || ty != aiStartY))
                {
                    var tile = Map.GetTile(tx, ty);
                    if (tile != null && tile.IsValidStartTile)
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
        // 1. Shroud currently visible areas for both player 1 and player 2
        for (int x = 0; x < Map.Width; x++)
        {
            for (int y = 0; y < Map.Height; y++)
            {
                if (VisibilityGrid[x, y] == FogState.Visible)
                {
                    VisibilityGrid[x, y] = FogState.Shrouded;
                }
                if (Player2VisibilityGrid != null && Player2VisibilityGrid[x, y] == FogState.Visible)
                {
                    Player2VisibilityGrid[x, y] = FogState.Shrouded;
                }
            }
        }

        // 2. Apply active vision around units for their respective faction
        foreach (var unit in Units)
        {
            if (unit.Faction != Faction.Player && unit.Faction != Faction.AiRival) continue;
            var targetGrid = unit.Faction == Faction.Player ? VisibilityGrid : Player2VisibilityGrid;
            if (targetGrid == null) continue;

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
                    int tx = Map.NormalizeX(unit.X + dx);
                    int ty = unit.Y + dy;
                    if (ty >= 0 && ty < Map.Height)
                    {
                        targetGrid[tx, ty] = FogState.Visible;
                    }
                }
            }
        }

        // 3. Apply active vision around cities for their respective faction
        foreach (var city in Cities)
        {
            if (city.Faction != Faction.Player && city.Faction != Faction.AiRival) continue;
            var targetGrid = city.Faction == Faction.Player ? VisibilityGrid : Player2VisibilityGrid;
            if (targetGrid == null) continue;

            int r = city.VisionRange;
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    int tx = Map.NormalizeX(city.X + dx);
                    int ty = city.Y + dy;
                    if (ty >= 0 && ty < Map.Height)
                    {
                        targetGrid[tx, ty] = FogState.Visible;
                    }
                }
            }
        }
    }

    public bool CanBuildCity(Unit unit)
    {
        if (unit.Type != UnitType.Settler) return false;

        // Civ3: can only found city on valid land (no water, mountains, volcanoes)
        var tile = Map.GetTile(unit.X, unit.Y);
        if (tile == null || !tile.CanFoundCity) return false;

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

        CalculateCulturalBorders();
        UpdateVisibility();
        return city;
    }

    public void CalculateCulturalBorders()
    {
        var highestCulture = new System.Collections.Generic.Dictionary<TileData, (string CityId, int Culture)>();

        foreach (var city in Cities)
        {
            int radius = 1;
            if (city.Buildings.Any(b => b.Id == "monument" || b.Id == "temple")) radius = Math.Max(radius, 2);
            if (city.AccumulatedCulture >= 10000) radius = Math.Max(radius, 5);
            else if (city.AccumulatedCulture >= 1000) radius = Math.Max(radius, 4);
            else if (city.AccumulatedCulture >= 100) radius = Math.Max(radius, 3);
            else if (city.AccumulatedCulture >= 10) radius = Math.Max(radius, 2);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int tx = Map.NormalizeX(city.X + dx);
                    int ty = city.Y + dy;
                    if (Map.IsInBounds(tx, ty))
                    {
                        var tile = Map.GetTile(tx, ty);
                        if (tile != null && tile.IsLand)
                        {
                            if (dx == 0 && dy == 0)
                            {
                                highestCulture[tile] = (city.Id, int.MaxValue);
                                continue;
                            }
                            
                            int dist = Map.GetChebyshevDistance(city.X, city.Y, tx, ty);
                            int influence = city.AccumulatedCulture / Math.Max(1, dist);
                            if (influence == 0) influence = 1; // base 1 influence for newly founded cities

                            if (!highestCulture.ContainsKey(tile) || highestCulture[tile].Culture < influence)
                            {
                                highestCulture[tile] = (city.Id, influence);
                            }
                        }
                    }
                }
            }
        }

        // Apply ownerships globally
        for (int x = 0; x < Map.Width; x++)
        {
            for (int y = 0; y < Map.Height; y++)
            {
                var tile = Map.GetTile(x, y);
                if (tile != null && tile.IsLand)
                {
                    if (highestCulture.TryGetValue(tile, out var claim))
                    {
                        tile.OwnerCityId = claim.CityId;
                    }
                    else
                    {
                        tile.OwnerCityId = null;
                    }
                }
            }
        }
    }

    public void CheckCulturalFlipping()
    {
        var flippedCities = new System.Collections.Generic.List<(City City, Faction NewFaction)>();

        foreach (var city in Cities)
        {
            if (city.IsCapital) continue; // Capitals do not flip

            var centerTile = Map.GetTile(city.X, city.Y);
            if (centerTile == null) continue;

            Faction? bestFaction = null;
            int maxEnemyInfluence = 0;

            foreach (var otherCity in Cities)
            {
                if (otherCity.Faction == city.Faction) continue;

                int dist = Map.GetChebyshevDistance(city.X, city.Y, otherCity.X, otherCity.Y);
                
                int radius = 1;
                if (otherCity.AccumulatedCulture >= 10000) radius = 5;
                else if (otherCity.AccumulatedCulture >= 1000) radius = 4;
                else if (otherCity.AccumulatedCulture >= 100) radius = 3;
                else if (otherCity.AccumulatedCulture >= 10) radius = 2;

                if (dist <= radius)
                {
                    int influence = otherCity.AccumulatedCulture / Math.Max(1, dist);
                    if (influence > maxEnemyInfluence)
                    {
                        maxEnemyInfluence = influence;
                        bestFaction = otherCity.Faction;
                    }
                }
            }

            if (bestFaction.HasValue && maxEnemyInfluence > city.AccumulatedCulture)
            {
                // Chance to flip (max 5% per turn)
                double chance = Math.Min(0.05, (maxEnemyInfluence - city.AccumulatedCulture) / 10000.0);
                if (Random.Shared.NextDouble() < chance)
                {
                    flippedCities.Add((city, bestFaction.Value));
                }
            }
        }

        foreach (var flip in flippedCities)
        {
            var city = flip.City;
            var oldFaction = city.Faction;
            city.Faction = flip.NewFaction;

            // Destroy all garrisoned units of the old faction
            var garrison = Units.Where(u => u.X == city.X && u.Y == city.Y && u.Faction == oldFaction).ToList();
            foreach (var u in garrison) DestroyUnit(u);

            // Spawn 1 defensive unit for the new faction
            UnitType defUnit = TurnNumber > 100 ? UnitType.Infantry : (TurnNumber > 50 ? UnitType.Musketman : UnitType.Spearman);
            var guard = new Unit(Guid.NewGuid().ToString(), defUnit, city.X, city.Y, flip.NewFaction);
            Units.Add(guard);

            Console.WriteLine($"[CULTURE FLIP] The city of {city.Name} has revolted against {oldFaction} and joined {flip.NewFaction} due to cultural influence!");
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

    public bool DoesFactionControlWonder(Faction faction, string wonderId)
    {
        Wonder? wonder = WonderRegistry.Get(wonderId);
        if (wonder == null) return false;

        if (wonder.IsNationalWonder)
        {
            return GetCompletedSmallWonders(faction).Contains(wonderId);
        }
        else
        {
            return Cities.Any(c => c.Faction == faction && c.Buildings.Any(b => b.Id == wonderId));
        }
    }

    /// <summary>
    /// Checks if a city receives clean Hydro Plant power from Hoover Dam on the same continent.
    /// </summary>
    public bool IsCityPoweredByHooverDam(City city)
    {
        if (!DoesFactionControlWonder(city.Faction, "hoover_dam")) return false;

        var hooverCity = Cities.FirstOrDefault(c => c.Faction == city.Faction && c.Buildings.Any(b => b.Id == "hoover_dam"));
        if (hooverCity == null) return false;

        return Map.AreOnSameContinent(city.X, city.Y, hooverCity.X, hooverCity.Y);
    }

    /// <summary>
    /// Civ3 Magellan's Voyage: all naval units of the owner receive +2 Movement.
    /// </summary>
    public void UpdateUnitMovementBonuses()
    {
        bool playerHasMagellan = DoesFactionControlWonder(Faction.Player, "magellans_voyage");
        bool aiHasMagellan = DoesFactionControlWonder(Faction.AiRival, "magellans_voyage");

        foreach (var unit in Units)
        {
            if (unit.IsNavalUnit)
            {
                bool hasMagellan = unit.Faction == Faction.Player ? playerHasMagellan : (unit.Faction == Faction.AiRival && aiHasMagellan);
                float prevBonus = unit.MovementBonus;
                unit.MovementBonus = hasMagellan ? 2.0f : 0.0f;
                if (unit.MovementBonus > prevBonus)
                {
                    unit.RemainingMovement += (unit.MovementBonus - prevBonus);
                }
            }
        }
    }

    /// <summary>
    /// Civ3 Theory of Evolution: grants free scientific advances immediately upon completion.
    /// </summary>
    public List<string> GrantFreeTechnologies(Faction faction, int count)
    {
        var grantedTechs = new List<string>();

        for (int i = 0; i < count; i++)
        {
            Technology? candidate = null;

            if (faction == Faction.Player)
            {
                // Check if current active research can be completed
                if (Research.CurrentResearch != null && !Research.IsResearched(Research.CurrentResearch.Id))
                {
                    candidate = Research.CurrentResearch;
                }

                // Otherwise pick earliest unresearched tech with prerequisites met
                if (candidate == null || grantedTechs.Contains(candidate.Id))
                {
                    candidate = Research.AllTechnologies
                        .Where(t => !Research.IsResearched(t.Id) && !grantedTechs.Contains(t.Id))
                        .Where(t => t.PrerequisiteIds.All(p => Research.IsResearched(p) || grantedTechs.Contains(p)))
                        .OrderBy(t => t.Era)
                        .ThenBy(t => t.ScienceCost)
                        .FirstOrDefault();
                }

                if (candidate != null)
                {
                    Research.ResearchedTechIds.Add(candidate.Id);
                    candidate.OnUnlocked(this);
                    grantedTechs.Add(candidate.Id);
                    if (Research.CurrentResearch?.Id == candidate.Id)
                    {
                        Research.SetResearch(null);
                    }
                    Console.WriteLine($"[Theory of Evolution] Player discovered free advance: {candidate.Name}!");
                }
            }
            else if (faction == Faction.AiRival)
            {
                if (!string.IsNullOrEmpty(AiCurrentResearchId) && !AiResearchedTechs.Contains(AiCurrentResearchId))
                {
                    candidate = Research.AllTechnologies.FirstOrDefault(t => t.Id == AiCurrentResearchId);
                }

                if (candidate == null || grantedTechs.Contains(candidate.Id))
                {
                    candidate = Research.AllTechnologies
                        .Where(t => !AiResearchedTechs.Contains(t.Id) && !grantedTechs.Contains(t.Id))
                        .Where(t => t.PrerequisiteIds.All(p => AiResearchedTechs.Contains(p) || grantedTechs.Contains(p)))
                        .OrderBy(t => t.Era)
                        .ThenBy(t => t.ScienceCost)
                        .FirstOrDefault();
                }

                if (candidate != null)
                {
                    AiResearchedTechs.Add(candidate.Id);
                    grantedTechs.Add(candidate.Id);
                    if (AiCurrentResearchId == candidate.Id)
                    {
                        AiCurrentResearchId = null;
                        AiScienceProgress = 0;
                    }
                    Console.WriteLine($"[Theory of Evolution] AI Rival discovered free advance: {candidate.Name}!");
                }
            }
        }

        return grantedTechs;
    }

    /// <summary>
    /// Civ3 Great Library: grants all technologies known by rival civilizations until Education is discovered.
    /// </summary>
    public void ApplyGreatLibraryCatchup(Faction? specificFaction = null)
    {
        // 1. Check Player
        if (specificFaction == null || specificFaction == Faction.Player)
        {
            if (DoesFactionControlWonder(Faction.Player, "great_library") && !Research.IsResearched("education"))
            {
                var techsToLearn = AiResearchedTechs.Where(t => !Research.IsResearched(t)).ToList();
                foreach (var techId in techsToLearn)
                {
                    Research.ResearchedTechIds.Add(techId);
                    var tech = Research.AllTechnologies.FirstOrDefault(t => t.Id == techId);
                    tech?.OnUnlocked(this);
                    Console.WriteLine($"[The Great Library] Player acquired {tech?.Name ?? techId} through ancient scrolls!");
                    if (Research.CurrentResearch?.Id == techId)
                    {
                        Research.SetResearch(null);
                    }
                }
            }
        }

        // 2. Check AI Rival
        if (specificFaction == null || specificFaction == Faction.AiRival)
        {
            if (DoesFactionControlWonder(Faction.AiRival, "great_library") && !AiResearchedTechs.Contains("education"))
            {
                var techsToLearn = Research.ResearchedTechIds.Where(t => !AiResearchedTechs.Contains(t)).ToList();
                foreach (var techId in techsToLearn)
                {
                    AiResearchedTechs.Add(techId);
                    var tech = Research.AllTechnologies.FirstOrDefault(t => t.Id == techId);
                    Console.WriteLine($"[The Great Library] AI Rival acquired {tech?.Name ?? techId} through ancient scrolls!");
                    if (AiCurrentResearchId == techId)
                    {
                        AiCurrentResearchId = null;
                        AiScienceProgress = 0;
                    }
                }
            }
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
        targetX = Map.NormalizeX(targetX);

        // Check if movement destination is adjacent (Chebyshev distance of 1)
        int dx = Map.GetDeltaX(targetX, unit.X);
        int dy = Math.Abs(targetY - unit.Y);
        if (dx > 1 || dy > 1 || (dx == 0 && dy == 0)) return MoveValidationResult.Invalid(MoveValidationFailureReason.TargetNotAdjacent);

        // Check if unit has any movement points left
        if (!unit.HasMovementRemaining()) return MoveValidationResult.Invalid(MoveValidationFailureReason.NoMovementRemaining);

        var tile = Map.GetTile(targetX, targetY);
        if (tile == null) return MoveValidationResult.Invalid(MoveValidationFailureReason.TargetTileMissing);

        // 1. Air Domain: cannot use regular step-by-step movement (use Rebase, Air Strike, Recon)
        if (unit.Domain == UnitDomain.Air)
        {
            return MoveValidationResult.Invalid(MoveValidationFailureReason.TargetTileImpassable);
        }

        // 2. Sea Domain: can only enter water tiles (Coast, Sea, Ocean) OR friendly coastal cities (ports)
        if (unit.Domain == UnitDomain.Sea)
        {
            bool isWater = tile.IsOcean;
            bool isFriendlyPort = Cities.Any(c => c.X == targetX && c.Y == targetY && c.Faction == unit.Faction && c.IsCoastal(Map));
            if (!isWater && !isFriendlyPort)
            {
                return MoveValidationResult.Invalid(MoveValidationFailureReason.TargetTileImpassable);
            }
        }

        // 3. Land Domain: can enter land tiles, OR enter water tile if boarding a friendly transport
        if (unit.Domain == UnitDomain.Land)
        {
            if (tile.IsOcean)
            {
                bool canBoard = Units.Any(u => u.X == targetX && u.Y == targetY && u.Faction == unit.Faction && u.Domain == UnitDomain.Sea && u.CanBoardTransport(unit, this));
                if (!canBoard)
                {
                    return MoveValidationResult.Invalid(MoveValidationFailureReason.TargetTileImpassable);
                }
            }
        }

        // Check hostile units on target tile
        var existingUnit = Units.Find(u => u.X == targetX && u.Y == targetY && u.TransportUnitId == null);
        if (existingUnit != null && IsHostile(unit, existingUnit))
        {
            // Domain combat restrictions: sea units cannot melee land units on land; land cannot melee sea units in ocean
            if (unit.Domain == UnitDomain.Sea && existingUnit.Domain == UnitDomain.Land && !tile.IsOcean)
            {
                return MoveValidationResult.Invalid(MoveValidationFailureReason.TargetTileImpassable);
            }
            if (unit.Domain == UnitDomain.Land && existingUnit.Domain == UnitDomain.Sea && tile.IsOcean)
            {
                return MoveValidationResult.Invalid(MoveValidationFailureReason.TargetTileImpassable);
            }
        }

        return MoveValidationResult.Valid();
    }


    public bool MoveUnit(Unit unit, int targetX, int targetY)
    {
        if (!ValidateUnitMove(unit, targetX, targetY).IsValid) return false;
        targetX = Map.NormalizeX(targetX);

        unit.IsFortified = false;
        unit.IsSentry = false;

        var tile = Map.GetTile(targetX, targetY)!;

        // Check if land unit is boarding a friendly transport in water
        if (unit.Domain == UnitDomain.Land && tile.IsOcean)
        {
            var transport = Units.Find(u => u.X == targetX && u.Y == targetY && u.Faction == unit.Faction && u.Domain == UnitDomain.Sea && u.CanBoardTransport(unit, this));
            if (transport != null)
            {
                unit.BoardTransport(transport, this);
                unit.RemainingMovement = 0;
                Console.WriteLine($"[Transport] {unit.Type} boarded {transport.Type} at ({targetX}, {targetY})!");
                UpdateVisibility();
                return true;
            }
        }

        // Check if there is an enemy unit on the target tile
        var enemyUnit = Units.Find(u => u.X == targetX && u.Y == targetY && u.TransportUnitId == null);
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

        // Check for Goody Hut
        if (GoodyHuts.Contains((targetX, targetY)) && unit.Domain == UnitDomain.Land)
        {
            ExploreGoodyHut(unit, targetX, targetY);
        }

        // Check for Barbarian Camp
        if (BarbarianCamps.Contains((targetX, targetY)) && unit.Faction != Faction.Barbarian && unit.Domain == UnitDomain.Land)
        {
            BarbarianCamps.Remove((targetX, targetY));
            if (unit.Faction == Faction.Player)
            {
                PlayerTreasury += 25;
                Console.WriteLine($"[Barbarian Camp] {unit.Type} dispersed a barbarian camp at ({targetX}, {targetY}) and collected 25 gold!");
            }
        }

        // ZOC (Zone of Control) check
        CheckZoneOfControl(unit, unit.X, unit.Y, targetX, targetY);

        // Normal movement
        float moveCost = GetTileMovementCostForUnit(tile, unit);
        unit.MoveTo(targetX, targetY, moveCost);

        // Synchronize cargo positions with transport
        if (unit.LoadedUnitIds.Count > 0)
        {
            foreach (var loadedId in unit.LoadedUnitIds)
            {
                var loadedUnit = Units.Find(u => u.Id == loadedId);
                if (loadedUnit != null)
                {
                    loadedUnit.X = targetX;
                    loadedUnit.Y = targetY;
                }
            }
        }
        
        // Check for city capture on normal move into undefended city
        CheckCityCapture(unit, targetX, targetY);

        UpdateVisibility();

        CheckGameEndConditions();
        return true;
    }

    private void CheckZoneOfControl(Unit movingUnit, int fromX, int fromY, int toX, int toY)
    {
        fromX = Map.NormalizeX(fromX);
        toX = Map.NormalizeX(toX);

        var hostileZocUnits = Units.Where(u =>
            u.TransportUnitId == null &&
            u.Health > 0 &&
            IsHostile(movingUnit, u) &&
            HasZoneOfControl(u) &&
            Map.GetChebyshevDistance(u.X, u.Y, fromX, fromY) <= 1 &&
            Map.GetChebyshevDistance(u.X, u.Y, toX, toY) <= 1 &&
            !(u.X == fromX && u.Y == fromY) && !(u.X == toX && u.Y == toY)
        ).ToList();

        foreach (var zocUnit in hostileZocUnits)
        {
            Console.WriteLine($"[ZOC] {zocUnit.Type} exerts Zone of Control on {movingUnit.Type}!");
            
            float hitChance = (float)zocUnit.AttackStrength / (zocUnit.AttackStrength + Math.Max(1.0f, movingUnit.DefenseStrength));
            hitChance = Math.Clamp(hitChance, 0.2f, 0.8f);

            if (Random.Shared.NextDouble() < hitChance)
            {
                int dmg = 20; // 1 HP = 20 Health in this engine
                movingUnit.Health -= dmg;
                Console.WriteLine($"[ZOC] {zocUnit.Type} hits {movingUnit.Type} for {dmg} damage! Remaining HP: {movingUnit.Health}");

                // ZOC usually cannot kill, leaves at 1 HP
                if (movingUnit.Health <= 0)
                {
                    movingUnit.Health = 1;
                }
            }
            else
            {
                Console.WriteLine($"[ZOC] {zocUnit.Type} fired but missed!");
            }
        }
    }

    private bool HasZoneOfControl(Unit u)
    {
        if (u.Domain != UnitDomain.Land) return false;
        if (u.MaxMovement > 1) return true;
        if (u.Type == UnitType.Infantry || u.Type == UnitType.MechInfantry || u.Type == UnitType.TOWInfantry) return true;
        return false;
    }

    private void ExploreGoodyHut(Unit unit, int x, int y)
    {
        GoodyHuts.Remove((x, y));
        if (unit.Faction != Faction.Player) return;

        var rand = new Random(TurnNumber * (x+1) * (y+1));
        int roll = rand.Next(100);

        bool isExpansionist = unit.Faction == Faction.Player && (PlayerCiv.Trait1 == CivTrait.Expansionist || PlayerCiv.Trait2 == CivTrait.Expansionist);

        if (roll < 20 && !isExpansionist)
        {
            // Barbarians
            Console.WriteLine("[Goody Hut] Ambush! Barbarians sprang from the village!");
            BarbarianCamps.Add((x, y));
            for(int i=0; i<2; i++) {
                string barbId = $"barb_{Guid.NewGuid().ToString().Substring(0, 8)}";
                Units.Add(new Unit(barbId, UnitType.Barbarian, x, y, Faction.Barbarian));
            }
        }
        else if (roll < 45 || (roll < 20 && isExpansionist))
        {
            // Gold
            int gold = rand.Next(25, 101);
            PlayerTreasury += gold;
            Console.WriteLine($"[Goody Hut] You found {gold} gold in a deserted village!");
        }
        else if (roll < 70)
        {
            // Map
            Console.WriteLine("[Goody Hut] The villagers provided you with a map of the surrounding area!");
            int revealRadius = 4;
            for (int dx = -revealRadius; dx <= revealRadius; dx++)
            {
                for (int dy = -revealRadius; dy <= revealRadius; dy++)
                {
                    int nx = Map.NormalizeX(x + dx);
                    int ny = y + dy;
                    if (ny >= 0 && ny < Map.Height)
                    {
                        if (Math.Abs(dx) + Math.Abs(dy) <= revealRadius * 1.5)
                        {
                            VisibilityGrid[nx, ny] = FogState.Visible;
                        }
                    }
                }
            }
        }
        else if (roll < 90)
        {
            // Unit
            Console.WriteLine("[Goody Hut] A skilled warrior joins your expedition!");
            string newId = $"warrior_{Guid.NewGuid().ToString().Substring(0, 8)}";
            Units.Add(new Unit(newId, UnitType.Warrior, x, y, Faction.Player));
        }
        else
        {
            // Tech (simplified)
            Console.WriteLine("[Goody Hut] The villagers shared their knowledge of a new technology!");
            LastTurnScience += 50; 
        }
    }

    public bool IsHostile(Unit a, Unit b)
    {
        if (a.Faction == b.Faction) return false;
        if (a.Faction == Faction.Barbarian || b.Faction == Faction.Barbarian) return true;
        if (a.HasHiddenNationality || b.HasHiddenNationality) return true;

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
        if (unit.HasHiddenNationality) return;

        var tile = Map.GetTile(targetX, targetY);
        if (tile == null || string.IsNullOrEmpty(tile.OwnerCityId)) return;

        var city = Cities.FirstOrDefault(c => c.Id == tile.OwnerCityId);
        if (city == null) return;

        // If a player unit enters AI territory and we are at peace
        if (unit.Faction == Faction.Player && city.Faction == Faction.AiRival && !IsAtWarWithAi)
        {
            IsAtWarWithAi = true;
            OnWarDeclared(isDefensive: false); // Player initiated war
            Console.WriteLine($"[Diplomacy] WAR declared! Player unit {unit.Type} intruded into {city.Name}'s territory at ({targetX}, {targetY})!");
        }
        // If an AI unit enters player territory and we are at peace
        else if (unit.Faction == Faction.AiRival && city.Faction == Faction.Player && !IsAtWarWithAi)
        {
            IsAtWarWithAi = true;
            OnWarDeclared(isDefensive: true); // AI attacked us - defensive war
            Console.WriteLine($"[Diplomacy] WAR declared! AI unit {unit.Type} intruded into {city.Name}'s territory at ({targetX}, {targetY})!");
        }
    }

    public void ResolveCombat(Unit attacker, Unit defender)
    {
        Console.WriteLine($"[Combat] {attacker.Type} (HP: {attacker.Health}) attacks {defender.Type} (HP: {defender.Health}) at ({defender.X}, {defender.Y})!");

        // Defensive Bombardment
        // Find units on the same tile as defender that have bombard strength (and haven't defended yet, but we'll just let all artillery do it once per combat).
        var bombardiers = Units.Where(u => u.Faction == defender.Faction && u.X == defender.X && u.Y == defender.Y && u.BombardStrength > 0 && u.Id != defender.Id).ToList();
        foreach (var bombardier in bombardiers)
        {
            if (attacker.Health <= 1) break;
            
            float defBombardStr = bombardier.BombardStrength;
            float attDefStr = Math.Max(1f, attacker.DefenseStrength);
            if (Random.Shared.NextDouble() < (defBombardStr / (defBombardStr + attDefStr)))
            {
                int dmg = Math.Clamp((int)(15f * defBombardStr / attDefStr), 5, 25);
                attacker.Health = Math.Max(1, attacker.Health - dmg);
                Console.WriteLine($"[Defensive Bombardment] {bombardier.Type} fired a defensive shot at {attacker.Type} for {dmg} damage! Attacker HP: {attacker.Health}");
            }
        }

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

            if (defTile.HasRiver)
            {
                defStrength *= 1.25f;
                Console.WriteLine($"[Combat] {defender.Type} receives +25% defense bonus from River!");
            }
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
        // Retreat logic for fast units (Civ3)
        if (attacker.Health <= 0 && attacker.MaxMovement > 1 && defender.MaxMovement <= 1 && defender.Health > 0)
        {
            attacker.Health = 1;
            Console.WriteLine($"[Combat] {attacker.Type} retreats from combat with 1 HP remaining!");
        }

        bool defenderDied = defender.Health <= 0;
        bool attackerDied = attacker.Health <= 0;

        if (defenderDied)
        {
            Console.WriteLine($"[Combat] {defender.Type} has been destroyed!");
            DestroyUnit(defender);

            // War weariness: +2 when losing a unit
            if (defender.Faction == Faction.Player)
                AddWarWearinessPoints(2);

            // Attacker advances onto the defender's tile upon victory and is registered as victorious
            if (!attackerDied)
            {
                var tile = Map.GetTile(defender.X, defender.Y)!;
                float moveCost = GetTileMovementCostForUnit(tile, attacker);
                attacker.MoveTo(defender.X, defender.Y, moveCost);
                
                // Synchronize cargo positions if attacker is a transport
                if (attacker.LoadedUnitIds.Count > 0)
                {
                    foreach (var cargoId in attacker.LoadedUnitIds)
                    {
                        var cargo = Units.Find(u => u.Id == cargoId);
                        if (cargo != null)
                        {
                            cargo.X = defender.X;
                            cargo.Y = defender.Y;
                        }
                    }
                }

                Console.WriteLine($"[Combat] {attacker.Type} wins and advances to ({defender.X}, {defender.Y})!");
                
                HandleCombatVictory(attacker);
            }
        }

        if (attackerDied)
        {
            Console.WriteLine($"[Combat] {attacker.Type} has been destroyed in battle!");
            DestroyUnit(attacker);

            // War weariness: +2 when losing a unit
            if (attacker.Faction == Faction.Player)
                AddWarWearinessPoints(2);
            
            // Defender wins and is registered as victorious
            if (!defenderDied)
            {
                HandleCombatVictory(defender);
            }
        }

        // Combat consumes movement points. Blitz units and Armies consume 1 point, others consume all.
        if (!attackerDied)
        {
            // Simple blitz check: Army or Tank/ModernArmor/MechInfantry
            bool hasBlitz = attacker.Type == UnitType.Army || 
                            attacker.Type == UnitType.Tank || 
                            attacker.Type == UnitType.Panzer || 
                            attacker.Type == UnitType.ModernArmor || 
                            attacker.Type == UnitType.MechInfantry;

            if (hasBlitz)
            {
                attacker.RemainingMovement = Math.Max(0, attacker.RemainingMovement - 1.0f);
            }
            else
            {
                attacker.RemainingMovement = 0;
            }
        }
    }

    private void HandleCombatVictory(Unit unit)
    {
        FactionsWithVictoriousUnit.Add(unit.Faction);
        CheckUniqueUnitGoldenAgeTrigger(unit);

        // Promotion
        if (unit.Experience != ExperienceLevel.Elite)
        {
            bool militaristic = unit.Civilization != null && 
                (unit.Civilization.Trait1 == CivTrait.Militaristic || unit.Civilization.Trait2 == CivTrait.Militaristic);
            
            int promoteChance = unit.Experience == ExperienceLevel.Veteran ? 25 : 50;
            if (militaristic) promoteChance = Math.Min(100, promoteChance * 2);

            if (Random.Shared.Next(100) < promoteChance)
            {
                unit.Experience++;
                unit.MaxHealth += 20; 
                unit.Health += 20;
                Console.WriteLine($"[Promotion] {unit.Type} promoted to {unit.Experience}!");
            }
        }
        else
        {
            // Elite -> Great Leader spawn check
            if (!unit.HasGeneratedLeader)
            {
                bool hasHeroicEpic = Cities.Any(c => c.Faction == unit.Faction && c.Buildings.Any(b => b.Id == "heroic_epic"));
                int leaderChance = hasHeroicEpic ? 8 : 6;

                bool hasLeader = Units.Any(u => u.Faction == unit.Faction && u.Type == UnitType.GreatLeader);

                if (!hasLeader && Random.Shared.Next(100) < leaderChance)
                {
                    unit.HasGeneratedLeader = true;
                    var leader = new Unit(Guid.NewGuid().ToString(), UnitType.GreatLeader, unit.X, unit.Y, unit.Faction, unit.CivilizationId);
                    Units.Add(leader);
                    Console.WriteLine($"[Great Leader] A Great Leader has emerged for {unit.Faction} at ({unit.X},{unit.Y})!");
                }
            }
        }
    }

    public void DestroyUnit(Unit unit)
    {
        if (unit.LoadedUnitIds.Count > 0)
        {
            var cargoUnits = Units.Where(u => unit.LoadedUnitIds.Contains(u.Id)).ToList();
            foreach (var cargo in cargoUnits)
            {
                Console.WriteLine($"[Transport] Cargo {cargo.Type} sank or was destroyed aboard {unit.Type}!");
                Units.Remove(cargo);
            }
            unit.LoadedUnitIds.Clear();
        }

        // If this unit was cargo inside another transport, remove it from the transport's loaded list
        if (unit.TransportUnitId != null)
        {
            var transport = Units.Find(u => u.Id == unit.TransportUnitId);
            transport?.LoadedUnitIds.Remove(unit.Id);
            unit.TransportUnitId = null;
        }

        Units.Remove(unit);
    }

    public bool CreateArmy(Unit leader)
    {
        if (leader.Type != UnitType.GreatLeader) return false;
        
        var army = new Unit(leader.Id, UnitType.Army, leader.X, leader.Y, leader.Faction, leader.CivilizationId);
        Units.Remove(leader);
        Units.Add(army);
        
        Console.WriteLine($"[Great Leader] {leader.Faction} formed an Army at ({leader.X},{leader.Y})!");
        return true;
    }

    public bool HurryProductionWithLeader(Unit leader, City city)
    {
        if (leader.Type != UnitType.GreatLeader) return false;
        if (city.Faction != leader.Faction) return false;
        if (leader.X != city.X || leader.Y != city.Y) return false;

        if (city.CurrentProject == ProductionProject.None) return false;

        city.CurrentProductionProgress = city.GetProjectCost(city.CurrentProject);
        Console.WriteLine($"[Great Leader] {leader.Faction}'s Great Leader rushed production of {city.CurrentProject} in {city.Name}!");
        
        Units.Remove(leader);
        return true;
    }

    public bool IsHostileToFaction(Faction a, Faction b)
    {
        if (a == b) return false;
        if (a == Faction.Barbarian || b == Faction.Barbarian) return true;
        return IsAtWarWithAi;
    }

    public bool LoadUnitIntoTransport(Unit cargo, Unit transport)
    {
        if (!cargo.CanBoardTransport(transport, this)) return false;
        int dist = Map.GetChebyshevDistance(cargo.X, cargo.Y, transport.X, transport.Y);
        if (dist > 1) return false;

        cargo.BoardTransport(transport, this);
        cargo.RemainingMovement = 0;
        UpdateVisibility();
        return true;
    }

    public bool UnloadUnitFromTransport(Unit cargo, int targetX, int targetY)
    {
        if (cargo.TransportUnitId == null) return false;
        var transport = Units.Find(u => u.Id == cargo.TransportUnitId);
        if (transport == null) return false;

        if (!Map.IsInBounds(targetX, targetY)) return false;
        targetX = Map.NormalizeX(targetX);
        int dist = Map.GetChebyshevDistance(targetX, targetY, transport.X, transport.Y);
        if (dist > 1) return false;

        var targetTile = Map.GetTile(targetX, targetY);
        if (targetTile == null || !targetTile.IsLand) return false;

        cargo.Disembark(targetX, targetY, transport);
        cargo.RemainingMovement = 0;
        UpdateVisibility();
        return true;
    }

    public void CheckNavalSinkingAtTurnEnd()
    {
        var vulnerableShips = Units.Where(u => u.SinksInSeaOcean).ToList();
        foreach (var ship in vulnerableShips)
        {
            var tile = Map.GetTile(ship.X, ship.Y);
            if (tile == null) continue;

            bool isSea = tile.Terrain.Id == "sea";
            bool isOcean = tile.Terrain.Id == "ocean";

            if (isSea || isOcean)
            {
                bool isSeafaring = ship.Civilization != null &&
                    (ship.Civilization.Trait1 == CivTrait.Seafaring || ship.Civilization.Trait2 == CivTrait.Seafaring);

                int sinkChance = isOcean ? (isSeafaring ? 35 : 60) : (isSeafaring ? 15 : 35);
                if (Random.Shared.Next(100) < sinkChance)
                {
                    Console.WriteLine($"[Naval Sinking] {ship.Type} ({ship.Faction}) sank in treacherous waters of {tile.Terrain.Name} at ({ship.X}, {ship.Y})!");
                    DestroyUnit(ship);
                }
            }
        }
    }
    public bool ExecutePillage(Unit unit)
    {
        if (unit.RemainingMovement <= 0) return false;
        var tile = Map.GetTile(unit.X, unit.Y);
        if (tile == null) return false;

        bool pillaged = false;
        // Pillage priority: Road -> Irrigation/Mine -> Base
        if (tile.HasRoad)
        {
            tile.HasRoad = false;
            pillaged = true;
        }
        else if (tile.Improvement != null)
        {
            tile.Improvement = null;
            pillaged = true;
        }

        if (pillaged)
        {
            unit.RemainingMovement = 0;
            return true;
        }
        return false;
    }

    public bool ExecuteBombard(Unit unit, int targetX, int targetY)
    {
        if (unit.BombardRange <= 0 || !unit.HasMovementRemaining()) return false;
        if (!Map.IsInBounds(targetX, targetY)) return false;
        targetX = Map.NormalizeX(targetX);

        int dist = Map.GetChebyshevDistance(targetX, targetY, unit.X, unit.Y);
        if (dist > unit.BombardRange || dist == 0) return false;

        var targets = Units.Where(u => u.X == targetX && u.Y == targetY && u.TransportUnitId == null && IsHostile(unit, u)).ToList();
        var enemyCity = Cities.Find(c => c.X == targetX && c.Y == targetY && c.Faction != unit.Faction && IsHostileToFaction(unit.Faction, c.Faction));
        var targetTile = Map.GetTile(targetX, targetY);

        if (targets.Count == 0 && enemyCity == null && (targetTile == null || (!targetTile.HasRoad && !targetTile.HasRailroad && targetTile.Improvement == null)))
            return false;

        unit.RemainingMovement = 0;

        if (targets.Count > 0)
        {
            targets.Sort((a, b) => b.DefenseStrength.CompareTo(a.DefenseStrength));
            var defender = targets[0];

            float hitChance = (float)unit.BombardStrength / (unit.BombardStrength + Math.Max(1.0f, defender.DefenseStrength));
            hitChance = Math.Clamp(hitChance, 0.25f, 0.85f);

            bool hit = Random.Shared.NextDouble() < hitChance;
            if (hit)
            {
                int dmg = Math.Clamp((int)(22.0f * ((float)unit.BombardStrength / Math.Max(1.0f, defender.DefenseStrength))), 12, 35);
                defender.Health -= dmg;
                Console.WriteLine($"[Bombard] {unit.Type} hits {defender.Type} at ({targetX}, {targetY}) for {dmg} damage! (Remaining HP: {defender.Health})");

                bool hasLethalBombard = unit.Domain == UnitDomain.Air || unit.Domain == UnitDomain.Sea || unit.Type == UnitType.CruiseMissile;
                if (defender.Health <= 0)
                {
                    if (hasLethalBombard)
                    {
                        Console.WriteLine($"[Bombard] {defender.Type} destroyed by lethal bombardment!");
                        DestroyUnit(defender);
                    }
                    else
                    {
                        defender.Health = 1;
                    }
                }
            }
            else
            {
                Console.WriteLine($"[Bombard] {unit.Type} bombarded ({targetX}, {targetY}) but missed.");
            }
        }
        else if (enemyCity != null)
        {
            Console.WriteLine($"[Bombard] {unit.Type} bombarded enemy city {enemyCity.Name}!");
            if (enemyCity.Population > 1 && Random.Shared.Next(2) == 0)
            {
                enemyCity.Population--;
                Console.WriteLine($"[Bombard] Citizen casualty in {enemyCity.Name}! Population reduced to {enemyCity.Population}.");
            }
            else if (enemyCity.Buildings.Count > 0 && Random.Shared.Next(2) == 0)
            {
                var nonPalace = enemyCity.Buildings.Where(b => !(b is Palace)).ToList();
                if (nonPalace.Count > 0)
                {
                    var ruined = nonPalace[Random.Shared.Next(nonPalace.Count)];
                    enemyCity.Buildings.Remove(ruined);
                    Console.WriteLine($"[Bombard] Building destroyed in {enemyCity.Name}: {ruined.Name}!");
                }
            }
        }
        else if (targetTile != null)
        {
            if (targetTile.Improvement != null)
            {
                Console.WriteLine($"[Bombard] {unit.Type} destroyed {targetTile.Improvement.Name} at ({targetX}, {targetY})!");
                targetTile.Improvement = null;
            }
            else if (targetTile.HasRailroad)
            {
                Console.WriteLine($"[Bombard] {unit.Type} damaged railroad at ({targetX}, {targetY}) to road!");
                targetTile.HasRailroad = false;
            }
            else if (targetTile.HasRoad)
            {
                Console.WriteLine($"[Bombard] {unit.Type} destroyed road at ({targetX}, {targetY})!");
                targetTile.HasRoad = false;
            }
        }

        UpdateVisibility();
        return true;
    }

    public bool ExecuteAirRebase(Unit airUnit, int targetX, int targetY)
    {
        if (airUnit.Domain != UnitDomain.Air || !airUnit.HasMovementRemaining()) return false;
        targetX = Map.NormalizeX(targetX);
        int dist = Map.GetChebyshevDistance(targetX, targetY, airUnit.X, airUnit.Y);
        if (dist > airUnit.OperationalRange * 2) return false;

        bool validBase = Cities.Any(c => c.X == targetX && c.Y == targetY && c.Faction == airUnit.Faction);
        if (!validBase)
        {
            var carrier = Units.Find(u => u.X == targetX && u.Y == targetY && u.Faction == airUnit.Faction && u.Type == UnitType.Carrier && u.LoadedUnitIds.Count < u.TransportCapacity);
            if (carrier != null)
            {
                validBase = true;
            }
        }

        if (!validBase) return false;

        airUnit.MoveTo(targetX, targetY, airUnit.MaxMovement);
        airUnit.RemainingMovement = 0;
        UpdateVisibility();
        Console.WriteLine($"[Air Rebase] {airUnit.Type} rebased to ({targetX}, {targetY})!");
        return true;
    }

    public bool ExecuteAirRecon(Unit airUnit, int targetX, int targetY)
    {
        if (airUnit.Domain != UnitDomain.Air || !airUnit.HasMovementRemaining()) return false;
        targetX = Map.NormalizeX(targetX);
        int dist = Map.GetChebyshevDistance(targetX, targetY, airUnit.X, airUnit.Y);
        if (dist > airUnit.OperationalRange) return false;

        int reconRadius = 2;
        for (int dx = -reconRadius; dx <= reconRadius; dx++)
        {
            for (int dy = -reconRadius; dy <= reconRadius; dy++)
            {
                int tx = Map.NormalizeX(targetX + dx);
                int ty = targetY + dy;
                if (ty >= 0 && ty < Map.Height)
                {
                    if (airUnit.Faction == Faction.Player)
                    {
                        VisibilityGrid[tx, ty] = FogState.Visible;
                    }
                }
            }
        }
        airUnit.RemainingMovement = 0;
        Console.WriteLine($"[Air Recon] {airUnit.Type} conducted reconnaissance over ({targetX}, {targetY})!");
        return true;
    }

    public bool ExecuteAirStrike(Unit airUnit, int targetX, int targetY)
    {
        if (airUnit.Domain != UnitDomain.Air || !airUnit.HasMovementRemaining()) return false;
        if (!Map.IsInBounds(targetX, targetY)) return false;
        targetX = Map.NormalizeX(targetX);

        int dist = Map.GetChebyshevDistance(targetX, targetY, airUnit.X, airUnit.Y);
        if (dist > airUnit.OperationalRange || dist == 0) return false;

        // Check for interception: is there an enemy interceptor or anti-air unit nearby?
        var interceptor = Units.FirstOrDefault(u => u.TransportUnitId == null && IsHostile(airUnit, u) &&
            (u.Type == UnitType.Fighter || u.Type == UnitType.JetFighter || u.Type == UnitType.F15 || u.Type == UnitType.Flak || u.Type == UnitType.MobileSAM || u.Type == UnitType.AegisCruiser) &&
            Map.GetChebyshevDistance(u.X, u.Y, targetX, targetY) <= 2);

        if (interceptor != null && !airUnit.IsStealth)
        {
            float interceptChance = (interceptor.Type == UnitType.JetFighter || interceptor.Type == UnitType.F15 || interceptor.Type == UnitType.MobileSAM) ? 0.65f : 0.45f;
            if (Random.Shared.NextDouble() < interceptChance)
            {
                Console.WriteLine($"[Air Interception] {interceptor.Type} intercepted {airUnit.Type}!");
                int dmg = Math.Clamp((int)(25.0f * (interceptor.DefenseStrength / Math.Max(1.0f, airUnit.DefenseStrength))), 15, 40);
                airUnit.Health -= dmg;
                airUnit.RemainingMovement = 0;
                if (airUnit.Health <= 0)
                {
                    Console.WriteLine($"[Air Interception] {airUnit.Type} was shot down!");
                    DestroyUnit(airUnit);
                    return true;
                }
                Console.WriteLine($"[Air Interception] {airUnit.Type} aborted mission due to enemy interception!");
                return true;
            }
        }

        bool struck = ExecuteBombard(airUnit, targetX, targetY);

        if (airUnit.Type == UnitType.CruiseMissile)
        {
            Console.WriteLine($"[Missile] {airUnit.Type} was expended in strike.");
            DestroyUnit(airUnit);
        }
        else if (airUnit.Type == UnitType.TacticalNuke || airUnit.Type == UnitType.ICBM)
        {
            Console.WriteLine($"[Nuclear Strike] Tactical Nuke detonated at ({targetX}, {targetY})!");
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = targetX + dx;
                    int ny = targetY + dy;
                    var blastUnits = Units.Where(u => u.X == nx && u.Y == ny).ToList();
                    foreach (var bu in blastUnits)
                    {
                        bu.Health -= 60;
                        if (bu.Health <= 0) DestroyUnit(bu);
                    }
                }
            }
            DestroyUnit(airUnit);
        }

        return struck;
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
            minDist = Math.Min(minDist, Civ3Distance(city, capital));
        }

        // Distance to Forbidden Palace cities (secondary capitals for corruption)
        foreach (var fpCity in Cities.Where(c => c.Faction == city.Faction && c.Buildings.Any(b => b.Id == "forbidden_palace")))
        {
            minDist = Math.Min(minDist, Civ3Distance(city, fpCity));
        }

        return minDist == int.MaxValue ? 0 : minDist;
    }

    /// <summary>
    /// Civ3 distance formula: d = max(dx,dy) + min(dx,dy)/2
    /// This gives a value between Manhattan and Chebyshev distance.
    /// </summary>
    private int Civ3Distance(City a, City b)
    {
        return Map.GetCiv3Distance(a.X, a.Y, b.X, b.Y);
    }

    public bool HasFreshWaterAccess(City city)
    {
        // Check adjacent tiles (including city center tile itself)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int tx = Map.NormalizeX(city.X + dx);
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

    /// <summary>
    /// Civ3 irrigation chain: A Farm can only be built on a tile adjacent to:
    /// 1. Fresh water (coast, floodplains, river), OR
    /// 2. A city (cities act as irrigation sources), OR
    /// 3. Another tile that already has a Farm (irrigation chain)
    /// After discovering Electricity, irrigation can be built anywhere.
    /// </summary>
    public bool HasIrrigationAccess(int x, int y)
    {
        // After Electricity: irrigate anywhere without fresh water requirement
        if (Research.IsResearched("electricity"))
            return true;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue; // Skip self
                int nx = Map.NormalizeX(x + dx);
                int ny = y + dy;
                if (!Map.IsInBounds(nx, ny)) continue;

                var adj = Map.GetTile(nx, ny);
                if (adj == null) continue;

                // Fresh water source: coast, floodplains, river
                if (adj.Terrain.Id == "coast" || adj.Terrain.Id == "floodplains" || adj.HasRiver)
                    return true;

                // City acts as irrigation source
                if (Cities.Any(c => c.X == nx && c.Y == ny))
                    return true;

                // Irrigation chain: adjacent tile has a Farm
                if (adj.Improvement is Farm)
                    return true;
            }
        }
        return false;
    }

    private void CheckCityCapture(Unit unit, int x, int y)
    {
        // Only military units can capture cities
        if (unit.AttackStrength <= 0) return;

        var city = Cities.Find(c => c.X == x && c.Y == y);
        if (city == null) return;

        // Check if it's an enemy city
        if (city.Faction != unit.Faction)
        {
            if (unit.Faction == Faction.Barbarian)
            {
                ApplyCityCaptureChoice(city, unit, CityCaptureChoice.Raze);
            }
            else if (unit.Faction == Faction.Player)
            {
                if (OnCityCapturePrompt != null)
                {
                    OnCityCapturePrompt.Invoke(city, unit);
                }
                else
                {
                    ApplyCityCaptureChoice(city, unit, CityCaptureChoice.Keep);
                }
            }
            else // AI Rival
            {
                // AI keeps cities unless it is a tiny pop 1 outpost without wonders and not capital
                bool shouldRaze = city.Population <= 1 && city.Buildings.Count <= 1 && !city.IsCapital;
                ApplyCityCaptureChoice(city, unit, shouldRaze ? CityCaptureChoice.Raze : CityCaptureChoice.Keep);
            }
        }
    }

    public void ApplyCityCaptureChoice(City city, Unit capturingUnit, CityCaptureChoice choice)
    {
        if (!Cities.Contains(city)) return;

        var oldFaction = city.Faction;
        bool wasCapital = city.IsCapital;

        if (choice == CityCaptureChoice.Raze)
        {
            Console.WriteLine($"[City Captured] Faction {capturingUnit.Faction} has razed the city of {city.Name} at ({city.X}, {city.Y})!");

            // 1. Reset ownership of tiles owned by this city
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

            // 2. Spoil of war: plunder treasury
            int plunderedGold = Math.Min(60, 15 + city.Population * 10);
            if (capturingUnit.Faction == Faction.Player)
            {
                PlayerTreasury += plunderedGold;
                Console.WriteLine($"[City Razed] Plundered {plunderedGold} gold from the ruins of {city.Name}!");
            }

            // 3. Civ3 mechanic: Enslave captured citizens into Workers
            if (capturingUnit.Faction != Faction.Barbarian)
            {
                int workersEnslaved = Math.Clamp(city.Population / 2, 1, 2);
                for (int i = 0; i < workersEnslaved; i++)
                {
                    var worker = new Unit(Guid.NewGuid().ToString(), UnitType.Worker, city.X, city.Y, capturingUnit.Faction);
                    worker.RemainingMovement = 0;
                    Units.Add(worker);
                }
                Console.WriteLine($"[City Razed] {workersEnslaved} captive worker(s) created from the population of {city.Name}!");
            }

            // 4. Remove city from simulation
            Cities.Remove(city);

            if (wasCapital)
            {
                RelocateCapital(oldFaction);
            }
        }
        else // Keep
        {
            city.Faction = capturingUnit.Faction;
            city.IsCapital = false; // Captured city is no longer the old faction's capital
            city.Buildings.RemoveAll(b => b is Palace);
            city.Buildings.RemoveAll(b => b is SmallWonder);

            // Clear city production progress upon capture
            city.CurrentProject = ProductionProject.None;
            city.StoredProduction = 0;
            city.CurrentProductionProgress = 0;

            // Reduce population by 1 (conquest penalty)
            city.Population = Math.Max(1, city.Population - 1);

            // Resisters / disorder turns in conquered city
            city.DisorderTurns = Math.Max(1, city.Population / 2);

            // Plunder small amount of gold upon capture
            int plunderedGold = Math.Min(35, 10 + city.Population * 5);
            if (capturingUnit.Faction == Faction.Player)
            {
                PlayerTreasury += plunderedGold;
            }

            Console.WriteLine($"[City Captured] Faction {capturingUnit.Faction} has captured the city of {city.Name} from {oldFaction} at ({city.X}, {city.Y})! ({city.DisorderTurns} resistance turns)");

            if (wasCapital)
            {
                RelocateCapital(oldFaction);
            }
        }

        UpdateUnitMovementBonuses();
        CalculateCulturalBorders();
        UpdateVisibility();
        CheckGameEndConditions();
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
                if (tile == null || !tile.IsLand) continue;
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
                if (tile == null || !tile.IsLand) continue;
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
        int playerEntertainmentRate,
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
        PlayerEntertainmentRate = playerEntertainmentRate;
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
            unit.SetExperience(dto.Experience);
            unit.Health = dto.Health; // preserve exact health after experience initialization
            unit.IsFortified = dto.IsFortified;
            unit.IsSleeping = dto.IsSleeping;
            unit.IsSentry = dto.IsSentry;
            unit.TransportUnitId = dto.TransportUnitId;
            unit.HasGeneratedLeader = dto.HasGeneratedLeader;
            if (dto.LoadedUnitIds != null)
            {
                unit.LoadedUnitIds.Clear();
                unit.LoadedUnitIds.AddRange(dto.LoadedUnitIds);
            }
            
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
            city.DisorderTurns = dto.DisorderTurns;
            city.IsWLTKD = dto.IsWLTKD;

            if (dto.Specialists != null && dto.Specialists.Count > 0)
            {
                city.Specialists.Clear();
                city.Specialists.AddRange(dto.Specialists);
            }

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
                int nx = Map.NormalizeX(cx + dxs[i]);
                int ny = cy + dys[i];

                if (!Map.IsInBounds(nx, ny)) continue;
                var tile = Map.GetTile(nx, ny);
                if (tile == null) continue;

                if (unit.Domain == UnitDomain.Sea)
                {
                    bool isWater = tile.IsOcean;
                    bool isFriendlyPort = Cities.Any(c => c.X == nx && c.Y == ny && c.Faction == unit.Faction && c.IsCoastal(Map));
                    if (!isWater && !isFriendlyPort) continue;
                }
                else if (unit.Domain == UnitDomain.Land)
                {
                    if (tile.IsOcean)
                    {
                        bool canBoard = Units.Any(u => u.X == nx && u.Y == ny && u.Faction == unit.Faction && u.Domain == UnitDomain.Sea && u.CanBoardTransport(unit, this));
                        if (!canBoard) continue;
                    }
                }
                else if (unit.Domain == UnitDomain.Air)
                {
                    continue;
                }

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
        if (Mode == GameMode.Hotseat)
        {
            EndHotseatTurn();
            return;
        }

        // 1. Process Barbarian action before starting next turn
        ProcessBarbarianTurn();

        // 2. Process AI Rival action
        ProcessAiRivalTurn();

        // 2.5 Process AI Research
        ProcessAiResearch();

        AdvanceWorldTurn();
    }

    public void EndHotseatTurn()
    {
        if (ActiveTurnFaction == Faction.Player)
        {
            // End Player 1's turn -> hand over to Player 2
            ActiveTurnFaction = Faction.AiRival;
            UpdateVisibility();
            OnHotseatTurnChange?.Invoke(Faction.AiRival);
        }
        else
        {
            // Player 2 finished turn -> simulate Barbarians, advance world simulation, cycle to Player 1
            ProcessBarbarianTurn();
            AdvanceWorldTurn();
            ActiveTurnFaction = Faction.Player;
            UpdateVisibility();
            OnHotseatTurnChange?.Invoke(Faction.Player);
        }
    }

    public void AdvanceWorldTurn()
    {
        // 3. Advance Worker Construction
        ProcessWorkerConstruction();

        // 3.5 Apply Battlefield Medicine healing effect
        HealWoundedUnits();

        // 3.5.5 Process Naval Sinking risks for early vessels in Sea/Ocean
        CheckNavalSinkingAtTurnEnd();

        // 3.6 Process Volcanic Eruptions (0.35% chance per turn per volcano)
        VolcanoSystem.ProcessEruptions(this);

        // 3.7 Process War Weariness (Civ3 mechanic)
        ProcessWarWeariness();

        // 3.8 Process Anarchy countdown — install pending government when anarchy ends
        if (AnarchyTurnsRemaining > 0)
        {
            AnarchyTurnsRemaining--;
            if (AnarchyTurnsRemaining <= 0 && PendingGovernment.HasValue)
            {
                PlayerGovernment = PendingGovernment.Value;
                var newGov = Government.Get(PlayerGovernment);
                Console.WriteLine($"[Government] Anarchy has ended! Your civilization is now a {newGov.Name}.");
                PendingGovernment = null;
            }
        }

        // 3.9 Process Golden Age countdown
        PlayerGoldenAge.ProcessTurn();
        AiGoldenAge.ProcessTurn();

        // 3.10 Record Histograph data for this turn
        Histograph.RecordTurn(this);

        // 3.11 Process active trade deals
        Diplomacy.ProcessTurnEnd();

        TurnNumber++;
        CalculateTradeNetworks();

        UpdateUnitMovementBonuses();

        foreach (var unit in Units)
        {
            unit.ResetMovement();
        }
        int scienceGenerated = CollectCityYields();
        Research.AddScience(scienceGenerated, this);

        ApplyGreatLibraryCatchup();
        
        CalculateCulturalBorders();
        CheckCulturalFlipping();
        ProcessDisease();

        ProcessAutomatedUnits();

        UpdateVisibility();

        // 4. Check for game end conditions
        CheckGameEndConditions();
    }

    private void ProcessDisease()
    {
        foreach (var city in Cities)
        {
            city.HasDisease = false;
            if (city.Population > 1 && !city.HasBuilding<Hospital>())
            {
                // Check for adjacent Jungle or Floodplains
                bool hasDiseaseSource = false;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var tile = Map.GetTile(city.X + dx, city.Y + dy);
                        if (tile != null && (tile.Terrain.Id == "jungle" || tile.Terrain.Id == "floodplains"))
                        {
                            hasDiseaseSource = true;
                            break;
                        }
                    }
                    if (hasDiseaseSource) break;
                }

                if (hasDiseaseSource)
                {
                    // 5% chance per turn per vulnerable city
                    if (Random.Shared.NextDouble() < 0.05)
                    {
                        city.HasDisease = true;
                        city.Population -= 1;
                        city.StoredFood = 0;
                        Console.WriteLine($"[DISEASE] Disease struck the city of {city.Name}, killing 1 population!");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Civ3 War Weariness calculation:
    /// - +1 WWP per turn with units in enemy territory
    /// - +2 WWP per unit lost (handled in combat code)
    /// - Defensive war starts at -30 WWP (effectively delayed)
    /// - In peace: decay 1/20 of current WWP per turn (rounded up)
    /// - Police Station reduces EFFECT by 25% (not the points)
    /// </summary>
    private void ProcessWarWeariness()
    {
        var gov = Government.Get(PlayerGovernment);

        if (IsAtWarWithAi)
        {
            PeaceTurns = 0;

            // +1 WWP if player has any unit in AI territory
            bool hasUnitsInEnemyTerritory = Units
                .Where(u => u.Faction == Faction.Player)
                .Any(u =>
                {
                    var tile = Map.GetTile(u.X, u.Y);
                    if (tile?.OwnerCityId == null) return false;
                    var ownerCity = Cities.FirstOrDefault(c => c.Id == tile.OwnerCityId);
                    return ownerCity != null && ownerCity.Faction == Faction.AiRival;
                });

            if (hasUnitsInEnemyTerritory)
                WarWearinessPoints++;

            // If no enemy in our territory and no units in enemy territory, -1 WWP (minimum 0)
            bool enemyInOurTerritory = Units
                .Where(u => u.Faction == Faction.AiRival)
                .Any(u =>
                {
                    var tile = Map.GetTile(u.X, u.Y);
                    if (tile?.OwnerCityId == null) return false;
                    var ownerCity = Cities.FirstOrDefault(c => c.Id == tile.OwnerCityId);
                    return ownerCity != null && ownerCity.Faction == Faction.Player;
                });

            if (!hasUnitsInEnemyTerritory && !enemyInOurTerritory && GetWarWearinessLevel() >= 1)
                WarWearinessPoints = Math.Max(0, WarWearinessPoints - 1);
        }
        else
        {
            // Peace: decay 1/20 of current WWP per turn (rounded up)
            if (WarWearinessPoints > 0)
            {
                PeaceTurns++;
                int decay = Math.Max(1, (int)Math.Ceiling(WarWearinessPoints / 20.0));
                WarWearinessPoints = Math.Max(0, WarWearinessPoints - decay);
            }
        }
    }

    /// <summary>
    /// Get war weariness level (0-4) based on accumulated WWP.
    /// </summary>
    public int GetWarWearinessLevel()
    {
        if (WarWearinessPoints <= 0) return 0;
        if (WarWearinessPoints <= 30) return 0;
        if (WarWearinessPoints <= 60) return 1;
        if (WarWearinessPoints <= 90) return 2;
        if (WarWearinessPoints <= 120) return 3;
        return 4;
    }

    /// <summary>
    /// Calculate number of unhappy citizens caused by war weariness in a city.
    /// Depends on government type and WWP level.
    /// Republic: Level 1=25%, 2=50%, 3=50%, 4=100%
    /// Democracy: Level 1=50%, 2=100%, 3=REVOLT
    /// Police Station reduces by 25%.
    /// </summary>
    public int GetWarWearinessUnhappiness(City city)
    {
        var govType = city.Faction == Faction.Player ? PlayerGovernment : AiGovernment;
        var gov = Government.Get(govType);
        if (!gov.HasWarWeariness || !IsAtWarWithAi) return 0;

        int level = GetWarWearinessLevel();
        if (level <= 0) return 0;

        float unhappyPercent = 0f;
        if (gov.WarWearinessSeverity == 1) // Republic
        {
            unhappyPercent = level switch
            {
                1 => 0.25f,
                2 => 0.50f,
                3 => 0.50f,
                _ => 1.00f
            };
        }
        else if (gov.WarWearinessSeverity == 2) // Democracy
        {
            unhappyPercent = level switch
            {
                1 => 0.50f,
                2 => 1.00f,
                _ => 1.00f // Level 3+ = revolt/anarchy (handled elsewhere)
            };
        }

        int unhappy = (int)(city.Population * unhappyPercent);

        // Police Station reduces war weariness effect by 25%
        if (city.Buildings.Any(b => b.Id == "police_station"))
            unhappy = (int)(unhappy * 0.75f);

        return unhappy;
    }

    /// <summary>
    /// Add war weariness points (e.g. +2 when losing a unit, +15 when city razed).
    /// Call from combat resolution code.
    /// </summary>
    public void AddWarWearinessPoints(int points)
    {
        if (IsAtWarWithAi)
            WarWearinessPoints += points;
    }

    /// <summary>
    /// Called when war is declared. Sets up initial WWP state.
    /// If defensive (AI attacked us), we get -30 initial offset.
    /// </summary>
    public void OnWarDeclared(bool isDefensive)
    {
        WarStartTurn = TurnNumber;
        IsDefensiveWar = isDefensive;
        PeaceTurns = 0;
        if (isDefensive)
            WarWearinessPoints = Math.Max(WarWearinessPoints - 30, -30);
    }

    /// <summary>
    /// Called when peace is signed.
    /// </summary>
    public void OnPeaceSigned()
    {
        IsAtWarWithAi = false;
        PeaceTurns = 0;
    }

    /// <summary>
    /// Check if the player can start a revolution to change government.
    /// Cannot change during anarchy or to the same government.
    /// </summary>
    public bool CanChangeGovernment(GovernmentType newType)
    {
        if (IsInAnarchy) return false;
        if (newType == PlayerGovernment) return false;
        var gov = Government.Get(newType);
        if (gov.RequiredTechId != null && !Research.IsResearched(gov.RequiredTechId)) return false;
        return true;
    }

    /// <summary>
    /// Start a revolution to change government. Triggers anarchy for a random number of turns.
    /// Religious trait reduces anarchy to 1 turn.
    /// </summary>
    public bool ChangeGovernment(GovernmentType newType)
    {
        if (!CanChangeGovernment(newType)) return false;

        var targetGov = Government.Get(newType);

        // Religious trait: anarchy lasts only 1 turn
        bool isReligious = PlayerCiv.Trait1 == CivTrait.Religious || PlayerCiv.Trait2 == CivTrait.Religious;

        if (targetGov.CausesAnarchy && PlayerGovernment != GovernmentType.Despotism)
        {
            if (isReligious)
            {
                AnarchyTurnsRemaining = 1;
            }
            else
            {
                var rng = new Random();
                AnarchyTurnsRemaining = rng.Next(targetGov.AnarchyMinTurns, targetGov.AnarchyMaxTurns + 1);
            }
            PendingGovernment = newType;
            Console.WriteLine($"[Government] Revolution! Anarchy will last {AnarchyTurnsRemaining} turn(s). Transitioning to {targetGov.Name}...");
        }
        else
        {
            // No anarchy when leaving Despotism or if not CausesAnarchy
            PlayerGovernment = newType;
            Console.WriteLine($"[Government] Government changed to {targetGov.Name}.");
        }

        return true;
    }

    /// <summary>
    /// Get list of governments the player has unlocked (has required tech).
    /// </summary>
    public GovernmentType[] GetAvailableGovernments()
    {
        return Government.All
            .Where(g =>
            {
                var gov = Government.Get(g);
                return gov.RequiredTechId == null || Research.IsResearched(gov.RequiredTechId);
            })
            .ToArray();
    }

    /// <summary>
    /// Check if a completed wonder triggers a Golden Age for the building faction.
    /// A wonder triggers GA if it has AssociatedTraits matching BOTH traits of the civ.
    /// </summary>
    public void CheckWonderGoldenAgeTrigger(Wonder wonder, Faction faction)
    {
        if (wonder.AssociatedTraits.Length < 2) return;

        var civ = faction == Faction.Player ? PlayerCiv : AiCiv;
        var ga = faction == Faction.Player ? PlayerGoldenAge : AiGoldenAge;

        if (ga.HasBeenUsed) return;

        // Check if wonder's traits contain BOTH of the civ's traits
        bool matchesTrait1 = wonder.AssociatedTraits.Contains(civ.Trait1);
        bool matchesTrait2 = wonder.AssociatedTraits.Contains(civ.Trait2);

        if (matchesTrait1 && matchesTrait2)
        {
            ga.Trigger(civ.Name, $"Built {wonder.Name} (matches {civ.Trait1} + {civ.Trait2})");
        }
    }

    /// <summary>
    /// Check if a Unique Unit combat victory triggers a Golden Age.
    /// First UU victory triggers the GA for that faction.
    /// </summary>
    public void CheckUniqueUnitGoldenAgeTrigger(Unit winner)
    {
        var ga = winner.Faction == Faction.Player ? PlayerGoldenAge : AiGoldenAge;
        if (ga.HasBeenUsed) return;

        var civ = winner.Faction == Faction.Player ? PlayerCiv : AiCiv;

        // Check if this unit is a Unique Unit (has bonuses from its civ)
        bool isUniqueUnit = winner.CivilizationId == civ.Id &&
            winner.Type == civ.ReplacedUnitType &&
            (civ.UniqueUnitAttackBonus > 0 || civ.UniqueUnitDefenseBonus > 0 || civ.UniqueUnitMovementBonus > 0);

        if (isUniqueUnit)
        {
            ga.Trigger(civ.Name, $"{civ.UniqueUnitName} won in combat");
        }
    }

    /// <summary>
    /// Get the count of distinct luxury resources connected to player cities.
    /// Used for happiness calculation.
    /// </summary>
    public int GetConnectedLuxuryCount()
    {
        var luxuries = new HashSet<string>();
        foreach (var city in Cities.Where(c => c.Faction == Faction.Player))
        {
            foreach (var lux in GetCityConnectedLuxuries(city))
            {
                luxuries.Add(lux);
            }
        }
        return luxuries.Count;
    }

    /// <summary>
    /// Get the set of luxury resource IDs connected to a specific city via Trade Networks.
    /// </summary>
    public HashSet<string> GetCityConnectedLuxuries(City city)
    {
        var luxuries = new HashSet<string>();
        if (CityTradeNetworks.TryGetValue(city.Id, out int networkId))
        {
            if (TradeNetworkResources.TryGetValue(networkId, out var resources))
            {
                foreach (var resId in resources)
                {
                    var res = ResourceRegistry.Get(resId);
                    if (res != null && res.Category == ResourceCategory.Luxury)
                    {
                        luxuries.Add(resId);
                    }
                }
            }
        }
        return luxuries;
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
                // Apply government worker efficiency
                var workerGov = Government.Get(unit.Faction == Faction.Player ? PlayerGovernment : AiGovernment);
                int efficiency = workerGov.WorkerEfficiency;

                // 50% = work every other turn; 100% = normal; 150% = extra work every 2nd turn; 200% = double
                if (efficiency <= 50 && TurnNumber % 2 != 0)
                {
                    // Skip this turn (half speed)
                    continue;
                }

                int progress = 1;
                if (efficiency >= 200) progress = 2;
                else if (efficiency >= 150 && TurnNumber % 2 == 0) progress = 2;

                unit.ConstructionTurnsRemaining -= progress;
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
                        else if (improvement is ClearForest)
                        {
                            tile.Terrain = TerrainRegistry.Get("grassland")!;
                            tile.Improvement = null;
                            var nearestCity = Cities.Where(c => c.Faction == unit.Faction)
                                                    .OrderBy(c => Map.GetManhattanDistance(c.X, c.Y, unit.X, unit.Y))
                                                    .FirstOrDefault();
                            if (nearestCity != null)
                            {
                                nearestCity.CurrentProductionProgress += 10;
                                Console.WriteLine($"[Worker] Cleared forest at ({unit.X}, {unit.Y})! +10 Shields to {nearestCity.Name}.");
                            }
                        }
                        else if (improvement is ClearJungle)
                        {
                            tile.Terrain = TerrainRegistry.Get("grassland")!;
                            tile.Improvement = null;
                            Console.WriteLine($"[Worker] Cleared jungle at ({unit.X}, {unit.Y})!");
                        }
                        else if (improvement is ClearMarsh)
                        {
                            tile.Terrain = TerrainRegistry.Get("grassland")!;
                            tile.Improvement = null;
                            Console.WriteLine($"[Worker] Cleared marsh at ({unit.X}, {unit.Y})!");
                        }
                        else if (improvement is PlantForest)
                        {
                            tile.Terrain = TerrainRegistry.Get("forest")!;
                            tile.Improvement = null;
                            Console.WriteLine($"[Worker] Planted forest at ({unit.X}, {unit.Y})!");
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

    private void ProcessAutomatedUnits()
    {
        var rng = new Random();
        foreach (var unit in Units.ToList())
        {
            if (!unit.IsAutomated || unit.RemainingMovement <= 0) continue;

            // Get valid neighbors
            var neighbors = new List<(int X, int Y)>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = unit.X + dx;
                    int ny = unit.Y + dy;
                    if (nx >= 0 && nx < Map.Width && ny >= 0 && ny < Map.Height)
                    {
                        neighbors.Add((nx, ny));
                    }
                }
            }

            if (unit.Type == UnitType.Explorer || unit.Type == UnitType.Warrior || unit.Type == UnitType.Archer)
            {
                var unrevealed = neighbors.Where(t => VisibilityGrid[t.X, t.Y] == FogState.Unexplored).ToList();
                if (unrevealed.Count > 0)
                {
                    var target = unrevealed[rng.Next(unrevealed.Count)];
                    if (MoveUnit(unit, target.X, target.Y))
                    {
                        Console.WriteLine($"[Auto-Explore] {unit.Type} explored {target.X},{target.Y}");
                    }
                }
                else
                {
                    var validNeighbors = neighbors.Where(t => CanMoveUnit(unit, t.X, t.Y)).ToList();
                    if (validNeighbors.Count > 0)
                    {
                        var target = validNeighbors[rng.Next(validNeighbors.Count)];
                        MoveUnit(unit, target.X, target.Y);
                    }
                }
            }
            else if (unit.Type == UnitType.Worker)
            {
                var tile = Map.GetTile(unit.X, unit.Y);
                if (tile != null)
                {
                    if (!tile.HasRoad)
                    {
                        unit.StartImprovement(new RoadBuild());
                        Console.WriteLine($"[Auto-Worker] Started road at {unit.X},{unit.Y}");
                    }
                    else if (tile.Improvement == null && tile.Terrain.Id == "grassland")
                    {
                        if (HasIrrigationAccess(unit.X, unit.Y))
                        {
                            unit.StartImprovement(new Farm());
                            Console.WriteLine($"[Auto-Worker] Started farm at {unit.X},{unit.Y}");
                        }
                        else
                        {
                            unit.StartImprovement(new Mine());
                            Console.WriteLine($"[Auto-Worker] Started mine at {unit.X},{unit.Y}");
                        }
                    }
                    else
                    {
                        var validNeighbors = neighbors.Where(t => CanMoveUnit(unit, t.X, t.Y)).ToList();
                        if (validNeighbors.Count > 0)
                        {
                            var target = validNeighbors[rng.Next(validNeighbors.Count)];
                            MoveUnit(unit, target.X, target.Y);
                        }
                    }
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
            ApplyGreatLibraryCatchup(Faction.Player);
        }
    }

    private void ProcessBarbarianTurn()
    {
        // Spawn barbarians based on difficulty settings
        int barbInterval = DifficultyConfig.BarbarianSpawnInterval;
        if (barbInterval > 0 && TurnNumber % barbInterval == 0)
        {
            // Spawn new camps in shrouded/unexplored areas
            var rand = new Random(TurnNumber * 1234);
            int campSpawnChance = 10; // 10% chance to spawn a new camp per tick
            if (rand.Next(100) < campSpawnChance)
            {
                var validTiles = new List<(int X, int Y)>();
                for (int x = 0; x < Map.Width; x++)
                {
                    for (int y = 0; y < Map.Height; y++)
                    {
                        var t = Map.GetTile(x, y);
                        if (t != null && t.IsLand && t.Terrain.Id != "mountain" && t.Terrain.Id != "volcano")
                        {
                            if (VisibilityGrid[x, y] == FogState.Shrouded || VisibilityGrid[x, y] == FogState.Unexplored)
                            {
                                if (!Cities.Any(c => Map.GetManhattanDistance(c.X, c.Y, x, y) < 4)) // Keep away from cities
                                {
                                    validTiles.Add((x, y));
                                }
                            }
                        }
                    }
                }

                if (validTiles.Count > 0)
                {
                    var newCamp = validTiles[rand.Next(validTiles.Count)];
                    if (!BarbarianCamps.Contains(newCamp))
                    {
                        BarbarianCamps.Add(newCamp);
                        Console.WriteLine($"[Barbarians] A new barbarian camp emerged in the wilderness!");
                    }
                }
            }

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
                int dist = Map.GetChebyshevDistance(pUnit.X, pUnit.Y, barb.X, barb.Y);
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
                int dist = Map.GetChebyshevDistance(city.X, city.Y, barb.X, barb.Y);
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
                    // Fallback to direct step if no path was found (e.g., if we want to attack or move directly)
                    int stepX = Map.NormalizeX(barb.X + Map.GetStepDirectionX(barb.X, targetX));
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
        if (Mode != GameMode.SinglePlayer) return;
        AiRivalBrain.ProcessTurn(this);
    }

    private void AutoAssignTiles(City city)
    {
        int unassigned = city.Population - (city.WorkedTiles.Count + city.Specialists.Count);
        
        if (unassigned < 0)
        {
            while (unassigned < 0 && city.Specialists.Count > 0)
            {
                city.Specialists.RemoveAt(city.Specialists.Count - 1);
                unassigned++;
            }
            while (unassigned < 0 && city.WorkedTiles.Count > 0)
            {
                var worst = city.WorkedTiles.OrderBy(t => {
                    var tile = Map.GetTile(t.X, t.Y);
                    return tile != null ? (tile.TotalYield.Food + tile.TotalYield.Production + tile.TotalYield.Commerce) : 0;
                }).First();
                city.WorkedTiles.Remove(worst);
                unassigned++;
            }
        }
        else if (unassigned > 0)
        {
            var potentialTiles = new System.Collections.Generic.List<(int X, int Y)>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int tx = Map.NormalizeX(city.X + dx);
                    int ty = city.Y + dy;
                    if (Map.IsInBounds(tx, ty) && !city.WorkedTiles.Contains((tx, ty)))
                    {
                        var tile = Map.GetTile(tx, ty);
                        if (tile != null && tile.OwnerCityId == city.Id)
                            potentialTiles.Add((tx, ty));
                    }
                }
            }
            potentialTiles.Sort((a, b) => {
                var ta = Map.GetTile(a.X, a.Y);
                var tb = Map.GetTile(b.X, b.Y);
                int ya = ta != null ? (ta.TotalYield.Food + ta.TotalYield.Production + ta.TotalYield.Commerce) : 0;
                int yb = tb != null ? (tb.TotalYield.Food + tb.TotalYield.Production + tb.TotalYield.Commerce) : 0;
                return yb.CompareTo(ya);
            });

            foreach(var pt in potentialTiles)
            {
                if (unassigned <= 0) break;
                city.WorkedTiles.Add(pt);
                unassigned--;
            }
            while(unassigned > 0)
            {
                city.Specialists.Add(SpecialistType.Entertainer);
                unassigned--;
            }
        }
    }

    private int CollectCityYields()
    {
        int totalCommerceThisTurn = 0;
        int totalMaintenanceThisTurn = 0;
        int totalSpecialistTaxThisTurn = 0;
        int totalSpecialistScienceThisTurn = 0;

        // Calculate OCN based on map size
        int mapSizeTotal = Map.Width * Map.Height;
        int optimalCityNumber = mapSizeTotal < 4000 ? 14 : 20;
        if (Difficulty == DifficultyLevel.Deity) optimalCityNumber = (int)(optimalCityNumber * 0.7f);

        // Pre-calculate ranks for corruption
        var factionRanks = new System.Collections.Generic.Dictionary<City, int>();
        foreach (var f in new[] { Faction.Player, Faction.AiRival })
        {
            var factionCities = Cities.Where(c => c.Faction == f).ToList();
            factionCities.Sort((a, b) => GetDistanceToNearestCapital(a).CompareTo(GetDistanceToNearestCapital(b)));
            for (int i = 0; i < factionCities.Count; i++)
            {
                factionRanks[factionCities[i]] = i;
            }
        }

        foreach (var city in Cities)
        {
            AutoAssignTiles(city);

            // Update Draft timers
            city.DraftsThisTurn = 0;
            for (int i = city.DraftUnhappinessTimers.Count - 1; i >= 0; i--)
            {
                city.DraftUnhappinessTimers[i]--;
                if (city.DraftUnhappinessTimers[i] <= 0) city.DraftUnhappinessTimers.RemoveAt(i);
            }

            // Update citizen happiness/mood and accumulate culture
            city.UpdateCitizenMood(this, DifficultyConfig);

            int cultPerTurn = city.GetCulturePerTurn();
            city.CultureOutput = cultPerTurn;
            city.AccumulatedCulture += cultPerTurn;

            // Cultural Borders expansion is now handled globally at the end of the turn phase

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

                // Civ3: city center guarantees minimum 1 food, 1 shield, 1 commerce
                if (centerFood < 1) centerFood = 1;
                if (centerProduction < 1) centerProduction = 1;
                if (centerCommerce < 1) centerCommerce = 1;

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

            // 4. Citizens work exactly the WorkedTiles list
            var workingTiles = new System.Collections.Generic.List<TileData>();
            foreach (var tilePos in city.WorkedTiles)
            {
                var t = Map.GetTile(tilePos.X, tilePos.Y);
                if (t != null && t.OwnerCityId == city.Id)
                {
                    workingTiles.Add(t);
                }
            }

            // Sum up yields from working tiles
            // Apply: Government tile penalty/bonus, Golden Age bonus
            var ga = city.Faction == Faction.Player ? PlayerGoldenAge : AiGoldenAge;
            var cityGov = Government.Get(city.Faction == Faction.Player ? PlayerGovernment : AiGovernment);
            foreach (var tile in workingTiles)
            {
                var yield = tile.TotalYield;
                int tileFood = yield.Food;
                int tileProd = yield.Production;
                int tileComm = yield.Commerce;

                // Despotism/Feudalism penalty: -1 on any yield type ≥ 3
                if (cityGov.HasTilePenalty)
                {
                    if (tileFood >= 3) tileFood--;
                    if (tileProd >= 3) tileProd--;
                    if (tileComm >= 3) tileComm--;
                }

                // Republic/Democracy commerce bonus: +1 commerce on tiles producing ≥1
                if (cityGov.HasCommerceBonus && tileComm >= 1)
                {
                    tileComm++;
                }

                // Golden Age: +1 shield on tiles with ≥1 production, +1 commerce on tiles with ≥1 commerce
                if (ga.IsActive)
                {
                    if (tileProd >= 1) tileProd++;
                    if (tileComm >= 1) tileComm++;
                }

                food += tileFood;
                production += tileProd;
                commerce += tileComm;
            }

            // Also apply Golden Age + government bonuses to center tile
            if (centerTile != null)
            {
                var cYield = centerTile.TotalYield;
                // Despotism penalty on center
                if (cityGov.HasTilePenalty)
                {
                    if (cYield.Food >= 3) food--;
                    if (cYield.Production >= 3) production--;
                    if (cYield.Commerce >= 3) commerce--;
                }
                // Commerce bonus on center
                if (cityGov.HasCommerceBonus && cYield.Commerce >= 1)
                {
                    commerce++;
                }
                // Golden Age on center
                if (ga.IsActive)
                {
                    if (cYield.Production >= 1) production++;
                    if (cYield.Commerce >= 1) commerce++;
                }
            }

            // Apply Small Wonder modifiers
            if (city.Buildings.Any(b => b.Id == "forbidden_palace"))
            {
                commerce = (int)Math.Round(commerce * 1.5f);
            }

            // Factory and Power Plants (Civ3 authentic production multipliers)
            bool hasFactory = city.Buildings.Any(b => b.Id == "factory");
            bool hasPower = city.Buildings.Any(b => b.Id == "coal_plant" || b.Id == "hydro_plant" || b.Id == "nuclear_plant")
                            || IsCityPoweredByHooverDam(city);

            float prodMultiplier = 1.0f;
            if (hasFactory) prodMultiplier += 0.5f;
            if (hasFactory && hasPower) prodMultiplier += 0.5f;
            if (city.Buildings.Any(b => b.Id == "iron_works")) prodMultiplier += 1.0f; // Iron Works +100%

            if (prodMultiplier > 1.0f)
            {
                production = (int)Math.Round(production * prodMultiplier);
            }

            // Apply Corruption & Waste (based on government type, distance to capital, and city rank)
            int dist = GetDistanceToNearestCapital(city);
            float corruptionRate;
            float wasteRate;

            if (city.IsCapital || city.Buildings.Any(b => b.Id == "forbidden_palace"))
            {
                corruptionRate = 0f;
                wasteRate = 0f;
            }
            else if (cityGov.Corruption == CorruptionLevel.Communal)
            {
                // Communism: flat corruption everywhere regardless of distance, based on total number of cities
                int totalCities = Cities.Count(c => c.Faction == city.Faction);
                float communalRate = Math.Min(totalCities * 0.02f, 0.50f);
                corruptionRate = communalRate;
                wasteRate = communalRate;
            }
            else
            {
                // Distance Corruption
                float distCorr = Math.Min(dist * 0.05f * cityGov.CorruptionModifier, 0.40f);
                
                // Rank Corruption (OCN)
                int rank = factionRanks.GetValueOrDefault(city, 0);
                float ocnModifier = cityGov.CorruptionModifier;
                
                // Commercial trait increases effective OCN by 25%
                float effectiveOcn = optimalCityNumber;
                if (city.HasTrait(CivTrait.Commercial)) effectiveOcn *= 1.25f;

                float rankCorr = Math.Min((rank / effectiveOcn) * 0.40f * ocnModifier, 0.50f);

                corruptionRate = Math.Min(distCorr + rankCorr, 0.90f);
                wasteRate = Math.Min(distCorr + rankCorr, 0.90f);
            }

            // Difficulty: Player corruption modifier
            if (city.Faction == Faction.Player)
            {
                corruptionRate *= DifficultyConfig.PlayerCorruptionMultiplier;
                wasteRate *= DifficultyConfig.PlayerCorruptionMultiplier;
            }

            if (city.Buildings.Any(b => b.Id == "courthouse")) { corruptionRate *= 0.6f; wasteRate *= 0.6f; }
            if (city.Buildings.Any(b => b.Id == "police_station")) { corruptionRate *= 0.6f; wasteRate *= 0.6f; }

            // WLTKD eliminates Rank Waste
            if (city.IsWLTKD)
            {
                float distCorr = Math.Min(dist * 0.05f * cityGov.CorruptionModifier, 0.40f);
                wasteRate = Math.Min(wasteRate, distCorr);
            }

            // Specialists (Taxmen and Scientists)
            int cityTax = 0;
            int cityScience = 0;
            foreach (var spec in city.Specialists)
            {
                if (spec == SpecialistType.Taxman) cityTax += 2;
                if (spec == SpecialistType.Scientist) cityScience += 3;
            }
            if (city.Faction == Faction.Player && !city.IsInDisorder)
            {
                totalSpecialistTaxThisTurn += cityTax;
                totalSpecialistScienceThisTurn += cityScience;
            }

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

            // ANARCHY: During revolution, all player cities produce nothing
            if (city.Faction == Faction.Player && IsInAnarchy)
            {
                food = city.Population * 2; // Just enough to not starve (subsistence)
                production = 0;
                commerce = 0;
            }

            // Civil Disorder halts all production, commerce, and food surplus
            if (city.IsInDisorder)
            {
                production = 0;
                commerce = 0;
                food = Math.Min(food, city.Population * 2); // No food surplus, just subsistence
                city.DisorderTurns++;
                Console.WriteLine($"[REVOLT] {city.Name} is in Civil Disorder (turn {city.DisorderTurns})! Production, Commerce and Growth HALTED.");

                // Democracy: 3+ turns of continuous disorder → automatic revolution to Anarchy→Despotism
                if (city.Faction == Faction.Player && PlayerGovernment == GovernmentType.Democracy && city.DisorderTurns >= 3)
                {
                    if (!IsInAnarchy)
                    {
                        Console.WriteLine($"[REVOLUTION] Prolonged civil disorder in {city.Name} has caused your Democracy to collapse!");
                        AnarchyTurnsRemaining = 4;
                        PendingGovernment = GovernmentType.Despotism;
                    }
                }
            }
            else
            {
                city.DisorderTurns = 0;
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
                    city.StoredFood = 0;

                    // Civ3: Starvation may destroy a building (most expensive non-essential)
                    var destroyCandidate = city.Buildings
                        .Where(b => b is not Palace && b is not Granary)
                        .OrderByDescending(b => b.ProductionCost)
                        .FirstOrDefault();
                    if (destroyCandidate != null)
                    {
                        city.Buildings.Remove(destroyCandidate);
                        Console.WriteLine($"[Famine] {city.Name} has starved! Population shrank to {city.Population}. {destroyCandidate.Name} was destroyed by the famine!");
                    }
                    else
                    {
                        Console.WriteLine($"[Famine] {city.Name} has starved! Population shrank to {city.Population}.");
                    }
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
        int scienceGain = totalSpecialistScienceThisTurn;
        int goldGain = totalSpecialistTaxThisTurn;

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
            goldGain += netCommerce; // Negative gold!
        }

        // Unit Support Cost (Civ3 mechanic):
        // Government provides free unit slots per city based on city size (Town/City/Metropolis)
        var playerGov = Government.Get(PlayerGovernment);
        int freeUnitSlots = 0;
        foreach (var c in Cities.Where(c => c.Faction == Faction.Player))
        {
            freeUnitSlots += playerGov.GetUnitSupport(c.Population);
        }
        int totalPlayerUnits = Units.Count(u => u.Faction == Faction.Player);
        int excessUnits = Math.Max(0, totalPlayerUnits - freeUnitSlots);
        int unitSupportCost = excessUnits * playerGov.UnitSupportCost;

        if (unitSupportCost > 0)
        {
            goldGain -= unitSupportCost;
            totalMaintenanceThisTurn += unitSupportCost;
            Console.WriteLine($"[Economy] Unit support cost: {unitSupportCost} gold ({excessUnits} excess units × {playerGov.UnitSupportCost}g, {playerGov.Name} free slots: {freeUnitSlots})");
        }

        PlayerTreasury += goldGain;
        if (PlayerTreasury < 0)
        {
            // Disband most expensive unsupported unit when bankrupt
            var disbandCandidate = Units
                .Where(u => u.Faction == Faction.Player && u.Type != UnitType.Settler && u.Type != UnitType.Worker)
                .OrderByDescending(u => u.AttackStrength + u.DefenseStrength)
                .FirstOrDefault();
            if (disbandCandidate != null)
            {
                Console.WriteLine($"[Economy] BANKRUPT! Disbanded {disbandCandidate.Type} at ({disbandCandidate.X}, {disbandCandidate.Y}) due to lack of treasury!");
                Units.Remove(disbandCandidate);
            }
            PlayerTreasury = 0;
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
        LastTurnUnitSupport = unitSupportCost;
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
            ProductionProject.GreatLibrary => "great_library",
            ProductionProject.TheoryOfEvolution => "theory_of_evolution",
            ProductionProject.MagellansVoyage => "magellans_voyage",
            ProductionProject.HooverDam => "hoover_dam",
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
            if (!UnitDefinitionRegistry.TryMapProjectToUnitType(project, out var type))
            {
                type = UnitType.Warrior;
            }

            var unit = new Unit(unitId, type, city.X, city.Y, city.Faction, city.CivilizationId);

            if (unit.IsNavalUnit && DoesFactionControlWonder(unit.Faction, "magellans_voyage"))
            {
                unit.MovementBonus = 2.0f;
                unit.RemainingMovement = unit.MaxMovement;
            }

            // Military Academy effect: if city has military_academy, military units start with +25% max health (125 HP instead of 100)
            if (city.Buildings.Any(b => b.Id == "military_academy") && unit.AttackStrength > 0)
            {
                unit.Experience = ExperienceLevel.Elite;
                unit.MaxHealth = 125;
                unit.Health = 125;
                System.Console.WriteLine($"[Military Academy] Trained elite {type} with +25% max health (125 HP)!");
            }
            else
            {
                // Barracks / Sun Tzu's War Academy: military units start as Veteran (120 HP instead of 100)
                bool hasBarracks = city.Buildings.Any(b => b.Id == "barracks") ||
                                   Cities.Any(c => c.Faction == city.Faction && c.Buildings.Any(b => b.Id == "sun_tzu_war_academy"));
                if (hasBarracks && unit.AttackStrength > 0 && unit.MaxHealth == 100)
                {
                    unit.Experience = ExperienceLevel.Veteran;
                    unit.MaxHealth = 120;
                    unit.Health = 120;
                    System.Console.WriteLine($"[Barracks] {unit.Type} enters service with Veteran status (120 HP)!");
                }
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

    public bool CanUpgradeUnit(Unit unit, out UnitType targetType, out int goldCost)
    {
        targetType = unit.Type;
        goldCost = 0;

        var def = UnitDefinitionRegistry.Get(unit.Type);
        if (def.UpgradesTo == null) return false;

        // Check if on friendly city tile with barracks or Sun Tzu's War Academy
        var city = Cities.FirstOrDefault(c => c.X == unit.X && c.Y == unit.Y && c.Faction == unit.Faction);
        if (city == null) return false;

        bool hasBarracks = city.Buildings.Any(b => b.Id == "barracks") ||
                           Cities.Any(c => c.Faction == city.Faction && c.Buildings.Any(b => b.Id == "sun_tzu_war_academy"));
        if (!hasBarracks) return false;

        // Check target unit type (consider civ UU for target)
        var nextType = UnitDefinitionRegistry.GetCivUnitType(def.UpgradesTo.Value, unit.CivilizationId);
        var targetDef = UnitDefinitionRegistry.Get(nextType);

        // Tech prerequisite
        if (targetDef.RequiredTechId != null && !IsTechResearched(unit.Faction, targetDef.RequiredTechId))
            return false;

        // Resource prerequisites
        foreach (var reqRes in targetDef.RequiredResourceIds)
        {
            if (!CityHasResourceAccess(city, reqRes))
                return false;
        }

        targetType = nextType;
        goldCost = Math.Max(10, (targetDef.ShieldCost - def.ShieldCost) * 3);

        if (unit.Faction == Faction.Player)
        {
            return PlayerTreasury >= goldCost;
        }
        return true;
    }

    public bool UpgradeUnit(Unit unit)
    {
        if (!CanUpgradeUnit(unit, out var targetType, out int goldCost))
            return false;

        if (unit.Faction == Faction.Player)
        {
            PlayerTreasury -= goldCost;
        }

        ExperienceLevel newExp = unit.Experience == ExperienceLevel.Elite ? ExperienceLevel.Veteran : unit.Experience;
        if (newExp < ExperienceLevel.Veteran) newExp = ExperienceLevel.Veteran;
        int newMaxHealth = newExp == ExperienceLevel.Veteran ? 120 : 100;

        var newUnit = new Unit(unit.Id, targetType, unit.X, unit.Y, unit.Faction, unit.CivilizationId)
        {
            Experience = newExp,
            MaxHealth = newMaxHealth,
            Health = Math.Min(unit.Health, newMaxHealth),
            RemainingMovement = 0f,
            IsFortified = unit.IsFortified
        };

        Units.Remove(unit);
        Units.Add(newUnit);
        Console.WriteLine($"[Upgrade] Upgraded {unit.Type} to {targetType} for {goldCost} gold!");
        return true;
    }

    // Helper to check if city has access to a strategic/luxury resource via Trade Network
    public bool CityHasResourceAccess(City city, string resourceId)
    {
        if (CityTradeNetworks.TryGetValue(city.Id, out int networkId))
        {
            if (TradeNetworkResources.TryGetValue(networkId, out var resources))
            {
                return resources.Contains(resourceId);
            }
        }
        return false;
    }

    private void CalculateTradeNetworks()
    {
        TradeNetworkResources.Clear();
        CityTradeNetworks.Clear();
        
        int nextNetworkId = 1;
        var visited = new HashSet<(int X, int Y)>();
        
        // Tiles that can form connections
        bool IsConnectableTile(int x, int y, Faction faction)
        {
            var tile = Map.GetTile(x, y);
            if (tile == null) return false;
            // Roads/Railroads always connect
            if (tile.HasRoad || tile.HasRailroad) return true;
            // City centers implicitly have a road (even if destroyed later, cities are hubs)
            if (Cities.Any(c => c.X == x && c.Y == y)) return true;
            // Harbors connect over water, but we'll handle harbors in a separate pass or globally
            return false;
        }

        for (int x = 0; x < Map.Width; x++)
        {
            for (int y = 0; y < Map.Height; y++)
            {
                if (visited.Contains((x, y))) continue;

                var tile = Map.GetTile(x, y);
                if (tile == null) continue;

                // Start a BFS if we find a road/city that hasn't been visited
                bool isCity = Cities.Any(c => c.X == x && c.Y == y);
                if (tile.HasRoad || tile.HasRailroad || isCity)
                {
                    int currentNetworkId = nextNetworkId++;
                    var resourcesInNetwork = new HashSet<string>();
                    var queue = new Queue<(int X, int Y)>();
                    queue.Enqueue((x, y));
                    visited.Add((x, y));
                    
                    int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
                    int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();
                        var currentTile = Map.GetTile(cx, cy);
                        
                        // Add city to this network
                        var cityOnTile = Cities.FirstOrDefault(c => c.X == cx && c.Y == cy);
                        if (cityOnTile != null)
                        {
                            CityTradeNetworks[cityOnTile.Id] = currentNetworkId;
                        }

                        // Collect resources on this tile (must have a road or be a city)
                        // In Civ3, you must have a road on the resource (unless it's under city)
                        // It must also be within cultural borders (OwnerCityId != null)
                        bool hasRoadOrCity = currentTile.HasRoad || currentTile.HasRailroad || cityOnTile != null;
                        if (hasRoadOrCity && currentTile.Resource != null && currentTile.OwnerCityId != null)
                        {
                            // Verify the owning city belongs to a faction (prevent exploiting enemy resources without war, etc.)
                            // For simplicity, we just add the resource. Only cities in this network will see it anyway.
                            resourcesInNetwork.Add(currentTile.Resource.Id);
                        }

                        // Traverse neighbors
                        for (int i = 0; i < 8; i++)
                        {
                            int nx = Map.NormalizeX(cx + dx[i]);
                            int ny = cy + dy[i];
                            if (Map.IsInBounds(nx, ny) && !visited.Contains((nx, ny)))
                            {
                                if (IsConnectableTile(nx, ny, Faction.Player))
                                {
                                    visited.Add((nx, ny));
                                    queue.Enqueue((nx, ny));
                                }
                            }
                        }
                    }

                    TradeNetworkResources[currentNetworkId] = resourcesInNetwork;
                }
            }
        }

        // Global pass: Merge networks for cities with Harbors or Airports (within the same faction)
        foreach (Faction faction in Enum.GetValues<Faction>())
        {
            var factionCities = Cities.Where(c => c.Faction == faction).ToList();
            
            // Collect cities with harbors
            var harborCities = factionCities.Where(c => c.Buildings.Any(b => b.Id == "harbor")).ToList();
            if (harborCities.Count > 1)
            {
                // Find the lowest Network ID among these cities
                var networkIds = harborCities
                    .Where(c => CityTradeNetworks.ContainsKey(c.Id))
                    .Select(c => CityTradeNetworks[c.Id])
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();

                if (networkIds.Count > 1)
                {
                    int primaryNetwork = networkIds.First();
                    for (int i = 1; i < networkIds.Count; i++)
                    {
                        int fromNetwork = networkIds[i];
                        // Move resources to primary
                        if (TradeNetworkResources.TryGetValue(fromNetwork, out var fromRes))
                        {
                            foreach (var r in fromRes) TradeNetworkResources[primaryNetwork].Add(r);
                            TradeNetworkResources.Remove(fromNetwork);
                        }
                        // Remap cities
                        foreach (var kvp in CityTradeNetworks.ToList())
                        {
                            if (kvp.Value == fromNetwork)
                                CityTradeNetworks[kvp.Key] = primaryNetwork;
                        }
                    }
                }
            }
            
            // Similarly for Airports
            var airportCities = factionCities.Where(c => c.Buildings.Any(b => b.Id == "airport")).ToList();
            if (airportCities.Count > 1)
            {
                var networkIds = airportCities
                    .Where(c => CityTradeNetworks.ContainsKey(c.Id))
                    .Select(c => CityTradeNetworks[c.Id])
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();

                if (networkIds.Count > 1)
                {
                    int primaryNetwork = networkIds.First();
                    for (int i = 1; i < networkIds.Count; i++)
                    {
                        int fromNetwork = networkIds[i];
                        if (TradeNetworkResources.TryGetValue(fromNetwork, out var fromRes))
                        {
                            foreach (var r in fromRes) TradeNetworkResources[primaryNetwork].Add(r);
                            TradeNetworkResources.Remove(fromNetwork);
                        }
                        foreach (var kvp in CityTradeNetworks.ToList())
                        {
                            if (kvp.Value == fromNetwork)
                                CityTradeNetworks[kvp.Key] = primaryNetwork;
                        }
                    }
                }
            }
        }
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
            (production: ProductionProject.GreatLibrary, buildingId: "great_library"),
            (production: ProductionProject.TheoryOfEvolution, buildingId: "theory_of_evolution"),
            (production: ProductionProject.MagellansVoyage, buildingId: "magellans_voyage"),
            (production: ProductionProject.HooverDam, buildingId: "hoover_dam"),
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

    // ══════════════════════════════════════════════════
    // AIR COMBAT (CIV3)
    // ══════════════════════════════════════════════════

    public bool SetAirMission(Unit unit, AirMission mission)
    {
        if (unit.Domain != UnitDomain.Air) return false;
        
        // Ensure unit is in a valid base
        bool inCity = Cities.Any(c => c.X == unit.X && c.Y == unit.Y && c.Faction == unit.Faction);
        bool onCarrier = Units.Any(u => u.X == unit.X && u.Y == unit.Y && u.Type == UnitType.Carrier && u.Faction == unit.Faction);
        
        if (!inCity && !onCarrier) return false;

        unit.CurrentAirMission = mission;
        
        // If AirSuperiority, it just waits for interception
        if (mission == AirMission.AirSuperiority)
        {
            Console.WriteLine($"[Air Force] {unit.Type} at ({unit.X},{unit.Y}) is on Air Superiority patrol.");
            unit.RemainingMovement = 0; // Mission takes its turn
        }
        
        return true;
    }

    public bool RebaseAirUnit(Unit unit, int targetX, int targetY)
    {
        if (unit.Domain != UnitDomain.Air || unit.RemainingMovement <= 0) return false;
        targetX = Map.NormalizeX(targetX);
        
        // Rebase range in Civ3 is usually 4x Operational Range
        int maxRebaseRange = unit.OperationalRange * 4;
        int distance = Map.GetChebyshevDistance(unit.X, unit.Y, targetX, targetY);
        if (distance > maxRebaseRange) return false;

        // Verify target is valid base
        bool isCity = Cities.Any(c => c.X == targetX && c.Y == targetY && c.Faction == unit.Faction);
        bool isCarrier = Units.Any(u => u.X == targetX && u.Y == targetY && u.Type == UnitType.Carrier && u.Faction == unit.Faction);
        
        if (!isCity && !isCarrier) return false;

        // Move unit
        unit.X = targetX;
        unit.Y = targetY;
        unit.RemainingMovement = 0;
        unit.CurrentAirMission = AirMission.None;
        
        Console.WriteLine($"[Air Force] {unit.Type} rebased to ({targetX},{targetY}).");
        return true;
    }

    public void ExecuteAirBombard(Unit bomber, int targetX, int targetY)
    {
        if (bomber.Domain != UnitDomain.Air || bomber.RemainingMovement <= 0) return;
        targetX = Map.NormalizeX(targetX);
        
        int distance = Map.GetChebyshevDistance(bomber.X, bomber.Y, targetX, targetY);
        if (distance > bomber.OperationalRange) return;

        bomber.RemainingMovement = 0;
        bomber.CurrentAirMission = AirMission.Bombard;

        Console.WriteLine($"[Air Force] {bomber.Type} from ({bomber.X},{bomber.Y}) attempting bombard on ({targetX},{targetY}).");

        // 1. Interception Check
        if (CheckAirInterception(bomber, targetX, targetY))
        {
            // Interception occurred! The bomber was either shot down or aborted.
            // Check if bomber still exists
            if (!Units.Contains(bomber))
            {
                return; // Shot down
            }
            
            // Civ3: if intercepted but survives, it often still aborts the bombing run.
            Console.WriteLine($"[Air Force] {bomber.Type} survived interception but aborted the mission.");
            return;
        }

        // 2. Perform Bombardment (Same logic as artillery, but air uses BombardStrength)
        var targetTile = Map.GetTile(targetX, targetY);
        if (targetTile == null) return;

        var defUnit = Units.Where(u => u.X == targetX && u.Y == targetY && u.Faction != bomber.Faction)
                           .OrderByDescending(u => u.DefenseStrength)
                           .FirstOrDefault();
                           
        var targetCity = Cities.FirstOrDefault(c => c.X == targetX && c.Y == targetY && c.Faction != bomber.Faction);

        Random rng = new Random();
        int roll = rng.Next(1, 101); // simplistic bombard chance

        if (defUnit != null)
        {
            // Bombard unit
            if (roll > 50) // Simplified: 50% hit chance
            {
                defUnit.Health -= 20; // 1 HP in Civ3 is usually 20 in our system
                Console.WriteLine($"[Air Force] Bombard hit! {defUnit.Type} damaged. Health: {defUnit.Health}/{defUnit.MaxHealth}");
                if (defUnit.Health <= 0)
                {
                    Units.Remove(defUnit);
                    Console.WriteLine($"[Air Force] {defUnit.Type} was completely destroyed by bombardment!");
                }
            }
            else
            {
                Console.WriteLine("[Air Force] Bombard missed the unit.");
            }
        }
        else if (targetCity != null)
        {
            // Bombard city (kill pop or destroy improvement)
            if (roll > 70 && targetCity.Population > 1)
            {
                targetCity.Population--;
                Console.WriteLine($"[Air Force] Bombard hit city! {targetCity.Name} population reduced to {targetCity.Population}.");
            }
            else
            {
                Console.WriteLine($"[Air Force] Bombard hit city but caused minimal damage.");
            }
        }
        else if (targetTile.HasRoad || targetTile.HasRailroad || targetTile.Improvement != null)
        {
            // Bombard improvement
            if (roll > 40)
            {
                targetTile.Improvement = null;
                targetTile.HasRoad = false;
                targetTile.HasRailroad = false;
                Console.WriteLine($"[Air Force] Bombard destroyed terrain improvements at ({targetX},{targetY}).");
            }
            else
            {
                Console.WriteLine($"[Air Force] Bombard missed terrain improvements.");
            }
        }
        else
        {
            Console.WriteLine($"[Air Force] Bombard fell on empty land.");
        }
    }

    private bool CheckAirInterception(Unit bomber, int targetX, int targetY)
    {
        targetX = Map.NormalizeX(targetX);
        // Find enemy fighters on Air Superiority mission whose operational range covers the target tile
        var interceptor = Units.FirstOrDefault(u => 
            u.Domain == UnitDomain.Air && 
            u.Faction != bomber.Faction &&
            u.CurrentAirMission == AirMission.AirSuperiority &&
            Map.GetChebyshevDistance(u.X, u.Y, targetX, targetY) <= u.OperationalRange);

        if (interceptor != null)
        {
            Console.WriteLine($"[Air Combat] {interceptor.Type} intercepts {bomber.Type} over ({targetX},{targetY})!");
            
            // Simple air-to-air combat: Interceptor Attack vs Bomber Defense
            // (Assuming standard Civ3 combat logic translated to our system)
            int interceptorStr = interceptor.AttackStrength;
            int bomberStr = bomber.DefenseStrength;
            if (interceptorStr == 0) interceptorStr = 1;
            if (bomberStr == 0) bomberStr = 1;
            
            float interceptorChance = (float)interceptorStr / (interceptorStr + bomberStr);
            Random rng = new Random();
            
            if (rng.NextDouble() < interceptorChance)
            {
                // Bomber takes damage or dies
                bomber.Health -= 50; // Heavy damage
                if (bomber.Health <= 0)
                {
                    Units.Remove(bomber);
                    Console.WriteLine($"[Air Combat] {bomber.Type} was shot down by {interceptor.Type}!");
                }
                else
                {
                    Console.WriteLine($"[Air Combat] {bomber.Type} was heavily damaged but survived.");
                }
            }
            else
            {
                // Interceptor takes damage or dies
                interceptor.Health -= 50;
                if (interceptor.Health <= 0)
                {
                    Units.Remove(interceptor);
                    Console.WriteLine($"[Air Combat] {interceptor.Type} was shot down by the bomber's defense guns!");
                }
                else
                {
                    Console.WriteLine($"[Air Combat] {interceptor.Type} was repelled by the bomber.");
                }
            }
            return true; // interception occurred
        }

        // Also check ground Anti-Air (Flak, SAM, Aegis Cruiser, etc.)
        var groundAA = Units.FirstOrDefault(u => 
            u.Faction != bomber.Faction &&
            u.X == targetX && u.Y == targetY &&
            (u.Type == UnitType.AegisCruiser || u.Type == UnitType.Destroyer || u.Type == UnitType.Cruiser || u.Type == UnitType.Battleship));
            
        if (groundAA != null)
        {
            Console.WriteLine($"[Air Combat] Ground Anti-Air fire from {groundAA.Type} at ({targetX},{targetY})!");
            Random rng = new Random();
            if (rng.Next(1, 100) < 30) // 30% chance to hit
            {
                bomber.Health -= 40;
                if (bomber.Health <= 0)
                {
                    Units.Remove(bomber);
                    Console.WriteLine($"[Air Combat] {bomber.Type} was shot down by AA fire!");
                }
                return true; // Aborted due to heavy AA fire
            }
        }

        return false;
    }
}
