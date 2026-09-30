using TMPro;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    /// Resources holds references, while the font assets themselves stay in Assets/Fonts/TMP.
    public sealed class VietFontLibrary : ScriptableObject
    {
        public TMP_FontAsset title;
        public TMP_FontAsset buttonHud;
        public TMP_FontAsset regular;
        public TMP_FontAsset bold;
        public Material titleMaterial;
        public Material primaryMaterial;
        public Material secondaryMaterial;
        public Material bodyMaterial;
        public Material bodyBoldMaterial;
    }
}
