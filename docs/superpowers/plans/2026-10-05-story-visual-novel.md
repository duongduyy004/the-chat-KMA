# Cốt truyện Visual Novel — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Viết lại 12 đoạn hội thoại hành trình theo giọng meme/teen code với dàn nhân vật có tên, và thay thẻ thoại bằng giao diện visual novel có chữ chạy, tư thế, sticker và emoji sprite Twemoji.

**Architecture:** Dữ liệu nằm trong `JourneyDialogueLibrary` (Progression): thêm cast `JourneyCharacter` và dòng thoại `characterId + pose + text + sticker`; `DialogueEmoji` đổi mã `:name:` thành thẻ `<sprite>` TMP. Builder Editor sinh cả library lẫn `TMP_SpriteAsset` từ PNG nguồn. `JourneyDialoguePresenter` (UI) được dựng lại bằng code theo bố cục visual novel, dùng `DialogueTypewriter` thuần C# cho chữ chạy; API công khai giữ nguyên để shell và test hành trình không đổi.

**Tech Stack:** Unity 6000.3.23f1, C#, ScriptableObject, uGUI + TextMeshPro (com.unity.ugui 2.0.0), Unity Test Framework 1.6.0.

**Spec:** `docs/superpowers/specs/2026-10-05-story-visual-novel-design.md` (đã duyệt).

## Global Constraints

- Không thêm package. Mọi file/asset mới trong `Assets/` phải commit kèm `.meta` (trừ thư mục kết thúc bằng `~`, Unity bỏ qua).
- Commit thẳng lên `master`; commit message **không** có dòng `Co-Authored-By`. Chỉ stage file của task đang làm.
- Batch-mode Unity cần đóng Unity Editor. Chạy test: `tools/run-unity-tests.sh <EditMode|PlayMode> <filter> <name>` (kết quả ở `Builds/TestResults/<name>.xml`, script in `result/total/passed/failed`).
- Unity: `UNITY="/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"`.
- Text hiển thị luôn qua `VietText.Fix`; label tạo qua `UiKit`/`VietTypography`; màu lấy từ `HomeMenuStyle` (Navy `#0B2A4A`, Gold `#FFC928`, GoldLight `#FFE066`).
- Cast id cố định: `tan_thu` (Tân Thủ, MalePerson, `#FFC928`, isPlayer), `mai_toang` (Mai Toang, FemalePerson, `#FF8FB1`), `anh_khoa_tren` (Anh Khoá Trên, MaleAdventurer, `#7FD1FF`), `co_the_chat` (Cô Thể Chất, FemaleAdventurer, `#B9F27C`).
- Tư thế: `Idle`→`_idle`, `Cheer`→`_cheer0`, `Hurt`→`_hit` (đã đổi ở Task 6: `_hurt` là ảnh quay lưng), `Jump`→`_jump`, `Duck`→`_duck` trong `Assets/_Project/Art/Characters/<Sprite>/<Sprite>_<suffix>.png`.
- 15 mã emoji theo thứ tự: `sob, skull, sunglasses, fire, scream, runner, dash, soccer, volleyball, eyes, clown, salute, 100, tada, muscle`.
- Node id giữ nguyên: `opening, sprint_intro, sprint_exam, sprint_pass, volleyball_intro, volleyball_exam, volleyball_pass, soccer_intro, soccer_exam, soccer_pass, supplementary, course_complete`; mỗi node 2–4 câu.
- Nút bỏ qua là GameObject tên `"BỎ QUA"` có `Button` (được `JourneyRuntimeDriver.SkipDialogues` dùng).
- Chữ chạy 45 ký tự/giây; slide 0,25 s; nảy 0,3 s (6% chiều cao); người nghe nhân màu 0,4 và lún 4%; sticker phóng 0→1,15→1 trong 0,25 s, nghiêng ±8°.

## Review Focus

1. Mở lại project (process mới) làm `TMP_SpriteAsset` tự "upgrade" và xoá sạch glyph nếu `m_Version` rỗng — Task 4 kiểm tra `m_Version == "1.1.0"` và đủ 15 glyph khi load từ Resources trong batch run mới.
2. Chạm khi câu cuối còn đang chạy chữ: phải chỉ hiện hết câu, không đóng node và không lưu "đã xem" — Task 5, test `LastLineFirstTapRevealsSecondTapClosesAndSaves`.
3. Lưu "đã xem" thất bại: overlay giữ mở, có thông báo, chạm lại thì lưu và đóng đúng một lần; chạm thêm khi đã đóng không gọi callback lần hai — Task 5, test `SaveFailureKeepsOverlayAndRetryClosesExactlyOnce`.
4. Dấu `:` bình thường trong câu ("Lộ trình: chạy…", "Thi: 5 cú") và mã dính nhau `:sob::sob:` hoặc mã lạ `:xyz:` cạnh mã đúng — Task 1, test `ExpandLeavesOrdinaryColonsAndUnknownCodesAlone`.
5. Node mà người chơi không nói (`sprint_pass`) vẫn hiện Tân Thủ bên phải ở trạng thái nghe; node đổi người nói bên trái giữa chừng (`soccer_intro`: Mai → Anh Khoá Trên → Cô) cuối cùng hiện đúng nhân vật — Task 5, tests `PlayerStaysRightAsListenerWhenSilent` và `LeftSlotSwapsToEachNewSpeaker`.

## File Structure

| File | Trách nhiệm | Task |
|---|---|---|
| `Assets/_Project/Scripts/Progression/Journey/DialogueEmoji.cs` (mới) | Danh sách mã emoji, `Expand`, `FindCodes`, `IsKnown` | 1 |
| `Assets/_Project/Scripts/UI/DialogueTypewriter.cs` (mới) | Đếm ký tự hiện theo thời gian | 2 |
| `Assets/_Project/Scripts/Progression/Journey/JourneyDialogueLibrary.cs` | `DialoguePose`, `JourneyPoseSprite`, `JourneyCharacter`, dòng thoại mới, cast, `Validate` | 3 |
| `Assets/Editor/StudentJourneyContentBuilder.cs` | Sinh cast + 12 node (Task 3), sinh atlas + `TMP_SpriteAsset` (Task 4) | 3, 4 |
| `Assets/_Project/Resources/Journey/JourneyDialogues.asset` | Dữ liệu sinh ra | 3 |
| `Assets/_Project/Art/Emoji/Source~/*.png` (mới, không import) | 15 PNG Twemoji 72×72 nguồn | 4 |
| `Assets/_Project/Art/Emoji/JourneyEmojiAtlas.png` (mới, sinh ra) | Atlas 360×216 | 4 |
| `Assets/_Project/Resources/Journey/JourneyEmoji.asset` (mới, sinh ra) | `TMP_SpriteAsset` | 4 |
| `Assets/_Project/CREDITS.md` | Credit Twemoji CC-BY 4.0 | 4 |
| `Assets/_Project/Scripts/UI/JourneyDialoguePresenter.cs` | Thích nghi dữ liệu mới (Task 3), viết lại visual novel (Task 5) | 3, 5 |
| `Assets/_Project/Scripts/Shell/S5ShellSceneController.cs`, `Assets/_Project/Scenes/Map.unity` | Bỏ `instructorPortrait` | 3 |
| `Assets/Editor/PlayModeScreenshot.cs`, `tools/qa-screenshot.sh` | Tham số QA mở node thoại | 6 |
| Tests | xem từng task | 1–6 |

Thứ tự: 1 → 2 → 3 → 4 → 5 → 6. Task 1, 2 độc lập; Task 3 dùng Task 1; Task 4 dùng Task 1 và 3; Task 5 dùng 1–4; Task 6 dùng 5.

## Execution Preparation

- [ ] Đọc spec và plan; `git status` sạch, nhánh `master`; đóng Unity Editor trước các bước batch.

---

### Task 1: DialogueEmoji

**Files:**
- Create: `Assets/_Project/Scripts/Progression/Journey/DialogueEmoji.cs`
- Test: `Assets/Tests/EditMode/Progression/DialogueEmojiTests.cs`

**Interfaces:**
- Consumes: không.
- Produces (namespace `KMA.Gameplay`, assembly `KMA.Gameplay.Progression`):
  - `public static IReadOnlyList<string> DialogueEmoji.KnownNames` — 15 mã theo thứ tự Global Constraints.
  - `public static bool DialogueEmoji.IsKnown(string name)`
  - `public static IEnumerable<string> DialogueEmoji.FindCodes(string text)` — tên trong mọi `:name:` (`[a-z0-9_]+`), kể cả mã lạ.
  - `public static string DialogueEmoji.Expand(string text)` — mã biết → `<sprite name="name">`, còn lại giữ nguyên.

- [ ] **Step 1: Viết test thất bại**

```csharp
using System.Linq;
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class DialogueEmojiTests
    {
        [Test]
        public void KnownNamesListTheFifteenScriptEmojiInOrder()
        {
            Assert.That(DialogueEmoji.KnownNames, Is.EqualTo(new[]
            {
                "sob", "skull", "sunglasses", "fire", "scream", "runner", "dash", "soccer", "volleyball",
                "eyes", "clown", "salute", "100", "tada", "muscle"
            }));
            Assert.That(DialogueEmoji.IsKnown("sob"), Is.True);
            Assert.That(DialogueEmoji.IsKnown("hot"), Is.False);
            Assert.That(DialogueEmoji.IsKnown(null), Is.False);
        }

        [Test]
        public void ExpandTurnsKnownCodesIntoSpriteTags()
        {
            Assert.That(DialogueEmoji.Expand("Toang :sob:"), Is.EqualTo("Toang <sprite name=\"sob\">"));
            Assert.That(DialogueEmoji.Expand(":runner::dash:"),
                Is.EqualTo("<sprite name=\"runner\"><sprite name=\"dash\">"));
            Assert.That(DialogueEmoji.Expand("QUA RỒI :sob::sob: nha"),
                Is.EqualTo("QUA RỒI <sprite name=\"sob\"><sprite name=\"sob\"> nha"));
            Assert.That(DialogueEmoji.Expand("điểm danh :100:"), Is.EqualTo("điểm danh <sprite name=\"100\">"));
        }

        [Test]
        public void ExpandLeavesOrdinaryColonsAndUnknownCodesAlone()
        {
            Assert.That(DialogueEmoji.Expand("Lộ trình: chạy, rồi bóng"), Is.EqualTo("Lộ trình: chạy, rồi bóng"));
            Assert.That(DialogueEmoji.Expand("Thi: 5 cú sút"), Is.EqualTo("Thi: 5 cú sút"));
            Assert.That(DialogueEmoji.Expand("lạ :xyz: rồi :fire:"),
                Is.EqualTo("lạ :xyz: rồi <sprite name=\"fire\">"));
            Assert.That(DialogueEmoji.Expand(":xyz::fire:"), Is.EqualTo(":xyz:<sprite name=\"fire\">"));
            Assert.That(DialogueEmoji.Expand(""), Is.EqualTo(""));
            Assert.That(DialogueEmoji.Expand(null), Is.Null);
        }

        [Test]
        public void FindCodesReturnsEveryCodeIncludingUnknownOnes()
        {
            Assert.That(DialogueEmoji.FindCodes("a :sob: b :xyz::fire: Lộ trình: x").ToArray(),
                Is.EqualTo(new[] { "sob", "xyz", "fire" }));
            Assert.That(DialogueEmoji.FindCodes(null), Is.Empty);
        }
    }
}
```

