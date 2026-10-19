"""Shared helpers for the DreamOS placeholder art generator (numpy float images, PIL shape masks, stroke font)."""
import math
from functools import lru_cache
import numpy as np
from PIL import Image, ImageDraw, ImageFilter


def hexc(v):
    return np.array([(v >> 16) & 255, (v >> 8) & 255, v & 255], dtype=np.float32) / 255


def mix(a, b, t):
    return a * (1 - t) + b * t


def smooth(t):
    t = np.clip(t, 0, 1)
    return t * t * (3 - 2 * t)


@lru_cache(maxsize=3)
def coords(w, h):
    u = (np.arange(w, dtype=np.float32) + .5) / w
    v = (np.arange(h, dtype=np.float32) + .5) / h
    return np.meshgrid(u, v)  # u, v: (h, w), v = 0 at the top


def vgrad(w, h, stops):
    """Vertical gradient. stops = [(pos 0..1, 0xRRGGBB), ...] -> float (h, w, 3)."""
    v = (np.arange(h, dtype=np.float32) + .5) / h
    pos = [p for p, _ in stops]
    cols = np.array([hexc(c) for _, c in stops])
    col = np.stack([np.interp(v, pos, cols[:, i]) for i in range(3)], -1)
    return np.repeat(col[:, None, :], w, 1).astype(np.float32)


def dgrad(w, h, c1, c2, angle=45.0):
    """Linear gradient c1 -> c2 along a direction (degrees, 0 = left->right, 90 = top->bottom)."""
    u, v = coords(w, h)
    a = math.radians(angle)
    t = ((u - .5) * w * math.cos(a) + (v - .5) * h * math.sin(a))
    ext = abs(w * math.cos(a)) + abs(h * math.sin(a))
    t = np.clip(t / ext + .5, 0, 1)[..., None]
    return (hexc(c1) * (1 - t) + hexc(c2) * t).astype(np.float32)


def glow(a, cx, cy, rx, ry, color, k=1.0, power=2.0):
    """Additive gaussian-ish glow. cx, cy, rx, ry are in 0..1 image units."""
    h, w = a.shape[:2]
    u, v = coords(w, h)
    g = np.exp(-(((u - cx) / rx) ** 2 + ((v - cy) / ry) ** 2) * power)
    a += (np.asarray(color, np.float32) if not isinstance(color, int) else hexc(color)) * (g * k)[..., None]


def blend(a, mask, color, alpha=1.0):
    c = hexc(color) if isinstance(color, int) else np.asarray(color, np.float32)
    m = (mask * alpha)[..., None]
    a += (c - a) * m


def to_img(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255 + .5).astype(np.uint8), "RGB")


def to_arr(img):
    return np.asarray(img.convert("RGB"), np.float32) / 255


def grain(a, seed=1, amt=1.2 / 255):
    h, w = a.shape[:2]
    a += (np.random.default_rng(seed).random((h, w, 1), dtype=np.float32) - .5) * 2 * amt


def vignette(a, strength=.25, power=2.0):
    h, w = a.shape[:2]
    u, v = coords(w, h)
    e = (np.abs(u - .5) * 2) ** power * .6 + (np.abs(v - .5) * 2) ** power * .6
    a *= (1 - np.clip(e, 0, 1) * strength)[..., None]


def blur(img, r):
    return img.filter(ImageFilter.GaussianBlur(r))


def blur_arr(a, r):
    return to_arr(blur(to_img(a), r))


def mask_from(w, h, fn, ss=2):
    """fn(draw, ss) draws white on black at ss x resolution; returns float mask (h, w)."""
    m = Image.new("L", (w * ss, h * ss), 0)
    fn(ImageDraw.Draw(m), ss)
    if ss != 1:
        m = m.resize((w, h), Image.LANCZOS)
    return np.asarray(m, np.float32) / 255


def over(a, rgba):
    """Composite a PIL RGBA layer (any size, resized to fit) over the float image a, in place."""
    h, w = a.shape[:2]
    if rgba.size != (w, h):
        rgba = rgba.resize((w, h), Image.BICUBIC)
    arr = np.asarray(rgba, np.float32) / 255
    al = arr[..., 3:4]
    a += (arr[..., :3] - a) * al


def vnoise(x, n, rng):
    """Smooth value noise over x in 0..1 with n lattice points (cosine-ish interpolation)."""
    pts = rng.random(n + 1) * 2 - 1
    t = x * n
    i = np.clip(np.floor(t).astype(int), 0, n - 1)
    f = t - i
    f = f * f * (3 - 2 * f)
    return pts[i] * (1 - f) + pts[i + 1] * f


def ridge_heights(w, rng, base, amp, octs=5, rough=.5, scale=1.0):
    x = (np.arange(w) + .5) / w
    y = np.zeros(w)
    for o in range(octs):
        y += vnoise(x, max(2, int(3 * 2 ** o * scale)), rng) * amp * rough ** o
    return base + y


