using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KMA.Input
{
    public sealed class ScreenTapArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] GameplayInputRouter router;
        [SerializeField] RectTransform gameplayArea;

        readonly HashSet<int> activePointerIds = new HashSet<int>();

        void OnEnable()
        {
            if (router != null)
                router.RegisterScreenTapArea();
        }

        void OnDisable()
        {
            if (router != null)
                router.UnregisterScreenTapArea();
            activePointerIds.Clear();
        }

        public void Configure(GameplayInputRouter inputRouter, RectTransform area)
        {
            if (router != null && isActiveAndEnabled)
                router.UnregisterScreenTapArea();

            router = inputRouter;
            gameplayArea = area;

            if (router != null && isActiveAndEnabled)
                router.RegisterScreenTapArea();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanOwn(eventData) || !IsInsideGameplayArea(eventData))
                return;

            activePointerIds.Add(eventData.pointerId);
            eventData.Use();
            router.FeedPointerDown(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null || !activePointerIds.Remove(eventData.pointerId) || eventData.used)
                return;

            eventData.Use();
            router.FeedPointerUp(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData == null || !activePointerIds.Contains(eventData.pointerId) || eventData.used)
                return;

            eventData.Use();
            router.FeedPointerMove(eventData.position);
        }

        bool CanOwn(PointerEventData eventData)
        {
            if (eventData == null || eventData.used || router == null || gameplayArea == null)
                return false;

            GameObject pressedObject = eventData.pointerPressRaycast.gameObject;
            return pressedObject == null || pressedObject == gameObject || pressedObject.transform.IsChildOf(transform);
        }

        bool IsInsideGameplayArea(PointerEventData eventData)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                gameplayArea, eventData.position, eventData.pressEventCamera);
        }
    }
}