- [ ] **Step 2: Chạy, xác nhận đỏ**

Run: `tools/run-unity-tests.sh EditMode KMA.Tests.Gameplay.Progression.DialogueEmojiTests emoji-red`
Expected: không có kết quả test, log có `error CS0103: The name 'DialogueEmoji' does not exist`.

- [ ] **Step 3: Cài đặt**

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KMA.Gameplay
{
    /// Emoji shortcodes (":sob:") used in journey dialogue, expanded to TMP sprite tags.
    public static class DialogueEmoji
    {
        static readonly string[] Names =
        {
            "sob", "skull", "sunglasses", "fire", "scream", "runner", "dash", "soccer", "volleyball",
            "eyes", "clown", "salute", "100", "tada", "muscle"
        };
        static readonly HashSet<string> Known = new HashSet<string>(Names, StringComparer.Ordinal);
        static readonly Regex Code = new Regex(":([a-z0-9_]+):", RegexOptions.CultureInvariant);

        public static IReadOnlyList<string> KnownNames => Names;

        public static bool IsKnown(string name) => name != null && Known.Contains(name);

        public static IEnumerable<string> FindCodes(string text)
        {
            if (string.IsNullOrEmpty(text)) yield break;
            foreach (Match match in Code.Matches(text))
                yield return match.Groups[1].Value;
        }

        public static string Expand(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return Code.Replace(text, match =>
            {
                string name = match.Groups[1].Value;
                return Known.Contains(name) ? $"<sprite name=\"{name}\">" : match.Value;
            });
        }
    }
}
```

Lưu ý `":xyz::fire:"`: regex khớp `:xyz:` trước (giữ nguyên), rồi `:fire:` — đúng kỳ vọng test.

- [ ] **Step 4: Chạy, xác nhận xanh**

Run: `tools/run-unity-tests.sh EditMode KMA.Tests.Gameplay.Progression.DialogueEmojiTests emoji`
Expected: `result=Passed`, `failed=0`, `passed=4`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Progression/Journey/DialogueEmoji.cs Assets/_Project/Scripts/Progression/Journey/DialogueEmoji.cs.meta Assets/Tests/EditMode/Progression/DialogueEmojiTests.cs Assets/Tests/EditMode/Progression/DialogueEmojiTests.cs.meta
git commit -m "feat(story): expand dialogue emoji shortcodes to sprite tags"
```

---

### Task 2: DialogueTypewriter

**Files:**
- Create: `Assets/_Project/Scripts/UI/DialogueTypewriter.cs`
- Test: `Assets/Tests/EditMode/Presentation/DialogueTypewriterTests.cs`

**Interfaces:**
- Consumes: không.
- Produces (namespace `KMA.Gameplay.UI`): `public sealed class DialogueTypewriter` với `DialogueTypewriter(float charactersPerSecond = 45f)` (≤0 → `ArgumentOutOfRangeException`), `void Begin(int totalCharacters)`, `void Tick(float deltaTime)`, `void Complete()`, `int VisibleCharacters`, `bool IsDone`.

- [ ] **Step 1: Viết test thất bại**

```csharp
using System;
using KMA.Gameplay.UI;
using NUnit.Framework;

namespace KMA.Tests.Presentation
{
    public sealed class DialogueTypewriterTests
    {
        [Test]
        public void RevealsCharactersOverTimeWithoutPassingTheTotal()
        {
            var writer = new DialogueTypewriter(10f);
            writer.Begin(25);
            Assert.That(writer.VisibleCharacters, Is.Zero);
            Assert.That(writer.IsDone, Is.False);
            writer.Tick(.55f);
            Assert.That(writer.VisibleCharacters, Is.EqualTo(5));
            writer.Tick(1f);
            Assert.That(writer.VisibleCharacters, Is.EqualTo(15));
            writer.Tick(10f);
            Assert.That(writer.VisibleCharacters, Is.EqualTo(25));
            Assert.That(writer.IsDone, Is.True);
        }

        [Test]
        public void CompleteShowsEverythingAndBeginRestarts()
        {
            var writer = new DialogueTypewriter();
            writer.Begin(80);
            writer.Complete();
            Assert.That(writer.VisibleCharacters, Is.EqualTo(80));
            Assert.That(writer.IsDone, Is.True);
            writer.Begin(12);
            Assert.That(writer.VisibleCharacters, Is.Zero);
            Assert.That(writer.IsDone, Is.False);
        }

        [Test]
        public void EmptyLineIsDoneImmediatelyAndBadInputIsIgnoredOrRejected()
        {
            var writer = new DialogueTypewriter();
            writer.Begin(0);
            Assert.That(writer.IsDone, Is.True);
            writer.Begin(10);
            writer.Tick(-1f);
            Assert.That(writer.VisibleCharacters, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => new DialogueTypewriter(0f));
        }
    }
}
```

- [ ] **Step 2: Chạy, xác nhận đỏ**

Run: `tools/run-unity-tests.sh EditMode KMA.Tests.Presentation.DialogueTypewriterTests typewriter-red`
Expected: log có `error CS0246: The type or namespace name 'DialogueTypewriter' could not be found`.

- [ ] **Step 3: Cài đặt**

```csharp
using System;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    /// Counts how many characters of a dialogue line are visible as time passes.
    public sealed class DialogueTypewriter
    {
        readonly float charactersPerSecond;
        float elapsed;
        int total;

        public DialogueTypewriter(float charactersPerSecond = 45f)
        {
            if (charactersPerSecond <= 0f)
                throw new ArgumentOutOfRangeException(nameof(charactersPerSecond));
            this.charactersPerSecond = charactersPerSecond;
        }

        public int VisibleCharacters { get; private set; }
        public bool IsDone => VisibleCharacters >= total;

        public void Begin(int totalCharacters)
        {
            total = Mathf.Max(0, totalCharacters);
            elapsed = 0f;
            VisibleCharacters = 0;
        }

        public void Tick(float deltaTime)
        {
            if (IsDone || deltaTime <= 0f) return;
            elapsed += deltaTime;
            VisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(elapsed * charactersPerSecond));
        }

        public void Complete() => VisibleCharacters = total;
    }
}
```

- [ ] **Step 4: Chạy, xác nhận xanh**

Run: `tools/run-unity-tests.sh EditMode KMA.Tests.Presentation.DialogueTypewriterTests typewriter`
Expected: `result=Passed`, `passed=3`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/UI/DialogueTypewriter.cs Assets/_Project/Scripts/UI/DialogueTypewriter.cs.meta Assets/Tests/EditMode/Presentation/DialogueTypewriterTests.cs Assets/Tests/EditMode/Presentation/DialogueTypewriterTests.cs.meta
git commit -m "feat(story): add dialogue typewriter timing"
```

---

### Task 3: Dàn nhân vật, dữ liệu thoại mới và kịch bản

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/Journey/JourneyDialogueLibrary.cs` (viết lại toàn bộ)
- Modify: `Assets/Editor/StudentJourneyContentBuilder.cs:16-27` (spec structs) và `:176-246` (`BuildDialogues`)
- Modify: `Assets/_Project/Scripts/UI/JourneyDialoguePresenter.cs` (chỉ phần dữ liệu/portrait)
- Modify: `Assets/_Project/Scripts/Shell/S5ShellSceneController.cs:20,65`
- Modify: `Assets/_Project/Scenes/Map.unity:4388` (xoá dòng `instructorPortrait:`)
- Regenerate: `Assets/_Project/Resources/Journey/JourneyDialogues.asset`
- Test: `Assets/Tests/EditMode/Progression/JourneyDialogueDataTests.cs`, Create `Assets/Tests/EditMode/Progression/JourneyDialogueValidationTests.cs`

**Interfaces:**
- Consumes: `DialogueEmoji.FindCodes`, `DialogueEmoji.IsKnown` (Task 1).
- Produces (namespace `KMA.Gameplay`):
  - `public enum DialoguePose { Idle, Cheer, Hurt, Jump, Duck }`
  - `JourneyPoseSprite(DialoguePose pose, Sprite sprite)`; `Pose`, `Sprite`.
  - `JourneyCharacter(string id, string displayName, Color tagColor, bool isPlayer, IEnumerable<JourneyPoseSprite> poses)`; `Id`, `DisplayName`, `TagColor`, `IsPlayer`, `Sprite GetPose(DialoguePose)` (thiếu → Idle → null), `bool HasPose(DialoguePose)`.
  - `JourneyDialogueLine(string characterId, DialoguePose pose, string text, string sticker = "")`; `CharacterId`, `Pose`, `Text`, `Sticker`.
  - `JourneyDialogueLibrary`: `IReadOnlyList<JourneyCharacter> Cast`, `IReadOnlyList<JourneyDialogueNode> Nodes`, `JourneyCharacter Player`, `JourneyCharacter GetCharacter(string id)` (không có → `KeyNotFoundException`), `void SetContent(IEnumerable<JourneyCharacter>, IEnumerable<JourneyDialogueNode>)`, `Get`, `LoadDefault`, `Validate` như cũ.
  - `JourneyDialoguePresenter.Configure(JourneyDialogueLibrary, Func<string,bool>, Sprite sharedBackground = null)` và `ShowJourney(GameSession, GameManager, Sprite sharedBackground = null)` — bỏ tham số portrait.

- [ ] **Step 1: Viết test thất bại — validation**

