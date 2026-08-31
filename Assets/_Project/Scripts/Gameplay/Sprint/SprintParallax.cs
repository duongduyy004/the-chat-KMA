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

            public void CacheOrigins()
            {
                if (first != null) firstOrigin = first.localPosition;
                if (second != null) secondOrigin = second.localPosition;
            }

            public void Scroll(float distance)
            {
                if (first == null || second == null) return;
                var offset = Mathf.Repeat(distance * scrollMultiplier, loopWidth);
                first.localPosition = firstOrigin + Vector3.left * offset;
                second.localPosition = secondOrigin + Vector3.left * offset;
            }
        }

        [SerializeField] SprintController controller;
        [SerializeField] Vector2 coveragePixels = new Vector2(2560f, 1080f);
        [SerializeField] Layer[] layers = Array.Empty<Layer>();

        public int LayerCount => layers == null ? 0 : layers.Length;
        public Vector2 CoveragePixels => coveragePixels;

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
