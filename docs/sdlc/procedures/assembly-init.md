# /assembly-init: Initialize or validate the SDLC workspace

> **Session protocol:** run `docs/sdlc/agent-protocol.md` §3 (session start) before step 1 and §6
> (session end) before finishing. **Start this procedure only when the user explicitly asked for it**
> (slash command or a direct request naming it). Never start it on your own initiative.

## 1. Purpose
Establish (or verify and repair) the documentation-driven, multi-agent SDLC foundation: shared rules,
shared memory files, the tool-independent core, templates and the tool-specific entry points. It is
idempotent: on an initialized workspace it validates and reports. It never writes application code.

## 2. Inputs
- Arguments: optional `--check` = report only, change nothing.
- The user's product vision (from conversation) if the overview does not exist yet.

## 3. Reads
- Workspace root listing, `git status`, `dotnet --info`, the agent tool's version (`claude --version`,
  `agy --version`).
- Every file in the expected inventory below, plus `.claude/commands/` (legacy Claude commands)
  and `.agents/` (alternative Antigravity root), if present.
- `docs/sdlc/guides/sdlc-documentation.md`.

## 4. Workflow
1. **Inventory.** Classify each expected item as *present*, *missing* or *deviating* (present but
   missing required sections):
   - Root: `AGENTS.md`, `CLAUDE.md`, `PROJECT_STATE.md`, `WORK_LOG.md`, `CURRENT_TASK.md`,
     `DECISIONS.md`, `ROADMAP.md`, `HANDOFF.md`, `.gitignore`.
   - Core: `docs/00-project-overview/sdlc-process.md`, `project-overview.md`; `docs/sdlc/README.md`,
     `agent-protocol.md`; `docs/sdlc/procedures/` (11 files: `assembly-init`, `-requirements`,
     `-research`, `-architecture`, `-design`, `-plan`, `-implement`, `-test`, `-qa`, `-release`,
     `-status`); `docs/sdlc/guides/` (5 files: `sdlc-documentation`, `assembly-emulator-research`,
     `cpu-instruction-testing`, `dotnet-wpf-engineering`, `desktop-ui-quality`).
   - Templates: `docs/templates/` (README, requirements, research-evaluation, architecture, adr,
     ui-spec, backlog, implementation-task, traceability, test-plan, test-report, qa-report,
     release-report, release-notes).
   - Claude Code: `.claude/skills/<name>/SKILL.md` for all 16 procedures and guides (thin wrappers).
   - Antigravity: `.agent/rules/assembly-studio.md`, `.agent/skills/<name>/SKILL.md` (16, CLI),
     `.agent/workflows/assembly-*.md` (11, IDE).
2. **Wrapper integrity.** Every wrapper only points to its core file (no procedure logic). Phase
   wrappers for Claude Code have `disable-model-invocation: true`. Every core file a wrapper names exists.
3. **Environment check.** Record OS, .NET SDKs/runtimes, assembler availability, Git state and agent
   tool versions in the overview's environment table. A missing SDK is a blocker for phase 6, not for docs.
4. **Conflict check.** Duplicate command names across `.claude/commands/` vs `.claude/skills/`, or
   `.agents/` vs `.agent/`. Report them and don't delete anything.
5. If `--check`: stop here and report.
6. **Create missing items** from the core and templates only. For *deviating* items, show the gap
   and propose a targeted edit. Never overwrite an existing file wholesale. Merge missing sections
   into `AGENTS.md`/`CLAUDE.md` and keep the user's content.
7. **Git.** If not a repository, ask the user whether to initialize. Never commit unless asked.
8. Session end (agent protocol §6).

## 5. Outputs
Only missing foundation files, plus shared-memory updates and an inventory report in chat.

## 6. Validation
- Every SKILL.md has YAML frontmatter with `name` matching its folder and a `description`.
  Antigravity rules have a valid `trigger`. Workflows have a `description`.
- Every template and core file referenced by a wrapper or procedure exists.
- `ROADMAP.md` phases match the process contract phases.
- Only foundation files were changed.

## 7. Approval
None required for creating missing foundation files. Changing `AGENTS.md`, the process contract or
the agent protocol substantively requires explicit user approval.

## 8. Failure handling
Unwritable directory, unreadable files or conflicting existing workflow: stop, report the exact
error and the options. Do not work around OS permissions.

## 9. Exit conditions
All expected items present and validated, or remaining gaps listed with reasons.

## 10. Next permitted phase
`/assembly-requirements`.
