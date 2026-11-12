using System;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.Core;

public enum CivilopediaCategory
{
    Units,
    Buildings,
    Wonders,
    Technologies,
    TerrainAndResources,
    Governments,
    Concepts
}

public class CivilopediaArticle
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public CivilopediaCategory Category { get; set; }
    public string SubCategory { get; set; } = "";
    public string Icon { get; set; } = "📖";
    public string Era { get; set; } = "";
    public Dictionary<string, string> KeyStats { get; set; } = new();
    public List<string> AbilityBadges { get; set; } = new();
    public string? RequiredTechId { get; set; }
    public string? RequiredTechName { get; set; }
    public List<string> RequiredResources { get; set; } = new();
    public string? UpgradesToId { get; set; }
    public string? UpgradesToName { get; set; }
    public string? UpgradesFromId { get; set; }
    public string? UpgradesFromName { get; set; }
    public List<(string TargetId, string Label)> RelatedArticles { get; set; } = new();
    public string StrategyAdvice { get; set; } = "";
    public string HistoricalLore { get; set; } = "";
}

/// <summary>
/// Core data repository for Civilopedia articles.
/// Pure C# engine-agnostic component that generates comprehensive encyclopedia entries
/// for all units, buildings, wonders, technologies, terrains, resources, governments, and game concepts.
/// </summary>
public static class CivilopediaRepository
{
    private static List<CivilopediaArticle>? _cachedArticles;

    public static List<CivilopediaArticle> GetAll()
    {
        if (_cachedArticles != null) return _cachedArticles;

        var list = new List<CivilopediaArticle>();
        BuildUnitArticles(list);
        BuildBuildingArticles(list);
        BuildWonderArticles(list);
        BuildTechArticles(list);
        BuildTerrainAndResourceArticles(list);
        BuildGovernmentArticles(list);
        BuildConceptArticles(list);

        _cachedArticles = list;
        return list;
    }

    public static IEnumerable<CivilopediaArticle> Search(string query, CivilopediaCategory? category = null)
    {
        var all = GetAll();
        if (category.HasValue)
            all = all.Where(a => a.Category == category.Value).ToList();

        if (string.IsNullOrWhiteSpace(query))
            return all;

        query = query.Trim().ToLowerInvariant();
        return all.Where(a =>
            a.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            a.SubCategory.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            a.Era.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            a.StrategyAdvice.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            a.HistoricalLore.Contains(query, StringComparison.OrdinalIgnoreCase)
        );
    }

