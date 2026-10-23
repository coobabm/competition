using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>Real local date (yyyymmdd) of the last paid board-game win, and how many were paid that day (v4 §3.7).</summary>
        public int gameRewardDay, gameRewardsToday;
    }

    /// <summary>What a finished hub game paid (<see cref="XgSim.ClaimGameReward"/>).</summary>
    public struct XgGameReward
    {
        public double amount;
        /// <summary>Paid wins today after this one (1–<see cref="XgSim.GameRewardsPerDay"/>); 0 when nothing was due.</summary>
        public int paidToday;
        /// <summary>Today's paid wins were already used up (the result still counts in the record).</summary>
        public bool capped;
    }

    /// <summary>
    /// 赢棋奖钱 (玩家反馈 F8, 技能树与自动化 v4 §3.7): beating its own AI at a hub board game pays about a minute of the
    /// current income — max(¥200, income/s × 60) — times the AI's strength tier (×1, ×1.5, ×2.5); a draw pays 30%.
    /// Only the first <see cref="GameRewardsPerDay"/> paid results of a real date count, so it cannot be farmed.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int GameRewardsPerDay = 3;
        public const double GameRewardFloor = 200, GameRewardSeconds = 60, GameDrawShare = .3;
        static readonly double[] GameRewardTiers = { 1, 1.5, 2.5 };

        /// <summary>The AI's strength tier for the reward: 0 (棋路 0–1), 1 (2–3), 2 (4, it learned from the player's games).</summary>
        public int GameRewardTier => BoardLogicLevel >= 4 ? 2 : BoardLogicLevel >= 2 ? 1 : 0;

        /// <summary>Money per second right now: contracts, crowd orders and every desk 自动答题 is earning on.</summary>
        public double CurrentIncomePerSecond(IXgHost host)
        {
            double sum = IncomePerSecond;
            if (host != null) foreach (var plan in AutoDeskPlans(host)) if (plan.Earning) sum += plan.income;
            return double.IsNaN(sum) || double.IsInfinity(sum) ? 0 : Math.Max(0, sum);
        }

        /// <summary>What a result against its own AI would pay at this income and tier (before the daily limit).</summary>
        public static double GameRewardFor(XgGameOutcome outcome, int tier, double incomePerSecond)
        {
            if (outcome == XgGameOutcome.PlayerLost) return 0;
            double rate = double.IsNaN(incomePerSecond) || double.IsInfinity(incomePerSecond) ? 0 : Math.Max(0, incomePerSecond);
            double win = Math.Max(GameRewardFloor, rate * GameRewardSeconds) * GameRewardTiers[Math.Max(0, Math.Min(GameRewardTiers.Length - 1, tier))];
            return Math.Round(outcome == XgGameOutcome.Draw ? win * GameDrawShare : win, 2);
        }

        /// <summary>Paid results left today (<paramref name="day"/> = the real local date as yyyymmdd).</summary>
        public int GameRewardsLeft(int day) => S.gameRewardDay == day ? Math.Max(0, GameRewardsPerDay - S.gameRewardsToday) : GameRewardsPerDay;

        /// <summary>
        /// A hub game against its own AI ended: pays a won (or drawn) game into the wallet, at most
        /// <see cref="GameRewardsPerDay"/> times per real date. A loss, a game against the built-in program or a used-up
        /// day pays nothing. Moving the clock back never resets the count of a later date.
        /// </summary>
        public XgGameReward ClaimGameReward(XgGameOutcome outcome, bool againstOwnAi, IXgHost host, int day)
        {
            var r = new XgGameReward();
            if (!againstOwnAi || host == null || outcome == XgGameOutcome.PlayerLost || day <= 0) return r;
            if (day < S.gameRewardDay) { r.capped = true; return r; }
            if (S.gameRewardDay != day) { S.gameRewardDay = day; S.gameRewardsToday = 0; }
            if (S.gameRewardsToday >= GameRewardsPerDay) { r.capped = true; return r; }
            r.amount = GameRewardFor(outcome, GameRewardTier, CurrentIncomePerSecond(host));
            if (r.amount <= 0) return r;
            S.gameRewardsToday++;
            r.paidToday = S.gameRewardsToday;
            host.Earn(r.amount); S.totalIncome += r.amount;
            return r;
        }

        /// <summary>The real local date as the reward counts it (yyyymmdd).</summary>
        public static int RewardDay(DateTime local) => local.Year * 10000 + local.Month * 100 + local.Day;
    }
}
