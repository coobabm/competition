# C5 mole review follow-up

Four-file focused candidate; all production files remain untouched by this follow-up. The parent already corrected `desk.Start` to `desk.Open` and that single-line fix is incorporated into the baseline, not re-applied by the patch.

From the project directory:

```powershell
git apply --check ../.coordination/integration-20261018/mole-review-polish/mole-review.patch
git apply ../.coordination/integration-20261018/mole-review-polish/mole-review.patch
```

After integrating and compiling, root can run `MoleReviewUiChecks.Run` using its existing serial Unity runner. This agent ran no Unity, .NET/compiler or model. A passing patch check is not a compile/runtime pass.

## Findings and changes

- The original UI QA found a live calculus slot but reviewed the latest slot without navigating to the calculus slot. The revised check uses real LCD Next controls, verifies the exact claim and complete selected-language conditions, and checks their text bounds.
- Actual LCD mole input uses pointer-down. A dedicated QA helper reuses the existing LCD coordinate mapper and asserts the real raycast target before forwarding down/up. It checks immediate hit feedback, empty-hole feedback and duplicate-click non-settlement/non-training. It does not invoke the private hit method or substitute a generated model prediction.
- Screenshots now yield a rendered frame after updating the canvas, preventing a stale render-texture capture.
- Restart deferred child destruction left `Root.Find("Rule")` able to select the previous inactive label. The candidate binds only current active rule text and guards all mole button callbacks by build generation, including Restart. The QA rebuilds through the real restart control, then deliberately invokes old callbacks to test rejection.
- Review blocks queued mole hits. Accepted hits paint their visible feedback immediately. Empty or already-used holes give a neutral response without settlement or training.
- Reduced effects cache uses the visible hit state rather than its changing timer, avoiding identical mesh rebuilds while the static hit face remains shown.
- Explicit simulation-owner replacement, hidden surface and disposed-round fixtures invoke pending review callbacks. These use a fresh unanswered lesson so they cannot pass merely because a previous answer was consumed. They are explicitly adversarial callback fixtures, not a claim of testing a full disk save-load interaction.
- Three added core tests cover explicit false-label supervision, blocked attempts remaining answerable, and bounded lesson eviction. Existing four core tests remain intact (seven total).

`XgSim.MoleTeaching.cs` was reviewed and is unchanged: the immutable question, ticket/consumed guard, correct explicit human label path and reward-free training remain the existing implementation. No learning math, generator, inference input, exam, holdout or threshold changed. The frozen unseen-question generalization failure remains valid. This work demonstrates explicit training entry and lifecycle checks; it does not establish learning transfer or success on unseen arithmetic.

The UI suite samples a naturally generated live calculus statement in each language, verifies complete conditions for that selected statement, and records the actual model's automatic whack/leave event. It does not assert that automatic decisions are accurate. Root must inspect captured LCD images and preserve any runtime failures rather than relaxing these assertions.
