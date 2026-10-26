# Desktop IDE UI quality

## 1. What "premium" means here
- **IDE, not dashboard.** Dense, calm, information-first layout (think professional code editors
  and debuggers). No card grids, KPI tiles, hero banners, decorative charts or gratuitous gradients.
- **Typography carries structure.** One UI font, one monospaced code font with clear 0/O and 1/l/I
  distinction. Register and memory values use tabular (monospaced) digits so columns don't jitter.
- **Restraint with color.** Neutral surfaces. Color reserved for meaning: current execution line,
  changed values, errors/warnings, breakpoints, selection. Every color comes from a semantic token.
- **Motion is functional.** Short (≤150 ms) transitions for state changes. Nothing animates while stepping fast.
- **Crisp at every DPI.** Vector icons, layout rounding, per-monitor DPI awareness. Test at 100/150/200 %.

## 2. Interaction rules
- Every command is reachable by keyboard and shown with its shortcut in menus and tooltips.
- Panels show explicit states: empty (what to do next), working, paused, error (what happened + how
  to fix), unsupported (what isn't supported). Never a blank panel.
- Changed values after a step are highlighted until the next step. The highlight must not rely on color alone
  (also use weight, marker or border).
- Destructive actions are confirmable or undoable. Unsaved changes are never silently lost.
- Long operations show progress and can be cancelled. The UI thread never freezes.

## 3. Accessibility (target WCAG 2.2 AA where applicable to desktop)
- Contrast: text ≥ 4.5:1, large text and UI component boundaries ≥ 3:1, in every theme.
- Windows high-contrast mode respected (system colors via `SystemColors`, not hard-coded brushes).
- UI Automation: meaningful `AutomationProperties.Name`, roles, and live announcements for
  execution state changes (paused at line N, error).
- Logical tab order, visible focus indicator, no keyboard traps (editor Tab handling has an escape route).
- Honors Windows text scaling. Layouts reflow without clipping.

## 4. Visualization integrity
- Every value shown comes from the current engine snapshot. When the program is running and the
  values are stale, show that state explicitly.
- No sample/mock data in production code paths. Design-time data (`d:DataContext`) only.
- Unknown or undefined values (e.g. undefined flags) are displayed as such, not guessed.

## 5. Review checklist
- [ ] Layout matches the approved UI spec (UI-### IDs). Deviations recorded
- [ ] All colors, fonts, spacing come from tokens/resources. No hard-coded literals in views
- [ ] All panels have every state from the spec's state matrix
- [ ] Keyboard-only walkthrough of the core flow (edit → assemble → step → inspect) works
- [ ] Contrast verified in dark, light and high-contrast
- [ ] Narrator/UIA names present on interactive and value elements
- [ ] 100/150/200 % DPI and 200 % text scaling checked
- [ ] No generic dashboard patterns. Density appropriate for an IDE
- [ ] No fabricated execution data anywhere
