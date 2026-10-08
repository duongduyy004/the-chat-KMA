using System;
using System.Linq;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEditor;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class RivalPaceProfileTests
    {
        const float MinSpeed = SprintController.DefaultMinRivalSpeed;
        const float MaxSpeed = SprintController.DefaultMaxRivalSpeed;
        const float FastestFinish = 12.9f;
        const float SlowestFinish = 18.5f;

        // Each race rolls the rivals' pace, but never so fast that a rival runs away from a
        // passing player, nor so slow that one is still crawling long after the field.
        [Test]
        public void RolledAuthoredRivalsStayWithinTheSpeedRangeAndFinishWindow()
        {
            RivalPaceProfile[] authored = LoadAuthoredProfiles();
            for (int seed = 0; seed < 200; seed++)
            {
                var random = new Random(seed);
                foreach (RivalPaceProfile profile in authored)
                {
                    RivalPaceProfile rolled = RivalPaceRandomizer.Roll(profile, MinSpeed, MaxSpeed, random);
                    Assert.That(rolled.SustainedSpeed, Is.InRange(MinSpeed, MaxSpeed));
                    Assert.That(rolled.Name, Is.EqualTo(profile.Name));
                    float finish = FinishTime(rolled);
                    Assert.That(finish, Is.InRange(FastestFinish, SlowestFinish), profile.Name + " seed " + seed);
                }
            }
        }

        [Test]
        public void ExtremeRollsHitTheFinishWindowEdges()
        {
            RivalPaceProfile[] authored = LoadAuthoredProfiles();
            float fastest = authored.Min(profile => FinishTime(AtSpeed(profile, MaxSpeed)));
            float slowest = authored.Max(profile => FinishTime(AtSpeed(profile, MinSpeed)));

            // A rival may occasionally beat the 14 s exam deadline, but only just.
            Assert.That(fastest, Is.InRange(FastestFinish, 14f));
            Assert.That(slowest, Is.InRange(16f, SlowestFinish));
        }

        [Test]
        public void RollKeepsTheProfileShape()
        {
            var fastStart = new RivalPaceProfile("Fast Start", 9f, 6f);
            RivalPaceProfile rolled = RivalPaceRandomizer.Roll(fastStart, 5f, 7f, new Random(3));

            Assert.That(rolled.OpeningSpeed / rolled.SustainedSpeed, Is.EqualTo(1.5f).Within(1e-4f));
        }

        [Test]
        public void SameSeedRollsTheSamePace()
        {
            var profile = new RivalPaceProfile("Steady", 6f, 6f);
            RivalPaceProfile a = RivalPaceRandomizer.Roll(profile, 5f, 7f, new Random(42));
            RivalPaceProfile b = RivalPaceRandomizer.Roll(profile, 5f, 7f, new Random(42));

            Assert.That(a.SustainedSpeed, Is.EqualTo(b.SustainedSpeed));
            Assert.That(a.OpeningSpeed, Is.EqualTo(b.OpeningSpeed));
        }

        [Test]
        public void InvertedRangeIsSwappedAndNullProfileStaysNull()
        {
            var profile = new RivalPaceProfile("Steady", 6f, 6f);
            RivalPaceProfile rolled = RivalPaceRandomizer.Roll(profile, 7f, 5f, new Random(1));

            Assert.That(rolled.SustainedSpeed, Is.InRange(5f, 7f));
            Assert.That(RivalPaceRandomizer.Roll(null, 5f, 7f, new Random(1)), Is.Null);
        }

        static RivalPaceProfile[] LoadAuthoredProfiles() => SprintRivalMappings.Required
            .Select(mapping => AssetDatabase.LoadAssetAtPath<RivalPaceProfileAsset>(mapping.ProfilePath))
            .Select(asset =>
            {
                Assert.That(asset, Is.Not.Null);
                return asset.ToRuntime();
            })
            .ToArray();

        static RivalPaceProfile AtSpeed(RivalPaceProfile profile, float sustained) =>
            RivalPaceRandomizer.Roll(profile, sustained, sustained, new Random(0));

        static float FinishTime(RivalPaceProfile profile)
        {
            var rules = new SprintRules(60f, new[] { profile });
            while (rules.GetRivalDistance(0) < 100f && rules.Elapsed < 60f)
                rules.Tick(1f / 120f);
            return rules.Elapsed;
        }
    }
}
