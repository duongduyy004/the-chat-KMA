using System.Collections.Generic;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    // Plays the authored plan with a deliberately sloppy bot that only uses what a player sees:
    // it reacts late, stops short of the contact ring and presses early. Such a player must keep
    // every ball the AI sends over off the floor, smashes included, and still get to attack.
    public sealed class AssistedRallyTests
    {
        const float ReactionSeconds = .3f;
        const float StopShort = 1.2f;
        // Early, not late: a receive's late side is cut short by the ball reaching the floor.
        const float EarlyPress = .2f;

        [Test]
        public void ASloppyRingFollowerKeepsEveryBallUpAndGetsToSmash()
        {
            var match = new VolleyballMatch(options: new VolleyballMatchOptions(0, 0f, true));
            var dropped = new List<string>();
            int aiSmashes = 0, playerSmashes = 0;
            match.TouchRegistered += (side, decision, _) =>
            {
                if (decision.Kind != ActionKind.Smash) return;
                if (side == CourtSide.Opponent) aiSmashes++;
                else playerSmashes++;
            };
            match.PointScored += winner =>
            {
                if (winner == CourtSide.Opponent && match.Rally.Possession == CourtSide.Player)
                    dropped.Add($"after {match.Rally.Touches} touches, last {match.LastDecision.Kind}");
            };

            for (float elapsed = 0f; elapsed < 240f; elapsed += MatchDriver.Step)
            {
                FollowTheRing(match);
                match.Tick(MatchDriver.Step);
            }

            Assert.That(dropped, Is.Empty, "Balls fell on the player's side: " + string.Join("; ", dropped));
            Assert.That(aiSmashes, Is.GreaterThan(0), "The bot should have faced (and dug) AI smashes.");
            Assert.That(playerSmashes, Is.GreaterThan(0), "The bot should have reached a set to smash.");
        }

        // The exam is the bar a casual player has to clear: the same sloppy bot, serving with a
        // merely GOOD toss, must reach five points before the AI does and before time runs out.
        [Test]
        public void ASloppyRingFollowerWinsTheExam()
        {
            var match = new VolleyballMatch(options: new VolleyballMatchOptions(VolleyballMatch.PointsToWin,
                VolleyballMatch.TimeLimit, requirePointsToWin: true));

            for (float elapsed = 0f; elapsed < 300f && !match.IsOver; elapsed += MatchDriver.Step)
            {
                if (match.Server == CourtSide.Player && match.BallState == BallState.Held)
                    match.PressAction();
                else if (match.Server == CourtSide.Player && match.BallState == BallState.Toss)
                {
                    if (match.FlightTime >= match.Flight.ApexTime + .15f)
                        match.PressAction();
                }
                else
                {
                    FollowTheRing(match);
                }
                match.Tick(MatchDriver.Step);
            }

            Assert.That(match.IsOver, Is.True);
            Assert.That(match.PlayerPoints, Is.EqualTo(VolleyballMatch.PointsToWin),
                $"Lost {match.PlayerPoints}:{match.OpponentPoints} after {match.Elapsed:F1}s");
            Assert.That(match.BuildResult().Pass, Is.True);
        }

        static void FollowTheRing(VolleyballMatch match)
        {
            if (match.BallState != BallState.InPlay || match.FlightTime < ReactionSeconds ||
                !match.TryGetPlayerContactCue(out float secondsToIdeal, out Vector2 contact))
            {
                match.SetMove(Vector2.zero);
                return;
            }

            Vector2 toContact = contact - match.Player.Position;
            match.SetMove(toContact.magnitude > StopShort ? toContact.normalized : Vector2.zero);
            if (secondsToIdeal <= EarlyPress)
                match.PressAction();
        }
    }
}
