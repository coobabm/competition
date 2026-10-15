# motion-006：通用反馈生命周期与复位

日期：2026-10-13（Asia/Singapore）。结论：在当前工厂创建按钮、整棵按钮隐藏/销毁/重建及窗口关闭路径中，静态审查未确认需要修正的缺陷；无需生产改动，不生成 changes.patch。实际运行和视觉验收均未进行。

范围只包括《动效策划》第 10 行对应的通用按钮反馈生命周期；未读取新的策划正文，未宣称全局“三层反馈，每个操作至少占两层”完成。保留已有业务强动效。

## 已有记录与无需变更依据

先核对 `Documentation/DesignPass/Agents/motion-task-001.md`：原任务已实现停用复位、私有文字效果栈和宿主可见性检查；其验证明确没有推进真实动画帧。本任务不重复制作相同补丁，也不把既有编译记录视为本次运行结果。

下列路径均相对于工程根目录，行号对应本次记录的源码版本；完整 SHA256 见同目录 `source-evidence.json`。

| 生命周期 | 原文件证据 | 结论及边界 |
| --- | --- | --- |
| 按钮或父页面 SetActive(false)，随后重开 | `XgButtonPressFeedback.cs:64–74`：OnDisable → ResetPress → 私有 pressFx.Clear、enabled=false；`XgJuice.cs:202–210,310–315`：Clear 调用恢复原位/原缩放并清空 punches；`XgUi.cs:115`：Show 使用 SetActive | 复位不依赖后续动画帧；重开不会自行重新登记旧动画。重复清理空栈是幂等的。按钮与 Label 的 OnDisable 先后顺序都可清空同一私有栈。 |
| 只禁用反馈组件 | `XgButtonPressFeedback.cs:64–67` | OnDisable 仍复位，且保留 uGUI 基类停用处理。再次启用要有新输入才开始效果。 |
| 动画中禁用交互 | `XgButtonPressFeedback.cs:53–61` | Disabled 状态转换和 LateUpdate 均可清理。不清空父级业务效果栈。 |
| 窗口先关闭动画、后停用对象 | `WindowManager.cs:224–237,457–470`：先 isOn=false，之后才 SetActive(false)；`XingGuangView.cs:104,148`：宿主 Visible 包含 Window.isOn；`XgButtonPressFeedback.cs:34–35,49–55` | 关闭后不启动新文字反馈；已有文字反馈由后续 LateUpdate 清理，之后 OnDisable 再清理也安全。这里不能写成“关闭事件内同步复位”；它是可见性轮询。 |
| 整棵按钮/列表销毁重建 | `XgUi.cs:181–210`：Label 与私有 pressFx 位于新建按钮的子树；`XgContractsPage.cs:277,308`、`XgDataPage.cs:159`、`XgItemsPage.cs:143`：销毁旧对象子树 | 正常销毁会触发停用清理，Label 上的私有效果组件也随子树销毁；新按钮获得新的实例字段，不复用旧 pressFx。延迟 Destroy 的实际帧时序仍须运行验证。 |
| Label 与组件销毁顺序 | `XgButtonPressFeedback.cs:51,72`：Unity 对象空值保护；`XgJuice.cs:314`：目标仍存在才写回变换 | 子对象已销毁时不会继续写其变换；尚存在时先恢复。未发现需要新增 OnDestroy 重复清理的证据。 |
| Submit 回调同步隐藏自身 | `XgButtonPressFeedback.cs:24–28`：先 PressLabel 再 base.OnSubmit；uGUI `Button.cs:149–159`：回调后复查 IsActive/IsInteractable | 按压已登记后执行隐藏，再由 OnDisable 清除；基类对回调后已停用的按钮不启动 Submit 协程。 |
| 连续隐藏/重开后的复用 | `XgButtonPressFeedback.cs:39–44`：仅 pressFx==null 时分配，并在新输入重置 elapsed/duration；`XgJuice.cs:105–106`：新登记保存当时静止变换 | 隐藏不销毁组件，重开按压复用同一个私有效果组件；重新登记可采用关闭期间合法更新后的静止位置/缩放。 |

表中 `XgButtonPressFeedback.cs`、`XgUi.cs`、`XgJuice.cs`、`XingGuangView.cs` 和页面文件位于 `Assets/LingGuangV05/XingGuang/Desktop/`；`WindowManager.cs` 位于 `Assets/DreamOS - Complete OS UI/Scripts/Window/`；uGUI 位于 `Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/UGUI/UI/Core/`。

## 保留的接口边界