`Assets/Tests/EditMode/Progression/JourneyDialogueValidationTests.cs`:

```csharp
using System.Collections.Generic;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class JourneyDialogueValidationTests
    {
        readonly List<Object> created = new List<Object>();

        Sprite MakeSprite()
        {
            var texture = new Texture2D(4, 4);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f));
            created.Add(texture);
            created.Add(sprite);
            return sprite;
        }

        JourneyCharacter Character(string id, bool player, params DialoguePose[] poses)
        {
            var list = new List<JourneyPoseSprite>();
            foreach (DialoguePose pose in poses) list.Add(new JourneyPoseSprite(pose, MakeSprite()));
            return new JourneyCharacter(id, id.ToUpperInvariant(), Color.white, player, list);
        }

        JourneyDialogueLibrary Library(JourneyCharacter[] cast, params JourneyDialogueLine[] lines)
        {
            var library = ScriptableObject.CreateInstance<JourneyDialogueLibrary>();
            created.Add(library);
            library.SetContent(cast, new[] { new JourneyDialogueNode("n", new List<JourneyDialogueLine>(lines)) });
            return library;
        }

        JourneyCharacter[] DefaultCast() => new[]
        {
            Character("me", true, DialoguePose.Idle),
            Character("friend", false, DialoguePose.Idle, DialoguePose.Cheer)
        };

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in created) if (item != null) Object.DestroyImmediate(item);
            created.Clear();
        }

        [Test]
        public void ValidLibraryPassesAndResolvesCharacters()
        {
            JourneyDialogueLibrary library = Library(DefaultCast(),
                new JourneyDialogueLine("friend", DialoguePose.Cheer, "Chào :sob:", "WOW"),
                new JourneyDialogueLine("me", DialoguePose.Idle, "Ừ"));
            Assert.That(library.Validate(out string error), Is.True, error);
            Assert.That(library.Player.Id, Is.EqualTo("me"));
            Assert.That(library.GetCharacter("friend").DisplayName, Is.EqualTo("FRIEND"));
            Assert.Throws<KeyNotFoundException>(() => library.GetCharacter("ghost"));
        }

        [Test]
        public void UnknownCharacterFailsValidation()
        {
            JourneyDialogueLibrary library = Library(DefaultCast(),
                new JourneyDialogueLine("ghost", DialoguePose.Idle, "Boo"),
                new JourneyDialogueLine("me", DialoguePose.Idle, "Á"));
            Assert.That(library.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain("ghost"));
        }

        [Test]
        public void PoseWithoutSpriteFailsValidation()
        {
            JourneyDialogueLibrary library = Library(DefaultCast(),
                new JourneyDialogueLine("friend", DialoguePose.Duck, "Né"),
                new JourneyDialogueLine("me", DialoguePose.Idle, "Ừ"));
            Assert.That(library.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain("Duck"));
        }

        [Test]
        public void UnknownEmojiFailsValidation()
        {
            JourneyDialogueLibrary library = Library(DefaultCast(),
                new JourneyDialogueLine("friend", DialoguePose.Idle, "Nóng :hot:"),
                new JourneyDialogueLine("me", DialoguePose.Idle, "Ừ"));
            Assert.That(library.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain(":hot:"));
        }

        [Test]
        public void CastMustHaveExactlyOnePlayer()
        {
            JourneyDialogueLibrary library = Library(new[]
                {
                    Character("a", false, DialoguePose.Idle),
                    Character("b", false, DialoguePose.Idle)
                },
                new JourneyDialogueLine("a", DialoguePose.Idle, "x"),
                new JourneyDialogueLine("b", DialoguePose.Idle, "y"));
            Assert.That(library.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain("player"));
        }

        [Test]
        public void MissingPoseFallsBackToIdle()
        {
            JourneyCharacter friend = Character("friend", false, DialoguePose.Idle);
            Assert.That(friend.GetPose(DialoguePose.Jump), Is.SameAs(friend.GetPose(DialoguePose.Idle)));
            Assert.That(friend.HasPose(DialoguePose.Jump), Is.False);
            var empty = new JourneyCharacter("x", "X", Color.white, false, new JourneyPoseSprite[0]);
            Assert.That(empty.GetPose(DialoguePose.Idle), Is.Null);
        }
    }
}
```

- [ ] **Step 2: Viết test thất bại — dữ liệu mặc định**

Thay toàn bộ `DefaultDialogueLibraryContainsShortNodesForEveryMilestone` trong `JourneyDialogueDataTests.cs` (giữ nguyên test `SeenMarkersAreSavedAndRestoredWithJourneyProgress`):

```csharp
        static readonly string[] NodeIds = { "opening", "sprint_intro", "sprint_exam", "sprint_pass",
            "volleyball_intro", "volleyball_exam", "volleyball_pass", "soccer_intro", "soccer_exam",
            "soccer_pass", "supplementary", "course_complete" };

        [Test]
        public void DefaultDialogueLibraryContainsShortNodesForEveryMilestone()
        {
            JourneyDialogueLibrary library = JourneyDialogueLibrary.LoadDefault();
            Assert.That(library.Validate(out string error), Is.True, error);
            Assert.That(library.Nodes.Select(node => node.Id), Is.EquivalentTo(NodeIds));
            foreach (string id in NodeIds)
            {
                Assert.That(library.Get(id).Lines.Count, Is.InRange(2, 4), id);
                foreach (JourneyDialogueLine line in library.Get(id).Lines)
                    Assert.That(library.GetCharacter(line.CharacterId).GetPose(line.Pose), Is.Not.Null,
                        $"{id}/{line.CharacterId}/{line.Pose}");
            }
        }

        [Test]
        public void DefaultCastIsTheFourNamedCharacters()
        {
            JourneyDialogueLibrary library = JourneyDialogueLibrary.LoadDefault();
            Assert.That(library.Cast.Select(c => c.Id + "=" + c.DisplayName), Is.EquivalentTo(new[]
            {
                "tan_thu=Tân Thủ", "mai_toang=Mai Toang", "anh_khoa_tren=Anh Khoá Trên", "co_the_chat=Cô Thể Chất"
            }));
            Assert.That(library.Player.Id, Is.EqualTo("tan_thu"));
            foreach (JourneyCharacter character in library.Cast)
                foreach (DialoguePose pose in System.Enum.GetValues(typeof(DialoguePose)))
                    Assert.That(character.HasPose(pose), Is.True, $"{character.Id}/{pose}");
        }

        [Test]
        public void CourseCompleteNoLongerRepeatsTheSoccerPassLine()
        {
            JourneyDialogueLibrary library = JourneyDialogueLibrary.LoadDefault();
            var soccerPass = library.Get("soccer_pass").Lines.Select(line => line.Text).ToList();
            foreach (JourneyDialogueLine line in library.Get("course_complete").Lines)
                Assert.That(soccerPass, Does.Not.Contain(line.Text));
        }
```

Thêm `using System.Linq;` ở đầu file.

- [ ] **Step 3: Chạy, xác nhận đỏ**

Run: `tools/run-unity-tests.sh EditMode KMA.Tests.Gameplay.Progression progression-dialogue-red`
Expected: log có `error CS0246` cho `JourneyCharacter`/`DialoguePose`.

