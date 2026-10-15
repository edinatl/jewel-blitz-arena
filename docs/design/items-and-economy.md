# Items and Economy

Owner: economy designer, meta gameplay engineer. This is the system the brief calls out as the
one that matters for an architecture read: items are **ownership-gated loot**. What you own
decides what can spawn. Expedition, the shop, and the mixer are progression because of this
rule, not in spite of it. Item power and economy invariants are in `balance.md` §6 and §10.

## 1. Item schema

Authored as data; runtime consumes an exported table keyed by a stable `item_id`.

```
Item
  item_id                 # stable integer, never reused
  name, description
  category                # firearm | explosive | melee | healing | status | mobility | summon | toy | material | card | cosmetic | gift
  rarity                  # 1..7; higher = rarer, with a higher power ceiling (balance.md §6)
  crate_eligibility       # always | when_owned | never
  pocketable              # bool; usable match items only
  stackable_default       # default slot capacity (see §3)
  buy_price, sell_price   # 0 buy = not sold by Tilly
  runtime                 # behaviour block: held/thrown/placed/consumed/summon, durability or ammo, damage, duration, status applied
  cosmetic_socket         # head | arm | waist | leg | back  (cosmetics only)
  tags[]                  # Tinker affinities, from the canonical list below
```

**Canonical tags** (11): flame, tide, storm, stone, star, ice, powder, metal, glass, paper,
food. Every tag has exactly one material and one Tincture. Export fails if a tag has zero
or two (`architecture.md` §11). Ice and tide are both legal. Ice is rime. Tide is water.
They do not counter each other, and neither is a combat type.

Rules:

- `crate_eligibility = always`: the starter set (§2.1) and fighter-unlock items. A fresh
  profile sees 25 item types in crates.
- `crate_eligibility = when_owned`: enters the crate table once the profile's **ever-owned**
  flag for the item is set. Selling to zero quantity does not clear the flag.
- `crate_eligibility = never`: cosmetics, materials, cards, and gifts. Obtained only
  through Expedition, Tinker, the Facet Wheel, or gifts. A `never` item that is also
  `pocketable` (the Solitaire) can still reach a match through Pocket Items (§2.3).
- `rarity` drives crate weight within a category and Tinker outcome tiers.

## 2. Crate table derivation

Built once per match by the mode orchestrator and handed to the sim in `MatchConfig`.

```
eligible = { i | i.crate_eligibility == always }
         ∪ { i | i.crate_eligibility == when_owned and profile.ever_owned[i] }
eligible ∩= custom.item_switch                       # Basic Rule; all on by default
eligible -= stage.excluded_categories                # e.g. no summons in Behind the Counter
pocket   = house_rules.pocket_items ? { p ∈ profile.pocket_items | p.pocketable } : ∅

roll(crate):
  category = weighted_pick(categories with any eligible ∪ pocket item,
                           category_weight[c] * stage.category_bias[c])
  item     = weighted_pick(items in category,
                           i ∈ pocket ? 3 * base_weight[rarity(i)] : base_weight[rarity(i)])
```

- **Two-stage roll.** Category is rolled first, then the item within it. A one-stage roll over
  every eligible item would make each new item dilute the others, so a veteran profile with
  80 items would see healing far less often than a new one. Rolling the category first keeps
  the match's mix stable however large the table grows. Ownership changes *which* weapon
  appears, not *how often* a weapon appears.
- `category_weight` (default): firearm 14, melee 16, explosive 20, healing 22, status 12,
  mobility 6, summon 4, toy 6. Categories with no eligible item are skipped and the rest
  renormalized.
- `base_weight` by rarity: 1: 100, 2: 70, 3: 45, 4: 25, 5: 12, 6: 5, 7: 2.
- `stage.category_bias` only shifts crate categories. Boss arenas lean toward healing.
  Materials, Tinctures, Blueprint Scraps, and Logbooks are `never` on the crate table, so
  they cannot be biased that way. Expedition drops them from containers on a separate
  stream. Citrine Bazaar weights Flame, Ice, Iron, and Gunpowder on that stream. Blueprint
  Scraps and Tinctures drop only in the Pressworks.
- Mode rules: Duel uses only the `always` set at half spawn rate. Tournament uses `always` ∪
  owned. Expedition adds gleam pickups as a separate drop stream (§7), not a crate category.
