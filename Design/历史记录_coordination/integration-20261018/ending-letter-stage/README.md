# E-C 终局公开来信候选

本目录仅为候选交付；没有改生产实现，没有运行 Unity、.NET、模型、重编译或完整通关。root 在串行 Unity 空档一次性合入新文件与共享最小补丁。资源路径按协调者后续决定使用 `Resources/LingGuangV05/Epilogue/ending_letters.json`，不放入 Story 根目录，不改 StoryLibrary。

## 内容与文件

- `candidate/Assets/LingGuangV05/XingGuang/Core/EndingLetterCatalog.cs`：三信目录、同段双语、公开来源/本机推荐排序，以及不接触存档的展示会话协议。
- `candidate/Assets/LingGuangV05/Mail2016/EndingLetterPresenter.cs`：可挂原 MailInboxView 的实际 UGUI/TMP 组件；原生 Win7 色彩、文字和按钮，无模糊/粒子/模型生成。
- `candidate/Assets/LingGuangV05/Resources/LingGuangV05/Epilogue/ending_letters.json`：三篇定稿全文，各9个稳定段落，共27对中英；包括问候和独立署名。来源 `public_article`，文章ID与 sourceArticleId 相同。另含30对UI文案和3个可选事实插段。
- `candidate/Assets/LocalBuild/Editor/EndingLetterChecks.cs`：内容及异步协议检查入口 `EndingLetterChecks.Run`，准备覆盖显式保存、确认取消、保存失败、位置修订、重复完成、旧绑定、隐藏后的回调和只读模式。
- `candidate/Assets/LocalBuild/Editor/EndingLetterUiChecks.cs`：准备好的真实 LCD 点击 Frames，调用方提供已可交互的隔离邮箱窗口。保存回调明确是测试夹具，不能用其成功代替真实 AtomicSaveStore 验收。
- `mail-inbox-integration.patch`：仅修改共享 MailInboxView 的必要hunk；`integration-preview` 是审阅副本，**不得整文件覆盖生产**。
- `TEXT-INVENTORY.md`：逐项列全部UI新增/复用文案及工作邮件来源；正文逐段直接提取来源稿，无临时扩写。

稳定路线/文章：`online → zhou_open_door`、`public_model → zhou_copies`、`local_only → zhou_unpressed_button`。旧E1/E2/E3不映射成路线，空路线维持目录默认顺序。三篇始终都可主动阅读；排序不向作者披露路线。无邮件地址、未加YY、未发帖均不影响公开信成立。

## 与 E-A / root 的接线

1. `EndingLetterPresenter.Install(host, font, nativeWindow)` 仅装载目录与UI。共享补丁新增 `MailInboxView.BindEndingLetter(input, callbacks, show=false)`，由owner负责在状态变化时绑定。公开专栏“保存”入口可使用 `show=true` 展示未保存的文章；收到保存成功回执后才显示邮箱行/红点，不自动加好友或自动发私信。
2. `EndingLetterInput.owner` 必须是实际 `runtime.Sim` 引用，`revision` 来自 E-A `epilogue.revision`（long兼容int）。仅投影 `articleId/paragraphId/saved/opened/consentShown/readOnly/completionEligible/english/reduceFx/soundEnabled`，不传完整存档或聊天。`soundEnabled` 应由现有静音/音效设置得出；Click复用现有Sfx池，不重置全局声音服务。
3. `EndingLetterCallbacks.IsCurrent(owner, revision)` 验证实际保存槽对象及当前修订号。回调收到 `EndingLetterRequest`，含owner、revision、requestId、articleId、version、sourceArticleId、paragraphId。
4. 回调适配E-A：`RequestSaveArticle → SaveArticle`，`RequestOpen → OpenArticle`，`ReadingPositionChanged → SaveReadingPosition`，`RequestComplete → Complete`；操作前重新核验请求令牌，绝不只按articleId批准。E-A成功保存后取新的 `CaptureBinding()`，将其实际owner/newRevision和操作Success/Code放入 `EndingLetterResult` 调用done。失败返回已回滚的当前owner/revision。不要先重绑Presenter再回复旧done；完成响应接纳之后，owner可同步新的输入。
5. `RequestBackToDesktop` 只负责安全关闭邮箱/回到桌面。回看时同一回调应路由结局档案。最终标题和成就仅订阅E-A持久完成事件；Presenter自己不写 `chapterComplete`、不写磁盘、不发标题路由、不发奖励。
6. 任何实际换档/销毁必须重新Bind或Hide。UI回调持有内部代次和请求ID；旧owner、旧修订、重复响应及隐藏后响应不会更改新UI。owner仍须独立拒绝过期请求，不能把UI令牌当保存权限的唯一校验。
7. 世界语言变化由共享邮箱Refresh调用 `SetLanguage`；音效/减少动态设置变化调用 `SetPreferences`。字距/字号只重排9个保留段落对象；滚动300ms稳定后按paragraphId回报。语言切换、字号变化、重开使用同段ID。隐藏时不刷新长正文。普通邮件点击隐藏来信面板，不完成终局。

