# Templates

Copy a template when a document is first created, then fill it in. Conventions (header block,
IDs, evidence labels) are in `docs/sdlc/guides/sdlc-documentation.md`. Never re-apply a
template over an existing document. Update in place instead.

| Template | Used by | Creates |
|---|---|---|
| `requirements.md` | `/assembly-requirements` | Section content for the `docs/01-requirements/` set (see `docs/sdlc/procedures/assembly-requirements.md` §5 for the file split) |
| `research-evaluation.md` | `/assembly-research` | `docs/02-research/<topic>.md` |
| `architecture.md` | `/assembly-architecture` | `docs/03-architecture/architecture.md` |
| `adr.md` | `/assembly-architecture` | `docs/decisions/ADR-####-<slug>.md` |
| `ui-spec.md` | `/assembly-design` | `docs/04-design/ui-spec.md` |
| `backlog.md` | `/assembly-plan` | `docs/05-planning/backlog.md` (+ milestones, risks) |
| `implementation-task.md` | `/assembly-plan`, `/assembly-implement` | Task entries inside the backlog |
| `traceability.md` | `/assembly-requirements`, extended by later phases | Full column layout of `docs/01-requirements/REQUIREMENTS_TRACEABILITY.md` |
| `test-plan.md` | `/assembly-test` | `docs/07-testing/test-plan.md` |
| `test-report.md` | `/assembly-test` | `docs/07-testing/test-report-YYYY-MM-DD-<n>.md` |
| `qa-report.md` | `/assembly-qa` | `docs/08-qa/qa-report-YYYY-MM-DD-<n>.md` |
| `release-report.md` | `/assembly-release` | `docs/09-release/release-<version>.md` (checklist + report) |
| `release-notes.md` | `/assembly-release` | `docs/09-release/release-notes-<version>.md` |
