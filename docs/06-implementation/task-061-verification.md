# TASK-061 Themes, tokens and contrast test: verification notes

| Field | Value |
|---|---|
| Document | Implementation verification record |
| Task | TASK-061 (NFR-022, FR-094, NFR-026; UI-001; design tokens v1.0) |
| Last updated | 2026-10-30 |
| Author | Claude Code (Opus 5.5) |

Evidence labels as in `docs/sdlc/guides/sdlc-documentation.md`.

## Results
| Item | Result | Evidence |
|---|---|---|
| Every theme defines every color token | `Themes/Dark.xaml`, `Light.xaml`, `HighContrast.xaml` each define exactly the 32 color and syntax tokens of design tokens §1. The 8 syntax tokens were missing from the TASK-060 `Dark.xaml` and are added. Typography, spacing and sizes moved to `Themes/Shared.xaml` (the same in every theme) | [Measured] `ThemeTokenTests.Every_theme_defines_exactly_the_color_tokens` |
| Dark and Light equal the approved values | All 64 values equal the token table (read from `design-tokens.md` by the test) | [Measured] `Dark_and_light_match_the_approved_token_values` |
| WCAG 2.2 contrast (NFR-022) | **Dark 95/95, Light 95/95 (190 pairs) pass.** Text ≥ 4.5:1, UI components, glyphs and squiggles ≥ 3:1. Lowest: Dark `Border.Strong` on `Editor.ExecutionLine` 3.12:1; Light `Status.Success` on `Surface.Window` 4.85:1 and `Syntax.Comment` on `Editor.Selection` 4.86:1. The formula reproduces the values design tokens §2 measured on 2026-10-26 (4.85, 3.47, 5.28) | [Measured] `Every_contrast_pair_meets_wcag_2_2_aa`, `The_contrast_formula_reproduces_the_documented_measurements` |
| About "166 pairs" | The SDLC-007 script that counted 166 pairs (83 per theme) is not in the repository, so its exact list couldn't be reproduced. The test rebuilds the list from the description in §2. Its 95 pairs per theme are a superset: every UI text color on window, panel and raised surfaces; every syntax color on the 4 editor backgrounds; text on the accent; and focus ring, control border, accent, both glyphs and both squiggle colors on window, panel, editor, gutter and execution-line backgrounds | — |
| High contrast (FR-094) | Every token maps to a `SystemColors` key per §3, via DynamicResource, so the user's palette applies and follows live changes. Syntax highlighting is off (one text color); status, breakpoint, arrow and changed values use WindowText. Two choices where §3 is silent: `Editor.CurrentLine` = Window (no distinct color); `Editor.ExecutionLine` background = Window, with the 1 px Highlight outline drawn by the editor (TASK-064/066), so text on it stays WindowText | [Measured] `High_contrast_maps_every_token_to_the_system_color_of_section_3`; render `artifacts/ui/shell-empty-highcontrast.png` (system palette of this PC, not in high-contrast mode) |
| Theme selection | `ThemeManager`: Windows high contrast wins; otherwise the preference (System, Dark, Light), where System follows "Choose your app mode" (`AppsUseLightTheme`). It re-applies on `SystemParameters.HighContrast` and `UserPreferenceChanged`. The theme dictionary is replaced, and open windows update at once (all token references are DynamicResource). The Settings override UI and persistence are TASK-067 (`ThemeManager.Preference` is the hook) | [Measured] `High_contrast_wins_and_system_follows_the_windows_app_mode`, `Switching_the_theme_updates_an_open_window…`; render `artifacts/ui/shell-empty-light.png` |
| Text scaling at 200 % | Implemented in TASK-060 (`TextScale`): the font-size tokens are scaled by the Windows text scale factor, and the window was rendered at 200 % at 1024 × 768 without clipping. Unchanged here: the sizes moved to `Shared.xaml`, which `TextScale` still reads through the application resources | [Measured] `MainWindowSmokeTests.Renders_200_percent_text_at_the_minimum_window_size_for_review` (passes) |
| **AC-059 display scaling (manual)** | **Not done: needs the user** (real displays). This PC runs at 150 %; TASK-060 recorded that the window fits there. See the checklist below | **BLK-002** |

## AC-059 manual check (for the user)
Use `src/AssemblyStudio.App/bin/Release/net10.0-windows/AssemblyStudio.App.exe` (build with `dotnet build AssemblyStudio.slnx -c Release`).

| # | Setting (Settings ▸ System ▸ Display ▸ Scale) | Check | Result |
|---|---|---|---|
| 1 | 100 % | Window opens; no clipped text in menus, command bar, panel headers, tabs, status bar; text sharp | Not run |
| 2 | 125 % | Same | Not run |
| 3 | 150 % | Same | Not run |
| 4 | 200 % | Same; at the minimum size 1024 × 768 (if the screen allows) the command bar shows icons only | Not run |
| 5 | Two monitors with different scale | Drag the window between them: it re-renders sharp at each scale, no clipping | Not run |
| 6 | Light app mode (Settings ▸ Personalization ▸ Colors ▸ Light) | The app switches to the light theme while open | Not run |
| 7 | High contrast on (Settings ▸ Accessibility ▸ Contrast themes) | The app switches to system colors while open; all text readable; focus ring visible | Not run |

Record the results here (date, monitor setup) and tell the agent; the task becomes Done when rows 1–5 pass.
