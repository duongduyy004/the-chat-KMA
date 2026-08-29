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
        public void RealInputAssemblyIsSeparateFromLegacyGameplayAssembly()
        {
            var assembly = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(
                "Assets/_Project/Scripts/Input/KMA.Input.asmdef");
            Assert.That(assembly, Is.Not.Null);
            StringAssert.DoesNotContain("KMA.Gameplay", assembly.text);
        }
    }
}
