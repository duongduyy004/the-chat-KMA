using System.Collections.Generic;
using System.IO;
using System.Linq;
using KMA.EditorTools;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEditor;

namespace KMA.Tests.EditorTools
{
    public sealed class StudentJourneyContentBuilderTests
    {
        [Test]
        public void RegeneratedFootballLessonsKeepNormalAndLearnKeepsKeeperOff()
        {
            ChallengeCatalog catalog = ChallengeCatalog.LoadDefault();
            var snapshots = catalog.Ordered.Select(AssetDatabase.GetAssetPath)
                .Append(AssetDatabase.GetAssetPath(catalog))
                .ToDictionary(path => path, File.ReadAllBytes);
            try
            {
                StudentJourneyContentBuilder.BuildChallenges();
                foreach (ChallengeDefinition lesson in catalog.Ordered.Where(item => item.Subject == SubjectId.Football))
                    Assert.That(lesson.Difficulty, Is.EqualTo(ChallengeDifficulty.Normal), lesson.Id);
                Assert.That(catalog.Get("soccer_learn").KeeperEnabled, Is.False);
                Assert.That(catalog.Get("soccer_practice").KeeperEnabled, Is.True);
                Assert.That(catalog.Get("soccer_exam").KeeperEnabled, Is.True);
                Assert.That(catalog.Get("volleyball_learn").Difficulty, Is.EqualTo(ChallengeDifficulty.Easy));
            }
            finally
            {
                foreach (KeyValuePair<string, byte[]> snapshot in snapshots)
                {
                    File.WriteAllBytes(snapshot.Key, snapshot.Value);
                    AssetDatabase.ImportAsset(snapshot.Key, ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
}
