<#
.SYNOPSIS
  Runs a queue of local image-generation jobs through ComfyUI, unattended.

.DESCRIPTION
  Reads a jobs file, fills a ComfyUI API-format workflow template for each job,
  submits it to a local ComfyUI server (starting one if needed), waits for the
  result and copies it to the output folder. Safe to stop and restart: jobs
  whose output already exists are skipped. Pauses while the GPU is hotter than
  -MaxGpuTempC. Writes one log line per job (JSON lines) for recipes.

  Nothing here needs an internet connection or any cloud service.
  Keep this file pure ASCII.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools/pipeline/Invoke-GenerationQueue.ps1 -JobsPath stylekit/jobs/concepts-v0.json -OutDir D:\godtree-work\concepts-v0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$JobsPath,
    [Parameter(Mandatory = $true)][string]$OutDir,
    [string]$ComfyRoot = 'D:\godtree-tools\ComfyUI_windows_portable',
    [string]$ModelPathsConfig = 'D:\godtree-tools\extra_model_paths.yaml',
    [string]$Server = 'http://127.0.0.1:8188',
    [int]$MaxGpuTempC = 83,
    [int]$JobTimeoutSeconds = 900,
    [int]$MaxJobs = 0,
    [int]$CooldownSeconds = 3,
    [switch]$StopServerWhenDone
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Get-GpuState {
    try {
        $line = (& nvidia-smi --query-gpu=temperature.gpu,memory.used,utilization.gpu,power.draw --format=csv,noheader,nounits) | Select-Object -First 1
        $p = $line -split ',\s*'
        return @{ TempC = [int]$p[0]; VramMb = [int]$p[1]; Util = [int]$p[2]; Watts = [double]$p[3] }
    } catch {
        return @{ TempC = 0; VramMb = 0; Util = 0; Watts = 0 }
    }
}

function Test-Server {
    try { [void](Invoke-RestMethod -Uri "$Server/system_stats" -TimeoutSec 5); return $true } catch { return $false }
}

