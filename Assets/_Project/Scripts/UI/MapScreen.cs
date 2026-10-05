using System;
using System.Linq;
using KMA.Gameplay;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    public sealed class MapScreen : ScreenBase
    {
        public event Action<SubjectId> SubjectRequested;
        public event Action<string, ChallengeAttemptMode> ChallengeRequested;
        public HeartBar Hearts { get; private set; }
        public MapNodeView[] Nodes { get; private set; } = new MapNodeView[0];
        public JourneyLessonList LessonList { get; private set; }
        public TMPro.TMP_Text BudgetLabel { get; private set; }
        public JourneyCourseSummary CourseSummary { get; private set; }


        GameSession boundSession;
        Func<bool> persistLives;

        /// Overrides how a regenerated life is saved (defaults to the GameManager save).
        public void ConfigureLifePersistence(Func<bool> persist) => persistLives = persist;

        bool PersistLives()
        {
            if (persistLives != null) return persistLives();
            var manager = KMA.Gameplay.Core.GameManager.Instance;
            return manager != null && manager.TryPersistSession(out _);
        }

        void Update()
        {
            if (boundSession == null) return;
            if (boundSession.RefreshLives())
            {
                PersistLives();
                RefreshJourney(boundSession);
            }
            Hearts?.SetCountdown(boundSession.TimeUntilNextLife, boundSession.Lives == 0);
        }

        public void BindPresentation(MapNodeView[] nodes, HeartBar heartBar, GameSession session)
            => BindPresentation(nodes, heartBar, session, null);

        public void BindPresentation(MapNodeView[] nodes, HeartBar heartBar, GameSession session,
            JourneyLessonList lessonList)
        {
            Nodes = nodes ?? new MapNodeView[0];
            Hearts = heartBar;
            LessonList = lessonList != null ? lessonList : GetComponentInChildren<JourneyLessonList>(true);
            CourseSummary = GetComponentInChildren<JourneyCourseSummary>(true);
            boundSession = session;
            if (session == null)
                return;
            if (LessonList != null)
                LessonList.Bind(session, (id, mode) => ChallengeRequested?.Invoke(id, mode));
            RefreshJourney(session);
        }

        public void RefreshJourney(GameSession session)
        {
            if (session == null) return;
            if (Hearts != null) Hearts.SetHearts(session.Lives);
            if (BudgetLabel != null) BudgetLabel.text = VietText.Fix($"Lượt thi: {session.Lives}/{GameSession.MaxLives}");
            if (CourseSummary != null)
            {
                if (session.Journey.CourseComplete) CourseSummary.Show(session);
                else CourseSummary.Hide();
            }
            foreach (MapNodeView node in Nodes)
            {
                if (node == null || node.IsComingSoon) continue;
                string title = node.SubjectId switch
                {
                    SubjectId.Sprint => "Chạy nước rút",
                    SubjectId.Volleyball => "Bóng chuyền",
                    SubjectId.Football => "Bóng đá",
                    _ => node.DisplayName
                };
                SubjectRecord record = session.GetRecord(node.SubjectId);
                node.ConfigureJourneyState(node.SubjectId, title,
                    session.Journey.IsSubjectUnlocked(node.SubjectId), record.Passed, record);
            }
            LessonList?.Bind(session, (id, mode) => ChallengeRequested?.Invoke(id, mode));
            ApplyMarkers();
        }

        public void BindBudgetLabel(TMPro.TMP_Text label) => BudgetLabel = label;

        public void SelectSubject(SubjectId subject)
        {
            LessonList?.ShowSubject(subject);
            ApplyMarkers();
            SubjectRequested?.Invoke(subject);
        }

        static readonly SubjectId[] CourseOrder = { SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football };

        void ApplyMarkers()
        {
            MapNodeView current = Nodes
                .Where(node => node != null && !node.IsComingSoon && !node.IsLocked && !node.IsCompleted)
                .OrderBy(node => Array.IndexOf(CourseOrder, node.SubjectId))
                .FirstOrDefault();
            SubjectId selected = LessonList != null ? LessonList.SelectedSubject : SubjectId.Sprint;
            foreach (MapNodeView node in Nodes)
                if (node != null)
                    node.SetJourneyMarkers(node == current, node.SubjectId == selected && !node.IsLocked);
        }
    }
}
