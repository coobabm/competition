# 终局检查点、标题档案与独立分支候选

状态：**staging 候选，未导入生产、未编译、未运行 Unity / Player / NUnit / 模型。**

依据：`staging/ending-design-implementation-audit.md` 第 1/7 节，设计稿第 155/161/162 行及冻结契约。候选基线为制作时生产文件；`package.py` 可重跑以重新生成补丁和检查基线漂移，但不要重建 baseline 覆盖现有候选。

## 具体行为

1. 正式 `ChooseRoute` 和 `AskAi` UI 入口在修改路线前调用 `CaptureEndingChoice`。要求真正的 asked 阶段、末页/SI/记忆回执、未选择路线、未发布、未完成且无旧 E* 选择。先调用既有 Saving 采集完整应用状态，再以既有 `AtomicSaveStore.TrySaveSnapshot` 写独立不可变快照并回读比对，成功后才执行原 flow。自动快照不占 1–6 手动槽，也不替换当前主档。
2. `GameState` 新增两个可缺省的字符串 `endingCheckpointId`、`endingBranchId`，没有改版本号、没有第二套进度格式。checkpoint ID 与真实选择同一次 SaveNow 落盘；选择失败则回滚该 ID。快照失败则不选择。新快照每次生成独立 GUID，不覆盖以前快照。失败可能留下未关联快照或 `.pending` 文件夹，但不会把它们列为完成/分支。
3. 标题增加“结局档案”按钮。电源/继续、读取存档后继续、在预载期间点击继续，均在实际完成档上转档案。只认 `chapterComplete + CanEnterEpilogue + completed phase + completionReceipt + 合法已开推荐信` 的新通关；旧 E1/E2/E3-1/E3-2 保留旧编号，**不推测新路线**。旧已选但未完成档不拦截到新档案、不改原字段。
4. 标题只读回看直接给既有 `XgEpilogueView` 传 release/epilogue 副本；三封公开信以资源正文加独立滚动视图显示。全部回调验证当前 runtime/Sim、原 lab JSON 与视图世代。不启用 Main 宿主、不进入 director 的发布/邮件/完成动作，也不调用 SaveNow。公开三信可读不代表三条路线均通关；行状态仅来自该存档的真实完成记录。旧结局此候选只提供保留原编号的只读记录，**未重演旧 Finale 演出**。
5. “从选择前继续 · 建立独立分支副本”仅在来源通关档关联的真实检查点可读、仍是选择前状态时可用。复用完整 Load→JsonUtility→ChapterOneSim→TrySaveSnapshot，而非复制 `.sav` 头文件。分支 profile（如确有）和存档先写 `.pending`，回读完全相同后再发布目录，最后切换 runtime store/profile/Sim。早期没有序章 profile 的实际旧档不被阻止、不伪造 profile。
6. 分支正常存盘、自动保存、ReloadSave 写其独立目录；原通关主档和原 6 槽不变。返回标题/重启默认仍载原主档，**没有活跃路径指针文件**；标题档案的分页“继续独立分支副本”可显式恢复分支（包括已完成分支）。选择已完成分支后继续会再打开该分支档案。
7. 切换前重建和验证；重绑事件若失败，恢复旧 store/profile/Sim/dirty 状态。档案操作期间阻止重入 SaveNow/SaveSlot/CaptureChapter/LoadArchive/ReloadSave/Reset/破产重开。分支恢复本身不重写主档。若故障发生在目录发布后、重绑失败且回移 `.pending` 也失败，可能留下完整且可恢复的孤立分支目录；原身份和原主档仍保持不变。

## 本地目录

相对于原主存档根目录：

- `endings/checkpoints/<GUID>/chapter-one.sav`（以及同目录 snapshot-archive 块）：不可变选择前快照。
- `endings/branches/<GUID>/chapter-one.sav`（以及独立块、可选 profile）：可继续的新身份。
- `endings/branches/<GUID>.pending/`：未发布候选，不显示在分支列表。

普通保存位置/主存档默认路径不变。显式 verification-save-dir 已导入当前基线，以上目录自然落在其隔离根内。

## 验证状态与 root 下一步

- 已做：Python 打包、C# 基本括号/词法扫描、基线哈希漂移检查、生产 `git apply --check`；不等同 C# 编译。
- **已写未运行：25 个 NUnit case**：16 个 `EndingArchiveDiskTests` + 9 个 `XgEndingArchiveRulesTests`。临时文件夹均由测试独立生成；包括真正 >2MiB 块档、源块删除后目标仍可读、源缺块拒绝复制、前置快照验证、六手动槽保留、原 profile 保留、重绑失败回滚、过期身份、目录穿越拒绝、旧档无 profile，以及三条真实 flow 完成与旧 E* 规则。
- root 串行执行新增测试与已有存档/终局回归；随后真实 Main UI 中分别点玩家路线与 AskAi，核对选择前自动快照 phase 与主档选择事务；故障注入确认无路线变化、重试可用。
- UI 实验需检查：中英标题与小窗、键盘/鼠标/触屏滚动，完成→返回标题→继续打开档案，完整尾声回看/三信前后正文与主档 hash/资金/邮件/公开回执不变（排除现有启动/退出主动保存造成的时间锚变化），分支创建/保存/重新启动后恢复，以及原 completed 主档+六槽字节保持。
- 已有正常启动 `EnsureInitialized` 和标题退出 `QuitGame` 的保存语义未更改；“只读回看”指档案自身没有写盘，不声称启动/退出全程从不更新 `lastSeenUnix`。
- 既有手动槽行为保持：独立分支活动期间，其保存根及槽随独立 store；本候选未把原六槽导入为分支槽，也未重写所有手动读取/重置的历史语义。

## 文件范围

6 个既有路径：Core/Models、Runtime/ChapterOneRuntime、Runtime/ChapterOneRuntime.Slots、XgEndingPreparationHost、CaseMenuController、CaseMenuHud.Archives。6 个新 `.cs` 及 `.meta`。不改共享 Sidebar，不改三结局策划、公开文案资源、真实回执算法或其他代理的候选。
