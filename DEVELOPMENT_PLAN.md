# CivGame - Development Plan & Implementation Status

This document serves as an overview of achieved progress and a high-level plan for further development phases of the 2D isometric 4X strategy game.

---

## 🛠️ What's Done (Current Status)

### 1. Isometric Engine & Render (`src/Render`)

- **Diamond 45° Map:** Tile rendering at 2:1 ratio (`256x128px`).
- **Procedural Generation:** Custom algorithm (Perlin Noise) running independently of Godot. Generates grasslands, plains, deserts, mountains, and oceans.
- **Camera:** Zoom with mouse wheel, movement via WASD / arrow keys.
- **Units (Sprites):** Custom procedurally generated images for Explorer (scout) and Settler (wagon). Dynamic slide effect (tweening) during movement.

### 2. Core and Render Separation (`src/Core`)

- **GameSimulation:** Main simulator running purely on .NET 10 types (no Godot dependencies, fully testable).
- **Fog of War:** Three tile states (`Unexplored`, `Shrouded`, `Visible`). Each unit reveals fog based on their `VisionRange` parameter.
- **Movement Validation:** Refined with precise failure reasons (`MoveValidationResult`), logged directly in debug output.

### 3. Recent Improvements (Milestone 2)

- **Turn Counter & HUD (`src/UI/GameHud.cs`):**
  - Added turn counter (`TurnNumber`). End turn (Spacebar) restores unit MP and increments counter.
  - Created game UI (HUD) displaying current turn and selected unit status (type, coordinates, MP).
- **Movement Range Highlight (`src/Render/HighlightRenderer.cs`):**
  - Implemented Core BFS pathfinding (`GetReachableTiles`) respecting terrain movement cost (Plains/Grassland = 1, Desert = 2, Mountain = 3, Ocean = impassable).
  - Rendering of semi-transparent green tiles for visual feedback of selected unit's reach.
- **City Founding (`B` key):**
  - Selected Settler can found a city by pressing `B`.
  - Created clean Core class `City.cs` with automatic city naming (`Rome`, `Sparta`, etc.) and permanent vision (`VisionRange = 2`).
  - Created `src/Render/CityRenderer.cs` procedurally generating castle texture with red flag and rendering city labels.
- **Cultural Borders:**
  - Cities claim territory in 1-tile radius upon founding (3x3 square in Core logic).
  - Connected to Core class `TileData.cs` via `OwnerCityId` property.
  - Created `src/Render/BorderRenderer.cs` procedurally generating thin gold isometric border texture (`assets/border.png`) and rendering borders only on explored territory.
- **Resource Yield Collection:**
  - Each city automatically collects Food/Production/Commerce from tiles in its territory at end of turn.
  - Resources accumulate in city (`StoredFood`, `StoredProduction`, `StoredCommerce`).
  - Updated `GameHud.cs` displays resource stock when selecting city (click on castle).
- **City Production Queue (Unit Construction via `P` key):**
  - Introduced production queue mechanic (`ProductionProject`): `None`, `Explorer`, `Settler`.
  - Pressing `P` with city selected allows player to cycle what the city is building.
  - Each turn, city's generated production adds to project. Upon reaching target (Explorer = 10, Settler = 20 Prod), new unit automatically spawns in city and project resets.
- **Population Growth & Food Consumption:**
  - Each citizen consumes 2 food per turn (`Population * 2`).
  - Implemented intelligent resource collection: city automatically works its center tile plus `Population` best owned tiles in 3x3 surroundings (sorted descending by total yield).
  - Food surplus is stored (`StoredFood`). Upon reaching growth threshold (`10 + (Population - 1) * 5`), city grows (`Population++`).
  - Food shortage causes starvation and population decline, with safety floor for minimum population 1.
  - HUD fully expanded with clear visualization of growth and food gain/loss trend (e.g., `(Pop: 1) Food: 3/10 (+2/turn)`).
