# ADR-0006: Code editor — AvalonEdit

| Field | Value |
|---|---|
| Status | Approved |
| Version | 1.0 |
| Date | 2026-10-26 |
| Requirements addressed | FR-011, FR-012, FR-013, FR-014, FR-015, FR-016, FR-017, FR-018, FR-019, FR-020, FR-022, FR-052, FR-053, FR-091, FR-093, NFR-001, NFR-022 |
| Research | `docs/02-research/editor-mvvm-testing.md` (R-EDIT) |

## Context
A native WPF editor control is needed: syntax highlighting, gutter markers, squiggles, search, zoom and accessibility, all offline
and permissively licensed.

## Options considered
| Option | Pros | Cons | License |
|---|---|---|---|
| **AvalonEdit 6.3.1.120** | Native WPF. XSHD highlighting, margins, background renderers, search panel. Mature | No released net10 build (the net8 build runs via compatibility, unverified). No built-in squiggles. Find-only search panel [Hypothesis]. Basic UIA support, open Narrator issue | MIT |
| AvalonEditB 2.5.0 | Adds Replace, block selection, current-line styling | Single maintainer. net6/net472 builds only | MIT |
| Monaco in WebView2 | Richest features and accessibility | Second UI stack. WebView2 runtime must be bundled offline (>250 MB fixed version) | MIT + BSD-style SDK |
| Scintilla.NET | Mature | WinForms only (hosting drawbacks) | MIT |

## Decision
Use **AvalonEdit 6.3.1.120** (MIT) and build:
- an XSHD definition for NASM 8086 syntax (FR-011);
- a squiggle/marker background renderer for diagnostics (FR-018);
- a breakpoint and current-line margin (FR-052, FR-053);
- our own Find/Replace and Go to Line UI (FR-015–FR-017);
- zoom (FR-022);
- encoding and line-ending handling in the document service (FR-019, FR-020; that logic lives outside AvalonEdit);
- **accessibility work:** UIA names and live announcements for execution state (FR-093). The editor's own TextPattern support is
  verified with Narrator.

AvalonEditB stays a fallback if upstream proves inadequate. Switching is cheap because both share the AvalonEdit API [Hypothesis].

## Consequences
- AvalonEdit is referenced only by `AssemblyStudio.App` (ADR-0002).
- An upstream 6.3.2 release with net10 support is watched. The pin is updated through a task.

## Validation
- **Unverified → first editor task:** AvalonEdit 6.3.1 runs on `net10.0-windows`; Narrator reads lines and caret position; the
  upstream SearchPanel capabilities. If Narrator support is insufficient, a requirement or design issue is raised (NFR-022) rather
  than silently accepted.
- NFR-001 (≤ 50 ms keystroke latency in 5,000 lines) is measured in the editor task.
- **Verified 2026-10-31 (TASK-064):** AvalonEdit 6.3.1.120 (its net8.0-windows build) loads, renders, edits and undoes on
  `net10.0-windows` (.NET 10.0.14; `AvalonEditPlatformTests`). Its UI Automation peer is a Document with Value, Text and Scroll
  patterns. NFR-001 measured on the development machine: median 14.5 ms, p95 22.2 ms (`EditorTests`). Narrator and the
  SearchPanel remain for TASK-069 and TASK-065.

## Changelog
- 2026-10-26 v0.1: Proposed (SDLC-006).
- 2026-10-26 v1.0: **Approved** by the user (2026-10-26 18:54 +0500: "approve architecture v0.1 and ADR-0001..0009; reference machine OK").
- 2026-10-31: Validation updated with the TASK-064 results (no decision changed).