- [ ] **Step 4: Viết lại `JourneyDialogueLibrary.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum DialoguePose { Idle, Cheer, Hurt, Jump, Duck }

    [Serializable]
    public sealed class JourneyPoseSprite
    {
        [SerializeField] DialoguePose pose;
        [SerializeField] Sprite sprite;

        public DialoguePose Pose => pose;
        public Sprite Sprite => sprite;

        public JourneyPoseSprite(DialoguePose pose, Sprite sprite)
        {
            this.pose = pose;
            this.sprite = sprite;
        }
    }

    [Serializable]
    public sealed class JourneyCharacter
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField] Color tagColor = Color.white;
        [SerializeField] bool isPlayer;
        [SerializeField] List<JourneyPoseSprite> poses = new List<JourneyPoseSprite>();

        public string Id => id;
        public string DisplayName => displayName;
        public Color TagColor => tagColor;
        public bool IsPlayer => isPlayer;

        public JourneyCharacter(string id, string displayName, Color tagColor, bool isPlayer,
            IEnumerable<JourneyPoseSprite> poses)
        {
            this.id = id;
            this.displayName = displayName;
            this.tagColor = tagColor;
            this.isPlayer = isPlayer;
            this.poses = poses == null ? new List<JourneyPoseSprite>() : poses.ToList();
        }

        public bool HasPose(DialoguePose pose) => Find(pose) != null;

        /// The sprite for a pose, falling back to Idle, or null when neither exists.
        public Sprite GetPose(DialoguePose pose) => Find(pose) ?? Find(DialoguePose.Idle);

        Sprite Find(DialoguePose pose)
        {
            if (poses == null) return null;
            foreach (JourneyPoseSprite entry in poses)
                if (entry != null && entry.Pose == pose && entry.Sprite != null) return entry.Sprite;
            return null;
        }
    }

    [Serializable]
    public sealed class JourneyDialogueLine
    {
        [SerializeField] string characterId;
        [SerializeField] DialoguePose pose;
        [SerializeField, TextArea] string text;
        [SerializeField] string sticker;

        public string CharacterId => characterId;
        public DialoguePose Pose => pose;
        public string Text => text;
        public string Sticker => sticker;

        public JourneyDialogueLine(string characterId, DialoguePose pose, string text, string sticker = "")
        {
            this.characterId = characterId;
            this.pose = pose;
            this.text = text;
            this.sticker = sticker ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class JourneyDialogueNode
    {
        [SerializeField] string id;
        [SerializeField] List<JourneyDialogueLine> lines = new List<JourneyDialogueLine>();

        public string Id => id;
        public IReadOnlyList<JourneyDialogueLine> Lines => lines;

        public JourneyDialogueNode(string id, List<JourneyDialogueLine> lines)
        {
            this.id = id;
            this.lines = lines ?? new List<JourneyDialogueLine>();
        }
    }

    [CreateAssetMenu(menuName = "KMA/Journey/Dialogue Library", fileName = "JourneyDialogues")]
    public sealed class JourneyDialogueLibrary : ScriptableObject
    {
        [SerializeField] List<JourneyCharacter> cast = new List<JourneyCharacter>();
        [SerializeField] List<JourneyDialogueNode> nodes = new List<JourneyDialogueNode>();
        Dictionary<string, JourneyDialogueNode> byId;
        Dictionary<string, JourneyCharacter> castById;

        public IReadOnlyList<JourneyCharacter> Cast => cast;
        public IReadOnlyList<JourneyDialogueNode> Nodes => nodes;
        public JourneyCharacter Player => cast.FirstOrDefault(character => character != null && character.IsPlayer);

        public static JourneyDialogueLibrary LoadDefault() =>
            Resources.Load<JourneyDialogueLibrary>("Journey/JourneyDialogues");

        public void SetContent(IEnumerable<JourneyCharacter> characters, IEnumerable<JourneyDialogueNode> dialogueNodes)
        {
            cast = characters == null ? new List<JourneyCharacter>() : characters.ToList();
            nodes = dialogueNodes == null ? new List<JourneyDialogueNode>() : dialogueNodes.ToList();
            byId = null;
            castById = null;
        }

        public JourneyDialogueNode Get(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) throw new ArgumentException("A dialogue node ID is required.", nameof(nodeId));
            byId ??= nodes.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Id))
                .GroupBy(x => x.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            if (!byId.TryGetValue(nodeId, out JourneyDialogueNode node))
                throw new KeyNotFoundException($"Journey dialogue '{nodeId}' was not found.");
            return node;
        }

        public JourneyCharacter GetCharacter(string characterId)
        {
            castById ??= cast.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Id))
                .GroupBy(x => x.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            if (characterId == null || !castById.TryGetValue(characterId, out JourneyCharacter character))
                throw new KeyNotFoundException($"Journey character '{characterId}' was not found.");
            return character;
        }

        public bool Validate(out string error)
        {
            error = null;
            if (nodes == null || nodes.Count == 0) { error = "Dialogue library has no nodes."; return false; }
            if (cast == null || cast.Count == 0) { error = "Dialogue library has no cast."; return false; }
            var characters = new Dictionary<string, JourneyCharacter>(StringComparer.Ordinal);
            foreach (JourneyCharacter character in cast)
            {
                if (character == null || string.IsNullOrWhiteSpace(character.Id) || !characters.TryAdd(character.Id, character))
                { error = "Character IDs must be present and unique."; return false; }
            }
            if (cast.Count(character => character.IsPlayer) != 1)
            { error = "Dialogue cast must contain exactly one player."; return false; }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (JourneyDialogueNode node in nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id) || !seen.Add(node.Id))
                { error = "Dialogue IDs must be present and unique."; return false; }
                if (node.Lines == null || node.Lines.Count is < 2 or > 4 ||
                    node.Lines.Any(line => line == null || string.IsNullOrWhiteSpace(line.CharacterId) ||
                                           string.IsNullOrWhiteSpace(line.Text)))
                { error = $"Dialogue '{node.Id}' must contain two to four complete lines."; return false; }
                foreach (JourneyDialogueLine line in node.Lines)
                {
                    if (!characters.TryGetValue(line.CharacterId, out JourneyCharacter speaker))
                    { error = $"Dialogue '{node.Id}' uses unknown character '{line.CharacterId}'."; return false; }
                    if (!speaker.HasPose(line.Pose))
                    { error = $"Character '{speaker.Id}' has no sprite for pose {line.Pose}."; return false; }
                    foreach (string code in DialogueEmoji.FindCodes(line.Text))
                        if (!DialogueEmoji.IsKnown(code))
                        { error = $"Dialogue '{node.Id}' uses unknown emoji ':{code}:'."; return false; }
                }
            }
            return true;
        }
    }
}
```

- [ ] **Step 5: Viết lại `BuildDialogues` trong builder**

Trong `StudentJourneyContentBuilder.cs`: xoá hai struct `DialogueLineSpec`, `DialogueSpec` (dòng 16–27); thêm `using System.Collections.Generic;`; thay toàn bộ thân `BuildDialogues()` (dòng 176–246) bằng:

```csharp
        const string CharacterArtFolder = "Assets/_Project/Art/Characters";
        const string TanThu = "tan_thu", Mai = "mai_toang", AnhKhoaTren = "anh_khoa_tren", Co = "co_the_chat";

        static readonly (DialoguePose Pose, string Suffix)[] PoseFiles =
        {
            (DialoguePose.Idle, "idle"), (DialoguePose.Cheer, "cheer0"), (DialoguePose.Hurt, "hurt"),
            (DialoguePose.Jump, "jump"), (DialoguePose.Duck, "duck")
        };

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

            JourneyCharacter[] cast =
            {
                Character(TanThu, "Tân Thủ", "#FFC928", true, "MalePerson"),
                Character(Mai, "Mai Toang", "#FF8FB1", false, "FemalePerson"),
                Character(AnhKhoaTren, "Anh Khoá Trên", "#7FD1FF", false, "MaleAdventurer"),
                Character(Co, "Cô Thể Chất", "#B9F27C", false, "FemaleAdventurer")
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
                    Line(Co, DialoguePose.Idle, "Thi đấu: ghi đủ 5 điểm trước đối thủ, trong 120 giây :fire:", "120 GIÂY"),
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
                    Line(Co, DialoguePose.Cheer, "Đạt học phần! Ba môn, ba lần qua. Cô hơi bị tự hào đó :tada:", "ĐẠT!"),
                    Line(Mai, DialoguePose.Cheer, "QUA RỒI :sob::sob: Từ \"toang\" lên \"đỉnh nóc\" trong một học kỳ!")),
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
```

- [ ] **Step 6: Thích nghi presenter, shell và scene (tạm, trước khi viết lại ở Task 5)**

Trong `JourneyDialoguePresenter.cs`:
- Xoá field `Sprite instructorPortrait;`.
- `Configure` thành:

```csharp
        public void Configure(JourneyDialogueLibrary dialogueLibrary, Func<string, bool> saveSeen,
            Sprite sharedBackground = null)
        {
            library = dialogueLibrary;
            persistSeen = saveSeen;
            EnsureView();
            if (backgroundImage != null && sharedBackground != null)
                backgroundImage.sprite = sharedBackground;
        }
```

- `ShowJourney(GameSession session, GameManager manager, Sprite sharedBackground = null)` và dòng gọi `Configure(JourneyDialogueLibrary.LoadDefault(), key => manager.TryMarkJourneyDialogueSeen(key, out _), sharedBackground);`.
- `RenderLine` thành:

```csharp
        void RenderLine()
        {
            JourneyDialogueLine line = activeLines[lineIndex];
            JourneyCharacter character = library.GetCharacter(line.CharacterId);
            speaker.text = VietText.Fix(character.DisplayName);
            body.text = VietText.Fix(line.Text);
            portrait.sprite = character.GetPose(line.Pose);
            portrait.enabled = portrait.sprite != null;
            continueLabel.text = VietText.Fix(lineIndex + 1 >= activeLines.Count ? "ĐÓNG" : "TIẾP");
        }
```

Trong `S5ShellSceneController.cs`: xoá dòng 20 `[SerializeField] Sprite instructorPortrait;`, đổi dòng 65 thành `dialogues.ShowJourney(router.Session, GameManager.Instance, mainMenuBackground);`.

Trong `Assets/_Project/Scenes/Map.unity`: xoá đúng dòng `  instructorPortrait: {fileID: 21300000, guid: 563fdfbb68d44144f9a3de4233d5de4e, type: 3}` (khoảng dòng 4388). Kiểm tra: `grep -rn "instructorPortrait" Assets` → không còn kết quả.

- [ ] **Step 7: Sinh lại asset thoại**

Run:
```bash
"$UNITY" -batchmode -projectPath "$(pwd -W)" -executeMethod KMA.EditorTools.StudentJourneyContentBuilder.BuildDialogues -logFile Builds/build-dialogues.log -quit
grep -n "dialogue library built\|Exception\|error CS" Builds/build-dialogues.log
```
Expected: có dòng `[KMA] Student journey dialogue library built.`, không có `Exception`/`error CS`. `git diff --stat Assets/_Project/Resources/Journey/JourneyDialogues.asset` cho thấy asset đã đổi (có `cast:` và `characterId:`).

- [ ] **Step 8: Chạy test, xác nhận xanh**

Run:
```bash
tools/run-unity-tests.sh EditMode KMA.Tests.Gameplay.Progression progression-dialogue
tools/run-unity-tests.sh EditMode KMA.Tests.EditorTools.StudentJourneyContentBuilderTests builder
tools/run-unity-tests.sh PlayMode KMA.Tests.Presentation.JourneyNarrativeTests narrative
```
Expected: cả ba `result=Passed`, `failed=0`.

- [ ] **Step 9: Commit**

```bash
git add Assets/_Project/Scripts/Progression/Journey/JourneyDialogueLibrary.cs Assets/Editor/StudentJourneyContentBuilder.cs Assets/_Project/Scripts/UI/JourneyDialoguePresenter.cs Assets/_Project/Scripts/Shell/S5ShellSceneController.cs Assets/_Project/Scenes/Map.unity Assets/_Project/Resources/Journey/JourneyDialogues.asset Assets/Tests/EditMode/Progression/JourneyDialogueDataTests.cs Assets/Tests/EditMode/Progression/JourneyDialogueValidationTests.cs Assets/Tests/EditMode/Progression/JourneyDialogueValidationTests.cs.meta
git commit -m "feat(story): give the cast names, poses and a meme-heavy script"
```

---

### Task 4: Bộ emoji Twemoji cho TextMeshPro

**Files:**
- Create: `Assets/_Project/Art/Emoji/Source~/<name>.png` (15 file, không có `.meta`)
- Create (sinh ra): `Assets/_Project/Art/Emoji/JourneyEmojiAtlas.png` (+ `.meta`), `Assets/_Project/Art/Emoji.meta`, `Assets/_Project/Resources/Journey/JourneyEmoji.asset` (+ `.meta`)
- Modify: `Assets/Editor/StudentJourneyContentBuilder.cs`
- Modify: `Assets/_Project/CREDITS.md`
- Modify: `Assets/Tests/PlayMode/Progression/StudentJourneyFlowTests.cs:86-88`
- Test: Create `Assets/Tests/EditMode/EditorTools/JourneyEmojiAssetTests.cs`

**Interfaces:**
- Consumes: `DialogueEmoji.KnownNames`, `DialogueEmoji.FindCodes` (Task 1); `JourneyDialogueLibrary.Nodes` (Task 3).
- Produces: `public static void StudentJourneyContentBuilder.BuildEmojiSpriteAsset()`; Resources `"Journey/JourneyEmoji"` là `TMP_SpriteAsset` với 15 `spriteCharacterTable` entry tên đúng mã. `BuildDialogues()` gọi `BuildEmojiSpriteAsset()` trước.

