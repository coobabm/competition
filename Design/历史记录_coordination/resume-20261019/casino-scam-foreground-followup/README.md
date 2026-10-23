# Casino 前台稳定性小补丁（冻结）

只改当前生产 CasinoScamActualUiRunner.Foreground 的一个 hunk：需要连续3秒真实 `Application.isFocused && !PlayerWindowState.IsMinimized` 后才返回；失焦重置稳定时钟，60秒未稳定则失败。与当前 group runner 同一边界，不伪造前台、不修改40秒八卦计时或产品逻辑。

`casino-scam-foreground-followup.patch` SHA256：`21e1f46c4df33e9ca9f450b518dd1db88bafff40327850c719a74876a317b6d7`。

真实生产 apply-check 通过；与最新 ending A runner 候选合并到仅本地验证的 Editor 编译项目，0错误、241个既有警告。日志位于 `.coordination/claude-batch-01/ending-small-A-ui/verification/editor-build.log`。没有修改生产，没有运行 Unity。现有文件无新meta。

热重载后 runner 可能停留PlayMode是已记录QA限制；本批按主控指示只修前台门槛，运行期间冻结C#规避热重载。
