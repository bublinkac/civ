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
