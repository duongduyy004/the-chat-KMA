using System;

namespace KMA.Gameplay
{
    public sealed class ChallengeSequence
    {
        readonly ChallengeStep[] steps;
        int index;

        public ChallengeSequence(ChallengeStep[] steps)
        {
            if (steps == null || steps.Length == 0)
                throw new ArgumentException("At least one challenge step is required.", nameof(steps));

            for (var stepIndex = 0; stepIndex < steps.Length; stepIndex++)
            {
                if (steps[stepIndex].Duration <= 0f || steps[stepIndex].Target <= 0f ||
                    !Enum.IsDefined(typeof(ChallengeMechanic), steps[stepIndex].Mechanic))
                    throw new ArgumentException("Challenge steps must be valid.", nameof(steps));
            }

            this.steps = (ChallengeStep[])steps.Clone();
            Current = this.steps[0];
        }

        public event Action Completed;

        public ChallengeStep Current { get; private set; }
        public bool IsComplete { get; private set; }

        public void ReportProgress(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (IsComplete || value < Current.Target)
                return;

            index++;
            IsComplete = index >= steps.Length;
            if (IsComplete)
            {
                Completed?.Invoke();
                return;
            }

            Current = steps[index];
        }

        public static ChallengeSequence BossDefault() => new ChallengeSequence(new[]
        {
            new ChallengeStep(ChallengeMechanic.TapMash, 10, 40),
            new ChallengeStep(ChallengeMechanic.RhythmHold, 12, 16),
            new ChallengeStep(ChallengeMechanic.AlternateTap, 10, 32)
        });

        public void Reset()
        {
            index = 0;
            IsComplete = false;
            Current = steps[0];
        }
    }
}
