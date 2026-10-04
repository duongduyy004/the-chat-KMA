using System;
using System.Collections.Generic;
using System.Linq;

namespace KMA.Gameplay
{
    public sealed class JourneyProgress
    {
        const string NoSupplementaryChallenge = "";
        readonly ChallengeCatalog catalog;
        readonly HashSet<string> completedChallengeIds = new HashSet<string>(StringComparer.Ordinal);

        ChallengeAttemptContext activeAttempt;
        string lastCommittedAttemptId;
        JourneyResultData lastCommittedResult;
        string awaitingSupplementaryChallengeId;
        int attemptsRemaining = GameSession.MaxLives;
        int supplementaryRounds;
        List<string> seenDialogueIds = new List<string>();

        public JourneyProgress(ChallengeCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public string CheckpointChallengeId
        {
            get
            {
                if (AwaitingSupplementary)
                    return awaitingSupplementaryChallengeId;
                return catalog.Ordered.FirstOrDefault(x => !completedChallengeIds.Contains(x.Id))?.Id;
            }
        }

        public int AttemptsRemaining => attemptsRemaining;
        public bool AwaitingSupplementary => !string.IsNullOrEmpty(awaitingSupplementaryChallengeId);
        public int SupplementaryRounds => supplementaryRounds;
        public bool CourseComplete => catalog.Ordered.All(x => completedChallengeIds.Contains(x.Id));
        public ChallengeAttemptContext ActiveAttempt => activeAttempt;
        public ChallengeCatalog Catalog => catalog;

        public bool IsDialogueSeen(string key) => !string.IsNullOrWhiteSpace(key) &&
            seenDialogueIds.Contains(key, StringComparer.Ordinal);

        public void MarkDialogueSeen(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A dialogue seen key is required.", nameof(key));
            if (!IsDialogueSeen(key)) seenDialogueIds.Add(key);
        }

        public void UnmarkDialogueSeen(string key)
        {
            if (!string.IsNullOrWhiteSpace(key)) seenDialogueIds.RemoveAll(item => item == key);
        }

        public bool IsChallengeComplete(string id) =>
            !string.IsNullOrWhiteSpace(id) && completedChallengeIds.Contains(id);

        public bool IsChallengeUnlocked(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;
            if (CourseComplete || IsChallengeComplete(id))
                return catalog.Ordered.Any(x => x.Id == id);
            return string.Equals(CheckpointChallengeId, id, StringComparison.Ordinal);
        }

        public bool IsSubjectUnlocked(SubjectId subject)
        {
            switch (subject)
            {
                case SubjectId.Sprint:
                    return true;
                case SubjectId.Volleyball:
                    return IsChallengeComplete("sprint_exam");
                case SubjectId.Football:
                    return IsChallengeComplete("volleyball_exam");
                default:
                    return false;
            }
        }

        public bool TryBegin(string id, ChallengeAttemptMode mode, ChallengeDifficulty difficulty,
            out ChallengeAttemptContext context)
        {
            context = null;
            if (activeAttempt != null || !Enum.IsDefined(typeof(ChallengeAttemptMode), mode) ||
                !Enum.IsDefined(typeof(ChallengeDifficulty), difficulty))
            {
                return false;
            }

            ChallengeDefinition definition;
            try
            {
                definition = catalog.Get(id);
            }
            catch (KeyNotFoundException)
            {
                return false;
            }

            bool allowed = mode switch
            {
                ChallengeAttemptMode.Journey => !AwaitingSupplementary && id == CheckpointChallengeId,
                ChallengeAttemptMode.Supplementary => AwaitingSupplementary &&
                    id == awaitingSupplementaryChallengeId && definition.Kind == ChallengeKind.Practice,
                ChallengeAttemptMode.Review => IsChallengeComplete(id) && IsSubjectUnlocked(definition.Subject),
                ChallengeAttemptMode.FreePlay => CourseComplete,
                _ => false
            };
            if (!allowed)
                return false;

            if (definition.Subject == SubjectId.Football)
                difficulty = ChallengeDifficulty.Normal;

            if ((mode == ChallengeAttemptMode.Journey || mode == ChallengeAttemptMode.Supplementary) &&
                difficulty != definition.Difficulty)
            {
                return false;
            }

            context = new ChallengeAttemptContext(Guid.NewGuid().ToString("N"), id, mode, difficulty);
            activeAttempt = context;
            return true;
        }

        public JourneyCommitOutcome Apply(ChallengeAttemptResult result)
        {
            if (result == null || result.Context == null || activeAttempt == null ||
                !SameContext(activeAttempt, result.Context) ||
                string.Equals(result.Context.AttemptId, lastCommittedAttemptId, StringComparison.Ordinal))
            {
                return Outcome(false);
            }

            ChallengeDefinition definition = catalog.Get(activeAttempt.ChallengeId);
            ChallengeAttemptMode mode = activeAttempt.Mode;
            if (mode == ChallengeAttemptMode.Supplementary)
            {
                if (result.Pass)
                {
                    completedChallengeIds.Add(definition.Id);
                    attemptsRemaining = GameSession.MaxLives;
                    supplementaryRounds++;
                    awaitingSupplementaryChallengeId = null;
                }
            }
            else if (mode == ChallengeAttemptMode.Journey)
            {
                if (definition.Kind == ChallengeKind.Exam)
                {
                    if (result.Pass)
                    {
                        completedChallengeIds.Add(definition.Id);
                    }
                    else
                    {
                        attemptsRemaining = Math.Max(0, attemptsRemaining - 1);
                        if (attemptsRemaining == 0)
                            awaitingSupplementaryChallengeId = PracticeId(definition.Subject);
                    }
                }
                else if (result.Pass)
                {
                    completedChallengeIds.Add(definition.Id);
                }
            }

            lastCommittedAttemptId = activeAttempt.AttemptId;
            lastCommittedResult = JourneyResultData.FromResult(result);
            activeAttempt = null;
            return Outcome(true);
        }

        public void AbandonAttempt() => activeAttempt = null;

        public JourneyStateData ToData() => new JourneyStateData
        {
            completedChallengeIds = catalog.Ordered.Where(x => completedChallengeIds.Contains(x.Id))
                .Select(x => x.Id).ToList(),
            activeAttempt = JourneyAttemptData.FromContext(activeAttempt),
            lastCommittedAttemptId = lastCommittedAttemptId,
            lastCommittedResult = lastCommittedResult?.Copy(),
            awaitingSupplementaryChallengeId = awaitingSupplementaryChallengeId,
            supplementaryRounds = supplementaryRounds,
            seenDialogueIds = new List<string>(seenDialogueIds)
        };

        public void Restore(JourneyStateData data, int attemptsRemaining)
        {
            completedChallengeIds.Clear();
            if (data?.completedChallengeIds != null)
            {
                foreach (string id in data.completedChallengeIds)
                {
                    if (catalog.Ordered.Any(x => x.Id == id))
                        completedChallengeIds.Add(id);
                }
            }

            this.attemptsRemaining = Math.Max(0, Math.Min(GameSession.MaxLives, attemptsRemaining));
            supplementaryRounds = Math.Max(0, data?.supplementaryRounds ?? 0);
            lastCommittedAttemptId = data?.lastCommittedAttemptId;
            lastCommittedResult = data?.lastCommittedResult?.Copy();
            seenDialogueIds = data?.seenDialogueIds == null
                ? new List<string>()
                : new List<string>(data.seenDialogueIds);

            awaitingSupplementaryChallengeId = IsPracticeId(data?.awaitingSupplementaryChallengeId)
                ? data.awaitingSupplementaryChallengeId
                : null;
            activeAttempt = data?.activeAttempt?.ToContext();
            if (activeAttempt != null && (string.IsNullOrWhiteSpace(activeAttempt.AttemptId) ||
                !catalog.Ordered.Any(x => x.Id == activeAttempt.ChallengeId)))
            {
                activeAttempt = null;
            }
        }

        JourneyCommitOutcome Outcome(bool accepted) => new JourneyCommitOutcome(accepted,
            CheckpointChallengeId, attemptsRemaining, AwaitingSupplementary, CourseComplete);

        bool IsPracticeId(string id) => !string.IsNullOrEmpty(id) && catalog.Ordered.Any(x =>
            x.Id == id && x.Kind == ChallengeKind.Practice);

        static bool SameContext(ChallengeAttemptContext left, ChallengeAttemptContext right) =>
            left != null && right != null && left.AttemptId == right.AttemptId &&
            left.ChallengeId == right.ChallengeId && left.Mode == right.Mode &&
            left.Difficulty == right.Difficulty;

        static string PracticeId(SubjectId subject) => subject switch
        {
            SubjectId.Sprint => "sprint_practice",
            SubjectId.Volleyball => "volleyball_practice",
            SubjectId.Football => "soccer_practice",
            _ => NoSupplementaryChallenge
        };
    }
}