    private static void BuildUnitArticles(List<CivilopediaArticle> list)
    {
        foreach (var def in UnitDefinitionRegistry.GetAll())
        {
            var art = new CivilopediaArticle
            {
                Id = def.Type.ToString(),
                Title = def.Name,
                Category = CivilopediaCategory.Units,
                Era = GetEraName(def.Era),
                RequiredTechId = def.RequiredTechId,
                RequiredTechName = GetTechDisplayName(def.RequiredTechId),
                RequiredResources = def.RequiredResourceIds.Select(r => r.ToUpperInvariant()).ToList()
            };

            // Icon & SubCategory
            art.SubCategory = def.Domain switch
            {
                UnitDomain.Sea => "Námorná jednotka",
                UnitDomain.Air => "Letecká jednotka",
                _ => def.ShieldCost == 0 || def.AttackStrength == 0 && def.DefenseStrength == 0 ? "Civilná jednotka" : "Pozemná jednotka"
            };

            art.Icon = def.Domain switch
            {
                UnitDomain.Sea => def.IsStealth ? "🌊" : "⚓",
                UnitDomain.Air => "✈️",
                _ => def.Type switch
                {
                    UnitType.Settler => "🏛️",
                    UnitType.Worker => "🔨",
                    UnitType.Explorer => "🧭",
                    UnitType.Archer or UnitType.Bowman => "🏹",
                    UnitType.Spearman or UnitType.Pikeman => "🛡️",
                    UnitType.Horseman or UnitType.Knight or UnitType.Cavalry => "🐎",
                    UnitType.Catapult or UnitType.Cannon or UnitType.Artillery => "🎯",
                    UnitType.Tank or UnitType.ModernArmor => "🚜",
                    _ => "⚔️"
                }
            };

            // Key Stats
            art.KeyStats["Útok"] = def.AttackStrength.ToString();
            art.KeyStats["Obrana"] = def.DefenseStrength.ToString();
            art.KeyStats["Pohyb"] = def.Movement.ToString("0.#");
            art.KeyStats["Cena"] = $"{def.ShieldCost} 🔨";
            if (def.BombardStrength > 0)
            {
                art.KeyStats["Bombard"] = $"{def.BombardStrength} (D:{def.BombardRange})";
            }
            if (def.TransportCapacity > 0)
            {
                art.KeyStats["Kapacita"] = $"{def.TransportCapacity} j.";
            }
            if (def.OperationalRange > 0)
            {
                art.KeyStats["Dolet"] = $"{def.OperationalRange} pol.";
            }

            // Ability Badges
            if (def.IsStealth)
                art.AbilityBadges.Add("🌊 Hlbinný stealth (Neviditeľná pre bežné lode)");
            if (def.DetectsStealth)
                art.AbilityBadges.Add("📡 ASW Sonar (Detekuje ponorky do 2 políčok)");
            if (def.HasHiddenNationality)
                art.AbilityBadges.Add("🏴‍☠️ Korzár (Skrytá národnosť - útok bez vyhlásenia vojny)");
            if (def.IsStealth && def.Domain == UnitDomain.Sea)
                art.AbilityBadges.Add("🛡️ Núdzový ponor (50% šanca na únik pri smrteľnom zásahu)");
            if (def.Type == UnitType.Tank || def.Type == UnitType.Panzer || def.Type == UnitType.ModernArmor || def.Type == UnitType.MechInfantry || def.Type == UnitType.Army)
                art.AbilityBadges.Add("⚡ Bleskový útok (Blitz - viac útokov za ťah)");
            if (def.Domain == UnitDomain.Sea || def.Domain == UnitDomain.Air || def.Type == UnitType.CruiseMissile)
                art.AbilityBadges.Add("💥 Smrtiace bombardovanie (Lethal Bombard)");
            if (def.SinksInSeaOcean)
                art.AbilityBadges.Add("⚠️ Riziko potopenia na mori a oceáne");
            if (def.IsUniqueUnit)
                art.AbilityBadges.Add("👑 Unikátna jednotka civilizácie (Spúšťa Zlatý vek)");

            // Upgrade link
            if (def.UpgradesTo.HasValue)
            {
                art.UpgradesToId = def.UpgradesTo.Value.ToString();
                art.UpgradesToName = UnitDefinitionRegistry.Get(def.UpgradesTo.Value).Name;
            }

            // Tactical Advice & Lore
            art.StrategyAdvice = GetUnitTacticalAdvice(def);
            art.HistoricalLore = !string.IsNullOrEmpty(def.Description) ? def.Description : $"{def.Name} zohráva kľúčovú úlohu v arzenáli civilizácií v ére {art.Era}.";

            list.Add(art);
        }
    }

    private static void BuildBuildingArticles(List<CivilopediaArticle> list)
    {
        foreach (var b in BuildingRegistry.All.Values.Where(b => !(b is Wonder)))
        {
            var art = new CivilopediaArticle
            {
                Id = b.Id,
                Title = b.Name,
                Category = CivilopediaCategory.Buildings,
                SubCategory = GetBuildingSubCategory(b),
                Icon = GetBuildingIcon(b),
                Era = GetTechEraForBuilding(b.RequiredTechId),
                RequiredTechId = b.RequiredTechId,
                RequiredTechName = GetTechDisplayName(b.RequiredTechId)
            };

            art.KeyStats["Cena"] = $"{b.ProductionCost} 🔨";
            art.KeyStats["Údržba"] = $"{b.MaintenanceCost} 💰/ťah";
            if (b.DefenseBonus > 0) art.KeyStats["Obrana"] = $"+{b.DefenseBonus}%";
            if (b.CommerceMultiplier > 1.0f) art.KeyStats["Obchod"] = $"+{(int)((b.CommerceMultiplier - 1.0f) * 100)}%";

            art.StrategyAdvice = GetBuildingTacticalAdvice(b);
            art.HistoricalLore = b.GetDescription();

            list.Add(art);
        }
    }

