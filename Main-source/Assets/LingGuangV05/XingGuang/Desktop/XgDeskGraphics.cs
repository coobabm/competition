using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>A 12306-style captcha tile: one simple icon, rotated and buried in noise lines and dots (more each level).</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgCaptchaGraphic : MaskableGraphic
    {
        int icon = -1, seed, level;
        public void Show(int value, int cardSeed, int noise) { if (value == icon && cardSeed == seed && noise == level) return; icon = value; seed = cardSeed; level = noise; SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float u = Mathf.Min(r.width, r.height);
            if (u <= 0 || icon < 0) return;
            var rng = new System.Random(seed * 31 + icon);
            float F() => (float)rng.NextDouble();
            Vector2 o = r.center - new Vector2(u, u) * .5f;
            XgDraw.Box(vh, o, o + new Vector2(u, u), Color.HSVToRGB(F(), .12f, .97f));
            float angle = (F() * 2 - 1) * (10 + 10 * level) * Mathf.Deg2Rad, scale = .78f + F() * .14f;
            Vector2 shift = new Vector2(F() - .5f, F() - .5f) * .08f * level;
            Vector2 P(float x, float y)
            {
                var p = new Vector2(x - .5f, y - .5f) * scale;
                p = new Vector2(p.x * Mathf.Cos(angle) - p.y * Mathf.Sin(angle), p.x * Mathf.Sin(angle) + p.y * Mathf.Cos(angle));
                return o + (p + new Vector2(.5f, .5f) + shift) * u;
            }
            float W(float w) => w * u * scale;
            DrawIcon(vh, icon, P, W, F);
            int lines = 2 + level * 3, dots = 10 + level * 25;
            for (int i = 0; i < lines; i++)
                XgDraw.Seg(vh, o + new Vector2(F(), F()) * u, o + new Vector2(F(), F()) * u, u * (.008f + F() * .01f), Color.HSVToRGB(F(), .6f, .55f + F() * .3f) * new Color(1, 1, 1, .75f));
            for (int i = 0; i < dots; i++)
                XgDraw.Disc(vh, o + new Vector2(F(), F()) * u, u * (.004f + F() * .01f), Color.HSVToRGB(F(), .5f, .5f + F() * .4f), 6);
        }

        delegate Vector2 Pt(float x, float y);

        static void DrawIcon(VertexHelper vh, int icon, System.Func<float, float, Vector2> P, System.Func<float, float> W, System.Func<float> F)
        {
            Color ink = new Color32(40, 44, 60, 255);
            switch (icon)
            {
                case 0: // 雨伞
                {
                    var canopy = new List<Vector2> { P(.5f, .58f) };
                    for (int i = 0; i <= 16; i++) { float a = Mathf.PI * i / 16; canopy.Add(P(.5f + Mathf.Cos(a) * .36f, .58f + Mathf.Sin(a) * .3f)); }
                    XgDraw.Poly(vh, canopy, new Color32(214, 64, 76, 255));
                    XgDraw.Seg(vh, P(.5f, .58f), P(.5f, .2f), W(.04f), ink);
                    var hook = new List<Vector2>(); for (int i = 0; i <= 8; i++) { float a = Mathf.PI + Mathf.PI * i / 8; hook.Add(P(.44f + Mathf.Cos(a) * .06f, .2f + Mathf.Sin(a) * .06f)); }
                    hook.Reverse(); XgDraw.Polyline(vh, hook, W(.04f), ink);
                    break;
                }
                case 1: // 茶杯
                    XgDraw.Poly(vh, new[] { P(.28f, .66f), P(.68f, .66f), P(.62f, .3f), P(.34f, .3f) }, new Color32(70, 130, 200, 255));
                    var handle = new List<Vector2>(); for (int i = 0; i <= 12; i++) { float a = -Mathf.PI / 2 + Mathf.PI * i / 12; handle.Add(P(.67f + Mathf.Cos(a) * .1f, .5f + Mathf.Sin(a) * .1f)); }
                    XgDraw.Polyline(vh, handle, W(.045f), new Color32(70, 130, 200, 255));
                    XgDraw.Poly(vh, new[] { P(.2f, .3f), P(.76f, .3f), P(.72f, .25f), P(.24f, .25f) }, ink);
                    XgDraw.Polyline(vh, new[] { P(.42f, .72f), P(.4f, .8f), P(.44f, .88f) }, W(.025f), new Color(.5f, .5f, .5f, .8f));
                    XgDraw.Polyline(vh, new[] { P(.54f, .72f), P(.52f, .8f), P(.56f, .88f) }, W(.025f), new Color(.5f, .5f, .5f, .8f));
                    break;
                case 2: // 风筝
                    XgDraw.Poly(vh, new[] { P(.5f, .88f), P(.74f, .58f), P(.5f, .32f), P(.26f, .58f) }, new Color32(240, 170, 40, 255));
                    XgDraw.Seg(vh, P(.5f, .88f), P(.5f, .32f), W(.02f), ink);
                    XgDraw.Seg(vh, P(.26f, .58f), P(.74f, .58f), W(.02f), ink);
                    XgDraw.Polyline(vh, new[] { P(.5f, .32f), P(.56f, .24f), P(.48f, .18f), P(.56f, .12f), P(.5f, .06f) }, W(.02f), ink);
                    break;
                case 3: // 灯笼
                    XgDraw.Ellipse(vh, P(.5f, .5f), W(.26f), W(.22f), new Color32(220, 40, 40, 255));
                    XgDraw.Box(vh, P(.4f, .7f), P(.6f, .77f), new Color32(230, 180, 40, 255));
                    XgDraw.Box(vh, P(.4f, .23f), P(.6f, .3f), new Color32(230, 180, 40, 255));
                    XgDraw.Seg(vh, P(.5f, .77f), P(.5f, .9f), W(.02f), ink);
                    for (int i = -1; i <= 1; i++) XgDraw.Seg(vh, P(.5f + i * .04f, .23f), P(.5f + i * .05f, .1f), W(.02f), new Color32(230, 180, 40, 255));
                    break;
                case 4: // 钥匙
                {
                    Color gold = new Color32(200, 160, 50, 255);
                    var ring = new List<Vector2>(); for (int i = 0; i <= 24; i++) { float a = Mathf.PI * 2 * i / 24; ring.Add(P(.3f + Mathf.Cos(a) * .13f, .5f + Mathf.Sin(a) * .13f)); }
                    XgDraw.Polyline(vh, ring, W(.06f), gold);
                    XgDraw.Seg(vh, P(.43f, .5f), P(.84f, .5f), W(.06f), gold);
                    XgDraw.Seg(vh, P(.72f, .5f), P(.72f, .38f), W(.05f), gold);
                    XgDraw.Seg(vh, P(.8f, .5f), P(.8f, .4f), W(.05f), gold);
                    break;
                }
                case 5: // 公交车
                    XgDraw.Poly(vh, new[] { P(.12f, .32f), P(.12f, .72f), P(.88f, .72f), P(.88f, .32f) }, new Color32(40, 150, 90, 255));
                    for (int i = 0; i < 4; i++) XgDraw.Poly(vh, new[] { P(.17f + i * .17f, .5f), P(.17f + i * .17f, .66f), P(.29f + i * .17f, .66f), P(.29f + i * .17f, .5f) }, new Color32(200, 230, 255, 255));
                    XgDraw.Disc(vh, P(.3f, .3f), W(.07f), ink);
                    XgDraw.Disc(vh, P(.7f, .3f), W(.07f), ink);
                    break;
                case 6: // 电风扇
                {
                    var cage = new List<Vector2>(); for (int i = 0; i <= 28; i++) { float a = Mathf.PI * 2 * i / 28; cage.Add(P(.5f + Mathf.Cos(a) * .25f, .6f + Mathf.Sin(a) * .25f)); }
                    XgDraw.Polyline(vh, cage, W(.025f), ink);
                    for (int k = 0; k < 3; k++)
                    {
                        float a = k * Mathf.PI * 2 / 3 + .3f;
                        XgDraw.Poly(vh, new[] { P(.5f, .6f), P(.5f + Mathf.Cos(a - .35f) * .21f, .6f + Mathf.Sin(a - .35f) * .21f), P(.5f + Mathf.Cos(a + .25f) * .2f, .6f + Mathf.Sin(a + .25f) * .2f) }, new Color32(80, 160, 220, 255));
                    }
                    XgDraw.Disc(vh, P(.5f, .6f), W(.04f), ink);
                    XgDraw.Seg(vh, P(.5f, .35f), P(.5f, .14f), W(.05f), ink);
                    XgDraw.Box(vh, P(.34f, .1f), P(.66f, .15f), ink);
                    break;
                }
                case 7: // 热水瓶
                    XgDraw.Box(vh, P(.37f, .14f), P(.63f, .72f), new Color32(220, 70, 90, 255));
                    XgDraw.Box(vh, P(.42f, .72f), P(.58f, .8f), new Color32(190, 190, 200, 255));
                    XgDraw.Box(vh, P(.4f, .8f), P(.6f, .88f), ink);
                    XgDraw.Box(vh, P(.37f, .3f), P(.63f, .34f), new Color32(250, 220, 120, 255));
                    XgDraw.Box(vh, P(.37f, .55f), P(.63f, .59f), new Color32(250, 220, 120, 255));
                    break;
                default: // 方向盘（老司机）
                {
                    var wheel = new List<Vector2>(); for (int i = 0; i <= 32; i++) { float a = Mathf.PI * 2 * i / 32; wheel.Add(P(.5f + Mathf.Cos(a) * .32f, .5f + Mathf.Sin(a) * .32f)); }
                    XgDraw.Polyline(vh, wheel, W(.07f), ink);
                    XgDraw.Disc(vh, P(.5f, .5f), W(.08f), ink);
                    XgDraw.Seg(vh, P(.5f, .5f), P(.18f, .5f), W(.05f), ink);
                    XgDraw.Seg(vh, P(.5f, .5f), P(.82f, .5f), W(.05f), ink);
                    XgDraw.Seg(vh, P(.5f, .5f), P(.5f, .18f), W(.05f), ink);
                    break;
                }
            }
        }
    }

    /// <summary>A rage-comic face for the meme desk: 0 笑, 1 生气, 2 哭, 3 发懵, 4 坏笑. The seed nudges the features.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgFaceGraphic : MaskableGraphic
    {
        int mood = -1, seed;
        public void Show(int value, int cardSeed) { if (value == mood && cardSeed == seed) return; mood = value; seed = cardSeed; SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float u = Mathf.Min(r.width, r.height);
            if (u <= 0 || mood < 0) return;
            var rng = new System.Random(seed);
            float J() => ((float)rng.NextDouble() * 2 - 1) * .025f;
            Vector2 o = r.center - new Vector2(u, u) * .5f;
            Vector2 P(float x, float y) => o + new Vector2(x + J() * .5f, y + J() * .5f) * u;
            float W(float w) => w * u;
            Color ink = new Color32(24, 24, 28, 255);
            XgDraw.Ellipse(vh, o + new Vector2(.5f, .5f) * u, W(.44f), W(.46f), Color.white, 48);
            XgDraw.Arc(vh, o + new Vector2(.5f, .5f) * u, W(.45f), W(.025f), 0, Mathf.PI * 2, ink, 64);
            float lw = W(.03f);
            switch (mood)
            {
                case 0: // 笑：眯眼 + 大张嘴
                    Curve(vh, P(.36f, .6f), .08f * u, Mathf.PI * .1f, Mathf.PI * .9f, lw, ink);
                    Curve(vh, P(.64f, .6f), .08f * u, Mathf.PI * .1f, Mathf.PI * .9f, lw, ink);
                    XgDraw.Pie(vh, P(.5f, .42f), W(.2f), Mathf.PI, Mathf.PI * 2, ink);
                    XgDraw.Pie(vh, P(.5f, .37f), W(.1f), Mathf.PI * 1.15f, Mathf.PI * 1.85f, new Color32(220, 90, 100, 255));
                    break;
                case 1: // 生气：倒八眉 + 青筋 + 撇嘴
                    XgDraw.Seg(vh, P(.26f, .72f), P(.44f, .64f), lw * 1.3f, ink);
                    XgDraw.Seg(vh, P(.74f, .72f), P(.56f, .64f), lw * 1.3f, ink);
                    XgDraw.Disc(vh, P(.38f, .56f), W(.035f), ink);
                    XgDraw.Disc(vh, P(.62f, .56f), W(.035f), ink);
                    Curve(vh, P(.5f, .2f), .18f * u, Mathf.PI * .3f, Mathf.PI * .7f, lw, ink);
                    Color red = new Color32(220, 40, 40, 255);
                    Vector2 v0 = P(.78f, .82f);
                    XgDraw.Seg(vh, v0 + new Vector2(-.05f, .02f) * u, v0 + new Vector2(-.01f, .01f) * u, lw, red);
                    XgDraw.Seg(vh, v0 + new Vector2(.05f, .02f) * u, v0 + new Vector2(.01f, .01f) * u, lw, red);
                    XgDraw.Seg(vh, v0 + new Vector2(-.05f, -.04f) * u, v0 + new Vector2(-.01f, -.02f) * u, lw, red);
                    XgDraw.Seg(vh, v0 + new Vector2(.05f, -.04f) * u, v0 + new Vector2(.01f, -.02f) * u, lw, red);
                    break;
                case 2: // 哭：八字眉 + 闭眼 + 泪 + 下撇嘴
                    XgDraw.Seg(vh, P(.26f, .64f), P(.44f, .72f), lw, ink);
                    XgDraw.Seg(vh, P(.74f, .64f), P(.56f, .72f), lw, ink);
                    XgDraw.Seg(vh, P(.3f, .58f), P(.44f, .58f), lw, ink);
                    XgDraw.Seg(vh, P(.56f, .58f), P(.7f, .58f), lw, ink);
                    Color tear = new Color32(80, 160, 240, 230);
                    XgDraw.Box(vh, P(.33f, .2f), P(.39f, .57f), tear);
                    XgDraw.Box(vh, P(.61f, .2f), P(.67f, .57f), tear);
                    Curve(vh, P(.5f, .22f), .14f * u, Mathf.PI * .25f, Mathf.PI * .75f, lw, ink);
                    break;
                case 3: // 发懵：大圆眼、小瞳孔乱看、O 嘴
                    XgDraw.Arc(vh, P(.36f, .6f), W(.09f), lw * .8f, 0, Mathf.PI * 2, ink);
                    XgDraw.Arc(vh, P(.64f, .6f), W(.07f), lw * .8f, 0, Mathf.PI * 2, ink);
                    XgDraw.Disc(vh, P(.39f, .63f), W(.025f), ink);
                    XgDraw.Disc(vh, P(.61f, .57f), W(.02f), ink);
                    XgDraw.Arc(vh, P(.5f, .3f), W(.05f), lw * .8f, 0, Mathf.PI * 2, ink);
                    XgDraw.Seg(vh, P(.28f, .74f), P(.42f, .76f), lw * .8f, ink);
                    XgDraw.Seg(vh, P(.58f, .79f), P(.72f, .76f), lw * .8f, ink);
                    break;
                default: // 坏笑：半眯眼 + 一边嘴角上扬
                    XgDraw.Seg(vh, P(.28f, .62f), P(.44f, .62f), lw, ink);
                    XgDraw.Seg(vh, P(.56f, .62f), P(.72f, .62f), lw, ink);
                    XgDraw.Disc(vh, P(.4f, .585f), W(.025f), ink);
                    XgDraw.Disc(vh, P(.68f, .585f), W(.025f), ink);
                    XgDraw.Seg(vh, P(.3f, .74f), P(.44f, .7f), lw * .8f, ink);
                    XgDraw.Seg(vh, P(.56f, .76f), P(.72f, .78f), lw * .8f, ink);
                    XgDraw.Polyline(vh, new[] { P(.34f, .34f), P(.5f, .32f), P(.62f, .35f), P(.7f, .42f) }, lw, ink);
                    XgDraw.Disc(vh, P(.3f, .44f), W(.05f), new Color32(255, 170, 170, 200));
                    XgDraw.Disc(vh, P(.7f, .44f), W(.05f), new Color32(255, 170, 170, 200));
                    break;
            }
        }

        static void Curve(VertexHelper vh, Vector2 c, float radius, float a0, float a1, float width, Color color)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i <= 12; i++) { float a = Mathf.Lerp(a0, a1, i / 12f); pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius); }
            XgDraw.Polyline(vh, pts, width, color);
        }
    }

    /// <summary>A 9×9 Go board: the marked black group gets red dots, the asked point a red ✕.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgGoGraphic : MaskableGraphic
    {
        string board = "";
        int marked = -1, cross = -1;
        public void Show(string cells, int markedStone, int crossCell)
        {
            if (cells == board && markedStone == marked && crossCell == cross) return;
            board = cells ?? ""; marked = markedStone; cross = crossCell; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float u = Mathf.Min(r.width, r.height);
            int n = XgVisual.Board;
            if (u <= 0 || board.Length != n * n) return;
            Vector2 o = r.center - new Vector2(u, u) * .5f;
            XgDraw.Box(vh, o, o + new Vector2(u, u), new Color32(222, 184, 118, 255));
            float m = u / (n + 1);
            Vector2 C(int i) => o + new Vector2((i % n + 1) * m, u - (i / n + 1) * m);
            Color line = new Color32(70, 50, 30, 255);
            for (int k = 0; k < n; k++)
            {
                XgDraw.Seg(vh, C(k), C(k + n * (n - 1)), 1.6f, line);
                XgDraw.Seg(vh, C(k * n), C(k * n + n - 1), 1.6f, line);
            }
            foreach (int s in new[] { 20, 24, 40, 56, 60 }) XgDraw.Disc(vh, C(s), m * .09f, line, 10);
            var group = new HashSet<int>(XgVisual.Group(board.ToCharArray(), marked));
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == '.') continue;
                bool black = board[i] == 'B';
                XgDraw.Disc(vh, C(i) + new Vector2(1.5f, -1.5f), m * .46f, new Color(0, 0, 0, .25f), 20);
                XgDraw.Disc(vh, C(i), m * .46f, black ? new Color32(28, 28, 32, 255) : new Color32(246, 246, 240, 255), 24);
                if (!black) XgDraw.Arc(vh, C(i), m * .45f, 1.2f, 0, Mathf.PI * 2, new Color32(90, 90, 90, 255), 24);
                if (group.Contains(i)) XgDraw.Disc(vh, C(i), m * .14f, new Color32(240, 60, 60, 255), 12);
            }
            if (cross >= 0 && cross < board.Length)
            {
                Color red = new Color32(230, 40, 40, 255);
                float h = m * .3f;
                XgDraw.Seg(vh, C(cross) + new Vector2(-h, -h), C(cross) + new Vector2(h, h), 3.5f, red);
                XgDraw.Seg(vh, C(cross) + new Vector2(-h, h), C(cross) + new Vector2(h, -h), 3.5f, red);
            }
        }
    }
}
