using System;
using UnityEngine;

namespace KMA.Gameplay.FrogJump
{
    public enum FrogJumpState
    {
        Aiming,
        Jumping,
        Fallen,
        Finished,
        TimedOut
    }

    public sealed class FrogJumpRules
    {
        readonly FrogJumpTuning tuning;
        float sweepTime;
        float stateTime;
        float pendingMetres;

        public FrogJumpRules(FrogJumpTuning tuning)
        {
            this.tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        }

        public event Action<float> Landed;

        public FrogJumpState State { get; private set; } = FrogJumpState.Aiming;
        public float Elapsed { get; private set; }
        public float Distance { get; private set; }
        public float? LastJumpMetres { get; private set; }
        public int Jumps { get; private set; }
        public int Falls { get; private set; }
        /// Seconds spent in the current Jumping/Fallen state; the view interpolates the hop with it.
        public float StateElapsed => stateTime;
        public float TimeRemaining => Mathf.Max(0f, tuning.timeLimitSeconds - Elapsed);
        public float Progress01 => tuning.trackMetres <= 0f ? 1f : Mathf.Clamp01(Distance / tuning.trackMetres);
        public bool IsOver => State == FrogJumpState.Finished || State == FrogJumpState.TimedOut;
        public bool ReachedFinish => State == FrogJumpState.Finished;
        public FrogJumpTuning Tuning => tuning;

        /// 0 at the left edge, 1 at the right edge; ping-pongs once per sweepSeconds.
        public float Needle01
        {
            get
            {
                if (tuning.sweepSeconds <= 0f) return .5f;
                float phase = Mathf.Repeat(sweepTime / tuning.sweepSeconds, 2f);
                return phase <= 1f ? phase : 2f - phase;
            }
        }

        public static bool IsFall(float needle01, FrogJumpTuning tuning) =>
            Mathf.Abs(Mathf.Clamp01(needle01) - .5f) * 2f > tuning.safeZone + 1e-5f;

        public static float JumpMetres(float needle01, FrogJumpTuning tuning)
        {
            if (IsFall(needle01, tuning)) return 0f;
            float d = Mathf.Abs(Mathf.Clamp01(needle01) - .5f) * 2f;
            float t = tuning.safeZone <= 0f ? 0f : Mathf.Clamp01(d / tuning.safeZone);
            return Mathf.Lerp(tuning.maxJumpMetres, tuning.minJumpMetres, t);
        }

        public bool Stop()
        {
            if (State != FrogJumpState.Aiming)
                return false;
            float needle = Needle01;
            stateTime = 0f;
            if (IsFall(needle, tuning))
            {
                LastJumpMetres = 0f;
                Falls++;
                State = FrogJumpState.Fallen;
                Landed?.Invoke(0f);
            }
            else
            {
                pendingMetres = JumpMetres(needle, tuning);
                LastJumpMetres = pendingMetres;
                State = FrogJumpState.Jumping;
            }
            return true;
        }

        public void Tick(float dt)
        {
            if (IsOver || dt <= 0f)
                return;
            float step = Mathf.Min(dt, TimeRemaining);
            Elapsed += step;
            switch (State)
            {
                case FrogJumpState.Aiming:
                    sweepTime += step;
                    break;
                case FrogJumpState.Jumping:
                    stateTime += step;
                    if (stateTime >= tuning.jumpSeconds - 1e-5f)
                    {
                        Distance = Mathf.Min(tuning.trackMetres, Distance + pendingMetres);
                        Jumps++;
                        Landed?.Invoke(pendingMetres);
                        if (Distance >= tuning.trackMetres - 1e-4f)
                        {
                            Distance = tuning.trackMetres;
                            State = FrogJumpState.Finished;
                            return;
                        }
                        BeginAim();
                    }
                    break;
                case FrogJumpState.Fallen:
                    stateTime += step;
                    if (stateTime >= tuning.recoverSeconds - 1e-5f)
                        BeginAim();
                    break;
            }
            if (!IsOver && Elapsed >= tuning.timeLimitSeconds - 1e-5f)
                State = FrogJumpState.TimedOut;
        }

        void BeginAim()
        {
            State = FrogJumpState.Aiming;
            sweepTime = 0f;
            stateTime = 0f;
        }
    }
}
