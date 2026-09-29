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
            float progress = Mathf.Clamp01(snapshot.Distance / 100f);

            DistanceText = $"{Mathf.RoundToInt(snapshot.Distance)} / 100 m";
            RankText = controller.RankText;
            CadenceText = $"COMBO ×{controller.CadenceCombo}";
            PipProgress = progress;

            if (distanceLabel != null) distanceLabel.text = DistanceText;
            if (rankLabel != null) rankLabel.text = RankText;
            if (cadenceLabel != null) cadenceLabel.text = CadenceText;
            if (distanceBar != null) distanceBar.SetValue(progress);
        }

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
        }
    }
}
