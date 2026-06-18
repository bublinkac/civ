namespace CivGame.Core;

public enum TerrainType
{
    Coast,
    Desert,
    Floodplains,
    Forest,
    Grassland,
    Hills,
    Jungle,
    Marsh,
    Mountain,
    Ocean,
    Plains,
    Sea,
    Tundra
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

    private float _movementCost;
    public float MovementCost
    {
        get
        {
            if (HasRailroad && Terrain.Id != "ocean" && Terrain.Id != "sea" && Terrain.Id != "coast")
            {
                return 0.0f; // Railroads cost 0 MP (unlimited movement)
            }
            if (HasRoad && Terrain.Id != "ocean" && Terrain.Id != "sea" && Terrain.Id != "coast")
            {
                return 1.0f / 3.0f; // Roads reduce movement cost to 1/3 MP
            }
            return _movementCost;
        }
        set => _movementCost = value;
    }
    public bool HasRoad { get; set; } = false;
    public bool HasRailroad { get; set; } = false;
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

                // Civ3 Railroad bonus: +1 Food for irrigated tiles (Farm), +1 Production for Mined tiles
                if (HasRailroad)
                {
                    if (Improvement is Farm)
                    {
                        food += 1;
                    }
                    else if (Improvement is Mine)
                    {
                        production += 1;
                    }
                }
            }

            // Civ3 Road bonus: +1 Commerce on worked tiles (except water)
            if (HasRoad && Terrain.Id != "ocean" && Terrain.Id != "sea" && Terrain.Id != "coast")
            {
                commerce += 1;
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
