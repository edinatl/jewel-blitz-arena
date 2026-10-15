# Prototype quarantine (Power Arena feel-lab)

Policy for keeping @benrapsis’s **Power Arena** track separate from `PLAN.md` orchestration and the shipping `JewelBlitzArena.*` sim.

## Ignore lists

| File | Role |
| --- | --- |
| `.cursorignore` | Agents / indexing skip feel-lab paths by default |
| `.gitignore` | Unity/`unity/**` project noise, .NET `bin`/`obj`, local unzip/scratch dirs |

## Quarantined (do not merge into Sim / new Unity host)

| Path | Why |
| --- | --- |
| `Power Arena Unity Prototype.zip` | Parallel Ben archive — leave zipped |
| `unity/PowerArenaPrototype/` | Float Unity 2022.3 feel greybox; wrong boundary for D1 |
| `docs/index.html`, `docs/sprite-arena.html` | Browser float demos; feel reference only |
| Local: `power-arena-unzipped/`, `PowerArenaUnityPrototype/`, `PowerArenaExtract/` | Scratch extract dirs (gitignored) |

## Complimentary (keep; do not ignore)

Ideas already aligned with `PLAN.md` / `docs/design/*` — use as **feel notes**, re-implement in fixed-point Sim later:

- Jewel scramble on hit, throw/catch windows, knockdown recovery timing
- Perfect dodge / recovery roll *feel* (encode in S1 with plan names: Luster/Blitz, not Power Gauge)
- Shared arena items and stage furniture as throwables
- Multi-fighter FFA chaos readability goals

Do **not** port: float physics, UnityEngine rules, Power Gauge/Drive/Fusion names, prototype cast (Soldier/Samurai…), or the 111-item catalog (plan roster is 81 usable).

## Sibling PLAN implementation (complimentary, not Ben)

| Branch | Content |
| --- | --- |
| `cursor/sim-foundation-d7b9` | S0 — `JewelBlitzArena.Sim` + tests + narrowed `.gitignore` |
| `cursor/determinism-ci-d7b9` | S8 — corpus runner, ban gate, Tools |

Those are the preferred merge path for orchestration. Recent Ben commits under `unity/PowerArenaPrototype/` are feel-lab only.

## Explicit non-goals

- Unzipping Ben’s zip into `unity/PowerArenaPrototype`
- Growing PowerArenaPrototype into the ship product (prefer new `unity/JewelBlitzArena/` host in S3)
- Blocking Sim PRs on feel-lab churn
