using System.Collections;
using KMA.EditorTools;
using KMA.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KMA.Tests.EditorTools
{
    public sealed class UnityQaPointerTests : InputTestFixture
    {
        GameObject root;
        EventSystem eventSystem;
        Canvas canvas;

        public override void Setup()
        {
            base.Setup();
            root = new GameObject("TestRoot");
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystemObject.transform.SetParent(root.transform, false);
            eventSystem = eventSystemObject.GetComponent<EventSystem>();

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(root);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator RealButton_PointerDownThenUp_DeliversOneClick()
        {
            var button = CreateGraphic("QAButton", canvas.transform, new Vector2(-150f, 0f), new Vector2(200f, 100f))
                .AddComponent<Button>();
            var clickCount = 0;
            button.onClick.AddListener(() => clickCount++);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(UnityQaTargetResolver.Resolve("QAButton", eventSystem, out var resolved), Is.True);
            var driver = new UnityQaInputDriver(eventSystem);

            driver.PointerDown(resolved.ScreenPosition);
            driver.PointerUp(resolved.ScreenPosition);

            Assert.That(clickCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RealScreenTapArea_DownMoveUp_RoutesEveryPointerPhase()
        {
            var routerObject = new GameObject("GameplayInputRouter", typeof(GameplayInputRouter));
            routerObject.transform.SetParent(root.transform, false);
            var router = routerObject.GetComponent<GameplayInputRouter>();
            var tap = new TapMashInputDetector();
            var swipe = new SwipeInputDetector();
            var hold = new HoldInputDetector();
            var receivedDown = false;
            var receivedMove = false;
            var receivedUp = false;
            tap.OnTap += () => receivedDown = true;
            swipe.OnSwipeProgress += _ => receivedMove = true;
            hold.OnHoldEnd += _ => receivedUp = true;
            router.SetDetectors(tap, null, hold, null, swipe);

            var surface = CreateGraphic("GameplaySurface", canvas.transform, Vector2.zero, Vector2.zero);
            var rect = (RectTransform)surface.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            surface.AddComponent<ScreenTapArea>().Configure(router, rect);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var driver = new UnityQaInputDriver(eventSystem);

            driver.PointerDown(new Vector2(Screen.width * .75f, Screen.height * .2f));
            driver.PointerMove(new Vector2(Screen.width * .75f, Screen.height * .7f));
            driver.PointerUp(new Vector2(Screen.width * .75f, Screen.height * .7f));

            Assert.That(receivedDown, Is.True);
            Assert.That(receivedMove, Is.True);
            Assert.That(receivedUp, Is.True);
        }

        [UnityTest]
        public IEnumerator ReleaseAll_WithPendingScreenTapAreaPress_RoutesOneUpAndClearsState()
        {
            var routerObject = new GameObject("GameplayInputRouter", typeof(GameplayInputRouter));
            routerObject.transform.SetParent(root.transform, false);
            var router = routerObject.GetComponent<GameplayInputRouter>();
            var hold = new HoldInputDetector();
            var upCount = 0;
            hold.OnHoldEnd += _ => upCount++;
            router.SetDetectors(null, null, hold, null, null);

            var surface = CreateGraphic("GameplaySurface", canvas.transform, Vector2.zero, Vector2.zero);
            var rect = (RectTransform)surface.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            surface.AddComponent<ScreenTapArea>().Configure(router, rect);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var driver = new UnityQaInputDriver(eventSystem);

            driver.PointerDown(new Vector2(Screen.width * .5f, Screen.height * .5f));
            driver.ReleaseAll();
            driver.ReleaseAll();

            Assert.That(upCount, Is.EqualTo(1));
        }

        static GameObject CreateGraphic(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var graphic = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            graphic.transform.SetParent(parent, false);
            var rect = (RectTransform)graphic.transform;
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return graphic;
        }
    }
}
