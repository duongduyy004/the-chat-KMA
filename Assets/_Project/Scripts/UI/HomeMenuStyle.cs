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
        public const float PanelWidth = 560f;
        public const float PanelHeight = 700f;
        public const float MenuScale = 1.7f;
        public const float ButtonWidth = 320f;
        public const float ButtonHeight = 56f;
        public const float ButtonStep = 70f;
    }
}
