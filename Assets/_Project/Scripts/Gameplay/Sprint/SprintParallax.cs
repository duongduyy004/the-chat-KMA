using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class SprintParallax : MonoBehaviour
    {
        [Serializable]
        public sealed class Layer
        {
            [SerializeField] Transform first;
            [SerializeField] Transform second;
            [SerializeField, Min(0f)] float scrollMultiplier = 1f;
            [SerializeField, Min(.01f)] float loopWidth = 25.6f;

            Vector3 firstOrigin;
            Vector3 secondOrigin;
            float lastDistance;
            bool hasCachedDistance;

            public void CacheOrigins()
            {
                if (first != null) firstOrigin = first.localPosition;
                if (second != null) secondOrigin = second.localPosition;
                lastDistance = 0f;
                hasCachedDistance = true;
            }

            public void Scroll(float distance)
            {
                if (first == null || second == null) return;
                if (!hasCachedDistance)
                    CacheOrigins();

                var delta = (distance - lastDistance) * scrollMultiplier;
                first.localPosition += Vector3.left * delta;
                second.localPosition += Vector3.left * delta;
                RecycleLeftTile();
                lastDistance = distance;
            }

            public bool IsBound => first != null && second != null;

            public int RendererCount
            {
                get
                {
                    var count = 0;
                    if (first != null && first.GetComponent<Renderer>() != null) count++;
                    if (second != null && second.GetComponent<Renderer>() != null) count++;
                    return count;
                }
            }

            public bool TryGetTilePositions(out Vector3 firstPosition, out Vector3 secondPosition)
            {
                firstPosition = first == null ? default : first.localPosition;
                secondPosition = second == null ? default : second.localPosition;
                return IsBound;
            }

            void RecycleLeftTile()
            {
                if (first.localPosition.x <= second.localPosition.x - loopWidth)
                    first.localPosition = new Vector3(second.localPosition.x + loopWidth, first.localPosition.y, first.localPosition.z);
                else if (second.localPosition.x <= first.localPosition.x - loopWidth)
                    second.localPosition = new Vector3(first.localPosition.x + loopWidth, second.localPosition.y, second.localPosition.z);
            }
        }

        [SerializeField] SprintController controller;
        [SerializeField] Vector2 coveragePixels = new Vector2(2560f, 1080f);
        [SerializeField] Layer[] layers = Array.Empty<Layer>();


        public int LayerCount => layers == null ? 0 : layers.Length;
        public int BoundLayerCount { get { var count = 0; if (layers == null) return 0; for (var i = 0; i < layers.Length; i++) if (layers[i] != null && layers[i].IsBound) count++; return count; } }
        public Vector2 CoveragePixels => coveragePixels;
        public int GetLayerTileRendererCount(int index) => layers == null || index < 0 || index >= layers.Length || layers[index] == null ? 0 : layers[index].RendererCount;
        public bool TryGetLayerTilePositions(int index, out Vector3 first, out Vector3 second)
        {
            if (layers == null || index < 0 || index >= layers.Length || layers[index] == null)
            {
                first = default;
                second = default;
                return false;
            }
            return layers[index].TryGetTilePositions(out first, out second);
        }

        void Awake()
        {
            if (controller == null) controller = Object.FindFirstObjectByType<SprintController>();
            if (layers == null) return;
            for (var i = 0; i < layers.Length; i++) layers[i]?.CacheOrigins();
        }

        void Update()
        {
            if (controller != null) RefreshForTest(controller.Snapshot.Distance);
        }

        public void RefreshForTest(float playerDistance)
        {
            if (layers == null) return;
            for (var i = 0; i < layers.Length; i++) layers[i]?.Scroll(playerDistance);
        }
    }
}
