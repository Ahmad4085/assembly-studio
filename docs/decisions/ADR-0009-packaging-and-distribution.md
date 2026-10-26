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
