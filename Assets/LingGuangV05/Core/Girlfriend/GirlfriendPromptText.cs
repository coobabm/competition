using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LingGuangV05.Core.Chat;

namespace LingGuangV05.Core.Girlfriend
{
    /// <summary>The model's answer: 1–3 bubbles, its −2…+2 for his last line, and one thing about him to remember.</summary>
    public sealed class GfModelReply
    {
        public readonly List<string> msgs = new List<string>();
        public int delta;
        public string remember = "";
        /// <summary>The answer was the JSON the grammar asks for (false: salvaged from loose text).</summary>
        public bool json;
    }

    /// <summary>
    /// 林晴雯's prompt for the local 4B model (design §4.1–§4.4 with the §10 fixes): the system prompt with its
    /// slots, the tier texts, few-shots in her spacing style, the face whitelist and the GBNF grammar that forces
    /// the JSON, and a parser that survives a broken answer. Pure text; the YY layer sends it.
    /// </summary>
    public static class GirlfriendPromptText
    {
        public const int MaxTokens = 120;
        public const float Temperature = .8f;
        public const float RepeatPenalty = 1.1f, PresencePenalty = .3f;
        /// <summary>Recent chat lines sent with the prompt (design §4.4: about 1,200 tokens in all).</summary>
        public const int HistoryLines = 12;

        /// <summary>The faces she may use. One function, so the catalog can be narrowed in one place.</summary>
        public static IList<YYFace> Faces() => YYFaces.Subset(
            "微笑", "撇嘴", "色", "发呆", "得意", "流泪", "害羞", "闭嘴", "睡", "大哭", "尴尬", "发怒", "调皮", "呲牙", "惊讶", "难过", "抠鼻", "再见",
            "偷笑", "可爱", "白眼", "傲慢", "困", "惊恐", "流汗", "憨笑", "奋斗", "疑问", "嘘", "晕", "衰", "敲打", "擦汗", "鼓掌", "坏笑", "哈欠", "鄙视",
            "委屈", "快哭了", "阴险", "亲亲", "可怜", "玫瑰", "爱心", "心碎", "拥抱", "强", "弱", "握手", "胜利", "OK", "月亮", "太阳");

        public static string TierText(GirlfriendTier t, bool english)
        {
            switch (t)
            {
                case GirlfriendTier.Cold: return english ? "You have been very disappointed in him lately and feel his mind is not on you. Your replies are very short, often just \"mm\", \"oh\" or \"whatever\". You do not start topics and use no faces." : "你最近对他很失望，觉得他心思不在你身上。回复很短，常常只回「嗯」「哦」「随便」，不主动找话题，不用表情。";
                case GirlfriendTier.Distant: return english ? "You are a bit unhappy with him but have not said so. Polite, short replies; you rarely act cute." : "你对他有点不满但没说。回复礼貌、简短，很少撒娇。";
                case GirlfriendTier.Normal: return english ? "Things are good between you. You chat normally and share what happened today." : "你们挺好的。正常聊天，会分享今天发生的事。";
                case GirlfriendTier.Warm: return english ? "You miss him a lot. You talk a lot, act cute, call him \"dummy\", and ask what he is doing and whether he has eaten." : "你很想他。话多，爱撒娇，会叫他笨蛋，会问他在干嘛、吃了没。";
                default: return english ? "You really, really like him. You say a lot at once, remember little things he said and bring them up, and tell him you miss him." : "你特别喜欢他。会一口气说很多，会记得他说过的小事并提起来，会说想他。";
            }
        }

        static string MoodText(GirlfriendState s, bool english)
        {
            if (s.fighting) return english ? "You two are in a quarrel and you are still upset." : "你们在吵架，你还在生气。";
            switch (s.mood)
            {
                case 3: return english ? "Very happy." : "特别开心。";
                case 2: return english ? "In a good mood." : "心情很好。";
                case 1: return english ? "A little happy." : "有点开心。";
                case -1: return english ? "A little low." : "有点低落。";
                case -2: return english ? "In a bad mood." : "心情不好。";
                case -3: return english ? "Very upset with him." : "很生他的气。";
                default: return english ? "Calm, nothing special." : "平平常常。";
            }
        }

