# CivGame - Development Plan & Implementation Status

This document serves as an overview of achieved progress and a high-level plan for further development phases of the 2D isometric 4X strategy game.

---

## 🛠️ What's Done (Current Status)

### 1. Isometric Engine & Render (`src/Render`)
- Rendering of isometric map tiles, roads, railroads, improvements, and units.
### 13. Wonders & National Wonders (COMPLETED)
- Wonders (e.g. Pyramids, Great Wall) and National Wonders (e.g. Forbidden Palace, Iron Works) with unique bonuses and effects.
### 14. Leaders, Civilizations & Main Menu (COMPLETED)
- Civilizations registry with unique traits, starting techs, and Unique Units.
- Dynamic Main Menu with leader selection, unique unit details, and leader portraits.
- Empire HUD displaying player's leader portrait and active civilization name.
### 15. Capital City & Relocation Mechanics (COMPLETED)
- Palace-centric Capital City system marking the first city as capital.
- Auto-relocation of Palace and Capital status to the oldest remaining city upon capture/razing.
- Palace construction project to manually relocate Capital status.
- Chebyshev distance-based corruption (reducing commerce) and waste (reducing production) affected by Courthouses, Police Stations, and Forbidden Palaces.
### 16. Advanced City Mechanics (COMPLETED)
- **Cultural Border Expansion:** Real accumulated culture with threshold expansion (radii 1 to 5 at 10, 100, 1000, 10000 culture).
- **Citizen Happiness & Mood:** Citizens categorized as Happy, Content, or Unhappy. Influenced by difficulty, Temples, Colosseums, Cathedrals, Military Police, and Luxuries (amplified by Marketplace).
- **Civil Disorder (Revolts):** Cities with more Unhappy than Happy citizens enter Revolt, completely halting all production and commerce.
- **Population Growth Limits:** Growth capped at size 6 (Town) without Fresh Water (adjacent to Coast/Floodplains) or Aqueducts, and size 12 without a Hospital.
### 17. Parametric Turn Year Timeline (COMPLETED)
- Parametric year calculation converting turn number into historical calendar years (BC/AD) with non-linear segment speeds matching Civilization 3 (from 50 years/turn down to 1 year/turn).
- Replaces hardcoded approximations in City Header panels, City Founding history, and the Main HUD.
### 18. Modular Pollution & Cleanup System (COMPLETED)
- Object-oriented Pollution system with dynamic, parametric Population-based pollution (starts above size 12) and Industrial-based pollution (Factory, Coal Plant, Iron Works, and high production).
- Turn-based probability roll triggers active Pollution breakout on city worked tiles.
- Polluted tiles produce 0 yields, with organic toxic orange/brown mud and green bubbling sludge custom visual rendering overlays.
- Workers can execute "Clean Pollution" project (takes 2 turns) to restore tiles.
### 19. Volcano Terrain & Eruption Mechanics (COMPLETED)
- Implemented **Volcano** as a unique, highly strategic, and hazardous terrain type (impassable for wheeled units, defense bonus +50%, yields: 0 food, 3 shields, 0 commerce).
- Procedural **Volcano Drawing** generating beautiful basalt charcoal-gray volcanic mountains with custom red-and-orange boiling magma craters and animated, billowy ash smoke plumes.
- **Deterministic Map Generation** creating rare, realistic volcano cones on 15% of mountain ranges.
- **Volcanic Eruptions:** At the end of a turn, each volcano has a 0.35% chance to erupt, spewing molten magma (creating Pollution tiles with a 60% chance), vaporizing any units standing on the volcano, damaging adjacent units by 50 HP, and hitting adjacent cities (reducing population by 1 and destroying a random building).
### 20. Advanced Civilization III Game Setup & Generation Options (COMPLETED)
- Implemented a unified `GameSetupOptions` model that carries customized world and civilization settings from the Main Menu into the map generator.
- Added support for selecting **"🎲 Random"** for Player civilization, AI Rival civilization, and all geographic parameters (random values are deterministically resolved using the seed).
- Parameterized **Water Coverage** (60% Water, 70% Water, 80% Water) affecting procedural sea/coast boundaries.
- Parameterized **Geological Age** (3 Billion [rugged mountains/abundant hills/25% volcano chance], 4 Billion [normal], 5 Billion [highly eroded/rare mountains/5% volcano chance]).
- Parameterized **Climate** (Arid [moisture penalty, expanding deserts/plains], Normal, Wet [moisture bonus, expanding grass/forests/jungles]).
- Parameterized **Temperature** (Warm [smaller tundra, wider equatorial jungles], Temperate, Cold [wider tundra, smaller jungles]).

---

## 🎯 High-Level Plan (Roadmap)

