"""灵光 app UI icons: groups wiring, desk, minigame, ability, tieba, life, nav (128 px).

Run:  python3 Tools/Icons/ui_icons.py [group ...]      (no args = every group)
Writes Assets/LingGuangV05/Resources/LingGuangV05/Icons/<group>/<id with . / space -> _>.png
Frames: nav/wiring/desk/tieba/life = rounded colour tile, minigame = disc. Glyphs are white with a soft shade,
details in the plate's dark tone, one accent hue.
"""
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from iconkit import *
from multiprocessing import Pool

OUT = "/Users/kk/unity/competition/Assets/LingGuangV05/Resources/LingGuangV05/Icons"
SCRATCH = "/private/tmp/claude-501/-Users-kk-unity/0b11848a-3722-4bc3-8ee4-9222e5441068/scratchpad"
REG = []


def reg(group, id_):
    def d(fn):
        REG.append((group, id_, fn))
        return fn
    return d


def fname(id_):
    return id_.replace(".", "_").replace("/", "_").replace(" ", "_")


# ── helpers ───────────────────────────────────────────────────────────────────
PT, PB = (255, 255, 255), (208, 218, 234)
GOLD = hue("gold")
R = m_rrect
C = m_circle


def wg(ic, m, top=PT, bot=PB, shade=.30):
    """White glyph with a soft shadow under it."""
    bb = m.getbbox()
    if not bb:
        return m
    ic.shade(m, shade, 0, 3, 2.5)
    ic.paint(m, vgrad(top, bot, bb[1] / S, bb[3] / S))
    return m


def col(ic, m, color):
    """A glossy coloured part (palette name)."""
    ic.shade(m, .3, 0, 3, 2.5)
    ic.body(m, color)
    return m


def dark(plate):
    return hue(plate)[2]


def plate(ic, color):
    m = m_rrect(14, 14, 242, 242, 54)
    ic.body(m, color)
    return m


def dplate(ic, color):
    m = m_circle(128, 128, 112)
    ic.body(m, color)
    return m


def ring(cx, cy, r, w):
    return cut(m_circle(cx, cy, r), m_circle(cx, cy, r - w))


def mirror(m):
    from PIL import ImageOps
    return ImageOps.mirror(m)


def fan(ic, cx, cy, r, blade=(255, 255, 255)):
    ic.paint(m_circle(cx, cy, r), vgrad((70, 76, 90), (22, 24, 30), cy - r, cy + r))
    bl = blank()
    for k in range(5):
        bl = union(bl, rotate_mask(m_ellipse(cx - r * .04, cy - r * .86, cx + r * .36, cy - r * .08), k * 72, cx, cy))
    ic.paint(bl, blade, .95)
    ic.paint(ring(cx, cy, r, max(2.5, r * .1)), (150, 158, 172))
    ic.paint(m_circle(cx, cy, r * .2), (190, 196, 208))


def gpu_card(ic, x=30, y=82, w=196, h=86, fans=2, body="dark"):
    ic.paint(m_rrect(x - 8, y + 4, x + 4, y + h + 14, 3), (176, 184, 198))
    b = R(x, y, x + w, y + h, 12)
    ic.shade(b, .3, 0, 4, 3)
    ic.body(b, body)
    pins = m_rect(x + 34, y + h, x + w - 34, y + h + 14)
    ic.paint(pins, vgrad((250, 210, 90), (190, 130, 30), y + h, y + h + 14))
    cxs = [x + w * (.28 if fans == 2 else .5), x + w * .72] if fans == 2 else [x + w * .5]
    for cx in cxs:
        fan(ic, cx, y + h * .48, h * .36)


def bolt(cx, cy, s):
    return m_poly([(cx + s * .15, cy - s), (cx - s * .62, cy + s * .1), (cx - s * .05, cy + s * .1), (cx - s * .2, cy + s),
                   (cx + s * .62, cy - s * .15), (cx + s * .05, cy - s * .15)])


def small_badge(ic, cx, cy, r, draw, color="orange"):
    m = C(cx, cy, r)
    ic.paint(grow(m, 3), WHITE)
    ic.body(m, color)
    draw(ic, cx, cy, r)


def arrow(pts, w, head):
    """A polyline arrow (mask) with a triangular head at the last point."""
    (x0, y0), (x1, y1) = pts[-2], pts[-1]
    a = math.atan2(y1 - y0, x1 - x0)
    base = (x1 - math.cos(a) * head * .5, y1 - math.sin(a) * head * .5)
    m = m_line(pts[:-1] + [base], w)
    p = [(x1, y1), (base[0] + math.cos(a + math.pi / 2) * head * .55, base[1] + math.sin(a + math.pi / 2) * head * .55),
         (base[0] + math.cos(a - math.pi / 2) * head * .55, base[1] + math.sin(a - math.pi / 2) * head * .55)]
    return union(m, m_poly(p))


def sparkle(cx, cy, r):
    return m_star(cx, cy, r, r * .26, 4)


def cloud(cx, cy, s):
    return union(m_ellipse(cx - s * .9, cy - s * .15, cx + s * .9, cy + s * .55),
                 C(cx - s * .45, cy - s * .15, s * .45), C(cx + s * .15, cy - s * .38, s * .55), C(cx + s * .55, cy - s * .08, s * .38))


def neuron_glyph(ic, cx, cy, spread=70, layers=(2, 3, 2), node=11, fill=(255, 255, 255), edge=None, edge_a=.7):
    xs = [cx - spread + 2 * spread * i / (len(layers) - 1) for i in range(len(layers))]
    pos = []
    for x, n in zip(xs, layers):
        pos.append([(x, cy - spread * .8 + (spread * 1.6) * (j + .5) / n) for j in range(n)])
    em = blank()
    for a, b in zip(pos, pos[1:]):
        for p in a:
            for q in b:
                em = union(em, m_line([p, q], 4))
    ic.paint(em, edge or fill, edge_a)
    for L in pos:
        for (x, y) in L:
            ic.shade(C(x, y, node), .3, 0, 2, 1.5)
            ic.paint(C(x, y, node), fill)


def cup(ic, cx, cy, s, color=(255, 255, 255)):
    body = R(cx - s * .55, cy - s * .35, cx + s * .45, cy + s * .55, s * .16)
    handle = m_arc(cx + s * .45, cy + s * .08, s * .3, -90, 90, s * .15)
    ic.paint(union(body, handle), color)
    ic.paint(m_line([(cx - s * .2, cy - s * .7), (cx - s * .2, cy - s * .5)], s * .1), color, .8)
    ic.paint(m_line([(cx + s * .12, cy - s * .7), (cx + s * .12, cy - s * .5)], s * .1), color, .8)


def monitor(ic, x0, y0, x1, y1, screen=(40, 60, 90)):
    frame = R(x0, y0, x1, y1, 12)
    wg(ic, frame)
    ic.paint(R(x0 + 9, y0 + 9, x1 - 9, y1 - 9, 6), vgrad(screen, tuple(int(c * .6) for c in screen), y0, y1))
    cx = (x0 + x1) / 2
    wg(ic, union(m_rect(cx - 10, y1, cx + 10, y1 + 22), R(cx - 34, y1 + 18, cx + 34, y1 + 30, 6)))
    return (x0 + 9, y0 + 9, x1 - 9, y1 - 9)