- **City Building Construction (Granary & Monument):**
  - Expanded production queue with buildings: `Granary` (25 Prod) and `Monument` (15 Prod).
  - Project cycling via `P` intelligently skips already built buildings to prevent duplicate construction.
  - **Monument Effect:** Upon completion, immediately expands city's cultural borders from radius 1 (3x3) to radius 2 (5x5 area around city, i.e., 25 tiles!). Borders render in gold on explored map immediately.
  - **Granary Effect:** On population growth, city retains 50% of food required for growth, dramatically accelerating further city development.
  - HUD fully visualizes list of built buildings (e.g., `Buildings: Granary, Monument`).
- **Complete Combat System & Barbarian Threat (Phase 3):**
  - Added new combat units: `Warrior` (Strength 2, Defense 2, Movement 1) and `Archer` (Strength 3, Defense 1, Movement 2 vision) with `Health` parameter (HP: 100/100). Built by city (press `P`).
  - **Combat Mechanic (`ResolveCombat`):** Right-click on tile with enemy (different category) triggers combat. Damage calculated based on attack/defense strength ratio with counter-attack. Unit with `Health <= 0` is destroyed; if attacker wins, automatically occupies enemy tile (Advance upon Victory). Combat consumes entire turn movement.
  - **Barbarian Threat:** Procedural spawn of 3 barbarian camps on land in unexplored territory. Camps generate a `Barbarian` warrior (Strength 2, Defense 1, Movement 1) every 8 turns.
  - **Simplified Barbarian AI:** Barbarians automatically move and head toward nearest player city or explorer, attacking immediately on contact.
  - HUD expanded with clear display of remaining HP (e.g., `Warrior (X, Y) MP: 1/1 HP: 100/100`).
- **Object-Oriented Science & Technology System (Phase 4):**
  - Created robust OOP structure with base class `Technology.cs` and concrete technologies `Pottery` (15 Science), `BronzeWorking` (25 Science) and `CeremonialBurial` (15 Science).
  - Created management component `TechManager.cs` handling unlocked technologies, science accumulation, and research project cycling.
  - **Research Cycle Closure:** Player cycles active research by pressing **`T`** (as in _Technology_).
  - **Science Supply:** At end of each turn, total Commerce generated by all cities transforms 1:1 to Science and adds to active project.
  - **Impact on City Production:** Construction of advanced units and buildings is now **technologically locked**. If player lacks required technology, city cannot build it and `P` menu automatically skips it (`Granary` requires _Pottery_, `Monument` requires _Ceremonial Burial_, `Archer` requires _Bronze Working_).
  - HUD expanded with clear display of active research, remaining science, and per-turn gain in turn panel (e.g., `Turn: 5 | Research: Pottery (4/15 Science) (+2/turn)`).
- **Workers and Tile Improvements (Phase 5):**
  - Created OOP structure with base class `TileImprovement.cs` and concrete improvements `Farm` (+1 Food, buildable on Plains/Grassland), `Mine` (+1 Production, buildable on Hills/Mountains), and `Plantation` (+1 Commerce, buildable on Plains/Grassland).
  - Extended `TileData.cs` with `Improvement` property and `TotalYield` calculation that sums base terrain yield with improvement bonuses.
  - Extended `Unit.cs` with `Worker` unit type, construction state properties (`IsBuilding`, `BuildingType`, `ConstructionProgress`, `ConstructionTurns`), and methods (`StartBuilding`, `ContinueBuilding`, `CancelBuilding`).
  - Updated `GameSimulation.cs` to use `TotalYield` for city yield collection, process worker construction at end of each turn via `ProcessWorkerConstruction()`, and integrate Worker into city production cycle.
  - Updated `MapRenderer.cs` with worker construction command handling: `F` for Farm, `M` for Mine, `L` for Plantation. Commands check terrain compatibility and existing improvements.
  - Updated `GameHud.cs` to display worker construction status, tile terrain and improvement info, and available worker actions when a Worker is selected.
  - Workers cannot move while building. Construction takes multiple turns (Farm: 3, Mine: 4, Plantation: 3). Upon completion, improvement is applied to tile and worker is freed.
