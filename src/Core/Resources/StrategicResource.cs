namespace CivGame.Core;

public class StrategicResource : Resource
{
    public string RequiredTechId { get; }
    public int AppearanceRatio { get; }
    public int DisappearanceProbability { get; }

    public StrategicResource(string id, string name, TileYield bonusYield, string[] allowedTerrains, 
        string requiredTechId, int appearanceRatio = 160, int disappearanceProbability = 800) 
        : base(id, name, ResourceCategory.Strategic, bonusYield, allowedTerrains)
    {
        RequiredTechId = requiredTechId;
        AppearanceRatio = appearanceRatio;
        DisappearanceProbability = disappearanceProbability;
    }
}
