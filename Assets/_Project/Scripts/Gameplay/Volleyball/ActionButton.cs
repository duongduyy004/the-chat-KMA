using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KMA.Gameplay.Volleyball
{
    /// The ĐÁNH hit area. Its look and press response come from the kit's KitPressFeedback.
    public sealed class ActionButton : MonoBehaviour, IPointerDownHandler
    {
        public event Action Pressed;

        // Fires on touch-down, not release: timing windows are only a few frames wide.
        public void OnPointerDown(PointerEventData eventData) => Press();

        public void Press() => Pressed?.Invoke();
    }
}
