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

    // Original Civilization III parameters
    public string WaterPercentage { get; set; } = "70%";   // "60%", "70%", "80%", "random"
    public string GeologicalAge { get; set; } = "4 Billion"; // "3 Billion" (Young/Mountainous), "4 Billion" (Normal), "5 Billion" (Old/Eroded), "random"
    public string Climate { get; set; } = "Normal";         // "Arid", "Normal", "Wet", "random"
    public string Temperature { get; set; } = "Temperate";   // "Warm", "Temperate", "Cold", "random"
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Regent;  // Default: balanced (classic Civ3)

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
            WaterPercentage = WaterPercentage,
            GeologicalAge = GeologicalAge,
            Climate = Climate,
            Temperature = Temperature,
            Difficulty = Difficulty
        };

        var rand = new Random(Seed);

        // Resolve Random Civilization if chosen
        if (resolved.PlayerCivId == "random")
        {
            var civs = CivilizationRegistry.All.Keys.ToList();
            resolved.PlayerCivId = civs[rand.Next(civs.Count)];
        }
        if (resolved.AiCivId == "random" || resolved.AiCivId == resolved.PlayerCivId)
        {
            var civs = CivilizationRegistry.All.Keys.Where(id => id != resolved.PlayerCivId).ToList();
            resolved.AiCivId = civs[rand.Next(civs.Count)];
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
