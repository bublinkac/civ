# AI Blueprint & Architecture Guide - Civilization III 4X Strategy Game

This document provides a comprehensive technical architecture blueprint of the codebase, ensuring that any developer, AI agent, or contributor can immediately understand the system structure, design conventions, and underlying mechanics.

---

## 🛠️ Technology Stack & Environment

- **Engine**: Godot Engine 4 (C# / .NET edition)
- **Language**: C# 12 / .NET 10.0+ (Strictly typed, null-safety enabled)
- **Architecture**: Decoupled, Data-Driven, Engine-Agnostic Simulation with Godot Presentation Bridge
- **Persistence**: System.Text.Json with dedicated Data Transfer Objects (DTOs)
- **Networking**: Raw TCP socket client/server protocol with JSON framing (`\n`)

---

## 📂 Project Directory Structure

```text
c:/_private/civ/
├── CivGame.csproj                  # Godot .NET C# project specification
├── DEVELOPMENT_PLAN.md             # Complete phase-by-phase implementation log & future backlog
├── AI_BLUEPRINT.md                 # Architecture, subsystem design, and engineering guidelines
├── README.md                       # Project overview, controls, build instructions
├── project.godot                   # Godot 4 engine configuration
├── assets/                         # Visual & sound assets (audio, textures, sprites, icons)
│   └── sounds/                     # SFX (.wav files for UI, moves, combat, espionage)
├── scripts/                        # Utility & automation scripts
└── src/
    ├── Audio/                      # Audio subsystem & sound manager
    │   └── AudioManager.cs         # AutoLoad singleton with graceful degradation
    ├── Core/                       # Pure C# Game Logic & Simulation (Engine-Agnostic)
    │   ├── AI/
    │   │   └── AiRivalBrain.cs     # Autonomous AI decision-making (settlers, workers, war)
    │   ├── Diplomacy/
    │   │   ├── DiplomacyManager.cs # Trade agreements, GPT ticks, embassies
    │   │   └── TradeDeal.cs        # Bilateral deal data model
    │   ├── Pathfinding/
    │   │   └── Pathfinder.cs       # Pre-allocated flat-array A* pathfinding with MinHeap
    │   ├── City.cs                 # City state, mood calculations, growth, projects
    │   ├── Civilization.cs         # Civilization registry, traits, Golden Age state
    │   ├── EspionageSystem.cs      # Spy missions, intelligence agency, success rolls
    │   ├── Faction.cs              # Faction enum (Player, AiRival, Barbarians)
    │   ├── GameEndState.cs         # Victory/Defeat state definitions
    │   ├── GameMap.cs              # Cylindrical map topology, distance metrics, bounds
    │   ├── GameSetupOptions.cs     # Seeded parameters (climate, age, water coverage, civs)
    │   ├── GameSimulation.cs       # Central game loop, yields, combat, turn resolution
    │   ├── Government.cs           # Authentic Civ3 governments, unit support, war weariness
    │   ├── HistographData.cs       # Turn-by-turn historical metrics recording
    │   ├── ISaveSystem.cs          # Save/load contracts and DTO definitions
    │   ├── JsonSaveSystem.cs       # JSON serialization/deserialization engine
    │   ├── MapGenerator.cs         # Multi-octave Perlin noise deterministic generation
    │   ├── PollutionSystem.cs      # Industrial & population toxic pollution calculations
    │   ├── Specialists.cs          # Citizen specialist definitions (Scientist, Taxman, etc.)
    │   ├── TechManager.cs          # Era-based technology trees and research progress
    │   ├── Terrain.cs              # Terrain definitions and tile yields
    │   ├── TileData.cs             # Individual tile state, improvements, roads, rivers
    │   ├── TurnTimeline.cs         # Non-linear Civ3 BC/AD parametric calendar mapping
    │   ├── Unit.cs                 # Unit state, veterancy, experience, transport cargo
    │   ├── UnitDefinitionRegistry.cs # Registry of all units, stats, upgrade paths
    │   ├── VolcanoSystem.cs        # Volcanic eruption cataclysms and damage radius
    │   └── Wonder.cs               # Great and Small Wonder effects and trait bindings
    ├── Network/                    # Multiplayer TCP Network Layer
    │   └── CivNetworkManager.cs    # Host/Client TCP protocol, packet serialization & polling
    ├── Render/                     # Godot Visualization & Presentation Bridge
    │   ├── BorderRenderer.cs       # Cultural territory borders (TileMapLayer)
    │   ├── FogRenderer.cs          # Fog of War / shroud visualizer (TileMapLayer)
    │   ├── HighlightRenderer.cs    # Reachable movement tiles overlay (TileMapLayer)
    │   ├── MapRenderer.cs          # Master TileMap scene orchestrator, camera, input
    │   ├── PollutionRenderer.cs    # Toxic mud and bubbling sludge rendering
    │   ├── TargetingReticleRenderer.cs # Tactical bombard & air mission targeting grid
    │   └── UnitRenderer.cs         # Unit sprites, animation tweens, HP bars, stack sorting
    └── UI/                         # User Interface Panels & Controls
        ├── CityComponents/         # Modular City Detail components
        │   ├── CityCitizensComponent.cs
        │   ├── CityEconomyModuleComponent.cs
        │   ├── CityGarrisonComponent.cs
        │   ├── CityGranaryComponent.cs
        │   ├── CityHeaderComponent.cs
        │   ├── CityInteractiveMapComponent.cs
        │   ├── CityProductionQueueComponent.cs
        │   ├── CityProductionSelectionDialog.cs
        │   └── CityStrategicResourcesComponent.cs
        ├── AdvisorScreen.cs        # Tabbed container for F1-F6 advisors
        ├── AdvisorsMenu.cs         # Main F1-F6 launcher menu modal
        ├── CityCaptureDialog.cs    # Annex / Raze / Liberate modal upon conquest
        ├── CityDetailPanel.cs      # Master container for modular city screen
        ├── CulturalAdvisorPanel.cs # Culture levels, Top 5 cities, wonder lists
        ├── DiplomacyDialog.cs      # Bilateral negotiation table
        ├── DomesticAdvisorPanel.cs # Budget, tax/lux sliders, city ledger
        ├── EspionageScreen.cs      # Intelligence agency mission center
        ├── ForeignAdvisorPanel.cs  # Diplomatic relations matrix and treaties
        ├── GameHud.cs              # Main in-game HUD (stats, unit actions, minimap)
        ├── GovernmentPanel.cs      # Revolution & government selection dialog
        ├── HistographPanel.cs      # Score, territory, culture line graphs
        ├── HotseatTransitionModal.cs # Pass-the-device privacy cover for hotseat
        ├── MainMenu.cs             # Title screen, game setup, leader preview
        ├── MilitaryAdvisorPanel.cs # Army balance, unit rosters, threats
        ├── MultiplayerMenu.cs      # LAN host/join & hotseat launcher
        ├── TechTreePanel.cs        # Interactive era-based science tech tree
        └── TradeAdvisorPanel.cs    # Commerce overview and trade network status
```

---

## 🏛️ Core Architectural Principles

### 1. Engine-Agnostic Core (`src/Core/`)
- Code in `src/Core` **MUST NEVER** import or reference the `Godot` namespace.
- It operates strictly with standard C# libraries, pure math, collections, and primitives.
- **Why**: Allows headless testing, zero-allocation algorithms, deterministic simulations, and total separation of concerns between game rules and rendering.

### 2. Cylindrical World Wrap Geometry
- The world wraps horizontally along the X-axis ($X = 0 \leftrightarrow X = \text{Width} - 1$).
- **Normalization**: Whenever coordinates are received from user input, pathfinding offsets, or relative calculations, they MUST be normalized via `sim.Map.NormalizeX(x)`.
- **Distance Metrics**:
  - **$\Delta X$**: $\min(|x_1 - x_2|, \text{Width} - |x_1 - x_2|)$
  - **Chebyshev Distance**: $\max(\Delta X, |y_1 - y_2|)$ (used for 8-way moves, vision, air missions, bombard, combat).
  - **Manhattan Distance**: $\Delta X + |y_1 - y_2|$ (used for city spacing and worker priority).
  - **Civ3 Distance**: $\max(\Delta X, |y_1 - y_2|) + \lfloor\min(\Delta X, |y_1 - y_2|) / 2\rfloor$ (strictly used for capital distance corruption).
- **Array Bounds Rule**: 2D arrays (e.g. `VisibilityGrid[x, y]`, `Player2VisibilityGrid[x, y]`) have fixed bounds `[Width, Height]`. While `IsInBounds(x, y)` returns `true` for out-of-range $X$ when `WrapsX` is true, indexing arrays directly with un-normalized $X$ will cause `IndexOutOfRangeException`. Always normalize $X$ before array access.

### 3. Zero-Allocation A* Pathfinding (`Pathfinder.cs`)
- Employs pre-allocated flat arrays (`float[] _gScore`, `int[] _cameFrom`, `bool[] _closedSet`) sized to `Width * Height`.
- Uses a custom binary min-heap (`MinHeap`) for priority queue operations with direct index tracking.
- Admissible heuristic accounts for East-West wrapping using `Map.GetChebyshevDistance`.
- Accounts for roads ($1/3$ MP), railroads ($0$ MP), terrain costs, zone of control, and friendly/hostile borders.

### 4. Autonomous AI Brain (`AiRivalBrain.cs`)
- Completely decoupled static AI brain orchestrating all non-player actions:
  - **Settlers**: Evaluates candidate tiles across the globe based on freshwater, luxury/strategic resources, and proximity to friendly/rival cities.
  - **Workers**: Prioritizes clearing pollution, constructing roads to link cities, connecting resources, and irrigating/mining based on city needs.
  - **Scouts/Naval**: Dispatches units to unexplored fog clusters and patrols coastlines.
  - **Military Sieges & Garrisons**: Groups units, defends weakly garrisoned cities, hunts barbarian camps, and coordinates naval landings.

### 5. Multiplayer Architecture (`src/Network/` & `src/UI/`)
- Supports **Local Hotseat** and **Peer-to-Peer TCP LAN**.
- **State Serialization**: Entire game state is encapsulated in `SaveDataDto`. Transmitted over TCP with newline framing (`\n`).
- **Turn Flow**:
  1. Active player takes actions.
  2. On turn end, a `turn_state` message with serialized DTO is transmitted.
  3. Receiving client reconstructs `GameSimulation` via `JsonSaveSystem.ReconstructFromDto()`.
  4. Local player switches active faction and refreshes renderers.
- **Fog of War Privacy**: Separate visibility grids (`VisibilityGrid` for Player 1, `Player2VisibilityGrid` for Player 2). Fog layers automatically swap active buffers on turn transition.

### 6. Presentation & Rendering Bridge (`src/Render/`)
- Subclasses Godot 4 `TileMapLayer` and `Node2D`.
- Layering hierarchy:
  - Z-Index 0: Terrain (`MapRenderer`)
  - Z-Index 1: Highlight Layer (`HighlightRenderer`)
  - Z-Index 1: Border Layer (`BorderRenderer`)
  - Z-Index 1: Pollution Layer (`PollutionRenderer`)
  - Z-Index 1: Tactical Reticle Layer (`TargetingReticleRenderer`)
  - Z-Index 2: Units (`UnitRenderer`)
  - Z-Index 3: Fog of War (`FogRenderer`)
- **Seam Snap**: When a unit moves across the cylindrical seam, `UnitRenderer` snaps position immediately instead of tweening across the entire display.

---

## 🔒 Key Invariants & Developer Guidelines

1. **Do Not Break Core Independence**: Never add `using Godot;` inside any file in `src/Core/`.
2. **Preserve Documentation & Comments**: Always preserve existing docstrings, rationale comments, and license headers.
3. **Always Normalize Coordinates**: Use `Map.NormalizeX` whenever converting user or tile input to map coordinates.
4. **Compile Validation**: Run `dotnet build` from the workspace root (`c:\_private\civ`) to verify clean compilation with 0 errors before committing changes.
