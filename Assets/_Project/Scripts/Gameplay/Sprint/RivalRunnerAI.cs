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
        [SerializeField] Animator animator;
        [SerializeField] float trackStartX;
        [SerializeField] float trackLength = 19.2f;

        RivalPaceProfile profile;

        public RivalPaceProfileAsset ProfileAsset => profileAsset;
        public RivalPaceProfile Profile => profile;
        public int Lane => lane;
        public int RivalIndex => rivalIndex;
        public RivalRunnerState State { get; private set; }
        public float VisualProgress01 { get; private set; }
        public Animator Animator => animator;
        public SpriteRenderer Sprite => visual == null ? null : visual.GetComponent<SpriteRenderer>();

        static readonly int RunHash = Animator.StringToHash("Run");
        static readonly int BurstHash = Animator.StringToHash("Burst");
        static readonly int StumbleHash = Animator.StringToHash("Stumble");
        static readonly int CelebrateHash = Animator.StringToHash("Celebrate");
        static readonly int FailHash = Animator.StringToHash("Fail");
        static readonly int IdleHash = Animator.StringToHash("Idle");
        RivalRunnerState lastPlayedState;
        bool hasPlayedState;

        void Awake()
        {
            if (visual == null) visual = transform;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (profileAsset != null) profile = profileAsset.ToRuntime();
            if (controller == null) controller = Object.FindFirstObjectByType<SprintController>();
        }

        void Update()
        {
            if (controller == null || rivalIndex < 0)
                return;

            if (rivalIndex >= controller.RivalCount)
                return;
            Refresh(controller.GetRivalDistance(rivalIndex), controller.Snapshot.Distance, controller.Phase, controller.LastResult,
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
            if (animator == null) animator = GetComponentInChildren<Animator>();
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

            if (animator != null && (!hasPlayedState || lastPlayedState != State))
            {
                animator.Play(StateHash(State), 0, 0f);
                lastPlayedState = State;
                hasPlayedState = true;
            }
        }

        static int StateHash(RivalRunnerState state) => state switch
        {
            RivalRunnerState.Run => RunHash,
            RivalRunnerState.Burst => BurstHash,
            RivalRunnerState.Stumble => StumbleHash,
            RivalRunnerState.Celebrate => CelebrateHash,
            RivalRunnerState.Fail => FailHash,
            _ => IdleHash
        };
    }
}
