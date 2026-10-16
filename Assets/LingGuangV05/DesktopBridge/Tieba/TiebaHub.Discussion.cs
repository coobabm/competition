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
        /// <summary>
        /// How the forum regards the player at this stage, for the model's prompt: the respect grows with what he has really
        /// done (he never gets flattered for nothing), and it stays in each person's own voice.
        /// </summary>
        static string Regard(string id, int stage)
        {
            string tail = "夸要具体、点出他做成了什么，不要每句都夸，也别肉麻。";
            if (id == ForumLibrary.LaoZhou)
            {
                if (stage <= 2) return "\n你把他当个爱折腾的显卡发烧友，嘴上嫌他瞎折腾，心里觉得他肯动手。";
                if (stage == 3) return "\n你注意到他问的问题越来越像样，对他另眼相看了一点，偶尔夸一句，很克制。" + tail;
                if (stage <= 5) return "\n你心里很佩服他：一个刚毕业的人，一个人把东西一步步做出来，比你们组有些人踏实。你会不经意地问他的看法，遇到拿不准的事想听听他怎么说。" + tail;
                return "\n你已经把他当同行：遇到拿不准的事，第一反应是想问问他。语气里有敬意，但还是老周的口气。" + tail;
            }
            if (id == ForumLibrary.ZhouNow)
                return stage >= 5 ? "\n你替人做过第三方检验，认真看过他交的译文，很服气，愿意叫他一声师兄，会向他请教怎么练的。" + tail : "";
            if (stage <= 2) return "";
            if (stage <= 4) return "\n吧里不少人认得他的 ID「显卡在燃烧」，知道他回帖有数有据，会客气地叫一声楼主、大佬。" + tail;
            return "\n吧里的人都认得他的 ID「显卡在燃烧」，觉得他是吧里真做事的人，叫他大神；有问题会想听他的意见。" + tail;
        }

        string DiscussionFallback(string id, string question, int turn)
        {
            if (!IsDiscussion(id) && id == ForumLibrary.LaoZhou)
            {
                bool gear = question.Contains("电脑") || question.Contains("显卡");
                int stage = Context().stage;
                if (stage <= 2) return gear
                    ? GameText.T("我们实验室那几张卡天天满载，你那张 980 Ti 还好吧？别烧了。", "Our lab cards run flat out all day. Is your 980 Ti holding up? Don't cook it.")
                    : GameText.T("刚从实验室出来才看到。你最近在忙什么？", "Just got out of the lab and saw this. What have you been busy with?");
                if (stage == 3) return gear
                    ? GameText.T("你那张 980 Ti 还好吧？别烧了。……你最近问的问题越来越像样了哈。", "Is your 980 Ti holding up? Don't cook it. … Your questions have been getting a lot better lately, huh.")
                    : GameText.T("刚从实验室出来才看到。你那边有进展说一声，我有点好奇哈。", "Just out of the lab. Tell me when you make progress, I'm a bit curious, huh.");
                if (stage <= 5) return gear
                    ? GameText.T("你那张 980 Ti 能撑到现在，我是服的。我们组要是有人像你这么肯一点一点试，导师能少骂一半哈。", "That 980 Ti has lasted this long, and I'm impressed. If anyone in our group tried things as patiently as you, the advisor would yell half as much, huh.")
                    : GameText.T("刚开完组会才看到。我们组遇到卡点的时候，我有时会想：换你，会怎么处理？", "Just out of group meeting. When our group hits a wall, I sometimes wonder: how would you handle it?");
                return gear
                    ? GameText.T("你那台机器现在算是实战过的了，比我们实验室的卡有故事。……说真的，模型再大一个量级，你觉得它还会只是背题吗？", "Your machine has seen real action now. It has more stories than our lab cards. … Honestly, with a model an order of magnitude bigger, would it still just be memorising?")
                    : GameText.T("才看到。有件事想听你的看法：我们组在吵先加数据还是先加结构，你站哪边？", "Just saw this. I'd like your take: our group is arguing data first or structure first. Which side are you on?");
            }
            if (!IsDiscussion(id) && id == ForumLibrary.ZhouNow)
                return Context().stage >= 5 ? GameText.T("刚开完组会才看到。……我还是想叫你一声师兄，你那天的译法，我拿去组会上讲过了。", "Just out of group meeting. … I still want to call you senior. I presented the way you translated at our group meeting.")
                    : GameText.T("刚开完组会，才看到。你是哪位？", "Just out of group meeting. Who are you?");
            if (!IsDiscussion(id)) return question.Contains("电脑") || question.Contains("显卡")
                ? "我那台老机器也还在撑着。你想换什么配件？先看看预算。"
                : "刚忙完才看到。你最近在忙什么？";
            string topic = DiscussionContext(id);
            if (question.Contains("你好") || question.Contains("在吗")) return "在，楼还没沉。你也看了主楼？";
            int regard = Context().stage;
            if (question.Contains("谢谢")) return regard >= 3
                ? GameText.T("客气啥。你在吧里回帖有数有据，我们才愿意多说两句。有后续再来楼里说一声。", "No problem. You back your replies up with numbers, so we're glad to say more. Come back and tell us how it goes.")
                : "客气啥，有后续再来楼里说一声。";
            if (question.Contains("程序") || question.Contains("灵光")) return "这个我没听说过，我也没用过。你说的是哪方面的东西？";
            if (question.Contains("贵") || question.Contains("预算") || question.Contains("多少钱")) return "先定个预算吧。新东西刚出溢价高，我一般等二手价稳了再买。";
            if (topic.Contains("显卡") || topic.Contains("1080") || topic.Contains("GPU")) return turn % 4 < 2
                ? "我比较在意功耗和实际能快多少。你是打游戏用，还是跑计算？"
                : "别光看跑分，电源和机箱空间也得算进去。你现在用什么卡？";
            if (topic.Contains("训练") || topic.Contains("数据")) return turn % 4 < 2
                ? "我看主楼说的也是先看数据。换一批没见过的题试试，比背答案有用。"
                : "我也还在看资料。你卡在主楼哪一句？贴出来咱们一起琢磨。";
            if (question.Contains("？") || question.Contains("?")) return "这个我也不敢打包票。你看到的是主楼的说法，还是有别的消息？";
            if (regard >= 5 && turn % 4 < 2) return GameText.T("你这么一说还真有道理。你在吧里回的帖我基本都看，大神，展开讲讲？", "That actually makes sense. I read nearly everything you post here. Come on, big shot, tell us more?");
            if (regard >= 3 && turn % 4 >= 2) return GameText.T("我倒想听个具体例子。你上回讲的那个就很清楚，这次也来一个？", "I'd like a concrete example. The one you gave last time was very clear. Another?");
            return turn % 4 < 2 ? "你这么一说还真有点道理，展开讲讲？" : "我倒想听个具体例子，光看标题很容易各说各的。";
        }
    }
}
