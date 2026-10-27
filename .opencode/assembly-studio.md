# Assembly Studio — OpenCode rules

`AGENTS.md` at the repository root contains the shared rules for every agent, and OpenCode loads it automatically.
OpenCode does not read `CLAUDE.md`. These rules add OpenCode specifics only. Loaded through `instructions` in
`opencode.jsonc`.

## Tool specifics
1. **Before any edit:** follow the session start in `docs/sdlc/agent-protocol.md` §3. Read `HANDOFF.md`,
   `CURRENT_TASK.md` and `PROJECT_STATE.md`, run `git status --short` and `git log --oneline -5`, and identify the
   active task. Never overwrite or revert changes you didn't make.
2. **SDLC commands:** `/assembly-*` are project commands in `.opencode/commands/`. Each one runs the
   tool-independent procedure in `docs/sdlc/procedures/`. Start a phase procedure only when the user explicitly runs
   it or names it. Never start one on your own initiative, even if it looks like the logical next step: suggest it instead.
3. **Guides:** the 5 guides (`sdlc-documentation`, `assembly-emulator-research`, `cpu-instruction-testing`,
   `dotnet-wpf-engineering`, `desktop-ui-quality`) are skills that OpenCode discovers in `.claude/skills/`. Load the
   relevant one with the `skill` tool before that kind of work. The `assembly-*` skills there are hidden phase
   wrappers (`disable-model-invocation`). Use the `/assembly-*` commands instead.
4. **Same IDs and documents as every other agent:** requirements, ADRs, task IDs and acceptance criteria come from
   `docs/` and the shared memory files. OpenCode todo lists are working notes only. `CURRENT_TASK.md` is authoritative.
5. **Approvals:** gate approvals are explicit user statements recorded in `PROJECT_STATE.md`. Your own plan or a
   permission grant is never an approval.
6. **After meaningful work:** complete the session end in `docs/sdlc/agent-protocol.md` §6. Record yourself as
   `OpenCode (<provider/model>)` in `WORK_LOG.md` and `CURRENT_TASK.md`, and rewrite `HANDOFF.md` before the session
   ends or the user switches tools.
7. **Permissions:** `opencode.jsonc` denies destructive Git commands and edits to the user's `.idea/` and `.mcp.json`.
   A denied command is a rule, not an obstacle: don't work around it with another command that has the same effect.

## Working discipline
- **Understand before changing.** Read the task block, its requirements, ADRs and the code you will touch. State the
  approach file by file and the tests that prove it before editing (`assembly-implement` step 3).
- **Small verified steps.** After each meaningful change, build and run the affected tests. Keep a todo list for
  multi-step work and update it as you go.
- **Verify, don't assume.** Check facts against files, tool output and official documentation. If a version, package
  name or API matters, look it up. Mark anything you couldn't check as unverified.
- **Prove tests can fail.** For rule or guard tests, make a real violation in a scratch copy, confirm the edit actually
  applied, and see the test fail. Then confirm it passes on the clean tree.
- **Root cause first.** When something fails, find out why before changing code. Never weaken, skip or delete a test to
  get green. Never call partial work done.
- **Honest reporting.** Quote real command output. Say what you didn't run. A failed result stays failed in every
  document until a re-run passes. Timestamps come from `date` or are written as "time not recorded".
- **Stay in scope.** If the task needs out-of-scope changes, stop and propose a backlog change.

## Known pitfalls in this repository
- Windows + Git Bash: prefix commands that pass `/x` style arguments (for example `reg query ... /s`) with
  `MSYS_NO_PATHCONV=1`. Get times with bash `date '+%Y-%m-%d %H:%M %z'`, not the Windows `date` command.
- PowerShell 5.1: no `&&`; native-command stderr under `$ErrorActionPreference = 'Stop'` can throw; `Set-Content`
  defaults to ANSI. Scripts in `build/` must run on Windows PowerShell 5.1.
- XML comments in `.props`/`.csproj` files cannot contain `--` (for example a `--coverlet` flag). The build breaks.
- The SDK is 10.0.403 with the Microsoft.Testing.Platform runner: use `dotnet test --solution AssemblyStudio.slnx`
  or `dotnet test --project <path>`. VSTest options do not apply.
- The architecture tests check both compiled references and `.csproj` files. A new `src` project must be added to
  `AllowedProductDependencies`.
- Oracle data (`testdata/`) is fetched by `build/fetch-singlesteptests.ps1` and verified only against the committed
  `build/*.sha256` manifests. Never regenerate a manifest from unverified downloads.
- `sed` edits with backslash paths often silently don't match. Check the file after a scripted edit.
