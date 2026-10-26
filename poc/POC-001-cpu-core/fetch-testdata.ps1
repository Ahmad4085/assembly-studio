# Downloads the SingleStepTests 8086 (MIT) cases used by POC-001, pinned to one commit.
# Source: https://github.com/SingleStepTests/8086 (Intel P80C86A-2 captures). Data stays out of Git (see .gitignore).
param(
    [string]$Commit = 'e71c68d215a6bb8c356bd4cb3842de3bef345ca9',
    [string]$Target = (Join-Path $PSScriptRoot 'testdata')
)
$ErrorActionPreference = 'Stop'

$opcodes = @('00','01','02','03','04','05','28','29','2A','2B','2C','2D','38','39','3A','3B','3C','3D',
             '88','89','8A','8B','C6','C7','C3','CD','E8','E9','EB')
$opcodes += 0x50..0x5F | ForEach-Object { '{0:X2}' -f $_ }
$opcodes += 0x70..0x7F | ForEach-Object { '{0:X2}' -f $_ }
$opcodes += 0xB0..0xBF | ForEach-Object { '{0:X2}' -f $_ }
$groups = foreach ($op in '80','81','82','83') { foreach ($reg in 0, 5, 7) { "$op.$reg" } }

New-Item -ItemType Directory -Force -Path $Target | Out-Null
$base = "https://raw.githubusercontent.com/SingleStepTests/8086/$Commit/v1"
$files = @('metadata.json') + (($opcodes + $groups) | ForEach-Object { "$_.json.gz" })
foreach ($file in $files) {
    $out = Join-Path $Target $file
    if (-not (Test-Path $out)) {
        Invoke-WebRequest -Uri "$base/$file" -OutFile $out -UseBasicParsing
    }
}
"source=https://github.com/SingleStepTests/8086`ncommit=$Commit`nlicense=MIT`nfiles=$($files.Count)" |
    Set-Content -Encoding utf8 (Join-Path $Target 'SOURCE.txt')
Write-Host "Downloaded $($files.Count) files to $Target"
