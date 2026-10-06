using System;
using UnityEngine;

namespace KMA.Gameplay
{
    /// Rect math for the shared campus backdrop, in any unit (world units or canvas pixels), y up.
    public static class CampusBackdropLayout
    {
        /// Number of skyline tiles a backdrop currently shows: Refit activates Skyline0..n-1 and hides the rest.
        public static int ActiveSkylineTiles(Transform root)
        {
            int count = 0;
            for (Transform tile; (tile = root.Find("Skyline" + count)) != null && tile.gameObject.activeSelf;)
                count++;
            return count;
        }

        /// The smallest rect with the sprite's aspect that covers the view, centred on it.
        public static Rect Cover(Rect view, float aspect)
        {
            RequirePositive(aspect);
            float width = Mathf.Max(view.width, view.height * aspect);
            float height = width / aspect;
            return new Rect(view.center.x - width * .5f, view.center.y - height * .5f, width, height);
        }

        /// Tiles of a horizontal band whose bottom edge sits at baseY. Every tile keeps the sprite's
        /// aspect at the given height; an odd number of touching tiles, centred on the view, covers its width.
        public static Rect[] Band(Rect view, float baseY, float height, float aspect)
        {
            RequirePositive(aspect);
            if (height <= 0f)
                return Array.Empty<Rect>();
            float tileWidth = height * aspect;
            int half = Mathf.Max(0, Mathf.CeilToInt((view.width - tileWidth) * .5f / tileWidth - 1e-4f));
            var tiles = new Rect[half * 2 + 1];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = new Rect(view.center.x + (i - half - .5f) * tileWidth, baseY, tileWidth, height);
            return tiles;
        }

        static void RequirePositive(float aspect)
        {
            if (!(aspect > 0f))
                throw new ArgumentOutOfRangeException(nameof(aspect), aspect, "Sprite aspect must be positive.");
        }
    }
}
