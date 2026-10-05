# Blackjack code attribution

Upstream: https://github.com/joaokucera/unity-blackjack
Revision: 0780e7c1873d2adbd6dfd57f896af6da6ceaafba
Copyright (c) 2018 João Kucera. MIT License, retained in LICENSE.txt.

XingGuang/Core/XgBlackjackRules.cs adapts:
- src/Assets/Blackjack/Scripts/Hand.cs, TotalValue: sum card values, downgrade one Ace at a time while over 21.
- src/Assets/Blackjack/Scripts/Deck.cs, ShuffleCards: descending Fisher–Yates shuffle.

Changes: no Unity CardDisplay/ScriptableObject dependencies; cards are persisted integer IDs (0–51); use the game's independent casino RNG delegate; no LINQ allocations for scoring; new S17, natural-blackjack 3:2 payout comparison and existing-wallet integration. Only these code algorithms are adapted. No upstream artwork, audio, prefabs or scenes are copied.

Also inspected edwinharly/simple-blackjack (MIT, c8c85c10869094dc1b5392980493f597c0bfcbbc) but did NOT copy it: its draw range excludes the last card and its multi-Ace conversion is unsuitable for this ruleset.
