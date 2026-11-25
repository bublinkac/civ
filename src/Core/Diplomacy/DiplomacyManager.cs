using System;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.Core.Diplomacy;

public enum DiplomaticAttitude
{
    Furious,
    Annoyed,
    Cautious,
    Polite,
    Gracious
}

/// <summary>
/// Manages diplomatic relationships, embassies, active trade deals, treaties, and AI diplomatic evaluations.
/// </summary>
public class DiplomacyManager
{
    public const int EmbassyCost = 50;

    private readonly GameSimulation _sim;

    public List<TradeDeal> ActiveDeals { get; } = new();

    // Embassies: true if Faction key has an embassy in Faction value
    public HashSet<(Faction From, Faction To)> Embassies { get; } = new();

    // Spies: true if Faction key has planted a spy in Faction value
    public HashSet<(Faction From, Faction To)> Spies { get; } = new();

    public event Action<TradeDeal>? OnTradeDealExpired;
    public event Action<TradeDeal, string>? OnAiProposedDeal;

    public void TriggerAiProposal(TradeDeal deal, string message)
    {
        OnAiProposedDeal?.Invoke(deal, message);
    }

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

    public bool CanEstablishEmbassy(Faction from, Faction to)
    {
        if (from == to || HasEmbassy(from, to)) return false;
        if (_sim.IsAtWar(from, to)) return false;
        if (from == Faction.Player && _sim.GetTreasury(from) < EmbassyCost) return false;
        return true;
    }

    public bool EstablishEmbassy(Faction from, Faction to)
    {
        if (from == to || HasEmbassy(from, to) || _sim.IsAtWar(from, to))
            return false;

        if (from == Faction.Player)
        {
            if (_sim.GetTreasury(from) < EmbassyCost)
                return false;

            _sim.SetTreasury(from, _sim.GetTreasury(from) - EmbassyCost);
        }

        Embassies.Add((from, to));

        if (from == Faction.Player)
        {
            var civ = _sim.GetCivilization(to);
            _sim.TurnEvents.AddEvent(new TurnEvent
            {
                Turn = _sim.TurnNumber,
                Faction = Faction.Player,
                Category = TurnEventCategory.Diplomacy,
                Severity = TurnEventSeverity.Success,
                Advisor = AdvisorType.Foreign,
                Icon = "🏛️",
                Title = $"Veľvyslanectvo v {civ.Name}",
                Message = $"Zriadili sme stále veľvyslanectvo u národa {civ.Name} ({civ.LeaderName}) za {EmbassyCost} zlata. Teraz môžeme uzatvárať pakty o prechode, obranné aliancie a sledovať ich vládu.",
                ActionType = TurnEventActionType.OpenAdvisor,
                ActionLabel = "Zahraničný poradca"
            });
        }

        return true;
    }

    public bool HasRightOfPassage(Faction a, Faction b) =>
        ActiveDeals.Any(d => d.RightOfPassage && ((d.FactionA == a && d.FactionB == b) || (d.FactionA == b && d.FactionB == a)));

    public bool HasMutualProtectionPact(Faction a, Faction b) =>
        ActiveDeals.Any(d => d.MutualProtectionPact && ((d.FactionA == a && d.FactionB == b) || (d.FactionA == b && d.FactionB == a)));

    public bool HasMilitaryAlliance(Faction a, Faction b) =>
        ActiveDeals.Any(d => d.MilitaryAlliance && ((d.FactionA == a && d.FactionB == b) || (d.FactionA == b && d.FactionB == a)));

    public List<TradeDeal> GetActiveDealsBetween(Faction a, Faction b) =>
        ActiveDeals.Where(d => (d.FactionA == a && d.FactionB == b) || (d.FactionA == b && d.FactionB == a)).ToList();

    public List<string> GetTradableResources(Faction faction)
    {
        var result = new HashSet<string>();
        foreach (var city in _sim.Cities.Where(c => c.Faction == faction))
        {
            if (_sim.CityTradeNetworks.TryGetValue(city.Id, out int netId) &&
                _sim.TradeNetworkResources.TryGetValue(netId, out var resSet))
            {
                foreach (var r in resSet)
                {
                    result.Add(r);
                }
            }
        }
        return result.OrderBy(r => r).ToList();
    }

    public List<Technology> GetTradableTechs(Faction from, Faction to)
    {
        var fromTechs = _sim.GetResearch(from).ResearchedTechIds;
        var toTechs = _sim.GetResearch(to).ResearchedTechIds;
        return _sim.Research.AllTechnologies
            .Where(t => fromTechs.Contains(t.Id) && !toTechs.Contains(t.Id))
            .OrderBy(t => t.Era)
            .ThenBy(t => t.ScienceCost)
            .ToList();
    }

