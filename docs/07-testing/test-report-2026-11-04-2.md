# Test Report 2026-11-04-2

| Field | Value |
|---|---|
| Build under test | Commit `e38fb3b` (TASK-117 website and TASK-118 logo since report 2026-11-04-1); only the plan's TC-098 class name was corrected during this phase (documents only) |
| Environment | Windows 11 Pro 10.0.26200 x64; .NET SDK 10.0.403; NASM 3.02 (bundled); Release configuration; the reference machine (D-014). Steps run in the foreground. The user's installed Assembly Studio 1.0.0 was open during part of the run (started 14:22, from `%LOCALAPPDATA%\Programs`, not the build folders); the user agreed to the measurements running alongside it |
| Executed by | Claude Code (Opus 5.5) session, 2026-11-04 14:13:00 – 14:33:14 +0500 |
| Test plan | `docs/07-testing/test-plan.md` v0.20 |
| Raw results | `artifacts/test-results/2026-11-04-2/` (git-ignored): 12 TRX and 12 xUnit XML files, `build.txt`, `console.txt`, `coverage-console.txt`, `coverage-summary.md`, `bench-console.txt`; measurements also in `artifacts/perf/e2e-2026-11-04-1429.txt` |

## Commands executed
```
dotnet build AssemblyStudio.slnx -c Release                                         → 0 warnings, 0 errors
powershell -File build/test.ps1 -NoBuild -ResultsDirectory artifacts/test-results/2026-11-04-2   → 14:14–14:19, exit 0
powershell -File build/coverage.ps1 -NoBuild                                         → 14:20–14:28, exit 0
powershell -File build/bench/run-e2e.ps1                                             → 14:29–14:33, exit 0
```

## Summary
| Total | Passed | Failed | Skipped | Not run |
|---|---|---|---|---|
| 1596 | 1589 | 0 | 7 | 0 |

- **Runner output:** step 1 reported `total: 1590, failed: 0, succeeded: 1586, skipped: 4`; step 2 (UI tests alone) reported
  `total: 6, failed: 0, succeeded: 3, skipped: 3`.
- **Cross-checks:** the 12 TRX files give the same counts, and the plan's TC counts add up to 1,596. Compared with report
  2026-11-04-1, the change is the 2 new TASK-118 tests (TC-098) in App.Tests.
- **The 7 skipped tests are opt-in by design:**
  - TC-088 (4) and TC-089 (2) are run by the measurement script, which passed 7 of 7.
  - The AC-043 real-key test passed on 2026-11-03 at 18:52. TASK-118 added only a window icon to the UI.

**Acceptance criteria (65):**
- **57 pass in this run.**
- **5 pass on manual or record-based evidence.**
- **0 fail.**
- **3 not run:** AC-040 and AC-041 (clean machines, WVR-002) and AC-062 (usability session).

Requirement-level results are in traceability v2.18.

## Failures
None, in the suite or under coverage.

During TASK-118's own full runs (13:51–13:55), the View menu test and the editor latency test failed once each (DEF-001 and
DEF-016; DEF-010, reopened). Neither recurred in this run.

## End-to-end measurements (`build/bench/run-e2e.ps1`, 14:29–14:33, published application)
| NFR | Target | Result |
|---|---|---|
| NFR-001 editor latency | p95 ≤ 50 ms | best round p95 22.0 ms (rounds 44.7, 42.1, 22.0) |
| NFR-002 step latency | p95 ≤ 100 ms | median 29.1, p95 41.1, max 57.5 ms |
| NFR-004 running | ≤ 100 ms | max 25.4 ms |
| NFR-004 Pause / Stop | ≤ 500 ms | 37.2 / 29.3 ms |
| NFR-004 hanging build | ≤ 100 ms | max 2.0 ms |
| NFR-005 warm start | ≤ 3 s | median 1,919 ms, max 2,015 ms |
| NFR-006 build | ≤ 2 s | max 705 ms |
| NFR-007 memory | ≤ 300 MB | peak 196.1 MB |

All pass. The editor-latency rounds were higher than in earlier runs (worst round 44.7 ms) while the user's own Assembly Studio was
open; the best round, which the test uses, is 22.0 ms.

## Coverage (NFR-021)
- **Engine:** 99.6%.
- **Runtime:** 95.3%. The target is 85%: met.
- **Total:** 90.3% (6,285 of 6,957 lines).

The coverage run completed with no failure.

## Defect re-runs
None were waiting for verification. DEF-010 was reopened on 2026-11-04 after a recurrence, and its test passed in this run.

## Regressions vs previous report (2026-11-04-1)
None. The 2 new tests pass.

## Gap filling
None needed. Every `Done` task has executed tests (TC-001..TC-098). No test was added, deleted or skipped, and no production code
changed. One document fix: the plan's TC-098 class name now matches the code (`App.Tests.Shell.AppIconTests`).

## Limitations
- **Not run:** AC-040, AC-041 (WVR-002) and AC-062.
- **The window icon on screen** was not looked at: it isn't rendered in-process, and the test checks the resource and the setting.
  The executable's icon was extracted and checked during TASK-118.

## Defects raised / updated
None in this run.
