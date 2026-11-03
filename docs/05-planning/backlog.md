# Implementation Backlog

| Field | Value |
|---|---|
| Document | Implementation backlog (milestones, tasks, risk register) |
| Status | Approved |
| Version | 1.0 |
| Last updated | 2026-10-26 |
| Depends on | requirements set v2.0, `architecture.md` v1.1, ADR-0001..0009 (ADR-0007 v1.1), `ui-spec.md` v1.0, `design-tokens.md` v1.0, D-001..D-015 |

**Conventions.**
- Task IDs are numbered in blocks per milestone (M0 = 001–009, M1 = 010–029, M2 = 030–049, M3 = 050–059, M4 = 060–079,
  M5 = 080–089, M6 = 090–099, M7 = 100–109), so an ID shows its milestone. IDs are never reused.
- Every task follows `docs/templates/implementation-task.md`, and the standard **Definition of Done** (§4) applies to all of them.
- **Test requirements** name the acceptance criteria they prove. Test cases get `TC-###` IDs in the test plan (`/assembly-test`).
- All 62 tasks were **approved** by the user on 2026-10-26 19:17 +0500 (whole backlog). Only `Approved` tasks can be run with `/assembly-implement <TASK-ID>`.
- Tasks marked **User action** are done by the user. The agent prepares and verifies.
- POC code (`poc/`) is a reference only. Product code is written fresh to product standards.

## 1. Milestones
| ID | Milestone | User-visible outcome | Exit criteria | Tasks | Status |
|---|---|---|---|---|---|
| M0 | Foundation | Solution skeleton builds and tests green with one command. Oracle data, NASM and coverage tooling in place. Test VMs available | Every M0 task Done. `dotnet build` and `dotnet test --solution` green. Architecture rule test active | TASK-001–TASK-006 (6) | Approved |
| M1 | 8086 engine (headless) | The complete documented 8086 instruction set runs headless and matches real-hardware data | SingleStepTests 8086 = 100% for every implemented opcode. Every opcode byte implemented or explicitly unsupported. Benchmarks recorded on the reference machine | TASK-010–TASK-020 (11) | Approved |
| M2 | Runtime, DOS and toolchain | A NASM source assembles, loads as .COM and runs with text I/O, all headless | Program-level tests pass for AC-021, 022, 023, 029, 031 and 064. POC-002 fixtures pass as regression tests | TASK-030–TASK-044 (12) | Approved |
| M3 | Debug session (headless) | Run, pause, step, breakpoints, budget, snapshots and replay work without UI | Session tests for AC-027, 030, 032, 033, 045 and 047 pass. 30-minute soak with 0 crashes | TASK-050–TASK-054 (5) | Approved |
| M4 | App shell and editor | A learner can open, edit, save and assemble files in a themed, accessible WPF window | Editor and document ACs pass (AC-002…012, 039, 046, 050). Theme contrast test green. Accessibility findings recorded | TASK-060–TASK-070 (11) | Approved |
| M5 | Debugger UI and visualization | Step through a program and see registers, flags, memory, stack, console and messages update truthfully | AC-024/025/026/043/044/048/049/052 pass. End-to-end NFR-002/004 measured on the reference machine | TASK-080–TASK-086 (7) | Approved |
| M6 | Learning content | Built-in samples, tutorials, reference and help work offline | AC-035, 036, 037 and 051 pass. Every sample verified automatically | TASK-090–TASK-093 (4) | Approved |
| M7 | Packaging and release readiness | An offline per-user installer passes clean-VM install on both supported OS versions | AC-040/041/042/034 pass on the clean VMs. Notices and SBOM complete. Usability session done | TASK-100–TASK-105 (6) | Approved |

## 2. Task index
| ID | Title | Milestone | Depends on | Risk | Requirements | Status |
|---|---|---|---|---|---|---|
| TASK-001 | Solution skeleton and build settings | M0 | — | L | NFR-019, NFR-020, NFR-021 | Done |
| TASK-002 | Architecture rule test | M0 | TASK-001 | L | NFR-019 | Done |
| TASK-003 | SingleStepTests oracle data fetch | M0 | TASK-001 | L | NFR-017 | Done |
| TASK-004 | Coverage tooling (coverlet with MTP) | M0 | TASK-001 | M | NFR-021 | Done |
| TASK-005 | Bundled NASM tooling layout | M0 | TASK-001 | L | FR-023, NFR-030, NFR-031 | Done |
| TASK-006 | Provide clean test VMs (user action) **(User action)** | M0 | — | M | NFR-025 | Approved |
| TASK-010 | Core abstractions v1 | M1 | TASK-001 | L | NFR-019, FR-033, NFR-018 | Done |
| TASK-011 | Engine skeleton: decode, ModR/M, memory access | M1 | TASK-010, TASK-003 | M | FR-033, FR-036, FR-037 | Done |
| TASK-012 | Data transfer instructions | M1 | TASK-011 | M | FR-036, NFR-017 | Done |
| TASK-013 | Arithmetic instructions | M1 | TASK-011 | M | FR-036, FR-048, NFR-017 | Done |
| TASK-014 | Logic, shift and rotate instructions | M1 | TASK-011 | M | FR-036, FR-048, NFR-017 | Done |
| TASK-015 | Multiply, divide, BCD and conversions | M1 | TASK-011 | M | FR-036, FR-038, FR-048, NFR-017 | Done |
| TASK-016 | Control transfer and interrupts | M1 | TASK-011 | M | FR-036, FR-038, NFR-017 | Done |
| TASK-017 | String instructions and REP prefixes | M1 | TASK-011 | M | FR-036, NFR-017 | Done |
| TASK-018 | Flag ops, misc and the full opcode table | M1 | TASK-012, TASK-013, TASK-014, TASK-015, TASK-016, TASK-017 | L | FR-036, FR-037, FR-045, NFR-017 | Done |
| TASK-019 | Disassembler adapter (Iced) | M1 | TASK-010 | M | FR-053, FR-058 | Done |
| TASK-020 | Engine conformance and performance gate | M1 | TASK-018, TASK-019 | L | NFR-002, NFR-003, NFR-016, NFR-017, FR-036 | Done |
| TASK-030 | Runtime memory, journal and machine | M2 | TASK-010, TASK-011 | L | FR-033, FR-056, NFR-015 | Done |
| TASK-031 | .COM loader and PSP | M2 | TASK-030 | L | FR-034 | Done |
| TASK-032 | Interrupt router and runtime error policy | M2 | TASK-030, TASK-016 | M | FR-038, FR-040, NFR-013 | Done |
| TASK-033 | Console device | M2 | TASK-030 | L | FR-063, FR-064, NFR-015, NFR-016 | Done |
| TASK-034 | DOS output and termination services | M2 | TASK-031, TASK-032, TASK-033 | M | FR-039, FR-041, FR-040 | Done |
| TASK-035 | DOS character input services | M2 | TASK-034 | M | FR-039, FR-064 | Done |
| TASK-036 | DOS buffered input and Ctrl-C | M2 | TASK-035 | M | FR-039, FR-064 | Done |
| TASK-040 | NASM runner | M2 | TASK-005, TASK-010 | M | FR-023, FR-024, FR-025, FR-026, FR-030, FR-095, NFR-014, NFR-006 | Done |
| TASK-041 | Diagnostics, message catalog, MASM hint, size limit | M2 | TASK-040 | L | FR-021, FR-029, FR-030, FR-031, FR-066, NFR-024 | Done |
| TASK-042 | CPU-directive guard | M2 | TASK-040 | L | FR-027 | Done |
| TASK-043 | Listing/map parsing and source map | M2 | TASK-040 | H | FR-028, FR-052, FR-053 | Done |
| TASK-044 | Program-level test harness | M2 | TASK-036, TASK-043 | M | FR-039, FR-041, NFR-016, NFR-020 | Done |
| TASK-050 | Debug session worker and commands | M3 | TASK-030, TASK-032 | M | FR-035, FR-042, FR-049, FR-051, FR-067, NFR-004, NFR-015 | Done |
| TASK-051 | Snapshots (ADR-0007 v1.1) | M3 | TASK-050, TASK-019, TASK-043 | M | FR-046, FR-048, FR-053, FR-054, FR-055, FR-056, FR-057, FR-058, NFR-018, NFR-002 | Done |
| TASK-052 | Breakpoints and Step Over | M3 | TASK-051 | M | FR-050, FR-052 | Done |
| TASK-053 | Input routing, Ctrl-C delivery and replay | M3 | TASK-050, TASK-036 | L | FR-064, NFR-016 | Done |
| TASK-054 | Fault containment, fuzz and soak | M3 | TASK-050 | M | NFR-010, FR-096, NFR-013, NFR-015 | Done |
| TASK-060 | WPF shell and layout | M4 | TASK-001 | M | FR-091, FR-092, NFR-026, NFR-005 | Done |
| TASK-061 | Themes, tokens and contrast test | M4 | TASK-060 | M | NFR-022, FR-094, NFR-026 | Done |
| TASK-062 | Document service and file workflow | M4 | TASK-060 | M | FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-007, FR-008, FR-019, FR-020, NFR-029 | Done |
| TASK-063 | Crash recovery | M4 | TASK-062 | L | NFR-011 | Done |
| TASK-064 | Editor integration (AvalonEdit) | M4 | TASK-061 | H | FR-011, FR-012, FR-013, FR-014, FR-022, NFR-001 | Done |
| TASK-065 | Find, replace and go to line | M4 | TASK-064 | L | FR-015, FR-016, FR-017 | Done |
| TASK-066 | Editor diagnostics and gutter | M4 | TASK-064, TASK-041 | M | FR-018, FR-052, FR-053 | Done |
| TASK-067 | Settings service and dialog | M4 | TASK-060, TASK-040 | M | FR-075, FR-077, FR-078, FR-079, FR-080, FR-081, FR-082, NFR-009 | Done |
| TASK-068 | About/Diagnostics, release notes, notices, logging | M4 | TASK-060 | L | FR-076, FR-087, FR-090, NFR-012 | Done |
| TASK-069 | Editor accessibility verification | M4 | TASK-064 | H | FR-093, NFR-022 | Done |
| TASK-070 | Fonts and icons on Windows 10 LTSC | M4 | TASK-061, TASK-006 | L | NFR-025 | Approved |
| TASK-080 | Build/run commands, command bar and status | M5 | TASK-050, TASK-066, TASK-067, TASK-042 | M | FR-028, FR-035, FR-049, FR-050, FR-051, FR-067, FR-091 | Done |
| TASK-081 | Registers and flags panels | M5 | TASK-051, TASK-061 | M | FR-046, FR-047, FR-048, FR-056, FR-057, NFR-018 | Done |
| TASK-082 | Memory and stack panels | M5 | TASK-051, TASK-061 | M | FR-054, FR-055, FR-056, NFR-018 | Done |
| TASK-083 | Instruction strip, runtime messages, history, breakpoints panels | M5 | TASK-051, TASK-052, TASK-061 | M | FR-037, FR-038, FR-040, FR-041, FR-042, FR-053, FR-058, FR-065, FR-066, FR-052 | Done |
| TASK-084 | Console panel | M5 | TASK-053, TASK-061 | M | FR-063, FR-064, FR-096 | Done |
| TASK-085 | Announcements, keyboard-only flow and UI smoke tests | M5 | TASK-080, TASK-081, TASK-082, TASK-083, TASK-084, TASK-069 | M | FR-091, FR-092, FR-093, NFR-022 | Done |
| TASK-086 | End-to-end performance check | M5 | TASK-085 | L | NFR-002, NFR-004, NFR-005, NFR-007, NFR-001, NFR-006 | Done |
| TASK-087 | Test reliability: wall-clock tests and placeholder tests | M5 | — | L | NFR-002, NFR-003, NFR-021 | Done |
| TASK-090 | Content catalog and Learn sidebar | M6 | TASK-062 | M | FR-069, FR-071, NFR-023 | Done |
| TASK-091 | Sample programs with automated verification | M6 | TASK-044, TASK-090 | L | FR-068 | Done |
| TASK-092 | Tutorials and reference | M6 | TASK-090, TASK-020 | M | FR-070, FR-071, FR-072 | Done |
| TASK-093 | Help: scope, limitations, NASM vs MASM, shortcuts | M6 | TASK-090, TASK-018, TASK-043 | L | FR-032, FR-045, NFR-033 | Done |
| TASK-100 | Product license decision (user action) **(User action)** | M7 | — | M | NFR-030 | Approved |
| TASK-101 | Self-contained publish and Inno Setup installer | M7 | TASK-005, TASK-068 | M | FR-083, FR-084, FR-085, FR-086, FR-087, FR-088, NFR-027, NFR-028 | Done |
| TASK-102 | Third-party notices and SBOM | M7 | TASK-101, TASK-100 | L | FR-090, NFR-030, NFR-031 | Approved |
| TASK-103 | Clean-VM install, offline and compatibility verification | M7 | TASK-101, TASK-006 | M | NFR-008, NFR-025, NFR-027, FR-083, FR-085, FR-088 | Approved |
| TASK-104 | Localizability and message review | M7 | TASK-085 | L | NFR-032, NFR-024 | Approved |
| TASK-105 | Beginner usability session (user-run) **(User action)** | M7 | TASK-091, TASK-092, TASK-086 | M | NFR-023 | Approved |
| TASK-106 | Variables panel | M5 | TASK-082 | M | FR-097 | Done |
| TASK-107 | UI test robustness | M5 | TASK-085 | L | FR-091, FR-092, NFR-021 | Done |
| TASK-108 | Startup time | M7 | TASK-086 | M | NFR-005 | Done |
| TASK-109 | Find bar test without a simulated Esc | M4 | TASK-065 | L | FR-015, FR-016, FR-017 | Done |
| TASK-110 | Test script options | M5 | TASK-107 | L | NFR-021 | Done |
| TASK-111 | Missing memory windows and repeated announcements | M5 | TASK-106 | M | FR-054, FR-097, FR-093, NFR-018 | Done |

