# 2026-10-20 包体发行准备核对（只读审计）

状态：当前是 2026.10.20.3 候选构建，不是最终交付。本审计未启动 Unity/游戏，未打包、未上传、未接触真实存档；只写本报告。通宵赶工加速规则、蜘蛛入口/购买门槛仍待用户确认，不能把候选包直接当最终版发行。

## 已核实

- 唯一目标工作树：C:/Users/xomem/Documents/ChatGPT/games/competition-claude-integration-20261020；分支 integration/claude-streak-20261020。正式发行源码必须在该树定稿并提交，不使用其他脏工作区。
- IntegratedReleaseBuild.cs 版本为 2026.10.20.3；候选 publish-release.ps1 的 version/tag/branch 同步为 2026.10.20.3 / v2026.10.20.3 / integration/claude-streak-20261020。
- 实际源模型 Qwen3-1.7B-Q4_K_M.gguf 长度 1,107,409,472 字节，现场 SHA256 为 b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897，与 MODEL_SOURCE.json、打包脚本、发布脚本一致。
- LLMUnity 包缓存存在：Library/PackageCache/ai.undream.llm@9931a9ea4690；manifest.json 固定同一完整提交 9931a9ea46906b1f43adaf9498c05ccf227b5d0b。
- Package-VerifiedRelease.ps1 对模型哈希/大小、唯一 GGUF、source-provenance 与构建结果、许可证、原画来源/哈希等做前置检查；1500 MiB 分卷、每卷小于 2 GiB、7z test 与 SHA256 清单均已实现。
- 当前源 THIRD_PARTY.txt 已明确 1.7B；末尾 4B 仅属注明的历史记录，不应把历史记录误认成本轮模型。
- 原生许可目录存在：C:/Users/xomem/Documents/ChatGPT/games/发布材料/Claude第二轮协作_20261019_183735/evidence/native-licenses。六个必需文件齐全：LlamaLib-LICENSE、llama.cpp-LICENSE、FeatureDetector-LICENSE、jsonhpp-LICENSE、cpp-httplib-LICENSE、README-SOURCES.md。
- 原生来源记录：LlamaLib v2.0.5 / 21d3b9dcd1c05ad13f94f198e59462960019a057；llama.cpp / 872646b30c342c5fa98efec002f94fdb4f1f1ea5；FeatureDetector / 077393260d5e0e19938582a33b9dd0e74baa8ff2。文档明确未重新下载 1.8GB 原生归档、未逐字节对比 DLL，因此不能声称本审计完成该比较。
- 候选发布脚本 PowerShell 语法解析无错误。本审计没有执行发布脚本、没有读取凭据。
- 核对时 C 盘可用约 31.6GB，最终构建/复制/ZIP/7z 前应再次检查，不删除既有用户包体。

## 发布前必须处理

1. `.coordination/claude-integration-20261020/publish-release.ps1:109` 新建 PR 标题仍写「交付 2026.10.20.1：上游桌面整合、训练入口与人物回复标注」；117 行新建 Release 名仍写「我的电脑活了 2026.10.20.1 · Windows 试玩版」。应改为 .3 及本轮具体范围。已有 PR 更新仅改 body，若曾创建过错标题也需同步 title。
2. 上述 native-licenses/README-SOURCES.md 两处把“本版”描述为 Qwen3-4B，且仍称当前 THIRD_PARTY.txt 是退役服务端记录。这是旧叙述，不能原样给 1.7B 当前包。建议复制原许可目录到本轮独立准备目录，仅更新该说明索引，保留原许可证正文与证据不动；若 SHA256SUMS 包含索引，随之重算对应条目。不要改另一会话源交付。
3. 两脚本记录 sourceDirty，却没有拒绝 true。当前树实际含未提交代码及 Unity 生成更改，候选可以如实保留该状态；最终包须定稿、提交、在构建时确认源码与 provenance 一致。发布脚本建议额外拒绝 sourceDirty=true，避免误把验证候选公开。
4. 现有 Package-VerifiedRelease.ps1 只生成 7z 分卷，publish-release.ps1 只上传 7z 分卷、SHA256SUMS.txt 和 READ-ME.txt。没有完整 ZIP。用户要求的可直接分享/上传平台包体，需从同一已许可补齐的 payload 额外生成完整 ZIP 并 test/哈希；不得直接压原始 Build 忽略补入的 ThirdPartyNotices。
5. 如把完整 ZIP 也作为 GitHub 资产上传，发布脚本资产白名单、SHA256 文件解析、最终远程资产验证需一起扩展。当前 SHA256 解析只允许 .7z.NNN；单纯追加 ZIP 哈希会触发失败。完整 ZIP 如超过 GitHub 单资产限制，提供其他已授权托管或保留本地可用链接，并把 GitHub 分卷说明说清楚，不能冒称已有直链。
6. 下载说明应区分完整 ZIP（正常解压整个文件夹）与 7z 分卷（所有卷同目录从 .001 提取一次）。包是 Windows 便携目录，不是安装器；不能只发 EXE。TapTap 上传是否接收该格式以实际平台校验为准，不声称已通过平台审核。
7. 当前源码的音画/科技断言结果与编辑器索引报错需如实写发行说明；候选构建成功不等于真实 1.7B 回复、完整通关或最终用户界面已验收。

