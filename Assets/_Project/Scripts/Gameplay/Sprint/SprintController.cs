using System;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using KMA.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KMA.Gameplay
{
    public sealed class SprintController : MinigameBase, IChallengeController
    {
        public override bool UsesSharedTutorial => false;
        public override bool UsesSharedCountdown => false;
        public override bool OwnsStartGate => true;

        [SerializeField] RivalPaceProfileAsset[] rivalProfiles;
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] bool directInputEnabled = true;
        [SerializeField] GameplayInputRouter inputRouter;
        [SerializeField] string leftInputAction = "SprintLeft";
        [SerializeField] string rightInputAction = "SprintRight";

        SprintRules rules;
        SprintChallengeRules challengeRules;
        ChallengeDefinition challengeDefinition;
        ChallengeAttemptContext challengeContext;
        InputAction leftAction;
        InputAction rightAction;
        bool terminalResolved;
        bool inputRouterSubscribed;
        int cadenceCombo;
        float stepDistance;

        public bool InputActionsReady => directInputEnabled && leftAction != null && rightAction != null;
        public Side ExpectedSide => rules == null ? Side.Left : rules.ExpectedSide;
        public SprintSnapshot Snapshot => rules == null ? default : rules.Snapshot;
        public MinigameResult LastResult { get; private set; }
        public MinigamePhase Phase => Lifecycle == null ? MinigamePhase.Tutorial : Lifecycle.Phase;
        public string LeftInputAction => leftInputAction;
        public string RightInputAction => rightInputAction;
        public int Rank => rules == null ? 1 : rules.Rank;
        public string RankText => Rank == 1 ? "1st" : Rank == 2 ? "2nd" : Rank == 3 ? "3rd" : "4th";
        public int CadenceCombo => cadenceCombo;
        public int CorrectStreak => rules == null ? 0 : rules.CorrectStreak;
        public float[] RivalDistances => rules == null ? System.Array.Empty<float>() : rules.RivalDistances;
        public SubjectId Subject => SubjectId.Sprint;
        public float TargetDistance => challengeDefinition != null && challengeDefinition.Distance > 0f
            ? challengeDefinition.Distance : 100f;
        public float TargetTime => challengeDefinition != null && challengeDefinition.TimeLimit > 0f
            ? challengeDefinition.TimeLimit : 14f;
        public int TargetCount => challengeDefinition == null ? 0 : challengeDefinition.TargetCount;
        public bool IsLearnChallenge => challengeDefinition != null && challengeDefinition.Kind == ChallengeKind.Learn;
        public event Action<ChallengeAttemptResult> ChallengeCompleted;
        public int RivalCount => rules == null ? 0 : rules.RivalCount;
        public float GetRivalDistance(int index) => rules == null ? 0f : rules.GetRivalDistance(index);

        protected override void Awake()
        {
            base.Awake();
            rules = CreateRulesFromAuthoredProfiles();
            ConfigureInputActions();
        }

        public void ConfigureChallenge(ChallengeDefinition definition, ChallengeAttemptContext context)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (definition.Subject != SubjectId.Sprint || definition.Id != context.ChallengeId)
                throw new ArgumentException("Sprint controller received a mismatched challenge context.");
            if (Lifecycle != null && Lifecycle.Phase != MinigamePhase.Tutorial)
                throw new InvalidOperationException("Sprint challenge must be configured before the start gate opens.");

            challengeDefinition = definition;
            challengeContext = context;
            challengeRules = new SprintChallengeRules(definition, SprintBalanceConfig.LoadDefault().ToRuntime(),
                CreateRuntimeProfiles());
            rules = challengeRules.Race;
            terminalResolved = false;
            cadenceCombo = 0;
            stepDistance = 0f;
            LastResult = null;
            SprintFestivalPresentation.ConfigureChallenge(definition);
            SprintStartPresentation presentation = FindFirstObjectByType<SprintStartPresentation>(
                FindObjectsInactive.Include);
            presentation?.ConfigureInstruction(definition.Kind == ChallengeKind.Learn
                ? $"BẤM TRÁI, PHẢI LUÂN PHIÊN {definition.TargetCount} LẦN"
                : $"CHẠY {definition.Distance:0} M TRONG {definition.TimeLimit:0} GIÂY");
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

            inputRouter.OnSprintTap += OnRouterSprintTap;
            inputRouterSubscribed = true;
        }

        void UnsubscribeInputRouter()
        {
            if (!inputRouterSubscribed)
                return;

            inputRouter.OnSprintTap -= OnRouterSprintTap;
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

        public void ConfigureForTest()
        {
            Lifecycle = new MinigameLifecycle(0f, 0f);
            Lifecycle.Tick(0f);
            Lifecycle.Tick(0f);
            rules = SprintRules.ForTest(0f, 0f, 1);
            terminalResolved = false;
            cadenceCombo = 0;
            LastResult = null;
            challengeRules = null;
            challengeDefinition = null;
            challengeContext = null;
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
            if (challengeRules != null && challengeContext != null)
            {
                ChallengeAttemptResult result = challengeRules.BuildResult(challengeContext);
                LastResult = result.ExamResult ?? new MinigameResult(result.Pass,
                    result.Pass ? result.Metrics.CompletedTargets : 0f,
                    result.Pass ? KMA.Gameplay.Rank.C : KMA.Gameplay.Rank.F);
            }
            else
                LastResult = rules.BuildResult();
            return LastResult;
        }

        protected override bool TryBeginResolve(bool passed) => base.TryBeginResolve(passed);

        protected override void OnResultResolved(MinigameResult result)
        {
            LastResult = result;
            if (challengeRules == null || challengeContext == null)
            {
                base.OnResultResolved(result);
                return;
            }
            ChallengeCompleted?.Invoke(challengeRules.BuildResult(challengeContext));
        }

        protected override MinigameHudState BuildHudState() => new MinigameHudState(
            phase: Phase.ToString(),
            timeRemaining: Mathf.Max(0f, rules == null ? 0f : TargetTime - rules.Elapsed),
            progress01: challengeDefinition != null && challengeDefinition.Kind == ChallengeKind.Learn
                ? Mathf.Clamp01((rules == null ? 0f : (float)rules.CorrectStreak) / Mathf.Max(1, TargetCount))
                : Mathf.Clamp01((rules == null ? 0f : rules.Snapshot.Distance) / TargetDistance),
            stamina01: Mathf.Clamp01((rules == null ? 0f : rules.Stamina) / 100f),
            score: rules == null ? 0f : rules.BuildResult().Score,
            statusText: challengeDefinition == null ? "TAP LEFT / RIGHT"
                : challengeDefinition.Kind == ChallengeKind.Learn
                    ? $"CHUỖI {rules.CorrectStreak}/{TargetCount}" : $"{TargetDistance:0} M · {TargetTime:0} S");
        protected override void TickPlay(float dt)
        {
            float before = rules.Snapshot.Distance;
            rules.Tick(dt);
            stepDistance += Mathf.Max(0f, rules.Snapshot.Distance - before);
            if (dt > 0f && stepDistance >= 1.6f)
            {
                stepDistance %= 1.6f;
                GameAudio.Play(GameSound.RunStep);
            }
            EvaluateTerminalOutcome();
        }

        void OnTap(Side side)
        {
            if (Lifecycle.Phase != MinigamePhase.Play)
                return;

            Side expected = rules.ExpectedSide;
            rules.Tap(side);
            cadenceCombo = side == expected ? cadenceCombo + 1 : 0;
            EvaluateTerminalOutcome();
        }

        SprintRules CreateRulesFromAuthoredProfiles()
        {
            RivalPaceProfile[] runtimeProfiles = CreateRuntimeProfiles();
            if (runtimeProfiles.Length == 0) return SprintRules.Default();
            return new SprintRules(14f, runtimeProfiles);
        }

        RivalPaceProfile[] CreateRuntimeProfiles()
        {
            if (rivalProfiles == null || rivalProfiles.Length == 0) return Array.Empty<RivalPaceProfile>();
            var runtimeProfiles = new RivalPaceProfile[rivalProfiles.Length];
            for (var i = 0; i < rivalProfiles.Length; i++)
                runtimeProfiles[i] = rivalProfiles[i] == null ? null : rivalProfiles[i].ToRuntime();
            return runtimeProfiles;
        }

        void EvaluateTerminalOutcome()
        {
            if (terminalResolved || Lifecycle.Phase != MinigamePhase.Play)
                return;

            bool learn = IsLearnChallenge;
            bool finished = learn ? challengeRules.IsComplete : rules.Snapshot.Distance >= TargetDistance;
            bool timedOut = !learn && rules.Snapshot.Elapsed >= TargetTime;
            if (!finished && !timedOut)
                return;

            terminalResolved = true;
            Finish(BuildResult());
        }
    }
}
