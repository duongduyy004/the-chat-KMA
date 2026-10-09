# Volleyball Jump Smash and Jump Block Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the volleyball player a JUMP button: a smash only happens mid-air and flies where the joystick aims (so the player can place it away from the NPC), and jumping at the net is how the player blocks.

**Architecture:** The rules stay in the plain-C# model (`VolleyAthlete`, `VolleyballMatch`, `ActionResolver`), driven by `Tick` and unit-tested in EditMode. `VolleyAthlete` gains an airborne timer; `VolleyballMatch` gains `PressJump`, `SmashAim`, `PlayerAim`, `TryGetJumpCue` and a `PlayerBlocked` event; `ActionResolver` only smashes mid-air and no longer blocks from the hit button. Presentation (`JumpButton`, input bridge, views, HUD, scene configurator) only reads the model.

**Tech Stack:** Unity 6000.3.23f1, C#, NUnit (Unity Test Framework, EditMode + PlayMode), uGUI + TextMeshPro, Input System.

**Spec:** `docs/superpowers/specs/2026-10-10-volleyball-jump-aim-design.md`

## Global Constraints

- Jump lasts `JumpSeconds = 0.7` s; visual peak `JumpPeak = 0.8` court units; the rules never read the jump height.
- Mid-air the athlete cannot move; the joystick aims.
- `SmashAim(stick)`: `x = 5.25 + stick.x * 2.25` clamped to `[3, 7.5]`, `y = stick.y * 5` clamped to `[-3.5, 3.5]` (so a diagonal stick already reaches the sideline); stick is clamped to length 1 first. Stick (0, 0) aims at (5.25, 0).
- `AimAtOpponent` stays unchanged and is used only for the PERFECT serve.
- Smash needs: airborne + touches 1–2 + `|x| ≤ SmashNetDistance` + apex > `SmashContactHeight` + existing timing/height/reach checks. Grounded presses never smash. Airborne presses that cannot smash return `ActionKind.None`.
- The hit button never blocks. A jump while `OpponentSmashTell` and `|x| ≤ BlockNetDistance` takes the Block pose and raises `PlayerActed(Block)`. The block succeeds when the player is airborne and lined up (`|y − netCrossY| ≤ BlockLateral`) at the NPC's contact.
- `JumpCueLead = 0.5` s.
- Keyboard: Space = ĐÁNH, J = NHẢY.
- Vietnamese UI text exactly: button `NHẢY`; hint `Joystick: di chuyển  ·  NHẢY rồi kéo joystick để nhắm  ·  ĐÁNH để đập`; practice hint `ĐỠ → CHUYỀN → NHẢY ĐẬP · {n}/{N} ĐIỂM`; block feedback `CHẮN!`.
- Unchanged: serve, receive, set, dive, free ball, `OpponentPlan`, NPC speed/reaction, scoring, `BuildResult`.
- Commit straight to `master`. **No `Co-Authored-By` trailer** in commit messages.
- The Unity Editor must be closed while `tools/run-unity-tests.sh` runs (batch mode cannot open an already-open project).

## Spec deviations (decided while planning, reflected back into the spec)

- The resolver reads `context.Athlete.IsAirborne`; `ActionContext` gets no new field, because the athlete already travels in the context.
- `SmashAim`'s short edge is x = 3 (not 2.5): x = 3 is the shortest smash target the current code proves clears the 2.24 m net (`AimAtOpponent`'s short aim).
- `SmashAim`'s lateral factor is 5 (not 4), still clamped to ±3.5. With 4, even the shortest, widest aim moved the net crossing only ~0.8 m, less than the 1 m block width, so aiming could never beat an AI block.
- `TryGetPlayerContactCue` is unchanged. It already points at the smash contact point near the net and falls back to the receive point after the smash window.
- The "NHẢY!" cue is the pulsing JUMP button only, with no text on the world-space contact ring. That would need a world-space TMP label, and the button pulse carries the same signal.
- "CHẮN!" is driven by a new `VolleyballMatch.PlayerBlocked` event raised after the block point is awarded, so it replaces the generic "GHI ĐIỂM!" text.

## Review Focus

1. **Jumping out of reach loses the ball.** A mid-air press can only smash; a player who jumps while the contact ring is still yellow cannot receive, and may land too late. Expect: the bot in `AssistedRallyTests` jumps only when in reach and still keeps every ball up (Task 4, Step 1).
2. **Holding the stick while jumping must not drift the athlete.** `VolleyballMatch.Tick` calls `Player.Move(move)` every frame. Expect: position fixed for the whole jump (Task 1, Step 1, `AirborneAthleteCannotMoveOrJumpAgain`).
3. **Jump presses outside a live rally** (held ball, toss, dead ball, finished match) must do nothing. Otherwise the athlete freezes before a serve. Expect: `PressJump` returns false (Task 2, Step 1, `JumpOnlyWorksWhileTheBallIsInPlay`).
4. **Jump and hit in the same frame** (two thumbs) must resolve as jump-then-hit, i.e. a smash. Expect: the controller drains jumps before presses (Task 3, Step 5). This is pinned by `JourneyRuntimeDriver`, which feeds jump + hit in one frame for every smash (Task 3, Step 6; it becomes load-bearing in Task 4, Step 6).
5. **The two thumbs must not hit each other's button.** The JUMP hit area must not overlap the ĐÁNH hit area. Expect: disjoint rects (Task 7, Step 1, `JumpButtonAndPlayerAimMarkerAreWired`).

---

## File Structure

| File | Responsibility |
|---|---|
| `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyAthlete.cs` | Airborne timer: `TryJump`, `IsAirborne`, `AirTimeLeft`, `JumpHeight`; no movement mid-air |
| `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.cs` | `PressJump`, `SmashAim`, `PlayerAim`, `TryGetJumpCue`, `PlayerBlocked`; smash target uses `SmashAim` |
| `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.Opponent.cs` | Player block = airborne + lined; NPC block prediction uses `SmashAim`; raise `PlayerBlocked` |
| `Assets/_Project/Scripts/Gameplay/Volleyball/ActionResolver.cs` | Smash only mid-air; no block from the hit button |
| `Assets/_Project/Scripts/Gameplay/Volleyball/JumpButton.cs` (new) | NHẢY hit area: `Pressed` event, cue pulse |
| `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballInputBridge.cs` | Jump button + J key → `ConsumeJumps()` |
| `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs` | Drain jumps before presses; cue pulse; "CHẮN!" |
| `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballChallengeRules.cs` | `PressJump()` pass-through |
| `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyAthleteView.cs` | Lift sprite by `JumpHeight` |
| `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyBallView.cs` | Player aim ring |
| `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballHud.cs` | New hint texts, `ShowBlock()` |
| `Assets/Editor/VolleyballSceneConfigurator.cs` | Build NHẢY button + player aim ring, wire them |
| `Assets/_Project/Scenes/MG_Volleyball.unity` | Regenerated by the configurator |
| Tests: `Assets/Tests/EditMode/Gameplay/Volleyball/{VolleyAthleteTests,VolleyballMatchTests,ActionResolverTests,OpponentAiTests,AssistedRallyTests,VolleyballChallengeTests,PresentationTests,MatchDriver}.cs`, `Assets/Tests/EditMode/EditorTools/VolleyballSceneConfiguratorTests.cs`, `Assets/Tests/PlayMode/Gameplay/Volleyball/{VolleyballInputBridgeTests,VolleyballControllerTests}.cs`, `Assets/Tests/PlayMode/Progression/JourneyRuntimeDriver.cs` | |

---

### Task 0: Preflight

**Files:** none

- [ ] **Step 1: Make sure the user's uncommitted scene work is safe**

Run: `git status --short`

When this plan was written, `Assets/_Project/Scenes/MG_Volleyball.unity` (and other scenes/prefabs) had uncommitted user changes. `VolleyballSceneConfiguratorTests` calls `VolleyballSceneConfigurator.BuildScene()`, which **overwrites `MG_Volleyball.unity`**, so even the baseline test run would destroy that work. If `MG_Volleyball.unity` shows ` M`, **stop and ask the user** to commit or stash it. Never commit, stash or revert the user's files yourself.

- [ ] **Step 2: Baseline the volleyball suites (Unity Editor closed)**

Run:
```bash
tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" baseline-volley-edit
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.VolleyballSceneConfiguratorTests" baseline-volley-config
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Volleyball" baseline-volley-play
tools/run-unity-tests.sh PlayMode "KMA.Tests.Progression" baseline-progression-play
```
Record the pass/fail counts from each `Builds/TestResults/*.xml`. Later tasks must not add failures beyond this baseline. Afterwards, `git checkout -- Assets/_Project/Scenes/MG_Volleyball.unity` **only if Step 1 confirmed the file was clean before the run** (the configurator test rewrites it).

