# LZ2 — verified 2016 technology content candidate

2026-10-19. Independent candidate only; no production edits, Unity runs, commits or pushes by this agent. Apply after C6. Parent owns dialogue approval, runtime integration and actual UI/save checks.

## Delivered

Six original bilingual news summaries with factual sources, event-date precision and conservative earliest visibility. Five entries are added; existing `n0928` is corrected, retaining its ID. Browser total becomes 50. Old era `ai_tay` and `gnmt` are corrected rather than leaving contradictory parallel summaries. Two approved topic discussions enter the existing era forum pool. Four group topics reuse existing presets: `g29` is updated, three added (total 33). No new timer, automatic push, contact, reward or ending logic.

All five modified JSON files are based on the **C6 candidate**. Unrelated objects and existing C6 corrections are preserved and checked individually. `laozhou-news-after-c6.patch` contains local hunks; do not overwrite whole production files with staged copies. `manifest.json` records C6 baselines and final candidate hashes. The parent confirmed C6 is integrated; `git apply --check --ignore-space-change` against the production working tree passed on 2026-10-19. Normalized LF C6 scratch-tree apply also passed.

## Actual consumers and dependency

| Pool | Existing consumer and gate |
| --- | --- |
| Browser | Resources `Browser/browser_news.json` → `BoduView.News.EnsureNews` → `XgNewsCatalog.Load/Available/Home`; `date` gates all stage-specific homepage rankings. `sourceUrl` is already parsed. Extra provenance fields remain in backing JSON for audit and do not claim new runtime behavior. |
| Forum | Resources `Era/era_events.json` → `EraEvents.Visible(Tieba,today)` → `TiebaView` and `TiebaHub.DiscussionAvailable/Context`. Both new posts use the existing dated discussion path. |
| Group | Resources `YY/yy_group_presets.json` → `AvailableGroupPresets` → existing three-choice rotation / `groupPresetTurn` → `SendGroupPreset` → existing 2–4 second reply queue. C6's exact `minDate` field and filter are a required code dependency. |

No new `after` links target non-delivered news IDs. No news item is pretended to be an era push. Group replies are authored as fiction and only appear after the player selects their topic. News and posts are passive dated listings, not six simultaneous popups. New visible dates are May 25, May 29, June 2, June 6, June 10 and September 28; the actual game begins May 24. Older events use retrospective wording. Repeat user selection follows existing preset behavior; this candidate does not claim a new read/once-only persistence feature.

## Sources actually retrieved and read

`source-fetch.json` records URL, final URL, UTC retrieval time, response/hash and failed attempts. `source-text/` contains local research extracts only; none are production reading assets. A successful HTTP status alone is not accepted as evidence: Nature returned a JavaScript challenge and is explicitly excluded. Four speculative legacy endpoints returned 404 and were not used. DeepMind's gzip response was decoded before reading.

