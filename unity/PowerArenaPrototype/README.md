# Power Arena — Prototype v0.8

An original arena brawler inspired by Power Stone 2's formula.
2 fighters, 1 arena, local play + a solo bot. No Capcom assets, names, or characters.

## What changed in v0.8 (Ben's test-mode pass)
- **The bot doesn't attack** (`PlayerController.BotAttacksEnabled = false`): it roams and collects, but never swings, throws, grabs, or fusions — a test dummy while you learn the systems. Flip the flag to re-arm it.
- **Catch is on the ○ Action button now**, same as the manual throw: a Heavy press that doesn't pick up or grab opens the catch window. Light/□ no longer catches.
- **The bouncing hazard orb is removed** from the arena.
- **Item pads:** four gold circles on the floor pop up random items; pick one up and a fresh random item respawns on that pad **1 second** later (`ItemPad.cs`).
- **3 new items with unique abilities (catalog now 114):** **Mega Buster** (rapid pellets, every 4th shot is a charged blast), **Spring Punch Glove** (huge BOING launch), **Golden Club** (every landed hit shakes a gold coin loose). Plus unique twists for classics: **Flame Thrower** fires a 3-jet cone, **Freeze Ray** slows victims, **Lightning Gun** arcs to a second nearby fighter, **Rocket Launcher** keeps its explosive shells.
- Re-run **Prototype → Build Arena Scene** once after updating to v0.8 (pads replace the hazard; item count changed).

## What changed in v0.7 (Action-button controls + HD sprite fighters)
- **○ Action is now PICK UP / manual throw:** items are no longer picked up by walking over them — stand next to one with empty hands and press **Heavy (G / ○)** to pick it up (it takes priority over grabbing). Heavy with an item in hand **throws it in the direction you're facing** — no aim assist.
- **□ Attack throws AIMED:** with a throwable or explosive in hand, **Light (F / □)** throws it **at the closest opponent** automatically.
- **HD animated sprite fighters:** Soldier and Samurai now fight as the generated HD sprite sheets (8 poses + gold transform variants) on camera-facing billboards (`SpriteBillboard.cs`, frames in `Assets/Resources/Sprites/`) — the same frames as the web demos. The primitive bodies are hidden automatically when the sprite is active; the other roster characters still use their 3D primitive models.
- Re-run **Prototype → Build Arena Scene** once after updating to v0.7 so the scene adds the sprite components.

## What changed in v0.6 (grabs + recovery)
- **Opponent grabs:** Heavy (the ○ Action button) with empty hands next to a foe **grabs them overhead** — press any attack to **throw them** across the arena (12 landing damage + a short knockdown, and a gem pops loose). If you do nothing they auto-throw after 1.1s. The victim **mashes action buttons** to escape early (4 presses); the bot wriggles out on its own sometimes and will grab you back — its close-range heavy is now a grab attempt.
- **Recovery roll:** press **Jump** in the last 0.42s of a knockdown to stand straight into an invincible dodge roll instead of lying there the full 0.85s. The bot does it too.
- The web demos went further in the same pass: the 3D demo is now a **4-fighter free-for-all** with character select, perfect dodges, hitstop, falling debris and a halftime GEM RUSH event.

## What changed in v0.5 (Design v2 systems — same rules as the web demos)