---

### Task 1: Airborne athlete

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyAthlete.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/VolleyAthleteTests.cs`

**Interfaces:**
- Produces: `VolleyAthlete.JumpSeconds` (`const float .7f`), `VolleyAthlete.JumpPeak` (`const float .8f`), `float AirTimeLeft { get; }`, `bool IsAirborne { get; }`, `float JumpHeight { get; }`, `bool TryJump(AthleteAction pose)`.

- [ ] **Step 1: Write the failing tests**

Append inside `VolleyAthleteTests`:

```csharp
        [Test]
        public void JumpStaysAirborneForJumpSecondsThenLands()
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-2f, 0f));
            Assert.That(athlete.TryJump(AthleteAction.Smash), Is.True);
            Assert.That(athlete.IsAirborne, Is.True);
            Assert.That(athlete.Action, Is.EqualTo(AthleteAction.Smash));
            Assert.That(athlete.JumpHeight, Is.EqualTo(0f).Within(1e-4f));

            athlete.Tick(VolleyAthlete.JumpSeconds / 2f);
            Assert.That(athlete.JumpHeight, Is.EqualTo(VolleyAthlete.JumpPeak).Within(1e-3f));

            athlete.Tick(VolleyAthlete.JumpSeconds / 2f + .01f);
            Assert.That(athlete.IsAirborne, Is.False);
            Assert.That(athlete.JumpHeight, Is.Zero);
            Assert.That(athlete.Action, Is.EqualTo(AthleteAction.Idle));
        }

        [Test]
        public void AirborneAthleteCannotMoveOrJumpAgain()
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-2f, 0f));
            athlete.TryJump(AthleteAction.Smash);

            athlete.Move(Vector2.up, .2f);
            athlete.MoveToward(new Vector2(-5f, 3f), .2f);
            Assert.That(athlete.Position, Is.EqualTo(new Vector2(-2f, 0f)));
            Assert.That(athlete.Action, Is.EqualTo(AthleteAction.Smash), "holding the stick must not cancel the pose");
            Assert.That(athlete.TryJump(AthleteAction.Smash), Is.False);
        }

        [Test]
        public void LockedAthleteCannotJump()
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-2f, 0f));
            athlete.BeginAction(AthleteAction.Receive, .35f);
            Assert.That(athlete.TryJump(AthleteAction.Smash), Is.False);
            Assert.That(athlete.IsAirborne, Is.False);
        }

        [Test]
        public void PlaceAtLandsAJumpingAthlete()
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-2f, 0f));
            athlete.TryJump(AthleteAction.Block);
            athlete.PlaceAt(new Vector2(-5f, 0f));
            Assert.That(athlete.IsAirborne, Is.False);
            Assert.That(athlete.Action, Is.EqualTo(AthleteAction.Idle));
        }

        [Test]
        public void ASmashLockEndingMidAirKeepsThePoseUntilLanding()
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-2f, 0f));
            athlete.TryJump(AthleteAction.Smash);
            athlete.BeginAction(AthleteAction.Smash, .2f);
            athlete.Tick(.3f);
            Assert.That(athlete.IsAirborne, Is.True);
            Assert.That(athlete.Action, Is.EqualTo(AthleteAction.Smash));
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.VolleyAthleteTests" t1-athlete`
Expected: compile error / FAIL: `VolleyAthlete` has no `TryJump`.

- [ ] **Step 3: Implement**

In `VolleyAthlete.cs`, add the constants and properties under `SideMargin` / `IsLocked`, and replace `PlaceAt`, `Move` and `Tick`:

```csharp
        public const float JumpSeconds = .7f;
        public const float JumpPeak = .8f;
```

```csharp
        public float AirTimeLeft { get; private set; }
        public bool IsAirborne => AirTimeLeft > 0f;

        // Visual only: the rules never read how high the athlete is.
        public float JumpHeight => IsAirborne
            ? JumpPeak * Mathf.Sin(Mathf.PI * (1f - AirTimeLeft / JumpSeconds))
            : 0f;

        public void PlaceAt(Vector2 position)
        {
            Position = Clamp(position);
            Action = AthleteAction.Idle;
            LockTimeLeft = 0f;
            AirTimeLeft = 0f;
        }

        // Mid-air the athlete holds its spot and pose; the stick aims instead of steering.
        public bool TryJump(AthleteAction pose)
        {
            if (IsAirborne || IsLocked)
                return false;

            AirTimeLeft = JumpSeconds;
            Action = pose;
            return true;
        }

        public void Move(Vector2 input, float deltaTime)
        {
            if (IsLocked || IsAirborne)
                return;
            // ... rest of the existing body unchanged
        }

        public void Tick(float deltaTime)
        {
            if (AirTimeLeft > 0f)
            {
                AirTimeLeft = Mathf.Max(0f, AirTimeLeft - deltaTime);
                if (AirTimeLeft <= 0f && LockTimeLeft <= 0f)
                    Action = AthleteAction.Idle;
            }

            if (LockTimeLeft <= 0f)
                return;

            LockTimeLeft = Mathf.Max(0f, LockTimeLeft - deltaTime);
            if (LockTimeLeft <= 0f && !IsAirborne)
                Action = AthleteAction.Idle;
        }
```

`MoveToward` needs no change: it calls `Move`, which returns early while airborne.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.VolleyAthleteTests" t1-athlete`
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/VolleyAthlete.cs Assets/Tests/EditMode/Gameplay/Volleyball/VolleyAthleteTests.cs
git commit -m "feat(volleyball): let an athlete jump and hold still mid-air"
```

---

### Task 2: Match jump, aim and jump cue (additive)

Nothing changes how a press resolves yet, so every existing test stays green.

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/VolleyballMatchTests.cs`

**Interfaces:**
- Consumes: `VolleyAthlete.TryJump`, `IsAirborne` (Task 1).
- Produces: `public const float JumpCueLead = .5f`, `public const float SmashAimMargin = .5f`, `public static Vector2 SmashAim(Vector2 stick)`, `public Vector2 PlayerAim { get; }`, `public bool PressJump()`, `public bool TryGetJumpCue(out float secondsToIdeal)`.

- [ ] **Step 1: Write the failing tests**

Append inside `VolleyballMatchTests`. Also add a shared helper at the top of the class next to `FrozenOpponentMatch`:

```csharp
        // Opponent serve received perfectly; returns with the set in the air and the player
        // standing on the smash contact point.
        static VolleyballMatch ReceivedAndUnderTheSet(out float smashIdeal)
        {
            var match = FrozenOpponentMatch();
            match.ForceServerForTest(CourtSide.Opponent);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 3f), Is.True);
            float receiveIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(receiveIdeal));
            MatchDriver.AdvanceToFlightTime(match, receiveIdeal);
            Assert.That(match.PressAction().Kind, Is.EqualTo(ActionKind.Receive));
            smashIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(smashIdeal));
            return match;
        }
```

