using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Girlfriend
{
    /// <summary>The five affection bands (design §2): 冷 / 淡 / 平 / 暖 / 甜.</summary>
    public enum GirlfriendTier { Cold, Distant, Normal, Warm, Sweet }

    /// <summary>A date she and the protagonist agreed on (七夕 video call, 《你的名字。》 on 12-2).</summary>
    [Serializable]
    public sealed class GfPromise
    {
        public string key = "";
        public string zh = "", en = "";
        /// <summary>Calendar day index (GameCalendar.DayIndex) it falls due.</summary>
        public int dueDay;
        /// <summary>0 open, 1 kept, 2 broken.</summary>
        public int state;
    }

    /// <summary>A gift on its way from 淘货 (design §4.6): ordered, then 已签收 two or three calendar days later.</summary>
    [Serializable]
    public sealed class GfGiftOrder
    {
        public string gift = "";
        public double price;
        public int orderedDay;
        /// <summary>Calendar day it arrives; for milk tea the same day (see <see cref="arriveAt"/>).</summary>
        public int arriveDay;
        /// <summary>Game seconds the same-day milk tea arrives (0 for parcels).</summary>
        public double arriveAt;
        /// <summary>0 on the way, 1 signed for, 2 refused (refunded).</summary>
        public int state;
        /// <summary>Affection it gave when it arrived, and whether it matched something she said.</summary>
        public int effect;
        public bool matched;
    }

    /// <summary>
    /// 林晴雯's hidden state (design §2), saved inside the YY chat state. The player never sees a number; the tier
    /// text, her timing, her words and her signature are the only signals. Times are kept two ways: game seconds
    /// (play time, what chat lines are stamped with) and story seconds (seconds of the desktop clock since
    /// 2016-05-24 01:47, which include the calendar's day jumps, so "8 hours without a reply" can pass in a minute
    /// of play when the calendar moves on).
    /// </summary>
    [Serializable]
    public sealed class GirlfriendState
    {
        public int version = 1;
        /// <summary>She has appeared in YY (the prologue is over and her first 「还没睡？」 was sent).</summary>
        public bool started;
        /// <summary>Seed of her deterministic dice, fixed when she first appears.</summary>
        public int seed;
        /// <summary>How many dice she has rolled so far (the RNG position, so a reload continues the same sequence).</summary>
        public int rolls;

        public int affection = GirlfriendRules.StartAffection;
        /// <summary>−3…+3; one step back towards 0 every 6 story hours.</summary>
        public int mood;
        /// <summary>Fractional affection carried between mood-scaled model deltas.</summary>
        public double affectionCarry;
        public double moodStepAt;

        /// <summary>Her last message (story seconds and game seconds) and whether the protagonist has answered since.</summary>
        public double herLastAt, herLastGame;
        public bool awaitingReply;
        /// <summary>How far the current unanswered message has been punished: 0 none, 1 mood −1 (2 h), 2 affection −3 (8 h).</summary>
        public int neglectLevel;
        /// <summary>Unanswered messages in a row from her (she stops double-texting after two).</summary>
        public int unansweredRun;
        /// <summary>The protagonist's last message in her chat (story and game seconds).</summary>
        public double meLastAt, meLastGame;
        /// <summary>Last time either side said something (story seconds).</summary>
        public double lastAnyAt;
        /// <summary>Topics the protagonist opened today (calendar day <see cref="openersDay"/>), at most 3 count.</summary>
        public int openersDay = -1, openersToday;
        /// <summary>Long silences already punished (story seconds of the last drift step).</summary>
        public double driftAt;

        /// <summary>Things she said or he promised, newest last (at most 24). Plain readable text.</summary>
        public List<string> memories = new List<string>();
        /// <summary>The keyword each memory is recognised by (same index as <see cref="memories"/>).</summary>
        public List<string> memoryKeys = new List<string>();
        /// <summary>Memory keys the protagonist already brought up (each pays once).</summary>
        public List<string> recalled = new List<string>();
        public List<GfPromise> promises = new List<GfPromise>();
        /// <summary>Events that happened: see the GirlfriendRules.Flag* constants.</summary>
        public List<string> flags = new List<string>();

        /// <summary>Game seconds of the last cold signal (a bare 「哦」, a typing-then-「嗯」, a sad signature).</summary>
        public double lastColdSignalAt;
        /// <summary>Calendar day of the last cold signal.</summary>
        public int lastColdSignalDay = -1;
        /// <summary>Her YY signature now (zh, en) and the library key it came from.</summary>
        public string signature = "", signatureEn = "", signatureKey = "";
        public double signatureAt;

        /// <summary>A quarrel is going on (red packets bounce, AI replies are noticed three times as often).</summary>
        public bool fighting;
        public double fightSince;

        /// <summary>Calendar day the state last saw (its own mirror of GameCalendar.CurrentDay).</summary>
        public int today = -1;
        /// <summary>Calendar day she went to sleep on (-1 awake); she wakes when the date changes.</summary>
        public int asleepDay = -1;
        /// <summary>Game seconds she has been around on <see cref="today"/> (her evening budget).</summary>
        public double onlineToday;
        /// <summary>She nagged him to sleep tonight, and how many lines he sent after that.</summary>
        public int naggedDay = -1, linesAfterNag;

        /// <summary>Proactive messages: today's count and the earliest game second the next may come.</summary>
        public int proactiveDay = -1, proactiveToday;
        public double nextProactiveGame;
        /// <summary>Scripted life lines (考试, 煤球, 口红…) and holiday steps already sent, by key.</summary>
        public List<string> done = new List<string>();

        /// <summary>Red packets sent today, and how many were over her limit so far.</summary>
        public int packetsDay = -1, packetsToday, bigPackets;
        public List<GfGiftOrder> orders = new List<GfGiftOrder>();

        /// <summary>Lines the AI wrote for him, times she caught it, and whether he denied it last time.</summary>
        public int aiReplies, aiCaught;
        /// <summary>Her total chat lines and his (the YY history itself is trimmed), and the first line's game seconds.</summary>
        public int totalMessages, herMessages;
        public double firstMessageAt = -1;
        public int firstMessageDay = -1;
        /// <summary>A question of hers waiting for his answer: "qixi", "movie" (a promise), "money", "caught".</summary>
        public string asking = "";
        public int askDay = -1;
        /// <summary>Play time at the last tick, and play time the calendar has spent on <see cref="today"/>.</summary>
        public double lastTickGame = -1, dayGame;
        /// <summary>His lines to her today (calendar day <see cref="linesDay"/>).</summary>
        public int linesDay = -1, linesToday;
        /// <summary>Calendar day the current quarrel started.</summary>
        public int fightDay = -1;
        /// <summary>Her last message he was shaken for (game seconds), so one silence gets one shake.</summary>
        public double shookFor = -1;
        /// <summary>A sulking signature after a forgotten day, until this calendar day.</summary>
        public int sulkUntil = -1;
        public string sulkZh = "", sulkEn = "";
        /// <summary>「让 灵光 代我回」 is switched on in her chat (stage 5; the AI turns it off for good at stage 6).</summary>
        public bool aiAuto;
        /// <summary>Her last lines, from the model or the library (GirlfriendLines.RecentLimit), so the fallback library does not repeat itself.</summary>
        public List<string> recentLines = new List<string>();
    }
}
