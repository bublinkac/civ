Add-Type -AssemblyName System.Drawing

$srcPath = "C:\Users\MichalKolodziej\.gemini\antigravity-ide\brain\5b44b100-6a62-4c4c-a8a3-21b27750371f\.user_uploaded\media_1792615633077.png"
$bmp = [System.Drawing.Bitmap]::FromFile($srcPath)

function IsFigurePixel($px) {
    # If it's saturated (colored, not gray checkerboard)
    $diffRG = [Math]::Abs($px.R - $px.G)
    $diffGB = [Math]::Abs($px.G - $px.B)
    $diffRB = [Math]::Abs($px.R - $px.B)
    $maxDiff = [Math]::Max($diffRG, [Math]::Max($diffGB, $diffRB))
    
    if ($maxDiff -ge 12) { return $true }
    
    # Or if it is significantly darker than the checkerboard gray (197)
    if ($px.R -lt 165 -or $px.G -lt 165 -or $px.B -lt 165) { return $true }
    
    return $false
}

$minX = $bmp.Width; $maxX = 0
$minY = $bmp.Height; $maxY = 0
$figurePixelCount = 0

for ($y = 0; $y -lt $bmp.Height; $y++) {
    for ($x = 0; $x -lt $bmp.Width; $x++) {
        $px = $bmp.GetPixel($x, $y)
        if (IsFigurePixel $px) {
            $figurePixelCount++
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }
}

Write-Host "Real Figure Pixels: $figurePixelCount"
Write-Host "Real Figure BBox: X=[$minX .. $maxX], Y=[$minY .. $maxY]"
Write-Host "Figure Width = $($maxX - $minX + 1), Figure Height = $($maxY - $minY + 1)"

$bmp.Dispose()
