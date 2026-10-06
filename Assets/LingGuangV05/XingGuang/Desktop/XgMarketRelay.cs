using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Carries the lab's market events to the desktop: 摆渡众包 tray popups for 新题型 and 甲方高价单, the 网吧 friends'
    /// lines into the YY group (from the lab's persistent outbox, one every couple of seconds, only once YY is up),
    /// and the desktop clock's time of day into the lab (the 网吧 boss works nights). Attaches itself next to the
    /// 灵光 controller at scene load, so neither the controller nor the scene needs editing. Presentation only.
    /// </summary>
    public sealed class XgMarketRelay : MonoBehaviour
    {
        public const string Group = "netbar";
        const float LineGap = 2.5f;

        XingGuangController controller;
        XgSim bound;
        float sinceLine = LineGap;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Attach()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.GetComponent<XgMarketRelay>() != null) return;
            runtime.gameObject.AddComponent<XgMarketRelay>();
        }

        static string T(string zh, string en) => GameText.T(zh, en);
        static string Who => T("摆渡众包", "Bodu Crowdsourcing");
        static string Yuan(double v) => "¥" + v.ToString(v >= 10 ? "0" : "0.##", CultureInfo.InvariantCulture);
        static string Pct(double v) => (v * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
        static void Popup(string text, float seconds) { PrologueDirector.Desk?.Popup(Who, text, seconds); }

        void Update()
        {
            if (controller == null) controller = GetComponent<XingGuangController>();
            if (controller == null || controller.Sim == null) return;
            if (!ReferenceEquals(bound, controller.Sim)) Rebind(controller.Sim);
            var runtime = controller.runtime;
            if (runtime != null && runtime.Sim != null) bound.ClockOfDaySeconds = GameCalendar.Now(runtime.Sim.S).TimeOfDay.TotalSeconds;
            sinceLine += Time.unscaledDeltaTime;
            if (sinceLine >= LineGap && bound.PendingYYLines > 0) DeliverLine();
        }

        void Rebind(XgSim sim)
        {
            Unsubscribe();
            bound = sim;
            bound.MemeDrifted += OnDrifted;
            bound.SlaSettled += OnSettled;
        }

        void Unsubscribe()
        {
            if (bound == null) return;
            bound.MemeDrifted -= OnDrifted;
            bound.SlaSettled -= OnSettled;
        }

        void OnDestroy() { Unsubscribe(); }

        /// <summary>One queued line into the 网吧 group, tagged with the speaker like the story's group lines.</summary>
        void DeliverLine()
        {
            var hub = YYChatHub.Instance;
            if (hub == null || hub.S == null || hub.Conversation(Group) == null) return;
            var line = bound.TakeYYLine();
            if (line == null) return;
            var info = XgSim.WorkerInfo(line.from);
            string name = info == null ? line.from : T(info.name, info.nameEn);
            hub.Receive(Group, "[" + name + "] " + T(line.zh, line.en));
            sinceLine = 0;
        }

        void OnDrifted(XgMemeDriftNotice notice)
        {
            if (notice == null || notice.desks.Count == 0) return;
            var zh = new List<string>(); var en = new List<string>();
            foreach (var d in notice.desks) { zh.Add(XgSim.DriftQueueName(d, false)); en.Add(XgSim.DriftQueueName(d, true)); }
            int month = notice.month % 100;
            Popup(T(string.Join("、", zh) + "题库更新：" + month + " 月新梗「" + notice.meme + "」\n检查点在这些桌上的准确率 −15%，手标或重新训练能追回来。",
                string.Join(", ", en) + ": question bank updated with month " + month + "'s new meme, " + notice.meme + " (" + notice.memeEn + ").\nYour checkpoint loses 15 points there until you hand-label or retrain."), 9);
        }

        void OnSettled(XgSlaResult r)
        {
            var offer = XgSim.SlaOffer(r.id);
            if (offer == null) return;
            string client = T(offer.client, offer.clientEn);
            switch (r.outcome)
            {
                case XgSlaOutcome.FiveStar:
                    Popup(client + T("：五星好评 ★★★★★\n抽检合格率 " + Pct(r.PassRate) + "，尾款 " + Yuan(r.paid) + " 已结清。",
                        ": five stars ★★★★★\nSpot-check pass rate " + Pct(r.PassRate) + "; the " + Yuan(r.paid) + " balance is paid."), 9);
                    break;
                case XgSlaOutcome.Docked:
                    Popup(client + T("：质量不达标，扣尾款\n抽检合格率 " + Pct(r.PassRate) + " < 95%，尾款只付一半：" + Yuan(r.paid) + "。",
                        ": quality below the clause, balance docked\nPass rate " + Pct(r.PassRate) + " < 95%; only half the balance is paid: " + Yuan(r.paid) + "."), 9);
                    break;
                case XgSlaOutcome.Unjudged:
                    Popup(client + T("：验收通过\n抽检不足 " + XgSim.SlaMinChecks + " 条，尾款 " + Yuan(r.paid) + " 全额结清。",
                        ": accepted\nFewer than " + XgSim.SlaMinChecks + " spot checks; the " + Yuan(r.paid) + " balance is paid in full."), 8);
                    break;
                default:
                    Popup(client + T("：账号被举报，合作终止\n尾款 " + Yuan(r.forfeited) + " 作废，信用分再 −" + XgSim.SlaCancelCredit.ToString("0", CultureInfo.InvariantCulture) + "。",
                        ": your account was reported, contract cancelled\nThe " + Yuan(r.forfeited) + " balance is forfeited and credit drops another " + XgSim.SlaCancelCredit.ToString("0", CultureInfo.InvariantCulture) + "."), 10);
                    break;
            }
        }
    }
}
