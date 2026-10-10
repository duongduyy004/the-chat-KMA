# Minigame How-To-Play Guide Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every minigame (Sprint, Football, Volleyball, FrogJump, Chess) shows a multi-page how-to-play/rules guide the first time it is entered. The guide can be reopened from a new HƯỚNG DẪN button in the pause menu.

**Architecture:**
- Each controller implements `IMinigameGuideSource`. Its pages come from a pure static builder per game, fed with the current lesson's numbers.
- A runtime-built `MinigameGuidePanel` draws above the pause menu at sorting order 950.
- An auto-installed `MinigameGuideHost` opens the panel on the first visit and serves the pause menu.
- While the first-run guide is open, the game is frozen through a shared, holder-counted `GameFreeze`. `PausePanel` now uses the same helper.

**Tech Stack:** Unity 6000.3.23f1, C#, NUnit (Unity Test Framework, EditMode + PlayMode), uGUI + TextMeshPro.

**Spec:** `docs/superpowers/specs/2026-10-11-minigame-guide-design.md`. Section 5 lists the decisions made while planning.

## Global Constraints

**Text and keys**
- Vietnamese copy is exactly as written in this plan. Labels go through `VietText.Fix` in the panel only; builders return raw strings.
- Page titles: `MỤC TIÊU`, `ĐIỀU KHIỂN`, `LUẬT`, `NGẮM`, `LỰC`, `DI CHUYỂN`, `ĐÁNH & NHẢY`, `NẾU TRƯỢT`.
- Buttons: `TIẾP`, `QUAY LẠI`, `BỎ QUA`, `BẮT ĐẦU` (last page, first run), `ĐÓNG` (last page, review). The pause button is `HƯỚNG DẪN`.
- Guide keys: `"Sprint"`, `"Football"`, `"Volleyball"`, `"Chess"` (the `SubjectId` names) and `"FrogJump"`.
- Numbers use `MinigameGuidePages.Number`: `"0.#"` with the vi-VN culture (`150`, `2,5`).

**Behaviour**
- The "NẾU TRƯỢT" page appears only for `ChallengeKind.Practice` and `ChallengeKind.Exam`. With `kind == null` (free play) there is no failure page.
- The guide canvas sorts at **950**; the pause menu stays at 900.
- The first-run guide freezes the game (`GameFreeze.Acquire`). The review guide does not; the pause menu already holds the freeze.
- Seen flags are per game, not per lesson. Skip and the last-page BẮT ĐẦU both mark the game seen. Review never changes the flag.
- No `SaveData.CurrentVersion` bump. Old saves load the new `frogJumpTutorialSeen` field as `false`.

**Workflow**
- Commit straight to `master`. **No `Co-Authored-By` trailer** in commit messages.
- The Unity Editor must be closed while `tools/run-unity-tests.sh` runs. Batch mode cannot open an already-open project.
- Unity creates `.meta` files for new `.cs` files during the first test run. `git add` each new file's `.meta` alongside it.
- Never commit `Assets/_Project/Scripts/Gameplay/Sprint/RunnerBreathing.cs.meta`. It is the user's untracked file.

## Review Focus

1. **Existing PlayMode tests hang if a loaded scene auto-opens the guide.** The guide sets `timeScale = 0`, so `WaitForSeconds` never returns.
   - Expect every PlayMode assembly to have the `GuideAutoOpenOffForPlayModeTests` fixture (Task 5, Step 5).
   - Expect the full PlayMode run to finish (Task 14).
2. **Opening the review guide from the pause menu, then closing it, must leave the game paused.** Resuming afterwards must restore the time scale saved before the pause, not 1 and not 0.
   - Expect: `PausePanel_GuideReviewKeepsTheGamePausedUntilResume` (Task 6, Step 1).
3. **A scene unloading while a holder is frozen must not leave `timeScale` at 0** in the next scene.
   - Expect the panel and pause menu to release in `OnDestroy`: `DestroyingAFirstRunPanelUnfreezes` (Task 4, Step 1).
4. **Sprint's start gate runs on unscaled time.** Without a fix, the 1.5 s gate and the countdown run underneath the guide and the race starts while the player is still reading.
   - Expect: `SprintStart_HoldsTheGateWhileTheGameIsFrozen` (Task 7, Step 6).
5. **The FrogJump flag must survive a save round trip.** `JourneySaveMigration.Normalize` rebuilds `SaveData` field by field, so a missed copy silently drops the flag.
   - Expect: `Migrate_KeepsTheFrogJumpTutorialFlag` (Task 3, Step 1).

---

## File Structure

| File | Responsibility |
|---|---|
| `Assets/_Project/Scripts/UI/TutorialStep.cs` (new, moved out of `TutorialOverlay.cs`) | Page data: title + instruction |
| `Assets/_Project/Scripts/UI/GuideMode.cs` (new) | `FirstRun` / `Review` |
| `Assets/_Project/Scripts/UI/IMinigameGuideSource.cs` (new) | `GuideKey`, `BuildGuide()` |
| `Assets/_Project/Scripts/UI/MinigameGuidePages.cs` (new) | Shared failure page, page list helper, number formatting |
| `Assets/_Project/Scripts/UI/GuideNavigator.cs` (new) | Pure paging state and button labels |
| `Assets/_Project/Scripts/UI/GameFreeze.cs` (new) | Holder-counted `timeScale` + `IPauseAware` freeze |
| `Assets/_Project/Scripts/UI/MinigameGuidePanel.cs` (new) | Runtime-built guide card (canvas 950) |
| `Assets/_Project/Scripts/UI/MinigameGuideHost.cs` (new) | Auto-install, first-run open, review open, seen flag |
| `Assets/_Project/Scripts/UI/PausePanel.cs` | Uses `GameFreeze`; HƯỚNG DẪN button; row layout |
| `Assets/_Project/Scripts/UI/TutorialSeenStore.cs` | `"FrogJump"` key |
| `Assets/_Project/Scripts/Progression/SaveData.cs`, `Progression/Journey/JourneySaveMigration.cs`, `Core/GameManager.cs` | `frogJumpTutorialSeen` |
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintGuide.cs` (new), `SprintController.cs`, `SprintStartPresentation.cs` | Sprint pages; frozen gate fix |
| `Assets/_Project/ScriptableObjects/Journey/sprint_practice.asset`, `sprint_exam.asset` | Objective text matches the limits |
| `Assets/_Project/Scripts/Gameplay/Football/FootballGuide.cs` (new), `FootballController.cs` | Football pages |
| `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballGuide.cs` (new), `VolleyballController.cs` | Volleyball pages |
| `Assets/_Project/Scripts/Gameplay/FrogJump/FrogJumpGuide.cs` (new), `FrogJumpController.cs` | FrogJump pages |
| `Assets/_Project/Scripts/Gameplay/Chess/ChessGuide.cs` (new), `ChessFinalController.cs` | Chess pages; `TimeWords` moves here |
| `Assets/_Project/Scripts/UI/TutorialOverlay.cs` (deleted), `PhaseOverlay.cs`, `Assets/_Project/Prefabs/UI/PhaseOverlay.prefab` | Remove the dead overlay |
| Tests | Listed per task |

---

### Task 0: Preflight and baseline

**Files:** none

- [ ] **Step 1: Check the working tree**

Run: `git status --short`

Expect only `?? Assets/_Project/Scripts/Gameplay/Sprint/RunnerBreathing.cs.meta`. If any scene, prefab or asset shows ` M`, **stop and ask the user**. Never commit, stash or revert the user's files yourself.

The full EditMode run includes scene-configurator tests that rewrite `MG_Volleyball.unity`, `MG_Football.unity` and `MG_FrogJump.unity`.

- [ ] **Step 2: Baseline both suites (Unity Editor closed)**

Run:

```bash
tools/run-unity-tests.sh EditMode "" baseline-edit
tools/run-unity-tests.sh PlayMode "" baseline-play
git status --short
```

Record the passed/failed counts and the names of any failing tests. Later tasks must not add failures beyond this baseline.

Configurator tests may have rewritten scenes. If `git status` now shows scene files modified that were clean in Step 1, restore them with `git checkout -- <those files>`.

---

### Task 1: Guide data, pages helper and navigator

**Files:**
- Create: `Assets/_Project/Scripts/UI/TutorialStep.cs`, `Assets/_Project/Scripts/UI/GuideMode.cs`, `Assets/_Project/Scripts/UI/IMinigameGuideSource.cs`, `Assets/_Project/Scripts/UI/MinigameGuidePages.cs`, `Assets/_Project/Scripts/UI/GuideNavigator.cs`
- Modify: `Assets/_Project/Scripts/UI/TutorialOverlay.cs` (remove the `TutorialStep` class; it moves to its own file)
- Test: `Assets/Tests/EditMode/Presentation/GuideNavigatorTests.cs`, `Assets/Tests/EditMode/Presentation/MinigameGuidePagesTests.cs`

**Interfaces:**
- Produces:
  - `TutorialStep(string title, string instruction, Sprite icon = null, string animationKey = null)` with `Title` and `Instruction`. Unchanged API.
  - `enum GuideMode { FirstRun, Review }`
  - `interface IMinigameGuideSource { string GuideKey { get; } IReadOnlyList<TutorialStep> BuildGuide(); }`
  - `MinigameGuidePages`:
    - `FailureTitle`, `FailureBody`
    - `TutorialStep FailurePage(ChallengeKind? kind)`
    - `IReadOnlyList<TutorialStep> Pages(params TutorialStep[] pages)` (drops nulls)
    - `string Number(float value)`
  - `GuideNavigator(IReadOnlyList<TutorialStep> pages, GuideMode mode)`:
    - Properties: `Mode`, `Index`, `Count`, `Current`, `CanGoBack`, `IsLast`, `ShowsSkip`, `PrimaryLabel`, `Progress`
    - Methods: `bool Primary()` returns true when the guide should close; `void Back()`
    - Constants: `NextLabel`, `StartLabel`, `CloseLabel`, `BackLabel`, `SkipLabel`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Presentation/GuideNavigatorTests.cs`:

```csharp
using System;
using KMA.Gameplay.UI;
using NUnit.Framework;

namespace KMA.Tests.Presentation
{
    public sealed class GuideNavigatorTests
    {
        static readonly TutorialStep[] ThreePages =
        {
            new TutorialStep("A", "a"), new TutorialStep("B", "b"), new TutorialStep("C", "c")
        };

        [Test]
        public void FirstRunWalksForwardAndFinishesOnBatDau()
        {
            var navigator = new GuideNavigator(ThreePages, GuideMode.FirstRun);
            Assert.That(navigator.Index, Is.EqualTo(0));
            Assert.That(navigator.CanGoBack, Is.False);
            Assert.That(navigator.ShowsSkip, Is.True);
            Assert.That(navigator.PrimaryLabel, Is.EqualTo("TIẾP"));
            Assert.That(navigator.Progress, Is.EqualTo("1 / 3"));

            Assert.That(navigator.Primary(), Is.False);
            Assert.That(navigator.Primary(), Is.False);
            Assert.That(navigator.IsLast, Is.True);
            Assert.That(navigator.Current.Title, Is.EqualTo("C"));
            Assert.That(navigator.PrimaryLabel, Is.EqualTo("BẮT ĐẦU"));
            Assert.That(navigator.ShowsSkip, Is.False, "the last page already starts the game");
            Assert.That(navigator.Primary(), Is.True, "primary on the last page closes the guide");
            Assert.That(navigator.Index, Is.EqualTo(2));
        }

        [Test]
        public void ReviewNeverSkipsAndClosesOnDong()
        {
            var navigator = new GuideNavigator(ThreePages, GuideMode.Review);
            Assert.That(navigator.ShowsSkip, Is.False);
            navigator.Primary();
            navigator.Primary();
            Assert.That(navigator.PrimaryLabel, Is.EqualTo("ĐÓNG"));
        }

        [Test]
        public void BackStopsAtTheFirstPage()
        {
            var navigator = new GuideNavigator(ThreePages, GuideMode.FirstRun);
            navigator.Back();
            Assert.That(navigator.Index, Is.EqualTo(0));
            navigator.Primary();
            Assert.That(navigator.CanGoBack, Is.True);
            navigator.Back();
            Assert.That(navigator.Index, Is.EqualTo(0));
        }

        [Test]
        public void SinglePageGuideStartsOnItsOnlyPage()
        {
            var navigator = new GuideNavigator(new[] { new TutorialStep("A", "a") }, GuideMode.FirstRun);
            Assert.That(navigator.IsLast, Is.True);
            Assert.That(navigator.PrimaryLabel, Is.EqualTo("BẮT ĐẦU"));
            Assert.That(navigator.Primary(), Is.True);
        }

        [Test]
        public void EmptyGuideIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new GuideNavigator(Array.Empty<TutorialStep>(), GuideMode.Review));
            Assert.Throws<ArgumentException>(() => new GuideNavigator(null, GuideMode.Review));
        }
    }
}
```

