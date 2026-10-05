using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace KMA.Gameplay.FrogJump
{
    public sealed class FrogJumpTapArea : MonoBehaviour, IPointerDownHandler
    {
        public event Action Tapped;

        public void OnPointerDown(PointerEventData eventData) => Tapped?.Invoke();

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                Tapped?.Invoke();
        }
    }
}
