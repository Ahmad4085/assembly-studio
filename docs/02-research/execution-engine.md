# Research — 8086 Execution Engine

| Field | Value |
|---|---|
| Document | Research: CPU execution engine |
| Status | Approved |
| Version | 1.0 |
| Last updated | 2026-10-26 |
| Depends on | `docs/01-requirements/PRODUCT_REQUIREMENTS.md` (set) v2.0 |

**Method.** Sources were read on 2026-10-26 by a research agent. The claims marked ✔ were independently
re-fetched by the lead agent on the same day. Guidance: `docs/sdlc/guides/assembly-emulator-research.md`.

## 1. Questions
OQ-001 (which engine), OQ-008 (state access, breakpoints, snapshots, determinism), OQ-015 (undefined flags),
OQ-016 (CPU semantics and reference data), OQ-017 (reverse stepping, P2). Related: FR-033, FR-036–FR-038,
FR-046–FR-062, NFR-002, NFR-003, NFR-013, NFR-016, NFR-017, NFR-019.

## 2. Evaluation criteria
| Criterion | Weight | Derived from |
|---|---|---|
| License compatible with a permissive open-source release | Gate | D-007, NFR-030 |
| Runs inside our process from .NET, fully offline | Gate | NFR-008, CON-002 |
| 8086-exact semantics, including documented quirks | High | FR-036, NFR-017 |
| Per-instruction step, full register/flag/memory access, breakpoints | High | FR-046–FR-056 |
| Deterministic, snapshot/restore possible | High / Medium | NFR-016, FR-060 |
| Replaceable behind our own interface | High | NFR-019 |
| Dependency weight and native binaries | Medium | NFR-028, NFR-031 |

