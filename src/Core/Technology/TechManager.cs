using System;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.Core;

public class TechManager
{
    public List<Technology> AllTechnologies { get; } = new();
    public HashSet<string> ResearchedTechIds { get; } = new();
    
    public Technology? CurrentResearch { get; private set; }
    public int CurrentScienceProgress { get; private set; }
    public int LastTurnScienceGenerated { get; set; }

    public TechManager()
    {
        // Ancient Era
        AllTechnologies.Add(new Alphabet());
        AllTechnologies.Add(new BronzeWorking());
        AllTechnologies.Add(new CeremonialBurial());
        AllTechnologies.Add(new CodeOfLaws());
        AllTechnologies.Add(new Construction());
        AllTechnologies.Add(new Currency());
        AllTechnologies.Add(new HorsebackRiding());
        AllTechnologies.Add(new IronWorking());
        AllTechnologies.Add(new Literature());
        AllTechnologies.Add(new MapMaking());
        AllTechnologies.Add(new Masonry());
        AllTechnologies.Add(new Mathematics());
        AllTechnologies.Add(new Medicine());
        AllTechnologies.Add(new Monarchy());
        AllTechnologies.Add(new Mysticism());
        AllTechnologies.Add(new Philosophy());
        AllTechnologies.Add(new Polytheism());
        AllTechnologies.Add(new Pottery());
        AllTechnologies.Add(new WarriorCode());
        AllTechnologies.Add(new TheWheel());
        AllTechnologies.Add(new Writing());
        AllTechnologies.Add(new Republic());

        // Middle Ages
        AllTechnologies.Add(new Astronomy());
        AllTechnologies.Add(new Banking());
        AllTechnologies.Add(new Chemistry());
        AllTechnologies.Add(new Chivalry());
        AllTechnologies.Add(new Democracy());
        AllTechnologies.Add(new Economics());
        AllTechnologies.Add(new Education());
        AllTechnologies.Add(new Engineering());
        AllTechnologies.Add(new Feudalism());
        AllTechnologies.Add(new FreeArtistry());
        AllTechnologies.Add(new Gunpowder());
        AllTechnologies.Add(new Invention());
        AllTechnologies.Add(new Magnetism());
        AllTechnologies.Add(new Metallurgy());
        AllTechnologies.Add(new MilitaryTradition());
        AllTechnologies.Add(new Monotheism());
        AllTechnologies.Add(new MusicTheory());
        AllTechnologies.Add(new Navigation());
        AllTechnologies.Add(new Physics());
        AllTechnologies.Add(new PrintingPress());
        AllTechnologies.Add(new Theology());
        AllTechnologies.Add(new TheoryOfGravity());

        // Industrial Era
        AllTechnologies.Add(new AdvancedFlight());
        AllTechnologies.Add(new AmphibiousWar());
        AllTechnologies.Add(new AtomicTheory());
        AllTechnologies.Add(new Combustion());
        AllTechnologies.Add(new Communism());
        AllTechnologies.Add(new Electricity());
        AllTechnologies.Add(new Electronics());
        AllTechnologies.Add(new Espionage());
        AllTechnologies.Add(new Fascism());
        AllTechnologies.Add(new Flight());
        AllTechnologies.Add(new Industrialization());
        AllTechnologies.Add(new Ironclads());
        AllTechnologies.Add(new MassProduction());
        AllTechnologies.Add(new MotorizedTransportation());
        AllTechnologies.Add(new Nationalism());
        AllTechnologies.Add(new Plastics());
        AllTechnologies.Add(new Radio());
        AllTechnologies.Add(new Refining());
        AllTechnologies.Add(new ReplaceableParts());
        AllTechnologies.Add(new Sanitation());
        AllTechnologies.Add(new ScientificMethod());
        AllTechnologies.Add(new SteamPower());
        AllTechnologies.Add(new Steel());
        AllTechnologies.Add(new TheCorporation());

        // Modern Era
        AllTechnologies.Add(new Computers());
        AllTechnologies.Add(new Ecology());
        AllTechnologies.Add(new Fission());
        AllTechnologies.Add(new Genetics());
        AllTechnologies.Add(new IntegratedDefense());
        AllTechnologies.Add(new Miniaturization());
        AllTechnologies.Add(new NuclearPower());
        AllTechnologies.Add(new Robotics());
        AllTechnologies.Add(new Rocketry());
        AllTechnologies.Add(new Satellites());
        AllTechnologies.Add(new SmartWeapons());
        AllTechnologies.Add(new SpaceFlight());
        AllTechnologies.Add(new Stealth());
        AllTechnologies.Add(new Superconductor());
        AllTechnologies.Add(new SyntheticFibers());
        AllTechnologies.Add(new Recycling());
        AllTechnologies.Add(new TheLaser());
    }

