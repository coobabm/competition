# Ending private UI adapter

Only `Assets/LocalBuild/Editor/EndingPrivateActualUiChecks.cs` changes. Production and the original paused feature patch were not edited by this agent.

## Integration

Root already applied the flatten-only adapter. Apply **patch/ending-private-ui-driver-isolation.patch** to that state; SHA256 `ac6d9c9d281b8ed89d1d948419c032e51066b7b04700f38b80b3600927750101`.

For a checkout containing only the paused feature patch, use **patch/ending-private-ui-adapter.patch** instead; SHA256 `9875a3c0290836c8aefe343e7a1de9fb485220759f594954c704698af1f75893`. Do not apply both. This combined artifact supersedes the old adapter for new integrations. The original `ending-private-ui-flatten.patch` remains unchanged for audit.

## What changed

The existing frame flattener still advances nested waits/taps/scroll routines and disposes all nested iterators. The suite now saves enabled flags and freezes the controller and YY ambient Updates plus AiJoinsYy, ZhouCardLoan, ShopNotices, ending preparation, and the same independent narrative/era/notifier owners used by the news-comment suite. Those owners can otherwise change stage-dependent fixture state or open competing UI.

The enabled flags are restored in a nested finally even if fixture/window restoration throws; each flag restoration is attempted. The suite rejects an already-running AiJoinsYy or ZhouCardLoan coroutine before freezing because disabling a MonoBehaviour does not cancel all coroutines. It never resets StoryFocus, input locks, typing queues, or coroutine state. The actual YY/mail views, epilogue director/mail bridge, LCD relay and EventSystem remain active.

## Prerequisites and limits

Root retains ownership of Sidebar phase wiring, Prepare/Restore, iterator disposal, scene/save/profile isolation, model suppression, basic story suppression, real foreground focus and final evidence. Start with idle story/coroutine owners; this helper restores enabled flags, not suspended presentation state. Runner model suppression must cover every model owner required by the project's no-model verification policy; this adapter does not start or disable models.

Coverage is unchanged: both languages, 960x600 native windows, actual LCD preview/cancel/close/send, stale language and cross-save rejection, real isolated slot-6 save/reload, LP4 local-route mail versus unchanged nine public paragraphs, and long R3 original-answer preview/wheel/cancel/forward. Stage/contact/SI-read/release/AI-answer prerequisites remain declared fixtures. Stale callback and trimmed serialized history are synthetic negatives. No natural progression, physical OS input, completed-ending replay, actual LP1/LP2/R1/R2/shutdown-send path, inference or performance claim.

## Validation

Read-only `git apply --check` passed against production after the flatten-only adapter. Source inspection confirmed synchronous disclosure methods and runtime subscriptions remain callable while controller/YY Updates are disabled. No Unity compile, PlayMode, build or model execution was performed by this agent; root must compile and run the integrated suite.
