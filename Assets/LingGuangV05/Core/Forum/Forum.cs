using System;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core.Story;

namespace LingGuangV05.Core.Forum
{
    /// <summary>A 摆渡贴吧 user: forum ID, level, signature.</summary>
    public sealed class ForumUser
    {
        public string id = "", name = "", nameEn = "", sign = "", signEn = "";
        public int level = 1;
    }

    /// <summary>One floor of a thread. It may appear later (<see cref="since"/>, <see cref="minStage"/>, <see cref="afterSeconds"/>) or be deleted from a stage on.</summary>
    public sealed class ForumFloor
    {
        public string author = "", text = "", textEn = "";
        public DateTime since = DateTime.MinValue;
        public int minStage, deletedFromStage;
        /// <summary>Replies to the player's own help post arrive this long after it was posted.</summary>
        public double afterSeconds;
        public bool folded;
    }

    public sealed class ForumThread
    {
        public string id = "", board = "", author = "", title = "", titleEn = "";
        public DateTime date;
        public int minStage, maxStage = 99;
        /// <summary>The player's own post (我的帖子).</summary>
        public bool mine;
        /// <summary>Opens on a 404 page (the stage-3 "我也被发了" thread).</summary>
        public bool dead;
        /// <summary>Only after the player posts it (the optional stage-1 help post).</summary>
        public bool helpPost;
        /// <summary>Only once the lab has raised this flag (for example "qc.trap" after the first trap-item fine).</summary>
        public string requires = "";
        public List<ForumFloor> floors = new List<ForumFloor>();
    }

    public sealed class ForumBoard { public string id = "", name = "", nameEn = ""; }

    /// <summary>What decides what the forum shows: the lab stage, the calendar day, game flags and the player's own actions.</summary>
    public sealed class ForumContext
    {
        public int stage = 1;
        public DateTime today = GameCalendar.Start.Date;
        public bool helpPosted;
        public double secondsSinceHelp;
        public bool english;
        /// <summary>Answers whether a game flag is set (threads with "requires"); null means no flag is set.</summary>
        public Func<string, bool> flag;
    }

    public enum FloorView { Shown, Deleted, Folded, Hidden }

    /// <summary>forum.json: users, boards and dated threads (design v1.1 §10, §14.6 and the per-stage clues).</summary>
    public sealed class ForumLibrary
    {
        public const string Me = "me", LaoZhou = "laozhou", ZhouNow = "zhou_now";
        public readonly Dictionary<string, ForumUser> Users = new Dictionary<string, ForumUser>();
        public readonly List<ForumBoard> Boards = new List<ForumBoard>();
        public readonly List<ForumThread> Threads = new List<ForumThread>();

        public static ForumLibrary Parse(string json)
        {
            var lib = new ForumLibrary();
            var root = StoryJson.Parse(json) as Dictionary<string, object> ?? throw new FormatException("forum: not an object");
            if (root.TryGetValue("users", out var u) && u is Dictionary<string, object> users)
                foreach (var kv in users)
                {
                    var o = (Dictionary<string, object>)kv.Value;
                    var user = new ForumUser { id = kv.Key, name = S(o, "name"), nameEn = S(o, "nameEn"), sign = S(o, "sign"), signEn = S(o, "signEn"), level = (int)N(o, "level", 1) };
                    // Chinese is the source; a missing English falls back to it (English is not maintained for now).
                    if (user.name.Length == 0) throw new FormatException("forum: user " + kv.Key + " needs a name");
                    if (user.nameEn.Length == 0) user.nameEn = user.name;
                    if (user.signEn.Length == 0) user.signEn = user.sign;
                    lib.Users[kv.Key] = user;
                }
            if (root.TryGetValue("boards", out var b) && b is List<object> boards)
                foreach (Dictionary<string, object> o in boards) lib.Boards.Add(new ForumBoard { id = S(o, "id"), name = S(o, "name"), nameEn = S(o, "nameEn") });
            var ids = new HashSet<string>();
            if (root.TryGetValue("threads", out var t) && t is List<object> threads)
                foreach (Dictionary<string, object> o in threads)
                {
                    var thread = new ForumThread
                    {
                        id = S(o, "id"), board = S(o, "board"), author = S(o, "author"), title = S(o, "title"), titleEn = S(o, "titleEn"),
                        date = D(o, "date"), minStage = (int)N(o, "minStage", 0), maxStage = (int)N(o, "maxStage", 99),
                        mine = B(o, "mine"), dead = B(o, "dead"), helpPost = B(o, "helpPost"), requires = S(o, "requires"),
                    };
                    if (!ids.Add(thread.id)) throw new FormatException("forum: repeated thread " + thread.id);
                    if (thread.title.Length == 0) throw new FormatException("forum: " + thread.id + " needs a title");
                    if (thread.titleEn.Length == 0) thread.titleEn = thread.title;
                    if (!lib.Users.ContainsKey(thread.author)) throw new FormatException("forum: " + thread.id + " has an unknown author");
                    if (!lib.Boards.Exists(x => x.id == thread.board)) throw new FormatException("forum: " + thread.id + " is on an unknown board");
                    if (o.TryGetValue("floors", out var f) && f is List<object> floors)
                        foreach (Dictionary<string, object> fo in floors)
                        {
                            var floor = new ForumFloor
                            {
                                author = S(fo, "author"), text = S(fo, "text"), textEn = S(fo, "textEn"), since = fo.ContainsKey("since") ? D(fo, "since") : DateTime.MinValue,
                                minStage = (int)N(fo, "minStage", 0), deletedFromStage = (int)N(fo, "deletedFromStage", 0), afterSeconds = N(fo, "afterSeconds", 0), folded = B(fo, "folded"),
                            };
                            if (!lib.Users.ContainsKey(floor.author)) throw new FormatException("forum: unknown floor author in " + thread.id);
                            if (floor.text.Length == 0) throw new FormatException("forum: a floor of " + thread.id + " needs text");
                            if (floor.textEn.Length == 0) floor.textEn = floor.text;
                            thread.floors.Add(floor);
                        }
                    lib.Threads.Add(thread);
                }
            return lib;
        }

