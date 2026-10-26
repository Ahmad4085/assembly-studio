# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## SDLC-003: Requirements phase

| Field | Value |
|---|---|
| Status | **Ready** (not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Procedure | `docs/sdlc/procedures/assembly-requirements.md` (invoke with `/assembly-requirements`) |
| Depends on | Phase 0 complete ✔ (SDLC-001, SDLC-002) |

**Objective:** Produce `docs/01-requirements/requirements.md` (users, use cases, FR/NFR with
acceptance criteria and priorities, constraints, supported-scope statement, assumptions, open
questions) and obtain explicit user approval.

**Requirements and inputs:** `docs/00-project-overview/project-overview.md` (vision, OQ-001…OQ-007),
the user's answers to the elicitation questions, `docs/templates/requirements.md`.

**Files in scope:** `docs/01-requirements/requirements.md` (new). `docs/00-project-overview/project-overview.md`
(OQ annotations only). Shared memory files. **No code.**

**Acceptance criteria (procedure exit conditions):**
- Every FR has ≥1 Given/When/Then acceptance criterion and a priority. Every NFR has a measurable threshold
  and a verification method.
- High-impact unknowns recorded as `OQ-###`, not guessed. The supported and unsupported scope is explicit.
- The document is `Approved` by an explicit user statement logged in `PROJECT_STATE.md`, or is
  `Awaiting approval` with the gate question asked.

**Validation performed:** none yet.
**Remaining work:** all of it. Starts with elicitation questions to the user.

---

## Last completed: SDLC-002, Multi-agent support (Done 2026-10-26, Claude Code)
**Validation performed (all executed 2026-10-26):**
1. Structural validator (`validate2.py`, kept in the session scratchpad rather than the repository) checked:
   - 11 procedures with all 10 sections and the session-protocol header, plus 5 guides;
   - 32 skill wrappers (16 Claude Code + 16 Antigravity), all pointing to existing core files, with matching names and descriptions;
   - Claude phase wrappers have `disable-model-invocation: true`;
   - 11 IDE workflows, and the rule `trigger: always_on`;
   - `AGENTS.md` has no frontmatter and is 5.9 KB, under the 24 KB limit;
   - no stale paths, no broken references, no parallel customization roots.
2. Antigravity CLI 1.4.4 (`agy -p`, plan mode, no tools): reported all 16 workspace skills plus `AGENTS.md` and
   `.agent/rules/assembly-studio.md` loaded. With `.agent/workflows/` present, the CLI loaded **no** workflows,
   so there are no duplicate commands.
3. Claude Code: `claude -p "/assembly-status"` (with `MSYS_NO_PATHCONV=1`) expanded the skill and produced a correct
   status report. Simulated arrival in both tools reported the same task, blockers and next action.
   Details are in `WORK_LOG.md` (SDLC-002).
**Not verified:** Antigravity desktop IDE 1.11.17 loading `.agent/workflows/` and `.agent/rules/` (needs the GUI).
