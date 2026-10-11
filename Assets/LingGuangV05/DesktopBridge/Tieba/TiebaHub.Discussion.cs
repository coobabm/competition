using System;
using System.Text;
using LingGuangV05.Core.Era;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Runtime;

namespace LingGuangV05.Desktop.Tieba
{
    public sealed partial class TiebaHub
    {
        public static bool IsDiscussion(string id) => id != null && (id.StartsWith("thread:", StringComparison.Ordinal) || id.StartsWith("era:", StringComparison.Ordinal));
        public bool DiscussionAvailable(string id)
        {
            if (!IsDiscussion(id) || S == null) return false;
            if (id.StartsWith("thread:", StringComparison.Ordinal)) {
                var t = Library.Get(id.Substring(7));
                return t != null && !t.dead && ForumLibrary.Visible(t, Context());
            }
            var e = EraContent.Events.Get(id.Substring(4));
            return e != null && e.channel == EraEvents.Tieba && !e.brief && e.date <= Context().today;
        }
        public string DiscussionTitle(string id)
        {
            if (id.StartsWith("thread:", StringComparison.Ordinal)) {
                var t = Library.Get(id.Substring(7)); return t == null ? "帖子讨论" : GameText.T(t.title,t.titleEn);
            }
            var e = EraContent.Events.Get(id.Substring(4)); return e == null ? "帖子讨论" : EraContent.Title(e);
        }
        public string DiscussionContext(string id)
        {
            if (!DiscussionAvailable(id)) return "";
            if (id.StartsWith("era:", StringComparison.Ordinal)) return DiscussionTitle(id) + "\n" + EraContent.Text(EraContent.Events.Get(id.Substring(4)));
            var t = Library.Get(id.Substring(7)); var b = new StringBuilder(DiscussionTitle(id));
            foreach (var floor in t.floors) {
                if (ForumLibrary.View(floor, Context()) != FloorView.Shown) continue;
                b.Append('\n').Append(GameText.T(floor.text,floor.textEn));
                if (b.Length > 700) break;
            }
            return b.ToString(0, Math.Min(900,b.Length));
        }
        string DiscussionFallback(string id, string question, int turn)
        {
            if (!IsDiscussion(id)) return question.Contains("电脑") || question.Contains("显卡")
                ? "我那台老机器也还在撑着。你想换什么配件？先看看预算。"
                : "刚忙完才看到。你最近在忙什么？";
            string topic = DiscussionContext(id);
            if (question.Contains("你好") || question.Contains("在吗")) return "在，楼还没沉。你也看了主楼？";
            if (question.Contains("谢谢")) return "客气啥，有后续再来楼里说一声。";
            if (question.Contains("程序") || question.Contains("灵光")) return "这个我没听说过，我也没用过。你说的是哪方面的东西？";
            if (question.Contains("贵") || question.Contains("预算") || question.Contains("多少钱")) return "先定个预算吧。新东西刚出溢价高，我一般等二手价稳了再买。";
            if (topic.Contains("显卡") || topic.Contains("1080") || topic.Contains("GPU")) return turn % 4 < 2
                ? "我比较在意功耗和实际能快多少。你是打游戏用，还是跑计算？"
                : "别光看跑分，电源和机箱空间也得算进去。你现在用什么卡？";
            if (topic.Contains("训练") || topic.Contains("数据")) return turn % 4 < 2
                ? "我看主楼说的也是先看数据。换一批没见过的题试试，比背答案有用。"
                : "我也还在看资料。你卡在主楼哪一句？贴出来咱们一起琢磨。";
            if (question.Contains("？") || question.Contains("?")) return "这个我也不敢打包票。你看到的是主楼的说法，还是有别的消息？";
            return turn % 4 < 2 ? "你这么一说还真有点道理，展开讲讲？" : "我倒想听个具体例子，光看标题很容易各说各的。";
        }
    }
}
