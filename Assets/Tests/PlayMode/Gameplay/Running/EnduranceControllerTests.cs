using System.Collections;
using System.Reflection;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using GameplayInputRouter = KMA.Input.GameplayInputRouter;
using ScreenTapArea = KMA.Input.ScreenTapArea;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class EnduranceControllerTests
    {
        [UnityTest]
        public IEnumerator CalibratedInputTime_AppliesRhythmOffsetInSeconds()
        {
            var controller = CreateController();
            controller.RhythmOffsetMs = 125.0;

            Assert.That(controller.CalibratedInputTime(10.0), Is.EqualTo(10.125).Within(0.000001));
            controller.Tap(10.0, 10.125);
            Assert.That(controller.Rules.PerfectCount, Is.EqualTo(1));

            DestroyController(controller);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ObstacleIcon_AppearsTwoBeatsBeforeSwipeMode()
        {
            var schedule = new EnduranceCueSchedule(obstacleBeat: 8, warningBeats: 2);

            schedule.AdvanceToBeat(5);
            Assert.That(schedule.ObstacleCueVisible, Is.False);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));

            schedule.AdvanceToBeat(6);
            Assert.That(schedule.ObstacleCueVisible, Is.True);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));

            schedule.AdvanceToBeat(8);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.ObstacleSwipe));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ObstacleActivation_UsesAuthoredObstacleBeat()
        {
            var controller = CreateController();
            controller.ConfigurePatternForTest(new LapPattern(new[]
            {
                new AuthoredBeat(BeatEvent.Tap),
                new AuthoredBeat(BeatEvent.Tap),
                new AuthoredBeat(BeatEvent.Tap),
                new AuthoredBeat(BeatEvent.Jump)
            }));

            controller.AdvanceToBeatForTest(0);
            Assert.That(controller.ObstacleCueVisible, Is.False);
            controller.AdvanceToBeatForTest(1);
            Assert.That(controller.ObstacleCueVisible, Is.True);
            Assert.That(controller.Rules.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));
            controller.AdvanceToBeatForTest(3);
            Assert.That(controller.Rules.Mode, Is.EqualTo(EnduranceInputMode.ObstacleSwipe));

            DestroyController(controller);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WarningAndActivation_RespectExactBeatBoundaries()
        {
            var schedule = new EnduranceCueSchedule(obstacleBeat: 10, warningBeats: 2);

            schedule.AdvanceToBeat(7);
            Assert.That(schedule.ObstacleCueVisible, Is.False);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));

            schedule.AdvanceToBeat(8);
            Assert.That(schedule.ObstacleCueVisible, Is.True);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));

            schedule.AdvanceToBeat(9);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));

            schedule.AdvanceToBeat(10);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.ObstacleSwipe));

            yield return null;
        }

        [UnityTest]
        public IEnumerator SerializedSceneActions_DispatchTouchSwipeThroughRuntimeBridge()
        {
            var operation = SceneManager.LoadSceneAsync("MG_Endurance", LoadSceneMode.Single);
            while (!operation.isDone)
                yield return null;

            var controller = Object.FindObjectOfType<EnduranceController>();
            var bridge = Object.FindObjectOfType<EnduranceInputBridge>();
            Assert.That(bridge.InputActionsAsset, Is.Not.Null);
            Assert.That(bridge.InputActionsReady, Is.True);
            Assert.That(HasBinding(bridge.InputActionsAsset, "<Touchscreen>/primaryTouch/position"), Is.True);
            Assert.That(HasBinding(bridge.InputActionsAsset, "<Touchscreen>/primaryTouch/delta"), Is.True);
            Assert.That(HasBinding(bridge.InputActionsAsset, "<Touchscreen>/primaryTouch/press"), Is.True);

            controller.ConfigureLifecycleForTest(0f, 0f, 3);
            controller.AdvanceToPlayForTest();
            controller.Dispatch(new AuthoredBeat(BeatEvent.Jump));
            Assert.That(controller.Phase, Is.EqualTo(MinigamePhase.Play));
            Assert.That(controller.Rules.Mode, Is.EqualTo(EnduranceInputMode.ObstacleSwipe));
            var tapArea = Object.FindFirstObjectByType<ScreenTapArea>();
            Assert.That(tapArea, Is.Not.Null);
            var eventSystem = EventSystem.current ?? new GameObject("EnduranceInputEventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            var start = new Vector2(Screen.width * .5f, Screen.height * .5f);
            tapArea.OnPointerDown(PointerAt(eventSystem, start, 1));
            tapArea.OnDrag(PointerAt(eventSystem, start + Vector2.up * 160f, 1));
            tapArea.OnPointerUp(PointerAt(eventSystem, start + Vector2.up * 160f, 1));
            yield return null;

            Assert.That(controller.InputSwipeCount, Is.EqualTo(1));
            Assert.That(controller.Rules.ObstacleCleared, Is.True);
        }

        [UnityTest]
        public IEnumerator EnduranceScene_IsBuildRegisteredAndStartsDspMetronome()
        {
            var operation = SceneManager.LoadSceneAsync("MG_Endurance", LoadSceneMode.Single);
            while (!operation.isDone)
                yield return null;

            var scene = SceneManager.GetActiveScene();
            Assert.That(scene.path, Is.EqualTo("Assets/_Project/Scenes/MG_Endurance.unity"));
            var controller = Object.FindObjectOfType<EnduranceController>();
            var inputSurface = GameObject.Find("FullScreenGameplayInput");
            Assert.That(controller, Is.Not.Null);
            Assert.That(inputSurface, Is.Not.Null);
            Assert.That(inputSurface.GetComponent<EnduranceInputBridge>(), Is.Not.Null);
            var tapAreas = Object.FindObjectsByType<ScreenTapArea>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.That(tapAreas, Has.Length.EqualTo(1), "Endurance must serialize one gameplay pointer surface.");
            var tapArea = tapAreas[0];
            Assert.That(tapArea.gameObject, Is.SameAs(inputSurface));
            Assert.That(tapArea.GetComponent<UnityEngine.UI.Graphic>(), Is.Not.Null);
            Assert.That(tapArea.GetComponent<UnityEngine.UI.Graphic>().raycastTarget, Is.True);
            Assert.That(PrivateField<GameplayInputRouter>(tapArea, "router"), Is.SameAs(inputSurface.GetComponent<GameplayInputRouter>()));
            Assert.That(PrivateField<RectTransform>(tapArea, "gameplayArea"), Is.SameAs(inputSurface.GetComponent<RectTransform>()));
            Assert.That(controller.MetronomeAudioSource, Is.Not.Null);
            Assert.That(controller.MetronomeAudioSource.clip, Is.Not.Null);

            controller.ConfigureLifecycleForTest(0f, 0f, 3);
            controller.AdvanceToPlayForTest();
            controller.Simulate(0f);
            Assert.That(controller.DspClockScheduled, Is.True);
            Assert.That(controller.MetronomeStartDspTime, Is.GreaterThan(0d));

            yield return null;
        }

        [UnityTest]
        public IEnumerator InputModes_MutateOnlyTheirMatchingRuleMetric()
        {
            var controller = CreateController();

            controller.Dispatch(new AuthoredBeat(BeatEvent.Tap));
            controller.Tap(10.1d, 10d);
            int judgedAfterTap = controller.Rules.JudgedCount;
            float staminaAfterTap = controller.Rules.Stamina;
            controller.EndHold(1f);
            controller.Swipe(SwipeDirection.Up);
            Assert.That(controller.Rules.JudgedCount, Is.EqualTo(judgedAfterTap));
            Assert.That(controller.Rules.Stamina, Is.EqualTo(staminaAfterTap));
            Assert.That(controller.Rules.ObstacleCleared, Is.False);

            controller.Dispatch(new AuthoredBeat(BeatEvent.Breath));
            controller.Tap(10d, 10d);
            controller.Swipe(SwipeDirection.Up);
            controller.EndHold(1f);
            Assert.That(controller.Rules.JudgedCount, Is.EqualTo(judgedAfterTap));
            Assert.That(controller.Rules.Stamina, Is.EqualTo(100f));
            Assert.That(controller.Rules.ObstacleCleared, Is.False);

            controller.Dispatch(new AuthoredBeat(BeatEvent.Jump));
            controller.Tap(10d, 10d);
            controller.EndHold(1f);
            controller.Swipe(SwipeDirection.Up);
            Assert.That(controller.Rules.JudgedCount, Is.EqualTo(judgedAfterTap));
            Assert.That(controller.Rules.Stamina, Is.EqualTo(100f));
            Assert.That(controller.Rules.ObstacleCleared, Is.True);

            DestroyController(controller);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PausingDspClock_IsIdempotentAndResumesFromPausedBeat()
        {
            var controller = CreateController();
            controller.Simulate(0f);
            var metronome = controller.MetronomeAudioSource;
            double pausedBeat = controller.CurrentBeatDspTime;

            controller.SetPaused(true);
            controller.SetPaused(true);
            yield return new WaitForSecondsRealtime(.05f);

            Assert.That(controller.CurrentBeatDspTime, Is.EqualTo(pausedBeat).Within(.000001d));
            Assert.That(controller.MetronomeAudioSource, Is.SameAs(metronome));
            Assert.That(controller.DspClockScheduled, Is.True);

            controller.SetPaused(false);
            controller.Simulate(0f);
            Assert.That(controller.CurrentBeatDspTime, Is.EqualTo(pausedBeat).Within(controller.BeatIntervalSeconds));
            Assert.That(controller.MetronomeAudioSource, Is.SameAs(metronome));

            DestroyController(controller);
        }

        static EnduranceController CreateController()
        {
            var controller = new GameObject("EnduranceController").AddComponent<EnduranceController>();
            controller.ConfigureForTest(1);
            controller.AdvanceToPlayForTest();
            return controller;
        }

        static void DestroyController(EnduranceController controller) => Object.Destroy(controller.gameObject);

        static bool HasBinding(InputActionAsset asset, string path)
        {
            foreach (var map in asset.actionMaps)
                foreach (var binding in map.bindings)
                    if (binding.effectivePath == path || binding.path == path)
                        return true;
            return false;
        }

        static PointerEventData PointerAt(EventSystem eventSystem, Vector2 position, int pointerId)
        {
            return new PointerEventData(eventSystem)
            {
                position = position,
                pointerId = pointerId
            };
        }

        static T PrivateField<T>(ScreenTapArea tapArea, string name) where T : class
        {
            var field = typeof(ScreenTapArea).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"ScreenTapArea must retain serialized {name} wiring.");
            return field.GetValue(tapArea) as T;
        }
    }
}
