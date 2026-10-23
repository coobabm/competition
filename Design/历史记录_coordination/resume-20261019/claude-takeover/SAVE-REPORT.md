# E1 接管整合报告

2026-10-19。生产目录：`competition-latest-20261016`。本次只编辑下列五个工程文件及本报告；没有启动 Unity、模型、Player，没有编译、执行测试、构建、commit/push，没有访问注册表、正常 PlayerPrefs 或用户存档。代码已整合，运行验收未完成。

## 基线与输入

已读 `Design/Claude第三轮协作分工_20261019.md`，并核对暂停后交接及纠偏记录。遵守当前正式身份 `DefaultCompany/competition`；不接入 D5 迁移，不扫描或搬迁旧版本目录，不改变备份保留规则，不覆盖其它既有本地修改。

`ClaudeReturnRound3/E1` 实际只有 README 增量与五个 files；没有 REPORT.md、QUESTIONS.md 或 Round3 总 README。候选未提供执行证据。本次逐项对照当前工作区，不把候选描述当执行事实。

开始时三个既有目标文件均无本地修改，其原始 Git blob：

- `Assets/LingGuangV05/Runtime/ChapterOneRuntime.cs`：43389bed0225940e37bb4f7923456229d04c5f01
- `Assets/LocalBuild/Editor/WindowsReleaseBuild.cs`：17c1176a7b1fea24bd5f5c04c9026b63fe69bed7
- `Assets/LocalBuild/Editor/NamingChecks.cs`：074f5fbc3c24d1e9352bfe38e3f0acf622c5f767

## 整合内容

1. `ChapterOneRuntime.cs`：破产重开时，先复制旧档；若 restartChosen 标记无法保存，撤回内存标记、保留原 Sim、返回 false 并显示失败，允许重试。标记保存成功但新局首次保存失败时，已发生的重开仍返回 true，新局保持 dirty，但明确显示尚未写盘。初始化/重新读档遇到已提交的重开标记时同样保留失败说明。另修候选遗漏：只有 player-profile 而无主档时，首次自动建局保存失败不再被“尚无存档”消息覆盖。
2. 同文件：未采用候选的实例路径优先级改动：保持原有命令行验证根 > Editor 全局 QA 根 > 实例目录优先级，非法显式验证参数仍在打开 store 前抛错。对应候选优先级测试未整合。TestMode/useDiskSave=false 后不再从 FinishPrologue/KnownProfile 写 player-profile.json；RecordName 已有相同保护，未重复改动。
3. `WindowsReleaseBuild.cs`：两个旧入口保留名称，但立即无副作用抛出说明，指向 IntegratedReleaseBuild.Run。移除永久修改身份与旧构建输出的执行体。未改 IntegratedReleaseBuild、ProjectSettings 或发行身份。
4. `NamingChecks.cs`：仅把旧测试版产品名断言改成 DefaultCompany + competition 双断言。未采用候选的 Unicode 字面量改写，减少无关 diff。
5. 新增 `Tests/PersistenceEditor/SaveFailurePropagationTests.cs` + `.meta`：采用候选 8 个临时目录案例，补 3 个启动/重新载入/仅 profile 首次写盘失败案例，共 11 项；含英文失败提示检查。没有新增 asmdef。新 GUID 在生产 Assets 中只有一处。

## 调用链和覆盖边界

