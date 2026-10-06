#if UNITY_EDITOR
using KMA.EditorTools;
using NUnit.Framework;

namespace KMA.Tests.EditorTools
{
    public sealed class PlayModeScreenshotGameViewSizeTests
    {
        [TestCase("2400x1080", 2400, 1080)]
        [TestCase("1920x1080", 1920, 1080)]
        [TestCase(" 2400X1080 ", 2400, 1080)]
        public void ParsesAFixedResolution(string value, int width, int height)
        {
            Assert.That(PlayModeScreenshot.TryParseGameViewSize(value, out int w, out int h), Is.True);
            Assert.That(w, Is.EqualTo(width));
            Assert.That(h, Is.EqualTo(height));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("2400")]
        [TestCase("2400x")]
        [TestCase("0x1080")]
        [TestCase("wide")]
        [TestCase("2400x1080x2")]
        public void RejectsAnythingElse(string value)
        {
            Assert.That(PlayModeScreenshot.TryParseGameViewSize(value, out _, out _), Is.False);
        }
    }
}
#endif
