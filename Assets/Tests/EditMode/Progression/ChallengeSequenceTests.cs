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

        [Test]
        public void NonFiniteProgress_CannotAdvanceOrCompletePunishment()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);
            session.SubmitResult(SubjectId.Sprint, Failed());
            var controller = new PunishmentController(session, SubjectId.Sprint,
                new ChallengeSequence(new[] { new ChallengeStep(ChallengeMechanic.TapMash, 5, 1) }));

            Assert.Throws<ArgumentOutOfRangeException>(() => controller.ReportDetectorProgress(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => controller.ReportDetectorProgress(float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => controller.ReportDetectorProgress(float.NegativeInfinity));

            Assert.That(controller.IsComplete, Is.False);
            Assert.That(controller.CurrentMechanic, Is.EqualTo(ChallengeMechanic.TapMash));
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(controller.CueVisible, Is.True);
            controller.ReportDetectorProgress(1);
            Assert.That(controller.IsComplete, Is.True);
        }

        [Test]
        public void Controller_ActivatesAuthoredCueAndCounterplayAdapter()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);
            session.SubmitResult(SubjectId.Sprint, Failed());
            var sequence = new ChallengeSequence(new[]
            {
                new ChallengeStep(ChallengeMechanic.TapMash, 5, 3),
                new ChallengeStep(ChallengeMechanic.RhythmHold, 6, 2),
                new ChallengeStep(ChallengeMechanic.AlternateTap, 7, 1)
            });
            var controller = new PunishmentController(session, SubjectId.Sprint, sequence);

            Assert.That(controller.Subject, Is.EqualTo(SubjectId.Sprint));
            Assert.That(controller.CurrentMechanic, Is.EqualTo(ChallengeMechanic.TapMash));
            Assert.That(controller.ActiveDetectors, Has.Exactly(1).TypeOf<TapMashDetector>());
            Assert.That(controller.CueVisible, Is.True);
            Assert.That(controller.CounterplayAvailable, Is.True);

            controller.ReportDetectorProgress(3);
            Assert.That(controller.ActiveDetectors, Has.Exactly(1).TypeOf<RhythmBeatDetector>());
            Assert.That(controller.ActiveDetectors, Has.Exactly(1).TypeOf<HoldDetector>());
            Assert.That(controller.Current.Mechanic, Is.EqualTo(ChallengeMechanic.RhythmHold));

            controller.ReportDetectorProgress(2);
            Assert.That(controller.ActiveDetectors, Has.Exactly(1).TypeOf<AlternateTapDetector>());
        }

        [Test]
        public void Controller_CompletesSessionOnceWithoutChangingLivesOrBypassingRetry()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);
            Assert.That(session.SubmitResult(SubjectId.Sprint, Failed()), Is.EqualTo(SessionRoute.Punishment));
            var controller = new PunishmentController(session, SubjectId.Sprint,
                new ChallengeSequence(new[] { new ChallengeStep(ChallengeMechanic.TapMash, 5, 1) }));
            var completionCount = 0;
            controller.Completed += route =>
            {
                completionCount++;
                Assert.That(route, Is.EqualTo(SessionRoute.RetrySubject));
            };

            controller.ReportDetectorProgress(1);
            controller.ReportDetectorProgress(1);

            Assert.That(completionCount, Is.EqualTo(1));
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(controller.IsComplete, Is.True);
            Assert.Throws<InvalidOperationException>(() => session.CompletePunishment());
            Assert.That(session.SubmitResult(SubjectId.Sprint, Failed()), Is.EqualTo(SessionRoute.Map));
            Assert.That(session.Lives, Is.EqualTo(4));
        }

        [Test]
        public void Controller_RejectsInvalidTransitions()
        {
            var session = new GameSession();
            Assert.Throws<ArgumentNullException>(() => new PunishmentController(null, SubjectId.Sprint,
                ChallengeSequence.BossDefault()));
            Assert.Throws<ArgumentNullException>(() => new PunishmentController(session, SubjectId.Sprint, null));
            Assert.Throws<InvalidOperationException>(() => new PunishmentController(session, SubjectId.Sprint,
                ChallengeSequence.BossDefault()));

            session.StartSubject(SubjectId.Sprint);
            session.SubmitResult(SubjectId.Sprint, Failed());
            Assert.Throws<InvalidOperationException>(() => new PunishmentController(session, SubjectId.Football,
                ChallengeSequence.BossDefault()));
        }

        static MinigameResult Failed() => new MinigameResult(false, 0, Rank.F);
    }
}
