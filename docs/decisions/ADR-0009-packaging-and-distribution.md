# ADR-0009: Packaging — self-contained publish + Inno Setup per-user installer

| Field | Value |
|---|---|
| Status | Approved (code signing explicitly **Deferred** to the release phase) |
| Version | 1.0 |
| Date | 2026-10-26 |
| Requirements addressed | FR-083, FR-084, FR-085, FR-086, FR-087, FR-088, FR-089, FR-090, NFR-008, NFR-027, NFR-028, NFR-030, NFR-031 |
| Research | `docs/02-research/platform-and-packaging.md` (R-PLAT, R-PKG). Answers OQ-013. OQ-020 deferred |

## Context
The installer must work offline on a clean machine (FR-083, FR-086), with no runtime prerequisite and no admin rights where
possible (NFR-027). WPF can't be trimmed. Research rejected WiX (maintenance-fee EULA on binaries) and MSIX (signing cost).

## Options considered
| Question | Option | Notes |
|---|---|---|
| Runtime | **Self-contained, untrimmed, folder layout** | No prerequisite, fully offline. Larger size (to be measured, NFR-028) |
| | Framework-dependent + bundled Desktop Runtime installer | Bundling permission not found [Unverified]. Needs admin |
| | Single-file | Native DLL extraction to %TEMP%, path API caveats |
| Installer | **Inno Setup 7.x** (Inno Setup License: permissive, notices kept) | Per-user via `PrivilegesRequired=lowest` |
| | WiX 7 | MS-RL source plus OSMF EULA on binaries |
| | MSIX | Requires a code-signing certificate |

## Decision
1. `dotnet publish src/AssemblyStudio.App -c Release -r win-x64 --self-contained true` (no trimming, no single-file).
2. **Inno Setup 7.x** script in `build/installer/`:
   - per-user (`PrivilegesRequired=lowest`), installing to `{autopf}\Assembly Studio`;
   - a Start-menu shortcut; an optional `.asm` association, off by default (FR-084);
   - bundled NASM, samples, tutorials, docs and `THIRD-PARTY-NOTICES.txt`;
   - no downloads (FR-086);
   - upgrade keeps `%APPDATA%` settings (FR-088); uninstall leaves user files and settings in place (FR-085).
3. **No auto-update** (FR-089 is P3).
4. **Code signing: Deferred** to the release phase (OQ-020). Until then the installer is unsigned and the SmartScreen behavior is
   documented in the install guide.
5. **Notices and SBOM** (NFR-030, NFR-031): the third-party notices list .NET (MIT + .NET Library License + Windows SDK License for
   `D3DCompiler_47_cor3`), NASM (BSD-2), AvalonEdit (MIT), CommunityToolkit.Mvvm (MIT), Iced (MIT) and the Inno Setup notice
   requirements. An SBOM is generated per release.

## Consequences
- The installed size is larger than framework-dependent. NFR-028 (≤ 300 MB, P2) is measured in the first packaging task.
- Each .NET security patch requires a new installer.

## Validation
- A packaging task measures the installed size and install time on a clean VM without network (AC-040/041, NFR-027/028).
- **Unverified:** Inno Setup offline behavior and minimum supported Windows (POC-004 was proposed in research but not approved).
  Covered by the packaging task's clean-VM test.

## Changelog
- 2026-10-26 v0.1: Proposed (SDLC-006).
- 2026-10-26 v1.0: **Approved** by the user (2026-10-26 18:54 +0500: "approve architecture v0.1 and ADR-0001..0009; reference machine OK").
- 2026-11-02 (TASK-108, D-021): **Amendment: the published application is precompiled (ReadyToRun)**, set once in `AssemblyStudio.App.csproj` (`PublishReadyToRun`), so the installer and the end-to-end measurements publish the same way. Measured on the reference machine (self-contained win-x64 publish, 4 warm launches each, alternating): startup median about 2,830 ms plain vs 2,630 ms ReadyToRun, JIT time about 950 vs 530 ms; the first launch after publishing 18.8 s vs 8.4 s. Cost: published size 176.7 → 180.6 MB. Build-time package Microsoft.NETCore.App.Crossgen2.win-x64 (Microsoft, MIT; research entry in `docs/02-research/platform-and-packaging.md`), not distributed separately. No other part of the decision changed.
- 2026-11-04 10:54 +0500 (TASK-102, D-026): **Amendment: license page and generated notices.** The installer shows `build/installer/license-agreement.txt` (MIT, then the .NET Library License and the Windows SDK License for the files Microsoft lists) and needs it accepted; `LICENSE` and `THIRD-PARTY-NOTICES.txt` are installed. Notices, license page, inventory and SBOM are generated from the publish folder by `build/installer/license-inventory.ps1`. §5 also applies to coreclr.dll and Microsoft.DiaSymReader.Native.amd64.dll (.NET Library License), which §5 did not name. Approved by the user (the user 2026-11-04 about 10:35 +0500, answering "How should end users agree to the Microsoft terms for the bundled .NET/WPF runtime files?": "Installer license page (Recommended)").
