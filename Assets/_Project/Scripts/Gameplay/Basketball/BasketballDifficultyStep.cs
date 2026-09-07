using System;
using UnityEngine;

namespace KMA.Gameplay
{
    // Spec S10: each step raises exactly one axis. finishCueLeadSeconds is the timing axis - how
    // much warning the finisher gets before the apex. chargeAngleSpanDegrees is the path axis -
    // widening the span shrinks the fraction of the charge that lands the lob in the authored
    // apex band, without touching the band itself.
    [Serializable]
    public struct BasketballDifficultyStep
    {
        [Min(.05f)] public float finishCueLeadSeconds;
        [Min(5f)] public float chargeAngleSpanDegrees;

        public BasketballDifficultyStep(float finishCueLeadSeconds, float chargeAngleSpanDegrees)
        {
            this.finishCueLeadSeconds = finishCueLeadSeconds;
            this.chargeAngleSpanDegrees = chargeAngleSpanDegrees;
        }
    }
}
