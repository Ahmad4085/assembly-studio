# POC-001: Custom C# 8086 core (throwaway)

Definition, scope and results: `docs/02-research/poc-001.md`. This code is **not** product code and is never
promoted to `src/` except through an approved backlog task.

```powershell
./fetch-testdata.ps1                                      # SingleStepTests 8086 (MIT), pinned commit, ~39 MB, git-ignored
dotnet build Poc.Cpu.slnx -c Release
dotnet test  Poc.Cpu.slnx -c Release                      # oracle tests + hand-written tests
dotnet run   --project Poc.Cpu.Bench -c Release           # latency and throughput
```
