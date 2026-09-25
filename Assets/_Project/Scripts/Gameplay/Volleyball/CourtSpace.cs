using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public enum CourtSide
    {
        Player,
        Opponent
    }

    public static class CourtSides
    {
        public static CourtSide Other(this CourtSide side) =>
            side == CourtSide.Player ? CourtSide.Opponent : CourtSide.Player;
    }

    // Court metres: x runs along the court (net at 0, player half negative), y across it (+y is
    // the far sideline, drawn higher on screen). Pixel constants come from beachbkgO.png, whose
    // outer lines sit at x 17..383 and y 145..413 with the net line at x 200.
    public static class CourtSpace
    {
        public const float HalfLength = 8f;
        public const float HalfWidth = 4f;
        public const float NetHeight = 2.24f;
        public const float BackgroundPixelsPerUnit = 30f;
        public const float PixelsPerMetreX = 22.875f;
        public const float PixelsPerMetreY = 33.5f;
        public const float HeightLift = .8f;

        static readonly Vector2 CourtCentrePixel = new Vector2(200f, 279f);
        static readonly Vector2 BackgroundCentrePixel = new Vector2(200f, 215f);

        public static Vector3 BackgroundWorldPosition => new Vector3(0f,
            (CourtCentrePixel.y - BackgroundCentrePixel.y) / BackgroundPixelsPerUnit, 0f);

        public static bool IsIn(Vector2 ground) =>
            Mathf.Abs(ground.x) <= HalfLength && Mathf.Abs(ground.y) <= HalfWidth;

        public static CourtSide SideOf(Vector2 ground) => ground.x < 0f ? CourtSide.Player : CourtSide.Opponent;

        public static float SideSign(CourtSide side) => side == CourtSide.Player ? -1f : 1f;

        public static Vector2 ToBackgroundPixel(Vector2 ground) => new Vector2(
            CourtCentrePixel.x + ground.x * PixelsPerMetreX,
            CourtCentrePixel.y - ground.y * PixelsPerMetreY);

        public static Vector3 ToWorld(Vector2 ground, float height) => new Vector3(
            ground.x * PixelsPerMetreX / BackgroundPixelsPerUnit,
            (ground.y + height * HeightLift) * PixelsPerMetreY / BackgroundPixelsPerUnit,
            0f);
    }
}
