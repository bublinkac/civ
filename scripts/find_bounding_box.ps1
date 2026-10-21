Add-Type -AssemblyName System.Drawing

$srcPath = "C:\Users\MichalKolodziej\.gemini\antigravity-ide\brain\5b44b100-6a62-4c4c-a8a3-21b27750371f\.user_uploaded\media_1792615633077.png"
$bmp = [System.Drawing.Bitmap]::FromFile($srcPath)

function IsCheckerboardPixel($px) {
    $diffRG = [Math]::Abs($px.R - $px.G)
    $diffGB = [Math]::Abs($px.G - $px.B)
    $diffRB = [Math]::Abs($px.R - $px.B)
    $maxDiff = [Math]::Max($diffRG, [Math]::Max($diffGB, $diffRB))
    
    # Near pure white cell
    if ($px.R -ge 245 -and $px.G -ge 245 -and $px.B -ge 245 -and $maxDiff -le 6) { return $true }
    # Gray cell or transition between white and gray (190 to 255 with almost zero saturation)
    if ($px.R -ge 185 -and $px.R -le 255 -and $maxDiff -le 5) { return $true }
    
    return $false
}

$minX = $bmp.Width; $maxX = 0
$minY = $bmp.Height; $maxY = 0

for ($y = 0; $y -lt $bmp.Height; $y++) {
    for ($x = 0; $x -lt $bmp.Width; $x++) {
        $px = $bmp.GetPixel($x, $y)
        if (-not (IsCheckerboardPixel $px)) {
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }
}

Write-Host "Figure Bounding Box: X=[$minX .. $maxX], Y=[$minY .. $maxY]"
Write-Host "Width = $($maxX - $minX + 1), Height = $($maxY - $minY + 1)"

$bmp.Dispose()
