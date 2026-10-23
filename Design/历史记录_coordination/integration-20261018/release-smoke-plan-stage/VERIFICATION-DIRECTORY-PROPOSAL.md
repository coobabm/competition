# verification 目录开关方案（待 root 选择实施范围，未改生产）

后续 root 指定 `--verification-save-dir` 并要求最小文件存档隔离；已按该范围交 ../release-verification-dir-stage/ 静态候选。本文件保留前期设计与偏好/native证据边界，不是当前候选的操作说明。当前以候选 README 为准。

最新方向：不创建 Windows 账户。此前 RELEASE-SMOKE-CHECKLIST 的账户路线暂停，不执行其中启动命令。下面是同账户、显式 CLI 的候选设计报告；开关尚不存在，不能现在拿该参数启动普通包并声称隔离。

## 建议入口与 fail-closed 行为

普通 Windows 包保留一个 inert-by-default 的 `--verification-root <absolute-new-directory>`，不依赖 RECORDING_PREVIEW。正常启动没有该参数时路径与行为完全走原实现。解析/校验逻辑放 Runtime 可引用程序集，所有持久化入口第一次访问时都同步确保已解析；不能只依赖多个 BeforeSceneLoad 回调的未定义顺序。

显式开关必须唯一、有一个非空绝对本地目录值；拒绝驱动根目录、UNC、已有目录（初次运行）、源/发布目录内部、真实 persistentDataPath 及其父子路径、路径中任意 reparse point。创建一个全新的叶目录并写 run manifest（nonce、启动UTC、exe路径、company/product）；失败时明确终止初始化，不能退回原用户目录。目录包含 `Save/`、`DesktopProfile/`、`ModelCache/`、`Evidence/`。同进程返回标题/加载Main继续使用已验证根；跨进程复用仅作为另一个明确 `--verification-resume` 加 manifest 校验的后续功能，不混入最小首跑开关。

最小文件存档隔离必改点：

1. `Runtime/ChapterOneRuntime.cs` 在构建 AtomicSaveStore 之前采用 verification Save。Slots、PlayerProfileStore、备份/归档均基于 store.Folder，自然随根隔离；仍需审计 SnapshotTransfer 的显式外部导入/导出，并在 smoke 禁用跨根导入/导出。
2. `DreamOS - Complete OS UI/Scripts/Data/DreamOSDataManager.cs` 的 InitializeProfile 内直接采用 verification DesktopProfile，不能只用一次私有字段反射注入（该方法以后可能重算）。保留 profileID 的子目录但限制为文件名安全值。
3. `LocalLLM/LocalEmbedder.cs:80` 的 embeddings 缓存也必须进 ModelCache；只改存档会漏掉这条 Application.persistentDataPath 写入。
4. 所有检查产物都进 Evidence；模型只读 StreamingAssets 的原打包 GGUF/native 库，不复制模型伪装最终包，也不启动第二套模型服务。

所有初始化错误要在加载可写 scene/runtime 前 fail closed；仅 Application.Quit 异步退出而继续载入 Main 并不够。建议 central 状态区分 disabled/enabled/invalid，invalid 的各写入口直接拒绝，启动协调器再退出。检查后再创建/写文件；无删除/覆盖/目录搬移操作。

## 文件存档之外的偏好边界

同账户开关若只改上述三条路径，**不能声称玩家配置也未触碰**。已查到正常启动/打开窗口就可能写 PlayerPrefs：

- `CaseMenuController` 音量/动效/全屏与 Save；
- `PlayerWindowSize.Resize` 首次窗口尺寸标记（AfterSceneLoad 自动）；
- `GameMusic` 默认设置标记（打开音乐自动）；
- `DesktopLanguageController`、`SparkMenuLocaleBootstrap` 语言；
- GamePerformance、GuideNote、YYFacePicker、XgTechTreePage；
- Reach 的 LocalizationManager/Slider/Switch/Selector/Dropdown 等潜在场景组件。

若 root 要求**游戏拥有的偏好也隔离**，需加正常模式转发 UnityEngine.PlayerPrefs、verification 模式使用独立 JSON 的小 facade，并为构建两场景可达的上述调用加显式 alias。不能全局定义同名 PlayerPrefs 偷换所有第三方程序集，也不能靠禁用标题/音乐组件来跳过真实试玩。先做可达组件审计，未接管调用应让检查失败。参数读取不应依赖 PlayerPrefs。

Unity 原生层自身是否在启动/退出保存 Screenmanager 等系统偏好也要实测：托管 facade 不能保证接管引擎内部注册表写入。因此同账户方案能承诺的是经审计的游戏存档/桌面数据/模型缓存（以及接管的应用偏好）；如要求整个 HKCU 无写入，需原生进程级隔离或其它外部沙箱，当前不把它冒称为 CLI 已实现能力。

## 真实 native 回应与串行取消的最小增补方向

当前 plain Player 缺逐请求证据。复用 SerialNativeQa 的 actual-task 观察与回调计数，增加独立 opt-in `--verification-native` 阶段，必须先有有效 verification root。观察 **当前 Main 的 LocalLlm.Instance**，不调用 Editor-only StartEditorQa、不创建另一个服务、不伪造聊天消息。

记录当前服务模型/架构/CPU模式/slot数、实际 Chat 回调正文与 failure；对一条 synthetic Judge 用独立 owner 通过生产 Judge/CancelJudges 请求取消，确保取消时 actual Task 未完成，随后 Chat 到达一次真实非空回调，并证明其 dispatch 晚于原 Task 结束；保留 native 调用和 actual task 峰值。仅该显式阶段冻结生产剧情/后台请求的方式必须注明，否则其它正常请求会混淆测量。诊断结果只证明同包 transport；YY页面那条真实输入仍需只读来源关联记录（request id / raw callback / fallback使用），不能用诊断回答填进 UI。

标题→开始→教/练/YY实际操作与音频仍复用已有外部窗口/loopback脚本。该方案目前只报告设计，没有创建/运行 probe，也没有扩大为第二套自动化框架。

## 静态/运行验收建议

先给开关解析与路径校验做纯文件负例：重复/缺值/相对/已有目录/父子/UNC/reparse 拒绝，不能触发用户路径 fallback。由 root 后续串行运行正式包验证正常无参数路径不变、有参数的所有写路径均在 manifest 根，退出后检查真实存档与游戏偏好是否保持原样（只读前后 hash/值快照，不备份覆盖）。同时记录该版本 company/product 实际值；开关不应修改发布身份。
