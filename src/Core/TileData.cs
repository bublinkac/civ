namespace CivGame.Core;

public enum TerrainType
{
    Grassland,
    Plains,
    Ocean,
    Mountain,
    Desert
}

public readonly struct TileYield
{
    public int Food { get; }
    public int Production { get; }
    public int Commerce { get; }

    public TileYield(int food, int production, int commerce)
    {
        Food = food;
        Production = production;
        Commerce = commerce;
    }

    public override string ToString() => $"F:{Food} P:{Production} C:{Commerce}";
}

public class TileData
{
    public int X { get; }
    public int Y { get; }
    public Terrain Terrain { get; set; }
    public Resource? Resource { get; set; }

    private int _movementCost;
    public int MovementCost
    {
        get
        {
            if (HasRoad && Terrain.Id != "ocean")
            {
                return 1; // Roads make movement through Desert and Mountain cost only 1 MP
            }
            return _movementCost;
        }
        set => _movementCost = value;
    }
    public bool HasRoad { get; set; } = false;
    public string? OwnerCityId { get; set; }
    public TileImprovement? Improvement { get; set; }

    public TileYield BaseYield => Terrain.BaseYield;

    public TileYield TotalYield
    {
        get
        {
            var baseYield = BaseYield;
            int food = baseYield.Food;
            int production = baseYield.Production;
            int commerce = baseYield.Commerce;

            if (Improvement != null)
            {
                food += Improvement.BonusYield.Food;
                production += Improvement.BonusYield.Production;
                commerce += Improvement.BonusYield.Commerce;
            }

            if (Resource != null)
            {
                food += Resource.BonusYield.Food;
                production += Resource.BonusYield.Production;
                commerce += Resource.BonusYield.Commerce;
            }
            
            return new TileYield(food, production, commerce);
        }
    }

    public TileData(int x, int y, Terrain terrain, Resource? resource = null)
    {
        X = x;
        Y = y;
        Terrain = terrain;
        Resource = resource;
        MovementCost = terrain.MovementCost;
    }
}
