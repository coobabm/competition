# OS / 资源兼容性审查（2026-10-20）

审查基线：base `c67a7db50ace0b9df6356b7c3b79b75c404e95e0`，本地快照 `c4e02003cece703b93b7d46b18e5cd86c977ffcd`，上游 `c5c63198`（`refs/remotes/ripple-audit/lingguang-v05`）。源码仓库 `C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016`，试合并仓库 `C:/Users/xomem/Documents/ChatGPT/games/competition-integration-20261020`。以下路径相对各仓库根目录。未运行 Unity、未占用窗口。

## 结论及已执行的授权修复

可以接受上游自研 OS 和旧付费包删除，但必须移植本地存档与学习门禁补丁，并迁移所有新增脚本命名空间。经主控转达用户确认“采用新版自研桌面＋保留我们修复”后，本审查代理仅修改了两处新 OS 类、删除两处旧冲突类；未提交。

1. `Assets/LingGuangV05/DesktopBridge/OS/Data/DreamOSDataManager.cs`：已移植以下本地行为。
   - `InitializeProfile()` 最先检查 `VerificationSavePaths.DesktopProfileDirectory`；防止 QA 写入真实桌面数据。
   - 非 Editor 使用 `DesktopProfilePaths.Resolve(null, PlayerDataRoot(), profileID)`；修复 EXE 文件名与 productName 不同导致数据写进 `<exe>_DataDreamOS_UserData` 的旧缺陷。
   - `PlayerDataRoot()` 正常发行使用 `Application.persistentDataPath`；`RECORDING_PREVIEW` 使用 `Application.dataPath/../UserData`。
   - `DeleteDataCategory()` 通过 `GetTempDataPath(cat)` 初始化目录/文件，再写 `{}`；不能恢复为裸 `dataPath + cat + .json`。
   - 保留上游小型重写的 JSON 读写实现、公开 API、类别枚举顺序、`profileID` 和私有静态 `dataPath`。后者仍供 QA 反射；不能改名或改“空表示未初始化”的语义。
2. `Assets/LingGuangV05/DesktopBridge/OS/Window/WindowManager.cs`：已将本地唯一增量 `StudyDesktopGates.AllowsWindow(this)` 放在 `OpenWindow()` 第一条执行语句。拦截发生于上游新增音效、Focus、isOn 状态改变之前；门禁拒绝不应发出开窗音效。保留上游其他生命周期、音效、窗口逻辑。
3. 已 `git rm` 旧 `Assets/DreamOS - Complete OS UI/Scripts/Data/DreamOSDataManager.cs` 和 `.../Window/WindowManager.cs`。不要恢复它们或建立第二套旧 namespace 包装 MonoBehaviour；新旧 meta GUID 相同会产生冲突。DataManager GUID 已验证都是 `ca4fb6cfe2f4e7c46a614cb3b86cc598`。

两新文件 `git diff --check` 通过；此为静态检查，尚非 Unity 编译/运行验证。

依赖必须保留：`Assets/LingGuangV05/Runtime/Persistence/DesktopProfilePaths.cs`、`VerificationSavePaths.cs`，以及 `Tests/PersistenceEditor/DesktopProfilePathsTests.cs`、`SaveFailurePropagationTests.cs` 等本地隔离与失败传播测试。路径辅助类明确不迁移旧发行桌面存档，这是现有本地产品决策，OS 合并不应擅自改变。

## 命名空间和窗口接口

上游从 `Michsky.DreamOS` 迁至 `LingGuangV05.Desktop.OS`，保留 `WindowManager`、`DreamOSDataManager`、`BootManager`、`UserManager` 等类名/API。`DesktopWindowIndex` 的 using 已迁移，上游新 WindowManager 仍在 Awake/OnEnable 注册、OnDestroy 注销，并保留初次打开后 Start 不再次关闭的行为。

试合并工作区抽查时，`rg -l 'Michsky\.DreamOS' Assets/LocalBuild Assets/LingGuangV05 Assets/HongmengOS -g '*.cs'` 返回 43 个文件。主控需统一替换 namespace 引用（包括完整限定类型名和反射字符串），不能只处理 using，也不能以删除 QA 解决编译失败。

实际运行脚本残留：

- `Assets/LingGuangV05/Mail2016/PublicLetterArchivePresenter.cs`
- `Assets/LingGuangV05/Mail2016/EndingLetterPresenter.cs`
- `Assets/LingGuangV05/XingGuang/Desktop/XgAppAcquisitionHost.cs`
- `Assets/LingGuangV05/XingGuang/Desktop/XgEndingPreparationHost.Narrative.cs`
- `Assets/LingGuangV05/XingGuang/Desktop/XgEpilogueMailBridge.cs`（完整限定 WindowManager 字段）
- `Assets/LingGuangV05/XingGuang/Desktop/MiniGames/XgBoardPictureDrag.cs`
- `Assets/LingGuangV05/DesktopBridge/Games/HubGame.BoardSessions.cs`

