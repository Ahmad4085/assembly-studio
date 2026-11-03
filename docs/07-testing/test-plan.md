# Test Plan

| Field | Value |
|---|---|
| Document | Test plan |
| Status | Draft |
| Version | 0.6 |
| Last updated | 2026-11-02 |
| Depends on | `docs/01-requirements/` v2.x (requirements, acceptance criteria, traceability v2.5), `docs/03-architecture/architecture.md` v1.0, `docs/05-planning/backlog.md` |
| Latest report | `docs/07-testing/test-report-2026-11-03-1.md` |

## 1. Scope
Everything implemented so far: the 57 `Done` backlog tasks (M0 except TASK-006; M1, M2, M3; M4 except TASK-070; M5 including
the added TASK-106; M6; M7 TASK-101). TASK-061's automated part is done; its manual display check (AC-059) is waived until QA
(WVR-001). Not in scope because not built yet: fonts on Windows 10 LTSC (TASK-070), notices and supply chain (TASK-102), the
clean-VM installer test (TASK-103), the localization review (TASK-104) and the usability session (TASK-105). Their acceptance
criteria are listed as not run in §6.

## 2. Test levels and tools
| Level | Tool / framework | Project | Automated? |
|---|---|---|---|
| Unit: CPU semantics, flags, decoding, oracle conformance | xUnit v3 on Microsoft.Testing.Platform; SingleStepTests 8086 data (`build/fetch-singlesteptests.ps1`) | `tests/AssemblyStudio.Engine.I8086.Tests` | Yes |
| Unit: runtime (memory, loader, console, interrupt routing) | xUnit v3 | `tests/AssemblyStudio.Runtime.Tests` | Yes |
| Integration: DOS services on the real engine | xUnit v3 | `tests/AssemblyStudio.Os.Dos.Tests` | Yes |
| Integration: NASM toolchain (bundled NASM 3.02) | xUnit v3 | `tests/AssemblyStudio.Toolchain.Nasm.Tests` | Yes |
| Integration: debug session, robustness, fuzz/soak | xUnit v3 | `tests/AssemblyStudio.Debugging.Tests` | Yes |
| Program-level: source → NASM → loader → engine → output; samples, reference, help | xUnit v3 | `tests/AssemblyStudio.ProgramTests` | Yes |
| UI / view model / real-process (UI Automation) | xUnit v3, WPF on an STA thread, System.Windows.Automation | `tests/AssemblyStudio.App.Tests` | Yes |
| UI on the real process (FlaUI.UIA3 5.0.0, ADR-0008) | xUnit v3 | `tests/AssemblyStudio.App.UiTests` | Yes (the real-key test is opt-in, D-018) |
| End-to-end performance on the reference machine (D-014) | xUnit v3, `build/bench/run-e2e.ps1` (`ASSEMBLY_STUDIO_BENCH=1`) | `App.Tests`, `App.UiTests` | Yes (opt-in) |
| Content catalog | xUnit v3 | `tests/AssemblyStudio.Content.Tests` | Yes |
| Architecture rules (ADR-0002) | xUnit v3, reflection and project-file analysis | `tests/AssemblyStudio.ArchitectureTests` | Yes |
| Coverage (NFR-021) | coverlet.MTP, `build/coverage.ps1` | all test projects | Yes |
| Installer install/upgrade/uninstall | PowerShell, `build/installer/test-installer.ps1` | — | Yes (needs a per-user install; run in TASK-101 and TASK-103) |
| Manual: display scaling (AC-059), Narrator/high contrast (AC-044), offline with a network monitor (AC-001, AC-034), clean VMs (AC-040, AC-041), usability (AC-062) | — | — | No |

## 3. Test inventory
One test case (TC) per test class. IDs are stable: new classes get the next free number; a removed class keeps its number
retired. "Verifies" lists the IDs the class names, plus three links that the sources don't name, marked here by evidence:
TC-071/TC-072 (ADR-0002 dependency and project-file rules) verify NFR-019, NFR-020 and the modularity part of AC-057, and
TC-058 (no networking library in any application assembly) verifies the static part of NFR-009.

