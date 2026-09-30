using System;
using KMA.Gameplay.UI;
using TMPro;
using UnityEngine;

namespace KMA.UI.Kit
{
    /// The baked sprites, font and stroke material every minigame UI is drawn with. It lives in a
    /// Resources folder so runtime builders (Sprint, the pause menu, the tutorial) and editor
    /// configurators load the same asset without a scene reference.
    public sealed class UiKitAssets : ScriptableObject
    {
        public const string ResourcePath = "UiKitAssets";
        public const string AssetPath = "Assets/_Project/Settings/UI/Resources/UiKitAssets.asset";

        [SerializeField] UITheme theme;
        public UITheme Theme => theme;
        public void SetTheme(UITheme value) => theme = value;

        [SerializeField] Sprite roundRect20;
        [SerializeField] Sprite roundRect24;
        [SerializeField] Sprite roundRect36;
        [SerializeField] Sprite circle;
        [SerializeField] Sprite ring;
        [SerializeField] TMP_FontAsset font;
        [SerializeField] Material outlineMaterial;

        static UiKitAssets cached;

        public Sprite RoundRect20 => roundRect20;
        public Sprite RoundRect24 => roundRect24;
        public Sprite RoundRect36 => roundRect36;
        public Sprite Circle => circle;
        public Sprite Ring => ring;
        public TMP_FontAsset Font => font;
        public Material OutlineMaterial => outlineMaterial;

        public static UiKitAssets Load()
        {
            if (cached == null)
                cached = Resources.Load<UiKitAssets>(ResourcePath);
            if (cached == null)
                throw new InvalidOperationException($"Missing {AssetPath}; run KMA/UI/Bake UI Kit Sprites.");
            return cached;
        }

        /// The baked rounded rect with this radius, or RoundRect24 for any other radius; callers
        /// scale its corners with Image.pixelsPerUnitMultiplier.
        public Sprite RoundRectFor(float radius)
        {
            if (Mathf.Approximately(radius, 20f))
                return roundRect20;
            if (Mathf.Approximately(radius, 36f))
                return roundRect36;
            return roundRect24;
        }

        public Sprite[] AllSprites() => new[] { roundRect20, roundRect24, roundRect36, circle, ring };

        public void Configure(Sprite roundRect20Sprite, Sprite roundRect24Sprite, Sprite roundRect36Sprite,
            Sprite circleSprite, Sprite ringSprite, TMP_FontAsset fontAsset, Material strokeMaterial)
        {
            roundRect20 = roundRect20Sprite;
            roundRect24 = roundRect24Sprite;
            roundRect36 = roundRect36Sprite;
            circle = circleSprite;
            ring = ringSprite;
            font = fontAsset;
            outlineMaterial = strokeMaterial;
        }
    }
}
