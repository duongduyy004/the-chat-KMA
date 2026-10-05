using System.Linq;
using KMA.EditorTools;
using KMA.Gameplay.FrogJump;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class FrogJumpSceneTests
    {
        [Test]
        public void SceneIsInBuildSettingsAndWired()
        {
            Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == FrogJumpSceneConfigurator.ScenePath));
            EditorSceneManager.OpenScene(FrogJumpSceneConfigurator.ScenePath);
            var controller = Object.FindFirstObjectByType<FrogJumpController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsWired, Is.True);
            Assert.That(Object.FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PausePanel>(FindObjectsInactive.Include), Is.Not.Null);
        }
    }
}
