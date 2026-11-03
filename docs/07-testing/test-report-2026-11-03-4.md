# Test Report 2026-11-03-4

| Field | Value |
|---|---|
| Build under test | Commit `53c6cfc` (TASK-115 included since report 2026-11-03-3); no code changed during this phase. Only documents changed |
| Environment | Windows 11 Pro 10.0.26200 x64; .NET SDK 10.0.403; NASM 3.02 (bundled); Release configuration; the reference machine (D-014). The user's Assembly Studio was closed throughout |
| Executed by | Claude Code (Opus 5.5) session, 2026-11-03 19:27:00 – 21:52:01 +0500 (see "Interruption") |
| Test plan | `docs/07-testing/test-plan.md` v0.15 |
| Raw results | `artifacts/test-results/2026-11-03-4/` (git-ignored): 12 TRX and 12 xUnit XML files, `build.txt`, `console.txt`, `coverage-console.txt`, `coverage-summary.md`, `bench-console.txt`; measurements also in `artifacts/perf/e2e-2026-11-03-2149.txt` |

## Commands executed
```
dotnet build AssemblyStudio.slnx -c Release                                         → 0 warnings, 0 errors
powershell -File build/test.ps1 -NoBuild -ResultsDirectory artifacts/test-results/2026-11-03-4   → 19:28–19:32, exit 0
powershell -File build/coverage.ps1 -NoBuild        (first attempt stopped, see below)  → rerun 21:41–21:49, exit 0
powershell -File build/bench/run-e2e.ps1                                             → 21:49–21:52, exit 0
```

## Interruption
- **What happened:** after the suite finished (19:32), Claude Code stopped the background command during the coverage step,
  because the PC was critically low on memory while the session was idle. The first coverage attempt has no result, and the
  measurements had not started.
- **What I did:** reported it to the user. With the user's choice ("Rerun coverage + measurements"), I shut down the .NET build
  servers my builds had left (`dotnet build-server shutdown`), which left about 3.1 GB free. Then I ran coverage and the
  measurements again on the same build, in the foreground.
- **Effect on the results:** the suite results are from the first, uninterrupted run. No test result was taken from the stopped
  attempt.

## Summary
| Total | Passed | Failed | Skipped | Not run |
|---|---|---|---|---|
| 1587 | 1580 | 0 | 7 | 0 |

The runner reported step 1 `total: 1581, failed: 0, succeeded: 1577, skipped: 4` and step 2 (UI tests alone) `total: 6, failed: 0,
succeeded: 3, skipped: 3`. The 12 TRX files give the same counts, and the plan's TC counts add up to 1,587. The 7 skipped tests
are opt-in by design:
- TC-088 (4) and TC-089 (2) are run by the measurement script, which passed 7 of 7.
- The AC-043 real-key test passed at 18:52 today on `bb87ae3`. TASK-115 changed no UI.

## Results by test project
| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Engine.I8086.Tests | 628 | 0 | 0 |
| ProgramTests | 121 | 0 | 0 |
| Toolchain.Nasm.Tests | 146 | 0 | 0 |
| Runtime.Tests | 86 | 0 | 0 |
| Os.Dos.Tests | 72 | 0 | 0 |
| Debugging.Tests | 62 | 0 | 0 |
| App.Tests | 388 | 0 | 4 |
| App.UiTests | 3 | 0 | 3 |
| ArchitectureTests | 53 | 0 | 0 |
| Content.Tests | 13 | 0 | 0 |
| Core.Tests | 8 | 0 | 0 |

**Acceptance criteria (65):**
- **55 pass in this run.** AC-020 is checked at the new limit of 65,278 bytes (FR-031 v2.2, D-023).
- **5 pass on manual or record-based evidence:** AC-001, AC-043, AC-034, AC-044 and AC-059.
- **0 fail.**
- **5 not run:** AC-040, AC-041, AC-042, AC-061 and AC-062, whose tasks aren't built yet.

Requirement-level results are in traceability v2.14.

## Failures
None, in the suite or in the coverage rerun.

## End-to-end measurements (`build/bench/run-e2e.ps1`, 21:49–21:52, published application)
| NFR | Target | Result |
|---|---|---|
| NFR-001 editor latency | p95 ≤ 50 ms | best round p95 16.8 ms |
| NFR-002 step latency | p95 ≤ 100 ms | median 32.3, p95 43.4, max 67.4 ms |
| NFR-004 running | ≤ 100 ms | max 32.5 ms |
| NFR-004 Pause / Stop | ≤ 500 ms | 40.1 / 32.8 ms |
| NFR-004 hanging build | ≤ 100 ms | max 3.5 ms |
| NFR-005 warm start | ≤ 3 s | median 1,796 ms, max 1,857 ms |
| NFR-006 build | ≤ 2 s | max 648 ms |
| NFR-007 memory | ≤ 300 MB | peak 192.5 MB |

All pass. This run started with less free memory than earlier runs (about 3.9 GB). Across today's three runs, the warm-start median
ranged from 1,562 to 1,861 ms. The cold start after a restart (7,752 ms) is reported without a target (NFR-005 v2.1, D-022).

## Coverage (NFR-021)
| Project | Coverage | Lines |
|---|---|---|
| Engine | 99.6% | |
| Runtime | 95.3% | |
| Os.Dos | 99.1% | |
| Debugging | 98.9% | |
| Toolchain.Nasm | 92.6% | |
| Content | 94.0% | |
| App | 84.8% | |
| Core | 45.0% | 20 lines |
| **Total** | **90.3%** | 6,284 of 6,957 |

The NFR-021 target is 85% for the engine and runtime: met. The rerun completed with no failure.

## Defect re-runs
| Defect | Evidence | New status |
|---|---|---|
| DEF-003 .COM size limit | `ComSizeLimitTests`: 65,278 bytes builds, loads and runs; 65,279 fails with BLD006. Also `BuildDiagnosticsTests` and the help consistency tests (suite and coverage) | Verified |

## Regressions vs previous report (2026-11-03-3)
None. No test failed. The 2 new TASK-115 tests pass, and the AC-020 tests now check the new limit.

## Gap filling
None needed. Every `Done` task has executed tests (TC-001..TC-095). No test was added, deleted or skipped, and no production code
changed.

## Limitations
- **Not run:** AC-040, AC-041, AC-042, AC-061 and AC-062 (TASK-070, TASK-102, TASK-103 and TASK-105).
- **Installer:** not rebuilt or retested in this run (TASK-103).

## Defects raised / updated
- DEF-003: Verified.
