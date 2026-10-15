# 动效当前单项：18 份交付整合记录

日期：2026-10-13（Asia/Singapore）。负责人：动效聊天。范围仍是《动效策划.md》第 10 行的两层反馈原则，阅读止于第 16 行；不读下一条，不标记全局原则完成。

18/18 个 `result.md` 已到达，无尚未到达项。当前批已实际修改生产文件并接入统一同步检查入口；未启动 Unity、游戏，未提交、推送、打包，未访问真实玩家存档。主聊天追加授权后，本轮才修改 `XgJuice.cs`、训练／购买页面；引导组、科技组、UiTip、壁纸和全局进度均未修改。

## 逐项决定

| 任务 | 决定 | 落地或理由 |
| --- | --- | --- |
| motion-001 鼠标按键 | 无需变更基础 Button 规则 | 保留 Left 判断、原 OnPointerClick 和业务次数。同步检查用普通 Button 对照；训练独立 Hold 的右键释放遗漏另随 015 修正。 |
| motion-002 Submit | 无需改通用提交路径 | 保留反馈先登记、仅一次 base.OnSubmit。普通成功后的完成时机随 010 修正；实际键盘／手柄映射仍待实景。 |
| motion-003 指针取消 | 已采纳 | 所属指针 Exit、父拖动接管的 Up、Cancel 复位文字。不同手指不会取消最新文字反馈；不实现 IDragHandler，不消费 Cancel，不改点击资格。同步检查已装入 Assets。 |
| motion-004 禁用 | 无需额外通用补丁 | 原 IsActive/IsInteractable、Disabled、OnDisable 路径保留；不把通用禁用检查冒充训练专用回调检查。 |
| motion-005 父级交互 | 驳回候选生产改动，保留测试场景 | 候选记录并永久取消经历过 CanvasGroup 禁用的手势，明确改变基础 Button 在松开前恢复可用后的原生行为；当前合同要求保持 Button 点击语义，没有单独要求重定义这一手势。不引入手势 List／自定义 OnPointerClick。当前始终不可交互时仍拒绝输入。 |
| motion-006 生命周期 | 无需新增生命周期代码 | 原隐藏、销毁、重开复位仍适用；真实窗口可见性轮询和延迟销毁留统一场景验证。 |
| motion-007 连点 | 无需额外生产代码 | Knock 保留初始基准和组件复用；布局更新被 009 显式处理。未加业务去抖。 |
| motion-008 长按移出 | 已采纳并合并 003 | 带启用 XgHold 的按钮，持有指针移出时仅调用 base.OnPointerUp 复位背景，不派发第二次业务 Up。物理持有指针与文字脉冲拥有者分开记录，文字自然结束后移出仍能复位背景。同步检查中“普通按钮移出文字仍播放”的旧断言与 003 冲突，已改为仅保持原背景状态、文字取消。 |
| motion-009 布局基准 | 已采纳 | 私有 Label 栈开启 TrackKnockLayoutChanges；按轴采纳与上次自身输出不同的绝对布局写入，应用于重触发、更新、取消。业务栈默认关闭。新增 SetKnockRestTransform 处理新基准恰等于旧动画值的歧义；调用者须提供完整静止值。非默认、零／负缩放、单轴和实际 LayoutGroup 验证入口已接入。 |
| motion-010 完成时钟 | 已采纳查询接口方案 | 用 HasActiveKnock 观察 XgJuice 真正完成，删除按钮重复 elapsed/duration 计时，避免 Update 之后输入导致 LateUpdate 提前清理。未改变 capped unscaled delta。 |
| motion-011 运行中减少动效 | 已采纳平滑收回方案 | 新增 SettleKnock，从当前姿态单调回到静止，最长 .06 动画秒且不延长原剩余寿命。反复开关不重播，关闭设置不放大旧动作。与 009 合并时补上收回分支 RememberApplied，并处理收回途中布局轴的更新。 |
| motion-012 业务隔离 | 无需改变所有权 | 通用栈仍只拥有 Label，宿主拥有按钮本体和业务粒子；Clear 不跨实例。009 的布局追踪默认关闭，不改变业务原 Knock 基准语义。 |
| motion-013 暗色可读性 | 采纳验证交付，无生产配色修改 | 未证明配色／字形缺陷，不凭名义对比值改主题。真实帧采集脚本保留专属交付目录，供主聊天的隔离验证副本导入，见下方入口。 |
| motion-014 浅色可读性 | 采纳验证交付，无生产配色修改 | 保留现有皮肤、颜色与边框；采集脚本只在隔离验证场景导入，不据静态颜色计算宣称视觉通过。 |
| motion-015 训练两层反馈 | 采纳现有层次结论，并补真实输入遗漏 | 现有训练本体 Knock／火花＋进度／结果已接线，不重复加效果。本轮复核确认独立 XgHold.Down 与 XgTrainingSubmit 绕过 Button 的可交互检查：前者补 Selectable 门控、重复按下和不同按钮／触点释放过滤，后者补 active/interactable 门控。保持训练业务与既有其他会话修改。未扩大为教学页或评估卡全输入审计。 |
| motion-016 购买两层反馈 | 已采纳 | XgItemsPage.Buy 的非 Buyable 预检查保留拒绝 Knock/Thud，追加真实 Sim.Why 的 ShowToast，再刷新状态。该分支不购买、不扣款、不虚构成功；成功路径不变。 |
| motion-017 旧效果复用 | 无需追加生产动效 | 原有训练／购买强反馈继续复用，不把多个装饰算成多个反馈层，不给所有按下追加成功粒子。壁纸依赖已有其他会话新修改，相关世界表现不沿用旧静态结论验收，由主聊天负责。 |
| motion-018 开销 | 已采纳最小缓存补丁 | XgJuice 活动 Punch 的完成列表惰性创建并复用，处理完成后 Clear 引用；不宣称零分配或 FPS 改善。测量入口已接入，但尚未运行。 |

