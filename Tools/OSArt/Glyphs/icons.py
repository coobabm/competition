"""Desktop glyph icons (2026-10-19): our own drawings, replaces the Lucide set.

Every glyph below is drawn by hand from simple primitives (lines, circles, arcs, rounded rects, polygons
computed here) on a 24x24 grid: white stroke 2, round caps and joins; the "Filled" variants add a fill.
The 24-unit grid maps straight onto the 256x256 texture (about 10.7 px per unit, 1 unit of margin), which
keeps the line weight and footprint of the set they replace, so the desktop stays consistent.

Renders all SVGs in one headless-Chrome screenshot (transparent background, same way as
Tools/OSArt/Icons/make_yy_icon.py), slices it, and writes the PNG (256x256 RGBA, pure white, alpha = coverage)
over every existing file of that name in Textures/Icons/{32x,64x,128x,256x} and in
Textures/Icons/Others/Settings Elements. Never creates files that are not there today; .meta files are
never touched, so GUIDs and import settings stay as they are.

Not drawn here (already ours, made by Tools/OSArt/Sprites/gen_sprites.py): Panel Indicator, Resize Stripes,
Cori, Cori Alt, Cori Alt 2, Discord, Reddit, Steam, Twitter, YouTube, and the Settings Elements Discord /
Twitter / YouTube line icons.

    python3 icons.py                 # rewrite the project PNGs in place
    python3 icons.py --out /tmp/x    # write <name>.png files to a folder instead (preview)
    python3 icons.py --sheet a.png   # also write a labelled contact sheet
"""
import argparse
import math
import os
import subprocess
import tempfile

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
ICONS = os.path.join(ROOT, "Assets/LingGuangV05/DesktopBridge/OSArt/Textures/Icons")
SIZE_DIRS = ("32x", "64x", "128x", "256x")
SETTINGS_DIR = "Others/Settings Elements"
CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
PX = 256
COLS = 10


# --------------------------------------------------------------------------- geometry helpers
def f(v):
    return ("%.2f" % v).rstrip("0").rstrip(".")


def pt(cx, cy, r, deg):
    """Point on a circle; 0 degrees = 12 o'clock, clockwise positive."""
    a = math.radians(deg)
    return cx + r * math.sin(a), cy - r * math.cos(a)


def arc(cx, cy, r, a0, a1):
    """Open clockwise arc path from a0 to a1 (degrees, see pt)."""
    x0, y0 = pt(cx, cy, r, a0)
    x1, y1 = pt(cx, cy, r, a1)
    large = 1 if (a1 - a0) % 360 > 180 else 0
    return f'<path d="M{f(x0)} {f(y0)}A{f(r)} {f(r)} 0 {large} 1 {f(x1)} {f(y1)}"/>'


def line(x0, y0, x1, y1):
    return f'<line x1="{f(x0)}" y1="{f(y0)}" x2="{f(x1)}" y2="{f(y1)}"/>'


def poly(pts, closed=False, fill=False):
    tag = "polygon" if closed else "polyline"
    s = " ".join(f"{f(x)},{f(y)}" for x, y in pts)
    extra = ' fill="#fff"' if fill else ""
    return f'<{tag} points="{s}"{extra}/>'


def circle(cx, cy, r, fill=False):
    extra = ' fill="#fff" stroke="none"' if fill else ""
    return f'<circle cx="{f(cx)}" cy="{f(cy)}" r="{f(r)}"{extra}/>'


def dot(cx, cy, r=1.25):
    return circle(cx, cy, r, fill=True)


def rect(x0, y0, x1, y1, r=0, fill=False, extra=""):
    fl = ' fill="#fff"' if fill else ""
    return (f'<rect x="{f(x0)}" y="{f(y0)}" width="{f(x1 - x0)}" height="{f(y1 - y0)}" rx="{f(r)}"'
            f'{fl}{extra}/>')


def path(d, fill=False):
    return f'<path d="{d}"{" fill=\"#fff\"" if fill else ""}/>'


def arrowhead(x, y, deg, size=3.2):
    """Open chevron tip at (x, y) pointing toward deg (0 = up, clockwise)."""
    a = pt(x, y, size, deg + 180 - 45)
    b = pt(x, y, size, deg + 180 + 45)
    return poly([a, (x, y), b])


