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
        public IEnumerator EnduranceTutorialRemainsManualUntilCompletionThenSeenSubjectStartsCountdown()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/PhaseOverlay.prefab");
            Assert.That(prefab, Is.Not.Null);

            var firstControllerObject = new GameObject("first-endurance-controller");
            var secondControllerObject = new GameObject("second-endurance-controller");
            var overlayObject = Object.Instantiate(prefab);
            try
            {
                var firstController = firstControllerObject.AddComponent<EnduranceController>();
                var overlay = overlayObject.GetComponent<PhaseOverlay>();
                var tutorial = overlayObject.GetComponentInChildren<TutorialOverlay>(true);
                var store = new MemoryTutorialSeenStore();
                tutorial.ConfigureForTest(store, "Endurance", new TutorialStep[0]);

                var completionCount = 0;
                var countdownTransitions = 0;
                tutorial.Completed += () => completionCount++;
                firstController.PhaseChanged += phase =>
                {
                    if (phase == MinigamePhase.Countdown)
                        countdownTransitions++;
                };

                overlay.Bind(firstController);
                Assert.That(tutorial.ShouldShow, Is.True);
                yield return new WaitForSeconds(2.1f);

                Assert.That(firstController.PresentationPhase, Is.EqualTo(MinigamePhase.Tutorial),
                    "An interactive tutorial must hold the lifecycle after the ordinary tutorial timeout.");
                tutorial.Next();
                tutorial.Next();
                tutorial.Close();
                tutorial.Close();
                tutorial.Skip();

                Assert.That(completionCount, Is.EqualTo(1));
                Assert.That(countdownTransitions, Is.EqualTo(1));
                Assert.That(firstController.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown));
                Assert.That(store.HasSeen("Endurance"), Is.True);

                var secondController = secondControllerObject.AddComponent<EnduranceController>();
                overlay.Bind(secondController);

                Assert.That(tutorial.ShouldShow, Is.False);
                Assert.That(overlay.IsTutorialVisible, Is.False);
                Assert.That(secondController.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown),
                    "An already-seen tutorial must release directly into countdown.");
            }
            finally
            {
                Object.Destroy(overlayObject);
                Object.Destroy(firstControllerObject);
                Object.Destroy(secondControllerObject);
            }
        }

        [UnityTest]
        public IEnumerator SprintStartPresentationReleasesAfterApprovedBannerAndMirrorsCountdown()
        {
            var controllerObject = new GameObject("sprint-controller");
            var presentationObject = new GameObject("sprint-start-presentation");
            try
            {
                var controller = controllerObject.AddComponent<SprintController>();
                var presentation = presentationObject.AddComponent<SprintStartPresentation>();
                var distanceBeforeBind = controller.Snapshot.Distance;

                presentation.Bind(controller);

                Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Tutorial));
                Assert.That(presentation.TutorialVisible, Is.True);
                Assert.That(presentation.TutorialText,
                    Is.EqualTo("← TRÁI     BẤM LUÂN PHIÊN ĐỂ CHẠY     PHẢI →"));
                Assert.That(controller.Snapshot.Distance, Is.EqualTo(distanceBeforeBind));
                controller.OnLeftTap();
                Assert.That(controller.Snapshot.Distance, Is.EqualTo(distanceBeforeBind),
                    "Sprint input must remain gated before Play.");

                presentation.TickForTest(1.49f);
                Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Tutorial));
                Assert.That(presentation.TutorialVisible, Is.True);

                presentation.TickForTest(.01f);
                Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown));
                Assert.That(presentation.TutorialVisible, Is.False);
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
                Assert.That(presentation.InstructionVisible, Is.True);
                presentation.TickForTest(.25f);
                Assert.That(presentation.InstructionVisible, Is.False);
                yield return null;
            }
            finally
            {
                Object.Destroy(presentationObject);
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
