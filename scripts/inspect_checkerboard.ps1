Add-Type -AssemblyName System.Drawing

$srcPath = "C:\Users\MichalKolodziej\.gemini\antigravity-ide\brain\5b44b100-6a62-4c4c-a8a3-21b27750371f\.user_uploaded\media_1792615633077.png"
$bmp = [System.Drawing.Bitmap]::FromFile($srcPath)

Write-Host "Width: $($bmp.Width), Height: $($bmp.Height)"

# Sample top-left corner 32x32 to see the exact pattern of the checkerboard
for ($y = 0; $y -lt 32; $y += 4) {
    $line = "y=$y : "
    for ($x = 0; $x -lt 32; $x += 4) {
        $px = $bmp.GetPixel($x, $y)
        $line += "($($px.R),$($px.G),$($px.B)) "
    }
    Write-Host $line
}

$bmp.Dispose()
