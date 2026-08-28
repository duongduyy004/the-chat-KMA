using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    public interface ITutorialSeenStore
    {
        bool HasSeen(string subjectId);
        void MarkSeen(string subjectId);
    }

    public sealed class MemoryTutorialSeenStore : ITutorialSeenStore
    {
        private readonly HashSet<string> seenSubjects = new HashSet<string>();
        public bool HasSeen(string subjectId) => seenSubjects.Contains(subjectId);
        public void MarkSeen(string subjectId) => seenSubjects.Add(subjectId);
    }

    public sealed class PlayerPrefsTutorialSeenStore : ITutorialSeenStore
    {
        private const string KeyPrefix = "KMA.tutorialSeen.";
        public bool HasSeen(string subjectId) => PlayerPrefs.GetInt(KeyPrefix + subjectId, 0) != 0;
        public void MarkSeen(string subjectId) => PlayerPrefs.SetInt(KeyPrefix + subjectId, 1);
    }
}
