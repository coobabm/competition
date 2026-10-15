using System;
    internal static class XgTechTooltipGeometry
    {
        internal static float Width(float available, float preferred) => Math.Max(1, Math.Min(preferred, available - 8));

        // x is the centre, y is the top edge. Callers cap w/h before placement.
        internal static void Place(float left, float bottom, float right, float top,
            float w, float h, float nodeX, float nodeTop, float nodeBottom, out float x, out float y)
        {
            float desiredTop = nodeTop + 10 + h <= top - 4 ? nodeTop + 10 + h : nodeBottom - 4;
            x = Math.Max(left + w * .5f + 4, Math.Min(nodeX, right - w * .5f - 4));
            y = Math.Max(bottom + h + 4, Math.Min(desiredTop, top - 4));
        }
    }


internal static class Program
{
    static int count;
    static void Check(bool ok, string message) { count++; if (!ok) throw new Exception(message); }
    static void Near(float a, float b, string m) => Check(Math.Abs(a-b) < .002f, m);
    static void Main()
    {
        Near(XgTechTooltipGeometry.Width(1000, 312), 312, "normal width");
        Near(XgTechTooltipGeometry.Width(240, 312), 232, "narrow page must reserve both margins");
        Near(XgTechTooltipGeometry.Width(320, 312), 312, "exact-fit width");
        Near(XgTechTooltipGeometry.Width(9, 312), 1, "small positive bounds");
        XgTechTooltipGeometry.Place(-400,-300,400,300,312,100,0,0,-90,out var x,out var y);
        Near(x,0,"centred node"); Near(y,110,"prefer above with 10-unit gap");
        XgTechTooltipGeometry.Place(-400,-300,400,300,312,100,0,250,160,out x,out y);
        Near(y,156,"fall below when top has no room");
        // Independent containment properties, off-page nodes, every edge/corner,
        // full-height content, fractional DPI-like dimensions, non-centred root pivots.
        var random = new Random(62013);
        for (int i=0; i<4000; ++i)
        {
            float left=(float)(random.NextDouble()*2000-1000);
            float bottom=(float)(random.NextDouble()*2000-1000);
            float bw=(float)(9+random.NextDouble()*1591), bh=(float)(9+random.NextDouble()*1191);
            float w=XgTechTooltipGeometry.Width(bw,312);
            float h=i%3==0 ? bh-8 : (float)(1+random.NextDouble()*(bh-9));
            float nx=left+(float)(random.NextDouble()*bw*3-bw);
            float nt=bottom+(float)(random.NextDouble()*bh*3-bh);
            XgTechTooltipGeometry.Place(left,bottom,left+bw,bottom+bh,w,h,nx,nt,nt-118,out x,out y);
            Check(w>0 && w<=bw-8+.001f,"width fits");
            Check(x-w/2>=left+4-.002f && x+w/2<=left+bw-4+.002f,"horizontal containment");
            Check(y-h>=bottom+4-.002f && y<=bottom+bh-4+.002f,"vertical containment");
            const float dx=137.25f,dy=-73.5f;
            XgTechTooltipGeometry.Place(left+dx,bottom+dy,left+bw+dx,bottom+bh+dy,w,h,nx+dx,nt+dy,nt-118+dy,out var x2,out var y2);
            Near(x2,x+dx,"translation x"); Near(y2,y+dy,"translation y");
        }
        Console.WriteLine($"Executed C# geometry assertions: {count} passed.");
        Console.WriteLine("Unity compile: NOT RUN. TMP glyph layout/input: NOT RUN. Visual acceptance: NOT RUN.");
    }
}
