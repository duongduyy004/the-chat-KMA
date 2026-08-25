using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class GameplayPresentationTests
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
        public IEnumerator GameplayScenesHaveVisiblePresentation()
        {
            foreach (var sceneName in SceneNames)
            {
                if (sceneName == "Punishment")
                    LogAssert.Expect(LogType.Error, "Punishment requires a pending subject from the live GameSession.");

                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

                Assert.That(Camera.main, Is.Not.Null, $"{sceneName} needs a tagged Main Camera.");
                var presentation = GameObject.Find("GameplayPresentation");
                Assert.That(presentation, Is.Not.Null, $"{sceneName} needs a GameplayPresentation object.");
                Assert.That(presentation.GetComponent("GameplayPresentation"), Is.Not.Null,
                    $"{sceneName} needs a visible gameplay presentation.");
            }
        }
    }
}
