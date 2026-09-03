using System;
using System.Collections.Generic;

namespace KMA.Gameplay
{
    public abstract class ChallengeDetectorAdapter
    {
        protected ChallengeDetectorAdapter(ChallengeMechanic mechanic)
        {
            Mechanic = mechanic;
        }

        public ChallengeMechanic Mechanic { get; }
        public bool Active { get; private set; }

        internal void Activate() => Active = true;
        internal void Deactivate() => Active = false;
    }

    public sealed class TapMashDetector : ChallengeDetectorAdapter
    {
        public TapMashDetector() : base(ChallengeMechanic.TapMash) { }
    }

    public sealed class RhythmBeatDetector : ChallengeDetectorAdapter
    {
        public RhythmBeatDetector() : base(ChallengeMechanic.RhythmHold) { }
    }

    public sealed class HoldDetector : ChallengeDetectorAdapter
    {
        public HoldDetector() : base(ChallengeMechanic.RhythmHold) { }
    }

    public sealed class AlternateTapDetector : ChallengeDetectorAdapter
    {
        public AlternateTapDetector() : base(ChallengeMechanic.AlternateTap) { }
    }

    public sealed class PunishmentController
    {
        readonly GameSession session;
        readonly SubjectId subject;
        readonly ChallengeSequence sequence;
        readonly List<ChallengeDetectorAdapter> activeDetectors =
            new List<ChallengeDetectorAdapter>();
        bool completionRouted;

        public PunishmentController(GameSession session, SubjectId subject, ChallengeSequence sequence)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.subject = subject;
            this.sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            if (!this.session.PendingPunishmentSubject.HasValue)
                throw new InvalidOperationException("No punishment is active.");
            if (this.session.PendingPunishmentSubject.Value != subject)
                throw new InvalidOperationException("Punishment subject does not match the active session subject.");
            ConfigureDetectors();
            this.sequence.Completed += OnSequenceCompleted;
        }

        public event Action<SessionRoute> Completed;

        public ChallengeStep Current => sequence.Current;
        public SubjectId Subject => subject;
        public ChallengeMechanic CurrentMechanic => sequence.Current.Mechanic;
        public bool IsComplete => sequence.IsComplete;
        public bool CueVisible => !sequence.IsComplete;
        public bool CounterplayAvailable => activeDetectors.Count > 0 && !sequence.IsComplete;
        public IReadOnlyList<ChallengeDetectorAdapter> ActiveDetectors => activeDetectors;

        public void ReportDetectorProgress(float value)
        {
            if (sequence.IsComplete)
                return;

            sequence.ReportProgress(value);
            if (!sequence.IsComplete)
                ConfigureDetectors();
        }

        void ConfigureDetectors()
        {
            for (var detectorIndex = 0; detectorIndex < activeDetectors.Count; detectorIndex++)
                activeDetectors[detectorIndex].Deactivate();
            activeDetectors.Clear();

            switch (sequence.Current.Mechanic)
            {
                case ChallengeMechanic.TapMash:
                    activeDetectors.Add(new TapMashDetector());
                    break;
                case ChallengeMechanic.RhythmHold:
                    activeDetectors.Add(new RhythmBeatDetector());
                    activeDetectors.Add(new HoldDetector());
                    break;
                case ChallengeMechanic.AlternateTap:
                    activeDetectors.Add(new AlternateTapDetector());
                    break;
                default:
                    throw new InvalidOperationException("Unsupported challenge mechanic.");
            }

            for (var detectorIndex = 0; detectorIndex < activeDetectors.Count; detectorIndex++)
                activeDetectors[detectorIndex].Activate();
        }

        void OnSequenceCompleted()
        {
            if (completionRouted)
                return;

            completionRouted = true;
            for (var detectorIndex = 0; detectorIndex < activeDetectors.Count; detectorIndex++)
                activeDetectors[detectorIndex].Deactivate();
            activeDetectors.Clear();
            Completed?.Invoke(SessionRoute.RetrySubject);
        }
    }
}
