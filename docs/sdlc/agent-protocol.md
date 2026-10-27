# Agent Protocol: shared memory, sessions, Git and recovery

| Field | Value |
|---|---|
| Document | Agent protocol (tool-independent) |
| Status | Approved (2026-10-26 16:39 +0500, with DECISIONS.md D-002 and D-003) |
| Version | 1.0 |
| Last updated | 2026-10-26 |
| Applies to | Every agent (Claude Code, Google Antigravity, OpenCode, others) and the user |

Assembly Studio is developed by more than one AI coding tool in the same workspace. This protocol
lets any agent resume the work from the repository alone, without the user re-explaining anything.
Phase gates and approvals are defined in `docs/00-project-overview/sdlc-process.md`. This file
defines how agents **start, track, finish and hand over** work.

## 1. Shared memory files (repository root)

| File | Purpose | Update rule |
|---|---|---|
| `AGENTS.md` | Shared engineering rules for all agents | Change only with user approval |
| `HANDOFF.md` | What the next agent needs, **read first** | Rewrite at the end of every session with meaningful work |
| `CURRENT_TASK.md` | The one active work item and its progress (acts as a lock) | Update at start, at milestones within the task, and at end |
| `PROJECT_STATE.md` | Verified project status, blockers, approvals, build/test status | Update after every meaningful change |
| `WORK_LOG.md` | Chronological, **append-only** log | Append one entry per session or per completed task |
| `ROADMAP.md` | Phases, gates, dependencies, milestones and progress | Update when a phase or milestone status changes |
| `DECISIONS.md` | Register of decisions (approved vs proposed), links to ADRs | Update when a decision is proposed, approved or superseded |

Sources of truth, in order: **source code and executed tests** > SDLC documents under `docs/` >
shared memory files. If memory files disagree with code or documents, the memory files are wrong:
fix them and log the correction.

## 2. Work item IDs
- `SDLC-###`: process, tooling and phase work (e.g. "Run requirements phase"). Registered in `ROADMAP.md`.
- `TASK-###` / `POC-###`: implementation backlog items (`docs/05-planning/backlog.md`).
- All other IDs (FR, NFR, ADR, TC, DEF, ...) follow `docs/sdlc/guides/sdlc-documentation.md`.
IDs are shared by all agents. Never create a parallel numbering in a tool-specific file.

## 3. Session start (mandatory, before any edit)
1. Read `HANDOFF.md`, `CURRENT_TASK.md`, `PROJECT_STATE.md` (in that order). Read `AGENTS.md` if
   your tool does not load it automatically.
2. Check the repository: `git status --short` and `git log --oneline -5`. Compare with the Git state
   recorded in `HANDOFF.md`. Unexpected differences (uncommitted changes, new commits) are
   **someone else's work**: list them and don't overwrite or revert them.
3. Check the lock: if `CURRENT_TASK.md` shows a task `In progress` owned by another agent or session
   and `HANDOFF.md` doesn't mark it paused or handed over, stop and ask the user whether to take it over.
4. Identify the task you are about to do. It must be either the current task, the next action in
   `HANDOFF.md`, or something the user explicitly asked for. If it differs from `CURRENT_TASK.md`, say
   so and get the user's confirmation before switching.
5. Verify claims you will build on. If a document says something is done, built or passing, check
   the code, test results or files before relying on it. Report contradictions.
6. Set `CURRENT_TASK.md` → `In progress`, owner = your tool name, start time (system clock).

## 4. Task lifecycle (`CURRENT_TASK.md`)
`Ready` → `In progress` → `Done` | `Blocked` | `Paused (handed off)`.
- Exactly one task is `In progress` at a time in this workspace.
- `Done` requires the procedure's exit conditions and validation to be met, with evidence.
- `Paused (handed off)` means the next agent may continue. `HANDOFF.md` must say exactly where it stopped.
- Implementation tasks additionally follow the backlog statuses in the process contract §2.

## 5. During work
- Stay within the task's scope and the procedure in `docs/sdlc/procedures/`. Scope changes need user approval.
- Never self-approve a gate. Approvals are explicit user statements, logged in `PROJECT_STATE.md`.
- Never overwrite or revert files you didn't change in this task unless the user asks.
- Record test results exactly as executed. A failed test stays `Fail` everywhere until a re-run passes.

