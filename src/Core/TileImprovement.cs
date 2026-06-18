using System;

namespace CivGame.Core;

public abstract class TileImprovement
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract TileYield BonusYield { get; }
    public abstract int ConstructionTurns { get; }

    public abstract bool CanBeBuiltOn(Terrain terrain);
}

public class Farm : TileImprovement
{
    public override string Id => "farm";
    public override string Name => "Farm";
    public override TileYield BonusYield => new TileYield(1, 0, 0); // +1 Food
    public override int ConstructionTurns => 3;

    public override bool CanBeBuiltOn(Terrain terrain)
    {
        return terrain.Id == "grassland" || terrain.Id == "plains";
    }
}

public class Mine : TileImprovement
{
    public override string Id => "mine";
    public override string Name => "Mine";
    public override TileYield BonusYield => new TileYield(0, 2, 0); // +2 Production
    public override int ConstructionTurns => 3;

    public override bool CanBeBuiltOn(Terrain terrain)
    {
        return terrain.Id == "mountain" || terrain.Id == "desert" || terrain.Id == "plains";
    }
}

public class Plantation : TileImprovement
{
    public override string Id => "plantation";
    public override string Name => "Plantation";
    public override TileYield BonusYield => new TileYield(0, 0, 2); // +2 Commerce
    public override int ConstructionTurns => 3;

    public override bool CanBeBuiltOn(Terrain terrain)
    {
        return terrain.Id == "grassland" || terrain.Id == "plains";
    }
}

public class RoadBuild : TileImprovement
{
    public override string Id => "road";
    public override string Name => "Road";
    public override TileYield BonusYield => new TileYield(0, 0, 0); // Roads only affect movement and +1 Commerce is added via HasRoad in TileData
    public override int ConstructionTurns => 2;

    public override bool CanBeBuiltOn(Terrain terrain)
    {
        return terrain.Id != "ocean" && terrain.Id != "sea" && terrain.Id != "coast";
    }
}

public class RailroadBuild : TileImprovement
{
    public override string Id => "railroad";
    public override string Name => "Railroad";
    public override TileYield BonusYield => new TileYield(0, 0, 0); // Railroads affect movement and improve existing mines/farms
    public override int ConstructionTurns => 3;

    public override bool CanBeBuiltOn(Terrain terrain)
    {
        return terrain.Id != "ocean" && terrain.Id != "sea" && terrain.Id != "coast";
    }
}

public class Fortress : TileImprovement
{
    public override string Id => "fortress";
    public override string Name => "Fortress";
    public override TileYield BonusYield => new TileYield(0, 0, 0);
    public override int ConstructionTurns => 4;

    public override bool CanBeBuiltOn(Terrain terrain)
    {
        return terrain.Id != "ocean" && terrain.Id != "sea" && terrain.Id != "coast";
    }
}

public class Barricade : TileImprovement
{
    public override string Id => "barricade";
    public override string Name => "Barricade";
    public override TileYield BonusYield => new TileYield(0, 0, 0);
    public override int ConstructionTurns => 3;

    public override bool CanBeBuiltOn(Terrain terrain)
    {
        return terrain.Id != "ocean" && terrain.Id != "sea" && terrain.Id != "coast";
    }
}

public class Outpost : TileImprovement
{
    public override string Id => "outpost";
    public override string Name => "Outpost";
    public override TileYield BonusYield => new TileYield(0, 0, 0);
    public override int ConstructionTurns => 3;

    public override bool CanBeBuiltOn(Terrain terrain)
    {
        return terrain.Id != "ocean" && terrain.Id != "sea" && terrain.Id != "coast";
    }
}
