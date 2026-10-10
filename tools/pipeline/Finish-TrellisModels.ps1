<#
.SYNOPSIS
  Blender finishing for TRELLIS.2 queue output: reduce each raw .glb to its triangle
  budget, re-bake the texture, scale, export and render a preview. Resumable. A model
  that cannot be reduced to its budget is reported and left out of model-log.jsonl, so
  Publish-Models.ps1 cannot publish it. Keep this file pure ASCII.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools/pipeline/Finish-TrellisModels.ps1 -JobsPath stylekit/jobs/trellis2-v0.json -RawDir D:\godtree-work\trellis2-v0 -OutDir D:\godtree-work\trellis2-v0\finished
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$JobsPath,
    [Parameter(Mandatory = $true)][string]$RawDir,
    [Parameter(Mandatory = $true)][string]$OutDir,
    [string]$Blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$spec = [System.IO.File]::ReadAllText((Resolve-Path $JobsPath).Path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$finish = (Resolve-Path (Join-Path $PSScriptRoot 'blender_finish_model.py')).Path
[void](New-Item -ItemType Directory -Force -Path $OutDir)
$logPath = Join-Path $OutDir 'model-log.jsonl'
$logged = @{}
if (Test-Path -LiteralPath $logPath) {
    foreach ($line in [System.IO.File]::ReadAllLines($logPath)) { if ($line.Trim() -ne '') { $logged[[string]($line | ConvertFrom-Json).id] = $true } }
}

$made = 0; $over = 0; $waiting = 0
foreach ($job in @($spec.jobs)) {
    $id = [string]$job.id
    if ($logged.ContainsKey($id)) { continue }
    $raw = Join-Path $RawDir ($id + '.glb')
    if (-not (Test-Path -LiteralPath $raw)) { $waiting++; continue }
    $glb = Join-Path $OutDir ($id + '.glb')
    $png = Join-Path $OutDir ($id + '_preview.png')
    $bLog = Join-Path $OutDir ($id + '.blender.log')
    cmd /c "`"$Blender`" -b --factory-startup -P `"$finish`" -- `"$raw`" `"$glb`" `"$png`" --rotx 0 --tris $($job.tris) --tex $($job.tex) --size $($job.size) > `"$bLog`" 2>&1"
    $m = Select-String -LiteralPath $bLog -Pattern 'GODTREE_RESULT (.*final_tris=(\d+).*)$' | Select-Object -First 1
    if ($null -eq $m) { Write-Output "FAIL  $id  see $bLog"; continue }
    $final = [int]$m.Matches[0].Groups[2].Value
    if ($final -gt [int]([double]$job.tris * 1.02)) {
        $over++
        Write-Output "OVER  $id  $final triangles, budget target $($job.tris): not publishable"
        continue
    }
    $rec = New-Object psobject -Property @{ id = $id; image = [string]$job.image; budget_class = [string]$job.budget_class; result = $m.Matches[0].Groups[1].Value; time = (Get-Date).ToString('s') }
    Add-Content -LiteralPath $logPath -Value ($rec | ConvertTo-Json -Compress) -Encoding UTF8
    $made++
    Write-Output "done  $id  $($m.Matches[0].Groups[1].Value)"
}
Write-Output "Finishing: $made within budget, $over over budget, $waiting not generated yet."
