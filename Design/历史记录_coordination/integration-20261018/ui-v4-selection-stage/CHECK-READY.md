# 可运行检查交付

- 源文件：`SelectionIsolationInteractionChecks.cs`。
- 根任务审查/应用业务补丁后，将此源文件复制到 `Assets/LocalBuild/Editor/`，由根任务串行执行 `SelectionIsolationInteractionChecks.Run`（不要带 `-quit`，检查会进入 PlayMode、退出后在批处理模式返回退出码）。菜单 `Tools/Local Verification/Selection isolation and focus` 同样可用。
- 仅 staging 写入。Unity Roslyn 静态编译退出码 0，日志 `static-compile.log`；未启动 Unity，没有运行 PASS 声明。响应文件基于现有 Editor 依赖快照，最终 Unity 编译仍必须跑。
- 测试使用实际 Main 场景/窗口/桌面路由，隔离磁盘存档与 DreamOS profile，禁用本地模型；阶段六全解锁夹具不证明自然进程可获取这些包。
- 输出 `LocalVerification/SelectionIsolation/<UTC>/report.json` 与成功时 `passed.txt`。检查结束恢复 EditMode；Unity Search 数据库内部错误单独记录，游戏运行错误直接失败。

验证内容：
1. 教页标题滑动包真实视图立即绑定，包行高亮，不出现基础桌重复高亮。
2. 打开真实众包窗口获得焦点；后台可见教页改为短信包立即换内容，但 `Focused` 仍 false；直接调用真实 Settle 入口不得改变状态（验证焦点防线，不代替小游戏正确性测试）。
3. 众包实际大厅按钮分别点 MNIST/标题桌，与私有短信/手写包交叉，断言 workDesk、CurrentCard、displayedId 同源；DeskGame/旧手写覆盖均关闭。
4. 公司题用真实 `XgLessonAnswerInput` pointer-down/click 提交正确答案，工作记录增加，教学统计/历史/小游戏记录/标题样本不变。
5. 回到教页恢复前台输入焦点和手写包；点实际常识基础桌按钮立即换玩法，清 teachingPack，保持公司选择。

范围限制：未覆盖完整小游戏操作、同阶段存档重绑、最小化后恢复、取消按下/失焦的所有输入组合；这些仍在 `定位与回归.md` 手工/后续矩阵中。没有把当前测试用途扩大为“所有交互已验收”。
