# 设置帮助：真实机箱菜单输入验收候选

供主协调者串行 Unity 执行，**此目录没有运行或编译 Unity 的记录**。仅新增独立 Editor QA 文件，不改生产玩法、场景资产或构建列表。

先合入 LZ3 五篇说明最小补丁并通过 `PlayerKnowledgeHudChecks.Run`，再合入本目录 `player-help-input.patch`。在没有未保存场景改动、没有其它 QA 运行的编辑器中调用：

`PlayerKnowledgeMenuInputQa.Run`

菜单：`Tools / Local Verification / Player knowledge menu real input`。命令行可用 `-executeMethod PlayerKnowledgeMenuInputQa.Run`，不要同时加 `-quit`。测试需要有鼠标、键盘和 GameView 的 Editor；运行时自动聚焦 GameView，执行期间不要进行其它输入。真实 GameView 必须至少 640×480；当前分辨率记录在报告中。

## 输入范围

- 打开真实 `ComputerCaseMenu.unity`；实际模型、相机、Collider、Controller 与 EventSystem 均来自该场景。
- 先用生产 `HitButton`/Physics 射线验证机箱设置键；队列鼠标按下与释放，验证按下压键、释放后打开设置。
- Help 与三个展开按钮均先验证 EventSystem 顶层命中，再注入 MouseState 按下/释放，由正常 InputSystemUIInputModule 分发。
- 鼠标滚轮实际滚动；滚动条通过其真实 Handle 指针拖动定位正文/补充末尾。没有直接设置 content 偏移或 scrollbar value 来冒充输入。
- 验证 K03/K05/K06 默认收起、展开/收起、末段可到达、重新打开回到顶部、K04入口不存在。
- 实际点击“返回设置”；再次打开后发送 KeyboardState Escape，验证第一次返回设置、第二次关闭设置及焦点回归。
- 在帮助打开时切换 GameText 夹具绑定，等待生产 `LateUpdate` 自动本地化/重排；中英分别截图。**语言绑定是夹具，并非不存在的主菜单语言选择器的实际点击验收。**
- 没有 `Button.onClick.Invoke`、`ExecuteEvents` 指针回调、手动调用 Controller.Update 或手动调用帮助刷新来代替输入。

## 不接触玩家存档的隔离

`BeforeSceneLoad` 在真实菜单载入前设置独立 `VerificationSaveDirectory` / DreamOS profile 路径作为意外加载防线，并注册 `sceneLoaded`。

Unity 的 `sceneLoaded` 位于 Awake/OnEnable 之后、Start 之前。此时真实菜单的 Awake 已构建 HUD，QA 暂时将**运行时实例** `displayRig` 置空，使原有 `CaseMenuMainDisplay.Preload` 的参数校验立即返回，尚未进入 Main 加载分支。看到预期错误、确认无 Main/ChapterOneRuntime 后恢复同一实例引用，随后测试正常 Controller.Update 与 UI 输入。

因此这不是桌面预加载或开机流程测试。报告明确记录预加载被隔离；QA 不点击电源键、不进入游戏、不读取存档、不下载/运行模型。每帧检查没有游戏 Runtime；开始/两种语言结束分别验证 Main 从未加载、TransitionCount 为0。备用独立存档目录结束时应为空。

本测试不改场景资产、构建列表或生产脚本，也不创建一个伪造 HUD 替代实际场景。退出 Play 后恢复原场景布局和保存/profile路径覆盖。异常/手动退出/重载记录失败或中断；不会把未完成测试记为通过。若 Unity 进程被强杀，SessionState/进程状态不会持久改变场景文件。

## 输出与限制

输出目录：`LocalVerification/PlayerKnowledgeMenuInput/<UTC时间>/`。包含 `report.json`、截图、两个应为空的隔离数据目录；全部自动检查成功才写 `passed.txt`。每种语言10张截图，共20张：正文、滚轮位置、三项展开/末尾、ESC设置及菜单。

仍需人工看图确认字体与视觉效果。自动通过只代表记录分辨率下的排队输入通过；不是人手物理点击、发行包、真实存档或自然通关证明。主菜单原有固定文字的整体英文覆盖不属于此候选，只检查新增帮助内容及入口。

`manifest.json` / `static-check.json` 只记录候选静态检查与哈希，不能据此声称实际 PlayMode 已通过。
