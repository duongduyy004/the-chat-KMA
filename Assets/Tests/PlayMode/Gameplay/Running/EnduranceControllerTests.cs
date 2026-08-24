using System.Collections;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
        public IEnumerator InputActions_DispatchTapHoldAndSwipeThroughRuntimeCallbacks()
        {
            var controller = CreateController();
            var bridgeObject = new GameObject("FullScreenGameplayInput");
            var bridge = bridgeObject.AddComponent<EnduranceInputBridge>();
            var actions = CreateInputActions();
            bridge.ConfigureForTest(controller, actions);
            Assert.That(bridge.InputActionsReady, Is.True);
            ReleaseGamepad();

            controller.Dispatch(new AuthoredBeat(BeatEvent.Tap));
            Press(GamepadButton.South);
            yield return null;
            ReleaseGamepad();
            Assert.That(controller.InputTapCount, Is.EqualTo(1));

            controller.Dispatch(new AuthoredBeat(BeatEvent.Breath));
            Press(GamepadButton.North);
            yield return new WaitForSeconds(0.05f);
            ReleaseGamepad();
            Assert.That(controller.InputHoldCount, Is.EqualTo(1));

            controller.Dispatch(new AuthoredBeat(BeatEvent.Jump));
            Press(GamepadButton.DpadUp);
            yield return null;
            ReleaseGamepad();
            Assert.That(controller.InputSwipeCount, Is.EqualTo(1));
            Assert.That(controller.Rules.ObstacleCleared, Is.True);

            Object.Destroy(bridgeObject);
            Object.Destroy(controller.gameObject);
            Object.Destroy(actions);
            yield return null;
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
            Assert.That(controller.MetronomeAudioSource, Is.Not.Null);
            Assert.That(controller.MetronomeAudioSource.clip, Is.Not.Null);

            controller.ConfigureLifecycleForTest(0f, 0f, 3);
            controller.AdvanceToPlayForTest();
            controller.Simulate(0f);
            Assert.That(controller.DspClockScheduled, Is.True);
            Assert.That(controller.MetronomeStartDspTime, Is.GreaterThan(0d));

            yield return null;
        }

        static EnduranceController CreateController()
        {
            var controller = new GameObject("EnduranceController").AddComponent<EnduranceController>();
            controller.ConfigureForTest(1);
            controller.AdvanceToPlayForTest();
            return controller;
        }

        static void DestroyController(EnduranceController controller) => Object.Destroy(controller.gameObject);

        static InputActionAsset CreateInputActions()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = asset.AddActionMap("Endurance");
            AddKeyboardAction(map, "Tap", "<Gamepad>/buttonSouth");
            AddKeyboardAction(map, "Hold", "<Gamepad>/buttonNorth", "Hold(duration=0.01)");
            AddKeyboardAction(map, "SwipeUp", "<Gamepad>/dpad/up");
            AddKeyboardAction(map, "SwipeDown", "<Gamepad>/dpad/down");
            return asset;
        }

        static void AddKeyboardAction(InputActionMap map, string name, string binding, string interactions = null)
        {
            var action = map.AddAction(name, InputActionType.Button);
            action.AddBinding(binding, interactions: interactions);
        }

        static void Press(GamepadButton button)
        {
            var gamepad = Gamepad.current ?? InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(gamepad, new GamepadState(button));
            InputSystem.Update();
        }

        static void ReleaseGamepad()
        {
            var gamepad = Gamepad.current ?? InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            InputSystem.Update();
        }
    }
}
