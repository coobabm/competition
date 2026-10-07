using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.Zhongbao
{
    /// <summary>
    /// 摆渡众包 on the desktop: installs <see cref="ZhongbaoView"/> into its native window, registers the router id
    /// "zhongbao" (so story steps, the to-do note and every <c>router.Open</c> reach it), shows the app from the moment
    /// 灵光.exe is installed, and keeps a short in-memory list of this session's platform notices for the 结算 page.
    /// Attaches itself next to the 灵光 controller at scene load, so neither the scene nor the controller is edited.
    /// It owns no game state: the pages run on <see cref="XingGuangController.Sim"/> and save with the lab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZhongbaoHub : MonoBehaviour
    {
        public const string AppId = XingGuangController.CrowdAppId;
        const int NoticeLimit = 30;

        /// <summary>One platform event seen this session (not saved: the sim keeps totals, not a history).</summary>
        public sealed class Notice { public string clock, zh, en; public double money; }

        public static ZhongbaoHub Instance { get; private set; }
        public ChapterOneRuntime runtime;
        public XingGuangController Controller { get; private set; }
        public ZhongbaoView View { get; private set; }
        public XgSim Sim => Controller != null ? Controller.Sim : null;
        /// <summary>This session's notices, newest first.</summary>
        public readonly List<Notice> Notices = new List<Notice>();
        /// <summary>Fines the platform took this session (the save keeps only the lifetime total).</summary>
        public double SessionFines { get; private set; }

        XgSim logged;
        ChapterOneDesktopBridge binding;
        float nextInstall;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Attach()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.GetComponent<ZhongbaoHub>() != null) return;
            runtime.gameObject.AddComponent<ZhongbaoHub>().runtime = runtime;
        }

        void Awake() { Instance = this; }

        void OnDestroy()
        {
            Unlog();
            if (Instance == this) Instance = null;
        }

        static string T(string zh, string en) => GameText.T(zh, en);

        /// <summary>摆渡众包 is on the desktop once 灵光.exe is installed (test runs count as installed).</summary>
        public bool Unlocked => runtime != null && runtime.Sim != null && (runtime.Sim.AppInstalled || runtime.TestMode) && Sim != null;

        void Update()
        {
            if (runtime == null) runtime = GetComponent<ChapterOneRuntime>();
            if (Controller == null) Controller = GetComponent<XingGuangController>() ?? FindAnyObjectByType<XingGuangController>();
            if (View == null && Time.unscaledTime >= nextInstall)
            {
                nextInstall = Time.unscaledTime + 1;
                View = ZhongbaoView.Install(this);
                if (View != null) Register();
            }
            if (!ReferenceEquals(logged, Sim)) Relog(Sim);
            if (View != null) View.Tick(Unlocked);
        }

        /// <summary>
        /// Adds a router binding for "zhongbao" that opens the window through <see cref="ZhongbaoView"/>. The bridge is
        /// configured while inactive so its Awake hooks the window's open event (taskbar and Start-menu restores too).
        /// </summary>
        void Register()
        {
            var router = GetComponent<ChapterOneDesktopRouter>() ?? FindAnyObjectByType<ChapterOneDesktopRouter>();
            if (router == null || View == null || View.Window == null) return;
            if (router.Get(AppId) != null) { binding = router.Get(AppId); binding.UseCustomView(View); return; }
            var go = new GameObject("ZhongbaoDesktopBridge");
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            binding = go.AddComponent<ChapterOneDesktopBridge>();
            binding.Configure(runtime, null, View.Window, View.Content, null, null, View.Window.taskbarButton, null, null, null, AppId);
            binding.UseCustomView(View);
            go.SetActive(true);
            var list = new List<ChapterOneDesktopBridge>(router.Bindings) { binding };
            router.Configure(list.ToArray());
        }

        /// <summary>Opens 摆渡众包 on a page (label, contracts, subcontract, ledger, credit).</summary>
        public bool Open(string page)
        {
            var router = GetComponent<ChapterOneDesktopRouter>() ?? FindAnyObjectByType<ChapterOneDesktopRouter>();
            return router != null && router.Open(AppId, page);
        }

        // ───────────── this session's platform notices (for 结算) ─────────────

        void Relog(XgSim sim)
        {
            Unlog();
            logged = sim;
            Notices.Clear(); SessionFines = 0;
            if (sim == null) return;
            sim.QualityFined += OnFined;
            sim.QualityWarned += OnWarned;
            sim.QualityReported += OnReported;
            sim.QualityUnfrozen += OnUnfrozen;
            sim.CaptchaFailed += OnCaptchaFailed;
            sim.CaptchaFlagged += OnCaptchaFlagged;
            sim.WorkerHired += OnWorkerHired;
            sim.WorkerLeft += OnWorkerLeft;
        }

        void Unlog()
        {
            if (logged == null) return;
            logged.QualityFined -= OnFined; logged.QualityWarned -= OnWarned;
            logged.QualityReported -= OnReported; logged.QualityUnfrozen -= OnUnfrozen;
            logged.CaptchaFailed -= OnCaptchaFailed; logged.CaptchaFlagged -= OnCaptchaFlagged;
            logged.WorkerHired -= OnWorkerHired; logged.WorkerLeft -= OnWorkerLeft;
            logged = null;
        }

        void Add(string zh, string en, double money = 0)
        {
            string clock = runtime != null && runtime.Sim != null ? GameCalendar.Now(runtime.Sim.S).ToString("HH:mm") : "";
            Notices.Insert(0, new Notice { clock = clock, zh = zh, en = en, money = money });
            if (Notices.Count > NoticeLimit) Notices.RemoveAt(Notices.Count - 1);
        }

        static string Yuan(double v) => "¥" + v.ToString(v >= 10 ? "0" : "0.##", System.Globalization.CultureInfo.InvariantCulture);
        static string DeskName(string id, bool english) { var d = XgCatalog.Desk(id); return d == null ? id : english ? d.nameEn : d.name; }

        void OnFined(XgQcFine fine)
        {
            if (fine == null) return;
            SessionFines += fine.fine;
            string trapZh = fine.trap ? "（金标题）" : "", trapEn = fine.trap ? " (trap item)" : "";
            Add("抽检不合格 · " + DeskName(fine.dataset, false) + trapZh + " · 罚款 " + Yuan(fine.fine),
                "Failed spot check · " + DeskName(fine.dataset, true) + trapEn + " · fined " + Yuan(fine.fine), -fine.fine);
        }

        void OnWarned(double errorRate)
        {
            string pass = ((1 - errorRate) * 100).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%";
            Add("警告：近期抽检合格率 " + pass, "Warning: recent spot-check pass rate " + pass);
        }

        void OnReported(XgReportReason reason, double seconds)
        {
            string minutes = (seconds / 60).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            Add("账号被举报（" + XgSim.ReportReasonText(reason, false) + "），冻结 " + minutes + " 分钟，信用 −" + XgSim.QcCreditReport.ToString("0"),
                "Account reported (" + XgSim.ReportReasonText(reason, true) + "), frozen " + minutes + " min, credit −" + XgSim.QcCreditReport.ToString("0"));
        }

        void OnUnfrozen(bool appealed) { Add(appealed ? "申诉通过，账号解冻" : "冻结期满，账号解冻", appealed ? "Appeal accepted, account unfrozen" : "Freeze over, account unfrozen"); }

        void OnCaptchaFailed(bool timeout, int inARow)
        {
            Add((timeout ? "人机验证超时" : "验证码错误") + "，信用 −" + XgSim.CaptchaCreditFail.ToString("0"),
                (timeout ? "Captcha timed out" : "Wrong captcha") + ", credit −" + XgSim.CaptchaCreditFail.ToString("0"));
        }

        void OnCaptchaFlagged() { Add("验证码代填被识破，信用 −" + XgSim.AutofillFlagCredit.ToString("0"), "Captcha autofill spotted, credit −" + XgSim.AutofillFlagCredit.ToString("0")); }

        void OnWorkerHired(string id)
        {
            var info = XgSim.WorkerInfo(id);
            if (info == null) return;
            Add("雇用 " + info.name + "，先付第一分钟 " + Yuan(info.wage), "Hired " + info.nameEn + ", first minute paid " + Yuan(info.wage), -info.wage);
        }

        void OnWorkerLeft(string id, string reason)
        {
            var info = XgSim.WorkerInfo(id);
            if (info == null) return;
            if (reason == "fired") Add("辞退 " + info.name, "Let " + info.nameEn + " go");
            else Add(info.name + " 走了", info.nameEn + " left");
        }
    }
}
