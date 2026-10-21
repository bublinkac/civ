using System;

namespace CivGame.Core;

public enum TechEra
{
    Ancient,
    MiddleAges,
    Industrial,
    Modern
}

public abstract class Technology
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract int ScienceCost { get; }
    public abstract string Description { get; }
    public abstract TechEra Era { get; }
    
    /// <summary>Tech IDs that must be researched before this technology becomes available.</summary>
    public virtual string[] PrerequisiteIds => Array.Empty<string>();
    
    /// <summary>Row position in the tech tree graph (0 = top).</summary>
    public virtual int Row => 0;
    
    /// <summary>Column position in the tech tree graph (0 = left).</summary>
    public virtual int Column => 0;

    /// <summary>Emoji icon representing this technology.</summary>
    public virtual string Icon => "🔬";

    public virtual void OnUnlocked(GameSimulation sim)
    {
        Console.WriteLine($"[Technology Unlocked] Global breakthrough: {Name} has been researched!");
    }
}

// ═══════════════════════════════════════════════════════
//  ANCIENT ERA (21 technologies)
// ═══════════════════════════════════════════════════════

public class Alphabet : Technology
{
    public override string Id => "alphabet";
    public override string Name => "Alphabet";
    public override int ScienceCost => 5;
    public override string Description => "Foundation of written language.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "🔤";
    public override int Row => 2;
    public override int Column => 0;
}

public class BronzeWorking : Technology
{
    public override string Id => "bronze_working";
    public override string Name => "Bronze Working";
    public override int ScienceCost => 10;
    public override string Description => "Allows training of powerful melee units.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "⚔️";
    public override int Row => 0;
    public override int Column => 0;
}

public class CeremonialBurial : Technology
{
    public override string Id => "ceremonial_burial";
    public override string Name => "Ceremonial Burial";
    public override int ScienceCost => 5;
    public override string Description => "Unlocks the construction of Monuments.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "⚱️";
    public override int Row => 6;
    public override int Column => 0;
}

public class CodeOfLaws : Technology
{
    public override string Id => "code_of_laws";
    public override string Name => "Code of Laws";
    public override int ScienceCost => 10;
    public override string Description => "Reduces corruption in cities (Courthouse).";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "⚖️";
    public override string[] PrerequisiteIds => new[] { "writing" };
    public override int Row => 3;
    public override int Column => 2;
}

public class Construction : Technology
{
    public override string Id => "construction";
    public override string Name => "Construction";
    public override int ScienceCost => 20;
    public override string Description => "Allows city growth beyond 6 population (Aqueduct).";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "🏗️";
    public override string[] PrerequisiteIds => new[] { "iron_working", "mathematics" };
    public override int Row => 0;
    public override int Column => 2;
}

public class Currency : Technology
{
    public override string Id => "currency";
    public override string Name => "Currency";
    public override int ScienceCost => 16;
    public override string Description => "Increases tax revenue by 50% (Marketplace).";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "💰";
    public override string[] PrerequisiteIds => new[] { "mathematics" };
    public override int Row => 1;
    public override int Column => 3;
}

public class HorsebackRiding : Technology
{
    public override string Id => "horseback_riding";
    public override string Name => "Horseback Riding";
    public override int ScienceCost => 8;
    public override string Description => "Enables fast cavalry units.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "🐴";
    public override string[] PrerequisiteIds => new[] { "the_wheel", "warrior_code" };
    public override int Row => 5;
    public override int Column => 1;
}

public class IronWorking : Technology
{
    public override string Id => "iron_working";
    public override string Name => "Iron Working";
    public override int ScienceCost => 12;
    public override string Description => "Allows advanced melee units and construction improvements.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "🔨";
    public override string[] PrerequisiteIds => new[] { "bronze_working" };
    public override int Row => 0;
    public override int Column => 1;
}

