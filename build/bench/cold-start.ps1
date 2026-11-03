<#
.SYNOPSIS
  NFR-005 cold start (TASK-108): one launch of the published application right after a reboot.

.DESCRIPTION
  Run this once, right after restarting Windows and before opening other programs: the first launch after a reboot is the
  cold start NFR-005 is about (nothing of the application is in the file cache yet). It starts the application published
  by build/bench/run-e2e.ps1 (artifacts/bench/app, self-contained win-x64 with ReadyToRun, as the installer publishes it)
  with temporary settings, recovery and log folders and --smoke-report, so the window closes itself after its first frame.
  The result and the application's own startup timeline are printed and saved in artifacts/perf/cold-start-<timestamp>.txt.
  Nothing is typed or clicked. Run it once: a second launch is no longer cold.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File build/bench/cold-start.ps1
#>
param()

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$exe = Join-Path $root 'artifacts\bench\app\AssemblyStudio.App.exe'
if (-not (Test-Path $exe)) { throw "The published application isn't there ($exe). Run build/bench/run-e2e.ps1 once first (before the reboot)." }

$uptime = (Get-Date) - (Get-CimInstance Win32_OperatingSystem).LastBootUpTime
$folder = Join-Path $env:TEMP ('asmstudio-cold-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory $folder | Out-Null
try {
    $report = Join-Path $folder 'startup.txt'
    $arguments = @('--settings', "`"$folder\settings.json`"", '--recovery', "`"$folder\recovery`"", '--log', "`"$folder\logs`"", '--smoke-report', "`"$report`"")
    $process = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru
    if (-not $process.WaitForExit(120000)) { $process.Kill(); throw 'The application did not close within 2 minutes.' }

    $ms = [double](Get-Content $report)
    $timeline = Get-ChildItem "$folder\logs\*.log" | Get-Content | Where-Object { $_ -match ' ms to the first frame:' } | Select-Object -Last 1
    $lines = @(
        "Assembly Studio NFR-005 cold start, $(Get-Date -Format 'yyyy-MM-dd HH:mm zzz')",
        ('Windows started {0:N0} min before this run.' -f $uptime.TotalMinutes),
        "Process start to the first rendered frame: $ms ms (target 3000 ms): $(if ($ms -le 3000) { 'PASS' } else { 'FAIL' })",
        ($timeline -replace '^.*INFO ', ''))
    $outDir = Join-Path $root 'artifacts\perf'
    New-Item -ItemType Directory -Force $outDir | Out-Null
    $out = Join-Path $outDir ("cold-start-{0}.txt" -f (Get-Date -Format 'yyyy-MM-dd-HHmm'))
    $lines | Set-Content -Encoding utf8 $out
    $lines | ForEach-Object { Write-Host $_ }
    Write-Host "Saved: $out"
}
finally {
    Remove-Item -Recurse -Force $folder -ErrorAction SilentlyContinue
}
