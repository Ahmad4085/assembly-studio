# Downloads SingleStepTests 8086 (MIT) hardware capture oracle cases, pinned to commit e71c68d215a6bb8c356bd4cb3842de3bef345ca9.
# Secondary oracle: 8088 v2 captured cases (MIT) via -Include8088 switch.
# ADR-0008: External oracle data stays outside of Git (see .gitignore).
[CmdletBinding()]
param(
    [string]$Commit = 'e71c68d215a6bb8c356bd4cb3842de3bef345ca9',
    [string]$TargetDir = '',
    [switch]$Include8088,
    [switch]$SubsetOnly,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($TargetDir)) {
    $TargetDir = Join-Path $RepoRoot 'testdata\singlesteptests'
}

$Target8086 = Join-Path $TargetDir '8086'
if (-not (Test-Path $Target8086)) {
    New-Item -ItemType Directory -Force -Path $Target8086 | Out-Null
}

Write-Host "Fetching SingleStepTests 8086 oracle data (commit: $Commit)..." -ForegroundColor Cyan

# 1. Determine file list
$fileListPath = Join-Path $PSScriptRoot 'singlesteptests-8086-v1-files.txt'
$filesToFetch = [System.Collections.Generic.List[string]]::new()

if ($SubsetOnly) {
    # The POC-001 / MVP core subset
    $subsetOps = @('00','01','02','03','04','05','28','29','2A','2B','2C','2D','38','39','3A','3B','3C','3D',
                   '88','89','8A','8B','C6','C7','C3','CD','E8','E9','EB')
    $subsetOps += 0x50..0x5F | ForEach-Object { '{0:X2}' -f $_ }
    $subsetOps += 0x70..0x7F | ForEach-Object { '{0:X2}' -f $_ }
    $subsetOps += 0xB0..0xBF | ForEach-Object { '{0:X2}' -f $_ }
    $filesToFetch.Add('metadata.json')
    foreach ($op in $subsetOps) { $filesToFetch.Add("$op.json.gz") }
    foreach ($op in '80','81','82','83') {
        foreach ($reg in 0, 5, 7) { $filesToFetch.Add("$op.$reg.json.gz") }
    }
} elseif (Test-Path $fileListPath) {
    Get-Content $fileListPath | ForEach-Object {
        $trimmed = $_.Trim()
        if (-not [string]::IsNullOrWhiteSpace($trimmed)) {
            $filesToFetch.Add($trimmed)
        }
    }
} else {
    Write-Host "Querying GitHub commit tree for v1 files..."
    $tree = Invoke-RestMethod -Uri "https://api.github.com/repos/SingleStepTests/8086/git/trees/$Commit?recursive=1" -Headers @{"User-Agent"="PowerShell"}
    foreach ($item in ($tree.tree | Where-Object { $_.path -like "v1/*" })) {
        $filesToFetch.Add($item.path.Substring(3))
    }
}

Write-Host "Total 8086 oracle files to verify/download: $($filesToFetch.Count)"

# 2. Download files with caching
$base8086Url = "https://raw.githubusercontent.com/SingleStepTests/8086/$Commit/v1"
$pocCacheDir = Join-Path $RepoRoot 'poc\POC-001-cpu-core\testdata'
$webClient = [System.Net.WebClient]::new()

$downloadCount = 0
$cachedCount = 0

foreach ($fileName in $filesToFetch) {
    $dest = Join-Path $Target8086 $fileName
    if ((-not $Force) -and (Test-Path $dest)) {
        $cachedCount++
        continue
    }

    # Check POC cache first
    $pocFile = Join-Path $pocCacheDir $fileName
    if ((-not $Force) -and (Test-Path $pocFile)) {
        Copy-Item $pocFile $dest
        $cachedCount++
        continue
    }

    $url = "$base8086Url/$fileName"
    try {
        $webClient.DownloadFile($url, $dest)
        $downloadCount++
        if ($downloadCount % 25 -eq 0) {
            Write-Host "  Downloaded $downloadCount files..."
        }
    } catch {
        Write-Warning "Failed to download $url : $_"
    }
}

$webClient.Dispose()
Write-Host "8086 files ready ($cachedCount cached, $downloadCount downloaded)." -ForegroundColor Green

# 3. Generate SHA-256 manifest and SOURCE.txt
Write-Host "Computing SHA-256 checksums..."
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$manifestLines = [System.Collections.Generic.List[string]]::new()

foreach ($fileName in ($filesToFetch | Sort-Object)) {
    $filePath = Join-Path $Target8086 $fileName
    if (Test-Path $filePath) {
        $bytes = [System.IO.File]::ReadAllBytes($filePath)
        $hashBytes = $sha256.ComputeHash($bytes)
        $hashHex = [BitConverter]::ToString($hashBytes).Replace('-', '').ToLowerInvariant()
        $manifestLines.Add("$hashHex  $fileName")
    }
}
$sha256.Dispose()

$manifestPath = Join-Path $Target8086 'SHA256SUMS.txt'
[System.IO.File]::WriteAllLines($manifestPath, $manifestLines)

$sourceContent = @"
source=https://github.com/SingleStepTests/8086
commit=$Commit
license=MIT
cpu=Intel P80C86A-2
files=$($manifestLines.Count)
updated=$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')
"@
Set-Content -Path (Join-Path $Target8086 'SOURCE.txt') -Value $sourceContent -Encoding utf8

# 4. Optional secondary 8088 oracle
if ($Include8088) {
    Write-Host "Fetching secondary 8088 v2 oracle data..." -ForegroundColor Cyan
    $Target8088 = Join-Path $TargetDir '8088'
    if (-not (Test-Path $Target8088)) {
        New-Item -ItemType Directory -Force -Path $Target8088 | Out-Null
    }
    $meta8088Url = "https://raw.githubusercontent.com/SingleStepTests/8088/main/v2/metadata.json"
    $meta8088Path = Join-Path $Target8088 'metadata.json'
    Invoke-WebRequest -Uri $meta8088Url -OutFile $meta8088Path -UseBasicParsing

    $source8088 = @"
source=https://github.com/SingleStepTests/8088
branch=main
version=2.0.0
license=MIT
cpu=AMD D8088 8441DMA (C)1982
updated=$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')
"@
    Set-Content -Path (Join-Path $Target8088 'SOURCE.txt') -Value $source8088 -Encoding utf8
    Write-Host "8088 metadata saved to $Target8088" -ForegroundColor Green
}

Write-Host "SingleStepTests oracle data setup complete: $($manifestLines.Count) files in $Target8086" -ForegroundColor Green
