<#
.SYNOPSIS
  Tests for Run-Checks.ps1. Builds throwaway fixture repositories in the temp
  folder and asserts that each rule passes or fails as intended.
  Run:  powershell -File tools/checks/tests/Run-Tests.ps1     (or pwsh)
  Keep this file pure ASCII.
#>
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$checks = Join-Path $repo 'tools/checks/Run-Checks.ps1'
$hostExe = (Get-Process -Id $PID).Path
$tmpRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('godtree-checks-' + [guid]::NewGuid().ToString('N'))
$script:Passed = 0
$script:Failed = 0

function New-Fixture {
    $dir = Join-Path $tmpRoot ([guid]::NewGuid().ToString('N'))
    [void](New-Item -ItemType Directory -Path $dir)
    return $dir
}
function Write-File([string]$Root, [string]$Rel, [string]$Text) {
    $full = Join-Path $Root $Rel
    [void](New-Item -ItemType Directory -Force -Path (Split-Path $full -Parent))
    [System.IO.File]::WriteAllText($full, $Text, (New-Object System.Text.UTF8Encoding($false)))
}
function Write-Png([string]$Root, [string]$Rel, [int]$W, [int]$H) {
    $full = Join-Path $Root $Rel
    [void](New-Item -ItemType Directory -Force -Path (Split-Path $full -Parent))
    $b = New-Object byte[] 64
    $sig = @(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52)
    for ($i = 0; $i -lt $sig.Count; $i++) { $b[$i] = [byte]$sig[$i] }
    $b[16] = [byte](($W -shr 24) -band 0xFF); $b[17] = [byte](($W -shr 16) -band 0xFF); $b[18] = [byte](($W -shr 8) -band 0xFF); $b[19] = [byte]($W -band 0xFF)
    $b[20] = [byte](($H -shr 24) -band 0xFF); $b[21] = [byte](($H -shr 16) -band 0xFF); $b[22] = [byte](($H -shr 8) -band 0xFF); $b[23] = [byte]($H -band 0xFF)
    [System.IO.File]::WriteAllBytes($full, $b)
}
function Write-Glb([string]$Root, [string]$Rel, [int]$IndexCount) {
    $full = Join-Path $Root $Rel
    [void](New-Item -ItemType Directory -Force -Path (Split-Path $full -Parent))
    $json = '{"asset":{"version":"2.0"},"accessors":[{"count":' + $IndexCount + '},{"count":8}],"meshes":[{"primitives":[{"indices":0,"attributes":{"POSITION":1}}]}]}'
    while (($json.Length % 4) -ne 0) { $json += ' ' }
    $jb = [System.Text.Encoding]::UTF8.GetBytes($json)
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    $bw.Write([uint32]0x46546C67); $bw.Write([uint32]2); $bw.Write([uint32](20 + $jb.Length))
    $bw.Write([uint32]$jb.Length); $bw.Write([uint32]0x4E4F534A); $bw.Write($jb)
    $bw.Flush()
    [System.IO.File]::WriteAllBytes($full, $ms.ToArray())
}

$goodBody = @"
## Issue
Closes #12

## Task type
asset

## Sponsor
Jane Example (@jane-example)

## What was done
Made the training sword.

## How it was verified
Opened the .glb in Blender; ran the checks locally.

## Not done or not confirmed
Nothing: all deliverables in the brief are included.

## Third-party material
Nothing: everything was created in this pull request.
"@

function Get-Recipe([string]$Generator = 'z-image-turbo', [string]$Prompt = 'a short steel training sword, cel shaded', [string]$Extra = '') {
    return @"
{
  "id": "wpn_sword_training",
  "title": "Training sword",
  "origin": "generated",
  "licence": "CC0-1.0",
  "budget_class": "weapon",
  "steps": [
    { "generator": "$Generator", "tool": "comfyui", "version": "1.0", "prompt": "$Prompt", "seed": 1234, "vram_gb": 16, "workflow": "workflow.json" }
  ],
  "cleanup": [ "Decimated to budget in Blender 4.5" ],
  $Extra
  "outputs": [
    { "path": "unity/Assets/Godtree/Art/Weapons/wpn_sword_training.glb" },
    { "path": "unity/Assets/Godtree/Art/Weapons/wpn_sword_training_albedo.png" }
  ]
}
"@
}

function New-GoodFixture {
    $f = New-Fixture
    Write-File $f 'recipes/wpn_sword_training/recipe.json' (Get-Recipe)
    Write-File $f 'recipes/wpn_sword_training/workflow.json' '{}'
    Write-Glb $f 'unity/Assets/Godtree/Art/Weapons/wpn_sword_training.glb' 6000
    Write-Png $f 'unity/Assets/Godtree/Art/Weapons/wpn_sword_training_albedo.png' 1024 1024
    Write-File $f 'changed.txt' ("A`trecipes/wpn_sword_training/recipe.json`nA`trecipes/wpn_sword_training/workflow.json`nA`tunity/Assets/Godtree/Art/Weapons/wpn_sword_training.glb`nA`tunity/Assets/Godtree/Art/Weapons/wpn_sword_training_albedo.png`n")
    Write-File $f 'body.md' $goodBody
    return $f
}

