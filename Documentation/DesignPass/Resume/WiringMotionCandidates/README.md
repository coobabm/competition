# 接线节点与端口：候选交付

2026-10-13。仅准备于 Assets 外，未应用生产补丁，未启动 Unity，未碰存档。原文停在动效第 10 行当前原则。

## 内容与行为

- `changes.patch`：仅修改 `Assets/LingGuangV05/XingGuang/Desktop/XgWiringPage.cs`。同目录完整候选 `XgWiringPage.cs` 供复核，不应与原文件同时导入 Assets。
- 节点改用已有 `XgButtonPressFeedback`，私有反馈只作用于标题 Rect；卡片位置、大小、拖动与业务回调不变。
- 输入/输出端口同样复用该组件。各自新建一次无 Graphic 的 `PressVisual` 子 Rect，原 DrawFront/DrawPort 读取它的缩放与微移。原前景绘制次序和裁剪保持；26×26 命中范围、临时线及实际线缆端点均继续用原坐标，不随动画移动。
- 不新增动效计时器、全局事件或音效。释放、拖出、禁用、CanvasGroup、减少动效均交既有组件；不在点击时重建页面。
- 保留 Select、Arm、ConnectPorts 及已有成功/拒绝 Hint/Toast。不把流点或任务完成脉冲改成每次点击成功的伪反馈。
- **线缆单击尚不在补丁内**：它与空白取消、平移共享 XgWiringGraphic 的输入入口。往全画布添加 Button 会影响原选择/提交路由，需下一小项单独处理。

原生产文件 SHA256：`2F97346E43C5F60C6C2FBD26A82C4C36488FFAF971011D1992C2A51FF3AEC1C1`。完整依赖与候选摘要见 `hashes.json`；如应用前源码变动，应逐段合并，勿覆盖主聊天新代码。

## 验证片段

`WiringMotionChecks.cs` 供统一验证时导入独立 Editor 验证目录。调用合同：在 PlayMode 协程内 `yield return WiringMotionChecks.Frames(Font)`，Font 为实际 Desktop CJK 字体。

它创建真实 XgWiringPage 与内存 XgSim，使用模拟钱包但不替换任何 Board/Connect/选择/拒绝逻辑；不创建控制器或访问存档。输入通过 ExecuteEvents 派发，动画只靠自然 Update/LateUpdate；不写 Punch.t，不反射调用生命周期。异常由共享运行器捕获；结束清理临时对象并恢复仍有效的原选中项。

覆盖：节点、输出端口、输入端口的右键与禁用；左键按下不执行业务；释放不额外点击；目标真实变形；命中矩形及线缆端点固定；端口实际网格随视觉目标变化；拖出取消；组件关闭与父 CanvasGroup 禁用；Reduced 初始及中途开启；自然准确复位、私有组件复用。随后以真实按钮回调验证节点详情、接线提示、实际接通、同节点接线拒绝、原有节点实例复用和一次点击一次分发。

天然等待按实际 capped unscaled delta 与独立 5 秒墙钟限制，不用固定帧数代替动画时间。它不是物理鼠标/键盘/手柄测试，也不代替截图。仍需现场查看端口小幅度是否清晰、标题字体/卡片边界、前景遮挡、跨节点拖拽和真实输入取消。

## 已执行状态

- 精确 `git apply --check` 通过；未应用。最初生成补丁时 PowerShell 写入 CRLF 与原 LF 不匹配，统一补丁 LF 后无忽略空白参数也通过。
- 最初整程序集候选编译撞到主聊天正在更新的 `XgConceptPull` 与旧 core.ref 不一致；保留 `LocalVerification/DesignPass/WiringMotionCompile/candidate.log`，未改主聊天源码/引用文件。
- 随后用实际现有程序集引用，对候选页面、原 XgWiringGraphic 与候选验证片段进行独立 Roslyn 切片编译，退出码 **0**，日志 `LocalVerification/DesignPass/WiringMotionCompile/slice.log`。重复来源类型采用源码优先，只抑制预期 CS0436；没有替身实现。程序集输出仅在 LocalVerification，不替换 Library。
- **没有 Unity 全量导入/运行 PASS，没有自然帧/视觉 PASS。** 当前交付是可复核的小补丁与已静态编译的检查候选，待主聊天安排。
