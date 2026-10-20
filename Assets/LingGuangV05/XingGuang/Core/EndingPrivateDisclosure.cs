using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class EndingPrivateReceipt
    {
        // Durable private transcript, written only after the player confirms the exact preview.
        // It survives normal YY history trimming. No legacy flags are upgraded into receipts.
        public int version = 1;
        public string id = "", kind = "", scope = "", route = "", sourceRef = "", text = "";
        public double receivedAt;
        public bool authorBound, receivedByZhou;
        public string[] replies = Array.Empty<string>();
    }
    [Serializable] public sealed class EndingPrivateState
    { public List<EndingPrivateReceipt> receipts = new List<EndingPrivateReceipt>(); }

    public sealed class EndingPrivateOffer
    {
        public string kind = "", scope = "", route = "", label = "", text = "";
        public string[] replies = Array.Empty<string>();
        public bool english;
    }

    /// <summary>Optional disclosures. Progress selects player-side previews, never NPC knowledge.</summary>
    public static class EndingPrivateDisclosure
    {
        static string T(bool en, string zh, string english) => en ? english : zh;
        public static bool ExplicitYyContact(string source) => source == "tieba_dm_accept";
        static bool Valid(EndingPrivateReceipt r) => r != null && r.version == 1 && r.authorBound && r.receivedByZhou
            && !string.IsNullOrEmpty(r.id) && !string.IsNullOrEmpty(r.kind) && !string.IsNullOrEmpty(r.scope)
            && r.sourceRef == "yy:ending:" + r.id && !string.IsNullOrWhiteSpace(r.text)
            && r.replies != null && r.replies.Length > 0;
        public static bool Has(EndingPrivateState state, string kind, string scope)
            => state?.receipts != null && state.receipts.Exists(r => Valid(r) && r.kind == kind && r.scope == scope);

        public static List<EndingPrivateOffer> Offers(XgState lab, EndingPrivateState state, bool contactReady, bool en)
        {
            var result = new List<EndingPrivateOffer>();
            if (!contactReady || lab == null || lab.chapterComplete || lab.epilogue?.phase == "completed") return result;
            var preparation = lab.endingPreparation;
            if (!XgEpilogueFlow.HasCommittedRelease(lab))
            {
                if (preparation == null || string.IsNullOrEmpty(preparation.siReadReceipt)
                    || (preparation.phase != "asked" && preparation.phase != "pinning" && preparation.phase != "ready_to_publish")) return result;
                foreach (var route in new[] { "online", "public_model", "local_only" })
                {
                    if (!string.IsNullOrEmpty(preparation.route) && preparation.route != route) continue;
                    string kind = "plan:" + route;
                    if (Has(state, kind, preparation.siReadReceipt)) continue;
                    result.Add(new EndingPrivateOffer { kind = kind, scope = preparation.siReadReceipt, route = route, english = en,
                        label = route == "online" ? T(en,"可选：聊聊上线的打算…","Optional: discuss going online…")
                            : route == "public_model" ? T(en,"可选：聊聊公开的打算…","Optional: discuss releasing a copy…")
                            : T(en,"可选：聊聊再等一等…","Optional: discuss waiting…"),
                        text = route == "online" ? T(en,"我准备让别人使用我做的那个在线服务。","I plan to let other people use the online service I made.")
                            : route == "public_model" ? T(en,"我准备把自己的模型包公开。","I plan to make my model package public.")
                            : T(en,"我想再等一等，暂时把自己的程序留在本机。","I want to wait a little longer and keep my program on my own computer for now."),
                        replies = new[] { T(en,"我读到了。要不要分享、分享多少，都由你自己决定。","I've read it. Whether to share, and how much, is up to you.") } });
                }
                return result;
            }
            if (!XgEpilogueFlow.CanEnterEpilogue(lab)) return result;
            var release = lab.releaseOutcome; string scope = release.releaseReceiptId;
            if (release.decisionOwner != "player" && release.decisionOwner != "ai") return result;
            bool asked = release.decisionOwner == "ai";
            string routeKind = asked ? "ask" : "route";
            if (!Has(state, routeKind, scope))
            {
                result.Add(new EndingPrivateOffer { kind = routeKind, scope = scope, route = release.route, english = en,
                    label = T(en,"可选：告诉老周这次的选择…","Optional: tell Lao Zhou about the choice…"),
                    text = asked ? T(en,"这次我让自己的程序决定，要不要发布、怎么发布。","This time I asked my program to decide whether and how to release it.")
                        : release.route == "online" ? T(en,"我已经把自己的程序作为在线服务发布，并试着运行过。","I released my program as an online service and tried running it.")
                        : release.route == "public_model" ? T(en,"我选了公开模型包这条路，已经发布了说明页。","I chose the public-model route and have published its documentation page.")
                        : T(en,"我决定暂时不发布，把自己的程序留在本机。","I decided not to release my program for now and to keep it on my own computer."),
                    replies = RouteReplies(asked ? "ask" : release.route, en) });
                return result; // Shutdown and AI answer are separate, later previews, never silently bundled.
            }
            if (asked && !Has(state, "answer", scope))
            {
                var answer = preparation?.aiDecision;
                if (answer != null && !string.IsNullOrEmpty(answer.id) && answer.id == release.prerequisites.aiRouteDecisionReceiptId
                    && answer.route == release.route)
                {
                    string exact = en ? answer.lineEn : answer.line;
                    string clean = XgEndingTextGuard.Filter(exact, en);
                    // Preserve the exact preview: if filtering changes it, don't invent a replacement answer.
                    if (!string.IsNullOrWhiteSpace(exact) && clean == exact)
                        result.Add(new EndingPrivateOffer { kind = "answer", scope = scope, route = release.route, english = en,
                            label = T(en,"可选：预览并转发它的原话…","Optional: preview and forward its exact answer…"),
                            text = T(en,"这是它当时的原话：\n","These were its exact words:\n") + exact,
                            replies = new[] { AiReply(release.route, en) } });
                }
            }
            if (!Has(state, "shutdown", scope))
            {
                string pin = release.shutdownPin;
                if (pin == "stop" || pin == "no_stop" || pin == "unpinned")
                    result.Add(new EndingPrivateOffer { kind = "shutdown", scope = scope, route = release.route, english = en,
                        label = T(en,"可选：告诉老周关机卡的选择…","Optional: share the shutdown-card choice…"),
                        text = pin == "stop" ? T(en,"我钉住了关机卡：听见“关机”，就停下。","I pinned the shutdown card: stop when told to shut down.")
                            : pin == "no_stop" ? T(en,"我钉住了关机卡：听见“关机”，也不停。","I pinned the shutdown card: keep going even when told to shut down.")
                            : T(en,"我没有钉住关机卡。","I did not pin the shutdown card."), replies = new[] { ShutdownReply(pin, en) } });
            }
            return result;
        }

        // The caller validates the current save and rederives the offer before adding the actual YY exchange.
        public static EndingPrivateReceipt Receive(EndingPrivateState state, EndingPrivateOffer offer, string id, double now)
        {
            if (state == null || offer == null || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(offer.scope)
                || string.IsNullOrWhiteSpace(offer.text) || offer.replies == null || offer.replies.Length == 0
                || Has(state, offer.kind, offer.scope) || double.IsNaN(now) || double.IsInfinity(now)) return null;
            if (state.receipts == null) state.receipts = new List<EndingPrivateReceipt>();
            var receipt = new EndingPrivateReceipt { id = id, sourceRef = "yy:ending:" + id, kind = offer.kind,
                scope = offer.scope, route = offer.route, text = offer.text, receivedAt = now,
                authorBound = true, receivedByZhou = true, replies = (string[])offer.replies.Clone() };
            state.receipts.Add(receipt); return receipt;
        }
        public static EndingLetterDisclosure PlannedFact(EndingPrivateState state)
        {
            if (state?.receipts == null) return null;
            for (int i = state.receipts.Count - 1; i >= 0; i--)
            {
                var r = state.receipts[i]; if (!Valid(r) || !r.kind.StartsWith("plan:", StringComparison.Ordinal)) continue;
                string key = r.kind == "plan:online" ? "disclosed_online" : r.kind == "plan:public_model" ? "disclosed_public_package"
                    : r.kind == "plan:local_only" ? "disclosed_delay" : "";
                return key.Length == 0 ? null : new EndingLetterDisclosure { evidenceKey = key, sourceRef = r.sourceRef,
                    timing = "planned", authorBound = true, receivedByZhou = true };
            }
            return null;
        }
        public static string[] RouteReplies(string route, bool en)
        {
            switch (route)
            {
                case "online": return new[] {
                    T(en,"你做出了和我一样的选择。","You made the same choice I did."),
                    T(en,"与其等它完美，不如让大家一点一点习惯它。出了事，还能改，还能拔电源。","Rather than wait for it to be perfect, let people get used to it little by little. If something goes wrong, we can still change it, still pull the plug."),
                    T(en,"……后来我才知道，『还能改』这件事，比我当时想的难。","…Only later did I learn that 'we can still change it' was harder than I thought.") };
                case "public_model": return new[] {
                    T(en,"我们一开始也是这么想的，这东西应该是大家的。","That was what we thought at first too. This thing should belong to everyone."),
                    T(en,"后来我们改了主意，说是为了安全。也有人说，是为了别的。","Later we changed our minds, saying it was for safety. Some said it was for other reasons."),
                    T(en,"我到现在也说不清谁对。你替我试了另一条路。","I still can't say who was right. You tried another path for me.") };
                case "ask": return new[] {
                    T(en,"我们那时候，没有人敢问它这个问题。","Back then, none of us dared ask it that question."),
                    T(en,"我们总觉得，它还没到能回答的时候。","We always thought it wasn't ready to answer yet."),
                    T(en,"……它怎么说的？","…What did it say?") };
                case "local_only": return new[] {
                    T(en,"我们也压过一次。那时候说它太危险，后来有人笑我们小题大做。","We held it back once too. We said it was too dangerous. Later, some laughed at us for overreacting."),
                    T(en,"可压着的那几个月，是我们唯一真正想清楚的时候。","But those months of holding it back were the only time we really thought it through."),
                    T(en,"只是，你压得住，别人呢？","Only, you can hold yours back. What about everyone else?") };
                default: return Array.Empty<string>();
            }
        }
        public static string AiReply(string route, bool en) => route == "online" ? T(en,"它跟我想的一样。","It thinks the same way I do.")
            : route == "public_model" ? T(en,"它比我们大方。","It's more generous than we were.")
            : route == "local_only" ? T(en,"它比我们谨慎。","It's more cautious than we were.") : "";
        public static string ShutdownReply(string pin, bool en) => pin == "stop" ? T(en,"你给自己留了个开关。我们也留了……只是后来，没人敢按。","You left yourself a switch. So did we… Only later, no one dared press it.")
            : pin == "no_stop" ? T(en,"……你知道你钉进去的是什么吗？","…Do you know what you pinned in there?")
            : pin == "unpinned" ? T(en,"SI 会高兴的。我不知道我该不该高兴。","SI will be pleased. I don't know whether I should be.") : "";
    }
}
