using System.Collections;
using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class StudentJourneyFlowTests
    {
        GameObject runtime;
        SceneRouter router;
        SaveData saved;
        int originalFrameRate, originalVsync;

        [UnityTest]
        public IEnumerator BootstrapToSummaryUsesAllNineRealControllersAndPersistsCompletion()
        {
            yield return StartRuntime();
            foreach (string id in new[] { "sprint_learn", "sprint_practice", "sprint_exam",
                         "volleyball_learn", "volleyball_practice", "volleyball_exam",
                         "soccer_learn", "soccer_practice", "soccer_exam" })
            {
                yield return PlayCheckpoint();
                Assert.That(router.Session.Journey.IsChallengeComplete(id), Is.True, id);
                if (id == "sprint_practice")
                    Assert.That(router.Session.Journey.IsSubjectUnlocked(SubjectId.Volleyball), Is.False);
            }
            Assert.That(router.Session.Journey.CourseComplete, Is.True);
            Assert.That(router.Session.Records.Values.Count(record => record.Passed), Is.EqualTo(3));
            var reloaded = new GameSession();
            reloaded.Restore(saved);
            Assert.That(reloaded.Journey.CourseComplete, Is.True);
            Assert.That(Object.FindFirstObjectByType<JourneyCourseSummary>().ScoreRows.Count, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator VolleyballFiveFailuresRequireFreshPracticeAndKeepSprintPassed()
        {
            yield return StartRuntime();
            for (int i = 0; i < 5; i++) yield return PlayCheckpoint();
            for (int i = 0; i < 5; i++)
            {
                yield return PlayCheckpoint(false);
                Assert.That(router.Session.Lives, Is.EqualTo(4 - i));
            }
            Assert.That(router.Session.Journey.AwaitingSupplementary, Is.True);
            Assert.That(router.Session.Journey.CheckpointChallengeId, Is.EqualTo("volleyball_practice"));
            Assert.That(router.Session.GetRecord(SubjectId.Sprint).Passed, Is.True);
            Assert.That(router.Session.Journey.IsSubjectUnlocked(SubjectId.Football), Is.False);
            yield return PlayCheckpoint();
            Assert.That(router.Session.Lives, Is.EqualTo(5));
            Assert.That(router.Session.Journey.SupplementaryRounds, Is.EqualTo(1));
            yield return PlayCheckpoint();
            Assert.That(router.Session.GetRecord(SubjectId.Volleyball).Passed, Is.True);
        }

        IEnumerator StartRuntime()
        {
            originalFrameRate = Application.targetFrameRate;
            originalVsync = QualitySettings.vSyncCount;
            foreach (var manager in Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None))
                Object.DestroyImmediate(manager.gameObject);
            foreach (var oldRouter in Object.FindObjectsByType<SceneRouter>(FindObjectsSortMode.None))
                Object.DestroyImmediate(oldRouter.gameObject);
            router = SceneRouter.EnsurePersistentInstance();
            runtime = new GameObject("StudentJourneyRuntime");
            runtime.SetActive(false);
            GameManager owner = runtime.AddComponent<GameManager>();
            owner.ConfigureStartup(() => SaveData.CreateDefault(), data =>
                saved = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data)), router, _ => { },
                configuredHasExistingSave: () => false);
            runtime.SetActive(true);
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            Assert.That(router.RouteToMenu(), Is.True);
            yield return JourneyRuntimeDriver.WaitForScene(router, "Menu");
            var menu = Object.FindFirstObjectByType<MainMenuScreen>();
            menu.NewGame();
            yield return JourneyRuntimeDriver.WaitForScene(router, "Map");
            Assert.That(ChallengeCatalog.LoadDefault().Ordered.Count, Is.EqualTo(9));
            Assert.That(JourneyDialogueLibrary.LoadDefault(), Is.Not.Null);
            Assert.That(SprintBalanceConfig.LoadDefault(), Is.Not.Null);
        }

        IEnumerator PlayCheckpoint(bool pass = true)
        {
            yield return JourneyRuntimeDriver.SkipDialogues();
            string id = router.Session.Journey.CheckpointChallengeId;
            ChallengeDefinition challenge = router.Session.Journey.Catalog.Get(id);
            ChallengeAttemptMode mode = router.Session.Journey.AwaitingSupplementary
                ? ChallengeAttemptMode.Supplementary : ChallengeAttemptMode.Journey;
            Assert.That(router.TryStartChallenge(id, mode, challenge.Difficulty), Is.True, id);
            yield return JourneyRuntimeDriver.WaitForScene(router, challenge.Subject switch
            {
                SubjectId.Sprint => "MG_Sprint", SubjectId.Volleyball => "MG_Volleyball", _ => "MG_Football"
            });
            if (challenge.Subject == SubjectId.Football)
            {
                Assert.That(router.Session.Journey.ActiveAttempt.Difficulty, Is.EqualTo(ChallengeDifficulty.Normal));
                var hud = Object.FindFirstObjectByType<FootballHud>();
                foreach (Transform item in hud.GetComponentsInChildren<Transform>(true))
                    if (item.name == "Easy" || item.name == "Normal" || item.name == "Hard" || item.name == "DifficultyTitle")
                        Assert.That(item.gameObject.activeSelf, Is.False, item.name);
            }
            yield return JourneyRuntimeDriver.CompleteCurrent(challenge, pass);
            ResultPanel panel = Object.FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include);
            Assert.That(panel.CurrentResult, Is.Not.Null, id);
            Assert.That(panel.CurrentResult.Pass, Is.EqualTo(pass), id);
            Assert.That(saved.journey.lastCommittedAttemptId, Is.Not.Null, "The receipt must persist before navigation.");
            // Returning to Map also covers quitting at a result without replaying its receipt.
            Assert.That(router.Route(SessionRoute.Map), Is.True);
            yield return JourneyRuntimeDriver.WaitForScene(router, "Map");
        }

        [UnityTearDown]
        public IEnumerator CleanupRuntime()
        {
            if (runtime != null)
            {
                Object.DestroyImmediate(runtime);
                if (router != null) Object.DestroyImmediate(router.gameObject);
                foreach (var splash in Object.FindObjectsByType<SplashScreenPresenter>(FindObjectsSortMode.None))
                    Object.Destroy(splash.gameObject);
                Application.targetFrameRate = originalFrameRate;
                QualitySettings.vSyncCount = originalVsync;
            }
            yield return null;
        }

        [Test]
        public void NewStudent_CanCompleteEveryChallengeAndUnlockAllSubjects()
        {
            var session = new GameSession();
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_learn"));

            foreach (ChallengeDefinition challenge in session.Journey.Catalog.Ordered)
            {
                Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo(challenge.Id));
                JourneyCommitOutcome outcome = JourneyGameplayDriver.CompleteActiveChallenge(session);
                Assert.That(outcome.Accepted, Is.True, challenge.Id);
                Assert.That(session.Journey.IsChallengeComplete(challenge.Id), Is.True, challenge.Id);
            }

            Assert.That(session.Journey.CourseComplete, Is.True);
            Assert.That(session.Journey.CheckpointChallengeId, Is.Null);
            Assert.That(session.Lives, Is.EqualTo(GameSession.MaxLives));
            Assert.That(session.GetRecord(SubjectId.Sprint).Passed, Is.True);
            Assert.That(session.GetRecord(SubjectId.Volleyball).Passed, Is.True);
            Assert.That(session.GetRecord(SubjectId.Football).Passed, Is.True);
        }

        [Test]
        public void FailedExam_CanBeRecoveredThroughSupplementaryPracticeAndRetry()
        {
            var session = new GameSession();
            JourneyCommitOutcome learn = JourneyGameplayDriver.CompleteActiveChallenge(session);
            JourneyCommitOutcome practice = JourneyGameplayDriver.CompleteActiveChallenge(session);
            Assert.That(learn.Accepted && practice.Accepted, Is.True);

            JourneyCommitOutcome failure = default;
            for (int attempt = 0; attempt < GameSession.MaxLives; attempt++)
            {
                failure = JourneyGameplayDriver.CompleteActiveChallenge(session, false);
                Assert.That(failure.Accepted, Is.True);
            }
            Assert.That(failure.AwaitingSupplementary, Is.True);
            Assert.That(session.Lives, Is.Zero);
            JourneyCommitOutcome recovery = JourneyGameplayDriver.CompleteActiveChallenge(session);
            Assert.That(recovery.Accepted, Is.True);
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_exam"));

            JourneyCommitOutcome retry = JourneyGameplayDriver.CompleteActiveChallenge(session);
            Assert.That(retry.Accepted, Is.True);
            Assert.That(session.Journey.IsChallengeComplete("sprint_exam"), Is.True);
            Assert.That(session.GetRecord(SubjectId.Sprint).Passed, Is.True);
        }
    }
}
