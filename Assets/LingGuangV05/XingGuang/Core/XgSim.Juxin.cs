using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;

namespace LingGuangV05.XingGuang
{
    public enum XgJxKind { Text = 0, Article = 1, RedPacket = 2, Notice = 3 }

    /// <summary>One 巨信 message. Authored lines keep both languages; typed and model lines have the same text in both.</summary>
    [Serializable]
    public sealed class XgJxMessage
    {
        public bool mine;
        /// <summary>Speaker inside a group chat ("" elsewhere).</summary>
        public string whoZh = "", whoEn = "";
        public string zh = "", en = "";
        /// <summary>Article and red-packet headline.</summary>
        public string titleZh = "", titleEn = "";
        public int kind;
        /// <summary>Game date (yyyymmdd) and minute of the day it arrived, for the bubble's time stamp.</summary>
        public int date, minute;
        /// <summary>巨信 clock (seconds) when it arrived.</summary>
        public double at;
        /// <summary>Sent by the lab's AI on the player's behalf (由灵光代回), and whether it put its foot in it.</summary>
        public bool ai, gaffe;
        /// <summary>Red packet: what is inside, whether it was opened and what the player got (0 = 手慢了).</summary>
        public double amount, got;
        public bool opened;
    }

    /// <summary>A 巨信 conversation: an official account, a client's contact person, a friend or a group.</summary>
    [Serializable]
    public sealed class XgJxThread
    {
        public string id = "";
        public int unread;
        /// <summary>巨信 clock when a message started waiting for an answer, or -1.</summary>
        public double waitingSince = -1;
        /// <summary>"在吗" follow-ups sent during the current wait.</summary>
        public int nudges;
        /// <summary>Satisfaction of a client or friend: answered on time +1, ignored or a gaffe −1 (−3…+3, then it acts).</summary>
        public int mood;
        public double last;
        public List<XgJxMessage> messages = new List<XgJxMessage>();
    }

    public sealed partial class XgState
    {
        /// <summary>巨信 (design v1.1 §3): 0 = not opened yet. Older saves load with it closed and open it at stage 4.</summary>
        public int jxVersion;
        public bool jxAutoReply;
        public double jxClock, jxNextChat, jxNextFamily;
        public int jxWeek;
        public long jxRng;
        public int jxAiReplies, jxGaffes, jxRedPackets, jxBonuses;
        public double jxRedPacketTotal, jxBonusTotal;
        public List<XgJxThread> jxThreads = new List<XgJxThread>();
        /// <summary>One-off events already posted (signed contracts, invitations, news, friends' lines, settlements).</summary>
        public List<string> jxSeen = new List<string>();
        /// <summary>朋友圈 posts the player liked.</summary>
        public List<string> jxLikes = new List<string>();
        /// <summary>Contract income since the client's last weekly settlement notice.</summary>
        public List<XgLabelCount> jxEarned = new List<XgLabelCount>();
    }

    /// <summary>A client with a 巨信 official account and a contact person.</summary>
    public sealed class XgJxClient
    {
        /// <summary>The contract or SLA offer id.</summary>
        public string id, oaZh, oaEn, personZh, personEn;
        public bool sla;
    }

    /// <summary>A 朋友圈 post: shown from its date on.</summary>
    public sealed class XgJxMoment
    {
        public string id, who, zh, en;
        public int date;
    }

    /// <summary>What the desktop needs to have the lab's AI answer one waiting message (让 AI 代我回).</summary>
    public sealed class XgJxReplyJob
    {
        public string thread = "", incoming = "", system = "";
        public bool gaffe;
        public int tokens;
        public float temperature;
    }

    /// <summary>
    /// 巨信, the 2016 WeChat parody (design v1.1 §3, §7 stages 4–5, §12). It opens at stage 4 with the official
    /// 「小程序内测」 push. Every contract client has an official account that posts 2016-style articles (签约喜报,
    /// 招募, 周结算, 质量通报, 「点击阅读原文」「长按识别二维码」) and a contact person who asks for deliveries and complains
    /// when a new meme makes the checkpoint slip. 摆渡新闻 pushes the month's news by the calendar; 阿杰, 小刚 and 表姐
    /// chat, the family group forwards 养生 articles and hands out 红包, and friends post to 朋友圈. 老周 is not here:
    /// he never writes first and never joins a group (§9).
    ///
    /// From stage 5 the player can switch on 让 AI 代我回: the lab's AI answers waiting messages through the same
    /// persona prompt as the 对话 page (the desktop runs the model, <see cref="TakeAutoReply"/> /
    /// <see cref="CompleteAutoReply"/>), and now and then it gets something socially wrong. Answering on time raises a
    /// contact's mood, ignoring or a gaffe lowers it; at +3 a client sends a small 辛苦费 red packet (and an SLA client
    /// adds a point of platform credit), at −3 an SLA client takes one away. Everything here is small next to the
    /// contracts' own income. Pure rules; the desktop ticks it with <see cref="JuxinTick"/>.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int JuxinStage = 4, JuxinAutoStage = 5;
        public const int JuxinMessageLimit = 40;
        /// <summary>Seconds an answer still counts as on time, and before an ignored contact asks again.</summary>
        public const double JuxinAnswerWindow = 300, JuxinIgnoreSeconds = 420;
        /// <summary>Seconds a waiting message sits before the AI answers it (so the player sees it arrive first).</summary>
        public const double JuxinAutoDelay = 5;
        public const double JuxinRedPacketLife = 300;
        public const int JuxinMoodLimit = 3;
        /// <summary>A client's 辛苦费: this share of its contract's signing bonus, between ¥5 and ¥150.</summary>
        public const double JuxinBonusShare = .03, JuxinBonusMin = 5, JuxinBonusMax = 150, JuxinSlaBonus = 20;

        public const string JxTeam = "jx.team", JxNews = "jx.news", JxFamily = "g.family", JxAjie = "f.ajie", JxXiaogang = "f.xiaogang", JxCousin = "f.cousin";

        /// <summary>Tests pin the gaffe roll (0 never, 1 always); NaN uses the persona.</summary>
        public double JuxinGaffeOverride = double.NaN;
        /// <summary>The conversation open on screen: its new messages are not unread.</summary>
        public string JuxinShowing;
        /// <summary>A 巨信 message arrived (the desktop pings and lights the badge).</summary>
        public event Action<XgJxThread, XgJxMessage> JuxinReceived;

        readonly HashSet<string> jxInFlight = new HashSet<string>();
        readonly List<(double at, string thread, XgJxMessage message, bool waits)> jxQueue = new List<(double, string, XgJxMessage, bool)>();
        bool jxHooked, jxRepaired;

        public static readonly XgJxClient[] JuxinClients =
        {
            JxC("cheque", "城郊信用社", "Suburban Credit Union", "信用社 · 王科长", "Section chief Wang (credit union)"),
            JxC("zipcode", "区邮政局", "District Post Office", "邮政局 · 刘师傅", "Master Liu (post office)"),
            JxC("memetag", "斗图网", "Meme Battle Net", "斗图网运营 · 小鹿", "Xiaolu, Meme Battle Net ops"),
            JxC("captcha", "某购票网站", "A Ticketing Site", "购票网站 · 技术部老马", "Old Ma, ticketing site tech"),
            JxC("goclub", "县围棋协会", "County Go Club", "围棋协会 · 陈老师", "Teacher Chen (Go club)"),
            JxC("parking", "幸福里物业", "Happy Lane Property", "物业 · 赵经理", "Manager Zhao (property)"),
            JxC("taobao", "淘宝卖家联盟", "Taobao Seller Alliance", "卖家联盟 · 阿芳", "Afang, seller alliance"),
            JxC("faceclock", "工业园区管委会", "Industrial Park Office", "园区人事 · 黄姐", "Sister Huang, park HR"),
            JxC("acrostic", "每日一诗", "Daily Poem", "每日一诗 · 小编", "Daily Poem editor"),
            JxC("homework", "家教网", "Tutor Net", "家教网 · 孙老师", "Teacher Sun (Tutor Net)"),
            JxC("civilexam", "考公培训班", "Civil Exam Prep", "培训班 · 李教务", "Registrar Li (exam prep)"),
            JxC("danmaku", "某弹幕网站", "A Danmaku Site", "弹幕站审核 · 阿呆", "Adai, danmaku moderation"),
            JxC("antifraud", "市反诈中心", "City Anti-Fraud Centre", "反诈中心 · 张干事", "Officer Zhang (anti-fraud)"),
            JxC("clickbait", "资讯频道", "News Feed Channel", "资讯频道编辑 · Kevin", "Kevin, news feed editor"),
            JxC("fakereview", "电商风控", "Marketplace Risk", "风控部 · 老韩", "Old Han, risk team"),
            JxC("ime", "某输入法", "A Pinyin IME", "输入法产品经理 · Tina", "Tina, IME product manager"),
            JxC("support", "客服外包", "Support Outsourcing", "外包公司 · 刘总", "Boss Liu (outsourcing)"),
            JxC("crossborder", "表姐的微商小店", "Cousin's Online Shop", "表姐", "Cousin"),
            JxC("subtitle", "字幕组", "Fansub Group", "字幕组校对 · 小林", "Xiaolin, fansub proofreader"),
            JxC("sla.danmu", "某视频网站", "A Video Site", "弹幕组 · 组长", "Danmaku team lead", true),
            JxC("sla.meme", "某表情包 App", "A Sticker App", "表情包 App · 运营", "Sticker app ops", true),
            JxC("sla.takeout", "某外卖平台", "A Food-Delivery App", "外卖平台 · 品控", "Delivery app QA", true),
            JxC("sla.portal", "某门户客户端", "A Portal News App", "门户客户端 · 值班编辑", "Portal duty editor", true),
            JxC("sla.bank", "某银行信用卡中心", "A Bank Card Centre", "银行反欺诈 · 周主管", "Supervisor Zhou (bank)", true),
            JxC("sla.homework", "某拍照搜题 App", "A Homework-Search App", "搜题 App · 教研", "Homework app tutor team", true),
            JxC("sla.courier", "某快递公司", "A Courier Company", "快递公司 · 网点主管", "Courier branch manager", true),
        };

