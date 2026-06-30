using System;
using System.Linq;

namespace CivGame.Core;

public enum ProductionProject
    {
        None,
        Explorer,
        Settler,
        Worker,
        Warrior,
        Archer,
        Granary,
        Monument,
        Walls,
        Barracks,
        Temple,
        Library,
        Courthouse,
        Marketplace,
        Aqueduct,
        Colosseum,
        Harbor,
        Bank,
        Cathedral,
        University,
        Hospital,
        Factory,
        CoalPlant,
        HydroPlant,
        SolarPlant,
        NuclearPlant,
        ManufacturingPlant,
        Airport,
        CivilDefense,
        PoliceStation,
        StockExchange,
        RecyclingCenter,
        ResearchLab,
        SAMMissileBattery,
        OffshorePlatform,
        MassTransitSystem,
        CommercialDock,
        CoastalFortress,
        SSCockpit,
        SSDockingBay,
        SSEngine,
        SSExteriorCasing,
        SSFuelCells,
        SSLifeSupportSystem,
        SSPlanetaryPartyLounge,
        SSStasisChamber,
        SSStorageSupply,
        SSThrusters,
        Palace,
        // Wonders
        Pyramids,
        HangingGardens,
        Colossus,
        GreatWall,
        StatueOfZeus,
        Oracle,
        KnightsHall,
        SovereignBath,
        LeonardoWorkshop,
        ShakespearesTheatre,
        SunTzusWarAcademy,
        CureForCancer,
        SistineChapel,
        TajMahal,
        Astrolabe,
        Hermitage,
        IronWorks,
        SmithsMansion,
        TrainStation,
        UnitedNations,
        ApolloProgram,
        ManhattanProject,
        Internet,
        LongevityVaccine,
        MarsColony,
        WorldBank,
        SpaceStation,
        // Small Wonders
        HeroicEpic,
        MilitaryAcademy,
        Pentagon,
        ForbiddenPalace,
        WallStreet,
        IntelligenceAgency,
        BattlefieldMedicine,
        SDIDefense
    }

public enum CityType
{
    Town,
    City,
    Metropolis
}

public class City
{
    public string Id { get; }
    public string Name { get; }
    public int X { get; }
    public int Y { get; }
    public int VisionRange { get; } = 2;
    public int FoundedYear { get; }

    public CityType Type { get; private set; } = CityType.Town;
    public Faction Faction { get; set; }
    public string? CivilizationId { get; set; }
    public Civilization? Civilization => CivilizationId != null ? CivilizationRegistry.Get(CivilizationId) : null;
    
    // Capital & Corruption properties
    public bool IsCapital { get; set; } = false;
    public int LastTurnWaste { get; set; } = 0;
    public int LastTurnCorruption { get; set; } = 0;

    // Culture and Happiness properties
    public int AccumulatedCulture { get; set; } = 0;
    /// <summary>Culture generated per turn (base 1 + building/wonder bonuses).</summary>
    public int CultureOutput { get; set; } = 1;
    public bool IsInDisorder { get; set; } = false;
    /// <summary>Consecutive turns this city has been in civil disorder.</summary>
    public int DisorderTurns { get; set; } = 0;
    public int HappyCitizens { get; set; } = 0;
    public int ContentCitizens { get; set; } = 1;
    public int UnhappyCitizens { get; set; } = 0;


    // Resource storage (accumulated yields)
    public int StoredFood { get; set; }
    public int StoredProduction { get; set; }
    public int StoredCommerce { get; set; }

    // Population & Growth
    public int Population { get; set; } = 1;
    public int FoodNeededForGrowth => Population < 2 ? 20 : 20 + (Population - 2) * 10;
    public int LastTurnNetFood { get; set; }

    // Production queue
    public ProductionProject CurrentProject { get; set; } = ProductionProject.None;
    public int CurrentProductionProgress { get; set; }
    public System.Collections.Generic.List<ProductionProject> ProductionQueue { get; } = new();

    // Completed Buildings
    public System.Collections.Generic.List<Building> Buildings { get; } = new();

