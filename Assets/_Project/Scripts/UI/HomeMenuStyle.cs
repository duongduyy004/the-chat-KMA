using UnityEngine;

namespace KMA.Gameplay.UI
{
    // Menu layout constants and access to the shared theme. The canvas is authored at 1920 x 1080 reference resolution.
    public static class HomeMenuStyle
    {
        public static Color Navy => UITheme.Shared.Menu.navy;
        public static Color Gold => UITheme.Shared.Menu.gold;
        public static Color GoldLight => UITheme.Shared.Menu.goldLight;
        public static Color GoldDark => UITheme.Shared.Menu.goldDark;
        public static Color Red => UITheme.Shared.Menu.red;
        public static Color Grass => UITheme.Shared.Menu.grass;
        public static Color White => UITheme.Shared.Card;
        public static Color Glass => KMA.UI.Kit.MinigameUiTheme.WithAlpha(Navy, 190f / 255f);
        public static readonly Color Cream = new Color32(251, 246, 232, 255);
        public static readonly Color Coral = new Color32(240, 98, 78, 255);
        public static readonly Color CreamDisabled = new Color32(226, 222, 210, 255);
        public const float BadgeSize = 132f;
        public const float TitleSize = 66f;
        public const float TitleTopSize = 48f;
        public const float TitleKmaSize = 94f;
        public const float PanelWidth = 500f;
        public const float PanelHeight = 640f;
        public const float MenuScale = 1.4f;
        public const float ButtonWidth = 424f;
        public const float ButtonHeight = 66f;
        public const float ButtonStep = 82f;
    }
}
