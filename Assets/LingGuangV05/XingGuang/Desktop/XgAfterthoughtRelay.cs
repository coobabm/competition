using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Turns the crowd-labelling firsts (the model's first money, the first fine, report, 金标题, captcha, new meme,
    /// SLA result, hiring 阿杰) into the protagonist's inner voice, once per save, and lets the AI ask its stage-5
    /// question in the 对话 log. Attaches itself next to the 灵光 controller at scene load. Presentation only: the
    /// lines and the once-only bookkeeping live in <see cref="XgSim.TakeAfterthought"/>.
    /// </summary>
    public sealed class XgAfterthoughtRelay : MonoBehaviour
    {
        const float ReflectionPoll = 5f;

        XingGuangController controller;
        XgSim bound;
        float sincePoll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.GetComponent<XgAfterthoughtRelay>() != null) return;
            runtime.gameObject.AddComponent<XgAfterthoughtRelay>();
        }

        void Update()
        {
            if (controller == null) controller = GetComponent<XingGuangController>();
            if (controller == null || controller.Sim == null) return;
            if (!ReferenceEquals(bound, controller.Sim)) Rebind(controller.Sim);
            // InnerVoice.Clear (a skipped cutscene) drops queued lines unseen: free their keys so the next event retries.
            if (queued.Count > 0 && !InnerVoice.Busy) queued.Clear();
            sincePoll += Time.unscaledDeltaTime;
            if (sincePoll < ReflectionPoll || bound.OfflineSimulation) return;
            sincePoll = 0;
            if (bound.TakeReflectionLine() != null)
                PrologueDirector.Desk?.Popup(LingGuangV05.Core.AppNames.AppZh, GameText.T("对话页有一条新消息。", "A new line on the chat page."), 5f);
        }

        void Rebind(XgSim sim)
        {
            Unsubscribe();
            queued.Clear();
            bound = sim;
            bound.AutoRouted += OnRouted;
            bound.QualityFined += OnFined;
            bound.QualityWarned += OnWarned;
            bound.QualityReported += OnReported;
            bound.CaptchaRequired += OnCaptcha;
            bound.CaptchaFlagged += OnFlagged;
            bound.MemeDrifted += OnDrifted;
            bound.SlaSettled += OnSettled;
            bound.WorkerHired += OnHire;
        }

        void Unsubscribe()
        {
            if (bound == null) return;
            bound.AutoRouted -= OnRouted;
            bound.QualityFined -= OnFined;
            bound.QualityWarned -= OnWarned;
            bound.QualityReported -= OnReported;
            bound.CaptchaRequired -= OnCaptcha;
            bound.CaptchaFlagged -= OnFlagged;
            bound.MemeDrifted -= OnDrifted;
            bound.SlaSettled -= OnSettled;
            bound.WorkerHired -= OnHire;
        }

        void OnDestroy() { Unsubscribe(); }

        void OnRouted(XgAutoRecord record) { if (record != null && record.correct && record.pay > 0) Think("first.income"); }
        void OnFined(XgQcFine fine) { Think(fine != null && fine.trap ? "first.trap" : "first.fine"); }
        void OnWarned(double rate) { Think("first.warning"); }
        void OnReported(XgReportReason reason, double seconds) { Think(reason == XgReportReason.SuspectedBot ? "first.bot" : "first.report"); }
        void OnCaptcha() { Think("first.captcha"); }
        void OnFlagged() { Think("first.flagged"); }
        void OnDrifted(XgMemeDriftNotice notice) { if (notice != null) Think("first.drift", notice.meme, string.IsNullOrEmpty(notice.memeEn) ? notice.meme : notice.memeEn); }
        void OnHire(string id)
        {
            var info = XgSim.WorkerInfo(id);
            Think("first.hire", info != null ? info.name : id, info != null ? info.nameEn : id);
        }
        void OnSettled(XgSlaResult result)
        {
            if (result == null) return;
            if (result.outcome == XgSlaOutcome.FiveStar) Think("first.fivestar");
            else if (result.outcome == XgSlaOutcome.Docked) Think("first.docked");
        }

        /// <summary>
        /// Queues an afterthought for the inner voice. It is only marked said once its first beat has really been on
        /// screen; a cutscene that covers it makes the inner voice say it again afterwards, and a line lost to a reload
        /// plays again the next time the same thing happens. Offline catch-up stays silent.
        /// </summary>
        void Think(string key, string argZh = null, string argEn = null)
        {
            if (bound == null || bound.OfflineSimulation || bound.AfterthoughtSaid(key) || queued.Contains(key)) return;
            var thought = bound.PeekAfterthought(key, argZh, argEn);
            if (thought == null) return;
            queued.Add(key);
            var sim = bound;
            var zh = thought.zh.Split('|');
            var en = thought.en.Split('|');
            for (int i = 0; i < zh.Length; i++)
            {
                System.Action shown = null;
                if (i == 0) shown = () => { if (ReferenceEquals(sim, bound)) { sim.MarkAfterthought(key); queued.Remove(key); } };
                InnerVoice.Say(zh[i], i < en.Length ? en[i] : zh[i], InnerVoice.DefaultSeconds, shown);
            }
        }

        /// <summary>Afterthoughts handed to the inner voice but not yet seen.</summary>
        readonly System.Collections.Generic.HashSet<string> queued = new System.Collections.Generic.HashSet<string>();
    }
}
