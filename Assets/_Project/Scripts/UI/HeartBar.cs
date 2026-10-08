using KMA.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class HeartBar : MonoBehaviour
    {
        const int SlotCount = 5;

        [SerializeField] Image[] slots = new Image[SlotCount];

        [SerializeField] TMPro.TMP_Text countdownLabel;
        // Set when the bar sits on a light surface: empty hearts and the countdown use this ink
        // instead of the default cream, which would vanish there.
        [SerializeField] bool hasInk;
        [SerializeField] Color ink;

        int shownSeconds = -1;
        bool shownWarning;

        public int CurrentHearts { get; private set; }
        public Color FilledColor => MinigameUiTheme.Energy;
        public Color EmptyColor => hasInk ? MinigameUiTheme.WithAlpha(ink, .3f)
            : MinigameUiTheme.WithAlpha(MinigameUiTheme.TextPrimary, .25f);
        Color CountdownColor => hasInk ? ink : MinigameUiTheme.TextPrimary;

        public void SetSlots(Image[] value) => slots = value ?? new Image[SlotCount];

        /// Places the life countdown in an authored label and draws on a light surface with `inkColor`.
        public void UseLightSurface(TMPro.TMP_Text countdown, Color inkColor)
        {
            countdownLabel = countdown;
            hasInk = true;
            ink = inkColor;
            if (countdownLabel != null) countdownLabel.gameObject.SetActive(false);
            SetHearts(CurrentHearts);
        }

        public void SetHearts(int hearts)
        {
            CurrentHearts = Mathf.Clamp(hearts, 0, SlotCount);
            for (var index = 0; index < slots.Length && index < SlotCount; index++)
            {
                if (slots[index] != null)
                    slots[index].color = index < CurrentHearts ? FilledColor : EmptyColor;
            }
        }

        public bool CountdownVisible => countdownLabel != null && countdownLabel.gameObject.activeSelf;
        public string CountdownText => CountdownVisible ? countdownLabel.text : string.Empty;

        /// Number of times the countdown text has been rewritten (it changes once per second).
        public int CountdownWrites { get; private set; }

        static int WholeSeconds(System.TimeSpan remaining) =>
            Mathf.Max(0, Mathf.CeilToInt((float)remaining.TotalSeconds));

        public static string FormatCountdown(System.TimeSpan remaining)
        {
            int seconds = WholeSeconds(remaining);
            return $"{seconds / 60}:{seconds % 60:00}";
        }

        /// Called every frame by the map: only rewrites the label when the shown second or the
        /// warning colour changes, so the per-frame call allocates nothing.
        public void SetCountdown(System.TimeSpan? remaining, bool warning)
        {
            EnsureCountdownLabel();
            if (countdownLabel.gameObject.activeSelf != remaining.HasValue)
                countdownLabel.gameObject.SetActive(remaining.HasValue);
            if (!remaining.HasValue)
            {
                shownSeconds = -1;
                return;
            }
            int seconds = WholeSeconds(remaining.Value);
            if (seconds == shownSeconds && warning == shownWarning) return;
            shownSeconds = seconds;
            shownWarning = warning;
            countdownLabel.text = FormatCountdown(remaining.Value);
            countdownLabel.color = warning ? MinigameUiTheme.Energy : CountdownColor;
            CountdownWrites++;
        }

        void EnsureCountdownLabel()
        {
            if (countdownLabel != null) return;
            countdownLabel = UiKit.Label(transform, "LifeTimer", string.Empty, 28f, MinigameUiTheme.TextPrimary);
            countdownLabel.fontSize = 28f;
            countdownLabel.alignment = TMPro.TextAlignmentOptions.Center;
            countdownLabel.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            var layout = countdownLabel.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            RectTransform rect = countdownLabel.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 34f);
            countdownLabel.raycastTarget = false;
        }
    }
}