`Assets/Tests/EditMode/Presentation/MinigameGuidePagesTests.cs`:

```csharp
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;

namespace KMA.Tests.Presentation
{
    public sealed class MinigameGuidePagesTests
    {
        [TestCase(ChallengeKind.Practice)]
        [TestCase(ChallengeKind.Exam)]
        public void PenalizedKindsGetTheFailurePage(ChallengeKind kind)
        {
            TutorialStep page = MinigameGuidePages.FailurePage(kind);
            Assert.That(page, Is.Not.Null);
            Assert.That(page.Title, Is.EqualTo("NẾU TRƯỢT"));
            Assert.That(page.Instruction, Is.EqualTo(
                "Trượt lần đầu: phải qua Nhảy ếch để giữ mạng. Trượt từ lần hai: mất 1 mạng và vẫn phải nhảy ếch."));
        }

        [TestCase(ChallengeKind.Learn)]
        [TestCase(ChallengeKind.Final)]
        public void UnpenalizedKindsHaveNoFailurePage(ChallengeKind kind) =>
            Assert.That(MinigameGuidePages.FailurePage(kind), Is.Null);

        [Test]
        public void FreePlayHasNoFailurePage() => Assert.That(MinigameGuidePages.FailurePage(null), Is.Null);

        [Test]
        public void PagesDropsMissingPages()
        {
            var pages = MinigameGuidePages.Pages(new TutorialStep("A", "a"), null, new TutorialStep("B", "b"));
            Assert.That(pages.Count, Is.EqualTo(2));
            Assert.That(pages[1].Title, Is.EqualTo("B"));
        }

        [TestCase(150f, "150")]
        [TestCase(2.5f, "2,5")]
        [TestCase(90f, "90")]
        public void NumbersUseTheVietnameseDecimalComma(float value, string expected) =>
            Assert.That(MinigameGuidePages.Number(value), Is.EqualTo(expected));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Presentation.GuideNavigatorTests|KMA.Tests.Presentation.MinigameGuidePagesTests" t1-guide`

Expected: no results file, with `error CS0246` for `GuideNavigator` and `MinigameGuidePages` (compile failure).

- [ ] **Step 3: Move `TutorialStep` and add the new types**

In `Assets/_Project/Scripts/UI/TutorialOverlay.cs`, delete the whole `[Serializable] public sealed class TutorialStep { ... }` block. It sits between the namespace's opening brace and `public sealed class TutorialOverlay`. Leave the rest of the file unchanged.

Create `Assets/_Project/Scripts/UI/TutorialStep.cs`:

```csharp
using System;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    [Serializable]
    public sealed class TutorialStep
    {
        [SerializeField] string title;
        [SerializeField] string instruction;
        [SerializeField] Sprite icon;
        [SerializeField] string animationKey;

        public string Title => title ?? string.Empty;
        public string Instruction => instruction ?? string.Empty;
        public Sprite Icon => icon;
        public string AnimationKey => animationKey ?? string.Empty;

        public TutorialStep(string title, string instruction, Sprite icon = null, string animationKey = null)
        {
            this.title = title;
            this.instruction = instruction;
            this.icon = icon;
            this.animationKey = animationKey;
        }
    }
}
```

Create `Assets/_Project/Scripts/UI/GuideMode.cs`:

```csharp
namespace KMA.Gameplay.UI
{
    /// FirstRun opens by itself on the first visit and freezes the game; Review opens from the pause menu.
    public enum GuideMode { FirstRun, Review }
}
```

Create `Assets/_Project/Scripts/UI/IMinigameGuideSource.cs`:

```csharp
using System.Collections.Generic;

namespace KMA.Gameplay.UI
{
    /// A minigame that can explain itself. BuildGuide runs when the guide opens, so the pages carry
    /// the numbers of the lesson that is loaded at that moment.
    public interface IMinigameGuideSource
    {
        string GuideKey { get; }
        IReadOnlyList<TutorialStep> BuildGuide();
    }
}
```

Create `Assets/_Project/Scripts/UI/MinigameGuidePages.cs`:

```csharp
using System.Collections.Generic;
using System.Globalization;

namespace KMA.Gameplay.UI
{
    public static class MinigameGuidePages
    {
        public const string FailureTitle = "NẾU TRƯỢT";
        public const string FailureBody =
            "Trượt lần đầu: phải qua Nhảy ếch để giữ mạng. Trượt từ lần hai: mất 1 mạng và vẫn phải nhảy ếch.";

        static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

        /// Practice and exam failures send the player to the frog jump (JourneyProgress.IsPenalizedKind).
        public static TutorialStep FailurePage(ChallengeKind? kind) =>
            kind == ChallengeKind.Practice || kind == ChallengeKind.Exam
                ? new TutorialStep(FailureTitle, FailureBody)
                : null;

        public static IReadOnlyList<TutorialStep> Pages(params TutorialStep[] pages)
        {
            var list = new List<TutorialStep>(pages.Length);
            foreach (TutorialStep page in pages)
                if (page != null)
                    list.Add(page);
            return list;
        }

        /// Whole numbers print bare ("150"); fractions use the Vietnamese comma ("2,5").
        public static string Number(float value) => value.ToString("0.#", Vi);
    }
}
```

Create `Assets/_Project/Scripts/UI/GuideNavigator.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace KMA.Gameplay.UI
{
    public sealed class GuideNavigator
    {
        public const string NextLabel = "TIẾP";
        public const string StartLabel = "BẮT ĐẦU";
        public const string CloseLabel = "ĐÓNG";
        public const string BackLabel = "QUAY LẠI";
        public const string SkipLabel = "BỎ QUA";

        readonly IReadOnlyList<TutorialStep> pages;

        public GuideNavigator(IReadOnlyList<TutorialStep> pages, GuideMode mode)
        {
            if (pages == null || pages.Count == 0)
                throw new ArgumentException("A guide needs at least one page.", nameof(pages));
            this.pages = pages;
            Mode = mode;
        }

        public GuideMode Mode { get; }
        public int Index { get; private set; }
        public int Count => pages.Count;
        public TutorialStep Current => pages[Index];
        public bool CanGoBack => Index > 0;
        public bool IsLast => Index == pages.Count - 1;
        public bool ShowsSkip => Mode == GuideMode.FirstRun && !IsLast;
        public string PrimaryLabel => !IsLast ? NextLabel : Mode == GuideMode.FirstRun ? StartLabel : CloseLabel;
        public string Progress => $"{Index + 1} / {Count}";

        /// Turns the page; on the last page it returns true, meaning the guide should close.
        public bool Primary()
        {
            if (IsLast)
                return true;
            Index++;
            return false;
        }

        public void Back()
        {
            if (CanGoBack)
                Index--;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Presentation.GuideNavigatorTests|KMA.Tests.Presentation.MinigameGuidePagesTests|KMA.Tests.Presentation.TutorialOverlayTests" t1-guide`

Expected: `failed=0`. `TutorialOverlayTests` still compiles and passes because `TutorialStep` only moved.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/UI/TutorialStep.cs* Assets/_Project/Scripts/UI/GuideMode.cs* \
  Assets/_Project/Scripts/UI/IMinigameGuideSource.cs* Assets/_Project/Scripts/UI/MinigameGuidePages.cs* \
  Assets/_Project/Scripts/UI/GuideNavigator.cs* Assets/_Project/Scripts/UI/TutorialOverlay.cs \
  Assets/Tests/EditMode/Presentation/GuideNavigatorTests.cs* Assets/Tests/EditMode/Presentation/MinigameGuidePagesTests.cs*
git commit -m "feat(guide): add guide pages, paging state and the guide source contract"
```

---

### Task 2: Shared `GameFreeze`; `PausePanel` uses it

**Files:**
- Create: `Assets/_Project/Scripts/UI/GameFreeze.cs`
- Modify: `Assets/_Project/Scripts/UI/PausePanel.cs` (`Open`, `Resume`, the `previousTimeScale` field, a new `OnDestroy`)
- Test: `Assets/Tests/EditMode/Presentation/GameFreezeTests.cs`

**Interfaces:**
- Produces: `static class GameFreeze` with:
  - `void Acquire(object holder)`
  - `void Release(object holder)`
  - `bool IsFrozen`
  - `bool Holds(object holder)`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Presentation/GameFreezeTests.cs`:

```csharp
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class GameFreezeTests
    {
        GameObject probeObject;
        PauseProbe probe;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = .5f;
            probeObject = new GameObject("PauseProbe");
            probe = probeObject.AddComponent<PauseProbe>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(probeObject);
            Time.timeScale = 1f;
        }

        [Test]
        public void TheLastReleaseRestoresTheSavedScale()
        {
            object pause = new object(), guide = new object();
            GameFreeze.Acquire(pause);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(probe.Paused, Is.True);

            GameFreeze.Acquire(guide);
            GameFreeze.Release(pause);
            Assert.That(Time.timeScale, Is.Zero, "the guide still holds the freeze");
            Assert.That(GameFreeze.IsFrozen, Is.True);

            GameFreeze.Release(guide);
            Assert.That(Time.timeScale, Is.EqualTo(.5f));
            Assert.That(probe.Paused, Is.False);
            Assert.That(GameFreeze.IsFrozen, Is.False);
        }

        [Test]
        public void RepeatedCallsByOneHolderCountOnce()
        {
            object holder = new object();
            GameFreeze.Acquire(holder);
            GameFreeze.Acquire(holder);
            Assert.That(probe.PauseCalls, Is.EqualTo(1));
            GameFreeze.Release(holder);
            Assert.That(Time.timeScale, Is.EqualTo(.5f));
            GameFreeze.Release(holder);
            Assert.That(Time.timeScale, Is.EqualTo(.5f), "an extra release must not touch the scale");
            Assert.That(probe.ResumeCalls, Is.EqualTo(1));
        }

        [Test]
        public void ReleasingAnUnknownHolderDoesNothing()
        {
            GameFreeze.Release(new object());
            Assert.That(Time.timeScale, Is.EqualTo(.5f));
            Assert.That(probe.ResumeCalls, Is.Zero);
        }

        sealed class PauseProbe : MonoBehaviour, IPauseAware
        {
            public bool Paused;
            public int PauseCalls, ResumeCalls;

            public void SetPaused(bool paused)
            {
                Paused = paused;
                if (paused) PauseCalls++;
                else ResumeCalls++;
            }
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Presentation.GameFreezeTests" t2-freeze`

Expected: compile failure, `GameFreeze` does not exist.

- [ ] **Step 3: Implement `GameFreeze`**

`Assets/_Project/Scripts/UI/GameFreeze.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    /// Freezes gameplay while any holder (the pause menu, the first-run guide) is open. The first
    /// Acquire saves Time.timeScale and pauses every IPauseAware; the last Release restores both.
    public static class GameFreeze
    {
        static readonly HashSet<object> holders = new HashSet<object>();
        static float previousTimeScale = 1f;

        public static bool IsFrozen => holders.Count > 0;
        public static bool Holds(object holder) => holder != null && holders.Contains(holder);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            holders.Clear();
            previousTimeScale = 1f;
        }

        public static void Acquire(object holder)
        {
            if (holder == null || !holders.Add(holder) || holders.Count > 1)
                return;
            previousTimeScale = Time.timeScale;
            NotifyPauseAware(true);
            Time.timeScale = 0f;
        }

        public static void Release(object holder)
        {
            if (holder == null || !holders.Remove(holder) || holders.Count > 0)
                return;
            NotifyPauseAware(false);
            Time.timeScale = previousTimeScale;
        }

        static void NotifyPauseAware(bool paused)
        {
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (behaviour is IPauseAware pauseAware)
                    pauseAware.SetPaused(paused);
        }
    }
}
```

- [ ] **Step 4: Switch `PausePanel` to `GameFreeze`**

In `Assets/_Project/Scripts/UI/PausePanel.cs`:
- Delete the field `float previousTimeScale = 1f;`.
- Replace `Open()` and `Resume()` with the code below.
- Add `OnDestroy`.

```csharp
        public void Open()
        {
            if (IsOpen)
                return;
            GameFreeze.Acquire(this);
            IsOpen = true;
            SetMenuVisible(true);
        }

        public void Resume()
        {
            if (!IsOpen)
                return;
            GameFreeze.Release(this);
            IsOpen = false;
            SetMenuVisible(false);
        }

        // A scene change while paused must not carry timeScale 0 into the next scene.
        void OnDestroy() => GameFreeze.Release(this);
```

- [ ] **Step 5: Run the tests to verify they pass**

Run:

```bash
tools/run-unity-tests.sh EditMode "KMA.Tests.Presentation.GameFreezeTests" t2-freeze
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Core.PauseFlowTests" t2-pause
```

Expected: both `failed=0`. `PausePanel_RestoresPreviousTimeScaleAndRaisesActionsOnce` still restores `.5`.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/UI/GameFreeze.cs* Assets/_Project/Scripts/UI/PausePanel.cs \
  Assets/Tests/EditMode/Presentation/GameFreezeTests.cs*
