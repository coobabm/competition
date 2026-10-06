using System;

namespace LingGuangV05.Core
{
    /// <summary>
    /// Chinese is the source text; English lives apart, in the table of <c>Lang.En.cs</c>. Code writes only the
    /// Chinese: <c>Lang.T("训练一轮")</c> (or <c>T("…")</c> inside the lab simulation). With English on, the table is
    /// looked up and a line it does not have stays in Chinese. New text needs no English for now.
    /// </summary>
    public static partial class Lang
    {
        /// <summary>Whether the player has switched to English (bound by the runtime's GameText). Null = Chinese.</summary>
        public static Func<bool> IsEnglish;

        public static bool English => IsEnglish != null && IsEnglish();

        /// <summary>The line in the player's language.</summary>
        public static string T(string zh) => English ? En(zh) : zh;

        /// <summary>The English of a Chinese line from the table, or the Chinese itself when it has none.</summary>
        public static string En(string zh) => zh != null && Table.TryGetValue(zh, out var en) ? en : zh;

        /// <summary>Whether the table has an English line for this Chinese.</summary>
        public static bool HasEnglish(string zh) => zh != null && Table.ContainsKey(zh);
    }
}
