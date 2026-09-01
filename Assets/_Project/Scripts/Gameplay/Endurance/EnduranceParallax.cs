using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class EnduranceParallax : MonoBehaviour
    {
        [SerializeField] EnduranceController controller;
        [SerializeField] Transform[] layers = new Transform[3];
        [SerializeField] float[] speeds = { .25f, .5f, .9f };
        [SerializeField] float loopWidth = 25.6f;

        Vector3[] origins;

        public int LayerCount => layers == null ? 0 : layers.Length;
        public int BoundLayerCount
        {
            get
            {
                var count = 0;
                if (layers == null) return count;
                for (var index = 0; index < layers.Length; index++)
                    if (layers[index] != null) count++;
                return count;
            }
        }

        void Awake()
        {
            controller ??= Object.FindFirstObjectByType<EnduranceController>();
            CacheOrigins();
        }

        void Update()
        {
            if (Time.timeScale > 0f)
                RefreshForTest(Time.unscaledDeltaTime);
        }

        public void RefreshForTest(float deltaSeconds)
        {
            if (layers == null || origins == null)
                return;
            for (var index = 0; index < layers.Length; index++)
            {
                var layer = layers[index];
                if (layer == null) continue;
                var speed = index < speeds.Length ? speeds[index] : 0f;
                var position = layer.localPosition;
                position.x -= speed * deltaSeconds;
                if (position.x <= origins[index].x - loopWidth)
                    position.x += loopWidth;
                layer.localPosition = position;
            }
        }

        void CacheOrigins()
        {
            if (layers == null)
                return;
            origins = new Vector3[layers.Length];
            for (var index = 0; index < layers.Length; index++)
                if (layers[index] != null) origins[index] = layers[index].localPosition;
        }
    }
}
