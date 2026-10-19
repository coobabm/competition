#!/usr/bin/env python3
"""Generates our own replacements for the DreamOS placeholder pictures (profile pictures, wallpapers, photos,
music covers, website pictures, video thumbnails, game-hub banners) and the demo video Rolls.mp4.

    python3 Tools/OSArt/Placeholders/gen_art.py                  # overwrite the project files in place
    python3 Tools/OSArt/Placeholders/gen_art.py --out /tmp/prev  # write to another folder instead
    python3 Tools/OSArt/Placeholders/gen_art.py --only "Wallpapers/*" --no-video

Every file is overwritten at its current path with the same pixel size and PNG mode; .meta files are never touched.
"""
import argparse
import fnmatch
import os
import subprocess
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from gen_common import to_img, to_arr
import scenes_small as ss_
import scenes_big as sb

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
PH = "Assets/LingGuangV05/DesktopBridge/OSArt/Textures/Placeholders"


def arr(fn, *a):
    return lambda: to_img(fn(*a))


# rel path -> (size, mode, factory returning a PIL image)
REG = {}
for n in ss_.AVATARS:
    REG[f"Profile Pictures/{n}.png"] = ((256, 256), "RGB", (lambda n=n: ss_.avatar(n)))
for n in ss_.MUSIC:
    REG[f"Music Covers/{n}.png"] = ((512, 512), "RGB", (lambda n=n: ss_.music_cover(n)))
for n in ss_.ICONS:
    REG[f"Website/{n}.png"] = ((64, 64), "RGBA", (lambda n=n: ss_.site_icon(n)))
REG["Website/EyeBleachLogo.png"] = ((489, 100), "RGBA", lambda: ss_.cutebox_logo(489, 100))
REG["Website/EyeBleach.png"] = ((720, 718), "RGB", arr(sb.site_meadow, 720, 718))
REG["Website/EyeBleach2.png"] = ((572, 440), "P", arr(sb.site_beach, 572, 440))
REG["Website/EyeBleach3.png"] = ((518, 767), "RGB", arr(sb.site_balloons, 518, 767))
REG["Game Hub/3D Test Icon.png"] = ((535, 64), "RGBA", lambda: ss_.wordmark(535, 64, "3D TEST GAME", [0xFF6FB5, 0xB58AFF, 0x59BEFE]))
REG["Game Hub/Empty Game Icon.png"] = ((781, 64), "RGBA", lambda: ss_.wordmark(781, 64, "EMPTY GAME", [0xFFFFFF, 0xFFFFFF]))
REG["Game Hub/TicTacToe Icon.png"] = ((747, 64), "RGBA", lambda: ss_.wordmark(747, 64, "TIC-TAC-TOE", [0x4E97FB, 0x57E0D0]))
REG["Game Hub/3D Test Banner.png"] = ((1920, 1080), "RGB", arr(sb.banner_3d, 1920, 1080))
REG["Game Hub/Empty Game Banner.png"] = ((1920, 1080), "RGB", arr(sb.banner_empty, 1920, 1080))
REG["Game Hub/TicTacToe Banner.png"] = ((1920, 1080), "RGB", arr(sb.banner_tictactoe, 1920, 1080))
REG["Photos/Evening.png"] = ((1280, 1920), "P", arr(sb.photo_evening, 1280, 1920))
REG["Photos/Focused.png"] = ((1920, 1280), "P", arr(sb.photo_focused, 1920, 1280))
REG["Photos/Good Boy.png"] = ((1920, 1280), "P", arr(sb.photo_foliage, 1920, 1280))
REG["Photos/Mountain Fuji.png"] = ((1920, 1280), "P", arr(sb.photo_fuji, 1920, 1280))
REG["Photos/The Coolor Daniel.png"] = ((1280, 1352), "P", arr(sb.photo_brick_poster, 1280, 1352))
REG["Videos/Big Buck Bunny Thumbnail.png"] = ((1280, 720), "P", arr(sb.video_thumb_meadow, 1280, 720))
REG["Videos/Rolls Thumbnail.png"] = ((853, 480), "RGB", lambda: to_img(sb.waves_frame(853, 480, .3)))
REG["Wallpapers/Example Wallpaper.png"] = ((3840, 2160), "RGB", arr(sb.wp_underwater, 3840, 2160))
REG["Wallpapers/Example Wallpaper 2.png"] = ((1920, 1080), "RGB", arr(sb.wp_dusk_hills, 1920, 1080))
REG["Wallpapers/Example Wallpaper 3.png"] = ((3840, 2160), "RGB", arr(sb.wp_glass_cube, 3840, 2160))
REG["Wallpapers/Example Wallpaper 4.png"] = ((1920, 1080), "RGB", arr(sb.wp_rain_city, 1920, 1080))
REG["Wallpapers/Example Wallpaper 5.png"] = ((1920, 1080), "P", arr(sb.wp_warm_dunes, 1920, 1080))
REG["Wallpapers/Example Wallpaper 6.png"] = ((3840, 2160), "RGB", arr(sb.wp_alpine_lake, 3840, 2160))

