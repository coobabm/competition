"""Desktop identity icons (256 px), tray-app icons and tray-popup badges (128 px) for 灵光, plus the four Oxygen
Start-menu keys that our apps reuse (Calculator = 摆渡众包, Commander = 喵鱼, Reminder = 贴吧, Widget Library = 摆渡).

Run:  python3 Tools/Icons/desktop_icons.py [name ...]      (no names = everything)
Writes Assets/LingGuangV05/Resources/LingGuangV05/Icons/{desktop,tray,popup}/<id>.png and the four Oxygen PNGs under
Assets/StreamingAssets/HongmengOS-Aero2010/Oxygen/.
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from iconkit import *
from multiprocessing import Pool

ROOT = "/Users/kk/unity/competition/Assets"
ICONS = ROOT + "/LingGuangV05/Resources/LingGuangV05/Icons"
OXY = ROOT + "/StreamingAssets/HongmengOS-Aero2010/Oxygen"

# custom (light, base, dark) hues
INDIGO = ((112, 128, 255), (48, 58, 226), (20, 24, 130))
BODU = ((110, 160, 255), (52, 104, 214), (22, 44, 134))
TIEBA = ((110, 170, 255), (44, 110, 224), (18, 52, 140))
KK = ((99, 212, 246), (38, 125, 181), (13, 64, 118))
MIAOYU = ((255, 228, 100), (250, 192, 24), (196, 126, 0))
TAOHUO = ((255, 176, 96), (255, 110, 20), (180, 50, 0))
HOME = ((150, 230, 170), (44, 170, 110), (14, 100, 66))
RED = ((255, 130, 120), (216, 40, 48), (126, 12, 24))
NAVY = ((110, 140, 200), (34, 56, 110), (12, 20, 52))


# ── small helpers ───────────────────────────────────────────────────────────────

def m_oval(cx, cy, rx, ry, deg=0):
    m = m_ellipse(cx - rx, cy - ry, cx + rx, cy + ry)
    return rotate_mask(m, deg, cx, cy) if deg else m


def paw_mask(cx, cy, s):
    """A paw print about s design units tall, centred on (cx, cy)."""
    pad = union(m_oval(cx, cy + .20 * s, .27 * s, .21 * s), m_oval(cx, cy + .12 * s, .20 * s, .24 * s))
    toes = [m_oval(cx - .36 * s, cy - .02 * s, .10 * s, .145 * s, 28), m_oval(cx - .13 * s, cy - .25 * s, .10 * s, .15 * s, 8),
            m_oval(cx + .13 * s, cy - .25 * s, .10 * s, .15 * s, -8), m_oval(cx + .36 * s, cy - .02 * s, .10 * s, .145 * s, -28)]
    return union(pad, *toes)


def paw(ic, cx, cy, s, fill=None, shade=True):
    m = paw_mask(cx, cy, s)
    if shade:
        ic.shade(m, .32, 0, s * .03, s * .025)
    ic.paint(m, fill or vgrad((255, 255, 255), (214, 226, 250), cy - s * .4, cy + s * .45))
    return m


def bolt_mask(cx, cy, s):
    return m_poly([(cx + .10 * s, cy - .5 * s), (cx - .34 * s, cy + .06 * s), (cx - .02 * s, cy + .06 * s), (cx - .14 * s, cy + .5 * s),
                   (cx + .34 * s, cy - .1 * s), (cx + .02 * s, cy - .1 * s)])


def tri_mask(cx, cy, r, rot=0):
    pts = [(cx + math.cos(math.radians(rot + a)) * r, cy + math.sin(math.radians(rot + a)) * r) for a in (0, 120, 240)]
    return m_poly(pts)


def play(ic, cx, cy, r, color=WHITE):
    m = tri_mask(cx, cy, r, 0)
    ic.shade(m, .3, 0, 2, 2)
    ic.paint(m, color)
    return m


def finish(ic):
    return ic.finish(True)


# ── desktop identity ──────────────────────────────────────────────────────────

def d_zhongbao():
    """摆渡众包: the search giant's indigo, label-box brackets round a paw, a done check."""
    ic = Icon(); tile(ic, INDIGO)
    ic.paint(m_rrect(40, 40, 216, 216, 34), vgrad((70, 84, 240), (26, 32, 150), 40, 216), .5)
    gold = (255, 214, 80)
    for (x, y, dx, dy) in ((50, 50, 1, 1), (206, 50, -1, 1), (50, 206, 1, -1), (206, 206, -1, -1)):
        b = union(m_line([(x, y + dy * 40), (x, y), (x + dx * 40, y)], 12, True))
        ic.shade(b, .3, 0, 2, 2); ic.paint(b, gold)
    paw(ic, 128, 124, 104)
    badge(ic, "check", 190, 192, 34)
    return ic


