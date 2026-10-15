# Determinism corpus (S8)

Recorded matches for headless hash replay. Each `*.jbr.json` is a `MatchConfig` + input log + expected terminal xxHash64.

## Commands

```bash
# Rewrite seed fixtures with live hashes (after intentional Sim changes)
dotnet run --project tools/JewelBlitzArena.CorpusRunner -c Release -- record --corpus corpus

# CI / local gate
dotnet run --project tools/JewelBlitzArena.CorpusRunner -c Release -- run --corpus corpus

# Scrub one recording
dotnet run --project tools/JewelBlitzArena.ReplayViewer -c Release -- corpus/neutral-2p-180.jbr.json
```

When a Sim change intentionally alters state, re-`record` and commit the updated hashes with a clear explanation in the PR.
