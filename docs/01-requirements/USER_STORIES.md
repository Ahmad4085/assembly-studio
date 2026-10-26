# User Stories

| Field | Value |
|---|---|
| Document | User stories |
| Status | Approved |
| Version | 1.0 |
| Last updated | 2026-10-26 |
| Depends on | `docs/00-project-overview/project-overview.md` v0.3. `DECISIONS.md` D-006 to D-009 |
| Requirements set | Approved as one set together with `PRODUCT_REQUIREMENTS.md` |

Personas: `PRODUCT_REQUIREMENTS.md` §4. Format: *As a [user], I want [capability], so that [benefit].*

## PER-1 Beginner
| ID | Story | Goals | Requirements | Acceptance criteria |
|---|---|---|---|---|
| US-001 | As a Beginner, I want to install the application and start writing code without an account or internet connection, so that I can learn wherever I am. | PG-5 | FR-001, FR-079, FR-083, FR-084, FR-086 | AC-001, AC-034, AC-039, AC-040 |
| US-002 | As a Beginner, I want to open a built-in sample program and run it, so that I see a working program before writing my own. | PG-6, PG-3 | FR-033, FR-034, FR-035, FR-051, FR-063, FR-068, FR-069 | AC-021, AC-022, AC-027, AC-035, AC-045, AC-047 |
| US-003 | As a Beginner, I want to execute one instruction at a time and see which registers and flags changed, so that I understand what each instruction does. | PG-1 | FR-046, FR-047, FR-048, FR-049, FR-053, FR-056 | AC-024, AC-025, AC-026, AC-052 |
| US-004 | As a Beginner, I want plain-language explanations of registers, flags, memory, the stack and instructions inside the application, so that I can learn the concepts offline. | PG-6 | FR-070, FR-071, FR-072 | AC-036, AC-037 |
| US-005 | As a Beginner, I want errors in my program to be explained and to point to the line that caused them, so that I can fix my mistakes myself. | PG-2 | FR-018, FR-021, FR-029, FR-065, FR-066 | AC-013, AC-014, AC-055 |
| US-021 | As a Beginner / Student / Developer / Instructor, I want to use the application by keyboard and screen reader, with readable contrast, so that I can use it regardless of disability. | PG-4 | FR-091, FR-092, FR-093, FR-094 | AC-043, AC-044 |
| US-023 | As a Beginner, I want to upgrade or uninstall cleanly without losing my source files, so that I can manage the application safely. | PG-5 | FR-085, FR-088 | AC-041 |

## PER-2 Student
| ID | Story | Goals | Requirements | Acceptance criteria |
|---|---|---|---|---|
| US-006 | As a Student, I want to create, save and reopen my exercise files reliably, so that I never lose my work. | PG-2 | FR-002, FR-003, FR-004, FR-005, FR-008, FR-009, FR-019, FR-020 | AC-002, AC-003, AC-004, AC-005, AC-007, AC-008, AC-012 |
| US-007 | As a Student, I want to assemble my program with one command and see whether it succeeded and how large it is, so that I know the program is valid before running it. | PG-2 | FR-025, FR-026, FR-028, FR-031 | AC-017, AC-019, AC-020 |
| US-008 | As a Student, I want my program to read keyboard input and print results in an integrated console, so that I can complete typical input/output exercises. | PG-3 | FR-039, FR-041, FR-063, FR-064 | AC-022, AC-023, AC-045 |
| US-009 | As a Student, I want to inspect memory and the stack while my program is paused, so that I can check my data and procedure calls. | PG-1 | FR-054, FR-055, FR-056 | AC-025, AC-026 |
| US-010 | As a Student, I want to be told clearly when I use an instruction, service or feature that is not supported, so that I don't mistake a tool limit for my own bug. | PG-4 | FR-027, FR-032, FR-036, FR-037, FR-038, FR-040, FR-045 | AC-018, AC-028, AC-029, AC-031, AC-051 |
| US-022 | As a Student, I want my preferences to persist, and a broken settings file not to stop the application, so that my setup stays stable. | PG-5 | FR-077, FR-078, FR-080, FR-081, FR-082 | AC-039, AC-053, AC-054 |
| US-026 | As a Student, I want (Post-MVP) to run .EXE and BIOS-screen programs, split programs into several files and use checked exercises, so that I can work on larger course assignments. | PG-6 | FR-010, FR-043, FR-044, FR-073 | — |

## PER-3 Developer
| ID | Story | Goals | Requirements | Acceptance criteria |
|---|---|---|---|---|
| US-011 | As a Developer, I want to set breakpoints and run until one is hit, so that I can get to the code I'm interested in without stepping through everything. | PG-3 | FR-051, FR-052 | AC-022, AC-027, AC-032 |
| US-012 | As a Developer, I want to step over procedure calls and interrupts, so that I can debug at the level of detail I need. | PG-3 | FR-050 | AC-033 |
| US-013 | As a Developer, I want to see the history of recently executed instructions, so that I understand how the program reached its current state. | PG-1 | FR-058 | AC-049 |
| US-014 | As a Developer, I want multiple tabs, find and replace, go to line, indentation support and keyboard shortcuts, so that I can edit without leaving the keyboard. | PG-2 | FR-006, FR-007, FR-011, FR-012, FR-013, FR-014, FR-015, FR-016, FR-017 | AC-006, AC-007, AC-009, AC-010, AC-011 |
| US-015 | As a Developer, I want to configure which assembler is used and see tool versions, so that I control my toolchain and can report problems precisely. | PG-2 | FR-023, FR-024, FR-030, FR-075, FR-076 | AC-015, AC-016, AC-019, AC-038 |
| US-016 | As a Developer, I want a program stuck in an endless loop or producing endless output never to freeze the IDE, so that I can always stop it and carry on working. | PG-4 | FR-030, FR-042, FR-057, FR-067, FR-095, FR-096 | AC-016, AC-019, AC-030, AC-045, AC-048 |
| US-024 | As a Developer, I want to see the version, release notes and third-party licenses, so that I know what I'm running and what it contains. | PG-5 | FR-087, FR-090 | AC-038, AC-042 |
| US-025 | As a Developer, I want (Post-MVP) to step backwards, run to the cursor, edit state while paused and get memory-write warnings, so that I can explore program behavior in more depth. | PG-1 | FR-059, FR-060, FR-061, FR-062 | — |

## PER-4 Instructor
| ID | Story | Goals | Requirements | Acceptance criteria |
|---|---|---|---|---|
| US-017 | As an Instructor, I want to enlarge code and debug panels, so that students can read them on a projector. | PG-6 | FR-022, FR-074 | AC-046 |
| US-018 | As an Instructor, I want to reset a program and replay the same steps with the same results, so that I can repeat a demonstration reliably. | PG-1 | FR-035 | AC-022, AC-027, AC-047 |
| US-019 | As an Instructor, I want to be sure the displayed values are actual emulated CPU state, not illustrations, so that I teach accurate behavior. | PG-4 | FR-072 | AC-037 |
| US-020 | As an Instructor, I want to run demonstrations with no internet connection, so that classroom network problems don't disrupt lessons. | PG-5 | FR-001 | AC-001, AC-034 |

## Changelog
- 2026-10-26 v0.1: Initial draft (SDLC-003, Claude Code).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:11 +0500: "approve requirements v0.1 and D-010"). Requirement statuses PROPOSED → APPROVED. NEEDS_RESEARCH unchanged. Content otherwise identical to v0.1.