## 3. Task details

### TASK-001 Solution skeleton and build settings

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M0 |
| Requirements | NFR-019, NFR-020, NFR-021 |
| Decisions / design | ADR-0001, ADR-0002, ADR-0008 |
| Depends on | — |
| Risk | L |

**Goal:** Create the product solution exactly as ADR-0002 describes, with shared build settings and MTP test mode.

**Scope (in):**
- `AssemblyStudio.slnx` with all src projects (empty) and one test project per production project
- `Directory.Build.props` (nullable, warnings as errors, latest-recommended analyzers, deterministic), `Directory.Packages.props`
- Root `global.json` pinning SDK 10.0.403 with `test.runner = Microsoft.Testing.Platform`. `.editorconfig`
- One trivial passing test per test project

**Out of scope:**
- Any feature code

**Expected files / projects touched:** `AssemblyStudio.slnx`, `src/*/`, `tests/*/`, `global.json`, `Directory.*.props`

**Test requirements:**
- `dotnet build -c Release` → 0 warnings
- `dotnet test --solution AssemblyStudio.slnx` → all trivial tests pass

**Definition of Done:** standard (§4).

### TASK-002 Architecture rule test

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M0 |
| Requirements | NFR-019 |
| Decisions / design | ADR-0002 |
| Depends on | TASK-001 |
| Risk | L |

**Goal:** Make the dependency rules of ADR-0002 a failing test, not a convention.

**Scope (in):**
- Test that loads every product assembly and asserts allowed references (graph from ADR-0002)
- Assert no assembly except App references PresentationFramework, WindowsBase or AvalonEdit

**Out of scope:**
- Runtime behavior

**Expected files / projects touched:** `tests/AssemblyStudio.ArchitectureTests/`

**Test requirements:**
- Rule test passes on the skeleton
- Deliberately added forbidden reference makes it fail (then removed)

**Definition of Done:** standard (§4).

### TASK-003 SingleStepTests oracle data fetch

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M0 |
| Requirements | NFR-017 |
| Decisions / design | ADR-0008, ADR-0003 |
| Depends on | TASK-001 |
| Risk | L |

**Goal:** Provide the hardware oracle data for engine tests, reproducibly and outside Git.

**Scope (in):**
- `build/fetch-singlesteptests.ps1`: all 8086 v1 files + metadata, pinned commit, SHA-256 per file, SOURCE.txt (URL, commit, MIT)
- 8088 v2 set as optional secondary oracle (same script, switch)
- Test helper locating the data. Missing data fails tests with instructions
- `.gitignore` entry for the data folder

**Out of scope:**
- Engine code

**Expected files / projects touched:** `build/fetch-singlesteptests.ps1`, `tests/AssemblyStudio.Engine.I8086.Tests/Oracle/`

**Test requirements:**
- Script run twice is idempotent and hashes match
- Helper test fails with a clear message when data is absent

**Definition of Done:** standard (§4).

### TASK-004 Coverage tooling (coverlet with MTP)

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M0 |
| Requirements | NFR-021 |
| Decisions / design | ADR-0008 |
| Depends on | TASK-001 |
| Risk | M (ADR-0008 Unverified: coverlet MTP package name and xunit.v3 4.0.2 compatibility) |

**Goal:** Verify and enable line coverage under Microsoft.Testing.Platform.

**Scope (in):**
- Identify and verify the coverlet MTP package and its license
- `build/coverage.ps1` producing a report
- Record the finding in ADR-0008's validation section (minor)

**Out of scope:**
- Coverage thresholds enforcement (later, when engine exists)

**Expected files / projects touched:** `build/coverage.ps1`, `Directory.Packages.props`

**Test requirements:**
- Coverage report generated for the skeleton tests

**Definition of Done:** standard (§4).

### TASK-005 Bundled NASM tooling layout

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M0 |
| Requirements | FR-023, NFR-030, NFR-031 |
| Decisions / design | ADR-0005 |
| Depends on | TASK-001 |
| Risk | L |

**Goal:** Make the pinned NASM 3.02 available to builds and tests exactly as it will ship.

**Scope (in):**
- `build/fetch-nasm.ps1`: nasm-3.02-win64.zip from nasm.us, SHA-256 verification, extract to `tools/nasm/` (git-ignored), keep LICENSE
- Record source URL, version, hash and license in `tools/nasm/SOURCE.txt`
- Test helper resolving the bundled NASM for toolchain tests

**Out of scope:**
- Toolchain code

**Expected files / projects touched:** `build/fetch-nasm.ps1`, `tools/nasm/SOURCE.txt`

**Test requirements:**
- Fetch is idempotent and hash-verified
- `nasm -v` from `tools/nasm` reports 3.02

**Definition of Done:** standard (§4).

### TASK-006 Provide clean test VMs (user action) (User action)

| Field | Value |
|---|---|
| Status | Approved |
| Milestone | M0 |
| Requirements | NFR-025 |
| Decisions / design | D-011, ADR-0001 |
| Depends on | — |
| Risk | M (needs user environment) |

**Goal:** Make clean Windows 11 24H2 x64 and Windows 10 Enterprise LTSC 21H2 x64 machines available for verification tasks.

**Scope (in):**
- User provisions two clean VMs (snapshots before install) without .NET or NASM, and records access in `docs/06-implementation/environments.md`

**Out of scope:**
- Product work

**Expected files / projects touched:** `docs/06-implementation/environments.md`

**Test requirements:**
- Agent verifies the recorded OS versions (`winver`/`ver` output captured by the user)

**Definition of Done:** standard (§4), plus: the user confirms completion of the user action.

### TASK-010 Core abstractions v1

