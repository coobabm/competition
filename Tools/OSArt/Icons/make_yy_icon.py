"""YY icon (2026-10-19): a flat piglet in a red scarf, our own drawing in the style of 2016 flat messenger mascots.
Renders yy_pig.svg (headless Chrome) and writes the YY app icons in place (same files, metas untouched).

    python3 make_yy_icon.py
"""
import os, subprocess, tempfile
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
RENDER = 1024

APP_ICON = os.path.join(ROOT, "Assets/HongmengOS/Aero2010/Art/Aero-Messaging.png")
POPUP_ICON = os.path.join(ROOT, "Assets/LingGuangV05/Resources/LingGuangV05/Icons/popup/yy.png")
# The desktop shortcut loads this file at runtime (AeroLicensedIconLoader); it replaced Oxygen's kopete.png
DESKTOP_ICON = os.path.join(ROOT, "Assets/StreamingAssets/HongmengOS-Aero2010/Oxygen/Messaging.png")


def render_svg(svg_path, size):
    svg = open(svg_path).read().replace('width="512" height="512"', f'width="{size}" height="{size}"')
    with tempfile.TemporaryDirectory() as tmp:
        html = os.path.join(tmp, "icon.html")
        out = os.path.join(tmp, "icon.png")
        with open(html, "w") as f:
            f.write(f'<html><body style="margin:0;background:transparent">{svg}</body></html>')
        subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars",
                        "--default-background-color=00000000", f"--window-size={size},{size}",
                        f"--screenshot={out}", "file://" + html],
                       check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        return Image.open(out).convert("RGBA").crop((0, 0, size, size))


def badge(size):
    """The tray popups' round light badge the old YY logo sat on."""
    scale = 4
    big = Image.new("RGBA", (size * scale, size * scale), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    pad = 6 * scale
    d.ellipse((pad, pad, size * scale - pad, size * scale - pad), fill=(214, 220, 228, 255))
    inner = pad + 4 * scale
    d.ellipse((inner, inner, size * scale - inner, size * scale - inner), fill=(250, 251, 253, 255))
    return big.resize((size, size), Image.LANCZOS)


def main():
    art = render_svg(os.path.join(HERE, "yy_pig.svg"), RENDER)

    app = art.resize((128, 128), Image.LANCZOS)
    app.save(APP_ICON)
    app.save(DESKTOP_ICON)

    popup = badge(128)
    pig = art.resize((92, 92), Image.LANCZOS)
    popup.alpha_composite(pig, (18, 16))
    popup.save(POPUP_ICON)

    art.resize((512, 512), Image.LANCZOS).save(os.path.join(HERE, "yy_pig_512.png"))
    print("wrote", APP_ICON, DESKTOP_ICON, POPUP_ICON)


if __name__ == "__main__":
    main()
