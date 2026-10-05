using System;
using System.IO;
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class ChessFinalSaveTests
    {
        static readonly string[] AllNine =
        {
            "sprint_learn", "sprint_practice", "sprint_exam", "volleyball_learn", "volleyball_practice",
            "volleyball_exam", "soccer_learn", "soccer_practice", "soccer_exam"
        };

        string directory;
        SaveSystem saveSystem;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "KMA-ChessFinalSaveTests", Guid.NewGuid().ToString("N"));
            saveSystem = new SaveSystem(() => directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        static SaveData Version8(params string[] completed)
        {
            var data = new SaveData
            {
                version = 8,
                lives = 3,
                subjects = new[]
                {
                    new SubjectRecordData { id = SubjectId.Sprint, passed = true, bestScore = 7f, bestRank = Rank.B },
                    new SubjectRecordData { id = SubjectId.Football, passed = completed.Length >= 9, bestScore = 8f, bestRank = Rank.A },
                    new SubjectRecordData { id = SubjectId.Volleyball, passed = completed.Length >= 6, bestScore = 6f, bestRank = Rank.C }
                },
                tutorialSeen = new[] { true, false, true },
                settings = Settings.CreateDefault(),
                journey = new JourneyStateData()
            };
            data.journey.completedChallengeIds.AddRange(completed);
            data.journey.seenDialogueIds.Add("opening");
            return data;
        }

        [Test]
        public void Load_Version8MidCourse_KeepsJourney()
        {
            SaveData old = Version8("sprint_learn", "sprint_practice", "sprint_exam", "volleyball_learn");
            old.journey.failCounts.Add(new JourneyFailCountData { challengeId = "volleyball_practice", count = 1 });
            saveSystem.Save(old);

            SaveData loaded = saveSystem.Load();

            Assert.That(saveSystem.HasLoadedValidSave, Is.True);
            Assert.That(loaded.version, Is.EqualTo(9));
            Assert.That(loaded.subjects.Length, Is.EqualTo(4));
            Assert.That(loaded.tutorialSeen.Length, Is.EqualTo(4));
            Assert.That(loaded.journey.completedChallengeIds, Is.EqualTo(new[]
                { "sprint_learn", "sprint_practice", "sprint_exam", "volleyball_learn" }));
            Assert.That(loaded.journey.failCounts[0].challengeId, Is.EqualTo("volleyball_practice"));
            Assert.That(loaded.journey.seenDialogueIds, Does.Contain("opening"));
            Assert.That(loaded.lives, Is.EqualTo(3));
            Assert.That(loaded.tutorialSeen[0], Is.True);
            Assert.That(loaded.tutorialSeen[1], Is.False);
            Assert.That(loaded.tutorialSeen[2], Is.True);
            Assert.That(loaded.tutorialSeen[3], Is.False);
        }

        [Test]
        public void Load_Version8CompletedCourse_ResumesAtTheChessFinal()
        {
            saveSystem.Save(Version8(AllNine));
            var session = new GameSession();
            session.Restore(saveSystem.Load());
            Assert.That(session.Journey.CourseComplete, Is.False);
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("chess_final"));
            Assert.That(session.GetRecord(SubjectId.Football).BestScore, Is.EqualTo(8f));
            Assert.That(session.Journey.CelebrationSeen, Is.False);
        }

        [Test]
        public void ChessBestKeepsTheHighestWinAndRoundTrips()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_exam");
            Assert.That(session.TryStartChallenge("chess_final", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.True);
            CommitActive(session, true, 7.5f, 50f, 1, true);
            Assert.That(session.Journey.CelebrationSeen, Is.False);
            Assert.That(session.Journey.MarkCelebrationSeen(), Is.True);

            Assert.That(session.StartSubject(SubjectId.Chess), Is.EqualTo(SessionRoute.Subject));
            CommitActive(session, true, 9.2f, 31f, 0, false);
            Assert.That(session.StartSubject(SubjectId.Chess), Is.EqualTo(SessionRoute.Subject));
            CommitActive(session, false, 0f, 90f, 3, false);

            var restored = new GameSession();
            restored.Restore(session.ToSaveData());
            JourneyChessRecordData best = restored.Journey.ChessBest;
            Assert.That(best.recorded, Is.True);
            Assert.That(best.score, Is.EqualTo(9.2f));
            Assert.That(best.thinkSeconds, Is.EqualTo(31f));
            Assert.That(best.mistakes, Is.EqualTo(0));
            Assert.That(best.hintUsed, Is.False);
            Assert.That(restored.Journey.CelebrationSeen, Is.True);
        }

        [Test]
        public void CelebrationCannotBeMarkedBeforeTheCourseIsComplete()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_exam");
            Assert.That(session.Journey.MarkCelebrationSeen(), Is.False);
            SaveData data = session.ToSaveData();
            data.journey.celebrationSeen = true;
            var restored = new GameSession();
            restored.Restore(data);
            Assert.That(restored.Journey.CelebrationSeen, Is.False);
        }

        static void CommitActive(GameSession session, bool pass, float score, float seconds, int mistakes, bool hint)
        {
            ChallengeAttemptContext context = session.Journey.ActiveAttempt;
            session.SubmitChallengeResult(new ChallengeAttemptResult(context, pass,
                new ChallengeMetrics(elapsed: seconds, completedTargets: 2, mistakes: mistakes, hintUsed: hint),
                new MinigameResult(pass, score, pass ? ScoreUtil.ToRank(score) : Rank.F)));
        }
    }
}
