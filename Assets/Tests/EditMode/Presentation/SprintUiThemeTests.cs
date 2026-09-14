using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class SprintUiThemeTests
    {
        [Test]
        public void EveryFontStep_MeetsTheMinimumSize()
        {
            foreach (float size in SprintUiTheme.AllFontSizes())
                Assert.That(size, Is.GreaterThanOrEqualTo(SprintUiTheme.MinimumFontSize),
                    $"Font step {size} is below the {SprintUiTheme.MinimumFontSize} floor.");
        }

        [Test]
        public void EveryTextColor_ReachesReadableContrastOnSurface()
        {
            foreach (Color color in SprintUiTheme.TextColors())
                Assert.That(SprintUiTheme.ContrastRatio(color, SprintUiTheme.Surface),
                    Is.GreaterThanOrEqualTo(4.5f), $"Colour {color} is unreadable on Surface.");
        }

        [Test]
        public void ContrastRatio_IsSymmetricAndBoundedByTheWcagRange()
        {
            float forward = SprintUiTheme.ContrastRatio(Color.white, Color.black);
            float reverse = SprintUiTheme.ContrastRatio(Color.black, Color.white);
            Assert.That(forward, Is.EqualTo(reverse).Within(.001f));
            Assert.That(forward, Is.EqualTo(21f).Within(.05f));
            Assert.That(SprintUiTheme.ContrastRatio(Color.white, Color.white), Is.EqualTo(1f).Within(.001f));
        }

        [Test]
        public void PlayerAccent_IsDistinctFromEveryOtherToken()
        {
            Assert.That(SprintUiTheme.Player, Is.Not.EqualTo(SprintUiTheme.Accent));
            Assert.That(SprintUiTheme.Player, Is.Not.EqualTo(SprintUiTheme.Energy));
            Assert.That(SprintUiTheme.Player, Is.Not.EqualTo(SprintUiTheme.TextPrimary));
        }

        [Test]
        public void WithAlpha_ReplacesAlphaAndClamps()
        {
            Assert.That(SprintUiTheme.WithAlpha(SprintUiTheme.Surface, .42f).a, Is.EqualTo(.42f).Within(.001f));
            Assert.That(SprintUiTheme.WithAlpha(SprintUiTheme.Surface, 2f).a, Is.EqualTo(1f).Within(.001f));
            Assert.That(SprintUiTheme.WithAlpha(SprintUiTheme.Surface, -1f).a, Is.EqualTo(0f).Within(.001f));
        }
    }
}
