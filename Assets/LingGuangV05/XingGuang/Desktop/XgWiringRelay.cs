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
    /// the 接线 log, which has the same buttons. Attaches itself next to the 灵光 controller at scene load.
    /// Presentation only.
    /// </summary>
    public sealed class XgWiringRelay : MonoBehaviour
    {
        const string Group = XgMarketRelay.Group;

        XingGuangController controller;
        XgSim bound;
        bool asked, scripted;

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
            if (!ReferenceEquals(bound, controller.Sim)) { bound = controller.Sim; asked = false; }
            var hub = YYChatHub.Instance;
            if (hub == null || hub.S == null || hub.Conversation(Group) == null) return;
            Ask(hub);
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
    }
}