## 建议执行顺序

先取得待确认规则并完成修复 → 源码复核/提交 → 最终构建与相关游戏验收 → 独立 native-license 索引修正 → 打包并验证同一 payload 的 7z 和 ZIP → 确认 .3 未被他人占用/远端分支对应最终提交 → 上传并核对远程资产大小与 digest → 返回实际可玩入口/包体地址。若新建 PR，调用 attach_artifact 附到当前会话。

## 后续进展（同轮）

- 根代理已报告修复候选 publish-release.ps1 的 .1 标题/名称硬编码，并增加 sourceDirty!=false 时拒绝发布。本代理未改该文件。
- 新增本轮 `Package-FullZip.ps1`：只接受干净、已校验的 .3 package-result；核对 payload 内构建/源码记录及唯一 1.7B 模型现场哈希；拒绝路径及祖先/子文件符号链接，拒绝覆盖；从相同已补许可 payload 生成完整 ZIP，7z test 后独立输出 ZIP_SHA256SUMS.txt / zip-result.json。
- PowerShell 语法解析通过。按分工未执行压缩，也未用候选包冒充最终包；完整 ZIP 尚未生成。发布脚本是否上传 ZIP 仍需根代理决定/补接，不能仅把 ZIP 哈希混入原分卷清单。

## 发布配套已补齐（后续准备，不代表已发布）

- publish-release.ps1 已要求完整 ZIP 必须存在，核对 zip-result.json 的版本/提交/源码摘要/模型哈希/测试结果/大小，并读取独立 ZIP_SHA256SUMS.txt，与真实 ZIP 哈希交叉验证；在任何网络操作前再执行 ZIP 完整性检查。ZIP 和 ZIP_SHA256SUMS.txt 纳入同一远端上传、完整资产清单与 digest 校验。
- 原有 7z 连续分卷/哈希/测试以及 sourceDirty=false 门禁保留。ZIP 最大 2 GiB，旧分卷仍保持严格小于 2 GiB。缺 ZIP、错版本、错提交或错哈希均拒绝最终发布。
- 独立原生许可目录已准备：C:/Users/xomem/Documents/ChatGPT/games/.coordination/claude-integration-20261020/native-licenses。只修正文档索引中的本轮模型为 Qwen3-1.7B，补原始来源路径，并重算该目录 SHA256SUMS；其余 12 份许可证/来源证据逐一哈希确认与原交付完全一致。未改外部共享交付。
- 发布脚本修改后 PowerShell 解析通过。未运行压缩、未访问凭据、未运行网络修改，最终 ZIP/发布仍待主代理完成最终源码和构建。

## 用户新限制：完整压缩包严格小于 2GB（十进制）

- 本轮三份脚本统一使用 `2000000000` 字节排他上限：完整 ZIP 达到/超过即失败，不写成功记录；发布脚本再次核对，所有上传资产也受相同上限。不是 PowerShell 的 `2GB`（2 GiB）。
- 新增本轮独立 `Package-VerifiedRelease.ps1`（复制原已验证打包逻辑，只修正分卷上限为严格小于 2,000,000,000；Roboto Mono 原许可说明仍只读引用原 release-tools 文件）。最终打包请使用本轮副本；未修改共享旧脚本。1500 MiB 分卷本身低于新上限。
- `Package-FullZip.ps1` 的 zip-result 同时记录 `bytes`（压缩后的完整 ZIP）与 `uncompressedPayloadBytes`（解压后文件总大小），不可混称包体下载大小。
- 三份脚本 PowerShell 语法解析通过；未压缩未定候选，未上传。

### 实际候选容量预算

只读统计候选构建 `Build/MyComputerAlive-2026.10.20.3-20261020-120222`，排除 Unity 标注不分发的 *_DoNotShip 目录：273 个文件，共 **1,779,436,999 字节**（十进制约 1.779GB，约 1.657GiB），这是未压缩游戏文件总量，不是 ZIP 大小，尚未添加包装步骤补入的可读许可证和来源文件。

其中唯一 Qwen3-1.7B GGUF 为 **1,107,409,472 字节**，其它文件约 **672,027,527 字节**。对 2,000,000,000 字节阈值的未压缩余量约 **220,563,001 字节**（约 220.6MB）；许可来源和 ZIP 目录结构也占空间。现有单模型候选预计有容量空间，但最终是否满足只由实际 ZIP 字节数决定，不能用前版 7z 的约 1.35GB 代替 ZIP 实测。

用户新自研模型尚未定位，未替换模型，也不能假设双模型能装下。若后续加入新模型，需先核实准确格式/体积/许可/用途，再重新预算和打包，不删已有模型或压缩模型内容绕过验证。

### 整个 7z 包的总量限制补充

本轮 Package-VerifiedRelease.ps1 与 publish-release.ps1 均已新增所有 7z 分卷 Length 合计的门禁：总和必须严格小于 2,000,000,000 字节。不能通过多卷绕过完整包上限；打包结果额外记录 archiveTotalBytes 和 archiveLimitBytesExclusive。ZIP 保持相同整文件上限。三个脚本再次语法解析通过，本次未压缩或上传。
