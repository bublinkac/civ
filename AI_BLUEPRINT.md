# AI Blueprint - Civilization III Isometric Strategy Game

## Tech Stack
- **Engine**: Godot Engine 4 (C# / .NET version)
- **Language**: C# (strictly typed, .NET 10.0+)
- **Architecture**: Decoupled, Data-Driven & Modular Architecture

---

## Directory Structure

```text
c:/_private/civ/
├── assets/                     # Graphic resources (textures, sprites, tiles)
├── src/
│   ├── Core/                   # Pure C# Game Logic & Simulation (Engine-agnostic)
│   │   ├── TileData.cs
│   │   ├── GameMap.cs
│   │   └── MapGenerator.cs
│   ├── Render/                 # Godot Rendering Bridge & Visualization
│   │   └── MapRenderer.cs
│   └── UI/                     # UI components and UI controllers
├── project.godot               # Godot Project Config
├── CivGame.csproj              # C# Project File
└── AI_BLUEPRINT.md             # Architecture documentation
```

---

## Architectural Guidelines

### 1. Separation of Concerns (Core vs. Render)
- **Core (`/src/Core`)**: Contains the game simulation state, logic, rules, and mathematical generation algorithms. This folder **must not** import the `Godot` namespace. It runs entirely on pure C# types to ensure portability and ease of headless execution or automated unit testing.
- **Render (`/src/Render`)**: Inherits from Godot classes (`Node2D`, `TileMapLayer`, etc.). It acts as a bridge, querying the Core layer and updating Godot's visual scene tree based on simulation state changes.

### 2. Map Generation
- Generation uses custom mathematical Perlin Noise to ensure portability without depending on Godot's visual or utility classes.
- Terrain is mapped dynamically:
  - Elevation/Moisture threshold equations translate coordinates into terrain types.

### 3. Isometric Map Coordinates
- The isometric system uses a **Diamond 45°** layout.
- Render Tile size is `256x128` (or custom 2:1 ratio) to support detailed textures.
- The map coordinates `(x, y)` from `GameMap` map to Isometric coordinates via standard screen space conversion.

### 4. Code Standards
- Strict typing, no dynamic types unless absolutely necessary.
- Zero placeholder code or unhandled exceptions.
- Clear namespaces (e.g., `CivGame.Core` and `CivGame.Render`).
