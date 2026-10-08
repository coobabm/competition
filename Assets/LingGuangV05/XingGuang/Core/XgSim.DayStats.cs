using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>The game date (yyyymmdd) the day counters below belong to.</summary>
        public int dayStatsDay;
        /// <summary>Today at the 标注台: answers given, right ones, samples added, money won net of fines.</summary>
        public int dayDone, dayRight, daySamples;
        public double dayIncome;
    }

    /// <summary>
    /// 今天 on the labelling desk (摆渡众包): what the player and the spider answered by hand on the current game day.
    /// The counters restart when <see cref="XgSim.Today"/> moves on; automatic labels and contracts are not in them.
    /// </summary>
    public sealed partial class XgSim
    {
        void RollDayStats()
        {
            if (S.dayStatsDay == Today) return;
            S.dayStatsDay = Today; S.dayDone = S.dayRight = S.daySamples = 0; S.dayIncome = 0;
        }

        /// <summary>One answer at the desk: right or wrong, the money it made (negative for a fine), the samples it added.</summary>
        void NoteDayAnswer(bool correct, double net, int samples)
        {
            RollDayStats();
            S.dayDone++;
            if (correct) S.dayRight++;
            S.daySamples += Math.Max(0, samples);
            if (net == net && !double.IsInfinity(net)) S.dayIncome += net;
        }

        /// <summary>Answers given today by hand (and by the spider on the player's behalf).</summary>
        public int DayDone { get { RollDayStats(); return S.dayDone; } }
        public int DayRight { get { RollDayStats(); return S.dayRight; } }
        public int DaySamples { get { RollDayStats(); return S.daySamples; } }
        public double DayIncome { get { RollDayStats(); return S.dayIncome; } }
    }
}
