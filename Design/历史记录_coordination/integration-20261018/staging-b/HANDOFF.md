# B-02 交接（2026-10-18；未写入主工程）

## 内容与接线
- `XgTrainingLessons.cs` → `Assets/LingGuangV05/XingGuang/Core/`：21 道双语阶段小题的纯核备用数据、9 张知识卡、图鉴节点定义。
- `XgSim.TrainingLessons.cs` → 同上：存档字段空值修复、单次事件订阅、阶段与现象触发、一次性答题奖励、收藏状态。
- `XgTrainingLessonResources.cs` 与 `XgTrainPage.Lessons.cs` → `Assets/LingGuangV05/XingGuang/Desktop/`。
- 将 Design/data/stage_quiz.json 复制到 `Assets/LingGuangV05/Resources/LingGuangV05/Training/stage_quiz.json`。加载器校验中英选项长度、答案索引、ID 唯一性；错误时保留内置题库。
- `B02-hooks.patch` 包含 5 个文件的最小接线差异；hooks/ 是当前源文件加接线后的独立编译输入，不可整文件覆盖已更新的 main。
- Catalog：AddNodes 调用 AddAtlas；Collection：lesson./quiz. 走 TrainingAtlasLit。
- View.Bind：InitializeTrainingLessons。View.Update：隐藏窗口先清 TrainingLessonModalOpen；可见时 TickTrainingLessons。卡片显示排在 StoryFocus/结算卡之后；每 0.2 秒检查状态签名，状态不变不重画。
- Core.EpochAction：BeginEpoch 与 TickEpochs 在 TrainingLessonModalOpen 时停止，卡片关闭/页面隐藏恢复，旧进度不清空。
- Tree.Click：`view.Train.ShowTrainingReview(e.First.target)` 返回 true 就结束点击；未收藏/未回答不开放。复习界面仍在练页，已答小题不会再发奖励。

## 实际验证
- 使用 Unity Bee 真实编译响应文件、真实 Unity/TMP/DOTween 引用，独立 Roslyn 编译 XingGuang.Core + Assembly-CSharp（含以上 5 个 hook 和 4 个新文件），退出码均 0。
- 日志为 core-compile.log/ui-compile.log，产物在本暂存目录；没有修改 Library 或用这些 DLL 替换游戏。
- 这只是 C# 编译验证，不是 Unity 编辑器导入、运行、UI 验收或可运行打包。Unity 许可证恢复后还需逐项实测。

## 尚未纳入该单元
- 热力图/答卷缩略图、前三档学习率 UI、答卷固定前 4 题、YY 问题日志接线属于 B 后续单元，未宣称完成。
- shift 需要实际图片平移识别失败时调用 NoteTrainingPhenomenon("shift")；continues 需要实际聊天续写失败事件调用；没有假定玩家经历过这些行为。
- 存档兼容 null 修复已写；实际旧档加载与奖励不可重复测试仍待运行。
- 卡片布局需要在不同窗口大小验证；当前 134px 顶部小题栏、540×360 知识卡。
- 知识卡触发观察：连续五轮平台；稳档连续五轮平台；错误标签>20；验证集解锁后差距>.15；NaN；单轮下降；单轮提升≥.1；阶段提升。具体阈值对玩法的频率仍需实测。
- 2016 年限与原文 Grokking/后期 SFT/Transformer 命名冲突未擅自删除：按项目总待确认处理。
