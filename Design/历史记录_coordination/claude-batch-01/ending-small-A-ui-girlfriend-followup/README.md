# Ending A UI isolation follow-up

One-line QA-only change: add `YYGirlfriend` to the existing `QuietDrivers` list. The runner already cancels invokes, stops coroutines, and disables listed ambient drivers before fixture binding and after startup. Production behavior and strict other-contact/group transcript equality remain unchanged.

Evidence: `LocalVerification/EndingSmallAActual/20261019-093850-436/report.json` failed assertion 69, `A3 no other-contact or group transcript changes`. Comparing the isolated primary save and backup shows the expected two Lao Zhou messages and one unrelated Qingwen daily message, `吃晚饭了吗？`, with unread 0 -> 1. `YYGirlfriend.Update` remains independently active when YYChatHub is disabled and resumes daily delivery when the persistence fixture sets TestMode=false. This establishes ambient QA interference, not A3 information leakage.

Patch: `ending-small-A-ui-girlfriend-followup.patch`

SHA256: `8b8b0283f61bcdefb155309d6033911e45cf78151e1bf79adb9f9a6a3ff9a62f`

Validation: `git apply --check` passed against production when generated. No Unity run was launched and no production file was edited by this agent. Parent should integrate and rerun the serial actual-UI checks.
