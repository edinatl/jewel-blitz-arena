# JEWEL BLITZ ARENA — Game Design v2.0

*An original arena brawler inspired by the Power Stone 2 formula (2000-era Capcom
party fighting). All characters, names, items and stages are our own originals —
we borrow the* systems *(3-stone transformation, item chaos, interactive stages),
never Capcom's cast, art, or names.*

**Status key:** ✅ playable in a current build · 🛠 in this update · 🔜 next milestone · 📐 designed, not yet built

---

## 1. Fantasy & tone
Chaotic party brawling for 1–4 fighters in living arenas. Anime-cartoon look,
vibrant colors, exaggerated animation, slapstick item warfare. Matches are short,
loud and swingy: the arena itself is ammunition.

## 2. Core loop (the defining loop)
Fight → knock **Jewels** (our Power Stones) out of opponents with ANY hit →
collect **3 Jewels** → **TRANSFORM** with a dramatic gold form → burn your
**Power Gauge** on **Power Drives** and a **Power Fusion** super → win by KO or
by having the most health when the 2:00 timer ends.

- Jewels also spawn in the stage and burst out of item boxes. ✅
- A hit on a transformed fighter does reduced damage and can't steal Jewels. ✅
- Multiple fighters can be transformed at the same time. ✅
- Gauge drains over time (12s base); Drives cost ~25, Fusion ~50; empty gauge = form ends. 🛠

## 3. Controls (PlayStation-layout diamond, copied positions)
Analog stick / D-pad / touch joystick: full 360° movement.

| Button | Action |
|---|---|
| □ Attack | Attack / use held item. While transformed: **Power Drive** |
| ○ Action | **Pick up is MANUAL** (Ben's call): Action at an item's feet picks it up; Action with an item throws it **in the facing direction** (no aim assist); □ with a throwable throws it **aimed at the closest foe**; Action also = **catch** incoming items, **grab opponents** ✅, board stage gimmicks (turret), revive in co-op 🔜 |
| ✕ Jump | Jump. Jump + direction = **dodge roll** (i-frames). Jump at a wall = **wall jump** 🛠 |
| △ | Transformed: **Power Fusion**. Otherwise: **Discard item** |

Chords from the original formula also work: **Attack+Jump** and **Action+Jump**
while transformed fire Fusion 1 / Fusion 2 variants. (Touch: △ covers it.)
Keyboard (demo): F = □, G = ○, Space = ✕, H = △, R = rematch.

- **No blocking.** Defense is movement: dodge roll with brief invulnerability,
  and a **perfect dodge** (roll the instant before a hit lands) flashes
  "PERFECT DODGE!" and leaves the attacker open. 🛠
- **Air attacks:** attack while airborne = diving attack. 🛠
- **Combos:** chained hits within 1.2s count up; the 3rd hit **knocks down**. 🛠
- **Knockdowns** drop a Jewel or **gold coins**; the victim gets up with a
  **recovery roll** (direction + roll = i-frames). 🛠
- **Grabs:** ✅ Action next to an opponent while empty-handed grabs them
  overhead; any attack input throws them (they tumble, bowl into others
  in the web demo, and take landing damage + a short knockdown). The
  victim mashes buttons to escape. In all three builds (Unity v0.6).
- **Perfect dodge:** ✅ rolling within a split second of an incoming hit
  (web demos) — attacker is left open, brief hitstop freeze.
- **Recovery roll:** ✅ press Jump late in a knockdown to stand straight
  into an invincible roll instead of serving the full knockdown.
- **Air combat:** ✅ thrown items and explosions now connect with airborne
  fighters; ground melee still whiffs under a jump unless you jump too.
- **Stage events:** ✅ (3D web demo) telegraphed falling debris that
  crashes into throwable rocks, plus a halftime GEM RUSH event.
- **4-fighter free-for-all:** ✅ (3D web demo v0.9) — Blaze/Ronin sprites
  vs Bruno and Aegis bots, last fighter standing, character select.
- **Item pads:** ✅ gold floor circles pop up random items; take one and
  a new item respawns on the pad after 1 second (all three builds).
- **Passive bots (test mode):** ✅ Ben asked for non-attacking bots while
  testing (they roam and collect only) — one flag re-arms them.
- **Hazard orb:** ❌ removed from all builds at Ben's direction (falling
  debris remains the stage hazard in the web demos).
- **Unique item abilities:** ✅ Mega Buster (charge every 4th shot),
  Spring Punch Glove (huge launch), Golden Club (coins on hit), Flame
  Thrower (3-jet cone), Freeze Ray (slow), Lightning Gun (chain arc).
  Catalog is now **114 items**.

## 4. Fighters — roster of 12 (+4 unlockable) 🛠 data / ✅ 8 modeled in Unity
Each fighter: unique speed / power / defense, two Power Drives, one Fusion.

