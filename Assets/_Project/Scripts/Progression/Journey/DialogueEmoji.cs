using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KMA.Gameplay
{
    /// Emoji shortcodes (":sob:") used in journey dialogue, expanded to TMP sprite tags.
    public static class DialogueEmoji
    {
        static readonly string[] Names =
        {
            "sob", "skull", "sunglasses", "fire", "scream", "runner", "dash", "soccer", "volleyball",
            "eyes", "clown", "salute", "100", "tada", "muscle"
        };
        static readonly HashSet<string> Known = new HashSet<string>(Names, StringComparer.Ordinal);
        static readonly Regex Code = new Regex(":([a-z0-9_]+):", RegexOptions.CultureInvariant);

        public static IReadOnlyList<string> KnownNames => Names;

        public static bool IsKnown(string name) => name != null && Known.Contains(name);

        public static IEnumerable<string> FindCodes(string text)
        {
            if (string.IsNullOrEmpty(text)) yield break;
            foreach (Match match in Code.Matches(text))
                yield return match.Groups[1].Value;
        }

        public static string Expand(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return Code.Replace(text, match =>
            {
                string name = match.Groups[1].Value;
                return Known.Contains(name) ? $"<sprite name=\"{name}\">" : match.Value;
            });
        }
    }
}
