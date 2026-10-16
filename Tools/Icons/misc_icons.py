"""灵光 icons, group: YY toolbar, data-shop rows, Game Hub tiles and the 888 casino (chips, slot symbols, card suits).

Run:  python3 Tools/Icons/misc_icons.py            (all)
      python3 Tools/Icons/misc_icons.py yy hub     (only some groups)
Writes 128 px PNGs to Assets/LingGuangV05/Resources/LingGuangV05/Icons/<group>/<id>.png and contact sheets to
the scratchpad (see SHEET_DIR).
"""
import math, os, sys
from multiprocessing import Pool

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from iconkit import *  # noqa: F401,F403
import iconkit

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "LingGuangV05", "Resources", "LingGuangV05", "Icons")
SHEET_DIR = "/private/tmp/claude-501/-Users-kk-unity/0b11848a-3722-4bc3-8ee4-9222e5441068/scratchpad"

# extra hues (light, base, dark): casino gold/burgundy palette and a few materials
PAL["bur"] = ((196, 70, 96), (128, 24, 52), (66, 8, 26))
PAL["casinored"] = ((246, 96, 92), (204, 30, 46), (120, 10, 24))
PAL["felt"] = ((70, 180, 120), (20, 128, 70), (8, 70, 40))
PAL["chipblue"] = ((120, 160, 240), (44, 84, 200), (16, 36, 110))
PAL["wood"] = ((246, 212, 152), (214, 164, 92), (130, 88, 40))
PAL["ivory"] = ((255, 253, 246), (240, 232, 214), (186, 172, 146))
PAL["night"] = ((90, 84, 104), (40, 36, 50), (14, 12, 20))
IVORY = (250, 246, 236)
GOLD_D = hue("gold")[2]


def sphere(ic, cx, cy, r, light, dark, rim=None):
    m = m_circle(cx, cy, r)
    ic.shade(m, .28, 0, r * .12, r * .14)
    ic.paint(m, rgrad(light, dark, cx - r * .32, cy - r * .38, r * 1.5))
    if rim:
        ic.rim(m, rim, 1.6, .6)
    ic.glow(m_circle(cx - r * .34, cy - r * .4, r * .22), (255, 255, 255), 2.5, .75)
    return m


def outlined(ic, mask, color, outline_color=None, width=5):
    """A sticker-like dark outline under a glossy body (reads on the light reel / panel backgrounds)."""
    ic.paint(grow(mask, width), outline_color or hue(color)[2])
    ic.body(mask, color)


# ───────────────────────────── YY toolbar ─────────────────────────────

def yy_emoji():
    ic = Icon()
    d = m_circle(128, 128, 108)
    ic.glow(d, hue("yellow")[0], 8, .5)
    ic.body(d, "yellow")
    ic.paint(m_ellipse(66, 150, 100, 176), (255, 120, 120), .45)
    ic.paint(m_ellipse(156, 150, 190, 176), (255, 120, 120), .45)
    for x in (92, 164):
        ic.paint(m_ellipse(x - 13, 82, x + 13, 128), (60, 36, 10))
        ic.paint(m_ellipse(x - 6, 88, x + 3, 100), (255, 255, 255), .85)
    ic.paint(m_arc(128, 118, 66, 25, 155, 13), (120, 60, 10))
    return ic


def yy_file():
    ic = Icon()
    folder(ic, "yellow", 18, 44, 238, 222)
    badge(ic, "up", 198, 188, 42)
    return ic


