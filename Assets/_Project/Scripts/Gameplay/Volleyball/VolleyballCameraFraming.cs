using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    /// <summary>Camera framing that leaves room for the campus skyline above the far sideline.</summary>
    public static class VolleyballCameraFraming
    {
        public const float Size = 6.2f, Y = 1f;

        public static void Apply(Camera camera)
        {
            if (camera == null) return;
            camera.orthographicSize = Size;
            camera.transform.position = new Vector3(0f, Y, -10f);
        }
    }
}
