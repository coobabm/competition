# News and era-post photography audit — 2026-10-07

- Scope: 38 `news_<id>` and 9 `era_<id>` keys from `era_events.json`.
- Completed: 47 images, 47 distinct original URLs, 47 distinct SHA256 hashes; no missing keys.
- Files: `Assets/LingGuangV05/Resources/LingGuangV05/Media/Photos/`.
- Attribution: `news_sources.json` retains title, source page, original URL, author name, raw author/credit HTML, author links, license and license URL, capture date and editorial usage notes.
- License metadata: CC BY / CC BY-SA / CC0 / public domain only. No AI image generators were used for this group.
- QA: all three labeled contact sheets were visually reviewed; GTX1080 close-up, Tiangong-2 interior and final airport photo were also inspected individually. Files decoded with ImageMagick; dimensions and hashes validated.
- Editorial rule: use the UI label “资料图 / Reference image”. Contextual images and archival portraits must not be described as the exact event, model or venue. Per-key boundaries are in `usage_note`.
- Deliberate contextual choices: a semiconductor wafer for first-gen TPU news (not an actual TPU); a Velodyne lidar for Baidu autonomous-driving news (not a Baidu vehicle); motherboard PCIe slots for the RX480 power thread (not a substitute card model); 2016 PBOC headquarters for exchange-rate news; a real 2015 residual-block diagram for ResNet.
- Archive boundaries: Fan Hui (2005), Lee Sedol (2012), Yang Jiang (1962), Bob Dylan (2012), NIPS (2013), Faye Wong (2011), and FAST construction (2015) are not falsely labeled as 2016 event photographs. The Rio photograph is the China–Brazil quarterfinal, not the gold-medal final.
- Some timeless objects or period hardware were photographed later; no later-generation hardware is intentionally represented as a 2016 product. Source capture dates remain visible in the manifest.
- Unity import, UI wiring and Play-mode verification belong to the integrating task; this curation task did not operate Unity or modify source code.
