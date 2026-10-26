# /assembly-design: UI/UX specification

> **Session protocol:** run `docs/sdlc/agent-protocol.md` §3 (session start) before step 1 and §6
> (session end) before finishing. **Start this procedure only when the user explicitly asked for it**
> (slash command or a direct request naming it). Never start it on your own initiative.

## 1. Purpose
Define an implementable, premium desktop IDE experience before any XAML is written: what the
learner sees, how panels behave in every execution state, and the reusable tokens and components.

## 2. Inputs
`Arguments`: optional panel/area to (re)design.

## 3. Reads
Approved requirements and architecture (state model, engine API shape), `PROJECT_STATE.md`,
`docs/04-design/*`, `docs/sdlc/guides/desktop-ui-quality.md` (quality bar),
`docs/sdlc/guides/sdlc-documentation.md`, `docs/templates/ui-spec.md`.

## 4. Workflow
1. **Gate in:** architecture `Approved` (or waiver). Check *Depends on* versions.
2. **Principles and layout.** Derive design principles from requirements. Define the main window
   (command bar, editor, dockable panels, status bar), default layout, resizing and persistence.
3. **Panels (UI-###).** For each: purpose, content, data source (the engine/debugger API from the
   architecture), and requirement IDs. No panel may show data that has no real source.
4. **State matrix.** For every panel × state (empty, assembling, ready, running, paused, stepping,
   error, unsupported, disabled): what is shown and which actions are enabled. Changed-value highlighting.
5. **Editor behavior**, commands and keyboard shortcuts (follow established IDE conventions such as
   F5 run, F10 step over, F11 step into, F9 breakpoint, unless requirements say otherwise).
6. **Design tokens** in `docs/04-design/design-tokens.md`: color (dark, light, high-contrast
   mapping), typography (UI font, monospace code font, sizes), spacing scale, radius, elevation,
   motion durations. Name tokens semantically (e.g. `Surface.Panel`, `Text.Muted`,
   `Accent.ExecutionLine`), not by raw color.
7. **Accessibility** and **component conventions** per `desktop-ui-quality`.
8. Optional: low-fidelity ASCII/Mermaid wireframes inside the spec. Visual mockups only if the user asks.
9. Set `Awaiting approval`. Ask the gate question.

## 5. Outputs
`docs/04-design/ui-spec.md`, `docs/04-design/design-tokens.md`, `PROJECT_STATE.md` updated.

## 6. Validation
- Every UI-facing FR maps to ≥1 UI-###. Every UI-### maps to a real data source in the architecture.
- State matrix complete for each panel. Keyboard path exists for every command.
- Contrast targets stated for tokens (WCAG 2.2 AA: 4.5:1 text, 3:1 UI components).
- Passes the `desktop-ui-quality` checklist (no generic dashboard styling: no card grids,
  KPI tiles or decorative charts).

## 7. Approval
Gate question: "Approve UI spec v<x> and design tokens v<x>, with open design questions (list)?"

## 8. Failure handling
The spec needs data the architecture doesn't expose: raise it as an architecture change (OQ +
suggested ADR update). Don't invent an API.

## 9. Exit conditions
UI spec and tokens `Approved`.

## 10. Next permitted phase
`/assembly-plan`.