| Field | Value |
|---|---|
| Status | Approved |
| Milestone | M1 |
| Requirements | NFR-019, FR-033, NFR-018 |
| Decisions / design | ADR-0002, ADR-0003, ADR-0004, ADR-0007 |
| Depends on | TASK-001 |
| Risk | L |

**Goal:** Define the Core interfaces and immutable models every other component depends on.

**Scope (in):**
- `ICpu`, `CpuState` (record struct), `IMemory` (with write journal), `IInterruptRouter`, `StepResult`, `IDisassembler`, `IMachine`, `IConsoleDevice`, `IOsServices`
- `MachineSnapshot` shape including ADR-0007 v1.1 fields (types only)
- XML docs on public abstractions

**Out of scope:**
- Implementations

**Expected files / projects touched:** `src/AssemblyStudio.Core/`

**Test requirements:**
- Model tests: CpuState equality/with-expressions, snapshot immutability

**Definition of Done:** standard (§4).

### TASK-011 Engine skeleton: decode, ModR/M, memory access

| Field | Value |
|---|---|
| Status | Approved |
| Milestone | M1 |
| Requirements | FR-033, FR-036, FR-037 |
| Decisions / design | ADR-0003 |
| Depends on | TASK-010, TASK-003 |
| Risk | M |

**Goal:** Build the core execution loop, prefixes, effective-address calculation and the unsupported-opcode halt.

**Scope (in):**
- Fetch with 16-bit IP wrap, segment override and REP prefix capture, ModR/M with default segments
- 20-bit address wrap, word access wrapping within segment
- Unsupported opcode → `StepResult` halt with CS:IP, bytes and reason, state unchanged
- Interrupt interception callback contract

**Out of scope:**
- Instruction families (next tasks)

**Expected files / projects touched:** `src/AssemblyStudio.Engine.I8086/`

**Test requirements:**
- Hand tests for EA modes and wraps
- Unsupported opcode test (AC-028 engine part)

**Definition of Done:** standard (§4).

### TASK-012 Data transfer instructions

| Field | Value |
|---|---|
| Status | Approved |
| Milestone | M1 |
| Requirements | FR-036, NFR-017 |
| Decisions / design | ADR-0003 |
| Depends on | TASK-011 |
| Risk | M |

**Goal:** Implement MOV (all forms incl. segment registers and moffs), XCHG, LEA/LDS/LES, XLAT, LAHF/SAHF, PUSH/POP (reg, seg, mem), PUSHF/POPF.

**Scope (in):**
- Opcodes 88–8E, A0–A3, B0–BF, C6/C7 (/0–/7 behave as /0), 86/87, 90–97, 8D, C4/C5, D7, 9E/9F, 50–5F, 06/07/0E/16/17/1E/1F, 8F, FF/6, 9C/9D
- 8086 PUSH SP and POPF reserved-bit semantics per oracle

**Out of scope:**
- Arithmetic

**Expected files / projects touched:** `src/AssemblyStudio.Engine.I8086/`

**Test requirements:**
- SingleStepTests 100% for every listed opcode file
- Hand tests for PUSH SP and segment-register moves

**Definition of Done:** standard (§4).

### TASK-013 Arithmetic instructions

| Field | Value |
|---|---|
| Status | Approved |
| Milestone | M1 |
| Requirements | FR-036, FR-048, NFR-017 |
| Decisions / design | ADR-0003 |
| Depends on | TASK-011 |
| Risk | M |

**Goal:** Implement ADD/ADC/SUB/SBB/CMP/INC/DEC/NEG in all forms with exact flags.

**Scope (in):**
- Opcodes 00–05, 10–15, 18–1D, 28–2D, 38–3D, 40–4F, 80–83 (all /reg), F6/F7 /3, FE/FF /0 /1

**Out of scope:**
- MUL/DIV, BCD

**Expected files / projects touched:** `src/AssemblyStudio.Engine.I8086/`

**Test requirements:**
- SingleStepTests 100% for every listed file
- AC-025 example as hand test

**Definition of Done:** standard (§4).

### TASK-014 Logic, shift and rotate instructions

| Field | Value |
|---|---|
| Status | Approved |
| Milestone | M1 |
| Requirements | FR-036, FR-048, NFR-017 |
| Decisions / design | ADR-0003 |
| Depends on | TASK-011 |
| Risk | M |

**Goal:** Implement AND/OR/XOR/TEST/NOT and D0–D3 shifts/rotates with 8086 count semantics and undefined-flag masks.

**Scope (in):**
- Opcodes 08–0D, 20–25, 30–35, 84/85, A8/A9, F6/F7 /0 /2, D0–D3 (all /reg incl. undocumented /6 handling decision)
- Per-step undefined-flags mask from SingleStepTests metadata

**Out of scope:**
- Multiply/divide

**Expected files / projects touched:** `src/AssemblyStudio.Engine.I8086/`

**Test requirements:**
- SingleStepTests 100% (flags-mask applied)
- Undefined mask equals metadata mask per opcode

**Definition of Done:** standard (§4).

### TASK-015 Multiply, divide, BCD and conversions

| Field | Value |
|---|---|
| Status | Done (2026-10-28 10:25 +0500, Claude Code (Opus 5.5); analysis started by OpenCode (GLM-5.3-Flash Free)) |
| Milestone | M1 |
| Requirements | FR-036, FR-038, FR-048, NFR-017 |
| Decisions / design | ADR-0003 |
| Depends on | TASK-011 |
| Risk | M |

**Goal:** Implement MUL/IMUL/DIV/IDIV (incl. divide error → INT 0), AAA/AAS/AAM/AAD/DAA/DAS, CBW/CWD.

**Scope (in):**
- Opcodes F6/F7 /4–/7, 37, 3F, D4, D5, 27, 2F, 98, 99
- Divide error raises interrupt 0 through the router callback

**Out of scope:**
- Runtime error policy (TASK-032)

**Expected files / projects touched:** `src/AssemblyStudio.Engine.I8086/`

**Test requirements:**
- SingleStepTests 100% (flags-mask applied)
- Divide-by-zero hand test (AC-031 engine part)

**Definition of Done:** standard (§4).

### TASK-016 Control transfer and interrupts

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M1 |
| Requirements | FR-036, FR-038, NFR-017 |
| Decisions / design | ADR-0003 |
| Depends on | TASK-011 |
| Risk | M |

**Goal:** Implement jumps, calls, returns, loops and interrupt instructions with interception before vectoring.

**Scope (in):**
- 70–7F, E3, E0–E2, EB, E9, EA, E8, 9A, C2/C3, CA/CB, FF /2–/5, CC, CD, CE, CF
- IVT vectoring with FLAGS/CS/IP push and IF/TF clear

**Out of scope:**
- Router policy (TASK-032)

**Expected files / projects touched:** `src/AssemblyStudio.Engine.I8086/`

**Test requirements:**
- SingleStepTests 100%
- Interception hook tests (handled vs vectored)

**Definition of Done:** standard (§4).

### TASK-017 String instructions and REP prefixes

| Field | Value |
|---|---|
| Status | Done (2026-10-28 13:28 +0500, Antigravity (IDE, Gemini 3.8 Flash)) |
| Milestone | M1 |
| Requirements | FR-036, NFR-017 |
| Decisions / design | ADR-0003 |
| Depends on | TASK-011 |
| Risk | M |

**Goal:** Implement MOVS/CMPS/SCAS/LODS/STOS with REP/REPZ/REPNZ, DF and segment overrides.

**Scope (in):**
- A4–A7, AA–AF with F2/F3
- CX = 0 and DF = 1 behavior, per-iteration step semantics decision documented (each REP iteration as one step or whole instruction)

**Out of scope:**
- Interrupted REP resume quirks beyond the oracle

**Expected files / projects touched:** `src/AssemblyStudio.Engine.I8086/`

**Test requirements:**
- SingleStepTests 100%
- Hand tests for CX=0 and DF both directions

**Definition of Done:** standard (§4).

### TASK-018 Flag ops, misc and the full opcode table

| Field | Value |
|---|---|
| Status | Done (2026-10-28 14:06 +0500, Antigravity (IDE, Gemini 3.8 Flash)) |
| Milestone | M1 |
| Requirements | FR-036, FR-037, FR-045, NFR-017 |
| Decisions / design | ADR-0003 |
| Depends on | TASK-012, TASK-013, TASK-014, TASK-015, TASK-016, TASK-017 |
| Risk | L |

**Goal:** Finish CLC/STC/CMC/CLD/STD/CLI/STI, HLT, WAIT, LOCK, NOP, ESC, IN/OUT policy, and make every opcode byte explicitly implemented or unsupported.

**Scope (in):**
- F5, F8–FD, F4 (halted), 9B (no-op), F0 (ignored), D8–DF (unsupported: no 8087), E4–E7/EC–EF (unsupported port access in MVP)
- Undocumented and alias opcodes (60–6F, C0/C1/C8/C9, D6, 0F, F1) → unsupported
- Generated table of supported/unsupported opcodes for help (FR-045)

**Out of scope:**
- Port devices

**Expected files / projects touched:** `src/AssemblyStudio.Engine.I8086/`, `docs/06-implementation/opcode-support.md`

**Test requirements:**
- Exhaustive test over 256 opcodes × ModR/M reg groups: each implemented or unsupported
- SingleStepTests 100% for listed opcodes

**Definition of Done:** standard (§4).

### TASK-019 Disassembler adapter (Iced)

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M1 |
| Requirements | FR-053, FR-058 |
| Decisions / design | ADR-0003 |
| Depends on | TASK-010 |
| Risk | M (ADR-0003 Unverified: Iced 1.21.0 on .NET 10, INTEL8086_ONLY coverage) |

**Goal:** Provide `IDisassembler` via Iced with NASM formatting and verify the unverified claims.

**Scope (in):**
- Iced package (MIT) in Directory.Packages.props with license recorded
- 16-bit decode, NASM formatter, 8086-only tagging
- Verification test: instruction lengths equal the engine's for all oracle cases

