# 科技购买反馈候选（未合入、未编译、未运行）

## 文件与应用

- `tech-feedback.patch`：完整补丁，只改 `Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs`，新增 `Assets/LocalBuild/Editor/TechPurchaseFeedbackChecks.cs` 和 `TechPurchaseFeedbackRunner.cs`。
- `production.patch` / `checks.patch`：可分开审阅或合入；不要与完整补丁重复应用。
- `candidate/`：对应完整文件。`baseline.json` 记录生产文件原始SHA256；`prepare.py` 和 `package.py` 只用于此暂存目录的可复核生成，不是游戏脚本。
- 在项目根执行 `git apply --check <stage>/tech-feedback.patch` 后应用。先确保协调者的Unity编译/采样已结束。无需动皮肤、目录、价格、节点ID或存档格式。Unity自行生成两份Editor脚本的meta即可。
- **最终版接线依赖**：必须同时合入`tech_pan_candidate`的`XgPan`补丁（`XgTreePage.cs`），其公开API为`bool ReduceMotion { get; set; }`与`CancelMotion()`。本候选在Build初始化减少特效状态，并由同一页面watcher独立同步Reduced变化与隐藏取消；隐藏不伪装成ReduceMotion，恢复时保留真实用户开关。

## 证据与真实缺口

设计依据：`Design/ui/exe_UI_v4_交接_给GPT.md` §3；`Design/整合记录.md` 原始行121–122、202确认视觉无需再问、购买实时反馈、减少特效全停动画而声音独立。

- `XgTechTreePage.MakeNode`只在Build创建；Refresh→Sync/Paint复用对象，**不存在购买Refresh重建原节点的问题**。候选不引入延迟重建或复制图标。
- 原`OnBought`是随机`DOShakeScale`且Shockwave采用`LaneColor`。候选为1→1.2→1的短回弹与金色光圈；购买事件仍完全来自真实`XgSim.NodeBought`。
- 原节点是自定义Pointer/Drag处理，不经过`XgButtonPressFeedback`，缺95%按压；候选把固定透明命中图形放在节点根，单独缩放原Square，保留拖动转交，不自动伪造购买。
- 原悬停进入/退出直接DOKill会终止购买动画。候选购买序列期间悬停只更新静态状态，不终止购买序列。
- 原DOTween只在开始时判断Reduced。候选节点各自持有自己的Tween，OnDisable清理；全页仅一个`XgTechMotionWatch.LateUpdate`负责Reduced、Fx停用、CanvasGroup隐藏状态变化时一次清全页，不给每个节点增加逐帧回调。Refresh的清理在Pan.Moving早退之前；Sim重新绑定也清理旧动画。
- `XingGuangView.cs:121`已设置`Juice.DisableMotionWhenReduced=true`；XgJuice.Update清理其已有波纹/浮字/粒子，Play只检查Muted/Visible。候选另外显式门控科技购买视觉调用，声音调用留在门控之外。没有扩大修改共享Juice。
- `XgTechTreeSkin`只负责现有sprite资源，没有Tween生命周期职责，无需修改。现有通用按钮按压生命周期已有`UiV4PressChecks`覆盖，不无谓重写。

## 定向检查入口

协调者在专用Unity batch编辑器串行执行：`-executeMethod TechPurchaseFeedbackRunner.Run`，**不要加`-quit`**。Runner按现有SfxBackgroundLoadRunner形式创建未保存空场景，进入Play，完成检查后退出Play并退出进程；不打开Main，不生成真实存档，不修改Enter Play Mode设置。失败或错误退出码1，成功0。

外层报告：系统Temp目录的`LingGuang-TechFeedbackBatch-QA-*/report.json`；详细断言报告由`TechPurchaseFeedbackChecks PASS: <path>`日志给出。未成功时不生成PASS详细报告。检查不用旧TestMode经济夹具：采用真实XgSim、真实科技页，内存IXgHost钱包；订阅真实NodeBought并调用OnBought/Refresh。

检查涵盖：固定点击区域、95%按压/回弹、真实GraphicRaycaster在按压前后命中节点原边缘、详情按钮自己的射线目标与点击不购买、取消及拖动不购买、成功实扣实时价格一次且金圈、连续Refresh不重建或取消、悬停离开不取消、失败Spend和余额不足均无成功金圈、减少特效中途清理/开启时购买、音效与减少特效独立、关闭重开/CanvasGroup隐藏/Sim重绑取消旧动画、Pan减少特效与隐藏取消接线。

## 已验证与边界

- 本代理仅静态审阅、补丁可应用检查、生产文件哈希不变检查。**没有运行Unity、编译、Play检查或画面验收；不得标PASS。**
- 节点候选已接入平行`tech_pan_candidate`提供的减少特效/隐藏取消API；`XgPan`内部边界glide修复、导航行为和专项测试归该候选负责。需两份合入后联合验收，不能把本静态检查当作完整“所有动画关闭”验收。
- 锁定/失败点击保留既有左右抖动；按钮普遍的指尖淡光圈、三音和弦最终音色、真实LCD视觉、连点/性能验收未在此候选声称完成。普通UI按钮沿用原有声音和按压实现。
- 检查使用隔离Screen Space Overlay Canvas中的真实EventSystem.RaycastAll/GraphicRaycaster和实际输入处理器，未声称执行过真实LCD射线交互。自动检查通过后仍需实际窗口购买、减少特效中途切换、快速移出节点、拖动/缩放、详情按钮及中英文画面验收。