- 本结论依据当前 XgUi 工厂“Label 属于按钮子树、赋值一次”的用法。只销毁/重建 Button 组件而保留原 Label、任意换绑 Label、将其移到别的宿主，不是当前已核实的页面重建路径；不据此制造补丁或承诺自动迁移私有效果所有权。
- 仅把透明度设为零、同一帧内将 Visible 从 true 改 false 再改 true，且既不停用对象也不让 LateUpdate 观察到隐藏，不能由现有轮询保证取消。当前已核实的窗口关闭路径使用 isOn，页面隐藏使用 SetActive；尚未确认上述瞬时切换存在实际调用方，因此列为接口限制，不报告成已复现缺陷。
- 宿主关闭在后续 LateUpdate 被观察到时才清理私有反馈。要证明“画面重新出现前没有残余帧”，仍需实际窗口运行和截图；静态源码不能代替视觉验收。
- 只检查通用 Label 压感。宿主粒子、声音、世界反馈自身的关闭策略不在本任务范围，也未被改动。

## 待主聊天统一验证（全部未运行）

沿用 `MotionButtonChecks` 已覆盖的同步登记/清理检查；以下是其尚未证明的生命周期边界，不新增重复生产测试入口。使用隔离临时对象/场景，不创建业务控制器，不读取玩家存档。

公共前置：Label 初始缩放设为 (1.2, 0.9, 1)，位置设为 (7, 3)，触发真实指针按下；推进真实 Unity 帧并先断言变换已经偏离初值，再执行下列动作。不要用“只登记 Knock 尚未变形”代替恢复验证。每项结束清理临时对象。

| 动作 | 必须观察到的结果 |
| --- | --- |
| 分别隐藏按钮和父页面；同帧重开及隔帧重开 | 停用后位置/缩放立即恢复；再推进至少 0.3 秒未缩放时间无旧动画重播；再次按压能变形并自然复位。 |
| 停用反馈组件后重启；将按钮设为不可交互后恢复 | 文字复位；重新可用后新输入有效；同一存活 Label 的私有 XgJuice 数量不增加。 |
| 关闭宿主可见状态但保持层级 active，推进一帧后重开 | 后续 LateUpdate 已清除私有动画，重开不续播；既有宿主业务效果未被通用 ResetPress 清空。另行观察实际关闭动画与重开首帧。 |
| 动画中 Destroy 整棵按钮；推进到销毁完成后按原布局重建，重复 20 次 | 旧按钮、Label 和私有 XgJuice 的 Unity 引用失效；新实例从其自身初始变换开始，不出现 MissingReferenceException；销毁完成后对象数量不逐轮累积。不能在调用 Destroy 的同一行就断言对象已全部释放。 |
| 动画中先隐藏再 Destroy 整棵父页面，以及活动状态直接 Destroy | 两种顺序均无缺失引用异常；重建页面可正常交互且无旧文字位移。 |
| Submit 回调中 SetActive(false)，以及回调中整棵按钮延迟 Destroy 并重建 | 一次提交只执行业务一次；被隐藏/销毁按钮不残留文字动画；新按钮可独立按压。分别记录同步隐藏和帧末销毁结果。 |
| 隐藏后合法修改 Label 的静止位置/缩放，再重开按压 | 新按压结束回到新基准而不是上次按钮布局；保留原有按钮本体强 Knock。 |

视觉验收：分别在浅色和暗色窗口检查关闭重开首帧、切页、列表重建，记录位置/缩放是否残留及旧业务动效是否保留。结果只覆盖这些具体操作，不据此勾选整份动效策划。

## 检查状态与待整合项

- 已做：只读源码审查、已有任务记录核对、当前重建调用方核对、源文件 SHA256 记录。
- 未做：本次 C# 编译、Unity 运行、独立验证入口运行、真实帧测试、截图与视觉验收。没有将任何未运行项目标为 PASS。
- 待整合：负责人采用“本范围暂无确认缺陷、无需补丁”的结论，并将上表的真实帧边界纳入统一验收；无需应用生产补丁。
- 交付仅有本目录 `result.md` 和 `source-evidence.json`；未改 Assets、ProjectSettings、全局清单或其他交付目录，未启动 Unity/游戏、提交、推送、打包或访问真实玩家存档。
- 结束前依赖复核结果见下方追加记录；后续整合如哈希变化，应以最新源码重新确认对应证据。

结束前复核：source-evidence.json 中 13 个依赖文件 SHA256 均与记录一致；核心组件、XgUi、XgJuice、已有检查入口及 motion-task-001 也与首次读取时的哈希一致。此记录仅表示源码版本未漂移，不代表编译或运行通过。