public class Literature : Technology
{
    public override string Id => "literature";
    public override string Name => "Literature";
    public override int ScienceCost => 12;
    public override string Description => "Increases research by 50% (Library).";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "📜";
    public override string[] PrerequisiteIds => new[] { "writing", "code_of_laws" };
    public override int Row => 4;
    public override int Column => 2;
}

public class MapMaking : Technology
{
    public override string Id => "map_making";
    public override string Name => "Map Making";
    public override int ScienceCost => 12;
    public override string Description => "Enables ocean travel (Harbor).";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "🗺️";
    public override string[] PrerequisiteIds => new[] { "the_wheel", "writing" };
    public override int Row => 4;
    public override int Column => 3;
}

public class Masonry : Technology
{
    public override string Id => "masonry";
    public override string Name => "Masonry";
    public override int ScienceCost => 4;
    public override string Description => "Allows construction of defensive walls and the Palace.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "🧱";
    public override int Row => 1;
    public override int Column => 0;
}

public class Mathematics : Technology
{
    public override string Id => "mathematics";
    public override string Name => "Mathematics";
    public override int ScienceCost => 12;
    public override string Description => "Enables advanced construction and currency.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "📐";
    public override string[] PrerequisiteIds => new[] { "masonry", "alphabet" };
    public override int Row => 1;
    public override int Column => 1;
}

public class Monarchy : Technology
{
    public override string Id => "monarchy";
    public override string Name => "Monarchy";
    public override int ScienceCost => 30;
    public override string Description => "Enables monarchy government.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "👑";
    public override string[] PrerequisiteIds => new[] { "polytheism", "code_of_laws" };
    public override int Row => 5;
    public override int Column => 3;
}

public class Mysticism : Technology
{
    public override string Id => "mysticism";
    public override string Name => "Mysticism";
    public override int ScienceCost => 4;
    public override string Description => "Foundation of religious thought (Polytheism).";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "🔮";
    public override string[] PrerequisiteIds => new[] { "ceremonial_burial" };
    public override int Row => 6;
    public override int Column => 1;
}

public class Philosophy : Technology
{
    public override string Id => "philosophy";
    public override string Name => "Philosophy";
    public override int ScienceCost => 8;
    public override string Description => "Enables the Republic government.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "🏛️";
    public override string[] PrerequisiteIds => new[] { "mathematics", "writing" };
    public override int Row => 1;
    public override int Column => 2;
}

public class Polytheism : Technology
{
    public override string Id => "polytheism";
    public override string Name => "Polytheism";
    public override int ScienceCost => 12;
    public override string Description => "Enables Monarchy and religious buildings.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "🕌";
    public override string[] PrerequisiteIds => new[] { "mysticism", "ceremonial_burial" };
    public override int Row => 6;
    public override int Column => 2;
}

public class Pottery : Technology
{
    public override string Id => "pottery";
    public override string Name => "Pottery";
    public override int ScienceCost => 5;
    public override string Description => "Unlocks the construction of Granaries.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "🏺";
    public override int Row => 3;
    public override int Column => 0;
}

public class WarriorCode : Technology
{
    public override string Id => "warrior_code";
    public override string Name => "Warrior Code";
    public override int ScienceCost => 8;
    public override string Description => "Enables advanced military units.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "🛡️";
    public override int Row => 5;
    public override int Column => 0;
}

public class TheWheel : Technology
{
    public override string Id => "the_wheel";
    public override string Name => "The Wheel";
    public override int ScienceCost => 10;
    public override string Description => "Enables trade routes and early transportation.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "☸️";
    public override int Row => 4;
    public override int Column => 0;
}

public class Writing : Technology
{
    public override string Id => "writing";
    public override string Name => "Writing";
    public override int ScienceCost => 5;
    public override string Description => "Enables literature and advanced trade (Map Making).";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "✍️";
    public override string[] PrerequisiteIds => new[] { "alphabet" };
    public override int Row => 2;
    public override int Column => 1;
}

