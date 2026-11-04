# Release 1.0.0: Checklist and Report

| Field | Value |
|---|---|
| Version | 1.0.0 |
| Date | 2026-11-04 (release candidate; not published) |
| Build | Commit `12ac130` (tag `v1.0.0`); the executable reports ProductVersion `1.0.0+12ac130…`, FileVersion 1.0.0.0. Earlier builds (`8cd9271`, `0aaeaf6` in the old numbering) were replaced; the unpushed history was rewritten before publishing (`docs/sdlc/commit-hash-map-2026-11-04.md`) |
| QA report | `docs/08-qa/qa-report-2026-11-04-2.md`: **Pass with conditions**. Condition 1 waived (WVR-002); conditions 2–4 accepted by the user 2026-11-04 11:33 (approval log) |
| Readiness | **Ready, with accepted conditions.** Published to the private GitHub repository on the user's request 2026-11-04 (first the user chose to commit and rebuild only, 2026-11-04 11:50 +0500) |

## Checklist
| # | Check | Evidence | Result |
|---|---|---|---|
| 1 | QA verdict is Pass or Pass with accepted conditions | QA 2026-11-04-2 Pass with conditions; WVR-002 (2026-11-04 10:25) and conditions 2–4 (2026-11-04 11:33) in `PROJECT_STATE.md` approval log | Pass |
| 2 | No open Critical defects. High defects accepted by the user in writing | `docs/07-testing/defects.md`: no open Critical or High; open defects are 8 Low intermittent test-run failures (accepted, condition 4) | Pass |
| 3 | Clean release build executed | `build/installer/build-installer.ps1` 11:34:34–11:37:02: deletes `artifacts/publish/win-x64`, then `dotnet publish src/AssemblyStudio.App -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -p:PublishSingleFile=false` (ReadyToRun, ADR-0009, D-021), exit 0; 495 files, 180.7 MB | Pass |
| 4 | Full test suite executed on the release build | `dotnet build -c Release` 0 warnings; `build/test.ps1 -NoBuild -ResultsDirectory artifacts/test-results/release-1.0.0` 11:38–11:42: 1588 + 6 total, 1584 + 3 passed, 4 + 3 skipped (opt-in), 0 failed. `build/bench/run-e2e.ps1` 11:42–11:44 on the published application: 7 of 7 passed (NFR-005 warm median 1,888 ms, max 1,979 ms; NFR-002 p95 35.5 ms; NFR-006 max 517 ms; NFR-007 peak 194.8 MB). Latest full test report with coverage: 2026-11-04-1 (same code except the version property) | Pass |
| 5 | Installer built | Inno Setup 7.1.0 (`tools/innosetup`, pinned in `build/fetch-innosetup.ps1`), `build/installer/AssemblyStudio.iss`; license page (D-026); `artifacts/installer/AssemblyStudio-1.0.0-setup.exe` 53.5 MB | Pass |
| 6 | Installer tested: clean install, upgrade, uninstall on Windows 10 and 11 | This PC (Windows 11 Pro 25H2, not clean): `build/installer/test-installer.ps1` with the release installer 11:46:21–11:47:18, **21/21**: install 17.7 s, 185.1 MB, smoke test of the installed application, NASM 3.02 runs, upgrade to 1.0.0-upgradetest (25.5 s) with the association, uninstall clean, settings and user file unchanged. Clean Windows 11 24H2 and Windows 10 LTSC 21H2: **not tested** | Waived (WVR-002, accepted); release notes say so |
| 7 | App runs fully offline after install | No network API or URL in `src` (code search, QA 2026-11-04-2); the installer downloads nothing (FR-086); an offline start recorded 2026-11-03 12:47 (Wi-Fi off in Windows' records, application log without error); the workflow offline: user-reported (AC-034, accepted condition 3). Not repeated on this release build | Pass (with user-reported part) |
| 8 | Runtime prerequisites handled (self-contained or runtime check) | Self-contained win-x64: the .NET 10.0.14 runtime and WPF are in the installer; nothing to install first | Pass |
| 9 | All dependencies listed with license. Obligations met (notices, source offers) | `docs/09-release/third-party-notices.md` (obligations), `license-inventory.md` and `sbom.spdx.json` regenerated from this publish folder (495 files, no unknown file, NASM checksums 2/2); `THIRD-PARTY-NOTICES.txt` installed; no copyleft, no source offers. The Microsoft licenses' **indemnity** clause rests with the distributor | Pass (indemnity: the distributor's responsibility) |
| 10 | Code signing (if applicable) or documented unsigned-installer warning | Not signed; the user decided to ship 1.0.0 unsigned (D-027, 2026-11-04 11:50 +0500; OQ-020 stays open). The install guide documents the SmartScreen warning and the SHA-256 check | Pass (unsigned, documented, accepted) |
| 11 | Release notes and install guide written | `docs/09-release/release-notes-1.0.0.md` (also `RELEASE-NOTES.md`, installed and shown in Help → About, FR-087), `docs/09-release/install-guide.md` | Pass |
| 12 | Version numbers consistent (assembly, installer, notes) | `Directory.Build.props` 1.0.0 → executable ProductVersion 1.0.0+…, FileVersion 1.0.0.0; the installer reads the version from the executable (`AssemblyStudio-1.0.0-setup.exe`, AppVersion 1.0.0); notes and guide say 1.0.0 | Pass |
| 13 | User approval to publish | The user: "publish it on GitHub", then "Keep the repo private for now (Recommended)" (2026-11-04 about 11:58); before that "Commit and rebuild only" (2026-11-04 11:50 +0500): no tag, push or upload | Pass (private repository) |

