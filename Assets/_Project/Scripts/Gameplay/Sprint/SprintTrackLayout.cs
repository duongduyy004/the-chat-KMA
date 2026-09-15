using UnityEngine;

namespace KMA.Gameplay
{
    /// <summary>
    /// Maps the lanes painted into <c>Track.png</c> onto world space.
    ///
    /// The artwork carries five painted lane lines; the four bands between them are the lanes a
    /// runner is meant to be standing in. Runner Y is derived from here rather than hand-tuned, so
    /// a runner cannot drift off the lane they are drawn to be standing in.
    ///
    /// The backdrop keeps its authored size — its bottom edge rests on the viewport floor, so the
    /// track fills the screen. The lowest lane therefore runs underneath the TRÁI/PHẢI buttons;
    /// those draw a deliberately small visual inside a full-size tap area so they sit on the lane
    /// without hiding the runner in it. See <see cref="SprintUiLayout.ControlVisualRect01"/>.
    /// </summary>
    public static class SprintTrackLayout
    {
        /// Row of the centre of each painted lane line, top-down, in Track.png's own pixels.
        /// Measured from the cream core of each line; identical at x = 10%, 50% and 90%.
        public static readonly float[] LaneLineRows = { 471.5f, 553.5f, 639f, 722f, 798f };

        public const int TextureHeight = 875;
        public const float TexturePixelsPerUnit = 100f;
        public const int LaneCount = 4;

        /// The backdrop as authored in MG_Sprint: its bottom edge sits on the viewport floor at
        /// ortho size 5.4, and it stands this tall in world units.
        public const float BackdropBottomY = -5.4f;
        public const float BackdropHeight = 15.4477f;

        public static float UnitsPerRow => BackdropHeight / TextureHeight;

        public static float WorldYForRow(float row) =>
            BackdropBottomY + (TextureHeight - row) * UnitsPerRow;

        /// Row midway between the two painted lines that bound this lane.
        public static float LaneCenterRow(int lane)
        {
            int clamped = Mathf.Clamp(lane, 0, LaneCount - 1);
            return (LaneLineRows[clamped] + LaneLineRows[clamped + 1]) * .5f;
        }

        /// Where a runner in this lane plants their feet. Lane 0 is the topmost lane.
        public static float LaneCenterY(int lane) => WorldYForRow(LaneCenterRow(lane));

        /// Lane as authored on the runners, which numbers them 1-4 from the top.
        public static float LaneCenterYForAuthoredLane(int authoredLane) => LaneCenterY(authoredLane - 1);

        public static float BackdropScaleY =>
            BackdropHeight / (TextureHeight / TexturePixelsPerUnit);

        public static float BackdropCenterY => BackdropBottomY + BackdropHeight * .5f;
    }
}