git commit -m "refactor(pause): share a holder-counted game freeze between pause and guide"
```

---

### Task 3: Persisted seen flag for FrogJump

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/SaveData.cs`
- Modify: `Assets/_Project/Scripts/Progression/Journey/JourneySaveMigration.cs` (the `return new SaveData { ... }` in `Normalize`, near line 138)
- Modify: `Assets/_Project/Scripts/Core/GameManager.cs` (field near line 31, methods next to `MarkTutorialSeen` near line 145, load near line 193, saves near lines 237 and 292)
- Modify: `Assets/_Project/Scripts/UI/TutorialSeenStore.cs`
- Test: `Assets/Tests/PlayMode/Core/GameManagerStartupTests.cs`

**Interfaces:**
- Produces:
  - `SaveData.frogJumpTutorialSeen` (bool)
  - `GameManager.HasSeenFrogJumpTutorial` (bool property)
  - `GameManager.MarkFrogJumpTutorialSeen()`
  - `SaveDataTutorialSeenStore.FrogJumpKey = "FrogJump"`

- [ ] **Step 1: Write the failing tests**

Add these two tests inside `GameManagerStartupTests`, right after `SaveDataTutorialSeenStore_MarksSprintInTheAuthoritativeSave`:

```csharp
        [Test]
        public void SaveDataTutorialSeenStore_MarksFrogJumpInTheAuthoritativeSave()
        {
            SceneRouter router = CreateRouter();
            SaveData persisted = SaveData.CreateDefault();
            var saves = 0;
            GameManager manager = CreateInactiveManager();
            manager.ConfigureStartup(
                () => persisted,
                data =>
                {
                    saves++;
                    persisted = data;
                },
                router,
                _ => { });
            manager.gameObject.SetActive(true);
            var fallback = new MemoryTutorialSeenStore();
            var store = new SaveDataTutorialSeenStore(fallback);

            Assert.That(store.HasSeen(SaveDataTutorialSeenStore.FrogJumpKey), Is.False);
            store.MarkSeen(SaveDataTutorialSeenStore.FrogJumpKey);

            Assert.That(manager.HasSeenFrogJumpTutorial, Is.True);
            Assert.That(store.HasSeen(SaveDataTutorialSeenStore.FrogJumpKey), Is.True);
            Assert.That(persisted.frogJumpTutorialSeen, Is.True);
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(fallback.HasSeen(SaveDataTutorialSeenStore.FrogJumpKey), Is.False);
            Assert.That(persisted.tutorialSeen, Has.None.True, "the frog flag must not touch subject flags");
        }

        [Test]
        public void Migrate_KeepsTheFrogJumpTutorialFlag()
        {
            SaveData data = SaveData.CreateDefault();
            data.frogJumpTutorialSeen = true;

            SaveData migrated = new SaveSystem().Migrate(data);

            Assert.That(migrated.frogJumpTutorialSeen, Is.True);
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Core.GameManagerStartupTests" t3-frog-flag`

Expected: compile failure on `frogJumpTutorialSeen` and `FrogJumpKey`.

- [ ] **Step 3: Add the field and its plumbing**

`SaveData.cs`: add after `public bool[] tutorialSeen;`:

```csharp
        // The frog jump has no SubjectId, so its guide flag lives outside tutorialSeen.
        public bool frogJumpTutorialSeen;
```

`JourneySaveMigration.cs`, in `Normalize`'s `return new SaveData { ... }`: add after `tutorialSeen = tutorials,`:

```csharp
                frogJumpTutorialSeen = source.frogJumpTutorialSeen,
```

`GameManager.cs`:
- Field: add `bool frogJumpTutorialSeen;` under `bool[] tutorialSeen;`.
- In the initialize block, after `tutorialSeen = CloneTutorialFlags(loaded.tutorialSeen);`, add `frogJumpTutorialSeen = loaded.frogJumpTutorialSeen;`.
- In **both** places that write `current.tutorialSeen = CloneTutorialFlags(tutorialSeen);` (the journey persist try-block and `SaveCurrentState`), add `current.frogJumpTutorialSeen = frogJumpTutorialSeen;` on the next line.
- After `MarkTutorialSeen`, add:

```csharp
        public bool HasSeenFrogJumpTutorial => frogJumpTutorialSeen;

        public void MarkFrogJumpTutorialSeen()
        {
            if (!initialized)
                throw new InvalidOperationException("GameManager has not initialized.");
            if (frogJumpTutorialSeen)
                return;
            frogJumpTutorialSeen = true;
            SaveCurrentState();
        }
```

`TutorialSeenStore.cs`: replace the body of `SaveDataTutorialSeenStore` from `HasSeen` through `MarkSeen` with:

```csharp
        public const string FrogJumpKey = "FrogJump";

        public bool HasSeen(string subjectId)
        {
            var manager = GameManager.Instance;
            if (manager == null || !manager.IsInitialized)
                return fallback.HasSeen(subjectId);
            if (IsFrogJump(subjectId))
                return manager.HasSeenFrogJumpTutorial;
            return TryParseSubject(subjectId, out var subject)
                ? manager.HasSeenTutorial(subject)
                : fallback.HasSeen(subjectId);
        }

        public void MarkSeen(string subjectId)
        {
            var manager = GameManager.Instance;
            if (manager == null || !manager.IsInitialized)
                fallback.MarkSeen(subjectId);
            else if (IsFrogJump(subjectId))
                manager.MarkFrogJumpTutorialSeen();
            else if (TryParseSubject(subjectId, out var subject))
                manager.MarkTutorialSeen(subject);
            else
                fallback.MarkSeen(subjectId);
        }

        static bool IsFrogJump(string value) =>
            string.Equals(value, FrogJumpKey, System.StringComparison.OrdinalIgnoreCase);
```

Keep the existing `TryParseSubject` helper.

- [ ] **Step 4: Run the tests to verify they pass**

Run:

```bash
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Core.GameManagerStartupTests|KMA.Tests.Gameplay.Core.S4BootstrapPersistenceGateTests" t3-frog-flag
tools/run-unity-tests.sh EditMode "KMA.Tests.Presentation.UIThemeTests" t3-store
```

Expected: `failed=0` for both.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Progression/SaveData.cs Assets/_Project/Scripts/Progression/Journey/JourneySaveMigration.cs \
  Assets/_Project/Scripts/Core/GameManager.cs Assets/_Project/Scripts/UI/TutorialSeenStore.cs \
  Assets/Tests/PlayMode/Core/GameManagerStartupTests.cs
git commit -m "feat(save): persist whether the frog jump guide was seen"
```

---

### Task 4: `MinigameGuidePanel`

**Files:**
- Create: `Assets/_Project/Scripts/UI/MinigameGuidePanel.cs`
- Test: `Assets/Tests/PlayMode/Presentation/MinigameGuidePanelTests.cs`. These are PlayMode tests because EditMode does not call `Awake`.

**Interfaces:**
- Consumes: `GuideNavigator`, `GuideMode`, `TutorialStep`, `GameFreeze` (Tasks 1–2).
- Produces: `MinigameGuidePanel` with:
  - `const int SortingOrder = 950`
  - `static MinigameGuidePanel Create(Transform parent)`
  - `void Open(IReadOnlyList<TutorialStep> pages, GuideMode mode)`
  - Actions: `PressPrimary()`, `PressBack()`, `PressSkip()`
  - State: `bool IsOpen`, `GuideMode Mode`, `int PageIndex`, `TutorialStep CurrentPage`
  - Rendered text: `string PrimaryText`, `string ProgressText`
  - Button state: `bool SkipVisible`, `bool BackInteractable`
  - `event Action<GuideMode> Closed`
  - Children: `Canvas`; `Scrim/Card/{Title,Progress,Body,SkipButton,BackButton,PrimaryButton}`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/PlayMode/Presentation/MinigameGuidePanelTests.cs`:

```csharp
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class MinigameGuidePanelTests
    {
        static readonly TutorialStep[] Pages =
        {
            new TutorialStep("MỤC TIÊU", "Chạy 150 m."), new TutorialStep("LUẬT", "Đừng bấm sai.")
        };

        GameObject host;
        MinigameGuidePanel panel;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            host = new GameObject("GuideHostRoot");
            panel = MinigameGuidePanel.Create(host.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            Time.timeScale = 1f;
        }

        [Test]
        public void PanelStartsHiddenAndDrawsAboveThePauseMenu()
        {
            Canvas canvas = panel.GetComponent<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.sortingOrder, Is.EqualTo(950));
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(panel.transform.Find("Scrim").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void FirstRunFreezesPagesAndUnfreezesOnBatDau()
        {
            GuideMode? closed = null;
            panel.Closed += mode => closed = mode;
            panel.Open(Pages, GuideMode.FirstRun);

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(panel.CurrentPage.Title, Is.EqualTo("MỤC TIÊU"));
            Assert.That(panel.ProgressText, Is.EqualTo("1 / 2"));
            Assert.That(panel.SkipVisible, Is.True);
            Assert.That(panel.BackInteractable, Is.False);

            panel.transform.Find("Scrim/Card/PrimaryButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(panel.PageIndex, Is.EqualTo(1));
            Assert.That(panel.BackInteractable, Is.True);
            Assert.That(panel.SkipVisible, Is.False);
            Assert.That(panel.PrimaryText, Does.Contain("B"), "the label is the VietText-fixed BẮT ĐẦU");

            panel.PressPrimary();
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(closed, Is.EqualTo(GuideMode.FirstRun));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void SkipClosesAFirstRunGuideFromAnyPage()
        {
            panel.Open(Pages, GuideMode.FirstRun);
            panel.PressSkip();
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void ReviewDoesNotTouchTheTimeScaleAndHasNoSkip()
        {
            panel.Open(Pages, GuideMode.Review);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(panel.SkipVisible, Is.False);
            panel.PressSkip();
            Assert.That(panel.IsOpen, Is.True, "review closes only through ĐÓNG");
            panel.PressPrimary();
            panel.PressPrimary();
            Assert.That(panel.IsOpen, Is.False);
        }

        [Test]
        public void OpeningTwiceKeepsTheFirstGuide()
        {
            panel.Open(Pages, GuideMode.FirstRun);
            panel.PressPrimary();
            panel.Open(Pages, GuideMode.Review);
            Assert.That(panel.Mode, Is.EqualTo(GuideMode.FirstRun));
            Assert.That(panel.PageIndex, Is.EqualTo(1));
        }

        [Test]
        public void EmptyPagesDoNotOpen()
        {
            panel.Open(new TutorialStep[0], GuideMode.FirstRun);
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void DestroyingAFirstRunPanelUnfreezes()
        {
            panel.Open(Pages, GuideMode.FirstRun);
            Object.DestroyImmediate(host);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Presentation.MinigameGuidePanelTests" t4-panel`

Expected: compile failure, `MinigameGuidePanel` does not exist.

- [ ] **Step 3: Implement the panel**

`Assets/_Project/Scripts/UI/MinigameGuidePanel.cs`:

```csharp
using System;
using System.Collections.Generic;
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    /// The how-to-play card. Its own overlay canvas sorts above the pause menu (900), so it can
    /// open from there; the scrim blocks every control underneath.
    public sealed class MinigameGuidePanel : MonoBehaviour
    {
        public const int SortingOrder = 950;
        static readonly Vector2 Centre = new Vector2(.5f, .5f);

        GameObject scrim;
        TMP_Text titleLabel;
        TMP_Text progressLabel;
        TMP_Text bodyLabel;
        Button skipButton;
        Button backButton;
        Button primaryButton;
        TMP_Text primaryLabel;
        GuideNavigator navigator;

        public event Action<GuideMode> Closed;

        public bool IsOpen => navigator != null;
        public GuideMode Mode => navigator?.Mode ?? GuideMode.Review;
        public int PageIndex => navigator?.Index ?? -1;
        public TutorialStep CurrentPage => navigator?.Current;
        public string PrimaryText => primaryLabel != null ? primaryLabel.text : string.Empty;
        public string ProgressText => progressLabel != null ? progressLabel.text : string.Empty;
        public bool SkipVisible => skipButton != null && skipButton.gameObject.activeSelf;
        public bool BackInteractable => backButton != null && backButton.interactable;

        public static MinigameGuidePanel Create(Transform parent)
        {
            var root = new GameObject(nameof(MinigameGuidePanel), typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var panel = root.AddComponent<MinigameGuidePanel>();
            panel.Build();
            return panel;
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform scrimRect = UiKit.Rect(transform, "Scrim");
            UiKit.Stretch(scrimRect);
            Image scrimImage = scrimRect.gameObject.AddComponent<Image>();
            scrimImage.color = MinigameUiTheme.Scrim;
            scrimImage.raycastTarget = true;
            scrim = scrimRect.gameObject;

            Image card = UiKit.Panel(scrimRect, "Card");
            card.raycastTarget = true;
            UiKit.Place(card.rectTransform, Centre, Centre, Vector2.zero, new Vector2(1200f, 720f));

            titleLabel = UiKit.Label(card.transform, "Title", string.Empty, MinigameUiTheme.Title,
                MinigameUiTheme.TextPrimary);
            UiKit.Place(titleLabel.rectTransform, Centre, Centre, new Vector2(-80f, 280f), new Vector2(900f, 80f));

            progressLabel = UiKit.Label(card.transform, "Progress", string.Empty, MinigameUiTheme.Caption,
                MinigameUiTheme.Accent, TextAlignmentOptions.Right);
            UiKit.Place(progressLabel.rectTransform, Centre, Centre, new Vector2(470f, 280f), new Vector2(180f, 60f));

            bodyLabel = UiKit.Label(card.transform, "Body", string.Empty, MinigameUiTheme.BodyLarge,
                MinigameUiTheme.TextPrimary, TextAlignmentOptions.Left);
            UiKit.Place(bodyLabel.rectTransform, Centre, Centre, new Vector2(0f, 20f), new Vector2(1080f, 400f));
            UiKit.FitLabel(bodyLabel, MinigameUiTheme.BodyLarge);

            skipButton = CreateButton(card.transform, "SkipButton", GuideNavigator.SkipLabel, -380f,
                ButtonVariant.Secondary).Button;
            backButton = CreateButton(card.transform, "BackButton", GuideNavigator.BackLabel, 0f,
                ButtonVariant.Secondary).Button;
            ButtonHandle primary = CreateButton(card.transform, "PrimaryButton", GuideNavigator.NextLabel, 380f,
                ButtonVariant.Primary);
            primaryButton = primary.Button;
            primaryLabel = primary.Label;

            skipButton.onClick.AddListener(PressSkip);
            backButton.onClick.AddListener(PressBack);
            primaryButton.onClick.AddListener(PressPrimary);
            scrim.SetActive(false);
        }

        static ButtonHandle CreateButton(Transform parent, string name, string label, float x, ButtonVariant variant)
        {
            ButtonHandle handle = UiKit.Button(parent, name, label, variant);
            UiKit.Place((RectTransform)handle.Button.transform, Centre, Centre, new Vector2(x, -270f),
                new Vector2(320f, MinigameUiTheme.ButtonHeight));
            return handle;
        }

        public void Open(IReadOnlyList<TutorialStep> pages, GuideMode mode)
        {
            if (IsOpen || pages == null || pages.Count == 0)
                return;
            navigator = new GuideNavigator(pages, mode);
            if (mode == GuideMode.FirstRun)
                GameFreeze.Acquire(this);
            scrim.transform.SetAsLastSibling();
            scrim.SetActive(true);
            Refresh();
        }

        public void PressPrimary()
        {
            if (navigator == null)
                return;
            if (navigator.Primary())
                Close();
            else
                Refresh();
        }

        public void PressBack()
        {
            if (navigator == null)
                return;
            navigator.Back();
            Refresh();
        }

        public void PressSkip()
        {
            if (navigator != null && navigator.ShowsSkip)
                Close();
        }

        void Close()
        {
            GuideMode mode = navigator.Mode;
            navigator = null;
            scrim.SetActive(false);
            GameFreeze.Release(this);
            Closed?.Invoke(mode);
        }

        void OnDestroy() => GameFreeze.Release(this);

        void Refresh()
        {
            TutorialStep page = navigator.Current;
            titleLabel.text = VietText.Fix(page.Title);
            bodyLabel.text = VietText.Fix(page.Instruction);
            progressLabel.text = VietText.Fix(navigator.Progress);
            primaryLabel.text = VietText.Fix(navigator.PrimaryLabel);
            backButton.interactable = navigator.CanGoBack;
            skipButton.gameObject.SetActive(navigator.ShowsSkip);
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Presentation.MinigameGuidePanelTests" t4-panel`

Expected: `failed=0`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/UI/MinigameGuidePanel.cs* Assets/Tests/PlayMode/Presentation/MinigameGuidePanelTests.cs*
git commit -m "feat(guide): add the runtime-built how-to-play panel"
```

---

### Task 5: `MinigameGuideHost` and the PlayMode test switch

**Files:**
- Create: `Assets/_Project/Scripts/UI/MinigameGuideHost.cs`
- Create: `GuideAutoOpenOffForPlayModeTests.cs` in every PlayMode test folder that has an `.asmdef`:
  - `Assets/Tests/PlayMode/Core/`
  - `Assets/Tests/PlayMode/EditorTools/`
  - `Assets/Tests/PlayMode/Gameplay/Ball/`
  - `Assets/Tests/PlayMode/Gameplay/Celebration/`
  - `Assets/Tests/PlayMode/Gameplay/Chess/`
  - `Assets/Tests/PlayMode/Gameplay/Football/`
  - `Assets/Tests/PlayMode/Gameplay/Running/`
  - `Assets/Tests/PlayMode/Gameplay/Volleyball/`
  - `Assets/Tests/PlayMode/Input/`
  - `Assets/Tests/PlayMode/Presentation/`
  - `Assets/Tests/PlayMode/Progression/`
- Modify: each of those folders' `.asmdef`, adding `"KMA.Gameplay.UI"` to `references` where it is missing
- Test: `Assets/Tests/PlayMode/Presentation/MinigameGuideHostTests.cs`

**Interfaces:**
- Consumes: `MinigameGuidePanel`, `IMinigameGuideSource`, `ITutorialSeenStore`, `SaveDataTutorialSeenStore`.
- Produces: `MinigameGuideHost` with:
  - Statics: `static bool AutoOpenEnabled` (default true), `static MinigameGuideHost Current`
  - `void Configure(IMinigameGuideSource source, ITutorialSeenStore store)`
  - `bool TryOpenFirstRun()`, `bool OpenReview()`
  - `MinigameGuidePanel Panel`, `IMinigameGuideSource Source`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/PlayMode/Presentation/MinigameGuideHostTests.cs`:

```csharp
using System.Collections.Generic;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class MinigameGuideHostTests
    {
        sealed class FakeSource : IMinigameGuideSource
        {
            public int Builds;
            public string GuideKey => "Sprint";

            public IReadOnlyList<TutorialStep> BuildGuide()
            {
                Builds++;
                return new[] { new TutorialStep("A", "a"), new TutorialStep("B", "b") };
            }
        }

        GameObject root;
        MinigameGuideHost host;
        FakeSource source;
        MemoryTutorialSeenStore store;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("Host");
            host = root.AddComponent<MinigameGuideHost>();
            source = new FakeSource();
            store = new MemoryTutorialSeenStore();
            host.Configure(source, store);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Time.timeScale = 1f;
        }

        [Test]
        public void FirstRunOpensOnceAndSkipMarksTheGameSeen()
        {
            Assert.That(MinigameGuideHost.Current, Is.SameAs(host));
            Assert.That(host.TryOpenFirstRun(), Is.True);
            Assert.That(host.Panel.Mode, Is.EqualTo(GuideMode.FirstRun));
            Assert.That(Time.timeScale, Is.Zero);

            host.Panel.PressSkip();
            Assert.That(store.HasSeen("Sprint"), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(host.TryOpenFirstRun(), Is.False, "a seen game does not open by itself again");
        }

        [Test]
        public void FinishingTheFirstRunMarksTheGameSeen()
        {
            host.TryOpenFirstRun();
            host.Panel.PressPrimary();
            host.Panel.PressPrimary();
            Assert.That(store.HasSeen("Sprint"), Is.True);
        }

        [Test]
        public void ReviewOpensEvenWhenSeenAndNeverMarks()
        {
            Assert.That(host.OpenReview(), Is.True);
            Assert.That(host.Panel.Mode, Is.EqualTo(GuideMode.Review));
            host.Panel.PressPrimary();
            host.Panel.PressPrimary();
            Assert.That(store.HasSeen("Sprint"), Is.False);
        }

        [Test]
        public void PagesAreBuiltEachTimeTheGuideOpens()
        {
            host.OpenReview();
            host.Panel.PressPrimary();
            host.Panel.PressPrimary();
            host.OpenReview();
            Assert.That(source.Builds, Is.EqualTo(2));
        }

        [Test]
        public void CurrentClearsWhenTheHostIsDestroyed()
        {
            Object.DestroyImmediate(root);
            Assert.That(MinigameGuideHost.Current == null, Is.True);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Presentation.MinigameGuideHostTests" t5-host`

Expected: compile failure, `MinigameGuideHost` does not exist.

- [ ] **Step 3: Implement the host**

`Assets/_Project/Scripts/UI/MinigameGuideHost.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMA.Gameplay.UI
{
    /// Opens a minigame's how-to-play guide on its first visit and lets the pause menu reopen it.
    /// Installed on every scene load that has an IMinigameGuideSource.
    public sealed class MinigameGuideHost : MonoBehaviour
    {
        /// PlayMode test assemblies turn this off: the first-run guide freezes Time.timeScale.
        public static bool AutoOpenEnabled = true;
        public static MinigameGuideHost Current { get; private set; }

        IMinigameGuideSource source;
        ITutorialSeenStore seenStore;
        MinigameGuidePanel panel;

        public MinigameGuidePanel Panel => panel;
        public IMinigameGuideSource Source => source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            AutoOpenEnabled = true;
            Current = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= Ensure;
            SceneManager.sceneLoaded += Ensure;
            Ensure(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void Ensure(Scene scene, LoadSceneMode mode)
        {
            if (FindFirstObjectByType<MinigameGuideHost>() != null)
                return;
            IMinigameGuideSource found = FindSource();
            if (found == null)
                return;
            new GameObject(nameof(MinigameGuideHost)).AddComponent<MinigameGuideHost>()
                .Configure(found, new SaveDataTutorialSeenStore());
        }

        static IMinigameGuideSource FindSource()
        {
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (behaviour is IMinigameGuideSource guide && behaviour.isActiveAndEnabled)
                    return guide;
            return null;
        }

        public void Configure(IMinigameGuideSource guideSource, ITutorialSeenStore store)
        {
            source = guideSource;
            seenStore = store ?? new SaveDataTutorialSeenStore();
        }

        void Awake()
        {
            Current = this;
            panel = MinigameGuidePanel.Create(transform);
            panel.Closed += OnClosed;
        }

        // Start runs after SceneRouter's sceneLoaded binding, so the lesson numbers are in place.
        void Start()
        {
            if (AutoOpenEnabled)
                TryOpenFirstRun();
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
            if (panel != null)
                panel.Closed -= OnClosed;
        }

        public bool TryOpenFirstRun() =>
            source != null && seenStore != null && !seenStore.HasSeen(source.GuideKey) && Open(GuideMode.FirstRun);

        public bool OpenReview() => source != null && Open(GuideMode.Review);

        bool Open(GuideMode mode)
        {
            if (panel.IsOpen)
                return false;
            IReadOnlyList<TutorialStep> pages = source.BuildGuide();
            if (pages == null || pages.Count == 0)
                return false;
            panel.Open(pages, mode);
            return panel.IsOpen;
        }

        void OnClosed(GuideMode mode)
        {
            if (mode == GuideMode.FirstRun && source != null)
                seenStore.MarkSeen(source.GuideKey);
        }
    }
}
```

- [ ] **Step 4: Run the host tests**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Presentation.MinigameGuideHostTests" t5-host`

Expected: `failed=0`.

- [ ] **Step 5: Turn auto-open off in every PlayMode test assembly**

For each PlayMode folder listed under **Files**:
1. Create `GuideAutoOpenOffForPlayModeTests.cs` with exactly this content. It has no namespace, so the fixture covers the whole assembly. Each assembly gets its own copy.
2. Open that folder's `.asmdef`. If `"KMA.Gameplay.UI"` is not in `references`, add it after `"KMA.Gameplay"`, or as the last entry if `"KMA.Gameplay"` is absent.

```csharp
using KMA.Gameplay.UI;
using NUnit.Framework;

/// Scenes these tests load must not open the first-run guide: it freezes Time.timeScale, so
/// WaitForSeconds would never return. Guide tests call MinigameGuideHost directly instead.
[SetUpFixture]
public sealed class GuideAutoOpenOffForPlayModeTests
{
    [OneTimeSetUp]
    public void DisableGuideAutoOpen() => MinigameGuideHost.AutoOpenEnabled = false;

    [OneTimeTearDown]
    public void RestoreGuideAutoOpen() => MinigameGuideHost.AutoOpenEnabled = true;
}
```

Check: `ls Assets/Tests/PlayMode/*/*.asmdef Assets/Tests/PlayMode/Gameplay/*/*.asmdef | wc -l` must equal `ls Assets/Tests/PlayMode/*/GuideAutoOpenOffForPlayModeTests.cs Assets/Tests/PlayMode/Gameplay/*/GuideAutoOpenOffForPlayModeTests.cs | wc -l`.

