using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class JourneyMapChessTests
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
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void MapShowsFourStopsWithTheFinalLockedUntilTheSoccerExam()
        {
            var session = new GameSession();
            CompleteThrough(session, "soccer_practice");
            MapPresentationBuilder.Build(screen, session);
            Assert.That(screen.Nodes.Select(n => n.SubjectId), Is.EqualTo(new[]
                { SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football, SubjectId.Chess }));
            Assert.That(screen.Nodes[3].IsInteractable, Is.False);
            Assert.That(screen.Nodes[3].StatusText, Is.EqualTo(VietText.Fix("Đạt Bóng đá để mở")));

            CompleteThrough(session, "soccer_exam");
            screen.RefreshJourney(session);
            Assert.That(screen.Nodes[3].IsInteractable, Is.True);
            Assert.That(screen.LessonList.CurrentChallengeId, Is.EqualTo("chess_final"));
            Assert.That(screen.LessonList.LessonIds, Is.EqualTo(new[] { "chess_final" }));
        }

        [Test]
        public void TheFinalCardStaysPlayableAtZeroLives()
        {
            var session = new GameSession();
            CompleteThrough(session, "soccer_exam");
            session.Journey.SetAttemptsRemaining(0);
            MapPresentationBuilder.Build(screen, session);
            screen.RefreshJourney(session);
            Assert.That(screen.LessonList.CurrentChallengeId, Is.EqualTo("chess_final"));
            screen.SelectSubject(SubjectId.Chess);
            Button play = screen.LessonList.transform.Find("DetailCard/PlayButton").GetComponent<Button>();
            Assert.That(play.interactable, Is.True);
            Assert.That(screen.LessonList.transform.Find("DetailCard/Status").GetComponent<TMPro.TMP_Text>().text,
                Does.Not.Contain(VietText.Fix("Hết lượt thi")));
        }

        [Test]
        public void CompletedCourseOffersTheCelebrationReplay()
        {
            var session = new GameSession();
            CompleteThrough(session, "chess_final");
            MapPresentationBuilder.Build(screen, session);
            screen.RefreshJourney(session);
            int requests = 0;
            screen.CelebrationRequested += () => requests++;
            Assert.That(screen.CourseSummary.gameObject.activeSelf, Is.True);
            screen.CourseSummary.ReplayButton.onClick.Invoke();
            Assert.That(requests, Is.EqualTo(1));
        }

        static void CompleteThrough(GameSession session, string id)
        {
            foreach (ChallengeDefinition definition in session.Journey.Catalog.Ordered)
            {
                if (session.Journey.IsChallengeComplete(definition.Id))
                {
                    if (definition.Id == id) return;
                    continue;
                }
                session.TryStartChallenge(definition.Id, ChallengeAttemptMode.Journey, definition.Difficulty,
                    out ChallengeAttemptContext context);
                session.SubmitChallengeResult(new ChallengeAttemptResult(context, true,
                    new ChallengeMetrics(completedTargets: definition.TargetCount),
                    ChallengeDefinition.IsScored(definition.Kind) ? new MinigameResult(true, 8f, Rank.A) : null));
                if (definition.Id == id) return;
            }
        }
    }
}
