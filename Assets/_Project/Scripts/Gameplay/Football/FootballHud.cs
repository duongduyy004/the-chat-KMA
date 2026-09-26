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

        FootballDifficulty selectedDifficulty = FootballDifficulty.Normal;
        bool startSubscribed;

        public event Action<FootballDifficulty> StartRequested;
        public Button AimButton => aimButton;
        public FootballHoldButton ShootButton => shootButton;

        public void Configure(Button aim, FootballHoldButton shoot, Image fill, TMP_Text percent,
            GameObject warning, TMP_Text score, TMP_Text remaining, TMP_Text[] markers, GameObject start)
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
            SubscribeStart();
        }

        public bool ValidateReferences() => aimButton && shootButton && powerFill && powerPercent && overPowerWarning &&
            scoreLabel && remainingLabel && kickMarkers != null && kickMarkers.Length == 5 &&
            Array.TrueForAll(kickMarkers, marker => marker) && startPanel;

        public void ShowStart(FootballDifficulty selected)
        {
            selectedDifficulty = selected;
            if (startPanel)
                startPanel.SetActive(true);
            if (aimButton) aimButton.interactable = false;
            if (shootButton) shootButton.SetInteractable(false);
        }

        public void SetDifficulty(FootballDifficulty selected) => selectedDifficulty = selected;

        public void RequestStart() => StartRequested?.Invoke(selectedDifficulty);

        public void Render(FootballRules rules)
        {
            if (rules == null)
                return;
            if (scoreLabel) scoreLabel.text = rules.Goals.ToString();
            if (remainingLabel) remainingLabel.text = Mathf.Max(0, 5 - rules.Kicks).ToString();
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

        void OnEnable() => SubscribeStart();
        void OnDisable()
        {
            if (startSubscribed && startButton)
                startButton.onClick.RemoveListener(RequestStart);
            startSubscribed = false;
        }

        void SubscribeStart()
        {
            if (startSubscribed || !startButton)
                return;
            startButton.onClick.AddListener(RequestStart);
            startSubscribed = true;
        }
    }
}
