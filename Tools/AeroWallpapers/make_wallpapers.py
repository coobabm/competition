"""Original Aero-era desktop wallpapers for hongmengos (2026-10-19): they replace DreamOS's sample photos
(dolphins, glass cube, …) in the desktop, Settings, Setup and lock screen.

Same procedure as AeroIconBuilder.BuildWallpaper (Assets/HongmengOS/Aero2010/Editor): a vertical gradient, soft
atmosphere glows and a few thin light ribbons, with a different palette per wallpaper. No photos, no downloads.

    python3 Tools/AeroWallpapers/make_wallpapers.py

Writes Assets/HongmengOS/Aero2010/Resources/AeroWallpapers/<id>.png (1920x1080) and a 256x144 thumbnail sheet
preview next to this script.
"""
import os
import numpy as np
from PIL import Image

W, H = 1920, 1080
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "HongmengOS", "Aero2010", "Resources", "AeroWallpapers")


def hexc(v):
    return np.array([(v >> 16) & 255, (v >> 8) & 255, v & 255], dtype=np.float32) / 255


def gauss(x, y):
    return np.exp(-(x * x + y * y) * 2)


# id: (top, bottom, glow, horizon, corner, ribbon, thread, sweep, ribbon height shift, darken edges)
PALETTES = {
    # Light morning sky: pale blue to white, soft cyan ribbons. The default.
    "morning": (0x4F8FCF, 0xBFDDF3, (0.05, 0.08, 0.06), (0.06, 0.07, 0.03), (0.04, 0.02, 0.06),
                (0.30, 0.42, 0.40), (0.55, 0.62, 0.55), (0.10, 0.14, 0.12), 0.0, 0.05),
    # The original blue Aurora (same palette as Aurora-Blue-1920x1080.png).
    "aurora": (0x061E42, 0x0769A8, (0.014, 0.115, 0.18), (0.015, 0.16, 0.13), (0.023, 0.012, 0.048),
               (0.24, 0.78, 0.92), (0.54, 0.91, 1.0), (0.06, 0.24, 0.32), 0.0, 0.12),
    # Green harmony.
    "harmony": (0x0B3A1E, 0x5DAA3A, (0.08, 0.14, 0.04), (0.10, 0.12, 0.02), (0.02, 0.05, 0.03),
                (0.55, 0.85, 0.40), (0.85, 1.0, 0.70), (0.18, 0.28, 0.10), 0.03, 0.10),
    # Warm dusk.
    "dusk": (0x3B2A6B, 0xF2B27A, (0.14, 0.06, 0.08), (0.16, 0.08, 0.02), (0.06, 0.02, 0.08),
             (0.95, 0.72, 0.62), (1.0, 0.90, 0.80), (0.24, 0.14, 0.12), -0.02, 0.10),
}


def build(p):
    top, bottom, glow, horizon, corner, ribbon, thread, sweep, shift, darken = p
    top, bottom = hexc(top), hexc(bottom)
    u = (np.arange(W, dtype=np.float32) + .5) / W
    v = (np.arange(H, dtype=np.float32) + .5) / H
    u, v = np.meshgrid(u, v)  # v = 0 at the top
    t = np.power(v, .8)[..., None]
    c = top * (1 - t) + bottom * t
    c += np.array(glow) * gauss((u - .64) / .60, (v - .60) / .68)[..., None]
    c += np.array(horizon) * gauss((u - .68) / .43, (v - .91) / .29)[..., None]
    c += np.array(corner) * gauss((u - .98) / .32, (v - .17) / .5)[..., None]
    vb = v  # measured from the top, as in the C# source (row y=0 is written to the top of the texture)
    for r in range(4):
        center = .96 - .44 * u + .13 * np.sin(u * 5.1 - .5) + r * .055 + shift
        d = vb - center
        broad = np.exp(-d * d / (.0025 + r * .001))
        thin = np.exp(-d * d / .0000036)
        fade = np.sin(np.clip(u * .9 + .06, 0, 1) * np.pi) * (.13 - r * .018)
        c += np.array(ribbon) * (broad * fade)[..., None]
        c += np.array(thread) * (thin * (.075 - r * .012))[..., None]
    sc = 1.10 - .68 * u + .06 * np.sin(u * 8) + shift
    sd = vb - sc
    smooth = np.clip((u - .15) / .7, 0, 1)
    smooth = smooth * smooth * (3 - 2 * smooth)
    c += np.array(sweep) * (np.exp(-sd * sd / .018) * smooth * .32)[..., None]
    edge = np.power(np.abs(u - .5) * 1.25, 2) + np.power(np.abs(v - .5) * .65, 2)
    c *= (1 - edge * darken)[..., None]
    # A little dither so the gradients do not band.
    c += (np.random.default_rng(7).random((H, W, 1), dtype=np.float32) - .5) / 255
    return Image.fromarray((np.clip(c, 0, 1) * 255 + .5).astype(np.uint8), "RGB")


def main():
    os.makedirs(OUT, exist_ok=True)
    sheet = Image.new("RGB", (256 * len(PALETTES), 144))
    for i, (name, p) in enumerate(PALETTES.items()):
        img = build(p)
        img.save(os.path.join(OUT, name + ".png"), optimize=True)
        sheet.paste(img.resize((256, 144), Image.LANCZOS), (256 * i, 0))
        print("wrote", name)
    sheet.save(os.path.join(os.path.dirname(__file__), "preview.png"))


if __name__ == "__main__":
    main()
