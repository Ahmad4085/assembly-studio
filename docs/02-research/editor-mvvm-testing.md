# Research — Code Editor, MVVM Library and Testing Tools

| Field | Value |
|---|---|
| Document | Research: editor component, MVVM, testing, UI automation, coverage |
| Status | Approved |
| Version | 1.0 |
| Last updated | 2026-10-26 |
| Depends on | `docs/01-requirements/PRODUCT_REQUIREMENTS.md` (set) v1.0 |

**Method.** Sources were read on 2026-10-26 by a research agent (NuGet registration API, GitHub API and
repositories). The claims marked ✔ were independently re-fetched by the lead agent on the same day.

## 1. Questions
Which editor control can deliver FR-011–FR-022 and the accessibility requirements FR-091–FR-094 / NFR-022.
Which MVVM, test, UI-automation and coverage tools support NFR-017, NFR-020 and NFR-021. All must pass the license
gate (D-007) and work offline.

## 2. Code editor
| Option | License | Status | Fit | Evidence |
|---|---|---|---|---|
| **AvalonEdit** ✔ | MIT | Latest NuGet **6.3.1.120 (2025-04-13)**, built for net462, net6.0-windows and **net8.0-windows** (no net10.0 build). Master has net10.0 and version 6.3.2 (commit 2025-12-04), unreleased. 117 open issues | Native WPF. XSHD highlighting, folding, search panel, line-number margin, custom margins, `IBackgroundRenderer`. **No built-in squiggle markers** (build with a background renderer). Upstream search panel appears to be **find-only** [Hypothesis]. **UIA:** basic TextPattern support (PR #97, 2017). **Narrator issue #173 still open.** A fix PR has been open since 2021 | ✔ [Verified: https://www.nuget.org/packages/AvalonEdit] [Verified: https://github.com/icsharpcode/AvalonEdit] [Verified: https://github.com/icsharpcode/AvalonEdit/issues/173] |
| AvalonEditB | MIT | 2.5.0 (2026-10-08), net472 / net6.0-windows, single maintainer | Fork that adds search-and-replace, block selection and current-line styling | [Verified: https://www.nuget.org/packages/AvalonEditB] |
| Monaco in WebView2 | Monaco MIT. WebView2 SDK BSD-style | Active | Richest features and documented accessibility. **But:** a second UI stack (JS/HTML), MVVM bridging via messages, and the runtime must be bundled offline (Evergreen standalone installer, or Fixed Version over 250 MB that never auto-updates). Runtime redistribution terms [Unverified] | [Verified: https://github.com/microsoft/monaco-editor] [Verified: https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution] |
| Scintilla.NET | MIT | WinForms only (maintained fork) | WPF would need WindowsFormsHost (airspace and UIA drawbacks) [Hypothesis] | [Verified: https://api.github.com/repos/desjarlais/Scintilla.NET] |

**Recommendation R-EDIT:**
- Use **AvalonEdit 6.3.1.120** (MIT). Its net8.0-windows build should run on .NET 10 through framework
  compatibility; confirm in POC-003.
- Build the squiggle renderer, breakpoint and current-line margins, and the Replace UI ourselves (FR-016, FR-018).
  Alternatively adopt AvalonEditB if the architecture prefers its features over upstream continuity.
- **Plan explicit accessibility work** (FR-093, NFR-022): AvalonEdit's UIA support is basic, so verify it with
  Narrator in POC-003.
- **Rejected:** Monaco/WebView2 (second stack, offline runtime servicing) and Scintilla (WinForms hosting).

## 3. MVVM
| Option | License | Status | Evidence |
|---|---|---|---|
| **CommunityToolkit.Mvvm** | MIT | 8.4.2 (2026-03-25). netstandard2.0/2.1, net8.0 (usable from net10) | [Verified: https://www.nuget.org/packages/CommunityToolkit.Mvvm] [Verified: https://github.com/CommunityToolkit/dotnet/blob/main/License.md] |

**Recommendation R-MVVM:** CommunityToolkit.Mvvm (source generators for `ObservableProperty`/`RelayCommand`).

## 4. Testing
| Tool | License | Latest | Notes | Evidence |
|---|---|---|---|---|
| xUnit v3 | Apache-2.0 | 4.0.2 (2026-10-09) | net472 / net8.0 builds. v2 is in maintenance mode | [Verified: https://www.nuget.org/packages/xunit.v3] [Verified: https://xunit.net/releases/] |
| NUnit | MIT | 5.0.0 (2026-09-27) | **Ships a net10.0 build** | [Verified: https://www.nuget.org/packages/NUnit] |
| MSTest | MIT | 4.5.1 (2026-10-07) | net8.0 / net9.0 builds | [Verified: https://www.nuget.org/packages/MSTest] |
| **FlaUI.UIA3** | MIT | 5.0.0 (2025-02-25) | UIA3 "works great for WPF". No release since February 2025 (repo active 2026-08) | [Verified: https://github.com/FlaUI/FlaUI] |
| WinAppDriver / Appium Windows Driver | MIT / Apache-2.0 | 2021 / deprecated | Unmaintained or deprecated | [Verified: https://github.com/appium/appium-windows-driver] |
| **coverlet** | MIT | collector 10.1.0 (2026-09-27) | `coverlet.MTP` for Microsoft.Testing.Platform. On .NET 10 the collector needs `TestingPlatformDotnetTestSupport=false` | [Verified: https://github.com/coverlet-coverage/coverlet] |
| Microsoft.Testing.Extensions.CodeCoverage | **Proprietary** (Microsoft Software License Terms) | 18.12.0 | Not open source. Acceptable only as a developer tool that is never shipped [Hypothesis] | [Verified: https://www.nuget.org/packages/Microsoft.Testing.Extensions.CodeCoverage/18.12.0/license] |

**Recommendation R-TEST:**
- Use **xUnit v3** as the default (it matches the stated preference, and data-driven `Theory` tests suit the
  SingleStepTests JSON).
- **NUnit 5** is an equal alternative and the only one that ships an explicit net10.0 build. The ADR decides.
- **FlaUI.UIA3** for WPF UI automation (AC-043/044 smoke tests). Release cadence is a monitored risk.
- **coverlet** (MIT) for NFR-021. Avoid the proprietary coverage extension in the repository.

## 5. Unverified items and resolution
| Item | Resolution |
|---|---|
| AvalonEdit 6.3.1 (net8 build) on net10.0-windows | POC-003 |
| AvalonEdit with Narrator: what a screen reader actually reads | POC-003 manual check |
| Whether upstream SearchPanel supports replace | Read the source during POC-003 |
| xUnit v3 / MSTest official .NET 10 support statements | Confirm when the SDK is installed (dependent on BLK-001) |
| WebView2 Runtime redistribution terms | Not needed (Monaco rejected) |

## Changelog
- 2026-10-26 v0.1: Initial research (SDLC-004).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:59 +0500). Content unchanged from v0.1.
