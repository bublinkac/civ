using System;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.Core.Diplomacy;

/// <summary>
/// Manages diplomatic relationships, active trade deals, and AI diplomatic evaluations.
/// </summary>
public class DiplomacyManager
{
    private readonly GameSimulation _sim;
    
    public List<TradeDeal> ActiveDeals { get; } = new();

    // Embassies: true if Faction key has an embassy in Faction value
    public HashSet<(Faction From, Faction To)> Embassies { get; } = new();

    // Spies: true if Faction key has planted a spy in Faction value
    public HashSet<(Faction From, Faction To)> Spies { get; } = new();

    public DiplomacyManager(GameSimulation sim)
    {
        _sim = sim;
    }

    public bool HasEmbassy(Faction from, Faction to) => Embassies.Contains((from, to));
    public bool HasSpy(Faction from, Faction to) => Spies.Contains((from, to));
    
    public void PlantSpy(Faction from, Faction to)
    {
        Spies.Add((from, to));
    }

    public void EstablishEmbassy(Faction from, Faction to)
    {
        if (from == Faction.Player)
        {
            if (_sim.PlayerTreasury >= 50) // Flat cost for prototype
            {
                _sim.PlayerTreasury -= 50;
                Embassies.Add((from, to));
            }
        }
        else
        {
            Embassies.Add((from, to)); // AI gets it for free or handles it in AI brain
        }
    }

    public void ExecuteDeal(TradeDeal deal)
    {
        // 1. Process Instantaneous Items
        ProcessInstantTransfers(deal);

        // 2. If it's a Peace Treaty, end war
        if (deal.PeaceTreaty && (deal.FactionA == Faction.Player || deal.FactionB == Faction.Player))
        {
            _sim.IsAtWarWithAi = false;
            _sim.PeaceTurns = 0;
            _sim.WarWearinessPoints = Math.Max(0, _sim.WarWearinessPoints / 2); // Halve weariness instantly
        }

        // 3. Register Recurring Items
        if (deal.HasRecurringElements)
        {
            ActiveDeals.Add(deal);
        }
    }

    private void ProcessInstantTransfers(TradeDeal deal)
    {
        // Gold
        if (deal.LumpSumGoldA > 0) TransferGold(deal.FactionA, deal.FactionB, deal.LumpSumGoldA);
        if (deal.LumpSumGoldB > 0) TransferGold(deal.FactionB, deal.FactionA, deal.LumpSumGoldB);

        // Techs from A to B
        foreach (var techId in deal.TechsA) GiveTech(deal.FactionB, techId);
        
        // Techs from B to A
        foreach (var techId in deal.TechsB) GiveTech(deal.FactionA, techId);
        
        // Maps (If A gives World Map to B, B discovers all tiles A knows)
        if (deal.WorldMapA) ShareMap(deal.FactionA, deal.FactionB);
        if (deal.WorldMapB) ShareMap(deal.FactionB, deal.FactionA);
    }

    public void CancelAllDealsBetween(Faction a, Faction b)
    {
        ActiveDeals.RemoveAll(d => 
            (d.FactionA == a && d.FactionB == b) || 
            (d.FactionA == b && d.FactionB == a));
    }

    public void DeclareWar(Faction attacker, Faction defender)
    {
        if ((attacker == Faction.Player && defender == Faction.AiRival) || 
            (attacker == Faction.AiRival && defender == Faction.Player))
        {
            _sim.IsAtWarWithAi = true;
            _sim.WarStartTurn = _sim.TurnNumber;
            _sim.IsDefensiveWar = (defender == Faction.Player);
            
            // Cancelling all active trade deals due to war
            CancelAllDealsBetween(attacker, defender);
        }
    }

    public void ProcessTurnEnd()
    {
        foreach(var deal in ActiveDeals.ToList()) // ToList to avoid modification during enumeration
        {
            // Transfer GPT
            if (deal.GoldPerTurnA > 0) TransferGold(deal.FactionA, deal.FactionB, deal.GoldPerTurnA);
            if (deal.GoldPerTurnB > 0) TransferGold(deal.FactionB, deal.FactionA, deal.GoldPerTurnB);

            // Treaties & right of passage logic would just be checked globally using ActiveDeals, no per-turn action needed.

            deal.TurnsRemaining--;
            if (deal.TurnsRemaining <= 0)
            {
                ActiveDeals.Remove(deal);
                // Optionally notify players that a deal has expired
            }
        }
    }

    private void TransferGold(Faction from, Faction to, int amount)
    {
        if (from == Faction.Player) _sim.PlayerTreasury -= amount;
        // If AI had a real treasury, we'd deduct it there, but for now prototype

        if (to == Faction.Player) _sim.PlayerTreasury += amount;
    }

    private void GiveTech(Faction to, string techId)
    {
        if (to == Faction.Player)
        {
            var tech = _sim.Research.AllTechnologies.Find(t => t.Id == techId);
            if (tech != null && !_sim.Research.IsResearched(techId))
            {
                _sim.Research.ResearchedTechIds.Add(techId);
                tech.OnUnlocked(_sim);
            }
        }
        else if (to == Faction.AiRival)
        {
            _sim.AiResearchedTechs.Add(techId);
        }
    }

    private void ShareMap(Faction from, Faction to)
    {
        // Simple map sharing: for every tile 'from' has discovered, mark it for 'to'
        int width = _sim.VisibilityGrid.GetLength(0);
        int height = _sim.VisibilityGrid.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var state = _sim.VisibilityGrid[x, y];
                if (from == Faction.Player && (state == FogState.Shrouded || state == FogState.Visible))
                {
                    // If AI had a fog map, we would mark it for AI. 
                }
                else if (from == Faction.AiRival)
                {
                    // Assume AI knows everything or share AI's fog map with Player.
                    // For now, if AI shares with player, let's just reveal tiles around AI cities
                    if (to == Faction.Player)
                    {
                        foreach(var city in _sim.Cities.Where(c => c.Faction == Faction.AiRival))
                        {
                             _sim.VisibilityGrid[city.X, city.Y] = FogState.Shrouded;
                             // Just a simplistic prototype, ideally AI has its own FogState[,]
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if a faction has access to a specific resource via active trade deals.
    /// </summary>
    public bool HasTradedResource(Faction faction, string resourceId)
    {
        foreach (var deal in ActiveDeals)
        {
            if (deal.FactionA == faction && deal.ResourcesB.Contains(resourceId)) return true;
            if (deal.FactionB == faction && deal.ResourcesA.Contains(resourceId)) return true;
        }
        return false;
    }
}
