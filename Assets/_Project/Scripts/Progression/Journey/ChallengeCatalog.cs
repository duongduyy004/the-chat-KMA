using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KMA.Gameplay
{
    [CreateAssetMenu(menuName = "KMA/Journey/Challenge Catalog", fileName = "ChallengeCatalog")]
    public sealed class ChallengeCatalog : ScriptableObject
    {
        const string DefaultResourcePath = "Journey/ChallengeCatalog";
        public const string FinalChallengeId = "chess_final";
        const int SubjectChallengeCount = 9;
        static readonly string[] ExpectedIds =
        {
            "sprint_learn", "sprint_practice", "sprint_exam",
            "volleyball_learn", "volleyball_practice", "volleyball_exam",
            "soccer_learn", "soccer_practice", "soccer_exam",
            FinalChallengeId
        };

        [SerializeField] ChallengeDefinition[] challenges = Array.Empty<ChallengeDefinition>();

        public IReadOnlyList<ChallengeDefinition> Ordered => challenges ?? Array.Empty<ChallengeDefinition>();

        public static ChallengeCatalog LoadDefault()
        {
            ChallengeCatalog catalog = Resources.Load<ChallengeCatalog>(DefaultResourcePath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"Journey challenge catalog was not found at Resources/{DefaultResourcePath}.");
            }

            if (!catalog.Validate(out string error))
            {
                throw new InvalidOperationException($"Journey challenge catalog is invalid: {error}");
            }

            return catalog;
        }

        public ChallengeDefinition Get(string id)
        {
            ChallengeDefinition definition = Ordered.FirstOrDefault(x => x != null && x.Id == id);
            if (definition == null)
            {
                throw new KeyNotFoundException($"Journey challenge '{id}' was not found.");
            }

            return definition;
        }

        public bool Validate(out string error)
        {
            if (challenges == null || challenges.Length != ExpectedIds.Length)
            {
                error = $"Expected exactly {ExpectedIds.Length} challenge definitions.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < challenges.Length; i++)
            {
                ChallengeDefinition item = challenges[i];
                if (item == null)
                {
                    error = $"Challenge at index {i} is missing.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    error = $"Challenge at index {i} has an empty ID.";
                    return false;
                }

                if (!ids.Add(item.Id))
                {
                    error = $"Challenge ID '{item.Id}' is duplicated.";
                    return false;
                }

                if (item.Id != ExpectedIds[i])
                {
                    error = $"Challenge at index {i} must be '{ExpectedIds[i]}', found '{item.Id}'.";
                    return false;
                }

                if (!Enum.IsDefined(typeof(SubjectId), item.Subject) ||
                    !Enum.IsDefined(typeof(ChallengeKind), item.Kind) ||
                    !Enum.IsDefined(typeof(ChallengeDifficulty), item.Difficulty))
                {
                    error = $"Challenge '{item.Id}' has an unsupported enum value.";
                    return false;
                }

                if (!IsFiniteNonNegative(item.Distance) || !IsFiniteNonNegative(item.TimeLimit) ||
                    item.TargetCount < 0)
                {
                    error = $"Challenge '{item.Id}' has a negative or non-finite goal.";
                    return false;
                }
            }

            if (challenges[0].Subject != SubjectId.Sprint ||
                challenges[3].Subject != SubjectId.Volleyball ||
                challenges[6].Subject != SubjectId.Football)
            {
                error = "Course subjects must be ordered Sprint, Volleyball, Football.";
                return false;
            }

            for (int i = 0; i < SubjectChallengeCount; i++)
            {
                ChallengeKind expectedKind = i % 3 == 0
                    ? ChallengeKind.Learn
                    : i % 3 == 1 ? ChallengeKind.Practice : ChallengeKind.Exam;
                if (challenges[i].Kind != expectedKind ||
                    challenges[i].Subject != challenges[(i / 3) * 3].Subject)
                {
                    error = $"Challenge '{challenges[i].Id}' is out of course order.";
                    return false;
                }
            }

            ChallengeDefinition final = challenges[SubjectChallengeCount];
            if (final.Subject != SubjectId.Chess || final.Kind != ChallengeKind.Final)
            {
                error = "The last challenge must be the chess final.";
                return false;
            }

            if (challenges[1].TimeLimit <= challenges[2].TimeLimit)
            {
                error = "Sprint Practice must allow more time than Sprint Exam.";
                return false;
            }

            error = null;
            return true;
        }

        static bool IsFiniteNonNegative(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }
}
