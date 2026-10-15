# motion-008：长按、移出松开与移回的视觉节奏

日期：2026-10-13（Asia/Singapore）。状态：只读核对完成；交付一个候选补丁，尚未整合、编译或在 Unity 中运行。未做视觉验收。

## 结论

确认一个局部问题：带启用中 `XgHold` 的按钮，左键按下后移出，业务按住状态已经结束，但背景仍保持 Pressed；保持鼠标按下再移回时仍显示 Pressed，并不重新进入 Held。建议仅在此类按钮的左键移出路径复位 Selectable 按下状态，同时取消尚未结束的私有文字回弹。

原有长按的一次文字回弹无需改动：按下只登记一次 Knock，普通模式持续 .18 秒、减少动效模式 .12 秒，随后恢复；持续按住没有循环，松开没有第二次文字冲击。不要为了“长按”追加计时触发、Down、Up、Press 或 onClick。

## 原文件证据与已有工作

- 已读取 `Documentation/DesignPass/Agents/motion-task-001.md`。该记录有意采用“一次按下回弹、不在松开重播”，并把 XgHold 独立业务与真实长按场景留待验证。保留这一基础，不重复实现。
- `Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs:18–55`：只有 PointerDown、Submit 启动 PressLabel；LateUpdate 按未缩放时间结束；没有 PointerExit 或 PointerEnter 覆盖。
- `Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs:119–127`：XgHold 在 Down 置 Held；Exit 置 false 并仅在之前 Held 时调用 Up；没有 Enter 处理，所以移回不会重新开始。Exit 后再 Up 不重复调用 Up。
- `Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/UGUI/UI/Core/Selectable.cs:610–623,1201–1248,1273–1305`：currentSelectionState 优先检查 isPointerDown；Exit 只把 isPointerInside 置 false；Up 才把 isPointerDown 置 false。因此旧背景在移出和移回时均可保持 Pressed，与已结束的 XgHold 不一致。这是源码状态路径结论，不冒充现场截图。
- `Assets/LingGuangV05/XingGuang/Desktop/XgTrainPage.cs:122–137`：训练按钮由 XgUi 建立，并安装 XgHold，Down 连接 Press；确有生产使用点。
- `Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs:102–107,292–315`：Knock 保存静止变换；Clear 恢复自身登记目标。标签私有效果与按钮本体强 Knock 分属不同效果栈。
- `Assets/LocalBuild/Editor/MotionButtonChecks.cs` 已有一般按钮检查；没有覆盖 XgHold 移出和移回状态，本交付只补该边界。

## 交付与修正范围

- `changes.patch`：唯一生产目标为 `Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs`，含上下文与原 SHA256：`1F284BD333A1049AA0A8463BF9B3A3913D6C359AF0FEAA22794A66BCFAAABE78`。
- `XgButtonPressFeedback.candidate.cs`：应用后的参考副本。整合时使用补丁或合并对应方法，不要同时导入此副本和原文件。
- `Motion008HoldChecks.cs`：可整合到临时 Editor 验证目录的独立同步检查，未导入、未编译、未运行。
- `source-hashes.json`：目标及七个只读依赖的 SHA256。
- `build_delivery.py`：只在本目录生成候选与补丁，不写生产文件；目标哈希不匹配时停止。

新增 OnPointerExit 先调用原 base 路径，再对左键、存在且启用的 XgHold 调用 `base.OnPointerUp(eventData)` 与 ResetPress。此处直接调用基类方法只复位 Selectable 内部状态，没有向 EventSystem 派发 Up，因此不会调用 XgHold.Up 或按钮 onClick。真正的 XgHold.Up 仍由原 Exit 处理器调用。不要改成 ExecuteEvents.pointerUpHandler，也不要检查 Held 后才取消：两个组件收到 Exit 的先后顺序不应改变结果。

移出后保持非按下底色；移回只恢复原 hover／selected 状态，不重放文字、不重新 Down。正常松开、Submit、普通按钮没有改变；字体压缩参数与已有宿主强 Knock、声音、飘字都保留。背景仍使用原 fadeDuration=.05 秒，标签移出时立即恢复静止变换，其观感由统一场景验收决定。

此候选仅处理左键与启用中的 XgHold。多指、多鼠标键并行，以及 XgHold 自身对其他按钮事件的过滤，不在本任务修复范围。检查使用当下生产接口，无缺失依赖。

## 检查状态

- 静态检查：核对实际 uGUI 源码及回调路径；候选只新增视觉取消，不新增业务调用。补丁适用性检查退出码和结束前依赖复核记录见 `verification.txt`。
- C# 编译：未运行，留给整合窗口统一编译。
- Unity 运行：未运行；同步检查和现有 MotionButtonChecks 均待运行。
- 视觉验收：未进行；没有声称任何截图或真实交互通过。
- 同步检查涵盖：两种 Exit 组件调用顺序、普通／减少动效、非默认文字原始变换恢复、移回不重放、不重新 Down、移出松开不重复 Up、再次按下可用、原有合法 click 只调用一次、宿主强 Knock 保留、无 XgHold 的普通按钮行为保留。取消前的位置变换是人工设置的中间姿态，不能证明自然动画完成。

## 待整合与待验证

1. 整合前重算 target 与相关依赖哈希；若负责人已修改同一文件，按接口语义合并 OnPointerExit，不盲目覆盖整份候选。若采用本补丁，统一编译后运行现有检查及候选同步检查。候选检查反射 currentSelectionState 和 punches，仅用于临时验证，不属于生产接口。
2. 隔离场景使用计数器替代真实训练回调。在按钮内左键按住至少 2 秒：文字自然回弹后停稳，背景维持按下；Down=1、Up=0，未追加 click；松开后 Up=1。分别检查 .18/.12 秒脉冲，保留按钮宿主强动效。
3. 分别在动画尚未结束和已结束时移出，再在外部松开：移出即 Held=false、Up=1，背景在原 .05 秒过渡后不再按下，标签准确回原变换；外部松开不追加 Up 或 click。捕获移出前、移出后过渡完成两张图。
4. 按下→移出→保持鼠标按下移回→内部松开：移回只有原 hover／selected 表现，Down=1、Up=1，文字不重播。若现有输入模块认定该手势仍为合法普通 click，可按其原逻辑触发一次 onClick；补丁不取消该资格，也不主动增加 click。带 XgHold 的训练按钮当前没有普通 onClick 回调。
5. 验证新一轮按下可再次 Down；重复出入不增加 Up；无 XgHold 的按钮行为保留。明暗界面、减少动效开关、实际鼠标事件路由、低帧率和 timeScale=0 均由主聊天场景验证。本候选没有用合成事件结果替代实际输入模块验收。

本聊天仅写本专属目录；未更改 Assets、ProjectSettings、全局清单或他人交付，未读取新策划正文、启动 Unity／游戏、接触玩家存档、提交／推送／打包、派发聊天或向其他聊天发消息。本任务交付不代表第 10 行全局原则或整份文档完成。
