# hongmengos 桌面改造

## 当前入口

`/Users/kk/competition/Assets/DreamOS - Complete OS UI/Scenes/Desktop.unity`

使用现有场景和 DreamOS 交互，不复制新的一套桌面。用户已有的未保存场景状态先保存到了 `Assets/_Recovery/HongmengOS-Before-20261002.unity`。

## 任务栏

- 场景内 13 个应用的 TaskbarButton 启用 Open Windows Only。
- 未打开：隐藏且不拦截鼠标。打开：出现。最小化：保留，可点击恢复。关闭：隐藏。
- 禁用本模式下的固定/取消固定入口；保留旧的固定数据，不删除其他场景的用户设置。
- 开始入口、音量、网络、通知和时钟保留；删除的是默认常驻应用快捷图标。
- 配置开关是场景级别，默认关闭，不强制修改其他场景。
- 用缓存 CanvasGroup 的 LateUpdate 纠正 Animator Write Defaults 在失焦/最小化时重置启动透明度的问题；每按钮每帧 O(1)，无分配。窗口查找及临时集合仅用于测试，不在运行时循环中。
- 该模式下窗口动画等待实际播放完成再禁用 Animator，避免帧停顿或 Minimize/In 动画时长不同导致停在半途中。

## 显示器边框（用户追加要求后的最终版本）

- 外边框全拉伸到游戏视口四边，offset 为 0；不是居中的小显示器。
- 去掉了房间背景、桌面和显示器支架，不留外侧空白。
- 保留窄屏幕边框、顶部摄像头、底部 hongmengos 铭牌和电源灯。
- 内屏留出左/右/顶部 22、底部 60 个参考 UI 单位，底部包含铭牌；原 OS Canvas 在内屏中拉伸填充。
- 外层 CanvasScaler 按屏幕缩放，边框和内屏用伸展锚点适配比例。
- 装饰元素 raycastTarget=false；RectMask2D 防止窗口画到显示器边框外；WindowDragger 显式使用内屏坐标。

## 名称

- 开机标识、设置的系统标识、首次设置提示、Commander 欢迎语改为 hongmengos。
- 原包命名空间、资源路径与第三方许可证保持不变；不改其他游戏模块的全局 Product Name。
- 已取消的开机密码及 Lucide 图标保留。

## 改动与回退

- 场景：原 Desktop.unity。
- TaskbarButton.cs：可选的运行中应用可见策略。
- TaskbarButtonEditor.cs：Inspector 配置开关。
- WindowManager.cs：仅上述模式启用的动画完成保护。
- 代码原件和测试证据位于 `Evidence/HongmengOS/`，属于本地备份，不包含用户密码。
- 恢复改造前界面可使用 `_Recovery` 场景副本；不要直接覆盖当前场景中后续用户修改。

## 验证

实际结果见 `Evidence/HongmengOS/lifecycle-tests.json`、`frame-validation.json` 及同目录截图。测试覆盖所有 13 个应用的打开、最小化、恢复和关闭，以及初始隐藏、禁止固定和鼠标穿透检查。

### 最终复核

- `Evidence/HongmengOS/final-verification.json`：Canvas.pixelRect 与边框四角均为 (0,0)–(1920,1080)，确认边框完整贴边。
- 最新截图：`Documentation/HongmengOS/desktop-final.png`。
- 重启 Play 后时钟 singleton 正常；13 个关闭应用全部隐藏且不拦截点击。
- 控制台并非零错误：仍有 1 条 TMP_InputField.OnDisable 的 DestroyImmediate Mesh 错误，复核期间未增长；未修改该第三方包。另有 MCP 端口及渲染警告。
- 验证范围为 Unity Editor Play，未执行独立 Player 构建。

### 桌面图标加大

- 13 个桌面应用图标从 60×60 改为 78×78（+30%），保留网格间距、文字和 Lucide 渐变。
- Settings 桌面入口并未删除；仍在第二列第四行。原 prefab 子对象排序未更改，未为调整排序解包整个系统 prefab。
- 实际 EventSystem 射线命中 Settings 后双击可打开设置，窗口及底栏图标 alpha=1，关闭后底栏隐藏。
- 回退场景：`Assets/_Recovery/HongmengOS-Before-LargerIcons-20261002.unity`。

### 设置固定在底栏（2026-10-03）

按用户追加要求，仅 Settings 关闭 Open Windows Only，默认固定并更新 Settings_TaskbarShortcut=true。其他 12 个应用保持仅打开时显示。Play 实测：启动时设置未打开但底栏可见且可点击；EventSystem 实际点击可打开设置；关闭后仍可见且可点击。未修改 C# 或原 Prefab。

截图：`Documentation/HongmengOS/desktop-settings-pinned.png`。回退场景：`Assets/_Recovery/HongmengOS-Before-PinnedSettings-20261003.unity`（本地固定偏好独立保存）。

### About 清理（2026-10-03）

当前 Desktop 场景中停用 About 列表的 Support Channels、Discord、Twitter、YouTube 四个完整区块（含说明文字和链接按钮），不修改原 Prefab、脚本及许可证。系统信息保留。

重启 Play 后通过 About 导航验证，可见文字仅 About、hongmengos、System version、3.0.0，四个外链区块全部 inactive，不显示、不占列表布局、不接收点击。截图：`Documentation/HongmengOS/about-clean.png`。恢复副本：`Assets/_Recovery/HongmengOS-Before-AboutCleanup-20261003.unity`。

复核修复：Settings 固定到底栏后不再走 open-only 动画保护，出现 isOn=true 但 alpha=0 的复现。WindowManager 新增默认关闭的 waitForAnimationCompletion，仅在当前场景 Settings 启用，等待动画播放完成再禁用 Animator；其他场景默认行为不变。编译错误检查 0 条；从底栏打开设置后实际 alpha=1，About 可见文字检查通过。最终截图以 `about-clean-final.png` 为准；前两次截图未显示窗口，不作为视觉验收证据。

### 点击音效（2026-10-03）

原桌面与分享场景均采用独立 HongmengOS UI Audio 配置，启用全局左/右键点击声、关闭键盘声；使用原 Click.wav（约 48 ms），mouseStrokeVolume=0.45，沿用 Master 路由。现有按钮声保持关闭，避免重复。AudioManager 新增无输入设备、空音源、空配置和空列表保护，未创建新 Mixer。

Play 输入测试确认左右键触发、按住不连响；主线程轮询未捕获短音频，改用 OnAudioFilterRead 后，音源与 Listener 均捕获非零输出（峰值 0.140964136）。证据为 Evidence/HongmengOS/click-audio-live.json 及 click-audio-dsp.json；临时探针已移除。未验证外部扬声器实际听感，也未做独立 Player 音频验证。
