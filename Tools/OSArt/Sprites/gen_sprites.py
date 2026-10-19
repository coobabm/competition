#!/usr/bin/env python3
"""Procedural UI sprites for the 灵光 desktop (replaces the DreamOS PNGs in place).

Every image is drawn from code: signed-distance coverage for the large borders and
shadows, supersampled PIL drawing (8x, box-filtered down) for the icons.
Borders, shadows and glyph icons are white-on-transparent so Unity can tint them.

Usage:
    python3 gen_sprites.py                  # overwrite the PNGs in the project
    python3 gen_sprites.py --out /some/dir  # write the same tree somewhere else (preview)
    python3 gen_sprites.py --only Shadows   # only paths containing this text

Only needs numpy + Pillow.  File names, sizes and RGBA mode match the originals; the
.meta files (GUIDs, spriteBorder, pixels-per-unit) are never touched.
"""
import argparse
import math
import os
import random

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
TEX = os.path.join(PROJECT, "Assets", "LingGuangV05/DesktopBridge/OSArt", "Textures")

# --------------------------------------------------------------------------- output
def save_alpha(path, alpha, rgb=None):
    """alpha: float array 0..1 (H,W).  rgb: None (white) or float array (H,W,3) 0..255."""
    h, w = alpha.shape
    out = np.empty((h, w, 4), np.uint8)
    if rgb is None:
        out[..., :3] = 255
    else:
        out[..., :3] = np.clip(rgb + 0.5, 0, 255).astype(np.uint8)
    out[..., 3] = np.clip(alpha * 255 + 0.5, 0, 255).astype(np.uint8)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    Image.fromarray(out, "RGBA").save(path, optimize=True)


# --------------------------------------------------------------------------- numpy SDF helpers
def grid(w, h):
    y, x = np.mgrid[0:h, 0:w]
    return x + 0.5, y + 0.5


def cov(d):
    """Signed distance in pixels (negative inside) -> anti-aliased coverage."""
    return np.clip(0.5 - d, 0.0, 1.0)


def sd_rrect(x, y, cx, cy, hw, hh, r):
    qx = np.abs(x - cx) - (hw - r)
    qy = np.abs(y - cy) - (hh - r)
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r


def sd_circle(x, y, cx, cy, r):
    return np.hypot(x - cx, y - cy) - r


def blur(a, sigma):
    """Gaussian blur through FFT (zero padded)."""
    h, w = a.shape
    pad = int(sigma * 4) + 2
    H, W = h + 2 * pad, w + 2 * pad
    fy = np.fft.fftfreq(H)[:, None]
    fx = np.fft.rfftfreq(W)[None, :]
    k = np.exp(-2 * (math.pi ** 2) * sigma ** 2 * (fx ** 2 + fy ** 2))
    buf = np.zeros((H, W))
    buf[pad:pad + h, pad:pad + w] = a
    res = np.fft.irfft2(np.fft.rfft2(buf) * k, s=(H, W))
    return np.clip(res[pad:pad + h, pad:pad + w], 0, 1)


def smoothstep(t):
    t = np.clip(t, 0, 1)
    return t * t * (3 - 2 * t)


