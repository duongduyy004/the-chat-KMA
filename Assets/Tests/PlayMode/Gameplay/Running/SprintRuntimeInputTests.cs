using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class SprintRuntimeInputTests
    {
        GameObject routerObject;
        GameObject controllerObject;
        InputActionAsset actions;
        Keyboard keyboard;

        [SetUp]
        public void Setup()
        {
            routerObject = new GameObject("GameplayInputRouter");
            controllerObject = new GameObject("SprintController");
        }

        [TearDown]
        public void TearDown()
        {
            if (keyboard != null)
                InputSystem.RemoveDevice(keyboard);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(routerObject);
            if (actions != null)
                Object.DestroyImmediate(actions);
        }

        [Test]
        public void SprintActions_RouteValidAlternationToControllerOncePerTap()
        {
            var controller = controllerObject.AddComponent<KMA.Gameplay.SprintController>();
            controller.ConfigureForTest(.8f);
            actions = new InputActionAsset();
            var map = actions.AddActionMap("Sprint");
            var left = map.AddAction("SprintLeft", InputActionType.Button, "<Keyboard>/leftArrow");
            var right = map.AddAction("SprintRight", InputActionType.Button, "<Keyboard>/rightArrow");
            var router = routerObject.AddComponent<KMA.Input.GameplayInputRouter>();
            controller.ConfigureInputRouterForTest(router);
            router.ConfigureSprintForTest(actions);
            keyboard = InputSystem.AddDevice<Keyboard>();

            var before = controller.Snapshot;
            Tap(keyboard.leftArrowKey);
            controller.Simulate(.1f);
            var afterLeft = controller.Snapshot;

            Assert.That(afterLeft.Distance, Is.GreaterThan(before.Distance));
            Assert.That(controller.ExpectedSide, Is.EqualTo(KMA.Gameplay.Side.Right));
            var speedBeforeWrongTap = controller.Snapshot.Speed;

            Tap(keyboard.leftArrowKey);
            Assert.That(controller.ExpectedSide, Is.EqualTo(KMA.Gameplay.Side.Right));
            Assert.That(controller.Snapshot.Speed, Is.EqualTo(speedBeforeWrongTap));

            var speedBeforeRightTap = controller.Snapshot.Speed;
            Tap(keyboard.rightArrowKey);
            Assert.That(controller.Snapshot.Speed, Is.EqualTo(speedBeforeRightTap + 18f));
            controller.Simulate(.1f);
            Assert.That(controller.ExpectedSide, Is.EqualTo(KMA.Gameplay.Side.Left));
            Assert.That(controller.Snapshot.Distance, Is.GreaterThan(afterLeft.Distance));
            Assert.That(left.bindings[0].path, Is.EqualTo("<Keyboard>/leftArrow"));
            Assert.That(right.bindings[0].path, Is.EqualTo("<Keyboard>/rightArrow"));
        }

        static void Tap(KeyControl key)
        {
            InputSystem.QueueDeltaStateEvent(key, 1f);
            InputSystem.Update();
            InputSystem.QueueDeltaStateEvent(key, 0f);
            InputSystem.Update();
        }
    }
}
