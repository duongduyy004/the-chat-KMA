# One Hero Across Minigames Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sprint, Football and Volleyball draw every character from Kenney Toon Characters, and the player is the same `MaleAdventurer` sprite set in all three.

**Architecture:** One editor class, `ToonCharacterArt`, owns the pose files, their import settings and loading. Each scene configurator (Sprint artwork, Football scene, Volleyball scene) takes its sprites from it instead of from per-game art. Football gains a small runtime pose table (which pose each character shows at each moment), and its keeper collision capsules are refitted to the new keeper sprite. A cross-game EditMode test pins "the player is the hero everywhere and nobody else is".

**Tech Stack:** Unity 6000.3.23f1, C#, URP 2D, Unity Test Framework (NUnit), Unity editor scripting (`TextureImporter`, `AssetDatabase`, `EditorSceneManager`), Git Bash on Windows.

**Spec:** `docs/superpowers/specs/2026-09-29-hero-character-consistency-design.md`

## Global Constraints

- Pack: Kenney Toon Characters, `https://kenney.nl/assets/toon-characters`, CC0 1.0. Use the `PNG/Poses HD` images (192×256).
- Hero: `MaleAdventurer`. The hero is never tinted, recoloured or mirrored by a configurator (Volleyball mirrors only the opponent).
- Cast: Sprint lane 1 `MalePerson`, lane 3 `FemalePerson`, lane 4 `FemaleAdventurer`; Football keeper `MalePerson`; Volleyball opponent `FemaleAdventurer`. `Robot` and `Zombie` are not used.
- Pose files: `Assets/_Project/Art/Characters/<Character>/<Character>_<pose>.png`.
- Import: Sprite (Single), 200 pixels per unit, bottom-centre pivot `(0.5, 0)`, bilinear, no mipmaps, clamp, `FullRect` mesh, max size 512. Each pose is 0.96×1.28 world units.
- The pose set, identical for all four characters: `idle run0 run1 run2 hit cheer0 cheer1 fallDown back climb0 climb1 hurt duck hold jump attack1 slide fall`.
- The Unity Editor must be closed while running batch-mode commands. Batch mode cannot open a project that is already open.
- Python is not installed on this machine, so `tools/run-unity-tests.sh` cannot parse results. Run tests with this command and read the summary line it prints:

  ```bash
  UNITY="/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
  mkdir -p Builds/TestResults
  "$UNITY" -batchmode -projectPath "$(pwd -W)" -runTests -testPlatform <EditMode|PlayMode> \
    -testFilter "<filter>" -testResults Builds/TestResults/<name>.xml -logFile Builds/TestResults/<name>.log
  grep -m1 -o '<test-run[^>]*' Builds/TestResults/<name>.xml || grep -n "error CS\|Exception" Builds/TestResults/<name>.log | head -20
  grep -o 'fullname="[^"]*" [^>]*result="Failed"' Builds/TestResults/<name>.xml | head -20
  ```

  In this plan, "Run tests: `<Platform>` `<filter>` `<name>`" means that command with those three values.
- Run a scene configurator with:

  ```bash
  "$UNITY" -batchmode -projectPath "$(pwd -W)" -executeMethod <Method> -logFile Builds/<name>.log -quit; echo "exit=$?"
  grep -n "error CS\|Exception\|\[KMA\]" Builds/<name>.log | head -20
  ```

- Git: commit directly on `master`. Commit messages carry **no** `Co-Authored-By` trailer.
- Commit the `.meta` files Unity creates next to new or changed assets.

## Review Focus

1. **Keeper saves vs. what the player sees.** A ball could fly through the drawn keeper, or be saved by empty air next to him, if the capsules don't trace the new sprite. Expected: the capsules cover the `fall` pose within 85% recall and 85% precision. This is pinned in Task 3, `KeeperSilhouetteTests`.
2. **Sprint runners change size or float off their lanes after the switch to HD.** Expected: every pose is still 0.96×1.28 world units with its pivot at the feet. This is pinned in Task 1, `EveryPoseImportsAtRunnerSize`. Task 2 re-runs the existing lane and presentation tests.
3. **Stale or dead sprite references after a configurator reimports textures.** A clip or scene could keep pointing at a deleted `Runner_*` file or a missing sprite. Expected: every hero clip key and every scene sprite is a live hero pose. This is pinned in Task 5, `HeroConsistencyTests`, which fails on null sprites.
4. **Wrong Football reactions.** The kicker could celebrate a post or crossbar hit, or the keeper could stay in the "beaten" pose into the next kick. Expected: the kicker celebrates only on `Goal`; both characters return to their ready poses once the flight is cleared. This is pinned in Task 3, `FootballPosesTests`.
5. **Volleyball opponent faces away from the net, or the hero is tinted or mirrored.** Expected: the player is unmirrored and white, and the opponent is mirrored and white. This is pinned in Task 4, `VolleyballSceneConfiguratorTests`, with the scene built twice to cover re-runs.

---

## File Structure

| Path | Change | Responsibility |
|---|---|---|
| `Assets/_Project/Art/Characters/{MaleAdventurer,MalePerson,FemalePerson,FemaleAdventurer}/<Character>_<pose>.png` | Create (18 per folder) | HD pose images copied from the pack |
| `Assets/_Project/Art/Characters/*/Runner_*.png` | Delete (Task 2) | Old 96×128 Sprint poses |
| `Assets/_Project/Art/Characters/BeachVolley/` | Delete (Task 4) | Old BVA2 athlete sheets |
| `Assets/_Project/Art/Football/GoalView/{keeper,player}.{png,svg}` | Delete (Task 3) | Old vector keeper and striker |
| `Assets/Editor/ToonCharacterArt.cs` | Create | Pose paths, import settings, loading, "is this sprite character X" |
| `Assets/Editor/DemoSprintArtConfigurator.cs` | Modify | Sprint runners load through `ToonCharacterArt` |
| `Assets/_Project/Scripts/Gameplay/Football/FootballPoses.cs` | Create | Pose enums, pose-selection rules, serialisable pose sprite set |
| `Assets/_Project/Scripts/Gameplay/Football/FootballRules.cs` | Modify | Make `KickAnimationSeconds` public |
| `Assets/_Project/Scripts/Gameplay/Football/FootballFlightSimulation.cs` | Modify | `KeeperHipY`, refitted capsules, capsule accessor |
| `Assets/_Project/Scripts/Gameplay/Football/FootballPresentation.cs` | Modify | Keeper feet-pivot placement, pose switching |
| `Assets/Editor/FootballSceneConfigurator.cs` | Modify | Build kicker and keeper from Toon poses |
| `Assets/Editor/VolleyballSceneConfigurator.cs` | Modify | Build athletes from Toon poses |
| `tools/export-football-preview-art.py`, `Assets/_Project/Art/Football/GoalView/README.md` | Modify | Stop exporting keeper and player; document the new capsule source |
| `Assets/_Project/CREDITS.md` | Modify | Toon Characters roles; BVA2 environment credit |
| `Assets/Tests/EditMode/EditorTools/ToonCharacterArtTests.cs` | Create | Pose files and import contract |
| `Assets/Tests/EditMode/EditorTools/KeeperSilhouetteTests.cs` | Create | Capsule ↔ sprite fit |
| `Assets/Tests/EditMode/EditorTools/VolleyballSceneConfiguratorTests.cs` | Create | Volleyball cast |
| `Assets/Tests/EditMode/EditorTools/HeroConsistencyTests.cs` | Create | Hero is the same in every minigame |
| `Assets/Tests/EditMode/EditorTools/KMA.EditorTools.EditMode.Tests.asmdef` | Modify | Reference Volleyball and Sprint assemblies |
| `Assets/Tests/EditMode/Gameplay/Ball/FootballPosesTests.cs` | Create | Pose table and keeper placement |
| `Assets/Tests/PlayMode/Presentation/RunnerVisualTests.cs` | Modify | Rival textures are pack characters, not `Runner_*` |

