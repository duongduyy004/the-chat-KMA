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

        [Test]
        public void MarkersSitAboveTheTallestPoseOfEachAthlete()
        {
            VolleyballSceneConfigurator.BuildScene();
            EditorSceneManager.OpenScene(VolleyballSceneConfigurator.ScenePath, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<VolleyballController>();
            AssertMarkerClearsHead(controller.PlayerView, "PlayerMarkerEdge");
            AssertMarkerClearsHead(controller.OpponentView, "EnemyMarkerEdge");
        }

        static void AssertMarkerClearsHead(VolleyAthleteView view, string markerName)
        {
            float markerBottom = view.transform.Find(markerName).GetComponent<SpriteRenderer>().bounds.min.y
                                 - view.transform.position.y;
            float headTop = 0f;
            foreach (AthleteAction action in Enum.GetValues(typeof(AthleteAction)))
                foreach (Sprite sprite in view.FramesFor(action))
                    headTop = Mathf.Max(headTop, OpaqueTop(sprite) * VolleyballSceneConfigurator.AthleteScale);
            Assert.That(markerBottom, Is.GreaterThanOrEqualTo(headTop),
                $"{markerName} must not cover the head of {view.name} (tallest pose reaches {headTop:0.00}).");
        }

        // World height, above the feet pivot, of the highest opaque pixel of an imported Toon pose.
        static float OpaqueTop(Sprite sprite)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
            for (int y = texture.height - 1; y >= 0; y--)
                for (int x = 0; x < texture.width; x++)
                    if (texture.GetPixel(x, y).a > .5f)
                    {
                        float top = (y + 1) / sprite.pixelsPerUnit;
                        Object.DestroyImmediate(texture);
                        return top;
                    }
            Object.DestroyImmediate(texture);
            return 0f;
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