```csharp
        [Test]
        public void SmashAimMapsTheStickContinuouslyInsideTheOpponentCourt()
        {
            Assert.That(VolleyballMatch.SmashAim(Vector2.zero), Is.EqualTo(new Vector2(5.25f, 0f)));
            Assert.That(VolleyballMatch.SmashAim(Vector2.right), Is.EqualTo(new Vector2(7.5f, 0f)));
            Assert.That(VolleyballMatch.SmashAim(Vector2.left), Is.EqualTo(new Vector2(3f, 0f)));
            Assert.That(VolleyballMatch.SmashAim(Vector2.up), Is.EqualTo(new Vector2(5.25f, 3.5f)));
            Assert.That(VolleyballMatch.SmashAim(Vector2.down), Is.EqualTo(new Vector2(5.25f, -3.5f)));
            Assert.That(VolleyballMatch.SmashAim(new Vector2(0f, .5f)).y, Is.EqualTo(2.5f).Within(1e-4f));
            Assert.That(VolleyballMatch.SmashAim(new Vector2(5f, 0f)), Is.EqualTo(new Vector2(7.5f, 0f)), "stick is clamped to 1");

            Vector2 diagonal = VolleyballMatch.SmashAim(new Vector2(1f, 1f));
            Assert.That(diagonal.x, Is.EqualTo(5.25f + 2.25f * .70710678f).Within(1e-4f));
            Assert.That(diagonal.y, Is.EqualTo(3.5f).Within(1e-4f), "a diagonal reaches the sideline");
            Assert.That(CourtSpace.IsIn(diagonal), Is.True);
        }

        [Test]
        public void PlayerAimFollowsTheStick()
        {
            var match = FrozenOpponentMatch();
            match.SetMove(Vector2.up);
            Assert.That(match.PlayerAim, Is.EqualTo(VolleyballMatch.SmashAim(Vector2.up)));
        }

        [Test]
        public void JumpOnlyWorksWhileTheBallIsInPlay()
        {
            var match = FrozenOpponentMatch();
            Assert.That(match.BallState, Is.EqualTo(BallState.Held));
            Assert.That(match.PressJump(), Is.False);
            match.PressAction();
            Assert.That(match.BallState, Is.EqualTo(BallState.Toss));
            Assert.That(match.PressJump(), Is.False);
            Assert.That(match.Player.IsAirborne, Is.False);

            VolleyballMatch rally = ReceivedAndUnderTheSet(out _);
            Assert.That(rally.PressJump(), Is.True);
            Assert.That(rally.Player.IsAirborne, Is.True);
            Assert.That(rally.Player.Action, Is.EqualTo(AthleteAction.Smash));
            Assert.That(rally.PressJump(), Is.False, "no double jump");

            rally.SetScoreForTest(0, VolleyballMatch.PointsToWin - 1);
            rally.Player.PlaceAt(new Vector2(-8f, 0f));
            Assert.That(MatchDriver.AdvanceUntil(rally, () => rally.IsOver, 10f), Is.True);
            Assert.That(rally.PressJump(), Is.False);
        }

        [Test]
        public void JumpCueOpensHalfASecondBeforeTheSmashAndClosesOnceAirborne()
        {
            VolleyballMatch match = ReceivedAndUnderTheSet(out float smashIdeal);
            MatchDriver.AdvanceToFlightTime(match, smashIdeal - .6f);
            Assert.That(match.TryGetJumpCue(out _), Is.False, "too early");

            MatchDriver.AdvanceToFlightTime(match, smashIdeal - .4f);
            Assert.That(match.TryGetJumpCue(out float seconds), Is.True);
            Assert.That(seconds, Is.EqualTo(.4f).Within(1e-3f));

            match.PressJump();
            Assert.That(match.TryGetJumpCue(out _), Is.False);
        }

        [Test]
        public void NoJumpCueAwayFromTheNet()
        {
            VolleyballMatch match = ReceivedAndUnderTheSet(out float smashIdeal);
            match.Player.PlaceAt(new Vector2(-5f, 0f));
            MatchDriver.AdvanceToFlightTime(match, smashIdeal - .3f);
            Assert.That(match.TryGetJumpCue(out _), Is.False);
        }

        [Test]
        public void JumpCueAndBlockPoseWhenTheOpponentTelegraphsASmashAtTheNet()
        {
            var match = new VolleyballMatch();
            MatchDriver.ServeGood(match);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.OpponentSmashTell, 6f), Is.True);

            match.Player.PlaceAt(new Vector2(-3f, 0f));
            Assert.That(match.TryGetJumpCue(out _), Is.False, "too far from the net to block");

            match.Player.PlaceAt(new Vector2(-.8f, 0f));
            Assert.That(match.TryGetJumpCue(out _), Is.True);

            ActionDecision acted = ActionDecision.None;
            match.PlayerActed += d => acted = d;
            Assert.That(match.PressJump(), Is.True);
            Assert.That(match.Player.Action, Is.EqualTo(AthleteAction.Block));
            Assert.That(acted.Kind, Is.EqualTo(ActionKind.Block));
            Assert.That(acted.IsTimed, Is.False, "a block jump is not graded");
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.VolleyballMatchTests" t2-match`
Expected: compile error: `SmashAim`, `PlayerAim`, `PressJump`, `TryGetJumpCue` not defined.

- [ ] **Step 3: Implement**

In `VolleyballMatch.cs`, add constants next to `SmashRise`:

```csharp
        public const float JumpCueLead = .5f;
        public const float SmashAimMargin = .5f;
```

Add after `AimAtOpponent`:

```csharp
        // Where a jump smash lands: continuous, always inside the opponent's court. Full left is a
        // short drop at x 3 (the shortest smash proven to clear the net), full right the deep line;
        // a diagonal already reaches the sideline, so a short wide smash can swing past a block.
        public static Vector2 SmashAim(Vector2 stick)
        {
            stick = Vector2.ClampMagnitude(stick, 1f);
            float deep = CourtSpace.HalfLength - SmashAimMargin;
            float wide = CourtSpace.HalfWidth - SmashAimMargin;
            return new Vector2(Mathf.Clamp(5.25f + stick.x * 2.25f, 3f, deep),
                Mathf.Clamp(stick.y * 5f, -wide, wide));
        }

        public Vector2 PlayerAim => SmashAim(move);
```

Add after `PressAction`:

```csharp
        // A jump at the net while the opponent telegraphs a smash is a block; anywhere else it is
        // the take-off for a smash. Either way the athlete hangs for JumpSeconds and the stick aims.
        public bool PressJump()
        {
            if (IsOver || BallState != BallState.InPlay)
                return false;

            bool blocking = OpponentSmashTell && Mathf.Abs(Player.Position.x) <= ActionResolver.BlockNetDistance;
            if (!Player.TryJump(blocking ? AthleteAction.Block : AthleteAction.Smash))
                return false;

            if (blocking)
            {
                var decision = new ActionDecision(ActionKind.Block, TimingGrade.Miss, 0f);
                LastDecision = decision;
                PlayerActed?.Invoke(decision);
            }

            return true;
        }
```

Add after `TryGetPlayerContactCue(out float, out Vector2)`:

```csharp
        // True while a jump now would pay off: a smash coming up near the net, or an opponent
        // smash telegraphed while the player stands at the net.
        public bool TryGetJumpCue(out float secondsToIdeal)
        {
            secondsToIdeal = 0f;
            if (IsOver || BallState != BallState.InPlay || Flight == null || Player.IsAirborne)
                return false;

            if (Rally.Possession != CourtSide.Player)
                return OpponentSmashTell && Mathf.Abs(Player.Position.x) <= ActionResolver.BlockNetDistance;

            if (Rally.Touches < 1 || Rally.Touches > 2 ||
                Mathf.Abs(Player.Position.x) > ActionResolver.SmashNetDistance ||
                Flight.ApexHeight <= ActionResolver.SmashContactHeight)
                return false;

            secondsToIdeal = Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight) - FlightTime;
            return secondsToIdeal <= JumpCueLead && secondsToIdeal >= -TimingWindows.SmashLate;
        }
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" t2-volley`
Expected: all PASS (new and existing).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.cs Assets/Tests/EditMode/Gameplay/Volleyball/VolleyballMatchTests.cs
git commit -m "feat(volleyball): add match jump, smash aim and jump cue"
```

---

### Task 3: Jump input (button, J key, controller)

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/JumpButton.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballInputBridge.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs:110-125` (`TickPlay`)
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballChallengeRules.cs`
- Modify: `Assets/Tests/PlayMode/Progression/JourneyRuntimeDriver.cs:100-118`
- Test: `Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballInputBridgeTests.cs`, `Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballControllerTests.cs`

**Interfaces:**
- Consumes: `VolleyballMatch.PressJump()` (Task 2).
- Produces: `JumpButton : MonoBehaviour, IPointerDownHandler` with `event Action Pressed`, `void Press()`, `bool Cued { get; }`, `void SetCue(bool cued)`, `const float PulseAmount = .12f`; `VolleyballInputBridge.Configure(VirtualJoystick, ActionButton, JumpButton jump = null)`, `JumpButton JumpButton { get; }`, `int ConsumeJumps()`, `void FeedJumpForTest()`; `ClearPresses()` also clears jumps; `VolleyballChallengeRules.PressJump()` returns `bool`.

- [ ] **Step 1: Write the failing tests**

In `VolleyballInputBridgeTests.cs`, change `CreateBridge` to also build a jump button, and add a test:

```csharp
        VolleyballInputBridge CreateBridge(out VirtualJoystick joystick, out ActionButton button) =>
            CreateBridge(out joystick, out button, out _);

        VolleyballInputBridge CreateBridge(out VirtualJoystick joystick, out ActionButton button, out JumpButton jump)
        {
            root = new GameObject("Bridge", typeof(RectTransform));
            root.SetActive(false);
            joystick = root.AddComponent<VirtualJoystick>();
            var stickBase = new GameObject("Base", typeof(RectTransform)).transform as RectTransform;
            var knob = new GameObject("Knob", typeof(RectTransform)).transform as RectTransform;
            stickBase.SetParent(root.transform, false);
            knob.SetParent(root.transform, false);
            joystick.Configure((RectTransform)root.transform, stickBase, knob, 100f, Vector2.zero);
            button = root.AddComponent<ActionButton>();
            jump = new GameObject("Jump", typeof(RectTransform)).AddComponent<JumpButton>();
            jump.transform.SetParent(root.transform, false);
            var bridge = root.AddComponent<VolleyballInputBridge>();
            bridge.Configure(joystick, button, jump);
            root.SetActive(true);
            return bridge;
        }

        [UnityTest]
        public IEnumerator JumpPressesQueueSeparatelyFromHits()
        {
            VolleyballInputBridge bridge = CreateBridge(out _, out ActionButton button, out JumpButton jump);
            yield return null;

            Assert.That(bridge.JumpButton, Is.SameAs(jump));
            jump.Press();
            button.Press();
            Assert.That(bridge.ConsumeJumps(), Is.EqualTo(1));
            Assert.That(bridge.ConsumeJumps(), Is.Zero);
            Assert.That(bridge.ConsumePresses(), Is.EqualTo(1));

            jump.Press();
            bridge.FeedJumpForTest();
            bridge.ClearPresses();
            Assert.That(bridge.ConsumeJumps(), Is.Zero, "pausing drops queued jumps too");
        }

        [UnityTest]
        public IEnumerator JumpCuePulsesTheButtonAndRestsAtScaleOne()
        {
            CreateBridge(out _, out _, out JumpButton jump);
            yield return null;

            jump.SetCue(true);
            Assert.That(jump.Cued, Is.True);
            Assert.That(jump.transform.localScale.x, Is.InRange(1f, 1f + JumpButton.PulseAmount + 1e-4f));
            jump.SetCue(false);
            Assert.That(jump.transform.localScale, Is.EqualTo(Vector3.one));
        }