| ID | Test class | Level | Verifies | Tests |
|---|---|---|---|---|
| TC-001 | `Engine.I8086.Tests.ArithmeticTests` | Unit (CPU engine) | AC-025, FR-037, TASK-013, TASK-014 | 20 |
| TC-002 | `Engine.I8086.Tests.ControlTransferTests` | Unit (CPU engine) | TASK-016 | 37 |
| TC-003 | `Engine.I8086.Tests.Cpu8086Tests` | Unit (CPU engine) | AC-028, FR-037, TASK-011 | 16 |
| TC-004 | `Engine.I8086.Tests.DataTransferTests` | Unit (CPU engine) | TASK-012 | 30 |
| TC-005 | `Engine.I8086.Tests.DecoderTests` | Unit (CPU engine) | TASK-011 | 21 |
| TC-006 | `Engine.I8086.Tests.DeterminismTests` | Unit (CPU engine) | AC-047, NFR-016, TASK-020 | 2 |
| TC-007 | `Engine.I8086.Tests.EngineConformanceReportTests` | Unit (CPU engine) | AC-058, FR-036, FR-045, NFR-017, TASK-020 | 18 |
| TC-008 | `Engine.I8086.Tests.EnginePerformanceTests` | Unit (CPU engine) | AC-056, NFR-002, NFR-003, TASK-020 | 2 |
| TC-009 | `Engine.I8086.Tests.FlagsMiscAndOpcodeTableTests` | Unit (CPU engine) | FR-036, FR-037, FR-045, NFR-017, TASK-018 | 40 |
| TC-010 | `Engine.I8086.Tests.IcedDisassemblerTests` | Unit (CPU engine) | FR-053, FR-058, TASK-019 | 76 |
| TC-011 | `Engine.I8086.Tests.LogicShiftRotateTests` | Unit (CPU engine) | FR-037, TASK-014 | 22 |
| TC-012 | `Engine.I8086.Tests.MulDivBcdTests` | Unit (CPU engine) | AC-031, TASK-015 | 21 |
| TC-013 | `Engine.I8086.Tests.Oracle.SingleStepOracleHelperTests` | Unit (CPU engine) | — | 6 |
| TC-014 | `Engine.I8086.Tests.SingleStepOracleTests` | Unit (CPU engine) | TASK-012, TASK-013, TASK-014, TASK-015, TASK-016, TASK-017, TASK-018 | 283 |
| TC-015 | Retired 2026-10-30 (placeholder `SkeletonTests` deleted in TASK-087, user-approved) | — | — | 0 |
| TC-016 | `Engine.I8086.Tests.StringInstructionTests` | Unit (CPU engine) | FR-036, NFR-017, TASK-017 | 16 |
| TC-017 | `Engine.I8086.Tests.UndefinedFlagMaskTests` | Unit (CPU engine) | FR-048, TASK-092 | 18 |
| TC-018 | `Runtime.Tests.ComLoaderTests` | Unit (runtime) | AC-021, FR-056 | 9 |
| TC-019 | `Runtime.Tests.ConsoleDeviceTests` | Unit (runtime) | AC-045, AC-047, FR-063, NFR-015, NFR-016, TASK-036, TASK-054 | 26 |
| TC-020 | `Runtime.Tests.InterruptRouterTests` | Unit (runtime) | AC-029, AC-031, FR-038, FR-040, NFR-013 | 18 |
| TC-021 | `Runtime.Tests.MachineTests` | Unit (runtime) | — | 5 |
| TC-022 | `Runtime.Tests.RealModeMemoryTests` | Unit (runtime) | — | 24 |
| TC-023 | Retired 2026-10-30 (placeholder `SkeletonTests` deleted in TASK-087, user-approved) | — | — | 0 |
| TC-024 | `Os.Dos.Tests.DosServicesTests` | Integration (DOS services on the engine) | AC-022, AC-023, AC-064, TASK-034, TASK-035, TASK-036 | 72 |
| TC-025 | Retired 2026-10-30 (placeholder `SkeletonTests` deleted in TASK-087, user-approved) | — | — | 0 |
| TC-026 | `Toolchain.Nasm.Tests.BuildDiagnosticsTests` | Integration (NASM toolchain) | AC-013, AC-014, AC-018, AC-020, AC-055, FR-021, FR-027, FR-029, FR-031, FR-066, NFR-024, TASK-041 | 28 |
| TC-027 | `Toolchain.Nasm.Tests.BuildTimeTests` | Integration (NASM toolchain) | AC-056, NFR-006, TASK-086 | 1 |
| TC-028 | `Toolchain.Nasm.Tests.CpuDirectiveGuardTests` | Integration (NASM toolchain) | FR-027, TASK-042 | 16 |
| TC-029 | `Toolchain.Nasm.Tests.HelpConsistencyTests` | Integration (NASM toolchain) | AC-051, FR-032, TASK-093 | 16 |
| TC-030 | `Toolchain.Nasm.Tests.NasmAssemblerTests` | Integration (NASM toolchain) | AC-015, AC-016, AC-019, FR-023, FR-024, FR-025, FR-026, FR-030, FR-095, NFR-014, TASK-040 | 29 |
| TC-031 | `Toolchain.Nasm.Tests.NasmLocatorTests` | Integration (NASM toolchain) | — | 5 |
| TC-032 | Retired 2026-10-30 (placeholder `SkeletonTests` deleted in TASK-087, user-approved) | — | — | 0 |
| TC-033 | `Toolchain.Nasm.Tests.SourceMapTests` | Integration (NASM toolchain) | TASK-042, TASK-043 | 49 |
| TC-034 | `Debugging.Tests.AcceptanceStepTests` | Integration (debug session) | AC-024, AC-026, FR-049, FR-054, FR-055, FR-056, TASK-080, TASK-082 | 2 |
| TC-035 | `Debugging.Tests.BreakpointStepOverTests` | Integration (debug session) | AC-032, AC-033, FR-050, FR-052, TASK-052 | 12 |
| TC-036 | `Debugging.Tests.DebugSessionTests` | Integration (debug session) | AC-027, AC-030, AC-054, NFR-004, NFR-010, TASK-050 | 15 |
| TC-037 | `Debugging.Tests.InputAndReplayTests` | Integration (debug session) | AC-047, FR-064, NFR-016, TASK-034, TASK-036, TASK-051, TASK-053 | 10 |
| TC-038 | `Debugging.Tests.Robustness.FaultContainmentTests` | Integration (debug session) | NFR-010, TASK-054 | 4 |
| TC-039 | `Debugging.Tests.Robustness.FuzzAndSoakTests` | Integration (debug session) | NFR-010, TASK-054 | 2 |
| TC-040 | `Debugging.Tests.Robustness.HostileProgramTests` | Integration (debug session) | AC-045, FR-096, NFR-015, TASK-054 | 3 |
| TC-041 | Retired 2026-10-30 (placeholder `SkeletonTests` deleted in TASK-087, user-approved) | — | — | 0 |
| TC-042 | `Debugging.Tests.SnapshotTests` | Integration (debug session) | AC-048, AC-049, FR-053, NFR-002, NFR-018, TASK-051 | 13 |
| TC-043 | `ProgramTests.HelpTests` | Program-level (assembler -> loader -> engine) | AC-051, FR-045, NFR-033, TASK-093 | 6 |
| TC-044 | `ProgramTests.ProgramTests` | Program-level (assembler -> loader -> engine) | AC-017, AC-021, AC-022, AC-023, AC-029, AC-031, AC-047, AC-064, TASK-044 | 14 |
| TC-045 | `ProgramTests.ReferenceTests` | Program-level (assembler -> loader -> engine) | AC-037, FR-071, FR-072, TASK-092 | 80 |
| TC-046 | `ProgramTests.SampleTests` | Program-level (assembler -> loader -> engine) | AC-035, FR-068, TASK-091 | 18 |
| TC-047 | `App.Tests.About.AboutTests` | UI / view model / process | AC-038, FR-076, FR-087, FR-090, TASK-068 | 10 |
| TC-048 | `App.Tests.Documents.AtomicFileAndErrorsTests` | UI / view model / process | AC-004, FR-004, FR-008, NFR-029, TASK-062 | 5 |
| TC-049 | `App.Tests.Documents.CrashRecoveryProcessTests` | UI / view model / process | AC-050, NFR-011 | 1 |
| TC-050 | `App.Tests.Documents.DocumentUiTests` | UI / view model / process | AC-005, AC-006, TASK-062 | 6 |
| TC-051 | `App.Tests.Documents.OpenFromCommandLineProcessTests` | UI / view model / process | FR-001, FR-084, TASK-101 | 2 |
| TC-052 | `App.Tests.Documents.RecoveryTests` | UI / view model / process | NFR-011, TASK-063 | 7 |
| TC-053 | `App.Tests.Documents.TextFileCodecTests` | UI / view model / process | FR-019, FR-020, TASK-062 | 12 |
| TC-054 | `App.Tests.Documents.WorkspaceViewModelTests` | UI / view model / process | AC-002, AC-003, AC-004, AC-005, AC-006, AC-007, AC-008, AC-012, TASK-062 | 19 |
| TC-055 | `App.Tests.JsonLayoutStoreTests` | UI / view model / process | TASK-060 | 6 |
| TC-056 | `App.Tests.Learn.HelpPagesTests` | UI / view model / process | AC-051, NFR-033, TASK-093 | 49 |
| TC-057 | `App.Tests.Learn.LearnTests` | UI / view model / process | AC-035, AC-036, FR-069, FR-070, TASK-090, TASK-091, TASK-092, TASK-093 | 12 |
| TC-058 | `App.Tests.Logging.LoggingTests` | UI / view model / process | AC-063, NFR-009, NFR-012, TASK-068 | 10 |
| TC-059 | `App.Tests.MainWindowSmokeTests` | UI / view model / process | NFR-026, TASK-060 | 8 |
| TC-060 | `App.Tests.Settings.SettingsProcessTests` | UI / view model / process | AC-039, AC-063, FR-081 | 1 |
| TC-061 | `App.Tests.Settings.SettingsTests` | UI / view model / process | AC-015, AC-039, AC-053, AC-054, FR-079, FR-080, TASK-067 | 27 |
| TC-062 | `App.Tests.Settings.SettingsUiTests` | UI / view model / process | TASK-067 | 3 |
| TC-063 | `App.Tests.ShellViewModelTests` | UI / view model / process | TASK-060 | 17 |
| TC-064 | `App.Tests.StartupAndScalingTests` | UI / view model / process | FR-084, NFR-005, NFR-026 | 10 |
| TC-065 | `App.Tests.StartupTimeTests` | UI / view model / process | NFR-005, TASK-060 | 1 |
| TC-066 | `App.Tests.ThemeTokenTests` | UI / view model / process | NFR-022, TASK-061 | 19 |
| TC-067 | `Content.Tests.ContentCatalogTests` | Unit (content catalog) | FR-068, FR-069, TASK-090 | 13 |
| TC-068 | `Core.Tests.CpuStateTests` | Unit (core) | — | 4 |
| TC-069 | `Core.Tests.MachineSnapshotTests` | Unit (core) | — | 4 |
| TC-070 | Retired 2026-10-30 (placeholder `SkeletonTests` deleted in TASK-087, user-approved) | — | — | 0 |
| TC-071 | `ArchitectureTests.ArchitectureRuleTests` | Architecture rules | AC-057, NFR-019, NFR-020 | 23 |
| TC-072 | `ArchitectureTests.ProjectFileRuleTests` | Architecture rules | AC-057, NFR-019 | 16 |
| TC-073 | `Debugging.Tests.Robustness.FloodPerformanceTests` | Integration (debug session), performance | AC-045, FR-096, NFR-015, TASK-054, TASK-087 | 1 |
| TC-074 | `App.Tests.Editor.AvalonEditPlatformTests` | UI / view model | TASK-064 | 1 |
| TC-075 | `App.Tests.Editor.EditorTests` | UI / view model | AC-009, AC-010, AC-046, FR-011, FR-012, FR-013, FR-014, FR-022, NFR-001, TASK-064 | 12 |
| TC-076 | `App.Tests.Editor.EditorLatencyTests` | UI, performance | NFR-001, TASK-064, DEF-010 | 1 |
| TC-077 | `App.Tests.Editor.AdornmentTests` | UI / view model | AC-013, FR-018, FR-052, FR-053, TASK-066, DEF-013 | 9 |
| TC-078 | `App.Tests.Commands.BuildRunTests` | UI / view model (real NASM and session) | AC-013, AC-017, FR-028, FR-049, FR-052, FR-067, TASK-080 | 9 |
| TC-079 | `App.Tests.Commands.CommandTableTests` | UI / view model | AC-013, AC-017, FR-091, UI-016, TASK-065, TASK-080, TASK-085 | 6 |
| TC-080 | `App.Tests.Panels.ConsoleTests` | UI / view model (real NASM and session) | AC-022, AC-023, AC-045, FR-063, FR-064, NFR-015, UI-010, TASK-080, TASK-084 | 25 |
| TC-081 | `App.Tests.Panels.CpuTests` | UI / view model (real NASM and session) | AC-025, AC-037, AC-052, FR-047, FR-057, NFR-018, UI-005, UI-006, TASK-081, DEF-008 | 17 |
| TC-082 | `App.Tests.Panels.MemoryTests` | UI / view model (real NASM and session) | AC-026, FR-054, NFR-018, UI-008, UI-009, TASK-082 | 20 |
| TC-083 | `App.Tests.Panels.SessionPanelsTests` | UI / view model (real NASM and session) | AC-024, AC-028, AC-029, AC-031, AC-048, AC-049, FR-041, FR-042, FR-052, FR-058, UI-007, UI-012, UI-013, UI-014, TASK-083 | 9 |
| TC-084 | `Runtime.Tests.RuntimeMessageTests` | Unit (runtime) | FR-038, FR-040, FR-066, TASK-083 | 3 |
| TC-085 | `App.Tests.Accessibility.EditorAccessibilityTests` | UI / UI Automation | FR-093, NFR-022, TASK-069 | 2 |
| TC-086 | `App.Tests.Panels.AnnouncementTests` | UI / view model | FR-093, TASK-085 | 3 |
| TC-087 | `App.UiTests.UiSmokeTests` | UI (real process, FlaUI) | AC-043, FR-091, FR-092, TASK-085 | 2 |
| TC-088 | `App.Tests.Performance.EndToEndPerformanceTests` | UI, performance (opt-in) | AC-030, NFR-002, NFR-004, NFR-006, NFR-007, TASK-086 | 4 |
| TC-089 | `App.UiTests.PerformanceTests` | UI (real process), performance (opt-in) | NFR-005, NFR-007, TASK-086, DEF-017 | 2 |
| TC-090 | `App.Tests.Panels.VariablesTests` | UI / view model (real NASM and session) | AC-065, FR-057, FR-097, NFR-018, UI-023, TASK-106 | 18 |
| TC-091 | `App.Tests.Editor.FindReplaceTests` | UI / view model | AC-011, FR-015, FR-016, FR-017, UI-004, TASK-065 | 22 |
| TC-092 | `App.UiTests.AppUnderTestTests` | UI test harness | DEF-020, TASK-107 | 2 |
| TC-093 | `App.Tests.Logging.StartupTimelineTests` | Unit (startup diagnostics) | NFR-005, DEF-021, TASK-108 | 2 |

