using System.Collections.Generic;

namespace CivGame.Core;

public static class CivilizationRegistry
{
    public static readonly Dictionary<string, Civilization> All = new();
    public static readonly List<Civilization> BaseGame = new();
    public static readonly List<Civilization> PlayTheWorld = new();
    public static readonly List<Civilization> Conquests = new();

    static CivilizationRegistry()
    {
        RegisterBase(new("america", "America", "Lincoln", CultureGroup.American, CivTrait.Expansionist, CivTrait.Industrious, new[]{"pottery", "masonry"}, "F-15", UnitType.F15, "Replaces Jet Fighter. Advanced fighter aircraft with superior air combat and bombardment capabilities."));
        RegisterBase(new("aztec", "Aztecs", "Montezuma", CultureGroup.American, CivTrait.Agricultural, CivTrait.Militaristic, new[]{"warrior_code", "pottery"}, "Jaguar Warrior", UnitType.JaguarWarrior, "Replaces Warrior. Swift jungle fighter with faster movement (+1 Mov).", movBonus:1));
        RegisterBase(new("babylon", "Babylon", "Hammurabi", CultureGroup.MidEastern, CivTrait.Religious, CivTrait.Scientific, new[]{"bronze_working", "ceremonial_burial"}, "Bowman", UnitType.Bowman, "Replaces Archer. Balanced defensive marksman with upgraded close combat skills (+1 Def).", defBonus:1));
        RegisterBase(new("china", "China", "Mao", CultureGroup.Asian, CivTrait.Industrious, CivTrait.Militaristic, new[]{"warrior_code", "masonry"}, "Rider", UnitType.Rider, "Replaces Knight. Fast-moving mounted cavalry with superior mobility (+1 Mov).", movBonus:1));
        RegisterBase(new("egypt", "Egypt", "Cleopatra", CultureGroup.Mediterranean, CivTrait.Industrious, CivTrait.Religious, new[]{"masonry", "ceremonial_burial"}, "War Chariot", UnitType.WarChariot, "Replaces Chariot. Fast attack chariot (+1 Atk, requires no horses).", atkBonus:1));
        RegisterBase(new("england", "England", "Elizabeth", CultureGroup.European, CivTrait.Commercial, CivTrait.Seafaring, new[]{"alphabet", "pottery"}, "Man-O-War", UnitType.ManOWar, "Replaces Frigate. Powerful naval warship with superior bombardment and combat (+1 Atk, +1 Def, Bombard 3).", atkBonus:1, defBonus:1));
        RegisterBase(new("france", "France", "Joan d'Arc", CultureGroup.European, CivTrait.Commercial, CivTrait.Industrious, new[]{"masonry", "alphabet"}, "Musketeer", UnitType.Musketeer, "Replaces Musketman. Elite guard infantry with advanced firearms (+1 Atk).", atkBonus:1));
        RegisterBase(new("germany", "Germany", "Bismarck", CultureGroup.European, CivTrait.Militaristic, CivTrait.Scientific, new[]{"warrior_code", "bronze_working"}, "Panzer", UnitType.Panzer, "Replaces Tank. Heavy tank unit with devastating offensive power (+1 Mov).", movBonus:1));
        RegisterBase(new("greece", "Greece", "Alexander", CultureGroup.Mediterranean, CivTrait.Commercial, CivTrait.Scientific, new[]{"bronze_working", "alphabet"}, "Hoplite", UnitType.Hoplite, "Replaces Spearman. Spear-armed phalanx with massive defensive fortification (+1 Def).", defBonus:1));
        RegisterBase(new("india", "India", "Gandhi", CultureGroup.Asian, CivTrait.Commercial, CivTrait.Religious, new[]{"ceremonial_burial", "alphabet"}, "War Elephant", UnitType.WarElephant, "Replaces Knight. Armored elephant cavalry (+1 HP, requires no resources)."));
        RegisterBase(new("iroquois", "Iroquois", "Hiawatha", CultureGroup.American, CivTrait.Agricultural, CivTrait.Commercial, new[]{"alphabet", "pottery"}, "Mounted Warrior", UnitType.MountedWarrior, "Replaces Horseman. Fast mounted woodland raider (+1 Atk).", atkBonus:1));
        RegisterBase(new("japan", "Japan", "Tokugawa", CultureGroup.Asian, CivTrait.Militaristic, CivTrait.Religious, new[]{"ceremonial_burial", "the_wheel"}, "Samurai", UnitType.Samurai, "Replaces Knight. Elite warrior with Bushido spirit (+1 Def, requires no horses).", defBonus:1));
        RegisterBase(new("persia", "Persia", "Xerxes", CultureGroup.MidEastern, CivTrait.Industrious, CivTrait.Scientific, new[]{"masonry", "bronze_working"}, "Immortals", UnitType.Immortals, "Replaces Swordsman. Legendary elite guard infantry with superior attack (+1 Atk).", atkBonus:1));
        RegisterBase(new("rome", "Rome", "Caesar", CultureGroup.Mediterranean, CivTrait.Commercial, CivTrait.Militaristic, new[]{"warrior_code", "alphabet"}, "Legionary", UnitType.Legionary, "Replaces Swordsman. Elite heavy infantry with superior defensive shields (+1 Def).", defBonus:1));
        RegisterBase(new("russia", "Russia", "Catherine", CultureGroup.European, CivTrait.Expansionist, CivTrait.Scientific, new[]{"bronze_working", "pottery"}, "Cossack", UnitType.Cossack, "Replaces Cavalry. Swift mounted raider with excellent defense (+1 Def).", defBonus:1));
        RegisterBase(new("zulu", "Zulu", "Shaka", CultureGroup.MidEastern, CivTrait.Expansionist, CivTrait.Militaristic, new[]{"warrior_code", "pottery"}, "Impi", UnitType.Impi, "Replaces Spearman. Fast-moving disciplined warrior formation (+1 Mov).", movBonus:1));

        RegisterPtw(new("arabia", "Arabia", "Abu Bakr", CultureGroup.MidEastern, CivTrait.Expansionist, CivTrait.Religious, new[]{"ceremonial_burial", "pottery"}, "Ansar Warrior", UnitType.AnsarWarrior, "Replaces Knight. Fast desert cavalry raider (+1 Mov, requires no iron).", movBonus:1));
        RegisterPtw(new("carthage", "Carthage", "Hannibal", CultureGroup.Mediterranean, CivTrait.Industrious, CivTrait.Seafaring, new[]{"alphabet", "masonry"}, "Numidian Mercenary", UnitType.NumidianMercenary, "Replaces Spearman. Highly trained African skirmishers (+1 Atk, +1 Def).", atkBonus:1, defBonus:1));
        RegisterPtw(new("celts", "Celts", "Brennus", CultureGroup.European, CivTrait.Agricultural, CivTrait.Religious, new[]{"pottery", "ceremonial_burial"}, "Gallic Swordsman", UnitType.GallicSwordsman, "Replaces Swordsman. Fierce charging swordsman (+1 Mov).", movBonus:1));
        RegisterPtw(new("korea", "Korea", "Wang Kon", CultureGroup.Asian, CivTrait.Commercial, CivTrait.Scientific, new[]{"alphabet", "bronze_working"}, "Hwach'a", UnitType.Hwacha, "Replaces Trebuchet. Advanced rocket-propelled missile cart."));
        RegisterPtw(new("mongols", "Mongols", "Temujin", CultureGroup.Asian, CivTrait.Expansionist, CivTrait.Militaristic, new[]{"warrior_code", "pottery"}, "Keshik", UnitType.Keshik, "Replaces Knight. Steppe horse archer (costs less, requires no iron)."));
        RegisterPtw(new("ottomans", "Ottomans", "Osman", CultureGroup.MidEastern, CivTrait.Industrious, CivTrait.Scientific, new[]{"bronze_working", "masonry"}, "Sipahi", UnitType.Sipahi, "Replaces Cavalry. Elite Ottoman heavy cavalry (+2 Atk).", atkBonus:2));
        RegisterPtw(new("spain", "Spain", "Isabella", CultureGroup.European, CivTrait.Religious, CivTrait.Seafaring, new[]{"alphabet", "ceremonial_burial"}, "Conquistador", UnitType.Explorer, "Replaces Explorer. Armored exploration unit with superior combat (+1 Atk, +1 Def).", atkBonus:1, defBonus:1));
        RegisterPtw(new("vikings", "Vikings", "Ragnar Lodbrok", CultureGroup.European, CivTrait.Militaristic, CivTrait.Seafaring, new[]{"alphabet", "warrior_code"}, "Berserk", UnitType.Berserk, "Replaces Medieval Infantry. Furious Norse warrior with devastating assault (+2 Atk).", atkBonus:2));

        RegisterCon(new("byzantines", "Byzantines", "Theodora", CultureGroup.Mediterranean, CivTrait.Scientific, CivTrait.Seafaring, new[]{"bronze_working", "alphabet"}, "Dromon", UnitType.Dromon, "Replaces Galley. Byzantine warship armed with Greek Fire (+1 Atk, Bombard 1).", atkBonus:1));
        RegisterCon(new("dutch", "Dutch", "William", CultureGroup.European, CivTrait.Agricultural, CivTrait.Seafaring, new[]{"pottery", "alphabet"}, "Swiss Mercenary", UnitType.SwissMercenary, "Replaces Pikeman. Disciplined mercenary pikeman (+1 Def, requires no iron).", defBonus:1));
        RegisterCon(new("hittites", "Hittites", "Mursilis", CultureGroup.MidEastern, CivTrait.Commercial, CivTrait.Expansionist, new[]{"pottery", "alphabet"}, "Three-Man Chariot", UnitType.ThreeManChariot, "Replaces Chariot. Heavy bronze-age chariot (+1 Atk).", atkBonus:1));
        RegisterCon(new("inca", "Inca", "Pachacuti", CultureGroup.American, CivTrait.Agricultural, CivTrait.Expansionist, new[]{"pottery", "masonry"}, "Chasqui Scout", UnitType.ChasquiScout, "Replaces Explorer. Swift mountain messenger."));
        RegisterCon(new("maya", "Maya", "Smoke-Jaguar", CultureGroup.American, CivTrait.Agricultural, CivTrait.Industrious, new[]{"pottery", "masonry"}, "Javelin Thrower", UnitType.JavelinThrower, "Replaces Archer. Skilled javelineer with defense (+1 Def).", defBonus:1));
        RegisterCon(new("portugal", "Portugal", "Henry", CultureGroup.European, CivTrait.Expansionist, CivTrait.Seafaring, new[]{"pottery", "alphabet"}, "Carrack", UnitType.Carrack, "Replaces Caravel. Ocean-faring exploration vessel with superior defense and transport (+1 Def, +1 Cargo).", defBonus:1));
        RegisterCon(new("sumeria", "Sumeria", "Gilgamesh", CultureGroup.MidEastern, CivTrait.Agricultural, CivTrait.Scientific, new[]{"pottery", "bronze_working"}, "Enkidu Warrior", UnitType.EnkiduWarrior, "Replaces Warrior. Ancient heroic warrior with defense (+1 Def).", defBonus:1));
    }

    private static void RegisterBase(Civilization civ)
    {
        All[civ.Id] = civ;
        BaseGame.Add(civ);
    }

    private static void RegisterPtw(Civilization civ)
    {
        All[civ.Id] = civ;
        PlayTheWorld.Add(civ);
    }

    private static void RegisterCon(Civilization civ)
    {
        All[civ.Id] = civ;
        Conquests.Add(civ);
    }

    public static Civilization? Get(string id) => All.TryGetValue(id, out var c) ? c : null;
    public static Civilization GetRandom() => BaseGame[System.Random.Shared.Next(BaseGame.Count)];
}