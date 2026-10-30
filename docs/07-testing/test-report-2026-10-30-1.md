# Test Report 2026-10-30-1

| Field | Value |
|---|---|
| Build under test | Commit `9153920` plus the uncommitted test additions of this phase (`AcceptanceStepTests`, the FR-001 process test); production code unchanged since `9153920` |
| Environment | Windows 11 Pro 10.0.26200 x64; .NET SDK 10.0.403 (`dotnet --version`); NASM 3.02 (bundled, `tools/nasm`); Release configuration; development machine |
| Executed by | Claude Code (Opus 5.5) session, 2026-10-30 20:58:28 – 21:02:34 +0500 |
| Test plan | `docs/07-testing/test-plan.md` v0.1 |
| Raw results | `artifacts/test-results/2026-10-30-1/` (git-ignored): 10 TRX and 10 xUnit XML files, `console.txt`, `build.txt`, `coverage/` |

## Commands executed
```
dotnet build AssemblyStudio.slnx -c Release                       → 0 warnings, 0 errors
dotnet test --solution AssemblyStudio.slnx -c Release --no-build --results-directory artifacts/test-results/2026-10-30-1 --report-xunit-trx --report-xunit-xml
powershell -File build/coverage.ps1 -NoBuild                       → artifacts/coverage/summary.md (copied to the raw results)
```

## Summary
| Total | Passed | Failed | Skipped | Not run |
|---|---|---|---|---|
| 1388 | 1387 | 1 | 0 | 0 |

The runner reported `total: 1388, failed: 1, succeeded: 1387, skipped: 0` (4 min 02 s). Six of the passed tests are the
placeholder `SkeletonTests` (TC-015, TC-023, TC-025, TC-032, TC-041, TC-070), which verify nothing beyond loading.

## Results by area
| Area | TC IDs | Passed | Failed | Result |
|---|---|---|---|---|
| Unit (CPU engine) | TC-001–TC-017 | 629 | 0 | Pass |
| Unit (runtime) | TC-018–TC-023 | 83 | 0 | Pass |
| Integration (DOS services on the engine) | TC-024–TC-025 | 73 | 0 | Pass |
| Integration (NASM toolchain) | TC-026–TC-033 | 140 | 0 | Pass |
| Integration (debug session) | TC-034–TC-042 | 61 | 1 | Fail |
| Program-level (assembler -> loader -> engine) | TC-043–TC-046 | 118 | 0 | Pass |
| UI / view model / process | TC-047–TC-066 | 222 | 0 | Pass |
| Unit (content catalog) | TC-067–TC-067 | 13 | 0 | Pass |
| Unit (core) | TC-068–TC-070 | 9 | 0 | Pass |
| Architecture rules | TC-071–TC-072 | 39 | 0 | Pass |

Acceptance criteria: 47 pass, 1 fails (AC-045 through TC-040, DEF-006), 16 not run (test plan §6). Requirement-level results are
in `docs/01-requirements/REQUIREMENTS_TRACEABILITY.md` v2.4 ("Test cases", "Test status").

## Failures
| TC | Failure message (excerpt) | Defect | Status |
|---|---|---|---|
| TC-040 `Debugging.Tests.Robustness.HostileProgramTests.A_flood_of_long_strings_reaches_the_budget_quickly` | "Took 00:00:10.1241321." (limit 10 s; "~40 million characters in 10.12 s") | DEF-006 | Open |

The test guards against the DEF-005 hang; alone it takes 1.6–1.8 s. During this run an Assembly Studio instance started by
the user (20:45, from the publish folder) was also running. Not retried, per the procedure.

## Coverage (NFR-021)
`build/coverage.ps1` (20:48–20:55) on the same build:

| Assembly | Line coverage |
|---|---|
| AssemblyStudio.Engine.I8086 | 99.6% |
| AssemblyStudio.Runtime | 93.5% |
| AssemblyStudio.Os.Dos | 99.0% |
| AssemblyStudio.Debugging | 98.4% |
| AssemblyStudio.Toolchain.Nasm | 91.7% |
| AssemblyStudio.Content | 94.0% |
| AssemblyStudio.App | 86.6% |
| AssemblyStudio.Core | 50.0% (18 coverable lines) |
| **Total** | **93.3%** |

NFR-021 (engine and runtime ≥ 85%) is met. The coverage run is marked **incomplete**: two wall-clock tests failed under
coverage instrumentation (TC-008 `Free_run_throughput_satisfies_nfr003_target`, TC-040 flood test, 18.6 and 18.7 s), so their
projects exited 2; line data was still collected for all 10 projects. Recorded as DEF-009.

## Gap filling in this phase
New tests for implemented behavior that no test covered:
- TC-034 `AcceptanceStepTests` (2): AC-024 session part (Step Into executes exactly one instruction; IP moves to the next
  instruction or jump target; the current instruction follows) and AC-026 session part (after `push ax`: SP = FFFCh, 1234h on
  the stack window, bytes `34 12` at SS:FFFC, both changed bytes reported). The panels (TASK-080, TASK-082) aren't built.
- TC-051 `OpenFromCommandLineProcessTests.FR_001_without_a_file_the_app_starts_with_an_editable_untitled_document`: the real
  application starts with an editable Untitled document (FR-001; the offline and timing parts of AC-001 remain not run).
No production code was changed.

## Regressions vs previous report
This is the first test-phase report. Compared with the last full runs recorded in `PROJECT_STATE.md` (1385/1385 at 20:36 and
20:38 on 2026-10-30): no regression in product behavior; the one failure is the known load-sensitive timing test (DEF-006,
first seen 2026-10-30 13:38).

## Limitations
- Not run and not counted: 16 acceptance criteria (test plan §6), for features not built yet or needing the user, clean
  machines, a network monitor or Narrator.
- The installer was tested in TASK-101 (`test-installer.ps1` 21/21, 2026-10-30 20:31), not in this run; the clean-VM test is
  TASK-103.
- Wall-clock tests depend on machine load; their results are reported, not retried.

## Defects raised / updated
- DEF-006 (Open): recurred in this run (10.12 s).
- DEF-009 (new, Open): wall-clock tests fail under coverage instrumentation, so the coverage run is reported incomplete.
