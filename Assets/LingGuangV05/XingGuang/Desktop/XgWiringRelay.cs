using System;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Carries 接线 (XgSim.Wiring.cs) to the other desktop apps. 阿杰's question goes into the YY 网吧群 with the three
    /// answers as choices; his replies follow through the friends' outbox (XgMarketRelay). Without YY the question stays in
    /// the 接线 log, which has the same buttons. It also hooks 林晴雯's stand-in switch (YYGirlfriend.SetStandIn) to the
    /// 替我回晴雯 wire, and runs the 网吧群 job: with it wired, the AI answers 小刚's danmaku question in the group for
    /// the player. Attaches itself next to the 灵光 controller at scene load. Presentation only.
    /// </summary>
    public sealed class XgWiringRelay : MonoBehaviour
    {
        const string Group = XgMarketRelay.Group;
        /// <summary>Seconds before the AI answers a group question for the player (it reads like a person typing).</summary>
        const float NetbarDelay = 5;

        XingGuangController controller;
        XgSim bound;
        YYGirlfriend hooked;
        XgSim hookedSim;
        bool asked, scripted;
        string quizSeen = "";
        float quizAt = -1;
        readonly System.Random rng = new System.Random();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Attach()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.GetComponent<XgWiringRelay>() != null) return;
            runtime.gameObject.AddComponent<XgWiringRelay>();
        }

        static string T(string zh, string en) => GameText.T(zh, en);

        void Update()
        {
            if (controller == null) controller = GetComponent<XingGuangController>();
            if (controller == null || controller.Sim == null) return;
            if (!ReferenceEquals(bound, controller.Sim)) { bound = controller.Sim; asked = false; quizSeen = ""; }
            HookStandIn();
            var hub = YYChatHub.Instance;
            if (hub == null || hub.S == null || hub.Conversation(Group) == null) return;
            Ask(hub);
            Netbar(hub);
        }

        /// <summary>The stand-in switch lives in YY; the 接线 wire reads and writes it.</summary>
        void HookStandIn()
        {
            var gf = YYGirlfriend.Instance;
            if (gf == null || (ReferenceEquals(gf, hooked) && ReferenceEquals(bound, hookedSim))) return;
            hooked = gf; hookedSim = bound;
            bound.HookLife(XgSim.LifeQingwen, () => gf != null && gf.StandInAvailable, () => gf != null && gf.G != null && gf.G.aiAuto, on => { if (gf != null) gf.SetStandIn(on); });
        }

        void Ask(YYChatHub hub)
        {
            if (!bound.AjiePending)
            {
                // Answered (here or on the 接线 page): take the choices back.
                if (scripted) { hub.Script(null, null); if (hub.ChoicesFor == Group) hub.Offer(null, null); scripted = false; }
                asked = false;
                return;
            }
            if (!asked)
            {
                asked = true;
                hub.Receive(Group, "[" + T("阿杰", "Ajie") + "] " + bound.AjieQuestion(bound.S.wiring.ajieAsked > 1, Math.Max(1, bound.CafeCardsOnline)));
                hub.Script(Group, Reply);
                scripted = true;
            }
            // 林晴雯's choices win the one choice row; ours come back when hers are gone.
            if (hub.ChoicesFor == null) hub.Offer(Group, Choices());
        }

        string[] Choices()
        {
            var c = new string[XgSim.AjieAnswers.Length];
            for (int i = 0; i < c.Length; i++) c[i] = bound.AjieAnswerText(XgSim.AjieAnswers[i]);
            return c;
        }

        /// <summary>The player's line in the group: one of the three answers (picked or typed), or ordinary chat.</summary>
        bool Reply(string text)
        {
            if (bound == null || !bound.AjiePending) return false;
            string t = (text ?? "").ToLowerInvariant();
            string answer = t.Contains("ai") || t.Contains("训练") || t.Contains("train") ? "ai"
                : t.Contains("挖矿") || t.Contains("矿") || t.Contains("min") ? "mine"
                : t.Contains("游戏") || t.Contains("副本") || t.Contains("game") ? "game" : null;
            if (answer == null) return false;
            bound.AnswerAjie(answer);
            var hub = YYChatHub.Instance;
            if (hub != null) hub.Script(null, null);
            scripted = false;
            return true;
        }

        /// <summary>YY 网吧群 · 替我答题: the AI answers 小刚's question with the danmaku checkpoint's accuracy.</summary>
        void Netbar(YYChatHub hub)
        {
            var s = hub.S;
            bool open = s.quizFrom == Group && s.quizDesk == "danmu" && s.quizText.Length > 0;
            if (!open || !bound.S.wiring.netbarChat || !bound.LifeJobLive(XgSim.LifeNetbar)) { if (!open) quizAt = -1; return; }
            if (quizSeen != s.quizText) { quizSeen = s.quizText; quizAt = Time.unscaledTime + NetbarDelay; return; }
            if (quizAt < 0 || Time.unscaledTime < quizAt) return;
            quizAt = -1;
            bool right = rng.NextDouble() < Math.Max(.5, bound.BestAcc("danmu"));
            bool praise = right ? s.quizTruth : !s.quizTruth;
            hub.Send(Group, praise ? T("夸", "praise") : T("骂", "no, a jab"));
            var conv = hub.Conversation(Group);
            if (conv != null) for (int i = conv.messages.Count - 1; i >= 0; i--) if (conv.messages[i].from == YYChatHub.Me) { conv.messages[i].byAi = true; break; }
            bound.LogWiring(T("本体在 YY 网吧群替你回了小刚：", "It answered Xiaogang in the YY café group for you: ") + (praise ? T("夸", "praise") : T("骂", "a jab")) + (right ? "" : T("（答错了）", " (wrong)")), right ? 1 : 3);
        }
    }
}
