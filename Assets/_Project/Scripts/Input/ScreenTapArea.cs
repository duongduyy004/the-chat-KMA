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

        void OnDisable()
        {
            activePointerIds.Clear();
        }

        public void Configure(GameplayInputRouter inputRouter, RectTransform area)
        {
            router = inputRouter;
            gameplayArea = area;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanOwn(eventData) || !IsInsideGameplayArea(eventData))
                return;

            if (!activePointerIds.Add(eventData.pointerId))
            {
                eventData.Use();
                return;
            }

            eventData.Use();
            router.FeedPointerDown(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null || !activePointerIds.Remove(eventData.pointerId))
                return;

            router.FeedPointerUp(eventData.position);
            if (!eventData.used)
                eventData.Use();
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

            GameObject currentObject = eventData.pointerCurrentRaycast.gameObject;
            return currentObject == null || currentObject == gameObject;
        }

        bool IsInsideGameplayArea(PointerEventData eventData)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                gameplayArea, eventData.position, eventData.pressEventCamera);
        }
    }
}