    public DiplomaticAttitude GetAttitude(Faction observer, Faction target)
    {
        if (observer == target) return DiplomaticAttitude.Gracious;
        if (_sim.IsAtWar(observer, target)) return DiplomaticAttitude.Furious;

        int score = 0; // Neutral baseline -> Cautious

        if (HasEmbassy(observer, target) || HasEmbassy(target, observer)) score += 15;
        if (HasRightOfPassage(observer, target)) score += 15;
        if (HasMutualProtectionPact(observer, target)) score += 25;
        if (HasMilitaryAlliance(observer, target)) score += 25;

        int dealsCount = GetActiveDealsBetween(observer, target).Count;
        score += Math.Min(30, dealsCount * 10);

        if (observer == Faction.Player || target == Faction.Player)
        {
            score += Math.Min(15, _sim.PeaceTurns / 5);
        }

        if (score >= 45) return DiplomaticAttitude.Gracious;
        if (score >= 15) return DiplomaticAttitude.Polite;
        if (score >= -10) return DiplomaticAttitude.Cautious;
        if (score >= -35) return DiplomaticAttitude.Annoyed;
        return DiplomaticAttitude.Furious;
    }

    public static string GetAttitudeLabel(DiplomaticAttitude attitude) => attitude switch
    {
        DiplomaticAttitude.Gracious => "🤝 Gracious",
        DiplomaticAttitude.Polite   => "😊 Polite",
        DiplomaticAttitude.Cautious => "😐 Cautious",
        DiplomaticAttitude.Annoyed  => "😒 Annoyed",
        DiplomaticAttitude.Furious  => "😠 Furious",
        _ => "😐 Cautious"
    };

    /// <summary>
    /// Evaluates whether the target AI faction (FactionB) accepts the proposed TradeDeal from FactionA.
    /// </summary>
    public (bool Accepted, string Message) EvaluateDeal(TradeDeal deal)
    {
        if (deal.FactionA == deal.FactionB)
            return (false, "Cannot negotiate with yourself.");

        bool isAtWar = _sim.IsAtWar(deal.FactionA, deal.FactionB);

        if (isAtWar && !deal.PeaceTreaty)
            return (false, "We refuse to negotiate trade while our nations are at war! Offer a Peace Treaty first.");

        if ((deal.RightOfPassage || deal.MutualProtectionPact || deal.MilitaryAlliance) &&
            !HasEmbassy(deal.FactionA, deal.FactionB) && !HasEmbassy(deal.FactionB, deal.FactionA))
        {
            return (false, "We require an Embassy before signing formal pacts or alliances.");
        }

        // Check treasury affordability
        if (deal.LumpSumGoldA > _sim.GetTreasury(deal.FactionA))
            return (false, "You do not have enough gold in your treasury for this offer.");
        if (deal.LumpSumGoldB > _sim.GetTreasury(deal.FactionB))
            return (false, "Our treasury cannot afford that much gold right now.");

        int valueGivenToB = deal.LumpSumGoldA + (int)(deal.GoldPerTurnA * deal.TurnsRemaining * 0.85);
        int valueAskedFromB = deal.LumpSumGoldB + (int)(deal.GoldPerTurnB * deal.TurnsRemaining * 0.85);

        var researchB = _sim.GetResearch(deal.FactionB);
        var researchA = _sim.GetResearch(deal.FactionA);

        foreach (var techId in deal.TechsA)
        {
            if (!researchB.IsResearched(techId))
            {
                var tech = _sim.Research.AllTechnologies.Find(t => t.Id == techId);
                valueGivenToB += (tech?.ScienceCost ?? 20) * 3;
            }
        }

        foreach (var techId in deal.TechsB)
        {
            if (!researchA.IsResearched(techId))
            {
                var tech = _sim.Research.AllTechnologies.Find(t => t.Id == techId);
                valueAskedFromB += (tech?.ScienceCost ?? 20) * 3;
            }
        }

        foreach (var resId in deal.ResourcesA)
        {
            var res = ResourceRegistry.Get(resId);
            valueGivenToB += (res?.Category == ResourceCategory.Strategic) ? 120 : 80;
        }

        foreach (var resId in deal.ResourcesB)
        {
            var res = ResourceRegistry.Get(resId);
            valueAskedFromB += (res?.Category == ResourceCategory.Strategic) ? 120 : 80;
        }

        if (deal.WorldMapA) valueGivenToB += 40;
        if (deal.WorldMapB) valueAskedFromB += 40;

        // Mutual pacts carry a modest diplomacy weight
        if (deal.RightOfPassage)
        {
            valueGivenToB += 25;
            valueAskedFromB += 25;
        }
        if (deal.MutualProtectionPact)
        {
            valueGivenToB += 60;
            valueAskedFromB += 60;
        }
        if (deal.MilitaryAlliance)
        {
            valueGivenToB += 80;
            valueAskedFromB += 80;
        }

        // Peace treaty evaluation based on relative military power
        if (isAtWar && deal.PeaceTreaty)
        {
            int unitsA = _sim.Units.Count(u => u.Faction == deal.FactionA);
            int unitsB = _sim.Units.Count(u => u.Faction == deal.FactionB);
            int citiesA = _sim.Cities.Count(c => c.Faction == deal.FactionA);
            int citiesB = _sim.Cities.Count(c => c.Faction == deal.FactionB);

            int strengthDiff = (unitsA * 10 + citiesA * 25) - (unitsB * 10 + citiesB * 25);
            // If A is stronger, AI values peace more favorably for A
            valueGivenToB += Math.Clamp(strengthDiff, -100, 150);
        }

        bool hasAnything = deal.LumpSumGoldA > 0 || deal.LumpSumGoldB > 0 ||
                           deal.GoldPerTurnA > 0 || deal.GoldPerTurnB > 0 ||
                           deal.TechsA.Count > 0 || deal.TechsB.Count > 0 ||
                           deal.ResourcesA.Count > 0 || deal.ResourcesB.Count > 0 ||
                           deal.WorldMapA || deal.WorldMapB ||
                           deal.PeaceTreaty || deal.RightOfPassage || deal.MutualProtectionPact || deal.MilitaryAlliance;

        if (!hasAnything)
            return (false, "Put something on the table before proposing a deal.");

        if (valueGivenToB >= valueAskedFromB)
        {
            return (true, "We accept your generous proposal! May our nations prosper.");
        }
        else
        {
            int deficit = valueAskedFromB - valueGivenToB;
            return (false, $"This deal is unacceptable (short by ~{deficit} value). Offer more gold, tech, or resources.");
        }
    }