**Out of scope:**
- UI

**Expected files / projects touched:** `src/AssemblyStudio.Engine.I8086/Disassembly/`

**Test requirements:**
- Length agreement test
- Formatting snapshot tests
- Finding recorded in ADR-0003 validation (minor)

**Definition of Done:** standard (§4).

### TASK-020 Engine conformance and performance gate

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M1 |
| Requirements | NFR-002, NFR-003, NFR-016, NFR-017, FR-036 |
| Decisions / design | ADR-0003, D-014 |
| Depends on | TASK-018, TASK-019 |
| Risk | L |

**Goal:** Prove the whole engine against the oracle and measure performance on the reference machine.

**Scope (in):**
- Full SingleStepTests 8086 run report (pass per opcode) + 8088 secondary report
- Benchmark: step+snapshot latency and throughput (Release, reference machine)
- Determinism test over a long trace

**Out of scope:**
- UI rendering cost

**Expected files / projects touched:** `tests/AssemblyStudio.Engine.I8086.Tests/`, `build/bench/`

**Test requirements:**
- AC-058 (all supported instructions pass)
- AC-056 engine part measured
- AC-047 engine part

**Definition of Done:** standard (§4).

### TASK-030 Runtime memory, journal and machine

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M2 |
| Requirements | FR-033, FR-056, NFR-015 |
| Decisions / design | ADR-0004 |
| Depends on | TASK-010, TASK-011 |
| Risk | L |

**Goal:** Implement `IMachine` with 1 MiB memory, 20-bit wrap and a per-step write journal.

**Scope (in):**
- Memory with journal (address, old value) and changed-range extraction
- Machine composition: CPU + memory + router + OS services (injected)

**Out of scope:**
- Loader

**Expected files / projects touched:** `src/AssemblyStudio.Runtime/`

**Test requirements:**
- Journal tests (changed ranges, rollback capability)
- Wrap tests

**Definition of Done:** standard (§4).

### TASK-031 .COM loader and PSP

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M2 |
| Requirements | FR-034 |
| Decisions / design | ADR-0004 |
| Depends on | TASK-030 |
| Risk | L |

**Goal:** Load a .COM image with the documented initial state.

**Scope (in):**
- PSP at 0800h with fields 00h, 02h, 05h, 0Ah–15h, 16h, 2Ch, 50h, 5Ch/6Ch, 80h
- Registers: CS=DS=ES=SS=PSP, IP=0100h, SP=FFFEh, word 0000h, AX=BX=CX=DX=SI=DI=BP=0000h

**Out of scope:**
- MZ (P2)

**Expected files / projects touched:** `src/AssemblyStudio.Runtime/Loading/`

**Test requirements:**
- AC-021 (loader part)
- PSP byte-level tests against the research table

**Definition of Done:** standard (§4).

### TASK-032 Interrupt router and runtime error policy

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M2 |
| Requirements | FR-038, FR-040, NFR-013 |
| Decisions / design | ADR-0004 |
| Depends on | TASK-030, TASK-016 |
| Risk | M |

**Goal:** Route interrupts: default vector + service → OS layer; program vector → IVT; CPU exceptions → runtime errors; unsupported → halt.

**Scope (in):**
- IVT stubs in a reserved area
- Default vs program-installed vector detection
- Halts with what/where/next for unsupported INT/AH and exceptions

**Out of scope:**
- DOS function semantics

**Expected files / projects touched:** `src/AssemblyStudio.Runtime/Interrupts/`

**Test requirements:**
- AC-029 (unsupported service, no host access)
- AC-031 (divide error)
- Program-installed vector test

**Definition of Done:** standard (§4).

### TASK-033 Console device

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M2 |
| Requirements | FR-063, FR-064, NFR-015, NFR-016 |
| Decisions / design | ADR-0004 |
| Depends on | TASK-030 |
| Risk | L |

**Goal:** Implement output buffer (1 MB cap, trim oldest), input queue with recording, column counter, BEL.

**Scope (in):**
- Output delta API for snapshots
- Input events stamped with instruction count (replay)

**Out of scope:**
- UI

**Expected files / projects touched:** `src/AssemblyStudio.Runtime/Console/`

**Test requirements:**
- Cap/trim test (AC-045 runtime part)
- Recording/replay unit test

**Definition of Done:** standard (§4).

### TASK-034 DOS output and termination services

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M2 |
| Requirements | FR-039, FR-041, FR-040 |
| Decisions / design | ADR-0004, D-012 |
| Depends on | TASK-031, TASK-032, TASK-033 |
| Risk | M |

**Goal:** Implement INT 21h 02h, 09h, 00h, 4Ch and INT 20h with DOS 3.3/4.0 semantics, plus RET-to-PSP termination.

**Scope (in):**
- AL return values (02h last char, TAB → 20h; 09h → 24h)
- TAB/CR/BS output handling
- CS=PSP requirement for INT 20h/00h
- Termination path + exit code reporting

**Out of scope:**
- Input functions

**Expected files / projects touched:** `src/AssemblyStudio.Os.Dos/`

**Test requirements:**
- Per-function tests vs research table
- AC-064 (all four paths)
- AC-022 (runtime part)

**Definition of Done:** standard (§4).

### TASK-035 DOS character input services

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M2 |
| Requirements | FR-039, FR-064 |
| Decisions / design | ADR-0004 |
| Depends on | TASK-034 |
| Risk | M |

**Goal:** Implement INT 21h 01h, 07h, 08h (extended keys) and the WaitingForInput mechanism.

**Scope (in):**
- Echo rules, extended-key 00h + scan code
- Empty queue → WaitingForInput with CS:IP on the INT, re-executed on input

**Out of scope:**
- Buffered input

**Expected files / projects touched:** `src/AssemblyStudio.Os.Dos/`

**Test requirements:**
- Per-function tests
- Wait/resume test

**Definition of Done:** standard (§4).

### TASK-036 DOS buffered input and Ctrl-C

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M2 |
| Requirements | FR-039, FR-064 |
| Decisions / design | ADR-0004, D-015 |
| Depends on | TASK-035 |
| Risk | M |

**Goal:** Implement INT 21h 0Ah (MVP editing keys) and the Ctrl-C/INT 23h protocol.

**Scope (in):**
- 0Ah: buffer format, max 0, full → BEL, BS/DEL, Enter (CR only), Esc cancel
- Ctrl-C checks per function (none in 07h), default INT 23h terminates with reason Ctrl-C, program handler IRET/RETF protocol
- ^S pause handling, ^P unsupported (documented)

**Out of scope:**
- Template keys F1–F7 (post-MVP)

**Expected files / projects touched:** `src/AssemblyStudio.Os.Dos/`

**Test requirements:**
- AC-023 (runtime part)
- Ctrl-C protocol tests

**Definition of Done:** standard (§4).

### TASK-040 NASM runner

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M2 |
| Requirements | FR-023, FR-024, FR-025, FR-026, FR-030, FR-095, NFR-014, NFR-006 |
| Decisions / design | ADR-0005 |
| Depends on | TASK-005, TASK-010 |
| Risk | M |

**Goal:** Implement `IAssembler` process handling: locate, version-check, sandboxed invocation, timeout, output cap, failure kinds.

**Scope (in):**
- Bundled vs configured path, version ≥ 3.02 warning
- ArgumentList, temp dir, working dir = source folder (untitled: temp), 10 s timeout with tree kill, 1 MiB cap
- Unsaved documents assembled from a temp copy
- Build time measured (NFR-006)

**Out of scope:**
- Parsing (TASK-041/043)

**Expected files / projects touched:** `src/AssemblyStudio.Toolchain.Nasm/`

**Test requirements:**
- AC-015, AC-016, AC-019 (toolchain part)
- Hanging/crashing stub tests (from POC-002)
- Malicious file-name argument test

**Definition of Done:** standard (§4).

### TASK-041 Diagnostics, message catalog, MASM hint, size limit

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M2 |
| Requirements | FR-021, FR-029, FR-030, FR-031, FR-066, NFR-024 |
| Decisions / design | ADR-0005 |
| Depends on | TASK-040 |
| Risk | L |

**Goal:** Parse GNU diagnostics, define stable message codes, add the MASM hint and the .COM size check.

**Scope (in):**
- Diagnostics parser (incl. info and warning class)
- Message catalog (code, what, where, next step) in resources
- MASM heuristics
- 65,280-byte limit

**Out of scope:**
- UI

**Expected files / projects touched:** `src/AssemblyStudio.Toolchain.Nasm/Diagnostics/`

**Test requirements:**
- AC-013 (toolchain part), AC-014, AC-020
- Catalog completeness test (AC-055 part)

**Definition of Done:** standard (§4).

### TASK-042 CPU-directive guard

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M2 |
| Requirements | FR-027 |
| Decisions / design | ADR-0005 |
| Depends on | TASK-040 |
| Risk | L |

**Goal:** Reject non-8086 `cpu` directives, including ones hidden in macros or includes, via `nasm -E`.

**Scope (in):**
- Preprocess scan, build error at the directive line
- Guard runs before every build

**Out of scope:**
- Runtime halts (already in engine)

**Expected files / projects touched:** `src/AssemblyStudio.Toolchain.Nasm/`

**Test requirements:**
- AC-018
- Hidden-in-macro and include tests (from POC-002)

**Definition of Done:** standard (§4).

### TASK-043 Listing/map parsing and source map

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M2 |
| Requirements | FR-028, FR-052, FR-053 |
| Decisions / design | ADR-0005 |
| Depends on | TASK-040 |
| Risk | H (ADR-0005 Unverified: <10> nesting, nested includes, %rep, sections in macros) |

**Goal:** Productize POC-002's source map and close its unverified cases.

**Scope (in):**
- `ISourceMap` from listing + map (macro → invoking line, include → file, sections)
- POC-002 fixtures + 3 verifiers as regression tests
- New fixtures: ≥10-level nesting, nested includes in subfolders, `%rep`, section switch inside a macro. Failures narrow documented scope (FR-045)

