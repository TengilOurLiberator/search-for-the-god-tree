<#
.SYNOPSIS
  Tiles the images in a folder into labelled contact sheets (JPEG) for quick review.
  Windows only (uses System.Drawing). Keep this file pure ASCII.
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools/pipeline/New-ContactSheet.ps1 -InDir D:\godtree-work\concepts-v0 -Filter "con_*" -Columns 4
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$InDir,
    [string]$Filter = '*',
    [string]$OutDir,
    [int]$Columns = 4,
    [int]$Rows = 3,
    [int]$Cell = 384
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

if (-not $OutDir) { $OutDir = Join-Path $InDir 'sheets' }
[void](New-Item -ItemType Directory -Force -Path $OutDir)
$files = @(Get-ChildItem -LiteralPath $InDir -Filter ($Filter + '.png') -File | Sort-Object Name)
if ($files.Count -eq 0) { Write-Output 'No images.'; exit 0 }

$perSheet = $Columns * $Rows
$label = 22
$font = New-Object System.Drawing.Font('Consolas', 9)
$sheet = 0
for ($start = 0; $start -lt $files.Count; $start += $perSheet) {
    $sheet++
    $batch = @($files[$start..([math]::Min($start + $perSheet, $files.Count) - 1)])
    $rowsUsed = [int][math]::Ceiling($batch.Count / $Columns)
    $bmp = New-Object System.Drawing.Bitmap(($Columns * $Cell), ($rowsUsed * ($Cell + $label)))
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(34, 34, 59))
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    for ($i = 0; $i -lt $batch.Count; $i++) {
        $img = [System.Drawing.Image]::FromFile($batch[$i].FullName)
        try {
            $scale = [math]::Min($Cell / $img.Width, $Cell / $img.Height)
            $w = [int]($img.Width * $scale); $h = [int]($img.Height * $scale)
            $x = ($i % $Columns) * $Cell + [int](($Cell - $w) / 2)
            $y = [int][math]::Floor($i / $Columns) * ($Cell + $label) + [int](($Cell - $h) / 2)
            $g.DrawImage($img, $x, $y, $w, $h)
        } finally { $img.Dispose() }
        $name = $batch[$i].BaseName -replace '^con_', ''
        $g.DrawString($name, $font, [System.Drawing.Brushes]::White, [single](($i % $Columns) * $Cell + 4), [single]([math]::Floor($i / $Columns) * ($Cell + $label) + $Cell + 3))
    }
    $g.Dispose()
    $out = Join-Path $OutDir ("sheet_{0:d2}.jpg" -f $sheet)
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    $bmp.Dispose()
    Write-Output $out
}
