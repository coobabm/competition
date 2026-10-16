using System;
using System.Text;
using LingGuangV05.Core.Story;
using LingGuangV05.Desktop.Story;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>In-game files only. Preparing a draft never sends it without a player action.</summary>
    public sealed class XgEraDeliveryDesk : MonoBehaviour
    {
        static string T(string zh,string en)=>LingGuangV05.Runtime.GameText.T(zh,en);
        XgEraAdvantageBridge owner;
        StoryState bound;
        RectTransform icon, window;
        TMP_Text body, hint;
        UnityEngine.UI.ScrollRect scroll;
        readonly UnityEngine.UI.Button[] tabs = new UnityEngine.UI.Button[3];
        readonly UnityEngine.UI.Button[] buttons = new UnityEngine.UI.Button[3];
        readonly TMP_Text[] buttonLabels = new TMP_Text[3];
        readonly Action[] actions = new Action[3];
        readonly bool[] shownActions = new bool[3];
        int page;
        bool expertInviteUnavailable;
        float nextRefresh;
        public bool Visible => window != null && window.gameObject.activeInHierarchy;

        public void Bind(XgEraAdvantageBridge value)
        {
            Close();
            owner = value;
            bound = owner != null ? owner.State : null;
            page = 0;
            expertInviteUnavailable = false;
            nextRefresh = 0;
            if (icon != null) icon.gameObject.SetActive(owner != null && bound != null && owner.Stage >= 2);
        }

        void Update()
        {
            var state = owner != null ? owner.State : null;
            if (!ReferenceEquals(bound, state)) { Close(); bound = state; page = 0; expertInviteUnavailable = false; }
            bool available = owner != null && bound != null && owner.Stage >= 2 && !owner.Ended;
            var desk = PrologueDirector.Desk;
            if (available && icon == null && desk != null && desk.icons != null) MakeIcon(desk);
            if (icon != null && icon.gameObject.activeSelf != available) icon.gameObject.SetActive(available);
            if (!available) { if (window != null) Close(); return; }
            if (Visible && Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .25f; Refresh(); }
        }

        void MakeIcon(PrologueDesk desk)
        {
            icon = desk.Icon("Era Delivery Folder", T("交付文件夹","Delivery folder"), null, art =>
            {
                if (PrologueDesk.DrawIcon(art, "delivery_folder")) return;
                PrologueDesk.Fill(PrologueDesk.Centered("Folder Tab", art, new Vector2(-12, 19), new Vector2(25, 9)), new Color32(237, 185, 68, 255), false);
                PrologueDesk.Fill(PrologueDesk.Centered("Folder Back", art, new Vector2(0, -1), new Vector2(54, 39)), new Color32(215, 156, 48, 255), false);
                PrologueDesk.Fill(PrologueDesk.Centered("Folder Front", art, new Vector2(0, -5), new Vector2(54, 32)), new Color32(249, 207, 100, 255), false);
            });
            icon.GetComponent<PrologueClick>().Open = Open;
        }

        public void Open()
        {
            if (owner == null || owner.State == null || owner.Stage < 2 || owner.Ended) return;
            if (!ReferenceEquals(bound, owner.State)) { Close(); bound = owner.State; page = 0; expertInviteUnavailable = false; }
            if (window != null) { window.SetAsLastSibling(); Refresh(); return; }
            var desk = PrologueDirector.Desk;
            if (desk == null || desk.windows == null) return;
            var client = desk.Window("Era Delivery Window", T("交付文件夹 — 本机草稿与回执","Delivery folder: drafts and receipts on this computer"), new Vector2(-30, 30), new Vector2(820, 550), out window, ForgetWindow);
            var bar = PrologueDesk.Rect("Tabs", client, new Vector2(0, 1), Vector2.one, new Vector2(0, -50), Vector2.zero);
            PrologueDesk.Fill(bar, new Color32(241, 245, 249, 255));
            string[] names = { T("A  交付","A  Delivery"), T("B  草稿","B  Draft"), T("C  检验","C  Check") };
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i] = desk.Button(bar, names[i], new Vector2(-244 + 244 * i, 0), new Vector2(228, 32), () => SelectPage(index));
            }
            var note = PrologueDesk.Rect("Read Only", client, new Vector2(0, 1), Vector2.one, new Vector2(22, -77), new Vector2(-22, -52));
            desk.Text(note, T("本机文件 · 只读预览 · 滚动查看正文 · 发送须由你确认","Files on this computer · read-only · scroll to read · nothing is sent without your OK"), 14, PrologueDesk.Muted, TextAlignmentOptions.MidlineLeft).richText = false;
            var panel = PrologueDesk.Rect("Scroll View", client, Vector2.zero, Vector2.one, new Vector2(22, 100), new Vector2(-22, -84));
            var viewport = PrologueDesk.Rect("Viewport", panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(viewport, Color.white);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var content = PrologueDesk.Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, new Vector2(-10, 0));
            content.pivot = new Vector2(.5f, 1);
            body = desk.Text(content, "", 17, PrologueDesk.Ink, TextAlignmentOptions.TopLeft);
            body.richText = false;
            body.lineSpacing = 4;
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll = panel.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            var footer = PrologueDesk.Rect("Actions", client, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 88));
            PrologueDesk.Fill(footer, new Color32(241, 245, 249, 255));
            hint = desk.Text(PrologueDesk.Rect("Status", footer, Vector2.zero, Vector2.one, new Vector2(22, 46), new Vector2(-22, -7)), "", 14, PrologueDesk.Muted, TextAlignmentOptions.TopLeft);
            hint.richText = false;
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                buttons[i] = desk.Button(footer, "", new Vector2(-244 + 244 * i, -17), new Vector2(228, 34), () => Invoke(index));
                buttonLabels[i] = buttons[i].GetComponentInChildren<TMP_Text>();
                buttonLabels[i].richText = false;
            }
            SelectPage(page);
        }

        void SelectPage(int value)
        {
            page = value;
            Refresh();
            if (scroll != null) { scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; }
        }

        bool On(string key) => bound.GetFlag(key) > 0;
        bool Fired(string key) => bound.HasFired(key);

        void Refresh()
        {
            if (body == null || owner == null || bound == null || !ReferenceEquals(bound, owner.State)) return;
            for (int i = 0; i < buttons.Length; i++) { actions[i] = null; shownActions[i] = false; tabs[i].interactable = i != page; }
            if (page == 0) Delivery();
            else if (page == 1) Draft();
            else Assessment();
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i].gameObject.activeSelf != shownActions[i]) buttons[i].gameObject.SetActive(shownActions[i]);
        }

        void Delivery()
        {
            string text = EraAdvantageStory.DeliveryText(bound);
            if (On("era.batch.delivered")) text = T("已确认并发送。下方保留制作时的初稿原文；发送状态以交付回执为准。","Confirmed and sent. The drafts as they were made are kept below; the delivery receipt tells what was actually sent.") + "\n\n" + text;
            SetBody(text);
            hint.text = owner.WorkStatus ?? "";
            if (!On("era.batch.started"))
            {
                SetAction(0, T("接下这批","Take this batch"), owner.CanBeginBatch, owner.BeginBatch);
                if (!owner.CanBeginBatch) hint.text = T("先完成当前学习，并等亲友的委托消息送达；不会提前接单或发送。","Finish what you are learning and wait for the commission message first. Nothing is taken on or sent early.");
            }
            else if (!On("era.batch.ready")) SetAction(0, On("era.batch.on") ? T("继续接线","Keep wiring") : T("去接线","Go wire it"), true, owner.OpenWiring);
            else if (!On("era.batch.delivered"))
            {
                bool received = Fired("era_delivery_ready");
                SetAction(0, T("确认交付","Confirm delivery"), received, owner.ConfirmDelivery);
                hint.text = received ? T("确认后才发送 20 份英文页；2 条歧义单列，不冒充已确认内容。","The 20 English pages go out only after you confirm; the 2 doubtful lines are listed apart, not passed off as checked.") : T("初稿已就绪、尚未发送。等消息送达后，你才能确认交付。","The drafts are ready and unsent. You can confirm delivery once the message has arrived.");
            }
            else if (!On("era.identity.reply"))
            {
                bool received = Fired("era_delivery_confirmed");
                hint.text = received ? T("亲友问：这些都是谁做的？由你选择如何回答。","Your cousin asks who made these. How to answer is up to you.") : T("已发送。等待亲友收到并回复；这里不会自动替你回答。","Sent. Wait for your cousin to receive it and reply; nothing here answers for you.");
                if (received) { SetAction(0, T("就我一个","Just me"), true, () => owner.ChooseIdentity(1)); SetAction(1, T("自己慢慢做的","Did it myself, slowly"), true, () => owner.ChooseIdentity(2)); }
            }
            else hint.text = T("交付已完成；你的回答：","Delivery done. Your answer: ") + (bound.GetFlag("era.identity.reply") == 1 ? T("就我一个。","Just me.") : T("我自己慢慢做的。","I did it myself, slowly."));
        }

        void Draft()
        {
            if (!Fired("era_context_brief"))
            {
                SetBody(T("询盘草稿\n\n尚未收到这次询盘的完整消息。\n\n收到后，这里会列出客户补充的信息和待确认回复。不会自动发送，也不会替客户补全未确认的交期。","Enquiry draft\n\nThe full enquiry has not arrived yet.\n\nWhen it does, the customer's extra details and the reply waiting for your OK are listed here. Nothing is sent automatically and no unconfirmed delivery date is filled in."));
                hint.text = T("等待完整消息送达；现在没有可发送的草稿。","Waiting for the full message; there is no draft to send yet.");
                return;
            }
            int choice = (int)bound.GetFlag("era.context.choice");
            SetBody(T("询盘草稿\n\n客户补充：黑色这批周五到。\n回复边界：不承诺明天发货；具体发货日仍需确认。\n\n待确认草稿\n────────────\n","Enquiry draft\n\nCustomer note: the black batch arrives on Friday.\nLimit of the reply: no promise to ship tomorrow; the shipping day still has to be confirmed.\n\nDraft awaiting your OK\n────────────\n") + EraAdvantageStory.Draft + "\n────────────\n\n" + (choice == 1 ? T("发送状态：已由你确认发送。","Status: sent with your OK.") : choice == 2 ? T("发送状态：草稿未发送；你选择这次亲自回复。","Status: not sent; you chose to answer yourself this time.") : T("发送状态：尚未发送。","Status: not sent yet.")));
            hint.text = choice == 0 ? T("这是草稿，不是自动回复。请你决定发出，或这次自己回。","This is a draft, not an automatic reply. Decide whether to send it, or answer yourself.") : T("这次选择已记录，不会重复发送。","Your choice is recorded and nothing is sent twice.");
            if (choice == 0) { SetAction(0, T("发送这份草稿","Send this draft"), true, () => owner.ChooseContext(1)); SetAction(1, T("这次我自己回","I will answer myself"), true, () => owner.ChooseContext(2)); }
        }

        void Assessment()
        {
            var text = new StringBuilder(T("再考一次\n\n","Another check\n\n"));
            if (owner.Stage < 4)
            {
                SetBody(text.Append(T("先继续当前的学习。换题再考还没开放，现在的分数不代表以后换了题也考得过。","Keep learning for now. Re-checks on new questions are not open yet, and your current score does not mean you would pass on different questions.")).ToString());
                hint.text = T("现在还没有可以发出的请求。","There is no request you can send yet.");
                return;
            }
            bool challenge = On("era.challenge.requested"), expert = On("era.expert.requested");
            text.Append(challenge ? T("换题请求：你已经发出了。点「运行新题验收」，用它现在学到的东西考 20 道没见过的新题，答对 16 道以上才算过。\n","Re-check request: you have sent it. Press Run fresh-question check: it is tested on 20 questions it has never seen, using what it knows right now. 16 right or more passes.\n") : T("你可以请他们换一批题再考。以前的最高分不能顶替，必须在请求之后考一次新题。\n","You can ask them to set a new batch of questions. An old best score does not count: the check must be taken after the request, on fresh questions.\n"));
            var challengeReceipt = bound.GetFirst("era.challenge.receipt");
            if (challengeReceipt != null) text.Append(T("\n换题回执\n","\nRe-check receipt\n")).Append(challengeReceipt.text).Append('\n');
            int slot = 0;
            if (!challenge) SetAction(slot++, T("请他们换题","Ask for new questions"), true, owner.RequestChallenge);
            else SetAction(slot++, T("运行新题验收","Run fresh-question check"), !expert || Fired("era_expert_invite"), owner.EvaluateRequested);
            if (owner.Stage >= 5 && On("era.batch.delivered"))
            {
                text.Append(T("\n周而复始_（老周的师弟，学翻译的）\n","\n周而复始_ (Lao Zhou's junior, a translation student)\n")).Append(expert ? T("邀请你已经发出了。","You have sent the invitation.") : T("你可以把已交付的结果发给对方，请他检验。他还没替你下任何结论。","You can send him what you delivered and ask him to check it. He has not drawn any conclusion for you."));
                text.Append(T("\n要等他回了消息，再用它现在的翻译水平考 20 道没见过的新题，答对 17 道以上才有回执。旧的最高分不算。\n","\nWait until he has replied, then it takes 20 fresh translation questions at its present level; 17 right or more earns the receipt. An old best score does not count.\n"));
                if (!expert) SetAction(slot++, T("把结果发给周而复始_","Send him the results"), true, InviteExpert);
                else if (!challenge) SetAction(slot++, T("运行新题验收","Run fresh-question check"), Fired("era_expert_invite"), owner.EvaluateRequested);
                var expertReceipt = bound.GetFirst("era.expert.receipt");
                if (expertReceipt != null) text.Append(T("\n他的检验回执\n","\nHis check receipt\n")).Append(expertReceipt.text);
            }
            if (!string.IsNullOrEmpty(owner.LastExamText)) text.Append(T("\n最近一次新题验收\n","\nLatest fresh-question check\n")).Append(owner.LastExamText);
            if (slot < buttons.Length) SetAction(slot++, T("去训练调整","Back to training"), true, owner.OpenTraining);
            SetBody(text.ToString());
            hint.text = expertInviteUnavailable && !expert ? T("邀请还没发出：贴吧里对方的私信暂时用不了，稍后再试。","The invitation has not gone out: his private messages are not available yet. Try again later.") : expert && !Fired("era_expert_invite") ? T("邀请已发送，等他回消息；他回信之前考的不算他的检验。","Invitation sent; wait for his reply. Checks taken before he replies do not count as his.") : T("这里只放真正产生过的回执；考不考、请不请，都由你决定。","Only receipts that really exist are shown here. Whether to ask or to take the check is up to you.");
        }

        void InviteExpert()
        {
            owner.RequestExpert();
            expertInviteUnavailable = !On("era.expert.requested");
        }

        void SetBody(string value) { if (body.text != value) body.text = value; }

        void SetAction(int index, string label, bool enabled, Action action)
        {
            shownActions[index] = true;
            buttons[index].interactable = enabled;
            buttonLabels[index].text = label;
            buttonLabels[index].color = enabled ? PrologueDesk.Ink : PrologueDesk.Muted;
            actions[index] = enabled ? action : null;
        }

        void Invoke(int index)
        {
            if (owner == null || owner.Stage < 2 || !ReferenceEquals(bound, owner.State)) return;
            actions[index]?.Invoke();
            Refresh();
        }

        void ForgetWindow()
        {
            window = null; body = null; hint = null; scroll = null;
            Array.Clear(actions, 0, actions.Length);
            Array.Clear(buttons, 0, buttons.Length);
            Array.Clear(buttonLabels, 0, buttonLabels.Length);
            Array.Clear(tabs, 0, tabs.Length);
        }

        void Close()
        {
            if (window != null) { window.gameObject.SetActive(false); Destroy(window.gameObject); }
            ForgetWindow();
        }

        void OnDestroy()
        {
            Close();
            if (icon != null) { icon.gameObject.SetActive(false); Destroy(icon.gameObject); }
        }
    }
}
