"""Avatars, music covers, website icons, title wordmarks."""
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from gen_common import *


# ------------------------------------------------------------------------------------------ profile pictures
AVATARS = {
    # name: (colour 1, colour 2, gradient angle, hair colour, style)
    "Default PP": (0x6FB1E8, 0x3E6FC4, 60, 0x2C4F8C, "plain"),
    "Example PP 1": (0xFFB27A, 0xE8587A, 50, 0x6B2A45, "short"),
    "Example PP 2": (0x7FD6C2, 0x2E8FA8, 70, 0x1F5560, "long"),
    "Example PP 3": (0xB7A3F0, 0x6C6CD8, 40, 0x35318A, "bun"),
    "Example PP 4": (0xF4D77A, 0xE79A3E, 55, 0x7A4A1C, "cap"),
    "Example PP 5": (0x9CE0A0, 0x3FA66A, 65, 0x1E6040, "glasses"),
    "Example PP 6": (0xF29BC0, 0xB9569E, 45, 0x6D2D62, "curly"),
    "Example PP 7": (0x8DB8F5, 0x4B6FE0, 80, 0x2A3E94, "bob"),
}


def avatar(name):
    c1, c2, ang, hair, style = AVATARS[name]
    N, ss = 256, 4
    S = N * ss
    bg = dgrad(N, N, c1, c2, ang)
    glow(bg, .3, .22, .55, .55, 0xFFFFFF, .22)
    glow(bg, .85, 1.0, .6, .45, 0x000000 + c2, .0)
    vignette(bg, .12)
    # soft ring decoration behind the head
    lay = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    d.ellipse([S * .5 - S * .36, S * .4 - S * .36, S * .5 + S * .36, S * .4 + S * .36], fill=(255, 255, 255, 30))
    over_small = lay.resize((N, N), Image.LANCZOS)
    over(bg, over_small)

    body = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(body)
    white = (248, 251, 255, 255)
    shade = (214, 226, 244, 255)
    hc = rgb255(hair) + (255,)
    cx = S / 2

    def hair_back():
        if style == "long":
            d.rounded_rectangle([cx - S * .20, S * .22, cx + S * .20, S * .72], radius=S * .12, fill=hc)
        elif style == "bob":
            d.rounded_rectangle([cx - S * .19, S * .22, cx + S * .19, S * .56], radius=S * .1, fill=hc)
        elif style == "bun":
            d.ellipse([cx - S * .07, S * .12, cx + S * .07, S * .26], fill=hc)
        elif style == "curly":
            for k in range(9):
                a = math.radians(200 + k * 17.5)
                px, py = cx + math.cos(a) * S * .17, S * .37 + math.sin(a) * S * .17
                d.ellipse([px - S * .055, py - S * .055, px + S * .055, py + S * .055], fill=hc)

    hair_back()
    # shoulders and neck
    d.ellipse([cx - S * .38, S * .70, cx + S * .38, S * 1.24], fill=white)
    d.rounded_rectangle([cx - S * .065, S * .54, cx + S * .065, S * .74], radius=S * .03, fill=(228, 236, 248, 255))
    # head
    hx, hy, hr = cx, S * .385, S * .155
    d.ellipse([hx - hr, hy - hr * 1.08, hx + hr, hy + hr * 1.08], fill=white)
    # hair front
    if style == "short":
        d.pieslice([hx - hr * 1.06, hy - hr * 1.2, hx + hr * 1.06, hy + hr * .9], 180, 360, fill=hc)
    elif style in ("long", "bob"):
        d.pieslice([hx - hr * 1.08, hy - hr * 1.2, hx + hr * 1.08, hy + hr * .9], 180, 360, fill=hc)
        d.polygon([(hx - hr * 1.0, hy - hr * .55), (hx + hr * .2, hy - hr * 1.0), (hx + hr * 1.0, hy - hr * .3),
                   (hx + hr * 1.0, hy - hr * .6), (hx, hy - hr * 1.2), (hx - hr, hy - hr * .7)], fill=hc)
    elif style == "bun":
        d.pieslice([hx - hr * 1.06, hy - hr * 1.2, hx + hr * 1.06, hy + hr * .9], 190, 350, fill=hc)
    elif style == "cap":
        d.pieslice([hx - hr * 1.12, hy - hr * 1.28, hx + hr * 1.12, hy + hr * .62], 180, 360, fill=hc)
        d.rounded_rectangle([hx - hr * 1.15, hy - hr * .42, hx + hr * 1.5, hy - hr * .2], radius=hr * .12, fill=hc)
    elif style == "glasses":
        d.pieslice([hx - hr * 1.02, hy - hr * 1.2, hx + hr * 1.02, hy + hr * .8], 190, 350, fill=hc)
        lw = int(S * .012)
        for sx in (-1, 1):
            gx = hx + sx * hr * .45
            d.ellipse([gx - hr * .34, hy - hr * .18, gx + hr * .34, hy + hr * .5], outline=hc, width=lw)
        d.line([(hx - hr * .12, hy + hr * .08), (hx + hr * .12, hy + hr * .08)], fill=hc, width=lw)
    elif style == "curly":
        for k in range(7):
            a = math.radians(190 + k * 23)
            px, py = hx + math.cos(a) * hr * .95, hy + math.sin(a) * hr * .9
            d.ellipse([px - S * .05, py - S * .05, px + S * .05, py + S * .05], fill=hc)
    # shoulder shading
    shade_layer = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ds = ImageDraw.Draw(shade_layer)
    ds.ellipse([cx - S * .2, S * .62, cx + S * .2, S * .76], fill=(120, 140, 190, 70))
    shade_layer = shade_layer.filter(ImageFilter.GaussianBlur(S * .02))
    mask = body.split()[3]
    shade_layer.putalpha(Image.fromarray(np.minimum(np.asarray(shade_layer.split()[3]), np.asarray(mask))))
    body = Image.alpha_composite(body, shade_layer)
    # drop shadow
    sh = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    sh.putalpha(body.split()[3].point(lambda v: int(v * .28)))
    sh = sh.filter(ImageFilter.GaussianBlur(S * .02))
    sh_off = Image.new("RGBA", (S, S), (20, 30, 70, 0))
    sh_off.putalpha(sh.split()[3])
    sh_off = sh_off.transform((S, S), Image.AFFINE, (1, 0, 0, 0, 1, -S * .012))
    over(bg, sh_off.resize((N, N), Image.LANCZOS))
    over(bg, body.resize((N, N), Image.LANCZOS))
    grain(bg, 3, .8 / 255)
    return to_img(bg)


