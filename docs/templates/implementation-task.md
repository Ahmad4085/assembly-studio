### TASK-### <Title>

| Field | Value |
|---|---|
| Status | Proposed / Approved / In progress / Done / Blocked / Dropped |
| Milestone | M# |
| Requirements | FR-…, NFR-… |
| Decisions / design | ADR-…, UI-… |
| Depends on | TASK-… |
| Risk | High / Medium / Low (reason) |

**Goal:** One sentence describing the outcome.

**Scope (in):**
- …

**Out of scope:**
- …

**Expected files / projects touched:** `src/…`, `tests/…`

**Test requirements:**
- TC-… : <what must be proven, deterministic inputs/expected outputs>

**Definition of Done:**
- [ ] Code builds with zero warnings in affected projects (`dotnet build` executed)
- [ ] New and affected tests executed and passing (`dotnet test` executed)
- [ ] Acceptance criteria of linked requirements met (or partial scope documented)
- [ ] No fabricated data in UI; unsupported cases surfaced explicitly
- [ ] Traceability, `WORK_LOG.md`, `CURRENT_TASK.md`, `PROJECT_STATE.md` and `HANDOFF.md` updated

Progress and results are logged in `WORK_LOG.md` (format: `docs/sdlc/agent-protocol.md` §7). While the
task is active it is mirrored in `CURRENT_TASK.md`.
