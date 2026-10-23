# 历史审查，任务建议已撤销

用户随后确认“只用支持我们现在在设计的最新版”。以下旧迁移继续完善建议已失效，仅保留源码风险证据；当前交接以 D5.md 为准。

# D5 存档迁移候选：第三轮交接审查

审查日期：2026-10-19。完整阅读 `ClaudeReturnRound2/D5` 的 4 份 C#、3 份 meta、REPORT、QUESTIONS、README 升级段，对照当前 production 的运行时、AtomicSaveStore、SnapshotArchive、PlayerProfileStore、VerificationSavePaths、槽位/分支路径及相关构建配置。仅写本报告；未改生产、未运行 Unity、未访问真实玩家存档、未编译或执行测试。

**结论：D5 可继续作为独立候选完善，但当前不能直接合入自动启动路径。** “存档我们写新逻辑”只明确了工作方向，不能代替具体迁移策略的确认。除策略未定外，候选存在目标文件冲突、崩溃恢复及链接路径方面的明确缺口。正式身份固定 `DefaultCompany/competition`；不因迁移而换 company/product，不把 QA 指向真实 LocalLow。

## 1. 当前实现及 D5 的实际增量

- 生产 `ProjectSettings/ProjectSettings.asset:15–16` 当前为 DefaultCompany/competition。普通主存档位于 `Application.persistentDataPath/LingGuangV05`；`chapter-one.sav` 使用带摘要的原子主文件，较大快照引用 `snapshot-archive/objects` 的不可变内容块；另有 `.bak`、重置/破产备份、slots、chapters、endings、player-profile。
- 生产没有 D5 的自动旧身份迁移。D5 对 ChapterOneRuntime.cs 的差异只有：首次打开 store 前调用迁移；将现有主档 parser 抽为 ParseSavePayload 共用。没有发现候选整文件额外倒退当前 runtime 的其他增量，但整合仍应交最小 hunk。
- D5 新 helper 枚举 4 个 RippleLocalPreview 身份和 DefaultCompany/competition，当前身份自动排除。按旧主文件 mtime 降序挑选，坏的跳过选下一个；只接受主文件有效，不接受仅 backup 可恢复。
- 复制整个旧 LingGuangV05 到临时 sibling，校验临时 primary 后：目标目录不存在则 Directory.Move；目标存在且根目录没有“进度证据”则逐文件 merge，不覆盖已有文件。
- Unity wrapper 拒绝 Editor、TestMode、disk off、验证目录和 SaveDirectoryOverride；普通 Windows Player 启动会自动触发，没有尚待确认策略的开关。因而“先合候选以后再定策略”不可行。
- 现有验证目录体系会在无效参数时拒绝打开普通存储；这一行为必须保持。候选 tests 使用临时目录模拟 LocalLow，本次未执行。

## 2. 明确风险与可独立修正项

### A. 合并后可能宣称成功，却留下不可读主档（高优先）

`LegacySaveMigration.cs:201–204` 只校验 staging payload。`:264–271` 对非 primary/bak/marker 的目标冲突一律保留，包括快照内容块；没有比较字节/摘要，也没有在目标校验最终载入结果。

可构造的精确情形：目标无主档/profile，但 `snapshot-archive/objects/<hash>.chunk` 已存在且损坏；旧档 staging 全部有效。Merge 跳过该块，随后发布引用它的 primary 并返回 Migrated。生产 SnapshotArchive.Get 会检查块摘要并拒绝读取，最终存档与候选成功报告矛盾。目标父路径被普通文件占据时 `kept++` 也会跳过依赖块而继续提交。

Claude 可独立修：依赖内容冲突必须 fail closed，或验证确为同一内容才复用；提交前应验证**将实际保留的目标内容构成的完整结果**。不能先发布主档、验证失败后才声称目标从未改变。不得修坏已有目标数据或删除现有玩家文件。

### B. 异常回滚不等于崩溃/断电恢复；“失败完全不动当前目录”承诺过强

`:243–294` 逐文件移动；进程中断不会执行 catch 回滚。排序中 profile/slots/blocks 先于 `.bak` 和主档，可能留下 profile 或 `.bak`，下一次 CurrentHasSaveEvidence 即跳过迁移；也可能留下部分 slot/ending。`:136` 的 stale staging 清理会删除剩余 staging，却没有 journal 来识别/恢复已发布的目标文件。

`:284–285` 回滚的 File.Move/File.Delete 错误又被吞掉，但仍返回“已撤回”，上层仍说“当前位置未改动”。这是可以独立修正的结果准确性问题；测试目前只注入可捕获异常，没有模拟重启后的半提交状态。

Claude 可独立交恢复协议候选和临时目录故障测试：必须区分 clean failure、partial publication、cleanup failure；只处理有可靠所有权记录的本次文件。不要扩大成一般存档协议重写。是否允许 merge 已有非空目标属于策略问题，待确认；在候选中明确禁用 merge 是一种待选设计，不能自行作为用户已选方案。

### C. 目标链接检查不完整，文件操作可能越过预期目录