    public void ExecuteDeal(TradeDeal deal)
    {
        // 1. Process Instantaneous Items
        ProcessInstantTransfers(deal);

        // 2. If it's a Peace Treaty, end war
        if (deal.PeaceTreaty)
        {
            _sim.MakePeace(deal.FactionA, deal.FactionB);
        }

        // 3. Register Recurring Items
        if (deal.HasRecurringElements)
        {
            ActiveDeals.Add(deal);
        }

        // 4. Notify player
        if (deal.FactionA == Faction.Player || deal.FactionB == Faction.Player)
        {
            var other = deal.FactionA == Faction.Player ? deal.FactionB : deal.FactionA;
            var civ = _sim.GetCivilization(other);
            var summaryParts = new List<string>();
            if (deal.PeaceTreaty) summaryParts.Add("Mierová zmluva");
            if (deal.RightOfPassage) summaryParts.Add("Právo prechodu");
            if (deal.MutualProtectionPact) summaryParts.Add("Obranný pakt");
            if (deal.MilitaryAlliance) summaryParts.Add("Vojenská aliancia");
            if (deal.WorldMapA || deal.WorldMapB) summaryParts.Add("Výmena máp");
            if (deal.TechsA.Count > 0 || deal.TechsB.Count > 0) summaryParts.Add("Výmena technológií");
            if (deal.ResourcesA.Count > 0 || deal.ResourcesB.Count > 0) summaryParts.Add("Obchod so surovinami");
            if (deal.GoldPerTurnA > 0 || deal.GoldPerTurnB > 0) summaryParts.Add("Zlato za ťah");
            if (deal.LumpSumGoldA > 0 || deal.LumpSumGoldB > 0) summaryParts.Add("Jednorazové zlato");

            _sim.TurnEvents.AddEvent(new TurnEvent
            {
                Turn = _sim.TurnNumber,
                Faction = Faction.Player,
                Category = TurnEventCategory.Diplomacy,
                Severity = TurnEventSeverity.Success,
                Advisor = AdvisorType.Foreign,
                Icon = "🤝",
                Title = $"Dohoda s {civ.Name}",
                Message = $"Uzavreli sme diplomatickú dohodu s {civ.Name} ({civ.LeaderName}): {string.Join(", ", summaryParts)}.",
                ActionType = TurnEventActionType.OpenAdvisor,
                ActionLabel = "Zahraničný poradca"
            });
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
        if (attacker == defender) return;

        bool alreadyAtWar = _sim.IsAtWar(attacker, defender);
        _sim.DeclareWar(attacker, defender);

        if (!alreadyAtWar && (attacker == Faction.Player || defender == Faction.Player))
        {
            var other = attacker == Faction.Player ? defender : attacker;
            var civ = _sim.GetCivilization(other);
            _sim.TurnEvents.AddEvent(new TurnEvent
            {
                Turn = _sim.TurnNumber,
                Faction = Faction.Player,
                Category = TurnEventCategory.Military,
                Severity = TurnEventSeverity.Critical,
                Advisor = AdvisorType.Foreign,
                Icon = "⚔️",
                Title = $"Vojna s {civ.Name}!",
                Message = attacker == Faction.Player
                    ? $"Vyhlásili sme vojnu národu {civ.Name} ({civ.LeaderName}). Všetky obchodné dohody boli zrušené!"
                    : $"Národ {civ.Name} ({civ.LeaderName}) nám vyhlásil vojnu! Pripravte obranu miest.",
                ActionType = TurnEventActionType.OpenAdvisor,
                ActionLabel = "Zahraničný poradca"
            });
        }

        // Trigger Mutual Protection Pacts (MPP) for the defender
        if (!alreadyAtWar)
        {
            var mppAllies = ActiveDeals
                .Where(d => d.MutualProtectionPact && (d.FactionA == defender || d.FactionB == defender))
                .Select(d => d.FactionA == defender ? d.FactionB : d.FactionA)
                .Distinct()
                .ToList();

            foreach (var ally in mppAllies)
            {
                if (ally != attacker && ally != defender && !_sim.IsAtWar(ally, attacker))
                {
                    _sim.DeclareWar(ally, attacker);
                    if (ally == Faction.Player || attacker == Faction.Player)
                    {
                        var allyCiv = _sim.GetCivilization(ally);
                        var atkCiv = _sim.GetCivilization(attacker);
                        _sim.TurnEvents.AddEvent(new TurnEvent
                        {
                            Turn = _sim.TurnNumber,
                            Faction = Faction.Player,
                            Category = TurnEventCategory.Diplomacy,
                            Severity = TurnEventSeverity.Critical,
                            Advisor = AdvisorType.Foreign,
                            Icon = "🛡️",
                            Title = "Aktivácia obranného paktu!",
                            Message = $"{allyCiv.Name} vstúpil do vojny proti {atkCiv.Name} na základe Paktu o vzájomnej ochrane!",
                            ActionType = TurnEventActionType.OpenAdvisor,
                            ActionLabel = "Zahraničný poradca"
                        });
                    }
                }
            }
        }
    }

    public void CancelDeal(TradeDeal deal)
    {
        if (ActiveDeals.Remove(deal))
        {
            Console.WriteLine($"[DIPLOMACY] Trade deal between {deal.FactionA} and {deal.FactionB} has been cancelled.");
        }
    }

    public void ProcessTurnEnd()
    {
        foreach (var deal in ActiveDeals.ToList())
        {
            if (deal.GoldPerTurnA > 0) TransferGold(deal.FactionA, deal.FactionB, deal.GoldPerTurnA);
            if (deal.GoldPerTurnB > 0) TransferGold(deal.FactionB, deal.FactionA, deal.GoldPerTurnB);

            deal.TurnsRemaining--;
            if (deal.TurnsRemaining <= 0)
            {
                ActiveDeals.Remove(deal);
                OnTradeDealExpired?.Invoke(deal);
                Console.WriteLine($"[TRADE EXPIRED] The 20-turn trade agreement between {deal.FactionA} and {deal.FactionB} has concluded.");
            }
        }
    }

    private void TransferGold(Faction from, Faction to, int amount)
    {
        int fromCurrent = _sim.GetTreasury(from);
        _sim.SetTreasury(from, Math.Max(0, fromCurrent - amount));

        int toCurrent = _sim.GetTreasury(to);
        _sim.SetTreasury(to, toCurrent + amount);
    }

    private void GiveTech(Faction to, string techId)
    {
        var research = _sim.GetResearch(to);
        if (!research.ResearchedTechIds.Contains(techId))
        {
            research.ResearchedTechIds.Add(techId);
            if (to == Faction.Player)
            {
                var tech = _sim.Research.AllTechnologies.Find(t => t.Id == techId);
                tech?.OnUnlocked(_sim);
            }
        }
    }

    private void ShareMap(Faction from, Faction to)
    {
        if (to == Faction.Player)
        {
            int width = _sim.Map.Width;
            int height = _sim.Map.Height;

            void RevealRadius(int cx, int cy, int radius)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int nx = _sim.Map.NormalizeX(cx + dx);
                        int ny = cy + dy;
                        if (ny >= 0 && ny < height && _sim.VisibilityGrid[nx, ny] == FogState.Unexplored)
                        {
                            _sim.VisibilityGrid[nx, ny] = FogState.Shrouded;
                        }
                    }
                }
            }

            foreach (var city in _sim.Cities.Where(c => c.Faction == from))
            {
                RevealRadius(city.X, city.Y, 3);
            }

            foreach (var unit in _sim.Units.Where(u => u.Faction == from))
            {
                RevealRadius(unit.X, unit.Y, 2);
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
