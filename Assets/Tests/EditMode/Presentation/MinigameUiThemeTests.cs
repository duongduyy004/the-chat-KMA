using KMA.Gameplay.UI;
using KMA.UI.Kit;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class MinigameUiThemeTests
    {
        [Test]
        public void EveryFontStep_MeetsTheMinimumSize()
        {
            foreach (float size in MinigameUiTheme.AllFontSizes())
                Assert.That(size, Is.GreaterThanOrEqualTo(MinigameUiTheme.MinimumFontSize),
                    $"Font step {size} is below the {MinigameUiTheme.MinimumFontSize} floor.");
        }

        [Test]
        public void EveryTextColor_ReachesReadableContrastOnSurface()
        {
            foreach (Color color in MinigameUiTheme.TextColors())
                Assert.That(MinigameUiTheme.ContrastRatio(color, MinigameUiTheme.Surface),
                    Is.GreaterThanOrEqualTo(4.5f), $"Colour {color} is unreadable on Surface.");
        }

        [Test]
        public void SurfaceText_IsReadableOnTheAccentAndEnergyFaces()
        {
            Assert.That(MinigameUiTheme.ContrastRatio(MinigameUiTheme.Surface, MinigameUiTheme.Accent),
                Is.GreaterThanOrEqualTo(4.5f), "Primary button text");
            Assert.That(MinigameUiTheme.ContrastRatio(MinigameUiTheme.Surface, MinigameUiTheme.Energy),
                Is.GreaterThanOrEqualTo(4.5f), "Danger button text");
        }

        [Test]
        public void ContrastRatio_IsSymmetricAndBoundedByTheWcagRange()
        {
            float forward = MinigameUiTheme.ContrastRatio(Color.white, Color.black);
            float reverse = MinigameUiTheme.ContrastRatio(Color.black, Color.white);
            Assert.That(forward, Is.EqualTo(reverse).Within(.001f));
            Assert.That(forward, Is.EqualTo(21f).Within(.05f));
            Assert.That(MinigameUiTheme.ContrastRatio(Color.white, Color.white), Is.EqualTo(1f).Within(.001f));
        }

        [Test]
        public void PlayerAccent_IsDistinctFromEveryOtherToken()
        {
            Assert.That(MinigameUiTheme.Player, Is.Not.EqualTo(MinigameUiTheme.Accent));
            Assert.That(MinigameUiTheme.Player, Is.Not.EqualTo(MinigameUiTheme.Energy));
            Assert.That(MinigameUiTheme.Player, Is.Not.EqualTo(MinigameUiTheme.TextPrimary));
            Assert.That(MinigameUiTheme.Player, Is.Not.EqualTo(MinigameUiTheme.Success));
        }

        [Test]
        public void WithAlpha_ReplacesAlphaAndClamps()
        {
            Assert.That(MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, .42f).a, Is.EqualTo(.42f).Within(.001f));
            Assert.That(MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, 2f).a, Is.EqualTo(1f).Within(.001f));
            Assert.That(MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, -1f).a, Is.EqualTo(0f).Within(.001f));
        }

        [Test]
        public void Lighten_MovesTowardWhiteAndKeepsAlpha()
        {
            Color source = MinigameUiTheme.WithAlpha(MinigameUiTheme.Accent, .5f);
            Color lighter = MinigameUiTheme.Lighten(source, .15f);
            Assert.That(lighter.a, Is.EqualTo(.5f).Within(.001f));
            Assert.That(lighter.b, Is.GreaterThan(source.b));
            Assert.That(lighter, Is.EqualTo(Color.Lerp(source, new Color(1f, 1f, 1f, .5f), .15f)));
        }

        [Test]
        public void Palette_HoldsEveryColourTokenAndMatchesIgnoringAlpha()
        {
            Color[] palette = MinigameUiTheme.Palette();
            foreach (Color token in new[]
                     {
                         MinigameUiTheme.Surface, MinigameUiTheme.TextPrimary, MinigameUiTheme.Accent,
                         MinigameUiTheme.Player, MinigameUiTheme.Energy, MinigameUiTheme.TextOutline,
                         MinigameUiTheme.Success, MinigameUiTheme.Track, MinigameUiTheme.Scrim
                     })
                Assert.That(System.Array.Exists(palette, entry => MinigameUiTheme.RgbEquals(entry, token)), Is.True,
                    token.ToString());
            Assert.That(MinigameUiTheme.RgbEquals(MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, .3f),
                MinigameUiTheme.Surface), Is.True);
            Assert.That(MinigameUiTheme.RgbEquals(new Color32(255, 152, 0, 255), MinigameUiTheme.Accent), Is.False,
                "Volleyball's old orange is not a token.");
        }

        [Test]
        public void KitAndJourneyShareOneShadowOffset()
        {
            Assert.That(MinigameUiTheme.ShadowOffset, Is.EqualTo(UITheme.Shared.ShadowOffset));
            Assert.That(UITheme.Shared.ShadowOffset, Is.EqualTo(new Vector2(0f, -4f)));
        }
    }
}
