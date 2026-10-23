# Ending desktop display clock candidate

Status: the parent has imported the runtime and test patches. The parent's combined Editor report passed 67/67, of which `EpilogueDesktopClockTests` accounts for 12/12. This agent has not modified production or started Unity, Player, .NET, models, builds, or tests. See `VALIDATION-20261019.md` for the exact suite breakdown and remaining UI boundary.

## Evidence and scope

The inspected historical screenshot is `LocalVerification/EndingArchiveActual/20261018-225153-642/ending-final-read-to-end-zh.png`: the letter is dated 2017-06-13, while the taskbar and guide-note header show 2016-12-01 / December 1. The spider is visible over the mail controls and the note still asks for the early common-sense lesson. These are observations from that earlier run, not results of this candidate.

Approved design `Design/三结局终局表现与六阶段网络讨论_设计稿_20261019.md`, F3/F4 and the paragraph at line 109, requires a 2017-06-13 morning presentation clock, no economic catch-up, no reversal for late old saves, and no main-save date changes in replay. The parent approved 08:00 as the exact morning time and requested that the guide-note date string share the display clock. Task content and spider behavior are explicitly outside this change.

Root cause: `GameEraClock.LateUpdate` only read `GameCalendar.Now`. Both `AeroShellClock` and `DesktopLocalizedClock` read the resulting `DateAndTimeManager`; neither was connected to `epilogue.displayDate`. `GuideNote.Layout` independently read `GameCalendar.Now` for its date string.

## Runtime patch

Apply `ending-display-clock.patch` from the production repository root. It changes only three existing files:

- `GameEraClock.cs`: adds shared `DisplayNow(runtime)` and uses it for the native desktop manager. Existing `saveTimeData=false`, `useSystemTime=false`, and 12/24-hour formatting remain.
- `XgEpilogueDirector.cs`: reads only its current save/lab binding and committed release evidence. Projects the supported epilogue date as 2017-06-13 08:00. An in-memory reveal latch records the completed F4 presentation, including reduced motion or skipping; it is cleared on every Bind and never saved.
- `GuideNote.cs`: uses the same clock only for the date string. Its tasks and holiday rules still use their original gameplay state. The guide is not converted into an epilogue task list.

No GameCalendar, economic clock, ledger, training, archive timestamps, public receipts, or save schema is changed. Historical records continue to call GameCalendar. No static/global date override is installed. Existing epilogue simulation holds remain responsible for pausing gameplay.

## Boundaries and restoration

1. Ordinary play, an unbound director, invalid/unsupported state, incomplete online launch night, and an unrevealed date presentation return the original economic date/time.
2. F3/F4 keep that clock until the calendar presentation finishes. The first subsequent display read observes the completed view. Hide also observes it before resetting the view, so returning to the desktop immediately after F4 does not lose the reveal. Hiding before F4 finishes does not reveal the future date.
3. `headline`, `route`, `letter`, and `reading` project directly from the current persisted epilogue phase, including after reload. A saved `date` phase repeats its reveal after reload; the temporary latch itself is never persisted.
4. `historicalReview=true` or an actual game date later than 2017-06-13 keeps the economic clock. This guards late legacy saves even when their historical flag is absent. Historical-view headings and article dates remain the existing presentation content.
5. `completed`, replay, or a disabled director return the economic clock. Completion's existing closing presentation covers the desktop until title routing. Reopening the completed archive does not change the date.
6. Every read verifies the supplied save reference, runtime save, controller lab, and `BoundTo` owner. A replacement save immediately receives its own calendar, even before the director is rebound. Bind clears the reveal latch; there is nothing in the save or DreamOS clock data to undo when normal play resumes.

## Verification

The runtime patch passed plain `git apply --check` before import. Candidate source is UTF-8 without BOM and uses LF. The parent's combined Editor run now confirms compilation and 12 passing clock tests; actual UI behavior remains pending the parent's ActualEndingArchive run.

`ending-display-clock-tests.patch` adds `EpilogueDesktopClockTests.cs` and its meta file. Its 12 tests passed in the parent's combined run. They invoke the real `GameEraClock.LateUpdate` and `AeroShellClock.Refresh` against inactive fixture components, assert taskbar dates, compare complete serialized game/lab state before and after, and cover phase gates, late saves, malformed proof/schema/date, replay, disable, replacement owner, and latch clearing. The latch test explicitly injects the in-memory latch; it does not claim to verify an actual animation.

Still required in the actual UI: inspect F3/F4 before/after, Skip and reduced-motion paths, early versus post-reveal Back to desktop, both languages' mail taskbar and guide-note date, reload while reading, completed replay, and normal-play restoration. Confirm no bill/profit/training catch-up and no archive/receipt timestamp changes. Screenshot expectations: mail reading shows June 13, 2017 / 08:00; the guide-note header shows June 13, while its pre-existing task may remain visible by explicit scope decision.