- [ ] **Step 1: Tải PNG nguồn**

```bash
mkdir -p "Assets/_Project/Art/Emoji/Source~"
while read -r name cp; do
  curl -fsSL "https://cdn.jsdelivr.net/gh/jdecked/twemoji@15.1.0/assets/72x72/$cp.png" -o "Assets/_Project/Art/Emoji/Source~/$name.png" || echo "FAILED $name"
done <<'EOF'
sob 1f62d
skull 1f480
sunglasses 1f60e
fire 1f525
scream 1f631
runner 1f3c3
dash 1f4a8
soccer 26bd
volleyball 1f3d0
eyes 1f440
clown 1f921
salute 1fae1
100 1f4af
tada 1f389
muscle 1f4aa
EOF
ls "Assets/_Project/Art/Emoji/Source~" | wc -l
file "Assets/_Project/Art/Emoji/Source~/sob.png"
```
Expected: không có dòng `FAILED`, đếm được `15`, `file` báo `PNG image data, 72 x 72`.

- [ ] **Step 2: Viết test thất bại**

`Assets/Tests/EditMode/EditorTools/JourneyEmojiAssetTests.cs`:

```csharp
using System.Linq;
using KMA.Gameplay;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class JourneyEmojiAssetTests
    {
        static TMP_SpriteAsset Load() => Resources.Load<TMP_SpriteAsset>("Journey/JourneyEmoji");

        [Test]
        public void EmojiSpriteAssetHasOneNamedGlyphPerKnownCode()
        {
            TMP_SpriteAsset asset = Load();
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.spriteSheet, Is.Not.Null);
            Assert.That(asset.material, Is.Not.Null);
            Assert.That(new SerializedObject(asset).FindProperty("m_Version").stringValue, Is.EqualTo("1.1.0"),
                "An empty version makes TMP upgrade the asset on load and wipe its glyphs.");
            Assert.That(asset.spriteCharacterTable.Select(character => character.name),
                Is.EqualTo(DialogueEmoji.KnownNames));
            Assert.That(asset.spriteGlyphTable.Count, Is.EqualTo(DialogueEmoji.KnownNames.Count));
            foreach (TMP_SpriteGlyph glyph in asset.spriteGlyphTable)
            {
                Assert.That(glyph.glyphRect.width, Is.EqualTo(72));
                Assert.That(glyph.glyphRect.height, Is.EqualTo(72));
                Assert.That(glyph.glyphRect.x + glyph.glyphRect.width, Is.LessThanOrEqualTo(asset.spriteSheet.width));
                Assert.That(glyph.glyphRect.y + glyph.glyphRect.height, Is.LessThanOrEqualTo(asset.spriteSheet.height));
            }
        }

        [Test]
        public void EveryEmojiCodeInTheScriptHasAGlyph()
        {
            TMP_SpriteAsset asset = Load();
            JourneyDialogueLibrary library = JourneyDialogueLibrary.LoadDefault();
            foreach (JourneyDialogueNode node in library.Nodes)
                foreach (JourneyDialogueLine line in node.Lines)
                    foreach (string code in DialogueEmoji.FindCodes(line.Text))
                        Assert.That(asset.GetSpriteIndexFromName(code), Is.GreaterThanOrEqualTo(0), $"{node.Id}: :{code}:");
        }
    }
}
```

Run: `tools/run-unity-tests.sh EditMode KMA.Tests.EditorTools.JourneyEmojiAssetTests emoji-asset-red`
Expected: `result=Failed`, lỗi `Expected: not null` ở `asset`.

- [ ] **Step 3: Thêm `BuildEmojiSpriteAsset` vào builder**

Thêm `using System.IO; using TMPro; using UnityEngine.TextCore;` vào đầu file. Thêm hằng và phương thức:

```csharp
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

            // TMP treats an empty version as a legacy asset and rebuilds (empties) the tables on load.
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("m_Version").stringValue = "1.1.0";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA] Journey emoji sprite asset built.");
        }
```

Ở đầu `BuildDialogues()` (ngay sau `EnsureFolder(ResourcesFolder);`) thêm `BuildEmojiSpriteAsset();`.

- [ ] **Step 4: Sinh asset**

Run:
```bash
"$UNITY" -batchmode -projectPath "$(pwd -W)" -executeMethod KMA.EditorTools.StudentJourneyContentBuilder.BuildDialogues -logFile Builds/build-emoji.log -quit
grep -n "emoji sprite asset built\|dialogue library built\|Exception\|error CS" Builds/build-emoji.log
```
Expected: có cả hai dòng `built`, không có `Exception`/`error CS`. `JourneyEmojiAtlas.png` là 360×216.

- [ ] **Step 5: Chạy test trong process mới, xác nhận xanh**

