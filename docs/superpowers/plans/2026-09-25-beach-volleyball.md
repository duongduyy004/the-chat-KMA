# Beach Volleyball Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a playable 1v1 top-down beach volleyball subject (`MG_Volleyball`), built from scratch on the `BVA2.zip` art, with a virtual joystick, a context-sensitive timed action button, and an authored AI.

**Architecture:** All rules are plain C# in `KMA.Gameplay.Volleyball`: court space, ball flight, athletes, rally state, action resolver, authored opponent plan, and match. They are driven by `Tick(dt)` and are fully EditMode-testable. Thin MonoBehaviours pass joystick and button input in and draw sprites from the match state. An editor configurator imports and slices the art and builds the scene. The existing `MinigameUIAssembler` then adds the shared HUD, phase overlay, result panel and pause panel.

**Tech Stack:** Unity 6000.3.23f1, C#, Input System 1.20, uGUI + TextMeshPro, `com.unity.2d.sprite` (sprite slicing), NUnit via the Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-09-25-beach-volleyball-design.md`

## Global Constraints

- Written from scratch: nothing is restored or adapted from the volleyball code removed in `d313f0b`.
- No random number generation anywhere in the rules; AI variety comes only from `OpponentPlan`.
- Lifecycle `Tutorial → Countdown → Play → Resolve` comes from `MinigameBase`. There is exactly one completion, and the result is built with `ScoreUtil.Build`.
- First to 5 rally points wins, with a 120 s cap. At the cap a lead wins and a tie is a loss.
- Timing windows: PERFECT ±0.08 s (serve ±0.12 s), GOOD ±0.18 s, LATE/EARLY ±0.30 s. Quality: 1.0 / 0.6 / 0.25.
- `SubjectId.Volleyball = 7`, and the save format goes to version 6.
- The controller caps frame time at 0.1 s.
- Namespace `KMA.Gameplay.Volleyball`; tests use `KMA.Tests.Gameplay.Volleyball`.
- Commits go directly on `master`, with no `Co-Authored-By` or other AI attribution trailer.
- **Running tests:** `tools/run-unity-tests.sh <EditMode|PlayMode> <filter> <name>` (created in Task 1).
  - The Unity Editor must be **closed**, because batch mode cannot open a project that is already open.
  - Unity exits 0 even when tests fail. Read the printed `<test-run ... passed=".." failed="..">` line, never the exit code.
- **Running an editor method:** `"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit -projectPath "$(pwd -W)" -executeMethod <Type.Method> -logFile Builds/TestResults/<name>.log` (Editor closed). A non-zero exit or `Exception` in the log means it failed.
- Unity creates `.meta` files for new assets when it imports them (any batch run does this). Commit every generated `.meta` next to its asset.

### Spec amendments (applied in `docs/superpowers/specs/2026-09-25-beach-volleyball-design.md`)

1. **Input:** the keyboard actions are built in code inside `VolleyballInputBridge` instead of adding a `Volleyball` map to `KMA.inputactions`. `InputAssetContractTests` pins that asset's map list to exactly `Sprint, Gameplay, Punishment, UI`.
2. **Contact timing:** a Smash's ideal moment is the descending ball passing 2.6 m (a jump spike hits on the way down), not its apex. Reach (≤ 1.0 m) is measured to the ball's ground position at the ideal moment (the contact point), so timing and positioning are judged separately.
3. **Presentation:** the net is a static frame (no wobble), and only `ballRoll.png` is used for the ball spin.
4. **Scripted rally:** the rally that wins a point is scripted against `VolleyballMatch` in EditMode. PlayMode covers the input-to-match wiring and the single completion.

---

## File Structure

**Runtime:** `Assets/_Project/Scripts/Gameplay/Volleyball/`

| File | Responsibility |
|---|---|
| `KMA.Gameplay.Volleyball.asmdef` | The assembly |
| `CourtSpace.cs` | `CourtSide`, court metres, in/out tests, mapping to background pixels and world units |
| `TimingWindows.cs` | `TimingGrade`, grading and quality |
| `BallFlight.cs` | The ballistic arc from start to target |
| `VolleyAthlete.cs` | `AthleteAction`, one athlete's position, movement, actions and lockout |
| `RallyState.cs` | `TouchOutcome`, possession, touch count, point attribution |
| `OpponentPlan.cs` | `AttackKind`, `OpponentStep`, the authored cycle |
| `ActionResolver.cs` | `BallState`, `ActionKind`, `ActionDecision`, `ActionContext`, and the button logic |
| `VolleyballMatch.cs` | `OpponentTuning`; serve, rally, points, clock, result |
| `VolleyballMatch.Opponent.cs` | The AI half of the match (partial class) |
| `SpriteFlipbook.cs` | Frame-array sprite animation |
| `VolleyAthleteView.cs` | Draws a `VolleyAthlete` |
| `VolleyBallView.cs` | Draws the ball, shadow, contact marker and aim marker |
| `VolleyballHud.cs` | Score, timing feedback and tutorial hint |
| `VirtualJoystick.cs` | The floating left-thumb stick |
| `ActionButton.cs` | The right-thumb button |
| `VolleyballInputBridge.cs` | Merges joystick, button and keyboard input into a move vector and a press count |
| `VolleyballController.cs` | The `MinigameBase` that ticks the match and renders the views |

**Editor:** `Assets/Editor/VolleyballSceneConfigurator.cs` handles art import and slicing and builds the scene.

**Art:**
- `Assets/_Project/Art/Characters/BeachVolley/`: `playerIdle`, `playerRun`, `playerReception`, `playerBlock`, `playerSmash`, `playerSlide` (`.png`)
- `Assets/_Project/Art/Environments/Volleyball/`: `beachbkgO`, `net0`, `ballRoll`, `shadow1` (`.png`), plus a generated `Pixel.png`

**Scene:** `Assets/_Project/Scenes/MG_Volleyball.unity` (generated).

**Tests:**
- `Assets/Tests/EditMode/Gameplay/Volleyball/` (asmdef `KMA.Gameplay.Volleyball.EditMode.Tests`)
- `Assets/Tests/PlayMode/Gameplay/Volleyball/` (asmdef `KMA.Gameplay.Volleyball.PlayMode.Tests`)

**Modified:**

| File | Change |
|---|---|
| `SubjectId.cs`, `SaveData.cs`, `SaveSystem.cs` | New subject and save v6 |
| `SceneRouter.cs`, `S5RouteBootstrap.cs` | Route to the new scene |
| `MapPresentationBuilder.cs` | New map card |
| `MinigameUIAssembler.cs` | New public `AssembleScenePath` plus the scene path |
| `KMA.EditorTools.asmdef` | New references |
| `ScriptableObjects/Subjects/Volleyball.asset` | New subject config |
| `README.md`, `PLAN.md` | Docs |
| Progression and presentation tests | Subject counts |

---

### Task 1: Assembly, test harness, `CourtSpace`, `TimingWindows`

**Files:**
- Create: `tools/run-unity-tests.sh`
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/KMA.Gameplay.Volleyball.asmdef`
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/CourtSpace.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/TimingWindows.cs`
- Create: `Assets/Tests/EditMode/Gameplay/Volleyball/KMA.Gameplay.Volleyball.EditMode.Tests.asmdef`
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/CourtSpaceTests.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/TimingWindowsTests.cs`

**Interfaces:**
- Produces:
  - `enum CourtSide { Player, Opponent }`
  - `CourtSides.Other(this CourtSide)`
  - `static class CourtSpace`:
    - consts `HalfLength=8`, `HalfWidth=4`, `NetHeight=2.24`, `BackgroundPixelsPerUnit=30`, `PixelsPerMetreX=22.875`, `PixelsPerMetreY=33.5`, `HeightLift=0.8`
    - `IsIn(Vector2)`, `SideOf(Vector2)`, `SideSign(CourtSide)`
    - `ToBackgroundPixel(Vector2)`, `ToWorld(Vector2 ground, float height)`, `BackgroundWorldPosition`
  - `enum TimingGrade { Miss, Late, Good, Perfect }`
  - `static class TimingWindows`:
    - consts `Perfect=.08`, `Good=.18`, `Late=.30`, `ServePerfect=.12`
    - `Grade(float offset, float perfectWindow = Perfect)`, `Quality(TimingGrade)`

- [ ] **Step 1: Create the test runner script**

`tools/run-unity-tests.sh`:
```bash
#!/usr/bin/env bash
# Run a Unity test filter in batch mode and print the <test-run> summary.
# Usage: tools/run-unity-tests.sh <EditMode|PlayMode> <filter> <name>
# The Unity Editor must be closed: batch mode cannot open a project that is already open.
# Unity exits 0 even when tests fail, so read the printed summary, not the exit code.
set -uo pipefail

PLATFORM="${1:?usage: run-unity-tests.sh <EditMode|PlayMode> <filter> <name>}"
FILTER="${2:?filter required}"
NAME="${3:?result name required}"
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe}"
OUT="Builds/TestResults"

mkdir -p "$OUT"
rm -f "$OUT/$NAME.xml"
"$UNITY" -batchmode -projectPath "$(pwd -W)" -runTests -testPlatform "$PLATFORM" \
  -testFilter "$FILTER" -testResults "$OUT/$NAME.xml" -logFile "$OUT/$NAME.log"

if [ -f "$OUT/$NAME.xml" ]; then
  grep -o '<test-run[^>]*>' "$OUT/$NAME.xml"
  grep -o '<test-case [^>]*result="Failed"[^>]*>' "$OUT/$NAME.xml" | head -20
else
  echo "No test results; compiler errors from $OUT/$NAME.log:"
  grep -n "error CS" "$OUT/$NAME.log" | head -20
  exit 1
fi
```
Run: `chmod +x tools/run-unity-tests.sh`

- [ ] **Step 2: Create the runtime and test assemblies**

`Assets/_Project/Scripts/Gameplay/Volleyball/KMA.Gameplay.Volleyball.asmdef`:
```json
{
    "name": "KMA.Gameplay.Volleyball",
    "rootNamespace": "KMA.Gameplay.Volleyball",
    "references": [
        "KMA.Gameplay",
        "Unity.InputSystem",
        "UnityEngine.UI",
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/Tests/EditMode/Gameplay/Volleyball/KMA.Gameplay.Volleyball.EditMode.Tests.asmdef`:
```json
{
    "name": "KMA.Gameplay.Volleyball.EditMode.Tests",
    "rootNamespace": "KMA.Tests.Gameplay.Volleyball",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "KMA.Gameplay",
        "KMA.Gameplay.Volleyball",
        "UnityEngine.UI",
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/Volleyball/CourtSpaceTests.cs`:
```csharp
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class CourtSpaceTests
    {
        [Test]
        public void CourtCornersAndNetMapOntoTheBackgroundLines()
        {
            Assert.That(CourtSpace.ToBackgroundPixel(new Vector2(-8f, 4f)), Is.EqualTo(new Vector2(17f, 145f)));
            Assert.That(CourtSpace.ToBackgroundPixel(new Vector2(8f, -4f)), Is.EqualTo(new Vector2(383f, 413f)));
            Assert.That(CourtSpace.ToBackgroundPixel(Vector2.zero), Is.EqualTo(new Vector2(200f, 279f)));
        }

        [Test]
        public void WorldPositionAgreesWithTheBackgroundSpritePlacement()
        {
            var ground = new Vector2(-5.5f, 2.25f);
            Vector2 pixel = CourtSpace.ToBackgroundPixel(ground);
            Vector3 expected = CourtSpace.BackgroundWorldPosition + new Vector3(
                (pixel.x - 200f) / CourtSpace.BackgroundPixelsPerUnit,
                (215f - pixel.y) / CourtSpace.BackgroundPixelsPerUnit, 0f);

            Vector3 actual = CourtSpace.ToWorld(ground, 0f);

            Assert.That(actual.x, Is.EqualTo(expected.x).Within(1e-4f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(1e-4f));
        }

        [Test]
        public void HeightLiftsTheWorldPositionStraightUp()
        {
            Vector3 ground = CourtSpace.ToWorld(new Vector2(3f, 1f), 0f);
            Vector3 raised = CourtSpace.ToWorld(new Vector2(3f, 1f), 2f);

            Assert.That(raised.x, Is.EqualTo(ground.x));
            Assert.That(raised.y - ground.y,
                Is.EqualTo(2f * CourtSpace.HeightLift * CourtSpace.PixelsPerMetreY / CourtSpace.BackgroundPixelsPerUnit).Within(1e-4f));
        }

        [Test]
        public void LinesCountAsIn()
        {
            Assert.That(CourtSpace.IsIn(new Vector2(8f, 4f)), Is.True);
            Assert.That(CourtSpace.IsIn(new Vector2(-8f, -4f)), Is.True);
            Assert.That(CourtSpace.IsIn(new Vector2(8.01f, 0f)), Is.False);
            Assert.That(CourtSpace.IsIn(new Vector2(0f, -4.01f)), Is.False);
        }

        [Test]
        public void SidesSplitAtTheNet()
        {
            Assert.That(CourtSpace.SideOf(new Vector2(-.1f, 0f)), Is.EqualTo(CourtSide.Player));
            Assert.That(CourtSpace.SideOf(new Vector2(.1f, 0f)), Is.EqualTo(CourtSide.Opponent));
            Assert.That(CourtSide.Player.Other(), Is.EqualTo(CourtSide.Opponent));
            Assert.That(CourtSide.Opponent.Other(), Is.EqualTo(CourtSide.Player));
            Assert.That(CourtSpace.SideSign(CourtSide.Player), Is.EqualTo(-1f));
        }
    }
}
```

`Assets/Tests/EditMode/Gameplay/Volleyball/TimingWindowsTests.cs`:
```csharp
using KMA.Gameplay.Volleyball;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class TimingWindowsTests
    {
        [TestCase(0f, TimingGrade.Perfect)]
        [TestCase(.08f, TimingGrade.Perfect)]
        [TestCase(-.08f, TimingGrade.Perfect)]
        [TestCase(.081f, TimingGrade.Good)]
        [TestCase(-.18f, TimingGrade.Good)]
        [TestCase(.181f, TimingGrade.Late)]
        [TestCase(-.3f, TimingGrade.Late)]
        [TestCase(.301f, TimingGrade.Miss)]
        public void GradesByAbsoluteOffset(float offset, TimingGrade expected)
        {
            Assert.That(TimingWindows.Grade(offset), Is.EqualTo(expected));
        }

        [Test]
        public void ServeUsesTheWiderPerfectWindow()
        {
            Assert.That(TimingWindows.Grade(.11f, TimingWindows.ServePerfect), Is.EqualTo(TimingGrade.Perfect));
            Assert.That(TimingWindows.Grade(.11f), Is.EqualTo(TimingGrade.Good));
        }

        [Test]
        public void QualityMatchesTheSpec()
        {
            Assert.That(TimingWindows.Quality(TimingGrade.Perfect), Is.EqualTo(1f));
            Assert.That(TimingWindows.Quality(TimingGrade.Good), Is.EqualTo(.6f));
            Assert.That(TimingWindows.Quality(TimingGrade.Late), Is.EqualTo(.25f));
            Assert.That(TimingWindows.Quality(TimingGrade.Miss), Is.EqualTo(0f));
        }
    }
}
```

- [ ] **Step 4: Run to verify failure**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" vb-t1`
Expected: "No test results" with `error CS0246` for `CourtSpace` / `TimingWindows`.

- [ ] **Step 5: Implement**

`Assets/_Project/Scripts/Gameplay/Volleyball/CourtSpace.cs`:
```csharp
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public enum CourtSide
    {
        Player,
        Opponent
    }

    public static class CourtSides
    {
        public static CourtSide Other(this CourtSide side) =>
            side == CourtSide.Player ? CourtSide.Opponent : CourtSide.Player;
    }

    // Court metres: x runs along the court (net at 0, player half negative), y across it (+y is
    // the far sideline, drawn higher on screen). Pixel constants come from beachbkgO.png, whose
    // outer lines sit at x 17..383 and y 145..413 with the net line at x 200.
    public static class CourtSpace
    {
        public const float HalfLength = 8f;
        public const float HalfWidth = 4f;
        public const float NetHeight = 2.24f;
        public const float BackgroundPixelsPerUnit = 30f;
        public const float PixelsPerMetreX = 22.875f;
        public const float PixelsPerMetreY = 33.5f;
        public const float HeightLift = .8f;

        static readonly Vector2 CourtCentrePixel = new Vector2(200f, 279f);
        static readonly Vector2 BackgroundCentrePixel = new Vector2(200f, 215f);

        public static Vector3 BackgroundWorldPosition => new Vector3(0f,
            (CourtCentrePixel.y - BackgroundCentrePixel.y) / BackgroundPixelsPerUnit, 0f);

        public static bool IsIn(Vector2 ground) =>
            Mathf.Abs(ground.x) <= HalfLength && Mathf.Abs(ground.y) <= HalfWidth;

        public static CourtSide SideOf(Vector2 ground) => ground.x < 0f ? CourtSide.Player : CourtSide.Opponent;

        public static float SideSign(CourtSide side) => side == CourtSide.Player ? -1f : 1f;

        public static Vector2 ToBackgroundPixel(Vector2 ground) => new Vector2(
            CourtCentrePixel.x + ground.x * PixelsPerMetreX,
            CourtCentrePixel.y - ground.y * PixelsPerMetreY);

        public static Vector3 ToWorld(Vector2 ground, float height) => new Vector3(
            ground.x * PixelsPerMetreX / BackgroundPixelsPerUnit,
            (ground.y + height * HeightLift) * PixelsPerMetreY / BackgroundPixelsPerUnit,
            0f);
    }
}
```

`Assets/_Project/Scripts/Gameplay/Volleyball/TimingWindows.cs`:
```csharp
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public enum TimingGrade
    {
        Miss,
        Late,
        Good,
        Perfect
    }

    // Late covers both early and late presses; the sign of the offset tells them apart.
    public static class TimingWindows
    {
        public const float Perfect = .08f;
        public const float Good = .18f;
        public const float Late = .30f;
        public const float ServePerfect = .12f;

        public static TimingGrade Grade(float offset, float perfectWindow = Perfect)
        {
            float distance = Mathf.Abs(offset);
            if (distance <= perfectWindow) return TimingGrade.Perfect;
            if (distance <= Good) return TimingGrade.Good;
            if (distance <= Late) return TimingGrade.Late;
            return TimingGrade.Miss;
        }

        public static float Quality(TimingGrade grade) => grade switch
        {
            TimingGrade.Perfect => 1f,
            TimingGrade.Good => .6f,
            TimingGrade.Late => .25f,
            _ => 0f
        };
    }
}
```

- [ ] **Step 6: Run to verify pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" vb-t1`
Expected: `<test-run ... failed="0" ...>` with 15 passed.

- [ ] **Step 7: Commit** (including the generated `.meta` files)

```bash
git add tools/run-unity-tests.sh Assets/_Project/Scripts/Gameplay/Volleyball Assets/_Project/Scripts/Gameplay/Volleyball.meta Assets/Tests/EditMode/Gameplay/Volleyball Assets/Tests/EditMode/Gameplay/Volleyball.meta
git commit -m "feat(volleyball): add court space and timing windows"
```

---

### Task 2: `BallFlight`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/BallFlight.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/BallFlightTests.cs`

**Interfaces:**
- Consumes: `CourtSpace.NetHeight`, `CourtSide`
- Produces: `sealed class BallFlight`
  - ctor `(Vector2 start, float startHeight, Vector2 target, float apexHeight, CourtSide hitter)`
  - const `Gravity=12`
  - props `Start`, `Target`, `StartHeight`, `ApexHeight`, `VerticalSpeed`, `Duration`, `Hitter`, `ApexTime`, `CrossesNet`, `NetCrossTime`, `ClearsNet`
  - methods `GroundAt(t)`, `HeightAt(t)`, `IsDescending(t)`, `TimeAtHeightDescending(h)`

- [ ] **Step 1: Write the failing test**

`Assets/Tests/EditMode/Gameplay/Volleyball/BallFlightTests.cs`:
```csharp
using System;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class BallFlightTests
    {
        static BallFlight Serve() =>
            new BallFlight(new Vector2(8.5f, 0f), 3.2f, new Vector2(-5f, 0f), 4.5f, CourtSide.Opponent);

        [Test]
        public void PeaksAtTheApexHeight()
        {
            BallFlight flight = Serve();
            Assert.That(flight.HeightAt(flight.ApexTime), Is.EqualTo(4.5f).Within(1e-3f));
            Assert.That(flight.ApexTime, Is.LessThan(flight.Duration));
        }

        [Test]
        public void LandsOnTheTargetAtItsDuration()
        {
            BallFlight flight = Serve();
            Assert.That(flight.GroundAt(flight.Duration), Is.EqualTo(new Vector2(-5f, 0f)));
            Assert.That(flight.HeightAt(flight.Duration), Is.EqualTo(0f).Within(1e-3f));
            Assert.That(flight.GroundAt(flight.Duration + 1f), Is.EqualTo(new Vector2(-5f, 0f)));
            Assert.That(flight.GroundAt(0f), Is.EqualTo(new Vector2(8.5f, 0f)));
        }

        [Test]
        public void TimeAtHeightDescendingIsAfterTheApex()
        {
            BallFlight flight = Serve();
            float t = flight.TimeAtHeightDescending(1f);
            Assert.That(t, Is.GreaterThan(flight.ApexTime));
            Assert.That(flight.HeightAt(t), Is.EqualTo(1f).Within(1e-3f));
            Assert.That(flight.IsDescending(t), Is.True);
            Assert.That(flight.TimeAtHeightDescending(10f), Is.EqualTo(flight.ApexTime));
            Assert.That(flight.TimeAtHeightDescending(0f), Is.EqualTo(flight.Duration));
        }

        [Test]
        public void SmashFromTheSetSpotClearsTheNet()
        {
            var smash = new BallFlight(new Vector2(-1.5f, 0f), 2.6f, new Vector2(7f, 0f), 2.8f, CourtSide.Player);
            Assert.That(smash.CrossesNet, Is.True);
            Assert.That(smash.NetCrossTime, Is.GreaterThan(0f).And.LessThan(smash.Duration));
            Assert.That(smash.ClearsNet, Is.True);
        }

        [Test]
        public void LowFlatShotFailsNetClearance()
        {
            var shot = new BallFlight(new Vector2(-3f, 0f), 1f, new Vector2(5f, 0f), 1f, CourtSide.Player);
            Assert.That(shot.CrossesNet, Is.True);
            Assert.That(shot.ClearsNet, Is.False);
        }

        [Test]
        public void FlightThatStaysOnOneSideDoesNotCrossTheNet()
        {
            var set = new BallFlight(new Vector2(-6f, 0f), 1f, new Vector2(-1.5f, 0f), 4f, CourtSide.Player);
            Assert.That(set.CrossesNet, Is.False);
            Assert.That(set.NetCrossTime, Is.EqualTo(-1f));
            Assert.That(set.ClearsNet, Is.True);
        }

        [Test]
        public void RejectsAnApexBelowTheStart()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BallFlight(Vector2.zero, 2f, Vector2.one, 1f, CourtSide.Player));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BallFlight(Vector2.zero, 0f, Vector2.one, 0f, CourtSide.Player));
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.BallFlightTests" vb-t2`
Expected: compile error `BallFlight` not found.

- [ ] **Step 3: Implement**

`Assets/_Project/Scripts/Gameplay/Volleyball/BallFlight.cs`:
```csharp
using System;
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    // A ball arc: linear over the ground from Start to Target, parabolic in height, landing
    // (height 0) exactly on Target at Duration. Solved once at launch so every landing point and
    // contact moment is known up front.
    public sealed class BallFlight
    {
        public const float Gravity = 12f;

        public BallFlight(Vector2 start, float startHeight, Vector2 target, float apexHeight, CourtSide hitter)
        {
            if (startHeight < 0f)
                throw new ArgumentOutOfRangeException(nameof(startHeight));
            if (apexHeight < startHeight || apexHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(apexHeight));

            Start = start;
            Target = target;
            StartHeight = startHeight;
            ApexHeight = apexHeight;
            Hitter = hitter;
            VerticalSpeed = Mathf.Sqrt(2f * Gravity * (apexHeight - startHeight));
            Duration = (VerticalSpeed + Mathf.Sqrt(VerticalSpeed * VerticalSpeed + 2f * Gravity * startHeight)) / Gravity;
        }

        public Vector2 Start { get; }
        public Vector2 Target { get; }
        public float StartHeight { get; }
        public float ApexHeight { get; }
        public float VerticalSpeed { get; }
        public float Duration { get; }
        public CourtSide Hitter { get; }
        public float ApexTime => VerticalSpeed / Gravity;
        public bool CrossesNet => (Start.x < 0f) != (Target.x < 0f);
        public float NetCrossTime => CrossesNet ? Duration * -Start.x / (Target.x - Start.x) : -1f;
        public bool ClearsNet => !CrossesNet || HeightAt(NetCrossTime) >= CourtSpace.NetHeight;

        public Vector2 GroundAt(float time) => Vector2.Lerp(Start, Target, Mathf.Clamp01(time / Duration));

        public float HeightAt(float time)
        {
            float t = Mathf.Clamp(time, 0f, Duration);
            return Mathf.Max(0f, StartHeight + VerticalSpeed * t - .5f * Gravity * t * t);
        }

        public bool IsDescending(float time) => time > ApexTime;

        public float TimeAtHeightDescending(float height)
        {
            if (height >= ApexHeight) return ApexTime;
            if (height <= 0f) return Duration;
            return (VerticalSpeed + Mathf.Sqrt(VerticalSpeed * VerticalSpeed + 2f * Gravity * (StartHeight - height))) / Gravity;
        }
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.BallFlightTests" vb-t2`
Expected: `failed="0"`, 7 passed.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/BallFlight.cs* Assets/Tests/EditMode/Gameplay/Volleyball/BallFlightTests.cs*
git commit -m "feat(volleyball): add ballistic ball flight"
```

---

### Task 3: `VolleyAthlete` and `RallyState`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyAthlete.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/RallyState.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/VolleyAthleteTests.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/RallyStateTests.cs`

**Interfaces:**
- Consumes: `CourtSide`, `CourtSpace`
- Produces:
  - `enum AthleteAction { Idle, Run, Serve, Receive, Smash, Block, Dive }`
  - `sealed class VolleyAthlete`:
    - ctor `(CourtSide side, float speed)`
    - `Side`, `Speed`, `Position`, `Action`, `LockTimeLeft`, `IsLocked`
    - `PlaceAt(Vector2)`, `Move(Vector2 input, float dt)`, `MoveToward(Vector2 target, float dt)`, `BeginAction(AthleteAction, float seconds)`, `Lunge(Vector2 toward, float distance)`, `Tick(float dt)`
  - `enum TouchOutcome { Kept, SentOver, FourthTouchFault }`
  - `sealed class RallyState`:
    - `Possession`, `Touches`, `LastToucher`
    - `BeginServe(CourtSide)`, `RegisterServe(CourtSide)`, `RegisterTouch(CourtSide, bool sendsOver) → TouchOutcome`
    - `WinnerForLanding(Vector2) → CourtSide`, `WinnerForNetFault() → CourtSide`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/Volleyball/VolleyAthleteTests.cs`:
```csharp
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyAthleteTests
    {
        [Test]
        public void MovesAtItsSpeedAndReportsRunning()
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-5f, 0f));
            athlete.Move(Vector2.up, .2f);
            Assert.That(athlete.Position, Is.EqualTo(new Vector2(-5f, 1f)));
            Assert.That(athlete.Action, Is.EqualTo(AthleteAction.Run));
            athlete.Move(Vector2.zero, .2f);
            Assert.That(athlete.Action, Is.EqualTo(AthleteAction.Idle));
        }

        [Test]
        public void InputLongerThanOneIsClamped()
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-5f, 0f));
            athlete.Move(new Vector2(0f, 3f), .2f);
            Assert.That(athlete.Position.y, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void StaysOnItsOwnSideOfTheNet()
        {
            var player = new VolleyAthlete(CourtSide.Player, 5f);
            player.PlaceAt(new Vector2(-1f, 0f));
            player.Move(Vector2.right, 2f);
            Assert.That(player.Position.x, Is.EqualTo(-VolleyAthlete.NetGap));

            var opponent = new VolleyAthlete(CourtSide.Opponent, 5f);
            opponent.PlaceAt(new Vector2(-3f, 20f));
            Assert.That(opponent.Position, Is.EqualTo(new Vector2(VolleyAthlete.NetGap,
                CourtSpace.HalfWidth + VolleyAthlete.SideMargin)));
        }

        [Test]
        public void LockedAthleteCannotMoveUntilTheActionEnds()
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-5f, 0f));
            athlete.BeginAction(AthleteAction.Dive, .8f);
            athlete.Move(Vector2.up, .2f);
            Assert.That(athlete.Position, Is.EqualTo(new Vector2(-5f, 0f)));
            Assert.That(athlete.Action, Is.EqualTo(AthleteAction.Dive));

            athlete.Tick(.8f);
            Assert.That(athlete.IsLocked, Is.False);
            Assert.That(athlete.Action, Is.EqualTo(AthleteAction.Idle));
        }

        [Test]
        public void MoveTowardDoesNotOvershoot()
        {
            var athlete = new VolleyAthlete(CourtSide.Opponent, 5f);
            athlete.PlaceAt(new Vector2(5f, 0f));
            athlete.MoveToward(new Vector2(5f, .5f), 1f);
            Assert.That(athlete.Position.y, Is.EqualTo(.5f).Within(1e-4f));
        }

        [Test]
        public void LungeMovesAtMostTheGivenDistance()
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-5f, 0f));
            athlete.Lunge(new Vector2(-5f, 3f), 1.2f);
            Assert.That(athlete.Position.y, Is.EqualTo(1.2f).Within(1e-4f));
        }
    }
}
```

`Assets/Tests/EditMode/Gameplay/Volleyball/RallyStateTests.cs`:
```csharp
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class RallyStateTests
    {
        [Test]
        public void ServeHandsPossessionToTheReceiver()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            rally.RegisterServe(CourtSide.Player);
            Assert.That(rally.Possession, Is.EqualTo(CourtSide.Opponent));
            Assert.That(rally.Touches, Is.Zero);
            Assert.That(rally.LastToucher, Is.EqualTo(CourtSide.Player));
        }

        [Test]
        public void FourthTouchOnOneSideIsAFault()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Opponent);
            rally.RegisterServe(CourtSide.Opponent);
            Assert.That(rally.RegisterTouch(CourtSide.Player, false), Is.EqualTo(TouchOutcome.Kept));
            Assert.That(rally.RegisterTouch(CourtSide.Player, false), Is.EqualTo(TouchOutcome.Kept));
            Assert.That(rally.RegisterTouch(CourtSide.Player, false), Is.EqualTo(TouchOutcome.Kept));
            Assert.That(rally.RegisterTouch(CourtSide.Player, false), Is.EqualTo(TouchOutcome.FourthTouchFault));
        }

        [Test]
        public void SendingOverResetsTouchesForTheOtherSide()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Opponent);
            rally.RegisterServe(CourtSide.Opponent);
            rally.RegisterTouch(CourtSide.Player, false);
            Assert.That(rally.RegisterTouch(CourtSide.Player, true), Is.EqualTo(TouchOutcome.SentOver));
            Assert.That(rally.Possession, Is.EqualTo(CourtSide.Opponent));
            Assert.That(rally.Touches, Is.Zero);
        }

        [Test]
        public void BallLandingInScoresForTheOtherSide()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            rally.RegisterServe(CourtSide.Player);
            Assert.That(rally.WinnerForLanding(new Vector2(6f, 2f)), Is.EqualTo(CourtSide.Player));
            Assert.That(rally.WinnerForLanding(new Vector2(-6f, 2f)), Is.EqualTo(CourtSide.Opponent));
            Assert.That(rally.WinnerForLanding(new Vector2(8f, 4f)), Is.EqualTo(CourtSide.Player));
        }

        [Test]
        public void BallLandingOutScoresAgainstTheLastToucher()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            rally.RegisterServe(CourtSide.Player);
            Assert.That(rally.WinnerForLanding(new Vector2(9f, 0f)), Is.EqualTo(CourtSide.Opponent));
            rally.RegisterTouch(CourtSide.Opponent, true);
            Assert.That(rally.WinnerForLanding(new Vector2(-9f, 0f)), Is.EqualTo(CourtSide.Player));
        }

        [Test]
        public void NetFaultScoresAgainstTheHitter()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Opponent);
            rally.RegisterServe(CourtSide.Opponent);
            rally.RegisterTouch(CourtSide.Player, true);
            Assert.That(rally.WinnerForNetFault(), Is.EqualTo(CourtSide.Opponent));
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" vb-t3`
Expected: compile errors for `VolleyAthlete`, `RallyState`.

