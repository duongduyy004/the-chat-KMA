using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class JourneyProgressTests
    {
        [TestCase(ChallengeDifficulty.Easy)]
        [TestCase(ChallengeDifficulty.Hard)]
        public void FootballAlwaysUsesNormalForLegacyDifficultyRequests(ChallengeDifficulty legacyDifficulty)
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "volleyball_exam");
            Assert.That(session.TryStartChallenge("soccer_learn", ChallengeAttemptMode.Journey, legacyDifficulty,
                out ChallengeAttemptContext normal), Is.True);
            Assert.That(normal.Difficulty, Is.EqualTo(ChallengeDifficulty.Normal));
            session.SubmitChallengeResult(new ChallengeAttemptResult(normal, true, new ChallengeMetrics()));
            Assert.That(session.TryStartChallenge("soccer_learn", ChallengeAttemptMode.Review, legacyDifficulty,
                out ChallengeAttemptContext review), Is.True);
            Assert.That(review.Difficulty, Is.EqualTo(ChallengeDifficulty.Normal));
            session.AbandonActiveChallenge();
            JourneyTestData.CompleteThrough(session, "soccer_exam");
            Assert.That(session.TryStartChallenge("soccer_exam", ChallengeAttemptMode.FreePlay, legacyDifficulty,
                out ChallengeAttemptContext replay), Is.True);
            Assert.That(replay.Difficulty, Is.EqualTo(ChallengeDifficulty.Normal));
        }

        [Test]
        public void CourseProgress_UnlocksSubjectsOnlyAfterTheirExamPasses()
        {
            var session = new GameSession();

            Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Sprint), Is.True);
            Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Volleyball), Is.False);
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Volleyball), Is.False);
            JourneyTestData.CompleteThrough(session, "sprint_exam");
            Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Volleyball), Is.True);
            Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Football), Is.False);
        }

        [Test]
        public void LearnFailureIsFreeAndPracticeFailureRequiresAFrogJump()
        {
            var session = new GameSession();

            Assert.That(JourneyTestData.Play(session, "sprint_learn", false).FrogJumpRequired, Is.False);
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_learn"));

            JourneyTestData.CompleteThrough(session, "sprint_learn");
            JourneyCommitOutcome practice = JourneyTestData.Play(session, "sprint_practice", false);
            Assert.That(practice.FrogJumpRequired, Is.True);
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_practice"));
        }

        [Test]
        public void ReviewAndFreePlay_CannotAdvanceAnIncompleteCourse()
        {
            var session = new GameSession();

            Assert.That(session.TryStartChallenge("sprint_learn", ChallengeAttemptMode.Review,
                ChallengeDifficulty.Normal, out _), Is.False);
            Assert.That(session.TryStartChallenge("volleyball_learn", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Easy, out _), Is.False);
            Assert.That(session.TryStartChallenge("soccer_exam", ChallengeAttemptMode.FreePlay,
                ChallengeDifficulty.Hard, out _), Is.False);
            Assert.That(session.Journey.IsChallengeComplete("sprint_learn"), Is.False);
            Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Volleyball), Is.False);
        }

        [Test]
        public void ResultMustMatchActiveAttemptAndReceiptCanCommitOnlyOnce()
        {
            var session = new GameSession();
            Assert.That(session.TryStartChallenge("sprint_learn", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out ChallengeAttemptContext context), Is.True);
            Assert.That(session.TryStartChallenge("unknown", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.False);
            Assert.That(session.TryStartChallenge("sprint_learn", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Hard, out _), Is.False);

            var mismatched = new ChallengeAttemptResult(new ChallengeAttemptContext(
                "wrong-attempt", context.ChallengeId, context.Mode, context.Difficulty), true,
                new ChallengeMetrics(completedTargets: 12));
            Assert.That(session.SubmitChallengeResult(mismatched).Accepted, Is.False);
            var wrongMode = new ChallengeAttemptResult(new ChallengeAttemptContext(
                context.AttemptId, context.ChallengeId, ChallengeAttemptMode.Review, context.Difficulty), true,
                new ChallengeMetrics(completedTargets: 12));
            Assert.That(session.SubmitChallengeResult(wrongMode).Accepted, Is.False);
            Assert.That(session.Journey.ActiveAttempt, Is.SameAs(context));

            var result = new ChallengeAttemptResult(context, true, new ChallengeMetrics(completedTargets: 12));
            Assert.That(session.SubmitChallengeResult(result).Accepted, Is.True);
            JourneyStateData afterCommit = session.Journey.ToData();
            Assert.That(session.SubmitChallengeResult(result).Accepted, Is.False);
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_practice"));
            Assert.That(session.Journey.ToData().completedChallengeIds,
                Is.EqualTo(afterCommit.completedChallengeIds));
        }

        [Test]
        public void RepeatedExamFailuresDrainLivesAndNeverOpenSupplementaryPractice()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");

            JourneyTestData.Play(session, "sprint_exam", false);
            session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, false);
            for (int i = 0; i < 4; i++)
            {
                JourneyTestData.Play(session, "sprint_exam", false);
                session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, true);
            }

            Assert.That(session.Lives, Is.Zero);
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_exam"));
            Assert.That(session.Journey.TryBegin("sprint_practice", ChallengeAttemptMode.Supplementary,
                ChallengeDifficulty.Normal, out _), Is.False);
        }

        [Test]
        public void CompletingAllChallenges_AllowsReplayThatOnlyImprovesBestExamScore()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_exam");

            Assert.That(session.Journey.CourseComplete, Is.True);
            Assert.That(session.Journey.CheckpointChallengeId, Is.Null);
            Assert.That(session.TryStartChallenge("soccer_exam", ChallengeAttemptMode.FreePlay,
                ChallengeDifficulty.Normal, out ChallengeAttemptContext replay), Is.True);
            var result = new ChallengeAttemptResult(replay, true,
                new ChallengeMetrics(kicks: 5, completedTargets: 3), new MinigameResult(true, 8f, Rank.A));
            Assert.That(session.SubmitChallengeResult(result).Accepted, Is.True);
            Assert.That(session.GetRecord(SubjectId.Football).BestScore, Is.EqualTo(8f));

            Assert.That(session.TryStartChallenge("soccer_exam", ChallengeAttemptMode.FreePlay,
                ChallengeDifficulty.Normal, out ChallengeAttemptContext replayAgain), Is.True);
            session.SubmitChallengeResult(new ChallengeAttemptResult(replayAgain, true,
                new ChallengeMetrics(kicks: 5, completedTargets: 3), new MinigameResult(true, 6f, Rank.B)));
            Assert.That(session.GetRecord(SubjectId.Football).BestScore, Is.EqualTo(8f));
            Assert.That(session.Journey.CourseComplete, Is.True);
        }

        [Test]
        public void LegacyRecordWithScoreButNoPass_KeepsHigherBestScore()
        {
            SubjectRecord record = SubjectRecord.FromData(new SubjectRecordData
            {
                id = SubjectId.Football,
                passed = false,
                bestScore = 8f,
                bestRank = Rank.A,
                failedVisits = 2
            });

            record.Accept(new MinigameResult(true, 6f, Rank.B));

            Assert.That(record.Passed, Is.True);
            Assert.That(record.BestScore, Is.EqualTo(8f));
            Assert.That(record.BestRank, Is.EqualTo(Rank.A));
        }
    }
}
