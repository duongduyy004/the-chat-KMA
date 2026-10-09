using System.Collections;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballInputBridgeTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root)
                Object.Destroy(root);
        }

        VolleyballInputBridge CreateBridge(out VirtualJoystick joystick, out ActionButton button) =>
            CreateBridge(out joystick, out button, out _);

        VolleyballInputBridge CreateBridge(out VirtualJoystick joystick, out ActionButton button, out JumpButton jump)
        {
            root = new GameObject("Bridge", typeof(RectTransform));
            root.SetActive(false);
            joystick = root.AddComponent<VirtualJoystick>();
            var stickBase = new GameObject("Base", typeof(RectTransform)).transform as RectTransform;
            var knob = new GameObject("Knob", typeof(RectTransform)).transform as RectTransform;
            stickBase.SetParent(root.transform, false);
            knob.SetParent(root.transform, false);
            joystick.Configure((RectTransform)root.transform, stickBase, knob, 100f, Vector2.zero);
            button = root.AddComponent<ActionButton>();
            jump = new GameObject("Jump", typeof(RectTransform)).AddComponent<JumpButton>();
            jump.transform.SetParent(root.transform, false);
            var bridge = root.AddComponent<VolleyballInputBridge>();
            bridge.Configure(joystick, button, jump);
            root.SetActive(true);
            return bridge;
        }

        [UnityTest]
        public IEnumerator ButtonPressesQueueUntilConsumed()
        {
            VolleyballInputBridge bridge = CreateBridge(out _, out ActionButton button);
            yield return null;

            button.Press();
            button.Press();
            Assert.That(bridge.ConsumePresses(), Is.EqualTo(2));
            Assert.That(bridge.ConsumePresses(), Is.Zero);

            button.Press();
            bridge.ClearPresses();
            Assert.That(bridge.ConsumePresses(), Is.Zero);
        }

        [UnityTest]
        public IEnumerator JumpPressesQueueSeparatelyFromHits()
        {
            VolleyballInputBridge bridge = CreateBridge(out _, out ActionButton button, out JumpButton jump);
            yield return null;

            Assert.That(bridge.JumpButton, Is.SameAs(jump));
            jump.Press();
            button.Press();
            Assert.That(bridge.ConsumeJumps(), Is.EqualTo(1));
            Assert.That(bridge.ConsumeJumps(), Is.Zero);
            Assert.That(bridge.ConsumePresses(), Is.EqualTo(1));

            jump.Press();
            bridge.FeedJumpForTest();
            bridge.ClearPresses();
            Assert.That(bridge.ConsumeJumps(), Is.Zero, "pausing drops queued jumps too");
        }

        [UnityTest]
        public IEnumerator JumpCuePulsesTheButtonAndRestsAtScaleOne()
        {
            CreateBridge(out _, out _, out JumpButton jump);
            yield return null;

            jump.SetCue(true);
            Assert.That(jump.Cued, Is.True);
            Assert.That(jump.transform.localScale.x, Is.InRange(1f, 1f + JumpButton.PulseAmount + 1e-4f));
            jump.SetCue(false);
            Assert.That(jump.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [UnityTest]
        public IEnumerator JoystickDrivesMoveAndTestFeedOverridesIt()
        {
            VolleyballInputBridge bridge = CreateBridge(out VirtualJoystick joystick, out _);
            yield return null;

            Assert.That(bridge.Move, Is.EqualTo(Vector2.zero));
            joystick.Press(Vector2.zero);
            joystick.Drag(new Vector2(0f, 50f));
            Assert.That(bridge.Move, Is.EqualTo(new Vector2(0f, .5f)));

            bridge.FeedMoveForTest(new Vector2(-1f, 0f));
            Assert.That(bridge.Move, Is.EqualTo(new Vector2(-1f, 0f)));

            bridge.FeedActionForTest();
            Assert.That(bridge.ConsumePresses(), Is.EqualTo(1));
        }
    }
}
