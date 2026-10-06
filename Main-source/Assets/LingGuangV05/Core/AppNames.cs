namespace LingGuangV05.Core
{
    /// <summary>
    /// Placeholder names until the software name is decided (Documentation/LingGuang-Incremental/策划案v0.2落地对照.md, D3).
    /// Rename here only; user-facing code builds its strings from these constants.
    /// </summary>
    public static class AppNames
    {
        /// <summary>Short app name: desktop icon, window title, "in X".</summary>
        public const string AppZh = "灵光", AppEn = "LingGuang";
        /// <summary>The executable the player receives.</summary>
        public const string ExeZh = AppZh + ".exe", ExeEn = AppEn + ".exe";
        /// <summary>Game title for menus and credits.</summary>
        public const string GameZh = AppZh, GameEn = AppEn;
        /// <summary>The player's AI before the opening setup names it (design v0.2 §4 P9).</summary>
        public const string AiZh = AppZh, AiEn = AppEn;

        public static string App(bool english) { return english ? AppEn : AppZh; }
        public static string Exe(bool english) { return english ? ExeEn : ExeZh; }
        public static string Game(bool english) { return english ? GameEn : GameZh; }
        public static string Ai(bool english) { return english ? AiEn : AiZh; }
    }
}
