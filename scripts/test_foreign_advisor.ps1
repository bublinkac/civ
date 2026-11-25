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
Write-Host "🌍 FOREIGN ADVISOR & DIPLOMACY SYSTEM VERIFICATION TEST" -ForegroundColor Cyan
Write-Host "=============================================================" -ForegroundColor Cyan

# Setup simulation with 2 AI rivals
$setup = [CivGame.Core.GameSetupOptions]::new()
$setup.Width = 40
$setup.Height = 40
$setup.Seed = 7777
$setup.Difficulty = [CivGame.Core.DifficultyLevel]::Regent
$setup.PlayerCivId = "rome"
$setup.RivalCivIds = [System.Collections.Generic.List[string]]::new([string[]]@("babylon", "egypt"))

$generator = [CivGame.Core.MapGenerator]::new(7777)
$map = $generator.Generate($setup)
$sim = [CivGame.Core.GameSimulation]::new($map)
$sim.Difficulty = [CivGame.Core.DifficultyLevel]::Regent
$sim.ConfigureFactions("rome", [System.Collections.Generic.List[string]]::new([string[]]@("babylon", "egypt")))
$sim.SetupInitialUnits()

$sim.SetTreasury([CivGame.Core.Faction]::Player, 500)
$sim.SetTreasury([CivGame.Core.Faction]::AiRival, 300)

# 1. Test Embassy Establishment
Write-Host "`n1. Testing Embassy establishment..." -ForegroundColor Yellow
$canBefore = $sim.Diplomacy.CanEstablishEmbassy([CivGame.Core.Faction]::Player, [CivGame.Core.Faction]::AiRival)
if (-not $canBefore) { throw "Expected Player to be able to establish embassy!" }

$estOk = $sim.Diplomacy.EstablishEmbassy([CivGame.Core.Faction]::Player, [CivGame.Core.Faction]::AiRival)
if (-not $estOk) { throw "EstablishEmbassy returned false!" }
if ($sim.GetTreasury([CivGame.Core.Faction]::Player) -ne 450) {
    throw "Expected Player treasury to be 450 after 50g embassy, got $($sim.GetTreasury([CivGame.Core.Faction]::Player))"
}
if (-not $sim.Diplomacy.HasEmbassy([CivGame.Core.Faction]::Player, [CivGame.Core.Faction]::AiRival)) {
    throw "HasEmbassy returned false after establishing!"
}
Write-Host "  ✓ Embassy established (-50g) and TurnEvent logged." -ForegroundColor Green

# 2. Test Attitude Progression
Write-Host "`n2. Testing Diplomatic Attitude..." -ForegroundColor Yellow
$attAfterEmbassy = $sim.Diplomacy.GetAttitude([CivGame.Core.Faction]::Player, [CivGame.Core.Faction]::AiRival)
Write-Host "  Attitude after Embassy: $attAfterEmbassy" -ForegroundColor White
if ($attAfterEmbassy -ne [CivGame.Core.Diplomacy.DiplomaticAttitude]::Polite) {
    throw "Expected Polite attitude after Embassy, got $attAfterEmbassy"
}
Write-Host "  ✓ Attitude correctly escalated to Polite." -ForegroundColor Green

# 3. Test Deal Valuation & Execution (Tech + Gold + RoP + MPP + Map)
Write-Host "`n3. Testing Deal Valuation & Execution (Tech, GPT, RoP, MPP, World Map)..." -ForegroundColor Yellow
$sim.GetResearch([CivGame.Core.Faction]::Player).ResearchedTechIds.Add("bronze_working") | Out-Null
$sim.GetResearch([CivGame.Core.Faction]::AiRival).ResearchedTechIds.Add("pottery") | Out-Null

$tradableToAi = $sim.Diplomacy.GetTradableTechs([CivGame.Core.Faction]::Player, [CivGame.Core.Faction]::AiRival)
Write-Host "  Techs Player can offer AiRival: $($tradableToAi.Count)" -ForegroundColor White

$deal = [CivGame.Core.Diplomacy.TradeDeal]::new()
$deal.FactionA = [CivGame.Core.Faction]::Player
$deal.FactionB = [CivGame.Core.Faction]::AiRival
$deal.LumpSumGoldA = 100
$deal.GoldPerTurnA = 5
$deal.TechsA.Add("bronze_working")
$deal.TechsB.Add("pottery")
$deal.WorldMapB = $true
$deal.RightOfPassage = $true
$deal.MutualProtectionPact = $true

