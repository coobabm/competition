# Focused world polish and audit

`world-polish-names-credits-privacy.patch`: life-event popup uses Sim.AiDisplayName in both languages; teaching sidebar labels, tips and answer-history legend use chosen AI name (AI marker); chronicle title and author use chosen name, original-name origin and metaphorical wording preserved; Bodu source page is visibly Credits and asset sources, adds Lichess puzzle database CC0 1.0 with source and license URLs; preset g10 changes forbidden rent topic to office PC bluescreen, replies translated.

Unity Roslyn compile of complete overlay + this polish + expanded Editor QA: zero errors (`world-yy-routing-check/polish-compile.log`). All out/refout paths under coordination; no Unity was launched.

Expanded `WorldYyGiftRoutingChecks.Run` checks real gift panel -> hub.Send -> OnPlayerLine, preserved/continuable Qingwen arc; creates real rent toolbar and amount-panel buttons, invokes their listeners for partial transfer, completes by chat then rejects repeated settlement; news date filtering/sorting; forum resource post/reply flag gating; 3 group preset bubbles with translations/privacy; all 14 photo mappings resolve. This QA has only been compiled, not Unity-executed. It uses isolated inactive objects/TestMode and does not load or save player data. Forum check is data visibility, not actual UI button click.

13 new group photos are still absent in main (only existing chat_geyou resolves on disk); generation and genuine PHOTO_CREDITS entries remain necessary. Do not weaken QA or use placeholders.

384 AI-name candidates recorded in world-ai-name-string-inventory.json include legitimate original names, comments, internal model paths and logs. Higher-priority unresolved rendered cases are MiniGames/XgActionGames.cs, XgSocialGames.PoetryAndReviews.cs and Conversations.cs, ZhongbaoAutoPage.cs; broad conversion must be semantic. LaoZhouHelp.Reply old teaching instructions are unreachable (no callers found), retained pending deprecation decision. NPCs' own roommates are not the protagonist's roommates and were retained.
