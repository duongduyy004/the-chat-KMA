using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using InputLayer = KMA.Input;

namespace KMA.Tests.Input
{
    public sealed class GameplayInputRouterTests : InputTestFixture
    {
        GameObject eventSystemObject;
        GameObject routerObject;
        GameObject areaObject;
        readonly List<InputActionAsset> temporaryAssets = new List<InputActionAsset>();

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
            foreach (var asset in temporaryAssets)
                Object.DestroyImmediate(asset);
            temporaryAssets.Clear();
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
        public void ChildGameplayRaycast_IsOwnedByParentHandler()
        {
            var detector = new InputLayer.TapMashInputDetector();
            var delivered = 0;
            detector.OnTap += () => delivered++;
            Router.SetDetectors(detector, null, null, null, null);
            var gameplayChild = new GameObject("GameplayChild", typeof(RectTransform));
            gameplayChild.transform.SetParent(areaObject.transform, false);
            var gameplayRect = (RectTransform)gameplayChild.transform;
            gameplayRect.sizeDelta = new Vector2(1000f, 1000f);
            Area.Configure(Router, gameplayRect);
            var eventData = PointerAt(Vector2.zero, 1);
            eventData.pointerCurrentRaycast = new RaycastResult { gameObject = gameplayChild };

            Area.OnPointerDown(eventData);

            Assert.That(delivered, Is.EqualTo(1));
        }

        [Test]
        public void InteractableUiChild_IsNotOwnedByGameplayHierarchy()
        {
            var detector = new InputLayer.TapMashInputDetector();
            var delivered = 0;
            detector.OnTap += () => delivered++;
            Router.SetDetectors(detector, null, null, null, null);
            var gameplayChild = new GameObject("GameplayChild", typeof(RectTransform));
            gameplayChild.transform.SetParent(areaObject.transform, false);
            var gameplayRect = (RectTransform)gameplayChild.transform;
            gameplayRect.sizeDelta = new Vector2(1000f, 1000f);
            Area.Configure(Router, gameplayRect);
            var buttonObject = new GameObject("GameplayButton", typeof(RectTransform), typeof(Button));
            buttonObject.transform.SetParent(gameplayChild.transform, false);
            var eventData = PointerAt(Vector2.zero, 1);
            eventData.pointerCurrentRaycast = new RaycastResult { gameObject = buttonObject };

            Area.OnPointerDown(eventData);

            Assert.That(delivered, Is.EqualTo(0));
        }

        [Test]
        public void DisabledUiChild_IsNotOwnedByGameplayHierarchy()
        {
            var detector = new InputLayer.TapMashInputDetector();
            var delivered = 0;
            detector.OnTap += () => delivered++;
            Router.SetDetectors(detector, null, null, null, null);
            var gameplayChild = new GameObject("GameplayChild", typeof(RectTransform));
            gameplayChild.transform.SetParent(areaObject.transform, false);
            var gameplayRect = (RectTransform)gameplayChild.transform;
            gameplayRect.sizeDelta = new Vector2(1000f, 1000f);
            Area.Configure(Router, gameplayRect);
            var buttonObject = new GameObject("DisabledGameplayButton", typeof(RectTransform), typeof(Button));
            buttonObject.transform.SetParent(gameplayChild.transform, false);
            buttonObject.GetComponent<Button>().interactable = false;
            var eventData = PointerAt(Vector2.zero, 1);
            eventData.pointerCurrentRaycast = new RaycastResult { gameObject = buttonObject };

            Area.OnPointerDown(eventData);

            Assert.That(delivered, Is.EqualTo(0));
            Object.DestroyImmediate(buttonObject);
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

        [Test]
        public void KeyboardHold_IsCanceledBeforeDisableAndReconfigure()
        {
            var hold = new InputLayer.HoldInputDetector();
            var holdEnds = 0;
            hold.OnHoldEnd += _ => holdEnds++;
            Router.SetDetectors(null, null, hold, null, null);
            Router.ConfigureInputForTest(SharedActions(), "Endurance");
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.hKey);
            Router.enabled = false;
            Assert.That(holdEnds, Is.EqualTo(1));
            Release(keyboard.hKey);

            Router.enabled = true;
            Press(keyboard.hKey);
            Router.ConfigureInputForTest(SharedActions(), "Endurance");

            Assert.That(holdEnds, Is.EqualTo(2));
        }

        [Test]
        public void SharedSprintActions_RouteBothAlternateSides()
        {
            var detector = new InputLayer.AlternateTapInputDetector();
            var validTaps = 0;
            detector.OnValidTap += _ => validTaps++;
            Router.SetDetectors(null, null, null, detector, null);
            Router.ConfigureInputForTest(SharedActions(), "Sprint");
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.leftArrowKey);
            Release(keyboard.leftArrowKey);
            Press(keyboard.rightArrowKey);

            Assert.That(validTaps, Is.EqualTo(2));
        }

        [Test]
        public void SharedEnduranceActions_RouteTapHoldAndBothSwipes()
        {
            var taps = new InputLayer.TapMashInputDetector();
            var hold = new InputLayer.HoldInputDetector();
            var swipe = new InputLayer.SwipeInputDetector();
            var tapCount = 0;
            var holdStarts = 0;
            var holdEnds = 0;
            var swipeCount = 0;
            taps.OnTap += () => tapCount++;
            hold.OnHoldStart += () => holdStarts++;
            hold.OnHoldEnd += _ => holdEnds++;
            swipe.OnSwipe += _ => swipeCount++;
            Router.SetDetectors(taps, null, hold, null, swipe);
            Router.ConfigureInputForTest(SharedActions(), "Endurance");
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.tKey);
            Release(keyboard.tKey);
            Press(keyboard.hKey);
            Release(keyboard.hKey);
            Press(keyboard.upArrowKey);
            Release(keyboard.upArrowKey);
            Press(keyboard.downArrowKey);
            Release(keyboard.downArrowKey);