# --------------------------------------------------------------------------- supersampled vector canvas
class Cv:
    """Vector canvas in 1x units; drawn at ss x and box-filtered down."""

    def __init__(self, w, h, ss=8):
        self.w, self.h, self.ss = w, h, ss
        self.im = Image.new("L", (w * ss, h * ss), 0)
        self.d = ImageDraw.Draw(self.im)

    def _s(self, pts):
        s = self.ss
        return [(x * s, y * s) for x, y in pts]

    def poly(self, pts, v=255):
        self.d.polygon(self._s(pts), fill=v)

    def circle(self, cx, cy, r, v=255):
        s = self.ss
        self.d.ellipse([(cx - r) * s, (cy - r) * s, (cx + r) * s, (cy + r) * s], fill=v)

    def ellipse(self, cx, cy, rx, ry, v=255):
        s = self.ss
        self.d.ellipse([(cx - rx) * s, (cy - ry) * s, (cx + rx) * s, (cy + ry) * s], fill=v)

    def rrect(self, x0, y0, x1, y1, r, v=255):
        s = self.ss
        self.d.rounded_rectangle([x0 * s, y0 * s, x1 * s - 1, y1 * s - 1], radius=r * s, fill=v)

    def stroke(self, pts, width, v=255, closed=False, cap="round"):
        """Thick polyline; round joins.  cap='round' or 'butt'."""
        pts = list(pts)
        if closed:
            pts = pts + [pts[0]]
        r = width / 2.0
        for i in range(len(pts) - 1):
            (x0, y0), (x1, y1) = pts[i], pts[i + 1]
            dx, dy = x1 - x0, y1 - y0
            L = math.hypot(dx, dy)
            if L < 1e-9:
                continue
            nx, ny = -dy / L * r, dx / L * r
            self.poly([(x0 + nx, y0 + ny), (x1 + nx, y1 + ny), (x1 - nx, y1 - ny), (x0 - nx, y0 - ny)], v)
        for i, (x, y) in enumerate(pts):
            if cap == "round" or closed or 0 < i < len(pts) - 1:
                self.circle(x, y, r, v)

    def ring(self, cx, cy, r_out, r_in, v=255):
        self.circle(cx, cy, r_out, v)
        self.circle(cx, cy, r_in, 0 if v else 255)

    def mask(self):
        im = self.im.resize((self.w, self.h), Image.BOX)
        return np.asarray(im, np.float64) / 255.0


def bez(p0, p1, p2, p3, n=96):
    pts = []
    for i in range(n + 1):
        t = i / n
        u = 1 - t
        pts.append((u ** 3 * p0[0] + 3 * u * u * t * p1[0] + 3 * u * t * t * p2[0] + t ** 3 * p3[0],
                    u ** 3 * p0[1] + 3 * u * u * t * p1[1] + 3 * u * t * t * p2[1] + t ** 3 * p3[1]))
    return pts


def arc(cx, cy, r, a0, a1, n=64):
    """Angles in degrees, counter-clockwise on screen (y down)."""
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)),
             cy - r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def inset_poly(pts, r):
    """Move every edge of a convex polygon inward by r (any winding)."""
    n = len(pts)
    area = sum(pts[i][0] * pts[(i + 1) % n][1] - pts[(i + 1) % n][0] * pts[i][1] for i in range(n))
    sgn = 1 if area > 0 else -1
    lines = []
    for i in range(n):
        (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % n]
        dx, dy = x1 - x0, y1 - y0
        L = math.hypot(dx, dy)
        nx, ny = -dy / L * sgn, dx / L * sgn  # inward normal (screen coords)
        lines.append(((x0 + nx * r, y0 + ny * r), (dx, dy)))
    out = []
    for i in range(n):
        (px, py), (dx, dy) = lines[i - 1]
        (qx, qy), (ex, ey) = lines[i]
        den = dx * ey - dy * ex
        t = ((qx - px) * ey - (qy - py) * ex) / den
        out.append((px + dx * t, py + dy * t))
    return out


def round_poly(cv, pts, r, v=255):
    ins = inset_poly(pts, r)
    cv.poly(ins, v)
    cv.stroke(ins, 2 * r, v, closed=True)


def star4(cx, cy, reach, pinch=0.62, n=200, rot=0.0):
    """Four-pointed sparkle: superellipse with exponent < 1 (concave sides)."""
    pts = []
    e = 2.0 / pinch
    for i in range(n):
        t = 2 * math.pi * i / n
        c, s = math.cos(t), math.sin(t)
        x = reach * math.copysign(abs(c) ** e, c)
        y = reach * math.copysign(abs(s) ** e, s)
        if rot:
            x, y = x * math.cos(rot) - y * math.sin(rot), x * math.sin(rot) + y * math.cos(rot)
        pts.append((cx + x, cy + y))
    return pts