def serv_units(ic, x0, x1, ys, h=38, plate_dk=(20, 80, 40), lights=(120, 240, 120), fan_l=False):
    for y in ys:
        u_ = R(x0, y, x1, y + h, 8)
        wg(ic, u_)
        if fan_l:
            ic.paint(C(x0 + 24, y + h / 2, 12), (70, 76, 90))
            ic.paint(m_line([(x0 + 52, y + h / 2), (x1 - 34, y + h / 2)], 6), (150, 160, 176))
        else:
            ic.paint(m_line([(x0 + 14, y + h / 2), (x1 - 60, y + h / 2)], 6), (150, 160, 176))
        ic.paint(C(x1 - 18, y + h / 2, 6), lights)
        ic.paint(C(x1 - 36, y + h / 2, 6), (250, 190, 60))


def pencil(cx, cy, length, w, ang=45, body=(250, 200, 50)):
    """Pencil lying along angle `ang` (deg, 0 = pointing right), tip at (cx, cy)."""
    a = math.radians(ang)
    ux, uy = math.cos(a), math.sin(a)
    px, py = -uy, ux
    tipx, tipy = cx, cy
    b0 = (cx + ux * w * 1.7, cy + uy * w * 1.7)
    b1 = (cx + ux * length, cy + uy * length)
    tip = m_poly([(tipx, tipy), (b0[0] + px * w / 2, b0[1] + py * w / 2), (b0[0] - px * w / 2, b0[1] - py * w / 2)])
    shaft = m_poly([(b0[0] + px * w / 2, b0[1] + py * w / 2), (b1[0] + px * w / 2, b1[1] + py * w / 2),
                    (b1[0] - px * w / 2, b1[1] - py * w / 2), (b0[0] - px * w / 2, b0[1] - py * w / 2)])
    cap = m_poly([(b1[0] + px * w / 2, b1[1] + py * w / 2), (b1[0] + ux * w * .5 + px * w / 2, b1[1] + uy * w * .5 + py * w / 2),
                  (b1[0] + ux * w * .5 - px * w / 2, b1[1] + uy * w * .5 - py * w / 2), (b1[0] - px * w / 2, b1[1] - py * w / 2)])
    return tip, shaft, cap


def draw_pencil(ic, cx, cy, length=90, w=22, ang=45):
    tip, shaft, cap = pencil(cx, cy, length, w, ang)
    ic.shade(union(tip, shaft, cap), .3, 0, 3, 2.5)
    ic.paint(tip, (240, 214, 170))
    ic.body(shaft, "yellow")
    ic.paint(cap, (235, 110, 120))
    a = math.radians(ang)
    ic.paint(C(cx + math.cos(a) * 4, cy + math.sin(a) * 4, w * .16), (50, 50, 60))


# ═══ NAV ═════════════════════════════════════════════════════════════════════
@reg("nav", "teach")
def _(ic):
    plate(ic, "blue")
    base = m_poly([(64, 126), (128, 158), (192, 126), (192, 176), (128, 204), (64, 176)])
    wg(ic, base, (226, 234, 248), (168, 184, 214))
    top = m_poly([(128, 54), (232, 102), (128, 150), (24, 102)])
    wg(ic, top)
    ic.paint(m_poly([(128, 54), (232, 102), (128, 102)]), (255, 255, 255), .0)
    ic.paint(m_line([(208, 112), (208, 166)], 6), (240, 190, 60))
    ic.body(C(208, 172, 11), "gold")


@reg("nav", "train")
def _(ic):
    plate(ic, "orange")
    wg(ic, R(70, 116, 186, 140, 8), (240, 244, 250), (160, 170, 188))
    for x0, x1 in ((40, 78), (178, 216)):
        wg(ic, R(x0, 78, x1, 178, 14))
    for x0, x1 in ((78, 98), (158, 178)):
        wg(ic, R(x0, 94, x1, 162, 8), (236, 240, 248), (150, 162, 182))


@reg("nav", "tree")
def _(ic):
    plate(ic, "green")
    lines = [((128, 66), (76, 126)), ((128, 66), (180, 126)), ((76, 126), (50, 190)), ((76, 126), (106, 190)),
             ((180, 126), (150, 190)), ((180, 126), (206, 190))]
    em = blank()
    for a, b in lines:
        em = union(em, m_line([a, b], 8))
    ic.shade(em, .25, 0, 3, 2)
    ic.paint(em, (255, 255, 255), .92)
    for (x, y) in [(76, 126), (180, 126), (50, 190), (106, 190), (150, 190), (206, 190)]:
        wg(ic, C(x, y, 20))
    col(ic, C(128, 66, 26), "gold")


@reg("nav", "home")
def _(ic):
    plate(ic, "purple")
    big = sparkle(122, 122, 84)
    ic.glow(big, (255, 240, 160), 12, .6)
    ic.shade(big, .3, 0, 3, 2.5)
    ic.body(big, "gold")
    wg(ic, sparkle(196, 70, 30))
    wg(ic, sparkle(188, 192, 22))


@reg("nav", "board")
def _(ic):
    plate(ic, "pink")
    L = union(C(98, 108, 42), C(80, 146, 34), C(110, 168, 32), C(112, 100, 34))
    half = cut(L, m_rect(126, 0, 256, 256))
    br = union(half, mirror(half))
    br = cut(br, m_rect(124, 40, 132, 220))
    wg(ic, br)
    for pts in [[(70, 100), (92, 118), (84, 140)], [(186, 100), (164, 118), (172, 140)], [(96, 166), (114, 152), (110, 190)], [(160, 166), (142, 152), (146, 190)]]:
        ic.paint(m_line(pts, 5), dark("pink"), .5)
    ic.paint(m_rect(124, 60, 132, 210), dark("pink"), .55)


@reg("nav", "data")
def _(ic):
    plate(ic, "teal")
    for y in (138, 98, 58):
        body = union(m_rect(54, y + 16, 202, y + 56), m_ellipse(54, y, 202, y + 34), m_ellipse(54, y + 38, 202, y + 74))
        wg(ic, body)
        ic.paint(m_ellipse(54, y, 202, y + 34), (255, 255, 255))
        ic.paint(ring_ellipse(54, y, 202, y + 34, 3), dark("teal"), .4)
    ic.paint(C(176, 176, 6), (80, 220, 120))


def ring_ellipse(x0, y0, x1, y1, w):
    return cut(m_ellipse(x0, y0, x1, y1), m_ellipse(x0 + w, y0 + w, x1 - w, y1 - w))