def d_bodu():
    """摆渡: a paw and a small magnifier on the blue disc."""
    ic = Icon(); disc(ic, BODU, 128, 128, 108)
    paw(ic, 112, 108, 112)
    ring = cut(m_circle(166, 166, 36), m_circle(166, 166, 22))
    handle = m_line([(190, 190), (222, 222)], 17)
    for part in (ring, handle):
        ic.paint(grow(part, 4), (16, 36, 120), .55)
    ic.paint(m_circle(166, 166, 22), (200, 225, 255), .35)
    ic.paint(union(ring, handle), vgrad((255, 255, 255), (206, 218, 246), 130, 222))
    return ic


def d_tieba():
    """贴吧: a bubble on a blue tile with a paw in it."""
    ic = Icon(); tile(ic, TIEBA)
    b = union(m_rrect(48, 56, 208, 168, 46), m_poly([(76, 160), (118, 160), (84, 208)]))
    ic.shade(b, .3, 0, 4, 4)
    ic.body(b, "paper", rim=False)
    paw(ic, 128, 110, 88, vgrad((70, 140, 245), (30, 80, 190), 70, 150), shade=False)
    return ic


def d_miaoyu():
    """喵鱼: a white cat face with a blue fish, on the yellow shop tile."""
    ic = Icon(); tile(ic, MIAOYU)
    cx, cy = 112, 120
    ears = union(m_poly([(cx - 66, cy - 8), (cx - 58, cy - 70), (cx - 14, cy - 40)]), m_poly([(cx + 66, cy - 8), (cx + 58, cy - 70), (cx + 14, cy - 40)]))
    head = union(ears, m_oval(cx, cy, 66, 54))
    ic.shade(head, .3, 0, 4, 4)
    ic.paint(head, vgrad((255, 255, 255), (222, 226, 234), cy - 70, cy + 54))
    ic.rim(head, (150, 120, 70), 2, .6)
    inner = union(m_poly([(cx - 54, cy - 22), (cx - 50, cy - 56), (cx - 24, cy - 38)]), m_poly([(cx + 54, cy - 22), (cx + 50, cy - 56), (cx + 24, cy - 38)]))
    ic.paint(inner, (250, 170, 180))
    for sx in (-1, 1):
        ic.paint(m_oval(cx + sx * 26, cy - 4, 9, 13), INK)
        ic.paint(m_circle(cx + sx * 26 + 3, cy - 9, 3.5), WHITE)
        for dy in (0, 12):
            ic.paint(m_line([(cx + sx * 44, cy + 14 + dy), (cx + sx * 80, cy + 8 + dy * 1.5)], 2.6), (150, 150, 160))
    ic.paint(m_poly([(cx - 8, cy + 12), (cx + 8, cy + 12), (cx, cy + 22)]), (240, 110, 130))
    ic.paint(union(m_arc(cx - 8, cy + 22, 9, 10, 150, 3.4), m_arc(cx + 8, cy + 22, 9, 30, 170, 3.4)), (120, 90, 80))
    # the fish
    fx, fy = 176, 184
    fish = union(m_oval(fx, fy, 38, 22), m_poly([(fx - 26, fy), (fx - 62, fy - 22), (fx - 62, fy + 22)]))
    ic.shade(fish, .35, 0, 3, 3)
    ic.body(fish, "sky")
    ic.paint(m_poly([(fx - 4, fy - 26), (fx + 18, fy - 22), (fx + 4, fy - 6)]), hue("blue")[1], .9)
    ic.paint(m_circle(fx + 22, fy - 8, 5.5), WHITE); ic.paint(m_circle(fx + 24, fy - 8, 2.6), INK)
    return ic


