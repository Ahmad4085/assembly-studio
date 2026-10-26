# ADR-0007: UI architecture — MVVM, threading, snapshots, settings

| Field | Value |
|---|---|
| Status | Approved |
| Version | 1.0 |
| Date | 2026-10-26 |
| Requirements addressed | FR-035, FR-049, FR-050, FR-051, FR-056, FR-057, FR-064, FR-067, FR-077, FR-078, FR-080, FR-081, FR-082, NFR-002, NFR-004, NFR-009, NFR-018, NFR-019, NFR-032 |
| Research | `docs/02-research/editor-mvvm-testing.md` (R-MVVM), `poc-001.md` (latency and throughput) |

## Context
The UI must never block (FR-067, NFR-004), must show only real engine state (NFR-018), and must stay replaceable relative to the
engine (NFR-019). The engine runs at about 48 M instructions/s and steps in 0.091 µs [Measured, POC-001], so UI rendering is the
bottleneck, not emulation.

## Options considered
| Question | Option | Pros | Cons |
|---|---|---|---|
| MVVM support | **CommunityToolkit.Mvvm 8.4.2** | Source-generated properties and commands. MIT | One dependency |
| | Hand-written INotifyPropertyChanged/ICommand | No dependency | Boilerplate and error-prone |
| Execution thread | **Dedicated worker thread per debug session, commands via a channel** | UI never blocks. Simple ordering | Snapshots must be marshalled |
| | Execute on the UI thread in slices | No threading | Jank risk, harder to guarantee NFR-004 |
| State to UI | **Immutable snapshots published by the session** | NFR-018 by construction: the UI can't read live state | Allocation per publish (cheap at UI rates) |
| | UI polls live engine objects | — | Tearing and races. Violates NFR-018 |

## Decision
1. **MVVM** with CommunityToolkit.Mvvm (MIT). Views are XAML plus view concerns only. View models have no WPF control types. A
   manual composition root (ADR-0002). All user-facing strings live in `.resx` resources (NFR-032).
2. **`DebugSession`** (`AssemblyStudio.Debugging`) owns the machine on a dedicated worker thread:
   - **Commands:** Run, Pause, Stop, Reset, StepInto, StepOver, input events and breakpoint changes, posted through a channel.
   - **Run loop:** execute; check pause/stop/breakpoints **before each instruction**; check the instruction budget (FR-042,
     default 10,000,000); stop at termination or fault. Cancellation is checked at instruction granularity, which keeps Pause/Stop
     far within 500 ms (NFR-004).
   - **Step Over (FR-050):** a temporary breakpoint at the next instruction after CALL/INT, still stopping on breakpoints, errors
     and termination.
   - **Publishing:** an immutable `MachineSnapshot` (CPU state, undefined-flag mask, changed memory ranges since the last publish,
     console delta, session state, current source location). Snapshots are published **immediately** after a step or pause, and
     **at most 30 per second** while running (with a "running — not current" state, FR-057).
3. **View models** consume only snapshots, through the Dispatcher. Changed-value highlighting comes from snapshot diffs (FR-056),
   using a non-color cue (desktop-ui-quality guide).
4. **Settings:** JSON at `%APPDATA%\AssemblyStudio\settings.json` (System.Text.Json, part of .NET). On an invalid file: use
   defaults, rename the bad file to `settings.invalid-<timestamp>.json`, and notify (FR-080, FR-081). No network, no telemetry (NFR-009).
5. **Logging:** a local rolling file in `%LOCALAPPDATA%\AssemblyStudio\logs`, capped at 10 MB, never uploaded (NFR-012).
   Implementation: a small in-house logger (no extra dependency) [Hypothesis that it's sufficient; revisit if needs grow].

## Consequences
- Every UI feature that shows machine state must go through snapshots. This is reviewable and enforced by the `Core` API shape.
- The 30 Hz publish rate is a starting value, validated against NFR-004 and AC-030.

## Validation
- Headless tests of `DebugSession` (pause latency, budget, breakpoints, step-over, deterministic replay) (AC-027, AC-030, AC-032, AC-033, AC-047).
- A view-model test asserts that displayed values equal snapshot values (AC-025, AC-037).

## Changelog
- 2026-10-26 v0.1: Proposed (SDLC-006).
- 2026-10-26 v1.0: **Approved** by the user (2026-10-26 18:54 +0500: "approve architecture v0.1 and ADR-0001..0009; reference machine OK").
