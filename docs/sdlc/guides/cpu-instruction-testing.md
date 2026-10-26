# CPU instruction and runtime testing

## 1. Principles
- **Deterministic:** fixed initial state, fixed bytes, no wall clock, no host-dependent paths, seeded randomness only.
- **Full-state assertions:** assert what changed *and* that nothing else changed (registers, flags,
  touched memory, IP/CS, cycle/step counters if modeled). Use a state-diff helper.
- **Encoding-level inputs:** engine unit tests feed raw instruction bytes, not assembler output, so
  engine tests don't depend on the assembler. Assembler round-trips are integration tests.
- **Spec-sourced expectations:** expected values come from the CPU documentation or a reference
  test suite, cited in the test (e.g. comment with manual section or suite case ID). Never copy
  the engine's current output into the expected value.

## 2. Test shape (xUnit style, adapt to the approved framework)
```csharp
[Theory]
[InlineData(0x7F, 0x01, 0x80, /*OF*/true, /*SF*/true, /*ZF*/false, /*AF*/true, /*PF*/false, /*CF*/false)]
public void Add_r8_r8_sets_flags(byte al, byte bl, byte expected, bool of, bool sf, bool zf, bool af, bool pf, bool cf)
{
    var cpu = CpuStateBuilder.Default().WithAL(al).WithBL(bl).WithCode(0x00, 0xD8 /* ADD AL,BL */).Build();
    var before = cpu.Snapshot();

    cpu.Step();

    var diff = cpu.Snapshot().DiffFrom(before);
    diff.Should().OnlyChange(Reg.AL, Reg.IP, Flags.Arithmetic);
    // assert AL == expected and each flag ...
}
```
Names (`CpuStateBuilder`, `Snapshot`, `DiffFrom`) are illustrative. Use the architecture's real API.

## 3. Coverage checklist per instruction
- Every operand form (reg/reg, reg/mem, mem/reg, immediate, sign-extended immediate, 8/16-bit).
- All ModR/M addressing modes incl. displacement sizes, default segments (BP → SS) and segment overrides.
- Boundary values: 0, 1, 0x7F/0x80, 0xFF, 0x7FFF/0x8000, 0xFFFF. Carry/borrow in and out.
- All six arithmetic flags (CF, PF, AF, ZF, SF, OF) plus DF, IF, TF effects where relevant.
- **Undefined flags** (e.g. after MUL, DIV, some shifts/rotates, AAA/DAA families): only assert them
  if an ADR fixes the emulated behavior. Otherwise mask them out explicitly in the assertion and say why.
- Memory wrap-around at segment end (offset 0xFFFF + 1) and 20-bit address wrap (8086 has no A20).
- Stack: PUSH/POP/CALL/RET/INT/IRET effects on SP and memory, `PUSH SP` 8086 semantics.
- String ops with REP prefixes, DF both directions, CX = 0.
- Faults: divide error (INT 0) conditions, invalid/unsupported opcodes → defined engine behavior.
- Prefix combinations and instruction length (IP advance).

## 4. Higher-level tests
- **Interrupt/service tests:** INT 21h function N with given registers → expected output buffer and
  register results. Unsupported function → explicit diagnostic, not silent success.
- **Loader tests:** .COM loads at PSP:0100h with expected segment registers and SP. MZ relocations if in scope.
- **Program tests (integration):** small `.asm` fixtures in `tests/fixtures/` assembled by the real
  assembler, run to completion with a step limit, asserting output and final state. Skip with an
  explicit reason if the assembler isn't available. Never fake the pass.
- **Execution control:** breakpoints, step-over vs step-into, run limits, cancellation, history replay
  producing identical states.
- **Regression tests:** every fixed `DEF-###` gets a test named or tagged with the defect ID.

## 5. Reference suites
External per-instruction test suites may be used as data if their license allows. Record the source,
version and license in research and third-party notices. Load them from files in `tests/`, not the network.