def d_kk_exe():
    """kk.exe, the program the player's AI lives in: the glowing-core network on the deep blue tile."""
    ic = Icon(); tile(ic, KK)
    ic.paint(m_rrect(38, 38, 218, 218, 36), vgrad((38, 125, 181), (16, 52, 101), 38, 218), .8)
    c = (128, 128)
    pts = [(68, 84), (74, 178), (186, 184), (190, 74)]
    for p in pts:
        ic.paint(m_line([p, c], 8), (142, 231, 248), .9)
    ic.glow(m_circle(*c, 44), (255, 214, 90), 20, .8)
    for p in pts:
        ic.body(m_circle(p[0], p[1], 15), "gray")
    core = m_circle(*c, 31)
    ic.body(core, "gold")
    ic.paint(m_circle(*c, 14), (255, 253, 219))
    return ic


def d_xunbao():
    """淘货: a smiling shopping bag on the orange shop tile."""
    ic = Icon(); tile(ic, TAOHUO)
    handle = m_arc(128, 98, 32, 180, 360, 11)
    ic.shade(handle, .3, 0, 3, 3); ic.paint(handle, (255, 246, 232))
    bag = m_poly([(66, 96), (190, 96), (200, 212), (56, 212)])
    bag = intersect(bag, m_rrect(54, 96, 202, 214, 14))
    ic.shade(bag, .35, 0, 5, 5)
    ic.body(bag, "paper", rim=True)
    ic.paint(m_circle(104, 124, 7), (220, 90, 20)); ic.paint(m_circle(152, 124, 7), (220, 90, 20))
    ic.paint(m_arc(128, 140, 36, 25, 155, 11), (230, 84, 16))
    return ic


def d_home():
    """家庭: a house with a power bolt on the green tile (household income and the electricity bill)."""
    ic = Icon(); tile(ic, HOME)
    wall = m_rrect(66, 116, 190, 206, 8)
    ic.shade(wall, .3, 0, 4, 4); ic.body(wall, "paper", rim=True)
    roof = m_poly([(40, 130), (128, 48), (216, 130)])
    ic.shade(roof, .3, 0, 4, 4); ic.body(roof, "red")
    ic.paint(grow(bolt_mask(128, 162, 70), 2.5), (170, 100, 0), .6)
    ic.body(bolt_mask(128, 162, 70), "yellow", rim=False, gloss=False)
    return ic


def d_recycle_bin():
    """回收站: a glassy bin with ribs and a green recycling triangle."""
    ic = Icon()
    body = m_poly([(66, 92), (190, 92), (176, 226), (80, 226)])
    body = intersect(body, m_rrect(60, 88, 196, 228, 14))
    ic.shade(body, .3, 0, 5, 5)
    ic.paint(body, vgrad((205, 222, 240), (110, 140, 176), 92, 226), .96)
    for k in range(-2, 3):
        x0 = 128 + k * 24
        ic.paint(m_line([(x0, 108), (128 + k * 21, 212)], 4.5), (60, 84, 118), .45)
    ic.rim(body, (60, 84, 118), 2.4, .7)
    ic.paint(m_rrect(104, 50, 152, 70, 8), vgrad((186, 196, 210), (110, 122, 142), 50, 70))
    lid = m_rrect(52, 64, 204, 94, 12)
    ic.shade(lid, .3, 0, 3, 3); ic.body(lid, "steel")
    # recycle mark: a triangle ring of three arrows
    green = vgrad((140, 235, 110), (40, 150, 50), 120, 200)
    ring = cut(tri_mask(128, 166, 44, -90), tri_mask(128, 166, 24, -90))
    ic.paint(grow(ring, 1.5), (16, 70, 24), .35)
    ic.paint(ring, green)
    return ic


