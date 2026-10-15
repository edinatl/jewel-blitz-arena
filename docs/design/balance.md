# Balance Framework

Owner: combat designer, economy designer. This file is the single source of truth for the
numbers every other spec is tuned against: Life, damage, Luster, time-to-KO, status control,
item power, boss kill times, and the acceptance metrics that decide whether a build is
balanced. Other specs state per-move or per-item values; this file states the budgets those
values must fit. When a spec and this file disagree, fix the spec or reopen the budget here,
deliberately.

All values are M1 starting points. They are expected to move, but they move as a set: a
change to the damage budget is a change to time-to-KO, which is a change to stage phase
timing.

## 1. Design targets

| Target | Value | Why |
| --- | --- | --- |
| Tournament / Expedition FFA length (ends at two left; see `modes.md` §3) | median 65–80 s | Phase C starts at 50–55 s; players must see it |
| Custom 4-actor FFA to last standing | median 80–95 s | 99 s timer is the soft cap, Squall the hard cap |
| Duel (1v1) | median 45–60 s | Learning mode; short |
| Matches that reach Squall | ≤ 10% | Squall is a tiebreak, not a phase |
| Matches that end in a draw | ≤ 2% | |
| Tournament FFAs that reach the stage's final phase | ≥ 80% | Pillar 2 |
| Time a first-KO'd human waits (Custom FFA) | median ≤ 40 s | Party pillar; Wisps cover the rest (`combat-and-match.md` §10) |
| Damage share by source, 4-actor FFA | fighter normals 45–55%, items 20–30%, Flare + Surges 15–25%, stage + Wisps 5–10% | Contention pillar: items and stage must matter, but the fighter is still the main tool |

The damage-share band is the most useful single health check. If items fall under 20%,
crates are too sparse or too weak. If Surges exceed 25%, Blitz is deciding matches on
its own and the jewel race has become the only race. Measure it on a Custom 4-actor
free-for-all with default rules, Wisps on. Tournament sits at the low end of the stage
share because Wisps are off there.

## 2. Life and weight

Every fighter has an integer max Life and a knockback multiplier. The Life bar on the HUD
shows percentage of max; the fighter select screen shows the absolute number.

| Life rating | Max Life |
| --- | --- |
| 1 | 850 |
| 2 | 925 |
| 3 | 1000 |
| 4 | 1075 |
| 5 | 1150 |

Knockback taken is ×1.0 except where a kit says otherwise (La Cumbre ×0.9). Weight does not
affect damage.

## 3. Damage budget

Values are at 100% attack against a 1000-Life fighter. Integer math throughout; scaled
damage is `(base × percent + 50) / 100` (round half up), applied once per hit.

### 3.1 Base kit

| Move class | Damage per hit | Notes |
| --- | --- | --- |
| Attack (each hit of A, AA, AAA) | 30–40 | Full string 100–120 |
| Strong | 80–100 | Launches or knocks down |
| Air Attack / Air Strong | 40–50 / 70–90 | |
| Dash Attack | 60–80 | |
| Throw | 100 | +60 to anyone hit by the thrown body |
| Pillar charge | as Strong | |
| Getup attack | 40 | |

The **Power** rating in `cast.md` §6 scales these by 0.85 / 0.92 / 1.0 / 1.08 / 1.15 for
ratings 1–5. The scale is applied to the kit's authored table at export, not at runtime.

Two Strongs are exempt from the 80–100 band and from the Power scale. They are authored as
final values, and export validation accepts the exemption only with the tag:

- `utility_strong`: displacement with no damage. Delphine's sonic cone. The trait budget
  already paid for it. Hob's pan is not in this set. It deals 80 and reflects, and the
  Power scale applies to the 80.
- `ranged_strong`: a shot with a magazine. Ferro's shot is 60. It is safer than a melee
  Strong, so it sits under the band on purpose.

### 3.2 Combo ceiling

