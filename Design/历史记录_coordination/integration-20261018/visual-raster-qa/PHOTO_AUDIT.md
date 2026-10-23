# Existing photos audit — 2026-10-18

Inspected the actual local thumbnails in existing-photo-candidates.png and matched credits to local files. existing-photo-candidates.json preserves each source, author and license block. This is a local provenance audit, not a fresh check of remote Commons pages.

Current game uses six keys with one image per class: cat/avatar_guaji, controller/avatar_ajie, peony/avatar_afang, rose/avatar_aunt, graphics card/avatar_akazhan, case/product_case. IMPORTANT: product_case credits explicitly call it synthetic reference artwork; it must not be counted as a real photograph. Thus current grid is five photographs plus one synthetic picture, reused across nine cells.

Document's requested photograph candidates (distinct original images; no crops counted as new photographs):

| Class | Existing count | Keys | Shortfall |
|---|---:|---|---|
| Aircraft | 0 | none; airport interior is not an aircraft | 10 to grid quota |
| Car | 1 | news_didi_uber | 9 grid / 4 five-photo quota |
| Cat | 3 | avatar_guaji, avatar_qingwen, chat_qingwen_cat | 7 grid / 2 five-photo quota |
| Dog | 1 | avatar_xiaogang | 9 grid / 4 five-photo quota |
| Red wine | 0 | none | 10 grid |
| Towel | 0 | none | 10 grid |
| Phone | 4 | gift_iphone7, news_iphone7, news_leeco, news_note7_recall | 1 five-photo quota |
| Apple fruit | 0 | none; watermelon and Apple phones are not fruit photos | 5 five-photo quota |

The document names six example grid categories, leaving the last two unspecified. It names five example ImageNet categories. Apple branded phones may be deliberately confusing negative examples as designed, but cannot fill missing apple fruit positives.

Other usable candidates exist (one peony, one rose; one whole controller plus one button macro; several GPUs), but they cannot make eight classes with ten distinct true photos each. No cross-category substitutions, clones or synthetic placeholders have been silently added. Existing datasets therefore do NOT satisfy the photo quantity requirement.

Current mapped source licenses: cat CC BY-SA 2.0; controller CC BY 3.0; peony CC BY-SA 3.0; rose public domain; graphics card CC BY-SA 3.0 AT. Full exact original credit text is in JSON. Verify credits before any expanded redistribution.
