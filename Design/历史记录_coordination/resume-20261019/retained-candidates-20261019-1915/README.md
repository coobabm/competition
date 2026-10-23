# Codex 保留候选与运行权交接（2026-10-19）

用户最新选择：另一路继续操作正式工程，本会话先保留候选。自该确认起，本会话不再修改正式工程、不启动 Unity、不打包或推送；以下内容保存在工程外。

## 本轮已完成

- C2 通知输入：定位 QA 的 Editor/Player 鼠标缓存错位，只修 QA 写入阶段，游戏规则不变。诊断版 `SidebarLayout/20261019-105613-794` 和移除诊断后的 `110253-787` 都 passed，分别 65 条记录、31 张截图，运行/基础设施错误 0。临时探针备份在相邻 `c2-passing-diagnostic-backup/`。
- D2 贴吧：采纳最小版本，Go 绘制后记录原刷新键，避免下一轮轮询重复重建，增加测量点。其余缓存/门禁改写没有合入。已随干净 C2 编译和执行；尚未获得新性能收益数据。
- D3 文档：七份文档合入，两篇开发日志保留生产原文的有效改动，复制六张展示图且 SHA256 一致。最终 Player/模型/音频未验的边界保留。
- DreamOS：用户 2026-10-19 明确确认“已购买，项目有授权”，已写入生产许可映射及整合记录，不重复索要订单或账号。
- 字体：另一路新增字重生成器使用内部 setter 导致 CS0200；修为原数组元素赋值。生成 Medium/Semibold TMP 并预热常规字形，构建保留缓存。生成日志 `ui-v4-font-weights-build-20261019.log` 成功。不要恢复 UiSans 到 HEAD，这会丢失字重映射。

## 已放入正式工程，但尚未 Unity 实跑的 D1

`EndingPrivateRemainingUiChecks.cs` / `EndingPrivateRemainingUiRunner.cs` 与两份 meta 已在本次运行权确认之前放入生产。审查修正四处中英参数反序；通过状态延后到编辑器恢复成功；失败截图在夹具清理前捕获。独立 Roslyn 编译通过，**不等于 Unity 实窗通过**。

接续入口：`EndingPrivateRemainingUiRunner.Run`，使用独立 runner，**不接 Claude 的 Sidebar optional hunk**。要求 16 个中英具名结果及隔离磁盘、运行/清理错误全检查。原来源和最小修补保存在相邻 `round2-d1-review/`。

## 本轮未完成，不能写成通过

- 赌场最新 `CasinoScamActual/20261019-110632-556` 12 条检查后在实际 40 秒消息等待中失焦，运行/清理错误 0，但整套未过。此前部分通过不能替代完整结果。下一次需要可持续前台环境，不能降低门禁。
- D2 新性能采样：本会话启动的 PID 22444 已因用户交出正式工程运行权而主动停止。日志 `d2-fonts-integration-profiler-20261019.log`、目录 `IntegrationProfiler/20261019-111346-808-9ae82a` 为**中止的部分采样**，不得用于通过结论或性能收益比较。
- D1 剩余私人结局实际界面、最终 Windows 构建、隔离 Player、同请求本地模型来源、真实音频、完整包许可复核、GitHub 提交与 Release 均仍待完成。
- 本轮最后 fetch：HEAD/origin main 都是 `3533141a406940d571e064f3797328c7d214235a`，0 ahead/behind；之后继续整合前重新 fetch。

## 使用此目录

`files/` 是本轮相关文件的精确留档，`manifest.json` 为 SHA256。`tracked-review.patch` 仅是 C2/D2 对 HEAD 的审阅差异，不包含全项目未提交内容。

这些文件多数**已经存在于正式工程**；不要再整文件覆盖或重复 apply。后续应比较 manifest 和现有文件，逐 hunk 合并另一路的新修改。没有回退另一路的开场/字体设计，没有改普通玩家存档，没有发送或发布到 GitHub。

正式工程有大量其他有效未提交内容，本目录不是完整工程备份或可玩包。两篇开发日志和六张图已在 `files/Design/发布材料/` 一并保留。
