"""Tech-tree (科技 page) sprites drawn in code (2026-10-19). They replace the four textures taken from the paid
Upgrade Tree PRO package (box, box_outline_0, box_outline_1, Demo_Examples/button_square_border).

    python3 Tools/TechTreeArt/make_tech_tree_art.py

Writes Assets/LingGuangV05/Art/TechTree/{fill,frame,ring,line}.png (128x128, white, tinted at runtime).
"""
import os
import numpy as np
from PIL import Image

N = 128
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "LingGuangV05", "Art", "TechTree")


def rounded(n, inset, radius):
    """Antialiased coverage of a rounded square inset by `inset` px."""
    c = (np.arange(n) + .5)
    x, y = np.meshgrid(c, c)
    half = n / 2 - inset
    qx = np.abs(x - n / 2) - (half - radius)
    qy = np.abs(y - n / 2) - (half - radius)
    d = np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qy, 0) ** 2) + np.minimum(np.maximum(qx, qy), 0) - radius
    return np.clip(.5 - d, 0, 1)


def save(name, rgb, alpha):
    img = np.zeros((N, N, 4), np.uint8)
    img[..., :3] = np.clip(rgb * 255 + .5, 0, 255).astype(np.uint8)
    img[..., 3] = np.clip(alpha * 255 + .5, 0, 255).astype(np.uint8)
    os.makedirs(OUT, exist_ok=True)
    Image.fromarray(img, "RGBA").save(os.path.join(OUT, name + ".png"))
    print("wrote", name)


white = np.ones((N, N, 3))
# Square fill: the whole cell, very slightly rounded.
save("fill", white, rounded(N, 0, 2))
# State frame: an 8 px ring.
save("frame", white, rounded(N, 0, 3) - rounded(N, 8, 1))
# Hover ring: a 5 px ring.
save("ring", white, rounded(N, 0, 3) - rounded(N, 5, 1))
# Connector / divider: a grey-blue cell with a light inner bevel line.
outer = rounded(N, 0, 6)
bevel = rounded(N, 4, 4) - rounded(N, 8, 2)
rgb = np.empty((N, N, 3))
rgb[...] = np.array([152, 154, 175]) / 255
rgb = rgb * (1 - bevel[..., None]) + np.array([222, 224, 234]) / 255 * bevel[..., None]
save("line", rgb, outer)