    // Map tiles this city is actively working
    public System.Collections.Generic.HashSet<(int X, int Y)> WorkedTiles { get; } = new();

    public int GetDefenseBonus()
    {
        int buildingDefense = Buildings.Sum(b => b.DefenseBonus);
        int sizeBonus = Type switch
        {
            CityType.Town => 0,
            CityType.City => 50,
            CityType.Metropolis => 100,
            _ => 0
        };
        return buildingDefense + sizeBonus;
    }

    public void UpdateCityType()
    {
        Type = Population switch
        {
            >= 13 => CityType.Metropolis,
            >= 7 => CityType.City,
            _ => CityType.Town
        };
    }

    public int GetCulturePerTurn()
    {
        int culturePerTurn = 0;
        foreach (var b in Buildings)
        {
            if (b.Id == "temple") culturePerTurn += 2;
            else if (b.Id == "library") culturePerTurn += 3;
            else if (b.Id == "monument") culturePerTurn += 1;
            else if (b.Id == "cathedral") culturePerTurn += 4;
            else if (b.Id == "university") culturePerTurn += 4;
            else if (b.Id == "research_lab") culturePerTurn += 2;
            else if (b is Wonder wonder)
            {
                culturePerTurn += wonder.IsNationalWonder ? 1 : 2;
            }
        }
        return culturePerTurn;
    }

