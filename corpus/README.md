# Determinism corpus (S8)

Recorded matches for headless hash replay. Each `*.jbr.json` is a `MatchConfig` + input log +
optional CPU handoffs (format v2) + item spawns (format v3) + expected terminal xxHash64.

Replay always loads the compiled S5 stub catalog (`SampleContentFactory` → same content as
`content/export/s5_stub.jba`), so `stageId` 1/2 exercise Citrine/Fulgur phase graphs and
fighter kits resolve by authored id.

## Commands

```bash
# Rewrite seed fixtures with live hashes (after intentional Sim / content changes)
dotnet run --project tools/JewelBlitzArena.CorpusRunner -c Release -- record --corpus corpus

# CI / local gate
dotnet run --project tools/JewelBlitzArena.CorpusRunner -c Release -- run --corpus corpus

# Scrub one recording
dotnet run --project tools/JewelBlitzArena.ReplayViewer -c Release -- corpus/neutral-2p-180.jbr.json
```

Fixtures include `cpu-replace-2p-120` (CPU brain takes slot 1 at frame 30),
`status-patches-2p-120` (format v3 item-spawn handoffs for Ice/Smoke/Adhesive world patches),
`fulgur-library-2p-300` (Fulgur Cloister at 10% Phase Clock reaches Library), and
`sunken-breach-2p-360` (Sunken Strongroom at 10% Phase Clock reaches Hull breach).

When a Sim or S5 content change intentionally alters state, re-`record` and commit the updated
hashes with a clear explanation in the PR.