- **AI Rivals & Diplomacy (Phase 6):**
  - **Procedural AI Spawn:** Spawns a rival AI nation (`Faction.AiRival`) with starting Explorer and Settler on the opposite side of the map (using elegant coordinate inversion).
  - **Dynamic AI Behavior:** Smart, turn-based decision logic for AI cities (produces settlers to expand, workers to build improvements, and units for patrol/defense) and units (exploration, random walking on land, and starting Farm/Mine improvements).
  - **Territory Intrusion & Diplomacy:** Introduces a 'Peace' state by default where unit stacking is blocked. If any unit crosses into another faction's claimed territory, **WAR** is automatically declared!
  - **Visual Distinction:** AI cities use localized names (Kyoto, Berlin, Paris, London, etc.). AI cultural borders are generated and rendered in a distinct **Royal Purple** color, contrasting with the player's gold borders.
  - **HUD Integration:** The HUD is fully expanded to display AI city/unit ownership tags and show the active diplomatic relationship status (e.g. `AI Rival: Peace` or `AI Rival: 🔴 WAR`).
  - **Control Protection:** Input controls strictly validate ownership, blocking players from moving AI units, settling cities with AI settlers, changing AI city production queues, or selecting units on unexplored tiles.
- **Classic 4X HUD Dashboard (Phase 7):**
  - **Bottom-Left Minimap:** Created a real-time mini-viewport control `MinimapCtrl` rendering explored terrain (Grassland, Plains, Mountains, Desert, Ocean), Shrouded Fog memory state, and faction-colored indicator dots for units and cities.
  - **Bottom-Center Actions Panel:** Dynamic context-sensitive action button dock. Displays clicking controls for Settlers (Found City [B]) and Workers (Build Farm [F], Build Mine [M], Build Plantation [L], Stop Work [ESC]) and Cities (Cycle Project [P]) with hotkey labeling.
  - **Bottom-Right Details & Turn Panel:** Features structural selection details for Units/Cities (Health, MP, yields, projects) and a massive green "END TURN" button connected directly to the simulation.
  - **Top-Left Bar & Popups:** Implemented standard top menu bar with direct links to a styled Pause Menu (Resume, Restart Map, Quit) and a comprehensive multi-tab **Civilopedia** containing a detailed in-game Strategy Guide for players.
  - **Event-Driven Architecture:** Connected HUD action buttons directly to `MapRenderer` via decoupled C# event actions (`OnActionTriggered`) for robust simulation updates.
- **Win / Loss Conditions & Game End State (Phase 8):**
  - **Comprehensive Win/Loss Conditions:** Supported dynamic conquest, science, and score conditions.
  - **Domination Victory / Defeat:** Capturing enemy cities (or losing your own) checks for domination. City captures now support a -1 population conquest penalty and clear progress. Razing rules applied for Barbarians!
  - **Scientific Victory:** Achieved by researching all 3 techs (Pottery, Ceremonial Burial, Bronze Working).
  - **Score Victory (Turn Limit):** Games have a max turn limit of 50. Upon expiry, score is dynamically calculated based on Cities (+15), Population (+3), Tiles (+1), Units (+2), and Techs (+10).
  - **Immersive End Game Screen:** Designed a gorgeous semi-transparent fullscreen overlay displaying animated title (🏆 VICTORY! or 💀 DEFEAT!), contextual narrative based on the victory/loss reason, a detailed statistics comparison card, and "Play Again" / "Exit" controls.
  - **Gameplay Input Freeze:** Once game is ended, input listeners are blocked, preventing any illegal unit moves or selections.
