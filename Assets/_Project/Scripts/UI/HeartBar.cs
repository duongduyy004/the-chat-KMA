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

        public int CurrentHearts { get; private set; }
        public Color FilledColor => MinigameUiTheme.Energy;
        public Color EmptyColor => MinigameUiTheme.WithAlpha(MinigameUiTheme.TextPrimary, .25f);

        public void SetSlots(Image[] value) => slots = value ?? new Image[SlotCount];

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

        public static string FormatCountdown(System.TimeSpan remaining)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt((float)remaining.TotalSeconds));
            return $"{seconds / 60}:{seconds % 60:00}";
        }

        public void SetCountdown(System.TimeSpan? remaining, bool warning)
        {
            EnsureCountdownLabel();
            countdownLabel.gameObject.SetActive(remaining.HasValue);
            if (!remaining.HasValue) return;
            countdownLabel.text = FormatCountdown(remaining.Value);
            countdownLabel.color = warning ? MinigameUiTheme.Energy : MinigameUiTheme.TextPrimary;
        }

        void EnsureCountdownLabel()
        {
            if (countdownLabel != null) return;
            countdownLabel = UiKit.Label(transform, "LifeTimer", string.Empty, 22f, MinigameUiTheme.TextPrimary);
            countdownLabel.fontSize = 22f;
            countdownLabel.alignment = TMPro.TextAlignmentOptions.Center;
            countdownLabel.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            var layout = countdownLabel.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            RectTransform rect = countdownLabel.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 24f);
            countdownLabel.raycastTarget = false;
        }
    }
}
