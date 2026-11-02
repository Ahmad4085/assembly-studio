# Performance Report 2026-11-02: end-to-end measurements (TASK-086)

| Field | Value |
|---|---|
| Task | TASK-086 (NFR-001, NFR-002, NFR-004, NFR-005, NFR-006, NFR-007; AC-030 UI part, AC-056) |
| Date | 2026-11-02, final run 15:02–15:05 +0500 |
| Machine | The reference machine (D-014), the development machine: Intel Core i5-8265U 1.60 GHz, 15.9 GB, Windows 11 Pro 10.0.26200. Not freshly rebooted; Rider and the usual desktop programs were open, Assembly Studio was not |
| Build | Release, .NET SDK 10.0.403, NASM 3.02 (`tools/nasm`) |
| Script | `build/bench/run-e2e.ps1` → `artifacts/perf/e2e-2026-11-02-1502.txt` (git-ignored; the lines are copied below) |
| Status | Final for TASK-086 |

## 1. Results

All targets are met in the final Release run.

| NFR | Target | Measured (final run) | Result |
|---|---|---|---|
| NFR-001 editor latency | keystroke to render ≤ 50 ms p95, 5,000-line file | p95 per round 24.3 / 17.8 / 13.0 ms; best round median 8.4, max 16.2 ms | PASS |
| NFR-002 step latency | step to every visible panel updated ≤ 100 ms p95 | 200 steps: median 26.8, **p95 36.1**, max 58.4 ms | PASS (after DEF-018) |
| NFR-004 running | UI thread never blocked > 100 ms | endless loop printing without line breaks, 81 samples in 5 s: median 1.1, p95 8.1, **max 30.3 ms** | PASS (after DEF-019) |
| NFR-004 / AC-030 Pause, Stop | ≤ 500 ms | Pause to paused and rendered: 5×, max **26.6 ms**; Stop: **22.7 ms** | PASS |
| NFR-004 building | UI thread never blocked > 100 ms | assembler that never returns, 49 samples in 3 s: **max 2.6 ms**; Cancel build 16.6 ms | PASS |
| NFR-005 startup | ≤ 3 s to the editable main window | 5 launches: median **2,344 ms**, max 2,464 ms | PASS for warm starts (see §3) |
| NFR-006 build turnaround | ≤ 2 s, 1,000 lines, first launch excluded | 5 rebuilds: median 488, **max 522 ms** | PASS |
| NFR-007 memory | working set ≤ 300 MB, 1,000-line file, active session | **peak 193.8 MB** (private 105 MB) | PASS |

NFR-003 (free-run speed) is not part of TASK-086: it is measured by `EngineThroughputTests` (TC-008) and stays as recorded in
the test report.

## 2. How each number is measured

In-process measurements (`App.Tests.Performance.EndToEndPerformanceTests`, plus the existing `EditorLatencyTests`) use the
real main window, the real NASM and the real debug session on an STA thread. Process measurements
(`App.UiTests.PerformanceTests`) start the real `AssemblyStudio.App.exe` with temporary settings, recovery and log paths. All
of them run only from the script (`ASSEMBLY_STUDIO_BENCH=1`) and skip in normal test runs, because wall-clock numbers depend
on machine load. Nothing presses keys (D-018).

| Measurement | Method |
|---|---|
| NFR-002 | A 300-instruction program; 200 × Step Into. Timed from the command to `Paused` with the new instruction count, then layout and a dispatcher round at `Loaded` priority (below `Render`), so every panel (registers, flags, memory, stack, strip, history, editor arrow) is laid out and rendered |
| NFR-004 running | A background thread posts an `Input`-priority callback every 50 ms and records how late it runs while the UI thread pumps normally. Input priority is where keyboard and mouse input runs |
| NFR-004 Pause/Stop | Timed from the command to the state change plus render, 5 Pause/Run cycles and one Stop |
| NFR-004 building | A test assembler that never finishes (`BuildRunTests.WaitingAssembler`), then the same sampler, then Cancel |
| NFR-005 | 5 launches with `--smoke-report`: process start to the first rendered frame of the main window |
| NFR-006 | A 1,000-line program; a warm-up build (the first NASM launch is excluded, as NFR-006 says), then 5 rebuilds, each after a real edit, timed until the result is shown |
| NFR-007 | A 1,000-line file saved with a UTF-8 BOM, opened in the app; Build, 50 × Step Into, Run to the instruction budget (history full), via UI Automation; the working set is sampled at each stage |