def fill_below(a, heights, color, alpha=1.0, haze=None):
    """Fill everything below the per-column heights (pixel y) with a colour (soft 1px edge)."""
    h, w = a.shape[:2]
    Y = np.arange(h, dtype=np.float32)[:, None]
    m = np.clip(Y - heights[None, :].astype(np.float32) + .5, 0, 1)
    blend(a, m, color, alpha)
    return m


def reflect(a, yw, tint, k=.45, ripple=3.0, seed=3):
    """Mirror everything above row yw into the area below it, with ripples and a water tint."""
    h, w = a.shape[:2]
    rng = np.random.default_rng(seed)
    xs = np.arange(w)
    ph = rng.random() * 6
    out = a.copy()
    t = hexc(tint) if isinstance(tint, int) else np.asarray(tint, np.float32)
    for y in range(int(yw), h):
        d = y - yw
        src = int(np.clip(yw - d * 1.0, 0, h - 1))
        dx = (np.sin(xs * .045 + y * .35 + ph) * (ripple * (.3 + d / max(1, h - yw) * 1.6))).astype(int)
        row = a[src][np.clip(xs + dx, 0, w - 1)]
        f = min(1.0, k + .35 * d / max(1, h - yw))
        out[y] = row * (1 - f) + t * f
    a[:] = out


def clouds(a, rng, n, region, color=0xFFFFFF, alpha=.5, sz=(.12, .05), blur_r=18):
    h, w = a.shape[:2]
    lay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    c = tuple(int(x * 255) for x in hexc(color))
    for _ in range(n):
        cx = rng.uniform(region[0], region[2]) * w
        cy = rng.uniform(region[1], region[3]) * h
        sw = rng.uniform(.6, 1.2) * sz[0] * w
        sh = rng.uniform(.6, 1.2) * sz[1] * h
        for _k in range(6):
            ox = rng.normal(0, sw * .35)
            oy = rng.normal(0, sh * .25)
            rw = sw * rng.uniform(.3, .6)
            rh = sh * rng.uniform(.4, .8)
            d.ellipse([cx + ox - rw, cy + oy - rh, cx + ox + rw, cy + oy + rh], fill=c + (int(255 * alpha),))
    lay = lay.filter(ImageFilter.GaussianBlur(blur_r))
    over(a, lay)


def bokeh(a, rng, n, rmin, rmax, colors, amin=.05, amax=.25, ring=.4, region=(0, 0, 1, 1), blur_r=1.2, work=1920):
    """Soft discs / rings. Radii are in px of the working canvas (width <= `work`), scaled up to a."""
    h, w = a.shape[:2]
    s = min(1.0, work / w)
    ww, hh = int(w * s), int(h * s)
    ss = 2
    lay = Image.new("RGBA", (ww * ss, hh * ss), (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    for _ in range(n):
        cx = rng.uniform(region[0], region[2]) * ww * ss
        cy = rng.uniform(region[1], region[3]) * hh * ss
        r = rng.uniform(rmin, rmax) * ss * (ww / 1920 if work == 1920 else 1)
        col = colors[rng.integers(len(colors))]
        c = tuple(int(x * 255) for x in (hexc(col) if isinstance(col, int) else col))
        al = rng.uniform(amin, amax)
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=c + (int(255 * al),))
        if rng.random() < ring:
            d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=c + (int(255 * min(1, al * 2.2)),), width=max(1, int(r * .06)))
    lay = lay.resize((ww, hh), Image.LANCZOS).filter(ImageFilter.GaussianBlur(blur_r))
    over(a, lay)


def beams(a, cx, cy_top, angle_deg, width, color, k, rng=None, n=6, spread=.5, power=1.0):
    """Soft diagonal light shafts that start near the top (u = cx +- spread)."""
    h, w = a.shape[:2]
    u, v = coords(w, h)
    rng = rng or np.random.default_rng(5)
    th = math.radians(angle_deg)
    for i in range(n):
        x0 = cx + (rng.random() - .5) * spread * 2
        dist = (u - x0) * w * math.cos(th) + (v - cy_top) * h * math.sin(th)
        wd = width * rng.uniform(.5, 1.5)
        g = np.exp(-(dist / wd) ** 2) * (1 - smooth((v - cy_top) * 1.3)) * rng.uniform(.4, 1.0)
        a += hexc(color) * (g * k)[..., None] if isinstance(color, int) else np.asarray(color, np.float32) * (g * k)[..., None]


def rrect(d, box, r, fill=None, outline=None, width=1):
    d.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=width)


def iso_cube_faces(cx, cy, s):
    k = .8660254
    top = [(cx, cy), (cx - k * s, cy - .5 * s), (cx, cy - s), (cx + k * s, cy - .5 * s)]
    left = [(cx, cy), (cx - k * s, cy - .5 * s), (cx - k * s, cy + .5 * s), (cx, cy + s)]
    right = [(cx, cy), (cx + k * s, cy - .5 * s), (cx + k * s, cy + .5 * s), (cx, cy + s)]
    return top, left, right


