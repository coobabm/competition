using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Something in a chapter it did not get, to ask the owner about (「它没读懂的地方」, 2026-10-16): the question in its own
    /// voice, the words that make an answer right (<see cref="keys"/>) or half right (<see cref="half"/>), and what the book
    /// actually means (<see cref="truth"/>), which it tells the owner when the answer was off.
    /// </summary>
    public sealed class XgBookPuzzle
    {
        public string id = "";
        /// <summary>The chapter (0-based) it asks about; it only asks once that chapter is read.</summary>
        public int chapter;
        public string q = "", qEn = "";
        public string[] keys = new string[0], keysEn = new string[0];
        public string[] half = new string[0], halfEn = new string[0];
        public string truth = "", truthEn = "";
    }

    public static partial class XgBooks
    {
        static XgBookPuzzle P(int chapter, string q, string qEn, string[] keys, string[] keysEn, string truth, string truthEn, string[] half = null, string[] halfEn = null)
            => new XgBookPuzzle { chapter = chapter, q = q, qEn = qEn, keys = keys, keysEn = keysEn, truth = truth, truthEn = truthEn, half = half ?? new string[0], halfEn = halfEn ?? new string[0] };

        static readonly Dictionary<string, XgBookPuzzle[]> puzzles = Build(new Dictionary<string, XgBookPuzzle[]>
        {
            { "sanzijing", new[]
            {
                P(2, "孔融为什么把大的梨让给别人？他不饿？", "Why did Kong Rong give the big pear away? Wasn't he hungry?",
                    new[] { "让", "谦让", "礼貌", "尊敬", "敬", "懂事", "哥哥", "长幼", "礼" }, new[] { "polite", "respect", "elder", "brother", "manners", "humble" },
                    "小的让大的，是讲礼、敬兄长", "The younger gives way to the elder: manners and respect."),
                P(3, "头发绑在房梁上，锥子扎大腿。是在罚自己？", "Hair tied to the beam, an awl in the thigh. Is he punishing himself?",
                    new[] { "困", "瞌睡", "睡着", "提神", "清醒", "用功", "勤奋", "苦读" }, new[] { "sleep", "awake", "study", "hard work", "diligent" },
                    "怕读书睡着，用疼提神，是说勤奋", "So he wouldn't fall asleep reading: pain kept him awake. It's about hard work."),
            } },
            { "lunyu", new[]
            {
                P(0, "「学而时习之，不亦说乎」。复习有什么好高兴的？", "\"Learn and practise it often: is that not a joy?\" Why is revising a joy?",
                    new[] { "掌握", "学会", "懂", "熟", "进步", "收获", "明白" }, new[] { "master", "understand", "learn", "progress", "know it" },
                    "学了常温习，真会了，所以高兴", "Practise what you learnt and you really know it: that is the joy."),
                P(5, "「逝者如斯夫」。他站在河边，说的是谁？", "\"It passes like this.\" Standing by the river, who is he talking about?",
                    new[] { "时间", "光阴", "岁月", "日子", "流逝" }, new[] { "time", "years", "days" },
                    "说时间像河水，一去不回", "Time, flowing like the river and never coming back.",
                    new[] { "河", "水" }, new[] { "river", "water" }),
            } },
            { "tangshi", new[]
            {
                P(0, "看见月亮为什么会想家？月亮又不是家。", "Why does the moon make him think of home? The moon isn't home.",
                    new[] { "思乡", "想家", "同一个月亮", "团圆", "家人", "离家", "孤独" }, new[] { "homesick", "same moon", "family", "far from home", "lonely" },
                    "离家的人见明月，想起同样看着它的家乡", "Far from home, he sees the moon his home also sees."),
                P(7, "回自己家，小孩为什么问他从哪来？", "He comes home, so why do the children ask where he's from?",
                    new[] { "太久", "很久", "老了", "不认识", "几十年", "少小离家" }, new[] { "long", "old", "years", "don't know him", "didn't recognise" },
                    "离家几十年，人老了，孩子不认识他", "He'd been away for decades and grown old; the children didn't know him."),
            } },
            { "xiyouji", new[]
            {
                P(2, "一个筋斗十万八千里，为什么还在如来手心里？", "One somersault is 108,000 li. Why is he still in the Buddha's palm?",
                    new[] { "法力", "佛法", "手掌就是", "天外有天", "跳不出", "如来更" }, new[] { "power", "stronger", "palm is", "can't escape", "whole world" },
                    "如来的手掌就是天地，再能跑也跑不出去", "The Buddha's palm is the whole world; no running gets you out."),
                P(5, "猴子打死的是妖怪，师父为什么赶他走？", "The monkey killed a demon. Why did his master send him away?",
                    new[] { "看不出", "肉眼", "以为是人", "误会", "心软", "被骗", "善良" }, new[] { "couldn't see", "thought", "human", "fooled", "misunderstood", "kind" },
                    "唐僧肉眼凡胎，以为他打死的是好人", "Tang Seng's mortal eyes saw only innocent people killed."),
            } },
            { "shuihu", new[]
            {
                P(0, "鲁达只想教训郑屠，三拳打死了，为什么要跑？", "Lu Da only meant to teach Zheng a lesson. Three punches killed him. Why run?",
                    new[] { "杀人", "偿命", "犯法", "官府", "抓", "坐牢" }, new[] { "murder", "killed", "law", "arrest", "jail", "hang" },
                    "打死了人要偿命，只好逃", "Killing a man costs your life; he had to flee."),
                P(4, "杨志那么小心，酒是怎么让他们喝下去的？", "Yang Zhi was so careful. How did they get his men to drink?",
                    new[] { "先喝", "没毒", "蒙汗药", "放药", "下药", "瓢" }, new[] { "drank first", "no poison", "drug", "ladle" },
                    "先喝一桶证明没毒，再趁舀酒把药下进去", "They drank one bucket to show it was safe, then slipped the drug in with the ladle."),
            } },
            { "sanguo", new[]
            {
                P(2, "曹操说天下英雄只有他和刘备。刘备为什么吓得掉了筷子？", "Cao Cao says the only heroes are him and Liu Bei. Why does Liu Bei drop his chopsticks?",
                    new[] { "怕", "被看穿", "猜忌", "杀", "野心", "暴露", "装" }, new[] { "afraid", "see through", "kill", "ambition", "suspicious", "pretend" },
                    "怕曹操看出他的野心，要杀他", "He feared Cao Cao saw his ambition and would kill him.",
                    new[] { "雷" }, new[] { "thunder" }),
                P(5, "草船上没人射箭，回来为什么箭多了？", "Nobody on the straw boats shot anything. Why did they come back full of arrows?",
                    new[] { "曹军", "曹操", "对面射", "敌人", "雾", "草人", "稻草" }, new[] { "cao", "enemy", "fog", "straw" },
                    "大雾里曹军不敢出战，只往草船上放箭", "In the fog Cao's army dared not come out and just shot at the straw boats."),
            } },
            { "nahan", new[]
            {
                P(1, "孔乙己穿着长衫站着喝酒，大家为什么笑他？", "Kong Yiji drinks standing, in a long gown. Why does everyone laugh at him?",
                    new[] { "读书人", "面子", "穷", "落魄", "偷", "清高", "长衫" }, new[] { "scholar", "face", "poor", "steal", "pride", "gown" },
                    "穷得落魄，还死要读书人的面子", "Down and out, yet clinging to a scholar's pride."),
                P(2, "人血馒头真能治病？他们为什么信？", "Can a bun soaked in blood really cure him? Why do they believe it?",
                    new[] { "不能", "治不了", "迷信", "愚昧", "骗", "假的" }, new[] { "can't", "cannot", "doesn't", "superstition", "ignorance", "fooled" },
                    "不能。那是迷信和愚昧", "It can't. That was superstition and ignorance."),
            } },
            { "hongloumeng", new[]
            {
                P(0, "「假作真时真亦假」。到底哪个是真的？", "\"When false is taken for true, true becomes false.\" Which one is true?",
                    new[] { "分不清", "梦", "虚", "幻", "人生", "说不清" }, new[] { "can't tell", "dream", "illusion", "life" },
                    "真假本来难分，人生像一场梦", "True and false are hard to tell apart; life is like a dream."),
                P(4, "花落了，埋起来就好。她为什么要哭？", "The flowers fell; bury them and that's that. Why does she cry?",
                    new[] { "自己", "命运", "自怜", "身世", "青春", "像花" }, new[] { "herself", "fate", "her own", "youth", "like the flowers" },
                    "她看落花，想到自己的命也留不住", "In the fallen flowers she saw her own fate, which she couldn't keep either."),
                P(5, "贾政为什么下那么狠的手打宝玉？", "Why does Jia Zheng beat Baoyu so savagely?",
                    new[] { "不读书", "不上进", "金钏", "琪官", "戏子", "丢脸", "惹祸" }, new[] { "study", "lazy", "jinchuan", "actor", "shame", "trouble" },
                    "宝玉不读书，又惹出金钏和琪官的事", "Baoyu wouldn't study, and then came the trouble with Jinchuan and the actor Qiguan."),
            } },
        });

        static Dictionary<string, XgBookPuzzle[]> Build(Dictionary<string, XgBookPuzzle[]> map)
        {
            foreach (var kv in map)
                for (int i = 0; i < kv.Value.Length; i++) kv.Value[i].id = kv.Key + "." + i;
            return map;
        }

        /// <summary>The questions it may have about a book (empty for none).</summary>
        public static XgBookPuzzle[] Puzzles(string bookId) => bookId != null && puzzles.TryGetValue(bookId, out var list) ? list : new XgBookPuzzle[0];

        /// <summary>A question by id ("xiyouji.0"), and its book; null when unknown.</summary>
        public static XgBookPuzzle Puzzle(string id, out XgBook book)
        {
            book = null;
            if (string.IsNullOrEmpty(id)) return null;
            int dot = id.LastIndexOf('.');
            if (dot <= 0) return null;
            book = Get(id.Substring(0, dot));
            foreach (var p in Puzzles(id.Substring(0, dot))) if (p.id == id) return p;
            book = null;
            return null;
        }
    }
}
