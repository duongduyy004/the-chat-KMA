# Sports Supplement Character Art Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every minigame shows the context-specific poses from `chay-tron-the-chat-sports-supplement.zip` (back-view kicker, a dedicated student goalkeeper, volleyball, frog-jump and fall poses, hero/teacher expressions), by changing only which sprites the editor configurators wire in.

**Architecture:** New PNGs are copied into the existing per-character folders (plus a new `StudentKeeper/` and `UI/Portraits/`), so `CharacterArt` keeps its one-folder-per-character layout. `CharacterArt` learns the extra pose lists and imports them. Each scene configurator loads different pose files. The pose keys the runtime controllers ask for stay unchanged, so no runtime script changes, except the football keeper's save capsules: they are data that must trace the drawn keeper.

**Tech Stack:** Unity 6000.3.23f1, C#, NUnit (Unity Test Framework, EditMode + PlayMode), editor configurators under `Assets/Editor` (assembly `KMA.EditorTools`).

**Spec:** `docs/superpowers/specs/2026-10-10-sports-supplement-assets-design.md`

## Global Constraints

- Source PNGs come from `chay-tron-the-chat-sports-supplement.zip` at the repo root. Copy only: `HeroExtra` (36) → `Art/Characters/MaleAdventurer/`, `BossPEExtra` (8) → `Art/Characters/BossPE/`, `StudentKeeper` (12) → `Art/Characters/StudentKeeper/`, `UI/Portraits` (12) → `Art/UI/Portraits/`. Never copy `ReusedHero/`, `ReusedBossPE/`, `Art/Football/Characters/`, `Previews/`, `Sources/`, `Docs/` or the zip's `README.md`.
- Never delete or overwrite an existing sprite or `.meta`. Never commit the zip itself.
- Character poses: 192 × 256, Sprite Single, PPU 200, pivot bottom-centre `(96, 0)`, Bilinear, no mipmaps, Clamp, Max Size 512, FullRect (the existing `CharacterArt.Import` settings).
- Portraits: 256 × 256, Sprite Single, PPU 100, pivot centre `(128, 128)`, Bilinear, no mipmaps, Clamp, Max Size 256, FullRect. Imported, but referenced by nothing.
- Opponents, rivals and the classmate (`MalePerson`, `FemalePerson`, `FemaleAdventurer`) keep every pose they use today.
- The football keeper never shows a pose with a ball drawn in (`*Ball`): `FootballPresentation` keeps the real ball visible on a save.
- No changes under `Assets/_Project/Scripts` except `FootballFlightSimulation.cs` (keeper capsules).
- Commit straight to `master`. **No `Co-Authored-By` trailer** in commit messages. Leave the user's untracked `Assets/_Project/Scripts/Gameplay/Sprint/RunnerBreathing.cs.meta` alone (do not add, delete or commit it).
- The Unity Editor must be closed while batch mode runs (tests or `-executeMethod`).
- Run tests with `tools/run-unity-tests.sh <EditMode|PlayMode> <filter> <name>` (prints `result/total/passed/failed`; non-zero exit on any failure).
- Run an editor method with (Git Bash, repo root):
  ```bash
  "/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit -projectPath "$(pwd -W)" \
    -executeMethod <Fully.Qualified.Method> -logFile Builds/<name>.log; echo "exit=$?"
  ```
  `exit=0` means success. On non-zero, run `grep -n "error CS\|Exception" Builds/<name>.log | head -20` and fix before continuing.

## Spec deviations (decided while planning; already written back into the spec)

- **Execution note:** with `ready` the two tests pinning the default centre shot as a save (`KeeperReactsAfterDelayAndSavesActualContact`, `FiveResolvedShotsEmitOneCampaignResultAfterTheFeedbackPhase`) had to move to chest-height shots (power .4; 0.9 s charge). See spec §4.1.
- **Keeper capsules retuned (user-approved).** `StudentKeeper_ready` fits the current capsules at recall 0.63 / precision 0.55 (`KeeperSilhouetteTests` needs ≥ 0.85). The capsules in `FootballFlightSimulation.cs` are replaced with a set fitted to `ready` (0.91 / 0.91, same as the current keeper). The save area is ~13 % smaller and has no raised arms, so high corners are easier to score.
- **Volleyball marker raised.** `volleySpikeContact` reaches 246 px (2.83 world units at `AthleteScale` 2.3); the marker's lowest point is at 3.1 − 0.368 = 2.73. `MarkerWorldHeight` goes 3.1 → 3.25 (lowest point 2.88).
- **Celebration hero keeps `cheer0`/`cheer1`.** The controller alternates slots 1 and 2 every 0.35 s; `celebrate` ↔ `cheer1` would flicker between two unrelated drawings. Only slot 0 (arrival) becomes `happy`.
- **`BuildDialogues` imports character art first**, because the new hero/teacher files must be imported as sprites before `AssetDatabase.LoadAssetAtPath<Sprite>` can find them.
- **Sprint scene not re-committed.** `DemoSprintArtConfigurator.Configure` re-authors clips and also rewrites `MG_Sprint.unity` and the runner prefab. Only the hero `.anim` clips carry the change; other rewritten files are inspected and reverted if they are just re-serialisation.

## Review Focus

1. **Two balls on a save.** A keeper pose with a ball drawn in plus the live ball. Expect: no keeper slot ends with `Ball` (Task 2, `FootballKicksFromBehindAgainstTheStudentKeeper`).
2. **Save area drifting from the drawn keeper.** Expect: `KeeperSilhouetteTests` ≥ 0.85 against `StudentKeeper_ready`, and the centre shot still `Saved` (`FootballShotSolverTests.KeeperReactsAfterDelayAndSavesActualContact`) (Task 2, Step 4).
3. **Marker covering the hero's raised spike hand.** Expect: `MarkersSitAboveTheTallestPoseOfEachAthlete` passes with the new frames (Task 3, Step 4).
4. **Rivals/opponent accidentally switched.** A shared pose list edited for everyone. Expect: rival Fail clip stays `fallDown`, volleyball opponent stays `FemaleAdventurer_idle`/`run*`, Tân Thủ's dialogue Cheer stays `_cheer0` (Tasks 3, 4, 6).
5. **Unrelated scene changes committed.** A configurator re-run rewrites tuned scenes (Sprint was hand-tuned in recent commits). Expect: each task's `git status` check keeps only the files listed in that task.

---

### Task 0: Preflight

**Files:** none

- [ ] **Step 1: Confirm a clean tree**

Run: `git status --short`
Expected: only `?? Assets/_Project/Scripts/Gameplay/Sprint/RunnerBreathing.cs.meta` and `?? chay-tron-the-chat-sports-supplement.zip`. If any tracked file shows ` M`, **stop and ask the user**: configurator runs overwrite scenes, so uncommitted scene work would be lost.

- [ ] **Step 2: Baseline EditMode (Unity Editor closed)**

Run: `tools/run-unity-tests.sh EditMode "" baseline-edit`
Record `passed/failed`. Later tasks must not add failures beyond this baseline. Then run `git status --short`; if test runs rewrote any scene, `git checkout -- <that file>` (the tree was clean in Step 1).

---

### Task 1: Import the supplement art through `CharacterArt`

