using UnityEngine;

namespace KMA.Gameplay
{
    /// <summary>Reads race state for presentation; never advances or changes the race rules.</summary>
    public sealed class RunnerVisualPresenter : MonoBehaviour
    {
        [SerializeField] SprintController controller;
        [SerializeField] Animator animator;
        string lastState;
        bool sawWindExpiry;
        float stumbleUntil;

        void Awake()
        {
            if (controller == null) controller = FindFirstObjectByType<SprintController>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        void Update()
        {
            if (controller == null || animator == null) return;
            if (controller.WindChallengeExpired && !sawWindExpiry)
                stumbleUntil = Time.time + .45f;
            sawWindExpiry = controller.WindChallengeExpired;
            string state = controller.Phase == MinigamePhase.Resolve
                ? (controller.LastResult != null && controller.LastResult.Pass ? "Celebrate" : "Fail")
                : controller.Phase != MinigamePhase.Play ? "Idle"
                : controller.WindChallengeFailed || Time.time < stumbleUntil ? "Stumble"
                : controller.Snapshot.Distance >= 70f ? "Burst" : "Run";
            if (state == lastState) return;
            animator.Play(state, 0, 0f);
            lastState = state;
        }
    }
}