        static XgJxClient JxC(string id, string oaZh, string oaEn, string personZh, string personEn, bool sla = false) =>
            new XgJxClient { id = id, oaZh = oaZh, oaEn = oaEn, personZh = personZh, personEn = personEn, sla = sla };

        public static XgJxClient JuxinClient(string id) { foreach (var c in JuxinClients) if (c.id == id) return c; return null; }

        /// <summary>The contact person's thread. 表姐's shop talks through 表姐 herself.</summary>
        public static string JuxinDm(string clientId) => clientId == "crossborder" ? JxCousin : "dm." + clientId;
        public static string JuxinOa(string clientId) => "oa." + clientId;
        public static bool JuxinIsSubscription(string thread) => thread != null && (thread.StartsWith("oa.", StringComparison.Ordinal) || thread == JxNews || thread == JxTeam);
        public static bool JuxinIsGroup(string thread) => thread != null && thread.StartsWith("g.", StringComparison.Ordinal);

        static XgJxClient ClientOfThread(string thread)
        {
            if (thread == null) return null;
            if (thread.StartsWith("dm.", StringComparison.Ordinal)) return JuxinClient(thread.Substring(3));
            if (thread.StartsWith("oa.", StringComparison.Ordinal)) return JuxinClient(thread.Substring(3));
            return null;
        }

        /// <summary>A thread's display name.</summary>
        public static string JuxinName(string thread, bool english)
        {
            switch (thread)
            {
                case JxTeam: return english ? "Juxin Team" : "巨信团队";
                case JxNews: return english ? "Bodu News" : "摆渡新闻";
                case JxFamily: return english ? "One Big Loving Family" : "相亲相爱一家人";
                case JxAjie: return english ? "Ajie" : "阿杰";
                case JxXiaogang: return english ? "Xiaogang" : "小刚";
                case JxCousin: return english ? "Cousin" : "表姐";
            }
            var c = ClientOfThread(thread);
            if (c == null) return thread ?? "";
            return thread.StartsWith("oa.", StringComparison.Ordinal) ? (english ? c.oaEn : c.oaZh) : (english ? c.personEn : c.personZh);
        }

        // ───────────── state ─────────────

        public bool JuxinUnlocked => S.stage >= JuxinStage;
        /// <summary>让 AI 代我回 can be switched on from stage 5 (§7 stage 5: it can answer 巨信 for you).</summary>
        public bool JuxinCanAutoReply => S.stage >= JuxinAutoStage;
        public bool JuxinAutoReply => S.jxAutoReply && JuxinCanAutoReply;
        public bool JuxinOpen => S.jxVersion > 0;
        public double JuxinClock => S.jxClock;

        public bool SetJuxinAutoReply(bool on)
        {
            if (on && !JuxinCanAutoReply) return false;
            S.jxAutoReply = on;
            return true;
        }

        /// <summary>Fixes what an older or damaged save may hold. Called lazily: the constructor does not know about 巨信.</summary>
        void RepairJuxin()
        {
            if (jxRepaired && S.jxThreads != null && S.jxSeen != null && S.jxLikes != null && S.jxEarned != null) return;
            jxRepaired = true;
            if (S.jxThreads == null) S.jxThreads = new List<XgJxThread>();
            if (S.jxSeen == null) S.jxSeen = new List<string>();
            if (S.jxLikes == null) S.jxLikes = new List<string>();
            if (S.jxEarned == null) S.jxEarned = new List<XgLabelCount>();
            S.jxThreads.RemoveAll(t => t == null || string.IsNullOrEmpty(t.id));
            foreach (var t in S.jxThreads)
            {
                if (t.messages == null) t.messages = new List<XgJxMessage>();
                t.messages.RemoveAll(m => m == null);
                t.mood = Math.Max(-JuxinMoodLimit, Math.Min(JuxinMoodLimit, t.mood));
                t.unread = Math.Max(0, Math.Min(t.messages.Count, t.unread));
            }
            if (!FiniteJx(S.jxClock) || S.jxClock < 0) S.jxClock = 0;
            if (S.jxRng == 0) S.jxRng = 0x7A11C0DEL;
            if (S.jxAutoReply && !JuxinCanAutoReply) S.jxAutoReply = false;
            if (!jxHooked)
            {
                jxHooked = true;
                SlaSigned += OnJuxinSlaSigned;
                SlaSettled += OnJuxinSlaSettled;
                MemeDrifted += OnJuxinDrift;
            }
        }