- [ ] **Step 3: Implement**

`Assets/_Project/Scripts/Gameplay/Volleyball/VolleyAthlete.cs`:
```csharp
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public enum AthleteAction
    {
        Idle,
        Run,
        Serve,
        Receive,
        Smash,
        Block,
        Dive
    }

    public sealed class VolleyAthlete
    {
        public const float NetGap = .3f;
        public const float BackMargin = 1.5f;
        public const float SideMargin = 1f;

        public VolleyAthlete(CourtSide side, float speed)
        {
            Side = side;
            Speed = Mathf.Max(0f, speed);
        }

        public CourtSide Side { get; }
        public float Speed { get; }
        public Vector2 Position { get; private set; }
        public AthleteAction Action { get; private set; }
        public float LockTimeLeft { get; private set; }
        public bool IsLocked => LockTimeLeft > 0f;

        public void PlaceAt(Vector2 position)
        {
            Position = Clamp(position);
            Action = AthleteAction.Idle;
            LockTimeLeft = 0f;
        }

        public void Move(Vector2 input, float deltaTime)
        {
            if (IsLocked)
                return;

            Vector2 direction = Vector2.ClampMagnitude(input, 1f);
            if (direction.sqrMagnitude < .0001f)
            {
                Action = AthleteAction.Idle;
                return;
            }

            Position = Clamp(Position + direction * Speed * deltaTime);
            Action = AthleteAction.Run;
        }

        public void MoveToward(Vector2 target, float deltaTime)
        {
            Vector2 delta = target - Position;
            float distance = delta.magnitude;
            float step = Speed * deltaTime;
            if (distance < .01f || step <= 0f)
            {
                Move(Vector2.zero, deltaTime);
                return;
            }

            Move(delta / distance * Mathf.Min(1f, distance / step), deltaTime);
        }

        public void BeginAction(AthleteAction action, float seconds)
        {
            Action = action;
            LockTimeLeft = Mathf.Max(0f, seconds);
        }

        public void Lunge(Vector2 toward, float distance)
        {
            Position = Clamp(Position + Vector2.ClampMagnitude(toward - Position, distance));
        }

        public void Tick(float deltaTime)
        {
            if (LockTimeLeft <= 0f)
                return;

            LockTimeLeft = Mathf.Max(0f, LockTimeLeft - deltaTime);
            if (LockTimeLeft <= 0f)
                Action = AthleteAction.Idle;
        }

        Vector2 Clamp(Vector2 position)
        {
            float far = CourtSpace.HalfLength + BackMargin;
            float wide = CourtSpace.HalfWidth + SideMargin;
            float y = Mathf.Clamp(position.y, -wide, wide);
            return Side == CourtSide.Player
                ? new Vector2(Mathf.Clamp(position.x, -far, -NetGap), y)
                : new Vector2(Mathf.Clamp(position.x, NetGap, far), y);
        }
    }
}
```

`Assets/_Project/Scripts/Gameplay/Volleyball/RallyState.cs`:
```csharp
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public enum TouchOutcome
    {
        Kept,
        SentOver,
        FourthTouchFault
    }

    public sealed class RallyState
    {
        public const int MaxTouches = 3;

        public CourtSide Possession { get; private set; }
        public int Touches { get; private set; }
        public CourtSide LastToucher { get; private set; }

        public void BeginServe(CourtSide server)
        {
            Possession = server;
            LastToucher = server;
            Touches = 0;
        }

        public void RegisterServe(CourtSide server)
        {
            LastToucher = server;
            Possession = server.Other();
            Touches = 0;
        }

        public TouchOutcome RegisterTouch(CourtSide side, bool sendsOver)
        {
            if (side != Possession)
            {
                Possession = side;
                Touches = 0;
            }

            LastToucher = side;
            Touches++;
            if (Touches > MaxTouches)
                return TouchOutcome.FourthTouchFault;
            if (!sendsOver)
                return TouchOutcome.Kept;

            Possession = side.Other();
            Touches = 0;
            return TouchOutcome.SentOver;
        }

        public CourtSide WinnerForLanding(Vector2 landing) => CourtSpace.IsIn(landing)
            ? CourtSpace.SideOf(landing).Other()
            : LastToucher.Other();

        public CourtSide WinnerForNetFault() => LastToucher.Other();
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" vb-t3`
Expected: `failed="0"`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/VolleyAthlete.cs* Assets/_Project/Scripts/Gameplay/Volleyball/RallyState.cs* Assets/Tests/EditMode/Gameplay/Volleyball/VolleyAthleteTests.cs* Assets/Tests/EditMode/Gameplay/Volleyball/RallyStateTests.cs*
git commit -m "feat(volleyball): add athlete movement and rally rules"
```

---

### Task 4: `OpponentPlan`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/OpponentPlan.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/OpponentPlanTests.cs`

**Interfaces:**
- Produces:
  - `enum AttackKind { Smash, Tip, Lob }`
  - `readonly struct OpponentStep`:
    - ctor `(Vector2 serveTarget, AttackKind attack, float attackDepth, float attackLateral, bool weakReceive, bool blocksPlayerSmash)`
    - fields of the same names in PascalCase
  - `sealed class OpponentPlan`:
    - ctor `(IReadOnlyList<OpponentStep>)`, `static Authored()`
    - `Count`, `Index`, `Current`, `Advance()`
    - `static AttackTarget(OpponentStep, Vector2 playerPosition) → Vector2`

- [ ] **Step 1: Write the failing test**

`Assets/Tests/EditMode/Gameplay/Volleyball/OpponentPlanTests.cs`:
```csharp
using System;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class OpponentPlanTests
    {
        [Test]
        public void AuthoredPlanHasEightStepsAndWraps()
        {
            OpponentPlan plan = OpponentPlan.Authored();
            Assert.That(plan.Count, Is.EqualTo(8));
            OpponentStep first = plan.Current;
            for (int i = 0; i < 8; i++)
                plan.Advance();
            Assert.That(plan.Index, Is.Zero);
            Assert.That(plan.Current.ServeTarget, Is.EqualTo(first.ServeTarget));
        }

        [Test]
        public void AuthoredPlanMatchesTheSpecTable()
        {
            OpponentPlan plan = OpponentPlan.Authored();
            var attacks = new AttackKind[8];
            var blocks = new bool[8];
            var weak = new bool[8];
            for (int i = 0; i < 8; i++)
            {
                attacks[i] = plan.Current.Attack;
                blocks[i] = plan.Current.BlocksPlayerSmash;
                weak[i] = plan.Current.WeakReceive;
                Assert.That(CourtSpace.IsIn(plan.Current.ServeTarget), Is.True, $"step {i + 1}");
                Assert.That(plan.Current.ServeTarget.x, Is.LessThan(0f), $"step {i + 1}");
                plan.Advance();
            }

            Assert.That(attacks, Is.EqualTo(new[]
            {
                AttackKind.Smash, AttackKind.Tip, AttackKind.Lob, AttackKind.Smash,
                AttackKind.Lob, AttackKind.Smash, AttackKind.Tip, AttackKind.Lob
            }));
            Assert.That(blocks, Is.EqualTo(new[] { true, false, false, true, false, true, false, false }));
            Assert.That(weak, Is.EqualTo(new[] { false, false, false, false, true, false, false, false }));
        }

        [Test]
        public void AttackTargetsTheSidelineAwayFromThePlayer()
        {
            var step = new OpponentStep(new Vector2(-7f, 0f), AttackKind.Smash, -7f, 3f, false, false);
            Assert.That(OpponentPlan.AttackTarget(step, new Vector2(-5f, 1f)), Is.EqualTo(new Vector2(-7f, -3f)));
            Assert.That(OpponentPlan.AttackTarget(step, new Vector2(-5f, -1f)), Is.EqualTo(new Vector2(-7f, 3f)));
            Assert.That(OpponentPlan.AttackTarget(step, new Vector2(-5f, 0f)), Is.EqualTo(new Vector2(-7f, -3f)));
        }

        [Test]
        public void TwoAuthoredPlansAreIdentical()
        {
            OpponentPlan a = OpponentPlan.Authored();
            OpponentPlan b = OpponentPlan.Authored();
            for (int i = 0; i < 16; i++)
            {
                Assert.That(a.Current.ServeTarget, Is.EqualTo(b.Current.ServeTarget));
                Assert.That(a.Current.Attack, Is.EqualTo(b.Current.Attack));
                a.Advance();
                b.Advance();
            }
        }

        [Test]
        public void RejectsAnEmptyPlan()
        {
            Assert.Throws<ArgumentException>(() => new OpponentPlan(Array.Empty<OpponentStep>()));
            Assert.Throws<ArgumentException>(() => new OpponentPlan(null));
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.OpponentPlanTests" vb-t4`
Expected: compile error `OpponentPlan` not found.

- [ ] **Step 3: Implement**

