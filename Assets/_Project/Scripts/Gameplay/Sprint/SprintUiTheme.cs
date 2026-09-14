using UnityEngine;

namespace KMA.Gameplay
{
    /// Design tokens for the Sprint minigame HUD. Pure data: no scene access.
    public static class SprintUiTheme
    {
        public static readonly Color Surface = new Color32(8, 35, 61, 255);
        public static readonly Color TextPrimary = new Color32(255, 249, 231, 255);
        public static readonly Color Accent = new Color32(255, 202, 58, 255);
        public static readonly Color Player = new Color32(58, 230, 255, 255);
        public static readonly Color Energy = new Color32(255, 89, 94, 255);
        public static readonly Color TextOutline = new Color32(3, 18, 33, 255);

        public const float Display = 160f;
        public const float Title = 54f;
        public const float Headline = 48f;
        public const float BodyLarge = 40f;
        public const float Body = 32f;
        public const float Caption = 24f;
        public const float MinimumFontSize = 24f;

        public const float RadiusPanel = 24f;
        public const float RadiusControl = 36f;
        public const float RadiusPause = 20f;
        public const float BorderWidth = 3f;

        public const float SpaceXs = 8f;
        public const float SpaceSm = 16f;
        public const float SpaceMd = 24f;
        public const float SpaceLg = 32f;

        public static readonly Color ShadowColor = new Color(0f, 0f, 0f, .35f);
        public static readonly Vector2 ShadowOffset = new Vector2(0f, -4f);

        public static float[] AllFontSizes() =>
            new[] { Display, Title, Headline, BodyLarge, Body, Caption };

        public static Color[] TextColors() =>
            new[] { TextPrimary, Accent, Player, Energy };

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        /// WCAG 2.1 relative-luminance contrast ratio, from 1 to 21.
        public static float ContrastRatio(Color a, Color b)
        {
            float first = RelativeLuminance(a);
            float second = RelativeLuminance(b);
            float lighter = Mathf.Max(first, second);
            float darker = Mathf.Min(first, second);
            return (lighter + .05f) / (darker + .05f);
        }

        static float RelativeLuminance(Color color) =>
            .2126f * Channel(color.r) + .7152f * Channel(color.g) + .0722f * Channel(color.b);

        static float Channel(float value) =>
            value <= .03928f ? value / 12.92f : Mathf.Pow((value + .055f) / 1.055f, 2.4f);
    }
}
