# ADR-0001: Platform — .NET 10 LTS + WPF on Windows x64

| Field | Value |
|---|---|
| Status | Approved |
| Version | 1.0 |
| Date | 2026-10-26 |
| Requirements addressed | CON-001, NFR-025, NFR-026, NFR-008, NFR-030, FR-083, FR-086 |
| Research | `docs/02-research/platform-and-packaging.md` (R-PLAT), D-011 |

## Context
The product is a Windows desktop IDE (CON-001) that must run offline (NFR-008) on Windows 11 24H2+ and Windows 10
Enterprise LTSC 21H2 (D-011, NFR-025). Research verified that .NET 10 is LTS, supported until 2028-11-14, and that it
supports exactly those Windows editions. It also verified that WPF ships with .NET 10 and is actively maintained, and that
.NET 8 and .NET 9 end support on 2026-11-10. POC-001/002 built and tested on .NET SDK 10.0.403 [Measured].

## Options considered
| Option | Pros | Cons | License / distribution impact |
|---|---|---|---|
| **.NET 10 + WPF** | Support to 2028-11-14. Matches D-011 exactly. Proven in POC-001/002. Modern C# | No trimming for WPF (larger self-contained output). Each runtime patch means an app release | MIT runtime + .NET Library License (WPF native DLLs) + Windows SDK License (D3DCompiler): notices required, no copyleft |
| .NET Framework 4.8.1 + WPF | Preinstalled. Smallest install. Also covers Windows 10 22H2 | Legacy runtime. Not needed after D-011. Older language and library support [Hypothesis] | Windows component, nothing to ship |
| .NET 8 / 9 + WPF | Same stack | Support ends 2026-11-10, before the first release | As .NET 10 |

## Decision
Target **.NET 10 LTS** (`net10.0-windows`) with **WPF**, **x64**. Arm64 is optional (it may be added later as a second
build; not required by NFR-025). Supported OS: as in D-011. Library projects that don't need Windows target `net10.0`.

## Consequences
- Upgrade to the next LTS before 2028-11-14. Ship a patch release when a .NET security patch affects the app.
- The WPF native DLLs' license texts go into the third-party notices (FR-090).
- C# language version and nullable reference types per `docs/sdlc/guides/dotnet-wpf-engineering.md`.

## Validation
- The first implementation task builds a WPF shell on `net10.0-windows`, with 0 warnings and analyzers on.
- AC-040 runs a clean install on Windows 11 24H2+ and Windows 10 Enterprise LTSC 21H2 (release phase).

## Changelog
- 2026-10-26 v0.1: Proposed (SDLC-006).
- 2026-10-26 v1.0: **Approved** by the user (2026-10-26 18:54 +0500: "approve architecture v0.1 and ADR-0001..0009; reference machine OK").