# --------------------------------------------------------------------------- borders
def gen_borders(out):
    # Flat filled: 4x4, fully opaque white (1px 9-slice border).
    save_alpha(os.path.join(out, "Borders/Flat/Flat Filled.png"), np.ones((4, 4)))

    # Rounded filled: one squircle-free circular corner of radius 75 on a 1024 canvas.
    x, y = grid(1024, 1024)
    rounded = cov(sd_rrect(x, y, 512, 512, 512, 512, 75))
    for rel in ["Rounded/1024px/Rounded Filled 1024px.png", "Rounded/128px/Rounded Filled 128px.png",
                "Rounded/256px/Rounded Filled 256px (Chat Message).png",
                "Rounded/256px/Rounded Filled 256px.png", "Rounded/64px/Rounded Filled 64px.png"]:
        save_alpha(os.path.join(out, "Borders", rel), rounded)

    # "Cut": 126x126, square on the left, radius 15 on the two right corners.
    x, y = grid(126, 126)
    full = sd_rrect(x, y, 63, 63, 63, 63, 15)
    # left half: plain rectangle coverage (edges at the canvas border, so fully opaque)
    alpha = cov(np.where(x < 63, -1.0, full))
    save_alpha(os.path.join(out, "Borders/Rounded/128px/Rounded Filled 128px - Cut.png"), alpha)

    # Radial: filled disc and rings on a 1024 canvas.
    x, y = grid(1024, 1024)
    r = np.hypot(x - 512, y - 512)
    disc = cov(r - 512)
    for rel in ["Radial/1024px/Radial Filled 1024px.png", "Radial/128px/Radial Filled 128px.png",
                "Radial/256px/Radial Filled 256px.png", "Radial/32px/Radial Filled 32px.png",
                "Radial/64px/Radial Filled 64px.png"]:
        save_alpha(os.path.join(out, "Borders", rel), disc)
    thick = {"1x": 25, "2x": 40, "3x": 65, "6x": 110, "8x": 145, "10x": 175}  # ring thickness at 1024 px
    for rel in ["Radial/128px/Radial Outline 128px - 10x.png", "Radial/128px/Radial Outline 128px - 2x.png",
                "Radial/128px/Radial Outline 128px - 3x.png", "Radial/256px/Radial Outline 256px - 1x.png",
                "Radial/256px/Radial Outline 256px - 2x.png", "Radial/256px/Radial Outline 256px - 6x.png",
                "Radial/256px/Radial Outline 256px - 8x.png", "Radial/64px/Radial Outline 64px - 10x.png",
                "Radial/64px/Radial Outline 64px - 2x.png", "Radial/64px/Radial Outline 64px - 3x.png",
                "Radial/64px/Radial Outline 64px - 6x.png", "Radial/64px/Radial Outline 64px - 8x.png"]:
        k = rel.rsplit(" - ", 1)[1].split(".")[0]
        t = thick[k]
        ring = np.minimum(cov(r - 512), cov(-(r - (512 - t))))
        save_alpha(os.path.join(out, "Borders", rel), ring)


# --------------------------------------------------------------------------- shadows
def glow_around_hole(n, hole_half, hole_r, grow, sigma, peak):
    """Soft glow outside a rounded-square hole; the hole itself stays transparent."""
    x, y = grid(n, n)
    c = n / 2.0
    src = cov(sd_rrect(x, y, c, c, hole_half + grow, hole_half + grow, hole_r + grow))
    hole = cov(sd_rrect(x, y, c, c, hole_half, hole_half, hole_r))
    return blur(src, sigma) * peak * (1 - hole)