`:135` 只查 currentRoot/currentSave 自身，未像 VerificationSavePaths 检查全部祖先；Merge 的目标现有子目录也未查 reparse point。若目标 `snapshot-archive` 或 `slots` 是 junction，EnsureDirectory 会接受现有目录，File.Move 写入链接指向的位置。源目录树有链接拒绝，目标树没有同等保护。

Claude 可独立修：读写前验证祖先和目标树、每个解析后的最终目标仍在允许根下；不要通过路径字符串前缀或 staging 哨兵推断整棵树安全。清理同样只对经验证的自有 staging，遇到异常保留诊断而非宣称已全部删除。本次没有创建 junction 或进行文件系统复现。

### D. “从未有存档”只是有限根目录标记，不是完整历史判断

CurrentHasSaveEvidence 只看根部 `chapter-one.sav*`、`player-profile.json*`、marker；只有 slots/chapters/endings 的目标会被认为可 merge。候选可能把旧 primary 与现有 slots/分支混在一起。即使它不覆盖单个文件，也不等于当前进度未被影响。README 的“只在从未有过存档时”不精确，应写实际条件，或在确认策略后扩大进度证据。

### E. 验证范围不足以称“整个旧版本完整校验”

ParseSavePayload 与当前主档 reader 一致，仅验证 GameState 基本结构及构造；它不是所有应用、lab、槽位、分支和 profile 的完整兼容认证。D5 仅验证 primary，其他复制文件主要只比较长度。测试 validator 是 `payload.Contains("\"ok\"")`，profile 用 `{"profile":1}`，部分分支文件是任意文本。它们适合文件搬运单元测试，不证明实际 ChapterOneRuntime 可完整恢复旧版本。

Claude 可独立拆清声明：字节搬运测试、主文件结构验证、实际 runtime 兼容是三个层级。补真实结构的临时夹具与待跑清单，但不要读取用户真实存档，也不要声称核心 tests 或 Windows Player 已过。

### F. README 候选有尚未实现/确认的承诺

- “首次启动会自动复制”“最近保存的那份”“出错当前位置不动”依赖未确认策略或上述缺陷；发行稿目前必须标为待决定/待验证。
- “设置无法跨版本带过来/会恢复默认一次”过度绝对。当前身份不变的普通升级并不会因 D5 主动清空当前 PlayerPrefs；从另一身份不迁移旧 prefs，也不代表覆盖当前已有设置。应写“本迁移不导入早期身份的设置”，不要写技术上绝对无法。
- README 建议玩家改名当前 LingGuangV05 来再次触发自动旧档选择，是新的手动恢复流程；在未确认选择策略且未验证前不要发布为推荐操作。
- Music2016 不在 LingGuangV05 树下，候选确实不迁移。DreamOS 按当前源码写入安装附近的数据目录，候选也不处理；不能用“删除旧游戏目录不会删除存档”掩盖尚未评估的桌面数据损失。

## 3. 哪些需要用户确认，哪些不必重复询问

不能把 D5 QUESTIONS 六条全部当已同意，也不能把技术验证事项都推给用户。

| 决策/事项 | 状态与处理 |
|---|---|
| 正式 company/product | 已确定 DefaultCompany/competition；保持，不再询问是否改身份。 |
| 旧身份白名单 | 必须核实哪些真发布过。候选只对 Playability-20261008 给出较强发布依据；10-03/06/07 是项目副本推断。禁止把“目录/项目存在”当发布证明。可先交证据表，待发布者/用户确认自动迁移范围。 |
| 自动静默迁移还是显式选择/提示；多个档按 mtime 自动选且坏档退回更旧档 | “写新逻辑”未确认这些行为。mtime 会被复制/恢复改变；替玩家挑旧进度有可见结果，需合并前确认。Claude 先给简洁选项及影响，不自行定案。 |
| 主档坏但 .bak 有效；只有档案/分支；目标已有 slots/缓存等 | 候选自定拒绝 backup-only 与允许非空 merge。这些兼容/保留策略未获全面同意。应汇总为“可恢复旧档及非空目标如何处理”的具体决策，不逐个隐含默认。 |
| PlayerPrefs、音乐、DreamOS 迁移范围 | 设置是否迁哪些键、用户音乐是否纳入、桌面资料是否要保留需明确；当前可先做只读键/路径清单。不要默认直接读取注册表或替用户重置设置。 |
| 旧 WindowsReleaseBuild 覆盖身份 / NamingChecks 旧断言 | 已确认正式身份，因此防止工具改回旧身份是技术一致性工作，无需用户再决定“要不要保持”。Claude 只交独立最小 hunk；root 统一处理构建入口，不交给迁移包整文件覆盖。 |
| QA ProtectedRoots 扩展 | 保护可能真实存在的旧目录是安全边界；可以独立交最小候选，不意味着批准从这些目录迁移。验证路径继续 fail closed，不能为了测迁移放开真实目录。 |
| Windows Player 验收 | 执行责任在 root 串行设备 owner，不是产品偏好问题。仅临时/明确隔离的合成历史目录，不能把正式真实存档用作试跑目标。 |

