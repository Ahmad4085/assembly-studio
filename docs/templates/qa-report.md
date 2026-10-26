# QA Report YYYY-MM-DD-<n>

| Field | Value |
|---|---|
| Build under QA | <commit / build id> |
| Scope | Milestone M# / release candidate <version> |
| Based on test report | docs/07-testing/test-report-… |
| Verdict | Pass / Pass with conditions / Fail |

## 1. Requirement verification
| Requirement | Priority | Acceptance criteria | Evidence (test, manual check) | Result |
|---|---|---|---|---|
| FR-001 | Must | AC1… | TC-…, manual step … | Verified / Failed / Not verified |

## 2. Quality checks
| Area | Checks performed | Result | Notes |
|---|---|---|---|
| UI functionality | | | |
| Usability | | | |
| Accessibility (keyboard, UIA names, contrast, high contrast, scaling) | | | |
| Offline behavior (network disabled) | | | |
| Reliability (runaway programs, faults, large files) | | | |
| Compatibility (Windows 10 / 11, DPI) | | | |
| Visualization integrity (shown state == engine state) | | | |
| Known limitations documented | | | |

## 3. Defects
| ID | Severity (Critical/High/Medium/Low) | Summary | Status |
|---|---|---|---|

## 4. Verdict rationale
Release recommendation is **blocked** if any Must acceptance criterion fails or any Critical
defect is open. List the conditions for "Pass with conditions".

## 5. Not verified
Items that could not be checked and why (never counted as passed).
