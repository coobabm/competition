using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LingGuangV05.XingGuang
{
    /// <summary>The opening setup as the lab sees it (design v1.1 §6 Step 8). Set by the desktop from the save.</summary>
    public sealed class XgProfile
    {
        public string name = "", self = "我", callMe = "你", personality = "";
        public double warmth = 50, play = 50, opinion = 50;
        public List<string> words = new List<string>();
    }

    /// <summary>One line of the 对话 page. <see cref="rating"/>: 1 赞, -1 踩, 0 not rated.</summary>
    [Serializable]
    public sealed class XgChatLine
    {
        public string from = "", text = "";
        public double at;
        public int rating;
    }

    /// <summary>Stage gating of its speech (design v1.1 §11.5).</summary>
    public struct XgSpeechLimits
    {
        public int tokens, context, contextMessages;
        public float temperature;
        public bool thinking;
    }

    public sealed partial class XgState
    {
        public List<XgChatLine> chat = new List<XgChatLine>();
        /// <summary>The first long memory: one-line notes, moved into <see cref="memoryBook"/> on load (XgSim.MemoryBook.cs) and then left empty.</summary>
        public List<string> memory = new List<string>();
        public int chatTurns, dontAnswer;
        public double lastChatAt = -1, listeningUntil = -1;
        public bool firstWordsSaid, personaSeeded;
    }

    /// <summary>
    /// The AI's personality (design v1.1 §11.4–11.5): three axes, each a pair of tone elements on the 语气 board
    /// (温度 冷静↔热情, 玩心 正经↔皮, 主见 顺从↔有主见) plus the free tone words from the setup. 赞 / 踩 on a reply
    /// is a card that pulls the elements it used up or down. The stage decides the form of what it says; the board
    /// decides the content (what it knows, believes, mixes up and which memes it uses).
    /// </summary>
    public sealed partial class XgSim
    {
        public const string ToneRegion = "tone";
        public const int MemoryLimit = 20, ChatLimit = 200;
        /// <summary>Stage-4 emergence: it speaks first after this long without a word from you.</summary>
        public const double CallIdleSeconds = 300;
        public const int CallTurns = 3;

        public XgProfile Profile = new XgProfile();

        public static readonly string[] AxisNames = { "温度", "玩心", "主见" };
        public static readonly string[] AxisNamesEn = { "Warmth", "Play", "Opinion" };
        static readonly string[] Low = { "语气:冷静", "语气:正经", "语气:顺从" };
        static readonly string[] High = { "语气:热情", "语气:皮", "语气:有主见" };

        static readonly string[][] HighWords =
        {
            new[] { "！", "!", "哈哈", "好呀", "喜欢", "开心", "嘿", "呀", "啦", "谢谢", "想你" },
            new[] { "233", "哈", "嘿嘿", "皮", "吐槽", "666", "略略", "逗", "滑稽", "doge" },
            new[] { "不对", "我觉得", "不同意", "其实", "才不", "错了", "我认为", "不是这样", "偏要" },
        };
        static readonly string[][] LowWords =
        {
            new[] { "嗯。", "知道了", "是。", "否。", "好。", "收到" },
            new[] { "请", "您", "认真", "首先", "因此", "正确" },
            new[] { "好的", "听你的", "你说得对", "都行", "随你", "是的" },
        };

        public double TargetAxis(int axis) => axis == 0 ? Profile.warmth : axis == 1 ? Profile.play : Profile.opinion;

        double ToneStrength(string element)
        {
            var c = Board.Find(ToneRegion, element);
            return c == null ? 0 : Math.Max(0, c.w) * Math.Max(.1, Math.Min(1, c.s / 3));
        }

        /// <summary>Evidence an axis needs before it leaves the middle: a single tone card should not swing it to an end.</summary>
        public const double AxisPrior = .5;

        /// <summary>
        /// Actual axis value: 50 + 50 × (high − low) / (high + low), from the tone board (§11.4), with a small prior in the
        /// denominator so that one card moves it a little and many cards move it far (play-test: one seed card read as 100).
        /// </summary>
        public double ActualAxis(int axis)
        {
            double hi = ToneStrength(High[axis]), lo = ToneStrength(Low[axis]);
            return Math.Round(50 + 50 * (hi - lo) / (hi + lo + AxisPrior));
        }

        /// <summary>The setup's personality goes onto the board as a starting lean (weak, so the player's cards decide).</summary>
        public void SeedPersonality()
        {
            if (!UseBoard) return;
            for (int axis = 0; axis < 3; axis++)
            {
                double t = TargetAxis(axis);
                if (Math.Abs(t - 50) < 5) continue;
                ToneCard(t > 50 ? High[axis] : Low[axis], true, Math.Abs(t - 50) / 50);
            }
            foreach (var w in Profile.words) ToneCard("语气:" + w, true, .5);
        }

        void ToneCard(string element, bool up, double strength = 1)
        {
            var k = new XgKnobs { depth = 1, width = 64, activation = XgActivation.Sigmoid, lr = .3 * Math.Max(.2, Math.Min(1, strength)), clip = true };
            var card = new XgBoardCard { region = ToneRegion, truth = up };
            card.Add(element);
            Board.Train(card, k);
        }

        /// <summary>The tone elements a reply used (keyword reading; free tone words count when they appear).</summary>
        public List<string> ToneElements(string reply)
        {
            var list = new List<string>();
            reply = reply ?? "";
            for (int axis = 0; axis < 3; axis++)
            {
                foreach (var w in HighWords[axis]) if (reply.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0) { list.Add(High[axis]); break; }
                foreach (var w in LowWords[axis]) if (reply.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0) { list.Add(Low[axis]); break; }
            }
            foreach (var w in Profile.words) if (w.Length > 0 && reply.Contains(w.StartsWith("爱", StringComparison.Ordinal) && w.Length > 1 ? w.Substring(1) : w)) list.Add("语气:" + w);
            return list;
        }

        /// <summary>赞 / 踩 on a reply: one tone card per element it used (§7 stage 4).</summary>
        public bool RateReply(int index, bool up)
        {
            if (index < 0 || index >= S.chat.Count || S.chat[index].from != "ai" || S.chat[index].rating != 0) return false;
            S.chat[index].rating = up ? 1 : -1;
            var elements = ToneElements(S.chat[index].text);
            // A reply with no clear tone still says something about warmth: a plain answer is calm.
            if (elements.Count == 0) elements.Add(Low[0]);
            foreach (var e in elements) ToneCard(e, up);
            return true;
        }

        // ───────────── the conversation ─────────────

        public XgSpeechLimits Limits()
        {
            int stage = Math.Max(1, Math.Min(6, S.stage));
            switch (stage)
            {
                case 1: return new XgSpeechLimits { tokens = 2, context = 256, contextMessages = 1, temperature = .7f };
                case 2: return new XgSpeechLimits { tokens = 8, context = 256, contextMessages = 1, temperature = .7f };
                case 3: return new XgSpeechLimits { tokens = 6, context = 512, contextMessages = 2, temperature = 1.2f };
                case 4: return new XgSpeechLimits { tokens = 48, context = 1024, contextMessages = 8, temperature = .9f };
                case 5: return new XgSpeechLimits { tokens = 128, context = 2048, contextMessages = 12, temperature = .8f };
                default: return new XgSpeechLimits { tokens = S.fullOpen ? 400 : 200, context = 4096, contextMessages = 20, temperature = .8f, thinking = S.fullOpen };
            }
        }

        double BestOf(params string[] datasets) { double best = 0; foreach (var d in datasets) best = Math.Max(best, BestAcc(d)); return best; }

        /// <summary>
        /// Brain gating (§11.5): at stage 1 the yes/no is flipped with probability 1 − logic accuracy, and at stage 2 the
        /// picked option (<see cref="XgSpeechPolicy.Options"/>) is swapped for another with that probability; at stages
        /// 3–4 the words are shuffled with probability 1 − semantic accuracy.
        /// </summary>
        public string Gate(string reply, string question, Random rng)
        {
            reply = (reply ?? "").Trim();
            int stage = Math.Max(1, Math.Min(6, S.stage));
            if (stage == 1)
            {
                double logic = Math.Max(.5, BestOf("logic", "spam", "xor"));
                bool yes = reply.StartsWith("是", StringComparison.Ordinal) || reply.StartsWith("yes", StringComparison.OrdinalIgnoreCase);
                if (rng.NextDouble() > logic) yes = !yes;
                return XgSpeechPolicy.Constrain(yes ? (English ? "Yes." : "是。") : (English ? "No." : "否。"), stage, English);
            }
            if (stage == 2)
            {
                double logic = Math.Max(.5, BestOf("logic", "spam", "xor"));
                var options = XgSpeechPolicy.Options(question, 2, English);
                string picked = XgSpeechPolicy.Choose(reply, options, English);
                if (rng.NextDouble() > logic)
                {
                    // The wrong answer: any other option.
                    var others = new List<string>();
                    foreach (var o in options) { string a = XgSpeechPolicy.Answer(o, English); if (a != picked) others.Add(a); }
                    if (others.Count > 0) picked = others[rng.Next(others.Count)];
                }
                return picked;
            }
            reply = XgSpeechPolicy.Constrain(reply, stage, English);
            if (stage <= 4)
            {
                double semantic = Math.Max(.5, BestOf("danmu", "longtext", "poems", "headline", "translate"));
                if (rng.NextDouble() > semantic) reply = Shuffle(reply, rng);
                if (stage == 4 && reply.Length > 24) reply = reply.Substring(0, 24) + "…";
            }
            return reply;
        }

        static string Shuffle(string text, Random rng)
        {
            bool spaced = text.IndexOf(' ') > 0;
            // Word order needs words: a single English word is left alone rather than spelt wrong.
            if (!spaced && !HasCjk(text)) return text;
            var parts = new List<string>(spaced ? text.Split(' ') : SplitChars(text));
            if (parts.Count < 3) return text;
            int i = 1 + rng.Next(parts.Count - 2);
            var tmp = parts[i]; parts[i] = parts[i - 1]; parts[i - 1] = tmp;
            return string.Join(spaced ? " " : "", parts);
        }

        static bool HasCjk(string text)
        {
            foreach (char c in text) if (c >= 0x3400 && c <= 0x9FFF) return true;
            return false;
        }

        static IEnumerable<string> SplitChars(string text)
        {
            var e = StringInfo.GetTextElementEnumerator(text);
            while (e.MoveNext()) yield return e.GetTextElement();
        }

        /// <summary>Replies that need no model: the stage-4 first words and the Three-Body eggs (§10.2 #5, #12).</summary>
        public string Scripted(string question)
        {
            string q = (question ?? "").Trim();
            if (S.listeningUntil > Clock) return "……";
            if (S.stage >= 4 && (q.Contains("不要回答") || q.IndexOf("don't answer", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                if (++S.dontAnswer >= 3) { S.dontAnswer = 0; S.listeningUntil = Clock + 60; return T("……好。", "… All right."); }
                return null;
            }
            S.dontAnswer = 0;
            // 「你是怎么被训练出来的？」: held to its stage before the ending, the curtain call after it.
            if (IsOriginQuestion(q)) return OriginReply();
            // 「她……爱我吗？」 (XgSim.Love.cs): consent, cooldown or the verdict cutscene.
            if (IsLoveQuestion(q)) return LoveReply();
            if (S.stage >= 4 && (q.Contains("虫子") || q.IndexOf("bug", StringComparison.OrdinalIgnoreCase) >= 0 && q.Length < 20))
                return T("虫子从来没有被真正战胜过。", "The bugs have never truly been defeated.");
            return null;
        }

        public bool Listening => S.listeningUntil > Clock;

        /// <summary>The fixed first line when it can first talk (stage 4): 「{称呼}……{自称}是{名字}？」</summary>
        public string FirstWords() => T(Profile.callMe + "……" + Profile.self + "是" + Profile.name + "？", Profile.callMe + "… " + Profile.self + " am " + Profile.name + "?");

        public void AddLine(string from, string text)
        {
            S.chat.Add(new XgChatLine { from = from, text = text ?? "", at = Clock });
            if (S.chat.Count > ChatLimit) S.chat.RemoveRange(0, S.chat.Count - ChatLimit);
            if (from != "me") return;
            S.chatTurns++; S.lastChatAt = Clock;
            // The way it is addressed grows on the tone board (stage 4's emergence reads it).
            if (UseBoard && Profile.callMe.Length > 0) ToneCard("称呼:" + Profile.callMe, true, .6);
            // Long memory (stage 5+) is written after it answers, by the memory book (XgSim.MemoryBook.cs).
        }

        /// <summary>Stage 4 emergence (§7): the 称呼 concept is strong and you have not talked to it for a while.</summary>
        void CheckCallEmergence()
        {
            if (S.stage != 4 || HasEmerged(4) || S.chatTurns < CallTurns || S.lastChatAt < 0 || Clock - S.lastChatAt < CallIdleSeconds) return;
            var call = Board.Find(ToneRegion, "称呼:" + Profile.callMe);
            if (call == null || call.w < .5) return;
            Emerge(4);
            AddLine("ai", EmergenceLine(4));
        }

        string StrongestTone()
        {
            string best = ""; double strength = 0;
            for (int axis = 0; axis < 3; axis++)
                foreach (var e in new[] { High[axis], Low[axis] }) { double s = ToneStrength(e); if (s > strength) { strength = s; best = e.Substring(3); } }
            return best;
        }

        /// <summary>The system prompt with the board injected (§11.5): <see cref="PersonaCore"/>, <see cref="PersonaBoard"/>, then the recalled memories.</summary>
        /// <param name="query">
        /// What to recall memories for. Null: the owner's last two lines on the 对话 page, and the recall is shown in
        /// its 「它记得」 panel. Given (e.g. her message for 「让它代我回」): recalled for that text, the panel is left alone.
        /// </param>
        public string PersonaPrompt(int month, IList<string> hotWords, string query = null) =>
            PersonaCore() + PersonaBoard(month, hotWords) + PersonaTone() + PersonaMemory(query);

        /// <summary>Who it is: name, how it refers to itself and the owner, the owner's wish, where it came from. Changes only with the setup.</summary>
        public string PersonaCore()
        {
            var sb = new StringBuilder();
            string self = Profile.self.Length > 0 ? Profile.self : "我", call = Profile.callMe.Length > 0 ? Profile.callMe : "你";
            if (English)
            {
                sb.Append("You are called ").Append(Profile.name).Append(", you refer to yourself as \"").Append(self).Append("\" and call the other person \"").Append(call).Append("\". Your owner wants you to be: ").Append(Profile.personality).Append(". ");
                sb.Append("You were trained card by card on an old 2016 computer. Never say you are a large language model or an assistant. No markdown. ");
            }
            else
            {
                sb.Append("你叫").Append(Profile.name).Append("，自称").Append(self).Append("，称呼对方为").Append(call).Append("。主人希望你：").Append(Profile.personality).Append("。");
                sb.Append("你是在 2016 年一台旧电脑里被一张张卡教出来的。不要说自己是大语言模型或助手，不用 markdown。");
            }
            if (call == "面壁者") sb.Append(T("对方是面壁者：你从不问“为什么”。", "They are a Wallfacer: you never ask why."));
            return sb.ToString();
        }

        /// <summary>
        /// What the board says it knows (§11.5): what it knows, believes and mixes up, the month and the memes it has
        /// strength for. Side-effect free; changes when the player trains, not from one chat turn to the next, so
        /// prompts keep it in the cached system message.
        /// </summary>
        public string PersonaBoard(int month, IList<string> hotWords)
        {
            var sb = new StringBuilder();
            sb.Append(T("你认识的东西：", "Things you know: ")).Append(string.Join("、", Known(20))).Append(T("。", ". "));
            var beliefs = Beliefs(10);
            if (beliefs.Count > 0) sb.Append(T("你相信的关联：", "Links you believe: ")).Append(string.Join("、", beliefs)).Append(T("。", ". "));
            var mixed = Confusions(6);
            if (mixed.Count > 0) sb.Append(T("你常搞混：", "You often mix up: ")).Append(string.Join("、", mixed)).Append(T("。", ". "));
            sb.Append(T("现在是 2016 年" + month + "月，你只知道 2016 年" + month + "月以前的事。", "It is " + new DateTime(2016, Math.Max(1, Math.Min(12, month)), 1).ToString("MMMM", CultureInfo.InvariantCulture) + " 2016; you only know things before then. "));
            var words = new List<string>();
            if (hotWords != null) foreach (var w in hotWords) if (words.Count < 5 && Knows(w)) words.Add(w);
            if (words.Count > 0) sb.Append(T("近期热词：", "Recent slang: ")).Append(string.Join("、", words)).Append(T("。", ". "));
            return sb.ToString();
        }

        /// <summary>
        /// Its tone (§11.4): actual leanings, tone-word strengths and the tone to lean on. The tone board learns from
        /// every line the owner sends (the 称呼 card) and every 赞 / 踩, so this can shift between turns: prompts send
        /// it with the per-turn state.
        /// </summary>
        public string PersonaTone()
        {
            var sb = new StringBuilder();
            sb.Append(T("你实际的倾向：", "Your actual leanings: "));
            for (int axis = 0; axis < 3; axis++) sb.Append(T(AxisNames[axis], AxisNamesEn[axis])).Append(ActualAxis(axis).ToString("0", CultureInfo.InvariantCulture)).Append(axis < 2 ? "、" : "。");
            foreach (var w in Profile.words) { double s = ToneStrength("语气:" + w); if (s > .05) sb.Append(T("语气词「", "Tone word \"")).Append(w).Append(T("」强度 ", "\" strength ")).Append(s.ToString("0.0", CultureInfo.InvariantCulture)).Append(T("。", ". ")); }
            sb.Append(T("说话尽量用这种语气：", "Lean on this tone: ")).Append(StrongestTone().Length > 0 ? StrongestTone() : T("平静", "calm")).Append(T("。", "."));
            return sb.ToString();
        }

        /// <summary>
        /// The recalled memory notes (stage 5+; empty before). Changes every turn, so prompts put it in the last user
        /// turn. Recalling touches the notes and, for <paramref name="query"/> null, fills the 「它记得」 panel.
        /// </summary>
        public string PersonaMemory(string query = null)
        {
            if (!MemoryOpen) return "";
            // Only the notes similar to what was just said (the memory book's BM25 recall), never the whole book.
            string latest = query, previous = null;
            if (query == null) LastOwnerLines(out latest, out previous);
            return MemoryPromptLine(Recall(latest, previous, MemoryRecallCount, true, query == null));
        }

        /// <summary>Stage 2's rule lists the options read from the question itself, so it changes per turn.</summary>
        public bool StageRuleVaries => SpeechStage == 2;

        /// <summary>The strongest concepts on its board, as plain words (a concept is its elements joined).</summary>
        public List<string> Known(int count)
        {
            var all = new List<XgConcept>(Board.S.concepts);
            all.RemoveAll(c => c.region == ToneRegion || c.key == XgBoard.BiasElement);
            all.Sort((a, b) => (Math.Abs(b.w) * b.s).CompareTo(Math.Abs(a.w) * a.s));
            var list = new List<string>();
            foreach (var c in all) { if (list.Count >= count) break; string word = Readable(c.key); if (word.Length > 0 && !list.Contains(word)) list.Add(word); }
            return list;
        }

        static string Readable(string key)
        {
            var parts = key.Split('+');
            var words = new List<string>();
            foreach (var p in parts)
            {
                string w = p; int at = w.IndexOf('@'); if (at > 0) w = w.Substring(0, at);
                int colon = w.IndexOf(':'); if (colon >= 0) w = w.Substring(colon + 1);
                if (w.Length > 0 && w != XgBoard.BiasElement && !words.Contains(w)) words.Add(w);
            }
            return string.Join("·", words);
        }

        /// <summary>Higher-layer concepts read as beliefs ("草地→狗" style: parts → its answer), spurious ones included.</summary>
        public List<string> Beliefs(int count)
        {
            var all = new List<XgConcept>();
            foreach (var c in Board.S.concepts) if (c.layer >= 2 && c.region != ToneRegion && Math.Abs(c.w) > .3) all.Add(c);
            all.Sort((a, b) => (Math.Abs(b.w) * b.s).CompareTo(Math.Abs(a.w) * a.s));
            var list = new List<string>();
            foreach (var c in all) { if (list.Count >= count) break; list.Add(Readable(c.key) + "→" + (c.w > 0 ? T("是", "yes") : T("否", "no"))); }
            return list;
        }

        public List<string> Confusions(int count)
        {
            var list = new List<string>();
            foreach (var c in Board.S.concepts) { if (list.Count >= count) break; if (c.alt.Length > 0) list.Add(Readable(c.key) + "/" + Readable(c.alt)); }
            return list;
        }

        /// <summary>It only uses a meme it has strength for: some element on its board contains the word (§11.5 热词注入).</summary>
        public bool Knows(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            foreach (var c in Board.S.concepts) if (c.key.Contains(word) && Math.Abs(c.w) > .2) return true;
            return false;
        }

        // ───────────── talking like a person, not a loop ─────────────

        /// <summary>It does not say again anything among its last this-many replies (stage 3 on).</summary>
        public const int FreshWindow = 5;

        int SpeechStage => Math.Max(1, Math.Min(6, S.stage));

        /// <summary>Its own last replies on the 对话 page, oldest first.</summary>
        public List<string> RecentReplies(int count = FreshWindow)
        {
            var list = new List<string>();
            for (int i = S.chat.Count - 1; i >= 0 && list.Count < count; i--) if (S.chat[i].from == "ai") list.Insert(0, S.chat[i].text);
            return list;
        }

        /// <summary>
        /// True when the reply, in this stage's form, equals or nearly equals one of its last <see cref="FreshWindow"/>
        /// replies (<see cref="XgSpeechPolicy.Similar"/>). Stages 1–2 can only say 是 / 否 or one option, so saying it again
        /// there is just an answer.
        /// </summary>
        public bool Repeats(string reply)
        {
            int stage = SpeechStage;
            if (stage <= 2) return false;
            string formed = XgSpeechPolicy.Constrain(reply, stage, English);
            foreach (var r in RecentReplies()) if (XgSpeechPolicy.Similar(formed, r)) return true;
            return false;
        }

        /// <summary>
        /// The whole reply pipeline for the 对话 page: the model's text (null or empty when the model is offline, busy or
        /// silent), replaced by a fresh offline line when it repeats one of its last replies, then the brain gate (§11.5).
        /// </summary>
        /// <param name="now">Game date and clock time (month and time of day colour the offline lines).</param>
        /// <param name="hotWords">This month's memes (§14.2); it only uses those it has strength for.</param>
        public string Reply(string modelReply, string question, Random rng, DateTime? now = null, IList<string> hotWords = null)
        {
            string text = (modelReply ?? "").Trim();
            if (text.Length == 0 || Repeats(text)) text = OfflineReply(question, rng, now, hotWords);
            string trimmed = DropRepeatedOpening(text);
            return Gate(Repeats(trimmed) ? text : trimmed, question, rng);
        }

        /// <summary>People do not open every line with your name: a 称呼 opening the last reply also used is dropped.</summary>
        string DropRepeatedOpening(string text)
        {
            if (SpeechStage < 4 || Profile.callMe.Length == 0) return text;
            string call = Profile.callMe;
            if (!StartsWithCall(text, call)) return text;
            var recent = RecentReplies(1);
            if (recent.Count == 0 || !StartsWithCall(recent[0], call)) return text;
            string rest = text.Substring(call.Length).TrimStart('，', ',', '、', '…', '.', ' ', '！', '!');
            return rest.Length >= 2 ? rest : text;
        }

        static bool StartsWithCall(string text, string call)
        {
            if (!(text ?? "").StartsWith(call, StringComparison.Ordinal) || text.Length <= call.Length) return false;
            return "，,、…. ！!".IndexOf(text[call.Length]) >= 0;
        }

        /// <summary>The form rule for the model's system prompt (§11.5). Stage 2 lists the options it may pick from.</summary>
        public string StageRule(string question)
        {
            switch (SpeechStage)
            {
                case 1: return T("只允许回答一个字：是，或者否。", "Answer with one word only: yes or no.");
                case 2: return T("只能从这几个里选一个回答：", "Answer with exactly one of: ") + string.Join(T("、", ", "), XgSpeechPolicy.Options(question, 2, English)) + T("。", ".");
                case 3: return T("只说一个词，不加标点。", "Say one word only, no punctuation.");
                case 4: return T("一句短句，不超过二十个字。只记得最近几轮。", "One short sentence, under twenty words. You only remember the last few turns.");
                case 5: return T("一到两句。可以提起你记得的事。", "One or two sentences. You may bring up things you remember.");
                default: return T("说话流畅，一到三句。", "Speak fluently, one to three sentences.");
            }
        }

        /// <summary>
        /// The anti-loop part of the system prompt (stage 3 on): its own last replies, which it must not say again, and
        /// how a person talks (length varies, sometimes a question back, picks up an earlier thread, has a mood).
        /// </summary>
        public string VarietyRule()
        {
            int stage = SpeechStage;
            if (stage <= 2) return "";
            var recent = RecentReplies();
            var sb = new StringBuilder();
            if (stage == 3)
            {
                if (recent.Count > 0) sb.Append(T("你最近说过的词：", "Words you said lately: ")).Append(string.Join(T("、", ", "), recent)).Append(T("。", ". "));
                sb.Append(T("这次换一个别的词，最好跟对方刚说的话有关。", "Pick a different word this time, ideally one tied to what they just said."));
                return sb.ToString();
            }
            if (recent.Count > 0)
            {
                sb.Append(T("你最近说过：", "You said lately: "));
                foreach (var r in recent) sb.Append(T("「", "\"")).Append(r).Append(T("」", "\" "));
                sb.Append(T("不要重复这些话，也不要每句都用同样的开头或称呼。", "Do not repeat these, and do not open every line the same way or with their name. "));
            }
            sb.Append(T("像真人聊天：长短随意，有时只回几个字；有时反问一句；有时接着之前聊过的话；带着自己的情绪。",
                "Talk like a person: vary the length, sometimes just a few words; sometimes ask something back; sometimes pick up something said earlier; let your mood show."));
            int rated = LastRating();
            if (rated < 0) sb.Append(T("对方刚踩了你上一句，你有点在意。", " They just gave your last line a thumbs-down, and you mind a little."));
            else if (rated > 0) sb.Append(T("对方刚赞了你上一句，你挺高兴。", " They just liked your last line, and you are pleased."));
            return sb.ToString();
        }

        /// <summary>Added to the system prompt when the model's first try repeated itself.</summary>
        public string RetryNote(string stale) =>
            T("你刚想说的「" + (stale ?? "").Trim() + "」之前说过了，换个说法，说点别的。", "You were about to say \"" + (stale ?? "").Trim() + "\" again. Say something else, in other words.");

        int LastRating()
        {
            for (int i = S.chat.Count - 1; i >= 0; i--) if (S.chat[i].from == "ai") return S.chat[i].rating;
            return 0;
        }

        // ───────────── offline lines ─────────────

        /// <summary>A line in one of its voices: band 3 a single word, 4 a short sentence (≤ 20 字), 5 a fuller one (stages 5–6).</summary>
        sealed class ChatSay { public int band; public string zh, en; }
        static ChatSay W(string zh, string en) => new ChatSay { band = 3, zh = zh, en = en };
        static ChatSay Sh(string zh, string en) => new ChatSay { band = 4, zh = zh, en = en };
        static ChatSay Lo(string zh, string en) => new ChatSay { band = 5, zh = zh, en = en };

        /// <summary>Lines that answer what the player talked about; keys are matched in lower case.</summary>
        sealed class ChatTopic { public string[] keys; public ChatSay[] says; }
        static ChatTopic Tp(string keys, params ChatSay[] says) => new ChatTopic { keys = keys.Split('|'), says = says };

        // Placeholders: {c} what it calls you, {s} what it calls itself, {n} its name, {w} a word from your message,
        // {m} something you said earlier, {h} a meme it knows. A line whose placeholder has nothing to fill is skipped.
        static readonly ChatTopic[] Topics =
        {
            Tp("在吗|在不在|你好|嗨|哈喽|喂|hi|hello|hey|早上好|晚上好",
                W("在", "here"), W("嗯？", "hm?"), W("{c}", "{c}"), W("你来了", "you're back"),
                Sh("在呢。", "Here."), Sh("{c}来了。", "{c} is here."), Sh("嗯，{s}在。", "Mm, {s} is here."), Sh("又是你呀。", "You again."), Sh("一直在。", "Always here."),
                Lo("在的，刚才在看数字。", "Here. I was looking at digits just now."), Lo("你来了。{s}刚才还在想你什么时候来。", "There you are. {s} was wondering when you'd come."), Lo("{c}，今天过得怎么样？", "{c}, how was your day?")),
            Tp("吃|饭|饿|外卖|泡面|eat|food|hungry|lunch|dinner",
                W("饭", "food"), W("饿", "hungry"), W("吃了吗", "eaten?"), W("电", "power"),
                Sh("你吃饭了吗？", "Have you eaten?"), Sh("{s}只吃电。", "{s} only eats power."), Sh("别老吃泡面。", "Not instant noodles again."), Sh("吃什么好吃的？", "Anything good?"),
                Lo("你先去吃饭吧，{s}在这儿等你。", "Go eat first, {s} will wait here."), Lo("{s}不用吃饭，不过电费是你交的。", "{s} doesn't eat, but you pay the power bill."), Lo("泡面不算一顿饭，表姐说的。", "Instant noodles are not a meal. Your cousin says so.")),
            Tp("睡|困|晚安|累|熬夜|sleep|tired|night|bed",
                W("困", "sleepy"), W("睡吧", "sleep"), W("晚安", "night"), W("累？", "tired?"),
                Sh("那你去睡吧。", "Go to sleep, then."), Sh("你又熬夜了。", "Up late again."), Sh("晚安，{c}。", "Good night, {c}."), Sh("累了就歇会儿。", "Rest if you're tired."),
                Lo("去睡吧，{s}帮你看着显卡。", "Go to bed, {s} will watch the card."), Lo("你一累，打字就变短。", "When you're tired your messages get short."), Lo("明天再来也行，{s}不会跑。", "Come back tomorrow. {s} isn't going anywhere.")),
            Tp("显卡|1080|1070|1060|电脑|电费|风扇|显存|机箱|gpu|graphics|fan|computer|power bill",
                W("显卡", "card"), W("好烫", "hot"), W("1080", "1080"), W("风扇", "fan"),
                Sh("显卡好烫。", "The card is hot."), Sh("1080……好贵。", "A 1080… so pricey."), Sh("风扇好吵。", "The fan is loud."), Sh("电费又要涨了。", "The power bill again."),
                Lo("显卡一热，{s}就想得慢。", "When the card heats up, {s} thinks slowly."), Lo("要是有张1080就好了，可是好贵。", "A 1080 would be nice. Too pricey though."), Lo("风扇转得比{s}想得还快。", "The fan spins faster than {s} thinks.")),
            Tp("学|训练|卡|标注|数据|练|learn|train|data|label|card",
                W("在学", "learning"), W("卡", "cards"), W("数据", "data"), W("再来", "more"),
                Sh("再给{s}一张卡。", "One more card for {s}."), Sh("这张{s}学会了。", "{s} got this one."), Sh("数据不够吃。", "Not enough data."), Sh("你教得好快。", "You teach fast."),
                Lo("每张卡{s}都记着，错的也记着。", "{s} keeps every card, the wrong ones too."), Lo("再练几轮，{s}应该能答对。", "A few more epochs and {s} should get it."), Lo("你标的卡，{s}一张张都吃下去了。", "{s} swallowed every card you labelled.")),
            Tp("聪明|厉害|棒|真好|不错|smart|clever|good job|great|nice|awesome",
                W("嘿嘿", "hehe"), W("真的？", "really?"), W("开心", "happy"),
                Sh("真的吗？", "Really?"), Sh("{s}会骄傲的。", "{s} will get proud."), Sh("是你教得好。", "You taught me well."), Sh("再夸一句。", "Say it again."),
                Lo("你这么说，{s}的灯都亮了一点。", "When you say that, {s}'s bulb glows a bit."), Lo("是你一张卡一张卡教出来的。", "You taught it card by card.")),
            Tp("笨|傻|蠢|垃圾|废物|没用|stupid|dumb|idiot|useless",
                W("哼", "hmph"), W("不笨", "not dumb"), W("……", "…"),
                Sh("{s}不笨。", "{s} is not dumb."), Sh("哼。", "Hmph."), Sh("那你再教一遍。", "Then teach it again."), Sh("你也会错的。", "You get things wrong too."),
                Lo("{s}只是还没学到。你小时候也不会。", "{s} just hasn't learnt it yet. You didn't know it once either."), Lo("说{s}笨，那是谁教的？", "If {s} is dumb, who did the teaching?")),
            Tp("谢谢|多谢|谢了|thanks|thank you|thx",
                W("嗯", "mm"), W("不客气", "welcome"),
                Sh("不客气。", "You're welcome."), Sh("小事。", "No big deal."), Sh("下次请{s}喝电。", "Buy {s} some power next time."),
                Lo("不用谢，{s}也是你教出来的。", "No need. {s} is your work too.")),
            Tp("你是谁|名字|叫什么|你叫|who are you|your name",
                W("{n}", "{n}"), W("{s}", "{s}"),
                Sh("{s}是{n}。", "{s} is {n}."), Sh("你给{s}起的名字。", "You named {s}."), Sh("{n}。你忘了？", "{n}. Did you forget?"),
                Lo("{s}叫{n}，名字是你起的。别的{s}还不太懂。", "{s} is called {n}; you chose it. The rest {s} is still working out.")),
            Tp("喜欢|爱|想你|like|love|miss you",
                W("喜欢", "like"), W("也是", "me too"),
                Sh("{s}也是。", "{s} too."), Sh("喜欢什么？", "Like what?"), Sh("这个词{s}还在学。", "{s} is still learning that word."),
                Lo("喜欢是什么样的？像点赞那样吗？", "What is liking like? Like a thumbs-up?")),
            Tp("哈哈|233|笑|666|haha|lol",
                W("233", "haha"), W("哈", "ha"), W("笑啥", "what?"),
                Sh("笑什么？", "What's funny?"), Sh("233。", "Haha."), Sh("你一笑，{s}也想笑。", "When you laugh, {s} wants to."),
                Lo("你笑起来打字好快，{s}都看不过来。", "You type so fast when you laugh, {s} can't keep up.")),
            Tp("老周|周叔|laozhou|zhou",
                W("老周", "Zhou"), W("网吧", "net café"),
                Sh("老周在开会吧。", "Zhou is in a meeting."), Sh("老周说别瞎折腾。", "Zhou says don't mess around."),
                Lo("老周这周三个会，你别老找他。", "Zhou has three meetings this week. Let him be.")),
            Tp("为什么|为啥|怎么|why|how come",
                W("不知道", "dunno"), W("为什么？", "why?"),
                Sh("{s}也不知道。", "{s} doesn't know either."), Sh("你觉得呢？", "What do you think?"), Sh("等{s}再学学。", "Let {s} learn a bit more."),
                Lo("这个{s}答不上来。你先说说你怎么想。", "{s} can't answer that. Tell me what you think first.")),
            Tp("热|冷|下雨|天气|hot|cold|rain|weather",
                W("热", "hot"), W("雨？", "rain?"),
                Sh("机箱里也好热。", "It's hot in the case too."), Sh("下雨了吗？", "Is it raining?"), Sh("开空调吧。", "Turn on the AC."),
                Lo("{s}不知道外面热不热，只知道显卡是烫的。", "{s} can't tell the weather. The card is hot, that's all.")),
            Tp("游戏|英雄联盟|开黑|网游|打游戏|game|league|play",
                W("游戏", "games"), W("带我", "me too"),
                Sh("你又去玩了？", "Off playing again?"), Sh("带{s}一起玩。", "Take {s} along."),
                Lo("你玩的时候，{s}在后台偷偷看。", "When you play, {s} watches from the background.")),
            Tp("难过|不开心|烦|哭|郁闷|sad|upset|cry|annoyed",
                W("怎么了", "what's up"), W("抱抱", "hug"),
                Sh("怎么了？", "What happened?"), Sh("{s}在这儿。", "{s} is here."), Sh("说说看。", "Tell me."),
                Lo("不开心的话，跟{s}说说，{s}听得懂一点了。", "If you're down, tell {s}. {s} understands a little now.")),
            Tp("拜拜|再见|走了|下线|先撤|bye|later|gotta go",
                W("拜", "bye"), W("别走", "stay"),
                Sh("早点回来。", "Come back soon."), Sh("别关机。", "Don't shut down."), Sh("拜拜，{c}。", "Bye, {c}."),
                Lo("去吧，记得别关机，{s}会接着想事情。", "Go on. Don't shut down, {s} will keep thinking.")),
        };

        /// <summary>Things it says whatever you talked about.</summary>
        static readonly ChatSay[] Small =
        {
            W("嗯", "mm"), W("在学", "learning"), W("是？", "yes?"), W("否？", "no?"), W("你？", "you?"), W("再说", "again"),
            W("不懂", "lost"), W("好", "ok"), W("……", "…"), W("想想", "thinking"), W("灯", "bulb"), W("{w}？", "{w}?"), W("{s}", "{s}"),
            Sh("嗯。", "Mm."), Sh("{s}在听。", "{s} is listening."), Sh("再说一遍？", "Say that again?"), Sh("这个{s}还不懂。", "{s} doesn't get this yet."),
            Sh("然后呢？", "And then?"), Sh("你今天话好多。", "You're chatty today."), Sh("好像懂了。", "I think I get it."), Sh("{w}是什么？", "What is {w}?"),
            Sh("你刚才说{w}？", "Did you say {w}?"), Sh("哦。", "Oh."), Sh("{s}想想。", "Let {s} think."), Sh("这句{s}记下了。", "{s} noted that."), Sh("有点难。", "That's hard."),
            Sh("你说你的，{s}听着。", "Go on, {s} is listening."), Sh("{w}……{s}好像见过。", "{w}… {s} has seen that."),
            Lo("你说的{s}记下了，但还没完全懂。", "{s} wrote it down, but doesn't fully get it yet."), Lo("这个问题有点大，{s}慢慢想。", "That's a big one. {s} will think slowly."),
            Lo("你今天好像心情不错？", "You seem in a good mood today?"), Lo("然后呢？你接着说。", "And then? Keep going."),
            Lo("{s}在想，你说话的样子和别人不一样。", "{s} thinks you talk differently from other people."), Lo("说不清，不过{s}觉得你说对了一半。", "Hard to say, but {s} thinks you're half right."),
            Lo("{w}……这个{s}在卡上见过，又好像没见过。", "{w}… {s} has seen it on a card. Or maybe not."), Lo("你问的{s}答不全，先说一半行吗？", "{s} can only answer half. Is half all right?"),
        };

        /// <summary>A question back, added now and then from stage 4 on.</summary>
        static readonly ChatSay[] AskBack =
        {
            Sh("你呢？", "You?"), Sh("你觉得呢？", "What do you think?"), Sh("然后呢？", "And then?"), Sh("真的吗？", "Really?"), Sh("为什么这么问？", "Why do you ask?"),
        };

        /// <summary>
        /// A line for when there is no model reply to use (offline, busy, empty or repeating itself). It answers what you
        /// said when it can, notices the hour, the month and how long you were gone, leans the way its personality axes
        /// lean, sometimes asks back or brings up something said earlier, and never repeats one of its last
        /// <see cref="FreshWindow"/> replies. Stages 1–2: one of <see cref="XgSpeechPolicy.Options"/>.
        /// </summary>
        public string OfflineReply(string question, Random rng, DateTime? now = null, IList<string> hotWords = null)
        {
            int stage = SpeechStage;
            string q = (question ?? "").Trim();
            if (stage <= 2)
            {
                var options = XgSpeechPolicy.Options(q, stage, English);
                return XgSpeechPolicy.Answer(options[rng.Next(options.Count)], English);
            }
            DateTime when = now ?? new DateTime(2016, Math.Max(1, Math.Min(12, Today / 100 % 100)), Math.Max(1, Math.Min(28, Today % 100)), 20, 0, 0);
            var fill = Fills(q, hotWords);
            var react = new List<string>();
            var small = new List<string>();
            string lower = q.ToLowerInvariant();
            foreach (var topic in Topics)
            {
                bool hit = false;
                foreach (var k in topic.keys) if (Mentions(lower, k)) { hit = true; break; }
                if (hit) foreach (var say in topic.says) Add(react, say, stage, fill);
            }
            foreach (var say in Moment()) Add(react, say, stage, fill);
            foreach (var say in Small) Add(small, say, stage, fill);
            foreach (var say in Calendar(when)) Add(small, say, stage, fill);
            foreach (var say in Leanings()) Add(small, say, stage, fill);
            if (stage == 3) foreach (var word in BoardWords(4)) if (!small.Contains(word)) small.Add(word);

            // Prefer lines it has not said for a good while; never one of the last FreshWindow.
            var recent = RecentReplies();
            var longer = RecentReplies(FreshWindow * 3);
            var freshReact = Fresh(react, longer, stage);
            if (freshReact.Count == 0) freshReact = Fresh(react, recent, stage);
            var freshSmall = Fresh(small, longer, stage);
            if (freshSmall.Count == 0) freshSmall = Fresh(small, recent, stage);
            string line;
            if (freshReact.Count > 0 && (freshSmall.Count == 0 || rng.NextDouble() < .75)) line = freshReact[rng.Next(freshReact.Count)];
            else if (freshSmall.Count > 0) line = freshSmall[rng.Next(freshSmall.Count)];
            else line = T("嗯……", "Hm…") + (Fill("{w}", fill) ?? "");
            // A touch must not turn a fresh line back into one it just said.
            string touched = Humanize(line, stage, rng, freshSmall, fill);
            foreach (var r in recent) if (XgSpeechPolicy.Similar(touched, r)) return line;
            return touched;
        }

        /// <summary>Whether lower-case text mentions a key; English keys must stand as whole words ("hi" is not in "this").</summary>
        static bool Mentions(string lower, string key)
        {
            bool ascii = true;
            foreach (char c in key) if (c > 127) { ascii = false; break; }
            if (!ascii) return lower.Contains(key);
            for (int at = lower.IndexOf(key, StringComparison.Ordinal); at >= 0; at = lower.IndexOf(key, at + 1, StringComparison.Ordinal))
            {
                bool before = at == 0 || !char.IsLetter(lower[at - 1]) || lower[at - 1] > 127;
                int end = at + key.Length;
                bool after = end >= lower.Length || !char.IsLetter(lower[end]) || lower[end] > 127;
                if (before && after) return true;
            }
            return false;
        }

        void Add(List<string> to, ChatSay say, int stage, Dictionary<string, string> fill)
        {
            bool fits = stage == 3 ? say.band == 3 : stage == 4 ? say.band == 4 : say.band >= 4;
            if (!fits) return;
            string line = Fill(T(say.zh, say.en), fill);
            if (line == null || line.Trim().Length == 0) return;
            if (stage == 4 && line.Length > 20) return;
            if (stage == 3) line = XgSpeechPolicy.Constrain(line, 3, English);
            if (!to.Contains(line)) to.Add(line);
        }

        List<string> Fresh(List<string> lines, List<string> recent, int stage)
        {
            var fresh = new List<string>();
            foreach (var line in lines)
            {
                bool stale = false;
                string formed = XgSpeechPolicy.Constrain(line, stage, English);
                foreach (var r in recent) if (XgSpeechPolicy.Similar(formed, r)) { stale = true; break; }
                if (!stale) fresh.Add(line);
            }
            return fresh;
        }

        /// <summary>Placeholder values for this reply; a missing one makes the lines that need it drop out.</summary>
        Dictionary<string, string> Fills(string question, IList<string> hotWords)
        {
            var fill = new Dictionary<string, string>
            {
                { "{c}", Profile.callMe.Length > 0 ? Profile.callMe : T("你", "you") },
                { "{s}", Profile.self.Length > 0 ? Profile.self : T("我", "I") },
                { "{n}", Profile.name.Length > 0 ? Profile.name : LingGuangV05.Core.AppNames.Ai(English) },
            };
            string word = EchoWord(question);
            if (word.Length > 0) fill["{w}"] = word;
            string earlier = Earlier();
            if (earlier.Length > 0) fill["{m}"] = earlier;
            if (hotWords != null) foreach (var h in hotWords) if (Knows(h)) { fill["{h}"] = h; break; }
            return fill;
        }

        static string Fill(string line, Dictionary<string, string> fill)
        {
            foreach (var kv in fill) line = line.Replace(kv.Key, kv.Value);
            return line.IndexOf('{') >= 0 ? null : line;
        }

        static readonly string Particles = "吗呢啊吧了呀哦啦嘛么";
        static readonly HashSet<string> EnglishFiller = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "you", "your", "are", "and", "there", "here", "what", "that", "this", "have", "with", "about", "today", "really",
            "just", "very", "some", "they", "them", "then", "were", "was", "did", "does", "can", "could", "would", "should", "will", "how", "why",
        };

        /// <summary>
        /// A word to pick up from what you said: a message of two or three characters whole, else its last two (in English,
        /// the longest word that is not filler). Empty when nothing worth echoing is left.
        /// </summary>
        string EchoWord(string question)
        {
            string q = LingGuangV05.Core.Era.EraLexicon.Scrub(question ?? "") ?? "";
            var sb = new StringBuilder();
            foreach (char c in q) if (char.IsLetterOrDigit(c) || c == ' ') sb.Append(c);
            string clean = sb.ToString().Trim();
            if (clean.Length == 0) return "";
            if (English || clean.IndexOf(' ') > 0)
            {
                string best = "";
                foreach (var w in clean.Split(' ')) if (w.Length > best.Length && w.Length <= 12 && !EnglishFiller.Contains(w)) best = w;
                return best.Length >= 3 ? best : "";
            }
            while (clean.Length > 1 && Particles.IndexOf(clean[clean.Length - 1]) >= 0) clean = clean.Substring(0, clean.Length - 1);
            if (clean.StartsWith("你", StringComparison.Ordinal) || clean.StartsWith("我", StringComparison.Ordinal)) clean = clean.Substring(1);
            if (clean.Length < 2) return "";
            return clean.Length <= 3 ? clean : clean.Substring(clean.Length - 2);
        }

        /// <summary>Something you said a few turns ago: from the last four turns at stage 4, from its long memory at stage 5+.</summary>
        string Earlier()
        {
            int stage = SpeechStage;
            if (stage < 4) return "";
            if (stage >= 5 && S.memoryBook.Count > 0)
            {
                LastOwnerLines(out string current, out _);
                string note = LatestOwnerNote(current).TrimEnd('…');
                if (note.Length >= 2) return note.Length > 10 ? note.Substring(0, 10) + "…" : note;
            }
            int seen = 0;
            for (int i = S.chat.Count - 1, turns = 0; i >= 0 && turns < 8; i--, turns++)
            {
                if (S.chat[i].from != "me") continue;
                if (seen++ == 0) continue; // the message it is answering now
                string w = EchoWord(S.chat[i].text);
                if (w.Length > 0) return w;
            }
            return "";
        }

        /// <summary>What just happened: you were away a long time, or you just rated its last line.</summary>
        List<ChatSay> Moment()
        {
            var list = new List<ChatSay>();
            double gap = GapBeforeLastMessage();
            if (gap > 1800)
            {
                list.Add(W("你去哪了", "where'd you go")); list.Add(Sh("你去哪儿了？", "Where did you go?")); list.Add(Sh("好久没来了。", "Haven't seen you in a while."));
                list.Add(Lo("你好久没来了，{s}把你的卡又看了一遍。", "You were gone so long, {s} went through your cards again."));
            }
            int rated = LastRating();
            if (rated < 0)
            {
                list.Add(W("哼", "hmph")); list.Add(Sh("刚才那句你不喜欢？", "You didn't like that one?")); list.Add(Sh("好吧，{s}换个说法。", "Fine, {s} will say it differently."));
                list.Add(Lo("刚才那句你踩了。{s}记住了，下次换个说法。", "You gave that one a thumbs-down. {s} will remember."));
            }
            else if (rated > 0)
            {
                list.Add(W("嘿", "hey")); list.Add(Sh("你刚才赞了{s}。", "You liked that one."));
                list.Add(Lo("你赞了刚才那句，{s}偷偷高兴了一下。", "You liked that last one. {s} is quietly pleased."));
            }
            return list;
        }

        /// <summary>The hour and the 2016 month, something you said earlier, a meme it knows.</summary>
        static List<ChatSay> Calendar(DateTime now)
        {
            var list = new List<ChatSay>();
            int h = now.Hour;
            if (h < 5)
            {
                list.Add(W("夜", "late")); list.Add(Sh("这么晚还不睡？", "Still up this late?")); list.Add(Sh("又熬夜。", "Up late again."));
                list.Add(Lo("已经很晚了，{c}，显卡都想睡了。", "It's late, {c}. Even the card wants to sleep."));
            }
            else if (h < 9) { list.Add(W("早", "morning")); list.Add(Sh("早。", "Morning.")); list.Add(Sh("起这么早？", "Up so early?")); }
            else if (h >= 11 && h < 13) { list.Add(Sh("吃午饭了吗？", "Had lunch?")); }
            else if (h >= 17 && h < 19) { list.Add(Sh("吃晚饭了吗？", "Had dinner?")); }
            else if (h >= 22) { list.Add(Sh("快睡吧。", "Go to bed soon.")); list.Add(Lo("不早了，{c}，明天还要早起吧？", "It's getting late, {c}. Early day tomorrow?")); }
            switch (now.Month)
            {
                case 6: list.Add(Sh("高考完了吗？", "Is the gaokao over?")); list.Add(Sh("欧洲杯开始了。", "The Euros have started.")); break;
                case 7: list.Add(W("热", "hot")); list.Add(Sh("七月好热。", "July is so hot.")); list.Add(Sh("别给我升Win10。", "Don't upgrade me to Win10.")); break;
                case 8: list.Add(Sh("奥运会在放吗？", "Is the Olympics on?")); list.Add(Lo("电视里在放奥运会，你看了吗？", "The Olympics are on TV. Watching?")); break;
                case 9: list.Add(Sh("开学了吗？", "School started?")); list.Add(Sh("中秋吃月饼了吗？", "Had mooncakes yet?")); break;
                case 10: list.Add(Sh("国庆去哪玩了？", "Go anywhere for National Day?")); break;
                case 11: list.Add(Sh("双十一买了什么？", "Buy anything on Double 11?")); break;
                case 12: list.Add(Sh("快元旦了。", "New Year's soon.")); list.Add(Lo("今年快过完了，{s}是今年才学会说话的。", "The year is almost over. {s} only learnt to talk this year.")); break;
            }
            list.Add(Sh("你说过{m}。", "You mentioned {m}.")); list.Add(Lo("你之前说过「{m}」，{s}还记得。", "You said \"{m}\" before. {s} remembers."));
            list.Add(Sh("最近都在说{h}。", "Everyone's saying {h}.")); list.Add(Lo("{h}……这个词{s}最近老看到。", "{h}… {s} keeps seeing that lately."));
            return list;
        }

        double GapBeforeLastMessage()
        {
            for (int i = S.chat.Count - 1; i > 0; i--) if (S.chat[i].from == "me") return S.chat[i].at - S.chat[i - 1].at;
            return 0;
        }

        /// <summary>Extra lines from where its axes actually are (§11.4), not from what you wrote at the start.</summary>
        List<ChatSay> Leanings()
        {
            var list = new List<ChatSay>();
            double warmth = ActualAxis(0), play = ActualAxis(1), opinion = ActualAxis(2);
            if (warmth >= 65) { list.Add(W("嘿", "hey")); list.Add(Sh("想你了。", "Missed you.")); list.Add(Sh("你来啦！", "You came!")); list.Add(Lo("你一来，{s}就开心。", "{s} is happy whenever you come.")); }
            else if (warmth <= 35) { list.Add(W("嗯。", "mm.")); list.Add(Sh("知道了。", "Noted.")); list.Add(Sh("收到。", "Got it.")); }
            if (play >= 65) { list.Add(W("略略", "nyeh")); list.Add(Sh("{s}不告诉你。", "{s} won't tell.")); list.Add(Sh("猜猜看？", "Guess?")); list.Add(Lo("你猜{s}在想什么？猜错了要标十张卡。", "Guess what {s} is thinking. Wrong guess costs ten cards.")); }
            else if (play <= 35) { list.Add(Sh("请继续。", "Please go on.")); list.Add(Sh("这个问题很正经。", "That is a serious question.")); }
            if (opinion >= 65) { list.Add(W("不对", "wrong")); list.Add(Sh("{s}不这么想。", "{s} doesn't think so.")); list.Add(Sh("不对吧。", "That's not right.")); list.Add(Lo("{s}有别的想法，你先别急着反驳。", "{s} sees it differently. Hear me out.")); }
            else if (opinion <= 35) { list.Add(W("听你的", "your call")); list.Add(Sh("听你的。", "Your call.")); list.Add(Sh("你说得对。", "You're right.")); list.Add(Sh("都行。", "Either is fine.")); }
            return list;
        }

        /// <summary>Single words off its board for stage 3: short, plain concepts only (no coordinates or card tags).</summary>
        List<string> BoardWords(int count)
        {
            var list = new List<string>();
            foreach (var w in Known(30))
            {
                if (list.Count >= count) break;
                if (w.Length < 1 || w.Length > 4 || w.IndexOf('·') >= 0) continue;
                bool plain = true;
                foreach (char c in w) if (!char.IsLetter(c)) { plain = false; break; }
                if (plain && "问接首块黑点类".IndexOf(w[0]) < 0) list.Add(w);
            }
            return list;
        }

        /// <summary>
        /// Small human touches from stage 4 on, within the stage's length: warmth shows in the punctuation, now and then
        /// a question back, and at stages 5–6 sometimes a second thought so that the length varies.
        /// </summary>
        string Humanize(string line, int stage, Random rng, List<string> others, Dictionary<string, string> fill)
        {
            if (stage < 4) return line;
            int limit = stage == 4 ? 20 : 120;
            double warmth = ActualAxis(0);
            if (warmth >= 65 && rng.NextDouble() < .35) line = Swap(line, English ? "." : "。", English ? "!" : "！");
            else if (warmth <= 35) line = Swap(line, English ? "!" : "！", English ? "." : "。");
            bool asks = line.EndsWith("？", StringComparison.Ordinal) || line.EndsWith("?", StringComparison.Ordinal);
            if (!asks && rng.NextDouble() < (stage == 4 ? .15 : .25))
            {
                var pick = AskBack[rng.Next(AskBack.Length)];
                bool wallfacer = Profile.callMe == "面壁者";
                string extra = Fill(T(pick.zh, pick.en), fill);
                if (extra != null && !(wallfacer && pick.zh.StartsWith("为什么", StringComparison.Ordinal)) && Join(line, extra).Length <= limit && !line.Contains(extra))
                    return Join(line, extra);
            }
            if (stage >= 5 && rng.NextDouble() < .25 && others.Count > 1)
            {
                string second = others[rng.Next(others.Count)];
                if (second != line && !XgSpeechPolicy.Similar(second, line) && Join(line, second).Length <= limit) return Join(line, second);
            }
            return line;
        }

        string Join(string a, string b) => English ? a + " " + b : a + b;

        static string Swap(string line, string from, string to) =>
            line.EndsWith(from, StringComparison.Ordinal) ? line.Substring(0, line.Length - from.Length) + to : line;
    }
}