    private static void BuildWonderArticles(List<CivilopediaArticle> list)
    {
        foreach (var b in BuildingRegistry.All.Values.Where(b => b is Wonder))
        {
            var w = (Wonder)b;
            var art = new CivilopediaArticle
            {
                Id = w.Id,
                Title = w.Name,
                Category = CivilopediaCategory.Wonders,
                SubCategory = w.IsNationalWonder ? "Malý div sveta" : "Veľký div sveta",
                Icon = w.IsNationalWonder ? "🎖️" : "⭐",
                Era = GetTechEraForBuilding(w.RequiredTechId),
                RequiredTechId = w.RequiredTechId,
                RequiredTechName = GetTechDisplayName(w.RequiredTechId)
            };

            art.KeyStats["Cena"] = $"{w.ProductionCost} 🔨";
            art.KeyStats["Kultúra"] = $"+{w.CulturePerTurn} /ťah";
            art.KeyStats["Údržba"] = "Bezplatná";

            if (w.AssociatedTraits.Length > 0)
            {
                foreach (var trait in w.AssociatedTraits)
                {
                    art.AbilityBadges.Add($"✨ Črta civilizácie: {trait}");
                }
            }

            art.StrategyAdvice = GetWonderTacticalAdvice(w);
            art.HistoricalLore = $"{w.Name} patrí medzi najslávnejšie architektonické pamiatky ľudstva, ktoré inšpirujú celé generácie.";

            list.Add(art);
        }
    }

    private static void BuildTechArticles(List<CivilopediaArticle> list)
    {
        var tm = new TechManager();
        foreach (var tech in tm.AllTechnologies)
        {
            var art = new CivilopediaArticle
            {
                Id = tech.Id,
                Title = tech.Name,
                Category = CivilopediaCategory.Technologies,
                SubCategory = "Vedecký objav",
                Icon = !string.IsNullOrEmpty(tech.Icon) ? tech.Icon : "🔬",
                Era = GetEraName(tech.Era)
            };

            art.KeyStats["Cena vedy"] = $"{tech.ScienceCost} 💡";
            art.KeyStats["Éra"] = art.Era;

            if (tech.PrerequisiteIds.Length > 0)
            {
                foreach (var pre in tech.PrerequisiteIds)
                {
                    var preTech = tm.AllTechnologies.FirstOrDefault(t => t.Id == pre);
                    art.RelatedArticles.Add((pre, preTech != null ? preTech.Name : pre));
                }
            }

            // Find what units/buildings this tech unlocks
            var unlockedUnits = UnitDefinitionRegistry.GetAll().Where(u => u.RequiredTechId == tech.Id).Select(u => u.Name).ToList();
            var unlockedBuildings = BuildingRegistry.All.Values.Where(b => b.RequiredTechId == tech.Id).Select(b => b.Name).ToList();

            foreach (var u in unlockedUnits) art.AbilityBadges.Add($"⚔️ Odomyká jednotku: {u}");
            foreach (var b in unlockedBuildings) art.AbilityBadges.Add($"🏛️ Odomyká budovu: {b}");

            art.StrategyAdvice = $"Výskum technológie {tech.Name} otvára nové strategické možnosti v ére {art.Era}. Urýchľuje rozvoj armády a mestskej infraštruktúry.";
            art.HistoricalLore = !string.IsNullOrEmpty(tech.Description) ? tech.Description : $"Objav {tech.Name} zásadne zmenil spôsob, akým ľudské civilizácie organizovali svoje hospodárstvo a vojenskú moc.";

            list.Add(art);
        }
    }

