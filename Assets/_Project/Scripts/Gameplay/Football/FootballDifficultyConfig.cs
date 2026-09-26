using UnityEngine;

namespace KMA.Gameplay
{
    [CreateAssetMenu(menuName = "KMA/Football/Difficulty Config", fileName = "FootballDifficulty")]
    public sealed class FootballDifficultyConfig : ScriptableObject
    {
        [SerializeField] Vector3 easy = new Vector3(2.4f, .38f, 1.9f);
        [SerializeField] Vector3 normal = new Vector3(2.042035f, .23f, 2.5f);
        [SerializeField] Vector3 hard = new Vector3(1.7f, .15f, 3.2f);

        public FootballTuning Get(FootballDifficulty difficulty)
        {
            Vector3 values = difficulty switch
            {
                FootballDifficulty.Easy => easy,
                FootballDifficulty.Normal => normal,
                FootballDifficulty.Hard => hard,
                _ => throw new System.ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unknown Football difficulty.")
            };
            return new FootballTuning(values.x, values.y, values.z);
        }

        void OnValidate()
        {
            if (!IsValid(easy)) Debug.LogError("Football easy tuning values must be finite and positive.", this);
            if (!IsValid(normal)) Debug.LogError("Football normal tuning values must be finite and positive.", this);
            if (!IsValid(hard)) Debug.LogError("Football hard tuning values must be finite and positive.", this);
        }

        static bool IsValid(Vector3 values)
        {
            try { _ = new FootballTuning(values.x, values.y, values.z); return true; }
            catch (System.ArgumentOutOfRangeException) { return false; }
        }
    }
}
