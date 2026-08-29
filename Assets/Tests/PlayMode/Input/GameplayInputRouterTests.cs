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
