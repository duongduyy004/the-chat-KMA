# One Hero Across Sprint, Football and Volleyball

**Date:** 2026-09-29
**Status:** Approved in conversation (character pack, hero, roles)

## Goal

The player controls the same character in all three minigames. Sprint, Football and
Volleyball draw every character from one pack, **Kenney Toon Characters** (CC0), and the
player is always the pack's `Male adventurer`. The main player looks the same in every
game: same sprite source, same import settings, same outfit, no tint.

## Decisions

- **Pack:** Kenney Toon Characters, `https://kenney.nl/assets/toon-characters`, CC0 1.0.
  Kenney Modular Characters was considered and rejected because it has front-view parts
  only and no poses.
- **Hero:** `Male adventurer` (project folder `MaleAdventurer`). He is already the Sprint
  player.
- **Supporting cast** (all from the same pack; `Robot` and `Zombie` are not used):

| Role | Character |
|---|---|
| Sprint rival, lane 1 | `MalePerson` |
| Sprint rival, lane 3 | `FemalePerson` |
| Sprint rival, lane 4 | `FemaleAdventurer` |
| Football goalkeeper | `MalePerson` |
| Volleyball opponent | `FemaleAdventurer` |

- **Resolution:** the pack's `PNG/Poses HD` images, 192×256, imported at 200 pixels per unit
  so that each pose is 0.96×1.28 world units (the Sprint runners' current size). Every pose
  uses a bottom-centre pivot, bilinear filtering and no mipmaps.
- **One import path:** a single editor class imports and loads every pose. All three scene
  configurators go through it.
- **Hero is never tinted or recoloured.** Opponents are told apart by being different pack
  characters, not by tint.

## Per-game use

| Game | Hero poses | Notes |
|---|---|---|
| Sprint (side view) | `idle`, `run0-2`, `hit`, `cheer0-1`, `fallDown` | Same states as today. Only the file source changes to HD. |
| Football (hero seen from behind) | `back` (ready), `climb0` (run-up), `climb1` (strike), `hurt` (arms wide, used for the goal celebration) | The keeper (`MalePerson`) uses `fall` (arms-spread ready pose and dive, rotated as today), `hold` (save) and `hit` (beaten). |
| Volleyball (side view, 2.5D) | idle `idle`; run `run0,run1,run2,run1`; receive `duck,hold`; smash/serve `jump,attack1`; block `jump,cheer1`; dive `fall,slide` | The opponent (`FemaleAdventurer`) uses the same pose table, mirrored and not tinted. |

## Football keeper collision

`FootballFlightSimulation` decides saves with capsules that trace the keeper sprite in
preview-pixel space around the hip at y = 281, where the dive rotates. The new keeper
sprite is drawn 108×144 preview px, with its feet pivot 21 px below the hip, so the dive
still rotates about the hip. The capsules are refitted to the `fall` pose. A test pins the
fit: at least 85% of the pose's opaque pixels must fall inside the capsules, and at least
85% of the capsule area must lie on opaque pixels.

## Out of scope

- The Volleyball environment art from BVA2 (court background, ball, ball shadow) stays. Only
  the BVA2 character sheets are removed.
- The Sprint track, campus and sky artwork is unchanged.
- There is no character selection or customisation.
