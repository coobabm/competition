# Git publication audit — 2026-10-19

Read-only audit of `competition-latest-20261016`; only this report is written. No stage, commit, push, PR, release or Unity operation. `git fetch origin main --no-tags` updated remote tracking as authorized. Counts are a live snapshot while root continues delivery work.

## Remote and authentication

- Remote: https://github.com/coobabm/competition.git; PUBLIC repository, default branch main, not archived.
- HEAD and freshly fetched origin/main both `3533141a406940d571e064f3797328c7d214235a`; ahead/behind 0/0. No remote divergence at audit time.
- Git Credential Manager works. GitHub `/user` and `/repos/coobabm/competition` read-only API calls confirmed login `tianxingguoo-collab`, push=true, admin=false, maintain=false. No token was printed or saved. Commit user.name matches that login.
- `gh` is absent from PATH and common install locations. This is a tooling gap, not an authentication blocker: root can use the authenticated GitHub API or obtain gh.
- Git LFS 3.7.1 is installed and required by filters. Standard `git lfs status` trips over quoted Chinese filenames here; `git -c core.quotepath=false lfs status` succeeds. Use that override for Git/LFS publication commands when needed.

## Working-tree inventory

- 148 tracked status changes, 188 untracked files, 0 staged files; 336 total status entries.
- Root distribution: Assets 293, Design 39, ProjectSettings 2, Packages 1, .vsconfig 1.
- `git diff --name-only` finds 140 effective tracked diffs; some status entries are normalization-only. For example SparkNeuronDemoFont, Hardpixel, PC_RPAsset and UniversalRenderPipelineGlobalSettings had unchanged Git-content hashes despite status changes.
- Largest changed files: OriginSerif SDF.asset 37,773,592 bytes; new UiSansSemibold.asset 17,709,443; UiSans.asset 17,659,692; new UiSansMedium.asset 17,495,784; new MiSans-Medium.ttf 8,103,840; MiSans-Semibold.ttf 8,034,980. None exceeds GitHub's 100 MB regular-file ceiling. These font atlases/fonts are part of intended UI work; do not drop them merely because generated/large.
- `.gitattributes` routes PNG/JPG/TTF/etc through LFS; `.asset` files remain ordinary text. Ensure new media/fonts get real LFS pointer entries and referenced objects are uploaded with the branch.
- No newly untracked GGUF/DLL/EXE/archive/private-key file was returned by the suffix audit. No tracked *.gguf/*.env/*.pem/*.key found.

## Keep/exclude decisions

Keep all reviewed gameplay/runtime/UI changes and their required new partial C# files, .meta files, Resources assets, font assets, licenses, source manifests, model metadata/cards/hash, build/QA source and genuine project settings. Restricting the commit to Qwen files would produce an incomplete source representation of the delivered game. Preserve the full playable Assets delta, including existing DreamOS save isolation change and new LocalBuild QA files.

Exclude local coordination state, personal task handoffs, caches, saved games, raw verification output, generated builds, raw GGUF/native runtime payloads. Existing ignore rules cover Build, LocalVerification, LLMUnityBuild, .coordination, model GGUF/.meta and downloaded LlamaLib. Do not force-add these to source Git. They can remain locally untouched. The model may be included inside the explicitly authorized runnable release package, separate from source history.

Do not broadly add Design. It currently contains new Claude/Codex task assignment, pause/resume, handoff and environment notes, plus local machine paths. Five tracked changed Design files are: Claude交接_20261019/交接总表与提示词.md; 六小时整合决策记录_20261019.md; 整合记录.md; 老周知识策划_更新差异_20261019.md; 老周知识问答与2016科技资讯_跨会话策划交接_2026-10-19.md. Keep personal coordination edits unstaged. The public repository already contains older handoff notes in HEAD; excluding new notes does not erase history, and no history rewrite is recommended.

Curate public documentation separately: a concise factual release note, model source/hash/license instructions, and reproducible asset generator source if desired. `Design/素材源码/group-chat/draw_chat_scenes.py` is relevant authoring source; its adjacent manifest still says candidate/not imported and discusses agent/key availability, so sanitize/update that public manifest rather than blanket publishing the whole Design folder. `Design/发布材料/*` is draft promotional material with placeholders and old .4 state; do not publish it verbatim for .5. Screenshots can be selected after delivery, not treated as game runtime requirements.

`.vsconfig` is a harmless local IDE workload suggestion, not required for the runnable game. The ivory material diff changes only float serialization precision. ProjectSettings/ProjectSettings.asset adds `Standalone: 0` (Mono backend), matching the Windows build policy; retain if desired as reproducible project configuration. Packages/Aura/.gitignore and GraphicsSettings showed no effective numstat delta at audit time.

## Secret/privacy check scope

Basic credential-signature scan of changed/untracked textual code/config/docs/assets found no verified secret. The one match in Design/整合记录.md was a false positive inside an acquisition-risk-* test report filename, not a credential. Filenames rather than candidate secret values were output. This is a targeted pattern scan, not a guarantee over all historical repository content. Public-source selection must still exclude local personal handoffs and user data. API credentials were held only in process memory for the requested identity/permission query.

## Recommended publication sequence

1. Finish and deliver the tested runnable .5 build locally as user requested. Freeze its actual runtime source/assets and record package SHA256 plus build/source provenance; do not conflate historical Editor tests with final Player results.
2. Refresh origin/main once more immediately before publication. Create a dedicated source branch from current HEAD while preserving the dirty working files (no reset/stash-loss/cleanup). Curate an explicit path manifest for complete Assets changes and intended project settings, plus sanitized public docs. Leave Design task handoffs and all excluded files untouched. A temporary index/isolated source snapshot is an alternative if root needs to avoid disturbing the live index, but it must include all selected untracked assets and metadata.
3. Inspect the staged name/status and size list, `git diff --cached --check`, secret scan of staged content, and LFS pointer/object status. Commit the whole playable intended source snapshot with title/body describing the integrated final game, model switch and UI changes, rather than claiming it is a tiny model-only patch.
4. Push that branch and create a reviewable PR to main. Do not force-push or silently overwrite main. Attach created PR through the app artifact tool. No additional user permission is needed for already-authorized upload, but publication should describe actual completed checks and limitations.
5. If uploading the runnable package, create a release/tag targeting the exact published source commit. GitHub Release assets are limited to 2 GiB each: the 2.74 GB model alone already requires split archive assets or another approved host. Use volumes comfortably below 2 GiB (e.g. 1900 MB), include SHA256SUMS, extraction/launch README, notices/licenses, and verify extraction. Do not put build volumes or raw model weights into Git/LFS source history.
6. Replace draft placeholders with actual version, download URLs, volume count, checksums and commit/tag. Keep old test reports accurately labeled. Then hand the completed delivery/PR/release links and current promo-source notes to the authorized promotional chat.

No remote/auth/size blocker to source publication was found. Remaining required work is final .5 delivery/verification, curated staging/public docs, LFS upload, and real PR/release creation by root. GitHub release package splitting is a concrete upload constraint.