def d_delivery_folder():
    """交付文件夹: the yellow folder with a blue 'out' arrow."""
    ic = Icon(); folder(ic, "yellow")
    badge(ic, "up", 196, 196, 42)
    return ic


def d_sophon_dll():
    """sophon.dll: a library file, a page with a cog."""
    ic = Icon(); page(ic, 50, 22, 206, 234, fold=42)
    g = m_gear(128, 126, 52, teeth=8, depth=.24, hole=.36)
    ic.shade(g, .3, 0, 3, 3); ic.body(g, "steel")
    ic.paint(m_rrect(70, 192, 186, 216, 8), vgrad((120, 135, 160), (70, 84, 108), 192, 216))
    ic.text("DLL", "black", 24, 128, 204, WHITE)
    return ic


def d_origin_mp4():
    """我从哪儿来.mp4: a dark film frame, sprocket holes, a play triangle on the lime frame."""
    ic = Icon()
    strip = m_rrect(24, 52, 232, 204, 20)
    ic.shade(strip, .35, 0, 5, 5); ic.body(strip, "dark")
    for i in range(7):
        x = 44 + i * 28
        ic.paint(m_rrect(x - 8, 62, x + 8, 78, 4), (226, 230, 238)); ic.paint(m_rrect(x - 8, 178, x + 8, 194, 4), (226, 230, 238))
    screen = m_rrect(40, 88, 216, 168, 10)
    ic.paint(screen, vgrad((222, 245, 120), (90, 170, 60), 88, 168))
    ic.rim(screen, (30, 80, 20), 2, .6)
    ic.gloss(screen, .3, .5)
    play(ic, 132, 128, 30)
    return ic


def d_note_txt():
    """笔记.txt: a yellow memo pad with a spiral binding and a pencil."""
    ic = Icon()
    pad = m_rrect(40, 36, 208, 228, 16)
    ic.shade(pad, .3, 0, 5, 5); ic.body(pad, "yellow", gloss=False)
    sheet_m = m_rrect(52, 66, 196, 218, 8)
    ic.paint(sheet_m, vgrad((255, 255, 250), (240, 236, 214), 66, 218))
    for k in range(5):
        ic.paint(m_line([(66, 104 + k * 24), (182 - (24 if k == 4 else 0), 104 + k * 24)], 5), (140, 160, 200), .85)
    for k in range(6):
        x = 64 + k * 24
        ic.paint(m_rrect(x - 5, 28, x + 5, 58, 5), vgrad((120, 130, 150), (40, 46, 60), 28, 58))
    # pencil
    d = (-0.60, 0.80); n = (0.80, 0.60)
    p0 = (206, 100); p1 = (140, 188)
    body = m_line([p0, p1], 24, False)
    ic.shade(body, .35, 0, 3, 3); ic.body(body, "red", gloss=False)
    tipc = (p1[0] + d[0] * 24, p1[1] + d[1] * 24)
    tip = m_poly([(p1[0] + n[0] * 12, p1[1] + n[1] * 12), (p1[0] - n[0] * 12, p1[1] - n[1] * 12), (tipc[0] + d[0] * 18, tipc[1] + d[1] * 18)])
    ic.paint(tip, (240, 206, 150))
    ic.paint(m_poly([(tipc[0] + d[0] * 6 + n[0] * 3, tipc[1] + d[1] * 6 + n[1] * 3), (tipc[0] + d[0] * 6 - n[0] * 3, tipc[1] + d[1] * 6 - n[1] * 3),
                     (tipc[0] + d[0] * 18, tipc[1] + d[1] * 18)]), (50, 50, 60))
    return ic


