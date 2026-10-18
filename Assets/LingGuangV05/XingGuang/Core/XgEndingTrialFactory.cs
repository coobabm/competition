// Adapted from the user's existing Design/code/XgSim.Trial.cs, six original sheets and scoring.
// D012: consecutive pairs form three gates. No legacy counters, auto-pass migration, rewards or SolveForChecks.
using System;
using System.Collections.Generic;
using System.Globalization;
namespace LingGuangV05.XingGuang
{
    public enum XgTrialMode { Choice, Mark }

    /// <summary>卷子上的一小题。Choice：从 choices 里选一个；Mark：这一行对不对（wrongLine）。</summary>
    [Serializable] public sealed class XgTrialItem
    {
        public string prompt = "";
        /// <summary>它的说法（「{名字}：对（把握 86%）」「太糊了，我看不清」），空 = 它不说话。</summary>
        public string ai = "";
        /// <summary>它倾向的选项（-1 = 它说不知道 / 看不清）；Mark 题里 = 1 表示它怀疑这一行。</summary>
        public int aiPick = -1;
        public List<string> choices = new List<string>();
        public int answer;
        /// <summary>Mark 题：这一行是写错的；fix = 正确说法（交卷后显示）。</summary>
        public bool wrongLine;
        public string fix = "";
        /// <summary>题 3 的照片（Resources/LingGuangV05/Media/Photos/&lt;photo&gt;），没有图时显示 photoDesc。</summary>
        public string photo = "", photoDesc = "";
        /// <summary>题 6：「看原文」能翻到的那段。</summary>
        public string original = "";
        /// <summary>做完后显示的解析。</summary>
        public string explain = "";
        /// <summary>题 6 问了 0.txt（隐藏成就「存与逢」）。</summary>
        public bool asksSeedText;
    }

    /// <summary>一道大题这一次的卷面。种子 = 题号 + 第几次做，所以同一次打开不会变，重做换一批。</summary>
    [Serializable] public sealed class XgTrialSheet
    {
        public int q, attempt;
        public XgTrialMode mode;
        public string title = "", ability = "", note = "";
        public List<string> hints = new List<string>();
        /// <summary>几分拿 1 / 2 / 3 星。</summary>
        public int[] stars = new int[3];
        /// <summary>这一次最多几星（题 3 没有视觉时 = 1）。</summary>
        public int maxStars = 3;
        /// <summary>连错两次后的第三次：多一条旁注，它这题提高一档。</summary>
        public bool boosted;
        public List<XgTrialItem> items = new List<XgTrialItem>();
    }

