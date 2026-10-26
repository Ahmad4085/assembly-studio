# .NET / WPF engineering conventions

Approved ADRs override anything here. If an ADR is missing for a choice below, treat it as a default
to propose, not a decision.

## 1. Solution layout (default proposal)
```
AssemblyStudio.sln
Directory.Build.props          # shared settings
Directory.Packages.props       # central package versions
src/
  AssemblyStudio.App/          # WPF shell: Views, ViewModels, themes, DI composition root
  AssemblyStudio.Core/         # abstractions + shared models (no WPF)
  AssemblyStudio.Engine.I8086/ # CPU engine (no WPF, no DOS)
  AssemblyStudio.Runtime.Dos/  # memory map, loader, DOS/BIOS services (no WPF)
  AssemblyStudio.Assembler.Nasm/ # assembler process integration (no WPF)
  AssemblyStudio.Debugging/    # session control, breakpoints, history (no WPF)
tests/
  <Project>.Tests/             # one test project per production project
  fixtures/                    # .asm samples for integration tests
```
Dependency rule: `App → Debugging → Core ← Engine/Runtime/Assembler`. Only `App` references WPF.
Wire concretes in the composition root only.

## 2. Build settings
`Directory.Build.props`: `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`,
`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` (at least for non-UI projects),
`<AnalysisLevel>latest-recommended</AnalysisLevel>`, deterministic builds. WPF project:
`<UseWPF>true</UseWPF>`, Windows target framework (e.g. `net10.0-windows` once the ADR fixes the version).
Use central package management. Every package must have a recorded license.

## 3. MVVM rules
- Views: XAML + bindings. Code-behind only for pure view concerns (focus, scrolling, editor control glue).
- ViewModels: no WPF control types; testable without a dispatcher where possible. Use
  `INotifyPropertyChanged` and `ICommand` (or the MVVM toolkit chosen by ADR).
- Services injected through constructors. No service locator or static mutable state.
- Display formatting (hex/decimal, flag names) lives in view models or converters, never in the engine.

## 4. Engine ↔ UI threading
- The engine runs off the UI thread. UI never blocks on execution.
- Engine publishes immutable state snapshots or diffs. The UI marshals to the dispatcher and
  throttles updates during free-run (e.g. coalesce to the frame rate). Every step shows exact state.
- Run/Pause/Stop use `CancellationToken`s. Runaway programs hit a configurable step/time limit with
  a clear diagnostic.
- Unhandled exceptions in the engine become a session error state, never a crash of the shell.

## 5. External processes (assembler)
`ProcessStartInfo` with `UseShellExecute = false`, `ArgumentList` (no string-concatenated
arguments), explicit working directory in a per-build temp folder, timeout plus kill, capture
stdout/stderr, parse diagnostics into structured errors (file, line, column, message). Bundled
tool path resolved relative to the app. Never from `PATH` silently.

## 6. Code style
Follow .NET naming conventions. File-scoped namespaces. `async` all the way (no `.Result`/`.Wait()`
on UI paths). Small, focused types. XML docs on public abstractions in `Core`. Match the existing code.

## 7. Commands
```
dotnet --list-sdks                         # verify SDK per ADR before building
dotnet build AssemblyStudio.sln -c Debug
dotnet test  AssemblyStudio.sln -c Debug --logger "trx;LogFileName=results.trx"
dotnet publish src/AssemblyStudio.App -c Release -r win-x64   # options per packaging ADR
```
Report the exact commands run and their summarized output.
