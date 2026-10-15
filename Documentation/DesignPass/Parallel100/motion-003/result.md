# motion-003：指针退出、拖拽接管、取消与多指反馈

日期：2026-10-13（Asia/Singapore）。结论：发现即时复位缺口，交付单文件候选补丁；未修改生产源码。静态编译成功，运行检查、硬件输入与视觉验收均未执行。

## 已确认的问题与现有正确行为

已读取 `motion-task-001.md`、当前 `XgButtonPressFeedback`、`XgUi`、`MotionButtonChecks` 及相关 `XgJuice`、安装包输入处理源码。未读取任何新增策划正文，也未将全局「至少两层反馈」标记完成。

- 原 `XgButtonPressFeedback.cs:18–22` 在左键／触点按下启动 Label 效果，但没有退出、拖拽接管或 Cancel 的复位入口。原文件第 49–56 行仅在时长到达、交互禁用或宿主不可见时结束效果。因此按下后立即移出、被父滚动区域接管或取消时，反馈会继续到原有定时清理（最长配置时长 0.18 秒，非永久残留）。
- 现有 `ResetPress` 调用私有 `XgJuice.Clear`；`XgJuice.cs:310–315` 恢复保存的位置与缩放。此实现正确，继续复用，保留宿主业务 Knock 和原有幅度、时长、减少动效参数。
- `MotionButtonChecks` 已覆盖正常点击、禁用和业务动效隔离等基础边界，缺少本任务的退出、拖拽、取消和多指顺序检查。保留原验证文件，提供独立补充入口。
- 普通 Button 的业务点击条件无需修改。`Button.cs:67–74, 103–109` 只在左键点击且 Active／Interactable 时执行业务；点击事件是否产生由输入模块决定。不得在退出时自行永久取消点击：移出后返回再松开、且未被拖拽接管时，普通 Button 仍允许点击。

## 候选修正

目标唯一生产文件：`Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs`。

基准原文件 SHA256：`1F284BD333A1049AA0A8463BF9B3A3913D6C359AF0FEAA22794A66BCFAAABE78`。

`changes.patch` 含目标路径、基准哈希与上下文；`XgButtonPressFeedback.candidate.cs` 是同一改动的完整审阅副本，不应作为第二个同名类型一起导入。其他依赖哈希见 `source-sha256.json`。

1. 成功启动的指针反馈记录 `pointerId`；无指针的 Submit 使用可空值。0 和负数均可作为真实指针编号，不借用数字哨兵值。最新成功启动的按压拥有当前共享 Label 效果。
2. `OnPointerExit` 仍先调用基类，仅当退出的是反馈拥有者时复位。旧手指退出不会清掉第二根手指已启动的效果。退出后返回不重播，也不修改点击资格。
3. `OnPointerUp` 保留基类行为，仅在同一拥有者处于拖拽且 `pointerPress != pointerDrag` 时复位。`PointerInputModule.cs:369–377` 和 `InputSystemUIInputModule.cs:773–782` 均先发送 Up，再清除点击资格；故不能只在 Up 中判断 `eligibleForClick`。不实现 `IDragHandler`，父 ScrollRect 继续获得拖拽。
4. 实现 `ICancelHandler`，普通导航 Cancel 只清理反馈、不调用 `Use()`、不改业务资格；带 PointerEventData 的显式取消只清理对应拥有者。普通 Button 的导航 Cancel 本来就不会使仍按住的指针失去点击资格，本补丁保持这一行为。
5. 普通松开保留当前回弹，不重播动效、不重写 OnPointerClick、不过滤第二根手指的合法点击。普通 Button 中两根手指各完成一次有效点击，可产生两次业务回调；本任务不引入单指互斥或去重规则。

实际系统触控取消与导航 Cancel 不同。当前 uGUI `PointerInputModule.cs:78, 92–95` 对 TouchPhase.Canceled 生成 released 且清空命中；`StandaloneInputModule.cs:426–460` 因而不发送点击，并最终发送退出事件。该原有点击保护正确；补丁通过退出立即恢复 Label。未声称所有平台或自定义输入模块都遵循该触控取消映射。

## 交付与检查状态

- `prepare_patch.py`：校验固定原文件哈希，生成目录内候选和补丁，不写生产文件。源文件变化会拒绝生成。
- `Motion003PointerChecks.cs`：待整合的独立检查，使用安装包真实 `ProcessTouchPress`／`ProcessMove`／`ProcessDrag`，不复写业务点击条件；在普通 Button 与反馈按钮之间比较七种序列。使用父 ScrollRect 验证拖拽处理器归属，并断言任意原始 Label 位置／缩放复位、旧触点不清除新触点、0 编号触点、右键干扰、Cancel 不消费事件、宿主业务效果保留。
- `compile-candidate.ps1`、`compile.log`：候选和补充检查使用 Unity 6000.4.5f1 的 Roslyn／Unity 引用及当前 `Library/ScriptAssemblies/Assembly-CSharp.dll`、`UnityEngine.UI.dll` 静态编译，最终退出码 0、无诊断。仅抑制预期的同名源类型优先警告 CS0436；不代表 Unity 全量导入或运行成功。最初引用配置曾重复引用 UnityEditor 门面，已移除后重编译。编译产物与响应文件均只写本目录。
- 补丁仅做 `git apply --check --ignore-space-change` 可应用性检查，未应用。末次源依赖哈希与补丁检查结果见 `final-recheck.txt`。
- 独立检查尚未运行；真实设备输入、实际动画帧推进和视觉验收均未完成。测试通过的日志只能在负责人实际执行后认定，不能从本静态编译结论推导。

## 待整合与待验证

整合负责人：`01a130ed-1766-7ee2-b60d-0d4d755c4e33`。未向其他聊天发送消息。

应用前再次比对目标源码及 `source-sha256.json`；若别的动效任务修改了 PressLabel、OnSubmit、ResetPress 或生命周期，需合并方法内容，不覆盖整份生产文件。无外部缺失接口，新增状态仅属此按钮私有反馈。

由负责人将补充检查放入选定 Editor 验证目录；在无现有 EventSystem、未锁定光标的隔离场景执行 `Tools > Design Pass > Check Motion 003 Pointer Boundaries`，并重跑原 `MotionButtonChecks`。对象只在内存建立并销毁，不创建业务控制器或接触玩家存档。补充检查直接控制 PointerEventData，覆盖输入模块分派与同步复位，不证明硬件事件采样或视觉帧正确。

实际验收应包括：快速移出后松开（0 点击）；移出返回后松开且未拖拽（1 点击）；父 ScrollRect 阈值触发且仍位于按钮内部（0 点击、Label 复位）；真实 OS 触控取消（无业务、Label 复位）；两指相继按下、旧指退出／拖动、新指保留反馈；两次有效触点释放与普通 Button 同次数；先触控后 Submit 时旧触点退出不清掉 Submit 反馈。最后检查正常鼠标松开回弹、触控点击后退出复位的观感，以及原帅气业务动效未被清理。

本任务到此交付，未启动 Unity／游戏、未提交／推送／打包，未修改 Assets、ProjectSettings、全局清单或其他任务目录。
