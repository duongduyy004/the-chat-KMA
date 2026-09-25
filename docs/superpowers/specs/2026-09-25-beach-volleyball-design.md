# Beach Volleyball Minigame

**Date:** 2026-09-25
**Status:** Approved (design); spec under review

## Goal

Add a new playable subject, Beach Volleyball: a 1v1 top-down 2.5D match against an
authored AI, built from scratch on the art in `BVA2.zip`. The player moves with a virtual
joystick and uses one context-sensitive action button with timed presses. First to 5
rally points wins; the match is capped at 120 s.

## Constraints

- Written from scratch. Nothing is restored or adapted from the volleyball code removed
  in `d313f0b`.
- Rule models use no random number generation. AI variety comes from an authored,
  cyclic plan.
- Shared lifecycle `Tutorial → Countdown → Play → Resolve` with exactly one completion
  event; the result uses the shared 0–10 formula via `ScoreUtil`.
- Landscape mobile first; keyboard fallback for Editor and QA.
- Existing Sprint and Football progress in v5 saves must survive.

## Source assets (`BVA2.zip`)

| File | Size | Use |
|---|---|---|
| `beachbkgO.png` | 400×430 | Court background (orthographic lines; the net line is at x=200 px) |
| `beachbkgIso.png` | 400×430 | Not used |
| `net0.png` | 270×450 | Net, 6 frames of 45 px (idle frame plus a wobble when hit) |
| `playerIdle/Run.png` | 384×43 | 12 frames of 32 px |
| `playerReception.png` | 352×43 | 11 frames of 32 px |
| `playerBlock/Smash.png` | 416×46 / 416×50 | 13 frames of 32 px |
| `playerSlide.png` | 645×43 | Dive, 15 frames of 43 px |
| `ballFull/Shot/Bounce/BounceHard/Roll.png` | height 20 | Ball spin, shot and bounce sheets, sliced into 20 px-high frames. Frame widths are checked when the sheets are imported. |
| `shadow1.png` | 23×10 | Ball shadow |
| `bubbleOK.png`, `pushbutton.png` | — | Not used; the HUD uses the existing UI theme |
| `__MACOSX/*` | — | Discarded |

Imported to `Assets/_Project/Art/Characters/BeachVolley/` and
`Assets/_Project/Art/Environments/Volleyball/` as point-filtered, uncompressed sprites
sliced into grids. The opponent reuses the player sheets, mirrored and tinted red with
`SpriteRenderer.color`.

## Architecture

Assembly `KMA.Gameplay.Volleyball` in `Assets/_Project/Scripts/Gameplay/Volleyball/`.
Game state lives in plain C# classes; MonoBehaviours only pass input and time into
those classes and draw what they report.

### Plain C# model

| Unit | Responsibility |
|---|---|
| `CourtSpace` | Court in metres: 8 m deep (y) × 16 m long (x). The net is at x=0; the player's half is x<0 and the AI's is x>0. Answers "is this point in/out" and "which side is it on", and maps court points to pixels on the background linearly (outer lines at x 17→383 px, y 145→413 px). |
| `BallFlight` | The arc from a launch point to a target point, given an apex height. `Sample(t)` returns ground (x, y) and height z. Also reports landing time, `TimeAtHeight(z, descending)` and net clearance: z at x=0 must be ≥ net height 2.24 m. |
| `VolleyAthlete` | Position, facing, move speed (5 m/s for the player), current action (Idle, Run, Receive, Smash, Block, Dive, Serve) and the lockout while an action plays. Movement is clamped to the athlete's own half and the area just outside it. Used for both the player and the AI. |
| `ActionResolver` | Maps (athlete, ball, rally state, press time) to an action (Serve, Receive, FreeBall, Smash, Dive, Block or None) and a timing quality. |
| `RallyState` | Which side the ball is on, the touch count per side, the last toucher, and detection of: landed in, landed out, net fault, and a fourth touch. |
| `OpponentPlan` | Authored 8-step cycle driving the AI (see below). |
| `VolleyballMatch` | The top-level model. Holds the score, the server, the 120 s clock and `MinigameLifecycle`, and runs the point → reset → serve loop. Raises `PointScored`, `BallHit`, `Fault` and `Completed`, and builds the `MinigameResult`. |

### Unity layer

