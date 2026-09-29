#if UNITY_EDITOR
using System.IO;
using KMA.EditorTools;
using KMA.UI.Kit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class UiKitAssetsTests
    {
        [OneTimeSetUp]
        public void Bake() => UiKitSpriteBaker.Bake();

        [Test]
        public void LoadReturnsTheBakedAssetWithFiveSpritesTheFontAndTheStroke()
        {
            UiKitAssets assets = UiKitAssets.Load();
            Assert.That(assets, Is.SameAs(AssetDatabase.LoadAssetAtPath<UiKitAssets>(UiKitAssets.AssetPath)));
            Assert.That(assets.AllSprites(), Has.Length.EqualTo(5));
            Assert.That(assets.AllSprites(), Has.All.Not.Null);
            Assert.That(assets.Font, Is.Not.Null);
            Assert.That(assets.Font.name, Is.EqualTo("Baloo2-ExtraBold"));
            Assert.That(assets.OutlineMaterial, Is.Not.Null);
            Assert.That(assets.OutlineMaterial.name, Is.EqualTo("Baloo2-ExtraBold-TextStrokeDark"));
        }

        [TestCase(20)]
        [TestCase(24)]
        [TestCase(36)]
        public void EachRoundRectCarriesABorderEqualToItsRadius(int radius)
        {
            Sprite sprite = UiKitAssets.Load().RoundRectFor(radius);
            Assert.That(sprite.name, Is.EqualTo("RoundRect" + radius));
            Assert.That(sprite.border, Is.EqualTo(new Vector4(radius, radius, radius, radius)));
            Assert.That(sprite.rect.width, Is.EqualTo(radius * 2 + 2));
        }

        [Test]
        public void OtherRadiiFallBackToRoundRect24() =>
            Assert.That(UiKitAssets.Load().RoundRectFor(16f), Is.SameAs(UiKitAssets.Load().RoundRect24));

        [Test]
        public void EverySpriteImportsBilinearWithoutMipmapsAt100PixelsPerUnit()
        {
            foreach (Sprite sprite in UiKitAssets.Load().AllSprites())
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), sprite.name);
                Assert.That(importer.mipmapEnabled, Is.False, sprite.name);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f), sprite.name);
                Assert.That(sprite.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), sprite.name);
            }
        }

        [Test]
        public void CircleIsSolidAndRingIsHollow()
        {
            UiKitAssets assets = UiKitAssets.Load();
            Assert.That(CentreAlpha(assets.Circle), Is.EqualTo(1f).Within(.01f));
            Assert.That(CentreAlpha(assets.Ring), Is.EqualTo(0f).Within(.01f));
            Assert.That(assets.Circle.rect.width, Is.EqualTo(UiKitSpriteBaker.CircleDiameter));
        }

        [Test]
        public void BakingTwiceKeepsTheSameSpriteGuids()
        {
            string before = AssetDatabase.AssetPathToGUID(UiKitSpriteBaker.SpriteDir + "/Circle.png");
            UiKitSpriteBaker.Bake();
            Assert.That(AssetDatabase.AssetPathToGUID(UiKitSpriteBaker.SpriteDir + "/Circle.png"), Is.EqualTo(before));
            Assert.That(UiKitAssets.Load().Circle, Is.Not.Null);
        }

        static float CentreAlpha(Sprite sprite)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
            float alpha = texture.GetPixel(texture.width / 2, texture.height / 2).a;
            Object.DestroyImmediate(texture);
            return alpha;
        }
    }
}
#endif
