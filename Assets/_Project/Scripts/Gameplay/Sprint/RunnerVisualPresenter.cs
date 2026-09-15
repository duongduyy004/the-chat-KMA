using UnityEngine;

namespace KMA.Gameplay
{
    /// <summary>Reads race state for presentation; never advances or changes the race rules.</summary>
    public sealed class RunnerVisualPresenter : MonoBehaviour
    {
        [SerializeField] SprintController controller;
        [SerializeField] Animator animator;
        [SerializeField] Transform raceRoot;
        [SerializeField] float trackStartX = -9.6f;
        [SerializeField] float trackLength = 19.2f;
        string lastState;

        void Awake()
        {
            if (controller == null) controller = FindFirstObjectByType<SprintController>();
            if (animator == null) animator = GetComponent<Animator>();
            if (raceRoot == null) raceRoot = transform.parent == null ? transform : transform.parent;
        }

        void Update()
        {
            if (controller == null || animator == null) return;
            RefreshPosition(controller.Snapshot.Distance);
            string state = controller.Phase == MinigamePhase.Resolve
                ? (controller.LastResult != null && controller.LastResult.Pass ? "Celebrate" : "Fail")
                : controller.Phase != MinigamePhase.Play ? "Idle"
                : controller.Snapshot.Distance >= 70f ? "Burst" : "Run";
            if (state == lastState) return;
            animator.Play(state, 0, 0f);
            lastState = state;
        }

        void RefreshPosition(float distance)
        {
            if (raceRoot == null) return;
            var position = raceRoot.localPosition;
            position.x = trackStartX + trackLength * Mathf.Clamp01(distance / 100f);
            raceRoot.localPosition = position;
        }
    }
}
