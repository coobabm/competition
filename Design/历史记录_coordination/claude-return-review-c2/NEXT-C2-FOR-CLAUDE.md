# 下一阶段 C2：输入夹具时序审查与读值判读（无需VM/Unity）

工作目标：主控保留真实前台Unity运行权；Claude用随包文本审查条件修复，收到实际trace后定位原因并交最小候选。当前诊断已由Codex整理，不能重复覆盖或重做该批探针。

立即可并行、无需等待Unity的工作：

1. 读本目录 REPORT、当前runner、返回的fix-after-readings.diff和InputSystem1.19相关源码摘录。画出Set(true)、Editor Refresh、player onAfterUpdate、Bridge.Update、Notifier.Update、Set(false)、OnPointerUp、Dispose各步的“当前buffer/目标buffer/raw/player缓存/editor缓存/current鼠标”时序表。明确每个结论的前提，不能写产品已无缺陷。
2. 静态审查条件修复的release pending、两段按住、失败/正常清理、一次按下边沿/点击音、InputSystem UI module恢复、原鼠标恢复。只报告具体可触发风险；不要反射写缓存、换掉产品InputBusy或删除严格断言。
3. 交一张判读表区分：H1缓存/缓冲、H2 current设备替换、真实失焦/最小化、绑定变化、notifier未执行或pre与notifier之间状态变化。判读必须引用具体phase/frame；不要把quiet30或全局mismatch计数单独当根因。

主控拿到日志后追加资料：完整单次trace、report.json、失败截图、当前source hash。Claude根据真实读值更新H1支持/否定/未决，并只在原因得到支持后交完整候选文件和小diff；先前未证实的条件方案保持候选。

交付到新独立目录，例如 `ClaudeReturnNext/C2`，包含 REPORT、QUESTIONS（设定冲突没有则写无）、时序表、必要完整候选与diff。默认仅允许 C2 runner及探针；产品/shared host若确有问题先列证据与最小hunk提案，不自行改共享源。无执行能力时写明未编译/未运行。不得要求Codex盲跑固定次数，测试次数由证据与新增修改决定。

双方边界：Codex独占当前诊断集成与Unity；Claude并行负责文本审查和证据判读，不运行Unity/模型、不上传/发布、不修改正常存档。当前公共阅读尾项、Files/Guide、ending-A事务、casino UI、最终Player按主控最新证据分别记状态；不得因本候选完成就宣称全任务通过。
