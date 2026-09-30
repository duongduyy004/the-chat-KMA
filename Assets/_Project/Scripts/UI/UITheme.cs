using System;
using KMA.UI.Kit;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    [CreateAssetMenu(menuName = "KMA/UI Theme", fileName = "UITheme")]
    public sealed class UITheme : ScriptableObject
    {
        [SerializeField] private Color primary = new Color32(0xFF, 0x59, 0x5E, 0xFF);
        [SerializeField] private Color accent = new Color32(0xFF, 0xCA, 0x3A, 0xFF);
        [SerializeField] private Color background = new Color32(0x19, 0x82, 0xC4, 0xFF);
        [SerializeField] private Color success = new Color32(0x8A, 0xCB, 0x88, 0xFF);
        [SerializeField] private Color card = Color.white;
        [SerializeField] private Color muted = new Color32(0xE2, 0xE8, 0xF0, 0xFF);
        [SerializeField] private Color mutedForeground = new Color32(0x47, 0x55, 0x69, 0xFF);
        [SerializeField] private Color border = Color.black;
        [SerializeField] private float spacing = 8f;
        [Min(0f)] [SerializeField] private float cornerRadius = 24f;
        [Min(0f)] [SerializeField] private float borderWidth = 4f;
        [SerializeField] private Vector2 shadowOffset = new Vector2(6f, -6f);

        // UiKitAssets lives in Resources and references this asset, including in player builds.
        public static UITheme Shared
        {
            get
            {
                UITheme theme = UiKitAssets.Load().Theme;
                return theme != null ? theme
                    : throw new InvalidOperationException("Assign UITheme on the shared UiKitAssets asset.");
            }
        }

        [Header("Shared surfaces")]
        [SerializeField] private Color surface = new Color32(8, 35, 61, 255);
        [SerializeField] private Color textPrimary = new Color32(255, 249, 231, 255);
        [SerializeField] private Color textOutline = new Color32(3, 18, 33, 255);
        [SerializeField] private Color player = new Color32(58, 230, 255, 255);
        [SerializeField] private Color resultSuccess = new Color32(94, 222, 140, 255);
        [SerializeField] private Color scrim = new Color(3f / 255f, 18f / 255f, 33f / 255f, .7f);
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, .35f);
        [Header("Screen palette variants")]
        [SerializeField] private Color settingsBackdrop = new Color(.02f, .06f, .1f, .85f);
        public Color SettingsBackdrop => settingsBackdrop;
        [SerializeField] private Color settingsHint = new Color32(190, 219, 239, 255);
        public Color SettingsHint => settingsHint;
        [SerializeField] private Color settingsTrack = new Color32(48, 74, 99, 255);
        public Color SettingsTrack => settingsTrack;
        [SerializeField] private Color mapBackgroundTint = new Color32(10, 48, 82, 255);
        public Color MapBackgroundTint => mapBackgroundTint;
        [SerializeField] private Color mapHint = new Color32(201, 226, 245, 255);
        public Color MapHint => mapHint;
        [SerializeField] private Color mapLivesSurface = new Color32(13, 57, 92, 238);
        public Color MapLivesSurface => mapLivesSurface;
        [SerializeField] private Color mapLockedBorder = new Color32(117, 138, 156, 255);
        public Color MapLockedBorder => mapLockedBorder;
        [SerializeField] private Color mapReadyText = new Color32(12, 105, 94, 255);
        public Color MapReadyText => mapReadyText;
        [SerializeField] private Color mapActionText = new Color32(163, 104, 0, 255);
        public Color MapActionText => mapActionText;
        [SerializeField] private Color mapLockIcon = new Color32(38, 60, 77, 255);
        public Color MapLockIcon => mapLockIcon;
        [SerializeField] private Color mapButtonHighlight = new Color32(255, 236, 170, 255);
        public Color MapButtonHighlight => mapButtonHighlight;
        [SerializeField] private Color mapCompleteBorder = new Color32(65, 170, 104, 255);
        public Color MapCompleteBorder => mapCompleteBorder;
        [SerializeField] private Color mapLockedCard = new Color32(184, 199, 211, 255);
        public Color MapLockedCard => mapLockedCard;
        [SerializeField] private Color mapLockedIcon = new Color32(124, 144, 160, 255);
        public Color MapLockedIcon => mapLockedIcon;
        [SerializeField] private Color mapLockedText = new Color32(56, 75, 90, 255);
        public Color MapLockedText => mapLockedText;
        [Header("Menu palette and geometry")]
        [SerializeField] private MenuStyle menu = new MenuStyle();
        [Header("Animation (unscaled seconds)")]
        [SerializeField] private MotionStyle motion = new MotionStyle();

        public Color Surface => surface;
        public Color TextPrimary => textPrimary;
        public Color TextOutline => textOutline;
        public Color Player => player;
        public Color ResultSuccess => resultSuccess;
        public Color Scrim => scrim;
        public Color ShadowColor => shadowColor;
        public MenuStyle Menu => menu;
        public MotionStyle Motion => motion;

        [Serializable]
        public sealed class MenuStyle
        {
            public Color navy = new Color32(11, 42, 74, 255);
            public Color gold = new Color32(255, 201, 40, 255);
            public Color goldLight = new Color32(255, 224, 102, 255);
            public Color goldDark = new Color32(255, 180, 0, 255);
            public Color red = new Color32(226, 85, 61, 255);
            public Color grass = new Color32(107, 164, 58, 255);
            [Range(-45f, 45f)] public float titleAngle = -8f;
            [Range(0f, 40f)] public float buttonSlantAngle = 12.094757f;
            [Range(.5f, 1f)] public float pressScale = .96f;
        }

        [Serializable]
        public sealed class MotionStyle
        {
            [Min(.001f)] public float pressRestore = .1f;
            [Range(.5f, 1f)] public float pressScale = .94f;
            [Range(0f, 1f)] public float pressLighten = .15f;
            [Min(0f)] public float buttonFade = .08f;
            [Min(0f)] public float resultScrim = .12f;
            [Min(0f)] public float resultModal = .18f;
            [Min(0f)] public float resultTitle = .14f;
            [Min(0f)] public float resultScore = .35f;
            [Min(0f)] public float resultRank = .16f;
            [Min(.001f)] public float badgeShineDuration = .8f;
            [Min(.001f)] public float badgeShineCycle = 4.5f;
        }

        public Color Primary => primary;
        public Color Accent => accent;
        public Color Background => background;
        public Color Success => success;
        public Color Card => card;
        public Color Muted => muted;
        public Color MutedForeground => mutedForeground;
        public Color Border => border;
        public float Spacing => spacing;
        public float CornerRadius => Mathf.Max(0f, cornerRadius);
        public float BorderWidth => Mathf.Max(0f, borderWidth);
        public Vector2 ShadowOffset => shadowOffset;
    }
}
