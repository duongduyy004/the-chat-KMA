using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace KMA.Tests.Presentation
{
    public sealed class VietnameseFontTests
    {
        private const string PrimaryFontPath = "Assets/_Project/Fonts/Baloo2-ExtraBold.asset";
        private const string FallbackFontPath = "Assets/_Project/Fonts/VietnameseFallback.asset";

        [Test]
        [Category("FontAsset")]
        public void PrimaryFontPreauthorsRequiredVietnameseCharacters()
        {
            var font = AssetDatabase.LoadMainAssetAtPath(PrimaryFontPath);
            Assert.That(font, Is.Not.Null, PrimaryFontPath);

            var lookup = font.GetType()
                .GetProperty("characterLookupTable", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(font) as IDictionary;
            Assert.That(lookup, Is.Not.Null, "Baloo2-ExtraBold character lookup table");

            var requiredCharacters = new uint[]
            {
                0x0110, // Đ
                0x0111, // đ
                0x0103, // ă
                0x0102, // Ă
                0x1ED9, // ộ
                0x01A1, // ơ
                0x01AF, // Ư
                0x1EE9, // ứ
            };

            foreach (var character in requiredCharacters)
                Assert.That(lookup.Contains(character), Is.True, $"Baloo2-ExtraBold does not pre-author U+{character:X4}");
        }

        [Test]
        [Category("FontAsset")]
        public void PrimaryFontRetainsDynamicVietnameseFallback()
        {
            var font = AssetDatabase.LoadMainAssetAtPath(PrimaryFontPath);
            var expectedFallback = AssetDatabase.LoadMainAssetAtPath(FallbackFontPath);
            Assert.That(font, Is.Not.Null, PrimaryFontPath);
            Assert.That(expectedFallback, Is.Not.Null, FallbackFontPath);

            var fallbacks = font.GetType()
                .GetProperty("fallbackFontAssetTable", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(font) as IEnumerable;
            Assert.That(fallbacks, Is.Not.Null, "Baloo2-ExtraBold fallback table");

            object actualFallback = null;
            foreach (var fallback in fallbacks)
            {
                actualFallback = fallback;
                break;
            }

            Assert.That(actualFallback, Is.SameAs(expectedFallback), "Baloo2-ExtraBold first fallback");
            var fallbackMode = actualFallback.GetType()
                .GetProperty("atlasPopulationMode", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(actualFallback);
            Assert.That(fallbackMode, Is.Not.Null, "VietnameseFallback atlas population mode");
            Assert.That(fallbackMode.ToString(), Is.EqualTo("Dynamic"));
        }
    }
}
