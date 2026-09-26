using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class FootballHud : MonoBehaviour
    {
        [SerializeField] Button aimButton;
        [SerializeField] FootballHoldButton shootButton;
        [SerializeField] Image powerFill;
        [SerializeField] TMP_Text powerPercent;
        [SerializeField] GameObject overPowerWarning;
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text remainingLabel;
        [SerializeField] TMP_Text[] kickMarkers = new TMP_Text[5];
        [SerializeField] GameObject startPanel;
        [SerializeField] Button startButton;
        [SerializeField] Button easyButton;
        [SerializeField] Button normalButton;
        [SerializeField] Button hardButton;
        [SerializeField] GameObject countdownPanel;
        [SerializeField] TMP_Text countdownLabel;

        FootballDifficulty selectedDifficulty = FootballDifficulty.Normal;
        bool listenersBound;

        public event Action<FootballDifficulty> StartRequested;
        public Button AimButton => aimButton;
        public FootballHoldButton ShootButton => shootButton;

        public void Configure(Button aim, FootballHoldButton shoot, Image fill, TMP_Text percent,
            GameObject warning, TMP_Text score, TMP_Text remaining, TMP_Text[] markers, GameObject start,
            Button startAction = null, Button easy = null, Button normal = null, Button hard = null,
            GameObject countdown = null, TMP_Text countdownText = null)
        {
            aimButton = aim;
            shootButton = shoot;
            powerFill = fill;
            powerPercent = percent;
            overPowerWarning = warning;
            scoreLabel = score;
            remainingLabel = remaining;
            kickMarkers = markers;
            startPanel = start;
            startButton = startAction;
            easyButton = easy;
            normalButton = normal;
            hardButton = hard;
            countdownPanel = countdown;
            countdownLabel = countdownText;
            BindButtons();
            UpdateDifficultyButtons();
        }

        public bool ValidateReferences() => aimButton && shootButton && powerFill && powerPercent && overPowerWarning &&
            scoreLabel && remainingLabel && kickMarkers != null && kickMarkers.Length == 5 &&
            Array.TrueForAll(kickMarkers, marker => marker) && startPanel;

        public void ShowStart(FootballDifficulty selected)
        {
            selectedDifficulty = selected;
            if (startPanel)
                startPanel.SetActive(true);
            if (countdownPanel)
                countdownPanel.SetActive(false);
            if (aimButton) aimButton.interactable = false;
            if (shootButton) shootButton.SetInteractable(false);
            UpdateDifficultyButtons();
        }

        public void HideStart()
        {
            if (startPanel) startPanel.SetActive(false);
        }

        public void ShowCountdown(string message)
        {
            HideStart();
            if (countdownLabel) countdownLabel.text = message ?? string.Empty;
            if (countdownPanel) countdownPanel.SetActive(true);
        }

        public void HideCountdown()
        {
            if (countdownPanel) countdownPanel.SetActive(false);
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
            if (scoreLabel) scoreLabel.text = "BÀN: " + rules.Goals;
            if (remainingLabel) remainingLabel.text = "CÒN " + Mathf.Max(0, 5 - rules.Kicks) + " LƯỢT";
            if (powerFill) powerFill.fillAmount = rules.Power;
            if (powerPercent) powerPercent.text = Mathf.RoundToInt(rules.Power * 100f) + "%";
            if (overPowerWarning) overPowerWarning.SetActive(rules.Power > .85f);
            for (int i = 0; kickMarkers != null && i < kickMarkers.Length; i++)
            {
                if (!kickMarkers[i]) continue;
                kickMarkers[i].text = i < rules.Outcomes.Count ? rules.Outcomes[i].ToString().ToUpperInvariant() : "•";
            }
            if (startPanel && rules.State != FootballState.Start)
                startPanel.SetActive(false);
            if (aimButton) aimButton.interactable = rules.State == FootballState.Aiming;
            if (shootButton) shootButton.SetInteractable(rules.State == FootballState.AimLocked || rules.State == FootballState.Charging);
        }

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
            SetDifficultyTint(easyButton, selectedDifficulty == FootballDifficulty.Easy);
            SetDifficultyTint(normalButton, selectedDifficulty == FootballDifficulty.Normal);
            SetDifficultyTint(hardButton, selectedDifficulty == FootballDifficulty.Hard);
        }

        static void SetDifficultyTint(Button button, bool selected)
        {
            if (button && button.targetGraphic)
                button.targetGraphic.color = selected ? new Color32(255, 211, 76, 255) : Color.white;
        }
    }
}