def d_chronicle_txt():
    """灵光记.txt: a chapter-novel book, a gold spark on the cover and a ribbon."""
    ic = Icon(); book(ic, "brown", 44, 28, 214, 228)
    spark = m_star(120, 108, 40, 12, 4, -90)
    ic.glow(spark, (255, 220, 110), 8, .6)
    ic.paint(spark, vgrad((255, 244, 170), (236, 176, 50), 68, 148))
    ic.paint(m_line([(78, 164), (162, 164)], 5), (255, 226, 150), .9)
    ic.paint(m_line([(90, 180), (150, 180)], 5), (255, 226, 150), .7)
    ic.paint(m_poly([(166, 28), (186, 28), (186, 236), (176, 224), (166, 236)]), (200, 40, 50))
    ic.rim(m_poly([(166, 28), (186, 28), (186, 236), (176, 224), (166, 236)]), (110, 14, 24), 1.5, .5)
    return ic


# ── tray apps ───────────────────────────────────────────────────────────────────

def t_kugou():
    ic = Icon(); disc(ic, ((90, 170, 255), (28, 124, 232), (10, 60, 150)))
    band = m_arc(128, 122, 60, 188, 352, 18)
    ic.shade(band, .3, 0, 3, 3); ic.paint(band, WHITE)
    for x in (66, 190):
        cup = m_rrect(x - 20, 112, x + 20, 180, 14)
        ic.shade(cup, .3, 0, 3, 3); ic.paint(cup, vgrad((255, 255, 255), (206, 222, 246), 112, 180))
    note = union(m_circle(116, 168, 20), m_rect(130, 80, 142, 168), m_poly([(140, 80), (172, 96), (172, 118), (140, 104)]))
    ic.shade(note, .3, 0, 3, 3); ic.paint(note, vgrad((255, 255, 255), (206, 222, 246), 80, 190))
    return ic


def t_netease():
    ic = Icon(); disc(ic, RED)
    ring = m_arc(128, 128, 62, 30, 330, 20)
    ic.shade(ring, .3, 0, 3, 3); ic.paint(ring, WHITE)
    ic.paint(m_poly([(190, 94), (212, 128), (176, 132)]), WHITE)
    dot = m_circle(128, 128, 24)
    ic.shade(dot, .3, 0, 2, 2); ic.paint(dot, WHITE)
    ic.paint(m_circle(128, 128, 10), RED[1])
    return ic


def t_baofeng():
    ic = Icon(); tile(ic, NAVY)
    play(ic, 112, 130, 62, (240, 246, 255))
    b = bolt_mask(172, 118, 120)
    ic.glow(b, (255, 200, 60), 10, .7)
    ic.paint(grow(b, 3), (20, 28, 60), .7)
    ic.body(b, "yellow", rim=False)
    return ic


def t_tgp():
    ic = Icon(); tile(ic, ((92, 96, 112), (36, 38, 48), (10, 10, 16)))
    pad = union(m_rrect(36, 92, 220, 168, 38), m_circle(78, 168, 32), m_circle(178, 168, 32))
    ic.shade(pad, .4, 0, 4, 4); ic.body(pad, "orange")
    ic.paint(union(m_rect(66, 118, 100, 132), m_rect(77, 107, 89, 143)), (50, 30, 10))
    for (x, y) in ((164, 112), (190, 136)):
        ic.body(m_circle(x, y, 11), "dark")
    return ic


def t_youdao():
    ic = Icon(); tile(ic, RED)
    left = m_poly([(128, 80), (60, 70), (60, 176), (128, 188)])
    right = m_poly([(128, 80), (196, 70), (196, 176), (128, 188)])
    ic.shade(union(left, right), .35, 0, 4, 4)
    ic.paint(left, vgrad((255, 255, 255), (222, 226, 236), 70, 188)); ic.paint(right, vgrad((250, 250, 252), (206, 212, 226), 70, 188))
    ic.paint(m_line([(128, 82), (128, 188)], 4), (150, 156, 170))
    for k in range(3):
        y = 100 + k * 24
        ic.paint(m_line([(76, y), (116, y + 4)], 6), (210, 60, 60), .9)
        ic.paint(m_line([(140, y + 4), (180, y)], 6), (140, 150, 170), .8)
    return ic


