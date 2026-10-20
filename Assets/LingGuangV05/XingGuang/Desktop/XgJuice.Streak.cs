using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 连对倍率的演出 (2026-10-20: 「连对两题以后，再下一次答对奖励翻倍，最高五倍；每个倍率单独设计特效音效，越来越震撼」).
    /// Three moments, each scaled by the multiplier (<see cref="XgSim.StreakMultiplierOf"/>):
    ///  - <see cref="StreakUp"/>: the answer that reaches ×2 / ×3 / ×4 / ×5. Its own stinger (Resources/XgSfx/Streak/Up2–5)
    ///    and a show that adds a layer per tier: ring → rings and shake → hit-stop, coin flight and confetti → the full
    ///    灵光一闪 with the spider joining in. Like an idle game's level-up, the next tier is always visibly bigger.
    ///  - <see cref="StreakHit"/>: every other right answer while the multiplier is up. A short coin layer (Hit2–5) under the
    ///    Kill1–10 step and a "×N" stamp in the tier's colour, so a ×5 run keeps sounding and looking ×5.
    ///  - <see cref="StreakDrop"/>: a wrong answer that loses a multiplier. The surge collapses (Drop), louder the more was lost.
    /// Everything goes through the normal effect calls, so 「FX−」 (Reduced), mute and the particle limit apply as usual.
    /// </summary>
    public sealed partial class XgJuice
    {
        /// <summary>Colour of a multiplier: ×1 white, ×2 green, ×3 orange, ×4 red, ×5 gold (the tier palette).</summary>
        public static Color StreakColor(int multiplier) => XgPalette.Tiers[Mathf.Clamp(multiplier - 1, 0, XgPalette.Tiers.Length - 1)];

        /// <summary>The tier's name: ×2 不错, ×3 手热了, ×4 上头, ×5 灵光一闪.</summary>
        public static string StreakName(int multiplier)
        {
            int i = Mathf.Clamp(multiplier - 2, 0, XgCatalog.ComboTierNames.Length - 1);
            return T(XgCatalog.ComboTierNames[i], XgCatalog.ComboTierNamesEn[i]);
        }

        static readonly string[] SpiderLinesZh = { "", "", "钱翻倍了！", "手热了，接着来。", "上头了……别停！", "灵光一闪！五倍了！" };
        static readonly string[] SpiderLinesEn = { "", "", "Double pay!", "Warming up, keep going.", "On fire... don't stop!", "Flash of insight! Five times!" };

        /// <summary>A small commemorative badge, only at 20 and 50; no economy or save changes.</summary>
        public void StreakMilestone(Vector2 at, int combo)
        {
            if (!Visible() || MotionDisabled || (combo != 20 && combo != 50)) return;
            bool gold = combo == 50;
            Color ink = gold ? new Color(1f, .82f, .43f) : new Color(.51f, .87f, .91f);
            Vector2 center = at + new Vector2(0, 124);
            Float(center, gold ? T("心流时刻", "IN THE FLOW") : T("渐入佳境", "FINDING YOUR RHYTHM"), ink,
                gold ? 30 : 27, 18, 1.55f, 1.12f);
            Float(center + new Vector2(0, -36), combo + T(" 连对 · 纪念", " CORRECT · MILESTONE"),
                Color.Lerp(ink, Color.white, .45f), 15, 18, 1.55f, 1.06f);
            // Paired stars frame the lettering; no full-screen flash or hit-stop.
            Burst(center + new Vector2(-145, -8), gold ? 5 : 3, ink, Shape.Star, 44, 25, .7f, .65f);
            Burst(center + new Vector2(145, -8), gold ? 5 : 3, ink, Shape.Star, 44, 25, .7f, .65f);
            Shake(gold ? 2.2f : 1.4f, .16f);
        }

        /// <summary>
        /// The answer that reached <paramref name="multiplier"/> (2–5). <paramref name="at"/> is where it landed (coins and
        /// sparks start there), <paramref name="center"/> the middle of the question area, where the call-out and the big
        /// rings go so they never pile onto the hit's own "+¥" and count (pass ComboHit countLabel: false for this answer);
        /// <paramref name="badge"/> the counter that shows the multiplier (popped), <paramref name="wallet"/> where the money
        /// goes (coins fly there from ×3). Call it instead of <see cref="StreakHit"/> for that answer.
        /// </summary>
        public void StreakUp(Vector2 at, Vector2 center, RectTransform badge, RectTransform wallet, int multiplier)
        {
            if (multiplier < 2 || !Visible()) return;
            int m = Mathf.Min(multiplier, XgSim.ComboMaxMultiplier);
            Color c = StreakColor(m);
            float k = (m - 2) / 3f; // 0 at ×2 … 1 at ×5
            PlayClip(Sfx.StreakClip("Up" + m), 1, Sfx.StreakGain);

            // ×2: one ring, a coin pop and the call-out.
            Flash(c, .1f + .14f * k, .16f + .3f * k);
            Shockwave(center, new Color(c.r, c.g, c.b, .9f), 180 + 160 * k, .32f + .12f * k, 8 + 8 * k);
            Burst(at, 12 + Mathf.RoundToInt(16 * k), XgPalette.Gold, Shape.Yen, 260 + 220 * k, 520, .8f + .3f * k, 1 + .4f * k);
            Float(center + new Vector2(0, -20), "×" + m + " " + StreakName(m) + "！", c, 34 + 22 * k, 60 + 30 * k, 1.1f + .5f * k, 1.8f + .7f * k);
            if (badge != null) { Knock(badge, .22f + .2f * k, Vector2.up * (10 + 20 * k), .3f + .1f * k, squash: true); FlashWhite(badge, .12f, .9f); }

            // ×3: a second ring, stars, a shake and the first coins flying to the wallet.
            if (m >= 3)
            {
                Shockwave(center, new Color(1, 1, 1, .55f), 260 + 200 * k, .5f, 4);
                Burst(center, 6 + Mathf.RoundToInt(10 * k), Color.Lerp(Color.white, c, .5f), Shape.Star, 240 + 200 * k, 300, .8f, 1.2f + .4f * k);
                Shake(4 + 10 * k, .22f + .2f * k);
                if (wallet != null) TransferCoins(at, At(wallet), 4 + Mathf.RoundToInt(8 * k));
            }
            // ×4: the hit-stop lands it, a wide third ring and confetti.
            if (m >= 4)
            {
                HitStop(70 + 90 * k);
                Shockwave(center, new Color(c.r, c.g, c.b, .5f), 420 + 200 * k, .7f, 14);
                Burst(center, 24 + Mathf.RoundToInt(12 * k), c, Shape.Confetti, 420, 520, 1.1f, 1.2f);
                Burst(at, 10, Color.white, Shape.Spark, 520, 300, .5f, 2.4f);
            }
            // ×5 灵光一闪: gold everywhere, a screen-wide ring, a second line and the spider cheering.
            if (m >= 5)
            {
                Shockwave(center, new Color(1, .95f, .7f, .65f), 760, .9f, 22);
                Burst(center, 18, XgPalette.Gold, Shape.Star, 620, 220, 1.3f, 1.8f);
                Float(center + new Vector2(0, -95), T("满倍 · 每题 ×5", "Max · ×5 every answer"), XgPalette.Gold, 26, 30, 1.6f, 1.3f);
            }
            if (m >= 4) XgResidentSpider.Instance?.Speak(T(SpiderLinesZh[m], SpiderLinesEn[m]), 2.4f);
        }

        /// <summary>Any other right answer while the multiplier is ×2 or more: the tier's coin layer and a ×N stamp.</summary>
        public void StreakHit(Vector2 at, int multiplier)
        {
            if (multiplier < 2 || !Visible()) return;
            int m = Mathf.Min(multiplier, XgSim.ComboMaxMultiplier);
            Color c = StreakColor(m);
            PlayClip(Sfx.StreakClip("Hit" + m), 1, Sfx.StreakGain);
            Float(at + new Vector2(70, 34), "×" + m, c, 24 + 4 * m, 50 + 8 * m, .6f + .05f * m, 1.3f + .1f * m);
            Burst(at, 2 + 2 * m, XgPalette.Gold, Shape.Yen, 180 + 30 * m, 520, .6f, .8f + .1f * m);
            if (m >= 4) Shockwave(at, new Color(c.r, c.g, c.b, .45f), 120 + 30 * m, .3f, 5);
        }

        /// <summary>A wrong answer that took the multiplier from <paramref name="lost"/> down to <paramref name="now"/>.</summary>
        public void StreakDrop(Vector2 at, RectTransform badge, int lost, int now)
        {
            if (lost < 2 || lost <= now || !Visible()) return;
            float k = (Mathf.Min(lost, XgSim.ComboMaxMultiplier) - 2) / 3f;
            PlayClip(Sfx.StreakClip("Drop"), .9f + .1f * (1 - k), Sfx.StreakGain * (.6f + .4f * k));
            Crumble(at, 10 + Mathf.RoundToInt(20 * k), StreakColor(lost));
            Float(at + new Vector2(0, 70), "×" + lost + " → ×" + Mathf.Max(1, now), XgPalette.Bad, 26 + 8 * k, 40, .9f, 1.2f);
            if (badge != null) Knock(badge, .1f + .1f * k, Vector2.down * (8 + 12 * k), .3f);
            if (lost >= 4) Shake(3 + 3 * k, .2f);
        }

        public static partial class Sfx
        {
            /// <summary>Bell accents sit beneath the original click/Kill sounds, which remain the lead layer.</summary>
            public const float StreakGain = .55f;
            static readonly Dictionary<string, AudioClip> streakClips = new Dictionary<string, AudioClip>();

            /// <summary>Stop existing streak tails when muting, without touching other effects or music.</summary>
            public static void StopStreakClips()
            {
                if (sources == null) return;
                foreach (var source in sources)
                {
                    if (source == null || source.clip == null || !streakClips.ContainsValue(source.clip)) continue;
                    source.Stop();
                    source.clip = null;
                }
            }

            /// <summary>Resources/XgSfx/Streak/<paramref name="name"/> (Up2–5, Hit2–5, Drop; made by Tools/XingGuang/sfx/make_streak_sfx.py), or null.</summary>
            public static AudioClip StreakClip(string name)
            {
                if (!streakClips.TryGetValue(name, out var clip))
                    streakClips[name] = clip = Resources.Load<AudioClip>("XgSfx/Streak/" + name);
                RequestAudioData(clip);
                return clip;
            }
        }
    }
}
