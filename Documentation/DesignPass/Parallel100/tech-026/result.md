# tech-026 · label.trapmemory（金标准题匹配）

日期：2026-10-13（Asia/Singapore）。范围仅为此节点，生产源码只读。

## 结论

当前 `XgTechTreeCopy` 的正式名称、通俗解释和真实效果准确，无需修改，不提交 `changes.patch`。已对现有进度、队列和 Parallel100 交付记录检索本节点，未发现重复交付；未读取新的策划正文。以下详版是可选展示文案，现有简版不存在必须修复的问题。

## 可直接采用的中英文文案

| 行 | 中文（当前简版，建议保留） | English (current copy, retain) |
| --- | --- | --- |
| 正式名称 | 金标准题匹配 | Gold-standard item matching |
| 通俗解释 | 记住平台用来检查质量的标准答案题。 | Remember known-answer items the platform uses to check quality. |
| 真实效果 | 再遇到已记住的金标准题时，转入人工复核，不自动提交。 | Send remembered gold-standard items to human review instead of submitting automatically. |

如版面需要独立说明购买时机，可采用下列详版效果（不要求替换当前简版）：

中文：购买后自动生效，无需装备或训练。自动流程再次遇到已被平台检查并记住的金标准题时，无论把握多高，都转入人工复核，并标记“眼熟”。只需购买一次，不按等级叠加；首次遇到且尚未记住的题仍按原有规则处理。

English: Active automatically after purchase; no equipping or training is required. When the automatic flow encounters a gold-standard item already checked and remembered by the platform, it goes to human review regardless of confidence and is marked “Familiar”. This is a one-time purchase with no stacking levels. Items not yet remembered still follow the existing routing rules.

## 生效、单位和叠加合同

- 这是标注流程节点，不是可装备的模型架构。前置 `label.coop` 已拥有后，花费 **¥800 游戏经费**购买，最高 **1 级**。只是满足前置或看到节点不会生效；购买成功将 ID 写入 `S.unlocked`，下一次符合条件的分流立即读取，无需重新训练、评估或部署。
- 已记住记录在购买前也会积累。平台质检启用、金标准题实际进入自动提交结算且被检查时，才调用 `RememberTrap`；答对、答错都会记住。仅看过题目、推理过题目、被低置信度分流或被内部抽检拦下，均不等于平台已检查。购买后立即可使用先前留下的记录。
- 匹配依据为 `trapId = desk + "#" + index`，是同一标注台同一题号的精确身份匹配，不是语义相似题识别，不会告诉玩家正确答案。首次未记住的题不能靠本节点识别，仍可能由既有规则送回复核。
- 单位是 **题/条不同题目记录**：每桌固定池 **12 题**，记录列表上限 **400 条**，跨桌共享；重复记忆不新增、不刷新顺序，超过上限从最早加入的记录删除。这些是系统现有常量，不是购买额外赠送 400 题，也不是模型记忆长度或准确率加成。
- 与置信度阈值按逻辑“或”共同分流，已识别题即使等于或高于阈值仍进人工复核，不需要再掷一次抽检概率。自身无百分比、加法或乘法增益，不能重复购买叠加。
- 复核队列共用 **20 题**容量。已满、自动流程暂停、无算力、项目占用或其他分流前置不满足时，不会强行入队；恢复条件后才继续分流。节点不是绕过自动暂停的特权。
- 此节点不额外增加报酬或样本。转入队列后仍由玩家作答；若玩家另已拥有 `label.hard`，非旧数据复核题答对时可按既有规则获得人工报酬 ×1.5、样本 2 份。这是其他已拥有节点的结算效果，不应写成本节点奖励，也不在简版中揭示其他节点。
- 默认金标准题混入概率为 **2%/次预取抽样**（`.02`），不是本节点加成。文件内有旧的 5%／“one in twenty”注释，判断以执行常量为准；本任务不改注释、不改数值，玩家文案不需要暴露这一概率。

## 原文件证据

下列路径均相对于 `Assets/LingGuangV05/XingGuang/`，行号是本次静态核对时的位置。

| 文件与行 | 依据 |
| --- | --- |
| `Core/XgCatalog.cs:462` | 实际目录由 `XgSim.LabelNodes` 注册，不存在另一个同 ID 的架构实现。 |
| `Core/XgSim.Collaboration.cs:37, 41–42` | `LabelNode(TrapMemoryNode, "label.coop", …, 800, 1, 910)`；类型 Label，树 label。 |
| `Core/XgSim.Collaboration.cs:146–164, 180–191` | 等级按拥有与否为 0/1；检查前置、经费、上限；成功购买写入 unlocked。 |
| `Core/XgSim.cs:188, 252–256` | Has 查 unlocked；BuyNode 将 label 树交给 BuyLabelNode。 |
| `Core/XgSim.QualityTraps.cs:30–43, 73–83, 96–100` | 12 题池、400 条上限；匹配题号；拥有节点且题号已记住才识别；记忆去重。 |
| `Core/XgSim.Quality.cs:79, 159–174` | QualityActive 条件；实际送达平台后金标准题必检查；在答对/答错分支之前记忆。 |
| `Core/XgSim.Collaboration.cs:103, 368–386` | 超限删除最早记录；Route 先检查暂停/容量；识别分支在抽检与自动提交之前入队并返回。 |
| `Core/XgSim.Collaboration.cs:419–443` | 玩家复核按实际作答结算；仅其他已拥有节点提供额外报酬/样本。 |
| `Desktop/XgLabelPage.cs:559, 1018–1024` | 复核卡调用 QcTags，识别题显示“眼熟”。 |
| `Assets/LingGuangV05/Core/Lang.En.cs:1199`（工程根相对） | “眼熟”译文为 Familiar。 |
| `Core/XgTechTreeCopy.cs:43, 119, 195` | 当前三行文案与以上实现一致。 |
| `Core/XgTechTree.cs:144–146`; `Desktop/XgTechTreePage.cs:766–772, 785, 799` | 节点名与说明实际调用文案层；页面显示游戏经费符号 ¥。 |

