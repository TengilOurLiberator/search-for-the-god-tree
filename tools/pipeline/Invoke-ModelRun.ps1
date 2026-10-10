<#
.SYNOPSIS
  Image to finished model in one unattended run: the TRELLIS.2 queue for a job file, then Blender
  finishing. The queue runs as a process of its own and only it is waited for (the generator's
  server would otherwise hold this script's output open). The server is stopped afterwards so
  the graphics memory is free. Publishing is left to the lead, who must look at the previews
  first (finished/<id>_preview.png). Keep this file pure ASCII.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools/pipeline/Invoke-ModelRun.ps1 -JobsPath stylekit/jobs/trellis2-f1-town.json -WorkDir D:\godtree-work\trellis2-f1-town
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$JobsPath,
    [Parameter(Mandatory = $true)][string]$WorkDir
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$jobsFull = (Resolve-Path $JobsPath).Path
New-Item -ItemType Directory -Force $WorkDir | Out-Null
$queueLog = Join-Path $WorkDir 'queue.log'
$queue = Start-Process powershell -ArgumentList '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $PSScriptRoot 'Invoke-GenerationQueue.ps1'), '-JobsPath', $jobsFull, '-OutDir', $WorkDir -WindowStyle Hidden -RedirectStandardOutput $queueLog -PassThru
$queue.WaitForExit()
Get-Content $queueLog -Tail 2
Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match 'ComfyUI' -and $_.Name -match 'python' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -Confirm:$false }
& powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Finish-TrellisModels.ps1') -JobsPath $jobsFull -RawDir $WorkDir -OutDir (Join-Path $WorkDir 'finished') | Select-Object -Last 30
Write-Output 'MODEL RUN DONE'