- Jewels are not in the crate table; they use `stage.jewel_crate_chance` (default 8%),
  subject to the jewel cap (`balance.md` §5).

### 2.1 Starter set (`always`, 25)

| Category | Items |
| --- | --- |
| Firearm (2) | Pistol, Flare Pistol |
| Melee (6) | Iron Pipe, Bat, Pry Bar, Mallet, Paper Fan, Cutlass |
| Explosive (4) | Oil Bottle, Small Bomb, Medium Bomb, Barrel |
| Healing (5) | Apple, Pear, Skewer, Shortcake, Steak |
| Status (4) | Adhesive Spray, Small Shield, Smoke Jar, Leg Irons |
| Mobility (2) | Rollerskates, Spring Boots |
| Summon (1) | Kitten |
| Toy (1) | Trumpet |

Fighter-unlock items, also `always`: Captain's Saber (Captain Citrine), Setter's Pick (Tilly Setter).

### 2.2 Tests

Tests the implementation must pass: owned appears, unowned does not, `never` never appears
except through a pocketed `pocketable` item, category weights sum correctly per stage after
renormalization, the healing share of crates stays within ±2 points between a fresh profile
and a complete one, and the same profile + same seed gives the same crate contents.

### 2.3 Pocket Items

Up to five `pocketable` items the player owns (quantity ≥ 1) can be flagged in the Ledger.
With House Rule 1 on, they join their category's pool at ×3 their rarity weight. Rarity still
counts, so a pocketed rarity-6 Buster is common for a rarity-6 item but never floods the
match. Pocket Items apply in Custom and Online, never in Expedition, which keeps the earning
mode clean. In Online only the host's Pocket Items apply (master plan open decision 5), and
every guest sees them in the table preview.

## 3. Inventory

- Quantity per `item_id`. Default slot capacity by category (weapons 3, explosives 5, food 5,
  status 3, mobility 2, summons 2, toys 2, materials 9, cards 9, cosmetics 1).
- **Capacity shifting.** The player can raise the capacity of one item in the Ledger by one
  step; every step is paid for by lowering another item's capacity by one. Total capacity is
  conserved. This is the brief's "steals capacity from others" rule, made explicit as a
  zero-sum reallocation so the UI can show the trade. Capacity matters because quantity is
  Tinker fodder and sale stock.
- **Expedition pickups.** Each crate-spawned item instance adds 1 to inventory the first time
  the player picks it up, whether or not it is then used. Re-picking the same instance adds
  nothing. Items beyond capacity are converted to sell price in carats at the results screen.
  Pickups in other modes add nothing to inventory.
- **Ledger.** Every item ever handled is recorded with: ever-owned flag, times handled, times
  used, Bench Notes and Logbook notes (§4), and Pocket flag. Handling means picking it up in
  Expedition or receiving it via shop, Tinker, Wheel, or gift. Items seen only in Tournament
  or Custom are shown as silhouettes. The Ledger filters by category, tag, owned, missing,
  and pocketed, and it can bulk-sell one rarity. Any sell above 10,000 carats, including the
  Regent's Diadem, requires a hold to confirm.

## 4. Tinker (crafting)

The bench at Tilly's Bench. A **binary mix with an optional card**; no recipe list is shown
up front. Before the roll, the bench shows the affinity tags and the integer target tier,
including whether a Blueprint Scrap is what lifted the cap to 7. It does not show the pool
or the outcome. Logbooks reveal pools.

Inputs: two of {item, material}, plus an optional card in the card slot: one **Tincture** or
one **Blueprint Scrap**. They compete for the slot, which is the decision. Cosmetics are
not inputs. They have no tags and no combat effect.

Resolution, using integer math only:

1. Compute the pair's **affinity set** = tags(A) ∪ tags(B), and
   `base_tier = (rarity(A) + rarity(B) + 1) / 2` (integer division; rounds half up).
   `tier_shift` = +1 if A and B share a tag.
2. **Tincture:** adds its tag to the affinity set with weight ×4. If that tag was already in
   the affinity set, `tier_shift` +1.
   **Blueprint Scrap:** `tier_shift` +1, and the high-tier limits in step 3 do not apply.
3. `target = clamp(base_tier + tier_shift, 1, 7)`. Without a Blueprint Scrap, `target` is
   capped at 6, and a tier-6 target moves 15 points from success to failure. This makes
   Blueprint Scraps the only route to tier 7 and the practical route to tier 6.