    /// <summary>
    /// Civ3-authentic citizen mood calculation.
    /// Order of effects:
    ///   1. Base population distribution (content vs unhappy based on difficulty)
    ///   2. War weariness (Republic/Democracy only)
    ///   3. Military police (government-dependent limit)
    ///   4. Buildings (Temple, Colosseum, Cathedral produce content faces)
    ///   5. Luxuries (connected luxury resources produce happy faces; Marketplace amplifies)
    /// 
    /// Content faces: make unhappy -> content
    /// Happy faces: make content -> happy (or unhappy -> content if no content available)
    /// </summary>
    public void UpdateCitizenMood(GameSimulation sim, DifficultySettings? difficulty = null)
    {
        int pop = Population;
        if (pop <= 0) return;

        // ═══════════════════════════════════════
        // STEP 0: Base distribution
        // ═══════════════════════════════════════
        int baseContent = difficulty?.BaseContentCitizens ?? 2;
        int baseUnhappy = difficulty?.BaseUnhappyCitizens ?? 0;

        int happy = 0;
        int content = Math.Min(pop, baseContent);
        int unhappy = Math.Max(0, pop - baseContent) + baseUnhappy;
        if (content + unhappy > pop)
            unhappy = Math.Max(0, pop - content);

        // ═══════════════════════════════════════
        // STEP 1: War Weariness (adds unhappy citizens)
        // Only affects Republic and Democracy
        // ═══════════════════════════════════════
        int warWearinessUnhappy = sim.GetWarWearinessUnhappiness(this);
        if (warWearinessUnhappy > 0)
        {
            // War weariness converts content -> unhappy, then happy -> unhappy
            int toConvert = Math.Min(content, warWearinessUnhappy);
            content -= toConvert;
            unhappy += toConvert;
            warWearinessUnhappy -= toConvert;

            if (warWearinessUnhappy > 0)
            {
                int fromHappy = Math.Min(happy, warWearinessUnhappy);
                happy -= fromHappy;
                unhappy += fromHappy;
            }
        }

        // ═══════════════════════════════════════
        // STEP 2: Military Police (content faces)
        // Government determines max MP units effective
        // ═══════════════════════════════════════
        var govType = Faction == Faction.Player ? sim.PlayerGovernment : sim.AiGovernment;
        var gov = Government.Get(govType);
        int maxMP = gov.MaxMilitaryPolice;
        if (maxMP > 0)
        {
            int mpCount = sim.Units.Count(u => u.X == X && u.Y == Y && u.Faction == Faction
                && (u.Type == UnitType.Warrior || u.Type == UnitType.Archer));
            int mpEffect = Math.Min(maxMP, mpCount);
            if (unhappy > 0 && mpEffect > 0)
            {
                int toConvert = Math.Min(unhappy, mpEffect);
                unhappy -= toConvert;
                content += toConvert;
            }
        }

        // ═══════════════════════════════════════
        // STEP 3: Buildings (produce content faces)
        // Temple: 1 (Religious trait: 2)
        // Colosseum: 2
        // Cathedral: 3 (Religious trait: 4)
        // ═══════════════════════════════════════
        int contentFaces = 0;
        int templeBonus = HasTrait(CivTrait.Religious) ? 2 : 1;
        int cathedralBonus = HasTrait(CivTrait.Religious) ? 4 : 3;

        if (Buildings.Any(b => b.Id == "temple")) contentFaces += templeBonus;
        if (Buildings.Any(b => b.Id == "colosseum")) contentFaces += 2;
        if (Buildings.Any(b => b.Id == "cathedral")) contentFaces += cathedralBonus;

        // Content faces: convert unhappy -> content
        if (unhappy > 0 && contentFaces > 0)
        {
            int toConvert = Math.Min(unhappy, contentFaces);
            unhappy -= toConvert;
            content += toConvert;
            contentFaces -= toConvert;
        }
        // Extra content faces make content -> happy
        if (content > 0 && contentFaces > 0)
        {
            int toConvert = Math.Min(content, contentFaces);
            content -= toConvert;
            happy += toConvert;
        }

        // ═══════════════════════════════════════
        // STEP 4: Luxuries (produce happy faces)
        // Civ3 mechanic: Each connected luxury = 1 happy face.
        // With Marketplace: 1-2 luxuries = 1 each, 3-4 = 2 each, 5-6 = 3 each, 7-8 = 4 each
        // ═══════════════════════════════════════
        var connectedLuxuries = sim.GetCityConnectedLuxuries(this);
        int luxCount = connectedLuxuries.Count;
        int happyFaces = 0;

        if (luxCount > 0)
        {
            bool hasMarketplace = Buildings.Any(b => b.Id == "marketplace");
            if (hasMarketplace)
            {
                // Marketplace bonus: luxuries 1-2 give 1 face each, 3-4 give 2, 5-6 give 3, 7-8 give 4
                for (int i = 1; i <= luxCount; i++)
                {
                    int tier = (i + 1) / 2; // 1,1 -> 1; 2,2 -> 1; 3,3 -> 2; ...
                    happyFaces += tier;
                }
            }
            else
            {
                // Without marketplace: each luxury = 1 happy face
                happyFaces = luxCount;
            }
        }

        // Happy faces: convert unhappy -> content first, then content -> happy
        if (unhappy > 0 && happyFaces > 0)
        {
            int toConvert = Math.Min(unhappy, happyFaces);
            unhappy -= toConvert;
            content += toConvert;
            happyFaces -= toConvert;
        }
        if (content > 0 && happyFaces > 0)
        {
            int toConvert = Math.Min(content, happyFaces);
            content -= toConvert;
            happy += toConvert;
        }

        // ═══════════════════════════════════════
        // FINAL: Assign results
        // ═══════════════════════════════════════
        HappyCitizens = Math.Max(0, happy);
        ContentCitizens = Math.Max(0, content);
        UnhappyCitizens = Math.Max(0, unhappy);

        // Civil Disorder: unhappy > happy and population > 1
        IsInDisorder = UnhappyCitizens > HappyCitizens && pop > 1;
    }

    public City(string id, string name, int x, int y, int foundedYear, Faction faction = Faction.Player, string? civilizationId = null)
    {
        Id = id;
        Name = name;
        X = x;
        Y = y;
        FoundedYear = foundedYear;
        Faction = faction;
        CivilizationId = civilizationId;
    }

    public bool HasBuilding<T>() where T : Building
    {
        return Buildings.Exists(b => b is T);
    }

    public bool HasTrait(CivTrait trait)
    {
        return Civilization != null && (Civilization.Trait1 == trait || Civilization.Trait2 == trait);
    }

