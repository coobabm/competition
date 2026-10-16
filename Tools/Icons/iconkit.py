"""Shared drawing kit for 灵光's icons: Oxygen-like glossy art (soft gradients, a top gloss, a soft drop shadow,
no hard black outline), drawn 4x supersampled and saved as transparent PNGs.

Every group script does:   from iconkit import *;  ic = Icon();  ...draw...;  ic.save(path, 128)
Coordinates are in a 256-unit design space regardless of output size (u(x) scales to the supersampled canvas).
"""
import math, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops

S = 4
D = 256                # design units
N = D * S              # canvas pixels

FONTS = {
    "black": "/System/Library/Fonts/Supplemental/Arial Black.ttf",
    "bold": "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
    "serif": "/System/Library/Fonts/Supplemental/Georgia Bold.ttf",
    "mono": "/System/Library/Fonts/Menlo.ttc",
    "cjk": "/System/Library/Fonts/STHeiti Medium.ttc",
}

# A shared palette: (light, base, dark) per hue so every group reads as one set.
PAL = {
    "blue":   ((120, 190, 255), (40, 120, 220), (16, 60, 140)),
    "sky":    ((170, 225, 255), (80, 170, 235), (30, 100, 170)),
    "teal":   ((120, 230, 215), (20, 160, 150), (8, 90, 90)),
    "green":  ((160, 235, 120), (60, 170, 60), (24, 100, 30)),
    "lime":   ((220, 250, 120), (150, 205, 40), (80, 120, 10)),
    "yellow": ((255, 240, 140), (250, 200, 40), (180, 120, 10)),
    "orange": ((255, 200, 120), (245, 140, 30), (170, 70, 10)),
    "red":    ((255, 140, 130), (220, 50, 50), (130, 15, 25)),
    "pink":   ((255, 175, 210), (235, 90, 150), (150, 30, 80)),
    "purple": ((210, 170, 255), (140, 80, 210), (70, 30, 130)),
    "brown":  ((225, 175, 120), (160, 105, 55), (90, 55, 25)),
    "gray":   ((240, 242, 246), (170, 176, 186), (90, 96, 108)),
    "steel":  ((200, 210, 225), (110, 125, 145), (50, 60, 75)),
    "dark":   ((110, 115, 130), (55, 60, 72), (22, 24, 30)),
    "gold":   ((255, 235, 150), (235, 185, 70), (150, 100, 25)),
    "paper":  ((255, 255, 255), (244, 244, 238), (205, 205, 195)),
}
WHITE = (255, 255, 255)
INK = (40, 44, 54)


def u(v):
    return int(round(v * S))


def rgba(c, a=255):
    return tuple(c[:3]) + (a,)


def font(kind, size):
    path = FONTS.get(kind, kind)
    return ImageFont.truetype(path, max(1, u(size)))


# ── masks (L images, 255 inside) ────────────────────────────────────────────────

def blank():
    return Image.new("L", (N, N), 0)


def m_rrect(x0, y0, x1, y1, r):
    m = blank(); ImageDraw.Draw(m).rounded_rectangle((u(x0), u(y0), u(x1), u(y1)), u(r), fill=255); return m


def m_rect(x0, y0, x1, y1):
    m = blank(); ImageDraw.Draw(m).rectangle((u(x0), u(y0), u(x1), u(y1)), fill=255); return m


def m_ellipse(x0, y0, x1, y1):
    m = blank(); ImageDraw.Draw(m).ellipse((u(x0), u(y0), u(x1), u(y1)), fill=255); return m


def m_circle(cx, cy, r):
    return m_ellipse(cx - r, cy - r, cx + r, cy + r)


def m_poly(pts):
    m = blank(); ImageDraw.Draw(m).polygon([(u(x), u(y)) for x, y in pts], fill=255); return m


def m_line(pts, width, round_caps=True):
    m = blank(); d = ImageDraw.Draw(m)
    p = [(u(x), u(y)) for x, y in pts]
    d.line(p, fill=255, width=u(width), joint="curve")
    if round_caps:
        r = u(width) / 2
        for x, y in (p[0], p[-1]):
            d.ellipse((x - r, y - r, x + r, y + r), fill=255)
    return m


def m_arc(cx, cy, r, a0, a1, width):
    m = blank(); ImageDraw.Draw(m).arc((u(cx - r), u(cy - r), u(cx + r), u(cy + r)), a0, a1, fill=255, width=u(width)); return m


def m_pie(cx, cy, r, a0, a1):
    m = blank(); ImageDraw.Draw(m).pieslice((u(cx - r), u(cy - r), u(cx + r), u(cy + r)), a0, a1, fill=255); return m


