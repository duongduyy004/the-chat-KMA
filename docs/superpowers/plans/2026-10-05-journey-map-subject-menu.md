# Journey-Map Subject Menu Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the three-card row on the `Map` scene with a winding journey map (three circular stops on a curved road) above a shorter lesson panel, without changing unlock rules, routes or save data.

**Architecture:** `MapPresentationBuilder` keeps building `S5MapPresentation`, but each subject node becomes a fixed-size "stop" (tag, circular badge, name, star/status pill) built by a new `MapStopBuilder`. `MapJourneyPathLayout` positions stops on fixed fractions of the `SelectionGrid` rect and draws the road as pooled rotated `Image` segments sampled from two cubic Béziers. `MapScreen` decides which stop is *current* and which is *selected* and pushes that into `MapNodeView`. The lesson panel keeps its component tree and logic but is re-anchored to be shorter, with a subject-name-only heading. `Map.unity` is regenerated through `ShellSceneAuthoring.ApplyMap`.

**Tech Stack:** Unity 6000.3.23f1, uGUI + TextMeshPro, NUnit EditMode/PlayMode tests, runtime-drawn sprites (no new bitmaps or packages).

**Spec:** `docs/superpowers/specs/2026-10-05-journey-map-subject-menu-design.md`. Approved preview: `journey-map-preview.html` from the brainstorm session (1920×1080 reference frame).

## Global Constraints

- Landscape only; reference frame 1920×1080, `CanvasScaler` reference resolution 1920×1080 (`MinigameUIAssembler`).
- Header title text stays `CHỌN MÔN THI`; the `Subtitle` line is removed; lives label stays `Lượt thi: n/5`; back button still calls `SceneRouter.Instance?.RouteToMenu()`.
- Panel heading is the subject name only (`Chạy nước rút`, `Bóng chuyền`, `Bóng đá`); no `Chương NN ·` prefix.
- Do not change `JourneyProgress`, `GameSession`, `SaveData`, `SceneRouter`, or the `onSelected`/`SubjectRequested`/`ChallengeRequested` contracts.
- Keep existing status strings in `MapNodeView`: `SẴN SÀNG`, `HOÀN THÀNH`, `CHƯA MỞ KHÓA`, `ĐANG PHÁT TRIỂN`; `DetailText` semantics for hand-bound nodes (`MapNode_ReportsReadyCompletedAndUnavailableStates`) must not change.
- Keep `SelectionGrid` carrying a disabled `GridLayoutGroup` and `ResponsiveGridLayout` (`ShellSceneAuthoring.Validate` requires both) and keep object names `S5MapPresentation/Content/{Header,SelectionGrid,JourneyLessons}`, `<Subject>Node`, `Lesson1..3`.
- Every TMP label built by `MapPresentationBuilder` keeps the existing pattern `LayoutLabel/TextTmp` (container + stretched label): tests require non-lesson labels to be anchored `Vector2.zero..one` inside their container.
- Fonts via `VietTypography`; colours from `UITheme.Shared` / `HomeMenuStyle`; no hard-coded colours in builders except through new `LessonJourneyStyle` fields.
- `UITheme.asset` already holds serialized values for `LessonJourneyStyle`; changing a field initializer in C# does **not** change them. Existing fields are changed by editing `Assets/_Project/Settings/UI/UITheme.asset`; new fields rely on their C# initializers.
- Commit directly to `master`; commit messages carry **no** `Co-Authored-By` trailer (user rule, overrides the harness default).
- Do not stage the unrelated working-tree changes (`.claude/settings.json`, `Bootstrap.unity`, `Menu.unity`, `HomePresentationBuilder.cs`, `SplashPresentationView.cs`, `docs/qa/images/story-course-complete.png`). Always `git add` explicit paths.
- Unity must be closed for batch-mode runs. Run tests with `tools/run-unity-tests.sh <EditMode|PlayMode> <filter> <name>`; read the XML in `Builds/TestResults/`, not the exit code.

## Review Focus

Failure modes the spec implies but no happy-path task exercises, each pinned by a test in the owning task:

1. Fresh game: Volleyball and Football are locked → they must show lock icon, `CHƯA MỞ KHÓA`, empty stars, and not be clickable; only Sprint is current (Task 2, Task 4).
2. Course complete (all three passed): no stop is current, no `ĐANG Ở ĐÂY` tag, summary strip visible without overlapping stops or panel (Task 4, Task 6).
3. Passed subject with a low rank (e.g. rank C → 1 star): stars must reflect `Stars`, not always 3 (Task 2).
4. Rebuild on an existing scene (`Build` called twice, or scene reopened) must not duplicate road segments, stops or panels (Task 3, Task 6).
5. Aspect ratios 1280×720, 1440×1080, 1728×1080, 1920×1080, 2400×1080: no stop label clipped, no stop overlapping the lesson panel or header (Task 3, Task 6).

## File Structure

| File | Responsibility |
| --- | --- |
| `Assets/_Project/Scripts/UI/MapStopBuilder.cs` (create) | Builds one stop (`<Subject>Node`) hierarchy and the runtime star/check sprites. Pure construction, no layout across stops. |
| `Assets/_Project/Scripts/UI/MapNodeView.cs` (modify) | Binds the stop's new parts; renders locked / ready / completed / current / selected state and stars. |
| `Assets/_Project/Scripts/UI/MapJourneyPathLayout.cs` (rewrite) | Places the three stops and draws/updates the curved road. |
| `Assets/_Project/Scripts/UI/MapPresentationBuilder.cs` (modify) | Compact header, uses `MapStopBuilder`, new map/panel anchors, summary strip placement. |
| `Assets/_Project/Scripts/UI/MapScreen.cs` (modify) | Derives current/selected stop and applies it to nodes. |
| `Assets/_Project/Scripts/UI/JourneyLessonList.cs` (modify) | Heading text, `SelectedSubject`, lock-state colours, card text sizes. |
| `Assets/_Project/Scripts/UI/JourneyLessonPresentation.cs` (modify) | Compact panel geometry and card layout. |
| `Assets/_Project/Scripts/UI/UITheme.cs` + `Assets/_Project/Settings/UI/UITheme.asset` (modify) | New map/stop style fields; new anchor values. |
| `Assets/Tests/EditMode/Presentation/UIComponentTests.cs`, `Assets/Tests/PlayMode/Progression/S5NewGameTests.cs`, `Assets/Tests/PlayMode/Presentation/FestivalUiExperienceTests.cs` (modify) | Updated contracts + new tests. |
| `Assets/_Project/Scenes/Map.unity` (regenerate) | Re-authored via `ShellSceneAuthoring.ApplyMap`. |
| `docs/qa/journey-map-menu.md` (create) | QA evidence. |

---

### Task 1: Style fields and `MapNodeView` journey-map state

**Files:**
- Modify: `Assets/_Project/Scripts/UI/UITheme.cs` (`LessonJourneyStyle`, after `glowAlpha`)
- Modify: `Assets/_Project/Scripts/UI/MapNodeView.cs`
- Test: `Assets/Tests/EditMode/Presentation/UIComponentTests.cs`

**Interfaces:**
- Produces (`UITheme.LessonJourneyStyle`): `Vector2 mapAnchorMin`, `Vector2 mapAnchorMax`, `Vector2 stopSize`, `float stopBadgeSize`, `float stopCurrentScale`, `float stopTagHeight`, `float[] stopX`, `float[] stopY`, `float roadOutlineWidth`, `float roadFillWidth`, `float roadDotSize`.
- Produces (`MapNodeView`): `bool IsCompleted`, `bool IsLocked`, `bool IsCurrent`, `bool IsSelected`, `string StatusText`, `void BindJourneyStop(Image badge, Image[] stars, GameObject lockIcon, GameObject doneMark, GameObject currentTag, Image selectionRing, RectTransform badgeRect, RectTransform labelGroup)`, `void SetJourneyMarkers(bool current, bool selected)`.
- Consumes: existing `MapNodeView.ConfigureJourneyState`, `RenderAvailability`.

- [ ] **Step 1: Write the failing tests** — append to `UIComponentTests` (inside the class, near the other `MapNode*` tests):

```csharp
[Test]
public void MapNodeJourneyStop_ShowsLockStarsAndMarkersFromState()
{
    var node = new GameObject("Stop", typeof(RectTransform)).AddComponent<MapNodeView>();
    try
    {
        var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image))
            .GetComponent<Image>();
        badge.transform.SetParent(node.transform, false);
        var stars = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            stars[i] = new GameObject("Star" + i, typeof(RectTransform), typeof(Image))
                .GetComponent<Image>();
            stars[i].transform.SetParent(node.transform, false);
        }
        var lockIcon = new GameObject("LockIcon", typeof(RectTransform));
        var doneMark = new GameObject("DoneMark", typeof(RectTransform));
        var tag = new GameObject("CurrentTag", typeof(RectTransform));
        var ring = new GameObject("SelectionRing", typeof(RectTransform), typeof(Image))
            .GetComponent<Image>();
        var labelGroup = new GameObject("LabelGroup", typeof(RectTransform)).GetComponent<RectTransform>();
        foreach (GameObject part in new[] { lockIcon, doneMark, tag, ring.gameObject, labelGroup.gameObject })
            part.transform.SetParent(node.transform, false);
        node.BindJourneyStop(badge, stars, lockIcon, doneMark, tag, ring,
            badge.rectTransform, labelGroup);

        node.ConfigureJourneyState(SubjectId.Volleyball, "Bóng chuyền", false, false, null);
        Assert.That(node.IsLocked, Is.True);
        Assert.That(lockIcon.activeSelf, Is.True);
        Assert.That(doneMark.activeSelf, Is.False);
        Assert.That(node.StatusText, Is.EqualTo("CHƯA MỞ KHÓA"));
        Assert.That(stars.Count(star => star.color == UITheme.Shared.Menu.gold), Is.Zero);

        var record = new SubjectRecord();
        record.Accept(new MinigameResult(true, 0f, Rank.C));
        node.ConfigureJourneyState(SubjectId.Volleyball, "Bóng chuyền", true, true, record);
        Assert.That(node.IsLocked, Is.False);
        Assert.That(node.IsCompleted, Is.True);
        Assert.That(lockIcon.activeSelf, Is.False);
        Assert.That(doneMark.activeSelf, Is.True);
        Assert.That(node.StatusText, Is.EqualTo("HOÀN THÀNH"));
        int filled = stars.Count(star => star.color == UITheme.Shared.Menu.gold);
        Assert.That(filled, Is.EqualTo(node.Stars), "Filled stars must equal Stars, not always 3.");
        Assert.That(node.Stars, Is.LessThan(3));

        node.SetJourneyMarkers(true, true);
        Assert.That(node.IsCurrent, Is.True);
        Assert.That(tag.activeSelf, Is.True);
        Assert.That(ring.gameObject.activeSelf, Is.True);
        Assert.That(badge.rectTransform.localScale.x,
            Is.EqualTo(UITheme.Shared.LessonJourney.stopCurrentScale).Within(.001f));

        node.SetJourneyMarkers(false, false);
        Assert.That(tag.activeSelf, Is.False);
        Assert.That(ring.gameObject.activeSelf, Is.False);
        Assert.That(badge.rectTransform.localScale.x, Is.EqualTo(1f).Within(.001f));
    }
    finally { Object.DestroyImmediate(node.gameObject); }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `tools/run-unity-tests.sh EditMode MapNodeJourneyStop_ShowsLockStarsAndMarkersFromState journey-stop-red`
Expected: compile failure (`BindJourneyStop`, `IsLocked`, … not defined) — the XML is not produced or the test is reported as an error.

- [ ] **Step 3: Add the style fields** — in `UITheme.cs`, inside `LessonJourneyStyle`, after the `glowAlpha` line:

```csharp
            // Journey map: the map zone sits between the compact header and the lesson panel.
            public Vector2 mapAnchorMin = new Vector2(0f, .35f);
            public Vector2 mapAnchorMax = new Vector2(1f, .915f);
            // Fixed-size stop: tag + badge area + name + star/status pill.
            public Vector2 stopSize = new Vector2(320f, 400f);
            public float stopBadgeSize = 170f;
            public float stopCurrentScale = 1.3f;
            public float stopTagHeight = 52f;
            // Badge-centre positions inside the map zone (x of width, y of height).
            public float[] stopX = { .17f, .5f, .83f };
            public float[] stopY = { .45f, .70f, .45f };
            public float roadOutlineWidth = 30f;
            public float roadFillWidth = 16f;
            public float roadDotSize = 12f;
