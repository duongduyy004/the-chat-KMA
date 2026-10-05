using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        static void Play(GameSession session, string id, bool pass)
        {
            ChallengeDefinition definition = session.Journey.Catalog.Get(id);
            Assert.That(session.TryStartChallenge(id, ChallengeAttemptMode.Journey,
                definition.Difficulty, out ChallengeAttemptContext context), Is.True);
            session.SubmitChallengeResult(new ChallengeAttemptResult(context, pass,
                new ChallengeMetrics(completedTargets: definition.TargetCount),
                definition.Kind == ChallengeKind.Exam
                    ? new MinigameResult(pass, pass ? 8f : 0f, pass ? Rank.A : Rank.F) : null));
        }
    }
}
