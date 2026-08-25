using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.Boss;
using KMA.Gameplay.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class PunishmentRouteTests : InputTestFixture
    {
        Keyboard testKeyboard;

        public override void Setup()
        {
            base.Setup();
            BossSceneSessionHandoff.ClearPendingSession();
        }

        public override void TearDown()
        {
            if (testKeyboard != null && testKeyboard.added)
                InputSystem.RemoveDevice(testKeyboard);
            testKeyboard = null;

            foreach (var router in Object.FindObjectsByType<SceneRouter>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(router.gameObject);
            }

            BossSceneSessionHandoff.ClearPendingSession();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator KeyboardInput_CompletesLivePunishmentAndRoutesSprintRetry()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            Assert.That(router.StartSubject(SubjectId.Sprint), Is.True);
            yield return WaitForScene("MG_Sprint");

            Assert.That(router.SubmitSubjectResult(SubjectId.Sprint,
                new MinigameResult(false, 0f, Rank.F)), Is.True);
            yield return WaitForScene("Punishment");
            Assert.That(router.Session.PendingPunishmentSubject, Is.EqualTo(SubjectId.Sprint));

            testKeyboard = InputSystem.AddDevice<Keyboard>();
            for (var tap = 0; tap < 3; tap++)
                yield return PressKey(Key.Space);
            yield return HoldKey(Key.H, .51f);
            yield return PressKey(Key.LeftArrow);
            yield return PressKey(Key.RightArrow);

            yield return new WaitForSeconds(.25f);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MG_Sprint"));
            Assert.That(router.Session.PendingPunishmentSubject, Is.Null);
        }

        static IEnumerator WaitForScene(string sceneName)
        {
            while (SceneManager.GetActiveScene().name != sceneName)
                yield return null;
            yield return null;
        }

        IEnumerator PressKey(Key key)
        {
            Press(testKeyboard[key], queueEventOnly: true);
            yield return null;
            Release(testKeyboard[key], queueEventOnly: true);
            yield return null;
        }

        IEnumerator HoldKey(Key key, float duration)
        {
            Press(testKeyboard[key], queueEventOnly: true);
            yield return null;
            yield return new WaitForSeconds(duration);
            Release(testKeyboard[key], queueEventOnly: true);
            yield return null;
        }
    }
}
