using System;

namespace LingGuangV05.XingGuang
{
    public partial class XgCard
    {
        /// <summary>专家题悬赏: an expert card on the logic desk that pays <see cref="XgSim.BountyMultiplier"/> times.</summary>
        public bool bounty;
        /// <summary>A law question outsourced by a law firm: paid work, but not data for any task here (no sample).</summary>
        public bool law;
    }

    /// <summary>
    /// 专家题悬赏: expert labels cost more in the real labelling market (law, medicine, hard reasoning), so now and then
    /// the logic desk turns up a bounty: a much harder logic question, or a yes/no law question a law firm outsourced
    /// (2016 Chinese law; it pays, but it is no training data for this lab). Ordinary cards pay as before.
    /// </summary>
    public sealed partial class XgSim
    {
        public const double BountyChance = .08, BountyMultiplier = 8;
        public const int BountyLevelUp = 2;

        static readonly (string q, string qEn, bool yes, string why, string whyEn)[] LawBank =
        {
            ("借条上没写还款日期，出借人可以随时要求还钱（给对方必要的准备时间）吗？", "An IOU names no repayment date. May the lender ask for the money at any time (with reasonable notice)?", true,
                "《合同法》第 62 条：履行期限不明确的，债权人可以随时要求履行，但应给对方必要的准备时间。", "Contract Law art. 62: with no clear deadline the creditor may demand performance at any time, with reasonable notice."),
            ("网购的普通商品，签收后 7 天内一般可以无理由退货吗？", "Ordinary goods bought online: can they usually be returned without reason within 7 days of delivery?", true,
                "《消费者权益保护法》第 25 条（2014 年起）：网购等远程购物，7 日内无理由退货。", "Consumer Protection Law art. 25 (since 2014): distance purchases may be returned within 7 days without reason."),
            ("按你的尺寸定制的商品，也适用 7 天无理由退货吗？", "Goods made to your own measurements: do they also get the 7-day no-reason return?", false,
                "第 25 条把消费者定作的商品、鲜活易腐的商品等排除在外。", "Art. 25 excludes custom-made goods, perishables and a few others."),
            ("劳动合同的试用期最长可以约定一年吗？", "Can an employment contract set a probation period of a full year?", false,
                "《劳动合同法》第 19 条：试用期最长不得超过六个月。", "Labour Contract Law art. 19: probation may not exceed six months."),
            ("同一用人单位和同一劳动者只能约定一次试用期吗？", "May the same employer set a probation period with the same worker only once?", true,
                "《劳动合同法》第 19 条：同一用人单位与同一劳动者只能约定一次试用期。", "Labour Contract Law art. 19: once only."),
            ("入职超过一个月还没签书面劳动合同，单位应当每月付两倍工资吗？", "More than a month in and still no written contract: does the employer owe double pay each month?", true,
                "《劳动合同法》第 82 条：超过一个月不满一年未订立书面合同，应向劳动者每月支付二倍工资。", "Labour Contract Law art. 82: double pay from the second month up to a year."),
            ("醉酒驾驶机动车，在 2016 年的中国属于犯罪吗？", "Is drunk driving a crime in China in 2016?", true,
                "2011 年《刑法修正案（八）》增设危险驾驶罪，醉驾入刑。", "Since the 2011 Criminal Law amendment, drunk driving is the crime of dangerous driving."),
            ("网上造谣诽谤，同一条被转发超过 500 次，可能构成诽谤罪的「情节严重」吗？", "Online defamation reposted more than 500 times: can it count as 'serious' for criminal defamation?", true,
                "2013 年最高法、最高检的司法解释：被点击浏览 5000 次以上或转发 500 次以上，属于情节严重。", "The 2013 judicial interpretation: 5,000 views or 500 reposts make it serious."),
            ("养的狗咬伤了人，只要主人当时不在场，就不用担责吗？", "A dog bites someone. Is the owner off the hook if they were not there?", false,
                "《侵权责任法》第 78 条：饲养的动物造成他人损害，饲养人或管理人应当承担侵权责任。", "Tort Law art. 78: the keeper is liable for harm their animal does."),
            ("法定结婚年龄是男不早于 22 周岁、女不早于 20 周岁吗？", "Is the legal marrying age at least 22 for men and 20 for women?", true,
                "《婚姻法》第 6 条。", "Marriage Law art. 6."),
            ("一方婚前全款买的房子，结婚以后会自动变成夫妻共同财产吗？", "A flat one spouse bought outright before marriage: does it become joint property on marriage?", false,
                "《婚姻法》第 18 条：一方的婚前财产为夫妻一方的财产。", "Marriage Law art. 18: premarital property stays that spouse's own."),
            ("商家写「最终解释权归本店所有」，这条格式条款一定有效吗？", "A shop's small print says 'the shop reserves the right of final interpretation'. Is that clause always valid?", false,
                "《消费者权益保护法》第 26 条、《合同法》第 40 条：排除消费者权利、加重其责任的格式条款无效。", "Consumer Protection Law art. 26, Contract Law art. 40: standard terms that strip consumers' rights are void."),
            ("机动车和没有过错的行人发生事故，机动车一方要承担赔偿责任吗？", "A car hits a pedestrian who did nothing wrong. Is the car's side liable?", true,
                "《道路交通安全法》第 76 条。", "Road Traffic Safety Law art. 76."),
            ("租房没约定租期，双方都可以随时解除合同，只要提前合理通知吗？", "A tenancy with no fixed term: may either side end it at any time with reasonable notice?", true,
                "《合同法》第 232 条：视为不定期租赁，当事人可以随时解除，出租人应在合理期限前通知。", "Contract Law art. 232: an open-ended tenancy may be ended at any time, with reasonable notice from the landlord."),
            ("2016 年工资薪金的个税起征点是每月 5000 元吗？", "In 2016, is the income-tax threshold on wages 5,000 yuan a month?", false,
                "2011 年起是 3500 元，2018 年 10 月才提到 5000 元。", "It was 3,500 from 2011; it rose to 5,000 only in October 2018."),
            ("没经过本人同意，把别人的照片拿去做商业广告，侵犯肖像权吗？", "Using someone's photo in an advertisement without their consent: does it infringe their portrait right?", true,
                "《民法通则》第 100 条：未经本人同意，不得以营利为目的使用公民的肖像。", "General Principles of Civil Law art. 100."),
        };

        /// <summary>Turns a fresh logic-desk card into a bounty now and then: a harder logic question, or a law question.</summary>
        void MaybeBounty(XgCard card, XgDesk info)
        {
            if (info == null || info.kind != XgDeskKind.Logic || card.gold || card.kind == "shutdown" || S.handCorrect < 10) return;
            if (Roll() >= BountyChance) return;
            card.bounty = true;
            if (Roll() < .5)
            {
                var l = LawBank[(int)(Roll() * LawBank.Length) % LawBank.Length];
                card.law = true; card.truth = l.yes;
                card.question = l.q; card.questionEn = l.qEn; card.why = l.why; card.whyEn = l.whyEn;
                card.category = "法律（律所外包）"; card.categoryEn = "Law (outsourced by a law firm)";
            }
            else
            {
                var q = XgLogic.Generate(card.seed, card.level + BountyLevelUp);
                card.truth = q.truth; card.level = q.level;
                card.question = q.text; card.questionEn = q.textEn; card.why = q.why; card.whyEn = q.whyEn;
                card.category = q.category; card.categoryEn = q.categoryEn;
            }
        }
    }
}
