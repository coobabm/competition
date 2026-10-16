"""灵光 tech-tree / items icons (group "tech", 128 px).

Run:  python3 Tools/Icons/tech_icons.py            all icons + contact sheets
      python3 Tools/Icons/tech_icons.py perceptron ph.xor      only these ids
Frames (one per category so the page reads at a glance):
  architectures   blue rounded tile, white network sketch
  research        white paper sheet with a green header, ink + colour sketch
  dataset packs   yellow folder with a card sticking out, content glyph on the card
  lr / depth / width        teal tile
  labelling skills          orange hexagon
  transformer parts         purple tile
  datacenter                steel rack tile, secret.6 gold tile
  phenomena       round navy "observation" disc with a steel bezel
  abilities       gold scalloped seal
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import iconkit
from iconkit import *
from PIL import Image, ImageFilter, ImageOps, ImageChops


# The kit's grow() is a big-kernel rank filter (1 s per call); this box-blur version is 100x faster and is
# all the gloss/rim code needs.
def grow(m, px):
    r = max(1, u(abs(px)))
    b = m.filter(ImageFilter.BoxBlur(r))
    return b.point(lambda v: 255 if v >= 250 else 0) if px < 0 else b.point(lambda v: 255 if v > 4 else 0)


iconkit.grow = grow

OUT = "/Users/kk/competition/Assets/LingGuangV05/Resources/LingGuangV05/Icons/tech"
SCRATCH = "/private/tmp/claude-501/-Users-kk-unity/0b11848a-3722-4bc3-8ee4-9222e5441068/scratchpad"

WH = (255, 255, 255)
PB = (214, 234, 255)        # pale blue (lines on blue)
INK = (44, 52, 70)
INK2 = (96, 108, 130)
GREY = (200, 205, 214)
BLUEc = (40, 120, 220)
ORGc = (245, 140, 30)
REDc = (222, 52, 52)
GRNc = (58, 168, 70)
YELc = (255, 205, 50)
PURc = (140, 80, 210)
TEALc = (20, 160, 150)
CYAN = (110, 225, 255)
PINK = (255, 120, 150)
LIME = (150, 230, 90)


def fname(i):
    return i.replace(".", "_").replace("/", "_").replace(" ", "_")


def C(c):
    return PAL[c][1] if isinstance(c, str) else tuple(c[:3])


# ── drawing helpers ───────────────────────────────────────────────────────────

def line(ic, pts, w, col, a=1.0, caps=True):
    ic.paint(m_line(pts, w, caps), C(col), a)


def dashed(ic, p0, p1, w, col, dash=9, gap=7, a=1.0):
    L = math.hypot(p1[0] - p0[0], p1[1] - p0[1]); ux, uy = (p1[0] - p0[0]) / L, (p1[1] - p0[1]) / L
    m = blank(); t = 0
    while t < L:
        e = min(L, t + dash)
        m = union(m, m_line([(p0[0] + ux * t, p0[1] + uy * t), (p0[0] + ux * e, p0[1] + uy * e)], w, False))
        t += dash + gap
    ic.paint(m, C(col), a)


def arrow(ic, p0, p1, w=6, col=WH, head=18, a=1.0):
    parrow(ic, [p0, p1], w, col, head, a)


def parrow(ic, pts, w=6, col=WH, head=18, a=1.0):
    p0, p1 = pts[-2], pts[-1]
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]; L = math.hypot(dx, dy) or 1
    ux, uy = dx / L, dy / L; nx, ny = -uy, ux
    base = (p1[0] - ux * head * .85, p1[1] - uy * head * .85)
    ic.paint(union(m_line(pts[:-1] + [base], w), m_poly([p1, (base[0] + nx * head * .6, base[1] + ny * head * .6),
                                                       (base[0] - nx * head * .6, base[1] - ny * head * .6)])), C(col), a)


def bez(p0, p1, p2, n=20):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]) for t in [i / n for i in range(n + 1)]]


def curve(fx, x0, x1, n=40):
    return [(x0 + (x1 - x0) * i / n, fx(x0 + (x1 - x0) * i / n)) for i in range(n + 1)]


def node(ic, x, y, r, col="paper", shadow=True):
    m = m_circle(x, y, r)
    if shadow: ic.shade(m, .3, 0, 2.5, 2.2)
    ic.body(m, col)
    return m


def slab(ic, x0, y0, x1, y1, col="paper", r=6, shadow=True):
    m = m_rrect(x0, y0, x1, y1, r)
    if shadow: ic.shade(m, .28, 0, 2.5, 2.2)
    ic.body(m, col)
    return m


def edge(ic, p, q, w=3.6, col=PB, a=.75):
    line(ic, [p, q], w, col, a)


def netlayout(layers, x0, x1, y0, y1):
    xs = [x0 + (x1 - x0) * i / max(1, len(layers) - 1) for i in range(len(layers))]
    return [[(x, y0 + (y1 - y0) * (j + .5) / n) for j in range(n)] for x, n in zip(xs, layers)]


def dense_edges(ic, pos, w=3.2, col=PB, a=.7):
    em = blank()
    for a_, b_ in zip(pos, pos[1:]):
        for p in a_:
            for q in b_:
                em = union(em, m_line([p, q], w))
    ic.paint(em, C(col), a)


def axes(ic, x0=60, y0=208, x1=206, y1=76, col=INK2, w=5):
    parrow(ic, [(x0, y0), (x1, y0)], w, col, 15)
    parrow(ic, [(x0, y0), (x0, y1)], w, col, 15)


def ring(cx, cy, r, w):
    return cut(m_circle(cx, cy, r), m_circle(cx, cy, r - w))


def check(ic, cx, cy, s, col=GRNc, w=None):
    w = w or s * .42
    line(ic, [(cx - s * .8, cy), (cx - s * .2, cy + s * .62), (cx + s * .85, cy - s * .65)], w, col)


def cross(ic, cx, cy, s, col=REDc, w=None):
    w = w or s * .36
    line(ic, [(cx - s * .7, cy - s * .7), (cx + s * .7, cy + s * .7)], w, col)
    line(ic, [(cx - s * .7, cy + s * .7), (cx + s * .7, cy - s * .7)], w, col)


def star_body(ic, cx, cy, r, col="gold", pts=5):
    m = m_star(cx, cy, r, r * .48, pts)
    ic.shade(m, .3, 0, 2.5, 2)
    ic.body(m, col)


def text(ic, s, kind, size, cx, cy, col):
    ic.paint(m_text(s, kind, size, cx, cy), C(col))


def rpoly(pts, r, n=8):
    out = []; k = len(pts)
    for i in range(k):
        p0, p1, p2 = pts[i - 1], pts[i], pts[(i + 1) % k]

        def toward(a, b, d):
            L = math.hypot(b[0] - a[0], b[1] - a[1]) or 1
            return (a[0] + (b[0] - a[0]) * d / L, a[1] + (b[1] - a[1]) * d / L)
        a, b = toward(p1, p0, r), toward(p1, p2, r)
        for j in range(n + 1):
            t = j / n
            out.append(((1 - t) ** 2 * a[0] + 2 * (1 - t) * t * p1[0] + t * t * b[0],
                        (1 - t) ** 2 * a[1] + 2 * (1 - t) * t * p1[1] + t * t * b[1]))
    return m_poly(out)


def mirror_x(mask):
    return ImageOps.mirror(mask)


# ── plates ───────────────────────────────────────────────────────────────────

def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def ptile(ic, color, mask, gloss=.3):
    lo, base, dk = PAL[color]
    ic.body(mask, color, top=mix(base, lo, .3), bottom=mix(base, dk, .75), gloss=False)
    ic.gloss(mask, gloss)


def plate_arch(ic):
    ptile(ic, "blue", tile_mask())


def tile_mask():
    return m_rrect(20, 20, 236, 236, 44)


def plate_scale(ic):
    ptile(ic, "teal", tile_mask())


def plate_purple(ic):
    ptile(ic, "purple", tile_mask())


def plate_steel(ic):
    ptile(ic, "steel", tile_mask())


def plate_gold(ic):
    ptile(ic, "gold", tile_mask())


def plate_label(ic):
    hexm = rpoly([(128 + 118 * math.cos(math.radians(-90 + 60 * k)), 128 + 118 * math.sin(math.radians(-90 + 60 * k))) for k in range(6)], 26)
    ptile(ic, "orange", hexm)


def plate_research(ic):
    x0, y0, x1, y1, fold = 34, 22, 222, 234, 36
    body = m_poly([(x0, y0), (x1 - fold, y0), (x1, y0 + fold), (x1, y1), (x0, y1)])
    body = intersect(body, m_rrect(x0, y0, x1, y1, 18))
    ic.shade(body, .25, 0, 3, 3)
    ic.paint(body, vgrad((255, 255, 255), (230, 233, 240), y0, y1))
    band = intersect(body, m_rect(x0, y0, x1, y0 + 34))
    ic.paint(band, vgrad((120, 215, 130), (40, 150, 70), y0, y0 + 34))
    ic.paint(m_rect(x0, y0 + 30, x1, y0 + 34), (30, 120, 55), .5)
    ic.rim(body, (130, 140, 160), 1.8, .7)
    flap = m_poly([(x1 - fold, y0), (x1 - fold, y0 + fold), (x1, y0 + fold)])
    ic.paint(flap, vgrad((205, 210, 222), (250, 250, 252), y0, y0 + fold))
    for k in range(3):
        ic.paint(m_circle(52 + k * 15, y0 + 17, 4.5), (255, 255, 255), .85)


def plate_data(ic):
    back = union(m_rrect(20, 96, 236, 226, 18), m_rrect(20, 72, 112, 120, 12))
    ic.shade(back, .28, 0, 3, 3)
    ic.paint(back, vgrad((235, 170, 40), (170, 110, 20), 66, 226))
    card = m_rrect(40, 24, 216, 180, 12)
    ic.shade(card, .3, 0, 3, 3)
    ic.paint(card, vgrad((255, 255, 255), (228, 232, 240), 26, 170))
    ic.rim(card, (140, 150, 170), 1.8, .7)

    def front():
        pocket = m_rrect(20, 150, 236, 226, 18)
        ic.shade(pocket, .3, 0, -2, 4)
        ic.body(pocket, "yellow")
        ic.paint(m_line([(46, 166), (210, 166)], 4), (255, 255, 255), .4)
    return front


def plate_phen(ic):
    outer = m_circle(128, 128, 108)
    ic.shade(outer, .25, 0, 3, 3)
    ic.body(outer, "steel", gloss=False)
    inner = m_circle(128, 128, 88)
    ic.paint(inner, rgrad((52, 92, 150), (10, 20, 52), 128, 112, 108))
    ic.paint(cut(inner, grow(inner, -6)), (0, 0, 0), .35)

    def post():
        ic.gloss(inner, .3, .5)
        ic.paint(cut(outer, grow(outer, -3)), (255, 255, 255), .25)
    return post


def plate_ability(ic):
    m = m_circle(128, 128, 94)
    for k in range(16):
        a = 2 * math.pi * k / 16
        m = union(m, m_circle(128 + 98 * math.cos(a), 128 + 98 * math.sin(a), 15))
    ic.shade(m, .28, 0, 3, 3)
    ic.body(m, "gold")
    inner = m_circle(128, 128, 76)
    ic.paint(inner, vgrad((120, 62, 20), (70, 30, 10), 52, 204))
    ic.paint(cut(inner, grow(inner, -4)), (255, 235, 150), .85)

    def post():
        ic.gloss(inner, .22, .5)
    return post


# ── architectures (white sketches on the blue tile) ───────────────────────────

def g_perceptron(ic):
    ins = [(66, 82), (66, 128), (66, 174)]
    for p in ins: edge(ic, p, (148, 128), 4)
    arrow(ic, (180, 128), (214, 128), 8, YELc, 20)
    for p in ins: node(ic, *p, 15)
    node(ic, 148, 128, 32, "yellow")
    text(ic, "Σ", "bold", 46, 148, 128, INK)


def g_mlp(ic):
    pos = netlayout((3, 4, 4, 2), 62, 194, 64, 192)
    dense_edges(ic, pos, 3, PB, .55)
    for layer in pos:
        for p in layer: node(ic, *p, 11)
    for p in pos[-1]: node(ic, *p, 11, "yellow")


def sheets(ic, x, y, w, h, n, dx=8, dy=-8, col="paper"):
    for i in range(n - 1, -1, -1):
        slab(ic, x + i * dx, y + i * dy, x + i * dx + w, y + i * dy + h, col, 4)


def g_lenet(ic):
    slab(ic, 34, 92, 92, 150, "paper", 5)
    for k in (1, 2):
        line(ic, [(34 + k * 19.3, 94), (34 + k * 19.3, 148)], 2, INK2, .5); line(ic, [(36, 92 + k * 19.3), (90, 92 + k * 19.3)], 2, INK2, .5)
    ic.paint(m_rrect(41, 99, 63, 121, 2), C("orange"))
    arrow(ic, (96, 121), (110, 121), 6, PB, 12)
    sheets(ic, 114, 106, 34, 54, 3, 9, -9)
    arrow(ic, (166, 114), (178, 114), 6, PB, 11)
    sheets(ic, 182, 104, 16, 30, 2, 6, -6)
    slab(ic, 214, 96, 226, 144, "yellow", 4)


def g_alexnet(ic):
    slab(ic, 38, 68, 84, 188, "paper", 6)
    arrow(ic, (88, 128), (100, 128), 5, PB, 10)
    for i, (h, n) in enumerate(((112, 2), (92, 3), (74, 4), (58, 5))):
        x = 106 + i * 28
        sheets(ic, x, 128 - h / 2 + n * 2.5, 14, h, n, 4.5, -4.5)
    slab(ic, 222 - 6, 98, 232 - 6, 158, "yellow", 4)


def g_vgg(ic):
    hs = (118, 118, 94, 94, 70, 70, 70, 46)
    for i, h in enumerate(hs):
        x = 40 + i * 22
        slab(ic, x, 128 - h / 2, x + 15, 128 + h / 2, "yellow" if i == len(hs) - 1 else "paper", 4)


def g_googlenet(ic):
    node(ic, 50, 128, 14)
    boxes = [(100, 66, 140, 90, "sky"), (92, 100, 148, 156, "paper"), (84, 166, 156, 198, "yellow")]
    for (x0, y0, x1, y1, c) in boxes:
        cy = (y0 + y1) / 2
        edge(ic, (62, 128), (x0, cy), 4)
        edge(ic, (x1, cy), (190, 128), 4)
    slab(ic, 100, 66, 140, 92, "sky", 5); slab(ic, 90, 100, 150, 156, "paper", 6); slab(ic, 80, 164, 160, 200, "yellow", 6)
    slab(ic, 188, 90, 210, 166, "orange", 6)


def g_resnet(ic):
    node(ic, 50, 150, 12)
    slab(ic, 78, 132, 112, 168, "paper", 6); slab(ic, 124, 132, 158, 168, "paper", 6)
    edge(ic, (62, 150), (78, 150), 4); edge(ic, (112, 150), (124, 150), 4); edge(ic, (158, 150), (176, 150), 4)
    node(ic, 190, 150, 15, "yellow"); text(ic, "+", "black", 26, 190, 150, INK)
    parrow(ic, bez((50, 138), (120, 40), (190, 136), 24), 6, YELc, 18)


def g_rnn(ic):
    node(ic, 112, 150, 38)
    loop = [(112 + 40 * math.cos(math.radians(a)), 96 + 40 * math.sin(math.radians(a))) for a in range(130, 410, 12)]
    parrow(ic, loop, 9, YELc, 22)
    arrow(ic, (112, 228), (112, 196), 7, PB, 17)
    arrow(ic, (154, 150), (206, 150), 7, PB, 17)
    node(ic, 112, 150, 9, "orange")


def g_lstm(ic):
    cell = m_rrect(56, 86, 200, 200, 14)
    ic.shade(cell, .3, 0, 3, 3)
    ic.paint(cell, vgrad((90, 160, 235), (30, 90, 190), 86, 200))
    ic.rim(cell, (14, 50, 130), 2, .6)
    line(ic, [(36, 106), (220, 106)], 9, YELc)
    for x in (82, 128, 174):
        line(ic, [(x, 166), (x, 106)], 4, PB, .9)
        node(ic, x, 166, 14, "orange")
    node(ic, 100, 106, 12, "paper"); node(ic, 156, 106, 12, "paper")
    text(ic, "×", "black", 20, 100, 106, INK); text(ic, "+", "black", 20, 156, 106, INK)
    arrow(ic, (128, 232), (128, 204), 6, PB, 14)


def g_gru(ic):
    cell = m_rrect(56, 86, 200, 200, 14)
    ic.shade(cell, .3, 0, 3, 3)
    ic.paint(cell, vgrad((90, 160, 235), (30, 90, 190), 86, 200))
    ic.rim(cell, (14, 50, 130), 2, .6)
    line(ic, [(36, 106), (220, 106)], 9, LIME)
    for x in (98, 158):
        line(ic, [(x, 166), (x, 106)], 4, PB, .9)
        node(ic, x, 166, 15, "orange")
    node(ic, 128, 106, 12, "paper"); text(ic, "+", "black", 20, 128, 106, INK)
    arrow(ic, (128, 232), (128, 204), 6, PB, 14)


def g_seq2seq(ic):
    xs = [48, 84, 120]
    for i, x in enumerate(xs):
        slab(ic, x - 14, 108, x + 14, 148, "paper", 6)
        if i: arrow(ic, (xs[i - 1] + 15, 128), (x - 15, 128), 5, PB, 9)
        arrow(ic, (x, 202), (x, 156), 5, PB, 10, .85)
    arrow(ic, (136, 128), (156, 128), 6, PB, 11)
    node(ic, 172, 128, 17, "orange")
    arrow(ic, (190, 128), (204, 128), 6, PB, 11)
    slab(ic, 202, 108, 228, 148, "paper", 6)
    arrow(ic, (215, 100), (215, 66), 5, PB, 10, .85)
    slab(ic, 200, 50, 230, 66, "yellow", 5)


def g_attention(ic):
    top = (128, 72)
    bots = [(54 + i * 49, 190) for i in range(4)]
    ws = (3, 10, 4, 14)
    for b, w in zip(bots, ws):
        line(ic, [top, b], w, YELc if w == 14 else PB, 1 if w >= 10 else .7)
    for b in bots: node(ic, *b, 15)
    node(ic, *top, 20, "orange")


def g_textcnn(ic):
    xs = [50 + i * 31 for i in range(6)]
    for x in xs: slab(ic, x - 12, 142, x + 12, 196, "paper", 5)
    win = m_rrect(xs[1] - 18, 128, xs[3] + 18, 210, 9)
    ic.paint(win, C(YELc), .35); ic.rim(win, YELc, 5, 1)
    arrow(ic, (xs[2], 122), (xs[2], 94), 6, YELc, 15)
    for x, h in zip(xs[1:4], (24, 52, 34)):
        slab(ic, x - 10, 86 - h, x + 10, 86, "yellow" if h == 52 else "sky", 4)


def picture(ic, x0, y0, x1, y1, sky="sky", r=6):
    m = m_rrect(x0, y0, x1, y1, r)
    ic.shade(m, .3, 0, 2.5, 2.5)
    ic.paint(m, vgrad((150, 215, 255), (200, 235, 255), y0, y1))
    w, h = x1 - x0, y1 - y0
    ic.paint(intersect(m, m_poly([(x0, y1), (x0 + w * .38, y0 + h * .42), (x0 + w * .62, y1)])), (60, 150, 80))
    ic.paint(intersect(m, m_poly([(x0 + w * .35, y1), (x0 + w * .68, y0 + h * .55), (x1, y1)])), (40, 120, 60))
    ic.paint(m_circle(x0 + w * .75, y0 + h * .26, h * .12), (255, 220, 80))
    ic.rim(m, (255, 255, 255), 3, .9)
    return m


def g_caption(ic):
    picture(ic, 40, 74, 118, 142)
    parrow(ic, bez((122, 118), (136, 152), (150, 150), 10), 5, PB, 12)
    b = m_rrect(112, 130, 218, 196, 14)
    ic.shade(b, .3, 0, 2.5, 2.5); ic.body(b, "paper")
    ic.paint(m_poly([(124, 192), (140, 192), (118, 214)]), (225, 225, 220))
    for y, x1 in ((146, 202), (162, 190), (178, 168)): line(ic, [(126, y), (x1, y)], 6, INK2, .85)


def g_transformer(ic):
    tops = [(60 + i * 45, 80) for i in range(4)]
    bots = [(60 + i * 45, 176) for i in range(4)]
    em = blank()
    for p in tops:
        for q in bots: em = union(em, m_line([p, q], 3))
    ic.paint(em, PB, .55)
    for p, q in zip(tops, bots): line(ic, [p, q], 8, YELc)
    line(ic, [tops[0], bots[3]], 5, YELc, .9)
    for p in tops: node(ic, *p, 15, "paper")
    for q in bots: node(ic, *q, 15, "paper")
    arrow(ic, (128, 232), (128, 200), 6, PB, 14)
    arrow(ic, (128, 56), (128, 34), 6, PB, 12)


# ── research (ink sketches on the paper sheet) ────────────────────────────────

def g_weights(ic):
    ins = [(64, 98), (64, 146), (64, 194)]
    for p, w in zip(ins, (4, 10, 18)): line(ic, [p, (160, 146)], w, BLUEc, .75)
    for p in ins: node(ic, *p, 12, "gray")
    node(ic, 168, 146, 28, "orange")
    arrow(ic, (196, 146), (214, 146), 6, INK2, 14)


def g_learnrule(ic):
    for p in ((80, 104), (108, 88), (92, 134)): node(ic, *p, 10, "blue")
    for p in ((156, 176), (184, 156), (132, 190)): node(ic, *p, 10, "red")
    line(ic, [(66, 196), (200, 80)], 6, GREY, .9)
    line(ic, [(60, 148), (204, 124)], 8, ORGc)
    parrow(ic, bez((196, 90), (214, 106), (206, 122), 10), 5, INK2, 13)


def g_step(ic):
    axes(ic)
    line(ic, [(64, 192), (134, 192), (134, 100), (204, 100)], 9, ORGc)
    dashed(ic, (134, 100), (134, 208), 3, INK2, 7, 6)


def g_bias(ic):
    axes(ic)
    line(ic, [(64, 204), (196, 100)], 7, GREY)
    line(ic, [(64, 164), (196, 72)], 9, BLUEc)
    arrow(ic, (104, 172), (104, 140), 6, REDc, 16)


def g_features(ic):
    for p, c in (((70, 100), "blue"), ((110, 100), "red"), ((70, 140), "red"), ((110, 140), "blue")): node(ic, *p, 14, c)
    arrow(ic, (60, 176), (120, 176), 5, INK2, 13, .0)
    parrow(ic, [(134, 120), (156, 120)], 6, INK2, 15)
    ic.paint(m_rrect(160, 92, 208, 150, 8), (222, 227, 238))
    for x, y, c in ((172, 106, "red"), (172, 134, "red"), (196, 106, "blue"), (196, 134, "blue")): node(ic, x, y, 7, c, False)
    line(ic, [(184, 86), (184, 156)], 4, ORGc)
    text(ic, "x·y", "bold", 34, 128, 194, INK2)


def g_sigmoid(ic):
    axes(ic)
    s = lambda x: 204 - 116 / (1 + math.exp(-(x - 133) / 14))
    dashed(ic, (62, 88), (200, 88), 3, GREY, 7, 6)
    line(ic, curve(s, 64, 202, 50), 9, BLUEc)


def g_momentum(ic):
    f = lambda x: 206 - 0.021 * (x - 133) ** 2
    line(ic, curve(f, 58, 208, 40), 6, INK2)
    node(ic, 66, 112, 9, "orange", False); node(ic, 82, 130, 12, "orange", False)
    node(ic, 100, 156, 17, "orange")
    arrow(ic, (116, 168), (150, 192), 8, REDc, 18)


def g_backprop(ic):
    pos = netlayout((2, 3, 1), 70, 190, 88, 164)
    dense_edges(ic, pos, 3.6, INK2, .6)
    for layer in pos:
        for p in layer: node(ic, *p, 14, "blue")
    parrow(ic, [(190, 184), (190, 208), (70, 208), (70, 186)], 7, REDc, 18)


def g_chainrule(ic):
    for i, x in enumerate((82, 128, 174)):
        ic.paint(ring(x, 160, 30, 9), C(BLUEc if i % 2 == 0 else ORGc))
    text(ic, "∂", "bold", 64, 128, 96, INK)


def g_cudnn(ic):
    for k in range(4):
        for (x, y, w, h) in ((84 + k * 24, 82, 10, 16), (84 + k * 24, 194, 10, 16), (62, 108 + k * 22, 16, 10), (178, 108 + k * 22, 16, 10)):
            ic.paint(m_rrect(x, y, x + w, y + h, 2), (150, 156, 168))
    chip = m_rrect(76, 96, 180, 200, 10)
    ic.shade(chip, .3, 0, 2, 2); ic.body(chip, "dark")
    die = m_rrect(94, 114, 162, 182, 6)
    ic.paint(die, vgrad((160, 215, 40), (90, 150, 0), 114, 182)); ic.rim(die, (50, 90, 0), 2, .8)
    text(ic, "cu", "black", 36, 128, 150, WH)


def g_cooler(ic):
    ring_m = ring(128, 146, 64, 10)
    ic.shade(m_circle(128, 146, 64), .25, 0, 3, 3)
    ic.paint(m_circle(128, 146, 64), (225, 236, 248))
    for k in range(5):
        blade = rotate_mask(m_ellipse(116, 90, 140, 138), k * 72, 128, 146)
        ic.paint(blade, vgrad((130, 200, 255), (40, 120, 220), 90, 146), 1)
    ic.body(ring_m, "steel")
    node(ic, 128, 146, 14, "blue")


def g_relu(ic):
    axes(ic)
    line(ic, [(64, 194), (126, 194), (200, 100)], 9, GRNc)
    node(ic, 126, 194, 8, "lime", False)


def g_gradclip(ic):
    dashed(ic, (58, 100), (206, 100), 3, INK2, 8, 6)
    dashed(ic, (58, 188), (206, 188), 3, INK2, 8, 6)
    f = lambda x: 144 - 70 * math.sin((x - 58) / 148 * 2 * math.pi * 1.0)
    line(ic, curve(f, 58, 206, 60), 4, REDc, .35)
    g = lambda x: max(100, min(188, f(x)))
    line(ic, curve(g, 58, 206, 60), 9, REDc)


def g_irnn(ic):
    for i in range(3):
        for j in range(3):
            x, y = 76 + j * 36, 88 + i * 38
            ic.paint(m_rrect(x, y, x + 30, y + 32, 5), C(ORGc) if i == j else (222, 227, 238))
    for sx, d in ((60, 1), (196, -1)):
        line(ic, [(sx + d * 9, 84), (sx, 84), (sx, 208), (sx + d * 9, 208)], 5, INK2)


def g_dropout(ic):
    pos = netlayout((3, 4, 3), 70, 186, 84, 204)
    drop = {(0, 1), (1, 0), (1, 2), (2, 2)}
    em = blank()
    for l in range(2):
        for i, p in enumerate(pos[l]):
            for j, q in enumerate(pos[l + 1]):
                if (l, i) not in drop and (l + 1, j) not in drop: em = union(em, m_line([p, q], 4))
    ic.paint(em, C(BLUEc), .8)
    for l, layer in enumerate(pos):
        for i, p in enumerate(layer):
            if (l, i) in drop:
                node(ic, *p, 13, "gray", False); cross(ic, p[0], p[1], 11, REDc, 5)
            else: node(ic, *p, 13, "blue")


def g_augment(ic):
    spots = [(56, 76, 0), (132, 76, 1), (56, 152, 2), (132, 152, 3)]
    for (x, y, k) in spots:
        card = m_rrect(x, y, x + 68, y + 68, 8)
        ic.shade(card, .22, 0, 2, 2); ic.paint(card, (240, 243, 250)); ic.rim(card, (150, 160, 180), 2, .8)
        cx, cy = x + 34, y + 34
        if k == 0: ic.paint(m_text("F", "black", 50, cx, cy), C(BLUEc))
        elif k == 1: ic.paint(rotate_mask(m_text("F", "black", 50, cx, cy), -50, cx, cy), C(ORGc))
        elif k == 2: ic.paint(mirror_x(m_text("F", "black", 50, 256 - cx, cy)), C(GRNc))
        else: ic.paint(m_text("F", "black", 30, cx + 8, cy + 8), C(REDc))


def g_dataclean(ic):
    for x, c in ((88, "red"), (118, "blue"), (146, "red"), (172, "blue")): node(ic, x, 82 + (x % 3) * 3, 9, c, False)
    fun = union(m_poly([(66, 104), (190, 104), (146, 162), (110, 162)]), m_rrect(116, 158, 140, 200, 4))
    ic.shade(fun, .28, 0, 3, 3); ic.body(fun, "steel")
    line(ic, [(80, 112), (176, 112)], 5, WH, .5)
    node(ic, 128, 214, 9, "blue", False)
    star_body(ic, 168, 196, 14, "yellow")


def g_rmsprop(ic):
    up = [(62 + i * 12, 144 - 56 + i * 2.4) for i in range(13)]
    dn = [(62 + i * 12, 144 + 56 - i * 2.4) for i in range(13)]
    ic.paint(m_poly(up + dn[::-1]), (190, 215, 250), .85)
    zz = [(62 + i * 12, 144 + (1 if i % 2 else -1) * (46 - i * 2.4)) for i in range(13)]
    line(ic, zz, 6, BLUEc)
    line(ic, up, 4, BLUEc, .5); line(ic, dn, 4, BLUEc, .5)


def g_batchnorm(ic):
    base = 196
    line(ic, [(56, base), (206, base)], 5, INK2)
    wide = lambda x: base - 52 * math.exp(-((x - 100) ** 2) / (2 * 30 ** 2))
    ic.paint(m_poly(curve(wide, 56, 206, 50) + [(206, base), (56, base)]), (210, 214, 224), .8)
    nb = lambda x: base - 100 * math.exp(-((x - 140) ** 2) / (2 * 20 ** 2))
    pts = curve(nb, 56, 206, 70)
    ic.paint(m_poly(pts + [(206, base), (56, base)]), C(BLUEc), .45)
    line(ic, pts, 8, BLUEc)
    dashed(ic, (140, 84), (140, base), 3, INK2, 7, 5)
    arrow(ic, (104, 122), (124, 122), 5, ORGc, 13)


def g_adam(ic):
    for rx, ry, c in ((74, 54, (196, 205, 222)), (50, 36, (176, 188, 212)), (26, 18, (150, 165, 200))):
        ic.paint(ring(144, 146, rx, 5) if False else cut(m_ellipse(144 - rx, 146 - ry, 144 + rx, 146 + ry), m_ellipse(144 - rx + 5, 146 - ry + 5, 144 + rx - 5, 146 + ry - 5)), c)
    path = [(70, 200), (90, 178), (104, 176), (118, 162), (126, 158), (136, 150), (144, 146)]
    line(ic, path, 7, ORGc)
    for p in path[:-1]: node(ic, *p, 6, "orange", False)
    node(ic, 144, 146, 11, "green")


def g_lrschedule(ic):
    axes(ic)
    f = lambda x: 204 - 118 * math.exp(-(x - 66) / 42)
    # flipped: start high, decay
    g = lambda x: 90 + 100 * (1 - math.exp(-(x - 66) / 40))
    pts = curve(g, 66, 204, 50)
    line(ic, pts, 9, ORGc)
    for x in (70, 102, 140, 186): node(ic, x, g(x), 7, "red", False)


def g_position(ic):
    for i in range(4):
        x = 52 + i * 40
        c = slab(ic, x, 100, x + 34, 164, "paper", 6)
        text(ic, str(i + 1), "black", 34, x + 17, 132, BLUEc if i != 2 else ORGc)
    arrow(ic, (54, 188), (204, 188), 7, INK2, 17)


def g_transfer(ic):
    pos1 = netlayout((2, 3), 62, 96, 100, 188); dense_edges(ic, pos1, 3.5, GREY, .9)
    for l in pos1:
        for p in l: node(ic, *p, 10, "blue")
    pos2 = netlayout((3, 2), 166, 200, 100, 188); dense_edges(ic, pos2, 3.5, GREY, .9)
    for l in pos2:
        for p in l: node(ic, *p, 10, "orange")
    arrow(ic, (110, 144), (156, 144), 12, GRNc, 26)


def g_warmup(ic):
    axes(ic)
    line(ic, [(66, 198), (136, 100), (204, 100)], 9, ORGc)
    for x, y in ((84, 172), (110, 136)): node(ic, x, y, 7, "red", False)
    ic.paint(m_poly([(66, 198), (136, 100), (136, 198)]), (255, 190, 90), .25)


def g_fp16(ic):
    text(ic, "FP16", "black", 46, 128, 106, INK)
    slab(ic, 54, 148, 202, 168, "gray", 5)
    for i in range(1, 8): line(ic, [(54 + i * 18.5, 150), (54 + i * 18.5, 166)], 2, INK2, .5)
    slab(ic, 54, 182, 128, 202, "blue", 5)
    for i in range(1, 4): line(ic, [(54 + i * 18.5, 184), (54 + i * 18.5, 200)], 2, WH, .5)


def g_earlystop(ic):
    axes(ic)
    tr = lambda x: 90 + 100 * (1 - math.exp(-(x - 64) / 50))
    line(ic, curve(tr, 64, 204, 50), 7, BLUEc)
    va = lambda x: 196 - 58 * math.sin(min(1, (x - 64) / 120) * math.pi * 0.5) + (max(0, x - 132) ** 2) * .008 * 1.0
    va2 = lambda x: 112 + ((x - 132) ** 2) * 0.0062 if True else 0
    line(ic, curve(va2, 64, 204, 50), 8, REDc)
    dashed(ic, (132, 76), (132, 206), 3, INK2, 7, 5)
    node(ic, 132, 86, 12, "red")


def g_wordvec(ic):
    o = (72, 188)
    for tip, c in (((196, 118), "blue"), ((182, 176), "orange"), ((118, 88), "green")):
        arrow(ic, o, tip, 6, INK2, 15)
        node(ic, *tip, 11, c)
    node(ic, *o, 8, "gray")


def g_beamsearch(ic):
    root = (62, 144)
    l1 = [(108, 98), (108, 144), (108, 190)]
    l2 = [(154, 82), (154, 114), (154, 128), (154, 160), (154, 176), (154, 208)]
    kids = {0: (0, 1), 1: (2, 3), 2: (4, 5)}
    for i, p in enumerate(l1):
        edge(ic, root, p, 5, INK2, .5)
        for k in kids[i]: edge(ic, p, l2[k], 4, INK2, .4)
    line(ic, [root, l1[1], l2[2], (200, 128)], 8, ORGc)
    node(ic, *root, 11, "gray")
    for i, p in enumerate(l1): node(ic, *p, 10, "orange" if i == 1 else "gray")
    for i, p in enumerate(l2): node(ic, *p, 7, "orange" if i == 2 else "gray", False)
    node(ic, 204, 128, 11, "green")


def g_subword(ic):
    slab(ic, 54, 84, 202, 108, "gray", 6)
    for x in (100, 156): dashed(ic, (x, 78), (x, 118), 3, REDc, 5, 3)
    arrow(ic, (128, 120), (128, 138), 5, INK2, 12)
    for (x0, x1, c) in ((54, 98, "blue"), (106, 152, "orange"), (160, 202, "green")): slab(ic, x0, 148, x1, 196, c, 8)


def g_sft(ic):
    b1 = m_rrect(50, 78, 168, 128, 12); ic.shade(b1, .22, 0, 2, 2); ic.paint(b1, (232, 236, 244)); ic.rim(b1, (150, 158, 176), 2, .8)
    ic.paint(m_poly([(66, 124), (84, 124), (62, 146)]), (232, 236, 244))
    for y, x1 in ((94, 150), (110, 130)): line(ic, [(66, y), (x1, y)], 6, INK2, .7)
    b2 = m_rrect(88, 144, 206, 196, 12); ic.shade(b2, .22, 0, 2, 2); ic.body(b2, "blue")
    ic.paint(m_poly([(190, 192), (172, 192), (198, 214)]), (30, 90, 190))
    for y, x1 in ((160, 186), (176, 168)): line(ic, [(104, y), (x1, y)], 6, WH, .9)
    node(ic, 192, 150, 17, "green"); check(ic, 192, 150, 9, WH, 4.5)


def g_cot(ic):
    cloud = union(m_circle(88, 116, 36), m_circle(128, 94, 44), m_circle(172, 116, 38), m_circle(152, 142, 38), m_circle(104, 142, 36), m_rrect(88, 104, 172, 156, 10))
    ic.shade(cloud, .22, 0, 2, 3); ic.paint(cloud, vgrad((255, 255, 255), (222, 228, 240), 50, 180)); ic.rim(cloud, (140, 150, 172), 2, .8)
    node(ic, 72, 198, 12, "gray"); node(ic, 54, 216, 7, "gray")
    pts = [(86, 136), (124, 114), (164, 92)]
    for a, b in zip(pts, pts[1:]): arrow(ic, a, b, 6, INK2, 14)
    node(ic, *pts[0], 13, "blue", False); node(ic, *pts[1], 13, "orange", False); node(ic, *pts[2], 15, "green", False)
    text(ic, "1", "black", 17, pts[0][0], pts[0][1], WH); text(ic, "2", "black", 17, pts[1][0], pts[1][1], WH)
    check(ic, pts[2][0], pts[2][1], 7, WH, 3.5)


# ── dataset packs (glyphs on the card sticking out of the folder) ─────────────

def g_spam(ic):
    env = m_rrect(66, 54, 190, 122, 8)
    ic.shade(env, .2, 0, 2, 2); ic.paint(env, vgrad((250, 250, 252), (216, 222, 234), 54, 122)); ic.rim(env, (120, 130, 150), 2, .8)
    line(ic, [(70, 58), (128, 98), (186, 58)], 4, INK2, .8)
    node(ic, 180, 56, 22, "red"); text(ic, "!", "black", 32, 180, 57, WH)


def g_mnist(ic):
    pad = m_rrect(82, 40, 174, 126, 8)
    ic.paint(pad, vgrad((50, 56, 80), (20, 24, 40), 40, 126))
    for i in range(1, 6): line(ic, [(82 + i * 15.3, 42), (82 + i * 15.3, 124)], 1.6, WH, .12)
    for j in range(1, 5): line(ic, [(84, 40 + j * 17.2), (172, 40 + j * 17.2)], 1.6, WH, .12)
    ic.paint(m_text("7", "black", 78, 128, 85), (240, 245, 255))


def g_danmu(ic):
    scr = m_rrect(60, 42, 196, 124, 8)
    ic.paint(scr, vgrad((60, 70, 100), (24, 28, 48), 42, 124))
    for (x0, y0, x1, c) in ((70, 52, 150, PINK), (102, 76, 186, CYAN), (78, 100, 160, YELc)):
        ic.paint(m_rrect(x0, y0, x1, y0 + 14, 7), c)
        ic.paint(m_rrect(x0 + 6, y0 + 4, x1 - 26, y0 + 10, 3), (255, 255, 255), .6)


def g_headline(ic):
    ic.paint(m_rrect(62, 42, 194, 74, 6), vgrad((240, 90, 80), (190, 30, 40), 42, 74))
    ic.paint(m_rrect(72, 52, 150, 64, 3), (255, 255, 255), .9)
    for y, x1 in ((88, 150), (104, 194), (118, 170)): line(ic, [(64, y), (x1, y)], 6, INK2, .75)
    node(ic, 180, 100, 20, "yellow"); text(ic, "!?", "black", 22, 180, 101, INK)


def g_logic(ic):
    a, b = m_circle(106, 84, 38), m_circle(150, 84, 38)
    ic.paint(a, C(BLUEc), .8); ic.paint(b, C(ORGc), .8)
    ic.paint(intersect(a, b), (255, 255, 255), .95)
    ic.rim(a, BLUEc, 2.5, .9); ic.rim(b, ORGc, 2.5, .9)
    check(ic, 128, 84, 11, GRNc, 6)


def g_arith(ic):
    for ch, x, c in (("7", 82, BLUEc), ("+", 128, ORGc), ("5", 174, GRNc)):
        ic.paint(m_text(ch, "black", 78, x, 84), c)


def thumb(ic, x, y, w, h, kind):
    m = m_rrect(x, y, x + w, y + h, 5)
    ic.paint(m, vgrad((150, 210, 255), (205, 235, 255), y, y + h))
    if kind == 0:
        ic.paint(intersect(m, m_poly([(x, y + h), (x + w * .45, y + h * .35), (x + w, y + h)])), (70, 150, 90))
        ic.paint(m_circle(x + w * .75, y + h * .28, h * .16), (255, 215, 70))
    elif kind == 1:
        ic.paint(intersect(m, m_rect(x, y + h * .65, x + w, y + h)), (130, 135, 145))
        ic.paint(m_rrect(x + w * .2, y + h * .42, x + w * .8, y + h * .72, 5), C(REDc))
        for cx in (.32, .68): ic.paint(m_circle(x + w * cx, y + h * .75, h * .12), (40, 44, 54))
    elif kind == 2:
        ic.paint(intersect(m, m_rect(x, y + h * .75, x + w, y + h)), (100, 170, 90))
        ic.paint(m_rect(x + w * .46, y + h * .5, x + w * .54, y + h * .8), (130, 90, 50))
        ic.paint(m_circle(x + w * .5, y + h * .38, h * .26), (50, 150, 70))
    else:
        ic.paint(intersect(m, m_rect(x, y + h * .6, x + w, y + h)), (50, 110, 190))
        ic.paint(m_poly([(x + w * .22, y + h * .6), (x + w * .78, y + h * .6), (x + w * .66, y + h * .8), (x + w * .34, y + h * .8)]), (240, 240, 245))
        ic.paint(m_poly([(x + w * .5, y + h * .2), (x + w * .5, y + h * .58), (x + w * .72, y + h * .58)]), (255, 240, 200))
    ic.rim(m, (255, 255, 255), 2.4, .9)


def g_cifar(ic):
    for k, (x, y) in enumerate(((62, 40), (132, 40), (62, 86), (132, 86))): thumb(ic, x, y, 62, 38, k)


def g_meme(ic):
    m = m_circle(128, 84, 44)
    ic.shade(m, .3, 0, 2.5, 2.5); ic.body(m, "yellow")
    ic.paint(m_rrect(98, 68, 126, 86, 6), (30, 34, 44)); ic.paint(m_rrect(130, 68, 158, 86, 6), (30, 34, 44))
    line(ic, [(126, 74), (130, 74)], 4, (30, 34, 44))
    ic.paint(m_arc(128, 90, 24, 20, 160, 6), (150, 60, 20))
    ic.paint(m_rrect(104, 74, 116, 78, 2), (255, 255, 255), .5)


def g_poems(ic):
    paper = m_rrect(70, 44, 188, 122, 5)
    ic.shade(paper, .2, 0, 2, 2); ic.paint(paper, vgrad((250, 238, 205), (232, 212, 165), 44, 122)); ic.rim(paper, (150, 110, 60), 2, .7)
    for x in (66, 192):
        r = m_rrect(x - 6, 40, x + 6, 126, 6)
        ic.body(r, "brown", gloss=False)
    for i, x in enumerate((90, 112, 134, 156)):
        for j in range(3):
            y = 58 + j * 20
            line(ic, [(x, y), (x + (3 if (i + j) % 2 else -3), y + 11)], 5, INK, .85)
    ic.paint(m_rrect(168, 96, 182, 112, 2), C(REDc))


def pills(ic, x0, y, widths, h=12, gap=6, cols=(BLUEc, ORGc, GRNc, PURc)):
    x = x0
    for i, w in enumerate(widths):
        ic.paint(m_rrect(x, y, x + w, y + h, h / 2), C(cols[i % len(cols)]), .92)
        x += w + gap
    return x


def g_longtext(ic):
    rows = [(58, (18, 30, 14, 26, 22)), (78, (26, 12, 30, 18, 24)), (98, (14, 28, 20, 30, 18)), (118, (30, 16, 22, 12, 20))]
    for y, ws in rows: pills(ic, 58, y, ws, 12, 6)
    line(ic, [(58, 40), (198, 40)], 4, INK2, .0)
    for x in (196,): pass


def g_crosssentence(ic):
    pills(ic, 62, 50, (22, 34, 16, 30, 20), 14, 6)
    pills(ic, 62, 106, (30, 18, 34, 16, 24), 14, 6)
    ic.paint(m_rrect(112, 50, 128, 64, 6), C(YELc)); ic.paint(m_rrect(142, 106, 176, 120, 6), C(YELc))
    parrow(ic, bez((120, 66), (122, 96), (156, 98), 14) + [(158, 104)], 5, REDc, 13)


def g_review(ic):
    for i in range(5): star_body(ic, 68 + i * 30, 62, 15, "gold")
    for y, x1 in ((92, 170), (110, 148)): line(ic, [(64, y), (x1, y)], 6, INK2, .7)
    node(ic, 182, 108, 20, "red"); cross(ic, 182, 108, 10, WH, 5)


def g_go(ic):
    board = m_rrect(72, 38, 184, 128, 6)
    ic.shade(board, .25, 0, 2, 2); ic.paint(board, vgrad((236, 190, 110), (206, 150, 70), 38, 128)); ic.rim(board, (120, 80, 30), 2, .8)
    for i in range(5):
        line(ic, [(86 + i * 21, 50), (86 + i * 21, 116)], 1.8, (90, 60, 20), .8, False)
        line(ic, [(84, 52 + i * 16.5), (172, 52 + i * 16.5)], 1.8, (90, 60, 20), .8, False)
    node(ic, 107, 68.5, 10, "dark"); node(ic, 128, 85, 10, "paper"); node(ic, 149, 68.5, 10, "paper"); node(ic, 128, 52, 10, "dark"); node(ic, 149, 101.5, 10, "dark")


def g_imagenet(ic):
    back = m_rrect(66, 48, 184, 124, 6); ic.paint(back, (170, 176, 190)); ic.rim(back, (255, 255, 255), 2, .8)
    picture(ic, 76, 40, 192, 120)
    bb = m_rrect(114, 66, 164, 108, 3)
    ic.rim(bb, ORGc, 4, 1)
    ic.paint(m_rrect(114, 54, 150, 66, 3), C(ORGc))


def g_translate(ic):
    b1 = m_rrect(56, 44, 134, 92, 12); ic.shade(b1, .25, 0, 2, 2); ic.body(b1, "blue")
    ic.paint(m_poly([(70, 88), (88, 88), (66, 110)]), (30, 90, 190))
    text(ic, "EN", "black", 30, 95, 69, WH)
    b2 = m_rrect(122, 70, 200, 118, 12); ic.shade(b2, .25, 0, 2, 2); ic.body(b2, "orange")
    ic.paint(m_poly([(186, 114), (168, 114), (192, 130)]), (200, 100, 20))
    text(ic, "ZH", "black", 30, 161, 95, WH)


def g_news(ic):
    paper = m_rrect(70, 36, 186, 128, 6)
    ic.shade(paper, .2, 0, 2, 2); ic.paint(paper, vgrad((252, 250, 244), (228, 224, 212), 36, 128)); ic.rim(paper, (140, 130, 110), 2, .8)
    ic.paint(m_rrect(78, 44, 178, 60, 3), vgrad((230, 80, 70), (180, 30, 36), 44, 60))
    ic.paint(m_rrect(78, 68, 118, 100, 3), (170, 180, 196))
    ic.paint(m_poly([(78, 100), (96, 78), (118, 100)]), (110, 130, 120))
    for y in (70, 80, 90): line(ic, [(126, y), (178, y)], 4, INK2, .7)
    for y, x1 in ((110, 178), (120, 150)): line(ic, [(78, y), (x1, y)], 4, INK2, .6)


# ── lr / depth / width (teal tile) ───────────────────────────────────────────

def g_lr(ic):
    dial = m_circle(128, 140, 66)
    ic.shade(dial, .3, 0, 3, 3); ic.body(dial, "gray")
    ic.paint(m_circle(128, 140, 50), vgrad((70, 76, 90), (30, 34, 44), 90, 190))
    for k in range(9):
        a = math.radians(-210 + k * 30)
        p0 = (128 + 78 * math.cos(a), 140 + 78 * math.sin(a)); p1 = (128 + 90 * math.cos(a), 140 + 90 * math.sin(a))
        line(ic, [p0, p1], 6 if k % 4 == 0 else 4, YELc if 2 <= k <= 4 else WH)
    a = math.radians(-60)
    line(ic, [(128, 140), (128 + 44 * math.cos(a), 140 + 44 * math.sin(a))], 9, ORGc)
    node(ic, 128, 140, 10, "gray")


def _layer_slab(ic, y, wide, vision, col="paper"):
    pass


def g_depth(ic, vision):
    for i in range(4):
        y = 62 + i * 40
        if vision:
            slab(ic, 56, y, 160, y + 30, "paper", 7)
            ic.paint(m_rrect(64, y + 6, 82, y + 24, 2), C(ORGc))
            for k in range(3): ic.paint(m_rrect(94 + k * 20, y + 7, 108 + k * 20, y + 23, 2), (190, 200, 222))
        else:
            slab(ic, 56, y, 160, y + 30, "paper", 7)
            for k in range(5): node(ic, 76 + k * 19, y + 15, 6.5, "blue", False)
    arrow(ic, (194, 74), (194, 196), 10, YELc, 26)


def g_width(ic, vision):
    if vision:
        sheets(ic, 62, 100, 64, 64, 5, 14, -9)
    else:
        slab(ic, 44, 92, 212, 148, "paper", 12)
        for k in range(7): node(ic, 66 + k * 21, 120, 8.5, "blue", False)
    parrow(ic, [(128, 192), (214, 192)], 10, YELc, 24)
    parrow(ic, [(128, 192), (42, 192)], 10, YELc, 24)


# ── transformer parts (purple tile), datacenter, secret ──────────────────────

def g_multihead(ic):
    tops = [(60 + i * 45, 70) for i in range(4)]
    bots = [(60 + i * 45, 186) for i in range(4)]
    cols = [YELc, CYAN, PINK]
    for h, c in enumerate(cols):
        for i in range(4):
            line(ic, [tops[i], bots[(i + h * 1 + (1 if h == 1 else 0)) % 4]], 4.5, c, .95)
    for p in tops + bots: node(ic, *p, 13, "paper")


def g_layernorm(ic):
    for i in range(4):
        for j in range(5):
            x, y = 54 + j * 31, 80 + i * 34
            hl = (i == 1)
            ic.paint(m_rrect(x, y, x + 26, y + 28, 4), C(YELc) if hl else (225, 215, 245), 1)
    ic.rim(m_rrect(48, 112, 212, 148, 6), YELc, 4, 1)
    arrow(ic, (130, 196), (130, 160), 6, PB, 14)
    ic.paint(m_poly([(70, 214), (190, 214)] + []), WH, 0)


def g_residual(ic):
    slab(ic, 72, 132, 104, 170, "paper", 6); slab(ic, 118, 132, 150, 170, "paper", 6)
    node(ic, 48, 152, 11); node(ic, 190, 152, 16, "yellow"); text(ic, "+", "black", 28, 190, 152, INK)
    edge(ic, (59, 152), (72, 152), 4); edge(ic, (104, 152), (118, 152), 4); edge(ic, (150, 152), (174, 152), 4)
    parrow(ic, bez((48, 140), (120, 40), (190, 134), 24), 8, YELc, 20)


def g_datacenter(ic):
    for i in range(4):
        y = 54 + i * 40
        u_ = m_rrect(54, y, 202, y + 32, 6)
        ic.shade(u_, .3, 0, 2, 2); ic.body(u_, "dark")
        for k in range(3): ic.paint(m_rrect(66 + k * 22, y + 12, 82 + k * 22, y + 20, 2), (70, 78, 96))
        node(ic, 178, y + 16, 5, "green" if i != 2 else "orange", False); node(ic, 192, y + 16, 5, "green", False)
    ic.glow(m_circle(178, 56 + 0, 3), (120, 255, 120), 6, .0)


def g_secret(ic):
    bk = m_rrect(58, 52, 186, 204, 10)
    ic.shade(bk, .3, 0, 3, 3); ic.body(bk, "red")
    ic.paint(m_rect(70, 52, 80, 204), (90, 15, 25), .5)
    # key
    ic.paint(m_circle(138, 112, 24), C("gold")); ic.paint(m_circle(138, 112, 11), (150, 40, 45))
    line(ic, [(138, 134), (138, 178)], 10, "gold"); line(ic, [(138, 160), (156, 160)], 8, "gold"); line(ic, [(138, 174), (152, 174)], 8, "gold")
    star_body(ic, 192, 60, 22, "yellow")


# ── labelling skills (orange hexagon, white glyphs) ──────────────────────────

def coin_s(ic, cx, cy, r, sym="¥"):
    m = m_circle(cx, cy, r)
    ic.shade(m, .3, 0, 2, 2); ic.body(m, "gold")
    ic.paint(m_circle(cx, cy, r * .74), (200, 140, 30), .45)
    ic.paint(m_text(sym, "black", r * 1.1, cx, cy + 1), (130, 80, 10))


def g_l_raise(ic):
    coin_s(ic, 112, 138, 52)
    arrow(ic, (186, 190), (186, 80), 16, WH, 36)
    star_body(ic, 78, 76, 16, "yellow")


def g_l_auto(ic):
    gm = m_gear(128, 128, 60, 8, .24, .36)
    ic.shade(gm, .3, 0, 3, 3); ic.body(gm, "paper")
    check(ic, 128, 128, 22, GRNc, 11)
    arrow(ic, (58, 198), (96, 198), 5, YELc, 0.01, 0)


def g_l_coop(ic):
    b = m_rrect(54, 66, 202, 176, 28)
    b = union(b, m_poly([(80, 166), (116, 166), (70, 208)]))
    ic.shade(b, .3, 0, 3, 3); ic.body(b, "paper")
    ic.paint(m_text("?", "black", 100, 130, 124), C(ORGc))


def g_l_brain(ic):
    chip = m_rrect(66, 68, 190, 192, 16)
    for k in range(4):
        for (x, y, w, h) in ((84 + k * 28, 52, 10, 16), (84 + k * 28, 196, 10, 16), (50, 86 + k * 28, 16, 10), (190, 86 + k * 28, 16, 10)):
            ic.paint(m_rrect(x, y, x + w, y + h, 2), (255, 235, 200))
    ic.shade(chip, .3, 0, 3, 3); ic.body(chip, "dark")
    pos = netlayout((2, 3, 2), 90, 166, 92, 168)
    dense_edges(ic, pos, 3, CYAN, .7)
    for l in pos:
        for p in l: node(ic, *p, 9, "sky", False)


def g_l_hard(ic):
    text(ic, "×1.5", "black", 72, 128, 142, WH)
    coin_s(ic, 82, 70, 24); star_body(ic, 178, 68, 18, "yellow")


def g_l_audit(ic):
    ic.paint(m_circle(118, 118, 60), (255, 255, 255), .95)
    ic.paint(ring(118, 118, 60, 14), (40, 50, 70))
    line(ic, [(160, 160), (206, 206)], 20, (40, 50, 70))
    check(ic, 118, 118, 26, GRNc, 14)


def g_l_parallel(ic):
    for y in (96, 160):
        slab(ic, 50, y - 22, 96, y + 22, "paper", 8)
        gm = m_gear(73, y, 16, 6, .25, .35)
        ic.paint(gm, C(ORGc))
        arrow(ic, (104, y), (180, y), 9, WH, 22)
        node(ic, 196, y, 13, "yellow")
    text(ic, "×2", "black", 26, 128, 128, YELc)


def g_l_trap(ic):
    card = m_rrect(64, 54, 192, 200, 14)
    ic.shade(card, .3, 0, 3, 3); ic.body(card, "paper")
    star_body(ic, 128, 116, 42, "gold")
    ic.paint(m_poly([(150, 54), (180, 54), (180, 92), (165, 80), (150, 92)]), C(REDc))
    for y, x1 in ((178, 168),): line(ic, [(88, y), (x1, y)], 6, INK2, .5)


def g_l_captcha(ic):
    box = m_rrect(48, 74, 208, 170, 12)
    ic.shade(box, .3, 0, 3, 3); ic.body(box, "paper")
    for ch, x, a in (("A", 88, -12), ("7", 130, 10), ("k", 168, -8)):
        m = rotate_mask(m_text(ch, "black", 56, x, 124), a, x, 124)
        ic.paint(m, INK)
    for i in range(3):
        line(ic, bez((52, 96 + i * 26), (100, 74 + i * 30), (206, 130 + i * 12), 12), 3, (230, 80, 70), .85)
    node(ic, 184, 188, 20, "green"); check(ic, 184, 188, 10, WH, 5)


# ── phenomena (bright sketches on the navy observation disc) ─────────────────

def g_ph_xor(ic):
    for p, c in (((98, 98), "blue"), ((158, 98), "orange"), ((98, 158), "orange"), ((158, 158), "blue")): node(ic, *p, 15, c)
    line(ic, [(70, 134), (186, 122)], 5, REDc)
    cross(ic, 128, 128, 14, WH, 6)


def g_ph_vanish(ic):
    xs = [184, 152, 120, 88, 56]
    for i, x in enumerate(xs):
        a = [1, .8, .6, .42, .3][i]
        ic.paint(m_circle(x, 118, 17 - i * 1.5), CYAN, a)
        if i: line(ic, [(xs[i - 1] - 15, 118), (x + 15, 118)], max(3, 11 - i * 2), CYAN, a)
    parrow(ic, [(190, 168), (68, 168)], 7, PINK, 18, .85)


def g_ph_memorize(ic):
    for x, ok in ((82, True), (170, False)):
        c = m_rrect(x - 30, 90, x + 30, 164, 8)
        ic.paint(c, vgrad((255, 255, 255), (214, 222, 238), 90, 164))
        for y in (106, 120): line(ic, [(x - 18, y), (x + 18, y)], 5, INK2, .5)
        if ok: node(ic, x, 148, 15, "green"); check(ic, x, 148, 8, WH, 4)
        else: node(ic, x, 148, 15, "red"); cross(ic, x, 148, 7, WH, 4)


def g_ph_grok(ic):
    axes(ic, 66, 190, 196, 66, CYAN, 4)
    line(ic, [(70, 100), (190, 96)], 6, (130, 190, 255))
    line(ic, [(70, 184), (126, 180), (134, 176), (140, 106), (190, 98)], 7, YELc)
    star_body(ic, 150, 92, 18, "yellow")


def g_ph_generalize(ic):
    c = m_rrect(84, 82, 172, 174, 10)
    ic.paint(c, (255, 255, 255), .13)
    for i in range(10):
        a = i * 36
    dashed(ic, (84, 82), (172, 82), 4, CYAN, 8, 6); dashed(ic, (172, 82), (172, 174), 4, CYAN, 8, 6)
    dashed(ic, (172, 174), (84, 174), 4, CYAN, 8, 6); dashed(ic, (84, 174), (84, 82), 4, CYAN, 8, 6)
    ic.paint(m_text("?", "black", 56, 128, 124), (190, 230, 255))
    node(ic, 176, 170, 22, "green"); check(ic, 176, 170, 11, WH, 6)


def g_ph_deeper(ic):
    for i in range(6):
        h = 100 - i * 11 + (0 if i else 0)
        x = 62 + i * 22
        ic.paint(m_rrect(x, 190 - h, x + 15, 190, 4), vgrad((255, 150, 150), (200, 50, 60), 190 - h, 190))
    parrow(ic, [(60, 78), (190, 130)], 6, YELc, 18)


def g_ph_amnesia(ic):
    xs = [54 + i * 26 for i in range(6)]
    for i, x in enumerate(xs):
        ic.paint(m_rrect(x, 112, x + 21, 144, 6), CYAN, .12 + .17 * i)
    ic.paint(m_text("?", "black", 40, 70, 80), PINK)
    arrow(ic, (200, 172), (60, 172), 5, PINK, 15, .8)


def g_ph_forget(ic):
    for i, a in enumerate((1, .6, .25)):
        ic.paint(m_rrect(58 + i * 30, 76, 82 + i * 30, 100, 6), CYAN, a)
    er = rotate_mask(m_rrect(96, 108, 188, 144, 8), 18, 142, 126)
    ic.shade(er, .3, 0, 3, 3); ic.paint(er, vgrad((255, 160, 190), (230, 90, 130), 108, 144))
    ic.paint(rotate_mask(m_rrect(96, 108, 126, 144, 8), 18, 142, 126), (255, 255, 255), .9)
    for x in (66, 96, 126): ic.paint(m_circle(x + 8, 188, 6), (180, 200, 240), .5)


def g_ph_sycophancy(ic):
    # thumbs up
    ic.paint(m_rrect(72, 112, 98, 182, 6), C(BLUEc))
    hand = union(m_rrect(106, 112, 186, 182, 16), m_rrect(114, 72, 142, 126, 12))
    ic.shade(hand, .3, 0, 3, 3); ic.body(hand, "yellow")
    ic.paint(m_circle(176, 80, 5), PINK, 0)
    for x, y in ((170, 78), (186, 96)):
        ic.paint(m_heart(x, y, 24), PINK)


def g_ph_hallucinate(ic):
    gh = union(m_circle(128, 112, 46), m_rect(82, 112, 174, 186))
    wave = [(82, 186)]
    for i in range(7):
        wave.append((82 + (i + .5) * 92 / 7, 186 + (10 if i % 2 == 0 else -4)))
    wave.append((174, 186))
    gh = union(gh, m_poly(wave + [(174, 160), (82, 160)]))
    ic.shade(gh, .3, 0, 2, 3); ic.paint(gh, vgrad((255, 255, 255), (200, 215, 245), 66, 196))
    ic.paint(m_ellipse(104, 100, 120, 124), INK); ic.paint(m_ellipse(136, 100, 152, 124), INK)
    ic.paint(m_circle(112, 108, 4), WH); ic.paint(m_circle(144, 108, 4), WH)
    ic.paint(m_ellipse(116, 140, 140, 168), (200, 60, 120))
    ic.paint(m_text("!", "black", 36, 188, 80), YELc)


def g_ph_shortcut(ic):
    path = [(64, 186), (64, 98), (192, 98)]
    line(ic, [(64, 186), (64, 110), (80, 98), (192, 98)], 9, (120, 140, 190), .8)
    dashed(ic, (70, 182), (180, 106), 6, YELc, 10, 7)
    node(ic, 64, 186, 12, "blue"); star_body(ic, 184, 100, 16, "yellow")
    ic.paint(m_poly([(150, 200), (180, 200), (180, 170)]), (60, 160, 80), 0)


def g_ph_transfer(ic):
    node(ic, 70, 150, 26, "blue"); node(ic, 186, 150, 26, "green")
    parrow(ic, bez((86, 128), (128, 70), (172, 128), 20), 7, YELc, 18)
    star_body(ic, 128, 88, 15, "yellow")
    text(ic, "A", "black", 26, 70, 150, WH); text(ic, "B", "black", 26, 186, 150, WH)


def g_ph_drift(ic):
    o = (96, 176)
    arrow(ic, o, (96, 76), 7, GRNc, 20)
    arrow(ic, o, (188, 122), 8, ORGc, 22)
    ic.paint(m_arc(96, 176, 56, 270, 328, 5), CYAN)
    node(ic, *o, 12, "paper")


def g_ph_translation(ic):
    pills(ic, 60, 76, (22, 30, 18, 26, 18), 14, 6, (CYAN, CYAN))
    pills(ic, 60, 160, (28, 18, 32, 20, 18), 14, 6, (ORGc, ORGc))
    cross(ic, 128, 118, 16, REDc, 8)
    line(ic, [(128, 96), (128, 104)], 5, REDc); line(ic, [(128, 134), (128, 148)], 5, REDc)


def g_ph_bottleneck(ic):
    ic.paint(m_poly([(56, 72), (112, 118), (112, 138), (56, 184)]), CYAN, .5)
    ic.paint(m_poly([(200, 72), (144, 118), (144, 138), (200, 184)]), CYAN, .5)
    for i, y in enumerate((88, 112, 136, 160)):
        ic.paint(m_rrect(60, y - 7, 90, y + 7, 6), WH)
    node(ic, 128, 128, 15, "orange")
    for y in (100, 128, 156): ic.paint(m_rrect(168, y - 7, 198, y + 7, 6), (255, 190, 90))


def g_ph_serial(ic):
    xs = [62, 100, 138, 176]
    for i, x in enumerate(xs):
        node(ic, x, 156, 15, "orange" if i == 3 else "sky")
        if i: arrow(ic, (xs[i - 1] + 16, 156), (x - 17, 156), 5, WH, 11)
        text(ic, str(i + 1), "black", 18, x, 157, INK)
    clock = m_circle(128, 98, 26)
    ic.body(clock, "paper")
    line(ic, [(128, 98), (128, 82)], 5, INK); line(ic, [(128, 98), (140, 104)], 5, INK)


def g_ph_inbreeding(ic):
    ic.paint(m_arc(128, 128, 54, 200, 340, 12), CYAN)
    ic.paint(m_arc(128, 128, 54, 20, 160, 12), CYAN)
    for (x, y, a) in ((178, 158, 0), (78, 98, 0)):
        pass
    ic.paint(m_poly([(176, 92), (176, 132), (148, 118)]), CYAN, 0)
    parrow(ic, [(172, 100), (180, 116)], 10, CYAN, 22)
    parrow(ic, [(84, 156), (76, 140)], 10, CYAN, 22)
    cross(ic, 128, 128, 20, REDc, 10)


def g_ph_emergence(ic):
    dashed(ic, (58, 132), (200, 132), 3, CYAN, 8, 6)
    for i, h in enumerate((26, 44, 68, 90, 108)):
        x = 60 + i * 28
        hi = h > 70
        ic.paint(m_rrect(x, 190 - h, x + 18, 190, 4), vgrad((255, 235, 120), (240, 150, 30), 190 - h, 190) if hi else vgrad((120, 160, 220), (60, 90, 160), 190 - h, 190))
    star_body(ic, 168, 76, 17, "yellow")


# ── abilities (white sketches on the dark centre of the gold seal) ────────────

def g_ab1(ic):
    node(ic, 96, 128, 28, "green"); check(ic, 96, 128, 15, WH, 8)
    node(ic, 160, 128, 28, "red"); cross(ic, 160, 128, 13, WH, 8)


def g_ab2(ic):
    for i, y in enumerate((96, 128, 160)):
        sel = i == 1
        ic.paint(ring(88, y, 13, 5), WH if not sel else YELc)
        if sel: ic.paint(m_circle(88, y, 7), YELc)
        ic.paint(m_rrect(110, y - 8, 178, y + 8, 8), YELc if sel else (210, 190, 160), 1 if sel else .6)


def g_ab3(ic):
    picture(ic, 68, 86, 134, 134)
    arrow(ic, (136, 126), (148, 136), 5, WH, 11)
    b = m_rrect(134, 136, 184, 166, 10); ic.body(b, "paper")
    text(ic, "cat", "black", 20, 159, 152, INK)


def g_ab4(ic):
    b = m_rrect(66, 82, 190, 156, 18)
    b = union(b, m_poly([(84, 148), (112, 148), (74, 184)]))
    ic.shade(b, .3, 0, 2, 3); ic.body(b, "paper")
    for y, x1 in ((106, 168), (128, 146)): line(ic, [(86, y), (x1, y)], 8, INK2, .7)


def g_ab5(ic):
    bk = union(m_poly([(128, 100), (76, 90), (76, 160), (128, 170)]), m_poly([(128, 100), (180, 90), (180, 160), (128, 170)]))
    ic.shade(bk, .3, 0, 2, 3); ic.paint(bk, vgrad((255, 255, 255), (222, 226, 238), 80, 180))
    line(ic, [(128, 100), (128, 170)], 3, INK2, .6)
    for y in (112, 128, 144): line(ic, [(86, y), (118, y + 2)], 4, INK2, .5); line(ic, [(138, y + 2), (170, y)], 4, INK2, .5)
    parrow(ic, bez((78, 86), (128, 60), (178, 86), 14), 6, YELc, 15)
    parrow(ic, bez((178, 176), (128, 202), (78, 176), 14), 6, CYAN, 15)


def g_ab6(ic):
    star_body(ic, 128, 128, 66, "gold", 8)
    eye = m_ellipse(94, 108, 162, 148)
    ic.paint(eye, WH)
    ic.paint(m_circle(128, 128, 15), (70, 40, 20)); ic.paint(m_circle(133, 122, 5), WH)


# ── registry ─────────────────────────────────────────────────────────────────

ICONS = {}


def reg(i, plate, fn):
    ICONS[i] = (plate, fn)


for i, f in (("perceptron", g_perceptron), ("mlp", g_mlp), ("lenet", g_lenet), ("alexnet", g_alexnet), ("vgg", g_vgg), ("googlenet", g_googlenet),
             ("resnet", g_resnet), ("rnn", g_rnn), ("lstm", g_lstm), ("gru", g_gru), ("seq2seq", g_seq2seq), ("attention", g_attention),
             ("textcnn", g_textcnn), ("caption", g_caption), ("transformer", g_transformer)):
    reg(i, plate_arch, f)

for i, f in (("weights", g_weights), ("learnrule", g_learnrule), ("step", g_step), ("bias", g_bias), ("features", g_features), ("sigmoid", g_sigmoid),
             ("momentum", g_momentum), ("backprop", g_backprop), ("chainrule", g_chainrule), ("cudnn", g_cudnn), ("cooler", g_cooler), ("relu", g_relu),
             ("gradclip", g_gradclip), ("irnn", g_irnn), ("dropout", g_dropout), ("augment", g_augment), ("dataclean", g_dataclean), ("rmsprop", g_rmsprop),
             ("batchnorm", g_batchnorm), ("adam", g_adam), ("lrschedule", g_lrschedule), ("position", g_position), ("transfer", g_transfer),
             ("warmup", g_warmup), ("fp16", g_fp16), ("earlystop", g_earlystop), ("wordvec", g_wordvec), ("beamsearch", g_beamsearch),
             ("subword", g_subword), ("sft", g_sft), ("cot", g_cot)):
    reg(i, plate_research, f)

for i, f in (("spam", g_spam), ("mnist", g_mnist), ("danmu", g_danmu), ("headline", g_headline), ("logic", g_logic), ("arith", g_arith), ("cifar", g_cifar),
             ("meme", g_meme), ("poems", g_poems), ("longtext", g_longtext), ("crosssentence", g_crosssentence), ("review", g_review), ("go", g_go),
             ("imagenet", g_imagenet), ("translate", g_translate), ("news", g_news)):
    reg(i + ".pack", plate_data, f)

reg("shared.lr", plate_scale, g_lr)
reg("s.d2", plate_scale, lambda ic: g_depth(ic, False)); reg("v.d3", plate_scale, lambda ic: g_depth(ic, True))
reg("s.w1", plate_scale, lambda ic: g_width(ic, False)); reg("v.w1", plate_scale, lambda ic: g_width(ic, True))

reg("multihead", plate_purple, g_multihead); reg("layernorm", plate_purple, g_layernorm); reg("residual", plate_purple, g_residual)
reg("datacenter", plate_steel, g_datacenter); reg("secret.6", plate_gold, g_secret)
for i, f in (("label.raise", g_l_raise), ("label.auto", g_l_auto), ("label.coop", g_l_coop), ("label.brain", g_l_brain), ("label.hard", g_l_hard),
             ("label.audit", g_l_audit), ("label.parallel", g_l_parallel), ("label.trapmemory", g_l_trap), ("label.captcha", g_l_captcha)):
    reg(i, plate_label, f)

for i, f in (("xor", g_ph_xor), ("vanish", g_ph_vanish), ("memorize", g_ph_memorize), ("grok", g_ph_grok), ("generalize", g_ph_generalize),
             ("deeper", g_ph_deeper), ("amnesia", g_ph_amnesia), ("forget", g_ph_forget), ("sycophancy", g_ph_sycophancy),
             ("hallucinate", g_ph_hallucinate), ("shortcut", g_ph_shortcut), ("transfer", g_ph_transfer), ("drift", g_ph_drift),
             ("translation", g_ph_translation), ("bottleneck", g_ph_bottleneck), ("serial", g_ph_serial), ("inbreeding", g_ph_inbreeding),
             ("emergence", g_ph_emergence)):
    reg("ph." + i, plate_phen, f)

for n, f in enumerate((g_ab1, g_ab2, g_ab3, g_ab4, g_ab5, g_ab6), 1):
    reg("ab.%d" % n, plate_ability, f)


def put(ic, g, s, src, dst, clip=None):
    im = g.im
    if s != 1.0:
        im = im.resize((int(N * s), int(N * s)), Image.LANCZOS)
    layer = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    layer.paste(im, (int(round(u(dst[0]) - u(src[0]) * s)), int(round(u(dst[1]) - u(src[1]) * s))))
    if clip is not None:
        layer.putalpha(ImageChops.multiply(layer.split()[3], clip))
    ic.im.alpha_composite(layer)


# glyph scale / source centre / destination centre per plate (most glyphs are drawn smaller than the room their plate gives)
FIT = {plate_data: (1.14, (128, 83), (128, 90), None), plate_phen: (1.0, (128, 128), (128, 128), 86), plate_ability: (1.0, (128, 128), (128, 128), 70)}


def render(i):
    plate, fn = ICONS[i]
    ic = Icon()
    post = plate(ic)
    if plate in FIT:
        g = Icon(); fn(g)
        sc, src, dst, clip = FIT[plate]
        put(ic, g, sc, src, dst, m_circle(128, 128, clip) if clip else None)
    else:
        fn(ic)
    if post: post()
    ic.save(os.path.join(OUT, fname(i) + ".png"), 128)
    return i


def main(argv):
    from multiprocessing import Pool
    ids = [a for a in argv if not a.startswith("-")] or list(ICONS)
    for a in ids:
        if a not in ICONS:
            print("unknown id", a); return 1
    os.makedirs(OUT, exist_ok=True)
    with Pool() as p:
        for done in p.imap_unordered(render, ids):
            pass
    print("rendered", len(ids), "icons ->", OUT)
    if len(ids) == len(ICONS):
        sheet(OUT, os.path.join(SCRATCH, "sheet_tech.png"), cell=104, cols=12)
        sheet(OUT, os.path.join(SCRATCH, "sheet_tech_small.png"), cell=40, cols=24)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
