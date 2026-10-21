using System;
using System.Linq;

namespace CivGame.Core;

public class GameSetupOptions
{
    public int Width { get; set; } = 100;
    public int Height { get; set; } = 100;
    public int Seed { get; set; } = 1337;
    public string PlayerCivId { get; set; } = "rome";
    public string AiCivId { get; set; } = "babylon";
    public int NumberOfRivals { get; set; } = 3;
    public System.Collections.Generic.List<string> RivalCivIds { get; set; } = new();

    // Original Civilization III parameters
    public string WaterPercentage { get; set; } = "70%";   // "60%", "70%", "80%", "random"
    public string GeologicalAge { get; set; } = "4 Billion"; // "3 Billion" (Young/Mountainous), "4 Billion" (Normal), "5 Billion" (Old/Eroded), "random"
    public string Climate { get; set; } = "Normal";         // "Arid", "Normal", "Wet", "random"
    public string Temperature { get; set; } = "Temperate";   // "Warm", "Temperate", "Cold", "random"
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Regent;  // Default: balanced (classic Civ3)

    // Multiplayer parameters
    public GameMode Mode { get; set; } = GameMode.SinglePlayer;
    public string Player1Name { get; set; } = "Player 1";
    public string Player2Name { get; set; } = "Player 2";
    public string ServerIp { get; set; } = "127.0.0.1";
    public int ServerPort { get; set; } = 7777;

    /// <summary>
    /// Returns a resolved copy of options where all "random" selections are replaced with concrete values.
    /// </summary>
    public GameSetupOptions ResolveRandom()
    {
        var resolved = new GameSetupOptions
        {
            Width = Width,
            Height = Height,
            Seed = Seed,
            PlayerCivId = PlayerCivId,
            AiCivId = AiCivId,
            NumberOfRivals = NumberOfRivals,
            RivalCivIds = new System.Collections.Generic.List<string>(RivalCivIds),
            WaterPercentage = WaterPercentage,
            GeologicalAge = GeologicalAge,
            Climate = Climate,
            Temperature = Temperature,
            Difficulty = Difficulty,
            Mode = Mode,
            Player1Name = Player1Name,
            Player2Name = Player2Name,
            ServerIp = ServerIp,
            ServerPort = ServerPort
        };

        var rand = new Random(Seed);

        // Resolve Random Civilization if chosen
        if (resolved.PlayerCivId == "random")
        {
            var civs = CivilizationRegistry.All.Keys.ToList();
            resolved.PlayerCivId = civs[rand.Next(civs.Count)];
        }

        // Clamp number of rivals (1 to 7 rivals)
        if (resolved.NumberOfRivals < 1) resolved.NumberOfRivals = 1;
        if (resolved.NumberOfRivals > 7) resolved.NumberOfRivals = 7;

        // Resolve rival civilizations list
        var availableCivs = CivilizationRegistry.All.Keys.Where(id => id != resolved.PlayerCivId).ToList();
        resolved.RivalCivIds = new System.Collections.Generic.List<string>();

        // If a specific primary rival was selected and valid, add it first
        if (resolved.AiCivId != "random" && availableCivs.Contains(resolved.AiCivId))
        {
            resolved.RivalCivIds.Add(resolved.AiCivId);
            availableCivs.Remove(resolved.AiCivId);
        }

        while (resolved.RivalCivIds.Count < resolved.NumberOfRivals && availableCivs.Count > 0)
        {
            int idx = rand.Next(availableCivs.Count);
            resolved.RivalCivIds.Add(availableCivs[idx]);
            availableCivs.RemoveAt(idx);
        }

        if (resolved.RivalCivIds.Count > 0)
        {
            resolved.AiCivId = resolved.RivalCivIds[0];
        }

        // Resolve Random Water
        if (resolved.WaterPercentage.ToLower() == "random")
        {
            string[] waterChoices = { "60%", "70%", "80%" };
            resolved.WaterPercentage = waterChoices[rand.Next(waterChoices.Length)];
        }

        // Resolve Random Age
        if (resolved.GeologicalAge.ToLower() == "random")
        {
            string[] ageChoices = { "3 Billion", "4 Billion", "5 Billion" };
            resolved.GeologicalAge = ageChoices[rand.Next(ageChoices.Length)];
        }

        // Resolve Random Climate
        if (resolved.Climate.ToLower() == "random")
        {
            string[] climateChoices = { "Arid", "Normal", "Wet" };
            resolved.Climate = climateChoices[rand.Next(climateChoices.Length)];
        }

        // Resolve Random Temperature
        if (resolved.Temperature.ToLower() == "random")
        {
            string[] tempChoices = { "Warm", "Temperate", "Cold" };
            resolved.Temperature = tempChoices[rand.Next(tempChoices.Length)];
        }

        return resolved;
    }
}
