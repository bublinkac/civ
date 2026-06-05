namespace CivGame.Core;

public enum ResourceCategory { Strategic, Luxury, Bonus }

public abstract class Resource
{
    public string Id { get; }
    public string Name { get; }
    public ResourceCategory Category { get; }
    public TileYield BonusYield { get; }
    public string[] AllowedTerrains { get; }

    protected Resource(string id, string name, ResourceCategory category, TileYield bonusYield, string[] allowedTerrains)
    {
        Id = id;
        Name = name;
        Category = category;
        BonusYield = bonusYield;
        AllowedTerrains = allowedTerrains;
    }

    public bool CanSpawnOn(string terrainId)
    {
        foreach (var t in AllowedTerrains)
            if (t == terrainId) return true;
        return false;
    }
}
