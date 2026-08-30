using UnityEngine;

namespace KMA.Gameplay
{
    [CreateAssetMenu(menuName = "KMA/Instructor Quote Set", fileName = "InstructorQuoteSet")]
    public sealed class InstructorQuoteSet : ScriptableObject
    {
        public string[] chill =
        {
            "Bình tĩnh, giữ nhịp và làm lại nào.",
            "Tốt rồi, cứ đều tay như thế."
        };

        public string[] urgent =
        {
            "Nhanh lên, thời gian không chờ đâu!",
            "Tập trung! Còn một chút nữa thôi!"
        };
    }
}
