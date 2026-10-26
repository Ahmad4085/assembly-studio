# Assembly Studio — Shared Agent Rules

These rules apply to every AI coding agent (Claude Code, Google Antigravity, others) and to the
user. Tool-specific instructions live in `CLAUDE.md` (Claude Code) and `.agent/rules/` (Antigravity).

## Project
Assembly Studio is an offline-first Windows 10/11 desktop IDE for learning, writing, assembling,
executing, debugging and visualizing Assembly programs. Initial target: 8086 / 16-bit DOS. Planned
path: broader x86, then x86-64. Development follows a documentation-driven, phase-gated SDLC.
Nothing is implemented until its task is approved.

## Start of every session (before any edit)
Follow `docs/sdlc/agent-protocol.md` §3. In short:
1. Read `HANDOFF.md`, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`.
2. Run `git status --short` and `git log --oneline -5`. Changes you didn't make belong to someone
   else: never overwrite or revert them.
3. If a task is `In progress` under another agent and not handed off, ask the user before continuing.
4. Confirm the task you are about to do matches `CURRENT_TASK.md` / `HANDOFF.md` or an explicit user request.
5. Verify claims against code, tests and files before building on them.

## End of every session with meaningful work
Follow `docs/sdlc/agent-protocol.md` §6: update `CURRENT_TASK.md`, append to `WORK_LOG.md`, update
`PROJECT_STATE.md` (plus `ROADMAP.md` / `DECISIONS.md` if they changed), and rewrite `HANDOFF.md` with
the Git state and the exact next action.

## Where things are
| What | Where |
|---|---|
| Shared memory | `HANDOFF.md`, `CURRENT_TASK.md`, `PROJECT_STATE.md`, `WORK_LOG.md`, `ROADMAP.md`, `DECISIONS.md` |
| Phase gates, approvals, statuses | `docs/00-project-overview/sdlc-process.md` |
| Session/handoff/Git/recovery protocol | `docs/sdlc/agent-protocol.md` |
| Phase procedures (one per command) | `docs/sdlc/procedures/assembly-*.md` |
| Reusable guides | `docs/sdlc/guides/*.md` |
| Templates | `docs/templates/` |
| SDLC artifacts | `docs/01-requirements/` … `docs/09-release/`, ADRs in `docs/decisions/` |

## Workflow
Init → Requirements → Research → Architecture → UI/UX Design → Implementation Plan → Approved
Implementation → Testing → QA → Release. Commands: `/assembly-init`, `/assembly-requirements`,
`/assembly-research`, `/assembly-architecture`, `/assembly-design`, `/assembly-plan`,
`/assembly-implement <TASK-ID>`, `/assembly-test`, `/assembly-qa`, `/assembly-release <version>`,
`/assembly-status`. Each command runs the matching file in `docs/sdlc/procedures/`.
- Run a phase procedure only when the user explicitly asks for it.
- Never self-approve a gate. Approval is an explicit user statement, logged in `PROJECT_STATE.md`.
- Never silently bypass a missing or unapproved prerequisite.
- Implement only `Approved` backlog tasks, one at a time, within their scope.
- Unknown or high-impact decisions stay open questions (`OQ-###`). Don't guess.
- Never override an approved decision (`DECISIONS.md`, `docs/decisions/`). Propose a change instead.

## Architecture principles
- Keep these concerns as separate components behind interfaces: **editor/UI**, **assembler
  integration**, **CPU execution engine**, **runtime environment (memory map, loader, devices)**,
  **OS services (e.g. DOS INT 21h)**, **debugger/session control**, **visualization**.
- Assembling machine code is not executing it. A CPU emulator does not supply DOS services.
  NASM does not accept every syntax or target. Never treat any of these as interchangeable.
- The execution engine must be replaceable without rewriting the UI (the UI depends on abstractions).
- The visualizer displays only state produced by the real engine. **Never fabricate execution
  data**, including in demos, placeholders or screenshots.
- No x86 compatibility claim beyond what tests prove. Document unsupported instructions and services.

## Coding standards (details: `docs/sdlc/guides/dotnet-wpf-engineering.md`)
C# / .NET (version per approved ADR; preferred .NET 10 LTS), WPF + XAML, MVVM. Nullable enabled,
warnings as errors in core libraries, no logic in code-behind beyond view concerns, no UI types in
engine/assembler/runtime projects. Match the surrounding code.

## Testing and evidence
- Write tests with the implementation (engine/runtime: `docs/sdlc/guides/cpu-instruction-testing.md`).
- Source code and executed tests are the primary evidence. Never claim a feature, build, test
  pass or commit that you didn't verify in this session.
- A failed test stays **failed** in every document until a re-run proves the fix.
- Never fabricate timestamps. Use the system clock, or write "time not recorded".

## Dependencies and licensing
- Each third-party component needs an ADR or research entry recording license, maintenance state
  and distribution obligations **before** it is added.
- No paid dependencies and no mandatory online services/APIs. Copyleft components need an explicit
  approved ADR.

## Offline-first
All core features (edit, assemble, run, debug, tutorials, samples, help) must work with no network.

## Traceability
Stable IDs: `SDLC-###`, `PG-#`, `PER-#`, `US-###`, `FR-###`, `NFR-###`, `AC-###`, `ADR-####`, `UI-###`, `TASK-###`, `POC-###`,
`TC-###`, `DEF-###`, `RISK-###`, `OQ-###`, `ASM-###`, `WVR-###`, `BLK-###`. Never renumber or reuse
an ID. Every task cites its requirements, and every test cites what it verifies.

## Safety, files and Git
- Inspect a file before overwriting it. Update documents in place. Don't regenerate them.
- Never delete or rewrite approved content without recording the change and its reason.
- `.idea/` and `.mcp.json` belong to the user's JetBrains Rider setup. Don't modify them.
- No destructive Git commands (`reset --hard`, `clean`, forced checkout or push, history rewrites)
  without explicit user instruction. Commit only when the user asks or approves (agent protocol §8).
- No installs of SDKs or global tools unless the user asks.
