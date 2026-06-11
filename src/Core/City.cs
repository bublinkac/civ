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

    public int GetTotalMaintenance() => Buildings.Sum(b => b.MaintenanceCost);

    public T? GetBuilding<T>() where T : Building
    {
        return Buildings.Find(b => b is T) as T;
    }

    public int GetProjectCost(ProductionProject project)
    {
        return project switch
        {
            ProductionProject.Explorer => 10,
            ProductionProject.Settler => 20,
            ProductionProject.Worker => 15,
            ProductionProject.Warrior => 15,
            ProductionProject.Archer => 20,
            _ => GetBuildingCostFromRegistry(project)
        };
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