    public bool IsCoastal(GameMap map)
    {
        int[] dxs = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dys = { -1, -1, -1, 0, 0, 1, 1, 1 };
        for (int i = 0; i < 8; i++)
        {
            int tx = X + dxs[i];
            int ty = Y + dys[i];
            if (map.IsInBounds(tx, ty))
            {
                var tile = map.GetTile(tx, ty);
                if (tile != null && (tile.Terrain.Id == "coast" || tile.Terrain.Id == "sea" || tile.Terrain.Id == "ocean"))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public int GetTotalMaintenance() => Buildings.Sum(b => b.MaintenanceCost);

    public T? GetBuilding<T>() where T : Building
    {
        return Buildings.Find(b => b is T) as T;
    }

    public int GetProjectCost(ProductionProject project, DifficultySettings? difficulty = null)
    {
        int baseCost = project switch
        {
            ProductionProject.Explorer => 10,
            ProductionProject.Settler => 20,
            ProductionProject.Worker => 15,
            ProductionProject.Warrior => 15,
            ProductionProject.Archer => 20,
            _ => GetBuildingCostFromRegistry(project)
        };

        if (Faction == Faction.AiRival && difficulty != null)
        {
            bool isUnit = project == ProductionProject.Explorer ||
                          project == ProductionProject.Settler ||
                          project == ProductionProject.Worker ||
                          project == ProductionProject.Warrior ||
                          project == ProductionProject.Archer;
            if (isUnit)
            {
                baseCost = (int)Math.Max(1, Math.Round(baseCost * difficulty.AiProductionCostMultiplier));
            }
        }

        // Apply Civ3 trait discounts (50% reduction for specific buildings/units)
        if (HasTrait(CivTrait.Scientific))
        {
            if (project == ProductionProject.Library || project == ProductionProject.University || project == ProductionProject.ResearchLab)
            {
                return baseCost / 2;
            }
        }
        if (HasTrait(CivTrait.Agricultural))
        {
            if (project == ProductionProject.Granary)
            {
                return baseCost / 2;
            }
        }
        if (HasTrait(CivTrait.Militaristic))
        {
            if (project == ProductionProject.Barracks || project == ProductionProject.CoastalFortress)
            {
                return baseCost / 2;
            }
        }
        if (HasTrait(CivTrait.Religious))
        {
            if (project == ProductionProject.Temple || project == ProductionProject.Cathedral)
            {
                return baseCost / 2;
            }
        }
        if (HasTrait(CivTrait.Commercial))
        {
            if (project == ProductionProject.Marketplace || project == ProductionProject.Bank || project == ProductionProject.StockExchange)
            {
                return baseCost / 2;
            }
        }
        if (HasTrait(CivTrait.Seafaring))
        {
            if (project == ProductionProject.Harbor || project == ProductionProject.CommercialDock)
            {
                return baseCost / 2;
            }
        }
        if (HasTrait(CivTrait.Expansionist))
        {
            if (project == ProductionProject.Explorer)
            {
                return baseCost / 2;
            }
        }

        return baseCost;
    }

    private int GetBuildingCostFromRegistry(ProductionProject project)
    {
        string? buildingId = project switch
        {
            ProductionProject.Granary => "granary",
            ProductionProject.Monument => "monument",
            ProductionProject.Walls => "walls",
            ProductionProject.Barracks => "barracks",
            ProductionProject.Temple => "temple",
            ProductionProject.Library => "library",
            ProductionProject.Courthouse => "courthouse",
            ProductionProject.Marketplace => "marketplace",
            ProductionProject.Aqueduct => "aqueduct",
            ProductionProject.Colosseum => "colosseum",
            ProductionProject.Harbor => "harbor",
            ProductionProject.Bank => "bank",
            ProductionProject.Cathedral => "cathedral",
            ProductionProject.University => "university",
            ProductionProject.Hospital => "hospital",
            ProductionProject.Factory => "factory",
            ProductionProject.CoalPlant => "coal_plant",
            ProductionProject.HydroPlant => "hydro_plant",
            ProductionProject.SolarPlant => "solar_plant",
            ProductionProject.NuclearPlant => "nuclear_plant",
            ProductionProject.ManufacturingPlant => "manufacturing_plant",
            ProductionProject.Airport => "airport",
            ProductionProject.CivilDefense => "civil_defense",
            ProductionProject.PoliceStation => "police_station",
            ProductionProject.StockExchange => "stock_exchange",
            ProductionProject.RecyclingCenter => "recycling_center",
            ProductionProject.ResearchLab => "research_lab",
            ProductionProject.SAMMissileBattery => "sam_missile_battery",
            ProductionProject.OffshorePlatform => "offshore_platform",
            ProductionProject.MassTransitSystem => "mass_transit_system",
            ProductionProject.CommercialDock => "commercial_dock",
            ProductionProject.CoastalFortress => "coastal_fortress",
            ProductionProject.SSCockpit => "ss_cockpit",
            ProductionProject.SSDockingBay => "ss_docking_bay",
            ProductionProject.SSEngine => "ss_engine",
            ProductionProject.SSExteriorCasing => "ss_exterior_casing",
            ProductionProject.SSFuelCells => "ss_fuel_cells",
            ProductionProject.SSLifeSupportSystem => "ss_life_support_system",
            ProductionProject.SSPlanetaryPartyLounge => "ss_planetary_party_lounge",
            ProductionProject.SSStasisChamber => "ss_stasis_chamber",
            ProductionProject.SSStorageSupply => "ss_storage_supply",
            ProductionProject.SSThrusters => "ss_thrusters",
            ProductionProject.Palace => "palace",
            // Wonders
            ProductionProject.Pyramids => "pyramids",
            ProductionProject.HangingGardens => "hanging_gardens",
            ProductionProject.Colossus => "colossus",
            ProductionProject.GreatWall => "great_wall",
            ProductionProject.StatueOfZeus => "statue_of_zeus",
            ProductionProject.Oracle => "oracle",
            ProductionProject.KnightsHall => "knights_hall",
            ProductionProject.SovereignBath => "sovereign_bath",
            ProductionProject.LeonardoWorkshop => "leonardo_workshop",
            ProductionProject.ShakespearesTheatre => "shakespeares_theatre",
            ProductionProject.SunTzusWarAcademy => "sun_tzu_war_academy",
            ProductionProject.CureForCancer => "cure_for_cancer",
            ProductionProject.SistineChapel => "sistine_chapel",
            ProductionProject.TajMahal => "taj_mahal",
            ProductionProject.Astrolabe => "astrolabe",
            ProductionProject.Hermitage => "hermitage",
            ProductionProject.IronWorks => "iron_works",
            ProductionProject.SmithsMansion => "smith_mansion",
            ProductionProject.TrainStation => "train_station",
            ProductionProject.UnitedNations => "united_nations",
            ProductionProject.ApolloProgram => "apollo_program",
            ProductionProject.ManhattanProject => "manhattan_project",
            ProductionProject.Internet => "internet",
            ProductionProject.LongevityVaccine => "longevity_vaccine",
            ProductionProject.MarsColony => "mars_colony",
            ProductionProject.WorldBank => "world_bank",
            ProductionProject.SpaceStation => "space_station",
            // Small Wonders
            ProductionProject.HeroicEpic => "heroic_epic",
            ProductionProject.MilitaryAcademy => "military_academy",
            ProductionProject.Pentagon => "pentagon",
            ProductionProject.ForbiddenPalace => "forbidden_palace",
            ProductionProject.WallStreet => "wall_street",
            ProductionProject.IntelligenceAgency => "intelligence_agency",
            ProductionProject.BattlefieldMedicine => "battlefield_medicine",
            ProductionProject.SDIDefense => "sdi_defense",
            _ => null
        };
        if (buildingId == null) return 0;
        
        Wonder? wonder = WonderRegistry.Get(buildingId);
        if (wonder != null) return wonder.ProductionCost;
        
        var building = BuildingRegistry.Get(buildingId);
        return building?.ProductionCost ?? 0;
    }
}