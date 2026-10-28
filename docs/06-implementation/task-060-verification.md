# TASK-060 WPF shell and layout: verification notes

| Field | Value |
|---|---|
| Document | Implementation verification record |
| Task | TASK-060 (FR-091, FR-092, NFR-026, NFR-005; UI-001, UI-002, UI-016) |
| Last updated | 2026-10-28 |
| Author | Claude Code (Opus 5.5) |

Records the results for the items design tokens §4, ui-spec §9 and NFR-005 hand to "the first shell task".
Evidence labels as in `docs/sdlc/guides/sdlc-documentation.md`.

## Results
| Item | Source | Result | Evidence |
|---|---|---|---|
| Startup time ≤ 3 s (NFR-005) | backlog TASK-060 test requirements | **Measured** on the development machine: 1,482–2,543 ms over 5 consecutive smoke launches; 4,854 ms on the first launch straight after a build; 1,622 ms and 2,494 ms inside full test runs. Measured from process start to the main window's first rendered frame (`--smoke-report`). The formal check on the reference machine remains with TASK-086 | [Measured] `StartupTimeTests`, `WORK_LOG.md` 2026-10-28 |
| WPF doesn't apply Windows "Make text bigger" | design tokens §4 [Unverified → shell task] | **Implemented**: the shell reads `HKCU\Software\Microsoft\Accessibility\TextScaleFactor` (100–225) at startup and scales the four font-size tokens. Rendered at 200 % with the window at 1024 × 768: first render clipped the command bar labels and panel titles; fixed (minimum instead of fixed heights, icon-only command bar below 1280 px per ui-spec §9); second render shows no clipping | [Measured] `artifacts/ui/shell-text200-min.png` (rendered by `MainWindowSmokeTests`, not committed); the live Windows setting was not changed |
| Per-monitor DPI (NFR-026) | NFR-026 | `app.manifest` declares PerMonitorV2. The development machine runs at 150 % (1920 × 1080 → 1280 × 672 DIP work area); the window fits the work area there. Manual checks at 100/125/200 % and across monitors (AC-059) are not done here | [Measured] at 150 % only |
| Minimum window 1024 × 768 | NFR-026, ui-spec §2 | Enforced, except on a screen whose work area is smaller (the 150 % machine above has 672 DIP of height): the minimum then gives way to the screen instead of pushing the window off-screen | [Measured] |
| Segoe Fluent Icons / Cascadia Mono availability | ui-spec §9, OQ-025 | Development machine (Windows 11 Pro): `SegoeIcons.ttf` and `segmdl2.ttf` present; **Cascadia Mono not installed** (falls back to Consolas per design tokens §4). Windows 10 LTSC is still unverified; owned by TASK-070 | [Measured] `C:\Windows\Fonts` listing |

## Scope notes
- Layout persistence writes the `"layout"` object of `%APPDATA%\AssemblyStudio\settings.json` (ADR-0007 §4), keeping other
  properties. The settings service, the invalid-file notification (FR-081) and the Settings dialog remain TASK-067.
- Only the dark token set exists (`Themes/Dark.xaml`). Light, high contrast, theme switching and the contrast test are TASK-061.
- Menu and command-bar entries owned by later tasks are visible with their shortcuts but disabled.
