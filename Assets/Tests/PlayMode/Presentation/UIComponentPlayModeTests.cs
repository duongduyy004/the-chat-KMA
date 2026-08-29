using System.Linq;
using System.Reflection;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class UIComponentPlayModeTests
    {
        [Test]
        public void HudPrefabSatisfiesSharedPresentationContract()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/HUD_Minigame.prefab");
            Assert.That(prefab, Is.Not.Null);

            Assert.That(prefab.GetComponentsInChildren<Canvas>(true), Has.Length.EqualTo(1));
            var scaler = prefab.GetComponentInChildren<CanvasScaler>(true);
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(1f).Within(.001f));

            var fitter = prefab.GetComponentInChildren<SafeAreaFitter>(true);
            var hud = prefab.GetComponentInChildren<MinigameHUD>(true);
            var heartBar = prefab.GetComponentInChildren<HeartBar>(true);
            Assert.That(fitter, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(heartBar, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true), Has.None.Null);
            Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true)
                .Any(component => component.GetType().GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic) != null), Is.False);
            AssertRequiredHudReferences(hud);
        }

        private static void AssertRequiredHudReferences(MinigameHUD hud)
        {
            var serializedHud = new SerializedObject(hud);
            foreach (var propertyName in new[]
                     {
                         "theme", "timeLabel", "phaseLabel", "scoreLabel", "statusLabel", "progressFill", "staminaFill"
                     })
            {
                var property = serializedHud.FindProperty(propertyName);
                Assert.That(property, Is.Not.Null, propertyName);
                Assert.That(property.objectReferenceValue, Is.Not.Null, propertyName);
            }
        }
    }
}
