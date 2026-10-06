using UnityEditor;
using UnityEngine;

namespace KMA.EditorTools
{
    /// One-off migration that writes the unified kit tokens into the shared UITheme asset.
    public static class UiThemeMigration
    {
        const string ThemePath = "Assets/_Project/Settings/UI/UITheme.asset";

        [MenuItem("KMA/UI/Apply Theme Tokens")]
        public static void Apply()
        {
            var theme = AssetDatabase.LoadAssetAtPath<Object>(ThemePath);
            var so = new SerializedObject(theme);
            so.FindProperty("shadowOffset").vector2Value = new Vector2(0f, -4f);
            so.FindProperty("disabledSurface").colorValue = new Color32(74, 92, 110, 255);
            so.FindProperty("disabledText").colorValue = new Color32(190, 201, 212, 255);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
        }
    }
}