| Unit | Responsibility |
|---|---|
| `VolleyballController : MinigameBase` | Owns `VolleyballMatch` and ticks it with `min(deltaTime, 0.1)`. Passes joystick and button input in and completes the minigame once. If a required reference is missing, it logs one error and disables itself. |
| `VolleyAthleteView` | Places the athlete's sprite, flips it to face the net, sets its sorting order from ground y, and picks the flipbook clip from the current action. |
| `VolleyBallView` | Draws the ball at screen `y + z·ppmY·0.8`, where ppmY is the vertical pixels per metre (268 px / 8 m) and the shadow at ground y. The shadow scales from 100% down to 60% at the top of the arc. The ball always draws above the athletes. |
| `SpriteFlipbook` | A small component that plays a frame array at about 12 fps, looping or once. Used instead of Animator controllers. |
| `VirtualJoystick` | A floating stick in the left 40% of the screen: it appears where the thumb lands and outputs a normalized Vector2. |
| `ActionButton` | A fixed button in the bottom right; it timestamps each press. |
| `VolleyballInputBridge` | Merges touch input with keyboard input (WASD or arrows plus Space), using a new `Volleyball` action map in the existing Input System asset. Provides `FeedMoveForTest` and `FeedActionForTest`. `GameplayInputRouter` is not modified. |
| `VolleyballHud` | Score, remaining time, and PERFECT/GOOD/LATE feedback. Built on the existing HUD and `UITheme`. |
| `Editor/VolleyballSceneConfigurator` | The `KMA/Volleyball/Build Scene` menu. It imports and slices the art and builds `MG_Volleyball.unity`: camera, background, net, athletes, ball, shadow, UI and controller wiring. If a BVA2 source image is missing, it fails and names the file. |

## Match flow

1. **Tutorial** card (joystick plus button) → **Countdown** → **Play**.
2. **Serve.** The player serves first; after that, the winner of each point serves.
   - **Player serve:** the joystick moves along the baseline only. The first press
     tosses the ball; the second press hits it. A press within ±0.12 s of the top of the
     toss is a PERFECT serve: fast and flat, aimed where the joystick points on the AI's
     half. Otherwise the serve is slow and high to the middle. If the toss lands
     without a second press, it is a fault.
   - **AI serve:** follows the current plan step, 1.0 s after the point starts.
3. **Rally.** Each side may touch the ball up to 3 times (see Actions).
4. **Point.** Play pauses for 1.2 s with the score shown, then resets to the serve.
5. **Resolve.** Completion fires exactly once: when either side reaches 5, or when the
   120 s cap is reached. At the cap, the higher score wins; a tie counts as a loss.

## Actions (player's button)

Contact height for timing: the ideal moment is when the descending ball passes z = 1.0 m
(Receive, Dive) or reaches its apex (Smash, Block jump).

| Action | Available when | Result |
|---|---|---|
| Receive | Touch 1 or 2. Ball within 1.0 m of the athlete (ground distance) and descending. | High ball to a set-up spot about 1.5 m from the net on the player's side. Timing quality scales how far it lands from the ideal spot: 0 m at PERFECT, up to 2.5 m at LATE. |
| FreeBall | Touch 3 when Smash is not available. | High, safe lob to the centre of the AI's half. |
| Smash | Touch 2 or 3. Athlete within 3.0 m of the net, ball z ≥ 2.2 m near its apex, ball within 1.0 m. | Fast, low shot aimed with the joystick (deep/short × left/right, default deep centre). At GOOD or better, the landing spot is exactly on the aim. At LATE it either lands in the net (early press) or becomes a soft lob (late press). |
| Dive | Ball descending, z < 1.0 m, landing between 1.0 and 2.2 m from the athlete. | The athlete lunges in the joystick direction, and the ball becomes a weak, low Receive. The athlete is locked out for 0.8 s. |
| Block | The AI is winding up a smash (a 0.35 s tell), the athlete is within 1.2 m of the net and within 1.0 m of the smash's line. | The athlete jumps. If the smash passes within 1.0 m of the block when hit, the ball rebounds onto the AI's half and the player wins the point. |

**Timing windows** are measured from the ideal moment:

| Grade | Window | Timing quality |
|---|---|---|
| PERFECT | ±0.08 s | 1.0 |
| GOOD | ±0.18 s | 0.6 |
| LATE/EARLY | ±0.30 s | 0.25 |

A press outside ±0.30 s, or when no action is available, does nothing and is not
counted. A contact marker on the shadow shrinks toward the ideal moment.

**Faults and points:**

| Event | Point goes to |
|---|---|
| Ball lands in court | Whoever is not defending that half |
| Ball lands out | Opponent of the last toucher |
| Hit fails net clearance | Opponent of the hitter |
| Fourth touch on one side | Opponent of that side |
| Serve toss dropped | Receiver |

## AI (`OpponentPlan`)

