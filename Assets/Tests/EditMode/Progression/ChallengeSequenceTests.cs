using System;
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class ChallengeSequenceTests
    {
        [Test]
        public void Advance_UsesAuthoredOrderAndCompletesOnce()
        {
            var sequence = new ChallengeSequence(new[]
            {
                new ChallengeStep(ChallengeMechanic.TapMash, 5, 20),
                new ChallengeStep(ChallengeMechanic.RhythmHold, 6, 8)
            });
            var completionCount = 0;
            sequence.Completed += () => completionCount++;

            Assert.That(sequence.Current.Mechanic, Is.EqualTo(ChallengeMechanic.TapMash));
            sequence.ReportProgress(19);
            Assert.That(sequence.Current.Mechanic, Is.EqualTo(ChallengeMechanic.TapMash));
            sequence.ReportProgress(20);
            Assert.That(sequence.Current.Mechanic, Is.EqualTo(ChallengeMechanic.RhythmHold));
            sequence.ReportProgress(8);
            sequence.ReportProgress(100);

            Assert.That(sequence.IsComplete, Is.True);
            Assert.That(completionCount, Is.EqualTo(1));
        }

        [Test]
        public void InvalidSequenceAndSteps_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new ChallengeSequence(null));
            Assert.Throws<ArgumentException>(() => new ChallengeSequence(Array.Empty<ChallengeStep>()));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ChallengeStep(ChallengeMechanic.TapMash, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ChallengeStep(ChallengeMechanic.TapMash, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ChallengeStep(ChallengeMechanic.TapMash, float.NaN, 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ChallengeStep(ChallengeMechanic.TapMash, float.PositiveInfinity, 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ChallengeStep(ChallengeMechanic.TapMash, 1, float.NegativeInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ChallengeStep(ChallengeMechanic.TapMash, 1, float.NaN));
        }

        static ChallengeSequence ValidSequence() => new ChallengeSequence(new[]
        {
            new ChallengeStep(ChallengeMechanic.TapMash, 1, 1)
        });

        static MinigameResult Failed() => new MinigameResult(false, 0, Rank.F);
    }
}
