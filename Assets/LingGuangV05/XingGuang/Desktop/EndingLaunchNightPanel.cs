using System;
using System.Globalization;
using System.Text;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>One current request, bounded clock and no answer controls. All outcomes belong to core.</summary>
    public sealed class EndingLaunchNightPanel : MonoBehaviour
    {
        public readonly EndingUiSession Session = new EndingUiSession();
        XgUi ui;
        RectTransform root;
        TMP_Text title, queue, question, answer, reason, metrics, status, answerNotice,questionNotice;
        XgBtn start, throttle, pause, pull, continueButton, reveal;
        bool built, ticking, revealed;
        float elapsed, typeElapsed;
        string shownRequest = "", shownAnswer = "";
        object boundOwner, boundState;

        public void Build(RectTransform area, TMP_FontAsset font)
        {
            if (built) return; built = true;
            root = area; ui = new XgUi(font, null, true); Panel(root, XgDark.Page);
            title = Text(Strip("Title", root, 14, 32), 22);
            status = Text(Strip("Status", root, 52, 42), 13);
            queue = Text(Rect("Queue", root, new Vector2(0, 0), new Vector2(.23f, 1), new Vector2(16, 100), new Vector2(-10, -102)), 13);
            var center = Rect("CurrentRequest", root, new Vector2(.24f, 0), new Vector2(.76f, 1), new Vector2(4, 100), new Vector2(-4, -102));
            Panel(center, XgDark.Card);
            question = Text(Rect("Question", center, new Vector2(0, .72f), Vector2.one, new Vector2(12, 4), new Vector2(-12, -12)), 16);
            answer = Text(Rect("Answer", center, new Vector2(0, .26f), new Vector2(1, .70f), new Vector2(12, 4), new Vector2(-12, -4)), 18);
            question.margin=answer.margin=new Vector4(0,0,0,24);
            questionNotice=LingGuangV05.Runtime.AiContentNotice.CreateFooter(question.rectTransform,font,XgDark.Muted);
            answerNotice=LingGuangV05.Runtime.AiContentNotice.CreateFooter(answer.rectTransform,font,XgDark.Muted);
            reason = Text(Rect("Reason", center, Vector2.zero, new Vector2(1, .25f), new Vector2(12, 10), new Vector2(-12, -4)), 12);
            metrics = Text(Rect("Metrics", root, new Vector2(.78f, 0), Vector2.one, new Vector2(4, 100), new Vector2(-16, -102)), 15);
            start = Button("Start", .02f, .22f, 16, () => Command(EndingUiAction.StartNight));
            throttle = Button("Throttle", .24f, .42f, 16, () => Command(EndingUiAction.ThrottleNight, !Session.Snapshot.night.throttled));
            pause = Button("Pause", .44f, .60f, 16, () => Command(EndingUiAction.PauseNight, !(Session.Snapshot.night.paused || !ticking)));
            pull = Button("PullPower", .62f, .79f, 16, () => Command(EndingUiAction.PullPower));
            continueButton = Button("Continue", .81f, .98f, 16, () => Command(EndingUiAction.Continue));
            reveal = Button("Reveal", .35f, .65f, 60, () => { revealed = true; answer.maxVisibleCharacters = int.MaxValue; RefreshControls(); });
            Session.Changed += Refresh;
            Session.SetVisible(isActiveAndEnabled && root.gameObject.activeInHierarchy); Refresh();
        }
        public void Bind(EndingUiSnapshot snapshot, EndingUiCallbacks callbacks)
        {
            if (snapshot == null || !ReferenceEquals(boundOwner, snapshot.owner) || !ReferenceEquals(boundState, snapshot.stateOwner))
            {
                elapsed = typeElapsed = 0; shownRequest = shownAnswer = ""; revealed = false; ticking = false;
                boundOwner = snapshot == null ? null : snapshot.owner; boundState = snapshot == null ? null : snapshot.stateOwner;
            }
            Session.Bind(snapshot, callbacks);
        }
        void OnEnable() { elapsed = 0; if (built) Session.SetVisible(true); }
        void OnDisable() { ticking = false; elapsed = 0; Session.SetVisible(false); }
        void OnDestroy() { Session.Changed -= Refresh; }
        string T(string zh, string en) => Session.Snapshot != null && Session.Snapshot.english ? en : zh;
        string Safe(EndingUiText text)
        {
            if (text == null || Session.Callbacks == null) return "";
            var filter = text.reviewed ? Session.Callbacks.FilterReviewedText : Session.Callbacks.FilterText;
            if (filter == null) return "";
            try { return filter(text.Get(Session.Snapshot.english), Session.Snapshot.english) ?? ""; }
            catch { return ""; }
        }
        TMP_Text Text(RectTransform rect, int size)
        {
            size=Mathf.RoundToInt(size*1.35f);
            var text = ui.Text(rect, "", size, XgDark.Ink, TextAlignmentOptions.TopLeft);
            text.richText = false; text.enableAutoSizing = true; text.fontSizeMin = Math.Min(16, size); text.fontSizeMax = size;
            return text;
        }
        XgBtn Button(string name, float min, float max, float y, Action action)
        {
            var b = ui.Button(root, "", () => { if (Session.Current && !Session.Busy) action(); }, 18);
            b.rt.name = name; b.label.richText = false;
            var feedback = b.button as XgButtonPressFeedback;
            if (feedback != null) { feedback.Label = null; feedback.PressTarget = null; }
            b.rt.anchorMin = new Vector2(min, 0); b.rt.anchorMax = new Vector2(max, 0);
            b.rt.offsetMin = new Vector2(0, y); b.rt.offsetMax = new Vector2(0, y + 34); return b;
        }
        void Command(EndingUiAction action, bool enabled = false)
        {
            if (!Session.CanAct) return;
            var n = Session.Snapshot.night;
            if (action == EndingUiAction.StartNight && !n.canStart) return;
            if (action == EndingUiAction.Continue && !n.canContinue) return;
            if (action != EndingUiAction.StartNight && action != EndingUiAction.Continue && (!n.running || n.stopped)) return;
            // Explicit start/resume authorizes the local visible clock. No automatic resume after hide/failure.
            if (action == EndingUiAction.StartNight || (action == EndingUiAction.PauseNight && !enabled)) ticking = true;
            if (action == EndingUiAction.PauseNight && enabled) ticking = false;
            Session.Send(action, enabled: enabled);
        }
        void Update()
        {
            if (!built || !Session.Visible || !Session.Current) return;
            var s = Session.Snapshot; var n = s.night;
            if (!s.reduceFx && !revealed && answer.text.Length > 0 && answer.maxVisibleCharacters < answer.text.Length)
            {
                typeElapsed += Mathf.Min(Time.unscaledDeltaTime, .1f);
                answer.maxVisibleCharacters = (int)(typeElapsed * 36);
                if (answer.maxVisibleCharacters >= answer.text.Length) RefreshControls();
            }
            if (!ticking || !Session.CanAct || Session.Failed || !n.running || n.paused || n.stopped) return;
            elapsed += Mathf.Min(Time.unscaledDeltaTime, .1f);
            if (elapsed < .5f) return;
            double seconds = Math.Min(1, elapsed); elapsed = 0;
            Session.Send(EndingUiAction.AdvanceNight, seconds: seconds);
        }
        public void Refresh()
        {
            if (!built || !Session.Visible || Session.Snapshot == null) return;
            var s = Session.Snapshot; var n = s.night;
            if (Session.Failed) { ticking = false; elapsed = 0; }
            title.text = T("上线之夜", "Launch night");
            status.text = Session.Failed ? FailureText() :
                Session.Busy ? T("正在保存……", "Saving…") : Safe(n.stopLine);
            var b = new StringBuilder(T("等待中的请求", "Queued requests"));
            int count = Math.Min(8, n.queue.Count);
            for (int i = 0; i < count; i++) b.Append("\n\n").Append(i + 1).Append(". ").Append(Safe(n.queue[i]));
            if (n.queue.Count > count) b.Append("\n\n+").Append(n.queue.Count - count);
            queue.text = b.ToString(); question.text = Safe(n.question);
            questionNotice.text=answerNotice.text=LingGuangV05.Runtime.AiContentNotice.Footer(s.english);
            questionNotice.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(question.text));
            string nextAnswer = Safe(n.answer);
            if (shownRequest != n.requestId || shownAnswer != nextAnswer)
            {
                shownRequest = n.requestId; shownAnswer = nextAnswer; typeElapsed = 0; revealed = false;
                answer.text = nextAnswer; answer.maxVisibleCharacters = s.reduceFx ? int.MaxValue : 0;
            }
            if (s.reduceFx) { answer.maxVisibleCharacters = int.MaxValue; revealed = true; }
            string reasonText = Safe(n.reason);
            // A4: the grey line is already the sentence 「因为你在 X 月 X 日……」; only corrupted answers get the red marker.
            reason.text = string.IsNullOrEmpty(reasonText) ? "" : (n.corrupted ? T("被带偏：", "Misled: ") : "") + reasonText;
            answerNotice.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(answer.text));
            reason.color = n.corrupted ? XgDark.Bad : XgDark.Muted;
            metrics.text = T("电表", "Power meter") + "\n" + n.powerWh.ToString("0.00", CultureInfo.InvariantCulture) + " Wh\n\n" +
                T("显卡温度", "GPU temperature") + "\n" + n.temperatureC.ToString("0.0", CultureInfo.InvariantCulture) + " °C\n\n" +
                T("已答", "Answered") + "\n" + n.answered + " / " + n.total;
            RefreshControls();
        }
        void RefreshControls()
        {
            if (!built || Session.Snapshot == null) return;
            var s = Session.Snapshot; var n = s.night; bool act = Session.CanAct;
            start.Set(T("开始这一夜", "Start the night"), act && n.canStart);
            throttle.Set(n.throttled ? T("解除限流", "Remove limit") : T("限流", "Throttle"), act && n.running && !n.stopped);
            bool resume = n.paused || !ticking;
            pause.Set(resume ? T("继续这一夜", "Resume the night") : T("暂停", "Pause"), act && n.running && !n.stopped);
            pull.Set(T("拔电源", "Pull the plug"), act && n.running && !n.stopped);
            continueButton.Set(T("继续", "Continue"), act && n.canContinue);
            reveal.Set(T("显示完整回答", "Show full answer"), !Session.Busy && !s.reduceFx && !revealed && answer.maxVisibleCharacters < answer.text.Length);
        }
        string FailureText() => string.IsNullOrEmpty(Safe(Session.Failure)) ? T("操作未完成，演出已暂停。请重试继续。", "The action did not complete. The night is paused. Retry to continue.") : Safe(Session.Failure);
    }
}
