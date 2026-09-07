#if UNITY_EDITOR
using System;
using System.Linq;
using KMA.Gameplay;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KMA.EditorTools
{
    /// <summary>Repeatable demo artwork authoring through Unity's asset and scene APIs.</summary>
    public static class DemoSprintArtConfigurator
    {
        const string Art = "Assets/_Project/Art/";
        const string Animations = "Assets/_Project/Animations/";
        const string PlayerPrefab = "Assets/_Project/Prefabs/Gameplay/PlayerRunnerVisual.prefab";
        const string RivalPrefab = "Assets/_Project/Prefabs/Gameplay/RivalRunner.prefab";

        [MenuItem("KMA/Demo/Configure Sprint Artwork")]
        public static void Configure()
        {
            AssetDatabase.Refresh();
            var idle = ImportSprite("Characters/Runner/Runner_Idle.png", true);
            var run = new[] { "Run0", "Run1", "Run2" }
                .Select(p => ImportSprite("Characters/Runner/Runner_" + p + ".png", true)).ToArray();
            var hit = ImportSprite("Characters/Runner/Runner_Hit.png", true);
            foreach (string state in new[] { "Idle", "Run", "Burst", "Stumble", "Celebrate", "Fail" })
                AuthorClip(state, idle, run, hit);
            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Animations + "RivalRunner.controller");
            AuthorRival(idle);
            AuthorPlayer(idle, animatorController);

            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MG_Sprint.unity");
            var player = GameObject.Find("Player");
            if (player == null) throw new InvalidOperationException("Sprint Player root is missing.");
            foreach (Transform child in player.transform.Cast<Transform>().ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            var playerVisual = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab), player.transform);
            playerVisual.transform.localPosition = new Vector3(0f, -1.6f - player.transform.position.y, 0f);

            foreach (var rival in UnityEngine.Object.FindObjectsByType<RivalRunnerAI>(FindObjectsSortMode.None))
            {
                var visual = rival.transform.Find("Visual");
                // Preserve the lane roots used by race mappings; only place the feet on the illustrated track.
                float feetY = rival.Lane == 1 ? -0.6f : rival.Lane == 3 ? -2.6f : -3.6f;
                visual.localPosition = new Vector3(0f, feetY - rival.transform.position.y, 0f);
                var renderer = visual.GetComponent<SpriteRenderer>();
                renderer.sprite = idle;
                renderer.sortingOrder = 10 + rival.Lane;
                renderer.color = Color.white;
                PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }

            var parallax = UnityEngine.Object.FindFirstObjectByType<SprintParallax>();
            var serialized = new SerializedObject(parallax);
            var layers = serialized.FindProperty("layers");
            var names = new[] { "Sky", "Campus", "Track" };
            for (int i = 0; i < names.Length; i++)
            {
                var sprite = ImportSprite("Environments/Sprint/" + names[i] + ".png", false);
                var layer = layers.GetArrayElementAtIndex(i);
                foreach (string field in new[] { "first", "second" })
                {
                    var tile = (Transform)layer.FindPropertyRelative(field).objectReferenceValue;
                    var renderer = tile.GetComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.drawMode = SpriteDrawMode.Simple;
                    renderer.sharedMaterial = SpriteMaterial();
                    renderer.color = Color.white;
                    renderer.flipX = field == "second";
                    renderer.sortingOrder = -30 + i * 10;
                    tile.localScale = new Vector3(25.6f / sprite.bounds.size.x,
                        (i == 2 ? 14f : 10.8f) / sprite.bounds.size.y, 1f);
                    // Extend track down to the viewport edge while raising its first lane above the controls.
                    var position = tile.localPosition;
                    position.y = i == 2 ? 1.6f : 0f;
                    tile.localPosition = position;
                }
            }
            EditorSceneManager.SaveScene(scene);
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Brand/AppIcon.png");
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                var slots = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                foreach (var slot in slots)
                {
                    var textures = new Texture2D[slot.maxLayerCount];
                    textures[0] = icon;
                    for (int layer = 1; layer < textures.Length; layer++)
                        textures[layer] = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Brand/GameLogo.png");
                    slot.SetTextures(textures);
                }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, slots);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA] Sprint sprite clips, player prefab, parallax artwork and app icon configured.");
        }

        static Sprite ImportSprite(string relativePath, bool runner)
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
            settings.spriteAlignment = runner ? (int)SpriteAlignment.BottomCenter : (int)SpriteAlignment.Center;
            settings.spritePivot = runner ? new Vector2(.5f, 0f) : new Vector2(.5f, .5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void AuthorClip(string state, Sprite idle, Sprite[] run, Sprite hit)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Animations + "RivalRunner_" + state + ".anim");
            if (clip == null) throw new InvalidOperationException("Missing runner clip: " + state);
            clip.ClearCurves();
            bool running = state == "Run" || state == "Burst";
            float interval = state == "Burst" ? .075f : .12f;
            var frames = running ? new[] { run[0], run[1], run[2], run[1], run[0] }
                : new[] { state == "Stumble" || state == "Fail" ? hit : idle,
                    state == "Stumble" || state == "Fail" ? hit : idle };
            float duration = running ? interval * 4 : .6f;
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("Visual", typeof(SpriteRenderer), "m_Sprite"),
                frames.Select((sprite, index) => new ObjectReferenceKeyframe
                { time = running ? index * interval : index * duration, value = sprite }).ToArray());
            // Scale motion retains authored state feedback without fighting race-owned localPosition.x.
            clip.SetCurve("Visual", typeof(Transform), "m_LocalScale.y",
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(duration / 2, state == "Celebrate" ? 1.12f : 1.03f),
                    new Keyframe(duration, 1f)));
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        static void AuthorRival(Sprite idle)
        {
            var root = PrefabUtility.LoadPrefabContents(RivalPrefab);
            var renderer = root.transform.Find("Visual").GetComponent<SpriteRenderer>();
            renderer.sprite = idle;
            renderer.sharedMaterial = SpriteMaterial();
            renderer.color = Color.white;
            renderer.transform.localScale = Vector3.one;
            root.GetComponent<Animator>().applyRootMotion = false;
            PrefabUtility.SaveAsPrefabAsset(root, RivalPrefab);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void AuthorPlayer(Sprite idle, RuntimeAnimatorController controller)
        {
            var root = new GameObject("PlayerRunnerVisual");
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            root.AddComponent<RunnerVisualPresenter>();
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = idle;
            renderer.sharedMaterial = SpriteMaterial();
            renderer.sortingOrder = 12;
            var label = new GameObject("PlayerLabel");
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0, 1.65f, 0);
            var text = label.AddComponent<TextMesh>();
            text.text = "PLAYER";
            text.fontSize = 48;
            text.characterSize = .065f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color32(255, 202, 58, 255);
            label.GetComponent<MeshRenderer>().sortingOrder = 20;
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            UnityEngine.Object.DestroyImmediate(root);
        }

        static Material SpriteMaterial()
        {
            const string path = "Assets/_Project/Art/DemoSpriteUnlit.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new InvalidOperationException("URP sprite unlit shader is missing.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
#endif