## 补丁前核对与冲突处理

首次实际核对的生产 SHA256 与候选基线相同：

| 文件 | 整合前 SHA256 |
| --- | --- |
| XgButtonPressFeedback.cs | `1F284BD333A1049AA0A8463BF9B3A3913D6C359AF0FEAA22794A66BCFAAABE78` |
| XgUi.cs | `A81D4AA3591C28EFA023FB2CAB4DCC976CC581A1B132CFE910FD181CEDC50F14` |
| XgJuice.cs | `2ABCA8BD6A93D03CF679A786F1A8B6EB2B50A2505A68CE2AA5F890844CF567EC` |
| MotionButtonChecks.cs | `22E10F5BD0C8DC49168D401F727214E47B9782B7B26C6D801C48FB3D8D59530E` |
| XgItemsPage.cs | `8361F777B746D8E0359EF9BB02E2CEFC48E9B0AF0023EF85819DA59E5662239A` |
| XgTrainPage.cs（局部补 guard 前） | `1AFC0A20C289705A600CA0A73A5D5A9BDF402FCFFC5D395DB7FB19141B96895E` |

没有顺序机械应用原 patch，也没有复制整份生产候选覆盖共享工程。003/008 合并 OnPointerExit；009/011/018 合并 Punch 字段、Update、Clear 与缓存；010 删除了 011 原候选依赖的第二计时器。减少动效检查已删除对 elapsed 私有字段的反射；003 改为可空 pointerId 参数后，也适配了 ReducedSwitchChecks 的 PressLabel 反射参数。

布局追踪启用后，手工设置“中途姿态”属于外部布局写入，旧检查会错误地要求它被丢弃。因此基础、003、008 检查改为推进实际 XgJuice writer 到指定阶段，再执行取消／重触发。该同步方法仍不代表真实帧推进。

`motion-source-recheck.json` 记录交付中的 JSON 指纹与当前文件复核。核心源码因本轮整合产生的差异属预期，不能再直接应用旧候选。额外发现 motion-016 的 XgTechTreePage、motion-017 的 XgWallpaperConcepts 已被其负责人更新；本轮不覆盖它们。购买补丁只依赖未变的本地预检查和现有 ShowToast/Refresh/Why 合同，已逐处核对；壁纸旧效果结论不作为当前版本验收依据。

## 统一运行入口与检查状态