@reg("nav", "wiring")
def _(ic):
    plate(ic, "orange")
    ic.paint(m_line([(128, 168), (128, 196), (180, 206), (212, 176)], 9), (255, 255, 255), .95)
    wg(ic, R(86, 88, 170, 156, 16))
    wg(ic, R(106, 150, 150, 176, 6))
    for x in (106, 138):
        wg(ic, R(x, 46, x + 16, 96, 4), (240, 244, 250), (170, 180, 196))
    ic.paint(m_line([(108, 114), (148, 114)], 6), dark("orange"), .45)


@reg("nav", "repo")
def _(ic):
    plate(ic, "steel")
    wg(ic, R(50, 98, 206, 206, 14))
    wg(ic, R(38, 62, 218, 108, 12), (255, 255, 255), (190, 200, 216))
    ic.paint(R(100, 128, 156, 148, 8), dark("steel"), .7)
    ic.paint(m_line([(60, 172), (196, 172)], 4), dark("steel"), .25)


@reg("nav", "abilities")
def _(ic):
    plate(ic, "red")
    b = bolt(128, 128, 92)
    ic.glow(b, (255, 230, 150), 10, .5)
    ic.shade(b, .3, 0, 3, 2.5)
    ic.body(b, "gold")


@reg("nav", "items")
def _(ic):
    plate(ic, "purple")
    g = m_poly([(88, 66), (168, 66), (212, 112), (128, 208), (44, 112)])
    ic.shade(g, .3, 0, 3, 2.5)
    ic.body(g, "sky")
    for pts in [[(44, 112), (212, 112)], [(88, 66), (106, 112), (128, 208)], [(168, 66), (150, 112), (128, 208)], [(88, 66), (128, 112), (168, 66)]]:
        ic.paint(m_line(pts, 3.5, False), (255, 255, 255), .55)


@reg("nav", "backpack")
def _(ic):
    plate(ic, "brown")
    for x in (82, 174):
        ic.paint(m_arc(x, 150, 38, 200, 340, 12), (255, 240, 220), .8)
    wg(ic, union(R(60, 76, 196, 212, 38), R(78, 60, 178, 120, 28)), shade=.3)
    ic.paint(m_arc(128, 58, 22, 180, 360, 10), (255, 255, 255))
    ic.paint(m_line([(70, 118), (186, 118)], 4), dark("brown"), .45)
    wg(ic, R(90, 150, 166, 196, 14), (225, 232, 244), (160, 172, 196))
    ic.paint(m_line([(100, 160), (156, 160)], 5), dark("brown"), .4)


@reg("nav", "cards")
def _(ic):
    plate(ic, "pink")
    a = rotate_mask(R(60, 56, 152, 186, 12), -14)
    b = rotate_mask(R(104, 60, 196, 190, 12), 12)
    wg(ic, a, (255, 240, 246), (210, 170, 196))
    wg(ic, b)
    st = rotate_mask(m_star(150, 125, 34), 12, 128, 128)
    col(ic, st, "gold")


@reg("nav", "final")
def _(ic):
    plate(ic, "blue")
    for cx, pts in ((82, (180, 270)), (174, (270, 360))):
        pass
    ic.paint(m_arc(78, 90, 26, 90, 270, 11), (245, 205, 80))
    ic.paint(m_arc(178, 90, 26, 270, 450, 11), (245, 205, 80))
    cupm = union(m_rect(80, 50, 176, 100), m_pie(128, 100, 48, 0, 180))
    ic.shade(cupm, .3, 0, 3, 2.5)
    ic.body(cupm, "gold")
    col(ic, m_rect(116, 144, 140, 174), "gold")
    col(ic, R(84, 172, 172, 202, 8), "gold")


# ═══ WIRING ══════════════════════════════════════════════════════════════════
@reg("wiring", "psu")
def _(ic):
    plate(ic, "orange")
    wg(ic, R(36, 70, 220, 186, 14))
    ic.paint(C(94, 128, 40), (40, 44, 56))
    fan(ic, 94, 128, 36)
    ic.paint(R(144, 94, 202, 162, 8), hue("orange")[1], .9)
    ic.paint(bolt(173, 128, 26), (255, 255, 255))


@reg("wiring", "cafe")
def _(ic):
    plate(ic, "orange")
    sc = monitor(ic, 40, 52, 216, 160, (30, 50, 90))
    ic.glow(bolt(128, 106, 36), (255, 220, 100), 6, .6)
    ic.body(bolt(128, 106, 34), "yellow")
    cup(ic, 186, 196, 34)


@reg("wiring", "idc")
def _(ic):
    plate(ic, "orange")
    serv_units(ic, 46, 210, (50, 98, 146), 38)
    small_badge(ic, 196, 192, 32, lambda ic, x, y, r: ic.paint(bolt(x, y, r * .55), WHITE), "orange")


@reg("wiring", "gpu")
def _(ic):
    plate(ic, "green")
    gpu_card(ic, 30, 76, 196, 92)


@reg("wiring", "gpu_cafe")
def _(ic):
    plate(ic, "green")
    gpu_card(ic, 28, 64, 196, 88)
    small_badge(ic, 188, 190, 36, lambda ic, x, y, r: cup(ic, x, y + 2, r * .75), "brown")


@reg("wiring", "rack")
def _(ic):
    plate(ic, "green")
    serv_units(ic, 46, 210, (46, 94, 142), 40, fan_l=True)
    ic.paint(R(46, 188, 210, 202, 6), (255, 255, 255), .5)


@reg("wiring", "model")
def _(ic):
    plate(ic, "purple")
    neuron_glyph(ic, 128, 128, 64, (2, 3, 2), 17, edge_a=1.0)


@reg("wiring", "model_repo")
def _(ic):
    plate(ic, "purple")
    wg(ic, R(46, 100, 210, 206, 14))
    wg(ic, R(36, 62, 220, 108, 12))
    neuron_glyph(ic, 128, 156, 42, (2, 3, 2), 9, fill=hue("purple")[1], edge=hue("purple")[1], edge_a=.6)


@reg("wiring", "self")
def _(ic):
    plate(ic, "purple")
    g = union(C(128, 108, 58), m_poly([(86, 140), (170, 140), (156, 186), (100, 186)]))
    ic.glow(g, (255, 240, 160), 14, .7)
    ic.shade(g, .3, 0, 3, 2.5)
    ic.body(g, "yellow")
    col(ic, R(100, 182, 156, 208, 8), "steel")
    ic.paint(m_line([(116, 112), (128, 126), (142, 100)], 6), (255, 255, 255), .9)


def gear_badge(ic, cx=192, cy=192, r=36):
    small_badge(ic, cx, cy, r, lambda ic, x, y, rr_: ic.paint(m_gear(x, y, rr_ * .66, 8, .22, .36), WHITE), "steel")


@reg("wiring", "run_0")
def _(ic):
    plate(ic, "purple")
    eye = union(m_poly([(28, 112), (60, 80), (128, 62), (196, 80), (228, 112), (196, 144), (128, 162), (60, 144)]))
    wg(ic, eye)
    ic.body(C(128, 112, 34), "blue")
    ic.paint(C(128, 112, 14), (20, 24, 40))
    ic.paint(C(116, 100, 7), (255, 255, 255), .9)
    gear_badge(ic, 190, 190, 38)


