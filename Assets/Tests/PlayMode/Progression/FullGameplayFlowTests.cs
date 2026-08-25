using System;
using System.Collections;
using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.Boss;
using KMA.Gameplay.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class FullGameplayFlowTests
    {
        readonly List<GameObject> gameObjects = new List<GameObject>();

        [SetUp]
        public void SetUp() => BossSceneSessionHandoff.ClearPendingSession();

        [TearDown]
        public void TearDown()
        {
            foreach (var gameObject in gameObjects)
                UnityEngine.Object.DestroyImmediate(gameObject);
            gameObjects.Clear();
            foreach (var router in UnityEngine.Object.FindObjectsByType<SceneRouter>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(router.gameObject);
            }
            BossSceneSessionHandoff.ClearPendingSession();
        }

        [UnityTest]
        public IEnumerator FullFlow_UsesAttemptsLivesNormalizedResultsAndBossUnlock()
        {
            var harness = GameplayFlowHarness.Create();

            harness.Start(SubjectId.Sprint);
            harness.CompleteTransition();
            harness.Fail();
            Assert.That(harness.Route, Is.EqualTo(SessionRoute.Punishment));
            harness.CompleteTransition();

            harness.CompletePunishment();
            Assert.That(harness.Route, Is.EqualTo(SessionRoute.RetrySubject));
            harness.CompleteTransition();

            harness.Fail();
            Assert.That(harness.Session.Lives, Is.EqualTo(4));
            Assert.That(harness.Route, Is.EqualTo(SessionRoute.Map));
            harness.CompleteTransition();

            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
            {
                harness.Start(id);
                harness.CompleteTransition();
                harness.Pass(6f);
                harness.CompleteTransition();
            }

            Assert.That(harness.Session.Records, Has.Count.EqualTo(7));
            Assert.That(harness.Session.BossUnlocked, Is.True);
            foreach (var record in harness.Session.Records.Values)
            {
                Assert.That(record.BestResult.Pass, Is.True);
                Assert.That(record.BestResult.Score, Is.InRange(0f, 10f));
                Assert.That(record.BestResult.Rank, Is.EqualTo(ScoreUtil.ToRank(record.BestResult.Score)));
            }

            harness.StartBoss();
            Assert.That(harness.Route, Is.EqualTo(SessionRoute.Boss));
            Assert.That(harness.Transitions[harness.Transitions.Count - 1].Session,
                Is.SameAs(harness.Session));
            var handoff = CreateGameObject("Boss session handoff").AddComponent<BossSceneSessionHandoff>();
            Assert.That(handoff.Session, Is.SameAs(harness.Session));
            harness.CompleteTransition();

            harness.CompleteBoss();
            Assert.That(harness.Route, Is.EqualTo(SessionRoute.Map));
            Assert.That(harness.Transitions, Has.Count.EqualTo(20));
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
            harness.CompletePunishment();
            harness.CompleteTransition();
            harness.Fail(new MinigameResult(false, 10f, Rank.S));

            var record = harness.Session.GetRecord(SubjectId.Sprint);
            Assert.That(record.Passed, Is.False);
            Assert.That(record.BestScore, Is.Zero);
            Assert.That(record.BestRank, Is.EqualTo(Rank.F));
            Assert.That(record.FailedVisits, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedCompletionWhileTransitionIsPending_RoutesExactlyOnce()
        {
            var harness = GameplayFlowHarness.Create();

            harness.Start(SubjectId.Endurance);
            harness.CompleteTransition();
            harness.Pass(6f);
            harness.RepeatLastCompletion();

            Assert.That(harness.Transitions, Has.Count.EqualTo(2));
            Assert.That(harness.Transitions[0].Route, Is.EqualTo(SessionRoute.Subject));
            Assert.That(harness.Transitions[1].Route, Is.EqualTo(SessionRoute.Map));
        }

        [Test]
        public void RuntimeRouter_MapsEnabledProductionRoutes_AndRejectsUnsupportedSubjects()
        {
            var router = SceneRouter.EnsurePersistentInstance();

            AssertRoute(router, SessionRoute.Map, null);
            AssertRoute(router, SessionRoute.Punishment, SubjectId.Sprint);
            AssertRoute(router, SessionRoute.GameOver, null);
            AssertRoute(router, SessionRoute.Boss, null);
            AssertRoute(router, SessionRoute.Subject, SubjectId.Sprint);
            AssertRoute(router, SessionRoute.RetrySubject, SubjectId.Sprint);
            AssertRoute(router, SessionRoute.Subject, SubjectId.Endurance);
            AssertRoute(router, SessionRoute.RetrySubject, SubjectId.Endurance);

            foreach (var subject in new[]
            {
                SubjectId.Volleyball,
                SubjectId.Basketball,
                SubjectId.PingPong,
                SubjectId.Badminton,
                SubjectId.Football
            })
            {
                Assert.That(router.TryGetSceneName(SessionRoute.Subject, subject, out _), Is.False);
                Assert.That(router.TryGetSceneName(SessionRoute.RetrySubject, subject, out _), Is.False);
                Assert.Throws<InvalidOperationException>(() => router.StartSubject(subject));
            }
        }

        [UnityTest]
        public IEnumerator RuntimeRouter_AutoBindsRealSubjectAndBossCompletionEvents()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            var mapRouteCount = 0;
            router.TransitionStarted += CountMapRoutes;

            Assert.That(router.StartSubject(SubjectId.Sprint), Is.True);
            yield return WaitForScene("MG_Sprint");

            var sprint = UnityEngine.Object.FindFirstObjectByType<SprintController>();
            Assert.That(sprint, Is.Not.Null);
            sprint.ConfigureForTest(0f);
            sprint.AdvanceToDistance(100f);
            sprint.Simulate(0f);
            yield return WaitForScene("Map");

            Assert.That(router.Session.GetRecord(SubjectId.Sprint).Passed, Is.True);
            Assert.That(mapRouteCount, Is.EqualTo(1));

            foreach (SubjectId subject in Enum.GetValues(typeof(SubjectId)))
            {
                if (subject == SubjectId.Sprint)
                    continue;
                router.Session.StartSubject(subject);
                router.Session.SubmitResult(subject, new MinigameResult(true, 6f, Rank.C));
            }

            Assert.That(router.Session.BossUnlocked, Is.True);
            Assert.That(router.StartBoss(), Is.True);
            yield return WaitForScene("MG_Boss");

            var boss = UnityEngine.Object.FindFirstObjectByType<BossPhaseController>();
            Assert.That(boss.Session, Is.SameAs(router.Session));
            var bossCompletionCount = 0;
            boss.Completed += _ => bossCompletionCount++;
            yield return new WaitForSeconds(5.1f);
            boss.Begin();
            for (var tap = 0; tap < 40; tap++)
                boss.TapMashDetector.SubmitTap();
            for (var hold = 0; hold < 16; hold++)
                boss.RhythmHoldDetector.SubmitHold(1f);
            for (var alternate = 0; alternate < 32; alternate++)
                boss.AlternateTapDetector.SubmitTap(alternate % 2 == 0 ? BossTapSide.Left : BossTapSide.Right);

            yield return WaitForScene("Map");
            yield return null;

            Assert.That(bossCompletionCount, Is.EqualTo(1));
            Assert.That(mapRouteCount, Is.EqualTo(2));
            router.TransitionStarted -= CountMapRoutes;

            void CountMapRoutes(SceneRouteTransition transition)
            {
                if (transition.Route == SessionRoute.Map)
                    mapRouteCount++;
            }
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

            public void CompletePunishment() => RouteSession(Session.CompletePunishment(), active);

            public void StartBoss()
            {
                if (!Session.BossUnlocked)
                    throw new InvalidOperationException("Boss is still locked.");
                RouteSession(SessionRoute.Boss, null);
            }

            public void CompleteBoss() => RouteSession(SessionRoute.Map, null);

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

            public void Begin(SceneRouteTransition transition, Action onCompleted)
            {
                transitions.Add(transition);
                complete = onCompleted;
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
