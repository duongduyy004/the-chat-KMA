using System;
using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.Boss;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class BossPhaseControllerTests
    {
        [UnityTest]
        public IEnumerator SceneLoadsSerializedAssetAndRuntimeAdapters()
        {
            yield return LoadBoss();
            var boss = UnityEngine.Object.FindFirstObjectByType<BossPhaseController>();

            Assert.That(boss.SequenceAsset, Is.Not.Null);
            Assert.That(boss.SequenceAsset.CreateRuntimeSequence().Current.Mechanic,
                Is.EqualTo(ChallengeMechanic.TapMash));
            Assert.That(boss.Session, Is.Not.Null);
            Assert.That(boss.TapMashDetector, Is.Not.Null);
            Assert.That(boss.RhythmHoldDetector, Is.Not.Null);
            Assert.That(boss.AlternateTapDetector, Is.Not.Null);
            Assert.That(boss.RuntimeInputSource, Is.Not.Null);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneStartPath_UsesProductionSessionHandoff()
        {
            yield return LoadBoss();
            var boss = UnityEngine.Object.FindFirstObjectByType<BossPhaseController>();

            Assert.That(boss.Session, Is.Not.Null);
            Assert.That(boss.Session.BossUnlocked, Is.True);

            yield return WaitForPlay();
            Assert.DoesNotThrow(() => boss.Begin());
            Assert.That(boss.IsRunning, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LockedSession_CannotStartBoss()
        {
            yield return LoadBoss();
            var boss = UnityEngine.Object.FindFirstObjectByType<BossPhaseController>();
            boss.SetSession(new GameSession());
            yield return WaitForPlay();

            Assert.Throws<InvalidOperationException>(() => boss.Begin());

            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneRuntimeInput_ProgressesAllPhasesThroughKeyboard()
        {
            yield return LoadBoss();
            var boss = UnityEngine.Object.FindFirstObjectByType<BossPhaseController>();
            yield return WaitForPlay();
            boss.Begin();
            var input = boss.RuntimeInputSource;

            for (var tap = 0; tap < 40; tap++)
            {
                input.OnTapMashPressed();
                yield return null;
            }
            Assert.That(boss.CurrentMechanic, Is.EqualTo(ChallengeMechanic.RhythmHold));

            for (var beat = 0; beat < 16; beat++)
            {
                input.OnRhythmHoldReleased(.51f);
                yield return null;
            }
            Assert.That(boss.CurrentMechanic, Is.EqualTo(ChallengeMechanic.AlternateTap));

            for (var alternate = 0; alternate < 32; alternate++)
            {
                input.OnAlternateTapPressed(alternate % 2 == 0 ? BossTapSide.Left : BossTapSide.Right);
                yield return null;
            }
            yield return null;

            Assert.That(boss.IsComplete, Is.True);
            Assert.That(boss.LastResult.Pass, Is.True);
        }

        [UnityTest]
        public IEnumerator ConfigureRejectsReorderedChallengeSequence()
        {
            yield return LoadBoss();
            var boss = UnityEngine.Object.FindFirstObjectByType<BossPhaseController>();
            var reordered = new ChallengeSequence(new[]
            {
                new ChallengeStep(ChallengeMechanic.RhythmHold, 12f, 16f),
                new ChallengeStep(ChallengeMechanic.TapMash, 10f, 40f),
                new ChallengeStep(ChallengeMechanic.AlternateTap, 10f, 32f)
            });

            Assert.Throws<InvalidOperationException>(() =>
                boss.Configure(UnlockedSession(), reordered, 35f));

            var foreign = new ChallengeSequence(new[]
            {
                new ChallengeStep(ChallengeMechanic.TapMash, 1f, 1f),
                new ChallengeStep(ChallengeMechanic.RhythmHold, 1f, 1f),
                new ChallengeStep(ChallengeMechanic.AlternateTap, 1f, 1f)
            });
            Assert.Throws<InvalidOperationException>(() =>
                boss.Configure(UnlockedSession(), foreign, 35f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator BossConsumesEventsInCanonicalOrderAndWrongInputsDoNotAdvance()
        {
            yield return LoadBoss();
            var boss = UnityEngine.Object.FindFirstObjectByType<BossPhaseController>();
            boss.SetSession(UnlockedSession());
            yield return WaitForPlay();
            boss.Begin();

            boss.RhythmHoldDetector.SubmitHold(1f);
            Assert.That(boss.CurrentMechanic, Is.EqualTo(ChallengeMechanic.TapMash));
            for (var tap = 0; tap < 39; tap++)
                boss.TapMashDetector.SubmitTap();
            Assert.That(boss.CurrentProgress, Is.EqualTo(39f));
            boss.TapMashDetector.SubmitTap();
            Assert.That(boss.CurrentMechanic, Is.EqualTo(ChallengeMechanic.RhythmHold));

            boss.RhythmHoldDetector.SubmitHold(.1f);
            Assert.That(boss.CurrentProgress, Is.Zero);
            for (var beat = 0; beat < 16; beat++)
                boss.RhythmHoldDetector.SubmitHold(1f);
            Assert.That(boss.CurrentMechanic, Is.EqualTo(ChallengeMechanic.AlternateTap));

            boss.AlternateTapDetector.SubmitTap(BossTapSide.Left);
            boss.AlternateTapDetector.SubmitTap(BossTapSide.Left);
            Assert.That(boss.CurrentProgress, Is.EqualTo(1f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator AuthoredPhaseDurationFailsBeforeTargetIsReached()
        {
            yield return LoadBoss();
            var boss = UnityEngine.Object.FindFirstObjectByType<BossPhaseController>();
            boss.SetSession(UnlockedSession());
            yield return WaitForPlay();
            boss.Begin();

            yield return new WaitForSeconds(10.2f);

            Assert.That(boss.IsComplete, Is.True);
            Assert.That(boss.LastResult, Is.Not.Null);
            Assert.That(boss.LastResult.Pass, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompletionUsesFoundationLifecycleAndScoreUtilExactlyOnce()
        {
            yield return LoadBoss();
            var boss = UnityEngine.Object.FindFirstObjectByType<BossPhaseController>();
            boss.SetSession(UnlockedSession());
            yield return WaitForPlay();
            boss.Begin();

            for (var tap = 0; tap < 40; tap++)
                boss.TapMashDetector.SubmitTap();
            for (var beat = 0; beat < 16; beat++)
                boss.RhythmHoldDetector.SubmitHold(1f);
            for (var alternate = 0; alternate < 32; alternate++)
                boss.AlternateTapDetector.SubmitTap(alternate % 2 == 0 ? BossTapSide.Left : BossTapSide.Right);
            yield return null;

            Assert.That(typeof(BossPhaseController).IsSubclassOf(typeof(MinigameBase)), Is.True);
            Assert.That(typeof(BossPhaseController).GetMethod("CompleteCurrent"), Is.Null);
            Assert.That(boss.Phase, Is.EqualTo(MinigamePhase.Resolve));
            Assert.That(boss.LastResult, Is.Not.Null);
            Assert.That(boss.LastResult.Pass, Is.True);
            Assert.That(boss.LastResult.Score, Is.EqualTo(10f));
            Assert.That(boss.LastResult.Rank, Is.EqualTo(Rank.S));
            Assert.That(boss.CompletionCount, Is.EqualTo(1));

            yield return null;
            Assert.That(boss.CompletionCount, Is.EqualTo(1));
        }

        static IEnumerator LoadBoss()
        {
            var operation = SceneManager.LoadSceneAsync("MG_Boss", LoadSceneMode.Single);
            while (!operation.isDone)
                yield return null;

            yield return null;
        }

        static IEnumerator WaitForPlay()
        {
            yield return new WaitForSeconds(5.1f);
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
    }
}