    private static void BuildTerrainAndResourceArticles(List<CivilopediaArticle> list)
    {
        // 1. Terrains
        foreach (var t in TerrainRegistry.All.Values)
        {
            bool isWater = t.Id == "coast" || t.Id == "sea" || t.Id == "ocean";
            var art = new CivilopediaArticle
            {
                Id = t.Id,
                Title = t.Name,
                Category = CivilopediaCategory.TerrainAndResources,
                SubCategory = "Terén",
                Icon = isWater ? "🌊" : (t.Id == "mountain" ? "⛰️" : (t.Id == "hills" ? "🏔️" : (t.Id == "forest" ? "🌲" : "🌾"))),
                Era = "Príroda"
            };

            art.KeyStats["Jedlo"] = t.BaseYield.Food.ToString();
            art.KeyStats["Produkcia"] = t.BaseYield.Production.ToString();
            art.KeyStats["Obchod"] = t.BaseYield.Commerce.ToString();
            art.KeyStats["Pohyb"] = $"{t.MovementCost} MP";
            if (t.DefenseBonusPercent > 0) art.KeyStats["Obrana"] = $"+{t.DefenseBonusPercent}%";

            bool canIrrigate = t.Id == "grassland" || t.Id == "plains" || t.Id == "floodplains";
            bool canMine = t.Id == "hills" || t.Id == "mountain" || t.Id == "desert" || t.Id == "plains" || t.Id == "grassland";
            bool canRoad = !isWater;

            if (canIrrigate) art.AbilityBadges.Add("💧 Možné zavlažovanie (Farma)");
            if (canMine) art.AbilityBadges.Add("⛏️ Možná ťažba (Baňa)");
            if (canRoad) art.AbilityBadges.Add("🛤️ Možná stavba cesty & železnice");

            art.StrategyAdvice = $"Terén {t.Name} poskytuje základné suroviny pre okolité mestá. Zlepšenia robotníkmi výrazne znásobia jeho potenciál.";
            art.HistoricalLore = $"Geografické podmienky typu {t.Name} od pradávna formovali smerovanie expanzie a poľnohospodárstva ľudských osídlení.";

            list.Add(art);
        }

        // 2. Resources
        foreach (var res in ResourceRegistry.All.Values)
        {
            var art = new CivilopediaArticle
            {
                Id = res.Id,
                Title = res.Name,
                Category = CivilopediaCategory.TerrainAndResources,
                SubCategory = res is StrategicResource ? "Strategická surovina" : (res is LuxuryResource ? "Luxusná surovina" : "Bonusová surovina"),
                Icon = res is StrategicResource ? "⛏️" : (res is LuxuryResource ? "💎" : "🌾"),
                Era = "Suroviny"
            };

            art.KeyStats["Bonus Jedlo"] = $"+{res.BonusYield.Food}";
            art.KeyStats["Bonus Štíty"] = $"+{res.BonusYield.Production}";
            art.KeyStats["Bonus Obchod"] = $"+{res.BonusYield.Commerce}";

            if (res is StrategicResource sr)
            {
                if (!string.IsNullOrEmpty(sr.RequiredTechId))
                {
                    art.RequiredTechId = sr.RequiredTechId;
                    art.RequiredTechName = GetTechDisplayName(sr.RequiredTechId);
                }
                art.AbilityBadges.Add("⚠️ Riziko vyčerpania ložiska (1 z 800 ťahov)");
                art.AbilityBadges.Add("🔁 Možnosť 20-ťahového medzinárodného obchodu");
                art.StrategyAdvice = $"Kľúčová surovina pre vojenskú prevahu. Bez kontroly nad {res.Name} nemožno vyrábať najmodernejšie armádne jednotky.";
            }
            else if (res is LuxuryResource)
            {
                art.AbilityBadges.Add("😄 Zvyšuje spokojnosť občanov v ríši");
                art.AbilityBadges.Add("💰 Tržnica násobí efekt luxusných surovín");
                art.StrategyAdvice = $"Luxusné suroviny sú najefektívnejším spôsobom na potlačenie občianskych nepokojov (Civil Disorder) v rastúcich metropolách.";
            }

            art.HistoricalLore = $"Prírodné bohatstvo {res.Name} bolo po stáročia príčinou obchodných vojen i hospodárskeho rozkvetu impérií.";
            list.Add(art);
        }
    }

