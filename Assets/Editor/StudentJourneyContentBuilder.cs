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
        const string DialoguePath = ResourcesFolder + "/JourneyDialogues.asset";

        readonly struct DialogueLineSpec
        {
            public readonly string Speaker, Text, Portrait;
            public DialogueLineSpec(string speaker, string text, string portrait)
            { Speaker = speaker; Text = text; Portrait = portrait; }
        }

        readonly struct DialogueSpec
        {
            public readonly string Id;
            public readonly DialogueLineSpec[] Lines;
            public DialogueSpec(string id, params DialogueLineSpec[] lines) { Id = id; Lines = lines; }
        }

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

        [MenuItem("KMA/Journey/Build Dialogue Library")]
        public static void BuildDialogues()
        {
            EnsureFolder(ResourcesFolder);
            JourneyDialogueLibrary library = AssetDatabase.LoadAssetAtPath<JourneyDialogueLibrary>(DialoguePath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<JourneyDialogueLibrary>();
                AssetDatabase.CreateAsset(library, DialoguePath);
            }

            string male = "Assets/_Project/Art/Characters/MalePerson/MalePerson_idle.png";
            string female = "Assets/_Project/Art/Characters/FemalePerson/FemalePerson_idle.png";
            string senior = "Assets/_Project/Art/Characters/MaleAdventurer/MaleAdventurer_idle.png";
            DialogueSpec[] specs =
            {
                new DialogueSpec("opening", new DialogueLineSpec("Anh/chị khóa trên", "Muốn biết sân trường dài bao nhiêu, cứ đợi buổi thể chất đầu tiên.", senior),
                    new DialogueLineSpec("Bạn cùng lớp", "Nghe dọa đủ rồi. Ra sân tập thử đã.", female),
                    new DialogueLineSpec("Tân sinh viên", "Qua môn trước. Ngầu tính sau.", male),
                    new DialogueLineSpec("Giảng viên", "Học lần lượt: chạy nước rút, bóng chuyền, rồi bóng đá. Đạt môn trước mới học môn sau.", senior)),
                new DialogueSpec("sprint_intro", new DialogueLineSpec("Giảng viên", "Bắt đầu bằng nhịp chân. Trái, phải luân phiên và giữ nhịp.", senior),
                    new DialogueLineSpec("Bạn cùng lớp", "Mười hai lần đúng liên tiếp. Sai thì bình tĩnh làm lại nhé.", female)),
                new DialogueSpec("sprint_exam", new DialogueLineSpec("Giảng viên", "Bài thi: hoàn thành 100 mét trong 14 giây.", senior),
                    new DialogueLineSpec("Tân sinh viên", "Mình đã tập rồi. Vào thi thôi!", male)),
                new DialogueSpec("sprint_pass", new DialogueLineSpec("Giảng viên", "Đạt. Nhịp chân của em đã ổn định hơn.", senior),
                    new DialogueLineSpec("Bạn cùng lớp", "Sân trường vẫn dài, nhưng giờ mình biết cách chạy rồi.", female)),
                new DialogueSpec("volleyball_intro", new DialogueLineSpec("Bạn cùng lớp", "Hai chân trước, đỡ bóng đúng tầm rồi mới tính đường chuyền.", female),
                    new DialogueLineSpec("Giảng viên", "Hãy giữ bóng trong pha của đội và phối hợp đủ ba chạm.", senior)),
                new DialogueSpec("volleyball_exam", new DialogueLineSpec("Giảng viên", "Thi đấu đến năm điểm trước đối thủ, giới hạn 120 giây.", senior),
                    new DialogueLineSpec("Bạn cùng lớp", "Mình sẽ đỡ thật đẹp. Cậu lo cú đập nhé!", female)),
                new DialogueSpec("volleyball_pass", new DialogueLineSpec("Giảng viên", "Đạt. Em đã phối hợp tốt giữa các chạm bóng.", senior),
                    new DialogueLineSpec("Tân sinh viên", "Cảm ơn đồng đội. Sang môn tiếp theo thôi!", male)),
                new DialogueSpec("soccer_intro", new DialogueLineSpec("Bạn cùng lớp", "Hai môn rồi. Giờ bình tĩnh, bóng không có deadline đâu.", female),
                    new DialogueLineSpec("Giảng viên", "Tập hướng và lực sút; khi thi, em có đúng năm cú.", senior)),
                new DialogueSpec("soccer_exam", new DialogueLineSpec("Giảng viên", "Thi đủ năm cú sút. Cần ít nhất ba bàn để đạt.", senior),
                    new DialogueLineSpec("Tân sinh viên", "Nhắm chắc, giữ lực vừa đủ. Mình sẵn sàng.", male)),
                new DialogueSpec("soccer_pass", new DialogueLineSpec("Giảng viên", "Đạt học phần. Em đã hoàn thành đủ ba môn.", senior),
                    new DialogueLineSpec("Bạn cùng lớp", "Qua rồi! Lần sau nhớ kể nhẹ tay cho khóa dưới nhé.", female)),
                new DialogueSpec("supplementary", new DialogueLineSpec("Giảng viên", "Ôn lại bài luyện, rồi vào thi tiếp. Các phần đã đạt vẫn được ghi nhận.", senior),
                    new DialogueLineSpec("Bạn cùng lớp", "Mình tập đúng phần còn vướng rồi thử lại nhé.", female)),
                new DialogueSpec("course_complete", new DialogueLineSpec("Giảng viên", "Chúc mừng. Học phần Thể chất đã hoàn tất.", senior),
                    new DialogueLineSpec("Tân sinh viên", "Từ hơi lo đến tự tin ra sân. Đáng nhớ thật.", male),
                    new DialogueLineSpec("Bạn cùng lớp", "Qua rồi! Lần sau nhớ kể nhẹ tay cho khóa dưới nhé.", female))
            };

            SerializedObject serialized = new SerializedObject(library);
            SerializedProperty nodes = serialized.FindProperty("nodes");
            nodes.arraySize = specs.Length;
            for (int i = 0; i < specs.Length; i++)
            {
                SerializedProperty node = nodes.GetArrayElementAtIndex(i);
                node.FindPropertyRelative("id").stringValue = specs[i].Id;
                SerializedProperty lines = node.FindPropertyRelative("lines");
                lines.arraySize = specs[i].Lines.Length;
                for (int lineIndex = 0; lineIndex < specs[i].Lines.Length; lineIndex++)
                {
                    DialogueLineSpec line = specs[i].Lines[lineIndex];
                    SerializedProperty serializedLine = lines.GetArrayElementAtIndex(lineIndex);
                    serializedLine.FindPropertyRelative("speakerRole").stringValue = line.Speaker;
                    serializedLine.FindPropertyRelative("text").stringValue = line.Text;
                    serializedLine.FindPropertyRelative("portrait").objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<Sprite>(line.Portrait);
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!library.Validate(out string error))
                throw new InvalidOperationException("Generated journey dialogue library is invalid: " + error);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA] Student journey dialogue library built.");
        }

        [MenuItem("KMA/Journey/Build All Journey Content")]
        public static void BuildAll()
        {
            BuildChallenges();
            BuildSprintBalance();
            BuildDialogues();
            ShellSceneAuthoring.Apply();
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