`Assets/_Project/Scripts/Gameplay/Volleyball/OpponentPlan.cs`:
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public enum AttackKind
    {
        Smash,
        Tip,
        Lob
    }

    public readonly struct OpponentStep
    {
        public readonly Vector2 ServeTarget;
        public readonly AttackKind Attack;
        public readonly float AttackDepth;
        public readonly float AttackLateral;
        public readonly bool WeakReceive;
        public readonly bool BlocksPlayerSmash;

        public OpponentStep(Vector2 serveTarget, AttackKind attack, float attackDepth, float attackLateral,
            bool weakReceive, bool blocksPlayerSmash)
        {
            ServeTarget = serveTarget;
            Attack = attack;
            AttackDepth = attackDepth;
            AttackLateral = attackLateral;
            WeakReceive = weakReceive;
            BlocksPlayerSmash = blocksPlayerSmash;
        }
    }

    // The AI's only source of variety: a fixed cycle that advances every time the AI sends the
    // ball over the net (serve or attack). Nothing here is random.
    public sealed class OpponentPlan
    {
        readonly OpponentStep[] steps;

        public OpponentPlan(IReadOnlyList<OpponentStep> steps)
        {
            if (steps == null || steps.Count == 0)
                throw new ArgumentException("An opponent plan needs at least one step.", nameof(steps));

            this.steps = new OpponentStep[steps.Count];
            for (int i = 0; i < steps.Count; i++)
                this.steps[i] = steps[i];
        }

        public int Count => steps.Length;
        public int Index { get; private set; }
        public OpponentStep Current => steps[Index];

        public void Advance() => Index = (Index + 1) % steps.Length;

        public static Vector2 AttackTarget(OpponentStep step, Vector2 playerPosition)
        {
            float away = playerPosition.y >= 0f ? -1f : 1f;
            return new Vector2(step.AttackDepth, away * step.AttackLateral);
        }

        public static OpponentPlan Authored() => new OpponentPlan(new[]
        {
            new OpponentStep(new Vector2(-7f, 0f), AttackKind.Smash, -7f, 3f, false, true),
            new OpponentStep(new Vector2(-3f, -2.5f), AttackKind.Tip, -1.5f, 2f, false, false),
            new OpponentStep(new Vector2(-7f, 3f), AttackKind.Lob, -7f, 3f, false, false),
            new OpponentStep(new Vector2(-7f, -3f), AttackKind.Smash, -4.5f, 3f, false, true),
            new OpponentStep(new Vector2(-5f, 0f), AttackKind.Lob, -5f, 0f, true, false),
            new OpponentStep(new Vector2(-3f, 2.5f), AttackKind.Smash, -6f, 2f, false, true),
            new OpponentStep(new Vector2(-7f, -3f), AttackKind.Tip, -1.2f, 0f, false, false),
            new OpponentStep(new Vector2(-5f, 0f), AttackKind.Lob, -7f, 0f, false, false)
        });
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.OpponentPlanTests" vb-t4`
Expected: `failed="0"`, 5 passed.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/OpponentPlan.cs* Assets/Tests/EditMode/Gameplay/Volleyball/OpponentPlanTests.cs*
git commit -m "feat(volleyball): add the authored opponent plan"
```

---

### Task 5: `ActionResolver`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/ActionResolver.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/ActionResolverTests.cs`

**Interfaces:**
- Consumes: `VolleyAthlete`, `BallFlight`, `RallyState`, `CourtSide`, `TimingWindows`
- Produces:
  - `enum BallState { Held, Toss, InPlay, Dead }`
  - `enum ActionKind { None, ServeToss, ServeHit, Receive, FreeBall, Smash, Dive, Block }`
  - `readonly struct ActionDecision`:
    - ctor `(ActionKind kind, TimingGrade grade, float offset)`
    - `Kind`, `Grade`, `Offset`, `Quality`, `IsTimed`, `static None`
  - `readonly struct ActionContext`:
    - ctor `(VolleyAthlete athlete, BallState ball, BallFlight flight, float flightTime, RallyState rally, CourtSide server, bool opponentSmashTell, Vector2 opponentAim)`
  - `static class ActionResolver`:
    - consts `Reach=1`, `ReceiveContactHeight=1`, `SmashContactHeight=2.6`, `SmashMinHeight=2.2`, `SmashNetDistance=3`, `DiveMinDistance=1`, `DiveMaxDistance=2.2`, `BlockNetDistance=1.2`, `BlockLateral=1`
    - `Resolve(in ActionContext) → ActionDecision`

- [ ] **Step 1: Write the failing test**

`Assets/Tests/EditMode/Gameplay/Volleyball/ActionResolverTests.cs`:
```csharp
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class ActionResolverTests
    {
        static VolleyAthlete PlayerAt(Vector2 position)
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(position);
            return athlete;
        }

        static RallyState PlayerPossession(int touches)
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Opponent);
            rally.RegisterServe(CourtSide.Opponent);
            for (int i = 0; i < touches; i++)
                rally.RegisterTouch(CourtSide.Player, false);
            return rally;
        }

        static ActionDecision Resolve(VolleyAthlete athlete, BallState ball, BallFlight flight, float time,
            RallyState rally, CourtSide server = CourtSide.Opponent, bool tell = false, Vector2 aim = default) =>
            ActionResolver.Resolve(new ActionContext(athlete, ball, flight, time, rally, server, tell, aim));

        static BallFlight OpponentServe() =>
            new BallFlight(new Vector2(8.5f, 0f), 3.2f, new Vector2(-5f, 0f), 4.5f, CourtSide.Opponent);

        [Test]
        public void HeldBallTossesOnlyForTheServer()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            VolleyAthlete player = PlayerAt(new Vector2(-8.5f, 0f));
            Assert.That(Resolve(player, BallState.Held, null, 0f, rally, CourtSide.Player).Kind,
                Is.EqualTo(ActionKind.ServeToss));
            Assert.That(Resolve(player, BallState.Held, null, 0f, rally, CourtSide.Opponent).Kind,
                Is.EqualTo(ActionKind.None));
        }

        [TestCase(.1f, TimingGrade.Perfect)]
        [TestCase(.15f, TimingGrade.Good)]
        [TestCase(-.25f, TimingGrade.Late)]
        public void ServeHitIsGradedAgainstTheTossApex(float offset, TimingGrade expected)
        {
            var toss = new BallFlight(new Vector2(-8.5f, 0f), 1.2f, new Vector2(-8.5f, 0f), 3.2f, CourtSide.Player);
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            ActionDecision d = Resolve(PlayerAt(new Vector2(-8.5f, 0f)), BallState.Toss, toss,
                toss.ApexTime + offset, rally, CourtSide.Player);
            Assert.That(d.Kind, Is.EqualTo(ActionKind.ServeHit));
            Assert.That(d.Grade, Is.EqualTo(expected));
            Assert.That(d.IsTimed, Is.True);
        }

        [Test]
        public void ServeHitOutsideTheWindowDoesNothing()
        {
            var toss = new BallFlight(new Vector2(-8.5f, 0f), 1.2f, new Vector2(-8.5f, 0f), 3.2f, CourtSide.Player);
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            Assert.That(Resolve(PlayerAt(new Vector2(-8.5f, 0f)), BallState.Toss, toss, toss.ApexTime + .35f,
                rally, CourtSide.Player).Kind, Is.EqualTo(ActionKind.None));
        }

        [TestCase(0f, TimingGrade.Perfect)]
        [TestCase(.15f, TimingGrade.Good)]
        [TestCase(-.25f, TimingGrade.Late)]
        public void ReceiveIsGradedAgainstTheOneMetreContact(float offset, TimingGrade expected)
        {
            BallFlight serve = OpponentServe();
            float ideal = serve.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            ActionDecision d = Resolve(PlayerAt(serve.GroundAt(ideal)), BallState.InPlay, serve, ideal + offset,
                PlayerPossession(0));
            Assert.That(d.Kind, Is.EqualTo(ActionKind.Receive));
            Assert.That(d.Grade, Is.EqualTo(expected));
        }

        [Test]
        public void ReceiveNeedsTheAthleteWithinReachOfTheContactPoint()
        {
            BallFlight serve = OpponentServe();
            float ideal = serve.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            VolleyAthlete far = PlayerAt(serve.GroundAt(ideal) + new Vector2(0f, 1.5f));
            Assert.That(Resolve(far, BallState.InPlay, serve, ideal, PlayerPossession(0)).Kind,
                Is.EqualTo(ActionKind.None));
        }

        [Test]
        public void PressTooLateIsNotAReceive()
        {
            BallFlight serve = OpponentServe();
            float ideal = serve.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            Assert.That(Resolve(PlayerAt(serve.GroundAt(ideal)), BallState.InPlay, serve, ideal + .35f,
                PlayerPossession(0)).Kind, Is.Not.EqualTo(ActionKind.Receive));
        }

        [Test]
        public void DiveReachesALowBallLandingJustOutOfReach()
        {
            BallFlight serve = OpponentServe();
            float ideal = serve.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            VolleyAthlete athlete = PlayerAt(serve.Target + new Vector2(0f, 1.6f));
            ActionDecision d = Resolve(athlete, BallState.InPlay, serve, ideal + .05f, PlayerPossession(0));
            Assert.That(d.Kind, Is.EqualTo(ActionKind.Dive));
            Assert.That(d.Grade, Is.EqualTo(TimingGrade.Late));
        }

        [Test]
        public void SecondTouchNearTheNetSmashes()
        {
            var set = new BallFlight(new Vector2(-5f, 0f), 1f, new Vector2(-1.5f, 0f), 4f, CourtSide.Player);
            float ideal = set.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            ActionDecision d = Resolve(PlayerAt(set.GroundAt(ideal)), BallState.InPlay, set, ideal, PlayerPossession(1));
            Assert.That(d.Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(d.Grade, Is.EqualTo(TimingGrade.Perfect));
        }

        [Test]
        public void FirstTouchCannotSmash()
        {
            var set = new BallFlight(new Vector2(-5f, 0f), 1f, new Vector2(-1.5f, 0f), 4f, CourtSide.Player);
            float ideal = set.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            Assert.That(Resolve(PlayerAt(set.GroundAt(ideal)), BallState.InPlay, set, ideal, PlayerPossession(0)).Kind,
                Is.EqualTo(ActionKind.Receive));
        }

        [Test]
        public void FarFromTheNetTheSecondTouchIsAReceive()
        {
            var set = new BallFlight(new Vector2(-6f, 0f), 1f, new Vector2(-4.5f, 0f), 4f, CourtSide.Player);
            float ideal = set.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            Assert.That(Resolve(PlayerAt(set.GroundAt(ideal)), BallState.InPlay, set, ideal, PlayerPossession(1)).Kind,
                Is.EqualTo(ActionKind.Receive));
        }

        [Test]
        public void ThirdTouchAwayFromTheNetIsAFreeBall()
        {
            var set = new BallFlight(new Vector2(-6f, 0f), 1f, new Vector2(-4.5f, 0f), 4f, CourtSide.Player);
            float ideal = set.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            ActionDecision d = Resolve(PlayerAt(set.GroundAt(ideal)), BallState.InPlay, set, ideal, PlayerPossession(2));
            Assert.That(d.Kind, Is.EqualTo(ActionKind.FreeBall));
            Assert.That(d.Grade, Is.EqualTo(TimingGrade.Perfect));
        }

        [Test]
        public void BlockNeedsTheTellTheNetAndTheLine()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            rally.RegisterServe(CourtSide.Player);
            var attack = new BallFlight(new Vector2(4f, 0f), 1f, new Vector2(1.5f, 0f), 4f, CourtSide.Opponent);
            var aim = new Vector2(-7f, -3f);

            Assert.That(Resolve(PlayerAt(new Vector2(-.8f, -3f)), BallState.InPlay, attack, .5f, rally,
                CourtSide.Player, true, aim).Kind, Is.EqualTo(ActionKind.Block));
            Assert.That(Resolve(PlayerAt(new Vector2(-.8f, 0f)), BallState.InPlay, attack, .5f, rally,
                CourtSide.Player, true, aim).Kind, Is.EqualTo(ActionKind.None));
            Assert.That(Resolve(PlayerAt(new Vector2(-2.5f, -3f)), BallState.InPlay, attack, .5f, rally,
                CourtSide.Player, true, aim).Kind, Is.EqualTo(ActionKind.None));
            Assert.That(Resolve(PlayerAt(new Vector2(-.8f, -3f)), BallState.InPlay, attack, .5f, rally,
                CourtSide.Player, false, aim).Kind, Is.EqualTo(ActionKind.None));
        }

        [Test]
        public void LockedAthleteCannotAct()
        {
            BallFlight serve = OpponentServe();
            float ideal = serve.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            VolleyAthlete athlete = PlayerAt(serve.GroundAt(ideal));
            athlete.BeginAction(AthleteAction.Dive, .8f);
            Assert.That(Resolve(athlete, BallState.InPlay, serve, ideal, PlayerPossession(0)).Kind,
                Is.EqualTo(ActionKind.None));
        }

        [Test]
        public void DeadBallIgnoresPresses()
        {
            BallFlight serve = OpponentServe();
            Assert.That(Resolve(PlayerAt(serve.Target), BallState.Dead, serve, 1f, PlayerPossession(0)).Kind,
                Is.EqualTo(ActionKind.None));
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.ActionResolverTests" vb-t5`
Expected: compile errors for `ActionResolver`, `ActionContext`, `BallState`.

- [ ] **Step 3: Implement**

`Assets/_Project/Scripts/Gameplay/Volleyball/ActionResolver.cs`:
```csharp
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public enum BallState
    {
        Held,
        Toss,
        InPlay,
        Dead
    }

    public enum ActionKind
    {
        None,
        ServeToss,
        ServeHit,
        Receive,
        FreeBall,
        Smash,
        Dive,
        Block
    }

    public readonly struct ActionDecision
    {
        public readonly ActionKind Kind;
        public readonly TimingGrade Grade;
        public readonly float Offset;

        public ActionDecision(ActionKind kind, TimingGrade grade, float offset)
        {
            Kind = kind;
            Grade = grade;
            Offset = offset;
        }

        public static ActionDecision None => new ActionDecision(ActionKind.None, TimingGrade.Miss, 0f);
        public float Quality => TimingWindows.Quality(Grade);

        // Timed presses feed the accuracy score; the toss and the block are not graded.
        public bool IsTimed => Kind == ActionKind.ServeHit || Kind == ActionKind.Receive ||
                               Kind == ActionKind.FreeBall || Kind == ActionKind.Smash || Kind == ActionKind.Dive;
    }

    public readonly struct ActionContext
    {
        public readonly VolleyAthlete Athlete;
        public readonly BallState Ball;
        public readonly BallFlight Flight;
        public readonly float FlightTime;
        public readonly RallyState Rally;
        public readonly CourtSide Server;
        public readonly bool OpponentSmashTell;
        public readonly Vector2 OpponentAim;

        public ActionContext(VolleyAthlete athlete, BallState ball, BallFlight flight, float flightTime,
            RallyState rally, CourtSide server, bool opponentSmashTell, Vector2 opponentAim)
        {
            Athlete = athlete;
            Ball = ball;
            Flight = flight;
            FlightTime = flightTime;
            Rally = rally;
            Server = server;
            OpponentSmashTell = opponentSmashTell;
            OpponentAim = opponentAim;
        }
    }

    // The single context-sensitive button. Reach is measured to the contact point, the ball's
    // ground position at the ideal moment, so positioning and timing are judged separately.
    public static class ActionResolver
    {
        public const float Reach = 1f;
        public const float ReceiveContactHeight = 1f;
        public const float SmashContactHeight = 2.6f;
        public const float SmashMinHeight = 2.2f;
        public const float SmashNetDistance = 3f;
        public const float DiveMinDistance = 1f;
        public const float DiveMaxDistance = 2.2f;
        public const float BlockNetDistance = 1.2f;
        public const float BlockLateral = 1f;

        public static ActionDecision Resolve(in ActionContext context)
        {
            VolleyAthlete athlete = context.Athlete;
            if (athlete == null || athlete.IsLocked)
                return ActionDecision.None;

            switch (context.Ball)
            {
                case BallState.Held:
                    return context.Server == athlete.Side
                        ? new ActionDecision(ActionKind.ServeToss, TimingGrade.Miss, 0f)
                        : ActionDecision.None;
                case BallState.Toss:
                    return ResolveServeHit(context);
                case BallState.InPlay:
                    break;
                default:
                    return ActionDecision.None;
            }

            if (context.Flight == null || context.Rally == null)
                return ActionDecision.None;
            if (context.Rally.Possession != athlete.Side)
                return ResolveBlock(context);

            return ResolveTouch(context);
        }

        static ActionDecision ResolveServeHit(in ActionContext context)
        {
            if (context.Server != context.Athlete.Side || context.Flight == null)
                return ActionDecision.None;

            float offset = context.FlightTime - context.Flight.ApexTime;
            TimingGrade grade = TimingWindows.Grade(offset, TimingWindows.ServePerfect);
            return grade == TimingGrade.Miss ? ActionDecision.None : new ActionDecision(ActionKind.ServeHit, grade, offset);
        }

        static ActionDecision ResolveTouch(in ActionContext context)
        {
            VolleyAthlete athlete = context.Athlete;
            BallFlight flight = context.Flight;
            int touches = context.Rally.Touches;
            float time = context.FlightTime;

            if (touches >= 1 && touches <= 2 && Mathf.Abs(athlete.Position.x) <= SmashNetDistance &&
                flight.ApexHeight > SmashContactHeight)
            {
                float smashIdeal = flight.TimeAtHeightDescending(SmashContactHeight);
                float smashOffset = time - smashIdeal;
                TimingGrade smashGrade = TimingWindows.Grade(smashOffset);
                if (smashGrade != TimingGrade.Miss && flight.HeightAt(time) >= SmashMinHeight &&
                    Vector2.Distance(athlete.Position, flight.GroundAt(smashIdeal)) <= Reach)
                    return new ActionDecision(ActionKind.Smash, smashGrade, smashOffset);
            }

            if (touches > 2)
                return ActionDecision.None;

            float ideal = flight.TimeAtHeightDescending(ReceiveContactHeight);
            float offset = time - ideal;
            TimingGrade grade = TimingWindows.Grade(offset);
            if (grade != TimingGrade.Miss && Vector2.Distance(athlete.Position, flight.GroundAt(ideal)) <= Reach)
                return new ActionDecision(touches == 2 ? ActionKind.FreeBall : ActionKind.Receive, grade, offset);

            float landingDistance = Vector2.Distance(athlete.Position, flight.Target);
            if (flight.IsDescending(time) && flight.HeightAt(time) < ReceiveContactHeight &&
                landingDistance > DiveMinDistance && landingDistance <= DiveMaxDistance)
                return new ActionDecision(ActionKind.Dive, TimingGrade.Late, offset);

            return ActionDecision.None;
        }

        static ActionDecision ResolveBlock(in ActionContext context)
        {
            Vector2 position = context.Athlete.Position;
            bool lined = context.OpponentSmashTell &&
                         Mathf.Abs(position.x) <= BlockNetDistance &&
                         Mathf.Abs(position.y - context.OpponentAim.y) <= BlockLateral;
            return lined ? new ActionDecision(ActionKind.Block, TimingGrade.Miss, 0f) : ActionDecision.None;
        }
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.ActionResolverTests" vb-t5`
Expected: `failed="0"`, 18 passed.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/ActionResolver.cs* Assets/Tests/EditMode/Gameplay/Volleyball/ActionResolverTests.cs*
git commit -m "feat(volleyball): resolve the context-sensitive action button"
```

---

### Task 6: `VolleyballMatch` (serve, player actions, points, clock, result)

This task builds the match with a **passive** AI stub in `VolleyballMatch.Opponent.cs`: the AI serves but never plays the ball. Task 7 replaces that file with the real AI. All tests here use `new OpponentTuning(0f, .25f)` (an AI that cannot move) or no rally play, so they keep passing after Task 7.

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.Opponent.cs` (stub)
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/MatchDriver.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/VolleyballMatchTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 1–5; `MinigameResult`, `ScoreUtil` (namespace `KMA.Gameplay`)
- Produces:
  - `readonly struct OpponentTuning`:
    - ctor `(float speedFactor, float reactionDelay)`, `static Default` (.85, .25)
  - `sealed partial class VolleyballMatch`:
    - ctor `(OpponentPlan plan = null, OpponentTuning? tuning = null)`
    - consts `PointsToWin=5`, `TimeLimit=120`, `PointPause=1.2`, `OpponentServeDelay=1`, `PlayerSpeed=5`, `TossStartHeight=1.2`, `TossApexHeight=3.2`, `SetApexHeight=4`, `FreeBallApexHeight=4.5`, `ReceiveSeconds=.35`, `SmashSeconds=.45`, `BlockSeconds=.6`, `SmashRise=.2`
    - static spots `PlayerServeSpot`, `OpponentServeSpot`, `PlayerReadySpot`, `OpponentReadySpot`, `PlayerSetSpot`, `OpponentSetSpot`
    - state props `Player`, `Opponent`, `Rally`, `Plan`, `BallState`, `Flight`, `FlightTime`, `Server`, `PlayerPoints`, `OpponentPoints`, `Elapsed`, `TimeRemaining`, `IsOver`, `Winners`, `TimedPresses`, `QualitySum`, `LastDecision`, `OpponentSmashTell`, `OpponentAim`, `BallGround`, `BallHeight`
    - events `PointScored(CourtSide)`, `PlayerActed(ActionDecision)`, `Completed()`
    - methods `SetMove(Vector2)`, `PressAction() → ActionDecision`, `Tick(float)`, `TryGetPlayerContactCue(out float secondsToIdeal) → bool`, `BuildResult() → MinigameResult`, `static AimAtOpponent(Vector2 stick) → Vector2`
    - test seams `SetScoreForTest(int, int)`, `ForceServerForTest(CourtSide)`
    - private hooks used by the partial: `Launch(Vector2 target, float apex, CourtSide hitter)`, `AwardPoint(CourtSide winner, bool winnerShot)`
  - AI hooks in `VolleyballMatch.Opponent.cs`: `void ResetOpponentState()`, `void TickOpponent(float dt)`, `bool OpponentBlocks(Vector2 target)`
  - Test helper `MatchDriver`: `Step`, `Advance(m, seconds)`, `AdvanceUntil(m, Func<bool>, maxSeconds) → bool`, `AdvanceToFlightTime(m, t)`, `ServePerfectAce(m) → ActionDecision`, `ServeGood(m) → ActionDecision`

- [ ] **Step 1: Write the test helper and failing tests**

`Assets/Tests/EditMode/Gameplay/Volleyball/MatchDriver.cs`:
```csharp
using System;
using KMA.Gameplay.Volleyball;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    static class MatchDriver
    {
        public const float Step = 1f / 120f;

        public static void Advance(VolleyballMatch match, float seconds)
        {
            float elapsed = 0f;
            while (seconds - elapsed > 1e-6f)
            {
                float step = Mathf.Min(Step, seconds - elapsed);
                match.Tick(step);
                elapsed += step;
            }
        }

        public static bool AdvanceUntil(VolleyballMatch match, Func<bool> condition, float maxSeconds)
        {
            for (float elapsed = 0f; elapsed < maxSeconds; elapsed += Step)
            {
                if (condition())
                    return true;
                match.Tick(Step);
            }

            return condition();
        }

        public static void AdvanceToFlightTime(VolleyballMatch match, float flightTime) =>
            Advance(match, flightTime - match.FlightTime);

        // A perfect serve aimed at the far deep corner (7, 3), out of reach of an AI that cannot
        // move from its ready spot (5, 0). Vector2.up keeps the aim exact: a diagonal stick is
        // clamped to length 1 and would aim at (7, 2.12).
        public static ActionDecision ServePerfectAce(VolleyballMatch match)
        {
            match.SetMove(Vector2.zero);
            match.PressAction();
            AdvanceToFlightTime(match, match.Flight.ApexTime);
            match.SetMove(Vector2.up);
            ActionDecision decision = match.PressAction();
            match.SetMove(Vector2.zero);
            return decision;
        }

        // A GOOD serve: slow, high, to the middle of the AI's half at (5, 0).
        public static ActionDecision ServeGood(VolleyballMatch match)
        {
            match.SetMove(Vector2.zero);
            match.PressAction();
            AdvanceToFlightTime(match, match.Flight.ApexTime + .15f);
            return match.PressAction();
        }
    }
}
```

`Assets/Tests/EditMode/Gameplay/Volleyball/VolleyballMatchTests.cs`:
```csharp
using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballMatchTests
    {
        static VolleyballMatch FrozenOpponentMatch() => new VolleyballMatch(null, new OpponentTuning(0f, .25f));

        [Test]
        public void MatchStartsWithThePlayerHoldingTheServe()
        {
            var match = new VolleyballMatch();
            Assert.That(match.Server, Is.EqualTo(CourtSide.Player));
            Assert.That(match.BallState, Is.EqualTo(BallState.Held));
            Assert.That(match.Player.Position, Is.EqualTo(VolleyballMatch.PlayerServeSpot));
            Assert.That(match.Opponent.Position, Is.EqualTo(VolleyballMatch.OpponentReadySpot));
            Assert.That(match.BallGround, Is.EqualTo(VolleyballMatch.PlayerServeSpot));
            Assert.That(match.BallHeight, Is.EqualTo(VolleyballMatch.TossStartHeight));
            Assert.That(match.TimeRemaining, Is.EqualTo(VolleyballMatch.TimeLimit));
        }

        [Test]
        public void ServerMovesOnlyAlongTheBaseline()
        {
            var match = new VolleyballMatch();
            match.SetMove(new Vector2(1f, 1f));
            MatchDriver.Advance(match, .2f);
            Assert.That(match.Player.Position.x, Is.EqualTo(VolleyballMatch.PlayerServeSpot.x));
            Assert.That(match.Player.Position.y, Is.GreaterThan(0f));
        }

        [Test]
        public void PerfectServeFliesFastToTheAimedSpot()
        {
            var match = FrozenOpponentMatch();
            match.PressAction();
            Assert.That(match.BallState, Is.EqualTo(BallState.Toss));
            MatchDriver.AdvanceToFlightTime(match, match.Flight.ApexTime);
            match.SetMove(Vector2.left);
            ActionDecision serve = match.PressAction();

            Assert.That(serve.Kind, Is.EqualTo(ActionKind.ServeHit));
            Assert.That(serve.Grade, Is.EqualTo(TimingGrade.Perfect));
            Assert.That(match.BallState, Is.EqualTo(BallState.InPlay));
            Assert.That(match.Flight.Target, Is.EqualTo(new Vector2(3f, 0f)));
            Assert.That(match.Flight.ClearsNet, Is.True);
            Assert.That(match.Rally.Possession, Is.EqualTo(CourtSide.Opponent));
            Assert.That(match.TimedPresses, Is.EqualTo(1));
            Assert.That(match.QualitySum, Is.EqualTo(1f));
        }

        [Test]
        public void GoodServeIsTheSafeHighServe()
        {
            var match = FrozenOpponentMatch();
            ActionDecision serve = MatchDriver.ServeGood(match);
            Assert.That(serve.Grade, Is.EqualTo(TimingGrade.Good));
            Assert.That(match.Flight.Target, Is.EqualTo(new Vector2(5f, 0f)));
        }

        [Test]
        public void DroppedTossGivesThePointAndServeAway()
        {
            var match = new VolleyballMatch();
            match.PressAction();
            MatchDriver.Advance(match, match.Flight.Duration + .05f);
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
            Assert.That(match.Server, Is.EqualTo(CourtSide.Opponent));
            Assert.That(match.BallState, Is.EqualTo(BallState.Dead));
        }

        [Test]
        public void UnreturnedServeScoresForTheServer()
        {
            var match = FrozenOpponentMatch();
            Assert.That(MatchDriver.ServePerfectAce(match).Grade, Is.EqualTo(TimingGrade.Perfect));
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.PlayerPoints, Is.EqualTo(1));
            Assert.That(match.Winners, Is.Zero);
            Assert.That(match.Server, Is.EqualTo(CourtSide.Player));
        }

        [Test]
        public void OpponentServesItsPlanTargetAfterTheDelay()
        {
            var match = FrozenOpponentMatch();
            match.ForceServerForTest(CourtSide.Opponent);
            MatchDriver.Advance(match, VolleyballMatch.OpponentServeDelay + .01f);
            Assert.That(match.BallState, Is.EqualTo(BallState.Toss));
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 2f), Is.True);
            Assert.That(match.Flight.Target, Is.EqualTo(new Vector2(-7f, 0f)));
            Assert.That(match.Flight.Hitter, Is.EqualTo(CourtSide.Opponent));
            Assert.That(match.Plan.Index, Is.EqualTo(1));

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
        }

        [Test]
        public void NextPointStartsAfterThePause()
        {
            var match = FrozenOpponentMatch();
            match.ForceServerForTest(CourtSide.Opponent);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 6f), Is.True);
            MatchDriver.Advance(match, VolleyballMatch.PointPause + .01f);
            Assert.That(match.BallState, Is.EqualTo(BallState.Held));
            Assert.That(match.Opponent.Position, Is.EqualTo(VolleyballMatch.OpponentServeSpot));
            Assert.That(match.Player.Position, Is.EqualTo(VolleyballMatch.PlayerReadySpot));
        }

        [Test]
        public void ReceiveThenSmashWinsThePointAsAWinner()
        {
            var match = FrozenOpponentMatch();
            match.ForceServerForTest(CourtSide.Opponent);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 3f), Is.True);

            float receiveIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            Vector2 contact = match.Flight.GroundAt(receiveIdeal);
            match.Player.PlaceAt(contact);
            MatchDriver.AdvanceToFlightTime(match, receiveIdeal);
            ActionDecision receive = match.PressAction();
            Assert.That(receive.Kind, Is.EqualTo(ActionKind.Receive));
            Assert.That(receive.Grade, Is.EqualTo(TimingGrade.Perfect));
            Assert.That(match.Flight.Target, Is.EqualTo(new Vector2(-1.5f, Mathf.Clamp(contact.y, -3f, 3f))));
            Assert.That(match.Rally.Touches, Is.EqualTo(1));

            float smashIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(smashIdeal));
            MatchDriver.AdvanceToFlightTime(match, smashIdeal);
            match.SetMove(Vector2.right);
            ActionDecision smash = match.PressAction();
            match.SetMove(Vector2.zero);
            Assert.That(smash.Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(smash.Grade, Is.EqualTo(TimingGrade.Perfect));
            Assert.That(match.Flight.Target, Is.EqualTo(new Vector2(7f, 0f)));
            Assert.That(match.Rally.Possession, Is.EqualTo(CourtSide.Opponent));

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.PlayerPoints, Is.EqualTo(1));
            Assert.That(match.Winners, Is.EqualTo(1));
        }

        [Test]
        public void EarlyLateSmashDumpsTheBallOnTheOwnSide()
        {
            var match = FrozenOpponentMatch();
            match.ForceServerForTest(CourtSide.Opponent);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 3f), Is.True);
            float receiveIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(receiveIdeal));
            MatchDriver.AdvanceToFlightTime(match, receiveIdeal);
            match.PressAction();

            float smashIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(smashIdeal));
            MatchDriver.AdvanceToFlightTime(match, smashIdeal - .25f);
            ActionDecision smash = match.PressAction();
            Assert.That(smash.Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(smash.Grade, Is.EqualTo(TimingGrade.Late));
            Assert.That(CourtSpace.SideOf(match.Flight.Target), Is.EqualTo(CourtSide.Player));

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
        }

        [Test]
        public void FirstToFiveCompletesOnceWithTheSpecScore()
        {
            var match = FrozenOpponentMatch();
            int completions = 0;
            match.Completed += () => completions++;

            for (int point = 0; point < VolleyballMatch.PointsToWin; point++)
            {
                Assert.That(match.Server, Is.EqualTo(CourtSide.Player), $"point {point + 1}");
                Assert.That(MatchDriver.ServePerfectAce(match).Grade, Is.EqualTo(TimingGrade.Perfect));
                Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
                if (!match.IsOver)
                    MatchDriver.Advance(match, VolleyballMatch.PointPause + .02f);
            }

            Assert.That(match.PlayerPoints, Is.EqualTo(5));
            Assert.That(match.IsOver, Is.True);
            Assert.That(completions, Is.EqualTo(1));

            MinigameResult result = match.BuildResult();
            Assert.That(result.Pass, Is.True);
            Assert.That(result.Score, Is.EqualTo(9f));
            Assert.That(result.Rank, Is.EqualTo(Rank.S));
        }

        [Test]
        public void TimeCapWithALeadPasses()
        {
            var match = new VolleyballMatch();
            int completions = 0;
            match.Completed += () => completions++;
            match.SetScoreForTest(2, 1);
            MatchDriver.Advance(match, VolleyballMatch.TimeLimit + .5f);

            Assert.That(match.IsOver, Is.True);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(match.TimeRemaining, Is.Zero);
            Assert.That(match.BuildResult().Pass, Is.True);
        }

        [Test]
        public void TimeCapWithATieFails()
        {
            var match = new VolleyballMatch();
            match.SetScoreForTest(2, 2);
            MatchDriver.Advance(match, VolleyballMatch.TimeLimit + .5f);
            MinigameResult result = match.BuildResult();
            Assert.That(result.Pass, Is.False);
            Assert.That(result.Score, Is.Zero);
            Assert.That(result.Rank, Is.EqualTo(Rank.F));
        }

        [Test]
        public void FinishedMatchIgnoresInputAndTime()
        {
            var match = new VolleyballMatch();
            int completions = 0;
            match.Completed += () => completions++;
            match.SetScoreForTest(1, 0);
            MatchDriver.Advance(match, VolleyballMatch.TimeLimit + .5f);
            float elapsed = match.Elapsed;

            Assert.That(match.PressAction().Kind, Is.EqualTo(ActionKind.None));
            match.Tick(1f);
            Assert.That(match.Elapsed, Is.EqualTo(elapsed));
            Assert.That(completions, Is.EqualTo(1));
        }

        [Test]
        public void ZeroDeltaTimeChangesNothing()
        {
            var match = new VolleyballMatch();
            match.Tick(0f);
            Assert.That(match.Elapsed, Is.Zero);
        }

        [Test]
        public void ContactCueTracksTheTossApex()
        {
            var match = new VolleyballMatch();
            Assert.That(match.TryGetPlayerContactCue(out _), Is.False);
            match.PressAction();
            Assert.That(match.TryGetPlayerContactCue(out float seconds), Is.True);
            Assert.That(seconds, Is.EqualTo(match.Flight.ApexTime).Within(1e-4f));
        }

        [Test]
        public void PlayerActedReportsEveryResolvedPress()
        {
            var match = FrozenOpponentMatch();
            int acted = 0;
            match.PlayerActed += _ => acted++;
            match.PressAction();
            MatchDriver.Advance(match, .05f);
            match.PressAction();
            Assert.That(acted, Is.EqualTo(1));
        }

        [Test]
        public void AimMapsTheStickOntoTheOpponentHalf()
        {
            Assert.That(VolleyballMatch.AimAtOpponent(Vector2.zero), Is.EqualTo(new Vector2(7f, 0f)));
            Assert.That(VolleyballMatch.AimAtOpponent(new Vector2(-1f, 1f)), Is.EqualTo(new Vector2(3f, 3f)));
            Assert.That(VolleyballMatch.AimAtOpponent(new Vector2(1f, -1f)), Is.EqualTo(new Vector2(7f, -3f)));
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.VolleyballMatchTests" vb-t6`
Expected: compile error `VolleyballMatch` not found.

- [ ] **Step 3: Implement the match**

`Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.cs`:
```csharp
using System;
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public readonly struct OpponentTuning
    {
        public readonly float SpeedFactor;
        public readonly float ReactionDelay;

        public OpponentTuning(float speedFactor, float reactionDelay)
        {
            SpeedFactor = Mathf.Max(0f, speedFactor);
            ReactionDelay = Mathf.Max(0f, reactionDelay);
        }

        public static OpponentTuning Default => new OpponentTuning(.85f, .25f);
    }

    // The whole game as plain state: serve, rally, points, clock and result. Advanced only by
    // Tick, so pausing (dt 0) freezes it and every run with the same inputs is identical.
    public sealed partial class VolleyballMatch
    {
        public const int PointsToWin = 5;
        public const float TimeLimit = 120f;
        public const float PointPause = 1.2f;
        public const float OpponentServeDelay = 1f;
        public const float PlayerSpeed = 5f;
        public const float TossStartHeight = 1.2f;
        public const float TossApexHeight = 3.2f;
        public const float SetApexHeight = 4f;
        public const float FreeBallApexHeight = 4.5f;
        public const float ReceiveSeconds = .35f;
        public const float SmashSeconds = .45f;
        public const float BlockSeconds = .6f;
        public const float SmashRise = .2f;
        const float ServeSeconds = .4f;
        const float DiveSeconds = .8f;
        const float DiveLunge = 1.2f;
        const float DiveApexHeight = 3f;
        // .8 keeps a PERFECT serve to the short line (x 3) clear of the net with margin.
        const float PerfectServeRise = .8f;
        const float NormalServeApexHeight = 5f;
        const float OpponentServeApexHeight = 4.5f;

        public static readonly Vector2 PlayerServeSpot = new Vector2(-8.5f, 0f);
        public static readonly Vector2 OpponentServeSpot = new Vector2(8.5f, 0f);
        public static readonly Vector2 PlayerReadySpot = new Vector2(-5f, 0f);
        public static readonly Vector2 OpponentReadySpot = new Vector2(5f, 0f);
        public static readonly Vector2 PlayerSetSpot = new Vector2(-1.5f, 0f);
        public static readonly Vector2 OpponentSetSpot = new Vector2(1.5f, 0f);
        static readonly Vector2 NormalServeTarget = new Vector2(5f, 0f);
        static readonly Vector2 FreeBallTarget = new Vector2(5f, 0f);

        readonly OpponentTuning tuning;
        Vector2 move;
        float serveTimer;
        float pauseLeft;
        bool lastHitWasPlayerSmash;

        public VolleyballMatch(OpponentPlan plan = null, OpponentTuning? tuning = null)
        {
            this.tuning = tuning ?? OpponentTuning.Default;
            Plan = plan ?? OpponentPlan.Authored();
            Player = new VolleyAthlete(CourtSide.Player, PlayerSpeed);
            Opponent = new VolleyAthlete(CourtSide.Opponent, PlayerSpeed * this.tuning.SpeedFactor);
            Server = CourtSide.Player;
            BeginPoint();
        }

        public event Action<CourtSide> PointScored;
        public event Action<ActionDecision> PlayerActed;
        public event Action Completed;

        public VolleyAthlete Player { get; }
        public VolleyAthlete Opponent { get; }
        public RallyState Rally { get; } = new RallyState();
        public OpponentPlan Plan { get; }
        public BallState BallState { get; private set; }
        public BallFlight Flight { get; private set; }
        public float FlightTime { get; private set; }
        public CourtSide Server { get; private set; }
        public int PlayerPoints { get; private set; }
        public int OpponentPoints { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsOver { get; private set; }
        public int Winners { get; private set; }
        public int TimedPresses { get; private set; }
        public float QualitySum { get; private set; }
        public ActionDecision LastDecision { get; private set; }
        public bool OpponentSmashTell { get; private set; }
        public Vector2 OpponentAim { get; private set; }
        public float TimeRemaining => Mathf.Max(0f, TimeLimit - Elapsed);

        public Vector2 BallGround => BallState == BallState.Held || Flight == null
            ? ServerAthlete.Position
            : Flight.GroundAt(FlightTime);

        public float BallHeight => BallState == BallState.Held || Flight == null
            ? TossStartHeight
            : Flight.HeightAt(FlightTime);

        VolleyAthlete ServerAthlete => Server == CourtSide.Player ? Player : Opponent;

        public static Vector2 AimAtOpponent(Vector2 stick) =>
            new Vector2(stick.x < -.3f ? 3f : 7f, Mathf.Clamp(stick.y, -1f, 1f) * 3f);

        public void SetMove(Vector2 stick) => move = Vector2.ClampMagnitude(stick, 1f);

        public ActionDecision PressAction()
        {
            if (IsOver)
                return ActionDecision.None;

            ActionDecision decision = ActionResolver.Resolve(new ActionContext(Player, BallState, Flight, FlightTime,
                Rally, Server, OpponentSmashTell, OpponentAim));
            if (decision.Kind == ActionKind.None)
                return decision;

            // Count the press before applying it: applying can end the match, and the result
            // must already include this press.
            if (decision.IsTimed)
            {
                TimedPresses++;
                QualitySum += decision.Quality;
            }

            LastDecision = decision;
            switch (decision.Kind)
            {
                case ActionKind.ServeToss:
                    StartToss();
                    break;
                case ActionKind.ServeHit:
                    PlayerServe(decision);
                    break;
                case ActionKind.Block:
                    Player.BeginAction(AthleteAction.Block, BlockSeconds);
                    break;
                default:
                    PlayerHit(decision);
                    break;
            }

            PlayerActed?.Invoke(decision);
            return decision;
        }

        public void Tick(float deltaTime)
        {
            if (IsOver || deltaTime <= 0f)
                return;

            Elapsed += deltaTime;
            Player.Tick(deltaTime);
            Opponent.Tick(deltaTime);

            switch (BallState)
            {
                case BallState.Held:
                    if (Server == CourtSide.Player)
                    {
                        Player.Move(new Vector2(0f, move.y), deltaTime);
                    }
                    else
                    {
                        Player.Move(move, deltaTime);
                        serveTimer += deltaTime;
                        if (serveTimer >= OpponentServeDelay)
                            StartToss();
                    }
                    break;
                case BallState.Toss:
                    FlightTime += deltaTime;
                    if (Server == CourtSide.Opponent)
                    {
                        Player.Move(move, deltaTime);
                        if (FlightTime >= Flight.ApexTime)
                            OpponentServe();
                    }
                    else if (FlightTime >= Flight.Duration)
                    {
                        AwardPoint(CourtSide.Opponent, false);
                    }
                    break;
                case BallState.InPlay:
                    FlightTime += deltaTime;
                    Player.Move(move, deltaTime);
                    TickOpponent(deltaTime);
                    if (BallState == BallState.InPlay)
                        ResolveBall();
                    break;
                case BallState.Dead:
                    FlightTime += deltaTime;
                    Player.Move(move, deltaTime);
                    pauseLeft -= deltaTime;
                    if (pauseLeft <= 0f && !IsOver)
                        BeginPoint();
                    break;
            }

            if (!IsOver && Elapsed >= TimeLimit)
                Complete();
        }

        public bool TryGetPlayerContactCue(out float secondsToIdeal)
        {
            secondsToIdeal = 0f;
            if (Flight == null)
                return false;

            float ideal;
            if (BallState == BallState.Toss && Server == CourtSide.Player)
            {
                ideal = Flight.ApexTime;
            }
            else if (BallState == BallState.InPlay && Rally.Possession == CourtSide.Player)
            {
                bool smash = Rally.Touches >= 1 &&
                             Mathf.Abs(Player.Position.x) <= ActionResolver.SmashNetDistance &&
                             Flight.ApexHeight > ActionResolver.SmashContactHeight;
                ideal = Flight.TimeAtHeightDescending(smash
                    ? ActionResolver.SmashContactHeight
                    : ActionResolver.ReceiveContactHeight);
            }
            else
            {
                return false;
            }

            secondsToIdeal = ideal - FlightTime;
            return secondsToIdeal >= -TimingWindows.Late;
        }

        public MinigameResult BuildResult()
        {
            bool pass = IsOver && PlayerPoints > OpponentPoints;
            float accuracy = TimedPresses == 0 ? 0f : 2f * QualitySum / TimedPresses;
            float efficiency = 1f - OpponentPoints / (float)PointsToWin;
            float mastery = Mathf.Min(Winners, PointsToWin) / (float)PointsToWin;
            return ScoreUtil.Build(pass, accuracy, efficiency, mastery);
        }

        public void SetScoreForTest(int playerPoints, int opponentPoints)
        {
            PlayerPoints = Mathf.Max(0, playerPoints);
            OpponentPoints = Mathf.Max(0, opponentPoints);
        }

        public void ForceServerForTest(CourtSide server)
        {
            Server = server;
            BeginPoint();
        }

        void BeginPoint()
        {
            Rally.BeginServe(Server);
            BallState = BallState.Held;
            Flight = null;
            FlightTime = 0f;
            serveTimer = 0f;
            lastHitWasPlayerSmash = false;
            OpponentSmashTell = false;
            Player.PlaceAt(Server == CourtSide.Player ? PlayerServeSpot : PlayerReadySpot);
            Opponent.PlaceAt(Server == CourtSide.Opponent ? OpponentServeSpot : OpponentReadySpot);
            ResetOpponentState();
        }

        void StartToss()
        {
            VolleyAthlete server = ServerAthlete;
            Flight = new BallFlight(server.Position, TossStartHeight, server.Position, TossApexHeight, Server);
            FlightTime = 0f;
            BallState = BallState.Toss;
            server.BeginAction(AthleteAction.Serve, 0f);
        }

        void PlayerServe(ActionDecision decision)
        {
            bool perfect = decision.Grade == TimingGrade.Perfect;
            Vector2 target = perfect ? AimAtOpponent(move) : NormalServeTarget;
            float apex = perfect ? BallHeight + PerfectServeRise : NormalServeApexHeight;
            Rally.RegisterServe(CourtSide.Player);
            Player.BeginAction(AthleteAction.Serve, ServeSeconds);
            Launch(target, apex, CourtSide.Player);
        }

        void OpponentServe()
        {
            OpponentStep step = Plan.Current;
            Rally.RegisterServe(CourtSide.Opponent);
            Opponent.BeginAction(AthleteAction.Serve, ServeSeconds);
            Launch(step.ServeTarget, OpponentServeApexHeight, CourtSide.Opponent);
            Plan.Advance();
        }

        void PlayerHit(ActionDecision decision)
        {
            float startHeight = BallHeight;
            Vector2 target;
            float apex;
            AthleteAction animation;
            float lockSeconds;

            switch (decision.Kind)
            {
                case ActionKind.Receive:
                    target = SetTarget(decision.Grade, decision.Offset);
                    apex = SetApexHeight;
                    animation = AthleteAction.Receive;
                    lockSeconds = ReceiveSeconds;
                    break;
                case ActionKind.FreeBall:
                    target = FreeBallTarget;
                    apex = FreeBallApexHeight;
                    animation = AthleteAction.Receive;
                    lockSeconds = ReceiveSeconds;
                    break;
                case ActionKind.Smash:
                    if (decision.Grade == TimingGrade.Late && decision.Offset < 0f)
                    {
                        target = new Vector2(-.3f, BallGround.y);
                        apex = startHeight + .1f;
                    }
                    else if (decision.Grade == TimingGrade.Late)
                    {
                        target = AimAtOpponent(move);
                        apex = SetApexHeight;
                    }
                    else
                    {
                        target = AimAtOpponent(move);
                        apex = startHeight + SmashRise;
                    }
                    animation = AthleteAction.Smash;
                    lockSeconds = SmashSeconds;
                    break;
                default:
                    Player.Lunge(Flight.Target, DiveLunge);
                    target = SetTarget(TimingGrade.Late, decision.Offset);
                    apex = DiveApexHeight;
                    animation = AthleteAction.Dive;
                    lockSeconds = DiveSeconds;
                    break;
            }

            bool sendsOver = CourtSpace.SideOf(target) == CourtSide.Opponent;
            Player.BeginAction(animation, lockSeconds);
            if (Rally.RegisterTouch(CourtSide.Player, sendsOver) == TouchOutcome.FourthTouchFault)
            {
                AwardPoint(CourtSide.Opponent, false);
                return;
            }

            if (decision.Kind == ActionKind.Smash && sendsOver && OpponentBlocks(target))
                return;

            lastHitWasPlayerSmash = decision.Kind == ActionKind.Smash && sendsOver && decision.Grade != TimingGrade.Late;
            Launch(target, apex, CourtSide.Player);
        }

        Vector2 SetTarget(TimingGrade grade, float offset)
        {
            float error = grade == TimingGrade.Perfect ? 0f : grade == TimingGrade.Good ? 1f : 2.5f;
            float x = PlayerSetSpot.x + (offset < 0f ? -error : error);
            return new Vector2(Mathf.Clamp(x, -7.5f, -.5f), Mathf.Clamp(Player.Position.y, -3f, 3f));
        }

        void Launch(Vector2 target, float apex, CourtSide hitter)
        {
            float startHeight = BallHeight;
            Vector2 from = BallGround;
            Flight = new BallFlight(from, startHeight, target, Mathf.Max(apex, startHeight + .01f), hitter);
            FlightTime = 0f;
            BallState = BallState.InPlay;
        }

        void ResolveBall()
        {
            if (Flight.CrossesNet && !Flight.ClearsNet && FlightTime >= Flight.NetCrossTime)
            {
                AwardPoint(Rally.WinnerForNetFault(), false);
                return;
            }

            if (FlightTime < Flight.Duration)
                return;

            CourtSide winner = Rally.WinnerForLanding(Flight.Target);
            bool winnerShot = winner == CourtSide.Player && lastHitWasPlayerSmash && CourtSpace.IsIn(Flight.Target);
            AwardPoint(winner, winnerShot);
        }

        void AwardPoint(CourtSide winner, bool winnerShot)
        {
            if (winner == CourtSide.Player)
            {
                PlayerPoints++;
                if (winnerShot)
                    Winners++;
            }
            else
            {
                OpponentPoints++;
            }

            Server = winner;
            BallState = BallState.Dead;
            pauseLeft = PointPause;
            OpponentSmashTell = false;
            lastHitWasPlayerSmash = false;
            PointScored?.Invoke(winner);
            if (PlayerPoints >= PointsToWin || OpponentPoints >= PointsToWin)
                Complete();
        }

        void Complete()
        {
            if (IsOver)
                return;

            IsOver = true;
            Completed?.Invoke();
        }
    }
}
```

`Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.Opponent.cs` (stub, replaced in Task 7):
```csharp
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    // Passive opponent: serves (handled in VolleyballMatch) but never plays the ball.
    // Replaced by the authored AI in the next task.
    public sealed partial class VolleyballMatch
    {
        void ResetOpponentState()
        {
        }

        void TickOpponent(float deltaTime)
        {
        }

        bool OpponentBlocks(Vector2 target) => false;
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" vb-t6`
Expected: `failed="0"` (all volleyball EditMode tests so far).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.cs* Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.Opponent.cs* Assets/Tests/EditMode/Gameplay/Volleyball/MatchDriver.cs* Assets/Tests/EditMode/Gameplay/Volleyball/VolleyballMatchTests.cs*
git commit -m "feat(volleyball): add the match loop, scoring and result"
```

---

### Task 7: Authored opponent AI

**Files:**
- Modify (full rewrite): `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.Opponent.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/OpponentAiTests.cs`

**Interfaces:**
- Consumes (from Task 6, private in the same partial class): `Plan`, `Rally`, `Flight`, `FlightTime`, `BallState`, `BallGround`, `BallHeight`, `Player`, `Opponent`, `tuning`, `Launch(...)`, `AwardPoint(...)`, `OpponentSmashTell`/`OpponentAim` setters, consts `SetApexHeight`, `FreeBallApexHeight`, `ReceiveSeconds`, `SmashSeconds`, `BlockSeconds`, `SmashRise`, `OpponentSetSpot`, `OpponentReadySpot`
- Produces: consts `SmashTellSeconds=.35`, `OpponentBlockX=.6`, `TipApexHeight=3`, `LobApexHeight=5`, `WeakReceiveTarget=(-5,0)`; the real bodies of `ResetOpponentState`, `TickOpponent`, `OpponentBlocks`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/Volleyball/OpponentAiTests.cs`:
```csharp
using System.Collections.Generic;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class OpponentAiTests
    {
        [Test]
        public void ReturnsAnEasyServeWithATelegraphedSmashAwayFromThePlayer()
        {
            var match = new VolleyballMatch();
            Assert.That(MatchDriver.ServeGood(match).Kind, Is.EqualTo(ActionKind.ServeHit));

            bool sawTell = false;
            Vector2 aim = default;
            bool attacked = MatchDriver.AdvanceUntil(match, () =>
            {
                if (match.OpponentSmashTell)
                {
                    sawTell = true;
                    aim = match.OpponentAim;
                }

                return match.BallState == BallState.InPlay && match.Flight.Hitter == CourtSide.Opponent &&
                       match.Rally.Possession == CourtSide.Player;
            }, 6f);

            Assert.That(attacked, Is.True);
            Assert.That(sawTell, Is.True);
            Assert.That(aim, Is.EqualTo(new Vector2(-7f, -3f)));
            Assert.That(match.Flight.Target, Is.EqualTo(aim));
            Assert.That(match.Plan.Index, Is.EqualTo(1));
        }

        [Test]
        public void UnansweredSmashScoresForTheOpponent()
        {
            var match = new VolleyballMatch();
            MatchDriver.ServeGood(match);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 8f), Is.True);
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
        }

        [Test]
        public void WeakReceiveStepSendsAFreeBallStraightBack()
        {
            var plan = new OpponentPlan(new[]
            {
                new OpponentStep(new Vector2(-5f, 0f), AttackKind.Lob, -5f, 0f, true, false)
            });
            var match = new VolleyballMatch(plan);
            MatchDriver.ServeGood(match);

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.Flight.Hitter == CourtSide.Opponent, 4f), Is.True);
            Assert.That(match.Flight.Target, Is.EqualTo(VolleyballMatch.WeakReceiveTarget));
            Assert.That(match.Rally.Possession, Is.EqualTo(CourtSide.Player));
            Assert.That(match.Rally.Touches, Is.Zero);
        }

        [Test]
        public void PlayerBlockOnTheTelegraphedLineWinsThePoint()
        {
            var match = new VolleyballMatch();
            MatchDriver.ServeGood(match);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.OpponentSmashTell, 6f), Is.True);

            match.Player.PlaceAt(new Vector2(-.8f, match.OpponentAim.y));
            Assert.That(match.PressAction().Kind, Is.EqualTo(ActionKind.Block));

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 1f), Is.True);
            Assert.That(match.PlayerPoints, Is.EqualTo(1));
            Assert.That(match.Winners, Is.EqualTo(1));
        }

        static VolleyballMatch PlayerReadyToSmashIntoABlock()
        {
            var plan = new OpponentPlan(new[]
            {
                new OpponentStep(new Vector2(-5f, 0f), AttackKind.Lob, -5f, 0f, false, true)
            });
            var match = new VolleyballMatch(plan);
            match.ForceServerForTest(CourtSide.Opponent);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 3f), Is.True);

            float receiveIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(receiveIdeal));
            MatchDriver.AdvanceToFlightTime(match, receiveIdeal);
            Assert.That(match.PressAction().Kind, Is.EqualTo(ActionKind.Receive));

            float smashIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(smashIdeal));
            MatchDriver.AdvanceToFlightTime(match, smashIdeal);
            Assert.That(match.Opponent.Position.x, Is.LessThanOrEqualTo(ActionResolver.BlockNetDistance));
            return match;
        }

        [Test]
        public void OpponentBlocksASmashDownItsLine()
        {
            VolleyballMatch match = PlayerReadyToSmashIntoABlock();
            match.SetMove(Vector2.zero);
            Assert.That(match.PressAction().Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(match.BallState, Is.EqualTo(BallState.Dead));
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
            Assert.That(match.PlayerPoints, Is.Zero);
        }

        [Test]
        public void SmashAimedAwayFromTheBlockGetsThrough()
        {
            VolleyballMatch match = PlayerReadyToSmashIntoABlock();
            match.SetMove(Vector2.up);
            Assert.That(match.PressAction().Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(match.BallState, Is.EqualTo(BallState.InPlay));
            Assert.That(match.Flight.Target, Is.EqualTo(new Vector2(7f, 3f)));
        }

        static List<string> RunScripted()
        {
            var match = new VolleyballMatch();
            var log = new List<string>();
            match.PointScored += side => log.Add($"{side}:{match.PlayerPoints}-{match.OpponentPoints}@{match.Elapsed:F3}");
            for (int frame = 0; frame < 60 * 40 && !match.IsOver; frame++)
            {
                if (match.Server == CourtSide.Player && match.BallState == BallState.Held)
                    match.PressAction();
                else if (match.Server == CourtSide.Player && match.BallState == BallState.Toss &&
                         match.FlightTime >= match.Flight.ApexTime + .15f)
                    match.PressAction();
                match.Tick(1f / 60f);
            }

            return log;
        }

        [Test]
        public void IdenticalInputsProduceIdenticalMatches()
        {
            List<string> first = RunScripted();
            List<string> second = RunScripted();
            Assert.That(first, Is.Not.Empty);
            Assert.That(second, Is.EqualTo(first));
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.OpponentAiTests" vb-t7`
Expected: compile error because `VolleyballMatch.WeakReceiveTarget` doesn't exist. (After adding only that constant, the rally tests would fail at runtime because the stub AI never plays.)

- [ ] **Step 3: Replace the stub with the AI**

`Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.Opponent.cs`:
```csharp
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    // The authored AI. It reacts after a fixed delay, runs to the contact point at a tuned
    // speed, sets on its first touch and attacks on its second according to Plan.Current. It
    // telegraphs smashes and blocks on the steps authored to block. No randomness.
    public sealed partial class VolleyballMatch
    {
        public const float SmashTellSeconds = .35f;
        public const float OpponentBlockX = .6f;
        public const float TipApexHeight = 3f;
        public const float LobApexHeight = 5f;
        public static readonly Vector2 WeakReceiveTarget = new Vector2(-5f, 0f);
        const float ReboundRise = .3f;
        const float ReboundDepth = 2.5f;

        BallFlight opponentResolvedFlight;

        void ResetOpponentState()
        {
            opponentResolvedFlight = null;
        }

        void TickOpponent(float deltaTime)
        {
            if (Rally.Possession != CourtSide.Opponent)
            {
                PositionOpponentWhileDefending(deltaTime);
                return;
            }

            if (FlightTime < tuning.ReactionDelay || ReferenceEquals(opponentResolvedFlight, Flight))
                return;

            OpponentStep step = Plan.Current;
            bool attacking = Rally.Touches >= 1;
            bool smashing = attacking && step.Attack == AttackKind.Smash &&
                            Flight.ApexHeight > ActionResolver.SmashContactHeight;
            float ideal = Flight.TimeAtHeightDescending(smashing
                ? ActionResolver.SmashContactHeight
                : ActionResolver.ReceiveContactHeight);
            Vector2 contact = Flight.GroundAt(ideal);
            Opponent.MoveToward(contact, deltaTime);

            if (smashing && !OpponentSmashTell && FlightTime >= ideal - SmashTellSeconds)
            {
                OpponentSmashTell = true;
                OpponentAim = OpponentPlan.AttackTarget(step, Player.Position);
            }

            if (FlightTime < ideal)
                return;

            opponentResolvedFlight = Flight;
            if (Vector2.Distance(Opponent.Position, contact) > ActionResolver.Reach)
            {
                OpponentSmashTell = false;
                return;
            }

            if (attacking)
                OpponentAttack(step, smashing);
            else
                OpponentReceive(step);
        }

        void PositionOpponentWhileDefending(float deltaTime)
        {
            if (BallState != BallState.InPlay || Flight == null)
                return;

            if (Plan.Current.BlocksPlayerSmash && Rally.Possession == CourtSide.Player)
            {
                float lineY = Rally.Touches >= 1 && Flight.ApexHeight > ActionResolver.SmashContactHeight
                    ? Flight.GroundAt(Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight)).y
                    : Flight.Target.y;
                Opponent.MoveToward(new Vector2(OpponentBlockX, Mathf.Clamp(lineY, -3f, 3f)), deltaTime);
                return;
            }

            Opponent.MoveToward(OpponentReadySpot, deltaTime);
        }

        void OpponentReceive(OpponentStep step)
        {
            Opponent.BeginAction(AthleteAction.Receive, ReceiveSeconds);
            if (step.WeakReceive)
            {
                Rally.RegisterTouch(CourtSide.Opponent, true);
                Launch(WeakReceiveTarget, FreeBallApexHeight, CourtSide.Opponent);
                Plan.Advance();
                return;
            }

            Rally.RegisterTouch(CourtSide.Opponent, false);
            Launch(new Vector2(OpponentSetSpot.x, Mathf.Clamp(Opponent.Position.y, -3f, 3f)), SetApexHeight,
                CourtSide.Opponent);
        }

        void OpponentAttack(OpponentStep step, bool smashing)
        {
            Vector2 target = OpponentSmashTell ? OpponentAim : OpponentPlan.AttackTarget(step, Player.Position);
            OpponentSmashTell = false;
            Plan.Advance();

            if (smashing && PlayerBlocks(target))
            {
                Vector2 from = BallGround;
                float startHeight = BallHeight;
                AwardPoint(CourtSide.Player, true);
                Rebound(from, startHeight, new Vector2(ReboundDepth, Player.Position.y), CourtSide.Player);
                return;
            }

            Opponent.BeginAction(smashing ? AthleteAction.Smash : AthleteAction.Receive,
                smashing ? SmashSeconds : ReceiveSeconds);
            Rally.RegisterTouch(CourtSide.Opponent, true);
            float apex = smashing ? BallHeight + SmashRise : step.Attack == AttackKind.Tip ? TipApexHeight : LobApexHeight;
            Launch(target, apex, CourtSide.Opponent);
        }

        bool PlayerBlocks(Vector2 target) =>
            Player.Action == AthleteAction.Block &&
            Mathf.Abs(Player.Position.x) <= ActionResolver.BlockNetDistance &&
            Mathf.Abs(Player.Position.y - target.y) <= ActionResolver.BlockLateral;

        bool OpponentBlocks(Vector2 target)
        {
            if (!Plan.Current.BlocksPlayerSmash ||
                Opponent.Position.x > ActionResolver.BlockNetDistance ||
                Mathf.Abs(Opponent.Position.y - target.y) > ActionResolver.BlockLateral)
                return false;

            Vector2 from = BallGround;
            float startHeight = BallHeight;
            Opponent.BeginAction(AthleteAction.Block, BlockSeconds);
            AwardPoint(CourtSide.Opponent, false);
            Rebound(from, startHeight, new Vector2(-ReboundDepth, Opponent.Position.y), CourtSide.Opponent);
            return true;
        }

        // Visual only: the point is already awarded; the ball just drops back off the block.
        void Rebound(Vector2 from, float startHeight, Vector2 target, CourtSide blocker)
        {
            Flight = new BallFlight(from, startHeight, target, startHeight + ReboundRise, blocker);
            FlightTime = 0f;
        }
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" vb-t7`
Expected: `failed="0"` for all volleyball EditMode tests, including all of Task 6's.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.Opponent.cs Assets/Tests/EditMode/Gameplay/Volleyball/OpponentAiTests.cs*
git commit -m "feat(volleyball): add the authored opponent AI with smash tells and blocks"
```

---

### Task 8: Presentation components (`SpriteFlipbook`, views, HUD)

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/SpriteFlipbook.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyAthleteView.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyBallView.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballHud.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/PresentationTests.cs`

**Interfaces:**
- Consumes: `VolleyAthlete`, `VolleyballMatch`, `CourtSpace`, `ActionDecision`, `MinigamePhase` (namespace `KMA.Gameplay`)
- Produces:
  - `SpriteFlipbook`:
    - `Configure(SpriteRenderer, Sprite[] initialFrames, bool loop, float fps)`, `Play(Sprite[], bool loop)`, `Advance(float dt)`
    - `Frames`, `Loop`, `FrameIndex`
  - `VolleyAthleteView`:
    - `Configure(SpriteRenderer, SpriteFlipbook, bool mirror, Sprite[] idle, run, receive, smash, block, dive)`
    - `Render(VolleyAthlete)`, `FramesFor(AthleteAction)`
    - `static SortingOrderFor(float groundY)`, const `NetSortingOrder=100`
  - `VolleyBallView`:
    - `Configure(SpriteRenderer ball, shadow, contactMarker, aimMarker)`, `Render(VolleyballMatch)`
    - `static ShadowScaleFor(float height)`, `static MarkerScaleFor(float secondsToIdeal)`
    - consts `BallSortingOrder=300`, `ShadowSortingOrder=5`, `MarkerSortingOrder=6`
  - `VolleyballHud`:
    - `Configure(TMP_Text score, feedback, hint)`
    - `static ScoreText(int, int)`, `static FeedbackText(TimingGrade, float offset)`
    - `ShowFeedback(ActionDecision)`, `Render(VolleyballMatch, MinigamePhase, float dt)`
    - const `HintText`, `FeedbackLabel`, `HintLabel`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/Volleyball/PresentationTests.cs`:
```csharp
using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class PresentationTests
    {
        GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("PresentationTests");

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        static Sprite[] Frames(string name, int count)
        {
            var texture = new Texture2D(4, 4);
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(.5f, 0f));
                frames[i].name = $"{name}_{i}";
            }

            return frames;
        }

        SpriteRenderer Renderer(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform);
            return child.AddComponent<SpriteRenderer>();
        }

        [Test]
        public void FlipbookLoopsAndClamps()
        {
            SpriteRenderer renderer = Renderer("Book");
            var book = renderer.gameObject.AddComponent<SpriteFlipbook>();
            Sprite[] frames = Frames("f", 3);
            book.Configure(renderer, frames, true, 10f);

            Assert.That(renderer.sprite, Is.SameAs(frames[0]));
            book.Advance(.15f);
            Assert.That(book.FrameIndex, Is.EqualTo(1));
            book.Advance(.2f);
            Assert.That(book.FrameIndex, Is.EqualTo(0));

            book.Play(frames, false);
            book.Advance(5f);
            Assert.That(book.FrameIndex, Is.EqualTo(2));
            Assert.That(renderer.sprite, Is.SameAs(frames[2]));
        }

        [Test]
        public void AthleteViewFollowsTheModel()
        {
            SpriteRenderer body = Renderer("Athlete");
            var book = body.gameObject.AddComponent<SpriteFlipbook>();
            var view = body.gameObject.AddComponent<VolleyAthleteView>();
            Sprite[] idle = Frames("idle", 2), run = Frames("run", 2), receive = Frames("rec", 2),
                smash = Frames("smash", 2), block = Frames("block", 2), dive = Frames("dive", 2);
            view.Configure(body, book, true, idle, run, receive, smash, block, dive);

            var athlete = new VolleyAthlete(CourtSide.Opponent, 5f);
            athlete.PlaceAt(new Vector2(5f, 2f));
            view.Render(athlete);
            Assert.That(view.transform.position, Is.EqualTo(CourtSpace.ToWorld(new Vector2(5f, 2f), 0f)));
            Assert.That(body.sortingOrder, Is.EqualTo(80));
            Assert.That(body.flipX, Is.True);
            Assert.That(book.Frames, Is.SameAs(idle));

            athlete.BeginAction(AthleteAction.Smash, .45f);
            view.Render(athlete);
            Assert.That(book.Frames, Is.SameAs(smash));
            Assert.That(book.Loop, Is.False);
            Assert.That(view.FramesFor(AthleteAction.Serve), Is.SameAs(smash));
            Assert.That(view.FramesFor(AthleteAction.Dive), Is.SameAs(dive));
            Assert.That(view.FramesFor(AthleteAction.Run), Is.SameAs(run));
        }

        [Test]
        public void NearerAthletesDrawInFront()
        {
            Assert.That(VolleyAthleteView.SortingOrderFor(-3f), Is.GreaterThan(VolleyAthleteView.SortingOrderFor(3f)));
            Assert.That(VolleyAthleteView.SortingOrderFor(-5.5f), Is.LessThan(VolleyBallView.BallSortingOrder));
        }

        [Test]
        public void BallViewDrawsBallAboveItsShadow()
        {
            SpriteRenderer ball = Renderer("Ball"), shadow = Renderer("Shadow"),
                contact = Renderer("Contact"), aim = Renderer("Aim");
            var view = root.AddComponent<VolleyBallView>();
            view.Configure(ball, shadow, contact, aim);
            var match = new VolleyballMatch();

            view.Render(match);

            Assert.That(ball.transform.position,
                Is.EqualTo(CourtSpace.ToWorld(VolleyballMatch.PlayerServeSpot, VolleyballMatch.TossStartHeight)));
            Assert.That(shadow.transform.position, Is.EqualTo(CourtSpace.ToWorld(VolleyballMatch.PlayerServeSpot, 0f)));
            Assert.That(shadow.transform.localScale.x,
                Is.EqualTo(VolleyBallView.ShadowScaleFor(VolleyballMatch.TossStartHeight)).Within(1e-4f));
            Assert.That(ball.sortingOrder, Is.EqualTo(VolleyBallView.BallSortingOrder));
            Assert.That(contact.enabled, Is.False);
            Assert.That(aim.enabled, Is.False);

            match.PressAction();
            view.Render(match);
            Assert.That(contact.enabled, Is.True);
        }

        [Test]
        public void ShadowAndMarkerScalesShrinkAsDesigned()
        {
            Assert.That(VolleyBallView.ShadowScaleFor(0f), Is.EqualTo(1f));
            Assert.That(VolleyBallView.ShadowScaleFor(10f), Is.EqualTo(.6f));
            Assert.That(VolleyBallView.MarkerScaleFor(0f), Is.EqualTo(1f));
            Assert.That(VolleyBallView.MarkerScaleFor(.6f), Is.EqualTo(2.5f));
            Assert.That(VolleyBallView.MarkerScaleFor(-.2f), Is.EqualTo(1f));
        }

        [Test]
        public void HudTextsMatchTheSpec()
        {
            Assert.That(VolleyballHud.ScoreText(3, 2), Is.EqualTo("BẠN 3 – 2 MÁY"));
            Assert.That(VolleyballHud.FeedbackText(TimingGrade.Perfect, 0f), Is.EqualTo("PERFECT"));
            Assert.That(VolleyballHud.FeedbackText(TimingGrade.Good, .1f), Is.EqualTo("GOOD"));
            Assert.That(VolleyballHud.FeedbackText(TimingGrade.Late, -.2f), Is.EqualTo("EARLY"));
            Assert.That(VolleyballHud.FeedbackText(TimingGrade.Late, .2f), Is.EqualTo("LATE"));
        }

        [Test]
        public void HudShowsFeedbackOnlyForTimedPressesAndHidesItAgain()
        {
            var hud = root.AddComponent<VolleyballHud>();
            TMP_Text score = new GameObject("Score").AddComponent<TextMeshPro>();
            TMP_Text feedback = new GameObject("Feedback").AddComponent<TextMeshPro>();
            TMP_Text hint = new GameObject("Hint").AddComponent<TextMeshPro>();
            score.transform.SetParent(root.transform);
            feedback.transform.SetParent(root.transform);
            hint.transform.SetParent(root.transform);
            hud.Configure(score, feedback, hint);
            var match = new VolleyballMatch();

            hud.ShowFeedback(new ActionDecision(ActionKind.Block, TimingGrade.Miss, 0f));
            hud.Render(match, MinigamePhase.Play, 0f);
            Assert.That(feedback.enabled, Is.False);

            hud.ShowFeedback(new ActionDecision(ActionKind.Receive, TimingGrade.Perfect, 0f));
            hud.Render(match, MinigamePhase.Play, .1f);
            Assert.That(feedback.enabled, Is.True);
            Assert.That(feedback.text, Is.EqualTo("PERFECT"));
            Assert.That(score.text, Is.EqualTo("BẠN 0 – 0 MÁY"));
            Assert.That(hint.enabled, Is.False);

            hud.Render(match, MinigamePhase.Play, VolleyballHud.FeedbackSeconds);
            Assert.That(feedback.enabled, Is.False);

            hud.Render(match, MinigamePhase.Tutorial, 0f);
            Assert.That(hint.enabled, Is.True);
            Assert.That(hint.text, Is.EqualTo(VolleyballHud.HintText));
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.PresentationTests" vb-t8`
Expected: compile errors for the four new types.

- [ ] **Step 3: Implement**

`Assets/_Project/Scripts/Gameplay/Volleyball/SpriteFlipbook.cs`:
```csharp
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] SpriteRenderer target;
        [SerializeField] Sprite[] initialFrames;
        [SerializeField] bool initialLoop = true;
        [SerializeField, Min(1f)] float framesPerSecond = 12f;

        Sprite[] frames;
        bool loop;
        float time;

        public Sprite[] Frames => frames;
        public bool Loop => loop;
        public int FrameIndex { get; private set; }

        public void Configure(SpriteRenderer renderer, Sprite[] startFrames, bool startLoop, float fps)
        {
            target = renderer;
            initialFrames = startFrames;
            initialLoop = startLoop;
            framesPerSecond = Mathf.Max(1f, fps);
            frames = null;
            Play(startFrames, startLoop);
        }

        void Awake()
        {
            if (frames == null)
                Play(initialFrames, initialLoop);
        }

        void Update() => Advance(Time.deltaTime);

        public void Play(Sprite[] newFrames, bool shouldLoop)
        {
            if (newFrames == null || newFrames.Length == 0)
                return;
            if (ReferenceEquals(newFrames, frames) && shouldLoop == loop)
                return;

            frames = newFrames;
            loop = shouldLoop;
            time = 0f;
            Apply(0);
        }

        public void Advance(float deltaTime)
        {
            if (frames == null || frames.Length == 0)
                return;

            time += Mathf.Max(0f, deltaTime);
            int index = Mathf.FloorToInt(time * framesPerSecond);
            Apply(loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1));
        }

        void Apply(int index)
        {
            FrameIndex = index;
            if (!target)
                target = GetComponent<SpriteRenderer>();
            if (target)
                target.sprite = frames[index];
        }
    }
}
```

`Assets/_Project/Scripts/Gameplay/Volleyball/VolleyAthleteView.cs`:
```csharp
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public sealed class VolleyAthleteView : MonoBehaviour
    {
        public const int NetSortingOrder = 100;

        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteFlipbook flipbook;
        [SerializeField] bool mirror;
        [SerializeField] Sprite[] idle;
        [SerializeField] Sprite[] run;
        [SerializeField] Sprite[] receive;
        [SerializeField] Sprite[] smash;
        [SerializeField] Sprite[] block;
        [SerializeField] Sprite[] dive;

        AthleteAction? shown;

        public void Configure(SpriteRenderer renderer, SpriteFlipbook book, bool mirrored, Sprite[] idleFrames,
            Sprite[] runFrames, Sprite[] receiveFrames, Sprite[] smashFrames, Sprite[] blockFrames, Sprite[] diveFrames)
        {
            body = renderer;
            flipbook = book;
            mirror = mirrored;
            idle = idleFrames;
            run = runFrames;
            receive = receiveFrames;
            smash = smashFrames;
            block = blockFrames;
            dive = diveFrames;
            shown = null;
        }

        // Nearer to the camera (lower court y) draws in front. The court spans y -5..5 plus
        // margins, so athletes stay between 45 and 155, around the net at 100.
        public static int SortingOrderFor(float groundY) => NetSortingOrder - Mathf.RoundToInt(groundY * 10f);

        public Sprite[] FramesFor(AthleteAction action) => action switch
        {
            AthleteAction.Run => run,
            AthleteAction.Serve => smash,
            AthleteAction.Smash => smash,
            AthleteAction.Receive => receive,
            AthleteAction.Block => block,
            AthleteAction.Dive => dive,
            _ => idle
        };

        public void Render(VolleyAthlete athlete)
        {
            if (athlete == null || !body)
                return;

            transform.position = CourtSpace.ToWorld(athlete.Position, 0f);
            body.sortingOrder = SortingOrderFor(athlete.Position.y);
            body.flipX = mirror;
            if (shown == athlete.Action)
                return;

            shown = athlete.Action;
            if (flipbook)
                flipbook.Play(FramesFor(athlete.Action),
                    athlete.Action == AthleteAction.Idle || athlete.Action == AthleteAction.Run);
        }
    }
}
```

`Assets/_Project/Scripts/Gameplay/Volleyball/VolleyBallView.cs`:
```csharp
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public sealed class VolleyBallView : MonoBehaviour
    {
        public const int BallSortingOrder = 300;
        public const int ShadowSortingOrder = 5;
        public const int MarkerSortingOrder = 6;
        public const float MinShadowScale = .6f;
        public const float ShadowFullHeight = 5f;
        const float MarkerLeadSeconds = .6f;
        const float MarkerGrowth = 1.5f;

        [SerializeField] SpriteRenderer ball;
        [SerializeField] SpriteRenderer shadow;
        [SerializeField] SpriteRenderer contactMarker;
        [SerializeField] SpriteRenderer aimMarker;

        public void Configure(SpriteRenderer ballRenderer, SpriteRenderer shadowRenderer,
            SpriteRenderer contactRenderer, SpriteRenderer aimRenderer)
        {
            ball = ballRenderer;
            shadow = shadowRenderer;
            contactMarker = contactRenderer;
            aimMarker = aimRenderer;
        }

        public static float ShadowScaleFor(float height) =>
            Mathf.Lerp(1f, MinShadowScale, Mathf.Clamp01(height / ShadowFullHeight));

        // The contact ring starts large and closes to 1 at the ideal moment.
        public static float MarkerScaleFor(float secondsToIdeal) =>
            1f + MarkerGrowth * Mathf.Clamp01(secondsToIdeal / MarkerLeadSeconds);

        public void Render(VolleyballMatch match)
        {
            if (match == null || !ball || !shadow)
                return;

            Vector2 ground = match.BallGround;
            float height = match.BallHeight;
            ball.transform.position = CourtSpace.ToWorld(ground, height);
            ball.sortingOrder = BallSortingOrder;
            Vector3 shadowPosition = CourtSpace.ToWorld(ground, 0f);
            shadow.transform.position = shadowPosition;
            shadow.transform.localScale = Vector3.one * ShadowScaleFor(height);
            shadow.sortingOrder = ShadowSortingOrder;

            if (contactMarker)
            {
                bool cue = match.TryGetPlayerContactCue(out float secondsToIdeal);
                contactMarker.enabled = cue;
                if (cue)
                {
                    contactMarker.transform.position = shadowPosition;
                    contactMarker.transform.localScale = Vector3.one * MarkerScaleFor(secondsToIdeal);
                    contactMarker.sortingOrder = MarkerSortingOrder;
                }
            }

            if (aimMarker)
            {
                aimMarker.enabled = match.OpponentSmashTell;
                if (match.OpponentSmashTell)
                {
                    aimMarker.transform.position = CourtSpace.ToWorld(match.OpponentAim, 0f);
                    aimMarker.sortingOrder = MarkerSortingOrder;
                }
            }
        }
    }
}
```

`Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballHud.cs`:
```csharp
using TMPro;
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public sealed class VolleyballHud : MonoBehaviour
    {
        public const float FeedbackSeconds = .8f;
        public const string HintText = "Cần gạt trái: di chuyển · Nút phải: chạm bóng đúng lúc";

        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text feedbackLabel;
        [SerializeField] TMP_Text hintLabel;

        float feedbackLeft;

        public void Configure(TMP_Text score, TMP_Text feedback, TMP_Text hint)
        {
            scoreLabel = score;
            feedbackLabel = feedback;
            hintLabel = hint;
            if (hintLabel)
                hintLabel.text = HintText;
            if (feedbackLabel)
                feedbackLabel.enabled = false;
        }

        public static string ScoreText(int playerPoints, int opponentPoints) =>
            $"BẠN {playerPoints} – {opponentPoints} MÁY";

        public static string FeedbackText(TimingGrade grade, float offset) => grade switch
        {
            TimingGrade.Perfect => "PERFECT",
            TimingGrade.Good => "GOOD",
            TimingGrade.Late => offset < 0f ? "EARLY" : "LATE",
            _ => string.Empty
        };

        public void ShowFeedback(ActionDecision decision)
        {
            if (!decision.IsTimed || !feedbackLabel)
                return;

            feedbackLabel.text = FeedbackText(decision.Grade, decision.Offset);
            feedbackLabel.enabled = true;
            feedbackLeft = FeedbackSeconds;
        }

        public void Render(VolleyballMatch match, MinigamePhase phase, float deltaTime)
        {
            if (match != null && scoreLabel)
                scoreLabel.text = ScoreText(match.PlayerPoints, match.OpponentPoints);
            if (hintLabel)
            {
                hintLabel.text = HintText;
                hintLabel.enabled = phase == MinigamePhase.Tutorial;
            }

            if (!feedbackLabel || !feedbackLabel.enabled)
                return;

            feedbackLeft -= Mathf.Max(0f, deltaTime);
            if (feedbackLeft <= 0f)
                feedbackLabel.enabled = false;
        }
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" vb-t8`
Expected: `failed="0"`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/SpriteFlipbook.cs* Assets/_Project/Scripts/Gameplay/Volleyball/VolleyAthleteView.cs* Assets/_Project/Scripts/Gameplay/Volleyball/VolleyBallView.cs* Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballHud.cs* Assets/Tests/EditMode/Gameplay/Volleyball/PresentationTests.cs*
git commit -m "feat(volleyball): add sprite flipbook, athlete and ball views, and HUD"
```

---

### Task 9: Touch and keyboard input

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VirtualJoystick.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/ActionButton.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballInputBridge.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/TouchControlTests.cs`
- Create: `Assets/Tests/PlayMode/Gameplay/Volleyball/KMA.Gameplay.Volleyball.PlayMode.Tests.asmdef`
- Test: `Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballInputBridgeTests.cs`

**Interfaces:**
- Produces:
  - `VirtualJoystick : IPointerDownHandler, IDragHandler, IPointerUpHandler`:
    - `Configure(RectTransform area, RectTransform stickBase, RectTransform knob, float radius, Vector2 restPosition)`
    - `Value`, `IsHeld`, `Press(Vector2 local)`, `Drag(Vector2 local)`, `Release()`
  - `ActionButton : IPointerDownHandler`: `event Action Pressed`, `Press()`
  - `VolleyballInputBridge`:
    - `Configure(VirtualJoystick, ActionButton)`
    - `Move`, `ConsumePresses() → int`, `ClearPresses()`
    - `FeedMoveForTest(Vector2)`, `FeedActionForTest()`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/Volleyball/TouchControlTests.cs`:
```csharp
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class TouchControlTests
    {
        GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("TouchControlTests", typeof(RectTransform));

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        RectTransform Child(string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            return (RectTransform)child.transform;
        }

        [Test]
        public void JoystickReportsTheNormalizedDragFromWhereTheThumbLanded()
        {
            var joystick = root.AddComponent<VirtualJoystick>();
            RectTransform stickBase = Child("Base"), knob = Child("Knob");
            joystick.Configure((RectTransform)root.transform, stickBase, knob, 100f, new Vector2(0f, -200f));

            Assert.That(stickBase.anchoredPosition, Is.EqualTo(new Vector2(0f, -200f)));
            joystick.Press(new Vector2(40f, 40f));
            Assert.That(joystick.IsHeld, Is.True);
            Assert.That(stickBase.anchoredPosition, Is.EqualTo(new Vector2(40f, 40f)));

            joystick.Drag(new Vector2(90f, 40f));
            Assert.That(joystick.Value, Is.EqualTo(new Vector2(.5f, 0f)));
            Assert.That(knob.anchoredPosition, Is.EqualTo(new Vector2(90f, 40f)));

            joystick.Drag(new Vector2(40f, 440f));
            Assert.That(joystick.Value.y, Is.EqualTo(1f).Within(1e-4f));

            joystick.Release();
            Assert.That(joystick.IsHeld, Is.False);
            Assert.That(joystick.Value, Is.EqualTo(Vector2.zero));
            Assert.That(knob.anchoredPosition, Is.EqualTo(new Vector2(0f, -200f)));
        }

        [Test]
        public void ActionButtonRaisesPressed()
        {
            var button = root.AddComponent<ActionButton>();
            int pressed = 0;
            button.Pressed += () => pressed++;
            button.Press();
            Assert.That(pressed, Is.EqualTo(1));
        }
    }
}
```

`Assets/Tests/PlayMode/Gameplay/Volleyball/KMA.Gameplay.Volleyball.PlayMode.Tests.asmdef`:
```json
{
    "name": "KMA.Gameplay.Volleyball.PlayMode.Tests",
    "rootNamespace": "KMA.Tests.Gameplay.Volleyball",
    "references": [
        "UnityEngine.TestRunner",
        "KMA.Gameplay",
        "KMA.Gameplay.Volleyball",
        "Unity.InputSystem",
        "UnityEngine.UI"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballInputBridgeTests.cs`:
```csharp
using System.Collections;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballInputBridgeTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root)
                Object.Destroy(root);
        }

        VolleyballInputBridge CreateBridge(out VirtualJoystick joystick, out ActionButton button)
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
            var bridge = root.AddComponent<VolleyballInputBridge>();
            bridge.Configure(joystick, button);
            root.SetActive(true);
            return bridge;
        }

        [UnityTest]
        public IEnumerator ButtonPressesQueueUntilConsumed()
        {
            VolleyballInputBridge bridge = CreateBridge(out _, out ActionButton button);
            yield return null;

            button.Press();
            button.Press();
            Assert.That(bridge.ConsumePresses(), Is.EqualTo(2));
            Assert.That(bridge.ConsumePresses(), Is.Zero);

            button.Press();
            bridge.ClearPresses();
            Assert.That(bridge.ConsumePresses(), Is.Zero);
        }

        [UnityTest]
        public IEnumerator JoystickDrivesMoveAndTestFeedOverridesIt()
        {
            VolleyballInputBridge bridge = CreateBridge(out VirtualJoystick joystick, out _);
            yield return null;

            Assert.That(bridge.Move, Is.EqualTo(Vector2.zero));
            joystick.Press(Vector2.zero);
            joystick.Drag(new Vector2(0f, 50f));
            Assert.That(bridge.Move, Is.EqualTo(new Vector2(0f, .5f)));

            bridge.FeedMoveForTest(new Vector2(-1f, 0f));
            Assert.That(bridge.Move, Is.EqualTo(new Vector2(-1f, 0f)));

            bridge.FeedActionForTest();
            Assert.That(bridge.ConsumePresses(), Is.EqualTo(1));
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.TouchControlTests" vb-t9e`
Expected: compile errors for `VirtualJoystick`, `ActionButton`.

- [ ] **Step 3: Implement**

`Assets/_Project/Scripts/Gameplay/Volleyball/VirtualJoystick.cs`:
```csharp
using UnityEngine;
using UnityEngine.EventSystems;

namespace KMA.Gameplay.Volleyball
{
    // Floating stick: the base jumps to wherever the thumb lands inside the area. The base and
    // knob are children anchored at the area's centre, so their anchoredPosition equals the local
    // point in the area.
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        const int NoPointer = int.MinValue;

        [SerializeField] RectTransform area;
        [SerializeField] RectTransform stickBase;
        [SerializeField] RectTransform knob;
        [SerializeField, Min(1f)] float radius = 100f;
        [SerializeField] Vector2 restPosition;

        int pointerId = NoPointer;
        Vector2 origin;

        public Vector2 Value { get; private set; }
        public bool IsHeld { get; private set; }

        public void Configure(RectTransform touchArea, RectTransform baseRect, RectTransform knobRect,
            float stickRadius, Vector2 rest)
        {
            area = touchArea;
            stickBase = baseRect;
            knob = knobRect;
            radius = Mathf.Max(1f, stickRadius);
            restPosition = rest;
            Release();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId != NoPointer)
                return;

            pointerId = eventData.pointerId;
            Press(ToLocal(eventData));
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId)
                Drag(ToLocal(eventData));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId)
                Release();
        }

        public void Press(Vector2 local)
        {
            IsHeld = true;
            origin = local;
            if (stickBase)
                stickBase.anchoredPosition = origin;
            Drag(local);
        }

        public void Drag(Vector2 local)
        {
            if (!IsHeld)
                return;

            Vector2 offset = Vector2.ClampMagnitude(local - origin, radius);
            if (knob)
                knob.anchoredPosition = origin + offset;
            Value = offset / radius;
        }

        public void Release()
        {
            pointerId = NoPointer;
            IsHeld = false;
            Value = Vector2.zero;
            origin = restPosition;
            if (stickBase)
                stickBase.anchoredPosition = restPosition;
            if (knob)
                knob.anchoredPosition = restPosition;
        }

        void OnDisable() => Release();

        Vector2 ToLocal(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position,
                eventData.pressEventCamera, out Vector2 local);
            return local;
        }
    }
}
```

`Assets/_Project/Scripts/Gameplay/Volleyball/ActionButton.cs`:
```csharp
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KMA.Gameplay.Volleyball
{
    public sealed class ActionButton : MonoBehaviour, IPointerDownHandler
    {
        public event Action Pressed;

        // Fires on touch-down, not release: timing windows are only a few frames wide.
        public void OnPointerDown(PointerEventData eventData) => Press();

        public void Press() => Pressed?.Invoke();
    }
}
```

`Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballInputBridge.cs`:
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace KMA.Gameplay.Volleyball
{
    // Merges the touch controls with a keyboard fallback (WASD/arrows + Space). The keyboard
    // actions live in code because KMA.inputactions' map list is pinned by InputAssetContractTests.
    public sealed class VolleyballInputBridge : MonoBehaviour
    {
        [SerializeField] VirtualJoystick joystick;
        [SerializeField] ActionButton actionButton;

        InputAction moveAction;
        InputAction pressAction;
        ActionButton subscribedButton;
        int pendingPresses;
        Vector2? testMove;

        public void Configure(VirtualJoystick stick, ActionButton button)
        {
            joystick = stick;
            actionButton = button;
            if (isActiveAndEnabled)
                SubscribeButton();
        }

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

        public void ClearPresses() => pendingPresses = 0;

        public void FeedMoveForTest(Vector2 move) => testMove = move;

        public void FeedActionForTest() => pendingPresses++;

        void OnEnable()
        {
            EnsureActions();
            moveAction.Enable();
            pressAction.Enable();
            SubscribeButton();
        }

        void OnDisable()
        {
            moveAction?.Disable();
            pressAction?.Disable();
            UnsubscribeButton();
        }

        void OnDestroy()
        {
            moveAction?.Dispose();
            pressAction?.Dispose();
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
        }

        void SubscribeButton()
        {
            if (subscribedButton == actionButton)
                return;

            UnsubscribeButton();
            if (!actionButton)
                return;

            actionButton.Pressed += OnPressed;
            subscribedButton = actionButton;
        }

        void UnsubscribeButton()
        {
            if (subscribedButton)
                subscribedButton.Pressed -= OnPressed;
            subscribedButton = null;
        }

        void OnPressed() => pendingPresses++;
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" vb-t9e`
Expected: `failed="0"`.
Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Volleyball" vb-t9p`
Expected: `failed="0"`, 2 passed.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/VirtualJoystick.cs* Assets/_Project/Scripts/Gameplay/Volleyball/ActionButton.cs* Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballInputBridge.cs* Assets/Tests/EditMode/Gameplay/Volleyball/TouchControlTests.cs* Assets/Tests/PlayMode/Gameplay/Volleyball Assets/Tests/PlayMode/Gameplay/Volleyball.meta
git commit -m "feat(volleyball): add virtual joystick, action button and input bridge"
```

---

### Task 10: `VolleyballController`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs`
- Test: `Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballControllerTests.cs`

**Interfaces:**
- Consumes: `MinigameBase` (`Lifecycle`, `Finish`, `PresentationPhase`, `PhaseChanged`, `SetTutorialGate`, `TickPlay`, `BuildHudState`), `MinigameHudState` (namespace `KMA.Gameplay.UI`, assembly `KMA.Gameplay`), and Tasks 6–9
- Produces: `VolleyballController : MinigameBase`
  - const `MaxStep=.1`
  - `Configure(VolleyAthleteView player, VolleyAthleteView opponent, VolleyBallView ball, VolleyballInputBridge input, VolleyballHud hud)`
  - `PlayerView`, `OpponentView`, `BallView`, `Input`, `Hud`, `HasAllReferences`, `Match`, `LastResult`
  - `SkipToPlayForTest()`

- [ ] **Step 1: Write the failing test**

`Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballControllerTests.cs`:
```csharp
using System.Collections;
using System.Text.RegularExpressions;
using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballControllerTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root)
                Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator MissingReferencesLogOnceAndDisable()
        {
            root = new GameObject("Controller");
            var controller = root.AddComponent<VolleyballController>();
            LogAssert.Expect(LogType.Error, new Regex("VolleyballController is missing"));
            yield return null;
            yield return null;
            Assert.That(controller.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator PressesDuringPlayReachTheMatchAndCompletionFiresOnce()
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
            int completions = 0;
            controller.Completed += _ => completions++;
            yield return null;

            controller.SkipToPlayForTest();
            input.FeedActionForTest();
            yield return null;
            Assert.That(controller.Match.BallState, Is.EqualTo(BallState.Toss));

            // The toss above is still in the air, so this one long tick also drops it (+1 to the
            // opponent) before the cap: 3-1 becomes 3-2, still a winning lead.
            controller.Match.SetScoreForTest(3, 1);
            controller.Match.Tick(VolleyballMatch.TimeLimit + 1f);
            yield return null;
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(controller.LastResult.Pass, Is.True);
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Resolve));
        }

        VolleyAthleteView View(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform);
            var renderer = go.AddComponent<SpriteRenderer>();
            var book = go.AddComponent<SpriteFlipbook>();
            var view = go.AddComponent<VolleyAthleteView>();
            var frames = new[] { Sprite.Create(new Texture2D(4, 4), new Rect(0, 0, 4, 4), Vector2.zero) };
            view.Configure(renderer, book, false, frames, frames, frames, frames, frames, frames);
            return view;
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Volleyball.VolleyballControllerTests" vb-t10`
Expected: compile error `VolleyballController` not found.

- [ ] **Step 3: Implement**

`Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs`:
```csharp
using KMA.Gameplay.UI;
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public sealed class VolleyballController : MinigameBase
    {
        public const float MaxStep = .1f;

        [SerializeField] VolleyAthleteView playerView;
        [SerializeField] VolleyAthleteView opponentView;
        [SerializeField] VolleyBallView ballView;
        [SerializeField] VolleyballInputBridge input;
        [SerializeField] VolleyballHud hud;

        public VolleyballMatch Match { get; private set; }
        public MinigameResult LastResult { get; private set; }
        public VolleyAthleteView PlayerView => playerView;
        public VolleyAthleteView OpponentView => opponentView;
        public VolleyBallView BallView => ballView;
        public VolleyballInputBridge Input => input;
        public VolleyballHud Hud => hud;
        public bool HasAllReferences => playerView && opponentView && ballView && input && hud;

        public void Configure(VolleyAthleteView player, VolleyAthleteView opponent, VolleyBallView ball,
            VolleyballInputBridge inputBridge, VolleyballHud volleyballHud)
        {
            playerView = player;
            opponentView = opponent;
            ballView = ball;
            input = inputBridge;
            hud = volleyballHud;
        }

        protected override void Awake()
        {
            base.Awake();
            Match = new VolleyballMatch();
            Match.Completed += OnMatchCompleted;
            Match.PlayerActed += OnPlayerActed;
            PhaseChanged += OnPhaseChanged;
        }

        void Start()
        {
            if (HasAllReferences)
                return;

            Debug.LogError("[KMA] VolleyballController is missing a scene reference; disabling.", this);
            enabled = false;
        }

        void OnDestroy()
        {
            PhaseChanged -= OnPhaseChanged;
            if (Match == null)
                return;

            Match.Completed -= OnMatchCompleted;
            Match.PlayerActed -= OnPlayerActed;
        }

        protected override void TickPlay(float dt)
        {
            float step = Mathf.Min(dt, MaxStep);
            if (step <= 0f)
            {
                // Paused (timeScale 0): drop presses rather than replaying them on resume.
                input.ClearPresses();
                return;
            }

            Match.SetMove(input.Move);
            for (int presses = input.ConsumePresses(); presses > 0; presses--)
                Match.PressAction();
            Match.Tick(step);
        }

        void LateUpdate()
        {
            playerView.Render(Match.Player);
            opponentView.Render(Match.Opponent);
            ballView.Render(Match);
            hud.Render(Match, PresentationPhase, Time.deltaTime);
        }

        protected override MinigameHudState BuildHudState() => new MinigameHudState(
            PresentationPhase.ToString(),
            Match == null ? VolleyballMatch.TimeLimit : Match.TimeRemaining,
            0f,
            0f,
            Match == null ? 0f : Match.PlayerPoints,
            string.Empty);

        public void SkipToPlayForTest()
        {
            SetTutorialGate(false);
            Lifecycle.Tick(float.MaxValue);
        }

        void OnPhaseChanged(MinigamePhase phase)
        {
            if (phase == MinigamePhase.Play && input)
                input.ClearPresses();
        }

        void OnPlayerActed(ActionDecision decision)
        {
            if (hud)
                hud.ShowFeedback(decision);
        }

        void OnMatchCompleted()
        {
            LastResult = Match.BuildResult();
            Finish(LastResult);
        }
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Volleyball" vb-t10`
Expected: `failed="0"`, 4 passed.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs* Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballControllerTests.cs*
git commit -m "feat(volleyball): add the minigame controller"
```

---

### Task 11: Art import and the `MG_Volleyball` scene

**Files:**
- Create (copied from `BVA2.zip`): the 10 PNGs listed under File Structure → Art
- Create: `Assets/Editor/VolleyballSceneConfigurator.cs`
- Modify: `Assets/Editor/KMA.EditorTools.asmdef` (add the `KMA.Gameplay.Volleyball` and `Unity.2D.Sprite.Editor` references)
- Modify: `Assets/_Project/Scripts/UI/MinigameUIAssembler.cs` (add `MG_Volleyball` to `ScenePaths` and a public `AssembleScenePath`)
- Generated: `Assets/_Project/Scenes/MG_Volleyball.unity`, `ProjectSettings/EditorBuildSettings.asset`
- Test: `Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballSceneTests.cs`

**Interfaces:**
- Consumes: every runtime `Configure` method from Tasks 8–10; `CourtSpace.BackgroundWorldPosition`, `CourtSpace.ToWorld`, `CourtSpace.BackgroundPixelsPerUnit`; sorting-order constants
- Produces:
  - `VolleyballSceneConfigurator.BuildScene()` (menu `KMA/Volleyball/Build Scene`), `ImportArt()`, const `ScenePath`
  - `MinigameUIAssembler.AssembleScenePath(string scenePath)`

- [ ] **Step 1: Copy the art out of the zip**

```bash
S="$(mktemp -d)"
unzip -o -q BVA2.zip -x '__MACOSX/*' -d "$S"
mkdir -p Assets/_Project/Art/Characters/BeachVolley Assets/_Project/Art/Environments/Volleyball
for f in playerIdle playerRun playerReception playerBlock playerSmash playerSlide; do cp "$S/$f.png" Assets/_Project/Art/Characters/BeachVolley/; done
for f in beachbkgO net0 ballRoll shadow1; do cp "$S/$f.png" Assets/_Project/Art/Environments/Volleyball/; done
ls Assets/_Project/Art/Characters/BeachVolley Assets/_Project/Art/Environments/Volleyball
```
Expected: 6 PNGs and 4 PNGs listed. Leave `BVA2.zip` itself untracked.

- [ ] **Step 2: Write the failing scene contract test**

`Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballSceneTests.cs`:
```csharp
using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballSceneTests
    {
        [UnityTest]
        public IEnumerator SceneIsFullyWired()
        {
            yield return SceneManager.LoadSceneAsync("MG_Volleyball", LoadSceneMode.Single);
            yield return null;

            var controllers = Object.FindObjectsByType<VolleyballController>(FindObjectsSortMode.None);
            Assert.That(controllers, Has.Length.EqualTo(1));
            VolleyballController controller = controllers[0];
            Assert.That(controller.HasAllReferences, Is.True);
            Assert.That(controller.enabled, Is.True);

            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<VolleyAthleteView>(FindObjectsSortMode.None), Has.Length.EqualTo(2));
            Assert.That(Object.FindFirstObjectByType<EventSystem>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<InputSystemUIInputModule>(), Is.Not.Null);

            var joystick = Object.FindFirstObjectByType<VirtualJoystick>();
            var button = Object.FindFirstObjectByType<ActionButton>();
            Assert.That(joystick, Is.Not.Null);
            Assert.That(button, Is.Not.Null);
            Assert.That(joystick.GetComponentInParent<Canvas>(), Is.Not.Null);
            Assert.That(button.GetComponentInParent<Canvas>(), Is.Not.Null);
            Assert.That(controller.PlayerView.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ActionPressInTheSceneTossesTheServe()
        {
            yield return SceneManager.LoadSceneAsync("MG_Volleyball", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<VolleyballController>();

            controller.SkipToPlayForTest();
            controller.Input.FeedActionForTest();
            yield return null;

            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play));
            Assert.That(controller.Match.BallState, Is.EqualTo(BallState.Toss));
        }
    }
}
```
The PlayMode test asmdef needs no new references: `InputSystemUIInputModule` lives in `Unity.InputSystem`, which it already references.

- [ ] **Step 3: Run to verify failure**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Volleyball.VolleyballSceneTests" vb-t11`
Expected: 2 failures ("Scene 'MG_Volleyball' couldn't be loaded because it has not been added to the build settings").

- [ ] **Step 4: Add the assembler entry point**

In `Assets/_Project/Scripts/UI/MinigameUIAssembler.cs`, add `"Assets/_Project/Scenes/MG_Volleyball.unity"` as the last element of `ScenePaths`, and add this method directly after `AssembleTask5Presentation()`:
```csharp
        public static void AssembleScenePath(string scenePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CameraPrefabPath));
            var cameraPrefab = EnsureCameraPrefab();
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            AssembleScene(scene, cameraPrefab);
            EditorSceneManager.SaveScene(scene);
        }
```

- [ ] **Step 5: Add the editor references**

In `Assets/Editor/KMA.EditorTools.asmdef`, add `"KMA.Gameplay.Volleyball"` after `"KMA.Gameplay.UI"` and `"Unity.2D.Sprite.Editor"` after `"Unity.RenderPipelines.Universal.Runtime"` in `references`.

- [ ] **Step 6: Write the configurator**

`Assets/Editor/VolleyballSceneConfigurator.cs`:
```csharp
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using KMA.Gameplay.UI;
using KMA.Gameplay.Volleyball;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KMA.EditorTools
{
    public static class VolleyballSceneConfigurator
    {
        public const string ScenePath = "Assets/_Project/Scenes/MG_Volleyball.unity";
        const string CharacterDir = "Assets/_Project/Art/Characters/BeachVolley";
        const string EnvironmentDir = "Assets/_Project/Art/Environments/Volleyball";
        const string PixelPath = EnvironmentDir + "/Pixel.png";
        const string FontPath = "Assets/_Project/Fonts/Baloo2-ExtraBold.asset";
        const string HudRootName = "S2_HUD_Minigame";
        const string KnobSpritePath = "UI/Skin/Knob.psd";

        // Tuned by eye in the visual QA task; see the plan's Task 13.
        const float AthletePixelsPerUnit = 26f;
        const float NetPixelsPerUnit = 43f;
        const float HorizonWorldY = 4.97f;
        const int HudSortingOrder = 500;
        static readonly Color SkyColor = new Color32(91, 200, 224, 255);
        static readonly Color SandColor = new Color32(236, 194, 150, 255);
        static readonly Color OpponentTint = new Color(1f, .55f, .55f, 1f);
        static readonly Color ShadowTint = new Color(1f, 1f, 1f, .8f);
        static readonly Color ContactTint = new Color(1f, .9f, .2f, .85f);
        static readonly Color AimTint = new Color(1f, .25f, .2f, .85f);
        static readonly Color ButtonColor = new Color32(255, 152, 0, 242);

        readonly struct TextureSpec
        {
            public readonly string Path;
            public readonly int FrameWidth;
            public readonly float PixelsPerUnit;
            public readonly Vector2 Pivot;

            public TextureSpec(string path, int frameWidth, float pixelsPerUnit, Vector2 pivot)
            {
                Path = path;
                FrameWidth = frameWidth;
                PixelsPerUnit = pixelsPerUnit;
                Pivot = pivot;
            }

            public bool Sliced => FrameWidth > 0;
        }

        static readonly Vector2 Feet = new Vector2(.5f, 0f);
        static readonly Vector2 Centre = new Vector2(.5f, .5f);

        static readonly TextureSpec[] Textures =
        {
            new TextureSpec(CharacterDir + "/playerIdle.png", 32, AthletePixelsPerUnit, Feet),
            new TextureSpec(CharacterDir + "/playerRun.png", 32, AthletePixelsPerUnit, Feet),
            new TextureSpec(CharacterDir + "/playerReception.png", 32, AthletePixelsPerUnit, Feet),
            new TextureSpec(CharacterDir + "/playerBlock.png", 32, AthletePixelsPerUnit, Feet),
            new TextureSpec(CharacterDir + "/playerSmash.png", 32, AthletePixelsPerUnit, Feet),
            new TextureSpec(CharacterDir + "/playerSlide.png", 43, AthletePixelsPerUnit, Feet),
            new TextureSpec(EnvironmentDir + "/net0.png", 45, NetPixelsPerUnit, Centre),
            new TextureSpec(EnvironmentDir + "/ballRoll.png", 15, CourtSpace.BackgroundPixelsPerUnit, Centre),
            new TextureSpec(EnvironmentDir + "/beachbkgO.png", 0, CourtSpace.BackgroundPixelsPerUnit, Centre),
            new TextureSpec(EnvironmentDir + "/shadow1.png", 0, CourtSpace.BackgroundPixelsPerUnit, Centre),
            new TextureSpec(PixelPath, 0, 4f, Centre)
        };

        [MenuItem("KMA/Volleyball/Build Scene")]
        public static void BuildScene()
        {
            ImportArt();
            BuildWorld();
            MinigameUIAssembler.AssembleScenePath(ScenePath);
            AddControlsAndHud();
            EnsureInBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA] MG_Volleyball built.");
        }

        public static void ImportArt()
        {
            EnsurePixelTexture();
            foreach (TextureSpec spec in Textures)
            {
                if (!File.Exists(spec.Path))
                    throw new FileNotFoundException(
                        $"[KMA] Volleyball art is missing: {spec.Path}. Extract it from BVA2.zip.", spec.Path);
            }

            foreach (TextureSpec spec in Textures)
                ConfigureTexture(spec);
        }

        static void EnsurePixelTexture()
        {
            if (File.Exists(PixelPath))
                return;

            Directory.CreateDirectory(EnvironmentDir);
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
            texture.Apply();
            File.WriteAllBytes(PixelPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(PixelPath);
        }

        static void ConfigureTexture(TextureSpec spec)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(spec.Path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = spec.Sliced ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = spec.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = spec.Pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            if (!spec.Sliced)
                return;

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int count = width / spec.FrameWidth;
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] existing = provider.GetSpriteRects();
            if (existing.Length == count && existing.All(r => Mathf.Approximately(r.rect.width, spec.FrameWidth)))
                return;

            string baseName = Path.GetFileNameWithoutExtension(spec.Path);
            var rects = new SpriteRect[count];
            for (int i = 0; i < count; i++)
            {
                rects[i] = new SpriteRect
                {
                    name = $"{baseName}_{i:00}",
                    rect = new Rect(i * spec.FrameWidth, 0, spec.FrameWidth, height),
                    alignment = SpriteAlignment.Custom,
                    pivot = spec.Pivot,
                    spriteID = GUID.Generate()
                };
            }

            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(
                rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
            provider.Apply();
            importer.SaveAndReimport();
        }

        static Sprite[] Frames(string path)
        {
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
            if (frames.Length == 0)
                throw new InvalidOperationException($"[KMA] No sprites were sliced from {path}.");
            return frames;
        }

        static Sprite Single(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path) ??
            throw new InvalidOperationException($"[KMA] {path} did not import as a sprite.");

        static void BuildWorld()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Sprite pixel = Single(PixelPath);
            Sprite shadowSprite = Single(EnvironmentDir + "/shadow1.png");

            Quad("Sky", pixel, SkyColor, new Vector3(0f, HorizonWorldY + 10f, 0f), new Vector2(60f, 20f));
            Quad("Sand", pixel, SandColor, new Vector3(0f, HorizonWorldY - 20f, 0f), new Vector2(60f, 40f));
            Renderer("Court", Single(EnvironmentDir + "/beachbkgO.png"), CourtSpace.BackgroundWorldPosition, -20);
            Renderer("Net", Frames(EnvironmentDir + "/net0.png")[0], CourtSpace.ToWorld(Vector2.zero, 0f),
                VolleyAthleteView.NetSortingOrder);

            VolleyAthleteView player = Athlete("Player", false, Color.white);
            VolleyAthleteView opponent = Athlete("Opponent", true, OpponentTint);

            Sprite[] roll = Frames(EnvironmentDir + "/ballRoll.png");
            SpriteRenderer ball = Renderer("Ball", roll[0], Vector3.zero, VolleyBallView.BallSortingOrder);
            ball.gameObject.AddComponent<SpriteFlipbook>().Configure(ball, roll, true, 14f);
            SpriteRenderer shadow = Renderer("BallShadow", shadowSprite, Vector3.zero, VolleyBallView.ShadowSortingOrder);
            shadow.color = ShadowTint;
            SpriteRenderer contact = Renderer("ContactMarker", shadowSprite, Vector3.zero, VolleyBallView.MarkerSortingOrder);
            contact.color = ContactTint;
            SpriteRenderer aim = Renderer("AimMarker", shadowSprite, Vector3.zero, VolleyBallView.MarkerSortingOrder);
            aim.color = AimTint;
            var ballView = new GameObject("BallView").AddComponent<VolleyBallView>();
            ballView.Configure(ball, shadow, contact, aim);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            var controllerObject = new GameObject("VolleyballController");
            var input = controllerObject.AddComponent<VolleyballInputBridge>();
            var controller = controllerObject.AddComponent<VolleyballController>();
            controller.Configure(player, opponent, ballView, input, null);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static VolleyAthleteView Athlete(string name, bool mirror, Color tint)
        {
            Sprite[] idle = Frames(CharacterDir + "/playerIdle.png");
            SpriteRenderer body = Renderer(name, idle[0], Vector3.zero, 0);
            body.color = tint;
            var book = body.gameObject.AddComponent<SpriteFlipbook>();
            book.Configure(body, idle, true, 12f);
            var view = body.gameObject.AddComponent<VolleyAthleteView>();
            view.Configure(body, book, mirror, idle,
                Frames(CharacterDir + "/playerRun.png"),
                Frames(CharacterDir + "/playerReception.png"),
                Frames(CharacterDir + "/playerSmash.png"),
                Frames(CharacterDir + "/playerBlock.png"),
                Frames(CharacterDir + "/playerSlide.png"));
            return view;
        }

        static SpriteRenderer Renderer(string name, Sprite sprite, Vector3 position, int order)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        static void Quad(string name, Sprite pixel, Color color, Vector3 position, Vector2 size)
        {
            SpriteRenderer renderer = Renderer(name, pixel, position, -30);
            renderer.color = color;
            renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        static void AddControlsAndHud()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject hudRoot = scene.GetRootGameObjects().Single(go => go.name == HudRootName);
            hudRoot.GetComponent<Canvas>().sortingOrder = HudSortingOrder;
            var parent = (RectTransform)(hudRoot.transform.Find("SafeAreaRoot") ?? hudRoot.transform);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(KnobSpritePath);

            RectTransform controls = UiRect("VolleyballControls", parent, Vector2.zero, Vector2.one);
            controls.SetAsFirstSibling();

            RectTransform area = UiRect("JoystickArea", controls, Vector2.zero, new Vector2(.4f, 1f));
            area.gameObject.AddComponent<Image>().color = Color.clear;
            RectTransform stickBase = Circle("JoystickBase", area, knobSprite, 240f, new Color(1f, 1f, 1f, .3f));
            RectTransform knob = Circle("JoystickKnob", area, knobSprite, 120f, new Color(1f, 1f, 1f, .7f));
            var joystick = area.gameObject.AddComponent<VirtualJoystick>();
            joystick.Configure(area, stickBase, knob, 100f, new Vector2(0f, -220f));

            RectTransform buttonRect = Circle("ActionButton", controls, knobSprite, 280f, ButtonColor);
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(1f, 0f);
            buttonRect.anchoredPosition = new Vector2(-140f, 140f);
            buttonRect.GetComponent<Image>().raycastTarget = true;
            var button = buttonRect.gameObject.AddComponent<ActionButton>();
            Label("Label", buttonRect, font, "ĐÁNH", 64f, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TMP_Text score = Label("Score", controls, font, VolleyballHud.ScoreText(0, 0), 64f,
                new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 100f));
            TMP_Text feedback = Label("Feedback", controls, font, string.Empty, 80f,
                new Vector2(.5f, .65f), new Vector2(.5f, .65f), Vector2.zero, new Vector2(600f, 120f));
            TMP_Text hint = Label("Hint", controls, font, VolleyballHud.HintText, 40f,
                new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 40f), new Vector2(1400f, 80f));
            var hud = controls.gameObject.AddComponent<VolleyballHud>();
            hud.Configure(score, feedback, hint);

            var controller = Object.FindFirstObjectByType<VolleyballController>();
            controller.Input.Configure(joystick, button);
            controller.Configure(controller.PlayerView, controller.OpponentView, controller.BallView, controller.Input, hud);

            foreach (Object dirty in new Object[] { joystick, button, hud, controller, controller.Input, hudRoot })
                EditorUtility.SetDirty(dirty);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static RectTransform UiRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        static RectTransform Circle(string name, Transform parent, Sprite sprite, float size, Color color)
        {
            RectTransform rect = UiRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            rect.sizeDelta = new Vector2(size, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, string text, float size,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions)
        {
            RectTransform rect = UiRect(name, parent, anchorMin, anchorMax);
            rect.pivot = new Vector2(.5f, anchorMin.y >= 1f ? 1f : anchorMin.y <= 0f && anchorMax.y <= 0f ? 0f : .5f);
            rect.anchoredPosition = position;
            if (dimensions != Vector2.zero)
                rect.sizeDelta = dimensions;
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font)
                label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        static void EnsureInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath))
                return;

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
```

- [ ] **Step 7: Build the scene**

Run: `"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit -projectPath "$(pwd -W)" -executeMethod KMA.EditorTools.VolleyballSceneConfigurator.BuildScene -logFile Builds/TestResults/vb-build.log; echo "exit $?"; grep -n "MG_Volleyball built\|Exception\|error CS" Builds/TestResults/vb-build.log | head`
Expected: `exit 0` and `[KMA] MG_Volleyball built.`
Check the slicing: `grep -c "name: playerIdle_" Assets/_Project/Art/Characters/BeachVolley/playerIdle.png.meta` → `12`; `grep -c "name: playerSlide_" Assets/_Project/Art/Characters/BeachVolley/playerSlide.png.meta` → `15`; `grep -c "name: ballRoll_" Assets/_Project/Art/Environments/Volleyball/ballRoll.png.meta` → `8`.

- [ ] **Step 8: Run to verify pass**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Volleyball" vb-t11`
Expected: `failed="0"`, 6 passed.
Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.UI|KMA.Tests.Input" vb-t11-shared`
Expected: `failed="0"` (the assembler change and the new art don't break shared UI or input tests).

- [ ] **Step 9: Commit**

```bash
git add Assets/_Project/Art/Characters/BeachVolley Assets/_Project/Art/Characters/BeachVolley.meta Assets/_Project/Art/Environments/Volleyball Assets/_Project/Art/Environments/Volleyball.meta Assets/Editor/VolleyballSceneConfigurator.cs* Assets/Editor/KMA.EditorTools.asmdef Assets/_Project/Scripts/UI/MinigameUIAssembler.cs Assets/_Project/Scenes/MG_Volleyball.unity* ProjectSettings/EditorBuildSettings.asset Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballSceneTests.cs*
git status --short
git commit -m "feat(volleyball): import the BVA2 art and build the MG_Volleyball scene"
```
Check `git status --short` before committing. If the assembler re-saved `Assets/_Project/Prefabs/Gameplay/GameCamera.prefab` with a real diff, inspect it. Commit it only if the change is value-identical (a serialization reorder); otherwise revert it with `git checkout -- Assets/_Project/Prefabs/Gameplay/GameCamera.prefab`.

---

### Task 12: Register Volleyball as a subject (id, save v6, routing, map, docs)

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/SubjectId.cs`
- Modify: `Assets/_Project/Scripts/Progression/SaveData.cs:8`
- Modify: `Assets/_Project/Scripts/Core/SaveSystem.cs:203-231` (`MigrateTutorialSeen`)
- Modify: `Assets/_Project/Scripts/Core/SceneRouter.cs:550-554`
- Modify: `Assets/_Project/Scripts/Core/S5RouteBootstrap.cs:11-15`
- Modify: `Assets/_Project/Scripts/UI/MapPresentationBuilder.cs:28-32` and `:333-349`
- Create: `Assets/_Project/ScriptableObjects/Subjects/Volleyball.asset`
- Modify tests:
  - `Assets/Tests/EditMode/Progression/SaveDataTests.cs`
  - `Assets/Tests/EditMode/Progression/SaveSystemTests.cs`
  - `Assets/Tests/EditMode/Progression/GameSessionPersistenceTests.cs:218`
  - `Assets/Tests/EditMode/Presentation/UIComponentTests.cs:141-143,235`
  - `Assets/Tests/PlayMode/Progression/FullGameplayFlowTests.cs:71-81`
  - `Assets/Tests/EditMode/Progression/SceneRouterSessionTests.cs` (new test)
- Modify: `README.md`, `PLAN.md`

**Interfaces:**
- Produces: `SubjectId.Volleyball = 7`; `SaveData.CurrentVersion = 6`; the router maps `Volleyball → "MG_Volleyball"`; the map card order is Sprint, Volleyball, Football.

- [ ] **Step 1: Update and add the failing tests**

`SaveDataTests.cs`:
- In `SaveData_ContainsTwoRecordsAndSettings`, rename it to `SaveData_ContainsThreeRecordsAndSettings`.
- Replace `Has.Length.EqualTo(2)` on `subjectIds` and on `data.tutorialSeen` with `Has.Length.EqualTo(3)`.
- Replace the expected ids with `new[] { SubjectId.Sprint, SubjectId.Football, SubjectId.Volleyball }`.
- Replace `Is.EqualTo(5)` on `SaveData.CurrentVersion` with `Is.EqualTo(6)`.
- In the JSON round-trip test, change `restored.subjects` and `restored.tutorialSeen` from `Has.Length.EqualTo(2)` to `Has.Length.EqualTo(3)`.

`SaveSystemTests.cs`:
- In `Migrate_OlderVersion_FillsMissingFieldsAndUpdatesVersion` (lines 99 and 105): 2 → 3 for `subjects` and `tutorialSeen`.
- In `Load_VersionTwoSave_RetainsTheActiveAttempt` (lines 176 and 180): `Has.Length.EqualTo(3)`, and expected `tutorialSeen` `new[] { false, true, false }`. Add `Assert.That(actual.subjects[2].id, Is.EqualTo(SubjectId.Volleyball)); Assert.That(actual.subjects[2].passed, Is.False);`. This proves the old v2 record with id 2 (bestScore 8) is **not** read as Volleyball.
- In `Load_VersionFourSave_DropsBadmintonAndKeepsRemainingProgress` (lines 211 and 216): `Has.Length.EqualTo(3)`, and expected `tutorialSeen` `new[] { false, true, false }`.
- Add this test after the v4 test:

```csharp
        [Test]
        public void Load_VersionFiveSave_KeepsProgressAndAddsAFreshVolleyballRecord()
        {
            var versionFive = SaveData.CreateDefault();
            versionFive.version = 5;
            versionFive.subjects = new[]
            {
                new SubjectRecordData { id = SubjectId.Sprint, passed = true, bestScore = 8.5f, bestRank = Rank.A },
                new SubjectRecordData { id = SubjectId.Football, failedVisits = 1 }
            };
            versionFive.tutorialSeen = new[] { true, false };
            versionFive.lives = 4;
            versionFive.hasActiveSubject = true;
            versionFive.activeSubject = SubjectId.Sprint;
            versionFive.visitAttempt = 1;

            WriteRawSave(versionFive);
            SaveData actual = saveSystem.Load();

            Assert.That(actual.version, Is.EqualTo(6));
            Assert.That(actual.lives, Is.EqualTo(4));
            Assert.That(actual.subjects, Has.Length.EqualTo(3));
            Assert.That(actual.subjects[0].id, Is.EqualTo(SubjectId.Sprint));
            Assert.That(actual.subjects[0].bestScore, Is.EqualTo(8.5f));
            Assert.That(actual.subjects[1].id, Is.EqualTo(SubjectId.Football));
            Assert.That(actual.subjects[1].failedVisits, Is.EqualTo(1));
            Assert.That(actual.subjects[2].id, Is.EqualTo(SubjectId.Volleyball));
            Assert.That(actual.subjects[2].passed, Is.False);
            Assert.That(actual.tutorialSeen, Is.EqualTo(new[] { true, false, false }));
            Assert.That(actual.hasActiveSubject, Is.True);
            Assert.That(actual.activeSubject, Is.EqualTo(SubjectId.Sprint));
        }
```

`GameSessionPersistenceTests.cs:218`: `Has.Count.EqualTo(2)` → `Has.Count.EqualTo(3)`.

`UIComponentTests.cs`: lines 141–142, `grid.transform.childCount` and the `MapNodeView` count go from 2 to 3. Line 235, `iconSprites.Distinct().Count()` goes from 2 to 3. Add after line 143:
```csharp
                Assert.That(root.GetComponentsInChildren<MapNodeView>(true).Select(node => node.SubjectId),
                    Is.EqualTo(new[] { SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football }));
```
If the file lacks `using System.Linq;`, add it.

`FullGameplayFlowTests.cs`: line 71 `Has.Count.EqualTo(2)` → `3`; line 79 `harness.Transitions, Has.Count.EqualTo(8)` → `10` (two transitions per extra subject).

`SceneRouterSessionTests.cs`, add:
```csharp
        [Test]
        public void VolleyballRoutesToItsScene()
        {
            Assert.That(router.TryGetSceneName(SessionRoute.Subject, SubjectId.Volleyball, out string sceneName), Is.True);
            Assert.That(sceneName, Is.EqualTo("MG_Volleyball"));
        }
```

- [ ] **Step 2: Run to verify failure**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression|KMA.Tests.Gameplay.UI" vb-t12`
Expected: compile error `SubjectId.Volleyball` not defined.

- [ ] **Step 3: Implement the id and the save migration**

`SubjectId.cs`:
```csharp
namespace KMA.Gameplay
{
    public enum SubjectId
    {
        Sprint = 0,
        Football = 6,
        // 7, not a reused number: saves from before the content cut hold ids 1-5 for retired
        // subjects, and none of that progress may surface as Volleyball.
        Volleyball = 7
    }
}
```

`SaveData.cs:8`: `public const int CurrentVersion = 6;`

`SaveSystem.cs`, in `MigrateTutorialSeen`, replace the `int oldIndex = ...` expression with:
```csharp
                int oldIndex = sourceVersion >= 5
                    ? currentSubjects[currentIndex] switch
                    {
                        SubjectId.Sprint => 0,
                        SubjectId.Football => 1,
                        _ => -1
                    }
                    : sourceVersion == 4
                    ? currentSubjects[currentIndex] switch
                    {
                        SubjectId.Sprint => 0,
                        SubjectId.Football => 2,
                        _ => -1
                    }
                    : sourceVersion == 3
                    ? currentSubjects[currentIndex] switch
                    {
                        SubjectId.Sprint => 0,
                        SubjectId.Football => 3,
                        _ => -1
                    }
                    : (int)currentSubjects[currentIndex];
```

- [ ] **Step 4: Implement routing, map card, and subject config**

`SceneRouter.cs` `DefaultSubjectScenes()` and `S5RouteBootstrap.cs` `Routes`: in both, insert the entry between the Sprint and Football entries:
```csharp
            new SubjectScene { Subject = SubjectId.Volleyball, SceneName = "MG_Volleyball" },
```
In `S5RouteBootstrap.cs` the type is spelled `SceneRouter.SubjectScene`.

`MapPresentationBuilder.cs` `Entries`:
```csharp
        static readonly Entry[] Entries =
        {
            new Entry(SubjectId.Sprint, "Chạy nước rút", new Color32(49, 162, 222, 255), true),
            new Entry(SubjectId.Volleyball, "Bóng chuyền", new Color32(245, 158, 46, 255), true),
            new Entry(SubjectId.Football, "Bóng đá", new Color32(226, 232, 240, 255), false),
        };
```
In the icon `switch`, after the Football case, add:
```csharp
                case SubjectId.Volleyball:
                    DrawRing(pixels, size, 48, 48, 34, 6);
                    DrawLine(pixels, size, 48, 48, 48, 82, 5);
                    DrawLine(pixels, size, 48, 48, 19, 31, 5);
                    DrawLine(pixels, size, 48, 48, 77, 31, 5);
                    break;
```

`Assets/_Project/ScriptableObjects/Subjects/Volleyball.asset`:
```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 9dcb792ef8d440b0b4c0aeec82ff5de6, type: 3}
  m_Name: Volleyball
  m_EditorClassIdentifier:
  subjectId: 7
  displayName: Bóng chuyền
  icon: {fileID: 0}
  color: {r: 0.96, g: 0.62, b: 0.18, a: 1}
  goalText: Thắng 5 điểm trước đối thủ trong 120 giây.
  timeLimit: 120
  passThreshold: 0.6
  unlocked: 1
  comingSoon: 0
```

- [ ] **Step 5: Run to verify pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests" vb-t12-edit`
Expected: `failed="0"` across the whole EditMode suite.
Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Progression|KMA.Tests.Gameplay.Core" vb-t12-play`
Expected: `failed="0"`. If a test outside the files listed above asserts two subjects, update its count the same way and name it in the commit message.

- [ ] **Step 6: Update the docs**

`README.md`:
- "Unity gameplay prototype for KMA: two retained sports subjects, ..." → "Unity gameplay prototype for KMA: three sports subjects, ..."
- "- Sprint is selectable; Football remains disabled on the map." → "- Sprint and Volleyball are selectable; Football remains disabled on the map."
- "The retained subjects are Sprint and Football. Only Sprint is currently selectable on the map." → "The subjects are Sprint, Volleyball and Football. Sprint and Volleyball are selectable on the map."
- Add the scene row after `MG_Sprint`: `| \`MG_Volleyball\` | 1v1 beach volleyball against an authored AI; first to 5 points within 120 s |`
- Add the controls row after Sprint: `| Volleyball | WASD/arrow keys move, Space for the action button (touch: left-thumb joystick, right action button) |`

`PLAN.md`:
- Line 3: "Sprint và Football. Sprint có thể chơi; Football đang khóa trên bản đồ." → "Sprint, Volleyball và Football. Sprint và Volleyball có thể chơi; Football đang khóa trên bản đồ."
- Add a row after Sprint in the §3 table: `| Volleyball | \`MG_Volleyball\` | Có thể chơi |`
- In §4, add `MG_Volleyball` to the scene list.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/Progression/SubjectId.cs Assets/_Project/Scripts/Progression/SaveData.cs Assets/_Project/Scripts/Core/SaveSystem.cs Assets/_Project/Scripts/Core/SceneRouter.cs Assets/_Project/Scripts/Core/S5RouteBootstrap.cs Assets/_Project/Scripts/UI/MapPresentationBuilder.cs Assets/_Project/ScriptableObjects/Subjects/Volleyball.asset* Assets/Tests README.md PLAN.md
git commit -m "feat: register Volleyball as a playable subject and bump saves to v6"
```

---

### Task 13: Visual QA and tuning

**Files:**
- Modify (tuning only): the constants at the top of `Assets/Editor/VolleyballSceneConfigurator.cs`
- Regenerate: `Assets/_Project/Scenes/MG_Volleyball.unity`

- [ ] **Step 1: Load the screenshot skill**

Invoke the `testing-unity-ui-with-screenshots` skill and follow it. It needs the Editor open with `PlayModeScreenshot` listening.

- [ ] **Step 2: Capture the three states**

```bash
tools/qa-screenshot.sh Builds/Screenshots/vb-tutorial.png Assets/_Project/Scenes/MG_Volleyball.unity 1
tools/qa-screenshot.sh Builds/Screenshots/vb-play.png Assets/_Project/Scenes/MG_Volleyball.unity 7
tools/qa-screenshot.sh Builds/Screenshots/vb-rally.png Assets/_Project/Scenes/MG_Volleyball.unity 10
```
Open each PNG with the Read tool.

- [ ] **Step 3: Check against this list**

Fix anything that fails by changing only the configurator constants, then rebuild with the Task 11 Step 7 command:

| Check | Knob if it fails |
|---|---|
| The court's outer lines are fully on screen and the court fills about 80–90% of the height | `CourtSpace.BackgroundPixelsPerUnit` is fixed by tests, so adjust the camera instead: add `camera.orthographicSize` in `AddControlsAndHud` |
| No blue gap at the horizon; the sky and sand bands meet the background edges | `HorizonWorldY`, `SkyColor`, `SandColor` |
| The net stands on the centre line, and its posts reach roughly from the far sideline to the near one | `NetPixelsPerUnit` |
| The athletes are about 1.5× the net's post width and stand on their shadows' ground line, not floating | `AthletePixelsPerUnit` |
| The player faces the net (right) and the opponent faces left | Swap the `mirror` arguments in `BuildWorld` |
| The joystick is on the left, the ĐÁNH button bottom right, and the score top centre; none of them covers the court's near baseline | Anchors and positions in `AddControlsAndHud` |
| The tutorial hint shows during the tutorial and is gone in play | `VolleyballHud.Render` (code bug, so fix with a test) |
| Vietnamese diacritics render (BẠN, ĐÁNH, Cần gạt) | `FontPath` must point to the font with the Vietnamese fallback |

- [ ] **Step 4: Rerun the scene tests after the rebuild**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Volleyball" vb-t13` (Editor closed)
Expected: `failed="0"`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Editor/VolleyballSceneConfigurator.cs Assets/_Project/Scenes/MG_Volleyball.unity
git commit -m "fix(volleyball): tune court, sprite and control layout from screenshot QA"
```

---

### Task 14: Full verification

- [ ] **Step 1: Run both full suites** (Editor closed)

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests" vb-final-edit`
Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests" vb-final-play`
Expected: `failed="0"` in both. The README warns that older snapshots predate the current scope. For any failure, run the same filter on the parent commit of this work (`c9e8c4a`) to tell pre-existing failures from new ones, and fix every new one. Report pre-existing failures to the user with their names; don't fix them here.

- [ ] **Step 2: Play the route by hand in the Editor**

Open `Bootstrap`, press Play, and go Menu → Map → the "Bóng chuyền" card → play a point with the keyboard → finish or time out → the result panel → back to Map. Confirm the card shows the Volleyball icon and that a loss costs a life.

- [ ] **Step 3: Report**

Summarize for the user: test totals from both XML summaries, the screenshots taken, and any pre-existing failures left untouched. The work is already committed task by task; there is nothing more to push unless the user asks.
