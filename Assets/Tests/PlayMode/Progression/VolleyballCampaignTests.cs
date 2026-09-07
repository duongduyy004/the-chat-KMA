using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class VolleyballCampaignTests
    {
        const string SceneName = "MG_Volleyball";

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
        public IEnumerator MapToVolleyballRoute_LoadsTheProductionScene()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            Assert.That(router.StartSubject(SubjectId.Volleyball), Is.True);
            yield return WaitForRoute(router, SceneName);

            Assert.That(Object.FindFirstObjectByType<VolleyballController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PlaceholderMinigameController>(FindObjectsInactive.Include), Is.Null);
            Assert.That(router.Session.ActiveSubject, Is.EqualTo(SubjectId.Volleyball));
            Assert.That(router.Session.ToSaveData().hasActiveSubject, Is.True);
            Assert.That(router.Session.ToSaveData().activeSubject, Is.EqualTo(SubjectId.Volleyball));
        }

        [UnityTest]
        public IEnumerator VolleyballPass_PreviewsMapThenContinuesAndPersistsTheRecord()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            Assert.That(router.StartSubject(SubjectId.Volleyball), Is.True);
            yield return WaitForRoute(router, SceneName);

            var controller = Object.FindFirstObjectByType<VolleyballController>();
            var panel = Object.FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include);
            Assert.That(controller, Is.Not.Null);
            Assert.That(panel, Is.Not.Null, "The Volleyball scene must own a ResultPanel to continue a result.");

            yield return SkipTutorialAndReachPlay(controller);

            for (var rally = 0; rally < 5; rally++)
                SubmitAuthoredRally(controller);
            yield return null;

            Assert.That(controller.PlayerScore, Is.EqualTo(5));
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Resolve));
            Assert.That(panel.CurrentResult, Is.Not.Null);
            Assert.That(panel.CurrentResult.Pass, Is.True);
            Assert.That(panel.PreviewRoute, Is.EqualTo(SessionRoute.Map.ToString()));

            panel.Continue();
            yield return WaitForRoute(router, "Map");

            SubjectRecord record = router.Session.GetRecord(SubjectId.Volleyball);
            Assert.That(record.Passed, Is.True);
            Assert.That(record.FailedVisits, Is.Zero);
            Assert.That(router.Session.ActiveSubject, Is.Null);
            SaveData saved = router.Session.ToSaveData();
            Assert.That(saved.hasActiveSubject, Is.False);
            Assert.That(RecordFor(saved, SubjectId.Volleyball).passed, Is.True);
        }

        [UnityTest]
        public IEnumerator VolleyballFirstFailure_RoutesToPunishmentAndKeepsTheAttemptActive()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            Assert.That(router.StartSubject(SubjectId.Volleyball), Is.True);
            yield return WaitForRoute(router, SceneName);

            Assert.That(router.SubmitSubjectResult(SubjectId.Volleyball,
                new MinigameResult(false, 0f, Rank.F)), Is.True);
            yield return WaitForRoute(router, "Punishment");

            Assert.That(router.Session.PendingPunishmentSubject, Is.EqualTo(SubjectId.Volleyball));
            Assert.That(router.Session.VisitAttempt, Is.EqualTo(2),
                "A first failure promotes the attempt to the final visit of the two-attempt rule.");
            SaveData saved = router.Session.ToSaveData();
            Assert.That(saved.hasActiveSubject, Is.True);
            Assert.That(saved.activeSubject, Is.EqualTo(SubjectId.Volleyball));
            Assert.That(saved.awaitingPunishment, Is.True);

            Assert.That(router.CompletePunishment(SubjectId.Volleyball), Is.True);
            yield return WaitForRoute(router, SceneName);

            Assert.That(router.Session.PendingPunishmentSubject, Is.Null);
            Assert.That(router.Session.VisitAttempt, Is.EqualTo(2),
                "Completing Punishment retries the same final attempt without spending a life.");
            Assert.That(router.Session.Lives, Is.EqualTo(GameSession.MaxLives));
        }

        // The player owns the tutorial gate, so the route reaches Play only after Skip. The
        // overlay binds on the frame after the scene load, so keep offering Skip while waiting.
        static IEnumerator SkipTutorialAndReachPlay(VolleyballController controller)
        {
            float deadline = Time.time + 30f;
            while (controller.PresentationPhase != MinigamePhase.Play && Time.time < deadline)
            {
                var overlay = Object.FindFirstObjectByType<TutorialOverlay>(FindObjectsInactive.Include);
                if (overlay != null && overlay.ShouldShow)
                    overlay.Skip();
                yield return null;
            }

            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play),
                "The Volleyball route must reach Play after the tutorial is skipped.");
        }

        static void SubmitAuthoredRally(VolleyballController controller)
        {
            SubmitTouch(controller, BallContext.Low, Vector2.down);
            SubmitTouch(controller, BallContext.Rising, Vector2.up);
            SubmitTouch(controller, BallContext.ApexNearNet, new Vector2(1f, -1f));
            controller.ResolveCourtContact(ownCourt: false);
        }

        static void SubmitTouch(VolleyballController controller, BallContext context, Vector2 swipe)
        {
            BallRig ball = controller.Ball;
            ball.AttachTo(null);
            Vector2 reachCentre = controller.ReachZone.bounds.center;
            ball.Body.position = context == BallContext.ApexNearNet
                ? new Vector2(0f, reachCentre.y + 1f)
                : new Vector2(0f, reachCentre.y);
            ball.Body.velocity = context switch
            {
                BallContext.Low => new Vector2(0f, -2f),
                BallContext.Rising => new Vector2(0f, 2f),
                BallContext.ApexNearNet => Vector2.zero,
                _ => throw new System.ArgumentOutOfRangeException(nameof(context), context, null)
            };
            controller.SubmitSwipe(swipe, inReachZone: true, timingAccuracy: 1f);
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
            float deadline = Time.time + 30f;
            while (Time.time < deadline)
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
