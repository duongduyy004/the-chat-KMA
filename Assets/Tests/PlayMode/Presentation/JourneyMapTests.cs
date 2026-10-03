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
        public void ReviewDoesNotMoveCheckpointAndExhaustedBudgetOffersCurrentSupplementaryPractice()
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
            for (int attempt = 0; attempt < GameSession.MaxLives; attempt++)
                Play(session, "volleyball_exam", false);
            screen.RefreshJourney(session);

            Assert.That(session.Journey.AwaitingSupplementary, Is.True);
            Assert.That(screen.LessonList.CurrentChallengeId, Is.EqualTo("volleyball_practice"));
            screen.LessonList.transform.Find("Lesson2").GetComponent<Button>().onClick.Invoke();
            Assert.That(selectedMode, Is.EqualTo(ChallengeAttemptMode.Supplementary));
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
