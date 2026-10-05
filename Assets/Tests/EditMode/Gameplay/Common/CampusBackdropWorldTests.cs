using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Common
{
    public sealed class CampusBackdropWorldTests
    {
        GameObject cameraObject, backdropObject;
        CampusBackdropArt art;
        Texture2D texture;

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.aspect = 16f / 9f;
            texture = new Texture2D(200, 50);
            Sprite wide = Sprite.Create(texture, new Rect(0, 0, 200, 50), new Vector2(.5f, .5f), 100f);
            Sprite square = Sprite.Create(texture, new Rect(0, 0, 50, 50), new Vector2(.5f, .5f), 100f);
            art = ScriptableObject.CreateInstance<CampusBackdropArt>();
            art.Configure(square, wide, square, Color.cyan, Color.green);
            backdropObject = new GameObject("CampusBackdrop");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(backdropObject);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(art);
            Object.DestroyImmediate(texture);
        }

        CampusBackdropWorld Build(bool ground = true)
        {
            var world = backdropObject.AddComponent<CampusBackdropWorld>();
            world.Configure(art, cameraObject.GetComponent<Camera>(), 1f, 2f, ground);
            return world;
        }

        [Test]
        public void SkyCoversTheCameraAndPaintsTheClearColour()
        {
            CampusBackdropWorld world = Build();
            Bounds sky = world.Sky.bounds;
            Assert.That(sky.min.x, Is.LessThanOrEqualTo(-9.6f + 1e-3f));
            Assert.That(sky.max.x, Is.GreaterThanOrEqualTo(9.6f - 1e-3f));
            Assert.That(sky.min.y, Is.LessThanOrEqualTo(-5.4f + 1e-3f));
            Assert.That(sky.max.y, Is.GreaterThanOrEqualTo(5.4f - 1e-3f));
            Assert.That(world.Sky.sortingOrder, Is.EqualTo(CampusBackdropWorld.SkyOrder));
            Assert.That(cameraObject.GetComponent<Camera>().backgroundColor, Is.EqualTo(Color.cyan));
        }

        [Test]
        public void SkylineStandsOnTheHorizonAndSpansTheView()
        {
            CampusBackdropWorld world = Build();
            Renderer[] tiles = SkylineTiles();
            Assert.That(tiles.Length, Is.EqualTo(world.SkylineTileCount));
            float minX = float.MaxValue, maxX = float.MinValue;
            foreach (Renderer tile in tiles)
            {
                Assert.That(tile.bounds.min.y, Is.EqualTo(1f).Within(1e-3f));
                Assert.That(tile.bounds.size.y, Is.EqualTo(2f).Within(1e-3f));
                Assert.That(((SpriteRenderer)tile).sortingOrder, Is.EqualTo(CampusBackdropWorld.SkylineOrder));
                minX = Mathf.Min(minX, tile.bounds.min.x);
                maxX = Mathf.Max(maxX, tile.bounds.max.x);
            }
            Assert.That(minX, Is.LessThanOrEqualTo(-9.6f + 1e-3f));
            Assert.That(maxX, Is.GreaterThanOrEqualTo(9.6f - 1e-3f));
        }

        [Test]
        public void GroundFillsBelowTheHorizonWhenRequested()
        {
            CampusBackdropWorld world = Build();
            Assert.That(world.Ground.enabled, Is.True);
            Assert.That(world.Ground.color, Is.EqualTo(Color.green));
            Assert.That(world.Ground.bounds.max.y, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(world.Ground.bounds.min.y, Is.LessThanOrEqualTo(-5.4f + 1e-3f));
            Object.DestroyImmediate(world);
            Assert.That(Build(ground: false).Ground.enabled, Is.False);
        }

        [Test]
        public void GroundColourCanBeOverridden()
        {
            var world = backdropObject.AddComponent<CampusBackdropWorld>();
            world.Configure(art, cameraObject.GetComponent<Camera>(), 1f, 2f, true, Color.gray);
            Assert.That(world.Ground.color, Is.EqualTo(Color.gray));
        }

        [Test]
        public void RefitsWhenTheCameraAspectChanges()
        {
            CampusBackdropWorld world = Build();
            cameraObject.GetComponent<Camera>().aspect = 20f / 9f;
            world.Refit();
            Assert.That(world.Sky.bounds.max.x, Is.GreaterThanOrEqualTo(12f - 1e-3f));
            float maxX = float.MinValue;
            foreach (Renderer tile in SkylineTiles())
                maxX = Mathf.Max(maxX, tile.bounds.max.x);
            Assert.That(maxX, Is.GreaterThanOrEqualTo(12f - 1e-3f));
        }

        [Test]
        public void RefitTwiceKeepsOneSetOfChildren()
        {
            CampusBackdropWorld world = Build();
            int children = backdropObject.transform.childCount;
            world.Refit();
            world.Refit();
            Assert.That(backdropObject.transform.childCount, Is.EqualTo(children));
        }

        Renderer[] SkylineTiles()
        {
            var tiles = new System.Collections.Generic.List<Renderer>();
            foreach (Transform child in backdropObject.transform)
                if (child.name.StartsWith("Skyline") && child.gameObject.activeSelf)
                    tiles.Add(child.GetComponent<SpriteRenderer>());
            return tiles.ToArray();
        }
    }
}
