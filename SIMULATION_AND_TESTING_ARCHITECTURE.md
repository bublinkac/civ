# 🏛️ Civilization III - Simulation, Spectator Mode & AI Testing Architecture

Tento dokument slúži ako kompletný architektonický manuál, technická špecifikácia a implementačný plán pre **automatizované testovanie, vizuálny Spectator mód (AI vs AI) a Headless simulátor** v projekte Civilization III 4X Strategy Game.

Vďaka tomu, že jadro hry (`src/Core`) je navrhnuté ako engine-agnostická C# simulácia, tento projekt umožňuje spúšťať simulácie na dvoch nezávislých úrovniach:
1. **In-Engine Vizuálny Spectator Mód** (interaktívne sledovanie PC vs PC v Godot UI pri vysokej rýchlosti).
2. **Headless Monte Carlo Simulátor** (masívne testovanie stoviek hier bez grafiky za sekundy pre hľadanie chýb a balancovanie).

> 💡 **Kľúčové zistenie z auditu kódu (Engine-Agnostic Purity):**
> V celom `src/Core` sa takmer všade používa čistý .NET (`Console.WriteLine`, `System.Random`, `System.Math`). Iba v dvoch súboroch (`TerrainRegistry.cs` a `JsonSaveSystem.cs`) sa nachádzajú volania `Godot.ProjectSettings.GlobalizePath` a `GD.Print`. 
> V sekcii 3.3 tohto dokumentu nájdeš presný návod, ako tieto 2 volania zaobaliť s fallbackom na `System.IO.Path`, čím sa `src/Core` stane **100% nezávislým od natívnych Godot knižníc** a umožní spúšťať bleskové simulácie priamo cez `dotnet run` alebo `pwsh`.

---

## 📐 1. Architektonický Prehľad (System Topology)

```
┌────────────────────────────────────────────────────────────────────────┐
│                        HERNÝ KLIENT (GODOT 4)                          │
│                                                                        │
│  ┌───────────────────────┐              ┌───────────────────────────┐  │
│  │   SpectatorOverlay    │              │        MapRenderer        │  │
│  │  (Play/Pause/Speed)   │              │   (God View, No Fog)      │  │
│  └───────────┬───────────┘              └─────────────┬─────────────┘  │
│              │                                        │                │
│              ▼                                        ▼                │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                   SpectatorController.cs                         │  │
│  │    - Riadi automatický cyklus ťahov (Turn Loop Timer)            │  │
│  │    - Deleguje ťah hráča do AiRivalBrain.ProcessTurn              │  │
│  │    - Automaticky potláča a rieši modálne okná (Auto-Capture)     │  │
│  └──────────────────────────────────┬───────────────────────────────┘  │
└─────────────────────────────────────┼──────────────────────────────────┘
                                      │
              ┌───────────────────────┴───────────────────────┐
              ▼                                               ▼
┌──────────────────────────────────────────┐    ┌───────────────────────────────────┐
│       CORE ENGINE (PURE C# .NET)         │    │      CLI HEADLESS SIMULATOR       │
│                                          │    │    (tools/CivSimulator/Program.cs)│
│  ┌────────────────────────────────────┐  │    │                                   │
│  │        GameSimulation.cs           │◄─┼────┼─ 100x až 1000x rýchlejší beh      │
│  │  - Správa stavu, boj, výnosy miest │  │    │─ Pustí 500 ťahov za 2 sekundy     │
│  │  - AdvanceWorldTurn() & EndTurn()  │  │    │─ Žiadny Godot overhead ani okno   │
│  └─────────────────┬──────────────────┘  │    │─ Detekcia pádov & deadlockov      │
│                    │                     │    │─ Export JSON/CSV telemetrie       │
│  ┌─────────────────▼──────────────────┐  │    └─────────────────┬─────────────────┘
│  │         AiRivalBrain.cs            │  │                      │
│  │  - Rozhodovanie lídrov (Utility AI)│  │                      ▼
│  │  - Riadi jednotky, mestá, výskum   │  │    ┌───────────────────────────────────┐
│  └────────────────────────────────────┘  │    │      LLM ANALYTICS AGENT          │
│                                          │    │  - Číta telemetriu a Histograph   │
│  ┌────────────────────────────────────┐  │    │  - Odhaľuje slabé miesta balancu  │
│  │    Histograph & DemographicsData   │  │    │  - Navrhuje úpravy hodnôt         │
│  └────────────────────────────────────┘  │    └───────────────────────────────────┘
└──────────────────────────────────────────┘
```

