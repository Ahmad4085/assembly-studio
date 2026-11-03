# Assembly Studio — Design Tokens

| Field | Value |
|---|---|
| Document | Design tokens |
| Status | Approved |
| Version | 1.1 |
| Last updated | 2026-11-03 |
| Depends on | `docs/04-design/ui-spec.md` v1.1, `docs/sdlc/guides/desktop-ui-quality.md` |

Tokens are **semantic**: views reference token names only, never raw colors or sizes. Each theme is a WPF resource dictionary
(`Themes/Dark.xaml`, `Themes/Light.xaml`, `Themes/HighContrast.xaml`) defining every token key. The default theme follows the Windows
app mode (dark or light). Windows high-contrast mode overrides both (§3).

## 1. Color tokens
| Token | Dark | Light | Use |
|---|---|---|---|
| `Surface.Window` | `#1B1C1F` | `#F3F4F6` | Main window background |
| `Surface.Panel` | `#232428` | `#FFFFFF` | Tool panel background |
| `Surface.Raised` | `#2B2D32` | `#FFFFFF` | Menus, popups, dialogs |
| `Surface.Editor` | `#1B1C1F` | `#FFFFFF` | Editor text area |
| `Surface.Gutter` | `#1B1C1F` | `#FFFFFF` | Editor gutter (line numbers, breakpoints, execution arrow) |
| `Editor.CurrentLine` | `#26282C` | `#F2F4F7` | Caret line background |
| `Editor.ExecutionLine` | `#3A3420` | `#FFF3C4` | Line about to execute (paused) |
| `Editor.Selection` | `#1D3550` | `#DCEBFF` | Text selection |
| `Border.Subtle` | `#34373D` | `#DADDE2` | Panel separators (decorative, no contrast target) |
| `Border.Strong` | `#7A808A` | `#7D838D` | Input and control boundaries (≥ 3:1) |
| `Text.Primary` | `#E8E9EB` | `#1D2125` | Body text, values |
| `Text.Secondary` | `#BFC3C9` | `#454C55` | Labels, panel titles |
| `Text.Muted` | `#9BA1AA` | `#5E6570` | Hints, metadata |
| `Text.Disabled` | `#62666D` | `#A3A8B0` | Disabled controls (WCAG-exempt, never the only cue) |
| `Text.OnAccent` | `#0B1220` | `#FFFFFF` | Text on Accent.Primary buttons |
| `Accent.Primary` | `#5AA2FF` | `#0A5FCC` | Primary button, active tab indicator, links |
| `Focus.Ring` | `#5AA2FF` | `#0A5FCC` | 2 px keyboard focus indicator |
| `Accent.ExecutionArrow` | `#F5C542` | `#A86E00` | Execution arrow glyph |
| `Accent.Breakpoint` | `#F0605A` | `#C62828` | Breakpoint glyph |
| `Status.Error` | `#FF7B72` | `#B42318` | Error text, icons, squiggles |
| `Status.Warning` | `#E3B341` | `#8A5300` | Warning text, icons, squiggles |
| `Status.Info` | `#79B8FF` | `#0B5CAD` | Info text and icons |
| `Status.Success` | `#5FB870` | `#1E7B34` | Build succeeded, normal termination |
| `Value.Changed` | `#FFD866` | `#8A4500` | Changed register/flag/memory value (always with a non-color cue) |

### Control tokens (v1.1: text boxes, buttons, check boxes, combo boxes)
Standard input controls are themed like the rest of the window (before v1.1 they kept the Windows system look: bright white boxes and
grey buttons in the dark theme). Boundaries, focus and text reuse the tokens above; only the control surfaces are new.

| Token | Dark | Light | Use |
|---|---|---|---|
| `Control.Input` | `#1B1C1F` | `#FFFFFF` | Text box and combo box field, check box square |
| `Control.Button` | `#303237` | `#EEF0F3` | Button at rest (a dialog's or panel's secondary actions) |
| `Control.Hover` | `#3D4148` | `#E2E5EA` | Button, combo box and drop-down item under the pointer |
| `Control.Pressed` | `#474B53` | `#D3D7DD` | Button pressed |
| `Control.Selected` | `#1D3550` | `#DCEBFF` | Selected item in a combo box drop-down |
| `Accent.Primary.Hover` | `#7AB4FF` | `#0A4FAB` | Default button (Enter) under the pointer |

