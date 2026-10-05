using System;
using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KMA.Tests.Presentation
{
    public sealed class JourneyMapTests
    {
        GameObject root;
        MapScreen screen;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("JourneyMap", typeof(RectTransform));
            screen = root.AddComponent<MapScreen>();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void NewJourneyShowsCourseOrderLocksFutureSubjectsAndListsCheckpointLessons()
        {
            MapPresentationBuilder.Build(screen, new GameSession());

            Assert.That(screen.Nodes[0].SubjectId, Is.EqualTo(SubjectId.Sprint));
            Assert.That(screen.Nodes[0].IsInteractable, Is.True);
            Assert.That(screen.Nodes[1].IsInteractable, Is.False);
            Assert.That(screen.Nodes[2].IsInteractable, Is.False);
            Assert.That(screen.LessonList.CurrentChallengeId, Is.EqualTo("sprint_learn"));
            Assert.That(screen.LessonList.LessonIds,
                Is.EqualTo(new[] { "sprint_learn", "sprint_practice", "sprint_exam" }));
            Assert.That(screen.BudgetLabel.text, Is.EqualTo("Lượt thi: 5/5"));
        }

        [Test]
        public void PassingSprintExamUnlocksVolleyballAndMovesCheckpointToItsFirstLesson()
        {
            var session = new GameSession();
            Play(session, "sprint_learn", true);
            Play(session, "sprint_practice", true);
            Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Volleyball), Is.False);
            Play(session, "sprint_exam", true);

            MapPresentationBuilder.Build(screen, session);
            screen.RefreshJourney(session);

            Assert.That(screen.Nodes[1].IsInteractable, Is.True);
            Assert.That(screen.Nodes[2].IsInteractable, Is.False);
            Assert.That(screen.LessonList.CurrentChallengeId, Is.EqualTo("volleyball_learn"));
        }

        [Test]
        public void ReviewDoesNotMoveCheckpointAndZeroLivesLocksTheCurrentExam()
        {
            var session = new GameSession();
            Play(session, "sprint_learn", true);
            Play(session, "sprint_practice", true);
            Play(session, "sprint_exam", true);
            MapPresentationBuilder.Build(screen, session);
            ChallengeAttemptMode selectedMode = default;
            screen.ChallengeRequested += (_, mode) => selectedMode = mode;
            screen.SelectSubject(SubjectId.Sprint);
            screen.LessonList.transform.Find("Lesson1").GetComponent<Button>().onClick.Invoke();
            Assert.That(selectedMode, Is.EqualTo(ChallengeAttemptMode.Review));
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("volleyball_learn"));

            Play(session, "volleyball_learn", true);
            Play(session, "volleyball_practice", true);
            session.Journey.SetAttemptsRemaining(0);
            screen.RefreshJourney(session);

            Assert.That(screen.LessonList.CurrentChallengeId, Is.EqualTo("volleyball_exam"));
            Transform exam = screen.LessonList.transform.Find("Lesson3");
            Assert.That(exam.GetComponent<Button>().interactable, Is.False);
            Assert.That(exam.Find("Objective").GetComponent<TMP_Text>().text, Is.EqualTo(VietText.Fix("Hết lượt thi")));

            var fresh = new GameSession();
            fresh.Journey.SetAttemptsRemaining(0);
            screen.RefreshJourney(fresh);
            Assert.That(screen.LessonList.CurrentChallengeId, Is.EqualTo("sprint_learn"));
            Assert.That(screen.LessonList.transform.Find("Lesson1").GetComponent<Button>().interactable, Is.True);
        }

        [Test]
        public void CompletedCourseShowsSummaryAndLessonReplayUsesFreePlay()
        {
            var session = new GameSession();
            foreach (ChallengeDefinition challenge in session.Journey.Catalog.Ordered)
                Play(session, challenge.Id, true);
            MapPresentationBuilder.Build(screen, session);

            Assert.That(session.Journey.CourseComplete, Is.True);
            Assert.That(screen.CourseSummary, Is.Not.Null);
            Assert.That(screen.CourseSummary.gameObject.activeSelf, Is.True);
            Assert.That(screen.CourseSummary.ScoreRows.Count, Is.EqualTo(3));
            ChallengeAttemptMode mode = default;
            screen.ChallengeRequested += (_, requested) => mode = requested;
            screen.LessonList.transform.Find("Lesson1").GetComponent<Button>().onClick.Invoke();
            Assert.That(mode, Is.EqualTo(ChallengeAttemptMode.FreePlay));
        }

        sealed class TestClock : IClock
        {
            public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);
        }

        [UnityTest]
        public IEnumerator ALifeRegeneratedOnTheMapUnlocksTheExamAndIsSaved()
        {
            var clock = new TestClock();
            var session = new GameSession(null, clock);
            Play(session, "sprint_learn", true);
            Play(session, "sprint_practice", true);
            session.Journey.SetAttemptsRemaining(0);
            session.RefreshLives();
            Assert.That(session.TimeUntilNextLife, Is.EqualTo(TimeSpan.FromMinutes(5)));

            MapPresentationBuilder.Build(screen, session);
            int persists = 0;
            screen.ConfigureLifePersistence(() => { persists++; return true; });
            screen.SelectSubject(SubjectId.Sprint);
            Button exam = screen.LessonList.transform.Find("Lesson3").GetComponent<Button>();
            Assert.That(exam.interactable, Is.False);
            Assert.That(screen.BudgetLabel.text, Is.EqualTo("Lượt thi: 0/5"));
            yield return null;
            Assert.That(persists, Is.Zero, "Nothing regenerated yet.");

            clock.UtcNow += TimeSpan.FromMinutes(5);
            yield return null;

            Assert.That(session.Lives, Is.EqualTo(1));
            Assert.That(exam.interactable, Is.True);
            Assert.That(screen.BudgetLabel.text, Is.EqualTo("Lượt thi: 1/5"));
            Assert.That(screen.Hearts.CurrentHearts, Is.EqualTo(1));
            Assert.That(screen.Hearts.CountdownText, Is.EqualTo("5:00"));
            Assert.That(persists, Is.EqualTo(1), "The regenerated life is saved.");
        }

        static void Play(GameSession session, string id, bool pass)
        {
            ChallengeDefinition definition = session.Journey.Catalog.Get(id);
            Assert.That(session.TryStartChallenge(id, ChallengeAttemptMode.Journey,
                definition.Difficulty, out ChallengeAttemptContext context), Is.True);
            session.SubmitChallengeResult(new ChallengeAttemptResult(context, pass,
                new ChallengeMetrics(completedTargets: definition.TargetCount),
                ChallengeDefinition.IsScored(definition.Kind)
                    ? new MinigameResult(pass, pass ? 8f : 0f, pass ? Rank.A : Rank.F) : null));
        }
    }
}
