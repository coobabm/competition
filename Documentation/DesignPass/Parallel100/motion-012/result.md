# motion-012：业务强反馈与通用 Label 按压共存核查

日期：2026-10-13（Asia/Singapore）
范围：仅核查已有强 Knock、飘字等业务动效与通用 Label 按压的所有权、Clear 范围和覆盖风险。未阅读新的策划正文，未将第 10 行全局原则或整份文档标记完成。

## 结论

当前核查范围内无需修改生产代码，不提供 changes.patch。现有实现以「独立组件实例＋不同 RectTransform 目标」隔离通用反馈和业务强反馈，通用取消路径不会清空宿主的强 Knock、飘字、粒子或波纹。此结论来自静态源码核查，不代表实际运行或视觉验收通过。

已先阅读 Documentation/DesignPass/Agents/motion-task-001.md 和 MotionButtonChecks.cs；已有记录明确了目标隔离、强 Knock 保留及未来直接动画 Label 的所有权边界。本任务不重复制造相同实现或重复验证入口。

## 原文件证据

以下路径均相对工程根目录 C:/Users/xomem/Documents/ChatGPT/games/competition-20261011，行号对应 source-hashes.json 的快照。

| 文件与行号 | 代码依据及含义 |
| --- | --- |
| Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs:189–211 | Button 建立按钮本体 rt，再建立子对象 Label；button.Label 指向 text.rectTransform，返回 XgBtn.rt 仍是按钮本体。Text 在 181 行关闭 raycastTarget。 |
| Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs:34–45 | hostFx 从父层寻找，仅读取启用、可见和 Reduced；pressFx 通过 Label.gameObject.AddComponent<XgJuice>() 新建私有实例；Knock 只传 Label。没有借用宿主栈或把通用幅度写入宿主 Punch。 |
| Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs:49–75 | 到时、禁用及宿主不可见时进入 ResetPress；仅调用 pressFx.Clear() 并停用 pressFx，没有 hostFx.Clear()。 |
| Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs:31–43、102–107 | punches、floaters 及各池均为实例字段。Knock 按目标 RectTransform 查找、重置该实例的 Punch；已有目标沿用保存的静止位置和缩放。只有同一实例、同一目标的 Knock 会更新其幅度与时长。 |
| Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs:202–210、310–323 | Clear 调用本实例 OnDisable，恢复本实例 punches 目标、清空其字典并回收其飘字／粒子／波纹；不遍历层级，不调用其他 XgJuice 的 Clear。私有按压没有 Init、Shake 或 HitStop，因此其 Clear 也不持有宿主震动或全局时间尺度恢复权。 |
| Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs:109–118、327–338 | Float 使用所属实例的 textPool/floaters，新建文本挂在该实例 fxLayer 下，不借用按钮 Label；按压没有 Init，不建立第二个业务 Fx 层或调用 Sfx.Init。 |
| Assets/LingGuangV05/XingGuang/Desktop/XingGuangView.cs:46、117、147 | 页面 Fx 指向 host.Juice；宿主独立建立并 Init 业务 XgJuice。 |
| Assets/LingGuangV05/DesktopBridge/Zhongbao/ZhongbaoView.cs:354、370 | 众包界面使用自身 GameObject 上的宿主 Juice 并 Init，未从 Label 取通用组件。 |
| Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs:187–188、368、405–409 | 正误按钮经 ZhongbaoSkin.Outlined 接入；OnAnswer 将 button 设为 yes.rt/no.rt，成功时 Knock(button, .12f)，同时 Knock(paper)、Float、Burst、Play。Label 不在这些目标中。 |
| Assets/LingGuangV05/XingGuang/Desktop/XgTrainPage.cs:252–253 | 清洗噪声同时对 cleanNoise.rt Knock 并从该按钮位置发出 Float；均由业务 Fx 执行。 |
| Assets/LocalBuild/Editor/MotionButtonChecks.cs:37、63–78 | 已有检查登记宿主 .2f 强 Knock，重复按压 40 次后检查原 Punch 对象及幅度不变；禁用通用反馈后仍检查宿主 Punch 存在。无需另做相同同步检查。 |

补充只读检索覆盖 Assets 下的 Knock 调用点，以及相关目录的 XgJuice 查找、Label 赋值、Label 变换写入和 Clear 调用。未发现当前业务把 XgUi 创建的按钮 Label 作为强 Knock 目标，或从 Label 获取私有 pressFx 来发出业务飘字。按钮 Label 的检索命中主要为创建时布局设置；上述结论不扩展为对所有其他动画系统的全项目正确性保证。

## 所有权合同与边界

- 通用按压拥有 Label 的局部缩放、anchoredPosition 和私有 pressFx；业务拥有按钮本体／业务容器的变换及宿主效果栈。
- 不同目标的父子变换可以叠加。按钮本体缩放仍会影响子 Label 的最终屏幕大小；独立栈保证不会覆盖宿主参数，但不能仅靠静态检查保证视觉力度恰好合适。
- 业务若将来直接动画同一个 Label，即使另建 XgJuice，也会竞争同一变换，Clear 的恢复值可能覆盖业务姿态。届时应另建视觉子层或明确定义交接／取消协议；当前未发现此调用，不添加推测性补丁。
- 宿主在关闭／重建业务页面时主动 Clear 属于宿主的生命周期行为；本结论仅保证通用 ResetPress 不越界清理。
- 已有通用反馈不调用 Init/Play/HitStop，未改动业务动效参数、音效或旧效果流程。

## 验证状态与待验证项

静态检查：已完成上述源码阅读、调用点检索、实例所有权与清理路径检查。未在本任务编译、启动 Unity、运行游戏、读取真实玩家存档或执行 MotionButtonChecks；既有记录的编译结果不冒充本任务实测。

实际运行：未运行。交由统一验证会话补充以下边界，避免把同步登记检查误认为完整动画通过：

1. 宿主已 Init，先播放按钮本体强 Knock（例如 .2、位移 8）和长于通用按压的业务飘字／粒子／波纹，再连续触发通用按压；观察宿主条目身份、参数、时间进度及显示对象不被重置或回收。
2. 在两栈均播放途中，仅将按钮 interactable=false 或禁用按钮组件，保留宿主和页面可见。应只恢复 Label，按钮本体强 Knock 和 Fx 层业务飘字继续到自身寿命结束。避免通过关闭整个宿主来验证此项，否则无法区分正常页面生命周期清理。
3. 不禁用按钮，等待通用按压自然结束；验证 Label 回到非默认原始位置／缩放，而寿命更长的业务飘字仍存在。再等待宿主 Knock 自然完成，确认其自身复位。
4. 反向由宿主主动 Clear，确认业务效果按宿主规则结束；通用 Label 效果仍按自身时长或禁用路径复位，不残留缩放／位移。
5. 分别对鼠标按下后点击和 Submit 同回调触发业务效果运行上述场景，覆盖两种启动时序。

视觉验收：未验收。主聊天在暗色训练界面与浅色众包界面检查强反馈及飘字共存、文字清晰度、父子缩放叠加力度和结束姿态，采集实际场景截图。本任务未生成或声明截图证据。

## 待整合与依赖复核

仅需整合本核查结论和上述待验证项，无生产补丁、无新增测试脚本需要合入 Assets。交付文件只有本 result.md 与 source-hashes.json。

核心四个文件在首轮哈希和交付前复核间未变化；最终依赖复核记录见 source-hashes.json。若后续集成修改这些源文件，应按新内容重新判断本结论，不能将本快照视为持续有效的运行保证。