@reg("wiring", "run_1")
def _(ic):
    plate(ic, "purple")
    wg(ic, R(34, 52, 190, 188, 16))
    for i, w_ in enumerate((120, 96, 112, 70)):
        ic.paint(m_line([(54, 82 + i * 26), (54 + w_, 82 + i * 26)], 8), hue("purple")[1], .8)
    ic.paint(R(54 + 74, 150, 54 + 82, 168, 2), (230, 90, 60))
    gear_badge(ic, 196, 194, 38)


@reg("wiring", "label")
def _(ic):
    plate(ic, "teal")
    tag = m_poly([(40, 60), (150, 60), (216, 128), (150, 196), (40, 196)])
    tag = union(R(40, 60, 150, 196, 14), m_poly([(130, 60), (216, 128), (130, 196)]))
    wg(ic, tag)
    ic.paint(C(80, 128, 14), dark("teal"), .6)
    draw_pencil(ic, 150, 170, 82, 22, -135 + 180)


@reg("wiring", "contract")
def _(ic):
    plate(ic, "teal")
    page(ic, 50, 26, 196, 230, 36, lines=0)
    for i in range(3):
        ic.paint(m_line([(72, 98 + i * 24), (172 - (28 if i == 2 else 0), 98 + i * 24)], 6), (150, 160, 180))
    coin(ic, 172, 188, 40)


@reg("wiring", "train")
def _(ic):
    plate(ic, "teal")
    a1 = m_arc(128, 128, 62, 200, 340, 18)
    a2 = m_arc(128, 128, 62, 20, 160, 18)
    wg(ic, union(a1, a2))
    wg(ic, m_poly([(176, 60), (210, 104), (150, 104)]))
    wg(ic, m_poly([(80, 196), (46, 152), (106, 152)]))
    ic.body(C(128, 128, 22), "gold")


@reg("wiring", "pretrain")
def _(ic):
    plate(ic, "teal")
    gl = C(128, 128, 84)
    ic.shade(gl, .3, 0, 3, 2.5)
    ic.body(gl, "blue", top=(150, 215, 255))
    ic.paint(ring(128, 128, 84, 5), (255, 255, 255), .6)
    ic.paint(m_line([(48, 128), (208, 128)], 4), (255, 255, 255), .6)
    ic.paint(intersect(ring_ellipse(88, 44, 168, 212, 4), gl), (255, 255, 255), .6)
    ic.paint(intersect(ring_ellipse(60, 44, 196, 212, 4), gl), (255, 255, 255), .45)
    wg(ic, sparkle(192, 70, 32))


@reg("wiring", "ajie")
def _(ic):
    plate(ic, "teal")
    zig = [(64, 200)]
    x = 64
    for i in range(8):
        zig.append((x + 16, 216 if i % 2 == 0 else 200)); x += 16
    rc = m_poly([(64, 38), (192, 38), (192, 200)] + [(192 - 16 * i, 216 if i % 2 == 0 else 200) for i in range(1, 9)])
    wg(ic, rc)
    for i, w_ in enumerate((90, 70, 80)):
        ic.paint(m_line([(80, 70 + i * 26), (80 + w_, 70 + i * 26)], 6), (150, 160, 180))
    ic.paint(m_text("¥", "black", 60, 128, 166), hue("teal")[1])


@reg("wiring", "life")
def _(ic):
    plate(ic, "teal")
    ic.body(m_heart(128, 124, 160), "pink")
    wg(ic, sparkle(190, 70, 26))


# ═══ DESK ════════════════════════════════════════════════════════════════════
@reg("desk", "longtext")
def _(ic):
    plate(ic, "blue")
    wg(ic, R(40, 40, 216, 216, 16))
    for i, w_ in enumerate((136, 120, 136, 128, 100)):
        ic.paint(m_line([(60, 72 + i * 28), (60 + w_, 72 + i * 28)], 8), hue("blue")[1], .85)
    for k in range(3):
        ic.paint(C(158 + k * 20, 184, 5), hue("blue")[2])


@reg("desk", "mnist")
def _(ic):
    plate(ic, "teal")
    wg(ic, R(44, 40, 212, 216, 18))
    for t in range(2):
        pass
    ic.paint(m_line([(86, 78), (168, 78), (118, 184)], 18), (30, 50, 70))
    ic.paint(m_line([(110, 130), (150, 130)], 10), (30, 50, 70))
    ic.paint(m_line([(60, 60), (74, 60)], 5), hue("teal")[1], .0)


@reg("desk", "poems")
def _(ic):
    plate(ic, "brown")
    wg(ic, R(52, 44, 204, 212, 8), (255, 250, 232), (228, 210, 170))
    for y in (36, 204):
        wg(ic, R(40, y, 216, y + 20, 10), (190, 90, 60), (120, 50, 30))
    ic.paint(m_text("诗", "cjk", 100, 128, 128), (170, 40, 40))


@reg("desk", "sense")
def _(ic):
    plate(ic, "sky")
    cl = cloud(128, 112, 72)
    wg(ic, cl)
    for x in (92, 140, 188):
        pass
    ic.paint(m_line([(94, 108), (120, 134), (168, 80)], 16), (60, 170, 80))
    wg(ic, C(86, 196, 12))
    wg(ic, C(60, 214, 7))


@reg("desk", "arith")
def _(ic):
    plate(ic, "orange")
    wg(ic, R(56, 36, 200, 220, 16))
    ic.paint(R(72, 52, 184, 94, 6), (40, 60, 50))
    ic.paint(m_text("42", "mono", 34, 160, 74), (150, 255, 170))
    for r_ in range(3):
        for c_ in range(3):
            x, y = 74 + c_ * 38, 108 + r_ * 36
            k = r_ * 3 + c_
            ic.paint(R(x, y, x + 30, y + 26, 6), hue("orange")[1] if c_ == 2 else (186, 196, 214))


@reg("desk", "logic")
def _(ic):
    plate(ic, "red")
    a, b = C(100, 128, 58), C(156, 128, 58)
    ic.shade(union(a, b), .3, 0, 3, 2.5)
    ic.paint(a, (255, 255, 255), .88)
    ic.paint(b, (255, 255, 255), .88)
    ic.paint(intersect(a, b), (255, 200, 80))
    ic.paint(ring(100, 128, 58, 3), dark("red"), .4)
    ic.paint(ring(156, 128, 58, 3), dark("red"), .4)
    ic.paint(m_text("?", "black", 56, 128, 128), dark("red"))


@reg("desk", "danmu")
def _(ic):
    plate(ic, "pink")
    scr = R(32, 50, 224, 206, 16)
    ic.shade(scr, .3, 0, 3, 2.5)
    ic.paint(scr, vgrad((60, 66, 100), (24, 26, 48), 50, 206))
    ic.rim(scr, (255, 255, 255), 2, .5)
    for (x, y, w_, c_) in [(46, 80, 110, (255, 255, 255)), (96, 112, 112, (255, 200, 90)), (52, 144, 84, (130, 220, 255)), (110, 174, 90, (255, 130, 170))]:
        ic.paint(m_line([(x, y), (x + w_, y)], 14), c_, .95)


