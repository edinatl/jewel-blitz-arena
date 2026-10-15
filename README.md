Welcome to the Jewel Blitz Arena world!

This is a work in progress.

Note that the first working title during the planning stage was Power Coconuts, and Claude ran with that theme.  I had an agent remodel and remove those references, so that explains the notes about "coconut removal."  

Power Arena Unity Prototype.zip is a parallel approach started by @benrapsis for the same project, do not combine the two contents without explicit specification as such.

## Deterministic sim (S0) + content pipeline (S5)

Headless match simulation lives in `src/JewelBlitzArena.Sim` (no Unity references). Content tables live in `src/JewelBlitzArena.Data` and export to versioned `.jba` blobs under `content/export/`.

```bash
dotnet test JewelBlitzArena.sln -c Release
dotnet run --project tools/JewelBlitzArena.ContentExport -c Release -- stub content/export/s5_stub.jba content/authored/s5_stub.json
dotnet run --project tools/JewelBlitzArena.ContentExport -c Release -- validate content/export/s5_stub.jba
```

S2 adds fixed-point physics, jewel circulation, and item pickup/throw/catch stubs inside `JewelBlitzArena.Sim` (see `Physics/`, `Jewels/`, `Items/`). S1 owns damage/Blitz from contact events.

## Unity host (S3)

New Unity 6 project at `unity/JewelBlitzArena/` (presentation-only). Sync Sim DLLs, then open in Unity 6:

```bash
./tools/scripts/sync-sim-plugins.sh
# Open unity/JewelBlitzArena/ — Play Mode boots GreyboxBootstrap
dotnet run --project tools/JewelBlitzArena.GreyboxCli -c Release -- --frames 180 --ascii
```

`unity/PowerArenaPrototype` remains quarantined feel-lab — do not grow it into the ship host.

## Stage graph (S4)

Phase timers/transitions/hazards/kinematics step inside Sim (`Stages/StageRuntime`). Content schema v2; Citrine Bazaar three-phase stub in `content/authored/s5_stub.json`.

## Meta skeleton (S6)

Profile / Ledger / Custom+Sparring mode orchestrators live in `src/JewelBlitzArena.Meta` (no UnityEngine; never mutates Sim state).

```bash
dotnet test JewelBlitzArena.sln -c Release --filter MetaSkeleton
```

## Netcode scaffold (S7)

GGPO-style session shell in `src/JewelBlitzArena.Net` (input delay, rollback window 8, 2-byte inputs via PresentationBridge quantize). Headless dual-peer loopback:

```bash
dotnet test JewelBlitzArena.sln -c Release --filter NetcodeScaffold
dotnet run --project tools/JewelBlitzArena.NetSmoke -c Release -- --frames 180 --delivery-delay 3
```

Design index: `PLAN.md`. Architecture: `docs/tech/architecture.md`.
