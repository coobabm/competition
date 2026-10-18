"""Synthesized sounds for the title room (2026-10-19). They replace the clips taken from paid asset packs:
Reach's Hover.wav / Notification.wav and Feel / Nice Vibrations' SwitchLight1/2.wav and Fan1.wav (FanLoop).

Everything is generated from noise and sine waves with numpy; no samples, no downloads. Deterministic (seeded).

    python3 Tools/CaseMenuAudio/make_case_audio.py

Writes Assets/LingGuangV05/CaseMenu/Art/Audio/{Hover,Boot,SwitchPress,SwitchRelease,FanLoop}.wav (44.1 kHz mono 16-bit).
"""
import os
import wave
import numpy as np

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "LingGuangV05", "CaseMenu", "Art", "Audio")
rng = np.random.default_rng(20261019)


def t(sec):
    return np.arange(int(SR * sec)) / SR


def env(n, attack, decay):
    x = np.arange(n) / SR
    a = np.clip(x / max(attack, 1e-4), 0, 1)
    return a * np.exp(-np.maximum(x - attack, 0) / decay)


def onepole(x, cutoff, high=False):
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.zeros_like(x)
    prev = 0.0
    for i, v in enumerate(x):
        prev = (1 - a) * v + a * prev
        y[i] = prev
    return x - y if high else y


def bandnoise(n, lo, hi):
    spec = np.fft.rfft(rng.standard_normal(n))
    f = np.fft.rfftfreq(n, 1 / SR)
    spec[(f < lo) | (f > hi)] = 0
    y = np.fft.irfft(spec, n)
    return y / (np.abs(y).max() + 1e-9)


def level(x, rms_db):
    rms = np.sqrt((x ** 2).mean()) + 1e-12
    return x * (10 ** (rms_db / 20) / rms)


def write(name, x):
    x = np.clip(x, -1, 1)
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())
    print(name, round(len(x) / SR, 3), "s", round(20 * np.log10(np.sqrt((x ** 2).mean()) + 1e-12), 1), "dB rms")


def hover():
    # A very short, airy high tick: band noise around 8-12 kHz plus a faint 2.4 kHz ping.
    n = int(SR * .12)
    x = bandnoise(n, 6000, 13000) * env(n, .002, .012)
    x += .35 * np.sin(2 * np.pi * 2400 * t(.12)) * env(n, .001, .03)
    return level(x, -48)


def boot():
    # Power-on chime: two soft sine partials a fifth apart, the upper one entering 60 ms later.
    d = .9
    n = int(SR * d)
    tt = t(d)
    x = np.zeros(n)
    for f, start, amp in ((880, 0, 1), (1318.5, .06, .7), (1760, .06, .18)):
        s = int(start * SR)
        e = env(n - s, .004, .28)
        x[s:] += amp * np.sin(2 * np.pi * f * tt[: n - s]) * e
    x += .05 * bandnoise(n, 3000, 9000) * env(n, .001, .01)
    return level(x, -26)


def switch(press):
    # A plastic push switch: a sharp click transient, a short body resonance and (on press) a small second contact.
    d = .25
    n = int(SR * d)
    x = bandnoise(n, 1500, 12000) * env(n, .0005, .004)
    body = 1900 if press else 2600
    x += .45 * np.sin(2 * np.pi * body * t(d)) * env(n, .0005, .012)
    if press:
        s = int(.018 * SR)
        x[s:] += .4 * bandnoise(n - s, 2500, 10000) * env(n - s, .0005, .003)
    return level(x, -31 if press else -36)


def fan():
    # PC case fan: broadband air noise shaped low, a hum at the motor and blade-pass tones. Built in the frequency
    # domain over exactly the loop length, so the end meets the start without a crossfade.
    d = 2.0
    n = int(SR * d)
    f = np.fft.rfftfreq(n, 1 / SR)
    spec = (rng.standard_normal(len(f)) + 1j * rng.standard_normal(len(f)))
    shape = 1 / np.sqrt(1 + (f / 450) ** 2) / np.sqrt(1 + (f / 3500) ** 4)
    shape[f < 40] = 0
    spec *= shape
    x = np.fft.irfft(spec, n)
    x /= np.abs(x).max()
    tt = t(d)
    # Tones on whole-cycle frequencies of the 2 s loop (multiples of .5 Hz) stay seamless too.
    for hz, amp in ((120, .10), (240, .05), (330, .06), (660, .025)):
        x += amp * np.sin(2 * np.pi * hz * tt)
    # A gentle slow wobble (one cycle per loop).
    x *= 1 + .06 * np.sin(2 * np.pi * .5 * tt)
    return level(x, -18)


if __name__ == "__main__":
    write("Hover", hover())
    write("Boot", boot())
    write("SwitchPress", switch(True))
    write("SwitchRelease", switch(False))
    write("FanLoop", fan())
