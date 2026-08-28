using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class MinigameHUD : MonoBehaviour
    {
        [SerializeField] MonoBehaviour minigameSource;
        [SerializeField] UITheme theme;
        [SerializeField] TMP_Text timeLabel;
        [SerializeField] TMP_Text primaryLabel;
        [SerializeField] TMP_Text secondaryLabel;
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] Image primaryFill;
        [SerializeField] Image secondaryFill;

        void Update()
        {
            if (minigameSource is IMinigameHudStateSource source)
                RefreshFrom(source.ReadHudState());
        }

        public void RefreshFrom(MinigameHudState state)
        {
            if (timeLabel != null)
                timeLabel.text = Mathf.CeilToInt(Mathf.Max(0f, state.timeRemaining)).ToString();
            if (primaryLabel != null)
                primaryLabel.text = state.primaryLabel ?? string.Empty;
            if (secondaryLabel != null)
                secondaryLabel.text = state.secondaryLabel ?? string.Empty;
            if (statusLabel != null)
                statusLabel.text = state.statusText ?? string.Empty;
            if (primaryFill != null)
            {
                primaryFill.fillAmount = Mathf.Clamp01(state.primary01);
                if (theme != null)
                    primaryFill.color = theme.Success;
            }
            if (secondaryFill != null)
            {
                secondaryFill.fillAmount = Mathf.Clamp01(state.secondary01);
                if (theme != null)
                    secondaryFill.color = theme.Accent;
            }
        }
    }
}
