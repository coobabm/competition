# 灵光 六阶段剧情、横向科技树与人机协作标注 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement task-by-task. User authorized implementation to completion; do not stop at planning. Track every step below and in the requirement matrix.

**Goal:** 在当前 Main 场景完成《六阶段剧情与科技树设计》《人机协作标注设计》《训练动作与技能树设计》的全部适用要求，并以核心测试、真实推理、六阶段机器人和 Unity Play 验证证明。

**Architecture:** 保留纯 C# XgSim/XgState、Unity 主运行时、DreamOS 适配边界。使用同一 XgSim 的 partial 文件隔离阶段与协作规则，不额外建立第二份权威状态；LocalLlm 仅返回预测，结算与迁移仍在 Core。阶段优先级高于旧三树布局，但原训练动作与反馈要求未被取代的部分仍需完成。

**Tech Stack:** 当前 Unity 6000.4.5f1、C#、uGUI/TMP、现有 llama-server、本地 GGUF、NUnit/.NET 测试；不新增付费服务或第三方依赖。

**Spec:** `/Users/kk/competition/Documentation/LingGuang-Incremental/六阶段剧情与科技树设计.md`、`人机协作标注设计.md`、`训练动作与技能树设计.md`（均按当前文件读取）。

## Global Constraints

- 工作项目 `/Users/kk/competition`，活动场景 `Assets/LingGuangV05/Scenes/Main.unity`；不得误改 `/Users/kk/unity` 或旧独立 LingGuang 原型。
- 在当前已打开、含大量未提交/未跟踪内容的工作树增量实现，不 reset/clean，不自动提交用户现有文件，不用全新 worktree 丢失这些当前内容。修改前快照在 Evidence/SixStage/baseline。
- 不手改 Unity 场景或 meta 序列化，不修改第三方 DreamOS 代码，不改全局构建列表；优先运行时接管。
- 第一章到 Transformer 研发/最后是非题/历史字幕结束，第二章只交付蒸馏/转生入口，不实现第二章本体。
- 模拟 CNN/RNN 的数值成长与真实本地 Qwen 推理分开；checkpoint 预测/真实 brain 预测必须在 UI 标明来源。
- 不将 truth、why、roll 或任何答案字段送入本地 Judge；聊天正文作为数据，模型无状态修改权。
- 同一存档保存阶段、队列、已判断结果和研发实验；一次题目只结算一次，旧回包不能作用于新存档。
- 所有新增玩家文案中英齐全。低特效模式关闭震屏/顿帧，突破白闪提供减弱路径。
- 稳定算法成本：每 Tick 有界队列/预取；存档与补算不触发批量真实推理；不额外声称本机历史测量可泛化到所有设备。

## Decisions / spec interpretation

1. 采用推荐：另一专长随时可开、第二线突破×3；拒稿结尾+历史字幕；全局自动10级；队列20满暂停；1噪声抵2好样本。数值集中定义，便于后续机器人调参。
2. 新六阶段覆盖旧科技树默认镜头：打开停在前沿，工具栏可以返回⓪标注；未解锁列/页签/桌不显示而非灰锁。
3. 起始免费数据含 MNIST 与关键词短信；组合墙用明确 XOR 挑战，在阶段1作为墙挑战可见，不把未购买的完整逻辑桌提前打开。20次观察模型在组合挑战的失败触发墙，不强迫玩家故意答错。
4. 全局 stage=两线最高阶段，但每线保留各自阶段。视觉阶段5有空间注意力突破；caption要求两线均达5，避免视觉死路。
5. 2016 的 Transformer 属明确虚构提前突破。arXiv 不被叙述成正式同行评审，拒稿信为虚构投稿/审核退回；历史字幕与游戏剧情分开。
6. 历史文本中的“真实并行所以固定×0.3、GPU31%”属于游戏数值，不宣称硬件实测；热力图为教学可视化/模型报告关键词，不宣称真实神经注意力证明。
7. 新档通过 constructor state==null 与旧档区分。外层存档 version=1 保持兼容，新增 progressionVersion/coopVersion做幂等迁移；旧 LeNet/RNN授予前两阶段，已买的更高级架构迁移对应线与剧情已播记忆。
8. 本轮已停止 Play，防止边编辑边更新玩家存档。后续 Play 验证使用隔离测试存档/测试状态，不能污染玩家正常进度。

## Review Focus

