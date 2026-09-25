using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public enum AttackKind
    {
        Smash,
        Tip,
        Lob
    }

    public readonly struct OpponentStep
    {
        public readonly Vector2 ServeTarget;
        public readonly AttackKind Attack;
        public readonly float AttackDepth;
        public readonly float AttackLateral;
        public readonly bool WeakReceive;
        public readonly bool BlocksPlayerSmash;

        public OpponentStep(Vector2 serveTarget, AttackKind attack, float attackDepth, float attackLateral,
            bool weakReceive, bool blocksPlayerSmash)
        {
            ServeTarget = serveTarget;
            Attack = attack;
            AttackDepth = attackDepth;
            AttackLateral = attackLateral;
            WeakReceive = weakReceive;
            BlocksPlayerSmash = blocksPlayerSmash;
        }
    }

    // The AI's only source of variety: a fixed cycle that advances every time the AI sends the
    // ball over the net (serve or attack). Nothing here is random.
    public sealed class OpponentPlan
    {
        readonly OpponentStep[] steps;

        public OpponentPlan(IReadOnlyList<OpponentStep> steps)
        {
            if (steps == null || steps.Count == 0)
                throw new ArgumentException("An opponent plan needs at least one step.", nameof(steps));

            this.steps = new OpponentStep[steps.Count];
            for (int i = 0; i < steps.Count; i++)
                this.steps[i] = steps[i];
        }

        public int Count => steps.Length;
        public int Index { get; private set; }
        public OpponentStep Current => steps[Index];

        public void Advance() => Index = (Index + 1) % steps.Length;

        public static Vector2 AttackTarget(OpponentStep step, Vector2 playerPosition)
        {
            float away = playerPosition.y >= 0f ? -1f : 1f;
            return new Vector2(step.AttackDepth, away * step.AttackLateral);
        }

        public static OpponentPlan Authored() => new OpponentPlan(new[]
        {
            new OpponentStep(new Vector2(-7f, 0f), AttackKind.Smash, -7f, 3f, false, true),
            new OpponentStep(new Vector2(-3f, -2.5f), AttackKind.Tip, -1.5f, 2f, false, false),
            new OpponentStep(new Vector2(-7f, 3f), AttackKind.Lob, -7f, 3f, false, false),
            new OpponentStep(new Vector2(-7f, -3f), AttackKind.Smash, -4.5f, 3f, false, true),
            new OpponentStep(new Vector2(-5f, 0f), AttackKind.Lob, -5f, 0f, true, false),
            new OpponentStep(new Vector2(-3f, 2.5f), AttackKind.Smash, -6f, 2f, false, true),
            new OpponentStep(new Vector2(-7f, -3f), AttackKind.Tip, -1.2f, 0f, false, false),
            new OpponentStep(new Vector2(-5f, 0f), AttackKind.Lob, -7f, 0f, false, false)
        });
    }
}
