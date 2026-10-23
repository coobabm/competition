# E-B public event receipts

These are the exact authored IDs from the approved source list §6. E-A/root creates and persists a receipt only after successful in-game publication and the corresponding fictional public event. No real network activity, telemetry, or automatic private disclosure is implied. E-B never grants these IDs itself. Each row also requires the matching route and nonempty successful artifact ID.

| Content | Required IDs | Event that must actually be recorded |
| --- | --- | --- |
| R1-N | `online.description_use_reported`, `online.correction_channel_published` | A fictional user report about drafting descriptions is published; the service's correction channel is published. |
| R1-F | `online.paperplane_trial_posted`, `online.changelog_published` | The authored Paper Plane trial post is published; the public service change log exists. |
| R1-Y | `online.link_shared_publicly` | The service link has been publicly shared in the authored event. |
| R1-W | `online.correction_replied_and_logged` | A public correction, retained old version, response and pending-check entry are recorded. |
| R1-M | `online.public_maintenance_listing` | The fictional public website-maintenance job listing exists. |
| R2-N | `package.derivatives_published` | Authored derivative packages/modification notes have been published. |
| R2-F | `package.derivative_missing_notes` | The authored derivative download page missing its documentation exists. |
| R2-Y | `package.requirements_published` | The successful package's public requirements are available. |
| R2-W | `package.documentation_published` | Version notes, changes and known issues are published. |
| R2-M | `package.public_installer_listing` | The fictional installer-service listing exists; this is not author endorsement. |

Without receipts the complete gated record is hidden. E-B does not generate substitute dialogue; common historical content and independently authored general discussion remain readable. R4 requires no public artifact and does not generate a claim about the player's choice. The R4-W fallback is the approved D5-W public example-site text, clearly marked as somebody else's public sample. The R4-M final narrator-only sentence is excluded.

After verified state is committed or restored, E-A/root must call:

`XgPublicDiscourseDesktop.SetReleaseProjection(actualChapterOneSim, savedRevision, displayDate, route, successfulArtifactId, savedPublicEventIds)`

The adapter refuses stale revisions for the same owner and drops the projection when the actual save identity changes. It has no persistence mechanism of its own. Use the saved epilogue display date; never advance the economy for these pages.
