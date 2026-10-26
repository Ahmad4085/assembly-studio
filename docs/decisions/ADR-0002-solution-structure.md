# ADR-0002: Solution structure and dependency rules

| Field | Value |
|---|---|
| Status | Approved |
| Version | 1.0 |
| Date | 2026-10-26 |
| Requirements addressed | NFR-019, NFR-020, NFR-021, NFR-016, FR-033 |
| Research | `docs/02-research/research-summary.md`, `docs/sdlc/guides/dotnet-wpf-engineering.md` |

## Context
The engine must be replaceable without a UI rewrite (NFR-019). Engine, runtime and toolchain must be testable headless
(NFR-020), and the domain layers (assembler, CPU, runtime, OS services, debugger, visualization) must stay separate
(`AGENTS.md`). A boundary that isn't enforced by the compiler erodes [Hypothesis].

## Options considered
| Option | Pros | Cons |
|---|---|---|
| **Multiple projects, compile-time modules, dependencies toward `Core` abstractions** | The compiler enforces boundaries (a WPF reference is impossible in core projects). Each layer is testable in isolation | More projects to maintain |
| One project with folders | Simplest | Boundaries are by convention only. WPF types can leak into engine code |
| Plugin architecture (runtime-loaded CPU/OS modules) | Swappable at run time | Loading, versioning and security complexity with no MVP need (only one CPU model) |

## Decision
One solution `AssemblyStudio.sln(x)` with these projects:

| Project | Target | References | Contains |
|---|---|---|---|
| `AssemblyStudio.Core` | net10.0 | — | Abstractions and immutable models: `ICpu`, `IMachine`, `IMemory`, `IInterruptRouter`, `IOsServices`, `IConsoleDevice`, `IAssembler`, `ISourceMap`, `IDisassembler`, `IDebugSession`; `CpuState`, `MachineSnapshot`, `StepResult`, `Diagnostic` |
| `AssemblyStudio.Engine.I8086` | net10.0 | Core, Iced | 8086 core (ADR-0003) |
| `AssemblyStudio.Runtime` | net10.0 | Core | Machine: 1 MiB memory with write journal, .COM loader and PSP, interrupt routing, console device |
| `AssemblyStudio.Os.Dos` | net10.0 | Core | DOS service subset (ADR-0004) |
| `AssemblyStudio.Toolchain.Nasm` | net10.0 | Core | NASM runner, diagnostics, listing/map parsing, source map, CPU-directive check (ADR-0005) |
| `AssemblyStudio.Debugging` | net10.0 | Core | Debug session: worker thread, run/pause/step, breakpoints, budget, history, snapshot publication |
| `AssemblyStudio.Content` | net10.0 | Core | Samples, tutorials, instruction reference as embedded resources (FR-068–FR-072) |
| `AssemblyStudio.App` | net10.0-windows | all of the above, AvalonEdit, CommunityToolkit.Mvvm | WPF shell, views, view models, editor integration, settings, **composition root** |
| `tests/<Project>.Tests` | as project | project under test, xunit.v3 | One test project per production project |
| `tests/AssemblyStudio.App.UiTests` | net10.0-windows | FlaUI.UIA3 | UI smoke tests (AC-043/044) |

**Rules:**
- Only `App` references WPF or AvalonEdit.
- Only the composition root in `App` references concrete engine, runtime, OS and toolchain types. Everything else depends on `Core`.
- No project references `App`.
- `Debugging` drives `IMachine` and never references `Engine.I8086` directly.
- Shared build settings in `Directory.Build.props`: nullable, warnings as errors, `AnalysisLevel` latest-recommended, deterministic. Central package versions in `Directory.Packages.props`.
- Wiring: a manual composition root (no DI container package), so no extra dependency is needed.

## Consequences
- A new CPU model is a new `Engine.*` project implementing `ICpu` (extensibility, §9 of the architecture). Note that `Core`'s 16-bit
  register model must be generalized before 32/64-bit support [Hypothesis, see the architecture risks].
- The CA1051/CA1707 handling found in POC-001 carries over: test projects disable CA1707. Product code avoids public fields
  (the hot path uses `ref`/`Span` internals instead).

## Validation
- An architecture test (or build check) asserts that no non-App assembly references `PresentationFramework`/`WindowsBase`, and
  that the reference graph matches the table (implementation task in plan M0).

## Changelog
- 2026-10-26 v0.1: Proposed (SDLC-006).
- 2026-10-26 v1.0: **Approved** by the user (2026-10-26 18:54 +0500: "approve architecture v0.1 and ADR-0001..0009; reference machine OK").
