using System;
using KMA.Gameplay;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    public sealed class MapScreen : ScreenBase
    {
        public event Action<SubjectId> SubjectRequested;
        public event Action BossRequested;
        public bool BossUnlocked { get; private set; }
        public HeartBar Hearts { get; private set; }
        public MapNodeView[] Nodes { get; private set; } = new MapNodeView[0];

        public void SetBossUnlocked(bool unlocked) => BossUnlocked = unlocked;

        public void BindPresentation(MapNodeView[] nodes, HeartBar heartBar, GameSession session)
        {
            Nodes = nodes ?? new MapNodeView[0];
            Hearts = heartBar;
            if (session == null)
                return;
            if (Hearts != null)
                Hearts.SetHearts(session.Lives);
            foreach (var node in Nodes)
            {
                if (node != null && !node.IsComingSoon)
                {
                    if (node.HasSubjectConfigAsset)
                        node.Configure(node.SubjectConfigAsset, session.GetRecord(node.SubjectId), session.Lives);
                    else
                        node.Configure(node.SubjectId, node.DisplayName, false,
                            session.GetRecord(node.SubjectId), session.Lives);
                }
            }
        }

        public void SelectSubject(SubjectId subject) => SubjectRequested?.Invoke(subject);
        public void SelectBoss()
        {
            if (BossUnlocked)
                BossRequested?.Invoke();
        }
    }
}