### Phase 11: Gold, Economy & Domestic Advisor (PARTIALLY COMPLETED)
- Basic tax rate adjustments, maintenance tracking, and domestic adviser.

### Phase 12: Technology Tree & Building Prerequisites (COMPLETED)
- Research-driven building unlocking and tech tree progressions.

### Phase 13: Wonders & National Wonders (COMPLETED)
- Wonder claims and unique gameplay overrides.

### Phase 14: Professional City Screen Redesign (COMPLETED)

- [x] **Architecture Refactoring & Component Library:**
    - [x] Redesign `CityDetailPanel` to a modular container layout.
    - [x] Build a library of reusable UI components.
- [x] **Layout Orchestration:**
    - [x] Create main `CityDetailPanel` layout engine.
- [x] **Component Implementation:**
    - [x] **Component: Header:** Implement UI for name, date, gold, gov, population, nav, plus revolt indications.
    - [x] **Component: StrategicResources:** Icon row with resource quantity indicators.
    - [x] **Component: InteractiveMap:** Implement terrain grid, citizen toggles, yields.
    - [x] **Component: BuildingsList:** Implement icon-based inventory, stats, culture bonuses.
    - [x] **Component: ProductionQueue:** Implement active project, progress, queue management, and corruption/waste logs.
    - [x] **Component: Granary:** Implement food storage grid visualization.
    - [x] **Component: EconomyModule:** Implement commerce sliders, happiness (luxuries/unhappiness icons).
    - [x] **Component: Garrison:** Implement stationed units list.

### Phase 15: Leaders & Selection Screen (COMPLETED)
- [x] Register Civilization database with unique bonuses/Unique Units.
- [x] Main Menu OptionButton and leader portrait visualizer.
- [x] HUD integrating player's active civilization.

### Phase 16: Capital, Corruption & Advanced City Mechanics (COMPLETED)
- [x] **Palace & Capital Relocation:** Mark capital, move on capture/razing, move via Palace project.
- [x] **Corruption & Waste:** Chebyshev distance formulas, Forbidden Palace, Courthouse/Police Station reductions.
- [x] **Dynamic Border Expansion:** Accumulate real culture and expand territory radius dynamically.
- [x] **Mood & Revolt Engine:** Dynamic happiness state machine with MP, lux, buildings; halt production/commerce on revolt.
- [x] **Authentic Growth Limits:** Size 6/12 limits requiring Fresh Water/Aquaducts/Hospitals.

### Phase 17: Parametric Turn Year Timeline (COMPLETED)
- [x] **Data-Driven Configuration:** Segment system with custom thresholds and increments.
- [x] **Civ3 Timeline Realism:** Non-linear scale mapping 4000 BC to 2050 AD.
- [x] **Calendar Integration:** Historical BC/AD conversions, bypassing year 0.
- [x] **UI Synchronization:** Applied to main HUD and City Header Panel.

### Phase 18: Modular Pollution & Cleanup System (COMPLETED)
- [x] **OOP Calculations:** Dedicated `PollutionSystem` with configurable thresholds (pop > 12, industry/buildings).
- [x] **Breakout Mechanics:** Weighted probability rolls to spawn active pollution events on worked tiles during turns.
- [x] **Yield Impact:** Zero out (0 food, 0 production, 0 commerce) all yields on polluted tiles.
- [x] **Toxic Rendering:** Custom `PollutionRenderer` (isometric Godot `TileMapLayer`) drawing organic brown/orange mud and toxic green bubbling sludge.
- [x] **Worker Action:** "Clean Pollution" project (2 turns) restoring tile state and yields.
- [x] **UI Reporting:** Integrated real-time pollution points and per-turn probabilities in the City Economy panel.
- [x] **Persistence:** Full save/load support for polluted tiles in `ISaveSystem`/`JsonSaveSystem`.

### Phase 19: Volcano Terrain & Eruption Mechanics (COMPLETED)
- [x] **Terrain Definition:** Registered Volcano in `TerrainRegistry` with custom yields (0 Food, 3 Shields, 0 Commerce), 50% defense, and impassable for wheeled units.
- [x] **Visual Design:** Implemented procedural isometric Volcano asset with dark basalt charcoal stone, glowing orange/red molten magma crater, and transparent rising smoke billows.
- [x] **World Generator Integration:** Seed-based deterministic world generator converting 15% of mountains into hazardous volcano ranges.
- [x] **Disaster Simulation:** Turn-based simulation processing random volcanic eruptions (0.35% chance/turn).
- [x] **Cataclysmic Impact:** Eruptions vaporize units on the volcano, damage adjacent units (50 HP), pollute adjacent land (60% chance), and strike adjacent cities (kill 1 citizen, destroy 1 building).

