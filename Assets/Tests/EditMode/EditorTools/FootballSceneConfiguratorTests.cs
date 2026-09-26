#if UNITY_EDITOR
using System;
using KMA.EditorTools;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace KMA.Tests.EditorTools
{
    public sealed class FootballSceneConfiguratorTests
    {
        [TearDown]
        public void ReleaseBuiltSceneAfterTest() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void RepeatedBuildAndSharedAssemblerKeepOneSourcedFootballScene()
        {
            FootballSceneConfigurator.BuildScene();
            FootballSceneConfigurator.BuildScene();
            MinigameUIAssembler.AssembleScenePath(FootballSceneConfigurator.ScenePath);
            var scene = EditorSceneManager.OpenScene(FootballSceneConfigurator.ScenePath, OpenSceneMode.Single);

            int controllers = 0, resultPanels = 0, owners = 0, huds = 0, placeholders = 0;
            FootballPresentation presentation = null;
            EventSystem eventSystem = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                controllers += root.GetComponentsInChildren<FootballController>(true).Length;
                owners += root.GetComponentsInChildren<MinigamePresentationOwner>(true).Length;
                huds += root.GetComponentsInChildren<FootballHud>(true).Length;
                placeholders += root.GetComponentsInChildren<PlaceholderMinigameController>(true).Length;
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (behaviour is IResultPreviewPanel) resultPanels++;
                presentation ??= root.GetComponentInChildren<FootballPresentation>(true);
                eventSystem ??= root.GetComponentInChildren<EventSystem>(true);
            }

            Assert.That(controllers, Is.EqualTo(1));
            Assert.That(resultPanels, Is.EqualTo(1));
            Assert.That(owners, Is.EqualTo(1));
            Assert.That(huds, Is.EqualTo(1));
            Assert.That(placeholders, Is.Zero);
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.ValidateReferences(), Is.True);
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(eventSystem.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
            var back = GameObject.Find("BackButton").GetComponent<UnityEngine.UI.Button>();
            Assert.That(back.onClick.GetPersistentEventCount(), Is.EqualTo(1), "Back must remain wired after saving/loading the scene");
            var slider = GameObject.Find("DirectionSlider").GetComponent<UnityEngine.UI.Slider>();
            Assert.That(slider.minValue, Is.EqualTo(-1f));
            Assert.That(slider.maxValue, Is.EqualTo(1f));
            Assert.That(slider.handleRect, Is.Not.Null);
            Assert.That(GameObject.Find("AIM"), Is.Null);
            Assert.That(GameObject.Find("TrajectoryDot0").GetComponent<SpriteRenderer>().enabled, Is.False);
            Assert.That(GameObject.Find("S2_HUD_Minigame"), Is.Null);
            var subject = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/ScriptableObjects/Subjects/Football.asset");
            Assert.That(subject, Is.Not.Null);
            var serializedSubject = new SerializedObject(subject);
            Assert.That(serializedSubject.FindProperty("timeLimit").floatValue, Is.Zero);
            Assert.That(serializedSubject.FindProperty("goalText").stringValue, Is.EqualTo("Ghi ít nhất 3 bàn sau 5 lượt sút."));
            Assert.That(EditorBuildSettings.scenes, Has.Some.Matches<EditorBuildSettingsScene>(s => s.path == FootballSceneConfigurator.ScenePath && s.enabled));
        }
    }
}
#endif
