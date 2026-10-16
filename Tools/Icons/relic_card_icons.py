"""灵光 icons, groups "relic" (24 rare items, realistic objects with a rarity glow) and "card" (achievement medals).

    python3 relic_card_icons.py                 # everything (multiprocessing)
    python3 relic_card_icons.py relic           # one group
    python3 relic_card_icons.py relic redbull   # some ids
Writes Assets/LingGuangV05/Resources/LingGuangV05/Icons/<group>/<id with . / space -> _>.png at 128 px.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from iconkit import *
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "LingGuangV05", "Resources", "LingGuangV05", "Icons")
SIZE = 128


def fname(i):
    return i.replace(".", "_").replace("/", "_").replace(" ", "_")


# ── helpers ───────────────────────────────────────────────────────────────────

def shd(ic, m, a=.3, dy=4, blur=3):
    ic.shade(m, a, 0, dy, blur)


def vg(ic, m, c0, c1):
    """Paint a mask with a vertical gradient between its own top and bottom."""
    b = m.getbbox()
    if not b:
        return
    ic.paint(m, vgrad(c0, c1, b[1] / S, b[3] / S))


def solid_part(ic, m, c0, c1, rim=None, ra=.55, gloss=0.0, shadow=0.0):
    if shadow:
        shd(ic, m, shadow)
    vg(ic, m, c0, c1)
    if rim:
        ic.rim(m, rim, 2, ra)
    if gloss:
        ic.gloss(m, gloss)


def bez(p0, p1, p2, p3, n=24):
    pts = []
    for i in range(n + 1):
        t = i / n
        a = (1 - t) ** 3; b = 3 * (1 - t) ** 2 * t; c = 3 * (1 - t) * t * t; d = t ** 3
        pts.append((a * p0[0] + b * p1[0] + c * p2[0] + d * p3[0], a * p0[1] + b * p1[1] + c * p2[1] + d * p3[1]))
    return pts


def lines(ic, x0, x1, y0, n, gap, w=4, col=(150, 158, 175), a=.8, short=(2,)):
    for k in range(n):
        xe = x1 - (26 if k in short else 0)
        ic.paint(m_line([(x0, y0 + k * gap), (xe, y0 + k * gap)], w), col, a)


def rot_im(im, deg, cx=128, cy=128):
    return im.rotate(deg, resample=Image.BICUBIC, center=(u(cx), u(cy)))


def paste_rot(ic, sub, deg, dx=0, dy=0, cx=128, cy=128):
    im = rot_im(sub.im, deg, cx, cy)
    if dx or dy:
        im = ImageChops.offset(im, u(dx), u(dy))
    ic.im.alpha_composite(im)


def paper(ic, x0, y0, x1, y1, base=(252, 252, 248), low=(228, 228, 220), r=6, rim=(150, 150, 150), shadow=.25):
    m = m_rrect(x0, y0, x1, y1, r)
    shd(ic, m, shadow)
    vg(ic, m, base, low)
    ic.rim(m, rim, 1.5, .55)
    return m


def dashed(ic, x, y0, y1, col, dash=8, gap=6, w=3, a=.8):
    y = y0
    while y < y1:
        ic.paint(m_line([(x, y), (x, min(y1, y + dash))], w, False), col, a)
        y += dash + gap


def circ_pts(cx, cy, r, a0, a1, n=24):
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)), cy + r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def cylinder(ic, x0, y0, x1, y1, c0, c1, cap=None, rim=None):
    """Horizontal-axis-free simple vertical cylinder body (e.g. a can): body shaded left-to-right."""
    m = m_rrect(x0, y0, x1, y1, (x1 - x0) * .12)
    shd(ic, m, .3)
    ic.paint(m, lin(c0, c1, x0, 0, x1, 0))
    return m


# ═══════════════════════════ RELICS ═══════════════════════════════════════════

def r_dropout_paper(ic):
    s = Icon()
    paper(s, 54, 22, 202, 236, r=5)
    # title
    s.paint(m_line([(76, 52), (160, 52)], 9), (60, 66, 90), .9)
    s.paint(m_line([(76, 70), (132, 70)], 6), (130, 138, 156), .9)
    # net diagram: dropped nodes crossed out
    layers = [(84, [100, 126, 152]), (128, [92, 114, 138, 160]), (172, [100, 126, 152])]
    drop = {(0, 1), (1, 0), (1, 3), (2, 2)}
    em = blank()
    for (xa, ya), (xb, yb) in [(((l0[0]), y0), ((l1[0]), y1)) for l0, l1 in zip(layers, layers[1:]) for y0 in l0[1] for y1 in l1[1]]:
        em = union(em, m_line([(xa, ya), (xb, yb)], 1.6))
    s.paint(em, (150, 165, 190), .55)
    for li, (x, ys) in enumerate(layers):
        for ni, y in enumerate(ys):
            if (li, ni) in drop:
                s.paint(m_circle(x, y, 11), (255, 255, 255))
                s.paint(cut(m_circle(x, y, 11), m_circle(x, y, 8)), (170, 175, 190))
                s.paint(union(m_line([(x - 7, y - 7), (x + 7, y + 7)], 3.4), m_line([(x - 7, y + 7), (x + 7, y - 7)], 3.4)), (222, 60, 60))
            else:
                s.body(m_circle(x, y, 11), "blue")
    lines(s, 76, 182, 184, 3, 14, 4.5, (150, 158, 175), .8, short=(2,))
    # the corner folded over
    fold = m_poly([(202, 188), (202, 236), (154, 236)])
    s.paint(m_poly([(202, 188), (154, 236), (202, 236)]), (255, 255, 255))
    flap = m_poly([(202, 188), (154, 236), (164, 196)])
    shd(s, flap, .3, 2, 2)
    s.paint(flap, vgrad((222, 224, 230), (190, 194, 204), 188, 236))
    cover = intersect(m_rrect(54, 22, 202, 236, 5), m_poly([(202, 236), (202, 188), (154, 236)]))
    # white pop of the folded-over corner is the flap; erase paper under it for a clean cut
    s.im = s.im
    # red margin note
    s.paint(m_text("p=.5", "bold", 24, 140, 214, 0), (214, 52, 52), .95)
    s.paint(m_line(bez((74, 22), (66, 80), (60, 130), (64, 210), 10), 2.2), (232, 120, 120), .5)
    paste_rot(ic, s, -5)


def r_zhou_usb(ic):
    s = Icon()
    body = m_rrect(70, 98, 186, 158, 18)
    shd(s, body, .35, 5, 4)
    s.body(body, "dark")
    # label strip
    s.paint(m_rrect(98, 108, 168, 148, 6), vgrad((250, 250, 246), (215, 218, 224), 108, 148))
    s.paint(m_line([(108, 122), (148, 122)], 4), (60, 66, 90))
    s.paint(m_line([(108, 134), (140, 134)], 4), (150, 156, 172))
    # keyring hole end cap
    s.paint(m_rrect(70, 98, 92, 158, 14), (30, 33, 40), .6)
    hole = m_circle(80, 128, 7)
    s.paint(hole, (0, 0, 0), .0)
    # metal connector
    plug = m_rrect(186, 106, 232, 150, 4)
    shd(s, plug, .3, 3, 2)
    s.paint(plug, vgrad((235, 238, 244), (140, 148, 162), 106, 150))
    s.rim(plug, (90, 98, 112), 1.6, .8)
    s.paint(m_rrect(196, 117, 212, 124, 2), (50, 56, 70)); s.paint(m_rrect(196, 132, 212, 139, 2), (50, 56, 70))
    s.paint(m_rect(186, 104, 192, 152), (110, 116, 130), .6)
    # keyring
    ring = cut(m_circle(52, 128, 26), m_circle(52, 128, 17))
    ring = cut(ring, m_rect(70, 100, 90, 160))
    s.body(ring, "steel", gloss=False)
    s.paint(m_circle(70, 128, 4), (30, 33, 40))
    paste_rot(ic, s, 32, 2, -2)


def r_watermelon_book(ic):
    s = Icon()
    pages = m_rrect(66, 34, 206, 224, 8)
    shd(s, pages, .3, 5, 4)
    s.paint(pages, vgrad((255, 255, 250), (222, 220, 205), 34, 224))
    for k in range(4):
        s.paint(m_line([(76, 218 - k * 3), (204, 218 - k * 3)], 1.2), (190, 188, 175), .7)
    cover = m_rrect(50, 24, 196, 216, 10)
    s.body(cover, "green", gloss=True)
    s.paint(m_rect(50, 24, 64, 216), (14, 80, 28), .55)  # spine
    # stripes like a rind
    for k in range(5):
        x = 86 + k * 24
        s.paint(m_line(bez((x, 34), (x + 8, 90), (x - 8, 150), (x + 2, 206), 16), 5), (24, 110, 40), .35)
    # title plate
    plate = m_rrect(76, 48, 182, 100, 6)
    s.paint(plate, vgrad((255, 252, 235), (240, 232, 200), 48, 100))
    s.rim(plate, (150, 120, 60), 1.5, .8)
    s.paint(m_text("机器学习", "cjk", 22, 129, 74, 0), (30, 90, 40))
    # a watermelon slice
    sl = m_pie(128, 150, 50, 0, 180)
    s.paint(grow(sl, 3), (255, 255, 255), .0)
    shd(s, sl, .3, 3, 2)
    s.paint(sl, vgrad((255, 110, 110), (220, 40, 60), 150, 200))
    rind = cut(m_pie(128, 150, 50, 0, 180), m_pie(128, 150, 42, 0, 180))
    s.paint(rind, (250, 250, 235))
    rind2 = cut(m_pie(128, 150, 54, 0, 180), m_pie(128, 150, 48, 0, 180))
    s.paint(rind2, (30, 130, 50))
    for (x, y) in [(104, 168), (122, 178), (142, 170), (156, 160), (116, 156)]:
        s.paint(m_ellipse(x - 3, y - 4, x + 3, y + 4), (40, 30, 30))
    paste_rot(ic, s, 6)


def r_tablet(ic):
    s = Icon()
    body = m_rrect(34, 64, 216, 196, 14)
    shd(s, body, .35, 6, 5)
    s.body(body, "dark")
    act = m_rrect(66, 78, 204, 182, 6)
    s.paint(act, vgrad((92, 98, 112), (60, 66, 80), 78, 182))
    s.rim(act, (30, 32, 40), 2, .8)
    # active area corner marks
    for (x, y, dx, dy) in [(72, 84, 1, 1), (198, 84, -1, 1), (72, 176, 1, -1), (198, 176, -1, -1)]:
        s.paint(union(m_line([(x, y), (x + 12 * dx, y)], 2.4), m_line([(x, y), (x, y + 12 * dy)], 2.4)), (170, 178, 195), .8)
    # express keys
    for k in range(4):
        s.body(m_rrect(44, 82 + k * 26, 58, 98 + k * 26, 4), "steel", gloss=False, rim=False)
    s.paint(m_circle(44 + 7, 70 + 2, 2.5), (120, 220, 130))
    # a little scribble on the active area
    s.paint(m_line(bez((90, 150), (110, 100), (140, 170), (178, 108), 30), 3.4), (150, 220, 255), .9)
    # the pen
    pen = Icon()
    pb = m_rrect(40, 122, 206, 136, 7)
    pen.body(pb, "gray", gloss=True)
    pen.paint(m_rrect(150, 122, 206, 136, 7), vgrad((90, 96, 110), (40, 44, 54), 122, 136))
    tip = m_poly([(206, 124), (226, 129), (206, 134)])
    pen.paint(tip, (80, 84, 96))
    pen.paint(m_rrect(120, 122, 126, 136, 1), (60, 66, 80), .8)
    pen.paint(m_rrect(52, 126, 108, 132, 3), (80, 150, 255), .0)
    sp = Icon(); sp.im = rot_im(pen.im, 16, 128, 128)
    shd(s, sp.im.split()[3].copy(), .0)
    s.im.alpha_composite(ImageChops.offset(sp.im, u(0), u(34)))
    ic.im.alpha_composite(s.im)


def r_mining_1080(ic):
    s = Icon()
    # bracket
    br = m_rrect(24, 62, 38, 196, 3)
    s.paint(br, lin((240, 244, 250), (130, 138, 150), 24, 0, 38, 0))
    for k in range(4):
        s.paint(m_rrect(27, 76 + k * 26, 35, 92 + k * 26, 3), (70, 76, 90), .9)
    # pcb + fingers
    pcb = m_rect(46, 164, 226, 184)
    s.paint(pcb, vgrad((30, 100, 60), (14, 60, 36), 164, 184))
    for k in range(18):
        s.paint(m_rect(100 + k * 6.3, 184, 104.5 + k * 6.3, 198), (235, 190, 70))
    s.paint(m_rect(100, 184, 214, 186), (170, 130, 40), .7)
    # heat pipes
    for k in range(3):
        y = 62 + k * 7
        pm = m_rrect(54, y, 220, y + 8, 4)
        s.paint(pm, vgrad((255, 190, 130), (170, 90, 40), y, y + 8))
    # shroud
    sh_ = m_rrect(40, 82, 230, 170, 12)
    shd(s, sh_, .35, 5, 4)
    s.body(sh_, "dark")
    s.paint(m_rect(40, 84, 230, 88), (60, 66, 80), .5)
    # fans
    for cx in (92, 180):
        pit = m_circle(cx, 128, 38)
        s.paint(pit, (14, 15, 20))
        s.rim(pit, (90, 96, 110), 2.4, .9)
        blades = blank()
        for k in range(9):
            b = m_ellipse(cx - 7, 128 - 33, cx + 8, 128 - 6)
            blades = union(blades, rotate_mask(b, k * 40, cx, 128))
        s.paint(blades, vgrad((150, 158, 175), (80, 86, 100), 92, 164))
        s.paint(m_circle(cx, 128, 10), (30, 34, 42))
        s.paint(m_circle(cx, 128, 5), (170, 176, 190))
    # accent stripe
    s.paint(m_rrect(120, 168, 152, 170, 1), (230, 70, 50), .9)
    # scorched mark (ex-mining)
    s.paint(m_ellipse(196, 150, 232, 172), (120, 70, 20), .0)
    ic.im.alpha_composite(s.im)


def r_coupon(ic):
    s = Icon()
    # back ticket (orange)
    back = Icon()
    t2 = m_rrect(34, 70, 220, 176, 12)
    shd(back, t2, .3, 4, 3)
    back.body(t2, "orange", gloss=False)
    back.paint(m_text("¥", "black", 40, 70, 100), (255, 255, 255), .0)
    paste_rot(s, back, 11, 4, 12)
    # front ticket (red) with notches
    t = m_rrect(26, 74, 226, 186, 12)
    t = cut(t, m_circle(26, 130, 14)); t = cut(t, m_circle(226, 130, 14))
    shd(s, t, .35, 5, 4)
    s.body(t, "red")
    inner = grow(t, -8)
    s.paint(cut(inner, grow(inner, -2)), (255, 220, 210), .7)
    dashed(s, 168, 84, 176, (255, 225, 215), 8, 6, 3)
    s.paint(m_text("30", "black", 58, 98, 120), (130, 15, 25), .5)
    s.paint(m_text("30", "black", 58, 96, 117), (255, 245, 225))
    s.paint(m_text("¥", "black", 24, 53, 100), (255, 225, 140))
    s.paint(m_text("满300减30", "cjk", 17, 98, 160), (255, 235, 215))
    # barcode stub
    for k, w in enumerate([3, 5, 2, 4, 2, 5, 3, 2, 4]):
        s.paint(m_rect(178 + k * 5.2, 100, 178 + k * 5.2 + w * .8, 150), (255, 240, 232), .9)
    ic.im.alpha_composite(s.im)


def r_server_key(ic):
    s = Icon()
    # tag first (hangs from the ring)
    tag = Icon()
    tm = m_rrect(26, 128, 92, 210, 12)
    shd(tag, tm, .3, 4, 3)
    tag.body(tm, "red")
    tag.paint(m_circle(59, 144, 5), (60, 10, 14))
    tag.paint(m_text("机房", "cjk", 20, 59, 178), (255, 240, 230))
    tag.paint(m_text("B2", "bold", 16, 59, 198), (255, 210, 200), .9)
    # key (horizontal)
    shaft = m_rrect(84, 118, 222, 138, 6)
    teeth = union(m_rect(176, 134, 190, 164), m_rect(196, 134, 208, 154), m_rect(146, 134, 158, 150))
    head = cut(m_circle(66, 128, 34), m_circle(66, 128, 13))
    key = union(shaft, teeth, head)
    shd(s, key, .35, 4, 3)
    s.body(key, (( 255, 235, 160), (214, 164, 60), (126, 82, 24)))
    s.paint(m_line([(96, 124), (210, 124)], 2), (255, 245, 200), .6)
    s.paint(m_circle(66, 128, 13), (0, 0, 0), .0)
    # string loop through the ring
    s.paint(m_arc(66, 128, 18, 90, 200, 4), (230, 230, 235), .9)
    s.im.alpha_composite(tag.im)
    s2 = Icon(); s2.im = s.im
    paste_rot(ic, s2, -35, 6, -4)


def r_multimeter(ic):
    s = Icon()
    boot = m_rrect(62, 14, 194, 242, 24)
    shd(s, boot, .35, 5, 4)
    s.paint(boot, vgrad((70, 76, 90), (28, 30, 38), 14, 242))
    s.rim(boot, (14, 16, 22), 2, .8)
    body = m_rrect(72, 24, 184, 232, 16)
    s.body(body, "yellow")
    # screen
    scr = m_rrect(84, 36, 172, 90, 6)
    s.paint(scr, (40, 44, 52))
    inner = grow(scr, -3)
    s.paint(inner, vgrad((196, 214, 190), (150, 174, 150), 36, 90))
    s.paint(m_text("888", "mono", 34, 134, 66), (130, 152, 130), .45)
    s.paint(m_text("220", "mono", 34, 134, 66), (28, 40, 30))
    s.paint(m_text("V~", "bold", 11, 98, 80), (28, 40, 30))
    # dial
    cx, cy = 128, 148
    ring = m_circle(cx, cy, 46)
    shd(s, ring, .3, 3, 2)
    s.paint(ring, vgrad((70, 76, 90), (24, 26, 34), 102, 194))
    for k in range(10):
        a = math.radians(-220 + k * 30)
        s.paint(m_line([(cx + 40 * math.cos(a), cy + 40 * math.sin(a)), (cx + 48 * math.cos(a), cy + 48 * math.sin(a))], 3), (250, 235, 160), .0)
    knob = m_circle(cx, cy, 30)
    s.body(knob, "gray", gloss=True)
    s.paint(m_line([(cx, cy), (cx + 22 * math.cos(math.radians(-50)), cy + 22 * math.sin(math.radians(-50)))], 6), (30, 34, 44))
    # tick marks around the dial (white dots)
    for k in range(9):
        a = math.radians(-200 + k * 28)
        s.paint(m_circle(cx + 40 * math.cos(a), cy + 40 * math.sin(a), 2.6), (255, 255, 255), .9)
    # jacks
    for x, col in ((94, (200, 40, 40)), (128, (230, 230, 235)), (162, (240, 150, 40))):
        j = m_circle(x, 212, 10)
        s.paint(j, (20, 22, 28))
        s.paint(m_circle(x, 212, 6.5), col)
    ic.im.alpha_composite(s.im)


def r_spider_ua(ic):
    cx, cy = 128, 138
    legs = blank()
    for side in (-1, 1):
        for k, (a, b) in enumerate([(-70, -112), (-30, -70), (10, -30), (50, 8)]):
            # each leg: shoulder -> knee (up) -> foot (down)
            kx = cx + side * (52 + k * 6)
            ky = cy - 38 + k * 16
            fx = cx + side * (98 - k * 4 + (6 if k in (1, 2) else 0))
            fy = cy - 8 + k * 22 + (6 if k == 0 else 0)
            legs = union(legs, m_line([(cx + side * 22, cy + k * 4 - 6), (kx, ky - 16), (fx, fy)], 9))
    shd(ic, legs, .35, 4, 3)
    ic.body(legs, "dark", gloss=False, rim=False)
    abdomen = m_ellipse(cx - 42, cy - 2, cx + 42, cy + 82)
    shd(ic, abdomen, .35, 5, 4)
    ic.body(abdomen, "dark")
    ic.paint(m_ellipse(cx - 12, cy + 12, cx + 12, cy + 56), (220, 60, 60), .0)
    head = m_ellipse(cx - 36, cy - 52, cx + 36, cy + 12)
    ic.body(head, "dark")
    # disguise: round glasses + moustache
    for ex in (-17, 17):
        ic.paint(m_circle(cx + ex, cy - 20, 15), (255, 255, 255))
        ic.paint(m_circle(cx + ex + 2, cy - 20, 6), (20, 22, 28))
        ic.paint(cut(m_circle(cx + ex, cy - 20, 16), m_circle(cx + ex, cy - 20, 13)), (230, 190, 70))
    ic.paint(m_line([(cx - 4, cy - 22), (cx + 4, cy - 22)], 3), (230, 190, 70))
    must = union(m_ellipse(cx - 24, cy - 2, cx, cy + 10), m_ellipse(cx, cy - 2, cx + 24, cy + 10))
    ic.paint(must, (90, 54, 28))
    # a name-tag "UA"
    tag = m_rrect(cx - 28, cy + 36, cx + 28, cy + 66, 6)
    ic.paint(tag, vgrad((255, 255, 255), (225, 228, 236), cy + 36, cy + 66))
    ic.paint(m_text("UA", "black", 22, cx, cy + 52), (50, 60, 90))
    ic.paint(m_ellipse(cx - 36, cy + 4, cx - 12, cy + 12), (255, 255, 255), .0)


def r_arcade_tokens(ic):
    s = Icon()
    # pouch
    body = union(m_ellipse(54, 100, 202, 232), m_poly([(100, 78), (156, 78), (178, 130), (78, 130)]))
    shd(s, body, .35, 6, 5)
    s.paint(body, vgrad((190, 80, 70), (110, 30, 36), 78, 232))
    s.rim(body, (80, 16, 24), 2.2, .6)
    s.paint(m_ellipse(70, 112, 120, 180), (255, 255, 255), .0)
    # cloth highlight
    s.paint(intersect(body, m_ellipse(66, 106, 126, 196)), (255, 210, 190), .22)
    for k in range(3):
        s.paint(m_line(bez((98 + k * 30, 94), (92 + k * 34, 120), (96 + k * 26, 150), (92 + k * 38, 180), 12), 3), (90, 20, 28), .35)
    # gathered neck + drawstring
    neck = m_ellipse(94, 66, 162, 98)
    s.paint(neck, vgrad((210, 100, 86), (140, 44, 44), 66, 98))
    s.rim(neck, (80, 16, 24), 1.6, .6)
    s.paint(m_ellipse(104, 70, 152, 90), (60, 10, 18), .7)
    cord = m_line(bez((90, 102), (110, 112), (146, 112), (166, 102), 16), 6)
    s.paint(cord, vgrad((255, 230, 140), (200, 150, 50), 100, 116))
    s.paint(m_line(bez((100, 110), (88, 130), (86, 150), (94, 166), 12), 5), (240, 200, 90))
    s.paint(m_line(bez((156, 110), (170, 130), (172, 150), (164, 168), 12), 5), (240, 200, 90))
    ic.im.alpha_composite(s.im)

    def token(cx, cy, r=26, star=True):
        c = m_circle(cx, cy, r)
        shd(ic, c, .4, 4, 3)
        ic.body(c, "gold")
        ic.paint(cut(m_circle(cx, cy, r * .78), m_circle(cx, cy, r * .68)), (150, 100, 25), .8)
        if star:
            ic.paint(m_star(cx, cy + 1, r * .5, r * .22), (160, 108, 28), .95)
            ic.paint(m_star(cx, cy - 1, r * .5, r * .22), (255, 245, 190), .0)

    token(100, 74, 24)
    token(160, 70, 22)
    token(70, 208, 26)
    token(190, 200, 28)
    token(128, 218, 26)


def r_tieba(ic):
    s = Icon()
    # three pages offset (a thick stack of printed posts)
    for k, (dx, dy) in enumerate([(14, 14), (7, 7), (0, 0)]):
        paper(s, 50 + dx, 28 + dy, 190 + dx, 220 + dy, base=(250, 250, 246), low=(228, 228, 218), shadow=.25 if k else .3)
    # top page content: header bar + posts as bubbles
    s.paint(m_rrect(62, 40, 178, 62, 4), (60, 120, 220))
    s.paint(m_text("贴吧", "cjk", 16, 96, 51), (255, 255, 255))
    for k in range(3):
        y = 78 + k * 34
        s.paint(m_circle(76, y + 8, 9), (150, 175, 220))
        s.paint(m_line([(94, y), (172, y)], 5), (120, 128, 146), .9)
        s.paint(m_line([(94, y + 16), (150, y + 16)], 5), (170, 176, 190), .9)
    # red "featured" stamp
    st = m_circle(150, 188, 26)
    s.paint(cut(st, grow(st, -3)), (214, 40, 40), .95)
    s.paint(cut(grow(st, -6), grow(st, -8)), (214, 40, 40), .8)
    s.paint(m_text("精", "cjk", 30, 150, 188), (214, 40, 40), .95)
    # binder clip
    clip = m_rrect(70, 12, 124, 46, 6)
    shd(s, clip, .3, 3, 2)
    s.body(clip, "dark", gloss=True)
    for x in (62, 132):
        s.paint(m_line([(x, 32), (x + (18 if x < 100 else -18), 18)], 4), (200, 206, 216))
    ic.im.alpha_composite(rot_im(s.im, 4))


def r_allnight(ic):
    s = Icon()
    card = m_rrect(24, 58, 232, 202, 18)
    shd(s, card, .35, 6, 5)
    s.paint(card, vgrad((34, 44, 110), (10, 14, 48), 58, 202))
    s.rim(card, (150, 170, 255), 2, .5)
    s.gloss(card, .25)
    # stars
    import random
    rnd = random.Random(7)
    for _ in range(26):
        x, y = rnd.uniform(40, 216), rnd.uniform(70, 190)
        s.paint(m_circle(x, y, rnd.uniform(.9, 2.2)), (255, 255, 230), rnd.uniform(.4, .9))
    # moon
    moon = cut(m_circle(80, 112, 38), m_circle(98, 100, 34))
    s.glow(moon, (255, 235, 150), 12, .6)
    s.body(moon, "yellow")
    # text
    s.paint(m_text("通宵", "cjk", 46, 168, 108), (0, 0, 0), .35)
    s.paint(m_text("通宵", "cjk", 46, 166, 105), (255, 225, 110))
    s.paint(m_text("ALL NIGHT", "bold", 14, 166, 138), (190, 200, 255), .9)
    # magnetic strip + hole
    s.paint(m_rect(24, 166, 232, 188), (20, 20, 26))
    s.paint(m_rrect(40, 70, 62, 80, 5), (0, 0, 0), .0)
    s.paint(m_text("10 ¥", "bold", 13, 190, 178), (255, 255, 255), .0)
    ic.im.alpha_composite(rot_im(s.im, -6))


def can(ic, cx, y0, y1, w):
    """A generic slim energy-drink can (no logo): necked top, two-tone body, a sun disc and a bolt."""
    x0, x1 = cx - w / 2, cx + w / 2
    nx0, nx1 = cx - w * .40, cx + w * .40
    body = m_rrect(x0, y0 + 22, x1, y1 - 8, 8)
    shoulder = m_poly([(x0 + 1, y0 + 30), (nx0, y0 + 10), (nx1, y0 + 10), (x1 - 1, y0 + 30)])
    top = m_ellipse(nx0, y0 + 2, nx1, y0 + 18)
    bot = m_ellipse(x0 + 2, y1 - 18, x1 - 2, y1)
    full = union(body, shoulder, top, bot)
    shd(ic, full, .35, 5, 4)
    ic.paint(full, lin((110, 140, 205), (14, 34, 96), x0, 0, x1, 0))
    mid = y0 + (y1 - y0) * .56
    low = intersect(full, m_rect(x0, mid, x1, y1))
    ic.paint(low, lin((250, 252, 255), (130, 140, 160), x0, 0, x1, 0))
    ic.paint(intersect(full, m_rect(x0, mid - 2, x1, mid + 2)), (230, 60, 60), .95)
    ic.paint(intersect(full, m_rect(x0 + 5, y0 + 20, x0 + 12, y1 - 10)), (255, 255, 255), .5)
    ic.paint(intersect(full, m_rect(x1 - 11, y0 + 20, x1 - 5, y1 - 10)), (0, 0, 0), .22)
    # lid
    ic.paint(top, vgrad((252, 254, 255), (150, 158, 175), y0 + 2, y0 + 18))
    ic.paint(m_ellipse(nx0 + 4, y0 + 5, nx1 - 4, y0 + 14), (196, 202, 214), .95)
    ic.paint(m_rrect(cx - 7, y0 + 7, cx + 7, y0 + 11, 2), (110, 116, 130), .95)
    ic.paint(m_circle(cx + 7, y0 + 9, 2.5), (110, 116, 130), .0)
    # label: sun disc + bolt on the blue half
    ly = y0 + (mid - y0) * .62 + 10
    disc = m_circle(cx, ly, w * .30)
    ic.paint(disc, rgrad((255, 240, 150), (240, 140, 20), cx - 4, ly - 8, w * .5))
    bolt = m_poly([(cx + 3, ly - 17), (cx - 9, ly + 2), (cx - 1, ly + 2), (cx - 4, ly + 17), (cx + 10, ly - 4), (cx + 2, ly - 4)])
    ic.paint(bolt, (16, 36, 100))
    ic.paint(m_text("250ml", "bold", 11, cx, mid + 26), (60, 70, 100), .85)
    return full


def r_redbull(ic):
    s = Icon()
    box = m_rrect(36, 168, 220, 232, 6)
    shd(s, box, .35, 5, 4)
    s.paint(box, vgrad((222, 176, 116), (170, 118, 62), 168, 232))
    s.rim(box, (110, 70, 30), 2, .6)
    s.paint(m_rect(36, 168, 220, 178), (240, 205, 150), .8)
    s.paint(m_rrect(112, 178, 144, 192, 4), (90, 56, 24), .6)
    s.paint(m_line([(54, 208), (104, 208)], 4), (120, 78, 36), .5)
    ic.im.alpha_composite(s.im)
    can(ic, 88, 26, 192, 58)
    can(ic, 168, 26, 192, 58)
    can(ic, 128, 8, 208, 62)


def r_imagenet_disc(ic):
    s = Icon()
    # jewel-case sleeve peeking out
    sl = m_rrect(30, 40, 210, 220, 10)
    shd(s, sl, .3, 5, 4)
    s.paint(sl, vgrad((235, 240, 250), (190, 198, 215), 40, 220), .0)
    cx, cy = 128, 128
    disc = m_circle(cx, cy, 104)
    shd(s, disc, .35, 5, 4)
    # silver base
    s.paint(disc, vgrad((248, 250, 255), (176, 184, 200), 24, 232))
    # rainbow reflection sectors
    yy, xx = np.mgrid[0:N, 0:N] / S
    ang = (np.degrees(np.arctan2(yy - cy, xx - cx)) % 360)
    hsv = np.zeros((N, N, 3))
    t = ((ang * 2) % 360) / 360
    import colorsys
    h = (t * .85)
    # fast HSV->RGB
    i = (h * 6).astype(int) % 6; f = h * 6 - np.floor(h * 6)
    v = np.ones_like(h); p = 1 - .55; q = 1 - .55 * f; tt = 1 - .55 * (1 - f)
    rgb = np.zeros((N, N, 3))
    for k, (r_, g_, b_) in enumerate([(v, tt, p * v), (q, v, p * v), (p * v, v, tt), (p * v, q, v), (tt, p * v, v), (v, p * v, q)]):
        mk = i == k
        rgb[..., 0][mk] = np.broadcast_to(r_, h.shape)[mk]; rgb[..., 1][mk] = np.broadcast_to(g_, h.shape)[mk]; rgb[..., 2][mk] = np.broadcast_to(b_, h.shape)[mk]
    arr = np.dstack([(rgb * 255).astype("uint8"), np.full((N, N), 255, "uint8")])
    rainbow = Image.fromarray(arr, "RGBA")
    # two opposite fans of light
    fan = blank()
    fan = union(m_pie(cx, cy, 104, -60, -15), m_pie(cx, cy, 104, 120, 165))
    s.paint(intersect(disc, fan), rainbow, .55)
    s.rim(disc, (120, 128, 148), 2.4, .9)
    # printed label zone with mini pictures
    lab = cut(m_circle(cx, cy, 66), m_circle(cx, cy, 24))
    s.paint(lab, (250, 250, 252))
    cols = [(230, 80, 70), (80, 170, 90), (70, 130, 220), (240, 190, 50), (160, 100, 210), (70, 190, 190), (240, 130, 60), (130, 190, 80)]
    k = 0
    for r_i, rr in enumerate((46, 60 - 0)):
        pass
    for k in range(12):
        a = math.radians(k * 30 + 10)
        px, py = cx + 45 * math.cos(a), cy + 45 * math.sin(a)
        tile_ = m_rrect(px - 9, py - 9, px + 9, py + 9, 3)
        s.paint(tile_, cols[k % len(cols)])
        s.paint(m_circle(px - 2, py - 2, 3.4), (255, 255, 255), .6)
    # tracks
    s.paint(cut(m_circle(cx, cy, 20), m_circle(cx, cy, 17)), (150, 156, 170), .7)
    hole = m_circle(cx, cy, 11)
    s.paint(hole, (0, 0, 0), 1.0)
    s.im.putalpha(ImageChops.subtract(s.im.split()[3], hole))
    # scratches
    for (a, b, c, d) in [(70, 70, 120, 96), (160, 170, 200, 150), (60, 150, 92, 176)]:
        s.paint(m_line([(a, b), (c, d)], 1.2), (255, 255, 255), .8)
    ic.im.alpha_composite(s.im)


def bell_curve(ic, cx, base_y, w, h, col, alpha=1, width=5, shift=0):
    pts = []
    for i in range(61):
        x = -3 + i * .1
        pts.append((cx + shift + x * w / 6, base_y - h * math.exp(-x * x / 2)))
    ic.paint(m_line(pts, width), col, alpha)


def r_batchnorm_paper(ic):
    s = Icon()
    paper(s, 54, 22, 202, 236, r=5)
    s.paint(m_line([(74, 50), (180, 50)], 9), (60, 66, 90), .9)
    s.paint(m_line([(74, 68), (142, 68)], 6), (130, 138, 156), .9)
    # a skewed wide curve (before) and a clean narrow one (after)
    s.paint(m_line([(70, 168), (186, 168)], 2), (150, 158, 175))
    bell_curve(s, 100, 168, 90, 34, (230, 120, 50), .9, 5, -4)
    bell_curve(s, 150, 168, 56, 62, (40, 120, 230), 1, 6, 0)
    s.paint(m_poly([(112, 112), (128, 104), (126, 114), (140, 114), (140, 122), (126, 122), (128, 132)]), (80, 90, 110), .0)
    # arrow between
    s.paint(m_line([(94, 116), (126, 116)], 4), (90, 100, 125))
    s.paint(m_poly([(126, 108), (140, 116), (126, 124)]), (90, 100, 125))
    s.paint(m_text("μ=0  σ=1", "bold", 15, 128, 192), (70, 80, 110))
    lines(s, 74, 182, 206, 2, 12, 4.2, (150, 158, 175), .8, short=(1,))
    # paper clip
    clip = m_line([(170, 14), (170, 46), (184, 46), (184, 20), (176, 20), (176, 40)], 3.6)
    s.paint(clip, (180, 188, 202))
    ic.im.alpha_composite(rot_im(s.im, 4))


def r_resnet_sticker(ic):
    s = Icon()
    note = m_rrect(34, 34, 222, 222, 10)
    # a sticker: bright yellow, curled bottom-right corner
    note = cut(note, m_poly([(222, 176), (222, 222), (176, 222)]))
    shd(s, note, .35, 5, 4)
    s.paint(note, vgrad((255, 244, 140), (250, 210, 50), 34, 222))
    s.rim(note, (190, 140, 20), 1.8, .6)
    curl = m_poly([(222, 176), (176, 222), (212, 212)])
    shd(s, curl, .35, 2, 2)
    s.paint(curl, vgrad((240, 232, 190), (214, 196, 120), 176, 222))
    # residual block diagram
    bx = (84, 172)
    for y in (62, 126):
        b = m_rrect(bx[0], y, bx[1], y + 40, 8)
        s.body(b, "blue", gloss=True)
    s.paint(m_text("F(x)", "bold", 20, 128, 102), (255, 255, 255), .0)
    s.paint(m_text("F", "serif", 28, 128, 146), (255, 255, 255))
    # arrows
    s.paint(m_line([(128, 40 + 4), (128, 62)], 5), (50, 60, 90))
    s.paint(m_line([(128, 102), (128, 126)], 5), (50, 60, 90))
    s.paint(m_line([(128, 166), (128, 184)], 5), (50, 60, 90))
    # plus node
    s.body(m_circle(128, 198, 14), "red")
    s.paint(m_rect(120, 196, 136, 200), (255, 255, 255))
    s.paint(m_rect(126, 190, 130, 206), (255, 255, 255))
    # the skip connection
    s.paint(m_line([(128, 50), (62, 50), (62, 198), (112, 198)], 6), (230, 60, 60))
    s.paint(m_poly([(112, 190), (124, 198), (112, 206)]), (230, 60, 60))
    ic.im.alpha_composite(rot_im(s.im, -4))


def r_data_broker(ic):
    s = Icon()
    card = m_rrect(26, 56, 230, 202, 12)
    shd(s, card, .35, 6, 5)
    s.paint(card, vgrad((52, 54, 64), (20, 22, 28), 56, 202))
    s.rim(card, (210, 170, 70), 2.4, .9)
    s.gloss(card, .2)
    # database cylinder
    for k, y in enumerate((100, 126, 152)):
        slab = union(m_ellipse(48, y - 12, 108, y + 12), m_rect(48, y, 108, y + 26))
        slab = union(m_rect(48, y, 108, y + 18), m_ellipse(48, y - 12, 108, y + 12), m_ellipse(48, y + 6, 108, y + 30))
        s.paint(slab, lin((255, 235, 150), (200, 140, 40), 48, 0, 108, 0))
        s.paint(m_ellipse(48, y - 12, 108, y + 12), vgrad((255, 245, 190), (225, 170, 60), y - 12, y + 12))
        s.rim(m_ellipse(48, y - 12, 108, y + 12), (140, 90, 20), 1.4, .6)
    # text lines
    s.paint(m_text("数据", "cjk", 30, 176, 90), (240, 200, 90))
    s.paint(m_text("DATA", "bold", 14, 176, 114), (200, 204, 215), .8)
    s.paint(m_line([(128, 132), (212, 132)], 3), (120, 124, 138))
    s.paint(m_text("QQ 3 8 2 5 ...", "mono", 14, 170, 150), (210, 214, 225), .9)
    s.paint(m_text("不问来路", "cjk", 14, 170, 176), (150, 156, 170), .9)
    ic.im.alpha_composite(rot_im(s.im, 7))


def r_campus_vpn(ic):
    s = Icon()
    card = m_rrect(30, 40, 226, 214, 16)
    shd(s, card, .35, 6, 5)
    s.paint(card, vgrad((70, 140, 235), (22, 80, 170), 40, 214))
    s.rim(card, (14, 50, 120), 2, .8)
    s.gloss(card, .3)
    # header band
    s.paint(intersect(card, m_rect(30, 40, 226, 78)), (255, 255, 255), .0)
    s.paint(m_line([(104, 58), (206, 58)], 6), (255, 255, 255), .9)
    s.paint(m_circle(64, 58, 13), (255, 255, 255))
    s.paint(m_circle(64, 58, 8), (30, 80, 170))
    # photo
    ph = m_rrect(48, 92, 112, 176, 6)
    s.paint(ph, vgrad((225, 238, 255), (150, 190, 240), 92, 176))
    s.paint(m_circle(80, 124, 15), (60, 100, 170))
    s.paint(intersect(ph, m_ellipse(52, 146, 108, 200)), (60, 100, 170))
    lines(s, 126, 210, 106, 3, 18, 5, (230, 240, 255), .85, short=(2,))
    # shield with a key (the VPN login)
    sh_ = m_poly([(166, 148), (204, 148), (204, 172), (185, 198), (166, 172)])
    sh_ = union(sh_, m_poly([(150, 148), (222, 148), (222, 176), (186, 212), (150, 176)]))
    sh_ = m_poly([(150, 146), (222, 146), (222, 174), (186, 212), (150, 174)])
    shd(s, sh_, .4, 3, 2)
    s.body(sh_, "gold")
    s.paint(m_circle(176, 168, 8), (120, 76, 10))
    s.paint(union(m_line([(176, 168), (204, 168)], 5), m_rect(196, 168, 201, 180), m_rect(186, 168, 190, 176)), (120, 76, 10))
    ic.im.alpha_composite(rot_im(s.im, -4))


def r_gold_cert(ic):
    s = Icon()
    # ribbon tails
    for sgn in (-1, 1):
        r = m_poly([(128 + sgn * 18, 150), (128 + sgn * 62, 150), (128 + sgn * 74, 236), (128 + sgn * 46, 214), (128 + sgn * 22, 236)])
        shd(s, r, .3, 3, 2)
        s.body(r, "red", gloss=False)
    # scalloped rosette
    rose = m_gear(128, 110, 92, 24, .06, 0)
    rose = union(rose, m_circle(128, 110, 84))
    shd(s, rose, .4, 5, 4)
    s.body(rose, "gold")
    inner = m_circle(128, 110, 66)
    s.paint(inner, vgrad((150, 100, 25), (255, 235, 150), 44, 176), .85)
    s.rim(inner, (140, 90, 20), 2, .9)
    s.paint(m_star(128, 108, 44, 20), (255, 245, 200), .0)
    s.paint(m_text("金", "cjk", 78, 128, 114), (120, 74, 10))
    s.paint(m_text("金", "cjk", 78, 127, 111), (255, 244, 190))
    s.gloss(rose, .35)
    ic.im.alpha_composite(s.im)


def scratch_sheet(s, x0, y0, x1, y1, base=(255, 250, 215), low=(240, 232, 190), rule=(120, 170, 230)):
    m = m_rrect(x0, y0, x1, y1, 4)
    shd(s, m, .3, 5, 4)
    vg(s, m, base, low)
    s.rim(m, (180, 170, 120), 1.4, .6)
    for y in range(int(y0 + 24), int(y1 - 6), 17):
        s.paint(m_line([(x0 + 4, y), (x1 - 4, y)], 1.3), rule, .6)
    s.paint(m_line([(x0 + 22, y0 + 4), (x0 + 22, y1 - 4)], 1.5), (230, 120, 120), .6)
    return m


def r_attention_scratch(ic):
    s = Icon()
    # torn top edge via a ragged polygon
    pts = [(50, 36)]
    import random
    rnd = random.Random(3)
    x = 50
    while x < 206:
        x += rnd.uniform(8, 16)
        pts.append((min(x, 206), 30 + rnd.uniform(0, 9)))
    pts += [(206, 232), (50, 232)]
    m = m_poly(pts)
    shd(s, m, .3, 5, 4)
    vg(s, m, (255, 250, 215), (238, 230, 188))
    s.rim(m, (170, 160, 110), 1.3, .55)
    for y in range(54, 226, 17):
        s.paint(intersect(m_line([(52, y), (204, y)], 1.3), m), (120, 170, 230), .55)
    s.paint(m_line([(70, 40), (70, 230)], 1.5), (230, 120, 120), .55)
    # an attention matrix (heat-map 5x5)
    vals = [[9, 3, 1, 0, 1], [2, 8, 3, 1, 0], [1, 3, 9, 4, 1], [0, 1, 3, 8, 3], [1, 0, 1, 3, 9]]
    gx, gy, cell = 96, 62, 22
    for r_ in range(5):
        for c in range(5):
            v = vals[r_][c] / 9
            col = (int(255 - 215 * v), int(240 - 130 * v), int(235 - 20 * v))
            s.paint(m_rect(gx + c * cell, gy + r_ * cell, gx + c * cell + cell - 2, gy + r_ * cell + cell - 2), col)
    # Q K V scribbled in pencil
    for i, ch in enumerate("QKV"):
        s.paint(m_text(ch, "serif", 22, 82, 74 + i * 34, 0), (70, 74, 90), .85)
    s.paint(m_text("softmax", "serif", 18, 150, 190, 0), (70, 74, 90), .85)
    s.paint(m_line([(104, 198), (196, 198)], 2.2), (70, 74, 90), .8)
    s.paint(m_text("2017", "bold", 18, 176, 218), (200, 60, 60), .9)
    # a coffee ring
    ring = cut(m_circle(180, 100, 26), m_circle(180, 100, 22))
    s.paint(ring, (150, 100, 50), .0)
    ic.im.alpha_composite(rot_im(s.im, 6))


def go_stone(ic, cx, cy, r, white):
    m = m_circle(cx, cy, r)
    shd(ic, m, .45, 2, 1.5)
    if white:
        ic.paint(m, rgrad((255, 255, 255), (200, 204, 214), cx - r * .3, cy - r * .35, r * 1.6))
    else:
        ic.paint(m, rgrad((100, 104, 120), (10, 10, 14), cx - r * .3, cy - r * .35, r * 1.5))
    ic.paint(m_ellipse(cx - r * .55, cy - r * .7, cx + r * .1, cy - r * .2), (255, 255, 255), .5 if not white else .0)


def r_alphago_book(ic):
    s = Icon()
    # open notebook: left page ruled with move numbers, right page the board
    left = m_poly([(24, 60), (126, 66), (126, 214), (24, 208)])
    right = m_poly([(130, 66), (232, 60), (232, 208), (130, 214)])
    both = union(left, right)
    cover = m_rrect(14, 52, 242, 226, 10)
    shd(s, cover, .35, 6, 5)
    s.paint(cover, vgrad((50, 54, 66), (16, 18, 24), 52, 226))
    s.paint(left, vgrad((255, 253, 245), (232, 228, 214), 60, 214))
    s.paint(right, vgrad((255, 253, 245), (232, 228, 214), 60, 214))
    s.paint(m_rect(124, 62, 132, 214), (160, 150, 130), .5)
    # left: numbered moves
    for k in range(6):
        y = 88 + k * 20
        s.paint(m_text(str(k * 3 + 1) + ".", "mono", 14, 44, y), (90, 94, 110), .9)
        s.paint(m_line([(56, y + 6), (110, y + 6 + (k % 2) * -1)], 1.6), (140, 150, 175), .8)
    s.paint(m_text("A", "black", 14, 108, 200), (210, 60, 60), .0)
    # right: a go board with stones
    bx0, by0, bx1, by1 = 142, 80, 222, 196
    board = m_rrect(bx0 - 6, by0 - 8, bx1 + 6, by1 + 8, 4)
    s.paint(board, vgrad((236, 190, 110), (205, 150, 70), by0, by1))
    for k in range(6):
        x = bx0 + k * (bx1 - bx0) / 5
        s.paint(m_line([(x, by0), (x, by1)], 1.2), (90, 56, 20), .85)
    for k in range(8):
        y = by0 + k * (by1 - by0) / 7
        s.paint(m_line([(bx0, y), (bx1, y)], 1.2), (90, 56, 20), .85)
    stones = [(0, 2, 1), (1, 3, 0), (2, 3, 1), (2, 4, 0), (3, 4, 1), (3, 2, 0), (1, 5, 1), (4, 5, 0), (4, 3, 1), (2, 1, 0)]
    for c, r_, w in stones:
        go_stone(s, bx0 + c * (bx1 - bx0) / 5, by0 + r_ * (by1 - by0) / 7, 9, w)
    # red mark on the last move
    s.paint(m_circle(bx0 + 4 * (bx1 - bx0) / 5, by0 + 3 * (by1 - by0) / 7, 3), (230, 60, 60))
    # pencil
    pencil = Icon()
    pencil.paint(m_rrect(10, 124, 150, 140, 3), vgrad((255, 214, 80), (230, 160, 20), 124, 140))
    pencil.paint(m_poly([(150, 124), (176, 132), (150, 140)]), (240, 200, 150))
    pencil.paint(m_poly([(166, 128.5), (176, 132), (166, 135.5)]), (50, 50, 60))
    pencil.paint(m_rrect(0, 124, 20, 140, 3), (235, 120, 130))
    pencil.paint(m_rect(20, 124, 28, 140), (190, 196, 210))
    s.im.alpha_composite(ImageChops.offset(rot_im(pencil.im, -28, 128, 132), u(18), u(34)))
    ic.im.alpha_composite(s.im)


def r_taihulight(ic):
    s = Icon()
    card = m_rrect(22, 44, 234, 212, 18)
    shd(s, card, .4, 6, 5)
    s.paint(card, vgrad((150, 30, 36), (70, 8, 16), 44, 212))
    s.rim(card, (255, 210, 110), 3, .95)
    s.paint(cut(grow(card, -9), grow(card, -11)), (255, 210, 110), .7)
    s.gloss(card, .22)
    # a row of cabinets (the machine)
    for k in range(6):
        x = 42 + k * 24
        cab = m_rrect(x, 112, x + 20, 190, 3)
        s.paint(cab, vgrad((120, 126, 142), (36, 38, 48), 112, 190))
        s.rim(cab, (16, 18, 24), 1.2, .8)
        for j in range(5):
            on = (j + k) % 3 != 0
            s.paint(m_rect(x + 3, 120 + j * 13, x + 17, 126 + j * 13), (110, 230, 130) if on else (60, 66, 80), .95)
    # chip
    chip = m_rrect(168, 58, 214, 100, 6)
    s.body(chip, "gold")
    s.paint(m_rrect(176, 66, 206, 92, 3), (150, 100, 25), .8)
    for k in range(3):
        s.paint(m_rect(172 + k * 15, 54, 176 + k * 15, 58), (255, 235, 150))
        s.paint(m_rect(172 + k * 15, 100, 176 + k * 15, 104), (255, 235, 150))
    # text
    s.paint(m_text("神威", "cjk", 36, 90, 82), (0, 0, 0), .35)
    s.paint(m_text("神威", "cjk", 36, 88, 80), (255, 224, 130))
    s.paint(m_text("TAIHULIGHT", "bold", 13, 112, 102), (255, 210, 150), .9)
    s.paint(m_text("#1  2016.06", "mono", 11, 118, 202), (255, 210, 150), .0)
    ic.im.alpha_composite(rot_im(s.im, -5))


def signature_path():
    pts = []
    pts += bez((0, 0), (12, -44), (24, 30), (30, -10), 14)
    pts += bez((30, -10), (40, -36), (36, 24), (52, 0), 14)
    pts += bez((52, 0), (64, -20), (70, 18), (86, -4), 14)
    pts += bez((86, -4), (100, -14), (110, 12), (128, 0), 14)
    return pts


def r_advisor_page(ic):
    s = Icon()
    paper(s, 48, 26, 208, 232, r=5)
    s.paint(m_line([(70, 54), (186, 54)], 8), (60, 66, 90), .9)
    s.paint(m_line([(70, 72), (150, 72)], 5), (140, 148, 165), .9)
    lines(s, 70, 186, 98, 4, 15, 4, (160, 166, 182), .8, short=(3,))
    # signature line + signature
    s.paint(m_line([(70, 196), (190, 196)], 2.2), (90, 96, 112))
    sig = [(70 + x * .85, 188 + y * .8) for x, y in signature_path()]
    s.paint(m_line(sig, 3.6), (30, 60, 190), .95)
    s.paint(m_line([(80, 205), (150, 202)], 2), (30, 60, 190), .6)
    # red seal
    seal = m_rrect(144, 150, 190, 196, 4)
    s.paint(seal, (210, 44, 44), .9)
    s.paint(cut(grow(seal, -4), grow(seal, -6)), (255, 255, 255), .6)
    s.paint(m_text("准", "cjk", 34, 167, 174), (255, 255, 255), .95)
    # fountain pen resting
    pen = Icon()
    body = m_rrect(0, 118, 112, 134, 8)
    pen.body(body, "dark")
    pen.paint(m_rect(88, 118, 94, 134), (235, 190, 70))
    pen.paint(m_poly([(112, 120), (140, 126), (112, 132)]), (235, 190, 70))
    pen.paint(m_poly([(130, 124.5), (142, 126), (130, 127.5)]), (60, 40, 10))
    pen.paint(m_rrect(10, 124, 70, 128, 2), (255, 255, 255), .35)
    s.im.alpha_composite(ImageChops.offset(rot_im(pen.im, -32, 128, 126), u(78), u(66)))
    ic.im.alpha_composite(rot_im(s.im, -4))


def r_angel_term(ic):
    s = Icon()
    paper(s, 46, 24, 206, 232, r=5)
    s.paint(m_rect(46, 24, 206, 60), (30, 70, 160), .0)
    hdr = intersect(m_rrect(46, 24, 206, 232, 5), m_rect(46, 24, 206, 62))
    s.paint(hdr, vgrad((60, 110, 210), (24, 66, 160), 24, 62))
    s.paint(m_text("TERM SHEET", "bold", 17, 126, 44), (255, 255, 255))
    lines(s, 66, 186, 84, 3, 16, 4, (160, 166, 182), .85, short=(2,))
    s.paint(m_text("估值", "cjk", 17, 82, 150), (60, 66, 90))
    s.paint(m_line([(104, 156), (188, 156)], 2.6), (70, 76, 96))
    s.paint(m_text("$ ____", "mono", 14, 146, 144), (200, 200, 210), .0)
    # signature scribble at the bottom
    s.paint(m_line([(66, 214), (110, 214)], 2), (90, 96, 112))
    sig = [(70 + x * .5, 206 + y * .45) for x, y in signature_path()]
    s.paint(m_line(sig, 2.6), (30, 60, 190), .9)
    # gold coin stamped at a corner
    c = m_circle(176, 196, 32)
    shd(s, c, .4, 4, 3)
    s.body(c, "gold")
    s.paint(cut(m_circle(176, 196, 25), m_circle(176, 196, 22)), (150, 100, 25), .8)
    s.paint(m_text("¥", "black", 40, 176, 197), (150, 100, 25))
    s.paint(m_text("¥", "black", 40, 175, 195), (255, 244, 190))
    # paper clip
    s.paint(m_line([(176, 12), (176, 44), (192, 44), (192, 18), (184, 18), (184, 38)], 3.6), (180, 188, 202))
    ic.im.alpha_composite(rot_im(s.im, 4))


RELICS = {
    "dropout_paper": ("rare", r_dropout_paper), "zhou_usb": ("rare", r_zhou_usb), "watermelon_book": ("rare", r_watermelon_book),
    "tablet": ("rare", r_tablet), "mining_1080": ("rare", r_mining_1080), "coupon": ("rare", r_coupon),
    "server_key": ("rare", r_server_key), "multimeter": ("rare", r_multimeter), "spider_ua": ("rare", r_spider_ua),
    "arcade_tokens": ("rare", r_arcade_tokens),
    "tieba": ("epic", r_tieba), "allnight": ("epic", r_allnight), "redbull": ("epic", r_redbull),
    "imagenet_disc": ("epic", r_imagenet_disc), "batchnorm_paper": ("epic", r_batchnorm_paper),
    "resnet_sticker": ("epic", r_resnet_sticker), "data_broker": ("epic", r_data_broker), "campus_vpn": ("epic", r_campus_vpn),
    "gold_cert": ("epic", r_gold_cert),
    "attention_scratch": ("legendary", r_attention_scratch), "alphago_book": ("legendary", r_alphago_book),
    "taihulight": ("legendary", r_taihulight), "advisor_page": ("legendary", r_advisor_page), "angel_term": ("legendary", r_angel_term),
}
GLOW = {"rare": (93, 168, 232), "epic": (169, 155, 242), "legendary": (255, 190, 40)}


def render_relic(rid):
    rarity, fn = RELICS[rid]
    ic = Icon()
    fn(ic)
    im = ic.im
    a = im.split()[3]
    bb = a.point(lambda v: 255 if v > 24 else 0).getbbox()
    x0, y0, x1, y1 = [v / S for v in bb]
    w, h = x1 - x0, y1 - y0
    sc = min(1.0, 196 / max(w, h)) if max(w, h) > 196 else 196 / max(w, h) if max(w, h) < 150 else 1.0
    sc = 196 / max(w, h)
    sc = min(sc, 1.25)
    nw, nh = int(im.width * sc), int(im.height * sc)
    big = im.resize((nw, nh), Image.LANCZOS)
    # centre the bbox
    bx = ((x0 + x1) / 2) * sc * S; by = ((y0 + y1) / 2) * sc * S
    canvas = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    canvas.alpha_composite(big, (int(N / 2 - bx), int(N / 2 - by)))
    al = canvas.split()[3]
    glow_col = GLOW[rarity]
    out = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    g1 = al.filter(ImageFilter.GaussianBlur(u(11))).point(lambda v: min(255, int(v * 1.5)))
    g2 = al.filter(ImageFilter.GaussianBlur(u(5))).point(lambda v: int(v * .8))
    for g, aa in ((g1, .75), (g2, .55)):
        layer = Image.new("RGBA", (N, N), glow_col + (0,))
        layer.putalpha(g.point(lambda v, aa=aa: int(v * aa)))
        out.alpha_composite(layer)
    # drop shadow of the object
    sh_a = al.point(lambda v: v * 130 // 255).filter(ImageFilter.GaussianBlur(u(4)))
    sh = Image.new("RGBA", (N, N), (0, 0, 0, 0)); sh.putalpha(sh_a)
    out.alpha_composite(ImageChops.offset(sh, 0, u(4)))
    out.alpha_composite(canvas)
    path = os.path.join(OUT, "relic", fname(rid) + ".png")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    out.resize((SIZE, SIZE), Image.LANCZOS).save(path, optimize=True)
    return path


# ═══════════════════════════ CARDS ════════════════════════════════════════════
# ═══════════════════════════ CARDS ════════════════════════════════════════════
# One round medal per achievement: the disc is colour-coded by the album category, the rim metal tells the rarity,
# and the glyph (white and one accent, with a soft deboss) says what the achievement is.

CAT_HUE = {"wall": "blue", "insight": "orange", "road": "brown", "cure": "green", "phenomenon": "teal",
           "data": "sky", "fun": "red", "life": "purple", "ending": "dark"}
RING_METAL = {0: ((250, 252, 255), (150, 160, 178), (82, 90, 106)),      # silver
              1: ((255, 238, 160), (232, 180, 60), (140, 92, 20)),       # gold
              2: ((226, 246, 255), (130, 200, 235), (50, 110, 160)),     # pearl blue
              3: ((236, 206, 255), (170, 110, 235), (84, 40, 150)),      # violet
              4: None}                                                    # rainbow

GC = {"w": ((255, 255, 255), (208, 220, 242)), "y": ((255, 244, 160), (250, 190, 30)), "r": ((255, 150, 140), (214, 44, 50)),
      "g": ((176, 242, 140), (56, 168, 60)), "b": ((150, 205, 255), (40, 120, 220)), "k": ((96, 102, 120), (30, 32, 42)),
      "o": ((255, 205, 130), (240, 130, 30)), "p": ((255, 180, 214), (235, 90, 150)), "s": ((226, 232, 242), (140, 150, 170))}


def gl(ic, m, c="w", sh=.5, rim=True):
    if sh:
        sm = ImageChops.offset(m, 0, u(3)).filter(ImageFilter.GaussianBlur(u(2.2)))
        ic.paint(sm, ic.dk, sh)
    lo, hi = GC[c]
    vg(ic, m, lo, hi)
    if rim:
        ic.rim(m, ic.dk, 1.5, .45)


def dt(ic, m, a=.9):
    ic.paint(m, ic.dk, a)


def rainbow_img(cx=128, cy=128):
    yy, xx = np.mgrid[0:N, 0:N]
    ang = (np.degrees(np.arctan2(yy - cy * S, xx - cx * S)) % 360) / 360
    h = ang
    i = (h * 6).astype(int) % 6; f = h * 6 - np.floor(h * 6)
    v = np.ones_like(h); p = .45 * np.ones_like(h); q = 1 - .55 * f; t = .45 + .55 * f
    rgb = np.zeros((N, N, 3))
    for k, (r_, g_, b_) in enumerate([(v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q)]):
        mk = i == k
        rgb[..., 0][mk] = r_[mk]; rgb[..., 1][mk] = g_[mk]; rgb[..., 2][mk] = b_[mk]
    return Image.fromarray(np.dstack([(rgb * 255).astype("uint8"), np.full((N, N), 255, "uint8")]), "RGBA")


def medal(cat, rarity):
    ic = Icon()
    lo, base, dk = hue(CAT_HUE[cat])
    ic.dk = tuple(int(c * .55) for c in dk) if CAT_HUE[cat] != "dark" else (10, 12, 18)
    ring = m_circle(128, 128, 112)
    shd(ic, ring, .3, 4, 3)
    metal = RING_METAL[rarity]
    if metal is None:
        ic.paint(ring, rainbow_img())
        ic.paint(ring, vgrad((255, 255, 255), (255, 255, 255), 0, 256), .0)
        ic.paint(m_ellipse(30, 20, 226, 130), (255, 255, 255), .0)
        ic.rim(ring, (90, 70, 120), 2.2, .6)
        ic.gloss(ring, .5, .5)
    else:
        ic.body(ring, metal)
    groove = m_circle(128, 128, 100)
    ic.paint(groove, vgrad(metal[2] if metal else (70, 60, 90), metal[0] if metal else (230, 220, 245), 28, 228), .85)
    disc = m_circle(128, 128, 94)
    if CAT_HUE[cat] == "dark":
        ic.paint(disc, rgrad((120, 128, 150), (30, 34, 46), 108, 90, 150))
    else:
        ic.paint(disc, rgrad(lo, dk, 108, 92, 165))
        ic.paint(disc, vgrad((255, 255, 255), (0, 0, 0), 34, 222), .0)
    ic.rim(disc, (0, 0, 0), 2, .35)
    ic.paint(cut(m_circle(128, 128, 88), m_circle(128, 128, 85)), (255, 255, 255), .35)
    ic.gloss(disc, .38, .46)
    return ic


# ── glyphs (drawn around (128, 128), within about +-64) ──────────────────────

def g_pick(ic):
    for k, y in enumerate((90, 128, 166)):
        gl(ic, m_circle(82, y, 14), "w")
        gl(ic, m_rrect(106, y - 8, 188, y + 8, 8), "w")
    gl(ic, m_circle(82, 128, 14), "y")
    dt(ic, m_line([(75, 128), (80, 134), (90, 122)], 4.4), .9)


def g_picword(ic):
    fr = m_rrect(64, 66, 146, 130, 8)
    gl(ic, fr, "w")
    inner = m_rrect(72, 74, 138, 122, 4)
    dt(ic, inner, .25)
    gl(ic, m_circle(92, 90, 8), "y", sh=0, rim=False)
    gl(ic, m_poly([(72, 122), (96, 98), (112, 112), (124, 100), (138, 122)]), "g", sh=0, rim=False)
    b = union(m_rrect(120, 112, 194, 168, 14), m_poly([(150, 160), (170, 160), (162, 184)]))
    gl(ic, b, "y")
    dt(ic, m_text("词", "cjk", 34, 157, 140), .85)


def g_sentence(ic):
    b = union(m_rrect(66, 70, 190, 160, 18), m_poly([(86, 150), (112, 150), (92, 186)]))
    gl(ic, b, "w")
    for k, x in enumerate((176, 156, 126)):
        dt(ic, m_line([(86, 94 + k * 24), (x if k else 170, 94 + k * 24)], 8), .8)
    gl(ic, m_circle(176, 140, 6), "y", sh=0, rim=False)


def g_translate(ic):
    b1 = union(m_rrect(62, 66, 142, 126, 14), m_poly([(76, 118), (96, 118), (72, 144)]))
    gl(ic, b1, "w")
    dt(ic, m_text("A", "black", 38, 102, 96), .85)
    b2 = union(m_rrect(114, 108, 194, 168, 14), m_poly([(160, 160), (180, 160), (184, 186)]))
    gl(ic, b2, "y")
    dt(ic, m_text("文", "cjk", 36, 154, 138), .85)


def g_all(ic):
    pts = [(128 + 42 * math.cos(math.radians(a)), 128 + 42 * math.sin(math.radians(a))) for a in range(-90, 270, 60)]
    em = blank()
    for p in pts:
        em = union(em, m_line([(128, 128), p], 6))
    gl(ic, em, "w", sh=.3, rim=False)
    for p in pts:
        gl(ic, m_circle(p[0], p[1], 16), "w")
    gl(ic, m_circle(128, 128, 22), "y")


def g_pretrain(ic):
    slabs = [(58, 140, 198, 172, "w"), (74, 102, 182, 134, "w"), (92, 64, 164, 96, "y")]
    for x0, y0, x1, y1, c in slabs:
        gl(ic, m_rrect(x0, y0, x1, y1, 9), c)
        dt(ic, m_rrect(x0 + 10, y0 + (y1 - y0) / 2 - 2.5, x0 + 30, y0 + (y1 - y0) / 2 + 2.5, 2), .5)
        dt(ic, m_circle(x1 - 14, y0 + (y1 - y0) / 2, 3.5), .5)


def g_scale(ic):
    gl(ic, m_rrect(120, 68, 136, 176, 5), "w")
    gl(ic, m_rrect(92, 172, 164, 188, 7), "w")
    beam = m_line([(66, 86), (190, 86)], 9)
    gl(ic, beam, "w")
    gl(ic, m_circle(128, 76, 11), "y")
    for x in (72, 184):
        dt(ic, union(m_line([(x, 90), (x - 22, 128)], 3), m_line([(x, 90), (x + 22, 128)], 3)), .8)
        pan = m_pie(x, 124, 28, 0, 180)
        gl(ic, pan, "w")


def g_flash(ic):
    glass = union(m_circle(128, 108, 38), m_poly([(104, 128), (152, 128), (146, 156), (110, 156)]))
    gl(ic, glass, "y")
    gl(ic, m_rrect(108, 156, 148, 176, 5), "w")
    dt(ic, m_line([(110, 166), (146, 166)], 3), .6)
    ic.paint(m_ellipse(104, 82, 124, 104), (255, 255, 255), .55)
    dt(ic, m_line([(118, 120), (128, 104), (138, 120)], 3), .55)
    for a in (-90 - 70, -90 - 38, -90 + 38, -90 + 70, 180, 0):
        c, s_ = math.cos(math.radians(a)), math.sin(math.radians(a))
        gl(ic, m_line([(128 + 54 * c, 106 + 54 * s_), (128 + 70 * c, 106 + 70 * s_)], 6), "y", sh=0, rim=False)


def g_bricks(ic):
    rows = [(76, [(66, 108), (112, 150), (154, 190)]), (104, [(66, 88), (92, 134), (138, 172), (176, 190)]),
            (132, [(66, 108), (112, 150), (154, 190)]), (160, [(66, 88), (92, 134), (138, 172), (176, 190)])]
    for k, (y, xs) in enumerate(rows):
        for (a, b) in xs:
            gl(ic, m_rrect(a, y, b, y + 24, 4), "o" if (a + k) % 3 == 0 else "w", sh=.3)


def g_pill(ic):
    s = Icon()
    left = m_rrect(54, 104, 134, 152, 24)
    right = m_rrect(122, 104, 202, 152, 24)
    pill = union(left, right)
    sm = pill.filter(ImageFilter.GaussianBlur(u(2.2)))
    s.paint(ImageChops.offset(sm, 0, u(3)), ic.dk, .5)
    s.paint(left, vgrad((255, 255, 255), (206, 218, 240), 104, 152))
    s.paint(intersect(right, m_rect(128, 100, 210, 160)), vgrad((255, 150, 140), (214, 44, 50), 104, 152))
    s.rim(pill, ic.dk, 1.5, .45)
    s.paint(m_ellipse(64, 110, 120, 120), (255, 255, 255), .45)
    ic.im.alpha_composite(rot_im(s.im, 40))
    gl(ic, union(m_rrect(146, 60, 158, 90, 3), m_rrect(131, 75, 173, 87, 3)), "y", sh=0, rim=False)


def g_heartbeat(ic):
    h = m_heart(128, 122, 130)
    gl(ic, h, "r")
    dt(ic, m_line([(70, 122), (100, 122), (112, 98), (128, 150), (142, 112), (150, 122), (186, 122)], 6), .0)
    ic.paint(m_line([(66, 122), (98, 122), (110, 96), (126, 152), (140, 110), (148, 122), (190, 122)], 7), (255, 255, 255))


def g_eye(ic):
    eye = union(m_poly([(62, 128)] + [(62 + 132 * i / 20, 128 - 46 * math.sin(math.pi * i / 20)) for i in range(21)] + [(194, 128)]),
                m_poly([(62, 128)] + [(62 + 132 * i / 20, 128 + 46 * math.sin(math.pi * i / 20)) for i in range(21)] + [(194, 128)]))
    gl(ic, eye, "w")
    gl(ic, m_circle(128, 128, 30), "y", sh=0)
    dt(ic, m_circle(128, 128, 14), 1)
    ic.paint(m_circle(120, 120, 6), (255, 255, 255))


def g_funnel(ic):
    f = m_poly([(64, 90), (192, 90), (146, 150), (146, 190), (110, 190), (110, 150)])
    gl(ic, f, "w")
    dt(ic, m_ellipse(70, 86, 186, 100), .55)
    for (x, y, c) in [(92, 62, "r"), (124, 52, "r"), (158, 64, "r")]:
        gl(ic, m_rrect(x - 9, y - 9, x + 9, y + 9, 3), c, sh=.3)
    gl(ic, m_circle(128, 206, 9), "y", sh=.3)
    dt(ic, union(m_line([(84, 40), (100, 54)], 0.1), blank()), 0)


def g_flywheel(ic):
    gear = m_gear(128, 128, 48, 8, .24, .36)
    gl(ic, gear, "w")
    gl(ic, m_circle(128, 128, 14), "y", sh=0)
    for a0 in (200, 20):
        arc = m_arc(128, 128, 64, a0, a0 + 100, 10)
        gl(ic, arc, "y", sh=.35)
        a = math.radians(a0 + 104)
        x, y = 128 + 64 * math.cos(a), 128 + 64 * math.sin(a)
        d = a + math.pi / 2
        tip = (x + 16 * math.cos(d), y + 16 * math.sin(d))
        p1 = (x + 15 * math.cos(a), y + 15 * math.sin(a)); p2 = (x - 15 * math.cos(a), y - 15 * math.sin(a))
        gl(ic, m_poly([p1, tip, p2]), "y", sh=.35)


def g_boom(ic):
    gl(ic, m_star(128, 128, 68, 40, 12, -90), "y")
    gl(ic, m_star(128, 128, 48, 28, 12, -75), "o", sh=0, rim=False)
    dt(ic, m_text("NaN", "black", 26, 128, 130), .95)


def g_bolt(ic):
    b = m_poly([(144, 56), (88, 132), (122, 132), (104, 200), (172, 114), (136, 114), (156, 56)])
    gl(ic, b, "y")
    for k, y in enumerate((88, 124, 160)):
        gl(ic, m_line([(60 - (k % 2) * -8, y), (86, y)], 6), "w", sh=.25, rim=False)


def g_cards(ic):
    for (deg, dx, c) in [(-22, -22, "s"), (0, 0, "w"), (22, 22, "w")]:
        s = Icon(); s.dk = ic.dk
        gl(s, m_rrect(94, 70, 164, 170, 10), c)
        if deg == 22:
            gl(s, m_star(129, 120, 24, 10), "y")
        ic.im.alpha_composite(ImageChops.offset(rot_im(s.im, -deg, 128, 170), u(dx * .5), u(4)))


def g_busy(ic):
    b = union(m_rrect(62, 76, 168, 150, 18), m_poly([(76, 142), (102, 142), (74, 172)]))
    gl(ic, b, "w")
    for k in range(3):
        dt(ic, m_circle(88 + k * 26, 113, 7), .8)
    clk = m_circle(166, 150, 32)
    gl(ic, clk, "y")
    gl(ic, m_circle(166, 150, 24), "w", sh=0, rim=False)
    dt(ic, union(m_line([(166, 150), (166, 134)], 4), m_line([(166, 150), (178, 156)], 4)), .9)


def g_chip(ic):
    c = m_circle(128, 128, 64)
    gl(ic, c, "r")
    for k in range(8):
        a = math.radians(k * 45)
        stripe = m_poly([(128 + 52 * math.cos(a - .17), 128 + 52 * math.sin(a - .17)), (128 + 66 * math.cos(a - .17), 128 + 66 * math.sin(a - .17)),
                         (128 + 66 * math.cos(a + .17), 128 + 66 * math.sin(a + .17)), (128 + 52 * math.cos(a + .17), 128 + 52 * math.sin(a + .17))])
        ic.paint(intersect(stripe, c), (255, 255, 255), .95)
    ring = m_circle(128, 128, 44)
    gl(ic, ring, "w", sh=0)
    dt(ic, m_star(128, 129, 26, 12), .9)


def card_face(ic, cx, cy, rot, label, suit, tint="w"):
    s = Icon(); s.dk = ic.dk
    gl(s, m_rrect(cx - 34, cy - 50, cx + 34, cy + 50, 8), "w")
    s.paint(m_text(label, "black", 40, cx - 14, cy - 24), (30, 32, 42) if suit != "h" else (214, 44, 50))
    if suit == "h":
        s.paint(m_heart(cx + 4, cy + 18, 40), (214, 44, 50))
    else:
        s.paint(m_star(cx + 4, cy + 18, 20, 8, 4, -90), (30, 32, 42))
    ic.im.alpha_composite(rot_im(s.im, rot, cx, cy))


def g_blackjack(ic):
    card_face(ic, 100, 130, 14, "K", "s")
    card_face(ic, 150, 124, -10, "A", "h")


def g_slot(ic):
    gl(ic, m_rrect(58, 70, 176, 176, 14), "y")
    gl(ic, m_rrect(68, 84, 166, 150, 6), "k", sh=0)
    for k in range(3):
        x = 72 + k * 31
        gl(ic, m_rrect(x, 88, x + 28, 146, 4), "w", sh=0, rim=False)
        ic.paint(m_text("7", "black", 40, x + 14, 118), (214, 44, 50))
    gl(ic, m_rrect(178, 96, 188, 150, 4), "w", sh=.3)
    gl(ic, m_circle(183, 90, 12), "r")
    gl(ic, m_rrect(78, 156, 156, 168, 4), "w", sh=0, rim=False)


def g_wallet(ic):
    gl(ic, m_rrect(60, 84, 196, 170, 16), "w")
    flap = m_rrect(122, 114, 206, 150, 12)
    gl(ic, flap, "y")
    dt(ic, m_circle(148, 132, 7), .85)
    ic.paint(m_text("0", "black", 34, 92, 130), (214, 44, 50))
    gl(ic, m_rrect(66, 70, 160, 96, 8), "s", sh=.3, rim=False)


def g_dice(ic):
    s = Icon(); s.dk = ic.dk
    gl(s, m_rrect(66, 66, 142, 142, 14), "w")
    for (x, y) in [(86, 86), (122, 86), (104, 104), (86, 122), (122, 122)]:
        s.paint(m_circle(x, y, 6.5), (40, 44, 56))
    ic.im.alpha_composite(rot_im(s.im, -14, 104, 104))
    gl(ic, m_poly([(150, 112), (188, 112), (188, 146), (206, 146), (169, 196), (132, 146), (150, 146)]), "r")


def g_crown(ic):
    c = m_poly([(60, 84), (92, 120), (128, 68), (164, 120), (196, 84), (184, 168), (72, 168)])
    gl(ic, c, "y")
    gl(ic, m_rrect(72, 168, 184, 186, 5), "o")
    for (x, y) in [(60, 80), (128, 62), (196, 80)]:
        gl(ic, m_circle(x, y, 10), "w", sh=.3)
    for x in (100, 128, 156):
        gl(ic, m_circle(x, 150, 7), "r", sh=0, rim=False)


def g_chat_heart(ic):
    b = union(m_rrect(62, 70, 194, 160, 26), m_poly([(86, 150), (116, 150), (86, 186)]))
    gl(ic, b, "w")
    gl(ic, m_heart(128, 112, 54), "r", sh=.3)


def g_redenv(ic):
    e = m_rrect(82, 62, 174, 196, 10)
    gl(ic, e, "r")
    flap = m_poly([(82, 62), (174, 62), (174, 94), (128, 124), (82, 94)])
    ic.paint(flap, vgrad((255, 130, 120), (200, 30, 40), 62, 124), .9)
    ic.rim(flap, (110, 10, 20), 1.5, .4)
    gl(ic, m_circle(128, 124, 16), "y")
    dt(ic, m_circle(128, 124, 6), .6)
    ic.paint(m_text("520", "black", 24, 128, 168), (255, 226, 130))


def g_snow(ic):
    arms = blank()
    for k in range(6):
        a = math.radians(k * 60 - 90)
        ex, ey = 128 + 64 * math.cos(a), 128 + 64 * math.sin(a)
        arms = union(arms, m_line([(128, 128), (ex, ey)], 8))
        for t in (.5, .75):
            bx, by = 128 + 64 * t * math.cos(a), 128 + 64 * t * math.sin(a)
            for s_ in (-1, 1):
                b = a + s_ * math.radians(50)
                arms = union(arms, m_line([(bx, by), (bx + 20 * math.cos(b), by + 20 * math.sin(b))], 6))
    gl(ic, arms, "w")
    gl(ic, m_circle(128, 128, 11), "b")


def g_heart_band(ic):
    gl(ic, m_heart(128, 124, 134), "w")
    s = Icon(); s.dk = ic.dk
    gl(s, m_rrect(76, 112, 180, 140, 8), "y")
    for (x, y) in [(110, 122), (110, 130), (146, 122), (146, 130), (128, 122), (128, 130)]:
        s.paint(m_circle(x, y, 1.8), ic.dk, .6)
    s.paint(m_rect(112, 112, 144, 140), (255, 255, 255), .45)
    ic.im.alpha_composite(rot_im(s.im, -40, 128, 126))


def g_calendar(ic):
    gl(ic, m_rrect(64, 70, 192, 190, 12), "w")
    top = intersect(m_rrect(64, 70, 192, 190, 12), m_rect(60, 70, 196, 102))
    ic.paint(top, vgrad((255, 150, 140), (214, 44, 50), 70, 102))
    for x in (92, 164):
        gl(ic, m_rrect(x - 5, 58, x + 5, 84, 4), "s", sh=.3)
    for r_ in range(3):
        for c in range(4):
            x, y = 82 + c * 30, 116 + r_ * 24
            if (r_, c) == (1, 2):
                gl(ic, m_circle(x + 8, y + 6, 15), "r")
                ic.paint(m_text("!", "black", 20, x + 8, y + 7), (255, 255, 255))
            else:
                dt(ic, m_rrect(x, y, x + 16, y + 12, 3), .45)


def g_phone(ic):
    gl(ic, m_rrect(86, 54, 170, 204, 18), "o")
    scr = m_rrect(94, 72, 162, 184, 6)
    gl(ic, scr, "k", sh=0, rim=False)
    ic.paint(m_rrect(98, 76, 130, 120, 4), (255, 255, 255), .12)
    gl(ic, m_circle(142, 62, 4), "k", sh=0, rim=False)
    gl(ic, m_circle(128, 194, 6), "w", sh=0)
    ic.paint(m_text("7", "black", 54, 128, 128), (255, 190, 90))


def sparkle(ic, cx, cy, r, c="y"):
    gl(ic, m_star(cx, cy, r, r * .22, 4, -90), c, sh=.3, rim=False)


def g_sweet(ic):
    gl(ic, m_heart(128, 124, 124), "p")
    ic.paint(m_ellipse(84, 84, 112, 104), (255, 255, 255), .55)
    sparkle(ic, 66, 72, 16); sparkle(ic, 192, 100, 12); sparkle(ic, 178, 180, 15, "w")


def g_fireworks(ic):
    def burst(cx, cy, r, c):
        arms = blank()
        for k in range(10):
            a = math.radians(k * 36)
            arms = union(arms, m_line([(cx + r * .35 * math.cos(a), cy + r * .35 * math.sin(a)), (cx + r * math.cos(a), cy + r * math.sin(a))], 5))
        gl(ic, arms, c, sh=.35, rim=False)
        for k in range(10):
            a = math.radians(k * 36)
            gl(ic, m_circle(cx + r * 1.12 * math.cos(a), cy + r * 1.12 * math.sin(a), 5), c, sh=0, rim=False)
    burst(128, 106, 40, "y")
    burst(72, 164, 22, "w")
    burst(184, 158, 24, "r")
    gl(ic, m_circle(128, 106, 9), "w", sh=0, rim=False)


def g_heart_q(ic):
    gl(ic, m_heart(128, 122, 130), "w")
    dt(ic, m_text("?", "black", 70, 128, 122), .9)


def g_bubble_x(ic):
    b = union(m_rrect(62, 70, 194, 160, 26), m_poly([(86, 150), (116, 150), (86, 186)]))
    gl(ic, b, "w")
    gl(ic, union(m_line([(100, 94), (156, 136)], 12), m_line([(156, 94), (100, 136)], 12)), "r", sh=.3)


def g_q3(ic):
    for (x, y, r, c) in [(92, 96, 28, "w"), (170, 96, 28, "w"), (131, 164, 32, "y")]:
        gl(ic, m_circle(x, y, r), c)
        dt(ic, m_text("?", "black", r * 1.5, x, y + 1), .9)


def g_vr(ic):
    strap = m_arc(128, 128, 56, 200, 340, 14)
    gl(ic, strap, "s", sh=.3)
    gl(ic, m_rrect(58, 100, 198, 168, 24), "w")
    dt(ic, m_rrect(66, 108, 190, 160, 18), .55)
    for x in (98, 158):
        gl(ic, m_circle(x, 134, 20), "b", sh=0)
        ic.paint(m_circle(x - 6, 128, 6), (255, 255, 255), .6)
    dt(ic, m_rrect(122, 124, 134, 144, 3), .8)


def g_key(ic):
    s = Icon(); s.dk = ic.dk
    shaft = m_rrect(104, 118, 206, 138, 6)
    teeth = union(m_rect(170, 134, 184, 160), m_rect(190, 134, 202, 152))
    head = cut(m_circle(80, 128, 36), m_circle(80, 128, 14))
    gl(s, union(shaft, teeth, head), "y")
    ic.im.alpha_composite(rot_im(s.im, -35))
    sparkle(ic, 178, 66, 14, "w"); sparkle(ic, 66, 188, 11, "w")


def g_suns(ic):
    for (x, y, r, c) in [(96, 96, 36, "y"), (170, 84, 24, "o"), (176, 160, 18, "w")]:
        for k in range(12):
            a = math.radians(k * 30)
            ic.paint(m_line([(x + (r + 4) * math.cos(a), y + (r + 4) * math.sin(a)), (x + (r + 11) * math.cos(a), y + (r + 11) * math.sin(a))], 4), GC[c][1], .85)
        gl(ic, m_circle(x, y, r), c)
    gl(ic, m_circle(88, 172, 14), "k")


def g_sprout(ic):
    gl(ic, m_ellipse(66, 164, 190, 200), "o", sh=.3)
    gl(ic, m_line(bez((128, 168), (128, 140), (126, 118), (128, 96), 16), 9), "g")
    leaf1 = m_poly(bez((128, 118), (100, 80), (72, 92), (66, 100), 14) + bez((66, 100), (70, 130), (100, 138), (128, 118), 14))
    leaf2 = m_poly(bez((128, 100), (156, 56), (186, 64), (192, 72), 14) + bez((192, 72), (190, 110), (160, 118), (128, 100), 14))
    gl(ic, leaf1, "g"); gl(ic, leaf2, "w")


def g_silence(ic):
    b = union(m_rrect(70, 76, 186, 156, 24), m_poly([(90, 148), (116, 148), (90, 178)]))
    gl(ic, b, "w")
    for k in range(3):
        dt(ic, m_circle(102 + k * 26, 116, 7), .7)
    ring = cut(m_circle(128, 128, 64), m_circle(128, 128, 52))
    gl(ic, union(ring, m_line([(84, 84), (172, 172)], 12)), "r", sh=.4)


def g_bug(ic):
    legs = blank()
    for y in (108, 128, 148):
        legs = union(legs, m_line([(100, y), (68, y - 14 + (y - 128) * .5)], 6), m_line([(156, y), (188, y - 14 + (y - 128) * .5)], 6))
    legs = union(legs, m_line([(116, 76), (104, 56)], 5), m_line([(140, 76), (152, 56)], 5))
    gl(ic, legs, "k", sh=.3, rim=False)
    gl(ic, m_ellipse(96, 88, 160, 190), "r")
    gl(ic, m_circle(128, 80, 20), "k")
    dt(ic, m_line([(128, 96), (128, 188)], 3), .8)
    for (x, y) in [(112, 120), (144, 120), (110, 152), (146, 152)]:
        gl(ic, m_circle(x, y, 8), "k", sh=0, rim=False)


def g_wallfacer(ic):
    gl(ic, m_rrect(58, 60, 198, 196, 8), "w")
    for r_ in range(5):
        y = 66 + r_ * 26
        dt(ic, m_line([(60, y + 24), (196, y + 24)], 2.4), .5)
        for x in (110 if r_ % 2 else 90, 162 if r_ % 2 else 150):
            dt(ic, m_line([(x, y), (x, y + 24)], 2.4), .5)
    gl(ic, m_circle(128, 118, 17), "k")
    gl(ic, union(m_ellipse(92, 134, 164, 220), m_rect(92, 170, 164, 196)), "k")


def g_sophon(ic):
    sp = m_circle(114, 114, 56)
    ic.paint(sp, rgrad((150, 160, 190), (24, 28, 44), 96, 92, 90))
    ic.rim(sp, ic.dk, 1.6, .6)
    for r_ in (22, 40):
        ic.paint(cut(m_circle(114, 114, r_ + 2), m_circle(114, 114, r_)), (190, 215, 255), .75)
    ic.paint(union(m_line([(114, 60), (114, 168)], 2), m_line([(60, 114), (168, 114)], 2)), (190, 215, 255), .6)
    ic.paint(m_ellipse(84, 72, 120, 98), (255, 255, 255), .45)
    gl(ic, m_arc(165, 134, 17, 180, 360, 9), "s", sh=.3)
    gl(ic, m_rrect(138, 132, 192, 184, 8), "y")
    dt(ic, m_circle(165, 156, 6), .9)


def g_trophy(ic):
    cup = union(m_rrect(86, 62, 170, 118, 6), m_pie(128, 108, 42, 0, 180), m_rect(86, 62, 170, 108))
    for sgn in (-1, 1):
        gl(ic, cut(m_circle(128 + sgn * 46, 86, 20), m_circle(128 + sgn * 46, 86, 11)), "y", sh=.3)
    gl(ic, cup, "y")
    gl(ic, m_rrect(118, 148, 138, 172, 4), "y")
    gl(ic, m_rrect(92, 170, 164, 190, 7), "o")
    ic.paint(m_ellipse(98, 70, 112, 104), (255, 255, 255), .55)
    ic.paint(m_star(128, 100, 18, 8), (255, 255, 255), .9)


def g_go(ic):
    gl(ic, m_rrect(62, 62, 194, 194, 8), "o")
    for k in range(5):
        p = 80 + k * 24
        dt(ic, union(m_line([(p, 78), (p, 178)], 1.8, False), m_line([(78, p), (178, p)], 1.8, False)), .55)
    for (x, y, w) in [(104, 104, 0), (128, 104, 1), (128, 128, 0), (152, 128, 1), (104, 152, 1), (152, 152, 0), (80, 128, 1)]:
        m = m_circle(x, y, 11)
        shd(ic, m, .45, 2, 1.5)
        ic.paint(m, rgrad((255, 255, 255), (196, 202, 216), x - 3, y - 4, 20) if w else rgrad((110, 114, 130), (10, 10, 14), x - 3, y - 4, 18))


def flame(ic, cx, cy, h, w, c="o"):
    f = m_poly(bez((cx, cy - h), (cx + w * .1, cy - h * .55), (cx + w, cy - h * .3), (cx + w * .7, cy + h * .25), 14) +
               bez((cx + w * .7, cy + h * .25), (cx + w * .5, cy + h * .6), (cx - w * .5, cy + h * .6), (cx - w * .7, cy + h * .25), 14) +
               bez((cx - w * .7, cy + h * .25), (cx - w, cy - h * .3), (cx - w * .2, cy - h * .5), (cx, cy - h), 14))
    gl(ic, f, c)
    return f


def gpu_card(ic, cx, cy, w=130, h=64):
    x0, y0 = cx - w / 2, cy - h / 2
    gl(ic, m_rrect(x0, y0, x0 + w, y0 + h, 8), "w")
    for fx in (x0 + w * .3, x0 + w * .7):
        gl(ic, m_circle(fx, cy, h * .36), "k", sh=0, rim=False)
        blades = blank()
        for k in range(6):
            blades = union(blades, rotate_mask(m_ellipse(fx - 4, cy - h * .32, fx + 4, cy - 2), k * 60, fx, cy))
        ic.paint(blades, (200, 208, 224), .9)
    for k in range(7):
        ic.paint(m_rect(x0 + 16 + k * 14, y0 + h, x0 + 22 + k * 14, y0 + h + 9), GC["y"][1])


def g_burn(ic):
    gpu_card(ic, 128, 160, 140, 62)
    flame(ic, 128, 84, 52, 34, "o")
    flame(ic, 128, 94, 28, 18, "y")
    flame(ic, 80, 112, 26, 16, "o")
    flame(ic, 178, 112, 26, 16, "o")


def g_titan(ic):
    gpu_card(ic, 128, 150, 140, 62)
    halo = cut(m_ellipse(88, 52, 168, 80), m_ellipse(98, 58, 158, 74))
    gl(ic, halo, "y", sh=.35)
    for a in (-60, -30, 0, 30, 60):
        c, s_ = math.sin(math.radians(a)), -math.cos(math.radians(a))
        ic.paint(m_line([(128 + 84 * c, 112 + 70 * s_ * .9), (128 + 112 * c * 1.0, 112 + 96 * s_ * .9)], 4), GC["y"][0], .0)
    for a in (-50, -25, 25, 50):
        c = math.sin(math.radians(a)); s_ = math.cos(math.radians(a))
        ic.paint(m_line([(128 + 36 * c, 112 - 40 * s_ * .6 + 10), (128 + 76 * c, 112 - 76 * s_ * .6 + 10)], 5), GC["y"][0], .9)


def g_snail(ic):
    gl(ic, m_poly(bez((62, 176), (60, 148), (100, 148), (190, 150), 12) + [(196, 176), (190, 186), (62, 186)]), "y")
    gl(ic, m_line([(172, 154), (188, 110)], 8), "y", sh=.3)
    gl(ic, m_line([(190, 158), (210, 122)], 8), "y", sh=.3)
    for (x, y) in ((188, 108), (210, 120)):
        gl(ic, m_circle(x, y, 6), "w", sh=0)
    shell = m_circle(112, 118, 46)
    gl(ic, shell, "w")
    pts = []
    for i in range(60):
        t = i / 59
        rr = 40 * (1 - t) + 4
        a = t * 4.4 * math.pi + 1
        pts.append((112 + rr * math.cos(a), 118 + rr * math.sin(a)))
    dt(ic, m_line(pts, 6), .8)


def g_power(ic):
    gl(ic, m_arc(128, 132, 52, 120, 420, 16), "w")
    gl(ic, m_rrect(120, 54, 136, 134, 8), "w")


def g_blank(ic):
    pg = m_poly([(78, 56), (150, 56), (182, 88), (182, 200), (78, 200)])
    ic.paint(pg, (255, 255, 255), .22)
    ring = cut(pg, grow(pg, -5))
    ring_d = blank()
    # dashed outline: carve gaps out of the ring
    import itertools
    gap = blank()
    from PIL import ImageDraw as _D
    d = _D.Draw(gap)
    perim = [(78, 56), (150, 56), (182, 88), (182, 200), (78, 200), (78, 56)]
    for (a, b) in zip(perim, perim[1:]):
        L = math.hypot(b[0] - a[0], b[1] - a[1])
        n = int(L / 16)
        for i in range(n):
            t0, t1 = (i * 16 + 4) / L, (i * 16 + 14) / L
            d.line([(u(a[0] + (b[0] - a[0]) * t0), u(a[1] + (b[1] - a[1]) * t0)), (u(a[0] + (b[0] - a[0]) * t1), u(a[1] + (b[1] - a[1]) * t1))], fill=255, width=u(7))
    gl(ic, intersect(grow(ring, 3), gap), "w", sh=.4)
    dt(ic, m_text("?", "black", 72, 130, 134), .0)
    ic.paint(m_text("?", "black", 76, 130, 132), (255, 255, 255), .95)


def g_fork(ic):
    stem = m_line([(128, 196), (128, 132)], 16, False)
    left = m_line([(128, 132), (84, 96)], 16)
    right = m_line([(128, 132), (172, 96)], 16)
    gl(ic, union(stem, left, right), "w")
    gl(ic, m_poly([(60, 74), (96, 70), (80, 112)]), "w")
    gl(ic, m_poly([(196, 74), (160, 70), (176, 112)]), "y")
    gl(ic, m_circle(128, 200, 10), "y", sh=.3)


# books
def g_book_base(ic, c="w"):
    return None


def g_sanzijing(ic):
    gl(ic, m_rrect(72, 56, 184, 200, 10), "w")
    dt(ic, m_rect(72, 56, 86, 200), .5)
    for y in (82, 120, 158):
        dt(ic, m_line([(104, y), (164, y)], 7), .9)
    ic.paint(m_text("", "cjk", 10, 0, 0), (0, 0, 0), 0)
    gl(ic, m_circle(168, 176, 12), "r", sh=.3)


def g_lunyu(ic):
    for k in range(6):
        x = 70 + k * 24
        gl(ic, m_rrect(x, 56, x + 18, 196, 5), "y" if k % 2 == 0 else "w", sh=.35)
        dt(ic, m_line([(x + 9, 74), (x + 9, 90)], 3), .5)
        dt(ic, m_line([(x + 9, 104), (x + 9, 124)], 3), .5)
    gl(ic, m_rrect(60, 96, 196, 106, 4), "r", sh=.4)
    gl(ic, m_rrect(60, 150, 196, 160, 4), "r", sh=.4)


def g_tangshi(ic):
    gl(ic, m_rrect(74, 62, 186, 196, 10), "w")
    for k, x in enumerate((168, 148, 128)):
        dt(ic, m_line([(x, 110), (x, 180)], 6), .8)
    moon = cut(m_circle(112, 108, 34), m_circle(126, 98, 30))
    gl(ic, moon, "y", sh=.35)
    ic.paint(m_circle(98, 140, 3), GC["y"][1], .0)
    sparkle(ic, 96, 78, 9, "y")


def g_xiyouji(ic):
    cloud = union(m_circle(92, 160, 28), m_circle(128, 148, 36), m_circle(166, 160, 26), m_rrect(80, 160, 180, 188, 14))
    gl(ic, cloud, "w")
    st = Icon(); st.dk = ic.dk
    gl(st, m_rrect(120, 40, 136, 200, 6), "y")
    gl(st, m_rrect(116, 40, 140, 66, 6), "r"); gl(st, m_rrect(116, 174, 140, 200, 6), "r")
    ic.im.alpha_composite(rot_im(st.im, 28, 128, 128))


def g_shuihu(ic):
    for deg in (-38, 38):
        s = Icon(); s.dk = ic.dk
        gl(s, m_poly([(128, 40), (140, 62), (140, 148), (116, 148), (116, 62)]), "w")
        gl(s, m_rrect(98, 148, 158, 162, 5), "y")
        gl(s, m_rrect(120, 162, 136, 196, 5), "r")
        ic.im.alpha_composite(rot_im(s.im, deg, 128, 128))
    gl(ic, m_circle(128, 128, 14), "y")


def g_sanguo(ic):
    fan = blank()
    for k in range(7):
        f = m_ellipse(128 - 15, 164 - 102, 128 + 15, 164 - 10)
        fan = union(fan, rotate_mask(f, -(k - 3) * 24, 128, 164))
    gl(ic, fan, "w")
    for k in range(7):
        dt(ic, rotate_mask(m_line([(128, 164 - 30), (128, 164 - 92)], 2.2), -(k - 3) * 24, 128, 164), .35)
    gl(ic, m_rrect(120, 160, 136, 214, 7), "y")
    gl(ic, m_circle(128, 162, 13), "r")


def g_nahan(ic):
    cone = m_poly([(92, 108), (180, 62), (180, 158), (92, 128)])
    gl(ic, cone, "w")
    gl(ic, m_rrect(62, 104, 100, 134, 8), "y")
    gl(ic, m_rrect(176, 58, 194, 164, 8), "s")
    gl(ic, m_rrect(94, 128, 118, 176, 7), "s", sh=.3)
    for k, r_ in enumerate((22, 36)):
        arc = m_arc(194, 112, r_, -50, 50, 6)
        gl(ic, arc, "y", sh=.3, rim=False)


def g_hongloumeng(ic):
    for dx in (-10, 0, 10):
        ic.paint(m_line([(128, 176), (128 + dx * 1.4, 208)], 4.5), (255, 110, 100), .98)
    disc = m_circle(128, 118, 56)
    gl(ic, disc, "g")
    dt(ic, cut(m_circle(128, 118, 46), m_circle(128, 118, 44)), .35)
    ic.paint(m_text("玉", "cjk", 62, 128, 120), (255, 255, 255), .95)
    gl(ic, m_circle(128, 62, 9), "y", sh=.3)
    gl(ic, m_circle(128, 178, 8), "r", sh=.3)


def g_xor(ic):
    for (x, y, c) in [(88, 88, "w"), (168, 88, "y"), (88, 168, "y"), (168, 168, "w")]:
        gl(ic, m_circle(x, y, 20), c)
    ic.paint(m_line([(62, 154), (194, 102)], 6), (255, 80, 80), .95)
    dt(ic, m_line([(62, 154), (194, 102)], 6), .0)


def g_vanish(ic):
    for k in range(5):
        h = 100 - k * 19
        x = 68 + k * 28
        s = m_rrect(x, 176 - h, x + 22, 176, 5)
        gl(ic, s, "w" if k < 2 else "s", sh=.35)
    gl(ic, m_poly([(62, 186), (194, 186), (194, 194), (62, 194)]), "s", sh=0, rim=False)
    gl(ic, m_poly([(152, 66), (188, 66), (188, 76), (170, 92)]), "y", sh=.3)


def g_memorize(ic):
    gl(ic, m_rrect(60, 76, 130, 170, 8), "w")
    gl(ic, m_rrect(132, 76, 196, 170, 8), "w")
    dt(ic, m_line([(78, 108), (94, 124), (116, 94)], 7), .9)
    gl(ic, union(m_line([(150, 100), (178, 128)], 9), m_line([(178, 100), (150, 128)], 9)), "r", sh=.3)
    dt(ic, m_line([(76, 146), (112, 146)], 5), .4)
    dt(ic, m_line([(150, 146), (180, 146)], 5), .4)


def g_grok(ic):
    gl(ic, m_rrect(60, 60, 196, 190, 10), "w")
    ax = union(m_line([(78, 76), (78, 174), (180, 174)], 4, False))
    dt(ic, ax, .8)
    dt(ic, m_line([(80, 160), (130, 160), (130, 100), (178, 96)], 7), .0)
    ic.paint(m_line([(84, 160), (128, 160), (136, 94), (180, 92)], 8), GC["r"][1], .98)
    sparkle(ic, 150, 78, 14, "y")


def g_generalize(ic):
    for r_, c in ((60, "w"), (44, "r"), (28, "w"), (12, "r")):
        gl(ic, m_circle(120, 136, r_), c, sh=.3 if r_ == 60 else 0, rim=(r_ == 60))
    ar = Icon(); ar.dk = ic.dk
    gl(ar, m_line([(122, 134), (198, 58)], 6), "y", sh=.3)
    gl(ar, m_poly([(186, 54), (206, 50), (202, 70)]), "y", sh=.3)
    ic.im.alpha_composite(ar.im)


def g_deeper(ic):
    for k in range(5):
        y = 64 + k * 24
        w = 120 - k * 6
        gl(ic, m_rrect(128 - w / 2 - 14, y, 128 + w / 2 - 14, y + 16, 6), "w" if k < 4 else "s", sh=.3)
    gl(ic, m_poly([(176, 80), (200, 80), (200, 130), (214, 130), (188, 172), (162, 130), (176, 130)]), "r")


def g_amnesia(ic):
    for k in range(6):
        x = 66 + k * 26
        s = m_rrect(x, 112, x + 20, 148, 5)
        gl(ic, s, "w", sh=.3 if k > 1 else 0, rim=k > 1)
        if k < 3:
            ic.paint(s, ic.dk, [.85, .6, .3][k])
    ic.paint(m_text("?", "black", 40, 82, 130), (255, 255, 255), .0)
    sparkle(ic, 82, 80, 0.1, "y")
    ic.paint(m_text("?", "black", 36, 92, 86), GC["y"][0], .95)


def g_forget(ic):
    gl(ic, m_rrect(66, 62, 156, 186, 8), "w")
    for k, x in enumerate((156, 150, 120, 96, 90)):
        dt(ic, m_line([(80, 84 + k * 22), (x - 30, 84 + k * 22)], 6), .7 if k < 3 else .25)
    e = Icon(); e.dk = ic.dk
    gl(e, m_rrect(100, 96, 190, 128, 8), "r")
    gl(e, intersect(m_rrect(100, 96, 190, 128, 8), m_rect(100, 96, 130, 128)), "w", sh=0)
    e.rim(m_rrect(100, 96, 190, 128, 8), ic.dk, 1.5, .5)
    ic.im.alpha_composite(rot_im(e.im, -28, 145, 112))
    ic.im.alpha_composite(ImageChops.offset(Image.new("RGBA", (N, N), (0, 0, 0, 0)), 0, 0))


def g_thumb(ic):
    cuff = m_rrect(62, 112, 96, 192, 6)
    gl(ic, cuff, "b")
    hand = union(m_rrect(100, 112, 192, 192, 14), m_rrect(100, 100, 128, 124, 8))
    thumb = m_poly([(100, 116), (112, 104), (130, 66), (146, 62), (148, 84), (138, 112)])
    gl(ic, union(hand, thumb, m_rrect(110, 62, 150, 116, 16)), "y")
    for k in range(3):
        dt(ic, m_line([(124, 134 + k * 20), (186, 134 + k * 20)], 2.5), .35)


def g_ghost(ic):
    g = union(m_circle(128, 108, 54), m_rect(74, 108, 182, 188))
    for k in range(4):
        g = union(g, m_circle(88 + k * 26.6, 188, 13.3))
    gl(ic, g, "w")
    for x in (108, 150):
        dt(ic, m_ellipse(x - 8, 98, x + 8, 124), .9)
    dt(ic, m_ellipse(118, 134, 138, 152), .8)
    ic.paint(m_text("?", "black", 28, 186, 66), GC["y"][0], .95)
    ic.paint(m_ellipse(96, 72, 118, 92), (255, 255, 255), .0)


def g_shortcut(ic):
    road = m_line(bez((66, 196), (66, 130), (192, 160), (190, 76), 40), 26)
    gl(ic, road, "k")
    mid = bez((66, 196), (66, 130), (192, 160), (190, 76), 40)
    for i in range(0, 40, 4):
        ic.paint(m_line([mid[i], mid[min(40, i + 2)]], 4, False), (255, 255, 255), .9)
    gl(ic, m_line([(80, 188), (176, 96)], 9), "y")
    gl(ic, m_poly([(168, 66), (208, 62), (200, 104)]), "y")
    gl(ic, m_circle(76, 194, 12), "w", sh=.3)


def g_transfer(ic):
    gl(ic, m_rrect(58, 92, 112, 164, 10), "w")
    gl(ic, m_rrect(144, 92, 198, 164, 10), "w")
    gl(ic, m_rrect(120, 118, 136, 138, 3), "y")
    gl(ic, m_poly([(116, 120), (140, 120), (140, 108), (166, 128), (140, 148), (140, 136), (116, 136)]), "y")
    gl(ic, m_circle(85, 128, 15), "b", sh=0)
    gl(ic, m_circle(171, 128, 15), "g", sh=0)


def g_compass(ic):
    gl(ic, m_circle(128, 128, 62), "w")
    dt(ic, cut(m_circle(128, 128, 52), m_circle(128, 128, 50)), .5)
    for k in range(4):
        a = k * 90
        s = rotate_mask(m_rect(126, 70, 130, 82), a, 128, 128)
        dt(ic, s, .7)
    n = m_poly([(128, 128), (148, 112), (178, 80), (156, 148)])
    gl(ic, m_poly([(112, 142), (170, 92), (144, 148)]), "r", sh=.3)
    gl(ic, m_poly([(144, 114), (86, 164), (112, 108)]), "s", sh=.3)
    gl(ic, m_circle(128, 128, 8), "y", sh=0)


def g_translation(ic):
    gl(ic, m_circle(88, 108, 34), "w")
    dt(ic, m_text("A", "black", 38, 88, 109), .9)
    gl(ic, m_circle(170, 108, 34), "y")
    dt(ic, m_text("文", "cjk", 36, 170, 108), .9)
    gl(ic, m_poly([(106, 166), (150, 166), (150, 154), (178, 174), (150, 194), (150, 182), (106, 182)]), "w", sh=.3)
    gl(ic, union(m_line([(110, 156), (180, 196)], 10), m_line([(180, 156), (110, 196)], 10)), "r", sh=.3)


def g_bottleneck(ic):
    b = m_poly([(76, 66), (180, 66), (180, 92), (146, 128), (146, 154), (180, 190), (180, 196), (76, 196), (76, 190), (110, 154), (110, 128), (76, 92)])
    b = union(m_poly([(70, 62), (186, 62), (186, 94), (146, 130), (146, 152), (186, 188), (186, 200), (70, 200), (70, 188), (110, 152), (110, 130), (70, 94)]))
    gl(ic, b, "w")
    for (x, y) in [(94, 86), (128, 80), (162, 88), (104, 106), (150, 104)]:
        gl(ic, m_circle(x, y, 7), "y", sh=0, rim=False)
    gl(ic, m_circle(128, 140, 6), "y", sh=0, rim=False)
    for (x, y) in [(112, 184), (142, 182), (160, 190), (96, 190)]:
        gl(ic, m_circle(x, y, 6), "y", sh=0, rim=False)


def g_serial(ic):
    for k in range(3):
        x = 58 + k * 52
        gl(ic, m_rrect(x, 96, x + 40, 148, 8), "w" if k else "y")
        if k < 2:
            gl(ic, m_poly([(x + 41, 112), (x + 52, 122), (x + 41, 132)]), "w", sh=.2, rim=False)
    clk = m_circle(184, 172, 24)
    gl(ic, clk, "w")
    dt(ic, union(m_line([(184, 172), (184, 158)], 4), m_line([(184, 172), (194, 178)], 4)), .9)
    dt(ic, m_line([(62, 176), (150, 176)], 5), .0)
    dt(ic, m_line([(66, 176), (146, 176)], 6), .35)


def g_inbreed(ic):
    arcs = [m_arc(128, 128, 54, 40, 190, 14), m_arc(128, 128, 54, 220, 370, 14)]
    for k, a0 in enumerate((20, 200)):
        gl(ic, m_arc(128, 128, 52, a0, a0 + 140, 15), "w")
        a = math.radians(a0 + 150)
        x, y = 128 + 52 * math.cos(a), 128 + 52 * math.sin(a)
        d = a + math.pi / 2
        tip = (x + 22 * math.cos(d), y + 22 * math.sin(d))
        gl(ic, m_poly([(x + 22 * math.cos(a), y + 22 * math.sin(a)), tip, (x - 22 * math.cos(a), y - 22 * math.sin(a))]), "w")
    gl(ic, m_circle(128, 128, 18), "r")
    ic.paint(m_text("x", "black", 24, 128, 128), (255, 255, 255))


def g_emergence(ic):
    import random
    rnd = random.Random(11)
    for _ in range(14):
        a = rnd.uniform(0, 2 * math.pi); r_ = rnd.uniform(52, 72)
        gl(ic, m_circle(128 + r_ * math.cos(a), 128 + r_ * math.sin(a), rnd.uniform(3, 6)), "w", sh=0, rim=False)
    gl(ic, m_star(128, 128, 54, 14, 4, -90), "y")
    gl(ic, m_star(128, 128, 30, 8, 4, -45), "w", sh=0, rim=False)


# id -> (category, rarity, glyph) ; category/rarity mirror XgSim.Cards.cs
CARDS = {}


def C(cat, rar, i, fn):
    CARDS[i] = (cat, rar, fn)


C("wall", 1, "emerge.2", g_pick); C("wall", 1, "emerge.3", g_picword); C("wall", 1, "emerge.4", g_sentence)
C("wall", 1, "emerge.5", g_translate); C("wall", 2, "emerge.6", g_all); C("wall", 1, "wall.pretrain", g_pretrain)
C("insight", 2, "insight.pretrain", g_scale); C("insight", 4, "lingguang", g_flash)
C("road", 1, "road.scale", g_bricks)
C("cure", 1, "cure.first", g_pill); C("cure", 2, "cure.five", g_heartbeat); C("cure", 1, "cure.sight", g_eye)
C("data", 1, "data.junkoff", g_funnel); C("data", 1, "data.flywheel", g_flywheel)
C("fun", 0, "fun.nan", g_boom); C("fun", 1, "fun.combo", g_bolt); C("fun", 0, "fun.firstcard", g_cards)
C("life", 1, "life.gf.busy", g_busy); C("life", 0, "life.casino.first", g_chip); C("life", 1, "life.casino.blackjack", g_blackjack)
C("life", 2, "life.casino.jackpot", g_slot); C("life", 1, "life.casino.broke", g_wallet); C("life", 2, "life.casino.loss", g_dice)
C("life", 4, "life.casino.streak", g_crown); C("life", 0, "life.gf.chat", g_chat_heart); C("life", 1, "life.gf.packet", g_redenv)
C("life", 0, "life.gf.fight", g_snow); C("life", 1, "life.gf.makeup", g_heart_band); C("life", 1, "life.gf.forgot", g_calendar)
C("life", 1, "life.gf.iphone", g_phone); C("life", 2, "life.gf.sweet", g_sweet); C("life", 3, "life.gf.newyear", g_fireworks)
C("life", 4, "life.love.ask", g_heart_q); C("life", 2, "life.love.never", g_bubble_x); C("life", 1, "life.love.thrice", g_q3)
C("life", 0, "life.santi.login", g_vr); C("life", 1, "life.santi.code", g_key); C("life", 2, "life.santi.play", g_suns)
C("life", 4, "life.santi.civ", g_sprout); C("life", 3, "life.santi.silence", g_silence); C("life", 1, "life.santi.bug", g_bug)
C("life", 2, "life.santi.wallfacer", g_wallfacer); C("life", 2, "life.santi.sophon", g_sophon)
C("life", 1, "life.game.won", g_trophy); C("life", 2, "life.game.lost", g_go); C("life", 1, "life.hw.burn", g_burn)
C("life", 2, "life.hw.titan", g_titan); C("life", 0, "life.redpacket.slow", g_snail)
C("ending", 3, "end.E1", g_power); C("ending", 3, "end.E2", g_blank); C("ending", 3, "end.E3", g_fork)
# 书架: one card per book (rarity follows the ability that opens it: 2-3 common, 4 rare, 5 epic)
C("data", 0, "book.sanzijing", g_sanzijing); C("data", 0, "book.lunyu", g_lunyu); C("data", 0, "book.tangshi", g_tangshi)
C("data", 1, "book.xiyouji", g_xiyouji); C("data", 1, "book.shuihu", g_shuihu); C("data", 1, "book.sanguo", g_sanguo)
C("data", 2, "book.nahan", g_nahan); C("data", 2, "book.hongloumeng", g_hongloumeng)
# 现象图鉴: common
for _i, _f in [("xor", g_xor), ("vanish", g_vanish), ("memorize", g_memorize), ("grok", g_grok), ("generalize", g_generalize),
               ("deeper", g_deeper), ("amnesia", g_amnesia), ("forget", g_forget), ("sycophancy", g_thumb), ("hallucinate", g_ghost),
               ("shortcut", g_shortcut), ("transfer", g_transfer), ("drift", g_compass), ("translation", g_translation),
               ("bottleneck", g_bottleneck), ("serial", g_serial), ("inbreeding", g_inbreed), ("emergence", g_emergence)]:
    C("phenomenon", 0, "ph." + _i, _f)


def render_card(cid):
    cat, rar, fn = CARDS[cid]
    ic = medal(cat, rar)
    g = Icon()
    g.dk = ic.dk
    fn(g)
    # the glyph is drawn on its own layer so it can be nudged to sit centred on the disc
    ic.im.alpha_composite(g.im)
    path = os.path.join(OUT, "card", fname(cid) + ".png")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    ic.save(path, SIZE)
    return path



def job(args):
    group, i = args
    try:
        return render_relic(i) if group == "relic" else render_card(i)
    except Exception as e:  # keep going, report at the end
        import traceback
        return "FAIL %s/%s: %s\n%s" % (group, i, e, traceback.format_exc())


def main(argv):
    from multiprocessing import Pool
    groups = [argv[0]] if argv else ["relic", "card"]
    ids = argv[1:]
    jobs = []
    if "relic" in groups:
        jobs += [("relic", i) for i in RELICS if not ids or i in ids]
    if "card" in groups and "CARDS" in globals():
        jobs += [("card", i) for i in CARDS if not ids or i in ids]
    with Pool(min(10, len(jobs) or 1)) as p:
        for r in p.imap_unordered(job, jobs):
            print(r)


if __name__ == "__main__":
    main(sys.argv[1:])