public class Republic : Technology
{
    public override string Id => "republic";
    public override string Name => "The Republic";
    public override int ScienceCost => 40;
    public override string Description => "Enables Republic government.";
    public override TechEra Era => TechEra.Ancient;
    public override string Icon => "🏛️";
    public override string[] PrerequisiteIds => new[] { "philosophy", "code_of_laws" };
    public override int Row => 2;
    public override int Column => 3;
}

// ═══════════════════════════════════════════════════════
//  MIDDLE AGES (22 technologies)
// ═══════════════════════════════════════════════════════

public class Monotheism : Technology
{
    public override string Id => "monotheism";
    public override string Name => "Monotheism";
    public override int ScienceCost => 30;
    public override string Description => "Enables Cathedrals.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "✝️";
    public override string[] PrerequisiteIds => new[] { "polytheism" };
    public override int Row => 0;
    public override int Column => 0;
}

public class Theology : Technology
{
    public override string Id => "theology";
    public override string Name => "Theology";
    public override int ScienceCost => 40;
    public override string Description => "Enables Monotheism.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "📖";
    public override string[] PrerequisiteIds => new[] { "monotheism", "philosophy" };
    public override int Row => 0;
    public override int Column => 1;
}

public class Feudalism : Technology
{
    public override string Id => "feudalism";
    public override string Name => "Feudalism";
    public override int ScienceCost => 30;
    public override string Description => "Enables Feudalism government and improved infantry.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "🏰";
    public override string[] PrerequisiteIds => new[] { "warrior_code", "monarchy" };
    public override int Row => 3;
    public override int Column => 0;
}

public class Engineering : Technology
{
    public override string Id => "engineering";
    public override string Name => "Engineering";
    public override int ScienceCost => 30;
    public override string Description => "Enables advanced siege units.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "⚙️";
    public override string[] PrerequisiteIds => new[] { "construction", "mathematics" };
    public override int Row => 4;
    public override int Column => 0;
}

public class Chivalry : Technology
{
    public override string Id => "chivalry";
    public override string Name => "Chivalry";
    public override int ScienceCost => 40;
    public override string Description => "Enables powerful mounted units.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "🐎";
    public override string[] PrerequisiteIds => new[] { "horseback_riding", "feudalism" };
    public override int Row => 1;
    public override int Column => 1;
}

public class Education : Technology
{
    public override string Id => "education";
    public override string Name => "Education";
    public override int ScienceCost => 40;
    public override string Description => "Increases research by 50% (University).";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "🎓";
    public override string[] PrerequisiteIds => new[] { "mathematics", "literature" };
    public override int Row => 2;
    public override int Column => 1;
}

public class Invention : Technology
{
    public override string Id => "invention";
    public override string Name => "Invention";
    public override int ScienceCost => 44;
    public override string Description => "Unlocks Leonardo's Workshop and gunpowder units.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "💡";
    public override string[] PrerequisiteIds => new[] { "engineering" };
    public override int Row => 3;
    public override int Column => 1;
}

public class PrintingPress : Technology
{
    public override string Id => "printing_press";
    public override string Name => "Printing Press";
    public override int ScienceCost => 36;
    public override string Description => "Boosts culture and science output.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "📰";
    public override string[] PrerequisiteIds => new[] { "theology" };
    public override int Row => 0;
    public override int Column => 2;
}

public class MusicTheory : Technology
{
    public override string Id => "music_theory";
    public override string Name => "Music Theory";
    public override int ScienceCost => 40;
    public override string Description => "Increases culture and happiness.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "🎵";
    public override string[] PrerequisiteIds => new[] { "education", "theology" };
    public override int Row => 1;
    public override int Column => 2;
}

public class Gunpowder : Technology
{
    public override string Id => "gunpowder";
    public override string Name => "Gunpowder";
    public override int ScienceCost => 40;
    public override string Description => "Enables gunpowder units.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "💣";
    public override string[] PrerequisiteIds => new[] { "invention" };
    public override int Row => 3;
    public override int Column => 2;
}

