using System;
using UnityEngine;

namespace KMA.UI.Kit
{
    /// Anti-aliased white shapes as pixel arrays, row by row from the bottom. Callers tint
    /// through Image.color. Pure: used by the editor baker and by tests.
    public static class UiShapeRaster
    {
        public static int RoundedRectSize(int radius) => radius > 0 ? radius * 2 + 2 : 4;

        public static Color32[] RoundedRect(int radius)
        {
            if (radius < 0)
                throw new ArgumentOutOfRangeException(nameof(radius), "Corner radius cannot be negative.");

            int size = RoundedRectSize(radius);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = White(RoundedRectCoverage(x, y, size, radius));
            return pixels;
        }

        public static Color32[] Circle(int diameter) => Disc(diameter, 0f);

        public static Color32[] Ring(int diameter, float bandWidth)
        {
            if (bandWidth <= 0f || bandWidth * 2f >= diameter)
                throw new ArgumentOutOfRangeException(nameof(bandWidth), "The band must be thinner than the radius.");
            return Disc(diameter, bandWidth);
        }

        static Color32[] Disc(int diameter, float bandWidth)
        {
            if (diameter < 2)
                throw new ArgumentOutOfRangeException(nameof(diameter), "A disc needs at least 2 pixels.");

            float radius = diameter * .5f;
            var pixels = new Color32[diameter * diameter];
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x + .5f - radius;
                    float dy = y + .5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float coverage = Mathf.Clamp01(radius - distance + .5f);
                    if (bandWidth > 0f)
                        coverage *= 1f - Mathf.Clamp01(radius - bandWidth - distance + .5f);
                    pixels[y * diameter + x] = White(coverage);
                }
            }
            return pixels;
        }

        static Color32 White(float coverage) =>
            new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(coverage) * 255f));

        /// Coverage of one pixel by the rounded rectangle, antialiased over one pixel.
        static float RoundedRectCoverage(int x, int y, int size, int radius)
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
