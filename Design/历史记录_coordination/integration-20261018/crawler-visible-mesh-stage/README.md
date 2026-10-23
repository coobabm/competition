# Crawler visible mesh lifecycle repair

- Root cause: OpenCrawlerIntro could execute while Train (the actual mount parent) was inactive. TMP ForceMeshUpdate(true) did not guarantee material-slot buffers existed; CopyMeshInfoVertexData dereferenced a null vertices array. The same unsafe API was used in TEXT_CHANGED_EVENT.
- LoadPage now invalidates old snapshots and queues page creation. Actual content builds after visibility and one frame; snapshots wait a further frame and Canvas layout. No paid time, grabs or spider motion occurs while hidden/pending.
- Snapshots copy only vertices/colors consumed by Rewrite; validate all visible glyph indices and tolerate unused null material slots. This handles the new font's fallback allocations without catching and hiding exceptions.
- TEXT_CHANGED_EVENT schedules a geometry refresh; capture has a reentrancy guard. Close clears deferred state; page requests discard previous geometry.
- The Data-owned crawler is mounted to Train. View Update now drives it when Data is hidden but the crawler is visible, without ticking Data twice.
- Verification helper CrawlerVisibleMeshChecks.Frames(controller) is intended for the existing isolated Main fixture. It explicitly opens with Train inactive, confirms no snapshot/time/grab progress, activates Train and waits for real buffers/word rects through normal View.Update, then checks pending cancellation and reopen.

Validation performed: git apply --check --ignore-space-change passes (source files have CRLF). No Unity launch, static compile or runtime PASS claimed. Parent owns compilation/runtime check and production integration.
