using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class SprintController : MinigameBase
    {
        [SerializeField] SprintChallengePattern challengePattern = new SprintChallengePattern();
        [SerializeField] string leftInputAction = "SprintLeft";
        [SerializeField] string rightInputAction = "SprintRight";

        SprintRules rules;
        float cueAt;
        float activeAt;
        float challengeElapsed;
        bool windChallengeResolved;
        bool terminalResolved;

        public bool WindCueVisible { get; private set; }
        public bool WindWindowActive { get; private set; }
        public bool WindChallengeCountered { get; private set; }
        public bool WindChallengeFailed { get; private set; }
        public bool WindChallengeExpired { get; private set; }
        public Side ExpectedSide => rules == null ? Side.Left : rules.ExpectedSide;
        public SprintSnapshot Snapshot => rules == null ? default : rules.Snapshot;
        public MinigameResult LastResult { get; private set; }
        public MinigamePhase Phase => Lifecycle == null ? MinigamePhase.Tutorial : Lifecycle.Phase;
        public string LeftInputAction => leftInputAction;
        public string RightInputAction => rightInputAction;

        protected override void Awake()
        {
            base.Awake();
            rules = SprintRules.Default();
            ConfigureChallengePattern();
        }

        protected override void Update()
        {
            base.Update();
            if (Lifecycle.Phase != MinigamePhase.Play)
                return;

            if (!string.IsNullOrEmpty(leftInputAction) && Input.GetButtonDown(leftInputAction))
                OnLeftTap();
            if (!string.IsNullOrEmpty(rightInputAction) && Input.GetButtonDown(rightInputAction))
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

        protected override void TickPlay(float dt)
        {
            rules.Tick(dt);
            UpdateAuthoredChallenges(dt);
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

        void UpdateAuthoredChallenges(float dt)
        {
            if (!WindCueVisible && rules.Snapshot.Distance >= cueAt)
            {
                WindCueVisible = true;
                challengeElapsed = 0f;
            }

            if (WindCueVisible && !windChallengeResolved)
            {
                challengeElapsed += dt;
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
