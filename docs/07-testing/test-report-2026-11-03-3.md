# Test Report 2026-11-03-3

| Field | Value |
|---|---|
| Build under test | Commit `bb87ae3` (TASK-113, TASK-104, TASK-114 included since report 2026-11-03-2); no code changed during this phase. Only documents changed |
| Environment | Windows 11 Pro 10.0.26200 x64; .NET SDK 10.0.403 (`dotnet --version`); NASM 3.02 (bundled); Release configuration; the reference machine (D-014). The user's Assembly Studio was closed throughout |
| Executed by | Claude Code (Opus 5.5) session, 2026-11-03 18:31:29 – 18:47:02 +0500 |
| Test plan | `docs/07-testing/test-plan.md` v0.13 |
| Raw results | `artifacts/test-results/2026-11-03-3/` (git-ignored): 12 TRX and 12 xUnit XML files, `build.txt`, `console.txt`, `coverage-console.txt`, `coverage-summary.md`, `bench-console.txt`; measurements also in `artifacts/perf/e2e-2026-11-03-1844.txt` |

## Commands executed
```
dotnet build AssemblyStudio.slnx -c Release                                         → 0 warnings, 0 errors
powershell -File build/test.ps1 -NoBuild -ResultsDirectory artifacts/test-results/2026-11-03-3   → 18:32–18:37, exit 0
powershell -File build/coverage.ps1 -NoBuild                                         → 18:37–18:44, exit 0
powershell -File build/bench/run-e2e.ps1                                             → 18:44–18:47, exit 0
```

## Summary
| Total | Passed | Failed | Skipped | Not run |
|---|---|---|---|---|
| 1585 | 1578 | 0 | 7 | 0 |

The runner reported step 1 `total: 1579, failed: 0, succeeded: 1575, skipped: 4` and step 2 (UI tests alone) `total: 6, failed: 0,
succeeded: 3, skipped: 3`; the 12 TRX files give the same counts, and the plan's TC counts add up to 1,585. The 7 skipped tests are
opt-in by design:
- TC-088 (4) and TC-089 (2) are run by the measurement script, which passed 7 of 7.
- The AC-043 real-key test in TC-087 passed at 12:12 today on the code of `e6af964`. It hasn't been rerun since TASK-109..114.

## Results by test project
| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Engine.I8086.Tests | 628 | 0 | 0 |
| ProgramTests | 119 | 0 | 0 |
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
- **55 pass in this run**, including AC-060 for the first time (TC-094, TASK-104).
- **5 pass on manual or record-based evidence:** AC-001 (offline start, from the records), AC-043 (real keys at 12:12), and
  AC-034, AC-044 and AC-059 (user-reported).
- **0 fail.**
- **5 not run:** AC-040, AC-041, AC-042, AC-061 and AC-062, whose tasks aren't built yet.

Requirement-level results are in traceability v2.12.

## Failures
None, in the suite or under coverage.

## End-to-end measurements (`build/bench/run-e2e.ps1`, 18:44–18:47, published application)
| NFR | Target | Result |
|---|---|---|
| NFR-001 editor latency | p95 ≤ 50 ms | best round p95 13.9 ms |
| NFR-002 step latency | p95 ≤ 100 ms | median 27.9, p95 37.0, max 65.5 ms |
| NFR-004 running | ≤ 100 ms | max 25.0 ms |
| NFR-004 Pause / Stop | ≤ 500 ms | 38.0 / 47.9 ms |
| NFR-004 hanging build | ≤ 100 ms | max 5.2 ms |
| NFR-005 warm start | ≤ 3 s | median 1,861 ms, max 2,015 ms |
| NFR-006 build | ≤ 2 s | max 493 ms |
| NFR-007 memory | ≤ 300 MB | peak 201.2 MB |

All pass. The warm start is slower than in report 2026-11-03-2 (median 1,562 ms) and the peak memory higher (185.0 MB). Both are
well within target; this run has no comparison to separate run-to-run spread from a change. The cold start after a restart
(7,752 ms) is reported without a target (NFR-005 v2.1, D-022).

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

The NFR-021 target is 85% for the engine and runtime: met. This time the coverage run completed with no failures.

## Defect re-runs
| Defect | Evidence | New status |
|---|---|---|
| DEF-028 RTE021 wording | `HelpTests.TASK_114_…` and `RuntimeMessageTests` passed (suite and coverage) | Verified |

The intermittent DEF-011 (clipboard) and DEF-012 (crash recovery), seen in task runs today, did not recur in this run.

## Regressions vs previous report (2026-11-03-2)
None. No test failed (previous: none either). The 19 new tests from TASK-113, TASK-104 and TASK-114 pass.

## Gap filling
None needed. Every `Done` task has executed tests (TC-001..TC-094). No test was added, deleted or skipped, and no production code
changed.

## Limitations
- **Not run:** AC-040, AC-041, AC-042, AC-061 and AC-062 (TASK-070, TASK-102, TASK-103 and TASK-105; TASK-103 needs TASK-006).
- **Installer:** not rebuilt or retested in this run (TASK-103).
- **Real-key AC-043 test:** not rerun after TASK-109..114 (it's opt-in and needs the user away from the PC).

## Defects raised / updated
- DEF-028: Verified.