- 新档被误当旧档而跳过感知机；旧档反被重置：Task 1/2 以空旧状态、丰富旧状态、二次迁移、Unity JSON往返验证。
- 隐藏UI但路由/购买仍绕过门槛：Task 1/4 同时测试API门槛和真实按钮/页签可见性。
- 异步晚回包、排队重复、关窗/读档/断电导致重复发钱：Task 2/3 以票据ID、Sim身份和幂等结算测试。
- 模型高置信错误与概率缺项：Task 2/3 测logprobs规范化、缺失/非法数字、真值隔离、fallback来源、噪声与抽检。
- 最后一条专长无法到阶段5、研发无电仍跑、拒绝实验/重载重复奖励：Task 1/5/7 完整路径与存档重入验证。

## Task 0: 基线与可审计计划
- [x] 读取三份规范与活动项目；标记规范优先级与推荐默认值。
- [x] 保存原始代码/文档/场景来源快照与hash（Evidence/SixStage/baseline）。
- [ ] 运行 XingGuang、Story、Persistence 基线并记录现有失败（不把历史失败当本轮通过）。
- [ ] 建立逐条 requirement→code/test/runtime evidence 矩阵。

## Task 1: 六阶段核心与科技目录
**Ownership:** `Core/XgCatalog.cs`、新 `Core/XgSim.Progression.cs`、`Core/XgProgressionModels.cs`、`Tests/Editor/XgProgressionTests.cs`。原 XgSim/XgModels 的接线由 root 统一处理。
- [ ] RED：new XgSim() 必须 stage1/perceptron；旧含lenet/rnn状态必须迁移阶段3，两次修复不再给钱或重复事件。
- [ ] 增加 stage/lane/maxLevel、Breakthrough/Project 节点、感知机/MLP/caption/transformer、每线阶段与初始免费桌。
- [ ] RED/GREEN：墙精确数值、20次墙观察、墙解释后才暴露突破、评级/论文/钱门槛、第二专长×3、视觉阶段5与caption双线门槛。
- [ ] 接入样本/容量/训练/桌子/科技购买共同门槛，不只改变显示。
- [ ] RED/GREEN：600 GPU秒研发，三题按次序触发；否退进度不重复扣启动费；断电暂停；重载续跑；完成收益一次；两种结尾答案都闭章。
- [ ] 跑 `dotnet test Tools/XingGuang/tests/Tests.csproj`，保存red/green证据。

## Task 2: 人机协作纯规则
**Ownership:** 新 `Core/XgSim.Collaboration.cs`、`Core/XgCollaborationModels.cs`、`Tests/Editor/XgCollaborationTests.cs`；原核心接线 root 负责。
- [ ] RED/GREEN：global auto=max(legacy levels)、7个标注节点与多级加薪/自动共享购买、stage4 brain门槛。
- [ ] 卡片稳定ID、judgeSource/guess/confidence/brainP、每桌预取3、待复核总量20、已判断结果存档。
- [ ] RED/GREEN：Route阈值0.5–0.99、默认.8；queue满暂停；按实测返回结果分流前不使用truth；checkpoint模拟来源明确。
- [ ] RED/GREEN：正样本/噪声×2/抽检40%/难例×1.5和2样本/纠错+2连击/重复提交不付款。
- [ ] 近100路由统计、每桌近50真实brain统计、最近6自动通过记录；全局自动与训练自动分开。
- [ ] 全部规则在纯C#测试，无网络/Unity依赖。

## Task 3: 真实本地Judge与后台调度
**Ownership:** `LocalLLM/LocalLlm.cs`、新 `Desktop/XgBrain.cs`、纯请求/概率解析 helper 与对应测试；Controller root统一接线。
- [ ] RED：从允许的题面字段构造请求，序列化不包含truth/why/roll；是/否 logprobs缺项、空白token、NaN、Infinity、概率归一化可测。
- [ ] 新增 Judge，max_tokens=1、temperature=0、logprobs/top10、thinking off；原聊天接口保持兼容。
- [ ] 聊天优先/独立低优先级判题队列；有限请求数；无重试风暴；np1→4受控重启并隔离进行中请求。
- [ ] 预取与缓存；断电/暂停/重载/销毁取消或拒收；每次实际判断0.05 GPU秒计费；失败回checkpoint且标离线。
- [ ] 用包内服务和模型实际判文字题，保存请求脱敏/响应/耗时及真值未发送证据；检查聊天不被后台饿死。