```

In `VolleyballControllerTests.cs`, add (reusing the file's `View` helper and the `root` field):

```csharp
        [UnityTest]
        public IEnumerator JumpPressesReachTheMatch()
        {
            root = new GameObject("Controller");
            root.SetActive(false);
            var input = root.AddComponent<VolleyballInputBridge>();
            var controller = root.AddComponent<VolleyballController>();
            VolleyAthleteView player = View("Player"), opponent = View("Opponent");
            var ballView = new GameObject("BallView").AddComponent<VolleyBallView>();
            ballView.transform.SetParent(root.transform);
            var hud = root.AddComponent<VolleyballHud>();
            controller.Configure(player, opponent, ballView, input, hud);
            root.SetActive(true);
            yield return null;

            controller.SkipToPlayForTest();
            controller.Match.ForceServerForTest(CourtSide.Opponent);
            while (controller.Match.BallState != BallState.InPlay)
                controller.Match.Tick(1f / 60f);

            input.FeedJumpForTest();
            yield return null;
            Assert.That(controller.Match.Player.IsAirborne, Is.True);
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Volleyball" t3-input`
Expected: compile error: `JumpButton` not defined.

- [ ] **Step 3: Implement `JumpButton`**

Create `Assets/_Project/Scripts/Gameplay/Volleyball/JumpButton.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KMA.Gameplay.Volleyball
{
    /// The NHẢY hit area. KitPressFeedback scales the round face on press; the cue pulse scales
    /// this hit area itself, so the two never fight over one transform.
    public sealed class JumpButton : MonoBehaviour, IPointerDownHandler
    {
        public const float PulseAmount = .12f;
        const float PulseRate = 10f;

        public event Action Pressed;
        public bool Cued { get; private set; }

        // Fires on touch-down, not release, like ĐÁNH: the jump window is only a few frames wide.
        public void OnPointerDown(PointerEventData eventData) => Press();

        public void Press() => Pressed?.Invoke();

        public void SetCue(bool cued)
        {
            Cued = cued;
            float pulse = cued ? PulseAmount * Mathf.Abs(Mathf.Sin(Time.unscaledTime * PulseRate)) : 0f;
            transform.localScale = Vector3.one * (1f + pulse);
        }
    }
}
```

- [ ] **Step 4: Implement the bridge**

Replace `VolleyballInputBridge.cs` with:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace KMA.Gameplay.Volleyball
{
    // Merges the touch controls with a keyboard fallback (WASD/arrows, Space to hit, J to jump).
    // The keyboard actions live in code because KMA.inputactions' map list is pinned by
    // InputAssetContractTests.
    public sealed class VolleyballInputBridge : MonoBehaviour
    {
        [SerializeField] VirtualJoystick joystick;
        [SerializeField] ActionButton actionButton;
        [SerializeField] JumpButton jumpButton;

        InputAction moveAction;
        InputAction pressAction;
        InputAction jumpAction;
        ActionButton subscribedButton;
        JumpButton subscribedJump;
        int pendingPresses;
        int pendingJumps;
        Vector2? testMove;

        public void Configure(VirtualJoystick stick, ActionButton button, JumpButton jump = null)
        {
            joystick = stick;
            actionButton = button;
            jumpButton = jump;
            if (isActiveAndEnabled)
                SubscribeButtons();
        }

        public JumpButton JumpButton => jumpButton;

        public Vector2 Move
        {
            get
            {
                if (testMove.HasValue)
                    return testMove.Value;
                if (joystick && joystick.Value.sqrMagnitude > .0001f)
                    return joystick.Value;
                return moveAction == null ? Vector2.zero : Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            }
        }

        public int ConsumePresses()
        {
            int presses = pendingPresses;
            pendingPresses = 0;
            return presses;
        }

        public int ConsumeJumps()
        {
            int jumps = pendingJumps;
            pendingJumps = 0;
            return jumps;
        }

        public void ClearPresses()
        {
            pendingPresses = 0;
            pendingJumps = 0;
        }

        public void FeedMoveForTest(Vector2 move) => testMove = move;

        public void FeedActionForTest() => pendingPresses++;

        public void FeedJumpForTest() => pendingJumps++;

        void OnEnable()
        {
            EnsureActions();
            moveAction.Enable();
            pressAction.Enable();
            jumpAction.Enable();
            SubscribeButtons();
        }

        void OnDisable()
        {
            moveAction?.Disable();
            pressAction?.Disable();
            jumpAction?.Disable();
            UnsubscribeButtons();
        }

        void OnDestroy()
        {
            moveAction?.Dispose();
            pressAction?.Dispose();
            jumpAction?.Dispose();
        }

        void EnsureActions()
        {
            if (moveAction != null)
                return;

            moveAction = new InputAction("VolleyballMove", InputActionType.Value, expectedControlType: "Vector2");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            pressAction = new InputAction("VolleyballAction", InputActionType.Button, "<Keyboard>/space");
            pressAction.performed += _ => OnPressed();
            jumpAction = new InputAction("VolleyballJump", InputActionType.Button, "<Keyboard>/j");
            jumpAction.performed += _ => OnJumped();
        }

        void SubscribeButtons()
        {
            if (subscribedButton != actionButton)
            {
                if (subscribedButton)
                    subscribedButton.Pressed -= OnPressed;
                subscribedButton = actionButton;
                if (actionButton)
                    actionButton.Pressed += OnPressed;
            }

            if (subscribedJump != jumpButton)
            {
                if (subscribedJump)
                    subscribedJump.Pressed -= OnJumped;
                subscribedJump = jumpButton;
                if (jumpButton)
                    jumpButton.Pressed += OnJumped;
            }
        }

        void UnsubscribeButtons()
        {
            if (subscribedButton)
                subscribedButton.Pressed -= OnPressed;
            if (subscribedJump)
                subscribedJump.Pressed -= OnJumped;
            subscribedButton = null;
            subscribedJump = null;
        }

        void OnPressed() => pendingPresses++;

        void OnJumped() => pendingJumps++;
    }
}
```

- [ ] **Step 5: Implement the controller and challenge pass-through**

In `VolleyballChallengeRules.cs`, after `PressAction`:

```csharp
        public bool PressJump() => Match.PressJump();
```

In `VolleyballController.TickPlay`, insert the jump loop **before** the press loop (a jump and a hit in the same frame must resolve as a jump smash):

```csharp
            if (challengeRules != null) challengeRules.SetMove(input.Move);
            else Match.SetMove(input.Move);
            // Jumps first: two thumbs landing in one frame mean "take off, then hit".
            for (int jumps = input.ConsumeJumps(); jumps > 0; jumps--)
            {
                if (challengeRules != null) challengeRules.PressJump();
                else Match.PressJump();
            }
            for (int presses = input.ConsumePresses(); presses > 0; presses--)
```

- [ ] **Step 6: Make the journey bot jump before its smash**

In `Assets/Tests/PlayMode/Progression/JourneyRuntimeDriver.cs`, inside `if (smash) { ... }`, feed a jump before the hit. The controller drains jumps first, so the jump is a no-op until Task 4 makes it required:

```csharp
                            if (smash)
                            {
                                match.Opponent.PlaceAt(new Vector2(8.5f, -3.5f));
                                volley.Input.FeedMoveForTest(new Vector2(1f, 1f));
                                volley.Input.FeedJumpForTest();
                            }
                            volley.Input.FeedActionForTest();
```

- [ ] **Step 7: Run the tests to verify they pass**

Run:
```bash
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Volleyball" t3-input
tools/run-unity-tests.sh PlayMode "KMA.Tests.Progression" t3-progression
```
Expected: all PASS; progression matches the Task 0 baseline.

- [ ] **Step 8: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/JumpButton.cs Assets/_Project/Scripts/Gameplay/Volleyball/JumpButton.cs.meta Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballInputBridge.cs Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballChallengeRules.cs Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballInputBridgeTests.cs Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballControllerTests.cs Assets/Tests/PlayMode/Progression/JourneyRuntimeDriver.cs
git commit -m "feat(volleyball): route a jump button and the J key to the match"
```

(If Unity did not generate `JumpButton.cs.meta` because batch mode ran without an import, open the project once or run any test so that Unity imports it, then add the `.meta`.)

---

### Task 4: Smash only mid-air, toward the aim

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/ActionResolver.cs:115-145` (`ResolveTouch`)
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.cs` (`PlayerHit` smash branch)
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.Opponent.cs` (`PositionOpponentWhileDefending`)
- Modify: `Assets/Tests/EditMode/Gameplay/Volleyball/MatchDriver.cs`
- Test: `ActionResolverTests.cs`, `VolleyballMatchTests.cs`, `OpponentAiTests.cs`, `AssistedRallyTests.cs`, `VolleyballChallengeTests.cs`

**Interfaces:**
- Consumes: `VolleyAthlete.IsAirborne`, `TryJump` (Task 1); `VolleyballMatch.PressJump`, `SmashAim`, `TryGetJumpCue`, `PlayerInReachOf` (Task 2 / existing).
- Produces: `MatchDriver.JumpSmash(VolleyballMatch match, Vector2 stick)` returning `ActionDecision`.

- [ ] **Step 1: Write and update the tests**

Add to `MatchDriver.cs`:

```csharp
        // Take off, aim with the stick and hit, all at the current flight time.
        public static ActionDecision JumpSmash(VolleyballMatch match, Vector2 stick)
        {
            match.PressJump();
            match.SetMove(stick);
            ActionDecision decision = match.PressAction();
            match.SetMove(Vector2.zero);
            return decision;
        }
```

**`ActionResolverTests.cs`:** add the helper below `PlayerAt`:

```csharp
        static VolleyAthlete Airborne(VolleyAthlete athlete)
        {
            athlete.TryJump(AthleteAction.Smash);
            return athlete;
        }
```

Then change these tests:
- `SecondTouchNearTheNetSmashes`: rename to `SecondTouchNearTheNetSmashesMidAir`, and pass `Airborne(PlayerAt(set.GroundAt(ideal)))`.
- `AGoodLatePressStillSmashesInsteadOfFallingThroughToReceive`: pass `Airborne(PlayerAt(set.GroundAt(ideal)))`.
- Replace `BlockNeedsTheTellTheNetAndTheLine` with:

```csharp
        [Test]
        public void WithoutPossessionTheHitButtonDoesNothingEvenOnTheBlockLine()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            rally.RegisterServe(CourtSide.Player);
            var attack = new BallFlight(new Vector2(4f, 0f), 1f, new Vector2(1.5f, 0f), 4f, CourtSide.Opponent);
            var aim = new Vector2(-7f, -3f);
            float netCrossY = ActionResolver.NetCrossY(attack.GroundAt(.5f), aim);
            Assert.That(Resolve(PlayerAt(new Vector2(-.8f, netCrossY)), BallState.InPlay, attack, .5f, rally,
                CourtSide.Player, true, aim).Kind, Is.EqualTo(ActionKind.None));
        }
