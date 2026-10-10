<#
.SYNOPSIS
  Takes man-shaped characters from concept image to a rigged, published model in one run:
  TRELLIS.2 (ComfyUI queue), Blender finishing, binding to the CC0 Mesh2Motion human skeleton
  (blender_rig_body.py --style m2m --mode skinned), publishing with a recipe, and installing the
  rigged file in place of the unrigged one. Runs unattended; safe to re-run (finished steps are
  skipped by the tools it calls). Keep this file pure ASCII.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools/pipeline/Invoke-RiggedCharacters.ps1 -JobsPath stylekit/jobs/trellis2-f1-rigged.json -ConceptJobs stylekit/jobs/concepts-f1-rigged.json -WorkDir D:\godtree-work\trellis2-f1-rigged
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$JobsPath,
    [Parameter(Mandatory = $true)][string]$ConceptJobs,
    [Parameter(Mandatory = $true)][string]$WorkDir,
    [string]$ConceptSeeds = 'stylekit/golden/f1/README.md',
    [string]$Skeleton = 'D:\godtree-tools\downloads\mesh2motion\human-base-animations.glb',
    [string]$SkeletonSource = 'https://github.com/Mesh2Motion/mesh2motion-app',
    [string]$Blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$enc = New-Object System.Text.UTF8Encoding($false)
$jobsFull = (Resolve-Path $JobsPath).Path
$jobs = ([System.IO.File]::ReadAllText($jobsFull, $enc) | ConvertFrom-Json).jobs
$finished = Join-Path $WorkDir 'finished'
$rigged = Join-Path $WorkDir 'rigged'
New-Item -ItemType Directory -Force $WorkDir, $rigged | Out-Null

Write-Output "== 1. image to 3D"
# The queue starts the generator's server, which then holds this script's output open for as long
# as it lives. So the queue runs as a process of its own, logging to a file, and only it is waited for.
$missing = @($jobs | Where-Object { -not (Test-Path (Join-Path $WorkDir ("{0}.glb" -f $_.id))) })
if ($missing.Count -gt 0) {
$queueLog = Join-Path $WorkDir 'queue.log'
$queue = Start-Process powershell -ArgumentList '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $PSScriptRoot 'Invoke-GenerationQueue.ps1'), '-JobsPath', $jobsFull, '-OutDir', $WorkDir -WindowStyle Hidden -RedirectStandardOutput $queueLog -PassThru
$queue.WaitForExit()
Get-Content $queueLog -Tail 2
} else { Write-Output 'all models already generated' }

# The generator is finished with the graphics card; stop it so later steps and the game have the memory.
Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match 'ComfyUI' -and $_.Name -match 'python' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -Confirm:$false }

Write-Output "== 2. finishing"
& powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Finish-TrellisModels.ps1') -JobsPath $jobsFull -RawDir $WorkDir -OutDir $finished | Select-Object -Last 2

Write-Output "== 3. rigging"
$results = @{}
foreach ($job in $jobs) {
    $id = [string]$job.id
    $body = Join-Path $finished "$id.glb"
    if (-not (Test-Path $body)) { Write-Output "skip  $id  (no finished model)"; continue }
    # Creatures are cut into parts so limbs can come off; people are not.
    $extra = @(); if ($id.StartsWith('enm_')) { $extra = @('--parts') }
    # Blender prints harmless warnings on its error stream; they must not stop the run.
    $ErrorActionPreference = 'Continue'
    $out = & $Blender -b --python (Join-Path $PSScriptRoot 'blender_rig_body.py') -- --skeleton $Skeleton --style m2m --mode skinned --body $body --out $rigged --name $id @extra 2>$null
    $ErrorActionPreference = 'Stop'
    $line = $out | Where-Object { "$_" -like 'GODTREE_RESULT *' } | Select-Object -Last 1
    if ($null -eq $line) { Write-Output "FAIL  $id  rigging gave no result"; ($out | Select-Object -Last 6) | ForEach-Object { Write-Output "      $_" }; continue }
    $r = ("$line".Substring(15)) | ConvertFrom-Json
    $results[$id] = $r
    Write-Output ("rig   {0}  arms {1} deg  legs {2} deg  stretch {3}  unbound {4}" -f $id, ($r.arm_angle_deg -join '/'), ($r.leg_angle_deg -join '/'), ($r.arm_stretch -join '/'), $r.unbound_vertices)
}

Write-Output "== 4. publishing"
$folders = @{ chr = 'Characters'; enm = 'Enemies' }
foreach ($id in @($results.Keys | Sort-Object)) {
    $r = $results[$id]
    & powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Publish-Models.ps1') -ModelJobs $jobsFull -ConceptJobs $ConceptJobs -ConceptSeeds $ConceptSeeds -WorkDir $finished -Ids $id -Trellis | Select-Object -Last 1
    $folder = $folders[$id.Substring(0, 3)]
    Copy-Item (Join-Path $rigged "$id.glb") (Join-Path $repo "unity/Assets/Godtree/Art/$folder/$id.glb") -Force
    Copy-Item (Join-Path $rigged "${id}_check_pose.png") (Join-Path $repo "recipes/$id/preview.png") -Force

    $recipePath = Join-Path $repo "recipes/$id/recipe.json"
    $recipe = [System.IO.File]::ReadAllText($recipePath, $enc) | ConvertFrom-Json
    $recipe.origin = 'mixed'
    $recipe.steps += [pscustomobject][ordered]@{ generator = 'blender'; version = 'Blender 5.2.2 LTS, tools/pipeline/blender_rig_body.py --style m2m --mode skinned'; prompt = 'n/a'; seed = 0; vram_gb = 0 }
    $rigNote = "Automatic only, by tools/pipeline/blender_rig_body.py: seams welded; scaled to the skeleton's height; arms raised level and legs brought together to a T-pose (measured: arms $($r.arm_angle_deg[0]) degrees below level, legs $($r.leg_angle_deg[0]) degrees out); arms scaled $($r.arm_stretch[0]) along their length; automatic weights to the skeleton's body bones, fingers excluded."
    $recipe.cleanup = @($recipe.cleanup[0], $rigNote, 'No hand editing.')
    $recipe.sources = @([pscustomobject][ordered]@{ name = 'Mesh2Motion human rig (skeleton only)'; url = $SkeletonSource; author = 'Mesh2Motion contributors'; licence = 'CC0-1.0'; note = 'The skeleton inside this file. Its animations are in anm_human_basic.' })
    $recipe.notes = [string]$recipe.notes + ' Rigged: the file holds the mesh bound to the CC0 Mesh2Motion human skeleton, so the clips in anm_human_basic drive it. Fingers do not bend. Judged from rendered test poses only; not yet seen in the headset.'
    [System.IO.File]::WriteAllText($recipePath, ($recipe | ConvertTo-Json -Depth 8), $enc)
    Write-Output "done  $id"
}
Write-Output ("Rigged characters: {0} of {1} published." -f $results.Count, @($jobs).Count)
