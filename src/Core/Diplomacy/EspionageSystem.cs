using System;
using System.Linq;

namespace CivGame.Core;

public enum EspionageMission
{
    EstablishEmbassy,
    InvestigateCity,
    PlantSpy,
    StealTechnology,
    SabotageProduction,
    StealWorldMap
}

public class EspionageSystem
{
    private readonly GameSimulation _sim;

    public EspionageSystem(GameSimulation sim)
    {
        _sim = sim;
    }

    public int GetMissionCost(EspionageMission mission, Faction targetFaction, City? targetCity = null)
    {
        return mission switch
        {
            EspionageMission.EstablishEmbassy => 50,
            EspionageMission.PlantSpy => 100,
            EspionageMission.InvestigateCity => 30 + (targetCity?.Population ?? 1) * 5,
            EspionageMission.StealWorldMap => 150,
            EspionageMission.StealTechnology => 500, // Civ3 tech stealing is very expensive
            EspionageMission.SabotageProduction => targetCity != null ? (targetCity.CurrentProductionProgress * 2) + 50 : 0,
            _ => 0
        };
    }

    public bool ExecuteMission(Faction playerFaction, Faction targetFaction, EspionageMission mission, City? targetCity = null)
    {
        int cost = GetMissionCost(mission, targetFaction, targetCity);

        // Deduct gold
        if (playerFaction == Faction.Player)
        {
            if (_sim.PlayerTreasury < cost)
            {
                Console.WriteLine($"[Espionage] Not enough gold to perform {mission}. Need {cost}.");
                return false;
            }
            _sim.PlayerTreasury -= cost;
        }

        Random rng = new Random();
        int roll = rng.Next(1, 101);

        switch (mission)
        {
            case EspionageMission.EstablishEmbassy:
                if (_sim.Diplomacy.HasEmbassy(playerFaction, targetFaction))
                {
                    Console.WriteLine($"[Espionage] Embassy already exists with {targetFaction}!");
                    if (playerFaction == Faction.Player) _sim.PlayerTreasury += cost; // refund
                    return false;
                }
                
                // 100% success
                _sim.Diplomacy.EstablishEmbassy(playerFaction, targetFaction);
                Console.WriteLine($"[Espionage] Embassy established with {targetFaction}!");
                return true;

            case EspionageMission.PlantSpy:
                if (_sim.Diplomacy.HasSpy(playerFaction, targetFaction))
                {
                    Console.WriteLine($"[Espionage] Spy is already planted in {targetFaction}!");
                    if (playerFaction == Faction.Player) _sim.PlayerTreasury += cost; // refund
                    return false;
                }

                // Requires Intelligence Agency
                bool hasAgency = _sim.CompletedSmallWondersByFaction.ContainsKey(playerFaction) &&
                                 _sim.CompletedSmallWondersByFaction[playerFaction].Contains("intelligence_agency");
                if (!hasAgency)
                {
                    Console.WriteLine("[Espionage] You need the Intelligence Agency to plant a spy!");
                    if (playerFaction == Faction.Player) _sim.PlayerTreasury += cost; // refund
                    return false;
                }

                if (roll > 30) // 70% success
                {
                    _sim.Diplomacy.PlantSpy(playerFaction, targetFaction);
                    Console.WriteLine($"[Espionage] Successfully planted a spy in {targetFaction}'s capital!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[Espionage] Our spy was caught and executed by {targetFaction}!");
                    return false;
                }

            case EspionageMission.StealTechnology:
                if (roll > 50) // 50% chance
                {
                    // Pick a random tech the target has that the player doesn't
                    var targetTechs = _sim.AiResearchedTechs; // Simplified for AI target
                    var playerTechs = _sim.Research.ResearchedTechIds;
                    
                    var stealable = targetTechs.Where(t => !playerTechs.Contains(t)).ToList();
                    if (stealable.Count > 0)
                    {
                        var stolenTech = stealable[rng.Next(stealable.Count)];
                        _sim.Research.ResearchedTechIds.Add(stolenTech);
                        var t = _sim.Research.AllTechnologies.FirstOrDefault(x => x.Id == stolenTech);
                        if (t != null) t.OnUnlocked(_sim);
                        
                        Console.WriteLine($"[Espionage] Successfully stole {stolenTech} from {targetFaction}!");
                        return true;
                    }
                    Console.WriteLine($"[Espionage] {targetFaction} has no technologies we don't already know.");
                    return false;
                }
                else
                {
                    Console.WriteLine($"[Espionage] Our agent was caught trying to steal technology!");
                    CheckDiplomaticIncident(targetFaction);
                    return false;
                }

            case EspionageMission.SabotageProduction:
                if (targetCity == null) return false;
                
                if (roll > 40) // 60% chance
                {
                    targetCity.CurrentProductionProgress = 0;
                    Console.WriteLine($"[Espionage] Successfully sabotaged production in {targetCity.Name}!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[Espionage] Saboteurs were caught in {targetCity.Name}!");
                    CheckDiplomaticIncident(targetFaction);
                    return false;
                }

            case EspionageMission.InvestigateCity:
                if (targetCity == null) return false;
                // Always succeeds if you pay
                Console.WriteLine($"[Espionage] Investigated {targetCity.Name}. Population: {targetCity.Population}, Project: {targetCity.CurrentProject}.");
                return true;

            default:
                return false;
        }
    }

    private void CheckDiplomaticIncident(Faction targetFaction)
    {
        Random rng = new Random();
        if (rng.Next(1, 100) < 30) // 30% chance for war if caught
        {
            Console.WriteLine($"[Diplomacy] INCIDENT! {targetFaction} is furious about our espionage and has declared WAR!");
            _sim.IsAtWarWithAi = true;
            _sim.WarStartTurn = _sim.TurnNumber;
        }
    }
}
