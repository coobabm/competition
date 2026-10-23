# Classic binary teaching tools

classic-tools.patch adds an actual threshold slider to the classic spam/headline/review factual cards, shows actual Board prediction under player AI name, optional true confidence via sigmoid, and saves actual hard/suspected review evidence before model updates. Does not change Answer truth, payout, queued review semantics, family or work cards. Threshold only affects AI display, not truth. Stale-save and stale-card callbacks guarded; slider hidden on other desks and mode changes.

Desktop compilation passed against current Core. UI not yet runtime verified. Slider occupies y343–364, above answers y368–422; classic AI answer y300–342; no popup or extra navigation.

ChessAlternateInteractionChecks.cs is separate PlayMode runner: sets known introductory ladder fixture, clicks actual board square buttons a1→a7, allows production opponent response, chooses a legal mate and clicks its actual buttons. Asserts correct counter and solved intro id. QA compilation passed. Root runs Unity. Independent core chess audit all57 verified FEN/PGN and all legal first moves pass; only alternative is ladder a1a7 vs standard b2b7.
