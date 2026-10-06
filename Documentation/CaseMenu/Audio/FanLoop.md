# Case menu fan loop

- Source: `Assets/Feel/NiceVibrations/HapticSamples/Objects/Fan1.wav` from the project's existing Feel / Nice Vibrations asset. The source file is preserved unchanged; the derivative retains the source asset's license requirements.
- Output: `Assets/LingGuangV05/CaseMenu/Art/Audio/FanLoop.wav`.
- Reason: the source is 3.000 seconds of mono 44,100 Hz, 16-bit PCM, but its final 1.000 second is digital silence. Direct looping would repeatedly interrupt the fan.
- Derivation: extract source samples covering 0.200–1.800 seconds; use a 0.120-second equal-power overlap (`cos(t*pi/2)` for the tail, `sin(t*pi/2)` for the head). Emit the segment from sample 5,292 through the pre-overlap tail, followed by the blended tail/head. The final blended sample meets the following source sample at the loop boundary.
- Result: 1.480 seconds, 65,268 samples, mono 44,100 Hz, 16-bit PCM WAV. No silence padding, normalization, synthetic audio, downloaded media, external model call, or additional dependency. Generated using Python standard-library `wave`, `struct`, and `math` only.
- Validation: full no-output FFmpeg decode completed without errors. The loop waveform is continuous at the boundary; the root task is responsible for listening to the result in Unity and validating gain/pitch during boot.
- Original SHA256: `ac8e964a9113e98dd1f6bec93a6e0be96280e61919e9dca8e6b1c122f71e35d8`.
- Derivative SHA256: `4652832071842b55fd7210bc921c15351163f3292dd460680b0129085011fafa`.
- Measured levels: -18.132 dBFS overall RMS, -9.589 dBFS peak. Every 100 ms window contains signal (-19.072 to -17.362 dBFS RMS). The boundary step is 0.000916 full scale, below the clip's largest ordinary adjacent-sample step of 0.007874.

The runtime controller should assign this derivative to the existing fan AudioSource and pass the same clip to `CaseMenuFanAudio.Configure`; no additional voice or AudioListener is needed. Envelope volume is local source gain and remains subject to the existing master-volume route.
