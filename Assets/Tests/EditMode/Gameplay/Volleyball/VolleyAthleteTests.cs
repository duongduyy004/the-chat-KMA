using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyAthleteTests
    {
        [Test]
        public void MovesAtItsSpeedAndReportsRunning()
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-5f, 0f));
            athlete.Move(Vector2.up, .2f);
            Assert.That(athlete.Position, Is.EqualTo(new Vector2(-5f, 1f)));
            Assert.That(athlete.Action, Is.EqualTo(AthleteAction.Run));
            athlete.Move(Vector2.zero, .2f);
            Assert.That(athlete.Action, Is.EqualTo(AthleteAction.Idle));
        }

        [Test]
        public void InputLongerThanOneIsClamped()
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-5f, 0f));
            athlete.Move(new Vector2(0f, 3f), .2f);
            Assert.That(athlete.Position.y, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void StaysOnItsOwnSideOfTheNet()
        {
            var player = new VolleyAthlete(CourtSide.Player, 5f);
            player.PlaceAt(new Vector2(-1f, 0f));
            player.Move(Vector2.right, 2f);
            Assert.That(player.Position.x, Is.EqualTo(-VolleyAthlete.NetGap));

            var opponent = new VolleyAthlete(CourtSide.Opponent, 5f);
            opponent.PlaceAt(new Vector2(-3f, 20f));
            Assert.That(opponent.Position, Is.EqualTo(new Vector2(VolleyAthlete.NetGap,
                CourtSpace.HalfWidth + VolleyAthlete.SideMargin)));
        }

        [Test]
        public void LockedAthleteCannotMoveUntilTheActionEnds()
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-5f, 0f));
            athlete.BeginAction(AthleteAction.Dive, .8f);
            athlete.Move(Vector2.up, .2f);
            Assert.That(athlete.Position, Is.EqualTo(new Vector2(-5f, 0f)));
            Assert.That(athlete.Action, Is.EqualTo(AthleteAction.Dive));

            athlete.Tick(.8f);
            Assert.That(athlete.IsLocked, Is.False);
            Assert.That(athlete.Action, Is.EqualTo(AthleteAction.Idle));
        }

        [Test]
        public void MoveTowardDoesNotOvershoot()
        {
            var athlete = new VolleyAthlete(CourtSide.Opponent, 5f);
            athlete.PlaceAt(new Vector2(5f, 0f));
            athlete.MoveToward(new Vector2(5f, .5f), 1f);
            Assert.That(athlete.Position.y, Is.EqualTo(.5f).Within(1e-4f));
        }

        [Test]
        public void LungeMovesAtMostTheGivenDistance()
        {
            var athlete = new VolleyAthlete(CourtSide.Player, 5f);
            athlete.PlaceAt(new Vector2(-5f, 0f));
            athlete.Lunge(new Vector2(-5f, 3f), 1.2f);
            Assert.That(athlete.Position.y, Is.EqualTo(1.2f).Within(1e-4f));
        }
    }
}
