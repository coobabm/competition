using System.Collections;
using LingGuangV05.Desktop.Tieba;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// 宇宙闪烁 (design v1.1 §10.2 #8): the moment the abilities emerge in stage 6, the whole desktop flashes three
    /// times. Watches the lab and plays once per save; a save loaded after the moment never flashes.
    /// </summary>
    public sealed class UniverseFlicker : MonoBehaviour
    {
        object lab;
        bool wasOn;

        public static void Install(GameObject host) { if (host.GetComponent<UniverseFlicker>() == null) host.AddComponent<UniverseFlicker>(); }

        void Update()
        {
            var sim = TiebaHub.Lab();
            if (sim == null) return;
            if (!ReferenceEquals(sim, lab)) { lab = sim; wasOn = sim.S.abilities; return; }
            if (sim.S.abilities && !wasOn && PrologueDirector.Desk != null) StartCoroutine(Flash(PrologueDirector.Desk));
            wasOn = sim.S.abilities;
        }

        IEnumerator Flash(PrologueDesk desk)
        {
            var veil = PrologueDesk.Rect("Universe Flicker", desk.layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            veil.SetAsLastSibling();
            var img = PrologueDesk.Fill(veil, new Color(1, 1, 1, 0), false);
            for (int i = 0; i < 3; i++)
            {
                for (float t = 0; t < .09f; t += Time.unscaledDeltaTime) { img.color = new Color(1, 1, 1, t / .09f * .95f); yield return null; }
                for (float t = 0; t < .22f; t += Time.unscaledDeltaTime) { img.color = new Color(1, 1, 1, (1 - t / .22f) * .95f); yield return null; }
                yield return new WaitForSecondsRealtime(.18f);
            }
            Destroy(veil.gameObject);
        }
    }
}
