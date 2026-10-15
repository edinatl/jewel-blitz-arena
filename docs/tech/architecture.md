# Technical Architecture

Owner: combat/sim engineer, netcode engineer, tools engineer. Decisions D1–D7 are recorded in
`PLAN.md` §5; this document is how they are carried out.

## 1. Assemblies and boundaries

```
JewelBlitzArena.Sim            # deterministic; no UnityEngine; fixed-point; pure C#; includes CPU brains
JewelBlitzArena.Data           # exported content tables (items, fighters, stages, rules); no engine refs
JewelBlitzArena.Meta           # profile, inventory, Tinker, Facet Wheel, unlocks, mode orchestrators
JewelBlitzArena.Net            # rollback session, transport, lobby
JewelBlitzArena.Presentation   # Unity project: rendering, camera, VFX, audio, UI, input, platform
JewelBlitzArena.Tools          # editor extensions: stage graph editor, item editor, economy sim, replay viewer
JewelBlitzArena.Tests          # headless NUnit: sim determinism, crate table, Tinker, unlocks, migrations
```

Enforced by assembly definitions and a CI check that `Sim`, `Data`, and `Meta` compile against
.NET Standard 2.1 with no Unity reference. `Sim` is the only assembly allowed to mutate match
state. `Presentation` reads snapshots and writes inputs.

## 2. Simulation

### 2.1 Step and numerics

- 60 Hz fixed step. `Sim.Step(in InputFrame[4]) → void`; `Sim.Hash() → ulong`.
- **Q48.16 fixed-point** (`long` backed, 16 fractional bits) for positions, velocities,
  forces, and angles. This is the format Photon Quantum uses for its deterministic Unity
  games. Precision is 1/65,536 m, far below anything visible. Multiplication is a single
  64-bit multiply and shift, safe for operands up to about ±46,000 (stages are under 200 m,
  and squared distances stay under 40,000). Debug builds check every multiply for overflow.
  - v0.1 chose Q32.32. Its products need a 128-bit intermediate, and Unity's .NET Standard
    2.1 profile has neither `Int128` nor `Math.BigMul(long, long, out long)`. Every multiply
    would have become four partial products, in the hottest code in the game, to buy
    precision nothing needs.
- Gameplay quantities that are naturally whole numbers are plain `int`, not fixed-point:
  Life, Luster, damage, frame timers, counts. Luster is 0–1200 so the 20 s drain is exactly
  1 per frame (`balance.md` §3.3).
- Trigonometry and sqrt via lookup tables and iterative routines in the Sim assembly.
- **RNG.** xoshiro256** streams, all part of `MatchState`, all seeded from
  `MatchConfig.seed`: one gameplay stream, and one stream per CPU slot. A change to CPU
  decision code therefore cannot shift crate contents in recorded replays. Presentation has
  its own non-deterministic RNG and never touches the sim's.

### 2.2 Determinism rules

A Roslyn analyzer enforces these in `Sim` and `Data` and fails the build on violation:

| Banned | Why | Use instead |
| --- | --- | --- |
| `float`, `double`, `decimal`, `System.Math` floating functions | Platform and JIT-dependent rounding | `Fix` (Q48.16) and Sim math library |
| `System.Random`, `DateTime`, `Stopwatch`, `Environment.TickCount` | Non-deterministic sources | Sim RNG streams, frame counter |
| Iterating `Dictionary` / `HashSet` | Order is implementation-defined | Flat arrays indexed by slot |
| `string.GetHashCode()` | Randomized per process on .NET Core | Ids from the registry |
| `Array.Sort`, `List.Sort`, LINQ `OrderBy` | Introsort is unstable, and runtimes (Mono, CoreCLR, IL2CPP) may order equal keys differently | Sim's stable insertion/merge sort with a slot-index tiebreak |
| Threads, tasks, `Parallel` inside `Step` | Scheduling order | Single-threaded `Step` |
| Managed allocation inside `Step` | GC pauses and pooled-object order | Fixed pools in `MatchState` |
| Float → `Fix` conversion inside `Sim` | Rounding differs across platforms | Convert at data export or at input quantization, outside `Sim` |

### 2.3 State layout

Everything the match needs to resume is in one `MatchState`: a single blittable unmanaged
struct of fixed-size buffers (`unsafe fixed` arrays and nested blittable structs; no managed
arrays, which are heap references and would make `memcpy` copy pointers). Caps are design
constraints (see `stages.md` §5) and snapshot constraints:

| Pool | Cap |
| --- | --- |
| Actors | 4 |
| Boss sub-actors | 8 |
| Loose items | 12 |
| Projectiles | 32 |
| Summons | 2 |
| Kinematic bodies | 16 |
| Hazard volumes | 32 |
| Jewels (field + held, incl. bonus) | 10 |
| Crates | 8 |
| Gleam pickups | 24 |
| Stage creatures (void beetles, young, guards) | 8 |
| CPU brain state | 4 |

No managed allocation inside `Step`. Snapshot = one block copy of the struct (~32 KB
target). Restore = copy back. Hash = xxHash64 over the struct bytes. `MatchState` uses
sequential layout with fields ordered by size, and a CI test asserts `sizeof(MatchState)`
equals the sum of its field sizes. That proves there is no padding, which would otherwise be
hashed as garbage.

### 2.4 Physics

Custom solver, kept small by design:

- Shapes: sphere, capsule, box, convex hull (≤ 16 planes) for static and kinematic stage
  geometry.
- Actors are capsules with a separate ground probe; items are spheres or boxes; projectiles
  are swept spheres.
- Integration: semi-implicit Euler, sequential impulses with 4 iterations, fixed restitution
  and friction per material (ice stages set friction on the stage material).
- Kinematic bodies follow authored tracks (`Data`) and push dynamic bodies; crush detection
  when a dynamic body is squeezed between two kinematics or a kinematic and static.
- Triggers: pickup rings, hazard volumes, phase-transition volumes, fall-out volumes.
- Broadphase: uniform grid sized per stage; stage bounds are authored.
- All pair lists are processed in a canonical order (lower slot index first) so contact
  resolution order never depends on insertion history.

Not in the sim: cloth, particles, ragdolls, cosmetic debris, hair, banners flapping. These are
presentation and may use the engine's physics freely because they never influence a sim
result.

### 2.5 Actor kits as data

A fighter kit is a `Data` record: movement constants, a move table (startup/active/recovery,
hitbox primitives per frame, damage, stun, knockback vector, cancel rules, `otg` flag, Luster
cost), base and Blitzed tables, Surge variant selectors. The sim runs the move table; it
has no per-fighter code. Fighter-specific behavior that cannot be expressed in the table
(Sable's vanish, Pip & Pepper swap, Clank's double hold, Lune's riposte, Bulwark's carry) is
a small set of named **kit behaviors** registered by enum, so the exception list stays
visible.

### 2.6 CPU brains

CPU fighters run inside `Sim`. Before each `Step`, every CPU slot's brain reads `MatchState`
and writes that slot's `InputFrame`, using its own RNG stream. Because brains are in the sim:

- Online lobbies can fill empty slots with CPUs and hand a disconnected peer's fighter to a
  CPU at an agreed frame, with no extra traffic (`modes.md` §7).
- Replays, determinism CI, and the economy simulator run CPU matches headless and
  reproducibly.

Budget: ≤ 0.03 ms per brain per step. Brains think every 2nd frame on a staggered schedule
and hold their output in between, which also bakes in a minimum reaction delay.

### 2.7 Stage graph runtime

Consumes the exported phase graph (`stages.md` §1). Phase timers, transitions, hazard
triggers, and kinematic tracks are all stepped inside `Sim`. Scripted camera paths are
exported alongside but consumed by Presentation.

## 3. Rollback

- Session of 2–4 peers in a full mesh, peer-to-peer, with the host used for lobby
  authority only. Inputs are broadcast to every peer, with redundancy (each packet carries
  the last 8 unacknowledged frames).
- **Input frame.** Presentation quantizes each player's input before it enters the sim:
  stick angle to 32 directions (5 bits), magnitude to 4 bands (2 bits: neutral < 25%, walk,
  run, full > 80%), and six logical buttons (Jump, Attack, Action, Strong, Surge A,
  Surge B). That is 13 bits, sent as 2 bytes. Quantization does two jobs. It makes
  "repeat last input" prediction correct far more often, since a raw analog stick changes
  value almost every frame. And it makes the dodge flick thresholds (`combat-and-match.md`
  §4) exact and deterministic. Remapping happens before quantization, so the sim only sees
  logical buttons.
- **Input delay.** 2 frames by default. The lobby negotiates 0–4 from measured round-trip
  times: `ceil(max one-way latency / 16.7 ms)`, clamped. Rollback window 8 frames.