特别容易遗漏：`Assets/LingGuangV05/UI/Tests/Editor/EpilogueDesktopClockTests.cs:50,63` 的 `Desktop("Michsky.DreamOS.DateAndTimeManager")` 是反射字符串，编译成功也会运行失败。`Assets/LocalBuild/Editor/BrainWindowPlayQa.cs` 冲突应保留本地 `VerificationSavePaths.DesktopProfileDirectory`，同时将 typeof 改到新 OS namespace。BoardGamesUiPlayQa、LaoZhouQuestionsUiChecks、LifeResultUiPlayQa、PlayerKnowledgeMenuInputQa 等同样含完整限定类型名。

## 删除素材与本地依赖

用 git 对上游删除的 `.meta` 提取 GUID，并与上游全部 Assets `.meta` GUID 去重，再扫描 base→本地快照改变的 `.asset/.mat/.unity/.prefab/.controller` 的 GUID 引用：未发现这些本地改变的 YAML 指向“被删除且上游没有替代 GUID”的资源。4278 个被删 GUID 在上游无对应资产，但本次本地改变的 YAML 没有引用它们。这不是全工程 Missing Reference 保证：未覆盖全部未改资源、运行时字符串路径或 Unity 导入语义。

付费包删除涉及 DreamOS、Feel、Reach、HolenderGames。不要看到文件被删就恢复旧素材：上游有同 GUID 的 `DesktopBridge/OSArt` 替代物。两处具体旧路径已在上游修正：

- `Assets/LocalBuild/Editor/WindowRegistryLifecycleChecks.cs` fixture Animator 从旧 DreamOS `Animations/Apps/App Window/Window.controller` 改到 `Assets/LingGuangV05/DesktopBridge/OSArt/Animations/Apps/App Window/Window.controller`。本地新增 QA 使用该 helper 时应保留新版路径。
- `Assets/LingGuangV05/CaseMenu/Editor/CaseMenuSceneBuilder.cs` 的 Hover、SwitchPress、SwitchRelease、Boot 音效已改为 `Assets/LingGuangV05/CaseMenu/Art/Audio/` 自有文件；不要从旧 Reach/Feel 路径重新建场景。

字体删除主要是包内 LCR、OpenSans 多字重、Feel 示例 Lato/RobotoMono/BigJohn、Holender Hardpixel、Reach Rajdhani。部分 OpenSans/TMP 资产已迁到 OSArt；本地 MiSans UIv4 源字体没有被上游删除。必须保留本地 `UiSansMedium.asset`、`UiSansSemibold.asset`、MiSans-Medium/Semibold.ttf、`XgUiV4Fonts.cs` 字重接口及 `Editor/UiV4FontWeights.cs`。

### 两个字体冲突的技术判断

`Resources/LingGuangV05/Fonts/UiV4/UiSans.asset` 三方 source GUID 都是 `75f62267335fcdb4daa0aff166dca296`，均为动态 atlas、2048 尺寸、允许多 atlas。base/本地/上游字形数分别 1116/1402/1151，说明很大部分 diff 是运行中动态缓存；但本地 weight table 有实质功能：Medium GUID `5a546f7d6d4db224ca003bdf921f9687`，Semibold/Bold GUID `3235bdc6734e6094584b6e30c99f8cb9`。上游 table 全空，整文件 theirs 会退回伪粗体。建议保留本地 weight table 和完整配套源字体；缓存不要逐项拼 GlyphRect/Texture 十六进制，选完整一致 atlas 或后续在 Unity 重建。

`Localization/Fonts/Desktop CJK.asset` 三方 source GUID 都是 `bcefe69df9a2240f1841ef848779f9ac`，都是动态 atlas、2048、多 atlas、ClearDynamicDataOnBuild=1，weight table 均空。本地字形数 0、上游 635，表面属于动态缓存差异；选择有效完整资产后由 Unity 重建即可，不能把该冲突当作替换中文字体的授权。用户仍需按既定冲突流程确认。

## Voice 归档

上游将 Resources 下原始 `Voice/generated`、`Voice/me` 和 Prologue 的中英文 shutdown_voice 移到 `Assets/LingGuangV05/VoiceArchive/`，避免随 Resources 打包。`DesktopBridge/Story/VoiceLines.cs` 保留旧 Resources lookup 但说明默认走 BlipTalk；`Runtime/PrologueContent.cs` 的 ShutdownVoice 也明确允许归档后返回 null。

`Assets/LingGuangV05/UI/Tests/Editor/MonologueVoiceTests.cs` 上游已变为验证录音不打包、Find 返回 null、BlipTalk 正常生成。整合时不能恢复旧“必须有录音”的断言或把 VoiceArchive 移回 Resources。需要产品层面保留特定录音时另行明确，不应静默恢复整包。新旧 Qwen 模型内容与此资源迁移无关。

## Build 与验证路径

