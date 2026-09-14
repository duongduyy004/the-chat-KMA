using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using KMA.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class BasketballCampaignTests
    {
        const string SceneName = "MG_Basketball";

        [TearDown]
        public void TearDown()
        {
            foreach (var router in Object.FindObjectsByType<SceneRouter>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(router.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator MapToBasketballRoute_LoadsTheProductionScene()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            Assert.That(router.StartSubject(SubjectId.Basketball), Is.True);
            yield return WaitForRoute(router, SceneName);

            Assert.That(Object.FindFirstObjectByType<BasketballController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PlaceholderMinigameController>(FindObjectsInactive.Include), Is.Null);
            Assert.That(router.Session.ActiveSubject, Is.EqualTo(SubjectId.Basketball));
            Assert.That(router.Session.ToSaveData().activeSubject, Is.EqualTo(SubjectId.Basketball));
        }

        [UnityTest]
        public IEnumerator BasketballPass_PreviewsMapThenContinuesAndPersistsTheRecord()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            Assert.That(router.StartSubject(SubjectId.Basketball), Is.True);
            yield return WaitForRoute(router, SceneName);

            var controller = Object.FindFirstObjectByType<BasketballController>();
            var panel = Object.FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include);
            Assert.That(panel, Is.Not.Null, "The Basketball scene must own a ResultPanel to continue a result.");

            yield return SkipTutorialAndReachPlay(controller);

            for (var basket = 0; basket < 5; basket++)
                yield return ScoreOneBasketThroughTheRouter(controller);

            Assert.That(controller.Baskets, Is.EqualTo(5));
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Resolve));
            Assert.That(panel.CurrentResult, Is.Not.Null);
            Assert.That(panel.CurrentResult.Pass, Is.True);
            Assert.That(panel.PreviewRoute, Is.EqualTo(SessionRoute.Map.ToString()));

            panel.Continue();
            yield return WaitForRoute(router, "Map");

            SubjectRecord record = router.Session.GetRecord(SubjectId.Basketball);
            Assert.That(record.Passed, Is.True);
            Assert.That(record.FailedVisits, Is.Zero);
            Assert.That(router.Session.ActiveSubject, Is.Null);
            Assert.That(RecordFor(router.Session.ToSaveData(), SubjectId.Basketball).passed, Is.True);
        }

        [UnityTest]
        public IEnumerator BasketballFailure_ReturnsToSubjectSelectAndSpendsALife()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            Assert.That(router.StartSubject(SubjectId.Basketball), Is.True);
            yield return WaitForRoute(router, SceneName);

            int livesBefore = router.Session.Lives;
            Assert.That(router.SubmitSubjectResult(SubjectId.Basketball,
                new MinigameResult(false, 0f, Rank.F)), Is.True);
            yield return WaitForRoute(router, "Map");

            Assert.That(router.Session.PendingPunishmentSubject, Is.Null);
            Assert.That(router.Session.ActiveSubject, Is.Null);
            SaveData saved = router.Session.ToSaveData();
            Assert.That(saved.awaitingPunishment, Is.False);
            Assert.That(saved.hasActiveSubject, Is.False);
            Assert.That(router.Session.Lives, Is.EqualTo(livesBefore - 1));
        }

        [Test]
        public void BasketballTutorialKeyParsesToItsSubjectId()
        {
            Assert.That(System.Enum.TryParse("Basketball", out SubjectId parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(SubjectId.Basketball));
        }

        // The player owns the tutorial gate, so the route reaches Play only after Skip. The
        // overlay binds on the frame after the scene load, so keep offering Skip while waiting.
        static IEnumerator SkipTutorialAndReachPlay(MinigameBase controller)
        {
            float deadline = Time.unscaledTime + 30f;
            while (controller.PresentationPhase != MinigamePhase.Play && Time.unscaledTime < deadline)
            {
                var overlay = Object.FindFirstObjectByType<TutorialOverlay>(FindObjectsInactive.Include);
                if (overlay != null && overlay.ShouldShow)
                    overlay.Skip();
                yield return null;
            }

            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play),
                "The route must reach Play after the tutorial is skipped.");
        }

        // Plays a basket the way the build does: one pointer gesture through the scene's own
        // router for the lob, then one pointer tap at the apex.
        static IEnumerator ScoreOneBasketThroughTheRouter(BasketballController controller)
        {
            int before = controller.Baskets;
            GameplayInputRouter router = controller.InputRouter;
            var start = new Vector2(600f, 400f);
            double charge = (controller.TargetChargeMin + controller.TargetChargeMax) * .5f;

            router.FeedPointerDownForTest(start, 0d);
            router.FeedPointerMoveForTest(start + new Vector2(180f, 0f), charge * .5d);
            router.FeedPointerUpForTest(start + new Vector2(240f, 0f), charge);

            float guard = Time.unscaledTime + 20f;
            while (!controller.Ball.Snapshot.IsInFlight && Time.unscaledTime < guard)
                yield return new WaitForFixedUpdate();
            Assert.That(controller.Ball.Snapshot.IsInFlight, Is.True, "The AI never launched the alley-oop.");

            while (controller.Ball.Body.linearVelocity.y > 0f && Time.unscaledTime < guard)
                yield return new WaitForFixedUpdate();

            router.FeedPointerDownForTest(start, 30d);
            router.FeedPointerUpForTest(start, 30.05d);

            Assert.That(controller.Baskets, Is.EqualTo(before + 1),
                "The routed gesture must score. Judge: " + controller.LastJudge);
            yield return new WaitForFixedUpdate();
        }

        static SubjectRecordData RecordFor(SaveData data, SubjectId subject)
        {
            foreach (SubjectRecordData record in data.subjects)
                if (record.id == subject)
                    return record;

            Assert.Fail("Save data is missing a record for " + subject);
            return null;
        }

        // A pending route keeps the router transitioning, so waiting on the scene name alone can
        // observe the outgoing scene when the destination name is already active.
        static IEnumerator WaitForRoute(SceneRouter router, string sceneName)
        {
            float deadline = Time.unscaledTime + 30f;
            while (Time.unscaledTime < deadline)
            {
                if (!router.IsTransitioning && SceneManager.GetActiveScene().name == sceneName)
                    break;
                yield return null;
            }

            Assert.That(router.IsTransitioning, Is.False, "The router is still transitioning.");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
            yield return null;
        }
    }
}