    private static void BuildGovernmentArticles(List<CivilopediaArticle> list)
    {
        var govs = new[]
        {
            (GovernmentType.Despotism, "Despotizmus", "Tyrania a autokratická vláda", "Neobmedzená", "Nútené práce (pop)", "Žiadna", "Základná forma vlády. Žiadny postih na armádu, ale produkčný trest na bohatých políčkach a vysoká korupcia."),
            (GovernmentType.Monarchy, "Monarchia", "Tradičná dedičná vláda kráľa", "Problémová", "Výkup zlatom", "Mierna", "Vynikajúca vláda na vedenie vojen v stredoveku. Podporuje veľkú armádu zadarmo a odstraňuje despotický trest."),
            (GovernmentType.Republic, "Republika", "Vláda voleného senátu a občanov", "Nízka", "Výkup zlatom", "Značná", "Obrovský bonus k obchodu (+1 obchod na každom ťaženom políčku). Vyžaduje opatrnosť pri dlhých vojnách pre vojnovú únavu."),
            (GovernmentType.Democracy, "Demokracia", "Plná vláda ľudu a slobodný trh", "Minimálna", "Výkup zlatom", "Vysoká", "Najvyššia efektivita výskumu a obchodu, robotníci pracujú o 50% rýchlejšie. Dlhá vojna však môže viesť k pádu vlády."),
            (GovernmentType.Communism, "Komunizmus", "Centrálne riadený štát a rovnostárstvo", "Rovnomerná (Communal)", "Nútené práce", "Žiadna", "Všetky mestá majú rovnakú nízku korupciu bez ohľadu na vzdialenosť od hlavného mesta. Ideálna pre obrovské kontinentálne ríše."),
            (GovernmentType.Feudalism, "Feudalizmus", "Vláda lénnych pánov a rytierov", "Problémová", "Nútené práce", "Mierna", "Masívna podpora armády v malých mestách. Ideálna pri expanzii a hustom osídlení."),
            (GovernmentType.Fascism, "Fašizmus", "Totalitná militaristická diktatúra", "Nízka", "Nútené práce", "Žiadna", "Najrýchlejší robotníci (200%), vysoká produkcia, žiadna vojnová únava. Mestá však strácajú časť populácie pri prevrate.")
        };

        foreach (var (type, name, sub, corr, hurry, wear, desc) in govs)
        {
            var art = new CivilopediaArticle
            {
                Id = type.ToString(),
                Title = name,
                Category = CivilopediaCategory.Governments,
                SubCategory = "Štátne zriadenie",
                Icon = "⚖️",
                Era = "Spoločnosť"
            };

            art.KeyStats["Korupcia"] = corr;
            art.KeyStats["Urýchlenie"] = hurry;
            art.KeyStats["Vojnová únava"] = wear;

            art.AbilityBadges.Add($"⚖️ Spôsob urýchlenia: {hurry}");
            art.AbilityBadges.Add($"🛡️ Vojnová únava: {wear}");

            art.StrategyAdvice = desc;
            art.HistoricalLore = $"Systém {name} predstavuje zásadný míľnik vo vývoji politického a hospodárskeho myslenia ľudskej spoločnosti.";

            list.Add(art);
        }
    }

