# 大脑窗口接管报告（第一、二批）

2026-10-19。生产目录 `competition-latest-20261016`。本报告对应 Window/Feedback 负责部分；底图、侧视区域、统一颜色、视觉棋类映射及旧动画层由 takeover_tieba 负责，参见其报告。这里没有启动 Unity、Player、模型，没有运行编译、检查入口或实窗截图；静态完成不等于运行验收通过。

## 输入与文件边界

已读 `ClaudeReturnRound2/E2/大脑窗口光效_设计说明.md` 与 `ClaudeReturnRound3/brain-v2/brain_window_v2.html`，结合主控本轮明确授权实现。不改游戏能力、购买条件、学习/存档模型、角色性格或终局。Glass 经核对是其它应用窗口的不透明背板保护，本次没有修改。

编辑：

- `Assets/LingGuangV05/XingGuang/Desktop/XgBrainWindow.cs`
- `Assets/LingGuangV05/XingGuang/Desktop/XgBrainWindowFeedback.cs`
- 新增 `Assets/LocalBuild/Editor/BrainWindowFeedbackChecks.cs` + `.meta`

开始时两个既有文件无本地修改。原始 Git blob：Window `e1a4ea3686eb5c4612581f1d89895af8b289f925`；Feedback `48ff3f22e2826765bc879396627a5182f800f587`。

## 已实现

- 窗口标题栏、按钮、文字采用 .exe 暗色。只改本窗口，保留购买 brain.window、序章门禁、拖动、置顶、关闭/任务栏重开。
- 虚拟画布保持 1280×832，缩放取两个轴的较小比例，留边且不压扁。反馈层从客户区迁入同一 virtualBrain，使用共用 `XgWallpaperConcepts.RegionCenter`，避免等比留边使光效错位。
- 新接真实 `StageAdvanced/Emerged/EpochStarted/Assessed/DataFileInstalled/Diverged`，保留已有事件。按源码真实签名处理：StageAdvanced 的参数是旧阶段，显示 from+1；DataFileInstalled 是 fileId，先查已 installed 文件再取 dataset；Assessed 无 pass 字段，因此显示实际准确率/等级，record 时用对应反馈，不捏造“考试通过”。
- 阶段与涌现待播状态合并为最多一个最新阶段/能力，等 `StagePresentationHeld` 与全屏 Curtain 均释放后才播。关闭窗口保留当前档的待播阶段，切档/解绑清队列、NaN、标题、字幕及反馈；Current 检查阻止旧 Sim 回调污染新档。
- NaN 显示静态 coral 标记和 loss=NaN 标题；同显示区下一次真实答题/训练恢复状态。只展示真实分歧事件，不闪、不抖。
- 减少特效条件为保存设置 reduceFx、View.Juice.Reduced、Low 任一开启。Feedback 不推进任何动画，只在事件/设置变化重画；字幕保持静态。整个窗口其它挂件的 OR 修正由 takeover_tieba 完成。
- 取消原整窗 α.85 白块。阶段/能力反馈为细环，峰值 α.38；普通结果统一 mint #5FE3B5 / coral #FF7A6E。
- 每次事件都有静态状态；超限只更新静态反馈，不再次启动高亮。ConceptsChanged 不再另起第二次动画。

## 第二批真实签名与信号

真实 settled 答题（仅答对展示成功签名）、完成训练、真实数据安装根据 dataset 选择签名：

| dataset | 实现 |
|---|---|
| sense | 六点字链，末点 mint |
| spam | 两侧 coral 拦截括号与中心方点 |
| poems | 两行五点，末点 amber |
| translate | 两行四点与交错连接 |
| mnist | 十二个像素方点组成零形签名 |
| go | 5×5 棋盘线、中央棋子、四气环 |
| chess | amber L 形马步 |
| logic / arith / xor | Y 分支，mint 与 dim coral 两端 |

CIFAR/ImageNet/meme 暂复用像素签名，其它题库用通用字链。这里是题库签名，不宣称当前输入真的识别为零、真的学会指定概念。没有可靠 concept ID 的反馈落区中心，不把区域里第一个旧概念冒充这次学会的落点。

