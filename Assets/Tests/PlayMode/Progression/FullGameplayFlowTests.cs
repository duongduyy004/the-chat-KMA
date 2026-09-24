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
    public sealed class FullGameplayFlowTests
    {
        readonly List<GameObject> gameObjects = new List<GameObject>();
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
            foreach (var gameObject in gameObjects)
            {
                if (gameObject != null)
                    UnityEngine.Object.DestroyImmediate(gameObject);
            }
            gameObjects.Clear();
            DestroyAll<GameManager>();
            DestroyAll<SceneRouter>();
            Application.targetFrameRate = originalTargetFrameRate;
            QualitySettings.vSyncCount = originalVSyncCount;
        }

        [UnityTest]
        public IEnumerator FullFlow_UsesAttemptsLivesAndNormalizedResults()
        {
            var harness = GameplayFlowHarness.Create();

            harness.Start(SubjectId.Sprint);
            harness.CompleteTransition();
            harness.Fail();
            Assert.That(harness.Route, Is.EqualTo(SessionRoute.Map));
            harness.CompleteTransition();

            harness.Start(SubjectId.Sprint);
            Assert.That(harness.Route, Is.EqualTo(SessionRoute.Subject));
            harness.CompleteTransition();

            harness.Fail();
            Assert.That(harness.Session.Lives, Is.EqualTo(3));
            Assert.That(harness.Route, Is.EqualTo(SessionRoute.Map));
            harness.CompleteTransition();

            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
            {
                harness.Start(id);
                harness.CompleteTransition();
                harness.Pass(6f);
                harness.CompleteTransition();
            }

            Assert.That(harness.Session.Records, Has.Count.EqualTo(3));
            foreach (var record in harness.Session.Records.Values)
            {
                Assert.That(record.BestResult.Pass, Is.True);
                Assert.That(record.BestResult.Score, Is.InRange(0f, 10f));
                Assert.That(record.BestResult.Rank, Is.EqualTo(ScoreUtil.ToRank(record.BestResult.Score)));
            }

            Assert.That(harness.Transitions, Has.Count.EqualTo(10));
            yield return null;
        }

        [Test]
        public void FailedVisit_DoesNotStoreNonNormalizedFailedScore()
        {
            var harness = GameplayFlowHarness.Create();

            harness.Start(SubjectId.Sprint);
            harness.CompleteTransition();
            harness.Fail(new MinigameResult(false, 10f, Rank.S));
            harness.CompleteTransition();
            harness.Start(SubjectId.Sprint);
            harness.CompleteTransition();
            harness.Fail(new MinigameResult(false, 10f, Rank.S));

            var record = harness.Session.GetRecord(SubjectId.Sprint);
            Assert.That(record.Passed, Is.False);
            Assert.That(record.BestScore, Is.Zero);
            Assert.That(record.BestRank, Is.EqualTo(Rank.F));
            Assert.That(record.FailedVisits, Is.EqualTo(2));
        }

        [Test]
        public void RepeatedCompletionWhileTransitionIsPending_RoutesExactlyOnce()
        {
            var harness = GameplayFlowHarness.Create();

            harness.Start(SubjectId.Badminton);
            harness.CompleteTransition();
            harness.Pass(6f);
            harness.RepeatLastCompletion();

            Assert.That(harness.Transitions, Has.Count.EqualTo(2));
            Assert.That(harness.Transitions[0].Route, Is.EqualTo(SessionRoute.Subject));
            Assert.That(harness.Transitions[1].Route, Is.EqualTo(SessionRoute.Map));
        }

        [Test]
        public void RuntimeRouter_MapsAllThreeProductionSubjects()
        {
            var router = SceneRouter.EnsurePersistentInstance();

            AssertRoute(router, SessionRoute.Map, null);
            AssertRoute(router, SessionRoute.Map, SubjectId.Sprint);
            AssertRoute(router, SessionRoute.GameOver, null);
            AssertRoute(router, SessionRoute.Subject, SubjectId.Sprint);
            foreach (var subject in new[]
            {
                SubjectId.Badminton,
                SubjectId.Football
            })
            {
                AssertRoute(router, SessionRoute.Subject, subject);
            }
        }

        [UnityTest]
        public IEnumerator RuntimeRouter_AutoBindsRealSubjectCompletionEvents()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            var mapRouteCount = 0;
            router.TransitionStarted += CountMapRoutes;

            Assert.That(router.StartSubject(SubjectId.Sprint), Is.True);
            yield return WaitForScene("MG_Sprint");

            var sprint = UnityEngine.Object.FindFirstObjectByType<SprintController>();
            Assert.That(sprint, Is.Not.Null);
            sprint.ConfigureForTest();
            sprint.AdvanceToDistance(100f);
            sprint.Simulate(0f);
            var resultPanel = UnityEngine.Object.FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include);
            Assert.That(resultPanel, Is.Not.Null);
            Assert.That(resultPanel.CurrentResult.Pass, Is.True);
            resultPanel.Continue();
            yield return WaitForScene("Map");

            Assert.That(router.Session.GetRecord(SubjectId.Sprint).Passed, Is.True);
            Assert.That(mapRouteCount, Is.EqualTo(1));

            yield return null;

            Assert.That(mapRouteCount, Is.EqualTo(1));
            router.TransitionStarted -= CountMapRoutes;

            void CountMapRoutes(SceneRouteTransition transition)
            {
                if (transition.Route == SessionRoute.Map)
                    mapRouteCount++;
            }
        }

        [UnityTest]
        public IEnumerator MutationsWhileTransitionIsPending_AreRejectedWithoutChangingSession()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            Assert.That(router.StartSubject(SubjectId.Sprint), Is.True);
            Assert.That(router.IsTransitioning, Is.True);

            GameSession session = router.Session;
            int livesBefore = session.Lives;
            var persistenceEvents = 0;
            router.SessionChanged += () => persistenceEvents++;

            Assert.That(router.StartSubject(SubjectId.Badminton), Is.False, "StartSubject must be rejected.");
            Assert.That(router.SubmitSubjectResult(SubjectId.Sprint, new MinigameResult(false, 0f, Rank.F)),
                Is.False, "SubmitSubjectResult must be rejected.");
            Assert.That(router.RestartActiveSubject(), Is.False, "Restart must be rejected.");
            Assert.That(router.ExitActiveSubjectToMap(), Is.False, "Exit must be rejected.");
            Assert.That(router.RouteToMenu(), Is.False, "Menu must be rejected.");
            Assert.That(router.ResumeCampaign(), Is.False, "Continue must be rejected.");

            Assert.That(session.ActiveSubject, Is.EqualTo(SubjectId.Sprint));
            Assert.That(session.PendingPunishmentSubject, Is.Null);
            Assert.That(session.Lives, Is.EqualTo(livesBefore));
            Assert.That(session.GetRecord(SubjectId.Sprint).FailedVisits, Is.Zero);
            Assert.That(session.GetRecord(SubjectId.Sprint).Passed, Is.False);
            Assert.That(persistenceEvents, Is.Zero, "A rejected route must not emit a persistence event.");

            yield return WaitForRoutedScene(router, "MG_Sprint");

            var sprint = UnityEngine.Object.FindFirstObjectByType<SprintController>();
            Assert.That(sprint, Is.Not.Null, "The original subject route must keep its pending binding.");
            sprint.ConfigureForTest();
            sprint.AdvanceToDistance(100f);
            sprint.Simulate(0f);
            var resultPanel = UnityEngine.Object.FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include);
            Assert.That(resultPanel.CurrentResult.Pass, Is.True,
                "The original subject controller must remain bound after rejected routes.");

            Assert.That(router.ExitActiveSubjectToMap(), Is.True, "Rejection must end with the transition.");
            Assert.That(persistenceEvents, Is.EqualTo(1));
            Assert.That(session.ActiveSubject, Is.Null);
            yield return WaitForRoutedScene(router, "Map");
        }

        [UnityTest]
        public IEnumerator Continue_AfterFirstFailure_ResumesAtSubjectSelectAcrossRelaunch()
        {
            SaveData persisted = SaveData.CreateDefault();
            SceneRouter router = SceneRouter.EnsurePersistentInstance();
            CreateManager(router, () => persisted, data => persisted = data);

            Assert.That(router.StartSubject(SubjectId.Sprint), Is.True);
            yield return WaitForRoutedScene(router, "MG_Sprint");
            Assert.That(router.SubmitSubjectResult(SubjectId.Sprint, new MinigameResult(false, 0f, Rank.F)),
                Is.True);
            yield return WaitForRoutedScene(router, "Map");

            Assert.That(persisted.hasActiveSubject, Is.False);
            Assert.That(persisted.visitAttempt, Is.EqualTo(1));
            Assert.That(persisted.awaitingPunishment, Is.False);
            Assert.That(persisted.lives, Is.EqualTo(4));

            DestroyAll<GameManager>();
            DestroyAll<SceneRouter>();
            yield return null;

            SceneRouter relaunched = SceneRouter.EnsurePersistentInstance();
            GameManager relaunchedManager = CreateManager(relaunched, () => persisted, data => persisted = data);
            Assert.That(relaunchedManager.Session.ResumeRoute(), Is.EqualTo(SessionRoute.Map));

            var transitions = new List<SceneRouteTransition>();
            relaunched.TransitionStarted += transitions.Add;
            MainMenuScreen menu = CreateGameObject("Relaunched shell").AddComponent<MainMenuScreen>();
            menu.gameObject.AddComponent<S5ShellSceneController>();

            Assert.That(menu.CanContinue, Is.True);
            menu.Continue();

            Assert.That(transitions, Has.Count.EqualTo(1));
            Assert.That(transitions[0].Route, Is.EqualTo(SessionRoute.Map));
            Assert.That(transitions[0].Subject, Is.Null);
            Assert.That(relaunched.Session.PendingPunishmentSubject, Is.Null);
            Assert.That(relaunched.Session.ActiveSubject, Is.Null);
            Assert.That(relaunched.Session.VisitAttempt, Is.EqualTo(1));
            Assert.That(relaunched.Session.Lives, Is.EqualTo(4));

            yield return WaitForRoutedScene(relaunched, "Map");
        }

        [UnityTest]
        public IEnumerator MutationsWhileMenuLoadIsPending_AreRejectedWithoutChangingTheSession()
        {
            SceneRouter router = SceneRouter.EnsurePersistentInstance();
            Assert.That(router.StartSubject(SubjectId.Sprint), Is.True);
            yield return WaitForRoutedScene(router, "MG_Sprint");

            GameSession session = router.Session;
            var persistenceEvents = 0;
            router.SessionChanged += () => persistenceEvents++;

            Assert.That(router.RouteToMenu(), Is.True);
            Assert.That(router.IsTransitioning, Is.True);
            Assert.That(router.StartSubject(SubjectId.Badminton), Is.False);
            Assert.That(router.SubmitSubjectResult(SubjectId.Sprint, new MinigameResult(false, 0f, Rank.F)),
                Is.False);
            Assert.That(router.RestartActiveSubject(), Is.False);
            Assert.That(router.ExitActiveSubjectToMap(), Is.False);
            Assert.That(router.ResumeCampaign(), Is.False);
            Assert.That(router.ResetCampaign(), Is.False);

            Assert.That(session.ActiveSubject, Is.EqualTo(SubjectId.Sprint));
            Assert.That(session.VisitAttempt, Is.EqualTo(1));
            Assert.That(session.AwaitingPunishment, Is.False);
            Assert.That(persistenceEvents, Is.Zero);

            yield return WaitForScene("Menu");
            Assert.That(router.IsTransitioning, Is.False);
        }

        GameManager CreateManager(SceneRouter router, Func<SaveData> load, Action<SaveData> save)
        {
            GameObject gameObject = CreateGameObject("FullGameplayFlowTests.Manager");
            gameObject.SetActive(false);
            var manager = gameObject.AddComponent<GameManager>();
            manager.ConfigureStartup(load, save, router, _ => { }, null, () => true);
            gameObject.SetActive(true);
            return manager;
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

        static void AssertRoute(SceneRouter router, SessionRoute route, SubjectId? subject)
        {
            Assert.That(router.TryGetSceneName(route, subject, out var sceneName), Is.True);
            Assert.That(Application.CanStreamedLevelBeLoaded(sceneName), Is.True,
                $"{route} must resolve to an enabled scene.");
        }

        static IEnumerator WaitForScene(string sceneName)
        {
            while (SceneManager.GetActiveScene().name != sceneName)
                yield return null;
            yield return null;
        }

        GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            gameObjects.Add(gameObject);
            return gameObject;
        }

        sealed class GameplayFlowHarness
        {
            readonly RecordingTransitionSink sink = new RecordingTransitionSink();
            SubjectId active;
            MinigameResult lastCompletion;

            GameplayFlowHarness()
            {
                Session = new GameSession();
                Router = new SessionRouteTransitioner(Session, sink);
            }

            public GameSession Session { get; }
            public SessionRouteTransitioner Router { get; }
            public SessionRoute Route { get; private set; }
            public IReadOnlyList<SceneRouteTransition> Transitions => sink.Transitions;

            public static GameplayFlowHarness Create() => new GameplayFlowHarness();

            public void Start(SubjectId id)
            {
                active = id;
                RouteSession(Session.StartSubject(id), id);
            }

            public void Pass(float score)
            {
                lastCompletion = new MinigameResult(true, score, ScoreUtil.ToRank(score));
                RouteSession(Session.SubmitResult(active, lastCompletion), active);
            }

            public void Fail() => Fail(new MinigameResult(false, 0f, Rank.F));

            public void Fail(MinigameResult result)
            {
                lastCompletion = result;
                RouteSession(Session.SubmitResult(active, result), active);
            }

            public void RepeatLastCompletion()
            {
                if (lastCompletion == null)
                    throw new InvalidOperationException("No completion is available to repeat.");

                Router.TryRoute(Route, active);
            }

            public void CompleteTransition() => sink.CompleteActiveTransition();

            void RouteSession(SessionRoute route, SubjectId? subject)
            {
                Route = route;
                Assert.That(Router.TryRoute(route, subject), Is.True);
            }
        }

        sealed class RecordingTransitionSink : ISceneRouteTransitionSink
        {
            readonly List<SceneRouteTransition> transitions = new List<SceneRouteTransition>();
            Action complete;

            public IReadOnlyList<SceneRouteTransition> Transitions => transitions;

            public bool Begin(SceneRouteTransition transition, Action onCompleted)
            {
                transitions.Add(transition);
                complete = onCompleted;
                return true;
            }

            public void CompleteActiveTransition()
            {
                var completed = complete;
                complete = null;
                completed?.Invoke();
            }
        }
    }
}