def star_pts(cx, cy, ro, ri, n=5):
    out = []
    for k in range(2 * n):
        out.append(pt(cx, cy, ro if k % 2 == 0 else ri, k * 180 / n))
    return out


def gear_pts(cx, cy, ro, ri, teeth=8, top=0.36, root=0.5):
    """Gear outline: each tooth spans `top` of its pitch at ro, flanks taper to the root circle ri."""
    pitch = 360 / teeth
    out = []
    for k in range(teeth):
        c = k * pitch
        ht = pitch * top / 2
        hr = pitch * root / 2
        out += [pt(cx, cy, ri, c - hr), pt(cx, cy, ro, c - ht), pt(cx, cy, ro, c + ht), pt(cx, cy, ri, c + hr)]
    return out


class Masked:
    """Draw `body` with transparent gaps where `cut` strokes would be (gap = cut width)."""
    n = 0

    def __call__(self, body, cut, width=5):
        Masked.n += 1
        mid = f"m{Masked.n}"
        return (f'<mask id="{mid}" maskUnits="userSpaceOnUse" x="0" y="0" width="24" height="24">'
                f'<rect width="24" height="24" fill="#fff" stroke="none"/>'
                f'<g stroke="#000" fill="#000" stroke-width="{f(width)}">{cut}</g></mask>'
                f'<g mask="url(#{mid})">{body}</g>')


masked = Masked()


# --------------------------------------------------------------------------- shared parts
def wifi(levels, dot_on=True):
    cx, cy = 12, 19.5
    s = dot(cx, cy, 1.45) if dot_on else ""
    for r in (4.6, 9.2, 13.6)[:levels]:
        s += arc(cx, cy, r, -46, 46)
    return s


def speaker():
    return poly([(3.5, 9.2), (7, 9.2), (11.5, 5), (11.5, 19), (7, 14.8), (3.5, 14.8)], closed=True)


def mic():
    return (rect(9, 2.5, 15, 13.5, 3) + path("M5.5 11a6.5 6.5 0 0 0 13 0") + line(12, 17.5, 12, 21)
            + line(8.5, 21, 15.5, 21))


def bell():
    return (path("M6 16.5V11a6 6 0 0 1 12 0v5.5l1.8 2.2H4.2Z") + path("M10 21a2 2 0 0 0 4 0")
            + line(12, 3, 12, 5))


def person():
    return circle(12, 7.8, 4.2) + path("M4.5 21a7.5 6.5 0 0 1 15 0")


def refresh():
    a0, a1 = 50, 320
    hx, hy = pt(12, 12, 8.3, a1)
    return arc(12, 12, 8.3, a0, a1) + arrowhead(hx, hy, a1 + 90 - 8)


def theme():
    return circle(12, 12, 9.5) + path("M12 2.5A9.5 9.5 0 0 1 12 21.5Z", fill=True)


def eye():
    return path("M2.5 12C5 7.3 8.4 5.2 12 5.2S19 7.3 21.5 12C19 16.7 15.6 18.8 12 18.8S5 16.7 2.5 12Z") \
        + circle(12, 12, 3)


def heart(fill=False):
    return path("M12 20.2L4.3 12.6a4.7 4.7 0 0 1 6.6-6.7L12 7l1.1-1.1a4.7 4.7 0 0 1 6.6 6.7Z", fill=fill)


def star(fill=False):
    return poly(star_pts(12, 12.7, 10, 4.3), closed=True, fill=fill)


def pin(fill=False):
    body = poly([(9, 3), (15, 3), (14.4, 9.6), (18, 13.6), (6, 13.6), (9.6, 9.6)], closed=True, fill=fill)
    return body + line(12, 13.6, 12, 21)


def play(fill=False):
    return poly([(7, 4.5), (19, 12), (7, 19.5)], closed=True, fill=fill)


def photo_scene(x0, y0, x1, y1, sun):
    """Hills + sun inside a frame; hills start/end on the frame edges."""
    h = y1 - y0
    w = x1 - x0
    hills = [(x0, y0 + 0.78 * h), (x0 + 0.32 * w, y0 + 0.45 * h), (x0 + 0.6 * w, y0 + 0.74 * h),
             (x0 + 0.75 * w, y0 + 0.6 * h), (x1, y0 + 0.86 * h)]
    return poly(hills) + circle(sun[0], sun[1], 1.6)


