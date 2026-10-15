# motion-004：禁用按钮时的复位与副作用防护

日期：2026-10-13（Asia/Singapore）。交付状态：只读静态核对完成；无需生产补丁。运行与视觉验收尚未进行。

## 结论与范围

在当前 `XgUi.Button` / `XgButtonPressFeedback` 路径中，未发现“按下前、动画中、业务回调内禁用按钮”需要修正的已确认缺陷。现有保护已经覆盖这些路径，不生成 `changes.patch`，不调整普通可点击反馈或旧业务动效。

仅核对委派范围及已有 `Documentation/DesignPass/Agents/motion-task-001.md` 记录；没有读取新策划正文。未改动 Assets、ProjectSettings、全局清单或其他任务目录；未启动 Unity/游戏、提交、推送、打包或访问玩家存档。没有创建聊天、子 agent 或发送跨聊天消息。

## 原文件证据

以下路径均相对工程根目录 `C:/Users/xomem/Documents/ChatGPT/games/competition-20261011`。

| 禁用时机 | 当前代码与结论 |
| --- | --- |
| 按下前 `interactable=false` | `XgButtonPressFeedback.cs:33` 在创建私有动效组件之前检查 `IsActive()` 与 `IsInteractable()`；因此不会登记新文字 Knock。继承的 uGUI `Button.Press()`（包文件第 65–72 行）在调用业务事件之前做同样检查，鼠标点击与 Submit 均受保护。 |
| 按下前组件禁用或对象隐藏 | 同一 `IsActive()` 检查阻止文字反馈与业务事件；已有反馈由 `XgButtonPressFeedback.OnDisable()`（64–67 行）清理。真实 EventSystem 事件投递也应在统一运行验证中确认。 |
| 动画中 `interactable=false` | uGUI `Selectable.interactable`（361–372 行）调用 `OnSetProperty()`；该方法（536–543 行）转换到当前状态。`currentSelectionState`（610 行起）优先返回 Disabled；子类 `DoStateTransition()`（58–62 行）立即执行 `ResetPress()`，不需要等待动画自然结束。 |
| 动画中父 CanvasGroup 禁用交互 | `Selectable.OnCanvasGroupChanged()`（429–437 行）更新组交互缓存并调用 `OnSetProperty()`，进入同一 Disabled 清理路径。`LateUpdate()`（49–55 行）的 `!IsInteractable()` 也是兜底。以 `ignoreParentGroups` 为边界的实际父组语义仍沿用 uGUI。 |
| 动画中组件禁用／对象隐藏 | `OnDisable()` 先复位自己的文字效果再调用基类；重复清理为空操作，不重新登记效果。 |
| 鼠标业务回调内禁用 | 文字反馈已经在 PointerDown 阶段开始；继承的 `Button.OnPointerClick()` 只调用 `Press()`。回调触发上述禁用清理后，子类没有回调结束后重新启动反馈的代码。后续仍处于禁用状态的输入被 `Press()` 拒绝。 |
| Submit 业务回调内禁用 | 子类 `OnSubmit()`（24–28 行）先反馈、后调用基类。基类 `Button.OnSubmit()`（149–160 行）在 `Press()` 返回后再次检查活跃与交互状态，禁用则不执行 Pressed 转换、不启动 `OnFinishSubmit()` 协程；不会在清理后补播文字动效。 |
| 保留旧业务动效 | `ResetPress()`（70–74 行）只清理 `pressFx`，不会调用 `hostFx.Clear()`。`XgJuice.Knock()`（102–106 行）保存初始位置与缩放；`Clear()`（202–210 行）调用自身 `OnDisable()`；后者（310–315 行）恢复保存的变换并清空 punches。取消文字反馈不会清除宿主按钮本体 Knock。 |

`Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs:192,198,210` 确认按钮创建、原业务监听器和 Label 接入；第 111 行的 `XgBtn.Set()` 使用标准 `button.interactable` setter，未绕开上述清理。