public class Astronomy : Technology
{
    public override string Id => "astronomy";
    public override string Name => "Astronomy";
    public override int ScienceCost => 40;
    public override string Description => "Enables exploration of the seas (Navigation).";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "🔭";
    public override string[] PrerequisiteIds => new[] { "education", "theology" };
    public override int Row => 2;
    public override int Column => 2;
}

public class Democracy : Technology
{
    public override string Id => "democracy";
    public override string Name => "Democracy";
    public override int ScienceCost => 50;
    public override string Description => "Enables Democracy government.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "🗳️";
    public override string[] PrerequisiteIds => new[] { "printing_press", "free_artistry" };
    public override int Row => 0;
    public override int Column => 4;
}

public class FreeArtistry : Technology
{
    public override string Id => "free_artistry";
    public override string Name => "Free Artistry";
    public override int ScienceCost => 50;
    public override string Description => "Great persons provide greater cultural benefits.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "🎨";
    public override string[] PrerequisiteIds => new[] { "music_theory", "printing_press" };
    public override int Row => 0;
    public override int Column => 3;
}

public class Banking : Technology
{
    public override string Id => "banking";
    public override string Name => "Banking";
    public override int ScienceCost => 36;
    public override string Description => "Increases tax revenue by 50% (Bank).";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "🏦";
    public override string[] PrerequisiteIds => new[] { "education" };
    public override int Row => 1;
    public override int Column => 3;
}

public class Economics : Technology
{
    public override string Id => "economics";
    public override string Name => "Economics";
    public override int ScienceCost => 40;
    public override string Description => "Increases trade efficiency.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "📊";
    public override string[] PrerequisiteIds => new[] { "banking", "printing_press" };
    public override int Row => 1;
    public override int Column => 4;
}

public class Physics : Technology
{
    public override string Id => "physics";
    public override string Name => "Physics";
    public override int ScienceCost => 50;
    public override string Description => "Foundation of modern physics.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "⚛️";
    public override string[] PrerequisiteIds => new[] { "astronomy" };
    public override int Row => 2;
    public override int Column => 3;
}

public class Navigation : Technology
{
    public override string Id => "navigation";
    public override string Name => "Navigation";
    public override int ScienceCost => 50;
    public override string Description => "Enables transoceanic travel.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "⛵";
    public override string[] PrerequisiteIds => new[] { "astronomy", "economics" };
    public override int Row => 2;
    public override int Column => 4;
}

public class TheoryOfGravity : Technology
{
    public override string Id => "theory_of_gravity";
    public override string Name => "Theory of Gravity";
    public override int ScienceCost => 60;
    public override string Description => "Fundamental principles of physics.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "🍎";
    public override string[] PrerequisiteIds => new[] { "physics", "astronomy" };
    public override int Row => 2;
    public override int Column => 5;
}

public class Chemistry : Technology
{
    public override string Id => "chemistry";
    public override string Name => "Chemistry";
    public override int ScienceCost => 40;
    public override string Description => "Enables advanced weaponry and improvements.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "🧪";
    public override string[] PrerequisiteIds => new[] { "gunpowder" };
    public override int Row => 3;
    public override int Column => 3;
}

public class Metallurgy : Technology
{
    public override string Id => "metallurgy";
    public override string Name => "Metallurgy";
    public override int ScienceCost => 50;
    public override string Description => "Enables Coastal Fortresses and modern armor.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "🔩";
    public override string[] PrerequisiteIds => new[] { "gunpowder", "iron_working" };
    public override int Row => 4;
    public override int Column => 3;
}

public class Magnetism : Technology
{
    public override string Id => "magnetism";
    public override string Name => "Magnetism";
    public override int ScienceCost => 50;
    public override string Description => "Enables advanced naval vessels.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "🧲";
    public override string[] PrerequisiteIds => new[] { "physics", "chemistry" };
    public override int Row => 3;
    public override int Column => 4;
}

