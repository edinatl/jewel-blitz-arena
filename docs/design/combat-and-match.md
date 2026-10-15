# Combat and Match Rules

Owner: combat designer, combat/sim engineer. Everything in this file runs inside the
deterministic sim assembly. Numbers are starting values for M1 and are expected to move;
the budgets they must fit are in `balance.md`.

## 1. Frame of reference

- Sim runs at 60 Hz. All timings below are in frames (f) unless stated in seconds.
- A match has 2–4 fighters (plus boss parts on boss arenas) in a bounded 3D volume.
- **Life** is an integer per fighter, 850–1150 depending on kit (`balance.md` §2). The HUD
  bar shows percentage of max. **Luster** is an integer 0–1200, shown as a second bar under
  Life while Blitzed.
- There is no block, no meter outside Blitz, and no round structure. A match ends on
  last fighter standing, team elimination, the Tournament two-left rule (`modes.md` §3), or
  the Squall/draw sequence.

## 2. Controls

Default mapping, remappable in options. Positions follow the classic four-face-button layout
and are preserved across pad vendors (bottom = Jump, left = Attack, right = Action, top =
Strong).

| Input | Verb |
| --- | --- |
| Left stick / D-pad | Move. Analog magnitude sets walk/run. |
| Bottom face (Jump) | Jump. Second press in air: double jump for fighters that have it. |
| Left face (Attack) | Attack. Uses or throws a held item. While Blitzed: **Flare**. |
| Right face (Action) | Pick up item, grab pillar, board a gimmick, catch a thrown item. |
| Top face (Strong) | Strong attack. Launches or knocks down. |
| Left shoulder / Attack + Jump (Blitzed) | **Surge A**. Spends Luster. |
| Right shoulder / Strong + Jump (Blitzed) | **Surge B**. Spends Luster. |
| Attack + Strong near opponent | Throw. |
| Direction flick just before a hit lands | Dodge (§4). |
| Start | Pause. From pause, Action opens Photo Mode (local only). Local pause also shows the stage's phase timeline: phase names and the time until the next transition. It is not drawn on the match HUD. |

Surges are on the shoulders by default, with the face-button chords also accepted. The
chord exists because the reference used it. The shoulder default exists because, while
Blitzed, Jump-then-Attack for an air Flare is one frame away from the Surge A chord, and an
accidental 420-Luster spend is the worst misread in the game.

- **Chord window:** 3 f. A chord only reads if neither button was already held when the
  window opened.
- **Input buffer:** 5 f for attacks, jumps, and Action during recovery, so strings and
  pickups do not eat inputs pressed slightly early.

## 3. Movement

- Run speed, jump height, air control, and double-jump count are per-fighter kit values.
- **Wall spring.** Jump while airborne and touching a wall normal: launches away from the
  wall. Unlimited springs; each costs 8 f of no-attack.
- **Pillar grab.** Action near a pillar-tagged collider.
  - Grounded: actor swings around the pillar and is released as a **charge** in the facing
    direction (24 f of forward travel, hits as a Strong, no armor).
  - Airborne: actor hangs on the pillar. Jump releases upward, Attack releases with a kick,
    Strong drops to the ground. Hang expires after 90 f. The same pillar cannot be
    re-grabbed for 60 f, which stops hang-stalling against the clock.
- **Ledges.** No ledge grab; falling off is a real consequence. Fall-out volumes deal 150
  Life, drop the held item, and drop all held jewels (they reappear at jewel points
  after 3 s, as in the reference). The fighter respawns at the nearest active spawn point
  after 60 f with 90 f of invulnerability (blinking) and no attacks for the first 30 f.
  Stages flagged `fall_is_ko` (Spirewalk, The Cascade) count a fall as a KO instead. The
  volume lip is marked in the geometry, and a soft tone plays while a fighter is within
  1.5 m of it. The fall stays an untelegraphed kill. The lip is how the boundary is readable.

## 4. Dodge

No block exists. The defensive verb is a reactive dodge.

