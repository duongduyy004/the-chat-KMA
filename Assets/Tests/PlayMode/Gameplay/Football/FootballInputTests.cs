using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KMA.Tests.Gameplay.Football
{
    public sealed class FootballInputTests
    {
        GameObject root;
        EventSystem eventSystem;
        Slider aim;
        FootballHoldButton shoot;
        FootballInputBridge bridge;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("FootballInputTests");
            eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            var aimObject = new GameObject("AIM", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
            aimObject.transform.SetParent(root.transform);
            aim = aimObject.GetComponent<Slider>();
            var shootObject = new GameObject("SHOOT", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(FootballHoldButton));
            shootObject.transform.SetParent(root.transform);
            shoot = shootObject.GetComponent<FootballHoldButton>();
            bridge = root.AddComponent<FootballInputBridge>();
            bridge.Configure(aim, shoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (eventSystem != null) Object.DestroyImmediate(eventSystem.gameObject);
        }

        [Test]
        public void OnlyThePointerThatPressedShootCanReleaseIt()
        {
            bridge.SetEnabled(false, true);
            int releases = 0;
            bridge.ShootReleased += () => releases++;

            PointerDown(shoot.gameObject, 10);
            PointerUp(shoot.gameObject, 11, new Vector2(-100f, -100f));
            Assert.That(releases, Is.Zero);
            PointerUp(shoot.gameObject, 10, new Vector2(-100f, -100f));
            PointerUp(shoot.gameObject, 10, new Vector2(-100f, -100f));

            Assert.That(releases, Is.EqualTo(1));
        }

        [Test]
        public void DisablingShootCancelsRatherThanReleasesAndAllowsANewPointerLater()
        {
            bridge.SetEnabled(false, true);
            int releases = 0, cancellations = 0, presses = 0;
            bridge.ShootReleased += () => releases++;
            bridge.ShootCancelled += () => cancellations++;
            bridge.ShootPressed += () => presses++;

            PointerDown(shoot.gameObject, 10);
            bridge.SetEnabled(false, false);
            bridge.SetEnabled(false, true);
            PointerUp(shoot.gameObject, 10);
            PointerDown(shoot.gameObject, 12);

            Assert.That(releases, Is.Zero);
            Assert.That(cancellations, Is.EqualTo(1));
            Assert.That(presses, Is.EqualTo(2));
        }

        [Test]
        public void DisabledSliderAndRepeatedBridgeEnableDoNotRaiseExtraEvents()
        {
            int aims = 0;
            bridge.AimChanged += _ => aims++;
            bridge.SetEnabled(false, false);
            Assert.That(aim.interactable, Is.False);
            aim.onValueChanged.Invoke(.5f);
            Assert.That(aims, Is.Zero);

            bridge.SetEnabled(true, false);
            bridge.enabled = false;
            bridge.enabled = true;
            aim.onValueChanged.Invoke(.5f);

            Assert.That(aims, Is.EqualTo(1));
        }

        [Test]
        public void PauseCancellationReturnsChargingRulesToAimingWithoutAShot()
        {
            var rules = new FootballRules(FootballTuning.For(FootballDifficulty.Normal));
            rules.Start();
            rules.SetAim(0f);
            bridge.Configure(aim, shoot);
            bridge.AimChanged += _ => rules.SetAim(0f);
            bridge.ShootPressed += () => rules.BeginCharge();
            bridge.ShootReleased += () => rules.ReleaseShot();
            bridge.ShootCancelled += rules.CancelCharge;
            bridge.SetEnabled(false, true);

            PointerDown(shoot.gameObject, 10);
            rules.Tick(.5f);
            bridge.CancelActivePointer();

            Assert.That(rules.State, Is.EqualTo(FootballState.Aiming));
            Assert.That(rules.Power, Is.Zero);
            Assert.That(rules.Kicks, Is.Zero);
        }

        static void PointerDown(GameObject target, int pointerId)
        {
            var data = new PointerEventData(EventSystem.current) { pointerId = pointerId, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
        }

        static void PointerUp(GameObject target, int pointerId, Vector2? position = null)
        {
            var data = new PointerEventData(EventSystem.current)
            {
                pointerId = pointerId,
                button = PointerEventData.InputButton.Left,
                position = position ?? Vector2.zero
            };
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
        }
    }
}
