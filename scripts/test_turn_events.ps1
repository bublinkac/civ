param(
    [string]$DllPath = ".\.godot\mono\temp\bin\Debug\CivGame.dll"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $DllPath)) {
    Write-Host "[Build] CivGame.dll not found. Compiling project via dotnet build..." -ForegroundColor Yellow
    dotnet build --no-restore
}

Add-Type -Path $DllPath -ErrorAction Stop

Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host "📰 TURN EVENT TICKER & ADVISOR NOTIFICATIONS TEST" -ForegroundColor Cyan
Write-Host "=============================================================" -ForegroundColor Cyan

# 1. Test TurnEvent and TurnEventLog Core API
Write-Host "`n1. Testing Core TurnEventLog operations..." -ForegroundColor Yellow
$log = [CivGame.Core.TurnEventLog]::new()

$ev1 = [CivGame.Core.TurnEvent]::new()
$ev1.Turn = 1
$ev1.Faction = [CivGame.Core.Faction]::Player
$ev1.Category = [CivGame.Core.TurnEventCategory]::Production
$ev1.Advisor = [CivGame.Core.AdvisorType]::Domestic
$ev1.Severity = [CivGame.Core.TurnEventSeverity]::Success
$ev1.Icon = "🔨"
$ev1.Title = "Výstavba dokončená: KASÁRNE"
$ev1.Message = "Mesto Rím dokončilo výstavbu budovy Kasárne."
$ev1.TargetCityId = "city_1"
$ev1.TargetCityName = "Rím"
$ev1.ActionType = [CivGame.Core.TurnEventActionType]::OpenCity
$ev1.ActionLabel = "Spravovať mesto"

$ev2 = [CivGame.Core.TurnEvent]::new()
$ev2.Turn = 1
$ev2.Faction = [CivGame.Core.Faction]::Player
$ev2.Category = [CivGame.Core.TurnEventCategory]::Disorder
$ev2.Advisor = [CivGame.Core.AdvisorType]::Domestic
$ev2.Severity = [CivGame.Core.TurnEventSeverity]::Critical
$ev2.Icon = "🚨"
$ev2.Title = "Občianske nepokoje v meste Antium!"
$ev2.Message = "Obyvatelia odmietajú pracovať! Pridajte zabávačov alebo znížte dane."
$ev2.TargetCityId = "city_2"
$ev2.TargetCityName = "Antium"
$ev2.ActionType = [CivGame.Core.TurnEventActionType]::OpenCity

$ev3 = [CivGame.Core.TurnEvent]::new()
$ev3.Turn = 2
$ev3.Faction = [CivGame.Core.Faction]::Player
$ev3.Category = [CivGame.Core.TurnEventCategory]::Science
$ev3.Advisor = [CivGame.Core.AdvisorType]::Science
$ev3.Severity = [CivGame.Core.TurnEventSeverity]::Success
$ev3.Icon = "🔬"
$ev3.Title = "Vedecký objav: ŽELEZO!"
$ev3.Message = "Naši vedci objavili spracovanie železa."
$ev3.ActionType = [CivGame.Core.TurnEventActionType]::OpenTechTree

$log.AddEvent($ev1)
$log.AddEvent($ev2)
$log.AddEvent($ev3)

# Assert counts
$turn1Events = $log.GetTurnEvents(1, [CivGame.Core.Faction]::Player)
Write-Host "  Turn 1 events count: $($turn1Events.Count) (expected 2)" -ForegroundColor White
if ($turn1Events.Count -ne 2) { throw "Expected 2 events for Turn 1, got $($turn1Events.Count)" }

$unread = $log.GetUnreadEvents([CivGame.Core.Faction]::Player)
Write-Host "  Unread events count: $($unread.Count) (expected 3)" -ForegroundColor White
if ($unread.Count -ne 3) { throw "Expected 3 unread events, got $($unread.Count)" }

$log.MarkAllAsRead([CivGame.Core.Faction]::Player)
$unreadAfter = $log.GetUnreadEvents([CivGame.Core.Faction]::Player)
Write-Host "  Unread after MarkAllAsRead: $($unreadAfter.Count) (expected 0)" -ForegroundColor White
if ($unreadAfter.Count -ne 0) { throw "Expected 0 unread events after MarkAllAsRead, got $($unreadAfter.Count)" }

Write-Host "  ✓ Core TurnEventLog tests passed." -ForegroundColor Green

# 2. Test GameSimulation Event Integration
Write-Host "`n2. Testing GameSimulation Turn Events during world simulation..." -ForegroundColor Yellow

$setup = [CivGame.Core.GameSetupOptions]::new()
$setup.Width = 40
$setup.Height = 40
$setup.Seed = 4242
$setup.Difficulty = [CivGame.Core.DifficultyLevel]::Regent
$setup.PlayerCivId = "rome"
$setup.RivalCivIds = [System.Collections.Generic.List[string]]::new([string[]]@("babylon"))

$generator = [CivGame.Core.MapGenerator]::new(4242)
$map = $generator.Generate($setup)
$sim = [CivGame.Core.GameSimulation]::new($map)
$sim.Difficulty = [CivGame.Core.DifficultyLevel]::Regent
$sim.ConfigureFactions("rome", [System.Collections.Generic.List[string]]::new([string[]]@("babylon")))
$sim.SetupInitialUnits()

# Check Turn 1 greeting event
$t1Events = $sim.TurnEvents.GetTurnEvents(1, [CivGame.Core.Faction]::Player)
Write-Host "  Turn 1 initial events in GameSimulation: $($t1Events.Count)" -ForegroundColor White
if ($t1Events.Count -eq 0) { throw "Expected at least 1 turn event on Turn 1 in GameSimulation" }
Write-Host "  -> $($t1Events[0].Icon) $($t1Events[0].Title): $($t1Events[0].Message)" -ForegroundColor Gray

# Found city with settler
$settler = $sim.Units | Where-Object { $_.Faction -eq [CivGame.Core.Faction]::Player -and $_.Type -eq [CivGame.Core.UnitType]::Settler } | Select-Object -First 1
if ($null -ne $settler) {
    Write-Host "  Founding city with settler at ($($settler.X), $($settler.Y))..." -ForegroundColor Gray
    $sim.BuildCity($settler)
}

# Run 10 turns
Write-Host "  Simulating 10 turns..." -ForegroundColor Gray
for ($t = 0; $t -lt 10; $t++) {
    if ($sim.ActiveTurnFaction -eq [CivGame.Core.Faction]::Player) {
        [CivGame.Core.AI.AiRivalBrain]::ProcessTurn($sim, [CivGame.Core.Faction]::Player)
    }
    $sim.EndTurn()
}

$allPlayerEvents = $sim.TurnEvents.GetLatestEvents([CivGame.Core.Faction]::Player, 50)
Write-Host "`nTotal Player Turn Events generated across 10 turns: $($allPlayerEvents.Count)" -ForegroundColor Green

foreach ($e in $allPlayerEvents) {
    Write-Host "  [Turn $($e.Turn)] $($e.Icon) $($e.Title) ($($e.GetAdvisorDisplayName())) [$($e.GetSeverityLabel())]" -ForegroundColor White
}

if ($allPlayerEvents.Count -lt 1) {
    throw "Expected events to be generated across 10 turns!"
}

Write-Host "`n=============================================================" -ForegroundColor Green
Write-Host "✅ ALL TURN EVENT TICKER & ADVISOR TESTS PASSED!" -ForegroundColor Green
Write-Host "=============================================================" -ForegroundColor Green

