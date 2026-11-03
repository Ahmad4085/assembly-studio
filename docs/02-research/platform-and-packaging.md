# Research — Platform (.NET / WPF / Windows) and Packaging

| Field | Value |
|---|---|
| Document | Research: platform and packaging |
| Status | Approved |
| Version | 1.0 |
| Last updated | 2026-10-26 |
| Depends on | `docs/01-requirements/PRODUCT_REQUIREMENTS.md` (set) v2.0 |

**Method.** Sources were read on 2026-10-26 by a research agent. The claims marked ✔ were independently
re-fetched by the lead agent on the same day. `[Verified: URL]` means a primary source was read this
session. `[Hypothesis]` marks an opinion or inference.

## 1. Questions
OQ-005 (target .NET version and WPF support), OQ-010 (minimum Windows versions, ARM64), OQ-013 (deployment
model, installer, per-user install). Related requirements: CON-001, NFR-025, NFR-027, NFR-028, FR-083, FR-086,
NFR-030.

## 2. Evaluation criteria
| Criterion | Weight | Derived from |
|---|---|---|
| License compatible with a permissive open-source release | Gate | D-007, NFR-030 |
| Fully offline install, no downloads | Gate | FR-083, FR-086, NFR-008 |
| Supported on the required Windows versions | Gate | CON-001, NFR-025 |
| Vendor support lifetime | High | Maintainability |
| Install footprint and admin rights | Medium | NFR-027, NFR-028 |