```

Add new tests:

```csharp
        [Test]
        public void OnTheGroundAHighSetNearTheNetIsOnlyAReceive()
        {
            var set = new BallFlight(new Vector2(-5f, 0f), 1f, new Vector2(-1.5f, 0f), 4f, CourtSide.Player);
            float smashIdeal = set.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            Assert.That(Resolve(PlayerAt(set.GroundAt(smashIdeal)), BallState.InPlay, set, smashIdeal,
                PlayerPossession(1)).Kind, Is.Not.EqualTo(ActionKind.Smash));

            float receiveIdeal = set.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            Assert.That(Resolve(PlayerAt(set.GroundAt(receiveIdeal)), BallState.InPlay, set, receiveIdeal,
                PlayerPossession(1)).Kind, Is.EqualTo(ActionKind.Receive));
        }

        [Test]
        public void MidAirWithNothingToSmashTheButtonDoesNothing()
        {
            BallFlight serve = OpponentServe();
            float ideal = serve.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            Assert.That(Resolve(Airborne(PlayerAt(serve.GroundAt(ideal))), BallState.InPlay, serve, ideal,
                PlayerPossession(0)).Kind, Is.EqualTo(ActionKind.None), "first touch: no receive mid-air");

            var set = new BallFlight(new Vector2(-6f, 0f), 1f, new Vector2(-4.5f, 0f), 4f, CourtSide.Player);
            float smashIdeal = set.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            Assert.That(Resolve(Airborne(PlayerAt(set.GroundAt(smashIdeal))), BallState.InPlay, set, smashIdeal,
                PlayerPossession(1)).Kind, Is.EqualTo(ActionKind.None), "too far from the net to smash");
        }
```

**`VolleyballMatchTests.cs`:** in each of these tests, replace the smash press with `MatchDriver.JumpSmash`:
- `ReceiveThenSmashWinsThePointAsAWinner`: replace the three lines `match.SetMove(Vector2.right); ActionDecision smash = match.PressAction(); match.SetMove(Vector2.zero);` with `ActionDecision smash = MatchDriver.JumpSmash(match, Vector2.right);`, and expect `match.Flight.Target` to equal `new Vector2(7.5f, 0f)`.
- `EarlyLateSmashDumpsTheBallOnTheOwnSide`: replace `ActionDecision smash = match.PressAction();` with `ActionDecision smash = MatchDriver.JumpSmash(match, Vector2.zero);`.
- `LateLatePressStillSmashesAsTheDocumentedSoftLob`: replace the three set-move/press lines with `ActionDecision smash = MatchDriver.JumpSmash(match, Vector2.right);` and expect `match.Flight.Target` to equal `new Vector2(7.5f, 0f)`.
- `OpponentReceivingASmashClearsTheWinnerCreditIfTheAiLosesThePointLater`: replace the three lines with `ActionDecision smash = MatchDriver.JumpSmash(match, Vector2.zero);`.

**`OpponentAiTests.cs`:**
- `OpponentBlocksASmashDownItsLine`: replace `match.SetMove(Vector2.zero); Assert.That(match.PressAction().Kind, ...Smash)` with `Assert.That(MatchDriver.JumpSmash(match, Vector2.zero).Kind, Is.EqualTo(ActionKind.Smash));`. The AI's block prediction now uses `SmashAim(0,0) = (5.25, 0)`, and the AI is still on that line.
- `SmashAimedAwayFromTheBlockGetsThrough`: aim short and wide to the side away from the AI, the way a player reads the block (add `using System.Linq;`):

```csharp
            // The AI lined up on the zero-stick line (no tick has passed since the set), so a
            // short wide smash to its far side must cross the net clear of its 1 m block.
            Vector2 stick = new[] { new Vector2(-.71f, .71f), new Vector2(-.71f, -.71f) }
                .OrderByDescending(s => Mathf.Abs(match.Opponent.Position.y -
                    ActionResolver.NetCrossY(match.BallGround, VolleyballMatch.SmashAim(s))))
                .First();
            Vector2 aim = VolleyballMatch.SmashAim(stick);
            float netCrossY = ActionResolver.NetCrossY(match.BallGround, aim);
            Assert.That(Mathf.Abs(match.Opponent.Position.y - netCrossY), Is.GreaterThan(ActionResolver.BlockLateral));

            Assert.That(MatchDriver.JumpSmash(match, stick).Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(match.BallState, Is.EqualTo(BallState.InPlay));
            Assert.That(match.Flight.Target, Is.EqualTo(aim));
```

If the `GreaterThan(BlockLateral)` precondition fails, aiming cannot beat a block at this contact point. **Do not weaken the assertion.** Report the measured gap to the user: it is a design-tuning question (the `SmashAim` lateral factor or short edge).

**`AssistedRallyTests.cs`:** teach the bot to jump. Replace `FollowTheRing` with:

```csharp
        // Jumps once the button cue lights and the ring is green, aims away from the AI, then
        // hits on the ring's early side like every other touch.
        const float JumpLead = .35f;

        static void FollowTheRing(VolleyballMatch match)
        {
            if (match.BallState != BallState.InPlay || match.FlightTime < ReactionSeconds ||
                !match.TryGetPlayerContactCue(out float secondsToIdeal, out Vector2 contact))
            {
                match.SetMove(Vector2.zero);
                return;
            }

            if (match.Player.IsAirborne)
            {
                match.SetMove(new Vector2(1f, match.Opponent.Position.y >= 0f ? -1f : 1f));
            }
            else
            {
                Vector2 toContact = contact - match.Player.Position;
                match.SetMove(toContact.magnitude > StopShort ? toContact.normalized : Vector2.zero);
                if (match.Rally.Possession == CourtSide.Player && match.TryGetJumpCue(out float toJump) &&
                    toJump <= JumpLead && match.PlayerInReachOf(contact))
                    match.PressJump();
            }

            if (secondsToIdeal <= EarlyPress)
                match.PressAction();
        }
```

**`VolleyballChallengeTests.cs`:** add:

```csharp
        [Test]
        public void PracticeCountsReceiveReceiveJumpSmash()
        {
            var rules = new VolleyballChallengeRules(ChallengeCatalog.LoadDefault().Get("volleyball_practice"));
            VolleyballMatch match = rules.Match;
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 3f), Is.True);
            for (int touch = 0; touch < 2; touch++)
            {
                float ideal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
                match.Player.PlaceAt(match.Flight.GroundAt(ideal));
                MatchDriver.AdvanceToFlightTime(match, ideal);
                Assert.That(rules.PressAction().Kind, Is.EqualTo(ActionKind.Receive), $"touch {touch + 1}");
            }

            float smashIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(smashIdeal));
            MatchDriver.AdvanceToFlightTime(match, smashIdeal);
            Assert.That(rules.PressJump(), Is.True);
            rules.SetMove(Vector2.up);
            Assert.That(rules.PressAction().Kind, Is.EqualTo(ActionKind.Smash));
            rules.SetMove(Vector2.zero);

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.PlayerPoints, Is.EqualTo(1));
            Assert.That(rules.CompletedTargets, Is.EqualTo(1));
        }
