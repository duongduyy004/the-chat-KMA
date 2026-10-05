using System.Collections;
using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class MinigamePauseNavigationTests
    {
        [UnityTest] public IEnumerator SprintExitFromDirectScene() => ExitToMap("MG_Sprint", 0, false);
        [UnityTest] public IEnumerator VolleyballExitFromDirectScene() => ExitToMap("MG_Volleyball", 3, false);
        [UnityTest] public IEnumerator FootballExitFromDirectScene() => ExitToMap("MG_Football", 6, false);
        [UnityTest] public IEnumerator SprintLearnExitFromJourney() => ExitToMap("MG_Sprint", 0, true);
        [UnityTest] public IEnumerator SprintPracticeExitFromJourney() => ExitToMap("MG_Sprint", 1, true);
        [UnityTest] public IEnumerator SprintExamExitFromJourney() => ExitToMap("MG_Sprint", 2, true);
        [UnityTest] public IEnumerator VolleyballLearnExitFromJourney() => ExitToMap("MG_Volleyball", 3, true);
        [UnityTest] public IEnumerator VolleyballPracticeExitFromJourney() => ExitToMap("MG_Volleyball", 4, true);
        [UnityTest] public IEnumerator VolleyballExamExitFromJourney() => ExitToMap("MG_Volleyball", 5, true);
        [UnityTest] public IEnumerator FootballLearnExitFromJourney() => ExitToMap("MG_Football", 6, true);
        [UnityTest] public IEnumerator FootballPracticeExitFromJourney() => ExitToMap("MG_Football", 7, true);
        [UnityTest] public IEnumerator FootballExamExitFromJourney() => ExitToMap("MG_Football", 8, true);

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            DestroyAll<GameManager>();
            DestroyAll<SceneRouter>();
            // Bootstrap tests can leave a persistent splash while interrupting its load.
            // These tests begin at a minigame and must own their pointer environment.
            DestroyAll<SplashScreenPresenter>();
        }

        IEnumerator ExitToMap(string sceneName, int completed, bool journey)
        {
            SceneRouter router = null;
            SaveData persisted = null;
            string checkpoint = null;
            if (journey)
            {
                router = SceneRouter.EnsurePersistentInstance();
                for (int i = 0; i < completed; i++)
                    JourneyGameplayDriver.CompleteActiveChallenge(router.Session);
                checkpoint = router.Session.Journey.CheckpointChallengeId;
                router.ConfigureJourneyPersistence((out string error) =>
                {
                    persisted = router.Session.ToSaveData();
                    error = null;
                    return true;
                });
                Assert.That(router.TryStartChallenge(checkpoint, difficulty:
                    router.Session.Journey.Catalog.Get(checkpoint).Difficulty), Is.True);
                yield return JourneyRuntimeDriver.WaitForScene(router, sceneName);
            }
            else
            {
                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                yield return null;
            }

            var pause = Object.FindFirstObjectByType<PausePanel>();
            Assert.That(pause, Is.Not.Null, sceneName);
            pause.GetComponent<Button>().onClick.Invoke();
            Assert.That(pause.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            yield return null;
            Canvas.ForceUpdateCanvases();
            // PauseMenu is a sibling of PausePanel, including Sprint's runtime chrome.
            var exit = pause.transform.parent.Find("PauseMenu/PauseCard/ExitButton")?.GetComponent<Button>();
            Assert.That(exit, Is.Not.Null);
            var canvas = exit.GetComponentInParent<Canvas>();
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    ((RectTransform)exit.transform).TransformPoint(((RectTransform)exit.transform).rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, sceneName + " exit must receive pointer input.");
            var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.That(target, Is.EqualTo(exit.gameObject), sceneName + " exit is blocked by " +
                hits[0].gameObject.name + " in " + hits[0].gameObject.scene.name +
                " at " + pointer.position + "; canvas=" + canvas.renderMode +
                "; camera=" + canvas.worldCamera);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            router = SceneRouter.Instance;
            Assert.That(router, Is.Not.Null, "Direct scene play must provide routing when exiting.");
            yield return JourneyRuntimeDriver.WaitForScene(router, "Map");
            Assert.That(router.Session.ActiveSubject, Is.Null);
            Assert.That(router.Session.Journey.ActiveAttempt, Is.Null);
            Assert.That(router.Session.Lives, Is.EqualTo(5));
            Assert.That(Object.FindFirstObjectByType<MapScreen>(), Is.Not.Null);
            if (journey)
            {
                Assert.That(router.Session.Journey.CheckpointChallengeId, Is.EqualTo(checkpoint));
                var reloaded = new GameSession();
                reloaded.Restore(persisted);
                Assert.That(reloaded.ActiveSubject, Is.Null);
                Assert.That(reloaded.Journey.CheckpointChallengeId, Is.EqualTo(checkpoint));
                Assert.That(router.TryStartChallenge(checkpoint, difficulty:
                    router.Session.Journey.Catalog.Get(checkpoint).Difficulty), Is.True,
                    "Returning to the map must allow the unfinished lesson to start again.");
                yield return JourneyRuntimeDriver.WaitForScene(router, sceneName);
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Map", LoadSceneMode.Single);
            DestroyAll<GameManager>();
            DestroyAll<SceneRouter>();
        }

        static void DestroyAll<T>() where T : Component
        {
            foreach (var component in Object.FindObjectsByType<T>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(component.gameObject);
        }
    }
}
