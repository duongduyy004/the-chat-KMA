using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class BallShadow : MonoBehaviour
    {
        const string ConfigurationError =
            "BallShadow requires target, shadow Transform, SpriteRenderer, maxHeight > 0, and ordered scale/alpha bounds.";

        [SerializeField] Transform target;
        [SerializeField] Transform shadow;
        [SerializeField] SpriteRenderer shadowRenderer;
        [SerializeField] float groundY;
        [SerializeField, Min(.001f)] float maxHeight = 4f;
        [SerializeField, Min(0f)] float minScale = .35f;
        [SerializeField, Min(0f)] float maxScale = 1f;
        [SerializeField, Range(0f, 1f)] float minAlpha = .2f;
        [SerializeField, Range(0f, 1f)] float maxAlpha = .75f;

        bool configurationErrorLogged;

        public Transform Target => target;
        public Transform Shadow => shadow;
        public SpriteRenderer Renderer => shadowRenderer;

        void Awake()
        {
            ValidateConfiguration();
        }

        void LateUpdate()
        {
            Refresh();
        }

        public void Configure(
            Transform targetTransform,
            Transform shadowTransform,
            SpriteRenderer renderer,
            float ground,
            float maximumHeight,
            float minimumScale,
            float maximumScale,
            float minimumAlpha,
            float maximumAlpha)
        {
            target = targetTransform;
            shadow = shadowTransform;
            shadowRenderer = renderer;
            groundY = ground;
            maxHeight = maximumHeight;
            minScale = minimumScale;
            maxScale = maximumScale;
            minAlpha = minimumAlpha;
            maxAlpha = maximumAlpha;
            ValidateConfiguration();
        }

        public void Refresh()
        {
            if (!ValidateConfiguration())
                return;

            float height = Mathf.Max(0f, target.position.y - groundY);
            float height01 = Mathf.Clamp01(height / maxHeight);
            float scale = Mathf.Lerp(maxScale, minScale, height01);
            float alpha = Mathf.Lerp(maxAlpha, minAlpha, height01);
            Vector3 shadowPosition = shadow.position;
            shadow.position = new Vector3(target.position.x, groundY, shadowPosition.z);
            Vector3 shadowScale = shadow.localScale;
            shadow.localScale = new Vector3(scale, scale, shadowScale.z);
            Color color = shadowRenderer.color;
            color.a = alpha;
            shadowRenderer.color = color;
        }

        bool ValidateConfiguration()
        {
            bool valid = target
                && shadow
                && shadowRenderer
                && maxHeight > 0f
                && minScale >= 0f
                && maxScale >= minScale
                && minAlpha >= 0f
                && maxAlpha <= 1f
                && maxAlpha >= minAlpha;
            if (valid)
            {
                enabled = true;
                return true;
            }

            enabled = false;
            if (!configurationErrorLogged)
            {
                configurationErrorLogged = true;
                Debug.LogError(ConfigurationError, this);
            }

            return false;
        }
    }
}
