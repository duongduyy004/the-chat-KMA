using System.Reflection;
using System.Collections;
using UnityEditor;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class UIComponentTests
    {
        [Test]
        public void SafeAreaFitterMapsLandscapeInsetsToBothHorizontalEdges()
        {
            var root = new GameObject("safe-area-fitter", typeof(RectTransform));
            try
            {
                var fitter = root.AddComponent<SafeAreaFitter>();
                var offsets = fitter.CalculateOffsets(
                    new Rect(100f, 0f, 1720f, 1080f),
                    new Vector2(1920f, 1080f));

                Assert.That(offsets.left, Is.EqualTo(100f).Within(.01f));
                Assert.That(offsets.right, Is.EqualTo(100f).Within(.01f));

                fitter.Apply(new Rect(100f, 0f, 1720f, 1080f), new Vector2Int(1920, 1080));
                var rectTransform = root.GetComponent<RectTransform>();
                Assert.That(rectTransform.offsetMin.x, Is.EqualTo(100f).Within(.01f));
                Assert.That(rectTransform.offsetMax.x, Is.EqualTo(-100f).Within(.01f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BrutalButtonReturnsToRestAfterPointerUp()
        {
            var root = new GameObject("brutal-button", typeof(RectTransform));
            try
            {
                var shadowObject = new GameObject("shadow", typeof(RectTransform));
                shadowObject.transform.SetParent(root.transform, false);
                var shadow = shadowObject.GetComponent<RectTransform>();
                shadow.anchoredPosition = new Vector2(6f, -6f);
                var button = root.AddComponent<BrutalButton>();
                SetPrivateField(button, "shadow", shadow);

                button.SetPressedForTest(true);
                Assert.That(button.CurrentVisualOffset, Is.EqualTo(new Vector2(4f, -4f)));
                Assert.That(shadow.anchoredPosition, Is.EqualTo(Vector2.zero));

                button.SetPressedForTest(false);
                Assert.That(button.CurrentVisualOffset, Is.EqualTo(Vector2.zero));
                Assert.That(shadow.anchoredPosition, Is.EqualTo(new Vector2(6f, -6f)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SharedFontAssetsExposeSourceAndAtlasTextures()
        {
            foreach (var assetName in new[] { "Baloo2-ExtraBold", "Nunito-Bold" })
            {
                var path = $"Assets/_Project/Fonts/{assetName}.asset";
                var font = AssetDatabase.LoadMainAssetAtPath(path);
                Assert.That(font, Is.Not.Null, path);

                var source = font.GetType().GetProperty("sourceFontFile")?.GetValue(font);
                Assert.That(source, Is.Not.Null, $"{assetName} source font");

                var atlases = font.GetType().GetProperty("atlasTextures")?.GetValue(font) as IEnumerable;
                Assert.That(atlases, Is.Not.Null, $"{assetName} atlas textures");
                var atlasCount = 0;
                foreach (var atlas in atlases)
                {
                    atlasCount++;
                    Assert.That(atlas, Is.Not.Null, $"{assetName} atlas {atlasCount}");
                }
                Assert.That(atlasCount, Is.GreaterThan(0), $"{assetName} atlas count");
            }
        }

        [Test]
        public void HeartBarClampsToFiveSlotsAndRendersFilledAndEmptyStates()
        {
            var root = new GameObject("heart-bar");
            try
            {
                var heartBar = root.AddComponent<HeartBar>();
                var slots = new Image[5];
                for (var index = 0; index < slots.Length; index++)
                {
                    var slot = new GameObject($"heart-{index}", typeof(RectTransform));
                    slot.transform.SetParent(root.transform, false);
                    slots[index] = slot.AddComponent<Image>();
                }
                SetPrivateField(heartBar, "slots", slots);

                heartBar.SetHearts(3);

                Assert.That(heartBar.CurrentHearts, Is.EqualTo(3));
                Assert.That(slots[0].color, Is.EqualTo(heartBar.FilledColor));
                Assert.That(slots[2].color, Is.EqualTo(heartBar.FilledColor));
                Assert.That(slots[3].color, Is.EqualTo(heartBar.EmptyColor));

                heartBar.SetHearts(99);
                Assert.That(heartBar.CurrentHearts, Is.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