Run: `tools/run-unity-tests.sh EditMode KMA.Tests.EditorTools.JourneyEmojiAssetTests emoji-asset`
Expected: `result=Passed`, `passed=2`. (Batch run là process mới nên asset được load lại từ đĩa — đây là kiểm chứng cho Review Focus #1.)

- [ ] **Step 6: Smoke test Player + credit**

Trong `StudentJourneyFlowTests.cs` ngay sau dòng `Assert.That(JourneyDialogueLibrary.LoadDefault(), Is.Not.Null);` thêm:

```csharp
            var emoji = Resources.Load<TMPro.TMP_SpriteAsset>("Journey/JourneyEmoji");
            Assert.That(emoji, Is.Not.Null);
            Assert.That(emoji.spriteCharacterTable.Count, Is.EqualTo(DialogueEmoji.KnownNames.Count));
```

Cuối `Assets/_Project/CREDITS.md` thêm:

```markdown

Twemoji graphics (15 emoji used in journey dialogue) © Twitter, Inc. and other contributors,
maintained at https://github.com/jdecked/twemoji, licensed under CC BY 4.0:
https://creativecommons.org/licenses/by/4.0/
Edits: packed into a 360x216 atlas (Assets/_Project/Art/Emoji/JourneyEmojiAtlas.png).
```

Run: `tools/run-unity-tests.sh PlayMode KMA.Tests.Gameplay.Progression.StudentJourneyFlowTests journey-flow`

Expected: `result=Passed`, `failed=0`.

- [ ] **Step 7: Commit**

```bash
git add "Assets/_Project/Art/Emoji.meta" "Assets/_Project/Art/Emoji/Source~" Assets/_Project/Art/Emoji/JourneyEmojiAtlas.png Assets/_Project/Art/Emoji/JourneyEmojiAtlas.png.meta Assets/_Project/Resources/Journey/JourneyEmoji.asset Assets/_Project/Resources/Journey/JourneyEmoji.asset.meta Assets/Editor/StudentJourneyContentBuilder.cs Assets/_Project/CREDITS.md Assets/Tests/PlayMode/Progression/StudentJourneyFlowTests.cs Assets/Tests/EditMode/EditorTools/JourneyEmojiAssetTests.cs Assets/Tests/EditMode/EditorTools/JourneyEmojiAssetTests.cs.meta
git commit -m "feat(story): bundle Twemoji as a TextMeshPro sprite asset"
```

---

### Task 5: Presenter visual novel

**Files:**
- Modify: `Assets/_Project/Scripts/UI/JourneyDialoguePresenter.cs` (viết lại toàn bộ)
- Test: `Assets/Tests/PlayMode/Presentation/JourneyNarrativeTests.cs`

**Interfaces:**
- Consumes: `DialogueTypewriter` (Task 2); `DialogueEmoji.Expand` (Task 1); `JourneyDialogueLibrary.Player/GetCharacter`, `JourneyCharacter.GetPose/DisplayName/TagColor/IsPlayer`, `JourneyDialogueLine.Sticker` (Task 3); Resources `"Journey/JourneyEmoji"` (Task 4).
- Produces: API giữ nguyên `Configure(library, saveSeen, sharedBackground = null)`, `Show(nodeId, onClosed)`, `ShowJourney(session, manager, sharedBackground = null)`, `IsShowing`, `CurrentLineIndex`; thêm `public void Advance()`, `public bool IsLineFullyShown`, `public static readonly Color ListenerTint`. Tên GameObject cố định cho test: `"LeftActor"`, `"RightActor"`, `"NameTag"` (con `"Name"`), `"Dialogue"`, `"Sticker"` (con `"Label"`), `"Hint"`, `"ProgressDots"`, `"SaveError"`, `"TapArea"` (Button), `"BỎ QUA"` (Button).

- [ ] **Step 1: Viết test thất bại**

Thay toàn bộ `JourneyNarrativeTests.cs`:

```csharp
using System.Collections;
using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class JourneyNarrativeTests
    {
        GameObject root;
        JourneyDialoguePresenter presenter;
        JourneyDialogueLibrary library;
        bool allowSave;
        string savedKey;
        int closed;

        void Open(string node)
        {
            root = new GameObject("JourneyNarrativeCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            presenter = root.AddComponent<JourneyDialoguePresenter>();
            library = JourneyDialogueLibrary.LoadDefault();
            allowSave = true;
            savedKey = null;
            closed = 0;
            presenter.Configure(library, key => { savedKey = key; return allowSave; });
            presenter.Show(node, () => closed++);
        }

        T Find<T>(string name) where T : Component =>
            root.GetComponentsInChildren<T>(true).First(component => component.name == name);

        void Tap() => Find<Button>("TapArea").onClick.Invoke();

        void TapToLine(int index)
        {
            while (presenter.CurrentLineIndex < index) Tap();
        }

        [UnityTest]
        public IEnumerator SkipCanRetryAfterSaveFailureAndPersistsBeforeClosing()
        {
            Open("opening");
            allowSave = false;
            yield return null;
            Button skip = Find<Button>("BỎ QUA");
            skip.onClick.Invoke();
            Assert.That(presenter.IsShowing, Is.True);
            Assert.That(savedKey, Is.EqualTo("opening"));
            Assert.That(closed, Is.Zero);
            allowSave = true;
            skip.onClick.Invoke();
            Assert.That(presenter.IsShowing, Is.False);
            Assert.That(closed, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FirstTapRevealsWholeLineSecondTapAdvances()
        {
            Open("opening");
            yield return null;
            TMP_Text body = Find<TMP_Text>("Dialogue");
            Assert.That(presenter.IsLineFullyShown, Is.False);
            Tap();
            Assert.That(presenter.IsLineFullyShown, Is.True);
            Assert.That(presenter.CurrentLineIndex, Is.Zero);
            Assert.That(body.maxVisibleCharacters, Is.GreaterThanOrEqualTo(body.textInfo.characterCount));
            Tap();
            Assert.That(presenter.CurrentLineIndex, Is.EqualTo(1));
            Assert.That(presenter.IsLineFullyShown, Is.False);
        }

        [UnityTest]
        public IEnumerator LastLineFirstTapRevealsSecondTapClosesAndSaves()
        {
            Open("sprint_exam");
            yield return null;
            TapToLine(1);
            Assert.That(presenter.IsLineFullyShown, Is.False);
            Tap();
            Assert.That(presenter.IsShowing, Is.True);
            Assert.That(savedKey, Is.Null);
            Assert.That(Find<TMP_Text>("Hint").text, Does.Contain("ĐÓNG"));
            Tap();
            Assert.That(presenter.IsShowing, Is.False);
            Assert.That(savedKey, Is.EqualTo("sprint_exam"));
            Assert.That(closed, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SaveFailureKeepsOverlayAndRetryClosesExactlyOnce()
        {
            Open("sprint_exam");
            allowSave = false;
            yield return null;
            TapToLine(1);
            Tap();
            Tap();
            Assert.That(presenter.IsShowing, Is.True);
            Assert.That(Find<TMP_Text>("SaveError").text, Is.Not.Empty);
            Assert.That(closed, Is.Zero);
            allowSave = true;
            Tap();
            Assert.That(presenter.IsShowing, Is.False);
            presenter.Advance();
            Assert.That(closed, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PlayerStaysRightAsListenerWhenSilent()
        {
            Open("sprint_pass");
            yield return new WaitForSecondsRealtime(.4f);
            Image right = Find<Image>("RightActor");
            Image left = Find<Image>("LeftActor");
            JourneyCharacter co = library.GetCharacter("co_the_chat");
            Assert.That(right.enabled, Is.True);
            Assert.That(right.sprite, Is.SameAs(library.Player.GetPose(DialoguePose.Idle)));
            Assert.That(right.color, Is.EqualTo(JourneyDialoguePresenter.ListenerTint));
            Assert.That(left.sprite, Is.SameAs(co.GetPose(DialoguePose.Cheer)));
            Assert.That(left.color, Is.EqualTo(Color.white));
            Assert.That(Find<TMP_Text>("Name").text, Is.EqualTo("Cô Thể Chất"));
        }

        [UnityTest]
        public IEnumerator LeftSlotSwapsToEachNewSpeaker()
        {
            Open("soccer_intro");
            yield return null;
            TapToLine(1);
            yield return new WaitForSecondsRealtime(.5f);
            Image left = Find<Image>("LeftActor");
            Assert.That(left.sprite, Is.SameAs(library.GetCharacter("anh_khoa_tren").GetPose(DialoguePose.Cheer)));
            TapToLine(2);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(left.sprite, Is.SameAs(library.GetCharacter("co_the_chat").GetPose(DialoguePose.Idle)));
            Assert.That(left.rectTransform.anchoredPosition.x, Is.LessThan(0f));
        }

        [UnityTest]
        public IEnumerator EmojiRenderThroughTheSpriteAssetAndStickersFollowTheLine()
        {
            Open("opening");
            yield return null;
            TMP_Text body = Find<TMP_Text>("Dialogue");
            Assert.That(body.text, Does.Contain("<sprite name=\"eyes\">"));
            Assert.That(body.spriteAsset, Is.Not.Null);
            Assert.That(body.spriteAsset.name, Is.EqualTo("JourneyEmoji"));
            RectTransform sticker = Find<RectTransform>("Sticker");
            Assert.That(sticker.gameObject.activeSelf, Is.False);
            TapToLine(1);
            Assert.That(sticker.gameObject.activeSelf, Is.True);
            Assert.That(sticker.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("ÉT O ÉT!"));
            TapToLine(2);
            Assert.That(sticker.gameObject.activeSelf, Is.False);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root) Object.Destroy(root);
            yield return null;
        }
    }
}
```

Lưu ý: `TapToLine` chạm tới khi `CurrentLineIndex` đạt index (mỗi câu cần hai chạm: hiện hết rồi sang câu).

- [ ] **Step 2: Chạy, xác nhận đỏ**

Run: `tools/run-unity-tests.sh PlayMode KMA.Tests.Presentation.JourneyNarrativeTests narrative-red`
Expected: log có `error CS1061` (`Advance`, `IsLineFullyShown`, `ListenerTint` chưa có).

- [ ] **Step 3: Viết lại `JourneyDialoguePresenter.cs`**

```csharp
using System;
using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    /// Visual-novel style journey dialogue: two actor slots (player always right), a typewriter
    /// text box with emoji sprites, optional reaction stickers, tap anywhere to continue.
    public sealed class JourneyDialoguePresenter : MonoBehaviour
    {
        public static readonly Color ListenerTint = new Color(.4f, .4f, .4f, 1f);

        const string EmojiResource = "Journey/JourneyEmoji";
        const float ActorHeight = 734f, ActorBottom = 260f, ActorX = 630f, ActorOffX = 1250f;
        const float SlideSeconds = .25f, HopSeconds = .3f, StickerSeconds = .25f;
        const float BoxHeight = 313f, BoxMargin = 67f, BoxBottom = 38f;
        static readonly Color StickerRed = new Color32(0xE2, 0x55, 0x3D, 0xFF);
        static readonly Color DotIdle = new Color(1f, 1f, 1f, .3f);

        readonly struct DialogueRequest
        {
            public readonly string NodeId, SeenKey;
            public DialogueRequest(string nodeId, string seenKey) { NodeId = nodeId; SeenKey = seenKey; }
        }

        sealed class ActorSlot
        {
            public Image Image;
            public float HomeX, OffX, Slide, Hop = -1f;
            public bool Speaking;
            public JourneyCharacter Character, Pending;
            public Sprite PendingSprite;
        }

        JourneyDialogueLibrary library;
        Func<string, bool> persistSeen;
        readonly Queue<DialogueRequest> pending = new Queue<DialogueRequest>();
        readonly DialogueTypewriter typewriter = new DialogueTypewriter();
        readonly List<Image> dots = new List<Image>();
        readonly ActorSlot left = new ActorSlot { HomeX = -ActorX, OffX = -ActorOffX };
        readonly ActorSlot right = new ActorSlot { HomeX = ActorX, OffX = ActorOffX };
        RectTransform overlay, nameTagRect, dotsRoot, stickerRect;
        Image backgroundImage, nameTagImage;
        TMP_Text nameTag, body, errorText, hint, stickerText;
        IReadOnlyList<JourneyDialogueLine> activeLines;
        string activeSeenKey;
        int lineIndex;
        float stickerAge = -1f;
        Action closed;

        public bool IsShowing => overlay != null && overlay.gameObject.activeSelf;
        public int CurrentLineIndex => lineIndex;
        public bool IsLineFullyShown => typewriter.IsDone;

        public void Configure(JourneyDialogueLibrary dialogueLibrary, Func<string, bool> saveSeen,
            Sprite sharedBackground = null)
        {
            library = dialogueLibrary;
            persistSeen = saveSeen;
            EnsureView();
            if (sharedBackground != null) backgroundImage.sprite = sharedBackground;
        }

        public void Show(string nodeId, Action onClosed) => Show(nodeId, nodeId, onClosed);

        public void ShowJourney(GameSession session, GameManager manager, Sprite sharedBackground = null)
        {
            if (session == null || manager == null) return;
            Configure(JourneyDialogueLibrary.LoadDefault(), key => manager.TryMarkJourneyDialogueSeen(key, out _),
                sharedBackground);
            pending.Clear();
            JourneyProgress journey = session.Journey;
            void Add(string node, string key = null)
            {
                string seenKey = key ?? node;
                if (!journey.IsDialogueSeen(seenKey)) pending.Enqueue(new DialogueRequest(node, seenKey));
            }

            Add("opening");
            if (journey.AwaitingSupplementary)
            {
                Add("supplementary", $"supplementary_{journey.SupplementaryRounds + 1}");
            }
            else if (journey.CourseComplete)
            {
                Add("soccer_pass");
                Add("course_complete");
            }
            else
            {
                switch (journey.CheckpointChallengeId)
                {
                    case "sprint_learn": Add("sprint_intro"); break;
                    case "sprint_exam": Add("sprint_exam"); break;
                    case "volleyball_learn": Add("sprint_pass"); Add("volleyball_intro"); break;
                    case "volleyball_exam": Add("volleyball_exam"); break;
                    case "soccer_learn": Add("volleyball_pass"); Add("soccer_intro"); break;
                    case "soccer_exam": Add("soccer_exam"); break;
                }
            }
            ShowNext();
        }

        /// Tap handler: reveal the rest of the line, then move to the next line, then close.
        public void Advance()
        {
            if (!IsShowing) return;
            errorText.text = string.Empty;
            if (!typewriter.IsDone)
            {
                typewriter.Complete();
                body.maxVisibleCharacters = typewriter.VisibleCharacters;
                UpdateHint();
                return;
            }
            if (lineIndex + 1 < activeLines.Count)
            {
                lineIndex++;
                RenderLine();
                return;
            }
            FinishNode();
        }

        void Show(string nodeId, string seenKey, Action onClosed)
        {
            if (library == null) library = JourneyDialogueLibrary.LoadDefault();
            if (library == null) throw new InvalidOperationException("Journey dialogue Resources asset is missing.");
            EnsureView();
            JourneyDialogueNode node = library.Get(nodeId);
            activeLines = node.Lines;
            activeSeenKey = seenKey;
            closed = onClosed;
            lineIndex = 0;
            errorText.text = string.Empty;
            overlay.gameObject.SetActive(true);
            ResetActors();
            BuildDots(activeLines.Count);
            RenderLine();
        }

        void Update()
        {
            if (!IsShowing) return;
            float dt = Time.unscaledDeltaTime;
            if (!typewriter.IsDone)
            {
                typewriter.Tick(dt);
                body.maxVisibleCharacters = typewriter.VisibleCharacters;
                if (typewriter.IsDone) UpdateHint();
            }
            AnimateSlot(left, dt);
            AnimateSlot(right, dt);
            if (stickerAge >= 0f)
            {
                stickerAge += dt;
                float t = Mathf.Clamp01(stickerAge / StickerSeconds);
                float scale = t < .6f ? Mathf.Lerp(0f, 1.15f, t / .6f) : Mathf.Lerp(1.15f, 1f, (t - .6f) / .4f);
                stickerRect.localScale = new Vector3(scale, scale, 1f);
                if (t >= 1f) stickerAge = -1f;
            }
        }

        void ResetActors()
        {
            JourneyCharacter player = library.Player;
            right.Character = player;
            right.Pending = null;
            right.Image.sprite = player?.GetPose(DialoguePose.Idle);
            right.Slide = 0f;
            right.Hop = -1f;
            left.Character = null;
            left.Pending = null;
            left.Image.sprite = null;
            left.Slide = 0f;
            left.Hop = -1f;
            foreach (JourneyDialogueLine line in activeLines)
            {
                JourneyCharacter character = library.GetCharacter(line.CharacterId);
                if (character.IsPlayer) continue;
                left.Character = character;
                left.Image.sprite = character.GetPose(DialoguePose.Idle);
                break;
            }
        }

        void RenderLine()
        {
            JourneyDialogueLine line = activeLines[lineIndex];
            JourneyCharacter speaker = library.GetCharacter(line.CharacterId);
            Sprite sprite = speaker.GetPose(line.Pose);
            bool onRight = speaker.IsPlayer;
            ActorSlot speaking = onRight ? right : left;
            if (onRight) right.Image.sprite = sprite;
            else PlaceLeft(speaker, sprite);
            right.Speaking = onRight;
            left.Speaking = !onRight;
            speaking.Hop = 0f;
            ApplySlot(left);
            ApplySlot(right);

            nameTag.text = VietText.Fix(speaker.DisplayName);
            nameTagImage.color = speaker.TagColor;
            nameTagRect.anchorMin = nameTagRect.anchorMax = new Vector2(onRight ? 1f : 0f, 1f);
            nameTagRect.pivot = new Vector2(onRight ? 1f : 0f, .5f);
            nameTagRect.anchoredPosition = new Vector2(onRight ? -36f : 36f, 0f);
            nameTagRect.localEulerAngles = new Vector3(0f, 0f, onRight ? 3f : -3f);

            body.text = VietText.Fix(DialogueEmoji.Expand(line.Text));
            body.maxVisibleCharacters = 0;
            body.ForceMeshUpdate();
            typewriter.Begin(body.textInfo.characterCount);
            body.maxVisibleCharacters = typewriter.VisibleCharacters;

            bool hasSticker = !string.IsNullOrWhiteSpace(line.Sticker);
            stickerRect.gameObject.SetActive(hasSticker);
            stickerAge = hasSticker ? 0f : -1f;
            if (hasSticker)
            {
                stickerText.text = VietText.Fix(line.Sticker);
                stickerRect.anchoredPosition = new Vector2(speaking.HomeX + (onRight ? -170f : 170f),
                    ActorBottom + ActorHeight * .82f);
                stickerRect.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(-8f, 8f));
                stickerRect.localScale = Vector3.zero;
            }

            for (int i = 0; i < dots.Count; i++) dots[i].color = i <= lineIndex ? HomeMenuStyle.Gold : DotIdle;
            UpdateHint();
        }

        void PlaceLeft(JourneyCharacter character, Sprite sprite)
        {
            if (left.Character == null || left.Character == character)
            {
                left.Character = character;
                left.Image.sprite = sprite;
                left.Pending = null;
                left.PendingSprite = null;
                return;
            }
            left.Pending = character;
            left.PendingSprite = sprite;
        }

        void AnimateSlot(ActorSlot slot, float dt)
        {
            float step = dt / (SlideSeconds * .5f);
            if (slot.Pending != null)
            {
                slot.Slide = Mathf.MoveTowards(slot.Slide, 0f, step);
                if (slot.Slide <= 0f)
                {
                    slot.Character = slot.Pending;
                    slot.Image.sprite = slot.PendingSprite;
                    slot.Pending = null;
                    slot.PendingSprite = null;
                }
            }
            else
            {
                slot.Slide = Mathf.MoveTowards(slot.Slide, 1f, step);
            }
            if (slot.Hop >= 0f)
            {
                slot.Hop += dt;
                if (slot.Hop >= HopSeconds) slot.Hop = -1f;
            }
            ApplySlot(slot);
        }

        void ApplySlot(ActorSlot slot)
        {
            float hop = slot.Hop >= 0f ? Mathf.Sin(Mathf.PI * Mathf.Clamp01(slot.Hop / HopSeconds)) * ActorHeight * .06f : 0f;
            float sink = slot.Speaking ? 0f : -ActorHeight * .04f;
            float eased = 1f - (1f - slot.Slide) * (1f - slot.Slide);
            slot.Image.rectTransform.anchoredPosition =
                new Vector2(Mathf.Lerp(slot.OffX, slot.HomeX, eased), ActorBottom + hop + sink);
            slot.Image.color = slot.Speaking ? Color.white : ListenerTint;
            slot.Image.enabled = slot.Character != null && slot.Image.sprite != null;
        }

        void UpdateHint()
        {
            string text = !typewriter.IsDone ? "chạm để hiện hết"
                : lineIndex + 1 >= activeLines.Count ? "ĐÓNG »" : "TIẾP »";
            hint.text = VietText.Fix(text);
        }

        void FinishNode()
        {
            errorText.text = string.Empty;
            if (persistSeen == null || !persistSeen(activeSeenKey))
            {
                errorText.text = VietText.Fix("Không lưu được mốc hội thoại. Chạm để thử lại.");
                return;
            }
            overlay.gameObject.SetActive(false);
            stickerRect.gameObject.SetActive(false);
            Action callback = closed;
            closed = null;
            callback?.Invoke();
        }

        void ShowNext()
        {
            if (pending.Count == 0) return;
            DialogueRequest request = pending.Dequeue();
            Show(request.NodeId, request.SeenKey, ShowNext);
        }

        void BuildDots(int count)
        {
            foreach (Image dot in dots)
                if (dot != null) Destroy(dot.gameObject);
            dots.Clear();
            for (int i = 0; i < count; i++)
            {
                Image dot = UiKit.Disc(dotsRoot, "Dot" + i, false, DotIdle);
                dot.rectTransform.sizeDelta = new Vector2(16f, 16f);
                LayoutElement element = dot.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = element.preferredHeight = 16f;
                dots.Add(dot);
            }
        }

        void EnsureView()
        {
            if (overlay != null) return;
            overlay = UiKit.Rect(transform, "JourneyDialogueOverlay");
            UiKit.Stretch(overlay);
            overlay.gameObject.AddComponent<CanvasGroup>();

            var backgroundObject = new GameObject("SharedMenuBackground", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
            backgroundObject.transform.SetParent(overlay, false);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(.5f, .5f);
            backgroundRect.sizeDelta = Vector2.zero;
            backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.raycastTarget = false;
            AspectRatioFitter backgroundFit = backgroundObject.GetComponent<AspectRatioFitter>();
            backgroundFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundFit.aspectRatio = 1928f / 816f;

            Image dimmer = UiKit.Rect(overlay, "BackgroundDimmer").gameObject.AddComponent<Image>();
            UiKit.Stretch(dimmer.rectTransform);
            dimmer.color = MinigameUiTheme.WithAlpha(HomeMenuStyle.Navy, .36f);
            dimmer.raycastTarget = false;

            left.Image = MakeActor("LeftActor", false);
            right.Image = MakeActor("RightActor", true);

            stickerRect = UiKit.Rect(overlay, "Sticker");
            UiKit.Place(stickerRect, new Vector2(.5f, 0f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(340f, 84f));
            Image stickerCard = stickerRect.gameObject.AddComponent<Image>();
            UiKit.SetRadius(stickerCard, 18f);
            stickerCard.color = Color.white;
            stickerCard.raycastTarget = false;
            Outline stickerOutline = stickerRect.gameObject.AddComponent<Outline>();
            stickerOutline.effectColor = HomeMenuStyle.Navy;
            stickerOutline.effectDistance = new Vector2(3f, -3f);
            stickerText = MakeText(stickerRect, "Label", 40f, StickerRed, TextAlignmentOptions.Center);
            UiKit.Stretch(stickerText.rectTransform);
            stickerText.fontStyle = FontStyles.Bold;
            stickerRect.gameObject.SetActive(false);

            Image box = UiKit.Shape(overlay, "DialogueBox", 28f, MinigameUiTheme.WithAlpha(HomeMenuStyle.Navy, .93f));
            RectTransform boxRect = box.rectTransform;
            boxRect.anchorMin = Vector2.zero;
            boxRect.anchorMax = new Vector2(1f, 0f);
            boxRect.pivot = new Vector2(.5f, 0f);
            boxRect.offsetMin = new Vector2(BoxMargin, BoxBottom);
            boxRect.offsetMax = new Vector2(-BoxMargin, BoxBottom + BoxHeight);
            Outline boxOutline = box.gameObject.AddComponent<Outline>();
            boxOutline.effectColor = HomeMenuStyle.Gold;
            boxOutline.effectDistance = new Vector2(3f, -3f);

            nameTagImage = UiKit.Shape(boxRect, "NameTag", 26f, HomeMenuStyle.Gold);
            nameTagRect = nameTagImage.rectTransform;
            nameTagRect.sizeDelta = new Vector2(320f, 56f);
            Outline tagOutline = nameTagImage.gameObject.AddComponent<Outline>();
            tagOutline.effectColor = Color.white;
            tagOutline.effectDistance = new Vector2(2f, -2f);
            nameTag = MakeText(nameTagRect, "Name", 28f, HomeMenuStyle.Navy, TextAlignmentOptions.Center);
            UiKit.Stretch(nameTag.rectTransform);
            nameTag.fontStyle = FontStyles.Bold;

            body = MakeText(boxRect, "Dialogue", 34f, HomeMenuStyle.White, TextAlignmentOptions.TopLeft);
            UiKit.Stretch(body.rectTransform, new Vector2(56f, 58f), new Vector2(-56f, -44f));
            body.spriteAsset = Resources.Load<TMP_SpriteAsset>(EmojiResource);

            errorText = MakeText(boxRect, "SaveError", 22f, HomeMenuStyle.GoldLight, TextAlignmentOptions.Right);
            UiKit.Place(errorText.rectTransform, Vector2.one, Vector2.one, new Vector2(-40f, -10f), new Vector2(760f, 34f));
            hint = MakeText(boxRect, "Hint", 22f, HomeMenuStyle.Gold, TextAlignmentOptions.Right);
            UiKit.Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 12f), new Vector2(360f, 34f));
            dotsRoot = UiKit.Rect(boxRect, "ProgressDots");
            UiKit.Place(dotsRoot, Vector2.zero, Vector2.zero, new Vector2(40f, 20f), new Vector2(200f, 18f));
            HorizontalLayoutGroup dotLayout = dotsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            dotLayout.spacing = 10f;
            dotLayout.childAlignment = TextAnchor.MiddleLeft;
            dotLayout.childControlWidth = dotLayout.childControlHeight = false;
            dotLayout.childForceExpandWidth = dotLayout.childForceExpandHeight = false;

            RectTransform tapRect = UiKit.Rect(overlay, "TapArea");
            UiKit.Stretch(tapRect);
            Image tapImage = tapRect.gameObject.AddComponent<Image>();
            tapImage.color = new Color(0f, 0f, 0f, 0f);
            tapImage.raycastTarget = true;
            Button tap = tapRect.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(Advance);

            MakeSkip().onClick.AddListener(FinishNode);
            overlay.gameObject.SetActive(false);
        }

        Image MakeActor(string name, bool flipped)
        {
            RectTransform rect = UiKit.Rect(overlay, name);
            UiKit.Place(rect, new Vector2(.5f, 0f), new Vector2(.5f, 0f), Vector2.zero,
                new Vector2(ActorHeight * .8f, ActorHeight));
            if (flipped) rect.localScale = new Vector3(-1f, 1f, 1f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        Button MakeSkip()
        {
            Image plate = UiKit.Shape(overlay, "BỎ QUA", 32f, MinigameUiTheme.WithAlpha(HomeMenuStyle.Navy, .85f));
            plate.raycastTarget = true;
            UiKit.Place(plate.rectTransform, Vector2.one, Vector2.one, new Vector2(-58f, -43f), new Vector2(220f, 64f));
            Outline outline = plate.gameObject.AddComponent<Outline>();
            outline.effectColor = HomeMenuStyle.Gold;
            outline.effectDistance = new Vector2(2f, -2f);
            TMP_Text label = MakeText(plate.rectTransform, "Label", 24f, HomeMenuStyle.White, TextAlignmentOptions.Center);
            UiKit.Stretch(label.rectTransform);
            label.text = VietText.Fix("BỎ QUA »");
            return plate.gameObject.AddComponent<Button>();
        }

        static TMP_Text MakeText(Transform parent, string name, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            TMP_Text text = UiKit.Rect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            UiKit.StyleLabel(text, fontSize, color);
            VietTypography.Apply(text);
            return text;
        }
    }
}
```

Ghi chú: `DialogueBox` và `TapArea` đều phủ màn hình nhưng `TapArea` tạo sau nên nằm trên và nhận chạm; `BỎ QUA` tạo sau cùng nên nằm trên `TapArea`.

- [ ] **Step 4: Chạy test, xác nhận xanh**

Run:
```bash
tools/run-unity-tests.sh PlayMode KMA.Tests.Presentation.JourneyNarrativeTests narrative
tools/run-unity-tests.sh PlayMode KMA.Tests.Gameplay.Progression.StudentJourneyFlowTests journey-flow
```
Expected: cả hai `result=Passed`, `failed=0` (`journey-flow` xác nhận `SkipDialogues` vẫn tìm được nút `BỎ QUA` trong luồng thật).

- [ ] **Step 5: Chạy toàn bộ test để bắt hồi quy**

Run:
```bash
tools/run-unity-tests.sh EditMode "" all-editmode
tools/run-unity-tests.sh PlayMode "" all-playmode
```
Expected: cả hai `failed=0`.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/UI/JourneyDialoguePresenter.cs Assets/Tests/PlayMode/Presentation/JourneyNarrativeTests.cs
git commit -m "feat(story): present journey dialogue as a visual novel"
```

---

### Task 6: QA thị giác

**Files:**
- Modify: `Assets/Editor/PlayModeScreenshot.cs`
- Modify: `tools/qa-screenshot.sh`
- Create: `docs/qa/images/story-opening.png`, `docs/qa/images/story-course-complete.png`
- Modify: `docs/qa/kma-student-journey.md`
- Có thể modify (tinh chỉnh): `Assets/Editor/StudentJourneyContentBuilder.cs` (metrics emoji), `JourneyEmoji.asset`

**Interfaces:**
- Consumes: `JourneyDialoguePresenter.Configure/Show/Advance` (Task 5); `S5ShellSceneController.MainMenuBackground`.
- Produces: tham số request `openDialogue` (string) và `dialogueTaps` (int); `tools/qa-screenshot.sh` nhận thêm tham số thứ 8 `openDialogue` và thứ 9 `dialogueTaps`.

- [ ] **Step 1: Thêm hook QA vào `PlayModeScreenshot.cs`**

Thêm hằng `const string KeyOpenDialogue = "KMA_PMS_OpenDialogue";`, `const string KeyDialogueTaps = "KMA_PMS_DialogueTaps";`. Trong `class Request` thêm:

```csharp
            // QA-only: open a journey dialogue node on the loaded scene and tap it N times.
            public string openDialogue = "";
            public int dialogueTaps;
```

Ngay sau `SessionState.SetBool(KeyOpenPause, req.openPause);` thêm:

```csharp
            SessionState.SetString(KeyOpenDialogue, req.openDialogue ?? "");
            SessionState.SetInt(KeyDialogueTaps, req.dialogueTaps);
```

Cuối `OnPlayModeStateChanged` (sau khối `KeyOpenPause`) thêm:

```csharp
            string dialogueNode = SessionState.GetString(KeyOpenDialogue, "");
            if (!string.IsNullOrEmpty(dialogueNode))
            {
                Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    GameObject host = canvas.rootCanvas.gameObject;
                    var presenter = host.GetComponent<KMA.Gameplay.UI.JourneyDialoguePresenter>();
                    if (presenter == null) presenter = host.AddComponent<KMA.Gameplay.UI.JourneyDialoguePresenter>();
                    var shell = UnityEngine.Object.FindFirstObjectByType<KMA.Gameplay.Shell.S5ShellSceneController>();
                    presenter.Configure(KMA.Gameplay.JourneyDialogueLibrary.LoadDefault(), _ => true,
                        shell != null ? shell.MainMenuBackground : null);
                    presenter.Show(dialogueNode, null);
                    for (int i = 0; i < SessionState.GetInt(KeyDialogueTaps, 0); i++) presenter.Advance();
                }
            }
