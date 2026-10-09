<#
.SYNOPSIS
  Copies finished audio (.ogg from finish_audio.py) into the Unity project and writes
  a recipe per sound. Sound-effect seeds become numbered variations of one sound
  (sfx_x_s1101 -> sfx_x_01); each music job is its own track. Keep this file pure ASCII.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools/pipeline/Publish-Audio.ps1 -JobsPath stylekit/jobs/sfx-v0.json -OggDir D:\godtree-work\sfx-v0\ogg -Kind sfx
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$JobsPath,
    [Parameter(Mandatory = $true)][string]$OggDir,
    [Parameter(Mandatory = $true)][ValidateSet('sfx', 'music')][string]$Kind,
    [string]$Note = 'Not yet listened to by a person: checked by measurement only (length, level, not silent, not clipped).'
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$enc = New-Object System.Text.UTF8Encoding($false)
$jobsFile = (Resolve-Path $JobsPath).Path
$spec = [System.IO.File]::ReadAllText($jobsFile, $enc) | ConvertFrom-Json
$workflowSrc = Join-Path (Split-Path $jobsFile -Parent) $spec.workflow
$titles = (Get-Culture).TextInfo
$report = @{}
foreach ($r in ([System.IO.File]::ReadAllText((Join-Path $OggDir 'audio-report.json'), $enc) | ConvertFrom-Json)) { $report[[string]$r.file] = $r }

if ($Kind -eq 'sfx') { $generator = 'stable-audio-open'; $version = 'Stable Audio Open 1.0 (Comfy-Org repack), t5-base text encoder, ComfyUI 0.39.0'; $vram = 6; $folder = 'Sfx' }
else { $generator = 'ace-step-1.5'; $version = 'ACE-Step 1.5 turbo all-in-one (Comfy-Org repack), ComfyUI 0.39.0'; $vram = 13; $folder = 'Music' }

$groups = @{}
$order = @()
foreach ($job in @($spec.jobs)) {
    $base = [string]$job.id
    if ($Kind -eq 'sfx') { $base = $base -replace '_s\d+$', '' }
    if (-not $groups.ContainsKey($base)) { $groups[$base] = @(); $order += $base }
    $groups[$base] += $job
}

$published = 0
foreach ($base in $order) {
    $steps = @(); $outputs = @(); $measures = @(); $n = 0
    foreach ($job in $groups[$base]) {
        $src = Join-Path $OggDir ([string]$job.id + '.ogg')
        if (-not (Test-Path -LiteralPath $src)) { continue }
        $m = $report[[string]$job.id + '.ogg']
        if ($null -ne $m -and @($m.flags).Count -gt 0) { Write-Output "skipped $($job.id): $(@($m.flags) -join ',')"; continue }
        $n++
        if ($Kind -eq 'sfx') { $name = '{0}_{1:d2}.ogg' -f $base, $n } else { $name = "$base.ogg" }
        $rel = "unity/Assets/Godtree/Audio/$folder/$name"
        $dst = Join-Path $repo $rel
        [void](New-Item -ItemType Directory -Force -Path (Split-Path $dst -Parent))
        Copy-Item -LiteralPath $src -Destination $dst -Force
        $steps += [ordered]@{ generator = $generator; tool = 'comfyui'; version = $version; prompt = [string]$job.prompt; seed = [long]$job.seed; vram_gb = $vram; workflow = 'workflow.json' }
        $outputs += [ordered]@{ path = $rel }
        if ($null -ne $m) { $measures += ("{0}: {1} s, {2} ch, {3} KB" -f $name, $m.seconds, $m.channels, $m.kb) }
    }
    if ($n -eq 0) { continue }
    $recipeDir = Join-Path $repo "recipes/$base"
    [void](New-Item -ItemType Directory -Force -Path $recipeDir)
    Copy-Item -LiteralPath $workflowSrc -Destination (Join-Path $recipeDir 'workflow.json') -Force
    $plain = (($base -split '_', 2)[1]) -replace '_', ' '
    $extra = ''
    if ($Kind -eq 'music') { $j = $groups[$base][0]; $extra = " Requested: $($j.seconds) s, $($j.bpm) bpm, $($j.keyscale), time signature $($j.timesignature), instrumental." }
    $recipe = [ordered]@{
        id      = $base
        title   = $titles.ToTitleCase($plain)
        origin  = 'generated'
        licence = 'CC0-1.0'
        steps   = $steps
        cleanup = @('Automatic only, by tools/pipeline/finish_audio.py: resampled to 48 kHz, level set by peak, short fades, encoded as Ogg Vorbis; sound effects also mixed to mono and trimmed of silence.', 'No hand editing.')
        sources = @()
        outputs = $outputs
        notes   = ("Each step is one seed and produced one output file, in order.$extra Measured: " + ($measures -join '; ') + ". $Note").Trim()
    }
    [System.IO.File]::WriteAllText((Join-Path $recipeDir 'recipe.json'), ((($recipe | ConvertTo-Json -Depth 6) -replace "`r`n", "`n") + "`n"), $enc)
    $published += $n
}
Write-Output "published $published $Kind file(s) in $($order.Count) recipe(s)"
