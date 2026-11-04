# Test Report 2026-11-04-1

| Field | Value |
|---|---|
| Build under test | Commit `2f5558a` (TASK-116, TASK-100, TASK-102 included since report 2026-11-03-4); no code changed during this phase. Only documents changed |
| Environment | Windows 11 Pro 10.0.26200 x64; .NET SDK 10.0.403; NASM 3.02 (bundled); Release configuration; the reference machine (D-014). The user's Assembly Studio was closed throughout. Each step ran in the foreground (a background run was stopped for low memory on 2026-11-03) |
| Executed by | Claude Code (Opus 5.5) session, 2026-11-04 11:07:15 – 11:23:56 +0500 |
| Test plan | `docs/07-testing/test-plan.md` v0.18 |
| Raw results | `artifacts/test-results/2026-11-04-1/` (git-ignored): 12 TRX and 12 xUnit XML files, `build.txt`, `console.txt`, `coverage-console.txt`, `coverage-summary.md`, `bench-console.txt`; measurements also in `artifacts/perf/e2e-2026-11-04-1120.txt` |

## Commands executed
```
dotnet build AssemblyStudio.slnx -c Release                                         → 0 warnings, 0 errors
powershell -File build/test.ps1 -NoBuild -ResultsDirectory artifacts/test-results/2026-11-04-1   → 11:08–11:12, exit 0
powershell -File build/coverage.ps1 -NoBuild                                         → 11:12–11:20, exit 0
powershell -File build/bench/run-e2e.ps1                                             → 11:20–11:23, exit 0
```

## Summary
| Total | Passed | Failed | Skipped | Not run |
|---|---|---|---|---|
| 1594 | 1587 | 0 | 7 | 0 |

The runner reported step 1 `total: 1588, failed: 0, succeeded: 1584, skipped: 4` and step 2 (UI tests alone) `total: 6, failed: 0,
succeeded: 3, skipped: 3`. The 12 TRX files give the same counts, and the plan's TC counts add up to 1,594. The 7 skipped tests
are opt-in by design:
- TC-088 (4) and TC-089 (2) are run by the measurement script, which passed 7 of 7.
- The AC-043 real-key test passed at 2026-11-03 18:52. Since then no task changed the UI.

**Acceptance criteria (65):**
- **57 pass in this run**, including AC-042 (notices) and AC-061 (supply chain), for the first time (TASK-102).
- **5 pass on manual or record-based evidence:** AC-001, AC-034, AC-043, AC-044 and AC-059.
- **0 fail.**
- **3 not run:** AC-040 and AC-041 (clean machines; waived for the first release, WVR-002) and AC-062 (usability session,
  TASK-105).

Requirement-level results are in traceability v2.16.

**AC-042 evidence:**
- TC-047 reads the notices from the application's build output, which holds the same files the installer installs, as
  Help → About shows them. TC-097 checks that the notices name every inventoried component.
- The installer test (2026-11-04 11:00, 21/21) installed and removed the application. It does not check the notices file in the
  installed folder.

## Failures
None, in the suite or under coverage.

## End-to-end measurements (`build/bench/run-e2e.ps1`, 11:20–11:23, published application)
| NFR | Target | Result |
|---|---|---|
| NFR-001 editor latency | p95 ≤ 50 ms | best round p95 15.5 ms |
| NFR-002 step latency | p95 ≤ 100 ms | median 30.6, p95 41.5, max 58.3 ms |
| NFR-004 running | ≤ 100 ms | max 23.9 ms |
| NFR-004 Pause / Stop | ≤ 500 ms | 44.2 / 35.7 ms |
| NFR-004 hanging build | ≤ 100 ms | max 3.0 ms |
| NFR-005 warm start | ≤ 3 s | median 1,780 ms, max 2,853 ms |
| NFR-006 build | ≤ 2 s | max 1,192 ms |
| NFR-007 memory | ≤ 300 MB | peak 199.6 MB |

All pass.
- **NFR-005's slowest start** (2,853 ms) was the first of the 5 starts. It is within the target, but by less than in earlier runs
  (max 1,636–2,015 ms on 2026-11-03), and the median is unchanged. One run can't tell run-to-run spread from a change.
- **NFR-006's slowest build** (1,192 ms) is also higher than before (493–734 ms), with the median unchanged.
- **The cold start after a restart** (7,752 ms) is reported without a target (D-022).

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
| **Total** | **90.3%** | 6,285 of 6,957 |

The NFR-021 target is 85% for the engine and runtime: met. The coverage run completed with no failure.

## Defect re-runs
None were waiting for verification. Open: DEF-001, 002, 011, 012, 014, 015, 016 and 027 (all Low, intermittent test-run failures).
None recurred in this run.

DEF-004, 005, 007 and 008 still carry the older status "Fixed" rather than "Verified". Their tests pass in this run, so their
status needs updating, which this report records but doesn't do.

## Regressions vs previous report (2026-11-03-4)
None. No test failed. The new tests from TASK-100 (TC-096), TASK-102 (TC-097, TC-047 +1) and TASK-116 pass.

## Gap filling
None needed. Every `Done` task has executed tests (TC-001..TC-097). No test was added, deleted or skipped, and no production code
changed.

## Limitations
- **Not run:**
  - AC-040 and AC-041: clean machines, WVR-002.
  - AC-062: the usability session, TASK-105, which needs real participants.
- **The installer's license page** wasn't shown: a silent install skips it. Its text is checked by TC-097.

## Defects raised / updated
None.
