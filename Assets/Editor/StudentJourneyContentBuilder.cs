using System;
using UnityEditor;
using UnityEngine;
using KMA.Gameplay;

namespace KMA.EditorTools
{
    public static class StudentJourneyContentBuilder
    {
        const string ChallengeFolder = "Assets/_Project/ScriptableObjects/Journey";
        const string ResourcesFolder = "Assets/_Project/Resources/Journey";
        const string CatalogPath = ResourcesFolder + "/ChallengeCatalog.asset";
        const string SprintBalancePath = ResourcesFolder + "/SprintBalance.asset";

        readonly struct ChallengeSpec
        {
            public readonly string Id;
            public readonly SubjectId Subject;
            public readonly ChallengeKind Kind;
            public readonly float Distance;
            public readonly float TimeLimit;
            public readonly int TargetCount;
            public readonly bool KeeperEnabled;
            public readonly ChallengeDifficulty Difficulty;
            public readonly bool TimingHelp;
            public readonly string Objective;

            public ChallengeSpec(string id, SubjectId subject, ChallengeKind kind, float distance,
                float timeLimit, int targetCount, bool keeperEnabled, ChallengeDifficulty difficulty,
                bool timingHelp, string objective)
            {
                Id = id;
                Subject = subject;
                Kind = kind;
                Distance = distance;
                TimeLimit = timeLimit;
                TargetCount = targetCount;
                KeeperEnabled = keeperEnabled;
                Difficulty = difficulty;
                TimingHelp = timingHelp;
                Objective = objective;
            }
        }

        [MenuItem("KMA/Journey/Build Challenge Assets")]
        public static void BuildChallenges()
        {
            EnsureFolder(ChallengeFolder);
            EnsureFolder(ResourcesFolder);

            ChallengeSpec[] specs =
            {
                new ChallengeSpec("sprint_learn", SubjectId.Sprint, ChallengeKind.Learn,
                    0f, 0f, 12, false, ChallengeDifficulty.Normal, true,
                    "Bấm trái, phải luân phiên đúng 12 lần liên tiếp."),
                new ChallengeSpec("sprint_practice", SubjectId.Sprint, ChallengeKind.Practice,
                    100f, 20f, 0, false, ChallengeDifficulty.Normal, true,
                    "Chạy 100 m trong tối đa 20 giây. Bài luyện không giới hạn thể lực."),
                new ChallengeSpec("sprint_exam", SubjectId.Sprint, ChallengeKind.Exam,
                    100f, 14f, 0, false, ChallengeDifficulty.Normal, false,
                    "Chạy 100 m trong tối đa 14 giây."),
                new ChallengeSpec("volleyball_learn", SubjectId.Volleyball, ChallengeKind.Learn,
                    0f, 0f, 3, false, ChallengeDifficulty.Easy, true,
                    "Đỡ thành công ba đường bóng trong một lượt."),
                new ChallengeSpec("volleyball_practice", SubjectId.Volleyball, ChallengeKind.Practice,
                    0f, 0f, 2, false, ChallengeDifficulty.Easy, true,
                    "Ghi hai điểm bằng chuỗi Đỡ → Chuyền → Đập trong cùng pha bóng."),
                new ChallengeSpec("volleyball_exam", SubjectId.Volleyball, ChallengeKind.Exam,
                    0f, 120f, 5, false, ChallengeDifficulty.Normal, false,
                    "Đạt năm điểm trước đối thủ trong tối đa 120 giây."),
                new ChallengeSpec("soccer_learn", SubjectId.Football, ChallengeKind.Learn,
                    0f, 0f, 3, false, ChallengeDifficulty.Easy, true,
                    "Ghi ba bàn; thủ môn tắt và số cú sút không giới hạn."),
                new ChallengeSpec("soccer_practice", SubjectId.Football, ChallengeKind.Practice,
                    0f, 0f, 2, true, ChallengeDifficulty.Easy, true,
                    "Ghi hai bàn trước thủ môn Easy; số cú sút không giới hạn."),
                new ChallengeSpec("soccer_exam", SubjectId.Football, ChallengeKind.Exam,
                    0f, 0f, 3, true, ChallengeDifficulty.Normal, false,
                    "Ghi ít nhất ba bàn sau đủ năm cú sút trước thủ môn Normal.")
            };

            var definitions = new ChallengeDefinition[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                ChallengeSpec spec = specs[i];
                string path = $"{ChallengeFolder}/{spec.Id}.asset";
                ChallengeDefinition definition = AssetDatabase.LoadAssetAtPath<ChallengeDefinition>(path);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<ChallengeDefinition>();
                    AssetDatabase.CreateAsset(definition, path);
                }

                Apply(definition, spec);
                definitions[i] = definition;
            }

