using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>数据飞轮: unlabelled user logs waiting per dataset.</summary>
        public List<XgLabelCount> logs = new List<XgLabelCount>();
        /// <summary>Datasets whose logs the deployed model labels by itself.</summary>
        public List<string> logAuto = new List<string>();
        /// <summary>Logs ever collected, labelled by hand and labelled by the model.</summary>
        public double logsTotal, logsHand, logsAuto;
    }

    /// <summary>
    /// 数据飞轮 (design v1.1 §11.8.4): a signed contract that meets its threshold sends back unlabelled user logs
    /// (<see cref="LogPerSec"/>, about twice its ¥/s, a tenth of that before stage 5). Logs are labelled two ways:
    /// by hand on the desk (clean; each one is worth <see cref="LogHandSamples"/> samples because it is real user
    /// data) or by the deployed model (fast, but wrong as often as the model is: noise = 1 − its accuracy, and those
    /// wrong rows are its own mistakes, so they count towards 近亲繁殖). The logs hold users' chats and private
    /// lives, which is where the rule card 「不偷看聊天记录」 comes from.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>Logs per second per ¥/s of a contract's base income (design v1.1 §13).</summary>
        public const double LogPerIncome = 8;
        /// <summary>Before stage 5 the flywheel turns slowly, so buying data still matters.</summary>
        public const double LogEarlyFactor = .1;
        public const int FlywheelStage = 5;
        public const double LogPileCap = 200000;
        /// <summary>Samples one log the model labelled itself is worth (it mostly repeats what it already knows).</summary>
        public const double PseudoLabelWorth = .25;
        /// <summary>Samples one hand-labelled log is worth.</summary>
        public const double LogHandSamples = 4;
        /// <summary>Logs per second the model labels per unit of compute (square root).</summary>
        public const double LogAutoRate = 400;
        public const double LogAutoGpuSeconds = .002;
        public const string PrivacyRuleSource = "logs";
        /// <summary>Cards the 底层规则 page can hold: the shutdown card and 15 rules (with 「不偷看聊天记录」).</summary>
        public const int MaxRuleCards = 16;

        /// <summary>Logs per second a contract sends back once it meets its threshold (at stage 5 and later).</summary>
        public static double LogPerSec(XgContract c) => c == null ? 0 : c.income * LogPerIncome;

        /// <summary>Logs per second this contract sends back now.</summary>
        public double LogRate(XgContract c) => c != null && ContractIncome(c) > 0 ? LogPerSec(c) * (S.stage >= FlywheelStage ? 1 : LogEarlyFactor) : 0;

        public double LogsPerSecond(string dataset)
        {
            double n = 0;
            foreach (var c in XgCatalog.Contracts) if (c.dataset == dataset) n += LogRate(c);
            return n;
        }

        public double LogsPerSecondTotal { get { double n = 0; foreach (var c in XgCatalog.Contracts) n += LogRate(c); return n; } }

        public double Logs(string dataset) => Count(S.logs, dataset);
        public double LogsWaiting { get { double n = 0; foreach (var l in S.logs) n += l.count; return n; } }
        public bool LogAutoOn(string dataset) => S.logAuto.Contains(dataset);

        /// <summary>Share of logs the deployed model would label wrong (1 − its accuracy).</summary>
        public double LogAutoNoise(string dataset) => Math.Max(0, Math.Min(1, 1 - (UseBoard ? BinaryAccuracy(dataset, BestAcc(dataset)) : BestAcc(dataset))));

        /// <summary>Why the model cannot label this dataset's logs (null when it can).</summary>
        public string LogAutoBlocker(string dataset)
        {
            if (XgCatalog.Dataset(dataset) == null) return T("未知数据集", "Unknown dataset");
            if (BestAcc(dataset) <= 0) return T("先训练出一个检查点", "Train a checkpoint first");
            return null;
        }

        /// <summary>Switches automatic labelling of this dataset's logs on or off.</summary>
        public bool SetLogAuto(string dataset, bool on)
        {
            if (!on) return S.logAuto.Remove(dataset);
            var why = LogAutoBlocker(dataset);
            if (why != null) { Say(why); return false; }
            if (!S.logAuto.Contains(dataset)) S.logAuto.Add(dataset);
            return true;
        }

        /// <summary>
        /// The deployed model labels up to <paramref name="max"/> logs at once: right ones become samples, wrong ones
        /// become its own noise. Returns how many were labelled.
        /// </summary>
        public double AutoLabelLogs(string dataset, IXgHost host, double max = double.MaxValue)
        {
            if (host == null || LogAutoBlocker(dataset) != null || host.Blocker != null || !(host.Compute > 0)) return 0;
            double n = Math.Floor(Math.Min(Logs(dataset), max));
            if (n <= 0) return 0;
            LabelLogs(dataset, n, host);
            return n;
        }

        void LabelLogs(string dataset, double n, IXgHost host)
        {
            double wrong = n * LogAutoNoise(dataset);
            SetCount(S.logs, dataset, Math.Max(0, Logs(dataset) - n));
            // A label the model already agrees with teaches it little (self-training): worth a quarter of a fresh one.
            SetCount(S.dataExtra, dataset, ExtraSamples(dataset) + (n - wrong) * (UseBoard ? PseudoLabelWorth : 1));
            if (wrong > 0) SetCount(S.noise, dataset, Noise(dataset) + wrong);
            S.logsAuto += n;
            host.Train(n * LogAutoGpuSeconds);
            foreach (var run in Runs) if (run.dataset == dataset) Evaluate(run);
        }

        /// <summary>A right hand answer on a desk with logs waiting labels a real log: extra samples, same combo.</summary>
        double HandLabelLog(string desk)
        {
            if (Logs(desk) < 1) return 0;
            SetCount(S.logs, desk, Logs(desk) - 1);
            SetCount(S.dataExtra, desk, ExtraSamples(desk) + LogHandSamples - 1);
            S.logsHand++;
            foreach (var run in Runs) if (run.dataset == desk) Evaluate(run);
            return LogHandSamples - 1;
        }

        void TickFlywheel(double dt, IXgHost host)
        {
            foreach (var c in XgCatalog.Contracts)
            {
                double rate = LogRate(c);
                if (rate <= 0) continue;
                double before = Logs(c.dataset);
                double add = Math.Min(rate * dt, Math.Max(0, LogPileCap - before));
                if (add <= 0) continue;
                if (S.logsTotal <= 0) Say(T("订单开始回传用户日志：没标注过的真实数据，里面有用户的聊天记录。", "Contracts start sending back user logs: real, unlabelled data, with users' chats in it."));
                SetCount(S.logs, c.dataset, before + add);
                S.logsTotal += add;
            }
            if (host == null || S.logAuto.Count == 0 || host.Blocker != null || !(host.Compute > 0)) return;
            double budget = LogAutoRate * Math.Sqrt(host.Compute) * dt;
            foreach (var dataset in S.logAuto.ToArray())
            {
                if (LogAutoBlocker(dataset) != null) { S.logAuto.Remove(dataset); continue; }
                double n = Math.Min(Logs(dataset), budget);
                if (n <= 0) continue;
                LabelLogs(dataset, n, host);
            }
        }

        void RepairFlywheel()
        {
            if (S.logs == null) S.logs = new List<XgLabelCount>();
            if (S.logAuto == null) S.logAuto = new List<string>();
            S.logs.RemoveAll(n => n == null || XgCatalog.Dataset(n.dataset) == null || !FiniteCollaboration(n.count) || n.count < 0);
            S.logAuto.RemoveAll(id => XgCatalog.Dataset(id) == null);
            if (!FiniteCollaboration(S.logsTotal) || S.logsTotal < 0) S.logsTotal = 0;
            if (!FiniteCollaboration(S.logsHand) || S.logsHand < 0) S.logsHand = 0;
            if (!FiniteCollaboration(S.logsAuto) || S.logsAuto < 0) S.logsAuto = 0;
        }

        /// <summary>The 「不偷看聊天记录」 card is earned once the contracts have sent back any logs.</summary>
        bool PrivacyRuleEarned => S.logsTotal > 0;
    }
}
