# Downloads, verifies and installs the pinned Inno Setup 7.1.0 (x64) used to compile the installer (ADR-0009, TASK-101).
# It is installed for the current user only, into tools/innosetup (git-ignored): no administrator rights, no Start-menu
# icons, no optional tasks (such as the .iss file association). Approved by the user on 2026-10-30.
# Inno Setup is free for any use (Inno Setup License); installers built with it keep its notice (TASK-102).
[CmdletBinding()]
param(
    [string]$TargetDir = '',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$Version = '7.1.0'
$DownloadUrl = 'https://github.com/jrsoftware/issrc/releases/download/is-7_1_0/innosetup-7.1.0-x64.exe'
# The SHA-256 digest GitHub publishes for this release asset (released 2026-08-12).
$ExpectedSha256 = '0362a383ed217d4c4239b5933866dd96d3eb2102737da92f80f6057a4b40df2f'

$RepoRoot = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($TargetDir)) {
    $TargetDir = Join-Path $RepoRoot 'tools\innosetup'
}

$iscc = Join-Path $TargetDir 'ISCC.exe'
$sourceTxt = Join-Path $TargetDir 'SOURCE.txt'

# ISCC.exe carries no file version; the compiler library contains the version string.
function Test-InstalledVersion {
    $compiler = Join-Path $TargetDir 'ISCmplr.dll'
    if (-not (Test-Path -LiteralPath $iscc) -or -not (Test-Path -LiteralPath $compiler)) { return $false }
    $text = [Text.Encoding]::GetEncoding(28591).GetString([IO.File]::ReadAllBytes($compiler))
    return $text.Contains($Version)
}

if (-not $Force -and (Test-Path -LiteralPath $sourceTxt) -and (Test-InstalledVersion)) {
    Write-Host "Inno Setup $Version already installed in $TargetDir." -ForegroundColor Green
    exit 0
}

$download = Join-Path ([IO.Path]::GetTempPath()) "innosetup-$Version-x64.exe"
Write-Host "Fetching Inno Setup $Version from $DownloadUrl..." -ForegroundColor Cyan
Invoke-WebRequest -Uri $DownloadUrl -OutFile $download -UseBasicParsing

$actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $download).Hash.ToLowerInvariant()
if ($actual -ne $ExpectedSha256) {
    Remove-Item -LiteralPath $download -Force
    throw "SHA-256 mismatch for $DownloadUrl. Expected $ExpectedSha256, got $actual. Nothing was installed."
}

Write-Host "Verified ($actual). Installing for the current user into $TargetDir..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null
$log = Join-Path ([IO.Path]::GetTempPath()) "innosetup-$Version-install.log"
$arguments = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-', '/CURRENTUSER', '/NOICONS', '/TASKS=""', "/DIR=""$TargetDir""", "/LOG=""$log""")
$process = Start-Process -FilePath $download -ArgumentList $arguments -Wait -PassThru
Remove-Item -LiteralPath $download -Force
if ($process.ExitCode -ne 0) {
    throw "The Inno Setup installer exited with code $($process.ExitCode). See $log."
}

if (-not (Test-InstalledVersion)) {
    throw "The Inno Setup compiler in $TargetDir isn't version $Version."
}

@(
    "Inno Setup $Version (x64), installed for the current user by build/fetch-innosetup.ps1",
    "Source: $DownloadUrl",
    "SHA-256: $ExpectedSha256",
    "License: Inno Setup License (free for any use, including commercial; keep the notice). See license.txt in this folder.",
    "Installed: $(Get-Date -Format 'yyyy-MM-dd HH:mm zzz')",
    "Uninstall: Windows Settings > Apps > Inno Setup, or unins000.exe in this folder."
) | Set-Content -LiteralPath $sourceTxt -Encoding utf8

Write-Host "Inno Setup $Version ready: $iscc" -ForegroundColor Green
