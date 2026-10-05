using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LingGuangV05.Core.Chat;

namespace LingGuangV05.XingGuang
{
    /// <summary>One note in its memory book: a short fact, who it is about, how much it matters and when it was used.</summary>
    [Serializable]
    public sealed class XgMemoryEntry
    {
        public int id;
        public string text = "";
        /// <summary>One of <see cref="XgSim.MemorySubjects"/> (主人, 晴雯, 老周, 它自己, 世界).</summary>
        public string subject = "";
        /// <summary>1 ordinary, 2 important, 3 very important (promises, birthdays, exams).</summary>
        public int importance = 1;
        /// <summary>Calendar day it was written (yyyymmdd, the 2016 calendar); 0 until the date is known.</summary>
        public int createdDay;
        public double createdAt;
        public double lastUsedAt = -1;
        public int lastUsedDay;
        public int uses;
    }

    public enum XgMemoryOpKind { None, Add, Update, Delete }

    /// <summary>One change the extractor asks for (the mem0-style ADD / UPDATE / DELETE / NONE).</summary>
    public sealed class XgMemoryOp
    {
        public XgMemoryOpKind kind;
        public int id;
        public string text = "", subject = "";
        public int importance = 1;
    }

    /// <summary>What a write did to the book, for the UI and tests.</summary>
    public sealed class XgMemoryChange
    {
        public readonly List<XgMemoryEntry> added = new List<XgMemoryEntry>(), updated = new List<XgMemoryEntry>(), deleted = new List<XgMemoryEntry>(), evicted = new List<XgMemoryEntry>();
        public bool Any => added.Count + updated.Count + deleted.Count > 0;
    }

    public sealed partial class XgState
    {
        /// <summary>Its memory book (stage 5+), authoritative: the 「笔记.txt」 notebook is only a mirror of it.</summary>
        public List<XgMemoryEntry> memoryBook = new List<XgMemoryEntry>();
        public int nextMemoryId = 1;
        /// <summary>The old one-line notes in <see cref="memory"/> have been moved into the book.</summary>
        public bool memoryMigrated;
        /// <summary>Ids of the notes it recalled for its last answer on the 对话 page.</summary>
        public List<int> memoryRecalled = new List<int>();
        public string memoryRecallQuery = "";
        /// <summary>Bumped on every change to the book, so the notebook file is rewritten only when needed.</summary>
        public int memoryRevision;
    }

    /// <summary>
    /// The memory book (stage 5+). Important things from the conversation are kept as short notes; before it answers,
    /// the notes most similar to what was just said (BM25, <see cref="LingGuangV05.Core.Chat.Bm25Scorer"/>) go into its
    /// prompt as 「你记得：…」.
    ///
    /// Writing follows the memory update loop of the open-source mem0 project (github.com/mem0ai/mem0, Apache-2.0):
    /// after an exchange, a model call sees the exchange and the most similar existing notes and answers with
    /// ADD / UPDATE / DELETE / NONE operations, which code then checks and applies. This is our own C# version of that
    /// idea; no mem0 source or prompt text is copied. Without a model, a simple cue rule keeps the lines that sound
    /// like plans, preferences or promises.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary><see cref="MemoryTextLimit"/> characters for a Chinese note; English notes, without CJK, get <see cref="MemoryTextLimitEn"/>.</summary>
        public const int MemoryBookLimit = 200, MemoryTextLimit = 30, MemoryTextLimitEn = 64, MemoryRecallCount = 5, MemoryExtractOps = 2, MemoryFirstStage = 5;
        public static readonly string[] MemorySubjects = { "主人", "晴雯", "老周", "它自己", "世界" };
        public static readonly string[] MemorySubjectsEn = { "owner", "Qingwen", "Lao Zhou", "itself", "world" };

        /// <summary>The similarity seam: BM25 today, an embedding scorer later.</summary>
        public IMemoryScorer MemoryScorer = new Bm25Scorer();

        public List<XgMemoryEntry> MemoryBook => S.memoryBook;
        public bool MemoryOpen => S.stage >= MemoryFirstStage;

