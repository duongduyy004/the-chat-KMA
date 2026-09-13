using UnityEngine;

namespace KMA.Gameplay
{
    public static class SprintUiLayout
    {
        public const float FinishRevealDistance = 70f;

        public static float LaneCenter01(int laneIndex, int laneCount)
        {
            if (laneCount <= 0) throw new System.ArgumentOutOfRangeException(nameof(laneCount));
            return (Mathf.Clamp(laneIndex, 0, laneCount - 1) + .5f) / laneCount;
        }

        public static Rect VisibleControlRect(Rect safe, bool left) =>
            BottomCornerRect(safe, left, .20f, .16f, .05f);

        public static Rect HitAreaRect(Rect safe, bool left) =>
            BottomCornerRect(safe, left, .28f, .24f, .01f);

        public static bool FinishVisible(float distance) => distance >= FinishRevealDistance;

        static Rect BottomCornerRect(Rect safe, bool left, float width01, float height01, float inset01)
        {
            float width = safe.width * width01;
            float height = safe.height * height01;
            float x = left ? safe.xMin + safe.width * inset01 : safe.xMax - safe.width * inset01 - width;
            return new Rect(x, safe.yMin + safe.height * .05f, width, height);
        }
    }
}
