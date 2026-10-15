# Stages

Owner: stage designers, stages gameplay engineer. Eleven arenas: five story, two boss, four
extra. Story and boss arenas are **phase graphs**: a set of phases with timed or triggered
transitions, each phase swapping colliders, spawn points, hazards, camera volumes, and crate
tables. Extra arenas are single-phase.

## 1. Phase graph model

Authored in the stage editor, exported as data. Runtime lives in the sim assembly.

```
Stage
  id, name, fall_is_ko, player_count_variants[2,3,4]
  jewel_cap_override      # optional; e.g. Pressworks = 6
  tags[]                    # stage-select labels: "favors aerial", "tiny", "water"
  phases[]
    id
    duration_seconds        # null = until end
    geometry_set            # which colliders/kinematic tracks are active
    spawn_points[]          # actor respawn
    jewel_points[]
    crate_points[]          # with respawn timer and optional table override
    hazards[]               # hazard id, params, trigger (timer | button | proximity | phase_enter)
    gimmicks[]              # boardable / grabbable objects
    stage_weapons[]         # stage-only items that never enter inventory
    blitz_suppressed        # bool; Pressworks sorting line only
    camera_volume
    transitions[]
      to_phase
      trigger: after_seconds | on_event | on_actor_count
      transition_seconds    # 0 = instant; > 0 = scripted, sim keeps running
      scripted_camera (optional)
      carry_items: all | held_only | none
      carry_jewels: all | drop_chance(p)
Boss stage adds:
  boss_id, timeout_seconds, timeout_move, timeout_starts_at_phase
```

Rules:

- Transitions are deterministic. Random elements use the match seed through the sim RNG.
- `carry_jewels: drop_chance(p)` is how a stage can make jewels fall out of inventory
  during a transition (used in Citrine Bazaar's Breakaway).
- Player-count variants may remove or add geometry within a phase (Fulgur Cloister), but
  the phase list is identical across variants so the clock reads the same.
- Every phase has at least one jewel point and one crate point. No phase lasts less than
  20 s. Short scripted moments (the Monastery landslide) are **transitions** with
  `transition_seconds`, not phases. During a transition the sim runs, fighters can act, and
  hazards are off.
- **Timing rule.** Match length targets are in `balance.md` §1 (Tournament FFA median
  65–80 s). The final phase of every story arena must begin by 55 s at Phase Clock 100%, so
  that at least 80% of Tournament FFAs reach it. House Rule 7 (Phase Clock) scales every
  `duration_seconds`.
- Hazards have a telegraph of at least 30 f before they deal damage. Stage-volume kills
  (crush, fall-out in `fall_is_ko` stages) are the only untelegraphed damage and only exist
  where the arena's whole premise is the drop.
- Every phase change is announced: a two-beat audio sting, a banner with the phase name, and
  a 3 s countdown in the HUD before timed transitions. Players who know the graph have an
  edge, and players who do not still get a warning.
- A **water volume** is a region, not a stage theme. Standing in one slows walk speed where
  the phase says so, and it is what Seam Charge reads (`items-and-economy.md` §8). Water
  volumes exist in Sunken Strongroom phases B and C, Rime Cutworks phase B, and The Cascade.

## 2. Story arenas

Tournament and Expedition run five stages: two story FFAs, the Harvester, one story FFA, and
the Pressworks (`modes.md` §3). The first two FFAs rotate through {Citrine Bazaar, Fulgur Cloister, Sunken Strongroom}; the fourth stage alternates between {Rime Cutworks, The Orrery}.
Duel visits all five story arenas plus both bosses.

### 2.1 Citrine Bazaar

A floating market of gem stalls, chained over open air on the outer Ring. The vertical-slice
stage, and the first stage on a fresh profile. Accent: festival light and a little flame.

| Phase | Duration | What changes |
| --- | --- | --- |
| A. Bazaar | 25 s | Stalls chained tight; light-ropes and awnings. Stall counters are crates. Vendors toss candied drops (healing) onto the walk every 10 s. Two post pillars (grabbable). Awnings are springable walls. |
| B. Breakaway | 25 s | A gust snaps the chains. Stalls drift and collide on kinematic tracks. Collisions are telegraphed by a creak and a ripple of light 40 f ahead, and knock down anyone on the colliding edges. Awnings become springboards that launch toward the arena center. The transition into B has `carry_jewels: drop_chance(0.5)`: jewels spill, hang in the air, and can be collected until they drift to the edge, where they return to a jewel point. |
| C. Geode shelf | until end (from 50 s) | The stalls are drawn onto a raw geode shelf. Crystal spars are pillars; a split geode is a swinging platform. Gimmick: a spike turret on the shelf (boardable, 3 shots, 8 s reload, 90 damage, Pin). |

Expedition material drip, separate from the crate table: Flame, Ice, Iron, and Gunpowder
drop from broken containers, weighted up. Materials are `never` on the crate table
(`items-and-economy.md` §2).

### 2.2 Fulgur Cloister

A cliffside cloister in a lightning storm. Layout differs by player count: the 2-player
variant closes the lower terrace, and the 4-player variant opens the bell tower roof.
Accent: storm.

| Phase | Duration | What changes |
| --- | --- | --- |
| A. Courtyard | 40 s | Storm gutters fill and flush on a 15 s cycle, sweeping loose items along a channel. A gong (hit with Attack) rings the **Weightless Toll**: gravity drops to 30% for every actor and loose item for 4 s (20 s cooldown). Jumps go high and throws carry far. Prayer flags are springable walls. Stage weapon: a ceremonial staff racked at the gate (long reach, 6 hits, 45 each). |
| Landslide | transition, 4 s | The hillside gives way and everyone slides into the library. `carry_items: held_only`. Scripted camera. |
| B. Library | until end (from 44 s) | Sliding bookcases on kinematic tracks crush against pillars (telegraphed by a creak and dust, 45 f). Lantern spirits drift through and drop a random crate-table item when hit. A rope bridge across the reading well collapses 12 s into the phase, splitting the floor into two ledges connected by the bookcase tops. Stage weapon: an incense cannon from the shrine door (3 smoke shots that cause Dizzy). |

v0.1 had the gong reverse gravity in the open-air courtyard. With no ceiling, that sends
every fighter into the sky for 3 s. Low gravity keeps the gong's surprise and keeps everyone
in the arena. The bridge collapse used to be 25 s into the library, about 69 s into the
match, so most Tournament free-for-alls ended on a whole floor. At 12 s the split is the
phase's shape, and it still lands after the 55 s final-phase rule.

### 2.3 Sunken Strongroom

A gem vault in a cavern House Vitre flooded. The air pocket shrinks. Accent: tide. This is
the water story stage.

| Phase | Duration | What changes |
| --- | --- | --- |
| A. Vault floor | 25 s | Safe-deposit drawers open when hit: most Stun the opener for 20 f, some drop a jewel (subject to the cap), and one in eight drops a gleam pickup in Expedition. A Tidewight in a bottle on the counter: hit it and it either heals everyone nearby (200) or inks the area (Dizzy), 50/50 on the seed. |
| B. Vault door | 25 s | The great round door detaches and rolls around the room on a track: a rotating hazard that knocks down on contact (120). Water rises 1 m, a water volume, making the lower floor slippery and slowing walk speed 20%. |
| C. Hull breach | until end (from 50 s) | The hull gives way. A **tidal pull** chase: the floor tilts and the current drags toward the breach; fighters must keep moving against it or spring off pillars. Fall-out at the breach deals 150 and respawns. **Teller's Reel**: a jackpot reel mounted on the teller window spins when hit (15 s cooldown) and pays food, bombs, gleam pickups (Expedition), or a jewel (subject to the cap). The breach and the risen water are water volumes. |

### 2.4 Rime Cutworks

An ice cutting yard on a shelf of rime. Slippery surfaces everywhere: friction coefficient
0.4, 20% longer stops. Accent: ice.

| Phase | Duration | What changes |
| --- | --- | --- |
| A. Dry yard | 25 s | Three boardable **cutter sleds** on rails with different loadouts: cannon (slow, knockdown, 120), spike caster (fast, Pin, 70), and a drop-crane (lifts and drops a crate or a nearby actor for 100). A boarded sled ejects its pilot after 12 s or on a Strong hit. Ice blocks are throwable items (heavy, one throw, 90, shatter into a slip patch). |
| B. Flooding | 25 s | The yard floods. Sleds float free and drift. Floating ice rafts become the floor; rafts tip if three actors stand on one. The flooded floor is a water volume. |
| C. Crane yard | until end (from 50 s) | Overhead magnet cranes sweep the yard on a cycle; a magnet lifts any metal item and any actor holding one, carries them 6 m, and drops them. Rideable crane hooks (board with Action). |

### 2.5 The Orrery

House Vitre's star-tower: a caged orrery that predicts when the Heart Diamond will shed, so
the hauls arrive first. The last story arena before the Pressworks. Accent: star.

| Phase | Duration | What changes |
| --- | --- | --- |
| A. Gear floor | 25 s | Updrafts lift between three rings (step in, Jump to ride up). A conveyor crosses the floor; a **lever** reverses its direction and doubles its speed for 10 s. Hanging gondolas (board with Action; they traverse the room). |
| B. Infestation | 30 s | A **void beetle**, a giant beetle that has been eating the tower's star-ore, breaks in. It has a **spit** attack that leaves an adhesive puddle (Slow), a **tail swipe** knockdown (100), and it lays **egg sacs** that hatch into young which chase the nearest actor (30 per bite, 3 young max). Egg sacs are throwable items and burst on impact. The adult has 1200 Life. The first bonus jewel drops at 50% Life. The second drops on kill. If it is alive when phase C begins, it flees and drops nothing further; a jewel already dropped at 50% stays. |
| C. Planet disc | until end (from 55 s) | The floor rises on the central lift into the orrery's planet disc. Rotating arms outside; the room is a disc with a slow spin and a gap where an arm passes (fall-out, 150). Gusts every 12 s push toward the gap (telegraphed by vane-lights, 60 f). |

The beetle's jewels are bonus jewels (`balance.md` §5), so fighting it creates a second
Blitz opportunity without breaking the circulation cap. A kill still pays both jewels, one
at half Life and one at the end. A lobby that only wounds the beetle, or that lets it flee
after the first drop, does not walk away with both. The incentive for the whole lobby to
turn on the stage creature for a moment is still pillar 2 at its best.
The inventory toy Brood Gem is the egg sac's cousin and does not spawn a young
(`items-and-economy.md` §8), so it cannot break the stage-creature cap.

## 3. Boss arenas

Boss arenas count as teams for every rule (Rally applies). In Tournament the two advancing
fighters cooperate. In Expedition the runner-up CPU from the previous FFA joins as an ally at
difficulty 3 and above; at difficulty 1–2 and in Duel the fight is solo. In Custom, 1–4
fighters can take on a boss. Boss Life and every breakable part scale by difficulty and
participant count (`balance.md` §8). A boss timeout ends the attempt with a stage-wide kill
move. Tournament grants one retry of that stage; in Expedition the run ends.

### 3.1 Terraces — The Harvester

Quarry terraces at dusk, the Crown Vein cut open. Spine crystals grow out of the faces.
A walking excavator the size of a house, crewed by masked House Vitre **Cutters**.

Boss state machine:

- **Patrol.** Walks the terrace loop. Each footfall is telegraphed by a shadow 30 f before
  it lands and deals 400 to anyone under it (dodgeable).
- **Intake.** The front intake spins and drags anyone in a 4 m cone toward the maw (300 and
  a throw backward). Telegraph: intake spin-up sound, 40 f.
- **Beam.** The cutting laser sweeps a line across the arena: **instant KO**. Telegraph: a
  red line on the ground for 60 f. Jumping over it, dodging it, or springing off a terrace
  wall avoids it. At Expedition difficulty 1–4 it deals 500 instead.
- **Stomp.** Jumps and lands: everyone grounded within 8 m is Dizzy for 90 f.
- **Spray.** Abrasive dust from the side nozzles; 50 per second while inside. It darkens
  the area, but fighters stay visible as silhouettes.
- **Buck.** While any fighter is mounted, the Harvester shakes every 6 s (40 f telegraph);
  mounted fighters who are not hanging on a grab point are thrown off for 80.

Breakables (base values, before scaling): four legs at 500 each; the head housing (1200),
exposed while two legs are broken; and the tail controller (1000), mountable. To reach the
tail controller, break one leg, climb the slumped side, and the operator's cab is in reach.
While any leg is broken the Cutters lean out and fire gravel-shot projectiles (80 each).
**Broken legs are repaired 30 s after they break** unless the fight has been won.

Routes:

| Route | Damage needed | Risk | Fits |
| --- | --- | --- | --- |
| Steady: two legs, then the head | 2,200 | On the ground with Stomp, Spray, and footfalls; both legs must fall within 30 s of each other | Patient pairs, low difficulty |
| Burst: one leg, mount, tail controller | 1,500 | Bucks every 6 s, Cutters focus the mounted fighter, 30 s window before the leg is repaired | A Blitzed fighter on the tail |

In v0.1 the tail route needed 1,300 damage against the head route's 2,500, with no added
risk, so it was the only route anyone would take. The repair timer and Buck make the burst
route faster but conditional.

Timeout: 150 s. **Blade Sweep**: the harvester's main blade sweeps the whole arena at
ground height. It is unavoidable and ends the attempt.

Jewels spawn from broken crates on the terraces. Blitz is allowed and intended, and
the jewel cap is 6 so both fighters can Blitz together.

### 3.2 The Pressworks — Chairman Vitre

House Vitre's smelting plant. Jewel cap 6.

Phase A, **Sorting line** (until the gate opens): a long hall with a conveyor and hanging
poles (grabbable pillars, horizontal: hang and swing). Vein Guard automatons patrol, each
built around one weapon type: mortar guard, frost-staff guard, mallet guard, net guard, and
flame guard (300 Life each, scaled). Destroying a guard drops its weapon as a stage weapon
(never enters inventory). Jewels spawn here **but the phase is `blitz_suppressed`**: a
third jewel is held, the pips flash, and the fighter Blitzes on the first frame of phase B.
The gate opens when four guards are down or 90 s elapse.

Phase B, **The Press**: a circular floor under a hydraulic press head. Vitre fights from a
mechanized chair fed by Dullglass.

Vitre's moves: **Swing-and-grab** (a long arm sweeps; the grabbed actor is held and
squeezed for 200, then thrown; breakable in the first 10 f), **Pistons** (three ground
stabs, 150 each, telegraphed by floor cracks for 45 f), **Spew** (a poison arc that leaves a
pool, 40 per second inside), and **Live Hands** (electrified palms that chase one actor for
6 s, 120 on touch; a touch chains to any actor within 1.5 m for 60). Below 30% Life he adds
**Overpress**: the press head drops on a ring that shrinks toward the center over 3 s, the
one instant KO in the fight. It is telegraphed by the ring itself, and once per 30 s at most.

