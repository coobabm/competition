# YY 群聊配图 Sender / Illustration Notice 高度修复候选

真实失败来源：`competition-latest-20261016/LocalVerification/SidebarLayout/20261019-051351-283/report.json`，首个失败为 `native sender fully rendered without ellipsis`，运行错误为空。该报告不是整套通过证据。

仅修改 `YYChatView.cs` 的 `PhotoBubble` 方法。普通文字消息、私人终局预览、选择按钮等区域保持基准原样；交付局部 `photo-sender-fix.patch`，不要整文件覆盖生产。root 可与 ending-private-paused 补丁按局部差异合并。

## 修改

- Sender 保留 12 号固定字体，不允许 auto sizing。用实际照片卡片宽度调用 TMP `GetPreferredValues`，高度取向上取整后的首选高度 + 4px，最低 16px；姓名行后留 2px。没有姓名标签时不保留空白姓名行。
- 头像和照片气泡均位于姓名行下方。气泡/图片布局与 `PhotoBubble` 返回的消息总高度都包含真实 `nameHeight`，保证下一条消息不会覆到图片或姓名。
- `Illustration Notice` 保留 10 号，按与实际 Rect 相同的宽度测高，最低 26px，另有底部 4px。原有中英文说明原样保留。
- 图注仍是 14 号、原有测高逻辑；它的底部位置和气泡总高度使用新的实际说明高度，使图注、说明、图片之间继续保持间隔。
- 图片尺寸、preserveAspect、来源标签、头像尺寸均不改变。不通过 Overflow 或字号缩小掩盖不足，所有原测试断言不变。

测高发生在现有消息重建路径，每个标签一次；没有新增每帧测量或布局循环。

## 验证边界

`verification.json` 仅记录静态检查和 `git apply --check`；本 agent 未启动 Unity、编译或运行 UI。需主控合入后串行复跑 `PublicReadingInteractionUiChecks`，保留原 Sender、Notice、图注、画幅、全窗口/滚动区域边界断言，要求 13 张中英共 26 个配图场景通过后才更新验收状态。

本修复适用 PhotoBubble 内的 Illustration Notice。其它普通文字聊天 Sender 仍用旧布局，本候选不扩大到未报告失败的通用聊天路径。