## 3. Options
### RES-001 Unicorn Engine
| Attribute | Finding | Evidence |
|---|---|---|
| License | **GPL-2.0** (`COPYING`). README: "released under the GPL license" | [Verified: https://raw.githubusercontent.com/unicorn-engine/unicorn/master/COPYING] |
| CPU coverage ✔ | Has a 16-bit mode, but the oldest CPU model is **`UC_CPU_X86_486`**. There is no 8086, 8088 or 286 model | ✔ [Verified: https://raw.githubusercontent.com/unicorn-engine/unicorn/master/include/unicorn/x86.h] |
| Hooks | Per-instruction code hook, interrupt, memory and invalid-instruction hooks. `uc_emu_start(..., count)` supports single steps and budgets. Context save/restore covers CPU state only | [Verified: unicorn.h, x86.h] |
| .NET | P/Invoke binding (NuGet `UnicornEngine.Unicorn` 2.1.3, depends on FSharp.Core) plus native `unicorn.dll` | [Verified: https://www.nuget.org/packages/UnicornEngine.Unicorn] |
| Verdict | **Rejected.** Fails the license gate, and 8086 semantics are unlikely with a 486 model [Hypothesis] | |

### RES-002 Spice86 / Spice86.Core
| Attribute | Finding | Evidence |
|---|---|---|
| License | **Apache-2.0** | [Verified: https://raw.githubusercontent.com/OpenRakis/Spice86/master/LICENSE] |
| Package ✔ | `Spice86.Core` 16.1.0 (2026-08-05), net10.0. **25 dependencies**, including Avalonia, SkiaSharp (+ Linux native assets), ASP.NET Core OpenAPI, ModelContextProtocol.AspNetCore, Swashbuckle, Serilog, Spectre.Console and **Mt32emu.net (LGPL-2.1-or-later)** | ✔ [Verified: https://www.nuget.org/packages/Spice86.Core] [Verified: https://www.nuget.org/packages/Mt32emu.net] |
| CPU / DOS | README: "8086/8088 fully implemented and tested". 186 is partial, 286 and 386 have no protected mode. **Provides DOS 5 INT 21h and BIOS services.** No CPU-model option is documented | [Verified: https://raw.githubusercontent.com/OpenRakis/Spice86/master/README.md] |
| Debugging | GDB remote protocol, built-in debugger, HTTP and MCP APIs, and determinism options (`--ClockJitterSeed`, instruction-based timing). **No documented in-process step API for a host UI.** No snapshots | [Verified: README] |
| Verdict | **Viable but heavy fallback.** Its purpose (reverse engineering whole DOS programs), its dependency footprint (an LGPL component and a web stack in a desktop teaching tool) and the unknown embedding API all count against it. Its full DOS layer goes well beyond our scope (D-008) [Hypothesis]. Evaluate only via POC-005 if the custom core proves infeasible | |

### RES-003 MartyPC (`marty_core`)
| Attribute | Finding | Evidence |
|---|---|---|
| License | MIT | [Verified: https://github.com/dbalsom/martypc/blob/main/LICENSE] |
| Accuracy | Cycle-accurate **8088** validated against real hardware. Claims 99.9997% on the 8088 V2 suite | [Verified: https://github.com/dbalsom/martypc] |
| Integration | Rust library crate. No C ABI, so we would need our own Rust `cdylib` and shim plus a native DLL [Hypothesis]. Boots real DOS, with no DOS services layer | [Verified: repo] |
| Verdict | **Rejected as engine** (second language toolchain; cycle accuracy is beyond our needs). Useful as a **reference** for 8088 behavior | |

### RES-004 Small cores and other managed emulators
| Candidate | Finding | Verdict |
|---|---|---|
| 8086tiny (C) | MIT, dormant since 2014, heavily macro-compressed C [Verified: https://github.com/adriancable/8086tiny] | Rejected: porting risk [Hypothesis] |
| DotXT (C#) | Says "public domain" with no license file, .NET 9 [Verified: https://github.com/folkertvanheusden/DotXT] | Rejected: legal ambiguity. Reference reading only |
| e8086 / 86sim | No license file [Verified: https://github.com/amensch/e8086, https://github.com/ghaerr/86sim] | Not usable |
| 8086sim (C#/WPF) | MIT, only "a handful" of instructions [Verified: https://github.com/f1nalspace/8086sim] | Not usable |
| cuppu86 | GPL-3.0 [Verified: https://github.com/aelobdog/cuppu86] | Fails the gate |

### RES-005 DOSBox / DOSBox-X / DOSBox Staging
GPL-2.0 / GPL-2.0 / GPL-2.0-or-later. Stand-alone applications with no per-instruction hooks. **Rejected** (license
gate; no debugger integration) [Verified: https://sourceforge.net/projects/dosbox/, https://github.com/joncampbell123/dosbox-x, https://github.com/dosbox-staging/dosbox-staging].

### RES-006 Custom managed C# 8086 core (ours, MIT)
| Attribute | Finding | Evidence |
|---|---|---|
| License | Our own code, under our license | — |
| Reference data ✔ | **SingleStepTests 8086** (MIT): captured from an Intel P80C86A-2, 2,000 tests per opcode, per-opcode `flags-mask` for undefined flags. Excludes POP CS, WAIT, HLT, LOCK, LEA/LES/LDS register forms and some group forms. **SingleStepTests 8088** (MIT): AMD D8088, 10,000 tests per opcode | ✔ [Verified: https://github.com/SingleStepTests/8086] [Verified: https://github.com/SingleStepTests/8088] |
| Primary documentation | Intel *The 8086 Family User's Manual* (Oct 1979, 9800722-03), scanned copy on the Internet Archive (copyrighted; use as reference only, don't redistribute) | [Unverified: PDF not opened] |
| Disassembly | **Iced** (MIT, NuGet 1.21.0, netstandard2.0): 16-bit decoding with a NASM formatter. `CpuidFeature.INTEL8086` / `INTEL8086_ONLY` can tag non-8086 instructions after decoding | [Verified: https://github.com/icedland/iced] [Verified: https://docs.rs/iced-x86/latest/iced_x86/enum.CpuidFeature.html] |
| Hooks, determinism, snapshots | Entirely under our control: designed in, not retrofitted | — |
| Effort and risk | Bounded instruction set (about 300 opcode forms with ModRM, prefixes, BCD, MUL/DIV edge cases) with a strong external test oracle [Hypothesis]. No effort estimate given | |
| Verdict | **Recommended** | |

## 4. Comparison
| Option | License gate | 8086 semantics | .NET in-process | Step/state/breakpoints | Snapshots | DOS layer | Weight |
|---|---|---|---|---|---|---|---|
| Unicorn | **Fail** | Unlikely (486 model) | P/Invoke | Yes | CPU only | No | Native DLL |
| Spice86.Core | Pass (+ LGPL dep) | Claimed | Yes (.NET 10) | Via GDB/HTTP. In-process API unknown | No | Yes (DOS 5) | 25 deps |
| MartyPC | Pass | Cycle-exact 8088 | Rust FFI | Unknown | Unknown | No | Native DLL |
| DOSBox family | **Fail** | No | No | No | No | Yes | — |
| **Custom C# core** | Pass | Proven by tests | Native | Designed in | Designed in | Ours (D-008 subset) | None |

## 5. Recommendation R-ENG (input to an ADR)
- Build a **custom managed 8086 core** behind an `ICpu`/engine interface (NFR-019).
- Validate it instruction by instruction against **SingleStepTests 8086** (and 8088 where useful). Use the
  suites' `flags-mask` to decide the undefined-flag policy (OQ-015): show undefined flags as undefined in the UI,
  and optionally reproduce the captured silicon values in a separate test mode [Hypothesis, decide in an ADR].
- Use **Iced** for the disassembly view and for instruction-length decoding.
- Implement the **DOS subset ourselves** (see `dos-services.md`). A CPU core provides no DOS services.
- Make snapshots part of the design (full CPU state plus 1 MiB memory or dirty-page tracking) so FR-060 stays
  feasible (OQ-017) [Hypothesis].

**What we must build:** the decoder and executor, the memory map (1 MiB with wrap-around), the interrupt
dispatch to our DOS layer, the step/run/budget/breakpoint control loop, and snapshots. Everything that SingleStepTests
excludes (POP CS, HLT, WAIT, LOCK and the like) needs our own documented behavior (FR-036).

**Fallback:** if POC-001 shows that the custom core is infeasible within the MVP, evaluate Spice86.Core
embedding (POC-005) with a dependency and license review.

## 6. Unverified items and resolution
| Item | Resolution |
|---|---|
| Feasibility, step latency (NFR-002) and throughput (NFR-003) of a C# core | **POC-001** (needs the .NET SDK, see BLK-001) |
| Exact undefined-flag policy | ADR in architecture, using SingleStepTests metadata |
| Intel 1979 manual contents and availability | Open the archive copy when implementing. Reference only |
| Iced 1.21.0 on .NET 10 in practice; whether `INTEL8086_ONLY` covers POP CS | POC-001 |
| SingleStepTests size and repository activity | Check when downloading the test data (store it in `tests/`, offline) |
| The remaining 25 of 35 unevaluated GitHub C# 8086 results | Low priority. A custom core is recommended anyway |

## Changelog
- 2026-10-26 v0.1: Initial research (SDLC-004).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:59 +0500). Content unchanged from v0.1.
- 2026-10-26: Minor. *Depends on* updated to requirements v2.0 after revalidation: v2.0 applies D-011/D-012, which came from this research. No content change.