---

### Task 1: Toon character library

**Files:**
- Create: `Assets/_Project/Art/Characters/<Character>/<Character>_<pose>.png` (4 × 18 files)
- Create: `Assets/Editor/ToonCharacterArt.cs`
- Create: `Assets/Tests/EditMode/EditorTools/ToonCharacterArtTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces (namespace `KMA.EditorTools`, static class `ToonCharacterArt`):
  - `const string Root = "Assets/_Project/Art/Characters/"`
  - `const string Hero = "MaleAdventurer"`
  - `const float PixelsPerUnit = 200f`
  - `static readonly string[] Characters`
  - `static readonly string[] Poses`
  - `static string PosePath(string character, string pose)`
  - `static void ImportAll()`
  - `static Sprite Load(string character, string pose)`
  - `static Sprite[] Frames(string character, params string[] poses)`
  - `static bool IsPoseOf(Sprite sprite, string character)`

- [ ] **Step 1: Copy the HD poses from the pack**

```bash
TMP="$(mktemp -d)"
ZIP_URL="$(curl -sL https://kenney.nl/assets/toon-characters | grep -oE 'https://kenney.nl/media/pages/assets/[^"]+\.zip' | head -1)"
echo "$ZIP_URL"
curl -sL -o "$TMP/toon.zip" "$ZIP_URL" && unzip -q "$TMP/toon.zip" -d "$TMP/toon"
grep -i "CC0" "$TMP/toon/License.txt"
POSES="idle run0 run1 run2 hit cheer0 cheer1 fallDown back climb0 climb1 hurt duck hold jump attack1 slide fall"
copy_character() { # <project folder> <pack folder> <pack file name>
  mkdir -p "Assets/_Project/Art/Characters/$1"
  for pose in $POSES; do
    cp "$TMP/toon/$2/PNG/Poses HD/character_$3_$pose.png" "Assets/_Project/Art/Characters/$1/$1_$pose.png" || echo "MISSING $1 $pose"
  done
}
copy_character MaleAdventurer "Male adventurer" maleAdventurer
copy_character MalePerson "Male person" malePerson
copy_character FemalePerson "Female person" femalePerson
copy_character FemaleAdventurer "Female adventurer" femaleAdventurer
ls Assets/_Project/Art/Characters/*/ | grep -c "_.*\.png$"
rm -rf "$TMP"
```

Expected: the licence line mentions CC0, there are no `MISSING` lines, and the count is `72`.

- [ ] **Step 2: Write the failing test**

Create `Assets/Tests/EditMode/EditorTools/ToonCharacterArtTests.cs`:

```csharp
#if UNITY_EDITOR
using System;
using KMA.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class ToonCharacterArtTests
    {
        [Test]
        public void HeroIsTheMaleAdventurer()
        {
            Assert.That(ToonCharacterArt.Hero, Is.EqualTo("MaleAdventurer"));
            Assert.That(ToonCharacterArt.Characters, Does.Contain(ToonCharacterArt.Hero));
        }

        [Test]
        public void EveryPoseImportsAtRunnerSize()
        {
            ToonCharacterArt.ImportAll();
            foreach (string character in ToonCharacterArt.Characters)
            foreach (string pose in ToonCharacterArt.Poses)
            {
                Sprite sprite = ToonCharacterArt.Load(character, pose);
                string label = character + " " + pose;
                Assert.That(sprite.texture.width, Is.EqualTo(192), label);
                Assert.That(sprite.texture.height, Is.EqualTo(256), label);
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(ToonCharacterArt.PixelsPerUnit), label);
                // The Sprint lanes were tuned for 0.96 x 1.28 runners standing on their pivot.
                Assert.That(sprite.bounds.size.x, Is.EqualTo(.96f).Within(.001f), label);
                Assert.That(sprite.bounds.size.y, Is.EqualTo(1.28f).Within(.001f), label);
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(96f, 0f)), label);
                Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Bilinear), label);
                Assert.That(ToonCharacterArt.IsPoseOf(sprite, character), Is.True, label);
            }
        }

        [Test]
        public void IsPoseOfTellsCharactersApart()
        {
            ToonCharacterArt.ImportAll();
            Sprite hero = ToonCharacterArt.Load(ToonCharacterArt.Hero, "idle");
            Assert.That(ToonCharacterArt.IsPoseOf(hero, "MalePerson"), Is.False);
            Assert.That(ToonCharacterArt.IsPoseOf(null, ToonCharacterArt.Hero), Is.False);
        }

        [Test]
        public void MissingPoseNamesThePath()
        {
            var error = Assert.Throws<InvalidOperationException>(
                () => ToonCharacterArt.Load(ToonCharacterArt.Hero, "notAPose"));
            Assert.That(error.Message, Does.Contain("MaleAdventurer/MaleAdventurer_notAPose.png"));
        }
    }
}
#endif
```

- [ ] **Step 3: Run the test to verify it fails**

Run tests: `EditMode` `KMA.Tests.EditorTools.ToonCharacterArtTests` `toon-art`.
Expected: no XML is written. The log shows `error CS0103: The name 'ToonCharacterArt' does not exist`.

- [ ] **Step 4: Write the implementation**

Create `Assets/Editor/ToonCharacterArt.cs`:

```csharp
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KMA.EditorTools
{
    /// <summary>
    /// The one place Kenney Toon Characters poses are imported and loaded. Every minigame draws
    /// its characters through here, so the hero keeps one size, pivot and filter in all scenes.
    /// </summary>
    public static class ToonCharacterArt
    {
        public const string Root = "Assets/_Project/Art/Characters/";
        /// <summary>The player's character in every minigame.</summary>
        public const string Hero = "MaleAdventurer";
        /// <summary>192x256 HD poses at this density are 0.96 x 1.28 units, the Sprint runner size.</summary>
        public const float PixelsPerUnit = 200f;
        const int MaxTextureSize = 512;

        public static readonly string[] Characters = { Hero, "MalePerson", "FemalePerson", "FemaleAdventurer" };

        public static readonly string[] Poses =
        {
            "idle", "run0", "run1", "run2", "hit", "cheer0", "cheer1", "fallDown", "back",
            "climb0", "climb1", "hurt", "duck", "hold", "jump", "attack1", "slide", "fall"
        };

        public static string PosePath(string character, string pose) =>
            Root + character + "/" + character + "_" + pose + ".png";

        /// <summary>
        /// Imports every pose before anything is loaded: reimporting a texture invalidates Sprite
        /// references handed out earlier, so importing and loading must not interleave.
        /// </summary>
        public static void ImportAll()
        {
            foreach (string character in Characters)
                foreach (string pose in Poses)
                    Import(PosePath(character, pose));
        }

        public static Sprite Load(string character, string pose)
        {
            string path = PosePath(character, pose);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException("[KMA] Toon character pose is missing or not imported: " + path);
            return sprite;
        }

        public static Sprite[] Frames(string character, params string[] poses) =>
            poses.Select(pose => Load(character, pose)).ToArray();

        public static bool IsPoseOf(Sprite sprite, string character) =>
            sprite != null && AssetDatabase.GetAssetPath(sprite).StartsWith(Root + character + "/", StringComparison.Ordinal);

        static void Import(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "[KMA] Missing Toon character pose: " + path + ". Copy it from Kenney Toon Characters (PNG/Poses HD).", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var current = new TextureImporterSettings();
            importer.ReadTextureSettings(current);
            // Skipping an already-correct texture keeps earlier Sprite references alive.
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit) &&
                importer.filterMode == FilterMode.Bilinear && !importer.mipmapEnabled &&
                importer.alphaIsTransparency && importer.wrapMode == TextureWrapMode.Clamp &&
                importer.maxTextureSize == MaxTextureSize &&
                current.spriteMeshType == SpriteMeshType.FullRect &&
                current.spriteAlignment == (int)SpriteAlignment.BottomCenter)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = MaxTextureSize;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spritePivot = new Vector2(.5f, 0f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
#endif
```

- [ ] **Step 5: Run the test to verify it passes**

Run tests: `EditMode` `KMA.Tests.EditorTools.ToonCharacterArtTests` `toon-art`.
Expected: `result="Passed"`, `total="4"`, `failed="0"`.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Art/Characters Assets/Editor/ToonCharacterArt.cs Assets/Editor/ToonCharacterArt.cs.meta \
  Assets/Tests/EditMode/EditorTools/ToonCharacterArtTests.cs Assets/Tests/EditMode/EditorTools/ToonCharacterArtTests.cs.meta
git status --short   # only the new pose PNGs, their .meta files and the two scripts
git commit -m "feat(art): add the shared Toon character pose library"
```

---

### Task 2: Sprint draws runners from the library

**Files:**
- Modify: `Assets/Editor/DemoSprintArtConfigurator.cs`
- Modify: `Assets/Tests/PlayMode/Presentation/RunnerVisualTests.cs:94`
- Delete: `Assets/_Project/Art/Characters/*/Runner_*.png` and their `.meta` files
- Regenerated by the configurator: `Assets/_Project/Animations/*.anim`, `*.overrideController`, `Assets/_Project/Prefabs/Gameplay/PlayerRunnerVisual.prefab`, `RivalRunner.prefab`, `Assets/_Project/Scenes/MG_Sprint.unity`

**Interfaces:**
- Consumes: from Task 1, `ToonCharacterArt.ImportAll()`, `Load`, `Frames` and `Hero`.
- Produces: no new API. After this task, the committed Sprint assets reference only `<Character>_<pose>.png` sprites.

- [ ] **Step 1: Make the rival test expect pack characters**

In `Assets/Tests/PlayMode/Presentation/RunnerVisualTests.cs`, replace:

```csharp
                Assert.That(rival.Sprite.sprite.texture.name, Does.StartWith("Runner_"));
```

with:

```csharp
                Assert.That(rival.Sprite.sprite.texture.name,
                    Does.Match("^(MalePerson|FemalePerson|FemaleAdventurer)_"),
                    "Rivals draw from the shared Toon character library, never from the hero.");
```

- [ ] **Step 2: Run the test to verify it fails**

Run tests: `PlayMode` `KMA.Tests.Presentation.RunnerVisualTests` `runner-visual`.
Expected: `SceneUsesArtworkOnEveryParallaxTileAndRunner` fails, because the texture name is still `Runner_Idle`.

- [ ] **Step 3: Point the configurator at the library**

In `Assets/Editor/DemoSprintArtConfigurator.cs`:

1. Delete the line `static readonly string[] Poses = { "Idle", "Run0", "Run1", "Run2", "Hit", "Cheer0", "Cheer1", "FallDown" };`.
2. Replace `const string PlayerCharacter = "MaleAdventurer";` with `const string PlayerCharacter = ToonCharacterArt.Hero;`.
3. In `Configure()`, replace:

```csharp
            // Every pose is imported before any is loaded; see LoadCharacter for why.
            foreach (string folder in characters)
                foreach (string pose in Poses)
                    ImportSprite("Characters/" + folder + "/Runner_" + pose + ".png", true);
```

with:

```csharp
            // Every pose is imported before any is loaded; see ToonCharacterArt.ImportAll for why.
            ToonCharacterArt.ImportAll();
```

4. In the parallax loop, replace `ImportSprite("Environments/Sprint/" + names[i] + ".png", false)` with `ImportSprite("Environments/Sprint/" + names[i] + ".png")`.
5. Replace the whole `ImportSprite(string relativePath, bool runner)` method with this backdrop-only version:

```csharp
        static Sprite ImportSprite(string relativePath)
        {
            string path = Art + relativePath;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new InvalidOperationException("Missing demo art: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(.5f, .5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
```

6. Replace the whole `LoadCharacter` method, including its doc comment, with:

```csharp
        /// <summary>One character's Sprint poses; ToonCharacterArt.ImportAll must have run first.</summary>
        static CharacterArt LoadCharacter(string folder) => new CharacterArt
        {
            Folder = folder,
            Idle = ToonCharacterArt.Load(folder, "idle"),
            Hit = ToonCharacterArt.Load(folder, "hit"),
            FallDown = ToonCharacterArt.Load(folder, "fallDown"),
            Run = ToonCharacterArt.Frames(folder, "run0", "run1", "run2"),
            Cheer = ToonCharacterArt.Frames(folder, "cheer0", "cheer1")
        };
```

- [ ] **Step 4: Delete the old poses, then regenerate the Sprint artwork**

```bash
git rm -q Assets/_Project/Art/Characters/*/Runner_*.png Assets/_Project/Art/Characters/*/Runner_*.png.meta
ls Assets/_Project/Art/Characters/*/ | grep -c "Runner_"   # expect 0
```

Run configurator: method `KMA.EditorTools.DemoSprintArtConfigurator.Configure`, log `configure-sprint`.
Expected: `exit=0`, and the log contains `[KMA] Sprint sprite clips, player prefab, parallax artwork and app icon configured.` with no `Exception`.

```bash
grep -l "Runner_" Assets/_Project/Animations/*.anim Assets/_Project/Prefabs/Gameplay/*.prefab Assets/_Project/Scenes/MG_Sprint.unity || echo "no stale Runner_ references"
git status --short
```

If `ProjectSettings/ProjectSettings.asset` shows up as modified, run `git diff ProjectSettings/ProjectSettings.asset`. Keep the change only if it is limited to icon entries; otherwise restore the file with `git checkout -- ProjectSettings/ProjectSettings.asset`.

- [ ] **Step 5: Run the Sprint tests to verify they pass**

Run tests: `PlayMode` `KMA.Tests.Presentation` `sprint-presentation`.
Expected: `failed="0"`.

Run tests: `PlayMode` `KMA.Tests.Gameplay.Running` `sprint-running`.
Expected: `failed="0"`.

Run tests: `EditMode` `KMA.Tests.Presentation` `sprint-layout`.
Expected: `failed="0"`. This covers `SprintTrackLayoutTests` and the other Sprint UI tests.

- [ ] **Step 6: Commit**

```bash
git add -A Assets/_Project/Art/Characters Assets/_Project/Animations Assets/_Project/Prefabs/Gameplay \
  Assets/_Project/Scenes/MG_Sprint.unity Assets/Editor/DemoSprintArtConfigurator.cs \
  Assets/Tests/PlayMode/Presentation/RunnerVisualTests.cs
git commit -m "refactor(sprint): load runners from the shared Toon character library"
```

---

### Task 3: Football kicker and keeper from the library

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Football/FootballPoses.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Football/FootballRules.cs:9`
- Modify: `Assets/_Project/Scripts/Gameplay/Football/FootballFlightSimulation.cs:122-155`
- Modify: `Assets/_Project/Scripts/Gameplay/Football/FootballPresentation.cs`
- Modify: `Assets/Editor/FootballSceneConfigurator.cs`
- Modify: `tools/export-football-preview-art.py`, `Assets/_Project/Art/Football/GoalView/README.md`
- Delete: `Assets/_Project/Art/Football/GoalView/{keeper,player}.{png,svg}` and their `.meta` files
- Regenerated: `Assets/_Project/Scenes/MG_Football.unity`
- Test: `Assets/Tests/EditMode/Gameplay/Ball/FootballPosesTests.cs`, `Assets/Tests/EditMode/EditorTools/KeeperSilhouetteTests.cs`

**Interfaces:**
- Consumes: from Task 1, `ToonCharacterArt.ImportAll`, `Load`, `PosePath` and `Hero`.
- Produces:
  - `enum KickerPose { Ready, RunUp, Strike, Celebrate }` and `enum KeeperPose { Ready, Save, Beaten }`, in namespace `KMA.Gameplay`.
  - `static class FootballPoses`:
    - `KickerPose Kicker(FootballState state, float stateElapsed, FootballOutcome? outcome)`
    - `KeeperPose Keeper(FootballOutcome? outcome)`
  - `[Serializable] sealed class FootballPoseSprites`:
    - public fields `kickerReady`, `kickerRunUp`, `kickerStrike`, `kickerCelebrate`, `keeperReady`, `keeperSave`, `keeperBeaten`
    - `Sprite For(KickerPose)` and `Sprite For(KeeperPose)`
  - `FootballRules.KickAnimationSeconds`, now `public const float`.
  - On `FootballFlightSimulation`:
    - `const float KeeperHipY = 281f`
    - `static int KeeperCapsuleCount`
    - `static void GetKeeperCapsule(int index, out Vector2 a, out Vector2 b, out float radius)`
  - On `FootballPresentation`:
    - consts `KeeperDisplayWidth = 108f`, `KeeperDisplayHeight = 144f`, `KeeperFeetDrop = 21f`
    - `static Vector3 KeeperWorldPosition(float keeperX, float angleDegrees)`
    - `FootballPoseSprites Poses { get; }`
    - `void ConfigurePoses(FootballPoseSprites sprites)`
  - On `FootballSceneConfigurator`: consts `KeeperCharacter = "MalePerson"` and `KeeperReadyPose = "fall"`.

- [ ] **Step 1: Write the failing pose-rule and keeper-placement tests**

Create `Assets/Tests/EditMode/Gameplay/Ball/FootballPosesTests.cs`:

```csharp
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class FootballPosesTests
    {
        [Test]
        public void KickerRunsUpStrikesAndCelebratesOnlyGoals()
        {
            float kick = FootballRules.KickAnimationSeconds;
            Assert.That(FootballPoses.Kicker(FootballState.Start, 0f, null), Is.EqualTo(KickerPose.Ready));
            Assert.That(FootballPoses.Kicker(FootballState.Aiming, 0f, null), Is.EqualTo(KickerPose.Ready));
            Assert.That(FootballPoses.Kicker(FootballState.Charging, .4f, null), Is.EqualTo(KickerPose.Ready));
            Assert.That(FootballPoses.Kicker(FootballState.Kicking, kick * .25f, null), Is.EqualTo(KickerPose.RunUp));
            Assert.That(FootballPoses.Kicker(FootballState.Kicking, kick * .75f, null), Is.EqualTo(KickerPose.Strike));
            Assert.That(FootballPoses.Kicker(FootballState.Flying, .2f, null), Is.EqualTo(KickerPose.Strike));
            Assert.That(FootballPoses.Kicker(FootballState.Flying, .6f, FootballOutcome.Goal), Is.EqualTo(KickerPose.Celebrate));
            Assert.That(FootballPoses.Kicker(FootballState.ShotResult, .1f, FootballOutcome.Goal), Is.EqualTo(KickerPose.Celebrate));
            foreach (var miss in new[] { FootballOutcome.Saved, FootballOutcome.Wide, FootballOutcome.High,
                         FootballOutcome.Short, FootballOutcome.Post, FootballOutcome.Crossbar })
                Assert.That(FootballPoses.Kicker(FootballState.ShotResult, .1f, miss), Is.EqualTo(KickerPose.Ready), miss.ToString());
        }

        [Test]
        public void KeeperHoldsSavesAndIsBeatenOnlyByGoals()
        {
            Assert.That(FootballPoses.Keeper(null), Is.EqualTo(KeeperPose.Ready));
            Assert.That(FootballPoses.Keeper(FootballOutcome.Saved), Is.EqualTo(KeeperPose.Save));
            Assert.That(FootballPoses.Keeper(FootballOutcome.Goal), Is.EqualTo(KeeperPose.Beaten));
            Assert.That(FootballPoses.Keeper(FootballOutcome.Post), Is.EqualTo(KeeperPose.Ready));
        }

        [Test]
        public void PoseSpritesMapEveryPose()
        {
            Sprite Make(string name) { var s = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero); s.name = name; return s; }
            var set = new FootballPoseSprites
            {
                kickerReady = Make("kr"), kickerRunUp = Make("ku"), kickerStrike = Make("ks"), kickerCelebrate = Make("kc"),
                keeperReady = Make("gr"), keeperSave = Make("gs"), keeperBeaten = Make("gb")
            };
            Assert.That(set.For(KickerPose.Ready), Is.SameAs(set.kickerReady));
            Assert.That(set.For(KickerPose.RunUp), Is.SameAs(set.kickerRunUp));
            Assert.That(set.For(KickerPose.Strike), Is.SameAs(set.kickerStrike));
            Assert.That(set.For(KickerPose.Celebrate), Is.SameAs(set.kickerCelebrate));
            Assert.That(set.For(KeeperPose.Ready), Is.SameAs(set.keeperReady));
            Assert.That(set.For(KeeperPose.Save), Is.SameAs(set.keeperSave));
            Assert.That(set.For(KeeperPose.Beaten), Is.SameAs(set.keeperBeaten));
        }

        [Test]
        public void KeeperDiveRotatesAboutTheHip()
        {
            float drop = FootballPresentation.KeeperFeetDrop * FootballPresentation.PixelToWorld;
            Vector3 hip = FootballPresentation.ScreenToWorld(600f, FootballFlightSimulation.KeeperHipY);
            Vector3 upright = FootballPresentation.KeeperWorldPosition(0f, 0f);
            Assert.That(upright.x, Is.EqualTo(hip.x).Within(1e-5f));
            Assert.That(hip.y - upright.y, Is.EqualTo(drop).Within(1e-5f), "Feet stand below the hip.");
            foreach (float angle in new[] { -45f, -20f, 30f, 45f })
            {
                Vector3 feet = FootballPresentation.KeeperWorldPosition(0f, angle);
                Vector3 backToHip = feet + Quaternion.Euler(0f, 0f, -angle) * new Vector3(0f, drop, 0f);
                Assert.That(Vector3.Distance(backToHip, hip), Is.LessThan(1e-5f), "angle " + angle);
            }
            Assert.That(FootballPresentation.KeeperWorldPosition(1f, 0f).x, Is.GreaterThan(upright.x));
        }
    }
}
```

- [ ] **Step 2: Write the failing silhouette test**

Create `Assets/Tests/EditMode/EditorTools/KeeperSilhouetteTests.cs`:

```csharp
#if UNITY_EDITOR
using System.IO;
using KMA.EditorTools;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class KeeperSilhouetteTests
    {
        [Test]
        public void KeeperCapsulesTraceTheDrawnKeeperPose()
        {
            var texture = new Texture2D(2, 2);
            string path = ToonCharacterArt.PosePath(FootballSceneConfigurator.KeeperCharacter, FootballSceneConfigurator.KeeperReadyPose);
            Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
            float scale = FootballPresentation.KeeperDisplayHeight / texture.height; // preview px per texture px
            Assert.That(FootballPresentation.KeeperDisplayWidth / texture.width, Is.EqualTo(scale).Within(1e-4f),
                "The keeper must be drawn without stretching.");

            int opaque = 0, opaqueCovered = 0, covered = 0, coveredOpaque = 0;
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                // Capsule space: origin at the hip, y grows downward. Texture rows grow upward from the feet.
                var point = new Vector2((x + .5f - texture.width * .5f) * scale,
                    FootballPresentation.KeeperFeetDrop - (y + .5f) * scale);
                bool solid = texture.GetPixel(x, y).a > .5f;
                bool inside = InsideCapsules(point);
                if (solid) { opaque++; if (inside) opaqueCovered++; }
                if (inside) { covered++; if (solid) coveredOpaque++; }
            }
            Object.DestroyImmediate(texture);

            float recall = (float)opaqueCovered / opaque;
            float precision = (float)coveredOpaque / covered;
            Assert.That(recall, Is.GreaterThanOrEqualTo(.85f), "Share of the drawn keeper that can save the ball.");
            Assert.That(precision, Is.GreaterThanOrEqualTo(.85f), "Share of the save area that is drawn keeper.");
        }

        static bool InsideCapsules(Vector2 point)
        {
            for (int i = 0; i < FootballFlightSimulation.KeeperCapsuleCount; i++)
            {
                FootballFlightSimulation.GetKeeperCapsule(i, out Vector2 a, out Vector2 b, out float radius);
                Vector2 delta = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, delta) / Mathf.Max(delta.sqrMagnitude, .000001f));
                if (Vector2.Distance(point, a + delta * t) <= radius) return true;
            }
            return false;
        }
    }
}
#endif
```

- [ ] **Step 3: Run both tests to verify they fail**

Run tests: `EditMode` `KMA.Tests.Gameplay.Ball.FootballPosesTests` `football-poses`.
Expected: compile errors in the log, for example `error CS0103: The name 'FootballPoses' does not exist`.

- [ ] **Step 4: Add the pose rules**

In `Assets/_Project/Scripts/Gameplay/Football/FootballRules.cs`, change `const float KickAnimationSeconds = .18f;` to `public const float KickAnimationSeconds = .18f;`.

Create `Assets/_Project/Scripts/Gameplay/Football/FootballPoses.cs`:

```csharp
using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum KickerPose { Ready, RunUp, Strike, Celebrate }

    public enum KeeperPose { Ready, Save, Beaten }

    /// <summary>Which pose the kicker and the keeper show at each moment of a penalty.</summary>
    public static class FootballPoses
    {
        public static KickerPose Kicker(FootballState state, float stateElapsed, FootballOutcome? outcome)
        {
            if (outcome.HasValue)
                return outcome.Value == FootballOutcome.Goal ? KickerPose.Celebrate : KickerPose.Ready;
            if (state == FootballState.Kicking)
                return stateElapsed < FootballRules.KickAnimationSeconds * .5f ? KickerPose.RunUp : KickerPose.Strike;
            return state == FootballState.Flying ? KickerPose.Strike : KickerPose.Ready;
        }

        public static KeeperPose Keeper(FootballOutcome? outcome) => outcome switch
        {
            FootballOutcome.Saved => KeeperPose.Save,
            FootballOutcome.Goal => KeeperPose.Beaten,
            _ => KeeperPose.Ready
        };
    }

    /// <summary>The sprite for every pose. All poses share one size, so swapping keeps the renderer's scale.</summary>
    [Serializable]
    public sealed class FootballPoseSprites
    {
        public Sprite kickerReady, kickerRunUp, kickerStrike, kickerCelebrate;
        public Sprite keeperReady, keeperSave, keeperBeaten;

        public Sprite For(KickerPose pose) => pose switch
        {
            KickerPose.RunUp => kickerRunUp,
            KickerPose.Strike => kickerStrike,
            KickerPose.Celebrate => kickerCelebrate,
            _ => kickerReady
        };

        public Sprite For(KeeperPose pose) => pose switch
        {
            KeeperPose.Save => keeperSave,
            KeeperPose.Beaten => keeperBeaten,
            _ => keeperReady
        };
    }
}
```

- [ ] **Step 5: Refit the keeper capsules**

In `Assets/_Project/Scripts/Gameplay/Football/FootballFlightSimulation.cs`:

1. Add this at the top of the class body, above the existing `readonly bool keeperEnabled;`:

```csharp
        /// <summary>Screen-pixel height of the keeper's hip: the dive rotates about it and KeeperCapsules are measured from it.</summary>
        public const float KeeperHipY = 281f;
```

2. In `TouchesKeeper`, replace `float dy = screen.y - 281f;` with `float dy = screen.y - KeeperHipY;`. Replace the comment above the method with `// Capsule silhouettes trace the drawn keeper pose in preview pixels around the hip; KeeperSilhouetteTests pins the fit.`

3. Replace the `KeeperCapsules` array with:

```csharp
        // Kenney Toon `Male person` `fall` pose drawn FootballPresentation.KeeperDisplayWidth x Height,
        // feet KeeperFeetDrop px below the hip. Head, torso, upper arms, hands, legs.
        static readonly Capsule[] KeeperCapsules = {
            new Capsule(-8,-74,14,-74,15), new Capsule(-8,-57,14,-57,16),
            new Capsule(1,-32,1,-12,19),
            new Capsule(-20,-34,-36,-30,7), new Capsule(20,-37,40,-33,7),
            new Capsule(-38,-21,-38,-21,10), new Capsule(41,-24,41,-24,10),
            new Capsule(-11,-6,-12,14,9), new Capsule(15,-6,19,8,9)
        };

        public static int KeeperCapsuleCount => KeeperCapsules.Length;

        public static void GetKeeperCapsule(int index, out Vector2 a, out Vector2 b, out float radius)
        {
            Capsule capsule = KeeperCapsules[index];
            a = capsule.a;
            b = capsule.b;
            radius = capsule.radius;
        }
```

- [ ] **Step 6: Place the keeper by his feet and switch poses**

In `Assets/_Project/Scripts/Gameplay/Football/FootballPresentation.cs`:

1. Below `public const float PixelToWorld = .016f;`, add:

```csharp
        public const float KeeperDisplayWidth = 108f, KeeperDisplayHeight = 144f;
        /// <summary>Screen pixels from the keeper's hip (the dive's rotation centre) down to the sprite's feet pivot.</summary>
        public const float KeeperFeetDrop = 21f;
```

2. Below `[SerializeField] SpriteRenderer goalNet;`, add `[SerializeField] FootballPoseSprites poses;`.
3. Below the `ValidateReferences` method, add:

```csharp
        public FootballPoseSprites Poses => poses;
        public void ConfigurePoses(FootballPoseSprites sprites) => poses = sprites;

        /// <summary>Where the keeper's feet pivot goes so that the dive still rotates about his hip.</summary>
        public static Vector3 KeeperWorldPosition(float keeperX, float angleDegrees)
        {
            Vector3 hip = ScreenToWorld(600f + keeperX * 300f / FootballShotSolver.GoalHalfWidth, FootballFlightSimulation.KeeperHipY);
            return hip + Quaternion.Euler(0f, 0f, -angleDegrees) * new Vector3(0f, -KeeperFeetDrop * PixelToWorld, 0f);
        }
```

4. In `Render`, replace:

```csharp
            float keeperX = flying ? flight.KeeperX : 0f;
            goalkeeper.transform.position = ScreenToWorld(600f + keeperX * 300f / FootballShotSolver.GoalHalfWidth, 281f);
            goalkeeper.transform.rotation = Quaternion.Euler(0f, 0f, flying ? -flight.KeeperAngle : 0f);
```

with:

```csharp
            float keeperX = flying ? flight.KeeperX : 0f;
            float keeperAngle = flying ? flight.KeeperAngle : 0f;
            goalkeeper.transform.position = KeeperWorldPosition(keeperX, keeperAngle);
            goalkeeper.transform.rotation = Quaternion.Euler(0f, 0f, -keeperAngle);
```

5. In `Render`, directly before `RenderAimArrow(rules);`, add:

```csharp
            if (poses != null)
            {
                FootballOutcome? outcome = flight?.Outcome;
                Sprite kicker = poses.For(FootballPoses.Kicker(rules.State, rules.StateElapsed, outcome));
                if (kicker) player.sprite = kicker;
                Sprite keeperSprite = poses.For(FootballPoses.Keeper(outcome));
                if (keeperSprite) goalkeeper.sprite = keeperSprite;
            }
```

- [ ] **Step 7: Build the kicker and keeper from Toon poses in the configurator**

In `Assets/Editor/FootballSceneConfigurator.cs`:

1. Below `static readonly Color Gold = ...;`, add:

```csharp
        public const string KeeperCharacter = "MalePerson";
        public const string KeeperReadyPose = "fall";
        // The kicker stands nearest the camera: 240x320 preview px, feet on the penalty spot row.
        const float KickerDisplayWidth = 240f, KickerDisplayHeight = 320f;
        static readonly Vector2 KickerFeet = new Vector2(490f, 602f);
```

2. In `BuildScene()`, directly after `ImportGoalViewArt();`, add `ToonCharacterArt.ImportAll();`.
3. In `BuildWorld`, replace these two lines:

```csharp
            var keeper = Layer("Goalkeeper", "keeper", 600, 281, 140, 130, 20);
            var player = Layer("Player", "player", 490, 486, 140, 230, 21);
```

with:

```csharp
            var poses = LoadPoses();
            var keeper = Renderer(world.transform, "Goalkeeper", poses.keeperReady, FootballPresentation.KeeperWorldPosition(0f, 0f), 20,
                new Vector2(FootballPresentation.KeeperDisplayWidth, FootballPresentation.KeeperDisplayHeight) * FootballPresentation.PixelToWorld);
            var player = Renderer(world.transform, "Player", poses.kickerReady, FootballPresentation.ScreenToWorld(KickerFeet.x, KickerFeet.y), 21,
                new Vector2(KickerDisplayWidth, KickerDisplayHeight) * FootballPresentation.PixelToWorld);
```

4. Directly after `presentation.Configure(field, goal, ball, shadow, player, keeper, crosshair, left, right, dots, net);`, add `presentation.ConfigurePoses(poses);`.
5. Add this method below `BuildWorld`:

```csharp
        /// <summary>The hero kicks, seen from behind; the keeper faces him.</summary>
        static FootballPoseSprites LoadPoses() => new FootballPoseSprites
        {
            kickerReady = ToonCharacterArt.Load(ToonCharacterArt.Hero, "back"),
            kickerRunUp = ToonCharacterArt.Load(ToonCharacterArt.Hero, "climb0"),
            kickerStrike = ToonCharacterArt.Load(ToonCharacterArt.Hero, "climb1"),
            kickerCelebrate = ToonCharacterArt.Load(ToonCharacterArt.Hero, "hurt"),
            keeperReady = ToonCharacterArt.Load(KeeperCharacter, KeeperReadyPose),
            keeperSave = ToonCharacterArt.Load(KeeperCharacter, "hold"),
            keeperBeaten = ToonCharacterArt.Load(KeeperCharacter, "hit")
        };
```

6. In `ImportGoalViewArt`, replace the pivot switch:

```csharp
                settings.spritePivot = Path.GetFileNameWithoutExtension(file) switch
                {
                    "keeper" => new Vector2(.5f, 27f / 130f),
                    "player" => new Vector2(65f / 140f, .5f),
                    _ => new Vector2(.5f, .5f)
                };
```

with `settings.spritePivot = new Vector2(.5f, .5f);`.

- [ ] **Step 8: Retire the vector keeper and striker**

```bash
git rm -q Assets/_Project/Art/Football/GoalView/keeper.png Assets/_Project/Art/Football/GoalView/keeper.png.meta \
  Assets/_Project/Art/Football/GoalView/keeper.svg Assets/_Project/Art/Football/GoalView/keeper.svg.meta \
  Assets/_Project/Art/Football/GoalView/player.png Assets/_Project/Art/Football/GoalView/player.png.meta \
  Assets/_Project/Art/Football/GoalView/player.svg Assets/_Project/Art/Football/GoalView/player.svg.meta
```

In `tools/export-football-preview-art.py`:
- Delete the two lines that start with `keeper = next(` and `player = next(`.
- Delete the two lines that start with `export("keeper"` and `export("player"`.
- Change the final line to `print("Exported 9 original SVG/PNG Football layers to", art)`.
- Keep the `end=children.index(keeper)` expression working by replacing `start, end = children.index(marker)+1, children.index(keeper)` with `start, end = children.index(marker)+1, children.index(next(n for n in children if n.get("id") == "keeper"))`.

In `Assets/_Project/Art/Football/GoalView/README.md`, replace the paragraph starting with `The keeper collider capsules use` with:

```markdown
The kicker (the hero, `MaleAdventurer`) and the keeper (`MalePerson`) are Kenney Toon Characters poses loaded by `ToonCharacterArt`; see `Assets/_Project/CREDITS.md`. The keeper's collision capsules in `FootballFlightSimulation` trace the `fall` pose drawn 108x144 preview px with its feet 21 px below the hip; `KeeperSilhouetteTests` fails if the sprite and the capsules drift apart. UI sprites outside GoalView retain their existing source and licensing.
```

- [ ] **Step 9: Run the new tests to verify they pass**

Run tests: `EditMode` `KMA.Tests.Gameplay.Ball.FootballPosesTests` `football-poses`.
Expected: `result="Passed"`, `total="4"`.

Run tests: `EditMode` `KMA.Tests.EditorTools.KeeperSilhouetteTests` `keeper-silhouette`.
Expected: `result="Passed"`. The measured fit is about 88% recall and 89% precision. If the test fails, report the measured numbers; do not lower the thresholds.

- [ ] **Step 10: Rebuild the scene and run the whole Football suite**

Run configurator: method `KMA.EditorTools.FootballSceneConfigurator.BuildScene`, log `configure-football`.
Expected: `exit=0`, and the log contains `[KMA] MG_Football penalty scene built and saved.`

Run tests: `EditMode` `KMA.Tests.Gameplay.Ball` `football-editmode`.
Expected: `failed="0"`. `KeeperReactsAfterDelayAndSavesActualContact` still saves the centre shot.

Run tests: `EditMode` `KMA.Tests.EditorTools.FootballSceneConfiguratorTests` `football-configurator`.
Expected: `failed="0"`.

Run tests: `PlayMode` `KMA.Tests.Gameplay.Football` `football-playmode`.
Expected: `failed="0"`.

- [ ] **Step 11: Commit**

```bash
git add -A Assets/_Project/Scripts/Gameplay/Football Assets/Editor/FootballSceneConfigurator.cs \
  Assets/_Project/Art/Football/GoalView Assets/_Project/Scenes/MG_Football.unity tools/export-football-preview-art.py \
  Assets/Tests/EditMode/Gameplay/Ball/FootballPosesTests.cs Assets/Tests/EditMode/Gameplay/Ball/FootballPosesTests.cs.meta \
  Assets/Tests/EditMode/EditorTools/KeeperSilhouetteTests.cs Assets/Tests/EditMode/EditorTools/KeeperSilhouetteTests.cs.meta
git commit -m "feat(football): the hero takes the penalty against a Toon keeper"
```

---

### Task 4: Volleyball athletes from the library

**Files:**
- Modify: `Assets/Editor/VolleyballSceneConfigurator.cs`
- Modify: `Assets/Tests/EditMode/EditorTools/KMA.EditorTools.EditMode.Tests.asmdef`
- Modify: `Assets/_Project/CREDITS.md`
- Delete: `Assets/_Project/Art/Characters/BeachVolley/` and `Assets/_Project/Art/Characters/BeachVolley.meta`
- Regenerated: `Assets/_Project/Scenes/MG_Volleyball.unity`
- Test: `Assets/Tests/EditMode/EditorTools/VolleyballSceneConfiguratorTests.cs`

**Interfaces:**
- Consumes: from Task 1, `ToonCharacterArt.ImportAll`, `Frames`, `IsPoseOf` and `Hero`. From existing code, `VolleyballController.PlayerView` / `OpponentView` and `VolleyAthleteView.FramesFor(AthleteAction)`.
- Produces, on `VolleyballSceneConfigurator`: `public const string OpponentCharacter = "FemaleAdventurer"` and `public const float AthleteScale = 1.8f`.

- [ ] **Step 1: Reference the Volleyball and Sprint assemblies from the editor tests**

In `Assets/Tests/EditMode/EditorTools/KMA.EditorTools.EditMode.Tests.asmdef`, add `"KMA.Gameplay.Volleyball"` and `"KMA.Gameplay.Sprint"` to `references`, after `"KMA.Gameplay"`. Task 5 needs Sprint.

- [ ] **Step 2: Write the failing test**

Create `Assets/Tests/EditMode/EditorTools/VolleyballSceneConfiguratorTests.cs`:

```csharp
#if UNITY_EDITOR
using System;
using KMA.EditorTools;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KMA.Tests.EditorTools
{
    public sealed class VolleyballSceneConfiguratorTests
    {
        [TearDown]
        public void ReleaseBuiltSceneAfterTest() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void HeroFacesAnUntintedPackOpponent()
        {
            VolleyballSceneConfigurator.BuildScene();
            VolleyballSceneConfigurator.BuildScene();
            EditorSceneManager.OpenScene(VolleyballSceneConfigurator.ScenePath, OpenSceneMode.Single);

            Assert.That(Object.FindObjectsByType<VolleyAthleteView>(FindObjectsSortMode.None), Has.Length.EqualTo(2));
            var controller = Object.FindFirstObjectByType<VolleyballController>();
            AssertDrawnFrom(controller.PlayerView, ToonCharacterArt.Hero);
            AssertDrawnFrom(controller.OpponentView, VolleyballSceneConfigurator.OpponentCharacter);
            Assert.That(Mirrored(controller.PlayerView), Is.False, "The hero is never mirrored.");
            Assert.That(Mirrored(controller.OpponentView), Is.True, "The opponent faces the net.");
            foreach (var view in new[] { controller.PlayerView, controller.OpponentView })
            {
                Assert.That(view.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white), view.name);
                Assert.That(view.transform.localScale, Is.EqualTo(Vector3.one * VolleyballSceneConfigurator.AthleteScale), view.name);
            }
        }

        static bool Mirrored(VolleyAthleteView view) => new SerializedObject(view).FindProperty("mirror").boolValue;

        static void AssertDrawnFrom(VolleyAthleteView view, string character)
        {
            foreach (AthleteAction action in Enum.GetValues(typeof(AthleteAction)))
                foreach (Sprite sprite in view.FramesFor(action))
                    Assert.That(ToonCharacterArt.IsPoseOf(sprite, character), Is.True,
                        $"{view.name} {action} uses {AssetDatabase.GetAssetPath(sprite)}");
        }
    }
}
#endif
```

- [ ] **Step 3: Run the test to verify it fails**

Run tests: `EditMode` `KMA.Tests.EditorTools.VolleyballSceneConfiguratorTests` `volleyball-configurator`.
Expected: compile errors for `OpponentCharacter` and `AthleteScale`.

- [ ] **Step 4: Build the athletes from Toon poses**

In `Assets/Editor/VolleyballSceneConfigurator.cs`:

1. Delete `const string CharacterDir = "Assets/_Project/Art/Characters/BeachVolley";`, `const float AthletePixelsPerUnit = 26f;` and `static readonly Color OpponentTint = ...;`. Add:

```csharp
        public const string OpponentCharacter = "FemaleAdventurer";
        // Toon poses are 1.28 units tall; this matches the court scale the BVA2 athletes were tuned for.
        public const float AthleteScale = 1.8f;
        // Marker sits just above the athlete's head, in world units above the feet.
        const float MarkerWorldHeight = 1.95f;
```

2. In `Textures`, delete the six `CharacterDir + "/player…"` entries, keeping the four environment entries.
3. At the end of `ImportArt()`, after the `ConfigureTexture` loop, add `ToonCharacterArt.ImportAll();`.
4. In `BuildWorld`, replace:

```csharp
            VolleyAthleteView player = Athlete("Player", false, Color.white);
            VolleyAthleteView opponent = Athlete("Opponent", true, OpponentTint);
```

with:

```csharp
            VolleyAthleteView player = Athlete("Player", false, ToonCharacterArt.Hero);
            VolleyAthleteView opponent = Athlete("Opponent", true, OpponentCharacter);
```

5. Replace the whole `Athlete` method with:

```csharp
        static VolleyAthleteView Athlete(string name, bool mirror, string character)
        {
            Sprite[] Poses(params string[] poses) => ToonCharacterArt.Frames(character, poses);
            Sprite[] idle = Poses("idle");
            SpriteRenderer body = Renderer(name, idle[0], Vector3.zero, 0);
            body.transform.localScale = Vector3.one * AthleteScale;
            var book = body.gameObject.AddComponent<SpriteFlipbook>();
            book.Configure(body, idle, true, 12f);
            var view = body.gameObject.AddComponent<VolleyAthleteView>();
            view.Configure(body, book, mirror, idle,
                Poses("run0", "run1", "run2", "run1"),
                Poses("duck", "hold"),
                Poses("jump", "attack1"),
                Poses("jump", "cheer1"),
                Poses("fall", "slide"));
            return view;
        }
```

6. In `AddAthleteMarker`, replace `new Vector3(0f, 1.8f, 0f)` with `new Vector3(0f, MarkerWorldHeight / AthleteScale, 0f)`. Replace `new Vector3(.43f, .43f, 1f)` with `new Vector3(.43f, .43f, 1f) / AthleteScale`, and `new Vector3(.31f, .31f, 1f)` with `new Vector3(.31f, .31f, 1f) / AthleteScale`. The markers are children of the scaled athlete, so this keeps their world size.

- [ ] **Step 5: Remove the BVA2 athlete sheets and credit the art**

```bash
git rm -rq Assets/_Project/Art/Characters/BeachVolley Assets/_Project/Art/Characters/BeachVolley.meta
grep -rn "BeachVolley" Assets --include=*.cs || echo "no BeachVolley references"
```

In `Assets/_Project/CREDITS.md`, replace the whole `## Runner character sprites` section, from its heading through the line ending in `...fallDown}.png`.`, with:

```markdown
## Toon character sprites

Every character in Sprint, Football and Volleyball comes from Kenney's Toon Characters
pack. `MaleAdventurer` is the player in every minigame and is never tinted.

- Source: `https://kenney.nl/assets/toon-characters`
- Pack: Kenney Toon Characters, version 1.0 (2019)
- License: Creative Commons Zero (CC0 1.0)
- Retrieved: 2026-09-29 (HD poses)
- Changes: `PNG/Poses HD/character_<name>_<pose>.png` renamed to `<Folder>_<pose>.png`; pixels unchanged

| Project folder | Pack character | Roles |
| --- | --- | --- |
| `Characters/MaleAdventurer/` | `Male adventurer` | Player in Sprint, Football and Volleyball |
| `Characters/MalePerson/` | `Male person` | Sprint rival (lane 1), Football goalkeeper |
| `Characters/FemalePerson/` | `Female person` | Sprint rival (lane 3) |
| `Characters/FemaleAdventurer/` | `Female adventurer` | Sprint rival (lane 4), Volleyball opponent |

Each folder holds the same poses: `idle run0 run1 run2 hit cheer0 cheer1 fallDown back
climb0 climb1 hurt duck hold jump attack1 slide fall`.

## Beach volleyball environment

The court background, ball and ball shadow under `Art/Environments/Volleyball/` come from
Eidern's "Beach Volley Asset" (`BVA2.zip`).

- Source: `https://eidern.itch.io/beach-volley-new-version`
- Author: Eidern
- License: free to use; the author asks for credit
- Files used: `beachbkgO.png`, `ballRoll.png`, `shadow1.png` (`net0.png` is kept but not drawn); `Pixel.png` is generated by the project
- Changes: none
```

- [ ] **Step 6: Rebuild the scene and run the tests to verify they pass**

Run tests: `EditMode` `KMA.Tests.EditorTools.VolleyballSceneConfiguratorTests` `volleyball-configurator`.
Expected: `result="Passed"`. The test builds `MG_Volleyball.unity` itself.

Run tests: `EditMode` `KMA.Tests.Gameplay.Volleyball` `volleyball-editmode`.
Expected: `failed="0"`.

Run tests: `PlayMode` `KMA.Tests.Gameplay.Volleyball` `volleyball-playmode`.
Expected: `failed="0"`.

- [ ] **Step 7: Commit**

```bash
git add -A Assets/Editor/VolleyballSceneConfigurator.cs Assets/_Project/Art/Characters Assets/_Project/Scenes/MG_Volleyball.unity \
  Assets/_Project/CREDITS.md Assets/Tests/EditMode/EditorTools
git status --short   # nothing left unstaged under Assets/
git commit -m "feat(volleyball): the hero plays a Toon opponent"
```

---

### Task 5: Hero consistency guard and visual check

**Files:**
- Create: `Assets/Tests/EditMode/EditorTools/HeroConsistencyTests.cs`

**Interfaces:**
- Consumes:
  - `ToonCharacterArt.IsPoseOf` and `Hero`.
  - `FootballPresentation.Poses`.
  - `FootballSceneConfigurator.ScenePath`.
  - `VolleyballSceneConfigurator.ScenePath`.
  - `VolleyballController.PlayerView` / `OpponentView`.
  - `RivalRunnerAI.Sprite`.
- Produces: nothing new.

- [ ] **Step 1: Write the guard test**

Create `Assets/Tests/EditMode/EditorTools/HeroConsistencyTests.cs`:

```csharp
#if UNITY_EDITOR
using System;
using KMA.EditorTools;
using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KMA.Tests.EditorTools
{
    /// <summary>The player is the same Toon character in every minigame, untinted, and nobody else is.</summary>
    public sealed class HeroConsistencyTests
    {
        const string SprintScene = "Assets/_Project/Scenes/MG_Sprint.unity";
        const string PlayerRunnerPrefab = "Assets/_Project/Prefabs/Gameplay/PlayerRunnerVisual.prefab";

        [TearDown]
        public void ReleaseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void SprintPlayerAndEveryClipItPlaysAreTheHero()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerRunnerPrefab);
            var renderer = prefab.GetComponentInChildren<SpriteRenderer>();
            AssertHero(renderer.sprite, "Sprint player prefab");
            Assert.That(renderer.color, Is.EqualTo(Color.white), "Sprint player tint");
            foreach (AnimationClip clip in prefab.GetComponent<Animator>().runtimeAnimatorController.animationClips)
                foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    foreach (ObjectReferenceKeyframe key in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                        AssertHero(key.value as Sprite, "Sprint clip " + clip.name);

            EditorSceneManager.OpenScene(SprintScene, OpenSceneMode.Single);
            foreach (var rival in Object.FindObjectsByType<RivalRunnerAI>(FindObjectsSortMode.None))
                AssertNotHero(rival.Sprite.sprite, "Sprint rival lane " + rival.Lane);
        }

        [Test]
        public void FootballKickerIsTheHeroAndTheKeeperIsNot()
        {
            EditorSceneManager.OpenScene(FootballSceneConfigurator.ScenePath, OpenSceneMode.Single);
            FootballPoseSprites poses = Object.FindFirstObjectByType<FootballPresentation>().Poses;
            Assert.That(poses, Is.Not.Null, "Football poses are configured");
            foreach (Sprite sprite in new[] { poses.kickerReady, poses.kickerRunUp, poses.kickerStrike, poses.kickerCelebrate })
                AssertHero(sprite, "Football kicker pose");
            foreach (Sprite sprite in new[] { poses.keeperReady, poses.keeperSave, poses.keeperBeaten })
                AssertNotHero(sprite, "Football keeper pose");
            var player = GameObject.Find("FootballWorld/Player").GetComponent<SpriteRenderer>();
            AssertHero(player.sprite, "Football player renderer");
            Assert.That(player.color, Is.EqualTo(Color.white), "Football player tint");
        }

        [Test]
        public void VolleyballPlayerIsTheHeroAndTheOpponentIsNot()
        {
            EditorSceneManager.OpenScene(VolleyballSceneConfigurator.ScenePath, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<VolleyballController>();
            foreach (AthleteAction action in Enum.GetValues(typeof(AthleteAction)))
            {
                foreach (Sprite sprite in controller.PlayerView.FramesFor(action)) AssertHero(sprite, "Volleyball player " + action);
                foreach (Sprite sprite in controller.OpponentView.FramesFor(action)) AssertNotHero(sprite, "Volleyball opponent " + action);
            }
            Assert.That(controller.PlayerView.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white), "Volleyball player tint");
        }

        static void AssertHero(Sprite sprite, string where)
        {
            Assert.That(sprite, Is.Not.Null, where + ": sprite is missing");
            Assert.That(ToonCharacterArt.IsPoseOf(sprite, ToonCharacterArt.Hero), Is.True,
                where + " uses " + AssetDatabase.GetAssetPath(sprite));
        }

        static void AssertNotHero(Sprite sprite, string where)
        {
            Assert.That(sprite, Is.Not.Null, where + ": sprite is missing");
            Assert.That(ToonCharacterArt.IsPoseOf(sprite, ToonCharacterArt.Hero), Is.False, where + " must not be the hero");
        }
    }
}
#endif
```

`RivalRunnerAI.Lane` and `RivalRunnerAI.Sprite` already exist; `DemoSprintArtConfigurator` and `RunnerVisualTests` use them.

- [ ] **Step 2: Run the test**

Run tests: `EditMode` `KMA.Tests.EditorTools.HeroConsistencyTests` `hero-consistency`.
Expected: `result="Passed"`, `total="3"`. This test guards work already done in Tasks 2–4, so it should pass on the first run. If it fails, the failure message names the scene, clip or pose that still points at the wrong art. Fix that owning task's configurator and re-run it; do not edit the test.

- [ ] **Step 3: Visual check**

Use the `testing-unity-ui-with-screenshots` project skill to capture `MG_Sprint`, `MG_Football` (during aiming, and after a goal) and `MG_Volleyball`. Check each item and write down what you saw:
- **Same hero everywhere:** the hero looks the same in all three: brown hair, green shirt, no tint.
- **Sprint:** the runners stand on their lane lines.
- **Football, kicker:** the kicker is seen from behind, below the goal, and does not overlap the direction panel.
- **Football, keeper:** the keeper stands on the goal line inside the posts.
- **Football, after a goal:** the kicker spreads his arms and the keeper shows `hit`.
- **Volleyball:** the hero faces the net, the opponent faces back towards him, and the diamond markers sit just above both heads.

If any item is wrong, fix the constant that owns it and rebuild with that task's configurator:
- Football kicker: `KickerFeet` or `KickerDisplayWidth`/`KickerDisplayHeight`.
- Volleyball: `AthleteScale` or `MarkerWorldHeight`.

- [ ] **Step 4: Run the full suites**

Run tests: `EditMode` `` `all-editmode` (an empty filter runs everything).
Expected: `failed="0"`.

Run tests: `PlayMode` `` `all-playmode`.
Expected: `failed="0"`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Tests/EditMode/EditorTools/HeroConsistencyTests.cs Assets/Tests/EditMode/EditorTools/HeroConsistencyTests.cs.meta
git status --short   # include any constant or scene changes from Step 3
git commit -m "test(art): guard one hero across Sprint, Football and Volleyball"
```