```

Trong `tools/qa-screenshot.sh`: thêm `OPEN_DIALOGUE="${8:-}"` và `DIALOGUE_TAPS="${9:-0}"` sau dòng `OPEN_PAUSE=...`, cập nhật dòng `# Usage:` thêm `[openDialogue] [dialogueTaps]`, và nối vào JSON trước `}`: `,"openDialogue":"$OPEN_DIALOGUE","dialogueTaps":$DIALOGUE_TAPS`.

- [ ] **Step 2: Chụp ảnh**

Dùng skill `testing-unity-ui-with-screenshots` (mở Unity Editor với project; tool gửi request tới Editor đang chạy). Chạy:

```bash
tools/qa-screenshot.sh docs/qa/images/story-opening.png Assets/_Project/Scenes/Map.unity 4 false -1 "" false opening 3
tools/qa-screenshot.sh docs/qa/images/story-course-complete.png Assets/_Project/Scenes/Map.unity 4 false -1 "" false course_complete 0
```
Expected: cả hai in `"status":"ok"`. Ảnh 1: Mai Toang (tư thế hurt) bên trái sáng, Tân Thủ bên phải tối, sticker "ÉT O ÉT!", câu có emoji 😭. Ảnh 2: Cô Thể Chất cheer, sticker "HOÀN THÀNH!", emoji 🎉.

