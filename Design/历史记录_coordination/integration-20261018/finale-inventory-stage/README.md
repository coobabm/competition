# C8 — documentation-only inventory

Primary deliverable: `终局现状与待确认_Codex.md` (five required sections: completion status, dialogue review, all tasks, stage inventory, questions).

Snapshot main HEAD: `d3540147b032596b745b40df47878c9b8f0c158c`. No production edits or ending implementation; no Unity run. `source-manifest.json` records the actual inspected production sources. Parent may continue integrating after this snapshot.

The read-only documentation exporter in `inventory/` compiled **main** Core sources, referenced the existing Core dependency, loaded the same two YY JSON resources as `EnsureLearningData`, and enumerated the live catalogs. This is inventory tooling only, not a new game feature. It never trained a model, executed a storyline, touched a save, or imported the C5 candidate. Initial catalog enumeration without the YY bootstrap was 178 nodes; the corrected actual runtime inventory includes 8 dynamic persona nodes for **186 nodes**.

Included enumerations: 20 datasets, 13 legacy desks, 23 teaching desks (19 packs + 4 stage lessons), 19 contracts, 79 achievements, 8 current books, 21 stage quizzes, 12 chat unlocks, 100 life choices, and 66 dynamic guide Step call sites. The document explicitly separates catalog/metadata presence from end-to-end UI completion. Effects and incomplete source jumps are not fabricated.

Main findings:

- Main still implements old E1/E2/E3, 6 automatically accepted final-exam steps, SI letter, 10-line final conversation, and at most 5 pinned rules.
- The old Trial/paper/ContentFilter/LlmContentGuard/LaoZhouInsight/shutdown_hints assets exist under `Play/Main-NoModel-20261003/Design/`, outside main Assets. No transplant was performed.
- New source-linked memory cards, 3 red-team gates, release-action routes, 3 pin slots, 3 shutdown states, launch-night 40 requests, and route × shutdown endings are not an implemented chain.
- R3's select → pin → execute wording conflicts with execute → pin wording. Missing route-action tables, chapter tables, 40 request content, memory trigger templates, 12-ending mapping, examples and P0/P1 classification remain explicitly unresolved.
- The latest user exception is respected: real library titles, authors and required attribution stay; finale company/person names are anonymous. No new dialogue was written, including the missing unpinned red-pen sentence.

`build_inventory.py` assembles the document from `outline.md`, catalog export and exact source/data references. `main-catalog.json`, `counts.json`, and `inventory/export.log` preserve evidence. This snapshot does not claim that later C4/C5/C6 candidates are merged.