def rgb255(c):
    return tuple(int(x * 255 + .5) for x in (hexc(c) if isinstance(c, int) else c))


# ---------------------------------------------------------------------------------------------- stroke font
def _arc(cx, cy, rx, ry, a0, a1, n=18):
    return [(cx + rx * math.cos(math.radians(a0 + (a1 - a0) * i / n)),
             cy + ry * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


# glyphs in a 0..W wide, 0..1 tall box (y down). value = (advance width, [polylines])
GLYPHS = {
    "T": (.6, [[(0, 0), (.6, 0)], [(.3, 0), (.3, 1)]]),
    "E": (.55, [[(.55, 0), (0, 0), (0, 1), (.55, 1)], [(0, .5), (.42, .5)]]),
    "I": (.2, [[(.1, 0), (.1, 1)]]),
    "S": (.6, [_arc(.3, .25, .3, .25, -30, -270) + _arc(.3, .75, .3, .25, -90, 150)[1:]]),
    "G": (.62, [_arc(.31, .5, .31, .5, -40, -360) + [(.31, .5)]]),
    "C": (.6, [_arc(.31, .5, .31, .5, -40, -320)]),
    "O": (.64, [_arc(.32, .5, .32, .5, 0, 360, 36)]),
    "D": (.6, [[(0, 0), (0, 1), (.22, 1)] + _arc(.22, .5, .36, .5, 90, -90)[0:] + [(0, 0)]]),
    "3": (.55, [_arc(.27, .25, .27, .25, -150, 90) + _arc(.28, .75, .27, .25, -90, 150)[1:]]),
    "A": (.64, [[(0, 1), (.32, 0), (.64, 1)], [(.12, .66), (.52, .66)]]),
    "M": (.7, [[(0, 1), (0, 0), (.35, .62), (.7, 0), (.7, 1)]]),
    "P": (.55, [[(0, 1), (0, 0), (.22, 0)] + _arc(.22, .27, .33, .27, -90, 90) + [(0, .54)]]),
    "Y": (.62, [[(0, 0), (.31, .52), (.62, 0)], [(.31, .52), (.31, 1)]]),
    "U": (.6, [[(0, 0), (0, .7)] + _arc(.3, .7, .3, .3, 180, 0) + [(.6, 0)]]),
    "B": (.58, [[(0, 0), (0, 1)],
                [(0, 0), (.26, 0)] + _arc(.26, .25, .3, .25, -90, 90) + [(0, .5)],
                [(0, .5), (.28, .5)] + _arc(.28, .75, .3, .25, -90, 90) + [(0, 1)]]),
    "X": (.6, [[(0, 0), (.6, 1)], [(.6, 0), (0, 1)]]),
    "-": (.4, [[(.04, .52), (.36, .52)]]),
    " ": (.35, []),
}


def text_mask(w, h, text, letter_h, stroke, x0, y_mid, advance=None, width_scale=1.0, ss=4, dotted=0):
    """Draw text with the stroke font into an L mask (w x h). Letters are letter_h tall; `advance` is the pitch
    per character (px) or None for natural spacing. dotted>0 draws dashes of that many px instead of lines."""
    m = Image.new("L", (w * ss, h * ss), 0)
    d = ImageDraw.Draw(m)
    x = x0
    for i, ch in enumerate(text):
        gw, lines = GLYPHS[ch]
        for pl in lines:
            pts = [((x + px * letter_h * width_scale) * ss, (y_mid - letter_h / 2 + py * letter_h) * ss) for px, py in pl]
            if dotted:
                _dash(d, pts, stroke * ss, dotted * ss)
            else:
                d.line(pts, fill=255, width=int(stroke * ss), joint="curve")
                r = stroke * ss / 2
                for p in (pts[0], pts[-1]):
                    d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r], fill=255)
        x += advance if advance else (gw * width_scale * letter_h + stroke * 1.6 + letter_h * .12)
    return np.asarray(m.resize((w, h), Image.LANCZOS), np.float32) / 255, x


def _dash(d, pts, width, dash):
    for p, q in zip(pts[:-1], pts[1:]):
        L = math.hypot(q[0] - p[0], q[1] - p[1])
        n = max(1, int(L / (dash * 1.6)))
        for i in range(n + 1):
            t = i / n
            cx, cy = p[0] + (q[0] - p[0]) * t, p[1] + (q[1] - p[1]) * t
            d.ellipse([cx - width / 2, cy - width / 2, cx + width / 2, cy + width / 2], fill=255)


def text_width(text, letter_h, stroke, width_scale=1.0):
    return sum(GLYPHS[c][0] * width_scale * letter_h + stroke * 1.6 + letter_h * .12 for c in text)
