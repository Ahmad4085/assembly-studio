# Release 1.0.1: Checklist and Report

| Field | Value |
|---|---|
| Version | 1.0.1 |
| Date | 2026-11-04 (published 14:52 +0500) |
| Build | Commit `2c10a2f` (tag `v1.0.1`); the executable reports ProductVersion `1.0.1+2c10a2f…`, FileVersion 1.0.1.0 |
| Change from 1.0.0 | The logo (TASK-118) as the executable, window and installer icon. No other product change |
| QA report | `docs/08-qa/qa-report-2026-11-04-3.md`: **Pass with conditions**; conditions accepted by the user for 1.0.1 (approval log, 2026-11-04 14:36 +0500) |
| Readiness | **Ready, with accepted conditions.** Publishing on GitHub approved by the user (same answer) |

## Checklist
| # | Check | Evidence | Result |
|---|---|---|---|
| 1 | QA verdict is Pass or Pass with accepted conditions | QA 2026-11-04-3 Pass with conditions; accepted 2026-11-04 14:36 (approval log) | Pass |
| 2 | No open Critical defects. High defects accepted by the user in writing | No open Critical or High; 9 open Low intermittent test-run failures (DEF-010 reopened 2026-11-04), accepted with the conditions | Pass |
| 3 | Clean release build executed | `build/installer/build-installer.ps1` 14:37:51–14:40:21 on `2c10a2f`: clean publish folder, self-contained win-x64, exit 0; 495 files, 180.8 MB | Pass |
| 4 | Full test suite executed on the release build | `dotnet build -c Release` 0 warnings; `build/test.ps1 -NoBuild -ResultsDirectory artifacts/test-results/release-1.0.1` 14:41:10–14:44:59: 1586 + 3 passed, 4 + 3 skipped (opt-in), 0 failed. Measurements and coverage: test report 2026-11-04-2 (14:14–14:33, same code except the version property): measurements 7 of 7 | Pass |
| 5 | Installer built | Inno Setup 7.1.0; `artifacts/installer/AssemblyStudio-1.0.1-setup.exe` 53.5 MB; Windows extracts the logo from the setup file and the published executable (centre pixel #F5C542) | Pass |
| 6 | Installer tested: clean install, upgrade, uninstall on Windows 10 and 11 | The automatic `test-installer.ps1` was **not run**: it refuses while Assembly Studio is installed for the user, and the user's 1.0.0 was installed. Instead (the user's choice), on this PC: the user uninstalled 1.0.0 (14:47:53; the program folder, uninstall entry and shortcut removed, `settings.json` kept) and installed 1.0.1 (user-reported). Checked by me 14:49: uninstall entry DisplayVersion 1.0.1, the installed executable identical (SHA-256) to the build, 497 files, Start-menu shortcut, notices and release notes say 1.0.1, the executable's icon is the logo; the installed application's headless smoke test exit 0 (startup 2,412 ms, temporary settings), NASM 3.02 runs, the user's settings unchanged. **Not tested for 1.0.1:** the upgrade over 1.0.0 (passed for 1.0.0, 21/21) and the automated uninstall checks; clean machines (WVR-002) | Pass, partly user-run; upgrade path not retested |
| 7 | App runs fully offline after install | No network code in `src` (code search, QA 2026-11-04-3); as 1.0.0 | Pass (as 1.0.0) |
| 8 | Runtime prerequisites handled | Self-contained win-x64 (.NET 10 runtime and WPF included) | Pass |
| 9 | All dependencies listed with license. Obligations met | `license-inventory.ps1` regenerated `THIRD-PARTY-NOTICES.txt`, `license-inventory.md` and `sbom.spdx.json` from this publish folder; only the version changed (495 files, no unknown file, NASM checksums 2/2) | Pass (indemnity: the distributor's responsibility, as 1.0.0) |
| 10 | Code signing or documented unsigned-installer warning | Unsigned, by the user's decision for 1.0.1 (OQ-020 open); the install guide documents SmartScreen and the SHA-256 check | Pass (unsigned, documented, accepted) |
| 11 | Release notes and install guide written | `docs/09-release/release-notes-1.0.1.md` (also `RELEASE-NOTES.md`, installed), `docs/09-release/install-guide.md` (1.0.1) | Pass |
| 12 | Version numbers consistent | `Directory.Build.props` 1.0.1 → ProductVersion 1.0.1+2c10a2f…, FileVersion 1.0.1.0; installer `AssemblyStudio-1.0.1-setup.exe`, DisplayVersion 1.0.1; notes, guide, notices 1.0.1 | Pass |
| 13 | User approval to publish | "Publish on GitHub (Recommended)", 2026-11-04 14:36 +0500 | Pass |

## Report
### Publication
Release 1.0.1 (the logo) **published** 2026-11-04 14:52 +0500: https://github.com/Ahmad4085/assembly-studio/releases/tag/v1.0.1 (tag `v1.0.1` on `2c10a2f`, marked Latest; asset AssemblyStudio-1.0.1-setup.exe, 56,087,303 bytes, GitHub digest sha256:17f4f02ed28c69f0a155f2106ab76d7a3303b424e4a15026c2efa5d780990370 equal to the local file). The first create attempt returned HTTP 502 and left nothing; the retry succeeded. Website and README point to 1.0.1.

### Artifacts
In `artifacts/installer/` (git-ignored), built from `2c10a2f` 2026-11-04 14:37–14:40:

| File | Size | SHA-256 |
|---|---|---|
| `AssemblyStudio-1.0.1-setup.exe` | 53.5 MB (56,087,303 bytes) | `17f4f02ed28c69f0a155f2106ab76d7a3303b424e4a15026c2efa5d780990370` |

### Known limitations
Unchanged from 1.0.0 (`release-notes-1.0.1.md`, "Known limitations").

### Items not completed
- The automatic installer test (install, upgrade, uninstall) on 1.0.1; see item 6.
- Clean-machine tests (WVR-002), the usability session (TASK-105), code signing (OQ-020): as 1.0.0.

## Change log
- 2026-11-04 14:50 +0500: Release record written; publishing follows.
- 2026-11-04 14:52 +0500: Published on GitHub; the website and README point to 1.0.1.