    private static void BuildConceptArticles(List<CivilopediaArticle> list)
    {
        var concepts = new[]
        {
            ("naval_combat_asw", "Námorný boj, ponorky & ASW Sonar", "Námorná taktika", "⚓",
             "Ponorky sú neviditeľné pre bežné lode. Pohyb cudzej lode na políčko neviditeľnej ponorky spôsobí v mieri kolíziu (Submarine Bump), odhalí ponorku a zastaví loď bez vojny. Vo vojne vyvolá prepad zo zálohy.\n\n" +
             "• ASW Sonar (Torpédoborce a AEGIS Krížniky) odhaľuje všetky ponorky v okruhu 2 políčok.\n" +
             "• Taktický ústup (Emergency Deep Dive): Brániaca sa ponorka má 50% šancu uniknúť pred loďami bez ASW núdzovým ponorom s 1 HP do bezpečia."),

            ("privateers_hidden_nationality", "Korzárske lode & Skrytá národnosť", "Námorná taktika", "🏴‍☠️",
             "Korzári (Privateers) plávajú pod čiernou neutrálnou vlajkou (Jolly Roger). Môžu napadnúť akékoľvek cudzie plavidlo bez toho, aby vyhlásili vojnu materským štátom.\n\n" +
             "• Prepadávanie cudzích lodí nezruší mierové zmluvy ani 20-ťahové obchodné dohody.\n" +
             "• Boje s korzármi nespôsobujú vojnovú únavu."),

            ("cultural_flips", "Kultúra, hranice & Kultúrny prevrat miest", "Kultúra & Diplomacia", "🎭",
             "Kultúra produkovaná chrámami, knižnicami a divmi rozširuje hranice mesta. Ak má susedné impérium výrazne vyššiu kultúru a vaše pohraničné mesto je ďaleko od hlavného mesta, hrozí mu kultúrny prevrat (Cultural Flip)!\n\n" +
             "• Kultúrny poradca (F5) zobrazuje mieru rizika prevratu (🟢 Bezpečné, 🟡 Varovanie, 🚨 VYSOKÉ RIZIKO).\n" +
             "• Silná vojenská posádka v meste potláča nepokoje a znižuje riziko prevratu."),

            ("trade_agreements_20turns", "Obchodné dohody & 20-ťahové bilaterálne pakty", "Diplomacia & Obchod", "📜",
             "Všetky zmluvy o strategických surovinách, luxusoch, platbách za ťah (GPT) a práve prechodu (Right of Passage) trvajú 20 ťahov.\n\n" +
             "• Obchodný poradca (F2) ponúka prehľad všetkých aktívnych zmlúv s odpočtom zostávajúcich ťahov.\n" +
             "• Porušenie zmluvy pred uplynutím 20 ťahov vážne poškodzuje medzinárodnú reputáciu civilizácie."),

            ("golden_age", "Zlatý vek (Golden Age)", "Ríša & Prosperita", "✨",
             "Zlatý vek trvá 20 ťahov a poskytuje mimoriadny bonus: +1 štít produkcie a +1 zlato na každom políčku, ktoré už generuje aspoň 1 štít alebo zlato!\n\n" +
             "• Spúšťa sa víťazstvom unikátnej jednotky danej civilizácie v boji.\n" +
             "• Alebo dokončením Veľkého divu sveta, ktorý pokrýva obe historické črty vašej civilizácie (napr. Militaristic + Commercial)."),

            ("corruption_waste", "Korupcia, plytvanie & Efektívnosť", "Ekonomika", "💰",
             "Korupcia znižuje komerčné príjmy a plytvanie znižuje produkciu štítov. Zvyšuje sa so vzdialenosťou od hlavného mesta a prekročením optimálneho počtu miest (OCN).\n\n" +
             "• Súd (Courthouse) a Policajná stanica (Police Station) znižujú korupciu v meste o 50%.\n" +
             "• Zakázaný palác (Forbidden Palace) slúži ako druhé administratívne centrum ríše."),

            ("pollution_volcanoes", "Znečistenie, sopky & Globálne otepľovanie", "Životné prostredie", "🌋",
             "Veľké priemyselné mestá a nárast populácie vytvárajú znečistenie (Pollution). Neodstránené znečistenie spôsobuje globálne otepľovanie a mení úrodné lúky na púšte.\n\n" +
             "• Robotníci môžu vyčistiť znečistené políčko za niekoľko ťahov.\n" +
             "• Sopky môžu náhodne explodovať a zničiť vylepšenia v okolitom okruhu."),

            ("railroad_logistics", "Železničná sieť & Moderná logistika", "Infraštruktúra", "🚆",
             "Cesty znižujú pohybové náklady na 1/3 MP. Objav parného pohonu a stavba železníc však prináša neobmedzený pohyb (0 MP)!\n\n" +
             "• Armády sa môžu v okamihu presunúť z jedného konca kontinentu na druhý.\n" +
             "• Železnica pridáva +1 jedlo k zavlaženým farmám a +1 produkciu k baniam.")
        };

        foreach (var (id, title, sub, icon, text) in concepts)
        {
            var art = new CivilopediaArticle
            {
                Id = id,
                Title = title,
                Category = CivilopediaCategory.Concepts,
                SubCategory = sub,
                Icon = icon,
                Era = "Pravidlá hry",
                StrategyAdvice = text,
                HistoricalLore = $"Dôkladné pochopenie tejto hernej mechaniky je kľúčom k dosiahnutiu víťazstva na vyšších obtiažnostiach."
            };

            list.Add(art);
        }
    }

