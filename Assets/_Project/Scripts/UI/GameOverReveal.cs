using UnityEngine;

namespace KMA.Gameplay.UI
{
    // Fades and slides the Game Over layout in; unscaled so it plays while paused.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class GameOverReveal : MonoBehaviour
    {
        const float Duration = .45f;
        const float Slide = 28f;

        CanvasGroup group;
        RectTransform rect;
        Vector2 resting;
        float elapsed;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            rect = (RectTransform)transform;
            resting = rect.anchoredPosition;
            Apply(0f);
        }

        void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Duration);
            Apply(Mathf.SmoothStep(0f, 1f, t));
            if (t >= 1f) enabled = false;
        }

        void Apply(float t)
        {
            group.alpha = t;
            rect.anchoredPosition = resting + new Vector2(0f, -Slide * (1f - t));
        }
    }
}
