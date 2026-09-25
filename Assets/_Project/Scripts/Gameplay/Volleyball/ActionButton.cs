using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KMA.Gameplay.Volleyball
{
    public sealed class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public event Action Pressed;

        [SerializeField] Image face;
        [SerializeField] RectTransform faceRect;

        public void Configure(Image buttonFace, RectTransform buttonFaceRect)
        {
            face = buttonFace;
            faceRect = buttonFaceRect;
        }

        // Fires on touch-down, not release: timing windows are only a few frames wide.
        public void OnPointerDown(PointerEventData eventData)
        {
            SetPressed(true);
            Press();
        }

        public void OnPointerUp(PointerEventData eventData) => SetPressed(false);

        void OnDisable() => SetPressed(false);

        void SetPressed(bool pressed)
        {
            if (face)
                face.color = pressed ? new Color32(255, 197, 61, 255) : new Color32(255, 151, 34, 255);
            if (faceRect)
                faceRect.localScale = pressed ? Vector3.one * .93f : Vector3.one;
        }

        public void Press() => Pressed?.Invoke();
    }
}
