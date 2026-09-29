using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum KickerPose { Ready, RunUp, Strike, Celebrate }

    public enum KeeperPose { Ready, Save, Beaten }

    /// <summary>Which pose the kicker and the keeper show at each moment of a penalty.</summary>
    public static class FootballPoses
    {
        public static KickerPose Kicker(FootballState state, float stateElapsed, FootballOutcome? outcome)
        {
            if (outcome.HasValue)
                return outcome.Value == FootballOutcome.Goal ? KickerPose.Celebrate : KickerPose.Ready;
            if (state == FootballState.Kicking)
                return stateElapsed < FootballRules.KickAnimationSeconds * .5f ? KickerPose.RunUp : KickerPose.Strike;
            return state == FootballState.Flying ? KickerPose.Strike : KickerPose.Ready;
        }

        public static KeeperPose Keeper(FootballOutcome? outcome) => outcome switch
        {
            FootballOutcome.Saved => KeeperPose.Save,
            FootballOutcome.Goal => KeeperPose.Beaten,
            _ => KeeperPose.Ready
        };
    }

    /// <summary>The sprite for every pose. All poses share one size, so swapping keeps the renderer's scale.</summary>
    [Serializable]
    public sealed class FootballPoseSprites
    {
        public Sprite kickerReady, kickerRunUp, kickerStrike, kickerCelebrate;
        public Sprite keeperReady, keeperSave, keeperBeaten;

        public Sprite For(KickerPose pose) => pose switch
        {
            KickerPose.RunUp => kickerRunUp,
            KickerPose.Strike => kickerStrike,
            KickerPose.Celebrate => kickerCelebrate,
            _ => kickerReady
        };

        public Sprite For(KeeperPose pose) => pose switch
        {
            KeeperPose.Save => keeperSave,
            KeeperPose.Beaten => keeperBeaten,
            _ => keeperReady
        };
    }
}
