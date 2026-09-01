using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class EnduranceHud : MonoBehaviour
    {
        [SerializeField] EnduranceController controller;
        [SerializeField] TMP_Text timerLabel;
        [SerializeField] TMP_Text lapLabel;
        [SerializeField] TMP_Text staminaLabel;
        [SerializeField] TMP_Text comboLabel;
        [SerializeField] TMP_Text modeLabel;
        [SerializeField] Image staminaFill;
        [SerializeField] RectTransform miniMapMarker;
        [SerializeField] Vector2 miniMapStart = new(-250f, 0f);
        [SerializeField] Vector2 miniMapEnd = new(250f, 0f);
        [SerializeField] EnduranceBeatRing beatRing;
        [SerializeField] EnduranceObstacleCue obstacleCue;

        public bool HasBoundVisuals => controller != null && timerLabel != null && lapLabel != null &&
            staminaLabel != null && comboLabel != null && modeLabel != null && staminaFill != null &&
            miniMapMarker != null && beatRing != null && obstacleCue != null;

        void Awake() => CacheReferences();

        void OnEnable()
        {
            CacheReferences();
            Refresh();
        }

        void Update() => Refresh();

        public void Refresh()
        {
            if (controller == null || controller.Rules == null)
                return;

            var state = controller.ReadHudState();
            var rules = controller.Rules;
            var prompt = PromptFor(rules.Mode);
            if (timerLabel != null) timerLabel.text = Mathf.CeilToInt(state.timeRemaining).ToString();
            if (lapLabel != null) lapLabel.text = $"LAP {rules.Laps}/{rules.RequiredLaps}";
            if (staminaLabel != null) staminaLabel.text = $"STAMINA {Mathf.RoundToInt(rules.Stamina)}%";
            if (comboLabel != null) comboLabel.text = $"COMBO x{rules.Combo}";
            if (modeLabel != null) modeLabel.text = prompt;
            if (staminaFill != null) staminaFill.fillAmount = Mathf.Clamp01(rules.Stamina / 100f);
            if (miniMapMarker != null)
                miniMapMarker.anchoredPosition = Vector2.Lerp(miniMapStart, miniMapEnd, rules.LapProgress);

            if (beatRing != null) beatRing.Refresh();
            if (obstacleCue != null) obstacleCue.Refresh();
        }

        static string PromptFor(EnduranceInputMode mode) => mode == EnduranceInputMode.BreathHold
            ? "HOLD TO BREATHE"
            : mode == EnduranceInputMode.ObstacleSwipe ? "SWIPE TO CLEAR" : "TAP TO THE BEAT";

        void CacheReferences()
        {
            controller ??= Object.FindFirstObjectByType<EnduranceController>();
            beatRing ??= Object.FindFirstObjectByType<EnduranceBeatRing>();
            obstacleCue ??= Object.FindFirstObjectByType<EnduranceObstacleCue>();
        }
    }
}
