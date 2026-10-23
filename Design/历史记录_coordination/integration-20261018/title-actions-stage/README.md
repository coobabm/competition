# 标题退出 / 读档暂存补丁

应用 title-actions.patch；只涉及 CaseMenuController.cs、CaseMenuHud.cs、新增 CaseMenuHud.Archives.cs 与 Editor/TitleArchivesQa.cs。
不包含 Slots、SaveSlotsPanel 或存档后端改动；未写生产项目，未启动 Unity。

行为：标题右下两个按钮（读取存档[L]、退出游戏[Q]）；读档列6槽，空槽禁用，选择后再确认读取并开机；桌面未准备好给提示。使用既有 LoadArchive、保持既有开关机动画。关闭模态不改进度。
独立 QuitGame 保存成功才退出；失败提示并留在标题；Editor退出Play、Player Application.Quit。原GameExit关机逻辑保持不变。
新增交互组在淡出/设置中关闭raycast与interactable。读档窗位于同一32000 HUD内最后层，不受既有31000存档窗覆盖问题影响。Controller物理按钮射线忽略屏幕UI热点；读档模态独占键盘；标题选中UI按钮时Enter/Space不触发机箱开始。L/Q仅无设置/无模态/无转场/未进游戏时有效。

验证：Assembly-CSharp、Assembly-CSharp-Editor 安全编译通过。TitleArchivesQa.Run为EditMode独立检查入口，构造inactive Runtime且先指定LocalVerification目录，再初始化。验证真实槽2保存/确认加载/重读与UI门控，退出按钮只接测试回调，绝不实际关闭编辑器。并非真实LCD指针、键盘或Native Player退出实测。主会话应额外验标题实际点击、L/Q/ESC、中英文/小分辨率、开机后无透明遮挡、读坏档失败不转场、实际独立Player退出与关机返回。

现有风险由其他任务负责：SaveNow吞Saving异常可能返回成功（legacy_port_audit负责）；LoadArchive构造异常与仅.bak时间显示（主会话负责）。本补丁UI层捕获LoadArchive异常并停留提示，不重写后端。自动重载模板进入标题时原有SaveNow读档时间更新不变。
