namespace CivGame.Core;

public class BonusResource : Resource
{
    public BonusResource(string id, string name, TileYield bonusYield, string[] allowedTerrains) 
        : base(id, name, ResourceCategory.Bonus, bonusYield, allowedTerrains)
    {
    }
}