## Report
### Publication
2026-11-04 12:32 +0500, Release 1.0.0 **published** to the private repository: https://github.com/Ahmad4085/assembly-studio/releases/tag/v1.0.0 (tag `v1.0.0` on `12ac130`; asset AssemblyStudio-1.0.0-setup.exe, 56,099,852 bytes, GitHub digest sha256:16ac15358b80d2d12425be5ed05bce5e89dbf2a453514cd5dfaa8ce7153f3d83 equal to the local file). Pushed `master` and the tag only. The repository was private at first; it was made public 2026-11-04 12:48 +0500, so the release is now public.

### What was built and how
Assembly Studio 1.0.0, a self-contained win-x64 WPF application (.NET 10.0.14, ReadyToRun), with NASM 3.02. The Inno Setup 7.1.0
per-user installer shows a license page and installs:
- the application;
- `LICENSE`, `THIRD-PARTY-NOTICES.txt` and `RELEASE-NOTES.md`;
- a Start-menu shortcut, plus an optional desktop shortcut and `.asm` association.

### Artifacts
In `artifacts/installer/` (git-ignored, not committed); built from commit `12ac130` 2026-11-04 12:26–12:28 with
`build/installer/build-installer.ps1`:

| File | Size | SHA-256 |
|---|---|---|
| `AssemblyStudio-1.0.0-setup.exe` | 53.5 MB (56,099,852 bytes) | `16ac15358b80d2d12425be5ed05bce5e89dbf2a453514cd5dfaa8ce7153f3d83` |
| `AssemblyStudio-1.0.0-upgradetest-setup.exe` | 53.5 MB | test only, **not a release artifact** |

**Verification on `12ac130`:**
- Release build: 0 warnings.
- `build/test.ps1`: 1584 + 3 passed, 4 + 3 skipped, 0 failed (12:21–12:25).
- `test-installer.ps1` with this installer: 21/21 (12:30:11–12:31:08, install 17.5 s).
- `docs/09-release/sbom.spdx.json` was regenerated from this build.

The measurements (item 4) and the first installer test (item 6) ran on earlier candidates of the same code.

### Known limitations
As in `release-notes-1.0.0.md`, "Known limitations":
- the first start after a restart;
- the Variables panel's limits;
- 65,278-byte programs;
- 8086, .COM and DOS text services only;
- NASM syntax only;
- tested on one Windows 11 PC;
- no usability session yet;
- English only;
- an unsigned installer.

### Items not completed
- **Clean-machine install tests:** Windows 11 24H2 and Windows 10 LTSC 21H2, including fonts on Windows 10 LTSC (TASK-006, TASK-070,
  TASK-103). Waived for this release (WVR-002).
- **The usability session** (TASK-105, AC-062). The user accepted releasing without it.
- **The installer's license page** was not clicked through (silent installs skip it); its text is tested (TC-097).
- **Code signing:** 1.0.0 ships unsigned (D-027); OQ-020 stays open.
- **The Microsoft licenses' indemnity duty** rests with the distributor; it can't be met by a file.

## Change log
- 2026-11-04 11:56 +0500: Rebuilt from commit `0aaeaf6`; final SHA-256 recorded; the rebuilt installer passed the installer test.
- 2026-11-04 12:31 +0500: History rewritten before the first push (student ID removed); rebuilt from `12ac130`; final SHA-256 recorded; published to the private repository.
- 2026-11-04 12:48 +0500: The repository was made public; the release is public.