- The AI moves at 85% of the player's speed (4.25 m/s) toward the predicted contact
  point, starting after a fixed 0.25 s delay. Its timing is always GOOD, except on
  steps authored as a weak receive.
- An authored 8-step cycle, advanced once per AI possession:

| Step | Serve target | Attack |
|---|---|---|
| 1 | Deep centre | Smash, deep corner away from the player |
| 2 | Short left | Tip, short over the net |
| 3 | Deep right | Lob, deep line |
| 4 | Deep left | Smash, cross-court |
| 5 | Centre | Weak receive → free ball (gives the player a smash chance) |
| 6 | Short right | Smash, line away from the player |
| 7 | Deep corner | Tip, over the player's block position |
| 8 | Centre | Lob, deep centre |

- "Away from the player" means the court corner or line farthest from the player's
  position at the moment the AI hits. This depends only on game state, so it is
  repeatable.
- On steps 1, 4 and 6, if the AI is at the net when the player smashes, it blocks. The
  block wins the point only if the player's aim passes within 1.0 m of the AI; aiming
  away beats it.

## Scoring (0–10)

`ScoreUtil.Build(pass, accuracy, efficiency, mastery)` with:

- **pass (objective 6):** the player wins the match.
- **accuracy 0..2:** 2 × the mean timing quality over all counted player presses (0 if none).
- **efficiency 0..1:** 1 − opponent points / 5.
- **mastery 0..1:** min(winners, 5) / 5, where a winner is a point won directly by a
  player smash or block.

## Integration

| Area | Change |
|---|---|
| `SubjectId` | Add `Volleyball = 7`. This is a new value, so legacy saves that stored ids 1–5 (retired subjects) can't be read as Volleyball progress. |
| `SaveData` / `SaveSystem` | Bump `CurrentVersion` to 6. Migrate v5 to v6: subject records are matched by id, so Volleyball gets a fresh record. `tutorialSeen` for v5 maps Sprint→0 and Football→1; Volleyball starts false. Without this, a v5 save fails `IsCurrentVersionStructureValid` because the subject count changes, and is reset. |
| `SceneRouter` | Add `{ Volleyball, "MG_Volleyball" }`. |
| `MapPresentationBuilder` | Add an entry between Sprint and Football: `"Bóng chuyền"`, playable, with its own colour. |
| `MinigameUIAssembler`, `S5RouteBootstrap`, `S5SceneConfigurator` | Register the new subject wherever Sprint and Football are listed. |
| `SubjectConfig` | New `ScriptableObjects/Subjects/Volleyball.asset`. |
| `EditorBuildSettings` | Add `MG_Volleyball.unity`. |
| Input asset | New `Volleyball` action map: `Move` (Vector2, WASD/arrows) and `Action` (Space). |
| `README.md` | Add Volleyball to the scenes and controls tables and the subject list. |

## Error handling

- Frame time is capped at 0.1 s, so the ball can't skip past a timing window or the
  ground after a hitch.
- Pause freezes the match, because the match only advances when `Update` feeds it time.
- The completion guard makes the result fire exactly once.
- The configurator fails fast, naming the missing source file. The controller logs
  one error and disables itself when it is misconfigured.

## Testing

**EditMode** (`Assets/Tests/EditMode/Gameplay/Volleyball/`):

- `BallFlightTests`: the arc passes through its landing point and apex height, and
  `TimeAtHeight` and net clearance are correct.
- `CourtSpaceTests`: in/out and side tests; the court corners map to the background's
  line pixels.
- `ActionResolverTests`: every row of the action table, and the PERFECT/GOOD/LATE
  window boundaries.
- `RallyStateTests`: every fault and point rule, and the fourth touch.
- `OpponentPlanTests`: the cycle is 8 steps and wraps, and identical inputs produce
  identical AI decisions.
- `VolleyballMatchTests`: the first to 5 wins; at the 120 s cap a lead wins and a tie
  loses; completion fires once; server rotation; the result formula.
- `SaveSystemTests`: a v5 save with Sprint and Football progress migrates to v6 intact,
  with a fresh Volleyball record.

**PlayMode:**

- Scene contract: `MG_Volleyball` loads with a controller, both athletes, the ball, the
  shadow, the joystick, the button and the HUD, with all references wired.
- A scripted rally: injected move and action input wins a point for the player.
- Route: the Map's Volleyball card → `MG_Volleyball` → complete → back to Map.

**Visual:** screenshot checks with the `testing-unity-ui-with-screenshots` skill at
phone landscape size (court, athletes, HUD, joystick and button placement).

## Out of scope

2v2 play, difficulty settings, audio beyond the existing shared SFX, the
`beachbkgIso` variant, and new character art.