### Phase 20: Advanced Civilization III Game Setup & Generation Options (COMPLETED)
- [x] **Setup Options Model:** Unified C# `GameSetupOptions` carrying screen parameters seamlessly.
- [x] **Dropdown Integration:** Beautiful 2x2 GridContainer setup in MainMenu with classic custom selections.
- [x] **Randomizer Support:** Integrated "🎲 Random" options for player faction, rival, and geographic parameters, deterministically resolving based on the seed.
- [x] **Water Coverage Parameterization:** Generates continents or archipelagos by dynamically adjusting land/ocean elevation bands (60%, 70%, 80%).
- [x] **Geological Age Parameterization:** Younger worlds (3B years) have rugged elevations, tighter mountain/hill bands, and high volcano density (25% chance); older worlds (5B years) are flatter with rare volcanoes.
- [x] **Climate Parameterization:** Scales moisture thresholds (Arid drylands, balanced Normal, lush wet grasslands and swamps).
- [x] **Temperature Parameterization:** Adjusts polar bands (tundra) and equatorial tropical bands (jungle/forest) to fit Cold, Temperate, or Warm climates.

### Phase 21: Super Ultra Modern Optimized Pathfinder (A*) (COMPLETED)
- [x] **Zero-Allocation Memory Architecture:** Pre-allocated flat index arrays (`float[] _gScore`, `int[] _cameFrom`, `bool[] _closedSet`) sized to the map dimensions. This allows direct O(1) memory lookup, avoiding slow dictionary hashes, object instantiation, and garbage collector overhead.
- [x] **Custom Binary Min-Heap Priority Queue:** High-performance, zero-allocation binary heap supporting O(log N) insert/pop and fast O(log N) decrease-key operations for node updates.
- [x] **Accurate Civilization III Movement Semantics:** Models multi-turn movement restrictions, road cost reductions (1/3 MP), railroad cost-free travel (0 MP), and faction-specific rules (impassable enemy tiles vs friendly passable tiles), finding paths that minimize actual turns taken.
- [x] **Intelligent AI Navigation Integration:** Integrated into the Barbarian turn management system, enabling Barbarian units to plan paths around mountain ranges, oceans, and lakes, with an automated direct-step fallback.

### Phase 22: Authentic Civilization III Traits Integration (COMPLETED)
- [x] **Trait-Specific Production Cost Reductions:** Implemented 50% discount for characteristic buildings/units based on civilization traits:
  - **Scientific:** Half-price Libraries, Universities, and Research Labs.
  - **Agricultural:** Half-price Granaries.
  - **Militaristic:** Half-price Barracks and Coastal Fortresses.
  - **Religious:** Half-price Temples and Cathedrals.
  - **Commercial:** Half-price Marketplaces, Banks, and Stock Exchanges.
  - **Seafaring:** Half-price Harbors and Commercial Docks.
  - **Expansionist:** Half-price Explorers.
- [x] **Dynamic City Center Yield Buffs:**
  - **Agricultural:** +1 Food in the City Center tile (boosts early expansion and settlement).
  - **Industrious:** +1 Production in the City Center tile (faster early infrastructures).
  - **Commercial:** +1 Commerce in the City Center tile.
  - **Seafaring:** +1 Commerce in the City Center tile specifically for Coastal Cities.
- [x] **Militaristic Combat Tuning:** Added +10% attack strength for units belonging to Militaristic civilizations and +10% defense strength for defending militaristic units (emulates increased tactical skill and combat training).
- [x] **Industrious Construction Buff:** Workers belonging to Industrious civilizations gain a 50% increase in build speed, completing roads, mines, and farms in half the required turns.
- [x] **Commercial Anti-Corruption Measures:** Decreases the distance-based corruption rate in all non-capital cities of Commercial civilizations by 25%.
- [x] **Scientific Academic Boost:** Boosts national science generated every turn by +10% for Scientific civilizations.
- [x] **Expansionist Rapid Reconnaissance:** Starts the game with an extra starting Explorer (scout) for player or AI civilizations possessing the Expansionist trait to capture goody huts and map territories rapidly.
- [x] **Religious Spiritual Peace:** Enhances the happiness generated by Temples (+2 content instead of +1) and Cathedrals (+4 content instead of +3) for Religious civilizations.
- [x] **Code Validation:** Verified compile safety and flawless execution with a successful build of the unified model.

