<#
.SYNOPSIS
    Headless Monte Carlo Simulator for CivGame (Civilization III Remake).
    Runs automated AI vs AI matches at native CPU speed without opening Godot GUI.

.DESCRIPTION
    Executes batches of games, validates simulation invariants, collects telemetry,
    calculates winrate balance, and exports structured JSON reports.

.EXAMPLE
    .\scripts\run_headless_simulation.ps1 -GamesCount 5 -MaxTurns 200 -Quiet
#>

param(
    [int]$GamesCount = 5,
    [int]$MaxTurns = 200,
    [int]$MapWidth = 40,
    [int]$MapHeight = 40,
    [string]$Difficulty = "Regent",
    [string]$PlayerCiv = "rome",
    [string]$RivalCiv = "babylon",
    [string]$OutputDir = "simulation_results",
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

# 1. Locate CivGame.dll
$dllPath = Join-Path $PSScriptRoot "..\.godot\mono\temp\bin\Debug\CivGame.dll"
if (-not (Test-Path $dllPath)) {
    Write-Host "[Build] CivGame.dll not found. Compiling project via dotnet build..." -ForegroundColor Yellow
    dotnet build
    if (-not (Test-Path $dllPath)) {
        throw "Failed to locate compiled CivGame.dll at $dllPath"
    }
}

Add-Type -Path $dllPath -ErrorAction Stop

# Ensure output directory exists
$resolvedOutputDir = Join-Path $PSScriptRoot "..\" | Join-Path -ChildPath $OutputDir
if (-not (Test-Path $resolvedOutputDir)) {
    New-Item -ItemType Directory -Path $resolvedOutputDir -Force | Out-Null
}

# Resolve difficulty enum
$diffEnum = [CivGame.Core.DifficultyLevel]::$Difficulty

Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host "🏛️  CIVGAME HEADLESS MONTE CARLO SIMULATOR" -ForegroundColor Cyan
Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host "Games: $GamesCount | Max Turns: $MaxTurns | Map: ${MapWidth}x${MapHeight} | Diff: $Difficulty"
Write-Host "Player AI Civ: $PlayerCiv vs Rival AI Civ: $RivalCiv"
Write-Host "Output Directory: $resolvedOutputDir"
Write-Host "=============================================================`n"

$results = [System.Collections.Generic.List[PSObject]]::new()
$totalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$gameStopwatch = [System.Diagnostics.Stopwatch]::new()

$playerWins = 0
$rivalWins = 0
$draws = 0

for ($i = 1; $i -le $GamesCount; $i++) {
    $seed = 1000 + $i * 137
    $gameStopwatch.Restart()

    # Configure Game
    $setup = [CivGame.Core.GameSetupOptions]::new()
    $setup.Width = $MapWidth
    $setup.Height = $MapHeight
    $setup.Seed = $seed
    $setup.Difficulty = $diffEnum
    $setup.PlayerCivId = $PlayerCiv
    $setup.RivalCivIds = [System.Collections.Generic.List[string]]::new([string[]]@($RivalCiv))

    $generator = [CivGame.Core.MapGenerator]::new($seed)
    $map = $generator.Generate($setup)
    $sim = [CivGame.Core.GameSimulation]::new($map)
    $sim.Difficulty = $diffEnum
    $sim.ConfigureFactions($PlayerCiv, [System.Collections.Generic.List[string]]::new([string[]]@($RivalCiv)))
    $sim.SetupInitialUnits()

    # Auto-resolve city capture prompt to avoid modal halt
    $sim.add_OnCityCapturePrompt({
        param($city, $unit)
        $shouldRaze = ($city.Population -le 1) -and ($city.Buildings.Count -le 1) -and (-not $city.IsCapital)
        $choice = if ($shouldRaze) { [CivGame.Core.CityCaptureChoice]::Raze } else { [CivGame.Core.CityCaptureChoice]::Keep }
        $sim.ApplyCityCaptureChoice($city, $unit, $choice)
    })

    $warnings = [System.Collections.Generic.List[string]]::new()

    # Simulate turns
    for ($t = 0; $t -lt $MaxTurns; $t++) {
        # Auto-pick tech if player has none
        if ($null -eq $sim.Research.CurrentResearch) {
            $avail = $sim.Research.GetAvailableTechnologies()
            if ($avail.Count -gt 0) {
                $cheapest = $avail | Sort-Object -Property Era, ScienceCost | Select-Object -First 1
                $sim.Research.SetResearch($cheapest)
            }
        }

        # Player AI turn
        if ($sim.ActiveTurnFaction -eq [CivGame.Core.Faction]::Player) {
            [CivGame.Core.AI.AiRivalBrain]::ProcessTurn($sim, [CivGame.Core.Faction]::Player)
        }

        # Advance world turn
        $sim.EndTurn()

        # Invariant checks
        foreach ($unit in $sim.Units) {
            if ($unit.X -lt 0 -or $unit.X -ge $sim.Map.Width -or $unit.Y -lt 0 -or $unit.Y -ge $sim.Map.Height) {
                $warnings.Add("[Turn $($sim.TurnNumber)] Unit $($unit.Id) out of bounds: ($($unit.X), $($unit.Y))")
            }
        }
        foreach ($city in $sim.Cities) {
            $tile = $sim.Map.GetTile($city.X, $city.Y)
            if ($null -eq $tile -or $tile.IsOcean) {
                $warnings.Add("[Turn $($sim.TurnNumber)] City $($city.Name) in ocean: ($($city.X), $($city.Y))")
            }
        }

        # Check for game end
        if ($sim.EndState -ne [CivGame.Core.GameEndState]::None) {
            break
        }
    }

    $gameStopwatch.Stop()

    $pCities = ($sim.Cities | Where-Object { $_.Faction -eq [CivGame.Core.Faction]::Player }).Count
    $rCities = ($sim.Cities | Where-Object { $_.Faction -eq [CivGame.Core.Faction]::AiRival }).Count
    $pUnits = ($sim.Units | Where-Object { $_.Faction -eq [CivGame.Core.Faction]::Player }).Count
    $rUnits = ($sim.Units | Where-Object { $_.Faction -eq [CivGame.Core.Faction]::AiRival }).Count

    $outcome = switch ($sim.EndState) {
        ([CivGame.Core.GameEndState]::VictoryConquest)   { "Player Victory (Conquest)" }
        ([CivGame.Core.GameEndState]::VictoryDomination) { "Player Victory (Domination)" }
        ([CivGame.Core.GameEndState]::VictorySpaceRace)  { "Player Victory (Space Race)" }
        ([CivGame.Core.GameEndState]::VictoryCultural)   { "Player Victory (Cultural)" }
        ([CivGame.Core.GameEndState]::VictoryDiplomatic) { "Player Victory (Diplomatic)" }
        ([CivGame.Core.GameEndState]::VictoryScore)      { "Player Victory (Score)" }
        ([CivGame.Core.GameEndState]::DefeatConquest)    { "Rival Victory (Conquest)" }
        ([CivGame.Core.GameEndState]::DefeatDomination)  { "Rival Victory (Domination)" }
        ([CivGame.Core.GameEndState]::DefeatSpaceRace)   { "Rival Victory (Space Race)" }
        ([CivGame.Core.GameEndState]::DefeatCultural)    { "Rival Victory (Cultural)" }
        ([CivGame.Core.GameEndState]::DefeatDiplomatic)  { "Rival Victory (Diplomatic)" }
        ([CivGame.Core.GameEndState]::DefeatScore)       { "Rival Victory (Score)" }
        default {
            if ($pCities -gt $rCities) { "Time Limit (Player Lead)" }
            elseif ($rCities -gt $pCities) { "Time Limit (Rival Lead)" }
            else { "Time Limit (Draw)" }
        }
    }

    if ($outcome -match "^Player Victory") { $playerWins++ }
    elseif ($outcome -match "^Rival Victory") { $rivalWins++ }
    else { $draws++ }

    $formattedYear = [CivGame.Core.TurnTimeline]::GetFormattedYear($sim.TurnNumber)

    $resultObj = [PSCustomObject]@{
        GameIndex = $i
        Seed = $seed
        FinalTurn = $sim.TurnNumber
        FinalYear = $formattedYear
        Outcome = $outcome
        ElapsedMs = $gameStopwatch.ElapsedMilliseconds
        PlayerCities = $pCities
        RivalCities = $rCities
        PlayerUnits = $pUnits
        RivalUnits = $rUnits
        PlayerTreasury = $sim.GetTreasury([CivGame.Core.Faction]::Player)
        RivalTreasury = $sim.GetTreasury([CivGame.Core.Faction]::AiRival)
        PlayerTechs = $sim.GetResearchedTechs([CivGame.Core.Faction]::Player).Count
        RivalTechs = $sim.GetResearchedTechs([CivGame.Core.Faction]::AiRival).Count
        WarningsCount = $warnings.Count
        Warnings = $warnings
    }

    $results.Add($resultObj)

    $color = if ($outcome -match "^Player Victory") { "Green" }
             elseif ($outcome -match "^Rival Victory") { "Red" }
             else { "Yellow" }

    Write-Host ("[{0:D2}/{1:D2}] Seed: {2} | Turn: {3:D3} ({4,-8}) | Result: {5,-28} | Cities: {6} vs {7} | Time: {8,5} ms" -f `
        $i, $GamesCount, $seed, $sim.TurnNumber, $formattedYear, $outcome, $pCities, $rCities, $gameStopwatch.ElapsedMilliseconds) -ForegroundColor $color
}

$totalStopwatch.Stop()

# Compute summary stats
$totalTurns = ($results | Measure-Object -Property FinalTurn -Sum).Sum
$avgTurns = if ($GamesCount -gt 0) { $totalTurns / $GamesCount } else { 0 }
$avgTurnTime = if ($totalTurns -gt 0) { $totalStopwatch.ElapsedMilliseconds / $totalTurns } else { 0 }

Write-Host "`n======================= BATCH SUMMARY =======================" -ForegroundColor Cyan
Write-Host "Total Games:       $GamesCount in $([Math]::Round($totalStopwatch.Elapsed.TotalSeconds, 2))s"
Write-Host "Player ($PlayerCiv) Wins:  $playerWins ($([Math]::Round(($playerWins / $GamesCount) * 100, 1))%)" -ForegroundColor Green
Write-Host "Rival  ($RivalCiv) Wins:  $rivalWins ($([Math]::Round(($rivalWins / $GamesCount) * 100, 1))%)" -ForegroundColor Red
Write-Host "Draws / Timeouts:  $draws ($([Math]::Round(($draws / $GamesCount) * 100, 1))%)" -ForegroundColor Yellow
Write-Host "Avg Turns / Game:  $([Math]::Round($avgTurns, 1))"
Write-Host "Avg Turn Time:     $([Math]::Round($avgTurnTime, 2)) ms"
Write-Host "=============================================================" -ForegroundColor Cyan

# Export JSON report
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $resolvedOutputDir "simulation_report_$timestamp.json"

$summaryObj = [PSCustomObject]@{
    Timestamp = (Get-Date).ToString("o")
    Config = @{
        GamesCount = $GamesCount
        MaxTurns = $MaxTurns
        MapWidth = $MapWidth
        MapHeight = $MapHeight
        Difficulty = $Difficulty
        PlayerCiv = $PlayerCiv
        RivalCiv = $RivalCiv
    }
    Summary = @{
        TotalGames = $GamesCount
        TotalTurns = $totalTurns
        PlayerWins = $playerWins
        RivalWins = $rivalWins
        Draws = $draws
        PlayerWinRate = [Math]::Round(($playerWins / $GamesCount) * 100, 2)
        RivalWinRate = [Math]::Round(($rivalWins / $GamesCount) * 100, 2)
        AverageTurnsPerGame = [Math]::Round($avgTurns, 2)
        AverageTurnDurationMs = [Math]::Round($avgTurnTime, 2)
        TotalElapsedSeconds = [Math]::Round($totalStopwatch.Elapsed.TotalSeconds, 2)
    }
    Results = $results
}

$summaryObj | ConvertTo-Json -Depth 5 | Set-Content -Path $reportFile -Encoding UTF8
Write-Host "`n📄 Full telemetry exported to: $reportFile`n" -ForegroundColor Green

