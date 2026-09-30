using System.Collections;
using System.Collections.Generic;
using KMA.Gameplay.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Core
{
    public sealed class VietFallbackPresentationTests
    {
        [UnityTest]
        public IEnumerator FallbackYieldsToRealCanvasAndReturnsWhenItIsRemoved()
        {
            var presentations = new List<KeyValuePair<GameplayPresentation, bool>>();
            var canvases = new List<KeyValuePair<Canvas, bool>>();
            foreach (var p in Object.FindObjectsByType<GameplayPresentation>(FindObjectsSortMode.None))
            { presentations.Add(new KeyValuePair<GameplayPresentation, bool>(p, p.enabled)); p.enabled = false; }
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            { canvases.Add(new KeyValuePair<Canvas, bool>(c, c.gameObject.activeSelf)); c.gameObject.SetActive(false); }
            var root = new GameObject("FallbackTest");
            GameObject real = null;
            try
            {
                root.AddComponent<GameplayPresentation>();
                yield return null;
                var fallback = root.transform.Find("FallbackPresentation");
                Assert.That(fallback, Is.Not.Null);
                Assert.That(fallback.gameObject.activeSelf, Is.True);
                real = new GameObject("Authored UI", typeof(RectTransform), typeof(Canvas));
                yield return null;
                Assert.That(fallback.gameObject.activeSelf, Is.False, "The fallback must not cover authored UI.");
                Object.Destroy(real);
                yield return null;
                yield return null;
                Assert.That(fallback.gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.Destroy(root);
                if (real != null) Object.Destroy(real);
                foreach (var c in canvases) if (c.Key != null) c.Key.gameObject.SetActive(c.Value);
                foreach (var p in presentations) if (p.Key != null) p.Key.enabled = p.Value;
            }
        }
    }
}
