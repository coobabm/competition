# World tests migration — 2026-10-18

Apply `world-tests-manual-rent-gifts.patch` after all five world patches; use `git apply --ignore-space-change` because original test files have CRLF context (apply-check passed). Existing economics, power, hardware wear, affection, decay, phone first-gift and film-promise assertions remain. Rent assertions now require explicit transfer, partial settlement, fresh invoice and escalating overdue messages. Added invalid-transfer atomicity, legacy debt reload idempotence, gift negative/AI/unavailable inventory and all bilingual panel names.

Apply `world-manual-rent-migration-fix.patch` after the base world patch: regression testing discovered negative rentOldestDueDays made migrated debt saves fail ValidateState on reload. The migration now shifts the rent clock forward, preserving the same overdue age and net wealth with nonnegative fields.

Actual .NET run of the staged NUnit methods: 78 pass, 0 fail; three Unity JsonUtility methods intentionally await Unity. `world-tests-check/test-results.txt` and compile.log record this. No tests were deleted.

`world-yy-routing-check/WorldYyGiftRoutingChecks.cs` is an Editor check for the real Gift panel -> hub.Send -> YYGirlfriend.OnPlayerLine route, duplicate protection, preserved Qingwen hello beat and its subsequent answer. It uses an inactive isolated GameObject and runtime TestMode; does not load/save player data. This has not been run in Unity by the subagent. Parent should copy it into Assets/LocalBuild/Editor, run WorldYyGiftRoutingChecks.Run, and report actual result. Pure core arc preservation alone is not UI routing proof.

Run EconomyTests and GirlfriendTests in Unity including all three JsonUtility tests. Full Unity Roslyn static compile log: world-yy-routing-check/compile.log.