        void RepairMemoryBook()
        {
            if (S.memoryBook == null) S.memoryBook = new List<XgMemoryEntry>();
            if (S.memoryRecalled == null) S.memoryRecalled = new List<int>();
            if (S.memoryRecallQuery == null) S.memoryRecallQuery = "";
            if (S.memory == null) S.memory = new List<string>();
            S.memoryBook.RemoveAll(e => e == null || string.IsNullOrWhiteSpace(e.text));
            foreach (var e in S.memoryBook)
            {
                if (e.subject == null || Array.IndexOf(MemorySubjects, e.subject) < 0) e.subject = MemorySubjects[0];
                e.importance = Math.Max(1, Math.Min(3, e.importance));
                if (e.id >= S.nextMemoryId) S.nextMemoryId = e.id + 1;
            }
            if (S.nextMemoryId < 1) S.nextMemoryId = 1;
            if (!S.memoryMigrated)
            {
                // The old long memory: 「你说过：…」 lines about what the owner said, oldest first.
                foreach (var line in S.memory)
                {
                    string note = (line ?? "").Trim();
                    foreach (var prefix in new[] { "你说过：", "You said: " })
                        if (note.StartsWith(prefix, StringComparison.Ordinal)) note = (prefix == "你说过：" ? "主人说：" : "Owner said: ") + note.Substring(prefix.Length);
                    note = CleanMemoryText(note);
                    if (note.Length == 0 || FindSame(note, MemorySubjects[0]) != null) continue;
                    S.memoryBook.Add(new XgMemoryEntry { id = S.nextMemoryId++, text = note, subject = MemorySubjects[0], importance = 1, createdAt = Clock });
                }
                S.memory.Clear();
                S.memoryMigrated = true;
                S.memoryRevision++;
            }
            EvictMemories(null);
        }

        // ───────────── dates ─────────────

        static int DayNumber(int yyyymmdd)
        {
            if (yyyymmdd <= 0) return 0;
            try { return (int)(new DateTime(yyyymmdd / 10000, Math.Max(1, yyyymmdd / 100 % 100), Math.Max(1, yyyymmdd % 100)) - new DateTime(2016, 1, 1)).TotalDays; }
            catch (ArgumentOutOfRangeException) { return 0; }
        }

        /// <summary>Notes written before the calendar was known (migrated ones) take today's date once it is.</summary>
        void DateUndatedMemories()
        {
            if (!TodayKnown) return;
            foreach (var e in S.memoryBook) if (e.createdDay <= 0) { e.createdDay = Today; S.memoryRevision++; }
        }

        double MemoryAgeDays(XgMemoryEntry e)
        {
            int last = Math.Max(e.createdDay, e.lastUsedDay);
            if (last <= 0) return 0;
            return Math.Max(0, DayNumber(Today) - DayNumber(last));
        }

        /// <summary>How much a note is worth keeping: importance × use, fading with the days since it was written or used.</summary>
        public double MemoryKeepScore(XgMemoryEntry e) => e.importance * (1 + .1 * Math.Min(10, e.uses)) / (1 + MemoryAgeDays(e) / 30);

        // ───────────── writing ─────────────

        /// <summary>A note as stored: one line, no quotes, at most <see cref="MemoryTextLimit"/> characters.</summary>
        public static string CleanMemoryText(string text)
        {
            string t = Regex.Replace(text ?? "", "\\s+", " ").Trim().Trim('"', '「', '」', '“', '”', '\'').Trim();
            t = t.TrimEnd('。', '.', '，', ',', '；', ';', '！', '!');
            int limit = MemoryTextLimit;
            if (Regex.IsMatch(t, "^[^\u3400-\u9fff]*$")) limit = MemoryTextLimitEn;
            if (t.Length > limit) t = t.Substring(0, limit - 1) + "…";
            return t;
        }

        static readonly string[] NotAFact = { "无值得", "没有值得", "无需记", "不需要记", "没什么值得", "无内容", "nothing worth", "nothing to remember", "no memory" };

        public static string NormalSubject(string subject)
        {
            string s = (subject ?? "").Trim();
            int i = Array.IndexOf(MemorySubjects, s);
            if (i < 0) for (int k = 0; k < MemorySubjectsEn.Length; k++) if (string.Equals(MemorySubjectsEn[k], s, StringComparison.OrdinalIgnoreCase)) i = k;
            return MemorySubjects[Math.Max(0, i)];
        }

        public string SubjectName(string subject)
        {
            int i = Math.Max(0, Array.IndexOf(MemorySubjects, subject));
            return English ? MemorySubjectsEn[i] : MemorySubjects[i];
        }