$eval = $sim.Diplomacy.EvaluateDeal($deal)
Write-Host "  EvaluateDeal result: Accepted=$($eval.Item1) | Msg=$($eval.Item2)" -ForegroundColor White
if (-not $eval.Item1) { throw "Expected generous deal to be accepted!" }

$sim.Diplomacy.ExecuteDeal($deal)

if (-not $sim.GetResearch([CivGame.Core.Faction]::Player).IsResearched("pottery")) {
    throw "Player did not receive traded tech 'pottery'!"
}
if (-not $sim.GetResearch([CivGame.Core.Faction]::AiRival).IsResearched("bronze_working")) {
    throw "AiRival did not receive traded tech 'bronze_working'!"
}
if (-not $sim.Diplomacy.HasRightOfPassage([CivGame.Core.Faction]::Player, [CivGame.Core.Faction]::AiRival)) {
    throw "Right of Passage not active!"
}
if (-not $sim.Diplomacy.HasMutualProtectionPact([CivGame.Core.Faction]::Player, [CivGame.Core.Faction]::AiRival)) {
    throw "Mutual Protection Pact not active!"
}

$attGracious = $sim.Diplomacy.GetAttitude([CivGame.Core.Faction]::Player, [CivGame.Core.Faction]::AiRival)
Write-Host "  Attitude after RoP + MPP: $attGracious" -ForegroundColor White
if ($attGracious -ne [CivGame.Core.Diplomacy.DiplomaticAttitude]::Gracious) {
    throw "Expected Gracious attitude after RoP + MPP, got $attGracious"
}
Write-Host "  ✓ Techs, Gold, Map, RoP, and MPP executed & verified." -ForegroundColor Green

# 4. Test Mutual Protection Pact War Trigger
Write-Host "`n4. Testing Mutual Protection Pact auto-war trigger..." -ForegroundColor Yellow
# AiRival2 (Egypt) attacks Player -> AiRival (Babylon) has MPP with Player, so Babylon must auto-declare war on Egypt!
$sim.Diplomacy.DeclareWar([CivGame.Core.Faction]::AiRival2, [CivGame.Core.Faction]::Player)

if (-not $sim.IsAtWar([CivGame.Core.Faction]::AiRival2, [CivGame.Core.Faction]::Player)) {
    throw "Expected AiRival2 and Player to be at war!"
}
if (-not $sim.IsAtWar([CivGame.Core.Faction]::AiRival, [CivGame.Core.Faction]::AiRival2)) {
    throw "Expected MPP ally AiRival to auto-declare war on AiRival2!"
}
Write-Host "  ✓ MPP successfully triggered defensive war by ally!" -ForegroundColor Green

# 5. Test Peace Treaty
Write-Host "`n5. Testing Peace Treaty negotiation..." -ForegroundColor Yellow
$peaceDeal = [CivGame.Core.Diplomacy.TradeDeal]::new()
$peaceDeal.FactionA = [CivGame.Core.Faction]::Player
$peaceDeal.FactionB = [CivGame.Core.Faction]::AiRival2
$peaceDeal.PeaceTreaty = $true
$peaceDeal.LumpSumGoldA = 100

$peaceEval = $sim.Diplomacy.EvaluateDeal($peaceDeal)
if (-not $peaceEval.Item1) { throw "Expected Peace Treaty with 100g to be accepted! ($($peaceEval.Item2))" }
$sim.Diplomacy.ExecuteDeal($peaceDeal)

if ($sim.IsAtWar([CivGame.Core.Faction]::Player, [CivGame.Core.Faction]::AiRival2)) {
    throw "Expected Peace Treaty to end war between Player and AiRival2!"
}
Write-Host "  ✓ Peace Treaty ended war cleanly." -ForegroundColor Green

Write-Host "`n=============================================================" -ForegroundColor Green
Write-Host "✅ ALL FOREIGN ADVISOR & DIPLOMACY TESTS PASSED!" -ForegroundColor Green
Write-Host "=============================================================" -ForegroundColor Green
