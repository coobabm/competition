# motion-007：连点复用、位移和回调次数

日期：2026-10-13（Asia/Singapore）。结论：当前生产实现在本任务范围内无需修改，不生成 changes.patch。完成的是源码静态审查；未运行 Unity、游戏或 MotionButtonChecks，未进行视觉验收，也不标记《动效策划》全局原则完成。

## 范围与现有记录

只审查 XgUi 创建的同一按钮、同一 Label 连点时的静止变换复用、位移计算、反馈组件复用和业务回调次数。先读取了 Documentation/DesignPass/Agents/motion-task-001.md 及现有 MotionButtonChecks，未读取新策划正文、后续条目或真实玩家存档。没有修改生产源码、全局清单或其他任务目录。

已有记录已说明连点复用和私有效果隔离；本次补充其实际代码依据、回调与业务去抖的边界，以及现有检查尚未直接覆盖的断言，不重复实现。

## 无需变更的代码依据

下列行号对应本次读取版本，路径均相对工程根目录。

| 核对项 | 证据与结论 |
| --- | --- |
| 动画中复用静止变换 | Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs:102–106：仅当 punches 中不存在该 RectTransform 时读取 localScale、anchoredPosition 建立 Punch。重触发只重置 t、life、amp、dir、squash，不重写 scale、pos，因此不会把中途形变记作静止状态。 |
| 不累计位移或缩放 | XgJuice.cs:298–304：每帧直接写入保存的 scale 乘缩放系数、保存的 pos 加当次偏移，没有在当前变换上累加。300、306 行先恢复静止变换再移除完成条目；314–315 行在清理时同样恢复后清空。下一轮动画可重新读取已恢复的变换。 |
| 不重复创建反馈组件 | Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs:39–44：只有 pressFx == null 才 AddComponent<XgJuice>，后续仅启用同一实例并重启 Knock。70–74 行 Clear 后停用而不销毁引用，因此结束、取消后再次按压仍复用同一组件。XgUi.cs:192、210 每次构建按钮安装一个反馈 Button 并绑定自己的 Label；不会在每次输入中重建按钮。 |
| PointerDown 不触发业务 | XgButtonPressFeedback.cs:18–21 只调用基类按下及 PressLabel；当前 uGUI Selectable.cs:1201–1211 只更新选中和按下状态。PointerUp 路径同样只更新状态（1241–1247）。没有调用 onClick 或新增监听器。 |
| 每个有效点击或 Submit 各调用一次 | XgUi.cs:198 在构建时登记一次传入回调；XgButtonPressFeedback 不重写 OnPointerClick，OnSubmit:24–28 先反馈、再且仅调用一次 base.OnSubmit。当前 uGUI Button.cs:110–115、149–151 分别调用一次 Press，Press:65–71 通过活动/可交互检查后调用一次 m_OnClick.Invoke。回弹、重启、清理路径不调用业务。 |
| 保留原业务动效 | PressLabel 对 Label 的私有 XgJuice 调用 Knock，ResetPress 仅清理该实例；没有清理宿主或改写宿主按钮本体的 Knock。现有颜色反馈与业务动效无本任务改动。 |

uGUI 证据目录：Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/UGUI/UI/Core/。

这里的“复用组件”不等于“零分配”：一次 Knock 完成并从字典移除后，下一轮可分配新的普通 Punch 对象；XgJuice.Update 也创建临时 List。这些不是重复 AddComponent，本任务不修改旧动效引擎。

## 与业务去抖的明确边界

- 对持续可交互、注册一次计数监听器的按钮，40 次单独 PointerDown 应产生 0 次业务回调；40 次有效的 Down → Up → Click 完整序列应产生 40 次回调；40 次有效 Submit 应产生 40 次回调。动画中重启反馈不会合并有效业务事件。
- 一次 Click 和一次 Submit 是两个提交事件；若输入模块均投递，应计为两次，不能要求反馈层擅自去重。
- 购买、奖励、交易、冷却等业务若需要拒绝重复操作，应由业务处理器的状态检查或禁用按钮负责。反馈层不增加节流窗口，也不保证回调内部只执行一次业务副作用。
- 如果回调禁用了按钮，后续非交互状态下的输入不再通过 uGUI Press；这与“连点默认只回调一次”不同。多个外部监听器、重复注册相同监听器、回调自己递归提交也不属于反馈层保证。
- 结论以 XgUi 的既有所有权为前提：每个按钮拥有固定 Label、私有反馈实例，其他逻辑不在动画期间共同改写 Label。所审查 XingGuang 源码中，反馈按钮 Label 的赋值点为 XgUi.cs:210。

