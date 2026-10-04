using System.Reflection;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class JourneyRoutingTests
    {
        GameObject root;
        SceneRouter router;

        [SetUp]
        public void SetUp()
        {
            // These router unit tests own their result panel. Scene panels left
            // by preceding integration tests must not intercept its bindings.
            foreach (ResultPanel panel in Object.FindObjectsByType<ResultPanel>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(panel.gameObject);
            var existing = Object.FindFirstObjectByType<SceneRouter>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            router = SceneRouter.EnsurePersistentInstance();
            root = router.gameObject;
            router.LoadSession(new GameSession());
            router.ConfigureRouteAcceptanceForTests((transition, complete) =>
            {
                complete();
                return true;
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void FutureChallengeIsRejectedWithoutChangingSessionOrLoadingScene()
        {
            int routes = 0;
            router.TransitionStarted += _ => routes++;

            Assert.That(router.TryStartChallenge("volleyball_learn"), Is.False);

            Assert.That(router.Session.Journey.ActiveAttempt, Is.Null);
            Assert.That(router.Session.ActiveSubject, Is.Null);
            Assert.That(routes, Is.Zero);
        }

        [Test]
        public void RestartPreservesReviewOfAPreviousSubject()
        {
            for (int i = 0; i < 3; i++) JourneyGameplayDriver.CompleteActiveChallenge(router.Session);
            Assert.That(router.TryStartChallenge("sprint_learn", ChallengeAttemptMode.Review), Is.True);
            string previousId = router.Session.Journey.ActiveAttempt.AttemptId;
            Assert.That(router.RestartActiveSubject(), Is.True);
            ChallengeAttemptContext restarted = router.Session.Journey.ActiveAttempt;
            Assert.That(restarted.ChallengeId, Is.EqualTo("sprint_learn"));
            Assert.That(restarted.Mode, Is.EqualTo(ChallengeAttemptMode.Review));
            Assert.That(restarted.AttemptId, Is.Not.EqualTo(previousId));
            Assert.That(router.Session.Journey.CheckpointChallengeId, Is.EqualTo("volleyball_learn"));
            Assert.That(router.Session.Lives, Is.EqualTo(5));
        }

        [TestCase(1, ChallengeAttemptMode.Review)]
        [TestCase(9, ChallengeAttemptMode.FreePlay)]
        public void RestartPreservesChallengeAndModeAwayFromCheckpoint(int completed, ChallengeAttemptMode mode)
        {
            for (int i = 0; i < completed; i++) JourneyGameplayDriver.CompleteActiveChallenge(router.Session);
            string checkpoint = router.Session.Journey.CheckpointChallengeId;
            Assert.That(router.TryStartChallenge("sprint_learn", mode), Is.True);
            Assert.That(router.RestartActiveSubject(), Is.True);
            Assert.That(router.Session.Journey.ActiveAttempt.ChallengeId, Is.EqualTo("sprint_learn"));
            Assert.That(router.Session.Journey.ActiveAttempt.Mode, Is.EqualTo(mode));
            Assert.That(router.Session.Journey.CheckpointChallengeId, Is.EqualTo(checkpoint));
            Assert.That(router.Session.Lives, Is.EqualTo(5));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedRestartKeepsOriginalAttemptInMemoryAndSave(bool rejectRoute)
        {
            SaveData persisted = null;
            bool allowSave = true;
            router.ConfigureJourneyPersistence((out string error) =>
            {
                error = allowSave ? null : "Save unavailable";
                if (allowSave) persisted = router.Session.ToSaveData();
                return allowSave;
            });
            Assert.That(router.TryStartChallenge("sprint_learn"), Is.True);
            string original = router.Session.Journey.ActiveAttempt.AttemptId;
            if (rejectRoute)
                router.ConfigureRouteAcceptanceForTests((_, _) => false);
            else allowSave = false;
            Assert.That(router.RestartActiveSubject(), Is.False);
            Assert.That(router.Session.Journey.ActiveAttempt.AttemptId, Is.EqualTo(original));
            var reload = new GameSession();
            reload.RestoreSnapshot(persisted);
            Assert.That(reload.Journey.ActiveAttempt.AttemptId, Is.EqualTo(original));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedStartFromResultCanBeRetriedAfterPersistenceRecovers(bool supplementary)
        {
            for (int i = 0; i < 2; i++) JourneyGameplayDriver.CompleteActiveChallenge(router.Session);
            if (supplementary)
                for (int i = 0; i < 4; i++) JourneyGameplayDriver.CompleteActiveChallenge(router.Session, false);
            bool allowSave = true;
            router.ConfigureJourneyPersistence((out string error) =>
            {
                error = allowSave ? null : "Save unavailable";
                return allowSave;
            });
            Assert.That(router.TryStartChallenge("sprint_exam"), Is.True);
            ChallengeAttemptContext context = router.Session.Journey.ActiveAttempt;
            ResultPanel panel = CreateResultPanel();
            var result = new ChallengeAttemptResult(context, false, new ChallengeMetrics(),
                new MinigameResult(false, 0f, Rank.F));
            typeof(SceneRouter).GetMethod("PreviewChallengeResult", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(router, new object[] { context, result });
            allowSave = false;
            if (supplementary) panel.Continue(); else panel.Retry();
            Assert.That(panel.HasContinued, Is.False, "A failed start must leave the result usable.");
            Assert.That(panel.ContinueInteractable, Is.True);
            Assert.That(router.Session.Journey.ActiveAttempt, Is.Null);
            Assert.That(router.Session.Lives, Is.EqualTo(supplementary ? 0 : 4));
            string detail = panel.transform.Find("Detail").GetComponent<TMP_Text>().text;
            Assert.That(detail, Is.Not.Null, "The fixture panel must receive the routing error.");
            Assert.That(detail, Does.Contain("Save unavailable"));
            allowSave = true;
            if (supplementary) panel.Continue(); else panel.Retry();
            Assert.That(router.Session.Journey.ActiveAttempt.ChallengeId,
                Is.EqualTo(supplementary ? "sprint_practice" : "sprint_exam"));
        }

        ResultPanel CreateResultPanel()
        {
            var go = new GameObject("JourneyTestResult", typeof(RectTransform));
            go.transform.SetParent(root.transform);
            var panel = go.AddComponent<ResultPanel>();
            TMP_Text Text(string name)
            {
                var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
                text.transform.SetParent(go.transform);
                return text;
            }
            Button Button(string name)
            {
                var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
                button.transform.SetParent(go.transform);
                return button;
            }
            panel.Configure(go, Text("Status"), Text("Detail"), Text("Score"), Text("Rank"),
                Text("Lives"), Button("Continue"), Button("Retry"), Text("Error"));
            return panel;
        }

        [Test]
        public void ChallengeStartPersistsActiveAttemptBeforeRouting()
        {
            bool persisted = false;
            router.ConfigureJourneyPersistence((out string error) =>
            {
                error = null;
                persisted = router.Session.Journey.ActiveAttempt != null;
                return persisted;
            });
            bool persistedAtRoute = false;
            router.TransitionStarted += _ => persistedAtRoute = persisted;

            Assert.That(router.TryStartChallenge("sprint_learn"), Is.True);

            Assert.That(persisted, Is.True);
            Assert.That(persistedAtRoute, Is.True);
            Assert.That(router.Session.Journey.ActiveAttempt.ChallengeId, Is.EqualTo("sprint_learn"));
        }
    }
}
