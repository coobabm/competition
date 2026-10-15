# motion-014：摆渡众包浅色页通用反馈的文字、边界与可见性

日期：2026-10-13（Asia/Singapore）。交付结论：本范围未确认通用反馈需要生产修正，不生成 changes.patch。已提供独立实际帧采集组件；本轮只完成源码核查、名义颜色计算与独立静态编译，未启动 Unity／游戏，未取得运行截图，视觉验收仍待主聊天完成。

## 范围与避免重复

核对了 `Documentation/DesignPass/Agents/motion-task-001.md` 及现有 `MotionButtonChecks.cs`。已有同步检查覆盖输入与清理，本交付补其缺少的真实 Update/LateUpdate 后渲染帧证据，不再复制反射读取 punches 的同步检查。不读取新的策划正文，不改 Assets、ProjectSettings、全局清单或其他任务目录；未创建聊天、子 agent 或发消息；未接触存档。

仅覆盖授权条目下浅色通用按钮表现，不能代表“三层反馈，每个操作至少占两层”全局完成，也不能代表旧业务强 Knock、音效和粒子已验收。

## 原文件证据与无需补丁的理由

以下行号针对本轮读取版本；全部依赖 SHA256 及结束复核见 `source-hashes.json`。

- `Assets/LingGuangV05/DesktopBridge/Zhongbao/ZhongbaoView.cs:349` 使用 `new XgUi(font, window)`，默认 `dark=false`，确实走浅色分支。
- `Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs:188–210`：`targetGraphic=img`，按压颜色乘数是 `(0.82,0.86,0.96)`，标签独立且非射线目标；左右各留 6 单位。新增按压没有改写文字颜色、按钮矩形或命中区。`NoWrap + Overflow` 仍可能让长译文越界，不能仅凭 6 单位留白声称所有文字都安全。
- `Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs:31–55`：普通幅度 0.045／下移 1 单位，减少动效幅度 0.015／无位移；只把 Label 交给私有效果栈。禁用与宿主隐藏会清理，宿主隐藏也阻止开始。第 64–75 行处理组件禁用复位。
- `Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs:295–314`：按实际帧改变 Label 的缩放与位置，含回弹过冲，完成和禁用都恢复基准。故不能把“只压缩”误写成“绝无边界风险”；紧贴按钮边沿的长文字须采集回弹帧。
- `Assets/LingGuangV05/DesktopBridge/Zhongbao/ZhongbaoSkin.cs:92–95` 分离蓝底白字 Primary 与白底蓝字 Ghost；第 132–140 行的 Outlined 另有静止框架。通用 Label 动画不修改这些边框。
- `Assets/LingGuangV05/XingGuang/Desktop/XgContractsPage.cs:116–124,253–258` 提供实际短筛选按钮（72×26、13 字号）及训练链接（150×28、13 字号）；viewport 使用 RectMask2D。它们是采集样本的尺寸、文本和样式依据。

以上确认的是接线和所有权正确；文字清晰度、是否被遮挡、裁剪边缘以及实际背景变化仍需真实帧判断，不以静态检查替代。

## 名义对比计算

按不透明 sRGB 颜色、相对亮度公式 `(L亮+0.05)/(L暗+0.05)` 计算。按压背景为底色逐通道乘以上述 pressedColor；未采样真实像素，未包含 TMP 抗锯齿、材质、颜色空间转换、CanvasGroup 淡出或业务闪光。数据文件为 `nominal-contrast.json`。

| 样式 | 静止 | 按压终值 |
| --- | ---: | ---: |
| 通用浅底深字 | 13.72 | 9.97 |
| 白底蓝字 | 7.96 | 5.76 |
| 蓝底白字 | 7.96 | 8.74 |
| 选中筛选浅蓝底蓝字 | 7.12 | 5.18 |
| 已有金底白字 | 2.17 | 3.01 |
| 已有绿底白字 | 3.45 | 4.49 |

前四种常用样式未出现名义文字对比跌破 4.5:1 的新问题。4.5:1 仅作为常见小字号可读性筛查参考，并非本条策划新增验收标准。金／绿底白字在静止态就较低，分别来自 `XgLabelPage.Side.cs:117` 和 `XgLabelPage.cs:1398`；将其作为已知原配色风险保留进截图样本，不能归咎于本次 Label 动画，也不能据此擅改整套旧配色。主聊天可结合真实画面另行处理；本交付不宣称浅色页所有颜色都合格。

## 独立帧采集脚本与运行合同

`Motion014LightFrameProbe.cs` 是待整合的 MonoBehaviour 源码，不在 Assets 下，不会自动执行。主聊天统一验证时在隔离验证工程或隔离场景接入；不能把它挂到已打开的正式游戏场景。

