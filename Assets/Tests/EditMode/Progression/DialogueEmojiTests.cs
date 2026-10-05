using System.Linq;
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class DialogueEmojiTests
    {
        [Test]
        public void KnownNamesListTheFifteenScriptEmojiInOrder()
        {
            Assert.That(DialogueEmoji.KnownNames, Is.EqualTo(new[]
            {
                "sob", "skull", "sunglasses", "fire", "scream", "runner", "dash", "soccer", "volleyball",
                "eyes", "clown", "salute", "100", "tada", "muscle"
            }));
            Assert.That(DialogueEmoji.IsKnown("sob"), Is.True);
            Assert.That(DialogueEmoji.IsKnown("hot"), Is.False);
            Assert.That(DialogueEmoji.IsKnown(null), Is.False);
        }

        [Test]
        public void ExpandTurnsKnownCodesIntoSpriteTags()
        {
            Assert.That(DialogueEmoji.Expand("Toang :sob:"), Is.EqualTo("Toang <sprite name=\"sob\">"));
            Assert.That(DialogueEmoji.Expand(":runner::dash:"),
                Is.EqualTo("<sprite name=\"runner\"><sprite name=\"dash\">"));
            Assert.That(DialogueEmoji.Expand("QUA RỒI :sob::sob: nha"),
                Is.EqualTo("QUA RỒI <sprite name=\"sob\"><sprite name=\"sob\"> nha"));
            Assert.That(DialogueEmoji.Expand("điểm danh :100:"), Is.EqualTo("điểm danh <sprite name=\"100\">"));
        }

        [Test]
        public void ExpandLeavesOrdinaryColonsAndUnknownCodesAlone()
        {
            Assert.That(DialogueEmoji.Expand("Lộ trình: chạy, rồi bóng"), Is.EqualTo("Lộ trình: chạy, rồi bóng"));
            Assert.That(DialogueEmoji.Expand("Thi: 5 cú sút"), Is.EqualTo("Thi: 5 cú sút"));
            Assert.That(DialogueEmoji.Expand("lạ :xyz: rồi :fire:"),
                Is.EqualTo("lạ :xyz: rồi <sprite name=\"fire\">"));
            Assert.That(DialogueEmoji.Expand(":xyz::fire:"), Is.EqualTo(":xyz:<sprite name=\"fire\">"));
            Assert.That(DialogueEmoji.Expand(""), Is.EqualTo(""));
            Assert.That(DialogueEmoji.Expand(null), Is.Null);
        }

        [Test]
        public void FindCodesReturnsEveryCodeIncludingUnknownOnes()
        {
            Assert.That(DialogueEmoji.FindCodes("a :sob: b :xyz::fire: Lộ trình: x").ToArray(),
                Is.EqualTo(new[] { "sob", "xyz", "fire" }));
            Assert.That(DialogueEmoji.FindCodes(null), Is.Empty);
        }
    }
}