        public XgMemoryEntry MemoryEntry(int id) { foreach (var e in S.memoryBook) if (e.id == id) return e; return null; }

        XgMemoryEntry FindSame(string text, string subject)
        {
            string a = XgSpeechPolicy.Normalize(text);
            foreach (var e in S.memoryBook)
            {
                if (e.subject != subject) continue;
                string b = XgSpeechPolicy.Normalize(e.text);
                if (a == b || a.Length >= 4 && b.Length >= 4 && (a.Contains(b) || b.Contains(a)) || XgSpeechPolicy.Similar(text, e.text)) return e;
            }
            return null;
        }

        static readonly string[] PromiseCues = { "答应", "保证", "约定", "说好", "promise", "promised" };

        static int Importance(int asked, string text)
        {
            int i = Math.Max(1, Math.Min(3, asked));
            foreach (var c in PromiseCues) if (text.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0) return Math.Max(i, 2);
            return i;
        }

        /// <summary>
        /// Applies at most <see cref="MemoryExtractOps"/> operations. UPDATE and DELETE must name a note the extractor was
        /// shown (<paramref name="shown"/>; null allows any). An ADD that repeats an existing note refreshes it instead.
        /// The book is then trimmed to <see cref="MemoryBookLimit"/>.
        /// </summary>
        public XgMemoryChange ApplyMemoryOps(IList<XgMemoryOp> ops, ICollection<int> shown = null)
        {
            var change = new XgMemoryChange();
            if (ops == null) return change;
            DateUndatedMemories();
            int applied = 0;
            foreach (var op in ops)
            {
                if (op == null || op.kind == XgMemoryOpKind.None) continue;
                if (applied >= MemoryExtractOps) break;
                string text = CleanMemoryText(op.text);
                string subject = NormalSubject(op.subject);
                bool fact = text.Length >= 3;
                foreach (var n in NotAFact) if (text.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0) fact = false;
                switch (op.kind)
                {
                    case XgMemoryOpKind.Add:
                    {
                        if (!fact) continue;
                        var same = FindSame(text, subject);
                        if (same != null) { Rewrite(same, text, subject, Math.Max(same.importance, Importance(op.importance, text))); change.updated.Add(same); }
                        else
                        {
                            var e = new XgMemoryEntry { id = S.nextMemoryId++, text = text, subject = subject, importance = Importance(op.importance, text), createdDay = TodayKnown ? Today : 0, createdAt = Clock };
                            S.memoryBook.Add(e);
                            change.added.Add(e);
                        }
                        break;
                    }
                    case XgMemoryOpKind.Update:
                    {
                        var e = MemoryEntry(op.id);
                        if (e == null || !fact || shown != null && !shown.Contains(op.id)) continue;
                        Rewrite(e, text, subject, Importance(op.importance, text));
                        change.updated.Add(e);
                        break;
                    }
                    case XgMemoryOpKind.Delete:
                    {
                        var e = MemoryEntry(op.id);
                        if (e == null || shown != null && !shown.Contains(op.id)) continue;
                        S.memoryBook.Remove(e);
                        S.memoryRecalled.Remove(e.id);
                        change.deleted.Add(e);
                        break;
                    }
                }
                applied++;
            }
            EvictMemories(change);
            if (change.Any || change.evicted.Count > 0) S.memoryRevision++;
            return change;
        }

        void Rewrite(XgMemoryEntry e, string text, string subject, int importance)
        {
            e.text = text; e.subject = subject; e.importance = Math.Max(1, Math.Min(3, importance));
            // A rewritten note is dated by its rewrite: the notebook shows when it last changed.
            if (TodayKnown) e.createdDay = Today;
            e.createdAt = Clock;
        }

        void EvictMemories(XgMemoryChange change)
        {
            while (S.memoryBook.Count > MemoryBookLimit)
            {
                XgMemoryEntry worst = null; double low = double.MaxValue;
                foreach (var e in S.memoryBook)
                {
                    double k = MemoryKeepScore(e);
                    if (k < low || k == low && worst != null && e.id < worst.id) { low = k; worst = e; }
                }
                S.memoryBook.Remove(worst);
                S.memoryRecalled.Remove(worst.id);
                change?.evicted.Add(worst);
            }
        }

