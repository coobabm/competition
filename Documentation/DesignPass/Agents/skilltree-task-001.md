# 科技词条任务 001：正式术语、通俗解释、可见效果

状态：已实施当前单项的科技页文案层；静态编译与独立内存检查通过；Unity 场景和截图待验收。不得据此勾选整条全局原则或整份策划完成。

日期：2026-10-13（Asia/Singapore）。

## 阅读与修改边界

- 策划文件 `Play/Main-NoModel-20261003/Design/技能树词条策划.md` 仅读取前 3 行，停在「词条名用正式的专业名词，解释让外行听得懂；效果写成玩家一眼看得出的变化」。未读取后续正文。
- 只修改 `Assets/LingGuangV05/XingGuang/Core/XgTechTree.cs`、`Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs`；新增同 Core 目录 `XgTechTreeCopy.cs(.meta)`、`Assets/LocalBuild/Editor/SkillTreeCopyChecks.cs(.meta)` 和本记录。
- XgCatalog、XgSim、XgGlossary、壁纸、全局进度文件均保持本聊天只读。没有更改玩法数值、购买路径、阶段条件和存档结构；没有启动 Unity/游戏、提交、推送、打包或读取真实玩家存档。
- 用户要求多 agent 后，使用一个只读文案核查 agent 和一个独立 Editor 检查 agent，二者写入范围不重叠。后续主聊天扩展 100 个独立聊天后，本聊天保留科技词条整合职责，未重复新增同范围任务。

## 实施内容

- `XgTechTreeCopy.Name/Note` 提供科技页独立中英文文案：专业名词、通俗解释、单独的「效果 / Effect」段落。当前非图鉴节点均有解释和效果；未知未来节点保留原文，不虚构效果。
- 节点标签、提示标题、前置节点名称、自动化等级列表使用统一名称；深度/宽度链称为「网络深度 / 网络宽度」，沿用下一等级所属训练线；满级展示最后一级。
- 说明使用下一购买节点；人工报酬按当前级别显示真实倍率变化，保留现有职称；满级不展示不存在的下一级。未知自动标注节点保留原神秘提示。图鉴属于发生记录，保留其原显示与说明，此次没有重写。
- 数值优先读取现有常量和目录：自动标注每级基础速率 `AutoRatePerLevel`（当前 0.15，而旧节点文案是 0.3）、自动训练间隔/半效/早停阈值、报酬倍率、下载时间、抽检概率、预训练规模倍数等。
- 明确上限不等于立即增大、购买结构不保证立即提分、实际每轮卡数受算力/取整/上下限影响，以及优化器仅使用最快已拥有项、不彼此相乘。
- 未重写 `Sim.Why` 的锁定原因或全局购买消息；这些跨页面共享接口仍可能显示旧名称，需由相应负责人另行统一，不能在本范围修改 XgSim。

## 关键效果证据与修正

| 条目 | 核对实现 | 本次表述 |
| --- | --- | --- |
| 自动标注 | XgCatalog.AutoRatePerLevel/AutoMinAccuracy；XgSim.Collaboration.EligibleDesk/CollaborationRate | 每个合格台的基础速率增量，60% 检查点，并注明算力/复核/暂停限制 |
| 自动训练 | XgSim.AutoInterval、Tick、EarlyStopEpochs | 自动尝试间隔、窗口/游戏条件、半效与早停；AutoML 合同和数据切换 |
| 学习率搜索/衰减 | XgSim.AutoModel.RateFinal、CeilingAcc；XgSim.SafeLr | 上限公式系数 0.9/0.85，替代而非叠加；不宣称实现了可见搜索或专属自动调参 |
| 数据包 | XgSim.Collection.StartDownload；XgSim.DataSources.PackSamples | 提供样本，须启用并完成下载，等价剧情数据不重复计数 |
| 散热器 | XgSim.Consumables.TrainWearFactor | 训练磨损 ×0.5，非直接保证烧卡概率减半 |
| 混合精度 | XgSim.Finale.ScaleItems；现有显存公式 | 预训练有效规模 ×2，不声称实际训练显存减半 |
| 支线研究 | XgSim.Collection.FeelSpeed；XgSim.Board.CardsPerEpoch | 当前是通用训练处理量倍数，非限定文字/翻译或直接增加预训练速度 |
| 难例奖励 | XgSim.Collaboration.Review：`Has("label.hard") && !card.recheck` | 新题人工复核答对 ×1.5 报酬、×2 样本；旧数据重审除外 |
| 迁移学习 | XgSim.AutoModel.ChangeArchitecture/Grow；XgSim.CarryConcepts | 换结构保留60%数值进度，同结构扩容70%→90%；概念迁移仍受结构约束 |
| Transformer附属技术 | XgSim.Progression.ApplyProgressionNode/ProgressionBlocker | 多头注意力、层归一化、残差为随附标记，不另收费或重复承诺独立加成 |