public class MilitaryTradition : Technology
{
    public override string Id => "military_tradition";
    public override string Name => "Military Tradition";
    public override int ScienceCost => 60;
    public override string Description => "Veteran units gain even more experience.";
    public override TechEra Era => TechEra.MiddleAges;
    public override string Icon => "🎖️";
    public override string[] PrerequisiteIds => new[] { "metallurgy" };
    public override int Row => 4;
    public override int Column => 4;
}

// ═══════════════════════════════════════════════════════
//  INDUSTRIAL ERA (24 technologies)
// ═══════════════════════════════════════════════════════

public class Nationalism : Technology
{
    public override string Id => "nationalism";
    public override string Name => "Nationalism";
    public override int ScienceCost => 100;
    public override string Description => "Enables Communism and Fascism governments.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🏴";
    public override string[] PrerequisiteIds => new[] { "military_tradition", "democracy" };
    public override int Row => 0;
    public override int Column => 0;
}

public class Ironclads : Technology
{
    public override string Id => "ironclads";
    public override string Name => "Ironclads";
    public override int ScienceCost => 100;
    public override string Description => "Enables armored naval vessels.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🚢";
    public override string[] PrerequisiteIds => new[] { "magnetism", "steam_power" };
    public override int Row => 1;
    public override int Column => 0;
}

public class SteamPower : Technology
{
    public override string Id => "steam_power";
    public override string Name => "Steam Power";
    public override int ScienceCost => 100;
    public override string Description => "Enables Industrialization.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🚂";
    public override string[] PrerequisiteIds => new[] { "theory_of_gravity", "metallurgy" };
    public override int Row => 2;
    public override int Column => 0;
}

public class Medicine : Technology
{
    public override string Id => "medicine";
    public override string Name => "Medicine";
    public override int ScienceCost => 40;
    public override string Description => "Allows city growth beyond 12 population (Hospital).";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "💊";
    public override string[] PrerequisiteIds => new[] { "sanitation" };
    public override int Row => 4;
    public override int Column => 0;
}

public class Sanitation : Technology
{
    public override string Id => "sanitation";
    public override string Name => "Sanitation";
    public override int ScienceCost => 50;
    public override string Description => "Allows city growth beyond 12 population (Hospital).";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🚰";
    public override string[] PrerequisiteIds => new[] { "engineering" };
    public override int Row => 5;
    public override int Column => 0;
}

public class Communism : Technology
{
    public override string Id => "communism";
    public override string Name => "Communism";
    public override int ScienceCost => 120;
    public override string Description => "Enables Communism government and Police Station.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "☭";
    public override int Row => 0;
    public override int Column => 1;
    public override string[] PrerequisiteIds => new[] { "nationalism" };
}

public class Espionage : Technology
{
    public override string Id => "espionage";
    public override string Name => "Espionage";
    public override int ScienceCost => 70;
    public override string Description => "Enables espionage missions.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🕵️";
    public override string[] PrerequisiteIds => new[] { "nationalism", "communism" };
    public override int Row => 0;
    public override int Column => 2;
}

public class Industrialization : Technology
{
    public override string Id => "industrialization";
    public override string Name => "Industrialization";
    public override int ScienceCost => 100;
    public override string Description => "Enables Factory and Coal Plant.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🏭";
    public override string[] PrerequisiteIds => new[] { "steam_power" };
    public override int Row => 2;
    public override int Column => 1;
}

public class TheCorporation : Technology
{
    public override string Id => "the_corporation";
    public override string Name => "The Corporation";
    public override int ScienceCost => 120;
    public override string Description => "Enables Stock Exchange.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🏢";
    public override string[] PrerequisiteIds => new[] { "industrialization", "economics" };
    public override int Row => 2;
    public override int Column => 2;
}

public class Electricity : Technology
{
    public override string Id => "electricity";
    public override string Name => "Electricity";
    public override int ScienceCost => 80;
    public override string Description => "Enables the Industrial Age.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "⚡";
    public override string[] PrerequisiteIds => new[] { "industrialization" };
    public override int Row => 3;
    public override int Column => 1;
}

