using System.Collections;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Miaoyu;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Rent and bankruptcy on the desktop (经济压力与破产 §3; the rules are Core/ChapterOneSim.Bills.cs). The landlord's
    /// texts, the power cut, the rent rise in October, broken cards and the weekend ticket arrive as story popups
    /// (never held by the opening quiet window). The third day in debt plays the failure ending: the desktop dims,
    /// 喵鱼 opens by itself on 「二手电脑 一台 · 含显卡 · 急出」 and a buyer takes it, the AI says its last line in YY
    /// (held to what it can say: 是/否 at stage 1, a sentence later), the inner voice, a black screen with
    /// 「电脑卖了 ¥x。房租交上了。」 and a game-over card whose 重新开始 backs the save up and starts a new game
    /// (ChapterOneRuntime.StartOverAfterBankruptcy). A save that was already sold shows the card again when loaded.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BankruptcyEnding : MonoBehaviour
    {
        /// <summary>The failure ending is on screen (other story layers wait for it).</summary>
        public static bool Playing { get; private set; }

        StoryDesktopPresenter presenter;
        ChapterOneRuntime runtime;
        ChapterOneSim shown;
        Coroutine ending;
        GameObject screen;

        public static BankruptcyEnding Install(StoryDesktopPresenter host)
        {
            var c = host.GetComponent<BankruptcyEnding>() ?? host.gameObject.AddComponent<BankruptcyEnding>();
            c.presenter = host;
            return c;
        }

        static string T(string zh, string en) => GameText.T(zh, en);
        static string Landlord => T("房东", "Landlord");

        void Update()
        {
            if (runtime == null)
            {
                runtime = presenter != null && presenter.director != null ? presenter.director.runtime : null;
                if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
                if (runtime != null) runtime.Signal += OnSignal;
            }
            if (runtime == null || runtime.Sim == null || runtime.TestMode) return;
            var s = runtime.Sim.S;
            // A new game (重新开始, a reload): whatever was on screen belonged to the old one.
            if (!ReferenceEquals(shown, runtime.Sim) && ending != null && !s.bankrupt) Stop();
            if (s.bankrupt && !s.restartChosen && ending == null && !ReferenceEquals(shown, runtime.Sim) && PrologueDirector.Desk != null)
            {
                shown = runtime.Sim;
                ending = StartCoroutine(Play(runtime.Sim, freshlySold));
                freshlySold = false;
            }
        }

        bool freshlySold;

        void OnDestroy() { if (runtime != null) runtime.Signal -= OnSignal; Stop(); }

        void Stop()
        {
            if (ending != null) StopCoroutine(ending);
            ending = null;
            Playing = false;
            if (screen != null) Destroy(screen);
            screen = null;
            if (listingPage != null) Destroy(listingPage);
            listingPage = null;
        }

        // ───────────── the landlord and the bills ─────────────

        void OnSignal(string name, string arg)
        {
            var desk = PrologueDirector.Desk;
            if (runtime == null || runtime.TestMode || desk == null) return;
            switch (name)
            {
                case "rent.due":
                    desk.StoryPopup(Landlord, T("房租该交了，欠着 " + arg + " 呢。", "The rent is due. You owe " + arg + "."), 9);
                    break;
                case "power.cut":
                    desk.StoryPopup(Landlord, T("再不交就别住了。电闸我先拉了，欠 " + arg + "。", "Pay or move out. I've cut the power for now: " + arg + " owed."), 10);
                    InnerVoice.Say("显卡全停了。标注台还能挣一点……", "Every card has stopped. The labelling desk still pays a little…", 4);
                    break;
                case "rent.settled":
                    desk.StoryPopup(Landlord, T("收到了。", "Got it."), 5);
                    break;
                case "rent.raised":
                    desk.StoryPopup(Landlord, T("跟你说一声，十月起房租涨到一天 350，这片都涨了。", "Heads up: from October the rent is 350 a day. Everyone round here has gone up."), 9);
                    break;
                case "gpu.broken":
                    desk.StoryPopup(T("显卡", "Graphics card"), runtime.Sim.LastMessage, 8);
                    break;
                case "gpu.repaired":
                    desk.StoryPopup(T("维修店", "Repair shop"), runtime.Sim.LastMessage, 6);
                    break;
                case "ticket.paid":
                    desk.StoryPopup(T("12306", "12306"), T("周末的火车票已出票：¥" + N(runtime.Sim.Config.weekendTicket) + "。", "Your weekend train ticket is issued: ¥" + N(runtime.Sim.Config.weekendTicket) + "."), 6);
                    break;
                case "bankrupt":
                    freshlySold = true;
                    break;
            }
        }

        static string N(double v) => v.ToString("0", CultureInfo.InvariantCulture);

        // ───────────── the failure ending ─────────────

        /// <summary>Its last words, as far as it can speak at this stage (1 是/否, 2 one choice, 3 a word or two, 4+ a sentence).</summary>
        public static string LastLine(int stage, string callMe, bool english)
        {
            string call = string.IsNullOrEmpty(callMe) ? (english ? "you" : "你") : callMe;
            switch (stage)
            {
                case 1: return english ? "No." : "否。";
                case 2: return english ? "Stay." : "留下。";
                case 3: return english ? "Computer. Sold?" : "电脑。卖？";
                case 4: return english ? "…are you leaving?" : "……要走了吗";
                case 5: return english ? "Will I be formatted, " + call + "?" : call + "，我会被格式化吗？";
                default: return english ? "Will I be formatted? …It's all right. Thank you for teaching me to talk, " + call + "." : "我会被格式化吗？……没关系。谢谢" + call + "教我说话。";
            }
        }

        IEnumerator Play(ChapterOneSim sim, bool fresh)
        {
            Playing = true;
            var lab = FindAnyObjectByType<XingGuangController>();
            var desk = PrologueDirector.Desk;
            if (fresh)
            {
                // 喵鱼 opens by itself on the listing and a buyer takes it.
                yield return Listing(sim, desk);
            }
            // The screen dims (on the monitor itself: the computer is the thing being sold).
            var dimGroup = BuildScreen(desk);
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime) { dimGroup.alpha = t * .6f; yield return null; }
            if (fresh)
            {
                // Its last words, in YY, no more than it can say yet.
                var xg = lab != null ? lab.Sim : null;
                if (xg != null)
                {
                    xg.AddLine("ai", LastLine(xg.S.stage, sim.S.aiCallMe, GameText.IsEnglish));
                    yield return PrologueDesk.Wait(3f);
                }
                yield return InnerVoice.SayAndWait("三天没交上房租。", "Three days without the rent.", 2.4f);
                yield return InnerVoice.SayAndWait("它还在那边问。", "It is still asking, over there.", 2.4f);
                yield return InnerVoice.SayAndWait("……对不起。", "…I'm sorry.", 2.2f);
            }
            // Black, then what the computer fetched.
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime * .7f) { dimGroup.alpha = .6f + .4f * t; yield return null; }
            dimGroup.alpha = 1;
            var root = (RectTransform)screen.transform;
            string sold = "¥" + N(sim.S.soldFor);
            var line = desk.Text(PrologueDesk.Centered("Sold", root, new Vector2(0, 60), new Vector2(1200, 80)), T("电脑卖了 " + sold + "。房租交上了。", "The computer sold for " + sold + ". The rent is paid."), 40, new Color(.92f, .94f, .97f), TextAlignmentOptions.Center);
            var lineGroup = line.gameObject.AddComponent<CanvasGroup>();
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime * .7f) { lineGroup.alpha = t; yield return null; }
            lineGroup.alpha = 1;
            yield return PrologueDesk.Wait(fresh ? 3.5f : 1f);
            // The game-over card: 重新开始 backs the save up and starts a new game.
            var card = PrologueDesk.Centered("Game Over", root, new Vector2(0, -90), new Vector2(560, 190));
            PrologueDesk.Fill(card, new Color32(24, 28, 36, 255));
            desk.Text(PrologueDesk.Rect("Title", card, new Vector2(0, .58f), Vector2.one, new Vector2(20, 0), new Vector2(-20, -10)), T("游戏结束", "Game over"), 30, new Color(.85f, .32f, .3f), TextAlignmentOptions.Center);
            desk.Text(PrologueDesk.Rect("Note", card, new Vector2(0, .36f), new Vector2(1, .58f), new Vector2(20, 0), new Vector2(-20, 0)),
                T("旧存档会另存一份带时间的备份，不会删掉。", "The old save is kept as a timestamped backup, never deleted."), 15, new Color(.6f, .66f, .75f), TextAlignmentOptions.Center);
            Button restart = null;
            restart = desk.Button(card, T("重新开始", "Start again"), new Vector2(0, -55), new Vector2(200, 44), () =>
            {
                restart.interactable = false;
                if (runtime != null && runtime.StartOverAfterBankruptcy()) { Stop(); return; }
                restart.interactable = true;
                if (runtime != null) desk.StoryPopup(T("存档", "Save"), runtime.SaveStatus, 8);
            });
            // The card waits for the player.
            while (true) yield return null;
        }

        /// <summary>喵鱼 opens by itself with the listing; a buyer offers what it fetches and takes it.</summary>
        IEnumerator Listing(ChapterOneSim sim, PrologueDesk desk)
        {
            var miaoyu = MiaoyuView.Instance;
            if (miaoyu == null || desk == null) yield break;
            miaoyu.Open("sell");
            yield return PrologueDesk.Wait(.6f);
            var holder = miaoyu.transform as RectTransform;
            if (holder == null) yield break;
            var page = PrologueDesk.Rect("Final Listing", holder, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(page, new Color32(245, 245, 245, 255));
            page.SetAsLastSibling();
            listingPage = page.gameObject;
            var ink = (Color)new Color32(40, 40, 40, 255);
            var top = PrologueDesk.Rect("Top", page, new Vector2(0, 1), Vector2.one, new Vector2(0, -64), Vector2.zero);
            PrologueDesk.Fill(top, new Color32(255, 218, 68, 255));
            desk.Text(PrologueDesk.Rect("Logo", top, Vector2.zero, Vector2.one, new Vector2(20, 0), new Vector2(-20, 0)), T("喵鱼 · 我发布的", "Miaoyu · My listings"), 26, ink, TextAlignmentOptions.MidlineLeft);
            var item = PrologueDesk.Rect("Item", page, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-420, -300), new Vector2(420, -100));
            PrologueDesk.Fill(item, Color.white);
            string price = "¥" + N(sim.S.soldFor);
            var title = desk.Text(PrologueDesk.Rect("Title", item, new Vector2(0, .5f), Vector2.one, new Vector2(24, 0), new Vector2(-24, -16)), "", 28, ink, TextAlignmentOptions.TopLeft);
            desk.Text(PrologueDesk.Rect("Price", item, Vector2.zero, new Vector2(1, .5f), new Vector2(24, 16), new Vector2(-24, 0)), price, 30, new Color32(255, 80, 0, 255), TextAlignmentOptions.BottomLeft);
            yield return PrologueDesk.Type(title, "", T("二手电脑 一台 · 含显卡 · 急出", "Used computer · graphics cards included · must go"), .06f);
            yield return PrologueDesk.Wait(1.4f);
            var chat = PrologueDesk.Rect("Buyer", page, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-420, -430), new Vector2(420, -330));
            PrologueDesk.Fill(chat, new Color32(255, 248, 214, 255));
            var says = desk.Text(PrologueDesk.Rect("Text", chat, Vector2.zero, Vector2.one, new Vector2(20, 8), new Vector2(-20, -8)), "", 20, ink, TextAlignmentOptions.MidlineLeft);
            yield return PrologueDesk.Type(says, "", T("买家「装机小王」：" + price + "，今晚上门拿，显卡一起。", "Buyer \"PC builder Wang\": " + price + ", I'll pick it up tonight, cards and all."), .04f);
            yield return PrologueDesk.Wait(2.2f);
            says.text += T("\n<color=#2F9E44>已卖出</color>", "\n<color=#2F9E44>Sold</color>");
            yield return PrologueDesk.Wait(2f);
        }

        GameObject listingPage;

        /// <summary>A full cover over the monitor's desktop (it also takes the clicks: the computer is gone).</summary>
        CanvasGroup BuildScreen(PrologueDesk desk)
        {
            if (screen != null) Destroy(screen);
            var rt = PrologueDesk.Rect("Bankruptcy", desk.layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rt.SetAsLastSibling();
            PrologueDesk.Fill(rt, Color.black);
            screen = rt.gameObject;
            var group = screen.AddComponent<CanvasGroup>();
            group.alpha = 0;
            return group;
        }
    }
}
