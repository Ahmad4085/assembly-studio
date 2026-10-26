# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## SDLC-007: UI/UX design phase

| Field | Value |
|---|---|
| Status | **Ready** (not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Procedure | `docs/sdlc/procedures/assembly-design.md` (invoke with `/assembly-design`) |
| Depends on | Architecture v1.0 + ADR-0001..0009 approved ✔ (2026-10-26 18:54 +0500) |

**Objective:** produce `docs/04-design/ui-spec.md` (layout, panels UI-###, state matrix, editor behavior, commands and keyboard, accessibility,
component conventions) and `docs/04-design/design-tokens.md`, then obtain user approval. Builds no UI.

**Key inputs:** requirements v2.0 (UI-facing FRs, NFR-022/026), architecture §2–§4 (snapshots, session states, 30 Hz publishing), ADR-0006 (AvalonEdit),
ADR-0007 (MVVM), `docs/sdlc/guides/desktop-ui-quality.md`. Open question to settle: OQ-021 (Ctrl+C routing between console and editor).

**Acceptance criteria:** the procedure's validation (`docs/sdlc/procedures/assembly-design.md` §6) and approval.

**Validation performed:** none yet.
**Remaining work:** all of it.

---

## Last completed: SDLC-006, Architecture phase (Done 2026-10-26, Claude Code)
- Architecture v1.0 + ADR-0001..0009 **Approved** 2026-10-26 18:54 +0500 ("approve architecture v0.1 and ADR-0001..0009; reference machine OK"). D-014 reference machine approved.
- Validation: allocation 129/129, traceability ADR column (v2.1), requirements validator and structural validator ALL OK. OPEN_QUESTIONS v2.3.