- **Input.** A *flick*: the stick goes from neutral (< 25%) to > 80% within 3 f; or, while
  already tilted, the stick's direction changes by ≥ 90° at > 80% within 3 f; or any D-pad
  press. The second form lets a running fighter dodge by cutting sharply, without returning
  to neutral first.
- **Window.** A flick arms the dodge for 6 f (Teo Cruz: 8 f). If a dodgeable hitbox would
  connect while armed, the dodge fires instead: 14 f of invulnerability, a short step in the
  flick direction, no attacks for the first 10 f.
- **Lockout.** A flick that arms but is not used locks out further arming for 24 f. That
  makes stick-wiggling a 20% guess instead of a free defense, and makes a well-timed single
  flick the correct play.
- **Cooldown.** After a dodge fires, another cannot fire for 40 f. Multi-hit moves can be
  dodged at their start, not threaded hit by hit.
- **States.** A dodge can only fire from neutral, movement, jump, landing, or attack
  recovery. It cannot fire from hit-stun, knockdown, dizzy, freeze, or attack startup and
  active frames.
- **What dodges.** Attacks, projectiles, and explosions are dodgeable. Throws, grabs,
  stage-volume hazards (crush, fall-out, floods), and Squall strikes are not.
- The sim checks the armed state at the moment a hitbox would connect. It is deterministic
  and rollback-safe because it reads only the input history.
- Items in hand are kept through a dodge.
- **Dodge Assist** (accessibility, §13): widens the window by 4 f for that player and shows
  an assist icon on their status bar.

## 5. Attacks

Each fighter has:

- Attack string: three ground hits (A, AA, AAA). AAA knocks back.
- Strong: one ground hit that launches or knocks down; a few kits have a charge variant.
- Air Attack, Air Strong.
- Dash Attack (Attack during run).
- Throw (§5.4).
- Getup attack (§5.3).

### 5.1 Priority

No super armor exists anywhere, including during Blitz. Priority is resolved purely by
hitbox timing and the "both hit" rule: if two hitboxes connect on the same frame, both actors
take the hit.

### 5.2 Combos and gang-up protection

The **combo system** is hit-stun chaining. Each hit applies stun frames and knockback, and a
follow-up that connects during stun continues the chain. The chain counter lives on the
**target**, so it is shared by every attacker. Two fighters juggling a third share one
ceiling.

- Each chained hit has 15% less stun and 15% more knockback than the one before.
- At chain length 6 or 220 chain damage, the next hit forces a knockdown.
- The chain resets after 30 f out of hit-stun.
- The combo counter is displayed. It has no mechanical reward beyond damage dealt.

### 5.3 Knockdown and getup

- A knocked-down fighter is grounded for at least 30 f, then gets up on any input or
  automatically at 60 f.
- Getup options: neutral getup, roll getup (direction, 1.5 m), or getup attack (Attack; 40
  damage, 16 f startup). The first 20 f of any getup are invulnerable; the getup attack's
  active frames are not.
- Grounded fighters can only be hit by moves tagged `otg` (a few Strongs, stomps, and
  explosions), at 50% damage, once per knockdown. This removes floor loops.

### 5.4 Throws

- Attack + Strong within 1.2 m of a grounded opponent who is not in hit-stun, knockdown, or
  a dodge. 4 f startup. 100 damage. The thrown body is a projectile for 20 f and deals 60 to
  anyone it hits.
- **Interaction triangle.** An active hitbox beats a throw in startup. A throw beats a dodge,
  because throws are undodgeable. A dodge beats an attack. With no block, this triangle is
  the core read.
