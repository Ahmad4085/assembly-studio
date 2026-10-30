# TASK-101 Verification: self-contained publish and Inno Setup installer

| Field | Value |
|---|---|
| Task | TASK-101 (FR-083..FR-088, NFR-027, NFR-028; ADR-0009) |
| Date | 2026-10-30 |
| Machine | Development machine (Windows 11 Pro 10.0.26200, x64). Not a clean machine: the clean-VM run is TASK-103 |
| Tools | .NET SDK 10.0.403; Inno Setup 7.1.0 x64 (pinned, SHA-256 verified, per-user in `tools/innosetup`, `build/fetch-innosetup.ps1`) |

## How to build and test
1. `build/fetch-nasm.ps1` and `build/fetch-innosetup.ps1` (once).
2. `build/installer/build-installer.ps1` → `artifacts/installer/AssemblyStudio-<version>-setup.exe`.
3. `build/installer/test-installer.ps1 -Installer <setup.exe> -UpgradeInstaller <newer setup.exe>`: silent per-user install,
   checks, upgrade, uninstall; always removes the test installation. For the upgrade, compile a second installer from the
   same publish folder with another version, for example `ISCC.exe /DAppVersion=1.0.0-upgradetest ...` (run from PowerShell:
   Git Bash rewrites ISCC's `/Q` and `/D...` switches as paths).

## Results (2026-10-30)
| Check | Result |
|---|---|
| Publish (self-contained win-x64, no trimming, no single file) | 418 files, 144.2 MB |
| Installer | `AssemblyStudio-1.0.0-setup.exe`, 45.2 MB, SHA-256 `7cab770eea9b3f0abe0b6dfe1bde96c3e224527ad606f7b0ff0f317048a041ab` |
| Build time (publish + compile) | 3 min 23 s |
| Silent install time (NFR-027 ≤ 2 min) | 15.3 s and 16.6 s (two runs) |
| Installed size (NFR-028 ≤ 300 MB) | 148.6 MB |
| Files, Start-menu shortcut, per-user uninstall entry; `.asm` association off by default (FR-083, FR-084) | Pass |
| Installed application starts (smoke test) and the installed NASM 3.02 runs | Pass |
| Upgrade 1.0.0 → 1.0.0-upgradetest with the association selected: one installation, new version, association created (FR-084, FR-088) | Pass (28.7 s) |
| Uninstall removes program files, shortcut, association and uninstall entry (FR-085) | Pass |
| User settings file and a user source file unchanged by install, upgrade and uninstall (FR-085, FR-088) | Pass |
| `test-installer.ps1` overall | 21/21 checks passed |

## Observations
- **First launch after install:** the smoke test measured 5,802 ms and 6,141 ms for the first start of freshly installed
  files; repeated starts of the same published build took 1,718–1,951 ms (4 runs). Likely antivirus scanning and cold file
  cache on first use. NFR-005 (≤ 3 s cold start) should be judged in TASK-086 on the reference machine, including whether
  "cold" covers the first start after installation; ReadyToRun publishing is a candidate improvement (an ADR-0009 change).
- **Version:** the installer is named after the application's version, which is the build default 1.0.0 until the release
  procedure sets the first version.
- **Not yet in the installer:** `THIRD-PARTY-NOTICES.txt` and `RELEASE-NOTES.md` (TASK-102 and the release procedure);
  they are packaged automatically once present at the repository root. No license page (TASK-100, product license decision).
- **Unsigned** (ADR-0009 §4, code signing deferred).
- **Still unverified (ADR-0009):** offline install on clean Windows 11 24H2 and Windows 10 Enterprise LTSC 21H2 machines, and
  the minimum OS check (`MinVersion=10.0.19044`): TASK-103 on the TASK-006 VMs.
