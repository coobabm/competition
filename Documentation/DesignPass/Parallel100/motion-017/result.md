# motion-017：桌面大脑与按钮旧特效复用核对

日期：2026-10-13（Asia/Singapore）。结论：当前已存在可直接沿用的实现与调用链；本任务未证实需要修正的复用缺陷，**无需生产改动，不生成 changes.patch**。下列候选逐一落到现有调用点，避免在通用按钮或桌面上再播放一次相同效果。本结论只涉及旧特效复用，不代表《动效策划》第 10 行的全局原则完成。

仅阅读现有源码、`Documentation/DesignPass/Agents/motion-task-001.md`、`MotionButtonChecks.cs` 和已出现的相关任务记录；未读取新策划正文。仅写本目录；未启动 Unity、游戏，未下载资源，未读取真实玩家存档，未发消息、提交、推送或打包。

## 接入候选与处理决定

下述源码路径均相对工程根目录 `C:/Users/xomem/Documents/ChatGPT/games/competition-20261011`；Desktop 简写为 `Assets/LingGuangV05/XingGuang/Desktop/`。精确源文件 SHA256 见同目录 `source-sha256.json`，行号仅用于导航。

### 候选 A：旧按钮本体回弹、火花、冲击波与新文字按压并存——已接入，保留

- 目标：Desktop/XgUi.cs:188–211，XgButtonPressFeedback.cs:30–45、70–74。
- `XgUi.Button(...)` 已构建 `XgButtonPressFeedback` 并赋 `button.Label = text.rectTransform`；私有 XgJuice 只 Knock 文字，ResetPress 只清理私有实例。
- 业务继续使用页面的 `Fx`，目标为按钮 `rt` 或 GPU 区域。不能把下面业务效果搬入 `PressLabel`：按下不一定成功，且文字私有栈没有完整 Init。
- 具体沿用现场：Desktop/XgTrainPage.cs:259–299。`XingGuangView.Bind` 订阅 `EpochStarted`/`EpochDone`，由现有视图事件转发到 Train，训练页检查窗口可见、当前页和轨道后才播放；无需再订阅。

```csharp
// 现有 OnEpochStarted 的手动成功分支，保留原参数：
Fx.Knock(train.rt, .1f, new Vector2(0, -4));
Fx.Burst(Fx.At(gpuTarget.gameObject.activeInHierarchy ? gpuTarget : train.rt),
    12, new Color32(255, 200, 90, 255), XgJuice.Shape.Spark, 300, 500, .45f);

// 现有 OnEpoch 的完成反馈，事件携带手动/自动信息：
Fx.Shockwave(Fx.At(epochBar, new Vector2(epochBar.rect.width * .5f, 0)),
    XgDark.Accent, e.hand ? 60 : 28, .3f, e.hand ? 5 : 2);
```

这段是当前源码的调用摘录，不是待追加代码。失败分支仍在 `Press()` 中使用小幅横向 Knock 和 Thud；不要在失败时复用成功火花。结果文本、进度和曲线变化提供具体结果表达，多个粒子或音效不能单独算成多层。

### 候选 B：保留结算盖章的旧强特效——已接入，保留

- 目标：Desktop/XgTrainPage.cs:408–434。
- 当前已有 `Shockwave(at, color, 260, .4f, 12)`、白色 `Flash`、40 个 Yen、30 个 Confetti、奖励飘字；高等级再加金环和 Star。调用位于既有结算条件分支，不能复制到所有按钮点击。
- Desktop/XingGuangView.cs:117、147–148 建立页面宿主 `Juice` 并 `Init(root, root, fontAsset)`；205–206 行设置 Reduced/Muted。继续复用这个实例即可。
- Desktop/XgJuice.cs 的 `At(rt, offset)` 将控件中心转换到 Fx 层坐标。不能直接用屏幕坐标或控件 anchoredPosition 代替。
- `Burst` 的 Square/Confetti/Spark 使用已有 Image 池，Yen/Star 使用 TMP 字形池；`Shockwave` 使用 XgRingGraphic 池，均不需要下载图片。所用中文字体和星形/货币字形仍需统一视觉验收。
- `Reduced` 已减少 Burst 数量、削弱 Flash 并禁止 Shake/HitStop；它不自动削弱所有 Knock/Shockwave。继续遵守现有手动/自动分支，不宣称全部效果都受相同减弱策略控制。

### 候选 C：皮层改线与统一大脑动画——现有页面已接入，不整体移植为桌面壁纸

- 目标：Desktop/XgBoardPage.Cortex.cs:156–195，XgCortexGraphic.cs:129–150。
- 现有调用合同：先同步真实概念到 `Regions[i].cells`/`UnifiedCells`，同步 `Reduced`；调用 `SetArch(i, arch, cortexInitialised)`、`SetUnified(one, cortexInitialised)`，最后 `Animate()`。首次同步不重播改线；后续同架构不会重启改线计时。
- 现有实现保留拓扑流光、金色信号与统一扩散，不需要建立第二个皮层对象。
- **不采纳的具体移植候选**：在 `XgBrainWallpaper.BuildFirstPoints` 中用 `AddComponent<XgCortexGraphic>()` 并设置 `GlyphArch` 替换桌面节点。XgCortexGraphic.cs:488–491 明确在 GlyphArch 模式使用 `Demo()`；它是图鉴演示节点，不能当作桌面的已学习概念。该候选会改变数据含义，所以不输出补丁。

