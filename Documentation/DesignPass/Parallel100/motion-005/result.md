# motion-005 — CanvasGroup 继承状态变化

日期：2026-10-13（Asia/Singapore）。交付状态：确认一项输入边界问题，提供候选补丁；未写入生产目录，未完成运行或视觉验收。

## 结论

现有实现正确沿用 uGUI 的 `IsInteractable()`，不需要另写一套祖先 CanvasGroup 遍历。父级变为不可交互时，`DoStateTransition(Disabled)` 会立即清理文字效果；`LateUpdate` 还有兜底。`ignoreParentGroups` 的隔离有效时，上层禁用不应取消按钮，候选保持这一行为。

确认的问题是**被 CanvasGroup 禁用中断的按住手势，会在松开前恢复可交互后重新触发业务**。这是当前继承的 Button 行为，不能因文字已复位就认定点击已取消。其背景 Pressed 状态也可能在恢复交互后重新出现。

静态可达路径：左键 Down → 祖先 `interactable=false`（或中间层 `ignoreParentGroups=false` 暴露被禁用的祖先）→ 文字复位，但 Selectable 的 `isPointerDown` 和输入模块的点击资格没有一起取消 → 恢复可交互 → Up/Click → `Button.Press()` 仅检查当前可交互性，执行旧点击。另一个边界是按钮不可交互时开始按下，恢复后才松开。

## 源码依据与原有覆盖

- `Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs:18`：Down 交给基类后尝试播放；没有点击手势取消记录。`:33`、`:53`、`:58`、`:70`：当前交互性检查及私有动效复位已经存在。
- `Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs:188`：安装该 Button 子类并保持原业务 onClick；候选不改 UI 工厂或原业务动效。
- `Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs:202`、`:314`：Clear/OnDisable 恢复 Knock 保存的静止位置和缩放。
- `Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/UGUI/UI/Core/Selectable.cs:429`、`:440`、`:486`：有效 CanvasGroup 继承状态及 ignoreParentGroups 由基类维护；`:528`：换父级重新计算；`:610`：恢复可交互后仍可能读取旧 isPointerDown；`:1201`、`:1248`：该私有按住标记在 Down/Up 中改变，CanvasGroup 变化不清除它。
- 同包 `Button.cs:65`、`:110`：Press/Click 只检查点击发生时的 active/interactable，不追踪之前是否被中断。
- 同包 `StandaloneInputModule.cs:207`、`:426`：Up 和 Click 同步派发，依据目标和 eligibleForClick 判断。
- `Library/PackageCache/com.unity.inputsystem@21a28c3a6c83/InputSystem/Plugins/UI/InputSystemUIInputModule.cs:476`、`:707`、`:725`：新输入系统复制每键状态，并在 Up 前算好 isClick。因此只改共享 PointerEventData 的 eligibleForClick 不能作为稳妥修正，候选在按钮 Click 层阻止已取消手势。
- 已读 `Documentation/DesignPass/Agents/motion-task-001.md` 与 `Assets/LocalBuild/Editor/MotionButtonChecks.cs`。现有入口只覆盖父 CanvasGroup 已禁用时不创建反馈，未覆盖 Down 后禁用再恢复、ignoreParentGroups 动态变化或旧手势 Click。

以上为静态源码判断，未声称在 Unity 中复现。没有读取新的策划正文或其他聊天的候选交付。

## 交付与整合合同

- `changes.patch`：唯一目标为 `Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs`，有统一差异上下文。
- 应用前原文件 SHA256 必须为 `1F284BD333A1049AA0A8463BF9B3A3913D6C359AF0FEAA22794A66BCFAAABE78`；否则重新阅读并合并，不能覆盖其他输入补丁。
- `XgButtonPressFeedback.candidate.cs`：同一补丁的完整候选源码，供比较；只采纳一次，不能把它与原类同时导入。
- 候选按 pointerId 保存当前手势；基类判定有效不可交互时标记取消，通过基类 PointerUp 清除背景按住标记；恢复继承状态不撤销取消。新 Down 创建新手势，Submit 保持现有路径。多指独立取消，某一新按下不会复活另一旧指针。
- 跟踪在首次左键使用时建立；Up 后保留取消状态至同帧 Click，LateUpdate 回收已释放的记录，包含目标外释放；组件禁用时清空容器。此时序以工程现有两个输入模块的同步 Up/Click 为合同。未来若引入跨帧延迟 Click 的自定义模块，须另定手势标识合同。
- 保留现有 Knock 幅度、减弱动效、文字复位、宿主业务效果与颜色配置。只借用基类判断继承边界，不改变 `ignoreParentGroups` / `CanvasGroup.enabled` 的包版本语义。
- `Motion005CanvasGroupChecks.cs`：供整合负责人放入隔离 Editor 验证程序集后运行 `Motion005CanvasGroupChecks.Run()`。覆盖禁用/恢复、隔离撤销/恢复、隔离内层自身禁用、CanvasGroup enabled、禁用时起手、禁用时 Submit、多指、换父级和无 Click 的释放回收。人为改变文字变换只验证复位，不模拟真实动画。
- `build_delivery.py` 只在本目录重建候选与补丁，先验证原源码散列；`compile_candidate.ps1` 只做独立静态编译，输出也在本目录。`dependency-sha256.json` 记录已读取依赖。

CanvasGroup.alpha 与 blocksRaycasts 不等同于 interactable；本候选不把透明度或射线规则擅自转换成禁用规则。直接 `onClick.Invoke()` 是程序业务调用。组件关闭重开等其他生命周期策略仍由对应任务整合，本项不扩大为所有输入取消规则。

## 已执行与未执行

- 已执行：源码审查；`git apply --check` 检查补丁上下文（退出码 0，未应用）；以本机 Unity 6000.4.5f1 引用和现有 ScriptAssemblies 对候选与独立验证代码做 Roslyn 静态编译（最终退出码 0，见 `static-compile.log`）。同名源码优先引用的 CS0436 单独抑制。初次编译引用了重复的 Editor 聚合程序集，调整为引擎模块引用后成功；此过程没有启动 Unity。
- 未执行：Unity 导入/全量编译、验证入口、真实鼠标/触摸/键盘操作、真实帧动画、场景截图、视觉验收。没有任何运行测试 PASS 结论。
- 结束前复核：生产按钮、XgUi、XgJuice、原验证入口、原任务记录的 SHA256 与首次读取一致；补丁仍能通过只读应用检查。依赖散列清单也做了最终复核。

## 待整合与待验证

整合负责人需先与其他手势/生命周期候选合并，再统一编译。独立入口应在隔离验证环境实际运行；未打补丁的当前代码预期会在“恢复后按住标记已清除/旧点击被阻止”断言处失败，这个预期尚未运行确认。

实际场景须分别检查普通祖先禁用、ignoreParentGroups 隔离有效和隔离撤销三种情况：按住中切换，松开旧手势无业务，新手势只执行一次；检查背景与文字无残余压缩/偏移，原业务强动效保持。至少跨一帧、同帧禁用恢复各测一次，分别覆盖项目实际使用的输入模块；键盘在禁用时 Submit 不触发，恢复后新 Submit 可正常触发。动画自然结束后的长按再切换继承状态也须验证，避免把动效结束误当作手势结束。

本任务只交付 motion-005，不标记原文全局原则或任何整份策划完成；未启动 Unity/游戏、未提交/推送/打包、未接触真实玩家存档、未向其他聊天发消息。
