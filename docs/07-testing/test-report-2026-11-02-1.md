# Test Report 2026-11-02-1

| Field | Value |
|---|---|
| Build under test | Commit `16814e0` (TASK-065 committed); production and test code unchanged during this phase. Only documents changed |
| Environment | Windows 11 Pro 10.0.26200 x64; .NET SDK 10.0.403 (`dotnet --version`); NASM 3.02 (bundled, `tools/nasm`); Release configuration; the reference machine (D-014: Intel Core i5-8265U, 15.9 GB). Rider, Chrome and a VPN client were running |
| Executed by | Claude Code (Opus 5.5) session, 2026-11-02 20:04:54 – 20:25:47 +0500 |
| Test plan | `docs/07-testing/test-plan.md` v0.3 |
| Raw results | `artifacts/test-results/2026-11-02-1/` (git-ignored): 11 TRX and 11 xUnit XML files, `build.txt`, `console.txt`, `coverage-console.txt`, `coverage-summary.md`, `bench-console.txt`; measurements also in `artifacts/perf/e2e-2026-11-02-2023.txt` |

## Commands executed
```
dotnet build AssemblyStudio.slnx -c Release                        → 0 warnings, 0 errors (20:04–20:06)
dotnet test --solution AssemblyStudio.slnx -c Release --no-build --results-directory artifacts/test-results/2026-11-02-1 --report-xunit-trx --report-xunit-xml
                                                                    → 20:06–20:10
powershell -File build/coverage.ps1 -NoBuild                        → 20:10–20:23, exit 0
powershell -File build/bench/run-e2e.ps1                            → 20:23–20:25, exit 1 (one measurement failed)
```

## Summary
| Total | Passed | Failed | Skipped | Not run |
|---|---|---|---|---|
| 1556 | 1548 | 1 | 7 | 0 |

The runner reported `total: 1556, failed: 1, succeeded: 1548, skipped: 7` (3 min 42 s); the 11 TRX files give the same counts.
The 7 skipped tests are opt-in by design and were not counted as passed: the 4 in-process end-to-end measurements (TC-088),
the 2 process measurements (TC-089), both run by the measurement script below, and the AC-043 keyboard-only workflow in TC-087,
which presses real keys (D-018) and was not run in this phase.

Measurement script (opt-in tests, executed in this phase): **7 tests, 6 passed, 1 failed** (NFR-005, DEF-021).

## Results by test project
| Project | TC IDs | Passed | Failed | Skipped | Result |
|---|---|---|---|---|---|
| Engine.I8086.Tests (unit, CPU engine) | TC-001–TC-017 | 628 | 0 | 0 | Pass |
| Runtime.Tests (unit, runtime) | TC-018–TC-023, TC-084 | 85 | 0 | 0 | Pass |
| Os.Dos.Tests (integration, DOS services) | TC-024–TC-025 | 72 | 0 | 0 | Pass |
| Toolchain.Nasm.Tests (integration, NASM) | TC-026–TC-033 | 144 | 0 | 0 | Pass |
| Debugging.Tests (integration, debug session) | TC-034–TC-042, TC-073 | 62 | 0 | 0 | Pass |
| ProgramTests (program level) | TC-043–TC-046 | 118 | 0 | 0 | Pass |
| App.Tests (UI, view model, real process) | TC-047–TC-066, TC-074–TC-086, TC-088, TC-090, TC-091 | 379 | 0 | 4 | Pass |
| App.UiTests (real process, FlaUI) | TC-087, TC-089 | 0 | 1 | 3 | **Fail** |
| Content.Tests | TC-067 | 13 | 0 | 0 | Pass |
| Core.Tests | TC-068–TC-069 | 8 | 0 | 0 | Pass |
| ArchitectureTests | TC-071–TC-072 | 39 | 0 | 0 | Pass |

