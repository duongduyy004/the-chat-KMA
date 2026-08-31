using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class SprintHud : MonoBehaviour
    {
        [SerializeField] SprintController controller;
        [SerializeField] TMP_Text timerLabel;
        [SerializeField] TMP_Text staminaLabel;
        [SerializeField] TMP_Text distanceLabel;
        [SerializeField] TMP_Text rankLabel;
        [SerializeField] TMP_Text cadenceLabel;
        [SerializeField] Image staminaFill;
        [SerializeField] Image distanceFill;

        public string TimerText { get; private set; } = string.Empty;
        public string StaminaText { get; private set; } = string.Empty;
        public string DistanceText { get; private set; } = string.Empty;
        public string RankText { get; private set; } = string.Empty;
        public string CadenceText { get; private set; } = string.Empty;

        void Awake()
        {
            if (controller == null)
                controller = Object.FindFirstObjectByType<SprintController>();
            CacheVisuals();
        }

        void OnEnable() => Refresh();
        void Update() => Refresh();

        public void Refresh()
        {
            if (controller == null)
                return;
            var state = controller.ReadHudState();
            var snapshot = controller.Snapshot;
            TimerText = Mathf.CeilToInt(state.timeRemaining).ToString();
            StaminaText = $"STAMINA {Mathf.RoundToInt(snapshot.Stamina)}%";
            DistanceText = $"{Mathf.RoundToInt(snapshot.Distance)} m";
            RankText = $"{controller.RankText}";
            CadenceText = $"COMBO x{controller.CadenceCombo}";
            if (timerLabel != null) timerLabel.text = TimerText;
            if (staminaLabel != null) staminaLabel.text = StaminaText;
            if (distanceLabel != null) distanceLabel.text = DistanceText;
            if (rankLabel != null) rankLabel.text = RankText;
            if (cadenceLabel != null) cadenceLabel.text = CadenceText;
            if (staminaFill != null) staminaFill.fillAmount = Mathf.Clamp01(snapshot.Stamina / 100f);
            if (distanceFill != null) distanceFill.fillAmount = Mathf.Clamp01(snapshot.Distance / 100f);
        }

        public bool HasBoundVisuals => timerLabel != null && staminaLabel != null && distanceLabel != null &&
            rankLabel != null && cadenceLabel != null && staminaFill != null && distanceFill != null;

        void CacheVisuals()
        {
            var hud = GameObject.Find("S2_HUD_Minigame");
            if (hud == null)
                return;

            var root = hud.transform.Find("SafeAreaRoot");
            if (root == null)
                return;

            timerLabel ??= root.Find("Timer")?.GetComponent<TMP_Text>();
            staminaLabel ??= root.Find("Stamina")?.GetComponent<TMP_Text>();
            distanceLabel ??= root.Find("Score")?.GetComponent<TMP_Text>();
            rankLabel ??= root.Find("Phase")?.GetComponent<TMP_Text>();
            cadenceLabel ??= root.Find("Status")?.GetComponent<TMP_Text>();
            staminaFill ??= root.Find("Stamina/Fill")?.GetComponent<Image>();
            distanceFill ??= root.Find("Progress/Fill")?.GetComponent<Image>();
        }
    }
}