Reused for controls: `Border.Strong` (1 px boundary), `Focus.Ring` (2 px when focused), `Text.Primary` (text, check mark, drop-down
chevron), `Text.Muted` (hints), `Text.Disabled` with `Border.Subtle` (disabled), `Status.Error` (an invalid entry's border and message),
`Accent.Primary` with `Text.OnAccent` (the default button). Text selection inside a text box uses `Accent.Primary` at 40 % opacity.

### Syntax tokens (editor, NASM 8086)
| Token | Dark | Light | Highlights |
|---|---|---|---|
| `Syntax.Mnemonic` | `#82AAFF` | `#0B4FA3` | Instruction mnemonics (`mov`, `int`) |
| `Syntax.Register` | `#F7A072` | `#9C3B00` | Registers (`ax`, `ds`) |
| `Syntax.Directive` | `#D3A6F5` | `#7A2FA3` | Directives (`org`, `section`, `db`, `times`, `%macro`) |
| `Syntax.Number` | `#F2C76E` | `#00695C` | Numeric literals (`21h`, `0x10`, `1010b`, `'A'`) |
| `Syntax.String` | `#B5E08A` | `#2E6B1F` | String literals |
| `Syntax.Comment` | `#A3AAB4` | `#5E6570` | Comments |
| `Syntax.Label` | `#7FDBCA` | `#005C80` | Label definitions and references |
| `Syntax.Text` | `#E8E9EB` | `#1D2125` | Everything else |

## 2. Contrast verification (WCAG 2.2 AA) [Measured]
Computed with the WCAG 2.2 relative-luminance formula on 2026-10-26 for **every** pair below, by a script run during SDLC-007 (the script is
**not** in the repository). The first theme implementation task must add an automated contrast test over the XAML theme dictionaries.
Targets: text ≥ 4.5:1; UI components, glyphs and squiggles ≥ 3:1. `Border.Subtle` is decorative (never the only boundary of a control).
`Text.Disabled` is exempt (inactive components) and never the only cue.

**Dark: 83/83 pairs pass.** Lowest margins:

| Foreground | Background | Kind | Minimum | Measured |
|---|---|---|---|---|
| `Accent.Primary` | `Surface.Raised` | text | 4.5:1 | **5.28:1** |
| `Text.Muted` | `Surface.Raised` | text | 4.5:1 | **5.3:1** |
| `Syntax.Comment` | `Editor.ExecutionLine` | code text | 4.5:1 | **5.3:1** |
| `Syntax.Comment` | `Editor.Selection` | code text | 4.5:1 | **5.35:1** |
| `Syntax.Mnemonic` | `Editor.ExecutionLine` | code text | 4.5:1 | **5.4:1** |

**Light: 83/83 pairs pass.** Lowest margins:

| Foreground | Background | Kind | Minimum | Measured |
|---|---|---|---|---|
| `Status.Success` | `Surface.Window` | text | 4.5:1 | **4.85:1** |
| `Syntax.Comment` | `Editor.Selection` | code text | 4.5:1 | **4.86:1** |
| `Border.Strong` | `Surface.Window` | UI component | 3.0:1 | **3.47:1** |
| `Syntax.Comment` | `Editor.ExecutionLine` | code text | 4.5:1 | **5.28:1** |
| `Status.Success` | `Surface.Panel` | text | 4.5:1 | **5.33:1** |

The full pair list (166 pairs: text and code text on every background including current, execution and selection lines; focus ring, control borders, accent, glyphs and squiggles on window, panel, editor, gutter and execution-line backgrounds) must be re-measured whenever a token changes (by that theme test).

### Control pairs (v1.1) [Measured]
Computed with the same WCAG 2.2 formula on 2026-11-03 for the control tokens: text and hints on every control surface; `Border.Strong`
and `Focus.Ring` on every surface a control sits on and on the control's own fill; the check mark and chevron (3:1); the default button.
**Dark: 25/25 pass. Light: 25/25 pass.** Lowest margins:

| Theme | Foreground | Background | Kind | Minimum | Measured |
|---|---|---|---|---|---|
| Dark | `Border.Strong` | `Control.Button` | UI component | 3.0:1 | **3.23:1** |
| Dark | `Border.Strong` | `Surface.Raised` | UI component | 3.0:1 | **3.47:1** |
| Light | `Border.Strong` | `Control.Button` | UI component | 3.0:1 | **3.34:1** |
| Light | `Border.Strong` | `Surface.Window` | UI component | 3.0:1 | **3.47:1** |

(A first value of `#34373D` for dark `Control.Button` measured exactly 3.00:1 against `Border.Strong` and was darkened for margin.)
The automated theme contrast test (TC-066) must add these 50 pairs when the tokens are implemented.

## 3. High-contrast mapping
In Windows high-contrast mode, **all** color tokens map to system colors (`SystemColors.*BrushKey` dynamic resources). Meaning is carried by
shape, glyph and weight, never by hue alone:

| Token group | System color |
|---|---|
| `Surface.*`, `Surface.Gutter` | `WindowBrush` |
| `Text.Primary`, `Text.Secondary`, `Syntax.*` | `WindowTextBrush` (syntax highlighting off: one text color) |
| `Text.Muted`, `Text.Disabled` | `GrayTextBrush` |
| `Editor.Selection` (+ text) | `HighlightBrush` / `HighlightTextBrush` |
| `Accent.Primary`, links | `HotTrackBrush` |
| `Focus.Ring`, `Border.Strong` | `WindowTextBrush` (2 px) |
| `Editor.ExecutionLine` | `HighlightBrush` outline (1 px) + arrow glyph in `WindowTextBrush` |
| `Accent.Breakpoint`, `Accent.ExecutionArrow`, `Status.*`, `Value.Changed` | `WindowTextBrush`. Glyph shape (●, ➜, ⨯, ▲, ⓘ) and bold weight carry the meaning |
| `Control.Input` (+ text) | `WindowBrush` / `WindowTextBrush` (v1.1) |
| `Control.Button` (+ text) | `ControlBrush` / `ControlTextBrush` (button face, v1.1) |
| `Control.Hover`, `Control.Pressed`, `Control.Selected` (+ text) | `HighlightBrush` / `HighlightTextBrush` (v1.1) |
| `Accent.Primary.Hover` | `HotTrackBrush` (v1.1); the default button also has a 2 px `WindowTextBrush` border |

## 4. Typography
| Token | Value | Notes |
|---|---|---|
| `Font.UI` | Segoe UI (Windows system font) | Not bundled |
| `Font.Code` | Cascadia Mono → fallback Consolas | Not bundled in the MVP. Availability on Windows 10 Enterprise LTSC 21H2 [Unverified → first shell task]. Both distinguish 0/O and 1/l/I |
| `FontSize.UI` | 13 px | Menus, panels, dialogs |
| `FontSize.UI.Small` | 12 px | Status bar, metadata (never below 12 px) |
| `FontSize.Title` | 15 px semibold | Dialog and empty-state titles |
| `FontSize.Code` | 14 px | Editor, registers, memory, stack, console. Zoom 50–300 % (FR-022) |
| `FontWeight.Changed` | SemiBold | Non-color cue for changed values |
| `Typography.Numbers` | Tabular | `Font.Code` for all values, so columns don't jitter |

Text respects Windows text scaling. WPF doesn't apply the "Make text bigger" setting automatically [Unverified → shell task must implement
and verify at 200 %, NFR-026].

