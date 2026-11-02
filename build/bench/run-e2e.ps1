<#
.SYNOPSIS
  TASK-086 end-to-end performance measurements on the reference machine (D-014): NFR-001, NFR-002, NFR-004, NFR-005,
  NFR-006, NFR-007.

.DESCRIPTION
  Builds the solution in Release, then runs the measurement tests one project at a time with ASSEMBLY_STUDIO_BENCH=1:
  the in-process measurements (App tests: step latency, responsiveness, Pause/Stop, build time, editor latency) and the
  process measurements (UI tests: startup, memory). Each prints BENCH lines; they are collected in
  artifacts/perf/e2e-<timestamp>.txt. Nothing presses keys. Close other heavy programs first: these are wall-clock
  measurements, and other load on the machine shows in the numbers.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File build/bench/run-e2e.ps1
#>
param()

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$stamp = Get-Date -Format 'yyyy-MM-dd-HHmm'
$outDir = Join-Path $root 'artifacts\perf'
New-Item -ItemType Directory -Force $outDir | Out-Null
$out = Join-Path $outDir "e2e-$stamp.txt"

Push-Location $root
try {
    dotnet build AssemblyStudio.slnx -c Release --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    $env:ASSEMBLY_STUDIO_BENCH = '1'
    $lines = @("Assembly Studio end-to-end measurements, $(Get-Date -Format 'yyyy-MM-dd HH:mm zzz')",
               "Machine: $((Get-CimInstance Win32_Processor).Name.Trim()), $([math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 1)) GB, $((Get-CimInstance Win32_OperatingSystem).Caption)",
               '')
    $failed = $false
    foreach ($run in @(
        @{ Project = 'tests/AssemblyStudio.App.Tests';   Classes = @('*EndToEndPerformanceTests*', '*EditorLatencyTests*') },
        @{ Project = 'tests/AssemblyStudio.App.UiTests'; Classes = @('*PerformanceTests*') })) {
        $filters = $run.Classes | ForEach-Object { '--filter-class'; $_ }
        $output = dotnet test --project $run.Project -c Release --no-build --output Detailed @filters 2>&1
        if ($LASTEXITCODE -ne 0) { $failed = $true }
        $lines += $output | Where-Object { $_ -match 'BENCH |Keystroke to layout|^\s*(failed|passed|skipped) ' } | ForEach-Object { $_.Trim() }
    }

    $lines | Set-Content -Encoding utf8 $out
    $lines | ForEach-Object { Write-Host $_ }
    Write-Host "Saved: $out"
    if ($failed) { exit 1 }
}
finally {
    Remove-Item Env:ASSEMBLY_STUDIO_BENCH -ErrorAction SilentlyContinue
    Pop-Location
}