def m_text(text, kind, size, cx, cy, stroke=0):
    m = blank(); d = ImageDraw.Draw(m); f = font(kind, size)
    l, t, r, b = d.textbbox((0, 0), text, font=f, stroke_width=u(stroke))
    d.text((u(cx) - (l + r) / 2, u(cy) - (t + b) / 2), text, font=f, fill=255, stroke_width=u(stroke), stroke_fill=255)
    return m


def m_star(cx, cy, r_out, r_in=None, points=5, rot=-90):
    r_in = r_in if r_in is not None else r_out * .45
    pts = []
    for k in range(points * 2):
        r = r_out if k % 2 == 0 else r_in
        a = math.radians(rot + k * 180 / points)
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return m_poly(pts)


def m_heart(cx, cy, s):
    r = s * .28
    m = m_ellipse(cx - s * .5, cy - s * .42, cx - s * .5 + 2 * r, cy - s * .42 + 2 * r)
    m = union(m, m_ellipse(cx + s * .5 - 2 * r, cy - s * .42, cx + s * .5, cy - s * .42 + 2 * r))
    return union(m, m_poly([(cx - s * .49, cy - s * .2), (cx + s * .49, cy - s * .2), (cx, cy + s * .48)]))


def m_gear(cx, cy, r, teeth=8, depth=.22, hole=.38):
    pts = []
    steps = teeth * 4
    for k in range(steps):
        a = 2 * math.pi * k / steps
        rr = r if (k % 4) in (0, 1) else r * (1 - depth)
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
    return cut(m_poly(pts), m_circle(cx, cy, r * hole))


def union(*ms):
    out = ms[0]
    for m in ms[1:]:
        out = ImageChops.lighter(out, m)
    return out


def cut(a, b):
    return ImageChops.subtract(a, b)


def intersect(a, b):
    return ImageChops.multiply(a, b)


def grow(m, px):
    """Dilate (px > 0) or erode (px < 0) a mask by about px design units."""
    k = max(3, u(abs(px)) * 2 + 1) | 1
    return m.filter(ImageFilter.MaxFilter(k) if px > 0 else ImageFilter.MinFilter(k))


def rotate_mask(m, deg, cx=128, cy=128):
    return m.rotate(deg, resample=Image.BICUBIC, center=(u(cx), u(cy)))


# ── fills ─────────────────────────────────────────────────────────────────────

def lin(c0, c1, x0=128, y0=0, x1=128, y1=256):
    """Linear gradient image from c0 at (x0,y0) to c1 at (x1,y1), clamped beyond the ends."""
    import numpy as np
    yy, xx = np.mgrid[0:N, 0:N] / S
    dx, dy = x1 - x0, y1 - y0
    t = np.clip(((xx - x0) * dx + (yy - y0) * dy) / (dx * dx + dy * dy or 1), 0, 1)[..., None]
    a, b = np.array(rgba(c0), float), np.array(rgba(c1), float)
    return Image.fromarray((a + (b - a) * t).astype("uint8"), "RGBA")


def vgrad(c0, c1, y0=0, y1=256):
    """Vertical gradient (exact, fast)."""
    import numpy as np
    y = np.arange(N) / S
    t = np.clip((y - y0) / max(1e-6, y1 - y0), 0, 1)[:, None]
    a, b = np.array(rgba(c0), float), np.array(rgba(c1), float)
    col = (a + (b - a) * t).astype("uint8")[:, None, :]
    return Image.fromarray(np.repeat(col, N, axis=1), "RGBA")


def rgrad(c_in, c_out, cx=128, cy=110, r=150):
    g = Image.radial_gradient("L").resize((u(2 * r), u(2 * r)))
    w = Image.new("L", (N, N), 255); w.paste(g, (u(cx - r), u(cy - r)))
    a = Image.new("RGBA", (N, N), rgba(c_in)); b = Image.new("RGBA", (N, N), rgba(c_out))
    return Image.composite(b, a, w)


def solid(c, a=255):
    return Image.new("RGBA", (N, N), rgba(c, a))


def hue(name):
    """(light, base, dark) for a palette name, or the tuple itself."""
    return PAL[name] if isinstance(name, str) else name


# ── the icon canvas ───────────────────────────────────────────────────────────

