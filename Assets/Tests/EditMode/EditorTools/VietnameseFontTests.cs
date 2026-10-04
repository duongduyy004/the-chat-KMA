using System;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using KMA.EditorTools;
using KMA.Gameplay.UI;

namespace KMA.Tests.EditorTools
{
    public sealed class VietnameseFontTests
    {
        const string Sample = "Thể Chất KMA – Nguyễn Quỳnh Ầ Ẫ Ệ Ộ Ữ Ứ Ằ Ẳ Ự Ị Ọ Ổ ơ ư đ Đ";

        [TestCase("SairaCondensed-Black", AtlasPopulationMode.Static, 2048)]
        [TestCase("BarlowSemiCondensed-Bold", AtlasPopulationMode.Static, 2048)]
        [TestCase("BeVietnamPro-Regular", AtlasPopulationMode.Dynamic, 2048)]
        [TestCase("BeVietnamPro-Bold", AtlasPopulationMode.Dynamic, 2048)]
        public void RequestedFontCoversVietnamese(string name, AtlasPopulationMode mode, int size)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"Assets/Fonts/TMP/{name}.asset");
            Assert.That(font, Is.Not.Null, "Setup must create the requested font asset.");
            Assert.That(font.atlasPopulationMode, Is.EqualTo(mode));
            Assert.That(font.atlasWidth, Is.EqualTo(size));
            Assert.That(font.atlasPadding, Is.EqualTo(9));
            foreach (char c in Sample.Distinct())
                Assert.That(font.HasCharacter(c, false, true), Is.True, $"{name}: {c} U+{(int)c:X4}");
            Assert.That(font.isMultiAtlasTexturesEnabled, Is.EqualTo(mode == AtlasPopulationMode.Dynamic));
        }

        [Test]
        public void SettingsKeepWarningsEnabledAndUseVietnameseDefault()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/Resources/TMP Settings.asset");
            var serialized = new SerializedObject(settings);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/BeVietnamPro-Regular.asset");
            Assert.That(font, Is.Not.Null);
            Assert.That(serialized.FindProperty("m_defaultFontAsset").objectReferenceValue, Is.EqualTo(font));
            Assert.That(serialized.FindProperty("m_warningsDisabled").boolValue, Is.False);
        }

        [Test]
        public void NormalizationComposesVietnameseWithoutChangingNullOrEmpty()
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("VietText"))
                .FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, "VietText normalization is missing.");
            var fix = type.GetMethod("Fix");
            Assert.That(fix.Invoke(null, new object[] { "Nguye\u0302\u0303n" }), Is.EqualTo("Nguyễn"));
            Assert.That(fix.Invoke(null, new object[] { null }), Is.Null);
            Assert.That(fix.Invoke(null, new object[] { "" }), Is.EqualTo(""));
        }

        [Test]
        public void AllVietnamesePrecomposedLettersAreAvailableInEveryFont()
        {
            foreach (string name in new[] { "SairaCondensed-Black", "BarlowSemiCondensed-Bold", "BeVietnamPro-Regular", "BeVietnamPro-Bold" })
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"Assets/Fonts/TMP/{name}.asset");
                for (char c = '\u1EA0'; c <= '\u1EF9'; c++)
                    Assert.That(font.HasCharacter(c, false, true), Is.True, $"{name}: U+{(int)c:X4}");
            }
        }

        [Test]
        public void BodyAndTitleMaterialsBindTheCorrectAtlasAndKeepStackedAccentsSafe()
        {
            var fonts = VietTypography.Library;
            foreach (var pair in new[] { (fonts.title, fonts.titleMaterial), (fonts.buttonHud, fonts.primaryMaterial),
                (fonts.buttonHud, fonts.secondaryMaterial), (fonts.regular, fonts.bodyMaterial), (fonts.bold, fonts.bodyBoldMaterial) })
                Assert.That(pair.Item2.GetTexture(ShaderUtilities.ID_MainTex) == pair.Item1.atlasTexture, Is.True,
                    pair.Item2.name + ": " + AssetDatabase.GetAssetPath(pair.Item2.GetTexture(ShaderUtilities.ID_MainTex)) +
                    " vs " + AssetDatabase.GetAssetPath(pair.Item1.atlasTexture));
            Assert.That(fonts.titleMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth), Is.EqualTo(.15f).Within(.001f));
            Assert.That(fonts.primaryMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth), Is.EqualTo(.1f).Within(.001f));
            Assert.That(fonts.bodyMaterial.IsKeywordEnabled("UNDERLAY_ON"), Is.False);
            Assert.That(fonts.bodyMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth), Is.Zero);
        }

        [Test]
        public void ReapplyingRolesPreservesFontMaterialAndAutosizeBounds()
        {
            var root = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            try
            {
                var text = root.GetComponent<TMP_Text>();
                text.text = "Nguyễn Quỳnh Ầ Ẫ Ệ";
                VietTypography.Apply(text, VietFontRole.BodyBold);
                VietTypography.Apply(text);
                Assert.That(text.font, Is.SameAs(VietTypography.Library.bold));
                text.fontSize = 32;
                text.enableAutoSizing = true; text.fontSizeMin = 28; text.fontSizeMax = 32;
                VietTypography.Apply(text, VietFontRole.ButtonPrimary);
                VietTypography.Apply(text, VietFontRole.ButtonPrimary);
                Assert.That(text.fontSizeMin, Is.EqualTo(28));
                Assert.That(text.fontSizeMax, Is.EqualTo(32));
                Assert.That(text.extraPadding, Is.True);
                Assert.That(text.lineSpacing, Is.InRange(10f, 20f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void CurrentProjectCoverageHasNoMissingCharactersOrLegacyComponents()
        {
            var report = FontCoverageChecker.Scan();
            Assert.That(report.scenes, Is.EqualTo(AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).Length));
            Assert.That(report.missing, Is.Empty, string.Join("\n", report.missing));
            Assert.That(report.sourceCandidates, Is.Empty, string.Join("\n", report.sourceCandidates));
        }
    }
}
