using System.Linq;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEditor;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class RivalPaceProfileTests
    {
        const float ExamDeadline = 14f;
        const float MinFinishGap = 1f;

        // Anyone who passes the exam beats every rival, and the rivals finish clearly apart
        // rather than in a bunch.
        [Test]
        public void AuthoredRivalsFinishAfterTheExamDeadlineAndSpreadOut()
        {
            float[] finishes = SprintRivalMappings.Required
                .Select(mapping => AssetDatabase.LoadAssetAtPath<RivalPaceProfileAsset>(mapping.ProfilePath))
                .Select(asset =>
                {
                    Assert.That(asset, Is.Not.Null);
                    return FinishTime(asset.ToRuntime());
                })
                .OrderBy(time => time)
                .ToArray();

            Assert.That(finishes[0], Is.GreaterThan(ExamDeadline), string.Join(", ", finishes));
            for (int i = 1; i < finishes.Length; i++)
                Assert.That(finishes[i] - finishes[i - 1], Is.GreaterThanOrEqualTo(MinFinishGap),
                    string.Join(", ", finishes));
        }

        static float FinishTime(RivalPaceProfile profile)
        {
            var rules = new SprintRules(60f, new[] { profile });
            while (rules.GetRivalDistance(0) < 100f && rules.Elapsed < 60f)
                rules.Tick(1f / 120f);
            return rules.Elapsed;
        }
    }
}
