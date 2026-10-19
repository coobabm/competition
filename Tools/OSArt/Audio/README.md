# OSArt / Audio

`make_audio.py` synthesizes all sounds of the desktop UI from scratch (numpy + stdlib `wave`; no samples, no downloads).
It overwrites the files in `Assets/LingGuangV05/DesktopBridge/OSArt/Audio/` in place (same names, so the `.meta` GUIDs stay valid).

    python3 make_audio.py                      # regenerate everything
    python3 make_audio.py --only keys,music    # subset: keys hover click mouse notification received sent error music desktop
    python3 make_audio.py --only click,mouse,desktop   # just the UI click family (below)
    python3 measure_sfx.py                     # table of the 13 click/desktop sounds: length, peak, loudness, centroid, bands, attack, decay, nearest neighbour
    python3 make_audio.py --out <folder>       # render elsewhere (needs SFX/ and Placeholder/ subfolders)

All files are real 16-bit PCM WAV (the DreamOS originals were a mix of WAV, MP3 and Ogg data under a .wav name).

| File | Format | Length | Sound |
|---|---|---|---|
| SFX/Keyboard_Keystroke1..6 | 48 kHz stereo | 0.21-0.46 s | soft key tap + key-up, six variants (normal / space / enter / big key with a little rattle) |
| SFX/Hover | 48 kHz stereo | 0.16 s | very quiet light tick |
| SFX/Click | 44.1 kHz mono | 0.03 s | the crisp "哒" (see below) |
| SFX/Mouse_Click1 | 48 kHz mono | 0.075 s | the "哒" + a faint higher release tick 40 ms later; plain click on any clickable control |
| SFX/Notification | 48 kHz stereo | 3.2 s | two-note bell (G5 -> C6) with reverb tail |
| SFX/ReceivedMessage | 48 kHz stereo | 1.2 s | softer glassy two-note chime (B5 -> F#5) |
| SFX/SentMessage | 48 kHz stereo | 0.38 s | filtered-noise whoosh + rising pop |
| SFX/Error | 44.1 kHz stereo | 1.62 s | two low, soft, descending buzz pulses |
| Placeholder/Music_JackTheLumberer | 44.1 kHz stereo | 46.8 s | original lo-fi loop "Pocket Lantern": 16 bars, 82 BPM, Cmaj7-Am7-Dm7-G7, e-piano + bass + drums + chiptune-ish lead; rendered circularly so it loops seamlessly |

Short UI sounds start on the first sample (0.2 ms anti-click ramp) and end with a short fade-out.
`_Mixer.mixer` is not generated (see the report: it is left as is, it contains nothing but group names/GUIDs).

## UI click family (2026-10-19)

`Resources/DesktopSfx/*` (48 kHz mono) plus `SFX/Click` and `SFX/Mouse_Click1`. Every mouse release plays exactly one of them (see `DesktopSfx.cs`), so each action sound is "the click + one gesture".

- **The 哒** (`da()` / `snap()`): seven short inharmonic modes 1.5-9.6 kHz, all gone in ~2 ms (no pitch, nothing below 1 kHz), a 0.2 ms edge burst, and a smaller bottom-out contact 1.4 ms later. Causal filters only (no pre-ringing), 0.08 ms onset, no fade-in. Mode phases are fixed (picked for a low crest factor and a 12 % / 70 % / 17 % balance at 1-2.5 / 2.5-6 / 6-12 kHz).
- **Gestures**: `glass()` (a light tone that glides and settles), `swish()` (band-passed air, low-passed at 7 kHz so it never fizzes), or a second contact. Mirror pairs use the same parts with the pitch direction reversed.
- **Loudness** is matched by K-weighted energy (`loudness_k`, about momentary LUFS), not by peak: clicks at `LK_CLICK` (-30.5, peak about -4 dBFS), actions 1-2 dB under, Tap 7.5 dB under.

| Sound | Gesture |
|---|---|
| Tap | smaller, softer single-contact tick |
| Click, SFX/Click | the 哒 |
| Select | higher 哒 + a tiny glass glint |
| Tab | two dry contacts 24 ms apart, low then high ("嗒哒") |
| ToggleOn / ToggleOff | 哒 + short glass chirp up / down a fifth |
| MenuOpen | 哒 + quick airy flick upward |
| WindowMaximize / WindowMinimize | 哒 + air swoosh up / down landing on a small high / low contact |
| WindowOpen / WindowClose | 哒 + glass tone blooming up / falling an octave, with air behind it |

Import note: all these clips are imported as Vorbis. PCM (or ADPCM) keeps a 2 ms transient intact; Vorbis can smear it slightly.