- 流入束 180ms，6 段尾迹；训练最多 4 束，间隔 45ms；落点后 2 条 100ms 分叉，分叉 α≤.22。不随机产生空闲念头/灰尘，不把视觉演示写成模型能力。
- 至多一个当前签名/主反馈动画，不累积粒子列表或异步计时器。签名最多 12 点；底层静态区域标记最多 4 个；事件动画最多 30fps、1.2秒后停更。没有实时模糊。
- 沿用 MaskableGraphic 和客户区 RectMask2D，所有原语由已有 XgDraw 程序生成；未引入纹理、材质、第三方图片或 shader，也没有实现 additive/128px 图集。无新增图片许可待处理；本次程序形状来源就是用户交付原型与现有代码绘制助手。

## 共享起始门禁（静态审核完成，未实测）

纯对象 `XgBrainReactionGate` 统一记录任意1秒最多3次高亮起始，且同 region 至少间隔1/3秒。Window 公开 `OnsetGate`，Feedback 与 takeover_tieba 接入的旧层共用该实例：

- Feedback 结果/阶段/能力/数据/训练反馈。
- AnswerPath 参与节点 glow 起始（拒绝时仍留低亮真实路径）。
- Training 开始确认光晕、概念生灭高亮（拒绝时仍刷新真实 cell）。
- Concepts 教学 LessonPulse 起始。

普通低亮路径流动、已存在的柔和连续绘制不算新 glow 起始。本次不宣称“全窗口所有亮度变化经过认证”或已完成光敏实测；共享门禁能覆盖上述明确接线点，仍需主控在同帧多事件场景核验真实视觉。旧墙纸模式本身不受这个小窗门禁改造。

## 棋类与真实数据边界

主控最终要求仅视觉重归类。由另一路新增 `XgBoardData.DisplayRegion("chess") == "logic"`，底层 `Region("chess") == "sequence"` 保持不变；go 沿用既有视觉区域。Window 签名/事件用 DisplayRegion，历史词概念依旧按真实保存区绘制，不迁移历史词点，不改变学习平衡。不能把这批工作描述为底层棋区迁移。

## 已检查 / 待执行

已做：事件签名/调用先后核对（Diverged 先于 EpochDone，避免重复 NaN 反馈）；订阅/解绑逐项核对；共享坐标/API 对接；GUID 唯一性；git diff --check 无错误（仅行尾规范提示）。没有编译/执行。

新增 `BrainWindowFeedbackChecks.Run` 待主控在 Edit Mode 执行：只创建并销毁 inactive 临时对象，不开场景、不读写存档/PlayerPrefs；通过 ReducedMotion=true 短路 Low getter。覆盖门禁窗口/区间、极端比例等比适配、棋视觉/底层分离、每事件静态回应、时间不改变静态网格、八签名顶点上限及范围、错误回答无成功签名、Reset 清状态。

仍须执行：Unity 编译；上述独立检查；既有 BrainWindowPlayQa/PerformanceQa；实际购买/关闭/任务栏重开/缩放/置顶；从两种设置分别开启减少特效后整个小窗 mesh 是否静止；切档旧回调和阶段幕布补播；中英小窗字幕截断、真实事件高速连发、裁剪/性能；最终 Player。

## 尚未完全实现的 v2 内容

- prototype 的三层纵深神经丝、完整自适应结构细节、全套阶段2.4秒编排及5.3秒标题序列。
- 完整按安装顺序排列的所有持久插槽/生长路径、安装顺序恢复。当前事件束从共同输入口流向实际显示区。
- 聊天赞踩三轴游标、八种以外的数据签名细分、按真正新概念 ID 精确落点。
- additive 材质/128px 自制图集（本批只用兼容 Mask 的alpha几何）。
- NaN 整区渐变灰化/虚线、700ms恢复；本批为无闪烁静态NaN标记/标题。
- 动画/光敏/低画质/最终Player实测。

以上清单不作已完成宣称，不为了补视觉编造学习记录或修改产品规则。

## 冻结时 SHA256

- XgBrainWindow.cs：01871D143EC9A9BBB17FB385B6BA5501DD6F688C5D3AD5208AEEE5E7B275E39A
- XgBrainWindowFeedback.cs：D46E6DA680DF54203F26D4A3EE8257D0139FA748A9B47777460A1472A5F25BA3
- BrainWindowFeedbackChecks.cs：C31E2C75215DD0A747D282C468F03C13CEC977DAC0D9285233CBB1C94B5B5406
- BrainWindowFeedbackChecks.cs.meta：B072D8789DBE1457F8BF0B838726EDB7A98EC79DA648604942A1269B59EFA75D
