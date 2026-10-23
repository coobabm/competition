# 2026.10.20.1 打包与上传脚本复核

范围：协调目录 `release-tools`，不修改 Unity 源代码、不启动 Unity、不执行真实上传。初次只读审阅发现风险后，主控明确授权修改 `publish-release.ps1`；本次修改保留原有四个调用参数。

## 已修复的上传风险

1. **缺失分卷也可能公开发布。** 原脚本直接枚举通配文件，没有读取 package-result，也没有要求 `.001` 或检查 SHA256SUMS 的内容。现在先验证版本 `2026.10.20.1`、完整提交 SHA、package-result 的 sourceCommit/sourceDigest/tested、规范模型元数据、非零且连续的分卷与精确卷数、校验清单条目无缺失/重复/未知文件、本地全部卷 SHA-256，以及每个上传资产非空且小于 2 GiB。最后重新执行 `7z t`；这些检查发生在获取凭据和任何远端修改前。
2. **先公开、后发现远端多余资产。** 原脚本先 `draft=false`，再只检查资产数量。现在在草稿状态分页读取全部资产，要求名称、大小、上传状态及 SHA-256 精确匹配；缺少、重复、多余、未完成或冲突资产均阻止公开。发布后再次完整检查。
3. **提交与标签可能错位。** 现在核对远端分支 HEAD、PR head 和 `-Commit`；已有 tag 通过 Git ref 解析，支持轻量及 annotated tag，最终比较实际 commit SHA。不能用 release 的 `target_commitish` 替代已存在 tag 的真实目标。公开前再检查分支、标签及草稿状态。
4. **恢复上传和旧正文。** 已有完整匹配资产可复用；冲突资产不覆盖、不删除。已有 PR 更新正文。已公开且资产完全匹配的 release 只复核，不追加或替换资产。上传失败保留草稿用于诊断和重试。
5. **清理与记录。** HttpClient、上传响应/内容/文件流显式释放；凭据变量在外层 finally 清理。无凭据输出语句，不记录请求头或令牌。结果文件只保存公开 PR/Release URL、提交及资产元数据。

## 验证

- PowerShell Parser 静态解析 `publish-release.ps1`：通过。
- `release-tools/verify-publish-offline.ps1`：**12/12 通过**，从实际脚本 AST 提取资产核验和标签解析函数；覆盖完整集合、可恢复子集、缺卷、重复、多余、错误摘要、未上传完成、空集合、轻量标签、annotated 标签与不存在的标签。
- 离线检查使用本地内存对象与替代 API 函数，没有调用凭据助手、GitHub、真实上传、7-Zip 或 Unity。它不代替最终实际包上的发布前检查，也不证明 GitHub 在线 API 流程成功。

## 仍需发行负责人处理的交付边界

- **本地完整 ZIP 尚需独立产出。** `Package-VerifiedRelease.ps1` 当前只生成 `MyComputerAlive-Windows.7z.*`，每卷 1500 MiB；没有生成完整 ZIP。若本次交付要求完整本地 ZIP，应从同一已验证 payload 生成 ZIP64 兼容 ZIP，单独测试并记录大小/SHA-256。不要把旧 ZIP 当作本次 2026.10.20.1 产物。公开 GitHub 不上传超过单资产限制的完整 ZIP。
- **源码与产物一致性。** 新上传检查要求 `package-result.sourceCommit == -Commit`。若先用旧 HEAD 的未提交修改构建、再创建最终提交，不能只改参数假装其出处已一致，应由主控完成准确构建/溯源流程。打包脚本记录 sourceDirty，但并不把 dirty 构建证明为标签提交的干净构建；最终发布须明确实际来源。不得手改元数据绕过检查。
- **模型与包合法性检查保留。** 包装脚本已更新 Roboto 来源路径、移除失效 DreamOS/Reach/Lucide 路径和旧 Lucida 声明，保留 Open Sans/Roboto Apache 与现有 Oxygen、照片、字体、Blackjack、模型、native notices 检查。旧固定版本正文已改用 result.version。
- **外部许可证输入仍由调用者保证准确。** `NativeLicenseDirectory` 大多检查必需文件存在；除完整 Apache 文本外，LLMUnity 外部 notices、native notices 与旁置 Roboto notice 未全部纳入统一版本/哈希清单。应固定已审计目录并记录其哈希，不能替换为另一运行库版本的许可证。`Assets/LingGuangV05/ThirdParty` 仍整目录复制，建议最终盘点其源码、meta 和重复 notice，确认所需内容；本次未改该打包行为。
- **归档完整性与可玩性不同。** `7z t` 验证压缩数据，不等于已在新目录解压、启动、存取档、重启或完成模型加载/回答。最终 Windows Player 检查仍须单独记录。
- **发布正文仍是草稿。** 两份草稿已纳入 EditMode 103/103、FilesWindowUiChecks PASS 和逐消息标注 QA PASS，保留 Ctrl 键自动化及最终成品验收边界。发布前填入最终资产清单/准确大小/校验值/链接与实际验证结果；不要直接公开含“待填”内容的草稿。
- **创建 PR 后需附加到当前聊天。** 脚本输出 `PR_READY` 并写 github-source-result.json；主控收到成功 URL 后应调用应用 attach_artifact 工具。脚本本身不能完成应用附件操作。
- **并发修改窗口仍存在。** 已在公开前复查远端状态，但 GitHub 跨请求没有事务锁；正式发布期间应冻结本分支、标签和草稿资产，避免另一发布操作同时修改。此为实际工作流程要求，不宣称完全消除并发风险。

本次没有生成本次完整 ZIP、没有运行真实发布脚本、没有创建或更新远端 PR/Release，也没有上传任何资产。