# --------------------------------------------------------------------------- the glyphs
def glyphs():
    G = {}
    G["Accessibility"] = (circle(12, 12, 9.8) + dot(12, 6.6, 1.5) + line(7.2, 9.6, 16.8, 9.6)
                          + line(12, 9.6, 12, 14) + poly([(9.3, 18.2), (12, 14), (14.7, 18.2)]))
    G["Add Circle"] = circle(12, 12, 9.5) + line(12, 7.6, 12, 16.4) + line(7.6, 12, 16.4, 12)
    G["Add"] = line(12, 4.5, 12, 19.5) + line(4.5, 12, 19.5, 12)
    G["Alarm"] = (circle(12, 13, 7.6) + poly([(12, 9.2), (12, 13), (14.8, 14.6)])
                  + arc(12, 13, 10.6, -62, -30) + arc(12, 13, 10.6, 30, 62)
                  + line(7, 19.4, 5.4, 21.2) + line(17, 19.4, 18.6, 21.2))
    G["Apps"] = (rect(3.5, 3.5, 10.5, 10.5, 2) + rect(13.5, 3.5, 20.5, 10.5, 2)
                 + rect(3.5, 13.5, 10.5, 20.5, 2) + rect(13.5, 13.5, 20.5, 20.5, 2))
    G["Arrow Down"] = poly([(6, 9), (12, 15), (18, 9)])
    G["Arrow Top"] = poly([(6, 15), (12, 9), (18, 15)])
    G["Arrow Left"] = poly([(15, 6), (9, 12), (15, 18)])
    G["Arrow Right"] = poly([(9, 6), (15, 12), (9, 18)])
    keys = "".join(dot(x, y, 1.15) for y in (13.6, 17.6) for x in (8.7, 12, 15.3))
    G["Calculator"] = rect(4.8, 2.5, 19.2, 21.5, 2.6) + rect(8, 5.8, 16, 9.4, 0.8) + keys
    G["Cannot Reach"] = ('<g transform="rotate(-45 12 12)">' + rect(1.8, 8.8, 10.2, 15.2, 3.2)
                         + rect(13.8, 8.8, 22.2, 15.2, 3.2) + line(12, 4.4, 12, 6.4) + line(12, 17.6, 12, 19.6)
                         + "</g>" + line(5.2, 9.3, 3.6, 8.6) + line(18.8, 14.7, 20.4, 15.4))
    G["Chat Image"] = (rect(2.5, 6.5, 16.5, 20.5, 2.2) + photo_scene(2.5, 6.5, 16.5, 20.5, (11.5, 10.6))
                       + line(19.5, 2.5, 19.5, 7.5) + line(17, 5, 22, 5))
    G["Chat Voice"] = mic()
    bubble = "M8 17H16a5 5 0 0 0 5-5V9a5 5 0 0 0-5-5H8a5 5 0 0 0-5 5v3a5 5 0 0 0 4 4.9V21Z"
    G["Chat"] = path(bubble)
    G["Check Circle"] = circle(12, 12, 9.5) + poly([(8, 12.4), (10.8, 15.1), (16, 9.6)])
    G["Check"] = poly([(4.5, 12.6), (9.5, 17.5), (19.5, 7)])
    G["Clock"] = circle(12, 12, 9.5) + poly([(12, 6.8), (12, 12), (15.6, 14)])
    G["Close Circle"] = circle(12, 12, 9.5) + line(9, 9, 15, 15) + line(15, 9, 9, 15)
    G["Close"] = line(6, 6, 18, 18) + line(18, 6, 6, 18)
    G["Commander Input"] = poly([(5, 7), (10, 12), (5, 17)]) + line(12.5, 17, 19, 17)
    G["Commander"] = rect(2.5, 4, 21.5, 20, 2.5) + poly([(6.6, 9), (9.6, 12), (6.6, 15)]) + line(12, 15, 16.6, 15)
    G["Compass"] = (circle(12, 12, 9.5) + poly([(12, 5.6), (14.4, 12), (9.6, 12)], closed=True, fill=True)
                    + poly([(14.4, 12), (12, 18.4), (9.6, 12)], closed=True))
    G["Customization"] = ('<ellipse cx="12" cy="12" rx="9.6" ry="8.4"/>' + circle(15.6, 15.4, 1.9)
                          + dot(7.4, 10.4, 1.45) + dot(10.6, 7.2, 1.45) + dot(15, 7.8, 1.45) + dot(7.8, 14.6, 1.45))
    G["Delete Back"] = (path("M8.5 5H19.5a1.5 1.5 0 0 1 1.5 1.5v11a1.5 1.5 0 0 1-1.5 1.5H8.5L2.5 12Z")
                        + line(11.6, 9.4, 16.6, 14.4) + line(16.6, 9.4, 11.6, 14.4))
    G["Delete"] = (line(3.5, 6.5, 20.5, 6.5) + path("M9 6.5V4.2h6v2.3")
                   + path("M5.6 6.5l1.1 13.1a1.8 1.8 0 0 0 1.8 1.6h7a1.8 1.8 0 0 0 1.8-1.6l1.1-13.1")
                   + line(10, 10.5, 10.3, 17) + line(14, 10.5, 13.7, 17))
    G["Desktop Order"] = (line(3, 6, 13.5, 6) + line(3, 12, 10.5, 12) + line(3, 18, 7.5, 18)
                          + line(18, 4.5, 18, 19) + poly([(14.8, 15.8), (18, 19), (21.2, 15.8)]))
    G["Download"] = (line(12, 3.5, 12, 15) + poly([(7.6, 10.6), (12, 15), (16.4, 10.6)])
                     + path("M3.5 15.5v3a2 2 0 0 0 2 2h13a2 2 0 0 0 2-2v-3"))
    G["Downloads"] = (path("M3 7a2 2 0 0 1 2-2h4l2 2.5h8a2 2 0 0 1 2 2V18a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2Z")
                      + line(12, 10.6, 12, 16.6) + poly([(9.4, 14), (12, 16.6), (14.6, 14)]))
    G["Enter"] = poly([(19.5, 4.5), (19.5, 13), (5, 13)]) + poly([(9, 9), (5, 13), (9, 17)])
    G["Error"] = (poly([pt(12, 12, 10, 22.5 + 45 * k) for k in range(8)], closed=True)
                  + line(12, 7, 12, 12.8) + dot(12, 16.4, 1.3))
    G["Eye"] = eye()
    G["Eye Off"] = masked(eye(), line(4, 4, 20, 20), 5.5) + line(4, 4, 20, 20)
    G["Fullscreen"] = (poly([(3.5, 8.5), (3.5, 3.5), (8.5, 3.5)]) + poly([(15.5, 3.5), (20.5, 3.5), (20.5, 8.5)])
                       + poly([(20.5, 15.5), (20.5, 20.5), (15.5, 20.5)]) + poly([(8.5, 20.5), (3.5, 20.5), (3.5, 15.5)]))
    G["Fullscreen Exit"] = (poly([(8.5, 3.5), (8.5, 8.5), (3.5, 8.5)]) + poly([(15.5, 3.5), (15.5, 8.5), (20.5, 8.5)])
                            + poly([(20.5, 15.5), (15.5, 15.5), (15.5, 20.5)]) + poly([(8.5, 20.5), (8.5, 15.5), (3.5, 15.5)]))
    G["Game Hub"] = (path("M7.2 6.5h9.6a4.8 4.8 0 0 1 4.7 3.9l1 5.4a2.9 2.9 0 0 1-5.1 2.4L15.4 16H8.6l-2 2.2"
                          "a2.9 2.9 0 0 1-5.1-2.4l1-5.4A4.8 4.8 0 0 1 7.2 6.5Z")
                     + line(7.6, 9.6, 7.6, 12.8) + line(6, 11.2, 9.2, 11.2) + dot(15.4, 10, 1.15) + dot(17.6, 12.3, 1.15))
    G["Hamburger Menu"] = line(4, 6, 20, 6) + line(4, 12, 20, 12) + line(4, 18, 20, 18)
    G["Heart"] = heart()
    G["Heart Filled"] = heart(True)
    G["Home"] = (poly([(2.8, 11.2), (12, 3.4), (21.2, 11.2)]) + path("M5.5 9.2V19.5a1 1 0 0 0 1 1h11a1 1 0 0 0 1-1V9.2")
                 + path("M10 20.5v-5h4v5"))
    G["Info"] = circle(12, 12, 9.5) + line(12, 11, 12, 16.6) + dot(12, 7.6, 1.3)
    G["Library"] = (rect(3, 4, 6.8, 20.5, 1) + rect(8.6, 4, 12.4, 20.5, 1) + line(3, 8, 6.8, 8) + line(8.6, 16.5, 12.4, 16.5)
                    + f'<g transform="rotate(18 14 20.5)">{rect(14, 5.5, 17.8, 20.5, 1)}</g>')
    G["Lock"] = rect(4.5, 10.5, 19.5, 21, 2.5) + path("M8 10.5V7.5a4 4 0 0 1 8 0v3") + line(12, 14.6, 12, 17)
    G["Lyrics"] = path(bubble) + dot(10.4, 13.2, 1.8) + line(12, 13.2, 12, 7.4) + path("M12 7.4c.8 1.1 2.6 1.4 3.4 2.6")
    G["Mail"] = rect(2.5, 5, 21.5, 19, 2.5) + poly([(3.2, 6.6), (12, 13), (20.8, 6.6)])
    front = path("M11 9.5h8a2.5 2.5 0 0 1 2.5 2.5v4.5a2.5 2.5 0 0 1-2.5 2.5h-.6l.8 2.6L15.4 19H11a2.5 2.5 0 0 1-2.5-2.5V12"
                 "a2.5 2.5 0 0 1 2.5-2.5Z")
    back = path("M5 3h8a2.5 2.5 0 0 1 2.5 2.5V10a2.5 2.5 0 0 1-2.5 2.5H8.2L4.6 15.2l.5-2.7A2.5 2.5 0 0 1 2.5 10V5.5"
                "A2.5 2.5 0 0 1 5 3Z")
    G["Messages"] = masked(back, rect(8.5, 9.5, 21.5, 19, 2.5, fill=True), 4.5) + front
    G["Messaging"] = (path("M8.2 18.4A9 7.6 0 1 0 5.1 16.4L3.2 20.8Z")
                      + dot(8, 11.5, 1.3) + dot(12, 11.5, 1.3) + dot(16, 11.5, 1.3))
    G["Minus"] = line(5, 12, 19, 12)
    G["Mods"] = path("M4 7H8.3a2.4 2.4 0 1 1 4.4 0H17V11.3a2.4 2.4 0 1 1 0 4.4V20H4Z")
    G["More"] = dot(5, 12, 1.7) + dot(12, 12, 1.7) + dot(19, 12, 1.7)
    G["Music Player"] = (rect(5.5, 2.5, 18.5, 21.5, 3) + rect(8, 5, 16, 10, 1) + circle(12, 15.6, 3.2)
                         + dot(12, 15.6, 0.9))
    G["Music"] = circle(8, 17.6, 3) + line(11, 17.6, 11, 3.4) + path("M11 3.4c.4 2.6 3.6 3.6 5.4 5 1.4 1.1 1.6 2.6 1.1 4")
    G["Next"] = poly([(5.5, 5.5), (15.5, 12), (5.5, 18.5)], closed=True) + line(19, 5, 19, 19)
    G["Previous"] = poly([(18.5, 5.5), (8.5, 12), (18.5, 18.5)], closed=True) + line(5, 5, 5, 19)
    G["No Connection"] = masked(wifi(3), line(3.5, 3.5, 20.5, 20.5), 5.5) + line(3.5, 3.5, 20.5, 20.5)
    page = "M6 2.5h8l5 5V19.5a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V4.5a2 2 0 0 1 2-2Z"
    G["Note Add"] = path(page) + poly([(14, 2.5), (14, 7.5), (19, 7.5)]) + line(11.5, 11, 11.5, 17) + line(8.5, 14, 14.5, 14)
    G["Note"] = (path("M5 3.5h14a1.5 1.5 0 0 1 1.5 1.5V14.5L14.5 20.5H5a1.5 1.5 0 0 1-1.5-1.5V5A1.5 1.5 0 0 1 5 3.5Z")
                 + poly([(20.5, 14.5), (14.5, 14.5), (14.5, 20.5)]) + line(7.2, 8, 16.5, 8) + line(7.2, 11.6, 12.8, 11.6))
    G["Notepad"] = (rect(5.5, 3, 19.5, 21, 2) + line(3.2, 7.5, 7.6, 7.5) + line(3.2, 12, 7.6, 12) + line(3.2, 16.5, 7.6, 16.5)
                    + line(10.6, 8.5, 16.2, 8.5) + line(10.6, 12, 16.2, 12))
    G["Notification"] = bell()
    G["Now Playing"] = (line(4.5, 20, 4.5, 14) + line(9.5, 20, 9.5, 5) + line(14.5, 20, 14.5, 10.5)
                        + line(19.5, 20, 19.5, 16))
    G["Options"] = (line(3, 6, 12.8, 6) + circle(15, 6, 2.2) + line(17.2, 6, 21, 6)
                    + line(3, 12, 5.8, 12) + circle(8, 12, 2.2) + line(10.2, 12, 21, 12)
                    + line(3, 18, 10.8, 18) + circle(13, 18, 2.2) + line(15.2, 18, 21, 18))
    G["Pause"] = rect(5.5, 4, 10, 20, 1.5) + rect(14, 4, 18.5, 20, 1.5)
    G["Photo Gallery"] = (rect(7, 7, 21.5, 20.5, 2) + photo_scene(7, 7, 21.5, 20.5, (16.8, 10.8))
                          + path("M3.5 16.5V5.5a2 2 0 0 1 2-2H17"))
    G["Photo"] = rect(3, 3.5, 21, 20.5, 2.5) + photo_scene(3, 3.5, 21, 20.5, (15.6, 8.6))
    G["Pin"] = pin()
    G["Pin Filled"] = pin(True)
    G["Play"] = play()
    G["Play Filled"] = play(True)
    G["Playlist"] = (line(3, 6, 15, 6) + line(3, 11, 15, 11) + line(3, 16, 10, 16)
                     + circle(15.8, 18.2, 2.4) + line(18.2, 18.2, 18.2, 7.5) + line(18.2, 7.5, 21, 8.8))
    G["Power"] = arc(12, 13, 8.2, 35, 325) + line(12, 3, 12, 11)
    G["Refresh"] = refresh()
    G["Repeat"] = (path("M13 6.5h3.5a4 4 0 0 1 4 4v3a4 4 0 0 1-4 4h-9a4 4 0 0 1-4-4v-3a4 4 0 0 1 4-4H9.6")
                   + poly([(7.4, 3.9), (10, 6.5), (7.4, 9.1)]))
    rx, ry = pt(12, 12, 9.5, 270)
    G["Rotate Lock"] = (arc(12, 12, 9.5, -30, 270) + arrowhead(rx, ry, 270 + 90 - 8)
                        + rect(8.4, 11, 15.6, 16.8, 1.2) + path("M10 11V9.6a2 2 0 0 1 4 0V11"))
    G["Seek"] = poly([(3, 6), (11, 12), (3, 18)], closed=True) + poly([(12.5, 6), (20.5, 12), (12.5, 18)], closed=True)
    G["Send"] = poly([(3.2, 4), (21, 12), (3.2, 20), (6.6, 12)], closed=True) + line(6.6, 12, 12, 12)
    G["Settings"] = poly(gear_pts(12, 12, 10, 7.4), closed=True) + circle(12, 12, 3)
    G["Shopping Bag"] = (path("M5.2 7.5h13.6l-.9 12a1.6 1.6 0 0 1-1.6 1.5H7.7a1.6 1.6 0 0 1-1.6-1.5Z")
                         + path("M9 10.5V6.6a3 3 0 0 1 6 0v3.9"))
    G["Shuffle"] = (path("M3 7h2.6c5.2 0 7.3 10 12.4 10h2.4") + path("M3 17h2.6c2 0 3.5-1.5 4.6-3.4")
                    + path("M13.8 10.4C15 8.5 16.4 7 18 7h2.4")
                    + poly([(18, 4.2), (20.8, 7), (18, 9.8)]) + poly([(18, 14.2), (20.8, 17), (18, 19.8)]))
    G["Signal Best"] = wifi(3)
    G["Signal Strong"] = wifi(2)
    G["Signal Normal"] = wifi(1)
    G["Signal Weak"] = wifi(0)
    G["Wi-Fi"] = wifi(3)
    G["Spam"] = (path("M12 2.8L19.5 5.6V11c0 4.8-3.2 8.2-7.5 10C7.7 19.2 4.5 15.8 4.5 11V5.6Z")
                 + line(12, 8, 12, 12.4) + dot(12, 15.7, 1.25))
    G["Star"] = star()
    G["Star Filled"] = star(True)
    G["Theme"] = theme()
    G["Timer"] = (circle(12, 13.6, 8) + line(10, 2.6, 14, 2.6) + line(12, 2.6, 12, 5.6) + line(12, 13.6, 14.8, 10.6)
                  + line(18.4, 6.6, 19.8, 5.2))
    G["User"] = person()
    G["Video Clip"] = (rect(3, 9.5, 21, 20.5, 2) + rect(3, 4.5, 21, 9.5, 1)
                       + line(8.6, 4.5, 6.6, 9.5) + line(13.6, 4.5, 11.6, 9.5) + line(18.6, 4.5, 16.6, 9.5))
    G["Video Player"] = (rect(2.5, 4, 21.5, 17, 2) + line(8, 20.5, 16, 20.5)
                         + poly([(10, 7.8), (15, 10.5), (10, 13.2)], closed=True, fill=True))
    G["Visualizer"] = "".join(line(x, 12 - h / 2, x, 12 + h / 2)
                              for x, h in zip((3, 6, 9, 12, 15, 18, 21), (2, 7, 12, 17, 9, 13, 4)))
    G["Volume"] = speaker() + arc(12, 12, 3.8, 45, 135) + arc(12, 12, 7.6, 45, 135)
    G["Volume Low"] = speaker() + arc(12, 12, 3.8, 45, 135)
    G["Volume Mute"] = speaker() + line(15.6, 9.4, 20.8, 14.6) + line(20.8, 9.4, 15.6, 14.6)
    lat = 9.5 * math.sqrt(1 - (3.6 / 9.5) ** 2)
    G["Web Browser"] = (circle(12, 12, 9.5) + '<ellipse cx="12" cy="12" rx="4.3" ry="9.5"/>'
                        + line(12 - lat, 8.4, 12 + lat, 8.4) + line(12 - lat, 15.6, 12 + lat, 15.6))
    G["Widgets"] = (rect(3, 3, 11, 13, 2) + rect(13, 3, 21, 9, 2) + rect(13, 11, 21, 21, 2) + rect(3, 15, 11, 21, 2))

    # ---- Others/Settings Elements (prefix "SE:")
    cal = (rect(3, 4.5, 17.5, 18.5, 2) + line(7, 2.5, 7, 6.2) + line(13.5, 2.5, 13.5, 6.2) + line(3, 9, 17.5, 9))
    G["SE:Date & Time"] = (masked(cal, circle(17, 17, 5.2, fill=True), 2) + circle(17, 17, 4.6)
                           + poly([(17, 14.8), (17, 17), (18.6, 18)]))
    G["SE:Language"] = (line(7, 2.6, 7, 4.4) + line(3, 6, 11, 6) + line(4.6, 7.8, 10, 14.2) + line(9.4, 7.8, 4, 14.2)
                        + poly([(12.8, 21), (16.6, 10.6), (20.4, 21)]) + line(14.1, 17.4, 19.1, 17.4))
    G["SE:Microphone"] = mic()
    G["SE:Notification"] = bell()
    G["SE:Reset Password"] = (circle(7.6, 16.4, 4.2) + line(10.6, 13.4, 20.4, 3.6) + line(17.4, 6.6, 19.8, 9)
                              + line(14.8, 9.2, 16.6, 11))
    G["SE:Save Desktop Layout"] = (path("M5 3.5h11l4.5 4.5V19a1.5 1.5 0 0 1-1.5 1.5H5A1.5 1.5 0 0 1 3.5 19V5A1.5 1.5 0 0 1 5 3.5Z")
                                   + path("M8 3.5V8h6.5V3.5") + path("M7 20.5v-6.6h10v6.6"))
    G["SE:Snap Desktop Items"] = (rect(2.4, 2.4, 14.6, 14.6, 2.6)
                                  + "".join(dot(x, y, 1.25) for x in (5.6, 11.4, 18.6) for y in (5.6, 11.4, 18.6)))
    G["SE:Support"] = (circle(12, 12, 9.5) + path("M9.2 9.4a2.9 2.9 0 1 1 3.9 2.7c-.7.3-1.1.9-1.1 1.6v.7")
                       + dot(12, 17.2, 1.25))
    G["SE:Theme"] = theme()
    G["SE:Update"] = refresh()
    G["SE:User"] = person()
    G["SE:Wallpaper"] = (rect(2.5, 3.5, 21.5, 16.5, 2) + photo_scene(2.5, 3.5, 21.5, 16.5, (16.4, 7.4))
                         + line(12, 16.5, 12, 20.5) + line(8, 20.5, 16, 20.5))
    G["SE:Wipe Data"] = ('<g transform="rotate(-45 12 11)">' + rect(4, 7, 20, 15, 2) + line(10.4, 7, 10.4, 15) + "</g>"
                         + line(12.5, 21.5, 20.5, 21.5))
    return G


