using UnityEngine;

namespace KMA.Gameplay.UI
{
    // Menu-only design tokens. The canvas is authored at 1920 x 1080 reference resolution.
    public static class HomeMenuStyle
    {
        public static readonly Color Navy = new Color32(11, 42, 74, 255);
        public static readonly Color Gold = new Color32(255, 201, 40, 255);
        public static readonly Color GoldLight = new Color32(255, 224, 102, 255);
        public static readonly Color GoldDark = new Color32(255, 180, 0, 255);
        public static readonly Color Red = new Color32(226, 85, 61, 255);
        public static readonly Color Grass = new Color32(107, 164, 58, 255);
        public static readonly Color White = Color.white;
        public static readonly Color Glass = new Color32(11, 42, 74, 190);
        public const float PanelWidth = 560f;
        public const float PanelHeight = 700f;
        public const float MenuScale = 1.7f;
        public const float ButtonWidth = 320f;
        public const float ButtonHeight = 56f;
        public const float ButtonStep = 70f;
    }
}