Deterministic inputs and expected results are in each test. Engine tests use hand-assembled images and the SingleStepTests
data; random tests use fixed seeds (fuzz: per-test seeds; reference flag probes: a seed per mnemonic).

## 4. Environment
Recorded per run in the test report: OS, `dotnet --version`, NASM version, commit. Run 2026-10-30-1: Windows 11 Pro
10.0.26200 x64, .NET SDK 10.0.403, NASM 3.02 (bundled), Release configuration.

Commands:
- Full suite: `powershell -File build/test.ps1` (TASK-107: every project except the UI tests, then the UI tests alone, so the
  real-application window wait doesn't compete with the other projects; add `-NoBuild` and, after `--`, the result options
  `--results-directory <dir> --report-xunit-trx --report-xunit-xml`). `dotnet test --solution AssemblyStudio.slnx -c Release`
  still runs everything at once.
- Coverage: `powershell -File build/coverage.ps1 -NoBuild` → `artifacts/coverage/summary.md`
- Installer: `build/installer/build-installer.ps1`, then `build/installer/test-installer.ps1` (see TASK-101 verification)

## 5. Entry and exit criteria
Entry: Release build with 0 warnings and 0 errors. Exit: every test for implemented P0 requirements executed; every failure
has a defect; no unexplained failure.

