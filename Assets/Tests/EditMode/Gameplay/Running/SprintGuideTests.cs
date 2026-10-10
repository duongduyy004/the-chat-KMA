using KMA.Gameplay;
using NUnit.Framework;
using UnityEditor;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class SprintGuideTests
    {
        [Test]
        public void LearnAsksForTheRhythmCountWithoutAClock()
        {
            var pages = SprintGuide.Build(ChallengeKind.Learn, 12, 150f, 22f, 3);
            Assert.That(pages.Count, Is.EqualTo(3));
            Assert.That(pages[0].Title, Is.EqualTo("MỤC TIÊU"));
            Assert.That(pages[0].Instruction,
                Is.EqualTo("Bấm TRÁI, PHẢI luân phiên đúng 12 nhịp liên tiếp. Không tính giờ."));
            Assert.That(pages[1].Title, Is.EqualTo("ĐIỀU KHIỂN"));
            Assert.That(pages[2].Instruction, Is.EqualTo("Bấm sai bên thì chuỗi về 0 và phải đếm lại từ đầu."));
        }

        [Test]
        public void ExamStatesTheRaceAndAddsTheFailurePage()
        {
            var pages = SprintGuide.Build(ChallengeKind.Exam, 0, 150f, 15f, 3);
            Assert.That(pages.Count, Is.EqualTo(4));
            Assert.That(pages[0].Instruction, Is.EqualTo("Chạy 150 m trong 15 giây. Có 3 bạn chạy cùng."));
            Assert.That(pages[1].Instruction, Is.EqualTo(
                "Bấm TRÁI rồi PHẢI luân phiên để chạy. Giữ nhịp đều để lên CHUỖI và BỨT TỐC. Ngừng bấm là chậm lại."));
            Assert.That(pages[2].Instruction, Is.EqualTo(
                "Bấm sai bên thì mất chuỗi và gần như không tăng tốc. Hết giờ chưa về đích là trượt. " +
                "Điểm tính theo độ chính xác, thứ hạng và thời gian còn dư."));
            Assert.That(pages[3].Title, Is.EqualTo("NẾU TRƯỢT"));
        }

        [Test]
        public void FreePlayHasNoFailurePageAndNoRivalSentenceWithoutRivals()
        {
            var pages = SprintGuide.Build(null, 0, 150f, 22f, 0);
            Assert.That(pages.Count, Is.EqualTo(3));
            Assert.That(pages[0].Instruction, Is.EqualTo("Chạy 150 m trong 22 giây."));
        }

        [TestCase("sprint_practice")]
        [TestCase("sprint_exam")]
        public void ObjectiveTextMatchesTheLessonLimits(string id)
        {
            var definition = AssetDatabase.LoadAssetAtPath<ChallengeDefinition>(
                $"Assets/_Project/ScriptableObjects/Journey/{id}.asset");
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Objective,
                Is.EqualTo($"Chạy {definition.Distance:0} m trong tối đa {definition.TimeLimit:0} giây."));
        }
    }
}
