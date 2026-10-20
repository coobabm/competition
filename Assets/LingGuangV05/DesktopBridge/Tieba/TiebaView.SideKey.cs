using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core.Forum;

namespace LingGuangV05.Desktop.Tieba
{
    // E2 patch-5 (sidebar reuse). Public for the standalone editor check TiebaSideKeyChecks.
    public sealed partial class TiebaView
    {
        /// <summary>
        /// Everything DrawSide reads that can differ between two draws: language, highlighted board, and each board's
        /// id, name and English name in list order. Two lists with the same count but different names or order differ.
        /// </summary>
        public static string SideKey(bool english, string highlighted, IList<ForumBoard> boards)
        {
            var sb = new StringBuilder(english ? "en" : "zh");
            sb.Append('\u001e').Append(highlighted ?? "");
            if (boards != null)
                foreach (var b in boards)
                {
                    sb.Append('\u001e');
                    if (b == null) { sb.Append('\u0000'); continue; }
                    sb.Append(b.id).Append('\u001f').Append(b.name).Append('\u001f').Append(b.nameEn);
                }
            return sb.ToString();
        }
    }
}
