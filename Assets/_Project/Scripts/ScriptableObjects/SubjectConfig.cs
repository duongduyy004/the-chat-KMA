using UnityEngine;

namespace KMA.Gameplay
{
    [CreateAssetMenu(menuName = "KMA/Subject Config", fileName = "SubjectConfig")]
    public sealed class SubjectConfig : ScriptableObject
    {
        public SubjectId subjectId;
        public string displayName;
        public Sprite icon;
        public Color color = Color.white;
        [TextArea] public string goalText;
        [Min(0f)] public float timeLimit = 60f;
        [Range(0f, 1f)] public float passThreshold = 0.6f;
        public bool unlocked = true;
        public bool comingSoon;
    }
}
