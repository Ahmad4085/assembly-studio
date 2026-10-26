# Test Plan

| Field | Value |
|---|---|
| Document | Test plan |
| Status | Draft |
| Version | 0.1 |
| Last updated | YYYY-MM-DD |
| Depends on | requirements.md v<x>, architecture.md v<x>, backlog.md v<x> |

## 1. Scope
What is tested in this iteration (milestone, components) and what is explicitly not.

## 2. Test levels and tools
| Level | Tool / framework | Project | Automated? |
|---|---|---|---|
| Unit (CPU semantics, flags, decoding) | | `tests/…` | Yes |
| Integration (assembler → loader → engine) | | | |
| Reference/golden comparison | | | |
| UI / view model | | | |
| Manual exploratory | | | No |

## 3. Test inventory
| ID | Title | Level | Verifies (FR/NFR/TASK) | Deterministic inputs | Expected result |
|---|---|---|---|---|---|
| TC-001 | | | | | |

## 4. Environment
OS, .NET SDK version, assembler version, hardware notes. Record the actual versions at execution time.

## 5. Entry and exit criteria
Entry: build succeeds. Exit: all tests for P0 requirements executed, no unexplained failures.

## 6. Known limitations
Areas not testable yet and why.

## Changelog
- YYYY-MM-DD v0.1: Initial draft.
