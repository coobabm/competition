# tech-053：residual（残差连接）核对结果

日期：2026-10-13（Asia/Singapore）。结论：**现有 XgTechTreeCopy 的本节点文案正确，无需修改，不提交 changes.patch。**

仅审查 residual。本轮先读取已有任务记录 `Documentation/DesignPass/Agents/skilltree-task-001.md`，其中已明确 Transformer 附属技术仅为随附标记；本轮独立追踪实现，确认该结论。没有阅读任何新增策划正文、修改生产源码或其他目录、启动 Unity/游戏、访问玩家存档、创建子聊天或发送消息。

## 可直接采用的中英文文案

以下为当前生产文案，可原样保留，不需新补丁：

| 字段 | 中文 | English |
| --- | --- | --- |
| 正式名称 | 残差连接 | Residual connections |
| 通俗解释 | 给信息留一条绕过中间层的直路，减少层层传递时的损失。 | Give information a shortcut around intermediate layers to reduce transmission loss. |
| 真实效果 | 随 Transformer 一同获得的技术标记，不另收费，也不再叠加独立购买加成。 | A technology marker granted with Transformer, with no separate charge or additional purchase bonus. |

解释描述技术原理，效果段落明确这是游戏中的标记；没有声称单独点亮 residual 会提分、提速、提高深度上限或提供规模倍率。无需为增加字数而改写。

## 生效时点、单位与叠加方式

- **解锁/购买：** residual 是 `Label` 类型、`research` 树、名义阶段 6、前置 `transformer`、目录价格 0 的节点。它不是免费购买按钮；`ProgressionBlocker` 明确阻止单独购买。购买 Transformer 时 `ApplyProgressionNode` 同步授予该标记；另一条路径为阶段 5 升至 6 时 `CompleteTransformer` 免费授予 Transformer 及 residual。因此不可写成“必须花钱购买 Transformer 才获得”。
- **可见变化：** 授予后拥有状态立即改变，科技页下一次刷新显示“已拥有”。`NodeVisible` 优先检查拥有状态，因此提前购买 Transformer 时，即使阶段未到 6，已获得的 residual 也可以显示。仅到阶段 6 并不构成 residual 的独立购买资格。
- **装备：** 没有独立 residual 装备操作。真正的跨层直连资格由 `Has("resnet") || Has("transformer")` 决定；当前架构为 ResNet/Transformer 时训练参数默认带直连，其他架构还取决于 `run.skip`。这不是读取 residual 标记产生的效果。
- **训练：** 获得标记本身不需要训练即可记为拥有，也不立即增加准确率。底层直连属于模型配置；之后模型训练/评估的具体表现不能作为 residual 独立数值奖励承诺。
- **数值及单位：** 该标记额外收费 ¥0；没有独立的百分比、百分点、层数、样本/秒或预训练规模倍率。不要写成“残差连接 +2”或“准确率 ×2”。
- **叠加：** `Grant` 去重，反复授予不累积；residual 不在 `ScaleItems` 中。名为“残差 / residuals”的预训练有效规模 ×2 条目实际键为 `resnet`，与其他规模道具相乘，并非本标记的效果，也不会因本标记再乘一次。

英文实施说明（用于说明时点，非必需替换现有简短提示）：

> Granted automatically with Transformer, either when it is purchased or when stage 6 is reached. The marker becomes owned immediately; no separate purchase, equipment slot or training step is required to obtain it. It adds no independent numerical bonus and does not stack on repeated grants. Actual skip connections depend on the owned architecture and model configuration. The pre-training “residuals ×2” multiplier belongs to ResNet, not this marker.

## 原文件证据

路径均相对于工程根目录；SHA256 全值见同目录 `source-sha256.json`。

| 文件与行 | 本节点相关依据 |
| --- | --- |
| `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:442` | residual 的正式中英文名、Label 类型、Transformer 前置、0 价格、阶段 6/research。 |
| `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs:15,45,51,57–60,109,185` | 名称回退目录；神秘节点先回退原提示；通俗解释与共同效果文案；规模附加行只匹配当前节点 id。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs:106,140–162,206,587–596` | Grant 去重；拥有节点优先可见，否则按阶段显示；阻止单买；购买 Transformer 与完成 Transformer 均 Grant residual。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Stages.cs:60–79` | 阶段 5 升至 6 调用 CompleteTransformer(true)。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:195–205,252–277` | Status 先判断拥有/阻塞；只有 Buyable 才购买；购买后应用进度、自动配置并评估。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs:83,104` | SkipOwned 和 Knobs.skip 都不读取 residual。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.AutoModel.cs:203` | 自动技术配置设置 run.skip = SkipOwned。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Finale.cs:74–84,99–102` | 规模表为 resnet ×2，无 residual；倍率按拥有的各表项连乘。 |
| `Assets/LingGuangV05/XingGuang/Core/XgTechTree.cs:127–130` | 科技条目可见性服从 NodeVisible。 |
| `Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs:426–431,758–772` | 隐藏不可见条目；神秘提示提前返回；可见节点才使用统一文案并展示拥有状态。 |

## 未发现节点的神秘性

本次不改任何可见性/神秘性分支。residual 在未拥有且阶段小于 6 时由 NodeVisible 隐藏；提前随 Transformer 获得则按已发现处理。NodeMystery 当前只处理 label.auto，不应误称 residual 自身会显示“？？？”。现有 Name/Note 的神秘分支和科技页提前返回均保留；不在更早节点文案中新增 residual 剧透。

## 验证状态与待整合事项

- **已完成静态检查：** 定向检索 Core 对 residual/resnet 的引用并逐项追踪目录、授予、购买阻塞、训练参数、规模倍率和 UI 显示路径。没有把源码推导记为运行 PASS。
- **未执行：** C# 编译、独立运行测试、Unity 导入、实际购买/训练、场景截图和视觉验收。本任务没有生产修改，不重复已有整合记录的编译与检查；该记录中的通过结果不视为本轮运行结果。
- **待整合：** 接收本报告为 residual 的“已核对，无需文案改动”。不要将全局原则或整份策划标为完成。
- **主聊天待验证的真实边界：** ①阶段 5 未拥有 Transformer 时 residual 隐藏；②阶段 5 合法购买 Transformer 后标记成为拥有，重复调用购买不重复收费/加成；③不购买 Transformer 而进入阶段 6 仍免费获得标记；④隔离内存状态仅切换 residual 时，ScaleMultiplier 和 Knobs.skip 不变，拥有 resnet 才对应 ×2 规模项；⑤中英文提示可读、没有误附 ×2 行。④须固定其他解锁、阶段及模型配置，不把“同时拿到 Transformer”造成的变化误归因于 residual。
- **依赖结束前复核：** 见同目录 `final-recheck.json`，逐文件比较本轮快照 SHA256 与收尾源码。若后续整合修改了这些文件，请按当前源码再次核对，不直接沿用行号。
