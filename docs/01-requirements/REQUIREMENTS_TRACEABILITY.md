# Requirements Traceability Matrix

| Field | Value |
|---|---|
| Document | Requirements traceability matrix |
| Status | Approved |
| Version | 2.17 |
| Last updated | 2026-10-30 |
| Depends on | `docs/00-project-overview/project-overview.md` v0.3. `DECISIONS.md` D-006 to D-009 |
| Requirements set | Approved as one set together with `PRODUCT_REQUIREMENTS.md` |

Chain: **Product goal → User story → Requirement → Acceptance criterion → Test case.** This is the
project's single traceability matrix. Later phases add columns here rather than creating a new
matrix: ADRs (architecture), UI items (design), tasks (plan), test results (testing) and QA status.

Test case `NOT CREATED` and status `NOT RUN` mean no test exists yet. They must not be read as passed.

| Goal(s) | User stories | Requirement | Priority | Status | Acceptance criteria | ADRs | UI items | Tasks | Test cases | Test status | QA status |
|---|---|---|---|---|---|---|---|---|---|---|---|
| PG-5 | US-001, US-020 | FR-001 | P0 | APPROVED | AC-001, AC-034 | ADR-0007 | UI-020 | TASK-062 | TC-051 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-006 | FR-002 | P0 | APPROVED | AC-002 | ADR-0007 | UI-003, UI-020 | TASK-062 | TC-054 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-006 | FR-003 | P0 | APPROVED | AC-002 | ADR-0007 | UI-003, UI-020 | TASK-062 | TC-054 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-006 | FR-004 | P0 | APPROVED | AC-003, AC-004 | ADR-0007 | UI-002, UI-019 | TASK-062 | TC-048, TC-054 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-006 | FR-005 | P0 | APPROVED | AC-005 | ADR-0007 | UI-019 | TASK-062 | TC-050, TC-054 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-014 | FR-006 | P1 | APPROVED | AC-006 | ADR-0007 | UI-003 | TASK-062 | TC-050, TC-054 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-014 | FR-007 | P1 | APPROVED | AC-007 | ADR-0007 | UI-019, UI-020 | TASK-062 | TC-054 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-006 | FR-008 | P0 | APPROVED | AC-004, AC-007, AC-008 | ADR-0007 | UI-019 | TASK-062 | TC-048, TC-054 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-006 | FR-009 | P2 | APPROVED | — | ADR-0007 | UI-003 | — (post-MVP) | NOT CREATED | NOT RUN | QA 2026-11-04-2: Out of scope |
| PG-6 | US-026 | FR-010 | P2 | NEEDS_RESEARCH | — | ADR-0005, ADR-0007 | UI-003 | — (post-MVP) | NOT CREATED | NOT RUN | QA 2026-11-04-2: Out of scope |
| PG-2 | US-014 | FR-011 | P0 | APPROVED | AC-009 | ADR-0006 | UI-003 | TASK-064 | TC-075 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-014 | FR-012 | P0 | APPROVED | AC-009 | ADR-0006 | UI-003 | TASK-064 | TC-075 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-014 | FR-013 | P0 | APPROVED | AC-010 | ADR-0006 | UI-003 | TASK-064 | TC-075 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-014 | FR-014 | P1 | APPROVED | AC-010 | ADR-0006 | UI-003 | TASK-064 | TC-075 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-014 | FR-015 | P0 | APPROVED | AC-011 | ADR-0006 | UI-004 | TASK-065 | TC-091 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-014 | FR-016 | P1 | APPROVED | AC-011 | ADR-0006 | UI-004 | TASK-065 | TC-091 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-014 | FR-017 | P1 | APPROVED | AC-011 | ADR-0006 | UI-004 | TASK-065 | TC-091 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-005 | FR-018 | P0 | APPROVED | AC-013 | ADR-0006 | UI-003, UI-011 | TASK-066, TASK-116 | TC-026, TC-077, TC-078, TC-079 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-006 | FR-019 | P1 | APPROVED | AC-012 | ADR-0006, ADR-0007 | UI-003, UI-016 | TASK-062 | TC-053, TC-054 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-006 | FR-020 | P1 | APPROVED | AC-012 | ADR-0006, ADR-0007 | UI-003, UI-016 | TASK-062 | TC-053, TC-054 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-005 | FR-021 | P1 | APPROVED | AC-014 | ADR-0005, ADR-0006 | UI-003, UI-011 | TASK-041 | TC-026 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-6 | US-017 | FR-022 | P1 | APPROVED | AC-046 | ADR-0006, ADR-0007 | UI-003, UI-022 | TASK-064 | TC-075 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-015 | FR-023 | P0 | NEEDS_RESEARCH | AC-015 | ADR-0005 | UI-017, UI-018 | TASK-005, TASK-040 | TC-030, TC-061 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-015 | FR-024 | P0 | APPROVED | AC-015, AC-016 | ADR-0005, ADR-0007 | UI-011, UI-016 | TASK-040 | TC-030, TC-061 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-007 | FR-025 | P0 | APPROVED | AC-017, AC-019 | ADR-0005 | UI-002 | TASK-040 | TC-030, TC-044, TC-078, TC-079 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-007 | FR-026 | P0 | APPROVED | AC-017 | ADR-0005 | UI-011 | TASK-040 | TC-030, TC-044, TC-078, TC-079 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-010 | FR-027 | P0 | NEEDS_RESEARCH | AC-018 | ADR-0005 | UI-011 | TASK-042 | TC-026, TC-028 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-007 | FR-028 | P0 | APPROVED | AC-017 | ADR-0005 | UI-016 | TASK-043, TASK-080 | TC-044, TC-078, TC-079 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-005 | FR-029 | P0 | APPROVED | AC-013 | ADR-0005 | UI-011 | TASK-041 | TC-026, TC-077, TC-078, TC-079 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2, PG-4 | US-015, US-016 | FR-030 | P0 | APPROVED | AC-016, AC-019 | ADR-0005 | UI-011 | TASK-040, TASK-041 | TC-030 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-007 | FR-031 | P0 | APPROVED | AC-020 | ADR-0005 | UI-011 | TASK-041, TASK-115 | TC-026, TC-095 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-010 | FR-032 | P1 | APPROVED | AC-051 | ADR-0002, ADR-0005 | UI-015 | TASK-093 | TC-029, TC-043, TC-056 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-3, PG-6 | US-002 | FR-033 | P0 | NEEDS_RESEARCH | AC-021 | ADR-0002, ADR-0003, ADR-0004 | — | TASK-010, TASK-011, TASK-030 | TC-018, TC-044 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-3, PG-6 | US-002 | FR-034 | P0 | NEEDS_RESEARCH | AC-021 | ADR-0004 | UI-005 | TASK-031 | TC-018, TC-044 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1, PG-3, PG-6 | US-002, US-018 | FR-035 | P0 | APPROVED | AC-022, AC-027, AC-047 | ADR-0007 | UI-002 | TASK-050, TASK-080 | TC-006, TC-019, TC-024, TC-036, TC-037, TC-044, TC-080 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-010 | FR-036 | P0 | NEEDS_RESEARCH | AC-028 | ADR-0003 | — | TASK-011, TASK-012, TASK-013, TASK-014, TASK-015, TASK-016, TASK-017, TASK-018, TASK-020 | TC-003, TC-007, TC-009, TC-016, TC-083 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-010 | FR-037 | P0 | APPROVED | AC-028 | ADR-0003, ADR-0007 | UI-007, UI-012 | TASK-011, TASK-018, TASK-083 | TC-001, TC-003, TC-009, TC-011, TC-083 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-010 | FR-038 | P0 | APPROVED | AC-031 | ADR-0003, ADR-0004 | UI-012 | TASK-015, TASK-016, TASK-032, TASK-083 | TC-012, TC-020, TC-044, TC-083, TC-084 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-3 | US-008 | FR-039 | P0 | APPROVED | AC-022, AC-023, AC-064 | ADR-0004 | UI-010 | TASK-034, TASK-035, TASK-036, TASK-044 | TC-024, TC-044, TC-080 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-010 | FR-040 | P0 | APPROVED | AC-029 | ADR-0004 | UI-012 | TASK-032, TASK-034, TASK-083, TASK-114 | TC-020, TC-044, TC-083, TC-084 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-3 | US-008 | FR-041 | P0 | APPROVED | AC-022, AC-064 | ADR-0004, ADR-0007 | UI-012 | TASK-034, TASK-044, TASK-083 | TC-024, TC-044, TC-080, TC-083 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-016 | FR-042 | P0 | APPROVED | AC-030 | ADR-0004, ADR-0007 | UI-012 | TASK-050, TASK-083 | TC-036, TC-083, TC-088 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-6 | US-026 | FR-043 | P2 | NEEDS_RESEARCH | — | ADR-0004 | — | — (post-MVP) | NOT CREATED | NOT RUN | QA 2026-11-04-2: Out of scope |
| PG-6 | US-026 | FR-044 | P2 | NEEDS_RESEARCH | — | ADR-0004 | — | — (post-MVP) | NOT CREATED | NOT RUN | QA 2026-11-04-2: Out of scope |
| PG-4 | US-010 | FR-045 | P0 | APPROVED | AC-051 | ADR-0002 | UI-015 | TASK-018, TASK-093, TASK-113 | TC-007, TC-009, TC-029, TC-043, TC-056 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1 | US-003 | FR-046 | P0 | APPROVED | AC-025 | ADR-0003, ADR-0007 | UI-005 | TASK-051, TASK-081 | TC-001, TC-081 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1 | US-003 | FR-047 | P1 | APPROVED | AC-052 | ADR-0007 | UI-005 | TASK-081 | TC-081 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1 | US-003 | FR-048 | P0 | APPROVED | AC-025 | ADR-0003, ADR-0007 | UI-006 | TASK-013, TASK-014, TASK-015, TASK-051, TASK-081 | TC-001, TC-017, TC-081 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1 | US-003 | FR-049 | P0 | APPROVED | AC-024 | ADR-0003, ADR-0007 | UI-002 | TASK-050, TASK-080 | TC-034, TC-078, TC-083 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-3 | US-012 | FR-050 | P1 | APPROVED | AC-033 | ADR-0007 | UI-002 | TASK-052, TASK-080 | TC-035 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-3, PG-6 | US-002, US-011 | FR-051 | P0 | APPROVED | AC-022, AC-027 | ADR-0007 | UI-002 | TASK-050, TASK-080 | TC-024, TC-036, TC-044, TC-080 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-3 | US-011 | FR-052 | P1 | APPROVED | AC-032 | ADR-0005, ADR-0006, ADR-0007 | UI-002, UI-003, UI-014 | TASK-043, TASK-052, TASK-066, TASK-083 | TC-035, TC-077, TC-078, TC-083 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1 | US-003 | FR-053 | P0 | APPROVED | AC-024 | ADR-0003, ADR-0005, ADR-0006, ADR-0007 | UI-003, UI-007 | TASK-019, TASK-043, TASK-051, TASK-066, TASK-083 | TC-010, TC-034, TC-042, TC-077, TC-083 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1 | US-009 | FR-054 | P0 | APPROVED | AC-026 | ADR-0007 | UI-008 | TASK-051, TASK-082, TASK-111 | TC-034, TC-082 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1 | US-009 | FR-055 | P0 | APPROVED | AC-026 | ADR-0007 | UI-009 | TASK-051, TASK-082 | TC-034, TC-082 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1 | US-003, US-009 | FR-056 | P0 | APPROVED | AC-025, AC-026 | ADR-0003, ADR-0004, ADR-0007 | UI-005, UI-006, UI-008, UI-009 | TASK-030, TASK-051, TASK-081, TASK-082 | TC-001, TC-018, TC-034, TC-081, TC-082 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-016 | FR-057 | P1 | APPROVED | AC-048 | ADR-0007 | UI-005, UI-006, UI-016 | TASK-051, TASK-081 | TC-042, TC-081, TC-083, TC-090 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1 | US-013 | FR-058 | P1 | APPROVED | AC-049 | ADR-0003, ADR-0007 | UI-013 | TASK-019, TASK-051, TASK-083 | TC-010, TC-042, TC-083 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1 | US-025 | FR-059 | P2 | APPROVED | — | ADR-0004 | UI-008 | — (post-MVP) | NOT CREATED | NOT RUN | QA 2026-11-04-2: Out of scope |
| PG-1 | US-025 | FR-060 | P2 | NEEDS_RESEARCH | — | ADR-0003, ADR-0004, ADR-0007 | UI-013 | — (post-MVP) | NOT CREATED | NOT RUN | QA 2026-11-04-2: Out of scope |
| PG-1 | US-025 | FR-061 | P2 | APPROVED | — | ADR-0007 | UI-002 | — (post-MVP) | NOT CREATED | NOT RUN | QA 2026-11-04-2: Out of scope |
| PG-1 | US-025 | FR-062 | P2 | APPROVED | — | ADR-0007 | UI-005, UI-008 | — (post-MVP) | NOT CREATED | NOT RUN | QA 2026-11-04-2: Out of scope |
| PG-3, PG-6 | US-002, US-008 | FR-063 | P0 | APPROVED | AC-022, AC-045 | ADR-0004, ADR-0007 | UI-010 | TASK-033, TASK-084 | TC-019, TC-024, TC-040, TC-044, TC-073, TC-080 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-3 | US-008 | FR-064 | P0 | APPROVED | AC-023 | ADR-0004, ADR-0007 | UI-010 | TASK-033, TASK-035, TASK-036, TASK-053, TASK-084 | TC-024, TC-037, TC-044, TC-080 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-005 | FR-065 | P0 | APPROVED | AC-013 | ADR-0007 | UI-001, UI-012 | TASK-083 | TC-026, TC-077, TC-078, TC-079 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-005 | FR-066 | P1 | APPROVED | AC-055 | ADR-0005, ADR-0007 | UI-011, UI-012 | TASK-041, TASK-083 | TC-026, TC-084 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-016 | FR-067 | P0 | APPROVED | AC-030 | ADR-0007 | UI-001, UI-002 | TASK-050, TASK-080 | TC-036, TC-078, TC-088 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-3, PG-6 | US-002 | FR-068 | P0 | APPROVED | AC-035 | ADR-0002, ADR-0008 | UI-015 | TASK-091 | TC-046, TC-057, TC-067 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-3, PG-6 | US-002 | FR-069 | P1 | APPROVED | AC-035 | ADR-0002, ADR-0007 | UI-015, UI-020 | TASK-090 | TC-046, TC-057, TC-067 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-6 | US-004 | FR-070 | P1 | APPROVED | AC-036 | ADR-0002, ADR-0007 | UI-015 | TASK-092 | TC-057 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-6 | US-004 | FR-071 | P1 | APPROVED | AC-036 | ADR-0002, ADR-0007 | UI-015 | TASK-090, TASK-092 | TC-045, TC-057 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4, PG-6 | US-004, US-019 | FR-072 | P0 | APPROVED | AC-037 | ADR-0002, ADR-0007 | UI-015 | TASK-092 | TC-045, TC-081 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-6 | US-026 | FR-073 | P2 | APPROVED | — | ADR-0002 | UI-015 | — (post-MVP) | NOT CREATED | NOT RUN | QA 2026-11-04-2: Out of scope |
| PG-6 | US-017 | FR-074 | P2 | APPROVED | — | ADR-0007 | UI-022 | — (post-MVP) | NOT CREATED | NOT RUN | QA 2026-11-04-2: Out of scope |
| PG-2 | US-015 | FR-075 | P0 | APPROVED | AC-015 | ADR-0005, ADR-0007 | UI-017 | TASK-067 | TC-030, TC-061 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | US-015 | FR-076 | P0 | APPROVED | AC-038 | ADR-0007 | UI-018 | TASK-068 | TC-047 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-5 | US-022 | FR-077 | P1 | APPROVED | AC-053 | ADR-0007 | UI-017 | TASK-067 | TC-061 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-5 | US-022 | FR-078 | P1 | APPROVED | AC-054 | ADR-0007 | UI-017 | TASK-067 | TC-036, TC-061 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-5 | US-001 | FR-079 | P0 | APPROVED | AC-039 | ADR-0007, ADR-0009 | UI-017 | TASK-067 | TC-060, TC-061 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-5 | US-022 | FR-080 | P0 | APPROVED | AC-039 | ADR-0007 | UI-017 | TASK-067 | TC-060, TC-061 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-5 | US-022 | FR-081 | P0 | APPROVED | AC-039 | ADR-0007 | UI-019 | TASK-067 | TC-060, TC-061 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-5 | US-022 | FR-082 | P1 | APPROVED | AC-053 | ADR-0007 | UI-017 | TASK-067 | TC-061 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-5 | US-001 | FR-083 | P0 | APPROVED | AC-040 | ADR-0001, ADR-0009 | — | TASK-101, TASK-103 | NOT CREATED | NOT RUN | QA 2026-11-04-2: Not verified (waived, WVR-002) |
| PG-5 | US-001 | FR-084 | P1 | APPROVED | AC-040 | ADR-0009 | — | TASK-101 | TC-051, TC-064 | PASS 2026-11-04-1 | QA 2026-11-04-2: Not verified (waived, WVR-002) |
| PG-5 | US-023 | FR-085 | P0 | APPROVED | AC-041 | ADR-0009 | — | TASK-101, TASK-103 | NOT CREATED | NOT RUN | QA 2026-11-04-2: Not verified (waived, WVR-002) |
| PG-5 | US-001 | FR-086 | P0 | NEEDS_RESEARCH | AC-040 | ADR-0001, ADR-0009 | — | TASK-101 | NOT CREATED | NOT RUN | QA 2026-11-04-2: Not verified (waived, WVR-002) |
| PG-5 | US-024 | FR-087 | P0 | APPROVED | AC-038 | ADR-0007, ADR-0009 | UI-018 | TASK-068, TASK-101 | TC-047 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-5 | US-023 | FR-088 | P1 | APPROVED | AC-041 | ADR-0009 | — | TASK-101, TASK-103 | NOT CREATED | NOT RUN | QA 2026-11-04-2: Not verified (waived, WVR-002) |
| PG-5 | — | FR-089 | P3 | APPROVED | — | ADR-0009 | — | — (post-MVP) | NOT CREATED | NOT RUN | QA 2026-11-04-2: Out of scope |
| PG-5 | US-024 | FR-090 | P0 | APPROVED | AC-042 | ADR-0007, ADR-0009 | UI-018 | TASK-068, TASK-102 | TC-047, TC-097 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-021 | FR-091 | P0 | APPROVED | AC-043 | ADR-0006, ADR-0007 | UI-001, UI-002 | TASK-060, TASK-080, TASK-085, TASK-107 | TC-079, TC-087 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-021 | FR-092 | P0 | APPROVED | AC-043 | ADR-0006, ADR-0007 | UI-001 | TASK-060, TASK-085, TASK-107 | TC-087 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-021 | FR-093 | P1 | APPROVED | AC-044 | ADR-0006, ADR-0007 | UI-001 | TASK-069, TASK-085, TASK-111 | TC-085, TC-086 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-021 | FR-094 | P1 | APPROVED | AC-044 | ADR-0006, ADR-0007 | UI-001 | TASK-061, TASK-112 | NOT CREATED | NOT RUN | QA 2026-11-04-2: Verified |
| PG-4 | US-016 | FR-095 | P0 | APPROVED | AC-019 | ADR-0005, ADR-0007 | UI-011, UI-012 | TASK-040 | TC-030 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | US-016 | FR-096 | P0 | APPROVED | AC-030, AC-045 | ADR-0003, ADR-0004, ADR-0007 | UI-010, UI-012 | TASK-054, TASK-084 | TC-019, TC-036, TC-040, TC-073, TC-080, TC-088 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1 | US-009 | FR-097 | P1 | APPROVED | AC-065 | ADR-0005, ADR-0007 | UI-023 | TASK-106, TASK-111, TASK-113 | TC-090 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | — | NFR-001 | P1 | APPROVED | AC-056 | ADR-0006 | UI-003 | TASK-064, TASK-086 | TC-008, TC-027, TC-075, TC-076 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1 | — | NFR-002 | P0 | APPROVED | AC-024 | ADR-0003, ADR-0007 | UI-005 | TASK-020, TASK-051, TASK-086 | TC-008, TC-034, TC-042, TC-083, TC-088 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-3 | — | NFR-003 | P1 | NEEDS_RESEARCH | AC-056 | ADR-0003 | — | TASK-020 | TC-008, TC-027 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-3, PG-4 | — | NFR-004 | P0 | APPROVED | AC-030 | ADR-0007 | UI-002 | TASK-050, TASK-086 | TC-036, TC-088 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-2 | — | NFR-005 | P1 | APPROVED | AC-001, AC-056 | ADR-0007 | UI-020 | TASK-060, TASK-086, TASK-108, TASK-113 | TC-008, TC-027, TC-064, TC-065, TC-089 | PASS 2026-11-04-1 (warm, NFR-005 v2.1: median 1,780 ms, max 2,853 ms (the first of 5) on the published application, TC-089). Cold start 7,752 ms after a restart reported, no target (known limitation, DEF-023) | QA 2026-11-04-2: Verified |
| PG-2 | — | NFR-006 | P1 | APPROVED | AC-056 | ADR-0005 | — | TASK-040, TASK-086 | TC-008, TC-027, TC-088 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | — | NFR-007 | P1 | APPROVED | AC-056 | ADR-0004, ADR-0007 | — | TASK-086 | TC-008, TC-027, TC-088, TC-089 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-5 | — | NFR-008 | P0 | APPROVED | AC-001, AC-034 | ADR-0001, ADR-0005, ADR-0007, ADR-0009 | — | TASK-103 | NOT CREATED | NOT RUN | QA 2026-11-04-2: Out of scope |
| PG-5 | — | NFR-009 | P0 | APPROVED | AC-034 | ADR-0007 | — | TASK-067 | TC-058 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | — | NFR-010 | P0 | APPROVED | AC-019, AC-030 | ADR-0004, ADR-0005, ADR-0007 | — | TASK-054 | TC-030, TC-036, TC-038, TC-039, TC-088 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | — | NFR-011 | P1 | APPROVED | AC-050 | ADR-0007 | UI-021 | TASK-063 | TC-049, TC-052 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | — | NFR-012 | P1 | APPROVED | AC-063 | ADR-0007 | — | TASK-068 | TC-058, TC-060 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | — | NFR-013 | P0 | APPROVED | AC-029 | ADR-0003, ADR-0004 | — | TASK-032, TASK-054 | TC-020, TC-044, TC-083 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | — | NFR-014 | P0 | APPROVED | AC-019 | ADR-0005 | — | TASK-040 | TC-030 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | — | NFR-015 | P0 | APPROVED | AC-030, AC-045 | ADR-0004, ADR-0007 | — | TASK-030, TASK-033, TASK-050, TASK-054 | TC-019, TC-036, TC-040, TC-073, TC-080, TC-088 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1, PG-4 | — | NFR-016 | P0 | APPROVED | AC-047 | ADR-0002, ADR-0003, ADR-0004, ADR-0007, ADR-0008 | — | TASK-020, TASK-033, TASK-044, TASK-053 | TC-006, TC-019, TC-037, TC-044 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1, PG-4 | — | NFR-017 | P0 | APPROVED | AC-058 | ADR-0003, ADR-0008 | — | TASK-003, TASK-012, TASK-013, TASK-014, TASK-015, TASK-016, TASK-017, TASK-018, TASK-020 | TC-007, TC-009, TC-016 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | — | NFR-018 | P0 | APPROVED | AC-025, AC-037 | ADR-0007 | UI-005, UI-006, UI-008, UI-009 | TASK-010, TASK-051, TASK-081, TASK-082, TASK-111 | TC-001, TC-042, TC-045, TC-081, TC-082, TC-090 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | — | NFR-019 | P0 | APPROVED | AC-057 | ADR-0002, ADR-0003, ADR-0007, ADR-0008 | — | TASK-001, TASK-002, TASK-010 | TC-071, TC-072 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | — | NFR-020 | P0 | APPROVED | AC-057 | ADR-0002, ADR-0008 | — | TASK-001, TASK-044 | TC-071, TC-072 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-4 | — | NFR-021 | P1 | APPROVED | AC-057 | ADR-0002, ADR-0008 | — | TASK-001, TASK-004, TASK-107 | Build output + `build/coverage.ps1` (no TC) | PASS 2026-11-04-1: Release build 0 warnings; line coverage Engine 99.6%, Runtime 95.3% (target 85%); the coverage run completed with no failure | QA 2026-11-04-2: Verified |
| PG-4 | — | NFR-022 | P0 | APPROVED | AC-043, AC-044 | ADR-0006, ADR-0007, ADR-0008 | UI-001 | TASK-061, TASK-069, TASK-085, TASK-112 | TC-066, TC-085, TC-087 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-1, PG-6 | — | NFR-023 | P1 | APPROVED | AC-062 | ADR-0002, ADR-0007 | UI-015, UI-020 | TASK-090, TASK-105 | NOT CREATED | NOT RUN | QA 2026-11-04-2: Not verified |
| PG-2 | — | NFR-024 | P1 | APPROVED | AC-013, AC-055 | ADR-0005, ADR-0007 | UI-011, UI-012, UI-019 | TASK-041, TASK-104, TASK-114 | TC-026, TC-077, TC-078, TC-079, TC-084 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-5 | — | NFR-025 | P0 | APPROVED | AC-040 | ADR-0001, ADR-0009 | — | TASK-006, TASK-070, TASK-103 | NOT CREATED | NOT RUN | QA 2026-11-04-2: Out of scope |
| PG-4 | — | NFR-026 | P1 | APPROVED | AC-059 | ADR-0001, ADR-0007 | UI-001 | TASK-060, TASK-061, TASK-112 | TC-059, TC-064 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-5 | — | NFR-027 | P0 | APPROVED | AC-040, AC-041 | ADR-0009 | — | TASK-101, TASK-103 | NOT CREATED | NOT RUN | QA 2026-11-04-2: Not verified (waived, WVR-002) |
| PG-5 | — | NFR-028 | P2 | APPROVED | — | ADR-0009 | — | TASK-101 | NOT CREATED | NOT RUN | QA 2026-11-04-2: Verified |
| PG-2 | — | NFR-029 | P0 | APPROVED | AC-004 | ADR-0007 | UI-019 | TASK-062 | TC-048, TC-054 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-5 | — | NFR-030 | P0 | APPROVED | AC-042 | ADR-0001, ADR-0005, ADR-0009 | — | TASK-005, TASK-100, TASK-102 | TC-047, TC-096, TC-097 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-5 | — | NFR-031 | P1 | APPROVED | AC-061 | ADR-0008, ADR-0009 | — | TASK-005, TASK-102 | TC-097 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-6 | — | NFR-032 | P1 | APPROVED | AC-060 | ADR-0007 | UI-001 | TASK-104 | TC-094 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |
| PG-6 | — | NFR-033 | P1 | APPROVED | AC-051 | ADR-0002 | UI-015 | TASK-093, TASK-113 | TC-029, TC-043, TC-056 | PASS 2026-11-04-1 | QA 2026-11-04-2: Verified |

