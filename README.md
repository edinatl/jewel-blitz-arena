Welcome to the Jewel Blitz Arena world!

This is a work in progress.

Note that the first working title during the planning stage was Power Coconuts, and Claude ran with that theme.  I had an agent remodel and remove those references, so that explains the notes about "coconut removal."  

Power Arena Unity Prototype.zip is a parallel approach started by @benrapsis for the same project, do not combine the two contents without explicit specification as such.

## Deterministic sim (S0)

Headless match simulation lives in `src/JewelBlitzArena.Sim` (no Unity references). Run tests without the Editor:

```bash
dotnet test JewelBlitzArena.sln -c Release
```

## Determinism CI (S8)

```bash
# Unit tests + corpus-loaded NUnit cases
dotnet test JewelBlitzArena.sln -c Release

# Recorded match corpus (hash gate)
dotnet run --project tools/JewelBlitzArena.CorpusRunner -c Release -- run --corpus corpus

# Fail on float / banned APIs in Sim and Data
python3 tools/scripts/check-sim-bans.py
```

Corpus files live in `corpus/`. Tools library: `src/JewelBlitzArena.Tools`.

Design index: `PLAN.md`. Architecture: `docs/tech/architecture.md`.

