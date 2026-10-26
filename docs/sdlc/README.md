# SDLC Core (tool-independent)

Everything here is plain Markdown that any agent or person can follow. Tool-specific files
(`CLAUDE.md`, `.claude/skills/`, `.agent/`) are thin entry points that point back here. They
contain no procedure logic of their own.

| Concern | Defined in |
|---|---|
| Phases, approval gates, document statuses, change control, waivers, evidence rules | `docs/00-project-overview/sdlc-process.md` |
| Sessions, shared memory, task lifecycle, handoff, Git, error handling and recovery | `docs/sdlc/agent-protocol.md` |
| Requirements and acceptance criteria | `docs/sdlc/procedures/assembly-requirements.md` + `docs/templates/requirements.md` |
| Research and evidence | `docs/sdlc/procedures/assembly-research.md` + `docs/sdlc/guides/assembly-emulator-research.md` |
| Architecture and decision records | `docs/sdlc/procedures/assembly-architecture.md` + `docs/templates/architecture.md`, `adr.md` + `DECISIONS.md` |
| UI/UX specification | `docs/sdlc/procedures/assembly-design.md` + `docs/sdlc/guides/desktop-ui-quality.md` |
| Planning and implementation task lifecycle | `docs/sdlc/procedures/assembly-plan.md`, `assembly-implement.md` + `docs/templates/implementation-task.md` |
| Testing | `docs/sdlc/procedures/assembly-test.md` + `docs/sdlc/guides/cpu-instruction-testing.md` |
| QA | `docs/sdlc/procedures/assembly-qa.md` |
| Release | `docs/sdlc/procedures/assembly-release.md` |
| Status and handoff | `docs/sdlc/procedures/assembly-status.md` + agent protocol §6 |
| Document writing conventions and IDs | `docs/sdlc/guides/sdlc-documentation.md` |
| .NET/WPF coding conventions | `docs/sdlc/guides/dotnet-wpf-engineering.md` |
| Templates | `docs/templates/` |

## Procedures (one per command)
`assembly-init`, `assembly-requirements`, `assembly-research`, `assembly-architecture`,
`assembly-design`, `assembly-plan`, `assembly-implement`, `assembly-test`, `assembly-qa`,
`assembly-release`, `assembly-status`: `docs/sdlc/procedures/<name>.md`.

Running a procedure without a slash command: read the procedure file and follow it, treating the
user's words after the command name as its *Arguments*.

## Guides (reusable knowledge)
`sdlc-documentation`, `assembly-emulator-research`, `cpu-instruction-testing`,
`dotnet-wpf-engineering`, `desktop-ui-quality`: `docs/sdlc/guides/<name>.md`.

## Editing the core
Changing a procedure changes it for every tool at once. Substantive procedure changes follow the
process contract §4 (user approval). Wrapper files must never contain procedure logic.
