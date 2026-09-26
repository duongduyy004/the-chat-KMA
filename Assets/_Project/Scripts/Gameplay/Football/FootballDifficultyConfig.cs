using UnityEngine;

namespace KMA.Gameplay
{
    [CreateAssetMenu(menuName = "KMA/Football/Difficulty Config", fileName = "FootballDifficulty")]
    public sealed class FootballDifficultyConfig : ScriptableObject
    {
        [SerializeField] Vector4 easy = new Vector4(2.4f, 1.4f, .40f, .75f);
        [SerializeField] Vector4 normal = new Vector4(1.7f, 1.4f, .27f, 1.20f);
        [SerializeField] Vector4 hard = new Vector4(1.1f, .9f, .15f, 1.70f);

        public FootballTuning Get(FootballDifficulty difficulty)
        {
            Vector4 values = difficulty switch
            {
                FootballDifficulty.Easy => easy,
                FootballDifficulty.Normal => normal,
                FootballDifficulty.Hard => hard,
                _ => throw new System.ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unknown Football difficulty.")
            };
            return new FootballTuning(values.x, values.y, values.z, values.w);
        }

        void OnValidate()
        {
            if (!IsValid(easy)) Debug.LogError("Football easy tuning values must be finite and positive.", this);
            if (!IsValid(normal)) Debug.LogError("Football normal tuning values must be finite and positive.", this);
            if (!IsValid(hard)) Debug.LogError("Football hard tuning values must be finite and positive.", this);
        }

        static bool IsValid(Vector4 values)
        {
            try { _ = new FootballTuning(values.x, values.y, values.z, values.w); return true; }
            catch (System.ArgumentOutOfRangeException) { return false; }
        }
    }
}
