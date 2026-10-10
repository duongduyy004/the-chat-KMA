using KMA.Gameplay.Chess;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class ChessGuideTests
    {
        [Test]
        public void PagesCarryThePuzzleNumbers()
        {
            var pages = ChessGuide.Build(2, 90f, 2);
            Assert.That(pages.Count, Is.EqualTo(3));
            Assert.That(pages[0].Instruction, Is.EqualTo(
                "Chiếu hết trong 2 nước. Thời gian suy nghĩ một phút rưỡi. Bài thi cuối không mất mạng."));
            Assert.That(pages[1].Instruction, Is.EqualTo(
                "Chạm quân của bạn, rồi chạm ô muốn đi tới. Tốt phong cấp thì chọn quân muốn đổi. " +
                "Nút GỢI Ý có 3 mức, từ ý tưởng tới nước đi cụ thể."));
            Assert.That(pages[2].Instruction, Is.EqualTo(
                "Nước không hợp lệ thì không tính. Đi sai bị tính 1 lỗi, bàn cờ giữ nguyên. " +
                "Sai quá 2 lần hoặc hết giờ là trượt. Đồng hồ chỉ chạy trong lượt của bạn. " +
                "Dùng gợi ý, đi sai và đi chậm đều bị trừ điểm."));
        }

        [TestCase(60f, "một phút")]
        [TestCase(90f, "một phút rưỡi")]
        [TestCase(120f, "hai phút")]
        [TestCase(45f, "45 giây")]
        public void TimeWordsReadAloud(float seconds, string expected) =>
            Assert.That(ChessGuide.TimeWords(seconds), Is.EqualTo(expected));
    }
}
