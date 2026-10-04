using System;
using System.Globalization;
using System.Text;
using LingGuangV05.Core;
using AppNames = LingGuangV05.Core.AppNames;

namespace LingGuangV05.Runtime
{
    public static class NativeLaoZhouReplies
    {
        public const int MaxInputLength = 160;
        public const int MaxReplyLength = 520;
        /// <summary>Plain text only: no tag interpretation, no commands, bounded UTF-16 without split pairs.</summary>
        public static string NormalizeInput(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var result = new StringBuilder(MaxInputLength);
            int scanLimit = Math.Min(value.Length, 4096);
            for (int i = 0; i < scanLimit && result.Length < MaxInputLength; i++)
            {
                char c = value[i];
                if (char.IsWhiteSpace(c))
                {
                    if (result.Length > 0 && result[result.Length - 1] != ' ') result.Append(' ');
                    continue;
                }
                if (char.IsControl(c)) continue;
                if (char.IsHighSurrogate(c))
                {
                    if (i + 1 < scanLimit && char.IsLowSurrogate(value[i + 1]) && result.Length + 2 <= MaxInputLength)
                    { result.Append(c); result.Append(value[++i]); }
                    continue;
                }
                if (!char.IsLowSurrogate(c)) result.Append(c);
            }
            return result.ToString().Trim();
        }

        public static string BuildReply(string input, ChapterOneSim sim)
        {
            string query = NormalizeInput(input).ToLowerInvariant();
            if (query.Length == 0) return null;
            if (sim == null) return "这是老周的本地预设帮助，不是实时 AI。学习机还在准备，请稍后再问。";
            GameState state = sim.S;
            string reply;
            if (!sim.AppInstalled)
                reply = DownloadHelp;
            else if (Has(query, "考试", "入学", "exam", "然后", "第二章"))
            {
                if (state.chapterOneComplete)
                    reply = "你已经通过第一章，奖励只发放一次。第二章尚未开放；还可以去「邮件」标注，在「灵光.exe」调整网络，去「家庭」管理收入和电费。";
                else if (state.examActive)
                    reply = "你在「灵光.exe」的入学考试已完成 " + state.examAnswered + " / " + sim.Config.examQuestions + " 题。请回灵光继续；这时「邮件」分拣暂停，不能代替考试答题。存档会保留进度。";
                else
                    reply = "考试在「灵光.exe」。准备进度：标注 " + state.cardsReviewed + " / " + sim.Config.examRequiredCards + "，训练步 " + Whole(state.steps) + " / " + Whole(sim.Config.examRequiredSteps) + "，模拟识别率 " + Percent(sim.Accuracy) + " / " + Percent(sim.Config.examRequiredAccuracy) + "。两个输出与供电也要正常。共 " + sim.Config.examQuestions + " 题，答对 " + sim.Config.examPassScore + " 题通过；失败可以再试。";
            }
            else if (Has(query, "显卡", "gpu", "机箱", "寻宝", "硬件", "shop", "hardware", "case"))
                reply = "硬件在「寻宝」买卖。GPU 单价 ¥" + Money(sim.Config.gpuPrice) + "，机箱 ¥" + Money(sim.Config.casePrice) + "；现有 " + state.gpuCount + " 张 GPU，槽位 " + state.gpuCount + " / " + state.caseCount * sim.Config.gpusPerCase + "，钱包 ¥" + Money(state.money) + "。别花光维护费，也要去「家庭」检查用电。都是本地模拟交易。";
            else if (Has(query, "赚钱", "接单", "工作", "收入", "order", "income", "job", "earn money"))
                reply = "先在「邮件」核对 3 封，再到「家庭」的收入页启用分类订单。你已核对 " + state.cardsReviewed + " 封，订单" + (state.jobEnabled ? "已启用" : "未启用") + "，当前每秒收入 ¥" + Money(sim.IncomePerSecond) + "。收入取决于网络、识别率、心跳与供电；关闭窗口不会暂停。";
            else if (Has(query, "电", "钱", "欠费", "账单", "跳闸", "断路", "power", "bill", "electricity", "breaker", "debt"))
            {
                reply = "到「家庭」查看电费与断路器。当前待付 ¥" + Money(state.billDue) + "，钱包 ¥" + Money(state.money) + "。";
                if (state.unpaidPower) reply += "欠费停机后去「邮件」正确标注，报酬会直接抵扣欠费，不会同时发现金。";
                else reply += "每天会自动结算电费，记得留出余额。";
                if (state.breakerTripped) reply += "现在已跳闸：先去「寻宝」出售多余 GPU 降低负载，再回「家庭」恢复断路器。";
                else reply += "高温会降低训练速度，超功率才会跳闸。";
            }
            else if (Has(query, "网络", "连接", "连线", "神经", "board", "connect", "network", "neural"))
                reply = "在「灵光.exe」的神经网络里，先点起点，再点相邻终点，建立有向连接；重复连接会删掉该边。输入和是、否端口不能删除。当前瓶颈：" + sim.Bottleneck + "。青色是运行脉冲，紫色是纠错反馈；训练数据来自「邮件」。";
            else if (Has(query, "升级", "技能", "训练", "学习点", "train", "skill", "upgrade", "learning point"))
                reply = "「灵光.exe」有语义理解和训练效率升级，需要学习点；正确处理「邮件」可积累样本与学习点。你当前有 " + Whole(state.learningPoints) + " 学习点，语义等级 " + state.semanticLevel + "，训练等级 " + state.trainingLevel + "。更多硬件不等于自动学会，还需要样本和有效网络。";
            else if (Has(query, "邮件", "标注", "样本", "开始", "你好", "您好", "hello", "help", "帮助", "mail", "label", "sample", "start"))
                reply = "先打开「邮件」阅读并给出真实分类，不要只附和模型。核对 3 封后，到「家庭」接第一份订单；网络、技能和入学考试在「灵光.exe」，买硬件去「寻宝」。这是老周的本地预设留言，不是实时 AI。";
            else
                reply = "我是老周的本地预设帮助，不是真人在线，也不是实时 AI。可以问我：怎么开始、邮件标注、网络连线、显卡、赚钱、电费、入学考试。我只解释当前第一章，不会替你操作或连接网络。";
            return reply.Length <= MaxReplyLength ? reply : reply.Substring(0, MaxReplyLength);
        }

        public const string DownloadHelp = AppNames.ExeZh + " 还没下载：打开桌面上的「浏览器」，在主页点 lingguang.cc，再点「下载」。进度条走完，它会出现在桌面上。";

        private static bool Has(string input, params string[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++) if (input.IndexOf(keywords[i], StringComparison.Ordinal) >= 0) return true;
            return false;
        }
        private static string Money(double value) { return value.ToString("0.00", CultureInfo.InvariantCulture); }
        private static string Whole(double value) { return value.ToString("0", CultureInfo.InvariantCulture); }
        private static string Percent(double value) { return (value * 100).ToString("0", CultureInfo.InvariantCulture) + "%"; }
    }
}
