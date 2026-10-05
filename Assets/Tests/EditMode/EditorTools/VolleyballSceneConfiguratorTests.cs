#if UNITY_EDITOR
using System;
using KMA.EditorTools;
using KMA.Gameplay.UI;
using KMA.Gameplay.Volleyball;
using KMA.UI.Kit;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
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
            AssertDrawnFrom(controller.PlayerView, CharacterArt.Hero);
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
        public void EverySpriteIsSmoothFlatArtSoTheHeroDoesNotClashWithTheCourt()
        {
            VolleyballSceneConfigurator.BuildScene();
            EditorSceneManager.OpenScene(VolleyballSceneConfigurator.ScenePath, OpenSceneMode.Single);
            var allowedEnvironment = new[] { "Ball.png", "Shadow.png", "Pixel.png" };
            int checkedSprites = 0;
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                Assert.That(renderer.sprite, Is.Not.Null, renderer.name);
                string path = AssetDatabase.GetAssetPath(renderer.sprite);
                Assert.That(renderer.sprite.texture.filterMode, Is.EqualTo(FilterMode.Bilinear),
                    renderer.name + " uses " + path + ": pixel-filtered art next to the smooth character athletes");
                if (path.Contains("/Environments/Volleyball/"))
                    Assert.That(allowedEnvironment, Does.Contain(System.IO.Path.GetFileName(path)),
                        renderer.name + " still draws the pixel-art BVA2 sheet " + path);
                checkedSprites++;
            }
            Assert.That(checkedSprites, Is.GreaterThan(10));
            Assert.That(System.IO.Directory.GetFiles("Assets/_Project/Art/Environments/Volleyball", "*.png"),
                Has.Length.EqualTo(allowedEnvironment.Length), "Unused BVA2 sheets must be deleted from the project.");
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


        [Test]
        public void ControlsScoreboardAndPauseAreDrawnWithTheUiKit()
        {
            VolleyballSceneConfigurator.BuildScene();
            EditorSceneManager.OpenScene(VolleyballSceneConfigurator.ScenePath, OpenSceneMode.Single);
            UiKitAssets assets = UiKitAssets.Load();

            foreach (Image image in Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (image.sprite != null)
                    Assert.That(image.sprite.name, Is.Not.EqualTo("Knob"), image.name + " still stretches the built-in knob");

            var scoreboard = GameObject.Find("VolleyballScoreboard").GetComponent<Image>();
            Assert.That(scoreboard.sprite, Is.SameAs(assets.RoundRect24));
            Assert.That(scoreboard.type, Is.EqualTo(Image.Type.Sliced), "a rounded panel, not a stretched ellipse");

            var action = Object.FindFirstObjectByType<ActionButton>();
            var feedback = action.GetComponent<KitPressFeedback>();
            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.RestColor, Is.EqualTo(MinigameUiTheme.Accent));

            Assert.That(GameObject.Find("JoystickBase").GetComponent<Image>().sprite, Is.SameAs(assets.Circle));
            Assert.That(GameObject.Find("PlayerTitle").GetComponent<TMPro.TMP_Text>().color, Is.EqualTo(MinigameUiTheme.Player));
            Assert.That(GameObject.Find("EnemyTitle").GetComponent<TMPro.TMP_Text>().color, Is.EqualTo(MinigameUiTheme.Energy));

            var pause = Object.FindFirstObjectByType<PausePanel>();
            Assert.That(pause.GetComponent<Image>().sprite, Is.SameAs(assets.RoundRect20));
            Assert.That(Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
        }
        // World height, above the feet pivot, of the highest opaque pixel of an imported character pose.
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
                    Assert.That(CharacterArt.IsPoseOf(sprite, character), Is.True,
                        $"{view.name} {action} uses {AssetDatabase.GetAssetPath(sprite)}");
        }
    }
}
#endif
