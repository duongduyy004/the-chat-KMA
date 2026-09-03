using UnityEngine;

namespace KMA.Gameplay
{
    /// <summary>
    /// Authored name/lane/profile data for a single MG_Sprint cosmetic rival.
    /// Shared source of truth for both the Editor scene configurator
    /// (KMA.EditorTools.SprintSceneConfigurator) and PlayMode tests
    /// (KMA.Tests.Gameplay.Running.SprintControllerTests) so the two never
    /// drift independently.
    /// </summary>
    public readonly struct SprintRivalMapping
    {
        public SprintRivalMapping(string name, int lane, int rivalIndex, float localY, string profilePath)
        {
            Name = name;
            Lane = lane;
            RivalIndex = rivalIndex;
            LocalPosition = new Vector3(-9.6f, localY, 0f);
            ProfilePath = profilePath;
        }

        public string Name { get; }
        public int Lane { get; }
        public int RivalIndex { get; }
        public Vector3 LocalPosition { get; }
        public string ProfilePath { get; }
    }

    public static class SprintRivalMappings
    {
        public static readonly SprintRivalMapping[] Required =
        {
            new SprintRivalMapping("Runner_01", 1, 0, 2.1f,
                "Assets/_Project/ScriptableObjects/Sprint/RivalPaceProfile_Lane1.asset"),
            new SprintRivalMapping("Runner_03", 3, 1, -.7f,
                "Assets/_Project/ScriptableObjects/Sprint/RivalPaceProfile_Lane3.asset"),
            new SprintRivalMapping("Runner_04", 4, 2, -2.1f,
                "Assets/_Project/ScriptableObjects/Sprint/RivalPaceProfile_Lane4.asset")
        };
    }
}
