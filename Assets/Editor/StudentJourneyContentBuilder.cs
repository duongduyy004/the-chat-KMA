using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
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
            public readonly int AttemptLimit;

            public ChallengeSpec(string id, SubjectId subject, ChallengeKind kind, float distance,
                float timeLimit, int targetCount, bool keeperEnabled, ChallengeDifficulty difficulty,
                bool timingHelp, string objective, int attemptLimit = 0)
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
                AttemptLimit = attemptLimit;
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
                    0f, 120f, 5, false, ChallengeDifficulty.Easy, true,
                    "Đạt năm điểm trước đối thủ trong 2 phút."),
                new ChallengeSpec("volleyball_exam", SubjectId.Volleyball, ChallengeKind.Exam,
                    0f, 0f, 5, false, ChallengeDifficulty.Normal, false,
                    "Đạt năm điểm trước đối thủ."),
                new ChallengeSpec("soccer_learn", SubjectId.Football, ChallengeKind.Learn,
                    0f, 0f, 3, false, ChallengeDifficulty.Normal, true,
                    "Ghi ba bàn; thủ môn tắt và số cú sút không giới hạn."),
                new ChallengeSpec("soccer_practice", SubjectId.Football, ChallengeKind.Practice,
                    0f, 0f, 2, true, ChallengeDifficulty.Normal, true,
                    "Ghi hai bàn trước thủ môn Normal trong tối đa 6 cú sút.", 6),
                new ChallengeSpec("soccer_exam", SubjectId.Football, ChallengeKind.Exam,
                    0f, 0f, 3, true, ChallengeDifficulty.Normal, false,
                    "Ghi ít nhất ba bàn sau đủ năm cú sút trước thủ môn Normal."),
                new ChallengeSpec("chess_final", SubjectId.Chess, ChallengeKind.Final,
                    0f, 90f, 2, false, ChallengeDifficulty.Normal, false,
                    "Chiếu hết trong 2 nước, tối đa 90 giây.")
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

        const string CharacterArtFolder = "Assets/_Project/Art/Characters";
        const string TanThu = "tan_thu", Mai = "mai_toang", AnhKhoaTren = "anh_khoa_tren", Co = "co_the_chat";

        static readonly (DialoguePose Pose, string Suffix)[] PoseFiles =
        {
            (DialoguePose.Idle, "idle"), (DialoguePose.Cheer, "cheer0"), (DialoguePose.Hurt, "hurt"),
            (DialoguePose.Jump, "jump"), (DialoguePose.Duck, "duck")
        };

        const string EmojiSourceFolder = "Assets/_Project/Art/Emoji/Source~";
        const string EmojiFolder = "Assets/_Project/Art/Emoji";
        const string EmojiAtlasPath = EmojiFolder + "/JourneyEmojiAtlas.png";
        const string EmojiAssetPath = ResourcesFolder + "/JourneyEmoji.asset";
        const int EmojiCell = 72;
        const int EmojiColumns = 5;

        [MenuItem("KMA/Journey/Build Emoji Sprites")]
        public static void BuildEmojiSpriteAsset()
        {
            EnsureFolder(EmojiFolder);
            EnsureFolder(ResourcesFolder);
            IReadOnlyList<string> names = DialogueEmoji.KnownNames;
            int rows = (names.Count + EmojiColumns - 1) / EmojiColumns;
            var atlas = new Texture2D(EmojiColumns * EmojiCell, rows * EmojiCell, TextureFormat.RGBA32, false);
            atlas.SetPixels32(new Color32[atlas.width * atlas.height]);
            var rects = new RectInt[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                string source = $"{EmojiSourceFolder}/{names[i]}.png";
                if (!File.Exists(source)) throw new InvalidOperationException("Missing emoji source: " + source);
                var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!image.LoadImage(File.ReadAllBytes(source)) || image.width != EmojiCell || image.height != EmojiCell)
                    throw new InvalidOperationException($"Emoji source must be a {EmojiCell}x{EmojiCell} PNG: {source}");
                int x = i % EmojiColumns * EmojiCell;
                int y = atlas.height - (i / EmojiColumns + 1) * EmojiCell;
                atlas.SetPixels32(x, y, EmojiCell, EmojiCell, image.GetPixels32());
                rects[i] = new RectInt(x, y, EmojiCell, EmojiCell);
                UnityEngine.Object.DestroyImmediate(image);
            }
            File.WriteAllBytes(EmojiAtlasPath, atlas.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(atlas);

            AssetDatabase.ImportAsset(EmojiAtlasPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(EmojiAtlasPath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.isReadable = false;
            importer.SaveAndReimport();
            Texture2D sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(EmojiAtlasPath);

            TMP_SpriteAsset asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(EmojiAssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(asset, EmojiAssetPath);
            }
            asset.spriteSheet = sheet;
            if (asset.material == null)
            {
                var material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "JourneyEmoji Material" };
                AssetDatabase.AddObjectToAsset(material, asset);
                asset.material = material;
            }
            asset.material.SetTexture(ShaderUtilities.ID_MainTex, sheet);

            // TMP treats an empty version as a legacy asset and rebuilds (empties) the tables on first
            // access, and throws while doing so. Stamp the version before touching any table.
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("m_Version").stringValue = "1.1.0";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            asset.spriteGlyphTable.Clear();
            asset.spriteCharacterTable.Clear();
            for (int i = 0; i < names.Count; i++)
            {
                var glyph = new TMP_SpriteGlyph((uint)i,
                    new GlyphMetrics(EmojiCell, EmojiCell, 0f, EmojiCell * .8f, EmojiCell),
                    new GlyphRect(rects[i].x, rects[i].y, EmojiCell, EmojiCell), 1f, 0);
                asset.spriteGlyphTable.Add(glyph);
                asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xFFFE, glyph) { name = names[i] });
            }
            asset.UpdateLookupTables();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA] Journey emoji sprite asset built.");
        }

        [MenuItem("KMA/Journey/Build Dialogue Library")]
        public static void BuildDialogues()
        {
            EnsureFolder(ResourcesFolder);
            BuildEmojiSpriteAsset();
            JourneyDialogueLibrary library = AssetDatabase.LoadAssetAtPath<JourneyDialogueLibrary>(DialoguePath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<JourneyDialogueLibrary>();
                AssetDatabase.CreateAsset(library, DialoguePath);
            }

            JourneyCharacter[] cast =
            {
                Character(TanThu, "Tân Thủ", "#FFC928", true, "MalePerson"),
                Character(Mai, "Mai Toang", "#FF8FB1", false, "FemalePerson"),
                Character(AnhKhoaTren, "Anh Khoá Trên", "#7FD1FF", false, "MaleAdventurer"),
                Character(Co, "Cô Thể Chất", "#B9F27C", false, "BossPE")
            };
            JourneyDialogueNode[] nodes =
            {
                Node("opening",
                    Line(AnhKhoaTren, DialoguePose.Cheer, "Chào tân binh :eyes: Sân trường dài bao nhiêu hả? Đợi buổi thể chất đầu tiên là biết liền :skull:"),
                    Line(Mai, DialoguePose.Hurt, "Ổng dọa tụi mình kìa :sob: Mới nhập học mà đã thấy mùi toang rồi đó.", "ÉT O ÉT!"),
                    Line(TanThu, DialoguePose.Idle, "Bình tĩnh. Qua môn trước, flex tính sau :sunglasses:"),
                    Line(Co, DialoguePose.Idle, "Lộ trình: chạy nước rút, rồi bóng chuyền, rồi bóng đá. Qua môn trước mới mở khóa môn sau nha các em :salute:")),
                Node("sprint_intro",
                    Line(Co, DialoguePose.Idle, "Khởi động bằng nhịp chân. Trái, phải, trái, phải. Đều như nhịp tim crush lúc nhắn \"seen\" :eyes:"),
                    Line(Mai, DialoguePose.Hurt, "Ét o ét :sob: 12 nhịp liền mạch, sai một phát là làm lại từ đầu đó bà con ơi!", "TOANG?!"),
                    Line(TanThu, DialoguePose.Idle, "Chân trái, chân phải thôi mà. Chạy như chưa từng được chạy :runner::dash:")),
                Node("sprint_exam",
                    Line(Co, DialoguePose.Idle, "Bài thi: 100 mét trong 14 giây. Giữ sức, đừng bung hết từ vạch xuất phát nha :fire:", "14 GIÂY"),
                    Line(TanThu, DialoguePose.Jump, "Tập rồi, giờ thi thôi. Đường đua ơi, chờ anh :100:")),
                Node("sprint_pass",
                    Line(Co, DialoguePose.Cheer, "Đạt! Nhịp chân ổn áp rồi đó. Cô công nhận em hơi bị đỉnh nóc :fire:", "ĐẠT!"),
                    Line(Mai, DialoguePose.Cheer, "Sân trường vẫn dài, nhưng giờ mình chạy hết nổi rồi :sob::tada:")),
                Node("volleyball_intro",
                    Line(Mai, DialoguePose.Cheer, "Bóng chuyền nè! Đỡ bóng đúng tầm trước, chuyền đẹp tính sau :volleyball:"),
                    Line(Co, DialoguePose.Idle, "Đỡ, chuyền, đập, đủ ba chạm. Bóng rơi xuống sân mình là mất điểm, không có chuyện \"chưa sẵn sàng\" đâu :eyes:"),
                    Line(TanThu, DialoguePose.Duck, "Ba đường bóng liền :scream: Thôi được, tay em đây, cứ phát bóng đi!", "CỨU!")),
                Node("volleyball_exam",
                    Line(Co, DialoguePose.Idle, "Thi đấu: ai ghi đủ 5 điểm trước thì thắng :fire:", "5 ĐIỂM"),
                    Line(Mai, DialoguePose.Jump, "Tui đỡ thật đẹp, ông lo cú đập nha. Đừng để tui phải ét o ét :sob:"),
                    Line(TanThu, DialoguePose.Cheer, "Combo đỡ, chuyền, đập, nhận về 5 điểm :100:")),
                Node("volleyball_pass",
                    Line(Co, DialoguePose.Cheer, "Đạt! Phối hợp mượt như wifi thư viện lúc 6 giờ sáng :volleyball:", "ĐẠT!"),
                    Line(TanThu, DialoguePose.Jump, "Cảm ơn đồng đội :salute: Hai môn rồi, môn cuối đâu, ra đây!")),
                Node("soccer_intro",
                    Line(Mai, DialoguePose.Idle, "Còn môn cuối thôi. Bình tĩnh, quả bóng không có deadline đâu :soccer:"),
                    Line(AnhKhoaTren, DialoguePose.Cheer, "Hồi anh thi, thủ môn cao hai mét, sân dốc lên trời :skull: Em giờ sướng chán.", "HỒI ĐÓ…"),
                    Line(Co, DialoguePose.Idle, "Đừng nghe ổng chém :clown: Tập hướng sút và lực sút trước. Lúc thi em có đúng 5 cú.")),
                Node("soccer_exam",
                    Line(Co, DialoguePose.Idle, "Thi: 5 cú sút, vào ít nhất 3 bàn là qua. Thủ môn hôm nay không nương tay đâu :eyes:", "5 CÚ"),
                    Line(TanThu, DialoguePose.Idle, "Nhắm chắc, lực vừa đủ, sút là vào. Chân này đã được khai quang :fire:")),
                Node("soccer_pass",
                    Line(Co, DialoguePose.Cheer, "Đạt ba môn! Còn đúng một bài kiểm tra cuối với cô nữa thôi :eyes:", "ĐẠT!"),
                    Line(Mai, DialoguePose.Cheer, "QUA RỒI :sob::sob: Còn bài cuối, ông ráng nốt nha!")),
                Node("chess_intro",
                    Line(Co, DialoguePose.Idle, "Bài cuối không chạy, không nhảy. Cô đặt một thế cờ, em chiếu hết trong hai nước :eyes:", "BÀI CUỐI"),
                    Line(TanThu, DialoguePose.Hurt, "Thể chất mà thi cờ vua ạ? Em tưởng cô chỉ biết thổi còi :sob:"),
                    Line(Co, DialoguePose.Idle, "Còi vẫn mang theo đây. Đi sai là cô thổi. Em có 90 giây, sai tối đa hai lần :fire:", "90 GIÂY")),
                Node("supplementary",
                    Line(Co, DialoguePose.Idle, "Chưa qua thì ôn lại bài luyện rồi thi tiếp. Phần đã đạt vẫn được giữ nguyên, không mất gì đâu :salute:"),
                    Line(Mai, DialoguePose.Hurt, "Toang nhẹ thôi, chưa toang hẳn :clown: Luyện đúng chỗ còn vướng rồi quẩy lại nha!", "HỒI SINH!")),
                Node("course_complete",
                    Line(Co, DialoguePose.Cheer, "Chúc mừng! Học phần Thể chất chính thức hoàn tất :tada:", "HOÀN THÀNH!"),
                    Line(TanThu, DialoguePose.Jump, "Từ tân binh run run thành tuyển thủ cấp trường. Flex được rồi đúng không? :sunglasses:"),
                    Line(AnhKhoaTren, DialoguePose.Cheer, "Được! Nhưng năm sau nhớ dọa khóa dưới y như anh nha :skull:"),
                    Line(Mai, DialoguePose.Cheer, "Hội qua môn Thể chất, điểm danh :100::tada: :muscle:"))
            };

            library.SetContent(cast, nodes);
            if (!library.Validate(out string error))
                throw new InvalidOperationException("Generated journey dialogue library is invalid: " + error);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA] Student journey dialogue library built.");
        }

        static JourneyCharacter Character(string id, string displayName, string hex, bool isPlayer, string spriteSet)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
                throw new InvalidOperationException("Invalid tag color " + hex);
            var poses = new List<JourneyPoseSprite>();
            foreach ((DialoguePose pose, string suffix) in PoseFiles)
            {
                string path = $"{CharacterArtFolder}/{spriteSet}/{spriteSet}_{suffix}.png";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) throw new InvalidOperationException("Missing dialogue sprite: " + path);
                poses.Add(new JourneyPoseSprite(pose, sprite));
            }
            return new JourneyCharacter(id, displayName, color, isPlayer, poses);
        }

        static JourneyDialogueNode Node(string id, params JourneyDialogueLine[] lines) =>
            new JourneyDialogueNode(id, new List<JourneyDialogueLine>(lines));

        static JourneyDialogueLine Line(string characterId, DialoguePose pose, string text, string sticker = "") =>
            new JourneyDialogueLine(characterId, pose, text, sticker);

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
            serialized.FindProperty("attemptLimit").intValue = spec.AttemptLimit;
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