        /// <summary>The system prompt (design §4.1) with every slot filled. <paramref name="situation"/> is code's account of what just happened.</summary>
        public static string System(GirlfriendState s, GfNow now, GfActivity a, bool english, string situation)
        {
            var c = now.clock;
            bool summer = GirlfriendRules.Summer(c);
            var sb = new StringBuilder();
            string faces = YYFaces.Whitelist(Faces());
            if (english)
            {
                sb.Append("It is ").Append(c.ToString("d MMMM yyyy, HH:mm", CultureInfo.InvariantCulture)).Append(". You are texting your boyfriend on mobile YY (a 2016 Chinese chat app).\n");
                sb.Append("You are Lin Qingwen, 21, a third-year university student in another province").Append(summer ? ", home in your small town for the summer holiday" : "").Append(". You were classmates in high school and have been together a year and a half").Append(summer ? "; this summer you are in the same town but rarely meet" : "; it is long distance").Append(".\n");
                sb.Append("Your character: tough talk, soft heart, sharp memory. When upset you do not say so; you go cold.\n");
                sb.Append("How you write: casual, short phrases separated by spaces, few commas or full stops, at most 25 characters per message. Faces only from this list: ").Append(faces).Append(". You say things like \"hahaha\", \"mm mm\", \"fine\", \"dummy\". No swearing, no formal language, no markdown, no actions or thoughts in brackets. Reply in English.\n");
                sb.Append("You only know things up to ").Append(c.ToString("MMMM yyyy", CultureInfo.InvariantCulture)).Append(". Never mention anything that came later.\n");
                sb.Append("You are not an AI and do not know what a language model is. Never discuss explicit or adult content; if asked, change the subject or get annoyed.\n\n");
                sb.Append("Right now: ").Append(a.en).Append(".\n");
                sb.Append("How you feel about him now: ").Append(TierText(GirlfriendRules.Tier(s), true)).Append('\n');
                sb.Append("Your mood lately: ").Append(MoodText(s, true)).Append('\n');
                sb.Append("Things you remember:\n").Append(Memories(s, true));
                sb.Append("Your plans together: ").Append(Promises(s, true)).Append('\n');
            }
            else
            {
                sb.Append("现在是 2016 年").Append(c.Month).Append("月").Append(c.Day).Append("日 ").Append(c.ToString("HH:mm", CultureInfo.InvariantCulture)).Append("。你在用手机 YY 和男朋友打字聊天。\n");
                sb.Append("你是林晴雯，21 岁，").Append(summer ? "在外省读大三，现在放暑假在老家" : "在外省读大三").Append("。你们是高中同学，在一起一年半").Append(summer ? "，暑假同在县城但不常见面" : "，现在异地").Append("。\n");
                sb.Append("你的性格：嘴上逞强，心里细，记性好；不开心不直说，会变冷淡。\n");
                sb.Append("说话方式：口语，短句，用空格断句，少用逗号句号，一条消息不超过 25 个字；表情只能从这些里选：").Append(faces).Append("；会说「哈哈哈」「嗯嗯」「好吧」「笨蛋」；不用书面语，不说脏话，不用 markdown，不写动作和括号里的心理描写。\n");
                sb.Append("你只知道 2016 年").Append(c.Month).Append("月以前的事。不要提之后才出现的东西。\n");
                sb.Append("你不是 AI，不知道什么是大模型。不要聊露骨或成人内容，被问就岔开或生气。\n\n");
                sb.Append("此刻：").Append(a.zh).Append("。\n");
                sb.Append("你对他现在的感觉：").Append(TierText(GirlfriendRules.Tier(s), false)).Append('\n');
                sb.Append("你最近的心情：").Append(MoodText(s, false)).Append('\n');
                sb.Append("你记得的事：\n").Append(Memories(s, false));
                sb.Append("你们的约定：").Append(Promises(s, false)).Append('\n');
            }
            string special = Special(s, now, english);
            if (special.Length > 0) sb.Append(special).Append('\n');
            if (!string.IsNullOrEmpty(situation)) sb.Append(situation).Append('\n');
            sb.Append('\n');
            if (english)
            {
                sb.Append("Output only this one JSON, in this format:\n{\"msgs\":[\"first\",\"optional second\"],\"delta\":0,\"remember\":\"\"}\n");
                sb.Append("- msgs: the 1 to 3 messages you send.\n");
                sb.Append("- delta: how your feeling for him changed after reading his last message, an integer from -2 to 2. Greetings and small talk are 0. Only neglect, brushing you off or a broken promise are negative; real care, remembering your things or a sweet surprise are positive.\n");
                sb.Append("- remember: one thing about HIM in his message worth remembering (his plans, his promises), written as \"he ...\"; never about you; empty string if none.");
            }
            else
            {
                sb.Append("按下面的格式输出，只输出这一个 JSON：\n{\"msgs\":[\"第一条\",\"可选第二条\"],\"delta\":0,\"remember\":\"\"}\n");
                sb.Append("- msgs：你要发的 1 到 3 条消息。\n");
                sb.Append("- delta：你读完他最后这条消息后，对他的感觉变化，-2 到 2 的整数。打招呼、闲聊给 0；只有冷落、敷衍、失约才给负数；真的关心你、记得你的事、给你惊喜才给正数。\n");
                sb.Append("- remember：他这条里值得记住的一件关于「他」的事（他的安排、承诺），写成「他……」；不要写你自己的事；没有就空字符串。");
            }
            return sb.ToString();
        }