# ------------------------------------------------------------------------------------------ music covers
MUSIC = {
    "Example Music": (0x4DC3C9, 0x2D5FB5, "note"),
    "Example Playlist": (0xF2A65A, 0xD04E7A, "list"),
    "Mod Playlist": (0x8E7CE8, 0x3D4FB0, "mixer"),
}


def music_cover(name):
    c1, c2, sym = MUSIC[name]
    N, ss = 512, 2
    S = N * ss
    bg = dgrad(N, N, c1, c2, 55)
    glow(bg, .25, .2, .5, .5, 0xFFFFFF, .25)
    rng = np.random.default_rng(len(name) * 7)
    bokeh(bg, rng, 14, 30, 90, [0xFFFFFF, c1], .04, .12, ring=.3, blur_r=2)
    # concentric wave rings (vinyl / sound ripples)
    ring = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(ring)
    for i in range(7):
        r = S * (.2 + i * .075)
        d.ellipse([S * .5 - r, S * .5 - r, S * .5 + r, S * .5 + r], outline=(255, 255, 255, int(46 - i * 5)), width=ss * 2)
    over(bg, ring.resize((N, N), Image.LANCZOS))

    sym_l = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(sym_l)
    u = S / 512
    if sym == "note":
        d.ellipse([150 * u, 300 * u, 250 * u, 372 * u], fill=255)
        d.rounded_rectangle([232 * u, 130 * u, 252 * u, 338 * u], radius=10 * u, fill=255)
        d.polygon([(246 * u, 130 * u), (246 * u, 182 * u), (332 * u, 238 * u), (340 * u, 192 * u), (300 * u, 150 * u)], fill=255)
    elif sym == "list":
        d.polygon([(130 * u, 170 * u), (130 * u, 270 * u), (212 * u, 220 * u)], fill=255)
        for i, (x0, x1) in enumerate([(240, 390), (240, 390), (130, 390)]):
            y = (186 + i * 52) * u if i < 2 else 330 * u
            d.rounded_rectangle([x0 * u, y, x1 * u, y + 24 * u], radius=12 * u, fill=255)
        d.rounded_rectangle([240 * u, 238 * u, 390 * u, 262 * u], radius=12 * u, fill=255)
    else:
        for i, kx in enumerate((160, 256, 352)):
            d.rounded_rectangle([(kx - 9) * u, 150 * u, (kx + 9) * u, 362 * u], radius=9 * u, fill=255)
            ky = (230, 300, 200)[i]
            d.rounded_rectangle([(kx - 34) * u, (ky - 16) * u, (kx + 34) * u, (ky + 16) * u], radius=16 * u, fill=255)
    sym_l = sym_l.resize((N, N), Image.LANCZOS)
    m = np.asarray(sym_l, np.float32) / 255
    sh = np.asarray(blur(sym_l, 9), np.float32) / 255
    sh = np.roll(sh, 6, axis=0)
    bg *= (1 - sh * .28)[..., None]
    bg += (1 - bg) * (m * .96)[..., None]
    grain(bg, 5, .8 / 255)
    return to_img(bg)