### Phase 23: State-of-the-Art AI Rival Brain (Leader Strategies & Automated Decisions) (COMPLETED)
- [x] **Leader Strategy Configurations:** Registered unique personality profiles for all 16 core leaders (Lincoln, Montezuma, Hammurabi, Mao, Cleopatra, Elizabeth, Joan d'Arc, Bismarck, Alexander, Gandhi, Hiawatha, Tokugawa, Xerxes, Caesar, Catherine, Shaka), capturing their classic Civ3 aggression levels, wonder-building tendencies, and building/unit type preferences.
- [x] **Threat-Aware City Production Planning:** Cities evaluate local defensive status (garrisons) and immediate nearby unit threats, prioritizing defender training (Archers/Warriors) under pressure, Aqueducts when at population limits, Great Wonders for wonder builders, and preferred thematic infrastructure (Temples/Libraries/Marketplaces) over default unit spam.
- [x] **State-of-the-Art Settler Automation:** Settlers scan the map to locate ideal, high-yield settlement sites at least 4 tiles away from existing cities, map the routes using the pre-allocated flat array `Pathfinder`, travel intelligently around water/mountain blockages, and automatically found cities upon arrival.
- [x] **State-of-the-Art Worker Automation:** Workers dynamically evaluate worked city tiles to prioritize cleaning pollution, building mines/farms on unproved lands, and networking with roads, traveling efficiently using pathfinding.
- [x] **Shroud-Aware Explorer Automation:** Scouts scan the fog of war for closest unexplored sectors and map paths to reveal the shroud systematically.
- [x] **Tactical Military Groupings & Target Runs:** Combat units group, garrison undefended cities, hunt barbarian camps, or march along planned optimal paths to siege enemy player cities if at war.
- [x] **System Integration & Compile Safety:** Delegated turn processing seamlessly from `GameSimulation.cs` to the isolated `AiRivalBrain` static class and verified the build succeeds flawlessly.

### Phase 24: Authentic Civilization III Difficulty Levels (COMPLETED)
- [x] **Difficulty Level Enum & Settings Class:** Created `DifficultyLevel` enum (Chieftain, Warlord, Regent, Monarch, Emperor, Deity) and `DifficultySettings` class with authentic Civ3 parameters per level, including base content/unhappy citizens, AI yield multipliers, AI production cost multipliers, extra AI starting units/techs, barbarian spawn intervals, player research multipliers, and player corruption modifiers.
- [x] **Difficulty Selection UI:** Integrated a styled difficulty dropdown (`OptionButton`) in the Main Menu game setup screen, positioned between the geographic parameters grid and the seed row, defaulting to Regent (the balanced middle level).
- [x] **GameSetupOptions Integration:** Added `Difficulty` property to `GameSetupOptions` and included it in `ResolveRandom()` so the selected difficulty flows seamlessly from the Main Menu through to the game simulation.
- [x] **Citizen Mood & Happiness Scaling:** `City.UpdateCitizenMood` now accepts `DifficultySettings` — easier levels (Chieftain: 4 content, 0 unhappy) keep citizens happier, while harder levels (Deity: 1 content, 3 unhappy) cause widespread unrest from the start, matching authentic Civ3 behavior.
- [x] **AI Yield Multiplier:** At higher difficulties, AI rival cities receive multiplied production and commerce yields (e.g., Deity: +60%, Emperor: +30%), while at lower difficulties AI yields are reduced (Chieftain: -50%), applied post-corruption in `CollectCityYields`.
- [x] **Player Research & Corruption Bonuses:** Easier levels grant the player a research multiplier (Chieftain: +20%, Warlord: +10%) and reduced corruption/waste (Chieftain: -25%, Warlord: -10%), applied after trait modifiers but before building reductions.
- [x] **Barbarian Spawn Scaling:** Barbarian spawn interval is controlled by difficulty — no barbarians at Chieftain, every 16 turns at Warlord, 8 at Regent, 6 at Monarch, and 4 at Emperor/Deity, making early-game defense increasingly critical.
- [x] **AI Extra Starting Units:** At Monarch+ the AI receives bonus starting Warriors (1 at Monarch, 2 at Emperor, 3 at Deity), giving the AI an early military advantage that mirrors Civ3's handicap system.
- [x] **End-to-End Integration:** Difficulty flows from `MainMenu` → `GameSetupOptions` → `MapRenderer.StartGame` → `GameSimulation.Difficulty` → all affected systems, with `DifficultyConfig` accessor providing cached settings throughout the simulation.
- [x] **Compile Safety:** Build verified successfully with zero errors.

## Phase 25 — Victory Conditions (Full Implementation)

Implemented all 6 Civ3 victory types for both player and AI, plus AI research system.

### Victory Types
- [x] **Conquest Victory:** Eliminate all rival cities + settlers → `VictoryConquest` / `DefeatConquest`
- [x] **Domination Victory:** Control 2/3 of world's land tiles AND 2/3 of population → `VictoryDomination` / `DefeatDomination` (both player and AI checked)
- [x] **Cultural Victory:** Any single city accumulates 50,000+ culture → `VictoryCultural` / `DefeatCultural` (both player and AI checked)
- [x] **Diplomatic Victory:** Build United Nations wonder + control >50% population → `VictoryDiplomatic` / `DefeatDiplomatic` (both factions)
- [x] **Space Race Victory:** Build Apollo Program + all 10 spaceship parts → `VictorySpaceRace` / `DefeatSpaceRace` (tracked for both player `BuiltSpaceshipParts` and AI `AiBuiltSpaceshipParts`)
- [x] **Histograph (Score) Victory:** Turn limit reached, compare scores → `VictoryScore` / `DefeatScore`

### AI Research System
- [x] **AI Research Tracking:** `AiResearchedTechs`, `AiCurrentResearchId`, `AiScienceProgress` fields in `GameSimulation`
- [x] **ProcessAiResearch():** AI accumulates science from city commerce each turn, picks cheapest available tech with prerequisites met, auto-researches
- [x] **Integration:** Called in `EndTurn()` after `ProcessAiRivalTurn()`
- [x] **AI Score Fix:** `CalculateScore` now uses actual `AiResearchedTechs.Count` instead of rough approximation
- [x] **CultureOutput Update:** `city.CultureOutput` set each turn in `CollectCityYields` alongside `AccumulatedCulture`

### Save System Updates
- [x] Added `AiCurrentResearchId`, `AiScienceProgress`, `BuiltSpaceshipParts`, `AiBuiltSpaceshipParts` to `SaveDataDto`
- [x] Full serialization/deserialization in `JsonSaveSystem`

### UI Updates
- [x] All new defeat states (`DefeatDomination`, `DefeatCultural`, `DefeatSpaceRace`, `DefeatDiplomatic`) added to `GameEndState` enum with corresponding messages in `GameHud`

## Phase 26 — Advisor System (Civ3-Authentic)

Full 6-advisor system matching Civ3's F1-F6 keybinding layout.

### Advisor Panels
| Key | Advisor | File | Status |
|-----|---------|------|--------|
| F1 | Domestic | `DomesticAdvisorPanel.cs` | Full — economy, tax slider, per-city table |
| F2 | Trade | `TradeAdvisorPanel.cs` | New — commerce overview, treasury stats, city trade table |
| F3 | Military | `MilitaryAdvisorPanel.cs` | Wired — army comparison, unit roster, threat assessment |
| F4 | Foreign | `ForeignAdvisorPanel.cs` | New — diplomacy table, war/peace status, rival civ info |
| F5 | Cultural | `CulturalAdvisorPanel.cs` | Full rewrite — Civ3 culture levels, Top 5 ranking, wonders, victory progress |
| F6 | Science | `TechTreePanel.cs` | Full — tech tree with era tabs, advisor overlay |

### AdvisorsMenu Redesign
- [x] All 6 advisors listed in Civ3 F1-F6 order with color-coded buttons, hotkey labels, and descriptions
- [x] Unified dark modal style with gold accent borders

### Cultural Advisor (Civ3-Faithful)
- [x] **Advisor portrait + speech bubble:** Context-sensitive advice (recommends Monuments → Temples → Libraries → Cathedrals)
- [x] **National stats panel:** Total culture, per-turn output, best city, victory fraction
- [x] **City Culture Table:** Columns for City, Culture/Turn, Total, Level with zebra striping
- [x] **Civ3 Culture Levels:** Unknown → Fledgling (10) → Developing (100) → Refined (1000) → Influential (5000) → Distinguished (10000) → Legendary (50000) with color-coded labels
- [x] **Top 5 Cities Ranking:** Combined player + AI cities ranked by culture (green = player, red = rival)
- [x] **Wonders List:** All player wonders with hosting city names

### MapRenderer Integration
- [x] Added fields for all 4 new panels (`_militaryAdvisorPanel`, `_foreignAdvisorPanel`, `_culturalAdvisorPanel`, `_tradeAdvisorPanel`)
- [x] Opener methods wired to `AdvisorsMenu` events
- [x] F1-F6 keyboard hotkeys for direct advisor access
- [x] `IsFullScreenUiOpen` updated to include all panels

---

## Phase 27: Happiness, War Weariness & Government System

### Government System (`src/Core/Government.cs`) — Civ3 Complete v1.22
- [x] **GovernmentType enum:** Despotism, Monarchy, Republic, Democracy, Communism, Feudalism, Fascism
- [x] **HurryMethod enum:** None, ForcedLabor (1 pop = 20 shields), PayCitizens (4 gold = 1 shield)
- [x] **CorruptionLevel enum:** Minimal, Nuisance, Problematic, Communal, Rampant, Catastrophic
- [x] **Government class** with full Civ3-authentic properties per type:
  - **Unit Support per Town/City/Metropolis** (pop 1-6 / 7-12 / 13+):
    - Despotism: 4/4/4, Monarchy: 2/4/8, Republic: 1/3/4, Democracy: 0/0/0
    - Communism: 6/6/6, Feudalism: 5/2/1, Fascism: 4/7/10
  - **Unit Support Cost:** 1gpt (most), Republic 2gpt, Feudalism 3gpt
  - **Worker Efficiency:** Despotism 50%, Democracy 150%, Fascism 200%, rest 100%
  - **Hurry Method:** Despotism=None, Monarchy/Republic/Democracy=PayCitizens, Communism/Feudalism/Fascism=ForcedLabor
  - **Military Police:** Despotism 2, Monarchy 3, Communism 4, Fascism 4, Feudalism 3, Republic/Democracy 0
  - **War Weariness:** None (Despotism/Monarchy/Communism/Fascism), Low (Republic/Feudalism), High (Democracy)
  - **Draft Rate:** 0-4 citizens per city per turn depending on government
  - **Corruption:** Communal flat for Communism, distance-based for others (CorruptionModifier applied)
  - **Commerce Bonus:** Republic/Democracy +1 commerce on tiles producing ≥1
  - **Tile Penalty:** Despotism/Feudalism -1 on any yield ≥ 3
  - **Anarchy duration:** 2-8 turns depending on government
- [x] **GameSimulation state:** `PlayerGovernment`, `AiGovernment`, `AnarchyTurnsRemaining`
- [x] **Worker efficiency** applied to `ProcessWorkerConstruction` (50%=half speed, 200%=double)
- [x] **Government commerce bonus** (+1 commerce) applied in `CollectCityYields`
- [x] **Despotism tile penalty** (-1 on yields ≥ 3) applied in `CollectCityYields`
- [x] **Corruption uses government type:** Communal=flat 20%, others=distance×modifier

### War Weariness (`src/Core/GameSimulation.cs`)
- [x] **WWP (War Weariness Points) tracking** — Civ3-authentic thresholds: 0-30 no effect, 31-60 level 1, 61-90 level 2, 91-120 level 3, 121+ level 4
- [x] **WWP accumulation:**
  - +1 per turn with player units in enemy territory
  - +2 per unit lost in combat
  - Defensive war offset: -30 WWP initial (delayed WW when AI attacks first)
  - Natural decay when not at war: 1/20 per turn
  - Passive decay when quiet (no units in either territory): -1/turn if level >= 1
- [x] **Effect on citizens** (Republic):
  - Level 1: 25% unhappy, Level 2: 50%, Level 3: 50%, Level 4: 100%
- [x] **Effect on citizens** (Democracy):
  - Level 1: 50% unhappy, Level 2: 100%, Level 3+: revolt/anarchy
- [x] **Police Station** reduces WW effect by 25%
- [x] **OnWarDeclared / OnPeaceSigned** lifecycle hooks

### Citizen Mood Calculation (`src/Core/City.cs`)
- [x] **5-step Civ3-authentic calculation:**
  1. Base distribution (difficulty-based content/unhappy citizens)
  2. War weariness (Republic/Democracy — converts content→unhappy)
  3. Military police (government-limited — converts unhappy→content)
  4. Buildings (Temple 1, Colosseum 2, Cathedral 3 content faces; Religious trait bonuses)
  5. Luxuries (connected resources → happy faces; Marketplace amplifies per Civ3 formula: tiers of 2 → 1/2/3/4 faces each)
- [x] **Civil Disorder:** Triggers when unhappy > happy and population > 1
- [x] **Faction-aware:** Military police uses city's own faction units, government selection per faction

### AI Integration (`src/Core/AI/AiRivalBrain.cs`)
- [x] **Happiness-aware production planning:**
  - AI updates citizen mood before choosing production
  - Priority 0 (above defense): Build happiness buildings when in disorder
  - Escalation chain: Temple → Colosseum → Cathedral → Marketplace
  - AI considers tech prerequisites before choosing happiness buildings
- [x] **Faction-correct calculations:** AI cities use `AiGovernment` for all mood/police/WW checks

### Unit Support Cost (`src/Core/GameSimulation.cs`)
- [x] **Civ3 formula:** Free unit slots per city based on city size (Town/City/Metropolis)
  - Uses `Government.GetUnitSupport(population)` — Town: pop 1-6, City: pop 7-12, Metro: pop 13+
  - Total free slots = sum of all player cities' allowances
  - Excess units × `UnitSupportCost` deducted from gold each turn
- [x] **Deducted from treasury** each turn after commerce/maintenance calculation
- [x] **Bankruptcy disbanding:** If treasury goes negative, strongest non-settler/worker unit is disbanded
- [x] **LastTurnUnitSupport** property exposed for UI display
- [x] **Console logging** with breakdown (excess units × cost, government free slots)

---

## Phase 28: Golden Age (Civ3-authentic)

### GoldenAge Class (`src/Core/Civilization.cs`)
- [x] **Duration:** 20 turns
- [x] **One-time only:** `HasBeenUsed` flag prevents re-triggering
- [x] **Trigger method** with faction name + reason logging
- [x] **ProcessTurn** countdown with end message

### Triggers (`src/Core/GameSimulation.cs`)
- [x] **Unique Unit Victory:** First combat victory with a UU triggers Golden Age
  - Checked via `CivilizationId`, `ReplacedUnitType`, and UU bonus presence
  - Works for both attacker and defender winning
- [x] **Wonder Completion:** Building a wonder whose `AssociatedTraits` match BOTH civ traits
  - 17 wonders have trait associations (Pyramids=Agricultural+Industrious, etc.)
  - `CheckWonderGoldenAgeTrigger` called from `Wonder.OnCompleted`

### Yield Bonus (`src/Core/GameSimulation.cs` → `CollectCityYields`)
- [x] **+1 Production** on every worked tile producing ≥1 production
- [x] **+1 Commerce** on every worked tile producing ≥1 commerce
- [x] Applies to center tile and all citizen-worked tiles
- [x] Works for both Player and AI factions

### Wonder Trait Associations (`src/Core/Wonder.cs`)
- [x] `AssociatedTraits` property on `Wonder` base class
- [x] `GenericWonder` accepts optional `traits` parameter
- [x] Authentic Civ3 mappings:
  - Pyramids: Agricultural + Industrious
  - Hanging Gardens: Agricultural + Religious
  - Colossus: Commercial + Seafaring
  - Great Wall: Militaristic + Expansionist
  - Statue of Zeus: Militaristic + Religious
  - Oracle: Religious + Scientific
  - Knights Hall: Militaristic + Industrious
  - Leonardo's Workshop: Scientific + Industrious
  - Shakespeare's Theatre: Commercial + Religious
  - Sun Tzu's War Academy: Militaristic + Scientific
  - Cure for Cancer: Agricultural + Scientific
  - Sistine Chapel: Religious + Industrious
  - Hermitage: Expansionist + Religious
  - Smith's Mansion: Commercial + Scientific
  - Train Station: Industrious + Commercial
  - Internet: Scientific + Commercial
  - Longevity Vaccine: Agricultural + Religious

---

## Phase 29: Revolution & Anarchy System

### Government Change (`src/Core/GameSimulation.cs`)
- [x] **ChangeGovernment(type)** — starts revolution with random anarchy duration
- [x] **CanChangeGovernment(type)** — checks tech prereqs, not in anarchy, not same gov
- [x] **GetAvailableGovernments()** — returns governments player has unlocked
- [x] **PendingGovernment** — tracks which government will be installed after anarchy
- [x] **IsInAnarchy** property for easy checks
- [x] **Anarchy duration** — random within gov-specific range (2-8 turns)
- [x] **Religious trait** — anarchy reduced to 1 turn
- [x] **Leaving Despotism** — no anarchy (instant switch)
- [x] **Anarchy countdown** — processed each turn, installs PendingGovernment when done

### Anarchy Effects (`src/Core/GameSimulation.cs` → `CollectCityYields`)
- [x] **NO production** — all player cities produce 0 production during anarchy
- [x] **NO commerce** — all player cities produce 0 commerce (no gold, no science)
- [x] **Subsistence food** — cities get exactly enough food to not starve (pop × 2)
- [x] Applied AFTER all other yield calculations, BEFORE civil disorder check

### Government Panel UI (`src/UI/GovernmentPanel.cs`)
- [x] **Full-screen panel** with parchment style (matches advisor panels)
- [x] **Current status banner** — shows current government or anarchy countdown
- [x] **Government cards** for all 7 types showing:
  - Name, required tech, lock status
  - Worker efficiency, military police, war weariness, draft rate
  - Unit support (Town/City/Metropolis), cost, corruption level, hurry method
  - Special bonuses/penalties (tile penalty, commerce bonus, communal corruption)
- [x] **Revolution button** per available government with confirmation dialog
- [x] **Anarchy warning** in confirmation (shows duration range, effects)

### HUD Integration (`src/UI/GameHud.cs`, `src/Render/MapRenderer.cs`)
- [x] **Government button** in top bar: shows "⚖ {GovName}" or "⚠ ANARCHY (N)"
- [x] **Red text** during anarchy for visibility
- [x] **F7 keyboard shortcut** for quick access
- [x] **UI refresh** after government change

---

## Phase 30: Starvation & Civil Disorder (Civ3-authentic)

### Starvation (`src/Core/GameSimulation.cs` → `CollectCityYields`)
- [x] **Population loss** when StoredFood < 0 and pop > 1
- [x] **Building destruction** — most expensive non-essential building destroyed on starvation
  - Palace and Granary protected from destruction
- [x] **StoredFood reset** to 0 after population loss (no soft cushion)
- [x] **Pop 1 protection** — city cannot shrink below 1 citizen

### Civil Disorder (`src/Core/GameSimulation.cs` → `CollectCityYields`)
- [x] **Production halted** — 0 production during disorder
- [x] **Commerce halted** — 0 commerce (no gold, no science)
- [x] **Food surplus halted** — food capped at subsistence (pop × 2), no growth
- [x] **DisorderTurns counter** — tracks consecutive turns in disorder (`src/Core/City.cs`)
- [x] **Democracy collapse** — 3+ turns of continuous disorder triggers automatic revolution to Despotism (4 turns anarchy)

### AI Awareness (`src/Core/AI/AiRivalBrain.cs`)
- [x] **Starvation priority** — AI builds Granary when city has negative food (priority 0a, above disorder)
- [x] **Disorder priority** — AI already builds happiness buildings (Temple → Colosseum → Cathedral → Marketplace)

---

## Phase 31: Irrigation Chain (Civ3-authentic)

### HasIrrigationAccess (`src/Core/GameSimulation.cs`)
- [x] **Fresh water adjacency:** Farm can be built if adjacent to coast or floodplains
- [x] **City as source:** Cities act as irrigation sources (adjacent tiles can be irrigated)
- [x] **Irrigation chain:** Farm can be built if adjacent to another existing Farm
- [x] **Electricity bypass:** After researching Electricity, irrigation can be built anywhere
- [x] Checks all 8 adjacent tiles (including diagonals)

### Player Enforcement (`src/Render/MapRenderer.cs`)
- [x] Farm building blocked with message if no irrigation access
- [x] Existing improvement check preserved

### AI Enforcement (`src/Core/AI/AiRivalBrain.cs`)
- [x] AI checks `HasIrrigationAccess` before building Farm
- [x] Falls back to Mine if terrain supports it but no irrigation available

### UI (`src/UI/GameHud.cs`)
- [x] Irrigate button only visible when `HasIrrigationAccess` returns true

---

## Phase 32: Histograph (Civ3-style)

### Data Model (`src/Core/HistographData.cs`)
- [x] **HistographEntry** — per-turn snapshot: Turn, Score, Population, Territory, Culture, Military
- [x] **HistographData** — holds `PlayerHistory` and `AiHistory` lists
- [x] **RecordTurn()** — captures stats for both factions each turn
- [x] **5 categories:** Score, Population, Territory, Culture, Military
- [x] **Military power:** sum of AttackStrength + DefenseStrength of all units

### Recording (`src/Core/GameSimulation.cs`)
- [x] `Histograph.RecordTurn(this)` called at end of each turn before turn counter increment

### Persistence (`src/Core/ISaveSystem.cs`, `src/Core/JsonSaveSystem.cs`)
- [x] `PlayerHistograph` and `AiHistograph` in `SaveDataDto`
- [x] Saved/loaded with full turn history

### UI Panel (`src/UI/HistographPanel.cs`)
- [x] **Dark themed** full-screen panel with gold border
- [x] **Category buttons** — Score / Population / Territory / Culture / Military
- [x] **Line graph** — Player (blue) vs AI (red) lines drawn with Godot `_Draw()`
- [x] **Grid lines** — 5 horizontal + up to 10 vertical with axis labels
- [x] **Legend** — colored indicators for Player and AI civ names
- [x] **Endpoint values** — dot + value label at the latest data point
- [x] **Dynamic Y-axis** — auto-scales to max value + 10% headroom

### Integration
- [x] **📊 Histograph** button in HUD top bar
- [x] **F8 keyboard shortcut**
- [x] `open_histograph` action handler in MapRenderer

### Future TODOs
- Trade Advisor: Trade route visualization, luxury/strategic resource deals, import/export
- Foreign Advisor: Embassy system, tech/map trading, alliance proposals, multi-civ support
- Cultural Advisor: Culture borders map overlay, culture flip warnings, culture rate graph
- Happiness: Entertainment slider, We Love The King Day (WLTKD), Entertainers specialist