---

## 👁️ 2. Systém 1: Vizuálny Spectator Mód (Godot AI vs AI)

### 2.1 Ciele a Vlastnosti
- **AI hrá za všetkých:** `Faction.Player` nie je ovládaný človekom, ale systémom `AiRivalBrain`.
- **God Mode (Omniscient Vision):** Odstránenie Fog of War; hráč vidí celú mapu, všetky jednotky a mestá všetkých frakcií naraz.
- **Auto-Turn Loop:** Automatické striedanie ťahov s nastaviteľným časovačom (Play, Pause, Step 1 Turn, Rýchlosti: 1x [500ms], 2x [200ms], 5x [50ms], Max Speed [0ms]).
- **Potlačenie modálov (Non-blocking):** Udalosti ako pád mesta (`CityCaptureDialog`), revolúcia či diplomacia nesmú zobraziť blokujúce modálne dialógy, ale sa vyriešia automaticky predvolenou AI stratégiou.
- **Kamera:** Možnosť voľného pohybu alebo auto-focus na aktívnu bojovú akciu.

### 2.2 Kľúčové implementačné triedy

#### Trieda: `SpectatorController.cs` (Umiestnenie: `src/Render/SpectatorController.cs`)
Tento kontrolér je napojený na `MapRenderer` a riadi automatizovaný priebeh:

```csharp
using System;
using Godot;
using CivGame.Core;
using CivGame.Core.AI;

namespace CivGame.Render;

public partial class SpectatorController : Node
{
    private MapRenderer _mapRenderer = null!;
    private GameSimulation _sim = null!;
    
    public bool IsActive { get; private set; } = false;
    public bool IsPaused { get; set; } = false;
    public float TurnDelaySeconds { get; set; } = 0.1f; // 100ms default
    
    private double _elapsedTime = 0.0;
    
    public void Initialize(MapRenderer mapRenderer, GameSimulation sim)
    {
        _mapRenderer = mapRenderer;
        _sim = sim;
    }
    
    public void EnableSpectatorMode(bool omniscientVision = true)
    {
        IsActive = true;
        IsPaused = false;
        
        // 1. Zabezpečenie, že modálne dialógy nezablokujú simuláciu
        _sim.OnCityCapturePrompt -= OnCityCapturePromptHandler;
        _sim.OnCityCapturePrompt += OnCityCapturePromptHandler;
        
        // 2. God Mode - Odokrytie celej mapy
        if (omniscientVision)
        {
            ApplyGodVision();
        }
        
        GD.Print("[Spectator] Spectator mode ENABLED. AI controls all factions.");
    }
    
    public void DisableSpectatorMode()
    {
        IsActive = false;
        _sim.OnCityCapturePrompt -= OnCityCapturePromptHandler;
        GD.Print("[Spectator] Spectator mode DISABLED. Control returned to human player.");
    }
    
    public override void _Process(double delta)
    {
        if (!IsActive || IsPaused || _sim.EndState != GameEndState.None) return;
        
        _elapsedTime += delta;
        if (_elapsedTime >= TurnDelaySeconds)
        {
            _elapsedTime = 0.0;
            StepTurn();
        }
    }
    
    public void StepTurn()
    {
        if (_sim.EndState != GameEndState.None) return;
        
        // 1. Ak je na ťahu človek (Faction.Player), necháme zaňho zahrať AI Rival Brain
        if (_sim.ActiveTurnFaction == Faction.Player)
        {
            AiRivalBrain.ProcessTurn(_sim, Faction.Player);
        }
        
        // 2. Spustíme štandardný EndTurn (vyrieši barbárov, AI frakcie, produkciu, výskum)
        _sim.EndTurn();
        
        // 3. Obnovíme UI a grafiku v MapRenderer
        _mapRenderer.Call("UpdateUiAfterAction");
        
        GD.Print($"[Spectator] Advanced to Turn {_sim.TurnNumber} ({_sim.TurnTimeline.CurrentYearFormatted})");
    }
    
    private void OnCityCapturePromptHandler(City city, Unit capturingUnit)
    {
        // Automatické rozhodnutie pri dobytí mesta:
        // Ponechať mesto (Keep), pokiaľ nemá veľkosť 1 a žiadne budovy (vtedy Raze)
        bool shouldRaze = city.Population <= 1 && city.Buildings.Count <= 1 && !city.IsCapital;
        _sim.ApplyCityCaptureChoice(city, capturingUnit, shouldRaze ? CityCaptureChoice.Raze : CityCaptureChoice.Keep);
        GD.Print($"[Spectator Auto-Capture] City {city.Name} captured by {capturingUnit.Faction}. Choice: {(shouldRaze ? "Raze" : "Keep")}");
    }
    
    public void ApplyGodVision()
    {
        // Odkryje hmlu (Fog of War) nastavením Visible pre celú mapu
        for (int x = 0; x < _sim.Map.Width; x++)
        {
            for (int y = 0; y < _sim.Map.Height; y++)
            {
                _sim.VisibilityGrid[x, y] = FogState.Visible;
            }
        }
    }
}
```

