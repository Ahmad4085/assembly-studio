# License inventory, third-party notices, SBOM and installer license page (TASK-102; FR-090, NFR-030, NFR-031; ADR-0009, D-025,
# D-026). Reads the ACTUAL published application, so what is listed is what ships:
#   1. maps every file of the publish folder to a component: Assembly Studio's own files, the packages and runtime packs named in
#      <app>.deps.json, the WPF resource folders (found in the Windows Desktop runtime pack) and the bundled NASM (tools\nasm);
#      an unknown file stops the script;
#   2. checks the bundled NASM against tools\nasm\SHA256SUMS.txt;
#   3. applies Microsoft's per-file licenses (build/installer/licenses/license-information-windows.md);
#   4. writes THIRD-PARTY-NOTICES.txt (repository root; About shows it), build/installer/license-agreement.txt (the installer's
#      license page), docs/09-release/license-inventory.md and docs/09-release/sbom.spdx.json (SPDX 2.3).
# License texts come from the restored NuGet packages and from build/installer/licenses (Microsoft's texts, which are not in the
# packages). Run after `dotnet publish` (build-installer.ps1 does), or point -PublishDir at another publish folder.
#
#   powershell -ExecutionPolicy Bypass -File build/installer/license-inventory.ps1 [-PublishDir <folder>] [-CopyToPublish]
param(
    [string]$PublishDir,
    [switch]$CopyToPublish
)
$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $PublishDir) { $PublishDir = Join-Path $RepoRoot 'artifacts\publish\win-x64' }
$PublishDir = (Resolve-Path -LiteralPath $PublishDir).Path
$Packages = Join-Path $env:USERPROFILE '.nuget\packages'
$LicenseDir = Join-Path $PSScriptRoot 'licenses'
$Utf8 = New-Object System.Text.UTF8Encoding $false
$Utf8Bom = New-Object System.Text.UTF8Encoding $true