# ------------------------------------------------------------------------------------------ website icons 64x64
ICONS = {
    "Example Website": (0xFF9A5A, 0xE2455E, "grid"),
    "Example Website 2": (0x4FA0FF, 0x2E4FD8, "ring"),
    "Example Website 3": (0xFF7A8E, 0xA8305E, "star"),
}


def site_icon(name):
    c1, c2, kind = ICONS[name]
    N, ss = 64, 8
    S = N * ss
    bg = dgrad(N, N, c1, c2, 60)
    glow(bg, .3, .2, .6, .6, 0xFFFFFF, .22)
    lay = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(lay)
    u = S / 64
    if kind == "grid":
        for gx in (0, 1):
            for gy in (0, 1):
                x, y = (19 + gx * 15) * u, (19 + gy * 15) * u
                d.rounded_rectangle([x, y, x + 11 * u, y + 11 * u], radius=3 * u, fill=255)
    elif kind == "ring":
        d.ellipse([16 * u, 16 * u, 48 * u, 48 * u], outline=255, width=int(5 * u))
        d.ellipse([27 * u, 27 * u, 37 * u, 37 * u], fill=255)
    else:
        pts = []
        for i in range(10):
            r = (16 if i % 2 == 0 else 7.2) * u
            a = math.radians(-90 + i * 36)
            pts.append((32 * u + r * math.cos(a), 33 * u + r * math.sin(a)))
        d.polygon(pts, fill=255)
    sym = np.asarray(lay.resize((N, N), Image.LANCZOS), np.float32) / 255
    bg += (1 - bg) * (sym * .97)[..., None]
    circ = Image.new("L", (S, S), 0)
    ImageDraw.Draw(circ).ellipse([0, 0, S - 1, S - 1], fill=255)
    al = np.asarray(circ.resize((N, N), Image.LANCZOS))
    rgb = (np.clip(bg, 0, 1) * 255 + .5).astype(np.uint8)
    return Image.fromarray(np.dstack([rgb, al]), "RGBA")


# ------------------------------------------------------------------------------------------ title wordmarks
def wordmark(W, H, text, colors, letter_h=40, stroke=4.2, dotted_upto=0):
    """RGBA strip, transparent background, letters tracked out to fill the width."""
    n = len(text)
    lw = .62
    letter_w = letter_h * lw
    margin = 6
    adv = (W - 2 * margin - letter_w) / (n - 1)
    mask, _ = text_mask(W, H, text, letter_h, stroke, margin, H / 2, advance=adv, width_scale=1.0)
    u, _v = coords(W, H)
    cols = np.stack([np.interp(u[0], np.linspace(0, 1, len(colors)), [hexc(c)[i] for c in colors]) for i in range(3)], -1)
    rgb = np.repeat(cols[None, :, :], H, 0)
    a = (mask * 255 + .5).astype(np.uint8)
    out = np.dstack([(rgb * 255 + .5).astype(np.uint8), a])
    return Image.fromarray(out, "RGBA")


def cutebox_logo(W=489, H=100):
    letter_h = 58
    stroke_a, stroke_b = 5, 9
    ma, x = text_mask(W, H, "CUTE", letter_h, stroke_a, 8, H / 2, dotted=0)
    mb, _ = text_mask(W, H, "BOX", letter_h, stroke_b, x + 22, H / 2)
    m = np.maximum(ma * .85, mb)
    a = (m * 255 + .5).astype(np.uint8)
    return Image.fromarray(np.dstack([np.full((H, W, 3), 255, np.uint8), a]), "RGBA")
