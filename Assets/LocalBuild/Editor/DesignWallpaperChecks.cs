using System;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;

public static class DesignWallpaperChecks
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); Debug.Log("WALLPAPER_CHECK: " + message); }
    public static void Run()
    {
        var reveal = new XgWallpaperReveal();
        var fresh = new object();
        Check(!reveal.Observe(fresh, false) && reveal.Opacity == 0, "Opening keeps original wallpaper");
        Check(reveal.Observe(fresh, true) && reveal.Opacity == 0, "Setup completion starts without a brightness jump");
        float last = 0;
        for (int frame = 0; frame < 48; frame++)
        {
            reveal.Tick(1f / 60);
            Check(reveal.Opacity >= last && reveal.Opacity - last < .04f, "Smooth fade frame " + frame);
            last = reveal.Opacity;
        }
        Check(reveal.Opacity > .999f, "Fade reaches opaque brain background");
        Check(!reveal.Observe(fresh, true), "Repeated refresh never restarts reveal");
        Check(!reveal.Observe(new object(), true) && reveal.Opacity == 1, "Existing save restores immediately");
        Check(!reveal.Observe(new object(), false) && reveal.Opacity == 0, "New opening restores default wallpaper");
        var meeting = new object(); reveal.Observe(meeting, false); reveal.Observe(meeting, true);
        reveal.Tick(.85f);
        Check(reveal.PartOpacity(0) == 0 && reveal.PartOpacity(3) == 0, "Points wait for wallpaper fade");
        for (int part = 0; part < 4; part++)
        {
            reveal.Tick(.225f);
            Check(reveal.PartOpacity(part) > .49f && reveal.PartOpacity(part) < .51f, "Part fades in smoothly: " + part);
            if (part < 3) Check(reveal.PartOpacity(part + 1) == 0, "Next point waits: " + part);
            reveal.Tick(.225f);
        }
        Check(reveal.PartOpacity(3) > .999f, "Golden question is last");
        Debug.Log("DESIGN_WALLPAPER_CHECKS_PASS");
    }
}
