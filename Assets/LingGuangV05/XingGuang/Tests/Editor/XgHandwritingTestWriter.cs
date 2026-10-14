using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang.Tests
{
/// <summary>Synthetic pen strokes for tests: each digit is a few skeleton paths, bent by one writer's habits plus a wobble per drawing.</summary>
public sealed class HandwritingWriter
{
    readonly float slant, width, height, wobble, tilt;
    readonly int[] variant = new int[10];
    public HandwritingWriter(int seed)
    {
        var r = new Random(seed);
        slant = (float)(r.NextDouble() * .8 - .4); width = (float)(.5 + r.NextDouble() * .6); height = (float)(.8 + r.NextDouble() * .2);
        wobble = (float)(.025 + r.NextDouble() * .03); tilt = (float)(r.NextDouble() * .2 - .1);
        for (int d = 0; d < 10; d++) variant[d] = r.NextDouble() < .5 ? 0 : 1;
    }
    static List<(float, float)> Arc(float cx, float cy, float rx, float ry, float a0, float a1)
    {
        var p = new List<(float, float)>(); int n = 28;
        for (int i = 0; i <= n; i++) { double a = (a0 + (a1 - a0) * i / n) * Math.PI / 180; p.Add((cx + rx * (float)Math.Cos(a), cy + ry * (float)Math.Sin(a))); }
        return p;
    }
    static List<(float, float)> Pl(params float[] c) { var p = new List<(float, float)>(); for (int i = 0; i < c.Length; i += 2) p.Add((c[i], c[i + 1])); return p; }
    static List<(float, float)> Cat(List<(float, float)> a, List<(float, float)> b) { var r = new List<(float, float)>(a); r.AddRange(b); return r; }

    static List<List<(float, float)>> Skeleton(int d, int v)
    {
        var s = new List<List<(float, float)>>();
        switch (d)
        {
            case 0: s.Add(v == 0 ? Arc(.5f, .5f, .32f, .46f, 0, 360) : Arc(.5f, .5f, .3f, .46f, 260, 640)); break;
            case 1: s.Add(v == 0 ? Pl(.5f, .05f, .5f, .95f) : Pl(.28f, .26f, .5f, .05f, .5f, .95f)); break;
            case 2: s.Add(Cat(Arc(.5f, .3f, .3f, .24f, 190, 400), v == 0 ? Pl(.2f, .95f, .85f, .95f) : Pl(.25f, .85f, .8f, .95f))); break;
            case 3: s.Add(v == 0 ? Cat(Arc(.45f, .27f, .28f, .22f, 200, 450), Arc(.45f, .72f, .33f, .25f, 270, 520)) : Cat(Pl(.2f, .08f, .75f, .08f, .4f, .46f), Arc(.45f, .72f, .33f, .25f, 270, 520))); break;
            case 4: if (v == 0) { s.Add(Pl(.7f, .05f, .15f, .65f, .85f, .65f)); s.Add(Pl(.66f, .3f, .66f, .95f)); } else { s.Add(Pl(.25f, .05f, .2f, .6f, .85f, .6f)); s.Add(Pl(.7f, .05f, .7f, .95f)); } break;
            case 5: s.Add(Cat(Pl(.75f, .07f, .27f, .07f, .22f, .45f), Arc(.46f, .67f, .32f, .27f, 250, 490))); break;
            case 6: s.Add(Cat(v == 0 ? Pl(.7f, .07f, .42f, .2f, .26f, .5f) : Pl(.62f, .05f, .3f, .4f), Arc(.5f, .72f, .28f, .22f, 190, 550))); break;
            case 7: s.Add(Pl(.15f, .08f, .85f, .08f, .4f, .95f)); if (v == 1) s.Add(Pl(.3f, .55f, .7f, .55f)); break;
            case 8: s.Add(Arc(.5f, .28f, .24f, .2f, 0, 360)); s.Add(Arc(.5f, .72f, .3f, .23f, 0, 360)); break;
            default: s.Add(Arc(.5f, .3f, .27f, .22f, 0, 360)); s.Add(v == 0 ? Pl(.77f, .3f, .65f, .95f) : Cat(Pl(.77f, .3f), Arc(.45f, .6f, .32f, .35f, 0, 80))); break;
        }
        return s;
    }

    public bool[] Draw(int digit, Random r)
    {
        var raster = new bool[XgHandwriting.Grid * XgHandwriting.Grid];
        float dSlant = slant + (float)(r.NextDouble() * .36 - .18), dWidth = width * (float)(.8 + r.NextDouble() * .4);
        int v = r.NextDouble() < .7 ? variant[digit] : 1 - variant[digit];
        float box = 26 + (float)r.NextDouble() * 24, ox = 6 + (float)r.NextDouble() * (52 - box), oy = 6 + (float)r.NextDouble() * (52 - box);
        float rot = tilt + (float)(r.NextDouble() * .16 - .08), cos = (float)Math.Cos(rot), sin = (float)Math.Sin(rot);
        float ph = (float)(r.NextDouble() * 6.28), ph2 = (float)(r.NextDouble() * 6.28);
        foreach (var path in Skeleton(digit, v))
        {
            (float x, float y)? prev = null;
            int idx = 0;
            foreach (var (px, py) in path)
            {
                if (path.Count > 8 && idx++ % 3 != 0 && idx != path.Count) continue;
                float x = (px - .5f) * dWidth, y = (py - .5f) * height;
                x += dSlant * -(py - .5f);
                x += wobble * (float)Math.Sin(py * 9 + ph) + (float)(r.NextDouble() - .5) * wobble;
                y += wobble * (float)Math.Sin(px * 8 + ph2) + (float)(r.NextDouble() - .5) * wobble;
                float rx = x * cos - y * sin, ry = x * sin + y * cos;
                var cur = (ox + (rx + .5f) * box, oy + (ry + .5f) * box);
                if (prev.HasValue) XgHandwriting.Line(raster, prev.Value.x, prev.Value.y, cur.Item1, cur.Item2);
                prev = cur;
            }
        }
        return raster;
    }
}
}