首次双击/Enter/明确打开按钮先显示告知，默认焦点“暂不打开”；确认后才发打开请求，已持久consentShown可继续阅读。取消不发完成。完成按钮是独立最终请求，等待期间冻结重复请求和返回按钮；失败留信可重试。只读回看不提供保存/完成写路径，返回按钮写“返回结局档案”。有色圆点同时有未打开/未读完文字，不靠红点消失表达通关。

## 共享邮件最小变更

- 原毕业通知所在索引3原位替换为来源稿的普通值班表邮件，日期仍2016-05-22；其他行与会话内阅读标志保留。原类没有持久邮件ID，候选为6行增加稳定key，明确旧槽3对应 `background.work_roster`，不声称存在旧ID或完成了新的普通邮件持久化。
- 退稿发件人明确“虚构投稿系统”。保留旧剧情flag作为必要条件，额外要求 `HasVerifiedPreprintSubmission(actualSave)`；该predicate默认空，因此无证据时隐藏。root必须接真实游戏内论文提交回执，不能从已选路线/旧flag推断提交。
- 发现独立遗留项：`Story/six_stage_story.json` 的 `ending_reject` 仍在 `lg.project:completed` 后直接发 `six_reject_notice` 并设flag。本包没有修改共享Story；root应为该通知补相同真实提交证据门，否则邮箱隐藏但旧剧情通知仍可能说有退稿。不能放松StoryLibrary目录验证。

## 证据和仍未接线项

已做：3信/27双语段落的54段文字均逐段匹配定稿；30UI key成对且引用完整；4个C#候选仅词法括号检查；无chapterComplete写入、AtomicSaveStore实例或LocalLlm调用；共享patch的只读 `git apply --check` 退出0。详见 `static-inspection.json`。这些均不证明编译或运行通过。

尚需root：合入与编译；E-A五动作/实际owner修订适配；公开专栏三篇入口与持久回执到邮箱绑定；正式完成前置门与标题档案路由；真实提交证据provider及旧剧情退稿通知门；UiChecks加入串行Main runner；中文/英文、小窗口、真实双击/键盘/触控、最小化重开、只读回看实机验收；实际SaveNow失败/恢复及三路线自然完整通关证据。

三个LP私人插段仅在资源和纯目录提供条件筛选，默认Presenter完全不插入私人段落。当前未收到可验证的sourceRef/已读/作者归属生产接口，因此不伪造私人回复，也不自行监听YY。将来若接插段，需owner提供完整证据投影并加来源跳转；“已公开”时态不自动借用“准备公开”稿。公开三信本身已完整可读，不因此阻塞公开路径。

UI没有真实浏览/声音/性能验收证据，不把静态预算说成性能测量；模型服务不会由本包启动、取消或关闭。
