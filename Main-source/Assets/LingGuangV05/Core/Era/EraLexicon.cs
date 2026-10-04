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
            "皮皮虾我们走", "扎心了老铁", "请开始你的表演", "惊不惊喜", "意不意外", "尬聊", "油腻中年", "打 call", "freestyle",
            "戏精", "佛系", "吃鸡", "确认过眼神", "skr", "C 位", "官宣", "真香", "锦鲤附体", "我酸了", "安排上",
        };

        static readonly Regex[] Patterns =
        {
            P("皮皮虾\\s*[，,]?\\s*我们走"), P("扎心了\\s*[，,]?\\s*老铁"), P("请开始你的表演"), P("惊不惊喜"), P("意不意外"), P("尬聊"),
            P("油腻(的)?(中年|大叔|男)"), P("打\\s*call"), P("free\\s*style"), P("戏精"), P("佛系"), P("吃鸡(?![腿肉蛋翅爪汤块])"),
            P("确认过眼神"), P("(?<![a-z])skr(?![a-z])"), P("(?<![a-z])c\\s*位"), P("官宣"), P("(?<![饭菜肉汤花茶面])真香"),
            P("锦鲤(附体|本鲤)|转发锦鲤|成为锦鲤"), P("我酸了"), P("安排上|给(你|我|他|她|您)安排(上|了)(?!一)"),
        };

        static Regex P(string pattern) => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>The first post-2016 meme in the text, or null.</summary>
        public static string FirstBanned(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            foreach (var p in Patterns) { var m = p.Match(text); if (m.Success) return m.Value; }
            return null;
        }

        /// <summary>Removes post-2016 memes from model output.</summary>
        public static string Scrub(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            foreach (var p in Patterns) text = p.Replace(text, "");
            return text;
        }

        /// <summary>Line for the model's system prompt.</summary>
        public static string PromptRule(bool english)
        {
            return english
                ? "It is 2016. Never use internet slang from after 2016, such as: " + string.Join(", ", Banned) + "."
                : "现在是 2016 年。不要用 2016 年以后才流行的网络用语，例如：" + string.Join("、", Banned) + "。";
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
