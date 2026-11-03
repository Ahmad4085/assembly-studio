# Builds the Assembly Studio installer (ADR-0009, TASK-101):
#   1. dotnet publish src/AssemblyStudio.App -c Release -r win-x64 --self-contained (no trimming, no single file; ReadyToRun from the project, TASK-108)
#   2. checks the publish folder holds what the installer must ship (the app, the runtime, the bundled NASM and its license)
#   3. compiles build/installer/AssemblyStudio.iss with the pinned Inno Setup (build/fetch-innosetup.ps1)
# Output: artifacts/installer/AssemblyStudio-<version>-setup.exe. Prints the published size and the installer size (NFR-028).
# Needs tools/nasm (build/fetch-nasm.ps1) and tools/innosetup (build/fetch-innosetup.ps1).
[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$iscc = Join-Path $RepoRoot 'tools\innosetup\ISCC.exe'
$publishDir = Join-Path $RepoRoot 'artifacts\publish\win-x64'
$outputDir = Join-Path $RepoRoot 'artifacts\installer'

if (-not (Test-Path -LiteralPath $iscc)) { throw "Inno Setup not found at $iscc. Run build/fetch-innosetup.ps1 first." }
if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot 'tools\nasm\nasm.exe'))) { throw "NASM not found in tools\nasm. Run build/fetch-nasm.ps1 first." }

if (Test-Path -LiteralPath $publishDir) { Remove-Item -LiteralPath $publishDir -Recurse -Force }
Write-Host "Publishing self-contained win-x64 ($Configuration)..." -ForegroundColor Cyan
& dotnet publish (Join-Path $RepoRoot 'src\AssemblyStudio.App\AssemblyStudio.App.csproj') -c $Configuration -r win-x64 --self-contained true `
    -p:PublishTrimmed=false -p:PublishSingleFile=false -o $publishDir --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)." }

$required = @('AssemblyStudio.App.exe', 'AssemblyStudio.App.dll', 'coreclr.dll', 'PresentationFramework.dll', 'tools\nasm\nasm.exe', 'tools\nasm\LICENSE')
foreach ($file in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $publishDir $file))) { throw "The publish folder lacks $file." }
}

$exe = Get-Item -LiteralPath (Join-Path $publishDir 'AssemblyStudio.App.exe')
$version = $exe.VersionInfo.ProductVersion.Split('+')[0]
$publishBytes = (Get-ChildItem -LiteralPath $publishDir -Recurse -File | Measure-Object -Property Length -Sum).Sum

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
Write-Host "Compiling the installer for version $version..." -ForegroundColor Cyan
& $iscc /Q "/DPublishDir=$publishDir" "/DAppVersion=$version" "/DOutputDir=$outputDir" (Join-Path $PSScriptRoot 'AssemblyStudio.iss')
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed ($LASTEXITCODE)." }

$installer = Get-Item -LiteralPath (Join-Path $outputDir "AssemblyStudio-$version-setup.exe")
$sha = (Get-FileHash -Algorithm SHA256 -LiteralPath $installer.FullName).Hash.ToLowerInvariant()
Write-Host ("Published files: {0:N1} MB ({1:N0} files)" -f ($publishBytes / 1MB), (Get-ChildItem -LiteralPath $publishDir -Recurse -File).Count)
Write-Host ("Installer: {0} ({1:N1} MB), SHA-256 {2}" -f $installer.FullName, ($installer.Length / 1MB), $sha) -ForegroundColor Green
