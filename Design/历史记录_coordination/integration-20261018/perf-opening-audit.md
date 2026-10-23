# 首批开局与教页移植（2026-10-18）

基准：main 30fbe6e4。未启动Unity。下列源文件已写入共享工作区，等待主控统一Unity编译。

## 已实现

- XgSim.TeachingFlow.cs：新档启用流程schema布尔标记；12个实际提交答案（包括错误）开科技；train.curve购买开练；代答须24题+1轮。老档默认不倒锁；存档字段默认false。
- XgCatalog.cs：train.curve（10元）、brain.window（15元）、label.ai（20元）Research节点及中英；后两者前置curve。brain.window目前仅节点，实际窗口需后续单元完成，不能标成已完成。
- XgSim.Features.cs / Opening.cs / Progression.cs / XgSim.cs / XgGuide.cs：统一门控，首个待办12题、第二个待办买曲线，样本达到4时不再错误提示已开练。
- XingGuangView.cs / Frame.cs：教/科技/练/AI四项显示；未开放灰锁和锁定提示；新档AI主页需首次对话。
- XgSocialGames.Deduction.cs：XgQuizGame改持续答题，移除安全点/爬梯/求助次数/换题/放弃/继续按钮，回答后下一帧出新题（遵循正文“立刻”高于文档0.25秒/1秒）。独立解释条1.5秒不挡下一题。每12题只计一套，连击不重置，不封顶。时间到0不失败。
- XgSim.cs / MiniPay.cs：慢读不再断连击；常识题15秒计时前半答对额外+50%，连击奖励最高+100%。其余小游戏旧奖励规则暂保持，等各游戏重做时统一。
- XgQuizGame：有label.ai时每题常驻AiDisplayName答案/把握，<55%显示不知道，结算后附对错。经典题面与其余小游戏代答未做。
- XgLabelPage.Hall.cs / XgLabelPage.cs：任务大厅首次可见且宽度有效后的下一帧RefreshHall+ForceMeshUpdate，重建和Shown均标脏。
- StageOneTutor.cs：教页首次、第3题、第12题、首次训练后四句独白；保存一次性标记；尊重已有过场/InnerVoice忙状态。
- XgMiniGames.cs：常识桌显示名与规则去除旧开心辞典12题爬梯描述，内部id保持兼容。

## 测试与限制

- XgTeachingFlowTests新增4条覆盖12题门控、121连击跨套、代答前置、存档往返与旧档不倒锁。
- XgOpeningTests更新过时开练断言，自动游玩辅助先购买curve。
- git diff --check通过（只有CRLF提示）。
- 现有Tools/LingGuangSim.Tests的.NET测试因Nuget代理127.0.0.1:9拒绝连接（NU1301）未执行；该项目其实只覆盖旧LingGuang。当前Tools/XingGuang/tests只有UnityShim.cs无项目。主控应运行Unity EditMode新测试，不能将本条记为已通过。
- 旧StageOneTutor仍会在旧后续节点自发老周私信，与无人知道AI/不能产生未做过事情冲突，应世界剧情单元重写为独白；本单元未覆盖剧情。
- 科技首次只能买curve，其余卡目前仍可见且锁定，灰轮廓外观需科技UI单元处理。
- 科技出现图标光圈、首次开科技“还能买东西？”、新题图库/经典卡每阶段题型、小工具和脑窗口留后续单元，不能宣称本单元全完成。

## 性能只读诊断移交

XingGuangView.Update每0.2秒Refresh当前整页。XgTechTreePage.Refresh每次Sync+Paint全部节点+PaintLinks；Tick每帧PlaceTooltip包含LayoutRebuilder.ForceRebuildLayoutImmediate。XgPan拖动本身仅位置改变且有惯性，但滚轮当前平移，按钮最大1.6，限位40px；需求鼠标锚点0.5–1.5、200px弹性、拖时关闭tip和延期刷新。viewport已有RectMask2D，未独立Canvas。性能Profiler采样应录真实可见UI三段各30秒，DeepProfile关闭，记CPU/GC.Alloc/Canvas.BuildBatch；未测不得声称60FPS。