## 5. Spacing, size, radius, elevation, motion
| Token | Value |
|---|---|
| `Space.1` … `Space.6` | 2, 4, 8, 12, 16, 24 px |
| `Size.RowHeight` | 22 px (register, flag, list rows) |
| `Size.PanelHeader` | 28 px |
| `Size.CommandBar` | 36 px |
| `Size.Gutter.Breakpoint` | 16 px |
| `Size.MinWindow` | 1024 × 768 (NFR-026) |
| `Radius.Small` / `Radius.Medium` | 2 px (inputs, badges) / 4 px (popups, dialogs) |
| `Size.Control` | 24 px minimum height of text boxes, buttons and combo boxes; padding `Space.2` horizontal, `Space.1` vertical (v1.1) |
| `Size.CheckBox` | 14 px square, `Space.2` gap to its label (v1.1) |
| `Elevation.Flat` | Panels and docked areas use 1 px `Border.Subtle` separators, no shadows |
| `Elevation.Popup` | Menus, tooltips, dialogs: 1 px `Border.Strong` + 8 px soft shadow (disabled in high contrast) |
| `Motion.Fast` | 120 ms ease-out (panel collapse, banners). **No motion while running or stepping** |
| `Motion.None` | Honors Windows "Show animations" off → all motion off |

## Changelog
- 2026-10-26 v0.1: Initial tokens (SDLC-007). All 166 contrast pairs measured, all passing.
- 2026-10-26 v1.0: **Approved** by the user (2026-10-26 19:03 +0500: "approve" (reply to the gate question naming ui-spec v0.1, design tokens v0.1, the ADR-0007 v1.1 amendment and the Ctrl+C proposal)).
- 2026-11-03 15:21 +0500 v1.1: **Substantive** (`/assembly-design`, scope chosen by the user: "Themed input controls only"): control tokens `Control.Input`, `Control.Button`, `Control.Hover`, `Control.Pressed`, `Control.Selected`, `Accent.Primary.Hover`; `Size.Control`, `Size.CheckBox`; 50 new contrast pairs measured, all passing; high-contrast mapping for them. No existing token changed. Status `Awaiting approval`.
- 2026-11-03 v1.1: **Approved** by the user (User 2026-11-03 15:30 +0500: "approve UI spec v1.1 and design tokens v1.1"; OQ-024 and OQ-026 remain open).
