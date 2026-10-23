# Ending trial photo candidate — PARTIAL, 2/5

Prepared 2026-10-19. This package is **not a complete five-photo trial**. Only the two assets under `candidate/` are approved visual matches. Do not ship the `review/` or `research/` directories as trial resources.

| Original ID | Status | Unity resource path (without extension) |
| --- | --- | --- |
| trial_netbar | Verified existing real photo, CC BY-SA 3.0 | LingGuangV05/Media/Photos/trial_netbar |
| trial_gpubox | Missing exact photo | — |
| trial_westlake | Missing exact photo | — |
| trial_serverroom | Verified existing real photo, CC BY-SA 4.0 | LingGuangV05/Media/Photos/trial_serverroom |
| trial_drafts | Missing exact photo | — |

`manifest.json` contains source, author, license, SHA-256, exact original descriptions and answers for missing assets, and precise replacement requirements with unexecuted generation prompts. `SHA256SUMS.txt` covers the candidate files. The original manifest is retained in `existing-reuse-manifest.json` for provenance; its inherited netbar exterior usage note is explicitly corrected in the final manifest and credits, because visual inspection confirms an interior.

The two approved files are byte-for-byte copies of existing production images. Their Unity metas have distinct deterministic GUIDs. No crop, blur, retouch or re-encoding has been applied. They are sourced reference photos; fictional first-person dialogue must not be read as factual authorship attribution. Retain `ENDING_TRIAL_PHOTO_CREDITS.txt` and the CC BY-SA license links on distribution.

## Integration boundary

Import only the candidate assets and credits. Root owns integration and runtime validation. The original five questions, answer keys, shuffled choice behavior and star thresholds 3/4/5 remain unchanged. Missing photos must not become description-only passes, placeholders counted as ready, substitutions from unrelated assets, or a reduced two-photo trial. Host readiness must still require all five real texture resources. Existing no-vision behavior is a separate original gameplay rule and must not be used to conceal missing art.

## Research outcome

Public Wikimedia Commons search metadata is saved under `research/`. Downloaded review candidates retain their full source metadata and byte hash next to the image. Read/download operations were bounded; no external uploads or image-generation calls occurred.

Rejected after visual inspection:

- `westlake-2016-night.jpg`: West Lake at blue hour/dusk; no visible rain. Cannot represent the specified rainy night.
- `westlake-193930.jpg`: genuine 2014 West Lake night scene with an illuminated bridge and artificial water mist; mist is not rain.
- `gpu-box.jpg`: vintage GeForce 6200 retail box is closed. Cannot represent the opened box.
- `dreamer-gpu.jpg`: open computer case, not a graphics-card retail box.
- `messy-desk.jpg`: office clutter, a couple of notepads and mostly printed/stacked papers. Not a desk covered in densely handwritten draft sheets.

Other metadata-level searches returned bare/installed GPUs, post-2016 RTX unboxing, daylight rain at West Lake, Hanoi's unrelated West Lake, old manuscript scans, blank paper, unrelated historical documents, or no results. Existing project assets were also inspected; no exact match for the remaining three was found. This is a bounded unsuccessful search, not proof that no suitable photo exists anywhere.

## Pending replacement plan

The precise preferred option is three newly generated fictional photographs using the prompts in `manifest.json`, followed by visual verification and documented provenance. The alternative is matching reusable real photographs with author/source/license retained. A real rainy-night Hangzhou West Lake photo requires visible rain evidence; do not darken a daytime photo or use artificial mist. An opened GPU box must visibly be open. Draft paper must visibly be handwritten and cover the desk.

At the last environment check, built-in image generation was unavailable, no OPENAI_API_KEY was present in process/user/machine environment, and Python `openai` was absent. API fallback is authorized but could not be executed. No credentials files were searched and no dependencies were installed. Prepared prompts are **not generated image assets**.

## Verification performed

Source hashes matched the existing licensed catalog; copied asset hashes matched; images were inspected; candidate GUIDs were made distinct; package hashes were generated. No Unity import, .NET build, runtime test, inference or playtest was executed by this agent.
