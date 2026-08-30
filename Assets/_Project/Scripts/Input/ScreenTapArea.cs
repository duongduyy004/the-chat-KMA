using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;

namespace KMA.Input
{
    public sealed class ScreenTapArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] GameplayInputRouter router;
        [SerializeField] RectTransform gameplayArea;

        readonly HashSet<int> activePointerIds = new HashSet<int>();
        static int enhancedTouchAreaCount;
        bool enhancedTouchEnabled;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            enhancedTouchEnabled = true;
            enhancedTouchAreaCount++;
        }

        void OnDisable()
        {
            router?.FlushPointerState();
            activePointerIds.Clear();
            if (!enhancedTouchEnabled)
                return;

            enhancedTouchEnabled = false;
            enhancedTouchAreaCount--;
            EnhancedTouchSupport.Disable();
        }

        public void Configure(GameplayInputRouter inputRouter, RectTransform area)
        {
            router = inputRouter;
            gameplayArea = area;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || !CanOwn(eventData) || !IsInsideGameplayArea(eventData))
                return;

            if (!activePointerIds.Add(eventData.pointerId))
            {
                eventData.Use();
                return;
            }

            eventData.Use();
            router.FeedPointerDown(eventData.pointerId, eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || eventData == null || !activePointerIds.Remove(eventData.pointerId))
                return;

            router.FeedPointerUp(eventData.pointerId, eventData.position);
            if (!eventData.used)
                eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || eventData == null || !activePointerIds.Contains(eventData.pointerId) || eventData.used)
                return;

            eventData.Use();
            router.FeedPointerMove(eventData.pointerId, eventData.position);
        }

        bool CanOwn(PointerEventData eventData)
        {
            if (eventData == null || eventData.used || router == null || !router.AcceptsPointerEvents || gameplayArea == null)
                return false;

            GameObject currentObject = eventData.pointerCurrentRaycast.gameObject;
            if (currentObject == null || currentObject == gameObject)
                return true;

            if (currentObject != gameplayArea.gameObject && !currentObject.transform.IsChildOf(gameplayArea))
                return false;

            Selectable selectable = currentObject.GetComponentInParent<Selectable>();
            return selectable == null || !selectable.IsInteractable();
        }

        bool IsInsideGameplayArea(PointerEventData eventData)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                gameplayArea, eventData.position, eventData.pressEventCamera);
        }
    }
}