**Out of scope:**
- Debugger use

**Expected files / projects touched:** `src/AssemblyStudio.Toolchain.Nasm/SourceMapping/`, `tests/.../fixtures/`

**Test requirements:**
- All verifiers green on all fixtures, or documented limitation with a failing-case test marked as known

**Definition of Done:** standard (§4).

### TASK-044 Program-level test harness

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M2 |
| Requirements | FR-039, FR-041, NFR-016, NFR-020 |
| Decisions / design | ADR-0008 |
| Depends on | TASK-036, TASK-043 |
| Risk | M |

**Goal:** Assemble → load → run headless with scripted input and assert output, exit path and trace.

**Scope (in):**
- Harness API used by samples later
- Fixtures for hello, input echo, unsupported service, divide error, four exit paths

**Out of scope:**
- UI

**Expected files / projects touched:** `tests/AssemblyStudio.ProgramTests/`

**Test requirements:**
- AC-021, AC-022, AC-023, AC-029, AC-031, AC-064 (runtime level)
- AC-047 (replay, runtime level)

**Definition of Done:** standard (§4).

### TASK-050 Debug session worker and commands

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M3 |
| Requirements | FR-035, FR-042, FR-049, FR-051, FR-067, NFR-004, NFR-015 |
| Decisions / design | ADR-0007 |
| Depends on | TASK-030, TASK-032 |
| Risk | M |

**Goal:** Run the machine on a dedicated worker with a command channel and session states.

**Scope (in):**
- Run/Pause/Stop/Reset/StepInto, per-instruction cancellation, budget (default 10,000,000, configurable)
- Session states as in ui-spec §4

**Out of scope:**
- Breakpoints, snapshots detail

**Expected files / projects touched:** `src/AssemblyStudio.Debugging/`

**Test requirements:**
- AC-027
- AC-030 (pause ≤ 500 ms, budget pause)
- AC-054 (budget setting honored, session level)

**Definition of Done:** standard (§4).

### TASK-051 Snapshots (ADR-0007 v1.1)

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M3 |
| Requirements | FR-046, FR-048, FR-053, FR-054, FR-055, FR-056, FR-057, FR-058, NFR-018, NFR-002 |
| Decisions / design | ADR-0007, UI-005, UI-006, UI-007, UI-008, UI-009, UI-013 |
| Depends on | TASK-050, TASK-019, TASK-043 |
| Risk | M |

**Goal:** Publish immutable snapshots with CPU state, undefined mask, changes, console delta, location, current instruction, memory windows, stack window and history.

**Scope (in):**
- `WatchMemory` (≤ 4 windows × 4 KiB)
- Immediate publish on step/pause, ≤ 30/s while running with not-current state
- History ring buffer 1,000

**Out of scope:**
- View models

**Expected files / projects touched:** `src/AssemblyStudio.Debugging/Snapshots/`

**Test requirements:**
- Snapshot equals machine state test
- AC-048 (session level)
- AC-049 (session level)
- Publish-rate test

**Definition of Done:** standard (§4).

### TASK-052 Breakpoints and Step Over

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M3 |
| Requirements | FR-050, FR-052 |
| Decisions / design | ADR-0007, ADR-0005 |
| Depends on | TASK-051 |
| Risk | M |

**Goal:** Bind line breakpoints via the source map and implement Step Over with a temporary breakpoint.

**Scope (in):**
- Bound/unbound state, re-binding after rebuild
- Step Over for CALL and INT, stops early on breakpoints/errors/termination

**Out of scope:**
- Run to Cursor (P2)

**Expected files / projects touched:** `src/AssemblyStudio.Debugging/`

**Test requirements:**
- AC-032
- AC-033

**Definition of Done:** standard (§4).

### TASK-053 Input routing, Ctrl-C delivery and replay

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M3 |
| Requirements | FR-064, NFR-016 |
| Decisions / design | ADR-0007, D-015 |
| Depends on | TASK-050, TASK-036 |
| Risk | L |

**Goal:** Deliver console input and Ctrl-C to the program and replay recorded sessions identically.

**Scope (in):**
- SendInput / SendCtrlC commands
- Replay from recorded input

**Out of scope:**
- UI key handling (TASK-074)

**Expected files / projects touched:** `src/AssemblyStudio.Debugging/`

**Test requirements:**
- AC-047 (session level)
- Ctrl-C during 01h/07h behavior tests

**Definition of Done:** standard (§4).

### TASK-054 Fault containment, fuzz and soak

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M3 |
| Requirements | NFR-010, FR-096, NFR-013, NFR-015 |
| Decisions / design | ADR-0004 |
| Depends on | TASK-050 |
| Risk | M |

**Goal:** Prove that hostile or faulty programs can't crash or hang the host.

**Scope (in):**
- Engine exception → session error state + local log
- Fuzz: random images under budget
- 30-minute soak
- Output flood

**Out of scope:**
- UI

**Expected files / projects touched:** `tests/AssemblyStudio.Debugging.Tests/Robustness/`

**Test requirements:**
- 0 host crashes in fuzz + 30-min soak (NFR-010)
- AC-045 (session level)

**Definition of Done:** standard (§4).

### TASK-060 WPF shell and layout

| Field | Value |
|---|---|
| Status | Done (2026-10-28 11:31 +0500, Claude Code (Opus 5.5)) |
| Milestone | M4 |
| Requirements | FR-091, FR-092, NFR-026, NFR-005 |
| Decisions / design | ADR-0007, UI-001, UI-002, UI-016 |
| Depends on | TASK-001 |
| Risk | M |

**Goal:** Create the main window regions, splitters, focus cycling, menus skeleton, status bar and layout persistence.

**Scope (in):**
- Regions per ui-spec §2, min 1024×768, responsive collapse < 1280 px
- F6/Shift+F6, View menu panel commands
- Persisted sizes/visibility, Reset Layout

**Out of scope:**
- Panel contents

**Expected files / projects touched:** `src/AssemblyStudio.App/Shell/`

**Test requirements:**
- UI smoke test: window opens, F6 cycles regions
- Startup time measured (NFR-005)

**Definition of Done:** standard (§4).

### TASK-061 Themes, tokens and contrast test

| Field | Value |
|---|---|
| Status | Done (2026-10-30 21:17 +0500, waiver WVR-001: the AC-059 manual check is postponed to QA) |
| Milestone | M4 |
| Requirements | NFR-022, FR-094, NFR-026 |
| Decisions / design | UI-001, design tokens v1.0 |
| Depends on | TASK-060 |
| Risk | M (design-tokens Unverified: WPF text scaling) |

**Goal:** Implement Dark, Light and HighContrast dictionaries from the approved tokens, with an automated contrast test.

**Scope (in):**
- All token keys per theme; HC via SystemColors
- Follow Windows app mode; override in settings
- Automated WCAG contrast test over the dictionaries (166 pairs)
- Text scaling implementation and check at 200 %

**Out of scope:**
- Panel styling details

**Expected files / projects touched:** `src/AssemblyStudio.App/Themes/`

**Test requirements:**
- Contrast test 166/166
- AC-059 (DPI/scaling) manual check recorded

**Definition of Done:** standard (§4).

### TASK-062 Document service and file workflow

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M4 |
| Requirements | FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-007, FR-008, FR-019, FR-020, NFR-029 |
| Decisions / design | UI-003, UI-019, UI-020 |
| Depends on | TASK-060 |
| Risk | M |

**Goal:** Open, edit, save (atomic) and close documents safely, preserving encoding and line endings.

**Scope (in):**
- New (empty / .COM template), Open (dialog, drag-drop), Save/Save As atomic, Close with prompt
- Encoding detection + preservation, EOL preservation + normalize confirmation
- File error messages, recent files, welcome page

**Out of scope:**
- Editor control (TASK-064)

**Expected files / projects touched:** `src/AssemblyStudio.App/Documents/`

**Test requirements:**
- AC-001, AC-002, AC-003, AC-004, AC-005, AC-006, AC-007, AC-008, AC-012

**Definition of Done:** standard (§4).

### TASK-063 Crash recovery

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M4 |
| Requirements | NFR-011 |
| Decisions / design | UI-021 |
| Depends on | TASK-062 |
| Risk | L |

**Goal:** Autosave unsaved documents at most every 60 s and offer recovery after an unexpected exit.

**Scope (in):**
- Recovery folder, autosave timer, recovery prompt

**Out of scope:**
- Versioned history

**Expected files / projects touched:** `src/AssemblyStudio.App/Documents/Recovery/`

**Test requirements:**
- AC-050

**Definition of Done:** standard (§4).

### TASK-064 Editor integration (AvalonEdit)

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M4 |
| Requirements | FR-011, FR-012, FR-013, FR-014, FR-022, NFR-001 |
| Decisions / design | ADR-0006, UI-003 |
| Depends on | TASK-061 |
| Risk | H (ADR-0006 Unverified: AvalonEdit 6.3.1 on net10.0-windows) |

**Goal:** Integrate AvalonEdit, verify it on .NET 10, and add NASM highlighting, editing behavior and zoom.

**Scope (in):**
- Verify the net8 build on net10 first (stop and report if it fails)
- XSHD for NASM 8086 with Syntax tokens
- Indent/unindent, auto-indent, Ctrl+M focus escape, tabs
- Zoom 50–300 %
- Keystroke latency measurement (NFR-001)

**Out of scope:**
- Find/replace, diagnostics

**Expected files / projects touched:** `src/AssemblyStudio.App/Editor/`

**Test requirements:**
- AC-009, AC-010, AC-046
- NFR-001 measured (AC-056 part)

**Definition of Done:** standard (§4).

### TASK-065 Find, replace and go to line

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M4 |
| Requirements | FR-015, FR-016, FR-017 |
| Decisions / design | ADR-0006, UI-004 |
| Depends on | TASK-064 |
| Risk | L |

