# 发行准备只读复核

2026-10-19；生产目录 `C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016`。已读 `Design/Claude协作交接_20261019_暂停后最新版.md`、`Design/协作当前状态_20261019.md` 的恢复说明及上次 `release-final-audit/AUDIT.md`。本报告完成后停止，遵守主控转达的最新暂停要求。

**结论：未找到本轮新增、已证实的构建配置、模型路径或素材许可缺陷；当前仍不能据此发布。** 当前未提交的产品改动尚无对应最终 Player；主要风险是源码/标签归属与最终成品证据没有闭合。以下不要求重复已经通过且未改变的检查，也不提出新补丁。

## 必须保留的发行门槛

### P1：不要把当前脏工作区 Player 归到尚未包含这些改动的发布提交

当前 `git status --short` 仍包含未提交的新闻、私人结局、桌游、群图、字体及未跟踪源文件。构建器 `Assets/LocalBuild/Editor/IntegratedReleaseBuild.cs:88` 的 `CaptureSource` 能记录这些文件，`:109`–`:110` 将当前 HEAD 和 dirty 标记写入来源；所以它不会漏掉新增 Assets/Design 文件，也不会谎称工作区干净。

但交接包的 `release-tools/Publish-VerifiedRelease.ps1:14` 仅要求包已测试且 `sourceCommit == -Commit`，不拒绝 `sourceDirty=true`；`:45` 用该 Commit 创建标签/草稿。若在当前脏工作区构建后，直接以旧 HEAD 发布，Player 会包含当前改动，公开标签源码却不包含它们。增强打包器接受 dirty 是已有明确设计，**这里报告的是当前执行风险，不把它重新判成打包器代码错误**。

最终应先审阅并提交要发行的改动，再以对应已推送提交构建；若选择保留 dirty 构建，则必须明确公开其来源差异，不能把旧标签称为完整对应源码。构建来源快照至 BuildPlayer 结束期间停止其他生产编辑；晚于构建的运行时内容修改需重建。当前未合入的图鉴/群预设等候选不能写入“此包已实现”的说明。

### P1：本轮最终二进制、普通 Player、实际音频与模型来源证据仍缺

本次只读确认 `LocalVerification/Release/latest-build.json` 仍不存在。交接也明确没有本轮新 Windows 包。收尾时主控反馈：TitleArchivesQa 30/30通过；桌游509成功、1文字布局失败，另有卡牌遮挡异常，运行错误计数0；Unity均已退出。此为主控提供的最新结果，本审查未另跑或重复核验，不把桌游整套记为通过。

剩余门槛沿用修订清单：完成当前已合功能所需验收后，以唯一最终构建在隔离存档目录运行普通 Player，核对真实保存/退出、PlayerPrefs 保护恢复、逐请求 native/native_filtered/fallback 来源、实际听取录音，以及包内模型/许可/分卷完整性。历史 Editor 夹具、核心测试或模型就绪文字不能替代这些证据。无需因本报告重跑未改且已经通过的新闻UI、教学、日常等套件。

## 已核实没有回退的发布配置

- 七个上次 `release-final-audit/source-evidence.json` 记录的小文件 hash **全部相同**：IntegratedReleaseBuild、VerificationSavePaths、YYChatHub、YYChatHub.EndingPrivate、TiebaHub、TiebaHub.LaoZhouNews、LaoZhouNewsComments。复用旧审计的存档路径保护、双状态保存、源文件捕获结论，未重跑其测试。
- `IntegratedReleaseBuild.cs:23` 仍为 **2026.10.19.1**，Win64 Mono / D3D11、两场景、唯一规范模型检查、保留公司/产品身份。ProjectSettings 的 **2026.10.12.2** 是被构建时临时版本覆盖并在 finally 恢复的项目设置，不是新发现的版本错误；身份仍为 `DefaultCompany / competition`。
- 最新交接包 `games/发布材料/Claude-Handoff-20261019-1412/release-tools/RELEASE-SMOKE-CHECKLIST.md:23` 已检查 **2026.10.19.1**，`:40` 已指向增强版打包器。上次审计的旧版本硬门禁/旧打包器路由问题，**在当前交接清单中已经修订，不重复列为未修复**。
- 交接包与协调目录增强打包器 SHA256 均为 `42769a446a1c9e6a8fd0732918637351bbcbf3b5a94a79c81d84774ff87aa440`。`RequireBuildSource`、十图 manifest、稳定绘图源码及三份 credits 校验存在；复用先前17小文件/原生许可交叉检查，不重复执行。
- 规范模型路径仍为 `Assets/StreamingAssets/LingGuang/Models/Qwen3-4B-Q4_K_M.gguf`，目录中一个 GGUF，文件长度 **2,497,280,256** 字节，LICENSE/MODEL_CARD/SHA256SUMS 均在旁。未重算2.5GB模型hash、未加载模型；真正打包时仍须校验已记录的 `7485fe6f11af29433bc51cab58009521f205840f5b4ae3a32fa7f92e8534fdf5`。没有新embedding文件或依赖引入的迹象。
- 最新新增 `XgSim.ReadNews.cs` 使用同一 XgState 的 `readNewsIds` 和已有新闻目录；`XgYyTalk` 仅新增私人AI上下文，不引入新外部内容下载、模型、原生库或独立存档路径。它位于已捕获的 Assets 根内。功能/隐私细节由本轮设定审查负责，本报告不重复其审查。
- 三张许可照片和十张原创插画的既有来源方案保持；未提交新增图片有同名 meta，credits 仍区分原创、照片及 CC BY-SA 单图适用范围。没有新的第三方素材类型需要另造许可方案。原始 Unity 输出仍需增强打包器附可读 notices，不能直接把裸 Build 目录当完整分发包。

## 交接执行注意，非新增产品阻塞

- `release-tools/PATHS.txt:1` 已明确清单含本机绝对路径、运行前映射到实际项目/工具目录；`:2` 明确用当前允许的窗口工具，不执行旧 PowerShell UI 注入。清单仍提到旧 Player-WindowQa 命令而附件未带该文件，是已明确替换的 UI 操作步骤，不要求补发或执行旧工具。
- `Publish-VerifiedRelease.ps1` 无 `-Publish` 也会创建草稿和上传；交接已明确说明。不得用它做试运行。当前没有执行它。
- 两篇 `Design/发布材料/*_待发布.md` 仍保留日期、版本、可玩链接占位，并明确尚未发布。发布时按实际验收补齐，不应把草稿链接或候选源码链接当可玩包链接。
- 动态字体/Aura换行/.vsconfig 仍在未提交状态，由主控按既有交接备份后甄别；本报告未把大量字体差异直接认作缺字、许可故障或批准丢弃它们。

## 本次操作范围

只读了源码、状态、少量哈希、文件名/长度、发布工具文本和交接；只新建本报告。没有编辑生产、图鉴或其它候选，没有运行/应用任何补丁，没有运行打包器、发布器、模型hash、Unity、Player、录音或压缩。所有最终验收与发布动作留给主控后续明确恢复时安排。
