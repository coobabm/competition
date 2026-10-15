# tech-022 — label.brain 本地模型推理

结论：名称和通俗解释正确，保留。现有效果说明方向正确，但没有说明对应标注台的检查点门槛、生效对象、一次性购买、判断替代关系及推理用量。交付一处仅改 `label.brain` 效果分支的文案补丁；未修改生产源码或模拟数值。

范围：仅当前获授权的《技能树词条策划》前三行要求。不读取新的策划正文，不扩展其他词条，不启动 Unity/游戏，不读取玩家存档，不提交或推送，不联系其他聊天。已核对 `Documentation/DesignPass/Agents/skilltree-task-001.md`；该记录已有总体文案层和基础检查，本交付只补充本节点的已确认遗漏。检索已到达的 Parallel100 result.md 时未发现本节点的重复交付。

## 可直接采用的中英文文案

**名称（保留）**：本地模型推理 / Local model inference

**通俗解释（保留）**：让本地 AI 阅读文字题，再给出自己的判断。 / Let the local AI read text cards and give its own judgment.

**效果（中文）**：第四阶段、已购置信度分流后可购买一次；无需装备或额外训练步骤。服务就绪后，本地模型为已开放且检查点准确率 ≥ 60% 的受支持文字台判断待判题；已有判断不重算，答案与把握度仍按原阈值分流。离线、离线结算或无有效结果时回退检查点。断电、研发或复核队列满时不派发新推理。每次实际派发计入 0.05 秒 GPU 用量（按电费规则结算，并非答题耗时）。本地判断替代而非叠加检查点评分；不直接增加速度或准确率。

**Effect (English)**: Buy once from stage 4 after Confidence-based routing; no equipment or extra training step is needed. Once the service is ready, the local model judges supported text cards at open desks with checkpoint accuracy ≥ 60%; existing judgments stay unchanged, and answers and confidence still use the current routing threshold. Offline service, offline catch-up or no valid result falls back to checkpoints. Power loss, research or a full review queue blocks new requests. Each dispatched request accounts for 0.05 seconds of GPU use under the electricity billing rules, not a response-time promise. Local judgments replace checkpoint estimates; this adds no speed or accuracy bonus.

补丁以 `XgCatalog.AutoMinAccuracy * 100`、`XgSim.BrainGpuSeconds` 动态读取上述 60 和 0.05，复用已有 `N` 格式化函数。价格仍由原科技页显示，不另写死：当前节点基础及购买价格 ¥1200、最大等级 1；label 节点不使用普通研究节点的 0.25 价格系数。

## 原文件证据（工程相对路径）

- `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:462` 将 `XgSim.LabelNodes` 加入真实目录；`:671` 的 `AutoMinAccuracy = .6`。
- `Core/XgSim.Collaboration.cs:33` 定义 `label.brain`，前置 `label.coop`，价格 1200、maxLevel 1。`:146–192` 的等级、购买状态和购买路径只在付费成功后写入 unlocked，无装备或训练操作；stage < 4 不可购买。此处及下方简写 Core/Desktop 均相对 `Assets/LingGuangV05/XingGuang/`。
- `Core/XgSim.Progression.cs:131–145`：label 使用自身价格；未拥有的 brain 在 stage < 4 时不可见，已拥有的节点保留可见性。
- `Core/XgSim.Collaboration.cs:217–244`：每台单独要求 DeskOpen 且 BestAcc ≥ .6；仅有较好的其他台检查点不足以启用此台。UsesBrain 要求已购买、受支持台、BrainOnline 且非 OfflineSimulation。预取只为合格台建题；没有判断、没有等待中的请求且不走本地模型时才生成检查点判断。
- `Core/XgSim.Collaboration.cs:229–231` 的受支持台：logic、arith、sense、danmu、spam、headline、review、translate、longtext、crosssentence、poems。文案不用这些内部 ID，也不披露尚未开放的台名。
- `Core/XgSim.Collaboration.cs:274–303`：TryTakeBrainTicket 跳过已有判断、正在等待的题；没有有效题面时回退检查点。派发前再次检查供电、算力、项目占用、队列容量及有效票据；成功派发才 `host.Train(BrainGpuSeconds)`，同一票据不能重复派发。`:11` 定义 .05 秒。
- `Core/XgSim.Collaboration.cs:310–329`：空、非有限或超出 [0,1] 的概率回退检查点；有效概率以 p ≥ .5 判“是”，confidence = max(p, 1-p)，judgeSource 设为 brain；不混合检查点评分，不直接改 BestAcc 或训练进度。派发后失败也已经计入用量，不承诺免费重试。
- `Core/XgSim.Collaboration.cs:373–388`：回答继续使用现有 coopThreshold 分流；低于阈值才进入复核（等于阈值不因低把握进入复核），题库比对和抽检等既有规则继续适用。本节点不附加报酬倍率或加速倍率。
- `Core/XgJudgeProtocol.cs:20–64`：只支持白名单题面；模式图案、缺失/过长题面等可回退。`:68–72` 请求仅输出“是/否”，不输出解释或推理过程；所以没有沿用旧目录“用自己的话判断”的短句生成暗示，也不声称展示思维链。概率不等于经过校准的答对概率。
- `Desktop/XgBrain.cs:47–99`：就绪依赖有效供电/算力、无研发项目、本地服务 JudgeReady 及已购买。实际连接由适配器更新；购买本身不保证服务立刻就绪。暂停、离线结算也不发起真实推理。
- `Desktop/XingGuangHost.cs:53–58` 仅在 Live、PowerCostsEnabled 且 seconds > 0 时转入 BillTraining；`Assets/LingGuangV05/Core/ChapterOneSim.Bills.cs:186–207` 按当前硬件耗电、阶梯电价及磨损规则处理。因此 .05 是每次派发的模拟 GPU 计量秒数，不是固定人民币价格、实测耗时或每秒 20 题承诺。
- `Desktop/XgLabelPage.cs:972–979` 显示本地来源/回退检查点和近期判断准确率；这属于可见结果，但近期统计不能当作节点保证的准确率增益。

