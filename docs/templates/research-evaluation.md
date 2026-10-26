# Research — <Topic, e.g. CPU execution engine options>

| Field | Value |
|---|---|
| Document | Research: <topic> |
| Status | Draft |
| Version | 0.1 |
| Last updated | YYYY-MM-DD |
| Depends on | docs/01-requirements/requirements.md v<x> |

## 1. Question
<Decision this research informs; linked OQ-### and FR/NFR IDs.>

## 2. Evaluation criteria
| Criterion | Weight | Derived from |
|---|---|---|
| License compatible with distribution model | Gate (pass/fail) | NFR-…, OQ-007 |
| Windows 10/11 + target .NET support | Gate | CON-… |
| Works fully offline | Gate | NFR-… |
| Maintenance activity | | |
| Architecture coverage (8086 now, x86/x64 later) | | |
| Integration complexity from C# | | |
| Debug/introspection hooks (step, registers, memory, breakpoints) | | |
| Determinism / testability | | |

## 3. Options
### RES-001 <Option name>
| Attribute | Finding | Evidence |
|---|---|---|
| What it is | | [Verified: URL, YYYY-MM-DD] |
| License (exact SPDX id) and obligations | | |
| Latest release / last activity | | |
| Supported CPU architectures and modes | | |
| Windows / .NET integration path | | |
| Offline | | |
| Debug hooks available | | |
| Known limitations | | |
| Unknowns to validate (POC candidates) | | [Hypothesis] |

## 4. Comparison
| Criterion | RES-001 | RES-002 | … |
|---|---|---|---|

## 5. Recommendation
Recommended option, why, rejected options and why, residual risks, proposed `POC-###`.
Recommendation ≠ decision. The decision is recorded as an ADR in the architecture phase.

## 6. Unverified items
List every `[Unverified]` or `[Hypothesis]` claim and how it will be resolved.

## Changelog
- YYYY-MM-DD v0.1: Initial draft.