def yy_shot():
    ic = Icon()
    # dashed selection frame
    x0, y0, x1, y1 = 26, 30, 230, 176
    frame = blank()
    dash, gap = 18, 12
    for (ax, ay, bx, by) in ((x0, y0, x1, y0), (x1, y0, x1, y1), (x1, y1, x0, y1), (x0, y1, x0, y0)):
        L = math.hypot(bx - ax, by - ay); t = 0
        while t < L:
            t2 = min(L, t + dash)
            frame = union(frame, m_line([(ax + (bx - ax) * t / L, ay + (by - ay) * t / L), (ax + (bx - ax) * t2 / L, ay + (by - ay) * t2 / L)], 7, False))
            t += dash + gap
    ic.paint(m_rect(x0, y0, x1, y1), (170, 215, 255), .38)
    ic.paint(frame, hue("blue")[1])
    for (hx, hy) in ((x0, y0), (x1, y0), (x1, y1), (x0, y1)):
        ic.body(m_rect(hx - 9, hy - 9, hx + 9, hy + 9), "blue")
    # scissors
    blade_a = m_line([(70, 66), (176, 180)], 24)
    blade_b = m_line([(186, 66), (80, 180)], 24)
    ic.shade(union(blade_a, blade_b), .3, 0, 4, 3)
    ic.body(blade_a, "steel"); ic.body(blade_b, "steel")
    for cx in (66, 190):
        ring = cut(m_circle(cx, 208, 38), m_circle(cx, 208, 19))
        ic.body(ring, "casinored")
    ic.body(m_circle(128, 126, 10), "gray")
    return ic


