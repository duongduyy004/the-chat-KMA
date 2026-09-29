#if UNITY_EDITOR
using System;
using KMA.EditorTools;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KMA.Tests.EditorTools
{
    public sealed class VolleyballSceneConfiguratorTests
    {
        [TearDown]
        public void ReleaseBuiltSceneAfterTest() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void HeroFacesAnUntintedPackOpponent()
        {
            VolleyballSceneConfigurator.BuildScene();
            VolleyballSceneConfigurator.BuildScene();
            EditorSceneManager.OpenScene(VolleyballSceneConfigurator.ScenePath, OpenSceneMode.Single);

            Assert.That(Object.FindObjectsByType<VolleyAthleteView>(FindObjectsSortMode.None), Has.Length.EqualTo(2));
            var controller = Object.FindFirstObjectByType<VolleyballController>();
            AssertDrawnFrom(controller.PlayerView, ToonCharacterArt.Hero);
            AssertDrawnFrom(controller.OpponentView, VolleyballSceneConfigurator.OpponentCharacter);
            Assert.That(Mirrored(controller.PlayerView), Is.False, "The hero is never mirrored.");
            Assert.That(Mirrored(controller.OpponentView), Is.True, "The opponent faces the net.");
            foreach (var view in new[] { controller.PlayerView, controller.OpponentView })
            {
                Assert.That(view.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white), view.name);
                Assert.That(view.transform.localScale, Is.EqualTo(Vector3.one * VolleyballSceneConfigurator.AthleteScale), view.name);
            }
        }

        static bool Mirrored(VolleyAthleteView view) => new SerializedObject(view).FindProperty("mirror").boolValue;

        static void AssertDrawnFrom(VolleyAthleteView view, string character)
        {
            foreach (AthleteAction action in Enum.GetValues(typeof(AthleteAction)))
                foreach (Sprite sprite in view.FramesFor(action))
                    Assert.That(ToonCharacterArt.IsPoseOf(sprite, character), Is.True,
                        $"{view.name} {action} uses {AssetDatabase.GetAssetPath(sprite)}");
        }
    }
}
#endif
