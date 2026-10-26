# Release <version>: Checklist and Report

| Field | Value |
|---|---|
| Version | <semver> |
| Date | YYYY-MM-DD |
| Build | <commit / build id> |
| QA report | docs/08-qa/qa-report-… (verdict) |
| Readiness | Ready / Not ready |

## Checklist
| # | Check | Evidence | Result |
|---|---|---|---|
| 1 | QA verdict is Pass or Pass with accepted conditions | | |
| 2 | No open Critical defects. High defects accepted by the user in writing | | |
| 3 | Clean release build executed | `dotnet publish …` output | |
| 4 | Full test suite executed on the release build | test-report-… | |
| 5 | Installer built | Installer tool + version, script path | |
| 6 | Installer tested: clean install, upgrade, uninstall on Windows 10 and 11 | | |
| 7 | App runs fully offline after install | | |
| 8 | Runtime prerequisites handled (self-contained or runtime check) | | |
| 9 | All dependencies listed with license. Obligations met (notices, source offers) | third-party-notices.md | |
| 10 | Code signing (if applicable) or documented unsigned-installer warning | | |
| 11 | Release notes and install guide written | | |
| 12 | Version numbers consistent (assembly, installer, notes) | | |
| 13 | User approval to publish | Approval log | |

## Report
What was built, how, the artifacts with SHA-256 hashes, known limitations, and items not completed.
