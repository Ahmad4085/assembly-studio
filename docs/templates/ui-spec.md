# Assembly Studio — UI/UX Specification

| Field | Value |
|---|---|
| Document | UI/UX specification |
| Status | Draft |
| Version | 0.1 |
| Last updated | YYYY-MM-DD |
| Depends on | requirements.md v<x>, architecture.md v<x> |

## 1. Design principles
3–6 principles specific to a learning-focused Assembly IDE (e.g. "machine state is always one
glance away"), each tied to requirement IDs.

## 2. Information architecture and layout
Main window regions (menu/command bar, editor, docked panels, status bar), default layout,
docking/resizing rules, minimum window size, layout persistence.

## 3. Screens and panels
| ID | Panel / screen | Purpose | Content | Data source (engine API) | Requirements |
|---|---|---|---|---|---|
| UI-001 | Editor | | | | |
| UI-002 | Registers | | | | |
| UI-003 | Flags | | | | |
| UI-004 | Memory | | | | |
| UI-005 | Stack | | | | |
| UI-006 | Output / console | | | | |
| UI-007 | Diagnostics | | | | |
| UI-008 | Execution history | | | | |
| UI-009 | Breakpoints | | | | |
| UI-010 | Tutorials / samples | | | | |

## 4. Interaction states
For each panel: empty, loading/assembling, ready, running, paused, stepping, error, unsupported,
disabled. Define what is shown and which actions are enabled. Changed-value highlighting rules.

## 5. Editor behavior
Syntax highlighting, error squiggles, gutter (breakpoints, current line), find/replace, folding,
indentation, current-instruction highlight, listing/address mapping.

## 6. Commands and keyboard
| Command | Shortcut | Menu location | Enabled when |
|---|---|---|---|

## 7. Themes and design tokens
Reference `docs/04-design/design-tokens.md` (color, typography, spacing, radius, elevation,
motion). Dark and light themes, plus high-contrast support.

## 8. Accessibility
Keyboard-only operation, focus order, UI Automation names/roles, contrast ratios, Windows
high-contrast mode, text scaling, screen-reader announcements for execution state changes.

## 9. Component conventions
Reusable controls (panel header, value cell, hex/decimal toggle, toolbar button), states and
XAML style naming.

## 10. Open design questions
| ID | Question | Impact | Status |
|---|---|---|---|

## Changelog
- YYYY-MM-DD v0.1: Initial draft.
