# Research Summary

| Field | Value |
|---|---|
| Document | Research summary (entry document of the research set) |
| Status | Approved |
| Version | 1.0 |
| Last updated | 2026-10-26 |
| Depends on | `docs/01-requirements/PRODUCT_REQUIREMENTS.md` (set) v1.0 |

Research **recommends**. Architecture decides, through ADRs. Approving this summary accepts the recommendations
as **input** to `/assembly-architecture`. It doesn't approve any ADR or change any requirement.

## 1. Topic documents
| Topic | Document | Main open questions |
|---|---|---|
| Platform and packaging | `platform-and-packaging.md` | OQ-005, OQ-010, OQ-013 |
| CPU execution engine | `execution-engine.md` | OQ-001, OQ-008, OQ-015, OQ-016, OQ-017 |
| DOS services and .COM loading | `dos-services.md` | OQ-003, OQ-004 (loading) |
| Assembler and source mapping | `assembler.md` | OQ-002, OQ-004 (MZ), OQ-009 |
| Editor, MVVM, testing tools | `editor-mvvm-testing.md` | FR-011–022, FR-091–094, NFR-017/020/021 |

**How the evidence was gathered.** Five parallel research agents read primary sources on 2026-10-26. The lead
agent independently re-fetched 11 high-impact claims (marked ✔ in the topic files): the .NET 10 OS matrix,
.NET Framework lifecycle, Windows 10 ESU, Unicorn CPU models, Spice86.Core dependencies, SingleStepTests 8086,
AvalonEdit's latest release, the MS-DOS license, and three .COM loader facts (IP, SP and the return word in
`EXEC.ASM`). All agreed with the agents' reports. Assembler behavior was **measured** locally against NASM 3.02.
No product code was written and no POC was run.

## 2. Recommendations
| ID | Recommendation | Satisfies | Gaps it leaves (our work) |
|---|---|---|---|
| R-PLAT | **.NET 10 LTS + WPF** (supported to 2028-11-14), self-contained, untrimmed, folder layout, win-x64 (Arm64 optional) | FR-083, FR-086, NFR-008, NFR-030 | Ship the .NET Library License and Windows SDK License notices. Release the app again for each runtime patch. **Windows 10 conflict (§4, D1)** |
| R-PKG | **Inno Setup 7.x**, per-user (`PrivilegesRequired=lowest`), offline | FR-083–FR-085, FR-088, NFR-027 | Code-signing question (new OQ-020). WiX rejected (maintenance-fee EULA on binaries), MSIX rejected (signing cost) |
| R-ENG | **Custom managed C# 8086 core** behind an engine interface, validated against **SingleStepTests 8086/8088 (MIT)**. **Iced (MIT)** for disassembly | FR-033, FR-036–FR-038, FR-046–FR-062, NFR-016, NFR-017, NFR-019 | Decoder/executor, memory map, interrupt dispatch, control loop, snapshots. Behavior for instructions the tests exclude (POP CS, HLT, WAIT, LOCK). Feasibility and performance unproven (POC-001) |
| R-DOS | Implement the D-008 subset **faithfully to DOS 3.3/4.0** (MS-DOS source, MIT), with documented deviations | FR-034, FR-039–FR-041, FR-063, FR-064 | Deterministic BX..BP (deviation), no 0Ah template keys in the MVP, Ctrl-C checked on every character, ^P unsupported. Ctrl+C key conflict in the UI (new OQ-021) |
| R-ASM | **Bundle NASM 3.02 (BSD-2-Clause)**. Fixed flags: `-f bin --before "cpu 8086" --before "[map all …]" -l … -Xgnu --reproducible` | FR-023–FR-031, FR-021, NFR-014, NFR-030 | Source-mapping algorithm (listing + map, POC-002). **Detect `cpu` directives in user source** (a measured bypass of FR-027) |
| R-EDIT | **AvalonEdit 6.3.1.120 (MIT)**, net8 build on .NET 10 | FR-011–FR-020, FR-022 | Squiggle renderer, breakpoint and current-line margins, Replace UI, accessibility work (Narrator issue open). Verify on net10 (POC-003) |
| R-MVVM | **CommunityToolkit.Mvvm 8.4.2 (MIT)** | NFR-019, NFR-020 | — |
| R-TEST | **xUnit v3 (Apache-2.0)** (NUnit 5 is an equal alternative with a net10 build), **FlaUI.UIA3 (MIT)**, **coverlet (MIT)** | NFR-017, NFR-020, NFR-021, AC-043/044 | FlaUI release cadence. Proprietary Microsoft coverage extension not used |

**Rejected:**
- **Unicorn:** GPL-2.0; oldest model is the 486.
- **DOSBox, DOSBox-X, DOSBox Staging:** GPL; no hooks.
- **MartyPC as engine:** needs a Rust FFI; kept as a reference only.
- **8086tiny:** dormant, heavily compressed C.
- **DotXT:** license unclear.
- **Monaco/WebView2:** a second UI stack, and its runtime must be serviced offline.
- **Scintilla:** WinForms hosting only.
- **YASM:** no release since 2014.
- **.NET 8/9:** support ends 2026-11-10.