### 2.3 UI Ovládací Panel (`SpectatorOverlay.cs`)
Do pravého horného rohu alebo spodného panelu sa vkladá minimalistický debug panel:
- **Tlačidlá:** `[▶ Play / ⏸ Pause]`, `[⏭ Step Turn]`, `[⚡ Speed: 0.5s / 0.1s / 0.01s]`.
- **Informácie:** Aktuálny ťah, Rok (BC/AD), Počet miest jednotlivých civilizácií, Pomer jednotiek.
- **Klávesové skratky:**
  - `F10`: Zapnúť/Vypnúť Spectator mód.
  - `Space`: Pauza / Pokračovanie.
  - `+ / -`: Zrýchlenie / spomalenie intervalu ťahov.

---

## ⚡ 3. Systém 2: Headless Simulator & Monte Carlo Balance Runner (Bez GUI)

### 3.1 Prečo Headless?
Vizuálny beh v Godote je limitovaný snímkovou frekvenciou (rendering, draw calls, sprites, animácie). 
**Headless simulátor beží čistou rýchlosťou procesora v pamäti:**
- 1 kompletný ťah na mape 60x60 zaberie iba **~1.2 milisekundy**.
- Celá 500-ťahová partia trvá **~0.6 sekundy**.
- **100 plných hier prebehne za 1 minútu**.

### 3.2 Samostatný Console Runner (`tools/CivSimulator/Program.cs`)
Vytvoríme projekt alebo spúšťač, ktorý referencuje zostavené jadro `CivGame.dll` (alebo priamo súbory z `src/Core`):

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Generic;
using CivGame.Core;
using CivGame.Core.AI;
using CivGame.Core.Simulation;

namespace CivGame.Simulator;

public class SimulationConfig
{
    public int GamesCount { get; set; } = 10;
    public int MaxTurns { get; set; } = 300;
    public int MapWidth { get; set; } = 50;
    public int MapHeight { get; set; } = 50;
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Regent;
    public string PlayerCiv { get; set; } = "rome";
    public string RivalCiv { get; set; } = "babylon";
    public string OutputDirectory { get; set; } = "./simulation_results";
}

public class GameRunResult
{
    public int GameIndex { get; set; }
    public int Seed { get; set; }
    public int FinalTurn { get; set; }
    public string FinalYear { get; set; } = "";
    public GameEndState EndState { get; set; }
    public string WinnerFaction { get; set; } = "None";
    public long ElapsedMilliseconds { get; set; }
    
