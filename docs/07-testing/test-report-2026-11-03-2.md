# Test Report 2026-11-03-2

| Field | Value |
|---|---|
| Build under test | Commit `813137e` (TASK-109..112 included); no code changed during this phase. Only documents changed |
| Environment | Windows 11 Pro 10.0.26200 x64; .NET SDK 10.0.403 (`dotnet --version`); NASM 3.02 (bundled); Release configuration; the reference machine (D-014). The user's Assembly Studio (pid 25200) was open during the suite and coverage runs and closed before the measurements |
| Executed by | Claude Code (Opus 5.5) session, 2026-11-03 16:11:25 – 16:35:56 +0500 |
| Test plan | `docs/07-testing/test-plan.md` v0.9 |
| Raw results | `artifacts/test-results/2026-11-03-2/` (git-ignored): 12 TRX and 12 xUnit XML files, `build.txt`, `console.txt`, `coverage-console.txt`, `coverage-summary.md`, `bench-console.txt`; measurements also in `artifacts/perf/e2e-2026-11-03-1631.txt` |

## Commands executed
```
dotnet build AssemblyStudio.slnx -c Release                                         → 0 warnings, 0 errors
powershell -File build/test.ps1 -NoBuild -ResultsDirectory artifacts/test-results/2026-11-03-2   → 16:13–16:18, exit 0
powershell -File build/coverage.ps1 -NoBuild                                         → 16:18–16:25, exit 1 (one test failed under it)
powershell -File build/bench/run-e2e.ps1                                             → 16:31–16:35, exit 0
```

## Summary
| Total | Passed | Failed | Skipped | Not run |
|---|---|---|---|---|
| 1566 | 1559 | 0 | 7 | 0 |

The runner reported step 1 `total: 1560, failed: 0, succeeded: 1556, skipped: 4` and step 2 (UI tests alone) `total: 6, failed: 0,
succeeded: 3, skipped: 3`; the 12 TRX files give the same counts. The 7 skipped tests are opt-in by design: TC-088 (4) and TC-089 (2),
run by the measurement script (7 of 7 passed), and the AC-043 real-key test in TC-087 (passed at 12:12 today on the code of
`e6af964`; TASK-109..112 since then changed tests, the panels, the announcements and the control styles).

## Results by test project
| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Engine.I8086.Tests | 628 | 0 | 0 |
| Runtime.Tests | 85 | 0 | 0 |
| Os.Dos.Tests | 72 | 0 | 0 |
| Toolchain.Nasm.Tests | 144 | 0 | 0 |
| Debugging.Tests | 62 | 0 | 0 |
| ProgramTests | 118 | 0 | 0 |
| App.Tests | 384 | 0 | 4 |
| App.UiTests | 3 | 0 | 3 |
| Content.Tests | 13 | 0 | 0 |
| Core.Tests | 8 | 0 | 0 |
| ArchitectureTests | 39 | 0 | 0 |

Acceptance criteria (65): **54 pass in this run**, **5 pass on manual or record-based evidence** (AC-001 offline start from the
records, AC-043 real keys at 12:12, AC-034/AC-044/AC-059 user-reported), **0 fail**, **6 not run** (AC-040, AC-041, AC-042, AC-060,
AC-061, AC-062: tasks not built). Requirement-level results: traceability v2.11.

## Failures
None in the test run. Under the coverage run, one test failed: `EditorTests.AC_010_…` "OpenClipboard Failed (CLIPBRD_E_CANT_OPEN)"
(DEF-011, recurrence; the clipboard is shared by every program; the same test passed in the suite run).

## End-to-end measurements (`build/bench/run-e2e.ps1`, 16:31–16:35, published application)
| NFR | Target | Result |
|---|---|---|
| NFR-001 editor latency | p95 ≤ 50 ms | best round p95 14.3 ms |
| NFR-002 step latency | p95 ≤ 100 ms | median 33.8, p95 61.5, max 256.6 ms |
| NFR-004 running | ≤ 100 ms | max 57.7 ms |
| NFR-004 Pause / Stop | ≤ 500 ms | 42.1 / 31.8 ms |
| NFR-004 hanging build | ≤ 100 ms | max 3.3 ms |
| NFR-005 warm start | ≤ 3 s | median 1,562 ms, max 1,636 ms |
| NFR-006 build | ≤ 2 s | max 734 ms |
| NFR-007 memory | ≤ 300 MB | peak 185.0 MB |

All pass. The cold start after a restart (7,752 ms, 2026-11-03 11:19) is reported without a target (NFR-005 v2.1, D-022).

## Coverage (NFR-021)
Engine 99.6%, Runtime 93.8% (target 85%: met), Os.Dos 99.0%, Debugging 98.9%, Toolchain.Nasm 92.2%, Content 94.0%, App 84.8%,
Core 45.0% (20 lines); total 90.2% (6,262 of 6,940 lines). The run is **incomplete**: App.Tests exited 2 because of the DEF-011
failure above; line data was still collected for all 11 projects.

## Defect re-runs
| Defect | Evidence | New status |
|---|---|---|
| DEF-022 find bar test | TC-091 passed (suite and coverage) | Verified |
| DEF-024 `build/test.ps1` options | this run used `-ResultsDirectory`: 12 TRX + 12 XML | Verified |
| DEF-025 missing memory window crash | regression tests and all Variables/Memory/Build-Run tests passed | Verified |
| DEF-026 repeated announcement | regression test and all announcement tests passed | Verified |
| DEF-011 clipboard | recurred under coverage | Open |

## Regressions vs previous report (2026-11-03-1)
None. No test failed in the suite (previous: none either); new tests from TASK-109..112 pass.

## Gap filling
None needed; every `Done` task has executed tests (TC-001..TC-093). No test added, deleted or skipped; no production code changed.

## Limitations
- Not run: AC-040, AC-041, AC-042, AC-060, AC-061, AC-062 (TASK-102..105, TASK-103 needs TASK-006).
- The installer was not rebuilt or retested in this run (it publishes with ReadyToRun since TASK-108; TASK-103).
- The real-key AC-043 test was not rerun after TASK-109..112 (opt-in, needs the user away).

## Defects raised / updated
- DEF-022, DEF-024, DEF-025, DEF-026: Verified.
- DEF-011: recurrence recorded (Open).