public class ScientificMethod : Technology
{
    public override string Id => "scientific_method";
    public override string Name => "Scientific Method";
    public override int ScienceCost => 160;
    public override string Description => "Increases science output and enables modern techs.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🔬";
    public override string[] PrerequisiteIds => new[] { "electricity", "physics" };
    public override int Row => 4;
    public override int Column => 1;
}

public class Fascism : Technology
{
    public override string Id => "fascism";
    public override string Name => "Fascism";
    public override int ScienceCost => 90;
    public override string Description => "Enables Fascism government.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🦅";
    public override string[] PrerequisiteIds => new[] { "communism", "espionage" };
    public override int Row => 0;
    public override int Column => 3;
}

public class Refining : Technology
{
    public override string Id => "refining";
    public override string Name => "Refining";
    public override int ScienceCost => 100;
    public override string Description => "Enables advanced fuel and materials.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🛢️";
    public override string[] PrerequisiteIds => new[] { "industrialization", "scientific_method" };
    public override int Row => 1;
    public override int Column => 2;
}

public class Steel : Technology
{
    public override string Id => "steel";
    public override string Name => "Steel";
    public override int ScienceCost => 100;
    public override string Description => "Enables advanced construction materials.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🔧";
    public override string[] PrerequisiteIds => new[] { "industrialization", "metallurgy" };
    public override int Row => 2;
    public override int Column => 3;
}

public class Flight : Technology
{
    public override string Id => "flight";
    public override string Name => "Flight";
    public override int ScienceCost => 120;
    public override string Description => "Enables fighters, bombers, and airports.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "✈️";
    public override string[] PrerequisiteIds => new[] { "combustion", "refining" };
    public override int Row => 1;
    public override int Column => 3;
}

public class Combustion : Technology
{
    public override string Id => "combustion";
    public override string Name => "Combustion";
    public override int ScienceCost => 120;
    public override string Description => "Enables tanks, destroyers, and Flight.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🔥";
    public override string[] PrerequisiteIds => new[] { "refining", "steel" };
    public override int Row => 2;
    public override int Column => 4;
}

public class ReplaceableParts : Technology
{
    public override string Id => "replaceable_parts";
    public override string Name => "Replaceable Parts";
    public override int ScienceCost => 120;
    public override string Description => "Enables mass production techniques.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🔩";
    public override string[] PrerequisiteIds => new[] { "steel" };
    public override int Row => 3;
    public override int Column => 3;
}

public class MassProduction : Technology
{
    public override string Id => "mass_production";
    public override string Name => "Mass Production";
    public override int ScienceCost => 120;
    public override string Description => "Enables battleships, carriers, and Commercial Dock.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🏭";
    public override string[] PrerequisiteIds => new[] { "steel", "combustion" };
    public override int Row => 2;
    public override int Column => 5;
}

public class MotorizedTransportation : Technology
{
    public override string Id => "motorized_transportation";
    public override string Name => "Motorized Transp.";
    public override int ScienceCost => 120;
    public override string Description => "Enables tanks and advanced vehicles.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🚗";
    public override string[] PrerequisiteIds => new[] { "replaceable_parts" };
    public override int Row => 3;
    public override int Column => 4;
}

public class AtomicTheory : Technology
{
    public override string Id => "atomic_theory";
    public override string Name => "Atomic Theory";
    public override int ScienceCost => 160;
    public override string Description => "Foundation of nuclear technology.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "⚛️";
    public override string[] PrerequisiteIds => new[] { "scientific_method", "electronics" };
    public override int Row => 4;
    public override int Column => 3;
}

public class Electronics : Technology
{
    public override string Id => "electronics";
    public override string Name => "Electronics";
    public override int ScienceCost => 120;
    public override string Description => "Enables modern defensive and production buildings.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "📻";
    public override string[] PrerequisiteIds => new[] { "atomic_theory", "electricity" };
    public override int Row => 3;
    public override int Column => 4;
}