    public void LoadResearchState(List<string> researchedIds, string? activeTechId, int progress, int lastTurnScience)
    {
        ResearchedTechIds.Clear();
        foreach (var id in researchedIds)
        {
            ResearchedTechIds.Add(id);
        }

        CurrentResearch = AllTechnologies.Find(t => t.Id == activeTechId);
        CurrentScienceProgress = progress;
        LastTurnScienceGenerated = lastTurnScience;
    }

    public bool IsResearched(string techId)
    {
        return ResearchedTechIds.Contains(techId);
    }

    /// <summary>
    /// Checks if all prerequisites for a technology have been researched.
    /// </summary>
    public bool CanResearch(Technology tech)
    {
        if (ResearchedTechIds.Contains(tech.Id)) return false;
        
        foreach (var prereqId in tech.PrerequisiteIds)
        {
            if (!ResearchedTechIds.Contains(prereqId)) return false;
        }
        return true;
    }

    /// <summary>
    /// Returns all technologies that the player can currently research 
    /// (prerequisites met but not yet researched).
    /// </summary>
    public List<Technology> GetAvailableTechnologies()
    {
        return AllTechnologies.FindAll(t => CanResearch(t));
    }

    /// <summary>
    /// Returns technologies for a given era.
    /// </summary>
    public List<Technology> GetTechnologiesByEra(TechEra era)
    {
        return AllTechnologies.FindAll(t => t.Era == era);
    }

    public void SetResearch(Technology? tech)
    {
        if (tech == null)
        {
            CurrentResearch = null;
            CurrentScienceProgress = 0;
            return;
        }

        if (ResearchedTechIds.Contains(tech.Id)) return;
        
        // Check prerequisites
        if (!CanResearch(tech))
        {
            Console.WriteLine($"[Research] Cannot research {tech.Name} — prerequisites not met.");
            return;
        }

        CurrentResearch = tech;
        CurrentScienceProgress = 0;
    }

    public void CycleResearch()
    {
        var available = GetAvailableTechnologies();

        if (available.Count == 0)
        {
            CurrentResearch = null;
            CurrentScienceProgress = 0;
            return;
        }

        if (CurrentResearch == null)
        {
            CurrentResearch = available[0];
            CurrentScienceProgress = 0;
        }
        else
        {
            int currentIndex = available.FindIndex(t => t.Id == CurrentResearch.Id);
            int nextIndex = currentIndex + 1;

            if (nextIndex >= available.Count)
            {
                CurrentResearch = null;
                CurrentScienceProgress = 0;
            }
            else
            {
                CurrentResearch = available[nextIndex];
                CurrentScienceProgress = 0;
            }
        }
    }

    public void AddScience(int amount, GameSimulation sim)
    {
        LastTurnScienceGenerated = amount;
        if (CurrentResearch == null) return;

        CurrentScienceProgress += amount;
        int cost = CurrentResearch.ScienceCost;

        if (CurrentScienceProgress >= cost)
        {
            var completedTech = CurrentResearch;
            ResearchedTechIds.Add(completedTech.Id);
            
            completedTech.OnUnlocked(sim);

            CurrentResearch = null;
            CurrentScienceProgress = 0;

            Console.WriteLine($"[Veda] Výskum technológie {completedTech.Name} bol úspešne dokončený!");
        }
    }

    public string GetResearchStatusString()
    {
        if (CurrentResearch == null)
        {
            return "Idle (No Research Chosen - Press 'T' to choose)";
        }

        return $"{CurrentResearch.Name} ({CurrentScienceProgress}/{CurrentResearch.ScienceCost} Science) (+{LastTurnScienceGenerated}/turn)";
    }

    /// <summary>
    /// Gets a smart recommendation for what to research next.
    /// Priorities: cheapest available in the earliest era not fully researched.
    /// </summary>
    public Technology? GetRecommendedResearch()
    {
        var available = GetAvailableTechnologies();
        if (available.Count == 0) return null;

        // Priority: cheapest tech in earliest era
        return available
            .OrderBy(t => t.Era)
            .ThenBy(t => t.ScienceCost)
            .FirstOrDefault();
    }
}
