using System.Collections.Generic;
using System.Linq;
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
            Assert.That(aim, Is.EqualTo(new Vector2(-6f, -2f)));
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
        public void MissedReceiveStepLetsTheBallDropAndMovesThePlanOn()
        {
            var plan = new OpponentPlan(new[]
            {
                new OpponentStep(new Vector2(-5f, 0f), AttackKind.Lob, -5f, 0f, false, false, missesReceive: true),
                new OpponentStep(new Vector2(-5f, 0f), AttackKind.Lob, -5f, 0f, false, false)
            });
            var match = new VolleyballMatch(plan);
            MatchDriver.ServeGood(match);

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 6f), Is.True);
            Assert.That(match.Flight.Hitter, Is.EqualTo(CourtSide.Player), "the AI must not touch the ball");
            Assert.That(match.PlayerPoints, Is.EqualTo(1));
            Assert.That(match.Plan.Index, Is.EqualTo(1));
        }

        static VolleyballMatch OpponentAboutToSmash(out float netCrossY)
        {
            var match = new VolleyballMatch();
            MatchDriver.ServeGood(match);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.OpponentSmashTell, 6f), Is.True);
            // The block is judged against where the smash crosses the net, not its landing spot
            // (OpponentAim); for this serve the two differ by more than a block's width.
            netCrossY = ActionResolver.NetCrossY(match.Flight.GroundAt(match.FlightTime), match.OpponentAim);
            Assert.That(Mathf.Abs(netCrossY - match.OpponentAim.y), Is.GreaterThan(ActionResolver.BlockLateral));
            return match;
        }

        [Test]
        public void JumpingOnTheTelegraphedLineBlocksTheSmash()
        {
            VolleyballMatch match = OpponentAboutToSmash(out float netCrossY);
            int blocked = 0;
            match.PlayerBlocked += () => blocked++;
            match.Player.PlaceAt(new Vector2(-.8f, netCrossY));
            Assert.That(match.PressJump(), Is.True);

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 1f), Is.True);
            Assert.That(match.PlayerPoints, Is.EqualTo(1));
            Assert.That(match.Winners, Is.EqualTo(1));
            Assert.That(blocked, Is.EqualTo(1));
        }

        [Test]
        public void JumpingOnTheLandingSpotInsteadOfTheNetLineDoesNotBlock()
        {
            VolleyballMatch match = OpponentAboutToSmash(out _);
            int blocked = 0;
            match.PlayerBlocked += () => blocked++;
            match.Player.PlaceAt(new Vector2(-.8f, match.OpponentAim.y));
            Assert.That(match.PressJump(), Is.True);

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.PlayerPoints, Is.Zero);
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
            Assert.That(blocked, Is.Zero);
        }

        [Test]
        public void StandingOnTheLineWithoutJumpingDoesNotBlock()
        {
            VolleyballMatch match = OpponentAboutToSmash(out float netCrossY);
            match.Player.PlaceAt(new Vector2(-.8f, netCrossY));
            Assert.That(match.PressAction().Kind, Is.EqualTo(ActionKind.None), "the hit button no longer blocks");

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.PlayerPoints, Is.Zero);
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
            Assert.That(MatchDriver.JumpSmash(match, Vector2.zero).Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(match.BallState, Is.EqualTo(BallState.Dead));
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
            Assert.That(match.PlayerPoints, Is.Zero);
        }

        [Test]
        public void SmashAimedAwayFromTheBlockGetsThrough()
        {
            VolleyballMatch match = PlayerReadyToSmashIntoABlock();

            // The block is now judged against where the smash crosses the net (close to the
            // player's near-net contact point here), not its landing spot, so the lateral swing
            // needed to clear the AI's 1 m reach is much smaller than the eventual landing spread
            // suggests. A short, wide aim clears it; confirm that precondition directly so the
            // test stays meaningful if the tuning constants ever change.
            // The AI lined up on the zero-stick line (no tick has passed since the set), so a
            // short wide smash to its far side must cross the net clear of its 1 m block.
            Vector2 stick = new[] { new Vector2(-.71f, .71f), new Vector2(-.71f, -.71f) }
                .OrderByDescending(s => Mathf.Abs(match.Opponent.Position.y -
                    ActionResolver.NetCrossY(match.BallGround, VolleyballMatch.SmashAim(s))))
                .First();
            Vector2 aim = VolleyballMatch.SmashAim(stick);
            float netCrossY = ActionResolver.NetCrossY(match.BallGround, aim);
            Assert.That(Mathf.Abs(match.Opponent.Position.y - netCrossY), Is.GreaterThan(ActionResolver.BlockLateral));

            Assert.That(MatchDriver.JumpSmash(match, stick).Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(match.BallState, Is.EqualTo(BallState.InPlay));
            Assert.That(match.Flight.Target, Is.EqualTo(aim));
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