public class Radio : Technology
{
    public override string Id => "radio";
    public override string Name => "Radio";
    public override int ScienceCost => 120;
    public override string Description => "Enables Advanced Flight and mass communication.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "📡";
    public override string[] PrerequisiteIds => new[] { "electronics", "flight" };
    public override int Row => 1;
    public override int Column => 4;
}

public class AdvancedFlight : Technology
{
    public override string Id => "advanced_flight";
    public override string Name => "Advanced Flight";
    public override int ScienceCost => 180;
    public override string Description => "Enables advanced air units.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🛩️";
    public override string[] PrerequisiteIds => new[] { "radio", "rocketry" };
    public override int Row => 1;
    public override int Column => 5;
}

public class AmphibiousWar : Technology
{
    public override string Id => "amphibious_war";
    public override string Name => "Amphibious War";
    public override int ScienceCost => 120;
    public override string Description => "Enables marine units.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "🚁";
    public override string[] PrerequisiteIds => new[] { "flight", "mass_production" };
    public override int Row => 0;
    public override int Column => 5;
}

public class Plastics : Technology
{
    public override string Id => "plastics";
    public override string Name => "Plastics";
    public override int ScienceCost => 200;
    public override string Description => "Enables advanced manufacturing.";
    public override TechEra Era => TechEra.Industrial;
    public override string Icon => "♻️";
    public override string[] PrerequisiteIds => new[] { "refining" };
    public override int Row => 5;
    public override int Column => 2;
}

// ═══════════════════════════════════════════════════════
//  MODERN ERA (17 technologies)
// ═══════════════════════════════════════════════════════

public class Ecology : Technology
{
    public override string Id => "ecology";
    public override string Name => "Ecology";
    public override int ScienceCost => 200;
    public override string Description => "Enables Solar Plant and Mass Transit System.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "🌿";
    public override string[] PrerequisiteIds => new[] { "mass_production", "recycling" };
    public override int Row => 0;
    public override int Column => 0;
}

public class Recycling : Technology
{
    public override string Id => "recycling";
    public override string Name => "Recycling";
    public override int ScienceCost => 200;
    public override string Description => "Reduces pollution (Recycling Center).";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "♻️";
    public override string[] PrerequisiteIds => new[] { "ecology" };
    public override int Row => 0;
    public override int Column => 1;
}

public class SyntheticFibers : Technology
{
    public override string Id => "synthetic_fibers";
    public override string Name => "Synthetic Fibers";
    public override int ScienceCost => 200;
    public override string Description => "Enables SS Exterior Casing and related components.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "🧵";
    public override string[] PrerequisiteIds => new[] { "ecology" };
    public override int Row => 0;
    public override int Column => 2;
}

public class Stealth : Technology
{
    public override string Id => "stealth";
    public override string Name => "Stealth";
    public override int ScienceCost => 200;
    public override string Description => "Enables stealth units.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "🥷";
    public override string[] PrerequisiteIds => new[] { "synthetic_fibers" };
    public override int Row => 0;
    public override int Column => 3;
}

public class Rocketry : Technology
{
    public override string Id => "rocketry";
    public override string Name => "Rocketry";
    public override int ScienceCost => 160;
    public override string Description => "Enables SAM Missile Battery and space programs.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "🚀";
    public override string[] PrerequisiteIds => new[] { "fission", "electronics" };
    public override int Row => 1;
    public override int Column => 0;
}

public class SpaceFlight : Technology
{
    public override string Id => "space_flight";
    public override string Name => "Space Flight";
    public override int ScienceCost => 260;
    public override string Description => "Enables spaceship components.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "🛸";
    public override string[] PrerequisiteIds => new[] { "rocketry" };
    public override int Row => 1;
    public override int Column => 1;
}