```

- [ ] **Step 4: Implement the state API in `MapNodeView`** — add fields after `detailVisibilityRoot`:

```csharp
        [SerializeField] Image badgeImage;
        [SerializeField] Image[] starImages = new Image[0];
        [SerializeField] GameObject lockIcon;
        [SerializeField] GameObject doneMark;
        [SerializeField] GameObject currentTag;
        [SerializeField] Image selectionRing;
        [SerializeField] RectTransform badgeRect;
        [SerializeField] RectTransform labelGroup;
```

Add properties next to `Lives`:

```csharp
        public bool IsCompleted => completed;
        public bool IsLocked { get; private set; }
        public bool IsCurrent { get; private set; }
        public bool IsSelected { get; private set; }
        public string StatusText => statusLabel == null ? string.Empty : statusLabel.text;
```

Add the binding and marker methods after `BindPresentation`:

```csharp
        public void BindJourneyStop(Image badge, Image[] stars, GameObject lockPictogram,
            GameObject completedMark, GameObject currentMarker, Image ring,
            RectTransform badgeTransform, RectTransform labels)
        {
            badgeImage = badge;
            starImages = stars ?? new Image[0];
            lockIcon = lockPictogram;
            doneMark = completedMark;
            currentTag = currentMarker;
            selectionRing = ring;
            badgeRect = badgeTransform;
            labelGroup = labels;
            SetJourneyMarkers(false, false);
        }

        public void SetJourneyMarkers(bool current, bool selected)
        {
            IsCurrent = current;
            IsSelected = selected;
            if (currentTag != null) currentTag.SetActive(current);
            if (selectionRing != null) selectionRing.gameObject.SetActive(selected || current);
            if (badgeRect != null)
                badgeRect.localScale = Vector3.one * (current ? UITheme.Shared.LessonJourney.stopCurrentScale : 1f);
            if (labelGroup != null)
            {
                // Names sit 22px under a normal badge and 26px + the extra radius under the current one.
                float badge = UITheme.Shared.LessonJourney.stopBadgeSize;
                float extra = current ? badge * (UITheme.Shared.LessonJourney.stopCurrentScale - 1f) * .5f : 0f;
                labelGroup.anchoredPosition = new Vector2(0f, -extra);
            }
        }
```

In `ApplyVisualState`, append at the end (before the closing brace):

```csharp
            IsLocked = locked;
            if (lockIcon != null) lockIcon.SetActive(locked);
            if (doneMark != null) doneMark.SetActive(completed && !locked);
            if (badgeImage != null) badgeImage.color = locked ? LockedCard : subjectColor;
            for (int i = 0; i < starImages.Length; i++)
                if (starImages[i] != null)
                    starImages[i].color = i < Stars && !locked
                        ? UITheme.Shared.Menu.gold : UITheme.Shared.MapLockedIcon;
```

Add the current-stop glow to `MapNodeView` (new method, any position in the class) and reset the alpha when markers change — append `if (selectionRing != null) selectionRing.color = HomeMenuStyle.Gold;` as the last line of `SetJourneyMarkers`:

```csharp
        void Update()
        {
            if (!IsCurrent || selectionRing == null) return;
            float pulse = (Mathf.Sin(Time.unscaledTime * UITheme.Shared.LessonJourney.glowSpeed) + 1f) * .5f;
            Color gold = HomeMenuStyle.Gold;
            selectionRing.color = new Color(gold.r, gold.g, gold.b, Mathf.Lerp(.55f, 1f, pulse));
        }
```

Add to the Task 1 test, right after the `SetJourneyMarkers(true, true)` assertions: `Assert.That(ring.color.a, Is.EqualTo(1f).Within(.001f));` (alpha is reset by `SetJourneyMarkers`; `Update` does not run in EditMode).

Note: `ApplyVisualState` already reads `completed` as a parameter that shadows the field; the body uses the parameter, which equals the field at every call site.

- [ ] **Step 5: Run to verify it passes**

Run: `tools/run-unity-tests.sh EditMode MapNodeJourneyStop_ShowsLockStarsAndMarkersFromState journey-stop-green`
Expected: 1 passed. Then run `tools/run-unity-tests.sh PlayMode MapNode_ReportsReadyCompletedAndUnavailableStates journey-stop-regress` (the test lives in `Assets/Tests/PlayMode/Progression/S5NewGameTests.cs`) — expected: still passes (hand-bound nodes have no stop parts).

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/UI/UITheme.cs Assets/_Project/Scripts/UI/MapNodeView.cs Assets/Tests/EditMode/Presentation/UIComponentTests.cs
git commit -m "feat(map): add journey stop state to MapNodeView and stop style fields"
```

---

### Task 2: Build the circular stops (`MapStopBuilder`)

**Files:**
- Create: `Assets/_Project/Scripts/UI/MapStopBuilder.cs`
- Modify: `Assets/_Project/Scripts/UI/MapPresentationBuilder.cs` (`Card`, `Build`, remove `FutureRow`/`FutureChip` only if unused — leave them, they are already dead code and out of scope)
- Modify: `Assets/_Project/Scripts/UI/MapPresentationBuilder.cs` (`SportIconSprite`/`LockSprite`/`DrawLine` made `internal static` so the new builder can reuse them)
- Test: `Assets/Tests/EditMode/Presentation/UIComponentTests.cs`

**Interfaces:**
- Consumes: Task 1 `BindJourneyStop`, `LessonJourneyStyle` fields; existing `MapPresentationBuilder.SportIconSprite(SubjectId)`, `LockSprite()`, `DrawLine`, `DrawCircle`, `SetIconPixel`, `LayoutLabel`, `Rect`, `Stretch`; `UiKitAssets.Load().Circle/RoundRect20`.
- Produces: `internal static MapNodeView MapStopBuilder.Build(Transform parent, MapScreen screen, SubjectId subject, string label, Color accent, int order)`; node hierarchy:

```
<Subject>Node            (RectTransform stopSize, Image transparent raycast, MapNodeView, BrutalButton, Button)
  CurrentTag             (Image RoundRect20 gold + Outline navy) / TagContainer/Tag (TMP "ĐANG Ở ĐÂY")
  SelectionRing          (Image Circle gold, behind Badge, 14px larger each side)
  Badge                  (Image Circle, white border via Outline, Shadow)
    IconGlyph            (Image, SportIconSprite, preserveAspect)
    OrderBadge           (Image Circle navy) / OrderContainer/Order (TMP "1")
    DoneMark             (Image Circle gold) / CheckGlyph (Image, runtime check sprite)
    LockIcon             (Image LockSprite, bottom-right)
  LabelGroup             (RectTransform)
    TitleContainer/Title (TMP, font 30 bold)
    MetaPill             (Image RoundRect20 navy 80%)
      Stars              (3× Star1..3 Image, runtime star sprite)
      StatusContainer/Status (TMP font 20)
```

- [ ] **Step 1: Write the failing tests** — add to `UIComponentTests`:

```csharp
[Test]
public void MapStops_FreshGameShowsOneReadyStopAndTwoLockedStopsEachWithStarsAndStatus()
{
    var root = new GameObject("map", typeof(RectTransform));
    try
    {
        var screen = root.AddComponent<MapScreen>();
        MapPresentationBuilder.Build(screen, new GameSession());

        foreach (MapNodeView node in screen.Nodes)
        {
            Assert.That(node.transform.Find("Badge/IconGlyph").GetComponent<Image>().sprite, Is.Not.Null, node.name);
            Assert.That(node.transform.Find("Badge/OrderBadge"), Is.Not.Null, node.name);
            Assert.That(node.transform.Find("LabelGroup/TitleContainer/Title").GetComponent<TMP_Text>().text,
                Is.EqualTo(node.DisplayName), node.name);
            Transform pill = node.transform.Find("LabelGroup/MetaPill");
            Assert.That(pill, Is.Not.Null, node.name + " must always show stars and status");
            Assert.That(pill.Find("Stars").childCount, Is.EqualTo(3), node.name);
            Assert.That(pill.Find("StatusContainer/Status").GetComponent<TMP_Text>().text, Is.Not.Empty, node.name);
        }

        MapNodeView sprint = screen.Nodes[0], volley = screen.Nodes[1], football = screen.Nodes[2];
        Assert.That(sprint.IsLocked, Is.False);
        Assert.That(sprint.StatusText, Is.EqualTo("SẴN SÀNG"));
        Assert.That(sprint.transform.Find("Badge/LockIcon").gameObject.activeSelf, Is.False);
        foreach (MapNodeView locked in new[] { volley, football })
        {
            Assert.That(locked.IsLocked, Is.True, locked.name);
            Assert.That(locked.StatusText, Is.EqualTo("CHƯA MỞ KHÓA"), locked.name);
            Assert.That(locked.transform.Find("Badge/LockIcon").gameObject.activeSelf, Is.True, locked.name);
            Assert.That(locked.GetComponent<Button>().interactable, Is.False, locked.name);
        }
    }
    finally { Object.DestroyImmediate(root); }
}

[Test]
public void MapStops_UseCircularBadgesAndRuntimeSprites()
{
    var root = new GameObject("map", typeof(RectTransform));
    try
    {
        var screen = root.AddComponent<MapScreen>();
        MapPresentationBuilder.Build(screen, new GameSession());
        var stars = new HashSet<Sprite>();
        foreach (MapNodeView node in screen.Nodes)
        {
            Image badge = node.transform.Find("Badge").GetComponent<Image>();
            Assert.That(badge.sprite, Is.SameAs(KMA.UI.Kit.UiKitAssets.Load().Circle), node.name);
            Assert.That(badge.type, Is.EqualTo(Image.Type.Simple), node.name);
            Assert.That(badge.GetComponent<Shadow>(), Is.Not.Null, node.name);
            foreach (Image star in node.transform.Find("LabelGroup/MetaPill/Stars").GetComponentsInChildren<Image>())
            {
                Assert.That(star.sprite, Is.Not.Null, node.name);
                stars.Add(star.sprite);
            }
            Assert.That(((RectTransform)node.transform).sizeDelta,
                Is.EqualTo(UITheme.Shared.LessonJourney.stopSize), node.name);
        }
        Assert.That(stars, Has.Count.EqualTo(1), "All stars share one runtime sprite.");
        Assert.That(screen.Nodes[2].transform.Find("Badge/FinishFlag"), Is.Not.Null);
        Assert.That(screen.Nodes[0].transform.Find("Badge/FinishFlag"), Is.Null);
        Assert.That(screen.Nodes[1].transform.Find("Badge/FinishFlag"), Is.Null);
    }
    finally { Object.DestroyImmediate(root); }
}

[Test]
public void MapStops_PassedSubjectShowsDoneMarkAndRankStars()
{
    var root = new GameObject("map", typeof(RectTransform));
    try
    {
        var screen = root.AddComponent<MapScreen>();
        MapPresentationBuilder.Build(screen, CompleteSprintJourney());
        MapNodeView sprint = screen.Nodes.Single(node => node.SubjectId == SubjectId.Sprint);
        Assert.That(sprint.IsCompleted, Is.True);
        Assert.That(sprint.StatusText, Is.EqualTo("HOÀN THÀNH"));
        Assert.That(sprint.transform.Find("Badge/DoneMark").gameObject.activeSelf, Is.True);
        int filled = sprint.transform.Find("LabelGroup/MetaPill/Stars").GetComponentsInChildren<Image>()
            .Count(star => star.color == UITheme.Shared.Menu.gold);
        Assert.That(filled, Is.EqualTo(sprint.Stars));
    }
    finally { Object.DestroyImmediate(root); }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `tools/run-unity-tests.sh EditMode MapStops_ stops-red`
Expected: 3 failures (`Badge`, `LabelGroup` paths are null).

- [ ] **Step 3: Expose helpers** — in `MapPresentationBuilder.cs` change these signatures from `static` to `internal static`: `LockSprite`, `DrawCircle`, `DrawLine`, `SetIconPixel`, `Rect`, `Stretch`, `Anchor`, `TextTmp`, `LayoutLabel`, `UseRoundedSurface`, `ButtonColors`. (`SportIconSprite` is already `internal static`.)

- [ ] **Step 4: Create `MapStopBuilder.cs`:**

```csharp
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    // Builds one stop of the journey map. Cross-stop layout and the road belong to
    // MapJourneyPathLayout; selection/current state is pushed in by MapScreen.
    internal static class MapStopBuilder
    {
        static Sprite starSprite;
        static Sprite checkSprite;
        static Sprite flagSprite;
        static UITheme.LessonJourneyStyle Style => UITheme.Shared.LessonJourney;

        public static MapNodeView Build(Transform parent, MapScreen screen, SubjectId subject,
            string label, Color accent, int order)
        {
            RectTransform root = MapPresentationBuilder.Rect(parent, subject + "Node");
            root.sizeDelta = Style.stopSize;
            Image hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<BrutalButton>();

            float badgeSize = Style.stopBadgeSize;
            float badgeCenterFromTop = Style.stopTagHeight + badgeSize * Style.stopCurrentScale * .5f;

            // Tag above the badge ("ĐANG Ở ĐÂY"), shown only on the current stop.
            RectTransform tag = MapPresentationBuilder.Rect(root, "CurrentTag");
            Place(tag, new Vector2(.5f, 1f), new Vector2(190f, 38f), new Vector2(0f, -4f));
            Image tagSurface = tag.gameObject.AddComponent<Image>();
            tagSurface.sprite = UiKitAssets.Load().RoundRect20;
            tagSurface.type = Image.Type.Sliced;
            tagSurface.color = HomeMenuStyle.Gold;
            tagSurface.raycastTarget = false;
            Outline tagOutline = tag.gameObject.AddComponent<Outline>();
            tagOutline.effectColor = HomeMenuStyle.Navy;
            tagOutline.effectDistance = new Vector2(3f, -3f);
            MapPresentationBuilder.LayoutLabel(tag, "Tag", "ĐANG Ở ĐÂY", 20, HomeMenuStyle.Navy,
                TextAnchor.MiddleCenter);
            StretchChild(tag);

            // Gold ring behind the badge (current or selected).
            RectTransform ring = MapPresentationBuilder.Rect(root, "SelectionRing");
            Place(ring, new Vector2(.5f, 1f), Vector2.one * (badgeSize + 28f), new Vector2(0f, -badgeCenterFromTop));
            ring.pivot = new Vector2(.5f, .5f);
            Image ringImage = ring.gameObject.AddComponent<Image>();
            ringImage.sprite = UiKitAssets.Load().Circle;
            ringImage.color = HomeMenuStyle.Gold;
            ringImage.raycastTarget = false;
            ring.localScale = Vector3.one;

            // Badge.
            RectTransform badge = MapPresentationBuilder.Rect(root, "Badge");
            Place(badge, new Vector2(.5f, 1f), Vector2.one * badgeSize, new Vector2(0f, -badgeCenterFromTop));
            badge.pivot = new Vector2(.5f, .5f);
            Image badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.sprite = UiKitAssets.Load().Circle;
            badgeImage.color = accent;
            badgeImage.raycastTarget = false;
            Outline border = badge.gameObject.AddComponent<Outline>();
            border.effectColor = Color.white;
            border.effectDistance = new Vector2(6f, -6f);
            Shadow shadow = badge.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .4f);
            shadow.effectDistance = new Vector2(0f, -8f);

            RectTransform glyph = MapPresentationBuilder.Rect(badge, "IconGlyph");
            MapPresentationBuilder.Stretch(glyph, new Vector2(34f, 34f), new Vector2(-34f, -34f));
            Image glyphImage = glyph.gameObject.AddComponent<Image>();
            glyphImage.sprite = MapPresentationBuilder.SportIconSprite(subject);
            glyphImage.color = UITheme.Shared.Surface;
            glyphImage.preserveAspect = true;
            glyphImage.raycastTarget = false;

            RectTransform orderBadge = MapPresentationBuilder.Rect(badge, "OrderBadge");
            Place(orderBadge, new Vector2(.14f, .86f), Vector2.one * 44f, Vector2.zero);
            orderBadge.pivot = new Vector2(.5f, .5f);
            Image orderImage = orderBadge.gameObject.AddComponent<Image>();
            orderImage.sprite = UiKitAssets.Load().Circle;
            orderImage.color = HomeMenuStyle.Navy;
            orderImage.raycastTarget = false;
            MapPresentationBuilder.LayoutLabel(orderBadge, "Order", order.ToString(), 24, HomeMenuStyle.Gold,
                TextAnchor.MiddleCenter);
            StretchChild(orderBadge);

            RectTransform done = MapPresentationBuilder.Rect(badge, "DoneMark");
            Place(done, new Vector2(.86f, .86f), Vector2.one * 48f, Vector2.zero);
            done.pivot = new Vector2(.5f, .5f);
            Image doneImage = done.gameObject.AddComponent<Image>();
            doneImage.sprite = UiKitAssets.Load().Circle;
            doneImage.color = HomeMenuStyle.Gold;
            doneImage.raycastTarget = false;
            RectTransform check = MapPresentationBuilder.Rect(done, "CheckGlyph");
            MapPresentationBuilder.Stretch(check, new Vector2(10f, 10f), new Vector2(-10f, -10f));
            Image checkImage = check.gameObject.AddComponent<Image>();
            checkImage.sprite = CheckSprite();
            checkImage.color = HomeMenuStyle.Navy;
            checkImage.preserveAspect = true;
            checkImage.raycastTarget = false;

            RectTransform lockRect = MapPresentationBuilder.Rect(badge, "LockIcon");
            Place(lockRect, new Vector2(.84f, .14f), Vector2.one * 52f, Vector2.zero);
            lockRect.pivot = new Vector2(.5f, .5f);
            Image lockPlate = lockRect.gameObject.AddComponent<Image>();
            lockPlate.sprite = UiKitAssets.Load().Circle;
            lockPlate.color = HomeMenuStyle.Navy;
            lockPlate.raycastTarget = false;
            RectTransform lockGlyph = MapPresentationBuilder.Rect(lockRect, "Glyph");
            MapPresentationBuilder.Stretch(lockGlyph, new Vector2(11f, 9f), new Vector2(-11f, -13f));
            Image lockImage = lockGlyph.gameObject.AddComponent<Image>();
            lockImage.sprite = MapPresentationBuilder.LockSprite();
            lockImage.color = Color.white;
            lockImage.preserveAspect = true;
            lockImage.raycastTarget = false;

            if (order == 3)
            {
                RectTransform flag = MapPresentationBuilder.Rect(badge, "FinishFlag");
                Place(flag, new Vector2(1f, 1f), Vector2.one * 56f, new Vector2(-2f, 18f));
                flag.pivot = new Vector2(.5f, .5f);
                Image flagImage = flag.gameObject.AddComponent<Image>();
                flagImage.sprite = FlagSprite();
                flagImage.preserveAspect = true;
                flagImage.raycastTarget = false;
                flag.gameObject.AddComponent<Outline>().effectColor = HomeMenuStyle.Navy;
            }

            // Name + meta pill below the badge.
            RectTransform labelGroup = MapPresentationBuilder.Rect(root, "LabelGroup");
            float labelTop = badgeCenterFromTop + badgeSize * .5f + 22f;
            Place(labelGroup, new Vector2(.5f, 1f), new Vector2(Style.stopSize.x, 120f), new Vector2(0f, -labelTop));
            labelGroup.pivot = new Vector2(.5f, 1f);

            TMP_Text title = MapPresentationBuilder.LayoutLabel(labelGroup, "Title", label, 30,
                Color.white, TextAnchor.MiddleCenter);
            RectTransform titleBox = (RectTransform)title.transform.parent;
            titleBox.anchorMin = new Vector2(0f, 1f);
            titleBox.anchorMax = Vector2.one;
            titleBox.pivot = new Vector2(.5f, 1f);
            titleBox.offsetMin = new Vector2(0f, -48f);
            titleBox.offsetMax = Vector2.zero;
            title.fontStyle = FontStyles.Bold;
            title.enableWordWrapping = false;
            title.outlineWidth = .2f;
            title.outlineColor = HomeMenuStyle.Navy;

            RectTransform pill = MapPresentationBuilder.Rect(labelGroup, "MetaPill");
            pill.anchorMin = pill.anchorMax = new Vector2(.5f, 1f);
            pill.pivot = new Vector2(.5f, 1f);
            pill.sizeDelta = new Vector2(Style.stopSize.x - 20f, 44f);
            pill.anchoredPosition = new Vector2(0f, -54f);
            Image pillImage = pill.gameObject.AddComponent<Image>();
            pillImage.sprite = UiKitAssets.Load().RoundRect20;
            pillImage.type = Image.Type.Sliced;
            pillImage.color = MinigameUiTheme.WithAlpha(HomeMenuStyle.Navy, .8f);
            pillImage.raycastTarget = false;

            RectTransform starsRoot = MapPresentationBuilder.Rect(pill, "Stars");
            starsRoot.anchorMin = new Vector2(0f, 0f);
            starsRoot.anchorMax = new Vector2(.34f, 1f);
            starsRoot.offsetMin = new Vector2(12f, 4f);
            starsRoot.offsetMax = new Vector2(0f, -4f);
            var row = starsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 2f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform star = MapPresentationBuilder.Rect(starsRoot, "Star" + (i + 1));
                stars[i] = star.gameObject.AddComponent<Image>();
                stars[i].sprite = StarSprite();
                stars[i].preserveAspect = true;
                stars[i].raycastTarget = false;
                var element = star.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = element.preferredHeight = 30f;
            }

            TMP_Text status = MapPresentationBuilder.LayoutLabel(pill, "Status", string.Empty, 20,
                HomeMenuStyle.GoldLight, TextAnchor.MiddleRight);
            RectTransform statusBox = (RectTransform)status.transform.parent;
            statusBox.name = "StatusContainer";
            statusBox.anchorMin = new Vector2(.34f, 0f);
            statusBox.anchorMax = Vector2.one;
            statusBox.offsetMin = new Vector2(0f, 2f);
            statusBox.offsetMax = new Vector2(-14f, -2f);
            status.enableWordWrapping = false;
            status.fontStyle = FontStyles.Bold;

            MapNodeView node = root.gameObject.AddComponent<MapNodeView>();
            node.Bind(button, title, null);
            node.BindPresentation(status, null, badgeImage, glyphImage, border, accent);
            node.BindJourneyStop(badgeImage, stars, lockRect.gameObject, done.gameObject,
                tag.gameObject, ringImage, badge, labelGroup);
            node.Configure(subject, label, false, null, 5);
            button.onClick.AddListener(() => screen.SelectSubject(subject));
            return node;
        }

        // The tag/order labels are created by LayoutLabel inside a container; stretch it.
        static void StretchChild(RectTransform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = (RectTransform)parent.GetChild(i);
                if (child.name.EndsWith("Container"))
                    MapPresentationBuilder.Stretch(child, Vector2.zero, Vector2.zero);
            }
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        internal static Sprite StarSprite()
        {
            if (starSprite != null) return starSprite;
            const int size = 64;
            var polygon = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float radius = i % 2 == 0 ? 30f : 12.5f;
                float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
                polygon[i] = new Vector2(32f + Mathf.Cos(angle) * radius, 34f + Mathf.Sin(angle) * radius);
            }
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (Inside(polygon, x + .5f, y + .5f))
                    pixels[y * size + x] = new Color32(255, 255, 255, 255);
            starSprite = MakeSprite("RuntimeStarIcon", size, pixels);
            return starSprite;
        }

        internal static Sprite FlagSprite()
        {
            if (flagSprite != null) return flagSprite;
            const int size = 64;
            var pixels = new Color32[size * size];
            // Pole, then a 4x3 chequered cloth.
            for (int y = 4; y < 60; y++)
            for (int x = 8; x < 13; x++)
                pixels[y * size + x] = new Color32(255, 255, 255, 255);
            for (int cy = 0; cy < 3; cy++)
            for (int cx = 0; cx < 4; cx++)
            {
                bool dark = (cx + cy) % 2 == 0;
                for (int y = 0; y < 12; y++)
                for (int x = 0; x < 11; x++)
                    pixels[(36 + cy * 12 + y) * size + 13 + cx * 11 + x] = dark
                        ? new Color32(11, 42, 74, 255) : new Color32(255, 255, 255, 255);
            }
            flagSprite = MakeSprite("RuntimeFlagIcon", size, pixels);
            return flagSprite;
        }

        internal static Sprite CheckSprite()
        {
            if (checkSprite != null) return checkSprite;
            const int size = 64;
            var pixels = new Color32[size * size];
            MapPresentationBuilder.DrawLine(pixels, size, 10, 34, 25, 18, 10);
            MapPresentationBuilder.DrawLine(pixels, size, 25, 18, 54, 48, 10);
            checkSprite = MakeSprite("RuntimeCheckIcon", size, pixels);
            return checkSprite;
        }

        static Sprite MakeSprite(string name, int size, Color32[] pixels)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static bool Inside(Vector2[] polygon, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                bool crosses = (polygon[i].y > y) != (polygon[j].y > y) &&
                    x < (polygon[j].x - polygon[i].x) * (y - polygon[i].y) /
                    (polygon[j].y - polygon[i].y) + polygon[i].x;
                if (crosses) inside = !inside;
            }
            return inside;
        }
    }
}
```

Because `starSprite`/`checkSprite` are `HideAndDontSave` runtime objects, tests that destroy them (like `MapRebuildRecreatesDestroyedProceduralSportSprites`) must not leave a stale static. Guard both getters with `if (starSprite != null)` (already `UnityEngine.Object` null semantics) — already done above.

- [ ] **Step 5: Use it in `MapPresentationBuilder.Build`** — replace the `foreach (Entry entry in Entries) nodes.Add(Card(...))` line with:

```csharp
            for (int i = 0; i < Entries.Length; i++)
                nodes.Add(MapStopBuilder.Build(grid, screen, Entries[i].Subject, Entries[i].Label,
                    Entries[i].Color, i + 1));