# --------------------------------------------------------------------------- render
def svg(inner):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{PX}" height="{PX}" viewBox="0 0 24 24" fill="none" '
            f'stroke="#fff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" '
            f'style="display:block;float:left">{inner}</svg>')


def render_all(G):
    names = list(G)
    rows = (len(names) + COLS - 1) // COLS
    W, H = COLS * PX, rows * PX
    body = "".join(svg(G[n]) for n in names)
    with tempfile.TemporaryDirectory() as tmp:
        html = os.path.join(tmp, "glyphs.html")
        out = os.path.join(tmp, "glyphs.png")
        with open(html, "w") as fh:
            fh.write(f'<html><body style="margin:0;background:transparent;width:{W}px">{body}</body></html>')
        subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=1",
                        "--default-background-color=00000000", f"--window-size={W},{H}",
                        f"--screenshot={out}", "file://" + html],
                       check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        sheet = np.array(Image.open(out).convert("RGBA"))
    res = {}
    for i, n in enumerate(names):
        r, c = divmod(i, COLS)
        a = sheet[r * PX:(r + 1) * PX, c * PX:(c + 1) * PX, 3]
        img = np.empty((PX, PX, 4), np.uint8)
        img[..., :3] = 255
        img[..., 3] = a
        res[n] = Image.fromarray(img, "RGBA")
    return res