@reg("desk", "spam")
def _(ic):
    plate(ic, "steel")
    env = R(34, 70, 222, 190, 14)
    wg(ic, env)
    ic.paint(m_line([(40, 82), (128, 144), (216, 82)], 8), (150, 162, 184))
    small_badge(ic, 190, 80, 36, lambda ic, x, y, r: ic.paint(m_text("!", "black", r * 1.4, x, y + 1), WHITE), "red")


@reg("desk", "meme")
def _(ic):
    plate(ic, "purple")
    f = C(128, 128, 88)
    ic.shade(f, .3, 0, 3, 2.5)
    ic.body(f, "yellow")
    # sunglasses
    ic.paint(union(R(62, 96, 118, 134, 14), R(138, 96, 194, 134, 14), m_rect(114, 100, 142, 110)), (30, 30, 40))
    ic.paint(m_line([(70, 104), (92, 100)], 4), (255, 255, 255), .5)
    ic.paint(m_line([(146, 104), (168, 100)], 4), (255, 255, 255), .5)
    mouth = cut(C(128, 150, 40), m_rect(60, 100, 200, 150))
    ic.paint(mouth, (120, 40, 20))
    ic.paint(cut(mouth, m_rect(60, 100, 200, 158)), (255, 255, 255))
    ic.paint(intersect(mouth, m_rect(96, 150, 160, 160)), (255, 255, 255))


@reg("desk", "headline")
def _(ic):
    plate(ic, "yellow")
    wg(ic, R(36, 44, 220, 212, 12), (252, 252, 250), (220, 220, 214))
    ic.paint(R(52, 60, 204, 108, 4), (220, 50, 50))
    ic.paint(m_text("!!", "black", 56, 128, 85), WHITE)
    for i in range(3):
        ic.paint(m_line([(54, 130 + i * 24), (130, 130 + i * 24)], 7), (120, 126, 140), .8)
    ic.paint(R(150, 124, 204, 190, 4), (170, 178, 196))


@reg("desk", "review")
def _(ic):
    plate(ic, "green")
    b = R(30, 44, 226, 172, 24)
    b = union(b, m_poly([(70, 160), (110, 160), (66, 206)]))
    wg(ic, b)
    for i in range(5):
        ic.body(m_star(60 + i * 34, 106, 16), "gold", rim=False) if False else None
        ic.paint(m_star(60 + i * 34, 106, 17), (245, 190, 40))
    ic.paint(m_star(60 + 4 * 34, 106, 17), (200, 206, 218))
    small_badge(ic, 190, 186, 36, lambda ic, x, y, r: (ic.paint(m_line([(x - r * .4, y - r * .4), (x + r * .4, y + r * .4)], 6), WHITE), ic.paint(m_line([(x - r * .4, y + r * .4), (x + r * .4, y - r * .4)], 6), WHITE)), "red")


@reg("desk", "cifar")
def _(ic):
    plate(ic, "dark")
    for r_ in range(3):
        for c_ in range(3):
            x, y = 44 + c_ * 58, 44 + r_ * 58
            sel = (r_, c_) in ((0, 1), (1, 2), (2, 0))
            wg(ic, R(x, y, x + 50, y + 50, 8), (230, 238, 250) if not sel else (200, 240, 205), (170, 180, 200) if not sel else (110, 190, 120), shade=.2)
            if sel:
                ic.paint(m_line([(x + 12, y + 26), (x + 22, y + 36), (x + 40, y + 14)], 8), (30, 140, 60))


@reg("desk", "go")
def _(ic):
    plate(ic, "gold")
    bd = R(32, 32, 224, 224, 14)
    ic.paint(bd, vgrad((238, 190, 100), (206, 150, 60), 32, 224))
    for i in range(5):
        ic.paint(m_line([(60 + i * 34, 52), (60 + i * 34, 204)], 3), (110, 70, 20), .8)
        ic.paint(m_line([(52, 60 + i * 34), (204, 60 + i * 34)], 3), (110, 70, 20), .8)
    for (x, y, black) in [(94, 94, True), (128, 94, False), (128, 128, True), (162, 128, False), (94, 162, False), (162, 162, True)]:
        st = C(x, y, 17)
        ic.shade(st, .4, 0, 3, 2)
        ic.paint(st, rgrad((90, 90, 100), (10, 10, 16), x - 5, y - 6, 22) if black else rgrad((255, 255, 255), (186, 192, 204), x - 5, y - 6, 22))


# ═══ TIEBA ═══════════════════════════════════════════════════════════════════
@reg("tieba", "gpu")
def _(ic):
    plate(ic, "green")
    gpu_card(ic, 30, 76, 196, 92)


@reg("tieba", "ai")
def _(ic):
    plate(ic, "blue")
    neuron_glyph(ic, 128, 128, 70, (3, 4, 3), 14)


@reg("tieba", "santi")
def _(ic):
    plate(ic, "dark")
    for (x, y, r_) in [(70, 70, 3), (190, 60, 3), (200, 190, 3), (60, 196, 3), (160, 42, 2)]:
        ic.paint(C(x, y, r_), (255, 255, 255))
    ic.paint(ring_ellipse(30, 70, 226, 186, 4), (160, 190, 255), .55)
    for (x, y, r_, c_) in [(88, 106, 36, "orange"), (168, 100, 26, "yellow"), (132, 170, 30, "red")]:
        ic.glow(C(x, y, r_), hue(c_)[0], r_ * .5, .7)
        ic.body(C(x, y, r_), c_)


@reg("tieba", "souls")
def _(ic):
    plate(ic, "red")
    # a coiled sword in a bonfire
    flame = union(C(128, 168, 56), m_poly([(72, 170), (92, 100), (112, 140), (128, 64), (148, 140), (168, 96), (186, 170)]))
    ic.glow(flame, (255, 190, 60), 12, .7)
    ic.body(flame, "orange")
    inner = union(C(128, 176, 30), m_poly([(100, 176), (118, 120), (128, 146), (144, 108), (156, 176)]))
    ic.paint(inner, vgrad((255, 250, 180), (250, 200, 50), 110, 210))
    ic.shade(m_rect(122, 30, 134, 190), .3, 0, 3, 2)
    ic.paint(m_poly([(128, 26), (138, 52), (138, 196), (118, 196), (118, 52)]), vgrad((255, 255, 255), (170, 180, 196), 26, 196))
    wg(ic, R(90, 150, 166, 164, 6), (240, 244, 250), (160, 170, 190))
    ic.paint(R(120, 196, 136, 216, 3), (110, 70, 40))
    ic.paint(m_line([(60, 214), (196, 214)], 10), (80, 40, 30))


# ═══ LIFE ════════════════════════════════════════════════════════════════════
@reg("life", "zhou_card")
def _(ic):
    plate(ic, "green")
    gpu_card(ic, 28, 74, 176, 84)
    wg(ic, arrow([(130, 206), (212, 206)], 14, 40))