建议向用户只提交一组可审阅的迁移策略选择：来源白名单、自动/手动与多档选择、目标/恢复行为、附带资料范围。已有明确答复沿用；外部制作人的泛化答复不能扩展为全部授权。

## 4. 给 Claude 的独立工作包

建议第三轮分给 Claude：**“D5 迁移候选的安全修补与策略表，暂不接启动钩子”**。

允许修改交付目录内的 LegacySaveMigration.cs、LegacySaveMigrationTests.cs、D5 报告/升级文案；Runtime glue 只交最小可选 hunk。任务包括：

1. 修 A/C 的确定缺口、B 的结果误报；为非空目标与中断恢复交可审阅候选设计。策略未定时不要激活普通 Player 的自动迁移。
2. 增加临时树测试候选：同名损坏块、文件阻挡依赖目录、目标子目录链接、半提交后重入、回滚自身失败、slot-only 目标。测试不能访问实际 AppData/注册表，不启动 Unity；需要 root 执行的用例明确“未运行”。
3. 将当前猜测身份与已发布证据分开；把选择策略作为 QUESTIONS，保留已有正式身份。
4. 更新 README 为事实准确的候选，删去未验证成功保证和绝对设置重置说法；列出正式发布前必须填的验收项。
5. 输出 files、最小 diff、基线 SHA、REPORT、QUESTIONS 和单独验证清单。不得合生产、commit/push、搬动真实存档、修改 ProjectSettings 或实现注册表扫描。

root 保留：最终策略确认、CurrentHasSaveEvidence/启动入口整合、构建身份修正的统一合并、Unity 编译/隔离磁盘/普通 Player 流程、最终发布 README。DreamOS 存储迁移不并进此工作包；先交证据，另行界定范围。

## 5. 最小必要源文件清单

交接需带 D5 原包全部 10 个文件（含文档/meta），再带下面生产只读基线和 SHA manifest。以下均相对 `competition-latest-20261016/`；不带真实存档、Library、模型或整个工程。

**迁移核心必须**：

- `Assets/LingGuangV05/Runtime/ChapterOneRuntime.cs`
- `Assets/LingGuangV05/Runtime/ChapterOneRuntime.Slots.cs`
- `Assets/LingGuangV05/Runtime/ChapterOneRuntime.Endings.cs`
- `Assets/LingGuangV05/Runtime/ChapterOneRuntime.SnapshotTransfer.cs`
- `Assets/LingGuangV05/Runtime/Persistence/AtomicSaveStore.cs`
- `Assets/LingGuangV05/Runtime/Persistence/SnapshotArchive.cs`
- `Assets/LingGuangV05/Runtime/Persistence/PlayerProfileStore.cs`
- `Assets/LingGuangV05/Runtime/Persistence/VerificationSavePaths.cs`
- `Assets/LingGuangV05/Core/Models.cs`
- `Assets/LingGuangV05/Core/ChapterOneSim.cs`
- `Assets/LingGuangV05/Core/Prologue/PlayerProfile.cs`
- `Assets/LingGuangV05/Core/Prologue/PrologueProfile.cs`
- `Assets/LingGuangV05/Runtime/LingGuangV05.Runtime.asmdef`
- `Assets/LingGuangV05/Tests/PersistenceEditor/LingGuangV05.Persistence.Tests.asmdef`

**必要测试参考（读现有约束，避免重写已过功能）**：

- `Assets/LingGuangV05/Tests/PersistenceEditor/SaveStoreTests.cs`
- `Assets/LingGuangV05/Tests/PersistenceEditor/RuntimeDiskTests.cs`
- `Assets/LingGuangV05/Tests/PersistenceEditor/VerificationSavePathsTests.cs`
- `Assets/LingGuangV05/Tests/PersistenceEditor/EndingArchiveDiskTests.cs`
- `Assets/LingGuangV05/Tests/PersistenceEditor/BankruptcyRestartTests.cs`
- `Assets/LingGuangV05/Tests/PersistenceEditor/OpeningSkipTests.cs`

**只读身份/附带资料证据**：

- `ProjectSettings/ProjectSettings.asset`（可仅导出当前 companyName/productName 及证据位置；禁止改）
- `Assets/LocalBuild/Editor/WindowsReleaseBuild.cs`
- `Assets/LocalBuild/Editor/NamingChecks.cs`
- `Assets/DreamOS - Complete OS UI/Scripts/Data/DreamOSDataManager.cs`
- `Assets/LingGuangV05/DesktopBridge/Music2016.cs`

这是代码审查/候选修补用的最小上下文，不是假称可独立构建的完整 Unity 项目；若 Claude 发现确需额外依赖，列出精确缺项再由 root 补给，不自行扩大到全盘/旧项目扫描。

## 6. 交接用短结论

D5 未合入、未编译运行；保留“写新逻辑”的方向，但当前来源名单、自动选档、backup-only/非空目标、设置与桌面资料范围未全部确认。Claude 可先独立修文件事务/路径验证候选和文案，root 保留策略与实机整合。身份保持 DefaultCompany/competition；任何 QA 不碰真实存档。
