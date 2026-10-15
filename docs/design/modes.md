# Modes, Progression, and Unlocks

Owner: meta gameplay engineer, economy designer. Mode orchestrators sit outside the sim. Each
builds a `MatchConfig`, runs a match, consumes a `MatchResult`, and updates the profile.

## 1. Mode list

| Mode | Players | Item economy | Progression writes |
| --- | --- | --- | --- |
| Duel | 1–2 | `always` crate set at half rate | Fighter endings |
| Tournament | 1–2 humans + CPU | `always` ∪ owned | House Rules, fighter unlocks, extra arenas, palettes |
| Custom | 1–4 humans/CPU | Full, incl. Pocket Items | None |
| Expedition | 1 | Owned, plus gleam pickups (no Pocket Items) | Ledger, inventory, carats, clear bonus |
| Tilly's Bench | 1 | — | Inventory, carats, Ledger notes, gifts |
| Online | 2–4 humans, optional CPU fill | Host's profile table | None (1.0) |
| Profile / Save | — | — | — |

## 2. Duel

The story ladder as one-on-ones: five story arenas, two bosses, seven fights, fighter ending
on clear. Each fight is a 2-actor match, so each fighter starts holding one jewel
(`balance.md` §5). A second controller can join at any time as the opponent, replacing the
CPU for the current fight. No carats, no Ledger. Duel exists so the game has a clean,
item-light way to learn a kit.

## 3. Tournament

The arcade ladder. **Five stages**: two free-for-alls, the Harvester, a third free-for-all,
and Vitre. This is the reference's actual shape. The brief's "three free-for-alls and two
bosses, six stages total" does not add up, and v0.1 had inherited the error.

1. Story arena (FFA, 4 actors)
2. Story arena (FFA, 4 actors)
3. **Terraces — The Harvester** (the two advancing fighters cooperate)
4. Story arena (FFA, 4 actors: the two advancing plus two fresh CPUs)
5. **The Pressworks — Vitre** (the two advancing fighters cooperate)

Stages 1–2 are drawn from {Citrine Bazaar, Fulgur Cloister, Sunken Strongroom}, rotating by
clear count so all three are seen within two runs. A fresh profile always opens on Citrine Bazaar. Stage 4 alternates between {Rime Cutworks, The Orrery}.

**Difficulty.** CPU level 1–8, chosen at start, default 4. Every difficulty counts for
unlocks; the clear record stores the highest difficulty per fighter.

**Advancement.** A Tournament FFA ends the moment only two fighters remain, and both
advance. That keeps FFAs short (`balance.md` §1). The two advancing fighters are about to
fight the boss together, so they have not just spent the stage trying to KO each other. If no
human is among the two, the run ends. A continue restarts the stage and **still counts
toward unlocks**. The reference counted continued clears too, and a party game should not
withhold the roster from players who needed one more try. The results card marks a
continue-free clear with a badge.

**Two humans.** The run continues while at least one human advances. A human who did not
advance rejoins at the next stage in place of a fresh CPU. Every human-controlled fighter
present at the Vitre clear gets the distinct clear on the primary profile.

**Bosses.** The two advancing fighters cooperate. Rally applies. A boss timeout grants one
retry of that stage. A second human can drop in as Ally or Rival at any CPU-filled stage.

Clears are tracked per fighter. A **distinct clear** is the first clear with a given fighter.

## 4. Custom

The rules sandbox. One to four humans or CPUs, the Basic Rules in `combat-and-match.md`
§10.1 (time limit, teams 2v2 / 1v3 / 2v1v1, item frequency, item switch, handicap, stage
hazards, Wisps, per-slot CPU level), House Rules as unlocked, and stage selection. The stage list includes boss
arenas once each has been cleared in Tournament (1–4 fighters against the boss, with Life
scaled per `balance.md` §8) and the Extra Arena box past the end of the list. With teams on,
Rally applies. Pocket Items apply when House Rule 1 is on.

Presets: Classic (all defaults), Brawl (item frequency high, Jewel cap 7), Pure (items
off, stage hazards off; the competitive baseline), Boss Rush (both bosses back to back,
unlocked at 10 distinct clears), and **Sparring** (always available, local only).

