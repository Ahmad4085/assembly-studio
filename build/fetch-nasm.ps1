# Downloads and verifies the pinned NASM 3.02 win64 binary per ADR-0005.
# Tooling is extracted to tools/nasm/ (git-ignored).
[CmdletBinding()]
param(
    [string]$TargetDir = '',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$Version = '3.02'
$DownloadUrl = "https://www.nasm.us/pub/nasm/releasebuilds/$Version/win64/nasm-$Version-win64.zip"
$ExpectedZipSha256 = '161d0bfaff53c2f9e9f3e69fd0672323ebabafd1268976a5cec11be92a19aee7'

$ExpectedFileHashes = [ordered]@{
    'nasm.exe'    = '04ec2385879f7e1c45dbe76c4020970555de48eeb97c23f59620ede061328f51'
    'ndisasm.exe' = '81f1de14f5b2b4c443cc148897b6102fff67202789efca82c5b6e1b8b9c81e17'
    'LICENSE'     = '222b65a5fab7ba8ade645970ad21bd5c2e2e6533897d59d1ca0914681c1f9da6'
}

$RepoRoot = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($TargetDir)) {
    $TargetDir = Join-Path $RepoRoot 'tools\nasm'
}

$nasmExe = Join-Path $TargetDir 'nasm.exe'
$licensePath = Join-Path $TargetDir 'LICENSE'
$sourceTxt = Join-Path $TargetDir 'SOURCE.txt'
$shaSums = Join-Path $TargetDir 'SHA256SUMS.txt'

if (-not (Test-Path -LiteralPath $TargetDir)) {
    New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null
}

function Get-Sha256([string]$Path) {
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
}

# Idempotency check: if all expected files exist and match hashes and version check passes
if (-not $Force) {
    $allPresentAndValid = $true
    foreach ($file in $ExpectedFileHashes.Keys) {
        $p = Join-Path $TargetDir $file
        if ((-not (Test-Path -LiteralPath $p)) -or ((Get-Sha256 $p) -ne $ExpectedFileHashes[$file])) {
            $allPresentAndValid = $false
            break
        }
    }

    if ($allPresentAndValid -and (Test-Path -LiteralPath $sourceTxt) -and (Test-Path -LiteralPath $shaSums)) {
        $versionOutput = & $nasmExe -v 2>&1
        if ($versionOutput -match "version 3\.02") {
            Write-Host "NASM $Version already installed in $TargetDir and verified ($versionOutput)." -ForegroundColor Green
            exit 0
        }
    }
}

Write-Host "Fetching NASM $Version win64 from $DownloadUrl..." -ForegroundColor Cyan

$zipPath = Join-Path $env:TEMP "nasm-$Version-win64.zip"
$partPath = "$zipPath.part"

# Download zip if not already downloaded or hash mismatch
$needDownload = $true
if (Test-Path -LiteralPath $zipPath) {
    $existingHash = Get-Sha256 $zipPath
    if ($existingHash -eq $ExpectedZipSha256) {
        Write-Host "Reusing cached archive in temp directory ($zipPath)."
        $needDownload = $false
    }
}

if ($needDownload) {
    Write-Host "Downloading $DownloadUrl..."
    $webClient = [System.Net.WebClient]::new()
    try {
        $webClient.DownloadFile($DownloadUrl, $partPath)
        $actualPartHash = Get-Sha256 $partPath
        if ($actualPartHash -ne $ExpectedZipSha256) {
            throw "SHA-256 verification failed for downloaded archive.`nExpected: $ExpectedZipSha256`nActual:   $actualPartHash"
        }
        Move-Item -LiteralPath $partPath -Destination $zipPath -Force
    } finally {
        $webClient.Dispose()
        if (Test-Path -LiteralPath $partPath) {
            Remove-Item -LiteralPath $partPath -Force
        }
    }
}

# Verify SHA256 of archive
$actualHash = Get-Sha256 $zipPath
if ($actualHash -ne $ExpectedZipSha256) {
    throw "SHA-256 verification failed for $zipPath.`nExpected: $ExpectedZipSha256`nActual:   $actualHash"
}
Write-Host "Archive SHA-256 verified: $actualHash" -ForegroundColor Green

# Extract required binaries and license directly into $TargetDir
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    foreach ($entry in $zip.Entries) {
        $fileName = [System.IO.Path]::GetFileName($entry.FullName)
        if ($ExpectedFileHashes.Contains($fileName)) {
            $destFile = Join-Path $TargetDir $fileName
            if (Test-Path -LiteralPath $destFile) {
                Remove-Item -LiteralPath $destFile -Force
            }
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destFile)
            Write-Host "  Extracted $fileName -> $destFile"
        }
    }
} finally {
    $zip.Dispose()
}

# Verify extracted files match expected SHA-256
$manifestLines = [System.Collections.Generic.List[string]]::new()
foreach ($file in $ExpectedFileHashes.Keys) {
    $filePath = Join-Path $TargetDir $file
    if (-not (Test-Path -LiteralPath $filePath)) {
        throw "Expected extracted file $file was not found in $TargetDir"
    }
    $fileHash = Get-Sha256 $filePath
    if ($fileHash -ne $ExpectedFileHashes[$file]) {
        throw "Extracted file $file SHA-256 mismatch: expected $($ExpectedFileHashes[$file]), got $fileHash"
    }
    $manifestLines.Add("$fileHash  $file")
}

# Write SHA256SUMS.txt (LF line endings, UTF-8 without BOM)
$shaSumsContent = ($manifestLines -join "`n") + "`n"
[System.IO.File]::WriteAllText($shaSums, $shaSumsContent, [System.Text.UTF8Encoding]::new($false))

# Write SOURCE.txt with deterministic provenance details (no timestamps)
$sourceInfo = "source=$DownloadUrl`nversion=$Version`narchive_sha256=$ExpectedZipSha256`nlicense=BSD-2-Clause`n"
[System.IO.File]::WriteAllText($sourceTxt, $sourceInfo, [System.Text.UTF8Encoding]::new($false))

# Verify executable works and reports 3.02
$verifyOutput = & $nasmExe -v 2>&1
if ($verifyOutput -notmatch "version 3\.02") {
    throw "Installed nasm.exe failed version check: $verifyOutput"
}

Write-Host "NASM $Version ready in $($TargetDir): $verifyOutput" -ForegroundColor Green
exit 0
