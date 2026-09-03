using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class FontAssetStabilityTests
    {
        private const string PrimaryFontPath = "Assets/_Project/Fonts/Baloo2-ExtraBold.asset";

        [Test]
        [Category("FontAsset")]
        public void PrimaryFontKeepsStableAssetAndMaterialNames()
        {
            var font = AssetDatabase.LoadMainAssetAtPath(PrimaryFontPath);
            Assert.That(font, Is.Not.Null, PrimaryFontPath);
            Assert.That(font.name, Is.EqualTo("Baloo2-ExtraBold"));

            var material = font.GetType()
                .GetProperty("material", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(font) as Object;
            Assert.That(material, Is.Not.Null, "Baloo2-ExtraBold material");
            Assert.That(material.name, Is.EqualTo("Baloo2-ExtraBold Material"));
        }

        [Test]
        [Category("FontAsset")]
        public void PrimaryFontUsesNonMutatingAtlasPopulation()
        {
            var font = AssetDatabase.LoadMainAssetAtPath(PrimaryFontPath);
            Assert.That(font, Is.Not.Null, PrimaryFontPath);

            var populationMode = font.GetType()
                .GetProperty("atlasPopulationMode", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(font);
            Assert.That(populationMode, Is.Not.Null, "Baloo2-ExtraBold atlas population mode");
            Assert.That(populationMode.ToString(), Is.EqualTo("Static"));
        }
    }
}