- **Throw break.** The victim presses Attack + Strong within 10 f of the grab connecting:
  both fighters are pushed 1.5 m apart and nobody takes damage. Command grabs (Bulwark Oyo,
  Hob's Pincer) can be broken the same way during their first 10 f, except while the victim
  is dizzy.

## 6. Items in the match

Detailed item behavior is in `items-and-economy.md`; the match rules that apply to all items:

- **Pickup ring.** Any pickup-able item or boardable gimmick shows a ring at pickup range
  (1.0 m default). Ring color codes category (weapon, explosive, food, status, mobility,
  summon, toy, jewel, gleam). Colorblind palettes swap hue for pattern.
- **Hold.** One held item at a time (Clank: two). Picking up while holding drops the current
  item in place.
- **Use / throw.** Attack uses a held weapon (swing or fire) or throws a throwable. Held
  weapons have durability or ammo; when spent they break with a visible pop.
- **Catch.** A thrown item or thrown opponent can be caught on an Action press within 6 f
  of contact. Caught items go to hand. A caught opponent is re-thrown if Action is still held
  10 f later, or dropped behind if released.
- **Steal.** Hitting an actor with a Strong while they hold an item makes them drop it.
- **Caps.** Per-stage caps on simultaneous loose items (default 12) and active summons (2).
  These are design constraints and also rollback snapshot constraints. When a spawn would
  exceed the loose-item cap, the oldest unheld loose item despawns with a puff first.
- **Crates.** Spawn at stage-authored points on phase-graph timers; opening is one Attack
  hit. Contents come from the match's crate table (see items spec). Crates and other
  containers also drop **gleam pickups** in Expedition.

## 7. Jewels and Blitz

### 7.1 Jewels

- Jewels spawn at authored points; the circulation cap, opening, loss, and respawn rules
  are in `balance.md` §5 and are the authority. In summary: at most `jewel_cap` jewels
  are on the field or held at once (default 3 with two fighters, 5 with three or four), so
  by default only one fighter can be Blitzed at a time.
- Walking over a jewel collects it. Held count shows as pips on the status bar.
- A knockdown while holding jewels drops one, which bounces and re-enters the field.
- A fighter who has just reverted cannot collect jewels for 6 s (**Dross**, shown as a
  dull-stone icon on their pips). Sparring's `jewel_mode: respawn` is the only mode that
  turns Dross off (`modes.md` §4).
- Some crates (`stage.jewel_crate_chance`, default 8%) and some stage containers contain
  a jewel instead of an item. These count toward the cap; if the cap is full, the crate
  rolls an item instead.

### 7.2 Blitz

- Collecting the third jewel **Blitzes** the fighter on the same frame: 30 f of
  invulnerable transformation, ending in a 2 m shove that pushes nearby actors away without
  damage, then the Blitzed kit replaces the base kit. Luster is set to 1200.
- **Suppression.** A stage phase may set `blitz_suppressed` (Pressworks sorting line). A
  third jewel is held instead; the pips flash. A knockdown while holding three drops one
  as usual. The fighter Blitzes on the first frame suppression lifts.
- **Luster drain.** With `House Rules.luster_timer` on, Luster drains 1 per frame (20 s). A
  standard Surge requires and spends 420; Flare spends nothing. Per-move Luster costs are
  kit data, so a declared exception such as Tilly's Invoice (180) is an authored value, not
  a special case. With the timer off, Luster drains only through Surges, and a Surge may
  be started with any Luster above zero (it empties the gauge).
- **Revert.** When Luster reaches 0 the fighter reverts over 20 f (not invulnerable). Jewel
  count resets to 0. The three jewels respawn per `balance.md` §5.
- **Blitzed kit.** Movement generally improves (per fighter). Attack becomes **Flare**, the
  fighter's signature special: a repeatable move string with no Luster cost. Strong remains a
  Strong variant. **Surge A** and **Surge B** are the two finishers. Both have long
  startup, both are dodgeable, and both can be interrupted by a hit (no armor). Interrupted
  Surges refund half their Luster.
- **Damage scaling while Blitzed.** `House Rules.blitzed_damage_taken` (default 50%) and
  `House Rules.blitzed_attack` (default 100%) scale incoming and outgoing damage. Squall
  strikes, fall-outs in `fall_is_ko` stages, and boss instant-KO moves ignore the scaling.
- Blitzed fighters still pick up and use items. Blitzed fighters cannot collect jewels.
- **Variants.** Some Surges have variants selected by held direction at input (Clank).
  Each variant has its own Luster cost and damage in kit data, and each must fit the
  damage-per-Luster ratio in `balance.md` §3.3.

## 8. Knockout, Rally, placements

- Life 0 is a knockout. With Rally inactive, the body stays as a prop for 60 f, then
  despawns. With Rally active, the body stays for the whole Rally window.
- **Rally.** When teams are on (`MatchConfig.teams != None`, which includes every boss
  arena), a KO starts a Rally window of `House Rules.rally_seconds` (default 9 s; 0
  disables). A living teammate who stands within 1.5 m for 60 consecutive frames revives
  the downed fighter with 300 Life, transferring `min(150, rescuer Life − 1)` from the
  rescuer. Being hit resets the 60 f count. Each fighter can be Rallied once per match. A
  ring on the body shows the countdown and the rescuer's progress.
- In free-for-all a KO is final (open decision 3). Eliminated humans can play on as Wisps
  (§10).
- Placements are by KO order, last standing first. Simultaneous KOs tie, broken by
  remaining Life percentage at the previous frame.

## 9. Match time, Squall, draw

- `MatchConfig.time_limit` default 99 s for Tournament and Expedition FFAs, unlimited in
  Custom unless set.
- On expiry with more than the required number of survivors, **Squall** begins: 30 s of
  lightning strikes. Items stop spawning; jewels stop spawning.
  - Each strike is telegraphed by a 40 f ground ring (1.6 m radius) and KOs any fighter
    inside, ignoring Life, damage scaling, and Blitzed state. Dodge does not apply; moving
    out of the ring does.
  - Strikes are aimed at the survivor targeted least recently, at their position when the
    ring appears, so standing still loses and running wins. Every third strike lands at a
    random open point instead.
  - The interval starts at 90 f and multiplies by 0.8 every 5 s (90, 72, 58, 46, 37, 29 f).
  - Lightning is drawn without full-screen flashes and stays within the photosensitivity
    limits in `architecture.md` §7.
- If Squall ends with more survivors than required, the match is a **draw**. In Tournament
  a draw among the advancing two advances both. A draw involving more than two re-runs the
  stage once, then advances by remaining Life percentage.
- Boss arenas replace Squall with the boss's timeout move, which is unavoidable: the
  Harvester's **Blade Sweep** and Vitre's **Flood** (the Press floods the floor with Black
  Glass). See `stages.md`.

## 10. Basic Rules, Wisps, and drop-in

### 10.1 Basic Rules

Always available in Custom and Online lobbies, separate from House Rules, because they are
party staples rather than rewards.

| Rule | Range | Default |
| --- | --- | --- |
| Time limit | off, 60–300 s | off |
| Teams | off, 2v2, 1v3, 2v1v1 | off |
| Item frequency | off / low / normal / high (0, ×0.5, ×1, ×1.5 crate respawn rate) | normal |
| Item switch | per-item on/off from the Ledger, with presets (All, Starter only, No summons, Food only) | All |
| Handicap | per fighter, Life 50–150% in 25% steps | 100% |
| Stage hazards | on/off (gimmicks and phases still run; damaging hazards are removed) | on |
| Wisps | on/off | on |
| CPU level | per CPU slot, 1–8 | 4 |

### 10.2 Wisps

Eliminated humans become a **Wisp** that circles at the edge of the camera frame, so nobody
sits out a long free-for-all. This is modeled on Bomberman's revenge carts. Every 10 s a
Wisp can drop one thing on a target ring: a wisp cracker (60 damage, light knockback) or a
sugar drop (heals 50). Wisp drops cannot KO (they leave a fighter at 1 Life), cannot touch
jewels, and cannot hit a Blitzed fighter's Luster.

Wisps exist only in Custom and Online, and only where KO is final. They are off in
Tournament, Expedition, Duel, boss arenas, and Sparring. A revenge drop should not decide
who advances or what a run pays. The Custom and Online default is on (open decision 9).

### 10.3 Drop-in

During any CPU-filled match in Tournament or Expedition, pressing Start on an idle
controller pauses and offers "Join as Ally" or "Join as Rival". The chosen CPU slot is
swapped for the human at the current Life and position. In Expedition the drop-in player
earns no carats and writes nothing to the Ledger, so Expedition remains single-player for
progression.

## 11. House Rules

Each entry becomes editable in Custom after a distinct Tournament clear (any fighter, order
of unlock fixed as listed). Before unlock, the default applies and the row is shown locked.

| # | Rule | Range | Default |
| --- | --- | --- | --- |
| 1 | Pocket Items in crates | on/off | off |
| 2 | Jewel cap | Auto, 3–7 | Auto (3 with two fighters, 5 with three or four) |
| 3 | Luster timer | on/off | on |
| 4 | Blitzed damage taken | 25–100% | 50% |
| 5 | Blitzed attack | 50–200% | 100% |
| 6 | Rally seconds | 0–15 | 9 |
| 7 | Phase clock | 50–200% | 100% (scales every stage phase timer) |

The reference's Target Line moved to Assists (§13). It is a learning aid, so it should not
be gated behind seven clears. Phase Clock takes its slot because it is the one rule that
speaks directly to pillar 2: a veteran group can run the stages hot and a new group can
slow them down. Sound Test unlocks in the Profile menu at eight distinct clears
(`modes.md` §8). It is not a House Rule. A row that only opens a menu was a reward
pretending to be a rule, and Tilly Setter is already the eighth clear's reward.

## 12. Camera

Shared camera. It solves for a position that keeps all living actors in frame with a margin,
biased toward the match's centroid of activity (recent hits and jewel positions), clamped to
stage-authored camera volumes per phase. Phase transitions can hand the camera a scripted path
(bazaar breakaway, vault breach). The camera is presentation but reads only from the sim snapshot
and a per-stage authoring file, so it is replay-safe.

Hard rules: no actor off-screen for more than 10 f outside a scripted transition. When the
solve fails (actors too far apart), the camera pulls back to the phase's maximum and shows
each off-screen actor in an **edge bubble**: a small circular inset at the frame edge,
rendered from the main camera's angle, showing the actor, their pips, and their held item.
An actor shown in a bubble counts as on screen for the 10 f rule.

## 13. Assists and accessibility

Available from first boot, per player, saved on the profile:

- Full remap, including chords and shoulder shortcuts; hold vs toggle for glide and float.
- **Dodge Assist** (§4), **Target Line** (a line from the fighter to the nearest jewel or
  targeted opponent), **Auto-pickup** (walk over an item to take it when your hands are
  empty).
- **Teams.** When teams are on, Life bars group by team. Each fighter keeps a unique player
  color and gains a team shape on the bar and on the body, beside the color. Team identity
  is never carried by color alone.
- Colorblind ring and player-color palettes (pattern + hue), HUD scale 80–150%,
  screen-shake 0–100%, camera smoothing (reduced-motion).
- Audio cues for every telegraph that is otherwise visual-only (Squall ring, Beam line,
  Pistons cracks), with mono and caption options.

In Online lobbies, assists that change sim behavior (Dodge Assist, Auto-pickup) are shown
next to the player's name.

## 14. CPU fighters

Difficulty 1–8. Parameters per level: reaction delay, dodge probability, jewel greed (how
far it will path for a jewel), item preference weights by category, Surge usage policy,
team awareness (Rally attempts, focus fire), and hazard avoidance competence.

| Level | Reaction delay | Telegraphed hits dodged | Notes |
| --- | --- | --- | --- |
| 1 | 40 f | 0% | Never Surges, never throws |
| 4 | 20 f | 25% | Default for Tournament |
| 8 | 10 f | 60% | Plays for jewels first |

Fairness rules:

- CPUs read only the sim state a human could see. They never read other fighters' input
  buffers, and the reaction delay is a floor applied to every decision, so no CPU dodges
  frame-perfectly.
- Target selection scores distance, threat, and opportunity. It never scores "is human".
  In FFA, a CPU avoids picking the same target as another CPU when a different target is
  within 1.5× the distance, which prevents CPU dogpiles.
- CPU brains run inside the sim (`architecture.md` §2.6), so they are deterministic, work in
  online lobbies, and replay identically.