function Read-Text([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { throw "License source not found: $Path" }
    return ([IO.File]::ReadAllText($Path)).Replace("`r`n", "`n").TrimEnd()
}

function Rule([string]$Title) {
    $line = '=' * 78
    return "`n$line`n$Title`n$line`n"
}

# ---- 1. what deps.json says about the published files
$depsFile = Get-ChildItem -LiteralPath $PublishDir -Filter '*.deps.json' | Select-Object -First 1
if (-not $depsFile) { throw "No .deps.json in $PublishDir. Publish the application first." }
$deps = Get-Content -LiteralPath $depsFile.FullName -Raw | ConvertFrom-Json
$ridTarget = $deps.targets.PSObject.Properties | Where-Object { $_.Name -like '*/*' } | Select-Object -First 1
$fileToLib = @{}
foreach ($lib in $ridTarget.Value.PSObject.Properties) {
    foreach ($kind in 'runtime', 'native', 'resources') {
        $section = $lib.Value.$kind
        if ($section) { foreach ($f in $section.PSObject.Properties) { $fileToLib[[IO.Path]::GetFileName($f.Name)] = $lib.Name } }
    }
}
$libraries = @{}
foreach ($l in $deps.libraries.PSObject.Properties) { $libraries[$l.Name] = $l.Value.type }
$appLib = ($libraries.Keys | Where-Object { $_ -like 'AssemblyStudio.App/*' } | Select-Object -First 1)
$AppVersion = $appLib.Split('/')[1]
function LibVersion([string]$prefix) {
    $name = $libraries.Keys | Where-Object { $_ -like "$prefix/*" } | Select-Object -First 1
    if (-not $name) { throw "deps.json lists no $prefix." }
    return $name.Split('/')[1]
}
$coreVersion = LibVersion 'runtimepack.Microsoft.NETCore.App.Runtime.win-x64'
$desktopVersion = LibVersion 'runtimepack.Microsoft.WindowsDesktop.App.Runtime.win-x64'
$corePack = Join-Path $Packages "microsoft.netcore.app.runtime.win-x64\$coreVersion"
$desktopPack = Join-Path $Packages "microsoft.windowsdesktop.app.runtime.win-x64\$desktopVersion"
$avalonVersion = LibVersion 'AvalonEdit'
$toolkitVersion = LibVersion 'CommunityToolkit.Mvvm'
$icedVersion = LibVersion 'Iced'
$nasmSource = @{}
foreach ($line in Get-Content -LiteralPath (Join-Path $RepoRoot 'tools\nasm\SOURCE.txt')) {
    if ($line -match '^([a-z_0-9]+)=(.*)$') { $nasmSource[$Matches[1]] = $Matches[2] }
}
$innoVersion = (Select-String -LiteralPath (Join-Path $RepoRoot 'build\fetch-innosetup.ps1') -Pattern "^\`$Version = '([^']+)'").Matches[0].Groups[1].Value
$innoUrl = (Select-String -LiteralPath (Join-Path $RepoRoot 'build\fetch-innosetup.ps1') -Pattern "^\`$DownloadUrl = '([^']+)'").Matches[0].Groups[1].Value

# ---- 2. components (license texts are read from real files only)
$mitTemplate = (Read-Text (Join-Path $RepoRoot 'LICENSE')) -replace 'Copyright \(c\) 2026 Assembly Studio contributors', '{COPYRIGHT}'
$avalonCopyright = ([xml](Get-Content -LiteralPath (Join-Path $Packages "avalonedit\$avalonVersion\avalonedit.nuspec") -Raw)).package.metadata.copyright
$components = [ordered]@{
    'assembly-studio' = [ordered]@{ Name = 'Assembly Studio'; Version = $AppVersion; Spdx = 'MIT'; Source = 'This repository'
        Copyright = 'Copyright (c) 2026 Assembly Studio contributors'; Text = (Read-Text (Join-Path $RepoRoot 'LICENSE'))
        Obligations = 'Keep the copyright and permission notice (D-025)'; Maintenance = 'This project' }
    'dotnet-runtime' = [ordered]@{ Name = 'Microsoft .NET runtime (Microsoft.NETCore.App)'; Version = $coreVersion; Spdx = 'MIT'
        Source = "https://www.nuget.org/packages/Microsoft.NETCore.App.Runtime.win-x64/$coreVersion"; Copyright = 'Copyright (c) .NET Foundation and Contributors'
        Text = (Read-Text (Join-Path $corePack 'LICENSE.TXT')); Notices = (Read-Text (Join-Path $corePack 'THIRD-PARTY-NOTICES.TXT'))
        Obligations = 'MIT notice; the files below under the .NET Library License (end users accept its terms: installer license page, D-026)'
        Maintenance = 'Microsoft, .NET 10 LTS (supported to 2028-11-14, docs/02-research/platform-and-packaging.md)' }
    'windows-desktop' = [ordered]@{ Name = 'Microsoft Windows Desktop runtime (WPF)'; Version = $desktopVersion; Spdx = 'MIT'
        Source = "https://www.nuget.org/packages/Microsoft.WindowsDesktop.App.Runtime.win-x64/$desktopVersion"; Copyright = 'Copyright (c) .NET Foundation and Contributors'
        Text = (Read-Text (Join-Path $desktopPack 'LICENSE'))
        Obligations = 'MIT notice; the files below under the .NET Library License and the Windows SDK License (end users accept their terms: installer license page, D-026)'
        Maintenance = 'Microsoft, dotnet/wpf, actively maintained (docs/02-research/platform-and-packaging.md)' }
    'avalonedit' = [ordered]@{ Name = 'AvalonEdit'; Version = $avalonVersion; Spdx = 'MIT'; Source = "https://www.nuget.org/packages/AvalonEdit/$avalonVersion"
        Copyright = "Copyright (c) $avalonCopyright"; Text = ($mitTemplate -replace '\{COPYRIGHT\}', "Copyright (c) $avalonCopyright")
        Obligations = 'MIT notice (the package declares the MIT expression; the text is the MIT License with the package''s copyright line)'
        Maintenance = 'Latest release 2025-04-13; master active (docs/02-research/editor-mvvm-testing.md)' }
    'communitytoolkit-mvvm' = [ordered]@{ Name = 'CommunityToolkit.Mvvm'; Version = $toolkitVersion; Spdx = 'MIT'; Source = "https://www.nuget.org/packages/CommunityToolkit.Mvvm/$toolkitVersion"
        Copyright = 'Copyright (c) .NET Foundation and Contributors'; Text = (Read-Text (Join-Path $Packages "communitytoolkit.mvvm\$toolkitVersion\License.md"))
        Notices = (Read-Text (Join-Path $Packages "communitytoolkit.mvvm\$toolkitVersion\ThirdPartyNotices.txt"))
        Obligations = 'MIT notice and the package''s third-party notices'; Maintenance = '.NET Foundation; release 2026-03-25 (docs/02-research/editor-mvvm-testing.md)' }
    'iced' = [ordered]@{ Name = 'Iced'; Version = $icedVersion; Spdx = 'MIT'; Source = "https://www.nuget.org/packages/Iced/$icedVersion"
        Copyright = 'Copyright (C) 2018-present iced project and contributors'; Text = (Read-Text (Join-Path $Packages "iced\$icedVersion\LICENSE.txt"))
        Obligations = 'MIT notice'; Maintenance = 'icedland/iced (docs/02-research/execution-engine.md)' }
    'nasm' = [ordered]@{ Name = 'NASM (the Netwide Assembler)'; Version = $nasmSource['version']; Spdx = $nasmSource['license']; Source = $nasmSource['source']
        Copyright = 'Copyright 1996-2025 the NASM Authors'; Text = (Read-Text (Join-Path $RepoRoot 'tools\nasm\LICENSE'))
        Obligations = 'Binary redistribution reproduces the notice, conditions and disclaimer (installed as tools\nasm\LICENSE)'
        Maintenance = 'Release 3.02, 2026-06-29 (docs/02-research/assembler.md)' }
    'inno-setup' = [ordered]@{ Name = 'Inno Setup (in the installer only)'; Version = $innoVersion; Spdx = 'LicenseRef-Inno-Setup'; Source = $innoUrl
        Copyright = 'Copyright (C) 1997-2026 Jordan Russell; portions Copyright (C) 2000-2026 Martijn Laan'; Text = (Read-Text (Join-Path $RepoRoot 'tools\innosetup\license.txt'))
        Obligations = 'Keep the notices with the installer (the installer, not the installed application, contains it)'
        Maintenance = 'jrsoftware, release 7.1.0 2026-08-12 (docs/02-research/platform-and-packaging.md)' }
}
$microsoftTexts = [ordered]@{
    'LicenseRef-DotNet-Library-License' = [ordered]@{ Name = '.NET Library License'; Text = (Read-Text (Join-Path $LicenseDir 'dotnet-library-license.txt')) }
    'LicenseRef-Windows-SDK-License' = [ordered]@{ Name = 'Windows SDK License'; Text = (Read-Text (Join-Path $LicenseDir 'windows-sdk-license.txt')) }
}
if ($components['inno-setup'].Text -notmatch 'Jordan Russell') { throw 'tools\innosetup\license.txt is not the Inno Setup License.' }

# Microsoft's per-file licenses: the bullets under each heading of license-information-windows.md
$perFile = @()
$current = $null
foreach ($line in Get-Content -LiteralPath (Join-Path $LicenseDir 'license-information-windows.md')) {
    if ($line -match '\[\.NET Library License\]') { $current = 'LicenseRef-DotNet-Library-License' }
    elseif ($line -match '\[Windows SDK License\]') { $current = 'LicenseRef-Windows-SDK-License' }
    elseif ($line -match '^All other binaries') { $current = $null }
    elseif ($current -and $line -match '^\* (\S+)') {
        $name = $Matches[1]
        # "Name.{x86|amd64}.dll" lists variants; Windows PowerShell's -replace takes no script block, so use a MatchEvaluator.
        $evaluator = [Text.RegularExpressions.MatchEvaluator] { param($m) '(' + ($m.Groups[1].Value -replace '\\\|', '|') + ')' }
        $pattern = '^' + [regex]::Replace([regex]::Escape($name), '\\\{([^}]+)}', $evaluator) + '$'
        $perFile += [pscustomobject]@{ Pattern = $pattern; License = $current; Name = $name }
    }
}
if ($perFile.Count -lt 5) { throw 'license-information-windows.md: fewer per-file licenses than expected.' }

# ---- 3. every published file
$own = '^(AssemblyStudio\.|LICENSE$|THIRD-PARTY-NOTICES\.txt$|RELEASE-NOTES\.md$)'
$files = @()
foreach ($item in Get-ChildItem -LiteralPath $PublishDir -Recurse -File) {
    $rel = $item.FullName.Substring($PublishDir.Length + 1).Replace('\', '/')
    $leaf = $item.Name
    if ($rel -notmatch '/' -and $leaf -match $own) { $component = 'assembly-studio' }
    elseif ($rel -like 'tools/nasm/*') { $component = 'nasm' }
    elseif ($fileToLib.ContainsKey($leaf)) {
        $lib = $fileToLib[$leaf]
        $component = switch -Wildcard ($lib) {
            'runtimepack.Microsoft.NETCore.App.Runtime*' { 'dotnet-runtime' }
            'runtimepack.Microsoft.WindowsDesktop.App.Runtime*' { 'windows-desktop' }
            'AvalonEdit/*' { 'avalonedit' }
            'CommunityToolkit.Mvvm/*' { 'communitytoolkit-mvvm' }
            'Iced/*' { 'iced' }
            'AssemblyStudio.*' { 'assembly-studio' }
            default { throw "deps.json names an unexpected library for ${rel}: $lib" }
        }
    }
    elseif ($rel -match '^[a-zA-Z-]+/[^/]+\.resources\.dll$' -and (Test-Path -LiteralPath (Join-Path $desktopPack "runtimes\win-x64\lib\net10.0\$($rel.Replace('/', '\'))"))) {
        $component = 'windows-desktop'
    }
    else { throw "Unknown file in the publish folder: $rel (add it to a component in license-inventory.ps1)" }
    $license = $components[$component].Spdx
    foreach ($p in $perFile) { if ($rel -notmatch '/' -and $leaf -match $p.Pattern) { $license = $p.License } }
    $hash = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $files += [pscustomobject]@{ Path = $rel; Component = $component; License = $license; Sha256 = $hash; Bytes = $item.Length }
}

# ---- 4. the bundled NASM matches its recorded checksums (NFR-031)
$sums = @{}
foreach ($line in Get-Content -LiteralPath (Join-Path $RepoRoot 'tools\nasm\SHA256SUMS.txt')) {
    if ($line -match '^([0-9a-f]{64})\s+(\S+)$') { $sums[$Matches[2]] = $Matches[1] }
}
foreach ($f in $files | Where-Object { $_.Component -eq 'nasm' }) {
    $name = [IO.Path]::GetFileName($f.Path)
    if (-not $sums.ContainsKey($name)) { throw "tools\nasm\SHA256SUMS.txt has no checksum for $name." }
    if ($sums[$name] -ne $f.Sha256) { throw "Checksum mismatch for $($f.Path): $($f.Sha256), recorded $($sums[$name])." }
}
$nasmChecked = @($files | Where-Object { $_.Component -eq 'nasm' }).Count
$otherLicensed = @($files | Where-Object { $_.License -like 'LicenseRef-*' } | Sort-Object Path)
foreach ($p in $perFile) {
    if (-not ($otherLicensed | Where-Object { $_.Path -match $p.Pattern })) {
        throw "Microsoft lists $($p.Name) under $($p.License), but it is not in the publish folder."
    }
}

# ---- 5. THIRD-PARTY-NOTICES.txt
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("ASSEMBLY STUDIO ${AppVersion}: LICENSE AND THIRD-PARTY NOTICES")
[void]$sb.AppendLine()
[void]$sb.AppendLine('Assembly Studio is open source under the MIT License. It ships the third-party components listed below,')
[void]$sb.AppendLine('each with its license. This file is generated from the published application by')
[void]$sb.AppendLine('build/installer/license-inventory.ps1 (do not edit it by hand).')
[void]$sb.Append((Rule "Assembly Studio $AppVersion"))
[void]$sb.AppendLine('License: MIT')
[void]$sb.AppendLine()
[void]$sb.AppendLine($components['assembly-studio'].Text)
foreach ($key in $components.Keys | Where-Object { $_ -ne 'assembly-studio' }) {
    $c = $components[$key]
    $count = @($files | Where-Object { $_.Component -eq $key }).Count
    [void]$sb.Append((Rule "$($c.Name) $($c.Version)"))
    [void]$sb.AppendLine("Source: $($c.Source)")
    [void]$sb.AppendLine("License: $($c.Spdx)")
    if ($key -eq 'inno-setup') { [void]$sb.AppendLine('Files: none installed; the installer program is built with Inno Setup.') }
    else { [void]$sb.AppendLine("Files: $count") }
    $special = @($otherLicensed | Where-Object { $_.Component -eq $key })
    if ($special.Count) {
        [void]$sb.AppendLine('These files are under Microsoft license terms instead of MIT (texts below):')
        foreach ($s in $special) { [void]$sb.AppendLine("  $($s.Path): $($microsoftTexts[$s.License].Name)") }
    }
    if ($key -eq 'nasm') {
        [void]$sb.AppendLine('License text: installed as tools\nasm\LICENSE; Help > About shows it after these notices.')
    }
    else {
        [void]$sb.AppendLine()
        [void]$sb.AppendLine($c.Text)
    }
    if ($c.Contains('Notices')) {
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("Third-party notices of $($c.Name):")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine($c.Notices)
    }
}
foreach ($id in $microsoftTexts.Keys) {
    [void]$sb.Append((Rule $microsoftTexts[$id].Name))
    [void]$sb.AppendLine($microsoftTexts[$id].Text)
}
$notices = $sb.ToString().Replace("`r`n", "`n").Replace("`n", "`r`n")
[IO.File]::WriteAllText((Join-Path $RepoRoot 'THIRD-PARTY-NOTICES.txt'), $notices, $Utf8)

# ---- 6. the installer's license page (D-026)
$ag = New-Object System.Text.StringBuilder
[void]$ag.AppendLine('Assembly Studio is open source under the MIT License:')
[void]$ag.AppendLine()
[void]$ag.AppendLine($components['assembly-studio'].Text)
[void]$ag.AppendLine()
[void]$ag.AppendLine('Assembly Studio includes the Microsoft .NET runtime and WPF. Most of their files are under the MIT License. The')
[void]$ag.AppendLine('following files are under the Microsoft license terms below, which you accept by installing Assembly Studio:')
[void]$ag.AppendLine()
foreach ($s in $otherLicensed) { [void]$ag.AppendLine("  $($s.Path): $($microsoftTexts[$s.License].Name)") }
[void]$ag.AppendLine()
[void]$ag.AppendLine('All third-party components and their licenses are listed in THIRD-PARTY-NOTICES.txt in the installation folder')
[void]$ag.AppendLine('and in Help > About > Third-party notices.')
foreach ($id in $microsoftTexts.Keys) {
    [void]$ag.Append((Rule $microsoftTexts[$id].Name))
    [void]$ag.AppendLine($microsoftTexts[$id].Text)
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'license-agreement.txt'), $ag.ToString().Replace("`r`n", "`n").Replace("`n", "`r`n"), $Utf8Bom)

# ---- 7. docs/09-release/license-inventory.md
$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine('# License Inventory')
[void]$md.AppendLine()
[void]$md.AppendLine('| Field | Value |')
[void]$md.AppendLine('|---|---|')
[void]$md.AppendLine('| Document | Release license inventory (generated by `build/installer/license-inventory.ps1`; do not edit by hand) |')
[void]$md.AppendLine("| Application | Assembly Studio $AppVersion, win-x64, self-contained (the installer's publish folder) |")
[void]$md.AppendLine("| Files | $($files.Count) published files, each mapped to a component; no unknown file |")
[void]$md.AppendLine("| Checks | NASM: $nasmChecked of $nasmChecked files match ``tools/nasm/SHA256SUMS.txt``. Microsoft's per-file licenses applied from ``build/installer/licenses/license-information-windows.md`` |")
[void]$md.AppendLine('| Task | TASK-102 (FR-090, NFR-030, NFR-031; AC-042, AC-061; ADR-0009, D-025, D-026) |')
[void]$md.AppendLine()
[void]$md.AppendLine('## 1. Components')
[void]$md.AppendLine('| Component | Version | Source (official) | License | Files | Obligations | Maintenance |')
[void]$md.AppendLine('|---|---|---|---|---|---|---|')
foreach ($key in $components.Keys) {
    $c = $components[$key]
    $count = @($files | Where-Object { $_.Component -eq $key }).Count
    if ($key -eq 'inno-setup') { $count = '0 (installer only)' }
    [void]$md.AppendLine("| $($c.Name) | $($c.Version) | $($c.Source) | $($c.Spdx) | $count | $($c.Obligations) | $($c.Maintenance) |")
}
[void]$md.AppendLine()
[void]$md.AppendLine('## 2. Files under Microsoft license terms (not MIT)')
[void]$md.AppendLine('| File | Component | License |')
[void]$md.AppendLine('|---|---|---|')
foreach ($s in $otherLicensed) { [void]$md.AppendLine("| ``$($s.Path)`` | $($components[$s.Component].Name) | $($microsoftTexts[$s.License].Name) |") }
[void]$md.AppendLine()
[void]$md.AppendLine('## 3. Pinning')
[void]$md.AppendLine("- NuGet packages: exact versions in ``Directory.Packages.props`` (central package management): AvalonEdit $avalonVersion, CommunityToolkit.Mvvm $toolkitVersion, Iced $icedVersion.")
$sdk = (Get-Content -LiteralPath (Join-Path $RepoRoot 'global.json') -Raw | ConvertFrom-Json).sdk
[void]$md.AppendLine("- .NET: ``global.json`` names SDK $($sdk.version) with rollForward ``$($sdk.rollForward)``, so a newer SDK patch can be used and decides the runtime packs ($coreVersion here). The SBOM records the runtime actually shipped.")
[void]$md.AppendLine("- NASM $($nasmSource['version']): ``tools/nasm/SOURCE.txt`` (archive SHA-256 $($nasmSource['archive_sha256'])) and ``SHA256SUMS.txt``, checked above.")
[void]$md.AppendLine("- Inno Setup ${innoVersion}: pinned in ``build/fetch-innosetup.ps1``.")
[void]$md.AppendLine()
[void]$md.AppendLine('## 4. Outputs')
[void]$md.AppendLine('- `THIRD-PARTY-NOTICES.txt`: installed next to the application; Help > About > Third-party notices shows it, followed by `tools\nasm\LICENSE`.')
[void]$md.AppendLine('- `build/installer/license-agreement.txt`: the installer''s license page (D-026).')
[void]$md.AppendLine('- `docs/09-release/sbom.spdx.json`: SPDX 2.3 SBOM with every file and its SHA-256.')
[IO.File]::WriteAllText((Join-Path $RepoRoot 'docs\09-release\license-inventory.md'), $md.ToString().Replace("`r`n", "`n"), $Utf8)

# ---- 8. docs/09-release/sbom.spdx.json (SPDX 2.3)
$pkgId = @{}
$spdxPackages = @()
$relationships = @([ordered]@{ spdxElementId = 'SPDXRef-DOCUMENT'; relationshipType = 'DESCRIBES'; relatedSpdxElement = 'SPDXRef-Package-assembly-studio' })
foreach ($key in $components.Keys) {
    $c = $components[$key]
    $pkgId[$key] = "SPDXRef-Package-$key"
    $spdxPackages += [ordered]@{ SPDXID = $pkgId[$key]; name = $c.Name; versionInfo = $c.Version
        downloadLocation = $(if ($c.Source -like 'http*') { $c.Source } else { 'NOASSERTION' }); filesAnalyzed = $false
        licenseConcluded = $c.Spdx; licenseDeclared = $c.Spdx; copyrightText = $c.Copyright }
    if ($key -ne 'assembly-studio') {
        $type = if ($key -eq 'inno-setup') { 'BUILD_TOOL_OF' } else { 'DEPENDENCY_OF' }
        $relationships += [ordered]@{ spdxElementId = $pkgId[$key]; relationshipType = $type; relatedSpdxElement = 'SPDXRef-Package-assembly-studio' }
    }
}
$spdxFiles = @()
$n = 0
foreach ($f in $files | Sort-Object Path) {
    $n++
    $id = "SPDXRef-File-$n"
    $spdxFiles += [ordered]@{ SPDXID = $id; fileName = "./$($f.Path)"; checksums = @([ordered]@{ algorithm = 'SHA256'; checksumValue = $f.Sha256 })
        licenseConcluded = $f.License; copyrightText = 'NOASSERTION' }
    $relationships += [ordered]@{ spdxElementId = $pkgId[$f.Component]; relationshipType = 'CONTAINS'; relatedSpdxElement = $id }
}
$extracted = @()
foreach ($id in $microsoftTexts.Keys) { $extracted += [ordered]@{ licenseId = $id; name = $microsoftTexts[$id].Name; extractedText = $microsoftTexts[$id].Text } }
$extracted += [ordered]@{ licenseId = 'LicenseRef-Inno-Setup'; name = 'Inno Setup License'; extractedText = $components['inno-setup'].Text }
$sbom = [ordered]@{
    spdxVersion = 'SPDX-2.3'; dataLicense = 'CC0-1.0'; SPDXID = 'SPDXRef-DOCUMENT'; name = "Assembly Studio $AppVersion win-x64"
    documentNamespace = "https://spdx.org/spdxdocs/assembly-studio-$AppVersion-win-x64-$([guid]::NewGuid())"
    creationInfo = [ordered]@{ created = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'); creators = @('Tool: build/installer/license-inventory.ps1', 'Organization: Assembly Studio contributors') }
    packages = $spdxPackages; files = $spdxFiles; relationships = $relationships; hasExtractedLicensingInfos = $extracted
}
[IO.File]::WriteAllText((Join-Path $RepoRoot 'docs\09-release\sbom.spdx.json'), ($sbom | ConvertTo-Json -Depth 8), $Utf8)

if ($CopyToPublish) {
    Copy-Item -LiteralPath (Join-Path $RepoRoot 'THIRD-PARTY-NOTICES.txt') -Destination $PublishDir -Force
    Copy-Item -LiteralPath (Join-Path $RepoRoot 'LICENSE') -Destination $PublishDir -Force
}

Write-Host ("Inventory: {0} files, {1} components; NASM checksums {2}/{2}; {3} files under Microsoft terms." -f $files.Count, $components.Count, $nasmChecked, $otherLicensed.Count) -ForegroundColor Green
$files | Group-Object Component | Sort-Object Name | ForEach-Object { Write-Host ("  {0,-22} {1,4} files" -f $_.Name, $_.Count) }
