using UnityEngine;

namespace KMA.Gameplay
{
    public enum RivalRunnerState
    {
        Idle,
        Run,
        Burst,
        Stumble,
        Celebrate,
        Fail
    }

    public sealed class RivalRunnerAI : MonoBehaviour
    {
        [SerializeField] SprintController controller;
        [SerializeField] RivalPaceProfileAsset profileAsset;
        [SerializeField] int lane = 1;
        [SerializeField] int rivalIndex;
        [SerializeField] Transform visual;
        [SerializeField] float trackStartX = -9.6f;
        [SerializeField] float trackLength = 19.2f;

        RivalPaceProfile profile;

        public RivalPaceProfileAsset ProfileAsset => profileAsset;
        public RivalPaceProfile Profile => profile;
        public int Lane => lane;
        public int RivalIndex => rivalIndex;
        public RivalRunnerState State { get; private set; }
        public float VisualProgress01 { get; private set; }

        void Awake()
        {
            if (visual == null) visual = transform;
            if (profileAsset != null) profile = profileAsset.ToRuntime();
            if (controller == null) controller = Object.FindFirstObjectByType<SprintController>();
        }

        void Update()
        {
            if (controller == null || rivalIndex < 0)
                return;

            var distances = controller.RivalDistances;
            if (rivalIndex >= distances.Length)
                return;
            Refresh(distances[rivalIndex], controller.Snapshot.Distance, controller.Phase, controller.LastResult,
                controller.WindChallengeFailed || controller.WindChallengeExpired);
        }

        public void Configure(RivalPaceProfileAsset value, int valueLane, int valueRivalIndex, SprintController owner)
        {
            profileAsset = value;
            profile = value == null ? null : value.ToRuntime();
            lane = valueLane;
            rivalIndex = valueRivalIndex;
            controller = owner;
            if (visual == null) visual = transform;
        }

        public void RefreshForTest(float rivalDistance, float playerDistance, MinigamePhase phase, MinigameResult result) =>
            Refresh(rivalDistance, playerDistance, phase, result, false);

        void Refresh(float rivalDistance, float playerDistance, MinigamePhase phase, MinigameResult result, bool challengeFailed)
        {
            VisualProgress01 = Mathf.Clamp01(rivalDistance / 100f);
            if (visual != null)
            {
                var position = visual.localPosition;
                position.x = trackStartX + trackLength * VisualProgress01;
                visual.localPosition = position;
            }

            if (phase == MinigamePhase.Resolve)
                State = result != null && result.Pass ? RivalRunnerState.Celebrate : RivalRunnerState.Fail;
            else if (challengeFailed)
                State = RivalRunnerState.Stumble;
            else if (phase != MinigamePhase.Play)
                State = RivalRunnerState.Idle;
            else if (playerDistance >= 70f)
                State = RivalRunnerState.Burst;
            else
                State = RivalRunnerState.Run;
        }
    }
}