**Goal:** Provide the find/replace bar and Go to Line dialog.

**Scope (in):**
- Match case, whole word, next/previous, replace, replace all, Go to Line

**Out of scope:**
- Anything not listed above

**Expected files / projects touched:** `src/AssemblyStudio.App/Editor/Search/`

**Test requirements:**
- AC-011

**Definition of Done:** standard (§4).

### TASK-066 Editor diagnostics and gutter

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M4 |
| Requirements | FR-018, FR-052, FR-053 |
| Decisions / design | ADR-0006, UI-003 |
| Depends on | TASK-064, TASK-041 |
| Risk | M |

**Goal:** Show diagnostics as squiggles and gutter glyphs, plus the breakpoint margin, execution arrow and banners.

**Scope (in):**
- Squiggle renderer per severity, hover text
- Breakpoint margin (bound/unbound), execution arrow + line
- Stale-session and encoding banners

**Out of scope:**
- Debugger panels

**Expected files / projects touched:** `src/AssemblyStudio.App/Editor/Adornments/`

**Test requirements:**
- AC-013 (UI part)
- Glyph non-color cue check

**Definition of Done:** standard (§4).

### TASK-067 Settings service and dialog

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M4 |
| Requirements | FR-075, FR-077, FR-078, FR-079, FR-080, FR-081, FR-082, NFR-009 |
| Decisions / design | ADR-0007, UI-017, UI-019 |
| Depends on | TASK-060, TASK-040 |
| Risk | M |

**Goal:** Persist settings safely and provide the Settings dialog.

**Scope (in):**
- JSON settings with corrupt-file recovery + notice
- Assembler path validation, editor and execution preferences, theme, reset all
- Zero-config defaults with bundled NASM

**Out of scope:**
- Cloud sync (never)

**Expected files / projects touched:** `src/AssemblyStudio.App/Settings/`

**Test requirements:**
- AC-039, AC-053, AC-054 (UI part)
- AC-015 (UI part)

**Definition of Done:** standard (§4).

### TASK-068 About/Diagnostics, release notes, notices, logging

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M4 |
| Requirements | FR-076, FR-087, FR-090, NFR-012 |
| Decisions / design | UI-018, ADR-0007 |
| Depends on | TASK-060 |
| Risk | L |

**Goal:** Show versions, release notes and third-party notices, and add the local rolling log.

**Scope (in):**
- Copyable diagnostics text
- Release notes and notices views from installed files
- Rolling log ≤ 10 MB, no upload

**Out of scope:**
- Notice content (TASK-091)

**Expected files / projects touched:** `src/AssemblyStudio.App/About/`, `src/AssemblyStudio.App/Logging/`

**Test requirements:**
- AC-038
- AC-063

**Definition of Done:** standard (§4).

### TASK-069 Editor accessibility verification

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M4 |
| Requirements | FR-093, NFR-022 |
| Decisions / design | ADR-0006, UI-003 |
| Depends on | TASK-064 |
| Risk | H (ADR-0006 Unverified: Narrator support in AvalonEdit) |

**Goal:** Verify what Narrator reads in the editor and record the outcome; raise an issue if insufficient.

**Scope (in):**
- Narrator walkthrough script (lines, caret, selection)
- UIA names on editor chrome
- Findings in `docs/06-implementation/accessibility-findings.md`; requirement/design issue if gaps

**Out of scope:**
- Fixing AvalonEdit internals unless small

**Expected files / projects touched:** `docs/06-implementation/accessibility-findings.md`

**Test requirements:**
- Manual Narrator check recorded (AC-044 editor part)

**Definition of Done:** standard (§4).

### TASK-070 Fonts and icons on Windows 10 LTSC

| Field | Value |
|---|---|
| Status | Approved |
| Milestone | M4 |
| Requirements | NFR-025 |
| Decisions / design | UI spec §9, design tokens §4 |
| Depends on | TASK-061, TASK-006 |
| Risk | L (OQ-025) |

**Goal:** Verify Cascadia Mono and Segoe Fluent Icons availability on the supported OS versions; apply fallbacks.

**Scope (in):**
- Check on both VMs; fallback to Consolas / Segoe MDL2 Assets
- Record result, close OQ-025

**Out of scope:**
- Bundling fonts (would need a license review)

**Expected files / projects touched:** `docs/06-implementation/environments.md`

**Test requirements:**
- Screenshots/notes from both VMs recorded

**Definition of Done:** standard (§4).

### TASK-080 Build/run commands, command bar and status

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M5 |
| Requirements | FR-028, FR-035, FR-049, FR-050, FR-051, FR-067, FR-091 |
| Decisions / design | UI-002, UI-011, UI-016, ADR-0007 |
| Depends on | TASK-050, TASK-066, TASK-067, TASK-042 |
| Risk | M |

**Goal:** Wire all 33 commands with shortcuts, the build flow, Problems panel, state badge and status bar.

**Scope (in):**
- Command table per ui-spec §6 incl. access keys and tooltips
- Build → Problems, cancel build
- State badge as live region

**Out of scope:**
- Visualization panels

**Expected files / projects touched:** `src/AssemblyStudio.App/Commands/`, `src/AssemblyStudio.App/Panels/Problems/`

**Test requirements:**
- Every command reachable by keyboard (test over command table)
- AC-013 (UI flow)
- AC-017 (UI flow)

**Definition of Done:** standard (§4).

### TASK-081 Registers and flags panels

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M5 |
| Requirements | FR-046, FR-047, FR-048, FR-056, FR-057, NFR-018 |
| Decisions / design | UI-005, UI-006, ADR-0007 |
| Depends on | TASK-051, TASK-061 |
| Risk | M |

**Goal:** Show registers and flags from snapshots with formats, change marking, undefined '?' and stale state.

**Scope (in):**
- ValueCell/FlagCell components
- Formats hex/unsigned/signed/binary
- UIA names incl. changed state

**Out of scope:**
- Anything not listed above

**Expected files / projects touched:** `src/AssemblyStudio.App/Panels/Machine/`

**Test requirements:**
- AC-025, AC-052
- View-model equals snapshot test (AC-037 part)

**Definition of Done:** standard (§4).

### TASK-082 Memory and stack panels

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M5 |
| Requirements | FR-054, FR-055, FR-056, NFR-018 |
| Decisions / design | UI-008, UI-009, ADR-0007 |
| Depends on | TASK-051, TASK-061 |
| Risk | M |

**Goal:** Show memory windows and the stack window with change marking.

**Scope (in):**
- HexByteGrid with address entry and WatchMemory
- Stack view with SP marker

**Out of scope:**
- Memory editing (P2)

**Expected files / projects touched:** `src/AssemblyStudio.App/Panels/Memory/`

**Test requirements:**
- AC-026

**Definition of Done:** standard (§4).

### TASK-083 Instruction strip, runtime messages, history, breakpoints panels

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M5 |
| Requirements | FR-037, FR-038, FR-040, FR-041, FR-042, FR-053, FR-058, FR-065, FR-066, FR-052 |
| Decisions / design | UI-007, UI-012, UI-013, UI-014 |
| Depends on | TASK-051, TASK-052, TASK-061 |
| Risk | M |

**Goal:** Show the current instruction, session messages, execution history and breakpoints.

**Scope (in):**
- Unsupported/runtime error details with what/where/next
- Budget reached with Continue/Stop
- History activation selects line

**Out of scope:**
- Anything not listed above

**Expected files / projects touched:** `src/AssemblyStudio.App/Panels/Session/`

**Test requirements:**
- AC-024, AC-028, AC-029, AC-031 (UI parts)
- AC-048, AC-049

**Definition of Done:** standard (§4).

### TASK-084 Console panel

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M5 |
| Requirements | FR-063, FR-064, FR-096 |
| Decisions / design | UI-010, D-015 |
| Depends on | TASK-053, TASK-061 |
| Risk | M |

**Goal:** Show program output and accept input, with Ctrl-C routing per D-015.

**Scope (in):**
- Output rendering (CR/LF/BS/TAB, visual BEL), cap notice
- Input line and waiting state
- Ctrl+C/Ctrl+Break delivery, Ctrl+Shift+C copy

**Out of scope:**
- Anything not listed above

**Expected files / projects touched:** `src/AssemblyStudio.App/Panels/Console/`

**Test requirements:**
- AC-022, AC-023, AC-045 (UI parts)

**Definition of Done:** standard (§4).

### TASK-085 Announcements, keyboard-only flow and UI smoke tests

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M5 |
| Requirements | FR-091, FR-092, FR-093, NFR-022 |
| Decisions / design | UI-001, ADR-0008 |
| Depends on | TASK-080, TASK-081, TASK-082, TASK-083, TASK-084, TASK-069 |
| Risk | M |

**Goal:** Add live announcements and prove the keyboard-only core flow with FlaUI.

**Scope (in):**
- Per-step announcement text
- FlaUI.UIA3 smoke tests (license recorded)

**Out of scope:**
- Anything not listed above

**Expected files / projects touched:** `tests/AssemblyStudio.App.UiTests/`

**Test requirements:**
- AC-043
- AC-044 (automated part) + Narrator manual check

**Definition of Done:** standard (§4).

### TASK-086 End-to-end performance check

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M5 |
| Requirements | NFR-002, NFR-004, NFR-005, NFR-007, NFR-001, NFR-006 |
| Decisions / design | D-014 |
| Depends on | TASK-085 |
| Risk | L |

**Goal:** Measure step latency, responsiveness, startup, memory and build time end-to-end on the reference machine.

**Scope (in):**
- Scripted measurement runs, results recorded

**Out of scope:**
- Optimization beyond targets

**Expected files / projects touched:** `build/bench/`, `docs/07-testing/`

**Test requirements:**
- AC-030 (UI part)
- AC-056 complete

**Definition of Done:** standard (§4).