4. Roll an outcome: **success** (55%), **double** (15%, success with 2× quantity), or
   **failure** (30%).
5. On success, pick the result from the **Tinker pool**: items whose rarity equals the target
   and whose tags intersect the affinity set, weighted by intersection size. The pool
   excludes gifts, Logbooks, and Blueprint Scraps. If the pool is empty, step down one tier
   and retry. If tier 1 is empty, the outcome becomes a failure. A doubled result for an
   item at capacity (cosmetics are capacity 1) pays the extra copy's sell price in carats.
6. On failure, consume both inputs and pay one **Scrap** (material) and one **Chit**.
   The card is consumed in every outcome.

**Seeding.** The roll is seeded by `hash(profile_salt, tinker_counter, A, B, card)`, and the
profile saves before the result is shown. Retrying the same inputs from a reloaded save gives
the same result, so reloading cannot reroll. Because the inputs are part of the seed, a
player cannot learn that the next attempt is a failure and feed it junk. v0.1 seeded from the
counter alone, which allowed exactly that.

**Bench Notes.** Every pair the player has tried is remembered in the Ledger with the
outcomes they have seen ("Iron + Flame → Flamethrower, Ember Mallet"). Crafting-discovery
games keep players experimenting by remembering what they found. Hiding outcomes the player
already saw only punishes them for not taking notes.

**Logbooks.** A card category found in Expedition. Giving a Logbook to Tilly unlocks a full
Tinker note for the pair it describes, showing the whole pool but not the roll. Twenty
Logbooks at 1.0, covering the pairs that lead to tier 5–6 results.

Economy design intent: three failures cost roughly what one success is worth, and three
Chits buy one Facet Wheel spin whose expected value is roughly one success. Failure is
therefore a delay, not a loss. The no-arbitrage invariant in `balance.md` §10 is what stops
this generosity becoming a carat printer.

## 5. Facet Wheel

A fortune wheel at Tilly's. One spin costs three Chits. The wheel has eight bands:
item tier 1–2, tier 3–4, tier 5, tier 6–7, material bundle, carats (500–3,000), cosmetic, and
**nothing**. Default band widths favor low tiers and nothing.

Widening: carats can be spent to widen the high-tier and cosmetic bands at the expense of
nothing and tier 1–2. Width levels: 2,000 / 6,000 / 15,000 / 30,000 / 60,000 cumulative.
Widening is permanent on the profile. The 60,000 ceiling is the brief's cited figure and is
the single largest carat sink after the fighter and arena ladder has nothing left to give.
Every band's probability is shown on the wheel at all times; the spin is never a mystery box.

**Monetization firewall.** Carats, Chits, Tinker, and the Facet Wheel are never
purchasable with real money, now or in any later DLC. Paid content, if any, sells fighters,
arenas, or cosmetics outright. This keeps the game clear of loot-box regulation (Belgium has
banned paid loot boxes since 2018, and other markets have followed) and keeps the economy's
generosity honest.

## 6. Shop

Buy, Sell, Tinker, Talk, Ledger, Fitting Room, Facet Wheel. Talk is where Captain Citrine
(after unlock) shows up with gifts.

**Price bands** (buy), by rarity: r1 100–800, r2 800–1,800, r3 1,500–3,500, r4 3,000–5,500,
r5 4,500–7,000. Rarity 6–7 items are never sold; they come from Tinker, the Facet Wheel, and
Expedition clear bonuses. Sell price is 25–35% of buy for weapons and 15–20% for consumables.
Price anchors carried over from the brief as calibration, to be retuned at M4:

| Item | Rarity | Buy | Sell |
| --- | --- | --- | --- |
| Pistol | 2 | 1,500 | 500 |
| Repeater (machine gun) | 4 | 5,500 | 1,900 |
| Launcher (bazooka) | 5 | 6,000 | 1,800 |
| Flamethrower | 4 | 4,500 | 1,200 |
| Beam Gun | 5 | 6,000 | 1,500 |
| Apple | 1 | 100 | 20 |
| Roast Pork | 3 | 2,000 | 400 |
| Cosmetics | — | 800–20,000 | 10% |

v0.1's Flamethrower sold at 18% and the Repeater at 36%, both outside the stated weapon band;
they are corrected above.

