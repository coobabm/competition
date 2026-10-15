# tech-042 — shared.lr 学习率搜索

日期：2026-10-13（Asia/Singapore）。范围仅为当前授权的词条名称、通俗解释和实际效果；未读取任何新增策划正文。生产文件全部只读，仅写本目录。

## 结论

正式名称「学习率搜索 / Learning-rate search」准确，继续沿用 XgCatalog，无需添加名称覆盖。现有 XgTechTreeCopy 的 0.9 系数和同项不叠加判断正确，但“让上限更高；需训练后评估”未区分购买即重算的上限与训练后可能提高的成绩，并且忽略硬上限可能使上限不变。交付 changes.patch，仅修改该节点的两行中英文文案，不改任何模拟数值。

已先查阅 Documentation/DesignPass/Agents/skilltree-task-001.md：此前已修正“实际搜索/自动调参”的错误承诺，本次不重复该实现。搜索现有 Parallel100/result.md 未发现 shared.lr 的重复交付。该任务目录此前不存在。

## 可直接采用的文案

中文名称：学习率搜索

中文解释：学习率是每次调整的步幅；搜索就是比较哪种步幅更合适。

中文效果：购买后自动作用于视觉和序列两条训练线，无需装备：上限计算中的可缩减误差项 ×0.9，有机会提高成绩上限。更强的同项加成优先，不相乘。购买即重算上限；继续训练后评估成绩，受其他上限限制，不保证每轮涨分。

English name: Learning-rate search

English explanation: The learning rate is the size of each adjustment; a search compares which step size works better.

English effect: Applies automatically to both vision and sequence training after purchase; no equip step. The reducible error term in the ceiling calculation is multiplied by 0.9, which can raise the score ceiling. A stronger bonus to the same term takes priority; they do not multiply together. The ceiling is recalculated on purchase; train further and evaluate your score. Other caps still apply, and a gain every round is not guaranteed.

补丁中的 0.9 继续从 N(XgCatalog.LrFinal[3]) 读取，不写死数值。解释描述术语含义，效果描述游戏实际收益，不宣称购买会开启候选学习率试训或搜索动画。

## 原文件证据与接口合同

以下路径均相对工程根目录 C:/Users/xomem/Documents/ChatGPT/games/competition-20261011。