A chain is hits that land while the target is still in hit-stun. The chain counter lives on
the **target**, not the attacker, so three fighters ganging up on one share the same
ceiling (`combat-and-match.md` §5.2).

- Each hit in a chain reduces the next hit's stun by 15% and raises its knockback by 15%.
- At chain length 6 **or** 220 chain damage, whichever comes first, the next hit forces a
  knockdown.
- Expected chain damage is 120–180. Anything that reliably exceeds 220 before knockdown is
  a bug.

### 3.3 Blitzed kit

Luster is an integer, 0–1200. With the Luster timer on it drains 1 per frame, so a full gauge
lasts exactly 20 s.

| Move class | Budget |
| --- | --- |
| Flare, full string | 120–160 total; no Luster cost |
| Surge, single target | 280–330 |
| Surge, area (hits several) | ≤ 220 per target, ≤ 600 total if all three opponents are hit |
| Surge, utility (control, pull) | ≤ 80 damage |
| Surge, heal | 280–330 Life restored; priced as a single-target Surge, not capped at 80 |
| Standard Surge cost | 420 Luster (35%) |
| Repeatable Surge cost (Tilly's Invoice only) | 180 Luster per use (15%), damage scaled to the same damage-per-Luster ratio |

The area ceiling is twice the single-target budget because area Surges rarely catch
everyone. They land on about 1.5 targets on average, which puts their expected damage close
to a single-target Surge's. Telemetry checks that average at M4; if area moves start
reliably catching three, their per-target values come down.

Damage per Luster for a standard Surge is about 0.7 (300 / 420). Repeatable Surges and
damage variants stay within ±10% of that ratio. That is the rule that keeps "repeatable"
from meaning "best". A variant tagged utility is held to the utility budget instead, and
is not required to hit 0.7. That is what keeps a mine or a magnet from being retuned into
a second damage Surge.

A full gauge buys two standard Surges with Flare time around them. With the Luster timer
off, a Surge may be started with any Luster above zero and empties the gauge; that caps a
timerless Blitz at three Surges.

### 3.4 Stage, boss, and sudden death

| Source | Damage |
| --- | --- |
| Fall-out (non-`fall_is_ko` stage) | 150, plus all held jewels lost |
| Telegraphed stage hazards | 80–200 |
| Crush (kinematic squeeze) | 300 |
| Squall strike | KO regardless of Life, damage scaling, or Blitzed state |
| Boss attacks | per `stages.md` §3, 80–600. At most one telegraphed instant KO during the fight. The timeout move is a second KO and fires only when the clock runs out. |

## 4. Status effects and control

Every status is one of the classes below, so items, stage pieces, and kits share one set of
rules. Durations are at full strength.

| Status | Effect | Max duration | Immunity after |
| --- | --- | --- | --- |
| Stun | Can't act; brief (drawer jolt, Bateria) | 60 f | none, but counts toward the chain |
| Dizzy | Can't act; mashing shortens by up to 30% | 90 f | 300 f of dizzy immunity |
| Knockdown | Grounded; see getup rules | 30 f minimum | 20 f getup invulnerability |
| Pin | Rooted in place; can still attack | 75 f | 240 f of pin immunity |
| Freeze / Petrify | Can't act; takes 50% damage; a hit for 100+ breaks it early | 90 f | 240 f of freeze immunity |
| Slow | −40% move speed | 240 f | none |
| Blinded | Can't pick up, catch, or board; auto-facing off | 120 f | none |
| Carry / grab | Held by an opponent; mash to escape up to 50% sooner | 60 f | — |

Rules:

- **Hard-control cap.** No single source may disable an actor (stun, dizzy, freeze, pin,
  grab) for more than 120 f. Dizzy, pin, and freeze immunities stop chain-locking in
  four-player matches, where many sources overlap: Stomp, Smoke Vault, Confetti Bomb,
  Loudhailer, Thriller Novel, incense cannon, Tidewight ink.
- **Vision effects are world-space.** Smoke, ink, abrasive dust, and dimming darken the arena
  locally, but actor silhouettes and player-color outlines always stay visible. On a shared
  screen nothing can blind one player without blinding all four, so "blind" is a mechanical
  status (Blinded above), never a camera effect.
- The HUD shows an immunity halo on any actor who is currently immune, so players can read
  why their dizzy did not land.

## 5. Jewel economy and anti-snowball rules

The jewel race is the core loop, and it must not snowball. These rules are what make it
self-correcting.

1. **Circulation cap.** Jewels on the field plus jewels held by unblitzed actors are at
   most `jewel_cap`. Jewels locked in a Blitzed actor count toward the cap until they
   respawn. So the maximum number of fighters who can be Blitzed at once is
   `floor(jewel_cap / 3)`.
2. **Default cap by actor count.** 2 actors: 3. 3–4 actors: 5. That allows one Blitzed
   fighter at a time, with two loose jewels left for the others to fight over. House Rule 2
   overrides with 3–7, where 6–7 allows two Blitzed fighters at once. The reference's
   default of 7 is reachable but not the default; see `PLAN.md` open decision 7.
3. **Opening.** In a 2-actor match each fighter starts holding one jewel and one spawns at
   3 s, so the first fight is immediately over the third (as in the reference). In 3–4 actor
   matches nobody starts with one; two spawn at 3 s, then one every 5 s until the cap is
   reached.
4. **Loss.** A knockdown drops one held jewel. A fall-out drops all of them; they
   reappear at jewel points after 3 s.
5. **Revert.** When a Blitzed fighter reverts, their three jewels respawn 4 s later at
   the jewel points farthest from that fighter (ties broken by point index).
6. **Dross.** A fighter who just reverted cannot collect jewels for 6 s. This stops
   back-to-back Blitzes by whoever happened to be standing nearest the respawn.
7. **Bonus jewels.** Jewels from items (Jewel Catalog, Solitaire) are outside the
   cap. They count as pips and are consumed by Blitz, but they never respawn.

## 6. Item power bands

Rarity means rarer, and it also allows a higher ceiling. The bands set that ceiling.

| Rarity | Damage per use | Lifetime damage per pickup | Healing |
| --- | --- | --- | --- |
| 1–2 | ≤ 80 | ≤ 200 | 80–200 |
| 3–4 | ≤ 120 | ≤ 350 | 250–350 |
| 5 | ≤ 180 | ≤ 450 | 450 |
| 6–7 | ≤ 300 | ≤ 500 | full (Elixir) |

Hard rules for every item, at any rarity:

- No item KOs from full Life, and no item deals Squall-style KO damage.
- No item grants more than 60 f of invulnerability or bypasses the status immunities in §4.
- No item disables for more than the hard-control cap.
- Summons have their own Life and count toward the summon cap (2).

## 7. Fighter stat budget

`cast.md` §6 rates each fighter 1–5 on Speed, Life, Reach, Air, and Power. The five ratings
plus a trait cost (0–2, set by the combat designer) total 15 for every fighter. This is a
sanity check for kit sketches, not proof of balance. Win rates and damage share decide
balance (§9); the stat budget only catches kits that are obviously over or under before
anyone has played them.

## 8. Boss kill-time budget

Boss Life is scaled by difficulty and by the number of fighters taking part:

```
boss_life = base_life × difficulty_scale × participant_factor
participant_factor: 1 fighter 0.6, 2 fighters 1.0, 3 fighters 1.3, 4 fighters 1.55
difficulty_scale:   see modes.md §5 (0.6 … 1.2); Tournament and Custom use 1.0
```

The scale applies to every breakable part as well as to total Life.

Reference sustained damage on a boss: 25 per second per fighter unblitzed, 45 Blitzed.

A Blitzed window of about a third of the fight gives a blended rate of roughly 30 per second
per fighter. Expected kill time includes movement, telegraphs, and whiffs. It is not Life
divided by that rate. Vitre has no damage gate, so his times track the blended rate.
The Harvester's head and tail are gated behind legs, so his kill time is longer than
route-damage divided by the blended rate. At Expedition difficulty 1–4 the Harvester's Beam
deals 500 instead of a KO, so those settings have no in-fight instant KO. Vitre's Overpress
is his in-fight KO. Flood and Blade Sweep are the timeouts.

| Encounter | Vitre Life | Expected kill time | Timeout | Margin |
| --- | --- | --- | --- | --- |
| Harvester, 2 fighters, difficulty scale 1.0 (Tournament) | — (route damage 1,500–2,200) | 70–90 s | 150 s | ≥ 1.6× |
| Vitre, 2 fighters, scale 1.0 (Tournament) | 6,000 | 90–110 s | 180 s (from Phase B start) | ≥ 1.6× |
| Vitre, 2 fighters, scale 1.2 (Expedition 7–8, CPU ally) | 7,200 | 110–130 s | 180 s | ≥ 1.4× |
| Vitre, 1 fighter, scale 1.0 (Duel, solo Custom) | 3,600 | ~120 s | 180 s | 1.5× |
| Vitre, 1 fighter, scale 0.6 (Expedition 1–2) | 2,160 | ~75 s | 180 s | 2.4× |

Every boss encounter must hold a margin of at least 1.25× at its hardest supported setting.
v0.1 gave Expedition no boss ally, so difficulty 8 meant a solo Vitre at 7,200 Life against
180 s. That needs 40 sustained damage per second from one fighter, and cannot be cleared.
The participant factor and the Expedition ally (`modes.md` §5) fix that.

## 9. Acceptance metrics

These replace the original M4 single win-rate band, which was wrong for a four-player game.
In a 4-actor FFA the expected win rate is 25%, not 50%.

| Metric | Band | Source |
| --- | --- | --- |
| 1v1 matchup, any pair | 40–60% | Human playtests, ≥ 200 games per pair at M4; CPU sweeps for regression only |
| 1v1 overall, per fighter | 45–55% | Human playtests |
| 4-actor FFA win share, per fighter | 20–30% | Human playtests |
| Pick rate, per fighter | no fighter below 0.4× or above 2.5× the median | Telemetry |
| Item usage | 0.3×–2× the median for its category | Crate-spawn telemetry |
| Blitz conversion | 35–55% in 4-actor FFA | Telemetry. In a mode that ends at two left, conversion means the Blitzed fighter is one of the two who advance. In last-standing modes, it means first place. |
| Damage share by source | §1 bands | Telemetry |

CPU-vs-CPU sweeps measure the CPU's skill with a kit as much as the kit itself. They are
used to catch regressions between builds, never to sign off balance.

## 10. Economy invariants

Checked on every content export by the economy simulator (nightly CI, `architecture.md` §11):

1. `sell(i) < buy(i)` for every purchasable item.
2. **No arbitrage loop.** For every pair of shop-buyable inputs, the expected sell value of
   the Tinker output (including doubles) plus the expected value of the Chits it
   pays is at most 0.6× the inputs' buy cost. Buy → Tinker → sell → Facet Wheel must never be
   positive in expectation.
3. **Carats per minute** in Expedition rises with difficulty, and a full clear pays more per
   minute than quitting after any earlier stage at the same difficulty.
4. Every buyable price sits in its rarity band (`items-and-economy.md` §6).

## 11. Balance process

- Frame data, damage, and Luster costs are exported to a sheet on every build so designers
  can diff a balance change line by line.
- Each balance patch changes the smallest number of values that fixes the metric that
  failed, and states which metric it is aimed at.
- Telemetry from M5 onward reports every §9 metric per build.
