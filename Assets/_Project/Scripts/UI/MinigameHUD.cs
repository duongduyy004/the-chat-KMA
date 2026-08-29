using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class MinigameHUD : MonoBehaviour
    {
        [SerializeField] MonoBehaviour minigameSource;
        [SerializeField] UITheme theme;
        [SerializeField] TMP_Text timeLabel;
        [FormerlySerializedAs("primaryLabel")]
        [SerializeField] TMP_Text phaseLabel;
        [FormerlySerializedAs("secondaryLabel")]
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text statusLabel;
        [FormerlySerializedAs("secondaryFill")]
        [SerializeField] Image progressFill;
        [FormerlySerializedAs("primaryFill")]
        [SerializeField] Image staminaFill;

        void Update()
        {
            if (minigameSource is IMinigameHudStateSource source)
                RefreshFrom(source.ReadHudState());
        }

        public void RefreshFrom(MinigameHudState state)
        {
            if (timeLabel != null)
                timeLabel.text = Mathf.CeilToInt(Mathf.Max(0f, state.timeRemaining)).ToString();
            if (phaseLabel != null)
                phaseLabel.text = state.phase ?? string.Empty;
            if (scoreLabel != null)
                scoreLabel.text = Mathf.RoundToInt(Mathf.Max(0f, state.score)).ToString();
            if (statusLabel != null)
                statusLabel.text = state.statusText ?? string.Empty;
            if (progressFill != null)
            {
                progressFill.fillAmount = Mathf.Clamp01(state.progress01);
                if (theme != null)
                    progressFill.color = theme.Accent;
            }
            if (staminaFill != null)
            {
                staminaFill.fillAmount = Mathf.Clamp01(state.stamina01);
                if (theme != null)
                    staminaFill.color = theme.Success;
            }
        }
    }
}
