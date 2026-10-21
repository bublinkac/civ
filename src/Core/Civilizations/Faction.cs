namespace CivGame.Core;

public enum GameMode
{
    SinglePlayer,
    Hotseat,
    NetworkHost,
    NetworkClient
}

public enum Faction
{
    Player = 0,
    Barbarian = 1,
    AiRival = 2,
    AiRival2 = 3,
    AiRival3 = 4,
    AiRival4 = 5,
    AiRival5 = 6,
    AiRival6 = 7,
    AiRival7 = 8
}

public static class FactionExtensions
{
    public static bool IsPlayer(this Faction faction) => faction == Faction.Player;
    public static bool IsBarbarian(this Faction faction) => faction == Faction.Barbarian;
    public static bool IsAi(this Faction faction) => faction >= Faction.AiRival;

    public static string GetCapitalCityId(this Faction faction)
    {
        return faction switch
        {
            Faction.Player => "player_capital",
            Faction.AiRival => "ai_capital",
            Faction.AiRival2 => "ai_capital_2",
            Faction.AiRival3 => "ai_capital_3",
            Faction.AiRival4 => "ai_capital_4",
            Faction.AiRival5 => "ai_capital_5",
            Faction.AiRival6 => "ai_capital_6",
            Faction.AiRival7 => "ai_capital_7",
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
