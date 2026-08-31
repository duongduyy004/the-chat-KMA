using KMA.Gameplay.UI;
using KMA.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KMA.Gameplay
{
    public sealed class SprintController : MinigameBase
    {
        [SerializeField] SprintChallengePattern challengePattern = new SprintChallengePattern();
        [SerializeField] RivalPaceProfileAsset[] rivalProfiles;
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] bool directInputEnabled = true;
        [SerializeField] GameplayInputRouter inputRouter;
        [SerializeField] string leftInputAction = "SprintLeft";
        [SerializeField] string rightInputAction = "SprintRight";

        SprintRules rules;
        InputAction leftAction;
        InputAction rightAction;
        float cueAt;
        float activeAt;
        float challengeElapsed;
        bool windChallengeResolved;
        bool terminalResolved;
        bool inputRouterSubscribed;

        public bool WindCueVisible { get; private set; }
        public bool WindWindowActive { get; private set; }
        public bool WindChallengeCountered { get; private set; }
        public bool WindChallengeFailed { get; private set; }
        public bool WindChallengeExpired { get; private set; }
        public bool InputActionsReady => directInputEnabled && leftAction != null && rightAction != null;
        public Side ExpectedSide => rules == null ? Side.Left : rules.ExpectedSide;
        public SprintSnapshot Snapshot => rules == null ? default : rules.Snapshot;
        public MinigameResult LastResult { get; private set; }
        public MinigamePhase Phase => Lifecycle == null ? MinigamePhase.Tutorial : Lifecycle.Phase;
        public string LeftInputAction => leftInputAction;
        public string RightInputAction => rightInputAction;
        public int Rank => rules == null ? 1 : rules.Rank;
        public string RankText => Rank == 1 ? "1st" : Rank == 2 ? "2nd" : Rank == 3 ? "3rd" : "4th";
        public int CadenceCombo => rules == null ? 0 : Mathf.RoundToInt(rules.ValidTapRatio * rules.Snapshot.Elapsed);
        public float[] RivalDistances => rules == null ? System.Array.Empty<float>() : rules.RivalDistances;

        protected override void Awake()
        {
            base.Awake();
            rules = CreateRulesFromAuthoredProfiles();
            ConfigureChallengePattern();
            ConfigureInputActions();
        }

        void OnEnable()
        {
            ConfigureInputActions();
            SubscribeInputRouter();
        }

        void OnDisable()
        {
            UnsubscribeInputActions();
            UnsubscribeInputRouter();
        }

        void OnDestroy()
        {
            UnsubscribeInputActions();
            UnsubscribeInputRouter();
        }

        void ConfigureInputActions()
        {
            UnsubscribeInputActions();
            if (!directInputEnabled || inputActions == null)
                return;

            leftAction = inputActions.FindAction(leftInputAction, false);
            rightAction = inputActions.FindAction(rightInputAction, false);
            if (leftAction == null || rightAction == null)
                return;

            leftAction.performed += OnLeftActionPerformed;
            rightAction.performed += OnRightActionPerformed;
            inputActions.Enable();
        }

        void UnsubscribeInputActions()
        {
            if (leftAction != null)
                leftAction.performed -= OnLeftActionPerformed;
            if (rightAction != null)
                rightAction.performed -= OnRightActionPerformed;
            leftAction = null;
            rightAction = null;
        }

        void OnLeftActionPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && Lifecycle.Phase == MinigamePhase.Play)
                OnLeftTap();
        }

        void OnRightActionPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && Lifecycle.Phase == MinigamePhase.Play)
                OnRightTap();
        }

        public void ConfigureInputForTest(InputActionAsset actions)
        {
            inputActions = actions;
            ConfigureInputActions();
        }

        public void ConfigureInputRouterForTest(GameplayInputRouter router)
        {
            UnsubscribeInputRouter();
            inputRouter = router;
            SubscribeInputRouter();
        }

        void SubscribeInputRouter()
        {
            if (inputRouter == null || inputRouterSubscribed)
                return;

            inputRouter.OnSprintValidTap += OnRouterSprintTap;
            inputRouterSubscribed = true;
        }

        void UnsubscribeInputRouter()
        {
            if (!inputRouterSubscribed)
                return;

            inputRouter.OnSprintValidTap -= OnRouterSprintTap;
            inputRouterSubscribed = false;
        }

        void OnRouterSprintTap(KMA.Input.Side side)
        {
            if (Lifecycle.Phase != MinigamePhase.Play)
                return;

            if (side == KMA.Input.Side.Left)
                OnLeftTap();
            else
                OnRightTap();
        }

        public void ConfigureForTest(float cueLeadSeconds)
        {
            Lifecycle = new MinigameLifecycle(0f, 0f);
            Lifecycle.Tick(0f);
            Lifecycle.Tick(0f);
            rules = SprintRules.ForTest(0f, 0f, 1);
            challengePattern = SprintChallengePattern.AuthoredDefault();
            challengePattern.ConfigureForTest(cueLeadSeconds);
            ConfigureChallengePattern();
            WindCueVisible = false;
            WindWindowActive = false;
            WindChallengeCountered = false;
            WindChallengeFailed = false;
            WindChallengeExpired = false;
            windChallengeResolved = false;
            terminalResolved = false;
            challengeElapsed = 0f;
            LastResult = null;
        }

        public void AdvanceToDistance(float value)
        {
            rules = SprintRules.ForTest(value, rules == null ? 0f : rules.Elapsed, 1);
        }

        public void Simulate(float dt)
        {
            Lifecycle.Tick(dt);
            if (Lifecycle.Phase == MinigamePhase.Play)
                TickPlay(dt);
        }

        public void OnLeftTap() => OnTap(Side.Left);

        public void OnRightTap() => OnTap(Side.Right);

        public MinigameResult BuildResult()
        {
            var result = rules.BuildResult();
            if (WindChallengeFailed)
                result = new MinigameResult(false, result.Score, result.Rank);
            LastResult = result;
            return result;
        }

        protected override MinigameHudState BuildHudState() => new MinigameHudState(
            phase: Phase.ToString(),
            timeRemaining: Mathf.Max(0f, rules == null ? 0f : 14f - rules.Elapsed),
            progress01: Mathf.Clamp01((rules == null ? 0f : rules.Snapshot.Distance) / 100f),
            stamina01: Mathf.Clamp01((rules == null ? 0f : rules.Stamina) / 100f),
            score: rules == null ? 0f : rules.BuildResult().Score,
            statusText: WindWindowActive ? "WIND — COUNTER NOW" : "TAP LEFT / RIGHT");
        protected override void TickPlay(float dt)
        {
            float distanceBefore = rules.Snapshot.Distance;
            rules.Tick(dt);
            UpdateAuthoredChallenges(dt, distanceBefore);
            EvaluateTerminalOutcome();
        }

        void OnTap(Side side)
        {
            Side expected = rules.ExpectedSide;
            rules.Tap(side);
            if (!WindWindowActive || windChallengeResolved)
                return;

            windChallengeResolved = true;
            if (side == expected)
            {
                WindChallengeCountered = true;
                return;
            }

            WindChallengeFailed = true;
            EvaluateTerminalOutcome();
        }

        void ConfigureChallengePattern()
        {
            cueAt = challengePattern.WindCueDistance;
            activeAt = challengePattern.WindActivationDistance;
        }

        SprintRules CreateRulesFromAuthoredProfiles()
        {
            if (rivalProfiles == null || rivalProfiles.Length == 0)
                return SprintRules.Default();

            var runtimeProfiles = new RivalPaceProfile[rivalProfiles.Length];
            for (var i = 0; i < rivalProfiles.Length; i++)
                runtimeProfiles[i] = rivalProfiles[i] == null ? null : rivalProfiles[i].ToRuntime();
            return new SprintRules(14f, runtimeProfiles);
        }

        void UpdateAuthoredChallenges(float dt, float distanceBefore)
        {
            float distanceAfter = rules.Snapshot.Distance;
            float timerDt = dt;
            if (!WindCueVisible && distanceAfter >= cueAt)
            {
                float distanceDelta = distanceAfter - distanceBefore;
                float fractionBeforeCue = distanceDelta > 0f
                    ? Mathf.Clamp01((cueAt - distanceBefore) / distanceDelta)
                    : 0f;
                WindCueVisible = true;
                challengeElapsed = 0f;
                timerDt = Mathf.Max(0f, dt * (1f - fractionBeforeCue));
            }

            if (WindCueVisible && !windChallengeResolved)
            {
                challengeElapsed += timerDt;
                if (challengeElapsed >= challengePattern.WindCueLeadSeconds + challengePattern.WindWindowDuration)
                {
                    WindWindowActive = false;
                    WindChallengeExpired = true;
                    windChallengeResolved = true;
                }
                else if (!WindWindowActive &&
                    challengeElapsed >= challengePattern.WindCueLeadSeconds &&
                    rules.Snapshot.Distance >= activeAt)
                {
                    WindWindowActive = true;
                }
            }
        }

        void EvaluateTerminalOutcome()
        {
            if (terminalResolved || Lifecycle.Phase != MinigamePhase.Play)
                return;

            bool finished = rules.Snapshot.Distance >= 100f;
            bool timedOut = rules.Snapshot.Elapsed >= 14f;
            if (!finished && !timedOut && !WindChallengeFailed)
                return;

            terminalResolved = true;
            Finish(BuildResult());
        }
    }
}
