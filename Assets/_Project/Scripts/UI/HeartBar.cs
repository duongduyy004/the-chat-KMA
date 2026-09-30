using KMA.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class HeartBar : MonoBehaviour
    {
        const int SlotCount = 5;

        [SerializeField] Image[] slots = new Image[SlotCount];

        public int CurrentHearts { get; private set; }
        public Color FilledColor => MinigameUiTheme.Energy;
        public Color EmptyColor => MinigameUiTheme.WithAlpha(MinigameUiTheme.TextPrimary, .25f);

        public void SetSlots(Image[] value) => slots = value ?? new Image[SlotCount];

        public void SetHearts(int hearts)
        {
            CurrentHearts = Mathf.Clamp(hearts, 0, SlotCount);
            for (var index = 0; index < slots.Length && index < SlotCount; index++)
            {
                if (slots[index] != null)
                    slots[index].color = index < CurrentHearts ? FilledColor : EmptyColor;
            }
        }
    }
}
