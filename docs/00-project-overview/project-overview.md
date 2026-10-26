# Assembly Studio — Project Overview

| Field | Value |
|---|---|
| Document | Project overview |
| Status | Draft (context document; refined by `/assembly-requirements`) |
| Version | 0.3 |
| Last updated | 2026-10-26 |

## Vision
Assembly Studio is an independent, offline-first Windows desktop IDE for **learning, writing,
assembling, executing, debugging and visualizing** Assembly Language programs. It aims for a
professional, premium-quality experience that makes the machine's state (registers, flags, memory,
stack, execution history) visible and understandable.

## Initial scope (to be formalized as requirements)
- Source editor with Assembly syntax highlighting and diagnostics.
- Assembler integration.
- CPU registers and flags visualization.
- Step-by-step execution, breakpoints, execution history.
- Memory and stack visualization.
- Program output and diagnostics.
- Built-in tutorials and sample programs.
- Fully offline operation. Windows installer.

**Initial target:** 8086 / 16-bit real-mode DOS programs.
**Planned evolution:** broader x86 (e.g. 80186/80286/80386 real mode), then x86-64. Nothing beyond
8086 is in scope until requirements and architecture say so.

## Domain separation (why this is not "just an interpreter")
Running a student's program involves five distinct layers. Each is a separate decision:

1. **Assembler**: source → machine code and listing/symbol information (e.g. NASM, to be evaluated).
2. **CPU execution engine**: decodes and executes instructions, maintains registers and flags.
3. **Runtime environment**: memory map, program loading (.COM/.EXE), PSP, I/O ports and devices.
4. **Operating-system services**: DOS/BIOS interrupts (INT 21h, INT 10h, INT 16h…). These are not
   provided by a CPU emulator.
5. **Visualization and debugging**: presents real engine state. It never fabricates data.

## Technology direction (preferences, not decisions)
C# · WPF/XAML · MVVM · .NET 10 LTS · editor component to evaluate (AvalonEdit or alternative) ·
xUnit or justified alternative · NASM to evaluate · execution engine to research · Inno Setup
and .NET publishing options to evaluate. Target OS: Windows 10/11.
These preferences become decisions only through approved ADRs (`docs/decisions/`).

## Environment observed at initialization (2026-10-26)
| Item | Observation |
|---|---|
| OS | Windows 11 Pro 10.0.26200.9711 |
| .NET | Runtime 8.0.8 (NETCore + WindowsDesktop) only. **No .NET SDK installed** (`dotnet --list-sdks` → none) |
| Git | 2.55.0.windows.2. Repository initialized 2026-10-26 (D-004) |
| NASM | 3.02 on PATH (`nasm -v`). Not evaluated or approved yet (OQ-002) |
| IDE | JetBrains Rider project files present (`.idea/`, `.mcp.json` Rider MCP) |
| AI agents | Claude Code 2.1.308. Google Antigravity CLI 1.4.7 (auto-updated from 1.4.4; workspace skills re-verified) and desktop IDE 1.11.17 (see `AGENTS.md`) |

## Open decisions at initialization
| ID | Question | Impact | Resolved in |
|---|---|---|---|
| OQ-001 | Which execution engine approach (existing emulator library, custom C# 8086 core, or hybrid)? | High | Research → ADR |
| OQ-002 | Which assembler(s), and is bundling NASM's license/binaries acceptable? | High | Research → ADR |
| OQ-003 | Which DOS/BIOS services must be emulated in v1 (e.g. INT 21h subset)? | High | Requirements |
| OQ-004 | .EXE (MZ) support in v1 or .COM only? | Medium | Requirements |
| OQ-005 | Target .NET version (10 LTS preferred) and its WPF support lifecycle for Windows 10. | Medium | Research → ADR |
| OQ-006 | ~~Initialize a Git repository?~~ Resolved 2026-10-26: initialized with an initial commit (D-004). Remote/hosting still open | Low | User decision |
| OQ-007 | Product licensing and distribution model (open source? which license?). Affects which dependencies are allowed. | High | Requirements |

These are carried into `docs/01-requirements/requirements.md` when it is created.

## Changelog
- 2026-10-26 v0.1: Created at initialization.
- 2026-10-26 v0.2: Environment updated (NASM, Antigravity, Git). OQ-006 resolved.
- 2026-10-26 v0.3: Minor. Environment refreshed by `/assembly-init` (Antigravity CLI 1.4.7, Windows build, Git version).
