using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KMA.Gameplay
{
    public sealed class FootballHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        const int NoPointer = int.MinValue;

        int activePointer = NoPointer;
        bool interactable = true;

        public event Action Pressed;
        public event Action Released;
        public event Action Cancelled;

        public bool IsInteractable => interactable;
        public bool IsPressed => activePointer != NoPointer;

        public void SetInteractable(bool value)
        {
            if (!value)
                Cancel();
            interactable = value;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable || activePointer != NoPointer || eventData == null)
                return;

            activePointer = eventData.pointerId;
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null || activePointer != eventData.pointerId)
                return;

            activePointer = NoPointer;
            Released?.Invoke();
        }

        public void Cancel()
        {
            if (activePointer == NoPointer)
                return;

            activePointer = NoPointer;
            Cancelled?.Invoke();
        }
    }
}
