using System;
using System.Linq;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class JourneySaveTests
    {
        ChallengeCatalog catalog;

        [SetUp]
        public void SetUp() => catalog = ChallengeCatalog.LoadDefault();

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void LegacyVersionSix_ImportsOnlyTheContinuousSubjectPrefix(int mask)
        {
            SaveData source = LegacySave(mask);

            SaveData migrated = JourneySaveMigration.MigrateLegacy(source, catalog);

            int prefix = ContinuousPassedPrefix(mask);
            Assert.That(migrated.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(migrated.journey.completedChallengeIds.Count, Is.EqualTo(prefix * 3));
            Assert.That(migrated.journey.completedChallengeIds,
                Is.EqualTo(catalog.Ordered.Take(prefix * 3).Select(x => x.Id)));
            Assert.That(migrated.subjects.Single(x => x.id == SubjectId.Sprint).passed,
                Is.EqualTo(prefix >= 1));
            Assert.That(migrated.subjects.Single(x => x.id == SubjectId.Volleyball).passed,
                Is.EqualTo(prefix >= 2));
            Assert.That(migrated.subjects.Single(x => x.id == SubjectId.Football).passed,
                Is.EqualTo(prefix >= 3));

            SubjectRecordData football = migrated.subjects.Single(x => x.id == SubjectId.Football);
            if (prefix < 3)
            {
                Assert.That(football.bestScore, Is.EqualTo(8f));
                Assert.That(football.bestRank, Is.EqualTo(Rank.A));
                Assert.That(migrated.journey.completedChallengeIds, Does.Not.Contain("soccer_exam"));
            }
        }

        [Test]
        public void LegacySubjectOrder_IsSprintVolleyballFootballDespiteEnumValues()
        {
            Assert.That(catalog.Ordered.Where((_, index) => index % 3 == 0).Select(x => (int)x.Subject),
                Is.EqualTo(new[] { 0, 7, 6 }));
        }

        [Test]
        public void SettingsOnlySave_DoesNotImportCourseProgress()
        {
            SaveData source = LegacySave(7);
            source.settingsOnly = true;

            SaveData migrated = JourneySaveMigration.MigrateLegacy(source, catalog);

            Assert.That(migrated.settingsOnly, Is.True);
            Assert.That(migrated.journey.completedChallengeIds, Is.Empty);
            Assert.That(migrated.hasActiveSubject, Is.False);
            Assert.That(migrated.subjects.Single(x => x.id == SubjectId.Football).bestScore, Is.EqualTo(8f));
        }

        [Test]
        public void Normalize_DropsUnknownAndNonContinuousCompletionAndClampsBudget()
        {
            SaveData data = SaveData.CreateDefault();
            data.journey.completedChallengeIds.AddRange(new[]
            {
                "sprint_learn", "sprint_practice", "sprint_exam", "volleyball_learn", "soccer_learn", "unknown"
            });
            data.lives = 9;

            SaveData normalized = JourneySaveMigration.Normalize(data, catalog);

            Assert.That(normalized.lives, Is.EqualTo(5));
            Assert.That(normalized.journey.completedChallengeIds,
                Is.EqualTo(new[] { "sprint_learn", "sprint_practice", "sprint_exam", "volleyball_learn" }));
            var restored = new GameSession();
            restored.Restore(normalized);
            Assert.That(restored.Journey.CheckpointChallengeId, Is.EqualTo("volleyball_practice"));
        }

        [Test]
        public void Normalize_ZeroBudgetCreatesSupplementaryCheckpointForCurrentSubject()
        {
            SaveData data = SaveData.CreateDefault();
            data.journey.completedChallengeIds.AddRange(new[]
                { "sprint_learn", "sprint_practice", "sprint_exam" });
            data.lives = 0;

            SaveData normalized = JourneySaveMigration.Normalize(data, catalog);
            var restored = new GameSession();
            restored.Restore(normalized);

            Assert.That(restored.Lives, Is.Zero);
            Assert.That(restored.Journey.AwaitingSupplementary, Is.True);
            Assert.That(restored.Journey.CheckpointChallengeId, Is.EqualTo("volleyball_practice"));
            Assert.That(restored.Journey.IsSubjectUnlocked(SubjectId.Volleyball), Is.True);
            Assert.That(restored.Journey.IsSubjectUnlocked(SubjectId.Football), Is.False);
        }

        [Test]
        public void Restore_DiscardsUnresolvedAttemptAndStartsAtItsChallengeAgain()
        {
            var sourceSession = new GameSession();
            Assert.That(sourceSession.TryStartChallenge("sprint_learn", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.True);
            SaveData saved = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(sourceSession.ToSaveData()));

            var restored = new GameSession();
            restored.Restore(saved);

            Assert.That(restored.Journey.ActiveAttempt, Is.Null);
            Assert.That(restored.ActiveSubject, Is.Null);
            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
            Assert.That(restored.Journey.CheckpointChallengeId, Is.EqualTo("sprint_learn"));
        }

        [Test]
        public void Restore_RecordedExamFailureKeepsSpentAttemptAndDoesNotReplayReceipt()
        {
            var original = new GameSession();
            JourneyTestData.CompleteThrough(original, "sprint_practice");
            Assert.That(original.TryStartChallenge("sprint_exam", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out ChallengeAttemptContext context), Is.True);
            var failure = new ChallengeAttemptResult(context, false, new ChallengeMetrics(),
                new MinigameResult(false, 0f, Rank.F));
            Assert.That(original.SubmitChallengeResult(failure).AttemptsRemaining, Is.EqualTo(4));
            SaveData saved = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(original.ToSaveData()));

            var restored = new GameSession();
            restored.Restore(saved);

            Assert.That(restored.Lives, Is.EqualTo(4));
            Assert.That(restored.Journey.CheckpointChallengeId, Is.EqualTo("sprint_exam"));
            Assert.That(restored.SubmitChallengeResult(failure).Accepted, Is.False);
            Assert.That(restored.Lives, Is.EqualTo(4));
            Assert.That(restored.GetRecord(SubjectId.Sprint).FailedVisits, Is.EqualTo(1));
        }

        [Test]
        public void Restore_SupplementaryReceiptCannotResetAttemptsTwice()
        {
            var original = new GameSession();
            JourneyTestData.CompleteThrough(original, "sprint_practice");
            for (int i = 0; i < 5; i++)
                JourneyTestData.Play(original, "sprint_exam", false);
            Assert.That(original.TryStartChallenge("sprint_practice", ChallengeAttemptMode.Supplementary,
                ChallengeDifficulty.Normal, out ChallengeAttemptContext context), Is.True);
            var supplementary = new ChallengeAttemptResult(context, true,
                new ChallengeMetrics(distance: 100f, elapsed: 18f));
            original.SubmitChallengeResult(supplementary);
            SaveData saved = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(original.ToSaveData()));

            var restored = new GameSession();
            restored.Restore(saved);

            Assert.That(restored.Lives, Is.EqualTo(5));
            Assert.That(restored.Journey.SupplementaryRounds, Is.EqualTo(1));
            Assert.That(restored.SubmitChallengeResult(supplementary).Accepted, Is.False);
            Assert.That(restored.Lives, Is.EqualTo(5));
            Assert.That(restored.Journey.SupplementaryRounds, Is.EqualTo(1));
        }

        [Test]
        public void CurrentVersionJson_RoundTripsJourneyAndKeepsSettingsOnlyMarker()
        {
            SaveData data = SaveData.CreateDefault();
            data.settingsOnly = true;
            data.journey.completedChallengeIds.Add("sprint_learn");
            data.journey.lastCommittedAttemptId = "receipt-1";
            data.journey.lastCommittedResult = new JourneyResultData
            {
                context = new JourneyAttemptData
                {
                    attemptId = "receipt-1", challengeId = "sprint_learn",
                    mode = ChallengeAttemptMode.Journey, difficulty = ChallengeDifficulty.Normal
                },
                pass = true,
                completedTargets = 12,
                examResult = new MinigameResult(true, 8f, Rank.A)
            };
            SaveData restored = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));

            Assert.That(restored.journey.completedChallengeIds, Is.EqualTo(new[] { "sprint_learn" }));
            Assert.That(restored.journey.lastCommittedAttemptId, Is.EqualTo("receipt-1"));
            Assert.That(restored.journey.lastCommittedResult.examResult.Score, Is.EqualTo(8f));
            Assert.That(restored.settingsOnly, Is.True);
        }

        [Test]
        public void Normalize_MissingJourneyDataStartsAValidEmptyCourse()
        {
            SaveData data = SaveData.CreateDefault();
            data.journey = null;

            SaveData normalized = JourneySaveMigration.Normalize(data, catalog);

            Assert.That(normalized.journey, Is.Not.Null);
            Assert.That(normalized.journey.completedChallengeIds, Is.Empty);
            Assert.That(normalized.version, Is.EqualTo(SaveData.CurrentVersion));
        }

        static SaveData LegacySave(int mask)
        {
            SaveData data = SaveData.CreateDefault();
            data.version = 6;
            data.subjects = new[]
            {
                new SubjectRecordData { id = SubjectId.Sprint, passed = (mask & 1) != 0,
                    bestScore = 8f, bestRank = Rank.A },
                new SubjectRecordData { id = SubjectId.Volleyball, passed = (mask & 2) != 0,
                    bestScore = 7f, bestRank = Rank.B },
                new SubjectRecordData { id = SubjectId.Football, passed = (mask & 4) != 0,
                    bestScore = 8f, bestRank = Rank.A }
            };
            return data;
        }

        static int ContinuousPassedPrefix(int mask)
        {
            int prefix = 0;
            if ((mask & 1) == 0) return prefix;
            prefix++;
            if ((mask & 2) == 0) return prefix;
            prefix++;
            if ((mask & 4) != 0) prefix++;
            return prefix;
        }
    }
}
