using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballMatchTests
    {
        static VolleyballMatch FrozenOpponentMatch() => new VolleyballMatch(null, new OpponentTuning(0f, .25f));

        [Test]
        public void MatchStartsWithThePlayerHoldingTheServe()
        {
            var match = new VolleyballMatch();
            Assert.That(match.Server, Is.EqualTo(CourtSide.Player));
            Assert.That(match.BallState, Is.EqualTo(BallState.Held));
            Assert.That(match.Player.Position, Is.EqualTo(VolleyballMatch.PlayerServeSpot));
            Assert.That(match.Opponent.Position, Is.EqualTo(VolleyballMatch.OpponentReadySpot));
            Assert.That(match.BallGround, Is.EqualTo(VolleyballMatch.PlayerServeSpot));
            Assert.That(match.BallHeight, Is.EqualTo(VolleyballMatch.TossStartHeight));
            Assert.That(match.TimeRemaining, Is.EqualTo(VolleyballMatch.TimeLimit));
        }

        [Test]
        public void ServerMovesOnlyAlongTheBaseline()
        {
            var match = new VolleyballMatch();
            match.SetMove(new Vector2(1f, 1f));
            MatchDriver.Advance(match, .2f);
            Assert.That(match.Player.Position.x, Is.EqualTo(VolleyballMatch.PlayerServeSpot.x));
            Assert.That(match.Player.Position.y, Is.GreaterThan(0f));
        }

        [Test]
        public void PerfectServeFliesFastToTheAimedSpot()
        {
            var match = FrozenOpponentMatch();
            match.PressAction();
            Assert.That(match.BallState, Is.EqualTo(BallState.Toss));
            MatchDriver.AdvanceToFlightTime(match, match.Flight.ApexTime);
            match.SetMove(Vector2.left);
            ActionDecision serve = match.PressAction();

            Assert.That(serve.Kind, Is.EqualTo(ActionKind.ServeHit));
            Assert.That(serve.Grade, Is.EqualTo(TimingGrade.Perfect));
            Assert.That(match.BallState, Is.EqualTo(BallState.InPlay));
            Assert.That(match.Flight.Target, Is.EqualTo(new Vector2(3f, 0f)));
            Assert.That(match.Flight.ClearsNet, Is.True);
            Assert.That(match.Rally.Possession, Is.EqualTo(CourtSide.Opponent));
            Assert.That(match.TimedPresses, Is.EqualTo(1));
            Assert.That(match.QualitySum, Is.EqualTo(1f));
        }

        [Test]
        public void GoodServeIsTheSafeHighServe()
        {
            var match = FrozenOpponentMatch();
            ActionDecision serve = MatchDriver.ServeGood(match);
            Assert.That(serve.Grade, Is.EqualTo(TimingGrade.Good));
            Assert.That(match.Flight.Target, Is.EqualTo(new Vector2(5f, 0f)));
        }

        [Test]
        public void DroppedTossGivesThePointAndServeAway()
        {
            var match = new VolleyballMatch();
            match.PressAction();
            MatchDriver.Advance(match, match.Flight.Duration + .05f);
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
            Assert.That(match.Server, Is.EqualTo(CourtSide.Opponent));
            Assert.That(match.BallState, Is.EqualTo(BallState.Dead));
        }

        [Test]
        public void UnreturnedServeScoresForTheServer()
        {
            var match = FrozenOpponentMatch();
            Assert.That(MatchDriver.ServePerfectAce(match).Grade, Is.EqualTo(TimingGrade.Perfect));
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.PlayerPoints, Is.EqualTo(1));
            Assert.That(match.Winners, Is.Zero);
            Assert.That(match.Server, Is.EqualTo(CourtSide.Player));
        }

        [Test]
        public void OpponentServesItsPlanTargetAfterTheDelay()
        {
            var match = FrozenOpponentMatch();
            match.ForceServerForTest(CourtSide.Opponent);
            MatchDriver.Advance(match, VolleyballMatch.OpponentServeDelay + .01f);
            Assert.That(match.BallState, Is.EqualTo(BallState.Toss));
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 2f), Is.True);
            Assert.That(match.Flight.Target, Is.EqualTo(new Vector2(-7f, 0f)));
            Assert.That(match.Flight.Hitter, Is.EqualTo(CourtSide.Opponent));
            Assert.That(match.Plan.Index, Is.EqualTo(1));

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
        }

        [Test]
        public void NextPointStartsAfterThePause()
        {
            var match = FrozenOpponentMatch();
            match.ForceServerForTest(CourtSide.Opponent);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 6f), Is.True);
            MatchDriver.Advance(match, VolleyballMatch.PointPause + .01f);
            Assert.That(match.BallState, Is.EqualTo(BallState.Held));
            Assert.That(match.Opponent.Position, Is.EqualTo(VolleyballMatch.OpponentServeSpot));
            Assert.That(match.Player.Position, Is.EqualTo(VolleyballMatch.PlayerReadySpot));
        }

        [Test]
        public void ReceiveThenSmashWinsThePointAsAWinner()
        {
            var match = FrozenOpponentMatch();
            match.ForceServerForTest(CourtSide.Opponent);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 3f), Is.True);

            float receiveIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            Vector2 contact = match.Flight.GroundAt(receiveIdeal);
            match.Player.PlaceAt(contact);
            MatchDriver.AdvanceToFlightTime(match, receiveIdeal);
            ActionDecision receive = match.PressAction();
            Assert.That(receive.Kind, Is.EqualTo(ActionKind.Receive));
            Assert.That(receive.Grade, Is.EqualTo(TimingGrade.Perfect));
            Assert.That(match.Flight.Target, Is.EqualTo(new Vector2(-1.5f, Mathf.Clamp(contact.y, -3f, 3f))));
            Assert.That(match.Rally.Touches, Is.EqualTo(1));

            float smashIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(smashIdeal));
            MatchDriver.AdvanceToFlightTime(match, smashIdeal);
            match.SetMove(Vector2.right);
            ActionDecision smash = match.PressAction();
            match.SetMove(Vector2.zero);
            Assert.That(smash.Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(smash.Grade, Is.EqualTo(TimingGrade.Perfect));
            Assert.That(match.Flight.Target, Is.EqualTo(new Vector2(7f, 0f)));
            Assert.That(match.Rally.Possession, Is.EqualTo(CourtSide.Opponent));

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.PlayerPoints, Is.EqualTo(1));
            Assert.That(match.Winners, Is.EqualTo(1));
        }

        [Test]
        public void EarlyLateSmashDumpsTheBallOnTheOwnSide()
        {
            var match = FrozenOpponentMatch();
            match.ForceServerForTest(CourtSide.Opponent);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 3f), Is.True);
            float receiveIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(receiveIdeal));
            MatchDriver.AdvanceToFlightTime(match, receiveIdeal);
            match.PressAction();

            float smashIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(smashIdeal));
            MatchDriver.AdvanceToFlightTime(match, smashIdeal - .25f);
            ActionDecision smash = match.PressAction();
            Assert.That(smash.Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(smash.Grade, Is.EqualTo(TimingGrade.Late));
            Assert.That(CourtSpace.SideOf(match.Flight.Target), Is.EqualTo(CourtSide.Player));

            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.OpponentPoints, Is.EqualTo(1));
        }

        [Test]
        public void LateLatePressStillSmashesAsTheDocumentedSoftLob()
        {
            var match = FrozenOpponentMatch();
            match.ForceServerForTest(CourtSide.Opponent);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 3f), Is.True);
            float receiveIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(receiveIdeal));
            MatchDriver.AdvanceToFlightTime(match, receiveIdeal);
            match.PressAction();

            // Pressed +0.25 s past the ideal smash moment: still within the LATE window (<=0.30 s)
            // but, before the fix, HeightAt(pressTime) had already dropped below SmashMinHeight,
            // silently falling through instead of smashing.
            float smashIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(smashIdeal));
            MatchDriver.AdvanceToFlightTime(match, smashIdeal + .25f);
            match.SetMove(Vector2.right);
            ActionDecision smash = match.PressAction();
            match.SetMove(Vector2.zero);

            Assert.That(smash.Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(smash.Grade, Is.EqualTo(TimingGrade.Late));
            Assert.That(match.Flight.Target, Is.EqualTo(new Vector2(7f, 0f)));
            Assert.That(CourtSpace.SideOf(match.Flight.Target), Is.EqualTo(CourtSide.Opponent));
        }

        [Test]
        public void ThirdTouchDiveSendsAWeakFreeBallOverTheNetInsteadOfAlwaysLosingThePoint()
        {
            var match = FrozenOpponentMatch();
            match.ForceServerForTest(CourtSide.Opponent);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 3f), Is.True);

            // Touch 1: an early (LATE-graded) receive pushes the set well away from the net so
            // touch 2 cannot smash and instead becomes another kept receive.
            float receiveIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(receiveIdeal));
            MatchDriver.AdvanceToFlightTime(match, receiveIdeal - .25f);
            ActionDecision touch1 = match.PressAction();
            Assert.That(touch1.Kind, Is.EqualTo(ActionKind.Receive));
            Assert.That(touch1.Grade, Is.EqualTo(TimingGrade.Late));
            Assert.That(Mathf.Abs(match.Flight.Target.x), Is.GreaterThan(ActionResolver.SmashNetDistance));
            Assert.That(match.Rally.Touches, Is.EqualTo(1));

            // Touch 2: still too far from the net to smash, so it's kept as another receive,
            // reaching the third and final touch.
            float touch2Ideal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(touch2Ideal));
            MatchDriver.AdvanceToFlightTime(match, touch2Ideal);
            ActionDecision touch2 = match.PressAction();
            Assert.That(touch2.Kind, Is.EqualTo(ActionKind.Receive));
            Assert.That(match.Rally.Touches, Is.EqualTo(2));

            // Touch 3: out of clean reach with the ball dropping past 1 m - only a dive can save
            // it, and with no touches left it must legally cross the net.
            float diveIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            match.Player.PlaceAt(match.Flight.Target + new Vector2(0f, 1.6f));
            MatchDriver.AdvanceToFlightTime(match, diveIdeal + .05f);
            ActionDecision dive = match.PressAction();

            Assert.That(dive.Kind, Is.EqualTo(ActionKind.Dive));
            Assert.That(match.Rally.Touches, Is.EqualTo(0));
            Assert.That(match.Rally.Possession, Is.EqualTo(CourtSide.Opponent));
            Assert.That(CourtSpace.SideOf(match.Flight.Target), Is.EqualTo(CourtSide.Opponent));
        }

        [Test]
        public void OpponentReceivingASmashClearsTheWinnerCreditIfTheAiLosesThePointLater()
        {
            var match = FrozenOpponentMatch();
            match.ForceServerForTest(CourtSide.Opponent);
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.InPlay, 3f), Is.True);

            float receiveIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(receiveIdeal));
            MatchDriver.AdvanceToFlightTime(match, receiveIdeal);
            match.PressAction();

            float smashIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            match.Player.PlaceAt(match.Flight.GroundAt(smashIdeal));
            MatchDriver.AdvanceToFlightTime(match, smashIdeal);
            match.SetMove(Vector2.zero);
            ActionDecision smash = match.PressAction();
            match.SetMove(Vector2.zero);
            Assert.That(smash.Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(smash.Grade, Is.EqualTo(TimingGrade.Perfect));
            Assert.That(match.Rally.Possession, Is.EqualTo(CourtSide.Opponent));

            // The AI genuinely receives the smash (a real defensive touch) ...
            float aiReceiveIdeal = match.Flight.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            match.Opponent.PlaceAt(match.Flight.GroundAt(aiReceiveIdeal));
            MatchDriver.AdvanceToFlightTime(match, aiReceiveIdeal);
            Assert.That(match.Rally.Touches, Is.EqualTo(1));
            Assert.That(match.Rally.LastToucher, Is.EqualTo(CourtSide.Opponent));

            // ... but is frozen (speed 0) and never reaches its own follow-up set, so that set
            // simply lands untouched on the AI's own side - the player wins the point, but not
            // because of the original smash.
            Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
            Assert.That(match.PlayerPoints, Is.EqualTo(1));
            Assert.That(match.Winners, Is.Zero);
        }

        [Test]
        public void FirstToFiveCompletesOnceWithTheSpecScore()
        {
            var match = FrozenOpponentMatch();
            int completions = 0;
            match.Completed += () => completions++;

            for (int point = 0; point < VolleyballMatch.PointsToWin; point++)
            {
                Assert.That(match.Server, Is.EqualTo(CourtSide.Player), $"point {point + 1}");
                Assert.That(MatchDriver.ServePerfectAce(match).Grade, Is.EqualTo(TimingGrade.Perfect));
                Assert.That(MatchDriver.AdvanceUntil(match, () => match.BallState == BallState.Dead, 3f), Is.True);
                if (!match.IsOver)
                    MatchDriver.Advance(match, VolleyballMatch.PointPause + .02f);
            }

            Assert.That(match.PlayerPoints, Is.EqualTo(5));
            Assert.That(match.IsOver, Is.True);
            Assert.That(completions, Is.EqualTo(1));

            MinigameResult result = match.BuildResult();
            Assert.That(result.Pass, Is.True);
            Assert.That(result.Score, Is.EqualTo(9f));
            Assert.That(result.Rank, Is.EqualTo(Rank.S));
        }

        [Test]
        public void TimeCapWithALeadPasses()
        {
            var match = new VolleyballMatch();
            int completions = 0;
            match.Completed += () => completions++;
            match.SetScoreForTest(2, 1);
            MatchDriver.Advance(match, VolleyballMatch.TimeLimit + .5f);

            Assert.That(match.IsOver, Is.True);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(match.TimeRemaining, Is.Zero);
            Assert.That(match.BuildResult().Pass, Is.True);
        }

        [Test]
        public void TimeCapWithATieFails()
        {
            var match = new VolleyballMatch();
            match.SetScoreForTest(2, 2);
            MatchDriver.Advance(match, VolleyballMatch.TimeLimit + .5f);
            MinigameResult result = match.BuildResult();
            Assert.That(result.Pass, Is.False);
            Assert.That(result.Score, Is.Zero);
            Assert.That(result.Rank, Is.EqualTo(Rank.F));
        }

        [Test]
        public void FinishedMatchIgnoresInputAndTime()
        {
            var match = new VolleyballMatch();
            int completions = 0;
            match.Completed += () => completions++;
            match.SetScoreForTest(1, 0);
            MatchDriver.Advance(match, VolleyballMatch.TimeLimit + .5f);
            float elapsed = match.Elapsed;

            Assert.That(match.PressAction().Kind, Is.EqualTo(ActionKind.None));
            match.Tick(1f);
            Assert.That(match.Elapsed, Is.EqualTo(elapsed));
            Assert.That(completions, Is.EqualTo(1));
        }

        [Test]
        public void ZeroDeltaTimeChangesNothing()
        {
            var match = new VolleyballMatch();
            match.Tick(0f);
            Assert.That(match.Elapsed, Is.Zero);
        }

        [Test]
        public void ContactCueTracksTheTossApex()
        {
            var match = new VolleyballMatch();
            Assert.That(match.TryGetPlayerContactCue(out _), Is.False);
            match.PressAction();
            Assert.That(match.TryGetPlayerContactCue(out float seconds), Is.True);
            Assert.That(seconds, Is.EqualTo(match.Flight.ApexTime).Within(1e-4f));
        }

        [Test]
        public void PlayerActedReportsEveryResolvedPress()
        {
            var match = FrozenOpponentMatch();
            int acted = 0;
            match.PlayerActed += _ => acted++;
            match.PressAction();
            MatchDriver.Advance(match, .05f);
            match.PressAction();
            Assert.That(acted, Is.EqualTo(1));
        }

        [Test]
        public void AimMapsTheStickOntoTheOpponentHalf()
        {
            Assert.That(VolleyballMatch.AimAtOpponent(Vector2.zero), Is.EqualTo(new Vector2(7f, 0f)));
            Assert.That(VolleyballMatch.AimAtOpponent(new Vector2(-1f, 1f)), Is.EqualTo(new Vector2(3f, 3f)));
            Assert.That(VolleyballMatch.AimAtOpponent(new Vector2(1f, -1f)), Is.EqualTo(new Vector2(7f, -3f)));
        }
    }
}