## 3. Findings: .NET and WPF
| Attribute | Finding | Evidence |
|---|---|---|
| .NET 10 | Released 2025-11-11. **LTS**, supported until **2028-11-14**. Latest patch 10.0.14 (2026-10-25) | [Verified: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core] [Verified: https://raw.githubusercontent.com/dotnet/core/main/release-notes/10.0/README.md] |
| .NET 8 / 9 | Both end support **2026-11-10**, about two weeks from now. Not viable targets | [Verified: support policy above] |
| .NET 10 Windows matrix ✔ | Windows 11 26H1, 25H2, 24H2 (+ IoT and Enterprise editions), 23H2 (E only). **Windows 10: only 21H2 (E), 21H2 (IoT), 1809 (E), 1607 (E).** Windows 10 22H2 Home/Pro is **not listed**. Architectures: Arm64, x64, x86 | ✔ [Verified: https://raw.githubusercontent.com/dotnet/core/main/release-notes/10.0/supported-os.md] [Verified: https://learn.microsoft.com/en-us/dotnet/core/install/windows]: "Windows 10 support is limited to Long-Term Servicing Channel (LTSC) and Enterprise editions only." |
| Windows 10 lifecycle ✔ | Windows 10 left support on **2025-10-14**. The paid ESU program covers version 22H2, for up to three years for organizations | ✔ [Verified: https://learn.microsoft.com/en-us/windows/whats-new/extended-security-updates] |
| WPF on .NET 10 | Supported and actively maintained (dotnet/wpf, MIT, releases 2026-10-13). WPF supports ARM64 since .NET 6. Windows-only | [Verified: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/whats-new/net100] [Verified: https://raw.githubusercontent.com/dotnet/wpf/main/README.md] |
| .NET Framework 4.8.1 ✔ | Follows the Component Lifecycle Policy. No end date is listed, so support follows the host OS. Legacy runtime with no new feature development | ✔ [Verified: https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-framework]. Feature-freeze statement: [Unverified] |
| Trimming | **Not supported for WPF.** "trimming support for WPF is currently disabled in the .NET SDK" | [Verified: https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/incompatibilities] |
| Single-file | Supported. Native libraries are extracted to `%TEMP%` only with `IncludeNativeLibrariesForSelfExtract`. `Assembly.Location` returns empty. No WPF-specific statement found | [Verified: https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview] |
| Self-contained vs framework-dependent | Self-contained needs no preinstalled runtime but is larger, and the runtime is patched only by shipping a new app version. Framework-dependent needs the runtime on the machine | [Verified: https://learn.microsoft.com/en-us/dotnet/core/deploying/] |
| Runtime licenses | Mostly MIT. WPF native DLLs (`PresentationNative_cor3`, `wpfgfx_cor3`, `vcruntime140_cor3`) are under the **.NET Library License**, and `D3DCompiler_47_cor3` under the Windows SDK License. The .NET Library License allows distributing object code inside an application, forbids putting it under copyleft "Excluded Licenses", and requires passing on its terms | [Verified: https://raw.githubusercontent.com/dotnet/core/main/license-information-windows.md] [Verified: https://dotnet.microsoft.com/en-us/dotnet_library_license.htm] |
| Bundling the Desktop Runtime installer | No source found that explicitly permits this | [Unverified] |
| SDK install | WinGet `Microsoft.DotNet.SDK.10`, the .NET installer, or `dotnet-install.ps1` (recommended for non-admin and CI). Visual Studio 2026 18.0+ is needed for VS; Rider is present here | [Verified: https://learn.microsoft.com/en-us/dotnet/core/install/windows] |

## 4. Findings: installers
| Option | License (exact) | Fit | Evidence |
|---|---|---|---|
| **Inno Setup 7.1.0** (2026-08-12) | Custom "Inno Setup License": any purpose, including commercial. Binary redistributions keep notices; no payment clause. A commercial license is *requested* for organizations with revenue over $5,000, but "not strictly required". SPDX id not found [Unverified] | `PrivilegesRequired=lowest` gives a per-user install without admin, and `{autopf}` maps to the user's Program Files. Has a 64-bit edition with Arm64 matching | [Verified: https://raw.githubusercontent.com/jrsoftware/issrc/main/license.txt] [Verified: https://jrsoftware.org/isorder.php] [Verified: https://jrsoftware.org/ishelp/topic_setup_privilegesrequired.htm] |
| WiX Toolset 7.0.0 | Source MS-RL (weak copyleft). Binaries require agreeing to the **Open Source Maintenance Fee EULA** (fee for revenue-generating use at ≥ US$10k) | Workable, but the added EULA terms are a burden for contributors | [Verified: https://raw.githubusercontent.com/wixtoolset/wix/main/OSMFEULA.txt] |
| MSIX | n/a | **Requires a code-signing certificate**: self-signed for test only, otherwise about $10/month or $300–500/year. Sideloading friction (sources conflict) | [Verified: https://learn.microsoft.com/en-us/windows/msix/package/signing-package-overview] |

## 5. Comparison and recommendation (input to ADRs, not a decision)
| Option | License gate | Win10 22H2 consumer | Vendor support | Footprint | Verdict |
|---|---|---|---|---|---|
| **.NET 10 + WPF, self-contained** | Pass (MIT + .NET Library License notices) | **Not supported upstream** | To 2028-11-14 | Larger (no trimming) [Hypothesis] | **Recommended** |
| .NET Framework 4.8.1 + WPF | Pass | Supported (follows OS) | Follows OS | Smallest (preinstalled) | Alternative only if Win10 22H2 is mandatory |
| .NET 8 / 9 | Pass | Win10 support differs, [Unverified] | Ends 2026-11-10 | — | Rejected |

**Recommendation R-PLAT:**
- Target **.NET 10 LTS + WPF**, published **self-contained, untrimmed, folder layout** (not single-file), for `win-x64`. `win-arm64` is an optional second build.
- Package with **Inno Setup 7.x**, per-user (`PrivilegesRequired=lowest`) and fully offline.
- Ship the .NET Library License and Windows SDK License texts in the third-party notices.
- Each .NET security patch requires an app release [Hypothesis on cadence].

**Conflict requiring a decision (not resolved by research).** Approved CON-001 and NFR-025 say "Windows 10 and
Windows 11". With .NET 10, upstream support covers Windows 11 24H2+ and Windows 10 **Enterprise LTSC** only:
- **Option A (recommended):** change CON-001/NFR-025 to "Windows 11 24H2+ (supported, tested); Windows 10 Enterprise LTSC 21H2 (supported); Windows 10 22H2 (not supported, may work, untested)".
- **Option B:** keep Windows 10 22H2 and target .NET Framework 4.8.1. Cost: legacy runtime and older language and library support [Hypothesis].
- **Option C:** keep the wording and accept running unsupported on Windows 10 22H2. Not recommended, because it contradicts NFR-025's verification method.

Either A or B is a **substantive requirements change** (process contract §4) and needs your approval.

## 6. Unverified items and resolution
| Item | Resolution |
|---|---|
| WPF self-contained installed size | POC-004 measures it (NFR-028) |
| Inno Setup offline behavior and minimum Windows version | POC-004 clean-VM install |
| Bundling the Desktop Runtime installer | Not needed if self-contained is chosen |
| Meaning of (E)/(IoT)/(W) in the OS matrix | Read as Enterprise/IoT/Workstation [Hypothesis]. Low impact |
| Code signing for the installer (SmartScreen) | New open question for architecture: unsigned open-source installers may show warnings [Hypothesis] |

## Changelog
- 2026-10-26 v0.1: Initial research (SDLC-004).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:59 +0500). Content unchanged from v0.1.
- 2026-10-26: Minor. *Depends on* updated to requirements v2.0 after revalidation: v2.0 applies D-011/D-012, which came from this research. No content change.
- 2026-11-02 21:17 +0500: Addendum for TASK-108 (D-021), recorded before use: **ReadyToRun** (`PublishReadyToRun`) precompiles the application's own assemblies and its NuGet dependencies at publish time, removing their JIT time at startup. It needs the package Microsoft.NETCore.App.Crossgen2.win-x64 (Microsoft, MIT license, maintained with each .NET release, version tied to the SDK's runtime 10.0.x). It runs at build time only; the published assemblies get larger (native code next to IL), and nothing new is distributed beyond the application's own files. Offline builds need the package in the NuGet cache once. Status: [Hypothesis] until TASK-108 measures it.
