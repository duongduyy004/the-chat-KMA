using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.UI.Kit
{
    public enum ButtonVariant { Primary, Secondary, Danger }

    public readonly struct ButtonHandle
    {
        public readonly Button Button;
        public readonly Image Face;
        public readonly Image Fill;
        public readonly TMP_Text Label;
        public readonly KitPressFeedback Feedback;

        public ButtonHandle(Button button, Image face, Image fill, TMP_Text label, KitPressFeedback feedback)
        {
            Button = button;
            Face = face;
            Fill = fill;
            Label = label;
            Feedback = feedback;
        }
    }

    public readonly struct ChipHandle
    {
        public readonly Image Background;
        public readonly TMP_Text Label;

        public ChipHandle(Image background, TMP_Text label)
        {
            Background = background;
            Label = label;
        }
    }

    public readonly struct RoundButtonHandle
    {
        public readonly RectTransform Root;
        public readonly Image Shadow;
        public readonly Image Rim;
        public readonly Image Face;
        public readonly TMP_Text Label;
        public readonly KitPressFeedback Feedback;

        public RoundButtonHandle(RectTransform root, Image shadow, Image rim, Image face, TMP_Text label,
            KitPressFeedback feedback)
        {
            Root = root;
            Shadow = shadow;
            Rim = rim;
            Face = face;
            Label = label;
            Feedback = feedback;
        }
    }

    public readonly struct ControlPlateHandle
    {
        public readonly RectTransform Visual;
        public readonly Image Border;
        public readonly Image Background;
        public readonly TMP_Text Arrow;
        public readonly TMP_Text Label;

        public ControlPlateHandle(RectTransform visual, Image border, Image background, TMP_Text arrow, TMP_Text label)
        {
            Visual = visual;
            Border = border;
            Background = background;
            Arrow = arrow;
            Label = label;
        }
    }

    public readonly struct SliderHandle
    {
        public readonly Slider Slider;
        public readonly Image Track;
        public readonly Image Knob;

        public SliderHandle(Slider slider, Image track, Image knob)
        {
            Slider = slider;
            Track = track;
            Knob = knob;
        }
    }

    public readonly struct JoystickHandle
    {
        public readonly Image Base;
        public readonly Image Rim;
        public readonly Image Knob;

        public JoystickHandle(Image stickBase, Image rim, Image knob)
        {
            Base = stickBase;
            Rim = rim;
            Knob = knob;
        }
    }
}