@reg("life", "bad_sectors")
def _(ic):
    plate(ic, "dark")
    d = C(128, 128, 90)
    ic.shade(d, .35, 0, 3, 2.5)
    ic.paint(d, rgrad((200, 208, 222), (110, 120, 140), 100, 100, 130))
    ic.paint(C(128, 128, 36), (60, 66, 80))
    ic.paint(C(128, 128, 10), (220, 226, 236))
    ic.paint(m_arc(128, 128, 66, 200, 300, 4), (255, 255, 255), .5)
    arm = m_poly([(196, 54), (212, 66), (140, 132), (126, 120)])
    ic.paint(arm, (70, 76, 90))
    ic.paint(m_line([(60, 150), (88, 134), (84, 164), (116, 150)], 8), (230, 50, 50))
    ic.paint(m_line([(60, 180), (86, 192), (100, 176)], 6), (230, 50, 50), .8)


@reg("life", "broadband")
def _(ic):
    plate(ic, "sky")
    wg(ic, R(40, 150, 216, 204, 16))
    for x in (66, 100):
        ic.paint(C(x, 177, 7), (80, 200, 120))
    ic.paint(m_line([(140, 177), (194, 177)], 8), (160, 172, 192))
    for i, r_ in enumerate((92, 64, 36)):
        ic.paint(m_arc(128, 150, r_, 225, 315, 12 if i else 12), (255, 255, 255), 1)
    ic.paint(C(128, 140, 9), (255, 255, 255))


@reg("life", "landlord_meter")
def _(ic):
    plate(ic, "yellow")
    wg(ic, R(52, 34, 204, 222, 20), (252, 252, 250), (214, 218, 226))
    ic.paint(R(72, 56, 184, 104, 8), (40, 46, 56))
    for i, d_ in enumerate("0472"):
        ic.paint(m_text(d_, "mono", 34, 90 + i * 25, 80), (255, 230, 120))
    dial = C(128, 152, 38)
    ic.paint(dial, (236, 240, 248))
    ic.paint(ring(128, 152, 38, 5), (150, 160, 176))
    ic.paint(m_line([(128, 152), (150, 132)], 6), (220, 50, 50))
    ic.paint(bolt(128, 196, 14), (240, 170, 20))


@reg("life", "roommate_fan")
def _(ic):
    plate(ic, "teal")
    ic.paint(m_line([(128, 168), (128, 210)], 14), (220, 230, 242))
    wg(ic, R(78, 206, 178, 224, 8))
    ic.paint(C(128, 108, 76), (255, 255, 255), .25)
    bl = blank()
    for k in range(3):
        bl = union(bl, rotate_mask(m_ellipse(116, 36, 160, 106), k * 120, 128, 108))
    wg(ic, bl)
    ic.paint(ring(128, 108, 78, 6), (255, 255, 255), .9)
    col(ic, C(128, 108, 16), "steel")


@reg("life", "forum_post")
def _(ic):
    plate(ic, "blue")
    b = bubble(ic, "paper", 32, 44, 224, 176, tail_left=True) if False else None
    m = union(R(32, 44, 224, 176, 28), m_poly([(60, 160), (100, 160), (62, 208)]))
    wg(ic, m)
    for i, w_ in enumerate((140, 120, 90)):
        ic.paint(m_line([(56, 78 + i * 28), (56 + w_, 78 + i * 28)], 8), hue("blue")[1], .8)
    draw_pencil(ic, 150, 198, 62, 18, -45)


@reg("life", "zhou_loan")
def _(ic):
    plate(ic, "red")
    env = R(56, 34, 200, 222, 16)
    ic.shade(env, .3, 0, 3, 2.5)
    ic.body(env, "red")
    top = m_poly([(56, 50), (128, 120), (200, 50), (200, 34), (56, 34)])
    ic.paint(m_poly([(56, 40), (128, 130), (200, 40)]), (170, 25, 30), .6)
    coin(ic, 128, 138, 36)


@reg("life", "licai_call")
def _(ic):
    plate(ic, "orange")
    ph = R(50, 40, 160, 218, 22)
    wg(ic, ph)
    ic.paint(R(62, 62, 148, 188, 6), (30, 50, 80))
    ic.paint(m_line([(72, 168), (96, 140), (114, 154), (140, 106)], 7), (250, 200, 40))
    ic.paint(m_poly([(148, 92), (136, 94), (142, 104)]), (250, 200, 40))
    # ringing arcs
    for r_ in (22, 40):
        ic.paint(m_arc(172, 84, r_, 270, 360, 8), (255, 255, 255))
    ic.paint(C(172, 84, 8), (255, 255, 255))


@reg("life", "zhou_favour")
def _(ic):
    plate(ic, "purple")
    body = R(60, 92, 196, 170, 16)
    wg(ic, body)
    col(ic, R(166, 106, 226, 156, 8), "steel")
    ic.paint(R(196, 118, 212, 128, 2), (40, 44, 56))
    ic.paint(R(196, 136, 212, 146, 2), (40, 44, 56))
    ic.paint(m_heart(112, 132, 40), (235, 90, 150))
    ic.paint(C(76, 112, 5), hue("purple")[1], .5)


# ═══ MINIGAME ════════════════════════════════════════════════════════════════
def mg(id_, color):
    def d(fn):
        def run(ic):
            dplate(ic, color)
            fn(ic, color)
        REG.append(("minigame", id_, run))
        return fn
    return d


@mg("draw", "blue")
def _(ic, c):
    ic.paint(m_line([(66, 150), (92, 100), (122, 156), (150, 92), (176, 140)], 12), (255, 255, 255), .9)
    draw_pencil(ic, 112, 196, 100, 26, -45)


@mg("slice", "blue")
def _(ic, c):
    b = union(R(46, 60, 196, 160, 24), m_poly([(72, 150), (112, 150), (72, 194)]))
    wg(ic, b)
    for i, w_ in enumerate((100, 70)):
        ic.paint(m_line([(70, 90 + i * 26), (70 + w_, 90 + i * 26)], 8), hue("blue")[1], .8)
    ic.glow(m_line([(34, 210), (222, 50)], 8), (255, 255, 255), 6, .8)
    ic.paint(m_line([(34, 210), (222, 50)], 9), (255, 255, 255))
    ic.paint(m_line([(36, 208), (220, 52)], 4), (210, 60, 60))


@mg("esp", "blue")
def _(ic, c):
    card = R(78, 40, 178, 190, 14)
    wg(ic, card)
    ic.paint(m_star(128, 112, 38), (240, 190, 40))
    ic.paint(m_line([(100, 160), (156, 160)], 5), hue("blue")[1], .6)
    for sx in (-1, 1):
        ic.paint(m_arc(128 + sx * 62, 118, 22, 270 if sx > 0 else 90, 450 if sx > 0 else 270, 6), (255, 255, 255), .0)
        ic.paint(m_line([(128 + sx * 66, 88), (128 + sx * 84, 78)], 6), (255, 255, 255), .85)
        ic.paint(m_line([(128 + sx * 66, 118), (128 + sx * 90, 118)], 6), (255, 255, 255), .85)
        ic.paint(m_line([(128 + sx * 66, 148), (128 + sx * 84, 158)], 6), (255, 255, 255), .85)


