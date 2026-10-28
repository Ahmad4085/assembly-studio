# Defects

| ID | Title | Severity | Found in | Status | Description |
|---|---|---|---|---|---|
| DEF-001 | `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` fails when window is inactive | Low | TASK-060 / TASK-016 | Open | In `tests/AssemblyStudio.App.Tests/MainWindowSmokeTests.cs`, `view.IsSubmenuOpen = true` is asserted when the window may not be active/focused. In non-interactive or background test executions, WPF's `Menu` does not maintain submenu open state without active focus, causing `Assert.True(view.IsSubmenuOpen)` to fail. |
