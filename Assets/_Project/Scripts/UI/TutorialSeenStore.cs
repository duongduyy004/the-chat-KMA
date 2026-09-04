using System.Collections.Generic;
using KMA.Gameplay.Core;

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

    public sealed class SaveDataTutorialSeenStore : ITutorialSeenStore
    {
        readonly ITutorialSeenStore fallback;

        public SaveDataTutorialSeenStore(ITutorialSeenStore fallback = null)
        {
            this.fallback = fallback ?? new MemoryTutorialSeenStore();
        }

        public bool HasSeen(string subjectId)
        {
            var manager = GameManager.Instance;
            return manager != null && manager.IsInitialized && TryParseSubject(subjectId, out var subject)
                ? manager.HasSeenTutorial(subject)
                : fallback.HasSeen(subjectId);
        }

        public void MarkSeen(string subjectId)
        {
            var manager = GameManager.Instance;
            if (manager != null && manager.IsInitialized && TryParseSubject(subjectId, out var subject))
                manager.MarkTutorialSeen(subject);
            else
                fallback.MarkSeen(subjectId);
        }

        static bool TryParseSubject(string value, out SubjectId subject) =>
            System.Enum.TryParse(value, true, out subject) && System.Enum.IsDefined(typeof(SubjectId), subject);
    }
}
