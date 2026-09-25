using System.Collections.Generic;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class OpponentAiTests
    {
        [Test]
        public void ReturnsAnEasyServeWithATelegraphedSmashAwayFromThePlayer()
        {
            var match = new VolleyballMatch();
            Assert.That(MatchDriver.ServeGood(match).Kind, Is.EqualTo(ActionKind.ServeHit));

            bool sawTell = false;
            Vector2 aim = default;
            bool attacked = MatchDriver.AdvanceUntil(match, () =>
            {
                if (match.OpponentSmashTell)
                {
                    sawTell = true;
                    aim = match.OpponentAim;
                }

                return match.BallState == BallState.InPlay && match.Flight.Hitter == CourtSide.Opponent &&
                       match.Rally.Possession == CourtSide.Player;
            }, 6f);

            Assert.That(attacked, Is.True);
            Assert.That(sawTell, Is.True);
            Assert.That(aim, Is.EqualTo(new Vector2(-7f, -3f)));
            Assert.That(match.Flight.Target, Is.EqualTo(aim));
            Assert.That(match.Plan.Index, Is.EqualTo(1));
        }

        [Test]
        public void UnansweredSmashScoresForTheOpponent()
        {
            var match = new VolleyballMatch();
            MatchDriver.ServeGood(match);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 8f), Is.True);
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
        }

        [Test]
        public void WeakReceiveStepSendsAFreeBallStraightBack()
        {
            var plan = new OpponentPlan(new[]
            {
                new OpponentStep(new Vector2(-5f, 0f), AttackKind.Lob, -5f, 0f, true, false)
            });
            var match = new VolleyballMatch(plan);
            MatchDriver.ServeGood(match);

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.Flight.Hitter == CourtSide.Opponent, 4f), Is.True);
            Assert.That(match.Flight.Target, Is.EqualTo(VolleyballMatch.WeakReceiveTarget));
            Assert.That(match.Rally.Possession, Is.EqualTo(CourtSide.Player));
            Assert.That(match.Rally.Touches, Is.Zero);
        }

        [Test]
        public void PlayerBlockOnTheTelegraphedLineWinsThePoint()
        {
            var match = new VolleyballMatch();
            MatchDriver.ServeGood(match);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.OpponentSmashTell, 6f), Is.True);

            match.Player.PlaceAt(new Vector2(-.8f, match.OpponentAim.y));
            Assert.That(match.PressAction().Kind, Is.EqualTo(ActionKind.Block));

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 1f), Is.True);
            Assert.That(match.PlayerPoints, Is.EqualTo(1));
            Assert.That(match.Winners, Is.EqualTo(1));
        }

        static VolleyballMatch PlayerReadyToSmashIntoABlock()
        {
            var plan = new OpponentPlan(new[]
            {
                new OpponentStep(new Vector2(-5f, 0f), AttackKind.Lob, -5f, 0f, false, true)
            });
            var match = new VolleyballMatch(plan);
            match.ForceServerForTest(CourtSide.Opponent);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 3f), Is.True);

            float receiveIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(receiveIdeal));
            MatchDriver.AdvanceToFlightTime(match, receiveIdeal);
            Assert.That(match.PressAction().Kind, Is.EqualTo(ActionKind.Receive));

            float smashIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(smashIdeal));
            MatchDriver.AdvanceToFlightTime(match, smashIdeal);
            Assert.That(match.Opponent.Position.x, Is.LessThanOrEqualTo(ActionResolver.BlockNetDistance));
            return match;
        }

        [Test]
        public void OpponentBlocksASmashDownItsLine()
        {
            VolleyballMatch match = PlayerReadyToSmashIntoABlock();
            match.SetMove(Vector2.zero);
            Assert.That(match.PressAction().Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(match.BallState, Is.EqualTo(BallState.Dead));
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
            Assert.That(match.PlayerPoints, Is.Zero);
        }

        [Test]
        public void SmashAimedAwayFromTheBlockGetsThrough()
        {
            VolleyballMatch match = PlayerReadyToSmashIntoABlock();
            match.SetMove(Vector2.up);
            Assert.That(match.PressAction().Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(match.BallState, Is.EqualTo(BallState.InPlay));
            Assert.That(match.Flight.Target, Is.EqualTo(new Vector2(7f, 3f)));
        }

        static List<string> RunScripted()
        {
            var match = new VolleyballMatch();
            var log = new List<string>();
            match.PointScored += side => log.Add($"{side}:{match.PlayerPoints}-{match.OpponentPoints}@{match.Elapsed:F3}");
            for (int frame = 0; frame < 60 * 40 && !match.IsOver; frame++)
            {
                if (match.Server == CourtSide.Player && match.BallState == BallState.Held)
                    match.PressAction();
                else if (match.Server == CourtSide.Player && match.BallState == BallState.Toss &&
                         match.FlightTime >= match.Flight.ApexTime + .15f)
                    match.PressAction();
                match.Tick(1f / 60f);
            }

            return log;
        }

        [Test]
        public void IdenticalInputsProduceIdenticalMatches()
        {
            List<string> first = RunScripted();
            List<string> second = RunScripted();
            Assert.That(first, Is.Not.Empty);
            Assert.That(second, Is.EqualTo(first));
        }
    }
}
