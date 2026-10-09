using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballChallengeTests
    {
        const float Step = 1f / 60f;

        [Test]
        public void LearnCountsThreeReceivesEvenAcrossRallies()
        {
            var rules = new VolleyballChallengeRules(ChallengeCatalog.LoadDefault().Get("volleyball_learn"));
            Assert.That(rules.Match.WinningPoints, Is.EqualTo(0));
            Assert.That(rules.Match.ClockLimit, Is.EqualTo(0f));
            Assert.That(rules.CompletedTargets, Is.EqualTo(0));
        }

        [Test]
        public void PracticeHasNoClockAndFailsOnceTheOpponentReachesFivePoints()
        {
            var rules = new VolleyballChallengeRules(ChallengeCatalog.LoadDefault().Get("volleyball_practice"));
            Assert.That(rules.Match.ClockLimit, Is.EqualTo(0f));

            // An idle player hands every rally to the opponent.
            for (float t = 0f; t < 300f && !rules.IsComplete; t += Step) rules.Tick(Step);

            Assert.That(rules.IsComplete, Is.True);
            Assert.That(rules.Match.OpponentPoints, Is.EqualTo(VolleyballMatch.PointsToWin));
            Assert.That(rules.Match.Elapsed, Is.LessThan(120f), "The match ends on points, not on a clock.");
            Assert.That(rules.BuildResult(new ChallengeAttemptContext("attempt", "volleyball_practice",
                ChallengeAttemptMode.Journey, ChallengeDifficulty.Easy)).Pass, Is.False);
        }

        [Test]
        public void ExamHasNoClockAndEndsWhenEitherSideReachesFivePoints()
        {
            var rules = new VolleyballChallengeRules(ChallengeCatalog.LoadDefault().Get("volleyball_exam"));
            Assert.That(rules.Match.WinningPoints, Is.EqualTo(5));
            Assert.That(rules.Match.ClockLimit, Is.EqualTo(0f));

            // The player serves first; standing still must not hold the ball until a deadline.
            for (float t = 0f; t < 300f && !rules.IsComplete; t += Step) rules.Tick(Step);

            Assert.That(rules.Match.IsOver, Is.True);
            Assert.That(rules.Match.OpponentPoints, Is.EqualTo(VolleyballMatch.PointsToWin));
            Assert.That(rules.Match.Elapsed, Is.LessThan(120f), "The match ends on points, not on a clock.");
            Assert.That(rules.BuildResult(new ChallengeAttemptContext("attempt", "volleyball_exam",
                ChallengeAttemptMode.Journey, ChallengeDifficulty.Normal)).Pass, Is.False);
        }

        [Test]
        public void AnUnplayedPlayerServeTossesItselfAndCostsThePoint()
        {
            var match = new VolleyballMatch(options: new VolleyballMatchOptions(5, 0f));
            Assert.That(match.Server, Is.EqualTo(CourtSide.Player));
            for (float t = 0f; t < VolleyballMatch.PlayerServeTimeout - .1f; t += Step) match.Tick(Step);
            Assert.That(match.BallState, Is.EqualTo(BallState.Held), "The player gets time to serve.");

            for (float t = 0f; t < 10f && match.OpponentPoints == 0; t += Step) match.Tick(Step);
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
        }

        [Test]
        public void PracticeCountsReceiveReceiveJumpSmash()
        {
            var rules = new VolleyballChallengeRules(ChallengeCatalog.LoadDefault().Get("volleyball_practice"));
            VolleyballMatch match = rules.Match;
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 3f), Is.True);
            for (int touch = 0; touch < 2; touch++)
            {
                float ideal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
                match.Player.PlaceAt(match.Flight.GroundAt(ideal));
                MatchDriver.AdvanceToFlightTime(match, ideal);
                Assert.That(rules.PressAction().Kind, Is.EqualTo(ActionKind.Receive), $"touch {touch + 1}");
            }

            float smashIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(smashIdeal));
            MatchDriver.AdvanceToFlightTime(match, smashIdeal);
            Assert.That(rules.PressJump(), Is.True);
            rules.SetMove(Vector2.up);
            Assert.That(rules.PressAction().Kind, Is.EqualTo(ActionKind.Smash));
            rules.SetMove(Vector2.zero);

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.PlayerPoints, Is.EqualTo(1));
            Assert.That(rules.CompletedTargets, Is.EqualTo(1));
        }
    }
}
