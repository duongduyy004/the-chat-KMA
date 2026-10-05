using System;

namespace KMA.Gameplay.Celebration
{
    public enum CelebrationBeat { Arrive, Cheer, Teacher, Summary }

    /// Spec section 5.3: arrive 0-2 s, cheer 2-5 s, teacher 5-8 s, summary from 8 s. Skip jumps to the
    /// summary through the same path the timeline takes.
    public sealed class CelebrationTimeline
    {
        public const float CheerAt = 2f, TeacherAt = 5f, SummaryAt = 8f, Duration = 11f;

        public event Action SummaryRequested;

        public float Time { get; private set; }
        public bool SummaryShown { get; private set; }
        public bool Skipped { get; private set; }

        public CelebrationBeat Beat => Time >= SummaryAt ? CelebrationBeat.Summary
            : Time >= TeacherAt ? CelebrationBeat.Teacher
            : Time >= CheerAt ? CelebrationBeat.Cheer
            : CelebrationBeat.Arrive;

        public void Tick(float dt)
        {
            if (!(dt > 0f)) return;
            Time = Math.Min(Duration, Time + dt);
            if (Beat == CelebrationBeat.Summary) ShowSummary();
        }

        public void Skip()
        {
            if (SummaryShown) return;
            Skipped = true;
            Time = Math.Max(Time, SummaryAt);
            ShowSummary();
        }

        void ShowSummary()
        {
            if (SummaryShown) return;
            SummaryShown = true;
            SummaryRequested?.Invoke();
        }
    }
}
