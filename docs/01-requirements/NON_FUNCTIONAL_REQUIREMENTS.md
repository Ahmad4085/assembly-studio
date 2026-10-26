# Non-Functional Requirements

| Field | Value |
|---|---|
| Document | Non-functional requirements |
| Status | Approved |
| Version | 2.0 |
| Last updated | 2026-10-26 |
| Depends on | `docs/00-project-overview/project-overview.md` v0.3. `DECISIONS.md` D-006 to D-009 |
| Requirements set | Approved as one set together with `PRODUCT_REQUIREMENTS.md` |

Targets marked `PROPOSED` are starting values, not measured results. Each one names how it will be validated. No benchmark has been run.

| ID | Category | Requirement | Target | Basis | Verification | Priority | Status | Goals | Acceptance criteria |
|---|---|---|---|---|---|---|---|---|---|
| NFR-001 | Performance | Editor input latency | Keystroke-to-render <= 50 ms (95th percentile) in a 5,000-line file | PROPOSED | Automated UI timing test on the reference machine (OQ-014) | P1 | APPROVED | PG-2 | AC-056 |
| NFR-002 | Performance | Step latency | Step command to all visible debug panels updated <= 100 ms (95th percentile) | PROPOSED | Automated timing test, POC during research | P0 | APPROVED | PG-1 | AC-024 |
| NFR-003 | Performance | Free-run execution speed | >= 1,000,000 emulated instructions per second with UI updates throttled | PROPOSED | Benchmark on the reference machine after engine selection | P1 | NEEDS_RESEARCH | PG-3 | AC-056 |
| NFR-004 | Performance | UI responsiveness during build and run | UI thread never blocked > 100 ms. Pause/Stop take effect <= 500 ms after the command | PROPOSED | Automated test with an endless-loop program and a hanging assembler stub | P0 | APPROVED | PG-3, PG-4 | AC-030 |
| NFR-005 | Performance | Startup time | Cold start to editable main window <= 3 s | PROPOSED | Timed launch on the reference machine | P1 | APPROVED | PG-2 | AC-001, AC-056 |
| NFR-006 | Performance | Build turnaround | Build result shown <= 2 s for a 1,000-line source (excluding the first assembler launch after boot) | PROPOSED | Timed build test | P1 | APPROVED | PG-2 | AC-056 |
| NFR-007 | Resource use | Memory use | Working set <= 300 MB with one 1,000-line file and an active debug session. All history buffers bounded | PROPOSED | Measurement during the soak test | P1 | APPROVED | PG-4 | AC-056 |
| NFR-008 | Offline | Offline operation | All P0 and P1 features work with no network connectivity. Core workflows make no network requests | FIRM | Full workflow test with networking disabled, network monitor and code review | P0 | APPROVED | PG-5 | AC-001, AC-034 |
| NFR-009 | Privacy | Local data, no telemetry | No telemetry, no account and no data transmitted. User files and settings stay on the local machine | FIRM | Code review, network monitor during tests | P0 | APPROVED | PG-5 | AC-034 |
| NFR-010 | Stability | Fault containment | Faults in emulated programs, assembler failures or invalid input never terminate the application. 0 application crashes in the automated suite plus a 30-minute soak/fuzz run | PROPOSED | Automated fault-injection, soak and fuzz tests | P0 | APPROVED | PG-4 | AC-019, AC-030 |
| NFR-011 | Stability | Crash recovery | After an unexpected termination, at most the last 60 s of edits are lost (autosave/recovery) | PROPOSED | Kill-process test | P1 | APPROVED | PG-4 | AC-050 |
| NFR-012 | Stability | Local diagnostics log | Errors logged locally with rotation (<= 10 MB total). Never uploaded | PROPOSED | Inspection and test | P1 | APPROVED | PG-4 | AC-063 |
| NFR-013 | Security | Program isolation | User programs execute only in the emulator, with no access to host files, processes, devices or network (DOS file/device services unsupported in MVP) | FIRM | Architecture review, tests calling unsupported services | P0 | APPROVED | PG-4 | AC-029 |
| NFR-014 | Security | Safe external process execution | Assembler invoked without a shell, with an argument list, in an isolated per-build temp directory, with a timeout and output cap. All paths validated | FIRM | Code review, tests with malicious file names and a hanging assembler stub | P0 | APPROVED | PG-4 | AC-019 |
| NFR-015 | Resource use | Execution resource limits | Emulated memory exactly 1 MiB. Instruction budget per run. Console buffer cap (PROPOSED 1 MB, oldest output trimmed). Execution-history cap | PROPOSED | Automated limit tests | P0 | APPROVED | PG-4 | AC-030, AC-045 |
| NFR-016 | Testability | Deterministic execution | The same image and the same input sequence produce an identical CPU state trace | FIRM | Automated trace-comparison test | P0 | APPROVED | PG-1, PG-4 | AC-047 |
| NFR-017 | Correctness | Instruction semantics verified | Every supported instruction has automated tests covering its operand forms and all defined flag results, with expected values from CPU documentation or a reference test suite | FIRM | Test report and coverage mapping | P0 | APPROVED | PG-1, PG-4 | AC-058 |
| NFR-018 | Correctness | Visualization integrity | When paused, displayed values equal engine state. Production code paths contain no mock or sample execution data | FIRM | Automated view-model-versus-engine comparison, code review | P0 | APPROVED | PG-4 | AC-025, AC-037 |
| NFR-019 | Maintainability | Modularity | Assembler integration, CPU engine, loader/runtime, OS services, debugger, UI and visualization are separate components behind interfaces. The engine can be replaced without UI changes | FIRM | Architecture review, dependency analysis | P0 | APPROVED | PG-4 | AC-057 |
| NFR-020 | Testability | Headless testing | Engine, runtime and assembler integration are testable without UI, and the full automated suite runs with one command | FIRM | Build/test pipeline inspection | P0 | APPROVED | PG-4 | AC-057 |
| NFR-021 | Maintainability | Code quality gates | Zero compiler warnings in core libraries. Line coverage of engine and runtime >= 85% | PROPOSED | Build output and coverage report | P1 | APPROVED | PG-4 | AC-057 |
| NFR-022 | Accessibility | Accessibility baseline | Contrast >= 4.5:1 for text and >= 3:1 for UI components in all themes (WCAG 2.2 AA levels). Full keyboard operability of essential workflows | FIRM | Contrast measurement, keyboard-only walkthrough | P0 | APPROVED | PG-4 | AC-043, AC-044 |
| NFR-023 | Usability | Beginner first success | >= 80% of first-time participants (at least 5) open a sample, build, run and step within 5 minutes without outside help | PROPOSED | Moderated usability session | P1 | APPROVED | PG-1, PG-6 | AC-062 |
| NFR-024 | Usability | Error message quality | 100% of user-facing errors state what happened, where and a next step | FIRM | Review checklist over the message catalog | P1 | APPROVED | PG-2 | AC-013, AC-055 |
| NFR-025 | Compatibility | Supported Windows versions | Supported and tested: Windows 11 24H2 or later (x64) and Windows 10 Enterprise LTSC 21H2 (x64). Not supported: Windows 10 22H2 Home/Pro (may work, untested) (D-011) | FIRM | Install-and-run test on Windows 11 24H2+ and Windows 10 Enterprise LTSC 21H2 | P0 | APPROVED | PG-5 | AC-040 |
| NFR-026 | Compatibility | Display scaling | Correct layout at 100%, 125%, 150% and 200% scaling and per-monitor DPI. Minimum window 1024x768 | PROPOSED | Manual check at each scale factor | P1 | APPROVED | PG-4 | AC-059 |
| NFR-027 | Installability | Install and uninstall | Offline install on a clean machine completes <= 2 min. Uninstall removes all program files and leaves user files untouched | PROPOSED | Clean-VM install/uninstall test | P0 | APPROVED | PG-5 | AC-040, AC-041 |
| NFR-028 | Installability | Install footprint | Installed size <= 300 MB | PROPOSED | Measurement of installed folder | P2 | APPROVED | PG-5 | — |
| NFR-029 | Reliability | Data integrity on save | Saving is atomic, so no partial or corrupted file exists after a failed save | FIRM | Fault-injection test | P0 | APPROVED | PG-2 | AC-004 |
| NFR-030 | Licensing | Dependency licensing | Every distributed component is compatible with a permissive open-source release (D-007), and its license and obligations are recorded before inclusion. Notices are shipped. No paid dependencies | FIRM | License inventory review per release | P0 | APPROVED | PG-5 | AC-042 |
| NFR-031 | Supply chain | Dependency hygiene | Dependencies come from official sources, are version-pinned and have a recorded maintenance assessment. Bundled binaries are checksum-verified. An SBOM is produced per release | PROPOSED | Release checklist inspection | P1 | APPROVED | PG-5 | AC-061 |
| NFR-032 | Localizability | Localizable UI | All user-facing strings live in resource files. English only in the MVP | FIRM | Code inspection | P1 | APPROVED | PG-6 | AC-060 |
| NFR-033 | Documentation | Offline user guide | An offline guide documents the supported scope, limitations and keyboard shortcuts | FIRM | Inspection | P1 | APPROVED | PG-6 | AC-051 |

## Changelog
- 2026-10-26 v0.1: Initial draft (SDLC-003, Claude Code).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:11 +0500: "approve requirements v0.1 and D-010"). Requirement statuses PROPOSED → APPROVED. NEEDS_RESEARCH unchanged. Content otherwise identical to v0.1.
- 2026-10-26 v2.0: **Substantive change (SDLC-005)** applying user-approved decisions D-011 (supported Windows: CON-001, NFR-025, AC-040, ASM-001, OQ-010) and D-012 (INT 21h AH=00h: FR-039, FR-041, new AC-064, OQ-022). Changed requirements are `PROPOSED` until re-approval. Set status `Awaiting approval`.
- 2026-10-26 v2.0: **Approved** by the user (2026-10-26 18:18 +0500: "approve requirements v2.0"). FR-039, FR-041, NFR-025 → APPROVED.
