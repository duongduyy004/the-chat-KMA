using UnityEngine;

namespace KMA.Gameplay.FrogJump
{
    [CreateAssetMenu(menuName = "KMA/Frog Jump/Balance", fileName = "FrogJumpBalance")]
    public sealed class FrogJumpBalanceConfig : ScriptableObject
    {
        [SerializeField] FrogJumpTuning tuning = new FrogJumpTuning();
        public FrogJumpTuning Tuning => tuning ?? new FrogJumpTuning();
    }
}