- **Scalable Save System (Phase 9):**
  - **Scalable Interface Design (`ISaveSystem`):** Abstracted save/load operations using clean OOP design patterns (`ISaveSystem.cs`) supporting plug-and-play storage providers (JSON, Binary, Cloud, etc.).
  - **Doyrozmerné polia (DTO Mapping):** Solved native JSON limitation of handling 2D arrays (`TileData[,]` and `FogState[,]`) by introducing lightweight serializable `SaveDataDto`, `TileSaveDto`, `UnitSaveDto`, and `CitySaveDto` mapping layers.
  - **JSON Storage Provider (`JsonSaveSystem.cs`):** Fully implemented JSON-based serialization into Godot's local secure storage directories (`user://save_slot_1.json`) with app-domain fallbacks for pure unit test compilation environments.
  - **Menu Integrations & Warning Prompts:** Added styled buttons **"💾 Save Game"** and **"📂 Load Game"** to the Game Menu popup.
  - **Dynamic State Reconstruction:** Restored simulation, tiles, improvements, unit health/status, active city construction queues, completed structures, and technology progress, automatically triggering redrawing maps, units, fogs, and borders.
  - **Robust Confirmation Modal (`_confirmModal`):** Implemented a dual-mode dialog overlay for warning prompts. Shows confirmation warnings before destructive actions (Load Game, Restart Map, Exit Game) with **Confirm/Cancel** flows, and informative **OK** dialogue cards upon success (Save Complete). Re-chains the Pause Menu if a prompt is canceled.
- **Interactive Unit Control Panel:**
  - **Tile Roads & Fortifications:** Enhanced core logic (`TileData` & `Unit`) to support `HasRoad` (reduces land movement costs to 1 MP) and `IsFortified` (gives unit +25% defense bonus during combat resolution), with full save/load support.
  - **Context-Sensitive Icon Menu:** Redesigned the bottom-center "ACTIONS" box into a gorgeous, mouse-interactive strategy control grid. Uses large emoji symbols centered above descriptions and keyboard shortcuts.
  - **Unit Action Blueprints:** Workers display actions for 🌾 **Irrigate** (Farm), ⛏️ **Build Mine**, 🍇 **Plantation**, 🛣️ **Build Road**, 🛡️ **Fortify**, and 💤 **Sleep** (or ❌ **Stop Work** if busy). Settlers display 🏛️ **Found City**, 🛡️ **Fortify**, and 💤 **Sleep**. Awake units can be put to sleep or fortified, and inactive units can be woken up (☀️ **Wake Up**).
