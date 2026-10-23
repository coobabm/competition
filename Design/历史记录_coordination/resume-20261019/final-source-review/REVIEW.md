# 最终源码定点审查与英文赌场来源修复

状态：独立候选已冻结，生产未修改，等待主控整合。基线 HEAD `3533141a406940d571e064f3797328c7d214235a`（工作树已有其它交付改动）。本审查不读取真实存档，不启动 Unity/Player，不提交或打包。

## 唯一确认的问题：P2 英文终章来源说明显示新生成的中文赌场文本

位置：`Assets/LingGuangV05/XingGuang/Core/XgEndingNarrative.cs:65` 从 `record.question` 保存来源摘要；原 `XgEndingPresentation.Evidence`、`Desktop/XgEndingPreparationHost.cs:166` 与 `Desktop/XgEndingPreparationHost.Narrative.cs:185` 直接显示该摘要。赌场写入本身已有 `questionEn`。

`probe/baseline.log` 记录了原生产 DLL 上真实 paid/refused/ignored 结算与真实 DiscoverMemories 的复现，三种英文 Evidence 均包含中文。例：`Because on September 1 you answered: "888.vip「风控冻结」:盈利 ¥431 被冻结,要求先交保证金 ¥200 才能提现。"`。这属于系统编写的双语赌场事件，并非应保持原文的玩家输入。

修复仅在读取时选择语言：`SourceSummary` 按 `answer:<id>` 和日期找到精确记录，再用现有 `CasinoScamMemory` 验证赌场类型、bank、event 与来源合法性，选择记录自身已保存的 questionEn/question，并经过当前动态文本过滤器。三个来源展示入口统一调用它。完整来源详情页已有正确语言选择，无须改动。

不增加存档字段、不迁移或改写 canonical summary。无法解析、日期不符、非赌场、缺少英文或过滤拒绝时保留原存储摘要；不为旧档发明译文。忽略事件仍使用 record 语义，不误称玩家回答。当前过滤器接受 888.vip，未确认域名过滤冲突。

## 验证与边界

`verification/targeted-checks.log`：6/6 定向用例通过，直接执行相同 NUnit 测试方法，使用真实候选 Core。三项分别执行 paid/refused/ignored 的真实结算、发现记忆、英→中→英展示、全状态 JSON 不变检查及 JSON 往返恢复；其余三项覆盖非赌场/失配历史、旧记录缺少英文、拒绝保存译文的过滤回退。夹具明确指定冻结前提和单张 memory template，不伪造来源摘要。

独立编译通过（均 0 错误）：

| 程序集 | 警告数 | 日志 |
| --- | ---: | --- |
| LingGuangV05.Core | 1 | verification/LingGuangV05.Core-build.log |
| LingGuangV05.XingGuang.Core | 3 | verification/LingGuangV05.XingGuang.Core-build.log |
| Assembly-CSharp（包括两个实际桌面调用方） | 538 | verification/Assembly-CSharp-build.log |
| LingGuangV05.XingGuang.Tests | 3 | verification/LingGuangV05.XingGuang.Tests-build.log |

日志没有候选文件的编译警告；其余警告来自引用的生产源文件，未为本修复清理。测试程序集在 Unity define 下编译，定向执行则使用独立 .NET 与 System.Text.Json 分支。未执行 Unity JsonUtility、Unity Test Runner、真实 UI、磁盘存档或 Player，因此不能把本结果表述为这些验证通过。

主控整合后可使用 Unity 测试过滤器 `LingGuangV05.XingGuang.Tests.XgCasinoSourceLocalizationTests`（6 个 case）；UI 复核关注英文终章记忆来源及来源选择列表，切换语言后重开面板。

## 保存入口、源码与授权资产范围

本次 888/ending-A 增量审查没有另外确认正常存档根目录绕过或新增第三方资产许可证缺口。赌场仍经 controller/runtime 正常 SaveNow；A 分支沿用快照恢复与现有保存失败处理；本候选只新增来源展示逻辑和测试，不引入网络资源、包或第三方资产。实际磁盘保存失败、最终 Player、音频、模型来源及打包授权验证仍由主控的交付检查承担；此处不声称完成发行合规认证，也不重复既有发布审计中的版本清单问题。

## 冻结交付

- 补丁：`casino-source-localization.patch`（3 个修改源文件 + 1 个测试文件及 meta）。
- SHA-256：`74f4217972978dc205a61ebdf184127d8fc34ed74950c4105cf4ddcbb54c495a`。
- `manifest.json` 保存每个 baseline/candidate 文件的 SHA-256；冻结时三份生产文件与原始快照逐字节相同，两个新增路径无冲突。
- `verification/apply-check.log`：对当前生产执行 `git apply --check`，exit 0，未实际应用。
- `prepare_candidate.py`、`prepare_verification.py`、`verification/prepare_compile.py`、`freeze_patch.py` 保留复现入口。候选与 baseline 位于各自目录，原始复现 probe 未覆盖。

依赖当前已经整合的赌场记录 API、文本过滤与终章来源语义修复。主控若继续改动这三个目标文件，需重新 apply-check；不应整文件覆盖生产。
