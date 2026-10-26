# ADR-0008: Test framework, tools and oracle data

| Field | Value |
|---|---|
| Status | Approved |
| Version | 1.0 |
| Date | 2026-10-26 |
| Requirements addressed | NFR-017, NFR-020, NFR-021, NFR-016, NFR-022, FR-068 (sample verification), AC-043, AC-044, AC-056, AC-058 |
| Research | `docs/02-research/editor-mvvm-testing.md` (R-TEST), `poc-001.md`, `poc-002.md` |

## Context
Engine correctness must be proven against hardware data (NFR-017). Everything except the UI must be testable headless with one
command (NFR-020). POC-001 found that on the .NET 10 SDK, `dotnet test` with xUnit v3 requires the Microsoft.Testing.Platform (MTP)
mode [Measured].

## Options considered
| Question | Option | Notes |
|---|---|---|
| Framework | **xUnit v3 4.0.2** (Apache-2.0) | Used in both POCs. Theory data suits per-opcode oracle files |
| | NUnit 5 (MIT) | Ships a net10 build. Equally viable |
| | MSTest 4 (MIT) | Viable. No advantage here |
| UI automation | **FlaUI.UIA3 5.0.0** (MIT) | WinAppDriver and Appium Windows Driver are unmaintained or deprecated (research) |
| Coverage | **coverlet** (MIT) | Microsoft's coverage extension is proprietary, so it isn't used in the repo |
| Oracle data | **Fetch pinned SingleStepTests by script, git-ignored** | Vendoring about 39 MB (subset) or more in Git rejected |

## Decision
1. **xUnit v3** for all test projects. The repository root `global.json` sets `"test": {"runner": "Microsoft.Testing.Platform"}`.
   Tests run with `dotnet test --solution AssemblyStudio.slnx`. No VSTest adapter packages.
2. **Engine:** per-opcode SingleStepTests 8086 theory tests (100% required) plus hand-written spec tests. Data is fetched by
   `build/fetch-singlesteptests.ps1`, pinned to a commit with its source and license recorded. Missing data **fails** the tests
   with instructions (never skips).
3. **Toolchain:** the POC-002 fixtures and the three verifiers become regression tests (they need the bundled NASM).
4. **Program-level tests:** NASM fixture → load → run with scripted input → assert console output, exit path and state (samples
   FR-068, AC-022/023/064).
5. **UI smoke tests:** FlaUI.UIA3 for keyboard-only and Narrator-related checks (AC-043/044). Kept small. Manual checklists cover the rest (QA).
6. **Coverage:** coverlet via its Microsoft.Testing.Platform integration (package name `coverlet.MTP` per research; verified at setup).
   Targets per NFR-021.
7. **Analyzers:** `latest-recommended` with warnings as errors in product projects. Test projects disable CA1707 only.

## Consequences
- Running all tests offline requires the oracle data to have been fetched once. The data cache location is documented.
- FlaUI's release cadence (last release 2025-02) is a monitored risk.

## Validation
- The first M0 task creates the solution skeleton with `global.json` and runs `dotnet test` green (empty test).
- **Unverified:** the coverlet MTP package name and how it works with xunit.v3 4.0.2 on .NET 10. Checked when coverage is introduced.

## Changelog
- 2026-10-26 v0.1: Proposed (SDLC-006).
- 2026-10-26 v1.0: **Approved** by the user (2026-10-26 18:54 +0500: "approve architecture v0.1 and ADR-0001..0009; reference machine OK").