Sparring loads the Terraces geometry with the boss absent. It does not require the clear-9
unlock. That unlock adds Terraces to the normal Custom stage list. Sparring's copy is a
training stage id and is not that menu entry. Rules: hazards off, item frequency off, time
off, Wisps off, 1–2 fighters, CPU level per slot. `MatchConfig.training` carries
`jewel_mode: normal | respawn` and `item_spawner: off | on`. Respawn puts jewels back on
their points 2 s after a Blitz ends and turns Dross off. The item spawner is a pause-menu
insert of one crate-table item at a point. The insert is a sim event, so a replay that used
it stays deterministic. Hitbox display is presentation-only. Online lobbies reject any
config with training flags set. A non-default Basic or House Rule shows a stamp in the
Custom and Online lobby before ready-up, so a quiet rule change cannot disguise itself as
the default match.

## 5. Expedition

Single-player. The Tournament ladder shape (five stages), but every item picked up is
**handled** into the Ledger, hits and containers drop gleam pickups, and each stage ends with
a chest. Difficulty 1–8 chosen at start. FFAs end when two fighters remain, as in
Tournament, except at difficulty 1–2, which are 1-on-1 and run to last standing.

| Difficulty | Opponents per FFA stage | Gleam pickup scale | Boss difficulty scale | Boss ally |
| --- | --- | --- | --- | --- |
| 1–2 | 1 (plus crates) | 0.5 | 0.6 | none (solo) |
| 3–4 | 2 | 0.7 | 0.8 | runner-up CPU |
| 5–6 | 2 in stage 1, 3 in stages 2 and 4 | 0.85 | 1.0 | runner-up CPU |
| 7–8 | 3 | 1.0 | 1.2 | runner-up CPU |

The boss ally is the CPU who advanced alongside the player from the previous FFA. It plays
at the run's difficulty, can Rally the player, and cannot collect carats. Boss Life also uses
the participant factor in `balance.md` §8, so a solo boss at difficulty 1–2 is sized for one
fighter.

Clear bonus and carat targets are in `items-and-economy.md` §7. A failed run keeps carats and
items collected up to the failed stage (no chest for that stage). Drop-in humans earn nothing
so single-player progression stays single-player.

## 6. Tilly's Bench

Buy, Sell, Tinker, Talk, Ledger, Fitting Room, Facet Wheel. Opened from the main menu and
offered after every Expedition stage. Talk hosts Tilly's hints (which Logbooks exist, which
pairs she has notes for) and Captain Citrine's gifts after her unlock.

## 7. Online

Lobbies of 2–4 humans for Custom rules, with optional CPU fill up to four actors. CPUs run
inside the deterministic sim, so they cost no extra network traffic.