## 3. Limits of these numbers
- **NFR-005 says "cold start".** These are warm starts (the .NET runtime and the app's files are in the OS cache). A first
  start after a reboot was not measured; the earlier TASK-060 note recorded 4,854 ms for a first launch right after a build.
  The headroom is 0.5 s. A post-reboot measurement stays open for QA (TASK-105 / the release check).
- One machine, one run each. Earlier runs the same day gave NFR-005 median 2,702 ms (max 2,892 ms) and NFR-006 max 914 ms
  under more load (Rider building, Assembly Studio open): the margins shrink but held.
- NFR-001 was also measured in TASK-064; this run repeats it on the final build.

## 4. Defects found and fixed by this task

| Defect | Found by | Before | After |
|---|---|---|---|
| DEF-017 (High): a source file saved with a UTF-8 BOM didn't build (NASM: "instruction expected" on line 1) | NFR-007 run (the test file was written with a BOM) | build failed | builds; `NasmAssembler` assembles a BOM-less temporary copy and reports messages against the real file. Regression test `BuildDiagnosticsTests.DEF_017_…` (fails on the old code) |
| DEF-018 (Medium): step latency | NFR-002 | median 176, p95 329 ms | median 26.8, p95 36.1 ms. The memory and stack panels update their rows in place instead of rebuilding 16 rows and 256 cells per step |
| DEF-019 (High): output without line breaks starved input | NFR-004 running | 3 of ~100 input samples ran in 5 s; Pause up to 2.5 s | max 30.3 ms; Pause max 26.6 ms. The console wraps at column 80 as on the DOS screen (D-019), so no line grows without limit |

The AC-045 console flood test asserted that the text ends with `AAAA`, which no longer holds when Stop lands in the first
3 columns of a wrapped line (D-019). It failed once in a full run for that reason; the assertion now checks that each line is
at most 80 columns and that the output without line breaks ends with `AAAA` (4 of 4 class runs passed after the change).

## 5. Full test suite

| Run | Time (+0500) | Result |
|---|---|---|
| 1 | time not recorded (after the measurement) | 1,503 passed, 2 failed, 7 skipped. AC-045 (failure message not saved; it passed 6 of 6 alone, and the end-of-text assertion above is the identified cause, fixed); AC-015 Settings NASM version check (DEF-014 recurrence, passed alone) |
| 2 | time not recorded | 1,505 passed, 0 failed, 7 skipped |
| 3 | time not recorded | 1,504 passed, 1 failed, 7 skipped: the View menu smoke test (DEF-016 recurrence; the submenu reported closed) |
| 4 (after the AC-045 test fix) | 15:24–15:30 | 1,505 passed, 0 failed, 7 skipped |
| 5 (after the AC-045 test fix) | 15:30–15:36 | 1,505 passed, 0 failed, 7 skipped |

The 7 skips are the opt-in measurement and real-key UI tests (D-018), by design.

## 6. Raw output (`artifacts/perf/e2e-2026-11-02-1502.txt`)

```text
Assembly Studio end-to-end measurements, 2026-11-02 15:03 +05:00
Machine: Intel(R) Core(TM) i5-8265U CPU @ 1.60GHz, 15.9 GB, Microsoft Windows 11 Pro
Keystroke to layout in a 5,000-line file, p95 per round: 24.26 ms, 17.84 ms, 12.95 ms; best round median 8.40 ms, p95 12.95 ms, max 16.16 ms
BENCH NFR-006 1,000-line build to result shown: n=5 median 488.0 ms, p95 522.2 ms, max 522.2 ms; target 2000 ms
BENCH NFR-004 UI thread delay while the assembler hangs: n=49 median 1.4 ms, p95 2.3 ms, max 2.6 ms; target 100 ms
BENCH NFR-004 Cancel build to cancelled: n=1 median 16.6 ms, p95 16.6 ms, max 16.6 ms; target 500 ms
BENCH NFR-002 step to all panels rendered: n=200 median 26.8 ms, p95 36.1 ms, max 58.4 ms; target 100 ms
BENCH NFR-004 UI thread delay while running (endless loop with output): n=81 median 1.1 ms, p95 8.1 ms, max 30.3 ms; target 100 ms
BENCH NFR-004 Pause to paused and rendered: n=5 median 16.4 ms, p95 26.6 ms, max 26.6 ms; target 500 ms
BENCH NFR-004 Stop to stopped and rendered: n=1 median 22.7 ms, p95 22.7 ms, max 22.7 ms; target 500 ms
BENCH NFR-007 file open: working set 140.6 MB, private 71.9 MB
BENCH NFR-007 built and loaded: working set 161.1 MB, private 87.2 MB
BENCH NFR-007 after 50 steps: working set 184.4 MB, private 103.2 MB
BENCH NFR-007 running / at the budget: working set 188.3 MB, private 105.1 MB
BENCH NFR-007 paused at the budget, history full: working set 183.8 MB, private 98.5 MB
BENCH NFR-007 peak working set 193.8 MB; target 300 MB
BENCH NFR-005 process start to rendered main window (warm starts, not after a reboot): n=5 median 2344 ms, max 2464 ms, first 2289 ms; target 3000 ms
```
All 7 measurement tests passed in that run.
