using UnityEngine;

namespace KMA.UI.Kit
{
    /// Design tokens shared by every minigame's UI. Pure data: no scene access.
    public static class MinigameUiTheme
    {
        public static readonly Color Surface = new Color32(8, 35, 61, 255);
        public static readonly Color TextPrimary = new Color32(255, 249, 231, 255);
        public static readonly Color Accent = new Color32(255, 202, 58, 255);
        public static readonly Color Player = new Color32(58, 230, 255, 255);
        public static readonly Color Energy = new Color32(255, 89, 94, 255);
        public static readonly Color TextOutline = new Color32(3, 18, 33, 255);
        public static readonly Color Success = new Color32(94, 222, 140, 255);
        public static readonly Color Track = new Color(1f, 1f, 1f, .22f);
        public static readonly Color Scrim = new Color(3f / 255f, 18f / 255f, 33f / 255f, .7f);

        public const float SurfaceOpaque = .92f;
        public const float SurfaceSoft = .82f;
        public const float SurfaceControl = .42f;
        public const float SurfaceControlActive = .55f;
        public const float SurfaceDisabled = .30f;
        public const float BorderRest = .25f;
        public const float BorderHint = .75f;
        public const float BorderDisabled = .15f;
        public const float DisabledAlpha = .45f;

        public const float Display = 160f;
        public const float Title = 54f;
        public const float Headline = 48f;
        public const float BodyLarge = 40f;
        public const float Body = 32f;
        public const float Caption = 28f;
        public const float MinimumFontSize = 28f;

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

        public const float PressScale = .94f;
        public const float PressLighten = .15f;
        public const float PressRestoreSeconds = .1f;

        public const float ButtonHeight = 88f;
        public const float RoundButton = 220f;
        public const float JoystickBase = 224f;
        public const float JoystickKnob = 112f;
        public const float BarHeight = 32f;
        public const float SliderKnob = 56f;

        public static float[] AllFontSizes() =>
            new[] { Display, Title, Headline, BodyLarge, Body, Caption };

        public static Color[] TextColors() =>
            new[] { TextPrimary, Accent, Player, Energy, Success };

        /// Every colour a minigame UI may draw with, compared without alpha (Track is white).
        public static Color[] Palette() =>
            new[] { Surface, TextPrimary, Accent, Player, Energy, TextOutline, Success, Track };

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        /// Moves a colour toward white by amount (0..1) and keeps its alpha.
        public static Color Lighten(Color color, float amount)
        {
            Color lighter = Color.Lerp(color, Color.white, Mathf.Clamp01(amount));
            lighter.a = color.a;
            return lighter;
        }

        /// True when both colours have the same 8-bit RGB, whatever their alpha.
        public static bool RgbEquals(Color a, Color b)
        {
            Color32 first = a;
            Color32 second = b;
            return first.r == second.r && first.g == second.g && first.b == second.b;
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
