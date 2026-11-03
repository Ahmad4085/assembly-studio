# Test Report 2026-11-03-1

| Field | Value |
|---|---|
| Build under test | Commit `d1c9577` (TASK-107 and TASK-108 included); no code changed during this phase. Only documents changed |
| Environment | Windows 11 Pro 10.0.26200 x64; .NET SDK 10.0.403 (`dotnet --version`); NASM 3.02 (bundled, `tools/nasm`); Release configuration; the reference machine (D-014). Windows restarted at 10:49 the same morning |
| Executed by | Claude Code (Opus 5.5) session, 2026-11-03 11:23:44 – 11:41:14 +0500 |
| Test plan | `docs/07-testing/test-plan.md` v0.6 |
| Raw results | `artifacts/test-results/2026-11-03-1/` (git-ignored): 12 TRX and 12 xUnit XML files, `build.txt`, `console.txt` (step 1), `console-ui.txt` (step 2), `console-test-ps1-failed.txt` (the failed script attempt, DEF-024), `coverage-console.txt`, `coverage-summary.md`, `bench-console.txt`; measurements also in `artifacts/perf/e2e-2026-11-03-1134.txt`; the user's cold start in `artifacts/perf/cold-start-2026-11-03-1119.txt` |

## Commands executed
```
dotnet build AssemblyStudio.slnx -c Release                        → 0 warnings, 0 errors (11:23)
powershell -File build/test.ps1 -NoBuild -- --results-directory …   → did not run: "Parameter cannot be processed because the
                                                                       parameter name '' is ambiguous" (DEF-024); no test ran
powershell -File build/coverage.ps1 -NoBuild                        → 11:25–11:34, exit 0
powershell -File build/bench/run-e2e.ps1                            → 11:34–11:36, exit 0
dotnet test --solution AssemblyStudio.slnx -c Release --no-build --filter-not-namespace AssemblyStudio.App.UiTests --ignore-exit-code 8 --results-directory artifacts/test-results/2026-11-03-1 --report-xunit-trx --report-xunit-xml
                                                                    → step 1 of build/test.ps1, 11:36–11:41, exit 0
dotnet test --project tests/AssemblyStudio.App.UiTests -c Release --no-build --results-directory artifacts/test-results/2026-11-03-1 --report-xunit-trx --report-xunit-xml
                                                                    → step 2 (UI tests alone), 11:41, exit 0
```

## Summary
| Total | Passed | Failed | Skipped | Not run |
|---|---|---|---|---|
| 1560 | 1553 | 0 | 7 | 0 |

The runner reported step 1 `total: 1554, failed: 0, succeeded: 1550, skipped: 4` (4 min 08 s) and step 2 `total: 6, failed: 0,
succeeded: 3, skipped: 3` (15 s); the 12 TRX files give the same counts. The 7 skipped tests are opt-in by design and were not
counted as passed: TC-088 (4) and TC-089 (2), both run by the measurement script below, and the AC-043 keyboard-only workflow
in TC-087, which presses real keys (D-018) and was not run.

Measurement script (opt-in tests, executed): **7 of 7 passed**. Coverage run: **11 of 11 projects passed**.

## Results by test project
| Project | Passed | Failed | Skipped | Result |
|---|---|---|---|---|
| Engine.I8086.Tests | 628 | 0 | 0 | Pass |
| Runtime.Tests | 85 | 0 | 0 | Pass |
| Os.Dos.Tests | 72 | 0 | 0 | Pass |
| Toolchain.Nasm.Tests | 144 | 0 | 0 | Pass |
| Debugging.Tests | 62 | 0 | 0 | Pass |
| ProgramTests | 118 | 0 | 0 | Pass |
| App.Tests | 381 | 0 | 4 | Pass |
| App.UiTests | 3 | 0 | 3 | Pass |
| Content.Tests | 13 | 0 | 0 | Pass |
| Core.Tests | 8 | 0 | 0 | Pass |
| ArchitectureTests | 39 | 0 | 0 | Pass |

