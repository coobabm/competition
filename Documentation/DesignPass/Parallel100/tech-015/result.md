# tech-015 · layernorm（层归一化）核对

日期：2026-10-13（Asia/Singapore）。结论：现有 `XgTechTreeCopy` 对本节点的名称、通俗解释和效果准确，无需修改生产源码，不提交 changes.patch。

## 范围与已有记录

仅检查 layernorm 及其授予、购买、显示依赖。已核对 `Documentation/DesignPass/Agents/skilltree-task-001.md`：整合负责人已将本节点处理为 Transformer 随附技术标记，本次复核确认该结论，无需重复实现。没有读取策划原文件；初次在 DesignPass 内检索任务记录时，检索结果意外带出逐条清单中一条 layernorm 摘录，未继续打开该清单或后续正文，也未将其用于玩法结论。

生产文件只读；未启动 Unity/游戏、未读写真实玩家存档、未提交或打包。只在本专属目录交付。

## 可直接采用的三行文案

保留现有文案，无需替换：

| 字段 | 中文 | English |
| --- | --- | --- |
| 正式名称 | 层归一化 | Layer normalization |
| 通俗解释 | 把一个字内部的各项信号调到相近范围。 | Bring the signals within one word representation into comparable ranges. |
| 真实效果 | 随 Transformer 一同获得的技术标记，不另收费，也不再叠加独立购买加成。 | A technology marker granted with Transformer, with no separate charge or additional purchase bonus. |

解释描述术语含义，不等同于游戏逐特征执行归一化运算。当前节点效果只承诺已实现的随附标记；不应追加“训练更稳”“速度提高”“精度提高”或任何独立倍率。

## 生效、单位和叠加合同

| 情形 | 当前真实行为与可采用说明 |
| --- | --- |
| 解锁 / 获得 | 购买 Transformer 的同一次调用中授予本标记；阶段 5→6 的 `CompleteTransformer` 也授予。立即进入已拥有状态，无需等下一轮训练。EN: Granted immediately with Transformer; no further training is required to own this marker. |
| 单独购买 | 即使目录价格为 0，也不可点击单独购买；未拥有时被 `ProgressionBlocker` 阻止。EN: Granted with Transformer; not available as a separate purchase. |
| 装备 | 本节点不提供独立装备步骤或开关；不能将授予标记描述成选择或装备新架构。EN: No separate equip step. |
| 训练后 | 没有读取 `Has("layernorm")` 的独立训练/评估增益分支；Transformer 自身的重配置与评估不能归因成本节点的独立收益。EN: This marker adds no separate training or evaluation bonus. |
| 数值与单位 | 目录成本为 ¥0，且不单独收费；状态为未拥有/已拥有（科技页 0/1）。无本节点专属的题/秒、百分点、层数或规模倍数。EN: No separate charge and no independent numerical bonus. |
| 叠加 | `Grant` 去重；重复授予不增加等级。不在 `ScaleItems` 中，不附加预训练规模倍率，也无与其他技术相加/相乘的独立项。EN: Repeated grants do not stack. |

## 原文件证据（工程相对路径与核对时行号）

- `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:441`：ID `layernorm`，kind `Label`，parent `transformer`，cost 0，stage 6，lane `research`。`:359–360` 的 `Simple` 把 tree 设为 `trunk`，因此不是 `tree == "label"` 的可重复标注升级。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs:154–163`：未拥有时 layernorm 固定返回“随 Transformer 一起来”；`:199–206`：购买 Transformer 时调用 `Grant("layernorm")`；`:106`：Grant 使用 Contains 去重。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs:587–596`：`CompleteTransformer` 授予本标记；`:90–100`：已完成旧项目的兼容修复同样可进入此路径；`:583`：旧项目完成路径仍存在。只静态检查，未读取存档。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Stages.cs:72–79`：由阶段 5 推进时调用 `CompleteTransformer(true)`。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:188`、`:194–205`、`:252–279`：Has 读取已解锁集合；已拥有优先返回 Owned；未拥有需通过 blocker 才能购买；Transformer 购买先授予随附标记，再重配置并评估。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Finale.cs:75–84`、`:96–98`：预训练规模倍率列表无 layernorm，列表内项目才相乘。
- `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs:32`、`:108`、`:185`：三行文案与上表一致；`:15`、`:51`：神秘节点提前回退原提示；`:57–59` 只为 ScaleItems 内节点附加倍率。
- `Assets/LingGuangV05/XingGuang/Core/XgTechTree.cs:93–109`、`:126–130`、`:144–146`：本节点按单节点 0/1 所有权显示，沿用模拟层可见性及统一名称。
- `Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs:426–430`：未可见节点隐藏；`:758–764`：神秘提示提前返回；`:766–772`：标题、拥有状态、说明接入统一文案。

神秘性边界：`XgSim.Progression.cs:140–148` 先允许已拥有节点显示，否则本节点到 stage 6 才显示；所以 stage 5 提前买到 Transformer 后显示随附标记是当前已有规则，不应误改为强制隐藏到 stage 6。`XgSim.cs:250` 的问号型神秘提示当前仅针对 label.auto；本节点通过可见性隐藏，无需新增问号模式。

## 验证状态与待验证边界

- 静态检查已执行：在 `Assets/**/*.cs` 检索 layernorm / Layer normalization / 层归一化，引用仅落在目录、文案、购买阻止和授予路径；结合 ScaleItems 核实无本节点独立数值收益。
- 本聊天没有执行 C# 编译、运行模拟、Unity 场景或视觉验收，不标记运行 PASS。既有任务记录的验证不冒充本聊天执行。
- 无补丁待整合；负责人可保留原文案并采纳本核对结论。不得据此将全局原则或整份策划标为完成。
- 主聊天运行验收建议使用隔离内存状态：①阶段 5 未拥有 Transformer 时本节点隐藏；②阶段 5 买到 Transformer 后立即拥有本节点，单独购买被拒；③阶段 6 可见但人为缺失标记时仍不可单买；④重复授予不增加等级；⑤中英文名称/解释/效果无裁切。第二项中的钱款只应计 Transformer 自身价格，不能将本标记价格 0 解释为 Transformer 免费。

## 依赖 SHA256

下表为核对快照；整合前若有变化，应重读对应证据，不覆盖整合窗口的新改动。

| 文件（省略共同前缀 Assets/LingGuangV05/XingGuang/） | SHA256 |
| --- | --- |
| Core/XgCatalog.cs | 1D051CDC9A59AB3C1F1825A0445ABD486CC6638273320306FB427556F17685B7 |
| Core/XgSim.cs | C4F1998E95B717C93AD4195D5C9FBB43616614D5C4A88837CB3EBCF44DE9966B |
| Core/XgSim.Progression.cs | D997BB22368619936B2CC7D3144EFE82519D0114A792B945BA7A9ADE40ABB838 |
| Core/XgSim.Finale.cs | E565169876C92B4E434BDE1421B995F785DB8E160747268F32F30B95193CC11B |
| Core/XgSim.Stages.cs | CE64E64CCFB1CC7C24ADAD73C771650F367DAD6E4F55E30F762AB8BDC34BEE98 |
| Core/XgTechTreeCopy.cs | 308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8 |
| Core/XgTechTree.cs | C2795A20598FA9B22F91A4678AD7946F97AC0D03E518A62A3108A80350D0414D |
| Desktop/XgTechTreePage.cs | F27CC730C4871776FC3E29FB95D765C099C02007889648334C42E3491CA8A523 |

结束前依赖复核：已重新计算上述 8 个生产文件的 SHA256，全部与表中快照一致；核对期间未发现依赖变化。
