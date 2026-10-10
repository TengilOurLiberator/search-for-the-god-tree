<#
.SYNOPSIS
  Starts the two local services the talking NPCs need, if they are not already running:
    - the language model (llama.cpp's llama-server, MIT, with Qwen3.5-4B, Apache-2.0) on port 8790
    - the voice server (tools/voice/godtree_voice_server.py: Kokoro to speak, Whisper to hear) on port 8791
  Both listen on this machine only. Waits until both answer. -Stop shuts them down.
  The game works without them: NPCs then stay silent. Keep this file pure ASCII.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools/voice/Start-NpcServices.ps1
  powershell -ExecutionPolicy Bypass -File tools/voice/Start-NpcServices.ps1 -Stop
#>
[CmdletBinding()]
param(
    [switch]$Stop,
    [string]$LlamaServer = 'D:\godtree-tools\llama\llama-server.exe',
    [string]$Model = 'D:\godtree-tools\models\llm\Qwen3.5-4B-Q4_K_M.gguf',
    [string]$Python = 'D:\godtree-tools\voice-venv\Scripts\python.exe',
    [string]$VoiceModels = 'D:\godtree-tools\models\voice',
    [string]$LogDir = 'D:\godtree-work\npc-services',
    [int]$LlmPort = 8790,
    [int]$VoicePort = 8791,
    [int]$WaitSeconds = 120
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Test-Up([string]$url) {
    try { $null = Invoke-WebRequest -UseBasicParsing $url -TimeoutSec 3; return $true } catch { return $false }
}
function Get-Service-Processes {
    Get-CimInstance Win32_Process | Where-Object {
        ($_.Name -eq 'llama-server.exe') -or ($_.CommandLine -match 'godtree_voice_server\.py')
    }
}

if ($Stop) {
    Get-Service-Processes | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -Confirm:$false; Write-Output "stopped $($_.Name) ($($_.ProcessId))" }
    return
}

New-Item -ItemType Directory -Force $LogDir | Out-Null
$env:HF_HOME = 'D:\godtree-tools\hf-cache'

if (-not (Test-Up "http://127.0.0.1:$LlmPort/health")) {
    if (-not (Test-Path $LlamaServer)) { throw "llama-server not found at $LlamaServer" }
    if (-not (Test-Path $Model)) { throw "model not found at $Model" }
    # -ngl 99: whole model on the graphics card (about 4 GB). -c 4096: room for a character sheet and a short talk.
    Start-Process $LlamaServer -ArgumentList '-m', $Model, '--host', '127.0.0.1', '--port', $LlmPort, '-ngl', '99', '-c', '4096', '--jinja', '--parallel', '1' `
        -WindowStyle Hidden -RedirectStandardOutput (Join-Path $LogDir 'llm.out.log') -RedirectStandardError (Join-Path $LogDir 'llm.err.log')
    Write-Output 'starting the language model'
}
if (-not (Test-Up "http://127.0.0.1:$VoicePort/health")) {
    if (-not (Test-Path $Python)) { throw "voice environment not found at $Python" }
    Start-Process $Python -ArgumentList (Join-Path $PSScriptRoot 'godtree_voice_server.py'), '--models', $VoiceModels, '--port', $VoicePort `
        -WindowStyle Hidden -RedirectStandardOutput (Join-Path $LogDir 'voice.out.log') -RedirectStandardError (Join-Path $LogDir 'voice.err.log')
    Write-Output 'starting the voice server'
}

$deadline = (Get-Date).AddSeconds($WaitSeconds)
do {
    $llm = Test-Up "http://127.0.0.1:$LlmPort/health"
    $voice = Test-Up "http://127.0.0.1:$VoicePort/health"
    if ($llm -and $voice) { break }
    Start-Sleep -Seconds 2
} while ((Get-Date) -lt $deadline)
Write-Output ("language model: {0}   voice server: {1}" -f $(if ($llm) { 'up' } else { 'NOT up' }), $(if ($voice) { 'up' } else { 'NOT up' }))
if (-not ($llm -and $voice)) { Write-Output "logs are in $LogDir"; exit 1 }