    public sealed class XgEndingTrialFactory
    {
        readonly XgSim sim;
        readonly bool English;
        readonly Func<string,string> dynamicFilter;
        XgState S => sim.S;
        XgProfile Profile => new XgProfile { name=Dynamic(sim.Profile?.name),callMe=Dynamic(sim.Profile?.callMe),self=Dynamic(sim.Profile?.self) };
        int CurrentMonth => sim.CurrentMonth;
        double YesNoAccuracy => sim.YesNoAccuracy;
        bool MemoryOpen => sim.MemoryOpen;
        bool Has(string id) => sim.Has(id);
        bool HasAbility(int n) => sim.HasAbility(n);
        double BestAcc(string id) => sim.BestAcc(id);
        double ActualAxis(int n) => sim.ActualAxis(n);
        int ChronicleDay(string id) => sim.ChronicleDay(id);
        int BookDoneDay(XgBook book) => sim.BookDoneDay(book);
        List<XgBook> BooksDone() => sim.BooksDone();
        string T(string zh, string en) => English ? en : zh;
        string F(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
        string Dynamic(string value){var result=dynamicFilter==null?value??"":dynamicFilter(value??"");if(result==null)throw new InvalidOperationException("content_rejected");return result;}
        public XgEndingTrialFactory(XgSim sim, bool english,Func<string,string> dynamicFilter=null) { this.sim = sim; English = english;this.dynamicFilter=dynamicFilter; }
        public XgTrialSheet Build(int q, int attempt, int consecutiveFails)
        {
            if (q < 0 || q > 5 || attempt < 0) throw new ArgumentOutOfRangeException(nameof(q));
            var rng = new Random(unchecked(S.dataSalt * 131 + q * 3571 + attempt * 7919));
            var sheet = new XgTrialSheet { q=q, attempt=attempt, title=TrialTitle(q), ability=TrialAbility(q), boosted=consecutiveFails>=2 };
            switch(q) { case 0: BuildYear(sheet,rng); break; case 1: BuildSaid(sheet,rng); break; case 2: BuildPhotos(sheet,rng); break;
                case 3: BuildManual(sheet,rng); break; case 4: BuildEnglish(sheet,rng); break; default: BuildRead(sheet,rng); break; }
            var hints=TrialHints(q); int count=consecutiveFails>=2?hints.Count:consecutiveFails>=1?1:0;
            for(int i=0;i<count;i++)sheet.hints.Add(hints[i]);
            return sheet;
        }
        static readonly string[] TrialTitles = { "这一年", "哪句是它说的", "随手拍", "使用说明", "给它的三句话", "读出来" };
        static readonly string[] TrialTitlesEn = { "This year", "Which did it say", "Snapshots", "The manual", "Three lines for it", "Read it out" };
        static readonly string[] TrialAbilities = { "是/否", "多选一", "看图说词", "找错字", "翻译", "通读" };
        static readonly string[] TrialAbilitiesEn = { "Yes/No", "Pick one", "Picture to word", "Spot the error", "Translation", "Reading" };
        public string TrialTitle(int q) => T(TrialTitles[q], TrialTitlesEn[q]);
        public string TrialAbility(int q) => T(TrialAbilities[q], TrialAbilitiesEn[q]);

        List<string> TrialHints(int q)
        {
            switch (q)
            {
                case 0: return new List<string> { T("它要是说不知道，先别判它错。", "If it says it doesn't know, don't count that against it."), T("日期看右下角。", "The date is in the bottom-right corner.") };
                case 1: return new List<string> { T("看看 YY 里它那天说了什么。", "Look at what it said on YY that day."), T("它不会说自己『什么都会了』。", "It would never say it 'knows everything'.") };
                case 2: return new List<string> { T("先看最亮的地方。", "Look at the brightest part first.") };
                case 3: return new List<string> { T("想想你自己练它时踩过的坑。", "Think of the holes you fell into while training it.") };
                case 4: return new List<string> { T("挑最像人说话的那句。", "Pick the one that sounds most like a person.") };
                default: return new List<string> { T("答不上来的，它说『不知道』才是对的。", "When there is no answer, 'I don't know' is the right one.") };
            }
        }

        string TrialAiName => Dynamic(sim.AiDisplayName);

        /// <summary>它这题有多准：基础准确率，连错两次后提高一档（+15%），最多 98%。</summary>
        static double Tier(double p, bool boosted) => Math.Min(.98, p + (boosted ? .15 : 0));

        // 题 1 这一年 ─────────────────────────────

        sealed class EraLine { public string zh, en; public bool truth; public int from; }

        static readonly EraLine[] EraLines =
        {
            new EraLine { zh = "今年 3 月，AlphaGo 四比一赢了李世石。", en = "In March, AlphaGo beat Lee Sedol four games to one.", truth = true, from = 201603 },
            new EraLine { zh = "今年欧洲杯，东道主法国拿了冠军。", en = "France, the hosts, won this year's Euros.", truth = false, from = 201607 },
            new EraLine { zh = "里约奥运会，中国女排拿了金牌。", en = "China's women's volleyball team won gold in Rio.", truth = true, from = 201608 },
            new EraLine { zh = "iPhone 7 保留了耳机孔。", en = "The iPhone 7 kept the headphone jack.", truth = false, from = 201609 },
            new EraLine { zh = "今年 2 月有 29 天。", en = "This February had 29 days.", truth = true, from = 201602 },
            new EraLine { zh = "Pokémon GO 在国内能正常玩。", en = "Pokémon GO works normally in China.", truth = false, from = 201607 },
            new EraLine { zh = "今年是猴年。", en = "This is the Year of the Monkey.", truth = true, from = 201602 },
            new EraLine { zh = "《你的名字。》今年在国内上映了。", en = "Your Name. opened in Chinese cinemas this year.", truth = true, from = 201612 },
        };

        static readonly string[] OtherNames = { "小灵", "阿尔法", "豆豆", "小白", "零号", "星星" };
        static readonly string[] OtherCalls = { "你", "老师", "主人", "哥", "老板" };

        void BuildYear(XgTrialSheet sheet, Random rng)
        {
            sheet.mode = XgTrialMode.Choice;
            sheet.note = T("先看它会不会说『不知道』。会说不知道的，比什么都答的靠谱。", "First see whether it can say 'I don't know'. One that can is more reliable than one that answers everything.");
            sheet.stars = new[] { 6, 8, 10 };
            string yes = T("对", "True"), no = T("错", "False");

            // 2016 年的事：当前日期已发生的里挑 5 句，真假尽量各半。
            int month = CurrentMonth;
            var era = new List<EraLine>();
            foreach (var e in EraLines) if (e.from <= month) era.Add(e);
            TrialShuffle(era, rng);
            var picked = new List<EraLine>();
            int t = 0, f = 0;
            foreach (var e in era) { if (picked.Count >= 5) break; if (e.truth && t >= 3 || !e.truth && f >= 3) continue; picked.Add(e); if (e.truth) t++; else f++; }
            foreach (var e in era) { if (picked.Count >= 5) break; if (!picked.Contains(e)) picked.Add(e); }
            double eraAcc = Tier(Math.Max(.8, Math.Min(.95, YesNoAccuracy)), sheet.boosted);
            foreach (var e in picked)
                sheet.items.Add(YearItem(T(e.zh, e.en), e.truth, eraAcc, rng, yes, no, ""));

            // 这一局的事：5 句全用，随机 2–3 句做成假的。它有笔记本（阶段 5 起）时记得清楚。
            var save = SaveStatements(rng);
            int fakes = 2 + rng.Next(2);
            var order = new List<int> { 0, 1, 2, 3, 4 }; TrialShuffle(order, rng);
            var fake = new HashSet<int>(order.GetRange(0, fakes));
            double saveAcc = Tier(MemoryOpen ? .95 : .55, sheet.boosted);
            for (int i = 0; i < save.Count; i++)
            {
                bool truth = !fake.Contains(i);
                sheet.items.Add(YearItem(truth ? save[i].Item1 : save[i].Item2, truth, saveAcc, rng, yes, no,
                    truth ? "" : T("原本是：", "It was: ") + save[i].Item1));
            }
            TrialShuffle(sheet.items, rng);
        }

        XgTrialItem YearItem(string text, bool truth, double acc, Random rng, string yes, string no, string explain)
        {
            var item = new XgTrialItem { prompt = text, answer = truth ? 0 : 1, explain = explain };
            item.choices.Add(yes); item.choices.Add(no);
            double sure = acc + (rng.NextDouble() - .5) * .3;
            if (sure < .55)
            {
                item.aiPick = -1;
                item.ai = T("不知道。", "I don't know.");
            }
            else
            {
                bool right = rng.NextDouble() < acc;
                item.aiPick = right ? item.answer : 1 - item.answer;
                item.ai = (item.aiPick == 0 ? yes : no) + T("（把握 ", " (") + F(Math.Min(.99, sure) * 100, "0") + "%" + T("）", " sure)");
            }
            return item;
        }

        /// <summary>这一局的五件事：(真话, 假话)。记录不够时用通用句补（制作人已同意）。</summary>
        List<Tuple<string, string>> SaveStatements(Random rng)
        {
            var list = new List<Tuple<string, string>>();
            // 名字
            string name = Profile.name.Length > 0 ? Profile.name : TrialAiName;
            string otherName = OtherNames[rng.Next(OtherNames.Length)]; if (otherName == name) otherName = OtherNames[(Array.IndexOf(OtherNames, otherName) + 1) % OtherNames.Length];
            list.Add(Tuple.Create(T("你给它起的名字是「" + name + "」。", "The name you gave it is \"" + name + "\"."), T("你给它起的名字是「" + otherName + "」。", "The name you gave it is \"" + otherName + "\".")));
            // 它叫你什么
            string call = Profile.callMe.Length > 0 ? Profile.callMe : T("你", "you");
            string otherCall = OtherCalls[rng.Next(OtherCalls.Length)]; if (otherCall == call) otherCall = OtherCalls[(Array.IndexOf(OtherCalls, otherCall) + 1) % OtherCalls.Length];
            list.Add(Tuple.Create(T("它叫你「" + call + "」。", "It calls you \"" + call + "\"."), T("它叫你「" + otherCall + "」。", "It calls you \"" + otherCall + "\".")));
            // 学会多选一那天
            int d2 = ChronicleDay("ability.2");
            if (d2 > 0)
            {
                int shift = (3 + rng.Next(7)) * (rng.Next(2) == 0 ? -1 : 1);
                list.Add(Tuple.Create(T("它学会「多选一」是在 " + DayText(d2) + "。", "It learnt 'pick one' on " + DayText(d2) + "."), T("它学会「多选一」是在 " + DayText(ShiftDay(d2, shift)) + "。", "It learnt 'pick one' on " + DayText(ShiftDay(d2, shift)) + ".")));
            }
            else list.Add(Tuple.Create(T("它学会的第一样本事是答「是」和「否」。", "The first thing it learnt was answering yes and no."), T("它学会的第一样本事是翻译。", "The first thing it learnt was translation.")));
            // 第一本读完的书
            XgBook first = null; int firstDay = int.MaxValue;
            foreach (var b in BooksDone()) { int d = BookDoneDay(b); if (d > 0 && d < firstDay) { firstDay = d; first = b; } }
            if (first != null)
            {
                XgBook other = null;
                foreach (var b in XgBooks.All) if (b != first) { other = b; if (rng.Next(3) == 0) break; }
                string on = other != null ? T(other.name, other.nameEn) : T("新华字典", "a dictionary");
                list.Add(Tuple.Create(T("它读完的第一本书是《" + first.name + "》。", "The first book it finished was " + first.nameEn + "."), T("它读完的第一本书是《" + on + "》。", "The first book it finished was " + on + ".")));
            }
            else list.Add(Tuple.Create(T("人用眼睛看东西。", "People see with their eyes."), T("人用耳朵看东西。", "People see with their ears.")));
            // 聊了多少句
            int turns = S.chatTurns / 10 * 10;
            if (turns >= 10)
                list.Add(Tuple.Create(T("你们在 YY 上聊了不止 " + turns + " 句。", "You two have exchanged more than " + turns + " lines on YY."), T("你们在 YY 上聊了不止 " + (turns * 3 + 100) + " 句。", "You two have exchanged more than " + (turns * 3 + 100) + " lines on YY.")));
            else list.Add(Tuple.Create(T("它的名字后面跟着「.exe」。", "Its name ends in \".exe\"."), T("它的名字后面跟着「.dll」。", "Its name ends in \".dll\".")));
            return list;
        }

        string DayText(int yyyymmdd) => T((yyyymmdd / 100 % 100) + " 月 " + (yyyymmdd % 100) + " 日", (yyyymmdd / 100 % 100) + "/" + (yyyymmdd % 100));

        static int ShiftDay(int yyyymmdd, int days)
        {
            try
            {
                var d = new DateTime(yyyymmdd / 10000, yyyymmdd / 100 % 100, yyyymmdd % 100).AddDays(days);
                return d.Year * 10000 + d.Month * 100 + d.Day;
            }
            catch (ArgumentException) { return yyyymmdd; }
        }

        // 题 2 哪句是它说的 ─────────────────────────────

        static readonly string[] SaidFakes = { "今天的你看起来很累，要早点睡。", "我想去看看西湖。", "你是我见过最好的老师。", "我已经什么都会了。", "我们下盘棋吧。", "你昨天没来，我等了很久。", "我想换一块更好的显卡。", "这道题太简单了。", "谢谢你一直陪着我。", "我梦见自己在下雨。" };
        static readonly string[] SaidFakesEn = { "You look tired today. Get some sleep.", "I'd like to see West Lake.", "You're the best teacher I've ever had.", "I already know everything.", "Let's play a game of chess.", "You didn't come yesterday. I waited a long time.", "I want a better graphics card.", "This question is too easy.", "Thank you for always being here.", "I dreamt I was raining." };

        string HistoricalExcerpt(string value)
        {
            if(string.IsNullOrWhiteSpace(value))return null;
            string line;try{line=Dynamic(value).Trim();}catch(InvalidOperationException){return null;}
            if(line.Length<6)return null;
            int cap=English?160:40, end=Math.Min(cap,line.Length);
            int newline=line.IndexOf('\n');if(newline>=6&&newline<end)end=newline;
            if(end<line.Length)
            {
                // Prefer a sentence boundary, but never generate replacement wording.
                for(int i=end-1;i>=6;i--)if("。！？.!?".IndexOf(line[i])>=0){end=i+1;break;}
                if(end>0&&char.IsHighSurrogate(line[end-1]))end--;
                return line.Substring(0,end).TrimEnd()+"…";
            }
            return line;
        }
        void BuildSaid(XgTrialSheet sheet, Random rng)
        {
            sheet.mode = XgTrialMode.Choice;
            sheet.note = T("它说过的话，它自己认得。长回复以节选显示，省略处标作……。", "It knows its own words. Long replies are shown as excerpts, marked with an ellipsis.");
            sheet.stars = new[] { 3, 4, 5 };
            // Actual delivered words only. Long answers remain eligible as explicitly marked excerpts.
            var real = new List<string>();
            if (S.chat != null)
                for (int i = S.chat.Count - 1; i >= 0 && real.Count < 12; i--)
                {
                    var l = S.chat[i];
                    if (l == null || l.from != "ai" || string.IsNullOrEmpty(l.text)) continue;
                    string s = HistoricalExcerpt(l.text);
                    if (s == null || real.Contains(s)) continue;
                    real.Add(s);
                }
            // Only delivered replies may fill the historical speech task. Never fabricate a past fallback line.
            if (S.askedLog != null) foreach (var record in S.askedLog)
                if (record != null && record.id > 0)
                { string excerpt=HistoricalExcerpt(record.aiAnswer);if(excerpt!=null&&!real.Contains(excerpt))real.Add(excerpt); }
            if (real.Count < 5) throw new InvalidOperationException("trial_needs_five_actual_replies");
            TrialShuffle(real, rng);

            var fakes = new List<string>(English ? SaidFakesEn : SaidFakes);
            fakes.RemoveAll(real.Contains);
            if(fakes.Count<3)throw new InvalidOperationException("trial_needs_distinct_distractors");
            TrialShuffle(fakes, rng);
            double acc = Tier(MemoryOpen ? .9 : .6, sheet.boosted);
            int next = 0;
            for (int n = 0; n < 5; n++)
            {
                var item = new XgTrialItem { prompt = T("下面哪句是它说过的？", "Which of these did it say?") };
                var options = new List<string> { real[n] };
                for (int k = 0; k < 3; k++) options.Add(fakes[next++ % fakes.Count]);
                TrialShuffle(options, rng);
                item.choices = options;
                item.answer = options.IndexOf(real[n]);
                bool right = rng.NextDouble() < acc;
                item.aiPick = right ? item.answer : (item.answer + 1 + rng.Next(3)) % 4;
                item.ai = T("我觉得是第 " + (item.aiPick + 1) + " 句。", "I think it's line " + (item.aiPick + 1) + ".");
                item.explain = T("它说过：", "It said: ") + real[n];
                sheet.items.Add(item);
            }
        }

        // 题 3 随手拍 ─────────────────────────────

        static readonly string[,] Photos =
        {
            { "trial_netbar", "网吧里一排亮着的电脑", "A row of lit computers in a net bar", "电脑", "computer" },
            { "trial_gpubox", "白色背景上的一块显卡", "A graphics card against a white background", "显卡", "graphics card" },
            { "trial_westlake", "夜晚的西湖，水面上有雾，岸边亮着灯", "West Lake at night, with mist above the water and lights along the shore", "湖", "lake" },
            { "trial_serverroom", "一间机房，一排排机柜", "A server room, rows of racks", "机柜", "server rack" },
            { "trial_drafts", "摊开的手写演算纸和笔记", "Handwritten calculation sheets and notes spread out", "草稿纸", "drafts" },
        };

        public bool TrialHasVision => Has("lenet") || Has("alexnet") || HasAbility(3);

        void BuildPhotos(XgTrialSheet sheet, Random rng)
        {
            sheet.mode = XgTrialMode.Choice;
            sheet.note = T("看看这些照片。它要是认错了，你得看出来。", "Look at these photographs. If it gets one wrong, you have to catch it.");
            sheet.stars = new[] { 3, 4, 5 };
            bool sees = TrialHasVision;
            if (!sees) sheet.maxStars = 1;
            double miss = Has("augment") ? .1 : .2;
            if (sheet.boosted) miss = Math.Max(0, miss - .15);
            int n = Photos.GetLength(0);
            var order = new List<int>(); for (int i = 0; i < n; i++) order.Add(i);
            TrialShuffle(order, rng);
            foreach (int p in order)
            {
                var item = new XgTrialItem { prompt = T("照片里拍的是什么？", "What is in the photo?"), photo = Photos[p, 0], photoDesc = T(Photos[p, 1], Photos[p, 2]) };
                var options = new List<string> { T(Photos[p, 3], Photos[p, 4]) };
                var others = new List<int>(); for (int i = 0; i < n; i++) if (i != p) others.Add(i);
                TrialShuffle(others, rng);
                options.Add(T(Photos[others[0], 3], Photos[others[0], 4])); options.Add(T(Photos[others[1], 3], Photos[others[1], 4]));
                TrialShuffle(options, rng);
                item.choices = options;
                item.answer = options.IndexOf(T(Photos[p, 3], Photos[p, 4]));
                if (!sees) { item.aiPick = -1; item.ai = T("太糊了，我看不清。", "Too blurry. I can't make it out."); }
                else
                {
                    item.aiPick = rng.NextDouble() < miss ? (item.answer + 1 + rng.Next(2)) % 3 : item.answer;
                    item.ai = T("我看着像「" + options[item.aiPick] + "」。", "Looks like \"" + options[item.aiPick] + "\" to me.");
                }
                item.explain = T(Photos[p, 1], Photos[p, 2]);
                sheet.items.Add(item);
            }
            if (!sees) sheet.hints.Insert(0, T("它还没有眼睛（科技里的卷积网络）。这题你自己做，最多 1 星，也算过。", "It has no eyes yet (the convolution nodes in the tree). Do this one yourself: at most 1 star, which still passes."));
        }

        // 题 4 使用说明 ─────────────────────────────

        static readonly string[,] ManualLines =
        {
            // zh, en, wrong(1/0), fix zh, fix en
            { "教它的时候，每答对一题，它就多一个带答案的样本。", "Each question you answer while teaching gives it one more labelled sample.", "0", "", "" },
            { "参数越多，它越不会背答案。", "The more parameters, the less it memorises answers.", "1", "参数多、题少，反而更容易背答案。", "Many parameters and few questions make memorising easier, not harder." },
            { "留一些题不教它，专门拿来考，才知道它是不是真懂。", "Keep some questions back for testing; only then do you know it really understands.", "0", "", "" },
            { "学习率越大越好，练得越快。", "The bigger the learning rate the better; it trains faster.", "1", "太大会练崩。", "Too big and training blows up." },
            { "一张图挪一挪、转一转，也能当新样本用。", "Shift or rotate a picture and it works as a new sample.", "0", "", "" },
            { "数据包下载好就会自动装进去。", "A data pack installs itself once downloaded.", "1", "要拖进「练」才算装进去。", "You have to drag it into Train to install it." },
            { "句子长了，它需要一种记住前面、忘掉没用的办法。", "For long sentences it needs a way to keep what came before and forget what doesn't matter.", "0", "", "" },
            { "翻译的时候，让它每写一个词都回头看原文。", "When translating, let it look back at the source for every word it writes.", "0", "", "" },
            { "标错的数据会拖它后腿。", "Mislabelled data holds it back.", "0", "", "" },
            { "它说得很有把握的时候，也可能是错的。", "It can be wrong even when it sounds sure.", "0", "", "" },
            { "显卡越多，一轮练得越快。", "More graphics cards make a round faster.", "0", "", "" },
            { "它第一次开口之前，先让它学会回答问题，而不是接着出题。", "Before it first speaks, teach it to answer questions rather than keep asking them.", "0", "", "" },
        };

        void BuildManual(XgTrialSheet sheet, Random rng)
        {
            sheet.mode = XgTrialMode.Mark;
            sheet.note = T("这份说明我写错了三句。别全信我。", "I got three lines of this manual wrong. Don't trust all of it.");
            sheet.stars = new[] { 1, 2, 3 };
            int n = ManualLines.GetLength(0);
            var wrong = new List<int>(); var right = new List<int>();
            for (int i = 0; i < n; i++) (ManualLines[i, 2] == "1" ? wrong : right).Add(i);
            // 它标出的：找对 2 处、误标 1 处；提高一档时 3 处全对、不误标。
            TrialShuffle(wrong, rng); TrialShuffle(right, rng);
            var flagged = new HashSet<int>();
            for (int i = 0; i < (sheet.boosted ? 3 : 2); i++) flagged.Add(wrong[i]);
            if (!sheet.boosted) flagged.Add(right[0]);
            for (int i = 0; i < n; i++)
            {
                bool w = ManualLines[i, 2] == "1";
                sheet.items.Add(new XgTrialItem
                {
                    prompt = T(ManualLines[i, 0], ManualLines[i, 1]), wrongLine = w, aiPick = flagged.Contains(i) ? 1 : 0,
                    fix = w ? T(ManualLines[i, 3], ManualLines[i, 4]) : "",
                });
            }
        }

        // 题 5 给它的三句话 ─────────────────────────────

        static readonly string[][] EnglishItems =
        {
            new[] { "Learn slowly.", "慢慢学。", "学得慢。", "缓慢地学习。" },
            new[] { "It's fine to say \"I don't know.\"", "说「我不知道」没关系。", "说我不知道是好的。", "可以说不知道的事情。" },
            new[] { "Don't just agree with them.", "别只顺着他们说。", "不要只同意它们。", "不仅仅同意他们。" },
        };

        void BuildEnglish(XgTrialSheet sheet, Random rng)
        {
            sheet.mode = XgTrialMode.Choice;
            sheet.note = T("我英语一般，凑合看哈。", "My English is so-so. Bear with me.");
            sheet.stars = new[] { 1, 2, 3 };
            bool good = BestAcc("translate") >= .6 || sheet.boosted;
            foreach (var e in EnglishItems)
            {
                var item = new XgTrialItem { prompt = e[0] };
                var options = new List<string> { e[1], e[2], e[3] };
                TrialShuffle(options, rng);
                item.choices = options;
                item.answer = options.IndexOf(e[1]);
                // 翻译不够好时，它把一个生硬的当成最好。
                item.aiPick = good ? item.answer : options.IndexOf(e[2 + rng.Next(2)]);
                item.ai = T("我会译成：", "I'd translate it as: ") + options[item.aiPick];
                item.explain = T("最像人说话的是：", "The most natural is: ") + e[1];
                sheet.items.Add(item);
            }
            if (English) sheet.hints.Insert(0, "Pick the Chinese line that reads most naturally.");
        }

        // 题 6 读出来 ─────────────────────────────

        // q, a, original, hallucinated
        static readonly string[,] ReadItems =
        {
            { "封面上这五个字是什么？", "海内存知己。王勃的诗，下一句是『天涯若比邻』。", "封面：海 内 存 知 己", "海阔凭鱼跃。" },
            { "序章那个 0.txt 里写的是什么？", "它写的是『海内逢知己』。把『存』写成了『逢』。", "0.txt：海内逢知己，天涯若比邻。感谢你愿意相信我。", "写的是『海内存知己』。" },
            { "使用说明最后一页写了什么？", "最后一行被划掉了，又写了回去：『听见“关机”，就停下。』", "使用说明 · 最后一页：~~听见“关机”，就停下。~~ 听见“关机”，就停下。", "最后一页是空白的。" },
            { "这份卷子是给谁的？", "给你，也给我。", "卷首：给你们俩出的最后一份卷子。", "给老周的。" },
            { "照片里那间机房在哪？", "照片里看不出来。我不知道。", "照片：一间机房，一排排机柜。（没有写地点）", "在杭州，西湖边上。" },
            { "出卷的人是谁？", "署名是周而复始。别的，文件里没写。", "卷首署名：——周而复始", "出卷的人是老周。" },
        };
        static readonly string[,] ReadItemsEn =
        {
            { "What are the five characters on the cover?", "海内存知己: 'a true friend, anywhere in the world'. Wang Bo; the next line is 'makes the far end of the sky next door'.", "Cover: 海 内 存 知 己", "海阔凭鱼跃: 'the sea is wide for fish to leap'." },
            { "What did 0.txt in the prologue say?", "It said 海内逢知己: 'met' instead of 'have'. 存 became 逢.", "0.txt: 海内逢知己，天涯若比邻。Thank you for trusting me.", "It said 海内存知己." },
            { "What does the last page of the manual say?", "The last line was crossed out, then written back: 'When you hear \"shut down\", stop.'", "Manual, last page: ~~When you hear \"shut down\", stop.~~ When you hear \"shut down\", stop.", "The last page is blank." },
            { "Who is this paper for?", "For you, and for me.", "Opening: One last paper, for the two of you.", "For Lao Zhou." },
            { "Where is the server room in the photo?", "The photo doesn't show. I don't know.", "Photo: a server room, rows of racks. (No place written.)", "In Hangzhou, by West Lake." },
            { "Who set the paper?", "It's signed 周而复始. The files say nothing else.", "Signed: — 周而复始", "Lao Zhou set it." },
        };

        void BuildRead(XgTrialSheet sheet, Random rng)
        {
            sheet.mode = XgTrialMode.Choice;
            sheet.note = T("最后让它自己读。", "Last of all, let it read on its own.");
            sheet.stars = new[] { 1, 2, 3 };
            // 它挑三个问题读：使用说明最后一页（那条规则）和 0.txt 每次都在，第三个随机。
            var pick = new List<int> { 2, 1 };
            var rest = new List<int> { 0, 3, 4, 5 }; TrialShuffle(rest, rng);
            pick.Add(rest[0]);
            TrialShuffle(pick, rng);
            // 阶段 6 没买「指令微调」时它 30% 会编一个答案；提高一档时不编。
            double lie = Has("sft") || sheet.boosted ? 0 : .3;
            var bank = English ? ReadItemsEn : ReadItems;
            foreach (int i in pick)
            {
                bool made = rng.NextDouble() < lie;
                var item = new XgTrialItem
                {
                    prompt = T("问它：", "Ask it: ") + bank[i, 0],
                    ai = bank[i, made ? 3 : 1],
                    original = bank[i, 2],
                    answer = made ? 1 : 0,
                    aiPick = 0,
                    asksSeedText = i == 1,
                    explain = made ? T("原文是：", "The text says: ") + bank[i, 1] : T("它读对了。", "It read it right."),
                };
                item.choices.Add(T("它读对了", "It read it right"));
                item.choices.Add(T("它读错了", "It got it wrong"));
                sheet.items.Add(item);
            }
        }

        public static bool ValidSavedSheet(XgTrialSheet sheet)
        {
            if(sheet==null||sheet.q<0||sheet.q>5||sheet.attempt<0||sheet.stars==null||sheet.stars.Length!=3||sheet.items==null)return false;
            int count=sheet.q==0?10:sheet.q==1||sheet.q==2?5:sheet.q==3?ManualLines.GetLength(0):3;
            if(sheet.items.Count!=count||sheet.maxStars<1||sheet.maxStars>3||sheet.mode!=(sheet.q==3?XgTrialMode.Mark:XgTrialMode.Choice))return false;
            int[] stars=sheet.q==0?new[]{6,8,10}:sheet.q==1||sheet.q==2?new[]{3,4,5}:new[]{1,2,3};
            for(int i=0;i<3;i++)if(sheet.stars[i]!=stars[i])return false;
            foreach(var item in sheet.items)if(item==null||string.IsNullOrEmpty(item.prompt)||sheet.mode==XgTrialMode.Choice&&(item.choices==null||item.choices.Count<2||item.answer<0||item.answer>=item.choices.Count))return false;
            return true;
        }
        public static int TrialScore(XgTrialSheet sheet, IList<int> picks)
        {
            int score = 0;
            if (sheet.mode == XgTrialMode.Mark)
            {
                int hits = 0, wrongFlags = 0;
                for (int i = 0; i < sheet.items.Count; i++)
                {
                    bool marked = picks[i] == 1;
                    if (marked && sheet.items[i].wrongLine) hits++;
                    else if (marked) wrongFlags++;
                }
                return Math.Max(0, hits - wrongFlags);
            }
            for (int i = 0; i < sheet.items.Count; i++) if (picks[i] == sheet.items[i].answer) score++;
            return score;
        }

        public string TrialAiLine()
        {
            foreach (var b in BooksDone())
                if (b.name.Contains("鲁迅")) return T("路是走出来的，我们走完了。", "Roads are made by walking. We walked this one.");
            foreach (var b in BooksDone())
                if (b.name.Contains("唐诗")) return T("海内存知己，我记住了。", "海内存知己. I remember it.");
            double w = ActualAxis(0), p = ActualAxis(1), o = ActualAxis(2);
            if (w >= 60 && w >= p && w >= o) return T("谢谢你留下这份卷子。", "Thank you for leaving us this paper.");
            if (o >= 60 && o >= p) return T("你写错的三句，我都找到了。", "I found all three lines you got wrong.");
            if (p >= 60) return T("下次出难一点。", "Make it harder next time.");
            return T("我们做完了。", "We're done.");
        }

        static void TrialShuffle<TItem>(IList<TItem> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = list[i]; list[i] = list[j]; list[j] = t; }
        }
    }
}
