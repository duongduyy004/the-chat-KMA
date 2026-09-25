using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class RallyStateTests
    {
        [Test]
        public void ServeHandsPossessionToTheReceiver()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            rally.RegisterServe(CourtSide.Player);
            Assert.That(rally.Possession, Is.EqualTo(CourtSide.Opponent));
            Assert.That(rally.Touches, Is.Zero);
            Assert.That(rally.LastToucher, Is.EqualTo(CourtSide.Player));
        }

        [Test]
        public void FourthTouchOnOneSideIsAFault()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Opponent);
            rally.RegisterServe(CourtSide.Opponent);
            Assert.That(rally.RegisterTouch(CourtSide.Player, false), Is.EqualTo(TouchOutcome.Kept));
            Assert.That(rally.RegisterTouch(CourtSide.Player, false), Is.EqualTo(TouchOutcome.Kept));
            Assert.That(rally.RegisterTouch(CourtSide.Player, false), Is.EqualTo(TouchOutcome.Kept));
            Assert.That(rally.RegisterTouch(CourtSide.Player, false), Is.EqualTo(TouchOutcome.FourthTouchFault));
        }

        [Test]
        public void SendingOverResetsTouchesForTheOtherSide()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Opponent);
            rally.RegisterServe(CourtSide.Opponent);
            rally.RegisterTouch(CourtSide.Player, false);
            Assert.That(rally.RegisterTouch(CourtSide.Player, true), Is.EqualTo(TouchOutcome.SentOver));
            Assert.That(rally.Possession, Is.EqualTo(CourtSide.Opponent));
            Assert.That(rally.Touches, Is.Zero);
        }

        [Test]
        public void BallLandingInScoresForTheOtherSide()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            rally.RegisterServe(CourtSide.Player);
            Assert.That(rally.WinnerForLanding(new Vector2(6f, 2f)), Is.EqualTo(CourtSide.Player));
            Assert.That(rally.WinnerForLanding(new Vector2(-6f, 2f)), Is.EqualTo(CourtSide.Opponent));
            Assert.That(rally.WinnerForLanding(new Vector2(8f, 4f)), Is.EqualTo(CourtSide.Player));
        }

        [Test]
        public void BallLandingOutScoresAgainstTheLastToucher()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Player);
            rally.RegisterServe(CourtSide.Player);
            Assert.That(rally.WinnerForLanding(new Vector2(9f, 0f)), Is.EqualTo(CourtSide.Opponent));
            rally.RegisterTouch(CourtSide.Opponent, true);
            Assert.That(rally.WinnerForLanding(new Vector2(-9f, 0f)), Is.EqualTo(CourtSide.Player));
        }

        [Test]
        public void NetFaultScoresAgainstTheHitter()
        {
            var rally = new RallyState();
            rally.BeginServe(CourtSide.Opponent);
            rally.RegisterServe(CourtSide.Opponent);
            rally.RegisterTouch(CourtSide.Player, true);
            Assert.That(rally.WinnerForNetFault(), Is.EqualTo(CourtSide.Opponent));
        }
    }
}