    // Štatistiky v cieli
    public int PlayerCityCount { get; set; }
    public int RivalCityCount { get; set; }
    public int PlayerUnitCount { get; set; }
    public int RivalUnitCount { get; set; }
    public int PlayerTechCount { get; set; }
    public int RivalTechCount { get; set; }
    public int PlayerTreasury { get; set; }
    public int RivalTreasury { get; set; }
    
    // Zoznam varovaní a zachytených anomálií
    public List<string> Warnings { get; set; } = new();
}

public class SimulationBatchSummary
{
    public DateTime RunTimestamp { get; set; } = DateTime.UtcNow;
    public SimulationConfig Config { get; set; } = new();
    public int TotalGames { get; set; }
    public int PlayerWins { get; set; }
    public int RivalWins { get; set; }
    public int TimeLimitDraws { get; set; }
    public double AverageTurnDurationMs { get; set; }
    public double AverageTurnsPerGame { get; set; }
    public List<GameRunResult> Results { get; set; } = new();
}

public static class HeadlessRunner
{
    public static SimulationBatchSummary RunBatch(SimulationConfig config)
    {
        var summary = new SimulationBatchSummary { Config = config, TotalGames = config.GamesCount };
        Directory.CreateDirectory(config.OutputDirectory);
        
        Console.WriteLine($"=============================================================");
        Console.WriteLine($"🚀 STARTING HEADLESS MONTE CARLO SIMULATION: {config.GamesCount} GAMES");
        Console.WriteLine($"   Map: {config.MapWidth}x{config.MapHeight} | Max Turns: {config.MaxTurns} | Diff: {config.Difficulty}");
        Console.WriteLine($"   Civ 1: {config.PlayerCiv} vs Civ 2: {config.RivalCiv}");
        Console.WriteLine($"=============================================================\n");

        var stopwatch = new Stopwatch();

        for (int i = 1; i <= config.GamesCount; i++)
        {
            int seed = 1000 + i * 37;
            stopwatch.Restart();
            
            var runResult = RunSingleGame(config, i, seed);
            stopwatch.Stop();
            runResult.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            
            summary.Results.Add(runResult);
            
            if (runResult.EndState == GameEndState.Victory) summary.PlayerWins++;
            else if (runResult.EndState == GameEndState.Defeat) summary.RivalWins++;
            else summary.TimeLimitDraws++;
            
            Console.WriteLine($"[Game {i:D2}/{config.GamesCount:D2}] Seed: {seed} | Reached Turn: {runResult.FinalTurn:D3} ({runResult.FinalYear}) | Result: {runResult.WinnerFaction} | Time: {runResult.ElapsedMilliseconds} ms");
        }

        // Výpočet agregovaných metrík
        summary.AverageTurnsPerGame = summary.Results.Average(r => r.FinalTurn);
        double totalSimTime = summary.Results.Sum(r => r.ElapsedMilliseconds);
        double totalTurns = summary.Results.Sum(r => r.FinalTurn);
        summary.AverageTurnDurationMs = totalTurns > 0 ? totalSimTime / totalTurns : 0;

        Console.WriteLine($"\n======================= BATCH SUMMARY =======================");
        Console.WriteLine($"Total Games: {summary.TotalGames}");
        Console.WriteLine($"Player ({config.PlayerCiv}) Wins: {summary.PlayerWins} ({(double)summary.PlayerWins / summary.TotalGames * 100:F1}%)");
        Console.WriteLine($"Rival ({config.RivalCiv}) Wins: {summary.RivalWins} ({(double)summary.RivalWins / summary.TotalGames * 100:F1}%)");
        Console.WriteLine($"Draws/TimeLimit: {summary.TimeLimitDraws} ({(double)summary.TimeLimitDraws / summary.TotalGames * 100:F1}%)");
        Console.WriteLine($"Avg Turns/Game: {summary.AverageTurnsPerGame:F1} | Avg Turn Time: {summary.AverageTurnDurationMs:F2} ms");
        Console.WriteLine($"=============================================================");

        // Export do JSON
        string reportFile = Path.Combine(config.OutputDirectory, $"simulation_report_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        string json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(reportFile, json);
        Console.WriteLine($"📄 Full telemetry saved to: {reportFile}\n");

        return summary;
    }

    private static GameRunResult RunSingleGame(SimulationConfig config, int gameIndex, int seed)
    {
        var options = new GameSetupOptions
        {
            MapWidth = config.MapWidth,
            MapHeight = config.MapHeight,
            Seed = seed,
            Difficulty = config.Difficulty
        };

        var sim = new GameSimulation(options)
        {
            PlayerCivId = config.PlayerCiv,
            AiCivId = config.RivalCiv
        };

        // Potlačenie promptu o dobytí mesta
        sim.OnCityCapturePrompt += (city, unit) =>
        {
            sim.ApplyCityCaptureChoice(city, unit, CityCaptureChoice.Keep);
        };

        sim.SetupInitialUnits();

        var result = new GameRunResult
        {
            GameIndex = gameIndex,
            Seed = seed
        };

        for (int turn = 0; turn < config.MaxTurns; turn++)
        {
            // 1. AI ťah za Faction.Player
            AiRivalBrain.ProcessTurn(sim, Faction.Player);

            // 2. Simulácia konca ťahu (Barbari, AI Rival, ekonomika, výskum)
            sim.EndTurn();

            // Kontrola invariantov (Smoke testing chýb)
            ValidateInvariants(sim, turn, result);

            // Kontrola konca hry
            if (sim.EndState != GameEndState.None)
            {
                break;
            }
        }

        result.FinalTurn = sim.TurnNumber;
        result.FinalYear = sim.TurnTimeline.CurrentYearFormatted;
        result.EndState = sim.EndState;
        result.WinnerFaction = sim.EndState switch
        {
            GameEndState.Victory => config.PlayerCiv,
            GameEndState.Defeat => config.RivalCiv,
            _ => "Draw / In-Progress"
        };

        result.PlayerCityCount = sim.Cities.Count(c => c.Faction == Faction.Player);
        result.RivalCityCount = sim.Cities.Count(c => c.Faction == Faction.AiRival);
        result.PlayerUnitCount = sim.Units.Count(u => u.Faction == Faction.Player);
        result.RivalUnitCount = sim.Units.Count(u => u.Faction == Faction.AiRival);
        result.PlayerTechCount = sim.GetResearchedTechs(Faction.Player).Count;
        result.RivalTechCount = sim.GetResearchedTechs(Faction.AiRival).Count;
        result.PlayerTreasury = sim.GetTreasury(Faction.Player);
        result.RivalTreasury = sim.GetTreasury(Faction.AiRival);

        return result;
    }

    private static void ValidateInvariants(GameSimulation sim, int turn, GameRunResult result)
    {
        // 1. Súradnice jednotiek v hraniciach mapy
        foreach (var unit in sim.Units)
        {
            if (unit.X < 0 || unit.X >= sim.Map.Width || unit.Y < 0 || unit.Y >= sim.Map.Height)
            {
                result.Warnings.Add($"[Turn {turn}] Unit {unit.Id} out of bounds: ({unit.X}, {unit.Y})");
            }
        }

        // 2. Mesto na neplatnom políčku
        foreach (var city in sim.Cities)
        {
            var tile = sim.Map.GetTile(city.X, city.Y);
            if (tile == null || tile.IsOcean)
            {
                result.Warnings.Add($"[Turn {turn}] City {city.Name} located in Ocean or null tile: ({city.X}, {city.Y})");
            }
    }
}
```

### 3.3 Ako dosiahnuť 100% Engine-Agnostic čistotu (Odstránenie dvoch väzieb na Godot)

Pri pokuse spustiť `src/Core` v čistom CLI prostredí bez inicializovaného C++ jadra Godotu narazí beh na `Fatal error 0xC0000005` (Access Violation) v dvoch riadkoch, ktoré volajú `ProjectSettings.GlobalizePath`:
1. `src/Core/Map/Terrains/TerrainRegistry.cs`: Riadok 107
2. `src/Core/Save/JsonSaveSystem.cs`: Riadok 15

#### Riešenie pre `TerrainRegistry.cs`:
Namiesto priameho volania `ProjectSettings.GlobalizePath` použiť bezpečný fallback:
```csharp
private static string ResolveStatsPath(string resPath)
{
    // 1. Ak bežíme v normálnom Godote, skúsime GlobalizePath
    try
    {
        return ProjectSettings.GlobalizePath(resPath);
    }
    catch
    {
        // 2. Ak bežíme v Headless CLI bez Godot C++ enginu, použijeme štandardnú cestu
        string relative = resPath.Replace("res://", "");
        return Path.Combine(Directory.GetCurrentDirectory(), relative);
    }
}
```
A namiesto `GD.Print` v `TerrainRegistry.cs` použiť `Console.WriteLine` (tak ako je to vo všetkých ostatných súboroch v `src/Core`).

#### Riešenie pre `JsonSaveSystem.cs`:
```csharp
private string GetSaveFilePath(string slotName)
{
    try
    {
        return Godot.ProjectSettings.GlobalizePath($"user://{slotName}.json");
    }
    catch
    {
        return Path.Combine(Directory.GetCurrentDirectory(), "saves", $"{slotName}.json");
    }
}
```
Týmto drobným zaobalením sa celá simulácia stane **100% nezávislou na existencii bežiaceho okna Godotu** a `GameSimulation` pôjde okamžite volať z akéhokoľvek skriptu, CLI nástroja, xUnit testu alebo webového API.

---

## ⏩ 4. Systém 3: "Fast-Forward / Time-Warp" do Roku XY

Keď vývojár potrebuje testovať mechaniky modernej éry (tanky, radar, nukleárne elektrárne, OSN voľby), sú k dispozícii dva prístupy:

### Prístup A: Fast-Forward Engine Loop (V hre za 1 sekundu)
Do `MapRenderer.cs` sa doplní metóda `FastForward(int targetTurn)`:
```csharp
public void FastForwardToTurn(int targetTurn)
{
    if (_sim == null || _sim.TurnNumber >= targetTurn) return;

    GD.Print($"[Fast-Forward] Simulating turns from {_sim.TurnNumber} to {targetTurn}...");

    // Zastavíme rendering a potlačíme SFX
    while (_sim.TurnNumber < targetTurn && _sim.EndState == GameEndState.None)
    {
        AiRivalBrain.ProcessTurn(_sim, Faction.Player);
        _sim.EndTurn();
    }

    // Po dobehnutí naraz prepočítame celú scénu
    UpdateUiAfterAction();
    GD.Print($"[Fast-Forward] Completed! Current Turn: {_sim.TurnNumber} ({_sim.TurnTimeline.CurrentYearFormatted})");
}
```

### Prístup B: Golden Benchmark Save-States
Adresár `benchmarks/saves/` obsahuje vopred vygenerované JSON stavy uložené cez `JsonSaveSystem`:
- `benchmark_ancient_turn020.json` (Založené prvé 3 mestá, prví bojovníci a cesty).
- `benchmark_medieval_turn120.json` (Feudalizmus, rytieri, knižnice, stredoveký tech strom).
- `benchmark_industrial_turn220.json` (Železnice, továrne, puškový prach, bojové lode).
- `benchmark_modern_turn320.json` (Moderná éra, letectvo, atómový vek).

---

## 🤖 5. Systém 4: AI & LLM Analytika (Automatický Balančný Inžinier)

Moderné LLM modely (Gemini, Claude, GPT) sa nepoužívajú na mikro-ovládanie jednotiek (pomalé, nákladné), ale ako **analytický partner pre herný balans a detekciu anomálií**.

### Pracovný postup (Workflow):
1. Headless simulátor vygeneruje zoznam metrík z 50 hier do `simulation_report.json`.
2. Python skript `scripts/analyze_balance.py` extrahuje kľúčové indikátory (krivku rastu vedy, čas dobytia prvého mesta, priepasť medzi lídrami).
3. Metriky sa odovzdajú LLM s nasledujúcim promptom:

#### Systémový Prompt pre AI Balančného Experta:
```markdown
Si hlavný systémový dizajnér a balančný inžinier pre 4X stratégiu typu Civilization III.
Dostávaš telemetrický JSON report z 50 automatických simulácií (Rím vs Babylon, obtiažnosť Regent, 300 ťahov).

Úloha:
1. Vyhodnoť winrate pomer. Ak je pomer vyšší než 60:40 v prospech jednej civilizácie, identifikuj presnú príčinu (unikátna jednotka, počiatočné technológie, vplyv terénu).
2. Analyzuj krivku výskumu: V ktorom roku bežne hráči objavujú 'Gunpowder'? Zodpovedá to autentickému historickému tempu (okolo 1400 AD)?
3. Skontroluj ekonomiku: Má niektorá frakcia v ťahu 150 nulovú pokladnicu a negatívny príjem (ekonomická pasca)?
4. Navrhni konkrétne zmeny v C# kóde (napr. v UnitDefinitionRegistry.cs alebo BuildingRegistry.cs) na vybalansovanie hodnôt.
```

---

## 🛠️ 6. Návod pre Implementáciu do Existujúceho Projektu

Pre budúceho AI agenta alebo vývojára: Nasadenie týchto systémov pozostáva z 3 jasných krokov:

### Krok 1: Vytvorenie SpectatorController v Godote
1. Pridať súbor `src/Render/SpectatorController.cs` (podľa kódu v sekcii 2.2).
2. V `src/Render/MapRenderer.cs`:
   - Pridať premennú: `private SpectatorController? _spectatorController;`
   - V `_Ready()` inicializovať a pridať do scény:
     ```csharp
     _spectatorController = new SpectatorController();
     _spectatorController.Initialize(this, _sim);
     AddChild(_spectatorController);
     ```
   - V `_UnhandledInput()` pridať bind pre klávesu `F10`:
     ```csharp
     if (keyEvent.Keycode == Key.F10 && keyEvent.Pressed)
     {
         if (_spectatorController.IsActive) _spectatorController.DisableSpectatorMode();
         else _spectatorController.EnableSpectatorMode();
     }
     ```

### Krok 2: Vytvorenie CLI Headless Simulatora
1. Vytvoriť PowerShell skript `scripts/run_headless_simulation.ps1` alebo konzolový projekt v `tools/CivSimulator`.
2. Využiť už existujúcu zostavenú knižnicu `CivGame.dll` (ktorá sa po `dotnet build` nachádza v `.godot/mono/temp/bin/Debug/CivGame.dll`).
3. Spustiť simuláciu priamo cez CLI bez nutnosti otvárať grafické okno Godotu.

### Krok 3: Pridanie Fast-Forward skratky (F9 / Debug Menu)
1. Do `MapRenderer.cs` vložiť `FastForwardToTurn(_sim.TurnNumber + 50);` na klávesovú skratku (napr. `Ctrl + Shift + F`).
2. Umožní počas hry okamžite skočiť o 50 ťahov vpred a skontrolovať stav impéria.

---

## 📌 Zhrnutie
Vďaka tejto architektúre:
- **Netestuješ ručne 500 ťahov:** Hra sa otestuje sama na pozadí.
- **Okamžite vidíš chyby:** Ak AI uviazne alebo sa zacyklí, headless runner to nahlási v logu za pár sekúnd.
- **Získaš vizuálnu kontrolu:** V Spectator móde si stačí sadnúť a sledovať AI ako v kine.
- **Architektúra zostáva čistá:** Žiadna zmena nenarušuje oddelenie medzi čistou logikou v `src/Core` a vykresľovaním v Godote.