- 已做：当前源码及补丁哈希核对、逐处语义合并、空白检查；独立 Roslyn 编译核心 `XgUi` / `XgButtonPressFeedback` / `XgJuice` 和已安装同步检查、分配探针，退出码 0。使用现有程序集补充依赖；这不是 Unity 全量编译，也没有执行检查。训练／购买页面完整集成编译交主聊天。
- 主入口：隔离场景无 EventSystem 时运行菜单 `Tools > Design Pass > Check Integrated Motion Foundation`，或 `MotionIntegrationChecks.Run()`。依次调用基础检查、003、008、LabelBaselineChecks、ReducedSwitchChecks，再检查训练输入门控、实际 Punch 完成后才清理、收回过程中单轴布局更新、缓存复用和销毁目标。会创建并销毁临时对象，不创建控制器或访问存档。
- 分配测量：`MotionButtonPerfProbe.Run()`，仅隔离编辑模式；数据不等于真实帧耗。共享栈多目标、目标销毁、首次使用和重复使用仍需按 motion-018/measurement.md 做测量。
- 暗色实际帧：隔离验证副本导入 `motion-013/Motion013DarkButtonFrames.cs` 到非 Editor 的验证目录；脚本本身 UNITY_EDITOR 限定。使用真实字体、空 Play 场景及其菜单入口，按原 result.md 条件采集。
- 浅色实际帧：隔离验证副本导入 `motion-014/Motion014LightFrameProbe.cs`，仅含探针根对象的场景，赋真实字体后调用其 ContextMenu。此脚本不是生产运行组件，不复制进正式场景。
- 图像采集完成标记仅说明捕获结束，必须人工看字形、边界、回弹与旧强动效；真实页面与真实鼠标／键盘路由仍需单独验收。

## 未覆盖／未完成

全部交付已处理，无待到达项；完整整合检查、实际帧、购买／训练业务结果和截图待主聊天统一验收。尚无任一真实场景 PASS 证据。

普通 Button 的“按下期间禁用又恢复”继续遵循 uGUI；若产品以后明确要求旧手势永久失效，应另定语义并重新合并 motion-005，不能静默改变。程序直接 `onClick.Invoke()` 不受输入门控。手写按钮、滑块、拖拽业务、教学、科技和壁纸其他操作不在本轮全面覆盖内。

布局跟踪只识别外部绝对静止写入，不能推断数值相同的写入意图或第二个动画系统；这些需显式布局接口或独立视觉层。连续布局覆盖动画的每帧视觉表现仍待现场检查。训练评估卡遮挡／选中控件与父层输入路由、真实手柄映射也未通过本轮同步测试证明。

原文是全局原则，当前只补通用按钮、确认训练已有双层接线并补购买失败结果；不能据此勾选“每个操作至少占两层”或整份文档完成。停在当前条目。

## 主聊天实编译／实测反馈与本轮修复

- 主聊天第一次 Unity 编译在 MotionIntegrationChecks 发现 CS1061：PointerEventData 没有 eventSystem 属性（`26-lesson-wallpaper-scene.log`）。已改成夹具创建的 `events.GetComponent<EventSystem>()`；当前源码不存在该错误访问。
- 主聊天随后报告 `MotionButtonChecks` 通过，`MotionIntegrationChecks` 在 motion-003 的 EditMode 空场景夹具失败：StandaloneInputModule.ProcessTouchPress → DeselectIfSelectionChanged 空引用（`27-parallel-batch.log` 第 495 行后）。这不是场景验收通过，失败未被跳过。
- 原因：EditMode 中创建 UIBehaviour 后不保证其 OnEnable 生命周期已运行；BaseInputModule 的 m_EventSystem 正是在 OnEnable 中初始化。已在 fixture 派发任何事件之前显式调用安装包 BaseInputModule.OnEnable，并断言其绑定的是该夹具 EventSystem，然后停用模块的设备轮询。没有手写点击资格或直接篡改私有字段，不要求启动 Play Mode。
- 修复后再次独立 Roslyn 编译核心 3 个生产文件及 7 个已安装检查／探针文件；退出码 0。此结果只证明静态编译，未代替 Unity 全量导入或修复后的运行。
- 当前可交付主聊天重新编译并重跑 `MotionIntegrationChecks.Run()`；编辑模式初始化修复的实际结果仍待确认。继续保留完整失败历史，不把旧基础检查通过扩大为整合入口或真实场景通过。
