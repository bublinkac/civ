using System.Collections.Generic;

namespace CivGame.Core;

public static class ResourceRegistry
{
    public static readonly Dictionary<string, Resource> All = new();

    static ResourceRegistry()
    {
        Register(new BonusResource("cattle", "Cattle", new TileYield(2, 1, 0), new[]{"grassland", "plains"}));
        Register(new BonusResource("fish", "Fish", new TileYield(2, 0, 1), new[]{"coast", "sea", "marsh"}));
        Register(new BonusResource("game", "Game", new TileYield(2, 0, 0), new[]{"forest", "tundra", "marsh"}));
        Register(new BonusResource("gold", "Gold", new TileYield(0, 0, 4), new[]{"mountain", "hills"}));
        Register(new BonusResource("oasis", "Oasis", new TileYield(2, 0, 0), new[]{"desert"}));
        Register(new BonusResource("sugar", "Sugar", new TileYield(1, 0, 1), new[]{"hills", "plains"}));
        Register(new BonusResource("tobacco", "Tobacco", new TileYield(0, 0, 1), new[]{"hills", "grassland"}));
        Register(new BonusResource("tropical_fruit", "Tropical Fruit", new TileYield(1, 0, 1), new[]{"jungle"}));
        Register(new BonusResource("whales", "Whales", new TileYield(1, 1, 2), new[]{"sea"}));
        Register(new BonusResource("wheat", "Wheat", new TileYield(2, 0, 0), new[]{"grassland", "plains", "floodplains"}));

        Register(new StrategicResource("horses", "Horses", new TileYield(0, 1, 0), new[]{"hills", "grassland", "plains"}, "animal_husbandry"));
        Register(new StrategicResource("iron", "Iron", new TileYield(0, 1, 0), new[]{"hills", "mountain"}, "bronze_working"));
        Register(new StrategicResource("oil", "Oil", new TileYield(0, 1, 2), new[]{"tundra", "desert"}, "refining"));
        Register(new StrategicResource("coal", "Coal", new TileYield(0, 2, 1), new[]{"hills", "mountain", "jungle"}, "steam_power"));
        Register(new StrategicResource("rubber", "Rubber", new TileYield(0, 2, 0), new[]{"jungle", "forest"}, "replaceable_parts"));
        Register(new StrategicResource("saltpeter", "Saltpeter", new TileYield(0, 1, 0), new[]{"hills", "mountain", "desert"}, "gunpowder"));
        Register(new StrategicResource("aluminum", "Aluminum", new TileYield(0, 0, 2), new[]{"hills", "tundra"}, "rocketry"));
        Register(new StrategicResource("uranium", "Uranium", new TileYield(0, 2, 3), new[]{"mountain", "forest"}, "fission"));

        Register(new LuxuryResource("dyes", "Dyes", new TileYield(0, 0, 2), new[]{"jungle", "forest"}));
        Register(new LuxuryResource("furs", "Furs", new TileYield(0, 1, 1), new[]{"tundra", "forest"}));
        Register(new LuxuryResource("gems", "Gems", new TileYield(0, 0, 4), new[]{"mountain", "jungle"}));
        Register(new LuxuryResource("incense", "Incense", new TileYield(0, 0, 1), new[]{"hills", "desert"}));
        Register(new LuxuryResource("ivory", "Ivory", new TileYield(0, 0, 2), new[]{"forest", "plains"}));
        Register(new LuxuryResource("silk", "Silk", new TileYield(0, 0, 3), new[]{"forest", "jungle"}));
        Register(new LuxuryResource("spice", "Spice", new TileYield(0, 0, 2), new[]{"forest", "jungle"}));
        Register(new LuxuryResource("wine", "Wine", new TileYield(1, 0, 1), new[]{"grassland", "plains", "hills"}));
    }

    private static void Register(Resource resource) => All[resource.Id] = resource;

    public static Resource? Get(string id) => All.TryGetValue(id, out var r) ? r : null;
}