            Assert.That(tapCount, Is.EqualTo(1));
            Assert.That(holdStarts, Is.EqualTo(1));
            Assert.That(holdEnds, Is.EqualTo(1));
            Assert.That(swipeCount, Is.EqualTo(2));
        }

        [TestCase("Boss")]
        [TestCase("Punishment")]
        public void SharedSideActionMaps_RouteTapHoldSwipeAndAlternate(string mapName)
        {
            var taps = new InputLayer.TapMashInputDetector();
            var hold = new InputLayer.HoldInputDetector();
            var alternate = new InputLayer.AlternateTapInputDetector();
            var swipe = new InputLayer.SwipeInputDetector();
            var tapCount = 0;
            var holdStarts = 0;
            var holdEnds = 0;
            var alternateCount = 0;
            var swipeCount = 0;
            taps.OnTap += () => tapCount++;
            hold.OnHoldStart += () => holdStarts++;
            hold.OnHoldEnd += _ => holdEnds++;
            alternate.OnValidTap += _ => alternateCount++;
            swipe.OnSwipe += _ => swipeCount++;
            Router.SetDetectors(taps, null, hold, alternate, swipe);
            Router.ConfigureInputForTest(SharedActions(), mapName);
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.spaceKey);
            Release(keyboard.spaceKey);
            Press(keyboard.hKey);
            Release(keyboard.hKey);
            Press(keyboard.leftArrowKey);
            Release(keyboard.leftArrowKey);
            Press(keyboard.rightArrowKey);
            Release(keyboard.rightArrowKey);

            Assert.That(tapCount, Is.EqualTo(1));
            Assert.That(holdStarts, Is.EqualTo(1));
            Assert.That(holdEnds, Is.EqualTo(1));
            Assert.That(alternateCount, Is.EqualTo(2));
            Assert.That(swipeCount, Is.EqualTo(2));
        }

        [Test]
        public void EnhancedTouchScreenTapArea_RoutesTouchRhythmAndEnforcesOwnership()
        {
            var detector = new InputLayer.RhythmBeatInputDetector();
            var judges = 0;
            detector.OnJudge += (_, _) => judges++;
            Router.SetDetectors(null, detector, null, null, null);
            Router.RhythmOffsetMs = 125d;
            Router.RhythmBeatDsp = AudioSettings.dspTime - .125d;
            InputSystem.AddDevice<Touchscreen>();

            Assert.That(EnhancedTouchSupport.enabled, Is.True);
            Area.OnPointerDown(PointerAt(Vector2.zero, 7));

            Assert.That(judges, Is.EqualTo(1));
            Router.enabled = false;
            Assert.That(EnhancedTouchSupport.enabled, Is.False);
        }

        [Test]
        public void DisableFlushesActiveGestureAndRejectsFurtherPointers()
        {
            var hold = new InputLayer.HoldInputDetector();
            var swipe = new InputLayer.SwipeInputDetector();
            var holdEnds = 0;
            var swipes = 0;
            hold.OnHoldEnd += _ => holdEnds++;
            swipe.OnSwipe += _ => swipes++;
            Router.SetDetectors(null, null, hold, null, swipe);

            Area.OnPointerDown(PointerAt(Vector2.zero, 1));
            Area.OnDrag(PointerAt(new Vector2(100f, 0f), 1));
            Router.enabled = false;
            Area.OnPointerDown(PointerAt(Vector2.zero, 2));
            Area.OnPointerUp(PointerAt(Vector2.zero, 1));

            Assert.That(holdEnds, Is.EqualTo(1));
            Assert.That(swipes, Is.EqualTo(1));
        }

        [Test]
        public void MultiPointerGestures_DoNotEndAnotherPointer()
        {
            var hold = new InputLayer.HoldInputDetector();
            var swipe = new InputLayer.SwipeInputDetector();
            var holdEnds = 0;
            var swipes = 0;
            hold.OnHoldEnd += _ => holdEnds++;
            swipe.OnSwipe += _ => swipes++;
            Router.SetDetectors(null, null, hold, null, swipe);

            Area.OnPointerDown(PointerAt(Vector2.zero, 1));
            Area.OnPointerDown(PointerAt(new Vector2(10f, 0f), 2));
            Area.OnDrag(PointerAt(new Vector2(100f, 0f), 1));
            Area.OnPointerUp(PointerAt(new Vector2(10f, 0f), 2));

            Assert.That(holdEnds, Is.EqualTo(0));
            Assert.That(swipes, Is.EqualTo(0));

            Area.OnPointerUp(PointerAt(new Vector2(200f, 0f), 1));

            Assert.That(holdEnds, Is.EqualTo(1));
            Assert.That(swipes, Is.EqualTo(1));
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

        InputActionAsset SharedActions()
        {
            var path = Path.Combine(Application.dataPath, "_Project", "Settings", "Input", "KMA.inputactions");
            var asset = InputActionAsset.FromJson(File.ReadAllText(path));
            temporaryAssets.Add(asset);
            return asset;
        }
    }
}
