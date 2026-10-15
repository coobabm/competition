# tech-006：caption（图像描述生成）文案核对

日期：2026-10-13（Asia/Singapore）。范围仅为当前授权的专业名称、通俗解释、玩家可见真实效果；未读取新增策划正文。

## 结论与交付

现有专业名称「图像描述生成 / Image captioning」正确，无需修改。当前解释借用目录中的「边看图边说话 / Looks and talks at once」，效果借用所有架构的通用描述，未说明实际可见的配图描述核对题，也没有区分教学示意与真实图像模型生成。建议采用本目录 `changes.patch`，仅在 XgTechTreeCopy 的 Explanation、Effect 各加一个 caption 分支；不修改模拟数值或其他词条。补丁尚未应用到生产源码。

已核对既有记录 `Documentation/DesignPass/Agents/skilltree-task-001.md`：其通用架构说明和 Name/Note 接入已完成，本交付不重做这些工作；本任务目录开始时无已有交付。

目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`

补丁源文件 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。全部关键依赖摘要见 `source-sha256.json`；整合前必须重新比较。

## 可直接采用的中英文文案

**名称**：图像描述生成 / Image captioning

**中文解释**：用一句话描述图片里有什么、正在做什么。

**中文效果**：购买后，后续新生成的表情包题会让你核对图片与预设描述，并标出教学关注区域；无需装备或先训练，未调用图像模型生成描述。同时加入视觉训练的候选结构，由系统在训练轮次之间按数据、显存和已购上限选择；不保证立即切换或提分，需训练后查看评估。同一训练线只用一种结构，架构效果不相乘。

**English explanation**: Describe what is in a picture and what is happening in one sentence.

**English effect**: After purchase, newly generated meme cards ask you to check a preset description against the picture and highlight a teaching focus region. No equipping or training is required for these cards; no image model generates the descriptions. This also becomes an available vision architecture. Between epochs, the system chooses a model using the data, VRAM and purchased caps. A switch or score increase is not guaranteed; train and check the evaluation. Each track uses one architecture at a time; architecture effects do not multiply together.

## 生效与数值合同（供整合核对，不必塞入玩家提示）

- 正常购买路径：阶段 ≥5，已拥有 resnet 与 attention，资金足够。目录标价 20000 乘 `StoryResearchPrice = .25` 后向上取整，实际一次性价格 ¥5000。不是每秒或每轮费用。价格和前置条件已有页面独立展示，不在新文案重复硬编码。
- `BuyNode` 成功后立即拥有 caption，且会尝试自动配置；若该训练线正在一轮训练中，`AutoConfigure` 跳过，轮次之间再选。已拥有不能重复购买，不存在 caption 升级叠层。
- 描述核对题仅看 `Has("caption")` 与 `card.dataset == "meme"`，不检查正在使用的架构、训练轮数或检查点成绩。后续题目生成经过 `DecorateProgressionCard` 时生效；购买过程不改写已经生成的当前题或旧复核题。旧突破迁移也可授予 caption，同样适用“拥有”条件。
- 教学题从 5 条预设中英文脸部描述选择候选；由种子决定匹配/不匹配，玩家判断是否准确。区域标记是预设教学区域，不是模型内部注意力；不能宣传为真实看图自由生成、逐词生成或调用视觉模型。题型也不意味着自动答对。
- caption 为视觉专属架构（非 shared）。自动选择仅从当前训练线可用且已拥有的架构中择一，优先比较能达到的评级，再比较参数量、上限成绩、偏置系数；不会把 caption 与 resnet 的架构参数或偏置相乘。
- 被选中后，模型参数量为 `层数 × 每层宽度² × 2` 个参数；`ParamsK` 返回上述数量 /1000，单位为千参数 K。这里 2 是该架构的参数系数，不是购买后现有参数翻倍。
- 显存估算为 `300 + 参数量(K) × 0.016 + max(1, 层数) × 每层宽度 × 0.02` MB。无“显存减半”承诺；系统仍受已购宽深上限和可用显存限制。
- 架构深度限制 999 层，实际自动配置取已购层数上限与架构限制的较小值。不是购买即获得 999 层。`span = 999`、`seq2seq = true` 是目录属性；序列数据的跨度/输出惩罚只在序列分支使用，而 caption 不能被自动选择到序列线，不能据此宣称解锁翻译或长文本能力。
- `bias = .3` 进入成绩上限公式的参数不足项：`.3 × .25 × (CeilingScaleK / 有效参数量K)^.6`。这是无量纲误差项系数，非 +30% 准确率、+30 分或立即降低固定错误率。实际成绩受样本、模型形状、噪声等共同影响。
- 切换架构通常会重建当前模型的数值训练进度；拥有 transfer 时保留的是高于随机基线部分的 60%，不是最终准确率乘 60%。旧训练版本首次自动接管有保留进度例外。此为共有换结构逻辑，不在本任务修改。
- `ScaleItems` 没有 caption，故不附加独立预训练有效规模倍率。已拥有其他技术的全局效果仍照常存在，不因 caption 被重复计算。

## 原文件证据

以下行号基于本次核对快照，方法名用于源码继续更新后的定位。

| 文件（均位于 Assets/LingGuangV05/XingGuang） | 位置及依据 |
| --- | --- |
| Core/XgTechTreeCopy.cs | 15、23、50：名称正确，Name/Note 的神秘节点和图鉴保护先于新增分支；70、146：架构目前使用目录短句与通用效果 |
| Core/XgCatalog.cs | 143–144：caption 的视觉属性、系数和原 note；434：第 5 阶段、resnet 父节点、attention 额外前置与标价 |
| Core/XgSim.cs | 198–204：拥有、可见、前置与价格判断；252–280：购买授予、自动配置、评估；351–368：参数 K 与显存 MB 公式；404–410：深度限制；1289、1355：生成题目时修饰题型 |
| Core/XgSim.Progression.cs | 20：架构与训练线匹配；106：Grant 去重；130–146：价格折算及可见性；163：caption 阶段门槛；350–388：仅拥有 caption 的 meme 题转换；526–555：预设候选描述、真值和教学区域，无图像模型调用 |
| Core/XgSim.AutoModel.cs | 134–155：自动形状受已购上限与显存约束；166–186：架构择一；219–266：轮次间配置与切换；270–281：进度保留相对随机基线；333–368：真实成绩上限公式 |
| Core/XgSim.Board.cs | 66：caption 使用 LocalShared 连线，未单独实现图片到自由文本的解码器 |
| Core/XgSim.Finale.cs | 75–85：预训练倍率清单不含 caption |
| Core/XgSim.Abilities.cs | 251：旧 bt.spatial 授予 caption |
| Core/XgTechTree.cs | 127–130：节点可见性沿用 NodeVisible |
| Desktop/XgTechTreePage.cs | 428：隐藏未到阶段节点；758、772：神秘提示早退及 Note 接入；781–783：前置/阶段条件单独展示 |
| Desktop/XgLabelPage.cs | 593–597、612–620：显示双语描述、候选判断题和教学区域 |

## 神秘性

caption 未拥有且阶段不足 5 时按 NodeVisible 隐藏；已拥有的旧迁移状态遵从原有可见行为。本补丁不改阶段、可见性或前置节点关系，不提前展示其他未来词条。现有 NodeMystery 专用于 label.auto，不能误写成 caption 自带“？？？”分支；Name/Note 原有保护完整保留。

## 验证与待整合

- 静态检查：已沿目录、购买、自动配置、出题与显示调用链核对；最终补丁适用性与依赖复核结果见 `verification.txt`。没有以既有任务的运行结果冒充本补丁测试结果。
- 实际运行：未运行 C#、Unity 或游戏，未读取真实玩家存档；无运行 PASS 结论。此为可逆文案补丁，不新增镜像实现的单元测试。
- 视觉验收：未执行；主聊天需检查中英文长效果段落在最小窗口、缩放、提示框内是否裁切。
- 待整合：比较源摘要和上下文后，仅并入 caption 两处分支；若同文件已有其他子任务修改，按方法与节点合并，勿覆盖整文件。统一编译由主聊天负责。
- 待实际边界验证：未拥有/已拥有 caption；拥有但实际视觉架构仍是 resnet；训练活动中购买并在轮次之间重配；现有题保持、下一道新 meme 题出现描述核对；非 meme 不受影响；中英文匹配与不匹配候选均可正确显示；第 4/5 阶段可见性及原有神秘节点保护。所有这些均为待验证项。

只写入本任务专属目录；没有更改生产源码、全局清单或他人交付，没有创建或联系其他聊天，没有启动、提交、推送或打包。本结论仅覆盖 caption，不代表整份策划或全局文案原则完成。
