namespace LingGuangV05.Core
{
    internal static class EmailTemplates
    {
        internal sealed class Template
        {
            internal string text, explanation; internal bool yes;
            internal Template(string text, bool yes, string explanation) { this.text = text; this.yes = yes; this.explanation = explanation; }
        }
        private static readonly Template[] Items =
        {
            new Template("陌生发件人：恭喜抽中十万元！先汇 300 元手续费。", true, "陌生中奖通知要求先付手续费，是典型诈骗垃圾邮件。"),
            new Template("网吧老板：今晚十点维护，请保存客户的预约表。", false, "已知工作联系人的具体维护通知，不是推销或诈骗。"),
            new Template("陌生卖家：群发特价发票，可代开，回复即可办理。", true, "未经订阅的群发代开发票广告，属于垃圾邮件。"),
            new Template("你订阅的技术周刊：本周专题是 AlphaGo 和深度学习入门。", false, "明确订阅的技术内容不因群发就成为垃圾邮件。"),
            new Template("自称支付宝客服：账户异常将被冻结，请回信提供支付密码和短信验证码。", true, "索要支付密码和验证码是假冒客服的危险信号。"),
            new Template("同学小陈：明天的实验课改到三楼，附老师通知。", false, "熟悉同学发送具体课程变更通知，是正常来信。"),
            new Template("陌生发件人：做微商代理每天躺赚一万，零风险，立即转账入会。", true, "不合理收益承诺并要求转账，属于诈骗式垃圾信息。"),
            new Template("你刚购买的电脑配件：订单已经发货，可在原商店查询。", false, "与你刚完成的购物行为对应的发货通知是正常邮件。"),
            new Template("未知联系人：低价出售上万条用户邮箱和电话号码。", true, "未经请求兜售个人信息的广告是垃圾邮件。"),
            new Template("朋友阿林：周六来我家装电脑吗？地址还是上次那里。", false, "熟人的具体邀约没有群发推销或索取凭据特征。"),
            new Template("中奖中心：从未参加也能中奖，今天不交税就取消资格。", true, "未参加活动却中奖，并制造付款紧迫感，是诈骗。"),
            new Template("公司人事：下周二例会提前半小时，请各组确认。", false, "与你所在组织相关的正常工作安排不是垃圾邮件。"),
            new Template("陌生推广：永久会员免费送，先给所有联系人转发这封信。", true, "强制扩散的陌生推广和免费诱饵属于垃圾信息。"),
            new Template("你订阅的商店优惠通知：鼠标打九折，底部有退订入口。", false, "你主动订阅、可以退订的营销邮件，不按本题规则判为垃圾。"),
            new Template("自称管理员：请打开陌生附件，关闭杀毒软件才能领取补丁。", true, "陌生附件要求关闭安全防护，是恶意诱导。"),
            new Template("学校图书馆：你借的两本书将在周五到期。", false, "与实际借阅记录相符的到期提醒是正常事务邮件。"),
            new Template("陌生中介：贷款无需审核，先支付保证金才放款。", true, "无需审核的贷款诱饵加预付保证金，是常见诈骗。"),
            new Template("项目搭档：上次讨论的两处错误已经修正，请查收代码。", false, "已知合作伙伴就现有项目发送更新，是正常交流。"),
            new Template("群发招聘：不用面试日赚三千，先买培训材料入职。", true, "不切实际的报酬承诺并要求先付款，属于诈骗式招聘。"),
            new Template("你刚在论坛发帖：有人回复了你的显卡散热问题。", false, "与你刚才操作直接对应的论坛提醒是正常邮件。"),
            new Template("陌生广告：快速瘦十公斤，不运动，回复银行卡即可购买。", true, "夸张疗效与索取银行卡信息的陌生推广应判垃圾。"),
            new Template("你申请的密码重置邮件：若不是本人操作可以忽略。", false, "题目明确是你主动申请的重置通知，不应仅凭密码字样误杀。"),
            new Template("未知富商：愿赠你遗产，只需提交身份证和账户密码。", true, "陌生遗产赠送并索取敏感信息，是诈骗垃圾邮件。"),
            new Template("家人：回家路上帮忙带一袋米，钱回家给你。", false, "正常家庭事务请求，不是垃圾广告或钓鱼。"),
            new Template("陌生营销：最后一分钟抢购，已连续发送十封同样的广告。", true, "未经订阅、重复轰炸的广告符合垃圾邮件特征。"),
            new Template("你已报名的比赛：作品提交截止日期延后两天。", false, "与已报名活动有关的重要通知是正常邮件。"),
            new Template("自称网管：把游戏账号密码发过来，免费升级装备。", true, "冒充管理者索取账号密码并承诺免费利益，是钓鱼。"),
            new Template("已约好的维修店：你的主板已检测完，可凭单据领取。", false, "与你送修设备对应的服务通知属于正常邮件。"),
            new Template("陌生联系人：请替海外账户收款，再转到另一账户，佣金丰厚。", true, "陌生人要求代收转账并提供高佣金，是高风险诈骗邀请。"),
            new Template("社团负责人：这是你昨天索要的活动照片，不用转发。", false, "已知联系人发送你明确索要的资料，是正常邮件。"),
            new Template("未知推销：无需学习包过所有考试，付款后才告知详情。", true, "陌生群发包过承诺与不透明预付款要求属于垃圾推广。"),
            new Template("你关注的开源项目：发布安全修复，建议从原项目页下载。", false, "已关注项目发出可从原页面核验的更新通知是正常邮件。")
        };
        internal static int Count { get { return Items.Length; } }
        internal static Template Get(int index) { return Items[index]; }
    }
}
