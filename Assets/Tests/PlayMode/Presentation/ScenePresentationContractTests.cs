using System.Collections;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class ScenePresentationContractTests
    {
        static readonly string[] SceneNames =
        {
            "MG_Sprint",
            "MG_Endurance",
            "MG_Boss",
            "Punishment",
            "Map",
            "GameOver"
        };

        [UnityTest]
        public IEnumerator EveryExistingSceneHasS2CameraAndCanvas()
        {
            foreach (var sceneName in SceneNames)
            {
                if (sceneName == "Punishment")
                    LogAssert.Expect(LogType.Error, "Punishment requires a pending subject from the live GameSession.");

                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

                Assert.That(Camera.main, Is.Not.Null, sceneName);
                Assert.That(Camera.main.orthographic, Is.True, sceneName);
                Assert.That(Camera.main.GetComponent("UniversalAdditionalCameraData"), Is.Not.Null, sceneName);

                var canvas = Object.FindFirstObjectByType<Canvas>();
                Assert.That(canvas, Is.Not.Null, sceneName);

                var scaler = Object.FindFirstObjectByType<CanvasScaler>();
                Assert.That(scaler, Is.Not.Null, sceneName);
                Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(1f).Within(.001f), sceneName);

                Assert.That(Object.FindFirstObjectByType<SafeAreaFitter>(), Is.Not.Null, sceneName);
            }
        }
    }
}
