using System;
using KMA.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class ResultPanel : MonoBehaviour
    {
        [SerializeField] GameObject contentRoot;
        [SerializeField] UITheme theme;
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text rankLabel;
        [SerializeField] Button actionButton;

        public event Action<string> ActionRequested;

        public MinigameResult CurrentResult { get; private set; }
        public string PreviewRoute { get; private set; } = string.Empty;

        void Awake()
        {
            if (actionButton != null)
                actionButton.onClick.AddListener(Continue);
        }

        public void Show(MinigameResult result, string previewRoute)
        {
            CurrentResult = result ?? throw new ArgumentNullException(nameof(result));
            PreviewRoute = previewRoute ?? string.Empty;

            if (contentRoot != null)
                contentRoot.SetActive(true);
            if (statusLabel != null)
            {
                statusLabel.text = result.Pass ? "PASS" : "FAIL";
                if (theme != null)
                    statusLabel.color = result.Pass ? theme.Success : theme.Primary;
            }
            if (scoreLabel != null)
                scoreLabel.text = Mathf.RoundToInt(result.Score).ToString();
            if (rankLabel != null)
                rankLabel.text = $"RANK {result.Rank}";
        }

        public void Continue()
        {
            if (CurrentResult != null)
                ActionRequested?.Invoke(PreviewRoute);
        }
    }
}
