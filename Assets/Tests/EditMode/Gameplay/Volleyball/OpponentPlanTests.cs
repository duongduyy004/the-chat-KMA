using System;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class OpponentPlanTests
    {
        [Test]
        public void AuthoredPlanHasEightStepsAndWraps()
        {
            OpponentPlan plan = OpponentPlan.Authored();
            Assert.That(plan.Count, Is.EqualTo(8));
            OpponentStep first = plan.Current;
            for (int i = 0; i < 8; i++)
                plan.Advance();
            Assert.That(plan.Index, Is.Zero);
            Assert.That(plan.Current.ServeTarget, Is.EqualTo(first.ServeTarget));
        }

        [Test]
        public void AuthoredPlanMatchesTheSpecTable()
        {
            OpponentPlan plan = OpponentPlan.Authored();
            var attacks = new AttackKind[8];
            var blocks = new bool[8];
            var weak = new bool[8];
            for (int i = 0; i < 8; i++)
            {
                attacks[i] = plan.Current.Attack;
                blocks[i] = plan.Current.BlocksPlayerSmash;
                weak[i] = plan.Current.WeakReceive;
                Assert.That(CourtSpace.IsIn(plan.Current.ServeTarget), Is.True, $"step {i + 1}");
                Assert.That(plan.Current.ServeTarget.x, Is.LessThan(0f), $"step {i + 1}");
                plan.Advance();
            }

            Assert.That(attacks, Is.EqualTo(new[]
            {
                AttackKind.Smash, AttackKind.Tip, AttackKind.Lob, AttackKind.Smash,
                AttackKind.Lob, AttackKind.Smash, AttackKind.Tip, AttackKind.Lob
            }));
            Assert.That(blocks, Is.EqualTo(new[] { true, false, false, true, false, true, false, false }));
            Assert.That(weak, Is.EqualTo(new[] { false, false, false, false, true, false, false, false }));
        }

        [Test]
        public void AttackTargetsTheSidelineAwayFromThePlayer()
        {
            var step = new OpponentStep(new Vector2(-7f, 0f), AttackKind.Smash, -7f, 3f, false, false);
            Assert.That(OpponentPlan.AttackTarget(step, new Vector2(-5f, 1f)), Is.EqualTo(new Vector2(-7f, -3f)));
            Assert.That(OpponentPlan.AttackTarget(step, new Vector2(-5f, -1f)), Is.EqualTo(new Vector2(-7f, 3f)));
            Assert.That(OpponentPlan.AttackTarget(step, new Vector2(-5f, 0f)), Is.EqualTo(new Vector2(-7f, -3f)));
        }

        [Test]
        public void TwoAuthoredPlansAreIdentical()
        {
            OpponentPlan a = OpponentPlan.Authored();
            OpponentPlan b = OpponentPlan.Authored();
            for (int i = 0; i < 16; i++)
            {
                Assert.That(a.Current.ServeTarget, Is.EqualTo(b.Current.ServeTarget));
                Assert.That(a.Current.Attack, Is.EqualTo(b.Current.Attack));
                a.Advance();
                b.Advance();
            }
        }

        [Test]
        public void RejectsAnEmptyPlan()
        {
            Assert.Throws<ArgumentException>(() => new OpponentPlan(Array.Empty<OpponentStep>()));
            Assert.Throws<ArgumentException>(() => new OpponentPlan(null));
        }
    }
}