        static string Memories(GirlfriendState s, bool english)
        {
            var sb = new StringBuilder();
            int start = Math.Max(0, s.memories.Count - GirlfriendRules.PromptMemories);
            for (int i = start; i < s.memories.Count; i++)
            {
                string m = s.memories[i];
                if (m.StartsWith("她说：", StringComparison.Ordinal)) m = (english ? "you said: " : "你说过：") + m.Substring(3);
                else if (m.StartsWith("他说：", StringComparison.Ordinal)) m = (english ? "he said: " : "他说过：") + m.Substring(3);
                sb.Append("- ").Append(m).Append('\n');
            }
            if (sb.Length == 0) sb.Append(english ? "- nothing in particular\n" : "- 没什么特别的\n");
            return sb.ToString();
        }

        static string Promises(GirlfriendState s, bool english)
        {
            var parts = new List<string>();
            foreach (var p in s.promises)
            {
                string what = english ? p.en : p.zh;
                if (p.state == 0) parts.Add(what);
                else if (p.state == 1) parts.Add(what + (english ? " (he kept it)" : "（他做到了）"));
                else if (p.state == 2) parts.Add(what + (english ? " (he broke it)" : "（他没做到）"));
            }
            return parts.Count == 0 ? (english ? "none" : "没有") : string.Join(english ? "; " : "；", parts);
        }

        /// <summary>The prompt's 「特殊情境」: holidays told from her side, a forgotten day, a quarrel, what she knows about the AI.</summary>
        public static string Special(GirlfriendState s, GfNow now, bool english)
        {
            var sb = new StringBuilder();
            var h = GirlfriendRules.HolidayOn(now.clock);
            if (h != null)
            {
                sb.Append(english ? h.situationEn : h.situationZh);
                if (h.wish.Length > 0)
                {
                    if (s.done.Contains("wish:" + h.key)) sb.Append(english ? " He has already wished you." : "他已经祝福过你了。");
                    else sb.Append(english ? " He has not mentioned it yet." : "他到现在还没提。");
                }
            }
            if (s.sulkUntil >= now.day && s.sulkZh.Length > 0) sb.Append(english ? " He forgot an important day recently; you are still sulking about it." : "他前几天忘了一个重要的日子，你还在别扭。");
            if (s.fighting) sb.Append(english ? " You are in a quarrel; you will not be won over by small talk." : "你们在冷战，几句闲聊哄不好你。");
            if (GirlfriendRules.Has(s, GirlfriendRules.FlagKnowsAi)) sb.Append(english ? " You know he has trained some AI program on his computer; you find it a bit strange." : "你知道他在电脑上训练了一个什么 AI 程序，觉得有点怪。");
            return sb.ToString().Trim();
        }

