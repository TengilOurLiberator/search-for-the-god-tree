<#
.SYNOPSIS
  Expands a subjects file into a jobs file for Invoke-GenerationQueue.ps1.
  Prompt = subject + world fragments + framing fragment + style fragment.
  Keep this file pure ASCII.
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools/pipeline/New-ConceptJobs.ps1 -SubjectsPath stylekit/jobs/concepts-v0.subjects.json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$SubjectsPath,
    [string]$FragmentsPath,
    [string]$OutPath
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$subjectsFile = (Resolve-Path $SubjectsPath).Path
if (-not $FragmentsPath) { $FragmentsPath = Join-Path $PSScriptRoot '../../stylekit/prompt-fragments.json' }
if (-not $OutPath) { $OutPath = $subjectsFile -replace '\.subjects\.json$', '.json' }

$enc = New-Object System.Text.UTF8Encoding($false)
$spec = [System.IO.File]::ReadAllText($subjectsFile, $enc) | ConvertFrom-Json
$frag = [System.IO.File]::ReadAllText((Resolve-Path $FragmentsPath).Path, $enc) | ConvertFrom-Json

$jobs = @()
foreach ($s in @($spec.subjects)) {
    $parts = @([string]$s.subject)
    foreach ($w in @($s.world)) {
        if ($null -eq $w) { continue }
        $parts += [string]$frag.world.PSObject.Properties[[string]$w].Value
    }
    $parts += [string]$frag.framing.PSObject.Properties[[string]$s.framing].Value
    $isObject = @('object_for_3d', 'creature_for_3d') -contains [string]$s.framing
    $isScene = @('landscape', 'interior') -contains [string]$s.framing
    if ($isObject -and $null -ne $frag.PSObject.Properties['style_object']) { $parts += [string]$frag.style_object }
    elseif ($isScene -and $null -ne $frag.PSObject.Properties['style_scene']) { $parts += [string]$frag.style_scene }
    else { $parts += [string]$frag.style }
    $prompt = ($parts -join ' ')
    foreach ($seed in @($spec.seeds)) {
        $jobs += New-Object psobject -Property ([ordered]@{ id = ("{0}_s{1}" -f $s.id, $seed); prompt = $prompt; seed = [long]$seed; width = [int]$s.width; height = [int]$s.height })
    }
}
$out = New-Object psobject -Property ([ordered]@{ workflow = [string]$spec.workflow; fragments_version = $frag.version; jobs = $jobs })
[System.IO.File]::WriteAllText($OutPath, (($out | ConvertTo-Json -Depth 5) -replace "`r`n", "`n") + "`n", $enc)
Write-Output "Wrote $($jobs.Count) jobs to $OutPath"
