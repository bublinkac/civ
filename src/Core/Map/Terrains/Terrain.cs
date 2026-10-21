using Godot;

namespace CivGame.Core;

public abstract class Terrain
{
    public string Id { get; }
    public string Name { get; }
    public TileYield BaseYield { get; }
    public int MovementCost { get; }
    public int DefenseBonusPercent { get; }
    public Color Color { get; }

    protected Terrain(string id, string name, TileYield baseYield, int movementCost, int defenseBonusPercent, Color color)
    {
        Id = id;
        Name = name;
        BaseYield = baseYield;
        MovementCost = movementCost;
        DefenseBonusPercent = defenseBonusPercent;
        Color = color;
    }
}