@mg("forgetax", "orange")
def _(ic, c):
    an = union(R(60, 140, 196, 172, 8), m_poly([(84, 140), (172, 140), (150, 108), (100, 108)]), R(96, 172, 160, 208, 6))
    wg(ic, an, (190, 200, 216), (90, 100, 120))
    ic.paint(m_poly([(50, 120), (90, 120), (100, 140), (60, 140)]), (150, 160, 180))
    ic.paint(m_line([(150, 46), (110, 100)], 12), (170, 110, 50))
    col(ic, R(122, 34, 190, 74, 8), "steel")
    for (x, y) in [(76, 100), (170, 108), (190, 126)]:
        ic.paint(sparkle(x, y, 12), (255, 230, 120))


@mg("captcha12306", "blue")
def _(ic, c):
    body = R(60, 44, 196, 200, 36)
    wg(ic, body)
    ic.paint(R(76, 66, 180, 120, 12), (40, 80, 130))
    ic.paint(m_line([(86, 80), (112, 80)], 4), (255, 255, 255), .5)
    ic.paint(C(94, 160, 11), (250, 210, 70)); ic.paint(C(162, 160, 11), (250, 210, 70))
    ic.paint(R(112, 150, 144, 168, 5), (200, 70, 60))
    ic.paint(m_line([(70, 216), (186, 216)], 8), (255, 255, 255), .8)


@mg("alphago", "blue")
def _(ic, c):
    for x in (60, 128, 196):
        ic.paint(m_line([(x, 40), (x, 216)], 3), (255, 255, 255), .5)
        ic.paint(m_line([(40, x - 4 if False else x), (216, x)], 3), (255, 255, 255), .5)
    for (x, y, b) in [(94, 94, 1), (162, 94, 0), (94, 162, 0), (162, 162, 1)]:
        st = C(x, y, 28)
        ic.shade(st, .4, 0, 3, 2)
        ic.paint(st, rgrad((90, 90, 100), (10, 10, 16), x - 8, y - 10, 36) if b else rgrad((255, 255, 255), (180, 186, 200), x - 8, y - 10, 36))


@mg("feihualing", "blue")
def _(ic, c):
    pet = blank()
    for k in range(5):
        pet = union(pet, rotate_mask(m_ellipse(104, 40, 152, 112), k * 72, 128, 112))
    ic.shade(pet, .3, 0, 3, 2.5)
    ic.body(pet, "pink")
    ic.body(C(128, 112, 22), "gold")
    ic.paint(m_line([(128, 134), (122, 214)], 8), (80, 170, 80))
    ic.paint(m_ellipse(124, 170, 176, 194), (80, 180, 90))


@mg("doutu", "blue")
def _(ic, c):
    f = C(128, 124, 82)
    ic.shade(f, .3, 0, 3, 2.5)
    ic.body(f, "yellow")
    for x in (98, 158):
        ic.paint(m_arc(x, 112, 16, 200, 340, 7), (60, 40, 20))
    mouth = cut(C(128, 138, 44), m_rect(70, 80, 190, 138))
    ic.paint(mouth, (120, 40, 20))
    ic.paint(intersect(mouth, m_rect(96, 138, 160, 148)), (255, 255, 255))
    for x in (60, 196):
        ic.paint(m_ellipse(x - 6, 130, x + 6, 168), (120, 200, 255), .9)


@mg("swipe", "blue")
def _(ic, c):
    wg(ic, R(78, 44, 178, 188, 18))
    ic.paint(m_line([(94, 76), (162, 76)], 8), hue("blue")[1], .8)
    ic.paint(m_line([(94, 100), (150, 100)], 8), hue("blue")[1], .6)
    ic.paint(R(94, 124, 162, 170, 6), (200, 210, 228))
    wg(ic, arrow([(66, 208), (28, 208)], 10, 30)) if False else None
    ic.paint(arrow([(116, 214), (44, 214)], 11, 30), (255, 110, 110))
    ic.paint(arrow([(140, 214), (212, 214)], 11, 30), (120, 240, 140))


@mg("zhaoshuijun", "blue")
def _(ic, c):
    b = union(R(40, 40, 200, 150, 22), m_poly([(60, 140), (96, 140), (58, 180)]))
    wg(ic, b)
    for i in range(3):
        ic.paint(m_star(76 + i * 30, 88, 14), (245, 190, 40))
    ic.paint(m_line([(60, 118), (150, 118)], 6), (170, 180, 196))
    ln = C(156, 158, 36)
    ic.paint(ln, rgrad((235, 247, 255), (150, 200, 240), 146, 146, 40), .85)
    ic.paint(m_line([(182, 184), (218, 220)], 16), (60, 66, 82))
    ic.paint(ring(156, 158, 36, 9), (240, 244, 250))


@mg("dadishu", "green")
def _(ic, c):
    ic.paint(m_ellipse(46, 150, 210, 214), (90, 56, 30))
    mole = union(C(128, 140, 48), R(80, 140, 176, 168, 8))
    mole = cut(mole, m_rect(60, 164, 200, 230))
    ic.shade(mole, .3, 0, 3, 2.5)
    ic.body(mole, "brown")
    ic.paint(C(128, 134, 12), (240, 130, 150))
    for x in (106, 150):
        ic.paint(C(x, 118, 6), (20, 20, 20))
    ic.paint(m_ellipse(46, 168, 210, 214), (60, 36, 20))
    ic.paint(cut(m_ellipse(46, 160, 210, 214), m_ellipse(60, 160, 196, 200)), (110, 70, 40))
    ic.paint(m_line([(190, 40), (160, 100)], 10), (200, 150, 80))
    col(ic, R(166, 30, 220, 68, 8), "red")


@mg("shuishiwodi", "green")
def _(ic, c):
    hat = union(m_ellipse(46, 120, 210, 160), R(78, 54, 178, 136, 28))
    ic.shade(hat, .3, 0, 3, 2.5)
    ic.body(hat, "dark")
    ic.paint(m_rect(80, 108, 176, 124), (190, 60, 50))
    sg = union(R(60, 168, 118, 202, 12), R(138, 168, 196, 202, 12), m_rect(112, 172, 142, 182))
    wg(ic, sg, (70, 76, 90), (20, 22, 30))
    ic.paint(m_text("?", "black", 40, 128, 62), (255, 255, 255)) if False else None


@mg("kaixincidian", "green")
def _(ic, c):
    burst = m_star(128, 128, 100, 70, 12, -90)
    ic.shade(burst, .3, 0, 3, 2.5)
    ic.body(burst, "yellow")
    ic.paint(m_text("?", "black", 130, 128, 128), (255, 255, 255))
    ic.paint(m_text("?", "black", 130, 128, 122), (60, 130, 220))


