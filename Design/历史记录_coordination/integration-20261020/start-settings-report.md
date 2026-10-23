# 开始菜单设置整合报告

工作区：C:/Users/xomem/Documents/ChatGPT/games/competition-integration-20261020
状态：代码完成；未提交，未启动 Unity，未修改冲突文件/场景/模型。

## 变更文件

- Assets/HongmengOS/Aero2010/Scripts/AeroStartMenu.cs
- Assets/HongmengOS/Aero2010/Editor/AeroShellBuilder.cs
- Assets/LingGuangV05/CaseMenu/CaseMenuController.cs
- Assets/LingGuangV05/CaseMenu/CaseMenuHud.cs
- Assets/LingGuangV05/CaseMenu/GameSettings.cs（新增及 meta）
- Assets/LingGuangV05/CaseMenu/DesktopSettingsOverlay.cs（新增及 meta）

## 行为

- 开始菜单左侧程序列表首项新增“设置 / Settings”。使用现有程序行样式、可滚动列表，支持中英文搜索（设置、音量、帮助、settings、volume 等）和 Enter 打开。
- 运行时以 ProgramEntry.gameSettings 标记幂等补齐现有 Main 场景；反复开关开始菜单不会增生按钮/监听。未来通过 AeroShellBuilder 生成场景也会包含入口。
- 保留现有系统 Control Panel、网络及声音托盘入口；新增设置项打开游戏标题的真实设置面板。
- 标题进入后的正常流程直接调用已有 CaseMenuController.OpenDesktopSettings，复用同一个 CaseMenuHud 实例和原有帮助卡片，不加载标题、不预加载、不创建第二份游戏状态。
- 直接运行 Main 时，才创建根级 ScreenSpaceOverlay 的同款 CaseMenuHud。覆盖层不嵌套于 LCD 离屏 Canvas，关闭后回到当前桌面，组件销毁时清理覆盖层。
- GameSettings 统一保存逻辑，保留 LingGuangV05.MasterVolume / ReduceMotion / Fullscreen 键。事件间隔仍由 XingGuangController.SetLifeEventIntervalMode 修改当前存档，保留不可读存档保护和保存失败反馈。
- 复用音量、减少动态效果、全屏、高低画质、60/30 帧、8/15 分钟/关闭事件间隔、帮助全部真实功能。
- 设置每次打开重新读取画质/帧率状态并刷新中英文标签，避免从桌面更改语言后重用旧 HUD 出现中文残留。存档保存失败消息显示于设置卡片底部，不再意外唤出标题装饰。
- ESC 从帮助返回设置，再 ESC/返回按钮关闭设置并保存 PlayerPrefs。
- 标题背景、机箱、装饰、AI 声明等区域未改。当前合并基线 AI 声明背景 alpha 为 .62（已告知主控，仅供核对）。

## 已做验证

- 向上查找 cwd/父目录及目标源码树，未找到适用 AGENTS.md。
- 六个变更/新增 C# 文件均通过 PowerShell 内置 Roslyn CSharpSyntaxTree.ParseText 语法诊断（零错误）。
- 四个已跟踪修改文件通过 git diff --check。
- 确认 GameSettings / DesktopSettingsOverlay 无同名类冲突；旧偏好键不变；新增两个脚本均有独立 .meta GUID。
- 人工检查未改八个合并冲突文件，未触碰模型或 Main.unity。
- 未进行 Unity 语义编译、EditMode 测试、PlayMode 或截图验收（由主控统一执行），因此上面的语法检查不代表 Unity 编译通过。

## Unity 验收路径

1. 打开 ComputerCaseMenu，进入游戏；点击左下开始按钮。检查第一项“设置”，反复打开开始菜单三次，列表仍只一个 Game Settings 项。
2. 点击设置：确认仅已有 CaseMenuOverlay 的 Settings 打开，没有新增标题场景或 Desktop Game Settings 根节点。桌面内容保持原进度。
3. 调整音量、减少动态效果、全屏、画质和帧率；关闭再打开值一致。关机返回标题后检查标题设置保持相同值；重开应用检查旧偏好键仍生效。
4. 在有可用 XingGuang 存档时循环事件间隔 8 分钟→15 分钟→关闭；重载存档确认值持久化。无可用存档时入口禁用；不可读存档不得被此入口覆盖。
5. 帮助打开/展开/滚动；ESC 只返回设置；再 ESC 关闭设置，桌面点击与输入正常，标题底栏不出现。
6. 桌面切换英文后重新打开开始菜单和设置，检查 Settings / Master volume / Reduce motion / Fullscreen / Help 等；切回中文后同样验证。
7. 搜索“设置”“音量”“settings”“help”，Enter 打开设置；搜索无关程序仍能正常开启原应用；无匹配搜索显示原空列表提示。
8. 直接打开 Main Play：开始→设置可见可操作；Desktop Game Settings 根级覆盖层仅一个，重复开关不增长。停止 Play 或卸载 Main 后覆盖层清理。
9. 在 1920×1080 与 1280×720 检查列表、设置卡片、帮助滚动区；新增入口位于滚动列表，避免挤压右栏关机区。复测标题现有 CaseMenuHudTests / CaseMenuTests，以及父级统一Play QA。
## 新增可执行隔离验收入口（主控补充要求）

新增 Assets/LocalBuild/Editor/StartMenuSettingsChecks.cs 及 .meta。

调用：Unity -batchmode -projectPath <整合工作区> -executeMethod StartMenuSettingsChecks.Run -quit -logFile <验收日志>。
也可空白场景的 EditMode 使用菜单 LingGuang/Verification/Start menu shared settings。
前置条件：当前未处于Play，Main场景未加载且不存在CaseMenuController；不覆盖或保存现有编辑场景。

测试临时 Additive 打开真实 Main，实际调用其开始按钮逻辑/程序行按钮与同款设置HUD控件，不执行Runtime初始化、开机协程或普通存档IO。包含：

- 开始菜单重复打开三次只有一个设置项，置顶且中文可检索；Enter打开。
- Direct Main fallback为根级覆盖层；真实音量slider、减少动态、全屏toggle写旧prefs键。
- 帮助打开/关闭仍停留设置；返回按钮关闭到桌面；重复打开复用同一个HUD；销毁组件清理覆盖层。
- 注入CaseMenuController已进入游戏状态及已建Hud后，通过真实菜单行打开同一个标题Hud，既不增加场景也不增加转场/备用Hud。
- 标题路径同样写旧音量键；帮助返回、设置返回不退到标题。
- 切换英文后菜单和复用Hud标签刷新，英文搜索可以打开。
- finally关闭临时场景，恢复此前活动场景/语言绑定；按原始“是否存在”恢复3个prefs键，并恢复AudioListener.volume与Screen.fullScreen。

通过日志标记：START_MENU_SETTINGS_PASS <断言数>。
边界：这是EditMode真实UI回调集成，不是全开机Play、物理指针、真实系统全屏或磁盘存档验收；标题已进入状态通过reflection注入，主控应保留上方Play验收路径。

该测试源码及调整后的fallback已通过带 UNITY_EDITOR 预处理的 Roslyn 语法解析。尚未运行Unity语义编译及实际断言。