- [ ] **Step 6: Run the PlayMode suites that load scenes**

Run: `tools/run-unity-tests.sh PlayMode "" t5-play`

Expected: failures no greater than the Task 0 baseline. Every MG scene now installs a host only after Tasks 7–11 add sources, so nothing opens yet.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/UI/MinigameGuideHost.cs* Assets/Tests/PlayMode
git commit -m "feat(guide): add the guide host that opens on a minigame's first visit"
```

---

### Task 6: HƯỚNG DẪN in the pause menu

**Files:**
- Modify: `Assets/_Project/Scripts/UI/PausePanel.cs`
- Test: `Assets/Tests/PlayMode/Core/PauseFlowTests.cs`

**Interfaces:**
- Consumes: `MinigameGuideHost.Current`, `MinigameGuideHost.OpenReview()` (Task 5).
- Produces:
  - `PausePanel.GuideButtonLabel = "HƯỚNG DẪN"`
  - `PausePanel.OpenGuide()`
  - `PausePanel.GuideAvailable`
  - Menu child `PauseMenu/PauseCard/GuideButton`

- [ ] **Step 1: Write the failing tests**

Add to `PauseFlowTests`:
- `using System.Collections.Generic;` and `using UnityEngine.UI;` at the top.
- The members below inside the class.

```csharp
        sealed class FakeSource : IMinigameGuideSource
        {
            public string GuideKey => "Sprint";
            public IReadOnlyList<TutorialStep> BuildGuide() => new[] { new TutorialStep("A", "a") };
        }

        static (GameObject canvas, PausePanel panel) CreatePause()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var root = new GameObject("PausePanel", typeof(RectTransform));
            root.transform.SetParent(canvasObject.transform, false);
            return (canvasObject, root.AddComponent<PausePanel>());
        }

        static RectTransform Card(GameObject canvas) =>
            (RectTransform)canvas.transform.Find("PauseMenu/PauseCard");

        static Button GuideButton(GameObject canvas) =>
            canvas.transform.Find("PauseMenu/PauseCard/GuideButton").GetComponent<Button>();

        [Test]
        public void PausePanel_HidesTheGuideButtonWithoutAGuide()
        {
            var (canvas, panel) = CreatePause();
            try
            {
                panel.Open();
                Assert.That(panel.GuideAvailable, Is.False);
                Assert.That(GuideButton(canvas).gameObject.activeSelf, Is.False);
                Assert.That(Card(canvas).sizeDelta.y, Is.EqualTo(440f));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void PausePanel_GuideReviewKeepsTheGamePausedUntilResume()
        {
            var (canvas, panel) = CreatePause();
            var hostObject = new GameObject("Host");
            try
            {
                var host = hostObject.AddComponent<MinigameGuideHost>();
                host.Configure(new FakeSource(), new MemoryTutorialSeenStore());
                Time.timeScale = .5f;
                panel.Open();

                Button guide = GuideButton(canvas);
                Assert.That(guide.gameObject.activeSelf, Is.True);
                Assert.That(Card(canvas).sizeDelta.y, Is.EqualTo(544f), "one more row than the 440 card");

                guide.onClick.Invoke();
                Assert.That(host.Panel.IsOpen, Is.True);
                Assert.That(host.Panel.Mode, Is.EqualTo(GuideMode.Review));
                host.Panel.PressPrimary();
                Assert.That(host.Panel.IsOpen, Is.False);
                Assert.That(Time.timeScale, Is.Zero, "closing the review returns to the paused menu");
                Assert.That(panel.IsOpen, Is.True);

                panel.Resume();
                Assert.That(Time.timeScale, Is.EqualTo(.5f));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(hostObject);
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void PausePanel_FrogJumpMenuKeepsResumeAndGuide()
        {
            var (canvas, panel) = CreatePause();
            var hostObject = new GameObject("Host");
            try
            {
                hostObject.AddComponent<MinigameGuideHost>().Configure(new FakeSource(), new MemoryTutorialSeenStore());
                panel.SetLeaveOptionsVisible(false);
                panel.Open();

                Assert.That(GuideButton(canvas).gameObject.activeSelf, Is.True);
                Assert.That(canvas.transform.Find("PauseMenu/PauseCard/RestartButton").gameObject.activeSelf, Is.False);
                Assert.That(Card(canvas).sizeDelta.y, Is.EqualTo(336f));
                var resume = (RectTransform)canvas.transform.Find("PauseMenu/PauseCard/ResumeButton");
                var guide = (RectTransform)GuideButton(canvas).transform;
                Assert.That(resume.anchoredPosition.y - guide.anchoredPosition.y, Is.EqualTo(104f).Within(.01f));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(hostObject);
                Object.DestroyImmediate(canvas);
            }
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Core.PauseFlowTests" t6-pause`

Expected: compile failure on `GuideAvailable`.

- [ ] **Step 3: Add the button and the row layout**

In `PausePanel.cs`:

- Add `using System.Collections.Generic;`.
- Add the serialized field `[SerializeField] Button guideButton;` after `exitButton`.
- Add the constants and API below the existing `MenuSortingOrder` constant:

```csharp
        public const string GuideButtonLabel = "HƯỚNG DẪN";
        // The card grows by one row per visible button; 3 rows give the original 440 card.
        const float CardChrome = 232f;
        const float MinimumCardHeight = 270f;
        const float HeadingInset = 65f;
        const float FirstRowGap = 100f;
        const float RowSpacing = 104f;
```

```csharp
        public bool GuideAvailable => guideButton != null && MinigameGuideHost.Current != null;

        public void OpenGuide()
        {
            if (IsOpen)
                MinigameGuideHost.Current?.OpenReview();
        }
```

- Replace `ApplyMenuLayout` with:

```csharp
        // Stacks the visible buttons under the heading and sizes the card to fit them.
        void ApplyMenuLayout()
        {
            bool guide = GuideAvailable;
            if (guideButton != null)
                guideButton.gameObject.SetActive(guide);
            if (menuCard == null) return;

            var rows = new List<Button> { resumeButton };
            if (guide) rows.Add(guideButton);
            if (leaveOptionsVisible)
            {
                rows.Add(restartButton);
                rows.Add(exitButton);
            }

            float height = Mathf.Max(MinimumCardHeight, CardChrome + RowSpacing * (rows.Count - 1));
            ((RectTransform)menuCard).sizeDelta = new Vector2(560f, height);
            float headingY = height / 2f - HeadingInset;
            Transform heading = menuCard.Find("Heading");
            if (heading != null) ((RectTransform)heading).anchoredPosition = new Vector2(0f, headingY);
            for (int i = 0; i < rows.Count; i++)
                if (rows[i] != null)
                    ((RectTransform)rows[i].transform).anchoredPosition =
                        new Vector2(0f, headingY - FirstRowGap - RowSpacing * i);
        }
```

- In `WireButtons`, add:

```csharp
            if (guideButton != null)
                guideButton.onClick.AddListener(OpenGuide);
```

- In `EnsureMenu`, after `resumeButton = ...`, add:

```csharp
            guideButton = CreateMenuButton("GuideButton", GuideButtonLabel, 0f, ButtonVariant.Secondary);
```

`SetMenuVisible(true)` already calls `SetLeaveOptionsVisible`, which calls `ApplyMenuLayout`. Guide visibility is therefore decided each time the menu opens, after the host exists.

- [ ] **Step 4: Run the tests to verify they pass**

Run:

```bash
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Core.PauseFlowTests" t6-pause
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Progression.MinigamePauseNavigationTests" t6-nav
```

Expected: both `failed=0`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/UI/PausePanel.cs Assets/Tests/PlayMode/Core/PauseFlowTests.cs
git commit -m "feat(pause): reopen the minigame guide from the pause menu"
```

---

### Task 7: Sprint guide, frozen start gate, objective text

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintGuide.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintStartPresentation.cs` (`Update`, near line 58)
- Modify: `Assets/_Project/ScriptableObjects/Journey/sprint_practice.asset`, `sprint_exam.asset` (the `objective:` lines)
- Modify: `Assets/Tests/EditMode/Gameplay/Running/KMA.Gameplay.Running.EditMode.Tests.asmdef` (add `"KMA.Gameplay.UI"`)
- Test: `Assets/Tests/EditMode/Gameplay/Running/SprintGuideTests.cs`, `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`

**Interfaces:**
- Consumes: `MinigameGuidePages`, `TutorialStep`, `IMinigameGuideSource`, `GameFreeze`.
- Produces: `SprintGuide.Key = "Sprint"` and `SprintGuide.Build(ChallengeKind? kind, int targetCount, float distance, float timeLimit, int rivalCount)`. `SprintController` implements `IMinigameGuideSource`.

- [ ] **Step 1: Write the failing guide tests**

`Assets/Tests/EditMode/Gameplay/Running/SprintGuideTests.cs`:

```csharp
using KMA.Gameplay;
using NUnit.Framework;
using UnityEditor;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class SprintGuideTests
    {
        [Test]
        public void LearnAsksForTheRhythmCountWithoutAClock()
        {
            var pages = SprintGuide.Build(ChallengeKind.Learn, 12, 150f, 22f, 3);
            Assert.That(pages.Count, Is.EqualTo(3));
            Assert.That(pages[0].Title, Is.EqualTo("MỤC TIÊU"));
            Assert.That(pages[0].Instruction,
                Is.EqualTo("Bấm TRÁI, PHẢI luân phiên đúng 12 nhịp liên tiếp. Không tính giờ."));
            Assert.That(pages[1].Title, Is.EqualTo("ĐIỀU KHIỂN"));
            Assert.That(pages[2].Instruction, Is.EqualTo("Bấm sai bên thì chuỗi về 0 và phải đếm lại từ đầu."));
        }

        [Test]
        public void ExamStatesTheRaceAndAddsTheFailurePage()
        {
            var pages = SprintGuide.Build(ChallengeKind.Exam, 0, 150f, 15f, 3);
            Assert.That(pages.Count, Is.EqualTo(4));
            Assert.That(pages[0].Instruction, Is.EqualTo("Chạy 150 m trong 15 giây. Có 3 bạn chạy cùng."));
            Assert.That(pages[1].Instruction, Is.EqualTo(
                "Bấm TRÁI rồi PHẢI luân phiên để chạy. Giữ nhịp đều để lên CHUỖI và BỨT TỐC. Ngừng bấm là chậm lại."));
            Assert.That(pages[2].Instruction, Is.EqualTo(
                "Bấm sai bên thì mất chuỗi và gần như không tăng tốc. Hết giờ chưa về đích là trượt. " +
                "Điểm tính theo độ chính xác, thứ hạng và thời gian còn dư."));
            Assert.That(pages[3].Title, Is.EqualTo("NẾU TRƯỢT"));
        }

        [Test]
        public void FreePlayHasNoFailurePageAndNoRivalSentenceWithoutRivals()
        {
            var pages = SprintGuide.Build(null, 0, 150f, 22f, 0);
            Assert.That(pages.Count, Is.EqualTo(3));
            Assert.That(pages[0].Instruction, Is.EqualTo("Chạy 150 m trong 22 giây."));
        }

        [TestCase("sprint_practice")]
        [TestCase("sprint_exam")]
        public void ObjectiveTextMatchesTheLessonLimits(string id)
        {
            var definition = AssetDatabase.LoadAssetAtPath<ChallengeDefinition>(
                $"Assets/_Project/ScriptableObjects/Journey/{id}.asset");
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Objective,
                Is.EqualTo($"Chạy {definition.Distance:0} m trong tối đa {definition.TimeLimit:0} giây."));
        }
    }
}
```

Add `"KMA.Gameplay.UI"` to the `references` of `Assets/Tests/EditMode/Gameplay/Running/KMA.Gameplay.Running.EditMode.Tests.asmdef`.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Running.SprintGuideTests" t7-sprint`

Expected: compile failure, `SprintGuide` does not exist.

- [ ] **Step 3: Implement the guide and wire the controller**

`Assets/_Project/Scripts/Gameplay/Sprint/SprintGuide.cs`:

```csharp
using System.Collections.Generic;
using KMA.Gameplay.UI;

namespace KMA.Gameplay
{
    public static class SprintGuide
    {
        public const string Key = nameof(SubjectId.Sprint);
        public const string Controls =
            "Bấm TRÁI rồi PHẢI luân phiên để chạy. Giữ nhịp đều để lên CHUỖI và BỨT TỐC. Ngừng bấm là chậm lại.";
        public const string RaceRules =
            "Bấm sai bên thì mất chuỗi và gần như không tăng tốc. Hết giờ chưa về đích là trượt. " +
            "Điểm tính theo độ chính xác, thứ hạng và thời gian còn dư.";
        // The learn lesson has no clock: a wrong side only resets the streak it counts.
        public const string LearnRules = "Bấm sai bên thì chuỗi về 0 và phải đếm lại từ đầu.";

        public static IReadOnlyList<TutorialStep> Build(ChallengeKind? kind, int targetCount, float distance,
            float timeLimit, int rivalCount)
        {
            bool learn = kind == ChallengeKind.Learn;
            string goal = learn
                ? $"Bấm TRÁI, PHẢI luân phiên đúng {targetCount} nhịp liên tiếp. Không tính giờ."
                : $"Chạy {MinigameGuidePages.Number(distance)} m trong {MinigameGuidePages.Number(timeLimit)} giây." +
                  (rivalCount > 0 ? $" Có {rivalCount} bạn chạy cùng." : string.Empty);
            return MinigameGuidePages.Pages(
                new TutorialStep("MỤC TIÊU", goal),
                new TutorialStep("ĐIỀU KHIỂN", Controls),
                new TutorialStep("LUẬT", learn ? LearnRules : RaceRules),
                MinigameGuidePages.FailurePage(kind));
        }
    }
}
```

In `SprintController.cs`:
- Add `using System.Collections.Generic;`.
- Change the class line to `public sealed class SprintController : MinigameBase, IChallengeController, IMinigameGuideSource`.
- Add after `RivalCount`:

```csharp
        public string GuideKey => SprintGuide.Key;
        public IReadOnlyList<TutorialStep> BuildGuide() =>
            SprintGuide.Build(challengeDefinition?.Kind, TargetCount, TargetDistance, TargetTime, RivalCount);
```

- [ ] **Step 4: Fix the two objective texts**

In `Assets/_Project/ScriptableObjects/Journey/sprint_practice.asset`, the `objective:` value spans two lines (25–26). Replace both lines with this single line:

```yaml
  objective: "Chạy 150 m trong tối đa 20 gi\xE2y."
```

In `Assets/_Project/ScriptableObjects/Journey/sprint_exam.asset`, replace line 25 with:

```yaml
  objective: "Chạy 150 m trong tối đa 15 gi\xE2y."
```

Check: `grep -n "objective" Assets/_Project/ScriptableObjects/Journey/sprint_practice.asset Assets/_Project/ScriptableObjects/Journey/sprint_exam.asset` shows exactly one line per file.

- [ ] **Step 5: Run the guide tests to verify they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Running.SprintGuideTests" t7-sprint`

Expected: `failed=0`.

- [ ] **Step 6: Write the failing frozen-gate test**

Add to `SprintPresentationGateTests` (Presentation PlayMode). Add `using KMA.Gameplay.UI;` if missing.

```csharp
        [UnityTest]
        public IEnumerator SprintStart_HoldsTheGateWhileTheGameIsFrozen()
        {
            yield return LoadSprint();
            var controller = SceneObjects<SprintController>(SceneManager.GetActiveScene())[0];
            var holder = new object();
            GameFreeze.Acquire(holder);
            try
            {
                float until = Time.realtimeSinceStartup + 2f;
                while (Time.realtimeSinceStartup < until)
                    yield return null;
                Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Tutorial),
                    "the 1.5 s start gate must not run under the guide or the pause menu");
            }
            finally
            {
                GameFreeze.Release(holder);
            }
        }
```

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Presentation.SprintPresentationGateTests" t7-gate`

Expected: `SprintStart_HoldsTheGateWhileTheGameIsFrozen` FAILS, because the phase is already `Countdown`.

- [ ] **Step 7: Hold the start presentation while frozen**

In `SprintStartPresentation.Update()`, add as the first statement:

```csharp
            // Runs on unscaled time, so it must stop by itself while the guide or pause menu freezes the game.
            if (Time.timeScale == 0f)
                return;
```

- [ ] **Step 8: Run the Sprint suites**

Run:

```bash
tools/run-unity-tests.sh PlayMode "KMA.Tests.Presentation.SprintPresentationGateTests" t7-gate
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Running" t7-running
```

Expected: `failed=0` for both.

- [ ] **Step 9: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Sprint/SprintGuide.cs* Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs \
  Assets/_Project/Scripts/Gameplay/Sprint/SprintStartPresentation.cs \
  Assets/_Project/ScriptableObjects/Journey/sprint_practice.asset Assets/_Project/ScriptableObjects/Journey/sprint_exam.asset \
  Assets/Tests/EditMode/Gameplay/Running/SprintGuideTests.cs* Assets/Tests/EditMode/Gameplay/Running/KMA.Gameplay.Running.EditMode.Tests.asmdef \
  Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs
git commit -m "feat(sprint): how-to-play guide, frozen start gate and matching objective text"
```

---

### Task 8: Football guide

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Football/FootballGuide.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Football/FootballController.cs`
- Modify: `Assets/Tests/EditMode/Gameplay/Ball/KMA.Gameplay.Ball.EditMode.Tests.asmdef` (add `"KMA.Gameplay.UI"`)
- Test: `Assets/Tests/EditMode/Gameplay/Ball/FootballGuideTests.cs`

**Interfaces:**
- Produces: `FootballGuide.Key = "Football"` and `FootballGuide.Build(ChallengeKind? kind, int requiredGoals, int? maxKicks, bool keeperEnabled)`. `FootballController` implements `IMinigameGuideSource`.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/Ball/FootballGuideTests.cs`:

```csharp
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class FootballGuideTests
    {
        [Test]
        public void LearnHasNoKeeperAndNoKickLimit()
        {
            var pages = FootballGuide.Build(ChallengeKind.Learn, 3, null, false);
            Assert.That(pages.Count, Is.EqualTo(4));
            Assert.That(pages[0].Instruction,
                Is.EqualTo("Ghi 3 bàn. Không có thủ môn, sút bao nhiêu lượt cũng được."));
            Assert.That(pages[1].Title, Is.EqualTo("NGẮM"));
            Assert.That(pages[2].Title, Is.EqualTo("LỰC"));
            Assert.That(pages[3].Title, Is.EqualTo("LUẬT"));
        }

        [Test]
        public void PracticeCountsKicksAndAddsTheFailurePage()
        {
            var pages = FootballGuide.Build(ChallengeKind.Practice, 2, 6, true);
            Assert.That(pages.Count, Is.EqualTo(5));
            Assert.That(pages[0].Instruction, Is.EqualTo("Ghi 2 bàn trong 6 lượt sút. Có thủ môn."));
            Assert.That(pages[1].Instruction, Is.EqualTo(
                "Kéo thanh hướng sang TRÁI hoặc PHẢI để chọn góc. Đường bay dự kiến hiện ra khi bạn lấy lực."));
            Assert.That(pages[2].Instruction, Is.EqualTo(
                "Giữ nút SÚT thì lực tăng rồi giảm liên tục. Thả tay đúng lúc để sút. " +
                "Quá mạnh dễ vọt xà, quá yếu bóng dừng trước khung thành."));
            Assert.That(pages[3].Instruction,
                Is.EqualTo("Trúng cột, trúng xà, chệch khung hay bị thủ môn cản phá đều mất lượt."));
            Assert.That(pages[4].Title, Is.EqualTo("NẾU TRƯỢT"));
        }

        [Test]
        public void FinalHasNoFailurePage() =>
            Assert.That(FootballGuide.Build(ChallengeKind.Final, 3, 5, true).Count, Is.EqualTo(4));
    }
}
```

Add `"KMA.Gameplay.UI"` to `KMA.Gameplay.Ball.EditMode.Tests.asmdef` references.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Ball.FootballGuideTests" t8-football`

Expected: compile failure, `FootballGuide` does not exist.

- [ ] **Step 3: Implement the guide and wire the controller**

`Assets/_Project/Scripts/Gameplay/Football/FootballGuide.cs`:

```csharp
using System.Collections.Generic;
using KMA.Gameplay.UI;

namespace KMA.Gameplay
{
    public static class FootballGuide
    {
        public const string Key = nameof(SubjectId.Football);
        public const string Aim =
            "Kéo thanh hướng sang TRÁI hoặc PHẢI để chọn góc. Đường bay dự kiến hiện ra khi bạn lấy lực.";
        public const string Power =
            "Giữ nút SÚT thì lực tăng rồi giảm liên tục. Thả tay đúng lúc để sút. " +
            "Quá mạnh dễ vọt xà, quá yếu bóng dừng trước khung thành.";
        public const string Rules = "Trúng cột, trúng xà, chệch khung hay bị thủ môn cản phá đều mất lượt.";

        public static IReadOnlyList<TutorialStep> Build(ChallengeKind? kind, int requiredGoals, int? maxKicks,
            bool keeperEnabled)
        {
            string keeper = keeperEnabled ? "Có thủ môn" : "Không có thủ môn";
            string goal = maxKicks.HasValue
                ? $"Ghi {requiredGoals} bàn trong {maxKicks.Value} lượt sút. {keeper}."
                : $"Ghi {requiredGoals} bàn. {keeper}, sút bao nhiêu lượt cũng được.";
            return MinigameGuidePages.Pages(
                new TutorialStep("MỤC TIÊU", goal),
                new TutorialStep("NGẮM", Aim),
                new TutorialStep("LỰC", Power),
                new TutorialStep("LUẬT", Rules),
                MinigameGuidePages.FailurePage(kind));
        }
    }
}
```

In `FootballController.cs`:
- Add `using System.Collections.Generic;` if missing.
- Add `IMinigameGuideSource` to the class's interface list.
- Add next to `public FootballRules Rules => rules;`:

```csharp
        public string GuideKey => FootballGuide.Key;
        // Read from the live rules, because OptionsFor hardcodes the learn and exam numbers.
        public IReadOnlyList<TutorialStep> BuildGuide() => rules == null
            ? System.Array.Empty<TutorialStep>()
            : FootballGuide.Build(challengeDefinition?.Kind, rules.RequiredGoals, rules.MaximumKicks, rules.KeeperEnabled);
```

- [ ] **Step 4: Run the tests to verify they pass**

Run:

```bash
tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Ball" t8-football
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Football" t8-football-play
```

Expected: `failed=0` for both.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Football/FootballGuide.cs* Assets/_Project/Scripts/Gameplay/Football/FootballController.cs \
  Assets/Tests/EditMode/Gameplay/Ball/FootballGuideTests.cs* Assets/Tests/EditMode/Gameplay/Ball/KMA.Gameplay.Ball.EditMode.Tests.asmdef
git commit -m "feat(football): how-to-play guide"
```

---

### Task 9: Volleyball guide

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballGuide.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs`
- Modify: `Assets/Tests/EditMode/Gameplay/Volleyball/KMA.Gameplay.Volleyball.EditMode.Tests.asmdef` (add `"KMA.Gameplay.UI"`)
- Test: `Assets/Tests/EditMode/Gameplay/Volleyball/VolleyballGuideTests.cs`

**Interfaces:**
- Produces: `VolleyballGuide.Key = "Volleyball"` and `VolleyballGuide.Build(ChallengeKind? kind, int learnTarget, int pointsToWin, float clockLimit)`. `VolleyballController` implements `IMinigameGuideSource`.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/Volleyball/VolleyballGuideTests.cs`:

```csharp
using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballGuideTests
    {
        [Test]
        public void LearnCountsReceives()
        {
            var pages = VolleyballGuide.Build(ChallengeKind.Learn, 3, 0, 0f);
            Assert.That(pages.Count, Is.EqualTo(4));
            Assert.That(pages[0].Instruction, Is.EqualTo("Đỡ bóng thành công 3 lần. Đối thủ luôn giao bóng."));
            Assert.That(pages[1].Title, Is.EqualTo("DI CHUYỂN"));
            Assert.That(pages[1].Instruction, Is.EqualTo("Kéo joystick để chạy tới chỗ bóng rơi."));
            Assert.That(pages[2].Title, Is.EqualTo("ĐÁNH & NHẢY"));
            Assert.That(pages[2].Instruction, Is.EqualTo(
                "Nút ĐÁNH tự chọn giao, đỡ, chuyền, đập hoặc chắn tuỳ tình huống. " +
                "NHẢY rồi kéo joystick để nhắm hướng đập. Nút NHẢY sáng lên là lúc nên nhảy."));
            Assert.That(pages[3].Instruction, Is.EqualTo(
                "Bấm đúng nhịp: HOÀN HẢO, rồi TỐT, rồi SỚM/MUỘN. Mỗi bên chạm tối đa 3 lần. " +
                "Bóng không qua lưới là mất điểm."));
        }

        [Test]
        public void PracticeIsARaceAgainstTheClock()
        {
            var pages = VolleyballGuide.Build(ChallengeKind.Practice, 5, 5, 120f);
            Assert.That(pages.Count, Is.EqualTo(5));
            Assert.That(pages[0].Instruction,
                Is.EqualTo("Ghi 5 điểm trước đối thủ trong 120 giây. Đối thủ luôn giao bóng."));
            Assert.That(pages[4].Title, Is.EqualTo("NẾU TRƯỢT"));
        }

        [Test]
        public void ExamHasNoClockAndAlternatesServes()
        {
            var pages = VolleyballGuide.Build(ChallengeKind.Exam, 10, 10, 0f);
            Assert.That(pages[0].Instruction,
                Is.EqualTo("Ghi 10 điểm trước đối thủ. Hai bên luân phiên giao bóng."));
        }
    }
}
```

Add `"KMA.Gameplay.UI"` to the Volleyball EditMode test asmdef references.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.VolleyballGuideTests" t9-volley`

Expected: compile failure, `VolleyballGuide` does not exist.

- [ ] **Step 3: Implement the guide and wire the controller**

`Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballGuide.cs`:

```csharp
using System.Collections.Generic;
using KMA.Gameplay.UI;

namespace KMA.Gameplay.Volleyball
{
    public static class VolleyballGuide
    {
        public const string Key = nameof(SubjectId.Volleyball);
        public const string Move = "Kéo joystick để chạy tới chỗ bóng rơi.";
        public const string HitAndJump =
            "Nút ĐÁNH tự chọn giao, đỡ, chuyền, đập hoặc chắn tuỳ tình huống. " +
            "NHẢY rồi kéo joystick để nhắm hướng đập. Nút NHẢY sáng lên là lúc nên nhảy.";
        public const string Rules =
            "Bấm đúng nhịp: HOÀN HẢO, rồi TỐT, rồi SỚM/MUỘN. Mỗi bên chạm tối đa 3 lần. " +
            "Bóng không qua lưới là mất điểm.";

        public static IReadOnlyList<TutorialStep> Build(ChallengeKind? kind, int learnTarget, int pointsToWin,
            float clockLimit)
        {
            string goal;
            if (kind == ChallengeKind.Learn)
            {
                goal = $"Đỡ bóng thành công {learnTarget} lần. Đối thủ luôn giao bóng.";
            }
            else
            {
                goal = $"Ghi {pointsToWin} điểm trước đối thủ" +
                       (clockLimit > 0f ? $" trong {MinigameGuidePages.Number(clockLimit)} giây." : ".");
                // Practice keeps the serve with the opponent (VolleyballChallengeRules).
                goal += kind == ChallengeKind.Practice
                    ? " Đối thủ luôn giao bóng."
                    : " Hai bên luân phiên giao bóng.";
            }

            return MinigameGuidePages.Pages(
                new TutorialStep("MỤC TIÊU", goal),
                new TutorialStep("DI CHUYỂN", Move),
                new TutorialStep("ĐÁNH & NHẢY", HitAndJump),
                new TutorialStep("LUẬT", Rules),
                MinigameGuidePages.FailurePage(kind));
        }
    }
}
```

In `VolleyballController.cs`:
- Add `using System.Collections.Generic;`.
- Change the class line to `public sealed class VolleyballController : MinigameBase, IChallengeController, IMinigameGuideSource`.
- Add after `public SubjectId Subject => SubjectId.Volleyball;`:

```csharp
        public string GuideKey => VolleyballGuide.Key;
        public IReadOnlyList<TutorialStep> BuildGuide() => VolleyballGuide.Build(challengeDefinition?.Kind,
            challengeDefinition?.TargetCount ?? 0, Match.WinningPoints, Match.ClockLimit);
```

- [ ] **Step 4: Run the tests to verify they pass**

Run:

```bash
tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.VolleyballGuideTests" t9-volley
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Volleyball" t9-volley-play
```

Expected: `failed=0` for both.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballGuide.cs* Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs \
  Assets/Tests/EditMode/Gameplay/Volleyball/VolleyballGuideTests.cs* Assets/Tests/EditMode/Gameplay/Volleyball/KMA.Gameplay.Volleyball.EditMode.Tests.asmdef
git commit -m "feat(volleyball): how-to-play guide"
```

---

### Task 10: FrogJump guide

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/FrogJump/FrogJumpGuide.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/FrogJump/FrogJumpController.cs`
- Modify: `Assets/Tests/EditMode/Gameplay/FrogJump/KMA.Gameplay.FrogJump.EditMode.Tests.asmdef` (add `"KMA.Gameplay.UI"`)
- Test: `Assets/Tests/EditMode/Gameplay/FrogJump/FrogJumpGuideTests.cs`

**Interfaces:**
- Consumes: `SaveDataTutorialSeenStore.FrogJumpKey` (Task 3).
- Produces: `FrogJumpGuide.Key` and `FrogJumpGuide.Build(FrogJumpTuning tuning, bool savesLife)`. `FrogJumpController` implements `IMinigameGuideSource`.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/FrogJump/FrogJumpGuideTests.cs`:

```csharp
using KMA.Gameplay.FrogJump;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.FrogJump
{
    public sealed class FrogJumpGuideTests
    {
        [Test]
        public void ALifeSavingJumpSaysSo()
        {
            var pages = FrogJumpGuide.Build(new FrogJumpTuning(), true);
            Assert.That(pages.Count, Is.EqualTo(3));
            Assert.That(pages[0].Instruction, Is.EqualTo(
                "Nhảy hết 60 m trong 60 giây. Đây là thử thách bắt buộc sau khi trượt bài. Về đích để giữ mạng."));
            Assert.That(pages[1].Instruction, Is.EqualTo(
                "Kim chạy qua lại trên thanh lực. Chạm màn hình khi kim ở vùng xanh. Càng gần giữa càng nhảy xa."));
            Assert.That(pages[2].Instruction,
                Is.EqualTo("Chạm vùng đỏ là NGÃ: không tiến được và mất 2,5 giây đứng dậy."));
        }

        [Test]
        public void AJumpThatCannotSaveALifeOmitsTheSentence()
        {
            var pages = FrogJumpGuide.Build(new FrogJumpTuning(), false);
            Assert.That(pages[0].Instruction,
                Is.EqualTo("Nhảy hết 60 m trong 60 giây. Đây là thử thách bắt buộc sau khi trượt bài."));
        }

        [Test]
        public void KeyIsTheSavedFrogJumpKey() => Assert.That(FrogJumpGuide.Key, Is.EqualTo("FrogJump"));
    }
}
```

Add `"KMA.Gameplay.UI"` to the FrogJump EditMode test asmdef references.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.FrogJump.FrogJumpGuideTests" t10-frog`

Expected: compile failure, `FrogJumpGuide` does not exist.

- [ ] **Step 3: Implement the guide and wire the controller**

`Assets/_Project/Scripts/Gameplay/FrogJump/FrogJumpGuide.cs`:

```csharp
using System.Collections.Generic;
using KMA.Gameplay.UI;

namespace KMA.Gameplay.FrogJump
{
    public static class FrogJumpGuide
    {
        public const string Key = SaveDataTutorialSeenStore.FrogJumpKey;
        public const string Controls =
            "Kim chạy qua lại trên thanh lực. Chạm màn hình khi kim ở vùng xanh. Càng gần giữa càng nhảy xa.";

        /// savesLife: a finish keeps the life (JourneyProgress only charges SavesLife && !reachedFinish).
        public static IReadOnlyList<TutorialStep> Build(FrogJumpTuning tuning, bool savesLife)
        {
            string goal =
                $"Nhảy hết {MinigameGuidePages.Number(tuning.trackMetres)} m trong " +
                $"{MinigameGuidePages.Number(tuning.timeLimitSeconds)} giây. " +
                "Đây là thử thách bắt buộc sau khi trượt bài." + (savesLife ? " Về đích để giữ mạng." : string.Empty);
            string rules =
                $"Chạm vùng đỏ là NGÃ: không tiến được và mất {MinigameGuidePages.Number(tuning.recoverSeconds)} giây đứng dậy.";
            return MinigameGuidePages.Pages(
                new TutorialStep("MỤC TIÊU", goal),
                new TutorialStep("ĐIỀU KHIỂN", Controls),
                new TutorialStep("LUẬT", rules));
        }
    }
}
```

In `FrogJumpController.cs`:
- Add `using System.Collections.Generic;`.
- Change the class line to `public sealed class FrogJumpController : MinigameBase, IMinigameGuideSource`.
- Add after `public bool IsWired => ...;`:

```csharp
        public string GuideKey => FrogJumpGuide.Key;
        public IReadOnlyList<TutorialStep> BuildGuide() => FrogJumpGuide.Build(rules.Tuning, PendingJumpSavesLife());

        static bool PendingJumpSavesLife()
        {
            GameManager manager = GameManager.Instance;
            return manager != null && manager.IsInitialized && manager.Session?.PendingFrogJump?.SavesLife == true;
        }
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.FrogJump" t10-frog`

Expected: `failed=0`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/FrogJump/FrogJumpGuide.cs* Assets/_Project/Scripts/Gameplay/FrogJump/FrogJumpController.cs \
  Assets/Tests/EditMode/Gameplay/FrogJump/FrogJumpGuideTests.cs* Assets/Tests/EditMode/Gameplay/FrogJump/KMA.Gameplay.FrogJump.EditMode.Tests.asmdef
git commit -m "feat(frog-jump): how-to-play guide"
```

---

### Task 11: Chess guide

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Chess/ChessGuide.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Chess/ChessFinalController.cs` (`TimeWords` moves out; the class gains the interface)
- Modify: `Assets/Tests/EditMode/Gameplay/Chess/KMA.Gameplay.Chess.EditMode.Tests.asmdef` (add `"KMA.Gameplay.UI"`)
- Test: `Assets/Tests/EditMode/Gameplay/Chess/ChessGuideTests.cs`

**Interfaces:**
- Produces:
  - `ChessGuide.Key = "Chess"`
  - `ChessGuide.Build(int maxPlayerMoves, float timeLimitSeconds, int maxMistakes)`
  - `ChessGuide.TimeWords(float seconds)`
  - `ChessFinalController` implements `IMinigameGuideSource`.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/Chess/ChessGuideTests.cs`:

```csharp
using KMA.Gameplay.Chess;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class ChessGuideTests
    {
        [Test]
        public void PagesCarryThePuzzleNumbers()
        {
            var pages = ChessGuide.Build(2, 90f, 2);
            Assert.That(pages.Count, Is.EqualTo(3));
            Assert.That(pages[0].Instruction, Is.EqualTo(
                "Chiếu hết trong 2 nước. Thời gian suy nghĩ một phút rưỡi. Bài thi cuối không mất mạng."));
            Assert.That(pages[1].Instruction, Is.EqualTo(
                "Chạm quân của bạn, rồi chạm ô muốn đi tới. Tốt phong cấp thì chọn quân muốn đổi. " +
                "Nút GỢI Ý có 3 mức, từ ý tưởng tới nước đi cụ thể."));
            Assert.That(pages[2].Instruction, Is.EqualTo(
                "Nước không hợp lệ thì không tính. Đi sai bị tính 1 lỗi, bàn cờ giữ nguyên. " +
                "Sai quá 2 lần hoặc hết giờ là trượt. Đồng hồ chỉ chạy trong lượt của bạn. " +
                "Dùng gợi ý, đi sai và đi chậm đều bị trừ điểm."));
        }

        [TestCase(60f, "một phút")]
        [TestCase(90f, "một phút rưỡi")]
        [TestCase(120f, "hai phút")]
        [TestCase(45f, "45 giây")]
        public void TimeWordsReadAloud(float seconds, string expected) =>
            Assert.That(ChessGuide.TimeWords(seconds), Is.EqualTo(expected));
    }
}
```

Add `"KMA.Gameplay.UI"` to the Chess EditMode test asmdef references.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Chess.ChessGuideTests" t11-chess`

Expected: compile failure, `ChessGuide` does not exist.

- [ ] **Step 3: Implement the guide and wire the controller**

`Assets/_Project/Scripts/Gameplay/Chess/ChessGuide.cs`:

```csharp
using System.Collections.Generic;
using KMA.Gameplay.UI;
using UnityEngine;

namespace KMA.Gameplay.Chess
{
    public static class ChessGuide
    {
        public const string Key = nameof(SubjectId.Chess);
        // ChessBoardView.ClickSquare: tap a piece, then tap its destination.
        public const string Controls =
            "Chạm quân của bạn, rồi chạm ô muốn đi tới. Tốt phong cấp thì chọn quân muốn đổi. " +
            "Nút GỢI Ý có 3 mức, từ ý tưởng tới nước đi cụ thể.";

        public static IReadOnlyList<TutorialStep> Build(int maxPlayerMoves, float timeLimitSeconds, int maxMistakes)
        {
            string goal = $"Chiếu hết trong {maxPlayerMoves} nước. Thời gian suy nghĩ {TimeWords(timeLimitSeconds)}. " +
                          "Bài thi cuối không mất mạng.";
            string rules = "Nước không hợp lệ thì không tính. Đi sai bị tính 1 lỗi, bàn cờ giữ nguyên. " +
                           $"Sai quá {maxMistakes} lần hoặc hết giờ là trượt. Đồng hồ chỉ chạy trong lượt của bạn. " +
                           "Dùng gợi ý, đi sai và đi chậm đều bị trừ điểm.";
            return MinigameGuidePages.Pages(
                new TutorialStep("MỤC TIÊU", goal),
                new TutorialStep("ĐIỀU KHIỂN", Controls),
                new TutorialStep("LUẬT", rules));
        }

        public static string TimeWords(float seconds) => Mathf.RoundToInt(seconds) switch
        {
            60 => "một phút",
            90 => "một phút rưỡi",
            120 => "hai phút",
            int s => $"{s} giây"
        };
    }
}
```

In `ChessFinalController.cs`:
- Change the class line to `public sealed class ChessFinalController : MinigameBase, IChallengeMetricsSource, IMinigameGuideSource`.
- Delete the private `static string TimeWords(float seconds) => ...` method.
- In `Start()`, change `{TimeWords(Machine.Clock.Limit)}` to `{ChessGuide.TimeWords(Machine.Clock.Limit)}`.
- Add after the `OwnsCameraBackground` line:

```csharp
        public string GuideKey => ChessGuide.Key;
        public IReadOnlyList<TutorialStep> BuildGuide() =>
            ChessGuide.Build(Machine.MaxPlayerMoves, Machine.Clock.Limit, Machine.MaxMistakes);
```

- [ ] **Step 4: Run the tests to verify they pass**

Run:

```bash
tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Chess" t11-chess
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Chess" t11-chess-play
```

Expected: `failed=0` for both.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Chess/ChessGuide.cs* Assets/_Project/Scripts/Gameplay/Chess/ChessFinalController.cs \
  Assets/Tests/EditMode/Gameplay/Chess/ChessGuideTests.cs* Assets/Tests/EditMode/Gameplay/Chess/KMA.Gameplay.Chess.EditMode.Tests.asmdef
git commit -m "feat(chess): how-to-play guide"
```

---

### Task 12: Guide opens in every minigame scene

**Files:**
- Test: `Assets/Tests/PlayMode/Presentation/MinigameGuideSceneTests.cs`

**Interfaces:**
- Consumes:
  - From Task 5: `MinigameGuideHost.Current`, `Configure`, `TryOpenFirstRun`, `Source`, `Panel`
  - From Tasks 7–11: each controller's `GuideKey`

- [ ] **Step 1: Write the test**

`Assets/Tests/PlayMode/Presentation/MinigameGuideSceneTests.cs`:

```csharp
using System.Collections;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Presentation
{
    public sealed class MinigameGuideSceneTests
    {
        [UnityTest]
        public IEnumerator EveryMinigameSceneInstallsItsGuide(
            [Values("MG_Sprint:Sprint", "MG_Football:Football", "MG_Volleyball:Volleyball",
                "MG_FrogJump:FrogJump", "MG_ChessFinal:Chess")] string sceneAndKey)
        {
            string[] parts = sceneAndKey.Split(':');
            yield return SceneManager.LoadSceneAsync(parts[0], LoadSceneMode.Single);
            yield return null;
            yield return null;

            MinigameGuideHost host = MinigameGuideHost.Current;
            try
            {
                Assert.That(host, Is.Not.Null, parts[0] + " must install a guide host");
                Assert.That(host.Source.GuideKey, Is.EqualTo(parts[1]));
                Assert.That(host.Panel.IsOpen, Is.False, "auto-open is off in PlayMode tests");

                // A fresh store makes the test independent of any save left by earlier tests.
                host.Configure(host.Source, new MemoryTutorialSeenStore());
                Assert.That(host.TryOpenFirstRun(), Is.True);
                Assert.That(host.Panel.CurrentPage.Title, Is.EqualTo("MỤC TIÊU"));
                Assert.That(host.Panel.CurrentPage.Instruction, Is.Not.Empty);
                Assert.That(Time.timeScale, Is.Zero);

                host.Panel.PressSkip();
                Assert.That(Time.timeScale, Is.EqualTo(1f));
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }
    }
}
```

- [ ] **Step 2: Run it**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Presentation.MinigameGuideSceneTests" t12-scenes`

Expected: `failed=0` (5 cases).

If a scene fails on `host` being null, that scene's controller is not active at `sceneLoaded`. Check `FindSource` and the controller's `enabled` state; do not weaken the test.

- [ ] **Step 3: Commit**

```bash
git add Assets/Tests/PlayMode/Presentation/MinigameGuideSceneTests.cs*
git commit -m "test(guide): every minigame scene installs and opens its guide"
```

---

### Task 13: Remove the dead `TutorialOverlay`

**Files:**
- Delete: `Assets/_Project/Scripts/UI/TutorialOverlay.cs`, `Assets/_Project/Scripts/UI/TutorialOverlay.cs.meta`
- Delete: `Assets/Tests/EditMode/Presentation/TutorialOverlayTests.cs` and `.meta`
- Create: `Assets/Tests/EditMode/Presentation/TutorialSeenStoreTests.cs`
- Modify:
  - `Assets/_Project/Scripts/UI/PhaseOverlay.cs`
  - `Assets/_Project/Prefabs/UI/PhaseOverlay.prefab`
  - `Assets/Tests/EditMode/Presentation/ResultPanelRetryTests.cs`
  - `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`

- [ ] **Step 1: Keep the two behaviours the old test file covered**

Create `Assets/Tests/EditMode/Presentation/TutorialSeenStoreTests.cs`:

```csharp
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class TutorialSeenStoreTests
    {
        [Test]
        public void DirectSceneUsesTheMemoryFallbackAndNoPlayerPrefsKey()
        {
            const string playerPrefsKey = "KMA.tutorialSeen.Sprint";
            PlayerPrefs.DeleteKey(playerPrefsKey);
            try
            {
                var memory = new MemoryTutorialSeenStore();
                var store = new SaveDataTutorialSeenStore(memory);
                store.MarkSeen("Sprint");
                store.MarkSeen(SaveDataTutorialSeenStore.FrogJumpKey);

                Assert.That(store.HasSeen("Sprint"), Is.True);
                Assert.That(memory.HasSeen(SaveDataTutorialSeenStore.FrogJumpKey), Is.True);
                Assert.That(PlayerPrefs.HasKey(playerPrefsKey), Is.False,
                    "Direct-scene completion must not create the retired PlayerPrefs tutorial key.");
            }
            finally
            {
                PlayerPrefs.DeleteKey(playerPrefsKey);
            }
        }
    }
}
```

Inside the `ResultPanelRetryTests` class, add the result-panel half of the old `ClosingFinalStepMarksSeenAndResultActionOnlyReturnsPreviewRoute` test. Add `using KMA.Gameplay;` if missing.

```csharp
        [Test]
        public void ResultActionOnlyReturnsPreviewRoute()
        {
            var resultRoot = new GameObject("result-panel");
            try
            {
                var result = new MinigameResult(true, 1234.6f, Rank.A);
                var panel = resultRoot.AddComponent<ResultPanel>();
                string requestedRoute = null;
                panel.ActionRequested += route => requestedRoute = route;

                panel.Show(result, "MG_SprintPreview");
                panel.Continue();

                Assert.That(panel.CurrentResult, Is.SameAs(result));
                Assert.That(panel.PreviewRoute, Is.EqualTo("MG_SprintPreview"));
                Assert.That(requestedRoute, Is.EqualTo("MG_SprintPreview"));
            }
            finally
            {
                Object.DestroyImmediate(resultRoot);
            }
        }
```

- [ ] **Step 2: Delete the class and its tests**

```bash
git rm Assets/_Project/Scripts/UI/TutorialOverlay.cs Assets/_Project/Scripts/UI/TutorialOverlay.cs.meta \
  Assets/Tests/EditMode/Presentation/TutorialOverlayTests.cs Assets/Tests/EditMode/Presentation/TutorialOverlayTests.cs.meta
```

- [ ] **Step 3: Remove the overlay from `PhaseOverlay.cs`**

In `Assets/_Project/Scripts/UI/PhaseOverlay.cs`:
- Delete the field `[SerializeField] TutorialOverlay tutorialOverlay;`.
- Delete the field `bool tutorialSubscribed;`.
- In `Unsubscribe()`, delete the line `UnsubscribeTutorialCompletion();`.
- In `ApplyPhase`, change the `tutorialRoot` line to:

```csharp
            SetActive(tutorialRoot, sharedTutorial && phase == MinigamePhase.Tutorial);
```

- Replace `ConfigureTutorial`, `UnsubscribeTutorialCompletion` and `ReleaseTutorialGate` with:

```csharp
        // Minigames that own their start gate open it themselves; every other gate opens at once and
        // shared-tutorial minigames advance on the lifecycle timer. The how-to-play guide is
        // MinigameGuideHost's job.
        void ConfigureTutorial()
        {
            if (source == null)
                return;
            if (source.OwnsStartGate)
                FindSprintStartPresentation()?.Bind(source);
            else
                source.SetTutorialGate(false);
        }
```

- [ ] **Step 4: Remove the component from the prefab**

Run from the repo root:

```bash
python3 - <<'PY'
import re
path = "Assets/_Project/Prefabs/UI/PhaseOverlay.prefab"
text = open(path, encoding="utf-8", newline="").read()
fid = "4679405148162565661"
nl = "\r\n" if "\r\n" in text else "\n"
before = text
text = text.replace(f"  tutorialOverlay: {{fileID: {fid}}}{nl}", "", 1)
text = text.replace(f"  - component: {{fileID: {fid}}}{nl}", "", 1)
text = re.sub(rf"--- !u!114 &{fid}{nl}.*?(?=--- !u!)", "", text, count=1, flags=re.S)
assert text != before and fid not in text, "TutorialOverlay block not fully removed"
open(path, "w", encoding="utf-8", newline="").write(text)
print("removed TutorialOverlay", fid)
PY
grep -c "a9dd06a4448e575a1b7eabd7aae2abf8" Assets/_Project/Prefabs/UI/PhaseOverlay.prefab
```

Expected: `removed TutorialOverlay 4679405148162565661`, then `0`.

- [ ] **Step 5: Drop the overlay assertions from the Sprint scene test**

In `SprintPresentationGateTests.SprintSceneHasCompletePresentationContractAndUsesAutomaticStart`, delete these lines:
- `var overlays = SceneObjects<TutorialOverlay>(scene);`
- `Assert.That(overlays.Length, Is.EqualTo(1));`
- The two-line `Assert.That(overlays[0].ShouldShow, Is.False, "Sprint must not open the shared multi-page tutorial.");`

Run: `grep -rn "TutorialOverlay" Assets --include=*.cs --include=*.prefab --include=*.unity`

Expected: no matches.

- [ ] **Step 6: Run the affected suites**

Run:

```bash
tools/run-unity-tests.sh EditMode "KMA.Tests.Presentation" t13-edit
tools/run-unity-tests.sh PlayMode "KMA.Tests.Presentation" t13-play
```

Expected: `failed=0` for both, or no worse than the baseline for these namespaces. `PhaseOverlayPresentationFlagsTests` still passes because `tutorialRoot` stays.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/UI/PhaseOverlay.cs Assets/_Project/Prefabs/UI/PhaseOverlay.prefab \
  Assets/Tests/EditMode/Presentation/TutorialSeenStoreTests.cs* Assets/Tests/EditMode/Presentation/ResultPanelRetryTests.cs \
  Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs
git commit -m "refactor(ui): remove the unused shared tutorial overlay"
```

---

### Task 14: Full verification and screenshots

**Files:** none (unless verification finds a defect)

- [ ] **Step 1: Full suites (Unity Editor closed)**

Run:

```bash
tools/run-unity-tests.sh EditMode "" final-edit
tools/run-unity-tests.sh PlayMode "" final-play
git status --short
```

Expected:
- Failures are a subset of the Task 0 baseline failures.
- The PlayMode run finishes; a hang means a scene auto-opened the guide (Review Focus 1).
- If configurator tests rewrote scene files that were clean in Task 0, restore them with `git checkout -- <files>`.

- [ ] **Step 2: Screenshots**

Invoke the `testing-unity-ui-with-screenshots` skill. Capture:
- The guide's first page and last page in each of MG_Sprint, MG_Football, MG_Volleyball, MG_FrogJump and MG_ChessFinal. A direct scene load uses the memory seen store, so the first-run guide appears.
- The pause menu with HƯỚNG DẪN in MG_Football (4 rows) and MG_FrogJump (2 rows).
- The review guide open over the pause menu.

Check each capture:
- No text overflows the card.
- Vietnamese diacritics render.
- The guide covers the game's own start card.
- BẮT ĐẦU / ĐÓNG show on the last page.

Fix any layout defect in `MinigameGuidePanel.Build` or `PausePanel`, re-run Task 4 and Task 6 tests, and commit as `fix(guide): ...`.

- [ ] **Step 3: Report**

Summarize for the user:
- Test counts against the baseline.
- Screenshot paths.
- That the first-run guide appears once per game, and that Football and Chess still show their own start button after BẮT ĐẦU.
