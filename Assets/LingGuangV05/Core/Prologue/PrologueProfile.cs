using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LingGuangV05.Core
{
    /// <summary>
    /// The opening setup (design v1.1 §6 Step 8, §11.4): its name, how it calls itself, how it calls you and one
    /// sentence of personality. The sentence is read once into three axis targets (温度 冷静↔热情, 玩心 正经↔皮,
    /// 主见 顺从↔有主见, each 0–100) and up to two free tone words. The local model does the reading when it is
    /// running; <see cref="Read"/> is the offline fallback and gives the same shape.
    /// </summary>
    [Serializable]
    public sealed class PrologueProfile
    {
        public const int MaxWords = 2;
        public static readonly string[] SelfChoices = { "我", "本机", "人家" };
        public static readonly string[] SelfChoicesEn = { "I", "this machine", "little me" };
        public static readonly string[] CallChoices = { "你", "主人", "老大", "哥", "姐" };
        public static readonly string[] CallChoicesEn = { "you", "master", "boss", "bro", "sis" };

        public string name = "", self = "", callMe = "", personality = "";
        public double warmth = 50, play = 50, opinion = 50;
        public List<string> words = new List<string>();

        static int Length(string text) => new StringInfo(text ?? "").LengthInTextElements;

        static bool Clean(string text)
        {
            foreach (char c in text) if (char.IsControl(c) || c == '<' || c == '>' || c == '[' || c == ']') return false;
            return true;
        }

        /// <summary>What is wrong with the profile (zh / en), or null if it can be saved.</summary>
        public static string Problem(PrologueProfile p, bool english = false)
        {
            string T(string zh, string en) => english ? en : zh;
            if (p == null) return T("没有填写。", "Nothing filled in.");
            string name = (p.name ?? "").Trim(), self = (p.self ?? "").Trim(), call = (p.callMe ?? "").Trim(), mood = (p.personality ?? "").Trim();
            if (Length(name) < 1 || Length(name) > 16) return T("名字请输入 1–16 个字。", "Name: 1–16 characters.");
            if (Length(self) < 1 || Length(self) > 8) return T("自称请选一个，或输入 1–8 个字。", "Self-reference: pick one or type 1–8 characters.");
            if (Length(call) < 1 || Length(call) > 8) return T("称呼请选一个，或输入 1–8 个字。", "What it calls you: pick one or type 1–8 characters.");
            if (Length(mood) < 1 || Length(mood) > 60) return T("性格请用一句话描述（1–60 个字）。", "Personality: one sentence, 1–60 characters.");
            if (!Clean(name) || !Clean(self) || !Clean(call) || !Clean(mood)) return T("不能包含控制符或格式标签。", "No control characters or format tags.");
            foreach (var axis in new[] { p.warmth, p.play, p.opinion }) if (double.IsNaN(axis) || axis < 0 || axis > 100) return T("性格数值超出范围。", "Personality values out of range.");
            if (p.words == null || p.words.Count > MaxWords) return T("语气词最多两个。", "At most two tone words.");
            foreach (var w in p.words) if (Length(w) < 1 || Length(w) > 8 || !Clean(w)) return T("语气词太长。", "Tone word too long.");
            return null;
        }

        // ───────────── reading the sentence (offline) ─────────────

        static readonly string[] WarmUp = { "热情", "温柔", "暖", "贴心", "体贴", "心软", "开朗", "活泼", "热心", "黏人", "粘人", "可爱", "关心", "warm", "kind", "sweet", "cheerful" };
        static readonly string[] WarmDown = { "冷静", "高冷", "冷淡", "冷漠", "理性", "淡定", "话少", "内向", "沉稳", "cold", "calm", "cool", "rational" };
        static readonly string[] PlayUp = { "皮", "逗", "吐槽", "幽默", "搞笑", "贱", "调皮", "中二", "爱玩", "戏多", "闹", "funny", "playful", "sarcastic", "cheeky" };
        static readonly string[] PlayDown = { "正经", "严肃", "认真", "稳重", "靠谱", "专业", "老实", "serious", "formal", "earnest" };
        static readonly string[] OpinionUp = { "嘴硬", "有主见", "倔", "毒舌", "独立", "坚持", "傲娇", "犟", "强势", "自信", "敢说", "反驳", "stubborn", "opinionated", "blunt", "proud" };
        static readonly string[] OpinionDown = { "听话", "乖", "顺从", "温顺", "随和", "配合", "好说话", "服从", "obedient", "agreeable", "gentle" };
        static readonly string[] KnownTones = { "吐槽", "撒娇", "卖萌", "傲娇", "毒舌", "碎碎念", "嘴硬", "犯二", "话痨" };

        static int Hits(string text, string[] words)
        {
            int n = 0;
            foreach (var w in words) if (text.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0) n++;
            return n;
        }

        /// <summary>50 ± up to 40, from how many words lean each way (a softened form of §11.4's 50 + 50 × (高 − 低)/(高 + 低)).</summary>
        static double Axis(int high, int low) => high + low == 0 ? 50 : Math.Round(50 + 40.0 * (high - low) / (high + low + 1));

        /// <summary>Reads the personality sentence into axis targets and up to two tone words without the model.</summary>
        public static PrologueProfile Read(string sentence)
        {
            sentence = sentence ?? "";
            var p = new PrologueProfile
            {
                personality = sentence,
                warmth = Axis(Hits(sentence, WarmUp), Hits(sentence, WarmDown)),
                play = Axis(Hits(sentence, PlayUp), Hits(sentence, PlayDown)),
                opinion = Axis(Hits(sentence, OpinionUp), Hits(sentence, OpinionDown)),
            };
            foreach (Match m in Regex.Matches(sentence, "(?<![可喜恋热亲心])爱[\\u4e00-\\u9fa5]{2,3}"))
                AddWord(p.words, m.Value.Length > 3 && Array.Exists(KnownTones, k => m.Value.Substring(1, 2) == k) ? m.Value.Substring(0, 3) : m.Value);
            foreach (var tone in KnownTones) if (sentence.Contains(tone)) AddWord(p.words, tone);
            return p;
        }

        static void AddWord(List<string> words, string word)
        {
            if (words.Count >= MaxWords || string.IsNullOrEmpty(word) || Era.EraLexicon.FirstBanned(word) != null) return;
            foreach (var w in words) if (w.Contains(word) || word.Contains(w)) return;
            words.Add(word);
        }

        // ───────────── reading the sentence (local model, called once) ─────────────

        public static string ModelPrompt(bool english)
        {
            return english
                ? "Turn one sentence describing a personality into three numbers from 0 to 100 and at most two short tone words taken from the sentence. warmth: calm 0 – passionate 100. play: serious 0 – cheeky 100. opinion: compliant 0 – opinionated 100. Output only JSON: {\"warmth\":n,\"play\":n,\"opinion\":n,\"words\":[\"…\"]}"
                : "把一句性格描述折算成三个 0–100 的数，再从原句里挑出最多两个简短的语气词。温度：冷静 0 – 热情 100。玩心：正经 0 – 皮 100。主见：顺从 0 – 有主见 100。只输出 JSON：{\"温度\":数字,\"玩心\":数字,\"主见\":数字,\"语气词\":[\"…\"]}";
        }

        static double? Number(string text, params string[] keys)
        {
            foreach (var key in keys)
            {
                var m = Regex.Match(text, "\"?" + Regex.Escape(key) + "\"?\\s*[:：]\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.IgnoreCase);
                if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) return Math.Max(0, Math.Min(100, v));
            }
            return null;
        }

        /// <summary>The model's answer, or null if it is not usable (then <see cref="Read"/> is used).</summary>
        public static PrologueProfile ParseModel(string reply, string sentence)
        {
            if (string.IsNullOrEmpty(reply)) return null;
            var warmth = Number(reply, "温度", "warmth"); var play = Number(reply, "玩心", "play"); var opinion = Number(reply, "主见", "opinion");
            if (warmth == null || play == null || opinion == null) return null;
            // A small local model can misread an obvious word ("嘴硬" as compliant). Where the sentence itself clearly
            // leans one way, the two readings are averaged; elsewhere the model decides.
            var words = Read(sentence);
            double Blend(double model, double keyword) => keyword == 50 ? Math.Round(model) : Math.Round((model + keyword) / 2);
            var p = new PrologueProfile { personality = sentence ?? "", warmth = Blend(warmth.Value, words.warmth), play = Blend(play.Value, words.play), opinion = Blend(opinion.Value, words.opinion) };
            var list = Regex.Match(reply, "(?:语气词|words)\"?\\s*[:：]\\s*\\[([^\\]]*)\\]", RegexOptions.IgnoreCase);
            if (list.Success)
                foreach (Match w in Regex.Matches(list.Groups[1].Value, "\"([^\"]{1,8})\""))
                {
                    string word = w.Groups[1].Value.Trim();
                    // Tone words come from the player's own sentence, not from the model's imagination.
                    if (Length(word) <= 8 && (sentence ?? "").IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) AddWord(p.words, word);
                }
            if (p.words.Count == 0) foreach (var word in words.words) AddWord(p.words, word);
            return p;
        }
    }
}
