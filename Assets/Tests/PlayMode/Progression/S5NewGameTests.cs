using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.Shell;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class S5NewGameTests
    {
        readonly List<GameObject> spawned = new List<GameObject>();
        int originalTargetFrameRate;
        int originalVSyncCount;

        [SetUp]
        public void SetUp()
        {
            originalTargetFrameRate = Application.targetFrameRate;
            originalVSyncCount = QualitySettings.vSyncCount;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in spawned)
            {
                if (gameObject != null)
                    UnityEngine.Object.DestroyImmediate(gameObject);
            }

            spawned.Clear();
            DestroyAll<GameManager>();
            DestroyAll<SceneRouter>();
            Application.targetFrameRate = originalTargetFrameRate;
            QualitySettings.vSyncCount = originalVSyncCount;
        }

        [Test]
        public void ResetCampaign_ClearsRecordsAndRestoresFiveLives()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);
            session.SubmitResult(SubjectId.Sprint, new MinigameResult(true, 8f, Rank.A));

            session.ResetCampaign();

            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.GetRecord(SubjectId.Sprint).Passed, Is.False);
        }

        [Test]
        public void CalibrateScreen_ClampsOffsetToDeviceSafeRange()
        {
            var screen = new GameObject("CalibrateScreen").AddComponent<CalibrateScreen>();
            try
            {
                screen.SetOffset(999f);
                Assert.That(screen.RhythmOffsetMs, Is.EqualTo(500f));
                screen.SetOffset(-999f);
                Assert.That(screen.RhythmOffsetMs, Is.EqualTo(-500f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void NewGame_RequiresExplicitConfirmation()
        {
            var screen = new GameObject("MainMenuScreen").AddComponent<MainMenuScreen>();
            try
            {
                var calls = 0;
                screen.NewGameRequested += () => calls++;
                screen.NewGame();
                Assert.That(screen.IsConfirmingNewGame, Is.True);
                Assert.That(calls, Is.Zero);
                screen.ConfirmNewGame();
                Assert.That(calls, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(screen.gameObject); }
        }

        [Test]
        public void MapNode_ReportsReadyCompletedAndUnavailableStates()
        {
            var node = new GameObject("MapNode").AddComponent<MapNodeView>();
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var button = new GameObject("Button", typeof(RectTransform), typeof(Button))
                    .GetComponent<Button>();
                var title = new GameObject("Title", typeof(RectTransform), typeof(Text))
                    .GetComponent<Text>();
                var detail = new GameObject("Detail", typeof(RectTransform), typeof(Text))
                    .GetComponent<Text>();
                button.transform.SetParent(node.transform);
                title.transform.SetParent(node.transform);
                detail.transform.SetParent(node.transform);
                title.font = font;
                detail.font = font;
                node.Bind(button, title, detail);

                node.Configure(SubjectId.Sprint, "Chạy nước rút", false, null, 5);
                Assert.That(node.gameObject.activeSelf, Is.True);
                Assert.That(node.DetailText, Is.EqualTo("SẴN SÀNG"));
                Assert.That(node.IsInteractable, Is.True);

                var passed = new SubjectRecord();
                passed.Accept(new MinigameResult(true, 0f, Rank.A));
                node.Configure(SubjectId.Sprint, "Chạy nước rút", false, passed, 5);
                Assert.That(node.DetailText, Is.EqualTo("HẠNG A  ★ 3"));
                Assert.That(node.Stars, Is.EqualTo(3));

                node.Configure(SubjectId.Football, "Bóng đá", true, null, 5);
                Assert.That(node.gameObject.activeSelf, Is.True);
                Assert.That(node.DetailText, Is.EqualTo("ĐANG PHÁT TRIỂN"));
                Assert.That(node.IsInteractable, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(node.gameObject); }
        }

        [Test]
        public void MapPresentation_UsesResponsiveGridAndStretchedTextLabels()
        {
            var screenObject = new GameObject("MapScreen", typeof(RectTransform));
            var screen = screenObject.AddComponent<MapScreen>();
            try
            {
                screenObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
                MapPresentationBuilder.Build(screen, new GameSession());

                var content = screen.transform.Find("S5MapPresentation/Content")
                    .GetComponent<RectTransform>();
                Assert.That(content.offsetMin, Is.EqualTo(new Vector2(64f, 40f)));
                Assert.That(content.offsetMax, Is.EqualTo(new Vector2(-64f, -40f)));
                var grid = screen.transform.Find("S5MapPresentation/Content/SelectionGrid");
                var responsiveGrid = grid.GetComponent<ResponsiveGridLayout>();
                Assert.That(responsiveGrid, Is.Not.Null);
                var gridRect = grid.GetComponent<RectTransform>();
                gridRect.anchorMin = Vector2.zero;
                gridRect.anchorMax = Vector2.zero;
                gridRect.sizeDelta = new Vector2(1136f, 480f);
                responsiveGrid.Refresh();
                var gridLayout = grid.GetComponent<GridLayoutGroup>();
                Assert.That(gridLayout.cellSize.x, Is.Not.EqualTo(220f));
                Assert.That(grid.GetComponent<LayoutElement>().preferredHeight,
                    Is.GreaterThanOrEqualTo(gridLayout.cellSize.y * 2f + gridLayout.spacing.y));
                Assert.That(grid.transform.childCount, Is.EqualTo(3));
                Assert.That(grid.transform.GetChild(2).name, Is.EqualTo("FootballNode"));
                foreach (MapNodeView node in screen.Nodes)
                {
                    Assert.That(node.GetComponent<Image>().sprite, Is.Not.Null, node.name);
                    Transform stripe = node.transform.Find("HeaderStripe");
                    Assert.That(stripe, Is.Not.Null, node.name + " must have a subject-colored header stripe.");
                    Image glyph = node.transform.Find("CardHeader/SportIcon/IconGlyph").GetComponent<Image>();
                    Assert.That(glyph.sprite, Is.Not.Null, node.name);
                    Assert.That(glyph.preserveAspect, Is.True, node.name);
                }

                Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                foreach (var label in screen.GetComponentsInChildren<Text>())
                {
                    Assert.That(label.font, Is.SameAs(legacyFont), label.name);
                    if (label.name == "ActionHint")
                    {
                        Assert.That(label.rectTransform.anchorMin.y, Is.EqualTo(0f));
                        Assert.That(label.rectTransform.anchorMax.y, Is.EqualTo(0f));
                    }
                    else
                    {
                        Assert.That(label.rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
                        Assert.That(label.rectTransform.anchorMax, Is.EqualTo(Vector2.one));
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(screenObject); }
        }

        [Test]
        public void MapPresentation_FutureTagsAreInformationalAndRequestNoSubject()
        {
            var screenObject = new GameObject("MapScreen", typeof(RectTransform));
            var screen = screenObject.AddComponent<MapScreen>();
            try
            {
                var requested = new List<SubjectId>();
                screen.SubjectRequested += requested.Add;

                MapPresentationBuilder.Build(screen, new GameSession());

                Transform futureRow = screen.transform.Find("S5MapPresentation/Content/FutureRow");
                Assert.That(futureRow, Is.Not.Null);
                Assert.That(futureRow.GetComponentsInChildren<Text>(true).Select(text => text.text),
                    Is.EquivalentTo(new[] { "SẮP RA MẮT", "Hít đất" }));
                Assert.That(futureRow.GetComponentsInChildren<Button>(true), Is.Empty);
                Assert.That(futureRow.GetComponentsInChildren<MapNodeView>(true), Is.Empty);
                Assert.That(screen.Nodes, Has.Length.EqualTo(3));

                Assert.That(requested, Is.Empty,
                    "Presentation-only future chips must never request a campaign subject route.");
            }
            finally { UnityEngine.Object.DestroyImmediate(screenObject); }
        }

        [TestCase(false, 5)]
        [TestCase(true, 3)]
        public void MapPresentation_UsesSessionStateOrDefaultsForLives(
            bool hasSession, int expectedLives)
        {
            var screenObject = new GameObject("MapScreen", typeof(RectTransform));
            var screen = screenObject.AddComponent<MapScreen>();
            try
            {
                GameSession session = hasSession
                    ? CreateMapSession(expectedLives)
                    : null;

                MapPresentationBuilder.Build(screen, session);
                MapPresentationBuilder.Build(screen, session);

                Text livesLabel = screen.transform
                    .Find("S5MapPresentation/Content/Header/LivesPanel/LivesLabelContainer/LivesLabel")
                    .GetComponent<Text>();
                Assert.That(livesLabel.text, Is.EqualTo($"LƯỢT: {expectedLives}/5"));
                Assert.That(screen.Hearts.CurrentHearts, Is.EqualTo(expectedLives));
                Assert.That(screen.Hearts.GetComponentsInChildren<Image>(true), Has.Length.EqualTo(5));
                Assert.That(screen.transform.Cast<Transform>()
                    .Count(child => child.name == "S5MapPresentation"), Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(screenObject); }
        }

        [UnityTest]
        public IEnumerator MapScene_ContainsOnlyResponsiveSelectionPresentation()
        {
            SceneManager.LoadScene("Map", LoadSceneMode.Single);
            yield return null;

            Assert.That(Object.FindObjectsByType<MinigameHUD>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<PhaseOverlay>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<ResultPanel>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<PausePanel>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), Is.Empty);

            var screen = Object.FindFirstObjectByType<MapScreen>(FindObjectsInactive.Include);
            Assert.That(screen.transform.Find("S5MapPresentation"), Is.Not.Null);
            Assert.That(screen.Nodes.Where(node => node.IsInteractable).Select(node => node.SubjectId),
                Is.EquivalentTo(new[] { SubjectId.Sprint, SubjectId.Volleyball }));
            Assert.That(screen.Nodes.Where(node => !node.IsInteractable &&
                    node.DetailText == "ĐANG PHÁT TRIỂN").Select(node => node.SubjectId),
                Is.EquivalentTo(new[] { SubjectId.Football }));
            Assert.That(GameObject.Find("SelectionGrid").GetComponent<GridLayoutGroup>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator MapScene_ResponsiveLayout_KeepsCardsInsideTheGrid()
        {
            SceneManager.LoadScene("Map", LoadSceneMode.Single);
            yield return null;

            var screen = Object.FindFirstObjectByType<MapScreen>(FindObjectsInactive.Include);
            Assert.That(screen, Is.Not.Null);
            RectTransform grid = screen.transform.Find("S5MapPresentation/Content/SelectionGrid")
                .GetComponent<RectTransform>();
            RectTransform content = screen.transform.Find("S5MapPresentation/Content")
                .GetComponent<RectTransform>();
            Transform futureRowTransform = screen.transform.Find("S5MapPresentation/Content/FutureRow");
            Assert.That(futureRowTransform, Is.Not.Null);
            RectTransform futureRow = futureRowTransform.GetComponent<RectTransform>();

            foreach (Vector2Int resolution in new[]
            {
                new Vector2Int(1920, 1080),
                new Vector2Int(1280, 720),
                new Vector2Int(1440, 1080),
                new Vector2Int(1728, 1080),
                new Vector2Int(2400, 1080)
            })
            {
                Screen.SetResolution(resolution.x, resolution.y, false);
                yield return null;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(screen.transform as RectTransform);
                Canvas.ForceUpdateCanvases();

                Rect gridBounds = WorldBounds(grid);
                Rect futureBounds = WorldBounds(futureRow);
                Assert.That(gridBounds.Overlaps(futureBounds), Is.False,
                    $"SelectionGrid must not overlap FutureRow at {resolution.x}x{resolution.y}.");
                Assert.That(Contains(content, grid), Is.True);
                Assert.That(Contains(content, futureRow), Is.True);
                foreach (MapNodeView node in screen.Nodes)
                {
                    Assert.That(Contains(grid, node.transform as RectTransform), Is.True,
                        $"{node.SubjectId} must remain inside SelectionGrid at {resolution.x}x{resolution.y}.");
                    foreach (Text label in node.GetComponentsInChildren<Text>(true))
                    {
                        if (!label.gameObject.activeInHierarchy)
                            continue;
                        Assert.That(Contains(node.transform as RectTransform, label.rectTransform), Is.True,
                            $"{node.SubjectId}/{label.name} must remain inside its card at {resolution.x}x{resolution.y}.");
                        Assert.That(label.rectTransform.rect.height, Is.GreaterThanOrEqualTo(label.fontSize),
                            $"{node.SubjectId}/{label.name} must be tall enough to render at {resolution.x}x{resolution.y}.");
                        Assert.That(label.preferredHeight,
                            Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                            $"{node.SubjectId}/{label.name} must not be vertically truncated at {resolution.x}x{resolution.y}.");
                    }
                }

                foreach (Text tag in futureRow.GetComponentsInChildren<Text>(true))
                    Assert.That(Contains(futureRow, tag.rectTransform), Is.True,
                        $"{tag.name} must remain inside FutureRow at {resolution.x}x{resolution.y}.");
            }
        }

        [Test]
        public void Continue_WithoutAnExistingSave_IsDisabledAndRequestsNoRoute()
        {
            SceneRouter router = CreateRouter();
            CreateManager(router, SaveData.CreateDefault(), hasExistingSave: false);
            List<SceneRouteTransition> transitions = RecordTransitions(router);
            MainMenuScreen menu = CreateShellMenu();

            Assert.That(menu.CanContinue, Is.False);

            menu.Continue();

            Assert.That(transitions, Is.Empty);
            Assert.That(router.IsTransitioning, Is.False);
        }

        [UnityTest]
        public IEnumerator Continue_WithoutAnActiveAttempt_RequestsMapOnly()
        {
            yield return AssertContinueRequests(
                Arrange(session =>
                {
                    session.StartSubject(SubjectId.Sprint);
                    session.SubmitResult(SubjectId.Sprint, new MinigameResult(true, 8f, Rank.A));
                }),
                SessionRoute.Map, null, "Map");
        }

        [UnityTest]
        public IEnumerator Continue_DuringAttemptOne_RequestsTheSubjectOnly()
        {
            yield return AssertContinueRequests(
                Arrange(session => session.StartSubject(SubjectId.Sprint)),
                SessionRoute.Subject, SubjectId.Sprint, "MG_Sprint");
        }

        [UnityTest]
        public IEnumerator Continue_AfterALoss_RequestsMapOnly()
        {
            yield return AssertContinueRequests(
                Arrange(session =>
                {
                    session.StartSubject(SubjectId.Sprint);
                    session.SubmitResult(SubjectId.Sprint, new MinigameResult(false, 0f, Rank.F));
                }),
                SessionRoute.Map, null, "Map");
        }

        [UnityTest]
        public IEnumerator Continue_WithAnExistingUncompletedDefaultSave_IsEnabled()
        {
            SceneRouter router = CreateRouter();
            GameManager manager = CreateManager(router, SaveData.CreateDefault(), hasExistingSave: true);
            List<SceneRouteTransition> transitions = RecordTransitions(router);
            MainMenuScreen menu = CreateShellMenu();

            Assert.That(manager.HasSavedCampaign, Is.True);
            Assert.That(menu.CanContinue, Is.True);

            menu.Continue();

            Assert.That(transitions, Has.Count.EqualTo(1));
            Assert.That(transitions[0].Route, Is.EqualTo(SessionRoute.Map));
            yield return WaitForRoutedScene(router, "Map");
        }

        [Test]
        public void Continue_WithRestoredProgress_IsEnabled()
        {
            SaveData persisted = SaveData.CreateDefault();
            persisted.lives = 4;

            SceneRouter router = CreateRouter();
            GameManager manager = CreateManager(router, persisted, hasExistingSave: true);
            MainMenuScreen menu = CreateShellMenu();

            Assert.That(manager.HasSavedCampaign, Is.True);
            Assert.That(menu.CanContinue, Is.True);
        }

        [Test]
        public void StartSubject_WhileARestoredAttemptIsStillActive_IsRejectedWithoutThrowing()
        {
            SceneRouter router = CreateRouter();
            GameManager manager = CreateManager(
                router, Arrange(session => session.StartSubject(SubjectId.Sprint)), true);
            List<SceneRouteTransition> transitions = RecordTransitions(router);

            Assert.That(router.StartSubject(SubjectId.Football), Is.False);

            Assert.That(transitions, Is.Empty);
            Assert.That(manager.Session.ActiveSubject, Is.EqualTo(SubjectId.Sprint));
            Assert.That(manager.Session.VisitAttempt, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Play_AfterRestoringAnActiveAttempt_ClearsItSoTheMapStaysUsable()
        {
            SaveData persisted = Arrange(session => session.StartSubject(SubjectId.Sprint));

            SceneRouter router = CreateRouter();
            SaveData saved = null;
            GameManager manager = CreateManager(router, persisted, true, data => saved = data);
            List<SceneRouteTransition> transitions = RecordTransitions(router);
            MainMenuScreen menu = CreateShellMenu();

            Assert.That(manager.Session.ActiveSubject, Is.EqualTo(SubjectId.Sprint));

            menu.Play();

            Assert.That(transitions, Has.Count.EqualTo(1));
            Assert.That(transitions[0].Route, Is.EqualTo(SessionRoute.Map));
            Assert.That(manager.Session.ActiveSubject, Is.Null);
            Assert.That(manager.Session.AwaitingPunishment, Is.False);
            Assert.That(saved, Is.Not.Null, "Abandoning the stale attempt must be persisted.");
            Assert.That(saved.hasActiveSubject, Is.False);
            Assert.That(saved.awaitingPunishment, Is.False);
            Assert.That(saved.lives, Is.EqualTo(5));

            yield return WaitForRoutedScene(router, "Map");

            Assert.That(router.StartSubject(SubjectId.Football), Is.True);
            Assert.That(manager.Session.ActiveSubject, Is.EqualTo(SubjectId.Football));
            yield return WaitForRoutedScene(router, "MG_Football");
        }

        [UnityTest]
        public IEnumerator StartNewGame_ResetsTheCampaignThroughTheRouterButKeepsSettingsAndTutorialFlags()
        {
            SaveData persisted = SaveData.CreateDefault();
            persisted.lives = 2;
            persisted.subjects[0].passed = true;
            persisted.hasActiveSubject = true;
            persisted.activeSubject = SubjectId.Sprint;
            persisted.visitAttempt = 2;
            persisted.awaitingPunishment = true;
            persisted.tutorialSeen[1] = true;
            persisted.settings.musicVol = 0.3f;
            persisted.settings.vibration = false;

            SceneRouter router = CreateRouter();
            SaveData saved = null;
            GameManager manager = CreateManager(router, persisted, true, data => saved = data);
            var sessionChanges = 0;
            router.SessionChanged += () => sessionChanges++;
            List<SceneRouteTransition> transitions = RecordTransitions(router);

            manager.StartNewGame();

            Assert.That(sessionChanges, Is.EqualTo(1));
            Assert.That(transitions, Has.Count.EqualTo(1));
            Assert.That(transitions[0].Route, Is.EqualTo(SessionRoute.Map));
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved.lives, Is.EqualTo(5));
            Assert.That(saved.subjects[0].passed, Is.False);
            Assert.That(saved.hasActiveSubject, Is.False);
            Assert.That(saved.visitAttempt, Is.EqualTo(1));
            Assert.That(saved.awaitingPunishment, Is.False);
            Assert.That(saved.tutorialSeen[1], Is.True);
            Assert.That(saved.settings.musicVol, Is.EqualTo(0.3f));
            Assert.That(saved.settings.vibration, Is.False);
            Assert.That(manager.Session.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
            Assert.That(manager.HasSavedCampaign, Is.True,
                "A fresh saved campaign remains available for Continue until it is completed.");
            yield return WaitForRoutedScene(router, "Map");
        }

        IEnumerator AssertContinueRequests(SaveData persisted, SessionRoute expectedRoute,
            SubjectId? expectedSubject, string expectedScene)
        {
            SceneRouter router = CreateRouter();
            GameManager manager = CreateManager(router, persisted, true);
            List<SceneRouteTransition> transitions = RecordTransitions(router);
            MainMenuScreen menu = CreateShellMenu();

            Assert.That(menu.CanContinue, Is.True);
            SubjectId? activeBefore = manager.Session.ActiveSubject;
            int attemptBefore = manager.Session.VisitAttempt;
            bool awaitingBefore = manager.Session.AwaitingPunishment;
            int livesBefore = manager.Session.Lives;

            menu.Continue();

            Assert.That(transitions, Has.Count.EqualTo(1));
            Assert.That(transitions[0].Route, Is.EqualTo(expectedRoute));
            Assert.That(transitions[0].Subject, Is.EqualTo(expectedSubject));
            Assert.That(manager.Session.ActiveSubject, Is.EqualTo(activeBefore));
            Assert.That(manager.Session.VisitAttempt, Is.EqualTo(attemptBefore));
            Assert.That(manager.Session.AwaitingPunishment, Is.EqualTo(awaitingBefore));
            Assert.That(manager.Session.Lives, Is.EqualTo(livesBefore));

            yield return WaitForRoutedScene(router, expectedScene);
        }

        static SaveData Arrange(Action<GameSession> arrange)
        {
            var session = new GameSession();
            arrange(session);
            return session.ToSaveData();
        }

        static List<SceneRouteTransition> RecordTransitions(SceneRouter router)
        {
            var transitions = new List<SceneRouteTransition>();
            router.TransitionStarted += transitions.Add;
            return transitions;
        }

        SceneRouter CreateRouter() => Track(new GameObject("S5NewGameTests.Router"))
            .AddComponent<SceneRouter>();

        GameManager CreateManager(SceneRouter router, SaveData persisted, bool hasExistingSave,
            Action<SaveData> onSaved = null)
        {
            GameObject gameObject = Track(new GameObject("S5NewGameTests.Manager"));
            gameObject.SetActive(false);
            var manager = gameObject.AddComponent<GameManager>();
            manager.ConfigureStartup(
                () => persisted,
                data => onSaved?.Invoke(data),
                router,
                _ => { },
                null,
                () => hasExistingSave);
            gameObject.SetActive(true);
            return manager;
        }

        MainMenuScreen CreateShellMenu()
        {
            GameObject gameObject = Track(new GameObject("S5NewGameTests.Shell"));
            var menu = gameObject.AddComponent<MainMenuScreen>();
            gameObject.AddComponent<S5ShellSceneController>();
            return menu;
        }

        GameObject Track(GameObject gameObject)
        {
            spawned.Add(gameObject);
            return gameObject;
        }

        static void DestroyAll<T>() where T : Component
        {
            foreach (T component in UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(component.gameObject);
            }
        }

        static GameSession CreateMapSession(int lives)
        {
            var session = new GameSession();
            SaveData data = session.ToSaveData();
            data.lives = lives;
            session.Restore(data);
            return session;
        }

        static Rect WorldBounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        static bool Contains(RectTransform container, RectTransform child)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect bounds = container.rect;
            const float tolerance = 0.1f;
            foreach (Vector3 corner in corners)
            {
                Vector3 local = container.InverseTransformPoint(corner);
                if (local.x < bounds.xMin - tolerance || local.x > bounds.xMax + tolerance ||
                    local.y < bounds.yMin - tolerance || local.y > bounds.yMax + tolerance)
                    return false;
            }

            return true;
        }

        static IEnumerator WaitForRoutedScene(SceneRouter router, string sceneName)
        {
            while (SceneManager.GetActiveScene().name != sceneName || router.IsTransitioning)
                yield return null;
            yield return null;
        }
    }
}