## Coverage
- Requirements: 96 FR + 33 NFR. User stories: 26. Acceptance criteria: 64.
- P0/P1 requirements without an acceptance criterion: **0**.
- Requirements without an acceptance criterion (P2/P3 only, written when promoted): FR-009, FR-010, FR-043, FR-044, FR-059, FR-060, FR-061, FR-062, FR-073, FR-074, FR-089, NFR-028.
- FR without a user story: FR-089 (P3, out of scope).
- Test cases created: 0. Tests run: 0.

## Changelog
- 2026-10-26 v0.1: Initial draft (SDLC-003, Claude Code).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:11 +0500: "approve requirements v0.1 and D-010"). Requirement statuses PROPOSED → APPROVED. NEEDS_RESEARCH unchanged. Content otherwise identical to v0.1.
- 2026-10-26 v2.0: **Substantive change (SDLC-005)** applying user-approved decisions D-011 (supported Windows: CON-001, NFR-025, AC-040, ASM-001, OQ-010) and D-012 (INT 21h AH=00h: FR-039, FR-041, new AC-064, OQ-022). Changed requirements are `PROPOSED` until re-approval. Set status `Awaiting approval`.
- 2026-10-26 v2.0: **Approved** by the user (2026-10-26 18:18 +0500: "approve requirements v2.0"). FR-039, FR-041, NFR-025 → APPROVED.
- 2026-10-26 v2.1: Minor (SDLC-006). ADRs column added (proposed ADR-0001 to ADR-0009, awaiting approval). No requirement changed.
- 2026-10-26 v2.2: Minor (SDLC-007). UI items column added (UI-001 to UI-022 from `docs/04-design/ui-spec.md` v0.1, awaiting approval). No requirement changed.
- 2026-10-26 v2.3: Minor (SDLC-008). Tasks column added (`docs/05-planning/backlog.md` v0.1, awaiting approval). No requirement changed.
- 2026-10-30 v2.4: Minor (testing phase, `docs/07-testing/test-report-2026-10-30-1.md`). "Test cases" and "Test status" filled from test run 2026-10-30-1 (TC IDs from `docs/07-testing/test-plan.md` v0.1). "(partial: TASK-… pending)" marks requirements whose remaining tasks are not built. No requirement changed.
- 2026-11-02 v2.5: FR-097 / AC-065 / UI-023 / TASK-106 (Variables panel) added by the user's request (D-020). No other requirement changed.
- 2026-11-02 v2.6: Minor (testing phase, `docs/07-testing/test-report-2026-11-02-1.md`). "Test cases" and "Test status" refreshed from test run 2026-11-02-1 (TC IDs from test plan v0.3; TC-074..TC-091 added). FAIL: FR-091, FR-092 (TC-087, DEF-020) and NFR-005 (TC-089, DEF-021). No requirement changed.
- 2026-11-03 v2.7: Minor (testing phase, `docs/07-testing/test-report-2026-11-03-1.md`). "Test cases" and "Test status" refreshed from test run 2026-11-03-1. NFR-005 FAIL for the cold start (DEF-023); everything else that ran passes. No requirement changed.
- 2026-11-03 v2.8: Minor (QA phase). "QA status" column added from `docs/08-qa/qa-report-2026-11-03-1.md` (the matrix's planned QA column). No requirement changed.
- 2026-11-03 v2.9: NFR-005 revised (warm start, D-022): its test status now reads against the warm target. The QA status column still shows QA 2026-11-03-1 (before the revision) until QA runs again.
- 2026-11-03 v2.10: Minor (QA phase). "QA status" from `docs/08-qa/qa-report-2026-11-03-2.md` (after NFR-005 v2.1 and the user's manual checks). No requirement changed.
- 2026-11-03 v2.11: Minor (testing phase, `docs/07-testing/test-report-2026-11-03-2.md`). "Test cases" and "Test status" refreshed from test run 2026-11-03-2 (no failed test). No requirement changed.
- 2026-11-03 v2.12: Minor (testing phase, `docs/07-testing/test-report-2026-11-03-3.md`). "Test cases" and "Test status" refreshed from test run 2026-11-03-3 (no failed test). No requirement changed.
- 2026-11-03 v2.13: Minor (QA, `docs/08-qa/qa-report-2026-11-03-3.md`). "QA status" set from QA 2026-11-03-3. No requirement changed.
- 2026-11-03: FR-031 (v2.2, D-023) gains TASK-115. No other change.
- 2026-11-03: TC-095 (TASK-115) verifies FR-031 v2.2 and AC-020; "Test cases" and "Test status" are refreshed by the next test run.
- 2026-11-03 v2.14: Minor (testing phase, `docs/07-testing/test-report-2026-11-03-4.md`). "Test cases" and "Test status" refreshed from test run 2026-11-03-4 (no failed test). No requirement changed.
- 2026-11-04: FR-018 gains TASK-116 (ui-spec v1.2, D-024). No other change.
- 2026-11-04 v2.15: Minor (QA, `docs/08-qa/qa-report-2026-11-04-1.md`). "QA status" set from QA 2026-11-04-1. No requirement changed.
- 2026-11-04: NFR-030 cites TASK-100 (MIT `LICENSE`, D-025, TC-096). No other change.
- 2026-11-04: FR-090, NFR-030, NFR-031 cite TASK-102 (0 rows); test status is refreshed by the next test run.
- 2026-11-03 v2.16: Minor (testing phase, `docs/07-testing/test-report-2026-11-04-1.md`). "Test cases" and "Test status" refreshed from test run 2026-11-04-1 (no failed test). No requirement changed.
- 2026-11-04 v2.17: Minor (QA, `docs/08-qa/qa-report-2026-11-04-2.md`). "QA status" set from QA 2026-11-04-2. No requirement changed.
