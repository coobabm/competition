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
            icon = desk.Icon("Era Delivery Folder", "交付文件夹", null, art =>
            {
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
            var client = desk.Window("Era Delivery Window", "交付文件夹 — 本机草稿与回执", new Vector2(-30, 30), new Vector2(820, 550), out window, ForgetWindow);
            var bar = PrologueDesk.Rect("Tabs", client, new Vector2(0, 1), Vector2.one, new Vector2(0, -50), Vector2.zero);
            PrologueDesk.Fill(bar, new Color32(241, 245, 249, 255));
            string[] names = { "A  交付", "B  草稿", "C  检验" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i] = desk.Button(bar, names[i], new Vector2(-244 + 244 * i, 0), new Vector2(228, 32), () => SelectPage(index));
            }
            var note = PrologueDesk.Rect("Read Only", client, new Vector2(0, 1), Vector2.one, new Vector2(22, -77), new Vector2(-22, -52));
            desk.Text(note, "本机文件 · 只读预览 · 滚动查看正文 · 发送须由你确认", 14, PrologueDesk.Muted, TextAlignmentOptions.MidlineLeft).richText = false;
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
            if (On("era.batch.delivered")) text = "已确认并发送。下方保留制作时的初稿原文；发送状态以交付回执为准。\n\n" + text;
            SetBody(text);
            hint.text = owner.WorkStatus ?? "";
            if (!On("era.batch.started"))
            {
                SetAction(0, "接下这批", owner.CanBeginBatch, owner.BeginBatch);
                if (!owner.CanBeginBatch) hint.text = "先完成当前学习，并等亲友的委托消息送达；不会提前接单或发送。";
            }
            else if (!On("era.batch.ready")) SetAction(0, On("era.batch.on") ? "继续接线" : "去接线", true, owner.OpenWiring);
            else if (!On("era.batch.delivered"))
            {
                bool received = Fired("era_delivery_ready");
                SetAction(0, "确认交付", received, owner.ConfirmDelivery);
                hint.text = received ? "确认后才发送 20 份英文页；2 条歧义单列，不冒充已确认内容。" : "初稿已就绪、尚未发送。等消息送达后，你才能确认交付。";
            }
            else if (!On("era.identity.reply"))
            {
                bool received = Fired("era_delivery_confirmed");
                hint.text = received ? "亲友问：这些都是谁做的？由你选择如何回答。" : "已发送。等待亲友收到并回复；这里不会自动替你回答。";
                if (received) { SetAction(0, "就我一个", true, () => owner.ChooseIdentity(1)); SetAction(1, "我和它", true, () => owner.ChooseIdentity(2)); }
            }
            else hint.text = "交付已完成；你的回答：" + (bound.GetFlag("era.identity.reply") == 1 ? "就我一个。" : "我和它。");
        }

        void Draft()
        {
            if (!Fired("era_context_brief"))
            {
                SetBody("询盘草稿\n\n尚未收到这次询盘的完整消息。\n\n收到后，这里会列出客户补充的信息和待确认回复。不会自动发送，也不会替客户补全未确认的交期。");
                hint.text = "等待完整消息送达；现在没有可发送的草稿。";
                return;
            }
            int choice = (int)bound.GetFlag("era.context.choice");
            SetBody("询盘草稿\n\n客户补充：黑色这批周五到。\n回复边界：不承诺明天发货；具体发货日仍需确认。\n\n待确认草稿\n────────────\n" + EraAdvantageStory.Draft + "\n────────────\n\n" + (choice == 1 ? "发送状态：已由你确认发送。" : choice == 2 ? "发送状态：草稿未发送；你选择这次亲自回复。" : "发送状态：尚未发送。"));
            hint.text = choice == 0 ? "这是草稿，不是自动回复。请你决定发出，或这次自己回。" : "这次选择已记录，不会重复发送。";
            if (choice == 0) { SetAction(0, "发送这份草稿", true, () => owner.ChooseContext(1)); SetAction(1, "这次我自己回", true, () => owner.ChooseContext(2)); }
        }

        void Assessment()
        {
            var text = new StringBuilder("重新检验\n\n");
            if (owner.Stage < 4)
            {
                SetBody(text.Append("先继续当前的学习。新的换题检验尚未开放，现有成绩不等于已经通过未来的题目。").ToString());
                hint.text = "尚无可发出的检验请求。";
                return;
            }
            bool challenge = On("era.challenge.requested"), expert = On("era.expert.requested");
            text.Append(challenge ? "换题请求：已由你主动发出。请点击新题验收，以当前训练快照判读独立题，本次原始正确率达到80%才有回执。\n" : "你可以主动请他们换题。旧的最高分不能代替请求之后的一次独立新题验收。\n");
            var challengeReceipt = bound.GetFirst("era.challenge.receipt");
            if (challengeReceipt != null) text.Append("\n换题回执\n").Append(challengeReceipt.text).Append('\n');
            int slot = 0;
            if (!challenge) SetAction(slot++, "请他们换题", true, owner.RequestChallenge);
            else SetAction(slot++, "运行新题验收", !expert || Fired("era_expert_invite"), owner.EvaluateRequested);
            if (owner.Stage >= 5 && On("era.batch.delivered"))
            {
                text.Append("\n周而复始_\n").Append(expert ? "邀请已由你主动发出。" : "你可以主动把已交付的结果发给对方，邀请他检验。对方尚未替你下结论。");
                text.Append("\n必须等检验邀请的消息送达后，使用当前翻译快照完成20道独立新题，原始正确率达到85% 才有回执。不能用旧最高分。\n");
                if (!expert) SetAction(slot++, "把结果发给周而复始_", true, InviteExpert);
                else if (!challenge) SetAction(slot++, "运行新题验收", Fired("era_expert_invite"), owner.EvaluateRequested);
                var expertReceipt = bound.GetFirst("era.expert.receipt");
                if (expertReceipt != null) text.Append("\n专家检验回执\n").Append(expertReceipt.text);
            }
            if (!string.IsNullOrEmpty(owner.LastExamText)) text.Append("\n最近一次新题验收\n").Append(owner.LastExamText);
            if (slot < buttons.Length) SetAction(slot++, "去训练调整", true, owner.OpenTraining);
            SetBody(text.ToString());
            hint.text = expertInviteUnavailable && !expert ? "邀请尚未发出：贴吧或对方的私信入口尚未就绪，请稍后重试。" : expert && !Fired("era_expert_invite") ? "邀请已发送，等待对方消息；消息送达前的评测不算专家检验。" : "只展示实际产生的回执；评测与请求都由你主动操作。";
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
