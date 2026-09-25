using System;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class BallFlightTests
    {
        static BallFlight Serve() =>
            new BallFlight(new Vector2(8.5f, 0f), 3.2f, new Vector2(-5f, 0f), 4.5f, CourtSide.Opponent);

        [Test]
        public void PeaksAtTheApexHeight()
        {
            BallFlight flight = Serve();
            Assert.That(flight.HeightAt(flight.ApexTime), Is.EqualTo(4.5f).Within(1e-3f));
            Assert.That(flight.ApexTime, Is.LessThan(flight.Duration));
        }

        [Test]
        public void LandsOnTheTargetAtItsDuration()
        {
            BallFlight flight = Serve();
            Assert.That(flight.GroundAt(flight.Duration), Is.EqualTo(new Vector2(-5f, 0f)));
            Assert.That(flight.HeightAt(flight.Duration), Is.EqualTo(0f).Within(1e-3f));
            Assert.That(flight.GroundAt(flight.Duration + 1f), Is.EqualTo(new Vector2(-5f, 0f)));
            Assert.That(flight.GroundAt(0f), Is.EqualTo(new Vector2(8.5f, 0f)));
        }

        [Test]
        public void TimeAtHeightDescendingIsAfterTheApex()
        {
            BallFlight flight = Serve();
            float t = flight.TimeAtHeightDescending(1f);
            Assert.That(t, Is.GreaterThan(flight.ApexTime));
            Assert.That(flight.HeightAt(t), Is.EqualTo(1f).Within(1e-3f));
            Assert.That(flight.IsDescending(t), Is.True);
            Assert.That(flight.TimeAtHeightDescending(10f), Is.EqualTo(flight.ApexTime));
            Assert.That(flight.TimeAtHeightDescending(0f), Is.EqualTo(flight.Duration));
        }

        [Test]
        public void SmashFromTheSetSpotClearsTheNet()
        {
            var smash = new BallFlight(new Vector2(-1.5f, 0f), 2.6f, new Vector2(7f, 0f), 2.8f, CourtSide.Player);
            Assert.That(smash.CrossesNet, Is.True);
            Assert.That(smash.NetCrossTime, Is.GreaterThan(0f).And.LessThan(smash.Duration));
            Assert.That(smash.ClearsNet, Is.True);
        }

        [Test]
        public void LowFlatShotFailsNetClearance()
        {
            var shot = new BallFlight(new Vector2(-3f, 0f), 1f, new Vector2(5f, 0f), 1f, CourtSide.Player);
            Assert.That(shot.CrossesNet, Is.True);
            Assert.That(shot.ClearsNet, Is.False);
        }

        [Test]
        public void FlightThatStaysOnOneSideDoesNotCrossTheNet()
        {
            var set = new BallFlight(new Vector2(-6f, 0f), 1f, new Vector2(-1.5f, 0f), 4f, CourtSide.Player);
            Assert.That(set.CrossesNet, Is.False);
            Assert.That(set.NetCrossTime, Is.EqualTo(-1f));
            Assert.That(set.ClearsNet, Is.True);
        }

        [Test]
        public void RejectsAnApexBelowTheStart()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BallFlight(Vector2.zero, 2f, Vector2.one, 1f, CourtSide.Player));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BallFlight(Vector2.zero, 0f, Vector2.one, 0f, CourtSide.Player));
        }
    }
}