| Topic | Verified basis | Event / earliest historical gate / actual pool date |
| --- | --- | --- |
| T01 AlphaGo | [Google official match updates](https://blog.google/technology/ai/alphagos-ultimate-challenge/) explicitly list March 15 Game 5 and its victory; March 13 Game 4 is Lee's win. [DeepMind retrospective](https://deepmind.google/research/breakthroughs/alphago/) confirms 4–1 in Seoul in March. The January Nature paper is not used to prove March results. | Match completed 2016-03-15 / conservative 03-16 / 05-25. |
| T02 XiaoIce | [Microsoft's March 25, 2016 statement](https://blogs.microsoft.com/blog/2016/03/25/learning-tays-introduction/) describes XiaoIce as an existing Chinese social chatbot. [Author paper](https://arxiv.org/abs/1812.08989) says launched in 2014. The latter is creator-only retrospective evidence, not a 2016 document shown to characters. No exact launch day or product memory mechanism is invented. | Launch year 2014 / 2016-01-01 / 05-29. |
| T03 Tay | [Microsoft official account](https://blogs.microsoft.com/blog/2016/03/25/learning-tays-introduction/) dated March 25 says Wednesday launch, a coordinated attack within 24 hours, offensive outputs and current offline status. Report date is stored distinctly from inferred March 23 launch; no exact shutdown hour is invented. | Official report 2016-03-25 / conservative 03-26 / 06-02. Old era report also moves to 03-26. |
| T04 ResNet | [arXiv v1 record](https://arxiv.org/abs/1512.03385) gives December 10, 2015 submission and residual/152-layer experiments; [CVPR proceedings](https://openaccess.thecvf.com/content_cvpr_2016/html/He_Deep_Residual_Learning_CVPR_2016_paper.html) confirms 2016 venue. June 6 text says last year's paper, never a future conference result. | Submission 2015-12-10 / 2016-01-01 / 06-06. |
| T05 GNMT | [Paper history](https://arxiv.org/abs/1609.08144) distinguishes v1 September 26, 2016 and v2 October 8. [Google announcement](https://research.google/blog/a-neural-network-for-machine-translation-at-production-scale/) dated September 27 explicitly limits initial deployment to Chinese→English and notes remaining errors. Summary does not use October revision detail or claim all-language/global 60% improvement. | Product announcement 2016-09-27 / 09-28 (conservative China-day gate) / 09-28. |
| T06 Siri | [Apple October 4, 2011 release](https://www.apple.com/newsroom/2011/10/04Apple-Launches-iPhone-4S-iOS-5-iCloud/) announces Siri with calls and reminders; initial availability/languages are not generalized to every 2016 device. Group voice wording uses reminders rather than claiming a verified specific alarm implementation. | Announcement 2011-10-04 / 2016-01-01 / 06-10. |

The six summaries are newly authored. No source news text, paper abstract, real-person quotation, screenshot, cover, product photo or logo is shipped. `source-dates.csv` separates event precision, earliest historical gate and actual editorial release. Historical source facts and invented character dialogue are separate in `content.json` and `DIALOGUE-REVIEW.md`.

## Historical and naming boundaries

Reviewed the same-day design update and `Design/老周知识策划_更新差异_20261019.md`. The six topics contain no six-stage mechanics, Transformer 2017, GPT/ChatGPT, modern Qwen/LoRA implementation, or future-user help content. The game's approved fictional breakthrough is not presented as public 2016 history. Stage affects homepage ordering only, never historical availability or a character's private knowledge.

Current Design search found scoped restrictions for generated photos (no logos) and ending anonymity, not an applicable blanket text ban. The user explicitly authorizes real historical technology names here. Keep AlphaGo, Microsoft XiaoIce, Tay, ResNet, GNMT and Siri factual; preserve fictional 摆渡 and existing YY names. Do not extend ending anonymity to bibliographic facts. Existing `XgItemLore` speaks about the CVPR award without a date gate; that broader legacy-lore issue is reported for parent review, not silently changed here.

New generic group lines do not mention the player's training, exams, stage or private AI. LaoZhou is not an established member of this group, so no LaoZhou line was inserted into it. LZ1 was notified about the separate existing stage-based `TiebaHub.Discussion.Regard` prompt leak; LZ2 does not overwrite that work.

## Verification

- **74 .NET assertions passed** against actual `XgNewsCatalog` and `EraEvents` sources: all six news items absent before their date and visible on it, all six homepage stages respect dates, GNMT absent in May, forum dates, and no new push channel. `core-result.json`.
- **396 static checks passed**: C6 baseline hashes, preservation of unrelated objects, unique IDs/counts, required bilingual/source/rights fields, paired group lines, exact day gates, C6 minDate code presence, world text boundaries and new runtime/Design row agreement. `static-result.json`.
- Patch check passed against C6 scratch tree and production after parent C6 integration with `--ignore-space-change`.

No Unity, actual JsonUtility group deserialization, live UI scrolling, language switching, player clicks, group queue/rebind/save-load or model-generated discussion result was tested by this agent. Parent should verify date−1/date/date+1 for the six entries, late-stage May never showing GNMT, preset replies in both languages, C6 lifecycle regression, and approved dialogue variants. Existing warnings in DayStats and unused events are unchanged.