```

- [ ] **Step 2: Run the tests to verify the new expectations fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" t4-smash`
Expected FAIL: `OnTheGroundAHighSetNearTheNetIsOnlyAReceive` (ground smash still happens), `MidAirWithNothingToSmashTheButtonDoesNothing`, `WithoutPossessionTheHitButtonDoesNothingEvenOnTheBlockLine`, targets `(7.5, 0)` (still `(7, 0)`), and `PracticeCountsReceiveReceiveJumpSmash` (touch 2 smashes on the ground).

- [ ] **Step 3: Implement the resolver**

In `ActionResolver.cs`, replace `ResolveTouch` and add `ResolveSmash`. Also change the possession line in `Resolve`, and delete `ResolveBlock` (block is now a jump, see Task 5):

```csharp
            if (context.Flight == null || context.Rally == null)
                return ActionDecision.None;
            // Blocking is a jump now, so without possession the hit button has nothing to do.
            if (context.Rally.Possession != athlete.Side)
                return ActionDecision.None;

            return ResolveTouch(context);
```

```csharp
        static ActionDecision ResolveTouch(in ActionContext context)
        {
            VolleyAthlete athlete = context.Athlete;
            // Mid-air the only touch is the smash; on the ground the ball is played low.
            if (athlete.IsAirborne)
                return ResolveSmash(context);

            BallFlight flight = context.Flight;
            int touches = context.Rally.Touches;
            float time = context.FlightTime;
            if (touches > 2)
                return ActionDecision.None;

            float ideal = flight.TimeAtHeightDescending(ReceiveContactHeight);
            float offset = time - ideal;
            TimingGrade grade = TimingWindows.Grade(offset);
            if (grade != TimingGrade.Miss && Vector2.Distance(athlete.Position, flight.GroundAt(ideal)) <= Reach)
                return new ActionDecision(touches == 2 ? ActionKind.FreeBall : ActionKind.Receive, grade, offset);

            float landingDistance = Vector2.Distance(athlete.Position, flight.Target);
            if (flight.IsDescending(time) && flight.HeightAt(time) < ReceiveContactHeight &&
                offset > 0f && landingDistance > DiveMinDistance && landingDistance <= DiveMaxDistance)
                return new ActionDecision(ActionKind.Dive, TimingGrade.Late, offset);

            return ActionDecision.None;
        }

        static ActionDecision ResolveSmash(in ActionContext context)
        {
            VolleyAthlete athlete = context.Athlete;
            BallFlight flight = context.Flight;
            int touches = context.Rally.Touches;
            if (touches < 1 || touches > 2 || Mathf.Abs(athlete.Position.x) > SmashNetDistance ||
                flight.ApexHeight <= SmashContactHeight)
                return ActionDecision.None;

            float smashIdeal = flight.TimeAtHeightDescending(SmashContactHeight);
            float smashOffset = context.FlightTime - smashIdeal;
            TimingGrade smashGrade = TimingWindows.Grade(smashOffset, lateWindow: TimingWindows.SmashLate);
            // The height gate confirms the set was high enough to smash at all, judged at the
            // fixed ideal contact moment - not at the actual press time, which would otherwise
            // penalize a late-but-still-within-window press on top of the timing grade.
            if (smashGrade != TimingGrade.Miss && flight.HeightAt(smashIdeal) >= SmashMinHeight &&
                Vector2.Distance(athlete.Position, flight.GroundAt(smashIdeal)) <= Reach)
                return new ActionDecision(ActionKind.Smash, smashGrade, smashOffset);

            return ActionDecision.None;
        }
```

`ActionContext` is left unchanged. The resolver no longer reads its `OpponentSmashTell`/`OpponentAim` fields, but `PressAction` and the test helper still pass them, and trimming the struct is out of scope.

- [ ] **Step 4: Aim the smash and the AI's block prediction with `SmashAim`**

In `VolleyballMatch.PlayerHit`, `case ActionKind.Smash:` replace both `target = AimAtOpponent(move);` lines with `target = SmashAim(move);`.

In `VolleyballMatch.Opponent.cs` `PositionOpponentWhileDefending`, replace `AimAtOpponent(move)` with `SmashAim(move)`.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" t4-smash`
Expected: all PASS.

If `ASloppyRingFollowerWinsTheExam` or `ASloppyRingFollowerKeepsEveryBallUpAndGetsToSmash` fails, **do not loosen the test or the bot's sloppiness**. Report the failure message (points, dropped-ball log) to the user, because it measures whether a casual player can still win, and it may call for tuning `JumpSeconds`/`JumpCueLead`, which is a design decision.

- [ ] **Step 6: Run the PlayMode journey to confirm the jump-smash bot still completes**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Progression" t4-progression`
Expected: matches the Task 0 baseline.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/ActionResolver.cs Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.cs Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.Opponent.cs Assets/Tests/EditMode/Gameplay/Volleyball
git commit -m "feat(volleyball): smash only mid-air, toward the aimed spot"
```

---

### Task 5: Jump block

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.Opponent.cs` (`PlayerBlocks`, `OpponentAttack`)
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.cs` (event declaration)
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/OpponentAiTests.cs`

**Interfaces:**
- Consumes: `VolleyballMatch.PressJump` (Task 2), `VolleyAthlete.IsAirborne` (Task 1).
- Produces: `public event Action PlayerBlocked;` on `VolleyballMatch`, raised after the block point is awarded.

- [ ] **Step 1: Write the failing tests**

In `OpponentAiTests.cs`, replace `PlayerBlockOnTheTelegraphedLineWinsThePoint` with:

```csharp
        static VolleyballMatch OpponentAboutToSmash(out float netCrossY)
        {
            var match = new VolleyballMatch();
            MatchDriver.ServeGood(match);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.OpponentSmashTell, 6f), Is.True);
            // The block is judged against where the smash crosses the net, not its landing spot
            // (OpponentAim); for this serve the two differ by more than a block's width.
            netCrossY = ActionResolver.NetCrossY(match.Flight.GroundAt(match.FlightTime), match.OpponentAim);
            Assert.That(Mathf.Abs(netCrossY - match.OpponentAim.y), Is.GreaterThan(ActionResolver.BlockLateral));
            return match;
        }

        [Test]
        public void JumpingOnTheTelegraphedLineBlocksTheSmash()
        {
            VolleyballMatch match = OpponentAboutToSmash(out float netCrossY);
            int blocked = 0;
            match.PlayerBlocked += () => blocked++;
            match.Player.PlaceAt(new Vector2(-.8f, netCrossY));
            Assert.That(match.PressJump(), Is.True);

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 1f), Is.True);
            Assert.That(match.PlayerPoints, Is.EqualTo(1));
            Assert.That(match.Winners, Is.EqualTo(1));
            Assert.That(blocked, Is.EqualTo(1));
        }

        [Test]
        public void JumpingOnTheLandingSpotInsteadOfTheNetLineDoesNotBlock()
        {
            VolleyballMatch match = OpponentAboutToSmash(out _);
            int blocked = 0;
            match.PlayerBlocked += () => blocked++;
            match.Player.PlaceAt(new Vector2(-.8f, match.OpponentAim.y));
            Assert.That(match.PressJump(), Is.True);

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.PlayerPoints, Is.Zero);
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
            Assert.That(blocked, Is.Zero);
        }

        [Test]
        public void StandingOnTheLineWithoutJumpingDoesNotBlock()
        {
            VolleyballMatch match = OpponentAboutToSmash(out float netCrossY);
            match.Player.PlaceAt(new Vector2(-.8f, netCrossY));
            Assert.That(match.PressAction().Kind, Is.EqualTo(ActionKind.None), "the hit button no longer blocks");

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.PlayerPoints, Is.Zero);
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.OpponentAiTests" t5-block`
Expected: compile error: `PlayerBlocked` not defined.

