# Install / upgrade / uninstall test of the Assembly Studio installer (TASK-101 on the development machine, TASK-103 on
# the clean test machines). Per-user and silent; everything it installs is uninstalled at the end.
#   1. silent install into a temporary folder, timed (NFR-027: <= 2 min)
#   2. files, Start-menu shortcut and uninstall entry present; .asm association off by default (FR-083, FR-084)
#   3. the installed application starts (smoke test) and its bundled NASM runs
#   4. upgrade: a newer version installed over it, with the .asm association selected (FR-084, FR-088)
#   5. uninstall: program files, shortcut, association and uninstall entry removed (FR-085)
#   6. throughout: the user's settings file and a user source file are unchanged
# Exit code 0 when every check passes.
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Installer,
    [Parameter(Mandatory)] [string]$UpgradeInstaller
)

$ErrorActionPreference = 'Stop'
$appId = '{0C031CF2-3FBE-4413-AC58-C2AFFE9CD2CB}_is1'
$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\$appId"
$work = Join-Path ([IO.Path]::GetTempPath()) ("asmstudio-installtest-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
$installDir = Join-Path $work 'Assembly Studio'
$shortcut = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Assembly Studio.lnk'
$userSettings = Join-Path $env:APPDATA 'AssemblyStudio\settings.json'
$failures = 0

function Check([string]$name, [bool]$ok, [string]$detail = '') {
    if ($ok) { Write-Host "PASS  $name $detail" -ForegroundColor Green }
    else { Write-Host "FAIL  $name $detail" -ForegroundColor Red; $script:failures++ }
}

function Hash([string]$path) { if (Test-Path -LiteralPath $path) { (Get-FileHash -LiteralPath $path).Hash } else { 'absent' } }

function Install([string]$setup, [string]$tasks, [string]$log) {
    $args = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-', "/DIR=""$installDir""", "/LOG=""$log""")
    if ($tasks) { $args += "/MERGETASKS=""$tasks""" }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $p = Start-Process -FilePath $setup -ArgumentList $args -Wait -PassThru
    $watch.Stop()
    return @{ ExitCode = $p.ExitCode; Seconds = $watch.Elapsed.TotalSeconds }
}

if (Test-Path -LiteralPath $uninstallKey) { throw "Assembly Studio is already installed for this user; uninstall it before running this test." }
New-Item -ItemType Directory -Force -Path $work | Out-Null
$userFile = Join-Path $work 'my-program.asm'
Set-Content -LiteralPath $userFile -Value "; the user's own file" -Encoding ascii
$settingsBefore = Hash $userSettings
$userFileBefore = Hash $userFile

try {
# 1. Install
$first = Install $Installer '' (Join-Path $work 'install.log')
Check 'install exits 0' ($first.ExitCode -eq 0) "(exit $($first.ExitCode))"
Check 'install time <= 120 s (NFR-027)' ($first.Seconds -le 120) ("({0:N1} s)" -f $first.Seconds)

# 2. Installed state
$exe = Join-Path $installDir 'AssemblyStudio.App.exe'
Check 'application installed' (Test-Path -LiteralPath $exe)
Check 'bundled NASM and its license installed' ((Test-Path -LiteralPath (Join-Path $installDir 'tools\nasm\nasm.exe')) -and (Test-Path -LiteralPath (Join-Path $installDir 'tools\nasm\LICENSE')))
Check 'Start-menu shortcut created' (Test-Path -LiteralPath $shortcut)
Check 'uninstall entry registered for the current user' (Test-Path -LiteralPath $uninstallKey)
Check '.asm association off by default' (-not (Test-Path -LiteralPath 'HKCU:\Software\Classes\AssemblyStudio.asm'))
$sizeMb = (Get-ChildItem -LiteralPath $installDir -Recurse -File | Measure-Object Length -Sum).Sum / 1MB
Check 'installed size <= 300 MB (NFR-028)' ($sizeMb -le 300) ("({0:N1} MB)" -f $sizeMb)

# 3. The installed application runs, offline, with its bundled assembler
$smoke = Join-Path $work 'smoke'
New-Item -ItemType Directory -Force -Path $smoke | Out-Null
$report = Join-Path $smoke 'startup.txt'
$p = Start-Process -FilePath $exe -ArgumentList @('--settings', "`"$smoke\settings.json`"", '--recovery', "`"$smoke\recovery`"", '--log', "`"$smoke\logs`"", '--smoke-report', "`"$report`"") -Wait -PassThru
Check 'installed application starts (smoke test)' (($p.ExitCode -eq 0) -and (Test-Path -LiteralPath $report)) ("(startup {0} ms)" -f (Get-Content -LiteralPath $report -ErrorAction SilentlyContinue))
$nasmVersion = & (Join-Path $installDir 'tools\nasm\nasm.exe') -v 2>&1
Check 'installed NASM runs' ("$nasmVersion" -match 'version 3\.02') "($nasmVersion)"

# 4. Upgrade with the association selected
$upgradeVersion = (Get-ItemProperty -LiteralPath $uninstallKey).DisplayVersion
$second = Install $UpgradeInstaller 'associateasm' (Join-Path $work 'upgrade.log')
$newVersion = (Get-ItemProperty -LiteralPath $uninstallKey).DisplayVersion
Check 'upgrade exits 0' ($second.ExitCode -eq 0) ("(exit {0}, {1:N1} s)" -f $second.ExitCode, $second.Seconds)
Check 'upgrade replaced the version' ($newVersion -ne $upgradeVersion) "($upgradeVersion -> $newVersion)"
Check 'one installation after the upgrade' ((Get-ChildItem 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall' | Where-Object { $_.PSChildName -eq $appId }).Count -eq 1)
$command = (Get-ItemProperty -LiteralPath 'HKCU:\Software\Classes\AssemblyStudio.asm\shell\open\command' -ErrorAction SilentlyContinue).'(default)'
Check '.asm association created when selected' ($command -eq "`"$exe`" `"%1`"") "($command)"
Check 'settings untouched by install and upgrade (FR-088)' ((Hash $userSettings) -eq $settingsBefore)

# 5. Uninstall
$uninstaller = Join-Path $installDir 'unins000.exe'
Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART') -Wait | Out-Null
$deadline = (Get-Date).AddSeconds(60)
while ((Test-Path -LiteralPath $exe) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 500 }  # the uninstaller finishes from a copy of itself
Start-Sleep -Seconds 2
Check 'program files removed (FR-085)' (-not (Test-Path -LiteralPath $exe) -and -not (Get-ChildItem -LiteralPath $installDir -Recurse -File -ErrorAction SilentlyContinue))
Check 'Start-menu shortcut removed' (-not (Test-Path -LiteralPath $shortcut))
Check 'uninstall entry removed' (-not (Test-Path -LiteralPath $uninstallKey))
Check '.asm association removed' (-not (Test-Path -LiteralPath 'HKCU:\Software\Classes\AssemblyStudio.asm'))
Check 'user source file kept (FR-085)' ((Hash $userFile) -eq $userFileBefore)
Check 'settings untouched by uninstall (FR-085)' ((Hash $userSettings) -eq $settingsBefore)
}
finally {
    # Never leave a test installation behind, even when a step above failed.
    if (Test-Path -LiteralPath $uninstallKey) {
        Write-Host 'Removing the test installation...' -ForegroundColor Yellow
        $location = (Get-ItemProperty -LiteralPath $uninstallKey).InstallLocation
        Start-Process -FilePath (Join-Path $location 'unins000.exe') -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART') -Wait | Out-Null
        Start-Sleep -Seconds 5
    }
}

Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
if ($failures -gt 0) { Write-Host "$failures check(s) failed." -ForegroundColor Red; exit 1 }
Write-Host 'All installer checks passed.' -ForegroundColor Green