function Invoke-Checks([string]$Fixture, [string]$Author = 'stranger', [string]$Association = 'NONE', [switch]$NoBody) {
    $argList = @('-NoProfile')
    if ($PSVersionTable.PSVersion.Major -lt 6) { $argList += @('-ExecutionPolicy', 'Bypass') }
    $argList += @('-File', $checks, '-RepoRoot', $Fixture, '-RulesRoot', $repo, '-ChangedFilesPath', (Join-Path $Fixture 'changed.txt'), '-Author', $Author, '-AuthorAssociation', $Association)
    if (-not $NoBody) { $argList += @('-PrBodyPath', (Join-Path $Fixture 'body.md')) }
    $out = & $hostExe @argList 2>&1 | Out-String
    return New-Object psobject -Property @{ Code = $LASTEXITCODE; Output = $out }
}

function Assert-Result([string]$Name, $Result, [bool]$ShouldPass, [string]$Expect = '') {
    $ok = ($ShouldPass -and $Result.Code -eq 0) -or (-not $ShouldPass -and $Result.Code -ne 0)
    if ($ok -and $Expect -ne '' -and ($Result.Output -notmatch [regex]::Escape($Expect))) { $ok = $false }
    if ($ok) {
        $script:Passed++
        Write-Output "pass  $Name"
    } else {
        $script:Failed++
        Write-Output "FAIL  $Name  (exit $($Result.Code), wanted pass=$ShouldPass, expected text '$Expect')"
        Write-Output $Result.Output
    }
}

