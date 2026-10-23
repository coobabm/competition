# 老周新闻短评实际 LCD 验证候选（独立辅助类）

仅候选，未执行、未编译、未改生产。依赖主控先整合 `../laozhou-comments/laozhou-comments.patch` 的产品 API。当前目录不修改 `SidebarLayoutChecks.cs`，不抢占共享 runner 的 SessionState 或测试阶段。

文件：`candidate/Assets/LocalBuild/Editor/LaoZhouNewsCommentsUiChecks.cs` 和新 meta。`baseline` 为空，表示全新增文件。统一补丁为 `laozhou-comments-ui.patch`。

## 明确接入方式

这是辅助 `Frames` 类，不提供会误跑其他套件的 MenuItem/Run。由主控现有独立 Main 验证入口调用；无需在本候选中修改共享 Sidebar。入口采用现有 `BookLibraryUiChecks` / `SidebarLayoutChecks` / `PublicReadingInteractionUiChecks` 的生命周期和实际 LCD 工具：

1. 外层 runner 建立自己的 `LocalVerification/<本次>/Save` 隔离、PlayerPrefs/场景快照与恢复，加载真实 Main、关闭 LocalLlm；等桌面自然可交互。参考 `DailyFeedbackActualUiRunner` 或 Sidebar 已有初始化，不操作普通玩家档。
2. 外层令 runtime 为 `TestMode=false`, `useDiskSave=false`, `tickEnabled=false`，在真实前台运行。传入现有真实 `XingGuangController`。本辅助类会再次核验隔离状态、LLM 未就绪及真实前台，不设置 `inputEnabled` 或假造焦点。
3. 绑定 `GameText` 的本次语言变量，并调用：

```csharp
locale = GameText.Bind(() => language);
frames = LaoZhouNewsCommentsUiChecks.Frames(ctl,
    en => { language = en ? GameText.English : "zh-CN"; GameText.PublishChanged(); },
    filename => {
        if (!QaDesktopCapture.Save(Path.Combine(outputDirectory, filename)))
            throw new InvalidOperationException("LCD capture failed: " + filename);
        screenshots.Add(filename);
    });
```

4. 按既有 Sidebar 模式每个新 Unity frame 仅推进一次 `frames.MoveNext()`，建议本项上限 420 秒。正常耗时预计约 100–220 秒，但未经实测。异常/超时必须记 failed；不要重复启动 Editor。
5. 结束记录 `Scope`、`AssertionCount`、`ShareClicks`、`AcceptanceClicks`、`Checks`、截图列表。只有自然结束且 `ShareClicks == 24`、`AcceptanceClicks == 4`、无运行/清理错误时才能记录 passed。
6. 外层 finally 必须 Dispose `frames` 和 `locale`，以执行辅助类恢复保存所有者和独立驱动 enabled 状态；再由外层恢复场景/偏好/隔离路径。不能仅停止 MoveNext 而不 Dispose。

不要把它套在 `PublicDiscourseUiChecks.Frames` 的 `sourceFrames` 内：该旧套件将 `StoryFocus.Probe` 固定为 true，会使本项应保留的真实 `ZhouIdle` 一直关闭。调用本辅助类时不得继承这种人为忙碌探针。不要为通过测试强制输入可用或将焦点探针改为 false；在现有独立 driver 已冻结的 Main 场景中等焦点自然解除。

## 覆盖矩阵

- 三篇 T01/T03/T04 × 中文/英文 × 未接受 YY 的贴吧路径/真实接受 YY 的 YY 路径：12 次成功分享。
- 每次分享连续两次真实 LCD 点击旧按钮，断言仅一条玩家分享、一条短评、一张收据：24 次实际分享点击。
- 4 次真实 LCD “加 YY”：两轮 YY 路径先接受，两轮贴吧路径分享完再接受；没有预设接受收据。
- 每篇先真实打开首页/必要时旧新闻列表/标题，并等待；随后真实点击“我已读完”，可转给 AI 的新闻还真实点击“转给它看”。这些操作不能改变老周 QA/知情/收据，也不能给任一私信通道增加消息。
- 主动分享后，原新闻页与对应真实贴吧/YY 私信均显示批准回复，检查无截断，预计 24 张截图。私信由检查显式打开，不是产品主动打开；分享后先断言两个私信窗口没有自动打开。
- 每轮 JsonUtility 序列化并绑定新保存所有者，切换为另一语言重开三篇，显示已发送与对应译文，不再出现任何渠道的分享按钮。两路消息计数保持不变；贴吧分享后加 YY 也不搬运/重发。

## 夹具和未覆盖边界

阶段 1、2016-06-06、安装态、AI 已入群、未接受好友、资金等均为明确夹具。阶段 1 允许真实邀请，不会自动排入阶段 2/6 问答。独立剧情/经济/通知生产者和 YY 自主 Update 被暂时禁用，论坛 Tick 保留以真实投递邀请；视图、原生窗口、LCD 输入门禁和产品 ZhouIdle 均保留。所有点击使用既有 `FilesWindowUiChecks.Click`（LCD 坐标往返和命中归属校验），没有 Button.onClick.Invoke 或直接调用 ShareNewsWithZhou/Accept。滚动定位是测试夹具，不宣称物理滚轮输入。

本候选不测试磁盘 SaveNow/失败回滚、不跑模型、不证明自然日期推进/完整主线、不证明窗口之外的全部后台生产者均无副作用。没有强清消息队列、强开输入、假造操作系统前台；焦点或命中被拦截应真实失败并保留报告。
