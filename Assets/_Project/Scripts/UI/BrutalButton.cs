using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace KMA.Gameplay.UI
{
    public sealed class BrutalButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        static readonly Vector2 PressedOffset = new Vector2(4f, -4f);

        [SerializeField] RectTransform visual;
        [SerializeField] RectTransform shadow;
        [SerializeField] UnityEvent onPressed;

        Vector2 visualRestPosition;
        Vector2 shadowRestPosition;
        Coroutine restoreRoutine;

        public Vector2 CurrentVisualOffset { get; private set; }

        void Awake()
        {
            if (visual == null)
                visual = transform as RectTransform;
            CacheRestPositions();
        }

        void OnDisable()
        {
            if (restoreRoutine != null)
                StopCoroutine(restoreRoutine);
            restoreRoutine = null;
            RestoreImmediately();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (restoreRoutine != null)
                StopCoroutine(restoreRoutine);
            restoreRoutine = null;
            ApplyPressed();
            onPressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData) => RestoreOverTime();

        public void OnPointerExit(PointerEventData eventData) => RestoreOverTime();

        public void SetPressedForTest(bool pressed)
        {
            if (pressed)
                ApplyPressed();
            else
                RestoreImmediately();
        }

        void CacheRestPositions()
        {
            if (visual != null)
                visualRestPosition = visual.anchoredPosition;
            if (shadow != null)
                shadowRestPosition = shadow.anchoredPosition;
        }

        void ApplyPressed()
        {
            CacheRestPositions();
            CurrentVisualOffset = PressedOffset;
            if (visual != null)
                visual.anchoredPosition = visualRestPosition + PressedOffset;
            if (shadow != null)
                shadow.anchoredPosition = shadowRestPosition;
        }

        void RestoreOverTime()
        {
            if (!isActiveAndEnabled)
            {
                RestoreImmediately();
                return;
            }
            if (restoreRoutine != null)
                StopCoroutine(restoreRoutine);
            restoreRoutine = StartCoroutine(RestoreAfterDelay());
        }

        IEnumerator RestoreAfterDelay()
        {
            var startingOffset = CurrentVisualOffset;
            var elapsed = 0f;
            while (elapsed < .1f)
            {
                elapsed += Time.unscaledDeltaTime;
                SetVisualOffset(Vector2.Lerp(startingOffset, Vector2.zero, Mathf.Clamp01(elapsed / .1f)));
                yield return null;
            }
            RestoreImmediately();
            restoreRoutine = null;
        }

        void RestoreImmediately() => SetVisualOffset(Vector2.zero);

        void SetVisualOffset(Vector2 offset)
        {
            CurrentVisualOffset = offset;
            if (visual != null)
                visual.anchoredPosition = visualRestPosition + offset;
            if (shadow != null)
                shadow.anchoredPosition = shadowRestPosition;
        }
    }
}