### 候选 D：桌面光晕、发光连线与节点——已复用工程内绘制资源，保留

- 目标：Desktop/XgBrainWallpaper.cs:56–79、83–109；该 Install 已安装 Presentation、Concepts、Persona 等部件，并防止重复安装。
- `BuildFirstPoints` 的圆形辉光使用 `PrologueDesk.Circle()`。后者位于 `Assets/LingGuangV05/DesktopBridge/Story/PrologueDesk.cs:310–327`，缓存程序生成 Sprite；种子使用已有 HexCellGraphic。无需外部纹理。
- Desktop/XgWallpaperConcepts.cs:183 起的 `OnPopulateMesh` 已使用 `XgDraw.Seg` 的粗低透明光晕＋细线，以及 `XgDraw.Disc` 的两层半透明晕圈＋实心概念点。位置、颜色、连线来自实际 board；不要再叠一套演示连接。
- Desktop/XgWallpaperPersona.cs:26–54、66 起已根据实际性格轴生成六层轮廓光带、连续相位和 30 Hz 网格刷新；Presentation 的 Quiet 通过 MotionLevel 降低动作。调用现有 `SetQuiet(bool)` 即可沿用桌面安静模式，不应另建无关计时器。
- Desktop/XgBrainWallpaperPin.cs:93 起直接共享已有 Graphic 的网格/材质进行置顶绘制，不复制业务脚本；这就是现成的图像复用路径。其非交互叠层不能用作新的按钮输入层。

### 手写桌面按钮是否需要补接通用类

已核对 Pin Toggle（Desktop/XgBrainWallpaperPin.cs:61）与区域卡 Close（XgWallpaperRegionCard.cs:46）。它们仍使用普通 Button，但均设置 targetGraphic；Pin 点击切换真实叠层及标签，Close 点击隐藏说明卡。因此“未使用 XgButtonPressFeedback”本身不构成已确认缺陷，不能据此制造替换补丁。

若整合负责人另行要求统一文字压缩，具体代码变更位置为上述 AddComponent<Button> 改为 AddComponent<XgButtonPressFeedback>，并在 label/glyph 创建后赋 `button.Label = label.rectTransform` 或 `glyph.rectTransform`。**本轮不建议直接应用**：这些控件不在页面 Juice 的父链，当前类不会自动获得页面 Reduced 设置；Close 的 Submit 又会立即隐藏，文字动画未必有可见帧。需先明确桌面与页面减弱策略合同，不能把此候选当成已验证修复。本轮不新增宿主或公共 API。

## 初始化与所有权合同

1. 页面火花/飘字/冲击波/闪光使用已 Init 的页面 Juice；仅 Knock 可像现有文字反馈一样不 Init。Float/Burst/Shockwave 分配池对象时需要 fxLayer，Flash 后续 Update 需要 flash。
2. 同一个 RectTransform 由一个动效栈负责变换。通用类拥有 Label；业务拥有按钮本体。不可在取消文字按压时调用页面 `Juice.Clear()`。
3. 新桌面独立 Fx 层如以后确有需要，须明确桌面坐标、字体、可见性、Reduced/Quiet、关闭清理及结果事件唯一入口，不能借页面 Fx.At 在别的 Canvas 上绘制。当前桌面表现已经自带生命周期，不需为复用另加这一层。
4. 保留现有强效果条件；尤其不要为了桌面点击新增 `HitStop`，其默认路径会修改全局 Time.timeScale（LocalHitStop 才仅冻结本效果栈）。

## 静态检查、运行与待验证

- 已完成：当前源码控制流、调用参数、资源来源和既有验证入口的静态核对；保存依赖 SHA256，并在交付前再次比较。没有新增 C#，未进行本轮编译。
- 实际运行：未运行。视觉验收：未执行。motion-task-001 的旧编译记录不是本任务运行证据；MotionButtonChecks 中的 PASS 字符串也不是运行结果。
- 不重复已有 MotionButtonChecks 的登记/清理测试；主聊天统一检查普通按钮文字按压与业务强 Knock 同时播放，取消文字时粒子/业务回弹仍在，最终两种变换均复位。
- 真实训练边界：成功与拒绝各一次；手动与自动各一次；错误轨道、非训练页、隐藏窗口不新播训练事件效果；一轮结束只出现一次结果反馈。分别记录输入、结果、世界反馈，不把纯装饰叠加计作第二层。
- 桌面边界：首次解锁逐层显现；既有状态重新打开不重播首次显现；空概念桌面没有图鉴演示节点；Quiet 切换相位连续；置顶叠层不拦截点击；概念数量超过显示上限仍遵守现有裁剪。
- 视觉需检查强结算特效、文字清晰度、世界空间桌面的光晕与现有遮挡，记录静帧和完整动画过程。静态阅读无法确认画面“帅气”程度或流畅度。

待整合：保留 A/B/C/D 的既有生产实现；将本文件作为复用定位说明。无待应用补丁、无新增程序集依赖；统一编译、实际场景及截图由整合/主聊天处理。
