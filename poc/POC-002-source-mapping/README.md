# POC-002: NASM listing + map source mapping (throwaway)

Definition, scope and results: `docs/02-research/poc-002.md`. This code is **not** product code and is never
promoted to `src/` except through an approved backlog task.

Requires NASM 3.02 (`nasm.exe` and `ndisasm.exe`) on `PATH`, or `NASM_PATH` pointing to `nasm.exe`.

```powershell
dotnet build Poc.Mapping.slnx -c Release
dotnet test  --solution Poc.Mapping.slnx -c Release      # MTP mode via global.json (.NET 10 SDK)
```

Fixtures (`fixtures/`): single section, two sections with re-entry, `times`/`align`, multi-line macro,
`%include`, errors and a warning, and a `cpu 386` bypass attempt.
