namespace CivGame.Core;

public class LuxuryResource : Resource
{
    public int HappinessBonus { get; }

    public LuxuryResource(string id, string name, TileYield bonusYield, string[] allowedTerrains, int happinessBonus = 1) 
        : base(id, name, ResourceCategory.Luxury, bonusYield, allowedTerrains)
    {
        HappinessBonus = happinessBonus;
    }
}