**Files:**
- Create: `Assets/_Project/Art/Characters/MaleAdventurer/MaleAdventurer_<36 poses>.png`, `Assets/_Project/Art/Characters/BossPE/BossPE_<8 poses>.png`, `Assets/_Project/Art/Characters/StudentKeeper/StudentKeeper_<12 poses>.png`, `Assets/_Project/Art/UI/Portraits/<12>.png` (+ Unity-generated `.meta` and folder `.meta`)
- Modify: `Assets/Editor/CharacterArt.cs`
- Modify: `Assets/Tests/EditMode/EditorTools/CharacterArtTests.cs`
- Modify: `Assets/_Project/CREDITS.md`

**Interfaces:**
- Produces: `CharacterArt.Keeper` (`const string "StudentKeeper"`), `CharacterArt.HeroPoses` (`string[]`, 54), `CharacterArt.BossPoses` (now 38), `CharacterArt.KeeperPoses` (`string[]`, 12), `CharacterArt.PortraitRoot` (`const string`), `CharacterArt.Portraits` (`string[]`, 12), `CharacterArt.PortraitPath(string name)` → `string`. `CharacterArt.ImportAll()` now imports all of them plus portraits.

- [ ] **Step 1: Copy the PNGs**

```bash
TMP="$(mktemp -d)"
unzip -q chay-tron-the-chat-sports-supplement.zip \
  'Assets/_Project/Art/Characters/HeroExtra/*' 'Assets/_Project/Art/Characters/BossPEExtra/*' \
  'Assets/_Project/Art/Characters/StudentKeeper/*' 'Assets/_Project/Art/UI/Portraits/*' -d "$TMP"
A=Assets/_Project/Art
mkdir -p $A/Characters/StudentKeeper $A/UI/Portraits
cp -n "$TMP/$A/Characters/HeroExtra/"*.png $A/Characters/MaleAdventurer/
cp -n "$TMP/$A/Characters/BossPEExtra/"*.png $A/Characters/BossPE/
cp -n "$TMP/$A/Characters/StudentKeeper/"*.png $A/Characters/StudentKeeper/
cp -n "$TMP/$A/UI/Portraits/"*.png $A/UI/Portraits/
rm -rf "$TMP"
ls $A/Characters/MaleAdventurer/*.png | wc -l; ls $A/Characters/BossPE/*.png | wc -l
ls $A/Characters/StudentKeeper/*.png | wc -l; ls $A/UI/Portraits/*.png | wc -l
```
Expected counts: `54`, `38`, `12`, `12`. (`cp -n` guarantees no existing file is overwritten; no name collides.)

- [ ] **Step 2: Write the failing tests**

In `CharacterArtTests.cs` add `using UnityEditor;`, then replace `EveryPoseImportsAtRunnerSize` with the version below and add the three new tests and the helper:

```csharp
        [Test]
        public void EveryPoseImportsAtRunnerSize()
        {
            CharacterArt.ImportAll();
            foreach (string character in CharacterArt.Characters)
                AssertRunnerSize(character, CharacterArt.Poses);
        }

        [Test]
        public void SupplementPosesImportAtRunnerSize()
        {
            CharacterArt.ImportAll();
            AssertRunnerSize(CharacterArt.Hero, CharacterArt.HeroPoses);
            AssertRunnerSize(CharacterArt.Boss, CharacterArt.BossPoses);
            AssertRunnerSize(CharacterArt.Keeper, CharacterArt.KeeperPoses);
        }

        [Test]
        public void PoseListsCoverTheSupplement()
        {
            Assert.That(CharacterArt.HeroPoses, Has.Length.EqualTo(54).And.Unique);
            Assert.That(CharacterArt.HeroPoses, Does.Contain("footballBackKick").And.Contain("volleySpikeContact")
                .And.Contain("frogAirRight").And.Contain("disappointed"));
            Assert.That(CharacterArt.BossPoses, Has.Length.EqualTo(38).And.Unique);
            Assert.That(CharacterArt.BossPoses, Does.Contain("scoreWrite").And.Contain("congratulate"));
            Assert.That(CharacterArt.KeeperPoses, Has.Length.EqualTo(12).And.Unique);
            Assert.That(CharacterArt.KeeperPoses, Does.Contain("ready").And.Contain("holdBall"));
            Assert.That(CharacterArt.Keeper, Is.EqualTo("StudentKeeper"));
            Assert.That(CharacterArt.Characters, Does.Not.Contain(CharacterArt.Keeper),
                "Characters lists only the folders holding all 18 shared poses.");
        }

        [Test]
        public void PortraitsImportAsUiSprites()
        {
            CharacterArt.ImportAll();
            Assert.That(CharacterArt.Portraits, Has.Length.EqualTo(12));
            foreach (string name in CharacterArt.Portraits)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CharacterArt.PortraitPath(name));
                Assert.That(sprite, Is.Not.Null, name);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(256f, 256f)), name);
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(128f, 128f)), name);
                Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Bilinear), name);
            }
        }

        static void AssertRunnerSize(string character, string[] poses)
        {
            foreach (string pose in poses)
            {
                Sprite sprite = CharacterArt.Load(character, pose);
                string label = character + " " + pose;
                Assert.That(sprite.texture.width, Is.EqualTo(192), label);
                Assert.That(sprite.texture.height, Is.EqualTo(256), label);
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(CharacterArt.PixelsPerUnit), label);
                // The Sprint lanes were tuned for 0.96 x 1.28 runners standing on their pivot.
                Assert.That(sprite.bounds.size.x, Is.EqualTo(.96f).Within(.001f), label);
                Assert.That(sprite.bounds.size.y, Is.EqualTo(1.28f).Within(.001f), label);
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(96f, 0f)), label);
                Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Bilinear), label);
                Assert.That(CharacterArt.IsPoseOf(sprite, character), Is.True, label);
            }
        }
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.CharacterArtTests" t1-red`
Expected: no results / compile errors naming `HeroPoses`, `KeeperPoses`, `Keeper`, `Portraits`, `PortraitPath` (the script prints `error CS` lines from the log).

- [ ] **Step 4: Implement in `CharacterArt.cs`**

Replace the block from `public const string Boss = "BossPE";` through the end of `ImportAll()` with:

```csharp
        public const string Boss = "BossPE";

        /// BossPE has the 18 shared poses plus 20 of its own.
        public static readonly string[] BossPoses = Poses.Concat(new[]
        {
            "idleBoss", "whistle0", "whistle1", "command", "ready", "taunt",
            "chessThink", "chessMove", "strictLook", "penalty0", "penalty1", "count",
            "angry", "clap0", "clap1", "congratulate", "handsOnHips", "scoreHold", "scoreWrite", "think"
        }).ToArray();

        /// The hero has the 18 shared poses plus the sports supplement's 36.
        public static readonly string[] HeroPoses = Poses.Concat(new[]
        {
            "sprintStart", "sprintLaunch", "sprintFinish", "fallForwardRight", "fallSitRight", "getUpRight",
            "frogReadyRight", "frogTakeoffRight", "frogAirRight", "frogLandRight",
            "volleyReady", "volleyReceive", "volleyDig", "volleySet", "volleyRecover", "volleyJumpLoad",
            "volleySpikeWindup", "volleySpikeContact", "volleySpikeFollow", "volleyLand",
            "volleyShuffleLeft", "volleyShuffleRight",
            "footballBackIdle", "footballBackApproach", "footballBackWindup", "footballBackKick",
            "footballBackFollow", "footballBackCelebrate",
            "happy", "celebrate", "thumbsUp", "fear", "pokerFace", "surprise", "think", "disappointed"
        }).ToArray();

        /// <summary>The football goalkeeper: a different student, with only goalkeeping poses.</summary>
        public const string Keeper = "StudentKeeper";

        /// Left/Right mean screen left/right. The *Ball poses have a ball drawn in.
        public static readonly string[] KeeperPoses =
        {
            "idle", "ready", "reachLeftEmpty", "reachRightEmpty", "diveLeftEmpty", "diveRightEmpty",
            "catchLeftBall", "catchRightBall", "holdBall", "highCatchBall", "recover", "cheer"
        };

        public const string PortraitRoot = "Assets/_Project/Art/UI/Portraits/";
        const float PortraitPixelsPerUnit = 100f;
        const int PortraitMaxTextureSize = 256;

        /// 256x256 head-and-shoulders portraits for UI; nothing references them yet.
        public static readonly string[] Portraits =
        {
            "BossPE_angry", "BossPE_congratulate", "BossPE_handsOnHips", "BossPE_think",
            "Hero_celebrate", "Hero_disappointed", "Hero_fear", "Hero_happy",
            "Hero_pokerFace", "Hero_surprise", "Hero_think", "Hero_thumbsUp"
        };

        public static string PosePath(string character, string pose) =>
            Root + character + "/" + character + "_" + pose + ".png";

        public static string PortraitPath(string name) => PortraitRoot + name + ".png";

        /// <summary>
        /// Imports every pose before anything is loaded: reimporting a texture invalidates Sprite
        /// references handed out earlier, so importing and loading must not interleave.
        /// </summary>
        public static void ImportAll()
        {
            foreach (string character in Characters)
                foreach (string pose in character == Hero ? HeroPoses : Poses)
                    Import(PosePath(character, pose), PixelsPerUnit, MaxTextureSize, SpriteAlignment.BottomCenter);
            foreach (string pose in BossPoses)
                Import(PosePath(Boss, pose), PixelsPerUnit, MaxTextureSize, SpriteAlignment.BottomCenter);
            foreach (string pose in KeeperPoses)
                Import(PosePath(Keeper, pose), PixelsPerUnit, MaxTextureSize, SpriteAlignment.BottomCenter);
            foreach (string name in Portraits)
                Import(PortraitPath(name), PortraitPixelsPerUnit, PortraitMaxTextureSize, SpriteAlignment.Center);
        }
```

Delete the old `PosePath` (it moved into the block above). Then replace the `Import(string path)` method with:

```csharp
        static void Import(string path, float pixelsPerUnit, int maxTextureSize, SpriteAlignment alignment)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("[KMA] Missing character art: " + path, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var current = new TextureImporterSettings();
            importer.ReadTextureSettings(current);
            // Skipping an already-correct texture keeps earlier Sprite references alive.
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit) &&
                importer.filterMode == FilterMode.Bilinear && !importer.mipmapEnabled &&
                importer.alphaIsTransparency && importer.wrapMode == TextureWrapMode.Clamp &&
                importer.maxTextureSize == maxTextureSize &&
                current.spriteMeshType == SpriteMeshType.FullRect &&
                current.spriteAlignment == (int)alignment)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = maxTextureSize;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)alignment;
            settings.spritePivot = alignment == SpriteAlignment.BottomCenter ? new Vector2(.5f, 0f) : new Vector2(.5f, .5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.CharacterArtTests" t1-green`
Expected: `result=Passed`, `failed=0`, 7 tests. Also run `tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.BossArtImportTests" t1-boss` → `result=Passed`.

- [ ] **Step 6: Credit the art**

Append to `Assets/_Project/CREDITS.md`, after the `## Character sprites` section's pose line (`Each folder holds the same poses: ...`):

```markdown

### Sports supplement (2026-10-10)

`chay-tron-the-chat-sports-supplement.zip`, project-generated with image generation from the same
art brief, cut to 192 × 256 RGBA PNGs: 36 extra hero poses (sprint, falls, frog jump, volleyball,
back-view football, expressions) in `Characters/MaleAdventurer/`, 8 extra teacher gestures in
`Characters/BossPE/`, the football goalkeeper `Characters/StudentKeeper/` (12 poses, a different
student from the hero), and 12 256 × 256 portraits in `UI/Portraits/` (not used yet).
```

- [ ] **Step 7: Commit**

```bash
git status --short   # expect only the new PNGs/.meta, the two folder .meta files, CharacterArt.cs, CharacterArtTests.cs, CREDITS.md
git add Assets/_Project/Art/Characters/MaleAdventurer Assets/_Project/Art/Characters/BossPE \
  Assets/_Project/Art/Characters/StudentKeeper Assets/_Project/Art/Characters/StudentKeeper.meta \
  Assets/_Project/Art/UI/Portraits Assets/_Project/Art/UI/Portraits.meta \
  Assets/Editor/CharacterArt.cs Assets/Tests/EditMode/EditorTools/CharacterArtTests.cs Assets/_Project/CREDITS.md
git commit -m "feat(art): import the sports supplement hero, teacher, keeper and portrait art"
```
If a `.meta` for a new PNG is missing, the batch run did not import it; rerun Step 5 (it imports) before committing.

---

### Task 2: Football: back-view kicker, student keeper, retuned save capsules

**Files:**
- Create: `Assets/Tests/EditMode/EditorTools/SupplementPoseWiringTests.cs`
- Modify: `Assets/Editor/FootballSceneConfigurator.cs:29-30, 141-150`
- Modify: `Assets/_Project/Scripts/Gameplay/Football/FootballFlightSimulation.cs:151-158`
- Regenerate: `Assets/_Project/Scenes/MG_Football.unity`

**Interfaces:**
- Consumes: `CharacterArt.Keeper`, `CharacterArt.HeroPoses` (Task 1).
- Produces: `SupplementPoseWiringTests` class with helper `static string PoseName(Sprite sprite)` (file name without extension, e.g. `"MaleAdventurer_idle"`; `"<null>"` for null) and `static string[] PoseNames(IEnumerable<Sprite> sprites)`, used by Tasks 3–6.

- [ ] **Step 1: Write the failing test**

Create `Assets/Tests/EditMode/EditorTools/SupplementPoseWiringTests.cs`:

```csharp
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KMA.EditorTools;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KMA.Tests.EditorTools
{
    /// <summary>Which sports-supplement pose each saved scene and asset draws.</summary>
    public sealed class SupplementPoseWiringTests
    {
        [TearDown]
        public void ReleaseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void FootballKicksFromBehindAgainstTheStudentKeeper()
        {
            EditorSceneManager.OpenScene(FootballSceneConfigurator.ScenePath, OpenSceneMode.Single);
            FootballPoseSprites poses = Object.FindFirstObjectByType<FootballPresentation>().Poses;
            Assert.That(PoseNames(new[] { poses.kickerReady, poses.kickerRunUp, poses.kickerStrike, poses.kickerCelebrate }),
                Is.EqualTo(new[]
                {
                    "MaleAdventurer_footballBackIdle", "MaleAdventurer_footballBackApproach",
                    "MaleAdventurer_footballBackKick", "MaleAdventurer_footballBackCelebrate"
                }));
            string[] keeper = PoseNames(new[] { poses.keeperReady, poses.keeperSave, poses.keeperBeaten });
            Assert.That(keeper, Is.EqualTo(new[] { "StudentKeeper_ready", "StudentKeeper_cheer", "StudentKeeper_recover" }));
            // The live ball stays visible on a save, so a keeper drawn holding one would show two balls.
            Assert.That(keeper, Has.None.EndWith("Ball"));
            Assert.That(PoseName(GameObject.Find("FootballWorld/Goalkeeper").GetComponent<SpriteRenderer>().sprite),
                Is.EqualTo("StudentKeeper_ready"));
        }

        internal static string PoseName(Sprite sprite) =>
            sprite == null ? "<null>" : Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(sprite));

        internal static string[] PoseNames(IEnumerable<Sprite> sprites) => sprites.Select(PoseName).ToArray();
    }
}
#endif
```


