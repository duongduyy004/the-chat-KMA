using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KMA.Gameplay
{
    [Serializable]
    public sealed class JourneyDialogueLine
    {
        [SerializeField] string speakerRole;
        [SerializeField, TextArea] string text;
        [SerializeField] Sprite portrait;

        public string SpeakerRole => speakerRole;
        public string Text => text;
        public Sprite Portrait => portrait;

        public JourneyDialogueLine(string speakerRole, string text, Sprite portrait = null)
        {
            this.speakerRole = speakerRole;
            this.text = text;
            this.portrait = portrait;
        }
    }

    [Serializable]
    public sealed class JourneyDialogueNode
    {
        [SerializeField] string id;
        [SerializeField] List<JourneyDialogueLine> lines = new List<JourneyDialogueLine>();

        public string Id => id;
        public IReadOnlyList<JourneyDialogueLine> Lines => lines;

        public JourneyDialogueNode(string id, List<JourneyDialogueLine> lines)
        {
            this.id = id;
            this.lines = lines ?? new List<JourneyDialogueLine>();
        }
    }

    [CreateAssetMenu(menuName = "KMA/Journey/Dialogue Library", fileName = "JourneyDialogues")]
    public sealed class JourneyDialogueLibrary : ScriptableObject
    {
        [SerializeField] List<JourneyDialogueNode> nodes = new List<JourneyDialogueNode>();
        Dictionary<string, JourneyDialogueNode> byId;

        public static JourneyDialogueLibrary LoadDefault() =>
            Resources.Load<JourneyDialogueLibrary>("Journey/JourneyDialogues");

        public JourneyDialogueNode Get(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) throw new ArgumentException("A dialogue node ID is required.", nameof(nodeId));
            byId ??= nodes.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Id))
                .GroupBy(x => x.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            if (!byId.TryGetValue(nodeId, out JourneyDialogueNode node))
                throw new KeyNotFoundException($"Journey dialogue '{nodeId}' was not found.");
            return node;
        }

        public bool Validate(out string error)
        {
            error = null;
            if (nodes == null || nodes.Count == 0) { error = "Dialogue library has no nodes."; return false; }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (JourneyDialogueNode node in nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id) || !seen.Add(node.Id))
                { error = "Dialogue IDs must be present and unique."; return false; }
                if (node.Lines == null || node.Lines.Count is < 2 or > 4 ||
                    node.Lines.Any(line => line == null || string.IsNullOrWhiteSpace(line.SpeakerRole) ||
                                           string.IsNullOrWhiteSpace(line.Text)))
                { error = $"Dialogue '{node.Id}' must contain two to four complete lines."; return false; }
            }
            return true;
        }
    }
}