## 6. Session end (after meaningful work, and before ending any substantial task)
1. Run the validation the procedure requires (build, tests, document checks) and capture results.
2. `CURRENT_TASK.md`: status, validation performed, remaining work.
   - **Done:** record the completion summary, then set the next task as `Ready`.
     The finished task stays in `WORK_LOG.md`.
3. `WORK_LOG.md`: append an entry (format in §7).
4. `PROJECT_STATE.md`: phase, blockers, build/test status, approvals, next action.
5. `ROADMAP.md` / `DECISIONS.md`: only if a phase, milestone or decision changed.
6. `HANDOFF.md`: rewrite with the current situation, the Git state (`git status --short` summary and
   the last commit hash **as of writing**), files that must not be reverted, and the exact next action.
   If you then commit the handoff itself, a file can't contain its own commit's hash. Record the
   planned commit message instead, and the next agent verifies it with `git log -1`. Never describe a
   commit as existing before Git has created it.
7. Recommend a commit when a validated milestone is reached (§8). Don't commit unless asked.

## 7. WORK_LOG entry format
```markdown
### <YYYY-MM-DD HH:MM ±ZZZZ | "time not recorded"> · <Agent/contributor> · <Work item ID>
- **Summary:** what was done (one to three lines)
- **Files changed:** paths (created / modified / deleted)
- **Tests executed:** exact commands and results, or "none (no code)"
- **Git state after:** `<short status summary>`, last commit `<hash or "none">`
- **Unresolved issues:** ...
- **Next step:** ...
```
Use the system clock for timestamps. If a time isn't reliably known, write "time not recorded"
or cite the evidence (e.g. "from file modification time"). Never edit or delete past entries.
Corrections go in a new entry.

## 8. Git practices
- Never run destructive commands (`reset --hard`, `checkout -- <path>`, `clean -fd`, `push --force`,
  history rewrites, branch deletion) without explicit user instruction for that exact action.
- Commit only when the user asks, or when the user has approved committing a validated milestone.
  Message: `<WORK-ITEM-ID>: <summary>`. Never claim a commit exists without its hash from Git output.
- Don't commit secrets, build output (`bin/`, `obj/`) or personal IDE state.
- Before editing a file with uncommitted changes you didn't make, ask the user.

## 9. Error handling and recovery
| Situation | Action |
|---|---|
| Task `In progress` but no matching handoff (interrupted session) | Reconstruct from `git diff`, `WORK_LOG.md` and file times. Report the reconstruction, set the task to `Paused (handed off)`, and ask before continuing |
| Shared memory contradicts code, tests or documents | Trust code, tests and documents. Fix the memory file and log the correction |
| Two documents contradict each other | Don't pick one silently. Raise an `OQ-###` and ask the user |
| Build or tests fail | Record `Fail` with output, open or update `DEF-###`, and keep it reported until a passing re-run |
| Required tool or SDK missing | Record a blocker (`BLK-###`), tell the user what to install, don't install it yourself |
| Accidental change outside scope | Revert only your own change, and only if it is still unshared. Otherwise report it |
| Files changed by another agent mid-session | Stop editing those files, report it, and ask the user |

## 10. Switching agents
**Leaving (any agent):** complete §6 with `CURRENT_TASK.md` set to `Done` or `Paused (handed off)`.
Tell the user the next action from `HANDOFF.md`.
**Arriving (any agent):** run §3, then continue with the next action. Tool-specific invocation:
- Claude Code: see `CLAUDE.md`. Procedures run as `/assembly-*` skills (`.claude/skills/`).
- Google Antigravity: see `.agent/rules/assembly-studio.md`. Procedures run as `/assembly-*`
  skills (CLI) or workflows (IDE).
- OpenCode: see `.opencode/assembly-studio.md` (loaded through `opencode.jsonc`). Procedures run as `/assembly-*`
  commands (`.opencode/commands/`).
- Any other agent: read `AGENTS.md`, then open `docs/sdlc/procedures/<command>.md` and follow it.
