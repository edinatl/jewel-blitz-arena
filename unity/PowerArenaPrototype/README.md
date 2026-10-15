# Power Arena — Prototype v0.2

An original arena brawler inspired by Power Stone 2's formula.
2 fighters, 1 arena, local play + a solo bot. No Capcom assets, names, or characters.

## What changed in v0.2 (Ben's rules)

- **ANY hit knocks a gem loose.** No damage threshold. Melee, shots, bombs, thrown
  items, explosions, and the stage hazard all shake one gem out of whoever got hit.
  The gem flings away and can't be re-grabbed for 0.6s — it's a scramble, not a refund.
- **110 items in the game** (see the catalog below), all procedurally spawned.
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
| Jump | Space | Right Shift |
| Light attack / use held item / **CATCH (empty hands)** | F | . (Period) |
| Heavy attack / **THROW held item** / transformed special | G | / (Slash) |
| Restart match | R | R |

**Solo play:** Player 2 starts as AI. Any Player 2 keypress takes manual control.

## How a match works

- **120-second timer.** KO, or lead on health at time-up.
- **Power Gems (red):** collect **3** → transform for 12s: gold, bigger, faster,
  double light damage, energy wave on Heavy (empty-handed). One gem is knocked
  out of you by *any* hit while you're carrying them.
- **Items:** walk over an item with empty hands to pick it up.
  Light = use it (swing / shoot / eat). Heavy = throw it at the other fighter.
  Thrown items that miss land back on the floor as pickups again.
- **Stage Hazard (purple orb):** patrols the arena; it damages and knocks gems loose too.
- **HUD:** health, gem pips, held item name, and a CAUGHT! flash when a catch lands.

## The 110-item catalog (`ItemCatalog.cs`)

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

Total: **110 items.**

## Scripts

```
Assets/Scripts/
  PlayerController.cs  movement, keys, jump, AI (chases gems/items, throws, catches)
  PlayerCombat.cs      melee, items, throwing, catch window, gems, transformation
  ItemCatalog.cs       all 110 item definitions + procedural visuals
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

- Items are colored primitives (a "Television" is a box with TV stats) — stats first,
  real models in the art pass.
- Guns share a projectile system (Rocket/Grenade Launcher shots explode); elemental
  effects (freeze, burn, poison) are named but not simulated yet.
- Throwable-class items have effectively unlimited swings — because you don't swing
  them at all: Light throws them, like Heavy. Chairs are projectiles, not swords.