- **Host and rules.** The host picks stage, Basic Rules, and House Rules (limited to the
  host's unlocks). The eligible crate table is derived from the host's profile and shown to
  everyone before ready-up. Content hashes must match (`architecture.md` §4).
- **Joining.** Steam friend invites and a public lobby browser filtered by region and ping.
  No ranked play and no progression writes at 1.0; the point is that couch Custom works at a
  distance.
- **Input delay.** Negotiated per lobby from measured round-trip times (`architecture.md`
  §3). A peer whose latency would push the lobby past 4 frames of delay is warned before
  ready-up.
- **Disconnects.** When a peer drops, the remaining peers agree on a frame and that fighter
  is handed to a CPU at the lobby's CPU level from that frame on. The match continues and
  the results card marks the takeover. A peer who is silent for 3 s stalls the match with an
  on-screen "waiting" timer and is dropped at 10 s.
- **No pause** online; Photo Mode is local-only.
- **Spectating** a lobby of four from a fifth seat is a stretch goal and is cheap because
  the sim is deterministic from the input stream.
- **Remote Play Together** (Steam's streamed local multiplayer) is supported from M1 at no
  engineering cost. It gives players a way to play couch modes online, including
  Tournament, before and alongside rollback.

## 8. Unlock ladder

Everything unlockable, its condition, and where it is tested. Every distinct clear from 1
to 14 unlocks something.

| Distinct clears | Unlock |
| --- | --- |
| 1–6 | House Rules 1–6, in order (one per clear) |
| 7 | House Rule 7 (Phase Clock) and Captain Citrine |
| 8 | Sound Test in the Profile menu, and Tilly Setter |
| 9 | Terraces (no boss), Extra Arena slot 1 |
| 10 | Boss Rush preset in Custom |
| 11 | Behind the Counter, Extra Arena slot 2 |
| 12 | Tournament option "Remix": random stage order and random boss ally CPUs |
| 13 | Spirewalk, Extra Arena slot 3 |
| 14 | The Cascade, Extra Arena slot 4 (requires the full cast, including both unlocks) |

| Other unlock | Condition | Notes |
| --- | --- | --- |
| Boss arenas in Custom | Clear the boss once in Tournament | Per boss |
| Fighter palettes 3–4 | That fighter's first Tournament clear | Per fighter |
| Fighter endings | Duel or Tournament clear with that fighter | |
| Expedition difficulty 7–8 | Clear Expedition at 5 or higher | |
| Captain Citrine gifts | Her unlock, then probability by playtime and carats spent | Each once |
| Photo Mode | Always available from pause (local) | Cosmetic |

v0.1 put Terraces at "12 distinct clears (all base fighters)". Once the two unlocks join the
roster, those are different conditions, since twelve distinct clears no longer have to be
the twelve base fighters. It also left clears 9–11 with no reward. The ladder above uses a
plain count throughout and gives every clear a reward. The brief's disagreement about how
many clears each extra arena needs is settled by this fixed ladder. Automated tests drive a
synthetic profile through each condition and assert the unlock flips.

## 9. Save and profile

One profile per platform account, cloud-synced where the platform provides it. Multiple local
profiles supported for shared consoles. Profile contents are listed in
`items-and-economy.md` §9 plus: distinct-clear set with highest difficulty per fighter, House
Rule unlock count, boss clears, Expedition best times and difficulties, fighter endings seen,
palettes, settings, assists, remaps. Schema versioning and migration in the tech spec.

## 10. Front-end flow

```
Title → Main Menu
  Duel → fighter → ladder
  Tournament → difficulty → fighter(s) → ladder
  Custom → preset/rules → fighters → stage (list + Extra Arena box) → match → results → rematch/exit
  Expedition → difficulty → fighter → ladder (shop offered between stages) → results → clear bonus
  Tilly's Bench → Buy | Sell | Tinker | Talk | Ledger | Fitting Room | Facet Wheel
  Online → host/join/browse → lobby (rules, table preview, ready) → match → results
  Profile → select | settings | assists | remap | House Rules (read-only view) | sound test (after unlock)
```

Pause (local) → Resume | Remap | Assists | Photo Mode | Phase timeline | Quit. Photo Mode
freezes the sim, frees the camera, hides HUD, and offers sticker frames. The phase timeline
is the same pause-only bar as in `combat-and-match.md` §2.

**Rematch.** Duel, Custom, and Online results offer rematch: the same `MatchConfig`, a new
seed. Tournament and Expedition do not. They already have continue and the boss retry, and
a rematch mid-ladder would fork the run. Local results also offer **Watch replay** once the
input log is kept (M4). The replay format is already `MatchConfig` plus the input log
(`architecture.md` §3).

**Results.** The card shows placement, KOs, jewels taken, Blitz count, and a one-line
damage split (fighter, items, Blitz, stage). The full source breakdown waits for M4
telemetry. Continue-free Tournament clears keep their badge.

**First-run onboarding.** A fresh profile's first Tournament stage shows three one-line
prompts at the moments they matter: the first jewel ("Collect three to Blitz"), the first
incoming telegraphed hit ("Flick the stick to dodge"), and the first phase change. Each
prompt shows once per profile. There is no separate tutorial mode. Sparring is the practice
room. Duel is the story-shaped way to learn a kit.
