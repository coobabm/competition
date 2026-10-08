using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.Zhongbao;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Editor
{
    /// <summary>
    /// An edit-mode smoke test of the 摆渡众包 pages (no Play mode): it builds the 标注台, 企业订单, 分包, 结算 and 信用 pages
    /// on a throw-away canvas with a stub host, refreshes them against a few lab states, presses the answer buttons and
    /// the dragged spider's API, and reports exceptions and any control that falls outside the page area. Menu:
    /// Tools / LingGuang / Smoke test 摆渡众包 pages. Nothing is saved; the canvas is destroyed afterwards.
    /// </summary>
    public static class XgPlatformSmoke
    {
        sealed class Wallet : IXgHost
        {
            public double money = 2000;
            public double Compute => 1;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double a) { if (a < 0 || money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
            public string Blocker => null;
        }

        sealed class Stub : IXgPageHost
        {
            public XgSim sim; public Wallet wallet = new Wallet(); public XgJuice juice;
            public XingGuangController Controller => null;
            public XgSim Sim => sim;
            public IXgHost Host => wallet;
            public XgJuice Juice => juice;
            public int BuyAmount { get; set; } = 1;
            public bool Visible => true;
            public string Tab => "label";
            public RectTransform ComboChip => null;
            public int refreshes;
            public void Refresh(bool force) { refreshes++; }
            public void ShowToast(string text, float seconds) { }
            public void ShowTab(string id) { }
            public bool BuyNode(XgNode node, RectTransform from) { return false; }
        }

        static readonly StringBuilder Log = new StringBuilder();
        static int failures;

        static void Note(string s) { Log.AppendLine(s); }

        static void Try(string what, Action a)
        {
            try { a(); }
            catch (Exception e) { failures++; Note("FAIL " + what + ": " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace); }
        }

        static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(o);

        [MenuItem("Tools/LingGuang/Smoke test 摆渡众包 pages")]
        public static void Menu() { Debug.Log(Run()); }

        public static string Run()
        {
            Log.Length = 0; failures = 0;
            if (Application.isPlaying) return "skipped: Unity is in Play mode";
            var canvasGo = new GameObject("XgPlatformSmoke", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                canvasGo.hideFlags = HideFlags.DontSave;
                var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                var canvasRt = (RectTransform)canvasGo.transform; canvasRt.sizeDelta = new Vector2(1266, 763);
                var area = new GameObject("Area", typeof(RectTransform)).GetComponent<RectTransform>();
                area.SetParent(canvasRt, false);
                area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(144, 36); area.offsetMax = new Vector2(-12, -70);
                var jgo = new GameObject("Juice", typeof(RectTransform)); jgo.transform.SetParent(canvasRt, false);
                var stub = new Stub { sim = new XgSim { GoldChance = 0 } };
                Try("juice", () => { stub.juice = jgo.AddComponent<XgJuice>(); stub.juice.Init(canvasRt, canvasRt, null); stub.juice.Muted = true; });
                var ui = new XgUi(null, null);

                var label = new XgLabelPage();
                Try("label build", () => { label.Init(stub, ui); label.Build(area); });
                if (label.root == null) return Log + "label page did not build";
                Canvas.ForceUpdateCanvases();
                Try("label refresh new lab", () => { label.Refresh(); label.Tick(.1f); });
                Report("new lab", label, area);
                Dump(label, area, "Hall", "Workspace", "Workspace/Body/AutomaticFeed", "Workspace/Body/Meta", "Workspace/Body/Paper", "Workspace/Body/Timer", "Workspace/Body/Question", "Workspace/Body/Suggestion",
                    "Workspace/Body/AnswerYes", "Workspace/Body/AnswerNo", "Workspace/Body/Feedback", "Workspace/Body/InfoBox", "Workspace/Recent", "Side", "Side/Content/Today", "Side/Content/Today/Row薪", "Side/Content/LingGuang", "Side/Content/Shop", "Side/Content/Shop/Row自", "Side/Content/Shop/Row包");

                // The arithmetic desk is true/false.
                Try("arith labels", () =>
                {
                    var yes = Field<XgBtn>(label, "yes"); var no = Field<XgBtn>(label, "no");
                    Note("buttons on arith: '" + yes.label.text + "' / '" + no.label.text + "'");
                    if (!yes.label.text.StartsWith("对") && !yes.label.text.StartsWith("True")) { failures++; Note("FAIL arith yes button not 对/True"); }
                    string q = Field<TMP_Text>(label, "text").text;
                    Note("arith card: " + q);
                });

                // Answer a few cards by pressing the buttons (right and wrong).
                Try("answers", () =>
                {
                    var yes = Field<XgBtn>(label, "yes"); var no = Field<XgBtn>(label, "no");
                    for (int i = 0; i < 6; i++)
                    {
                        var card = stub.sim.Card(stub.sim.S.desk);
                        bool wrong = i % 3 == 2;
                        (card.truth != wrong ? yes : no).button.onClick.Invoke();
                        label.Tick(.05f);
                    }
                    Note("after 6 answers: money " + stub.wallet.money.ToString("0.00") + " fines " + stub.sim.S.handFines.ToString("0.00") + " today " + stub.sim.DayDone + "/" + stub.sim.DayRight);
                });
                Report("after answers", label, area);

                // More desks, a checkpoint, the review inbox, the spider.
                Try("open desks", () =>
                {
                    var s = stub.sim;
                    s.S.stage = s.S.stageVision = s.S.stageSequence = 3;
                    s.S.desksOpen.AddRange(new[] { "danmu", "mnist", "poems", "meme", "headline", "review", "cifar", "go", "longtext" });
                    s.S.best.Add(new XgBest { dataset = "arith", arch = "rnn", acc = .8 });
                    s.S.best.Add(new XgBest { dataset = "logic", arch = "rnn", acc = .5 });
                    s.S.spiderIntroDone = true;
                    label.RebuildTabs();
                    label.Refresh();
                });
                Report("open desks", label, area);
                Dump(label, area, "Side/Content/Today", "Side/Content/LingGuang", "Side/Content/Shop", "Side/Content/Shop/Row自", "Side/Content/Shop/Row包", "Side/Content/Shop/ToTrain");
                // Auto-answer owned, collaboration on, the platform active, the 摆渡众包 panel stacked.
                Try("coop", () =>
                {
                    var s = stub.sim;
                    s.S.unlocked.Add("label.coop"); s.S.autoLevel = 2; s.S.autoLabelRevealed = true;
                    label.Refresh(); label.Tick(.1f);
                });
                Dump(label, area, "Side/Content/Shop", "Side/Content/Shop/Row自", "Side/Content/Shop/Row包", "Side/Content/Shop/Collaboration", "Side/Content/Shop/PlatformChip", "Side/Content/Shop/ToTrain");
                Report("coop", label, area);
                foreach (var d in XgCatalog.Desks)
                    Try("select " + d.id, () => { if (stub.sim.DeskOpen(d.id)) { stub.sim.SelectDesk(d.id); label.Refresh(); label.Tick(.05f); } });
                Try("back to arith", () => { stub.sim.SelectDesk("arith"); label.Refresh(); });

                Try("chapter complete card", () =>
                {
                    stub.sim.S.chapterComplete = true;
                    label.Refresh(); label.Tick(.1f);
                    Note("special card: " + Field<TMP_Text>(label, "specialInfo").text.Replace("\n", " ").Substring(0, 40));
                    Report("special card", label, area);
                    var yes = Field<XgBtn>(label, "yes"); yes.button.onClick.Invoke();
                    stub.sim.S.chapterComplete = false;
                    label.Refresh(); label.Tick(.1f);
                });
                Try("spider api", () =>
                {
                    label.SetSpiderRole(XgLabelPage.SpiderRole.Held); label.SetDropHint(true); label.SetDropHint(false);
                    label.SetSpiderRole(XgLabelPage.SpiderRole.Working);
                    Note("blocker on arith: " + (label.SpiderBlocker() ?? "none"));
                    int right = 0, wrong = 0; double money = stub.wallet.money;
                    for (int i = 0; i < 40; i++)
                    {
                        if (!label.SpiderPeek(out bool yes)) { Note("peek failed at " + i); break; }
                        var r = label.SpiderHandPress(yes);
                        if (!r.accepted) { failures++; Note("FAIL spider press not accepted"); break; }
                        if (r.correct) right++; else wrong++;
                        label.Tick(.05f);
                    }
                    Note("spider 40 presses: right " + right + " wrong " + wrong + " money " + money.ToString("0.00") + " -> " + stub.wallet.money.ToString("0.00") + " fines " + stub.sim.S.handFines.ToString("0.00"));
                    stub.sim.SelectDesk("logic"); label.Refresh();
                    Note("blocker on logic (50% checkpoint): " + (label.SpiderBlocker() ?? "none"));
                    label.SpiderIdle(label.SpiderBlocker() ?? "ability");
                    label.SetSpiderRole(XgLabelPage.SpiderRole.Home);
                    stub.sim.SelectDesk("arith"); label.Refresh();
                    var press = label.SpiderPress();
                    Note("random help accepted: " + press.accepted + " correct " + press.correct);
                });

                Try("tick many", () => { for (int i = 0; i < 30; i++) { label.Tick(.1f); } });

                // The other pages.
                var contracts = new XgContractsPage();
                Try("contracts", () =>
                {
                    contracts.Init(stub, ui); contracts.Build(area); Canvas.ForceUpdateCanvases();
                    contracts.Refresh(); contracts.Tick(.1f);
                    stub.sim.S.stage = 3;
                    contracts.Refresh();
                });
                var crew = new ZhongbaoSubcontractPage();
                Try("subcontract", () => { crew.Init(stub, ui); crew.Build(area); crew.Refresh(); crew.Tick(.1f); });
                var ledger = new ZhongbaoLedgerPage(null);
                Try("ledger", () => { ledger.Init(stub, ui); ledger.Build(area); ledger.Refresh(); ledger.Tick(.1f); });
                var credit = new ZhongbaoCreditPage();
                Try("credit", () => { credit.Init(stub, ui); credit.Build(area); credit.Refresh(); credit.Tick(.1f); });
                foreach (var page in new XgPage[] { contracts, crew, ledger, credit })
                    if (page.root != null) Report("page " + page.root.name, page, area);

                // English: every page again, listing text that still has Chinese in it.
                using (LingGuangV05.Runtime.GameText.Bind(() => "en-US"))
                {
                    stub.sim.English = true;
                    Try("english", () =>
                    {
                        label.RebuildTabs(); label.Refresh(); contracts.Refresh(); crew.Refresh(); ledger.Refresh(); credit.Refresh();
                        foreach (var page in new XgPage[] { label, contracts, crew, ledger, credit }) CjkLeaks(page);
                    });
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasGo);
            }
            return (failures == 0 ? "SMOKE OK" : "SMOKE FAILURES " + failures) + "\n" + Log;
        }

        static void Dump(XgPage page, RectTransform area, params string[] paths)
        {
            var a = new Vector3[4]; area.GetWorldCorners(a);
            var c = new Vector3[4];
            foreach (var path in paths)
            {
                var t = page.root.Find(path) as RectTransform;
                if (t == null) { Note("  [" + path + "] missing"); continue; }
                t.GetWorldCorners(c);
                // Area-local, y from the top: x, y, w, h.
                Note("  [" + path + "] x " + (c[0].x - a[0].x).ToString("0") + " y " + (a[1].y - c[1].y).ToString("0") + " w " + (c[2].x - c[0].x).ToString("0") + " h " + (c[1].y - c[0].y).ToString("0"));
            }
        }

        /// <summary>Lists controls whose rectangles leave the page area, and texts that overflow their boxes.</summary>
        static void Report(string when, XgPage page, RectTransform area)
        {
            Canvas.ForceUpdateCanvases();
            var bounds = new Vector3[4]; area.GetWorldCorners(bounds);
            var inside = Rect.MinMaxRect(bounds[0].x, bounds[0].y, bounds[2].x, bounds[2].y);
            var corners = new Vector3[4];
            int outside = 0, empty = 0;
            foreach (var g in page.root.GetComponentsInChildren<Graphic>(false))
            {
                if (g is XgCrawlerLayer || !g.gameObject.activeInHierarchy) continue;
                var rt = g.rectTransform;
                // Scroll contents are allowed to run on; check controls, not their lists.
                if (rt.GetComponentInParent<ScrollRect>() != null) continue;
                rt.GetWorldCorners(corners);
                var r = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
                if (r.width < 1e-4f && r.height < 1e-4f) continue;
                if (r.xMin < inside.xMin - 1 || r.xMax > inside.xMax + 1 || r.yMin < inside.yMin - 1 || r.yMax > inside.yMax + 1)
                {
                    if (g.GetComponent<XgGlow>() != null || rt.name == "Glow" || rt.name == "Fx") continue;
                    outside++;
                    if (outside <= 8) Note("  outside " + Path(rt, page.root) + " " + r);
                }
            }
            foreach (var t in page.root.GetComponentsInChildren<TMP_Text>(false))
            {
                if (!t.gameObject.activeInHierarchy || t.rectTransform.GetComponentInParent<ScrollRect>() != null) continue;
                t.ForceMeshUpdate();
                var pref = t.GetPreferredValues(t.text);
                var size = t.rectTransform.rect.size;
                bool wrapOff = t.textWrappingMode == TextWrappingModes.NoWrap;
                if (!t.enableAutoSizing && size.x > 2 && size.y > 2 && (pref.y > size.y + 4 && !wrapOff || wrapOff && t.overflowMode == TextOverflowModes.Overflow && pref.x > size.x + 6))
                {
                    empty++;
                    if (empty <= 10) Note("  text may overflow " + Path(t.rectTransform, page.root) + " pref " + pref + " box " + size + " '" + Short(t.text) + "'");
                }
            }
            Note(when + ": " + outside + " controls outside the area, " + empty + " texts that may overflow (page " + page.root.name + ")");
        }

        static void CjkLeaks(XgPage page)
        {
            int n = 0;
            foreach (var t in page.root.GetComponentsInChildren<TMP_Text>(false))
            {
                if (!t.gameObject.activeInHierarchy || string.IsNullOrEmpty(t.text)) continue;
                foreach (char c in t.text)
                    if (c >= 0x4E00 && c <= 0x9FFF)
                    {
                        if (++n <= 120) Note("  Chinese left in English mode: " + Path(t.rectTransform, page.root) + " '" + Short(t.text) + "'");
                        break;
                    }
            }
            Note("english " + page.root.name + ": " + n + " texts with Chinese");
        }

        static string Short(string s) { s = (s ?? "").Replace("\n", " "); return s.Length > 40 ? s.Substring(0, 40) : s; }

        static string Path(Transform t, Transform root)
        {
            var parts = new List<string>();
            while (t != null && t != root) { parts.Add(t.name); t = t.parent; }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
