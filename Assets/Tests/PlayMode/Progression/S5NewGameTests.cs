using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class S5NewGameTests
    {
        [Test]
        public void ResetCampaign_ClearsRecordsAndRestoresFiveLives()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);
            session.SubmitResult(SubjectId.Sprint, new MinigameResult(true, 8f, Rank.A));

            session.ResetCampaign();

            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.BossUnlocked, Is.False);
            Assert.That(session.GetRecord(SubjectId.Sprint).Passed, Is.False);
        }

        [Test]
        public void MapScreen_OnlyRaisesBossRequestWhenUnlocked()
        {
            var screen = new GameObject("MapScreen").AddComponent<MapScreen>();
            try
            {
                var calls = 0;
                screen.BossRequested += () => calls++;
                screen.SelectBoss();
                screen.SetBossUnlocked(true);
                screen.SelectBoss();
                Assert.That(calls, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void CalibrateScreen_ClampsOffsetToDeviceSafeRange()
        {
            var screen = new GameObject("CalibrateScreen").AddComponent<CalibrateScreen>();
            try
            {
                screen.SetOffset(999f);
                Assert.That(screen.RhythmOffsetMs, Is.EqualTo(500f));
                screen.SetOffset(-999f);
                Assert.That(screen.RhythmOffsetMs, Is.EqualTo(-500f));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }
    }
}