# ── popup badges: a white disc with a colour emblem (the old look), 128 px ─────────

def popup_base():
    ic = Icon(); disc(ic, "paper", 128, 128, 112)
    return ic


def emblem_from(draw, scale=.74):
    sub = draw()
    ic = popup_base()
    ic.overlay(sub, 128 - 128 * scale, 128 - 128 * scale, scale)
    return ic


def p_yy():
    ic = popup_base()
    b = union(m_rrect(48, 62, 208, 168, 44), m_poly([(70, 160), (112, 160), (84, 206)]))
    ic.shade(b, .3, 0, 3, 3); ic.body(b, "orange")
    ic.text("YY", "black", 66, 128, 114, WHITE)
    return ic


def p_qh360():
    ic = popup_base()
    sh = m_poly([(128, 40), (200, 66), (196, 140), (128, 216), (60, 140), (56, 66)])
    ic.shade(sh, .3, 0, 3, 3); ic.body(sh, "green")
    ic.paint(m_line([(94, 126), (120, 152), (166, 94)], 18), WHITE)
    return ic


def p_xunlei():
    ic = popup_base()
    d = m_circle(128, 128, 82)
    ic.body(d, "blue")
    arrow = m_poly([(128, 196), (82, 140), (112, 140), (112, 66), (144, 66), (144, 140), (174, 140)])
    ic.shade(arrow, .3, 0, 3, 3); ic.paint(arrow, WHITE)
    b = bolt_mask(170, 88, 66)
    ic.paint(grow(b, 3), (20, 70, 160), .8); ic.body(b, "yellow", rim=False, gloss=False)
    return ic


def p_windows():
    """Generic system-update badge (file name kept as 'windows'): a blue shield with a cog and a circular arrow."""
    ic = popup_base()
    sh = m_poly([(128, 36), (204, 64), (200, 144), (128, 220), (56, 144), (52, 64)])
    ic.shade(sh, .3, 0, 3, 3); ic.body(sh, "blue")
    arc = m_arc(128, 124, 46, 40, 320, 15)
    ic.paint(arc, WHITE)
    a = math.radians(40)
    tip = (128 + math.cos(a) * 46, 124 + math.sin(a) * 46)
    ic.paint(m_poly([(tip[0] + 24, tip[1] - 4), (tip[0] - 16, tip[1] - 22), (tip[0] - 14, tip[1] + 22)]), WHITE)
    ic.paint(m_gear(128, 124, 24, teeth=6, depth=.26, hole=.4), WHITE)
    return ic


def p_bodu_cloud():
    ic = popup_base()
    cloud = union(m_circle(100, 134, 34), m_circle(144, 112, 44), m_circle(180, 142, 30), m_rrect(76, 128, 198, 172, 22))
    ic.shade(cloud, .3, 0, 3, 3); ic.body(cloud, "sky")
    up = m_poly([(128, 76), (160, 112), (140, 112), (140, 154), (116, 154), (116, 112), (96, 112)])
    ic.paint(up, WHITE)
    return ic


def p_ludashi():
    ic = popup_base()
    g = m_circle(128, 132, 84)
    ic.body(g, "orange")
    ic.paint(m_circle(128, 132, 64), (60, 28, 6), .55)
    ic.paint(m_arc(128, 132, 54, 150, 390, 12), (255, 240, 200))
    ic.paint(m_line([(128, 132), (170, 94)], 11), WHITE)
    ic.paint(m_circle(128, 132, 12), WHITE)
    return ic


def p_info():
    ic = popup_base()
    d = m_circle(128, 128, 82)
    ic.body(d, "steel")
    ic.paint(m_circle(128, 78, 11), WHITE)
    ic.paint(m_rrect(116, 100, 140, 184, 8), WHITE)
    return ic


# ── the job table ───────────────────────────────────────────────────────────────

def outs(group, name, size=128):
    return [(f"{ICONS}/{group}/{name}.png", size)]


