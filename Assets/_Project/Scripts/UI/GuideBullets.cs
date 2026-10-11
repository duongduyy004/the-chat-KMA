using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace KMA.Gameplay.UI
{
    /// Turns a guide page's prose into a bullet list: one bullet per sentence, hanging-indented so
    /// wrapped lines align under the text rather than under the dot.
    public static class GuideBullets
    {
        public const float Hang = 52f;
        const string Bullet = "•";
        static readonly Regex SentenceBreak = new Regex(@"(?<=[.!?])\s+(?=[\p{Lu}\d])", RegexOptions.Compiled);

        public static IReadOnlyList<string> Split(string text)
        {
            var items = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return items;
            foreach (string part in SentenceBreak.Split(text.Replace("\r", string.Empty)))
            {
                string item = part.Trim();
                if (item.Length > 0)
                    items.Add(item);
            }
            return items;
        }

        /// TextMeshPro rich text; the caller still runs VietText.Fix over the result.
        public static string Format(string text)
        {
            IReadOnlyList<string> items = Split(text);
            if (items.Count == 0)
                return string.Empty;
            var sb = new StringBuilder();
            sb.Append("<indent=").Append(Hang).Append("px><line-indent=-").Append(Hang).Append("px>");
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0)
                    sb.Append('\n');
                sb.Append(Bullet).Append("<pos=").Append(Hang).Append("px>").Append(items[i]);
            }
            return sb.ToString();
        }
    }
}