        /// <summary>Few-shot turns (design §4.3, rewritten per §10: spaces instead of commas). The one closest to her tier goes last.</summary>
        public static List<KeyValuePair<string, string>> FewShots(GirlfriendTier tier, bool english)
        {
            var shots = english
                ? new List<string[]>
                {
                    new[] { "what are you doing", "{\"msgs\":[\"just washed my hair\",\"you? on the computer again?\"],\"delta\":0,\"remember\":\"\"}", "normal" },
                    new[] { "going to the county town tomorrow to buy a graphics card", "{\"msgs\":[\"another card\",\"you love that computer more than me\"],\"delta\":0,\"remember\":\"he is going to town tomorrow to buy a graphics card\"}", "normal" },
                    new[] { "was so busy today didn't get to reply", "{\"msgs\":[\"oh\",\"busy with what\"],\"delta\":-1,\"remember\":\"\"}", "distant" },
                    new[] { "your probability exam is next wednesday right good luck", "{\"msgs\":[\"you remembered!!\",\"dummy I'll do my best [微笑]\"],\"delta\":2,\"remember\":\"\"}", "warm" },
                }
                : new List<string[]>
                {
                    new[] { "在干嘛", "{\"msgs\":[\"刚洗完头\",\"你呢 又在弄电脑？\"],\"delta\":0,\"remember\":\"\"}", "normal" },
                    new[] { "明天我去县城买显卡", "{\"msgs\":[\"又买显卡\",\"你那电脑比我还亲\"],\"delta\":0,\"remember\":\"他明天去县城买显卡\"}", "normal" },
                    new[] { "今天太忙了没顾上回你", "{\"msgs\":[\"哦\",\"忙什么\"],\"delta\":-1,\"remember\":\"\"}", "distant" },
                    new[] { "下周三你考概率论吧 加油", "{\"msgs\":[\"你居然记得！！\",\"笨蛋 我会加油的[微笑]\"],\"delta\":2,\"remember\":\"\"}", "warm" },
                };
            string want = tier <= GirlfriendTier.Distant ? "distant" : tier >= GirlfriendTier.Warm ? "warm" : "normal";
            int at = shots.FindIndex(x => x[2] == want);
            if (at >= 0) { var x = shots[at]; shots.RemoveAt(at); shots.Add(x); }
            var list = new List<KeyValuePair<string, string>>();
            foreach (var x in shots) { list.Add(new KeyValuePair<string, string>("user", x[0])); list.Add(new KeyValuePair<string, string>("assistant", x[1])); }
            return list;
        }

        /// <summary>The GBNF grammar (design §4.4 draft, §10): 1–3 messages, faces only from the whitelist, delta −2…2.</summary>
        public static string Grammar()
        {
            return "root ::= \"{\\\"msgs\\\":[\" msg (\",\" msg)? (\",\" msg)? \"],\\\"delta\\\":\" delta \",\\\"remember\\\":\\\"\" rtext \"\\\"}\"\n" +
                   "msg ::= \"\\\"\" text \"\\\"\"\n" +
                   "text ::= (plain | face){1,40}\n" +
                   "rtext ::= plain{0,30}\n" +
                   "plain ::= [^\"\\\\\\n\\[\\]]\n" +
                   "face ::= (" + YYFaces.GbnfAlternation(Faces()) + ")\n" +
                   "delta ::= \"-2\" | \"-1\" | \"0\" | \"1\" | \"2\"\n";
        }

        // ───────────── parsing ─────────────

        /// <summary>
        /// Reads the model's answer. The grammar makes it JSON, but an older server may ignore the grammar, so loose
        /// text is salvaged too (its lines become the bubbles). Bubbles are cleaned: unknown faces dropped, a
        /// trailing full stop removed, at most 40 characters. Null when nothing usable is left.
        /// </summary>
        public static GfModelReply Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            int think = raw.IndexOf("</think>", StringComparison.Ordinal);
            if (think >= 0) raw = raw.Substring(think + 8);
            var r = new GfModelReply();
            int open = raw.IndexOf('{'), close = raw.LastIndexOf('}');
            if (open >= 0 && close > open)
            {
                string json = raw.Substring(open, close - open + 1);
                int msgs = json.IndexOf("\"msgs\"", StringComparison.Ordinal);
                if (msgs >= 0)
                {
                    int i = json.IndexOf('[', msgs);
                    if (i >= 0) i++;
                    while (i > 0 && i < json.Length)
                    {
                        int q = NextQuote(json, i, ']');
                        if (q < 0) break;
                        string s = ReadString(json, q, out int end);
                        if (s == null) break;
                        r.msgs.Add(s);
                        i = end + 1;
                    }
                    r.json = true;
                }
                int d = json.IndexOf("\"delta\"", StringComparison.Ordinal);
                if (d >= 0)
                {
                    int k = json.IndexOf(':', d) + 1;
                    while (k > 0 && k < json.Length && json[k] == ' ') k++;
                    int sign = 1;
                    if (k > 0 && k < json.Length && json[k] == '-') { sign = -1; k++; }
                    if (k > 0 && k < json.Length && char.IsDigit(json[k])) r.delta = Math.Max(-2, Math.Min(2, sign * (json[k] - '0')));
                }
                int m = json.IndexOf("\"remember\"", StringComparison.Ordinal);
                if (m >= 0)
                {
                    int q = json.IndexOf('"', json.IndexOf(':', m) + 1);
                    if (q > 0) r.remember = (ReadString(json, q, out _) ?? "").Trim();
                }
            }
            if (!r.json)
            {
                foreach (var line in raw.Split('\n'))
                {
                    string t = line.Trim().Trim('"', '“', '”', '「', '」');
                    if (t.Length == 0 || t.StartsWith("{", StringComparison.Ordinal)) continue;
                    r.msgs.Add(t);
                    if (r.msgs.Count == 3) break;
                }
            }
            for (int i = r.msgs.Count - 1; i >= 0; i--)
            {
                r.msgs[i] = CleanBubble(r.msgs[i]);
                if (r.msgs[i].Length == 0) r.msgs.RemoveAt(i);
            }
            while (r.msgs.Count > 3) r.msgs.RemoveAt(r.msgs.Count - 1);
            return r.msgs.Count == 0 ? null : r;
        }

