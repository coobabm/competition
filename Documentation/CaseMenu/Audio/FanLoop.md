# Case menu sounds

2026-10-19: all title-room sounds are synthesized by `Tools/CaseMenuAudio/make_case_audio.py` (numpy, seeded; no samples,
no downloads). They replace the clips that came from paid packs: Reach's Hover.wav / Notification.wav, and Feel / Nice
Vibrations' SwitchLight1/2.wav and Fan1.wav (the old FanLoop was cut from Fan1).

| File | Use | Made of |
|---|---|---|
| `FanLoop.wav` (2.0 s, seamless) | fan under the menu | low-shaped noise built over the exact loop length in the frequency domain, 120/240/330/660 Hz tones, a one-cycle wobble |
| `Hover.wav` | switch hover | 6–13 kHz noise tick + faint 2.4 kHz ping |
| `SwitchPress.wav` / `SwitchRelease.wav` | press / release | click transient + short body resonance (press has a second contact) |
| `Boot.wav` | power-on | 880 / 1318.5 Hz sine chime |

All 44.1 kHz mono 16-bit, in `Assets/LingGuangV05/CaseMenu/Art/Audio/`.
