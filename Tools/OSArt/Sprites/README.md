# Sprites (DreamOS UI art replacement)

`gen_sprites.py` draws every UI PNG listed below from code (numpy + Pillow, no downloads, no copied art) and
writes them over the old files in `Assets/LingGuangV05/DesktopBridge/OSArt/Textures/`, same path, name, pixel size and
RGBA mode. `.meta` files are never touched, so GUIDs, `spriteBorder`, pivots and pixels-per-unit stay valid.

```
python3 Tools/OSArt/Sprites/gen_sprites.py                 # rewrite the project PNGs in place
python3 Tools/OSArt/Sprites/gen_sprites.py --out /tmp/x    # same tree elsewhere, for previewing
```

What it makes (76 files, deterministic: fixed seeds):

- Borders (24): flat 4x4, rounded 1024 canvas (corner radius 75 px, 9-slice insets from the metas), "Cut" 126x126
  (square left, radius 15 right), radial disc and rings (thickness 1x..10x). White on transparent, Unity tints them.
- Shadows (8): glow-around-hole flat shadows (rounded / square / circle) and the radial blob = blurred SDF shapes;
  four gradient shadows with analytic falloff curves. Hole interiors stay transparent.
- System icons: panel indicator triangle, resize stripes, 300 degree loading spinner with a fading tail.
- Media frames: Forward bar + triangle, Repeat P1/P2 (P2 = P1 rotated 180 degrees), Shuffle P1/P2 (P2 = P1 flipped).
- Brand / social icons are generic symbols: Cori -> orb with sparkle / voice orb / ripples, Discord -> chat bubble,
  Reddit -> forum bubbles, Steam -> game controller, Twitter -> share nodes, YouTube -> play disc; settings-element
  variants are the line-art versions of chat / share / play.
- Audio message waveforms (4 variants, 20 round bars), QR code (29x29 random module grid with three finder squares,
  modules are transparent holes in a white plate, not scannable), desktop mark (cyan->blue sparkle on a ring,
  replaces the old logo icon) and a wordless wide banner for the old logo.

Tweak constants at the top of each `gen_*` function (radii, thicknesses, sigmas, colors) and re-run.
