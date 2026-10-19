"""Landscapes, wallpapers, photos, website pictures, game-hub banners."""
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from gen_common import *


def layer(ww, hh, ss, fn):
    """RGBA layer drawn at ss x; fn(draw, ss). Returned at (ww, hh)."""
    lay = Image.new("RGBA", (ww * ss, hh * ss), (0, 0, 0, 0))
    fn(ImageDraw.Draw(lay), ss)
    return lay.resize((ww, hh), Image.LANCZOS) if ss != 1 else lay


def rgba(c, a):
    return rgb255(c) + (int(a * 255),)


# ===================================================================================================== landscape
def landscape(w, h, seed, sky, layers, water=None, sun=None, cloud_n=0, cone=None, fg=None, streaks=None):
    """Generic layered landscape. layers: dicts(base, amp, color, scale?, rough?, octs?)."""
    rng = np.random.default_rng(seed)
    a = vgrad(w, h, sky)
    if sun:
        su, sv, sc, sk, sr = sun
        glow(a, su, sv, sr, sr * w / h, sc, sk, 1.6)
        glow(a, su, sv, sr * .25, sr * .25 * w / h, 0xFFFFFF, sk * .5, 1.6)
    if cloud_n:
        clouds(a, rng, cloud_n, (0, .06, 1, .38), 0xFFFFFF, .38, (.14, .05), blur_r=max(8, w // 90))
    for L in layers:
        hts = ridge_heights(w, rng, L["base"] * h, L["amp"] * h, L.get("octs", 5), L.get("rough", .5), L.get("scale", 1.0))
        fill_below(a, hts, L["color"])
        if "rim" in L:
            Y = np.arange(h, dtype=np.float32)[:, None]
            blend(a, np.exp(-((Y - hts[None, :]) / (h * .003)) ** 2) * (Y >= hts[None, :]), L["rim"], .35)
    if cone:
        x = (np.arange(w) + .5) / w
        prof = np.clip(1 - np.abs(x - cone["u"]) / cone["half"], 0, 1)
        prof = prof ** cone.get("shape", 1.35)
        hts = cone["base"] * h - cone["height"] * h * prof + rng.normal(0, 1, w).cumsum() * 0.0
        hts += (vnoise(x, 60, rng) * .004 + vnoise(x, 14, rng) * .006) * h
        fill_below(a, hts, cone["color"])
        capy = cone["base"] * h - cone["height"] * h * cone.get("cap", .78)
        wavy = (vnoise(x, 24, rng) * .025 + vnoise(x, 7, rng) * .02) * h
        Y = np.arange(h, dtype=np.float32)[:, None]
        inside = np.clip(Y - hts[None, :] + .5, 0, 1)
        capm = inside * smooth((capy + wavy[None, :] - Y) / (h * .01) + .5)
        blend(a, capm, cone["snow"], .95)
        # soft shading on one flank
        shade = np.clip((x - cone["u"]) * 5, 0, 1)[None, :]
        blend(a, inside * shade * .25, cone["color"] if isinstance(cone["color"], int) else 0x101830, .6)
    if water is not None:
        yw = int(water["y"] * h)
        reflect(a, yw, water["tint"], water.get("k", .4), water.get("ripple", 3.0), seed + 1)
        if streaks:
            Y, X = np.mgrid[0:h, 0:w].astype(np.float32)
            for _ in range(streaks):
                sx, sy = rng.uniform(.1, .9) * w, rng.uniform(water["y"] + .02, .95) * h
                ln = rng.uniform(.03, .12) * w
                g = np.exp(-(((X - sx) / ln) ** 2 + ((Y - sy) / (h * .0016)) ** 2))
                a += hexc(water.get("streak", 0xFFFFFF)) * (g * .22)[..., None]
        for L in water.get("shore", []):
            hts = ridge_heights(w, rng, L["base"] * h, L["amp"] * h, L.get("octs", 5), L.get("rough", .55), L.get("scale", 1.0))
            fill_below(a, hts, L["color"])
    vignette(a, .16)
    grain(a, seed, 1.3 / 255)
    return a


def maple_branch(a, seed, color_leaves, region=(0, 0, .55, .3)):
    """Dark branch + clustered small red leaves, drawn in the upper-left like a framing foreground."""
    h, w = a.shape[:2]
    rng = np.random.default_rng(seed)
    back = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    front = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for lay, blur_r, n in ((back, 6, 260), (front, 0, 190)):
        d = ImageDraw.Draw(lay)
        for _ in range(n):
            cx = rng.uniform(region[0], region[2]) * w * (rng.random() ** .8)
            cy = rng.uniform(region[1], region[3]) * h * (rng.random() ** .9)
            s = rng.uniform(.012, .03) * w * (1.6 if blur_r else 1)
            col = color_leaves[rng.integers(len(color_leaves))]
            ang = rng.uniform(0, 6.28)
            pts = []
            for k in range(5):
                th = ang + k * 6.283 / 5
                for rr, off in ((1.0, 0), (.45, 3.14 / 5)):
                    pts.append((cx + math.cos(th + off) * s * rr, cy + math.sin(th + off) * s * rr))
            d.polygon(pts, fill=rgba(col, rng.uniform(.8, 1)))
        if blur_r:
            lay = lay.filter(ImageFilter.GaussianBlur(blur_r))
        over(a, lay)
    br = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(br)
    pts = [(0, h * .1), (w * .12, h * .13), (w * .26, h * .1), (w * .38, h * .16)]
    d.line(pts, fill=(30, 20, 28, 230), width=max(3, w // 260), joint="curve")
    d.line([(w * .12, h * .13), (w * .17, h * .2), (w * .2, h * .27)], fill=(30, 20, 28, 220), width=max(2, w // 400))
    over(a, br)
    # leaves in front of the branch
    over(a, front)


# ===================================================================================================== wallpapers
def wp_underwater(w=3840, h=2160):
    a = vgrad(w, h, [(0, 0xA9E9F4), (.18, 0x4FB8DC), (.55, 0x1676B8), (1, 0x06335F)])
    rng = np.random.default_rng(11)
    glow(a, .62, -.05, .55, .35, 0xFFFFFF, .3, 1.4)
    beams(a, .55, -.05, 62, 150 * w / 1920, 0xE8FBFF, .2, rng, n=9, spread=.55)
    beams(a, .4, -.05, 58, 70 * w / 1920, 0xFFFFFF, .14, np.random.default_rng(12), n=7, spread=.6)
    bokeh(a, rng, 90, 6, 34, [0xFFFFFF, 0xCFF4FF], .05, .2, ring=.8, blur_r=1.0)
    bokeh(a, rng, 24, 40, 120, [0x9FE4FF, 0xFFFFFF], .03, .09, ring=.2, blur_r=3, region=(0, .1, 1, 1))
    # dark soft seafloor hint
    hts = ridge_heights(w, rng, h * .93, h * .035, 4, .5)
    fill_below(a, hts, 0x052A4E, .85)
    vignette(a, .2)
    grain(a, 11, 1.2 / 255)
    return a


def wp_dusk_hills(w=1920, h=1080):
    rng = np.random.default_rng(21)
    a = vgrad(w, h, [(0, 0x40306E), (.34, 0x8A5FA0), (.58, 0xE59AA4), (.78, 0xFFD2A8), (1, 0xFFE8C8)])
    glow(a, .3, .64, .35, .34, 0xFFD6A8, .38, 1.7)
    glow(a, .3, .64, .08, .12, 0xFFF0D8, .3, 1.7)
    clouds(a, rng, 7, (0, .12, 1, .45), 0xFFD7D0, .22, (.16, .02), blur_r=14)
    cols = [0xC79AB8, 0xA77AA8, 0x8566A0, 0x62508E, 0x403A78, 0x2A2A60]
    for i, c in enumerate(cols):
        hts = ridge_heights(w, rng, h * (.62 + i * .07), h * (.05 - i * .002), 5, .5, 1.0 + i * .35)
        fill_below(a, hts, c, 1.0)
    stars = rng.random((90, 2))
    for sx, sy in stars:
        if sy < .3:
            y, x = int(sy * h), int(sx * w)
            a[y:y + 2, x:x + 2] += .5 * (1 - sy / .3)
    vignette(a, .18)
    grain(a, 21, 1.2 / 255)
    return a


def wp_glass_cube(w=3840, h=2160):
    a = vgrad(w, h, [(0, 0x02142F), (.55, 0x063C78), (1, 0x0A5C9E)])
    rng = np.random.default_rng(31)
    glow(a, .5, .62, .45, .42, 0x2E9AE8, .42, 1.5)
    glow(a, .5, .95, .6, .15, 0x66C8FF, .25, 1.5)
    bokeh(a, rng, 40, 8, 40, [0x6FC4FF, 0xFFFFFF], .03, .09, ring=.3, blur_r=2)
    # floor line
    fl = np.exp(-((coords(w, h)[1] - .8) / .0012) ** 2)
    a += hexc(0x7CD0FF) * (fl * .18)[..., None]
    ww, hh = 1920, 1080
    cx, cy, s = ww * .5, hh * .46, hh * .22

    def cube(d, ss):
        top, left, right = iso_cube_faces(cx * ss, cy * ss, s * ss)
        d.polygon(left, fill=(70, 150, 230, 105))
        d.polygon(right, fill=(20, 80, 190, 125))
        d.polygon(top, fill=(190, 235, 255, 150))
        k = .8660254
        for f in (top, left, right):
            d.line(f + [f[0]], fill=(235, 250, 255, 235), width=int(2.6 * ss), joint="curve")
        d.line([(cx * ss, cy * ss), (cx * ss, (cy + s) * ss)], fill=(255, 255, 255, 235), width=int(2.6 * ss))
        # accent on the bottom-right edge (warm light bleeding through the glass)
        d.line([((cx) * ss, (cy + s) * ss), ((cx + k * s) * ss, (cy + .5 * s) * ss)], fill=(255, 120, 150, 255), width=int(5 * ss))
        # inner smaller cube for depth
        t2, l2, r2 = iso_cube_faces(cx * ss, (cy + .26 * s) * ss, s * .52 * ss)
        for f in (t2, l2, r2):
            d.line(f + [f[0]], fill=(170, 220, 255, 120), width=int(1.6 * ss), joint="curve")

    lay = layer(ww, hh, 3, cube)
    # reflection
    arr = np.asarray(lay, np.float32) / 255
    base = int((cy + s) * 1.0)
    refl = np.zeros_like(arr)
    for y in range(base, min(hh, base + int(s * 1.2))):
        sy = 2 * base - y
        if 0 <= sy < hh:
            refl[y] = arr[sy] * np.array([1, 1, 1, .22 * (1 - (y - base) / (s * 1.2))])
    rl = Image.fromarray((np.clip(refl, 0, 1) * 255).astype(np.uint8), "RGBA").filter(ImageFilter.GaussianBlur(2))
    over(a, rl)
    gl = lay.filter(ImageFilter.GaussianBlur(14))
    ga = np.asarray(gl, np.float32) / 255
    big = np.asarray(Image.fromarray((ga * 255).astype(np.uint8), "RGBA").resize((w, h), Image.BICUBIC), np.float32) / 255
    a += big[..., :3] * big[..., 3:4] * .45
    over(a, lay)
    vignette(a, .22)
    grain(a, 31, 1.2 / 255)
    return a


def wp_rain_city(w=1920, h=1080):
    rng = np.random.default_rng(41)
    a = vgrad(w, h, [(0, 0x1B2F3C), (.5, 0x2E4B57), (1, 0x15242C)])
    glow(a, .5, .35, .6, .4, 0x7FB5C4, .22, 1.4)
    warm = [0xFFB36B, 0xFF7A6B, 0xFFD98A]
    cool = [0x7FD6E8, 0x9FB8FF, 0xBFEFE8]
    bokeh(a, rng, 55, 18, 70, warm, .08, .26, ring=.25, region=(0, .3, 1, .85), blur_r=4)
    bokeh(a, rng, 45, 14, 60, cool, .06, .2, ring=.25, region=(0, .15, 1, .8), blur_r=4)
    bokeh(a, rng, 30, 6, 22, warm + cool, .1, .3, ring=.4, region=(0, .4, 1, .9), blur_r=1.5)
    # window glass: faint vertical smears, then droplets
    streak = np.zeros((h, w), np.float32)
    for _ in range(70):
        x = rng.uniform(0, w)
        y0 = rng.uniform(0, h * .8)
        ln = rng.uniform(80, 360)
        yy = np.arange(h)
        g = np.exp(-((np.arange(w) - x) / 1.4) ** 2)[None, :] * ((yy >= y0) & (yy <= y0 + ln))[:, None]
        streak += g.astype(np.float32) * .12
    a += streak[..., None] * np.array([.8, .95, 1.0], np.float32)

    def drops(d, ss):
        for _ in range(520):
            x, y = rng.uniform(0, w) * ss, rng.uniform(0, h) * ss
            r = rng.choice([3, 4, 5, 7, 10, 14], p=[.25, .25, .2, .15, .1, .05]) * ss
            rx, ry = r, r * rng.uniform(1.0, 1.35)
            d.ellipse([x - rx, y - ry, x + rx, y + ry], fill=(255, 255, 255, 20), outline=(10, 25, 35, 90), width=max(1, ss))
            d.ellipse([x - rx * .55, y - ry * .8, x - rx * .05, y - ry * .35], fill=(255, 255, 255, 150))
            d.ellipse([x + rx * .1, y + ry * .35, x + rx * .6, y + ry * .75], fill=(255, 235, 200, 60))
    over(a, layer(w, h, 2, drops))
    vignette(a, .28)
    grain(a, 41, 1.5 / 255)
    return a


def wp_warm_dunes(w=1920, h=1080):
    rng = np.random.default_rng(51)
    a = vgrad(w, h, [(0, 0xFFF0D8), (.45, 0xFFD9B0), (.8, 0xF7B88A), (1, 0xE98F72)])
    glow(a, .72, .32, .22, .3, 0xFFFFFF, .22, 1.5)
    # sun disc
    u, v = coords(w, h)
    disc = smooth((.085 - np.sqrt(((u - .72) * w / h) ** 2 + (v - .32) ** 2)) * h / 2.0)
    np.clip(a, 0, 1, out=a)
    blend(a, disc, 0xFFFFFF, .97)
    a[disc > .5] = np.minimum(a[disc > .5] + .0, 1)
    clouds(a, rng, 6, (0, .08, 1, .4), 0xFFFFFF, .35, (.16, .03), blur_r=16)
    cols = [0xF4B488, 0xE89A78, 0xD98068, 0xC4685E]
    for i, c in enumerate(cols):
        hts = ridge_heights(w, rng, h * (.62 + i * .1), h * (.06 - i * .008), 3, .45, .8 + i * .4)
        fill_below(a, hts, c)
        blend(a, np.exp(-((np.arange(h, dtype=np.float32)[:, None] - hts[None, :]) / 2.5) ** 2), 0xFFF0DC, .35)
    vignette(a, .14)
    grain(a, 51, 1.2 / 255)
    return a


def wp_alpine_lake(w=3840, h=2160):
    layers = [
        dict(base=.50, amp=.055, color=0xA9C4DC, scale=.8),
        dict(base=.55, amp=.07, color=0x86A5BE, scale=1.0, rim=0xE6F0F6),
        dict(base=.60, amp=.05, color=0x5F8396, scale=1.4),
        dict(base=.645, amp=.025, color=0x44654F, scale=2.2, octs=6),
    ]
    water = dict(y=.68, tint=0x2E6E7E, k=.4, ripple=5.0, streak=0xD8F2F8,
                 shore=[dict(base=.9, amp=.04, color=0x7A6A45, scale=1.6), dict(base=.96, amp=.02, color=0x3E3426, scale=2.5)])
    a = landscape(w, h, 61, [(0, 0x5E9AD2), (.4, 0xA9D0E8), (.65, 0xEAF5F7)], layers, water,
                  sun=(.3, .5, 0xFFF1D0, .35, .5), cloud_n=9, streaks=60)
    return a


# ===================================================================================================== photos
def photo_fuji(w=1920, h=1280):
    layers = [
        dict(base=.50, amp=.03, color=0x9A9CC4, scale=1.0),
    ]
    cone = dict(u=.56, half=.34, base=.66, height=.30, color=0x4A5486, snow=0xF0E6F2, cap=.80, shape=1.45)
    layers2 = [
        dict(base=.615, amp=.025, color=0x38426E, scale=1.6),
        dict(base=.655, amp=.02, color=0x2A3256, scale=2.4),
    ]
    water = dict(y=.675, tint=0x4F5F92, k=.38, ripple=3.5, streak=0xFFD8C0,
                 shore=[dict(base=.93, amp=.025, color=0x1B2038, scale=1.8)])
    # build in two steps so the cone sits between the far ridge and the near hills
    a = landscape(w, h, 71, [(0, 0x3A4F86), (.35, 0x8FA2CE), (.58, 0xF2C4A8), (.68, 0xFFDDBB)], layers, None,
                  sun=(.74, .6, 0xFFB890, .6, .35), cloud_n=5, cone=cone)
    rng = np.random.default_rng(72)
    for L in layers2:
        hts = ridge_heights(w, rng, L["base"] * h, L["amp"] * h, 5, .5, L["scale"])
        fill_below(a, hts, L["color"])
    reflect(a, int(.675 * h), water["tint"], water["k"], water["ripple"], 73)
    Y, X = np.mgrid[0:h, 0:w].astype(np.float32)
    for _ in range(40):
        sx, sy = rng.uniform(.2, .9) * w, rng.uniform(.7, .92) * h
        ln = rng.uniform(.04, .12) * w
        a += hexc(0xFFD0B8) * (np.exp(-(((X - sx) / ln) ** 2 + ((Y - sy) / (h * .0015)) ** 2)) * .22)[..., None]
    for L in water["shore"]:
        hts = ridge_heights(w, rng, L["base"] * h, L["amp"] * h, 5, .55, L["scale"])
        fill_below(a, hts, L["color"])
    maple_branch(a, 74, [0xC63A2E, 0xE0553A, 0xB02A30, 0xF07A44])
    vignette(a, .2)
    grain(a, 75, 1.3 / 255)
    return a


def photo_evening(w=1280, h=1920):
    rng = np.random.default_rng(81)
    a = vgrad(w, h, [(0, 0x0A0D2A), (.35, 0x241C4E), (.55, 0x7A3A6A), (.62, 0xE0708A)])
    yh = int(h * .62)
    glow(a, .5, .6, .5, .12, 0xFFB080, .35, 1.6)
    img = to_img(a)
    d = ImageDraw.Draw(img)
    depths = [(0x2B2150, .46, 4, 90), (0x1C1640, .38, 4, 110), (0x110E2E, .3, 3, 150)]
    for col, hmax, n, bw in depths:
        x = -20
        while x < w:
            bwid = int(rng.uniform(.7, 1.6) * bw * (1.6 if col == 0x110E2E else 1))
            bh = int(rng.uniform(.35, 1.0) * hmax * h)
            top = yh - bh
            d.rectangle([x, top, x + bwid, yh], fill=rgb255(col))
            # lit windows
            wc = 0x110E2E
            for wy in range(top + 14, yh - 10, 22):
                for wx in range(x + 8, x + bwid - 10, 18):
                    if rng.random() < .22:
                        c = [0xFFD27A, 0xFFB35E, 0xBFE6FF][rng.integers(3)]
                        d.rectangle([wx, wy, wx + 7, wy + 11], fill=rgb255(c))
            x += bwid + int(rng.uniform(0, 8))
    # neon signs (plain shapes, no text)
    glow_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow_layer)
    for sx, col, hh in [(150, 0xFF4F9A, 360), (360, 0x59D8FF, 240), (870, 0xFF8A4F, 420), (1090, 0xB07AFF, 300), (620, 0x59FFB4, 200)]:
        top = yh - 520 - rng.integers(0, 120)
        gd.rounded_rectangle([sx, top, sx + 34, top + hh], radius=6, fill=rgba(col, .95))
        for k in range(4):
            yy = top + 28 + k * hh // 5
            gd.rounded_rectangle([sx + 8, yy, sx + 26, yy + 12], radius=3, fill=rgba(0xFFFFFF, .85))
    sharp = glow_layer.copy()
    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(26))
    a = to_arr(img)
    over(a, glow_layer)
    over(a, glow_layer)
    over(a, sharp)
    # road
    road = vgrad(w, h, [(0, 0), (1, 1)])
    Y = np.arange(h, dtype=np.float32)[:, None]
    m = np.clip(Y - yh + .5, 0, 1)
    blend(a, np.repeat(m, w, 1), 0x0B0A1C, 1.0)
    reflect(a, yh, 0x120E2A, .38, 4.5, 82)
    # wet-road colour streaks of lamps and cars
    X = np.arange(w, dtype=np.float32)[None, :]
    for _ in range(26):
        sx = rng.uniform(.05, .95) * w
        col = [0xFF6AA8, 0x6FD8FF, 0xFFB070, 0xFFFFFF][rng.integers(4)]
        top = yh + 20
        ln = rng.uniform(.06, .18) * h
        wd = rng.uniform(5, 16)
        g = np.exp(-((X - sx) / wd) ** 2) * np.clip(1 - (Y - top) / ln, 0, 1) * (Y > top)
        a += hexc(col) * (g * rng.uniform(.18, .4))[..., None]
    # perspective lane markings
    vx = w * .5
    lanes = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ld = ImageDraw.Draw(lanes)
    for k in range(9):
        t0, t1 = k / 9, (k + .5) / 9
        y0 = yh + (h - yh) * t0 ** 2
        y1 = yh + (h - yh) * t1 ** 2
        wd0, wd1 = 2 + 18 * t0 ** 2, 2 + 18 * t1 ** 2
        ld.polygon([(vx - wd0, y0), (vx + wd0, y0), (vx + wd1, y1), (vx - wd1, y1)], fill=(255, 235, 200, 70))
    over(a, lanes.filter(ImageFilter.GaussianBlur(1.5)))
    bokeh(a, rng, 55, 10, 46, [0xFF6AA8, 0x6FD8FF, 0xFFB070, 0xFFE2A0], .05, .22, ring=.2, region=(0, .5, 1, .9), blur_r=3, work=1280)
    vignette(a, .35)
    grain(a, 81, 2.0 / 255)
    return a


def photo_focused(w=1920, h=1280):
    ss = 2
    W, H = w * ss, h * ss
    img = Image.new("RGB", (W, H), (0, 0, 0))
    wall = to_img(dgrad(W, H, 0xA8C8BC, 0x7DA396, 80))
    img.paste(wall)
    d = ImageDraw.Draw(img)
    s = ss
    # window with warm light
    d.rounded_rectangle([140 * s, 90 * s, 560 * s, 560 * s], radius=10 * s, fill=(238, 247, 242), outline=(214, 226, 220), width=10 * s)
    d.line([(350 * s, 90 * s), (350 * s, 560 * s)], fill=(214, 226, 220), width=8 * s)
    d.line([(140 * s, 325 * s), (560 * s, 325 * s)], fill=(214, 226, 220), width=8 * s)
    # shelf
    for sy in (330, 570):
        d.rounded_rectangle([760 * s, sy * s, 1760 * s, (sy + 22) * s], radius=6 * s, fill=(196, 150, 104))
    books = [(0xC5604F, 58), (0xF0C468, 44), (0x4F7FA6, 66), (0xEFE9DD, 40), (0x7D9E6C, 52)]
    bx = 790
    for c, bw in books:
        bh = 140 + (bw % 30) * 2
        d.rectangle([bx * s, (330 - bh) * s, (bx + bw) * s, 330 * s], fill=rgb255(c))
        d.rectangle([(bx + 6) * s, (330 - bh + 14) * s, (bx + bw - 6) * s, (330 - bh + 20) * s], fill=(255, 255, 255))
        bx += bw + 6
    def plant(cx, base, scale, leaf=(88, 150, 96), pot=(206, 120, 88)):
        d.polygon([((cx - 46 * scale) * s, (base - 78 * scale) * s), ((cx + 46 * scale) * s, (base - 78 * scale) * s),
                   ((cx + 34 * scale) * s, base * s), ((cx - 34 * scale) * s, base * s)], fill=pot)
        for k in range(9):
            ang = math.radians(-90 + (k - 4) * 19)
            ln = (110 + (k % 3) * 26) * scale
            ex, ey = cx + math.sin(ang + math.pi / 2 + math.pi / 2) * 0, 0
            tx, ty = cx + math.cos(ang) * ln, (base - 78 * scale) + math.sin(ang) * ln
            mx, my = (cx + tx) / 2, (base - 78 * scale + ty) / 2
            nx, ny = -(ty - (base - 78 * scale)), tx - cx
            nn = math.hypot(nx, ny) or 1
            wdt = 22 * scale
            d.polygon([(cx * s, (base - 78 * scale) * s), ((mx + nx / nn * wdt) * s, (my + ny / nn * wdt) * s), (tx * s, ty * s),
                       ((mx - nx / nn * wdt) * s, (my - ny / nn * wdt) * s)], fill=leaf)
    plant(1000, 330, 1.0)
    plant(1560, 330, .85, (112, 168, 110), (228, 176, 112))
    plant(1250, 570, .7, (96, 160, 128), (180, 120, 150))
    # framed picture
    d.rounded_rectangle([1320 * s, 360 * s, 1720 * s, 540 * s], radius=6 * s, fill=(241, 235, 222), outline=(176, 130, 90), width=8 * s)
    d.polygon([(1340 * s, 530 * s), (1450 * s, 440 * s), (1520 * s, 500 * s), (1580 * s, 420 * s), (1700 * s, 530 * s)], fill=(124, 170, 150))
    d.ellipse([1620 * s, 385 * s, 1660 * s, 425 * s], fill=(240, 190, 90))
    # desk
    desk = to_img(dgrad(W, 420 * s, 0xD8A878, 0xB88556, 90))
    img.paste(desk, (0, 860 * s))
    d.rectangle([0, 850 * s, W, 866 * s], fill=(224, 186, 142))
    # notebook + pen
    d.polygon([(560 * s, 930 * s), (1160 * s, 920 * s), (1260 * s, 1130 * s), (620 * s, 1150 * s)], fill=(250, 246, 236), outline=(222, 212, 196))
    for k in range(7):
        y = 960 + k * 26
        d.line([(610 * s, (y + 6) * s), (1180 * s, (y - 8 + k * 6) * s)], fill=(210, 220, 232), width=2 * s)
    d.line([(1090 * s, 1010 * s), (1270 * s, 905 * s)], fill=(72, 96, 140), width=9 * s)
    # mug with steam
    d.rounded_rectangle([1360 * s, 930 * s, 1500 * s, 1060 * s], radius=22 * s, fill=(245, 245, 240))
    d.ellipse([1480 * s, 955 * s, 1550 * s, 1030 * s], outline=(245, 245, 240), width=12 * s)
    d.ellipse([1368 * s, 936 * s, 1492 * s, 962 * s], fill=(120, 76, 56))
    # vase with flowers
    d.polygon([(1620 * s, 900 * s), (1700 * s, 900 * s), (1712 * s, 1060 * s), (1608 * s, 1060 * s)], fill=(186, 214, 236))
    for k, (fx, fy, c) in enumerate([(1630, 790, (240, 140, 170)), (1672, 760, (248, 176, 196)), (1706, 800, (236, 120, 156)), (1650, 840, (252, 200, 214))]):
        d.line([(1660 * s, 900 * s), (fx * s, fy * s)], fill=(88, 140, 92), width=5 * s)
        for p in range(6):
            a_ = p * math.pi / 3
            d.ellipse([(fx + math.cos(a_) * 20 - 14) * s, (fy + math.sin(a_) * 20 - 14) * s, (fx + math.cos(a_) * 20 + 14) * s, (fy + math.sin(a_) * 20 + 14) * s], fill=c)
        d.ellipse([(fx - 9) * s, (fy - 9) * s, (fx + 9) * s, (fy + 9) * s], fill=(250, 224, 130))
    # lamp
    d.line([(300 * s, 1130 * s), (300 * s, 900 * s), (420 * s, 760 * s)], fill=(70, 80, 96), width=10 * s)
    d.ellipse([240 * s, 1110 * s, 360 * s, 1150 * s], fill=(70, 80, 96))
    d.polygon([(390 * s, 720 * s), (500 * s, 760 * s), (470 * s, 820 * s), (368 * s, 786 * s)], fill=(238, 196, 90))
    im = img.resize((w, h), Image.LANCZOS)
    a = to_arr(im)
    glow(a, .18, .3, .22, .35, 0xFFF4D8, .22, 1.5)
    glow(a, .25, .6, .3, .25, 0xFFE0A0, .12, 1.5)
    vignette(a, .25)
    grain(a, 91, 1.2 / 255)
    return a


def photo_foliage(w=1920, h=1280):
    rng = np.random.default_rng(101)
    a = vgrad(w, h, [(0, 0x264B2C), (1, 0x0F2414)])
    glow(a, .72, .12, .35, .3, 0xD8F0A0, .5, 1.5)
    greens = [0x2E6B34, 0x3F8A3E, 0x1F5028, 0x56A04A, 0x7DBD5A, 0x194A28]

    def leaves(n, smin, smax, blur_r, alpha):
        lay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        d = ImageDraw.Draw(lay)
        for _ in range(n):
            cx, cy = rng.uniform(-.05, 1.05) * w, rng.uniform(-.05, 1.05) * h
            ln = rng.uniform(smin, smax)
            wd = ln * rng.uniform(.22, .38)
            ang = rng.uniform(0, 6.283)
            col = greens[rng.integers(len(greens))]
            # sun-lit: lighten leaves near the top-right
            lit = max(0, 1 - math.hypot(cx / w - .72, cy / h - .12) * 1.5)
            c = hexc(col) * (1 - .3 * lit) + hexc(0xE6F6A8) * .3 * lit
            pts = []
            for k in range(13):
                t = k / 12
                pts.append((t * ln, math.sin(t * math.pi) * wd))
            pts += [(t_, -y_) for t_, y_ in reversed(pts)]
            ca, sa = math.cos(ang), math.sin(ang)
            poly = [(cx + (x - ln / 2) * ca - y * sa, cy + (x - ln / 2) * sa + y * ca) for x, y in pts]
            d.polygon(poly, fill=rgb255(c) + (int(255 * alpha),))
            d.line([(cx - ln / 2 * ca, cy - ln / 2 * sa), (cx + ln / 2 * ca, cy + ln / 2 * sa)], fill=rgb255(c * .7) + (int(200 * alpha),), width=2)
        return lay.filter(ImageFilter.GaussianBlur(blur_r)) if blur_r else lay

    over(a, leaves(500, 160, 330, 9, 1.0))
    over(a, leaves(700, 90, 200, 3, 1.0))
    bokeh(a, rng, 30, 20, 70, [0xE8F6B0, 0xFFFFFF], .05, .16, ring=.2, region=(.3, 0, 1, .5), blur_r=4)
    over(a, leaves(260, 120, 260, 0, 1.0))
    # light shaft
    beams(a, .75, 0, 70, 120, 0xFFF6C8, .13, rng, n=4, spread=.2)
    vignette(a, .3)
    grain(a, 101, 1.5 / 255)
    return a


def photo_brick_poster(w=1280, h=1352):
    ss = 2
    W, H = w * ss, h * ss
    rng = np.random.default_rng(111)
    img = Image.new("RGB", (W, H), (226, 222, 216))
    d = ImageDraw.Draw(img)
    bw, bh, mt = 124 * ss, 48 * ss, 5 * ss
    row = 0
    for y in range(-bh, H + bh, bh + mt):
        off = (bw // 2 + mt // 2) if row % 2 else 0
        for x in range(-bw + off, W + bw, bw + mt):
            v = rng.uniform(-8, 8)
            col = (int(238 + v), int(235 + v), int(228 + v))
            d.rectangle([x, y, x + bw, y + bh], fill=col)
            d.line([(x, y + bh), (x + bw, y + bh)], fill=(210, 205, 196), width=2 * ss)
        row += 1
    # poster with frame
    px0, py0, px1, py1 = 340 * ss, 250 * ss, 940 * ss, 1010 * ss
    d.rectangle([px0 + 16 * ss, py0 + 22 * ss, px1 + 16 * ss, py1 + 22 * ss], fill=(170, 164, 156))  # hard-ish shadow base
    d.rectangle([px0 - 22 * ss, py0 - 22 * ss, px1 + 22 * ss, py1 + 22 * ss], fill=(70, 54, 44))
    d.rectangle([px0, py0, px1, py1], fill=(248, 240, 222))
    cxm = (px0 + px1) // 2
    d.ellipse([cxm - 190 * ss, py0 + 90 * ss, cxm + 190 * ss, py0 + 470 * ss], fill=(240, 130, 80))
    d.pieslice([px0 + 40 * ss, py0 + 400 * ss, px0 + 300 * ss, py0 + 660 * ss], 180, 360, fill=(70, 130, 190))
    d.polygon([(px0 + 300 * ss, py1 - 40 * ss), (px0 + 440 * ss, py0 + 470 * ss), (px1 - 40 * ss, py1 - 40 * ss)], fill=(60, 160, 150))
    d.rectangle([px0 + 40 * ss, py1 - 100 * ss, px1 - 40 * ss, py1 - 80 * ss], fill=(60, 50, 60))
    for k in range(4):
        d.rectangle([px0 + 40 * ss, py0 + 40 * ss + k * 14 * ss, px0 + 40 * ss + (180 - k * 36) * ss, py0 + 48 * ss + k * 14 * ss], fill=(60, 50, 60))
    d.ellipse([px1 - 140 * ss, py0 + 40 * ss, px1 - 60 * ss, py0 + 120 * ss], fill=(252, 200, 80))
    im = img.resize((w, h), Image.LANCZOS)
    a = to_arr(im)
    # leaf shadows from the top-right corner
    sh = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    sd = ImageDraw.Draw(sh)
    for _ in range(26):
        cx, cy = w - rng.uniform(0, 380) * rng.random(), rng.uniform(0, 520) * rng.random()
        ln = rng.uniform(120, 240)
        ang = rng.uniform(1.6, 3.4)
        wd = ln * .26
        pts = [(t * ln, math.sin(t * math.pi) * wd) for t in np.linspace(0, 1, 11)]
        pts += [(x_, -y_) for x_, y_ in reversed(pts)]
        ca, sa = math.cos(ang), math.sin(ang)
        sd.polygon([(cx + x * ca - y * sa, cy + x * sa + y * ca) for x, y in pts], fill=(30, 40, 50, 80))
    over(a, sh.filter(ImageFilter.GaussianBlur(7)))
    glow(a, .85, .05, .6, .5, 0xFFF0D0, .22, 1.3)
    vignette(a, .22)
    grain(a, 111, 1.2 / 255)
    return a


# ===================================================================================================== website pictures
def site_meadow(w=720, h=718):
    ss = 2
    W, H = w * ss, h * ss
    rng = np.random.default_rng(121)
    a = vgrad(W, H, [(0, 0x9ED8F2), (.55, 0xE4F6F6), (1, 0xFFF3DC)])
    glow(a, .75, .22, .2, .2, 0xFFF4C0, .8, 1.5)
    img = to_img(a)
    d = ImageDraw.Draw(img)
    d.ellipse([W * .75 - 70 * ss, H * .22 - 70 * ss, W * .75 + 70 * ss, H * .22 + 70 * ss], fill=(255, 236, 160))
    for cx, cy, sc in [(.22, .17, 1), (.55, .3, .7), (.88, .42, .6)]:
        for ox, oy, r in [(-50, 0, 36), (0, -14, 46), (52, 0, 34), (10, 12, 40)]:
            d.ellipse([cx * W + (ox - r * sc) * ss * sc, cy * H + (oy - r * .75) * ss * sc, cx * W + (ox + r * sc) * ss * sc, cy * H + (oy + r) * ss * sc * .8], fill=(255, 255, 255))
    for base, col, amp in [(.62, (150, 206, 142), 40), (.72, (118, 188, 112), 50), (.84, (88, 166, 90), 40)]:
        pts = [(0, H)]
        ph = rng.uniform(0, 6)
        for x in range(0, W + 8, 8):
            pts.append((x, H * base + math.sin(x / W * 5 + ph) * amp * ss + math.sin(x / W * 11 + ph * 2) * amp * .3 * ss))
        pts.append((W, H))
        d.polygon(pts, fill=col)
        if base < .8:
            for k in range(3):
                tx = rng.uniform(.1, .9) * W
                ty = H * base + math.sin(tx / W * 5 + ph) * amp * ss + 6 * ss
                d.rectangle([tx - 4 * ss, ty - 30 * ss, tx + 4 * ss, ty + 4 * ss], fill=(120, 84, 60))
                d.ellipse([tx - 30 * ss, ty - 80 * ss, tx + 30 * ss, ty - 20 * ss], fill=(72, 160, 84))
    for _ in range(90):
        fx, fy = rng.uniform(0, W), rng.uniform(.78, .98) * H
        c = [(255, 120, 150), (255, 220, 90), (255, 255, 255), (170, 130, 255)][rng.integers(4)]
        r = rng.uniform(3, 6) * ss
        d.ellipse([fx - r, fy - r, fx + r, fy + r], fill=c)
    a = to_arr(img.resize((w, h), Image.LANCZOS))
    grain(a, 121, 1.0 / 255)
    return a


def site_beach(w=572, h=440):
    ss = 2
    W, H = w * ss, h * ss
    a = vgrad(W, H, [(0, 0xFFC8A0), (.3, 0xFFE6C8), (.45, 0xBFE4F0), (.5, 0x66B8D8), (.8, 0x2E8FB8), (1, 0xF4DDB4)])
    img = to_img(a)
    d = ImageDraw.Draw(img)
    d.ellipse([W * .3 - 50 * ss, H * .3 - 50 * ss, W * .3 + 50 * ss, H * .3 + 50 * ss], fill=(255, 244, 214))
    d.rectangle([0, int(H * .48), W, int(H * .5)], fill=(235, 248, 250))
    for k in range(6):
        y = H * (.54 + k * .045)
        d.line([(W * (.1 + (k % 3) * .25), y), (W * (.3 + (k % 3) * .25), y)], fill=(210, 240, 250), width=2 * ss)
    # sun reflection
    for k in range(7):
        y = H * (.52 + k * .04)
        ln = (60 - k * 6) * ss
        d.rounded_rectangle([W * .3 - ln, y, W * .3 + ln, y + 4 * ss], radius=2 * ss, fill=(255, 234, 190))
    # sailboat
    bx, by = W * .66, H * .56
    d.polygon([(bx - 60 * ss, by), (bx + 60 * ss, by), (bx + 40 * ss, by + 20 * ss), (bx - 40 * ss, by + 20 * ss)], fill=(250, 250, 248))
    d.line([(bx, by), (bx, by - 120 * ss)], fill=(120, 96, 84), width=4 * ss)
    d.polygon([(bx + 4 * ss, by - 120 * ss), (bx + 4 * ss, by - 8 * ss), (bx + 64 * ss, by - 8 * ss)], fill=(255, 120, 110))
    d.polygon([(bx - 4 * ss, by - 100 * ss), (bx - 4 * ss, by - 8 * ss), (bx - 48 * ss, by - 8 * ss)], fill=(255, 255, 255))
    # sand
    pts = [(0, H), (0, H * .86)]
    for x in range(0, W + 8, 8):
        pts.append((x, H * .83 + math.sin(x / W * 4 + 1) * 14 * ss))
    pts.append((W, H))
    d.polygon(pts, fill=(244, 222, 178))
    d.line(pts[1:-1], fill=(255, 255, 255), width=4 * ss)
    a = to_arr(img.resize((w, h), Image.LANCZOS))
    grain(a, 131, 1.0 / 255)
    return a


def site_balloons(w=518, h=767):
    ss = 2
    W, H = w * ss, h * ss
    rng = np.random.default_rng(141)
    a = vgrad(W, H, [(0, 0x7E9BE8), (.4, 0xC7A8E8), (.75, 0xFFC4C0), (1, 0xFFE6C4)])
    glow(a, .5, .8, .5, .25, 0xFFF0C8, .5, 1.5)
    clouds(a, rng, 6, (0, .55, 1, .95), 0xFFFFFF, .35, (.22, .035), blur_r=22)
    img = to_img(a)
    d = ImageDraw.Draw(img)

    def balloon(cx, cy, r, cols):
        ry = r * 1.18
        d.ellipse([cx - r, cy - ry, cx + r, cy + ry * .9], fill=cols[0])
        d.ellipse([cx - r * .62, cy - ry, cx + r * .62, cy + ry * .9], fill=cols[1])
        d.ellipse([cx - r * .24, cy - ry, cx + r * .24, cy + ry * .9], fill=cols[0])
        d.ellipse([cx - r, cy - ry, cx + r, cy + ry * .9], outline=cols[0], width=max(1, int(r * .04)))
        d.polygon([(cx - r * .18, cy + ry * .88), (cx + r * .18, cy + ry * .88), (cx + r * .1, cy + ry * 1.12), (cx - r * .1, cy + ry * 1.12)], fill=cols[0])
        d.line([(cx - r * .1, cy + ry * 1.1), (cx - r * .14, cy + ry * 1.45)], fill=(90, 70, 70), width=2 * ss)
        d.line([(cx + r * .1, cy + ry * 1.1), (cx + r * .14, cy + ry * 1.45)], fill=(90, 70, 70), width=2 * ss)
        d.rounded_rectangle([cx - r * .22, cy + ry * 1.45, cx + r * .22, cy + ry * 1.7], radius=4 * ss, fill=(150, 104, 76))

    balloon(W * .34, H * .3, 120 * ss, [(255, 110, 110), (255, 232, 200)])
    balloon(W * .72, H * .2, 70 * ss, [(90, 170, 240), (255, 255, 255)])
    balloon(W * .78, H * .5, 55 * ss, [(255, 200, 80), (255, 140, 90)])
    balloon(W * .2, H * .64, 40 * ss, [(120, 210, 170), (255, 255, 255)])
    a = to_arr(img.resize((w, h), Image.LANCZOS))
    grain(a, 141, 1.0 / 255)
    return a


def video_thumb_meadow(w=1280, h=720):
    rng = np.random.default_rng(151)
    a = vgrad(w, h, [(0, 0xA4DCF4), (.45, 0xDFF3F0), (.5, 0xB9E08A), (1, 0x4E9A3E)])
    glow(a, .22, .2, .2, .3, 0xFFF4C0, .3, 1.5)
    bokeh(a, rng, 60, 10, 55, [0xFFFFFF, 0xEFFFB0, 0xFFE9A0], .05, .2, ring=.2, region=(0, .1, 1, .8), blur_r=3, work=1280)
    for i, c in enumerate([0xA6D676, 0x7EBC5A, 0x58A046, 0x3C8636]):
        hts = ridge_heights(w, rng, h * (.55 + i * .1), h * .05, 3, .5, 1 + i * .5)
        fill_below(a, hts, c)
    # big friendly sun
    u, v = coords(w, h)
    disc = smooth((.08 - np.sqrt(((u - .22) * w / h) ** 2 + (v - .2) ** 2)) * h / 2)
    np.clip(a, 0, 1, out=a)
    blend(a, disc, 0xFFFDF0, .97)
    vignette(a, .2)
    grain(a, 151, 1.0 / 255)
    return a


# ===================================================================================================== game hub banners
def banner_3d(w=1920, h=1080):
    a = vgrad(w, h, [(0, 0x0B0824), (.5, 0x2A1058), (.62, 0x6A1F7A), (.63, 0x120A30), (1, 0x0A0620)])
    rng = np.random.default_rng(161)
    glow(a, .5, .55, .35, .3, 0xC43CC8, .5, 1.6)
    glow(a, .2, .3, .3, .3, 0x3A3CE8, .3, 1.5)
    glow(a, .85, .25, .25, .3, 0xE83C9A, .25, 1.5)
    bokeh(a, rng, 40, 10, 60, [0xFF5FD0, 0x5F8FFF, 0x9F6FFF], .04, .14, ring=.3, blur_r=3)
    vx, vy = w * .5, h * .63

    def grid(d, ss):
        for xx in np.linspace(-1.6 * w, 2.6 * w, 44):
            d.line([(vx * ss, vy * ss), (xx * ss, h * ss)], fill=(110, 90, 255, 150), width=2 * ss)
        for k in range(1, 15):
            t = (k / 14) ** 2.2
            y = vy + (h - vy) * t
            d.line([(0, y * ss), (w * ss, y * ss)], fill=(255, 90, 210, int(60 + 160 * t)), width=2 * ss)
        d.line([(0, vy * ss), (w * ss, vy * ss)], fill=(255, 150, 240, 230), width=3 * ss)
    g = layer(w, h, 1, grid)
    over(a, g.filter(ImageFilter.GaussianBlur(5)))
    over(a, g)
    cx, cy, s = w * .5, h * .5, h * .21

    def cube(d, ss):
        top, left, right = iso_cube_faces(cx * ss, cy * ss, s * ss)
        d.polygon(left, fill=(70, 70, 230, 235))
        d.polygon(right, fill=(200, 50, 190, 235))
        d.polygon(top, fill=(130, 190, 255, 240))
        for f, c in ((top, (200, 240, 255, 255)), (left, (160, 170, 255, 255)), (right, (255, 150, 235, 255))):
            d.line(f + [f[0]], fill=c, width=int(5 * ss), joint="curve")
        d.line([(cx * ss, cy * ss), (cx * ss, (cy + s) * ss)], fill=(255, 255, 255, 255), width=int(5 * ss))
    c = layer(w, h, 2, cube)
    # floor reflection
    arr = np.asarray(c, np.float32) / 255
    base = int(cy + s)
    refl = np.zeros_like(arr)
    for y in range(base, min(h, base + int(s * 1.1))):
        sy = 2 * base - y
        if 0 <= sy < h:
            refl[y] = arr[sy] * np.array([1, 1, 1, .3 * (1 - (y - base) / (s * 1.1))])
    over(a, Image.fromarray((np.clip(refl, 0, 1) * 255).astype(np.uint8), "RGBA").filter(ImageFilter.GaussianBlur(3)))
    over(a, c.filter(ImageFilter.GaussianBlur(26)))
    over(a, c.filter(ImageFilter.GaussianBlur(10)))
    over(a, c)
    vignette(a, .3)
    grain(a, 161, 1.5 / 255)
    return a


def banner_empty(w=1920, h=1080):
    a = vgrad(w, h, [(0, 0x0A0E1C), (1, 0x120A22)])
    glow(a, .66, .5, .4, .5, 0x1F8F9A, .35, 1.4)
    glow(a, .9, .8, .25, .3, 0x7A3CC0, .25, 1.4)
    cx, cy = w * .66, h * .5

    def tunnel(d, ss):
        n = 70
        for i in range(n):
            r = h * .5 * math.exp(-i * .06)
            ang = i * .075
            pts = [(cx + math.cos(ang + k * math.pi / 2 + math.pi / 4) * r, cy + math.sin(ang + k * math.pi / 2 + math.pi / 4) * r) for k in range(4)]
            t = i / n
            col = (int(60 + 180 * t), int(210 - 130 * t), int(220 - 20 * t), int(200 - 80 * t))
            d.line([(x * ss, y * ss) for x, y in pts + [pts[0]]], fill=col, width=max(1, int((3.0 - 2.4 * t) * ss)), joint="curve")
    t = layer(w, h, 2, tunnel)
    over(a, t.filter(ImageFilter.GaussianBlur(5)))
    over(a, t)
    # fade the tunnel out toward the left for a dark title side
    u, _v = coords(w, h)
    a *= (.45 + .55 * smooth((u - .05) / .5))[..., None]
    vignette(a, .3)
    grain(a, 171, 1.5 / 255)
    return a


def banner_tictactoe(w=1920, h=1080):
    rng = np.random.default_rng(181)
    a = vgrad(w, h, [(0, 0x2F383C), (1, 0x252C30)])
    glow(a, .5, .45, .6, .6, 0x5A6A70, .35, 1.2)
    # eraser smudges
    sm = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(sm)
    for _ in range(26):
        x, y = rng.uniform(0, w), rng.uniform(0, h)
        d.ellipse([x - rng.uniform(80, 280), y - rng.uniform(20, 70), x + rng.uniform(80, 280), y + rng.uniform(20, 70)], fill=(230, 240, 240, int(rng.uniform(8, 22))))
    over(a, sm.filter(ImageFilter.GaussianBlur(30)))
    # chalk strokes
    chalk = Image.new("L", (w, h), 0)
    cd = ImageDraw.Draw(chalk)

    def stroke(p0, p1, wd=9, jitter=2.5, passes=2):
        for _ in range(passes):
            n = 40
            ptsl = []
            for k in range(n + 1):
                t = k / n
                x = p0[0] + (p1[0] - p0[0]) * t + rng.normal(0, jitter)
                y = p0[1] + (p1[1] - p0[1]) * t + rng.normal(0, jitter)
                ptsl.append((x, y))
            cd.line(ptsl, fill=int(rng.uniform(190, 250)), width=int(wd), joint="curve")

    cx, cy, cs = w * .5, h * .5, 250
    for k in (-.5, .5):
        stroke((cx + k * cs, cy - 1.5 * cs - 20), (cx + k * cs + 6, cy + 1.5 * cs + 20), 10)
        stroke((cx - 1.5 * cs - 20, cy + k * cs), (cx + 1.5 * cs + 20, cy + k * cs - 6), 10)

    def cell(i, j):
        return cx + (i - 1) * cs, cy + (j - 1) * cs

    def X(i, j):
        x, y = cell(i, j)
        r = cs * .3
        stroke((x - r, y - r), (x + r, y + r), 11, 2)
        stroke((x + r, y - r), (x - r, y + r), 11, 2)

    def O(i, j):
        x, y = cell(i, j)
        r = cs * .3
        for _ in range(2):
            pts = []
            ph = rng.uniform(0, .4)
            for k in range(60):
                t = ph + k / 58 * 6.4
                rr = r + rng.normal(0, 2) + k * .03
                pts.append((x + math.cos(t) * rr, y + math.sin(t) * rr))
            cd.line(pts, fill=int(rng.uniform(190, 250)), width=11, joint="curve")

    X(0, 0); O(1, 0); X(2, 0)
    O(0, 1); X(1, 1); O(2, 1)
    O(0, 2); X(2, 2)
    # winning diagonal in a warm chalk
    win = Image.new("L", (w, h), 0)
    cd2 = chalk
    cm = np.asarray(chalk, np.float32) / 255
    # grainy chalk: multiply by noise
    grn = np.random.default_rng(182).random((h, w)).astype(np.float32)
    cm = cm * np.clip((grn - .12) * 1.5, 0, 1)
    cm = np.asarray(blur(Image.fromarray((cm * 255).astype(np.uint8)), .8), np.float32) / 255
    blend(a, cm, 0xF4F4EC, .92)
    sc = Image.new("L", (w, h), 0)
    sd = ImageDraw.Draw(sc)
    p0, p1 = cell(0, 0), cell(2, 2)
    pts = [(p0[0] - 120 + (p1[0] - p0[0] + 240) * k / 40 + rng.normal(0, 2), p0[1] - 120 + (p1[1] - p0[1] + 240) * k / 40 + rng.normal(0, 2)) for k in range(41)]
    sd.line(pts, fill=230, width=9)
    scm = np.asarray(sc, np.float32) / 255 * np.clip((grn - .1) * 1.5, 0, 1)
    blend(a, scm, 0xFFD36A, .9)
    # chalk dust along the bottom ledge
    ledge = np.zeros((h, w), np.float32)
    ledge[int(h * .94):, :] = 1
    blend(a, ledge, 0x161A1D, .6)
    for k in range(7):
        x = rng.uniform(.1, .9) * w
        blend(a, np.exp(-(((coords(w, h)[0] * w - x) / 60) ** 2 + ((coords(w, h)[1] * h - h * .955) / 7) ** 2)), 0xE8EEE8, .35)
    grain(a, 183, 3.0 / 255)
    vignette(a, .35)
    return a


# ===================================================================================================== video frames
def waves_frame(w, h, p):
    """One frame of the loopable wave animation; p in [0, 1) is the loop phase."""
    a = vgrad(w, h, [(0, 0x3C84C6), (.55, 0x8EC6EC), (1, 0xDEEFFB)])
    glow(a, .72, .28, .5, .45, 0xFFFFFF, .22, 1.4)
    glow(a, .15, .12, .35, .3, 0x9FE2FF, .14, 1.4)
    u, v = coords(w, h)
    tp = 2 * math.pi
    spec = [  # base, amp, k, f, phase, crest colour, deep colour, alpha
        (.44, .045, 1.3, 1, 0.3, 0xD8F1FF, 0x9ED2F2, .55),
        (.52, .055, 1.0, -1, 1.7, 0xC2E8FF, 0x7FBDE8, .6),
        (.61, .060, 1.6, 2, 2.9, 0xA8DCFF, 0x5FA8E0, .65),
        (.71, .065, 1.2, -1, 4.1, 0x84C6F6, 0x3E86CE, .72),
        (.82, .07, 1.8, 1, 5.3, 0x5AA4E8, 0x2563B4, .8),
        (.93, .06, 1.1, -2, 0.9, 0x3F86D8, 0x174C98, .9),
    ]
    for base, amp, k, f, ph, ctop, cdeep, al in spec:
        y = base + amp * np.sin(tp * (k * u + f * p) + ph) + .42 * amp * np.sin(tp * (2.3 * k * u - (f + 1) * p) + ph * 1.7)
        d = (v - y) * h  # px below the crest
        m = smooth(d / 1.6 + .5)
        t = smooth(d / (h * .22))[..., None]
        col = hexc(ctop) * (1 - t) + hexc(cdeep) * t
        a += (col - a) * (m * al)[..., None]
        a += np.array([1, 1, 1], np.float32) * (np.exp(-(d / (h * .006)) ** 2) * .30 * al)[..., None] * (d > -4)[..., None]
    vignette(a, .12)
    return a
