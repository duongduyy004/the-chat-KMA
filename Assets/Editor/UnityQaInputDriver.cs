using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KMA.EditorTools
{
    public sealed class UnityQaInputDriver
    {
        private const int PointerId = -31415;

        private readonly EventSystem eventSystem;
        private readonly PointerEventData eventData;
        private readonly List<RaycastResult> raycasts = new List<RaycastResult>();
        private GameObject pressHandler;
        private GameObject clickHandler;
        private GameObject dragHandler;
        private Vector2 pressPosition;
        private Vector2 currentPosition;
        private bool pointerIsDown;
        private bool dragging;

        public UnityQaInputDriver(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            eventData = new PointerEventData(eventSystem) { pointerId = PointerId };
        }

        public void PointerDown(Vector2 position)
        {
            if (pointerIsDown)
                ReleaseAll();

            pointerIsDown = true;
            dragging = false;
            pressPosition = position;
            currentPosition = position;
            PrepareEventData(position, Vector2.zero);

            pressHandler = eventData.pointerCurrentRaycast.gameObject == null
                ? null
                : ExecuteEvents.ExecuteHierarchy(
                    eventData.pointerCurrentRaycast.gameObject, eventData, ExecuteEvents.pointerDownHandler);
            clickHandler = eventData.pointerCurrentRaycast.gameObject == null
                ? null
                : ExecuteEvents.GetEventHandler<IPointerClickHandler>(eventData.pointerCurrentRaycast.gameObject);
            dragHandler = eventData.pointerCurrentRaycast.gameObject == null
                ? null
                : ExecuteEvents.GetEventHandler<IDragHandler>(eventData.pointerCurrentRaycast.gameObject);
            RestoreHandlerState();
        }

        public void PointerMove(Vector2 position)
        {
            if (!pointerIsDown)
                return;

            var delta = position - currentPosition;
            currentPosition = position;
            PrepareEventData(position, delta);
            RestoreHandlerState();

            if (dragHandler == null)
                return;

            if (!dragging)
            {
                ExecuteEvents.Execute(dragHandler, eventData, ExecuteEvents.beginDragHandler);
                dragging = true;
                eventData.dragging = true;
            }

            ExecuteEvents.Execute(dragHandler, eventData, ExecuteEvents.dragHandler);
        }

        public void PointerUp(Vector2 position)
        {
            if (!pointerIsDown)
                return;

            var delta = position - currentPosition;
            currentPosition = position;
            PrepareEventData(position, delta);
            RestoreHandlerState();

            if (pressHandler != null)
                ExecuteEvents.Execute(pressHandler, eventData, ExecuteEvents.pointerUpHandler);

            var releaseClickHandler = eventData.pointerCurrentRaycast.gameObject == null
                ? null
                : ExecuteEvents.GetEventHandler<IPointerClickHandler>(eventData.pointerCurrentRaycast.gameObject);
            if (clickHandler != null && clickHandler == releaseClickHandler)
                ExecuteEvents.Execute(clickHandler, eventData, ExecuteEvents.pointerClickHandler);

            EndDrag();
            ClearState();
        }

        public void ReleaseAll()
        {
            if (!pointerIsDown)
                return;

            PrepareEventData(currentPosition, Vector2.zero);
            RestoreHandlerState();
            if (pressHandler != null)
            {
                ExecuteEvents.Execute(pressHandler, eventData, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(pressHandler, eventData, ExecuteEvents.cancelHandler);
            }

            EndDrag();
            ClearState();
        }

        private void PrepareEventData(Vector2 position, Vector2 delta)
        {
            eventData.Reset();
            eventData.pointerId = PointerId;
            eventData.button = PointerEventData.InputButton.Left;
            eventData.position = position;
            eventData.delta = delta;
            eventData.pressPosition = pressPosition;
            raycasts.Clear();
            eventSystem.RaycastAll(eventData, raycasts);
            eventData.pointerCurrentRaycast = FindFirstRaycast(raycasts);
        }

        private void RestoreHandlerState()
        {
            eventData.pointerPress = pressHandler;
            eventData.rawPointerPress = pressHandler;
            eventData.pointerDrag = dragHandler;
            eventData.dragging = dragging;
            eventData.eligibleForClick = clickHandler != null;
        }

        private void EndDrag()
        {
            if (dragging && dragHandler != null)
                ExecuteEvents.Execute(dragHandler, eventData, ExecuteEvents.endDragHandler);
        }

        private void ClearState()
        {
            pointerIsDown = false;
            dragging = false;
            pressHandler = null;
            clickHandler = null;
            dragHandler = null;
            eventData.Reset();
            eventData.pointerId = PointerId;
        }

        private static RaycastResult FindFirstRaycast(List<RaycastResult> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (candidate.gameObject != null)
                    return candidate;
            }

            return default;
        }
    }
}
