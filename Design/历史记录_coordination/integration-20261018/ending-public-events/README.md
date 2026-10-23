# E-B2: persisted authored public scenes

Candidate only. No production files, Unity processes, .NET/compiler or model were run or changed by this package's author. Root owns integration, disk-save wiring, visible links and serialized verification.

## Integration

From `competition-latest-20261016`:

```powershell
git apply --check ../.coordination/integration-20261018/ending-public-events/public-events.patch
git apply ../.coordination/integration-20261018/ending-public-events/public-events.patch
```

The eight-path patch adds two Core partial files, one test file and their .meta files; it changes only the necessary declarations/copy/clear/receipt validation in `XgEpilogueFlow.cs`, and updates the old test that previously accepted an arbitrary source string. The package checks exact production baselines and the ten approved source hashes before producing the patch. The new NUnit source defines 15 cases; none were executed by this agent.

Root can expose the following existing-flow methods through its bound `XgEpilogueDirector`:

```csharp
flow.PublishAuthoredPublicEvents(binding, approvedCatalog);
flow.SavedPublicEventIds(binding);
flow.PublicSourcesForRecord(binding, recordId);
flow.FindPublicSource(binding, sourceRef);
```

Call the script operation only as the authored route scene starts, after the transition into `route` has been successfully saved. On load, `letter` and `reading` also permit the still-missing script operation so an interrupted run can resume. `date`, `headline`, `launch_night` and completed replay cannot create events. There is no creation during route selection, publication, querying, resource loading or ordinary page rendering. Public-package routes follow E-A's contract and require no online launch night.

After `Success` with code `saved`, capture a fresh binding and project `SavedPublicEventIds(newBinding)` into the desktop adapter. On `saved_detached`, do not project or open anything for the new save. Do not project raw `ReleaseOutcome.publicEventIds` or candidate data from the runtime Saving callback. Source and receipt candidates temporarily exist during serialization; the query methods deliberately return empty/null while the save is in progress. A failed or throwing SaveNow restores the exact prior release/epilogue references and completion flag. The prior disk snapshot remains the owner's AtomicSaveStore responsibility. A fresh-binding duplicate script request returns `public_events_already_created` with no second save; an old binding returns `stale_binding`.

For each R1/R2 card, `PublicSourcesForRecord` returns the persisted approved source snapshot. Show its explicit `authored_fiction` label, selected-language title/body, game display date and `sourceRef` as an in-game link. On click, re-check the original binding and resolve the exact URI with `FindPublicSource`. Never pass a saved string to `Application.OpenURL`, a filesystem loader, browser navigation or a general URI dispatcher. The resolver accepts only a current release's exact versioned `xg-public://epilogue/.../R*-*/v1` reference with matching white-listed ID, artifact, revision, timestamp and approved text hash; external, foreign-release, malformed and modified-text links return null. Returned DTOs/arrays are defensive copies.

## Persisted evidence

The existing `ReleaseOutcome` now holds `publicEventScriptVersion` and `publicEventSources`. Each of the five source documents for the chosen public route contains its full bilingual approved body/title, origin platform, stable approved-text ID, public artifact/release receipt, game display date, saved revision, script version, canonical local link, content hash and permitted event IDs. These documents and all seven online or five package receipts are committed in one atomic feature-state transaction. No separate disk format, network call, game economy tick or private-chat input is introduced.

The document bodies are exact R1/R2 authored scenes from `ending_public_discourse.json`. They are now durable objects rather than flags inferred from a route; the whitelist hashes pin the approved v1 text and prevent arbitrary supplied catalog text from becoming a source. They are **scripted fictional source scenes**, not independent empirical confirmation that real users, jobs or packages exist. There are ten authored source scenes in the script (five saved for a route), covering twelve allowed event IDs. A future text edit needs an explicit version/migration decision rather than silently changing saved v1 evidence.

An input to CommitRelease cannot smuggle sources, script version or event IDs into publication; they are cleared. `RecordPublicEvent` is retained for compatibility but now requires a valid persisted source document with matching event, artifact and date. A caller-supplied source string is insufficient. Legacy IDs without the new source evidence do not pass `SavedPublicEventIds`; this package does not silently grant or repair them.

## Approved defaults and limitations

- **Default A, confirmed by root:** online `power_cut` does not generate ongoing service/use/correction/listing scenes. The shared Transformer headline and unrelated general discussion remain available. The terminal night receipt can still permit epilogue presentation under E-A's existing contract.
- `local_only` cannot create any of these public sources, artifacts or private-choice disclosures.
- Historical review whose display date predates the release cannot backdate usage scenes; it returns `historical_review_no_public_events`.
- Completed replay reads saved source documents but never creates a missing batch or writes another receipt.
- The script never grants samples, revenue, accuracy, reputation, real downloads, messages or external publications. Root must keep the fiction label on every visible source screen.

## What is original authored plot, not measured social behaviour

| Source scene | Whitelisted events | Authored phenomenon |
| --- | --- | --- |
| R1-N | `online.description_use_reported`, `online.correction_channel_published` | A fictional description-writing use report and a published correction channel. This is not user telemetry or proof of product utility. |
| R1-F | `online.paperplane_trial_posted`, `online.changelog_published` | Paper Plane's authored trial post and the scene's change-log publication. This is not a model task benchmark. |
| R1-Y | `online.link_shared_publicly` | The scripted group scene shares a service link. It is not a sent private YY message or a real referral. |
| R1-W | `online.correction_replied_and_logged` | A fictional correction scene describes a retained old version, response and pending check. It is not an observed support incident. |
| R1-M | `online.public_maintenance_listing` | An authored maintenance-job listing scene. It creates no contract or earnings. |
| R2-N | `package.derivatives_published` | A scripted scene describes derivative versions and modification notes. No real package forks or executable downloads are created. |
| R2-F | `package.derivative_missing_notes` | An authored page scene lacks its notes. This is not a discovered external infringement or observed download site. |
| R2-Y | `package.requirements_published` | The script publishes the package-requirements scene. It is not a compatibility test. |
| R2-W | `package.documentation_published` | The authored documentation scene distinguishes original, changes and issues. It does not modify copies held elsewhere. |
| R2-M | `package.public_installer_listing` | A fictional third-party installer-service listing, explicitly not the original author's endorsement. No purchase occurs. |

## Verification boundary

Static package generation checks the exact approved hashes, twelve event IDs, baseline equality and focused patch applicability. Tests cover early phase/route-only rejection; saved JSON source bodies and gate projection; atomic rollback/exception; no candidate evidence during save; reentrancy; old revision/owner/duplicate; detached-save handling; local/power-cut; forged IDs/links/text; defensive copies; corrupt sources; completed replay; historical date. Root still needs compilation, these tests, actual SaveNow failure/reload checks and clickable-source UI checks. No runtime success is claimed here.
