using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Girlfriend
{
    /// <summary>
    /// The deterministic half of 林晴雯 (design §2–§4.6): tiers, mood, neglect, memories, promises, holidays, red
    /// packets and gifts. Pure C#; the YY layer calls these and the local model only writes her words.
    /// This file holds the stable public surface other systems read (the 「她爱我吗」 cutscene).
    /// </summary>
    public static partial class GirlfriendRules
    {
        public const int StartAffection = 62;
        /// <summary>A cold signal counts as recent for 48 hours (design §6.1).</summary>
        public const double ColdSignalWindowHours = 48;

        // Flags (GirlfriendState.flags).
        public const string FlagFought = "fought";          // a first quarrel happened
        public const string FlagMadeUp = "madeUp";          // and they made up
        public const string FlagKnowsAi = "knowsAi";        // she knows about the AI (told, or admitted when caught)
        public const string FlagCaughtAi = "caughtAi";      // she caught an AI-written reply
        public const string FlagDeniedAi = "deniedAi";      // he denied it when caught
        public const string FlagAiRefused = "aiRefused";    // stage 6: the AI will not answer her any more
        public const string FlagShook = "shook";            // she shook his window
        public const string FlagMoneyAsked = "moneyAsked";  // 「你哪来的钱」 was asked
        public const string FlagMoneyAnswered = "moneyAnswered";
        public const string FlagIphone = "iphone";          // the iPhone 7 counted (once)
        public const string FlagNewYear = "newYear";        // the 12-31 23:59 message was sent

        /// <summary>The band an affection value falls in: 冷 0–29, 淡 30–49, 平 50–69, 暖 70–84, 甜 85–100.</summary>
        public static GirlfriendTier Tier(int affection)
        {
            if (affection < 30) return GirlfriendTier.Cold;
            if (affection < 50) return GirlfriendTier.Distant;
            if (affection < 70) return GirlfriendTier.Normal;
            if (affection < 85) return GirlfriendTier.Warm;
            return GirlfriendTier.Sweet;
        }

        public static GirlfriendTier Tier(GirlfriendState s) => Tier(s == null ? StartAffection : s.affection);

        /// <summary>The verdict of 「她爱我吗」 (design §6.4): affection 50 or more.</summary>
        public static bool Loves(GirlfriendState s) => s != null && s.affection >= 50;

        /// <summary>Within 5 of the line: the bulb flickers twice more before it stops (design §6.4).</summary>
        public static bool Hesitant(GirlfriendState s) => s != null && Math.Abs(s.affection - 50) < 5;

        /// <summary>
        /// A cold signal (a bare 「哦 / 嗯 / 随便」, typing twice then one character, a sad signature) in the last 48
        /// hours. The hours are story hours: both the play time since the signal and the calendar days since it count,
        /// so a signal two calendar days old is stale even if it was a minute of play ago.
        /// </summary>
        public static bool RecentColdSignal(GirlfriendState s, double nowGameSeconds)
        {
            if (s == null || s.lastColdSignalAt <= 0 || s.lastColdSignalDay < 0) return false;
            if (nowGameSeconds - s.lastColdSignalAt > ColdSignalWindowHours * 3600) return false;
            return s.today < 0 || s.today - s.lastColdSignalDay <= 2;
        }

        public static bool Has(GirlfriendState s, string flag) => s != null && s.flags != null && s.flags.Contains(flag);

        /// <summary>Sets a flag; true if it was not set before (a "first time").</summary>
        public static bool Mark(GirlfriendState s, string flag)
        {
            if (s == null || string.IsNullOrEmpty(flag)) return false;
            if (s.flags == null) s.flags = new List<string>();
            if (s.flags.Contains(flag)) return false;
            s.flags.Add(flag);
            return true;
        }
    }
}
