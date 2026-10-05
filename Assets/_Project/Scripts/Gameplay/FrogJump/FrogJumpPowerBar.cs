using KMA.UI.Kit;
using UnityEngine;

namespace KMA.Gameplay.FrogJump
{
    public sealed class FrogJumpPowerBar : MonoBehaviour
    {
        [SerializeField] RectTransform track;
        [SerializeField] RectTransform needle;

        public bool NeedleVisible => needle != null && needle.gameObject.activeSelf;

        public void Configure(RectTransform trackRect, RectTransform needleRect)
        {
            track = trackRect;
            needle = needleRect;
        }

        public void SetNeedle(float needle01, bool visible)
        {
            if (needle == null || track == null) return;
            needle.gameObject.SetActive(visible);
            needle.anchorMin = needle.anchorMax = new Vector2(Mathf.Clamp01(needle01), .5f);
            needle.anchoredPosition = Vector2.zero;
        }

        /// Colour of a segment centred at needle01: green that fades towards the safe edge, red past it.
        public static Color SegmentColor(float needle01, FrogJumpTuning tuning)
        {
            if (FrogJumpRules.IsFall(needle01, tuning)) return MinigameUiTheme.Energy;
            float jump01 = Mathf.InverseLerp(tuning.minJumpMetres, tuning.maxJumpMetres,
                FrogJumpRules.JumpMetres(needle01, tuning));
            return Color.Lerp(MinigameUiTheme.WithAlpha(MinigameUiTheme.Success, .45f), MinigameUiTheme.Success, jump01);
        }
    }
}
