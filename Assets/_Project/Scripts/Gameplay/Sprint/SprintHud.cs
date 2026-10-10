using KMA.UI.Kit;
using TMPro;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class SprintHud : MonoBehaviour
    {
        [SerializeField] SprintController controller;
        [SerializeField] Transform metricsRoot;
        [SerializeField] TMP_Text distanceLabel;
        [SerializeField] TMP_Text rankLabel;
        [SerializeField] TMP_Text cadenceLabel;
        [SerializeField] KitBar distanceBar;
        [SerializeField] KitBar staminaBar;

        static readonly Color LowStaminaColor = new Color(.9f, .27f, .22f);

        public string DistanceText { get; private set; } = string.Empty;
        public string RankText { get; private set; } = string.Empty;
        public string CadenceText { get; private set; } = string.Empty;
        public float PipProgress { get; private set; }

        void Awake()
        {
            if (controller == null)
                controller = Object.FindFirstObjectByType<SprintController>();
            SprintFestivalPresentation.Build();
            CacheVisuals();
        }

        void OnEnable() => Refresh();
        void Update() => Refresh();

        public void Refresh()
        {
            if (controller == null)
                return;

            var snapshot = controller.Snapshot;
            float progress = controller.IsLearnChallenge
                ? Mathf.Clamp01((float)controller.CorrectStreak / Mathf.Max(1, controller.TargetCount))
                : Mathf.Clamp01(snapshot.Distance / Mathf.Max(1f, controller.TargetDistance));

            DistanceText = controller.IsLearnChallenge
                ? $"NHỊP {controller.CorrectStreak} / {controller.TargetCount}"
                : $"{Mathf.RoundToInt(snapshot.Distance)} / {controller.TargetDistance:0} m";
            RankText = controller.RankText;
            CadenceText = controller.IsWaitingForRivals ? SprintController.WaitForRivalsText
                : controller.IsComboBoosting
                    ? $"BỨT TỐC ×{controller.CadenceCombo}" : $"CHUỖI ×{controller.CadenceCombo}";
            PipProgress = progress;

            if (distanceLabel != null) distanceLabel.text = VietText.Fix(DistanceText);
            if (rankLabel != null) rankLabel.text = VietText.Fix(RankText);
            if (cadenceLabel != null) cadenceLabel.text = VietText.Fix(CadenceText);
            if (distanceBar != null) distanceBar.SetValue(progress);
            if (staminaBar != null)
            {
                staminaBar.SetValue(snapshot.Stamina / 100f);
                staminaBar.SetFillColor(StaminaColor(snapshot.Stamina));
            }
        }

        static Color StaminaColor(float stamina) =>
            SprintRules.ClassifyStamina(stamina) switch
            {
                StaminaBand.Low => LowStaminaColor,
                StaminaBand.Mid => MinigameUiTheme.Energy,
                _ => MinigameUiTheme.Success
            };

        public bool HasBoundVisuals => metricsRoot != null && distanceLabel != null && rankLabel != null &&
            cadenceLabel != null && distanceBar != null;

        void CacheVisuals()
        {
            var hud = GameObject.Find("S2_HUD_Minigame");
            if (hud == null)
                return;

            var chrome = hud.transform.Find("SafeAreaRoot/SprintBroadcastChrome");
            if (chrome == null)
                return;

            metricsRoot = chrome.Find("Scoreboard");
            if (metricsRoot == null)
                return;

            distanceLabel = metricsRoot.Find("Distance")?.GetComponent<TMP_Text>();
            rankLabel = metricsRoot.Find("RankBadge/RankLabel")?.GetComponent<TMP_Text>();
            cadenceLabel = metricsRoot.Find("Combo")?.GetComponent<TMP_Text>();
            distanceBar = chrome.Find("ProgressRail")?.GetComponent<KitBar>();
            staminaBar = metricsRoot.Find("StaminaBar")?.GetComponent<KitBar>();
        }
    }
}