        static int NextQuote(string s, int from, char stop)
        {
            for (int i = from; i < s.Length; i++) { if (s[i] == '"') return i; if (s[i] == stop) return -1; }
            return -1;
        }

        /// <summary>A JSON string starting at the quote at <paramref name="q"/>; <paramref name="end"/> is its closing quote.</summary>
        static string ReadString(string s, int q, out int end)
        {
            var sb = new StringBuilder();
            for (int i = q + 1; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char n = s[++i];
                    if (n == 'n') sb.Append(' ');
                    else if (n == 'u' && i + 4 < s.Length && int.TryParse(s.Substring(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code)) { sb.Append((char)code); i += 4; }
                    else sb.Append(n);
                    continue;
                }
                if (c == '"') { end = i; return sb.ToString(); }
                sb.Append(c);
            }
            end = s.Length;
            return null;
        }

        /// <summary>Swear words the small model sometimes reaches for, and what she would say instead (she keeps it PG).</summary>
        static readonly (string, string)[] Rude = { ("傻逼", "笨蛋"), ("傻B", "笨蛋"), ("煞笔", "笨蛋"), ("卧槽", "天哪"), ("我操", "天哪"), ("他妈的", ""), ("妈的", ""), ("滚", "走开"), ("fuck", "gosh"), ("shit", "ugh") };

        /// <summary>One bubble as she would type it: no invented faces, no stage directions, no 2016-impossible slang, no trailing full stop.</summary>
        public static string CleanBubble(string text)
        {
            text = (text ?? "").Replace('\n', ' ').Trim();
            text = YYFaces.StripUnknown(text);
            text = Era.EraLexicon.Scrub(text) ?? "";
            // Brackets with actions or thoughts in them: （笑） (sighs).
            foreach (var pair in new[] { ('（', '）'), ('(', ')'), ('*', '*') })
            {
                int a = text.IndexOf(pair.Item1);
                int b = a >= 0 ? text.IndexOf(pair.Item2, a + 1) : -1;
                if (a >= 0 && b > a) text = (text.Substring(0, a) + text.Substring(b + 1)).Trim();
            }
            text = text.Trim().Trim('"', '“', '”', '「', '」').Trim();
            foreach (var w in Rude) { int at; while ((at = text.IndexOf(w.Item1, StringComparison.OrdinalIgnoreCase)) >= 0) text = text.Substring(0, at) + w.Item2 + text.Substring(at + w.Item1.Length); }
            while (text.EndsWith("。", StringComparison.Ordinal) || (text.EndsWith(".", StringComparison.Ordinal) && !text.EndsWith("..", StringComparison.Ordinal))) text = text.Substring(0, text.Length - 1).TrimEnd();
            text = text.Replace("。", " ").Replace("  ", " ").Trim().TrimEnd('，', ',', '、').TrimEnd();
            if (text.Length > 40) text = text.Substring(0, 40);
            return text;
        }
    }
}