Vitre has 6000 base Life and no breakables; the fight is attrition. The floor's drains are
crate points that mostly pay food. Blueprint Scraps and Tinctures drop only here in
Expedition, on the container stream, not on the crate table.

Timeout: 180 s, counted from the start of phase B (`timeout_starts_at_phase`), so a slow
sorting line does not eat the boss clock. **Flood**: the Press floods the floor with Dullglass; unavoidable.

## 4. Extra arenas

Custom mode only. Single-phase. Reachable by scrolling the stage cursor past the end of the
list until the **Extra Arena** box appears (the box is visible but locked before unlock).
Unlock conditions in `modes.md`.

### 4.1 Terraces (no boss)

The Harvester arena with the harvester gone. **Spine crystals** are the interactive
objects: hitting one fires a volley of spines in the facing direction (3 × 25); throwing an
actor into one adds 100 damage. Open, flat, many jewel points: the neutral testing ground,
and the default stage for the balance acceptance tests in `balance.md` §9. Sparring loads
this geometry from the first boot without unlocking the Custom stage entry (`modes.md` §4).

### 4.2 Behind the Counter

Tilly's Bench with the counter shoved aside. Tiny: 8 m × 8 m with shelves on three walls.
A climbable lamp standard in the middle (pillar, hang and climb). Shelves drop their
contents when hit. Almost no room to dodge; the stage for people who want pure scrap.
Tagged "tiny".

### 4.3 Spirewalk

Spar bridges and guy-lines between gem spires, over a star-well at night. `fall_is_ko`.
Narrow spars, a few banners to spring from, and one nest platform with a jewel point. Every
spar has a net strung 4 m below on one side, so a knocked-off fighter with a double jump or
float has a recovery route. Tagged "favors aerial" (Wren, Delphine, Tilly).

### 4.4 The Cascade

Crystal crates stacked in three tiers over a falling cascade, gaps between stacks.
`fall_is_ko` into the water. Wall springs between stacks are the primary movement. The gaps
are a water volume. Crates on the top tier are real crates and respawn. Tagged "favors
aerial", "water".

## 5. Stage authoring budget

Per story arena: 3 phases average, 6–10 jewel points, 6–8 crate points, 2–4 hazards, 1–3
gimmicks, 0–2 stage weapons, 3 player-count variants. Each phase must fit the sim object caps
(12 loose items, 2 summons, 16 kinematic bodies, 32 hazard volumes, 8 stage creatures)
because rollback snapshots size against these caps. The content export check in
`architecture.md` §11 fails any phase that can exceed a cap, including through the worst
case of Tilly's Closing Time and the Teller's Reel.