### TASK-087 Test reliability: wall-clock tests and placeholder tests

| Field | Value |
|---|---|
| Status | Done (2026-10-30 21:34 +0500, Claude Code (Opus 5.5)); added 2026-10-30 21:17 +0500 by the user's approval |
| Milestone | M5 |
| Requirements | NFR-002, NFR-003, NFR-021 (test infrastructure; DEF-006, DEF-009) |
| Decisions / design | ADR-0008 |
| Depends on | — |
| Risk | L |

**Goal:** Make the wall-clock performance tests reliable and make the coverage run complete, and remove the placeholder tests.

**Scope (in):**
- Wall-clock tests run outside the parallel full-suite load (non-parallel collection) and are excluded from the instrumented coverage run (a trait the coverage script filters out)
- Delete the six placeholder `SkeletonTests` (user-approved 2026-10-30)

**Out of scope:**
- Changing performance limits or product code

**Expected files / projects touched:** `tests/*`, `build/coverage.ps1`

**Test requirements:**
- DEF-006 and DEF-009 re-verified: full suite twice, coverage run complete
- Test plan inventory updated (TC numbers of removed classes retired)

**Definition of Done:** standard (§4).

### TASK-090 Content catalog and Learn sidebar

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M6 |
| Requirements | FR-069, FR-071, NFR-023 |
| Decisions / design | UI-015, UI-020 |
| Depends on | TASK-062 |
| Risk | M |

**Goal:** Provide the content catalog and the Learn sidebar; samples open as untitled copies.

**Scope (in):**
- `IContentCatalog` from embedded resources
- Sidebar UI with keyboard navigation

**Out of scope:**
- Content authoring

**Expected files / projects touched:** `src/AssemblyStudio.Content/`, `src/AssemblyStudio.App/Learn/`

**Test requirements:**
- AC-035 (open-as-copy part)

**Definition of Done:** standard (§4).

### TASK-091 Sample programs with automated verification

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M6 |
| Requirements | FR-068 |
| Decisions / design | UI-015, ADR-0008 |
| Depends on | TASK-044, TASK-090 |
| Risk | L |

**Goal:** Write ≥ 8 samples with documented expected output, each verified by the program-level harness.

**Scope (in):**
- Hello, arithmetic and flags, loops, conditional jumps, stack and procedures, strings, keyboard input, addressing modes

**Out of scope:**
- Anything not listed above

**Expected files / projects touched:** `src/AssemblyStudio.Content/Samples/`

**Test requirements:**
- AC-035 (all samples pass)

**Definition of Done:** standard (§4).

### TASK-092 Tutorials and reference

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M6 |
| Requirements | FR-070, FR-071, FR-072 |
| Decisions / design | UI-015 |
| Depends on | TASK-090, TASK-020 |
| Risk | M |

**Goal:** Write ≥ 5 tutorials and the concept/instruction reference, with a consistency test against engine behavior.

**Scope (in):**
- Tutorials: registers, flags, memory and addressing, stack, loops and branches
- Reference generated from the opcode table + hand-written explanations
- Consistency test: flags-affected statements vs engine tests

**Out of scope:**
- Anything not listed above

**Expected files / projects touched:** `src/AssemblyStudio.Content/`

**Test requirements:**
- AC-036
- AC-037

**Definition of Done:** standard (§4).

### TASK-093 Help: scope, limitations, NASM vs MASM, shortcuts

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M6 |
| Requirements | FR-032, FR-045, NFR-033 |
| Decisions / design | UI-015 |
| Depends on | TASK-090, TASK-018, TASK-043 |
| Risk | L |

**Goal:** Document the supported scope and deviations offline.

**Scope (in):**
- Supported instructions/services/formats from TASK-018 table
- Deviations (ADR-0003/0004), assembler options, shortcuts

**Out of scope:**
- Anything not listed above

**Expected files / projects touched:** `src/AssemblyStudio.Content/Help/`

**Test requirements:**
- AC-051

**Definition of Done:** standard (§4).

### TASK-100 Product license decision (user action) (User action)

| Field | Value |
|---|---|
| Status | Approved |
| Milestone | M7 |
| Requirements | NFR-030 |
| Decisions / design | D-007, OQ-007 |
| Depends on | — |
| Risk | M (blocks release notices) |

**Goal:** User chooses the specific permissive license (e.g. MIT or Apache-2.0).

**Scope (in):**
- User decision recorded in DECISIONS.md; LICENSE file added

**Out of scope:**
- Anything not listed above

**Expected files / projects touched:** `LICENSE`, `DECISIONS.md`

**Test requirements:**
- LICENSE present and referenced by notices

**Definition of Done:** standard (§4), plus: the user confirms completion of the user action.

### TASK-101 Self-contained publish and Inno Setup installer

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M7 |
| Requirements | FR-083, FR-084, FR-085, FR-086, FR-087, FR-088, NFR-027, NFR-028 |
| Decisions / design | ADR-0009 |
| Depends on | TASK-005, TASK-068 |
| Risk | M (ADR-0009 Unverified: Inno Setup offline behavior, minimum OS) |

**Goal:** Build the offline per-user installer and measure size and install time.

**Scope (in):**
- `dotnet publish` self-contained win-x64 (no trim, no single-file)
- Inno Setup 7.x script: per-user, shortcuts, optional .asm association, bundled NASM/content/notices
- Upgrade keeps settings; uninstall keeps user files

**Out of scope:**
- Code signing (deferred)
- Auto-update (P3)

**Expected files / projects touched:** `build/installer/`

**Test requirements:**
- Installer builds
- Size and time measured (NFR-027/028)

**Definition of Done:** standard (§4).

### TASK-102 Third-party notices and SBOM

| Field | Value |
|---|---|
| Status | Approved |
| Milestone | M7 |
| Requirements | FR-090, NFR-030, NFR-031 |
| Decisions / design | ADR-0009 |
| Depends on | TASK-101, TASK-100 |
| Risk | L |

**Goal:** Produce the license inventory, notices and SBOM from the actual shipped files.

**Scope (in):**
- Inventory of every shipped file/package with license and obligations
- THIRD-PARTY-NOTICES.txt, SBOM

**Out of scope:**
- Anything not listed above

**Expected files / projects touched:** `build/installer/`, `docs/09-release/`

**Test requirements:**
- AC-042
- AC-061

**Definition of Done:** standard (§4).

### TASK-103 Clean-VM install, offline and compatibility verification

| Field | Value |
|---|---|
| Status | Approved |
| Milestone | M7 |
| Requirements | NFR-008, NFR-025, NFR-027, FR-083, FR-085, FR-088 |
| Decisions / design | ADR-0009, D-011 |
| Depends on | TASK-101, TASK-006 |
| Risk | M |

**Goal:** Verify install, first run, offline workflow, upgrade and uninstall on both supported OS versions.

**Scope (in):**
- Network disabled; network monitor during core workflow

**Out of scope:**
- Anything not listed above

**Expected files / projects touched:** `docs/07-testing/`

**Test requirements:**
- AC-034, AC-040, AC-041

**Definition of Done:** standard (§4).

### TASK-104 Localizability and message review

| Field | Value |
|---|---|
| Status | Approved |
| Milestone | M7 |
| Requirements | NFR-032, NFR-024 |
| Decisions / design | ADR-0007 |
| Depends on | TASK-085 |
| Risk | L |

**Goal:** Check that no user-facing string is hard-coded and every message has what/where/next.

**Scope (in):**
- Analyzer or test scanning XAML/C# for literals
- Message catalog review

**Out of scope:**
- Anything not listed above

**Expected files / projects touched:** `tests/AssemblyStudio.App.Tests/`

**Test requirements:**
- AC-055, AC-060

**Definition of Done:** standard (§4).

### TASK-105 Beginner usability session (user-run) (User action)

| Field | Value |
|---|---|
| Status | Approved |
| Milestone | M7 |
| Requirements | NFR-023 |
| Decisions / design | UI spec |
| Depends on | TASK-091, TASK-092, TASK-086 |
| Risk | M (needs participants) |

**Goal:** Run the first-success usability session with ≥ 5 first-time participants.

**Scope (in):**
- Agent prepares script and recording sheet; user runs sessions; results recorded

**Out of scope:**
- Anything not listed above

**Expected files / projects touched:** `docs/08-qa/`

**Test requirements:**
- AC-062

**Definition of Done:** standard (§4), plus: the user confirms completion of the user action.

### TASK-106 Variables panel

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M5 (added 2026-11-02, D-020) |
| Requirements | FR-097, FR-047, FR-056, FR-057 |
| Decisions / design | D-020, UI-023 |
| Depends on | TASK-082 |
| Risk | M |

**Goal:** Show the program's data labels and their current values so a learner doesn't have to read raw bytes.

**Scope (in):**
- Data labels and sizes from the NASM listing and map (`ISourceMap.DataSymbols`, additive Core contract)
- Variables view model fed by memory watches; value formats; change marks; open in the memory view
- Bottom tab, keyboard access, accessible names

**Out of scope:**
- Editing values (FR-062), type annotations, structures and arrays beyond hex bytes

**Expected files / projects touched:** `src/AssemblyStudio.Core`, `src/AssemblyStudio.Toolchain.Nasm`, `src/AssemblyStudio.App`, tests

**Test requirements:**
- AC-065

**Definition of Done:** standard (§4).

### TASK-107 UI test robustness

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M5 (added 2026-11-02, DEF-020) |
| Requirements | FR-091, FR-092, NFR-021 |
| Decisions / design | ADR-0008, D-018 |
| Depends on | TASK-085 |
| Risk | L |

**Goal:** A slow-starting app doesn't fail the UI smoke test, and a failed start leaves nothing behind (DEF-020).

**Scope (in):**
- `AppUnderTest`: when the main window isn't found, stop the process, delete the temporary folder and put the app's log in the failure message
- A longer window wait (60 s) for a loaded machine
- Run the UI test project after the other projects or on its own, so its window wait doesn't compete with them