try {
    [void](New-Item -ItemType Directory -Path $tmpRoot)

    $f = New-GoodFixture
    Assert-Result 'complete asset contribution passes' (Invoke-Checks $f) $true 'measured at 2000 triangles'

    $f = New-GoodFixture
    Remove-Item -Recurse -Force (Join-Path $f 'recipes')
    Write-File $f 'changed.txt' "A`tunity/Assets/Godtree/Art/Weapons/wpn_sword_training.glb`n"
    Assert-Result 'asset without a recipe fails' (Invoke-Checks $f) $false 'has no recipe'

    $f = New-GoodFixture
    Write-File $f 'recipes/wpn_sword_training/recipe.json' (Get-Recipe -Generator 'hunyuan3d-2.1')
    Assert-Result 'rejected generator fails' (Invoke-Checks $f) $false "status 'rejected'"

    $f = New-GoodFixture
    Write-File $f 'recipes/wpn_sword_training/recipe.json' (Get-Recipe -Generator 'trellis2')
    Assert-Result 'conditional generator fails' (Invoke-Checks $f) $false "status 'conditional'"

    $f = New-GoodFixture
    Write-File $f 'recipes/wpn_sword_training/recipe.json' (Get-Recipe -Generator 'some-cloud-service')
    Assert-Result 'unknown generator fails' (Invoke-Checks $f) $false 'is not in data/approved-generators.json'

    $f = New-GoodFixture
    Remove-Item (Join-Path $f 'recipes/wpn_sword_training/workflow.json')
    Assert-Result 'missing workflow file fails' (Invoke-Checks $f) $false 'workflow file'

    $f = New-GoodFixture
    Write-Png $f 'unity/Assets/Godtree/Art/Weapons/wpn_sword_training_albedo.png' 2048 2048
    Assert-Result 'texture over class limit fails' (Invoke-Checks $f) $false 'the limit here is 1024px'

    $f = New-GoodFixture
    Write-Png $f 'unity/Assets/Godtree/Art/Weapons/wpn_sword_training_albedo.png' 1000 1000
    Assert-Result 'non-power-of-two texture fails' (Invoke-Checks $f) $false 'powers of two'

    $f = New-GoodFixture
    Write-Glb $f 'unity/Assets/Godtree/Art/Weapons/wpn_sword_training.glb' 30000
    Assert-Result 'model over triangle budget fails' (Invoke-Checks $f) $false 'has 10000 triangles'

    $f = New-GoodFixture
    Write-Png $f 'unity/Assets/Godtree/Art/Weapons/Cool Sword.png' 512 512
    Add-Content -LiteralPath (Join-Path $f 'changed.txt') -Value "A`tunity/Assets/Godtree/Art/Weapons/Cool Sword.png"
    Assert-Result 'badly named asset fails' (Invoke-Checks $f) $false '[naming]'

    $f = New-GoodFixture
    Write-File $f 'tools/checks/Run-Checks.ps1' 'exit 0'
    Add-Content -LiteralPath (Join-Path $f 'changed.txt') -Value "M`ttools/checks/Run-Checks.ps1"
    Assert-Result 'stranger touching the checks fails' (Invoke-Checks $f) $false 'restricted area'

    $f = New-GoodFixture
    Write-File $f 'unity/Assets/Godtree/Art/Editor/Evil.cs' 'class Evil {}'
    Add-Content -LiteralPath (Join-Path $f 'changed.txt') -Value "A`tunity/Assets/Godtree/Art/Editor/Evil.cs"
    Assert-Result 'editor script hidden in an asset folder fails' (Invoke-Checks $f) $false 'restricted area'

    $f = New-GoodFixture
    Write-File $f 'body.md' ($goodBody -replace 'Jane Example \(@jane-example\)', '<!-- name the human responsible -->')
    Assert-Result 'missing sponsor fails' (Invoke-Checks $f) $false "section 'sponsor'"

    $f = New-GoodFixture
    Write-File $f 'body.md' ($goodBody -replace '(?m)^asset$', 'code')
    Assert-Result 'tier 0 author cannot open a code task' (Invoke-Checks $f) $false 'needs trust tier 1'

    $f = New-GoodFixture
    Write-File $f 'body.md' ($goodBody -replace '(?m)^asset$', 'restricted')
    Write-File $f 'data/budgets.json' '{}'
    Add-Content -LiteralPath (Join-Path $f 'changed.txt') -Value "M`tdata/budgets.json"
    Assert-Result 'owner may touch restricted paths, with a notice' (Invoke-Checks $f -Author 'owner' -Association 'OWNER') $true 'needs the owner'

    $f = New-GoodFixture
    Write-File $f 'recipes/wpn_sword_training/recipe.json' (Get-Recipe -Prompt 'the Elucidator sword of Kirito')
    Assert-Result 'name from an existing work fails' (Invoke-Checks $f) $false 'name from an existing work'

    $f = New-GoodFixture
    $src = '"sources": [ { "name": "Some texture", "url": "https://example.org/t", "author": "Someone", "licence": "CC-BY-NC-4.0" } ],'
    Write-File $f 'recipes/wpn_sword_training/recipe.json' ((Get-Recipe -Extra $src) -replace '"origin": "generated"', '"origin": "mixed"')
    Assert-Result 'unaccepted source licence fails' (Invoke-Checks $f) $false 'is not accepted'

    $f = New-GoodFixture
    Write-File $f 'recipes/wpn_sword_training/recipe.json' ((Get-Recipe) -replace 'wpn_sword_training\.glb', 'wpn_sword_training.fbx')
    Move-Item (Join-Path $f 'unity/Assets/Godtree/Art/Weapons/wpn_sword_training.glb') (Join-Path $f 'unity/Assets/Godtree/Art/Weapons/wpn_sword_training.fbx')
    Write-File $f 'changed.txt' ("A`trecipes/wpn_sword_training/recipe.json`nA`tunity/Assets/Godtree/Art/Weapons/wpn_sword_training.fbx`n")
    Assert-Result 'fbx without a declared triangle count fails' (Invoke-Checks $f) $false "must declare 'triangles'"

    $f = New-GoodFixture
    Write-File $f 'unity/Assets/Godtree/Art/Weapons/wpn_sword_helper.exe' 'MZ'
    Add-Content -LiteralPath (Join-Path $f 'changed.txt') -Value "A`tunity/Assets/Godtree/Art/Weapons/wpn_sword_helper.exe"
    Assert-Result 'executable in an asset folder fails' (Invoke-Checks $f) $false 'restricted area'

    $f = New-Fixture
    Write-File $f 'docs/guide.md' 'A guide.'
    Write-File $f 'changed.txt' "A`tdocs/guide.md`n"
    Write-File $f 'body.md' ($goodBody -replace '(?m)^asset$', 'docs')
    Assert-Result 'docs contribution passes' (Invoke-Checks $f) $true

    $f = New-Fixture
    Write-File $f 'docs/guide.md' 'A guide.'
    Write-File $f 'canon/world-bible.md' 'New lore.'
    Write-File $f 'changed.txt' "A`tdocs/guide.md`nM`tcanon/world-bible.md`n"
    Write-File $f 'body.md' ($goodBody -replace '(?m)^asset$', 'docs')
    Assert-Result 'docs task editing the canon fails' (Invoke-Checks $f) $false 'restricted area'
}
finally {
    if (Test-Path -LiteralPath $tmpRoot) { Remove-Item -Recurse -Force -LiteralPath $tmpRoot }
}

Write-Output ''
Write-Output "Tests: $($script:Passed) passed, $($script:Failed) failed."
if ($script:Failed -gt 0) { exit 1 }
exit 0