def yy_shake():
    ic = Icon()
    w = Icon()
    body = m_rrect(54, 44, 202, 212, 18)
    w.shade(body, .25, 0, 5, 4)
    w.paint(body, vgrad((255, 255, 255), (222, 230, 242), 58, 198))
    w.rim(body, (110, 130, 160), 2.2, .8)
    bar = m_rrect(54, 44, 202, 92, 18)
    w.body(bar, "blue", rim=False)
    for k, c in enumerate(("red", "yellow", "green")):
        w.body(m_circle(78 + k * 18, 68, 7), c, rim=False, gloss=False)
    w.paint(m_line([(76, 124), (180, 124)], 10), (150, 168, 196))
    w.paint(m_line([(76, 152), (150, 152)], 10), (150, 168, 196))
    w.paint(m_line([(76, 180), (168, 180)], 10), (150, 168, 196))
    w.im = w.im.rotate(-9, resample=Image.BICUBIC, center=(N // 2, N // 2))
    # vibration arcs
    for (r, wd) in ((112, 12), (130, 10)):
        ic.paint(m_arc(128, 128, r, 160, 200, wd), hue("orange")[1])
        ic.paint(m_arc(128, 128, r, -20, 20, wd), hue("orange")[1])
    ic.im.alpha_composite(w.im)
    return ic


def yy_history():
    ic = Icon()
    # circular arrow
    arc = m_arc(128, 128, 104, 120, 395, 15)
    ic.paint(arc, hue("green")[1])
    a = math.radians(395 % 360)  # arrow head at the end of the arc
    ex, ey = 128 + 104 * math.cos(a), 128 + 104 * math.sin(a)
    tx, ty = -math.sin(a), math.cos(a)
    nx, ny = math.cos(a), math.sin(a)
    head = m_poly([(ex + tx * 32, ey + ty * 32), (ex + nx * 24 - tx * 4, ey + ny * 24 - ty * 4), (ex - nx * 24 - tx * 4, ey - ny * 24 - ty * 4)])
    ic.paint(head, hue("green")[1])
    face = m_circle(128, 128, 80)
    ic.shade(face, .25, 0, 4, 4)
    ic.body(face, "blue", gloss=False)
    inner = m_circle(128, 128, 66)
    ic.paint(inner, vgrad((255, 255, 255), (226, 234, 246), 62, 194))
    for k in range(12):
        a = math.radians(k * 30)
        r0, r1 = (52 if k % 3 == 0 else 57), 62
        ic.paint(m_line([(128 + r0 * math.sin(a), 128 - r0 * math.cos(a)), (128 + r1 * math.sin(a), 128 - r1 * math.cos(a))], 4 if k % 3 == 0 else 2.6, False), (110, 124, 150))
    ic.paint(m_line([(128, 128), (128, 84)], 8), (50, 60, 80))
    ic.paint(m_line([(128, 128), (160, 148)], 8), (50, 60, 80))
    ic.paint(m_circle(128, 128, 8), (220, 60, 60))
    return ic


# ───────────────────────────── data shop ─────────────────────────────

def data_auto():
    ic = Icon()
    ic.paint(m_line([(128, 66), (128, 34)], 8), hue("steel")[1])
    ic.glow(m_circle(128, 28, 14), hue("orange")[0], 8, .8)
    ic.body(m_circle(128, 28, 14), "orange")
    for x0, x1 in ((22, 46), (210, 234)):
        ic.body(m_rrect(x0, 112, x1, 164, 10), "steel")
    head = m_rrect(40, 64, 216, 206, 44)
    ic.body(head, "teal")
    visor = m_rrect(62, 100, 194, 164, 26)
    ic.paint(visor, vgrad((40, 60, 80), (12, 22, 34), 100, 164))
    ic.rim(visor, (140, 230, 215), 1.8, .5)
    for x in (100, 156):
        ic.glow(m_circle(x, 132, 15), hue("lime")[0], 8, .9)
        ic.paint(m_circle(x, 132, 14), vgrad((230, 255, 150), (110, 200, 40), 118, 146))
    for k in range(4):
        ic.paint(m_line([(94 + k * 24, 184), (94 + k * 24, 196)], 6, False), hue("teal")[2], .85)
    badge(ic, "check", 204, 204, 42)
    return ic


def data_auto_hidden():
    ic = Icon()
    t = m_rrect(30, 30, 226, 226, 52)
    ic.body(t, "steel")
    ic.paint(m_text("?", "black", 190, 128, 132), (28, 34, 46), .5)
    ic.paint(m_text("?", "black", 190, 128, 126), (255, 255, 255))
    return ic


def data_pack():
    ic = Icon()
    for yt in (128, 80, 32):
        side = union(m_rect(52, yt + 22, 204, yt + 46), m_ellipse(52, yt + 26, 204, yt + 72))
        top = m_ellipse(52, yt, 204, yt + 46)
        ic.shade(side, .25, 0, 4, 4)
        ic.paint(side, lin(hue("teal")[0], hue("teal")[2], 52, 0, 204, 0))
        ic.rim(side, hue("teal")[2], 2, .5)
        ic.paint(top, vgrad(hue("teal")[0], hue("sky")[1], yt, yt + 46))
        ic.rim(top, hue("teal")[2], 1.8, .5)
        for k in range(3):
            ic.paint(m_circle(176 - k * 18, yt + 62, 5), hue("lime")[0] if k == 0 else (240, 255, 250), .95)
    ic.glow(m_ellipse(84, 40, 172, 70), (255, 255, 255), 5, .4)
    return ic


def data_junk():
    ic = Icon()
    # stuff peeking out of the bag
    ic.body(m_star(92, 62, 32, 14), "yellow")
    ic.body(m_circle(142, 56, 24), "pink")
    ic.body(m_rrect(150, 40, 196, 84, 6), "sky")
    bag = m_poly([(52, 84), (204, 84), (216, 224), (40, 224)])
    ic.shade(bag, .3, 0, 5, 4)
    ic.body(bag, "brown")
    for k in range(4):
        ic.paint(m_line([(60 + k * 36, 98), (50 + k * 40, 218)], 3), hue("brown")[2], .25)
    fold = m_rrect(44, 72, 212, 104, 8)
    ic.body(fold, "brown", top=hue("brown")[0], bottom=hue("brown")[1])
    for k in range(5):
        ic.paint(m_line([(58 + k * 36, 80), (58 + k * 36, 96)], 4, False), hue("brown")[2], .5)
    tag = m_circle(176, 176, 42)
    ic.paint(grow(tag, 4), WHITE)
    ic.body(tag, "casinored")
    ic.paint(m_text("¥", "black", 52, 176, 178), WHITE)
    return ic


def person(ic, cx, head_y, s, color, floor=222):
    head = m_circle(cx, head_y, 30 * s)
    body = intersect(m_ellipse(cx - 54 * s, head_y + 38 * s, cx + 54 * s, head_y + 38 * s + 130 * s), m_rect(0, 0, 256, floor))
    ic.shade(body, .25, 0, 3, 3)
    ic.body(body, color)
    ic.shade(head, .25, 0, 3, 3)
    ic.body(head, color)


def data_crowd():
    ic = Icon()
    person(ic, 62, 96, .8, "blue")
    person(ic, 194, 96, .8, "orange")
    person(ic, 128, 84, 1.0, "green")
    return ic


def data_story():
    ic = Icon()
    board = m_rrect(34, 104, 222, 222, 14)
    ic.shade(board, .3, 0, 5, 4)
    ic.body(board, "dark")
    for k, c in enumerate(((255, 200, 70), (120, 200, 255), (255, 120, 130))):
        ic.paint(m_line([(60, 146 + k * 24), (150 - k * 16, 146 + k * 24)], 8), c, .9)
    ic.paint(m_circle(192, 160, 16), (255, 255, 255), .85)
    ic.paint(m_poly([(186, 150), (186, 170), (203, 160)]), (40, 44, 54))
    st = Icon()
    stick = m_rrect(34, 50, 222, 98, 10)
    st.body(stick, "dark")
    stripes = blank()
    for k in range(0, 8):
        x = 26 + k * 40
        stripes = union(stripes, m_poly([(x, 98), (x + 22, 98), (x + 42, 50), (x + 20, 50)]))
    st.paint(intersect(stripes, grow(stick, -3)), (250, 250, 250), .95)
    st.gloss(stick, .3)
    st.im = st.im.rotate(14, resample=Image.BICUBIC, center=(u(40), u(100)))
    ic.im.alpha_composite(st.im)
    return ic


def data_crawl():
    ic = Icon()
    ic.paint(m_line([(128, 6), (128, 78)], 3.5), (150, 160, 180))
    legs = [
        [(112, 96), (66, 64), (30, 96), (24, 138)],
        [(108, 112), (52, 104), (22, 140), (24, 186)],
        [(108, 128), (56, 144), (36, 184), (44, 226)],
        [(114, 144), (72, 180), (66, 218), (80, 238)],
    ]
    lm = blank()
    for L in legs:
        lm = union(lm, m_line(L, 10))
        lm = union(lm, m_line([(256 - x, y) for x, y in L], 10))
    ic.shade(lm, .3, 0, 4, 3)
    ic.paint(lm, vgrad(hue("night")[1], hue("night")[0], 40, 240))
    abd = m_ellipse(78, 112, 178, 220)
    ic.body(abd, "night")
    ic.paint(m_poly([(128, 138), (110, 162), (128, 186), (146, 162)]), hue("casinored")[1], .95)
    head = m_circle(128, 98, 30)
    ic.body(head, "night")
    for x in (116, 140):
        ic.paint(m_circle(x, 94, 7), (255, 255, 255))
        ic.paint(m_circle(x, 95, 3.4), (200, 20, 30))
    return ic


# ───────────────────────────── game hub ─────────────────────────────

def hub_gomoku():
    ic = Icon()
    t = m_rrect(18, 18, 238, 238, 36)
    ic.body(t, "wood", gloss=False)
    ic.paint(grow(t, -10), vgrad((248, 218, 160), (222, 172, 100), 20, 236), .55)
    line = (96, 60, 24)
    for k in range(5):
        p = 52 + k * 38
        ic.paint(m_line([(p, 52), (p, 204)], 3, False), line, .85)
        ic.paint(m_line([(52, p), (204, p)], 3, False), line, .85)
    # five in a row, with a few white stones around
    blacks = [(52 + 38 * i, 204 - 38 * i) for i in range(5)]
    whites = [(90, 52), (166, 90), (90, 128), (166, 166), (52, 128)]
    for (x, y) in whites:
        sphere(ic, x, y, 16, (255, 255, 255), (196, 204, 218), (130, 140, 160))
    for (x, y) in blacks:
        sphere(ic, x, y, 16, (120, 124, 140), (10, 10, 16), (0, 0, 0))
    ic.gloss(t, .35, .4)
    return ic


def hub_go():
    ic = Icon()
    stones = [  # back (top) to front (bottom)
        (128, 38, 24, "w"),
        (96, 66, 27, "b"), (160, 66, 27, "w"),
        (66, 96, 28, "w"), (128, 92, 28, "b"), (190, 96, 28, "b"),
    ]
    for (x, y, r, c) in stones:
        if c == "b":
            sphere(ic, x, y, r, (126, 130, 146), (8, 8, 14), (0, 0, 0))
        else:
            sphere(ic, x, y, r, (255, 255, 255), (190, 198, 214), (130, 140, 160))
    bowl = intersect(m_circle(128, 118, 104), m_rect(0, 118, 256, 240))
    ic.shade(bowl, .3, 0, 5, 4)
    ic.body(bowl, "wood", top=(222, 168, 96), bottom=(110, 70, 30))
    lip = m_rrect(22, 108, 234, 134, 13)
    ic.body(lip, "wood", top=(250, 220, 160), bottom=(176, 124, 64))
    ic.paint(m_line([(44, 158), (212, 158)], 4, False), (110, 70, 30), .3)
    return ic


def hub_chess():
    ic = Icon()
    t = m_rrect(18, 18, 238, 238, 36)
    ic.body(t, "green", gloss=False)
    chk = blank()
    for r in range(4):
        for c in range(4):
            if (r + c) % 2 == 0:
                chk = union(chk, m_rect(18 + c * 55, 18 + r * 55, 18 + (c + 1) * 55, 18 + (r + 1) * 55))
    ic.paint(intersect(chk, t), (255, 255, 255), .2)
    ic.gloss(t, .35, .4)
    # the king
    king = union(
        m_rrect(66, 196, 190, 222, 10),
        m_rrect(80, 176, 176, 200, 8),
        m_poly([(92, 178), (164, 178), (148, 118), (108, 118)]),
        m_rrect(94, 104, 162, 126, 9),
        m_ellipse(92, 66, 164, 114),
    )
    cross = union(m_rect(119, 20, 137, 70), m_rect(104, 34, 152, 50))
    ic.shade(union(king, cross), .4, 4, 6, 4)
    ic.body(king, "ivory")
    ic.body(cross, "ivory")
    ic.paint(m_line([(100, 191), (156, 191)], 3), hue("ivory")[2], .6)
    return ic


def hub_home():
    ic = Icon()
    body = union(m_rrect(22, 80, 234, 178, 46), m_circle(70, 172, 40), m_circle(186, 172, 40))
    ic.shade(body, .3, 0, 6, 5)
    ic.body(body, "blue")
    ic.paint(m_rect(52, 112, 100, 126), WHITE); ic.paint(m_rect(69, 95, 83, 143), WHITE)
    ic.body(m_circle(170, 112, 13), "yellow"); ic.body(m_circle(198, 134, 13), "red")
    ic.body(m_circle(142, 134, 13), "green"); ic.body(m_circle(170, 156, 13), "orange")
    return ic


# ───────────────────────────── casino: chips ─────────────────────────────

def chip_face(value, color, size):
    ic = Icon()
    outer = m_circle(128, 128, 114)
    ic.shade(outer, .3, 0, 5, 3)
    ic.body(outer, color, gloss=False)
    stripes = blank()
    for k in range(8):
        stripes = union(stripes, rotate_mask(m_rrect(110, 14, 146, 50, 3), k * 45))
    ic.paint(intersect(stripes, outer), IVORY, .96)
    ic.paint(m_circle(128, 128, 82), IVORY, .96)
    inner = m_circle(128, 128, 74)
    ic.body(inner, color, gloss=False)
    ic.paint(cut(m_circle(128, 128, 66), m_circle(128, 128, 62)), (255, 235, 170), .8)
    ic.gloss(outer, .4)
    ic.paint(m_text(value, "black", size, 128, 134), (0, 0, 0), .45)
    ic.paint(m_text(value, "black", size, 128, 130), (255, 244, 214))
    return ic


def chip_allin():
    ic = Icon()
    colors = ["bur", "gold", "casinored", "dark"]
    ys = [172, 138, 104, 70]
    for y, c in zip(ys, colors):
        m = m_rrect(46, y, 210, y + 40, 20)
        ic.shade(m, .25, 0, 3, 3)
        ic.body(m, c, gloss=False)
        stripes = blank()
        for x in range(62, 200, 30):
            stripes = union(stripes, m_rect(x, y + 3, x + 13, y + 37))
        ic.paint(intersect(stripes, grow(m, -3)), IVORY, .93)
        ic.gloss(m, .35, .6)
    top = m_ellipse(46, 46, 210, 96)
    ic.body(top, "dark", gloss=False)
    ic.paint(cut(m_ellipse(66, 56, 190, 86), m_ellipse(72, 59, 184, 83)), (255, 220, 120), .9)
    ic.paint(m_ellipse(86, 62, 170, 80), vgrad((70, 64, 84), (30, 26, 38), 62, 80))
    ic.paint(m_star(128, 71, 15, 7), hue("gold")[0])
    return ic


# ───────────────────────────── casino: slot symbols ─────────────────────────────

def sym_7():
    ic = Icon()
    back = m_text("7", "black", 236, 128, 134, stroke=9)
    ic.paint(back, hue("gold")[2])
    ic.paint(m_text("7", "black", 236, 128, 134, stroke=5), vgrad((255, 235, 150), (235, 185, 70), 20, 240))
    seven = m_text("7", "black", 236, 128, 134)
    ic.body(seven, "casinored")
    return ic


def sym_bar():
    ic = Icon()
    plate = m_rrect(14, 76, 242, 180, 22)
    ic.shade(plate, .3, 0, 5, 3)
    ic.body(plate, "gold")
    face = m_rrect(26, 88, 230, 168, 14)
    ic.paint(face, vgrad((54, 50, 64), (14, 12, 20), 88, 168))
    ic.rim(face, (255, 228, 140), 2.2, .85)
    ic.paint(m_text("BAR", "black", 62, 128, 132), (0, 0, 0), .5)
    ic.paint(m_text("BAR", "black", 62, 128, 128), vgrad((255, 240, 160), (235, 180, 60), 98, 158))
    return ic


def sym_star():
    ic = Icon()
    m = m_star(128, 136, 114, 50)
    ic.shade(m, .3, 0, 6, 3)
    ic.paint(grow(m, 4), (140, 80, 14))
    ic.body(m, "gold", top=(255, 245, 160), bottom=(240, 170, 40))
    ic.paint(m_star(128, 136, 52, 24), (255, 250, 200), .5)
    return ic


def sym_bell():
    ic = Icon()
    body = union(m_circle(128, 96, 62), m_poly([(70, 108), (186, 108), (212, 176), (44, 176)]), m_rrect(38, 166, 218, 192, 13))
    ic.shade(body, .3, 0, 5, 3)
    ic.paint(grow(body, 4), (140, 80, 14))
    ic.body(body, "gold")
    ic.body(m_circle(128, 34, 14), "gold")
    ic.body(m_circle(128, 204, 20), "bur")
    ic.paint(m_line([(94, 76), (86, 138)], 9), (255, 255, 255), .45)
    ic.paint(m_line([(60, 178), (196, 178)], 3), hue("gold")[2], .5)
    return ic


def sym_melon():
    ic = Icon()
    cy, R = 66, 122
    rind = intersect(m_circle(128, cy, R), m_rect(0, cy, 256, 256))
    ic.shade(rind, .3, 0, 5, 3)
    ic.paint(grow(rind, 3), hue("green")[2])
    ic.paint(rind, vgrad((90, 200, 80), (24, 120, 40), cy, cy + R))
    ic.paint(intersect(m_circle(128, cy, R - 12), m_rect(0, cy, 256, 256)), (236, 248, 214))
    flesh = intersect(m_circle(128, cy, R - 24), m_rect(0, cy, 256, 256))
    ic.paint(flesh, vgrad((255, 110, 110), (214, 36, 56), cy, cy + R))
    ic.gloss(flesh, .3, .5)
    for (x, y, a) in ((80, 112, 20), (128, 132, -5), (176, 112, -20), (104, 156, 10), (152, 158, -12), (128, 100, 0)):
        seed = rotate_mask(m_ellipse(x - 5, y - 9, x + 5, y + 9), a, x, y)
        ic.paint(seed, (30, 22, 24))
    return ic


def sym_gem():
    ic = Icon()
    P = [(52, 94), (86, 46), (170, 46), (204, 94), (128, 214)]
    gem = m_poly(P)
    ic.shade(gem, .3, 0, 6, 3)
    ic.paint(grow(gem, 4), hue("chipblue")[2])
    ic.paint(gem, lin((120, 225, 255), (10, 90, 200), 60, 40, 200, 220))
    table = m_poly([(86, 46), (170, 46), (150, 94), (106, 94)])
    ic.paint(table, (240, 253, 255), .9)
    ic.paint(m_poly([(52, 94), (86, 46), (106, 94)]), (150, 215, 255), .75)
    ic.paint(m_poly([(204, 94), (170, 46), (150, 94)]), (70, 150, 240), .6)
    ic.paint(m_poly([(52, 94), (106, 94), (128, 214)]), (60, 140, 235), .5)
    ic.paint(m_poly([(106, 94), (150, 94), (128, 214)]), (210, 240, 255), .6)
    ic.paint(m_poly([(150, 94), (204, 94), (128, 214)]), (20, 70, 180), .45)
    for L in ([(52, 94), (204, 94)], [(106, 94), (128, 214)], [(150, 94), (128, 214)], [(86, 46), (106, 94)], [(170, 46), (150, 94)]):
        ic.paint(m_line(L, 2.4), (255, 255, 255), .7)
    ic.rim(gem, (10, 50, 140), 2, .7)
    ic.glow(m_circle(100, 70, 9), (255, 255, 255), 3, .9)
    return ic


# ───────────────────────────── casino: suits ─────────────────────────────

def suit_heart():
    ic = Icon()
    ic.body(m_heart(128, 112, 214), "casinored")
    return ic


def suit_diamond():
    ic = Icon()
    ic.body(m_poly([(128, 14), (212, 128), (128, 242), (44, 128)]), "casinored")
    return ic


def suit_spade():
    ic = Icon()
    top = m_heart(128, 144, 206).transpose(Image.FLIP_TOP_BOTTOM)
    m = union(top, m_poly([(128, 150), (92, 238), (164, 238)]))
    ic.body(m, "night")
    return ic


def suit_club():
    ic = Icon()
    m = union(m_circle(128, 66, 48), m_circle(70, 142, 48), m_circle(186, 142, 48), m_circle(128, 124, 30),
              m_poly([(128, 130), (96, 232), (160, 232)]))
    ic.body(m, "night")
    return ic


# ───────────────────────────── registry ─────────────────────────────

DRAW = {
    "yy": {"emoji": yy_emoji, "file": yy_file, "shot": yy_shot, "shake": yy_shake, "history": yy_history},
    "data": {"auto": data_auto, "auto_hidden": data_auto_hidden, "pack": data_pack, "junk": data_junk,
             "crowd": data_crowd, "story": data_story, "crawl": data_crawl},
    "hub": {"gomoku": hub_gomoku, "go": hub_go, "chess": hub_chess, "home": hub_home},
    "casino": {
        "chip_10": lambda: chip_face("10", "chipblue", 84),
        "chip_50": lambda: chip_face("50", "felt", 84),
        "chip_100": lambda: chip_face("100", "casinored", 66),
        "chip_500": lambda: chip_face("500", "night", 66),
        "chip_allin": chip_allin,
        "sym_7": sym_7, "sym_bar": sym_bar, "sym_star": sym_star, "sym_bell": sym_bell, "sym_melon": sym_melon, "sym_gem": sym_gem,
        "suit_spade": suit_spade, "suit_heart": suit_heart, "suit_club": suit_club, "suit_diamond": suit_diamond,
    },
}


def render(job):
    group, name = job
    path = os.path.join(OUT, group, name + ".png")
    DRAW[group][name]().save(path, 128)
    return path


def main(groups):
    jobs = [(g, n) for g in groups for n in DRAW[g]]
    with Pool(min(8, len(jobs))) as p:
        for path in p.imap_unordered(render, jobs):
            print(path)
    for g in groups:
        folder_path = os.path.join(OUT, g)
        iconkit.sheet(folder_path, os.path.join(SHEET_DIR, "sheet_%s.png" % g), cell=140, cols=8)
        iconkit.sheet(folder_path, os.path.join(SHEET_DIR, "sheet_%s_40.png" % g), cell=56, cols=16)


if __name__ == "__main__":
    main([g for g in sys.argv[1:] if g in DRAW] or list(DRAW))