## Task 4: 横向科技树与渐进界面
**Ownership:** `Desktop/XgTreePage.cs`、`XgShapes.cs`、`XingGuangView.cs`、`XgRepoPage.cs`、必要UI测试。
- [ ] stage列560宽、6泳道、label列0，未解锁内容根本不生成/显示；突破墙开裂/时代卡/镜头滑动。
- [ ] 滚轮横移、Shift竖移、拖拽惯性、缩放；小地图只显示已解锁列；前沿/⓪/可买/−/+工具条。
- [ ] 多级节点分段环与Lv；节点位置不重叠、父边合法、无环验证。
- [ ] 训练等老周消息播完；技能树等首次评估收入与提示；订单首论文；仓库首检查点；存入仓库首轮；隐藏页不能Open绕过。
- [ ] 旧档迁移后可见性恢复但不重播出现动画。

## Task 5: 六幕剧情、说话与新任务
**Ownership:** Story JSON/lines/ChapterOneStoryVars/StoryDirector/Presenter、LlmPersonas、YYChatHub/View、MailInboxView、Localization。
- [ ] 逐幕 wall/breakthrough/记忆/Tay/关注词/提案/三实验/拒稿/最终题/历史片尾节拍及英译。
- [ ] 幕1保留倒叙，幕2起仅当时角色消息；{time}使用游戏时钟；阶段迁移已播标记和补发时代卡。
- [ ] 六级语言限制与上下文1/1/2/14/14/可容纳历史、输出2/4/24/60/100/140token；第3幕联系人出现。
- [ ] 阶段4引用玩家真实旧话，不编造；阶段5模型报告关注词匹配原输入并安全下划线；阶段6反问。
- [ ] 新长句/跨句/attention题和caption区域；题面与真值独立确定；难例返训练。
- [ ] 最后一题两答案、邮件退回、明确2017年史实字幕、第二章入口不可冒充已完成章节。

## Task 6: 训练动作、图表与协作标注UI补齐
**Ownership:** `XgTrainPage.cs`、`XgLabelPage.cs`、`XgGraphics.cs`、`XgDeskGraphics.cs`、`XgJuice.cs`。
- [ ] 0.6秒动作/重复限制、阶段图表权重28×28/MLP/记忆/深度退化/attention/GPU，旧图缩小保留。
- [ ] 评估数字滚动0.8秒/评级印章/手动居中可关闭，自动等级4后小卡3秒自动消失；不遮住必要输入。
- [ ] 数据包拖显卡安装与检查点拖订单；重复点击/取消不重复付费；必要事件的反馈组合/音效变体/降级特效。
- [ ] 标注协作队列角标、guess把握与来源、.5–.99滑块、三统计、最近6通过流水/抽检黄卡回队、纠错反馈。
- [ ] 研发进度/3实验/结尾题与剧情衔接；阈值等协作购买后才出现。

## Task 7: 全路径验收与收尾
- [ ] 所有核心、Story、Persistence、UI EditMode测试；确认没有新增Console errors。
- [ ] 更新六阶段平衡机器人，完整跑到两种结尾并输出0–115min各里程碑；估计与真人试玩区别明确。
- [ ] Unity Play从新档验证起始仅必要UI、至少每阶段一张截图、墙/突破、两线、队列/噪声/真实Judge、研发三实验、结尾。
- [ ] 旧档迁移、存档往返、后台关窗/断电/回包/重启；不读写正常玩家存档作测试。
- [ ] 独立代码审查按requirement矩阵检查遗漏与安全边界；修复并复测。
- [ ] 文档/Graphify清单按最终实现更新；保留任务证据、列出未验证平台；只有全部要求有足够证据才标goal complete。

## Execution ledger

- 2026-10-03：已确认三份规范，停Play并备份。两名代理审核六阶段/协作，第三名运行基线与训练差距检查。尚无生产修改。

- 2026-10-03 implementation checkpoint: root integrated partial progression/collaboration, async BeginEpoch completion, feature visibility, actual YY delivery gates, 25 bilingual story beats, six-stage speech policy, attention-word safe rendering, story rejection mail. Core .NET last root run 133 passed; worker latest141 passed includes new protocol fixtures; Story22 passed. Unity refreshed/compiled with zero error CS. Editor-only presentation QA6/7: OnDisable restore assertion pending lifecycle diagnosis, not called a pass. Unity EditMode job f3d6ba297e1442c0bb6ffcf650c6a525 started. All goal remaining gates active.
- Agent task ownership: six_stage_audit owns progression/catalog/tree/shapes/bot and now cross-reviews coop/inference; collaboration_audit owns coop models/rules/label UI and now cross-reviews progression/story/root integration; baseline_and_training_audit owns LocalLlm/JudgeProtocol/Brain/train UI/graphics/juice/presentation QA and live model smoke on isolated port18089. Root owns originalSim/Models/Controller/View/StoryBridge/StoryDirector/StoryVars/story JSON/YY/Mail, actual Unity tests and Play checks. No commits, resets, scene serialization edits.
- Natural economy bot evidence: first vision 56:37; first sequence73:00; slower4s reading+native power outage+one research retry90:49. Both ending answers proved for each. These are bot profiles, not proven human playtime; no resource injection.