- [ ] **Step 3: Implement**

In `VolleyballMatch.cs`, next to the other events:

```csharp
        public event Action PlayerBlocked;
```

In `VolleyballMatch.Opponent.cs`, `PlayerBlocks`:

```csharp
        bool PlayerBlocks(Vector2 target)
        {
            float netCrossY = ActionResolver.NetCrossY(BallGround, target);
            return Player.IsAirborne &&
                   Mathf.Abs(Player.Position.x) <= ActionResolver.BlockNetDistance &&
                   Mathf.Abs(Player.Position.y - netCrossY) <= ActionResolver.BlockLateral;
        }
```

In `OpponentAttack`, raise the event after the rebound:

```csharp
            if (smashing && PlayerBlocks(target))
            {
                Vector2 from = BallGround;
                float startHeight = BallHeight;
                AwardPoint(CourtSide.Player, true);
                Rebound(from, startHeight, new Vector2(ReboundDepth, Player.Position.y), CourtSide.Player);
                // After PointScored, so the block call-out replaces the generic point call-out.
                PlayerBlocked?.Invoke();
                return;
            }
```

Update the class comment at the top of `VolleyballMatch.Opponent.cs` if it mentions blocking with the button (it says "blocks on the steps authored to block", which is about the AI, so no change is needed).

- [ ] **Step 4: Run the tests to verify they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" t5-block`
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.cs Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.Opponent.cs Assets/Tests/EditMode/Gameplay/Volleyball/OpponentAiTests.cs
git commit -m "feat(volleyball): block by jumping at the net"
```

---

### Task 6: Presentation (jump lift, aim ring, HUD texts, cue pulse, "CHẮN!")

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyAthleteView.cs` (`Render`)
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyBallView.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballHud.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs` (`SubscribeMatch`, `UnsubscribeMatch`, `LateUpdate`)
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/PresentationTests.cs`

**Interfaces:**
- Consumes: `VolleyAthlete.JumpHeight`, `IsAirborne`; `VolleyballMatch.PlayerAim`, `TryGetJumpCue`, `PlayerBlocked`; `VolleyballInputBridge.JumpButton`; `JumpButton.SetCue`.
- Produces: `VolleyBallView.Configure(SpriteRenderer, SpriteRenderer, SpriteRenderer, SpriteRenderer, SpriteRenderer playerAimRenderer = null)`, `VolleyBallView.PlayerAimMarker` (`SpriteRenderer`), `VolleyBallView.PlayerAimColor` (`static readonly Color`), `VolleyballHud.PracticeHintFormat`, `VolleyballHud.BlockText`, `VolleyballHud.ShowBlock()`.

- [ ] **Step 1: Write the failing tests**

Append to `PresentationTests`:

```csharp
        [Test]
        public void AthleteViewLiftsAJumpingAthlete()
        {
            SpriteRenderer body = Renderer("Jumper");
            var book = body.gameObject.AddComponent<SpriteFlipbook>();
            var view = body.gameObject.AddComponent<VolleyAthleteView>();
            Sprite[] frames = Frames("f", 2);
            view.Configure(body, book, false, frames, frames, frames, frames, frames, frames);

            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-2f, 1f));
            athlete.TryJump(AthleteAction.Smash);
            athlete.Tick(VolleyAthlete.JumpSeconds / 2f);
            view.Render(athlete);
            Assert.That(view.transform.position,
                Is.EqualTo(CourtSpace.ToWorld(new Vector2(-2f, 1f), athlete.JumpHeight)));
            Assert.That(athlete.JumpHeight, Is.GreaterThan(0f));
        }

        [Test]
        public void PlayerAimRingShowsOnlyMidAirOnTheAimedSpot()
        {
            SpriteRenderer ball = Renderer("Ball"), shadow = Renderer("Shadow"),
                contact = Renderer("Contact"), aim = Renderer("Aim"), playerAim = Renderer("PlayerAim");
            var view = root.AddComponent<VolleyBallView>();
            view.Configure(ball, shadow, contact, aim, playerAim);
            Assert.That(view.PlayerAimMarker, Is.SameAs(playerAim));
            var match = new VolleyballMatch(null, new OpponentTuning(0f, .25f));
            match.ForceServerForTest(CourtSide.Opponent);
            while (match.BallState != BallState.InPlay)
                match.Tick(1f / 60f);

            view.Render(match);
            Assert.That(playerAim.enabled, Is.False);

            Assert.That(match.PressJump(), Is.True);
            match.SetMove(Vector2.up);
            view.Render(match);
            Assert.That(playerAim.enabled, Is.True);
            Assert.That(Vector3.Distance(playerAim.transform.position,
                CourtSpace.ToWorld(VolleyballMatch.SmashAim(Vector2.up), 0f)), Is.LessThan(1e-4f));
            Assert.That(playerAim.sortingOrder, Is.EqualTo(VolleyBallView.MarkerSortingOrder));
        }

        [Test]
        public void HudExplainsJumpingAndCallsOutABlock()
        {
            Assert.That(VolleyballHud.HintText,
                Is.EqualTo("Joystick: di chuyển  ·  NHẢY rồi kéo joystick để nhắm  ·  ĐÁNH để đập"));
            Assert.That(string.Format(VolleyballHud.PracticeHintFormat, 1, 3),
                Is.EqualTo("ĐỠ → CHUYỀN → NHẢY ĐẬP · 1/3 ĐIỂM"));

            var hud = root.AddComponent<VolleyballHud>();
            TMP_Text score = new GameObject("Score").AddComponent<TextMeshPro>();
            TMP_Text feedback = new GameObject("Feedback").AddComponent<TextMeshPro>();
            TMP_Text hint = new GameObject("Hint").AddComponent<TextMeshPro>();
            score.transform.SetParent(root.transform);
            feedback.transform.SetParent(root.transform);
            hint.transform.SetParent(root.transform);
            hud.Configure(score, feedback, hint);
            hud.ShowPoint(CourtSide.Player);
            hud.ShowBlock();
            hud.Render(new VolleyballMatch(), MinigamePhase.Play, .1f);
            Assert.That(feedback.enabled, Is.True);
            Assert.That(feedback.text, Is.EqualTo(VolleyballHud.BlockText));
            Assert.That(VolleyballHud.BlockText, Is.EqualTo("CHẮN!"));
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.PresentationTests" t6-view`
Expected: compile error: 5-argument `Configure`, `PlayerAimMarker`, `PracticeHintFormat`, `ShowBlock`, `BlockText` not defined.

- [ ] **Step 3: Implement the views**

`VolleyAthleteView.Render`: change the position line to:

```csharp
            transform.position = CourtSpace.ToWorld(athlete.Position, athlete.JumpHeight);
```

`VolleyBallView.cs`: add a colour, a field, a property and the optional parameter, then render it at the end of `Render`:

```csharp
        public static readonly Color PlayerAimColor = new Color(.25f, .85f, 1f, .9f);
```

```csharp
        [SerializeField] SpriteRenderer playerAimMarker;

        public SpriteRenderer PlayerAimMarker => playerAimMarker;

        public void Configure(SpriteRenderer ballRenderer, SpriteRenderer shadowRenderer,
            SpriteRenderer contactRenderer, SpriteRenderer aimRenderer, SpriteRenderer playerAimRenderer = null)
        {
            ball = ballRenderer;
            shadow = shadowRenderer;
            contactMarker = contactRenderer;
            aimMarker = aimRenderer;
            playerAimMarker = playerAimRenderer;
        }
```

```csharp
            if (playerAimMarker)
            {
                // Mid-air the stick aims: this ring is where the smash will land.
                bool aiming = match.Player.IsAirborne && match.BallState == BallState.InPlay;
                playerAimMarker.enabled = aiming;
                if (aiming)
                {
                    playerAimMarker.transform.position = CourtSpace.ToWorld(match.PlayerAim, 0f);
                    playerAimMarker.sortingOrder = MarkerSortingOrder;
                }
            }
```

