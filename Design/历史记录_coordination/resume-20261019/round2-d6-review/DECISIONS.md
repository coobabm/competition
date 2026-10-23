# D6 决策记录（供 root 合入正式整合记录）

日期：2026-10-19。

root 已向用户提出精确分歧：先向老周告知 online 计划，后告知 public_model 计划，最终选择 online；现有实现无 LP1，D6 会追溯 earlier online 回执而显示 LP1。

用户明确回复（由 root 在本审查任务中转达）：**“保留现有：以最后告知的计划为准”。**

据此：

- 拒绝合入 D6 的按最终路线追溯匹配计划变更。
- 保留现有 `EndingPrivateDisclosure.PlannedFact(state)` 取最后有效计划的规则；不增加 D6 的按最终路线筛选 overload，不改生产 mailbox 调用。
- D1 `LP2-latest-plan` 的预期保持不变：online 后 public_model，最终 online → zhou_open_door 无 LP1。不能为适应 D6 而修改断言、Scope 或具名结果。
- D6 新增/替换的测试均服务于已否决规则；不合入、不扩展、不执行其新测试。
- D6 关闭为“已审查，产品规则变更被用户否决”。原交付、固定快照和差异证据保留，不删除。

本文件只在 `.coordination/resume-20261019/round2-d6-review/` 记录决策，尚未写入生产 Design；由 root 在生产编辑恢复允许后统一整合。
