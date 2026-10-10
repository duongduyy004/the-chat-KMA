using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum Side
    {
        Left,
        Right
    }

    public readonly struct SprintSnapshot : System.IEquatable<SprintSnapshot>
    {
        public float Distance { get; }
        public float Speed { get; }
        public float Elapsed { get; }

        public SprintSnapshot(float distance, float speed, float elapsed)
        {
            Distance = distance;
            Speed = speed;
            Elapsed = elapsed;
        }

        public bool Equals(SprintSnapshot other) =>
            Distance.Equals(other.Distance) && Speed.Equals(other.Speed) && Elapsed.Equals(other.Elapsed);

        public override bool Equals(object obj) => obj is SprintSnapshot other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Distance.GetHashCode();
                hash = hash * 397 ^ Speed.GetHashCode();
                return hash * 397 ^ Elapsed.GetHashCode();
            }
        }
    }

    public sealed class SprintRules
    {
        public const float RaceDistance = 150f;
        public const float DefaultTimeLimit = 22f;

        readonly float timeLimit;
        readonly float goalDistance;
        readonly Side[] authoredSequence;
        readonly RivalPaceProfile[] rivalProfiles;
        readonly float[] rivalDistances;
        readonly SprintBalanceParameters? balance;
        int sequenceIndex;
        int valid;
        int total;
        int correctStreak;
        int currentRank = 1;
        float lastTapElapsed;
        bool hasTapped;
        float distance;
        float speed;
        float elapsed;
        float rivalClock;
        bool finished;

        public SprintRules(float timeLimit = DefaultTimeLimit, RivalPaceProfile[] rivalProfiles = null,
            Side[] authoredSequence = null, SprintBalanceParameters? balance = null,
            float goalDistance = RaceDistance)
        {
            this.timeLimit = timeLimit;
            this.balance = balance;
            this.goalDistance = goalDistance;
            this.authoredSequence = authoredSequence == null ? new[] { Side.Left, Side.Right } : (Side[])authoredSequence.Clone();
            if (this.authoredSequence.Length == 0)
                throw new System.ArgumentException("Sprint authored sequence must contain at least one side.", nameof(authoredSequence));

            this.rivalProfiles = rivalProfiles == null ? Array.Empty<RivalPaceProfile>() : (RivalPaceProfile[])rivalProfiles.Clone();
            rivalDistances = new float[this.rivalProfiles.Length];
        }

        public static SprintRules Default() => new SprintRules(DefaultTimeLimit);

        public static SprintRules ForTest(float distance, float elapsed, int rank,
            RivalPaceProfile[] rivalProfiles = null, Side[] authoredSequence = null)
        {
            _ = rank;
            var value = new SprintRules(DefaultTimeLimit, rivalProfiles, authoredSequence);
            value.distance = distance;
            value.elapsed = elapsed;
            value.UpdateRank();
            return value;
        }

        public float Distance => distance;
        public float Speed => speed;
        public float Elapsed => elapsed;
        /// The player crossed the line: their numbers and rank are frozen while rivals keep running.
        public bool IsFinished => finished;
        float RivalTime => finished ? rivalClock : elapsed;
        public float TimeLimit => timeLimit;
        public int CorrectStreak => correctStreak;
        public float ComboBoost => ComboBoostFor(correctStreak, Tuning);
        public bool IsComboBoosting => ComboBoost > 0f;
        public float ValidTapRatio => total == 0 ? 0f : (float)valid / total;
        public int Rank => currentRank;
        public Side ExpectedSide => authoredSequence[sequenceIndex];
        public Side[] AuthoredSequence => (Side[])authoredSequence.Clone();
        public RivalPaceProfile[] RivalProfiles => (RivalPaceProfile[])rivalProfiles.Clone();
        public float[] RivalDistances => (float[])rivalDistances.Clone();
        public int RivalCount => rivalDistances.Length;
        public float GetRivalDistance(int index) => index < 0 || index >= rivalDistances.Length ? 0f : rivalDistances[index];
        public bool IsRivalSurging(int index) =>
            index >= 0 && index < rivalProfiles.Length && rivalProfiles[index] != null &&
            rivalProfiles[index].IsSurgingAt(RivalTime);
        public bool IsRivalSlowing(int index) =>
            index >= 0 && index < rivalProfiles.Length && rivalProfiles[index] != null &&
            rivalProfiles[index].IsSlowingAt(RivalTime);
        public SprintSnapshot Snapshot => new SprintSnapshot(distance, speed, elapsed);

        // Races without an authored balance asset use the default tuning.
        SprintBalanceParameters Tuning => balance ?? SprintBalanceParameters.Default;

        // A long unbroken alternation lifts both the impulse and the speed cap, up to ComboBoostMax.
        static float ComboBoostFor(int streak, SprintBalanceParameters tuning)
        {
            if (tuning.ComboBoostMax <= 0f || streak <= tuning.ComboBoostStartStreak) return 0f;
            int span = tuning.ComboBoostFullStreak - tuning.ComboBoostStartStreak;
            float t = span <= 0 ? 1f : Mathf.Clamp01((float)(streak - tuning.ComboBoostStartStreak) / span);
            return tuning.ComboBoostMax * t;
        }

        public void FinishRace()
        {
            if (finished) return;
            finished = true;
            rivalClock = elapsed;
            speed = 0f;
        }

        public bool AllRivalsReached(float goalDistance)
        {
            for (int i = 0; i < rivalDistances.Length; i++)
            {
                if (rivalProfiles[i] != null && rivalDistances[i] < goalDistance) return false;
            }
            return true;
        }

        public void Tap(Side side)
        {
            if (finished) return;
            bool correct = side == ExpectedSide;
            total++;
            if (correct)
            {
                valid++;
                correctStreak++;
                sequenceIndex = (sequenceIndex + 1) % authoredSequence.Length;
            }
            else
                correctStreak = 0;

            SprintBalanceParameters tuning = Tuning;
            float boost = 1f + ComboBoostFor(correctStreak, tuning);
            float impulse = (correct ? tuning.CorrectImpulse : tuning.CorrectImpulse * tuning.WrongImpulseFactor) * boost;
            float cap = tuning.SpeedCap * boost;
            // Losing the combo stops further gains but lets drag bleed off the extra speed.
            speed = Mathf.Max(speed, Mathf.Min(cap, speed + impulse));
            lastTapElapsed = elapsed;
            hasTapped = true;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            if (finished)
            {
                rivalClock += dt;
                for (int i = 0; i < rivalProfiles.Length; i++)
                {
                    if (rivalProfiles[i] != null) AdvanceRival(i, rivalClock, dt);
                }
                return;
            }
            elapsed += dt;
            SprintBalanceParameters tuning = Tuning;
            speed = Mathf.Max(0f, speed - tuning.DragPerSecond * dt - IdleBrakeOver(elapsed - dt, elapsed));
            distance += speed * dt * tuning.DistanceScale;

            for (int i = 0; i < rivalProfiles.Length; i++)
            {
                if (rivalProfiles[i] != null) AdvanceRival(i, elapsed, dt);
            }

            UpdateRank();
        }

        // A rival who has crossed the line stops there instead of running on past it.
        void AdvanceRival(int index, float time, float dt) =>
            rivalDistances[index] = Mathf.Min(goalDistance, rivalDistances[index] + rivalProfiles[index].SpeedAt(time) * dt);

        public MinigameResult BuildResult() => BuildResult(RaceDistance, timeLimit);

        public MinigameResult BuildResult(float goalDistance, float deadline)
        {
            bool pass = distance >= Mathf.Max(0f, goalDistance) && elapsed <= deadline;
            float accuracy = total == 0 ? 0f : 2f * valid / total;
            // 1st place earns the full bonus and last place none.
            int rivals = rivalDistances.Length;
            float placement = rivals == 0 ? 1f : Mathf.Clamp01((float)(rivals + 1 - currentRank) / rivals);
            float mastery = deadline <= 0f ? 0f : Mathf.Clamp01((deadline - elapsed) / 3f);
            return ScoreUtil.Build(pass, accuracy, placement, mastery);
        }

        // Once the player stops tapping, a hard brake eases in so the runner settles almost at once
        // while still sliding to a stop instead of freezing.
        // Speed the brake removes between two race times. The brake is integrated over the step, so
        // a long frame never brakes the part of it that still sat inside the grace window.
        float IdleBrakeOver(float from, float to)
        {
            if (!hasTapped || speed <= 0f) return 0f;
            SprintBalanceParameters tuning = Tuning;
            float start = lastTapElapsed + tuning.IdleGraceSeconds;
            return tuning.IdleBrakePerSecond *
                (RampedSeconds(to - start, tuning.IdleBrakeRampSeconds) -
                 RampedSeconds(from - start, tuning.IdleBrakeRampSeconds));
        }

        // Integral of a 0..1 ramp that rises over rampSeconds and then holds at 1.
        static float RampedSeconds(float seconds, float rampSeconds)
        {
            if (seconds <= 0f) return 0f;
            if (rampSeconds <= 0f) return seconds;
            return seconds <= rampSeconds
                ? seconds * seconds / (2f * rampSeconds)
                : rampSeconds * .5f + seconds - rampSeconds;
        }

        void UpdateRank()
        {
            currentRank = 1;
            for (int i = 0; i < rivalDistances.Length; i++)
            {
                if (rivalDistances[i] > distance || rivalDistances[i] >= goalDistance)
                    currentRank++;
            }
        }
    }
}
