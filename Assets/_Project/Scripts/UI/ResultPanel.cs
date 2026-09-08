using System;
using KMA.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class ResultPanel : MonoBehaviour, IResultPreviewPanel
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
        public bool HasContinued { get; private set; }

        void Awake()
        {
            if (actionButton != null)
                actionButton.onClick.AddListener(Continue);
        }

        public void Show(MinigameResult result, string previewRoute)
        {
            CurrentResult = result ?? throw new ArgumentNullException(nameof(result));
            PreviewRoute = previewRoute ?? string.Empty;
            HasContinued = false;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (contentRoot != null)
                contentRoot.SetActive(true);
            if (statusLabel != null)
            {
                statusLabel.text = result.Pass ? "CHIẾN THẮNG" : "THẤT BẠI";
                if (theme != null)
                    statusLabel.color = result.Pass ? theme.Success : theme.Primary;
            }
            if (scoreLabel != null)
                scoreLabel.text = Mathf.RoundToInt(result.Score).ToString();
            if (rankLabel != null)
                rankLabel.text = $"XẾP HẠNG {result.Rank}";

            if (actionButton != null)
            {
                var label = actionButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = "TIẾP TỤC";
            }
        }

        public void Continue()
        {
            if (CurrentResult != null && !HasContinued)
            {
                HasContinued = true;
                ActionRequested?.Invoke(PreviewRoute);
            }
        }
    }
}
