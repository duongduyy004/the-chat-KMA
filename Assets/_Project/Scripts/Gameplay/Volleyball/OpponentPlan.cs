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
        // The AI runs to the ball but whiffs its first touch, so the ball drops for a point.
        public readonly bool MissesReceive;

        public OpponentStep(Vector2 serveTarget, AttackKind attack, float attackDepth, float attackLateral,
            bool weakReceive, bool blocksPlayerSmash, bool missesReceive = false)
        {
            ServeTarget = serveTarget;
            Attack = attack;
            AttackDepth = attackDepth;
            AttackLateral = attackLateral;
            WeakReceive = weakReceive;
            BlocksPlayerSmash = blocksPlayerSmash;
            MissesReceive = missesReceive;
        }
    }

    // The AI's only source of variety: a fixed cycle that advances every time the AI sends the
    // ball over the net (serve or attack) or whiffs a receive. Nothing here is random.
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

        // Tuned to be beatable: serves land near the player's ready spot, attacks stay inside the
        // sidelines, three steps whiff the receive outright, one fumbles it into a free ball and
        // only one step blocks.
        public static OpponentPlan Authored() => new OpponentPlan(new[]
        {
            new OpponentStep(new Vector2(-5f, 0f), AttackKind.Smash, -6f, 2f, false, false),
            new OpponentStep(new Vector2(-4.5f, -1.5f), AttackKind.Smash, -5f, 2f, false, false),
            new OpponentStep(new Vector2(-5.5f, 1.5f), AttackKind.Lob, -6f, 2f, false, false, true),
            new OpponentStep(new Vector2(-4.5f, 1.5f), AttackKind.Smash, -5f, 2f, false, true),
            new OpponentStep(new Vector2(-5f, 0f), AttackKind.Lob, -5f, 1f, false, false, true),
            new OpponentStep(new Vector2(-5.5f, -1.5f), AttackKind.Lob, -5f, 1.5f, true, false),
            new OpponentStep(new Vector2(-5f, 0f), AttackKind.Tip, -2f, 0f, false, false),
            new OpponentStep(new Vector2(-5f, 1f), AttackKind.Lob, -6f, 0f, false, false, true)
        });
    }
}
