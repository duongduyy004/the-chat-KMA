using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class HeartBar : MonoBehaviour
    {
        const int SlotCount = 5;

        [SerializeField] Image[] slots = new Image[SlotCount];
        [SerializeField] Color filledColor = new Color32(0xFF, 0x59, 0x5E, 0xFF);
        [SerializeField] Color emptyColor = new Color32(0xE2, 0xE8, 0xF0, 0xFF);

        public int CurrentHearts { get; private set; }
        public Color FilledColor => filledColor;
        public Color EmptyColor => emptyColor;

        public void SetHearts(int hearts)
        {
            CurrentHearts = Mathf.Clamp(hearts, 0, SlotCount);
            for (var index = 0; index < slots.Length && index < SlotCount; index++)
            {
                if (slots[index] != null)
                    slots[index].color = index < CurrentHearts ? filledColor : emptyColor;
            }
        }
    }
}