接口边界：禁用阻止新的用户输入，不能撤销已经执行的业务回调；正在执行的 UnityEvent 监听链也不会因此自动截断。直接 `button.onClick.Invoke()` 是既有程序调用接口，不在本次输入防护承诺中。`XgHold` 的独立行为已被任务 001 明确列为范围外，本任务不扩展修改它。

## 避免重复劳动与待验证项

既有 `Assets/LocalBuild/Editor/MotionButtonChecks.cs` 已包含：按下前禁用（49 行）、父组禁用（54 行）、模拟变形后禁用并恢复非默认位置和缩放（73–77 行）、保留宿主业务效果（78 行）、Submit 回调禁用（80–84 行）。这些是已有验证代码，**本聊天未运行**，不把源码中的成功日志当成测试结果。记录中提到的旧静态编译成功也不作为本聊天的编译结果。

统一运行时建议在已有隔离验证入口基础上覆盖以下剩余边界，无需重复实现生产逻辑：

| 场景 | 可观察的验收条件 |
| --- | --- |
| 分别在按下前禁用交互、组件、对象、父组，投递 Down / Up / Click / Submit | 业务计数保持 0；没有新增文字效果；非默认位置、缩放不变。父组用 `interactable=false`，不要用 `blocksRaycasts=false` 代替键盘交互禁用。 |
| 真实动画推进至少一帧且实际观察到变形，再分别禁用以上四种状态 | 回到动画前非默认变换，私有文字效果停用且清空；宿主仍有效的业务 Knock 未被文字清理删除。组件禁用与整对象隐藏应分别测试。 |
| PointerDown 后、PointerUp/Click 前禁用 | 清理已开始的文字效果；之后的 Click 与 Submit 不能增加业务计数。 |
| 分别在鼠标 Click 和 Submit 回调内禁用交互／组件／对象／父组 | 当前业务回调恰好发生一次；回调结束即处于复位状态；继续保持禁用并再投递输入，计数不增加。Submit 没有新按压协程导致残留或重新变形。 |
| 禁用后重新启用并完成一次正常操作 | 无旧效果重播、无重复业务调用；新的按压以原始变换为基准；原有业务动效继续工作。 |

真实动画帧、完整 EventSystem 投递及深浅界面视觉效果由主聊天统一运行与截图验收。本交付只确认静态调用路径，不声称全局“三层反馈”原则或整份策划完成。

## 依赖快照与交付末尾复核

SHA256（用于本次只读结论的版本定位；没有待应用补丁）：

| 文件 | SHA256 |
| --- | --- |
| `Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs` | `1F284BD333A1049AA0A8463BF9B3A3913D6C359AF0FEAA22794A66BCFAAABE78` |
| `Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs` | `A81D4AA3591C28EFA023FB2CAB4DCC976CC581A1B132CFE910FD181CEDC50F14` |
| `Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs` | `2ABCA8BD6A93D03CF679A786F1A8B6EB2B50A2505A68CE2AA5F890844CF567EC` |
| `Assets/LocalBuild/Editor/MotionButtonChecks.cs` | `22E10F5BD0C8DC49168D401F727214E47B9782B7B26C6D801C48FB3D8D59530E` |
| `Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/UGUI/UI/Core/Button.cs` | `3AD850616D59ED6303135BA648A620A3730398EE9D398914612C581BECF7C69C` |
| `Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/UGUI/UI/Core/Selectable.cs` | `02EBB0484A16FE6FA0D34EA1CA5248B845503214D3D6E7318E055D54B8537596` |
| `Documentation/DesignPass/Agents/motion-task-001.md` | `755DBC9C34002157AE71792FA8484A02F0776311C36CF586518DC97D9C99B9EE` |

待整合：仅采纳本次无需修改结论和剩余验证矩阵；没有 C# 文件或补丁需要合并。生产依赖后续若变化，应重新核对相关调用路径。

交付末尾复核：上述七个依赖文件 SHA256 均与快照一致，未发现本次核对期间发生变更。该复核是文件一致性检查，不是 Unity 编译、运行或视觉验收。