Acceptance criteria (65): **53 pass**, **1 fails** (AC-056: NFR-005's cold start, DEF-023), **11 not run** (AC-001, AC-034,
AC-040, AC-041, AC-042, AC-043, AC-044, AC-059, AC-060, AC-061, AC-062; test plan §6). Requirement-level results:
`docs/01-requirements/REQUIREMENTS_TRACEABILITY.md` v2.7. NFR-005 fails for the cold start; every other requirement with
executed tests passes.

## Failures
No automated test failed. One requirement measurement fails:

| Measurement | Result | Defect | Status |
|---|---|---|---|
| NFR-005 cold start after a Windows restart (`build/bench/cold-start.ps1`, run by the user 2026-11-03 11:19, 30 min after the restart) | 7,752 ms (target 3,000 ms): runtime and WPF 4,344 ms before the application's code | DEF-023 | Open; the user's decision OQ-027 |

## End-to-end measurements (`build/bench/run-e2e.ps1`, 11:34–11:36, published application)
| NFR | Target | Result | |
|---|---|---|---|
| NFR-001 editor latency | p95 ≤ 50 ms | best round p95 15.7 ms (rounds 30.6 / 22.6 / 15.7) | Pass |
| NFR-002 step latency | p95 ≤ 100 ms | median 30.2, p95 47.2, max 118.3 ms | Pass |
| NFR-004 running | ≤ 100 ms | max 24.0 ms | Pass |
| NFR-004 Pause / Stop | ≤ 500 ms | 33.1 / 29.9 ms | Pass |
| NFR-004 hanging build | ≤ 100 ms | max 38.9 ms | Pass |
| NFR-005 startup, warm | ≤ 3 s | median 1,975 ms, max 2,010 ms (5 launches) | Pass |
| NFR-006 build | ≤ 2 s | max 565 ms | Pass |
| NFR-007 memory | ≤ 300 MB | peak 199.9 MB | Pass |

## Coverage (NFR-021)
| Assembly | Line coverage |
|---|---|
| AssemblyStudio.Engine.I8086 | 99.6% |
| AssemblyStudio.Runtime | 93.8% |
| AssemblyStudio.Os.Dos | 99.0% |
| AssemblyStudio.Debugging | 98.9% |
| AssemblyStudio.Toolchain.Nasm | 92.2% |
| AssemblyStudio.Content | 94.0% |
| AssemblyStudio.App | 84.8% |
| AssemblyStudio.Core | 45.0% (20 coverable lines) |
| **Total** | **90.2%** (6,253 of 6,933 lines) |

NFR-021 (engine and runtime ≥ 85%) is met.

## Defect re-runs
| Defect | Evidence | New status |
|---|---|---|
| DEF-020 UI smoke test window not found | TC-087 passed (UI tests alone, and under coverage); TC-092 passed | Verified |
| DEF-021 NFR-005 warm startup | measurement on the published application: median 1,975 ms, max 2,010 ms | Verified (the cold start is DEF-023) |
| DEF-011 clipboard, DEF-022 find bar Esc | both tests passed in this run; neither has a fix | Open (unchanged) |

## Gap filling in this phase
None needed: every `Done` task has executed tests (TC-001..TC-093). No test was added, deleted or skipped; no production code
changed.

## Regressions vs previous report (2026-11-02-1)
None. Both previous failures (TC-087, DEF-020; NFR-005 warm, DEF-021) pass and are verified. New finding: the cold start
(DEF-023), measured for the first time.

## Limitations
- Not run: 11 acceptance criteria (test plan §6).
- The installer was not tested in this run (TASK-101: 21/21 on 2026-10-30; it now publishes with ReadyToRun, TASK-108, so it
  should be rebuilt and retested before release; the clean-VM test is TASK-103).
- Wall-clock results depend on machine load; reported as measured.

## Defects raised / updated
- DEF-020, DEF-021: Verified.
- DEF-024 (new, Low, Open): `build/test.ps1` can't pass further `dotnet test` options after `--` under `powershell -File`.
- DEF-023 (High, Open, from TASK-108): NFR-005 cold start 7,752 ms.
