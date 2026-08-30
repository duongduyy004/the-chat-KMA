using UnityEngine;

namespace KMA.Gameplay
{
    [CreateAssetMenu(menuName = "KMA/Rival Pace Profile", fileName = "RivalPaceProfile")]
    public sealed class RivalPaceProfileAsset : ScriptableObject
    {
        public string profileName;
        [Min(0f)] public float openingSpeed;
        [Min(0f)] public float sustainedSpeed;

        public RivalPaceProfile ToRuntime() =>
            new RivalPaceProfile(profileName, openingSpeed, sustainedSpeed);
    }
}