| 证据 | 本节点的真实行为 |
| --- | --- |
| Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:384 | shared.lr 属于 LrKnob；第 1 阶段、trunk、前置 perceptron、目录基础价 60；中英文名称可直接保留。 |
| Core/XgSim.Progression.cs:131–148、154–164；Core/XgSim.cs:194–205 | 阶段可见及前置满足仅代表可以买，尚未获得加成；实际价格为 ceil(60 × 0.25)=¥15。本文案不另写价格，沿用现有价格 UI。 |
| Core/XgSim.cs:252–279、298 | 购买成功加入 S.unlocked，之后自动配置并 Evaluate 两条线。HasLrKnob 的 track 参数不限制作用线；shared.lr 或兼容旧 ID v.lr/s.lr 都满足。无需装备或手动启用。 |
| Core/XgSim.Progression.cs:63 | 旧节点 v.lr/s.lr 可迁移授予 shared.lr。这里只静态审查，没有读取玩家存档。 |
| Core/XgSim.AutoModel.cs:304；Core/XgCatalog.cs:135 | 无同项科技时 RateFinal=1；拥有 shared.lr 时为 0.9；拥有 lrschedule 或 AutoTrainLevel>=5 时为 0.85。0.85 优先，绝不是 0.9×0.85；旧 ID 同样不重复叠加。 |
| Core/XgSim.AutoModel.cs:356–368 | frac=clamp((parameters+data+structure)×RateFinal,0,0.97)。随后乘数据集误差范围，加不可消除误差，再施加结构限制、近亲惩罚及其他限制。0.9 是无单位倍数，不是实际学习率，也不是成绩+10%、准确率+10个百分点或总错误率必降10%。 |
| Core/XgSim.Progression.cs:255–265 | 例如未获得特征工程的 perceptron/logic 准确率上限为 0.55；即使公式系数降低，上限仍可能受此截断而不变。 |
| Core/XgSim.AutoModel.cs:199–239；Core/XgSim.cs:568–582 | AutoTechniques 已普遍启用 autoLr 并调用 SafeLr；SafeLr 不以 shared.lr 为开启条件。购买会使自动模型选择重新考虑上限；非训练中模型可能重新选型，因此不能承诺“购买完全不影响既有进度”。 |
| Core/XgSim.cs:517–528；Core/XgSim.AutoModel.cs:492–526 | Evaluate 更新 run.ceiling，但没有因本加成直接增加已存验证准确率；StepAccuracy 在有效训练时向上限移动，也可能退步。购买正在训练中的 shared.lr 仍会重算上限；自动选型延至轮次间，训练结算使用届时新上限。 |
| Core/XgSim.cs:686–691；Desktop/XgTrainPage.cs:467、477 | 玩家上限分数为 0–1000 分并向下取整；训练至少一轮（run.epoch>0）才显示上限数字和虚线。买后公式即生效不等于零轮次时立即显示，也不保证取整后可见变化。 |
| Core/XgTechTreeCopy.cs:15、53；Core/XgTechTree.cs:126–130、144–146 | 保留 NodeMystery 早返回与 NodeVisible 条件。shared.lr 本身非神秘节点；补丁不改变任何揭示逻辑，并把后续科技名称改为泛称“更强的同项加成”，避免主动剧透。 |

表中 Core/ 和 Desktop/ 的完整前缀均为 Assets/LingGuangV05/XingGuang/。

## 补丁及整合

- 目标：Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs。
- 基准 SHA256：308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8。
- changes.patch 附源文件 SHA256 和两处相邻源码上下文，只修改 shared.lr 的 Explanation、Effect，不改 Name、其他词条或共用逻辑。
- source-hashes.json 记录 8 个依赖文件的 SHA256；结束前再次核对这 8 个文件均未变化。整合时仍须再核对；若其他任务已修改同一文件，按 shared.lr 精确上下文重放，不覆盖整文件。
- 无缺失接口依赖；生产补丁尚未应用，由科技词条整合聊天统一整合。

## 验证状态和真实边界

本次完成源码静态核对与 git apply --check --ignore-space-change 的只读补丁适用性检查，后者退出码 0。未编译或运行 C#，未启动 Unity/游戏，未做视觉验收。既有任务记录的运行结果不计作本任务的运行结果。

待主聊天统一验证以下边界（均未运行，不记 PASS）：

1. 同一模型形状、数据和样本量，在无更强同项加成时购买：RateFinal 1→0.9；两条线均读取新值，Evaluate 刷新上限，无需装备。不要把模型自动换型造成的进度变化归因于直接加分。
2. 已有 lrschedule、AutoML 或两者并存时购买：RateFinal 仍为0.85；分别验证旧 ID 与 shared.lr 共存不重复乘。
3. perceptron/logic 的 0.55 硬上限、误差项仍被0.97截断、分数向下取整三种情形：即使购买成功，也允许上限显示不变；不能写“必涨”。
4. 训练0轮与至少1轮分别查看：零轮仍隐藏上限数字/虚线；已有训练时买后可刷新显示。正常训练后评估真实成绩，并允许抽中退步。
5. 训练轮次进行中购买与两轮之间购买：验证上限重算与延后自动选型的时点；保持原有样本、算力、可训练条件。
6. 中英文页面最小尺寸、缩放状态的换行与裁切；确认未发现节点仍按原规则隐藏。文案长度需要实际截图验收。

仅完成 shared.lr 的静态核对和可整合文案交付，不代表全局文案原则、整份策划或真实场景验收完成。
