using System.Reflection;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    /// <summary>
    /// Pins where the safe-area inset is allowed to be applied. A <see cref="SafeAreaFitter"/>
    /// writes offsetMin/offsetMax; on a rect whose anchors are coincident those offsets *are*
    /// sizeDelta, so applying there resizes the rect to nothing instead of insetting it. The
    /// minigame Canvas root is exactly such a rect, which is why the HUD collapsed to the origin.
    /// </summary>
    public sealed class SafeAreaContractTests
    {
        const string HudPrefabPath = "Assets/_Project/Prefabs/UI/HUD_Minigame.prefab";

        [Test]
        public void SafeAreaFitterLeavesACoincidentAnchorRectUnresized()
        {
            var host = new GameObject("coincident-anchors", typeof(RectTransform));
            try
            {
                var rect = host.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.sizeDelta = new Vector2(1920f, 1080f);

                host.AddComponent<SafeAreaFitter>()
                    .Apply(new Rect(0f, 0f, 1920f, 1080f), new Vector2Int(1920, 1080));

                Assert.That(rect.sizeDelta.x, Is.EqualTo(1920f).Within(.01f),
                    "Coincident anchors turn an offset write into a resize; the fitter must not touch that axis.");
                Assert.That(rect.sizeDelta.y, Is.EqualTo(1080f).Within(.01f),
                    "Coincident anchors turn an offset write into a resize; the fitter must not touch that axis.");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void SafeAreaFitterStillInsetsAStretchedRect()
        {
            var host = new GameObject("stretched-anchors", typeof(RectTransform));
            try
            {
                var rect = host.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;

                host.AddComponent<SafeAreaFitter>()
                    .Apply(new Rect(100f, 0f, 1720f, 1080f), new Vector2Int(1920, 1080));

                Assert.That(rect.offsetMin.x, Is.EqualTo(100f).Within(.01f));
                Assert.That(rect.offsetMax.x, Is.EqualTo(-100f).Within(.01f));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void HudPrefabKeepsTheSafeAreaFitterOffTheCanvasRoot()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"{HudPrefabPath} must exist.");

            var rootRect = (RectTransform)prefab.transform;
            Assert.That(rootRect.anchorMin, Is.EqualTo(rootRect.anchorMax),
                "Guard for the assertion below: the Canvas root is anchored coincidentally.");
            Assert.That(prefab.GetComponent<SafeAreaFitter>(), Is.Null,
                "A fitter on the Canvas root resizes it to 0x0, collapsing every anchored child to the origin.");
        }

        [Test]
        public void HudPrefabAppliesTheSafeAreaOnTheStretchedSafeAreaRoot()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"{HudPrefabPath} must exist.");

            var safeAreaRoot = prefab.transform.Find("SafeAreaRoot") as RectTransform;
            Assert.That(safeAreaRoot, Is.Not.Null, "HUD_Minigame must keep a SafeAreaRoot child.");
            Assert.That(safeAreaRoot.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(safeAreaRoot.anchorMax, Is.EqualTo(Vector2.one));

            var fitter = safeAreaRoot.GetComponent<SafeAreaFitter>();
            Assert.That(fitter, Is.Not.Null, "SafeAreaRoot is the only rect that can carry the inset.");
            Assert.That(fitter.enabled, Is.True, "SafeAreaRoot's fitter must be the one that runs.");
        }

        [Test]
        public void AssemblerMovesTheFitterOffTheCanvasRootOntoSafeAreaRoot()
        {
            var canvasRoot = new GameObject("S2_HUD_Minigame", typeof(RectTransform));
            try
            {
                var rootRect = canvasRoot.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.zero;
                canvasRoot.AddComponent<SafeAreaFitter>();

                var safeAreaRoot = new GameObject("SafeAreaRoot", typeof(RectTransform));
                safeAreaRoot.transform.SetParent(canvasRoot.transform, false);

                typeof(MinigameUIAssembler)
                    .GetMethod("EnsureSafeAreaFitter", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { canvasRoot });

                Assert.That(canvasRoot.GetComponent<SafeAreaFitter>(), Is.Null,
                    "Re-assembling a scene must not put a fitter back on the Canvas root.");
                var fitter = safeAreaRoot.GetComponent<SafeAreaFitter>();
                Assert.That(fitter, Is.Not.Null, "The assembler must place the fitter on SafeAreaRoot.");
                Assert.That(fitter.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(canvasRoot);
            }
        }

        [Test]
        public void SprintPrepareSafeAreaLeavesTheNestedFitterEnabled()
        {
            var safeArea = new GameObject("SafeAreaRoot", typeof(RectTransform));
            try
            {
                var rect = safeArea.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                var fitter = safeArea.AddComponent<SafeAreaFitter>();

                typeof(SprintFestivalPresentation)
                    .GetMethod("PrepareSafeArea", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { safeArea.transform });

                Assert.That(fitter.enabled, Is.True,
                    "Sprint must keep SafeAreaRoot's fitter as the rect that insets the HUD, not switch it off.");
            }
            finally
            {
                Object.DestroyImmediate(safeArea);
            }
        }
    }
}
