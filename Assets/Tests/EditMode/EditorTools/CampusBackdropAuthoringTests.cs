using KMA.EditorTools;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMA.Tests.EditorTools
{
    public sealed class CampusBackdropAuthoringTests
    {
        [Test]
        public void ArtAssetReferencesTheGeneratedCampusSprites()
        {
            CampusBackdropArt art = CampusBackdropAuthoring.EnsureArt();
            Assert.That(AssetDatabase.GetAssetPath(art), Is.EqualTo(CampusBackdropAuthoring.AssetPath));
            Assert.That(AssetDatabase.GetAssetPath(art.Sky), Does.EndWith("/Campus/CampusSky.png"));
            Assert.That(AssetDatabase.GetAssetPath(art.Skyline), Does.EndWith("/Campus/CampusSkyline.png"));
            Assert.That(AssetDatabase.GetAssetPath(art.Pixel), Does.EndWith("/Campus/CampusPixel.png"));
            foreach (string name in new[] { "CampusSky", "CampusSkyline", "CampusPixel", "SprintTrack", "VolleyNet" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(CampusBackdropAuthoring.ArtDir + "/" + name + ".png");
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), name);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f), name);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), name);
                Assert.That(importer.mipmapEnabled, Is.False, name);
            }
        }

        [Test]
        public void AddWorldTwiceKeepsOneBackdrop()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.orthographic = true;
            CampusBackdropAuthoring.AddWorld(scene, camera, 1f, 2f, true);
            CampusBackdropAuthoring.AddWorld(scene, camera, 1f, 2f, true);
            Assert.That(Object.FindObjectsByType<CampusBackdropWorld>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        [Test]
        public void AddUiPutsTheBackdropFirstUnderTheCanvas()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            new GameObject("SafeAreaRoot", typeof(RectTransform)).transform.SetParent(canvas.transform, false);
            CampusBackdropUi backdrop = CampusBackdropAuthoring.AddUi((RectTransform)canvas.transform, .3f, .25f, true);
            CampusBackdropAuthoring.AddUi((RectTransform)canvas.transform, .3f, .25f, true);
            Assert.That(backdrop.transform.GetSiblingIndex(), Is.Zero);
            Assert.That(canvas.GetComponentsInChildren<CampusBackdropUi>(true), Has.Length.EqualTo(1));
        }
    }
}