function ConvertTo-JsonString([string]$Text) {
    # A JSON string literal, quotes included, ASCII-safe.
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append('"')
    foreach ($ch in $Text.ToCharArray()) {
        $code = [int]$ch
        if ($ch -eq '"') { [void]$sb.Append('\"') }
        elseif ($ch -eq '\') { [void]$sb.Append('\\') }
        elseif ($code -eq 10) { [void]$sb.Append('\n') }
        elseif ($code -eq 13) { }
        elseif ($code -eq 9) { [void]$sb.Append('\t') }
        elseif ($code -lt 32 -or $code -gt 126) { [void]$sb.Append(('\u{0:x4}' -f $code)) }
        else { [void]$sb.Append($ch) }
    }
    [void]$sb.Append('"')
    return $sb.ToString()
}

$jobsFile = (Resolve-Path $JobsPath).Path
$spec = [System.IO.File]::ReadAllText($jobsFile, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$templatePath = Join-Path (Split-Path $jobsFile -Parent) $spec.workflow
$template = [System.IO.File]::ReadAllText((Resolve-Path $templatePath).Path, [System.Text.Encoding]::UTF8)
[void](New-Item -ItemType Directory -Force -Path $OutDir)
$logPath = Join-Path $OutDir 'generation-log.jsonl'
$comfyOut = Join-Path $ComfyRoot 'ComfyUI\output'

$serverProc = $null
if (-not (Test-Server)) {
    Write-Output 'Starting ComfyUI...'
    $py = Join-Path $ComfyRoot 'python_embeded\python.exe'
    $argList = @('-s', 'ComfyUI\main.py', '--windows-standalone-build', '--disable-auto-launch', '--listen', '127.0.0.1', '--port', '8188')
    if (Test-Path -LiteralPath $ModelPathsConfig) { $argList += @('--extra-model-paths-config', $ModelPathsConfig) }
    $serverLog = Join-Path $OutDir 'comfyui-server.log'
    $serverProc = Start-Process -FilePath $py -ArgumentList $argList -WorkingDirectory $ComfyRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $serverLog -RedirectStandardError ($serverLog + '.err')
    $deadline = (Get-Date).AddMinutes(5)
    while (-not (Test-Server)) {
        if ((Get-Date) -gt $deadline -or $serverProc.HasExited) { throw "ComfyUI did not start; see $serverLog.err" }
        Start-Sleep -Seconds 3
    }
}
Write-Output 'ComfyUI is up.'

$done = 0; $skipped = 0; $failed = 0
foreach ($job in @($spec.jobs)) {
    $id = [string]$job.id
    $target = Join-Path $OutDir ($id + '.png')
    if (Test-Path -LiteralPath $target) { $skipped++; continue }
    if ($MaxJobs -gt 0 -and $done -ge $MaxJobs) { break }

    $gpu = Get-GpuState
    while ($gpu.TempC -gt $MaxGpuTempC) {
        Write-Output "GPU at $($gpu.TempC) C; pausing 60 s."
        Start-Sleep -Seconds 60
        $gpu = Get-GpuState
    }

    $width = 1024; $height = 1024
    if ($null -ne $job.PSObject.Properties['width']) { $width = [int]$job.width }
    if ($null -ne $job.PSObject.Properties['height']) { $height = [int]$job.height }
    $wf = $template.Replace('"%PROMPT%"', (ConvertTo-JsonString ([string]$job.prompt)))
    $wf = $wf.Replace('"%SEED%"', [string][long]$job.seed).Replace('"%WIDTH%"', [string]$width).Replace('"%HEIGHT%"', [string]$height)
    $wf = $wf.Replace('"%ID%"', (ConvertTo-JsonString ("godtree_" + $id)))
    $body = '{"prompt":' + $wf + '}'

    $started = Get-Date
    $peakVram = 0; $peakTemp = 0
    try {
        $resp = Invoke-RestMethod -Uri "$Server/prompt" -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 60
        $pid2 = [string]$resp.prompt_id
        $file = $null
        $deadline = $started.AddSeconds($JobTimeoutSeconds)
        while ($null -eq $file) {
            Start-Sleep -Milliseconds 700
            $g = Get-GpuState
            if ($g.VramMb -gt $peakVram) { $peakVram = $g.VramMb }
            if ($g.TempC -gt $peakTemp) { $peakTemp = $g.TempC }
            if ((Get-Date) -gt $deadline) { throw 'timed out' }
            $hist = Invoke-RestMethod -Uri "$Server/history/$pid2" -TimeoutSec 30
            $entry = $hist.PSObject.Properties[$pid2]
            if ($null -ne $entry) {
                $status = $entry.Value.status
                if ($null -ne $status -and [string]$status.status_str -eq 'error') { throw 'ComfyUI reported an error for this job' }
                foreach ($node in $entry.Value.outputs.PSObject.Properties) {
                    $imgs = $node.Value.PSObject.Properties['images']
                    if ($null -ne $imgs) { foreach ($img in @($imgs.Value)) { $file = Join-Path (Join-Path $comfyOut ([string]$img.subfolder)) ([string]$img.filename) } }
                }
                if ($null -eq $file) { throw 'job finished without an image' }
            }
        }
        Move-Item -LiteralPath $file -Destination $target -Force
        $secs = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
        $done++
        $rec = New-Object psobject -Property @{ id = $id; ok = $true; seed = [long]$job.seed; width = $width; height = $height; seconds = $secs; peak_vram_mb = $peakVram; peak_temp_c = $peakTemp; prompt = [string]$job.prompt; workflow = [string]$spec.workflow; time = (Get-Date).ToString('s') }
        Add-Content -LiteralPath $logPath -Value ($rec | ConvertTo-Json -Compress) -Encoding UTF8
        Write-Output ("done  {0}  {1}s  vram {2} MB  {3} C" -f $id, $secs, $peakVram, $peakTemp)
        # Rest between jobs: keeps average power draw and heat down on long unattended runs.
        if ($CooldownSeconds -gt 0) { Start-Sleep -Seconds $CooldownSeconds }
    } catch {
        $failed++
        $rec = New-Object psobject -Property @{ id = $id; ok = $false; error = $_.Exception.Message; time = (Get-Date).ToString('s') }
        Add-Content -LiteralPath $logPath -Value ($rec | ConvertTo-Json -Compress) -Encoding UTF8
        Write-Output ("FAIL  {0}  {1}" -f $id, $_.Exception.Message)
        if (-not (Test-Server)) { Write-Output 'ComfyUI is no longer reachable; stopping the queue.'; break }
    }
}

Write-Output ''
Write-Output "Queue finished: $done generated, $skipped already present, $failed failed. Output: $OutDir"
if ($StopServerWhenDone -and $null -ne $serverProc -and -not $serverProc.HasExited) { Stop-Process -Id $serverProc.Id -Force }
if ($failed -gt 0) { exit 1 }
exit 0