Stock rotates: ten items always, five rotating from rarity 3–5, reseeded after every
Expedition stage. Rotating stock marks anything the profile has never owned.

### Gifts

Captain Citrine appears in Talk with a probability that rises with profile playtime and
cumulative carats spent. The chance is shown as a "Citrine is at the bench" flag on the shop menu
so players are not left guessing. Two gifts, each once per profile:

- **Solitaire** (rarity 7, `never`, `pocketable`). A holdable match item: using it grants
  one bonus jewel on the spot. Reaches matches only through Pocket Items. Visible, rare,
  and the thing people post about.
- **Regent's Diadem** (rarity 7, head cosmetic). Sells for **100,000**, the documented carat
  spike. Choosing to wear it or sell it is the point.

## 7. Expedition economy

Expedition pays carats through **gleam pickups** dropped in the match, a post-stage chest, and
a time adjustment. The clear bonus is an item, not carats.

| Gleam pickup | Value |
| --- | --- |
| Copper gleam | 100 |
| Silver gleam | 500 |
| Gold gleam | 1,000 |
| Purse | 2,000 |
| Ingot stack (3) | 3,000 |
| Ingot stack (5) | 5,000 |
| Coffer | 8,000 |

**Budgeted drops.** Each stage has an in-match carat budget. The sim splits it across KOs
(50%), knockdowns the player causes (30%), and broken containers (20%). It picks
denominations that sum to each allocation, using the sim RNG. Only the player can collect
carats, and uncollected objects despawn after 8 s. Budgeting makes the curve predictable for
the simulator, and the collection skill still matters.

**Targets** (expected carats collected for a full clear, before time adjustment; tuned by the
economy simulator at M2 and validated against playtest telemetry at M4):

| Difficulty | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Full clear | 2,000 | 2,600 | 3,300 | 4,000 | 5,000 | 6,000 | 7,000 | 8,000 |

The difficulty-8 figure is the brief's anchor: roughly one Launcher, or the second Facet
Wheel widening step. Per-stage split at difficulty 8, about 45% in-match and 55% chest:

| Stage | 1 FFA | 2 FFA | 3 Harvester | 4 FFA | 5 Vitre | Total |
| --- | --- | --- | --- | --- | --- | --- |
| Carats | 1,100 | 1,300 | 2,000 | 1,500 | 2,100 | 8,000 |

Later stages pay more, so a full clear always beats quitting early on carats per minute
(`balance.md` §10).

- **Time adjustment:** the post-stage chest is scaled ±20% for finishing under or over par
  time, linear between 0.75× and 1.5× par.
- **Post-stage chest:** contents are drawn to hit that stage's chest share. At difficulty 5
  and above it has a 20% chance of being a purse instead of gleams.
- **Coffer** (8,000) appears only as the Teller's Reel jackpot in Expedition (2%
  band) and as a 10% roll on a difficulty-8 clear under par time. It is a story to tell, not
  a farming target, and its expected value is included in the targets above.
- **Clear bonus:** one special item, rarity 6 at ≥ 70% probability on difficulty 8, rarity 5
  otherwise; faster time raises the rarity 6 odds by up to 15 points. The pool is items of
  that rarity the profile has never owned. Only if none remain does it fall back to owned
  ones. That is what makes the bonus "something new to the crate table" true.
- A failed run keeps carats and items collected up to the failed stage (no chest for that
  stage).

Carat sinks, in order of lifetime spend: shop weapons, Facet Wheel widening, cosmetics,
capacity shifting (free, but motivates buying duplicates), and Tinker inputs.

## 8. Item roster by functional category

Counts are the 1.0 target and include variants: 80 usable items, plus the Solitaire.
Damage values must fit the rarity bands in `balance.md` §6. Rarity is in brackets where it
matters for balance.

**Firearms and energy (10).** Pistol, Repeater, Launcher, Flamethrower, Beam Gun, Arm Cannon
(worn, fires with Attack, 20 shots), Spike Gun (Pins to walls), Buster [6] (one shot,
whole-line knockdown, 180), Net Gun (Pin), Flare Pistol.

**Thrown and placed explosives (12).** Oil Bottle, small/medium/large Bombs, Stick Bomb
(sticks to actors), Fireworks (random spray), Ember Nest (cinders chase the nearest actor for
6 s), Proximity Mine, Chain Bomb (three linked), Barrel, Seam Charge (rolls along the floor;
blast radius ×1.5 while the user is standing in a water volume), Confetti Bomb (Dizzy, no damage).

