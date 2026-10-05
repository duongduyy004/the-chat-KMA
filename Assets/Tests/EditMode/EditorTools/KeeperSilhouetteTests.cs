#if UNITY_EDITOR
using System.IO;
using KMA.EditorTools;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class KeeperSilhouetteTests
    {
        [Test]
        public void KeeperCapsulesTraceTheDrawnKeeperPose()
        {
            var texture = new Texture2D(2, 2);
            string path = CharacterArt.PosePath(FootballSceneConfigurator.KeeperCharacter, FootballSceneConfigurator.KeeperReadyPose);
            Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
            float scale = FootballPresentation.KeeperDisplayHeight / texture.height; // preview px per texture px
            Assert.That(FootballPresentation.KeeperDisplayWidth / texture.width, Is.EqualTo(scale).Within(1e-4f),
                "The keeper must be drawn without stretching.");

            int opaque = 0, opaqueCovered = 0, covered = 0, coveredOpaque = 0;
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                // Capsule space: origin at the hip, y grows downward. Texture rows grow upward from the feet.
                var point = new Vector2((x + .5f - texture.width * .5f) * scale,
                    FootballPresentation.KeeperFeetDrop - (y + .5f) * scale);
                bool solid = texture.GetPixel(x, y).a > .5f;
                bool inside = InsideCapsules(point);
                if (solid) { opaque++; if (inside) opaqueCovered++; }
                if (inside) { covered++; if (solid) coveredOpaque++; }
            }
            Object.DestroyImmediate(texture);

            float recall = (float)opaqueCovered / opaque;
            float precision = (float)coveredOpaque / covered;
            Assert.That(recall, Is.GreaterThanOrEqualTo(.85f), "Share of the drawn keeper that can save the ball.");
            Assert.That(precision, Is.GreaterThanOrEqualTo(.85f), "Share of the save area that is drawn keeper.");
        }

        static bool InsideCapsules(Vector2 point)
        {
            for (int i = 0; i < FootballFlightSimulation.KeeperCapsuleCount; i++)
            {
                FootballFlightSimulation.GetKeeperCapsule(i, out Vector2 a, out Vector2 b, out float radius);
                Vector2 delta = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, delta) / Mathf.Max(delta.sqrMagnitude, .000001f));
                if (Vector2.Distance(point, a + delta * t) <= radius) return true;
            }
            return false;
        }
    }
}
#endif