- **City Detail System (Phase 10):**
  - **Interactive City Panel:** Created `CityDetailPanel` UI, accessible via double-click on a city or pressing `C` key when a city is selected.
  - **Dynamic Stats:** Displays real-time population, food (with gain/loss trend), and production status.
  - **Management:** Allows cycling production projects directly from the detail panel with an automatic UI refresh.
  - **Building Inventory:** Displays a formatted list of all completed city buildings.
  - **Comprehensive Civilization System (Phase 10+: Nations & Leaders):**
  - **Dynamic Registry Pattern:** Refactored `Civilization` into an extensible data class with `Id`, `Name`, `LeaderName`, `CultureGroup`, `CivTrait` pair, `StartingTechIds`, and Unique Unit stats. Implemented `CivilizationRegistry` pattern (matching `TerrainRegistry` / `ResourceRegistry`) with all 31 Civ 3 civilizations across Base (16), Play the World (8) and Conquests (7) expansions.
  - **Faction-Civ Wiring:** Added `CivilizationId` string reference to both `Unit` and `City` classes (alongside existing `Faction`). `GameSimulation` now tracks `PlayerCivId` and `AiCivId` and automatically assigns correct civilization data to all spawned units and founded cities. Save/Load system fully supports civilization serialization.
  - **Trait System:** Full Civ 3 trait mapping (Militaristic, Scientific, Industrious, Agricultural, Expansionist, Commercial, Religious, Seafaring) with Unique Unit bonuses per civilization.
  - **Food & Population Mechanics (Civ 3 standard):** Updated city growth formula (`20` for pop 1-2, `+10` per pop above). Implemented `Aqueduct` (Pop cap 12) and `Hospital` (Pop cap 20) buildings, properly integrated into project queue and simulation logic. Updated `Granary` effect to retain 50% food upon growth. All systems work with Civilization data.
  - **Building & Maintenance System (Phase 12):** Implemented building registry with production costs and gold maintenance. Buildings (`Monument`, `Granary`, `Aqueduct`, `Hospital`) now have associated maintenance costs which are automatically deducted from city commerce each turn. Added `Aqueduct` and `Hospital` to production queue logic.
  - **Complex Resource System (Phase 11 start):** Implemented an extensible resource hierarchy (`Resource`, `StrategicResource`, `LuxuryResource`, `BonusResource`) with terrain compatibility (`AllowedTerrains`). Full Civ 3 resource list registered — 10 Bonus (Cattle, Fish, Game, Gold, Oasis, Sugar, Tobacco, Tropical Fruit, Whales, Wheat), 8 Strategic (Horses, Iron, Oil, Coal, Rubber, Saltpeter, Aluminum, Uranium), 8 Luxury (Dyes, Furs, Gems, Incense, Ivory, Silk, Spice, Wine). MapGenerator now uses terrain-compatible placement with weighted category chances.
  - **Extensible Terrain System:** Refactored terrain from `TerrainType` enum to a fully extensible class hierarchy (`Terrain` base class, `TerrainRegistry`). Added Civ 3 specific terrains (Hills, Tundra, Marsh, Flood Plains, etc.) with corresponding movement costs, yield profiles, and colors. Updated Map Generation, Rendering, and HUD to dynamically work with the new terrain system.
- **Civilization 3 Style New Game Menu & HUD Fixes:**
  - **Main Menu Screen (`MainMenu.cs`):** Created a beautiful, dark-themed pre-game menu screen matching the game's aesthetic. Allows players to select standard Civilization 3 map sizes (Tiny 60x60, Small 80x80, Standard 100x100, Large 140x120, Huge 180x180) and customize or randomize the map generation Seed 🎲.
  - **Deferred Game Initialization:** Updated `MapRenderer.cs` to show the main menu first and delay procedural map generation, simulation creation, and camera positioning until the player hits the "Start New Game" button.
  - **HUD Visibility Fix**: Removed the redundant and buggy `_rootControl` system in `GameHud.cs`. Adding UI panels directly to `CanvasLayer` resolved rendering issues where the bottom unit menu/dock was positioned off-screen.
  - **New Game Menu Option**: Added a styled "New Game" button to the pause menu, prompting users for confirmation before reloading the current scene to restart with a new map selection.
  - **Game End Domination Condition Fix:** Resolved a bug that caused immediate player victory on Turn 2. The game now checks faction survival based on having at least one city OR a Settler unit, rather than checking city counts prematurely.
  - **RTS Camera Controls (Edge Scrolling & Minimap Pan):**
    - **Edge Scrolling:** Implemented screen edge panning when the mouse is within 20px of the viewport edge, with smart region checks that ignore scrolling when clicking buttons on the top bar or bottom HUD dock.
    - **Minimap Interaction:** Added click and drag input detection to `MinimapCtrl` to immediately center the camera on the chosen region.

---

## 🎯 High-Level Plan (Roadmap)

### Phase 11: Gold, Economy & Domestic Advisor (PARTIALLY COMPLETED) 👈 _Recommended next step_