**Melee (16).** Iron Pipe, Cutlass, Longsword, Glass Sword [6] (one swing for 250, then
breaks), Mallet, Rod, Bat, Glacier Tusk, Paper Fan (knockback, no damage), Flail, Reaper's
Hook [7] (hits pull in, 90 per swing, 5 swings), Pry Bar, Setter's Pick, Captain's Saber, Keystone
(two-handed, slow, `otg`), Tuning Fork (Stun on hit).

**Healing (10).** Apple, Pear, Skewer, Shortcake, Steak, Noodle Bowl, Feast Platter,
Wedding Cake, Roast Pork, Elixir [6] (full heal).

**Status (12).** Adhesive Spray (Slow), Petrify Gas (Petrify, 90 f), Vanishing Powder (6 s
shimmer: drawn as a faint outline, enemy CPUs lose track), Small/Large Shield (absorbs 2/4
hits; not a block input), Leg Irons (thrown, Slow), Bear Trap (Pin), Tar Bucket (Slow
patch), Mirror (reflects one projectile), Smoke Jar, Ice Jar (freezes a floor patch),
Loudhailer (Dizzy in a cone).

**Mobility (6).** Glider Wings, Rollerskates, Spring Boots, Winged Sandals (triple jump),
Grapnel, Aether Mask (immune to water slow, ink, smoke, and abrasive dust).

**Summons (6).** Kitten (snatches the nearest opponent's held item and runs off with it
for 4 s; drops it when hit), Panther (chases one target), Flame Drake, Frost Drake, Storm
Drake, Skylark (carries the user for 3 s).

**Toys (8).** Stage gimmicks that became inventory items: Trumpet (knockback blast), Meteor
Chart [5] (calls one telegraphed strike, 60 f ring, 180; unlike Squall it is not a KO),
Typhoon Jar, Pitfall Hoop, Jewel Catalog [6] (drops one bonus jewel at a random jewel
point, announced), Thriller Novel [5] (all opponents Dizzy for 60 f, immunity applies),
Wishing Geode (random: heal 150, a bonus jewel, a Medium Bomb at your feet, a gold gleam
in Expedition, or Dizzy yourself), Brood Gem (bursts on impact into a slow patch; the
inventory cousin of the Orrery egg sac, and it does not spawn a young, so it cannot break
the stage-creature cap).

**Materials (12).** One material per canonical tag, plus Scrap: Flame (flame), Tide (tide),
Storm (storm), Stone (stone), Star (star), Ice (ice), Gunpowder (powder), Iron (metal),
Cullet (glass), Paper (paper), Ration (food), Scrap (no tag). All are rarity 1–2 except
Scrap, which is rarity 1 and the failure output. There is no Gold material. Currency is
carats, and the old metal-plus-light pair is a mix of Iron and Star. Cloth folded into star,
which is what wings and light cloth use. A food tag with no material was a hole in the
"one material per tag" rule. Ration fills it.

**Cards.** Tinctures ×11 (one per canonical tag), Blueprint Scrap (one stackable kind),
Logbooks ×20.

**Cosmetics (~25, Fitting Room only).** Head: hats, crowns, animal ears, wind-up key. Arm:
paws, gauntlets, bracelets. Waist: tails, sashes. Leg: boots, anklets. Back: small wings,
kites, barrels. Appearance only; no stat or hitbox change, verified by a test that compares
hitbox data with and without cosmetics. Silhouette limits in `cast.md` §7.

Name history, so the collisions do not come back: v0.1 used one word for the shop wheel, an
in-match toy, and the Strongroom reel. They are the **Facet Wheel**, the **Wishing Geode**,
and the **Teller's Reel**. The Geode was briefly a shell, and the wheel was briefly a tide.
Chits were Salvage Tags. Scrap is the material. A Chit is the token. They are not the same
object. The Kitten no longer "distracts CPU", which did nothing against humans.

## 9. Save-relevant data

Profile stores: inventory quantities, capacity shifts, ever-owned flags, Ledger counters,
Bench Notes, Pocket flags, profile salt and Tinker counter, Facet Wheel width level, gift
flags, Logbooks given, carats. Schema is versioned; see `docs/tech/architecture.md`.