Acceptance criteria (65): **53 pass**, **1 fails** (AC-056: NFR-005, DEF-021), **11 not run** (AC-001, AC-034, AC-040,
AC-041, AC-042, AC-043, AC-044, AC-059, AC-060, AC-061, AC-062; see test plan §6; AC-043 passed on 2026-11-02 12:52 when run with
real keys in TASK-085, and AC-044's Narrator checks were run manually by the user). Requirement-level results are in
`docs/01-requirements/REQUIREMENTS_TRACEABILITY.md` v2.6: FR-091 and FR-092 (TC-087, DEF-020) and NFR-005 (TC-089, DEF-021) fail.

## Failures
| TC | Test | Failure message (excerpt) | Defect | Status |
|---|---|---|---|---|
| TC-087 | `App.UiTests.UiSmokeTests.The_main_window_exposes_named_menus_commands_panels_and_the_editor` | "System.InvalidOperationException : The main window didn't appear." (30.9 s; FlaUI waits 30 s) | DEF-020 | Open |
| TC-089 | `App.UiTests.PerformanceTests.NFR_005_the_main_window_is_rendered_within_3_s_of_the_process_start` | "process start to rendered main window (warm starts, not after a reboot): n=5 median 3005 ms, max 3172 ms, first 2907 ms; target 3000 ms" | DEF-021 | Open |

Neither was retried, per the procedure. Notes from the evidence: TC-087's app logged its start at 20:06:47 and saved its
settings at 20:07:43 (it does so when its window closes), and the same test passed in the coverage run (20:10–20:23; App.UiTests there: 1 passed, 1 skipped).
TC-089 passed at 15:02 the same day (median 2,344 ms). Every measurement of this run was slower than at 15:02 (below), with other
heavy programs running, but a failure under load is still a failure: NFR-005 is about a cold start, which is slower than these.

## End-to-end measurements (`build/bench/run-e2e.ps1`, 20:23–20:25)
| NFR | Target | This run | TASK-086 run (15:02) | Result |
|---|---|---|---|---|
| NFR-001 editor latency | p95 ≤ 50 ms | rounds 56.7 / 39.6 / 21.8 ms; best round p95 21.8 ms | 24.3 / 17.8 / 13.0 | Pass (best of 3, DEF-010 rule) |
| NFR-002 step latency | p95 ≤ 100 ms | median 43.2, p95 73.3, max 215.6 ms | p95 36.1 | Pass |
| NFR-004 running | ≤ 100 ms | max 52.5 ms (p95 27.1) | max 30.3 | Pass |
| NFR-004 Pause / Stop | ≤ 500 ms | 68.3 / 34.2 ms | 26.6 / 22.7 | Pass |
| NFR-004 hanging build | ≤ 100 ms | max 5.6 ms; Cancel 28.4 ms | 2.6; 16.6 | Pass |
| NFR-005 startup | ≤ 3 s | median 3,005, max 3,172 ms | median 2,344 | **Fail** (DEF-021) |
| NFR-006 build | ≤ 2 s | max 1,131 ms | max 522 | Pass |
| NFR-007 memory | ≤ 300 MB | peak 189.2 MB | 193.8 | Pass |

## Coverage (NFR-021)
`build/coverage.ps1 -NoBuild` (20:10–20:23): **complete**, every project passed under instrumentation (11 of 11), so DEF-009
is verified.

| Assembly | Line coverage |
|---|---|
| AssemblyStudio.Engine.I8086 | 99.6% |
| AssemblyStudio.Runtime | 93.8% |
| AssemblyStudio.Os.Dos | 99.0% |
| AssemblyStudio.Debugging | 98.9% |
| AssemblyStudio.Toolchain.Nasm | 92.2% |
| AssemblyStudio.Content | 94.0% |
| AssemblyStudio.App | 85.9% |
| AssemblyStudio.Core | 45.0% (20 coverable lines; most of Core is records and interfaces) |
| **Total** | **90.9%** (6,243 of 6,871 lines) |

NFR-021 (engine and runtime ≥ 85%) is met.

## Defect re-runs
| Defect | Evidence in this phase | New status |
|---|---|---|
| DEF-006 flood test | TC-073 passed in the full run and under coverage | Verified |
| DEF-009 coverage incomplete | coverage run complete, 11 of 11 projects passed | Verified |
| DEF-010 editor latency test | TC-076 passed (full run, coverage, measurement script) | Verified |
| DEF-011 clipboard | TC-075 passed in this run, but the fix doesn't cover the path of its 2026-11-02 recurrence | Open (unchanged) |
| DEF-013 theme switch with underlines | TC-077 and the theme tests passed | Verified |
| DEF-017 UTF-8 BOM build | regression test passed; NFR-007 measurement (BOM file) built and ran | Verified |
| DEF-018 step latency | NFR-002 p95 73.3 ms ≤ 100 | Verified |
| DEF-019 output flood | NFR-004 running max 52.5 ms; Pause ≤ 68.3 ms | Verified |

## Gap filling in this phase
The test plan inventory (v0.3) now lists TC-074 to TC-091, the test classes added by TASK-064 to TASK-086, TASK-065 and TASK-106
since the last report. Every `Done` task has executed tests, so no test was added in this phase. No test was deleted or skipped,
and no production code changed.

## Regressions vs previous report (2026-10-30-1)
- The previous failure (TC-040/TC-073 flood, DEF-006) passes now and is verified.
- New failures: TC-087 (DEF-020) and the NFR-005 measurement in TC-089 (DEF-021). Both tests were added after the previous report
  (TASK-085, TASK-086). They aren't regressions in a test that passed in that report, but TC-087 had passed in all 13 full runs
  since TASK-085, and NFR-005 had passed at 13:21 and 15:02 today.

## Limitations
- Not run: 11 acceptance criteria (test plan §6) that need features not built yet, the user, clean machines, a network monitor or
  real key presses.
- The installer was not tested in this run. It passed 21/21 in TASK-101 (2026-10-30), and the clean-VM test is TASK-103.
- Wall-clock tests depend on machine load. Their results are reported as measured, not retried.

## Defects raised / updated
- DEF-020 (new, Low, Open): TC-087's main window wasn't found within 30 s. The test also leaves its process and temporary folder
  behind in that case.
- DEF-021 (new, Medium, Open): NFR-005 startup median 3,005 ms > 3 s.
- DEF-006, DEF-009, DEF-010, DEF-013, DEF-017, DEF-018, DEF-019: Verified.
