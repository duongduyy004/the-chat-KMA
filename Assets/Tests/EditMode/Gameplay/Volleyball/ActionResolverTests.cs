using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class ActionResolverTests
    {
        static VolleyAthlete PlayerAt(Vector2 position)
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(position);
            return athlete;
        }

        static RallyState PlayerPossession(int touches)
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Opponent);
            rally.RegisterServe(CourtSide.Opponent);
            for (int i = 0; i < touches; i++)
                rally.RegisterTouch(CourtSide.Player, false);
            return rally;
        }

        static ActionDecision Resolve(VolleyAthlete athlete, BallState ball, BallFlight flight, float time,
            RallyState rally, CourtSide server = CourtSide.Opponent, bool tell = false, Vector2 aim = default) =>
            ActionResolver.Resolve(new ActionContext(athlete, ball, flight, time, rally, server, tell, aim));

        static BallFlight OpponentServe() =>
            new BallFlight(new Vector2(8.5f, 0f), 3.2f, new Vector2(-5f, 0f), 4.5f, CourtSide.Opponent);

        [Test]
        public void HeldBallTossesOnlyForTheServer()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            VolleyAthlete player = PlayerAt(new Vector2(-8.5f, 0f));
            Assert.That(Resolve(player, BallState.Held, null, 0f, rally, CourtSide.Player).Kind,
                Is.EqualTo(ActionKind.ServeToss));
            Assert.That(Resolve(player, BallState.Held, null, 0f, rally, CourtSide.Opponent).Kind,
                Is.EqualTo(ActionKind.None));
        }

        [TestCase(.1f, TimingGrade.Perfect)]
        [TestCase(.15f, TimingGrade.Good)]
        [TestCase(-.25f, TimingGrade.Late)]
        public void ServeHitIsGradedAgainstTheTossApex(float offset, TimingGrade expected)
        {
            var toss = new BallFlight(new Vector2(-8.5f, 0f), 1.2f, new Vector2(-8.5f, 0f), 3.2f, CourtSide.Player);
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            ActionDecision d = Resolve(PlayerAt(new Vector2(-8.5f, 0f)), BallState.Toss, toss,
                toss.ApexTime + offset, rally, CourtSide.Player);
            Assert.That(d.Kind, Is.EqualTo(ActionKind.ServeHit));
            Assert.That(d.Grade, Is.EqualTo(expected));
            Assert.That(d.IsTimed, Is.True);
        }

        [Test]
        public void ServeHitOutsideTheWindowDoesNothing()
        {
            var toss = new BallFlight(new Vector2(-8.5f, 0f), 1.2f, new Vector2(-8.5f, 0f), 3.2f, CourtSide.Player);
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            Assert.That(Resolve(PlayerAt(new Vector2(-8.5f, 0f)), BallState.Toss, toss, toss.ApexTime + .35f,
                rally, CourtSide.Player).Kind, Is.EqualTo(ActionKind.None));
        }

        [TestCase(0f, TimingGrade.Perfect)]
        [TestCase(.15f, TimingGrade.Good)]
        [TestCase(-.25f, TimingGrade.Late)]
        public void ReceiveIsGradedAgainstTheOneMetreContact(float offset, TimingGrade expected)
        {
            BallFlight serve = OpponentServe();
            float ideal = serve.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            ActionDecision d = Resolve(PlayerAt(serve.GroundAt(ideal)), BallState.InPlay, serve, ideal + offset,
                PlayerPossession(0));
            Assert.That(d.Kind, Is.EqualTo(ActionKind.Receive));
            Assert.That(d.Grade, Is.EqualTo(expected));
        }

        [Test]
        public void ReceiveNeedsTheAthleteWithinReachOfTheContactPoint()
        {
            BallFlight serve = OpponentServe();
            float ideal = serve.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            VolleyAthlete far = PlayerAt(serve.GroundAt(ideal) + new Vector2(0f, 1.5f));
            Assert.That(Resolve(far, BallState.InPlay, serve, ideal, PlayerPossession(0)).Kind,
                Is.EqualTo(ActionKind.None));
        }

        [Test]
        public void PressTooLateIsNotAReceive()
        {
            BallFlight serve = OpponentServe();
            float ideal = serve.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            Assert.That(Resolve(PlayerAt(serve.GroundAt(ideal)), BallState.InPlay, serve, ideal + .35f,
                PlayerPossession(0)).Kind, Is.Not.EqualTo(ActionKind.Receive));
        }

        [Test]
        public void DiveReachesALowBallLandingJustOutOfReach()
        {
            BallFlight serve = OpponentServe();
            float ideal = serve.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            VolleyAthlete athlete = PlayerAt(serve.Target + new Vector2(0f, 1.6f));
            ActionDecision d = Resolve(athlete, BallState.InPlay, serve, ideal + .05f, PlayerPossession(0));
            Assert.That(d.Kind, Is.EqualTo(ActionKind.Dive));
            Assert.That(d.Grade, Is.EqualTo(TimingGrade.Late));
        }

        [Test]
        public void SecondTouchNearTheNetSmashes()
        {
            var set = new BallFlight(new Vector2(-5f, 0f), 1f, new Vector2(-1.5f, 0f), 4f, CourtSide.Player);
            float ideal = set.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            ActionDecision d = Resolve(PlayerAt(set.GroundAt(ideal)), BallState.InPlay, set, ideal, PlayerPossession(1));
            Assert.That(d.Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(d.Grade, Is.EqualTo(TimingGrade.Perfect));
        }

        [Test]
        public void AGoodLatePressStillSmashesInsteadOfFallingThroughToReceive()
        {
            // The height gate must be judged at the fixed ideal moment (2.6 m), not at the actual
            // press time: the ball drops below the 2.2 m minimum only ~0.065 s after the ideal
            // moment, so a +0.15 s press (well inside the GOOD window) must still smash.
            var set = new BallFlight(new Vector2(-5f, 0f), 1f, new Vector2(-1.5f, 0f), 4f, CourtSide.Player);
            float ideal = set.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            ActionDecision d = Resolve(PlayerAt(set.GroundAt(ideal)), BallState.InPlay, set, ideal + .15f,
                PlayerPossession(1));
            Assert.That(d.Kind, Is.EqualTo(ActionKind.Smash));
            Assert.That(d.Grade, Is.EqualTo(TimingGrade.Good));
        }

        [Test]
        public void FirstTouchCannotSmash()
        {
            var set = new BallFlight(new Vector2(-5f, 0f), 1f, new Vector2(-1.5f, 0f), 4f, CourtSide.Player);
            float ideal = set.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            Assert.That(Resolve(PlayerAt(set.GroundAt(ideal)), BallState.InPlay, set, ideal, PlayerPossession(0)).Kind,
                Is.EqualTo(ActionKind.Receive));
        }

        [Test]
        public void FarFromTheNetTheSecondTouchIsAReceive()
        {
            var set = new BallFlight(new Vector2(-6f, 0f), 1f, new Vector2(-4.5f, 0f), 4f, CourtSide.Player);
            float ideal = set.TimeAtHeightDescending(ActionResolver.SmashContactHeight);
            Assert.That(Resolve(PlayerAt(set.GroundAt(ideal)), BallState.InPlay, set, ideal, PlayerPossession(1)).Kind,
                Is.EqualTo(ActionKind.Receive));
        }

        [Test]
        public void ThirdTouchAwayFromTheNetIsAFreeBall()
        {
            var set = new BallFlight(new Vector2(-6f, 0f), 1f, new Vector2(-4.5f, 0f), 4f, CourtSide.Player);
            float ideal = set.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            ActionDecision d = Resolve(PlayerAt(set.GroundAt(ideal)), BallState.InPlay, set, ideal, PlayerPossession(2));
            Assert.That(d.Kind, Is.EqualTo(ActionKind.FreeBall));
            Assert.That(d.Grade, Is.EqualTo(TimingGrade.Perfect));
        }

        [Test]
        public void BlockNeedsTheTellTheNetAndTheLine()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            rally.RegisterServe(CourtSide.Player);
            var attack = new BallFlight(new Vector2(4f, 0f), 1f, new Vector2(1.5f, 0f), 4f, CourtSide.Opponent);
            var aim = new Vector2(-7f, -3f);
            // The block is judged against where the smash would cross the net (x=0), not its
            // landing spot (aim.y).
            float netCrossY = ActionResolver.NetCrossY(attack.GroundAt(.5f), aim);

            Assert.That(Resolve(PlayerAt(new Vector2(-.8f, netCrossY)), BallState.InPlay, attack, .5f, rally,
                CourtSide.Player, true, aim).Kind, Is.EqualTo(ActionKind.Block));
            Assert.That(Resolve(PlayerAt(new Vector2(-.8f, netCrossY + 2f)), BallState.InPlay, attack, .5f, rally,
                CourtSide.Player, true, aim).Kind, Is.EqualTo(ActionKind.None));
            Assert.That(Resolve(PlayerAt(new Vector2(-2.5f, netCrossY)), BallState.InPlay, attack, .5f, rally,
                CourtSide.Player, true, aim).Kind, Is.EqualTo(ActionKind.None));
            Assert.That(Resolve(PlayerAt(new Vector2(-.8f, netCrossY)), BallState.InPlay, attack, .5f, rally,
                CourtSide.Player, false, aim).Kind, Is.EqualTo(ActionKind.None));

            // The old (pre-fix) landing-spot position must now fail: it isn't where the ball
            // actually crosses the net.
            Assert.That(Resolve(PlayerAt(new Vector2(-.8f, aim.y)), BallState.InPlay, attack, .5f, rally,
                CourtSide.Player, true, aim).Kind, Is.EqualTo(ActionKind.None));
        }

        [Test]
        public void LockedAthleteCannotAct()
        {
            BallFlight serve = OpponentServe();
            float ideal = serve.TimeAtHeightDescending(ActionResolver.ReceiveContactHeight);
            VolleyAthlete athlete = PlayerAt(serve.GroundAt(ideal));
            athlete.BeginAction(AthleteAction.Dive, .8f);
            Assert.That(Resolve(athlete, BallState.InPlay, serve, ideal, PlayerPossession(0)).Kind,
                Is.EqualTo(ActionKind.None));
        }

        [Test]
        public void DeadBallIgnoresPresses()
        {
            BallFlight serve = OpponentServe();
            Assert.That(Resolve(PlayerAt(serve.Target), BallState.Dead, serve, 1f, PlayerPossession(0)).Kind,
                Is.EqualTo(ActionKind.None));
        }
    }
}
