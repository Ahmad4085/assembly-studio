# ADR-0005: Assembler integration — bundled NASM 3.02, listing + map source mapping

| Field | Value |
|---|---|
| Status | Approved |
| Version | 1.0 |
| Date | 2026-10-26 |
| Requirements addressed | FR-021, FR-023, FR-024, FR-025, FR-026, FR-027, FR-028, FR-029, FR-030, FR-031, FR-032, FR-052, FR-053, FR-075, NFR-006, NFR-014, NFR-030 |
| Research | `docs/02-research/assembler.md` (R-ASM), `poc-002.md` (POC-002: sufficient with limitations), D-006. Answers OQ-002, OQ-009 |

## Context
NASM syntax is the product syntax (D-006). NASM 3.02 is BSD-2-Clause, so it may be bundled with its notice. POC-002 showed that the
listing plus the map gives an exact line ↔ address mapping, including macros, includes and `times`/`align`, verified three
independent ways. It also showed that a `cpu` directive in user source overrides the injected 8086 restriction, and that `nasm -E`
detects such directives, even inside macros [Measured].

## Options considered
| Question | Option | Pros | Cons |
|---|---|---|---|
| Assembler | **NASM 3.02 bundled** | D-006 syntax. BSD-2. Measured behavior | External process (about 70 ms per run, measured) |
| | YASM / FASM | — | No release since 2014 / different syntax (research) |
| Source mapping | **Listing + map (POC-002)** | Verified, no extra tooling | Listing format undocumented (pin the version) |
| | Our own assembler or parser | Full control | A second assembler to maintain. Out of scope |
| CPU-directive guard | **`nasm -E` scan** | Catches macros and includes (measured) | Second process (62–67 ms) |
| | Scan the listing source column | One process | Not yet validated [Hypothesis] |

## Decision
1. **Bundle NASM 3.02 (win64 zip)** in `tools\nasm\` with `LICENSE` in the third-party notices. A **user-configured path**
   overrides it (FR-023, FR-075). The version is checked on use, with a warning below 3.02 (FR-024).
2. **Invocation** (`AssemblyStudio.Toolchain.Nasm`):
   - No shell; an argument list; a per-build temp directory; working directory = the source file's folder (untitled documents:
     the temp directory); a timeout of 10 s with process-tree kill; output capped at 1 MiB (NFR-014).
   - Flags: `-f bin --before "cpu 8086" --before "[map all <tmp>.map]" -l <tmp>.lst -Xgnu --reproducible -o <tmp>.com <source>`.
   - Unsaved documents are written to the temp directory and assembled from there (FR-025).
3. **CPU guard (FR-027):** before assembling, run `nasm -E`. Any `[cpu X]` with X ≠ 8086 is a **build error** at that line
   ("only 8086 is supported"). The runtime's unsupported-opcode halt (FR-037) stays as defense in depth.
4. **Results:** the GNU diagnostics parser (FR-029). Failures are distinguished: missing assembler, timeout, crash, non-zero exit
   without diagnostics (FR-030). The size limit is 65,280 bytes (FR-031). The MASM-style hint comes from source heuristics (FR-021).
5. **Source map** (`ISourceMap`): listing + map per POC-002. Macro bytes go to the invoking line (flagged), included lines to their
   file, sections via map `Vstart`. **After a failed build no map exists** (measured: an empty file), so the debugger requires a
   successful build.
6. **Version pinning:** the listing layout is undocumented, so the bundled NASM version is pinned. The POC-002 fixtures become
   permanent regression tests. Upgrading NASM is a planned task that has to re-run them.

## Consequences
- Each build costs about 140 ms of NASM time (measured on the dev machine), inside NFR-006 (≤ 2 s).
- Includes (`%include`) work for mapping, but multi-file programs remain FR-010 (P2) for UI and workflow support.

## Validation
- The POC-002 fixtures plus the three verifiers (image bytes, map symbols, ndisasm boundaries) run in the toolchain test project (AC-013/017/018/019/020).
- **Unverified:** nesting `<10>`+, nested includes in subfolders, `%rep`, sections switched inside macros. Fixtures are added in
  the first toolchain task. A failure there narrows the supported scope in FR-045 docs instead of failing silently.
- **Unverified:** the Authenticode signature of `nasm.exe`. Checked in the release phase.

## Changelog
- 2026-10-26 v0.1: Proposed (SDLC-006).
- 2026-10-26 v1.0: **Approved** by the user (2026-10-26 18:54 +0500: "approve architecture v0.1 and ADR-0001..0009; reference machine OK").
- 2026-11-02: Additive contract change for TASK-106 (FR-097, D-020): `ISourceMap.DataSymbols` (default: none) lists the labels on data with their size, read from the `[map all]` symbols and the listing (a symbol whose first bytes come from a `db`/`dw`/…/`times`/`res*`/`incbin` line; its size runs over the following data lines up to the next symbol). No change to the NASM command line or the decision.