## 神秘性与整合边界

`Core/XgTechTree.cs:127–130` 通过 NodeVisible 控制可见性；`Desktop/XgTechTreePage.cs:426–430` 对不可见节点关闭对象并清除钉住状态。因此未拥有的本节点在第四阶段前隐藏。`XgTechTreeCopy.Name/Note` 原有 NodeMystery 早退保持不变；`XgSim.NodeMystery` 当前专门对应自动标注的发现状态，不为 brain 人为扩大或取消神秘性。补丁不添加未来台名、并发节点介绍或其他词条效果。

补丁目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`，原效果位于第 191 行。原文件 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。`changes.patch` 带前后相邻分支上下文，只替换这一处分支；其他人的补丁可能同时修改该文件，整合时须核对当前内容，不能整文件覆盖。完整依赖哈希见 `source-hashes.json`。

## 验证状态及待验证边界

已执行：静态源码追踪、精确旧文本匹配、`git apply --check --ignore-space-change` 补丁适用性检查（退出码 0）。这只说明补丁可匹配当前源码，不代表 C# 编译、游戏运行或 UI 验收通过。未执行 Unity、C# 编译、模拟运行或视觉验收。

由整合负责人统一执行的验证：

| 边界 | 应观察的结果 |
| --- | --- |
| stage 3 未拥有 / stage 4 未购前置 / stage 4 前置与资金满足 | 依次隐藏 / 锁定 / 可买；购买一次后不可再次购买，无装备步骤 |
| 已开放支持台检查点 .599999 / .6 | 前者不生成该台自动待判题；后者达到门槛；另一个台 .9 不替代本台门槛 |
| 买入前已存在 checkpoint 判断 / 买入后的待判题且服务就绪 | 已有判断保持；待判题使用本地结果；不要求额外跑训练轮次 |
| 服务不可用、离线结算、无有效题面、无效返回 | 在对应回退路径使用检查点；离线结算不派发真实推理 |
| 队列满、研发、供电不可用 | 不派发新请求；恢复后不重复结算同一票据 |
| 同票据两次派发、派发后失败、派发前拒绝 | 每次有效派发计一次 .05 秒；失败不抹去已计用量；未派发不计量 |
| p = .5、0、1；NaN、负值、>1 | .5 判是且 confidence .5；端点有效；非法值走回退；无固定准确率提升 |
| 中英切换、最小窗口、长提示文本 | 效果完整可读，必要时可滚动；第四阶段前无名称、效果或价格泄露 |

以上均为待运行用例，未标为 PASS。当前补丁文字较长，实际提示框排版需视觉验收；不能用静态字符串存在替代验收。

交付日期：2026-10-13（Asia/Singapore）。结束前依赖复核结果见 `verification.md`。
