using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// 借你一张卡 on the desktop (XgSim.CardLoan.cs). When the 「短句」 exam opens and the desktop is quiet, 老周 types his
    /// ask into YY (his group's card burned out). The protagonist opens the side of the case first: a window with the
    /// machine drawn on the left (the cards in their slots, the memory chips, the PSU and the cable to the house line)
    /// and four short pages on the right, each lighting its part, with the save's numbers. Closing the case puts the two
    /// answers on 老周's conversation as reply buttons; his reply is typed out like his other messages. A reload resumes
    /// at the step it was on (a toast reopens the case).
    /// </summary>
    public sealed class ZhouCardLoan : MonoBehaviour
    {
        const float CheckEvery = .5f;
        static readonly Color Lit = new Color32(255, 196, 40, 255), Dim = new Color32(0, 0, 0, 0);

        MonoBehaviour host;
        ChapterOneRuntime runtime;
        float nextCheck;
        bool busy, asked, toastShown;
        RectTransform window;
        TMP_Text title, body, pageNo;
        RectTransform bottom;
        readonly List<GameObject> buttons = new List<GameObject>();
        readonly Dictionary<string, List<Outline>> parts = new Dictionary<string, List<Outline>>();
        List<XgRigPage> pages;
        int page;
        string litPart = "";

        public static ZhouCardLoan Install(MonoBehaviour host)
        {
            if (host == null) return null;
            var z = host.GetComponent<ZhouCardLoan>() ?? host.gameObject.AddComponent<ZhouCardLoan>();
            z.host = host;
            return z;
        }

        static string T(string zh, string en) => GameText.T(zh, en);
        static string T(string[] pair) => GameText.T(pair[0], pair[1]);

        static XingGuangController Lab => XingGuangController.Instance;
        static XgSim Sim { get { var lab = Lab; return lab != null ? lab.Sim : null; } }

        bool Busy()
        {
            if (AiJoinsYy.Playing || AutoLabelEpiphany.Playing || StoryFocus.Active) return true;
            return host is StoryDesktopPresenter presenter && (presenter.CutscenePlaying || presenter.NotificationsHeld);
        }

        void OnDisable() { CloseWindow(); busy = false; asked = false; }

        int CardCount => runtime != null && runtime.Sim != null ? runtime.Sim.Cards.Count : 1;

        void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + CheckEvery;
            if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            var sim = Sim;
            if (runtime == null || runtime.Sim == null || runtime.TestMode || runtime.Sim.InPrologue || sim == null || !sim.DesktopHosted) return;
            var hub = YYChatHub.Instance;
            sim.CardLoanContactAvailable = hub != null && hub.ZhouContactReady;
            if (hub == null || !sim.CardLoanContactAvailable || busy) return;
            switch (sim.S.clStep)
            {
                case 0:
                    // An active story script owns the conversation. Optional knowledge-question buttons never hold the mainline loan.
                    if (!sim.CardLoanDue || Busy() || InnerVoice.Busy || hub.TypingShown == YYChatHub.LaoZhou || hub.ChoicesFor == YYChatHub.LaoZhou) return;
                    StartCoroutine(Ask(sim, hub));
                    break;
                case 1:
                    if (window != null || Busy() || toastShown || !(host is StoryDesktopPresenter p)) return;
                    toastShown = true;
                    p.ShowToast(T("机箱侧板还开着。", "The side of the case is still off."), () => { toastShown = false; Open(Sim); });
                    break;
                case 2:
                    if (!asked && !Busy()) Answers(hub);
                    break;
            }
        }

        // ───────────── 老周 asks ─────────────

        IEnumerator Type(YYChatHub hub, List<string[]> bubbles)
        {
            foreach (var b in bubbles)
            {
                string text = T(b);
                hub.TypingShown = YYChatHub.LaoZhou;
                yield return new WaitForSecondsRealtime(Mathf.Min(LaoZhouYY.TypeMax, LaoZhouYY.TypeBase + LaoZhouYY.TypePerChar * text.Length));
                hub.Receive(YYChatHub.LaoZhou, text);
                if (hub.TypingShown == YYChatHub.LaoZhou) hub.TypingShown = null;
                yield return new WaitForSecondsRealtime(LaoZhouYY.BubbleGap);
            }
        }

        IEnumerator Ask(XgSim sim, YYChatHub hub)
        {
            busy = true;
            var bubbles = sim.CardLoanStart(CardCount);
            runtime.MarkDirty();
            yield return Type(hub, bubbles);
            yield return new WaitForSecondsRealtime(1f);
            while (Busy()) yield return null;
            // Its own lines only: the desktop's other thoughts may go on talking.
            InnerVoice.Say("借卡……", "A card…", 1.8f);
            yield return InnerVoice.SayAndWait("回他之前，先弄明白这台机器到底靠什么让它跑起来。", "Before I answer, I should know what this machine actually runs it on.", 3.2f);
            busy = false;
            Open(Sim);
        }

        // ───────────── the case ─────────────

        XgRigFacts Facts()
        {
            var f = new XgRigFacts();
            var lab = Lab;
            var house = runtime != null ? runtime.Sim : null;
            if (lab != null && lab.Host != null) foreach (var c in lab.Host.Cards) f.cards.Add(c);
            if (house != null)
            {
                f.cases = Mathf.Max(1, house.S.caseCount);
                f.slotsPerCase = house.Config.gpusPerCase;
                f.caseWatts = house.Config.caseWatts;
                f.limitWatts = house.Config.powerLimitWatts;
                f.kwhPrice = house.Config.electricityPrice;
            }
            return f;
        }

        void Open(XgSim sim)
        {
            var desk = PrologueDirector.Desk;
            if (sim == null || desk == null || window != null || sim.S.clStep != 1) return;
            var facts = Facts();
            pages = XgSim.CardLoanTour(facts);
            page = 0;
            var client = desk.Window("Card Loan Case", T("拆开的机箱", "The open case"), Vector2.zero, new Vector2(1000, 620), out window, () => { window = null; });
            client.GetComponent<Image>().color = new Color32(246, 248, 252, 255);
            DrawCase(desk, PrologueDesk.Rect("Case", client, Vector2.zero, new Vector2(0, 1), new Vector2(18, 64), new Vector2(438, -18)), facts);
            var right = PrologueDesk.Rect("Page", client, Vector2.zero, Vector2.one, new Vector2(462, 64), new Vector2(-24, -18));
            pageNo = desk.Text(PrologueDesk.Rect("No", right, new Vector2(0, 1), Vector2.one, new Vector2(0, -34), Vector2.zero), "", 14, PrologueDesk.Muted, TextAlignmentOptions.MidlineLeft);
            title = desk.Text(PrologueDesk.Rect("Title", right, new Vector2(0, 1), Vector2.one, new Vector2(0, -84), new Vector2(0, -36)), "", 28, PrologueDesk.Ink, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            body = desk.Text(PrologueDesk.Rect("Body", right, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -100)), "", 19, PrologueDesk.Ink, TextAlignmentOptions.TopLeft);
            body.lineSpacing = 18;
            bottom = PrologueDesk.Rect("Bottom", client, Vector2.zero, new Vector2(1, 0), new Vector2(18, 12), new Vector2(-18, 52));
            ShowPage();
        }

        void CloseWindow()
        {
            if (window != null) Destroy(window.gameObject);
            window = null;
            parts.Clear();
        }

        void ShowPage()
        {
            if (window == null || pages == null) return;
            foreach (var b in buttons) if (b != null) Destroy(b);
            buttons.Clear();
            var p = pages[page];
            pageNo.text = (page + 1) + " / " + pages.Count;
            title.text = T(p.titleZh, p.titleEn);
            body.text = T(p.zh, p.en);
            litPart = p.part;
            XgJuice.Sfx.Play(XgJuice.Sfx.Id.Tick, 1, .4f);
            var desk = PrologueDirector.Desk;
            bool last = page == pages.Count - 1;
            if (page > 0) buttons.Add(desk.Button(bottom, T("上一页", "Back"), new Vector2(-150, 0), new Vector2(240, 36), () => { page--; ShowPage(); }).gameObject);
            buttons.Add(desk.Button(bottom, last ? T("合上侧板", "Put the side back on") : T("下一页", "Next"), new Vector2(page > 0 ? 150 : 0, 0), new Vector2(240, 36),
                () => { if (last) Closed(); else { page++; ShowPage(); } }, new Color32(214, 230, 255, 255)).gameObject);
        }

        void LateUpdate()
        {
            if (window == null) return;
            float k = .55f + .45f * Mathf.Sin(Time.unscaledTime * 5f);
            foreach (var kv in parts)
                foreach (var o in kv.Value)
                    if (o != null) o.effectColor = kv.Key == litPart ? new Color(Lit.r, Lit.g, Lit.b, k) : Dim;
        }

        void Mark(string part, RectTransform rt)
        {
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectDistance = new Vector2(4, -4);
            o.useGraphicAlpha = false;
            o.effectColor = Dim;
            if (!parts.TryGetValue(part, out var list)) parts[part] = list = new List<Outline>();
            list.Add(o);
        }

        /// <summary>The first case, side off: the board with its slots and cards, the PSU, and the cable to the wall.</summary>
        void DrawCase(PrologueDesk desk, RectTransform box, XgRigFacts f)
        {
            var shell = PrologueDesk.Rect("Shell", box, new Vector2(0, .12f), new Vector2(.78f, 1), Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(shell, new Color32(52, 56, 64, 255), false);
            Mark("case", shell);
            var board = PrologueDesk.Rect("Board", shell, new Vector2(.08f, .3f), new Vector2(.92f, .95f), Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(board, new Color32(30, 92, 62, 255), false);
            desk.Text(PrologueDesk.Rect("Cpu", board, new Vector2(.36f, .74f), new Vector2(.64f, .95f), Vector2.zero, Vector2.zero), "CPU", 16, new Color32(200, 220, 210, 255), TextAlignmentOptions.Center);
            int slots = Mathf.Max(1, f.slotsPerCase);
            for (int i = 0; i < slots; i++)
            {
                float top = .62f - i * .3f;
                var slot = PrologueDesk.Rect("Slot " + i, board, new Vector2(.06f, top - .05f), new Vector2(.94f, top), Vector2.zero, Vector2.zero);
                PrologueDesk.Fill(slot, new Color32(18, 50, 34, 255), false);
                Mark("case", slot);
                if (i >= f.cards.Count)
                {
                    desk.Text(PrologueDesk.Rect("Empty", board, new Vector2(.06f, top - .2f), new Vector2(.94f, top - .05f), Vector2.zero, Vector2.zero), T("空槽", "empty slot"), 15, new Color32(150, 190, 170, 255), TextAlignmentOptions.Center);
                    continue;
                }
                var card = PrologueDesk.Rect("Card " + i, board, new Vector2(.04f, top - .24f), new Vector2(.96f, top - .02f), Vector2.zero, Vector2.zero);
                PrologueDesk.Fill(card, new Color32(26, 26, 30, 255), false);
                Mark("card", card);
                desk.Text(PrologueDesk.Rect("Fans", card, new Vector2(0, 0), new Vector2(.62f, 1), Vector2.zero, Vector2.zero), "◎ ◎", 34, new Color32(120, 126, 140, 255), TextAlignmentOptions.Center);
                desk.Text(PrologueDesk.Rect("Name", card, new Vector2(.5f, .5f), new Vector2(1, 1), Vector2.zero, Vector2.zero), f.cards[i].name, 13, new Color32(118, 200, 90, 255), TextAlignmentOptions.Center);
                var chips = PrologueDesk.Rect("Vram", card, new Vector2(.62f, .1f), new Vector2(.96f, .46f), Vector2.zero, Vector2.zero);
                for (int c = 0; c < 4; c++)
                {
                    var chip = PrologueDesk.Rect("Chip", chips, new Vector2(c / 4f + .02f, 0), new Vector2((c + 1) / 4f - .02f, 1), Vector2.zero, Vector2.zero);
                    PrologueDesk.Fill(chip, new Color32(8, 8, 10, 255), false);
                    Mark("vram", chip);
                }
            }
            var psu = PrologueDesk.Rect("Psu", shell, new Vector2(.08f, .04f), new Vector2(.6f, .24f), Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(psu, new Color32(84, 88, 98, 255), false);
            desk.Text(PrologueDesk.Rect("Label", psu, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), T("电源", "PSU"), 16, Color.white, TextAlignmentOptions.Center);
            Mark("power", psu);
            if (f.cases > 1)
                desk.Text(PrologueDesk.Rect("More", box, new Vector2(0, .02f), new Vector2(.78f, .1f), Vector2.zero, Vector2.zero), T("还有 " + (f.cases - 1) + " 台机箱", "and " + (f.cases - 1) + " more case" + (f.cases > 2 ? "s" : "")), 14, PrologueDesk.Muted, TextAlignmentOptions.Center);
            // The cable to the wall and the house line.
            var cable = PrologueDesk.Rect("Cable", box, new Vector2(.6f, .2f), new Vector2(.9f, .215f), Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(cable, new Color32(40, 40, 40, 255), false);
            var wall = PrologueDesk.Rect("Wall", box, new Vector2(.82f, .14f), new Vector2(1, .4f), Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(wall, new Color32(236, 236, 230, 255), false);
            desk.Text(PrologueDesk.Rect("Socket", wall, new Vector2(0, .4f), new Vector2(1, 1), Vector2.zero, Vector2.zero), T("插座", "Socket"), 18, new Color32(160, 110, 20, 255), TextAlignmentOptions.Center);
            desk.Text(PrologueDesk.Rect("Limit", wall, new Vector2(0, 0), new Vector2(1, .4f), Vector2.zero, Vector2.zero), f.limitWatts.ToString("0") + "W", 13, PrologueDesk.Muted, TextAlignmentOptions.Center);
            Mark("power", wall);
        }

        // ───────────── the answer, in YY ─────────────

        void Closed()
        {
            var sim = Sim;
            if (sim == null || !sim.CardLoanTourDone()) return;
            runtime.MarkDirty();
            CloseWindow();
            XgJuice.Sfx.Play(XgJuice.Sfx.Id.Thud, 1, .5f);
            var thought = XgSim.CardLoanThought(CardCount);
            InnerVoice.Say(thought[0], thought[1], 2.8f);
            InnerVoice.Say("回老周。", "Answer Lao Zhou.", 1.8f);
        }

        void Answers(YYChatHub hub)
        {
            asked = true;
            var answers = XgSim.CardLoanAnswers(CardCount);
            var labels = new[] { T(answers[0]), T(answers[1]) };
            hub.Offer(YYChatHub.LaoZhou, labels);
            hub.Script(YYChatHub.LaoZhou, text =>
            {
                int i = -1;
                for (int k = 0; k < answers.Length; k++) if (text == answers[k][0] || text == answers[k][1]) i = k;
                if (i < 0) { hub.Offer(YYChatHub.LaoZhou, labels); return false; } // typed something else: he still waits
                hub.EndScript();
                StartCoroutine(Decide(i == 0));
                return true;
            });
        }

        IEnumerator Decide(bool lend)
        {
            busy = true;
            var sim = Sim;
            var lab = Lab;
            int cards = CardCount;
            var reply = sim != null && lab != null ? sim.CardLoanDecide(lend, lab.Host) : new List<string[]>();
            if (reply.Count == 0) { busy = false; asked = false; yield break; }
            runtime.MarkDirty();
            var hub = YYChatHub.Instance;
            yield return new WaitForSecondsRealtime(.8f);
            if (hub != null) yield return Type(hub, reply);
            yield return new WaitForSecondsRealtime(1.2f);
            if (lend)
            {
                InnerVoice.Say("下午老周来了，把卡装进原来的纸盒，抱着走了。", "Lao Zhou came in the afternoon, boxed the card up and carried it off.", 3f);
                if (cards > 1) InnerVoice.Say("机箱里空出一个槽。它慢下来了。", "One slot is empty now. It has slowed down.", 2.8f);
                else
                {
                    InnerVoice.Say("它停了。进度条一动不动。", "It stopped. The bar doesn't move.", 2.6f);
                    InnerVoice.Say("没有卡，它就什么也学不了。", "Without a card it can't learn a thing.", 2.8f);
                }
            }
            else
            {
                InnerVoice.Say("卡还插着，风扇照转。", "The card stays in, the fans keep turning.", 2.6f);
                InnerVoice.Say("……他回得比平时慢了半拍。", "…His reply came a beat slower than usual.", 2.8f);
            }
            busy = false;
        }
    }
}
