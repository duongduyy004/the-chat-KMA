using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    /// <summary>Camera framing that leaves room for the campus skyline above the far sideline.</summary>
    public static class VolleyballCameraFraming
    {
        // Size is the height framing used on phone-wide screens; narrower screens open up just
        // enough to keep HalfWidth (the lecturer beside the left baseline and the far server) in view.
        public const float Size = 6.2f, Y = 1.2f;
        public const float HalfWidth = 13.4f;

        public static float SizeFor(float aspect) =>
            aspect <= 0f ? Size : Mathf.Max(Size, HalfWidth / aspect);

        public static void Apply(Camera camera)
        {
            if (camera == null) return;
            camera.orthographicSize = SizeFor(camera.aspect);
            camera.transform.position = new Vector3(0f, Y, -10f);
        }
    }
}