**Out of scope:**
- Any product code change

**Expected files / projects touched:** `tests/AssemblyStudio.App.UiTests/`, test run configuration

**Test requirements:**
- A test of the cleanup path (a launch whose window never appears leaves no process and no folder)
- Full solution twice

**Definition of Done:** standard (§4).

### TASK-108 Startup time

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M7 (added 2026-11-02, DEF-021) |
| Requirements | NFR-005, AC-056 |
| Decisions / design | D-014, TASK-086 report |
| Depends on | TASK-086 |
| Risk | M |

**Goal:** Startup back under 3 s with margin (DEF-021), and a real cold start measured.

**Scope (in):**
- Measure first: per-step startup timings up to the first frame (settings, themes, NASM check, recovery scan, layout, workspace, window build, first render), written to the startup log
- Then speed up only the measured costly steps (defer work the first frame doesn't need, or do it lazily)
- Cold-start measurement after a reboot, run by the user and recorded

**Out of scope:**
- Changing the 3 s target (a requirement change needing the user's approval)

**Expected files / projects touched:** `src/AssemblyStudio.App/`, `tests/AssemblyStudio.App.UiTests/`, `docs/07-testing/`

**Test requirements:**
- The NFR-005 measurement (`build/bench/run-e2e.ps1`): median under 2.5 s on warm starts
- Existing startup tests; full solution twice

**Definition of Done:** standard (§4), plus: the user's cold-start measurement recorded.

### TASK-109 Find bar test without a simulated Esc

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M4 (added 2026-11-03, DEF-022) |
| Requirements | FR-015, FR-016, FR-017 |
| Decisions / design | UI-004 |
| Depends on | TASK-065 |
| Risk | L |

**Goal:** The find bar window test no longer depends on a real modifier key being held during the run (DEF-022).

**Scope (in):**
- `FindReplaceTests.UI_004_…`: check the bar's Esc binding (key and command) and run the command, instead of simulating the key; the same for Enter (find next) and F3
- The checks that Esc closes the bar and returns the focus to the editor stay

**Out of scope:**
- Any product change

**Expected files / projects touched:** `tests/AssemblyStudio.App.Tests/Editor/FindReplaceTests.cs`

**Test requirements:**
- `FindReplaceTests`; full solution twice

**Definition of Done:** standard (§4).

### TASK-110 Test script options

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M5 (added 2026-11-03, DEF-024) |
| Requirements | NFR-021 |
| Decisions / design | TASK-107 |
| Depends on | TASK-107 |
| Risk | L |

**Goal:** `build/test.ps1` takes the result options, so a test-phase run can use it as documented (DEF-024).

**Scope (in):**
- A `-ResultsDirectory` parameter: both steps write TRX and xUnit XML results there
- The broken `--` pass-through removed; the script's help and test plan §4 corrected

**Out of scope:**
- Any change to which tests run

**Expected files / projects touched:** `build/test.ps1`, `docs/07-testing/test-plan.md`

**Test requirements:**
- The script with and without `-ResultsDirectory` (result files written); full solution through the script

**Definition of Done:** standard (§4).

### TASK-111 Missing memory windows and repeated announcements

| Field | Value |
|---|---|
| Status | Done |
| Milestone | M5 (added 2026-11-03, DEF-025, DEF-026) |
| Requirements | FR-054, FR-097, FR-093, NFR-018 |
| Decisions / design | UI-008, UI-023 |
| Depends on | TASK-106 |
| Risk | M |

**Goal:** A snapshot without a panel's memory window never throws (DEF-025), and a state is announced once (DEF-026).

**Scope (in):**
- `VariablesViewModel.Show` and `MemoryViewModel.Show`: a missing window is treated as missing (`MemoryWindow` is a struct: no `FirstOrDefault` into a nullable)
- `BuildRunViewModel`: announce only when the session state or the instruction count changes (a refresh snapshot announces nothing)
- Regression tests: a snapshot without the window (both panels); a refresh snapshot at Ready (one announcement)

**Out of scope:**
- Other panel or session changes

**Expected files / projects touched:** `src/AssemblyStudio.App/Panels/Memory/`, `src/AssemblyStudio.App/Commands/BuildRunViewModel.cs`, tests

**Test requirements:**
- The regression tests fail before the fix and pass after it; full solution twice

**Definition of Done:** standard (§4).

## 4. Definition of Done (all tasks)
- [ ] Builds with zero warnings in affected projects (`dotnet build -c Release` executed)
- [ ] New and affected tests executed and passing (`dotnet test --solution AssemblyStudio.slnx` executed; oracle/NASM data present)
- [ ] The task's test requirements and linked acceptance criteria are met, or the partial scope is documented
- [ ] No fabricated data in the UI. Unsupported cases are surfaced explicitly
- [ ] New dependencies only per approved ADRs, with license recorded
- [ ] Every "Unverified" item the task owns is resolved and the result recorded (ADR validation section or `docs/06-implementation/`)
- [ ] Traceability (Tasks column), `WORK_LOG.md`, `CURRENT_TASK.md`, `PROJECT_STATE.md` and `HANDOFF.md` updated

## 5. Not planned for the MVP (P2/P3)
FR-009, FR-010, FR-043, FR-044, FR-059, FR-060, FR-061, FR-062, FR-073, FR-074, FR-089: post-MVP. Planned only after a new `/assembly-plan` run and approval. NFR-028 (P2, install footprint) is measured in TASK-101 but not gated.

## 6. Unverified items and their verifying tasks
| Task | Verifies |
|---|---|
| TASK-004 | coverlet MTP package name and xunit.v3 4.0.2 compatibility |
| TASK-019 | Iced 1.21.0 on .NET 10, INTEL8086_ONLY coverage (Verified in TASK-019) |
| TASK-043 | <10> nesting, nested includes, %rep, sections in macros |
| TASK-061 | WPF text scaling |
| TASK-064 | AvalonEdit 6.3.1 on net10.0-windows |
| TASK-069 | Narrator support in AvalonEdit |
| TASK-101 | Inno Setup offline behavior, minimum OS |

Every task that builds on these items depends on the verifying task (checked by the plan generator).

## 7. Risk register
| ID | Risk | Likelihood | Impact | Mitigation | Owner task |
|---|---|---|---|---|---|
| RISK-001 | NASM listing format is undocumented and may change | Low | High | Pin NASM 3.02. POC-002 fixtures + TASK-043 fixtures as regression tests | TASK-043 |
| RISK-002 | AvalonEdit fails on net10 or its screen-reader support is insufficient | Medium | High | Verify first in TASK-064/069 before building on it. AvalonEditB fallback. Raise a design issue if gaps | TASK-064, TASK-069 |
| RISK-003 | Emulator defect crashes the in-process host | Low | High | No recursion or unbounded allocation. Session-boundary catch. Fuzz + soak | TASK-054 |
| RISK-004 | Engine effort larger than expected (≈ 300 opcode forms) | Medium | Medium | Opcode families as separate tasks with a hard oracle gate. Help documents only what passes | TASK-012…TASK-020 |
| RISK-005 | Test VMs (Windows 10 Enterprise LTSC, Windows 11 24H2) unavailable | Medium | Medium | TASK-006 in M0 as an early user action | TASK-006 |
| RISK-006 | Unsigned installer triggers SmartScreen warnings | High | Medium | Signing deferred (OQ-020). Install guide documents the warning | TASK-101 |
| RISK-007 | FlaUI release cadence | Low | Low | Keep UI tests few. Manual QA checklists | TASK-085 |
| RISK-008 | Product license undecided at release | Medium | High | TASK-100 user action before notices | TASK-100 |
| RISK-009 | Developer setup needs network once (oracle data, NASM, NuGet) | Low | Low | Fetch scripts are idempotent and hash-verified. The product itself stays offline | TASK-003, TASK-005 |

## 8. Coverage
- Tasks: 62 in 8 milestones (3 user actions).
- P0 requirements covered by ≥ 1 task: 77/77. P1 covered: 40/40 (no deferrals).
- P2/P3 not planned: 11 (§5).
- Per-requirement task lists: the Tasks column of `docs/01-requirements/REQUIREMENTS_TRACEABILITY.md`.

## Changelog
- 2026-10-26 v0.1: Initial backlog (SDLC-008).
- 2026-10-26 v1.0: **Approved** by the user (2026-10-26 19:17 +0500: "approve backlog v0.1"). All milestones and tasks → Approved.
- 2026-11-03: TASK-111 (DEF-025, DEF-026) added. User 2026-11-03 13:42 +0500, answering "How should I close TASK-109?": "Done, defects separate (Recommended)"; and "Should I add a fix task (TASK-111) for DEF-025 and DEF-026 …?": "Approve TASK-111, next (Recommended)".
- 2026-11-03: TASK-109 (find bar test, DEF-022) and TASK-110 (test script options, DEF-024) added. User 2026-11-03 13:24 +0500, answering "Do you approve adding TASK-109 (find bar test without a simulated Esc, DEF-022) and TASK-110 (build/test.ps1 -ResultsDirectory, DEF-024) to the backlog as described?": "Approve both (Recommended)".
- 2026-11-02: TASK-107 (UI test robustness, DEF-020) and TASK-108 (startup time, DEF-021) added from test report 2026-11-02-1. User 2026-11-02 20:37 +0500, answering "Do you approve adding TASK-107 (UI test robustness, DEF-020) and TASK-108 (startup time, DEF-021) to the backlog as described?": "Approve both (Recommended)".
- 2026-11-02: TASK-106 (Variables panel, FR-097) added by the user's request (D-020). User 2026-11-02 15:50 +0500, answering "Do you approve adding FR-097, AC-065, UI-023 and TASK-106 (the Variables panel, as described above) to the plan, and should I implement TASK-106 now?": "Approve and implement now (Recommended)".