public class Fission : Technology
{
    public override string Id => "fission";
    public override string Name => "Fission";
    public override int ScienceCost => 200;
    public override string Description => "Enables Nuclear Power and nuclear weapons.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "☢️";
    public override string[] PrerequisiteIds => new[] { "atomic_theory" };
    public override int Row => 2;
    public override int Column => 0;
}

public class NuclearPower : Technology
{
    public override string Id => "nuclear_power";
    public override string Name => "Nuclear Power";
    public override int ScienceCost => 200;
    public override string Description => "Enables Nuclear Plant.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "⚡";
    public override string[] PrerequisiteIds => new[] { "fission" };
    public override int Row => 2;
    public override int Column => 1;
}

public class TheLaser : Technology
{
    public override string Id => "the_laser";
    public override string Name => "The Laser";
    public override int ScienceCost => 260;
    public override string Description => "Enables SS Planetary Party Lounge.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "💎";
    public override string[] PrerequisiteIds => new[] { "nuclear_power", "miniaturization" };
    public override int Row => 3;
    public override int Column => 1;
}

public class Computers : Technology
{
    public override string Id => "computers";
    public override string Name => "Computers";
    public override int ScienceCost => 200;
    public override string Description => "Increases science by 50% (Research Lab).";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "💻";
    public override string[] PrerequisiteIds => new[] { "miniaturization", "electronics" };
    public override int Row => 4;
    public override int Column => 0;
}

public class Miniaturization : Technology
{
    public override string Id => "miniaturization";
    public override string Name => "Miniaturization";
    public override int ScienceCost => 180;
    public override string Description => "Enables Offshore Platforms and advanced electronics.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "🔎";
    public override string[] PrerequisiteIds => new[] { "computers" };
    public override int Row => 4;
    public override int Column => 1;
}

public class Superconductor : Technology
{
    public override string Id => "superconductor";
    public override string Name => "Superconductor";
    public override int ScienceCost => 260;
    public override string Description => "Enables SS Life Support System.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "🌡️";
    public override string[] PrerequisiteIds => new[] { "space_flight", "nuclear_power" };
    public override int Row => 1;
    public override int Column => 2;
}

public class Satellites : Technology
{
    public override string Id => "satellites";
    public override string Name => "Satellites";
    public override int ScienceCost => 240;
    public override string Description => "Enables SS Thrusters and global vision.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "🛰️";
    public override string[] PrerequisiteIds => new[] { "space_flight", "superconductor" };
    public override int Row => 1;
    public override int Column => 3;
}

public class SmartWeapons : Technology
{
    public override string Id => "smart_weapons";
    public override string Name => "Smart Weapons";
    public override int ScienceCost => 240;
    public override string Description => "Enables advanced missile systems.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "🎯";
    public override string[] PrerequisiteIds => new[] { "the_laser", "satellites" };
    public override int Row => 2;
    public override int Column => 3;
}

public class Genetics : Technology
{
    public override string Id => "genetics";
    public override string Name => "Genetics";
    public override int ScienceCost => 200;
    public override string Description => "Enables Cure for Cancer and longevity improvements.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "🧬";
    public override string[] PrerequisiteIds => new[] { "miniaturization", "the_laser" };
    public override int Row => 3;
    public override int Column => 2;
}

public class Robotics : Technology
{
    public override string Id => "robotics";
    public override string Name => "Robotics";
    public override int ScienceCost => 200;
    public override string Description => "Enables Manufacturing Plant.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "🤖";
    public override string[] PrerequisiteIds => new[] { "miniaturization", "computers" };
    public override int Row => 4;
    public override int Column => 2;
}

public class IntegratedDefense : Technology
{
    public override string Id => "integrated_defense";
    public override string Name => "Integrated Defense";
    public override int ScienceCost => 260;
    public override string Description => "Enables Strategic Missile Defense.";
    public override TechEra Era => TechEra.Modern;
    public override string Icon => "🛡️";
    public override string[] PrerequisiteIds => new[] { "satellites", "smart_weapons" };
    public override int Row => 1;
    public override int Column => 4;
}