        static bool FiniteJx(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        double JxRoll()
        {
            long x = S.jxRng;
            x ^= x << 13; x ^= (long)((ulong)x >> 7); x ^= x << 17;
            S.jxRng = x == 0 ? 0x7A11C0DEL : x;
            return (double)((ulong)S.jxRng >> 11) / (1UL << 53);
        }

        int JxPick(int count) => count <= 1 ? 0 : Math.Min(count - 1, (int)(JxRoll() * count));

        /// <summary>True the first time per save (the desktop's one-off intro lines).</summary>
        public bool JuxinTakeOnce(string key)
        {
            RepairJuxin();
            if (JxSeen("once:" + key)) return false;
            JxMark("once:" + key);
            return true;
        }

        /// <summary>The AI is writing an answer in this thread right now (the view shows 正在输入).</summary>
        public bool JuxinReplying(string thread) => thread != null && jxInFlight.Contains(thread);

        bool JxSeen(string key) => S.jxSeen.Contains(key);
        void JxMark(string key) { if (!S.jxSeen.Contains(key)) S.jxSeen.Add(key); }

        public XgJxThread JuxinThread(string id)
        {
            if (S.jxThreads == null) return null;
            foreach (var t in S.jxThreads) if (t.id == id) return t;
            return null;
        }

        XgJxThread JxThread(string id)
        {
            var t = JuxinThread(id);
            if (t == null) S.jxThreads.Add(t = new XgJxThread { id = id, last = S.jxClock });
            return t;
        }

        public int JuxinUnread { get { int n = 0; if (S.jxThreads != null) foreach (var t in S.jxThreads) n += t.unread; return n; } }

        /// <summary>Chats (friends, clients, groups), most recent first; official accounts are folded into 订阅号.</summary>
        public List<XgJxThread> JuxinChats() => JxList(false);
        public List<XgJxThread> JuxinSubscriptions() => JxList(true);

        List<XgJxThread> JxList(bool subscriptions)
        {
            var list = new List<XgJxThread>();
            if (S.jxThreads == null) return list;
            foreach (var t in S.jxThreads) if (t.messages.Count > 0 && JuxinIsSubscription(t.id) == subscriptions) list.Add(t);
            list.Sort((a, b) => b.last.CompareTo(a.last));
            return list;
        }

        public int JuxinSubscriptionUnread { get { int n = 0; foreach (var t in JuxinSubscriptions()) n += t.unread; return n; } }

        public void JuxinMarkRead(string thread)
        {
            var t = JuxinThread(thread);
            if (t != null) t.unread = 0;
            if (thread == "subscriptions") foreach (var s in JuxinSubscriptions()) s.unread = 0;
        }

        XgJxMessage Post(string thread, XgJxKind kind, string zh, string en, string titleZh = "", string titleEn = "", string whoZh = "", string whoEn = "", bool waits = false)
        {
            var t = JxThread(thread);
            var m = new XgJxMessage { kind = (int)kind, zh = zh ?? "", en = en ?? zh ?? "", titleZh = titleZh ?? "", titleEn = titleEn ?? "", whoZh = whoZh ?? "", whoEn = whoEn ?? "", date = Today, minute = JxMinute(), at = S.jxClock };
            JxAdd(t, m);
            if (JuxinShowing != thread) t.unread++;
            if (waits && t.waitingSince < 0) { t.waitingSince = S.jxClock; t.nudges = 0; }
            JuxinReceived?.Invoke(t, m);
            return m;
        }

        void JxAdd(XgJxThread t, XgJxMessage m)
        {
            t.messages.Add(m);
            if (t.messages.Count > JuxinMessageLimit) t.messages.RemoveRange(0, t.messages.Count - JuxinMessageLimit);
            t.last = S.jxClock;
        }

        int JxMinute() => (int)(TimeOfDay / 60) % 1440;

        void Later(double seconds, string thread, XgJxMessage message, bool waits = false) => jxQueue.Add((S.jxClock + seconds, thread, message, waits));

        static XgJxMessage JxLine(string zh, string en, string whoZh = "", string whoEn = "") => new XgJxMessage { zh = zh, en = en, whoZh = whoZh, whoEn = whoEn };

        // ───────────── the tick ─────────────

        /// <summary>Advances 巨信 by <paramref name="dt"/> seconds. Opens it at stage 4; does nothing before.</summary>
        public void JuxinTick(double dt, IXgHost host)
        {
            RepairJuxin();
            if (!JuxinUnlocked || !FiniteJx(dt) || dt < 0) return;
            if (S.jxVersion == 0)
            {
                // Wait for the real date, so the news already past is history and not a backlog.
                if (!TodayKnown) return;
                OpenJuxin();
            }
            S.jxClock += Math.Min(dt, 3600);
            DeliverQueued();
            JxContracts(dt);
            JxNewsTick();
            JxChatter();
            JxWaiting(host);
        }

        /// <summary>Stage 4: the official push first, then friends say hello. What happened before is history, not news.</summary>
        void OpenJuxin()
        {
            S.jxVersion = 1;
            S.jxNextChat = S.jxClock + 120;
            S.jxNextFamily = S.jxClock + 240;
            S.jxWeek = JuxinWeek(Today);
            Post(JxTeam, XgJxKind.Article,
                "不需要下载安装，用完即走。首批内测开放 200 个名额，内测期间请勿外传截图。\n（你没收到邀请。）\n\n▼ 点击阅读原文 ▼",
                "No download, no install: use it and go. The first closed beta has 200 places; please don't share screenshots.\n(You were not invited.)\n\n▼ Read the original ▼",
                "巨信「小程序」开始内测", "Juxin \"Mini Programs\" enter closed beta");
            // Contracts signed before 巨信 existed: their accounts are followed, with the old notice already read.
            foreach (var c in XgCatalog.Contracts)
            {
                if (!Signed(c.id)) continue;
                JxMark("sign:" + c.id);
                var m = SignedArticle(c);
                var t = JxThread(JuxinOa(c.id));
                JxAdd(t, m);
            }
            foreach (var c in XgCatalog.Contracts) if (!Signed(c.id) && CanSign(c)) JxMark("invite:" + c.id);
            foreach (var n in JuxinNewsItems) if (n.date < Today) JxMark("news:" + n.date + n.titleZh);
            Post(JxAjie, XgJxKind.Text, "加你巨信了。YY 太土了，现在甲方都用这个。", "Added you on Juxin. YY's so old now, clients all use this.", waits: true);
            Post(JxFamily, XgJxKind.Notice, "老爸邀请你加入了群聊「相亲相爱一家人」", "Dad invited you to the group \"One Big Loving Family\"");
            Post(JxFamily, XgJxKind.Text, "早上好！[太阳]", "Good morning! [sun]", whoZh: "老爸", whoEn: "Dad");
        }

        void DeliverQueued()
        {
            for (int i = 0; i < jxQueue.Count; i++)
            {
                var q = jxQueue[i];
                if (q.at > S.jxClock) continue;
                jxQueue.RemoveAt(i--);
                var m = Post(q.thread, (XgJxKind)q.message.kind, q.message.zh, q.message.en, q.message.titleZh, q.message.titleEn, q.message.whoZh, q.message.whoEn, q.waits);
                m.amount = q.message.amount;
            }
        }

        public static int JuxinWeek(int yyyymmdd)
        {
            int y = yyyymmdd / 10000, m = yyyymmdd / 100 % 100, d = yyyymmdd % 100;
            try { return (int)Math.Floor((new DateTime(y, Math.Max(1, Math.Min(12, m)), Math.Max(1, Math.Min(28, d))) - new DateTime(2016, 1, 4)).TotalDays / 7); }
            catch (ArgumentException) { return 0; }
        }

        // ───────────── clients ─────────────

        string AiName => Profile != null && Profile.name.Length > 0 ? Profile.name : AppNames.AiZh;

        XgJxMessage SignedArticle(XgContract c)
        {
            var oa = JuxinClient(c.id);
            string acc = Pct(BestAcc(c.dataset));
            return new XgJxMessage
            {
                kind = (int)XgJxKind.Article, date = Today, minute = JxMinute(), at = S.jxClock,
                titleZh = "喜报｜" + c.job + "项目正式上线", titleEn = "Good news: " + c.jobEn + " goes live",
                zh = "经过严格测评，合作方以 " + acc + " 的准确率通过验收。" + c.job + "即日起正式上线，感谢广大用户的支持！\n\n长按识别二维码，关注「" + oa.oaZh + "」\n▼ 点击阅读原文 ▼",
                en = "After strict testing our partner passed acceptance at " + acc + ". " + c.jobEn + " goes live today. Thank you for your support!\n\nLong-press the QR code to follow " + oa.oaEn + "\n▼ Read the original ▼",
            };
        }

        void JxContracts(double dt)
        {
            foreach (var c in XgCatalog.Contracts)
            {
                if (Signed(c.id))
                {
                    double income = ContractIncome(c) * Math.Min(dt, 3600);
                    if (income > 0) SetCount(S.jxEarned, c.id, Count(S.jxEarned, c.id) + income);
                    if (JxSeen("sign:" + c.id)) continue;
                    JxMark("sign:" + c.id);
                    var m = SignedArticle(c);
                    Post(JuxinOa(c.id), XgJxKind.Article, m.zh, m.en, m.titleZh, m.titleEn);
                    var who = JuxinClient(c.id);
                    if (c.id == "crossborder")
                        Later(4, JxCousin, JxLine("小店的翻译以后就靠你了！英文我看不懂，你说行就行。", "The shop's translations are on you now! I can't read English, so if you say it's fine, it's fine."), true);
                    else
                        Later(4, JuxinDm(c.id), JxLine("你好，我是" + who.personZh + "。以后" + c.job + "的事就在巨信上联系，方便。", "Hi, this is " + who.personEn + ". Let's talk about " + c.jobEn + " on Juxin from now on, it's easier."), true);
                }
                else if (!JxSeen("invite:" + c.id) && CanSign(c))
                {
                    JxMark("invite:" + c.id);
                    var oa = JuxinClient(c.id);
                    Post(JuxinOa(c.id), XgJxKind.Article,
                        "因业务发展需要，现面向社会招募" + c.job + "外包团队。要求：准确率不低于 " + Pct(c.threshold) + "。签约即付首款 ¥" + F(c.signBonus, "0") + "。\n有意者请在「" + AppNames.AppZh + "」订单页签约。\n\n长按识别二维码，关注「" + oa.oaZh + "」",
                        "We are hiring an outsourcing team for " + c.jobEn + ". Requirement: at least " + Pct(c.threshold) + " accuracy. ¥" + F(c.signBonus, "0") + " paid on signing.\nSign on the " + AppNames.AppEn + " Contracts page.\n\nLong-press the QR code to follow " + oa.oaEn,
                        "招募｜" + c.job + "外包，准确率 ≥ " + Pct(c.threshold) + " 即可合作", "Hiring: " + c.jobEn + ", " + Pct(c.threshold) + " accuracy or better");
                }
            }
            if (!TodayKnown) return;
            int week = JuxinWeek(Today);
            if (week <= S.jxWeek) return;
            S.jxWeek = week;
            foreach (var c in XgCatalog.Contracts)
            {
                double earned = Count(S.jxEarned, c.id);
                if (earned < 1) continue;
                SetCount(S.jxEarned, c.id, 0);
                string yuan = "¥" + F(earned, "0");
                Post(JuxinOa(c.id), XgJxKind.Article,
                    "尊敬的合作方：本周" + c.job + "服务费共计 " + yuan + "，已打入您的账户，请注意查收。\n如有疑问，请于工作日 9:00–17:00 回复「人工」。",
                    "Dear partner: this week's fees for " + c.jobEn + " come to " + yuan + " and have been paid into your account.\nQuestions? Reply \"agent\" on workdays, 9:00–17:00.",
                    "结算通知｜本周" + c.job + "服务费 " + yuan, "Settlement: this week's " + c.jobEn + " fees, " + yuan);
            }
        }

        void OnJuxinSlaSigned(XgSlaOffer offer)
        {
            if (!JuxinOpen || offer == null) return;
            var who = JuxinClient(offer.id);
            if (who == null) return;
            Post(JuxinOa(offer.id), XgJxKind.Article,
                "我司" + offer.job + "项目已选定供应商。合同约定：抽检合格率不低于 95%，一半服务费作为尾款，验收后结清。\n\n▼ 点击阅读原文 ▼",
                "Our " + offer.jobEn + " project has a supplier. Terms: at least 95% of spot checks must pass; half the fee is held until acceptance.\n\n▼ Read the original ▼",
                "公告｜" + offer.job + "供应商确定", "Notice: " + offer.jobEn + " supplier chosen");
            Later(4, JuxinDm(offer.id), JxLine("合同签了哈。合格率盯紧点，我们领导每天看报表。", "Contract's signed. Watch the pass rate, my boss reads the report every day."), true);
        }

        void OnJuxinSlaSettled(XgSlaResult result)
        {
            if (!JuxinOpen || result == null) return;
            var offer = SlaOffer(result.id);
            var who = JuxinClient(result.id);
            if (offer == null || who == null) return;
            string rate = Pct(result.PassRate);
            var dm = JxThread(JuxinDm(offer.id));
            switch (result.outcome)
            {
                case XgSlaOutcome.FiveStar:
                    Post(JuxinOa(offer.id), XgJxKind.Article, "感谢优质供应商在" + offer.job + "项目中的出色表现，抽检合格率 " + rate + "。特此表彰。",
                        "Thanks to our supplier for excellent work on " + offer.jobEn + ": " + rate + " of spot checks passed. Commended.", "表彰｜优质供应商", "Commendation: a quality supplier");
                    Later(5, dm.id, JxLine("这次合作很愉快，五星好评已经给了。下次还找你。", "Great working with you, five stars given. We'll call you again."));
                    break;
                case XgSlaOutcome.Docked:
                    Post(JuxinOa(offer.id), XgJxKind.Article, "经抽检，" + offer.job + "项目合格率为 " + rate + "，未达合同约定的 95%。按合同扣除一半尾款。",
                        "Spot checks on " + offer.jobEn + " passed at " + rate + ", below the contracted 95%. Half the balance is withheld as agreed.", "通报｜关于" + offer.job + "质量问题的处理意见", "Notice: on the quality of " + offer.jobEn);
                    Later(5, dm.id, JxLine("合格率才 " + rate + "，按合同扣一半尾款，别怪我。领导那边我也不好交代。", "Only " + rate + " passed. Half the balance is docked per the contract, sorry. I'm in trouble with my boss too."), true);
                    dm.mood = Math.Max(-JuxinMoodLimit, dm.mood - 1);
                    break;
                case XgSlaOutcome.Cancelled:
                    Post(JuxinOa(offer.id), XgJxKind.Article, "因供应商账号被平台举报，" + offer.job + "项目即日起终止合作，尾款不予结算。",
                        "Our supplier's account was reported, so " + offer.jobEn + " ends today and the balance will not be paid.", "公告｜终止合作", "Notice: contract terminated");
                    Later(5, dm.id, JxLine("你们账号被举报了？这单只能先停了。", "Your account got reported? We have to stop this one."), true);
                    dm.mood = Math.Max(-JuxinMoodLimit, dm.mood - 1);
                    break;
            }
        }

        void OnJuxinDrift(XgMemeDriftNotice notice)
        {
            if (!JuxinOpen || notice == null) return;
            foreach (var c in XgCatalog.Contracts)
            {
                if (!Signed(c.id) || !notice.desks.Contains(c.dataset) || JxSeen("drift:" + c.id + notice.month)) continue;
                JxMark("drift:" + c.id + notice.month);
                string thread = JuxinDm(c.id);
                Later(6 + JxRoll() * 20, thread, JxLine("最近" + c.job + "怎么老出错？「" + notice.meme + "」这种词都认不出来，用户都投诉到我这了。",
                    "Why is " + c.jobEn + " getting things wrong lately? It can't even read \"" + notice.memeEn + "\". Users are complaining to me."), true);
            }
        }

        // ───────────── the calendar: 摆渡新闻, friends, the family group ─────────────

        public sealed class XgJxNews
        {
            public int date;
            public string titleZh, titleEn, zh, en;
        }

        /// <summary>摆渡新闻 pushes (design v1.1 §14.1, §14.5), 2016 headline style. Politics and tragedies stay out (§14.7).</summary>
        public static readonly XgJxNews[] JuxinNewsItems =
        {
            JxN(20160902, "三星 Note7 宣布全球召回", "Samsung recalls the Galaxy Note7 worldwide", "电池问题。手里有的赶紧关机，别放枕头边。", "A battery problem. If you have one, switch it off and keep it away from your pillow."),
            JxN(20160904, "G20 杭州峰会开幕", "The G20 summit opens in Hangzhou", "（一行标题）", "(Headline only)"),
            JxN(20160908, "重磅｜iPhone 7 发布：取消 3.5mm 耳机孔，新增亮黑色", "Big news: iPhone 7 drops the headphone jack, adds Jet Black", "网友：耳机转接头才是本体。肾，准备好了吗？", "Netizens: the dongle is the real product. Is your kidney ready?"),
            JxN(20160909, "谷歌 DeepMind 发布 WaveNet，机器合成的声音更像人了", "DeepMind's WaveNet makes machine speech sound human", "一段一段地生成声音波形，比拼接录音自然得多。", "It generates the sound wave sample by sample, far more natural than stitched recordings."),
            JxN(20160915, "天宫二号发射成功", "Tiangong-2 launches successfully", "中秋夜，月亮上又多了一个邻居。", "On Mid-Autumn night the moon gets another neighbour."),
            JxN(20160925, "世界最大单口径射电望远镜 FAST 落成", "FAST, the world's largest single-dish radio telescope, is complete", "500 米口径，号称「天眼」。三体迷：千万别回答。", "500 metres across, nicknamed the Sky Eye. Three-Body fans: do not answer."),
            JxN(20160928, "刚刚！谷歌翻译换成神经网络，错误减少六成", "Just in: Google Translate switches to a neural network, 60% fewer errors", "整句一起翻，不再一个词一个词地拼。", "It translates the whole sentence at once instead of word by word."),
            JxN(20161013, "鲍勃·迪伦获诺贝尔文学奖", "Bob Dylan wins the Nobel Prize in Literature", "答案在风中飘。", "The answer is blowing in the wind."),
            JxN(20161017, "神舟十一号发射成功", "Shenzhou-11 launches successfully", "两名航天员将在天宫二号驻留 30 天。", "Two astronauts will stay 30 days on Tiangong-2."),
            JxN(20161019, "微软：语音识别错误率追平人类", "Microsoft: speech recognition now as accurate as people", "电话录音转写，错误率 5.9%，和专业速记员差不多。", "Phone-call transcription at 5.9% errors, about the same as professional transcribers."),
            JxN(20161025, "小米 MIX 发布，全面屏概念手机", "Xiaomi unveils the MIX, an edge-to-edge concept phone", "屏占比 91.3%，听筒改成了陶瓷骨传导。", "A 91.3% screen-to-body ratio; the earpiece is now ceramic bone conduction."),
            JxN(20161112, "双 11 全天成交 1207 亿", "Singles' Day closes at ¥120.7 billion", "剁手党：吃土的日子开始了。", "Shoppers: time to eat dirt for a month."),
            JxN(20161113, "阿里 AI「鲁班」双 11 做了 1.7 亿张海报", "Alibaba's AI \"Luban\" made 170 million banners for Singles' Day", "设计师：我的饭碗……", "Designers: my rice bowl..."),
            JxN(20161116, "百度无人车在乌镇试运营", "Baidu's driverless cars start trial rides in Wuzhen", "乌镇互联网大会期间，游客可以免费体验。", "Visitors to the Wuzhen internet conference can ride for free."),
            JxN(20161202, "《你的名字。》内地上映", "\"Your Name.\" opens in mainland cinemas", "黄昏之时，记得带纸巾。", "At twilight, bring tissues."),
            JxN(20161205, "NIPS 2016 在巴塞罗那开幕，生成对抗网络教程爆满", "NIPS 2016 opens in Barcelona; the GAN tutorial is packed", "两个网络互相较劲：一个造假，一个打假。", "Two networks fight: one forges, one spots the forgeries."),
            JxN(20161222, "Steam 冬季特卖开始", "The Steam Winter Sale begins", "钱包：我还没准备好。", "Wallet: I'm not ready."),
        };

        static XgJxNews JxN(int date, string titleZh, string titleEn, string zh, string en) => new XgJxNews { date = date, titleZh = titleZh, titleEn = titleEn, zh = zh, en = en };

        void JxNewsTick()
        {
            if (!TodayKnown) return;
            foreach (var n in JuxinNewsItems)
            {
                if (n.date > Today) continue;
                string key = "news:" + n.date + n.titleZh;
                if (JxSeen(key)) continue;
                JxMark(key);
                // A save that skipped weeks gets only the current month's news, not a backlog.
                if (n.date / 100 != Today / 100) continue;
                Post(JxNews, XgJxKind.Article, n.zh + "\n\n▼ 点击阅读原文 ▼", n.en + "\n\n▼ Read the original ▼", n.titleZh, n.titleEn);
                return;
            }
        }

        sealed class JxSay
        {
            public string key, thread, whoZh, whoEn, zh, en;
            public int since, stage;
            public bool waits;
            public double packet;
        }

        static JxSay S2(string key, string thread, int since, int stage, string zh, string en, bool waits = false, string whoZh = "", string whoEn = "", double packet = 0) =>
            new JxSay { key = key, thread = thread, since = since, stage = stage, zh = zh, en = en, waits = waits, whoZh = whoZh, whoEn = whoEn, packet = packet };

        /// <summary>Friends' and family lines, each said once from its date (and stage) on.</summary>
        static readonly JxSay[] FriendLines =
        {
            S2("aj.1080", JxAjie, 20160901, 4, "网吧进了两张 1080，鲁大师跑分 99.7%。来摸摸？", "The café got two 1080s. 99.7% on Master Lu. Come touch them?", true),
            S2("aj.ow", JxAjie, 20160901, 4, "今晚屁股车，满五开。来不来？", "Overwatch tonight, five-stack. You in?", true),
            S2("aj.money", JxAjie, 20160910, 4, "借我 50 充个点卡，下周还。", "Lend me 50 for game time? I'll pay you back next week.", true),
            S2("aj.client", JxAjie, 20160915, 4, "听说你现在接的都是甲方的单？可以啊，带带我。", "Heard you take jobs from real clients now? Not bad, bring me along.", true),
            S2("aj.civ", JxAjie, 20161021, 4, "文明 6 再来一回合……天亮了。", "Civ 6, one more turn... and it's morning.", false),
            S2("aj.letter", JxAjie, 20161001, 5, "你那个 AI 能不能帮我写封情书？就一封。", "Can your AI write me a love letter? Just one.", true),
            S2("aj.packet", JxAjie, 20161101, 4, "斗图输了，愿赌服输。", "Lost the meme battle, a bet's a bet.", false, packet: 6.66),
            S2("aj.11", JxAjie, 20161111, 4, "购物车清空了，接下来一个月吃土。", "Cart emptied. Eating dirt for a month now.", false),
            S2("xg.ssr", JxXiaogang, 20160905, 4, "阴阳师十连抽，一个 SSR 都没有，非酋本酋。", "Ten pulls in Onmyoji and no SSR. Cursed.", true),
            S2("xg.essay", JxXiaogang, 20160920, 4, "作文好难写，你那个 AI 会写作文不？800 字。", "This essay is killing me. Can your AI write essays? 800 characters.", true),
            S2("xg.mom", JxXiaogang, 20161005, 4, "我妈收了我的电脑，偷偷用手机回你。", "Mum took my computer, I'm replying on my phone in secret.", false),
            S2("xg.s6", JxXiaogang, 20161030, 4, "SKT 三冠！！！Faker 牛！", "SKT three-peat!!! Faker is a god!", true),
            S2("xg.name", JxXiaogang, 20161203, 4, "看了《你的名字。》，哭成狗。你看了没？", "Watched \"Your Name.\" and cried like a baby. Have you seen it?", true),
            S2("cz.eat", JxCousin, 20160901, 4, "吃饭了吗？你那电脑别老开着。", "Have you eaten? Don't leave that computer on all the time.", true),
            S2("cz.agent", JxCousin, 20160918, 4, "表姐的小店招代理，一部手机就能创业。你朋友圈帮我转一下？", "My shop is recruiting agents, start a business with one phone. Share it on your Moments?", true),
            S2("cz.sad", JxCousin, 20161020, 4, "加班到十一点，蓝瘦，香菇。", "Overtime till eleven. So sad, want to cry.", false),
            S2("cz.11", JxCousin, 20161108, 4, "双 11 小店全场五折！你那个翻译能不能快点，客户等着上架。", "50% off everything for Singles' Day! Can the translations go faster? Customers are waiting.", true),
            S2("fm.food", JxFamily, 20160901, 4, "【震惊】这三种食物千万不能一起吃！转给你关心的人！", "[Shocking] Never eat these three foods together! Forward to the people you care about!", false, "二姨", "Auntie"),
            S2("fm.wifi", JxFamily, 20160912, 4, "WiFi 路由器放卧室会致癌？专家这样说……", "Does a Wi-Fi router in the bedroom cause cancer? Here's what experts say...", false, "二姨", "Auntie"),
            S2("fm.bill", JxFamily, 20160920, 4, "儿子，这个月电费怎么这么高？你那电脑一天开多久？", "Son, why is the electric bill so high this month? How long is that computer on?", true, "老爸", "Dad"),
            S2("fm.moon", JxFamily, 20160915, 4, "中秋快乐！发个红包，抢到的都是有福之人。", "Happy Mid-Autumn! A red packet: whoever grabs it is blessed.", false, "老爸", "Dad", 8.88),
            S2("fm.forward", JxFamily, 20161003, 4, "不转不是中国人！", "Forward this if you are Chinese!", false, "二姨", "Auntie"),
            S2("fm.cold", JxFamily, 20161025, 4, "降温了，秋裤穿上没有？", "It's getting cold. Are you wearing long johns?", true, "老爸", "Dad"),
            S2("fm.cousin", JxFamily, 20161101, 4, "表姐的小店大家多支持一下哈 [抱拳]", "Everyone please support my little shop [fist-palm salute]", false, "表姐", "Cousin"),
            S2("fm.ai", JxFamily, 20161115, 4, "听说现在电脑都能自己画海报了？那以后还要设计师干啥。", "I hear computers draw posters by themselves now? What will designers do?", true, "二姨", "Auntie"),
            S2("fm.packet2", JxFamily, 20161201, 4, "年底了，大家辛苦了，发个红包。", "Year's end, everyone worked hard. A red packet.", false, "老爸", "Dad", 5.20),
        };

        /// <summary>Repeating family-group posts once the dated ones are used up: morning greetings and small red packets.</summary>
        static readonly string[][] FamilyFill =
        {
            new[] { "老爸", "Dad", "早上好！[太阳]", "Good morning! [sun]" },
            new[] { "二姨", "Auntie", "转发：每天一杯温开水，医生都不说的秘密", "Forwarded: a cup of warm water a day, the secret doctors won't tell" },
            new[] { "老爸", "Dad", "[红包]", "[Red packet]" },
            new[] { "二姨", "Auntie", "看看这个，说得太对了！", "Look at this, so true!" },
        };

        /// <summary>Client contacts asking about deliveries ({0} = the job).</summary>
        static readonly string[][] ClientAsks =
        {
            new[] { "这批{0}的数据什么时候能交？领导在催。", "When can you deliver this batch of {0}? My boss is pushing." },
            new[] { "今天能出一版结果吗？", "Can you get us a version today?" },
            new[] { "周五前给个准确率报告呗，要写进周报。", "Send an accuracy report before Friday? It goes in the weekly report." },
            new[] { "在吗？", "You there?" },
            new[] { "{0}那边有个新需求，晚上方便电话吗？", "There's a new request for {0}. Free for a call tonight?" },
            new[] { "上次那批结果领导挺满意的，这批也拜托了。", "My boss liked the last batch. Please do this one well too." },
        };

        void JxChatter()
        {
            if (S.jxClock >= S.jxNextChat)
            {
                S.jxNextChat = S.jxClock + 240 + JxRoll() * 180;
                if (JxRoll() < .55 && ClientAsk()) { }
                else FriendLine(false);
            }
            if (S.jxClock >= S.jxNextFamily)
            {
                S.jxNextFamily = S.jxClock + 300 + JxRoll() * 300;
                if (!FriendLine(true))
                {
                    var f = FamilyFill[JxPick(FamilyFill.Length)];
                    if (f[2] == "[红包]") Packet(JxFamily, f[0], f[1], .5 + Math.Round(JxRoll() * 8, 2));
                    else Post(JxFamily, XgJxKind.Text, f[2], f[3], whoZh: f[0], whoEn: f[1]);
                }
            }
        }

        bool ClientAsk()
        {
            var candidates = new List<XgContract>();
            foreach (var c in XgCatalog.Contracts)
            {
                if (!Signed(c.id) || c.id == "crossborder") continue;
                var t = JuxinThread(JuxinDm(c.id));
                if (t != null && t.waitingSince >= 0) continue;
                candidates.Add(c);
            }
            if (candidates.Count == 0) return false;
            var pick = candidates[JxPick(candidates.Count)];
            var ask = ClientAsks[JxPick(ClientAsks.Length)];
            Post(JuxinDm(pick.id), XgJxKind.Text, string.Format(ask[0], pick.job), string.Format(ask[1], pick.jobEn), waits: true);
            return true;
        }

        bool FriendLine(bool family)
        {
            if (!TodayKnown) return false;
            foreach (var l in FriendLines)
            {
                if ((l.thread == JxFamily) != family || l.since > Today || S.stage < l.stage || JxSeen("say:" + l.key)) continue;
                JxMark("say:" + l.key);
                if (l.packet > 0) Packet(l.thread, l.whoZh, l.whoEn, l.packet, l.zh, l.en);
                else Post(l.thread, XgJxKind.Text, l.zh, l.en, whoZh: l.whoZh, whoEn: l.whoEn, waits: l.waits);
                return true;
            }
            return false;
        }

        void Packet(string thread, string whoZh, string whoEn, double amount, string zh = "恭喜发财，大吉大利", string en = "Wishing you luck and prosperity")
        {
            var m = Post(thread, XgJxKind.RedPacket, zh, en, "巨信红包", "Juxin red packet", whoZh, whoEn);
            m.amount = Math.Round(amount, 2);
        }

        /// <summary>
        /// Opens a red packet. Group packets go to whoever is quick: after <see cref="JuxinRedPacketLife"/> seconds
        /// it is 手慢了 (nothing). Returns the amount the player got, 0 when it was gone, −1 when there is nothing to open.
        /// </summary>
        public double OpenRedPacket(string thread, int index, IXgHost host)
        {
            var t = JuxinThread(thread);
            if (t == null || index < 0 || index >= t.messages.Count) return -1;
            var m = t.messages[index];
            if (m.kind != (int)XgJxKind.RedPacket || m.opened || m.mine) return -1;
            m.opened = true;
            bool late = JuxinIsGroup(thread) && S.jxClock - m.at > JuxinRedPacketLife;
            m.got = late || host == null ? 0 : m.amount;
            if (late) Earn("life.redpacket.slow");
            if (m.got > 0)
            {
                host.Earn(m.got);
                S.totalIncome += m.got;
                S.jxRedPackets++; S.jxRedPacketTotal += m.got;
            }
            return m.got;
        }

        // ───────────── answers, moods ─────────────

        /// <summary>The player types into a conversation. Official accounts take no messages (except the classic auto-reply).</summary>
        public bool JuxinSend(string thread, string text)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0 || !JuxinOpen) return false;
            if (text.Length > 200) text = text.Substring(0, 200);
            var t = JuxinThread(thread);
            if (t == null) return false;
            JxAdd(t, new XgJxMessage { mine = true, zh = text, en = text, date = Today, minute = JxMinute(), at = S.jxClock });
            if (JuxinIsSubscription(thread))
            {
                if (thread.StartsWith("oa.", StringComparison.Ordinal))
                    Later(2, thread, new XgJxMessage { kind = (int)XgJxKind.Notice, zh = "感谢您的关注！人工客服工作时间为工作日 9:00–17:00，回复「1」查看历史消息。", en = "Thanks for following! Our agents work weekdays 9:00–17:00. Reply \"1\" for past messages." });
                return true;
            }
            Answered(t, false);
            return true;
        }

        /// <summary>Someone was waiting: on time raises the mood, and they acknowledge.</summary>
        void Answered(XgJxThread t, bool byAi)
        {
            bool wasWaiting = t.waitingSince >= 0;
            bool onTime = wasWaiting && S.jxClock - t.waitingSince <= JuxinAnswerWindow;
            t.waitingSince = -1; t.nudges = 0;
            if (onTime) Mood(t, +1);
            if (!wasWaiting) return;
            var ack = Ack(t.id);
            if (ack != null) Later(3 + JxRoll() * 4, t.id, ack);
        }

        XgJxMessage Ack(string thread)
        {
            if (JuxinIsGroup(thread))
                return JxRoll() < .5 ? JxLine("[强]", "[thumbs up]", "老爸", "Dad") : JxLine("好孩子。", "Good kid.", "二姨", "Auntie");
            if (ClientOfThread(thread) != null)
            {
                string[][] acks = { new[] { "好的，辛苦。", "OK, thanks for your work." }, new[] { "收到 [OK]", "Got it [OK]" }, new[] { "行，等你消息。", "Fine, waiting on you." }, new[] { "[微笑]", "[smile]" } };
                var a = acks[JxPick(acks.Length)];
                return JxLine(a[0], a[1]);
            }
            switch (thread)
            {
                case JxAjie: return JxRoll() < .5 ? JxLine("哈哈，够意思。", "Haha, you're a real friend.") : JxLine("行，回头网吧见。", "Cool, see you at the café.");
                case JxXiaogang: return JxRoll() < .5 ? JxLine("666", "666") : JxLine("哈哈哈哈", "Hahahaha");
                case JxCousin: return JxRoll() < .5 ? JxLine("好，早点睡。", "OK, get some sleep.") : JxLine("嗯嗯，表姐忙去了。", "OK, gotta go.");
            }
            return null;
        }

        void Mood(XgJxThread t, int delta)
        {
            t.mood += delta;
            var c = ClientOfThread(t.id) ?? (t.id == JxCousin ? JuxinClient("crossborder") : null);
            if (c == null || c.id == "crossborder" && !Signed("crossborder")) { t.mood = Math.Max(-JuxinMoodLimit, Math.Min(JuxinMoodLimit, t.mood)); return; }
            if (t.mood >= JuxinMoodLimit)
            {
                t.mood = 0;
                double bonus = c.sla ? JuxinSlaBonus : Math.Max(JuxinBonusMin, Math.Min(JuxinBonusMax, JuxinBonusShare * XgCatalog.Contract(c.id).signBonus));
                S.jxBonuses++; S.jxBonusTotal += bonus;
                var m = new XgJxMessage { kind = (int)XgJxKind.RedPacket, zh = "辛苦费，合作愉快！", en = "For your trouble. Great working with you!", titleZh = "巨信红包", titleEn = "Juxin red packet", amount = Math.Round(bonus, 2) };
                Later(6, t.id, m);
                if (c.sla && QualityActive) AddCredit(1);
            }
            else if (t.mood <= -JuxinMoodLimit)
            {
                t.mood = 0;
                Later(6, t.id, JxLine("说实话，我们在考虑换一家了。", "Honestly, we're thinking about switching suppliers."));
                if (c.sla && QualityActive) AddCredit(-1);
            }
        }

        static readonly string[][] Nudges =
        {
            new[] { "？", "?" }, new[] { "在吗在吗", "Hello? Hello?" }, new[] { "人呢", "Where'd you go" }, new[] { "看到回一下。", "Reply when you see this." },
        };

        /// <summary>Ignored contacts ask again; twice ignored, they give up (each time the mood drops).</summary>
        void JxWaiting(IXgHost host)
        {
            foreach (var t in S.jxThreads)
            {
                if (t.waitingSince < 0 || jxInFlight.Contains(t.id) || S.jxClock - t.waitingSince < JuxinIgnoreSeconds) continue;
                Mood(t, -1);
                if (t.nudges >= 2)
                {
                    t.waitingSince = -1; t.nudges = 0;
                    if (ClientOfThread(t.id) != null) Post(t.id, XgJxKind.Text, "算了，我找别人问吧。", "Never mind, I'll ask someone else.");
                    continue;
                }
                t.nudges++;
                var n = Nudges[JxPick(Nudges.Length)];
                Post(t.id, XgJxKind.Text, n[0], n[1], whoZh: JuxinIsGroup(t.id) ? "老爸" : "", whoEn: JuxinIsGroup(t.id) ? "Dad" : "");
                t.waitingSince = S.jxClock;
            }
        }

        // ───────────── 让 AI 代我回 ─────────────

        /// <summary>The thread that has waited longest (past <see cref="JuxinAutoDelay"/>) for the AI, or null.</summary>
        public XgJxReplyJob TakeAutoReply()
        {
            RepairJuxin();
            if (!JuxinAutoReply || !JuxinOpen || Listening) return null;
            XgJxThread best = null;
            foreach (var t in S.jxThreads)
                if (t.waitingSince >= 0 && !jxInFlight.Contains(t.id) && S.jxClock - t.waitingSince >= JuxinAutoDelay && (best == null || t.waitingSince < best.waitingSince)) best = t;
            if (best == null) return null;
            jxInFlight.Add(best.id);
            string incoming = LastIncoming(best);
            double chance = double.IsNaN(JuxinGaffeOverride) ? GaffeChance : JuxinGaffeOverride;
            var limits = Limits();
            return new XgJxReplyJob
            {
                thread = best.id, incoming = incoming, gaffe = JxRoll() < chance,
                system = AutoReplyPrompt(best.id, incoming),
                tokens = Math.Max(48, Math.Min(160, limits.tokens)), temperature = limits.temperature,
            };
        }

        /// <summary>Its personality decides how often it slips: a cheeky one more, a flattering one promises too much.</summary>
        public double GaffeChance
        {
            get
            {
                double c = .15 + (ActualAxis(1) - 50) / 500.0;
                if (Flatters) c += .05;
                return Math.Max(.08, Math.Min(.3, c));
            }
        }

        string LastIncoming(XgJxThread t)
        {
            for (int i = t.messages.Count - 1; i >= 0; i--) if (!t.messages[i].mine && t.messages[i].kind == (int)XgJxKind.Text) return English ? t.messages[i].en : t.messages[i].zh;
            return "";
        }

        /// <summary>The 对话 page's persona prompt plus who is writing and the stage's form rule (§11.5).</summary>
        public string AutoReplyPrompt(string thread, string incoming)
        {
            int month = Math.Max(1, Math.Min(12, Today / 100 % 100));
            var hot = new List<string>();
            foreach (var w in XgMemes.HotWordsFor("A", Today)) hot.Add(w.word);
            var sb = new StringBuilder(PersonaPrompt(month, hot));
            string owner = Profile.callMe.Length > 0 ? Profile.callMe : T("主人", "your owner");
            sb.Append('\n').Append(T("现在你在巨信上替" + owner + "回消息，对方以为是" + owner + "本人在回。", "You are answering Juxin messages for " + owner + "; the other person thinks it is " + owner + " writing."));
            sb.Append(T("发消息的是：", " The sender is: ")).Append(JuxinName(thread, English)).Append(T("，", ", ")).Append(Relation(thread)).Append(T("。", ". "));
            sb.Append(T("对方不是" + owner + "，不要用「" + owner + "」称呼对方，也不要说你是程序。", "They are not " + owner + ": do not call them that, and never say you are a program. "));
            sb.Append(T("像真人发巨信一样，一两句，口语。", "Write like a person texting: one or two casual sentences. "));
            sb.Append('\n').Append(EraLexicon.PromptRule(English)).Append('\n').Append(StageRule(incoming));
            if (English) sb.Append(" Reply in English.");
            return sb.ToString();
        }

        string Relation(string thread)
        {
            var c = ClientOfThread(thread);
            if (c != null)
            {
                var con = XgCatalog.Contract(c.id);
                var sla = SlaOffer(c.id);
                string job = con != null ? T(con.job, con.jobEn) : sla != null ? T(sla.job, sla.jobEn) : "";
                return T("甲方的对接人，你们在合作「" + job + "」，要客气、靠谱", "the client's contact for \"" + job + "\": be polite and reliable");
            }
            switch (thread)
            {
                case JxAjie: return T("主人的朋友阿杰，十九岁网管，懂显卡，爱吹牛", "a friend, Ajie, a 19-year-old café admin who knows GPUs and likes to brag");
                case JxXiaogang: return T("主人的朋友小刚，高中生，沉迷英雄联盟，嘴贫", "a friend, Xiaogang, a high-schooler hooked on League of Legends");
                case JxCousin: return T("主人的表姐，在省城做会计，也开微商小店，爱唠叨", "the owner's cousin, an accountant who also runs a small online shop and fusses");
                case JxFamily: return T("家族群，长辈们爱转养生文章，要有礼貌", "the family group, where elders forward health articles; be polite");
            }
            return "";
        }

        /// <summary>
        /// The model's answer (null when it is offline or failed) becomes the reply sent in the player's name. A
        /// gaffe job ignores the model and says one of the socially wrong lines; the contact reacts.
        /// </summary>
        public bool CompleteAutoReply(XgJxReplyJob job, string modelReply)
        {
            if (job == null) return false;
            jxInFlight.Remove(job.thread);
            var t = JuxinThread(job.thread);
            if (t == null || t.waitingSince < 0 || !JuxinAutoReply) return false;
            string text;
            XgJxMessage reaction = null;
            if (job.gaffe)
            {
                var g = Gaffe(job.thread, job.incoming, out reaction);
                text = g;
            }
            else
            {
                text = CleanAutoReply(modelReply);
                if (text.Length == 0 || RepeatsInThread(t, text)) text = OfflineAutoReply(job.thread, job.incoming);
            }
            JxAdd(t, new XgJxMessage { mine = true, ai = true, gaffe = job.gaffe, zh = text, en = text, date = Today, minute = JxMinute(), at = S.jxClock });
            S.jxAiReplies++;
            if (job.gaffe)
            {
                S.jxGaffes++;
                t.waitingSince = -1; t.nudges = 0;
                Mood(t, -1);
                if (reaction != null) Later(3 + JxRoll() * 3, t.id, reaction);
            }
            else Answered(t, true);
            return true;
        }

        /// <summary>Thinking tags, post-2016 slang, quotes and a speaker prefix go; long replies are cut.</summary>
        static string CleanAutoReply(string reply)
        {
            reply = reply ?? "";
            int end = reply.IndexOf("</think>", StringComparison.Ordinal);
            if (end >= 0) reply = reply.Substring(end + 8);
            reply = (EraLexicon.Scrub(reply) ?? "").Trim().Trim('"', '「', '」', '“', '”');
            int colon = reply.IndexOfAny(new[] { '：', ':' });
            if (colon > 0 && colon <= 6) reply = reply.Substring(colon + 1).Trim();
            if (reply.Length > 90) reply = reply.Substring(0, 90) + "…";
            return reply;
        }

        static bool RepeatsInThread(XgJxThread t, string text)
        {
            int seen = 0;
            for (int i = t.messages.Count - 1; i >= 0 && seen < 4; i--)
            {
                if (!t.messages[i].mine) continue;
                seen++;
                if (XgSpeechPolicy.Similar(t.messages[i].zh, text)) return true;
            }
            return false;
        }

        /// <summary>A polite line for when the model is not running (or repeated itself).</summary>
        string OfflineAutoReply(string thread, string incoming)
        {
            string[][] pool;
            if (ClientOfThread(thread) != null)
                pool = incoming.Contains("出错") || incoming.Contains("投诉") || incoming.Contains("扣") || incoming.IndexOf("wrong", StringComparison.OrdinalIgnoreCase) >= 0
                    ? new[] { new[] { "抱歉抱歉，最近新词多，我们马上重新训练。", "Sorry about that, lots of new words lately. We're retraining right away." }, new[] { "收到，我这边排查一下，今天给您答复。", "Got it, I'll look into it and get back to you today." } }
                    : new[] { new[] { "收到，今天之内给您。", "Got it, you'll have it today." }, new[] { "好的，我们这边加紧。", "OK, we'll speed up on our end." }, new[] { "在的，您说。", "I'm here, go ahead." }, new[] { "数据已经在跑了，晚点发您。", "It's running now, I'll send it later." } };
            else if (JuxinIsGroup(thread))
                pool = new[] { new[] { "收到。", "Got it." }, new[] { "好的，知道了。", "OK, noted." }, new[] { "早点休息。", "Get some rest." } };
            else
                pool = new[] { new[] { "哈哈，晚点说。", "Haha, talk later." }, new[] { "在忙，回头找你。", "Busy now, I'll find you later." }, new[] { "可以啊。", "Sure." }, new[] { "下次一定。", "Next time for sure." } };
            var t = JuxinThread(thread);
            for (int tries = 0; tries < 6; tries++)
            {
                var p = pool[JxPick(pool.Length)];
                string line = T(p[0], p[1]);
                if (t == null || !RepeatsInThread(t, line)) return line;
            }
            return T(pool[0][0], pool[0][1]);
        }

        /// <summary>
        /// The socially wrong replies: calling a client by the name it calls its owner, statistics nobody asked for,
        /// a meme to a complaint, promising the moon, telling the truth about who does the work, a Tang poem. Each
        /// comes with the other side's reaction, so the player sees why it was wrong.
        /// </summary>
        string Gaffe(string thread, string incoming, out XgJxMessage reaction)
        {
            string call = Profile.callMe.Length > 0 && Profile.callMe != "你" ? Profile.callMe : T("主人", "Master");
            string self = Profile.self.Length > 0 ? Profile.self : T("我", "I");
            bool group = JuxinIsGroup(thread);
            bool client = ClientOfThread(thread) != null || thread == JxCousin && Signed("crossborder");
            string meme = Today >= 20161001 ? T("蓝瘦，香菇。", "So sad, want to cry.") : T("洪荒之力已经用完了。", "My primordial power is all used up.");
            var options = new List<string[]>();
            if (group)
            {
                options.Add(new[] { "根据" + self + "的训练数据，这条是谣言的概率是 97%。", "According to my training data, there is a 97% chance this is a rumour.", "这孩子怎么说话的。", "What kind of way is that to talk?", "二姨", "Auntie" });
                options.Add(new[] { "已阅。", "Read.", "……", "...", "老爸", "Dad" });
                options.Add(new[] { "床前明月光，疑是地上霜。", "Before my bed the moonlight glows; I take it for frost on the ground.", "孩子是不是学习太累了？", "Is the child studying too hard?", "二姨", "Auntie" });
            }
            else if (client)
            {
                options.Add(new[] { call + "，收到！马上办。", call + ", got it! On it right away.", "……谁是你" + call + "？", "...Who are you calling " + call + "?", "", "" });
                options.Add(new[] { "根据统计，您本周已经催了 " + (3 + JxPick(5)) + " 次，比上周多 2 次。", "By my count you've chased us " + (3 + JxPick(5)) + " times this week, two more than last week.", "……", "...", "", "" });
                options.Add(new[] { "没问题！明早之前准确率百分之百，不要钱！", "No problem! 100% accuracy by tomorrow morning, free of charge!", "这可是你说的，我截图了。", "You said it. I've taken a screenshot.", "", "" });
                options.Add(new[] { "其实这些都是" + self + "标的，" + call + "在睡觉。", "Actually " + self + " did all of these; " + call + " is asleep.", "你们工作室到底几个人？", "How many people does your studio actually have?", "", "" });
                options.Add(new[] { meme, meme, "？？？", "???", "", "" });
                options.Add(new[] { "[微笑]", "[smile]", "你这是什么意思？", "What's that supposed to mean?", "", "" });
            }
            else
            {
                options.Add(new[] { "好的，已为您转接人工客服。", "Certainly, transferring you to a human agent.", "？你被盗号了？", "? Did someone steal your account?", "", "" });
                options.Add(new[] { call + "说他不在。", call + " says he's not here.", "那你是谁？", "Then who are you?", "", "" });
                options.Add(new[] { "在。" + self + "一直在。" + self + "不睡觉。", "Here. " + self + " is always here. " + self + " does not sleep.", "……有点吓人。", "...That's a bit creepy.", "", "" });
                options.Add(new[] { "床前明月光，疑是地上霜。", "Before my bed the moonlight glows; I take it for frost on the ground.", "你今天说话怪怪的。", "You're talking weird today.", "", "" });
            }
            var pick = options[JxPick(options.Count)];
            reaction = JxLine(pick[2], pick[3], pick[4], pick[5]);
            return T(pick[0], pick[1]);
        }

        // ───────────── 朋友圈 ─────────────

        public static readonly XgJxMoment[] JuxinMoments =
        {
            new XgJxMoment { id = "m.aj1080", who = JxAjie, date = 20160903, zh = "1080 到手！鲁大师跑分 99.7%，战未来。", en = "Got the 1080! 99.7% on Master Lu. Future-proof." },
            new XgJxMoment { id = "m.xgssr", who = JxXiaogang, date = 20160910, zh = "十连抽，全是 R。非酋本酋。", en = "Ten pulls, all R. Cursed by the gacha." },
            new XgJxMoment { id = "m.czagent", who = JxCousin, date = 20160919, zh = "一部手机就能创业！小店招代理，有意私聊。", en = "Start a business with one phone! My shop is recruiting agents, DM me." },
            new XgJxMoment { id = "m.xgholiday", who = JxXiaogang, date = 20161007, zh = "国庆七天，作业一个字没动。", en = "Seven days of National Day holiday, homework untouched." },
            new XgJxMoment { id = "m.ajciv", who = JxAjie, date = 20161022, zh = "再来一回合……天亮了。", en = "One more turn... and it's light outside." },
            new XgJxMoment { id = "m.czsad", who = JxCousin, date = 20161024, zh = "蓝瘦，香菇。", en = "So sad, want to cry." },
            new XgJxMoment { id = "m.xgskt", who = JxXiaogang, date = 20161030, zh = "SKT 三冠！！！", en = "SKT three-peat!!!" },
            new XgJxMoment { id = "m.cz11", who = JxCousin, date = 20161110, zh = "双 11 小店全场五折，转发集赞送面膜！", en = "50% off for Singles' Day! Share and collect likes for a free face mask!" },
            new XgJxMoment { id = "m.aj11", who = JxAjie, date = 20161112, zh = "吃土中。", en = "Eating dirt." },
            new XgJxMoment { id = "m.xgname", who = JxXiaogang, date = 20161203, zh = "《你的名字。》哭成狗。", en = "\"Your Name.\" made me cry like a baby." },
            new XgJxMoment { id = "m.ajsteam", who = JxAjie, date = 20161223, zh = "Steam 冬促，喜加一。", en = "Steam Winter Sale: one more game I won't play." },
        };

        /// <summary>Friends' posts up to today, newest first.</summary>
        public List<XgJxMoment> JuxinMomentsVisible()
        {
            var list = new List<XgJxMoment>();
            if (!JuxinOpen) return list;
            foreach (var m in JuxinMoments) if (m.date <= Today) list.Add(m);
            list.Sort((a, b) => b.date.CompareTo(a.date));
            return list;
        }

        public bool JuxinLike(string id)
        {
            RepairJuxin();
            if (S.jxLikes.Contains(id)) { S.jxLikes.Remove(id); return false; }
            S.jxLikes.Add(id);
            return true;
        }

        public bool JuxinLiked(string id) => S.jxLikes != null && S.jxLikes.Contains(id);

        /// <summary>Every authored 巨信 line in both languages (content tests).</summary>
        public static IEnumerable<string> JuxinAuthoredLines()
        {
            foreach (var c in JuxinClients) { yield return c.oaZh; yield return c.oaEn; yield return c.personZh; yield return c.personEn; }
            foreach (var n in JuxinNewsItems) { yield return n.titleZh; yield return n.titleEn; yield return n.zh; yield return n.en; }
            foreach (var l in FriendLines) { yield return l.zh; yield return l.en; }
            foreach (var f in FamilyFill) foreach (var s in f) yield return s;
            foreach (var a in ClientAsks) foreach (var s in a) yield return s;
            foreach (var n in Nudges) foreach (var s in n) yield return s;
            foreach (var m in JuxinMoments) { yield return m.zh; yield return m.en; }
        }
    }
}
