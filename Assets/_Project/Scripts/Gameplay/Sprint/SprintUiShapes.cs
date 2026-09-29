using System;
using KMA.UI.Kit;
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

            int size = UiShapeRaster.RoundedRectSize(radius);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"SprintRoundedRect{radius}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixels32(UiShapeRaster.RoundedRect(radius));
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;

            Cache[radius] = sprite;
            return sprite;
        }

        public static void ClearCacheForTest() => Cache.Clear();
    }
}
