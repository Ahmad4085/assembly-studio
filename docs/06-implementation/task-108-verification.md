# TASK-108 Verification: startup time (NFR-005, DEF-021)

| Field | Value |
|---|---|
| Task | TASK-108 (NFR-005, AC-056; DEF-021; D-021) |
| Dates | 2026-11-02 21:10 – 2026-11-03 10:16 +0500 (the PC slept from 21:44 to 10:06) |
| Machine | The reference machine (D-014): Intel Core i5-8265U, 15.9 GB, Windows 11 Pro 10.0.26200. Rider, Chrome and a VPN client running on 2026-11-02; a quieter machine on 2026-11-03 |
| Measured build | Self-contained win-x64 publish as the installer makes it (`build/bench/run-e2e.ps1` publishes it to `artifacts/bench/app`) |
| Status | Automated part done; **the cold start after a reboot is run by the user** (`build/bench/cold-start.ps1`) |

## 1. Measure first: where the startup time went
The application now writes one startup timeline line to its log (`StartupTimeline`): each step's time and its JIT time.
Before any change (Release build, 2026-11-02, 5 launches):

| Step | Time | of which JIT |
|---|---|---|
| runtime and WPF (before `OnStartup`) | 580–650 ms | ~100–150 ms |
| log, settings, themes, workspace, recovery, documents | ~100 ms | |
| content catalog (Learn) | 115–155 ms | ~90 ms |
| build and run, shell | ~40 ms | |
| **window** (building `MainWindow`) | **1,030–1,940 ms** | 380–630 ms |
| **show** (first layout) | **660–890 ms** | 240–450 ms |
| first frame | 110–180 ms | |

Inside the window step, timed by temporary probes (removed): the first AvalonEdit `TextEditor` 340–460 ms (250–410 ms
even with ReadyToRun: its assembly, types and theme styles), the NASM highlighting definition 85–120 ms, the first menu
(the system control theme) 70–120 ms; the rest of the window XAML 550–1,150 ms.

## 2. Changes
| Change | Why (measured) |
|---|---|
| `PublishReadyToRun` for the published application (ADR-0009 amendment, D-021; crossgen2 10.0.14, MIT) | Self-contained publish, 4 warm launches each, alternating: median ≈ 2,830 ms plain vs ≈ 2,630 ms ReadyToRun; JIT ≈ 950 vs ≈ 530 ms; the first launch after publishing 18.8 s vs 8.4 s. Published size 176.7 → 180.6 MB |
| Editor warm-up thread at the start of `OnStartup` (`App.StartEditorWarmUp`): loads the NASM highlighting definition (kept) and lays out a throwaway AvalonEdit editor and five standard controls (dropped with their thread) | The editor's and the control theme's first use moves off the UI thread. Published builds, 6 launches each, alternating: median ≈ 3,620 ms without, ≈ 2,840 ms with the editor warm-up, ≈ 2,610 ms with the controls added (same busy machine) |
| The content catalog loads on a worker thread while settings, themes and the workspace load | It cost 115–270 ms on the UI thread; it still waits ~95–130 ms at the point where the Learn sidebar is built |
| `StartupTimeline` in the log; the NFR-005 test reports each launch's timeline and measures the published application (`ASSEMBLY_STUDIO_APP_EXE`, set by `run-e2e.ps1`); its "first" value is now the first launch (it was the smallest after sorting) | So a regression shows where it comes from |

A failure in the warm-up is caught and logged; the first editor is then simply built without it.

## 3. Results
`build/bench/run-e2e.ps1`, 2026-11-03 10:14–10:16 (published application, warm starts): **NFR-005 median 1,915 ms, max
2,237 ms** (5 launches: 2,237, 1,903, 1,815, 1,915, 1,978 ms); the window step 500–794 ms, JIT 433–532 ms in all. Every other
measurement passed (NFR-001 best-round p95 11.8 ms, NFR-002 p95 35.8 ms, NFR-004 max 44.5 ms, Pause ≤ 32.2 ms, NFR-006 max
506 ms, NFR-007 peak 203.7 MB). Raw: `artifacts/perf/e2e-2026-11-03-1014.txt`.

The machine was quieter on 2026-11-03 than when DEF-021 was found (2026-11-02 20:23, median 3,005 ms on the unpublished
build). For comparison on a quiet machine: the unpublished build before these changes gave a median of 2,344 ms on 2026-11-02
at 15:02. Within one session, alternating launches, the changes saved about 0.7–1.0 s.

## 4. Not yet measured: the cold start (user action)
NFR-005 is about a cold start. The first launch of freshly published files was 8.4–12.6 s with ReadyToRun on 2026-11-02
(new files are scanned and read cold), so a true cold start must be measured, not assumed. To run it:
1. Run `powershell -ExecutionPolicy Bypass -File build/bench/run-e2e.ps1` once, if `artifacts/bench/app` doesn't exist.
2. Restart Windows. Don't open other programs.
3. Run `powershell -ExecutionPolicy Bypass -File build/bench/cold-start.ps1` once. It prints the result and saves it in
   `artifacts/perf/cold-start-<timestamp>.txt`.

## 5. Tests
- `App.Tests.Logging.StartupTimelineTests` (2): steps from the previous mark, the first from the process start; the log line.
- Full solution through `build/test.ps1`: 1553 passed + 7 skipped (opt-in), 0 failed, in run a (2026-11-02 21:38–21:42), run
  b (21:42 – 2026-11-03 10:10, paused by the PC's sleep from 21:44 to 10:06, so not counted as a clean run) and run c
  (2026-11-03 10:10–10:14).