def gen_shadows(out):
    S = os.path.join(out, "Shadows")
    # Flat shadows (9-slice, hole in the middle): glow strongest at the hole rim.
    save_alpha(os.path.join(S, "Flat/Rounded Outline Shadow.png"),
               glow_around_hole(128, 50, 5, 2.5, 5.0, 250 / 255))
    save_alpha(os.path.join(S, "Flat/Square Shadow.png"),
               glow_around_hole(128, 40, 0.5, 2.5, 9.0, 205 / 255))

    # Radial shadows.
    x, y = grid(102, 102)
    hole = cov(sd_circle(x, y, 51, 51, 32))
    src = cov(sd_circle(x, y, 51, 51, 32 + 4))
    save_alpha(os.path.join(S, "Radial/Radial Outline Shadow.png"), blur(src, 6.2) * (235 / 255) * (1 - hole))
    x, y = grid(726, 726)
    src = cov(sd_circle(x, y, 363, 363, 246))
    save_alpha(os.path.join(S, "Radial/Radial Directional Shadow Filled.png"), blur(src, 54.0))

    # Gradient shadows: alpha ramps from fully opaque on one edge to 0.
    def low(d):    # d: distance from the opaque edge in px, 1024 long
        return np.exp(-(d / 450.0) ** 1.55) * (1 - smoothstep((d - 700) / 150.0))

    def normal(d):
        return 1 - smoothstep((d / 880.0) ** 2.0)

    xs = np.arange(1024) + 0.5
    prof_low, prof_norm = low(xs), normal(xs)
    # Horizontal*: 1024 wide x 2048 high, opaque at the left, uniform in y.
    save_alpha(os.path.join(S, "Gradient/Horizontal Gradient Shadow (Low).png"),
               np.tile(prof_low[None, :], (2048, 1)))
    save_alpha(os.path.join(S, "Gradient/Horizontal Gradient Shadow (Normal).png"),
               np.tile(prof_norm[None, :], (2048, 1)))
    # Vertical*: 2048 wide x 1024 high, opaque at the bottom, uniform in x.
    save_alpha(os.path.join(S, "Gradient/Vertical Gradient Shadow (Low).png"),
               np.tile(prof_low[::-1][:, None], (1, 2048)))
    save_alpha(os.path.join(S, "Gradient/Vertical Gradient Shadow (Normal).png"),
               np.tile(prof_norm[::-1][:, None], (1, 2048)))


# --------------------------------------------------------------------------- small system icons
def gen_system(out):
    I = os.path.join(out, "Icons")
    # Panel indicator: 40x20 up-pointing triangle.
    cv = Cv(40, 20)
    cv.poly([(20, 0), (40, 20), (0, 20)])
    save_alpha(os.path.join(I, "64x/Panel Indicator.png"), cv.mask())

    # Resize stripes: two diagonal bands in a 64x64 grip corner (grip sits at lower right).
    n = 64
    ss = 8
    yy, xx = np.mgrid[0:n * ss, 0:n * ss]
    px, py = (xx + 0.5) / ss, (yy + 0.5) / ss
    s = px + py * 1.0
    inside = (px >= 6) & (px <= 56) & (py >= 6) & (py <= 56)
    m = (((s >= 62) & (s <= 75)) | ((s >= 95) & (s <= 109))) & inside
    a = m.reshape(n, ss, n, ss).mean(axis=(1, 3))
    save_alpha(os.path.join(I, "64x/Resize Stripes.png"), a)

    # Loading spinner: 300-degree arc, opaque head with a round cap, tail fading out. The ring keeps an 8 px margin
    # (2026-10-19): touching the texture edge clipped it flat at the top, right and bottom while it spun.
    n = 256
    x, y = grid(n, n)
    r = np.hypot(x - 128, y - 128)
    th = np.degrees(np.arctan2(-(y - 128), x - 128)) % 360
    ring = np.minimum(cov(r - 120), cov(-(r - 100)))
    head = 222.0          # round-capped end, fully opaque from here counter-clockwise...
    # angle travelled from the head (counter-clockwise) 0..360
    trav = (th - head) % 360
    # opaque for the first 235 deg, then fades to 0 by 320 deg
    fade = np.where(trav < 235, 1.0, (1 - np.clip((trav - 235) / 85.0, 0, 1)) ** 1.8)
    spin = ring * fade
    cap_c = (128 + 110 * math.cos(math.radians(head)), 128 - 110 * math.sin(math.radians(head)))
    cap = cov(sd_circle(x, y, cap_c[0], cap_c[1], 10))
    spin = np.maximum(spin, cap)
    save_alpha(os.path.join(out, "Spinners/Basic Spinner (64x).png"), spin)