        /// <summary>Simple cues that a line is worth keeping without a model: plans, dates, likes, promises, worries.</summary>
        public static readonly string[] MemoryCues =
        {
            "要", "想", "会", "明天", "后天", "下周", "下个月", "周末", "暑假", "喜欢", "讨厌", "答应", "生日", "考试", "打算", "准备", "约", "害怕", "担心", "最爱", "面试", "以后",
            "tomorrow", "next week", "weekend", "like", "love", "hate", "promise", "birthday", "exam", "going to", "want", "plan", "afraid", "worried", "interview",
        };
        static readonly string[] StrongCues = { "答应", "生日", "考试", "面试", "promise", "birthday", "exam", "interview" };

        /// <summary>
        /// The offline rule (no model, or the model was busy): a line of the owner's of at least 6 characters that
        /// carries a cue becomes 「主人说：…」. Questions to it are skipped: 「你会说话吗」 is not a plan.
        /// </summary>
        public XgMemoryChange RememberOffline(string playerLine)
        {
            var none = new XgMemoryChange();
            string line = (playerLine ?? "").Trim();
            if (!MemoryOpen || line.Length < 6) return none;
            char last = line[line.Length - 1];
            if (last == '？' || last == '?' || line.EndsWith("吗", StringComparison.Ordinal) || line.EndsWith("呢", StringComparison.Ordinal)) return none;
            bool cue = false, strong = false;
            foreach (var c in MemoryCues) if (line.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0) { cue = true; break; }
            foreach (var c in StrongCues) if (line.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0) { strong = true; break; }
            if (!cue && !strong) return none;
            var op = new XgMemoryOp { kind = XgMemoryOpKind.Add, text = T("主人说：", "Owner said: ") + line, subject = MemorySubjects[0], importance = strong ? 2 : 1 };
            return ApplyMemoryOps(new[] { op });
        }

        // ───────────── recall ─────────────

        /// <summary>
        /// The notes most similar to what was just said (BM25 plus small importance and recency boosts), at most
        /// <paramref name="count"/>, nothing irrelevant. With <paramref name="touch"/> each one's use count and last use
        /// are updated (the same query twice, as on a retry, counts once). With <paramref name="panel"/>
        /// <see cref="XgState.memoryRecalled"/> remembers them for the 对话 page's 「它记得」 panel.
        /// </summary>
        public List<XgMemoryEntry> Recall(string latest, string previous = null, int count = MemoryRecallCount, bool touch = true, bool panel = true)
        {
            var found = new List<XgMemoryEntry>();
            if (!MemoryOpen || S.memoryBook.Count == 0 || string.IsNullOrWhiteSpace(latest) && string.IsNullOrWhiteSpace(previous)) return found;
            DateUndatedMemories();
            var docs = new List<string>(); var importance = new List<int>(); var ages = new List<double>();
            foreach (var e in S.memoryBook) { docs.Add(e.text); importance.Add(e.importance); ages.Add(MemoryAgeDays(e)); }
            foreach (int i in MemoryRanker.Rank(MemoryScorer, MemoryQuery.Of(latest, previous), docs, count, importance, ages)) found.Add(S.memoryBook[i]);
            if (touch)
            {
                string key = (latest ?? "") + "\n" + (previous ?? "");
                if (key != S.memoryRecallQuery)
                {
                    S.memoryRecallQuery = key;
                    foreach (var e in found) { e.uses++; e.lastUsedAt = Clock; e.lastUsedDay = TodayKnown ? Today : e.lastUsedDay; }
                }
            }
            if (touch && panel)
            {
                S.memoryRecalled.Clear();
                foreach (var e in found) S.memoryRecalled.Add(e.id);
            }
            return found;
        }

        /// <summary>The owner's last two lines on the 对话 page (newest first), the default recall query.</summary>
        void LastOwnerLines(out string latest, out string previous)
        {
            latest = previous = null;
            for (int i = S.chat.Count - 1; i >= 0; i--)
            {
                if (S.chat[i].from != "me") continue;
                if (latest == null) latest = S.chat[i].text;
                else { previous = S.chat[i].text; break; }
            }
        }

        /// <summary>「你记得：a；b。」 for a prompt, or "" when nothing relevant came back.</summary>
        public string MemoryPromptLine(IList<XgMemoryEntry> notes)
        {
            if (notes == null || notes.Count == 0) return "";
            var parts = new List<string>();
            foreach (var e in notes) parts.Add(e.text);
            return T("你记得：", "You remember: ") + string.Join(T("；", "; "), parts) + T("。", ". ");
        }

