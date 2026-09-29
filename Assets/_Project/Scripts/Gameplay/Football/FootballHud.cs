using System;
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class FootballHud : MonoBehaviour
    {
        const float OverPowerThreshold = .85f;

        [SerializeField] Slider directionSlider;
        [SerializeField] TMP_Text directionLabel;
        [SerializeField] TMP_Text shotFeedback;
        [SerializeField] FootballHoldButton shootButton;
        [SerializeField] KitBar powerBar;
        [SerializeField] GameObject overPowerWarning;
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text remainingLabel;
        [SerializeField] Image[] kickMarkers = new Image[5];
        [SerializeField] GameObject startPanel;
        [SerializeField] Button startButton;
        [SerializeField] Button easyButton;
        [SerializeField] Button normalButton;
        [SerializeField] Button hardButton;

        FootballDifficulty selectedDifficulty = FootballDifficulty.Normal;
        bool listenersBound;

        public event Action<FootballDifficulty> StartRequested;
        public Slider DirectionSlider => directionSlider;
        public FootballHoldButton ShootButton => shootButton;

        public void Configure(Slider aim, FootballHoldButton shoot, KitBar power, GameObject warning, TMP_Text score,
            TMP_Text remaining, Image[] markers, GameObject start, Button startAction = null, Button easy = null,
            Button normal = null, Button hard = null, TMP_Text directionText = null, TMP_Text feedback = null)
        {
            directionSlider = aim;
            shootButton = shoot;
            powerBar = power;
            overPowerWarning = warning;
            scoreLabel = score;
            remainingLabel = remaining;
            kickMarkers = markers;
            startPanel = start;
            startButton = startAction;
            easyButton = easy;
            normalButton = normal;
            hardButton = hard;
            directionLabel = directionText;
            shotFeedback = feedback;
            BindButtons();
            UpdateDifficultyButtons();
        }

        public bool ValidateReferences() => directionSlider && shootButton && powerBar && overPowerWarning &&
            scoreLabel && remainingLabel && kickMarkers != null && kickMarkers.Length == 5 &&
            Array.TrueForAll(kickMarkers, marker => marker) && startPanel;

        public void ShowStart(FootballDifficulty selected)
        {
            selectedDifficulty = selected;
            if (startPanel)
                startPanel.SetActive(true);
            if (directionSlider) directionSlider.interactable = false;
            if (shootButton) shootButton.SetInteractable(false);
            UpdateDifficultyButtons();
        }

        public void HideStart()
        {
            if (startPanel) startPanel.SetActive(false);
        }

        public void SetDifficulty(FootballDifficulty selected)
        {
            selectedDifficulty = selected;
            UpdateDifficultyButtons();
        }

        public void RequestStart() => StartRequested?.Invoke(selectedDifficulty);

        public void Render(FootballRules rules)
        {
            if (rules == null)
                return;
            bool overPower = rules.Power > OverPowerThreshold;
            if (scoreLabel) scoreLabel.text = "BÀN: " + rules.Goals;
            if (remainingLabel) remainingLabel.text = "CÒN " + Mathf.Max(0, 5 - rules.Kicks) + " LƯỢT";
            if (powerBar)
            {
                powerBar.SetValue(rules.Power);
                powerBar.SetFillColor(overPower ? MinigameUiTheme.Energy : MinigameUiTheme.Accent);
                if (powerBar.Label) powerBar.Label.text = Mathf.RoundToInt(rules.Power * 100f) + "%";
            }
            if (overPowerWarning) overPowerWarning.SetActive(overPower);
            for (int i = 0; kickMarkers != null && i < kickMarkers.Length; i++)
            {
                if (!kickMarkers[i]) continue;
                kickMarkers[i].color = i < rules.Outcomes.Count
                    ? (rules.Outcomes[i] == FootballOutcome.Goal ? MinigameUiTheme.Accent : MinigameUiTheme.Energy)
                    : MinigameUiTheme.Track;
            }
            if (startPanel && rules.State != FootballState.Start)
                startPanel.SetActive(false);
            if (directionSlider) directionSlider.SetValueWithoutNotify(rules.AimX);
            if (directionLabel) directionLabel.text = Mathf.Abs(rules.AimX) < .02f ? "GIỮA" :
                (rules.AimX < 0f ? "TRÁI " : "PHẢI ") + Mathf.RoundToInt(Mathf.Abs(rules.AimX) * 100f) + "%";
            if (shotFeedback) shotFeedback.text = OutcomeText(rules.Flight?.Outcome);
        }

        static string OutcomeText(FootballOutcome? outcome) => outcome switch
        {
            FootballOutcome.Goal => "VÀO!",
            FootballOutcome.Saved => "THỦ MÔN CẢN PHÁ",
            FootballOutcome.Post => "TRÚNG CỘT DỌC",
            FootballOutcome.Crossbar => "TRÚNG XÀ NGANG",
            FootballOutcome.Wide => "CHỆCH KHUNG THÀNH",
            FootballOutcome.High => "BÓNG VƯỢT XÀ",
            FootballOutcome.Short => "BÓNG DỪNG TRƯỚC GOAL",
            _ => string.Empty
        };

        void OnEnable() => BindButtons();
        void OnDisable()
        {
            if (!listenersBound) return;
            if (startButton) startButton.onClick.RemoveListener(RequestStart);
            if (easyButton) easyButton.onClick.RemoveListener(SelectEasy);
            if (normalButton) normalButton.onClick.RemoveListener(SelectNormal);
            if (hardButton) hardButton.onClick.RemoveListener(SelectHard);
            listenersBound = false;
        }

        void BindButtons()
        {
            if (listenersBound) return;
            if (startButton) startButton.onClick.AddListener(RequestStart);
            if (easyButton) easyButton.onClick.AddListener(SelectEasy);
            if (normalButton) normalButton.onClick.AddListener(SelectNormal);
            if (hardButton) hardButton.onClick.AddListener(SelectHard);
            listenersBound = startButton || easyButton || normalButton || hardButton;
        }

        void SelectEasy() => SetDifficulty(FootballDifficulty.Easy);
        void SelectNormal() => SetDifficulty(FootballDifficulty.Normal);
        void SelectHard() => SetDifficulty(FootballDifficulty.Hard);

        void UpdateDifficultyButtons()
        {
            SetDifficultyVariant(easyButton, selectedDifficulty == FootballDifficulty.Easy);
            SetDifficultyVariant(normalButton, selectedDifficulty == FootballDifficulty.Normal);
            SetDifficultyVariant(hardButton, selectedDifficulty == FootballDifficulty.Hard);
        }

        static void SetDifficultyVariant(Button button, bool selected)
        {
            if (button && button.GetComponent<KitPressFeedback>())
                UiKit.ApplyVariant(UiKit.ButtonParts(button), selected ? ButtonVariant.Primary : ButtonVariant.Secondary);
        }
    }
}
