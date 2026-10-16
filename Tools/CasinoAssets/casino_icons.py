"""Draws the 888 casino's icons (site logo, the four game tiles, a chip) as 256 px transparent PNGs."""
import math, sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter

out = sys.argv[1]
S = 4                     # supersampling
N = 256 * S
GOLD = (236, 190, 86, 255); GOLD_DARK = (150, 104, 30, 255)
BURGUNDY = (110, 18, 44, 255); RED = (204, 30, 46, 255); BLACK = (24, 22, 28, 255)
GREEN = (20, 128, 70, 255); WHITE = (250, 247, 240, 255); FELT = (16, 92, 60, 255)
BLACK_FONT = "/System/Library/Fonts/Supplemental/Arial Black.ttf"
GEORGIA = "/System/Library/Fonts/Supplemental/Georgia Bold.ttf"


def canvas():
    return Image.new("RGBA", (N, N), (0, 0, 0, 0))


def font(path, px):
    return ImageFont.truetype(path, int(px))


def shadow(im, radius=10, offset=(0, 10), alpha=110):
    a = im.split()[3].point(lambda v: v * alpha // 255)
    sh = Image.new("RGBA", im.size, (0, 0, 0, 0)); sh.putalpha(a)
    sh = sh.filter(ImageFilter.GaussianBlur(radius * S))
    base = Image.new("RGBA", im.size, (0, 0, 0, 0)); base.paste(sh, (offset[0] * S, offset[1] * S), sh)
    base.alpha_composite(im)
    return base


def save(im, name):
    im = shadow(im, 6, (0, 5), 120)
    im.resize((256, 256), Image.LANCZOS).save(f"{out}/{name}.png")
    print(name)


def rounded(d, box, r, fill, outline=None, width=0):
    d.rounded_rectangle(box, r, fill=fill, outline=outline, width=width)


def centered_text(d, cx, cy, text, f, fill, stroke=0, stroke_fill=None):
    l, t, r, b = d.textbbox((0, 0), text, font=f, stroke_width=stroke)
    d.text((cx - (l + r) / 2, cy - (t + b) / 2), text, font=f, fill=fill, stroke_width=stroke, stroke_fill=stroke_fill)


def heart(d, cx, cy, s, fill):
    r = s * .28
    d.ellipse((cx - s * .5, cy - s * .42, cx - s * .5 + 2 * r, cy - s * .42 + 2 * r), fill=fill)
    d.ellipse((cx + s * .5 - 2 * r, cy - s * .42, cx + s * .5, cy - s * .42 + 2 * r), fill=fill)
    d.polygon([(cx - s * .49, cy - s * .2), (cx + s * .49, cy - s * .2), (cx, cy + s * .48)], fill=fill)


def spade(d, cx, cy, s, fill):
    r = s * .28
    d.ellipse((cx - s * .5, cy - s * .05, cx - s * .5 + 2 * r, cy - s * .05 + 2 * r), fill=fill)
    d.ellipse((cx + s * .5 - 2 * r, cy - s * .05, cx + s * .5, cy - s * .05 + 2 * r), fill=fill)
    d.polygon([(cx - s * .49, cy + s * .17), (cx + s * .49, cy + s * .17), (cx, cy - s * .5)], fill=fill)
    d.polygon([(cx, cy + s * .1), (cx - s * .2, cy + s * .55), (cx + s * .2, cy + s * .55)], fill=fill)


# ── Site logo: gold 888 on a burgundy tile with a gold rim.
im = canvas(); d = ImageDraw.Draw(im)
rounded(d, (14 * S, 14 * S, 242 * S, 242 * S), 46 * S, BURGUNDY, GOLD, 9 * S)
rounded(d, (30 * S, 30 * S, 226 * S, 226 * S), 34 * S, None, (255, 220, 140, 90), 3 * S)
centered_text(d, 128 * S, 112 * S, "888", font(BLACK_FONT, 104 * S), GOLD, 4 * S, GOLD_DARK)
centered_text(d, 128 * S, 190 * S, "VIP", font(GEORGIA, 40 * S), WHITE)
save(im, "casino_logo")

# ── Slots: a gold-framed window of three reels, all showing a red 7.
im = canvas(); d = ImageDraw.Draw(im)
rounded(d, (12 * S, 36 * S, 244 * S, 220 * S), 30 * S, GOLD, GOLD_DARK, 5 * S)
rounded(d, (28 * S, 54 * S, 228 * S, 202 * S), 16 * S, BLACK)
f7 = font(BLACK_FONT, 92 * S)
for k in range(3):
    x0 = (36 + k * 64) * S
    rounded(d, (x0, 62 * S, x0 + 56 * S, 194 * S), 10 * S, WHITE)
    centered_text(d, x0 + 28 * S, 124 * S, "7", f7, RED, 2 * S, (120, 0, 20, 255))
d.rectangle((30 * S, 124 * S, 226 * S, 128 * S), fill=(236, 190, 86, 150))
d.ellipse((222 * S, 20 * S, 248 * S, 46 * S), fill=RED)            # lever knob
d.rectangle((231 * S, 44 * S, 239 * S, 110 * S), fill=GOLD_DARK)
save(im, "casino_slots")

# ── Dice: two ivory dice, five and three.
im = canvas(); d = ImageDraw.Draw(im)
def die(cx, cy, size, angle, pips):
    face = Image.new("RGBA", (int(size), int(size)), (0, 0, 0, 0)); fd = ImageDraw.Draw(face)
    rounded(fd, (0, 0, size - 1, size - 1), size * .18, WHITE, (190, 180, 170, 255), int(size * .03))
    for px, py in pips:
        r = size * .085
        color = RED if len(pips) == 1 else BLACK
        fd.ellipse((px * size - r, py * size - r, px * size + r, py * size + r), fill=color)
    face = face.rotate(angle, resample=Image.BICUBIC, expand=True)
    im.alpha_composite(face, (int(cx - face.width / 2), int(cy - face.height / 2)))
five = [(.27, .27), (.73, .27), (.5, .5), (.27, .73), (.73, .73)]
three = [(.27, .27), (.5, .5), (.73, .73)]
die(92 * S, 150 * S, 118 * S, 14, five)
die(170 * S, 104 * S, 108 * S, -12, three)
save(im, "casino_dice")

# ── Roulette: 37 pockets (0 green, then red/black), gold rim and hub, a white ball.
im = canvas(); d = ImageDraw.Draw(im)
c = 128 * S; R = 116 * S
d.ellipse((c - R, c - R, c + R, c + R), fill=GOLD_DARK)
d.ellipse((c - R + 6 * S, c - R + 6 * S, c + R - 6 * S, c + R - 6 * S), fill=(92, 52, 22, 255))
r_out = R - 16 * S; r_in = R - 50 * S
for k in range(37):
    a0 = -90 + k * 360 / 37; a1 = a0 + 360 / 37
    color = GREEN if k == 0 else (RED if k % 2 else BLACK)
    d.pieslice((c - r_out, c - r_out, c + r_out, c + r_out), a0, a1, fill=color, outline=GOLD, width=S)
d.ellipse((c - r_in, c - r_in, c + r_in, c + r_in), fill=(120, 72, 30, 255), outline=GOLD, width=3 * S)
for k in range(4):
    a = math.radians(45 + k * 90)
    d.line((c, c, c + math.cos(a) * r_in * .9, c + math.sin(a) * r_in * .9), fill=GOLD, width=7 * S)
d.ellipse((c - 18 * S, c - 18 * S, c + 18 * S, c + 18 * S), fill=GOLD, outline=GOLD_DARK, width=2 * S)
ba = math.radians(-62); br = (r_out + r_in) / 2 + 8 * S
bx, by = c + math.cos(ba) * br, c + math.sin(ba) * br
d.ellipse((bx - 11 * S, by - 11 * S, bx + 11 * S, by + 11 * S), fill=WHITE, outline=(170, 170, 170, 255), width=S)
save(im, "casino_roulette")

# ── Blackjack: the ace of spades over the king of hearts.
im = canvas()
def card(rank, suit, color, angle, cx, cy):
    w, h = 132 * S, 184 * S
    cd = Image.new("RGBA", (w, h), (0, 0, 0, 0)); dd = ImageDraw.Draw(cd)
    rounded(dd, (0, 0, w - 1, h - 1), 14 * S, WHITE, (200, 194, 186, 255), 3 * S)
    fr = font(GEORGIA, 38 * S)
    dd.text((12 * S, 6 * S), rank, font=fr, fill=color)
    (spade if suit == "s" else heart)(dd, 30 * S, 66 * S, 26 * S, color)
    (spade if suit == "s" else heart)(dd, w / 2, h / 2 + 14 * S, 74 * S, color)
    cd = cd.rotate(angle, resample=Image.BICUBIC, expand=True)
    im.alpha_composite(cd, (int(cx - cd.width / 2), int(cy - cd.height / 2)))
card("K", "h", RED, 12, 98 * S, 132 * S)
card("A", "s", BLACK, -10, 158 * S, 124 * S)
save(im, "casino_blackjack")

# ── Chip: red with white edge stripes and a gold centre.
im = canvas(); d = ImageDraw.Draw(im)
c = 128 * S; R = 112 * S
d.ellipse((c - R, c - R, c + R, c + R), fill=RED)
for k in range(8):
    a0 = k * 45 - 9
    d.pieslice((c - R, c - R, c + R, c + R), a0, a0 + 18, fill=WHITE)
d.ellipse((c - R * .78, c - R * .78, c + R * .78, c + R * .78), fill=RED)
d.ellipse((c - R * .62, c - R * .62, c + R * .62, c + R * .62), fill=WHITE)
d.ellipse((c - R * .56, c - R * .56, c + R * .56, c + R * .56), fill=GOLD)
centered_text(d, c, c, "888", font(BLACK_FONT, 46 * S), BURGUNDY)
save(im, "casino_chip")

# ── Dragon / Tiger: two cards, a red 龙 and a black 虎, with a gold VS between them.
im = canvas()
HEI = "/System/Library/Fonts/STHeiti Medium.ttc"
def tile(ch, color, angle, cx, cy):
    w, h = 118 * S, 164 * S
    cd = Image.new("RGBA", (w, h), (0, 0, 0, 0)); dd = ImageDraw.Draw(cd)
    rounded(dd, (0, 0, w - 1, h - 1), 16 * S, WHITE, GOLD, 5 * S)
    rounded(dd, (12 * S, 12 * S, w - 12 * S, h - 12 * S), 10 * S, None, color, 3 * S)
    centered_text(dd, w / 2, h / 2, ch, font(HEI, 78 * S), color)
    cd = cd.rotate(angle, resample=Image.BICUBIC, expand=True)
    im.alpha_composite(cd, (int(cx - cd.width / 2), int(cy - cd.height / 2)))
tile("龙", RED, 10, 80 * S, 128 * S)
tile("虎", BLACK, -10, 176 * S, 128 * S)
d = ImageDraw.Draw(im)
d.ellipse((104 * S, 96 * S, 152 * S, 144 * S), fill=BURGUNDY, outline=GOLD, width=4 * S)
centered_text(d, 128 * S, 119 * S, "VS", font(BLACK_FONT, 22 * S), GOLD)
save(im, "casino_dragon")
