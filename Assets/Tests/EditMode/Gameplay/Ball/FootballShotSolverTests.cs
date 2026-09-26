using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class FootballShotSolverTests
    {
        static FootballFlightSimulation Finish(float direction, float power, bool keeper = false)
        {
            var flight = new FootballFlightSimulation(FootballShotSolver.Create(direction, power), FootballTuning.For(FootballDifficulty.Normal), keeper);
            for (int i = 0; i < 4000 && !flight.Done; i++) flight.Step();
            Assert.That(flight.Done, Is.True);
            return flight;
        }
        [Test]
        public void FreeFlightObeysVelocityAndGravity()
        {
            var shot = FootballShotSolver.Create(.4f, .5f);
            var flight = new FootballFlightSimulation(shot, FootballTuning.For(FootballDifficulty.Normal), false);
            for (int i = 0; i < 24; i++) flight.Step();
            var expected = new Vector3(0f, .11f, 0f) + shot.Velocity * .1f + Vector3.down * (.5f * 9.81f * .1f * .1f);
            Assert.That(Vector3.Distance(flight.Position, expected), Is.LessThan(.0001f));
        }
        [TestCase(.55f, .5f, FootballOutcome.Goal)]
        [TestCase(.55f, 1f, FootballOutcome.High)]
        [TestCase(1f, .5f, FootballOutcome.Wide)]
        [TestCase(0f, 0f, FootballOutcome.Short)]
        [TestCase(3.66f / 4.3f, .5f, FootballOutcome.Post)]
        public void PhysicalContactsDetermineOutcome(float direction, float power, FootballOutcome outcome)
        { Assert.That(Finish(direction, power).Outcome, Is.EqualTo(outcome)); }
        [Test]
        public void WeakShotBouncesAndStopsBeforeGoal()
        {
            var flight = Finish(.5f, 0f);
            Assert.That(flight.Bounces, Is.GreaterThan(0));
            Assert.That(flight.Position.z, Is.LessThan(11f));
            Assert.That(flight.Position.y, Is.GreaterThanOrEqualTo(.11f));
        }
        [Test]
        public void CrossbarAndPostReboundTowardField()
        {
            float angle = 22f * Mathf.Deg2Rad;
            float speed = Mathf.Sqrt(9.81f * 121f / (2f * Mathf.Cos(angle) * Mathf.Cos(angle) * (.11f + 11f * Mathf.Tan(angle) - 2.44f)));
            var bar = Finish(0f, (speed - 6f) / 20f);
            Assert.That(bar.Outcome, Is.EqualTo(FootballOutcome.Crossbar));
            Assert.That(bar.Velocity.z, Is.LessThanOrEqualTo(0f));
            Assert.That(Finish(3.66f / 4.3f, .5f).Velocity.z, Is.LessThanOrEqualTo(0f));
        }
        [Test]
        public void KeeperReactsAfterDelayAndSavesActualContact()
        {
            var f = new FootballFlightSimulation(FootballShotSolver.Create(.6f, .5f), FootballTuning.For(FootballDifficulty.Normal));
            for (int i = 0; i < 48; i++) f.Step();
            Assert.That(f.KeeperX, Is.Zero);
            for (int i = 0; i < 48; i++) f.Step();
            Assert.That(f.KeeperX, Is.GreaterThan(0f));
            Assert.That(Finish(0f, .5f, true).Outcome, Is.EqualTo(FootballOutcome.Saved));
        }
        [Test]
        public void PredictionMatchesLiveFlightWithoutKeeper()
        {
            var shot = FootballShotSolver.Create(.55f, .5f);
            var tuning = FootballTuning.For(FootballDifficulty.Normal);
            var points = new Vector3[140];
            int count = FootballShotSolver.Predict(shot, tuning, points);
            var flight = new FootballFlightSimulation(shot, tuning, false);
            while (!flight.Outcome.HasValue) flight.Step();
            Assert.That(Vector3.Distance(points[count - 1], flight.Position), Is.LessThan(.0001f));
            Assert.That(count, Is.GreaterThan(2));
        }
        [Test]
        public void IdenticalShotsAreDeterministicAndMirroredShotsAreSymmetric()
        {
            var a = Finish(.55f, .5f); var b = Finish(.55f, .5f); var c = Finish(-.55f, .5f);
            Assert.That(a.Position, Is.EqualTo(b.Position));
            Assert.That(a.Position.x, Is.EqualTo(-c.Position.x).Within(.0001f));
            Assert.That(a.Outcome, Is.EqualTo(c.Outcome));
            Assert.That(FootballShotSolver.Project(new Vector3(0f, .11f, 11f)).z,
                Is.LessThan(FootballShotSolver.Project(new Vector3(0f, .11f, 0f)).z));
        }
        [TestCase(float.NaN, .5f)] [TestCase(1.01f, .5f)] [TestCase(0f, float.PositiveInfinity)]
        public void InvalidShotArgumentsAreRejected(float aim, float power)
        { Assert.That(() => FootballShotSolver.Create(aim, power), Throws.TypeOf<System.ArgumentOutOfRangeException>()); }
    }
}