# --------------------------------------------------------------------------- media control frames
def gen_media(out):
    A = os.path.join(out, "Icons/Others/Animated Icons")
    # Forward: bar (right edge) + rounded play triangle (left).
    cv = Cv(256, 256)
    cv.rrect(226, 0, 256, 256, 15)
    save_alpha(os.path.join(A, "Forward Line.png"), cv.mask())
    cv = Cv(256, 256)
    round_poly(cv, [(2, 18), (2, 238), (170, 128)], 11)
    save_alpha(os.path.join(A, "Forward P1.png"), cv.mask())

    # Repeat: elbow arrow with an open chevron head; P2 is P1 rotated 180 degrees.
    W = 28
    cv = Cv(256, 256)
    path = [(30, 170), (30, 92)] + arc(74, 92, 44, 180, 90, 24) + [(218, 48)]
    cv.stroke(path, W, cap="round")
    cv.stroke([(192, 18), (228, 48), (192, 78)], W, cap="round")
    p1 = cv.mask()
    save_alpha(os.path.join(A, "Repeat P1.png"), p1)
    save_alpha(os.path.join(A, "Repeat P2.png"), p1[::-1, ::-1])

    # Shuffle: S-curve ending in a solid arrowhead; P2 is P1 flipped vertically.
    cv = Cv(256, 256)
    curve = bez((0, 46), (116, 46), (96, 212), (206, 212))
    cv.stroke(curve, W, cap="butt")
    cv.poly([(200, 176), (255, 212), (200, 248)])
    p1 = cv.mask()
    save_alpha(os.path.join(A, "Shuffle P1.png"), p1)
    save_alpha(os.path.join(A, "Shuffle P2.png"), p1[::-1, :])


# --------------------------------------------------------------------------- generic glyph icons (256x256, white)
def glyph_orb_spark():            # "Cori": soft orb with a sparkle
    cv = Cv(256, 256)
    cv.ring(128, 128, 124, 106)
    cv.circle(128, 128, 88)
    cv.poly(star4(128, 128, 62, 0.55), 0)
    cv.circle(128, 128, 7, 255)
    return cv.mask()


def glyph_orb_wave():             # "Cori Alt": voice orb, five equaliser bars cut out
    cv = Cv(256, 256)
    cv.circle(128, 128, 124)
    for x, h in zip((60, 94, 128, 162, 196), (44, 86, 118, 70, 36)):
        cv.rrect(x - 9, 128 - h / 2, x + 9, 128 + h / 2, 9, 0)
    return cv.mask()


def glyph_ripple():               # "Cori Alt 2": ripples
    cv = Cv(256, 256)
    cv.ring(128, 128, 124, 106)
    cv.ring(128, 128, 84, 68)
    cv.circle(128, 128, 42)
    return cv.mask()


def glyph_chat():                 # chat bubble with three dots
    cv = Cv(256, 256)
    cv.rrect(8, 22, 248, 188, 56)
    cv.poly([(56, 170), (110, 176), (50, 244)])
    for x in (76, 128, 180):
        cv.circle(x, 105, 16, 0)
    return cv.mask()


def glyph_controller():           # game controller
    cv = Cv(256, 256)
    cv.rrect(4, 56, 252, 176, 60)
    cv.rrect(16, 120, 100, 244, 40)
    cv.rrect(156, 120, 240, 244, 40)
    # d-pad (cut)
    cv.rrect(58, 84, 82, 148, 7, 0)
    cv.rrect(40, 102, 100, 126, 7, 0)
    # buttons (cut)
    for bx, by in ((176, 90), (206, 114), (176, 138), (146, 114)):
        cv.circle(bx, by, 12, 0)
    return cv.mask()


def glyph_forum():                # two overlapping bubbles
    cv = Cv(256, 256)
    cv.rrect(6, 10, 176, 128, 36)
    cv.poly([(34, 118), (78, 118), (30, 168)])
    cv.rrect(86 - 12, 90 - 12, 250 + 12, 198 + 12, 44, 0)       # gap around the second bubble
    cv.poly([(190, 190), (220, 190), (230, 244)], 0)
    cv.rrect(86, 90, 250, 198, 36)
    cv.poly([(192, 192), (222, 192), (226, 246)])
    cv.rrect(36, 44, 140, 58, 7, 0)
    cv.rrect(36, 74, 108, 88, 7, 0)
    cv.rrect(116, 130, 220, 144, 7, 0)
    cv.rrect(116, 152, 190, 166, 7, 0)
    return cv.mask()