```

Delete the `Card` method (the old card) and `AddCornerLockIcon` if nothing else calls it (`grep -n AddCornerLockIcon` — only `Card` did). Keep `GridLayoutGroup`/`ResponsiveGridLayout` setup on `SelectionGrid`.

- [ ] **Step 6: Run to verify it passes**

Run: `tools/run-unity-tests.sh EditMode MapStops_ stops-green`
Expected: 3 passed. (Other existing Map tests will now fail; they are fixed in Task 5. Do not fix them here.)

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/UI/MapStopBuilder.cs Assets/_Project/Scripts/UI/MapPresentationBuilder.cs Assets/Tests/EditMode/Presentation/UIComponentTests.cs
git commit -m "feat(map): build circular journey stops with stars, lock and current tag"
```

---

### Task 3: Curved road and stop placement (`MapJourneyPathLayout`)

**Files:**
- Modify (rewrite): `Assets/_Project/Scripts/UI/MapJourneyPathLayout.cs`
- Test: `Assets/Tests/EditMode/Presentation/UIComponentTests.cs`

**Interfaces:**
- Consumes: Task 1 style fields, `MapNodeView.IsCompleted`, `MapNodeView.SubjectId`.
- Produces: `public void Configure(IEnumerable<MapNodeView> mapNodes)` (unchanged signature); `public void Refresh()` (re-lays out immediately; used by tests); road containers `PathTrack1`, `PathTrack2` under the grid, each with children `Outline{n}`, `Fill{n}`, `Dot{n}`; public `Vector2 StopCenter(int index)` returning the badge centre in the grid's local space.

- [ ] **Step 1: Write the failing tests:**

