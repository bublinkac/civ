using System.Collections.Generic;
using Godot;

namespace CivGame.Core;

public static class TerrainRegistry
{
    public static readonly Dictionary<string, Terrain> All = new();
    
    private class TerrainImpl : Terrain 
    {
        public TerrainImpl(string id, string name, TileYield baseYield, int movementCost, Color color) 
            : base(id, name, baseYield, movementCost, color) { }
    }

    static TerrainRegistry()
    {
        Register(new TerrainImpl("grassland", "Grassland", new TileYield(2, 0, 1), 1, new Color(0.2f, 0.55f, 0.2f)));
        Register(new TerrainImpl("plains", "Plains", new TileYield(1, 1, 1), 1, new Color(0.55f, 0.48f, 0.25f)));
        Register(new TerrainImpl("desert", "Desert", new TileYield(0, 0, 0), 2, new Color(0.85f, 0.82f, 0.45f)));
        Register(new TerrainImpl("tundra", "Tundra", new TileYield(1, 0, 0), 1, new Color(0.6f, 0.6f, 0.6f)));
        Register(new TerrainImpl("hills", "Hills", new TileYield(0, 1, 0), 2, new Color(0.5f, 0.4f, 0.2f)));
        Register(new TerrainImpl("mountain", "Mountain", new TileYield(0, 1, 0), 3, new Color(0.4f, 0.4f, 0.42f)));
        Register(new TerrainImpl("forest", "Forest", new TileYield(1, 2, 0), 2, new Color(0.1f, 0.3f, 0.1f)));
        Register(new TerrainImpl("jungle", "Jungle", new TileYield(1, 0, 0), 2, new Color(0.2f, 0.5f, 0.2f)));
        Register(new TerrainImpl("marsh", "Marsh", new TileYield(0, 0, 0), 2, new Color(0.3f, 0.5f, 0.4f)));
        Register(new TerrainImpl("floodplains", "Flood Plains", new TileYield(3, 0, 0), 1, new Color(0.8f, 0.7f, 0.3f)));
        Register(new TerrainImpl("ocean", "Ocean", new TileYield(1, 0, 2), 99, new Color(0.1f, 0.28f, 0.65f)));
        Register(new TerrainImpl("sea", "Sea", new TileYield(1, 0, 1), 99, new Color(0.2f, 0.4f, 0.75f)));
        Register(new TerrainImpl("coast", "Coast", new TileYield(2, 0, 1), 99, new Color(0.3f, 0.5f, 0.85f)));
    }

    private static void Register(Terrain terrain)
    {
        All[terrain.Id] = terrain;
    }

    public static Terrain? Get(string id) => All.TryGetValue(id, out var t) ? t : null;
}