            ChallengeCatalog catalog = AssetDatabase.LoadAssetAtPath<ChallengeCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ChallengeCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var serializedCatalog = new SerializedObject(catalog);
            SerializedProperty challenges = serializedCatalog.FindProperty("challenges");
            challenges.arraySize = definitions.Length;
            for (int i = 0; i < definitions.Length; i++)
            {
                challenges.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            }
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();

            if (!catalog.Validate(out string error))
            {
                throw new InvalidOperationException($"Generated challenge catalog is invalid: {error}");
            }

            EditorUtility.SetDirty(catalog);
            BuildSprintBalance();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KMA] Student journey challenge catalog built.");
        }

        [MenuItem("KMA/Journey/Build Sprint Balance")]
        public static void BuildSprintBalance()
        {
            EnsureFolder(ResourcesFolder);
            SprintBalanceConfig config = AssetDatabase.LoadAssetAtPath<SprintBalanceConfig>(SprintBalancePath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<SprintBalanceConfig>();
                AssetDatabase.CreateAsset(config, SprintBalancePath);
            }

            SprintBalanceParameters value = SprintBalanceParameters.Default;
            SerializedObject serialized = new SerializedObject(config);
            serialized.FindProperty("initialStamina").floatValue = value.InitialStamina;
            serialized.FindProperty("maxStamina").floatValue = value.MaxStamina;
            serialized.FindProperty("correctImpulse").floatValue = value.CorrectImpulse;
            serialized.FindProperty("wrongImpulseFactor").floatValue = value.WrongImpulseFactor;
            serialized.FindProperty("speedCap").floatValue = value.SpeedCap;
            serialized.FindProperty("correctTapCost").floatValue = value.CorrectTapCost;
            serialized.FindProperty("wrongTapCost").floatValue = value.WrongTapCost;
            serialized.FindProperty("burstRateThreshold").floatValue = value.BurstRateThreshold;
            serialized.FindProperty("burstExtraCost").floatValue = value.BurstExtraCost;
            serialized.FindProperty("activeDrainSpeedThreshold").floatValue = value.ActiveDrainSpeedThreshold;
            serialized.FindProperty("activeDrainPerSpeed").floatValue = value.ActiveDrainPerSpeed;
            serialized.FindProperty("restRegenPerSecond").floatValue = value.RestRegenPerSecond;
            serialized.FindProperty("fatigueThreshold").floatValue = value.FatigueThreshold;
            serialized.FindProperty("fatigueImpulseFactor").floatValue = value.FatigueImpulseFactor;
            serialized.FindProperty("fatigueSpeedCap").floatValue = value.FatigueSpeedCap;
            serialized.FindProperty("dragPerSecond").floatValue = value.DragPerSecond;
            serialized.FindProperty("distanceScale").floatValue = value.DistanceScale;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        static void Apply(ChallengeDefinition definition, ChallengeSpec spec)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = spec.Id;
            serialized.FindProperty("subject").intValue = (int)spec.Subject;
            serialized.FindProperty("kind").intValue = (int)spec.Kind;
            serialized.FindProperty("distance").floatValue = spec.Distance;
            serialized.FindProperty("timeLimit").floatValue = spec.TimeLimit;
            serialized.FindProperty("targetCount").intValue = spec.TargetCount;
            serialized.FindProperty("keeperEnabled").boolValue = spec.KeeperEnabled;
            serialized.FindProperty("difficulty").intValue = (int)spec.Difficulty;
            serialized.FindProperty("timingHelp").boolValue = spec.TimingHelp;
            serialized.FindProperty("objective").stringValue = spec.Objective;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
