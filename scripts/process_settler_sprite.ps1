Add-Type -AssemblyName System.Drawing

$srcPath = "C:\Users\MichalKolodziej\.gemini\antigravity-ide\brain\5b44b100-6a62-4c4c-a8a3-21b27750371f\.user_uploaded\media_1792615633077.png"
$bmp = [System.Drawing.Bitmap]::FromFile($srcPath)
$w = $bmp.Width
$h = $bmp.Height

# 1. Create a boolean map for background vs figure
# True = background (transparent), False = figure
$isBg = New-Object 'bool[,]' $w, $h

function IsCheckerboard($px) {
    $diffRG = [Math]::Abs($px.R - $px.G)
    $diffGB = [Math]::Abs($px.G - $px.B)
    $diffRB = [Math]::Abs($px.R - $px.B)
    $maxDiff = [Math]::Max($diffRG, [Math]::Max($diffGB, $diffRB))
    
    # White checker cell (250-255)
    if ($px.R -ge 240 -and $px.G -ge 240 -and $px.B -ge 240 -and $maxDiff -le 8) { return $true }
    # Gray checker cell (180-215 with low saturation)
    if ($px.R -ge 175 -and $px.G -ge 175 -and $px.B -ge 175 -and $maxDiff -le 8) { return $true }
    # Gray transitions (170-255 with maxDiff <= 5)
    if ($px.R -ge 168 -and $maxDiff -le 5) { return $true }
    
    return $false
}

# BFS Queue to flood-fill background from edges
$queue = New-Object System.Collections.Generic.Queue[System.Drawing.Point]

# Enqueue top and bottom rows
for ($x = 0; $x -lt $w; $x++) {
    $pTop = $bmp.GetPixel($x, 0)
    if (IsCheckerboard $pTop) {
        $isBg[$x, 0] = $true
        $queue.Enqueue((New-Object System.Drawing.Point($x, 0)))
    }
    $pBot = $bmp.GetPixel($x, $h - 1)
    if (IsCheckerboard $pBot) {
        $isBg[$x, $h - 1] = $true
        $queue.Enqueue((New-Object System.Drawing.Point($x, $h - 1)))
    }
}

# Enqueue left and right columns
for ($y = 0; $y -lt $h; $y++) {
    $pLeft = $bmp.GetPixel(0, $y)
    if (IsCheckerboard $pLeft) {
        $isBg[0, $y] = $true
        $queue.Enqueue((New-Object System.Drawing.Point(0, $y)))
    }
    $pRight = $bmp.GetPixel($w - 1, $y)
    if (IsCheckerboard $pRight) {
        $isBg[$w - 1, $y] = $true
        $queue.Enqueue((New-Object System.Drawing.Point($w - 1, $y)))
    }
}

$dxs = @(0, 0, 1, -1)
$dys = @(1, -1, 0, 0)

while ($queue.Count -gt 0) {
    $pt = $queue.Dequeue()
    $currX = $pt.X
    $currY = $pt.Y

    for ($i = 0; $i -lt 4; $i++) {
        $nx = $currX + $dxs[$i]
        $ny = $currY + $dys[$i]

        if ($nx -ge 0 -and $nx -lt $w -and $ny -ge 0 -and $ny -lt $h) {
            if (-not $isBg[$nx, $ny]) {
                $npx = $bmp.GetPixel($nx, $ny)
                if (IsCheckerboard $npx) {
                    $isBg[$nx, $ny] = $true
                    $queue.Enqueue((New-Object System.Drawing.Point($nx, $ny)))
                }
            }
        }
    }
}

# Find figure bounding box from remaining non-bg pixels
$minX = $w; $maxX = 0
$minY = $h; $maxY = 0

for ($y = 0; $y -lt $h; $y++) {
    for ($x = 0; $x -lt $w; $x++) {
        if (-not $isBg[$x, $y]) {
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }
}

# Add small 4px margin
$pad = 4
$cropX = [Math]::Max(0, $minX - $pad)
$cropY = [Math]::Max(0, $minY - $pad)
$cropW = [Math]::Min($w - $cropX, ($maxX - $minX + 1) + $pad * 2)
$cropH = [Math]::Min($h - $cropY, ($maxY - $minY + 1) + $pad * 2)

Write-Host "Cropping: X=$cropX, Y=$cropY, W=$cropW, H=$cropH"

# Create output image with transparency
$outBmp = New-Object System.Drawing.Bitmap($cropW, $cropH, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

for ($cy = 0; $cy -lt $cropH; $cy++) {
    for ($cx = 0; $cx -lt $cropW; $cx++) {
        $srcX = $cropX + $cx
        $srcY = $cropY + $cy

        if ($srcX -ge 0 -and $srcX -lt $w -and $srcY -ge 0 -and $srcY -lt $h) {
            if ($isBg[$srcX, $srcY]) {
                $outBmp.SetPixel($cx, $cy, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            } else {
                $origPx = $bmp.GetPixel($srcX, $srcY)
                $outBmp.SetPixel($cx, $cy, [System.Drawing.Color]::FromArgb(255, $origPx.R, $origPx.G, $origPx.B))
            }
        }
    }
}

# Save full-res transparent PNG
$fullOut = "c:\_private\civ\assets\settler_caravan_clean.png"
$outBmp.Save($fullOut, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Saved clean full-res to $fullOut"

# Now create high-quality scaled versions for game rendering:
# Target game height ~72px, width proportionally ~124px
$targetHeight = 72
$targetWidth = [int][Math]::Round($cropW * ($targetHeight / $cropH))

$scaledBmp = New-Object System.Drawing.Bitmap($targetWidth, $targetHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($scaledBmp)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.Clear([System.Drawing.Color]::Transparent)
$g.DrawImage($outBmp, 0, 0, $targetWidth, $targetHeight)
$g.Dispose()

# Save scaled PNG for settler.png
$gameOut = "c:\_private\civ\assets\settler.png"
$scaledBmp.Save($gameOut, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Saved game sprite ($targetWidth x $targetHeight) to $gameOut"

# Also save webp equivalent / backup
$scaledBmp.Save("c:\_private\civ\assets\settler_orig.png", [System.Drawing.Imaging.ImageFormat]::Png)

$scaledBmp.Dispose()
$outBmp.Dispose()
$bmp.Dispose()
Write-Host "Processing complete successfully!"
