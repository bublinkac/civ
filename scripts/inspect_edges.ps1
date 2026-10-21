Add-Type -AssemblyName System.Drawing

$srcPath = "C:\Users\MichalKolodziej\.gemini\antigravity-ide\brain\5b44b100-6a62-4c4c-a8a3-21b27750371f\.user_uploaded\media_1792615633077.png"
$bmp = [System.Drawing.Bitmap]::FromFile($srcPath)

for ($y = 0; $y -lt $bmp.Height; $y += 20) {
    $p0 = $bmp.GetPixel(0, $y)
    $pEnd = $bmp.GetPixel($bmp.Width - 1, $y)
    Write-Host "y=$y Left: ($($p0.R),$($p0.G),$($p0.B)) Right: ($($pEnd.R),$($pEnd.G),$($pEnd.B))"
}

$bmp.Dispose()
