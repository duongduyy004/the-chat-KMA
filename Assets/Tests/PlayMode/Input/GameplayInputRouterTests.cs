using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using InputLayer = KMA.Input;

namespace KMA.Tests.Input
{
    public sealed class GameplayInputRouterTests : InputTestFixture
    {
        GameObject eventSystemObject;
        GameObject routerObject;
        GameObject areaObject;

        public override void Setup()
        {
            base.Setup();
            eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            routerObject = new GameObject("GameplayInputRouter");
            areaObject = new GameObject("GameplayArea", typeof(RectTransform));

            var area = areaObject.AddComponent<InputLayer.ScreenTapArea>();
            ((RectTransform)areaObject.transform).sizeDelta = new Vector2(1000f, 1000f);
            area.Configure(routerObject.AddComponent<InputLayer.GameplayInputRouter>(), (RectTransform)areaObject.transform);
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(areaObject);
            Object.DestroyImmediate(routerObject);
            Object.DestroyImmediate(eventSystemObject);
            base.TearDown();
        }

        [Test]
        public void GameplayTap_IsDeliveredExactlyOnce()
        {
            var detector = new InputLayer.TapMashInputDetector();
            var delivered = 0;
            detector.OnTap += () => delivered++;
            Router.SetDetectors(detector, null, null, null, null);

            Area.OnPointerDown(PointerAt(Vector2.zero, 1));

            Assert.That(delivered, Is.EqualTo(1));
        }

        [Test]
        public void DuplicatePointerDown_IsIdempotent()
        {
            var detector = new InputLayer.TapMashInputDetector();
            var delivered = 0;
            detector.OnTap += () => delivered++;
            Router.SetDetectors(detector, null, null, null, null);

            Area.OnPointerDown(PointerAt(Vector2.zero, 1));
            Area.OnPointerDown(PointerAt(Vector2.zero, 1));

            Assert.That(delivered, Is.EqualTo(1));
        }

        [Test]
        public void UiControlTap_DoesNotReachGameplayDetector()
        {
            var detector = new InputLayer.TapMashInputDetector();
            var delivered = 0;
            detector.OnTap += () => delivered++;
            Router.SetDetectors(detector, null, null, null, null);
            var eventData = PointerAt(Vector2.zero, 1);
            eventData.Use();

            Area.OnPointerDown(eventData);

            Assert.That(delivered, Is.EqualTo(0));
        }

        [Test]
        public void CurrentUiRaycastTap_DoesNotReachGameplayDetector()
        {
            var detector = new InputLayer.TapMashInputDetector();
            var delivered = 0;
            detector.OnTap += () => delivered++;
            Router.SetDetectors(detector, null, null, null, null);
            var uiControl = new GameObject("UiControl");
            var eventData = PointerAt(Vector2.zero, 1);
            eventData.pointerCurrentRaycast = new RaycastResult { gameObject = uiControl };

            Area.OnPointerDown(eventData);

            Assert.That(delivered, Is.EqualTo(0));
            Object.DestroyImmediate(uiControl);
        }

        [Test]
        public void OutsideGameplayArea_DoesNotReachGameplayDetector()
        {
            var detector = new InputLayer.TapMashInputDetector();
            var delivered = 0;
            detector.OnTap += () => delivered++;
            Router.SetDetectors(detector, null, null, null, null);

            Area.OnPointerDown(PointerAt(new Vector2(501f, 0f), 1));

            Assert.That(delivered, Is.EqualTo(0));
        }

        [Test]
        public void DragAndUsedPointerUp_StillCleansUpGameplayGesture()
        {
            var holdDetector = new InputLayer.HoldInputDetector();
            var swipeDetector = new InputLayer.SwipeInputDetector();
            var holdEnds = 0;
            var swipes = 0;
            holdDetector.OnHoldEnd += _ => holdEnds++;
            swipeDetector.OnSwipe += _ => swipes++;
            Router.SetDetectors(null, null, holdDetector, null, swipeDetector);

            Area.OnPointerDown(PointerAt(Vector2.zero, 1));
            Area.OnDrag(PointerAt(new Vector2(100f, 0f), 1));
            var up = PointerAt(new Vector2(200f, 0f), 1);
            up.Use();
            Area.OnPointerUp(up);

            Assert.That(holdEnds, Is.EqualTo(1));
            Assert.That(swipes, Is.EqualTo(1));
        }

        [Test]
        public void RhythmTap_AppliesRhythmOffsetMsBeforeDetectorFeed()
        {
            var detector = new InputLayer.RhythmBeatInputDetector();
            double deltaMs = 0d;
            detector.OnJudge += (_, delta) => deltaMs = delta;
            Router.SetDetectors(null, detector, null, null, null);
            Router.RhythmOffsetMs = 125d;

            Router.FeedRhythmTapForTest(10d, 10d);

            Assert.That(deltaMs, Is.EqualTo(125d).Within(.000001d));
        }

        [Test]
        public void RouterLifecycle_SubscribesKeyboardTapExactlyOnce()
        {
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = actions.AddActionMap("Endurance");
            map.AddAction("Tap", InputActionType.Button, "<Keyboard>/t");
            var detector = new InputLayer.TapMashInputDetector();
            var delivered = 0;
            detector.OnTap += () => delivered++;
            Router.SetDetectors(detector, null, null, null, null);
            Router.ConfigureInputForTest(actions, "Endurance");

            Router.enabled = false;
            Router.enabled = true;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.tKey);

            Assert.That(delivered, Is.EqualTo(1));
        }

        [Test]
        public void KeyboardRhythmAction_UsesConfiguredOffset()
        {
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = actions.AddActionMap("Endurance");
            var rhythmAction = map.AddAction("Rhythm", InputActionType.Button, "<Keyboard>/r");
            var detector = new InputLayer.RhythmBeatInputDetector();
            double deltaMs = 0d;
            detector.OnJudge += (_, delta) => deltaMs = delta;
            Router.SetDetectors(null, detector, null, null, null);
            Router.RhythmOffsetMs = 125d;
            Router.RhythmBeatDsp = AudioSettings.dspTime;
            Router.ConfigureInputForTest(actions, "Endurance", rhythmAction);

            var keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.rKey);

            Assert.That(deltaMs, Is.EqualTo(125d).Within(30d));
        }

        InputLayer.GameplayInputRouter Router => routerObject.GetComponent<InputLayer.GameplayInputRouter>();
        InputLayer.ScreenTapArea Area => areaObject.GetComponent<InputLayer.ScreenTapArea>();

        PointerEventData PointerAt(Vector2 position, int pointerId)
        {
            return new PointerEventData(eventSystemObject.GetComponent<EventSystem>())
            {
                position = position,
                pointerId = pointerId
            };
        }
    }
}
