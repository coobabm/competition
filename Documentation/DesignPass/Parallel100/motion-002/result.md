# motion-002：EventSystem Submit 反馈时机、自然回弹与单次回调

日期：2026-10-13（Asia/Singapore）。结论：在本次读取的源码版本中未发现本任务范围的生产缺陷，**无需修改生产代码，不提供 changes.patch**。已完成静态核对；本任务未编译、未启动 Unity／游戏、未执行运行测试、未做视觉验收。不能据此标记《动效策划》第 10 行的全局原则完成。

## 范围与已有工作

只核对 EventSystem 的 Submit 路径；未读取新策划正文，未增加全局键盘监听，未改 Assets、ProjectSettings、全局清单或其他任务目录。已阅读 `Documentation/DesignPass/Agents/motion-task-001.md` 和现有 `MotionButtonChecks`，避免重复提供已有实现。旧业务 Knock、声音、结果表现不属于本任务修改范围。

## 无需变更的代码证据

以下路径均相对于工程 `C:/Users/xomem/Documents/ChatGPT/games/competition-20261011`。

1. **反馈登记早于业务回调。** `Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs:24-29` 的 `OnSubmit` 先调用 `PressLabel()`，再且仅一次调用 `base.OnSubmit(eventData)`。`PressLabel` 会先检查按钮 active/interactable、Label 与宿主可见状态，再登记 Label 的独立 Knock。这里“早于”指效果已经登记，不能声称用户已经在回调前看到了动画帧；回调若立即关页／禁用，现有生命周期会取消反馈，这是预期行为。
2. **一次 Submit 只触发一次基础业务事件。** 当前包源码 `Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/UGUI/UI/Core/Button.cs:149-160` 的 `OnSubmit` 只调用一次 `Press()`；`:65-72` 的 `Press()` 在 active/interactable 条件满足时只执行一次 `m_OnClick.Invoke()`。`XgUi.cs:188-211` 仅安装该子类并为传入的业务回调注册一次监听。提交后的状态转换和 `OnFinishSubmit` 不再次调用业务。该保证指单次事件分发及单次已注册监听；业务主动重入或外部重复注册不在保证内。
3. **正常成功提交有自然回弹路径。** `XgButtonPressFeedback.cs:40-47` 登记普通 0.18 秒／减少动效 0.12 秒的 Knock；`:50-57` 的 `LateUpdate` 使用 `min(Time.unscaledDeltaTime, 0.05)` 累计到时长，随后 `ResetPress()` 清理并停用私有效果组件。`XgJuice.cs:102-106` 保存目标原始位置／缩放；`:292-306` 推进缓动并在完成时准确恢复原始变换。即使按钮侧计时先完成，`:202-204` 的 Clear 也通过 `:310-315` 恢复存储的原始变换。因此恢复不依赖额外 Submit、释放键或业务禁用。
4. **背景状态回弹独立存在。** uGUI `Button.cs:162-174` 的提交协程按未缩放时间等待颜色过渡后恢复当前选择状态；`XgUi.cs:194-197` 配置颜色过渡时长 0.05 秒。Label 的回弹仍由上述私有效果推进，不因背景先恢复而重新触发业务。
5. **保留旧强动效。** Label 的私有 `XgJuice` 与宿主作用于按钮本体的业务效果分离；Submit 不清空宿主效果。无需替换现有动效或新增输入监听。

动画计时每帧最多累计 0.05 秒；0.18／0.12 秒是累计动画步长，不是在严重卡顿时仍可保证的现实墙钟截止时间。没有把此现有保护机制判为本任务缺陷。

## 已有验证的缺口与待统一验证

`Assets/LocalBuild/Editor/MotionButtonChecks.cs` 已有 Submit 回调前存在效果、一次回调以及回调内禁用清理的同步检查。它未推进真实动画帧，不能证明正常成功 Submit 的完整回弹。原任务记录也已明确该缺口，本交付不把它重复描述成新增生产缺陷。

请整合负责人在隔离验证场景的 Play Mode 中补充以下实际帧验证。只创建临时按钮／Label／宿主与 EventSystem，不加载控制器或真实玩家存档；统一运行与截图仍归主聊天。

| 用例 | 操作与真实边界 | 验收依据 |
| --- | --- | --- |
| 普通成功提交 | Label 使用非默认初始缩放 `(1.2, 0.9, 1)` 和位置 `(7, 3)`；按钮保持可用，业务不关闭页面。使用 `BaseEventData` 经 `ExecuteEvents.submitHandler` 分发一次 | 回调内私有效果已登记且计数恰为 1；后续真实帧出现变形，之后恢复到非默认初始值，私有效果停用；继续观察至少两帧，回调仍为 1 |
| 暂停时成功提交 | 在隔离场景把 timeScale 设为 0，重复上述操作；结束用 finally 恢复原值 | Label 和背景仍能结束反馈；不依赖缩放时间或额外输入，回调计数不增加 |
| 减少动效 | 宿主 Reduced=true，再成功 Submit，保持界面开启 | 缩放变化较小、位置不偏移，真实帧后恢复；回调一次 |
| 跨帧快速重复提交 | 首次变形期间再分发一次 Submit | 两次独立 Submit 合计两次业务回调；最终恢复首次提交前的原始变换，不以中途形变作为新基准 |
| 禁用／非交互对照 | 分别禁用按钮 interactable、父 CanvasGroup.interactable，再分发 Submit | 无业务回调且无新增 Label 效果；不要仅用 pointerDown 代替 Submit 覆盖 |
| 回调当帧关闭 | 回调中分别禁用按钮、隐藏按钮对象 | 回调各一次、立即复位，没有残留变形；此用例不要求关页前已经呈现完整动画 |

不要只检查私有计时字段，也不要用手工调用 Update/LateUpdate 或复制缓动公式来代替实际帧推进。以可观察变形、最终变换、组件停止及业务计数作为主要断言；效果登记顺序可沿用现有检查的只读反射。测试应设有限等待，并区分“没有足够帧呈现动画”和“回弹后变换错误”。视觉验收另外确认现有输入映射下的键盘／手柄 Submit、背景变色和旧业务强动效；本交付没有宣称这些已验收。

## 原文件版本证据

SHA256（包含所读取文件的原始字节）：

| 文件 | SHA256 |
| --- | --- |
| `Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs` | `1F284BD333A1049AA0A8463BF9B3A3913D6C359AF0FEAA22794A66BCFAAABE78` |
| `Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs` | `A81D4AA3591C28EFA023FB2CAB4DCC976CC581A1B132CFE910FD181CEDC50F14` |
| `Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs` | `2ABCA8BD6A93D03CF679A786F1A8B6EB2B50A2505A68CE2AA5F890844CF567EC` |
| `Assets/LocalBuild/Editor/MotionButtonChecks.cs` | `22E10F5BD0C8DC49168D401F727214E47B9782B7B26C6D801C48FB3D8D59530E` |
| `Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/UGUI/UI/Core/Button.cs` | `3AD850616D59ED6303135BA648A620A3730398EE9D398914612C581BECF7C69C` |

## 交付与整合状态

唯一交付为本 `result.md`；没有生产修正需要应用。待整合项是保留以上静态结论与现有验证缺口，并由主聊天统一运行普通成功 Submit 的真实帧验证与视觉验收。无依赖缺失，无需新接口合同。结束前依赖复核结果见下。

结束前已再次计算上表全部 5 个依赖文件的 SHA256，与首次记录一致。此复核仅证明本次静态结论所依据源码未变化，不代表编译、实际运行或视觉验收通过。
