using KMA.Gameplay.Celebration;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Celebration
{
    public sealed class CelebrationTimelineTests
    {
        int requested;
        CelebrationTimeline timeline;

        [SetUp]
        public void SetUp()
        {
            requested = 0;
            timeline = new CelebrationTimeline();
            timeline.SummaryRequested += () => requested++;
        }

        [Test]
        public void BeatsFollowTheScheduleAndTheSummaryOpensOnce()
        {
            Assert.That(timeline.Beat, Is.EqualTo(CelebrationBeat.Arrive));
            timeline.Tick(2.1f);
            Assert.That(timeline.Beat, Is.EqualTo(CelebrationBeat.Cheer));
            timeline.Tick(3f);
            Assert.That(timeline.Beat, Is.EqualTo(CelebrationBeat.Teacher));
            timeline.Tick(3f);
            Assert.That(timeline.Beat, Is.EqualTo(CelebrationBeat.Summary));
            timeline.Tick(10f);
            Assert.That(requested, Is.EqualTo(1));
            Assert.That(timeline.Time, Is.EqualTo(CelebrationTimeline.Duration));
        }

        [TestCase(0f)]
        [TestCase(10.9f)]
        public void SkipConvergesOnTheSameSummary(float at)
        {
            timeline.Tick(at);
            timeline.Skip();
            timeline.Skip();
            timeline.Tick(5f);
            Assert.That(timeline.SummaryShown, Is.True);
            Assert.That(timeline.Beat, Is.EqualTo(CelebrationBeat.Summary));
            Assert.That(requested, Is.EqualTo(1));
        }
    }
}
