using KMA.Gameplay.UI;
using KMA.UI.Kit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class ShellButtonFamilyTests
    {
        [Test]
        public void BrutalPrefabIsGone() =>
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/Btn_Brutal.prefab"), Is.Null);

        [Test]
        public void HomeMenuDisabledColoursComeFromTheTheme()
        {
            UITheme.MenuStyle menu = UITheme.Shared.Menu;
            Assert.That(menu.disabledBorder.a, Is.EqualTo(.24f).Within(.01f));
            Assert.That(menu.disabledText.a, Is.EqualTo(.42f).Within(.01f));
        }
    }
}
