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
    public override TileYield BonusYield => new TileYield(0, 0, 0); // Roads only affect movement
    public override int ConstructionTurns => 2;

    public override bool CanBeBuiltOn(Terrain terrain)
    {
        return terrain.Id != "ocean";
    }
}