JOBS = {
    # Calculator.png / Commander.png stay Oxygen: the real calculator and terminal exist before 摆渡众包 / 喵鱼 unlock; the
    # rebranded windows get these sprites from code (ShopView.SetOxygenIcons).
    "d_zhongbao": (d_zhongbao, outs("desktop", "zhongbao", 256)),
    "d_miaoyu": (d_miaoyu, outs("desktop", "miaoyu", 256)),
    "d_tieba": (d_tieba, outs("desktop", "tieba", 256) + [(f"{OXY}/Reminder.png", 128)]),
    "d_bodu": (d_bodu, outs("desktop", "bodu", 256) + [(f"{OXY}/Widget Library.png", 128)]),
    "d_kk_exe": (d_kk_exe, outs("desktop", "kk_exe", 256)),
    "d_xunbao": (d_xunbao, outs("desktop", "xunbao", 256)),
    "d_home": (d_home, outs("desktop", "home", 256)),
    "d_recycle_bin": (d_recycle_bin, outs("desktop", "recycle_bin", 256)),
    "d_delivery_folder": (d_delivery_folder, outs("desktop", "delivery_folder", 256)),
    "d_sophon_dll": (d_sophon_dll, outs("desktop", "sophon_dll", 256)),
    "d_origin_mp4": (d_origin_mp4, outs("desktop", "origin_mp4", 256)),
    "d_note_txt": (d_note_txt, outs("desktop", "note_txt", 256)),
    "d_chronicle_txt": (d_chronicle_txt, outs("desktop", "chronicle_txt", 256)),
    "t_kugou": (t_kugou, outs("tray", "kugou")),
    "t_netease": (t_netease, outs("tray", "netease")),
    "t_baofeng": (t_baofeng, outs("tray", "baofeng")),
    "t_tgp": (t_tgp, outs("tray", "tgp")),
    "t_youdao": (t_youdao, outs("tray", "youdao")),
    "p_yy": (p_yy, outs("popup", "yy")),
    "p_tieba": (lambda: emblem_from(d_tieba), outs("popup", "tieba")),
    "p_qh360": (p_qh360, outs("popup", "qh360")),
    "p_xunlei": (p_xunlei, outs("popup", "xunlei")),
    "p_windows": (p_windows, outs("popup", "windows")),
    "p_bodu_crowd": (lambda: emblem_from(d_zhongbao), outs("popup", "bodu_crowd")),
    "p_bodu_cloud": (p_bodu_cloud, outs("popup", "bodu_cloud")),
    "p_taohuo": (lambda: emblem_from(d_xunbao), outs("popup", "taohuo")),
    "p_miaoyu": (lambda: emblem_from(d_miaoyu), outs("popup", "miaoyu")),
    "p_ludashi": (p_ludashi, outs("popup", "ludashi")),
    "p_info": (p_info, outs("popup", "info")),
}


def run(name):
    fn, targets = JOBS[name]
    ic = fn()
    im = ic.finish(True)
    for path, size in targets:
        os.makedirs(os.path.dirname(path), exist_ok=True)
        im.resize((size, size), Image.LANCZOS).save(path, optimize=True)
    return name


if __name__ == "__main__":
    names = [n for n in sys.argv[1:] if n in JOBS] or list(JOBS)
    with Pool(min(8, len(names))) as pool:
        for n in pool.imap_unordered(run, names):
            print("ok", n, flush=True)
    for g in ("desktop", "tray", "popup"):
        d = f"{ICONS}/{g}"
        if os.path.isdir(d):
            sheet(d, f"/private/tmp/claude-501/-Users-kk-unity/0b11848a-3722-4bc3-8ee4-9222e5441068/scratchpad/sheet_{g}.png", cell=128, cols=8)
            sheet(d, f"/private/tmp/claude-501/-Users-kk-unity/0b11848a-3722-4bc3-8ee4-9222e5441068/scratchpad/sheet_{g}_small.png", cell=40, cols=16)
