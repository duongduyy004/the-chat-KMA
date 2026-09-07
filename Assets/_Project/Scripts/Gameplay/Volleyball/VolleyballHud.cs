using TMPro;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class VolleyballHud : MonoBehaviour
    {
        [SerializeField] VolleyballController controller;
        [SerializeField] TMP_Text touchLabel;
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text comboLabel;
        [SerializeField] TMP_Text contextLabel;
        [SerializeField] TMP_Text timingLabel;
        [SerializeField] TMP_Text counterCueLabel;

        public string TouchText { get; private set; } = string.Empty;
        public string ScoreText { get; private set; } = string.Empty;
        public string ComboText { get; private set; } = string.Empty;
        public string ContextText { get; private set; } = string.Empty;
        public string TimingText { get; private set; } = string.Empty;
        public string CounterCueText { get; private set; } = string.Empty;

        public void Bind(VolleyballController value)
        {
            controller = value;
            Refresh();
        }

        void Update() => Refresh();

        public void Refresh()
        {
            if (controller == null)
                return;

            TouchText = $"TOUCH {Mathf.Clamp(controller.TouchCount + 1, 1, 3)}/3";
            ScoreText = $"{controller.PlayerScore} - {controller.OpponentScore}";
            ComboText = $"COMBO {controller.LongestCombo}";
            ContextText = controller.CurrentContext.ToString().ToUpperInvariant();
            TimingText = controller.InReachZone ? controller.ExpectedAction.ToString().ToUpperInvariant() : "MOVE INTO REACH";
            CounterCueText = controller.OpponentCounterCueVisible ? "COUNTER THE FAKE" : string.Empty;

            SetText(touchLabel, TouchText);
            SetText(scoreLabel, ScoreText);
            SetText(comboLabel, ComboText);
            SetText(contextLabel, ContextText);
            SetText(timingLabel, TimingText);
            SetText(counterCueLabel, CounterCueText);
        }

        static void SetText(TMP_Text label, string value)
        {
            if (label != null)
                label.text = value;
        }
    }
}
