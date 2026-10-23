# Final release audit: private/news/art delta

Audit scope: production source read-only, plus existing release-smoke-consistency-stage checklist and prior group-art release review. No Unity, Player, model, package, upload, GitHub or production write was executed. Existing art manifest/hash tests, native notices checks, helper parser checks and save isolation tests were not repeated. See source-evidence.json for the small source files read in this pass.

## Conclusion

No newly demonstrated private/news/art production-code blocker was found in save-path protection, source inclusion or licence inputs. Do not manufacture another build patch: IntegratedReleaseBuild already keeps company/product, builds the two scenes, normalizes the single model, and records dirty/untracked Assets/Packages/ProjectSettings/Design files. The model and >16 MiB exclusions remain explicit and are not complete payload hashes.

There is one current checklist execution blocker and one integration routing gap:

1. **Wrong required version, confirmed.** release-smoke-consistency-stage/RELEASE-SMOKE-CHECKLIST.md line 23 rejects anything except 2026.10.18.1, while production IntegratedReleaseBuild.cs line 23 builds 2026.10.19.1. Following the current checklist rejects the correct build. Minimal fix: update the three literal version references to 2026.10.19.1.
2. **The current checklist still selects the old packager.** Its line 40 invokes release-audit-stage/Package-VerifiedRelease-LowLoad.ps1, which does not contain RequireBuildSource/originalArtFiles. The already-reviewed group-art/release-review candidate does. This is not a new discovery of the underlying art defect and not another packager implementation; the minimal remaining action is to point the checklist at that reviewed candidate while keeping native-licenses in its existing directory. If root has already arranged that exact script through a separate run plan, this routing gap is already handled there.

The candidate checklist and patch address only those operational mismatches and add the new feature evidence boundaries. The candidate does not authorize execution or publication and is not evidence of successful validation.

## Save and source findings

- YYChatHub.cs stores endingPrivate inside YYState, serializes the complete state into chapter chatState in WriteToSave, and guards the current bound simulation. Receipt data is independent of transcript trimming. The private send uses Touch/normal runtime dirty/save behavior; no new file path, PlayerPrefs key or export is introduced.
- TiebaHub.LaoZhouNews.cs uses the current forum/YY owners and appends receipt + authored messages synchronously. TiebaHub.WriteToSave serializes forumState during the same runtime saving event as chatState. No independent news sidecar, migration file or ordinary-save bypass was added. No claim is made that the synchronous in-memory send is itself a completed disk save.
- Existing ordinary Player --verification-save-dir rejects overlapping normal/legacy/build roots and fails closed; existing PlayerPrefs guarding is still necessary. Private/news add no protection target outside that existing scope. Do not use the older pre-isolation audit as the current launch procedure.
- CaptureSource enumerates the roots containing all added private/news source and metadata, ten JPGs, credits, and Design drawing source/manifest. Prior art audit already confirmed the relevant art files are under the 16 MiB bound; it was not rerun. Untracked status is recorded, so lack of a new commit is not a reason to pretend HEAD includes the integration.
- This manifest hashes files, not a copied checkout or a post-build stability seal. Root should keep the reviewed source fixed from CaptureSource through BuildPlayer. A source change after the build does not update its binary. This is an existing execution constraint, not a newly proven concurrent-edit defect or a reason to rewrite the builder in this audit.

## Licences and evidence

No new third-party library, downloaded image or copied external article body appears in private/news additions. News shares reuse an existing catalogue title/link and new authored reply. Ten added art images are project-original, already described by credits and source/manifest. Prior group-art/AUDIT.md and CROSSCHECK.md own licence-presence and 17-file packaging checks; this audit reuses those conclusions without claiming another binary/licence verification.

The selected packager SHA256 is 42769a446a1c9e6a8fd0732918637351bbcbf3b5a94a79c81d84774ff87aa440. It preserves all prior notice inputs and adds exact artwork/source/credits binding. A raw Unity output still is not the complete distributed notice package.

The normal bounded title smoke cannot naturally reach late-game private disclosure or all dated news and art. Preserve the actual root test reports and declared fixtures separately. Do not relabel authored private/news replies as native inference, or the bounded fresh run as all-route/private/news/art acceptance. No repeat of already-passed suites is requested by this audit.

## Deliverables and validation

- candidate/RELEASE-SMOKE-CHECKLIST.md: updated copy for use/review.
- patch/release-smoke-current-integration.patch: applies relative to .coordination/integration-20261018/release-smoke-consistency-stage; SHA256 a5555779455770727c85d2f2b4487be262d27959963bdfa13c1f66715a3cfd54.
- git apply --check passed against that original checklist. No production patch was created.
- Final build, exact-binary Player run, final packaging and payload licence inspection remain root's pending release gates; this pass did not execute or duplicate them.
