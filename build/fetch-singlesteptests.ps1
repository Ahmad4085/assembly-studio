# Downloads the SingleStepTests hardware oracle data (MIT) used by the engine tests (ADR-0008, ADR-0003).
#   8086 v1 (primary oracle):   pinned commit, verified against build/singlesteptests-8086-v1.sha256
#   8088 v2 (secondary oracle): -Include8088, pinned commit, verified against build/singlesteptests-8088-v2.sha256
# The data stays outside Git (testdata/ is git-ignored). Every file, cached or downloaded, is checked against the
# committed SHA-256 manifest. Any missing, failed or mismatching file makes the script exit with code 1.
[CmdletBinding()]
param(
    [string]$TargetDir = '',
    [switch]$Include8088,
    [switch]$SubsetOnly,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$RepoRoot = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($TargetDir)) {
    $TargetDir = Join-Path $RepoRoot 'testdata\singlesteptests'
}

$Sets = @(
    @{ Name = '8086'; Repo = 'SingleStepTests/8086'; Commit = 'e71c68d215a6bb8c356bd4cb3842de3bef345ca9'; Folder = 'v1'
       Manifest = 'singlesteptests-8086-v1.sha256'; Cpu = 'Intel P80C86A-2' }
)
if ($Include8088) {
    $Sets += @{ Name = '8088'; Repo = 'SingleStepTests/8088'; Commit = 'aea84484abc79d09639d855b7b0ab32bc9e4dbeb'; Folder = 'v2'
                Manifest = 'singlesteptests-8088-v2.sha256'; Cpu = 'AMD D8088 8441DMA (C)1982' }
}

# The POC-001 / MVP core subset (8086 only). Faster to fetch, enough for the first engine tasks.
function Get-SubsetNames {
    $ops = @('00','01','02','03','04','05','28','29','2A','2B','2C','2D','38','39','3A','3B','3C','3D',
             '88','89','8A','8B','C6','C7','C3','CD','E8','E9','EB')
    $ops += 0x50..0x5F | ForEach-Object { '{0:X2}' -f $_ }
    $ops += 0x70..0x7F | ForEach-Object { '{0:X2}' -f $_ }
    $ops += 0xB0..0xBF | ForEach-Object { '{0:X2}' -f $_ }
    $names = @('metadata.json') + ($ops | ForEach-Object { "$_.json.gz" })
    foreach ($op in '80','81','82','83') {
        foreach ($reg in 0, 5, 7) { $names += "$op.$reg.json.gz" }
    }
    return $names
}

function Get-Sha256([string]$Path) {
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
}

$failures = [System.Collections.Generic.List[string]]::new()
$webClient = [System.Net.WebClient]::new()

foreach ($set in $Sets) {
    $manifestPath = Join-Path $PSScriptRoot $set.Manifest
    if (-not (Test-Path -LiteralPath $manifestPath)) {
        throw "Manifest $manifestPath not found. The $($set.Name) oracle set cannot be verified, so it is not fetched."
    }
    $expected = [ordered]@{}
    foreach ($line in Get-Content -LiteralPath $manifestPath) {
        if ($line -match '^([0-9a-f]{64})  (\S+)$') { $expected[$Matches[2]] = $Matches[1] }
    }
    if ($expected.Count -eq 0) { throw "Manifest $manifestPath is empty or unreadable." }

    $names = @($expected.Keys)
    if ($SubsetOnly -and $set.Name -eq '8086') {
        $names = Get-SubsetNames
        foreach ($n in $names) {
            if (-not $expected.Contains($n)) { throw "Subset file $n is not in $($set.Manifest)." }
        }
    }

    $dir = Join-Path $TargetDir $set.Name
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    Write-Host "SingleStepTests $($set.Name) $($set.Folder) @ $($set.Commit): $($names.Count) files -> $dir" -ForegroundColor Cyan

    $cached = 0; $downloaded = 0
    foreach ($name in $names) {
        $dest = Join-Path $dir $name
        if ((-not $Force) -and (Test-Path -LiteralPath $dest) -and ((Get-Sha256 $dest) -eq $expected[$name])) {
            $cached++
            continue
        }

        $part = "$dest.part"
        $url = "https://raw.githubusercontent.com/$($set.Repo)/$($set.Commit)/$($set.Folder)/$name"
        try {
            $webClient.DownloadFile($url, $part)
            $actual = Get-Sha256 $part
            if ($actual -ne $expected[$name]) {
                throw "SHA-256 mismatch (expected $($expected[$name]), got $actual)"
            }
            Move-Item -LiteralPath $part -Destination $dest -Force
            $downloaded++
            if ($downloaded % 25 -eq 0) { Write-Host "  downloaded $downloaded files..." }
        }
        catch {
            $failures.Add("$($set.Name)/$name : $($_.Exception.Message)")
        }
        finally {
            if (Test-Path -LiteralPath $part) { Remove-Item -LiteralPath $part -Force }
        }
    }

    $known = @($expected.Keys) + @('SHA256SUMS.txt', 'SOURCE.txt')
    $extra = @(Get-ChildItem -LiteralPath $dir -File | Where-Object { $known -notcontains $_.Name } | ForEach-Object { $_.Name })
    if ($extra.Count -gt 0) {
        Write-Warning "Files in $dir that are not in $($set.Manifest) (ignored by tests, safe to delete): $($extra -join ', ')"
    }

    # Deterministic metadata: identical content on every run for the same pinned set.
    Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $dir 'SHA256SUMS.txt') -Force
    $source = "source=https://github.com/$($set.Repo)`ncommit=$($set.Commit)`nfolder=$($set.Folder)`nlicense=MIT`ncpu=$($set.Cpu)`nfiles=$($expected.Count)`n"
    [System.IO.File]::WriteAllText((Join-Path $dir 'SOURCE.txt'), $source, [System.Text.UTF8Encoding]::new($false))

    Write-Host "  $($set.Name): $cached verified from cache, $downloaded downloaded and verified." -ForegroundColor Green
}

$webClient.Dispose()

if ($failures.Count -gt 0) {
    Write-Host "FAILED: $($failures.Count) file(s) could not be fetched or verified:" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}
Write-Host 'SingleStepTests oracle data complete and verified.' -ForegroundColor Green
exit 0
