# Work Log

> Append-only. Newest entry at the bottom. Never edit or delete past entries. Corrections go in a
> new entry. Format and rules: `docs/sdlc/agent-protocol.md` §7.

### 2026-10-26, time not recorded · Claude Code (Opus 5.5) · SDLC-001
- **Summary:** Inspected the empty workspace. The first write attempt failed with `EPERM`: the folder ACL
  granted write access only to Administrators and SYSTEM. Stopped and asked the user to fix permissions.
- **Files changed:** none
- **Tests executed:** none (no code)
- **Git state after:** not a repository
- **Unresolved issues:** folder not writable (user action)
- **Next step:** user grants Modify permission

### 2026-10-26, time not recorded · User · SDLC-001
- **Summary:** Granted Modify permission on `D:\assamby-ide` to the user account. Opened the folder in
  JetBrains Rider, which created `.idea/` and `.mcp.json` (Rider MCP server). Their file modification
  times are 16:06–16:10 +0500.
- **Files changed:** `.idea/` (created), `.mcp.json` (created)
- **Tests executed:** none
- **Git state after:** not a repository
- **Unresolved issues:** none
- **Next step:** continue SDLC-001

### 2026-10-26 ~16:07–16:13 +0500 (from file modification times) · Claude Code (Opus 5.5) · SDLC-001
- **Summary:** Created the SDLC foundation: `CLAUDE.md`, process contract, project overview, status file,
  13 templates, 11 phase command skills and 5 reusable skills. Validated frontmatter, required sections and
  cross-references. Confirmed Claude Code loads the skills, and ran `/assembly-status` headlessly (read-only, no changes).
- **Files changed:** created `CLAUDE.md`, `docs/00-project-overview/{sdlc-process,project-overview}.md`,
  `docs/status/project-status.md`, `docs/templates/*` (14), `.claude/skills/*/SKILL.md` (16)
- **Tests executed:** structural validation script (all 16 skills OK). `claude -p "/assembly-status"` → correct report
- **Git state after:** not a repository
- **Unresolved issues:** BLK-001 (no .NET SDK). OQ-006 (Git) open
- **Next step:** `/assembly-requirements`

### 2026-10-26 16:16–16:37 +0500 · Claude Code (Opus 5.5) · SDLC-002
- **Summary:** Added multi-agent support. Moved procedure and guide bodies into a tool-independent core
  (`docs/sdlc/procedures/`, `docs/sdlc/guides/`) with a new agent protocol (sessions, shared memory,
  task lifecycle, Git, recovery). Replaced the 16 Claude skills with thin wrappers (same names and
  descriptions). Added Antigravity entry points under `.agent/`. Created the root shared memory files.
  Migrated `docs/status/project-status.md` into `PROJECT_STATE.md` and deleted the old file. Moved
  shared rules from `CLAUDE.md` into `AGENTS.md` (`CLAUDE.md` imports it). Initialized Git (D-004).
- **Files changed:** created `AGENTS.md`, `PROJECT_STATE.md`, `WORK_LOG.md`, `CURRENT_TASK.md`, `DECISIONS.md`,
  `ROADMAP.md`, `HANDOFF.md`, `.gitignore`, `docs/sdlc/{README,agent-protocol}.md`, `docs/sdlc/procedures/*` (11),
  `docs/sdlc/guides/*` (5), `.agent/rules/assembly-studio.md`, `.agent/skills/*/SKILL.md` (16),
  `.agent/workflows/assembly-*.md` (11). Modified `CLAUDE.md`, `.claude/skills/*/SKILL.md` (16, now wrappers),
  `docs/00-project-overview/{sdlc-process,project-overview}.md`, `docs/templates/{README,implementation-task}.md`.
  Deleted `docs/status/project-status.md` (content migrated to `PROJECT_STATE.md`).
- **Tests executed:**
  - Structural validator: ALL OK, after fixing 1 stale path in `docs/templates/requirements.md` (details in `CURRENT_TASK.md`).
  - `agy -p "/skills"` (print mode, no model turn): lists all 16 workspace skills.
  - `agy -p` in plan mode, without tools: confirmed `AGENTS.md` and `.agent/rules/assembly-studio.md` load, and that
    `.agent/workflows/` is ignored by the CLI (no duplicate commands).
  - Simulated arriving-agent session start, read-only: Antigravity CLI and Claude Code
    (`claude -p "/assembly-status"`, 12 turns, skill expanded) both reported SDLC-003 `Ready`, SDLC-002 done, BLK-001,
    and the correct next command for their tool.
  - Both flagged real defects, which were fixed: a timestamp written ahead of the system clock, documents
    claiming the initial commit before it existed, and the next action missing the pending decision review.
    The commit-hash gap led to a fix in agent protocol §6.
- **Git state after:** repository initialized. All files committed in the single initial commit
  `SDLC-002: Multi-agent SDLC foundation` (this entry is part of it, so verify with `git log -1`). `.mcp.json`
  is git-ignored as machine-specific
- **Unresolved issues:** loading of Antigravity IDE 1.11.17 workflows and rules not verified (needs the GUI).
  Antigravity has no equivalent of Claude's `disable-model-invocation`, so the phase gate relies on rule text. BLK-001 remains
- **Next step:** user reviews D-002/D-003/D-005, then runs `/assembly-requirements` (SDLC-003)

### 2026-10-26 16:37 +0500 · Claude Code (Opus 5.5) · SDLC-001 (correction)
- **Summary:** Correction to the SDLC-001 entry. Its `claude -p "/assembly-status"` check was run from Git Bash,
  which converts a leading `/` argument into a Windows path (`C:/Program Files/Git/assembly-status`). So that
  run may not have expanded the skill, and it doesn't prove that invocation works. Invocation was re-verified
  in SDLC-002 with `MSYS_NO_PATHCONV=1`: the skill expanded and ran its procedure.
- **Files changed:** none (log correction only)
- **Tests executed:** see SDLC-002 entry
- **Git state after:** as SDLC-002
- **Unresolved issues:** none
- **Next step:** headless tests from Git Bash must set `MSYS_NO_PATHCONV=1`
