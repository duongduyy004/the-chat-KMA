using UnityEngine;

namespace KMA.Gameplay
{
    /// The one set of campus backdrop sprites and colours every scene draws from.
    [CreateAssetMenu(menuName = "KMA/Campus Backdrop Art", fileName = "CampusBackdropArt")]
    public sealed class CampusBackdropArt : ScriptableObject
    {
        [SerializeField] Sprite sky;
        [SerializeField] Sprite skyline;
        [SerializeField] Sprite pixel;
        [SerializeField] Color skyColor = new Color32(0x2e, 0x9b, 0xe6, 0xff);
        [SerializeField] Color groundColor = new Color32(0x62, 0xb5, 0x4a, 0xff);

        public Sprite Sky => sky;
        public Sprite Skyline => skyline;
        public Sprite Pixel => pixel;
        public Color SkyColor => skyColor;
        public Color GroundColor => groundColor;

        public void Configure(Sprite skySprite, Sprite skylineSprite, Sprite pixelSprite, Color sky, Color ground)
        {
            this.sky = skySprite;
            skyline = skylineSprite;
            pixel = pixelSprite;
            skyColor = sky;
            groundColor = ground;
        }

        public static float Aspect(Sprite sprite) => sprite.rect.width / sprite.rect.height;
    }
}
