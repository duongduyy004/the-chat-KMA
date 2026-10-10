using UnityEngine;

namespace KMA.Gameplay
{
    /// <summary>
    /// Heaving chest for a runner who has stopped or slowed to catch their breath. Drives only the
    /// target's scale (the animator only swaps sprites), so it layers on top of any animation state.
    /// </summary>
    public sealed class RunnerBreathing : MonoBehaviour
    {
        const float BreathsPerSecond = .9f;
        const float TallerBy = .045f;
        const float NarrowerBy = .02f;
        const float BlendPerSecond = 4f;

        [SerializeField] Transform target;

        Vector3 baseScale = Vector3.one;
        bool baseCaptured;
        float weight;
        float clock;

        public bool IsResting { get; private set; }
        public float Weight => weight;

        public void Bind(Transform value)
        {
            target = value;
            baseCaptured = false;
        }

        public void SetResting(bool value) => IsResting = value;

        void LateUpdate() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            if (target == null) target = transform;
            if (!baseCaptured)
            {
                baseScale = target.localScale;
                baseCaptured = true;
            }

            weight = Mathf.MoveTowards(weight, IsResting ? 1f : 0f, BlendPerSecond * Mathf.Max(0f, dt));
            if (IsResting) clock += dt;
            // (1 - cos) keeps the chest at its normal size at the start and end of every breath.
            float breath = (1f - Mathf.Cos(clock * BreathsPerSecond * 2f * Mathf.PI)) * .5f * weight;
            target.localScale = new Vector3(
                baseScale.x * (1f - NarrowerBy * breath),
                baseScale.y * (1f + TallerBy * breath),
                baseScale.z);
        }

        /// <summary>Finds or adds the breathing component on a runner's sprite child.</summary>
        public static RunnerBreathing Attach(Transform runner)
        {
            if (runner == null) return null;
            Transform sprite = runner.Find("Visual") ?? runner;
            var breathing = sprite.GetComponent<RunnerBreathing>() ?? sprite.gameObject.AddComponent<RunnerBreathing>();
            breathing.Bind(sprite);
            return breathing;
        }
    }
}
