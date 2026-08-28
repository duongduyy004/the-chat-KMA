using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class UIThemeTests
    {
        [Test]
        public void ThemeAssetUsesApprovedPalette()
        {
            var theme = AssetDatabase.LoadAssetAtPath<KMA.Gameplay.UI.UITheme>("Assets/_Project/Settings/UI/UITheme.asset");
            Assert.That(theme, Is.Not.Null);
            Assert.That((Color32)theme.Primary, Is.EqualTo(new Color32(0xFF, 0x59, 0x5E, 0xFF)));
            Assert.That((Color32)theme.Accent, Is.EqualTo(new Color32(0xFF, 0xCA, 0x3A, 0xFF)));
            Assert.That((Color32)theme.Background, Is.EqualTo(new Color32(0x19, 0x82, 0xC4, 0xFF)));
            Assert.That((Color32)theme.Success, Is.EqualTo(new Color32(0x8A, 0xCB, 0x88, 0xFF)));
            Assert.That(theme.Card, Is.EqualTo(Color.white));
            Assert.That((Color32)theme.Muted, Is.EqualTo(new Color32(0xE2, 0xE8, 0xF0, 0xFF)));
            Assert.That((Color32)theme.MutedForeground, Is.EqualTo(new Color32(0x47, 0x55, 0x69, 0xFF)));
            Assert.That(theme.Border, Is.EqualTo(Color.black));
        }

        [Test]
        public void TutorialSeenStoreRoundTripsBySubject()
        {
            var store = new KMA.Gameplay.UI.MemoryTutorialSeenStore();
            Assert.That(store.HasSeen("Sprint"), Is.False);
            store.MarkSeen("Sprint");
            Assert.That(store.HasSeen("Sprint"), Is.True);
            Assert.That(store.HasSeen("Endurance"), Is.False);
        }

        [Test]
        public void VietnameseFontAssetsContainRequiredGlyphs()
        {
            foreach (var assetName in new[] { "Baloo2-ExtraBold", "Nunito-Bold" })
            {
                var font = AssetDatabase.LoadMainAssetAtPath($"Assets/_Project/Fonts/{assetName}.asset");
                Assert.That(font, Is.Not.Null, assetName);
                var lookup = font.GetType().GetProperty("characterLookupTable", BindingFlags.Instance | BindingFlags.Public).GetValue(font) as IDictionary;
                Assert.That(lookup, Is.Not.Null, $"{assetName} has no character lookup table");
                foreach (var character in new[] { 'Đ', 'đ', 'ă', 'Ă', 'ộ', 'ơ', 'Ư', 'ứ' })
                    Assert.That(ContainsCharacter(font, (uint)character, new HashSet<object>()), Is.True, $"{assetName} and its fallbacks are missing {character}");
            }
        }

        private static bool ContainsCharacter(object font, uint character, HashSet<object> visited)
        {
            if (!visited.Add(font))
                return false;
            var type = font.GetType();
            var lookup = type.GetProperty("characterLookupTable", BindingFlags.Instance | BindingFlags.Public).GetValue(font) as IDictionary;
            if (lookup != null && lookup.Contains(character))
                return true;
            var fallbacks = type.GetProperty("fallbackFontAssetTable", BindingFlags.Instance | BindingFlags.Public).GetValue(font) as IEnumerable;
            if (fallbacks == null)
                return false;
            foreach (var fallback in fallbacks)
            {
                if (fallback != null && ContainsCharacter(fallback, character, visited))
                    return true;
            }
            return false;
        }
    }
}
