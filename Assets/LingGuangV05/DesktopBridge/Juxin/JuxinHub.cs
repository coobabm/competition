using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.Juxin
{
    /// <summary>
    /// 巨信 service on the desktop: ticks the lab's 巨信 rules (XgSim.Juxin.cs), shows or hides the app with the stage,
    /// says the one-off introduction (阿杰 in the YY group, then the inner voice), and runs 让 AI 代我回 through the
    /// local model with the same persona prompt, sampling and stage limits as the 对话 page. The rules fall back to
    /// offline lines when the model is not running. Attaches itself next to the 灵光 controller at scene load, so neither
    /// the scene nor the controller is edited. Presentation and transport only; all state is in the lab save.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JuxinHub : MonoBehaviour
    {
        const float TickEvery = .5f;

        public static JuxinHub Instance { get; private set; }
        public ChapterOneRuntime runtime;
        XingGuangController controller;
        JuxinView view;
        XgSim bound;
        float sinceTick;
        bool jobRunning;
        float nextInstall;

        public XgSim Sim => bound;
        public XingGuangController Controller => controller;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Attach()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.TestMode || runtime.GetComponent<JuxinHub>() != null) return;
            runtime.gameObject.AddComponent<JuxinHub>().runtime = runtime;
        }

        void Awake() { Instance = this; }
        void OnDestroy()
        {
            if (bound != null) bound.JuxinReceived -= OnReceived;
            if (Instance == this) Instance = null;
        }

        static string T(string zh, string en) => GameText.T(zh, en);

        void Update()
        {
            if (controller == null) controller = GetComponent<XingGuangController>() ?? FindAnyObjectByType<XingGuangController>();
            if (view == null && Time.unscaledTime >= nextInstall) { nextInstall = Time.unscaledTime + 1; view = JuxinView.Install(this); }
            if (controller == null || controller.Sim == null || runtime == null || runtime.Sim == null)
            {
                if (view != null) view.SetUnlocked(false);
                return;
            }
            if (!ReferenceEquals(bound, controller.Sim))
            {
                if (bound != null) bound.JuxinReceived -= OnReceived;
                bound = controller.Sim;
                bound.JuxinReceived += OnReceived;
                jobRunning = false; sinceTick = 0;
            }
            bool unlocked = !runtime.Sim.InPrologue && bound.JuxinUnlocked;
            if (view != null) view.SetUnlocked(unlocked);
            if (!unlocked) return;

            sinceTick += Time.unscaledDeltaTime;
            if (sinceTick >= TickEvery)
            {
                bool wasOpen = bound.JuxinOpen;
                bound.JuxinTick(sinceTick, controller.Host);
                sinceTick = 0;
                if (!wasOpen && bound.JuxinOpen) runtime.MarkDirty();
                Introduce();
                if (!jobRunning) RunAutoReply();
            }
            if (view != null) view.UpdateBadges(bound.JuxinUnread > 0);
        }

        void OnReceived(XgJxThread thread, XgJxMessage message)
        {
            if (thread == null || message == null || message.mine || bound == null || controller == null
                || !ReferenceEquals(bound, controller.Sim)) return;
            string sender = XgSim.JuxinName(thread.id, GameText.IsEnglish);
            string who = T(message.whoZh, message.whoEn);
            string title = T(message.titleZh, message.titleEn);
            string body = (who.Length > 0 ? who + ": " : "") + (title.Length > 0 ? title + "\n" : "") + T(message.zh, message.en);
            bool visible = bound.JuxinShowing == thread.id && view != null && view.isActiveAndEnabled && DesktopNotifications.IsWindowVisible(view);
            DesktopNotifications.Notify(runtime, Lang.T("巨信"), sender, body, visible,
                () => { if (view != null) view.Open(thread.id); });
        }

        /// <summary>
        /// The first time 巨信 is there: 阿杰 mentions it in the YY group (where the 网吧 friends already are), and the
        /// protagonist notices the new green icon. Once per save.
        /// </summary>
        void Introduce()
        {
            if (!bound.JuxinOpen) return;
            var yy = YYChatHub.Instance;
            if (yy != null && yy.S != null && yy.Conversation(XgMarketRelay.Group) != null && bound.JuxinTakeOnce("intro.yy"))
                yy.Receive(XgMarketRelay.Group, "[" + Lang.T("阿杰") + "] " + Lang.T("都九月了还只用 YY？下个巨信，现在甲方都在上面。我把你拉进来了。"));
            if (bound.JuxinTakeOnce("intro.voice"))
                InnerVoice.Say("桌面上多了个绿色的图标……巨信？", "A new green icon on the desktop... Juxin?", 3.2f);
            if (bound.JuxinCanAutoReply && bound.JuxinTakeOnce("intro.auto"))
                InnerVoice.Say("它现在说话挺像个人了。巨信的消息……要不让它替我回？", "It talks almost like a person now. Juxin messages... should I let it answer for me?", 3.6f);
        }

        /// <summary>
        /// One waiting message at a time goes to the model with the persona prompt the rules built. A gaffe needs no
        /// model: the rules pick the line. The reply always comes back through <see cref="XgSim.CompleteAutoReply"/>.
        /// </summary>
        void RunAutoReply()
        {
            var job = bound.TakeAutoReply();
            if (job == null) return;
            var sim = bound;
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            if (job.gaffe || llm == null || !llm.Ready)
            {
                sim.CompleteAutoReply(job, null);
                runtime.MarkDirty();
                return;
            }
            jobRunning = true;
            var messages = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("system", job.system),
                new KeyValuePair<string, string>("user", job.incoming.Length > 0 ? job.incoming : Lang.T("在吗？")),
            };
            var sampling = XgSpeechPolicy.Sampling(sim.S.stage);
            llm.Chat(messages, job.tokens, job.temperature, reply =>
            {
                if (!ReferenceEquals(sim, bound)) return;
                jobRunning = false;
                sim.CompleteAutoReply(job, reply);
                if (runtime != null) runtime.MarkDirty();
                // An automatic reply nobody is waiting on: background lane (null after its budget runs out).
            }, false, sampling, LingGuangV05.Core.Chat.LlmSeat.Others, LingGuangV05.Core.Chat.LlmLane.Background);
        }

        public void Changed() { if (runtime != null && !runtime.TestMode) runtime.MarkDirty(); }
    }
}