**Fallback:** Spice86.Core (Apache-2.0) only if POC-001 fails. It is a heavy dependency (25 packages,
including an LGPL one), and its embedding API is unknown.

## 3. Proposed proofs of concept (each needs your explicit approval; they would live in `poc/`)
| ID | Question it answers | Exit criterion | Blocked by |
|---|---|---|---|
| POC-001 | Can a custom C# core execute a representative 8086 subset correctly and fast enough? | A subset (MOV, ADD/SUB with flags, Jcc, CALL/RET, PUSH/POP, INT dispatch) passes its SingleStepTests 8086 cases. Step latency and throughput are **measured** against NFR-002/003 | **BLK-001** (no .NET SDK) |
| POC-002 | Does listing + map give reliable line ↔ address mapping with sections, `times`, `align`, macros? | Mapping is correct on a fixture set, including multi-section and macro cases | BLK-001 (a C# implementation; the parsing logic itself could be prototyped without the SDK) |
| POC-003 | AvalonEdit 6.3.1 on net10.0-windows: highlighting, squiggles, breakpoint gutter, Narrator | Renders on .NET 10. Narrator reads text and lines. Gaps listed | BLK-001 |
| POC-004 | Self-contained WPF + Inno Setup per-user offline install | Clean-VM install without network, **measured** size and time (NFR-027/028), clean uninstall | BLK-001 |
| POC-005 | (Only if POC-001 fails) Can Spice86.Core be driven step by step in-process? | In-process step/register API demonstrated, dependency licenses reviewed | BLK-001 |

**BLK-001 now affects research:** the .NET SDK is needed for every POC. Installing it needs your authorization
(WinGet: `Microsoft.DotNet.SDK.10`). Architecture can proceed without POCs, but the affected ADRs would then
stay conditional.

## 4. Decisions needed from you
| ID | Decision | Options | Recommendation |
|---|---|---|---|
| **D1** | Windows 10 support. Approved CON-001/NFR-025 say "Windows 10 and 11", but **.NET 10 supports Windows 10 only in Enterprise LTSC editions**, and Windows 10 22H2 itself has been out of support since 2025-10-14 (paid ESU only) | **A** Win11 24H2+ and Win10 LTSC supported; Win10 22H2 unsupported (may work, untested). **B** target .NET Framework 4.8.1 to keep Win10 22H2. **C** keep wording, run unsupported | **A**. It is a substantive requirements change (process §4), made via `/assembly-requirements` |
| D2 | DOS functions beyond the approved subset | Add AH=00h (DOS-1 terminate), 06h, 0Bh, 30h? Or keep D-008 as is | Add **00h** (trivial; same path as INT 20h). Decide 06h/0Bh in architecture (OQ-022) |
| D3 | POCs and SDK install | Approve POC-001..004 and authorize installing the .NET 10 SDK, or defer | Approve POC-001 and POC-002 before the engine and mapping ADRs |

## 5. Unverified items (all listed with a resolution plan in the topic files)
| Topic | Items |
|---|---|
| Platform | WPF self-contained size, Inno Setup offline and minimum OS (POC-004). Code signing / SmartScreen (OQ-020) |
| Engine | C# core feasibility and performance (POC-001). Iced on .NET 10, `INTEL8086_ONLY` coverage of POP CS. Intel 1979 manual contents |
| DOS | Default COMMAND.COM INT 23h behavior. Retail cancel key (ESC chosen) |
| Assembler | Mapping with macros/`times`/`%include` (POC-002). `nasm.exe` Authenticode signature. `exebin.mac` license |
| Editor/testing | AvalonEdit on net10 and with Narrator, upstream Replace support (POC-003). xUnit v3 / MSTest .NET 10 statements |

## 6. Open-question status after research
Updated in `docs/01-requirements/OPEN_QUESTIONS.md` (v1.1, minor: status column and three new questions).

## 7. Approval
Gate question: approve these research findings and recommendations (§2), with the unverified items in §5,
as input to architecture? D1–D3 can be answered at the same time or separately.

## 8. Approval record
Approved by the user on 2026-10-26 17:59 +0500 ("approve research v0.1; D1=A; D2=add 00h; D3=approve POC-001,002 and install the SDK"), logged in `PROJECT_STATE.md`.
- Recommendations R-PLAT, R-PKG, R-ENG, R-DOS, R-ASM, R-EDIT, R-MVVM and R-TEST are accepted as **input** to architecture. ADRs still decide.
- D1 = **A** → DECISIONS.md D-011. D2 = **add AH=00h** → D-012. D3 = **POC-001 and POC-002 approved, SDK install authorized** → D-013.
- D-011 and D-012 change approved requirements (CON-001, NFR-025, FR-039). They are applied through `/assembly-requirements` (SDLC-005).

## Changelog
- 2026-10-26 v0.1: Initial research summary (SDLC-004).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:59 +0500). Content unchanged from v0.1.