        /// <summary>Recall for a prompt from the given lines (the YY contact): use counts update, the 对话 page's panel does not.</summary>
        public string MemoryPromptFor(string latest, string previous, bool touch = true)
        {
            if (!MemoryOpen) return "";
            return MemoryPromptLine(Recall(latest, previous, MemoryRecallCount, touch, false));
        }

        /// <summary>The notes it recalled for its last 对话 answer (for the 「它记得」 panel).</summary>
        public List<XgMemoryEntry> RecalledMemories()
        {
            var list = new List<XgMemoryEntry>();
            foreach (int id in S.memoryRecalled) { var e = MemoryEntry(id); if (e != null) list.Add(e); }
            return list;
        }

        /// <summary>The notes that matter most (importance, then newest), for the finale's summary.</summary>
        public List<string> MemoryHighlights(int count)
        {
            var all = new List<XgMemoryEntry>(S.memoryBook);
            all.Sort((a, b) => a.importance != b.importance ? b.importance.CompareTo(a.importance) : b.id.CompareTo(a.id));
            var list = new List<string>();
            for (int i = 0; i < all.Count && list.Count < count; i++) list.Add(all[i].text);
            return list;
        }

        /// <summary>The newest note about the owner that is not the line it is answering, without the 「主人说：」 prefix.</summary>
        string LatestOwnerNote(string current)
        {
            for (int i = S.memoryBook.Count - 1; i >= 0; i--)
            {
                var e = S.memoryBook[i];
                if (e.subject != MemorySubjects[0]) continue;
                string note = e.text;
                int colon = Math.Max(note.IndexOf('：'), note.IndexOf(": ", StringComparison.Ordinal));
                if (colon >= 0 && colon <= 12) note = note.Substring(colon + 1).Trim();
                if (note.Length < 2 || !string.IsNullOrEmpty(current) && current.Contains(note.TrimEnd('…'))) continue;
                return note;
            }
            return "";
        }

        // ───────────── the extractor (model) ─────────────

        public string MemoryAiName => Profile.name.Length > 0 ? Profile.name : (English ? LingGuangV05.Core.AppNames.AiEn : LingGuangV05.Core.AppNames.AiZh);

        /// <summary>
        /// The request for the background extractor: the system prompt with the most similar existing notes (by id) and
        /// the last exchange as the user turn. <paramref name="shown"/> is the ids it may UPDATE or DELETE.
        /// </summary>
        public List<KeyValuePair<string, string>> MemoryExtractionMessages(string playerLine, string aiLine, out List<int> shown)
        {
            shown = new List<int>();
            var similar = Recall(playerLine, aiLine, MemoryRecallCount, false);
            var book = new StringBuilder();
            foreach (var e in similar) { shown.Add(e.id); book.Append('[').Append(e.id).Append("] ").Append(e.text).Append('\n'); }
            string ai = MemoryAiName;
            string system = English ? ExtractorEn(ai, book.Length > 0 ? book.ToString() : "(empty)\n") : ExtractorZh(ai, book.Length > 0 ? book.ToString() : "（空）\n");
            string user = T("主人：", "Owner: ") + (playerLine ?? "").Trim() + "\n" + ai + T("：", ": ") + (aiLine ?? "").Trim();
            return new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", system), new KeyValuePair<string, string>("user", user) };
        }

        static string ExtractorZh(string ai, string book)
        {
            return "你在整理" + ai + "的笔记本。" + ai + "是一个在 2016 年旧电脑里被训练出来的小程序，它的主人在和它聊天。读最后一轮对话，决定笔记本要不要改。\n" +
                "只记值得长期记住的事：主人的计划和日子（考试、生日、约会、出门）、喜欢和讨厌的东西、答应的事和约定、人名和关系、主人对某件事的感受。打招呼、闲聊、玩笑、天气、问它问题、让它训练或答题，都不记。\n" +
                "现有笔记（编号 内容）：\n" + book +
                "怎么改：\n" +
                "- 新的事，笔记里没有：ADD，id 留空。\n" +
                "- 和某条笔记是同一件事，但有了新信息或变了：UPDATE，写那条的 id 和改好的完整内容。\n" +
                "- 对话说明某条笔记已经不对了：DELETE，写那条的 id。\n" +
                "- 没有值得记的：只输出一个 NONE。\n" +
                "- 最多两条。text 不超过 30 个字，用第三人称，主人写成「主人」，不要写「你」「我」。\n" +
                "- 写清是谁的事：晴雯想要的东西写「晴雯想要……」，subject 填晴雯；老周的事 subject 填老周；" + ai + "答应主人的事写「" + ai + "答应主人……」，subject 填它自己（主人说「答应我……」，就是" + ai + "答应了主人）。\n" +
                "- subject 只能是：主人、晴雯、老周、它自己、世界。importance：1 一般，2 重要，3 很重要（答应的事、生日、考试、大事）。\n" +
                "只输出 JSON。";
        }

