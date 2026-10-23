# 五模块局部封印完整暂存补丁

交付 training-seals-complete.patch，替代旧 foundation.patch；二者不要重复应用。只写 staging，未改生产、未编译、未运行 Unity。当前只是可审阅候选，不能称为上游戏或验收通过。

## 改动

- XgTechTreePage.cs：独立 FocusNodeDetails，原 FocusNode 语义不变；定位真实节点后打开详情。
- XgTrainingSeal.cs：模块内三行名称/一句话/真实价格，半透明深玻璃、横纹、共享原创青紫贴图十四秒旋转；局部裁剪，无实时模糊。隐藏停止 Update，减少特效/低画质冻结。点击只跳真实节点说明，不扣钱或解锁。
- XgTrainPage.cs：Build 末尾接入；XgTrainPage.Parts.cs：原刷新末尾布局/状态同步。
- XgTrainPage.Seals.cs：五处模块本地槽位。回档在模型头部右侧；留出验证集在成绩下、答卷上；权重在曲线下方原诊断块；学习率在训练按钮上方；监控在进度条下方。不是集中封印栏。
- TrainingSealChecks.cs：组件门槛/价格/合成点击/新旧档检查，及供主会话隔离 PlayMode 使用的 AssertDetails(view)、AssertLayout(view)。检查器只审视调用者当前 fixture，不自行开真实玩家档。

## 布局策略

原模块仅 28–44px 高，三行封印预留至少 84px。主栏上部改为有滚动条的裁剪视口，训练按钮与进度始终固定底部；滚动内容保留可读高度，不挤小答卷或把封印盖到基础曲线上。答卷原有独立滚动保持。Viewport 尺寸变化回调和页面 Refresh 调整布局，无 Update 全场景查找或布局计算。

未买全时：底部控制 226px（学习率 84 + 间隔 6 + 进度/监控 136）；回档头 84；成绩 24；验证封印 84；答卷 180；曲线有效至少 90；诊断封印 87。上部内容约 626px，可滚动。已买全时控制恢复 162px，回档头 34；旧档真实门槛保留且全部原可见项不收费重锁。最小建议训练页面高度 376px（不含 .exe 自身顶栏）。更小窗口需整窗最小尺寸约束，当前补丁没有修改窗口规则。

五个映射只用现有 TrainPart / NodeCost：holdout→train.holdout、rate→shared.lr、checkpoint→train.checkpoint、monitor→train.monitor、weights→train.weights。学习率保持真实 ¥60；六挡风险收益公式完全不改。中英说明成对。封印闭包读当前 view.Sim，换档后原 Refresh 可切换状态。实际内容是封印的兄弟层，购买不会把内容一起隐藏。

## 已做静态核对

核对真实门槛、节点、价格调用、公开 view.Juice / Train 入口；各付费基本区域与封印矩形分离；完整补丁 git apply --check 通过，无生产写入。完整补丁包含 3 个已有文件增量 + 3 个新文件，不含 Side、Books、MiniGames，保留主会话最新缺字符号修复。

## 尚未验证，主会话串行执行

1. 应用后 Unity 编译；TrainingSealChecks.Run。
2. 隔离的新档（曲线已买、五项未买）、混合购买档、全购买档、legacy 档；Refresh 后渲染至少两帧，分别 AssertLayout。真实购买一次确认只原购买流程扣款，返回练页内容立即显示；再换回未买档。
3. AssertDetails + 五个封印分别真实鼠标点击，确认目标说明而非仅光圈/跳到前沿；对应节点若折叠也应可定位。
4. 中英默认窗、缩小窗、最大窗截图；检查头部回档/模型/双轨不重叠、滚动条/内层答卷滚动、基础训练按钮可点击、曲线刻度与诊断不拥挤。特别注意实际 .exe 内容宽度低于 500 时头部双轨和 240px 封印可能冲突。
5. 隐藏窗口/减少特效/低画质两帧角度不变；恢复后缓慢旋转。当前透明度/视觉效果未截图验收。

此批不新增记账本收费、模型/能力等科技门槛，不替代后续授权的独立功能批次。