VIDEO = "Videos/Rolls.mp4"
VIDEO_SIZE, VIDEO_FPS, VIDEO_FRAMES = (1920, 1080), 24, 131  # 131 / 24 = 5.458 s, like the original


def to_mode(img, mode):
    if mode == "P":
        img = img.convert("RGB")
        try:
            return img.quantize(256, method=Image.Quantize.LIBIMAGEQUANT, dither=Image.Dither.FLOYDSTEINBERG)
        except Exception:
            return img.quantize(256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.FLOYDSTEINBERG)
    return img.convert(mode)


def render_video(path):
    w, h = VIDEO_SIZE
    sw, sh = 960, 540  # waves are smooth: compute small, scale up
    cmd = ["ffmpeg", "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{w}x{h}", "-r", str(VIDEO_FPS),
           "-i", "-", "-c:v", "libx264", "-preset", "medium", "-crf", "19", "-pix_fmt", "yuv420p", "-movflags", "+faststart", "-an", path]
    pr = subprocess.Popen(cmd, stdin=subprocess.PIPE)
    rng = np.random.default_rng(5)
    for i in range(VIDEO_FRAMES):
        a = sb.waves_frame(sw, sh, i / VIDEO_FRAMES)
        im = to_img(a).resize((w, h), Image.BICUBIC)
        f = np.asarray(im, np.float32)
        f += rng.normal(0, .7, (h, w, 1)).astype(np.float32)  # faint grain hides 8-bit banding in the gradients
        pr.stdin.write(np.clip(f, 0, 255).astype(np.uint8).tobytes())
    pr.stdin.close()
    if pr.wait() != 0:
        raise SystemExit("ffmpeg failed")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", help="output root instead of the project (files go to <out>/<path relative to Placeholders>)")
    ap.add_argument("--only", help="glob on the relative path, e.g. 'Wallpapers/*'")
    ap.add_argument("--no-video", action="store_true")
    args = ap.parse_args()
    base = args.out if args.out else os.path.join(PROJECT, PH)
    for rel, (size, mode, make) in REG.items():
        if args.only and not fnmatch.fnmatch(rel, args.only):
            continue
        t0 = time.time()
        img = make()
        if img.size != size:
            raise SystemExit(f"{rel}: size {img.size} != expected {size}")
        img = to_mode(img, mode)
        dst = os.path.join(base, rel)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        img.save(dst, optimize=True)
        print(f"{rel:46s} {size[0]}x{size[1]} {mode:4s} {os.path.getsize(dst) // 1024:6d} KB  {time.time() - t0:4.1f}s", flush=True)
    if not args.no_video and (not args.only or fnmatch.fnmatch(VIDEO, args.only)):
        t0 = time.time()
        dst = os.path.join(base, VIDEO)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        render_video(dst)
        print(f"{VIDEO:46s} {os.path.getsize(dst) // 1024:6d} KB  {time.time() - t0:4.1f}s")


if __name__ == "__main__":
    main()