- Prediction: repeat last input.
- On a late input: restore the oldest affected snapshot, re-step to present, and re-fire
  presentation events by diffing a per-frame event buffer (hits, pickups, phase changes), so
  audio and VFX are not double-played.
- **Stalls.** If a peer's inputs are more than 8 frames behind, the local sim waits (shown as
  a small "waiting" icon) rather than predicting further. After 3 s the lobby shows the
  disconnect timer, and at 10 s the peer is replaced by a CPU (`modes.md` §7).
- Desync detection: peers exchange `Hash()` every 30 frames. A mismatch flags the match,
  dumps both `MatchState`s and the input log to disk, and (in development) halts. Telemetry
  reports desync rate.
- **Transport.** Steamworks Networking Sockets over Steam Datagram Relay on PC. It provides
  NAT traversal, relay fallback, and IP privacy without running our own relays. Console
  transports are wrapped behind the same interface when ports begin.

Replays are `MatchConfig` + input log. Replay viewer, spectating, determinism CI, and the
economy simulator all consume that same format.

## 4. Data pipeline

- Authoring: ScriptableObjects in the Unity project for items, fighters, stages, House Rules,
  prices, unlock conditions, Tinker tables, Facet Wheel bands.
- Export: a build step serializes to a versioned binary blob per table with a content hash.
  All fixed-point values are converted from authored decimals at export, using exact
  decimal-string parsing (never via `float`). `Sim`, `Meta`, and `Tests` load blobs; they
  never load ScriptableObjects.
- Content hash is part of the lobby handshake: mismatched content cannot play together.
- Stable ids: `item_id`, `fighter_id`, `stage_id` are integers allocated from a registry file
  and never reused.

## 5. Save and profile

- Profile is a versioned, forward-migratable record (`schema_version` + migration chain in
  `Meta`). Every alpha schema version is kept as a test fixture so M4 can prove migration from
  each.
- Written atomically (temp file + rename), with one rolling backup. Tinker and Facet Wheel
  commits save before the result is shown (`items-and-economy.md` §4).
- Cloud sync via platform APIs; conflict policy is "newest wins, keep loser as backup", with
  the backup restorable from the Profile menu.
- Cheating in a single-player economy is the player's business; there is no server authority
  at 1.0. Online Custom has no progression writes, so a modified profile only changes the
  host's crate table, which everyone sees before ready-up.

## 6. Camera

Presentation-side solver reading the latest snapshot, interpolated between sim frames:

1. Gather targets: living actors, plus weighted recent-event points (hits, jewel spawns).
2. Compute the bounding sphere; pick a distance along the phase's camera volume that fits it
   with the authored margin.
3. Clamp to the camera volume; if the fit fails, pull to maximum and assign edge bubbles to
   off-screen targets (`combat-and-match.md` §12).
4. Smooth with a critically damped spring; snap on phase transitions that provide a scripted
   path. Reduced-motion setting lowers the spring stiffness and disables snap-zooms.

Camera volumes and scripted paths are authored per phase in the stage editor.

## 7. Presentation

- Rendering at the platform's native refresh (60, 120, 144 Hz) with interpolation between the
  two latest sim frames; the sim stays at 60 Hz. Interpolation adds up to one sim frame of
  display latency, so a **Low Latency** option renders the latest sim frame directly at
  60 Hz with no interpolation.
- Each sim pool has a presentation pool of the same cap; objects bind by stable slot index so
  rollback restores are visual no-ops when state converges.
- Hit effects and audio fire from the per-frame event buffer, not from state diffs, so
  rollback can suppress duplicates.
- HUD: four horizontal Life bars with jewel pips, Dross and immunity indicators, and the
  Luster sub-bar; placement badges, held item icon, Rally ring, Squall warning, edge bubbles,
  target line (Assists), assist icons, phase banner and countdown.
- **Photosensitivity.** No effect may produce more than three general or red flashes in any
  one-second period (the WCAG 2.3.1 / broadcast-guideline threshold). Lightning, electric
  attacks, explosions, and Blitz transitions are designed to this from the start, not
  patched at certification.

## 8. Tools

