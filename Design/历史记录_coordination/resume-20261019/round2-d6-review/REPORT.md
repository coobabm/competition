# D6 只读审查：关闭，不合入

结论：D6 是私人附段规则变更，没有发现可与该变更分离、值得单独合入的 bug 修复。用户已明确选择 **“保留现有：以最后告知的计划为准”**；因此拒绝合入本包三文件，保留 D1 的 `LP2-latest-plan` 预期。精确决策见 `DECISIONS.md`。

本次只读生产、只写本审查目录；没有启动 Unity、没有改生产、没有编译或运行/新增测试，也没有制作替代产品补丁。生产被其他 Unity 任务占用的限制始终遵守。

## 收件和审查范围

完整阅读并固定三份已到源码，与生产逐文件对照：

| 文件 | D6 SHA256 | 生产 SHA256 |
|---|---|---|
| EndingPrivateDisclosure.cs | `9e82f123d013608935b046707092ced512b8213f899cba7c1676adb7b402f630` | `6351546a291be4e1138b9916f067efb71f835749186de37da2bdad451505ed92` |
| XgEpilogueMailBridge.cs | `9779d6e1790d3daef8ea734edbe6762ad101d97b49dc6cc1a52540163f2f0859` | `448131cf938ccd68db0b9df0c7799a204b363543efba7afacca0520c2f39ae2b` |
| EndingPrivateDisclosureTests.cs | `95002e2674ed6a6d676f12daf660a84143e08bd4a6a3663dca083f763c1ee5fe` | `2512dd22f8f5db55d055eb6636e823958fae083823a2e41ae90abbc2d9c85445` |

审查时 D6 自身未到 README/REPORT/QUESTIONS；已阅读并保存 Round2 根 README（只涉及 D1–D3），以及 D1 REPORT/QUESTIONS 的相关规则说明。不能把根 README 的“三包无设定冲突”用于 D6。D6 源码的 “Producer rule” 注释不是用户授权依据。

`snapshot/` 保留 D6 原件，`baseline/` 保留本次读取的生产原件，`diffs/` 是逐文件差异，`manifest.json` 记录初始 hash，`final-source-check.json` 记录终审来源复核。

## 唯一实质变更及冲突

`EndingPrivateDisclosure.cs:120–145` 抽出 FindPlan 并新增 `PlannedFact(state, finalRoute)`，从后向前寻找 **该最终路线** 的任一有效计划回执；保留原单参 overload 为“诊断”。`XgEpilogueMailBridge.cs:205–218` 将在线 YY 状态与序列化 fallback 两个路径都切至该新 overload，finalRoute 取当前绑定 lab 的 releaseOutcome.route。

| 已实际发送的计划顺序 | 最终路线 | 生产现规则 | D6 |
|---|---|---|---|
| online → public_model | online | 无 LP1 | LP1，引用较早 online 回执 |
| public_model → online | public_model | 无 LP2 | LP2，引用较早 public_model 回执 |
| online → local_only | online | 无 LP1 | LP1 |
| online → local_only | local_only | LP4 | LP4 |
| 只有 online | public_model | 无附段 | 无附段 |
| 没有有效 plan 回执 | 任意 | 无附段 | 无附段 |

这不是修复空引用、保存失败或旧档解析，而是改变“后来告知的计划是否覆盖 earlier 计划”的含义。三条路线本身、AI/玩家的决策归属、R3 转发与 shutdown 分开发送没有变更；但附段规则与用户本次明确确认冲突，所以整个行为变更被否决。

## 旧档与知情边界核对

- **格式兼容**：EndingPrivateReceipt/State 字段、版本和 Json 结构均未改；无迁移函数、无伪造回执、无覆盖存档操作。旧无回执或不完整回执仍不产生附段。新增测试的 round trip 是内存 Json 序列化，不是实际旧磁盘档验收。
- **呈现兼容并不等于格式兼容**：BindLetter 在普通阅读和已完成回放中都会调用 PrivateLetterDisclosure；所以 D6 也会改变多计划旧档在重新打开/回放时的附段。它没有把旧信件显示结果冻结。这是上述同一规则变更的历史档效果，不是额外独立 bug。
- **实际知情来源**：仍需要 Valid 回执（版本、作者绑定、老周收到、id/scope/sourceRef/text/replies）；选择结果自身不能创造 plan 回执。没有改 Offers/Receive/YYChatHub.SendEndingPrivate，也没有发送新消息或更新其他人物会话。只是从已发送历史中选择旧回执。
- **同档绑定**：mail bridge 的 Current、ObserveSave、BindLetter 和 epilogue 合法性守卫未变；live YY 只在 BoundTo(boundSave) 时读取，否则读取该 boundSave.chatState。未新增跨档状态入口。
- **中英/动态 AI 名**：所有中英玩家原话、老周回复、选择/预览文本、信件资源均未改。新增选择逻辑与语言无关，动态 AI 名处理未变。没有改变老周作者知情设定或让其他人物从后台阶段获取私人信息。
- **R3**：新函数未按 decisionOwner 分支，会按真实 release.route 选择曾告知的计划；没有替 AI 改路线或替玩家发送回答。这个影响仍属于已否决的附段选择变更，不另扩展测试。

没有找到可以从该包剥离出来的独立旧档修复、保存修复、中英修复或知情边界修复。

## D1 与现有测试的影响

若合入 D6，生产 D1 `EndingPrivateRemainingUiChecks.cs:381` 的 `LetterText(...,"zhou_open_door",null,...)` 将在 LP2-latest-plan 分支失败。`:35` Scope、`:38` RequiredResults、`:382–384` 图片/具名说明也仍描述旧规则。

D1 `:160` 仍调用单参 PlannedFact，因此会编译并通过其“最新计划”诊断，却不再代表 D6 mailbox fallback 的实际规则；`:370` 的 latest assertion 同样会继续通过。保留 overload 只避免编译错误，不会使旧 UI 预期正确。

D6 Tests 中 FinalChoiceOnlineAfterOnlineThenPublicPlansShowsLp1 / FinalChoicePublicAfterPublicThenOnlinePlansShowsLp2 等明确测试新规则；它替换了原 LatestPlanWinsAndReceiptSurvivesSaveReload。其他新增断言也都在验证新 overload 的空输入、序列化、只读和匹配行为，没有无关修复价值。该测试文件也不应单独合入。

**用户已选择现规则，所以上述 D1 调整全部不实施。** 原始 D1 的四个语言 helper、runner 清理和截图候选属于上一任务，独立于 D6；本次没有再修改它们。

## 最终处理

保留生产三个对应文件原样，保留 D1 既有最新计划预期，保留 D6 原件供追溯；不再为 D6 扩展新测试。root 可把 DECISIONS.md 的确认结论纳入之后的正式整合记录。本包审查关闭，不代表任何未运行测试已通过。
