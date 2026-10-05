using System;

namespace LingGuangV05.Core.Incremental
{
    /// <summary>
    /// Fixed chapter-one content (set in 2016): five jobs, three hardware items and the 7x5 neural skill tree.
    /// Tree uses the same axial hex coordinates as the chapter-one board (q = column 0..6, r = row 0..4).
    /// </summary>
    public static class IncrementalCatalog
    {
        public const int Columns = 7, Rows = 5;
        public const int InputQ = 0, InputR = 2;

        public static readonly JobDef[] Jobs =
        {
            new JobDef { id = "spam", name = "垃圾邮件分拣", description = "网吧老板的邮箱。最简单的活，几乎不会错。",
                rate = .2, score = 1, pay = .4, difficulty = 0, baseCost = 10, growth = 1.13, milestones = new[] { 25, 50, 100, 200 } },
            new JobDef { id = "review", name = "商品评论好坏", description = "寻宝卖家的评论区：好评还是差评。",
                rate = .12, score = 6, pay = 1.6, difficulty = .05, baseCost = 400, growth = 1.14, milestones = new[] { 20, 40, 80, 160 } },
            new JobDef { id = "news", name = "新闻分类", description = "资讯 App 的推荐流：体育、财经还是娱乐。",
                rate = .07, score = 36, pay = 6.4, difficulty = .10, baseCost = 1.2e4, growth = 1.14, milestones = new[] { 30, 60, 120 } },
            new JobDef { id = "captcha", name = "验证码识别", description = "打码平台：春运抢票和注册验证码。很难，但按题结算很高。",
                rate = .045, score = 220, pay = 26, difficulty = .16, baseCost = 4e5, growth = 1.15, milestones = new[] { 25, 50, 100 } },
            new JobDef { id = "translate", name = "中英翻译", description = "跨境电商的商品说明。需要很准的网络。",
                rate = .03, score = 1300, pay = 100, difficulty = .22, baseCost = 1.4e7, growth = 1.15, milestones = new[] { 20, 40, 80 } },
        };

        public static readonly HardwareDef[] Hardware =
        {
            new HardwareDef { id = "gpu750ti", name = "二手亮影 750Ti 显卡", description = "2G 显存的二手卡。所有活的速度 +1 份算力。",
                basePrice = 450, growth = 1.25, compute = 1, watts = 60, isGpu = true },
            new HardwareDef { id = "case", name = "扩展机箱", description = "多插 2 张显卡。",
                basePrice = 500, growth = 1.6, slots = 2, watts = 45 },
            new HardwareDef { id = "gpu1080", name = "亮影 1080 显卡", description = "今年的旗舰卡，8G 显存：4 份算力，功耗 180W。",
                basePrice = 5299, growth = 1.25, compute = 4, watts = 180, isGpu = true, revealAtTotalMoney = 5000 },
        };

        public const int GpuUsed = 0, Case = 1, GpuFlagship = 2;

        // Letter grid, row by row (r = 0..4), column q = 0..6.
        // I input, M ×mult, S speed, A accuracy, H manual, E efficiency, 2..5 job unlock, Y yes port, N no port.
        private static readonly string[] Grid =
        {
            "HAMSAM5",
            "SM2AMSN",
            "ISHM3AY",
            "AHSESMB",
            "EMA4MSM",
        };

        public static readonly SkillCell[] Cells = BuildCells();

        public static SkillCell Get(int q, int r)
        {
            if (q < 0 || q >= Columns || r < 0 || r >= Rows) return null;
            return Cells[q * Rows + r];
        }

        public static bool Adjacent(int q1, int r1, int q2, int r2)
        {
            int dq = q1 - q2, dr = r1 - r2;
            return (dq != 0 || dr != 0) && Math.Abs(dq) <= 1 && Math.Abs(dr) <= 1 && Math.Abs(dq + dr) <= 1;
        }

        public static int Distance(int q1, int r1, int q2, int r2)
        {
            int dq = q1 - q2, dr = r1 - r2;
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
        }

        private static SkillCell[] BuildCells()
        {
            var cells = new SkillCell[Columns * Rows];
            for (int r = 0; r < Rows; r++)
                for (int q = 0; q < Columns; q++)
                {
                    var c = new SkillCell { q = q, r = r };
                    switch (Grid[r][q])
                    {
                        case 'I': c.effect = SkillEffect.Input; c.name = "输入层"; c.description = "邮件从这里进入网络。"; break;
                        case 'M': c.effect = SkillEffect.Mult; c.value = .5; c.name = "特征组合"; c.description = "倍率 +50%（加法叠加）：所有活的分数和报酬。"; break;
                        case 'B': c.effect = SkillEffect.Mult; c.value = 1; c.name = "深层特征"; c.description = "倍率 +100%（加法叠加）：所有活的分数和报酬。"; break;
                        case 'S': c.effect = SkillEffect.Speed; c.value = .3; c.name = "并行通路"; c.description = "所有活的速度 +30%（加法叠加）。"; break;
                        case 'A': c.effect = SkillEffect.Accuracy; c.value = .06; c.name = "纠错回路"; c.description = "识别率 +6%。越难的活越吃识别率；入学考试要 75%。"; break;
                        case 'H': c.effect = SkillEffect.Manual; c.value = .5; c.name = "人工经验"; c.description = "你亲手答对一题，额外获得 0.5 秒的自动产量（分和 ¥）。"; break;
                        case 'E': c.effect = SkillEffect.Efficiency; c.value = .8; c.name = "低功耗剪枝"; c.description = "显卡功耗 ×0.8，电费更低、能插更多卡。"; break;
                        case 'Y': c.effect = SkillEffect.OutputYes; c.value = 2; c.name = "输出：是"; c.description = "分数和报酬 ×2，识别率 +5%。点亮是、否两个输出才能参加入学考试。"; break;
                        case 'N': c.effect = SkillEffect.OutputNo; c.value = 2; c.name = "输出：否"; c.description = "分数和报酬 ×2，识别率 +5%。点亮是、否两个输出才能参加入学考试。"; break;
                        default:
                            int job = Grid[r][q] - '1';
                            if (job < 1 || job >= Jobs.Length) throw new InvalidOperationException("Bad skill grid cell " + Grid[r][q]);
                            c.effect = SkillEffect.Job; c.value = job; c.name = "新技能：" + Jobs[job].name;
                            c.description = "解锁新活「" + Jobs[job].name + "」。";
                            break;
                    }
                    cells[q * Rows + r] = c;
                }
            return cells;
        }
    }
}