```csharp
static (MapScreen screen, RectTransform grid) BuildMapAt(GameObject root, Vector2 size, GameSession session)
{
    ((RectTransform)root.transform).sizeDelta = size;
    var screen = root.AddComponent<MapScreen>();
    MapPresentationBuilder.Build(screen, session);
    Canvas.ForceUpdateCanvases();
    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
    var grid = (RectTransform)root.transform.Find("S5MapPresentation/Content/SelectionGrid");
    grid.GetComponent<MapJourneyPathLayout>().Refresh();
    return (screen, grid);
}

[Test]
public void JourneyRoad_PassesThroughBadgeCentresAndIsBuiltOnce(
    [Values(1280f, 1920f, 2400f)] float width)
{
    var root = new GameObject("road", typeof(RectTransform));
    try
    {
        var (screen, grid) = BuildMapAt(root, new Vector2(width, 1080f), new GameSession());
        MapPresentationBuilder.Build(screen, new GameSession());   // second build must not duplicate
        var layout = grid.GetComponent<MapJourneyPathLayout>();
        Assert.That(grid.Cast<Transform>().Count(child => child.name.StartsWith("PathTrack")), Is.EqualTo(2));
        int segments = grid.Find("PathTrack1").childCount;
        Assert.That(segments, Is.GreaterThan(30));
        Vector2 first = layout.StopCenter(0), second = layout.StopCenter(1), third = layout.StopCenter(2);
        Assert.That(second.y, Is.GreaterThan(first.y), "Middle stop sits higher than the outer stops.");
        Assert.That(third.y, Is.EqualTo(first.y).Within(.5f));
        Assert.That(first.x, Is.LessThan(second.x));
        Assert.That(second.x, Is.LessThan(third.x));
        Image firstOutline = grid.Find("PathTrack1/Outline1").GetComponent<Image>();
        Assert.That(Vector2.Distance(firstOutline.rectTransform.anchoredPosition, first),
            Is.LessThan(Style().stopBadgeSize), "Road starts under the first badge.");
    }
    finally { Object.DestroyImmediate(root); }
}

[Test]
public void JourneyStops_StayInsideMapZoneAboveTheLessonPanel(
    [Values(1280f, 1440f, 1728f, 1920f, 2400f)] float width)
{
    var root = new GameObject("zone", typeof(RectTransform));
    try
    {
        var (screen, grid) = BuildMapAt(root, new Vector2(width, 1080f), new GameSession());
        var panel = (RectTransform)screen.LessonList.transform;
        Rect gridRect = WorldRect(grid), panelRect = WorldRect(panel);
        Assert.That(gridRect.Overlaps(panelRect), Is.False);
        foreach (MapNodeView node in screen.Nodes)
        {
            Rect stop = WorldRect((RectTransform)node.transform);
            Assert.That(stop.xMin, Is.GreaterThanOrEqualTo(gridRect.xMin - 1f), node.name);
            Assert.That(stop.xMax, Is.LessThanOrEqualTo(gridRect.xMax + 1f), node.name);
            Assert.That(stop.yMin, Is.GreaterThanOrEqualTo(gridRect.yMin - 1f), node.name);
            Assert.That(stop.yMax, Is.LessThanOrEqualTo(gridRect.yMax + 1f), node.name);
            Assert.That(stop.Overlaps(panelRect), Is.False, node.name);
        }
    }
    finally { Object.DestroyImmediate(root); }
}

[Test]
public void JourneyRoad_FillsOnlyThePassedSegments()
{
    var root = new GameObject("progress", typeof(RectTransform));
    try
    {
        var (_, grid) = BuildMapAt(root, new Vector2(1920f, 1080f), new GameSession());
        Assert.That(grid.Find("PathTrack1/Fill1").gameObject.activeSelf, Is.False,
            "Fresh game: no gold segment, and no stray dot at the start.");
        Assert.That(grid.Find("PathTrack1/Dot1").gameObject.activeSelf, Is.True);

        root = new GameObject("progress-done", typeof(RectTransform));
        var (_, doneGrid) = BuildMapAt(root, new Vector2(1920f, 1080f), CompleteSprintJourney());
        Assert.That(doneGrid.Find("PathTrack1/Fill1").gameObject.activeSelf, Is.True);
        Assert.That(doneGrid.Find("PathTrack1/Dot1").gameObject.activeSelf, Is.False);
        Assert.That(doneGrid.Find("PathTrack2/Fill1").gameObject.activeSelf, Is.False);
    }
    finally { foreach (var go in Object.FindObjectsByType<MapScreen>(FindObjectsSortMode.None)) Object.DestroyImmediate(go.gameObject); Object.DestroyImmediate(root); }
}

static UITheme.LessonJourneyStyle Style() => UITheme.Shared.LessonJourney;
static Rect WorldRect(RectTransform rect)
{
    var corners = new Vector3[4];
    rect.GetWorldCorners(corners);
    return new Rect(corners[0], corners[2] - corners[0]);
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `tools/run-unity-tests.sh EditMode JourneyRoad_ road-red` and `... JourneyStops_ stops-zone-red`
Expected: failures (`StopCenter`/`Refresh` missing → compile error).

- [ ] **Step 3: Rewrite `MapJourneyPathLayout.cs`:**

```csharp
using System.Collections.Generic;
using System.Linq;
using KMA.Gameplay;
using KMA.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    // Places the three stops on fixed fractions of the map zone and draws the
    // road between them as pooled rotated rectangles sampled from cubic Béziers.
    [DisallowMultipleComponent]
    public sealed class MapJourneyPathLayout : MonoBehaviour
    {
        const int SegmentsPerLeg = 36;
        const int DotsPerLeg = 20;

        sealed class Leg
        {
            public RectTransform Root;
            public readonly List<RectTransform> Outline = new List<RectTransform>();
            public readonly List<RectTransform> Fill = new List<RectTransform>();
            public readonly List<RectTransform> Dots = new List<RectTransform>();
        }

        readonly List<MapNodeView> nodes = new List<MapNodeView>(3);
        readonly List<Leg> legs = new List<Leg>(2);
        readonly Vector2[] centers = new Vector2[3];
        RectTransform root;
        Vector2 lastSize;

        static UITheme.LessonJourneyStyle Style => UITheme.Shared.LessonJourney;

        public void Configure(IEnumerable<MapNodeView> mapNodes)
        {
            root = (RectTransform)transform;
            nodes.Clear();
            if (mapNodes != null)
                nodes.AddRange(mapNodes.Where(node => node != null && !node.IsComingSoon)
                    .OrderBy(node => CourseOrder(node.SubjectId)));
            while (legs.Count < Mathf.Max(0, nodes.Count - 1)) legs.Add(CreateLeg(legs.Count));
            Refresh();
        }

        public void Refresh() => LayoutPath(true);

        public Vector2 StopCenter(int index) => centers[Mathf.Clamp(index, 0, centers.Length - 1)];

        void LateUpdate()
        {
            if (root == null) root = (RectTransform)transform;
            if (nodes.Count == 0) return;
            if (root.rect.size != lastSize) LayoutPath(false);
            UpdateProgress();
        }

        void LayoutPath(bool force)
        {
            if (root == null || root.rect.width <= 1f || root.rect.height <= 1f) return;
            Vector2 size = root.rect.size;
            if (!force && size == lastSize) return;
            lastSize = size;

            UITheme.LessonJourneyStyle style = Style;
            // Shrink the whole stop if the zone is shorter than the stop's fixed height.
            float stopScale = Mathf.Min(1f, size.y / (style.stopSize.y + 20f));
            float badgeFromTop = style.stopTagHeight + style.stopBadgeSize * style.stopCurrentScale * .5f;
            float minCenter = (style.stopSize.y - badgeFromTop) * stopScale;
            float maxCenter = size.y - badgeFromTop * stopScale;
            int count = Mathf.Min(nodes.Count, 3);
            for (int i = 0; i < count; i++)
            {
                float cy = Mathf.Clamp(style.stopY[i] * size.y, minCenter, Mathf.Max(minCenter, maxCenter));
                centers[i] = new Vector2((style.stopX[i] - .5f) * size.x, cy - size.y * .5f);
                RectTransform stop = (RectTransform)nodes[i].transform;
                stop.anchorMin = stop.anchorMax = new Vector2(.5f, .5f);
                stop.pivot = new Vector2(.5f, 1f);
                stop.sizeDelta = style.stopSize;
                stop.localScale = Vector3.one * stopScale;
                stop.anchoredPosition = new Vector2(centers[i].x, centers[i].y + badgeFromTop * stopScale);
            }
            for (int i = 0; i < Mathf.Min(legs.Count, count - 1); i++)
                PositionLeg(legs[i], centers[i], centers[i + 1]);
        }

        Leg CreateLeg(int index)
        {
            string name = $"PathTrack{index + 1}";
            Transform existing = transform.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            if (existing == null) go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            go.transform.SetAsFirstSibling();
            var leg = new Leg { Root = rect };
            if (existing != null)
            {
                foreach (Transform child in rect)
                {
                    var childRect = (RectTransform)child;
                    if (child.name.StartsWith("Outline")) leg.Outline.Add(childRect);
                    else if (child.name.StartsWith("Fill")) leg.Fill.Add(childRect);
                    else if (child.name.StartsWith("Dot")) leg.Dots.Add(childRect);
                }
                return leg;
            }
            for (int i = 0; i < SegmentsPerLeg; i++)
                leg.Outline.Add(Piece(rect, "Outline" + (i + 1), HomeMenuStyle.Navy, null));
            for (int i = 0; i < SegmentsPerLeg; i++)
                leg.Fill.Add(Piece(rect, "Fill" + (i + 1), HomeMenuStyle.Gold, null));
            Sprite circle = UiKitAssets.Load().Circle;
            for (int i = 0; i < DotsPerLeg; i++)
                leg.Dots.Add(Piece(rect, "Dot" + (i + 1), HomeMenuStyle.White, circle));
            return leg;
        }

        static RectTransform Piece(RectTransform parent, string name, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        void PositionLeg(Leg leg, Vector2 from, Vector2 to)
        {
            float dx = to.x - from.x;
            Vector2 c1 = from + new Vector2(dx * .40f, 0f);
            Vector2 c2 = to - new Vector2(dx * .45f, 0f);
            UITheme.LessonJourneyStyle style = Style;

            Vector2 previous = from;
            for (int i = 0; i < SegmentsPerLeg; i++)
            {
                Vector2 next = Bezier(from, c1, c2, to, (i + 1f) / SegmentsPerLeg);
                SetSegment(leg.Outline[i], previous, next, style.roadOutlineWidth);
                SetSegment(leg.Fill[i], previous, next, style.roadFillWidth);
                previous = next;
            }
            for (int i = 0; i < DotsPerLeg; i++)
            {
                RectTransform dot = leg.Dots[i];
                dot.anchoredPosition = Bezier(from, c1, c2, to, (i + .5f) / DotsPerLeg);
                dot.sizeDelta = Vector2.one * style.roadDotSize;
            }
        }

        static void SetSegment(RectTransform rect, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 delta = b - a;
            rect.anchoredPosition = (a + b) * .5f;
            // Overlap neighbours by half the thickness so the corners of a thick road stay closed.
            rect.sizeDelta = new Vector2(delta.magnitude + thickness * .5f, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
        }

        void UpdateProgress()
        {
            for (int i = 0; i < legs.Count && i + 1 < nodes.Count; i++)
            {
                bool passed = nodes[i].IsCompleted;
                foreach (RectTransform fill in legs[i].Fill) fill.gameObject.SetActive(passed);
                foreach (RectTransform dot in legs[i].Dots) dot.gameObject.SetActive(!passed);
            }
        }

        static int CourseOrder(SubjectId id) => id switch
        {
            SubjectId.Sprint => 0,
            SubjectId.Volleyball => 1,
            SubjectId.Football => 2,
            _ => int.MaxValue
        };
    }
}
```

`Refresh()` calls `UpdateProgress()` indirectly only via `LateUpdate` (not run in EditMode tests). Add `UpdateProgress();` as the last line of `LayoutPath` (inside the `if` that passes) so tests and the first frame are correct.

- [ ] **Step 4: Make `MapPresentationBuilder.ConfigureJourneyPath` anchor the grid to the map zone** — replace the `Anchor((RectTransform)selectionGrid, …courseAnchorMin, …courseAnchorMax)` call with `mapAnchorMin`/`mapAnchorMax`; in `Build`, the initial `Anchor(grid, new Vector2(0f, .37f), new Vector2(1f, .84f))` is overwritten by `ConfigureJourneyPath`, leave it.

- [ ] **Step 5: Run to verify it passes**

Run: `tools/run-unity-tests.sh EditMode "JourneyRoad_|JourneyStops_" road-green`
Expected: all pass. If `JourneyStops_StayInside…` fails at 1280 because the panel anchors still use the old tall panel (`panelAnchorMax y .56`), that is fixed in Task 5 step 3; in that case mark the failing cases with the result XML and continue — the Task 5 commit must make them green.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/UI/MapJourneyPathLayout.cs Assets/_Project/Scripts/UI/MapPresentationBuilder.cs Assets/Tests/EditMode/Presentation/UIComponentTests.cs
git commit -m "feat(map): draw the journey road as a curve through the stops"
```

---

### Task 4: Current and selected stop (`MapScreen`, `JourneyLessonList`)

**Files:**
- Modify: `Assets/_Project/Scripts/UI/MapScreen.cs`
- Modify: `Assets/_Project/Scripts/UI/JourneyLessonList.cs` (`SelectedSubject`, heading)
- Test: `Assets/Tests/EditMode/Presentation/UIComponentTests.cs`

**Interfaces:**
- Consumes: Task 1 `SetJourneyMarkers`, `IsLocked`, `IsCompleted`.
- Produces: `public SubjectId JourneyLessonList.SelectedSubject`; `MapScreen` applies markers at the end of `RefreshJourney` and in `SelectSubject`; rule: **current** = first node in course order that is not locked and not completed; none when all three are completed. **selected** = `LessonList.SelectedSubject`.

- [ ] **Step 1: Write the failing tests:**

```csharp
[Test]
public void MapMarkers_FreshGameMarksSprintCurrentAndSelected()
{
    var root = new GameObject("markers", typeof(RectTransform));
    try
    {
        var screen = root.AddComponent<MapScreen>();
        MapPresentationBuilder.Build(screen, new GameSession());
        Assert.That(screen.Nodes.Select(node => node.IsCurrent), Is.EqualTo(new[] { true, false, false }));
        Assert.That(screen.Nodes.Select(node => node.IsSelected), Is.EqualTo(new[] { true, false, false }));
        Assert.That(screen.Nodes[0].transform.Find("CurrentTag").gameObject.activeSelf, Is.True);
        Assert.That(screen.Nodes[1].transform.Find("CurrentTag").gameObject.activeSelf, Is.False);
    }
    finally { Object.DestroyImmediate(root); }
}

[Test]
public void MapMarkers_AfterSprintCourseTheCurrentStopMovesToVolleyballAndSelectionFollowsClicks()
{
    var root = new GameObject("markers-2", typeof(RectTransform));
    try
    {
        var screen = root.AddComponent<MapScreen>();
        MapPresentationBuilder.Build(screen, CompleteSprintJourney());
        MapNodeView sprint = screen.Nodes[0], volley = screen.Nodes[1], football = screen.Nodes[2];
        Assert.That(volley.IsCurrent, Is.True);
        Assert.That(sprint.IsCurrent, Is.False);
        Assert.That(football.IsLocked, Is.True);

        screen.SelectSubject(SubjectId.Sprint);
        Assert.That(sprint.IsSelected, Is.True);
        Assert.That(volley.IsSelected, Is.False);
        Assert.That(volley.IsCurrent, Is.True, "Selecting an older stop must not move the current marker.");

        screen.SelectSubject(SubjectId.Football);   // locked: selection must not change
        Assert.That(football.IsSelected, Is.False);
        Assert.That(sprint.IsSelected, Is.True);
    }
    finally { Object.DestroyImmediate(root); }
}

[Test]
public void MapMarkers_CompletedCourseHasNoCurrentStop()
{
    var root = new GameObject("markers-3", typeof(RectTransform));
    try
    {
        var session = new GameSession();
        foreach (ChallengeDefinition definition in session.Journey.Catalog.Ordered)
            Complete(session, definition.Id);
        var screen = root.AddComponent<MapScreen>();
        MapPresentationBuilder.Build(screen, session);
        Assert.That(screen.Nodes.Any(node => node.IsCurrent), Is.False);
        Assert.That(root.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == "CurrentTag").All(t => !t.gameObject.activeSelf), Is.True);
    }
    finally { Object.DestroyImmediate(root); }
}

[Test]
public void LessonPanelHeadingIsOnlyTheSubjectName()
{
    var root = new GameObject("heading", typeof(RectTransform));
    try
    {
        var screen = root.AddComponent<MapScreen>();
        MapPresentationBuilder.Build(screen, new GameSession());
        Assert.That(screen.LessonList.transform.Find("CourseTitle").GetComponent<TMP_Text>().text,
            Is.EqualTo("Chạy nước rút"));
        Assert.That(screen.LessonList.SelectedSubject, Is.EqualTo(SubjectId.Sprint));
    }
    finally { Object.DestroyImmediate(root); }
}
```

(`Complete(session, id)` exists at the bottom of `UIComponentTests`; reuse it. If it requires journey order, iterate `Catalog.Ordered` as above.)

- [ ] **Step 2: Run to verify it fails**

Run: `tools/run-unity-tests.sh EditMode "MapMarkers_|LessonPanelHeading" markers-red`
Expected: compile failure (`SelectedSubject` missing).

- [ ] **Step 3: `JourneyLessonList`** — add the property near `CurrentChallengeId`:

```csharp
        public SubjectId SelectedSubject => selectedSubject;
```

and change the heading line in `Refresh` to:

```csharp
                heading.text = VietText.Fix(CourseTitles[subjectIndex]);
```

- [ ] **Step 4: `MapScreen`** — at the end of `RefreshJourney` (after `LessonList?.Bind(...)`) add `ApplyMarkers();`, change `SelectSubject` to call it, and add the method:

```csharp
        public void SelectSubject(SubjectId subject)
        {
            LessonList?.ShowSubject(subject);
            ApplyMarkers();
            SubjectRequested?.Invoke(subject);
        }

        void ApplyMarkers()
        {
            MapNodeView current = Nodes
                .Where(node => node != null && !node.IsComingSoon && !node.IsLocked && !node.IsCompleted)
                .OrderBy(node => System.Array.IndexOf(CourseOrder, node.SubjectId))
                .FirstOrDefault();
            SubjectId selected = LessonList != null ? LessonList.SelectedSubject : SubjectId.Sprint;
            foreach (MapNodeView node in Nodes)
                if (node != null)
                    node.SetJourneyMarkers(node == current, node.SubjectId == selected && !node.IsLocked);
        }

        static readonly SubjectId[] CourseOrder = { SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football };
```

Add `using System.Linq;` to `MapScreen.cs`.

- [ ] **Step 5: Run to verify it passes**

Run: `tools/run-unity-tests.sh EditMode "MapMarkers_|LessonPanelHeading|MapStops_" markers-green`
Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/UI/MapScreen.cs Assets/_Project/Scripts/UI/JourneyLessonList.cs Assets/Tests/EditMode/Presentation/UIComponentTests.cs
git commit -m "feat(map): mark the current and selected stops and show the subject name as panel heading"
```

---

### Task 5: Compact header, shorter lesson panel, readable locked cards; update legacy tests

**Files:**
- Modify: `Assets/_Project/Scripts/UI/MapPresentationBuilder.cs` (`Header`, summary anchors)
- Modify: `Assets/_Project/Scripts/UI/JourneyLessonPresentation.cs`
- Modify: `Assets/_Project/Scripts/UI/JourneyLessonList.cs` (`ApplyState`)
- Modify: `Assets/_Project/Scripts/UI/UITheme.cs` (+ `Assets/_Project/Settings/UI/UITheme.asset`)
- Modify tests: `UIComponentTests.cs`, `S5NewGameTests.cs`, `FestivalUiExperienceTests.cs`

**Interfaces:**
- Consumes: Tasks 1–4.
- Produces: header height 84 (`Header` `LayoutElement` min/preferred), `BackButton` 60×60, title size 40, no `Subtitle`; panel anchors `(.02,.005)–(.98,.34)`; summary strip anchors `(.18,.855)–(.82,.915)` (just under the header, above the Volleyball stop).

- [ ] **Step 1: Update the YAML values** in `Assets/_Project/Settings/UI/UITheme.asset` (`lessonJourney:` block):

```yaml
    panelAnchorMin: {x: 0.02, y: 0.005}
    panelAnchorMax: {x: 0.98, y: 0.34}
    courseAnchorMin: {x: 0, y: 0.35}
    courseAnchorMax: {x: 1, y: 0.915}
    summaryAnchorMin: {x: 0.18, y: 0.855}
    summaryAnchorMax: {x: 0.82, y: 0.915}
    cardLeft: 0.025
    cardWidth: 0.28
    cardGap: 0.055
    cardBottom: 0.04
    cardTop: 0.66
    iconSize: 64
```

and mirror the same defaults in the `LessonJourneyStyle` initializers in `UITheme.cs` (`panelAnchorMin/Max`, `courseAnchorMin/Max`, `summaryAnchorMin/Max`, `cardBottom`, `cardTop`, `iconSize`). Add one new field after `captionSize`: `public float objectiveSize = 20f;` (new field → no YAML needed).

- [ ] **Step 2: Update the failing legacy tests first** (they currently encode the old design). In `UIComponentTests.cs`:

  - `MapHeaderMakesNavigationTitleAndLivesReadableAtLandscapeScale`: back `preferredWidth`/`preferredHeight` `>= 56f`; title `fontSize >= 36` and `<= 44`; delete the `subtitle` lines and add `Assert.That(header.Find("Heading/SubtitleContainer"), Is.Null);` and `Assert.That(title.text, Is.EqualTo("CHỌN MÔN THI"));`; the header height check goes in the Festival test below.
  - `LessonJourneyKeepsThreeReadableCardsInSequence`: change `height * .24f` to `height * .13f` (cards ≥ 140px at 1080) and the message to "Each stage needs room for icon, title, objective and action."
  - `MapCardsCommunicateCompletedAndLockedStatesWithoutColorAlone` → replace body with (keeps intent, new paths):

```csharp
var session = CompleteSprintJourney();
var screen = root.AddComponent<MapScreen>();
MapPresentationBuilder.Build(screen, session);
MapNodeView completed = screen.Nodes.Single(node => node.SubjectId == SubjectId.Sprint);
MapNodeView football = screen.Nodes.Single(node => node.SubjectId == SubjectId.Football);
Assert.That(completed.StatusText, Is.EqualTo("HOÀN THÀNH"));
Assert.That(completed.transform.Find("Badge/DoneMark").gameObject.activeSelf, Is.True);
Assert.That(football.StatusText, Is.EqualTo("CHƯA MỞ KHÓA"));
Assert.That(football.transform.Find("Badge/LockIcon").gameObject.activeSelf, Is.True);
Assert.That(completed.GetComponent<BrutalButton>(), Is.Not.Null);
Assert.That(football.GetComponent<Button>().interactable, Is.False);
var iconSprites = screen.Nodes.Select(node => node.transform.Find("Badge/IconGlyph")
    .GetComponent<Image>().sprite).ToArray();
Assert.That(iconSprites.Distinct().Count(), Is.EqualTo(3));
foreach (MapNodeView node in screen.Nodes)
    Assert.That(node.transform.Find("LabelGroup/TitleContainer/Title").GetComponent<TMP_Text>().fontSize,
        Is.GreaterThanOrEqualTo(30), node.name);
```

  - `FutureSubjectsAreLockedUntilPreviousExamPasses`: replace `future.transform.Find("StatusContainer/Status")…` with `Assert.That(future.StatusText, Is.EqualTo("CHƯA MỞ KHÓA"));`.
  - `MapCardsUseRoundedSurfacesAndControlledDepth` → rename `MapBadgesUseCircleSurfacesAndControlledDepth`: assert `node.transform.Find("Badge")` has `Image` with non-null sprite and `Shadow` with `|effectDistance.y| <= 8`.
  - `MapLayoutPreservesCardTitleLivesAndUpcomingLabelSpace`: replace the `cardHeader`/`cardTitle` block with `LayoutElement`-free checks: `Assert.That(sprint.Find("LabelGroup/TitleContainer").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(40f));` is not valid in EditMode (no layout) — use `offsetMin.y` instead: `Assert.That(-titleBox.offsetMin.y, Is.GreaterThanOrEqualTo(40f));` where `titleBox = (RectTransform)sprint.Find("LabelGroup/TitleContainer");`. Keep the lives-label and `Lesson1` `LayoutElement` assertions.
  - `AvailableSportsCardsRenderWithoutLockPictograms` → rename `LockPictogramsAppearOnlyOnLockedStops`: Sprint `Badge/LockIcon` inactive; Volleyball and Football active.
  - `MapSecondaryTextRemainsReadableOnCompactLandscapeScreens`: node checks become `sprint.Find("LabelGroup/MetaPill/StatusContainer/Status")` `fontSize >= 20`, `LabelGroup/TitleContainer/Title` `fontSize >= 30`; remove `Detail`/`ActionHint`; keep the lesson-panel `>= 16` loop.
  - `MapNodeAvailabilityTransitionsKeepInteractionAndVisualStateSynchronized`: `status` path → `node.transform.Find("LabelGroup/MetaPill/StatusContainer/Status")`; remove `action` and `DetailContainer`/`DetailText` assertions (the builder no longer binds a detail label); keep button/feedback/status text assertions (`status.text` `TẠM KHÓA` then `SẴN SÀNG`).
  - `MapPaletteUsesBlueForSubjectsGreenForCompletionAndGoldForReadyState`: `sprintAccent` from `sprint.transform.Find("Badge").GetComponent<Image>().color`; the completed outline check uses `sprint.transform.Find("Badge").GetComponent<Outline>()` is white now → replace with the done-mark colour check: `Assert.That(sprint.transform.Find("Badge/DoneMark").GetComponent<Image>().color, Is.EqualTo(HomeMenuStyle.Gold));`. Rename test `MapPaletteUsesBlueForSprintAndGoldForCompletion`.
  - `MapRebuildRecreatesDestroyedProceduralSportSprites`: both `Find("CardHeader/SportIcon/IconGlyph")` → `Find("Badge/IconGlyph")`.
  - `MapPresentation_UsesResponsiveGridAndStretchedTextLabels` (in `S5NewGameTests`): the `stripe` block and `glyph` path → `node.transform.Find("Badge")` has a sprite; `Badge/IconGlyph` has a sprite and `preserveAspect`; remove the `HeaderStripe`/`GetComponent<Image>().sprite` on root assertions; keep the label-anchors loop (our `LayoutLabel` labels are stretched in their containers; the `ActionHint` branch is now unused but harmless).
  - `MapScene_ContainsOnlyResponsiveSelectionPresentation` (`S5NewGameTests`): `DetailText == "CHƯA MỞ KHÓA"` → `StatusText == "CHƯA MỞ KHÓA"`.
  - `MapScene_ResponsiveLayout_KeepsCardsAndLessonsInsideTheirPanels` (`S5NewGameTests`): keep as is (stops must stay inside the grid, labels inside the stop; badge scaled 1.3 stays inside the stop root because the root is 400px tall and the badge area is 52 + 221px).
  - `MapUsesReadableLivesUniformCardsAndIntegratedProgress` (`FestivalUiExperienceTests`): header height `Is.InRange(76f, 92f)`; the "uniform size" assertion stays (all stops have `stopSize`); the `PathTrack` image count becomes `grid.Cast<Transform>().Count(child => child.name.StartsWith("PathTrack"))` `== 2`; delete the `SprintNode/ActionHint` assertion; label check uses `node.GetComponentsInChildren<TMP_Text>()` — keep.

- [ ] **Step 3: Run to confirm the updated tests fail for the right reasons**

Run: `tools/run-unity-tests.sh EditMode UIComponentTests ui-red`
Expected: header, lesson-height and layout tests FAIL (header still 116/56-pt title, panel still tall); stop tests pass.

- [ ] **Step 4: Compact header** — in `MapPresentationBuilder.Header` apply:
  - `headerElement.minHeight = 84; headerElement.preferredHeight = 84;` and in `Build`: `PinToTop((RectTransform)hearts.transform.parent.parent, 84f);`
  - `layout.padding = new RectOffset(14, 16, 8, 10); layout.spacing = 16;`
  - `HeaderButton`: `preferredWidth = 60f; preferredHeight = 60f;` and the `TextTmp(root, "Label", label, 56, …)` size → `40`.
  - Title: `LayoutLabel(heading, "Title", "CHỌN MÔN THI", 40, Color.white, TextAnchor.MiddleLeft)`, its container `LayoutElement.preferredHeight = 52`; **delete** the `subtitle` block (the `Subtitle` label and its `LayoutElement`).
  - Lives panel: `livesLayout.padding = new RectOffset(14, 14, 6, 6)`, `livesElement.preferredHeight = 60`, heart slot `LayoutElement` stay 32×30, `LivesLabel` size stays 24.
  - `Content` stretch offsets stay `(64, 40)`/`(-64, -40)` (a test asserts them).
  - The existing-scene branch of `Build` has no header code, so nothing else changes there.

- [ ] **Step 5: Compact lesson panel geometry** — in `JourneyLessonPresentation.Create`:

```csharp
            Image emblem = Shape(panel, "CourseIcon", Theme.LessonJourney.sprint, true);
            Place(emblem.rectTransform, new Vector2(.025f, .885f), new Vector2(52f, 52f), new Vector2(0f, .5f));
            // glyph children: unchanged

            TMP_Text heading = Label(panel, "CourseTitle", Style.headingSize, Theme.TextPrimary, FontStyles.Bold);
            Anchor(heading.rectTransform, new Vector2(.025f, .80f), new Vector2(.30f, .97f));
            heading.rectTransform.offsetMin = new Vector2(70f, 0f);
            TMP_Text progress = Label(panel, "CourseProgress", Style.bodySize, Theme.MapHint);
            Anchor(progress.rectTransform, new Vector2(.31f, .80f), new Vector2(.66f, .97f));

            Button continueButton = ActionButton(panel, "ContinueCheckpoint", "TIẾP TỤC BÀI HỌC  ›");
            Anchor((RectTransform)continueButton.transform, new Vector2(.69f, .81f), new Vector2(.975f, .96f));
```

(`ChapterAccent` stays at the top edge; `CreateCourtPattern` anchors unchanged.) Connectors: in `CreateConnector` replace the fixed `.425f/.437f` with the card mid-line `.36f/.37f` and `.365f` for the arrow `Place` (cards are now `.04–.66`, mid ≈ .35).

Card interior (`CreateCard`), replacing the vertical stack with the compact one:

```csharp
            TMP_Text number = Label(rect, "StepNumber", Style.captionSize, Theme.MutedForeground, FontStyles.Bold);
            number.text = $"0{index + 1}";
            Anchor(number.rectTransform, new Vector2(.05f, .83f), new Vector2(.22f, .97f));

            Image badge = Shape(rect, "StateBadge", Theme.Accent);
            Anchor(badge.rectTransform, new Vector2(.50f, .83f), new Vector2(.95f, .97f));
            // StateBadge/Label, CompletedMark, strokes: unchanged

            Image disc = Shape(rect, "StageIcon", Theme.Accent, true);
            Place(disc.rectTransform, new Vector2(.14f, .62f), Vector2.one * Style.iconSize);
            // Glow + Glyph: unchanged

            TMP_Text title = Label(rect, "StageTitle", Style.stageSize, Theme.Surface, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            Anchor(title.rectTransform, new Vector2(.28f, .50f), new Vector2(.95f, .78f));

            TMP_Text objective = Label(rect, "Objective", Style.objectiveSize, Theme.Surface);
            objective.alignment = TextAlignmentOptions.Top;
            objective.enableWordWrapping = true;
            Anchor(objective.rectTransform, new Vector2(.05f, .20f), new Vector2(.95f, .49f));

            Image action = Shape(rect, "ActionSurface", Theme.Muted);
            Anchor(action.rectTransform, new Vector2(.04f, .04f), new Vector2(.96f, .18f));
            TMP_Text status = Label(rect, "Status", Style.captionSize, Theme.Surface, FontStyles.Bold);
            status.alignment = TextAlignmentOptions.Center;
            Anchor(status.rectTransform, new Vector2(.045f, .04f), new Vector2(.955f, .18f));
```

`CreateCard` also sets `rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 320f;` — change to `150f` (a test asserts `>= 30`). The hint (`JourneyHint`) stays `.018–.085`? It would overlap the cards (`.04–.66` leaves `.66–.80` empty above, and the hint at the bottom overlaps `.04–.085`). Move the hint to the gap above the cards: `Anchor(hint.rectTransform, new Vector2(.025f, .68f), new Vector2(.975f, .78f))`.

- [ ] **Step 6: Readable locked cards** — in `JourneyLessonList.ApplyState` replace the background/outline/colour block:

```csharp
            card.Background.color = checkpoint ? theme.TextPrimary
                : complete ? theme.LessonJourney.completedSurface
                : unlocked ? theme.Card : theme.MapLockedCard;
            card.Outline.effectColor = checkpoint ? theme.Accent
                : complete ? theme.Success : theme.MapLockedBorder;
            ...
            card.Title.color = card.Objective.color = checkpoint || !unlocked && !complete
                ? theme.Surface : theme.TextPrimary;
```

Verify the numbers with the contrast test already in the file (`JourneyLessonLabelsMeetContrastOnTheirButtonSurface` uses Lesson1 = checkpoint) and add:

```csharp
[Test]
public void LockedLessonCardsStayReadable()
{
    var root = new GameObject("locked-cards", typeof(RectTransform));
    try
    {
        var screen = root.AddComponent<MapScreen>();
        MapPresentationBuilder.Build(screen, new GameSession());
        Transform locked = screen.LessonList.transform.Find("Lesson3");
        Color background = locked.GetComponent<Image>().color;
        Assert.That(ContrastRatio(locked.Find("Objective").GetComponent<TMP_Text>().color, background),
            Is.GreaterThanOrEqualTo(4.5f));
        Assert.That(ContrastRatio(locked.Find("StageTitle").GetComponent<TMP_Text>().color, background),
            Is.GreaterThanOrEqualTo(4.5f));
    }
    finally { Object.DestroyImmediate(root); }
}
```

If either ratio is under 4.5, darken `mapLockedCard` usage by choosing `theme.MapLockedBorder` for the background of locked cards instead and re-run; do not lower the threshold.

- [ ] **Step 7: Run to verify it passes**

Run: `tools/run-unity-tests.sh EditMode UIComponentTests ui-green` — expected: all pass.
Run: `tools/run-unity-tests.sh EditMode "JourneyRoad_|JourneyStops_|MapMarkers_|MapStops_" map-green` — expected: all pass (the zone test now sees the shorter panel).

- [ ] **Step 8: Commit**

```bash
git add Assets/_Project/Scripts/UI Assets/_Project/Settings/UI/UITheme.asset Assets/Tests
git commit -m "feat(map): compact header and lesson panel for the journey map"
```

(Re-check `git status` before this commit: `Assets/_Project/Scripts/UI` also contains the user's uncommitted `HomePresentationBuilder.cs` and `SplashPresentationView.cs`. Use explicit paths — `git add Assets/_Project/Scripts/UI/MapPresentationBuilder.cs Assets/_Project/Scripts/UI/JourneyLessonPresentation.cs Assets/_Project/Scripts/UI/JourneyLessonList.cs Assets/_Project/Scripts/UI/UITheme.cs …` — never the directory.)

---

### Task 6: Re-author `Map.unity`, PlayMode gates, screenshots, QA note

**Files:**
- Modify (regenerate): `Assets/_Project/Scenes/Map.unity`
- Modify: `Assets/Editor/ShellSceneAuthoring.cs` (`Validate`, Map block)
- Create: `docs/qa/journey-map-menu.md`, `docs/qa/images/journey-map-*.png`

**Interfaces:**
- Consumes: Tasks 1–5.
- Produces: authored `Map` scene containing the new presentation exactly once.

- [ ] **Step 1: Update `ShellSceneAuthoring.Validate` ("Map")** so it validates the new contract — replace the `Map must unlock only Sprint…` check text unchanged, and add after the node-count check:

```csharp
                Check(screen.Nodes.All(node => node.transform.Find("Badge") != null &&
                    node.transform.Find("LabelGroup/MetaPill/Stars") != null),
                    "Map stops must be the circular journey stops");
                Check(screen.Nodes[0].IsCurrent && !screen.Nodes[1].IsCurrent && !screen.Nodes[2].IsCurrent,
                    "A fresh game must mark only Sprint as the current stop");
                Check(grid.Find("PathTrack1") != null && grid.Find("PathTrack2") != null,
                    "Map road segments are missing");
```

- [ ] **Step 2: Re-author the scene** (Unity closed):

Run: `"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -nographics -projectPath "D:/project/the-chat-KMA" -executeMethod KMA.EditorTools.ShellSceneAuthoring.ApplyMap -quit -logFile Builds/author-map.log`
Expected: log ends with exit code 0; `git status` shows `Assets/_Project/Scenes/Map.unity` (and possibly `UITheme.asset`) modified. Then run `-executeMethod KMA.EditorTools.ShellSceneAuthoring.Validate -quit` and expect no `Check failed` lines in the log. If the log shows `Failed to load scene`/compile errors, fix those first.

Verify no duplicate UI: `grep -c "S5MapPresentation" Assets/_Project/Scenes/Map.unity` must equal the count before authoring (the object name appears in a fixed small number of places); and the PlayMode test below asserts exactly one `S5MapPresentation`.

- [ ] **Step 3: Add PlayMode gates** in `S5NewGameTests.cs` (after `MapScene_ResponsiveLayout_…`):

```csharp
[UnityTest]
public IEnumerator MapScene_JourneyMap_StopsClearTheHeaderAndLessonPanelAtEveryAspect()
{
    SceneManager.LoadScene("Map", LoadSceneMode.Single);
    yield return null;
    var screen = Object.FindFirstObjectByType<MapScreen>(FindObjectsInactive.Include);
    Assert.That(screen.transform.Cast<Transform>().Count(c => c.name == "S5MapPresentation"), Is.EqualTo(1));
    var header = (RectTransform)screen.transform.Find("S5MapPresentation/Content/Header");
    var panel = (RectTransform)screen.transform.Find("S5MapPresentation/Content/JourneyLessons");
    foreach (Vector2Int resolution in new[]
    {
        new Vector2Int(1280, 720), new Vector2Int(1440, 1080), new Vector2Int(1728, 1080),
        new Vector2Int(1920, 1080), new Vector2Int(2400, 1080)
    })
    {
        Screen.SetResolution(resolution.x, resolution.y, false);
        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(screen.transform as RectTransform);
        Canvas.ForceUpdateCanvases();
        foreach (MapNodeView node in screen.Nodes)
        {
            Rect stop = WorldBounds((RectTransform)node.transform);
            Assert.That(stop.Overlaps(WorldBounds(header)), Is.False,
                $"{node.SubjectId} overlaps the header at {resolution}.");
            Assert.That(stop.Overlaps(WorldBounds(panel)), Is.False,
                $"{node.SubjectId} overlaps the lesson panel at {resolution}.");
            foreach (TMP_Text label in node.GetComponentsInChildren<TMP_Text>(false))
                Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                    $"{node.SubjectId}/{label.name} truncated at {resolution}.");
        }
    }
}
```

(`WorldBounds` already exists in this test class.)

- [ ] **Step 4: Run the full gates**

Run: `tools/run-unity-tests.sh EditMode "" edit-full` — expected: 0 failed (the known 3 ignored `ChallengeSequenceTests` are fine; read individual case results in the XML).
Run: `tools/run-unity-tests.sh PlayMode "" play-full` — expected: 0 failed.
If a PlayMode label-truncation or overlap assertion fails, adjust only the anchors/sizes in `LessonJourneyStyle` / `JourneyLessonPresentation` named in the failure message, re-author (Step 2) and re-run; do not relax assertions.

- [ ] **Step 5: Screenshots** — invoke the `testing-unity-ui-with-screenshots` skill and capture the `Map` scene at 1920×1080 and 1440×1080 in four progress states (fresh; Sprint done; Sprint+Volleyball done; course complete) using its temporary in-memory probe (the probe must not be committed). Open every PNG and check: no clipped text, stars/status visible on all three stops, lock icons only on locked stops, current stop larger with the `ĐANG Ở ĐÂY` tag, road passes under the badges, panel clear of every stop, Football badge legible against the grass. Save the four 1920×1080 captures to `docs/qa/images/journey-map-{fresh,sprint-done,volleyball-done,complete}.png`.

- [ ] **Step 6: QA note** — create `docs/qa/journey-map-menu.md` with: what changed (one paragraph), the EditMode/PlayMode totals copied from the XML (passed/failed/ignored and XML paths), the four screenshots with their states and dimensions, and an explicit "Not verified" list: physical-device touch, Android safe areas/GPU, APK export, and that preview states with all three subjects open do not exist in the game.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scenes/Map.unity Assets/Editor/ShellSceneAuthoring.cs Assets/Tests/PlayMode/Progression/S5NewGameTests.cs docs/qa/journey-map-menu.md docs/qa/images/journey-map-fresh.png docs/qa/images/journey-map-sprint-done.png docs/qa/images/journey-map-volleyball-done.png docs/qa/images/journey-map-complete.png
git commit -m "feat(map): re-author the Map scene as a journey map and record QA evidence"
```

Run `git diff --check` and `git status --short` afterwards; the only remaining modified files must be the user's pre-existing unrelated ones.

---

## Self-Review

**Spec coverage**

| Spec section | Task |
| --- | --- |
| Header (compact, `CHỌN MÔN THI`, no subtitle, lives/back unchanged) | Task 5 step 4 |
| Road: curve through stops, behind badges, progress hidden at zero, flag | Task 3; finish flag on the Football badge in Task 2. |
| Stop: badge, number, ✓, icon, name, 3 stars, status pill, locked state | Task 1, Task 2 |
| Current stop larger/glow/tag; selected ring | Task 1 (`SetJourneyMarkers`, glow `Update`), Task 4. |
| Panel: lower/shorter, subject-name heading, shorter cards, locked readable, one accent, BẮT ĐẦU/CHƠI LẠI | Task 4 (heading), Task 5 (geometry/colours). `BẮT ĐẦU ›`/`ÔN LẠI ›`/`CHƠI LẠI ›` already exist as card actions in `JourneyLessonList.ApplyState`; no new button is created. |
| Code table incl. `MapScreen`, `JourneyLessonList`, `UITheme`, authoring + scene | Tasks 1–6 |
| Tests (geometry at 4 ratios, no cut text, stars+status on all, progress, current larger, no duplicate UI) | Tasks 2, 3, 4, 6 |

**Placeholder scan:** no TBD/TODO steps; the only conditional instructions ("if the ratio is under 4.5, use `MapLockedBorder`", "if a label truncation fails, adjust the named anchors") name the exact fallback and keep the assertion.

**Type consistency:** `BindJourneyStop(Image, Image[], GameObject, GameObject, GameObject, Image, RectTransform, RectTransform)` is defined in Task 1 and called with the same order in Task 2. `StopCenter`, `Refresh`, `PathTrack{n}/{Outline|Fill|Dot}{m}` are defined in Task 3 and used by Task 3/5/6 tests. `SelectedSubject` is defined in Task 4 and used only in Task 4. `IsCompleted/IsLocked/IsCurrent/IsSelected/StatusText` defined in Task 1 and used in Tasks 2–6.

**Known risk to watch (not a gap):** the geometry constants (`stopY`, panel `.34`, card bands) come from the preview and were not run in Unity while writing this plan. Task 3/5/6 tests and the Task 6 screenshots are the gate; tune only `LessonJourneyStyle` values and the named anchors.
