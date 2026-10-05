using UnityEngine;

namespace KMA.Gameplay
{
    public enum ChallengeKind
    {
        Learn,
        Practice,
        Exam
    }

    public enum ChallengeDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    [CreateAssetMenu(menuName = "KMA/Journey/Challenge", fileName = "Challenge")]
    public sealed class ChallengeDefinition : ScriptableObject
    {
        [SerializeField] string id;
        [SerializeField] SubjectId subject;
        [SerializeField] ChallengeKind kind;
        [SerializeField, Min(0f)] float distance;
        [SerializeField, Min(0f)] float timeLimit;
        [SerializeField, Min(0)] int targetCount;
        [SerializeField, Min(0)] int attemptLimit;
        [SerializeField] bool keeperEnabled;
        [SerializeField] ChallengeDifficulty difficulty;
        [SerializeField] bool timingHelp;
        [SerializeField, TextArea] string objective;

        public string Id => id;
        public SubjectId Subject => subject;
        public ChallengeKind Kind => kind;
        public float Distance => distance;
        public float TimeLimit => timeLimit;
        public int TargetCount => targetCount;
        public int AttemptLimit => attemptLimit;
        public bool KeeperEnabled => keeperEnabled;
        public ChallengeDifficulty Difficulty => difficulty;
        public bool TimingHelp => timingHelp;
        public string Objective => objective;
    }
}