## 现有验证与待统一验证

本次已完成：阅读当前生产源码、当前 uGUI 实现与已有任务记录，核对控制流并记录 SHA256。未进行编译、运行测试或视觉验收；旧任务记录的编译结果不是本次运行结果。

MotionButtonChecks.cs:25–28 使用非默认位置/缩放；66–77 人工设置中途形变后连续 40 次按下，检查效果条目数以及禁用后的复位。59–63 检查按下不回调、点击一次回调；82–84 检查一次 Submit 回调。这些已存在，无需重复交付相同检查。

现有入口未推进真实动画帧，也未直接断言连点后的 GetComponents<XgJuice>().Length 或连续完整点击/Submit 的精确计数。效果字典只有一个条目不能单独证明组件总数只有一个。以下由整合负责人在隔离验证对象上补验；本聊天不声称这些已通过。

| 待验证场景 | 判定标准 |
| --- | --- |
| 首次使用与动画中连续 40 次 Down | 使用非默认位置 (7,3)、缩放 (1.2,0.9,1)，实际推进至少一帧观察到变换变化后重触发；Label 上反馈组件数量从 0 到 1 后保持 1，同一实例，回调增量 0。 |
| 持续可交互的完整连点 | 单次注册计数监听器，连续执行 40 次 Down → Up → Click；每次计数增量恰为 1，总增量 40；组件保持同一实例。用实际事件路径再验证，不只直接调用 onClick.Invoke。 |
| 连续 Submit 与混合输入 | 连续 40 次 Submit 总增量 40；交替 20 次完整点击及 20 次 Submit 总增量 40。验证按下、抬起、动画结束都没有额外回调。 |
| 多次重触发后自然结束 | 在真实帧中反复重启，然后停止输入；组件最终停用、效果清空，位置与缩放恢复非默认基准（数值误差容差 1e-5），无累计偏移。对正常模式和 Reduced 模式各执行一次。 |
| 结束后及取消后再次连点 | 分别在自然结束、interactable=false 取消后重新允许输入；旧组件实例及数量保持不变，下一轮最终恢复同一基准。基准若在空闲时合法调整，下一轮应恢复调整后的基准。 |
| 回调第一次即禁用 | 连续完整点击或 Submit 中首个回调将 interactable 设为 false，保持禁用期间总增量仅 1；重新启用后再一次有效提交，总增量变为 2。此项验证 uGUI 状态门控，不为反馈层引入业务去抖。 |

视觉验收另行观察连点中的文字回弹、无逐次漂移及原业务强动效保留；数值断言不能替代实景视觉验收。自然完成应采用有上限的逐帧等待，以反馈实例停用为完成条件，不能仅固定等待 0.18 秒墙钟时间（当前逻辑将每帧时间步长限制为 0.05 秒）。

## 源文件指纹与交付状态

| 文件 | SHA256 |
| --- | --- |
| Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs | 1F284BD333A1049AA0A8463BF9B3A3913D6C359AF0FEAA22794A66BCFAAABE78 |
| Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs | A81D4AA3591C28EFA023FB2CAB4DCC976CC581A1B132CFE910FD181CEDC50F14 |
| Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs | 2ABCA8BD6A93D03CF679A786F1A8B6EB2B50A2505A68CE2AA5F890844CF567EC |
| Assets/LocalBuild/Editor/MotionButtonChecks.cs | 22E10F5BD0C8DC49168D401F727214E47B9782B7B26C6D801C48FB3D8D59530E |
| Documentation/DesignPass/Agents/motion-task-001.md | 755DBC9C34002157AE71792FA8484A02F0776311C36CF586518DC97D9C99B9EE |
| uGUI Button.cs（上述缓存目录） | 3AD850616D59ED6303135BA648A620A3730398EE9D398914612C581BECF7C69C |
| uGUI Selectable.cs（上述缓存目录） | 02EBB0484A16FE6FA0D34EA1CA5248B845503214D3D6E7318E055D54B8537596 |

结束前已重新计算上述 7 个依赖文件的 SHA256，全部与初次读取值一致，本次审查期间未检测到依赖变更。

交付仅此 result.md；无生产缺陷修正、无待应用补丁。待整合的是本结论及未覆盖的验证项目，统一编译、实际运行及截图由主聊天负责。未向其他聊天发送消息。
