# TASK-043 Source map: verification notes

| Field | Value |
|---|---|
| Document | Implementation verification record |
| Task | TASK-043 (FR-028, FR-052, FR-053; ADR-0005 §5–6) |
| Last updated | 2026-10-29 |
| Author | Claude Code (Opus 5.5) |

Records the result for each item ADR-0005 §Validation marks **Unverified** for "the first toolchain task", and the
limits of the source map. ADR-0005 itself is unchanged. Evidence labels as in `docs/sdlc/guides/sdlc-documentation.md`.

## Method
Each fixture in `tests/AssemblyStudio.Toolchain.Nasm.Tests/fixtures/` is built with the bundled NASM 3.02 through
`NasmAssembler`. The resulting source map is checked three independent ways (verifiers ported from POC-002):
1. Every known listing byte equals the image byte at its address.
2. Every label's address in the map equals the first mapped range after its listing row.
3. Every instruction range in `.text` starts on an instruction boundary according to `ndisasm`. Data rows (`db`…`dt`,
   also via `times`, and their wrapped continuation rows) are skipped, because `ndisasm` decodes data inside `.text`
   as instructions.

Plus explicit address → line expectations per fixture (`SourceMapTests`).

## Results
| Case (ADR-0005 Unverified) | Fixture | Result | Evidence |
|---|---|---|---|
| Nesting `<10>` and deeper | `08-deep-nesting.asm` (12 macro levels) | **Works after a parser fix.** NASM right-aligns the nesting marker: rows without bytes put `<9>` and deeper one column left (column 35 instead of 36). POC-002's fixed column-36 read misses them, so those rows were treated as level 0. The product parser finds the marker by pattern in columns 30–38. Mutation check: with the fixed column the test fails; with the fix it passes | [Measured] `Twelve_nested_macro_levels_map_to_the_outermost_invocation`, verifiers 1–3 |
| Nested includes in subfolders | `09-nested-include.asm` → `inc/outer.inc` → `inc/sub/inner.inc` | **Works.** NASM resolves the nested `%include "inc/sub/inner.inc"` from its working directory (the main file's folder); lines map to each file | [Measured] `Nested_includes_in_subfolders_map_to_each_file`, verifiers 1–3 |
| `%rep` | `10-rep.asm` (a `%rep`, and a `%rep` nested in a `%rep`) | **Works.** `%rep` expansions are listed one level deeper with their own line numbers, so they map to those lines (not flagged as a macro expansion). Inside a macro they map to the macro's invoking line | [Measured] `Rep_bodies_map_to_their_own_lines`, verifiers 1–3 |
| Section switch inside a macro | `11-macro-section.asm` (`section .data` / `section .text` inside a macro) | **Works.** Code and data from the expansion map to the invoking line. A `section` line inside a macro *definition* (listed at level 0 but not assembled) must not change the current section; the parser tracks `%macro`/`%endmacro` for that | [Measured] `A_section_switch_inside_a_macro_maps_code_and_data_to_the_invoking_line`, verifiers 1–3 |
| (added) Data wider than one listing row, reserved space | `12-long-data.asm` | Wrapped data continues on rows with the same line number and their own offsets; every byte maps to the data line. `resb` in `.bss` maps to nothing | [Measured] `Wrapped_data_maps_every_byte_and_reserved_space_maps_nothing` |

The POC-002 fixtures 01–05 pass the same verifiers and their POC-002 address expectations. 06 (errors) and 07 (CPU
directive, now rejected by the TASK-042 guard) produce no source map.

## Limits (not verified; documented scope, FR-045)
- `%include` *inside* a macro, `%line` directives, `incbin`, and sections with `vstart=`/`follows=` attributes are not
  covered by fixtures. The byte verifier assumes a section's file position equals its start address, which holds without
  `vstart`/`follows`.
- The listing layout is undocumented. The mapping is verified for NASM 3.02 only (version pinned, ADR-0005 §6). Any other
  NASM version gets the FR-024 version warning, and a listing the parser can't read gives a successful build without a
  source map (BLD010).
- A macro is attributed to the line that invoked the outermost macro; lines inside the macro definition are never the
  current line (ADR-0005 §5).
