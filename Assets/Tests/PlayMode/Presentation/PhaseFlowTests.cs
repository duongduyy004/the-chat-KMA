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
        public IEnumerator SprintStartPresentationReleasesAfterInstructionGateAndMirrorsCountdown()
        {
            var controllerObject = new GameObject("sprint-controller");
            var presentationObject = new GameObject("sprint-start-presentation");
            var countdownRoot = new GameObject("countdown-root", typeof(RectTransform));
            var instructionRoot = new GameObject("instruction-root", typeof(RectTransform));
            try
            {
                var countdownLabel = countdownRoot.AddComponent<TextMeshProUGUI>();
                var instructionLabel = instructionRoot.AddComponent<TextMeshProUGUI>();
                var controller = controllerObject.AddComponent<SprintController>();
                var presentation = presentationObject.AddComponent<SprintStartPresentation>();
                presentation.Configure(countdownRoot, countdownLabel, instructionRoot, instructionLabel);
                var distanceBeforeBind = controller.Snapshot.Distance;

                presentation.Bind(controller);

                Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Tutorial));
                Assert.That(presentation.InstructionVisible, Is.True);
                Assert.That(instructionRoot.activeSelf, Is.True);
                Assert.That(presentation.InstructionText, Is.EqualTo(SprintStartPresentation.InstructionCopy));
                Assert.That(controller.Snapshot.Distance, Is.EqualTo(distanceBeforeBind));
                controller.OnLeftTap();
                Assert.That(controller.Snapshot.Distance, Is.EqualTo(distanceBeforeBind),
                    "Sprint input must remain gated before Play.");

                presentation.TickForTest(1.49f);
                Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Tutorial));
                Assert.That(presentation.InstructionVisible, Is.True, "instruction persists through the gate");

                presentation.TickForTest(.01f);
                Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown));
                Assert.That(presentation.InstructionVisible, Is.True, "instruction persists through the countdown");
                Assert.That(countdownRoot.activeSelf, Is.True);
                Assert.That(presentation.CountdownText, Is.EqualTo("3"));

                controller.Simulate(1f);
                presentation.TickForTest(1f);
                Assert.That(presentation.CountdownText, Is.EqualTo("2"));
                controller.Simulate(1f);
                presentation.TickForTest(1f);
                Assert.That(presentation.CountdownText, Is.EqualTo("1"));
                controller.Simulate(1f);
                Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play));
                Assert.That(presentation.CountdownText, Is.EqualTo("GO!"));
                Assert.That(countdownRoot.activeSelf, Is.True,
                    "GO! must render at the Countdown-to-Play boundary.");
                Assert.That(presentation.InstructionVisible, Is.True);
                presentation.TickForTest(.5f);
                Assert.That(countdownRoot.activeSelf, Is.False);
                Assert.That(presentation.InstructionVisible, Is.False, "instruction fades shortly after GO");
                yield return null;
            }
            finally
            {
                Object.Destroy(presentationObject);
                Object.Destroy(controllerObject);
                Object.Destroy(countdownRoot);
                Object.Destroy(instructionRoot);
            }
        }

        [UnityTest]
        public IEnumerator PresentationPrefabsBindFontsEffectsAndPassiveResultFields()
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
                if (text.font.atlasPopulationMode == AtlasPopulationMode.Static)
                    Assert.That(text.font.fallbackFontAssetTable, Does.Contain(VietTypography.Library.regular), text.name);
                else
                    Assert.That(text.font.sourceFontFile, Is.Not.Null, text.name);
            }

            // Every preset must use the atlas of its assigned Vietnamese font.
            var fonts = VietTypography.Library;
            var materials = phasePrefab.GetComponentsInChildren<TMP_Text>(true)
                .Concat(resultPrefab.GetComponentsInChildren<TMP_Text>(true))
                .Select(text => text.fontSharedMaterial)
                .Distinct()
                .ToArray();
            Assert.That(materials, Is.SubsetOf(new[] { fonts.title.material, fonts.buttonHud.material,
                fonts.regular.material, fonts.bold.material, fonts.titleMaterial, fonts.primaryMaterial,
                fonts.secondaryMaterial, fonts.bodyMaterial, fonts.bodyBoldMaterial }));

            var resultObject = Object.Instantiate(resultPrefab);
            try
            {
                var panel = resultObject.GetComponent<ResultPanel>();
                Assert.That(panel, Is.Not.Null);
                panel.Show(new MinigameResult(false, 987.6f, Rank.B), "MapPreview");
                yield return new WaitForSecondsRealtime(1.5f); // the score now counts up from 0

                var labels = resultObject.GetComponentsInChildren<TMP_Text>(true)
                    .Where(label => label.name != "Label") // the action and retry buttons each have one
                    .ToDictionary(label => label.name, label => label.text);
                Assert.That(labels["StatusLabel"], Is.EqualTo("THẤT BẠI"));
                Assert.That(labels["ScoreLabel"], Is.EqualTo("988"));
                Assert.That(labels["RankLabel"], Is.EqualTo("XẾP HẠNG B"));
            }
            finally
            {
                Object.Destroy(resultObject);
            }
        }

        [Test]
        public void ResultPanel_ShowMovesModalAboveLateGameplayOverlays()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/ResultPanel.prefab");
            var canvas = new GameObject("canvas", typeof(RectTransform));
            var resultObject = Object.Instantiate(prefab, canvas.transform);
            var lateOverlay = new GameObject("late-overlay", typeof(RectTransform));
            lateOverlay.transform.SetParent(canvas.transform, false);
            try
            {
                resultObject.GetComponent<ResultPanel>().Show(
                    new MinigameResult(true, 8f, Rank.A), "Map");

                Assert.That(resultObject.transform.GetSiblingIndex(),
                    Is.EqualTo(canvas.transform.childCount - 1),
                    "The result modal must render above pause/HUD overlays created later.");
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
