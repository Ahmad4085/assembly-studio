# Line coverage for the product assemblies under Microsoft.Testing.Platform (ADR-0008: coverlet.MTP, MIT; NFR-021).
# Runs every test project in AssemblyStudio.slnx with coverlet, merges the line hits per product assembly (one assembly
# can be exercised by several test projects) and writes artifacts/coverage/summary.md next to the raw Cobertura files.
# Reports only. Threshold enforcement is planned for when the engine exists (backlog TASK-004, out of scope).
# Exit code 1 if a test project fails or produces no coverage file.
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$OutputDir = '',
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path $PSScriptRoot -Parent
$Solution = Join-Path $RepoRoot 'AssemblyStudio.slnx'
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot 'artifacts\coverage'
}
if (Test-Path -LiteralPath $OutputDir) {
    Remove-Item -LiteralPath $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

if (-not $NoBuild) {
    & dotnet build $Solution -c $Configuration --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }
}

$testProjects = @(([xml](Get-Content -LiteralPath $Solution -Raw)).SelectNodes('//Project') |
    ForEach-Object { $_.GetAttribute('Path') } |
    Where-Object { $_ -like 'tests/*' })
if ($testProjects.Count -eq 0) { throw "No test projects found in $Solution." }

$failures = [System.Collections.Generic.List[string]]::new()
$reports = [System.Collections.Generic.List[string]]::new()

foreach ($project in $testProjects) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($project)
    $resultsDir = Join-Path $OutputDir $name
    Write-Host "Coverage: $name" -ForegroundColor Cyan
    # Performance tests (trait Category=Performance) measure time, which instrumentation distorts (DEF-009); they run in
    # the normal test runs instead.
    & dotnet test --project (Join-Path $RepoRoot $project) -c $Configuration --no-build `
        --coverlet --coverlet-output-format cobertura --results-directory $resultsDir --filter-not-trait "Category=Performance"
    if ($LASTEXITCODE -ne 0) {
        $failures.Add("$name : dotnet test exit $LASTEXITCODE")
    }
    $files = @(Get-ChildItem -LiteralPath $resultsDir -Filter 'coverage.cobertura.*.xml' -ErrorAction SilentlyContinue)
    if ($files.Count -eq 0) {
        $failures.Add("$name : no Cobertura file written")
    }
    $files | ForEach-Object { $reports.Add($_.FullName) }
}

# Merge: a line counts as covered if any test project hit it.
$lines = @{}          # "assembly|file|line" -> covered (bool)
$assemblies = @{}     # assembly -> $true
foreach ($report in $reports) {
    $xml = [xml](Get-Content -LiteralPath $report -Raw)
    foreach ($package in $xml.SelectNodes('//package')) {
        $assembly = $package.GetAttribute('name')
        $assemblies[$assembly] = $true
        foreach ($class in $package.SelectNodes('classes/class')) {
            $file = $class.GetAttribute('filename')
            foreach ($line in $class.SelectNodes('lines/line')) {
                $key = "$assembly|$file|$($line.GetAttribute('number'))"
                $hit = [long]$line.GetAttribute('hits') -gt 0
                $lines[$key] = ($lines.ContainsKey($key) -and $lines[$key]) -or $hit
            }
        }
    }
}

$rows = foreach ($assembly in ($assemblies.Keys | Sort-Object)) {
    $keys = @($lines.Keys | Where-Object { $_.StartsWith("$assembly|") })
    $covered = @($keys | Where-Object { $lines[$_] }).Count
    [pscustomobject]@{ Assembly = $assembly; Coverable = $keys.Count; Covered = $covered }
}

function Format-Rate([int]$Covered, [int]$Coverable) {
    if ($Coverable -eq 0) { return 'n/a (no coverable lines)' }
    return ('{0:0.0}%' -f (100.0 * $Covered / $Coverable))
}

$totalCoverable = ($rows | Measure-Object -Property Coverable -Sum).Sum
$totalCovered = ($rows | Measure-Object -Property Covered -Sum).Sum
$commit = 'unknown'; $dirty = ''
try {
    $ErrorActionPreference = 'Continue'
    $head = & git -C $RepoRoot rev-parse --short HEAD 2>$null
    if ($LASTEXITCODE -eq 0 -and $head) {
        $commit = $head
        if (& git -C $RepoRoot status --porcelain --untracked-files=no 2>$null) { $dirty = ' (with uncommitted changes)' }
    }
}
catch { }
finally { $ErrorActionPreference = 'Stop' }

$md = [System.Collections.Generic.List[string]]::new()
$md.Add('# Line coverage summary')
$md.Add('')
$md.Add("Generated $(Get-Date -Format 'yyyy-MM-dd HH:mm zzz') by ``build/coverage.ps1`` at commit ``$commit``$dirty, configuration $Configuration.")
$md.Add("Tool: coverlet.MTP (Microsoft.Testing.Platform). Sources: $($reports.Count) Cobertura file(s) from $($testProjects.Count) test project(s).")
$md.Add('')
$md.Add('| Assembly | Coverable lines | Covered lines | Line coverage |')
$md.Add('|---|---|---|---|')
foreach ($row in $rows) {
    $md.Add("| $($row.Assembly) | $($row.Coverable) | $($row.Covered) | $(Format-Rate $row.Covered $row.Coverable) |")
}
$md.Add("| **Total** | $totalCoverable | $totalCovered | $(Format-Rate $totalCovered $totalCoverable) |")
if ($failures.Count -gt 0) {
    $md.Add('')
    $md.Add('**Incomplete:** ' + ($failures -join '; '))
}
$summaryPath = Join-Path $OutputDir 'summary.md'
[System.IO.File]::WriteAllLines($summaryPath, $md, [System.Text.UTF8Encoding]::new($false))

Write-Host ''
$rows | Select-Object Assembly, Coverable, Covered, @{ Name = 'LineCoverage'; Expression = { Format-Rate $_.Covered $_.Coverable } } |
    Format-Table -AutoSize | Out-String | Write-Host
Write-Host "Total: $(Format-Rate $totalCovered $totalCoverable) ($totalCovered/$totalCoverable lines). Summary: $summaryPath"

if ($failures.Count -gt 0) {
    Write-Host "FAILED: $($failures -join '; ')" -ForegroundColor Red
    exit 1
}
exit 0
