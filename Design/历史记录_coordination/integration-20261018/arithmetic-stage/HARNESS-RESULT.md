# Actual C# compile and execution

Parent explicitly authorized a standalone .NET harness after the initial static delivery. No Unity editor/player was launched.

- Runtime: .NET 10, installed `C:/Program Files/dotnet/dotnet.exe`.
- Project: `harness/ArithmeticCheck.csproj`.
- Actual candidate Core sources replace/extend the production Core source set. The harness references the existing `LingGuangV05.Core.dll` and installed NUnit framework; it does not mock `XgSim`, the board, math generator, tokens, catalog, or settlement.
- Executed all `[Test]` methods in `XgMoleArithmeticTests`, `XgArithmeticTests`, and `XgMiniPayTests` through NUnit assertions/reflection.
- Final result: **27 passed, 0 failed.** New math coverage executed **10,500 actual generated C# claims** across all three tiers; the existing test also recomputes 20,000 old arithmetic claims.
- Candidate core compiled without errors. Three pre-existing warnings: `XgSim.DayStats.cs` CS1718 same-variable comparison; unused events in `XgSim.cs` / `XgSim.Quality.cs` CS0067. No unrelated production fixes made.
- Initial run: 26 passed, 1 failed. The failure was the old `ArithmeticDeskOpensAtStageTwoAndPaysLeast` test asserting arithmetic was closed in stage 1. That directly contradicted the approved free stage-one design and existing opening tests. Candidate test now verifies stage-one availability and preserves its original 40-answer/reward/sample assertions. Initial output retained in `harness/initial-run.log` / `initial-runtime-report.json`; final output in `harness/compile-and-run.log` / `runtime-report.json`.
- Source review also fixed literal symbol encoding: the board internally uses `+` / `@` in concept keys, so displayed token code units are hex-encoded before becoming feature names. The test checks no answer-leaking clues or reserved key delimiters enter these features.

This validates real C# math, core model prediction, token ownership, catalog, and settlement behavior. It does **not** compile or run `XgActionGames` or its Unity presentation. The unresolved trained-symbol vocabulary and calculus sign readability remain unresolved acceptance items.
`NodeCost` follow-up: the common StoryResearchPrice=.25 would still discount the three catalog prices. Added a one-line ordinary-price exception in candidate XgSim.Progression.cs, tested actual NodeCost=800/3000/1500, and reran all 27 tests successfully.
