using System.Collections;
using KMA.Gameplay;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Football
{
    public sealed class FootballSceneTests
    {
        const string SceneName = "MG_Football";

        [UnityTest]
        public IEnumerator SavedSceneContainsPlayableSourcedFootballPresentation()
        {
            var operation = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone) yield return null;
            var scene = SceneManager.GetSceneByName(SceneName);
            Assert.That(scene.IsValid(), Is.True);
            int controllers = 0, placeholders = 0, results = 0, owners = 0;
            FootballPresentation presentation = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                controllers += root.GetComponentsInChildren<FootballController>(true).Length;
                placeholders += root.GetComponentsInChildren<PlaceholderMinigameController>(true).Length;
                results += root.GetComponentsInChildren<MonoBehaviour>(true).OfTypeResultPanels();
                owners += root.GetComponentsInChildren<MinigamePresentationOwner>(true).Length;
                presentation ??= root.GetComponentInChildren<FootballPresentation>(true);
            }
            Assert.That(controllers, Is.EqualTo(1));
            Assert.That(placeholders, Is.Zero);
            Assert.That(results, Is.EqualTo(1));
            Assert.That(owners, Is.EqualTo(1));
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.ValidateReferences(), Is.True);
            Assert.That(presentation.GetComponentsInChildren<SpriteRenderer>(true), Is.Not.Empty);
            var eventSystem = System.Array.Find(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), item => item.gameObject.scene == scene);
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(eventSystem.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
            var text = System.Array.Find(UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), item => item.gameObject.scene == scene);
            Assert.That(text, Is.Not.Null);
            Assert.That(text.font, Is.Not.Null);
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }

    static class FootballSceneTestExtensions
    {
        public static int OfTypeResultPanels(this MonoBehaviour[] components)
        {
            int count = 0;
            foreach (var component in components) if (component is IResultPreviewPanel) count++;
            return count;
        }
    }
}