## 未发现内容的保护

`XgTechTreeCopy.Name/Note` 的 `NodeMystery` / `IsAtlas` 提前回退（15、51 行），以及科技页 748–764 行未知图鉴/神秘节点提前返回均保持不动。`XgTechTree.Visible`（127–130 行）委托 `NodeVisible`（`XgSim.Progression.cs:140–148`）。本节点当前是可显示的 label 节点，不具有独立神秘状态；不为它捏造新的解锁阶段，不公开未来节点奖励，也不更改任何可见性入口。

## 验证状态与待验证边界

**已完成：静态代码核对。未执行：C# 编译、模拟运行、Unity/游戏运行及视觉验收。下表是交给整合负责人的运行验证期望，不是已运行结果。**

| 边界 | 应观察到的行为 |
| --- | --- |
| 已记住但尚未购买，置信度高于阈值 | 本节点不拦截，仍走原有路径。 |
| 购买前记住题目，再购买，置信度等于阈值或高于阈值 | 下一次分流转人工复核；不增加自动提交次数，不在此时支付自动奖励。 |
| 已购买、题号未记住，置信度低于阈值 | 可由普通低置信度规则送回复核，但不因本节点标为“眼熟”；仅进入队列不会新增平台已检查记录。 |
| 相同 index、不同 desk | 一个桌的记忆不能误识别另一桌的题。 |
| 队列 19 题 / 20 题 | 19 时能再接收一题；20 时 Route 返回且预取题保留，不能越过容量或偷偷自动提交。 |
| 平台第一次检查金标准题，分别答对/答错 | 两者均记录题号；已有记录再检查不重复计数。 |
| 再次购买 | Owned / 满级，不能再次扣费或叠加效果。 |
| 中英文科技页及复核卡 | 三行含义一致，“眼熟 / Familiar”可见且不挤压版面；此项必须实际截图验收。 |

待整合：无需合并源码补丁；整合负责人可直接保留现有三行，或按版面采用上面的详版效果。统一编译、真实运行与截图验收交由主聊天执行。不将该节点静态核对扩大为整份策划或全局原则完成。

## 源码指纹

SHA256，供结束复核和整合时识别并发变化；无需应用补丁。

| 文件（同上相对目录） | SHA256 |
| --- | --- |
| `Core/XgCatalog.cs` | `1D051CDC9A59AB3C1F1825A0445ABD486CC6638273320306FB427556F17685B7` |
| `Core/XgSim.cs` | `C4F1998E95B717C93AD4195D5C9FBB43616614D5C4A88837CB3EBCF44DE9966B` |
| `Core/XgSim.QualityTraps.cs` | `F205EC2B7906ED7412E2C0FBDDC905071825B34BBDB0F37A990650E34D537559` |
| `Core/XgSim.Quality.cs` | `14B94D8A8B5937E71C15C7D3C84D1A74021749290A88AAF64A3429ECBD6C4283` |
| `Core/XgSim.Collaboration.cs` | `31F6D4234DC64046ED6961F3DE7B7D28EE8A677C2ED1EA50226DCE421B7ACDB8` |
| `Core/XgTechTreeCopy.cs` | `308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8` |
| `Core/XgTechTree.cs` | `C2795A20598FA9B22F91A4678AD7946F97AC0D03E518A62A3108A80350D0414D` |
| `Core/XgSim.Progression.cs` | `D997BB22368619936B2CC7D3144EFE82519D0114A792B945BA7A9ADE40ABB838` |
| `Desktop/XgTechTreePage.cs` | `F27CC730C4871776FC3E29FB95D765C099C02007889648334C42E3491CA8A523` |
| `Desktop/XgLabelPage.cs` | `A9A4034835455A081FACB33388AEB1155814AB835146694381DFE7AB571ABA89` |

结束复核（2026-10-13）：

- 未变化：Core/XgCatalog.cs
- 未变化：Core/XgSim.cs
- 未变化：Core/XgSim.QualityTraps.cs
- 未变化：Core/XgSim.Quality.cs
- 未变化：Core/XgSim.Collaboration.cs
- 未变化：Core/XgTechTreeCopy.cs
- 未变化：Core/XgTechTree.cs
- 未变化：Core/XgSim.Progression.cs
- 未变化：Desktop/XgTechTreePage.cs
- 未变化：Desktop/XgLabelPage.cs

