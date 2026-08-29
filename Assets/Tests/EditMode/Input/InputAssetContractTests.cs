using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace KMA.Tests.Input
{
    public sealed class InputAssetContractTests
    {
        const string AssetPath = "Assets/_Project/Settings/Input/KMA.inputactions";

        [Test]
        public void SharedInputAssetDeclaresExactlyTheFiveS3Maps()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.actionMaps.Select(map => map.name), Is.EquivalentTo(new[]
            {
                "Sprint", "Endurance", "Boss", "Punishment", "UI"
            }));
        }

        [Test]
        public void SharedInputAssetDeclaresRequiredActionsAndMeaningfulBindings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            Assert.That(asset, Is.Not.Null);

            Assert.That(asset.FindActionMap("Sprint").actions.Select(action => action.name),
                Is.EquivalentTo(new[] { "SprintLeft", "SprintRight", "TouchPosition" }));
            Assert.That(asset.FindActionMap("Endurance").actions.Select(action => action.name),
                Is.EquivalentTo(new[] { "Tap", "Hold", "SwipeUp", "SwipeDown", "TouchPosition" }));
            Assert.That(asset.FindActionMap("Boss").actions.Select(action => action.name),
                Is.EquivalentTo(new[] { "Tap", "Hold", "Left", "Right" }));
            Assert.That(asset.FindActionMap("Punishment").actions.Select(action => action.name),
                Is.EquivalentTo(new[] { "Tap", "Hold", "Left", "Right" }));
            Assert.That(asset.FindActionMap("UI").actions.Select(action => action.name),
                Is.EquivalentTo(new[] { "Navigate", "Submit", "Cancel", "Pause" }));

            Assert.That(asset.actionMaps.SelectMany(map => map.bindings)
                .Where(binding => !binding.isComposite)
                .All(binding => !string.IsNullOrWhiteSpace(binding.effectivePath)), Is.True);
        }

        [Test]
        public void SprintDirectionUsesKeyboardAndSharedTouchPositionInsteadOfAmbiguousPressBindings()
        {
            var sprint = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath).FindActionMap("Sprint");
            var left = sprint.FindAction("SprintLeft");
            var right = sprint.FindAction("SprintRight");
            var touchPosition = sprint.FindAction("TouchPosition");

            Assert.That(left.bindings.Select(binding => binding.effectivePath),
                Is.EquivalentTo(new[] { "<Keyboard>/leftArrow" }));
            Assert.That(right.bindings.Select(binding => binding.effectivePath),
                Is.EquivalentTo(new[] { "<Keyboard>/rightArrow" }));
            Assert.That(left.bindings.Concat(right.bindings)
                .Any(binding => binding.effectivePath.Contains("<Touchscreen>/")), Is.False);
            Assert.That(touchPosition.bindings.Select(binding => binding.effectivePath),
                Is.EquivalentTo(new[] { "<Touchscreen>/primaryTouch/position" }));
        }

        [Test]
        public void RealInputAssemblyIsSeparateFromLegacyGameplayAssembly()
        {
            var assembly = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(
                "Assets/_Project/Scripts/Input/KMA.Input.asmdef");
            Assert.That(assembly, Is.Not.Null);
            StringAssert.DoesNotContain("KMA.Gameplay", assembly.text);
        }
    }
}