- [ ] **Step 2: Run the test to verify it fails**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.SupplementPoseWiringTests" t2-red`
Expected: FAIL, kicker names are `MaleAdventurer_back`, `MaleAdventurer_climb0`, ….

- [ ] **Step 3: Rewire the configurator**

In `FootballSceneConfigurator.cs` replace lines 29–30:

```csharp
        public const string KeeperCharacter = CharacterArt.Keeper;
        public const string KeeperReadyPose = "ready";
```

and replace `LoadPoses()`:

```csharp
        /// <summary>The hero kicks, seen from behind; the student keeper faces him.</summary>
        // Keeper poses never have a ball drawn in: the live ball stays visible on a save.
        static FootballPoseSprites LoadPoses() => new FootballPoseSprites
        {
            kickerReady = CharacterArt.Load(CharacterArt.Hero, "footballBackIdle"),
            kickerRunUp = CharacterArt.Load(CharacterArt.Hero, "footballBackApproach"),
            kickerStrike = CharacterArt.Load(CharacterArt.Hero, "footballBackKick"),
            kickerCelebrate = CharacterArt.Load(CharacterArt.Hero, "footballBackCelebrate"),
            keeperReady = CharacterArt.Load(KeeperCharacter, KeeperReadyPose),
            keeperSave = CharacterArt.Load(KeeperCharacter, "cheer"),
            keeperBeaten = CharacterArt.Load(KeeperCharacter, "recover")
        };
```

- [ ] **Step 4: Retune the keeper capsules**

In `FootballFlightSimulation.cs` replace the comment and array at lines 151–158 with:

```csharp
        // `StudentKeeper` `ready` pose (crouched, gloves out wide) drawn FootballPresentation.KeeperDisplayWidth x Height,
        // feet KeeperFeetDrop px below the hip. Head, torso, arms, gloves, legs, right shoe.
        static readonly Capsule[] KeeperCapsules = {
            new Capsule(5,-66,8,-59,14),
            new Capsule(-2,-40,-3,-22,19),
            new Capsule(-18,-41,-22,-32,5), new Capsule(17,-40,19,-29,6),
            new Capsule(-18,-32,-19,-27,4), new Capsule(28,-30,26,-26,7),
            new Capsule(-16,-24,-31,11,4), new Capsule(19,-16,16,8,5), new Capsule(19,9,30,10,4)
        };
```

(Fitted offline by reproducing `KeeperSilhouetteTests` against `StudentKeeper_ready.png`: recall 0.910, precision 0.910.)

- [ ] **Step 5: Regenerate the scene**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit -projectPath "$(pwd -W)" \
  -executeMethod KMA.EditorTools.FootballSceneConfigurator.BuildScene -logFile Builds/t2-football.log; echo "exit=$?"
```
Expected: `exit=0`.

- [ ] **Step 6: Run the tests to verify they pass**

```bash
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.SupplementPoseWiringTests" t2-green
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.KeeperSilhouetteTests" t2-silhouette
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.HeroConsistencyTests" t2-hero
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.FootballSceneConfiguratorTests" t2-config
tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Ball" t2-ball
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Football" t2-play
```
Expected: all `result=Passed`. If `KeeperReactsAfterDelayAndSavesActualContact` fails (centre shot not saved), stop and report it with the output; do not change the shot tuning.

- [ ] **Step 7: Commit**

```bash
git status --short   # expect: MG_Football.unity, FootballSceneConfigurator.cs, FootballFlightSimulation.cs, the new test + .meta
git add Assets/_Project/Scenes/MG_Football.unity Assets/Editor/FootballSceneConfigurator.cs \
  Assets/_Project/Scripts/Gameplay/Football/FootballFlightSimulation.cs \
  Assets/Tests/EditMode/EditorTools/SupplementPoseWiringTests.cs Assets/Tests/EditMode/EditorTools/SupplementPoseWiringTests.cs.meta