- [ ] **Step 3: Kiểm tra ảnh bằng mắt (Read ảnh) và tinh chỉnh nếu cần**

Checklist:
- Emoji nằm thẳng dòng với chữ, cỡ xấp xỉ chữ hoa. Nếu lệch dọc: chỉnh `EmojiCell * .8f` (bearingY) trong `BuildEmojiSpriteAsset`; nếu quá to/nhỏ: chỉnh tham số `scale` `1f` của `TMP_SpriteGlyph`. Sau đó chạy lại Task 4 Step 4 và chụp lại.
- Dấu tiếng Việt và `»` hiển thị (không có ô vuông). Nếu `»` thành ô vuông: đổi trong `UpdateHint`/`MakeSkip` sang `>` và chạy lại test Task 5.
- Tag tên, khung thoại, nút BỎ QUA không bị cắt; nhân vật không đè lên chữ trong khung.

- [ ] **Step 4: Ghi QA**

Cuối `docs/qa/kma-student-journey.md` thêm:

```markdown

## Cốt truyện visual novel (2026-10-05)

Spec: `docs/superpowers/specs/2026-10-05-story-visual-novel-design.md`.

| Node | Ảnh | Kiểm tra |
|---|---|---|
| `opening`, câu 2 | ![opening](images/story-opening.png) | Mai Toang nói (sáng, hurt), Tân Thủ nghe bên phải (tối), sticker, emoji |
| `course_complete`, câu 1 | ![course complete](images/story-course-complete.png) | Cô Thể Chất cheer, sticker "HOÀN THÀNH!", emoji |

Tests: `DialogueEmojiTests`, `DialogueTypewriterTests`, `JourneyDialogueValidationTests`, `JourneyDialogueDataTests`, `JourneyEmojiAssetTests`, `JourneyNarrativeTests`, `StudentJourneyFlowTests` — tất cả pass.
```

Điền đúng tình trạng thực tế (nếu có điều chỉnh ở Step 3, ghi lại).

- [ ] **Step 5: Commit**

```bash
git add Assets/Editor/PlayModeScreenshot.cs tools/qa-screenshot.sh docs/qa/images/story-opening.png docs/qa/images/story-course-complete.png docs/qa/kma-student-journey.md
git commit -m "chore(qa): capture visual novel dialogue screenshots"
```
(Nếu Step 3 đổi builder/asset/presenter, stage thêm các file đó và chạy lại test liên quan trước khi commit.)