@mg("danmushooter", "orange")
def _(ic, c):
    ship = m_poly([(128, 40), (172, 156), (128, 134), (84, 156)])
    wg(ic, ship)
    ic.paint(m_poly([(128, 66), (146, 116), (128, 108), (110, 116)]), hue("orange")[1])
    for (x, y) in [(70, 70), (186, 78), (70, 190), (190, 198), (128, 200)]:
        ic.glow(C(x, y, 10), (255, 120, 140), 6, .7)
        ic.body(C(x, y, 10), "pink")


@mg("spidersnake", "green")
def _(ic, c):
    path = [(60, 190), (100, 190), (150, 190), (190, 160), (170, 118), (116, 108), (88, 74), (128, 58)]
    for i, (x, y) in enumerate(path[:-1]):
        r_ = 22 - i * .3
        ic.shade(C(x, y, r_), .3, 0, 3, 2)
        ic.paint(C(x, y, r_), vgrad((150, 235, 120), (40, 140, 50), y - r_, y + r_))
    hx, hy = path[-1]
    head = C(hx, hy, 28)
    ic.shade(head, .3, 0, 3, 2)
    ic.body(head, "lime")
    for dx in (-10, 10):
        ic.paint(C(hx + dx, hy - 4, 8), (255, 255, 255)); ic.paint(C(hx + dx, hy - 3, 4), (20, 20, 20))
    ic.paint(m_line([(hx, hy + 24), (hx, hy + 40), (hx - 8, hy + 50)], 4), (230, 50, 60))


@mg("vramtetris", "orange")
def _(ic, c):
    cells = [(1, 0), (0, 1), (1, 1), (2, 1), (0, 2), (0, 3), (1, 3)]
    cols = ["sky", "sky", "sky", "sky", "orange", "orange", "orange"]
    for (cx_, cy_), cl in zip(cells, cols):
        x, y = 58 + cx_ * 46, 40 + cy_ * 46
        m = R(x, y, x + 42, y + 42, 7)
        ic.body(m, cl)


@mg("merge2048", "green")
def _(ic, c):
    t = R(48, 48, 208, 208, 24)
    ic.shade(t, .3, 0, 3, 2.5)
    ic.body(t, "yellow")
    ic.paint(m_text("2048", "black", 54, 128, 130), (255, 255, 255))
    ic.paint(m_text("2048", "black", 54, 128, 126), (180, 110, 20)) if False else None


@mg("sentence", "green")
def _(ic, c):
    rows = [[(56, 0), (74, 1)], [(86, 2), (44, 0)], [(40, 1), (92, 0)]]
    cs = ["blue", "orange", "green"]
    y = 52
    for row in rows:
        x = 50
        for w_, k in row:
            m = R(x, y, x + w_, y + 38, 10)
            ic.body(m, cs[k])
            x += w_ + 10
        y += 52


@mg("chuanhua", "green")
def _(ic, c):
    a = union(R(32, 44, 144, 130, 24), m_poly([(52, 120), (84, 120), (50, 158)]))
    wg(ic, a)
    ic.paint(m_text("文", "cjk", 56, 88, 88), hue("green")[2])
    b = union(R(112, 112, 224, 198, 24), m_poly([(190, 188), (220, 188), (214, 224)]))
    ic.shade(b, .3, 0, 3, 2.5)
    ic.body(b, "sky")
    ic.paint(m_text("A", "black", 56, 168, 156), (255, 255, 255))


@mg("turing", "purple")
def _(ic, c):
    ic.paint(C(84, 100, 28), (255, 255, 255))
    ic.paint(m_pie(84, 188, 50, 180, 360), (255, 255, 255))
    ic.shade(R(134, 62, 206, 138, 14), .3, 0, 3, 2)
    ic.body(R(134, 62, 206, 138, 14), "steel")
    for x in (156, 184):
        ic.paint(C(x, 94, 8), (140, 240, 255))
    ic.paint(R(150, 112, 190, 122, 4), (40, 44, 56))
    ic.paint(m_line([(170, 62), (170, 46)], 5), (210, 216, 228)); ic.paint(C(170, 44, 7), (230, 70, 70))
    ic.paint(m_text("?", "black", 56, 150, 196), (255, 230, 120))


@mg("qingwen", "purple")
def _(ic, c):
    h = m_heart(128, 124, 170)
    ic.shade(h, .3, 0, 3, 2.5)
    ic.body(h, "pink")
    for x in (102, 154):
        ic.paint(C(x, 106, 8), (255, 255, 255)); ic.paint(C(x, 108, 4), (60, 20, 40))
    ic.paint(m_arc(128, 118, 28, 30, 150, 6), (255, 255, 255))
    ic.paint(m_arc(128, 118, 28, 30, 150, 6), (255, 255, 255))


@mg("zhaobutong", "orange")
def _(ic, c):
    for x0 in (34, 134):
        fr = R(x0, 60, x0 + 90, 190, 10)
        wg(ic, fr)
        ic.paint(R(x0 + 8, 68, x0 + 82, 182, 5), (120, 190, 240))
        ic.paint(C(x0 + 30, 100, 11), (255, 230, 120))
        ic.paint(m_poly([(x0 + 8, 182), (x0 + 38, 138), (x0 + 62, 164), (x0 + 82, 182)]), (70, 150, 80))
    ic.paint(C(194, 160, 9), (230, 60, 60))
    ic.paint(ring(194, 160, 20, 4), (230, 60, 60))


@mg("lianliankan", "purple")
def _(ic, c):
    t1, t2 = R(36, 46, 106, 116, 12), R(150, 140, 220, 210, 12)
    wg(ic, t1); wg(ic, t2)
    ic.paint(m_star(71, 82, 24), (240, 190, 40))
    ic.paint(m_star(185, 176, 24), (240, 190, 40))
    ic.paint(m_line([(106, 81), (128, 81), (128, 175), (150, 175)], 8), (255, 255, 255))
    ic.paint(m_line([(106, 81), (128, 81), (128, 175), (150, 175)], 8), (255, 255, 255))


# ═══ run ═════════════════════════════════════════════════════════════════════
def render(i):
    group, id_, fn = REG[i]
    ic = Icon()
    fn(ic)
    path = os.path.join(OUT, group, fname(id_) + ".png")
    ic.save(path, 128)
    return path


if __name__ == "__main__":
    want = set(sys.argv[1:])
    idx = [i for i, j in enumerate(REG) if not want or j[0] in want or j[1] in want]
    jobs = [REG[i] for i in idx]
    with Pool() as p:
        for path in p.imap_unordered(render, idx):
            pass
    groups = sorted({g for g, _, _ in jobs})
    for g in groups:
        d = os.path.join(OUT, g)
        sheet(d, os.path.join(SCRATCH, "sheet_%s.png" % g), cell=128, cols=8)
        sheet(d, os.path.join(SCRATCH, "sheet_%s_small.png" % g), cell=40, cols=16)
    print("rendered", len(jobs), "icons:", {g: sum(1 for j in jobs if j[0] == g) for g in groups})