1. 在已导入当前生产代码的验证工程中放入脚本。建立仅含一个空根对象的场景，挂本组件，赋予 `ZhongbaoView` 实际使用且含中文字形的 TMP 字体。保持无控制器、存档组件及遗留 DontDestroyOnLoad 游戏对象的干净会话。脚本拒绝多场景或另有场景根对象的情况，但此检查不承诺枚举持久场景的遗留对象。
2. 主聊天启动该隔离场景的 Play Mode，保持 Game 视图可见，使用组件上下文菜单 `Capture isolated light feedback frames`。不得在 batchmode 运行本入口：WaitForEndOfFrame 需要实际渲染 Game 视图。
3. 输出只写项目内 `Documentation/DesignPass/Parallel100/motion-014/captures/<UTC时间与随机编号>/`。每帧一张 PNG 和对应 JSON；截图采自 `CaptureScreenshotAsTexture`，JSON 在同一帧末记录。不会读取真实业务对象、创建游戏控制器或调用生产业务回调。样本回调仅累计次数。
4. 分别采中文／英文、`fixtureScale=1` 与实际较小显示比例（例如 0.75）；Game 视图至少取实际使用分辨率，并记录实际运行环境。缩放模拟只能补充验证，不能替代真实桌面 Canvas 和窗口比例。
5. 阶段包括静止、悬停、按住期间完整压缩与回弹、释放、Submit、减少动效、动画中隐藏／重开、父组动态禁用／恢复。各阶段随真正玩家循环推进，不手动调用 Update、不人工改写标签变换，不调用真实业务事件。主操作阶段 18 个渲染帧，同时保留源代码真实时间步长行为。
6. 帧元数据包含屏幕尺寸、颜色空间、实际 deltaTime、标签缩放／位置、按钮与字形屏幕边界、裁剪状态、字符缺失计数及回调计数。PNG 是实际可见性证据；`glyphsOutsideMask` 仅指裁剪前几何越界，故刻意部分进入遮罩的样本应为 true，不等于渲染泄漏。`rendererCull=false` 也不能证明部分字形没被裁剪。
7. 脚本在自然结束／隐藏／禁用后验证变换归位，异常保留 `INCOMPLETE` 状态；完整执行只写 `CAPTURE_COMPLETE`，不写视觉 PASS。图片编码可能降低帧率，应先看 deltaTime；若主要动态帧 deltaTime 超过 0.05 秒，或没有充分采到压缩／过冲，必须标记时间采样不足，在更稳定环境重采，不能假称 60fps 手感验收。

普通和筛选样本使用现有工厂、源配色及已核对尺寸。金／绿色样本是隔离配色探针，文案和尺寸简化；遮罩边缘样本是故意构造的边界条件，不是完整业务页面副本。事件为合成输入，覆盖实际反馈执行与渲染；物理鼠标和键盘路由留给主聊天实景验证。

## 待整合与验收

- 将独立采集脚本接入验证环境，使用真实字体完成统一 Unity 导入／编译。所需接口均已存在：XgUi.Button、XgBtn.Set、XgButtonPressFeedback、XgJuice.Reduced/Visible、ZhongbaoSkin 配色、uGUI、TMP、ScreenCapture；不需要新增生产接口。
- 按时间序列核对字形可读且不消失、长译文是否越过按钮边框、回弹是否引入新越界、遮罩外是否残留像素；区分静止态已越界与动画新引入的越界。隐藏截图中整组样本应消失，恢复后不得残留压缩／微移。
- 采集静止／压缩／过冲／归位四类代表帧。默认浅色按钮与页面背景较近，按钮边界是否足够可见需人工看图；不要凭名义文字对比推断边框合格。
- 在主聊天的真实浅色页面补录旧业务强 Knock／结果／世界反馈与通用反馈同时出现的操作。这份隔离脚本未启动旧业务，不足以证明“三层至少两层”或旧动效实景表现。

## 本轮验证状态

- 静态：已读上述源码与任务记录；已计算名义颜色比值。
- 静态编译：Unity 6000.4.5f1 随附 Roslyn，引用该编辑器模块、NetStandard 及当前 Library/ScriptAssemblies；同时编译当前 XgUi.cs、XgButtonPressFeedback.cs 与交付脚本，退出码 0。仅抑制源码覆盖旧程序集同名类型的 CS0436。命令参数见 `compile.rsp`，记录见 `static-compile.log`。这不等于 Unity 导入或完整项目编译。
- 实际运行：未运行；无本任务采集截图。
- 视觉验收：未验收。
- 依赖复核：见 `source-hashes.json`；若整合时任意相关源码已改变，先重新核对再运行。本任务没有生产补丁可应用。