正式入口仍为 `Assets/LingGuangV05/Scenes/ComputerCaseMenu.unity` → `Assets/LingGuangV05/Scenes/Main.unity`，无须改为 `Assets/HongmengOS/Scenes/HongmengOS Desktop.unity`（后者是 OS 场景）。保留本地 `Assets/LocalBuild/Editor/IntegratedReleaseBuild.cs` 两场景显式数组、save identity、Mono/D3D11 设置、source-provenance 以及模型校验。`ProjectSettings/EditorBuildSettings.asset` 还有示例场景，发行构建应继续使用显式两场景列表。

当前 IntegratedReleaseBuild 输出 `Build/MyComputerAlive-<version>-<stamp>/MyComputerAlive.exe`，Data 路径 `MyComputerAlive_Data/StreamingAssets` 与校验一致；OS 迁移本身不要求改输出路径。版本字符串可由主控正常更新，勿为 OS 改 productName/companyName，避免真实存档根目录变化。

最终模型必须保留 `LocalModelRelease` 的 `Qwen3.5-4B-Q4_K_M.gguf`，2740937888 bytes，SHA256 `00fe7986ff5f6b463e62455821146049db6f9313603938a70800d1fb69ef11a4`。保留发行构建“恰好一个 canonical GGUF”及构建前后 hash 校验，不接受上游可能遗留的旧 LLM 配置覆盖。

建议主控后续验证顺序：先清零源码中的旧 namespace/旧资产路径并解决所有冲突；EditMode 跑既有 Persistence、窗口 registry、门禁和 voice 测试；再验证 Main 实际开关窗、学习门禁、存档隔离；最后正常 Player 构建验证 renamed EXE、真实 persistentDataPath、Qwen payload。此次审查和授权移植没有运行上述 Unity 测试。

## 第二轮静态编译兼容审查（主控完成 namespace 替换后）

本轮只读审查，未再改源码。对 79 个新 OS 脚本与本地快照内同名旧脚本提取公开/受保护成员名及方法签名（返回类型、参数类型、static），未发现被删除的公开成员名或不再匹配的方法签名。该检查为文本静态分析，不等于完整 C# 编译器验证；已重点人工查看新增运行代码涉及的 WindowManager、DreamOSDataManager、WebBrowserManager、UserManager、BootManager、DateAndTimeManager 调用。

- Assets 下 11 个 asmdef 均可解析，项目内引用无循环；引用名均能归入现有项目程序集或已声明 Unity 标准包程序集。没有 OS asmdef/asmref，`DesktopBridge/OS`、本地新增桌面/邮件/小游戏脚本继续属于 Assembly-CSharp；UI 测试按 `..., Assembly-CSharp` 反射的模式仍成立。
- `LingGuangV05.Runtime.asmdef` 仍 `autoReferenced: true` 且只引用 Core；新 OS 引用 `Runtime.Persistence` 是 Assembly-CSharp→Runtime 的合法方向。Runtime 源码无对 Desktop/OS 的直接 using，未形成反向程序集依赖。不要为这次迁移额外给 OS 建独立 asmdef，否则现有窗口门禁/桌面注册及反射假设需整体重构。
- Persistence.Tests 保留 Runtime/Core/TestRunner/NUnit 引用与 Editor/UNITY_INCLUDE_TESTS 条件；`DesktopProfilePathsTests` 为纯路径测试，`SaveFailurePropagationTests` 引用 Runtime/Core，未直接引用 Assembly-CSharp 的 OS 类型。真实 DataManager 首次清空验证位于 `Assets/LocalBuild/Editor/ClaudeTakeoverChecks.cs`，属于 Assembly-CSharp-Editor，可访问新 OS；其反射字段 `resolved/root/failure/dataPath` 均保留。
- 全 Assets 的 `.cs` 再扫描没有 `Michsky.`、`MoreMountains.`、`HolenderGames`、`UpgradesTreePro` 或旧 DreamOS/Reach/Feel Assets 字符串路径残留。Newtonsoft.Json 所需 `com.unity.nuget.newtonsoft-json` 仍在 manifest（3.2.2）；删除 Feel/DreamOS 没有移除它。
- `BrainWindowPlayQa` 已正确保留 `RequireIsolation()` 与 `VerificationSavePaths.DesktopProfileDirectory`，反射类型已换新 OS。`EpilogueDesktopClockTests` 的 DateAndTimeManager 反射字符串也已换新 namespace。
- `VerificationSavePaths.Root` 的失败仍缓存并反复抛出；移植后的新 DataManager 在算正常存档路径前访问该属性，因此显式隔离参数失败不会退回真实存档。LocalLlm 初始化仍访问 Root，Music2016 仍优先 MusicDirectory。OS 修改没有绕过这些入口。
- 运行测试提醒：`SaveFailurePropagationTests.Setup` 明确要求不传 `--verification-save-dir`，因为该 fixture 自建临时目录；它与需要显式隔离参数的实际 UI/Player QA 不能机械共用同一启动参数。`ClaudeTakeoverChecks` 会暂时注入隔离静态字段并在 finally 恢复。未执行这些测试。

本轮未发现可确认的新编译错误，不需要额外源码修复。尚待主控 Unity 编译、EditMode/实际窗口验证确认 Unity 导入、重载和运行时行为；不应将静态结论标为编译通过。
