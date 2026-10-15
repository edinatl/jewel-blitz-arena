# Jewel Blitz Arena — Unity 6 host (S3)

Presentation-only greybox that **drives** `JewelBlitzArena.Sim`. Rules stay in Sim; this project quantizes input, steps at 60 Hz, and draws actors/jewels/items.

Do **not** grow `unity/PowerArenaPrototype` — that tree stays quarantined feel-lab.

## Prerequisites

- Unity **6** (project version `6000.0.x`)
- .NET SDK 8+ (to rebuild plugin DLLs)

## Sync Sim / Data / PresentationBridge DLLs

From repo root:

```bash
./tools/scripts/sync-sim-plugins.sh
```

Copies Release `netstandard2.1` assemblies into `Assets/Plugins/Sim/`.

## Open / run

1. Run the sync script (above).
2. Open `unity/JewelBlitzArena/` in Unity 6.
3. Enter Play Mode (empty scene is fine — `GreyboxBootstrap` spawns the match).

### Controls

| Player | Move | Jump / Attack / Action | Strong / Surges |
| --- | --- | --- | --- |
| P1 | WASD | J / K / L | I / U / O |
| P2 | Arrows | 1 / 2 / 3 | — |

`R` restarts the match from the same seed.

## Headless greybox (no Unity Editor)

```bash
dotnet run --project tools/JewelBlitzArena.GreyboxCli -c Release -- --frames 180 --ascii
```

ASCII board + hash go to stdout; optional `--out path` writes a board dump.

## Determinism note

Unity never feeds floats back into Sim. Restart from the same seed + input log must match the headless hash (covered by PresentationBridge tests).