| Tool | Purpose | Milestone |
| --- | --- | --- |
| Stage graph editor | Phases, geometry sets, points, hazards, gimmicks, transitions, camera volumes; live preview in-editor | M1 |
| Item and crate editor | Item schema editing; crate table preview for a given synthetic profile and stage, including per-category share | M2 |
| Fighter move editor | Frame data, hitbox primitives per frame, Surge variants; hitbox overlay in play mode; budget warnings against `balance.md` | M1 |
| Balance sheet export | Frame data, damage, Luster cost, and damage-per-Luster for every move, diffable per build | M1 |
| Economy simulator | Headless Expedition runs with CPU fighters; reports carat curve and carats per minute by difficulty and stage, item handling rates, Tinker and Facet Wheel outcomes, and arbitrage-loop checks across N synthetic profiles | M2 |
| Replay viewer | Scrub, hitbox overlay, state inspector, desync compare of two dumps | M1 |
| Determinism CI | Runs the recorded match corpus and compares hashes; blocks merge on mismatch | M0 |
| Unlock test harness | Drives synthetic profiles through each unlock condition | M2 |
| Flash analysis | Renders replays of every Surge, Squall, and boss attack and runs automated flash detection | M3 |

## 9. Testing strategy

- **Sim unit tests:** move table resolution, dodge window, flick detection and lockout, chord
  detection, throw priority and throw break, chain ceiling, getup invulnerability, status
  immunities, Luster drain, jewel circulation cap and Dross, Blitz on third jewel and
  suppression, Rally rules, Squall schedule and targeting, draw resolution.
- **Determinism:** corpus of recorded matches (grows from every playtest) replayed in CI.
  The corpus runs in three places: the headless .NET test runner on Windows, the same
  runner on Linux, and the **shipping IL2CPP player** in batch mode on both OSes. IL2CPP
  turns C# into C++, which is compiled separately, so "the tests pass" is not evidence that
  the shipped binary is deterministic. Any hash divergence is a P0.
- **Crate table:** property tests over random profiles and stages (owned appears, unowned
  never, `never` never except pocketed `pocketable`, weights normalize, category share stable
  as the table grows, seed stability).
- **Tinker:** distribution tests over 100k rolls per tier against the target probabilities;
  Blueprint Scrap and Tincture shifts verified; integer rounding verified at every
  half-tier.
- **Economy:** simulator output within tolerance of targets in `items-and-economy.md` §7, and
  every invariant in `balance.md` §10.
- **Unlocks:** every row in `modes.md` §8.
- **Cosmetics:** hitbox data identical with and without each cosmetic attached; silhouette
  volume within the limit in `cast.md` §7.
- **Netcode:** soak tests with simulated latency, jitter, and loss; rollback depth
  distribution, stall rate, and desync rate tracked per build; disconnect-to-CPU handoff
  tested at random frames.
- **Save:** migration from every stored schema fixture; atomic write failure injection.

## 10. Minimum spec and performance targets

- PC minimum: 4-core CPU of 2018-class performance, 8 GB RAM, DX12/Vulkan GPU of 2016-class
  performance. 1080p60 at low settings. Steam Deck is a named target (Verified) from M4.
  It is the 60 fps floor in practice, and a natural two-player couch device.
- **Sim budget.** The worst case each displayed frame is one new step plus eight rollback
  re-steps: nine steps. With a 16.7 ms frame and a 12 ms presentation budget, the sim gets
  about 4 ms. So:
  - single `Step` including CPU brains ≤ 0.35 ms on minimum spec;
  - nine steps plus nine snapshots ≤ 4 ms.
  - v0.1 set the single step at 2 ms, which makes nine steps 18 ms, more than a whole frame.
- The sim runs on a dedicated thread from M1. Presentation reads a double-buffered snapshot,
  so a rollback spike delays the next sim frame, not the render.
- Presentation frame ≤ 12 ms at minimum spec.
- Memory: match working set ≤ 1.5 GB including the largest stage's phase variants resident.

## 11. Build and CI

- `main` always builds a playable Custom match. Feature branches gate on: compile, analyzers
  (§2.2, and no Unity refs in `Sim`/`Data`/`Meta`), unit tests, determinism corpus on the
  .NET runner.
- Nightly: determinism corpus on IL2CPP players (Windows, Linux); economy simulator and
  arbitrage checks; netcode soak; content export validation. Export validation checks every
  stage phase within pool caps in its worst case, every item has a valid category, price
  band, and rarity power band, every tag is canonical and has exactly one material and one
Tincture, and every fighter has Flare, Surge A, and Surge B within the `balance.md` budgets.