def glyph_share():                # three nodes, bird-free
    cv = Cv(256, 256)
    cv.stroke([(54, 128), (200, 48)], 20)
    cv.stroke([(54, 128), (200, 208)], 20)
    for c in ((54, 128), (200, 48), (200, 208)):
        cv.circle(c[0], c[1], 40)
    return cv.mask()


def glyph_play_disc():            # play button
    cv = Cv(256, 256)
    cv.circle(128, 128, 124)
    cv2 = Cv(256, 256)
    round_poly(cv2, [(96, 70), (96, 186), (196, 128)], 8)
    m = cv.mask() * (1 - cv2.mask())
    return m


def outline_chat():
    cv = Cv(256, 256)
    cv.rrect(8, 36, 248, 190, 52)
    cv.poly([(60, 176), (112, 182), (54, 238)])
    cv.rrect(8 + 15, 36 + 15, 248 - 15, 190 - 15, 38, 0)
    cv.poly([(76, 176), (100, 178), (66, 212)], 0)
    for x in (80, 128, 176):
        cv.circle(x, 113, 13)
    return cv.mask()


def outline_share():
    cv = Cv(256, 256)
    cv.stroke([(54, 128), (200, 48)], 15)
    cv.stroke([(54, 128), (200, 208)], 15)
    for c in ((54, 128), (200, 48), (200, 208)):
        cv.circle(c[0], c[1], 40)
    for c in ((54, 128), (200, 48), (200, 208)):
        cv.circle(c[0], c[1], 26, 0)
    return cv.mask()


def outline_play():
    cv = Cv(256, 256)
    cv.ring(128, 128, 124, 109)
    tri = [(98, 66), (98, 190), (196, 128)]
    cv.stroke(tri, 15, closed=True)
    return cv.mask()


def gen_glyphs(out):
    I = os.path.join(out, "Icons")
    table = {
        "Cori": glyph_orb_spark(), "Cori Alt": glyph_orb_wave(), "Cori Alt 2": glyph_ripple(),
        "Discord": glyph_chat(), "Reddit": glyph_forum(), "Steam": glyph_controller(),
        "Twitter": glyph_share(), "YouTube": glyph_play_disc(),
    }
    for folder in ("128x", "256x", "64x"):
        for nm, m in table.items():
            save_alpha(os.path.join(I, folder, nm + ".png"), m)
    save_alpha(os.path.join(I, "32x", "Steam.png"), table["Steam"])
    # settings-element line icons
    S = os.path.join(I, "Others/Settings Elements")
    save_alpha(os.path.join(S, "Discord.png"), outline_chat())
    save_alpha(os.path.join(S, "Twitter.png"), outline_share())
    save_alpha(os.path.join(S, "YouTube.png"), outline_play())


# --------------------------------------------------------------------------- audio messages / brand / QR
def gen_audio(out):
    """Four voice-message waveforms: 20 round-capped bars (10 px wide, 20 px pitch), 14..80 px tall."""
    for k in range(1, 5):
        rnd = random.Random(7000 + k)
        f1, f2, f3 = rnd.uniform(0.25, 0.45), rnd.uniform(0.7, 1.1), rnd.uniform(1.5, 2.4)
        p1, p2, p3 = (rnd.uniform(0, 6.28) for _ in range(3))
        cv = Cv(390, 86)
        for j in range(20):
            env = math.sin(math.pi * (j + 0.5) / 20) ** 0.6          # speech swells in the middle
            v = 0.5 + 0.28 * math.sin(f1 * j + p1) + 0.2 * math.sin(f2 * j + p2) + 0.14 * math.sin(f3 * j + p3)
            v = 0.15 + 0.85 * max(0.0, min(1.0, v * (0.55 + 0.45 * env)))
            h = max(14, min(80, round(80 * v / 2) * 2))
            x0 = j * 20
            cv.rrect(x0, 43 - h / 2, x0 + 10, 43 + h / 2, 5)
        save_alpha(os.path.join(out, "Icons/Others/System/Audio Message %d.png" % k), cv.mask())


