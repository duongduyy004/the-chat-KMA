using System.Collections;
using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Presentation
{
    public sealed class PhaseFlowTests
    {
        [UnityTest]
        public IEnumerator SprintDefaultsFlowFromTutorialThroughCountdownToPlayPresentation()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/PhaseOverlay.prefab");
            Assert.That(prefab, Is.Not.Null);

            var controllerObject = new GameObject("sprint-controller");
            var overlayObject = Object.Instantiate(prefab);
            try
            {
                var controller = controllerObject.AddComponent<SprintController>();
                var overlay = overlayObject.GetComponent<PhaseOverlay>();
                Assert.That(overlay, Is.Not.Null);
                var distanceBeforeBind = controller.Snapshot.Distance;

                overlay.Bind(controller);

                Assert.That(overlay.DisplayedPhase, Is.EqualTo(MinigamePhase.Tutorial));
                Assert.That(overlay.IsTutorialVisible, Is.True);
                Assert.That(controller.Snapshot.Distance, Is.EqualTo(distanceBeforeBind));

                yield return new WaitForSeconds(2.1f);
                Assert.That(overlay.DisplayedPhase, Is.EqualTo(MinigamePhase.Countdown));
                Assert.That(overlay.CountdownText, Is.EqualTo("3"));

                yield return new WaitForSeconds(3.1f);
                Assert.That(overlay.DisplayedPhase, Is.EqualTo(MinigamePhase.Play));
                Assert.That(overlay.IsPlayVisible, Is.True);
            }
            finally
            {
                Object.Destroy(overlayObject);
                Object.Destroy(controllerObject);
            }
        }

        [Test]
        public void PresentationPrefabsBindFontsEffectsAndPassiveResultFields()
        {
            var phasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/PhaseOverlay.prefab");
            var resultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/ResultPanel.prefab");

            Assert.That(phasePrefab, Is.Not.Null);
            Assert.That(resultPrefab, Is.Not.Null);
            Assert.That(phasePrefab.GetComponentsInChildren<MonoBehaviour>(true), Has.None.Null);
            Assert.That(resultPrefab.GetComponentsInChildren<MonoBehaviour>(true), Has.None.Null);

            foreach (var text in phasePrefab.GetComponentsInChildren<TMP_Text>(true)
                         .Concat(resultPrefab.GetComponentsInChildren<TMP_Text>(true)))
            {
                Assert.That(text.font, Is.Not.Null, text.name);
                Assert.That(text.font.fallbackFontAssetTable, Has.Some.Not.Null, text.name);
            }

            var materials = phasePrefab.GetComponentsInChildren<TMP_Text>(true)
                .Concat(resultPrefab.GetComponentsInChildren<TMP_Text>(true))
                .Select(text => text.fontSharedMaterial)
                .Distinct()
                .ToArray();
            var shadow = materials.Single(material => material.name == "Nunito-Bold-TextShadow");
            Assert.That(shadow.IsKeywordEnabled("UNDERLAY_ON"), Is.True);
            Assert.That(shadow.GetFloat(ShaderUtilities.ID_UnderlayOffsetX), Is.EqualTo(.04f).Within(.001f));
            Assert.That(shadow.GetFloat(ShaderUtilities.ID_UnderlayOffsetY), Is.EqualTo(-.04f).Within(.001f));
            Assert.That(shadow.GetFloat(ShaderUtilities.ID_UnderlaySoftness), Is.Zero.Within(.001f));
            Assert.That(shadow.GetColor(ShaderUtilities.ID_UnderlayColor), Is.EqualTo(Color.black));

            var stroke = materials.Single(material => material.name == "Baloo2-ExtraBold-TextStrokeDark");
            Assert.That(stroke.IsKeywordEnabled("UNDERLAY_ON"), Is.True);
            Assert.That(stroke.GetFloat(ShaderUtilities.ID_OutlineWidth), Is.EqualTo(.2f).Within(.001f));
            Assert.That(stroke.GetColor(ShaderUtilities.ID_OutlineColor), Is.EqualTo(Color.black));

            var resultObject = Object.Instantiate(resultPrefab);
            try
            {
                var panel = resultObject.GetComponent<ResultPanel>();
                Assert.That(panel, Is.Not.Null);
                panel.Show(new MinigameResult(false, 987.6f, Rank.B), "MapPreview");

                var labels = resultObject.GetComponentsInChildren<TMP_Text>(true)
                    .ToDictionary(label => label.name, label => label.text);
                Assert.That(labels["StatusLabel"], Is.EqualTo("FAIL"));
                Assert.That(labels["ScoreLabel"], Is.EqualTo("988"));
                Assert.That(labels["RankLabel"], Is.EqualTo("RANK B"));
            }
            finally
            {
                Object.Destroy(resultObject);
            }
        }
    }
}
