<#
.SYNOPSIS
  Runs the whole test suite with the UI tests on their own (TASK-107, DEF-020).

.DESCRIPTION
  The UI tests start the real application and wait for its window. Run alongside the other test projects, the machine is
  loaded and the window can be slow to appear, so this runs every other project first (in parallel, as usual), then the UI
  test project alone. Pass any further `dotnet test` options after the switches, for example
  `-- --results-directory artifacts/test-results/x --report-xunit-trx`. Exits 1 if either step fails.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File build/test.ps1
  powershell -ExecutionPolicy Bypass -File build/test.ps1 -Configuration Release -NoBuild
#>
param(
    [string]$Configuration = 'Release',
    [switch]$NoBuild,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$Rest = @()
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$common = @('-c', $Configuration)
if ($NoBuild) { $common += '--no-build' }

Push-Location $root
try {
    Write-Host "== 1/2: every project except the UI tests"
    # The UI test project starts too but runs no test; exit code 8 ("no test ran") is expected for it.
    dotnet test --solution AssemblyStudio.slnx @common --filter-not-namespace AssemblyStudio.App.UiTests --ignore-exit-code 8 @Rest
    $first = $LASTEXITCODE

    Write-Host "== 2/2: the UI tests alone"
    dotnet test --project tests/AssemblyStudio.App.UiTests @common --no-build @Rest
    $second = $LASTEXITCODE

    if ($first -ne 0 -or $second -ne 0) { exit 1 }
}
finally {
    Pop-Location
}
