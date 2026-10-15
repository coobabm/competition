namespace LingGuangV05.Core
{
    /// <summary>
    /// The fixed game title and the AI identity before the player names it.
    /// PersonalName/PersonalExe are for display; internal application and save IDs remain stable.
    /// </summary>
    public static class AppNames
    {
        /// <summary>Short app name: desktop icon, window title, "in X".</summary>
        public const string AppZh = "灵光", AppEn = "LingGuang";
        /// <summary>The executable the player receives.</summary>
        public const string ExeZh = AppZh + ".exe", ExeEn = AppEn + ".exe";
        /// <summary>Game title for menus and credits.</summary>
        public const string GameZh = "我的电脑活了", GameEn = "My Computer Is Alive";
        /// <summary>The player's AI before the opening setup names it (design v0.2 §4 P9).</summary>
        public const string AiZh = AppZh, AiEn = AppEn;

        /// <summary>The 2016 shops (design v1.1 §3): 淘货 sells new cards and data, 喵鱼 is the second-hand market.</summary>
        public const string ShopZh = "淘货", ShopEn = "Taohuo", UsedZh = "喵鱼", UsedEn = "Miaoyu";

        public static string App(bool english) { return english ? AppEn : AppZh; }
        public static string Exe(bool english) { return english ? ExeEn : ExeZh; }
        public static string Game(bool english) { return english ? GameEn : GameZh; }
        public static string Ai(bool english) { return english ? AiEn : AiZh; }
        public static string PersonalName(string name, bool english) => AiNamePolicy.Display(name, english);
        public static string PersonalExe(string name, bool english) => PersonalName(name, english) + ".exe";
    }
}