        static string S(Dictionary<string, object> o, string k) => o.TryGetValue(k, out var v) && v is string s ? s : "";
        static double N(Dictionary<string, object> o, string k, double fallback) => o.TryGetValue(k, out var v) && v is double d ? d : fallback;
        static bool B(Dictionary<string, object> o, string k) => o.TryGetValue(k, out var v) && v is bool b && b;
        static DateTime D(Dictionary<string, object> o, string k)
        {
            if (DateTime.TryParseExact(S(o, k), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d;
            throw new FormatException("forum: bad date " + k + " = " + S(o, k));
        }

        public ForumThread Get(string id) { foreach (var t in Threads) if (t.id == id) return t; return null; }

        /// <summary>A thread is listed once its date has come, inside its stage window, once its flag is set and (help post) once posted.</summary>
        public static bool Visible(ForumThread t, ForumContext c)
        {
            if (t.helpPost && !c.helpPosted) return false;
            if (t.requires.Length > 0 && (c.flag == null || !c.flag(t.requires))) return false;
            return t.date <= c.today.Date && c.stage >= t.minStage && c.stage <= t.maxStage;
        }

        /// <summary>How a floor shows today. 第 7 楼 goes from the long number to 「该楼层已被删除」 at stage 2 (§10.3).</summary>
        public static FloorView View(ForumFloor f, ForumContext c)
        {
            if (f.since > c.today.Date || c.stage < f.minStage) return FloorView.Hidden;
            if (f.afterSeconds > 0 && (!c.helpPosted || c.secondsSinceHelp < f.afterSeconds)) return FloorView.Hidden;
            if (f.deletedFromStage > 0 && c.stage >= f.deletedFromStage) return FloorView.Deleted;
            return f.folded ? FloorView.Folded : FloorView.Shown;
        }

        public List<ForumThread> Board(string board, ForumContext c)
        {
            var list = new List<ForumThread>();
            foreach (var t in Threads) if (t.board == board && !t.mine && Visible(t, c)) list.Add(t);
            list.Sort((a, b) => b.date.CompareTo(a.date));
            return list;
        }

        public List<ForumThread> Mine(ForumContext c)
        {
            var list = new List<ForumThread>();
            foreach (var t in Threads) if (t.mine && Visible(t, c)) list.Add(t);
            list.Sort((a, b) => b.date.CompareTo(a.date));
            return list;
        }

        public List<ForumThread> By(string author, ForumContext c)
        {
            var list = new List<ForumThread>();
            foreach (var t in Threads) if (t.author == author && Visible(t, c)) list.Add(t);
            list.Sort((a, b) => b.date.CompareTo(a.date));
            return list;
        }
    }

    // ───────────── saved forum state (GameState.forumState as JSON) ─────────────

    [Serializable]
    public sealed class ForumMessage
    {
        public string from = "", text = "";
        public double gameSeconds;
    }

    [Serializable]
    public sealed class ForumConversation
    {
        public string id = "";
        public int unread;
        public List<ForumMessage> messages = new List<ForumMessage>();
    }

    [Serializable]
    public sealed class ForumState
    {
        public int version = 1;
        public List<ForumConversation> conversations = new List<ForumConversation>();
        /// <summary>Topics already asked of 老周 ("topic#count"): the first answer gives a direction, a repeat explains.</summary>
        public List<string> asked = new List<string>();
        public bool helpPosted;
        public double helpPostedAt;
        /// <summary>The player has written to 周而复始_ (current-timeline 老周).</summary>
        public bool metZhouNow;
        /// <summary>The ending has reached the forum (design v1.1 §8 E-5): 周而复始's last message, or his account closed.</summary>
        public bool endingHandled, zhouGone;

        public ForumConversation Conversation(string id)
        {
            foreach (var c in conversations) if (c.id == id) return c;
            var created = new ForumConversation { id = id };
            conversations.Add(created);
            return created;
        }

        public int Asked(string topic)
        {
            foreach (var a in asked) if (a.StartsWith(topic + "#", StringComparison.Ordinal) && int.TryParse(a.Substring(topic.Length + 1), out int n)) return n;
            return 0;
        }

        public int Ask(string topic)
        {
            int n = Asked(topic) + 1;
            asked.RemoveAll(a => a.StartsWith(topic + "#", StringComparison.Ordinal));
            asked.Add(topic + "#" + n);
            return n;
        }
    }
}
