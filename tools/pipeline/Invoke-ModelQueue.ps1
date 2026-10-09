<#
.SYNOPSIS
  Image-to-3D queue: TripoSR on each job image, then Blender finishing
  (decimate, unwrap, bake, scale, export .glb, render preview). Unattended and
  resumable: a job whose .glb already exists is skipped. Keep this file pure ASCII.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools/pipeline/Invoke-ModelQueue.ps1 -JobsPath stylekit/jobs/models-v0.json -OutDir D:\godtree-work\models-v0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$JobsPath,
    [Parameter(Mandatory = $true)][string]$OutDir,
    [string]$TripoRoot = 'D:\godtree-tools\TripoSR',
    [string]$Python = 'D:\godtree-tools\triposr-venv\Scripts\python.exe',
    [string]$Blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe',
    [string]$HfHome = 'D:\godtree-tools\hf-cache',
    # Folder of cut-out images (transparent background), named like the job images.
    [Parameter(Mandatory = $true)][string]$CutoutDir,
    [int]$MaxGpuTempC = 83
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$jobsFile = (Resolve-Path $JobsPath).Path
$spec = [System.IO.File]::ReadAllText($jobsFile, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$imageDir = (Resolve-Path $CutoutDir).Path
$runner = (Resolve-Path (Join-Path $PSScriptRoot 'triposr_run.py')).Path
$finish = (Resolve-Path (Join-Path $PSScriptRoot 'blender_finish_model.py')).Path
[void](New-Item -ItemType Directory -Force -Path $OutDir)
$rawDir = Join-Path $OutDir 'raw'
$logPath = Join-Path $OutDir 'model-log.jsonl'

$todo = @($spec.jobs | Where-Object { -not (Test-Path -LiteralPath (Join-Path $OutDir ($_.id + '.glb'))) })
if ($todo.Count -eq 0) { Write-Output 'Nothing to do.'; exit 0 }

$temp = 0
try { $temp = [int]((& nvidia-smi --query-gpu=temperature.gpu --format=csv,noheader,nounits) | Select-Object -First 1) } catch { }
while ($temp -gt $MaxGpuTempC) {
    Write-Output "GPU at $temp C; pausing 60 s."
    Start-Sleep -Seconds 60
    $temp = [int]((& nvidia-smi --query-gpu=temperature.gpu --format=csv,noheader,nounits) | Select-Object -First 1)
}

# One TripoSR run for all pending images (the model loads once). Outputs land in raw/<index>/mesh.glb.
if (Test-Path -LiteralPath $rawDir) { Remove-Item -LiteralPath $rawDir -Recurse -Force }
$images = @($todo | ForEach-Object { '"' + (Join-Path $imageDir $_.image) + '"' }) -join ' '
$env:HF_HOME = $HfHome
$env:PYTHONIOENCODING = 'utf-8'
$triLog = Join-Path $OutDir 'triposr.log'
$started = Get-Date
Push-Location $TripoRoot
try {
    cmd /c "`"$Python`" `"$runner`" --triposr-root `"$TripoRoot`" --out `"$rawDir`" --mc-resolution $($spec.mc_resolution) $images > `"$triLog`" 2>&1"
    if ($LASTEXITCODE -ne 0) { throw "TripoSR failed; see $triLog" }
} finally { Pop-Location }
Write-Output ("TripoSR: {0} meshes in {1:N0} s" -f $todo.Count, ((Get-Date) - $started).TotalSeconds)

$failed = 0
for ($i = 0; $i -lt $todo.Count; $i++) {
    $job = $todo[$i]
    $raw = Join-Path $rawDir "$i\mesh.glb"
    $glb = Join-Path $OutDir ($job.id + '.glb')
    $png = Join-Path $OutDir ($job.id + '_preview.png')
    $bLog = Join-Path $OutDir ($job.id + '.blender.log')
    cmd /c "`"$Blender`" -b --factory-startup -P `"$finish`" -- `"$raw`" `"$glb`" `"$png`" --tris $($job.tris) --tex $($job.tex) --size $($job.size) > `"$bLog`" 2>&1"
    $result = Select-String -LiteralPath $bLog -Pattern 'GODTREE_RESULT (.*)$' | Select-Object -First 1
    if ($null -eq $result -or -not (Test-Path -LiteralPath $glb)) {
        $failed++
        Write-Output "FAIL  $($job.id)  see $bLog"
        continue
    }
    $rec = New-Object psobject -Property @{ id = [string]$job.id; image = [string]$job.image; budget_class = [string]$job.budget_class; result = $result.Matches[0].Groups[1].Value; time = (Get-Date).ToString('s') }
    Add-Content -LiteralPath $logPath -Value ($rec | ConvertTo-Json -Compress) -Encoding UTF8
    Write-Output "done  $($job.id)  $($result.Matches[0].Groups[1].Value)"
}
Write-Output "Model queue finished: $($todo.Count - $failed) made, $failed failed. Output: $OutDir"
if ($failed -gt 0) { exit 1 }
exit 0