    private static string GetEraName(TechEra era) => era switch
    {
        TechEra.Ancient => "Starovek",
        TechEra.MiddleAges => "Stredovek",
        TechEra.Industrial => "Priemyselná",
        TechEra.Modern => "Moderná",
        _ => "Starovek"
    };

    private static string GetTechDisplayName(string? techId)
    {
        if (string.IsNullOrEmpty(techId)) return "";
        var tm = new TechManager();
        var t = tm.AllTechnologies.FirstOrDefault(x => x.Id == techId);
        return t != null ? t.Name : techId;
    }

    private static string GetTechEraForBuilding(string? techId)
    {
        if (string.IsNullOrEmpty(techId)) return "Starovek";
        var tm = new TechManager();
        var t = tm.AllTechnologies.FirstOrDefault(x => x.Id == techId);
        return t != null ? GetEraName(t.Era) : "Starovek";
    }

    private static string GetBuildingSubCategory(Building b)
    {
        if (b.DefenseBonus > 0) return "Obranná stavba";
        if (b.CommerceMultiplier > 1.0f) return "Hospodárska stavba";
        return "Mestská stavba";
    }

    private static string GetBuildingIcon(Building b) => b.Id switch
    {
        "granary" => "🌾",
        "barracks" => "⚔️",
        "temple" => "🕊️",
        "library" => "📚",
        "marketplace" => "💰",
        "aqueduct" => "💧",
        "colosseum" => "🏟️",
        "courthouse" => "⚖️",
        "cathedral" => "⛪",
        "university" => "🎓",
        "bank" => "🏦",
        "harbor" => "⚓",
        "factory" => "🏭",
        "airport" => "✈️",
        "hospital" => "🏥",
        _ => "🏛️"
    };

