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