- [ ] **Step 4: Implement the HUD texts and block call-out**

In `VolleyballHud.cs`:

```csharp
        public const string HintText = "Joystick: di chuyển  ·  NHẢY rồi kéo joystick để nhắm  ·  ĐÁNH để đập";
        public const string PracticeHintFormat = "ĐỠ → CHUYỀN → NHẢY ĐẬP · {0}/{1} ĐIỂM";
        public const string BlockText = "CHẮN!";
```

In `Render`, change the practice line to:

```csharp
                    : challenge.Kind == ChallengeKind.Practice ? string.Format(PracticeHintFormat, challengeRules.CompletedTargets, challenge.TargetCount)
```

Add after `ShowPoint`:

```csharp
        public void ShowBlock()
        {
            if (!feedbackLabel)
                return;
            feedbackLabel.text = VietText.Fix(BlockText);
            feedbackLabel.enabled = true;
            feedbackLeft = 1.15f;
        }
```

- [ ] **Step 5: Wire the controller**

In `VolleyballController.cs`:

```csharp
        void SubscribeMatch(VolleyballMatch match)
        {
            match.Completed += OnMatchCompleted;
            match.PlayerActed += OnPlayerActed;
            match.PointScored += OnPointScored;
            match.PlayerBlocked += OnPlayerBlocked;
        }

        void UnsubscribeMatch(VolleyballMatch match)
        {
            if (match == null) return;
            match.Completed -= OnMatchCompleted;
            match.PlayerActed -= OnPlayerActed;
            match.PointScored -= OnPointScored;
            match.PlayerBlocked -= OnPlayerBlocked;
        }
```

```csharp
        void OnPlayerBlocked()
        {
            if (hud)
                hud.ShowBlock();
        }
```

In `LateUpdate`, after `hud.Render(...)`:

```csharp
            if (input.JumpButton)
                input.JumpButton.SetCue(PresentationPhase == MinigamePhase.Play && Match.TryGetJumpCue(out _));
```

- [ ] **Step 6: Run the tests to verify they pass**

Run:
```bash
tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" t6-view
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Volleyball" t6-play
```
Expected: all PASS. `HudTextsMatchTheSpec` and the challenge HUD tests still pass; if one asserts the old practice hint `ĐỠ → CHUYỀN → ĐẬP`, update that assertion to `ĐỠ → CHUYỀN → NHẢY ĐẬP` (the spec changed that text).

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/VolleyAthleteView.cs Assets/_Project/Scripts/Gameplay/Volleyball/VolleyBallView.cs Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballHud.cs Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs Assets/Tests
git commit -m "feat(volleyball): show the jump, the aim ring, the jump cue and block call-out"
```

---

### Task 7: Scene configurator and regenerated scene

**Files:**
- Modify: `Assets/Editor/VolleyballSceneConfigurator.cs` (ball view block ~lines 213-221, `AddControlsAndHud` ~lines 315-372)
- Regenerate: `Assets/_Project/Scenes/MG_Volleyball.unity`
- Test: `Assets/Tests/EditMode/EditorTools/VolleyballSceneConfiguratorTests.cs`

**Interfaces:**
- Consumes: `JumpButton` (Task 3), `VolleyballInputBridge.Configure(..., JumpButton)` and `.JumpButton` (Task 3), `VolleyBallView.Configure(..., playerAim)`, `.PlayerAimMarker`, `PlayerAimColor` (Task 6).

- [ ] **Step 1: Write the failing test**

Append to `VolleyballSceneConfiguratorTests`:

```csharp
        [Test]
        public void JumpButtonAndPlayerAimMarkerAreWired()
        {
            VolleyballSceneConfigurator.BuildScene();
            EditorSceneManager.OpenScene(VolleyballSceneConfigurator.ScenePath, OpenSceneMode.Single);

            var jump = Object.FindFirstObjectByType<JumpButton>();
            Assert.That(jump, Is.Not.Null);
            Assert.That(jump.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("NHẢY"));
            Assert.That(jump.GetComponent<KitPressFeedback>(), Is.Not.Null);

            var controller = Object.FindFirstObjectByType<VolleyballController>();
            Assert.That(controller.Input.JumpButton, Is.SameAs(jump));
            var aim = GameObject.Find("PlayerAimMarker").GetComponent<SpriteRenderer>();
            Assert.That(controller.BallView.PlayerAimMarker, Is.SameAs(aim));
            Assert.That(aim.color, Is.EqualTo(VolleyBallView.PlayerAimColor));

            Rect WorldRect(RectTransform rect)
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            }
            Rect jumpArea = WorldRect((RectTransform)jump.transform);
            Rect hitArea = WorldRect((RectTransform)Object.FindFirstObjectByType<ActionButton>().transform);
            Assert.That(jumpArea.Overlaps(hitArea), Is.False, "two thumbs must not share a hit area");
            Assert.That(jumpArea.center.x, Is.LessThan(hitArea.center.x), "NHẢY sits inside ĐÁNH");
        }
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.VolleyballSceneConfiguratorTests" t7-config`
Expected: FAIL: no `JumpButton` in the scene.

- [ ] **Step 3: Implement**

Ball view block in `BuildScene` (around `ballView.Configure(ball, shadow, contact, aim);`):

```csharp
            SpriteRenderer aim = Renderer("AimMarker", shadowSprite, Vector3.zero, VolleyBallView.MarkerSortingOrder);
            aim.color = AimTint;
            SpriteRenderer playerAim = Renderer("PlayerAimMarker", shadowSprite, Vector3.zero, VolleyBallView.MarkerSortingOrder);
            playerAim.color = VolleyBallView.PlayerAimColor;
            var ballView = new GameObject("BallView").AddComponent<VolleyBallView>();
            ballView.Configure(ball, shadow, contact, aim, playerAim);
```

In `AddControlsAndHud`, right after the `ActionButton`'s `UiKit.RoundButton(...)` line:

```csharp
            // NHẢY sits just inside ĐÁNH and a little higher, its hit area clear of ĐÁNH's.
            RectTransform jumpRect = UiRect("JumpButton", controls, Vector2.one, Vector2.one);
            jumpRect.anchorMin = jumpRect.anchorMax = jumpRect.pivot = new Vector2(1f, 0f);
            jumpRect.sizeDelta = new Vector2(260f, 260f);
            jumpRect.anchoredPosition = new Vector2(-428f, 120f);
            jumpRect.gameObject.AddComponent<Image>().color = Color.clear;
            var jump = jumpRect.gameObject.AddComponent<JumpButton>();
            UiKit.RoundButton(jumpRect, "NHẢY", MinigameUiTheme.RoundButton * JoystickScale);
```

Replace the wiring and dirty list:

```csharp
            controller.Input.Configure(joystick, button, jump);
            controller.Configure(controller.PlayerView, controller.OpponentView, controller.BallView, controller.Input, hud);

            foreach (Object dirty in new Object[] { joystick, button, jump, hud, controller, controller.Input, hudRoot })
                EditorUtility.SetDirty(dirty);
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.VolleyballSceneConfiguratorTests" t7-config`
Expected: all PASS. The run has rebuilt `MG_Volleyball.unity` through `BuildScene()`.

- [ ] **Step 5: Check the scene visually**

Use the `testing-unity-ui-with-screenshots` skill on `MG_Volleyball`. Capture the play state, and if the skill supports scripted states, a frame with the player mid-air. Check:
- NHẢY and ĐÁNH are both fully on screen at phone aspect, the same size, NHẢY left of and above ĐÁNH, not overlapping the joystick area or the hint chip.
- The new hint text fits its chip on one line. If it is cut off, widen the chip in `AddControlsAndHud` (`new Vector2(860f, 66f)` → wider) and re-run Steps 4–5.
- The player aim ring is visibly a different colour from the red NPC aim ring.

If something is off, adjust only the numbers in the configurator, re-run Step 4, and re-check.

- [ ] **Step 6: Commit**

```bash
git add Assets/Editor/VolleyballSceneConfigurator.cs Assets/Tests/EditMode/EditorTools/VolleyballSceneConfiguratorTests.cs Assets/_Project/Scenes/MG_Volleyball.unity
git commit -m "feat(volleyball): add the NHẢY button and player aim ring to the scene"
```

---

### Task 8: Full verification

**Files:** none

- [ ] **Step 1: Run every suite that touches volleyball**

Run:
```bash
tools/run-unity-tests.sh EditMode "" final-edit
tools/run-unity-tests.sh PlayMode "" final-play
```
Expected: no failures beyond the Task 0 baseline. Compare counts from the XML files. Any new failure gets fixed (systematic-debugging skill) before claiming completion.

- [ ] **Step 2: Confirm the working tree**

Run: `git status --short`
Expected: only the user's pre-existing changes (if any were left as instructed in Task 0). No stray generated files from this work.
