using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 「她爱我吗」 on the 对话 page (女友系统 §6.2–6.3): the suggested question 「她……爱我吗？」 once the inner voice has
    /// wondered, and the two consent buttons 【读吧】【算了】 after it asks to read her chats. The rules live in
    /// XgSim.Love.cs; the cutscene is LoveQuestionCutscene. Same pattern as the curtain call's chips.
    /// </summary>
    public sealed partial class XgChatPage
    {
        XgBtn loveAsk, loveRead, loveSkip;

        void BuildLove()
        {
            loveAsk = ui.Button(chips, "", AskLove, 15);
            loveRead = ui.Button(chips, "", () => AnswerLove(true), 15);
            loveSkip = ui.Button(chips, "", () => AnswerLove(false), 15);
            foreach (var b in new[] { loveAsk, loveRead, loveSkip }) { b.rt.anchorMin = Vector2.zero; b.rt.anchorMax = new Vector2(0, 1); b.rt.gameObject.SetActive(false); }
        }

        /// <summary>Shows the love chips from <paramref name="x"/> (after the curtain call's chips). True if any is shown.</summary>
        bool RefreshLove(float x)
        {
            if (loveAsk == null || Sim == null) return false;
            bool consent = Sim.S.loveAwaitingConsent, offer = !consent && !waiting && Sim.OfferLoveQuestion;
            loveAsk.rt.gameObject.SetActive(offer);
            loveRead.rt.gameObject.SetActive(consent); loveSkip.rt.gameObject.SetActive(consent);
            if (offer)
            {
                loveAsk.rt.offsetMin = new Vector2(x, 0); loveAsk.rt.offsetMax = new Vector2(x + 220, 0);
                loveAsk.Set(Sim.LoveQuestion, true, XgPalette.Accent, Color.white);
            }
            if (consent)
            {
                loveRead.rt.offsetMin = new Vector2(x, 0); loveRead.rt.offsetMax = new Vector2(x + 130, 0);
                loveSkip.rt.offsetMin = new Vector2(x + 140, 0); loveSkip.rt.offsetMax = new Vector2(x + 270, 0);
                loveRead.Set("【" + Sim.LoveConsentText(true) + "】", true, XgPalette.Accent, Color.white);
                loveSkip.Set("【" + Sim.LoveConsentText(false) + "】", true);
            }
            return offer || consent;
        }

        void AskLove()
        {
            if (waiting || !Sim.OfferLoveQuestion) return;
            input.text = Sim.LoveQuestion;
            Send();
        }

        void AnswerLove(bool read)
        {
            if (!Sim.AnswerLoveConsent(read)) return;
            Fx.Play(XgJuice.Sfx.Id.Tick);
            Refresh();
        }
    }
}
