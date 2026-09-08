using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class BasketballHud : MonoBehaviour
    {
        [SerializeField] BasketballController controller;
        [SerializeField] int targetBaskets = 5;
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text attemptsLabel;
        [SerializeField] TMP_Text judgeLabel;
        [SerializeField] TMP_Text comboLabel;
        [SerializeField] TMP_Text chargeLabel;
        [SerializeField] Image apexRingFill;
        [SerializeField] Image apexZoneGlow;
        [SerializeField] Image chargeFill;
        [SerializeField] RectTransform chargeTargetBand;
        [SerializeField] RectTransform chargeTrack;

        public BasketballController Controller => controller;
        public string ScoreText { get; private set; } = string.Empty;
        public string AttemptsText { get; private set; } = string.Empty;
        public string JudgeText { get; private set; } = string.Empty;
        public string ComboText { get; private set; } = string.Empty;
        public string ChargeText { get; private set; } = string.Empty;
        public float ApexRing01 { get; private set; }
        public bool ApexZoneGlowing { get; private set; }

        public void Bind(BasketballController value)
        {
            controller = value;
            Refresh();
        }

        void Update() => Refresh();

        public void Refresh()
        {
            if (controller == null) return;

            ScoreText = $"BASKETS {controller.Baskets}/{targetBaskets}";
            AttemptsText = $"ATTEMPTS {controller.Attempts}";
            ComboText = $"COMBO {controller.BestCombo}";
            JudgeText = controller.LastJudge == FinishJudge.Ignored
                ? string.Empty
                : controller.LastJudge.ToString().ToUpperInvariant();
            ChargeText = controller.IsCharging
                ? $"CHARGE {Mathf.RoundToInt(controller.ChargeRatio * 100f)}%"
                : "AIM";
            ApexRing01 = controller.FlightApexProgress;
            ApexZoneGlowing = controller.FinishCueVisible;

            SetText(scoreLabel, ScoreText);
            SetText(attemptsLabel, AttemptsText);
            SetText(judgeLabel, JudgeText);
            SetText(comboLabel, ComboText);
            SetText(chargeLabel, ChargeText);

            // The ring closes around the ball as the apex approaches; the zone lights up only
            // inside the authored cue lead, so the player is warned before they must act.
            if (apexRingFill != null) apexRingFill.fillAmount = 1f - Mathf.Clamp01(ApexRing01);
            if (apexZoneGlow != null) apexZoneGlow.enabled = ApexZoneGlowing;
            if (chargeFill != null) chargeFill.fillAmount = Mathf.Clamp01(controller.ChargeRatio);
            RefreshTargetBand();
        }

        // The glowing band is the charge range that lands the lob inside the authored apex band,
        // so it moves whenever the difficulty step widens the angle span.
        void RefreshTargetBand()
        {
            if (chargeTargetBand == null || chargeTrack == null) return;
            float min = Mathf.Clamp01(controller.TargetChargeMin);
            float max = Mathf.Clamp01(controller.TargetChargeMax);
            chargeTargetBand.anchorMin = new Vector2(chargeTargetBand.anchorMin.x, min);
            chargeTargetBand.anchorMax = new Vector2(chargeTargetBand.anchorMax.x, max);
            chargeTargetBand.offsetMin = Vector2.zero;
            chargeTargetBand.offsetMax = Vector2.zero;
        }

        static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
