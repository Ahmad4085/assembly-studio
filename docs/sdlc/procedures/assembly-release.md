# /assembly-release: Release

> **Session protocol:** run `docs/sdlc/agent-protocol.md` §3 (session start) before step 1 and §6
> (session end) before finishing. **Start this procedure only when the user explicitly asked for it**
> (slash command or a direct request naming it). Never start it on your own initiative.

## 1. Purpose
Turn a QA-approved build into a verifiable, documented Windows release candidate, and stop at the
publish decision for user approval.

## 2. Inputs
`Arguments`: semantic version (required, e.g. `0.1.0`). Ask if missing.

## 3. Reads
`PROJECT_STATE.md`, latest QA report and verdict, defects, test reports, packaging ADR, requirements
(installability/NFRs), project files (versions, package references), existing `docs/09-release/*`,
templates `release-report.md` and `release-notes.md`.

## 4. Workflow
1. **Prerequisites.** QA verdict `Pass` or `Pass with conditions` (conditions accepted in the approval
   log) for this build. No open Critical defects. Packaging ADR `Approved`. Required SDK and
   installer tool present (report missing tools, don't install them yourself). Any failure → `Not ready`, stop.
2. **Version consistency.** Set or verify the version in project properties, installer script and notes.
3. **Build and test.** Clean `dotnet publish` per the packaging ADR (self-contained or
   framework-dependent, RID `win-x64`, etc.), then run the full test suite against that build. Record
   commands and output.
4. **Package.** Build the installer per ADR (e.g. Inno Setup script under `build/installer/`). Compute SHA-256 hashes.
5. **Licenses.** Enumerate every shipped dependency (NuGet packages including transitive ones,
   bundled tools such as an assembler, fonts, icons, sample code). Record each license and its
   obligations in `docs/09-release/third-party-notices.md`, confirm they are met (notices included in
   the installer, source offers if required). Unknown license → `Not ready`.
6. **Install verification.** Clean install, upgrade, uninstall, first run offline on Windows 10 and
   11. Steps this environment can't perform are listed as user verification items, not ticked.
7. **Docs.** Release notes, install guide (`docs/09-release/install-guide.md`), release checklist and report.
8. **Readiness** = all checklist items have passing evidence. Ask the user for explicit approval
   before any tagging, signing, uploading or publishing.

## 5. Outputs
`docs/09-release/release-<version>.md`, `release-notes-<version>.md`, `third-party-notices.md`,
`install-guide.md`, installer artifacts (in the build output folder, not committed unless asked),
`PROJECT_STATE.md` update.

## 6. Validation
Checklist item results must cite evidence. Hashes match the artifacts. Notes list known limitations
and the supported scope truthfully.

## 7. Approval
Explicit user approval required to publish, tag, sign or distribute. Recorded in the approval log.
No Git tag or push without the user asking.

## 8. Failure handling
Any failed check → readiness `Not ready`, list failures and the command that fixes each
(`/assembly-implement`, `/assembly-test`, user action). Keep partial artifacts labeled as
non-release.

## 9. Exit conditions
Release report `Ready` and approved, or `Not ready` with failures listed.

## 10. Next permitted phase
After release: `/assembly-status`, then the next milestone via `/assembly-plan`.
