# Sheet attention PlayMode 验收

暂存脚本：`SheetAttentionPlayModeChecks.cs`。未写 main，未启动 Unity。

先合入答卷/热力图实现，再将此文件放入 `Assets/LocalBuild/Editor/`。从 Edit Mode 调用菜单 **Tools > Local Verification > Sheet original images and attention**，或执行 `-executeMethod SheetAttentionPlayModeChecks.Run`（不加 `-quit`）。使用 Main 场景、临时独立 Save/DesktopProfile；取消 BootManager 启动过程并由 UserManager.ShowDesktop 显式进入桌面，等任务栏和快捷方式启用后才 OpenWindow。不是自然流程进度验收。

输出到 `LocalVerification/SheetAttention/<UTC timestamp>/report.json`，每项记录 name / value / limit / passed / text。失败当场保留已测数值与异常；只有全部断言通过才写 passed.txt。不生成截图占位，也不把仅编译通过写成运行通过。

实际测量：

- 四个实际答卷行：缩略图宽/高各 48、原图纹理引用相同、继承透明度 >0.01、图片在行边界内。
- 点击 Question1 的 Unity Button 打开实际看它学：49 个遮挡格、非空颜色、累计透明度、原题图片不变。
- 答卷正误翻转前后比较同一纹理引用及完整像素哈希，确保不为了凑对错换图。
- 中英标题/说明的 TMP preferredHeight 对比实际矩形高度；英文无中文正文；文字边界不超出窗口、与原图交叠面积为零。
- 切到教页隐藏父对象，改 MLP 为 LeNet，经过 8 帧主动调用 Sync：签名和缓存项数不变；回到练页后签名变化，并输出 MLP/CNN 49 格差异绝对值之和。
- 点击照片原题：纹理仍为同一原图，热格累计 alpha 必须为 0，英文明确没有匹配分类器。

本脚本已用 Unity 6000.4.5f1 Roslyn + 暂存实现程序集编译成功，`compile.log` 无错误。运行态数值只能在主会话启动 Unity 后产生，本次没有编造 report.json 或运行结果。
