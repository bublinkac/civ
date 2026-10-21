Add-Type -AssemblyName System.Drawing
$b = [System.Drawing.Bitmap]::FromFile('c:\_private\civ\assets\settler.png')
Write-Host "Size: $($b.Width)x$($b.Height)"
$p = $b.GetPixel(0, 0)
Write-Host "Pixel(0,0) Alpha: $($p.A) R: $($p.R) G: $($p.G) B: $($p.B)"
$b.Dispose()
