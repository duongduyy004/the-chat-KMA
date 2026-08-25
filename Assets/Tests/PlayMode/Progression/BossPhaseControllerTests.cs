using System;
using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.Boss;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class BossPhaseControllerTests
    {
        [UnityTest]
        public IEnumerator LockedSession_CannotStartBoss()
        {
            var boss = CreateBoss(new GameSession());

            Assert.Throws<InvalidOperationException>(() => boss.Begin());

            DestroyBoss(boss);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossRunsTapRhythmAlternateInOrder()
        {
            var boss = CreateBoss(UnlockedSession());

            boss.Begin();
            Assert.That(boss.CurrentMechanic, Is.EqualTo(ChallengeMechanic.TapMash));
            boss.CompleteCurrent();
            Assert.That(boss.CurrentMechanic, Is.EqualTo(ChallengeMechanic.RhythmHold));
            boss.CompleteCurrent();
            Assert.That(boss.CurrentMechanic, Is.EqualTo(ChallengeMechanic.AlternateTap));

            DestroyBoss(boss);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WrongPhaseCompletion_IsRejectedWithoutAdvancing()
        {
            var boss = CreateBoss(UnlockedSession());
            boss.Begin();

            Assert.Throws<InvalidOperationException>(
                () => boss.CompleteCurrent(ChallengeMechanic.RhythmHold));
            Assert.That(boss.CurrentMechanic, Is.EqualTo(ChallengeMechanic.TapMash));

            DestroyBoss(boss);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompletedSequence_ProducesOnePassingResult()
        {
            var boss = CreateBoss(UnlockedSession());
            MinigameResult result = null;
            var resultCount = 0;
            boss.Resolved += value =>
            {
                result = value;
                resultCount++;
            };

            boss.Begin();
            boss.CompleteCurrent(ChallengeMechanic.TapMash);
            boss.CompleteCurrent(ChallengeMechanic.RhythmHold);
            boss.CompleteCurrent(ChallengeMechanic.AlternateTap);
            yield return null;

            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Pass, Is.True);
            Assert.That(boss.IsComplete, Is.True);
            Assert.That(boss.IsRunning, Is.False);

            DestroyBoss(boss);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Timeout_ProducesOneFailingResult()
        {
            var boss = CreateBoss(UnlockedSession(), .001f);
            MinigameResult result = null;
            boss.Resolved += value => result = value;

            boss.Begin();
            yield return new WaitForSeconds(.02f);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Pass, Is.False);
            Assert.That(boss.IsComplete, Is.True);
            Assert.That(boss.IsRunning, Is.False);

            DestroyBoss(boss);
            yield return null;
        }

        static GameSession UnlockedSession()
        {
            var session = new GameSession();
            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
            {
                session.StartSubject(id);
                session.SubmitResult(id, new MinigameResult(true, 6, Rank.C));
            }

            return session;
        }

        static BossPhaseController CreateBoss(GameSession session)
        {
            return CreateBoss(session, 35f);
        }

        static BossPhaseController CreateBoss(GameSession session, float duration)
        {
            var value = new GameObject("Boss").AddComponent<BossPhaseController>();
            value.Configure(session, ChallengeSequence.BossDefault(), duration);
            return value;
        }

        static void DestroyBoss(BossPhaseController boss)
        {
            UnityEngine.Object.DestroyImmediate(boss.gameObject);
        }
    }
}