## 6. Known limitations
- **Performance tests** (not deterministic by nature) carry the trait `Category=Performance`: TC-008 engine performance,
  TC-027 build time, TC-065 startup time, TC-073 flood (CPU time), TC-076 editor latency, and single methods in TC-019,
  TC-036 and TC-042. They run in non-parallel collections within their assembly and are left out of the instrumented
  coverage run (DEF-009).
- **Opt-in tests** skip in a normal full run and run on request: TC-088 and TC-089 (the end-to-end measurements, run by
  `build/bench/run-e2e.ps1`), and the AC-043 keyboard-only workflow in TC-087, which presses real keys
  (`ASSEMBLY_STUDIO_UI_INPUT=1`, only with the user's agreement and the user away from the PC, D-018).
- **Not run (not built):** AC-060 (localizable strings, TASK-104), AC-061 and AC-042 (supply chain and notices, TASK-102).
- **Not run (needs the user or clean machines):** AC-059 (display scaling; waived until QA, WVR-001), AC-001 and AC-034
  (offline with a network monitor), AC-040 and AC-041 (clean VMs, TASK-103; the installer script passed 21/21 on the
  development machine in TASK-101), AC-062 (usability session, TASK-105).
- **Manual checks run by the user** (not automated, recorded as manual evidence): the Narrator walkthrough of the editor
  (TASK-069) and of the step announcements (TASK-085), part of AC-044.

## Changelog
- 2026-10-30 v0.1: Initial plan (testing phase, Claude Code (Opus 5.5)): inventory TC-001..TC-072 from the executed suite.
- 2026-10-30 v0.2: TASK-087: TC-015, TC-023, TC-025, TC-032, TC-041, TC-070 retired (placeholders deleted); TC-073 `FloodPerformanceTests` (moved from TC-040); performance trait and coverage exclusion.
- 2026-11-02 v0.3: Testing phase run 2026-11-02-1 (Claude Code (Opus 5.5)): TC-074..TC-091 added for the test classes of TASK-064..086, 065 and 106 (one per class, IDs as the classes name them); test counts refreshed from the run; scope, levels (FlaUI UI tests, opt-in end-to-end measurements) and limitations updated.
- 2026-11-02 v0.4: TASK-107: TC-092 `AppUnderTestTests`; the full-suite command is `build/test.ps1` (UI tests run on their own).
- 2026-11-03 v0.5: TASK-108: TC-093 `StartupTimelineTests`; the NFR-005 and NFR-007 process measurements (TC-089) run on the application as the installer publishes it (`run-e2e.ps1` sets `ASSEMBLY_STUDIO_APP_EXE`); the cold start after a reboot is `build/bench/cold-start.ps1` (user-run).
- 2026-11-03 v0.6: Test run 2026-11-03-1: counts refreshed; the full suite ran as the two steps of `build/test.ps1` (the script's `--` pass-through fails, DEF-024).