- Play validation checkpoint: real Unity EditMode299/299 earlier; isolated Play diskSave=false, Story lifecycle helper7/7 (actual UpdateChat/Bind/Title startup; normal state restored), training presentation lifecycle7/7. Actual LCD click raised handCorrect0→1; epoch click left count20 immediately then21 only after duration; live brain in Play22 judged spam records, queue reached20 and auto stopped, LCD review reduced20→19 and gave2samples. Prepared stage1/2/3/4/5/6 screenshots in Assets/Screenshots with JSON snapshots under Evidence/SixStage/unity; prepared fixtures are NOT natural-progression evidence. Stage6 real story/mail/final no-answer/credits flag all completed in the isolated fixture. Normal player save SHA remains a2a6440087d8ecd8d132ae65cf7391f0cd9e65e1aff2957385fddd5139205729.
- Three cross-review P2 fixed and reverified: only actual delivery opens walls; unfinished purchased breakthrough recovers after reload; stale pending chats scoped to original ChapterOneSim and cleared on Rebound. All new six-stage say steps now await delivery ack before beat commit.
- Remaining: rerender/verify fixed Tree height-fit (prior screenshot clipped header/bottom); verify true caption wording/bilingual binding; isolated owned-server1→4 in Unity (main18088 is pre-existing external PID83614 started16:43, NEVER kill it); drag/hold and all-phase UI/English/full acceptance audit; final docs/Graphify refresh; restore original Editor useDiskSave setting after QA.
- Evidence incident: legacy smoke --help unintentionally ran second actual40 requests and overwrote first raw4 files. Original raw NOT restored; first summary historical only. Incident raw/new-summary independently revalidated40valid/37correct, Python2P2 fixed and22 guarded offline tests passed. Preserve distinct evidence sets, do not cite historical37/40 as independently reproducible first run.

- Fidelity follow-up: coverage audit found invisible/epoch-only structure/length/translation barriers and missing phase2 uncertainty scene. These are mandatory remaining work, not waived by green tests. Implement concrete preview challenges inside already-open desks so later desks stay hidden; real long source/candidate translations, spatial pattern pairs, counted memory distance; observe actual previews before walls. Phase2 uncertainty is forwarded by Lao Zhou (the direct LingGuang contact still unlocks at3), resolving the spec's channel conflict without leaking the future contact. Shared compression wall wording covers both sentence and image information loss.

## C7 clarification: existing-noise cleaning, separate from future relabelling

The current-tense promise that the training page can clean noise is fulfilled by a bounded **discard-known-noise** action on the simulated dataset. This is not the optional future workflow of manually relabelling stored bad examples, and it never alters the local GGUF.

- Core API: `CleanableNoise(dataset, host, maxCount=10)` is read-only; `CleanNoise(dataset, host, maxCount=10)` returns the actual number discarded.
- Requires `label.audit`, normal power and positive compute, and no active research project. Each call discards at most 10 known noisy rows; it uses the requested, available, affordable subset.
- Central tunables: `NoiseCleanBatchLimit=10`, `NoiseCleanMoneyPerItem=2`, `NoiseCleanGpuSecondsPerItem=.1`.
- A successful batch spends once, bills its GPU work once, decreases only that dataset's noise, and reevaluates its current training run. It does not create clean samples, training steps/epochs, best checkpoints, papers, income or combo points. Existing good-sample totals do not change.
- Empty/repeated-empty/locked/unpowered/project-blocked/invalid/insufficient-funds requests have no spend or electricity side effects; a rejected `host.Spend` leaves noise and electricity unchanged.
- Training-page owner adds a `label.audit`-gated button using `CleanableNoise`, plus current noise and effective-sample information, and explicit wording “模拟数据集清洗 / simulated dataset cleaning; not GGUF training”.
- Verification: 15 new failing tests before implementation (`Evidence/SixStage/noise-clean-red.log`) then 15 passed (`noise-clean-green.log`); full current Core suite 184 passed (`noise-clean-full-suite.log`). UI rendering and actual click/billing remain for the centralized Play pass.

- Continuation checkpoint: see Evidence/SixStage/NEXT.md. Latest observed Core184/184 +Story23/23. Play stopped, original useDiskSave=true restored, sceneDirty=false, normal player save hash unchanged. Goal ACTIVE. Current source has unimported incremental edits; refresh/check compilation before next Play. No hidden live QA process was left; external pre-existing18088 PID83614 untouched.
