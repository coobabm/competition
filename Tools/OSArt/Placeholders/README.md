# DreamOS placeholder art (our own)

Procedural replacements for the 38 placeholder files under
`Assets/LingGuangV05/DesktopBridge/OSArt/Textures/Placeholders/` (profile pictures, wallpapers, photos, music covers,
website pictures, video thumbnails, Game Hub banners and title strips) plus the demo video `Videos/Rolls.mp4`.
Everything is drawn by code (numpy + PIL, stroke-font lettering, ffmpeg for the video). No downloads, no photos,
no third-party logos.

    python3 Tools/OSArt/Placeholders/gen_art.py                    # overwrite in place (about 80 s)
    python3 Tools/OSArt/Placeholders/gen_art.py --out /some/dir     # preview elsewhere
    python3 Tools/OSArt/Placeholders/gen_art.py --only "Wallpapers/*" --no-video

Files are overwritten at their existing paths with the same pixel size and PNG mode (RGB / RGBA / palette `P`
as in the originals); `.meta` files are never touched. Requires Python 3 with Pillow and numpy; `ffmpeg` for
Rolls.mp4 (1920x1080, 24 fps, 131 frames = 5.46 s, H.264, no audio, loopable gradient waves).

- `gen_art.py`: registry (path -> size, mode, generator), PNG mode conversion, video encoding.
- `gen_common.py`: gradients, glows, bokeh, ridges, reflections, isometric cube, stroke-font text.
- `scenes_small.py`: avatars, music covers, 64 px site icons, title wordmarks, "CUTE BOX" logo.
- `scenes_big.py`: wallpapers, photos, website pictures, banners, wave frames (also the Rolls thumbnail).

Names are unchanged on purpose (GUIDs/scene references); the content no longer matches the file names
(e.g. `Good Boy.png` is a leaf-wall photo, `EyeBleach*.png` are illustrations, `EyeBleachLogo.png` reads "CUTE BOX").
Fixed seeds: re-running gives identical output.
