"""
连对倍率音效（×2 … ×5）: one stinger per multiplier tier, one hit layer per tier, and the drop when a streak breaks.
Built from the same felt-piano / bell / sub-drop blocks as make_sfx.py so they sit in the 灵光 sound set, and
each tier is bigger than the one before: more notes, a longer riser, a deeper drop, a wider room.

    .venv/bin/python make_streak_sfx.py [--preview out.wav]

Writes Assets/LingGuangV05/Resources/XgSfx/Streak/{Up2..Up5,Hit2..Hit5,Drop}.wav. Deterministic (seeded).
"""
import argparse
import os

import numpy as np

from make_sfx import (SR, OUT, bell, crackle, felt_note, finish, glide, lp, noise_sweep, place, space,
                      sub_drop, wood, write_wav)

STREAK_OUT = os.path.join(OUT, "Streak")

# C major / A minor family so every stinger agrees with the Kill1-10 ladder and with each other.
C5, E5, G5, C6 = 523.25, 659.25, 783.99, 1046.5


def coin(freq, rng, gain=1.0):
    """One 叮: two bells a fourth apart, the cash-register coin of make_sfx's s_coin."""
    x = np.zeros(int(SR * 0.9))
    place(x, bell(freq, 0.8, rng, 0.7) * 0.5 * gain, 0)
    place(x, bell(freq * 4 / 3, 0.8, rng, 0.8) * 0.55 * gain, 0.06)
    return x


def coin_shower(count, seconds, rng, low=1400, high=2600, gain=0.3):
    """Coins raining on a desk: many short bells at random pitches, thinning out."""
    x = np.zeros(int(SR * (seconds + 0.6)))
    for i in range(count):
        at = (rng.uniform(0, 1) ** 1.4) * seconds
        place(x, bell(rng.uniform(low, high), 0.35, rng, 0.35) * gain * rng.uniform(0.5, 1.0), at)
    return x


def riser(seconds, rng, f_from=250, f_to=5200, tone=(160, 640)):
    """The 2016 desktop 'charging' swell: air plus a rising hum, ending on the beat."""
    n = int(SR * seconds)
    shape = np.linspace(0, 1, n) ** 1.8
    x = noise_sweep(f_from, f_to, seconds, rng, 0.5, shape) * 0.4
    ph = glide(tone[0], tone[1], seconds, -2.0)
    vib = 1 + 0.006 * np.sin(2 * np.pi * 6 * np.arange(n) / SR)
    return x + np.sin(ph * vib) * shape * 0.22


def chord_roll(buf, root, ratios, at, rng, step=0.05, decay=2.2, gain=0.5):
    for i, r in enumerate(ratios):
        place(buf, felt_note(root * r, decay + 0.4, rng, 0.85, decay) * (gain - i * 0.04), at + i * step)


# ───────────── tier stingers ─────────────

def up2(rng):
    """×2 翻倍: a bright double 叮 and a small knock. Clearly a reward, still light."""
    x = np.zeros(int(SR * 1.5))
    place(x, sub_drop(90, 45, 0.5, 2.0) * 0.35, 0)
    place(x, felt_note(C5, 1.2, rng, 0.8, 1.2) * 0.45, 0)
    place(x, felt_note(G5, 1.2, rng, 0.8, 1.2) * 0.45, 0.08)
    place(x, coin(C6 * 1.5, rng, 0.9), 0.1)
    return finish(space(x, 1.4, 0.24, 1.0), 0.62)


def up3(rng):
    """×3 手热了: a short swell into a three-note climb, a coin cascade and a firmer drop."""
    rise = 0.28
    x = np.zeros(int(SR * 2.2))
    place(x, riser(rise, rng, 400, 3600, (220, 520)) * 0.7, 0)
    place(x, sub_drop(110, 40, 0.9, 2.4) * 0.6, rise)
    for i, f in enumerate((C5, E5, G5)):
        place(x, felt_note(f, 1.4, rng, 0.9, 1.4) * 0.45, rise + i * 0.07)
    place(x, bell(C6 * 2, 1.2, rng, 0.9) * 0.1, rise + 0.21)
    place(x, coin_shower(7, 0.5, rng, 1500, 2600, 0.32), rise + 0.12)
    return finish(space(x, 1.8, 0.27, 1.0), 0.72)


def up4(rng):
    """×4 上头: a charging riser, then an impact: deep drop, a major chord stab, sparks and a shower of coins."""
    rise = 0.45
    x = np.zeros(int(SR * 3.0))
    place(x, riser(rise, rng, 300, 5000, (180, 720)) * 0.85, 0)
    place(x, sub_drop(140, 34, 1.4, 2.8) * 0.9, rise)
    place(x, wood([180, 410, 860], 0.4, rng, 0.08) * 0.5, rise)
    chord_roll(x, 261.63, (1, 1.25, 1.5, 2, 2.5), rise, rng, 0.025, 2.0, 0.5)
    place(x, crackle(1.0, rng, 90) * 0.4, rise + 0.02)
    place(x, coin_shower(12, 0.8, rng, 1300, 2800, 0.3), rise + 0.1)
    place(x, bell(C6 * 2, 1.5, rng, 1.1) * 0.12, rise + 0.15)
    return finish(space(x, 2.3, 0.3, 1.0), 0.82)