| Fighter | Archetype | Spd | Pow | Def | Power Drive (□ / ○) | Power Fusion |
|---|---|---|---|---|---|---|
| **Blaze** (Soldier) | balanced hero | 3 | 3 | 3 | Rocket Salvo / Grenade Fan | Orbital Barrage |
| **Ronin** (Samurai) | swordsman | 3 | 4 | 2 | Blade Wave / Iaido Rush | Heaven Divider |
| **Bruno** (Warrior) | heavy tank | 2 | 5 | 4 | Shockwave Slam / Boulder Toss | Meteor Body Press |
| **Aegis** (Gladiator) | defender | 2 | 4 | 5 | Shield Cannon / Net Snare | Colosseum Quake |
| **Sir Roland** (Knight) | armored all-round | 3 | 3 | 4 | Lance Beam / Banner Rally | Holy Onslaught |
| **Viper** (Ninja) | fastest, fragile | 5 | 2 | 2 | Shuriken Storm / Smoke Bomb | Thousand Cuts |
| **Captain Mara** (Pirate) | brawler-gunner | 3 | 4 | 3 | Cannon Blast / Anchor Drop | Broadside Barrage |
| **Bolt** (Robot) | inventor's bot | 2 | 4 | 4 | Plasma Arc / Magnet Pull | Overdrive Meltdown |
| **Jester** 🆕 | clown trickster | 4 | 3 | 2 | Pie Barrage / Jack-in-the-Box | Big Top Chaos |
| **Pyra** 🆕 | fire mage | 3 | 4 | 2 | Fire Serpent / Flame Trap | Inferno Pillar |
| **Gusto** 🆕 | chef (transforms beastly) | 3 | 4 | 3 | Skillet Slam / Soup Splash | T-Rex Rampage |
| **Dusty** 🆕 | outlaw gunslinger | 4 | 3 | 3 | Trick Shot / Dynamite Hat | Dead Man's Hand |

Unlockables 📐: **Tinker** (inventor kid), **Madame Mirror** (dual-personality
duelist), **Mel the Shopkeep** (gains wings in form), **The Colonel** (Blaze's
father, veteran hero).

Bosses 📐: **SPHINX-9**, a giant robot sphinx piloted by masked twins (Arcade
mid-boss); **Dr. Vex**, the mad inventor who trapped everyone in his floating
**Castle Vex** (final boss, monstrous powered armor form).

## 5. Items — 111 in the catalog ✅, PS2-style families 🛠 3D models
Families copied in *type and spirit* from the PS2 item ecosystem (120+ items):
firearms (pistols, machine guns, bazooka-class launchers, flame thrower, ray /
plasma / freeze guns, bubble & sonic guns), melee of every flavor (katanas,
beam-style laser swords, hammers, axes, pipes, rackets, frying pans), bombs
(small→cluster, kegs, mines, fireworks), throwables (the stage furniture
itself: chairs, tables, benches, crates, barrels — plus cacti in the desert),
food that heals, and specials (element wands, gloves, magnet coils, gravity
orbs, mirror shields). Silly items included by design (ale barrels, globes,
televisions, treasure chests).

- Every item now has its own **3D model + idle animation** (spin/bob, bomb
  fuse-pulse, food sparkle, special-item glow) in the 3D demo and Unity. 🛠
- **Item boxes** spawn in the arena; smash one (2 hits) and it pops a random
  item, a Jewel, or gold. 🛠
- Item gauge = remaining uses, shown in the HUD. ✅

## 6. Stages — dynamic, interactive, multi-level 🔜/📐
Launch stage: **Desert Ruins** ✅ (sand arena, cactus ammunition, patrolling
hazard orb, boardable **turret** 🛠). Designed stages 📐, one gimmick each:
Sky Galleon (deck breaks → free-fall to a lower platform), Sub Showdown
(submarines submerge/resurface), Castle Vex (collapsing floors), Tomb of
SPHINX-9 (boulder chase corridor), Iceberg Drift (tilting ice), Orbit Station
(low-gravity + airlocks), plus multi-level platforms and wall/ceiling play.
Stage objects are throwable; hazards (spikes, debris, traps) hurt everyone.

## 7. Modes
1. **Duel (1-on-1)** ✅ demo form — story battles vs the roster.
2. **Arcade (4-fighter)** 🔜 — top 2 advance each stage; SPHINX-9 mid-boss,
   Dr. Vex finale.
3. **Original / Free Battle** 🛠 — 4-fighter free-for-all is live in the 3D web demo (v0.9); teams and stage/item pickers still 🔜.
4. **Adventure** 📐 — runs earn **gold** 🛠 (drops in matches now) and cards;
   spend gold in **Mel's Item Shop**; **mix 2 items + a card in the furnace**
   to craft better gear (failures → Scrap); register up to **5 handy items**
   that can appear in free play.

## 8. Story 📐
Dr. Vex has sealed twelve fighters inside his floating Castle Vex to power his
Jewel Engine with their battles. Fight through the castle's arenas, beat
SPHINX-9, and knock Vex out of his armor to escape. (Told in Duel/Arcade
interludes; deliberately light — this is a party game.)

## 9. Presentation
Colorful primitive-built 3D fighters with procedural animation ✅ (run cycles,
swings, catch poses, flinches, transform aura, KO falls), HQ comic sprite
sheets with 8-pose animation ✅ (sprite demo), particles, shockwave rings,
floating damage text, screen shake, dynamic camera that **zooms with fighter
separation** 🛠 (3D demo).

## 10. Implementation notes (what's where)
- **Unity** (`unity/PowerArenaPrototype`): the real game — 3D fighters, all
  systems above landing here first-class. Re-run *Prototype → Build Arena
  Scene* after updates.
- **3D web demo** (`docs/index.html`): Three.js mirror of the Unity design —
  the fastest way to feel changes on a phone.
- **Sprite web demo** (`docs/sprite-arena.html`): 2D canvas + animated sprite
  sheets; shares rules, catalog (111) and control layout.
- One catalog, one ruleset, three front-ends. Rules changes land in all three.
