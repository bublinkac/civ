# CivGame - Civilization III Remake

A high-fidelity, authentic 2D isometric 4X strategy game inspired by **Civilization III Complete (v1.22)**, engineered in **Godot 4** using **C# (.NET 10.0+)**.

The project features a decoupled, engine-agnostic simulation engine (`src/Core`) with a responsive Godot presentation layer (`src/Render` & `src/UI`), complete with autonomous AI rivals, cylindrical globe geometry, LAN/Hotseat multiplayer, and Civ3 mechanics.

---

## 🌟 Key Features

- **Cylindrical Globe Geometry (World Wrap)**:
  - Seamless East-West wrapping across the X-axis ($X = 0 \leftrightarrow X = \text{Width} - 1$).
  - Shortest-path distance metrics (Chebyshev, Manhattan, and authentic Civ3 capital distance corruption formulas).
  - Cross-seam continent generation, trade networks, water access, and military operations.
- **Autonomous AI Rival Brain**:
  - Independent decision-making for 16 historic leaders with authentic personality archetypes.
  - Strategic city founding, worker improvements, threat-based garrisons, and coordinated military sieges.
- **High-Performance Zero-Allocation A\* Pathfinder**:
  - Pre-allocated flat arrays with a custom binary min-heap priority queue.
  - Supports multi-turn movement, road/railroad speedups, zone of control, and cross-seam navigation.
- **Civ3 Government & Happiness System**:
  - 7 authentic governments (Despotism, Monarchy, Republic, Democracy, Communism, Feudalism, Fascism).
  - Anarchy countdowns, war weariness points, military police, and citizen mood calculations (Happy, Content, Unhappy).
- **Multiplayer Support**:
  - **Local Hotseat**: Seamless pass-and-play with privacy transition covers and separate per-player fog of war.
  - **Peer-to-Peer TCP/LAN**: Low-latency socket synchronization using deterministic full-state DTO packets.
- **6-Advisor System (F1 - F6)**:
  - Full suite of advisors: Domestic (F1), Trade (F2), Military (F3), Foreign (F4), Cultural (F5), and Science (F6).
- **Espionage & Diplomacy**:
  - Bilateral trade agreements (lump sum gold, gold per turn, techs, treaties).
  - Intelligence Agency missions (plant spies, investigate cities, steal technology, sabotage production).
- **Tactical Combat, Air Missions & Great Leaders**:
  - Artillery/Naval shore bombard, air strikes, air recon, and rebase with tactical reticle overlays.
  - Elite unit veterancy, Great Military Leaders, Army creation, and wonder hurry production.
- **Dynamic Cataclysms & Pollution**:
  - Active volcanic eruptions damaging adjacent tiles, and industrial/population toxic pollution cleanup.

---

## ⌨️ Controls & Keyboard Shortcuts

| Shortcut | Action | Description |
|---|---|---|
| **F1** | Domestic Advisor | Treasury, tax rates, luxury spending, city ledger |
| **F2** | Trade Advisor | Commercial networks, trade routes, active treaties |
| **F3** | Military Advisor | Army rosters, global military power comparison |
| **F4** | Foreign Advisor | Diplomatic matrix, bilateral negotiations |
| **F5** | Cultural Advisor | Culture ranks, Top 5 cities, wonder listings |
| **F6** | Science Advisor | Interactive era-based technology tree |
| **F7** | Government Panel | Initiate revolution, choose new government |
| **F8** | Histograph | Real-time score, territory, and culture line graphs |
| **Spacebar** | Skip / Wait | Yields current unit's turn to next available unit |
| **F** | Fortify | Fortifies military unit for +25% defense bonus |
| **S** | Sentry | Puts unit to sleep until an enemy approaches |
| **P** | Pillage | Destroys improvements in enemy/neutral territory |
| **B** | Bombard / Air Target | Activates tactical reticle for artillery/air missions |
| **U / Shift+U** | Upgrade Unit | Upgrades active unit (or all units of type) in city |
| **W, A, S, D** / Arrows | Camera Pan | Scrolls isometric camera across the map |
| **Mouse Wheel** | Zoom In / Out | Smooth camera zoom adjustment |
| **Enter** | End Turn | Finalizes turn and triggers simulation processing |

---

## 🚀 Building & Running

### Prerequisites
- [.NET 10.0+ SDK](https://dotnet.microsoft.com/download)
- [Godot Engine 4 (with .NET Support)](https://godotengine.org/download)

### 1. Build the C# Solution
From the root workspace directory (`c:/_private/civ`):
```bash
dotnet build
```
The solution should compile cleanly with **0 Errors**.

### 2. Launch the Game
Open the project in Godot 4:
```bash
godot --editor .
```
Or run the main scene directly (`main.tscn`):
```bash
godot main.tscn
```

---

## 📖 Project Documentation

For in-depth technical details, architectural rules, and development progress:
- 🗺️ **[DEVELOPMENT_PLAN.md](file:///c:/_private/civ/DEVELOPMENT_PLAN.md)**: Comprehensive log of all completed phases (Phases 1 through 42) and the future development backlog.
- 📐 **[AI_BLUEPRINT.md](file:///c:/_private/civ/AI_BLUEPRINT.md)**: Architectural specifications, folder structures, coordinate wrap math, networking protocols, and invariant guidelines.
