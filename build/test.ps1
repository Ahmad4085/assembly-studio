<#
.SYNOPSIS
  Runs the whole test suite with the UI tests on their own (TASK-107, DEF-020).

.DESCRIPTION
  The UI tests start the real application and wait for its window. Run alongside the other test projects, the machine is
  loaded and the window can be slow to appear, so this runs every other project first (in parallel, as usual), then the UI
  test project alone. With -ResultsDirectory, both steps write TRX and xUnit XML result files there (the testing phase's
  raw results; TASK-110, DEF-024). Exits 1 if either step fails.

.PARAMETER Configuration
  Build configuration (default Release).

.PARAMETER NoBuild
  Use the existing build.

.PARAMETER ResultsDirectory
  Where both steps write TRX and xUnit XML results. Without it, no result files are written.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File build/test.ps1
  powershell -ExecutionPolicy Bypass -File build/test.ps1 -NoBuild -ResultsDirectory artifacts/test-results/2026-11-03-2
#>
param(
    [string]$Configuration = 'Release',
    [switch]$NoBuild,
    [string]$ResultsDirectory
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$common = @('-c', $Configuration)
if ($NoBuild) { $common += '--no-build' }
$results = @()
if ($ResultsDirectory) {
    $resultsPath = if ([System.IO.Path]::IsPathRooted($ResultsDirectory)) { $ResultsDirectory } else { Join-Path $root $ResultsDirectory }
    New-Item -ItemType Directory -Force -Path $resultsPath | Out-Null
    $results = @('--results-directory', $resultsPath, '--report-xunit-trx', '--report-xunit-xml')
}

Push-Location $root
try {
    Write-Host "== 1/2: every project except the UI tests"
    # The UI test project starts too but runs no test; exit code 8 ("no test ran") is expected for it.
    dotnet test --solution AssemblyStudio.slnx @common --filter-not-namespace AssemblyStudio.App.UiTests --ignore-exit-code 8 @results
    $first = $LASTEXITCODE

    Write-Host "== 2/2: the UI tests alone"
    dotnet test --project tests/AssemblyStudio.App.UiTests -c $Configuration --no-build @results
    $second = $LASTEXITCODE

    if ($first -ne 0 -or $second -ne 0) { exit 1 }
}
finally {
    Pop-Location
}