class Icon:
    def __init__(self):
        self.im = Image.new("RGBA", (N, N), (0, 0, 0, 0))

    def paint(self, mask, fill, alpha=1.0):
        """Fill a mask with a colour (tuple) or an RGBA image (gradient)."""
        src = fill if isinstance(fill, Image.Image) else solid(fill)
        m = mask if alpha >= 1 else mask.point(lambda v: int(v * alpha))
        layer = Image.new("RGBA", (N, N), (0, 0, 0, 0))
        layer.paste(src, (0, 0), m)
        self.im.alpha_composite(layer)
        return self

    def body(self, mask, color, light_top=True, rim=True, gloss=True, top=None, bottom=None):
        """The standard glossy solid: vertical gradient light→dark, a darker rim, and a top gloss."""
        lo, base, dk = hue(color)
        bbox = mask.getbbox() or (0, 0, N, N)
        y0, y1 = bbox[1] / S, bbox[3] / S
        self.paint(mask, vgrad(top or lo, bottom or dk, y0, y1) if light_top else vgrad(dk, lo, y0, y1))
        if rim:
            self.rim(mask, dk)
        if gloss:
            self.gloss(mask)
        return self

    def rim(self, mask, color, width=2.2, alpha=.55):
        ring = cut(mask, grow(mask, -width))
        return self.paint(ring, color, alpha)

    def gloss(self, mask, strength=.55, height=.48):
        """White highlight over the top part of the shape, fading downward (the Oxygen sheen)."""
        bbox = mask.getbbox()
        if not bbox:
            return self
        x0, y0, x1, y1 = [v / S for v in bbox]
        h = (y1 - y0) * height
        inner = grow(mask, -3)
        cap = m_ellipse(x0 - (x1 - x0) * .15, y0 - h * .9, x1 + (x1 - x0) * .15, y0 + h)
        g = intersect(inner, cap)
        import numpy as np
        t = np.clip((np.arange(N) / S - y0) / max(1, h), 0, None)
        col = (255 * strength * np.clip(1 - t, 0, 1) ** 1.4).astype("uint8")[:, None]
        col = Image.fromarray(np.repeat(col, N, axis=1), "L")
        alpha = intersect(g, col)
        layer = Image.new("RGBA", (N, N), (255, 255, 255, 0)); layer.putalpha(alpha)
        self.im.alpha_composite(layer)
        return self

    def shade(self, mask, alpha=.25, dx=0, dy=3, blur=3):
        """A soft inner/contact shadow under a part (call before painting the part)."""
        m = ImageChops.offset(mask, u(dx), u(dy)).filter(ImageFilter.GaussianBlur(u(blur)))
        return self.paint(m, (0, 0, 0), alpha)

    def glow(self, mask, color, radius=10, alpha=.8):
        m = mask.filter(ImageFilter.GaussianBlur(u(radius)))
        return self.paint(m, color, alpha)

    def text(self, text, kind, size, cx, cy, color, stroke=0):
        return self.paint(m_text(text, kind, size, cx, cy, stroke), color)

    def overlay(self, other, x=0, y=0, scale=1.0):
        """Composite another Icon (e.g. a small badge) at design offset (x, y) and scale."""
        im = other.im if isinstance(other, Icon) else other
        if scale != 1.0:
            im = im.resize((int(N * scale), int(N * scale)), Image.LANCZOS)
        self.im.alpha_composite(im, (u(x), u(y)))
        return self

    def finish(self, shadow=True):
        im = self.im
        if shadow:
            a = im.split()[3].point(lambda v: v * 120 // 255)
            sh = Image.new("RGBA", im.size, (0, 0, 0, 0)); sh.putalpha(a)
            sh = sh.filter(ImageFilter.GaussianBlur(u(5)))
            base = Image.new("RGBA", im.size, (0, 0, 0, 0)); base.paste(sh, (0, u(5)), sh)
            base.alpha_composite(im)
            im = base
        return im

    def save(self, path, size=256, shadow=True):
        os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
        im = self.finish(shadow)
        im = im.resize((size, size), Image.LANCZOS)
        im.save(path, optimize=True)
        return path


# ── reusable parts (each paints onto an Icon) ─────────────────────────────────

def tile(ic, color, x0=20, y0=20, x1=236, y1=236, r=44):
    """The app-tile plate many icons sit on."""
    m = m_rrect(x0, y0, x1, y1, r)
    ic.body(m, color)
    return m


def disc(ic, color, cx=128, cy=128, r=104):
    m = m_circle(cx, cy, r)
    ic.body(m, color)
    return m


def page(ic, x0=52, y0=24, x1=204, y1=232, fold=40, lines=0, line_color=(150, 160, 180)):
    """A white document with a folded top-right corner."""
    body = m_poly([(x0, y0), (x1 - fold, y0), (x1, y0 + fold), (x1, y1), (x0, y1)])
    ic.shade(body, .2)
    ic.paint(body, vgrad((255, 255, 255), (232, 234, 238), y0, y1))
    ic.rim(body, (150, 155, 165), 1.6, .6)
    flap = m_poly([(x1 - fold, y0), (x1 - fold, y0 + fold), (x1, y0 + fold)])
    ic.paint(flap, vgrad((215, 218, 225), (250, 250, 252), y0, y0 + fold))
    for k in range(lines):
        y = y0 + fold + 18 + k * 22
        if y > y1 - 20:
            break
        ic.paint(m_line([(x0 + 22, y), (x1 - 22 - (30 if k % 3 == 2 else 0), y)], 5), line_color)
    return body


def folder(ic, color="yellow", x0=20, y0=48, x1=236, y1=220):
    lo, base, dk = hue(color)
    back = union(m_rrect(x0, y0 + 14, x1, y1, 16), m_rrect(x0, y0, x0 + 90, y0 + 40, 12))
    ic.shade(back, .25)
    ic.paint(back, vgrad(base, dk, y0, y1))
    ic.paint(m_rect(x0 + 10, y0 + 30, x1 - 10, y0 + 70), (250, 250, 248))
    front = m_poly([(x0, y0 + 50), (x1, y0 + 50), (x1 - 4, y1), (x0 + 4, y1)])
    front = intersect(front, m_rrect(x0, y0 + 50, x1, y1, 16))
    ic.body(front, color)
    return front


def bubble(ic, color="sky", x0=24, y0=36, x1=232, y1=190, tail_left=True):
    m = m_rrect(x0, y0, x1, y1, 56)
    tx = x0 + 52 if tail_left else x1 - 52
    m = union(m, m_poly([(tx - 18, y1 - 20), (tx + 22, y1 - 10), (tx - 30 if tail_left else tx + 30, y1 + 34)]))
    ic.body(m, color)
    return m


def coin(ic, cx=128, cy=128, r=100, color="gold", symbol="¥", sym_kind="black"):
    lo, base, dk = hue(color)
    outer = m_circle(cx, cy, r)
    ic.body(outer, color)
    inner = m_circle(cx, cy, r * .78)
    ic.paint(inner, vgrad(dk, lo, cy - r, cy + r), .7)
    ic.paint(m_text(symbol, sym_kind, r * 1.0, cx, cy + r * .02), dk)
    ic.paint(m_text(symbol, sym_kind, r * 1.0, cx, cy - r * .03), lo)
    ic.gloss(outer, .35)
    return outer


def badge(ic, kind, cx=196, cy=196, r=40):
    """Small corner badge: 'plus', 'check', 'cross', 'warn', 'lock', 'star', 'bolt', 'up'."""
    colors = {"plus": "green", "check": "green", "cross": "red", "warn": "yellow", "lock": "steel",
              "star": "gold", "bolt": "orange", "up": "blue"}
    m = m_circle(cx, cy, r)
    ic.paint(grow(m, 4), WHITE)
    ic.body(m, colors.get(kind, "blue"))
    s = r * .55
    if kind == "plus":
        g = union(m_rect(cx - s, cy - s * .22, cx + s, cy + s * .22), m_rect(cx - s * .22, cy - s, cx + s * .22, cy + s))
    elif kind == "check":
        g = m_line([(cx - s * .8, cy), (cx - s * .2, cy + s * .6), (cx + s * .85, cy - s * .6)], r * .26)
    elif kind == "cross":
        g = union(m_line([(cx - s * .7, cy - s * .7), (cx + s * .7, cy + s * .7)], r * .26),
                  m_line([(cx - s * .7, cy + s * .7), (cx + s * .7, cy - s * .7)], r * .26))
    elif kind == "warn":
        g = m_text("!", "black", r * 1.3, cx, cy)
    elif kind == "star":
        g = m_star(cx, cy + 2, s * 1.1)
    elif kind == "bolt":
        g = m_poly([(cx + s * .2, cy - s), (cx - s * .6, cy + s * .15), (cx, cy + s * .15), (cx - s * .2, cy + s),
                    (cx + s * .6, cy - s * .15), (cx, cy - s * .15)])
    elif kind == "up":
        g = m_poly([(cx, cy - s), (cx + s * .85, cy + s * .05), (cx + s * .3, cy + s * .05), (cx + s * .3, cy + s),
                    (cx - s * .3, cy + s), (cx - s * .3, cy + s * .05), (cx - s * .85, cy + s * .05)])
    else:  # lock
        g = union(m_rrect(cx - s * .7, cy - s * .1, cx + s * .7, cy + s * .8, 4), m_arc(cx, cy - s * .15, s * .45, 180, 360, r * .16))
    ic.paint(g, WHITE)
    return m


def neuron_net(ic, layers=(3, 4, 2), x0=40, x1=216, y0=56, y1=200, node_r=13, color="blue", edge=(120, 150, 190)):
    """A little layered network diagram: nodes and edges."""
    xs = [x0 + (x1 - x0) * i / max(1, len(layers) - 1) for i in range(len(layers))]
    pos = []
    for x, n in zip(xs, layers):
        pos.append([(x, y0 + (y1 - y0) * (j + .5) / n) for j in range(n)])
    em = blank()
    for a, b in zip(pos, pos[1:]):
        for p in a:
            for q in b:
                em = union(em, m_line([p, q], 3.2))
    ic.paint(em, edge, .8)
    for layer in pos:
        for (x, y) in layer:
            ic.body(m_circle(x, y, node_r), color)
    return pos


def bars(ic, values, x0=44, x1=212, y_base=208, h=150, colors=("blue", "green", "orange", "red")):
    n = len(values); w = (x1 - x0) / n
    for i, v in enumerate(values):
        m = m_rrect(x0 + i * w + w * .14, y_base - h * v, x0 + (i + 1) * w - w * .14, y_base, 6)
        ic.body(m, colors[i % len(colors)])


def magnifier(ic, cx=110, cy=110, r=60, color="steel"):
    ring = cut(m_circle(cx, cy, r), m_circle(cx, cy, r * .76))
    lens = m_circle(cx, cy, r * .76)
    ic.paint(lens, rgrad((230, 245, 255), (150, 200, 240), cx - r * .3, cy - r * .3, r), .75)
    handle = m_line([(cx + r * .72, cy + r * .72), (cx + r * 1.65, cy + r * 1.65)], r * .36)
    ic.body(handle, "dark")
    ic.body(ring, color)
    ic.gloss(lens, .5)


def book(ic, color="red", x0=46, y0=30, x1=210, y1=226, title=None):
    lo, base, dk = hue(color)
    pages = m_rrect(x0 + 12, y0 + 10, x1, y1 - 2, 8)
    ic.paint(pages, vgrad((255, 255, 250), (225, 222, 210), y0, y1))
    cover = m_rrect(x0, y0, x1 - 14, y1 - 12, 10)
    ic.body(cover, color)
    ic.paint(m_rect(x0 + 10, y0, x0 + 20, y1 - 12), dk, .6)
    if title:
        ic.text(title, "cjk", 46, (x0 + x1 - 14) / 2 + 6, (y0 + y1 - 12) / 2, lo)
    return cover


def bulb(ic, cx=128, cy=108, r=62, color="yellow"):
    glass = union(m_circle(cx, cy, r), m_poly([(cx - r * .55, cy + r * .6), (cx + r * .55, cy + r * .6), (cx + r * .4, cy + r * 1.25), (cx - r * .4, cy + r * 1.25)]))
    ic.glow(glass, hue(color)[0], 14, .7)
    ic.body(glass, color)
    base = m_rrect(cx - r * .42, cy + r * 1.22, cx + r * .42, cy + r * 1.7, 6)
    ic.body(base, "steel")
    for k in range(2):
        y = cy + r * (1.36 + k * .16)
        ic.paint(m_line([(cx - r * .4, y), (cx + r * .4, y)], 3), hue("steel")[2], .7)


def sheet(folder_path, out_path, cell=96, cols=12, bg=(196, 206, 222)):
    """Contact sheet of every PNG in a folder (for review), each label underneath."""
    files = sorted(f for f in os.listdir(folder_path) if f.endswith(".png"))
    rows = max(1, math.ceil(len(files) / cols))
    W, H = cols * cell, rows * (cell + 16)
    im = Image.new("RGBA", (W, H), bg + (255,)); d = ImageDraw.Draw(im)
    f = ImageFont.truetype(FONTS["bold"], 10)
    for i, name in enumerate(files):
        x, y = (i % cols) * cell, (i // cols) * (cell + 16)
        ic = Image.open(os.path.join(folder_path, name)).convert("RGBA").resize((cell - 16, cell - 16), Image.LANCZOS)
        im.alpha_composite(ic, (x + 8, y + 4))
        d.text((x + 4, y + cell - 10), name[:-4][:16], font=f, fill=(30, 30, 40))
    im.save(out_path)
    return out_path
