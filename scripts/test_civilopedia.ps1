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
Write-Host "📚 CIVILOPEDIA REPOSITORY VERIFICATION TEST" -ForegroundColor Cyan
Write-Host "=============================================================" -ForegroundColor Cyan

$allArticles = [CivGame.Core.CivilopediaRepository]::GetAll()
Write-Host "Total articles loaded: $($allArticles.Count)" -ForegroundColor Green

if ($allArticles.Count -lt 50) {
    throw "Too few articles loaded: $($allArticles.Count) (expected >= 50)"
}

# Group by category
$categories = [Enum]::GetValues([CivGame.Core.CivilopediaCategory])
foreach ($cat in $categories) {
    $count = ($allArticles | Where-Object { $_.Category -eq $cat }).Count
    Write-Host "  [$cat]: $count článkov" -ForegroundColor White
    if ($count -eq 0) {
        throw "Category $cat has 0 articles!"
    }
}

# Test Search
Write-Host "`nTesting Search:" -ForegroundColor Yellow

# 1. Search for submarine
$subResults = [CivGame.Core.CivilopediaRepository]::Search("ponorka")
Write-Host "Search 'ponorka': $($subResults.Count) result(s)"
foreach ($r in $subResults) {
    Write-Host "  -> $($r.Icon) $($r.Title) [$($r.Category) - $($r.SubCategory)]" -ForegroundColor Gray
}
if ($subResults.Count -eq 0) { throw "Search 'ponorka' returned 0 results!" }

# 2. Search for government
$govResults = [CivGame.Core.CivilopediaRepository]::Search("demokracia")
Write-Host "Search 'demokracia': $($govResults.Count) result(s)"
foreach ($r in $govResults) {
    Write-Host "  -> $($r.Icon) $($r.Title) [$($r.Category)]" -ForegroundColor Gray
}
if ($govResults.Count -eq 0) { throw "Search 'demokracia' returned 0 results!" }

# 3. Search for wonder
$wonderResults = [CivGame.Core.CivilopediaRepository]::Search("pyramídy")
Write-Host "Search 'pyramídy': $($wonderResults.Count) result(s)"
foreach ($r in $wonderResults) {
    Write-Host "  -> $($r.Icon) $($r.Title) [$($r.Category)]" -ForegroundColor Gray
}

# 4. Search with category filter
$unitResults = [CivGame.Core.CivilopediaRepository]::Search("", [CivGame.Core.CivilopediaCategory]::Units)
Write-Host "All Units count: $($unitResults.Count)" -ForegroundColor Cyan
if ($unitResults.Count -lt 15) { throw "Expected at least 15 units, got $($unitResults.Count)" }

Write-Host "`nAll Civilopedia Repository tests PASSED successfully! 🎉" -ForegroundColor Green

