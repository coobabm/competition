using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// A generated 摆渡百科 (Bodu Encyclopedia) article for the crawler (<see cref="XgSim.StartCrawl"/>): a title, a lead,
    /// sections of paragraphs, related news and a reference list, about a 2016 topic (graphics cards, internet cafés,
    /// danmaku, Tang poems, memes, Go, live streaming, captchas). Every block is a list of words, so the crawler knows
    /// where each word starts and ends: Chinese is segmented by hand in the templates ("显卡/是/…"), outside text
    /// (poems, news headlines, hot words) by <see cref="Segment"/>. Chinese and English pages come from paired
    /// templates; corpus text that has no English (hot words, poem lines) only appears on Chinese pages.
    /// </summary>
    public sealed class XgCrawlArticle
    {
        public enum Kind { Title, Lead, Heading, Paragraph, News, Reference }

        public sealed class Block
        {
            public Kind kind;
            public readonly List<string> words = new List<string>();
            /// <summary>The block's text: words joined (Chinese without spaces, English with).</summary>
            public string Text(bool english)
            {
                if (!english) return string.Concat(words);
                var sb = new StringBuilder();
                foreach (var w in words) { if (sb.Length > 0 && !IsClosingPunctuation(w)) sb.Append(' '); sb.Append(w); }
                return sb.ToString();
            }
            /// <summary>Character index where each word starts in <see cref="Text"/>.</summary>
            public int[] Starts(bool english)
            {
                var starts = new int[words.Count];
                int at = 0;
                for (int i = 0; i < words.Count; i++)
                {
                    if (english && i > 0 && !IsClosingPunctuation(words[i])) at++;
                    starts[i] = at;
                    at += words[i].Length;
                }
                return starts;
            }
        }

        public string title = "", slug = "";
        public bool english;
        public readonly List<Block> blocks = new List<Block>();

        /// <summary>Words a foot can take (anything with a letter or digit; punctuation is not).</summary>
        public static bool Grabbable(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            foreach (char c in word) if (char.IsLetterOrDigit(c)) return true;
            return false;
        }

        static bool IsClosingPunctuation(string w) => w.Length == 1 && ",.;:!?)]".IndexOf(w[0]) >= 0;

        public int WordCount { get { int n = 0; foreach (var b in blocks) foreach (var w in b.words) if (Grabbable(w)) n++; return n; } }

        // ───────────── segmentation ─────────────

        static bool Cjk(char c) => c >= 0x3400 && c <= 0x9FFF;

        /// <summary>
        /// Splits outside text into words: Latin and digit runs stay whole, Chinese runs are cut into two-character
        /// words (a single last character joins the word before it), punctuation stands alone, spaces are dropped.
        /// </summary>
        public static List<string> Segment(string text)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(text)) return list;
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (Cjk(c))
                {
                    int j = i;
                    while (j < text.Length && Cjk(text[j])) j++;
                    int run = j - i;
                    for (int k = 0; k < run;)
                    {
                        int len = run - k == 3 ? 3 : Math.Min(2, run - k);
                        list.Add(text.Substring(i + k, len));
                        k += len;
                    }
                    i = j;
                }
                else if (char.IsLetterOrDigit(c))
                {
                    int j = i;
                    while (j < text.Length && !Cjk(text[j]) && (char.IsLetterOrDigit(text[j]) || text[j] == '.' || text[j] == '-' || text[j] == '\'')) j++;
                    list.Add(text.Substring(i, j - i));
                    i = j;
                }
                else { list.Add(text.Substring(i, 1)); i++; }
            }
            return list;
        }

        /// <summary>A hand-segmented Chinese template ("显卡/是/…") or an English sentence (split at spaces).</summary>
        static IEnumerable<string> Words(string line, bool english)
        {
            if (english)
            {
                foreach (var w in line.Split(' '))
                {
                    if (w.Length == 0) continue;
                    // A trailing comma or full stop is its own (untakeable) token so the foot lands on the word.
                    char last = w[w.Length - 1];
                    if (w.Length > 1 && ",.;:!?".IndexOf(last) >= 0) { yield return w.Substring(0, w.Length - 1); yield return last.ToString(); }
                    else yield return w;
                }
            }
            else foreach (var w in line.Split('/')) if (w.Length > 0) yield return w;
        }

        // ───────────── content ─────────────

        sealed class Topic
        {
            public string zh, en, slugZh, slugEn;
            public int since = 20150101;
            /// <summary>Lead sentence (zh, en).</summary>
            public string[] lead;
            /// <summary>Section headings, zh and en in turn.</summary>
            public string[] headings;
            /// <summary>Fact sentences, zh and en in turn.</summary>
            public string[] facts;
            /// <summary>Extra corpus paragraph (poems, hot words); may return null.</summary>
            public Func<Random, int, bool, string[]> extra;
        }

        static readonly Topic[] Topics =
        {
            new Topic
            {
                zh = "显卡", en = "Graphics card", slugZh = "显卡", slugEn = "graphics-card",
                lead = new[] { "显卡/（/英文/：/graphics card/）/是/电脑/里/专门/负责/画图/的/部件/，/近年/也/被/拿来/训练/神经网络/。",
                               "A graphics card is the part of a computer that draws the picture, and lately it also trains neural networks." },
                headings = new[] { "工作/原理", "How it works", "选购/常识", "Buying advice", "网友/评价", "What people say" },
                facts = new[]
                {
                    "显卡/上/有/成百上千/个/小/核心/，/同时/算/很多/简单/的/乘法/。", "A card holds hundreds of small cores that do many simple multiplications at once.",
                    "显存/决定/一次/能/装下/多大/的/模型/，/不够/就/会/报错/。", "Video memory decides how big a model fits at once; run out and it crashes.",
                    "2016年/5月/，/GTX 1080/国行/上市/，/售价/4999/元/起/。", "In May 2016 the GTX 1080 went on sale in China from 4,999 yuan.",
                    "温度/超过/80/度/时/，/显卡/会/自动/降频/保护/自己/。", "Above 80 degrees the card slows itself down to stay safe.",
                    "二手/矿卡/价格/便宜/，/但/风扇/往往/已经/磨损/。", "Second-hand mining cards are cheap, but their fans are often worn out.",
                    "显卡吧/的/老哥/常说/「/买新不买旧/」/，/也/有人/坚持/等等党/。", "Forum regulars say buy new, never old; others swear by waiting for the next one.",
                    "网吧/常用/的/中端/显卡/能/流畅/运行/英雄联盟/。", "The mid-range cards in internet cafés run League of Legends smoothly.",
                    "深度学习/框架/大多/需要/英伟达/显卡/和/CUDA/驱动/。", "Most deep learning frameworks need an Nvidia card and the CUDA driver.",
                },
            },
            new Topic
            {
                zh = "网吧", en = "Internet café", slugZh = "网吧", slugEn = "internet-cafe",
                lead = new[] { "网吧/是/按/小时/出租/上网/电脑/的/场所/，/2016年/全国/仍有/十多万/家/。",
                               "An internet café rents out computers by the hour; in 2016 China still had over a hundred thousand of them." },
                headings = new[] { "发展/历史", "History", "上网/须知", "House rules", "网吧/文化", "Café culture" },
                facts = new[]
                {
                    "上网/前/需要/出示/身份证/，/未成年人/禁止/入内/。", "You show your ID card before logging on; minors are not allowed in.",
                    "包夜/一般/从/晚上/十点/到/次日/早上/八点/，/价格/实惠/。", "An overnight session runs from ten at night to eight in the morning and is cheap.",
                    "网管/会/在/机器/坏掉/时/喊/一声/「/重启/试试/」/。", "When a machine breaks, the attendant shouts: try restarting it.",
                    "泡面/和/冰红茶/是/网吧/最/畅销/的/商品/。", "Instant noodles and iced tea are the café's best sellers.",
                    "近年/很多/网吧/改名/叫/网咖/，/换上/了/电竞椅/。", "Lately many cafés renamed themselves e-sports lounges and bought gaming chairs.",
                    "开黑/的/玩家/常常/连着/坐/一排/，/戴着/耳机/大喊/。", "Teams of players sit in a row with headsets on, shouting at each other.",
                    "网吧/的/电脑/开机/后/会/自动/还原/系统/。", "Café computers restore a clean system every time they boot.",
                },
            },
            new Topic
            {
                zh = "弹幕", en = "Danmaku", slugZh = "弹幕", slugEn = "danmaku",
                lead = new[] { "弹幕/是/视频/播放/时/从/画面/上/飘过/的/评论/，/在/年轻/网友/中/非常/流行/。",
                               "Danmaku are comments that fly across a video while it plays, hugely popular with young viewers." },
                headings = new[] { "起源", "Origins", "常见/弹幕", "Common comments", "争议", "Controversy" },
                facts = new[]
                {
                    "弹幕/最早/来自/日本/的/视频/网站/，/后来/传入/国内/。", "Danmaku came from a Japanese video site before spreading here.",
                    "高能/片段/前/常有/人/发/「/前方高能/」/提醒/。", "Before a big moment someone always warns: hype incoming.",
                    "有人/觉得/弹幕/挡住/画面/，/于是/有了/关闭/按钮/。", "Some find the comments block the picture, hence the off switch.",
                    "很多/弹幕/是/反话/，/夸/和/骂/很难/分清/。", "Plenty of comments are sarcastic, so praise and mockery are hard to tell apart.",
                    "直播/平台/也/用/弹幕/，/主播/会/念出来/回应/。", "Live-streaming sites use danmaku too; streamers read them out and reply.",
                    "弹幕/情绪/分析/是/一种/文本/分类/任务/。", "Telling the mood of a comment is a text classification task.",
                },
                extra = HotWordParagraph("D"),
            },
            new Topic
            {
                zh = "唐诗", en = "Tang poetry", slugZh = "唐诗", slugEn = "tang-poetry",
                lead = new[] { "唐诗/泛指/唐代/诗人/创作/的/诗歌/，/《/全唐诗/》/收录/近/五万/首/。",
                               "Tang poetry means the poems of the Tang dynasty; the Complete Tang Poems collects nearly fifty thousand." },
                headings = new[] { "体裁", "Forms", "代表/作品", "Famous poems", "流传", "Legacy" },
                facts = new[]
                {
                    "五言/绝句/每句/五个/字/，/全诗/四句/。", "A five-character quatrain has four lines of five characters each.",
                    "李白/和/杜甫/并称/「/李杜/」/，/影响/深远/。", "Li Bai and Du Fu are named together and shaped all that came after.",
                    "小学/课本/里/收录/了/不少/唐诗/，/人人/会/背/。", "Primary-school textbooks carry many Tang poems that everyone can recite.",
                    "网友/常/改写/名句/，/比如/把/明月/换成/WiFi/。", "Netizens love rewriting famous lines, swapping the moon for WiFi.",
                    "用/循环/神经网络/写/唐诗/，/是/今年/很/火/的/玩法/。", "Writing Tang poems with a recurrent neural network is this year's craze.",
                    "律诗/讲究/对仗/和/平仄/，/规矩/很多/。", "Regulated verse demands parallel lines and set tones, with many rules.",
                },
                extra = PoemParagraph,
            },
            new Topic
            {
                zh = "表情包", en = "Meme stickers", slugZh = "表情包", slugEn = "meme-stickers",
                lead = new[] { "表情包/是/聊天/时/用来/代替/文字/的/图片/，/常/配有/一句/短/话/。",
                               "Meme stickers are pictures sent in chats instead of words, often with a short caption." },
                headings = new[] { "常见/类型", "Kinds", "斗图", "Sticker battles", "版权/问题", "Copyright" },
                facts = new[]
                {
                    "金馆长/和/暴走/漫画/是/最早/一批/流行/的/表情/。", "Panda-man and rage comics were among the first to catch on.",
                    "斗图/就是/群里/互相/发/表情包/，/谁/接不上/谁/输/。", "A sticker battle is trading stickers in a group; whoever runs out loses.",
                    "一张/好/的/表情包/要/看/脸/，/不看/字/。", "A good sticker is about the face, not the caption.",
                    "不少/明星/的/截图/被/做成/表情/，/流传/很广/。", "Plenty of celebrity screenshots became stickers and spread everywhere.",
                    "表情包/的/情绪/识别/难/在/反讽/。", "The hard part of reading a sticker's mood is irony.",
                    "QQ/和/微信/都/有/表情/商店/，/也/能/自己/收藏/。", "QQ and WeChat both have sticker shops, and you can save your own.",
                },
                extra = HotWordParagraph("M"),
            },
            new Topic
            {
                zh = "围棋", en = "Go", slugZh = "围棋", slugEn = "go",
                lead = new[] { "围棋/是/起源/于/中国/的/棋类/游戏/，/黑白/双方/在/棋盘/上/争夺/地盘/。",
                               "Go is a board game from China in which black and white compete for territory." },
                headings = new[] { "规则", "Rules", "人机/大战", "Man versus machine", "段位", "Ranks" },
                facts = new[]
                {
                    "一块/棋/没有/气/就/会/被/提掉/。", "A group with no liberties left is captured.",
                    "2016年/3月/，/AlphaGo/以/4/比/1/战胜/李世石/。", "In March 2016 AlphaGo beat Lee Sedol four games to one.",
                    "AlphaGo/用/了/卷积/神经网络/和/蒙特卡洛/树/搜索/。", "AlphaGo used convolutional networks and Monte Carlo tree search.",
                    "9路/小/棋盘/适合/初学者/练习/死活/。", "The small 9×9 board suits beginners practising life and death.",
                    "职业/棋手/从/初段/到/九段/，/九段/最高/。", "Professionals rank from first dan to ninth, the highest.",
                    "很多/人/在/网上/对局/，/输了/就/说/网卡/了/。", "Lots of people play online and blame the connection when they lose.",
                },
            },
            new Topic
            {
                zh = "网络直播", en = "Live streaming", slugZh = "网络直播", slugEn = "live-streaming",
                lead = new[] { "网络直播/是/主播/通过/摄像头/实时/播出/画面/，/观众/可以/送/礼物/互动/。",
                               "In live streaming a host broadcasts from a webcam in real time and viewers can send gifts." },
                headings = new[] { "平台", "Platforms", "主播", "Hosts", "监管", "Regulation" },
                facts = new[]
                {
                    "2016年/被/称为/直播/元年/，/新/平台/一个/接/一个/上线/。", "2016 is called the first year of live streaming, with new sites every month.",
                    "游戏/直播/最/火/，/很多/主播/原本/是/职业/选手/。", "Game streams are the hottest; many hosts used to be pro players.",
                    "观众/刷/的/礼物/可以/换成/真钱/，/平台/抽成/。", "Viewers' gifts turn into real money, and the site takes a cut.",
                    "有些/直播间/的/人气/是/刷/出来/的/。", "Some rooms' viewer counts are faked.",
                    "主播/要/一边/玩/一边/看/弹幕/，/很/考验/反应/。", "A host plays and reads comments at the same time, which takes quick wits.",
                    "手机/直播/让/每个/人/都/能/开播/。", "Phone streaming lets anyone go live.",
                },
            },
            new Topic
            {
                zh = "验证码", en = "Captcha", slugZh = "验证码", slugEn = "captcha",
                lead = new[] { "验证码/是/区分/人/和/程序/的/小/测试/，/通常/是/扭曲/的/文字/或/图片/。",
                               "A captcha is a little test that tells people from programs, usually warped text or pictures." },
                headings = new[] { "种类", "Kinds", "12306", "The 12306 captcha", "破解", "Cracking" },
                facts = new[]
                {
                    "12306/的/图片/验证码/要/点/出/所有/符合/描述/的/图/。", "The 12306 captcha asks you to click every picture that matches.",
                    "春运/期间/很多/人/被/验证码/难住/，/抢不到/票/。", "During the Spring Festival rush many fail the captcha and miss their tickets.",
                    "抢票/软件/会/把/验证码/发给/打码/平台/人工/识别/。", "Ticket-grabbing apps send captchas to typing farms where people solve them.",
                    "卷积/神经网络/已经/能/认出/很多/文字/验证码/。", "Convolutional networks already read many text captchas.",
                    "扭曲/和/干扰线/是/为了/让/程序/认/不出来/。", "The warping and noise lines are there to confuse programs.",
                    "有人/说/验证码/是/在/免费/帮/网站/标/数据/。", "Some say solving captchas is labelling data for free.",
                },
            },
        };

        static readonly string[] Filler =
        {
            "本/词条/由/网友/共同/编辑/，/如/有/错误/欢迎/指正/。", "This entry is edited by users; corrections are welcome.",
            "该/说法/尚/有/争议/，/需要/可靠/来源/。", "This claim is disputed and needs a reliable source.",
            "以上/内容/仅/供/参考/，/具体/以/官方/为准/。", "The above is for reference only; official sources take precedence.",
            "点击/下方/链接/可以/查看/更多/相关/词条/。", "Follow the links below for related entries.",
        };

        static readonly string[] Sources =
        {
            "《/电脑报/》/2016年/第{0}期", "China Computer News, 2016 issue {0}",
            "新浪/科技/：/相关/报道/，/2016年{1}月", "Sina Tech: coverage, {1}/2016",
            "摆渡/贴吧/：/网友/讨论/帖", "Bodu Tieba: forum thread",
            "《/大众/软件/》/2016年/第{0}期", "Popular Software, 2016 issue {0}",
            "中关村/在线/：/评测/文章", "ZOL: review article",
            "知乎/：/相关/问题/回答", "Zhihu: answers to a related question",
        };

        /// <summary>The hot words of a destination ("D" danmaku, "M" memes) as a quote list; Chinese pages only.</summary>
        static Func<Random, int, bool, string[]> HotWordParagraph(string destination) => (rng, today, english) =>
        {
            if (english) return null;
            var words = XgMemes.HotWordsFor(destination, today);
            if (words.Count == 0) return null;
            var sb = new StringBuilder("常见/的/有/");
            int n = Math.Min(5, words.Count);
            for (int i = 0; i < n; i++)
            {
                int j = i + rng.Next(words.Count - i);
                var w = words[j]; words[j] = words[i]; words[i] = w;
                sb.Append(i > 0 ? "/、/「/" : "「/").Append(string.Join("/", Segment(w.word))).Append("/」");
            }
            sb.Append("/等/。");
            return new[] { sb.ToString() };
        };

        static string[] PoemParagraph(Random rng, int today, bool english)
        {
            var poem = XgCatalog.Poems[rng.Next(XgCatalog.Poems.Length)];
            if (english) return new[] { poem.titleEn + " by " + poem.authorEn + " is one of the most recited poems in China." };
            var sb = new StringBuilder("《/" + string.Join("/", Segment(poem.title)) + "/》/是/" + string.Join("/", Segment(poem.author)) + "/的/作品/：/「");
            for (int i = 0; i < poem.count; i++) sb.Append(i > 0 ? "/，/" : "/").Append(string.Join("/", Segment(XgCatalog.PoemLines[poem.first + i])));
            sb.Append("/」/。");
            return new[] { sb.ToString() };
        }

        /// <summary>English names of the hot topics in <see cref="XgMemes.Topics"/>.</summary>
        static readonly Dictionary<string, string> TopicEn = new Dictionary<string, string>
        {
            { "太阳的后裔", "Descendants of the Sun" }, { "AlphaGo", "AlphaGo" }, { "科比", "Kobe's farewell" }, { "papi酱", "Papi Jiang" },
            { "集五福", "the Five Blessings cards" }, { "老司机", "\"old drivers\"" }, { "友谊的小船", "\"the boat of friendship\"" }, { "叶问3", "Ip Man 3" },
            { "魔兽", "the Warcraft film" }, { "欧洲杯", "Euro 2016" }, { "葛优躺", "the Ge You slouch" }, { "里约奥运", "the Rio Olympics" },
            { "洪荒之力", "\"primordial power\"" }, { "阴阳师", "Onmyoji" }, { "iPhone 7", "iPhone 7" }, { "蓝瘦香菇", "\"blue thin mushroom\"" },
            { "双11", "Singles' Day" }, { "厉害了我的哥", "\"awesome, bro\"" }, { "你的名字", "Your Name" },
        };

        // ───────────── generation ─────────────

        /// <summary>
        /// One article. <paramref name="headlines"/> are news headlines already out by <paramref name="today"/> as
        /// zh, en pairs (the desktop passes the era news); null or empty leaves the news section out.
        /// </summary>
        public static XgCrawlArticle Make(int seed, int today, bool english, IList<string[]> headlines = null)
        {
            var rng = new Random(seed);
            var open = new List<Topic>();
            foreach (var t in Topics) if (t.since <= today) open.Add(t);
            var topic = open[((seed % open.Count) + open.Count) % open.Count];
            var a = new XgCrawlArticle { english = english, title = english ? topic.en : topic.zh, slug = english ? topic.slugEn : topic.slugZh };
            a.Add(Kind.Title, english ? topic.en : topic.zh, english);
            a.Add(Kind.Lead, Pick(topic.lead, 0, english), english);

            // Facts shuffled once so no sentence repeats on the page.
            var order = new List<int>();
            for (int i = 0; i < topic.facts.Length / 2; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int x = order[i]; order[i] = order[j]; order[j] = x; }
            int next = 0;
            var fillersUsed = new HashSet<int>();
            int sections = topic.headings.Length / 2;
            for (int s = 0; s < sections; s++)
            {
                a.Add(Kind.Heading, Pick(topic.headings, s, english), english);
                int paragraphs = 1 + rng.Next(2);
                for (int p = 0; p < paragraphs; p++)
                {
                    var block = new Block { kind = Kind.Paragraph };
                    int sentences = 2 + rng.Next(2);
                    for (int k = 0; k < sentences && next < order.Count; k++) block.words.AddRange(Words(Pick(topic.facts, order[next++], english), english));
                    if (rng.NextDouble() < .35)
                    {
                        // Each boilerplate line at most once a page.
                        int f = rng.Next(Filler.Length / 2);
                        if (fillersUsed.Add(f)) block.words.AddRange(Words(Pick(Filler, f, english), english));
                    }
                    if (block.words.Count > 0) a.blocks.Add(block);
                }
                if (s == 1 && topic.extra != null)
                {
                    var extra = topic.extra(rng, today, english);
                    if (extra != null) foreach (var line in extra) a.Add(Kind.Paragraph, line, english);
                }
            }

            // Today's hot topic, as an encyclopedia "see also".
            string hot = XgMemes.TopicOf(today);
            if (hot.Length > 0)
            {
                if (!english) a.Add(Kind.Paragraph, "近期/热门/词条/：/" + string.Join("/", Segment(hot)) + "/。", false);
                else if (TopicEn.TryGetValue(hot, out var hotEn)) a.Add(Kind.Paragraph, "Trending entry: " + hotEn + ".", true);
            }

            if (headlines != null && headlines.Count > 0)
            {
                a.Add(Kind.Heading, english ? "Related news" : "相关/新闻", english);
                // Three of the eight newest (the list is in date order), newest first.
                var picked = new List<int>();
                int window = Math.Min(8, headlines.Count);
                while (picked.Count < Math.Min(3, window))
                {
                    int index = headlines.Count - 1 - rng.Next(window);
                    if (!picked.Contains(index)) picked.Add(index);
                }
                picked.Sort((x, y) => y.CompareTo(x));
                foreach (int index in picked)
                {
                    var h = headlines[index];
                    if (h == null || h.Length < 2) continue;
                    var block = new Block { kind = Kind.News };
                    block.words.Add("·");
                    block.words.AddRange(english ? Words(h[1], true) : Segment(h[0]));
                    a.blocks.Add(block);
                }
            }

            a.Add(Kind.Heading, english ? "References" : "参考/资料", english);
            int refs = 3 + rng.Next(3);
            int month = Math.Max(1, Math.Min(12, today / 100 % 100));
            for (int i = 0; i < refs; i++)
            {
                string line = Pick(Sources, rng.Next(Sources.Length / 2), english);
                line = string.Format(line, 1 + rng.Next(Math.Max(1, month * 4)), 1 + rng.Next(month));
                var block = new Block { kind = Kind.Reference };
                block.words.Add("[" + (i + 1) + "]");
                block.words.AddRange(Words(line, english));
                a.blocks.Add(block);
            }
            return a;
        }

        static string Pick(string[] pairs, int index, bool english) => pairs[index * 2 + (english ? 1 : 0)];

        void Add(Kind kind, string line, bool english)
        {
            var b = new Block { kind = kind };
            b.words.AddRange(Words(line, english));
            if (b.words.Count > 0) blocks.Add(b);
        }

        /// <summary>The page's address in the fake browser bar.</summary>
        public string Url => "baike.bodu.com/item/" + slug;
    }
}