只读审查 agent 提出的学习率语义、难例排除旧题、数据包条件、迁移扩容收益四项均重新核对当前源码后采纳。没有收到需拒绝的补丁。

## 已执行验证（不是场景验收）

1. 使用本机 .NET Roslyn 和已有 Unity 引用程序集，独立编译全部 XingGuang Core 源文件：通过。出现既有 `XgSim.DayStats.cs:33` CS1718 警告（变量与自身比较）；属其他文件，未修改。
2. 独立编译修改后的 XgTechTreePage 与新增 Editor 检查入口：通过。依赖当前 Library 中既有程序集及 DOTween，仅验证编译和接口兼容性，不代表 Unity 完整重导入通过。
3. 在临时目录执行同一 `SkillTreeCopyChecks.Run()`，用轻量 Unity 菜单/日志替身和 System.Text.Json 字段序列化替代 JsonUtility；纯内存 XgSim，输出 `SKILL_TREE_COPY_CHECKS_PASS 1674`。包括中英文全部节点、atlas 保留、神秘提示、null 回退、加薪0/1/满级、真实自动速率和条件、大小数据包、所有链逐级 Next/满级、读取前后状态一致。
4. `git diff --check` 对本次两个已跟踪文件通过。新增源码已在上述编译中覆盖。

独立检查临时产物：`C:/Users/xomem/AppData/Local/Temp/skilltree-copy-834c2fd8b07e49d08b7e3fe6a62c8e9b/`，其中 `LocalVerification/DesignPass/SkillTreeCopy/checks.json` 是独立运行报告。该目录只存临时程序集、检查宿主与报告，不是游戏存档。

## 主聊天统一执行入口与待验收

入口：`SkillTreeCopyChecks.Run()`，或 Unity 菜单 `Tools/Design Pass/Skill Tree Copy Checks`。真实 Editor 运行后输出工程内 `LocalVerification/DesignPass/SkillTreeCopy/checks.json`；只构造内存状态，不载入场景或玩家存档。

- [ ] Unity 真实导入、编译和上述 Editor 检查（尚未启动）。
- [ ] 隔离测试场景中中英文科技页截图：长专业名、段落、前置条件、折扣和自动化等级同时显示时不裁切；窗口最小尺寸、平移、缩放均可读。
- [ ] 未发现自动标注仍不泄露名称/效果/价格；购买前后及满级描述和等级一致。
- [ ] 按现有玩法实际购买、训练和评估，验证玩家能从显示变化理解效果；不得把静态数值核对视作真实玩法验收。
- [ ] 当前全局文案原则的跨页面一致性（尤其 Sim.Why 和购买日志旧名称）由主聊天统筹；本任务不越权修改共享接口。

## Parallel100 交付整合

2026-10-13 更新：已完成本轮60份交付的整合核对，现行决定、源码身份和验收缺口以 [tech-integration.md](../Parallel100/tech-integration.md) 为准。下段为整合开始前的历史状态。本文件1674项通过仅对应早期快照，不能证明当前整合版本通过；当前版本等待主聊天统一编译与场景验收。散热器结论也已纠正：沿实际账单路径确认训练磨损系数参与烧卡随机判定，应以新记录及现行文案的适用条件为准。

本轮结束前复查时 `Documentation/DesignPass/Parallel100/manifest.json` 已到达，确认总计100个聊天及本聊天的tech整合负责人身份；尚未发现 tech 交付文件。新增独立聊天交付统一记为待到达、待核对、待整合，不无限轮询。后续每个交付应用前重新核对当时生产源码，在此记录采纳/拒绝及原因，不可直接覆盖本轮文案层。
