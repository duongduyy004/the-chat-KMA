using System.IO;
using System.Linq;
using KMA.Gameplay.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KMA.EditorTools
{
    public static class VietFontTestScene
    {
        public const string Path = "Assets/Scenes/Dev/FontTest.unity";

        public static void PrepareAndVerify()
        {
            VietFontMigration.ApplyProject();
            Create();
            FontCoverageChecker.Run();
            // Check setup reruns do not change persistent asset identities or create duplicates.
            var paths = AssetDatabase.FindAssets("", new[] { VietFontSetup.Root })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => !AssetDatabase.IsValidFolder(p)).OrderBy(p => p).ToArray();
            var ids = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
            VietFontSetup.Setup();
            var after = AssetDatabase.FindAssets("", new[] { VietFontSetup.Root })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => !AssetDatabase.IsValidFolder(p)).OrderBy(p => p).ToArray();
            if (!paths.SequenceEqual(after) || !ids.SequenceEqual(after.Select(AssetDatabase.AssetPathToGUID)))
                throw new System.InvalidOperationException("Setup rerun changed asset paths/GUIDs.");
            Debug.Log($"[VietFont] Idempotence PASS: {paths.Length} font/material assets, identical paths and GUIDs after rerun.");
        }

        [MenuItem("Tools/KMA/Create Vietnamese Font Test Scene")]
        public static void Create()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var fonts = VietTypography.Library;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(22, 44, 64, 255);
                camera.cullingMask = 0;
                var canvasObject = new GameObject("Vietnamese Font Test", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                scaler.matchWidthOrHeight = .5f;
                var background = Rect(canvasObject.transform, "Background", Vector2.zero, new Vector2(1920, 1080));
                background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one;
                background.offsetMin = background.offsetMax = Vector2.zero;
                background.gameObject.AddComponent<Image>().color = new Color32(22, 44, 64, 255);
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                Label(canvasObject.transform, "Heading", "KIỂM THỬ FONT TIẾNG VIỆT — 4 FONT × 4 MATERIAL", new Vector2(0, 505), new Vector2(1840, 65), fonts.regular, fonts.bodyMaterial, 30);
                var assets = new[] { fonts.title, fonts.buttonHud, fonts.regular, fonts.bold };
                var presets = new[] { fonts.titleMaterial, fonts.primaryMaterial, fonts.secondaryMaterial, fonts.bodyMaterial };
                Directory.CreateDirectory(VietFontSetup.Root + "/Materials/FontTest");
                AssetDatabase.Refresh();
                for (int row = 0; row < assets.Length; row++)
                    for (int col = 0; col < presets.Length; col++)
                    {
                        float x = -690 + col * 460, y = 375 - row * 195;
                        var panel = Rect(canvasObject.transform, "Cell_" + row + "_" + col, new Vector2(x, y), new Vector2(445, 180));
                        panel.gameObject.AddComponent<Image>().color = new Color32(43, 69, 91, 255);
                        Label(panel, "Caption", assets[row].name + " / " + presets[col].name,
                            new Vector2(0, 65), new Vector2(425, 35), fonts.regular, fonts.bodyMaterial, 18);
                        // A material preset must always bind the atlas of the font under test.
                        string materialPath = VietFontSetup.Root + "/Materials/FontTest/" + assets[row].name + "_" + presets[col].name + ".mat";
                        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (material == null) { material = new Material(presets[col]); AssetDatabase.CreateAsset(material, materialPath); }
                        else material.CopyPropertiesFromMaterial(presets[col]);
                        material.SetTexture(ShaderUtilities.ID_MainTex, assets[row].atlasTexture);
                        material.SetFloat(ShaderUtilities.ID_GradientScale, assets[row].material.GetFloat(ShaderUtilities.ID_GradientScale));
                        EditorUtility.SetDirty(material);
                        var sample = Label(panel, "Sample", VietFontSetup.Sample, new Vector2(0, -15), new Vector2(420, 115), assets[row], material, 27);
                        if (col == 0)
                        {
                            sample.enableVertexGradient = true;
                            var top = new Color32(255, 224, 102, 255); var bottom = new Color32(255, 180, 0, 255);
                            sample.colorGradient = new VertexGradient(top, top, bottom, bottom);
                        }
                    }
                Label(canvasObject.transform, "InputHint", "Gõ tiếng Việt bằng Telex/VNI của hệ điều hành; kết thúc nhập để chuẩn hóa NFC.",
                    new Vector2(0, -385), new Vector2(1800, 40), fonts.regular, fonts.bodyMaterial, 24);
                var inputRoot = Rect(canvasObject.transform, "VietnameseInput", new Vector2(0, -460), new Vector2(1800, 85));
                inputRoot.gameObject.AddComponent<Image>().color = new Color32(243, 247, 250, 255);
                var area = Rect(inputRoot, "TextArea", Vector2.zero, new Vector2(1760, 75));
                // Leave comfortable inset around ascenders/stacked accents in the viewport.
                var inputText = Label(area, "InputText", "", Vector2.zero, new Vector2(1740, 65), fonts.regular, fonts.bodyMaterial, 30);
                inputText.color = new Color32(11, 42, 74, 255); inputText.alignment = TextAlignmentOptions.Left;
                var placeholder = Label(area, "Placeholder", "Nguyễn Quỳnh — thử Ầ Ẫ Ệ Ộ Ữ", Vector2.zero, new Vector2(1740, 65), fonts.regular, fonts.bodyMaterial, 30);
                placeholder.color = new Color32(93, 112, 130, 255); placeholder.alignment = TextAlignmentOptions.Left;
                var input = inputRoot.gameObject.AddComponent<TMP_InputField>();
                input.textViewport = area; input.textComponent = inputText; input.placeholder = placeholder;
                input.lineType = TMP_InputField.LineType.SingleLine;
                inputRoot.gameObject.AddComponent<VietInputNormalizer>();
                Directory.CreateDirectory("Assets/Scenes/Dev");
                AssetDatabase.Refresh();
                EditorSceneManager.SaveScene(scene, Path);
                AssetDatabase.SaveAssets();
                Debug.Log("[VietFont] Created " + Path + ": 16 font/material samples and NFC input.");
            }
            finally { VietFontMigration.RestoreScenes(previous); }
        }

        static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }

        static TextMeshProUGUI Label(Transform parent, string name, string value, Vector2 position, Vector2 size,
            TMP_FontAsset font, Material material, float fontSize)
        {
            var text = Rect(parent, name, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            text.text = VietText.Fix(value); text.font = font; text.fontSharedMaterial = material;
            text.fontSize = fontSize; text.color = Color.white; text.extraPadding = true;
            text.lineSpacing = 15; text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow; text.raycastTarget = false;
            return text;
        }
    }
}