def up5(rng):
    """×5 灵光一闪: the whole machine surges. Long riser with a fan-up hum, a double impact,
    a maj9 fanfare roll, a glass shimmer climbing out of it and coins raining down."""
    rise = 0.7
    x = np.zeros(int(SR * 4.2))
    place(x, riser(rise, rng, 200, 6500, (120, 960)) * 1.0, 0)
    # A fan spinning up under the riser (the PC itself joining in).
    n = int(SR * rise)
    t = np.arange(n) / SR
    fan = np.sin(2 * np.pi * np.cumsum(40 + 260 * (t / rise) ** 2) / SR) * (t / rise) ** 1.5
    place(x, lp(fan, 900) * 0.25, 0)
    # Double impact: a pickup hit one sixteenth before the downbeat.
    place(x, sub_drop(160, 30, 0.35, 3.0) * 0.6, rise - 0.09)
    place(x, sub_drop(150, 28, 2.0, 3.2) * 1.0, rise)
    place(x, wood([150, 340, 720], 0.5, rng, 0.1) * 0.6, rise)
    chord_roll(x, 261.63, (1, 1.25, 1.5, 1.875, 2.25, 3), rise, rng, 0.045, 2.8, 0.52)
    # Glass shimmer climbing out of the chord.
    for i, f in enumerate((C6, C6 * 1.25, C6 * 1.5, C6 * 2, C6 * 2.5)):
        place(x, bell(f, 1.4, rng, 1.1) * (0.14 - i * 0.015), rise + 0.32 + i * 0.06)
    # A soft pad that keeps it glowing after the hit.
    n = int(SR * 2.6)
    t = np.arange(n) / SR
    padv = sum(np.sin(2 * np.pi * 261.63 * c * t * (1 + d)) for c in (1, 1.5, 2) for d in (-0.003, 0.003)) / 6
    place(x, lp(padv, 2000) * np.sin(np.linspace(0, np.pi, n)) ** 2 * 0.28, rise + 0.1)
    place(x, crackle(1.6, rng, 120) * 0.45, rise + 0.02)
    place(x, coin_shower(22, 1.4, rng, 1200, 3000, 0.28), rise + 0.12)
    return finish(space(x, 3.0, 0.34, 1.0), 0.92)


# ───────────── per-answer layers ─────────────
# Played under the Kill1-10 step on every right answer while the multiplier is up, so a ×5 run keeps sounding ×5.

def hit2(rng):
    x = np.zeros(int(SR * 0.8))
    place(x, coin(C6 * 1.5, rng, 0.8), 0)
    return finish(space(x, 0.9, 0.18, 1.0), 0.32)


def hit3(rng):
    x = np.zeros(int(SR * 0.9))
    place(x, wood([220, 520], 0.25, rng, 0.06) * 0.4, 0)
    place(x, coin(C6 * 1.5, rng, 0.8), 0)
    place(x, coin(C6 * 2, rng, 0.5), 0.05)
    return finish(space(x, 1.0, 0.2, 1.0), 0.38)


def hit4(rng):
    x = np.zeros(int(SR * 1.0))
    place(x, sub_drop(110, 45, 0.35, 2.4) * 0.5, 0)
    place(x, coin(C6 * 1.5, rng, 0.8), 0)
    place(x, coin_shower(4, 0.18, rng, 1500, 2600, 0.3), 0.03)
    place(x, felt_note(G5, 0.7, rng, 0.8, 0.7) * 0.25, 0)
    return finish(space(x, 1.2, 0.22, 1.0), 0.44)


def hit5(rng):
    x = np.zeros(int(SR * 1.2))
    place(x, sub_drop(130, 40, 0.45, 2.8) * 0.6, 0)
    place(x, coin(C6 * 1.5, rng, 0.9), 0)
    place(x, coin_shower(7, 0.25, rng, 1400, 2900, 0.3), 0.02)
    for i, f in enumerate((C6, C6 * 1.5, C6 * 2)):
        place(x, bell(f, 0.8, rng, 0.7) * 0.1, 0.04 + i * 0.04)
    return finish(space(x, 1.5, 0.26, 1.0), 0.5)


def drop(rng):
    """连对断了: the surge collapsing — a falling hum, a dull drop, a fizzle. Gain is scaled by the tier lost at play time."""
    x = np.zeros(int(SR * 1.6))
    n = int(SR * 0.6)
    ph = glide(620, 110, 0.6, 2.5)
    place(x, np.sin(ph) * np.linspace(1, 0, n) ** 1.2 * 0.35, 0)
    place(x, sub_drop(120, 30, 1.0, 2.6) * 0.8, 0.05)
    place(x, crackle(0.9, rng, 50) * 0.3, 0.05)
    place(x, felt_note(146.83, 0.9, rng, 0.4, 0.8) * 0.2, 0.05)
    return finish(space(x, 1.1, 0.2, 1.0), 0.6)


SOUNDS = {"Up2": up2, "Up3": up3, "Up4": up4, "Up5": up5, "Hit2": hit2, "Hit3": hit3, "Hit4": hit4, "Hit5": hit5, "Drop": drop}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--preview", help="also write a reel of every sound to this path")
    ap.add_argument("--out", default=STREAK_OUT)
    args = ap.parse_args()
    os.makedirs(args.out, exist_ok=True)
    reel = []
    for i, (name, make) in enumerate(SOUNDS.items()):
        st = make(np.random.default_rng(2016 + i))
        write_wav(os.path.join(args.out, name + ".wav"), st)
        print(f"{name}.wav  {len(st) / SR:.2f}s  peak {np.abs(st).max():.2f}")
        reel += [st, np.zeros((int(SR * 0.5), 2))]
    if args.preview:
        write_wav(args.preview, np.concatenate(reel))


if __name__ == "__main__":
    main()
