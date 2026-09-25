using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class TouchControlTests
    {
        GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("TouchControlTests", typeof(RectTransform));

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        RectTransform Child(string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            return (RectTransform)child.transform;
        }

        [Test]
        public void JoystickReportsTheNormalizedDragFromWhereTheThumbLanded()
        {
            var joystick = root.AddComponent<VirtualJoystick>();
            RectTransform stickBase = Child("Base"), knob = Child("Knob");
            joystick.Configure((RectTransform)root.transform, stickBase, knob, 100f, new Vector2(0f, -200f));

            Assert.That(stickBase.anchoredPosition, Is.EqualTo(new Vector2(0f, -200f)));
            joystick.Press(new Vector2(40f, 40f));
            Assert.That(joystick.IsHeld, Is.True);
            Assert.That(stickBase.anchoredPosition, Is.EqualTo(new Vector2(40f, 40f)));

            joystick.Drag(new Vector2(90f, 40f));
            Assert.That(joystick.Value, Is.EqualTo(new Vector2(.5f, 0f)));
            Assert.That(knob.anchoredPosition, Is.EqualTo(new Vector2(90f, 40f)));

            joystick.Drag(new Vector2(40f, 440f));
            Assert.That(joystick.Value.y, Is.EqualTo(1f).Within(1e-4f));

            joystick.Release();
            Assert.That(joystick.IsHeld, Is.False);
            Assert.That(joystick.Value, Is.EqualTo(Vector2.zero));
            Assert.That(knob.anchoredPosition, Is.EqualTo(new Vector2(0f, -200f)));
        }

        [Test]
        public void ActionButtonRaisesPressed()
        {
            var button = root.AddComponent<ActionButton>();
            int pressed = 0;
            button.Pressed += () => pressed++;
            button.Press();
            Assert.That(pressed, Is.EqualTo(1));
        }
    }
}
