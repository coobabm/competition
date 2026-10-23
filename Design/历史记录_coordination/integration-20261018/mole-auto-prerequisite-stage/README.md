# C5 自动敲击 QA 前提补丁（未运行）

只修改现有 `Assets/LocalBuild/Editor/MoleReviewUiChecks.cs`。baseline 为主代理已合入 provenance + review-token 后的生产源码快照；导入 `mole-auto-prerequisite.patch` 最小差异，不覆盖整文件。入口仍是 `MoleReviewUiChecks.Run`，180 秒 runner 上限、自动阶段最多 100 × .15 秒额外游戏步进不变。

## 实际运行证据

读取并保留：

- `LocalVerification/SidebarLayout/20261018-223030-024/report.json`
- `.coordination/integration-20261018/mole-review-token-ui-20261019.log`
- 同报告目录的四张 `mole-*-live/taught.png`，已实际看图。

报告失败在自动阶段 `real model emits an automatic whack; leave alone is not fabricated action evidence`。中英 live/taught 四图存在；按 runner 的串行断言顺序，两种语言的跨题和切走再切回 held-pointer 回归、原始回调不训练、真实新点击恰好一次训练均已通过。进入英文流程还证明此前中文自动及其后的 stale/owner/hidden/disposed 分支已经通过。最终英文自动阶段未通过，整个 C5 不能报告通过。

中文 taught 图：`∫[0,2] 4x^4 dx=133/5`，选择 false，解释应为 `128/5`。英文 taught 图：`[d(4x^2)/dx]x=8: 65`，选择 false，解释应为 `64`。原 QA 每语言仅保证一次随机题目的正确监督；本次两次都是负标签，却随后要求随机 15 秒内至少一次正类输出（whack）。这是缺失监督分布前提的 QA 假设。旧自动断言不输出 decision/whack/score，因此不能进一步断言失败时“所有决策都是 Leave”或“根本没有决策”；本包补齐这些证据。

## 本包改变

原手动 whack、跨题/切回、单次人工训练及精确回执完全保留。在该流程之后，新增固定监督前提：每语言合计 4 正标签、4 负标签，包括此前已完成的原始教学，所以再教 7 道。数量在观察预测之前固定，不按模型输出追加训练。

从实际生成器已产生的未消费题目中选择所需标签；仅在 QA 内使用已知 truth 决定按哪个真实 LCD 控件。每道都实际打开回顾、导航到原题、验证显示全文、点击 true/false，并检查 board +1、note +1、record +1、正确 linked player provenance、金额/样本不变。未调用 TeachMoleStatement 直接注入教学，未设置预测/权重/阈值或训练标签，不改生产逻辑。

每题生成搜索有 200 步上限；若本局耗尽时间，用真实 Restart 控件重开。监督完成后再次 Restart，让新题通过原生成/推理流程取得训练后的预测；不会把旧题 spawn 时缓存的预测冒充训练后的输出。模型不重置。

自动阶段仍必须观察真实 whack，仍检查零新增教学笔记/训练 cards、每次敲击恰好一条 AI-action 记录、空 playerAnswer/no note、双语非玩家来源。额外检查自动前后完整 serialized board 的 SHA256 不变。每次额外教学记录原题/token/label/真实预测前后；自动阶段输出实际决策/whack 数、每题实际预测及 model before/after SHA256。

8 个监督样本只建立明确可审计的输入前提，不是数学泛化证明，也不是保证模型必然输出敲击。若仍未产生 whack，原断言继续失败，并留下足够诊断，不改阈值或伪造输出。

## 空洞结论

本次日志 1542/1555（中）及1568/1581（英）显示：expiry=0、sameGame/visible/focused/context/sameOwner/playing=true、reviewing=false，pointer 后出现正确空洞反馈；真实 hole.Down 恰好一次的前置断言也已通过。此前失败的旧报告没有对应状态，因此历史根因仍未知。此次是“该路径中英文实际通过”，不是“已证实修复旧根因”。本补丁不改变空洞逻辑或断言。

## 验证状态

仅静态检查：最小 patch 对 baseline 的 git apply --check 通过、UTF-8 LF、原 whack/不自动训练/精确 provenance 断言保留、无直接训练API或预测注入。未运行 Unity、C# 编译或模型；生产及历史证据未改动。
