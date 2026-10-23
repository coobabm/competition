using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LingGuangV05.Core.Era
{
    /// <summary>
    /// Memes that only caught on after 2016 (design v1.1 §14.2 禁用词). Nothing in the game may say them: authored
    /// content is checked by tests, model output is scrubbed and the model is told not to use them. Words that
    /// also have a plain 2016 meaning only match in their meme form (油腻 food is fine, 油腻中年 is not).
    /// </summary>
    public static class EraLexicon
    {
        /// <summary>Shown to the model and in docs.</summary>
        public static readonly string[] Banned =
        {
            "皮皮虾我们走", "扎心了老铁", "请开始你的表演", "惊不惊喜", "意不意外", "尬聊", "油腻中年", "打 call", "你有 freestyle 吗",
            "戏精", "佛系", "吃鸡", "确认过眼神", "skr", "C 位", "官宣", "真香", "锦鲤附体", "我酸了", "安排上",
        };

        static readonly Regex[] Patterns =
        {
            P("皮皮虾\\s*[，,]?\\s*我们走"), P("扎心了\\s*[，,]?\\s*老铁"), P("请开始你的表演"), P("惊不惊喜"), P("意不意外"), P("尬聊"),
            // Freestyle is an older swimming/music term; only the later Chinese catchphrase is a future meme.
            P("油腻(的)?(中年|大叔|男)"), P("打\\s*call"), P("(?:你\\s*)?有\\s*free\\s*style\\s*[吗么嘛]"), P("戏精"), P("佛系"), P("吃鸡(?![腿肉蛋翅爪汤块])"),
            P("确认过眼神"), P("(?<![a-z])skr(?![a-z])"), P("(?<![a-z])c\\s*位"), P("官宣"), P("(?<![饭菜肉汤花茶面])真香"),
            P("锦鲤(附体|本鲤)|转发锦鲤|成为锦鲤"), P("我酸了"), P("安排上|给(你|我|他|她|您)安排(上|了)(?!一)"),
        };

        /// <summary>
        /// AI words that only caught on after 2016 (research 04 §1.3 A6). They apply to in-world text and model output only:
        /// the player layer (PlayerKnowledgeCopy, the out-of-world "no real model is called" notes) may use them, so
        /// <see cref="FirstBanned"/> and <see cref="Audit"/> stay meme-only. Shown to the model and in docs.
        /// </summary>
        public static readonly string[] BannedAiTerms =
        {
            "大模型", "大语言模型", "LLM", "ChatGPT", "提示词", "AI 幻觉", "对齐微调", "RLHF", "RAG", "思维链", "AI 智能体", "AIGC", "扩散模型", "词元",
        };

        sealed class AiRule
        {
            public readonly Regex pattern; public readonly string zh, en;
            public AiRule(Regex pattern, string zh, string en) { this.pattern = pattern; this.zh = zh; this.en = en; }
        }

        // Each later term is replaced by the plain 2016 wording rather than cut out, so a model's sentence still reads.
        // Plain 2016 words stay: 放大模型, 幻觉 on its own, 智能体 in reinforcement learning, a cloth rag, and the game's
        // own 偏好对齐 (XgGlossary).
        static readonly AiRule[] AiRules =
        {
            new AiRule(P("(?<![放扩加增变庞])大(?:语言)?模型"), "模型", "model"),
            new AiRule(P("large (language models?)"), "$1", "$1"),
            new AiRule(C("(?<![A-Za-z])([Aa])n LLM(?![A-Za-z])"), "$1 language model", "$1 language model"),
            new AiRule(C("(?<![A-Za-z])LLMs?(?![A-Za-z])"), "语言模型", "language model"),
            new AiRule(P("ChatGPT"), "聊天程序", "chatbot"),
            new AiRule(P("提示(?:词|工程)"), "提示", "prompt"),
            new AiRule(P("(AI|人工智能|模型)\\s*的?\\s*幻觉"), "$1瞎编", "$1 making things up"),
            new AiRule(P("对齐微调|RLHF"), "微调", "fine-tuning"),
            new AiRule(P("检索增强(?:生成)?"), "检索", "retrieval"),
            new AiRule(C("(?<![A-Za-z])RAG(?![A-Za-z])"), "检索", "retrieval"),
            new AiRule(P("思维链|chain[- ]of[- ]thought"), "分步思考", "step-by-step reasoning"),
            new AiRule(P("AI\\s*智能体"), "AI 程序", "AI program"),
            new AiRule(P("AIGC"), "机器生成的内容", "machine-generated content"),
            new AiRule(P("扩散模型"), "生成模型", "generative model"),
            new AiRule(P("词元"), "字词", "token"),
        };

        static readonly Regex Cjk = new Regex("[\u4e00-\u9fff]", RegexOptions.CultureInvariant);

        static Regex P(string pattern) => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static Regex C(string pattern) => new Regex(pattern, RegexOptions.CultureInvariant);

        /// <summary>The first post-2016 meme in the text, or null.</summary>
        public static string FirstBanned(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            foreach (var p in Patterns) { var m = p.Match(text); if (m.Success) return m.Value; }
            return null;
        }

        /// <summary>The first post-2016 AI term in in-world text, or null (the player layer is exempt).</summary>
        public static string FirstBannedAiTerm(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            foreach (var r in AiRules) { var m = r.pattern.Match(text); if (m.Success) return m.Value; }
            return null;
        }

        /// <summary>Removes post-2016 memes from model output and rewords post-2016 AI terms in 2016 language.</summary>
        public static string Scrub(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            foreach (var p in Patterns) text = p.Replace(text, "");
            bool chinese = Cjk.IsMatch(text);
            foreach (var r in AiRules) text = r.pattern.Replace(text, chinese ? r.zh : r.en);
            return text;
        }

        /// <summary>Line for the model's system prompt.</summary>
        public static string PromptRule(bool english)
        {
            return english
                ? "It is 2016. Never use internet slang from after 2016, such as: " + string.Join(", ", Banned) + ". Nor AI terms that caught on after 2016, such as: " + string.Join(", ", BannedAiTerms) + "."
                : "现在是 2016 年。不要用 2016 年以后才流行的网络用语，例如：" + string.Join("、", Banned) + "。也不要用 2016 年以后才流行的 AI 说法，例如：" + string.Join("、", BannedAiTerms) + "。";
        }

        /// <summary>Every post-2016 AI term found in a set of in-world authored lines (for content tests).</summary>
        public static List<string> AuditAiTerms(IEnumerable<string> lines)
        {
            var found = new List<string>();
            foreach (var line in lines) { var hit = FirstBannedAiTerm(line); if (hit != null) found.Add(hit + " ← " + line); }
            return found;
        }

        /// <summary>Every banned meme found in a set of authored lines (for content tests).</summary>
        public static List<string> Audit(IEnumerable<string> lines)
        {
            var found = new List<string>();
            foreach (var line in lines) { var hit = FirstBanned(line); if (hit != null) found.Add(hit + " ← " + line); }
            return found;
        }
    }
}