    private static string GetBuildingTacticalAdvice(Building b) => b.Id switch
    {
        "granary" => "Sýpka uchováva 50% nazhromaždeného jedla po každom raste populácie. Nevyhnutná budova pre mestá s vysokým rastom, ktoré cvičia osadníkov a robotníkov.",
        "barracks" => "Kasárne umožňujú cvičiť skúsených vojakov (Veteran namiesto Regular) a zabezpečujú plné vyliečenie pozemných jednotiek za jediný ťah.",
        "temple" => "Chrám produkuje +2 kultúru a upokojuje 1 nespokojného občana. Kľúčový pre rýchlu expanziu hraníc a potlačenie kultúrneho prevratu.",
        "library" => "Knižnica zvyšuje výskumný výkon mesta o +50% a generuje +3 kultúru za ťah.",
        "marketplace" => "Tržnica zvyšuje komerčné daňové príjmy o +50% a výrazne zvyšuje účinok luxusných surovín na šťastie občanov.",
        "aqueduct" => "Akvadukt umožňuje mestu rásť nad populáciu 6 (až do veľkosti 12). Mestá pri rieke ho nepotrebujú.",
        "hospital" => "Nemocnica umožňuje rast metropoly nad populáciu 12.",
        "courthouse" => "Súd znižuje korupciu a plytvanie v meste o 50%, čím vracia vzdialeným mestám hospodársku silu.",
        _ => $"{b.Name} poskytuje mestu trvalé civilizačné a hospodárske výhody."
    };

    private static string GetWonderTacticalAdvice(Wonder w) => w.Id switch
    {
        "pyramids" => "Poskytuje bezplatnú sýpku v každom meste na kontinente. Jeden z najsilnejších divov v hre pre raketový rast populácie v staroveku.",
        "great_library" => "Udeľuje každú technológiu, ktorú objavili aspoň 2 iné známe civilizácie. Umožňuje nulové výdavky na vedu a masívnu akumuláciu zlata.",
        "leonardo_workshop" => "Znižuje náklady na vylepšovanie všetkých vojenských jednotiek o 50%. Šetrí tisíce zlatých pri prechode z rytierov na kavalériu či pechotu.",
        "hoover_dam" => "Bezplatná vodná elektráreň vo všetkých mestách na kontinente. Zvyšuje produkciu o +50% bez znečistenia z uhoľných elektrární.",
        "theory_evolution" => "Okamžite udelí 2 bezplatné technológie podľa vlastného výberu.",
        "heroic_epic" => "Zvyšuje šancu na zrodenie Veľkého vojenského vodcu z elitných víťazstiev o +50% a pridáva +25% útočný bonus.",
        _ => $"{w.Name} je mimoriadny div, ktorý natrvalo zmení priebeh hry v prospech staviteľa."
    };

    private static string GetUnitTacticalAdvice(UnitDefinition def)
    {
        if (def.IsStealth)
            return "Ponorka je neviditeľná pre nepriateľské povrchové lode. Využívajte ju na prepady nepriateľských konvojov a transportov. Pozor na torpédoborce s aktívnym ASW Sonarom!";
        if (def.DetectsStealth)
            return "Torpédoborec je vybavený aktívnym ASW Sonarom, ktorý lokalizuje nepriateľské ponorky do vzdialenosti 2 políčok a zabraňuje im v taktickom ústupe.";
        if (def.HasHiddenNationality)
            return "Korzár môže napádať cudzie plavidlá bez vyhlásenia vojny. Ideálny na pirátske narúšanie námorného obchodu bez diplomatických následkov.";
        if (def.BombardStrength > 0)
            return "Delostrelecká jednotka: Ostreľujte nepriateľské armády, mestské hradby a infraštruktúru z bezpečnej vzdialenosti pred začatím priameho útoku.";
        if (def.Domain == UnitDomain.Sea)
            return "Námorné plavidlo určené na kontrolu morí, ochranu transportov a ostreľovanie pobrežných cieľov.";
        if (def.Domain == UnitDomain.Air)
            return "Letecká jednotka poskytujúca strategický dosah na nepriateľské zázemie, prieskum a ničenie infraštruktúry.";
        if (def.Movement >= 2)
            return "Rýchla manévrová jednotka: Má schopnosť ustúpiť pred pomalšími obrancami a vďaka vysokej rýchlosti dokáže hlboko preniknúť za nepriateľské línie.";
        return "Štandardná armádna jednotka tvoriaca chrbticu vojenských zborov v príslušnej ére.";
    }
}