| 行为 | 当前源码链 | 覆盖/结论 |
|---|---|---|
| 手动保存/自动保存 | SaveNow；TryAutosave -> SaveNow -> Saving 收集 -> AtomicSaveStore.TrySaveSnapshot | 收集失败返回 false，旧主档不替换。候选测试验证失败字节不变。自动保存 300 秒且空闲 2 秒门禁未改。 |
| 手动档位保存/读取 | SaveSlot；LoadArchive -> 校验和构造 -> 主档写成功 -> Attach | 加入测试确认载入不调用旧 Saving 收集器；后续异步回调仍依赖各 app 的 Sim 身份保护，此测试不宣称覆盖全部 app 回调。 |
| 关机回标题 | GameExit.ReturnToTitle -> SaveNow -> 失败则 toast/return | 已有门禁，不重复修改。 |
| 标题退出 | CaseMenuController.QuitGame -> SaveNow -> 失败则显示错误/return | 已有门禁；源码随后有 PlayerPrefs.Save，本次没有执行此路径或访问正常 prefs。 |
| 系统关闭/暂停 | OnApplicationQuit / OnApplicationPause -> SaveNow | 仅尝试保存，不能把系统窗口关闭认定为可拦截事务。未改。 |
| 破产重开 | CopyForRestart -> 标记 SaveNow -> BeginNewGame -> FinishPrologue -> SaveNow | 本次修复标记失败与首存失败传播；保留 .gameover 副本。 |
| 重置 | ArchiveForReset -> BeginNewGame -> dirty 时 SaveNow | 原本会返回失败；新增回归确认 .reset 副本不会自动恢复旧进度。保留 chapter 首次解锁快照。 |
| 主档/备份恢复 | TryRead -> AtomicSaveStore.Load | 备份恢复与 recoveredBackup 保留策略未改；双损坏写保护未改，均有候选回归。 |
| 章节/结局 | CaptureChapter；CaptureEndingChoice/CreateEndingBranch/ResumeEndingBranch | 章节首次快照不被重置覆盖；结局已有 expected Sim / endingArchiveBusy / checkpoint 校验。未改产品规则；既有 EndingArchiveDiskTests 仍需统一执行。 |

新增 11 测试全用 NewTemp 生成目录，测试清理/故障注入只涉及夹具拥有的文件；Open 在 inactive GameObject 上先设置目录再初始化；Setup 明确断言本进程未传 --verification-save-dir，避免命令行优先级把临时夹具导向共享 QA 根。运行该 NUnit 程序集时不要传此参数。未在此进程运行这些测试。

## DreamOS / README 源码核对

`DreamOSDataManager.InitializeProfile` 在显式验证根存在时使用 DesktopProfile 子目录，普通模式仍由 Application.dataPath 推导 DreamOS_UserData。它与主档不是同一文件树，新解压目录可能失去下载列表/界面记录。`LingGuangInstallFlow` 的显示门禁取 Sim.AppInstalled，Reconcile 将未安装的主档状态同步到浏览器下载记录；因此主档与桌面下载记录权威不同，不能因记录缺失自动搬用户文件。未修改这两处文件。

E1 的发行 README 是候选增量，含“若并入 D7”的未决占位；本次没有改发行稿。主控可采用“只支持当前最新版、不迁移早期测试版”“游戏内保存失败不继续退出”说明，但必须保留最终 Player 验收未完成与当前 DreamOS 路径边界，不能照搬占位文字或宣称整体验收通过。

## 静态验证和待执行

已完成：候选/生产 diff 审核、调用链核对、11 个测试名/目录隔离/程序集引用检查、GUID 唯一性检查、`git diff --check`（三个既有修改文件）无错误。现有文件 diff 为 ChapterOneRuntime 33+/14-、NamingChecks 2+/1-、WindowsReleaseBuild 11+/47-。

未做：Unity 编译、NUnit、Editor UI、Player 保存/退出/重载、实际权限/磁盘故障测试。推荐主控统一执行 `LingGuangV05.Tests.SaveFailurePropagationTests`，并回归 `LingGuangV05.Persistence.Tests` 全程序集（尤其 BankruptcyRestartTests、OpeningSkipTests、RuntimeDiskTests、EndingArchiveDiskTests、VerificationSavePathsTests）；之后再做隔离 Player 验收。不把本次静态检查写成测试通过。

当前未发现需要新设定决定的冲突。IntegratedReleaseBuild 仍仅保持当前身份，不主动强制身份；主控正式构建前应确认 ProjectSettings 的 DefaultCompany/competition，并运行修正后的 NamingChecks。该构建器归主控，本次未修改。

## 整合后 SHA256

- ChapterOneRuntime.cs：6779B94AB4D941D93CDE03EB286CF0AC8D8C28E23CC7F0F8F06E69D8BE02E7E1
- WindowsReleaseBuild.cs：A98D88EC5AF68D4B6AEBE0C7650C61194CE95816FE3E9C6AC1B2960037FA6D01
- NamingChecks.cs：0E3CF935E450A1B4DC0B99126023F1B563EA7894BBAFC39F30BBFA31C9C2B57F
$12A998A230CB491D598300EFCB1124DED2093D441825D7D9CEE7FD0BBF283ECF7
- SaveFailurePropagationTests.cs.meta：93438560FE34409ABCCD11074D3E279FCF1FAF889488723A78B53566CEDD5204