- **Every item has its own 3D model** (`ItemVisual.cs`): all 111 items are built part-by-part at runtime in Power Stone item families — swords with guards and hilts, guns with barrels and drums, bombs with fuses, food, glowing wands — with idle animation (explosives pulse, special items and TVs glow). The old single-primitive look is gone.
- **Power Gauge transformation:** 3 gems now fill a 100-point gauge that drains over ~12s. □/Light while transformed fires your **Power Drive** (25 gauge — Soldier: 3-shell Rocket Salvo, Samurai: Blade Wave); △ **Power Fusion** (50 gauge) rains three telegraphed explosions on your opponent. Untransformed △ discards your held item.
- **Dodge roll, wall jump, air attack:** jump while moving to dodge roll (fully invincible, 0.38s), press jump again near a wall mid-air to wall jump toward center, attack mid-air for a diving strike.
- **Combo knockdowns + gold:** the 3rd hit inside 1.2s knocks a fighter down (0.85s, can't act or be hit), dropping an extra gem — or 3 gold coins worth 10 each when gem-less.
- **4 new fighters** in `CharacterVisual.cs` — **Jester** (bells + ruff), **Pyra** (flame hair + fire staff), **Gusto** (chef hat + frying pan), **Dusty** (cowboy hat + six-shooter) — 12 on the roster; the arena still duels Soldier vs Samurai.
- **Bug fix:** the Samurai's horns used `PrimitiveType.Cone`, which doesn't exist in Unity and would not compile — horns, item cones, and rings are now a tiny procedural mesh shared by `CharacterVisual` and `ItemVisual`.
- Design doc: `GAME_DESIGN_V2.md` in this folder. Re-run **Prototype → Build Arena Scene** once after updating to v0.5.

## What changed in v0.4 (3D animation + health x4)
- **Real 3D animated fighters.** The invisible-capsule fighters are now comic characters built from primitives at runtime (`CharacterVisual.cs`) — Soldier vs Samurai in the arena, with Warrior, Gladiator, Knight, Ninja, Pirate and Robot also defined with signature gear (katana, shield + gladius, axe, sword + cape, nunchucks, cutlass + eyepatch, boxing-glove robot).
- **Procedural animation for everything** (`FighterAnimator.cs`): idle breathing, run cycle with swinging arms/legs, jump tuck, punch and weapon-swing attacks, both-arms-forward catch pose, hurt flinch, transform gold tint + aura pulse, and a fall-over KO. Gems and items spin/bob, projectiles leave trails, the hazard orb pulses.
- **Health x4:** fighters now have 400 HP (was 100), so fights last longer.
- Re-run **Prototype → Build Arena Scene** once after updating to v0.4 so the scene picks up the character visuals.

## What changed in v0.3 (art update)
- **Comic art pack** in `Assets/Art/`: an 8-fighter character roster (Soldier, Samurai, Warrior, Gladiator, Knight, Ninja, Pirate, Robot — plus individual portraits in `Assets/Art/Characters/`), a desert arena illustration, a cactus throwable, and an item icon sheet.
- **Desert stage:** the arena floor now uses the desert art as its texture (the builder loads `Assets/Art/desert_arena.png` automatically).
- **Cactus throwable:** new item in the catalog (111 total) and the stage now starts with cacti among its throwable props.
- The mobile demo (repo `docs/index.html`) uses the same art: desert map, Soldier vs Samurai portraits, cactus props, and per-category item icons.

## What changed in v0.2 (Ben's rules)

- **ANY hit knocks a gem loose.** No damage threshold. Melee, shots, bombs, thrown
  items, explosions, and the stage hazard all shake one gem out of whoever got hit.
  The gem flings away and can't be re-grabbed for 0.6s — it's a scramble, not a refund.
- **111 items in the game** (see the catalog below), all procedurally spawned.
- **Throwing is a core verb.** Heavy attack throws whatever you're holding.
  Stage furniture (chairs, crates, barrels, tables...) is placed around the arena
  at match start specifically to be picked up and thrown.
- **Catching is in.** With empty hands, tap Light attack as a thrown item reaches
  you, while facing it — you catch it and hold it instead of eating the damage.
  The bot catches too (about 75% reflexes), throws items back, and grabs items off
  the floor. Caught food can be eaten by the catcher. Caught explosives are…
  already on their way back to the thrower.

## Get it playing (3 steps)

1. **Add the project in Unity Hub** — Add → select this `PowerArenaPrototype` folder.
   Built for **Unity 2022.3 LTS** (Unity 6 also opens it). Leave input handling at default/Both.
2. **Build the scene** — top menu: **Prototype → Build Arena Scene**.
3. **Press Play.** (Re-running the builder isn't needed for v0.2 — scripts update in place.
   If you kept a v0.1 copy, just replace the whole folder and rebuild the scene once.)

## Controls

| Action | Player 1 (Blue) | Player 2 (Red) |
|---|---|---|
| Move | WASD | Arrow keys |
| Jump / **DODGE ROLL (while moving)** / wall jump (again, near a wall) | Space | Right Shift |
| □ Light attack / use held item / **CATCH (empty hands)** / **Power Drive (transformed)** | F | . (Period) |
| ○ Heavy attack / **THROW held item** | G | / (Slash) |
| △ **Power Fusion (transformed, 50 gauge)** / discard held item | H | , (Comma) |
| Restart match | R | R |

**Solo play:** Player 2 starts as AI. Any Player 2 keypress takes manual control.

## How a match works

- **120-second timer.** KO, or lead on health at time-up.
- **Power Gems (red):** collect **3** → transform: gold, bigger, faster, and a
  **Power Gauge (100)** that drains over ~12s, spent on your Power Drive (25)
  and Power Fusion (50). One gem is knocked out of you by *any* hit while
  you're carrying them. A 3-hit combo **knocks you down**: you drop another
  gem — or spill **gold coins** (10 each) if you're gem-less.
- **Items:** walk over an item with empty hands to pick it up.
  Light = use it (swing / shoot / eat). Heavy = throw it at the other fighter.
  Thrown items that miss land back on the floor as pickups again.
- **Stage Hazard (purple orb):** patrols the arena; it damages and knocks gems loose too.
- **HUD:** health, gem pips, held item name, and a CAUGHT! flash when a catch lands.

## The 111-item catalog (`ItemCatalog.cs`)

Every item has its own name, damage, uses, throw damage/speed, knockback,
size, shape, and color — stats drive the behavior, so items play differently.

- **Melee (24):** Baseball Bat, Cricket Bat, Wooden Club, Steel Pipe, Wrench, Hammer,
  Mallet, Frying Pan, Katana, Broadsword, Rapier, Battle Axe, War Mace, Spear, Halberd,
  Chainsaw, Laser Sword, Spiked Club, Golf Club, Tennis Racket, Broom, Shovel, Torch, Umbrella
- **Guns (18):** Blaster Pistol, Revolver, Shotgun, Machine Gun, Laser Gun, Plasma Rifle,
  Flame Thrower, Freeze Ray, Rocket Launcher, Grenade Launcher, Crossbow, Bow, Slingshot,
  Water Cannon, Bubble Gun, Lightning Gun, Sonic Blaster, Railgun
- **Throwables (28):** Chair, Stool, Crate, Wooden Barrel, Metal Barrel, Rock, Brick,
  Book, Vase, Plant Pot, Table Lamp, Bench, Small Table, Tire, Bowling Ball, Dumbbell,
  Kettle, Bucket, Toolbox, Backpack, Globe, Television, Microwave, Safe, Anvil,
  Treasure Chest, Ale Barrel, Statue Head — heavy props fly slower, hit harder
- **Explosives (16):** Bomb, Big Bomb, Dynamite Bundle, Grenade, Fire Bomb, Ice Bomb,
  Shock Bomb, Powder Keg, Oil Drum, Gas Can, Firework Crate, Land Mine, Sticky Bomb,
  Holy Bomb, Poison Bomb, Cluster Bomb — thrown: explode on hit/floor; catching one
  mid-air is possible and encouraged
- **Food (12):** Cake, Roast Chicken, Pizza, Burger, Sushi Platter, Apple, Banana,
  Watermelon, Cheese Wheel, Honey Jar, Energy Drink, Magic Potion — Light to eat (+heal),
  Heavy to throw it at someone's face instead
- **Special (12):** Fire Wand, Ice Wand, Lightning Rod, Wind Fan, Poison Dagger,
  Vampire Fang, Golden Hammer, Giant Glove, Shock Glove, Magnet Coil, Gravity Orb,
  Mirror Shield — hard-hitting melee with big knockback

Total: **111 items.**

## Scripts

```
Assets/Scripts/
  PlayerController.cs  movement, keys, jump/dodge roll/wall jump, AI (chases gems/items, throws, catches, rolls, Fusions)
  PlayerCombat.cs      melee, items, throwing, catch window, gems, Power Gauge, Drive/Fusion, knockdowns
  ItemCatalog.cs       all 111 item definitions
  ItemVisual.cs        per-item multi-part 3D models + idle animation (pulse/glow)
  GoldCoin.cs          knockdown gold coins (magnet + pickup)
  ItemPickup.cs        an item on the ground
  ThrownItem.cs        flight physics, catch check, hit check, landing/explosion
  Projectile.cs        gun shots and transformed energy wave
  PowerGem.cs          gems (0.6s pickup delay after spawning)
  Spawner.cs           gem/item timers + 8 stage props placed at match start
  StageHazard.cs       patrolling hazard orb
  ArenaCamera.cs       framing/zoom arena camera
  GameManager.cs       timer, KO/winner, restart, HUD
  Health.cs            health + KO
Assets/Editor/
  PrototypeBuilder.cs  menu: Prototype → Build Arena Scene
```

## Honest shortcuts

- Items are procedural primitive assemblies (no model files) — every one of the
  111 has its own built shape now, but they're still primitives under the hood.
- Guns share a projectile system (Rocket/Grenade Launcher shots explode); elemental
  effects (freeze, burn, poison) are named but not simulated yet.
- Throwable-class items have effectively unlimited swings — because you don't swing
  them at all: Light throws them, like Heavy. Chairs are projectiles, not swords.
