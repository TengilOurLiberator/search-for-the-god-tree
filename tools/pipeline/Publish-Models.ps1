<#
.SYNOPSIS
  Copies chosen models from a model-queue output folder into the Unity project and
  writes a recipe for each (recipe.json, workflow.json, preview.png), taking the
  prompt and seed from the concept job that produced the input image.
  Keep this file pure ASCII.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools/pipeline/Publish-Models.ps1 -ModelJobs stylekit/jobs/models-v0.json -ConceptJobs stylekit/jobs/concepts-v0.json -ConceptSeeds stylekit/golden/concepts/README.md -WorkDir D:\godtree-work\models-v0 -Ids wpn_sword_training,prp_lantern_husk -Note "Placeholder grade."
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ModelJobs,
    [Parameter(Mandatory = $true)][string]$ConceptJobs,
    [Parameter(Mandatory = $true)][string]$ConceptSeeds,
    [Parameter(Mandatory = $true)][string]$WorkDir,
    [Parameter(Mandatory = $true)][string[]]$Ids,
    [string]$Note = '',
    # The models came from TRELLIS.2 (job file in the trellis2 format) instead of TripoSR.
    [switch]$Trellis
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Ids = @($Ids | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$enc = New-Object System.Text.UTF8Encoding($false)
$folders = @{ wpn = 'Weapons'; prp = 'Props'; env = 'Environment'; enm = 'Enemies'; chr = 'Characters' }
$titles = (Get-Culture).TextInfo

$models = [System.IO.File]::ReadAllText((Resolve-Path $ModelJobs).Path, $enc) | ConvertFrom-Json
$concepts = [System.IO.File]::ReadAllText((Resolve-Path $ConceptJobs).Path, $enc) | ConvertFrom-Json
$workflowSrc = Join-Path (Split-Path (Resolve-Path $ConceptJobs).Path -Parent) $concepts.workflow
$seedTable = [System.IO.File]::ReadAllLines((Resolve-Path $ConceptSeeds).Path)
$log = @{}
foreach ($line in [System.IO.File]::ReadAllLines((Join-Path $WorkDir 'model-log.jsonl'))) {
    if ($line.Trim() -eq '') { continue }
    $j = $line | ConvertFrom-Json
    $log[[string]$j.id] = [string]$j.result
}

foreach ($id in $Ids) {
    $job = $models.jobs | Where-Object { $_.id -eq $id } | Select-Object -First 1
    if ($null -eq $job) { throw "no model job '$id'" }
    $conceptName = [System.IO.Path]::GetFileNameWithoutExtension([string]$job.image)
    $row = $seedTable | Where-Object { $_ -match ('\| `' + [regex]::Escape([string]$job.image) + '` \| (\d+) \|') } | Select-Object -First 1
    if ($null -eq $row) { throw "no chosen seed for $($job.image) in $ConceptSeeds" }
    $seed = [long]([regex]::Match($row, '\| (\d+) \|$').Groups[1].Value)
    $concept = $concepts.jobs | Where-Object { $_.id -eq ("{0}_s{1}" -f $conceptName, $seed) } | Select-Object -First 1
    if ($null -eq $concept) { throw "no concept job ${conceptName}_s$seed" }

    $category = ($id -split '_')[0]
    $assetRel = "unity/Assets/Godtree/Art/$($folders[$category])/$id.glb"
    $assetFull = Join-Path $repo $assetRel
    [void](New-Item -ItemType Directory -Force -Path (Split-Path $assetFull -Parent))
    Copy-Item -LiteralPath (Join-Path $WorkDir "$id.glb") -Destination $assetFull -Force

    $recipeDir = Join-Path $repo "recipes/$id"
    [void](New-Item -ItemType Directory -Force -Path $recipeDir)
    Copy-Item -LiteralPath $workflowSrc -Destination (Join-Path $recipeDir 'workflow.json') -Force
    Copy-Item -LiteralPath (Join-Path $WorkDir ($id + '_preview.png')) -Destination (Join-Path $recipeDir 'preview.png') -Force

    $imageStep = [ordered]@{ generator = 'z-image-turbo'; tool = 'comfyui'; version = 'z_image_turbo_bf16 (Comfy-Org repack), ComfyUI 0.39.0'; prompt = [string]$concept.prompt; seed = $seed; vram_gb = 23; workflow = 'workflow.json' }
    $blenderStep = [ordered]@{ generator = 'blender'; version = 'Blender 5.2.2 LTS, tools/pipeline/blender_finish_model.py'; prompt = 'n/a'; seed = 0; vram_gb = 0 }
    if ($Trellis) {
        # The workflow the model job file names (standard or coarse), so the recipe records what was really run.
        $trellisWorkflow = Join-Path (Split-Path $workflowSrc -Parent) 'trellis2_image_to_glb.api.json'
        if ($null -ne $models.PSObject.Properties['workflow']) { $trellisWorkflow = Join-Path (Split-Path (Resolve-Path $ModelJobs).Path -Parent) ([string]$models.workflow) }
        Copy-Item -LiteralPath $trellisWorkflow -Destination (Join-Path $recipeDir 'trellis2.json') -Force
        $steps = @(
            $imageStep,
            [ordered]@{ generator = 'trellis2'; tool = 'comfyui'; version = 'trellis_2_bf16 with shape and texture VAEs and dino_v3_vit_l (Comfy-Org/TRELLIS.2), BiRefNet background removal inside the workflow, ComfyUI 0.39.0 built-in nodes'; prompt = 'n/a'; seed = [long]$job.seed; vram_gb = 24; workflow = 'trellis2.json' },
            $blenderStep
        )
        $how = "The concept image went through TRELLIS.2 (BiRefNet cut-out, shape, texture, remesh, reduce to about 40,000 triangles, unwrap, bake), then Blender reduced it to the budget and re-baked the texture."
    } else {
            $steps = @(
            $imageStep,
            [ordered]@{ generator = 'birefnet'; tool = 'comfyui'; version = 'birefnet.safetensors (Comfy-Org/BiRefNet), ComfyUI 0.39.0'; prompt = 'n/a'; seed = 0; vram_gb = 1; workflow = 'cutout.json' },
            [ordered]@{ generator = 'triposr'; version = 'TripoSR commit 107cefdc, stabilityai/TripoSR weights, torch 2.11.0+cu128, marching cubes via scikit-image, run by tools/pipeline/triposr_run.py (bundled background remover blocked)'; prompt = 'n/a'; seed = 0; vram_gb = 6 },
            $blenderStep
        )
        $how = "The background was removed with BiRefNet before TripoSR, which ran at marching-cubes resolution $($models.mc_resolution). BiRefNet, TripoSR and Blender take no seed (seed 0 is a placeholder)."
    }
    foreach ($stale in 'cutout.json', 'trellis2.json') {
        $stalePath = Join-Path $recipeDir $stale
        $wanted = ($Trellis -and $stale -eq 'trellis2.json') -or (-not $Trellis -and $stale -eq 'cutout.json')
        if (-not $wanted -and (Test-Path -LiteralPath $stalePath)) { [System.IO.File]::Delete($stalePath) }
    }
    $inputImage = 'stylekit/golden/concepts/' + [string]$job.image
    if ($null -ne $job.PSObject.Properties['image_path']) { $inputImage = 'stylekit/' + ([string]$job.image_path -replace '^\.\./', '') }
    $stats = $log[$id]
    $plainName = (($id -split '_', 2)[1]) -replace '_', ' '
    $title = $titles.ToTitleCase($plainName)
    $recipe = [ordered]@{
        id           = $id
        title        = $title
        origin       = 'generated'
        licence      = 'CC0-1.0'
        budget_class = [string]$job.budget_class
        steps        = $steps
        cleanup      = @(
            "Automatic only, by tools/pipeline/blender_finish_model.py: stood upright, welded, decimated to $($job.tris) triangles, smart UV unwrap, vertex colours baked to a $($job.tex) px texture, scaled so the longest side is $($job.size) m, origin at the centre of the base.",
            'No hand editing.'
        )
        sources      = @()
        outputs      = @( [ordered]@{ path = $assetRel } )
        notes        = ("Input image: $inputImage. $how Measured: $stats. $Note").Trim()
    }
    [System.IO.File]::WriteAllText((Join-Path $recipeDir 'recipe.json'), ((($recipe | ConvertTo-Json -Depth 6) -replace "`r`n", "`n") + "`n"), $enc)
    Write-Output "published $id -> $assetRel"
}
