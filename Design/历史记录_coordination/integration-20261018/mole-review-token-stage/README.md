# C5 回顾按压身份候选（未运行 Unity）

## 导入顺序

本包明确基于 `arithmetic-provenance-stage/candidate` 的两个文件，须先合入该 provenance 包。`mole-review-token.patch` 只有 `XgActionGames.cs`、`MoleReviewUiChecks.cs` 的最小差异；`new-files.txt` 列出新增组件及 meta。不要用 candidate 全文件覆盖生产。共享按钮、模型、题目生成器和训练核心不改。

## 行为

真假按钮原有 Button 保留外观，业务改由专用 `XgMoleReviewInput` 接收同一个对象的 EventSystem 事件。Down 捕获显示题目的 build generation、display revision、question 对象、question token、simulation/controller owner；有效 Up/Click 消耗一次捕获，再验证这些身份及当前上下文。切题后切回也因 revision 不同而失效。取消、拖动、离开、禁用、失去当前上下文会丢弃待提交输入。键盘 Submit 单独捕获当时显示题目，仍经过同样的业务检查。

`TeachReviewed` 接收捕获 token，不再在 click 时读取新题的 token。原始 `button.onClick.Invoke()` 没有业务能力。业务捕获也只可使用一次；核心 `TeachMoleStatement` 的 ticket/consumed/owner/surface 检查保留。

## 回归入口

仍用 `MoleReviewUiChecks.Run`（公共入口，Main 隔离存档，Sidebar phase14）。新增中英文两种模式的真实 LCD split-frame Down/Up：

- 按住判断，显式调用导航 fixture 切到另一道未答题，再松开；两道题都不能 consumed，cards/记录/笔记不变。
- 按住判断，切走再切回，再松开；即使 token 又一致，旧显示版本仍不能提交。
- 断言真实按压仍保持、eligible、无拖动、相同释放目标、pending capture 仍存在，避免取消输入造成假通过。
- 原后续真实新点击必须训练恰好一次；未经 Down/Submit 的原始回调不能训练。
- rebuilt/owner-swap/hidden/disposed 断言增加了真实可用时捕获的业务闭包，避免只调用已经不执行业务的 onClick 而失去意义。

导航是明确的反射 fixture；没有将它称为另一次鼠标点击。按压跨帧期间暂时禁用物理 BaseInputModule，以免真实鼠标干扰合成的 LCD 指针，finally 恢复。未改写题目标签、训练计数或消费标志。

## 空洞失败的证据和未完成项

实际历史日志 `mole-review-ui-canvas-fixed-20261019.log:1424` 和 `LocalVerification/SidebarLayout/20261018-191431-184/report.json` 只证明 `empty-hole pointer gives immediate feedback` 断言失败。报告的 screenshots/checks/runtimeErrors/infrastructureErrors 为空，没有当时的 feedback 文本、playing、focus、当前 game 身份或真实 pointerDown 回调次数。因此不能从此日志判定根因为生命周期、焦点、语言或输入未派发，也不能宣称已修复。

静态读取确认 `FocusToWindow` 只做 SetAsLastSibling，没有同步刷新/重建；`Hit` 在 playing/reviewing/context guard 后才产生空洞反馈；旧测试只验证 hover raycast 目标，没有验证真实 Down 已调用 hole.Down。

本候选保留原空洞反馈、金额、样本、cards 断言，并加：当前实例/owner/context/visible/focused/playing/reviewing/generation/time/语言/expiry/feedback 的前后日志，以及真实 hole.Down 恰好派发一次的断言。未重试、未调用 Hit 绕开 LCD、未强行激活失效对象。根因修复仍待主代理串行运行此候选后根据新增日志决定。

## 已验证 / 未验证

- 静态：基于 provenance 候选的 `git apply --check` 通过；UTF-8 LF；meta GUID；候选保留 provenance 的人类/AI 来源处理。
- 未运行 Unity、C# 编译、UI runner、模型或训练。不存在本包运行通过结论。
- 生产目录未写入；旧日志和报告未修改。
