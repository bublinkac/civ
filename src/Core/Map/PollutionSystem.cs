using System;
using System.Linq;

namespace CivGame.Core;

public static class PollutionSystem
{
    // Parameters that the user can easily tweak!
    public const int MinPopulationForPollution = 12;
    public const int MinProductionForPollution = 12;
    
    public const int FactoryFlatPollution = 2;
    public const int CoalPlantFlatPollution = 4;
    public const int IronWorksFlatPollution = 4;

    /// <summary>
    /// Calculates the population-based pollution points for a city.
    /// </summary>
    public static int GetPopulationPollution(City city)
    {
        return Math.Max(0, city.Population - MinPopulationForPollution);
    }

    /// <summary>
    /// Calculates the industrial/production-based pollution points for a city.
    /// </summary>
    public static int GetProductionPollution(City city, int baseProduction)
    {
        int industrialPoints = 0;

        // Flat points from buildings
        if (city.Buildings.Any(b => b.Id == "factory")) industrialPoints += FactoryFlatPollution;
        if (city.Buildings.Any(b => b.Id == "coal_plant")) industrialPoints += CoalPlantFlatPollution;
        if (city.Buildings.Any(b => b.Id == "iron_works")) industrialPoints += IronWorksFlatPollution;

        // Extra points for high production (1 point per 4 shields above threshold)
        if (baseProduction > MinProductionForPollution)
        {
            industrialPoints += (baseProduction - MinProductionForPollution) / 4;
        }

        return industrialPoints;
    }

    /// <summary>
    /// Calculates the total pollution score for a city and the percentage probability of a pollution event per turn.
    /// </summary>
    public static (int TotalPoints, float Probability) GetCityPollutionStats(City city, int baseProduction)
    {
        int popPoll = GetPopulationPollution(city);
        int prodPoll = GetProductionPollution(city, baseProduction);
        int total = popPoll + prodPoll;

        // Simple formula: 1.5% probability per point of pollution per turn
        // Cap at 60% per turn max per city.
        float probability = Math.Min(0.60f, total * 0.015f);

        return (total, probability);
    }
}