        static string ExtractorEn(string ai, string book)
        {
            return "You keep " + ai + "'s notebook. " + ai + " is a small program trained on an old 2016 computer; its owner is chatting with it. Read the last exchange and decide whether the notebook should change.\n" +
                "Only keep things worth remembering for a long time: the owner's plans and dates (exams, birthdays, dates, trips), likes and dislikes, promises and arrangements, names and relationships, how the owner feels about something. Do not keep greetings, small talk, jokes, weather, questions to it, or requests to train or answer.\n" +
                "Existing notes (id text):\n" + book +
                "How to change it:\n" +
                "- Something new that is not in the notes: ADD, leave id empty.\n" +
                "- The same thing as a note, with new information or changed: UPDATE with that note's id and the full rewritten text.\n" +
                "- The exchange shows a note is no longer true: DELETE with that note's id.\n" +
                "- Nothing worth keeping: output a single NONE.\n" +
                "- At most two. text at most 60 characters, third person, call the owner \"owner\", never \"you\" or \"I\".\n" +
                "- Say whose it is: what Qingwen wants is \"Qingwen wants ...\" with subject Qingwen; Lao Zhou's things have subject Lao Zhou; what " + ai + " promised is \"" + ai + " promised the owner ...\" with subject itself (\"promise me ...\" from the owner means " + ai + " promised).\n" +
                "- subject is one of: owner, Qingwen, Lao Zhou, itself, world. importance: 1 ordinary, 2 important, 3 very important (promises, birthdays, exams, big events).\n" +
                "Output only JSON.";
        }

        /// <summary>The GBNF grammar for the extractor's answer: {"ops":[…]} with one or two operations.</summary>
        public static string MemoryGrammar(bool english)
        {
            string subjects = english ? "\"owner\" | \"Qingwen\" | \"Lao Zhou\" | \"itself\" | \"world\"" : "\"主人\" | \"晴雯\" | \"老周\" | \"它自己\" | \"世界\"";
            return "root ::= \"{\\\"ops\\\":[\" op (\",\" op)? \"]}\"\n" +
                   "op ::= \"{\\\"op\\\":\\\"\" kind \"\\\",\\\"id\\\":\\\"\" id \"\\\",\\\"text\\\":\\\"\" text \"\\\",\\\"subject\\\":\\\"\" subject \"\\\",\\\"importance\\\":\" imp \"}\"\n" +
                   "kind ::= \"ADD\" | \"UPDATE\" | \"DELETE\" | \"NONE\"\n" +
                   "id ::= [0-9]{0,5}\n" +
                   "text ::= [^\"\\\\\\n]{0," + (english ? 72 : 36) + "}\n" +
                   "subject ::= " + subjects + "\n" +
                   "imp ::= [1-3]\n";
        }

