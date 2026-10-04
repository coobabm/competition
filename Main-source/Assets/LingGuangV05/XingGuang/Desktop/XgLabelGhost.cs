using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The mirror (act 1 of the auto-labelling realisation): while the player hand-labels a desk whose checkpoint
    /// is at 60% or better, each new card shows the model's guess faintly in its top-right corner for a moment
    /// (「它：是」 / "It: yes"), with no explanation. The guess comes from <see cref="XgSim.GhostGuess"/>, the same
    /// one the sim counts toward the realisation. Lives on the label page's paper; one line in the page drives it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class XgLabelGhost : MonoBehaviour
    {
        const float Delay = .25f, FadeIn = .3f, Hold = 1.3f, FadeOut = .7f, Peak = .4f;

        TMP_Text label;
        long cardId = -1;
        XgSim owner;
        bool guess, english, on;
        float age;

        /// <summary>The ghost on this paper, made on first use.</summary>
        public static XgLabelGhost For(RectTransform paper)
        {
            if (paper == null) return null;
            var ghost = paper.GetComponentInChildren<XgLabelGhost>(true);
            if (ghost != null) return ghost;
            var font = paper.GetComponentInChildren<TMP_Text>(true);
            var rt = XgUi.Rect("Ghost Guess", paper, Vector2.one, Vector2.one, new Vector2(-150, -40), new Vector2(-10, -8));
            ghost = rt.gameObject.AddComponent<XgLabelGhost>();
            ghost.label = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) ghost.label.font = font.font;
            ghost.label.fontSize = 17; ghost.label.fontStyle = FontStyles.Italic;
            ghost.label.alignment = TextAlignmentOptions.TopRight;
            ghost.label.color = new Color32(64, 84, 150, 0);
            ghost.label.raycastTarget = false;
            ghost.label.textWrappingMode = TextWrappingModes.NoWrap;
            return ghost;
        }

        /// <summary>Called whenever the page shows <paramref name="card"/>; a new card starts the ghost over.</summary>
        public void Show(XgSim sim, string desk, XgCard card)
        {
            if (sim == null || card == null) { Hide(); return; }
            if (card.id == cardId && sim == owner) return;
            cardId = card.id; owner = sim;
            on = sim.GhostGuess(desk, out guess, out _);
            age = 0;
            english = GameText.IsEnglish;
            label.text = Text();
            Paint(0);
            transform.SetAsLastSibling();
        }

        public void Hide() { on = false; cardId = -1; owner = null; if (label != null) Paint(0); }

        string Text() => GameText.T("它：" + (guess ? "是" : "否"), "It: " + (guess ? "yes" : "no"));

        void Paint(float alpha) { var c = label.color; c.a = alpha; label.color = c; }

        void Update()
        {
            if (!on || label == null) return;
            age += Time.unscaledDeltaTime;
            if (english != GameText.IsEnglish) { english = GameText.IsEnglish; label.text = Text(); }
            float t = age - Delay;
            float a = t < 0 ? 0 : t < FadeIn ? t / FadeIn : t < FadeIn + Hold ? 1 : 1 - (t - FadeIn - Hold) / FadeOut;
            Paint(Peak * Mathf.Clamp01(a));
            if (t >= FadeIn + Hold + FadeOut) { on = false; Paint(0); }
        }
    }
}
