using System;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.Core;

/// <summary>
/// Implements the authentic Civilization III strategic resource depletion and sudden discovery mechanics.
/// Connected resources have a 1 in X chance to deplete each turn, reappearing elsewhere on the globe.
/// Worked land improvements have a slight chance to strike new resource deposits.
/// </summary>
public static class ResourceDepletionSystem
{
    public static void ProcessResourceDynamics(GameSimulation sim)
    {
        var random = new Random();
        var depletedEvents = new List<(StrategicResource Resource, int X, int Y, City? NearestCity, Faction? Faction)>();
        var discoveredEvents = new List<(StrategicResource Resource, int X, int Y, City? NearestCity, Faction Faction)>();

        int width = sim.Map.Width;
        int height = sim.Map.Height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tile = sim.Map.GetTile(x, y);
                if (tile == null) continue;

                // 1. DEPLETION CHECK
                if (tile.Resource is StrategicResource strat)
                {
                    // Only actively worked or connected resources can deplete
                    bool isConnected = tile.HasRoad || tile.HasRailroad || tile.Improvement != null;
                    if (isConnected)
                    {
                        int prob = strat.DisappearanceProbability > 0 ? strat.DisappearanceProbability : 800;
                        if (random.Next(prob) == 0)
                        {
                            var nearestCity = sim.Cities
                                .OrderBy(c => sim.Map.GetChebyshevDistance(c.X, c.Y, x, y))
                                .FirstOrDefault();

                            Faction? ownerFaction = nearestCity?.Faction;

                            // Deplete the resource
                            tile.Resource = null;
                            depletedEvents.Add((strat, x, y, nearestCity, ownerFaction));

                            // Spawn a replacement deposit elsewhere in the world to maintain global balance
                            RelocateResource(sim, strat, random);
                        }
                    }
                }
                // 2. DISCOVERY CHECK (Prospecting in worked improved tiles)
                else if (tile.Resource == null && tile.Improvement != null && (tile.Improvement is Mine || tile.Improvement is Farm))
                {
                    // 1 in 1500 chance per turn to strike a new vein of an unlocked strategic resource
                    if (random.Next(1500) == 0)
                    {
                        var nearestCity = sim.Cities
                            .Where(c => sim.Map.GetChebyshevDistance(c.X, c.Y, x, y) <= 3)
                            .OrderBy(c => sim.Map.GetChebyshevDistance(c.X, c.Y, x, y))
                            .FirstOrDefault();

                        if (nearestCity != null)
                        {
                            var faction = nearestCity.Faction;
                            var candidates = ResourceRegistry.All.Values
                                .OfType<StrategicResource>()
                                .Where(s => s.AllowedTerrains.Contains(tile.Terrain.Id) &&
                                            sim.IsTechResearched(faction, s.RequiredTechId))
                                .ToList();

                            if (candidates.Count > 0)
                            {
                                var discovered = candidates[random.Next(candidates.Count)];
                                tile.Resource = discovered;
                                discoveredEvents.Add((discovered, x, y, nearestCity, faction));
                            }
                        }
                    }
                }
            }
        }

        // Fire notifications and logging
        foreach (var dep in depletedEvents)
        {
            string cityName = dep.NearestCity != null ? dep.NearestCity.Name : $"({dep.X}, {dep.Y})";
            Console.WriteLine($"[RESOURCE EXHAUSTED] Sire, our sources of {dep.Resource.Name} near {cityName} have been completely exhausted!");
            sim.TriggerResourceDepleted(dep.Resource, dep.X, dep.Y, dep.NearestCity);

            sim.TurnEvents.AddEvent(new TurnEvent
            {
                Turn = sim.TurnNumber,
                Faction = dep.Faction ?? Faction.Player,
                Category = TurnEventCategory.Trade,
                Advisor = AdvisorType.Trade,
                Severity = TurnEventSeverity.Warning,
                Icon = "⚠️",
                Title = $"Ložisko vyčerpané: {dep.Resource.Name}",
                Message = $"Ložisko strategickej suroviny {dep.Resource.Name} pri meste {cityName} bolo vyčerpané a zaniklo!",
                TileX = dep.X,
                TileY = dep.Y,
                TargetCityId = dep.NearestCity?.Id,
                TargetCityName = dep.NearestCity?.Name,
                ActionLabel = "Zobraziť ložisko",
                ActionType = TurnEventActionType.ZoomToTile
            });
        }

        foreach (var disc in discoveredEvents)
        {
            string cityName = disc.NearestCity != null ? disc.NearestCity.Name : $"({disc.X}, {disc.Y})";
            Console.WriteLine($"[RESOURCE DISCOVERY] Eureka! Prospectors have discovered a rich deposit of {disc.Resource.Name} near {cityName}!");
            sim.TriggerResourceDiscovered(disc.Resource, disc.X, disc.Y, disc.NearestCity);

            sim.TurnEvents.AddEvent(new TurnEvent
            {
                Turn = sim.TurnNumber,
                Faction = disc.Faction,
                Category = TurnEventCategory.Trade,
                Advisor = AdvisorType.Trade,
                Severity = TurnEventSeverity.Success,
                Icon = "💎",
                Title = $"Objavená surovina: {disc.Resource.Name}",
                Message = $"Naši geológovia objavili bohaté ložisko suroviny {disc.Resource.Name} pri meste {cityName}! Napojte ho cestou.",
                TileX = disc.X,
                TileY = disc.Y,
                TargetCityId = disc.NearestCity?.Id,
                TargetCityName = disc.NearestCity?.Name,
                ActionLabel = "Zobraziť ložisko",
                ActionType = TurnEventActionType.ZoomToTile
            });
        }
    }

    private static void RelocateResource(GameSimulation sim, StrategicResource strat, Random random)
    {
        // Try up to 100 random tiles to find an unexploited valid terrain tile
        int attempts = 100;
        while (attempts-- > 0)
        {
            int rx = random.Next(sim.Map.Width);
            int ry = random.Next(sim.Map.Height);
            var tile = sim.Map.GetTile(rx, ry);

            if (tile != null && tile.Resource == null && !tile.IsOcean && strat.AllowedTerrains.Contains(tile.Terrain.Id))
            {
                tile.Resource = strat;
                Console.WriteLine($"[RESOURCE SHIFT] A replacement deposit of {strat.Name} has formed at ({rx}, {ry}).");
                return;
            }
        }
    }
}

