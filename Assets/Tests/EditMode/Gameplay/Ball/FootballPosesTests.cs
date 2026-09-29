using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class FootballPosesTests
    {
        [Test]
        public void KickerRunsUpStrikesAndCelebratesOnlyGoals()
        {
            float kick = FootballRules.KickAnimationSeconds;
            Assert.That(FootballPoses.Kicker(FootballState.Start, 0f, null), Is.EqualTo(KickerPose.Ready));
            Assert.That(FootballPoses.Kicker(FootballState.Aiming, 0f, null), Is.EqualTo(KickerPose.Ready));
            Assert.That(FootballPoses.Kicker(FootballState.Charging, .4f, null), Is.EqualTo(KickerPose.Ready));
            Assert.That(FootballPoses.Kicker(FootballState.Kicking, kick * .25f, null), Is.EqualTo(KickerPose.RunUp));
            Assert.That(FootballPoses.Kicker(FootballState.Kicking, kick * .75f, null), Is.EqualTo(KickerPose.Strike));
            Assert.That(FootballPoses.Kicker(FootballState.Flying, .2f, null), Is.EqualTo(KickerPose.Strike));
            Assert.That(FootballPoses.Kicker(FootballState.Flying, .6f, FootballOutcome.Goal), Is.EqualTo(KickerPose.Celebrate));
            Assert.That(FootballPoses.Kicker(FootballState.ShotResult, .1f, FootballOutcome.Goal), Is.EqualTo(KickerPose.Celebrate));
            foreach (var miss in new[] { FootballOutcome.Saved, FootballOutcome.Wide, FootballOutcome.High,
                         FootballOutcome.Short, FootballOutcome.Post, FootballOutcome.Crossbar })
                Assert.That(FootballPoses.Kicker(FootballState.ShotResult, .1f, miss), Is.EqualTo(KickerPose.Ready), miss.ToString());
        }

        [Test]
        public void KeeperHoldsSavesAndIsBeatenOnlyByGoals()
        {
            Assert.That(FootballPoses.Keeper(null), Is.EqualTo(KeeperPose.Ready));
            Assert.That(FootballPoses.Keeper(FootballOutcome.Saved), Is.EqualTo(KeeperPose.Save));
            Assert.That(FootballPoses.Keeper(FootballOutcome.Goal), Is.EqualTo(KeeperPose.Beaten));
            Assert.That(FootballPoses.Keeper(FootballOutcome.Post), Is.EqualTo(KeeperPose.Ready));
        }

        [Test]
        public void PoseSpritesMapEveryPose()
        {
            Sprite Make(string name) { var s = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero); s.name = name; return s; }
            var set = new FootballPoseSprites
            {
                kickerReady = Make("kr"), kickerRunUp = Make("ku"), kickerStrike = Make("ks"), kickerCelebrate = Make("kc"),
                keeperReady = Make("gr"), keeperSave = Make("gs"), keeperBeaten = Make("gb")
            };
            Assert.That(set.For(KickerPose.Ready), Is.SameAs(set.kickerReady));
            Assert.That(set.For(KickerPose.RunUp), Is.SameAs(set.kickerRunUp));
            Assert.That(set.For(KickerPose.Strike), Is.SameAs(set.kickerStrike));
            Assert.That(set.For(KickerPose.Celebrate), Is.SameAs(set.kickerCelebrate));
            Assert.That(set.For(KeeperPose.Ready), Is.SameAs(set.keeperReady));
            Assert.That(set.For(KeeperPose.Save), Is.SameAs(set.keeperSave));
            Assert.That(set.For(KeeperPose.Beaten), Is.SameAs(set.keeperBeaten));
        }

        [Test]
        public void KeeperDiveRotatesAboutTheHip()
        {
            float drop = FootballPresentation.KeeperFeetDrop * FootballPresentation.PixelToWorld;
            Vector3 hip = FootballPresentation.ScreenToWorld(600f, FootballFlightSimulation.KeeperHipY);
            Vector3 upright = FootballPresentation.KeeperWorldPosition(0f, 0f);
            Assert.That(upright.x, Is.EqualTo(hip.x).Within(1e-5f));
            Assert.That(hip.y - upright.y, Is.EqualTo(drop).Within(1e-5f), "Feet stand below the hip.");
            foreach (float angle in new[] { -45f, -20f, 30f, 45f })
            {
                Vector3 feet = FootballPresentation.KeeperWorldPosition(0f, angle);
                Vector3 backToHip = feet + Quaternion.Euler(0f, 0f, -angle) * new Vector3(0f, drop, 0f);
                Assert.That(Vector3.Distance(backToHip, hip), Is.LessThan(1e-5f), "angle " + angle);
            }
            Assert.That(FootballPresentation.KeeperWorldPosition(1f, 0f).x, Is.GreaterThan(upright.x));
        }
    }
}