def gradient(w, h, c0, c1, diag=True):
    x, y = grid(w, h)
    t = (x / w + y / h) / 2 if diag else x / w
    t = t[..., None]
    return np.array(c0)[None, None, :] * (1 - t) + np.array(c1)[None, None, :] * t


def gen_brand(out):
    O = os.path.join(out, "Icons/Others/System")
    C0, C1 = (118, 226, 255), (58, 132, 255)     # light cyan -> blue
    # Desktop mark: orbit ring, sparkle, small satellite.
    cv = Cv(256, 256)
    cv.ring(128, 128, 122, 110)
    cv.poly(star4(128, 128, 92, 0.58))
    cv.poly(star4(128, 128, 40, 0.58, rot=math.pi / 4), 255)
    cv.circle(128 + 86, 128 - 86, 14)
    m = cv.mask()
    save_alpha(os.path.join(O, "DreamOS Icon.png"), m, gradient(256, 256, C0, C1))
    # Wide wordless banner: sparkle in the middle, tapering light rays, a soft glow.
    W, H = 680, 97
    cv = Cv(W, H)
    cx, cy = W / 2, H / 2
    for sgn in (-1, 1):
        cv.poly([(cx + sgn * 30, cy - 6), (cx + sgn * (W / 2 - 10), cy - 0.8),
                 (cx + sgn * (W / 2 - 10), cy + 0.8), (cx + sgn * 30, cy + 6)])
        cv.poly([(cx + sgn * 30, cy - 12), (cx + sgn * 190, cy - 0.2),
                 (cx + sgn * 190, cy + 0.2), (cx + sgn * 30, cy + 12)], 150)
    cv.poly(star4(cx, cy, 47, 0.55), 255)
    for dx, r in ((-232, 13), (232, 13), (-300, 8), (300, 8)):
        cv.poly(star4(cx + dx, cy, r, 0.55), 255)
    m = cv.mask()
    glow = blur(m, 14.0) * 0.55
    m = np.maximum(m, glow)
    save_alpha(os.path.join(O, "DreamOS Logo.png"), m, gradient(W, H, C0, C1, diag=False))


def gen_qr(out):
    rnd = random.Random(2016)
    N = 29
    mod = 446.0 / N
    ox = oy = 32.0
    grid_ = [[rnd.random() < 0.48 for _ in range(N)] for _ in range(N)]

    def finder(r0, c0):
        for r in range(-1, 8):
            for c in range(-1, 8):
                rr, cc = r0 + r, c0 + c
                if not (0 <= rr < N and 0 <= cc < N):
                    continue
                if r in (-1, 7) or c in (-1, 7):
                    grid_[rr][cc] = False
                elif r in (0, 6) or c in (0, 6):
                    grid_[rr][cc] = True
                elif 2 <= r <= 4 and 2 <= c <= 4:
                    grid_[rr][cc] = True
                else:
                    grid_[rr][cc] = False
    finder(0, 0)
    finder(0, N - 7)
    finder(N - 7, 0)
    for i in range(8, N - 8):                      # timing lines
        grid_[6][i] = grid_[i][6] = (i % 2 == 0)
    # alignment-like block near bottom right
    for r in range(-2, 3):
        for c in range(-2, 3):
            grid_[N - 9 + r][N - 9 + c] = (max(abs(r), abs(c)) != 1)
    ss = 4
    S = 512
    cvm = Cv(S, S, ss)
    cvm.rrect(0, 0, S, S, 10)                      # white plate
    for r in range(N):
        for c in range(N):
            if grid_[r][c]:
                x0, y0 = ox + c * mod, oy + r * mod
                cvm.d.rectangle([x0 * ss, y0 * ss, (x0 + mod) * ss - 1, (y0 + mod) * ss - 1], fill=0)
    save_alpha(os.path.join(out, "Icons/Others/System/QR Code.png"), cvm.mask())


# --------------------------------------------------------------------------- main
STEPS = [gen_borders, gen_shadows, gen_system, gen_media, gen_glyphs, gen_audio, gen_brand, gen_qr]

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=TEX, help="output Textures root (default: the project's DreamOS Textures folder)")
    args = ap.parse_args()
    for step in STEPS:
        print(step.__name__)
        step(args.out)
