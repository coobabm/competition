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
        bool ZhouReviewKnown => runtime?.Sim != null && ReferenceEquals(runtime.Sim, linked)
            && ZhouReviewEvidence.HasReview(runtime.Sim.S, S);
        /// <summary>Only verified received material establishes review knowledge; stage never grants familiarity.</summary>
        string Regard(string id, int stage)
        {
            if (id == ForumLibrary.LaoZhou) return "\n只根据对方真实发来的话回应，不按游戏阶段推断他做过什么，也不自动提升评价。";
            if (id == ForumLibrary.ZhouNow)
                return ZhouReviewKnown
                    ? "\n你已在这段私信里确认看过对方提交的独立翻译题和结果。只谈已交流的题目与结果；不知道他的私人训练数据、做题过程或未发送的内容。不要编造在组会上展示过译文。"
                    : "\n只根据这段私信里对方真实发来的话回应。可以聊公开的翻译材料和 Caffe 用法；不要声称看过尚未收到的译文、替他验收过，或凭阶段称他为师兄。";
            return "\n只根据本帖当前可见内容和对方真实发来的话回应；不要凭阶段编造他的过往回帖、名气、成绩或你读过他的其他帖子。";
        }

        string DiscussionFallback(string id, string question, int turn)
        {
            if (!IsDiscussion(id) && id == ForumLibrary.LaoZhou) return LaoZhouQuestions.GeneralReply(GameText.IsEnglish);
            if (!IsDiscussion(id) && id == ForumLibrary.ZhouNow)
                return ZhouReviewKnown
                    ? GameText.T("上次那组新题的结果还留着吧？错题也一起看，别只看答对的。", "You still have the results from that fresh set of questions, right? Let's look at the mistakes too, not just the correct answers.")
                    : GameText.T("刚看到。你想聊翻译材料，还是 Caffe 的使用？有具体例子可以贴出来。", "Just saw your message. Would you like to discuss translation material or using Caffe? You can post a specific example.");
            if (!IsDiscussion(id)) return question.Contains("电脑") || question.Contains("显卡")
                ? "我那台老机器也还在撑着。你想换什么配件？先看看预算。"
                : "刚忙完才看到。你最近在忙什么？";
            string topic = DiscussionContext(id);
            if (question.Contains("你好") || question.Contains("在吗")) return "在，楼还没沉。你也看了主楼？";
            if (question.Contains("谢谢") || question.IndexOf("thank", StringComparison.OrdinalIgnoreCase) >= 0)
                return GameText.T("客气啥，有后续再来楼里说一声。", "No problem. Come back and tell us how it goes.");
            if (question.Contains("程序") || question.Contains("灵光")) return "这个我没听说过，我也没用过。你说的是哪方面的东西？";
            if (question.Contains("贵") || question.Contains("预算") || question.Contains("多少钱")) return "先定个预算吧。新东西刚出溢价高，我一般等二手价稳了再买。";
            if (topic.Contains("显卡") || topic.Contains("1080") || topic.Contains("GPU")) return turn % 4 < 2
                ? "我比较在意功耗和实际能快多少。你是打游戏用，还是跑计算？"
                : "别光看跑分，电源和机箱空间也得算进去。你现在用什么卡？";
            if (topic.Contains("训练") || topic.Contains("数据")) return turn % 4 < 2
                ? "我看主楼说的也是先看数据。换一批没见过的题试试，比背答案有用。"
                : "我也还在看资料。你卡在主楼哪一句？贴出来咱们一起琢磨。";
            if (question.Contains("？") || question.Contains("?")) return "这个我也不敢打包票。你看到的是主楼的说法，还是有别的消息？";
            return turn % 4 < 2
                ? GameText.T("你这么一说还真有点道理，展开讲讲？", "That sounds reasonable. Could you say more?")
                : GameText.T("我倒想听个具体例子，光看标题很容易各说各的。", "I'd like a concrete example. A title alone can mean different things to different people.");
        }
    }
}
