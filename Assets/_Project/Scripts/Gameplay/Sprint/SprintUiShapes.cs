using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay
{
    /// Generates rounded-rect sprites at runtime so the Sprint HUD needs no bitmap assets.
    /// Every sprite is white; callers tint through Image.color.
    public static class SprintUiShapes
    {
        static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        public static Sprite RoundedRect(int radius)
        {
            if (radius < 0)
                throw new ArgumentOutOfRangeException(nameof(radius), "Corner radius cannot be negative.");

            if (Cache.TryGetValue(radius, out Sprite cached) && cached != null)
                return cached;

            int size = radius > 0 ? radius * 2 + 2 : 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"SprintRoundedRect{radius}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(Coverage(x, y, size, radius)) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;

            Cache[radius] = sprite;
            return sprite;
        }

        public static void ClearCacheForTest() => Cache.Clear();

        /// Signed coverage of one pixel by the rounded rectangle, antialiased over one pixel.
        static float Coverage(int x, int y, int size, int radius)
        {
            if (radius == 0)
                return 1f;

            float pixelX = x + .5f;
            float pixelY = y + .5f;
            float dx = Mathf.Max(radius - pixelX, pixelX - (size - radius), 0f);
            float dy = Mathf.Max(radius - pixelY, pixelY - (size - radius), 0f);
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            return radius - distance + .5f;
        }
    }
}
