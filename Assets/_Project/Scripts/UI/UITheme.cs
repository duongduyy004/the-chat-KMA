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
        [SerializeField] private float cornerRadius = 24f;
        [SerializeField] private float borderWidth = 4f;
        [SerializeField] private Vector2 shadowOffset = new Vector2(6f, -6f);

        public Color Primary => primary;
        public Color Accent => accent;
        public Color Background => background;
        public Color Success => success;
        public Color Card => card;
        public Color Muted => muted;
        public Color MutedForeground => mutedForeground;
        public Color Border => border;
        public float Spacing => spacing;
        public float CornerRadius => cornerRadius;
        public float BorderWidth => borderWidth;
        public Vector2 ShadowOffset => shadowOffset;
    }
}