        static readonly Regex OpObject = new Regex("\\{[^{}]*\\}", RegexOptions.Compiled);
        static readonly Regex StringField = new Regex("\"(op|id|text|subject)\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Compiled);
        static readonly Regex NumberField = new Regex("\"(importance|id)\"\\s*:\\s*(\\d+)", RegexOptions.Compiled);

        /// <summary>Reads the extractor's JSON. Tolerant: unknown fields, missing ones and loose text around it are fine.</summary>
        public static List<XgMemoryOp> ParseMemoryOps(string json)
        {
            var ops = new List<XgMemoryOp>();
            if (string.IsNullOrEmpty(json)) return ops;
            int start = json.IndexOf("\"ops\"", StringComparison.Ordinal);
            string body = start >= 0 ? json.Substring(start) : json;
            foreach (Match m in OpObject.Matches(body))
            {
                var op = new XgMemoryOp();
                bool hasKind = false;
                foreach (Match f in StringField.Matches(m.Value))
                {
                    string value = Unescape(f.Groups[2].Value);
                    switch (f.Groups[1].Value)
                    {
                        case "op":
                            hasKind = true;
                            switch (value.Trim().ToUpperInvariant())
                            {
                                case "ADD": op.kind = XgMemoryOpKind.Add; break;
                                case "UPDATE": op.kind = XgMemoryOpKind.Update; break;
                                case "DELETE": op.kind = XgMemoryOpKind.Delete; break;
                                default: op.kind = XgMemoryOpKind.None; break;
                            }
                            break;
                        case "id": int.TryParse(value.Trim().TrimStart('m', 'M', '#'), NumberStyles.Integer, CultureInfo.InvariantCulture, out op.id); break;
                        case "text": op.text = value; break;
                        case "subject": op.subject = value; break;
                    }
                }
                foreach (Match f in NumberField.Matches(m.Value))
                {
                    int.TryParse(f.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v);
                    if (f.Groups[1].Value == "importance") op.importance = v; else op.id = v;
                }
                if (hasKind) ops.Add(op);
            }
            return ops;
        }

        static string Unescape(string s)
        {
            if (s.IndexOf('\\') < 0) return s;
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c != '\\' || i + 1 >= s.Length) { sb.Append(c); continue; }
                char n = s[++i];
                switch (n)
                {
                    case 'n': sb.Append(' '); break;
                    case 't': sb.Append(' '); break;
                    case 'u':
                        if (i + 4 < s.Length && int.TryParse(s.Substring(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code)) { sb.Append((char)code); i += 4; }
                        break;
                    default: sb.Append(n); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>The extractor's answer arrived: parse and apply it, only touching notes it was shown.</summary>
        public XgMemoryChange RememberFromModel(string json, ICollection<int> shown)
        {
            if (!MemoryOpen) return new XgMemoryChange();
            return ApplyMemoryOps(ParseMemoryOps(json), shown);
        }

        // ───────────── the notebook mirror ─────────────

        public static string MemoryDate(int yyyymmdd) =>
            yyyymmdd <= 0 ? "2016-??-??" : (yyyymmdd / 10000).ToString("0000", CultureInfo.InvariantCulture) + "-" + (yyyymmdd / 100 % 100).ToString("00", CultureInfo.InvariantCulture) + "-" + (yyyymmdd % 100).ToString("00", CultureInfo.InvariantCulture);

        /// <summary>One notebook line: date, whose, the note and its importance as stars.</summary>
        public string NotebookLine(XgMemoryEntry e) => MemoryDate(e.createdDay) + "  [" + SubjectName(e.subject) + "] " + e.text + (e.importance > 1 ? "  " + new string('★', e.importance - 1) : "");

        /// <summary>The whole book as the plain-text notebook 「{AI}的笔记.txt」, oldest first.</summary>
        public string NotebookText()
        {
            DateUndatedMemories();
            var sb = new StringBuilder();
            sb.Append(T(MemoryAiName + "的笔记", MemoryAiName + "'s notes")).Append(T("（共 ", " (")).Append(S.memoryBook.Count).Append(T(" 条，最多 ", " of at most ")).Append(MemoryBookLimit).Append(T(" 条）", ")")).Append('\n');
            sb.Append(T("每行一件事：日期  [关于谁] 内容  ★ 越多越重要", "One thing per line: date  [about whom] note  more ★ = more important")).Append("\n\n");
            if (S.memoryBook.Count == 0) sb.Append(T("（还没有记下什么。）", "(Nothing written down yet.)")).Append('\n');
            foreach (var e in S.memoryBook) sb.Append(NotebookLine(e)).Append('\n');
            return sb.ToString();
        }

        /// <summary>The notebook's file name, safe on Windows and macOS.</summary>
        public string NotebookFileName()
        {
            string name = MemoryAiName;
            var sb = new StringBuilder();
            foreach (char c in name) sb.Append("\\/:*?\"<>|".IndexOf(c) >= 0 || c < 32 ? '_' : c);
            return sb.ToString().Trim() + T("的笔记.txt", " notes.txt");
        }
    }
}
