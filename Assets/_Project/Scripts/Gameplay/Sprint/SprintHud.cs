using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class SprintHud : MonoBehaviour
    {
        [SerializeField] SprintController controller;
        [SerializeField] Transform metricsRoot;
        [SerializeField] TMP_Text distanceLabel;
        [SerializeField] TMP_Text rankLabel;
        [SerializeField] TMP_Text cadenceLabel;
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
            if (distanceLabel != null) distanceLabel.text = DistanceText;
            if (rankLabel != null) rankLabel.text = RankText;
            if (cadenceLabel != null) cadenceLabel.text = CadenceText;
            if (distanceFill != null) distanceFill.fillAmount = Mathf.Clamp01(snapshot.Distance / 100f);
        }

        public bool HasBoundVisuals => metricsRoot != null && distanceLabel != null && rankLabel != null &&
            cadenceLabel != null && distanceFill != null;

        void CacheVisuals()
        {
            var hud = GameObject.Find("S2_HUD_Minigame");
            if (hud == null)
                return;

            var root = hud.transform.Find("SafeAreaRoot");
            if (root == null)
                return;

            metricsRoot ??= root.Find("SprintMetrics");
            if (metricsRoot == null)
                return;

            distanceLabel ??= metricsRoot.Find("SprintDistance")?.GetComponent<TMP_Text>();
            rankLabel ??= metricsRoot.Find("SprintRank")?.GetComponent<TMP_Text>();
            cadenceLabel ??= metricsRoot.Find("SprintCadence")?.GetComponent<TMP_Text>();
            distanceFill ??= metricsRoot.Find("SprintDistanceFill")?.GetComponent<Image>();
        }
    }
}
