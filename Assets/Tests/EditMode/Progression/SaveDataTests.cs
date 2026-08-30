using NUnit.Framework;
using KMA.Gameplay;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class SaveDataTests
    {
        [Test]
        public void SaveData_ContainsSevenRecordsAndSettings()
        {
            var data = SaveData.CreateDefault();
            Assert.That(data.subjects, Has.Length.EqualTo(7));
            Assert.That(data.settings, Is.Not.Null);
            Assert.That(data.tutorialSeen, Has.Length.EqualTo(7));
        }
    }
}
