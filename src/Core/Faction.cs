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
    VictoryDomination,
    VictoryScience,
    VictoryScore,
    DefeatDomination,
    DefeatScore
}