def targets(name):
    if name.startswith("SE:"):
        p = os.path.join(ICONS, SETTINGS_DIR, name[3:] + ".png")
        return [p] if os.path.exists(p) else []
    return [p for p in (os.path.join(ICONS, d, name + ".png") for d in SIZE_DIRS) if os.path.exists(p)]


def contact_sheet(imgs, path_out):
    cols, cell = 12, 96
    names = list(imgs)
    rows = (len(names) + cols - 1) // cols
    S = Image.new("RGBA", (cols * cell, rows * (cell + 14)), (40, 58, 92, 255))
    d = ImageDraw.Draw(S)
    for i, n in enumerate(names):
        r, c = divmod(i, cols)
        x, y = c * cell, r * (cell + 14)
        S.alpha_composite(imgs[n].resize((64, 64), Image.LANCZOS), (x + 16, y + 6))
        S.alpha_composite(imgs[n].resize((32, 32), Image.LANCZOS), (x + 60, y + 60))
        d.text((x + 3, y + cell - 4), n.replace("SE:", "*")[:15], fill=(200, 210, 230, 255))
    S.save(path_out)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out")
    ap.add_argument("--sheet")
    a = ap.parse_args()
    imgs = render_all(glyphs())
    written = 0
    for n, im in imgs.items():
        if a.out:
            os.makedirs(a.out, exist_ok=True)
            im.save(os.path.join(a.out, n.replace("SE:", "SE_") + ".png"), optimize=True)
            continue
        ts = targets(n)
        if not ts:
            print("no existing file for", n)
        for p in ts:
            im.save(p, optimize=True)
            written += 1
    if a.sheet:
        contact_sheet(imgs, a.sheet)
    print("glyphs:", len(imgs), "files written:", written)


if __name__ == "__main__":
    main()
