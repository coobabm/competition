# Desktop fade read-only review and regression candidate

Status: source/assets reviewed; candidate compiled; Unity NOT_RUN by this subagent.

Reviewed `UserManager.ShowDesktop`, `HideDesktop`, `DisableDesktopAnimator`, Main's desktop Animator, and the three Desktop animation clips. The actual controller has independent In/Out/Instant Out states with no transitions, speed 1, no looping. In/Out each last about 1/6 second. In ends with CanvasGroup alpha 1, interactable true, blocksRaycasts true; Out ends with alpha 0 and both input flags false. Main uses Normal update mode and AlwaysAnimate. The change's normalized-time completion criterion is consistent with these actual assets. No confirmed production defect found in the changed logic. A replacement controller, zero/negative speed, or permanent pause is outside the existing scene contract; do not impose timeout-driven forced alpha as a product fix.

The independent candidate is `LocalVerification/DesignPass/ResumeLessonCompile/DesignWallpaperSmoke.DesktopFade.candidate.cs`. No Assets or main-framework change was made for this candidate. Full Editor source list plus candidate compiled successfully using Unity's bundled Roslyn without launching Unity. Log: `LocalVerification/DesignPass/ResumeLessonCompile/LocaleBatch/desktop-fade-editor.log`.

Actual-frame cases: normal show/hide/show; duplicate show and duplicate hide; cross-frame Show→Hide→Show; same-frame Hide→Show→Hide; Show after desktop deactivation; Normal-mode pause and resume; slow Animator.speed=.1 completion. The final controlled-speed case stays enabled past the obsolete .267-second timer and reaches the real terminal sample. It is a deterministic actual-frame timing probe, not a claim to reproduce slow startup. Every case checks final CanvasGroup alpha, input flags, Animator disabled, and persistence through an additional real frame. Time scale and speed are restored in finally. No Animator.Update, manually assigned alpha, forced normalized time, save APIs, or first-lesson clocks are used.

Outputs: `desktop-fade-frame-samples.csv`, separate `desktop-fade-result.txt`, marker `DESKTOP_FADE_REAL_FRAMES_PASS`, and source/full screenshots of hide, show-again and slow-show. Full screenshots are captured synchronously from the end-of-frame texture; image review remains with the parent.

Integration choices (parent owns):

1. After all English assertions and complete PNG confirmation, but before that iterator exits, manually enumerate `DesktopFadeFrameSteps()` with try/finally disposal. This runs after English evidence, leaving its first-minute timing evidence intact. It must precede overall Finish; do not yield the nested IEnumerator directly through the existing exception wrapper, because nested failures would bypass that wrapper.

```csharp
var fadeChecks = DesktopFadeFrameSteps();
try { while (fadeChecks.MoveNext()) yield return fadeChecks.Current; }
finally { (fadeChecks as IDisposable)?.Dispose(); }
```

2. Standalone `RunDesktopFade()` uses mode `desktop-fade`; after main isolated case 0, call `StartDesktopFadeFrameChecks()` once. This sets due to infinity and runs the same checks with its own finish wrapper. No step number needs to be claimed. Use existing isolated Main/profile/save setup; never construct another active UserManager, whose OnEnable accesses DreamOS profile data.

No source batches should be deployed during the parent's Unity run. Candidate is optional until parent confirms safe merge timing.
Deployment update: after the parent confirmed run 50 had exited and passed visual acceptance, the frozen DesktopFade candidate was copied unchanged to `Assets/LocalBuild/Editor/DesignWallpaperSmoke.DesktopFade.cs`, with valid meta GUID `44a89b02eab84f71bba53906a2ed2cd3`. The main `DesignWallpaperSmoke.cs` routes mode `desktop-fade` directly from isolated case 0 into `StartDesktopFadeFrameChecks()`, before case 11. Public execute-method entry: `DesignWallpaperSmoke.RunDesktopFade`. FirstMinute was not changed. The full deployed Editor source set compiled successfully; log `LocalVerification/DesignPass/ResumeLessonCompile/LocaleBatch/deployed-desktop-fade-editor.log`. Main SHA256: `C3D563CDFE78ED3DB17E5A842A64F8B9B3401712A5844EDF87FEEA71CDC6E2B5`; DesktopFade SHA256: `3ABA49F99444A1BE1A5D3BA7442810C96ABA4C8FC0728F8E85D34735E7C3F58D`. Both Editor sources and the new meta are frozen for the parent's Unity acceptance. This subagent has still not launched Unity.

Run 52 regression result: FAIL, supplied and run by the parent. The preserved CSV proves the expected hidden terminal state was not reached: show-held at frame 515 and hide at frame 516 both remained alpha=1, interactable=true, blocksRaycasts=true, animator disabled, normalizedTime=1.064453, stateHash=-899110164. Hide was requested after the prior end-of-frame observation. The single initial yield-null allowed the production stop coroutine to inspect the previous completed In state before the following Animator evaluation, then disable the Animator before Out could be sampled. This is a production timing defect, not a reason to weaken endpoint assertions.

Only the DesktopFade fixture was expanded here: request-before/request-after samples; actual In/Out flags, clip names, active state, timeScale and Animator.speed in CSV; failure source/full screenshots plus visual-state dump; detailed observed values in the unchanged terminal assertion. Current fixture SHA256 `36F6F73E74706709883653EB02CF8BC67A6EF873162D76B42405A584E0056AAF`. Latest complete Editor response list compiled successfully (`LocaleBatch/desktop-fade-diagnostics-editor.log`). The parent separately fixed UserManager by passing expected In/Out and waiting past a real end-of-frame sample before state-name/normalized-time completion. Run 53 remains pending; this subagent has not run Unity. The 3:014 candidate preparation is paused, and no 3:014 production changes were deployed.

## 53 号桌面动画回归完成

主代理报告 53 DesktopFade 全 PASS 且已退出：淡入/淡出终态 alpha 与输入 flags、重复调用、同帧/跨帧反向、暂停恢复和慢速动画均正确。该结果确认了主代理在 UserManager 中等待请求后的实际动画采样并核对 In/Out 状态的修复；本组只提供诊断和回归，没有运行 Unity。3:014 的 Assets 外候选准备恢复，但默认桌改动不与 Growth/Wiring 同批落地。
