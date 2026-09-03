using System;
using NUnit.Framework;
using UnityEngine;
using KMA.Gameplay;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class SaveDataTests
    {
        [Test]
        public void SaveData_ContainsSevenRecordsAndSettings()
        {
            var data = SaveData.CreateDefault();
            var subjectIds = (SubjectId[])Enum.GetValues(typeof(SubjectId));

            Assert.That(subjectIds, Has.Length.EqualTo(7));
            Assert.That(SaveData.CurrentVersion, Is.EqualTo(2));
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(data.lives, Is.EqualTo(5));
            Assert.That(data.bossUnlocked, Is.False);
            Assert.That(data.gameCompleted, Is.False);
            Assert.That(data.hasActiveSubject, Is.False);
            Assert.That(data.visitAttempt, Is.EqualTo(1));
            Assert.That(data.awaitingPunishment, Is.False);
            Assert.That(data.subjects, Has.Length.EqualTo(subjectIds.Length));
            Assert.That(data.tutorialSeen, Has.Length.EqualTo(7));
            Assert.That(data.tutorialSeen, Is.All.False);

            for (int i = 0; i < subjectIds.Length; i++)
            {
                Assert.That(data.subjects[i], Is.Not.Null);
                Assert.That(data.subjects[i].id, Is.EqualTo(subjectIds[i]));
                Assert.That(data.subjects[i].passed, Is.False);
                Assert.That(data.subjects[i].bestScore, Is.EqualTo(0f));
                Assert.That(data.subjects[i].bestRank, Is.EqualTo(Rank.F));
                Assert.That(data.subjects[i].failedVisits, Is.EqualTo(0));
            }

            Assert.That(data.settings, Is.Not.Null);
            Assert.That(data.settings.musicVol, Is.EqualTo(1f));
            Assert.That(data.settings.sfxVol, Is.EqualTo(1f));
            Assert.That(data.settings.vibration, Is.True);
            Assert.That(data.settings.rhythmOffsetMs, Is.EqualTo(0f));
        }

        [Test]
        public void SaveData_JsonContainsPublicContractAndRoundTrips()
        {
            var json = JsonUtility.ToJson(SaveData.CreateDefault());

            StringAssert.Contains("\"version\":", json);
            StringAssert.Contains("\"lives\":", json);
            StringAssert.Contains("\"subjects\":", json);
            StringAssert.Contains("\"bossUnlocked\":", json);
            StringAssert.Contains("\"gameCompleted\":", json);
            StringAssert.Contains("\"hasActiveSubject\":", json);
            StringAssert.Contains("\"activeSubject\":", json);
            StringAssert.Contains("\"visitAttempt\":", json);
            StringAssert.Contains("\"awaitingPunishment\":", json);
            StringAssert.Contains("\"tutorialSeen\":", json);
            StringAssert.Contains("\"settings\":", json);
            StringAssert.Contains("\"id\":", json);
            StringAssert.Contains("\"passed\":", json);
            StringAssert.Contains("\"bestScore\":", json);
            StringAssert.Contains("\"bestRank\":", json);
            StringAssert.Contains("\"failedVisits\":", json);
            StringAssert.Contains("\"musicVol\":", json);
            StringAssert.Contains("\"sfxVol\":", json);
            StringAssert.Contains("\"vibration\":", json);
            StringAssert.Contains("\"rhythmOffsetMs\":", json);

            var restored = JsonUtility.FromJson<SaveData>(json);
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(restored.lives, Is.EqualTo(5));
            Assert.That(restored.subjects, Has.Length.EqualTo(7));
            Assert.That(restored.hasActiveSubject, Is.False);
            Assert.That(restored.visitAttempt, Is.EqualTo(1));
            Assert.That(restored.awaitingPunishment, Is.False);
            Assert.That(restored.tutorialSeen, Has.Length.EqualTo(7));
            Assert.That(restored.tutorialSeen, Is.All.False);
            Assert.That(restored.settings.musicVol, Is.EqualTo(1f));
            Assert.That(restored.settings.sfxVol, Is.EqualTo(1f));
            Assert.That(restored.settings.vibration, Is.True);
            Assert.That(restored.settings.rhythmOffsetMs, Is.EqualTo(0f));
        }
    }
}
