using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    /// <summary>
    /// One hole of 打地鼠, drawn in code (no sprites): a dark ellipse in the lawn, a mole that pops out of it, the plank it holds
    /// the sum on, LingGuang's purple hat, and the hit flash with a swinging mallet. The game sets <see cref="Rise"/>,
    /// <see cref="Ai"/> and <see cref="HitTime"/> and calls <see cref="Redraw"/>; the sum itself is a TMP label the game lays
    /// over <see cref="SignCenter"/>. It owns no child objects, so nothing has to be destroyed while a RectMask2D holds it.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgMoleGraphic : MaskableGraphic, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public const float CellWidth = 245, CellHeight = 100;
        /// <summary>How long the hit flash and mallet stay.</summary>
        public const float HitSeconds = .45f;
        const float HoleY = 24, BodyHeight = 62, BodyHalf = 38, SignLift = 6;

        static readonly Color Mound = new Color(.22f, .37f, .21f), Rim = new Color(.47f, .33f, .2f), Hole = new Color(.09f, .06f, .05f),
            Fur = new Color(.5f, .35f, .24f), FurDark = new Color(.4f, .27f, .18f), Plank = new Color(.93f, .8f, .52f), PlankEdge = new Color(.42f, .28f, .14f),
            HatColor = new Color(.55f, .36f, .8f), HatDark = new Color(.38f, .22f, .62f), HatBand = new Color(.86f, .78f, 1f);

        /// <summary>0 hidden, 1 fully up; the pop overshoots a little.</summary>
        public float Rise;
        /// <summary>The mole wears LingGuang's purple hat.</summary>
        public bool Ai;
        public bool Golden;
        public bool WideSign, Reduced;
        /// <summary>Seconds since the mole was whacked, or negative while it has not been.</summary>
        public float HitTime = -1;
        /// <summary>The whack was a right one (the sum was wrong), which flashes gold; a wrongful one flashes cool grey.</summary>
        public bool HitGood;
        public Action Down;
        public Action<bool> Hover;
        float paintedRise, paintedHit;
        bool painted, paintedAi, paintedGold, paintedGood, paintedWide, paintedReduced;

        public void Redraw()
        {
            float visibleHit = Reduced ? (HitTime >= 0 && HitTime < HitSeconds ? 0 : -1) : HitTime;
            if (painted && paintedRise == Rise && paintedHit == visibleHit && paintedAi == Ai && paintedGold == Golden
                && paintedGood == HitGood && paintedWide == WideSign && paintedReduced == Reduced) return;
            painted = true; paintedRise = Rise; paintedHit = visibleHit; paintedAi = Ai; paintedGold = Golden;
            paintedGood = HitGood; paintedWide = WideSign; paintedReduced = Reduced;
            SetVerticesDirty();
        }
        public void OnPointerEnter(PointerEventData e) => Hover?.Invoke(true);
        public void OnPointerExit(PointerEventData e) => Hover?.Invoke(false);

        /// <summary>The sign shows once the mole is up; the label's centre, in pixels from the cell's top left.</summary>
        public bool SignShown => Rise >= .9f;
        public Vector2 SignCenter
        {
            get
            {
                var r = rectTransform.rect;
                return new Vector2(r.width * .5f, r.height - (HoleY + SignLift - (1 - Mathf.Min(1, Rise)) * 20));
            }
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            Down?.Invoke();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float cx = r.center.x, cy = r.yMin + HoleY;

            // The hole: a mound of lawn, a lighter rim, the dark opening.
            XgDraw.Ellipse(vh, new Vector2(cx, cy - 3), 104, 25, Mound, 32);
            XgDraw.Ellipse(vh, new Vector2(cx, cy - 1), 88, 21, Rim, 32);
            XgDraw.Ellipse(vh, new Vector2(cx, cy), 80, 17, Hole, 32);

            float rise = Mathf.Max(0, Rise), body = BodyHeight * rise;
            bool hit = HitTime >= 0 && HitTime < HitSeconds;
            if (body > .5f)
            {
                if (hit && !Reduced) Burst(vh, new Vector2(cx, cy + Mathf.Min(body, BodyHeight) * .6f));
                Mole(vh, cx, cy, body, hit);
                if (rise >= .9f) Board(vh, cx, cy + SignLift - (1 - Mathf.Min(1, rise)) * 20);
                if (Ai && body > 24) Hat(vh, cx, cy + body);
            }
            // The front lip of the hole hides the mole's base.
            var lip = new Vector2[17];
            for (int i = 0; i <= 16; i++) { float a = Mathf.PI + Mathf.PI * i / 16; lip[i] = new Vector2(cx + Mathf.Cos(a) * 82, cy + Mathf.Sin(a) * 18.5f); }
            XgDraw.Polyline(vh, lip, 4, Rim);
            if (hit && body > .5f && !Reduced) Mallet(vh, cx, cy);
        }

        static void HalfEllipse(VertexHelper vh, Vector2 c, float rx, float ry, Color color)
        {
            var pts = new Vector2[17];
            for (int i = 0; i <= 16; i++) { float a = Mathf.PI * i / 16; pts[i] = c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry); }
            XgDraw.Poly(vh, pts, color);
        }

        static void RoundBox(VertexHelper vh, Vector2 min, Vector2 max, float radius, Color color)
        {
            radius = Mathf.Min(radius, (max.x - min.x) * .5f, (max.y - min.y) * .5f);
            XgDraw.Box(vh, new Vector2(min.x + radius, min.y), new Vector2(max.x - radius, max.y), color);
            XgDraw.Box(vh, new Vector2(min.x, min.y + radius), new Vector2(min.x + radius, max.y - radius), color);
            XgDraw.Box(vh, new Vector2(max.x - radius, min.y + radius), new Vector2(max.x, max.y - radius), color);
            XgDraw.Disc(vh, new Vector2(min.x + radius, min.y + radius), radius, color, 10);
            XgDraw.Disc(vh, new Vector2(max.x - radius, min.y + radius), radius, color, 10);
            XgDraw.Disc(vh, new Vector2(min.x + radius, max.y - radius), radius, color, 10);
            XgDraw.Disc(vh, new Vector2(max.x - radius, max.y - radius), radius, color, 10);
        }

        void Mole(VertexHelper vh, float cx, float cy, float body, bool hit)
        {
            Color fur=Golden?new Color(1,.72f,.08f):Fur,furDark=Golden?new Color(.76f,.41f,.02f):FurDark;
            float dome = Mathf.Min(BodyHalf, body), straight = Mathf.Max(0, body - dome);
            if (straight > 0) XgDraw.Box(vh, new Vector2(cx - BodyHalf, cy), new Vector2(cx + BodyHalf, cy + straight), fur);
            HalfEllipse(vh, new Vector2(cx, cy + straight), BodyHalf, dome, fur);
            if (body < 30) return;
            float top = cy + body;
            XgDraw.Disc(vh, new Vector2(cx - 29, top - 7), 7, furDark, 12);
            XgDraw.Disc(vh, new Vector2(cx + 29, top - 7), 7, furDark, 12);
            float eyeY = top - 17;
            foreach (float s in new[] { -1f, 1f })
            {
                var e = new Vector2(cx + s * 13, eyeY);
                if (hit)
                {
                    XgDraw.Seg(vh, e + new Vector2(-4, -4), e + new Vector2(4, 4), 2.4f, Hole);
                    XgDraw.Seg(vh, e + new Vector2(-4, 4), e + new Vector2(4, -4), 2.4f, Hole);
                }
                else
                {
                    XgDraw.Disc(vh, e, 5, Color.white, 12);
                    XgDraw.Disc(vh, e + new Vector2(0, -.5f), 2.4f, Hole, 10);
                }
            }
            XgDraw.Ellipse(vh, new Vector2(cx, eyeY - 10), 6.5f, 4.5f, new Color(.95f, .55f, .6f), 14);
        }

        void Board(VertexHelper vh, float cx, float cy)
        {
            float half = WideSign ? 119 : 70, low = WideSign ? 23 : 17, high = WideSign ? 23 : 15;
            RoundBox(vh, new Vector2(cx - half, cy - low), new Vector2(cx + half, cy + high), 6, PlankEdge);
            RoundBox(vh, new Vector2(cx - half + 2.5f, cy - low + 2.5f), new Vector2(cx + half - 2.5f, cy + high - 2.5f), 4, Golden?new Color(1,.91f,.35f):Plank);
            // The paws hold the sign up.
            XgDraw.Disc(vh, new Vector2(cx - 52, cy + 15), 7, FurDark, 12);
            XgDraw.Disc(vh, new Vector2(cx + 52, cy + 15), 7, FurDark, 12);
        }

        void Hat(VertexHelper vh, float cx, float top)
        {
            XgDraw.Ellipse(vh, new Vector2(cx, top + 2), 31, 6.5f, HatDark, 20);
            XgDraw.Poly(vh, new[] { new Vector2(cx - 20, top + 3), new Vector2(cx + 20, top + 3), new Vector2(cx + 5, top + 26), new Vector2(cx - 3, top + 30) }, HatColor);
            XgDraw.Poly(vh, new[] { new Vector2(cx - 20.5f, top + 3), new Vector2(cx + 20.5f, top + 3), new Vector2(cx + 17.5f, top + 10), new Vector2(cx - 17.5f, top + 10) }, HatBand);
            XgDraw.Disc(vh, new Vector2(cx + 1, top + 16), 2.2f, HatBand, 8);
        }

        /// <summary>The flash of a whack: a spiky star that swells and fades, with a ring.</summary>
        void Burst(VertexHelper vh, Vector2 c)
        {
            float t = Mathf.Clamp01(HitTime / HitSeconds), a = 1 - t;
            var color = HitGood ? new Color(1f, .8f, .2f, .95f * a) : new Color(.72f, .78f, .92f, .95f * a);
            float outer = 46 + 40 * t, inner = 22 + 12 * t;
            const int spikes = 10;
            var pts = new Vector2[spikes * 2];
            for (int i = 0; i < pts.Length; i++) { float ang = Mathf.PI * i / spikes + .3f, rad = i % 2 == 0 ? outer : inner; pts[i] = c + new Vector2(Mathf.Cos(ang) * rad * 1.45f, Mathf.Sin(ang) * rad * .9f); }
            for (int i = 0; i < spikes; i++) XgDraw.Poly(vh, new[] { c, pts[i * 2], pts[(i * 2 + 1) % pts.Length], pts[(i * 2 + 2) % pts.Length] }, color);
            var ring = color; ring.a *= .7f;
            XgDraw.Ring(vh, c, 20 + 50 * t, 4, ring, 28);
        }

        /// <summary>The mallet swings in from the right and lands on the mole's head.</summary>
        void Mallet(VertexHelper vh, float cx, float cy)
        {
            float swing = Mathf.Clamp01(HitTime / .12f);
            float eased = 1 - (1 - swing) * (1 - swing);
            float ang = Mathf.Lerp(65, 205, eased) * Mathf.Deg2Rad;
            var pivot = new Vector2(cx + 82, cy + 72);
            var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)); var side = new Vector2(-dir.y, dir.x);
            var end = pivot + dir * 56;
            XgDraw.Seg(vh, pivot, end, 8, new Color(.62f, .42f, .2f));
            var half = side * 14; var along = dir * 19;
            XgDraw.Quad(vh, end - along - half, end + along - half, end + along + half, end - along + half, new Color(.5f, .5f, .56f));
            XgDraw.Quad(vh, end + along * .55f - half, end + along - half, end + along + half, end + along * .55f + half, new Color(.36f, .36f, .42f));
        }
    }
}