- **Core Economy System:** Implemented national treasury (`PlayerTreasury`) in `GameSimulation.cs`. City commerce is now aggregated, and net commerce is split into Science and Gold based on the interactive `PlayerTaxRate`.
- **Maintenance Costs:** Building maintenance is now dynamically calculated and deducted from the gross commerce. Deficits actively drain the treasury.
- **Domestic Advisor UI:** Implemented a fullscreen `DomesticAdvisorPanel.cs` accessible from the Advisors Menu. Features interactive Science/Tax sliders, total income/expense reports, and comprehensive city yield tables showing Food, Production, Commerce, Maintenance, and Population using visual emojis (🧑).
- **Rushed Purchasing (PENDING):** Allow players to spend accumulated gold to instantly purchase units or buildings in cities.
- **Tribal Villages (PENDING):** Procedurally spawn ancient ruins/villages on the map that give one-time gold bonuses or free techs when explored.
- **Unit Maintenance & Trade Routes (PENDING):** Apply maintenance costs to units. Add trade routes or trade networks.

### Phase 12: Technology Tree & Building Prerequisites (COMPLETED)

- **Technology Registry:** Implemented full Civ 3 technology tree (71+ technologies) across Ancient, Middle Ages, Industrial, and Modern eras.
- **Technology Dependencies:** Each technology has prerequisites (e.g., Construction requires Mathematics + Iron Working) and unlocks specific units/buildings.
- **Building Prerequisites:** All buildings now have `RequiredTechId` property linking to prerequisite technologies (e.g., Library requires Literature, Factory requires Industrialization).
- **Resource Prerequisites:** Buildings with strategic resource requirements have `RequiredResourceId` (Factory/Iron, Coal Plant/Coal, Nuclear Plant/Uranium, etc.). Resource access validated via `CityHasResourceAccess()` during production cycling.
- **Production Suggestions:** City production cycling dynamically suggests next buildable item based on available technologies and already-built buildings.
- **City Classification:** Implemented Town (pop 1-6), City (pop 7-12), Metropolis (pop 13+) with defense bonuses and growth requirements.
- **Palace System:** Palace (masonry) can be built in any city; reduces corruption in capital city.

### Phase 13: Wonders & National Wonders (COMPLETED)

- **Wonder System:** Implemented `Wonder` abstract class inheriting from `Building` with `IsNationalWonder` property.
- **Wonder Registry:** Created `WonderRegistry` with all Civ3 wonders:
  - **Ancient Era:** Pyramids, Hanging Gardens, Colossus, Temple of Artemis, Great Wall, Statue of Zeus, Oracle, Louvre
  - **Medieval Era:** Knights Hall, Sovereign Bath, Leonardo's Workshop, Shakespeare's Theatre, Sun Tzu's War Academy, Cure for Cancer, Sistine Chapel, Taj Mahal
  - **Industrial Era:** Astrolabe, Hermitage, Iron Works, Smith's Mansion, Train Station, United Nations
  - **Modern Era:** Apollo Program, Manhattan Project, Internet, Longevity Vaccine, Mars Colony, World Bank, Space Station
- **National Wonders:** Heroic Epic, Military Academy, University Grounds, Bank of America, Forbidden Palace, Mount Rushmore, Pentagon, Intelligence Agency, Wall Street, Holocaust Memorial
- **Global Claiming:** Wonders are tracked globally via `CompletedWonderIds` in GameSimulation - when built, they cannot be built again.
- **Production Integration:** Wonders added to `ProductionProject` enum and production queue cycle.

### Phase 14: City UI Redesign (COMPLETED)

- **Layout Redesign:** City detail panel now uses a cleaner layout with header at top, map view in center, and bottom bar split into Buildings (left) and Production (right).
- **Map Visualization:** 5x5 grid shows city radius with terrain colors and worked tile indicators.
- **Yield Display:** Worked tiles show food/production/commerce values; citizen indicator (👤) shows which tiles are being worked.
- **Production Bar:** Progress bar shows current production project with shield count and cycle button.
- **Buildings List:** Shows all constructed buildings with wonder star indicator (★) for completed world wonders.
