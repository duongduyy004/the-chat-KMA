using System;
using System.Collections;
using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.Shell;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
            Assert.That(session.BossUnlocked, Is.False);
            Assert.That(session.GetRecord(SubjectId.Sprint).Passed, Is.False);
        }

        [Test]
        public void MapScreen_OnlyRaisesBossRequestWhenUnlocked()
        {
            var screen = new GameObject("MapScreen").AddComponent<MapScreen>();
            try
            {
                var calls = 0;
                screen.BossRequested += () => calls++;
                screen.SelectBoss();
                screen.SetBossUnlocked(true);
                screen.SelectBoss();
                Assert.That(calls, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(screen.gameObject);
            }
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
        public void MapNode_UsesDerivedStarsAndComingSoonLock()
        {
            var node = new GameObject("MapNode").AddComponent<MapNodeView>();
            try
            {
                node.Configure(SubjectId.Sprint, "Sprint", false, null, 5);
                Assert.That(node.Stars, Is.Zero);
                node.Configure(SubjectId.Sprint, "Sprint", true, null, 5);
                Assert.That(node.IsComingSoon, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(node.gameObject); }
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
        public IEnumerator Continue_AwaitingPunishment_RequestsPunishmentOnly()
        {
            yield return AssertContinueRequests(
                Arrange(session =>
                {
                    session.StartSubject(SubjectId.Sprint);
                    session.SubmitResult(SubjectId.Sprint, new MinigameResult(false, 0f, Rank.F));
                }),
                SessionRoute.Punishment, SubjectId.Sprint, "Punishment");
        }

        [UnityTest]
        public IEnumerator Continue_DuringAttemptTwo_RequestsTheRetryOnly()
        {
            yield return AssertContinueRequests(
                Arrange(session =>
                {
                    session.StartSubject(SubjectId.Endurance);
                    session.SubmitResult(SubjectId.Endurance, new MinigameResult(false, 0f, Rank.F));
                    session.CompletePunishment();
                }),
                SessionRoute.RetrySubject, SubjectId.Endurance, "MG_Endurance");
        }

        [Test]
        public void StartNewGame_ResetsTheCampaignButKeepsSettingsAndTutorialFlags()
        {
            SaveData persisted = SaveData.CreateDefault();
            persisted.lives = 2;
            persisted.subjects[0].passed = true;
            persisted.hasActiveSubject = true;
            persisted.activeSubject = SubjectId.Sprint;
            persisted.visitAttempt = 2;
            persisted.awaitingPunishment = true;
            persisted.tutorialSeen[2] = true;
            persisted.settings.musicVol = 0.3f;
            persisted.settings.vibration = false;

            SceneRouter router = CreateRouter();
            SaveData saved = null;
            GameManager manager = CreateManager(router, persisted, true, data => saved = data);

            manager.StartNewGame();

            Assert.That(saved, Is.Not.Null);
            Assert.That(saved.lives, Is.EqualTo(5));
            Assert.That(saved.subjects[0].passed, Is.False);
            Assert.That(saved.hasActiveSubject, Is.False);
            Assert.That(saved.visitAttempt, Is.EqualTo(1));
            Assert.That(saved.awaitingPunishment, Is.False);
            Assert.That(saved.tutorialSeen[2], Is.True);
            Assert.That(saved.settings.musicVol, Is.EqualTo(0.3f));
            Assert.That(saved.settings.vibration, Is.False);
            Assert.That(manager.Session.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
            Assert.That(manager.HasSavedCampaign, Is.True);
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

        static IEnumerator WaitForRoutedScene(SceneRouter router, string sceneName)
        {
            while (SceneManager.GetActiveScene().name != sceneName || router.IsTransitioning)
                yield return null;
            yield return null;
        }
    }
}