git commit -m "feat(football): back-view kicker and student keeper art with matching save capsules"
```
If any other tracked file changed, inspect `git diff <file>`; revert re-serialisation noise with `git checkout -- <file>`, and stop and report anything else.

---

### Task 3: Volleyball: hero volleyball poses, opponent unchanged

**Files:**
- Modify: `Assets/Editor/VolleyballSceneConfigurator.cs:26-27, 208-209, 238-253`
- Modify: `Assets/Tests/EditMode/EditorTools/SupplementPoseWiringTests.cs`
- Regenerate: `Assets/_Project/Scenes/MG_Volleyball.unity`

**Interfaces:**
- Consumes: `SupplementPoseWiringTests.PoseNames` (Task 2), `CharacterArt.HeroPoses` (Task 1).

- [ ] **Step 1: Write the failing test**

Add `using KMA.Gameplay.Volleyball;` to `SupplementPoseWiringTests.cs` and this test inside the class:

```csharp
        [Test]
        public void VolleyballPlayerUsesVolleyballPosesAndTheOpponentKeepsHers()
        {
            EditorSceneManager.OpenScene(VolleyballSceneConfigurator.ScenePath, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<VolleyballController>();
            string[] Player(AthleteAction action) => PoseNames(controller.PlayerView.FramesFor(action));
            string[] Opponent(AthleteAction action) => PoseNames(controller.OpponentView.FramesFor(action));

            Assert.That(Player(AthleteAction.Idle), Is.EqualTo(new[] { "MaleAdventurer_volleyReady" }));
            Assert.That(Player(AthleteAction.Run),
                Is.EqualTo(new[] { "MaleAdventurer_volleyShuffleRight", "MaleAdventurer_volleyShuffleLeft" }));
            Assert.That(Player(AthleteAction.Receive),
                Is.EqualTo(new[] { "MaleAdventurer_volleyDig", "MaleAdventurer_volleyRecover" }));
            Assert.That(Player(AthleteAction.Smash),
                Is.EqualTo(new[] { "MaleAdventurer_volleySpikeWindup", "MaleAdventurer_volleySpikeContact" }));
            Assert.That(Player(AthleteAction.Block),
                Is.EqualTo(new[] { "MaleAdventurer_volleyJumpLoad", "MaleAdventurer_volleySet" }));
            Assert.That(Player(AthleteAction.Dive),
                Is.EqualTo(new[] { "MaleAdventurer_fallForwardRight", "MaleAdventurer_fallSitRight" }));

            Assert.That(Opponent(AthleteAction.Idle), Is.EqualTo(new[] { "FemaleAdventurer_idle" }));
            Assert.That(Opponent(AthleteAction.Run), Is.EqualTo(new[]
                { "FemaleAdventurer_run0", "FemaleAdventurer_run1", "FemaleAdventurer_run2", "FemaleAdventurer_run1" }));
            Assert.That(Opponent(AthleteAction.Smash), Is.EqualTo(new[] { "FemaleAdventurer_jump", "FemaleAdventurer_attack1" }));
        }
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.SupplementPoseWiringTests" t3-red`
Expected: the volleyball test FAILS (player idle is `MaleAdventurer_idle`); the football test still passes.

- [ ] **Step 3: Per-character pose sets**

In `VolleyballSceneConfigurator.cs`:

Replace lines 26–27:

```csharp
        // Marker centre, in world units above the feet; its lowest point clears the tallest (spike contact) pose.
        const float MarkerWorldHeight = 3.25f;
```

Add below `OpponentCharacter` (line 23):

```csharp
        /// <summary>Pose names per action, in VolleyAthleteView.Configure order.</summary>
        sealed class AthletePoses
        {
            public string[] Idle, Run, Receive, Smash, Block, Dive;
        }

        /// The hero has dedicated volleyball art; the opponent keeps the shared poses.
        static readonly AthletePoses HeroVolleyPoses = new AthletePoses
        {
            Idle = new[] { "volleyReady" },
            Run = new[] { "volleyShuffleRight", "volleyShuffleLeft" },
            Receive = new[] { "volleyDig", "volleyRecover" },
            Smash = new[] { "volleySpikeWindup", "volleySpikeContact" },
            Block = new[] { "volleyJumpLoad", "volleySet" },
            Dive = new[] { "fallForwardRight", "fallSitRight" }
        };

        static readonly AthletePoses SharedVolleyPoses = new AthletePoses
        {
            Idle = new[] { "idle" },
            Run = new[] { "run0", "run1", "run2", "run1" },
            Receive = new[] { "duck", "hold" },
            Smash = new[] { "jump", "attack1" },
            Block = new[] { "jump", "cheer1" },
            Dive = new[] { "fall", "slide" }
        };
```

Change lines 208–209 to:

```csharp
            VolleyAthleteView player = Athlete("Player", false, CharacterArt.Hero, HeroVolleyPoses);
            VolleyAthleteView opponent = Athlete("Opponent", true, OpponentCharacter, SharedVolleyPoses);
```

Replace `Athlete(...)`:

```csharp
        static VolleyAthleteView Athlete(string name, bool mirror, string character, AthletePoses poses)
        {
            Sprite[] Poses(string[] names) => CharacterArt.Frames(character, names);
            Sprite[] idle = Poses(poses.Idle);
            SpriteRenderer body = Renderer(name, idle[0], Vector3.zero, 0);
            body.transform.localScale = Vector3.one * AthleteScale;
            var book = body.gameObject.AddComponent<SpriteFlipbook>();
            book.Configure(body, idle, true, 12f);
            var view = body.gameObject.AddComponent<VolleyAthleteView>();
            view.Configure(body, book, mirror, idle, Poses(poses.Run), Poses(poses.Receive), Poses(poses.Smash),
                Poses(poses.Block), Poses(poses.Dive));
            return view;
        }
```

- [ ] **Step 4: Regenerate and run the tests**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit -projectPath "$(pwd -W)" \
  -executeMethod KMA.EditorTools.VolleyballSceneConfigurator.BuildScene -logFile Builds/t3-volley.log; echo "exit=$?"
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.SupplementPoseWiringTests" t3-green
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.VolleyballSceneConfiguratorTests" t3-config
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.HeroConsistencyTests" t3-hero
tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball" t3-edit
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Volleyball" t3-play
```
Expected: `exit=0`, then all `result=Passed`, including `MarkersSitAboveTheTallestPoseOfEachAthlete`. The `VolleyballSceneConfiguratorTests` themselves call `BuildScene()` and rewrite `MG_Volleyball.unity` with the same content.

- [ ] **Step 5: Commit**

```bash
git status --short   # expect: MG_Volleyball.unity, VolleyballSceneConfigurator.cs, SupplementPoseWiringTests.cs
git add Assets/_Project/Scenes/MG_Volleyball.unity Assets/Editor/VolleyballSceneConfigurator.cs \
  Assets/Tests/EditMode/EditorTools/SupplementPoseWiringTests.cs
git commit -m "feat(volleyball): draw the player with the supplement volleyball poses"
```
(Same rule as Task 2 for any other changed file.)

---

### Task 4: Frog jump and sprint falls

**Files:**
- Modify: `Assets/Editor/FrogJumpSceneConfigurator.cs:72-74`
- Modify: `Assets/Editor/DemoSprintArtConfigurator.cs:193-202`
- Modify: `Assets/Tests/EditMode/EditorTools/SupplementPoseWiringTests.cs`
- Regenerate: `Assets/_Project/Scenes/MG_FrogJump.unity`, `Assets/_Project/Animations/MaleAdventurer_Stumble.anim`, `Assets/_Project/Animations/MaleAdventurer_Fail.anim`

**Interfaces:**
- Consumes: `SupplementPoseWiringTests.PoseName/PoseNames` (Task 2).

- [ ] **Step 1: Write the failing tests**

Add `using KMA.Gameplay.FrogJump;` to `SupplementPoseWiringTests.cs` and inside the class:

```csharp
        [Test]
        public void FrogJumpHopsAndFallsWithTheSupplementPoses()
        {
            EditorSceneManager.OpenScene(FrogJumpSceneConfigurator.ScenePath, OpenSceneMode.Single);
            var view = new SerializedObject(Object.FindFirstObjectByType<FrogJumpView>());
            Assert.That(Wired(view, "squatPose"), Is.EqualTo("MaleAdventurer_frogReadyRight"));
            Assert.That(Wired(view, "jumpPose"), Is.EqualTo("MaleAdventurer_frogAirRight"));
            Assert.That(Wired(view, "fallPose"), Is.EqualTo("MaleAdventurer_fallSitRight"));
        }

        [Test]
        public void SprintHeroFallsWithTheSupplementPosesAndRivalsKeepTheirs()
        {
            Assert.That(ClipPoses("MaleAdventurer_Stumble"), Is.Not.Empty.And.All.EqualTo("MaleAdventurer_fallForwardRight"));
            Assert.That(ClipPoses("MaleAdventurer_Fail"), Is.Not.Empty.And.All.EqualTo("MaleAdventurer_fallSitRight"));
            // Idle also plays mid-race while the player stops tapping, so it keeps the standing pose.
            Assert.That(ClipPoses("MaleAdventurer_Idle"), Is.Not.Empty.And.All.EqualTo("MaleAdventurer_idle"));
            Assert.That(ClipPoses("FemalePerson_Fail"), Is.Not.Empty.And.All.EqualTo("FemalePerson_fallDown"));
            Assert.That(ClipPoses("FemalePerson_Stumble"), Is.Not.Empty.And.All.EqualTo("FemalePerson_hurt"));
        }

        static string Wired(SerializedObject component, string field) =>
            PoseName((Sprite)component.FindProperty(field).objectReferenceValue);

        static string[] ClipPoses(string clipName)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/" + clipName + ".anim");
            Assert.That(clip, Is.Not.Null, clipName);
            return AnimationUtility.GetObjectReferenceCurveBindings(clip)
                .SelectMany(binding => AnimationUtility.GetObjectReferenceCurve(clip, binding))
                .Select(key => PoseName(key.value as Sprite)).ToArray();
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.SupplementPoseWiringTests" t4-red`
Expected: both new tests FAIL (`MaleAdventurer_duck`, `MaleAdventurer_hurt`); the football and volleyball tests pass.

- [ ] **Step 3: Rewire both configurators**

`FrogJumpSceneConfigurator.cs` lines 72–74:

```csharp
            Sprite squat = CharacterArt.Load(CharacterArt.Hero, "frogReadyRight");
            Sprite jump = CharacterArt.Load(CharacterArt.Hero, "frogAirRight");
            Sprite fall = CharacterArt.Load(CharacterArt.Hero, "fallSitRight");
```

`DemoSprintArtConfigurator.cs`, replace `LoadCharacter`:

```csharp
        /// <summary>One character's Sprint poses; CharacterArt.ImportAll must have run first.</summary>
        // The hero has its own trip and sit-down falls; the rivals keep the shared poses.
        static RunnerArt LoadCharacter(string folder) => new RunnerArt
        {
            Folder = folder,
            Idle = CharacterArt.Load(folder, "idle"),
            Hit = CharacterArt.Load(folder, folder == PlayerCharacter ? "fallForwardRight" : "hurt"),
            FallDown = CharacterArt.Load(folder, folder == PlayerCharacter ? "fallSitRight" : "fallDown"),
            Run = CharacterArt.Frames(folder, "run0", "run1", "run2"),
            Cheer = CharacterArt.Frames(folder, "cheer0", "cheer1")
        };
```

- [ ] **Step 4: Regenerate**

```bash
U="/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
"$U" -batchmode -quit -projectPath "$(pwd -W)" -executeMethod KMA.EditorTools.FrogJumpSceneConfigurator.BuildScene \
  -logFile Builds/t4-frog.log; echo "exit=$?"
"$U" -batchmode -quit -projectPath "$(pwd -W)" -executeMethod KMA.EditorTools.DemoSprintArtConfigurator.Configure \
  -logFile Builds/t4-sprint.log; echo "exit=$?"
git status --short
```
Expected: both `exit=0`. Keep `MG_FrogJump.unity`, `MaleAdventurer_Stumble.anim`, `MaleAdventurer_Fail.anim`. `Configure` also rewrites `MG_Sprint.unity` and runner prefabs: for each, inspect `git diff <file>`. If it is re-serialisation only (reordered/duplicated fields, no sprite or position change), `git checkout -- <file>`; otherwise stop and report the diff to the user.

- [ ] **Step 5: Run the tests to verify they pass**

```bash
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.SupplementPoseWiringTests" t4-green
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.FrogJumpSceneTests" t4-frog
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.HeroConsistencyTests" t4-hero
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.SprintSceneCleanupTests" t4-sprint
tools/run-unity-tests.sh PlayMode "KMA.Tests.Presentation.RunnerVisualTests" t4-runner
```
Expected: all `result=Passed`.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scenes/MG_FrogJump.unity Assets/_Project/Animations/MaleAdventurer_Stumble.anim \
  Assets/_Project/Animations/MaleAdventurer_Fail.anim Assets/Editor/FrogJumpSceneConfigurator.cs \
  Assets/Editor/DemoSprintArtConfigurator.cs Assets/Tests/EditMode/EditorTools/SupplementPoseWiringTests.cs
git commit -m "feat(art): frog jump hop and hero sprint falls use the supplement poses"
```

---

### Task 5: Chess and celebration expressions

**Files:**
- Modify: `Assets/Editor/ChessFinalSceneConfigurator.cs:1-3, 29-31, 113-114, 155-156, 177-179`
- Modify: `Assets/Editor/CelebrationSceneConfigurator.cs:23-24, 52-53, 101-102`
- Modify: `Assets/Tests/EditMode/EditorTools/KMA.EditorTools.EditMode.Tests.asmdef`
- Modify: `Assets/Tests/EditMode/EditorTools/SupplementPoseWiringTests.cs`
- Regenerate: `Assets/_Project/Scenes/MG_ChessFinal.unity`, `Assets/_Project/Scenes/Celebration.unity`

**Interfaces:**
- Consumes: `SupplementPoseWiringTests.PoseName/PoseNames` (Task 2).
- The controllers keep calling `SetStudent("idle"|"hurt"|"cheer0")` and `SetTeacher("idleBoss"|"taunt"|"chessThink"|"chessMove"|"strictLook"|"cheer0"|...)`. Only the sprite behind each key changes.

- [ ] **Step 1: Reference the two gameplay assemblies from the test assembly**

In `KMA.EditorTools.EditMode.Tests.asmdef`, add to `"references"` after `"KMA.Gameplay.Core"`:

```json
        "KMA.Gameplay.Core",
        "KMA.Gameplay.Chess",
        "KMA.Gameplay.Celebration"
```

- [ ] **Step 2: Write the failing tests**

Add `using KMA.Gameplay.Chess;` and `using KMA.Gameplay.Celebration;` to `SupplementPoseWiringTests.cs` and inside the class:

```csharp
        [Test]
        public void ChessCastShowsTheSupplementExpressions()
        {
            EditorSceneManager.OpenScene(ChessFinalSceneConfigurator.ScenePath, OpenSceneMode.Single);
            var cast = new SerializedObject(Object.FindFirstObjectByType<ChessCastView>());
            Assert.That(CastPose(cast, "studentPoses", "idle"), Is.EqualTo("MaleAdventurer_think"));
            Assert.That(CastPose(cast, "studentPoses", "hurt"), Is.EqualTo("MaleAdventurer_disappointed"));
            Assert.That(CastPose(cast, "studentPoses", "cheer0"), Is.EqualTo("MaleAdventurer_celebrate"));
            Assert.That(CastPose(cast, "studentPoses", "cheer1"), Is.EqualTo("MaleAdventurer_happy"));
            Assert.That(CastPose(cast, "teacherPoses", "idleBoss"), Is.EqualTo("BossPE_handsOnHips"));
            Assert.That(CastPose(cast, "teacherPoses", "taunt"), Is.EqualTo("BossPE_angry"));
            Assert.That(CastPose(cast, "teacherPoses", "chessThink"), Is.EqualTo("BossPE_think"));
            Assert.That(CastPose(cast, "teacherPoses", "cheer0"), Is.EqualTo("BossPE_congratulate"));
            Assert.That(CastPose(cast, "teacherPoses", "chessMove"), Is.EqualTo("BossPE_chessMove"));
            Assert.That(CastPose(cast, "teacherPoses", "strictLook"), Is.EqualTo("BossPE_strictLook"));
        }

        [Test]
        public void CelebrationCastShowsTheSupplementExpressions()
        {
            EditorSceneManager.OpenScene(CelebrationSceneConfigurator.ScenePath, OpenSceneMode.Single);
            var controller = new SerializedObject(Object.FindFirstObjectByType<CelebrationSceneController>());
            // Slots 1 and 2 alternate every 0.35 s, so they stay the designed cheer0/cheer1 pair.
            Assert.That(Frames(controller, "studentFrames"),
                Is.EqualTo(new[] { "MaleAdventurer_happy", "MaleAdventurer_cheer0", "MaleAdventurer_cheer1" }));
            Assert.That(Frames(controller, "classmateFrames"),
                Is.EqualTo(new[] { "FemalePerson_idle", "FemalePerson_cheer0", "FemalePerson_cheer1" }));
            Assert.That(Frames(controller, "teacherFrames"),
                Is.EqualTo(new[] { "BossPE_idleBoss", "BossPE_clap0", "BossPE_congratulate" }));
        }

        static string CastPose(SerializedObject cast, string list, string key)
        {
            SerializedProperty poses = cast.FindProperty(list);
            for (int i = 0; i < poses.arraySize; i++)
            {
                SerializedProperty pose = poses.GetArrayElementAtIndex(i);
                if (pose.FindPropertyRelative("name").stringValue == key)
                    return PoseName((Sprite)pose.FindPropertyRelative("sprite").objectReferenceValue);
            }
            return "<no pose " + key + ">";
        }

        static string[] Frames(SerializedObject component, string field)
        {
            SerializedProperty frames = component.FindProperty(field);
            return Enumerable.Range(0, frames.arraySize)
                .Select(i => PoseName((Sprite)frames.GetArrayElementAtIndex(i).objectReferenceValue)).ToArray();
        }
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.SupplementPoseWiringTests" t5-red`
Expected: the chess and celebration tests FAIL (`MaleAdventurer_idle`); the earlier tests pass.

- [ ] **Step 4: Chess: key → art table**

In `ChessFinalSceneConfigurator.cs` add `using System.Collections.Generic;` under `using System;`. Below `TeacherPoses` add:

```csharp
        // Pose keys the controller asks for → the supplement art drawing them; other keys draw the file of the same name.
        static readonly Dictionary<string, string> StudentArt = new Dictionary<string, string>
        {
            { "idle", "think" }, { "hurt", "disappointed" }, { "cheer0", "celebrate" }, { "cheer1", "happy" }
        };
        static readonly Dictionary<string, string> TeacherArt = new Dictionary<string, string>
        {
            { "idleBoss", "handsOnHips" }, { "taunt", "angry" }, { "chessThink", "think" }, { "cheer0", "congratulate" }
        };
```

Lines 113–114 become:

```csharp
            Image student = Avatar(parent, "Student", StudentX, Art(CharacterArt.Hero, StudentArt, "idle"));
            Image teacher = Avatar(parent, "Teacher", TeacherX, Art(CharacterArt.Boss, TeacherArt, "idleBoss"));
```

Lines 155–156 become:

```csharp
            cast.Configure(student, teacher, Poses(CharacterArt.Hero, StudentPoses, StudentArt),
                Poses(CharacterArt.Boss, TeacherPoses, TeacherArt), bubble.gameObject, bubbleText);
```

Replace `Poses(...)` (line 177) with:

```csharp
        static ChessCastView.Pose[] Poses(string character, string[] names, IReadOnlyDictionary<string, string> art) => names
            .Select(name => new ChessCastView.Pose { name = name, sprite = Art(character, art, name) })
            .ToArray();

        static Sprite Art(string character, IReadOnlyDictionary<string, string> art, string key) =>
            CharacterArt.Load(character, art.TryGetValue(key, out string file) ? file : key);
```

- [ ] **Step 5: Celebration: separate hero, classmate and teacher sets**

In `CelebrationSceneConfigurator.cs` replace lines 23–24:

```csharp
        // Slot order: arrive, cheer A, cheer B. Slots 1 and 2 alternate, so they stay the designed cheer pair.
        static readonly string[] HeroPoses = { "happy", "cheer0", "cheer1" };
        static readonly string[] ClassmatePoses = { "idle", "cheer0", "cheer1" };
        // Slot order: waiting, first reaction, final cheer.
        static readonly string[] TeacherPoses = { "idleBoss", "clap0", "congratulate" };
```

Line 52: `CharacterArt.Load(CharacterArt.Hero, "idle")` → `CharacterArt.Load(CharacterArt.Hero, HeroPoses[0])`.

Lines 101–102:

```csharp
            controller.Configure(student, classmate, teacher, Poses(CharacterArt.Hero, HeroPoses),
                Poses(Classmate, ClassmatePoses), Poses(CharacterArt.Boss, TeacherPoses), bubble.gameObject, bubbleText,
```

(The classmate avatar at line 50 still loads `"idle"`; leave it.)

- [ ] **Step 6: Regenerate and run the tests**

```bash
U="/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
"$U" -batchmode -quit -projectPath "$(pwd -W)" -executeMethod KMA.EditorTools.ChessFinalSceneConfigurator.BuildScene \
  -logFile Builds/t5-chess.log; echo "exit=$?"
"$U" -batchmode -quit -projectPath "$(pwd -W)" -executeMethod KMA.EditorTools.CelebrationSceneConfigurator.BuildScene \
  -logFile Builds/t5-celebration.log; echo "exit=$?"
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.SupplementPoseWiringTests" t5-green
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.ChessCelebrationBackdropTests" t5-backdrop
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Chess" t5-chess-play
```
Expected: both `exit=0`, all `result=Passed` (`ChessFinalSceneTests` checks keys `"hurt"`/`"idle"`, which are unchanged).

- [ ] **Step 7: Commit**

```bash
git status --short   # expect: the two scenes, two configurators, the asmdef, the test file
git add Assets/_Project/Scenes/MG_ChessFinal.unity Assets/_Project/Scenes/Celebration.unity \
  Assets/Editor/ChessFinalSceneConfigurator.cs Assets/Editor/CelebrationSceneConfigurator.cs \
  Assets/Tests/EditMode/EditorTools/KMA.EditorTools.EditMode.Tests.asmdef \
  Assets/Tests/EditMode/EditorTools/SupplementPoseWiringTests.cs
git commit -m "feat(chess): student and teacher show the supplement expressions in chess and celebration"
```

---

### Task 6: Start lecturer gestures and journey dialogue expressions

**Files:**
- Modify: `Assets/Editor/StartLecturerAuthoring.cs:85-86`
- Modify: `Assets/Editor/StudentJourneyContentBuilder.cs:174-178, 263-265, 346-348`
- Modify: `Assets/Tests/EditMode/EditorTools/SupplementPoseWiringTests.cs`
- Regenerate: `MG_Sprint.unity`, `MG_Volleyball.unity`, `MG_Football.unity`, `MG_FrogJump.unity` (lecturer only), `Assets/_Project/Resources/Journey/JourneyDialogues.asset`

**Interfaces:**
- Consumes: `SupplementPoseWiringTests.PoseName` (Task 2), `CharacterArt.Hero`, `CharacterArt.Boss`.

- [ ] **Step 1: Write the failing tests**

Inside `SupplementPoseWiringTests`:

```csharp
        [TestCase("MG_Sprint")]
        [TestCase("MG_Volleyball")]
        [TestCase("MG_Football")]
        [TestCase("MG_FrogJump")]
        public void StartLecturerReactsWithTheSupplementGestures(string scene)
        {
            EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{scene}.unity", OpenSceneMode.Single);
            var lecturer = new SerializedObject(Object.FindFirstObjectByType<StartLecturer>());
            Assert.That(Wired(lecturer, "taunt"), Is.EqualTo("BossPE_angry"));
            Assert.That(Wired(lecturer, "cheer"), Is.EqualTo("BossPE_congratulate"));
            Assert.That(Wired(lecturer, "cheerBothArms"), Is.EqualTo("BossPE_cheer1"));
            Assert.That(Wired(lecturer, "penalty"), Is.EqualTo("BossPE_penalty0"));
        }

        [Test]
        public void JourneyHeroAndTeacherReactWithTheSupplementExpressions()
        {
            var library = AssetDatabase.LoadAssetAtPath<JourneyDialogueLibrary>(
                "Assets/_Project/Resources/Journey/JourneyDialogues.asset");
            JourneyCharacter Cast(string id) => library.Cast.Single(character => character.Id == id);

            Assert.That(PoseName(Cast("anh_khoa_tren").GetPose(DialoguePose.Cheer)), Is.EqualTo("MaleAdventurer_celebrate"));
            Assert.That(PoseName(Cast("anh_khoa_tren").GetPose(DialoguePose.Hurt)), Is.EqualTo("MaleAdventurer_disappointed"));
            Assert.That(PoseName(Cast("anh_khoa_tren").GetPose(DialoguePose.Idle)), Is.EqualTo("MaleAdventurer_idle"));
            Assert.That(PoseName(Cast("co_the_chat").GetPose(DialoguePose.Cheer)), Is.EqualTo("BossPE_congratulate"));
            Assert.That(PoseName(Cast("co_the_chat").GetPose(DialoguePose.Hurt)), Is.EqualTo("BossPE_angry"));
            Assert.That(PoseName(Cast("tan_thu").GetPose(DialoguePose.Cheer)), Does.EndWith("_cheer0"));
            Assert.That(PoseName(Cast("mai_toang").GetPose(DialoguePose.Hurt)), Does.EndWith("_hurt"));
        }
```

(`StartLecturer`, `JourneyDialogueLibrary`, `JourneyCharacter` and `DialoguePose` are in namespace `KMA.Gameplay`, already imported.)

- [ ] **Step 2: Run the tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.SupplementPoseWiringTests" t6-red`
Expected: the lecturer tests FAIL (`BossPE_taunt`) and the journey test FAILS (`MaleAdventurer_cheer0`); earlier tests pass.

- [ ] **Step 3: Lecturer gestures**

In `StartLecturerAuthoring.cs` change lines 85–86 (the `taunt` and `cheer` arguments of `ConfigureGestures`):

```csharp
                CharacterArt.Load(CharacterArt.Boss, "angry"),
                CharacterArt.Load(CharacterArt.Boss, "congratulate"),
```

- [ ] **Step 4: Dialogue overrides**

In `StudentJourneyContentBuilder.cs`, below `PoseFiles` add:

```csharp
        // The hero and the teacher have their own reaction art; everyone else draws the shared poses.
        static readonly Dictionary<string, Dictionary<DialoguePose, string>> OwnPoseFiles =
            new Dictionary<string, Dictionary<DialoguePose, string>>
            {
                [CharacterArt.Hero] = new Dictionary<DialoguePose, string>
                    { [DialoguePose.Cheer] = "celebrate", [DialoguePose.Hurt] = "disappointed" },
                [CharacterArt.Boss] = new Dictionary<DialoguePose, string>
                    { [DialoguePose.Cheer] = "congratulate", [DialoguePose.Hurt] = "angry" }
            };
```

In `Character(...)`, replace the `string path = ...` line with:

```csharp
                string file = OwnPoseFiles.TryGetValue(spriteSet, out var own) && own.TryGetValue(pose, out string mine)
                    ? mine : suffix;
                string path = $"{CharacterArtFolder}/{spriteSet}/{spriteSet}_{file}.png";
```

In `BuildDialogues()`, make the first statement:

```csharp
            // New dialogue art must be imported as sprites before it can be loaded below.
            CharacterArt.ImportAll();
```


- [ ] **Step 5: Regenerate**

```bash
U="/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
"$U" -batchmode -quit -projectPath "$(pwd -W)" -executeMethod KMA.EditorTools.StartLecturerAuthoring.AddToSportsMinigames \
  -logFile Builds/t6-lecturer.log; echo "exit=$?"
"$U" -batchmode -quit -projectPath "$(pwd -W)" -executeMethod KMA.EditorTools.StudentJourneyContentBuilder.BuildDialogues \
  -logFile Builds/t6-dialogue.log; echo "exit=$?"
git status --short
```
Expected: both `exit=0`. Expected changes: the four sports scenes (lecturer sprites only) and `JourneyDialogues.asset`. Check `git diff --stat Assets/_Project/Scenes/MG_Sprint.unity`. If the Sprint diff touches anything besides the `StartLecturer` object, stop and report. If `JourneyEmoji` assets changed (`BuildDialogues` rebuilds them), inspect; revert if the content is identical apart from serialisation.

- [ ] **Step 6: Run the tests to verify they pass**

```bash
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.SupplementPoseWiringTests" t6-green
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.StartLecturerSceneTests" t6-lecturer
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.StudentJourneyContentBuilderTests" t6-journey
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.JourneyEmojiAssetTests" t6-emoji
```
Expected: all `result=Passed`.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scenes/MG_Sprint.unity Assets/_Project/Scenes/MG_Volleyball.unity \
  Assets/_Project/Scenes/MG_Football.unity Assets/_Project/Scenes/MG_FrogJump.unity \
  Assets/_Project/Resources/Journey/JourneyDialogues.asset Assets/Editor/StartLecturerAuthoring.cs \
  Assets/Editor/StudentJourneyContentBuilder.cs Assets/Tests/EditMode/EditorTools/SupplementPoseWiringTests.cs
git commit -m "feat(art): teacher and hero reactions use the supplement gestures and expressions"
```
(Only `git add` the scene files that actually changed.)

---

### Task 7: Full verification and screenshots

**Files:**
- Possibly modify: `Assets/Editor/VolleyballSceneConfigurator.cs` (only for the Step 3 fallback)

- [ ] **Step 1: Full test run**

```bash
tools/run-unity-tests.sh EditMode "" final-edit
tools/run-unity-tests.sh PlayMode "" final-play
git status --short
```
Expected: EditMode failures ≤ the Task 0 baseline (ideally 0), PlayMode `result=Passed`. Revert any scene that the test run rewrote (`git checkout -- <file>`; everything is committed at this point).

- [ ] **Step 2: Screenshots**

Use the `testing-unity-ui-with-screenshots` skill (it drives `tools/qa-screenshot.sh`) to capture:
- football: aiming, mid-kick, goal, save;
- volleyball: idle, running, receiving, smashing;
- frog jump: squat, mid-hop, fallen;
- sprint: stumble and fail;
- chess and celebration;
- one journey dialogue line where the hero cheers.

Check each one for:
- no cropped hands, cap, ponytail or gloves;
- feet on the ground (bottom pivot);
- no jump in character size between poses;
- exactly one ball in football;
- the volleyball marker sits clear of the raised spike hand.

- [ ] **Step 3: Volleyball run fallback (only if Step 2 shows a jerky shuffle)**

If alternating `volleyShuffleRight`/`volleyShuffleLeft` reads as jerky, set `HeroVolleyPoses.Run = new[] { "run0", "run1", "run2", "run1" }`. In the same change, update the expected `Player(AthleteAction.Run)` array in `VolleyballPlayerUsesVolleyballPosesAndTheOpponentKeepsHers`, regenerate (Task 3 Step 4), rerun the tests and commit `fix(volleyball): keep the run cycle for the player's run`.

- [ ] **Step 4: Report**

Tell the user:
- the EditMode/PlayMode counts versus the baseline;
- which screenshots looked wrong, if any;
- whether the volleyball run fallback was used;
- that `chay-tron-the-chat-sports-supplement.zip` is still untracked at the repo root and theirs to keep or delete.
