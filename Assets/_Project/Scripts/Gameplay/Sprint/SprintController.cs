using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class SprintController : MinigameBase
    {
        [SerializeField] SprintChallengePattern challengePattern = new SprintChallengePattern();

        SprintRules rules;
        float cueAt;
        float activeAt;
        float challengeElapsed;
        bool windChallengeResolved;

        public bool WindCueVisible { get; private set; }
        public bool WindWindowActive { get; private set; }
        public bool WindChallengeCountered { get; private set; }
        public bool WindChallengeFailed { get; private set; }
        public Side ExpectedSide => rules == null ? Side.Left : rules.ExpectedSide;
        public SprintSnapshot Snapshot => rules == null ? default : rules.Snapshot;
        public MinigameResult LastResult { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            rules = SprintRules.Default();
            ConfigureChallengePattern();
        }

        public void ConfigureForTest(float cueLeadSeconds)
        {
            rules = SprintRules.ForTest(0f, 0f, 1);
            challengePattern = SprintChallengePattern.AuthoredDefault();
            challengePattern.ConfigureForTest(cueLeadSeconds);
            ConfigureChallengePattern();
            WindCueVisible = false;
            WindWindowActive = false;
            WindChallengeCountered = false;
            WindChallengeFailed = false;
            windChallengeResolved = false;
            challengeElapsed = 0f;
        }

        public void AdvanceToDistance(float value)
        {
            rules = SprintRules.ForTest(value, rules == null ? 0f : rules.Elapsed, 1);
        }

        public void Simulate(float dt)
        {
            if (rules == null)
                return;

            rules.Tick(dt);
            UpdateAuthoredChallenges(dt);
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
            if (rules.Snapshot.Stamina <= 0f)
                Finish(BuildResult());
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
        }

        void ConfigureChallengePattern()
        {
            cueAt = challengePattern.WindCueDistance;
            activeAt = challengePattern.WindActivationDistance;
        }

        void UpdateAuthoredChallenges(float dt)
        {
            float distanceAfterTick = rules.Snapshot.Distance + Mathf.Max(0f, dt);
            if (!WindCueVisible && distanceAfterTick >= cueAt)
            {
                WindCueVisible = true;
                challengeElapsed = 0f;
            }

            if (WindCueVisible && !WindWindowActive)
            {
                challengeElapsed += dt;
                if (challengeElapsed >= challengePattern.WindCueLeadSeconds && distanceAfterTick >= activeAt)
                    WindWindowActive = true;
            }
        }
    }
}
