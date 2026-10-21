namespace CivGame.Core;

public enum Faction
{
    Player,
    Barbarian,
    AiRival
}

public static class FactionExtensions
{
    public static string GetCapitalCityId(this Faction faction)
    {
        return faction switch
        {
            Faction.Player => "player_capital",
            Faction.AiRival => "ai_capital",
            _ => string.Empty
        };
    }
}

public enum GameEndState
{
    None,
    // Military
    VictoryConquest,        // Eliminated all rivals
    DefeatConquest,         // Player was eliminated
    // Domination (2/3 land + 2/3 pop, rivals still alive)
    VictoryDomination,
    DefeatDomination,
    // Cultural
    VictoryCultural,
    DefeatCultural,
    // Diplomatic (UN vote)
    VictoryDiplomatic,
    DefeatDiplomatic,
    // Space Race
    VictorySpaceRace,
    DefeatSpaceRace,
    // Histograph / score at turn limit
    VictoryScore,
    DefeatScore
}
