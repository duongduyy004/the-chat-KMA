using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class JourneyDialogueDataTests
    {
        [Test]
        public void DefaultDialogueLibraryContainsShortNodesForEveryMilestone()
        {
            JourneyDialogueLibrary library = JourneyDialogueLibrary.LoadDefault();
            string[] ids = { "opening", "sprint_intro", "sprint_exam", "sprint_pass", "volleyball_intro",
                "volleyball_exam", "volleyball_pass", "soccer_intro", "soccer_exam", "soccer_pass",
                "supplementary", "course_complete" };
            foreach (string id in ids)
            {
                Assert.That(library.Get(id).Lines.Count, Is.InRange(2, 4), id);
                foreach (JourneyDialogueLine line in library.Get(id).Lines)
                    Assert.That(line.Portrait, Is.Not.Null, $"{id}/{line.SpeakerRole}");
            }
        }

        [Test]
        public void SeenMarkersAreSavedAndRestoredWithJourneyProgress()
        {
            var journey = new JourneyProgress(ChallengeCatalog.LoadDefault());
            Assert.That(journey.IsDialogueSeen("opening"), Is.False);
            journey.MarkDialogueSeen("opening");
            Assert.That(journey.IsDialogueSeen("opening"), Is.True);
            var restored = new JourneyProgress(ChallengeCatalog.LoadDefault());
            restored.Restore(journey.ToData(), GameSession.MaxLives);
            Assert.That(restored.IsDialogueSeen("opening"), Is.True);
        }
    }
}
